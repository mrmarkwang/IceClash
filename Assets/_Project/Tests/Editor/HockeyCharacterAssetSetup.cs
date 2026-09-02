/*
 * IceClash modular clean-humanoid asset generator and validator.
 * Consumes the isolated Male_Base_v1_1_Clean production visual/controller,
 * applies the existing mobile presentation policy,
 * builds four replaceable wearables plus the gameplay stick, uses the validated
 * Skate_Base_v1 left/right prefabs for every generated gameplay skater, parents the production stick
 * through RightHand/StickSocket, aligns its explicit grip/contact markers in a
 * right-handed two-hand hockey stance, binds LeftHand IK to SecondaryGrip, and
 * matches skate scale once to the clean character visual in gameplay, and creates shared
 * presentation assets, prefabs, IK, validation, and evidence
 * without changing gameplay systems or shipping debug geometry.
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
using UnityEngine.SceneManagement;

namespace IceClash.Tests.Editor
{
    [InitializeOnLoad]
    public static class HockeyCharacterAssetSetup
    {
        private const string ModelPath = MaleBaseV11GameplayIntegrationSetup.CleanModelPath;
        private const string StickDirectory = "Assets/Equipment/Sticks/Hockey_Stick_Base_v1";
        private const string StickModelPath = StickDirectory + "/Meshy_Hockey_Stick_Base_v1.fbx";
        private const string StickPrefabPath = StickDirectory + "/Hockey_Stick_Base_v1.prefab";
        private const string StickMaterialPath = StickDirectory + "/Hockey_Stick_Base_v1.mat";
        private const string SkateDirectory = "Assets/Equipment/Skates/Skate_Base_v1";
        private const string LeftSkatePrefabPath = SkateDirectory + "/Prefabs/Skate_L_v1.prefab";
        private const string RightSkatePrefabPath = SkateDirectory + "/Prefabs/Skate_R_v1.prefab";
        private const string SkateMaterialPath = SkateDirectory + "/Materials/Skate_Base_v1.mat";
        private const string GeneratedDirectory = "Assets/_Project/Art/HockeyPrototype";
        private const string FootMaskedMeshPath = GeneratedDirectory + "/Male_Base_v1_1_Clean_SkateMasked.asset";
        private const string PrefabPath = "Assets/_Project/Prefabs/HockeyPlayer.prefab";
        private const string ResourcePrefabPath = "Assets/_Project/Prefabs/Resources/Skater.prefab";
        private const string ScenePath = "Assets/_Project/Scenes/ModularCharacterTest.unity";
        private const string ControllerPath = MaleBaseV11GameplayIntegrationSetup.ControllerPath;
        private const string AutoGenerationKey = "IceClash.ModularCharacterGenerationRunning";
        private const string GameplayEvidencePath = ".docs/evidence/use-production-stick-in-gameplay/prototype-arena-production-stick.png";
        private const int ExpectedEquipmentSlotCount = 5;
        private const float GameplayControlForwardOffset = 1.15f;
        private const float GameplayControlVerticalOffset = 0.28f;
        private const float GameplaySkaterScale = 0.68f;
        private const float GameplayIceY = 0.2f;
        // CharacterController settles the prototype actor 0.04 m below its requested
        // spawn transform before presentation verification.
        private const float GameplaySpawnY = 0.96f;
        private const float ProductionShaftExtension = 3.05f;
        // Right-handed ready stance in StickPresentationRoot space.
        private static readonly Vector3 PrimaryGripPose = new(0.15f, 0.72f, 0.15f);
        private static readonly Vector3 LeftElbowHintPose = new(-0.55f, 0.70f, 0.25f);
        private static readonly Vector3 RightElbowHintPose = new(0.50f, 0.78f, 0.15f);
        private static readonly Vector3 BladePose = new(0.18f, 0.25f, 1.55f);
        private static readonly Vector3 RightPalmSocketOffset = new(0.09f, 0f, 0f);
        private static readonly Vector3 LeftPalmGripOffset = new(0.07f, 0f, 0f);

        static HockeyCharacterAssetSetup()
        {
            EditorApplication.delayCall += EnsureGenerated;
        }

        [MenuItem("IceClash/Generate Modular Hockey Character")]
        public static void GenerateAll()
        {
            EnsureFolder(GeneratedDirectory);
            MaleBaseV11GameplayIntegrationSetup.GenerateProductionAssets();
            ConfigureStickAssets();
            ConfigureSkateAssets();
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

        public static void GenerateAndValidateProductionSkatesBatch()
        {
            try
            {
                GenerateAll();
                GameObject canonical = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)
                    ?? throw new InvalidOperationException("Generated HockeyPlayer prefab is missing.");
                GameObject resource = AssetDatabase.LoadAssetAtPath<GameObject>(ResourcePrefabPath)
                    ?? throw new InvalidOperationException("Generated Resources/Skater prefab is missing.");
                if (PrefabUtility.GetPrefabAssetType(resource) != PrefabAssetType.Variant
                    || PrefabUtility.GetCorrespondingObjectFromOriginalSource(resource) != canonical)
                    throw new InvalidOperationException("Resources/Skater is not connected to HockeyPlayer.prefab.");
                ValidateSupportedEquipmentStructure(canonical, "HockeyPlayer.prefab");
                ValidateSupportedEquipmentStructure(resource, "Resources/Skater.prefab");
                ValidateProductionSkates(canonical, "HockeyPlayer.prefab");
                ValidateProductionSkates(resource, "Resources/Skater.prefab");
                ValidateSkateMaskRoundTrip(canonical);
                ValidateGeneratedSceneEquipment();
                Debug.Log("GAMEPLAY_SKATES_ASSETS_PASS canonical=true resourceVariant=true generatedPlayers=10 productionPairs=10");
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
            ValidateEquipmentEnumValues();
            if (loadout == null || !loadout.IsComplete() || loadout.SlotCount != ExpectedEquipmentSlotCount)
                throw new InvalidOperationException("HockeyPlayer does not contain the four wearables plus Stick.");
            if (!HasActiveEquipment(loadout))
                throw new InvalidOperationException("HockeyPlayer does not have one active item in every equipment slot.");
            ValidateSupportedEquipmentStructure(canonical, "HockeyPlayer.prefab");
            ValidateSupportedEquipmentStructure(resource, "Resources/Skater.prefab");
            ValidateProductionSkates(canonical, "HockeyPlayer.prefab");
            ValidateProductionSkates(resource, "Resources/Skater.prefab");
            ValidateGeneratedSceneEquipment();
            Debug.Log("SUPPORTED_EQUIPMENT_STRUCTURE_PASS slots=Helmet,Visor,Gloves,Skates,Stick productionSkates=true players=10");
            if (stickRig == null || !stickRig.HasValidReferences || rigBuilder == null || rigBuilder.layers.Count != 2)
                throw new InvalidOperationException("HockeyPlayer two-hand IK rig is incomplete.");
            if (stickRig.LeftHandConstraint.transform == stickRig.RightHandConstraint.transform
                || stickRig.LeftHandTarget == stickRig.RightHandTarget
                || stickRig.LeftHandConstraint.data.target != stickRig.LeftHandTarget
                || stickRig.RightHandConstraint.data.target != stickRig.RightHandTarget)
                throw new InvalidOperationException("Left and right hand IK constraints are not independent.");
            if (!MatchesProfessionalPose(loadout, stickRig))
                throw new InvalidOperationException("HockeyPlayer professional grip targets or elbow hints are stale.");
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
            GameObject equippedStick = loadout.GetEquipped(HockeyEquipmentSlot.Stick);
            Transform productionRoot = equippedStick != null ? equippedStick.transform : null;
            Transform importedStick = productionRoot;
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
                || !prefabDependencies.Contains(StickPrefabPath) || !resourceDependencies.Contains(StickPrefabPath))
                throw new InvalidOperationException("Production hockey stick integration or arena renderer policy is invalid.");
            Bounds stickBounds = stickRenderers[0].bounds;
            for (int i = 1; i < stickRenderers.Length; i++) stickBounds.Encapsulate(stickRenderers[i].bounds);
            float fittedLength = Vector3.Distance(gripMarker.position, visualBlade.position);
            if (Vector3.Distance(stickBounds.ClosestPoint(gripMarker.position), gripMarker.position) > fittedLength * 0.15f
                || Vector3.Distance(stickBounds.ClosestPoint(visualBlade.position), visualBlade.position) > fittedLength * 0.15f)
                throw new InvalidOperationException("Production hockey stick no longer reaches its grip and blade markers.");
            Transform socket = productionRoot != null ? productionRoot.parent : null;
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            float handSeparation = Vector3.Distance(primaryGrip.localPosition, secondaryGrip.localPosition)
                * productionRoot.localScale.x;
            Vector3 expectedTargetPrimary = stickRig.RightHandTarget.localPosition
                + stickRig.RightHandTarget.localRotation * RightPalmSocketOffset;
            if (socket == null || socket.name != "StickSocket" || socket.parent != rightHand
                || socket.childCount != 1 || socket.GetChild(0) != productionRoot
                || Vector3.Distance(expectedTargetPrimary, PrimaryGripPose) > 0.001f
                || stickRig.EquippedSecondaryGrip != secondaryGrip
                || stickRig.LeftHandTarget == null || stickRig.LeftHandTarget.IsChildOf(productionRoot)
                || Vector3.Distance(bladeContact.localPosition, visualBlade.localPosition) > 0.001f
                || handSeparation < 0.30f || handSeparation > 0.45f
                || !secondaryGrip.IsChildOf(productionRoot))
                throw new InvalidOperationException("Production stick shaft grips are not aligned to the two-hand IK targets.");
            Vector3 gripToBlade = BladePose - PrimaryGripPose;
            if (PrimaryGripPose.y - BladePose.y < 0.8f
                || gripToBlade.z < 0.9f
                || BladePose.x <= 0f
                || stickRig.LeftHandConstraint.data.hint == null
                || stickRig.LeftHandConstraint.data.hintWeight < 0.99f
                || stickRig.LeftHandConstraint.data.maintainTargetRotationOffset
                || stickRig.RightHandConstraint.data.maintainTargetRotationOffset)
                throw new InvalidOperationException("Production stick no longer forms the required professional two-hand carry.");

            ValidateEditorEquipmentPersistence();
            ValidateRendererPolicy(canonical);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("ModularCharacterTest scene is missing.");
            Debug.Log("MODULAR_CHARACTER_ASSETS_VALID");
        }

        [MenuItem("IceClash/Validate Supported Equipment Contract")]
        public static void ValidateSupportedEquipmentContract()
        {
            GameObject canonical = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            GameObject resource = AssetDatabase.LoadAssetAtPath<GameObject>(ResourcePrefabPath);
            if (canonical == null || resource == null)
                throw new InvalidOperationException("Generated hockey-player prefabs are missing.");
            ValidateEquipmentEnumValues();
            ValidateSupportedEquipmentStructure(canonical, "HockeyPlayer.prefab");
            ValidateSupportedEquipmentStructure(resource, "Resources/Skater.prefab");
            ValidateProductionSkates(canonical, "HockeyPlayer.prefab");
            ValidateProductionSkates(resource, "Resources/Skater.prefab");
            ValidateGeneratedSceneEquipment();
            ValidateEditorEquipmentPersistence();
            Debug.Log("SUPPORTED_EQUIPMENT_CONTRACT_PASS slots=Helmet,Visor,Gloves,Skates,Stick persistence=true players=10");
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
                && loadout.GetEquipped(HockeyEquipmentSlot.Stick)?.name == "Hockey_Stick_Base_v1"
                && MatchesProfessionalPose(loadout, stickRig)
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
                throw new InvalidOperationException("Humanoid auto-mapping failed for the clean production model.");
        }

        private static Avatar LoadHumanoidAvatar()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            Animator animator = model != null ? model.GetComponent<Animator>() : null;
            if (animator != null && animator.avatar != null) return animator.avatar;
            return AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
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

        private static void ConfigureSkateAssets()
        {
            GameObject left = AssetDatabase.LoadAssetAtPath<GameObject>(LeftSkatePrefabPath);
            GameObject right = AssetDatabase.LoadAssetAtPath<GameObject>(RightSkatePrefabPath);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(SkateMaterialPath);
            if (left == null || right == null || material == null)
                throw new FileNotFoundException("Validated production skate prefabs or material are missing.", SkateDirectory);
            foreach (GameObject skate in new[] { left, right })
            {
                if (skate.transform.Find("Visual") == null || skate.transform.Find("BladeContact") == null
                    || skate.GetComponentsInChildren<Animator>(true).Length != 0
                    || skate.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != 0)
                    throw new InvalidOperationException($"Production skate prefab is not rigid equipment: {skate.name}.");
            }
        }

        private static int CountMeshTriangles(Mesh mesh)
        {
            long indices = 0;
            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                indices += (long)mesh.GetIndexCount(subMesh);
            return (int)(indices / 3);
        }

        private static bool MatchesProfessionalPose(HockeyEquipmentLoadout loadout, HockeyStickRig stickRig)
        {
            if (loadout == null || stickRig == null || !stickRig.HasValidReferences) return false;
            Transform primaryGrip = loadout.FindEquippedChild(HockeyEquipmentSlot.Stick, "PrimaryGrip");
            Transform secondaryGrip = loadout.FindEquippedChild(HockeyEquipmentSlot.Stick, "SecondaryGrip");
            Transform bladeContact = loadout.FindEquippedChild(HockeyEquipmentSlot.Stick, "BladeContact");
            Transform equippedStick = loadout.GetEquipped(HockeyEquipmentSlot.Stick)?.transform;
            Transform socket = equippedStick != null ? equippedStick.parent : null;
            Transform leftHint = stickRig.LeftHandConstraint.data.hint;
            Transform rightHint = stickRig.RightHandConstraint.data.hint;
            Transform targetSpace = stickRig.RightHandTarget.parent;
            Vector3 generatedTargetPrimary = stickRig.RightHandTarget.localPosition
                + stickRig.RightHandTarget.localRotation * RightPalmSocketOffset;
            float lowerGripDistance = primaryGrip != null && secondaryGrip != null && equippedStick != null
                ? Vector3.Distance(primaryGrip.localPosition, secondaryGrip.localPosition) * equippedStick.localScale.x
                : 0f;
            Vector3 expectedSecondary = PrimaryGripPose
                + (BladePose - PrimaryGripPose).normalized * lowerGripDistance;
            Vector3 expectedShaftEnd = PrimaryGripPose
                + (expectedSecondary - PrimaryGripPose) * ProductionShaftExtension;
            bool matches = primaryGrip != null && secondaryGrip != null && bladeContact != null
                && leftHint != null && rightHint != null && targetSpace != null
                && socket != null && socket.name == "StickSocket"
                && socket.parent == loadout.RightHand
                && stickRig.EquippedSecondaryGrip == secondaryGrip
                && !stickRig.LeftHandTarget.IsChildOf(equippedStick)
                && !stickRig.RightHandTarget.IsChildOf(equippedStick)
                && Vector3.Distance(generatedTargetPrimary, PrimaryGripPose) <= 0.001f
                && Vector3.Distance(stickRig.BladeReference.localPosition, BladePose) <= 0.001f
                && Vector3.Distance(leftHint.localPosition, LeftElbowHintPose) <= 0.001f
                && Vector3.Distance(rightHint.localPosition, RightElbowHintPose) <= 0.001f
                && Vector3.Distance(stickRig.ShaftEndReference.localPosition, expectedShaftEnd) <= 0.001f
                && loadout.FindEquippedChild(HockeyEquipmentSlot.Stick, "Stick Blade") != null;
            if (!matches)
                Debug.LogWarning($"HOCKEY_GRIP_POSE_DIAGNOSTIC socket={socket?.name} socketParent={socket?.parent?.name} "
                    + $"rightHand={loadout.RightHand?.name} leftTarget={stickRig.LeftHandTarget?.name} secondary={stickRig.EquippedSecondaryGrip?.name} "
                    + $"leftChild={stickRig.LeftHandTarget != null && equippedStick != null && stickRig.LeftHandTarget.IsChildOf(equippedStick)} "
                    + $"rightChild={stickRig.RightHandTarget != null && equippedStick != null && stickRig.RightHandTarget.IsChildOf(equippedStick)} "
                    + $"targetPrimary={generatedTargetPrimary:F4} expectedPrimary={PrimaryGripPose:F4} "
                    + $"bladeLocal={stickRig.BladeReference?.localPosition:F4} leftHint={leftHint?.localPosition:F4} "
                    + $"rightHint={rightHint?.localPosition:F4} shaftEnd={stickRig.ShaftEndReference?.localPosition:F4} "
                    + $"expectedShaftEnd={expectedShaftEnd:F4}");
            return matches;
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
            Renderer[] characterRenderers = visual.GetComponentsInChildren<Renderer>(true);
            if (characterRenderers.Length == 0)
                throw new InvalidOperationException("Selected humanoid visual has no main-character renderers.");
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
            Transform head = animator.GetBoneTransform(HumanBodyBones.Head) ?? root.transform;
            Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand)
                ?? throw new InvalidOperationException("Humanoid LeftHand mapping is unavailable.");
            Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand)
                ?? throw new InvalidOperationException("Humanoid RightHand mapping is unavailable.");
            Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot) ?? visual.transform;
            Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot) ?? visual.transform;
            SkinnedMeshRenderer characterSkin = characterRenderers.OfType<SkinnedMeshRenderer>().Single();
            Mesh unmaskedCharacterMesh = characterSkin.sharedMesh;
            MaskCharacterFeetForSkates(characterRenderers, leftFoot, rightFoot);

            GameObject presentationRoot = NewChild("StickPresentationRoot", visualRoot.transform);
            GameObject targetsRoot = NewChild("StickRigTargets", presentationRoot.transform);
            Quaternion rightTargetRotation = Quaternion.Inverse(targetsRoot.transform.rotation) * rightHand.rotation;
            Vector3 rightTargetPosition = PrimaryGripPose - rightTargetRotation * RightPalmSocketOffset;
            Transform rightTarget = NewTarget("RightHandGripTarget", targetsRoot.transform,
                rightTargetPosition, rightTargetRotation);
            Transform leftHint = NewTarget("LeftElbowHint", targetsRoot.transform, LeftElbowHintPose);
            Transform rightHint = NewTarget("RightElbowHint", targetsRoot.transform, RightElbowHintPose);
            Transform shaftEndReference = NewTarget("ShaftEndReference", targetsRoot.transform, Vector3.zero);
            Transform bladeReference = NewTarget("BladeReference", targetsRoot.transform, BladePose);
            Vector3 inverseHandScale = new(1f / rightHand.lossyScale.x,
                1f / rightHand.lossyScale.y, 1f / rightHand.lossyScale.z);
            Transform stickSocket = NewTarget("StickSocket", rightHand,
                Vector3.Scale(RightPalmSocketOffset, inverseHandScale));
            stickSocket.localScale = inverseHandScale;
            GameObject productionStick = BuildStick(stickSocket, presentationRoot.transform,
                rightTarget, leftHand.rotation, PrimaryGripPose, BladePose);
            Transform productionPrimaryGrip = productionStick.transform.Find("PrimaryGrip");
            Transform productionSecondaryGrip = productionStick.transform.Find("SecondaryGrip");
            if (productionPrimaryGrip == null || productionSecondaryGrip == null)
                throw new InvalidOperationException("Production stick is missing its authored grip markers.");
            float lowerGripDistance = Vector3.Distance(productionPrimaryGrip.localPosition,
                productionSecondaryGrip.localPosition) * productionStick.transform.localScale.x;
            Vector3 designedSecondary = PrimaryGripPose
                + (BladePose - PrimaryGripPose).normalized * lowerGripDistance;
            Quaternion leftTargetRotation = Quaternion.Inverse(targetsRoot.transform.rotation) * leftHand.rotation;
            Transform leftTarget = NewTarget("LeftHandIKTarget", targetsRoot.transform,
                designedSecondary - leftTargetRotation * LeftPalmGripOffset, leftTargetRotation);
            shaftEndReference.localPosition = PrimaryGripPose
                + (designedSecondary - PrimaryGripPose) * ProductionShaftExtension;

            GameObject rigObject = NewChild("HockeyStickRigConstraints", visual.transform);
            Rig rightRig = NewChild("RightHandRig", rigObject.transform).AddComponent<Rig>();
            Rig leftRig = NewChild("LeftHandRig", rigObject.transform).AddComponent<Rig>();
            TwoBoneIKConstraint rightConstraint = CreateArmConstraint("RightHandIK", rightRig.transform, animator,
                HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, rightTarget, rightHint);
            TwoBoneIKConstraint leftConstraint = CreateArmConstraint("LeftHandIK", leftRig.transform, animator,
                HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, leftTarget, leftHint);
            // Rig-layer order is intentional: the right hand moves StickSocket first,
            // then the left layer resolves SecondaryGrip from the updated stick pose.
            rigBuilder.layers.Add(new RigLayer(rightRig));
            rigBuilder.layers.Add(new RigLayer(leftRig));

            HockeyStickRig stickRig = root.AddComponent<HockeyStickRig>();
            stickRig.Configure(leftConstraint, rightConstraint, leftTarget, rightTarget, leftHint, rightHint,
                shaftEndReference, bladeReference);

            Vector3 leftSkatePosition = visualRoot.transform.InverseTransformPoint(leftFoot.position);
            Vector3 rightSkatePosition = visualRoot.transform.InverseTransformPoint(rightFoot.position);
            float skateContactY = (GameplayIceY - GameplaySpawnY) / GameplaySkaterScale
                - visualRoot.transform.localPosition.y;
            // Match the clean character visual exactly once. The prior 1.90 fitting
            // multiplier compounded this 1.65 scale into 3.135-sized gameplay boots.
            float skateScale = visual.transform.localScale.x;
            float skateForwardFit = 0.07f * skateScale;
            leftSkatePosition = new Vector3(leftSkatePosition.x, skateContactY,
                leftSkatePosition.z + skateForwardFit);
            rightSkatePosition = new Vector3(rightSkatePosition.x, skateContactY,
                rightSkatePosition.z + skateForwardFit);
            HockeyEquipmentBinding[] bindings = new HockeyEquipmentBinding[ExpectedEquipmentSlotCount];
            bindings[0] = CreateBinding(HockeyEquipmentSlot.Helmet, head, "HelmetSlot",
                BuildSinglePrimitive("Helmet", PrimitiveType.Sphere, equipmentMaterial, new Vector3(0f, 0.1f, 0f), new Vector3(0.25f, 0.19f, 0.28f)));
            bindings[1] = CreateBinding(HockeyEquipmentSlot.Visor, head, "VisorSlot",
                BuildSinglePrimitive("Visor", PrimitiveType.Cube, equipmentMaterial,
                    new Vector3(0f, 0.03f, 0.24f), new Vector3(0.22f, 0.08f, 0.025f)));
            bindings[2] = CreateBinding(HockeyEquipmentSlot.Gloves, visualRoot.transform, "GlovesSlot",
                BuildFollowedPair("Gloves", PrimitiveType.Sphere, equipmentMaterial, leftTarget.localPosition,
                    rightTarget.localPosition, new Vector3(0.13f, 0.11f, 0.14f)));
            bindings[3] = CreateBinding(HockeyEquipmentSlot.Skates, visualRoot.transform, "SkatesSlot",
                BuildProductionSkatePair(leftSkatePosition, rightSkatePosition, skateScale));
            HockeyEquipmentBinding stickBinding = new();
            stickBinding.Configure(HockeyEquipmentSlot.Stick, stickSocket, productionStick);
            bindings[4] = stickBinding;
            AlignStickPresentationToGameplayControl(root.transform, presentationRoot.transform, bladeReference);
            HockeyEquipmentLoadout loadout = root.AddComponent<HockeyEquipmentLoadout>();
            animator.Play("Idle", 0, 0f);
            animator.Update(0f);
            HockeyPairedEquipmentFollower skateFollower = bindings[3].Equipped
                .GetComponent<HockeyPairedEquipmentFollower>()
                ?? throw new InvalidOperationException("Generated skate follower is missing.");
            AlignGameplaySkateToFoot(skateFollower.FirstVisual, leftFoot, root.transform);
            AlignGameplaySkateToFoot(skateFollower.SecondVisual, rightFoot, root.transform);
            loadout.Configure(bindings, stickRig, leftHand, rightHand, leftFoot, rightFoot);
            loadout.ConfigureSkateMask(characterSkin, unmaskedCharacterMesh, characterSkin.sharedMesh);
            animator.Rebind();
            HockeyCharacterPresentation presentation = root.AddComponent<HockeyCharacterPresentation>();
            presentation.Configure(animator, loadout, characterRenderers);
            presentation.SetTeamMaterial(jerseyMaterial);
            return root;
        }

        private static void AlignGameplaySkateToFoot(Transform skate, Transform foot, Transform actorRoot)
        {
            Transform contact = skate != null ? skate.Find("BladeContact") : null;
            if (skate == null || foot == null || contact == null)
                throw new InvalidOperationException("Gameplay skate alignment requires a foot and BladeContact.");

            skate.rotation = actorRoot.rotation;
            // The validated fitting places the skate pivot slightly ahead of the
            // ankle so the rear cuff, rather than the mesh center, seats under it.
            Vector3 position = foot.position + actorRoot.forward * 0.07f;
            position.y = skate.position.y;
            skate.position = position;
            float localContactY = (GameplayIceY - GameplaySpawnY) / GameplaySkaterScale;
            float targetContactY = actorRoot.TransformPoint(Vector3.up * localContactY).y;
            skate.position += actorRoot.up * (targetContactY - contact.position.y);
        }

        private static void MaskCharacterFeetForSkates(Renderer[] renderers,
            Transform leftFoot, Transform rightFoot)
        {
            SkinnedMeshRenderer skin = renderers.OfType<SkinnedMeshRenderer>().SingleOrDefault()
                ?? throw new InvalidOperationException("Skate foot masking requires one character skin renderer.");
            Mesh source = skin.sharedMesh
                ?? throw new InvalidOperationException("Skate foot masking requires a source character mesh.");
            Transform[] bones = skin.bones;
            HashSet<int> footBones = new();
            for (int index = 0; index < bones.Length; index++)
                if (bones[index] == leftFoot || bones[index] == rightFoot
                    || bones[index].IsChildOf(leftFoot) || bones[index].IsChildOf(rightFoot))
                    footBones.Add(index);
            if (footBones.Count == 0)
                throw new InvalidOperationException("Character mesh has no mapped foot bones to mask.");

            BoneWeight[] weights = source.boneWeights;
            Vector3[] vertices = source.vertices;
            bool[] maskedVertex = new bool[weights.Length];
            for (int vertex = 0; vertex < weights.Length; vertex++)
            {
                BoneWeight weight = weights[vertex];
                float footWeight = 0f;
                if (footBones.Contains(weight.boneIndex0)) footWeight += weight.weight0;
                if (footBones.Contains(weight.boneIndex1)) footWeight += weight.weight1;
                if (footBones.Contains(weight.boneIndex2)) footWeight += weight.weight2;
                if (footBones.Contains(weight.boneIndex3)) footWeight += weight.weight3;
                // Also clip the bind-pose foot volume below the production boot
                // opening. The broad placeholder feet include lower-leg-weighted
                // triangles that a bone-only mask cannot catch.
                maskedVertex[vertex] = footWeight >= 0.15f || vertices[vertex].y <= 0.18f;
            }

            Mesh masked = UnityEngine.Object.Instantiate(source);
            masked.name = "Male_Base_v1_1_Clean_SkateMasked";
            int removedTriangles = 0;
            for (int subMesh = 0; subMesh < source.subMeshCount; subMesh++)
            {
                int[] triangles = source.GetTriangles(subMesh);
                List<int> kept = new(triangles.Length);
                for (int triangle = 0; triangle < triangles.Length; triangle += 3)
                {
                    bool remove = maskedVertex[triangles[triangle]]
                        || maskedVertex[triangles[triangle + 1]]
                        || maskedVertex[triangles[triangle + 2]];
                    if (remove) removedTriangles++;
                    else
                    {
                        kept.Add(triangles[triangle]);
                        kept.Add(triangles[triangle + 1]);
                        kept.Add(triangles[triangle + 2]);
                    }
                }
                masked.SetTriangles(kept, subMesh, true);
            }
            if (removedTriangles < 20)
                throw new InvalidOperationException(
                    $"Character skate mask removed too little foot geometry ({removedTriangles} triangles).");

            Mesh asset = AssetDatabase.LoadAssetAtPath<Mesh>(FootMaskedMeshPath);
            if (asset == null)
            {
                AssetDatabase.CreateAsset(masked, FootMaskedMeshPath);
                asset = masked;
            }
            else
            {
                EditorUtility.CopySerialized(masked, asset);
                UnityEngine.Object.DestroyImmediate(masked);
                EditorUtility.SetDirty(asset);
            }
            skin.sharedMesh = asset;
            Debug.Log($"GAMEPLAY_SKATES_FOOT_MASK_PASS removedTriangles={removedTriangles}");
        }

        private static void AlignStickPresentationToGameplayControl(Transform gameplayRoot,
            Transform presentationRoot, Transform bladeReference)
        {
            // StickPuckInteraction's world-space forward offset is intentionally not scaled with the actor.
            // Pre-compensate the presentation child for the existing uniform runtime skater scale.
            Vector3 gameplayControlPoint = gameplayRoot.TransformPoint(
                (Vector3.forward * GameplayControlForwardOffset + Vector3.up * GameplayControlVerticalOffset)
                / GameplaySkaterScale);
            Vector3 correction = gameplayControlPoint - bladeReference.position;
            correction.x = 0f;
            presentationRoot.position += correction;
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
            data.hintWeight = 1f;
            data.maintainTargetPositionOffset = false;
            data.maintainTargetRotationOffset = false;
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

        private static GameObject BuildProductionSkatePair(Vector3 leftPosition,
            Vector3 rightPosition, float uniformScale)
        {
            GameObject leftAsset = AssetDatabase.LoadAssetAtPath<GameObject>(LeftSkatePrefabPath)
                ?? throw new FileNotFoundException("Validated left skate prefab is missing.", LeftSkatePrefabPath);
            GameObject rightAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RightSkatePrefabPath)
                ?? throw new FileNotFoundException("Validated right skate prefab is missing.", RightSkatePrefabPath);
            if (uniformScale <= 0f) throw new InvalidOperationException("Gameplay skate scale must be positive.");

            GameObject item = new("Skates");
            GameObject left = PrefabUtility.InstantiatePrefab(leftAsset) as GameObject
                ?? throw new InvalidOperationException("Unable to instantiate the validated left skate prefab.");
            GameObject right = PrefabUtility.InstantiatePrefab(rightAsset) as GameObject
                ?? throw new InvalidOperationException("Unable to instantiate the validated right skate prefab.");
            left.name = "Skate_L_v1";
            right.name = "Skate_R_v1";
            left.transform.SetParent(item.transform, false);
            right.transform.SetParent(item.transform, false);
            left.transform.SetLocalPositionAndRotation(leftPosition, Quaternion.identity);
            right.transform.SetLocalPositionAndRotation(rightPosition, Quaternion.identity);
            left.transform.localScale = Vector3.one * uniformScale;
            right.transform.localScale = Vector3.one * uniformScale;
            HockeyPairedEquipmentFollower follower = item.AddComponent<HockeyPairedEquipmentFollower>();
            follower.ConfigureVisuals(left.transform, right.transform);
            return item;
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root.name == childName) return root;
            for (int index = 0; index < root.childCount; index++)
            {
                Transform found = FindChildRecursive(root.GetChild(index), childName);
                if (found != null) return found;
            }
            return null;
        }

        private static GameObject BuildStick(Transform socket, Transform poseSpace, Transform rightTarget,
            Quaternion leftHandWorldRotation, Vector3 targetGrip, Vector3 targetBlade)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(StickPrefabPath);
            if (modelAsset == null) throw new InvalidOperationException("Unable to load the production hockey stick prefab.");
            GameObject model = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
            if (model == null) throw new InvalidOperationException("Unable to instantiate the production hockey stick prefab.");
            model.name = "Hockey_Stick_Base_v1";
            model.transform.SetParent(socket, false);
            Transform primaryGrip = model.transform.Find("PrimaryGrip");
            Transform secondaryGrip = model.transform.Find("SecondaryGrip");
            Transform bladeContact = model.transform.Find("BladeContact");
            if (primaryGrip == null || secondaryGrip == null || bladeContact == null)
                throw new InvalidOperationException("Production stick is missing its authored grip markers.");

            Vector3 sourceGrip = primaryGrip.localPosition;
            Vector3 sourceBlade = bladeContact.localPosition;
            Vector3 sourceDirection = sourceBlade - sourceGrip;
            Vector3 targetDirection = targetBlade - targetGrip;
            if (sourceDirection.sqrMagnitude < 0.000001f || targetDirection.sqrMagnitude < 0.000001f)
                throw new InvalidOperationException("Production stick alignment endpoints are invalid.");
            Vector3 sourceFace = Vector3.ProjectOnPlane(Vector3.forward, sourceDirection).normalized;
            Vector3 targetDirectionWorld = poseSpace.TransformDirection(targetDirection.normalized);
            Vector3 targetFaceWorld = Vector3.ProjectOnPlane(
                poseSpace.TransformDirection(Vector3.right), targetDirectionWorld).normalized;
            Quaternion sourceBasis = Quaternion.LookRotation(sourceDirection.normalized, sourceFace);
            Quaternion targetBasis = Quaternion.LookRotation(targetDirectionWorld, targetFaceWorld);
            Quaternion desiredStickWorldRotation = targetBasis * Quaternion.Inverse(sourceBasis);
            float scale = targetDirection.magnitude / sourceDirection.magnitude;

            socket.localRotation = Quaternion.Inverse(rightTarget.rotation) * desiredStickWorldRotation;
            model.transform.localScale = Vector3.one * scale;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localPosition = -sourceGrip * scale;
            secondaryGrip.rotation = leftHandWorldRotation;

            Transform gripMarker = NewTarget("Stick Grip", model.transform, sourceGrip);
            Transform bladeMarker = NewTarget("Stick Blade", model.transform, sourceBlade);
            Vector3 shaftEnd = sourceGrip + (secondaryGrip.localPosition - sourceGrip) * ProductionShaftExtension;
            Transform shaftMarker = NewTarget("Stick Shaft", model.transform, (sourceGrip + shaftEnd) * 0.5f);
            shaftMarker.localScale = new Vector3(0.035f, Vector3.Distance(sourceGrip, shaftEnd), 0.035f);
            shaftMarker.localRotation = Quaternion.FromToRotation(Vector3.up, shaftEnd - sourceGrip);
            gripMarker.localRotation = primaryGrip.localRotation;
            bladeMarker.localRotation = bladeContact.localRotation;

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException("Production hockey stick prefab has no renderers.");
            Bounds renderedBounds = renderers[0].bounds;
            for (int i = 0; i < renderers.Length; i++)
            {
                ApplyArenaStickRendererPolicy(renderers[i]);
                if (i > 0) renderedBounds.Encapsulate(renderers[i].bounds);
            }
            float expectedLength = Vector3.Distance(targetGrip, targetBlade);
            if (renderedBounds.size.magnitude < expectedLength * 0.75f
                || renderedBounds.size.magnitude > expectedLength * 2f)
                throw new InvalidOperationException($"Production stick rendered bounds are invalid: {renderedBounds.size}.");
            return model;
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
                    if (slot == HockeyEquipmentSlot.Stick)
                        NewTarget("SecondaryGrip", replacement.transform, new Vector3(0f, -0.35f, 0f));
                    loadout.Equip(slot, replacement);
                    foreach (HockeyEquipmentSlot other in Enum.GetValues(typeof(HockeyEquipmentSlot)))
                        if (other != slot && loadout.GetEquipped(other) != before[other])
                            throw new InvalidOperationException($"Edit Mode replacement of {slot} changed {other}.");
                }
                if (!rig.HasValidReferences || rig.LeftHandTarget.IsChildOf(loadout.GetEquipped(HockeyEquipmentSlot.Stick).transform)
                    || rig.EquippedSecondaryGrip == null
                    || !rig.EquippedSecondaryGrip.IsChildOf(loadout.GetEquipped(HockeyEquipmentSlot.Stick).transform)
                    || rig.RightHandTarget.IsChildOf(loadout.GetEquipped(HockeyEquipmentSlot.Stick).transform))
                    throw new InvalidOperationException("Stick replacement did not rebind the live SecondaryGrip target.");
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

        private static void ValidateEquipmentEnumValues()
        {
            HockeyEquipmentSlot[] values = Enum.GetValues(typeof(HockeyEquipmentSlot))
                .Cast<HockeyEquipmentSlot>().ToArray();
            if (values.Length != ExpectedEquipmentSlotCount
                || (int)HockeyEquipmentSlot.Helmet != 0
                || (int)HockeyEquipmentSlot.Gloves != 2
                || (int)HockeyEquipmentSlot.Skates != 4
                || (int)HockeyEquipmentSlot.Stick != 5
                || (int)HockeyEquipmentSlot.Visor != 8)
                throw new InvalidOperationException("Equipment slot serialized IDs are not migration-safe.");
        }

        private static void ValidateSupportedEquipmentStructure(GameObject root, string artifactName)
        {
            HockeyEquipmentLoadout loadout = root != null ? root.GetComponent<HockeyEquipmentLoadout>() : null;
            HockeyCharacterPresentation presentation = root != null
                ? root.GetComponent<HockeyCharacterPresentation>() : null;
            HashSet<HockeyEquipmentSlot> expected = new()
            {
                HockeyEquipmentSlot.Helmet,
                HockeyEquipmentSlot.Visor,
                HockeyEquipmentSlot.Gloves,
                HockeyEquipmentSlot.Skates,
                HockeyEquipmentSlot.Stick
            };
            if (loadout == null || loadout.SlotCount != expected.Count
                || loadout.Slots.Any(binding => binding == null || !expected.Remove(binding.Slot))
                || expected.Count != 0)
                throw new InvalidOperationException($"{artifactName} does not contain exactly the supported equipment bindings.");
            if (presentation == null || presentation.CharacterRenderers.Count == 0
                || presentation.CharacterRenderers.Any(renderer => renderer == null))
                throw new InvalidOperationException($"{artifactName} does not capture its main-character renderers.");

            HashSet<string> unsupportedNames = new(StringComparer.Ordinal)
            {
                "ShoulderPadsSlot", "Shoulder Pads", "Shoulder Pads Chest", "Shoulder Pad L", "Shoulder Pad R",
                "JerseySlot", "Jersey", "JerseyVisual", "PantsSlot", "Pants", "PantsVisual",
                "SocksSlot", "Socks", "Socks L", "Socks R"
            };
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
                if (unsupportedNames.Contains(transforms[i].name))
                    throw new InvalidOperationException(
                        $"{artifactName} still contains unsupported equipment object {transforms[i].name}.");
        }

        private static void ValidateGeneratedSceneEquipment()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
            if (openedForValidation) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                HockeyEquipmentLoadout[] loadouts = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<HockeyEquipmentLoadout>(true)).ToArray();
                if (loadouts.Length != 10)
                    throw new InvalidOperationException(
                        $"ModularCharacterTest must contain 10 generated players, found {loadouts.Length}.");
                for (int i = 0; i < loadouts.Length; i++)
                {
                    ValidateSupportedEquipmentStructure(loadouts[i].gameObject,
                        $"ModularCharacterTest player {i + 1}");
                    ValidateProductionSkates(loadouts[i].gameObject,
                        $"ModularCharacterTest player {i + 1}");
                }
            }
            finally
            {
                if (openedForValidation && scene.IsValid()) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ValidateProductionSkates(GameObject root, string artifactName)
        {
            HockeyEquipmentLoadout loadout = root != null ? root.GetComponent<HockeyEquipmentLoadout>() : null;
            GameObject item = loadout != null ? loadout.GetEquipped(HockeyEquipmentSlot.Skates) : null;
            HockeyPairedEquipmentFollower follower = item != null
                ? item.GetComponent<HockeyPairedEquipmentFollower>() : null;
            Transform left = item != null ? item.transform.Find("Skate_L_v1") : null;
            Transform right = item != null ? item.transform.Find("Skate_R_v1") : null;
            Transform leftContact = left != null ? left.Find("BladeContact") : null;
            Transform rightContact = right != null ? right.Find("BladeContact") : null;
            Mesh leftMesh = AssetDatabase.LoadAssetAtPath<Mesh>(
                SkateDirectory + "/Prefabs/Skate_Base_v1_Canonical.asset");
            Mesh rightMesh = AssetDatabase.LoadAssetAtPath<Mesh>(
                SkateDirectory + "/Prefabs/Skate_Base_v1_Mirrored.asset");
            Material skateMaterial = AssetDatabase.LoadAssetAtPath<Material>(SkateMaterialPath);
            MeshFilter leftFilter = left != null ? left.GetComponentInChildren<MeshFilter>(true) : null;
            MeshFilter rightFilter = right != null ? right.GetComponentInChildren<MeshFilter>(true) : null;
            Renderer[] renderers = item != null
                ? item.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
            SkinnedMeshRenderer characterSkin = root != null
                ? root.GetComponentInChildren<SkinnedMeshRenderer>(true) : null;
            float expectedContactY = (GameplayIceY - GameplaySpawnY) / GameplaySkaterScale;
            float leftY = leftContact != null ? root.transform.InverseTransformPoint(leftContact.position).y : float.NaN;
            float rightY = rightContact != null ? root.transform.InverseTransformPoint(rightContact.position).y : float.NaN;
            string assetPath = AssetDatabase.GetAssetPath(root);
            if (string.IsNullOrEmpty(assetPath))
                assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
            string[] dependencies = string.IsNullOrEmpty(assetPath)
                ? Array.Empty<string>() : AssetDatabase.GetDependencies(assetPath, true);
            bool positiveEqualScale = left != null && right != null
                && left.lossyScale.x > 0f && left.lossyScale.y > 0f && left.lossyScale.z > 0f
                && right.lossyScale.x > 0f && right.lossyScale.y > 0f && right.lossyScale.z > 0f
                && Vector3.Distance(left.lossyScale, right.lossyScale) <= 0.0001f;
            Transform characterVisual = root != null
                ? root.transform.Find("Visual/Male_Base_v1_1_Clean_Visual") : null;
            Vector3 expectedSkateScale = characterVisual != null
                ? Vector3.one * characterVisual.localScale.x : Vector3.zero;
            bool productionLocalScale = left != null && right != null && characterVisual != null
                && Vector3.Distance(left.localScale, expectedSkateScale) <= 0.0001f
                && Vector3.Distance(right.localScale, expectedSkateScale) <= 0.0001f;
            if (item == null || item.name != "Skates" || item.transform.childCount != 2
                || follower == null || follower.FirstVisual != left || follower.SecondVisual != right
                || leftContact == null || rightContact == null || leftFilter?.sharedMesh != leftMesh
                || rightFilter?.sharedMesh != rightMesh || leftMesh == null || rightMesh == null
                || CountMeshTriangles(leftMesh) != 4136 || CountMeshTriangles(rightMesh) != 4136
                || renderers.Length != 2 || renderers.Any(renderer => renderer.sharedMaterial != skateMaterial)
                || item.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != 0
                || item.GetComponentsInChildren<Animator>(true).Length != 0
                || characterSkin == null
                || AssetDatabase.GetAssetPath(characterSkin.sharedMesh) != FootMaskedMeshPath
                || !positiveEqualScale || !productionLocalScale
                || Mathf.Abs(leftY - expectedContactY) > 0.001f
                || Mathf.Abs(rightY - expectedContactY) > 0.001f
                || Vector3.Dot(left.up, root.transform.up) < 0.999f
                || Vector3.Dot(right.up, root.transform.up) < 0.999f
                || Vector3.Dot(left.forward, root.transform.forward) < 0.999f
                || Vector3.Dot(right.forward, root.transform.forward) < 0.999f
                || (!string.IsNullOrEmpty(assetPath)
                    && (!dependencies.Contains(LeftSkatePrefabPath) || !dependencies.Contains(RightSkatePrefabPath))))
                throw new InvalidOperationException(
                    $"{artifactName} production skates are invalid: leftY={leftY:F4} rightY={rightY:F4} expected={expectedContactY:F4}.");
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

        private static void ValidateSkateMaskRoundTrip(GameObject prefab)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject
                ?? throw new InvalidOperationException("Unable to instantiate HockeyPlayer for skate-mask validation.");
            try
            {
                HockeyEquipmentLoadout loadout = instance.GetComponent<HockeyEquipmentLoadout>()
                    ?? throw new InvalidOperationException("Skate-mask validation loadout is missing.");
                SkinnedMeshRenderer skin = instance.GetComponentInChildren<SkinnedMeshRenderer>(true)
                    ?? throw new InvalidOperationException("Skate-mask validation skin is missing.");
                GameObject equipped = loadout.GetEquipped(HockeyEquipmentSlot.Skates)
                    ?? throw new InvalidOperationException("Skate-mask validation equipment is missing.");
                GameObject replacement = UnityEngine.Object.Instantiate(equipped);
                loadout.Clear(HockeyEquipmentSlot.Skates);
                if (AssetDatabase.GetAssetPath(skin.sharedMesh) == FootMaskedMeshPath)
                    throw new InvalidOperationException("Clearing skates did not restore the unmasked character mesh.");
                loadout.Equip(HockeyEquipmentSlot.Skates, replacement);
                if (AssetDatabase.GetAssetPath(skin.sharedMesh) != FootMaskedMeshPath)
                    throw new InvalidOperationException("Re-equipping skates did not restore the masked character mesh.");
                Debug.Log("GAMEPLAY_SKATES_MASK_ROUNDTRIP_PASS clear=unmasked reEquip=masked");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
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

        private static Transform NewTarget(string name, Transform parent, Vector3 localPosition,
            Quaternion localRotation)
        {
            Transform target = NewTarget(name, parent, localPosition);
            target.localRotation = localRotation;
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
