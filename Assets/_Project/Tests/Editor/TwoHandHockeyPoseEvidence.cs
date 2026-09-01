/*
 * IceClash deterministic two-hand hockey pose evidence tooling.
 * Builds an isolated idle validation scene, captures requested orthographic
 * views in Play Mode, and records exact grip/IK transforms and settings.
 */

#if UNITY_EDITOR
using System;
using System.IO;
using IceClash.Hockey.Character;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.SceneManagement;

namespace IceClash.Tests.Editor
{
    [InitializeOnLoad]
    public static class TwoHandHockeyPoseEvidence
    {
        public const string ScenePath = "Assets/_Project/Scenes/TwoHandHockeyPoseTest.unity";
        public const string EvidenceDirectory = ".docs/evidence/two-handed-hockey-stick-pose";
        private const string PendingKey = "IceClash.TwoHandPoseCapturePending";
        private static double captureAfter;

        static TwoHandHockeyPoseEvidence()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("IceClash/Generate Two-Hand Hockey Pose Test")]
        public static void GenerateScene()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/HockeyPlayer.prefab")
                ?? throw new FileNotFoundException("Generated HockeyPlayer prefab is missing.");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject player = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject
                ?? throw new InvalidOperationException("Unable to instantiate HockeyPlayer for pose validation.");
            player.name = "Male_Base_v1_1_Clean Hockey Grip Test";
            player.transform.SetPositionAndRotation(new Vector3(0f, 1f, 0f), Quaternion.identity);
            player.transform.localScale = Vector3.one * 0.68f;
            HockeyEquipmentLoadout loadout = player.GetComponent<HockeyEquipmentLoadout>();
            foreach (HockeyEquipmentBinding binding in loadout.Slots)
                if (binding.Slot != HockeyEquipmentSlot.Stick && binding.Slot != HockeyEquipmentSlot.Skates)
                    binding.Equipped.SetActive(false);

            GameObject ice = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ice.name = "Validation Ice";
            ice.transform.position = new Vector3(0f, 0.2f, 0f);
            ice.transform.localScale = new Vector3(0.45f, 1f, 0.45f);
            Renderer iceRenderer = ice.GetComponent<Renderer>();
            Material ground = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Equipment/Sticks/Hockey_Stick_Base_v1/Hockey_Stick_Base_v1_Ground.mat");
            if (ground != null) iceRenderer.sharedMaterial = ground;

            GameObject cameraObject = new("Pose Camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.035f, 0.055f);
            camera.nearClipPlane = 0.01f;
            camera.fieldOfView = 32f;

            CreateLight("Pose Key", 1.15f, Quaternion.Euler(42f, -32f, 0f));
            CreateLight("Pose Fill", 0.5f, Quaternion.Euler(28f, 150f, 0f));
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.38f);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Unable to save the two-hand hockey pose scene.");
            AssetDatabase.SaveAssets();
            Debug.Log("TWO_HAND_HOCKEY_POSE_SCENE_GENERATED");
        }

        [MenuItem("IceClash/Capture Two-Hand Hockey Pose Evidence")]
        public static void CaptureBatch()
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("Exit Play Mode before starting deterministic pose capture.");
            GenerateScene();
            SessionState.SetBool(PendingKey, true);
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("IceClash/Capture Two-Hand Gameplay Camera Evidence")]
        public static void CaptureGameplayCamera()
        {
            if (!Application.isPlaying)
                throw new InvalidOperationException("PrototypeArena must be in Play Mode for gameplay-camera capture.");
            string directory = AbsoluteEvidenceDirectory();
            Directory.CreateDirectory(directory);
            ScreenCapture.CaptureScreenshot(Path.Combine(directory, "gameplay-camera.png"), 1);
            Debug.Log("TWO_HAND_HOCKEY_GAMEPLAY_CAPTURED");
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PendingKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                captureAfter = EditorApplication.timeSinceStartup + 1.2d;
                EditorApplication.update -= CaptureWhenReady;
                EditorApplication.update += CaptureWhenReady;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.EraseBool(PendingKey);
            }
        }

        private static void CaptureWhenReady()
        {
            if (EditorApplication.timeSinceStartup < captureAfter) return;
            EditorApplication.update -= CaptureWhenReady;
            try
            {
                HockeyCharacterPresentation player = UnityEngine.Object.FindFirstObjectByType<HockeyCharacterPresentation>()
                    ?? throw new InvalidOperationException("Pose validation player is missing.");
                Camera camera = Camera.main ?? throw new InvalidOperationException("Pose validation camera is missing.");
                Animator animator = player.Animator;
                Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                Transform blade = player.Equipment.FindEquippedChild(HockeyEquipmentSlot.Stick, "BladeContact");
                string directory = AbsoluteEvidenceDirectory();
                Directory.CreateDirectory(directory);
                Vector3 bodyFocus = player.transform.TransformPoint(new Vector3(0f, 0.9f, 0.55f));
                Capture(camera, bodyFocus + new Vector3(0f, 0.15f, 3.5f), bodyFocus, 34f,
                    Path.Combine(directory, "front.png"));
                Capture(camera, bodyFocus + new Vector3(3.5f, 0.15f, 0f), bodyFocus, 34f,
                    Path.Combine(directory, "side.png"));
                Capture(camera, bodyFocus + new Vector3(0f, 0.15f, -3.5f), bodyFocus, 34f,
                    Path.Combine(directory, "rear.png"));
                Vector3 handFocus = (leftHand.position + rightHand.position) * 0.5f;
                Capture(camera, handFocus + new Vector3(0f, 0.06f, 1.2f), handFocus, 24f,
                    Path.Combine(directory, "hands-close-up.png"));
                Capture(camera, blade.position + new Vector3(0.8f, 0.32f, 1.15f), blade.position, 22f,
                    Path.Combine(directory, "blade-close-up.png"));
                WriteReport(player, directory);
                Debug.Log("TWO_HAND_HOCKEY_POSE_CAPTURES_PASS images=5");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                EditorApplication.ExitPlaymode();
            }
        }

        private static void Capture(Camera camera, Vector3 position, Vector3 focus, float fieldOfView, string path)
        {
            camera.transform.position = position;
            camera.transform.rotation = Quaternion.LookRotation(focus - position, Vector3.up);
            camera.fieldOfView = fieldOfView;
            RenderTexture target = RenderTexture.GetTemporary(1280, 960, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            Texture2D image = new(1280, 960, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0f, 0f, 1280f, 960f), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            camera.targetTexture = null;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
        }

        private static void WriteReport(HockeyCharacterPresentation player, string directory)
        {
            HockeyStickRig rig = player.GetComponent<HockeyStickRig>();
            Transform stick = player.Equipment.GetEquipped(HockeyEquipmentSlot.Stick).transform;
            Transform socket = stick.parent;
            Transform primary = player.Equipment.FindEquippedChild(HockeyEquipmentSlot.Stick, "PrimaryGrip");
            Transform secondary = player.Equipment.FindEquippedChild(HockeyEquipmentSlot.Stick, "SecondaryGrip");
            Transform blade = player.Equipment.FindEquippedChild(HockeyEquipmentSlot.Stick, "BladeContact");
            TwoBoneIKConstraintData left = rig.LeftHandConstraint.data;
            string report = $@"# Two-Hand Hockey Pose Validation

## Right-Handed Grip Convention

- Right hand: authoritative top hand through `RightHand/StickSocket/Hockey_Stick_Base_v1`.
- Left hand: lower hand driven by `LeftHandIKTarget`, synchronized from the equipped `SecondaryGrip` after right-hand rig evaluation.

## Exact Transforms

- `StickSocket` local position: {Format(socket.localPosition)}
- `StickSocket` local rotation: {Format(socket.localEulerAngles)} deg
- `StickSocket` local scale: {Format(socket.localScale)}
- `PrimaryGrip` local position: {Format(primary.localPosition)}
- `PrimaryGrip` local rotation: {Format(primary.localEulerAngles)} deg
- `SecondaryGrip` local position: {Format(secondary.localPosition)}
- `SecondaryGrip` local rotation: {Format(secondary.localEulerAngles)} deg
- `BladeContact` local position: {Format(blade.localPosition)}
- `BladeContact` local rotation: {Format(blade.localEulerAngles)} deg
- `BladeContact` world position in test pose: {Format(blade.position)}
- `LeftHandIKTarget` local position: {Format(rig.LeftHandTarget.localPosition)}
- `LeftHandIKTarget` local rotation: {Format(rig.LeftHandTarget.localEulerAngles)} deg
- `LeftElbowHint` local position: {Format(left.hint.localPosition)}
- `LeftElbowHint` local rotation: {Format(left.hint.localEulerAngles)} deg

## Left-Hand IK Settings

- Constraint: `TwoBoneIKConstraint`
- Root / mid / tip: `{left.root.name}` / `{left.mid.name}` / `{left.tip.name}`
- Position weight: {left.targetPositionWeight:F3}
- Rotation weight: {left.targetRotationWeight:F3}
- Hint weight: {left.hintWeight:F3}
- Maintain position offset: {left.maintainTargetPositionOffset}
- Maintain rotation offset: {left.maintainTargetRotationOffset}
- Equipped source grip: `{rig.EquippedSecondaryGrip.name}`

## Evidence

- [Front](front.png)
- [Side](side.png)
- [Rear](rear.png)
- [Both hands close-up](hands-close-up.png)
- [Blade close-up](blade-close-up.png)
- [Gameplay camera](gameplay-camera.png)
";
            File.WriteAllText(Path.Combine(directory, "validation.md"), report);
        }

        private static void CreateLight(string name, float intensity, Quaternion rotation)
        {
            GameObject lightObject = new(name, typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = rotation;
        }

        private static string AbsoluteEvidenceDirectory()
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, EvidenceDirectory);
        }

        private static string Format(Vector3 value) =>
            $"({value.x:F6}, {value.y:F6}, {value.z:F6})";
    }
}
#endif
