/*
 * IceClash Phase 1 playable arena bootstrap.
 * Generates a vertically presented placeholder rink, boards, goals, local skater, independent physics puck,
 * and camera at runtime so the initial scene remains easy to inspect and every visual asset can be replaced later.
 */

using IceClash.CameraSystem;
using IceClash.Input;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IceClash.Hockey
{
    public sealed class PrototypeArenaBootstrap : MonoBehaviour
    {
        // World X maps across the Game view; world Z maps up the Game view.
        private const float RinkWidth = 20f;
        private const float RinkLength = 34f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForPrototypeArena()
        {
            if (SceneManager.GetActiveScene().name == "PrototypeArena" && FindAnyObjectByType<PrototypeArenaBootstrap>() == null)
            {
                new GameObject("Prototype Arena").AddComponent<PrototypeArenaBootstrap>();
            }
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            BuildArena();
        }

        public void BuildForValidation()
        {
            BuildArena();
        }

        private void BuildArena()
        {
            Material ice = MakeMaterial(new Color(0.72f, 0.9f, 1f));
            Material board = MakeMaterial(new Color(0.08f, 0.15f, 0.24f));
            Material red = MakeMaterial(new Color(0.88f, 0.15f, 0.18f));
            Material blue = MakeMaterial(new Color(0.08f, 0.37f, 0.9f));
            Material black = MakeMaterial(new Color(0.03f, 0.04f, 0.06f));

            CreateCube("Ice", Vector3.zero, new Vector3(RinkWidth, 0.4f, RinkLength), ice);
            CreateCube("Center Line", new Vector3(0f, 0.22f, 0f), new Vector3(RinkWidth - 1f, 0.03f, 0.25f), red, false);
            CreateCube("Blue Line A", new Vector3(0f, 0.22f, -RinkLength * 0.18f), new Vector3(RinkWidth - 1f, 0.03f, 0.16f), blue, false);
            CreateCube("Blue Line B", new Vector3(0f, 0.22f, RinkLength * 0.18f), new Vector3(RinkWidth - 1f, 0.03f, 0.16f), blue, false);

            CreateBoard("North Board", new Vector3(0f, 1.15f, RinkLength / 2f), new Vector3(RinkWidth + 1f, 2.3f, 0.5f), board);
            CreateBoard("South Board", new Vector3(0f, 1.15f, -RinkLength / 2f), new Vector3(RinkWidth + 1f, 2.3f, 0.5f), board);
            CreateBoard("East Board", new Vector3(RinkWidth / 2f, 1.15f, 0f), new Vector3(0.5f, 2.3f, RinkLength + 1f), board);
            CreateBoard("West Board", new Vector3(-RinkWidth / 2f, 1.15f, 0f), new Vector3(0.5f, 2.3f, RinkLength + 1f), board);
            CreateGoal("Blue Goal", new Vector3(0f, 0.95f, -RinkLength / 2f + 1.2f), blue);
            CreateGoal("Red Goal", new Vector3(0f, 0.95f, RinkLength / 2f - 1.2f), red);

            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Blue Skater (Local)";
            player.transform.SetPositionAndRotation(new Vector3(-2f, 1f, -6f), Quaternion.identity);
            player.GetComponent<Renderer>().material = blue;
            Destroy(player.GetComponent<Collider>());
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.45f;
            player.AddComponent<LocalPlayerInput>();
            player.AddComponent<PlayerController>();

            GameObject puck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puck.name = "Puck (Physics)";
            puck.transform.SetPositionAndRotation(new Vector3(0f, 0.55f, 0f), Quaternion.identity);
            puck.transform.localScale = new Vector3(0.65f, 0.1f, 0.65f);
            puck.GetComponent<Renderer>().material = black;
            Rigidbody body = puck.AddComponent<Rigidbody>();
            body.mass = 0.17f;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            puck.AddComponent<PuckController>();

            if (Camera.main != null) Destroy(Camera.main.gameObject);
            GameObject cameraObject = new GameObject("Elevated Follow Camera");
            cameraObject.tag = "MainCamera";
            Camera gameCamera = cameraObject.AddComponent<Camera>();
            gameCamera.fieldOfView = 58f;
            cameraObject.AddComponent<AudioListener>();
            ElevatedFollowCamera followCamera = cameraObject.AddComponent<ElevatedFollowCamera>();
            followCamera.Configure(player.transform, puck.transform);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.7f, 0.78f, 0.9f);
        }

        private static void CreateGoal(string goalName, Vector3 center, Material material)
        {
            CreateCube(goalName + " Post A", center + new Vector3(-2f, 0f, 0f), new Vector3(0.2f, 1.9f, 0.2f), material, false);
            CreateCube(goalName + " Post B", center + new Vector3(2f, 0f, 0f), new Vector3(0.2f, 1.9f, 0.2f), material, false);
            CreateCube(goalName + " Crossbar", center + new Vector3(0f, 0.95f, 0f), new Vector3(4.2f, 0.2f, 0.2f), material, false);
        }

        private static void CreateBoard(string boardName, Vector3 position, Vector3 scale, Material material) => CreateCube(boardName, position, scale, material);

        private static GameObject CreateCube(string objectName, Vector3 position, Vector3 scale, Material material, bool collider = true)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            cube.transform.SetPositionAndRotation(position, Quaternion.identity);
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().material = material;
            if (!collider) Destroy(cube.GetComponent<Collider>());
            return cube;
        }

        private static Material MakeMaterial(Color color)
        {
            Material material = new(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            material.color = color;
            return material;
        }
    }
}
