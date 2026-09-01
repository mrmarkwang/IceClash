/*
 * Meshy male base-character import, preview-asset generator, and validator.
 * Configures the canonical FBX as a Humanoid Avatar, retargets and loops the
 * supplied running clip, and creates an isolated gameplay-free prefab and scene.
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
    public static class MaleBaseV1ValidationSetup
    {
        private const string RootDirectory = "Assets/Characters/Male/Male_Base_v1";
        private const string BaseModelPath = RootDirectory + "/Meshy_AI_Navy_Training_Pose_biped_Character_output.fbx";
        private const string RunningModelPath = RootDirectory + "/Meshy_AI_Navy_Training_Pose_biped_Animation_Running_withSkin.fbx";
        private const string ControllerPath = RootDirectory + "/Male_Base_v1_Test.controller";
        private const string CharacterMaterialPath = RootDirectory + "/Male_Base_v1_Validation.mat";
        private const string GroundMaterialPath = RootDirectory + "/Male_Base_v1_Ground.mat";
        private const string PrefabPath = RootDirectory + "/Male_Base_v1_Test.prefab";
        private const string ScenePath = RootDirectory + "/Male_Base_v1_Test.unity";
        private const string EvidenceDirectory = ".docs/evidence/meshy-humanoid-validation";

        private static readonly HumanBodyBones[] RequiredBones =
        {
            HumanBodyBones.Hips,
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.Neck,
            HumanBodyBones.Head,
            HumanBodyBones.LeftShoulder,
            HumanBodyBones.RightShoulder,
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.RightUpperArm,
            HumanBodyBones.LeftLowerArm,
            HumanBodyBones.RightLowerArm,
            HumanBodyBones.LeftHand,
            HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.RightFoot
        };

        [MenuItem("IceClash/Character Validation/Generate Male Base v1 Test")]
        public static void Generate()
        {
            ConfigureBaseImporter();
            Avatar avatar = LoadValidAvatar(BaseModelPath);
            AnimationClip runningClip = ConfigureRunningImporter(avatar);
            AnimatorController controller = CreateController(runningClip);
            Material characterMaterial = GetOrCreateMaterial(CharacterMaterialPath, new Color(0.24f, 0.34f, 0.48f));
            Material groundMaterial = GetOrCreateMaterial(GroundMaterialPath, new Color(0.12f, 0.14f, 0.17f));
            CreatePrefab(avatar, controller, characterMaterial);
            CreateScene(groundMaterial);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MESHY_HUMANOID_GENERATED");
        }

        [MenuItem("IceClash/Character Validation/Select Male Base v1 FBX")]
        public static void SelectBaseModel()
        {
            UnityEngine.Object model = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(BaseModelPath);
            if (model == null) throw new InvalidOperationException("Base FBX model asset is missing.");
            Selection.activeObject = model;
            EditorGUIUtility.PingObject(model);
        }

        [MenuItem("IceClash/Character Validation/Validate Male Base v1 Test")]
        public static void Validate()
        {
            ModelImporter baseImporter = RequireImporter(BaseModelPath);
            if (baseImporter.animationType != ModelImporterAnimationType.Human
                || baseImporter.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                throw new InvalidOperationException("Base FBX is not configured as Humanoid/Create From This Model.");

            Avatar avatar = LoadValidAvatar(BaseModelPath);
            Debug.Log($"MESHY_HUMANOID_AVATAR valid={avatar.isValid} human={avatar.isHuman} name={avatar.name}");
            ValidateRequiredBones(avatar);

            IReadOnlyList<AnimationClip> baseClips = LoadAnimationClips(BaseModelPath);
            IReadOnlyList<AnimationClip> runningClips = LoadAnimationClips(RunningModelPath);
            LogClipNames("BASE", baseClips);
            LogClipNames("RUNNING", runningClips);
            if (runningClips.Count == 0)
                throw new InvalidOperationException("Running FBX contains no imported animation clip.");

            ModelImporter runningImporter = RequireImporter(RunningModelPath);
            ModelImporterClipAnimation[] clipSettings = runningImporter.clipAnimations;
            if (clipSettings.Length == 0 || clipSettings.Any(clip => !clip.loopTime))
                throw new InvalidOperationException("The imported Running clip is not configured to loop.");

            AnimationClip runningClip = SelectRunningClip(runningClips);
            if (!runningClip.humanMotion || runningClip.length <= 0f)
                throw new InvalidOperationException("The imported Running clip is not valid Humanoid motion.");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new InvalidOperationException("Male_Base_v1_Test prefab is missing.");
            Animator animator = prefab.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar != avatar || animator.runtimeAnimatorController == null
                || animator.applyRootMotion)
                throw new InvalidOperationException("Test prefab Animator is not wired to the generated Avatar/controller.");
            if (prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                throw new InvalidOperationException("Test prefab must not contain gameplay or validation MonoBehaviours.");

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            AnimatorState defaultState = controller != null ? controller.layers[0].stateMachine.defaultState : null;
            if (defaultState == null || defaultState.motion != runningClip || defaultState.name != "Running")
                throw new InvalidOperationException("Running is not the default state of the temporary controller.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("Male_Base_v1_Test scene is missing.");

            Debug.Log($"MESHY_HUMANOID_RUNNING clip={runningClip.name} length={runningClip.length:F3} loop=true humanMotion=true defaultState=true");
            Debug.Log($"MESHY_HUMANOID_PREFAB path={PrefabPath} gameplayScripts=0");
            Debug.Log($"MESHY_HUMANOID_SCENE path={ScenePath}");
            Debug.Log("MESHY_HUMANOID_VALIDATION_PASS");
        }

        public static void GenerateAndValidateBatch()
        {
            Generate();
            Validate();
        }

        [MenuItem("IceClash/Character Validation/Capture Male Base v1 Evidence")]
        public static void CaptureEvidence()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject character = GameObject.Find("Male_Base_v1_Test");
            Camera camera = Camera.main;
            AnimationClip runningClip = SelectRunningClip(LoadAnimationClips(RunningModelPath));
            if (character == null || camera == null)
                throw new InvalidOperationException("Preview scene is missing its validation character or Main Camera.");

            string absoluteEvidenceDirectory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, EvidenceDirectory);
            Directory.CreateDirectory(absoluteEvidenceDirectory);
            float[] normalizedTimes = { 0.125f, 0.375f, 0.625f, 0.875f };
            (string Name, Vector3 Direction)[] views =
            {
                ("front", Vector3.forward),
                ("side", Vector3.left),
                ("rear", Vector3.back)
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
                        CaptureCamera(camera, Path.Combine(absoluteEvidenceDirectory, fileName));
                    }
                }
            }
            finally
            {
                AnimationMode.StopAnimationMode();
            }

            Debug.Log($"MESHY_HUMANOID_EVIDENCE path={EvidenceDirectory} images={normalizedTimes.Length * views.Length}");
        }

        private static void ConfigureBaseImporter()
        {
            ModelImporter importer = RequireImporter(BaseModelPath);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.optimizeGameObjects = false;
            importer.SaveAndReimport();
            LoadValidAvatar(BaseModelPath);
        }

        private static AnimationClip ConfigureRunningImporter(Avatar sourceAvatar)
        {
            ModelImporter importer = RequireImporter(RunningModelPath);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = sourceAvatar;
            importer.importAnimation = true;
            importer.optimizeGameObjects = false;
            importer.SaveAndReimport();

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips.Length == 0)
                throw new InvalidOperationException("Running FBX exposes no default animation clips.");
            foreach (ModelImporterClipAnimation clip in clips) clip.loopTime = true;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            IReadOnlyList<AnimationClip> importedClips = LoadAnimationClips(RunningModelPath);
            if (importedClips.Count == 0)
                throw new InvalidOperationException("Running FBX exposes no imported animation clips after reimport.");
            return SelectRunningClip(importedClips);
        }

        private static AnimatorController CreateController(AnimationClip runningClip)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState childState in stateMachine.states.ToArray())
                stateMachine.RemoveState(childState.state);
            AnimatorState runningState = stateMachine.AddState("Running");
            runningState.motion = runningClip;
            stateMachine.defaultState = runningState;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void CreatePrefab(Avatar avatar, RuntimeAnimatorController controller, Material validationMaterial)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(BaseModelPath);
            if (model == null) throw new InvalidOperationException("Base FBX model asset is missing.");

            GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null) throw new InvalidOperationException("Base FBX could not be instantiated.");
            try
            {
                instance.name = "Male_Base_v1_Test";
                Animator animator = instance.GetComponentInChildren<Animator>(true);
                if (animator == null) animator = instance.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int i = 0; i < materials.Length; i++) materials[i] = validationMaterial;
                    renderer.sharedMaterials = materials;
                }
                PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static void CreateScene(Material groundMaterial)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject character = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (character == null) throw new InvalidOperationException("Test prefab could not be added to the preview scene.");
            character.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            Bounds bounds = CalculateBounds(character);
            float height = Mathf.Max(bounds.size.y, 1f);
            float width = Mathf.Max(bounds.size.x, 0.5f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Preview Ground";
            ground.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.01f, bounds.center.z);
            float groundScale = Mathf.Max(height, width) * 0.35f;
            ground.transform.localScale = new Vector3(groundScale, 1f, groundScale);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

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

        private static Material GetOrCreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("No lit shader is available for validation materials.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.2f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void FrameCamera(Camera camera, GameObject character, Vector3 direction)
        {
            Animator animator = character.GetComponentInChildren<Animator>(true);
            Transform head = animator != null ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
            Transform leftFoot = animator != null ? animator.GetBoneTransform(HumanBodyBones.LeftFoot) : null;
            Transform rightFoot = animator != null ? animator.GetBoneTransform(HumanBodyBones.RightFoot) : null;
            Bounds bounds = CalculateBounds(character);
            Vector3 feet = leftFoot != null && rightFoot != null
                ? (leftFoot.position + rightFoot.position) * 0.5f
                : new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            Vector3 top = head != null ? head.position + Vector3.up * Mathf.Max(0.12f, bounds.size.y * 0.05f) : bounds.max;
            float height = Mathf.Max(top.y - feet.y, 1f);
            Vector3 target = (top + feet) * 0.5f;
            float distance = height * 2.5f;
            camera.transform.position = target + direction.normalized * distance;
            camera.transform.LookAt(target);
        }

        private static void CaptureCamera(Camera camera, string path)
        {
            const int width = 1280;
            const int height = 720;
            RenderTexture renderTexture = new RenderTexture(width, height, 24);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static void ValidateRequiredBones(Avatar avatar)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(BaseModelPath);
            GameObject instance = UnityEngine.Object.Instantiate(model);
            try
            {
                Animator animator = instance.GetComponentInChildren<Animator>(true);
                if (animator == null) animator = instance.AddComponent<Animator>();
                animator.avatar = avatar;
                foreach (HumanBodyBones bone in RequiredBones)
                {
                    Transform mapped = animator.GetBoneTransform(bone);
                    if (mapped == null) throw new InvalidOperationException($"Required Humanoid bone is unmapped: {bone}");
                    Debug.Log($"MESHY_HUMANOID_BONE {bone}={mapped.name}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
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
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new InvalidOperationException($"ModelImporter is missing for {path}");
            return importer;
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
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .OrderBy(clip => clip.name, StringComparer.Ordinal)
                .ToArray();
        }

        private static AnimationClip SelectRunningClip(IReadOnlyList<AnimationClip> clips)
        {
            return clips.FirstOrDefault(clip => clip.name.IndexOf("run", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? clips.OrderByDescending(clip => clip.length).First();
        }

        private static void LogClipNames(string source, IReadOnlyList<AnimationClip> clips)
        {
            string names = clips.Count == 0 ? "<none>" : string.Join(" | ", clips.Select(clip => clip.name));
            Debug.Log($"MESHY_HUMANOID_CLIPS source={source} names={names}");
        }
    }
}
#endif
