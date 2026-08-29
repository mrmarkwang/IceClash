/*
 * IceClash Phase 1 local PvE arena bootstrap.
 * Generates the mobile-first marked rink, layered boards/glass, dimensional nets,
 * one-way goals/triggers, puck, 5v5-plus-goalies roster, match flow, HUD, and camera.
 * Shared rink geometry includes the corner and center-circle radii used by
 * gameplay systems. Goal triggers track the puck for swept high-speed scoring.
 * Visual primitive colliders are disabled before deferred cleanup.
 */

using System.Collections.Generic;
using IceClash.CameraSystem;
using IceClash.Core;
using IceClash.Match;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IceClash.Hockey
{
    internal static class PrototypeRinkGeometry
    {
        internal const float Width = 24f;
        internal const float Length = 48f;
        internal const float CornerRadius = 5f;
        internal const float GoalDepth = 0.9f;
        internal const float GoalLineDistance = Length / 2f - 3.2f;
        internal const float GoalieAnchor = GoalLineDistance - 0.65f;
        internal const float CenterFaceoffCircleRadius = 2.4f;
    }

    public sealed class PrototypeArenaBootstrap : MonoBehaviour
    {
        // World X maps across the Game view; world Z maps up the Game view.
        private const float RinkWidth = PrototypeRinkGeometry.Width;
        private const float RinkLength = PrototypeRinkGeometry.Length;
        private const float CornerRadius = PrototypeRinkGeometry.CornerRadius;
        private const int CornerSegments = 8;
        private const float BoardHeight = 1.05f;
        private const float BoardThickness = 0.36f;
        private const float GlassHeight = 1.25f;
        private const float IceSurfaceY = 0.2f;
        private const float IceThickness = 0.45f;
        private const float PuckDiameter = 0.42f;
        private const float PuckHeight = 0.12f;
        private const float GoalHalfWidth = 1.5f;
        private const float GoalHeight = 1.25f;
        private const float GoalDepth = PrototypeRinkGeometry.GoalDepth;
        private const float GoalLineDistance = PrototypeRinkGeometry.GoalLineDistance;
        private const float GoalTriggerWidth = 2.8f;
        private bool hasBuilt;

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
            if (hasBuilt) return;
            hasBuilt = true;
            Material ice = MakeMaterial(new Color(0.9f, 0.96f, 1f));
            Material arenaFloor = MakeMaterial(new Color(0.035f, 0.065f, 0.1f));
            Material board = MakeMaterial(new Color(0.92f, 0.94f, 0.95f));
            Material kickplate = MakeMaterial(new Color(0.98f, 0.72f, 0.08f));
            Material rail = MakeMaterial(new Color(0.04f, 0.2f, 0.55f));
            Material glass = MakeTransparentMaterial(new Color(0.72f, 0.9f, 1f, 0.22f));
            Material red = MakeMaterial(new Color(0.88f, 0.15f, 0.18f));
            Material blue = MakeMaterial(new Color(0.08f, 0.37f, 0.9f));
            Material black = MakeMaterial(new Color(0.03f, 0.04f, 0.06f));
            Material net = MakeTransparentMaterial(new Color(0.94f, 0.98f, 1f, 0.72f));
            Material redLine = MakeLineMaterial(red.color);
            Material blueLine = MakeLineMaterial(blue.color);

            List<Vector3> rinkOutline = CreateRinkOutline();
            CreateCube("Arena Floor", new Vector3(0f, -0.38f, 0f), new Vector3(RinkWidth + 7f, 0.25f, RinkLength + 7f), arenaFloor, false);
            CreateRoundedIce(rinkOutline, ice);
            CreateRoundedBoards(rinkOutline, board, kickplate, rail, glass);
            CreateCube("Center Line", new Vector3(0f, 0.22f, 0f), new Vector3(RinkWidth - 1f, 0.03f, 0.25f), red, false);
            CreateCube("Blue Line A", new Vector3(0f, 0.22f, -RinkLength * 0.18f), new Vector3(RinkWidth - 1f, 0.03f, 0.16f), blue, false);
            CreateCube("Blue Line B", new Vector3(0f, 0.22f, RinkLength * 0.18f), new Vector3(RinkWidth - 1f, 0.03f, 0.16f), blue, false);
            CreateHockeyMarkings(red, blue, redLine, blueLine);

            CreateGoal("Blue Goal", new Vector3(0f, GoalHeight / 2f, -GoalLineDistance), red, net);
            CreateGoal("Red Goal", new Vector3(0f, GoalHeight / 2f, GoalLineDistance), red, net);

            GameObject puck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puck.name = "Puck (Physics)";
            puck.transform.SetPositionAndRotation(new Vector3(0f, 0.55f, 0f), Quaternion.identity);
            // Unity's cylinder is one unit wide and two units tall.
            puck.transform.localScale = new Vector3(PuckDiameter, PuckHeight * 0.5f, PuckDiameter);
            puck.GetComponent<Renderer>().material = black;
            Collider primitiveCollider = puck.GetComponent<Collider>();
            primitiveCollider.enabled = false;
            puck.AddComponent<BoxCollider>();
            Destroy(primitiveCollider);
            Rigidbody body = puck.AddComponent<Rigidbody>();
            body.mass = 0.17f;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            puck.AddComponent<PuckController>();

            GameObject skaterPrefab = Resources.Load<GameObject>("Skater");
            if (skaterPrefab == null) throw new System.InvalidOperationException("Missing Phase 3 skater prefab at Assets/_Project/Prefabs/Resources/Skater.prefab.");
            LocalMatchSetup matchSetup = new GameObject("Local PvE 5v5 Match").AddComponent<LocalMatchSetup>();
            PlayerController player = matchSetup.BuildRoster(skaterPrefab, puck.GetComponent<PuckController>(), blue, red);
            CreateGoalTrigger("Blue Goal Trigger", new Vector3(0f, GoalHeight / 2f, -GoalLineDistance - GoalDepth * 0.5f), TeamId.Red, Vector3.back, matchSetup.MatchController, puck.GetComponent<PuckController>());
            CreateGoalTrigger("Red Goal Trigger", new Vector3(0f, GoalHeight / 2f, GoalLineDistance + GoalDepth * 0.5f), TeamId.Blue, Vector3.forward, matchSetup.MatchController, puck.GetComponent<PuckController>());

            if (Camera.main != null) Destroy(Camera.main.gameObject);
            GameObject cameraObject = new GameObject("Hockey Camera");
            cameraObject.tag = "MainCamera";
            Camera gameCamera = cameraObject.AddComponent<Camera>();
            gameCamera.fieldOfView = 46f;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = new Color(0.025f, 0.045f, 0.075f);
            cameraObject.AddComponent<AudioListener>();
            HockeyCameraController followCamera = cameraObject.AddComponent<HockeyCameraController>();
            followCamera.Configure(player.transform, puck.transform);
            matchSetup.SwitchController.SetCamera(followCamera);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.7f, 0.78f, 0.9f);
        }

        private static void CreateGoalTrigger(string triggerName, Vector3 position, TeamId scoringTeam,
            Vector3 scoringDirection, MatchController match, PuckController puck)
        {
            GameObject trigger = new(triggerName);
            trigger.transform.position = position;
            BoxCollider volume = trigger.AddComponent<BoxCollider>();
            volume.size = new Vector3(GoalTriggerWidth, 1.5f, GoalDepth);
            GoalTrigger goal = trigger.AddComponent<GoalTrigger>();
            goal.Configure(match, puck, scoringTeam, scoringDirection);
        }

        private static void CreateGoal(string goalName, Vector3 center, Material material, Material netMaterial)
        {
            float direction = center.z > 0f ? 1f : -1f;
            float backZ = center.z + direction * GoalDepth;
            CreateCube(goalName + " Post A", center + new Vector3(-GoalHalfWidth, 0f, 0f), new Vector3(0.18f, GoalHeight, 0.18f), material, false);
            CreateCube(goalName + " Post B", center + new Vector3(GoalHalfWidth, 0f, 0f), new Vector3(0.18f, GoalHeight, 0.18f), material, false);
            CreateCube(goalName + " Crossbar", center + new Vector3(0f, GoalHeight / 2f, 0f), new Vector3(GoalHalfWidth * 2f + 0.18f, 0.18f, 0.18f), material, false);
            CreateCube(goalName + " Rear Post A", new Vector3(-GoalHalfWidth, center.y, backZ), new Vector3(0.14f, GoalHeight, 0.14f), material, false);
            CreateCube(goalName + " Rear Post B", new Vector3(GoalHalfWidth, center.y, backZ), new Vector3(0.14f, GoalHeight, 0.14f), material, false);
            CreateCube(goalName + " Rear Crossbar", new Vector3(0f, center.y + GoalHeight / 2f, backZ), new Vector3(GoalHalfWidth * 2f, 0.14f, 0.14f), material, false);
            CreateCube(goalName + " Roof Rail A", new Vector3(-GoalHalfWidth, center.y + GoalHeight / 2f, center.z + direction * GoalDepth / 2f), new Vector3(0.14f, 0.14f, GoalDepth), material, false);
            CreateCube(goalName + " Roof Rail B", new Vector3(GoalHalfWidth, center.y + GoalHeight / 2f, center.z + direction * GoalDepth / 2f), new Vector3(0.14f, 0.14f, GoalDepth), material, false);
            CreateCube(goalName + " Base Rail A", new Vector3(-GoalHalfWidth, IceSurfaceY + 0.05f, center.z + direction * GoalDepth / 2f), new Vector3(0.14f, 0.1f, GoalDepth), material, false);
            CreateCube(goalName + " Base Rail B", new Vector3(GoalHalfWidth, IceSurfaceY + 0.05f, center.z + direction * GoalDepth / 2f), new Vector3(0.14f, 0.1f, GoalDepth), material, false);
            CreateGoalNet(goalName, center, netMaterial);
        }

        private static void CreateGoalNet(string goalName, Vector3 center, Material material)
        {
            float backOffset = center.z > 0f ? GoalDepth : -GoalDepth;
            float backZ = center.z + backOffset;
            for (int index = -4; index <= 4; index++)
            {
                float x = index * GoalHalfWidth / 4f;
                CreateCube($"{goalName} Net Back Vertical {index + 4}", new Vector3(x, center.y, backZ), new Vector3(0.035f, GoalHeight, 0.035f), material, false);
                CreateCube($"{goalName} Net Roof Longitudinal {index + 4}", new Vector3(x, center.y + GoalHeight / 2f, center.z + backOffset / 2f), new Vector3(0.035f, 0.035f, GoalDepth), material, false);
            }

            for (int index = 0; index <= 6; index++)
            {
                float y = IceSurfaceY + index * (GoalHeight - IceSurfaceY) / 6f;
                CreateCube($"{goalName} Net Back Horizontal {index}", new Vector3(0f, y, backZ), new Vector3(GoalHalfWidth * 2f, 0.035f, 0.035f), material, false);
                CreateCube($"{goalName} Net Side A Horizontal {index}", new Vector3(-GoalHalfWidth, y, center.z + backOffset / 2f), new Vector3(0.035f, 0.035f, GoalDepth), material, false);
                CreateCube($"{goalName} Net Side B Horizontal {index}", new Vector3(GoalHalfWidth, y, center.z + backOffset / 2f), new Vector3(0.035f, 0.035f, GoalDepth), material, false);
            }

            for (int index = 1; index < 4; index++)
            {
                float z = center.z + backOffset * index / 4f;
                CreateCube($"{goalName} Net Roof Transverse {index}", new Vector3(0f, center.y + GoalHeight / 2f, z), new Vector3(GoalHalfWidth * 2f, 0.035f, 0.035f), material, false);
                CreateCube($"{goalName} Net Side A Vertical {index}", new Vector3(-GoalHalfWidth, center.y, z), new Vector3(0.035f, GoalHeight, 0.035f), material, false);
                CreateCube($"{goalName} Net Side B Vertical {index}", new Vector3(GoalHalfWidth, center.y, z), new Vector3(0.035f, GoalHeight, 0.035f), material, false);
            }
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
            float topY = IceSurfaceY;
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

        private static void CreateRoundedBoards(List<Vector3> outline, Material boardMaterial, Material kickplateMaterial, Material railMaterial, Material glassMaterial)
        {
            for (int index = 0; index < outline.Count; index++)
            {
                Vector3 start = outline[index];
                Vector3 end = outline[(index + 1) % outline.Count];
                Vector3 direction = end - start;
                Vector3 midpoint = (start + end) / 2f;
                Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                float length = direction.magnitude + BoardThickness * 0.35f;
                CreateBoardLayer($"Rink Board {index:00}", midpoint + Vector3.up * (IceSurfaceY + BoardHeight / 2f), rotation, new Vector3(BoardThickness, BoardHeight, length), boardMaterial, true);
                CreateBoardLayer($"Yellow Kickplate {index:00}", midpoint + Vector3.up * (IceSurfaceY + 0.1f), rotation, new Vector3(BoardThickness + 0.025f, 0.2f, length), kickplateMaterial, false);
                CreateBoardLayer($"Blue Top Rail {index:00}", midpoint + Vector3.up * (IceSurfaceY + BoardHeight + 0.06f), rotation, new Vector3(BoardThickness + 0.05f, 0.12f, length), railMaterial, false);
                CreateBoardLayer($"Rink Glass {index:00}", midpoint + Vector3.up * (IceSurfaceY + BoardHeight + GlassHeight / 2f), rotation, new Vector3(0.055f, GlassHeight, direction.magnitude), glassMaterial, false);
            }
        }

        private static void CreateBoardLayer(string objectName, Vector3 position, Quaternion rotation, Vector3 scale, Material material, bool collider)
        {
            GameObject layer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            layer.name = objectName;
            layer.transform.SetPositionAndRotation(position, rotation);
            layer.transform.localScale = scale;
            layer.GetComponent<Renderer>().material = material;
            if (!collider) DisableAndDestroyCollider(layer);
        }

        private static void CreateHockeyMarkings(Material red, Material blue, Material redLine, Material blueLine)
        {
            float goalLineDistance = GoalLineDistance;
            CreateCube("Blue Goal Line", new Vector3(0f, 0.22f, -goalLineDistance), new Vector3(RinkWidth - CornerRadius * 2f, 0.03f, 0.13f), red, false);
            CreateCube("Red Goal Line", new Vector3(0f, 0.22f, goalLineDistance), new Vector3(RinkWidth - CornerRadius * 2f, 0.03f, 0.13f), red, false);
            CreateGoalCrease("Blue Goal Crease", new Vector3(0f, 0.23f, -goalLineDistance + 0.4f), true, blueLine);
            CreateGoalCrease("Red Goal Crease", new Vector3(0f, 0.23f, goalLineDistance - 0.4f), false, blueLine);
            CreateFaceoffMarkings(red, blue, redLine, blueLine);
        }

        private static void CreateFaceoffMarkings(Material red, Material blue, Material redLine, Material blueLine)
        {
            CreateCircleMark("Center Faceoff Circle", Vector3.up * 0.23f, PrototypeRinkGeometry.CenterFaceoffCircleRadius, blueLine, 36);
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
            DisableAndDestroyCollider(dot);
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
            if (!collider) DisableAndDestroyCollider(cube);
            return cube;
        }

        private static void DisableAndDestroyCollider(GameObject target)
        {
            Collider targetCollider = target != null ? target.GetComponent<Collider>() : null;
            if (targetCollider == null) return;
            targetCollider.enabled = false;
            Destroy(targetCollider);
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

        private static Material MakeTransparentMaterial(Color color)
        {
            Material material = MakeMaterial(color);
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_Mode", 2f);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return material;
        }
    }
}
