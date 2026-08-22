/*
 * IceClash Phase 1 playable arena bootstrap.
 * Generates a vertically presented hockey-rink outline with a solid ice foundation, rounded boards, inset goals,
 * smooth center/faceoff markings, netted goals, a local skater, independent physics puck, and camera at runtime.
 * Phase 2 attaches the configurable action/control components to the existing local skater.
 */

using System.Collections.Generic;
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
        private const float CornerRadius = 3.6f;
        private const int CornerSegments = 6;
        private const float BoardHeight = 2.3f;
        private const float BoardThickness = 0.45f;
        private const float IceThickness = 0.45f;

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
            Material net = MakeMaterial(new Color(0.92f, 0.95f, 1f));
            Material redLine = MakeLineMaterial(red.color);
            Material blueLine = MakeLineMaterial(blue.color);

            List<Vector3> rinkOutline = CreateRinkOutline();
            CreateRoundedIce(rinkOutline, ice);
            CreateRoundedBoards(rinkOutline, board);
            CreateCube("Center Line", new Vector3(0f, 0.22f, 0f), new Vector3(RinkWidth - 1f, 0.03f, 0.25f), red, false);
            CreateCube("Blue Line A", new Vector3(0f, 0.22f, -RinkLength * 0.18f), new Vector3(RinkWidth - 1f, 0.03f, 0.16f), blue, false);
            CreateCube("Blue Line B", new Vector3(0f, 0.22f, RinkLength * 0.18f), new Vector3(RinkWidth - 1f, 0.03f, 0.16f), blue, false);
            CreateHockeyMarkings(red, blue, redLine, blueLine);

            CreateGoal("Blue Goal", new Vector3(0f, 0.95f, -RinkLength / 2f + 2.1f), blue, net);
            CreateGoal("Red Goal", new Vector3(0f, 0.95f, RinkLength / 2f - 2.1f), red, net);

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

        private static void CreateGoal(string goalName, Vector3 center, Material material, Material netMaterial)
        {
            CreateCube(goalName + " Post A", center + new Vector3(-2f, 0f, 0f), new Vector3(0.2f, 1.9f, 0.2f), material, false);
            CreateCube(goalName + " Post B", center + new Vector3(2f, 0f, 0f), new Vector3(0.2f, 1.9f, 0.2f), material, false);
            CreateCube(goalName + " Crossbar", center + new Vector3(0f, 0.95f, 0f), new Vector3(4.2f, 0.2f, 0.2f), material, false);
            CreateGoalNet(goalName, center, netMaterial);
        }

        private static void CreateGoalNet(string goalName, Vector3 center, Material material)
        {
            float backOffset = center.z > 0f ? 0.85f : -0.85f;
            float backZ = center.z + backOffset;
            for (int index = -2; index <= 2; index++)
            {
                CreateCube($"{goalName} Net Vertical {index + 2}", new Vector3(index, 0.95f, backZ), new Vector3(0.045f, 1.85f, 0.045f), material, false);
            }

            for (int index = 0; index < 5; index++)
            {
                CreateCube($"{goalName} Net Horizontal {index}", new Vector3(0f, 0.12f + index * 0.44f, backZ), new Vector3(4.05f, 0.045f, 0.045f), material, false);
            }

            CreateCube($"{goalName} Net Side A", new Vector3(-2f, 0.12f, center.z + backOffset / 2f), new Vector3(0.05f, 0.05f, Mathf.Abs(backOffset)), material, false);
            CreateCube($"{goalName} Net Side B", new Vector3(2f, 0.12f, center.z + backOffset / 2f), new Vector3(0.05f, 0.05f, Mathf.Abs(backOffset)), material, false);
        }

        private static List<Vector3> CreateRinkOutline()
        {
            float halfWidth = RinkWidth / 2f;
            float halfLength = RinkLength / 2f;
            List<Vector3> points = new();

            AppendArc(points, new Vector3(halfWidth - CornerRadius, 0f, -halfLength + CornerRadius), -90f, 0f);
            AppendArc(points, new Vector3(halfWidth - CornerRadius, 0f, halfLength - CornerRadius), 0f, 90f);
            AppendArc(points, new Vector3(-halfWidth + CornerRadius, 0f, halfLength - CornerRadius), 90f, 180f);
            AppendArc(points, new Vector3(-halfWidth + CornerRadius, 0f, -halfLength + CornerRadius), 180f, 270f);
            return points;
        }

        private static void AppendArc(List<Vector3> points, Vector3 center, float startDegrees, float endDegrees)
        {
            for (int index = 0; index <= CornerSegments; index++)
            {
                float angle = Mathf.Lerp(startDegrees, endDegrees, index / (float)CornerSegments) * Mathf.Deg2Rad;
                points.Add(center + new Vector3(Mathf.Cos(angle) * CornerRadius, 0f, Mathf.Sin(angle) * CornerRadius));
            }
        }

        private static void CreateRoundedIce(List<Vector3> outline, Material material)
        {
            GameObject ice = new("Ice");
            Mesh mesh = new();
            float topY = 0.2f;
            float bottomY = topY - IceThickness;
            int topCenter = 0;
            int bottomCenter = outline.Count + 1;
            List<Vector3> vertices = new() { new Vector3(0f, topY, 0f) };
            for (int index = 0; index < outline.Count; index++) vertices.Add(outline[index] + Vector3.up * topY);
            vertices.Add(new Vector3(0f, bottomY, 0f));
            for (int index = 0; index < outline.Count; index++) vertices.Add(outline[index] + Vector3.up * bottomY);

            List<int> triangles = new();
            for (int index = 0; index < outline.Count; index++)
            {
                int next = (index + 1) % outline.Count;
                int topCurrent = index + 1;
                int topNext = next + 1;
                int bottomCurrent = bottomCenter + index + 1;
                int bottomNext = bottomCenter + next + 1;

                // Reverse the previous winding so the top face and MeshCollider point upward.
                triangles.Add(topCenter);
                triangles.Add(topNext);
                triangles.Add(topCurrent);

                triangles.Add(bottomCenter);
                triangles.Add(bottomCurrent);
                triangles.Add(bottomNext);

                triangles.Add(topCurrent);
                triangles.Add(bottomCurrent);
                triangles.Add(bottomNext);
                triangles.Add(topCurrent);
                triangles.Add(bottomNext);
                triangles.Add(topNext);
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            ice.AddComponent<MeshFilter>().sharedMesh = mesh;
            ice.AddComponent<MeshRenderer>().sharedMaterial = material;
            ice.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static void CreateRoundedBoards(List<Vector3> outline, Material material)
        {
            for (int index = 0; index < outline.Count; index++)
            {
                Vector3 start = outline[index];
                Vector3 end = outline[(index + 1) % outline.Count];
                Vector3 direction = end - start;
                GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
                board.name = $"Rounded Board {index:00}";
                board.transform.SetPositionAndRotation((start + end) / 2f + Vector3.up * (BoardHeight / 2f), Quaternion.LookRotation(direction.normalized, Vector3.up));
                board.transform.localScale = new Vector3(BoardThickness, BoardHeight, direction.magnitude + BoardThickness * 0.35f);
                board.GetComponent<Renderer>().material = material;
            }
        }

        private static void CreateHockeyMarkings(Material red, Material blue, Material redLine, Material blueLine)
        {
            float goalLineDistance = RinkLength / 2f - 3.3f;
            CreateCube("Blue Goal Line", new Vector3(0f, 0.22f, -goalLineDistance), new Vector3(RinkWidth - CornerRadius * 2f, 0.03f, 0.13f), red, false);
            CreateCube("Red Goal Line", new Vector3(0f, 0.22f, goalLineDistance), new Vector3(RinkWidth - CornerRadius * 2f, 0.03f, 0.13f), red, false);
            CreateGoalCrease("Blue Goal Crease", new Vector3(0f, 0.23f, -goalLineDistance + 0.4f), true, blueLine);
            CreateGoalCrease("Red Goal Crease", new Vector3(0f, 0.23f, goalLineDistance - 0.4f), false, blueLine);
            CreateFaceoffMarkings(red, blue, redLine, blueLine);
        }

        private static void CreateFaceoffMarkings(Material red, Material blue, Material redLine, Material blueLine)
        {
            CreateCircleMark("Center Faceoff Circle", Vector3.up * 0.23f, 2.4f, blueLine, 36);
            CreateFaceoffDot("Center Faceoff Dot", Vector3.up * 0.25f, blue);

            float zoneCircleX = RinkWidth * 0.21f;
            float zoneCircleZ = RinkLength * 0.28f;
            CreateFaceoffCircle("Faceoff Circle South West", new Vector3(-zoneCircleX, 0.23f, -zoneCircleZ), red, redLine);
            CreateFaceoffCircle("Faceoff Circle South East", new Vector3(zoneCircleX, 0.23f, -zoneCircleZ), red, redLine);
            CreateFaceoffCircle("Faceoff Circle North West", new Vector3(-zoneCircleX, 0.23f, zoneCircleZ), red, redLine);
            CreateFaceoffCircle("Faceoff Circle North East", new Vector3(zoneCircleX, 0.23f, zoneCircleZ), red, redLine);

            float neutralDotX = RinkWidth * 0.21f;
            float neutralDotZ = RinkLength * 0.075f;
            CreateFaceoffDot("Neutral Dot South West", new Vector3(-neutralDotX, 0.25f, -neutralDotZ), red);
            CreateFaceoffDot("Neutral Dot South East", new Vector3(neutralDotX, 0.25f, -neutralDotZ), red);
            CreateFaceoffDot("Neutral Dot North West", new Vector3(-neutralDotX, 0.25f, neutralDotZ), red);
            CreateFaceoffDot("Neutral Dot North East", new Vector3(neutralDotX, 0.25f, neutralDotZ), red);
        }

        private static void CreateFaceoffCircle(string circleName, Vector3 center, Material material, Material lineMaterial)
        {
            CreateCircleMark(circleName, center, 2.05f, lineMaterial, 32);
            CreateFaceoffDot(circleName + " Dot", center + Vector3.up * 0.02f, material);
            CreateCube(circleName + " Hash Horizontal", center + new Vector3(0f, 0.01f, -0.68f), new Vector3(1.15f, 0.03f, 0.09f), material, false);
            CreateCube(circleName + " Hash Vertical", center + new Vector3(0f, 0.01f, -0.68f), new Vector3(0.09f, 0.03f, 1.15f), material, false);
        }

        private static void CreateFaceoffDot(string dotName, Vector3 position, Material material)
        {
            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dot.name = dotName;
            dot.transform.SetPositionAndRotation(position, Quaternion.identity);
            dot.transform.localScale = new Vector3(0.17f, 0.025f, 0.17f);
            dot.GetComponent<Renderer>().material = material;
            Destroy(dot.GetComponent<Collider>());
        }

        private static void CreateCircleMark(string circleName, Vector3 center, float radius, Material material, int segments)
        {
            CreateArcLine(circleName, center, radius, 0f, 360f, material, segments, true);
        }

        private static void CreateGoalCrease(string creaseName, Vector3 center, bool facesNorth, Material material)
        {
            float start = facesNorth ? 0f : 180f;
            float end = facesNorth ? 180f : 360f;
            CreateArcLine(creaseName, center, 2.2f, start, end, material, 20, false);
        }

        private static void CreateArcLine(string lineName, Vector3 center, float radius, float startDegrees, float endDegrees, Material material, int segments, bool loop)
        {
            GameObject lineObject = new(lineName);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.loop = loop;
            line.widthMultiplier = 0.1f;
            line.numCornerVertices = 4;
            line.numCapVertices = 4;
            line.positionCount = loop ? segments : segments + 1;

            for (int index = 0; index < line.positionCount; index++)
            {
                float angle = Mathf.Lerp(startDegrees, endDegrees, index / (float)segments) * Mathf.Deg2Rad;
                line.SetPosition(index, center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }
        }

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

        private static Material MakeLineMaterial(Color color)
        {
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            Material material = new(shader) { color = color };
            return material;
        }
    }
}
