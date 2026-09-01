/*
 * IceClash production integration for the validated clean male Humanoid.
 * Generates an isolated visual prefab and Idle/temporary Running controller,
 * validates gameplay-root/collider/Avatar/attachment contracts, and captures
 * deterministic prototype-arena evidence without modifying validation assets.
 */

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using IceClash.Hockey.Character;
using IceClash.Player;
using IceClash.Tests.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IceClash.CharacterValidation.Editor
{
    [InitializeOnLoad]
    public static class MaleBaseV11GameplayIntegrationSetup
    {
        public const string CleanModelPath = "Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean.fbx";
        public const string RunningModelPath = "Assets/Characters/Male/Male_Base_v1/Meshy_AI_Navy_Training_Pose_biped_Animation_Running_withSkin.fbx";
        public const string RunningClipName = "Armature|Armature|running|baselayer";
        public const string ProductionDirectory = "Assets/Characters/Male/Male_Base_v1_1";
        public const string AnimationDirectory = ProductionDirectory + "/Animation";
        public const string VisualPrefabPath = ProductionDirectory + "/Male_Base_v1_1_Clean_Visual.prefab";
        public const string IdleClipPath = AnimationDirectory + "/MaleSkater_Idle.anim";
        public const string ControllerPath = AnimationDirectory + "/MaleSkater.controller";
        public const string GameplayPrefabPath = "Assets/_Project/Prefabs/HockeyPlayer.prefab";
        public const string ResourcePrefabPath = "Assets/_Project/Prefabs/Resources/Skater.prefab";
        public const string EvidenceDirectory = ".docs/evidence/integrate-clean-humanoid-player";

        private const string CapturePendingKey = "IceClash.CleanPlayerCapturePending";
        private const float FootTolerance = 0.03f;
        private const float PrototypeIceY = 0.2f;
        private const float GameplaySkaterSpawnY = 1f;
        private const float GameplaySkaterScale = 0.68f;
        private const float ProductionVisualScale = 1.65f;
        private static int captureExitCode;
        private static double captureAfter;

        private static readonly (string Name, AnimatorControllerParameterType Type)[] RequiredParameters =
        {
            ("Speed", AnimatorControllerParameterType.Float),
            ("ForwardAmount", AnimatorControllerParameterType.Float),
            ("TurnAmount", AnimatorControllerParameterType.Float),
            ("IsMoving", AnimatorControllerParameterType.Bool),
            ("IsBackward", AnimatorControllerParameterType.Bool),
            ("IsBraking", AnimatorControllerParameterType.Bool),
            ("IsSprinting", AnimatorControllerParameterType.Bool),
            ("CrossoverDirection", AnimatorControllerParameterType.Float)
        };

        private static readonly Dictionary<string, string> ImmutableCleanHashes = new()
        {
            { CleanModelPath, "a5682777ea0f7b14236fb96a5c3f19826b79113997bb929772b2d92cc892a159" },
            { CleanModelPath + ".meta", "602cef15f63e2edfad14ee120e8c302ffa38cd729058d95216ec1d2d07cd1d3f" },
            { "Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean_Test.prefab", "ea32b20a09953006094f4fb7332e4e9fcf205e94530f3d18e4566c6d95fafc17" },
            { "Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean_Test.controller", "bb0d50d15882fc55847564eace37dca2d7e758f1be08fa817e085cfe5f5da58d" },
            { "Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean_Test.unity", "eb42ad7c6fa3e18f7a8c6b1416d715811c495992cf34ba6c1197fcd5433528e6" }
        };

        static MaleBaseV11GameplayIntegrationSetup()
        {
            EditorApplication.playModeStateChanged -= OnCapturePlayModeChanged;
            EditorApplication.playModeStateChanged += OnCapturePlayModeChanged;
        }

        [MenuItem("IceClash/Character Validation/Generate Clean Gameplay Player")]
        public static void Generate()
        {
            GenerateProductionAssets();
            HockeyCharacterAssetSetup.GenerateAll();
            Validate();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CLEAN_PLAYER_INTEGRATION_GENERATED");
        }

        [MenuItem("IceClash/Character Validation/Open Gameplay Player Evidence")]
        public static void OpenGameplayPlayerEvidence()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayPrefabPath)
                ?? throw new InvalidOperationException("Gameplay HockeyPlayer prefab is missing.");
            AssetDatabase.OpenAsset(prefab);
            EditorApplication.delayCall += () =>
            {
                PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
                Animator animator = stage?.prefabContentsRoot?.GetComponentInChildren<Animator>(true);
                if (animator == null) return;
                Selection.activeGameObject = animator.gameObject;
                EditorGUIUtility.PingObject(animator.gameObject);
            };
        }

        public static void GenerateProductionAssets()
        {
            ValidateImmutableCleanAssets();
            ValidateCleanImporter();
            EnsureFolder(ProductionDirectory);
            EnsureFolder(AnimationDirectory);
            Avatar avatar = LoadCleanAvatar();
            AnimationClip running = LoadExactRunningClip();
            AnimationClip idle = CreateIdleClip();
            AnimatorController controller = CreateController(idle, running);
            CreateVisualPrefab(avatar, controller);
            AssetDatabase.SaveAssets();
        }

        public static void GenerateAndValidateBatch()
        {
            try
            {
                Generate();
                Debug.Log("CLEAN_PLAYER_INTEGRATION_ASSETS_PASS avatarValid=true avatarHuman=true states=Idle,Running");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [MenuItem("IceClash/Character Validation/Validate Clean Gameplay Player")]
        public static void Validate()
        {
            ValidateImmutableCleanAssets();
            ValidateCleanImporter();
            Avatar avatar = LoadCleanAvatar();
            AnimationClip running = LoadExactRunningClip();
            ValidateController(running);
            ValidateVisualPrefab(avatar);
            ValidateGameplayPrefabs(avatar);
            Debug.Log("CLEAN_PLAYER_INTEGRATION_VALID");
        }

        public static float AlignVisualToGameplayIce(GameObject gameplayRoot, Transform visualRoot, Animator animator)
        {
            if (gameplayRoot.GetComponent<CharacterController>() == null)
                throw new InvalidOperationException("Gameplay root is missing CharacterController.");
            (float left, float right) = CalculateFootContacts(gameplayRoot.transform, visualRoot, animator);
            float target = (PrototypeIceY - GameplaySkaterSpawnY) / GameplaySkaterScale;
            float offset = target - (left + right) * 0.5f;
            visualRoot.localPosition = new Vector3(0f, offset, 0f);
            (left, right) = CalculateFootContacts(gameplayRoot.transform, visualRoot, animator);
            if (Mathf.Abs(left - target) > FootTolerance || Mathf.Abs(right - target) > FootTolerance)
                throw new InvalidOperationException(
                    $"Clean feet cannot align to runtime ice contact with a Y-only offset: left={left:F4}, right={right:F4}, target={target:F4}.");
            return offset;
        }

        public static void CaptureGameplayEvidenceBatch()
        {
            string scenePath = "Assets/_Project/Scenes/PrototypeArena.unity";
            SessionState.SetBool(CapturePendingKey, true);
            EditorSceneManager.OpenScene(scenePath);
            EditorApplication.EnterPlaymode();
        }

        private static void OnCapturePlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(CapturePendingKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                captureAfter = EditorApplication.timeSinceStartup + 1.2d;
                EditorApplication.update -= CaptureWhenReady;
                EditorApplication.update += CaptureWhenReady;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.EraseBool(CapturePendingKey);
                if (Application.isBatchMode) EditorApplication.Exit(captureExitCode);
            }
        }

        private static void CaptureWhenReady()
        {
            if (EditorApplication.timeSinceStartup < captureAfter) return;
            EditorApplication.update -= CaptureWhenReady;
            captureExitCode = 0;
            try
            {
                PlayerController player = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None)
                    .FirstOrDefault(item => item.InputSource is IceClash.Input.PlayerInputController)
                    ?? throw new InvalidOperationException("Runtime controlled player was not built for capture.");
                Camera camera = Camera.main ?? throw new InvalidOperationException("Prototype camera is missing.");
                string directory = AbsoluteEvidenceDirectory();
                Directory.CreateDirectory(directory);
                CaptureGameplayView(camera, player.gameObject, Vector3.back, "front-gameplay.png");
                CaptureGameplayView(camera, player.gameObject, Vector3.left, "side-gameplay.png");
                typeof(PlayerMovementController).GetMethod("SetPlanarVelocityForValidation",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.Invoke(player.Movement, new object[] { (Vector3.forward + Vector3.right).normalized * 4f });
                player.GetComponent<HockeyCharacterPresentation>()?.SendMessage("Update");
                player.GetComponentInChildren<Animator>(true)?.Update(0.2f);
                CaptureGameplayView(camera, player.gameObject, new Vector3(-1f, 0.35f, -1f), "moving-turning-gameplay.png");
                string[] names = { "front-gameplay.png", "side-gameplay.png", "moving-turning-gameplay.png" };
                if (names.Any(name => !File.Exists(Path.Combine(directory, name))))
                    throw new IOException("One or more gameplay evidence images were not written.");
                Debug.Log("CLEAN_PLAYER_GAMEPLAY_CAPTURES_PASS images=3");
            }
            catch (Exception exception)
            {
                captureExitCode = 1;
                Debug.LogException(exception);
            }
            finally
            {
                EditorApplication.ExitPlaymode();
            }
        }

        private static AnimationClip CreateIdleClip()
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, IdleClipPath);
            }
            foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                AnimationUtility.SetEditorCurve(clip, binding, null);
            clip.name = "MaleSkater_Idle";
            clip.frameRate = 30f;
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), "Spine Front-Back"),
                new AnimationCurve(new Keyframe(0f, -0.02f), new Keyframe(0.5f, 0.02f), new Keyframe(1f, -0.02f)));
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static AnimatorController CreateController(AnimationClip idle, AnimationClip running)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = RequiredParameters.Select(parameter => new AnimatorControllerParameter
            {
                name = parameter.Name,
                type = parameter.Type
            }).ToArray();
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in machine.states.ToArray()) machine.RemoveState(child.state);
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions.ToArray())
                machine.RemoveAnyStateTransition(transition);
            AnimatorState idleState = machine.AddState("Idle");
            AnimatorState runningState = machine.AddState("Running");
            idleState.motion = idle;
            runningState.motion = running;
            machine.defaultState = idleState;
            AnimatorStateTransition toRunning = idleState.AddTransition(runningState);
            toRunning.hasExitTime = false;
            toRunning.duration = 0.1f;
            toRunning.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            AnimatorStateTransition toIdle = runningState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.1f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void CreateVisualPrefab(Avatar avatar, RuntimeAnimatorController controller)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(CleanModelPath)
                ?? throw new FileNotFoundException("Clean model is missing.", CleanModelPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject
                ?? throw new InvalidOperationException("Clean model could not be instantiated.");
            try
            {
                instance.name = "Male_Base_v1_1_Clean_Visual";
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                instance.transform.localScale = Vector3.one * ProductionVisualScale;
                Animator animator = instance.GetComponentInChildren<Animator>(true) ?? instance.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
                Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                if (head == null || leftFoot == null || rightFoot == null
                    || head.position.y <= (leftFoot.position.y + rightFoot.position.y) * 0.5f)
                    throw new InvalidOperationException("Clean production visual is not upright (Head must be above both Feet).");
                Material material = AssetDatabase.LoadAssetAtPath<Material>(
                    "Assets/Characters/Male/Male_Base_v1/Male_Base_v1_Validation.mat");
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    if (material != null)
                    {
                        Material[] materials = renderer.sharedMaterials;
                        for (int index = 0; index < materials.Length; index++) materials[index] = material;
                        renderer.sharedMaterials = materials;
                    }
                    renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                    renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                    renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    renderer.receiveShadows = false;
                }
                if (instance.GetComponentsInChildren<Collider>(true).Length > 0
                    || instance.GetComponentsInChildren<Rigidbody>(true).Length > 0)
                    throw new InvalidOperationException("Clean production visual contains physics components.");
                PrefabUtility.SaveAsPrefabAsset(instance, VisualPrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static void ValidateCleanImporter()
        {
            ModelImporter importer = AssetImporter.GetAtPath(CleanModelPath) as ModelImporter
                ?? throw new InvalidOperationException("Clean ModelImporter is missing.");
            if (importer.animationType != ModelImporterAnimationType.Human
                || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                throw new InvalidOperationException("Clean FBX must remain Humanoid/Create From This Model.");
        }

        private static Avatar LoadCleanAvatar()
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(CleanModelPath).OfType<Avatar>().SingleOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("Clean FBX Avatar is not valid and human.");
            return avatar;
        }

        private static AnimationClip LoadExactRunningClip()
        {
            AnimationClip[] matches = AssetDatabase.LoadAllAssetsAtPath(RunningModelPath).OfType<AnimationClip>()
                .Where(clip => clip.name == RunningClipName).ToArray();
            if (matches.Length != 1) throw new InvalidOperationException(
                $"Expected exactly one Running clip named '{RunningClipName}', found {matches.Length}.");
            AnimationClip clip = matches[0];
            if (!clip.humanMotion || clip.length <= 0f)
                throw new InvalidOperationException("Exact Running clip is not valid Humanoid motion.");
            ModelImporter importer = AssetImporter.GetAtPath(RunningModelPath) as ModelImporter;
            if (importer == null || importer.clipAnimations.Length != 1 || !importer.clipAnimations[0].loopTime)
                throw new InvalidOperationException("Exact Running clip importer must remain looped.");
            return clip;
        }

        private static void ValidateController(AnimationClip running)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath)
                ?? throw new InvalidOperationException("MaleSkater controller is missing.");
            if (controller.parameters.Length != RequiredParameters.Length)
                throw new InvalidOperationException("MaleSkater controller parameter count changed.");
            foreach ((string name, AnimatorControllerParameterType type) in RequiredParameters)
                if (controller.parameters.Count(parameter => parameter.name == name && parameter.type == type) != 1)
                    throw new InvalidOperationException($"MaleSkater controller parameter is missing or invalid: {name}.");
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            Dictionary<string, AnimatorState> states = machine.states.ToDictionary(child => child.state.name, child => child.state);
            if (states.Count != 2 || !states.ContainsKey("Idle") || !states.ContainsKey("Running")
                || machine.defaultState != states["Idle"] || states["Running"].motion != running)
                throw new InvalidOperationException("MaleSkater must contain only default Idle and exact temporary Running.");
            ValidateTransition(states["Idle"], "Running", AnimatorConditionMode.If);
            ValidateTransition(states["Running"], "Idle", AnimatorConditionMode.IfNot);
        }

        private static void ValidateTransition(AnimatorState source, string destination, AnimatorConditionMode mode)
        {
            AnimatorStateTransition[] matches = source.transitions.Where(transition => transition.destinationState != null
                && transition.destinationState.name == destination).ToArray();
            if (matches.Length != 1 || matches[0].hasExitTime || !Mathf.Approximately(matches[0].duration, 0.1f)
                || matches[0].conditions.Length != 1 || matches[0].conditions[0].parameter != "IsMoving"
                || matches[0].conditions[0].mode != mode)
                throw new InvalidOperationException($"Animator transition to {destination} is invalid.");
        }

        private static void ValidateVisualPrefab(Avatar avatar)
        {
            GameObject visual = AssetDatabase.LoadAssetAtPath<GameObject>(VisualPrefabPath)
                ?? throw new InvalidOperationException("Production clean visual prefab is missing.");
            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.avatar != avatar || animator.runtimeAnimatorController == null
                || animator.applyRootMotion || visual.transform.localPosition != Vector3.zero
                || visual.transform.localRotation != Quaternion.identity
                || visual.transform.localScale != Vector3.one * ProductionVisualScale)
                throw new InvalidOperationException("Production visual Animator or transform is invalid.");
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (head == null || leftFoot == null || rightFoot == null
                || head.position.y <= Mathf.Max(leftFoot.position.y, rightFoot.position.y))
                throw new InvalidOperationException("Production visual is not upright.");
            if (visual.GetComponentsInChildren<Collider>(true).Length > 0
                || visual.GetComponentsInChildren<Rigidbody>(true).Length > 0)
                throw new InvalidOperationException("Production visual contains prohibited physics.");
        }

        private static void ValidateGameplayPrefabs(Avatar avatar)
        {
            GameObject canonical = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayPrefabPath)
                ?? throw new InvalidOperationException("Gameplay HockeyPlayer prefab is missing.");
            GameObject resource = AssetDatabase.LoadAssetAtPath<GameObject>(ResourcePrefabPath)
                ?? throw new InvalidOperationException("Resources/Skater prefab is missing.");
            if (PrefabUtility.GetPrefabAssetType(resource) != PrefabAssetType.Variant
                || PrefabUtility.GetCorrespondingObjectFromOriginalSource(resource) != canonical)
                throw new InvalidOperationException("Resources/Skater is no longer the HockeyPlayer variant.");
            CharacterController controller = canonical.GetComponent<CharacterController>();
            string[] expectedRootComponents =
            {
                "UnityEngine.Transform",
                "UnityEngine.CharacterController",
                "IceClash.Hockey.Character.HockeyStickRig",
                "IceClash.Hockey.Character.HockeyEquipmentLoadout",
                "IceClash.Hockey.Character.HockeyCharacterPresentation"
            };
            string[] actualRootComponents = canonical.GetComponents<Component>()
                .Select(component => component.GetType().FullName).ToArray();
            if (!actualRootComponents.SequenceEqual(expectedRootComponents))
                throw new InvalidOperationException("Gameplay root component order/composition changed: "
                    + string.Join(",", actualRootComponents));
            if (controller == null || !controller.enabled || controller.center != Vector3.zero
                || !Mathf.Approximately(controller.height, 2f) || !Mathf.Approximately(controller.radius, 0.45f)
                || !Mathf.Approximately(controller.slopeLimit, 45f) || !Mathf.Approximately(controller.stepOffset, 0.3f)
                || !Mathf.Approximately(controller.skinWidth, 0.08f) || !Mathf.Approximately(controller.minMoveDistance, 0.001f))
                throw new InvalidOperationException("Gameplay CharacterController settings changed.");
            Transform visual = canonical.transform.Find("Visual");
            if (canonical.transform.Find("HumanoidVisual") != null)
                throw new InvalidOperationException("The previous HumanoidVisual placeholder is still present.");
            Transform clean = visual != null ? visual.Find("Male_Base_v1_1_Clean_Visual") : null;
            Animator animator = clean != null ? clean.GetComponentInChildren<Animator>(true) : null;
            if (visual == null || clean == null || animator == null || animator.avatar != avatar || animator.applyRootMotion
                || visual.localRotation != Quaternion.identity || visual.localScale != Vector3.one)
                throw new InvalidOperationException("Gameplay clean Visual hierarchy is invalid.");
            if (visual.GetComponentsInChildren<Collider>(true).Length > 0
                || visual.GetComponentsInChildren<Rigidbody>(true).Length > 0)
                throw new InvalidOperationException("Gameplay Visual contains prohibited physics.");
            GameObject evaluated = PrefabUtility.InstantiatePrefab(canonical) as GameObject
                ?? throw new InvalidOperationException("Gameplay prefab could not be instantiated for Idle-pose validation.");
            try
            {
                Transform evaluatedVisual = evaluated.transform.Find("Visual");
                Animator evaluatedAnimator = evaluatedVisual?.GetComponentInChildren<Animator>(true);
                if (evaluatedVisual == null || evaluatedAnimator == null)
                    throw new InvalidOperationException("Evaluated gameplay prefab is missing its clean visual Animator.");
                evaluatedAnimator.Rebind();
                evaluatedAnimator.Play("Idle", 0, 0f);
                evaluatedAnimator.Update(0f);
                (float left, float right) = CalculateFootContacts(
                    evaluated.transform, evaluatedVisual, evaluatedAnimator);
                float target = (PrototypeIceY - GameplaySkaterSpawnY) / GameplaySkaterScale;
                if (Mathf.Abs(left - target) > FootTolerance || Mathf.Abs(right - target) > FootTolerance)
                    throw new InvalidOperationException($"Gameplay feet are misaligned: left={left:F4}, right={right:F4}.");
            }
            finally { UnityEngine.Object.DestroyImmediate(evaluated); }
            foreach (HumanBodyBones bone in new[] { HumanBodyBones.Head, HumanBodyBones.LeftHand,
                         HumanBodyBones.RightHand, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot })
            {
                Transform transform = animator.GetBoneTransform(bone)
                    ?? throw new InvalidOperationException($"Production Avatar is missing {bone}.");
                Debug.Log($"CLEAN_PLAYER_BONE {bone}={AnimationUtility.CalculateTransformPath(transform, canonical.transform)}");
            }
            Debug.Log($"CLEAN_PLAYER_VISUAL_TRANSFORM position={visual.localPosition:F6} rotation={visual.localEulerAngles:F3} scale={visual.localScale:F3}");
        }

        private static (float Left, float Right) CalculateFootContacts(Transform gameplayRoot,
            Transform visualRoot, Animator animator)
        {
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (hips == null || leftFoot == null || rightFoot == null)
                throw new InvalidOperationException("Foot-contact calculation requires Hips and both Feet.");
            Vector3 hipsLocal = gameplayRoot.InverseTransformPoint(hips.position);
            Vector3 leftLocal = gameplayRoot.InverseTransformPoint(leftFoot.position);
            Vector3 rightLocal = gameplayRoot.InverseTransformPoint(rightFoot.position);
            float ceiling = Mathf.Max(leftLocal.y, rightLocal.y) + 0.25f;
            float left = float.PositiveInfinity;
            float right = float.PositiveInfinity;
            foreach (SkinnedMeshRenderer renderer in visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh baked = new Mesh();
                try
                {
                    renderer.BakeMesh(baked);
                    // BakeMesh already includes the renderer's lossy scale. Reapplying TransformPoint
                    // would square the calibrated production scale and place the visible feet too high.
                    Matrix4x4 bakedToWorld = Matrix4x4.TRS(
                        renderer.transform.position, renderer.transform.rotation, Vector3.one);
                    foreach (Vector3 vertex in baked.vertices)
                    {
                        Vector3 local = gameplayRoot.InverseTransformPoint(bakedToWorld.MultiplyPoint3x4(vertex));
                        if (local.y > ceiling) continue;
                        float leftDistance = Mathf.Abs(local.x - leftLocal.x) + Mathf.Abs(local.z - leftLocal.z) * 0.25f;
                        float rightDistance = Mathf.Abs(local.x - rightLocal.x) + Mathf.Abs(local.z - rightLocal.z) * 0.25f;
                        if (leftDistance <= rightDistance) left = Mathf.Min(left, local.y);
                        else right = Mathf.Min(right, local.y);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(baked); }
            }
            if (!float.IsFinite(left) || !float.IsFinite(right))
                throw new InvalidOperationException("Unable to resolve both baked foot-contact regions.");
            return (left, right);
        }

        private static void ValidateImmutableCleanAssets()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Unity project root is unavailable.");
            foreach (KeyValuePair<string, string> pair in ImmutableCleanHashes)
            {
                string absolute = Path.Combine(projectRoot, pair.Key);
                if (!File.Exists(absolute)) throw new FileNotFoundException("Immutable clean asset is missing.", pair.Key);
                using SHA256 algorithm = SHA256.Create();
                using FileStream stream = File.OpenRead(absolute);
                string actual = string.Concat(algorithm.ComputeHash(stream).Select(value => value.ToString("x2")));
                if (actual != pair.Value)
                    throw new InvalidOperationException($"Immutable clean asset changed: {pair.Key} ({actual}).");
            }
        }

        private static void CaptureGameplayView(Camera camera, GameObject character, Vector3 direction, string fileName)
        {
            Bounds bounds = CalculateBounds(character);
            float height = Mathf.Max(bounds.size.y, 1f);
            Vector3 target = bounds.center;
            camera.transform.position = target + direction.normalized * height * 2.6f + Vector3.up * height * 0.12f;
            camera.transform.LookAt(target);
            RenderTexture renderTexture = new(1280, 720, 24);
            Texture2D image = new(1280, 720, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                image.ReadPixels(new Rect(0f, 0f, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(AbsoluteEvidenceDirectory(), fileName), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Character has no renderers.");
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++) bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }

        private static string AbsoluteEvidenceDirectory()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Unity project root is unavailable.");
            return Path.Combine(projectRoot, EvidenceDirectory);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }
    }
}
#endif
