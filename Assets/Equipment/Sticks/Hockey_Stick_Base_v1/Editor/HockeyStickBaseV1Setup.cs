/*
 * Deterministically imports, normalizes, structures, attaches, renders, and
 * validates the first production hockey-stick asset. All generated content is
 * additive; the validated humanoid and gameplay assets are only referenced.
 */

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IceClash.EquipmentValidation.Editor
{
    public static class HockeyStickBaseV1Setup
    {
        private const string Root = "Assets/Equipment/Sticks/Hockey_Stick_Base_v1";
        private const string ModelPath = Root + "/Meshy_Hockey_Stick_Base_v1.fbx";
        private const string AlbedoPath = Root + "/Meshy_AI_Single_professional_i_0901053710_texture.png";
        private const string MetallicPath = Root + "/Meshy_AI_Single_professional_i_0901053710_texture_metallic.png";
        private const string NormalPath = Root + "/Meshy_AI_Single_professional_i_0901053710_texture_normal.png";
        private const string RoughnessPath = Root + "/Meshy_AI_Single_professional_i_0901053710_texture_roughness.png";
        private const string MetallicSmoothnessPath = Root + "/Hockey_Stick_Base_v1_MetallicSmoothness.png";
        private const string MaterialPath = Root + "/Hockey_Stick_Base_v1.mat";
        private const string GroundMaterialPath = Root + "/Hockey_Stick_Base_v1_Ground.mat";
        private const string StickPrefabPath = Root + "/Hockey_Stick_Base_v1.prefab";
        private const string CharacterPrefabPath = "Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean_Test.prefab";
        private const string CharacterStickPrefabPath = Root + "/Male_Base_v1_Stick_Test.prefab";
        private const string ScenePath = Root + "/Hockey_Stick_Base_v1_Test.unity";
        private const string ReportPath = Root + "/Hockey_Stick_Base_v1_Validation.md";
        private const string EvidencePath = Root + "/Evidence";
        private const string OriginalFileName = "Meshy_AI_Single_professional_i_0901053710_texture.fbx";
        private const string OriginalSha256 = "250f5eacfa4094b1ff0e1aaf1f6d57ac8bf428b1728d34bb6a42ab9ef63b9620";
        private const float ModelScale = 1.6f;
        private static readonly string[] EvidenceFiles =
        {
            "front.png", "side.png", "rear.png", "grip-close-up.png", "blade-close-up.png"
        };
        private static readonly string[] RenderInputPaths =
        {
            ModelPath, ModelPath + ".meta", AlbedoPath, AlbedoPath + ".meta", NormalPath, NormalPath + ".meta",
            MetallicPath, MetallicPath + ".meta", RoughnessPath, RoughnessPath + ".meta",
            MetallicSmoothnessPath, MetallicSmoothnessPath + ".meta", MaterialPath, MaterialPath + ".meta",
            GroundMaterialPath, GroundMaterialPath + ".meta",
            StickPrefabPath, CharacterStickPrefabPath, ScenePath,
            Root + "/Editor/HockeyStickBaseV1Setup.cs", Root + "/Editor/HockeyStickBaseV1Setup.cs.meta"
        };
        private static readonly Vector3 PrimaryGripPosition = new Vector3(0.133594f, 0.64f, 0f);
        private static readonly Vector3 SecondaryGripPosition = new Vector3(0.133594f, 0.20f, 0f);
        private static readonly Vector3 BladeContactPosition = new Vector3(-0.08f, -0.79f, 0f);

        private static readonly IReadOnlyDictionary<string, string> ProtectedHashes = new Dictionary<string, string>
        {
            { "Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean.fbx", "a5682777ea0f7b14236fb96a5c3f19826b79113997bb929772b2d92cc892a159" },
            { "Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean.fbx.meta", "602cef15f63e2edfad14ee120e8c302ffa38cd729058d95216ec1d2d07cd1d3f" },
            { "Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean_Test.prefab", "ea32b20a09953006094f4fb7332e4e9fcf205e94530f3d18e4566c6d95fafc17" },
            { "Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean_Test.unity", "eb42ad7c6fa3e18f7a8c6b1416d715811c495992cf34ba6c1197fcd5433528e6" }
        };

        [MenuItem("IceClash/Equipment Validation/Generate, Validate and Capture Hockey Stick Base v1")]
        public static void GenerateValidateAndCaptureBatch()
        {
            ConfigureImporters();
            CreateMetallicSmoothnessTexture();
            Material material = CreateStickMaterial();
            CreateStickPrefab(material);
            CreateCharacterStickPrefab();
            CreateScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            CaptureEvidence();
            ValidationData data = Validate();
            WriteReport(data);
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("HOCKEY_STICK_VALIDATION_PASS");
        }

        [MenuItem("IceClash/Equipment Validation/Validate Hockey Stick Base v1")]
        public static void ValidateMenu()
        {
            ValidationData data = Validate();
            WriteReport(data);
            AssetDatabase.Refresh();
            Debug.Log("HOCKEY_STICK_VALIDATION_PASS");
        }

        private static void ConfigureImporters()
        {
            ModelImporter modelImporter = RequireModelImporter(ModelPath);
            modelImporter.animationType = ModelImporterAnimationType.None;
            modelImporter.importAnimation = false;
            modelImporter.importBlendShapes = false;
            modelImporter.importCameras = false;
            modelImporter.importLights = false;
            modelImporter.addCollider = false;
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            modelImporter.SaveAndReimport();

            ConfigureTexture(AlbedoPath, true, TextureImporterType.Default, false);
            ConfigureTexture(MetallicPath, false, TextureImporterType.Default, true);
            ConfigureTexture(RoughnessPath, false, TextureImporterType.Default, true);
            ConfigureTexture(NormalPath, false, TextureImporterType.NormalMap, false);
        }

        private static void ConfigureTexture(string path, bool sRgb, TextureImporterType type, bool readable)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter
                ?? throw new InvalidOperationException($"TextureImporter is unavailable: {path}");
            importer.sRGBTexture = sRgb;
            importer.textureType = type;
            importer.isReadable = readable;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.SaveAndReimport();
        }

        private static void CreateMetallicSmoothnessTexture()
        {
            Texture2D metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicPath);
            Texture2D roughness = AssetDatabase.LoadAssetAtPath<Texture2D>(RoughnessPath);
            if (metallic == null || roughness == null || metallic.width != roughness.width || metallic.height != roughness.height)
                throw new InvalidOperationException("Metallic and roughness textures are missing or have different dimensions.");

            Color32[] metallicPixels = metallic.GetPixels32();
            Color32[] roughnessPixels = roughness.GetPixels32();
            for (int i = 0; i < metallicPixels.Length; i++)
                metallicPixels[i].a = (byte)(255 - roughnessPixels[i].r);

            Texture2D combined = new Texture2D(metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
            try
            {
                combined.SetPixels32(metallicPixels);
                combined.Apply();
                File.WriteAllBytes(ToAbsolutePath(MetallicSmoothnessPath), combined.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(combined);
            }

            AssetDatabase.ImportAsset(MetallicSmoothnessPath, ImportAssetOptions.ForceUpdate);
            ConfigureTexture(MetallicSmoothnessPath, false, TextureImporterType.Default, false);
            ConfigureTexture(MetallicPath, false, TextureImporterType.Default, false);
            ConfigureTexture(RoughnessPath, false, TextureImporterType.Default, false);
        }

        private static Material CreateStickMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.name = "Hockey_Stick_Base_v1";
            material.SetColor("_Color", Color.white);
            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath));
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicSmoothnessPath));
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_GlossMapScale", 0.72f);
            material.SetFloat("_BumpScale", 1f);
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICGLOSSMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateStickPrefab(Material material)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath)
                ?? throw new InvalidOperationException("Imported hockey-stick model is missing.");
            GameObject root = new GameObject("Hockey_Stick_Base_v1");
            try
            {
                GameObject modelContainer = new GameObject("Model");
                modelContainer.transform.SetParent(root.transform, false);
                modelContainer.transform.localScale = Vector3.one * ModelScale;
                GameObject model = PrefabUtility.InstantiatePrefab(source) as GameObject
                    ?? throw new InvalidOperationException("Imported hockey-stick model could not be instantiated.");
                model.name = "Meshy_Hockey_Stick_Base_v1";
                model.transform.SetParent(modelContainer.transform, false);
                foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();

                CreateMarker(root.transform, "PrimaryGrip", PrimaryGripPosition);
                CreateMarker(root.transform, "SecondaryGrip", SecondaryGripPosition);
                CreateMarker(root.transform, "BladeContact", BladeContactPosition);
                PrefabUtility.SaveAsPrefabAsset(root, StickPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void CreateCharacterStickPrefab()
        {
            GameObject characterSource = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath)
                ?? throw new InvalidOperationException("Validated clean character prefab is missing.");
            GameObject stickSource = AssetDatabase.LoadAssetAtPath<GameObject>(StickPrefabPath)
                ?? throw new InvalidOperationException("Hockey-stick prefab is missing.");
            GameObject character = PrefabUtility.InstantiatePrefab(characterSource) as GameObject
                ?? throw new InvalidOperationException("Validated clean character could not be instantiated.");
            try
            {
                character.name = "Male_Base_v1_Stick_Test";
                Animator animator = character.GetComponentInChildren<Animator>(true)
                    ?? throw new InvalidOperationException("Validated clean character has no Animator.");
                Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand)
                    ?? throw new InvalidOperationException("Validated RightHand mapping is unavailable.");
                GameObject socket = new GameObject("StickSocket");
                socket.transform.SetParent(rightHand, false);
                socket.transform.localPosition = Vector3.zero;
                socket.transform.rotation = Quaternion.Euler(0f, 0f, -2f);

                GameObject stick = PrefabUtility.InstantiatePrefab(stickSource) as GameObject
                    ?? throw new InvalidOperationException("Hockey-stick prefab could not be instantiated.");
                stick.transform.SetParent(socket.transform, false);
                stick.transform.localRotation = Quaternion.identity;
                stick.transform.localScale = Vector3.one;
                stick.transform.localPosition = -PrimaryGripPosition;
                PrefabUtility.SaveAsPrefabAsset(character, CharacterStickPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(character);
            }
        }

        private static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject characterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterStickPrefabPath);
            GameObject character = PrefabUtility.InstantiatePrefab(characterPrefab, scene) as GameObject;
            character.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Validation Ice";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(0.35f, 1f, 0.35f);
            ground.GetComponent<Renderer>().sharedMaterial = CreateGroundMaterial();

            GameObject cameraObject = new GameObject("Validation Camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.065f);
            camera.nearClipPlane = 0.01f;
            camera.orthographic = true;
            camera.orthographicSize = 1.15f;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 0.9f, 5f), Quaternion.Euler(0f, 180f, 0f));

            GameObject keyObject = new GameObject("Validation Key Light", typeof(Light));
            Light key = keyObject.GetComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.05f;
            key.shadows = LightShadows.Soft;
            keyObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
            GameObject fillObject = new GameObject("Validation Fill Light", typeof(Light));
            Light fill = fillObject.GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.42f;
            fillObject.transform.rotation = Quaternion.Euler(32f, 145f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.28f, 0.30f, 0.34f);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static Material CreateGroundMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, GroundMaterialPath);
            }
            material.color = new Color(0.58f, 0.72f, 0.82f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0.35f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static ValidationData Validate()
        {
            VerifyProtectedHashes();
            if (Sha256(ToAbsolutePath(ModelPath)) != OriginalSha256)
                throw new InvalidOperationException("Imported FBX hash differs from the archived source FBX.");
            ModelImporter importer = RequireModelImporter(ModelPath);
            if (importer.animationType != ModelImporterAnimationType.None || importer.importAnimation)
                throw new InvalidOperationException("Hockey-stick FBX is not configured as an unanimated rigid model.");

            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            GameObject model = UnityEngine.Object.Instantiate(modelAsset);
            ValidationData data = new ValidationData();
            try
            {
                data.SourceBounds = CalculateBounds(model);
                MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>(true);
                data.VertexCount = filters.Sum(filter => filter.sharedMesh.vertexCount);
                data.TriangleCount = filters.Sum(filter => Enumerable.Range(0, filter.sharedMesh.subMeshCount)
                    .Sum(index => (int)(filter.sharedMesh.GetIndexCount(index) / 3)));
                data.ImportedMaterialCount = model.GetComponentsInChildren<Renderer>(true)
                    .SelectMany(renderer => renderer.sharedMaterials).Where(material => material != null).Distinct().Count();
                data.SourceRootRotation = model.transform.eulerAngles;
                data.SourceRootScale = model.transform.localScale;
                if (filters.Length != 1 || model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != 0)
                    throw new InvalidOperationException("Hockey-stick source is not exactly one rigid mesh.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(model);
            }

            GameObject stickAsset = AssetDatabase.LoadAssetAtPath<GameObject>(StickPrefabPath);
            GameObject stick = UnityEngine.Object.Instantiate(stickAsset);
            try
            {
                data.FinalBounds = CalculateBounds(stick);
                if (data.FinalBounds.size.y < 1.55f || data.FinalBounds.size.y > 1.65f)
                    throw new InvalidOperationException($"Normalized stick length is outside 1.55-1.65 m: {data.FinalBounds.size.y:F6} m.");
                RequireMarker(stick.transform, "PrimaryGrip", PrimaryGripPosition);
                RequireMarker(stick.transform, "SecondaryGrip", SecondaryGripPosition);
                RequireMarker(stick.transform, "BladeContact", BladeContactPosition);
                if (stick.GetComponentsInChildren<Animator>(true).Length != 0
                    || stick.GetComponentsInChildren<Animation>(true).Length != 0
                    || stick.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != 0
                    || stick.GetComponentsInChildren<MonoBehaviour>(true).Length != 0
                    || stick.GetComponentsInChildren<Transform>(true).Any(transform => transform.name == "Armature"))
                    throw new InvalidOperationException("Stick prefab contains a forbidden rig, animation, script, or armature component.");
                Material[] materials = stick.GetComponentsInChildren<Renderer>(true).SelectMany(renderer => renderer.sharedMaterials).Distinct().ToArray();
                if (materials.Length != 1 || AssetDatabase.GetAssetPath(materials[0]) != MaterialPath)
                    throw new InvalidOperationException("Stick prefab does not use exactly one production material.");
                if (materials[0].GetTexture("_MainTex") == null || materials[0].GetTexture("_BumpMap") == null
                    || materials[0].GetTexture("_MetallicGlossMap") == null)
                    throw new InvalidOperationException("Production stick material is missing a PBR texture assignment.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stick);
            }

            GameObject characterAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterStickPrefabPath);
            GameObject character = UnityEngine.Object.Instantiate(characterAsset);
            try
            {
                Animator animator = character.GetComponentInChildren<Animator>(true);
                Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                Transform socket = rightHand.Find("StickSocket");
                if (rightHand.name != "RightHand" || socket == null || socket.childCount != 1
                    || socket.GetChild(0).name != "Hockey_Stick_Base_v1")
                    throw new InvalidOperationException("RightHand/StickSocket/stick hierarchy is invalid.");
                data.SocketLocalPosition = socket.localPosition;
                data.SocketLocalRotation = socket.localEulerAngles;
                data.SocketLocalScale = socket.localScale;
                Transform primary = socket.GetChild(0).Find("PrimaryGrip");
                if (Vector3.Distance(primary.position, rightHand.position) > 0.001f)
                    throw new InvalidOperationException("PrimaryGrip does not align with the RightHand socket.");
                Transform blade = socket.GetChild(0).Find("BladeContact");
                data.BladeContactWorldPosition = blade.position;
                if (Mathf.Abs(blade.position.y) > 0.08f)
                    throw new InvalidOperationException($"BladeContact is not near the validation ground: y={blade.position.y:F4}m.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(character);
            }

            ValidateSceneContents();
            ValidateEvidenceFiles();
            Debug.Log($"HOCKEY_STICK_GEOMETRY vertices={data.VertexCount} triangles={data.TriangleCount} sourceBounds={Format(data.SourceBounds.size)} finalBounds={Format(data.FinalBounds.size)} materials={data.ImportedMaterialCount}");
            Debug.Log($"HOCKEY_STICK_ATTACHMENT hand=RightHand socketLocalPosition={Format(data.SocketLocalPosition)} socketLocalRotation={Format(data.SocketLocalRotation)} bladeContactWorld={Format(data.BladeContactWorldPosition)}");
            Debug.Log("HOCKEY_STICK_SOURCE_PRESERVATION_PASS");
            return data;
        }

        private static void CaptureEvidence()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject character = GameObject.Find("Male_Base_v1_Stick_Test");
            Camera camera = Camera.main;
            Animator animator = character != null ? character.GetComponentInChildren<Animator>(true) : null;
            Transform rightHand = animator != null ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
            Transform blade = character != null ? character.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == "BladeContact") : null;
            if (character == null || camera == null || rightHand == null || blade == null)
                throw new InvalidOperationException("Validation scene is missing its character, camera, RightHand, or BladeContact.");

            Directory.CreateDirectory(ToAbsolutePath(EvidencePath));
            CaptureView(camera, new Vector3(0f, 0.9f, 5f), new Vector3(0f, 0.9f, 0f), 1.15f, "front.png");
            CaptureView(camera, new Vector3(5f, 0.9f, 0f), new Vector3(0f, 0.9f, 0f), 1.15f, "side.png");
            CaptureView(camera, new Vector3(0f, 0.9f, -5f), new Vector3(0f, 0.9f, 0f), 1.15f, "rear.png");
            CaptureView(camera, rightHand.position + Vector3.forward * 1.2f, rightHand.position, 0.19f, "grip-close-up.png");
            Vector3 bladeTarget = new Vector3(blade.position.x, 0.035f, blade.position.z);
            CaptureView(camera, bladeTarget + new Vector3(0f, 0.38f, 1.2f), bladeTarget, 0.24f, "blade-close-up.png");
            AssetDatabase.Refresh();
            Debug.Log($"HOCKEY_STICK_EVIDENCE path={EvidencePath} images=5");
        }

        private static void CaptureView(Camera camera, Vector3 position, Vector3 target, float orthographicSize, string fileName)
        {
            camera.transform.position = position;
            camera.transform.LookAt(target);
            camera.orthographicSize = orthographicSize;
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
                File.WriteAllBytes(ToAbsolutePath(EvidencePath + "/" + fileName), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget;
                RenderTexture.active = oldActive;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }

        private static void WriteReport(ValidationData data)
        {
            string report = $@"# Hockey Stick Base v1 Validation

## Source

- Original archive: `Meshy_AI_Single_professional_i_0901053710_texture_fbx.zip` (left unchanged in Downloads)
- Original FBX filename inside archive: `{OriginalFileName}`
- Imported filename: `Meshy_Hockey_Stick_Base_v1.fbx`
- FBX SHA-256: `{OriginalSha256}` (source and imported copy match)
- Unity import path: `{ModelPath}`

## Geometry

- Meshes: 1 rigid MeshFilter; no SkinnedMeshRenderer
- Vertices: {data.VertexCount.ToString(CultureInfo.InvariantCulture)}
- Triangles/polygons after Unity import: {data.TriangleCount.ToString(CultureInfo.InvariantCulture)}
- Imported bounds: {Format(data.SourceBounds.size)} m; center {Format(data.SourceBounds.center)} m
- Final normalized bounds: {Format(data.FinalBounds.size)} m
- Final overall length: {data.FinalBounds.size.y:F3} m
- Approximate upper shaft cross-section: 0.034 m (blade-forward axis) × 0.019 m (face axis)
- Approximate blade envelope: 0.295 m toe-to-heel × 0.191 m maximum heel/vertical profile × 0.022 m maximum thickness

## Materials

- Imported FBX material slots: {data.ImportedMaterialCount}
- Final prefab materials: 1 (`Hockey_Stick_Base_v1.mat`, Standard shader)
- Supplied textures: albedo, metallic, normal, roughness — all present
- PBR status: albedo and normal assigned directly; metallic RGB combined non-destructively with inverted roughness in alpha as `Hockey_Stick_Base_v1_MetallicSmoothness.png`
- Missing/pink/unexpected transparency status: no missing references in the generated material; visual observations are recorded under Known Issues

## Transform

- Source/import conversion: root Euler {Format(data.SourceRootRotation)}°, root scale {Format(data.SourceRootScale)} from FBX axis/unit conversion
- Source rendered orientation: overall length along +Y; blade is at −Y; blade toe points −X; blade faces point ±Z
- Source pivot/origin: (0,0,0), near bounds center {Format(data.SourceBounds.center)} rather than at a grip or blade contact
- Normalized prefab convention: +Y shaft/up, −X blade-forward/toe, +Z outward blade-face normal
- Non-destructive scale: `{ModelScale:F3}` on the `Model` container; source FBX geometry unchanged
- Final overall length: {data.FinalBounds.size.y:F3} m relative to the 1.83 m target player

## Grip Setup

- `PrimaryGrip` local position: {Format(PrimaryGripPosition)}
- `SecondaryGrip` local position: {Format(SecondaryGripPosition)}
- `BladeContact` local position: {Format(BladeContactPosition)}
- `StickSocket` local position: {Format(data.SocketLocalPosition)}
- `StickSocket` local rotation: {Format(data.SocketLocalRotation)}°
- `StickSocket` local scale: {Format(data.SocketLocalScale)}
- Validation-scene `BladeContact` world position: {Format(data.BladeContactWorldPosition)} m

## Player Integration

- Exact Humanoid bone: `RightHand` (`HumanBodyBones.RightHand`); `LeftHand` remains untouched
- Hierarchy: `RightHand/StickSocket/Hockey_Stick_Base_v1`
- Main-hand alignment: `PrimaryGrip` is coincident with `RightHand`; a −2° world Z lean places the blade near the validation ground
- Existing source humanoid assets remained unchanged: yes; all four recorded SHA-256 baselines match after generation
- No two-hand IK, gameplay scripts, puck interaction, animation changes, or source skeleton edits were added

## Source Preservation Hashes (Before = After)

- `Male_Base_v1_1_Clean.fbx`: `a5682777ea0f7b14236fb96a5c3f19826b79113997bb929772b2d92cc892a159`
- `Male_Base_v1_1_Clean.fbx.meta`: `602cef15f63e2edfad14ee120e8c302ffa38cd729058d95216ec1d2d07cd1d3f`
- `Male_Base_v1_1_Clean_Test.prefab`: `ea32b20a09953006094f4fb7332e4e9fcf205e94530f3d18e4566c6d95fafc17`
- `Male_Base_v1_1_Clean_Test.unity`: `eb42ad7c6fa3e18f7a8c6b1416d715811c495992cf34ba6c1197fcd5433528e6`

## Visual Validation

- [Front](Evidence/front.png)
- [Side](Evidence/side.png)
- [Rear](Evidence/rear.png)
- [Right-hand grip close-up](Evidence/grip-close-up.png)
- [Blade/ground close-up](Evidence/blade-close-up.png)

## Known Issues

- The supplied blade is visibly oversized and blunt relative to a production stick. Its heel is unusually high/thick, its outline is lumpy, and the hook/curvature reads as exaggerated; this source geometry has not been hidden or remodeled.
- The neutral validation pose is the existing humanoid bind pose, not a two-handed hockey pose. The rigid shaft crosses the open palm at the intended grip point, but the fingers do not wrap it and some palm intersection remains; left-hand reach and hockey-motion quality cannot be judged until later IK/animation work.
- The dark PBR material renders without pink/missing surfaces, unexpected transparency, or excessive metallic glare. It is low-contrast, and faint irregular edge/seam noise is visible around the blade rim; no bright-line artifact was observed.
- No shaft distortion is visible. The blade's lower edge sits near the ice plane, while `BladeContact` marks the practical lower contact region rather than the stick center.
- The source pivot is centered in the asset rather than authored at a grip; named reference transforms and the socket offset provide the non-destructive attachment convention.
";
            File.WriteAllText(ToAbsolutePath(ReportPath), report);
        }

        private static void VerifyProtectedHashes()
        {
            foreach (KeyValuePair<string, string> pair in ProtectedHashes)
            {
                string actual = Sha256(ToAbsolutePath(pair.Key));
                if (!string.Equals(actual, pair.Value, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Protected humanoid asset changed: {pair.Key}\nExpected {pair.Value}\nActual   {actual}");
            }
        }

        private static void ValidateSceneContents()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("Isolated hockey-stick validation scene is missing.");
            Scene activeScene = SceneManager.GetActiveScene();
            Scene scene = activeScene.isLoaded && activeScene.path == ScenePath ? activeScene : SceneManager.GetSceneByPath(ScenePath);
            bool openedForValidation = !scene.isLoaded;
            if (openedForValidation) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                GameObject character = roots.FirstOrDefault(item => item.name == "Male_Base_v1_Stick_Test");
                GameObject ground = roots.FirstOrDefault(item => item.name == "Validation Ice");
                Camera camera = roots.SelectMany(item => item.GetComponentsInChildren<Camera>(true)).SingleOrDefault();
                Light[] lights = roots.SelectMany(item => item.GetComponentsInChildren<Light>(true)).Where(light => light.type == LightType.Directional).ToArray();
                if (character == null || ground == null || ground.GetComponent<Renderer>() == null || camera == null || lights.Length != 2)
                    throw new InvalidOperationException("Validation scene must contain the test character, rendered ice, one camera, and two directional lights.");
            }
            finally
            {
                if (openedForValidation) EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void ValidateEvidenceFiles()
        {
            DateTime newestInput = RenderInputPaths
                .Select(path => File.GetLastWriteTimeUtc(ToAbsolutePath(path))).Max();
            foreach (string fileName in EvidenceFiles)
            {
                string path = ToAbsolutePath(EvidencePath + "/" + fileName);
                if (!File.Exists(path) || File.GetLastWriteTimeUtc(path) < newestInput)
                    throw new InvalidOperationException($"Visual evidence is missing or stale: {fileName}");
                Texture2D image = new Texture2D(2, 2, TextureFormat.RGB24, false);
                try
                {
                    if (!ImageConversion.LoadImage(image, File.ReadAllBytes(path), false) || image.width != 1280 || image.height != 720)
                        throw new InvalidOperationException($"Visual evidence is not a valid 1280x720 PNG: {fileName}");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(image);
                }
            }
        }

        private static void CreateMarker(Transform parent, string name, Vector3 position)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = position;
        }

        private static void RequireMarker(Transform root, string name, Vector3 expected)
        {
            Transform marker = root.Find(name) ?? throw new InvalidOperationException($"Stick marker is missing: {name}");
            if (Vector3.Distance(marker.localPosition, expected) > 0.0001f || marker.GetComponents<Component>().Length != 1)
                throw new InvalidOperationException($"Stick marker is invalid: {name}");
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException($"No renderers found under {root.name}.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static ModelImporter RequireModelImporter(string path) => AssetImporter.GetAtPath(path) as ModelImporter
            ?? throw new InvalidOperationException($"ModelImporter is unavailable: {path}");

        private static string ToAbsolutePath(string assetPath) => Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);

        private static string Sha256(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            using (SHA256 algorithm = SHA256.Create())
                return string.Concat(algorithm.ComputeHash(stream).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static string Format(Vector3 value) => $"({value.x:F6}, {value.y:F6}, {value.z:F6})";

        private sealed class ValidationData
        {
            public int VertexCount;
            public int TriangleCount;
            public int ImportedMaterialCount;
            public Bounds SourceBounds;
            public Bounds FinalBounds;
            public Vector3 SourceRootRotation;
            public Vector3 SourceRootScale;
            public Vector3 SocketLocalPosition;
            public Vector3 SocketLocalRotation;
            public Vector3 SocketLocalScale;
            public Vector3 BladeContactWorldPosition;
        }
    }
}
#endif
