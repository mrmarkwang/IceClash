/*
 * IceClash modular humanoid asset generator and validator.
 * Configures the selected FBX as Humanoid, applies the mobile texture policy,
 * builds all eight hockey gear pieces, shared presentation assets, prefabs, IK,
 * and the ten-player scene.
 */

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IceClash.Hockey.Character;
using IceClash.Player;
using IceClash.Puck;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Rendering;

namespace IceClash.Tests.Editor
{
    [InitializeOnLoad]
    public static class HockeyCharacterAssetSetup
    {
        private const string ModelPath = "Assets/_Project/Art/Characters/RealisticHumanMale/unity.Fbx";
        private const string CharacterDirectory = "Assets/_Project/Art/Characters/RealisticHumanMale";
        private const string GeneratedDirectory = "Assets/_Project/Art/HockeyPrototype";
        private const string PrefabPath = "Assets/_Project/Prefabs/HockeyPlayer.prefab";
        private const string ResourcePrefabPath = "Assets/_Project/Prefabs/Resources/Skater.prefab";
        private const string ScenePath = "Assets/_Project/Scenes/ModularCharacterTest.unity";
        private const string ControllerPath = GeneratedDirectory + "/HockeyPlayer.controller";
        private const string AutoGenerationKey = "IceClash.ModularCharacterGenerationRunning";
        private const int ExpectedEquipmentSlotCount = 8;

        static HockeyCharacterAssetSetup()
        {
            EditorApplication.delayCall += EnsureGenerated;
        }

        [MenuItem("IceClash/Generate Modular Hockey Character")]
        public static void GenerateAll()
        {
            EnsureFolder(GeneratedDirectory);
            ConfigureHumanoidImporter();
            ConfigureMobileTextures();
            Material skinMaterial = CreateOrUpdateMaterial("CharacterMobile", new Color(0.72f, 0.52f, 0.4f));
            Material equipmentMaterial = CreateOrUpdateMaterial("EquipmentDark", new Color(0.035f, 0.055f, 0.085f));
            Material jerseyMaterial = CreateOrUpdateMaterial("JerseyNeutral", new Color(0.22f, 0.42f, 0.78f));
            GameObject player = BuildPlayer(skinMaterial, equipmentMaterial, jerseyMaterial);
            try
            {
                CreateAnimationAssets(player);
                Animator animator = player.GetComponentInChildren<Animator>();
                animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
                PrefabUtility.SaveAsPrefabAsset(player, PrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(player);
            }

            CreateResourceVariant();
            CreateTestScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("MODULAR_CHARACTER_GENERATED");
        }

        public static void GenerateAndValidateBatch()
        {
            try
            {
                GenerateAll();
                string[] stablePaths =
                {
                    GeneratedDirectory + "/CharacterMobile.mat",
                    GeneratedDirectory + "/EquipmentDark.mat",
                    GeneratedDirectory + "/JerseyNeutral.mat",
                    GeneratedDirectory + "/Idle.anim",
                    GeneratedDirectory + "/Skate.anim",
                    GeneratedDirectory + "/Shoot.anim",
                    ControllerPath
                };
                string[] stableGuids = stablePaths.Select(AssetDatabase.AssetPathToGUID).ToArray();
                GenerateAll();
                for (int i = 0; i < stablePaths.Length; i++)
                    if (string.IsNullOrEmpty(stableGuids[i]) || AssetDatabase.AssetPathToGUID(stablePaths[i]) != stableGuids[i])
                        throw new InvalidOperationException("Generator changed the stable asset GUID for " + stablePaths[i]);
                ValidateGeneratedAssets();
                Debug.Log("MODULAR_CHARACTER_ASSETS_PASS");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode) EditorApplication.Exit(1);
                throw;
            }
        }

        [MenuItem("IceClash/Validate Modular Hockey Character Assets")]
        public static void ValidateGeneratedAssets()
        {
            Avatar avatar = LoadHumanoidAvatar();
            if (avatar == null || !avatar.isHuman || !avatar.isValid)
                throw new InvalidOperationException("Selected character does not have a valid Humanoid avatar.");

            GameObject canonical = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject resource = AssetDatabase.LoadAssetAtPath<GameObject>(ResourcePrefabPath);
            if (canonical == null || resource == null) throw new InvalidOperationException("Hockey player prefabs are missing.");
            if (PrefabUtility.GetPrefabAssetType(resource) != PrefabAssetType.Variant)
                throw new InvalidOperationException("Resources/Skater.prefab is not a connected prefab variant.");
            GameObject source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(resource);
            if (source != canonical) throw new InvalidOperationException("Skater variant is not based on HockeyPlayer.prefab.");

            Animator animator = canonical.GetComponentInChildren<Animator>();
            HockeyEquipmentLoadout loadout = canonical.GetComponent<HockeyEquipmentLoadout>();
            HockeyStickRig stickRig = canonical.GetComponent<HockeyStickRig>();
            RigBuilder rigBuilder = canonical.GetComponentInChildren<RigBuilder>();
            if (animator == null || animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid
                || animator.applyRootMotion || animator.runtimeAnimatorController == null)
                throw new InvalidOperationException("HockeyPlayer Animator is invalid or root motion is enabled.");
            if (loadout == null || !loadout.IsComplete() || loadout.SlotCount != ExpectedEquipmentSlotCount)
                throw new InvalidOperationException("HockeyPlayer does not contain all eight equipment slots.");
            if (!HasActiveEquipment(loadout))
                throw new InvalidOperationException("HockeyPlayer does not have one active item in every equipment slot.");
            if (stickRig == null || !stickRig.HasValidReferences || rigBuilder == null || rigBuilder.layers.Count != 1)
                throw new InvalidOperationException("HockeyPlayer two-hand IK rig is incomplete.");
            if (stickRig.LeftHandConstraint.transform == stickRig.RightHandConstraint.transform
                || stickRig.LeftHandTarget == stickRig.RightHandTarget)
                throw new InvalidOperationException("Left and right hand IK constraints are not independent.");
            if (DistanceToSegment(stickRig.LeftHandTarget.localPosition, stickRig.RightHandTarget.localPosition,
                    stickRig.ShaftEndReference.localPosition) > 0.03f)
                throw new InvalidOperationException("Left-hand IK target is not aligned to the replaceable stick shaft.");
            if (canonical.GetComponent<PlayerController>() != null)
                throw new InvalidOperationException("Gameplay PlayerController must remain runtime-composed.");

            AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedDirectory + "/Idle.anim");
            AnimationClip skate = AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedDirectory + "/Skate.anim");
            AnimationClip shoot = AssetDatabase.LoadAssetAtPath<AnimationClip>(GeneratedDirectory + "/Shoot.anim");
            if (idle == null || skate == null || shoot == null || !idle.humanMotion || !skate.humanMotion || !shoot.humanMotion
                || AnimationUtility.GetCurveBindings(idle).Length == 0
                || AnimationUtility.GetCurveBindings(skate).Length == 0
                || AnimationUtility.GetCurveBindings(shoot).Length == 0)
                throw new InvalidOperationException("Placeholder animation clips are missing Humanoid muscle motion.");
            ValidateHumanoidCurves(idle);
            ValidateHumanoidCurves(skate);
            ValidateHumanoidCurves(shoot);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            string[] stateNames = controller != null
                ? controller.layers[0].stateMachine.states.Select(state => state.state.name).ToArray()
                : Array.Empty<string>();
            if (!stateNames.Contains("Idle") || !stateNames.Contains("Skate") || !stateNames.Contains("Shoot"))
                throw new InvalidOperationException("Animator controller is missing placeholder states.");
            Transform visualBlade = loadout.FindEquippedChild(HockeyEquipmentSlot.Stick, "Stick Blade");
            if (visualBlade == null) throw new InvalidOperationException("Replaceable Stick has no visual blade.");

            ValidateEditorEquipmentPersistence();
            ValidateTexturePolicy();
            ValidateRendererPolicy(canonical);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("ModularCharacterTest scene is missing.");
            Debug.Log("MODULAR_CHARACTER_ASSETS_VALID");
        }

        private static void EnsureGenerated()
        {
            if (Application.isPlaying || SessionState.GetBool(AutoGenerationKey, false)) return;
            GameObject canonical = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            HockeyEquipmentLoadout loadout = canonical != null ? canonical.GetComponent<HockeyEquipmentLoadout>() : null;
            if (loadout != null && loadout.IsComplete() && loadout.SlotCount == ExpectedEquipmentSlotCount
                && HasActiveEquipment(loadout)
                && importer != null && importer.animationType == ModelImporterAnimationType.Human) return;

            SessionState.SetBool(AutoGenerationKey, true);
            try { GenerateAll(); }
            catch (Exception exception) { Debug.LogException(exception); }
            finally { SessionState.EraseBool(AutoGenerationKey); }
        }

        private static void ConfigureHumanoidImporter()
        {
            ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
            if (importer == null) throw new FileNotFoundException("Selected humanoid FBX is missing.", ModelPath);
            bool changed = importer.animationType != ModelImporterAnimationType.Human
                || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel
                || importer.isReadable || importer.optimizeGameObjects;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.isReadable = false;
            importer.optimizeGameObjects = false;
            if (changed) importer.SaveAndReimport();
            Avatar avatar = LoadHumanoidAvatar();
            if (avatar == null || !avatar.isHuman || !avatar.isValid)
                throw new InvalidOperationException("Humanoid auto-mapping failed for RealisticHumanMale/unity.Fbx.");
        }

        private static Avatar LoadHumanoidAvatar()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            Animator animator = model != null ? model.GetComponent<Animator>() : null;
            if (animator != null && animator.avatar != null) return animator.avatar;
            return AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
        }

        private static void ConfigureMobileTextures()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { CharacterDirectory });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                importer.isReadable = false;
                importer.mipmapEnabled = true;
                ApplyMobilePlatform(importer, "Android");
                ApplyMobilePlatform(importer, "iPhone");
                importer.SaveAndReimport();
            }
            Debug.Log($"MODULAR_CHARACTER_TEXTURE_POLICY count={guids.Length} max=1024 format=ASTC_6x6");
        }

        private static void ApplyMobilePlatform(TextureImporter importer, string platform)
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
            settings.name = platform;
            settings.overridden = true;
            settings.maxTextureSize = 1024;
            settings.format = TextureImporterFormat.ASTC_6x6;
            settings.textureCompression = TextureImporterCompression.Compressed;
            importer.SetPlatformTextureSettings(settings);
        }

        private static GameObject BuildPlayer(Material skinMaterial, Material equipmentMaterial, Material jerseyMaterial)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            Avatar avatar = LoadHumanoidAvatar();
            if (modelAsset == null || avatar == null) throw new InvalidOperationException("Selected humanoid model is unavailable.");

            GameObject root = new("HockeyPlayer");
            CharacterController characterController = root.AddComponent<CharacterController>();
            characterController.center = Vector3.zero;
            characterController.height = 2f;
            characterController.radius = 0.45f;

            GameObject visual = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (visual == null) throw new InvalidOperationException("Unable to instantiate selected humanoid model.");
            visual.name = "HumanoidVisual";
            visual.transform.SetParent(root.transform, false);
            Animator animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            OptimizeRenderers(visual, skinMaterial);

            RigBuilder rigBuilder = visual.AddComponent<RigBuilder>();
            GameObject presentationRoot = NewChild("StickPresentationRoot", visual.transform);
            GameObject targetsRoot = NewChild("StickRigTargets", presentationRoot.transform);
            Transform leftTarget = NewTarget("LeftHandTarget", targetsRoot.transform, new Vector3(0.05f, 0.66f, 0.36f));
            Transform rightTarget = NewTarget("RightHandTarget", targetsRoot.transform, new Vector3(0.24f, 0.95f, 0.1f));
            Transform leftHint = NewTarget("LeftElbowHint", targetsRoot.transform, new Vector3(-0.45f, 0.95f, 0.2f));
            Transform rightHint = NewTarget("RightElbowHint", targetsRoot.transform, new Vector3(0.52f, 1.05f, 0f));
            Transform shaftEndReference = NewTarget("ShaftEndReference", targetsRoot.transform, new Vector3(-0.153f, 0.35f, 0.638f));
            Transform bladeReference = NewTarget("BladeReference", targetsRoot.transform, new Vector3(0f, 0.412f, 1.69f));

            GameObject rigObject = NewChild("HockeyStickRigConstraints", visual.transform);
            Rig rig = rigObject.AddComponent<Rig>();
            TwoBoneIKConstraint leftConstraint = CreateArmConstraint("LeftHandIK", rigObject.transform, animator,
                HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, leftTarget, leftHint);
            TwoBoneIKConstraint rightConstraint = CreateArmConstraint("RightHandIK", rigObject.transform, animator,
                HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, rightTarget, rightHint);
            rigBuilder.layers.Add(new RigLayer(rig));

            HockeyStickRig stickRig = root.AddComponent<HockeyStickRig>();
            stickRig.Configure(leftConstraint, rightConstraint, leftTarget, rightTarget, leftHint, rightHint,
                shaftEndReference, bladeReference);

            Transform head = animator.GetBoneTransform(HumanBodyBones.Head) ?? root.transform;
            Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest) ?? root.transform;
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips) ?? root.transform;
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand) ?? visual.transform;
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand) ?? visual.transform;
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot) ?? visual.transform;
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot) ?? visual.transform;
            HockeyEquipmentBinding[] bindings = new HockeyEquipmentBinding[ExpectedEquipmentSlotCount];
            bindings[0] = CreateBinding(HockeyEquipmentSlot.Helmet, head, "HelmetSlot",
                BuildSinglePrimitive("Helmet", PrimitiveType.Sphere, equipmentMaterial, new Vector3(0f, 0.1f, 0f), new Vector3(0.25f, 0.19f, 0.28f)));
            bindings[1] = CreateBinding(HockeyEquipmentSlot.ShoulderPads, chest, "ShoulderPadsSlot",
                BuildShoulderPads(equipmentMaterial));
            bindings[2] = CreateBinding(HockeyEquipmentSlot.Jersey, chest, "JerseySlot",
                BuildSinglePrimitive("Jersey", PrimitiveType.Cube, jerseyMaterial, Vector3.zero, new Vector3(0.42f, 0.34f, 0.2f)));
            bindings[3] = CreateBinding(HockeyEquipmentSlot.Gloves, visual.transform, "GlovesSlot",
                BuildFollowedPair("Gloves", PrimitiveType.Sphere, equipmentMaterial, leftTarget.localPosition,
                    rightTarget.localPosition, new Vector3(0.13f, 0.11f, 0.14f)));
            bindings[4] = CreateBinding(HockeyEquipmentSlot.Pants, hips, "PantsSlot",
                BuildSinglePrimitive("Pants", PrimitiveType.Cube, equipmentMaterial, Vector3.zero, new Vector3(0.38f, 0.25f, 0.22f)));
            bindings[5] = CreateBinding(HockeyEquipmentSlot.Socks, visual.transform, "SocksSlot",
                BuildFollowedPair("Socks", PrimitiveType.Cube, jerseyMaterial, new Vector3(-0.13f, 0.35f, 0.01f),
                    new Vector3(0.13f, 0.35f, 0.01f), new Vector3(0.13f, 0.28f, 0.13f)));
            bindings[6] = CreateBinding(HockeyEquipmentSlot.Skates, visual.transform, "SkatesSlot",
                BuildFollowedPair("Skates", PrimitiveType.Cube, equipmentMaterial, new Vector3(-0.13f, 0.08f, 0.03f),
                    new Vector3(0.13f, 0.08f, 0.03f), new Vector3(0.11f, 0.07f, 0.27f)));
            bindings[7] = CreateBinding(HockeyEquipmentSlot.Stick, presentationRoot.transform, "StickSlot",
                BuildStick(equipmentMaterial, rightTarget.localPosition, shaftEndReference.localPosition,
                    bladeReference.localPosition));
            HockeyEquipmentLoadout loadout = root.AddComponent<HockeyEquipmentLoadout>();
            loadout.Configure(bindings, stickRig, leftHand, rightHand, leftFoot, rightFoot);
            HockeyCharacterPresentation presentation = root.AddComponent<HockeyCharacterPresentation>();
            presentation.Configure(animator, loadout);
            return root;
        }

        private static TwoBoneIKConstraint CreateArmConstraint(string name, Transform parent, Animator animator,
            HumanBodyBones rootBone, HumanBodyBones midBone, HumanBodyBones tipBone, Transform target, Transform hint)
        {
            Transform armRoot = animator.GetBoneTransform(rootBone);
            Transform armMid = animator.GetBoneTransform(midBone);
            Transform armTip = animator.GetBoneTransform(tipBone);
            if (armRoot == null || armMid == null || armTip == null)
                throw new InvalidOperationException($"Humanoid avatar is missing required bones for {name}.");
            TwoBoneIKConstraint constraint = NewChild(name, parent).AddComponent<TwoBoneIKConstraint>();
            TwoBoneIKConstraintData data = constraint.data;
            data.root = armRoot;
            data.mid = armMid;
            data.tip = armTip;
            data.target = target;
            data.hint = hint;
            data.targetPositionWeight = 1f;
            data.targetRotationWeight = 1f;
            data.hintWeight = 0.75f;
            data.maintainTargetPositionOffset = false;
            data.maintainTargetRotationOffset = true;
            constraint.data = data;
            constraint.weight = 1f;
            return constraint;
        }

        private static HockeyEquipmentBinding CreateBinding(HockeyEquipmentSlot slot, Transform parent,
            string anchorName, GameObject item)
        {
            Transform anchor = NewChild(anchorName, parent).transform;
            item.transform.SetParent(anchor, false);
            HockeyEquipmentBinding binding = new();
            binding.Configure(slot, anchor, item);
            return binding;
        }

        private static GameObject BuildSinglePrimitive(string name, PrimitiveType type, Material material,
            Vector3 position, Vector3 scale)
        {
            GameObject item = new(name);
            GameObject visual = CreatePrimitive(name + "Visual", type, material, item.transform);
            visual.transform.localPosition = position;
            visual.transform.localScale = scale;
            return item;
        }

        private static GameObject BuildFollowedPair(string name, PrimitiveType type, Material material,
            Vector3 firstPosition, Vector3 secondPosition, Vector3 scale)
        {
            GameObject item = new(name);
            GameObject first = CreatePrimitive(name + " L", type, material, item.transform);
            GameObject second = CreatePrimitive(name + " R", type, material, item.transform);
            first.transform.localPosition = firstPosition;
            second.transform.localPosition = secondPosition;
            first.transform.localScale = scale;
            second.transform.localScale = scale;
            HockeyPairedEquipmentFollower follower = item.AddComponent<HockeyPairedEquipmentFollower>();
            follower.ConfigureVisuals(first.transform, second.transform);
            return item;
        }

        private static GameObject BuildShoulderPads(Material material)
        {
            GameObject item = new("Shoulder Pads");
            GameObject chestPad = CreatePrimitive("Shoulder Pads Chest", PrimitiveType.Cube, material, item.transform);
            chestPad.transform.localScale = new Vector3(0.44f, 0.2f, 0.22f);
            GameObject leftCap = CreatePrimitive("Shoulder Pad L", PrimitiveType.Sphere, material, item.transform);
            leftCap.transform.localPosition = new Vector3(-0.38f, 0.08f, 0f);
            leftCap.transform.localScale = new Vector3(0.18f, 0.16f, 0.2f);
            GameObject rightCap = CreatePrimitive("Shoulder Pad R", PrimitiveType.Sphere, material, item.transform);
            rightCap.transform.localPosition = new Vector3(0.38f, 0.08f, 0f);
            rightCap.transform.localScale = new Vector3(0.18f, 0.16f, 0.2f);
            return item;
        }

        private static GameObject BuildStick(Material material, Vector3 grip, Vector3 shaftEnd, Vector3 blade)
        {
            GameObject item = new("Stick");
            Vector3 middle = (grip + shaftEnd) * 0.5f;
            Vector3 direction = shaftEnd - grip;
            GameObject shaft = CreatePrimitive("Stick Shaft", PrimitiveType.Cube, material, item.transform);
            shaft.transform.localPosition = middle;
            shaft.transform.localScale = new Vector3(0.035f, direction.magnitude, 0.035f);
            shaft.transform.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            GameObject bladeVisual = new("Stick Blade");
            bladeVisual.transform.SetParent(item.transform, false);
            bladeVisual.transform.localPosition = blade;
            Vector3 bladeDirection = blade - shaftEnd;
            GameObject bladeMesh = CreatePrimitive("Stick Blade Visual", PrimitiveType.Cube, material, bladeVisual.transform);
            bladeMesh.transform.localPosition = -bladeDirection * 0.5f;
            bladeMesh.transform.localScale = new Vector3(0.06f, bladeDirection.magnitude, 0.1f);
            bladeMesh.transform.localRotation = Quaternion.FromToRotation(Vector3.up, bladeDirection.normalized);
            return item;
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Material material, Transform parent)
        {
            GameObject primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            Collider collider = primitive.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            Renderer renderer = primitive.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            return primitive;
        }

        private static void OptimizeRenderers(GameObject visual, Material skinMaterial)
        {
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                renderers[i].reflectionProbeUsage = ReflectionProbeUsage.Off;
                renderers[i].lightProbeUsage = LightProbeUsage.Off;
                renderers[i].receiveShadows = false;
                if (renderers[i].sharedMaterials == null || renderers[i].sharedMaterials.Length == 0)
                    renderers[i].sharedMaterial = skinMaterial;
            }
        }

        private static void CreateAnimationAssets(GameObject player)
        {
            Animator animator = player.GetComponentInChildren<Animator>();
            AnimationClip idle = LoadOrCreateClip(GeneratedDirectory + "/Idle.anim", "Idle", true);
            AnimationClip skate = LoadOrCreateClip(GeneratedDirectory + "/Skate.anim", "Skate", true);
            AnimationClip shoot = LoadOrCreateClip(GeneratedDirectory + "/Shoot.anim", "Shoot", false);
            AddMuscleCurve(idle, "Spine Front-Back", new[] { 0f, 0.5f, 1f }, new[] { -0.03f, 0.03f, -0.03f });
            AddMuscleCurve(skate, "Left Upper Leg Front-Back", new[] { 0f, 0.3f, 0.6f }, new[] { -0.28f, 0.28f, -0.28f });
            AddMuscleCurve(skate, "Right Upper Leg Front-Back", new[] { 0f, 0.3f, 0.6f }, new[] { 0.28f, -0.28f, 0.28f });
            AddMuscleCurve(shoot, "Spine Twist Left-Right", new[] { 0f, 0.18f, 0.42f }, new[] { -0.18f, 0.32f, 0.04f });
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.parameters = new[]
            {
                new AnimatorControllerParameter { name = "Speed", type = AnimatorControllerParameterType.Float },
                new AnimatorControllerParameter { name = "Shoot", type = AnimatorControllerParameterType.Trigger }
            };
            if (controller.layers == null || controller.layers.Length == 0) controller.AddLayer("Base Layer");
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            ChildAnimatorState[] existingStates = machine.states;
            for (int i = 0; i < existingStates.Length; i++)
                machine.RemoveState(existingStates[i].state);
            AnimatorStateTransition[] existingAnyTransitions = machine.anyStateTransitions;
            for (int i = 0; i < existingAnyTransitions.Length; i++)
                machine.RemoveAnyStateTransition(existingAnyTransitions[i]);
            AnimatorState idleState = machine.AddState("Idle");
            AnimatorState skateState = machine.AddState("Skate");
            AnimatorState shootState = machine.AddState("Shoot");
            idleState.motion = idle;
            skateState.motion = skate;
            shootState.motion = shoot;
            machine.defaultState = idleState;
            AnimatorStateTransition toSkate = idleState.AddTransition(skateState);
            toSkate.hasExitTime = false;
            toSkate.duration = 0.12f;
            toSkate.AddCondition(AnimatorConditionMode.Greater, 0.05f, "Speed");
            AnimatorStateTransition toIdle = skateState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.12f;
            toIdle.AddCondition(AnimatorConditionMode.Less, 0.05f, "Speed");
            AnimatorStateTransition toShoot = machine.AddAnyStateTransition(shootState);
            toShoot.hasExitTime = false;
            toShoot.duration = 0.04f;
            toShoot.AddCondition(AnimatorConditionMode.If, 0f, "Shoot");
            AnimatorStateTransition shotComplete = shootState.AddTransition(idleState);
            shotComplete.hasExitTime = true;
            shotComplete.exitTime = 0.95f;
            shotComplete.duration = 0.08f;
        }

        private static AnimationClip LoadOrCreateClip(string path, string name, bool loop)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }
            EditorCurveBinding[] floatBindings = AnimationUtility.GetCurveBindings(clip);
            for (int i = 0; i < floatBindings.Length; i++) AnimationUtility.SetEditorCurve(clip, floatBindings[i], null);
            EditorCurveBinding[] objectBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            for (int i = 0; i < objectBindings.Length; i++) AnimationUtility.SetObjectReferenceCurve(clip, objectBindings[i], null);
            clip.name = name;
            clip.frameRate = 30f;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static void AddMuscleCurve(AnimationClip clip, string muscleName, float[] times, float[] values)
        {
            if (!HumanTrait.MuscleName.Contains(muscleName))
                throw new InvalidOperationException($"Unknown Humanoid muscle '{muscleName}'.");
            Keyframe[] keys = new Keyframe[times.Length];
            for (int i = 0; i < times.Length; i++)
                keys[i] = new Keyframe(times[i], values[i]);
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), muscleName), new AnimationCurve(keys));
        }

        private static void ValidateHumanoidCurves(AnimationClip clip)
        {
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(clip);
            if (bindings.Length == 0 || bindings.Any(binding => binding.type != typeof(Animator)
                    || !string.IsNullOrEmpty(binding.path) || !HumanTrait.MuscleName.Contains(binding.propertyName)))
                throw new InvalidOperationException($"{clip.name} contains a non-Humanoid animation binding.");
        }

        private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector3 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared < 0.000001f) return Vector3.Distance(point, start);
            float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
            return Vector3.Distance(point, start + segment * t);
        }

        private static void CreateResourceVariant()
        {
            GameObject canonical = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (canonical == null) throw new InvalidOperationException("Canonical HockeyPlayer prefab was not generated.");
            GameObject instance = PrefabUtility.InstantiatePrefab(canonical) as GameObject;
            if (instance == null) throw new InvalidOperationException("Unable to instantiate canonical HockeyPlayer prefab.");
            try
            {
                instance.name = "Skater";
                PrefabUtility.SaveAsPrefabAsset(instance, ResourcePrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static void CreateTestScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject canonical = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            for (int i = 0; i < 10; i++)
            {
                GameObject player = PrefabUtility.InstantiatePrefab(canonical, scene) as GameObject;
                player.name = $"Test HockeyPlayer {i + 1:00}";
                player.transform.position = new Vector3((i % 5 - 2) * 2.4f, 0f, (i / 5) * 3.2f);
            }

            GameObject puckObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puckObject.name = "Test Puck";
            puckObject.transform.position = new Vector3(0f, 0.28f, 0f);
            puckObject.transform.localScale = new Vector3(0.42f, 0.06f, 0.42f);
            Rigidbody body = puckObject.AddComponent<Rigidbody>();
            body.mass = 0.17f;
            PuckController puck = puckObject.AddComponent<PuckController>();
            GameObject harnessObject = new("Modular Character Test Harness");
            ModularCharacterTestHarness harness = harnessObject.AddComponent<ModularCharacterTestHarness>();
            harness.Configure(puck);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException("Unable to save ModularCharacterTest scene.");
        }

        private static void ValidateEditorEquipmentPersistence()
        {
            const string tempPath = GeneratedDirectory + "/EquipmentPersistenceValidation.prefab";
            DeleteAssetIfPresent(tempPath);
            GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                HockeyEquipmentLoadout loadout = contents.GetComponent<HockeyEquipmentLoadout>();
                HockeyStickRig rig = contents.GetComponent<HockeyStickRig>();
                foreach (HockeyEquipmentSlot slot in Enum.GetValues(typeof(HockeyEquipmentSlot)))
                {
                    Dictionary<HockeyEquipmentSlot, GameObject> before = Enum.GetValues(typeof(HockeyEquipmentSlot))
                        .Cast<HockeyEquipmentSlot>().ToDictionary(value => value, loadout.GetEquipped);
                    loadout.Clear(slot);
                    if (loadout.GetEquipped(slot) != null)
                        throw new InvalidOperationException($"Edit Mode clear of {slot} did not empty the slot.");
                    foreach (HockeyEquipmentSlot other in Enum.GetValues(typeof(HockeyEquipmentSlot)))
                        if (other != slot && loadout.GetEquipped(other) != before[other])
                            throw new InvalidOperationException($"Edit Mode clear of {slot} changed {other}.");
                    GameObject replacement = new("EditorReplacement_" + slot);
                    loadout.Equip(slot, replacement);
                    foreach (HockeyEquipmentSlot other in Enum.GetValues(typeof(HockeyEquipmentSlot)))
                        if (other != slot && loadout.GetEquipped(other) != before[other])
                            throw new InvalidOperationException($"Edit Mode replacement of {slot} changed {other}.");
                }
                if (!rig.HasValidReferences || rig.LeftHandTarget.IsChildOf(loadout.GetEquipped(HockeyEquipmentSlot.Stick).transform)
                    || rig.RightHandTarget.IsChildOf(loadout.GetEquipped(HockeyEquipmentSlot.Stick).transform))
                    throw new InvalidOperationException("Stick replacement invalidated stable IK references.");
                PrefabUtility.SaveAsPrefabAsset(contents, tempPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }

            GameObject reloaded = AssetDatabase.LoadAssetAtPath<GameObject>(tempPath);
            HockeyEquipmentLoadout persisted = reloaded != null ? reloaded.GetComponent<HockeyEquipmentLoadout>() : null;
            if (persisted == null) throw new InvalidOperationException("Temporary equipment validation prefab did not reload.");
            foreach (HockeyEquipmentSlot slot in Enum.GetValues(typeof(HockeyEquipmentSlot)))
                if (persisted.GetEquipped(slot) == null || !persisted.GetEquipped(slot).name.StartsWith("EditorReplacement_"))
                    throw new InvalidOperationException($"Edit Mode {slot} replacement did not persist after save/reload.");
            DeleteAssetIfPresent(tempPath);
        }

        private static bool HasActiveEquipment(HockeyEquipmentLoadout loadout)
        {
            if (loadout == null) return false;
            foreach (HockeyEquipmentSlot slot in Enum.GetValues(typeof(HockeyEquipmentSlot)))
            {
                GameObject equipped = loadout.GetEquipped(slot);
                if (equipped == null || !equipped.activeSelf) return false;
            }
            return true;
        }

        private static void ValidateTexturePolicy()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { CharacterDirectory });
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.isReadable || !importer.mipmapEnabled)
                    throw new InvalidOperationException("Texture policy failed for " + path);
                ValidatePlatform(importer, path, "Android");
                ValidatePlatform(importer, path, "iPhone");
            }
        }

        private static void ValidatePlatform(TextureImporter importer, string path, string platform)
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
            if (!settings.overridden || settings.maxTextureSize != 1024 || settings.format != TextureImporterFormat.ASTC_6x6)
                throw new InvalidOperationException($"{platform} texture policy failed for {path}.");
        }

        private static void ValidateRendererPolicy(GameObject canonical)
        {
            Renderer[] renderers = canonical.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("HockeyPlayer has no renderers.");
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i].motionVectorGenerationMode != MotionVectorGenerationMode.ForceNoMotion
                    || renderers[i].reflectionProbeUsage != ReflectionProbeUsage.Off)
                    throw new InvalidOperationException("Renderer mobile policy failed on " + renderers[i].name);
        }

        private static Material CreateOrUpdateMaterial(string name, Color color)
        {
            string path = GeneratedDirectory + "/" + name + ".mat";
            Shader shader = Shader.Find("Mobile/Diffuse") ?? Shader.Find("Standard");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.name = name;
            material.shader = shader;
            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform NewTarget(string name, Transform parent, Vector3 localPosition)
        {
            Transform target = NewChild(name, parent).transform;
            target.localPosition = localPosition;
            return target;
        }

        private static GameObject NewChild(string name, Transform parent)
        {
            GameObject child = new(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void DeleteAssetIfPresent(string path)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) AssetDatabase.DeleteAsset(path);
        }
    }
}
#endif
