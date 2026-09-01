/*
 * IceClash modular clean-humanoid asset generator and validator.
 * Consumes the isolated Male_Base_v1_1_Clean production visual/controller,
 * applies the existing mobile presentation policy,
 * builds all eight hockey gear pieces, nests the validated production stick,
 * aligns its explicit grip/contact markers in a professional two-hand hockey carry
 * with the top hand beside the hip and naturally bent, outward-facing elbows,
 * keeps the blade on the established gameplay control point, and creates shared
 * presentation assets, prefabs, IK, the ten-player scene, and gameplay evidence
 * without debug geometry.
 */

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IceClash.CharacterValidation.Editor;
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
        private const string ModelPath = MaleBaseV11GameplayIntegrationSetup.CleanModelPath;
        private const string CharacterDirectory = "Assets/_Project/Art/Characters/RealisticHumanMale";
        private const string StickDirectory = "Assets/Equipment/Sticks/Hockey_Stick_Base_v1";
        private const string StickModelPath = StickDirectory + "/Meshy_Hockey_Stick_Base_v1.fbx";
        private const string StickPrefabPath = StickDirectory + "/Hockey_Stick_Base_v1.prefab";
        private const string StickMaterialPath = StickDirectory + "/Hockey_Stick_Base_v1.mat";
        private const string LegacyStickModelPath = "Assets/_Project/Art/HockeyGear/LowPolyStick/hockey_stick_002.fbx";
        private const string LegacyStickMaterialPath = "Assets/_Project/Art/HockeyPrototype/LowPolyHockeyStick.mat";
        private const string GeneratedDirectory = "Assets/_Project/Art/HockeyPrototype";
        private const string PrefabPath = "Assets/_Project/Prefabs/HockeyPlayer.prefab";
        private const string ResourcePrefabPath = "Assets/_Project/Prefabs/Resources/Skater.prefab";
        private const string ScenePath = "Assets/_Project/Scenes/ModularCharacterTest.unity";
        private const string ControllerPath = MaleBaseV11GameplayIntegrationSetup.ControllerPath;
        private const string AutoGenerationKey = "IceClash.ModularCharacterGenerationRunning";
        private const string GameplayEvidencePath = ".docs/evidence/use-production-stick-in-gameplay/prototype-arena-production-stick.png";
        private const int ExpectedEquipmentSlotCount = 8;
        private const float GameplayControlForwardOffset = 1.15f;
        private const float GameplayControlVerticalOffset = 0.28f;
        private const float GameplaySkaterScale = 0.68f;
        private const float GameplayIceY = 0.2f;
        private const float GameplaySpawnY = 1f;
        private const float ProductionShaftExtension = 3.05f;
        private static readonly Vector3 PrimaryGripPose = new(0.58f, 1.16f, 0.02f);
        private static readonly Vector3 SecondaryGripPoseHint = new(0.30f, 1.10f, 0.30f);
        private static readonly Vector3 LeftElbowHintPose = new(-0.45f, 1.05f, 0.15f);
        private static readonly Vector3 RightElbowHintPose = new(0.72f, 1.18f, -0.05f);
        private static readonly Vector3 BladePose = new(0f, 0.25f, 1.55f);

        static HockeyCharacterAssetSetup()
        {
            EditorApplication.delayCall += EnsureGenerated;
        }

        [MenuItem("IceClash/Generate Modular Hockey Character")]
        public static void GenerateAll()
        {
            EnsureFolder(GeneratedDirectory);
            MaleBaseV11GameplayIntegrationSetup.GenerateProductionAssets();
            ConfigureMobileTextures();
            ConfigureStickAssets();
            Material skinMaterial = CreateOrUpdateMaterial("CharacterMobile", new Color(0.72f, 0.52f, 0.4f));
            Material equipmentMaterial = CreateOrUpdateMaterial("EquipmentDark", new Color(0.035f, 0.055f, 0.085f));
            Material jerseyMaterial = CreateOrUpdateMaterial("JerseyNeutral", new Color(0.22f, 0.42f, 0.78f));
            GameObject player = BuildPlayer(skinMaterial, equipmentMaterial, jerseyMaterial);
            try
            {
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

        [MenuItem("IceClash/Capture Production Stick Gameplay Evidence")]
        public static void CaptureProductionStickGameplayEvidence()
        {
            if (!Application.isPlaying)
                throw new InvalidOperationException("PrototypeArena must be in Play Mode to capture gameplay evidence.");
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string absolutePath = Path.Combine(projectRoot, GameplayEvidencePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            ScreenCapture.CaptureScreenshot(absolutePath, 1);
            Debug.Log($"PRODUCTION_STICK_GAMEPLAY_EVIDENCE path={GameplayEvidencePath}");
        }

        [MenuItem("IceClash/Validate Production Stick Gameplay Evidence")]
        public static void ValidateProductionStickGameplayEvidence()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string evidencePath = Path.Combine(projectRoot, GameplayEvidencePath);
            if (!File.Exists(evidencePath))
                throw new FileNotFoundException("Production-stick gameplay evidence is missing.", evidencePath);
            DateTime evidenceTime = File.GetLastWriteTimeUtc(evidencePath);
            string[] renderInputs = { PrefabPath, ScenePath };
            for (int i = 0; i < renderInputs.Length; i++)
            {
                string inputPath = Path.Combine(projectRoot, renderInputs[i]);
                if (!File.Exists(inputPath) || File.GetLastWriteTimeUtc(inputPath) > evidenceTime)
                    throw new InvalidOperationException(
                        $"Gameplay evidence predates its render input: {renderInputs[i]}.");
            }
            Debug.Log($"PRODUCTION_STICK_GAMEPLAY_EVIDENCE_VALID path={GameplayEvidencePath}");
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
            if (canonical.GetComponent<PlayerController>() != null)
                throw new InvalidOperationException("Gameplay PlayerController must remain runtime-composed.");

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            string[] stateNames = controller != null
                ? controller.layers[0].stateMachine.states.Select(state => state.state.name).ToArray()
                : Array.Empty<string>();
            if (stateNames.Length != 2 || !stateNames.Contains("Idle") || !stateNames.Contains("Running"))
                throw new InvalidOperationException("Animator controller must contain only Idle and temporary Running.");
            Transform visualBlade = loadout.FindEquippedChild(HockeyEquipmentSlot.Stick, "Stick Blade");
            if (visualBlade == null) throw new InvalidOperationException("Replaceable Stick has no visual blade.");
            Transform shaftMarker = loadout.FindEquippedChild(HockeyEquipmentSlot.Stick, "Stick Shaft");
            Transform gripMarker = loadout.FindEquippedChild(HockeyEquipmentSlot.Stick, "Stick Grip");
            Transform debugBlade = loadout.FindEquippedChild(HockeyEquipmentSlot.Stick, "Stick Blade Visibility");
            if (shaftMarker == null || gripMarker == null || shaftMarker.GetComponent<Renderer>() != null || debugBlade != null)
                throw new InvalidOperationException("Hockey stick must render only the imported mesh, not debug primitives.");
            Transform importedStick = loadout.FindEquippedChild(HockeyEquipmentSlot.Stick, "Production Hockey Stick");
            Transform productionRoot = importedStick != null ? importedStick.Find("Hockey_Stick_Base_v1") : null;
            Transform primaryGrip = productionRoot != null ? productionRoot.Find("PrimaryGrip") : null;
            Transform secondaryGrip = productionRoot != null ? productionRoot.Find("SecondaryGrip") : null;
            Transform bladeContact = productionRoot != null ? productionRoot.Find("BladeContact") : null;
            Renderer[] stickRenderers = productionRoot != null
                ? productionRoot.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
            int stickTriangles = productionRoot != null
                ? productionRoot.GetComponentsInChildren<MeshFilter>(true)
                    .Where(filter => filter.sharedMesh != null)
                    .Sum(filter => CountMeshTriangles(filter.sharedMesh))
                : 0;
            if (importedStick == null || productionRoot == null || primaryGrip == null || secondaryGrip == null
                || bladeContact == null || stickRenderers.Length == 0 || stickTriangles != 4347)
                throw new InvalidOperationException(
                    $"Validated production hockey stick is missing or has unexpected geometry ({stickTriangles} triangles).");
            Vector3 stickScale = importedStick.localScale;
            float minStickScale = Mathf.Min(stickScale.x, Mathf.Min(stickScale.y, stickScale.z));
            float maxStickScale = Mathf.Max(stickScale.x, Mathf.Max(stickScale.y, stickScale.z));
            Material stickMaterial = AssetDatabase.LoadAssetAtPath<Material>(StickMaterialPath);
            GameObject sourceStick = AssetDatabase.LoadAssetAtPath<GameObject>(StickPrefabPath);
            ModelImporter stickImporter = AssetImporter.GetAtPath(StickModelPath) as ModelImporter;
            string[] prefabDependencies = AssetDatabase.GetDependencies(PrefabPath, true);
            string[] resourceDependencies = AssetDatabase.GetDependencies(ResourcePrefabPath, true);
            if (minStickScale <= 0f || maxStickScale / minStickScale > 1.001f
                || stickRenderers.Any(renderer => renderer.allowOcclusionWhenDynamic
                    || renderer.sharedMaterial != stickMaterial || !HasSmallMeshCullingDisabled(renderer))
                || stickMaterial == null || stickMaterial.shader.name != "Standard"
                || stickMaterial.GetTexture("_MainTex") == null || stickMaterial.GetTexture("_BumpMap") == null
                || stickMaterial.GetTexture("_MetallicGlossMap") == null
                || sourceStick == null || stickImporter == null || stickImporter.isReadable
                || stickImporter.animationType != ModelImporterAnimationType.None || stickImporter.importAnimation
                || !prefabDependencies.Contains(StickPrefabPath) || !resourceDependencies.Contains(StickPrefabPath)
                || prefabDependencies.Contains(LegacyStickModelPath) || resourceDependencies.Contains(LegacyStickModelPath)
                || prefabDependencies.Contains(LegacyStickMaterialPath) || resourceDependencies.Contains(LegacyStickMaterialPath))
                throw new InvalidOperationException("Production hockey stick integration or arena renderer policy is invalid.");
            Bounds stickBounds = stickRenderers[0].bounds;
            for (int i = 1; i < stickRenderers.Length; i++) stickBounds.Encapsulate(stickRenderers[i].bounds);
            float fittedLength = Vector3.Distance(gripMarker.position, visualBlade.position);
            if (Vector3.Distance(stickBounds.ClosestPoint(gripMarker.position), gripMarker.position) > fittedLength * 0.15f
                || Vector3.Distance(stickBounds.ClosestPoint(visualBlade.position), visualBlade.position) > fittedLength * 0.15f)
                throw new InvalidOperationException("Production hockey stick no longer reaches its grip and blade markers.");
            if (Vector3.Distance(primaryGrip.position, stickRig.RightHandTarget.position) > 0.001f
                || Vector3.Distance(secondaryGrip.position, stickRig.LeftHandTarget.position) > 0.001f
                || Vector3.Distance(bladeContact.position, visualBlade.position) > 0.001f
                || DistanceToSegment(stickRig.LeftHandTarget.position, primaryGrip.position, secondaryGrip.position) > 0.001f
                || Vector3.Distance(stickBounds.ClosestPoint(stickRig.LeftHandTarget.position),
                    stickRig.LeftHandTarget.position) > fittedLength * 0.05f)
                throw new InvalidOperationException("Production stick grip markers are not aligned to the two-hand IK targets.");
            Vector3 gripToBlade = visualBlade.position - gripMarker.position;
            if (gripMarker.position.y - visualBlade.position.y < 0.8f
                || Mathf.Abs(gripToBlade.x) < 0.55f || gripToBlade.z < 1f
                || primaryGrip.position.y - secondaryGrip.position.y < 0.25f
                || secondaryGrip.position.z - primaryGrip.position.z < 0.25f
                || Vector3.Distance(primaryGrip.position, secondaryGrip.position) < 0.55f
                || stickRig.RightHandTarget.localPosition.x < 0.45f
                || stickRig.RightHandTarget.localPosition.y < 0.95f
                || stickRig.RightHandTarget.localPosition.y > 1.25f)
                throw new InvalidOperationException("Production stick no longer forms the required professional two-hand carry.");

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
            ModelImporter stickImporter = AssetImporter.GetAtPath(StickModelPath) as ModelImporter;
            HockeyEquipmentLoadout loadout = canonical != null ? canonical.GetComponent<HockeyEquipmentLoadout>() : null;
            HockeyStickRig stickRig = canonical != null ? canonical.GetComponent<HockeyStickRig>() : null;
            if (loadout != null && loadout.IsComplete() && loadout.SlotCount == ExpectedEquipmentSlotCount
                && HasActiveEquipment(loadout)
                && loadout.FindEquippedChild(HockeyEquipmentSlot.Stick, "Production Hockey Stick") != null
                && stickRig != null && stickRig.RightHandTarget != null
                && Vector3.Distance(stickRig.RightHandTarget.localPosition, PrimaryGripPose) <= 0.001f
                && importer != null && importer.animationType == ModelImporterAnimationType.Human
                && stickImporter != null && !stickImporter.isReadable) return;

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

        private static void ConfigureStickAssets()
        {
            ModelImporter importer = AssetImporter.GetAtPath(StickModelPath) as ModelImporter;
            if (importer == null) throw new FileNotFoundException("Production hockey stick FBX is missing.", StickModelPath);
            bool modelChanged = importer.animationType != ModelImporterAnimationType.None
                || importer.importAnimation || importer.isReadable || importer.addCollider;
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.isReadable = false;
            importer.addCollider = false;
            if (modelChanged) importer.SaveAndReimport();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(StickPrefabPath) == null
                || AssetDatabase.LoadAssetAtPath<Material>(StickMaterialPath) == null)
                throw new FileNotFoundException("Validated production hockey stick prefab or material is missing.", StickDirectory);
        }

        private static int CountMeshTriangles(Mesh mesh)
        {
            long indices = 0;
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                indices += (long)mesh.GetIndexCount(subMesh);
            return (int)(indices / 3);
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
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
                MaleBaseV11GameplayIntegrationSetup.VisualPrefabPath);
            Avatar avatar = LoadHumanoidAvatar();
            if (modelAsset == null || avatar == null) throw new InvalidOperationException("Selected humanoid model is unavailable.");

            GameObject root = new("HockeyPlayer");
            CharacterController characterController = root.AddComponent<CharacterController>();
            characterController.center = Vector3.zero;
            characterController.height = 2f;
            characterController.radius = 0.45f;

            GameObject visualRoot = NewChild("Visual", root.transform);
            GameObject visual = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (visual == null) throw new InvalidOperationException("Unable to instantiate selected humanoid model.");
            visual.name = "Male_Base_v1_1_Clean_Visual";
            visual.transform.SetParent(visualRoot.transform, false);
            Animator animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            OptimizeRenderers(visual, skinMaterial);
            animator.Rebind();
            animator.Play("Idle", 0, 0f);
            animator.Update(0f);
            MaleBaseV11GameplayIntegrationSetup.AlignVisualToGameplayIce(root, visualRoot.transform, animator);
            // Alignment is sampled from the evaluated Idle pose, but generated prefabs must retain the
            // clean FBX bind transforms rather than serializing an Animator-evaluated bone pose.
            animator.Rebind();

            RigBuilder rigBuilder = visual.AddComponent<RigBuilder>();
            // Keep gameplay presentation helpers outside the animated FBX root. Humanoid evaluation can
            // apply a body-position offset to that root even when root motion is disabled, which would
            // otherwise pull the blade reference away from StickPuckInteraction's control point.
            GameObject presentationRoot = NewChild("StickPresentationRoot", visualRoot.transform);
            GameObject targetsRoot = NewChild("StickRigTargets", presentationRoot.transform);
            Transform leftTarget = NewTarget("LeftHandTarget", targetsRoot.transform, Vector3.zero);
            Transform rightTarget = NewTarget("RightHandTarget", targetsRoot.transform, PrimaryGripPose);
            Transform leftHint = NewTarget("LeftElbowHint", targetsRoot.transform, LeftElbowHintPose);
            Transform rightHint = NewTarget("RightElbowHint", targetsRoot.transform, RightElbowHintPose);
            Transform shaftEndReference = NewTarget("ShaftEndReference", targetsRoot.transform, Vector3.zero);
            Transform bladeReference = NewTarget("BladeReference", targetsRoot.transform, BladePose);
            GameObject productionStick = BuildStick(rightTarget.localPosition, bladeReference.localPosition);
            Transform productionPrimaryGrip = productionStick.transform.Find(
                "Production Hockey Stick/Hockey_Stick_Base_v1/PrimaryGrip");
            Transform productionSecondaryGrip = productionStick.transform.Find(
                "Production Hockey Stick/Hockey_Stick_Base_v1/SecondaryGrip");
            if (productionPrimaryGrip == null || productionSecondaryGrip == null)
                throw new InvalidOperationException("Production stick is missing its authored grip markers.");
            // Keep the blade on the established gameplay control point and place the
            // presentation-only hand targets on the authored grips.
            leftTarget.localPosition = productionStick.transform.InverseTransformPoint(productionSecondaryGrip.position);
            Vector3 primaryLocal = productionStick.transform.InverseTransformPoint(productionPrimaryGrip.position);
            shaftEndReference.localPosition = primaryLocal
                + (leftTarget.localPosition - primaryLocal) * ProductionShaftExtension;

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
            Vector3 leftSkatePosition = visualRoot.transform.InverseTransformPoint(leftFoot.position);
            Vector3 rightSkatePosition = visualRoot.transform.InverseTransformPoint(rightFoot.position);
            float skateCenterY = (GameplayIceY - GameplaySpawnY) / GameplaySkaterScale
                - visualRoot.transform.localPosition.y + 0.035f;
            leftSkatePosition.y = skateCenterY;
            rightSkatePosition.y = skateCenterY;
            HockeyEquipmentBinding[] bindings = new HockeyEquipmentBinding[ExpectedEquipmentSlotCount];
            bindings[0] = CreateBinding(HockeyEquipmentSlot.Helmet, head, "HelmetSlot",
                BuildSinglePrimitive("Helmet", PrimitiveType.Sphere, equipmentMaterial, new Vector3(0f, 0.1f, 0f), new Vector3(0.25f, 0.19f, 0.28f)));
            bindings[1] = CreateBinding(HockeyEquipmentSlot.ShoulderPads, chest, "ShoulderPadsSlot",
                BuildShoulderPads(equipmentMaterial));
            bindings[2] = CreateBinding(HockeyEquipmentSlot.Jersey, chest, "JerseySlot",
                BuildSinglePrimitive("Jersey", PrimitiveType.Cube, jerseyMaterial, Vector3.zero, new Vector3(0.42f, 0.34f, 0.2f)));
            bindings[3] = CreateBinding(HockeyEquipmentSlot.Gloves, visualRoot.transform, "GlovesSlot",
                BuildFollowedPair("Gloves", PrimitiveType.Sphere, equipmentMaterial, leftTarget.localPosition,
                    rightTarget.localPosition, new Vector3(0.13f, 0.11f, 0.14f)));
            bindings[4] = CreateBinding(HockeyEquipmentSlot.Pants, hips, "PantsSlot",
                BuildSinglePrimitive("Pants", PrimitiveType.Cube, equipmentMaterial, Vector3.zero, new Vector3(0.38f, 0.25f, 0.22f)));
            bindings[5] = CreateBinding(HockeyEquipmentSlot.Socks, visualRoot.transform, "SocksSlot",
                BuildFollowedPair("Socks", PrimitiveType.Cube, jerseyMaterial, new Vector3(-0.13f, 0.35f, 0.01f),
                    new Vector3(0.13f, 0.35f, 0.01f), new Vector3(0.13f, 0.28f, 0.13f)));
            bindings[6] = CreateBinding(HockeyEquipmentSlot.Skates, visualRoot.transform, "SkatesSlot",
                BuildFollowedPair("Skates", PrimitiveType.Cube, equipmentMaterial, leftSkatePosition,
                    rightSkatePosition, new Vector3(0.11f, 0.07f, 0.27f)));
            bindings[7] = CreateBinding(HockeyEquipmentSlot.Stick, presentationRoot.transform, "StickSlot",
                productionStick);
            AlignStickPresentationToGameplayControl(root.transform, presentationRoot.transform, bladeReference);
            HockeyEquipmentLoadout loadout = root.AddComponent<HockeyEquipmentLoadout>();
            loadout.Configure(bindings, stickRig, leftHand, rightHand, leftFoot, rightFoot);
            HockeyCharacterPresentation presentation = root.AddComponent<HockeyCharacterPresentation>();
            presentation.Configure(animator, loadout);
            return root;
        }

        private static void AlignStickPresentationToGameplayControl(Transform gameplayRoot,
            Transform presentationRoot, Transform bladeReference)
        {
            // StickPuckInteraction's world-space forward offset is intentionally not scaled with the actor.
            // Pre-compensate the presentation child for the existing uniform runtime skater scale.
            Vector3 gameplayControlPoint = gameplayRoot.TransformPoint(
                (Vector3.forward * GameplayControlForwardOffset + Vector3.up * GameplayControlVerticalOffset)
                / GameplaySkaterScale);
            presentationRoot.position += gameplayControlPoint - bladeReference.position;
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

        private static GameObject BuildStick(Vector3 grip, Vector3 blade)
        {
            GameObject item = new("Stick");
            Transform shaftMarker = NewTarget("Stick Shaft", item.transform, Vector3.zero);

            NewTarget("Stick Grip", item.transform, grip);
            NewTarget("Stick Blade", item.transform, blade);

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(StickPrefabPath);
            if (modelAsset == null) throw new InvalidOperationException("Unable to load the production hockey stick prefab.");
            GameObject modelFit = NewChild("Production Hockey Stick", item.transform);
            GameObject model = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (model == null) throw new InvalidOperationException("Unable to instantiate the production hockey stick prefab.");
            model.name = "Hockey_Stick_Base_v1";
            model.transform.SetParent(modelFit.transform, false);
            FitProductionStick(modelFit.transform, model.transform, grip, blade);
            Transform primaryGrip = model.transform.Find("PrimaryGrip");
            Transform secondaryGrip = model.transform.Find("SecondaryGrip");
            if (primaryGrip == null || secondaryGrip == null)
                throw new InvalidOperationException("Production stick is missing its authored grip markers.");
            Vector3 primaryLocal = item.transform.InverseTransformPoint(primaryGrip.position);
            Vector3 secondaryLocal = item.transform.InverseTransformPoint(secondaryGrip.position);
            Vector3 shaftEnd = primaryLocal + (secondaryLocal - primaryLocal) * ProductionShaftExtension;
            Vector3 direction = shaftEnd - primaryLocal;
            shaftMarker.localPosition = (primaryLocal + shaftEnd) * 0.5f;
            shaftMarker.localScale = new Vector3(0.035f, direction.magnitude, 0.035f);
            shaftMarker.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            Renderer[] renderers = modelFit.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Production hockey stick prefab has no renderers.");
            Bounds renderedBounds = renderers[0].bounds;
            for (int i = 0; i < renderers.Length; i++)
            {
                ApplyArenaStickRendererPolicy(renderers[i]);
                if (i > 0) renderedBounds.Encapsulate(renderers[i].bounds);
            }
            float expectedLength = Vector3.Distance(grip, blade);
            if (renderedBounds.size.magnitude < expectedLength * 0.75f
                || renderedBounds.size.magnitude > expectedLength * 2f)
                throw new InvalidOperationException($"Production stick rendered bounds are invalid: {renderedBounds.size}.");
            return item;
        }

        private static void FitProductionStick(Transform fittedRoot, Transform productionRoot,
            Vector3 targetGrip, Vector3 targetBlade)
        {
            Transform primaryGrip = productionRoot.Find("PrimaryGrip");
            Transform bladeContact = productionRoot.Find("BladeContact");
            if (primaryGrip == null || bladeContact == null)
                throw new InvalidOperationException("Production stick is missing PrimaryGrip or BladeContact.");

            Vector3 sourceGrip = fittedRoot.InverseTransformPoint(primaryGrip.position);
            Vector3 sourceBlade = fittedRoot.InverseTransformPoint(bladeContact.position);
            Vector3 sourceDirection = sourceBlade - sourceGrip;
            Vector3 targetDirection = targetBlade - targetGrip;
            if (sourceDirection.sqrMagnitude < 0.000001f || targetDirection.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException("Production stick alignment endpoints are invalid.");

            Transform secondaryGrip = productionRoot.Find("SecondaryGrip");
            if (secondaryGrip == null)
                throw new InvalidOperationException("Production stick is missing SecondaryGrip.");
            Vector3 sourceSecondary = fittedRoot.InverseTransformPoint(secondaryGrip.position);
            Vector3 sourceFace = Vector3.ProjectOnPlane(sourceSecondary - sourceGrip, sourceDirection).normalized;
            Vector3 targetFace = Vector3.ProjectOnPlane(
                SecondaryGripPoseHint - targetGrip, targetDirection).normalized;
            if (sourceFace.sqrMagnitude < 0.000001f || targetFace.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException("Production stick secondary-grip orientation is invalid.");

            float scale = targetDirection.magnitude / sourceDirection.magnitude;
            Quaternion sourceBasis = Quaternion.LookRotation(sourceDirection.normalized, sourceFace);
            Quaternion targetBasis = Quaternion.LookRotation(targetDirection.normalized, targetFace);
            Quaternion rotation = targetBasis * Quaternion.Inverse(sourceBasis);
            fittedRoot.localScale = Vector3.one * scale;
            fittedRoot.localRotation = rotation;
            fittedRoot.localPosition = targetGrip - rotation * (sourceGrip * scale);
        }

        private static void ApplyArenaStickRendererPolicy(Renderer renderer)
        {
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.receiveShadows = false;
            renderer.allowOcclusionWhenDynamic = false;
            SerializedObject serializedRenderer = new(renderer);
            SerializedProperty smallMeshCulling = serializedRenderer.FindProperty("m_SmallMeshCulling");
            if (smallMeshCulling == null)
                throw new InvalidOperationException("This Unity version does not expose the required small-mesh culling policy.");
            smallMeshCulling.boolValue = false;
            serializedRenderer.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool HasSmallMeshCullingDisabled(Renderer renderer)
        {
            SerializedProperty property = new SerializedObject(renderer).FindProperty("m_SmallMeshCulling");
            return property != null && !property.boolValue;
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
