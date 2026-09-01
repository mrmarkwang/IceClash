/*
 * Additive validator/generator for the Blender-cleaned male base. This class
 * intentionally never writes or regenerates the canonical Male_Base_v1 assets.
 */

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IceClash.CharacterValidation.Editor
{
    public static class MaleBaseV11CleanValidationSetup
    {
        private const string RootDirectory = "Assets/Characters/Male/Male_Base_v1";
        private const string CanonicalModelPath = RootDirectory + "/Meshy_AI_Navy_Training_Pose_biped_Character_output.fbx";
        private const string CleanModelPath = RootDirectory + "/Male_Base_v1_1_Clean.fbx";
        private const string RunningModelPath = RootDirectory + "/Meshy_AI_Navy_Training_Pose_biped_Animation_Running_withSkin.fbx";
        private const string ControllerPath = RootDirectory + "/Male_Base_v1_1_Clean_Test.controller";
        private const string PrefabPath = RootDirectory + "/Male_Base_v1_1_Clean_Test.prefab";
        private const string ScenePath = RootDirectory + "/Male_Base_v1_1_Clean_Test.unity";
        private const string CharacterMaterialPath = RootDirectory + "/Male_Base_v1_Validation.mat";
        private const string GroundMaterialPath = RootDirectory + "/Male_Base_v1_Ground.mat";
        private const string EvidenceDirectory = ".docs/evidence/meshy-humanoid-cleanup/after";

        private static readonly HumanBodyBones[] RequiredBones =
        {
            HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest,
            HumanBodyBones.Neck, HumanBodyBones.Head,
            HumanBodyBones.LeftShoulder, HumanBodyBones.RightShoulder,
            HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm,
            HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm,
            HumanBodyBones.LeftHand, HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,
            HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg,
            HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot
        };

        [MenuItem("IceClash/Character Validation/Generate Male Base v1.1 Clean Test")]
        public static void Generate()
        {
            ConfigureCleanImporter();
            Avatar cleanAvatar = LoadValidAvatar(CleanModelPath);
            AnimationClip runningClip = SelectRunningClip(LoadAnimationClips(RunningModelPath));
            AnimatorController controller = CreateController(runningClip);
            CreatePrefab(cleanAvatar, controller);
            CreateScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MESHY_CLEAN_GENERATED");
        }

        [MenuItem("IceClash/Character Validation/Validate Male Base v1.1 Clean Test")]
        public static void Validate()
        {
            ModelImporter importer = RequireImporter(CleanModelPath);
            if (importer.animationType != ModelImporterAnimationType.Human
                || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                throw new InvalidOperationException("Clean FBX is not Humanoid/Create From This Model.");

            Avatar canonicalAvatar = LoadValidAvatar(CanonicalModelPath);
            Avatar cleanAvatar = LoadValidAvatar(CleanModelPath);
            Dictionary<HumanBodyBones, string> canonicalMap = ReadRequiredMap(CanonicalModelPath, canonicalAvatar);
            Dictionary<HumanBodyBones, string> cleanMap = ReadRequiredMap(CleanModelPath, cleanAvatar);
            foreach (HumanBodyBones bone in RequiredBones)
            {
                if (canonicalMap[bone] != cleanMap[bone])
                    throw new InvalidOperationException($"Humanoid mapping changed for {bone}: {canonicalMap[bone]} -> {cleanMap[bone]}");
                Debug.Log($"MESHY_CLEAN_BONE {bone}={cleanMap[bone]}");
            }

            CompareHierarchyAndTransforms();
            CompareBounds();

            AnimationClip runningClip = SelectRunningClip(LoadAnimationClips(RunningModelPath));
            if (!runningClip.humanMotion || runningClip.length <= 0f)
                throw new InvalidOperationException("The unchanged Running clip is not valid Humanoid motion.");
            ModelImporter runningImporter = RequireImporter(RunningModelPath);
            if (runningImporter.clipAnimations.Length == 0 || runningImporter.clipAnimations.Any(clip => !clip.loopTime))
                throw new InvalidOperationException("The unchanged Running clip is not configured to loop.");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new InvalidOperationException("Clean test prefab is missing.");
            Animator prefabAnimator = prefab.GetComponentInChildren<Animator>(true);
            if (prefabAnimator == null || prefabAnimator.avatar != cleanAvatar || prefabAnimator.runtimeAnimatorController == null
                || prefabAnimator.applyRootMotion)
                throw new InvalidOperationException("Clean test prefab Animator is not wired correctly.");
            if (prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                throw new InvalidOperationException("Clean test prefab must not contain scripts.");

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            AnimatorState defaultState = controller != null ? controller.layers[0].stateMachine.defaultState : null;
            if (defaultState == null || defaultState.name != "Running" || defaultState.motion != runningClip)
                throw new InvalidOperationException("Running is not the clean controller default state.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("Clean test scene is missing.");

            GameObject playback = UnityEngine.Object.Instantiate(prefab);
            try
            {
                AnimationMode.StartAnimationMode();
                for (int sample = 0; sample <= 18; sample++)
                {
                    float elapsed = runningClip.length * 2.25f * sample / 18f;
                    float wrappedTime = Mathf.Repeat(elapsed, runningClip.length);
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(playback, runningClip, wrappedTime);
                    AnimationMode.EndSampling();
                    Bounds sampledBounds = CalculateBounds(playback);
                    if (!float.IsFinite(sampledBounds.size.x + sampledBounds.size.y + sampledBounds.size.z))
                        throw new InvalidOperationException($"Running produced invalid renderer bounds at sample {sample}.");
                }
                AnimationMode.StopAnimationMode();
                Debug.Log($"MESHY_CLEAN_RUNNING clip={runningClip.name} length={runningClip.length:F3} loop=true sampledCycles=2.25");
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                UnityEngine.Object.DestroyImmediate(playback);
            }

            Debug.Log($"MESHY_CLEAN_AVATAR valid={cleanAvatar.isValid} human={cleanAvatar.isHuman}");
            Debug.Log($"MESHY_CLEAN_PREFAB path={PrefabPath} gameplayScripts=0");
            Debug.Log($"MESHY_CLEAN_SCENE path={ScenePath}");
            Debug.Log("MESHY_CLEAN_VALIDATION_PASS");
        }

        public static void GenerateValidateAndCaptureBatch()
        {
            Generate();
            Validate();
            CaptureEvidence();
        }

        [MenuItem("IceClash/Character Validation/Capture Male Base v1.1 Clean Evidence")]
        public static void CaptureEvidence()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject character = GameObject.Find("Male_Base_v1_1_Clean_Test");
            Camera camera = Camera.main;
            AnimationClip runningClip = SelectRunningClip(LoadAnimationClips(RunningModelPath));
            if (character == null || camera == null)
                throw new InvalidOperationException("Clean preview scene is missing its character or camera.");

            string evidence = Path.Combine(Directory.GetParent(Application.dataPath).FullName, EvidenceDirectory);
            Directory.CreateDirectory(evidence);
            float[] normalizedTimes = { 0.125f, 0.375f, 0.625f, 0.875f };
            (string Name, Vector3 Direction)[] views =
            {
                ("front", Vector3.forward), ("side", Vector3.left), ("rear", Vector3.back)
            };
            AnimationMode.StartAnimationMode();
            try
            {
                foreach (float normalizedTime in normalizedTimes)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(character, runningClip, runningClip.length * normalizedTime);
                    AnimationMode.EndSampling();
                    foreach ((string name, Vector3 direction) in views)
                    {
                        FrameCamera(camera, character, direction);
                        string fileName = $"running-{name}-{Mathf.RoundToInt(normalizedTime * 1000f):000}.png";
                        CaptureCamera(camera, Path.Combine(evidence, fileName));
                    }
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }
            Debug.Log($"MESHY_CLEAN_EVIDENCE path={EvidenceDirectory} images=12");
        }

        private static void ConfigureCleanImporter()
        {
            ModelImporter importer = RequireImporter(CleanModelPath);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.optimizeGameObjects = false;
            importer.SaveAndReimport();
            LoadValidAvatar(CleanModelPath);
        }

        private static AnimatorController CreateController(AnimationClip runningClip)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in machine.states.ToArray()) machine.RemoveState(child.state);
            AnimatorState running = machine.AddState("Running");
            running.motion = runningClip;
            machine.defaultState = running;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void CreatePrefab(Avatar avatar, RuntimeAnimatorController controller)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(CleanModelPath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(CharacterMaterialPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null) throw new InvalidOperationException("Clean FBX could not be instantiated.");
            try
            {
                instance.name = "Male_Base_v1_1_Clean_Test";
                Animator animator = instance.GetComponentInChildren<Animator>(true) ?? instance.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                if (material != null)
                {
                    foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                    {
                        Material[] materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++) materials[i] = material;
                        renderer.sharedMaterials = materials;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject character = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            character.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Bounds bounds = CalculateBounds(character);
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Preview Ground";
            ground.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.01f, bounds.center.z);
            ground.transform.localScale = Vector3.one * Mathf.Max(bounds.size.y, bounds.size.x) * 0.35f;
            ground.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            GameObject cameraObject = new GameObject("Preview Camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.065f);
            camera.nearClipPlane = 0.01f;
            camera.fieldOfView = 32f;
            FrameCamera(camera, character, Vector3.back);
            GameObject lightObject = new GameObject("Preview Key Light", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.8f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.24f, 0.26f, 0.3f);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static Dictionary<HumanBodyBones, string> ReadRequiredMap(string path, Avatar avatar)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject instance = UnityEngine.Object.Instantiate(model);
            try
            {
                Animator animator = instance.GetComponentInChildren<Animator>(true) ?? instance.AddComponent<Animator>();
                animator.avatar = avatar;
                return RequiredBones.ToDictionary(
                    bone => bone,
                    bone => animator.GetBoneTransform(bone)?.name
                        ?? throw new InvalidOperationException($"Required Humanoid bone is unmapped in {path}: {bone}"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void CompareHierarchyAndTransforms()
        {
            GameObject canonical = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CanonicalModelPath));
            GameObject clean = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CleanModelPath));
            try
            {
                Transform canonicalHips = canonical.GetComponentsInChildren<Transform>(true).First(transform => transform.name == "Hips");
                Transform cleanHips = clean.GetComponentsInChildren<Transform>(true).First(transform => transform.name == "Hips");
                Dictionary<string, Transform> canonicalTransforms = canonicalHips.GetComponentsInChildren<Transform>(true)
                    .ToDictionary(transform => RelativePath(canonicalHips, transform));
                Dictionary<string, Transform> cleanTransforms = cleanHips.GetComponentsInChildren<Transform>(true)
                    .ToDictionary(transform => RelativePath(cleanHips, transform));
                if (!canonicalTransforms.Keys.OrderBy(value => value).SequenceEqual(cleanTransforms.Keys.OrderBy(value => value)))
                    throw new InvalidOperationException("Clean Armature/bone hierarchy-name set differs from canonical v1.");
                foreach (string path in canonicalTransforms.Keys)
                {
                    Transform a = canonicalTransforms[path];
                    Transform b = cleanTransforms[path];
                    if (Vector3.Distance(a.localPosition, b.localPosition) > 0.0001f
                        || Quaternion.Angle(a.localRotation, b.localRotation) > 0.1f
                        || RelativeScaleDelta(a.localScale, b.localScale) > 0.001f)
                        throw new InvalidOperationException($"Clean transform exceeds tolerance at {path}.");
                }
                if (Vector3.Distance(canonical.transform.localPosition, clean.transform.localPosition) > 0.0001f
                    || Quaternion.Angle(canonical.transform.localRotation, clean.transform.localRotation) > 0.1f
                    || RelativeScaleDelta(canonical.transform.localScale, clean.transform.localScale) > 0.001f)
                    throw new InvalidOperationException("Clean FBX root transform exceeds tolerance.");
                Debug.Log($"MESHY_CLEAN_HIERARCHY bones={cleanTransforms.Count - 1} namesParents=exact transforms=withinTolerance root=withinTolerance");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canonical);
                UnityEngine.Object.DestroyImmediate(clean);
            }
        }

        private static void CompareBounds()
        {
            GameObject canonical = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CanonicalModelPath));
            GameObject clean = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CleanModelPath));
            try
            {
                Vector3 a = CalculateBounds(canonical).size;
                Vector3 b = CalculateBounds(clean).size;
                float delta = Mathf.Max(Mathf.Abs(a.x - b.x) / Mathf.Max(a.x, 0.0001f),
                    Mathf.Abs(a.y - b.y) / Mathf.Max(a.y, 0.0001f),
                    Mathf.Abs(a.z - b.z) / Mathf.Max(a.z, 0.0001f));
                if (delta > 0.01f) throw new InvalidOperationException($"Clean bounds changed by {delta:P2}.");
                Debug.Log($"MESHY_CLEAN_BOUNDS canonical={a:F6} clean={b:F6} maxDelta={delta:P4}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(canonical);
                UnityEngine.Object.DestroyImmediate(clean);
            }
        }

        private static string RelativePath(Transform root, Transform item)
        {
            if (item == root) return string.Empty;
            List<string> names = new List<string>();
            for (Transform current = item; current != root; current = current.parent) names.Add(current.name);
            names.Reverse();
            return string.Join("/", names);
        }

        private static float RelativeScaleDelta(Vector3 a, Vector3 b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x) / Mathf.Max(Mathf.Abs(a.x), 0.0001f),
                Mathf.Abs(a.y - b.y) / Mathf.Max(Mathf.Abs(a.y), 0.0001f),
                Mathf.Abs(a.z - b.z) / Mathf.Max(Mathf.Abs(a.z), 0.0001f));
        }

        private static void FrameCamera(Camera camera, GameObject character, Vector3 direction)
        {
            Animator animator = character.GetComponentInChildren<Animator>(true);
            Transform head = animator != null ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            Transform leftFoot = animator != null ? animator.GetBoneTransform(HumanBodyBones.LeftFoot) : null;
            Transform rightFoot = animator != null ? animator.GetBoneTransform(HumanBodyBones.RightFoot) : null;
            Bounds bounds = CalculateBounds(character);
            Vector3 feet = leftFoot != null && rightFoot != null ? (leftFoot.position + rightFoot.position) * 0.5f
                : new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            Vector3 top = head != null ? head.position + Vector3.up * Mathf.Max(0.12f, bounds.size.y * 0.05f) : bounds.max;
            float height = Mathf.Max(top.y - feet.y, 1f);
            Vector3 target = (top + feet) * 0.5f;
            camera.transform.position = target + direction.normalized * height * 2.5f;
            camera.transform.LookAt(target);
        }

        private static void CaptureCamera(Camera camera, string path)
        {
            RenderTexture renderTexture = new RenderTexture(1280, 720, 24);
            Texture2D image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            RenderTexture oldActive = RenderTexture.active;
            RenderTexture oldTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                image.ReadPixels(new Rect(0f, 0f, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(Vector3.up, new Vector3(1f, 2f, 1f));
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static ModelImporter RequireImporter(string path)
        {
            return AssetImporter.GetAtPath(path) as ModelImporter
                ?? throw new InvalidOperationException($"ModelImporter is missing for {path}");
        }

        private static Avatar LoadValidAvatar(string path)
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isHuman || !avatar.isValid)
                throw new InvalidOperationException($"Unity did not create a valid Humanoid Avatar for {path}");
            return avatar;
        }

        private static IReadOnlyList<AnimationClip> LoadAnimationClips(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .OrderBy(clip => clip.name, StringComparer.Ordinal).ToArray();
        }

        private static AnimationClip SelectRunningClip(IReadOnlyList<AnimationClip> clips)
        {
            if (clips.Count == 0) throw new InvalidOperationException("Running FBX contains no imported clip.");
            return clips.FirstOrDefault(clip => clip.name.IndexOf("run", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? clips.OrderByDescending(clip => clip.length).First();
        }
    }
}
#endif
