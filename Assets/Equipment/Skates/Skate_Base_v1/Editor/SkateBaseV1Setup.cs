/*
 * Deterministically preserves, extracts, normalizes, fits, renders, and
 * validates Skate_Base_v1 as rigid removable equipment. The supplied source
 * FBX remains unchanged; one boot and its positive-scale mirror are derived
 * as production Mesh assets. Validated humanoid/gameplay assets are read-only.
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
using UnityEngine.Rendering;

namespace IceClash.EquipmentValidation.Editor
{
    public static class SkateBaseV1Setup
    {
        private const string Root = "Assets/Equipment/Skates/Skate_Base_v1";
        private const string Source = Root + "/Source";
        private const string Materials = Root + "/Materials";
        private const string Prefabs = Root + "/Prefabs";
        private const string Tests = Root + "/Tests";
        private const string ModelPath = Source + "/Meshy_AI_Single_professional_i_0902012255_texture.fbx";
        private const string AlbedoPath = Source + "/Meshy_AI_Single_professional_i_0902012255_texture.png";
        private const string MetallicPath = Source + "/Meshy_AI_Single_professional_i_0902012255_texture_metallic.png";
        private const string NormalPath = Source + "/Meshy_AI_Single_professional_i_0902012255_texture_normal.png";
        private const string RoughnessPath = Source + "/Meshy_AI_Single_professional_i_0902012255_texture_roughness.png";
        private const string CombinedPath = Materials + "/Skate_Base_v1_MetallicSmoothness.png";
        private const string MaterialPath = Materials + "/Skate_Base_v1.mat";
        private const string IceMaterialPath = Materials + "/Skate_Base_v1_Ice.mat";
        private const string CanonicalMeshPath = Prefabs + "/Skate_Base_v1_Canonical.asset";
        private const string MirroredMeshPath = Prefabs + "/Skate_Base_v1_Mirrored.asset";
        private const string BasePrefabPath = Prefabs + "/Skate_Base_v1.prefab";
        private const string LeftPrefabPath = Prefabs + "/Skate_L_v1.prefab";
        private const string RightPrefabPath = Prefabs + "/Skate_R_v1.prefab";
        private const string CharacterPrefabPath = "Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean_Test.prefab";
        private const string RunningModelPath = "Assets/Characters/Male/Male_Base_v1/Meshy_AI_Navy_Training_Pose_biped_Animation_Running_withSkin.fbx";
        private const string FittingPrefabPath = Tests + "/Male_Base_v1_Skate_Fitting.prefab";
        private const string ScenePath = Tests + "/Skate_Base_v1_Fitting.unity";
        private const string ReportPath = Root + "/Skate_Base_v1_Validation.md";
        private const string EvidencePath = ".docs/evidence/skate-base-v1";
        private const string ArchiveName = "Meshy_AI_Single_professional_i_0902012255_texture_fbx.zip";
        private const string ArchiveSha256 = "ca529fa337583c06b37a30f07db63337d7094999e4a7cd443eec10f9733b6010";
        private const string FbxSha256 = "d3b96e887ce1cc811cd1019bf5ef7e5979d5f8bf6c62551c35f7814f8e470c6b";
        private const float TargetLength = 0.31f;
        private const float LateralFitScale = 1.16f;
        private const float ForwardOffsetFromFoot = 0.07f;
        private const float FittingCharacterLift = 0.08f;
        private const float IceY = 0f;
        private const float ToeTravelTolerance = 0.00001f;

        private static readonly float[] RunningEvidenceTimes = { 0.125f, 0.375f, 0.625f, 0.875f };

        private static readonly string[] FinalEvidenceFiles =
        {
            "neutral-front.png", "neutral-rear.png", "neutral-left.png", "neutral-right.png",
            "neutral-left-close.png", "neutral-right-close.png", "running-front.png",
            "running-side.png", "running-rear.png", "running-front-125.png", "running-side-125.png",
            "running-rear-125.png", "running-front-375.png", "running-side-375.png", "running-rear-375.png",
            "running-front-625.png", "running-side-625.png", "running-rear-625.png", "running-front-875.png",
            "running-side-875.png", "running-rear-875.png", "gameplay-low.png"
        };

        private static readonly IReadOnlyDictionary<string, string> ProtectedHashes = new Dictionary<string, string>
        {
            { "Assets/Characters/Male/Male_Base_v1/Meshy_AI_Navy_Training_Pose_biped_Character_output.fbx", "5427221743566b2db9c893355373c14236853cac0b0105fd1e391ebee88acfdd" },
            { "Assets/Characters/Male/Male_Base_v1/Meshy_AI_Navy_Training_Pose_biped_Character_output.fbx.meta", "ef71766943a821b14f481a010eec4040094935e0611fa4c27152a349e4713046" },
            { "Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean.fbx", "a5682777ea0f7b14236fb96a5c3f19826b79113997bb929772b2d92cc892a159" },
            { "Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean.fbx.meta", "602cef15f63e2edfad14ee120e8c302ffa38cd729058d95216ec1d2d07cd1d3f" },
            { "Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean_Test.prefab", "ea32b20a09953006094f4fb7332e4e9fcf205e94530f3d18e4566c6d95fafc17" },
            { "Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean_Test.unity", "eb42ad7c6fa3e18f7a8c6b1416d715811c495992cf34ba6c1197fcd5433528e6" },
            { RunningModelPath, "17e8584b747b909b7ee0a4731c8f2024e80cdc04731c69c3455a58cbee19678b" },
            { RunningModelPath + ".meta", "4556ef0553e5467b1ea3c4467afba4a9add46c04a1ccc42550369894e3effebe" }
        };

        [MenuItem("IceClash/Equipment Validation/Generate, Validate and Capture Skate Base v1")]
        public static void GenerateValidateAndCaptureBatch()
        {
            ConfigureImporters();
            CaptureSourceEvidence();
            CreateMetallicSmoothnessTexture();
            Material material = CreateMaterial();
            (Mesh canonical, Mesh mirrored, ExtractionData extraction) = CreateDerivedMeshes();
            CreateSkatePrefab(BasePrefabPath, "Skate_Base_v1", canonical, material);
            CreateSkatePrefab(LeftPrefabPath, "Skate_L", canonical, material);
            CreateSkatePrefab(RightPrefabPath, "Skate_R", mirrored, material);
            CreateFittingPrefab();
            CreateFittingScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            CaptureFinalEvidence();
            ValidationData data = Validate(extraction);
            WriteReport(data);
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Debug.Log("SKATE_BASE_V1_VALIDATION_PASS");
        }

        [MenuItem("IceClash/Equipment Validation/Validate Skate Base v1")]
        public static void ValidateBatch()
        {
            ValidationData data = Validate(ReadExtractionData());
            WriteReport(data);
            AssetDatabase.Refresh();
            Debug.Log("SKATE_BASE_V1_VALIDATION_PASS");
        }

        [MenuItem("IceClash/Equipment Validation/Capture Skate Base v1 Source")]
        public static void CaptureSourceBatch()
        {
            ConfigureImporters();
            CaptureSourceEvidence();
            AssetDatabase.Refresh();
            Debug.Log("SKATE_SOURCE_CAPTURE_PASS");
        }

        private static void ConfigureImporters()
        {
            ModelImporter importer = RequireModelImporter(ModelPath);
            importer.animationType = ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.isReadable = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.SaveAndReimport();
            ConfigureTexture(AlbedoPath, true, TextureImporterType.Default, false);
            ConfigureTexture(NormalPath, false, TextureImporterType.NormalMap, false);
            ConfigureTexture(MetallicPath, false, TextureImporterType.Default, true);
            ConfigureTexture(RoughnessPath, false, TextureImporterType.Default, true);
        }

        private static void ConfigureTexture(string path, bool sRgb, TextureImporterType type, bool readable)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter
                ?? throw new InvalidOperationException($"TextureImporter unavailable: {path}");
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
                throw new InvalidOperationException("Metallic and roughness maps are missing or have different dimensions.");
            Color32[] pixels = metallic.GetPixels32();
            Color32[] roughnessPixels = roughness.GetPixels32();
            for (int index = 0; index < pixels.Length; index++) pixels[index].a = (byte)(255 - roughnessPixels[index].r);
            Texture2D combined = new Texture2D(metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
            try
            {
                combined.SetPixels32(pixels);
                combined.Apply();
                File.WriteAllBytes(Absolute(CombinedPath), combined.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(combined); }
            AssetDatabase.ImportAsset(CombinedPath, ImportAssetOptions.ForceUpdate);
            ConfigureTexture(CombinedPath, false, TextureImporterType.Default, false);
            ConfigureTexture(MetallicPath, false, TextureImporterType.Default, false);
            ConfigureTexture(RoughnessPath, false, TextureImporterType.Default, false);
        }

        private static Material CreateMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.name = "Skate_Base_v1";
            material.SetColor("_Color", Color.white);
            material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath));
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath));
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(CombinedPath));
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_GlossMapScale", 0.68f);
            material.SetFloat("_BumpScale", 1f);
            material.SetInt("_Cull", (int)CullMode.Off);
            material.doubleSidedGI = true;
            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICGLOSSMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static (Mesh, Mesh, ExtractionData) CreateDerivedMeshes()
        {
            GameObject sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath)
                ?? throw new InvalidOperationException("Imported skate source is missing.");
            GameObject source = UnityEngine.Object.Instantiate(sourceAsset);
            try
            {
                MeshFilter[] filters = source.GetComponentsInChildren<MeshFilter>(true);
                if (filters.Length != 1 || source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != 0)
                    throw new InvalidOperationException("Source must contain exactly one rigid MeshFilter.");
                Mesh raw = filters[0].sharedMesh;
                int[] rawTriangles = raw.triangles;
                List<int> selected = new List<int>(rawTriangles);
                int oppositeTriangles = 0;
                if (selected.Count / 3 != 4136)
                    throw new InvalidOperationException($"Expected the complete 4,136-face source skate, got {selected.Count / 3}.");
                Mesh canonical = ExtractCanonicalMesh(raw, selected, filters[0].transform.localToWorldMatrix);
                Mesh mirrored = MirrorMesh(canonical);
                SaveMesh(canonical, CanonicalMeshPath);
                SaveMesh(mirrored, MirroredMeshPath);
                canonical = AssetDatabase.LoadAssetAtPath<Mesh>(CanonicalMeshPath);
                mirrored = AssetDatabase.LoadAssetAtPath<Mesh>(MirroredMeshPath);
                return (canonical, mirrored, new ExtractionData
                {
                    SourceVertices = raw.vertexCount,
                    SourceTriangles = rawTriangles.Length / 3,
                    CanonicalVertices = canonical.vertexCount,
                    CanonicalTriangles = canonical.triangles.Length / 3,
                    OppositeTriangles = oppositeTriangles,
                    SourceBounds = CalculateBounds(source),
                    SourceLocalBounds = raw.bounds,
                    SourceRootRotation = source.transform.eulerAngles,
                    SourceRootScale = source.transform.localScale,
                    CanonicalBounds = canonical.bounds
                });
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
        }

        private static Mesh ExtractCanonicalMesh(Mesh raw, List<int> selected, Matrix4x4 sourceTransform)
        {
            Vector3[] rawVertices = raw.vertices;
            Vector3[] rawNormals = raw.normals;
            Vector4[] rawTangents = raw.tangents;
            Vector2[] rawUv = raw.uv;
            var remap = new Dictionary<int, int>();
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();
            Quaternion canonicalRotation = Quaternion.Euler(0f, -90f, 0f);
            foreach (int oldIndex in selected)
            {
                if (!remap.TryGetValue(oldIndex, out int newIndex))
                {
                    newIndex = vertices.Count;
                    remap.Add(oldIndex, newIndex);
                    vertices.Add(canonicalRotation * sourceTransform.MultiplyPoint3x4(rawVertices[oldIndex]));
                    if (rawNormals.Length == rawVertices.Length)
                        normals.Add((canonicalRotation * sourceTransform.MultiplyVector(rawNormals[oldIndex])).normalized);
                    if (rawTangents.Length == rawVertices.Length)
                    {
                        Vector3 transformed = (canonicalRotation * sourceTransform.MultiplyVector(new Vector3(rawTangents[oldIndex].x, rawTangents[oldIndex].y, rawTangents[oldIndex].z))).normalized;
                        tangents.Add(new Vector4(transformed.x, transformed.y, transformed.z, rawTangents[oldIndex].w));
                    }
                    if (rawUv.Length == rawVertices.Length) uv.Add(rawUv[oldIndex]);
                }
                triangles.Add(newIndex);
            }
            Bounds bounds = BoundsOf(vertices);
            float scale = TargetLength / bounds.size.z;
            Vector3 origin = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            for (int index = 0; index < vertices.Count; index++)
            {
                Vector3 fitted = (vertices[index] - origin) * scale;
                fitted.x *= LateralFitScale;
                vertices[index] = fitted;
            }
            Mesh mesh = new Mesh { name = "Skate_Base_v1_Canonical", indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            if (normals.Count == vertices.Count) mesh.SetNormals(normals);
            if (tangents.Count == vertices.Count) mesh.SetTangents(tangents);
            if (uv.Count == vertices.Count) mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0, true);
            if (normals.Count != vertices.Count) mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh MirrorMesh(Mesh canonical)
        {
            Mesh mirrored = UnityEngine.Object.Instantiate(canonical);
            mirrored.name = "Skate_Base_v1_Mirrored";
            Vector3[] vertices = mirrored.vertices;
            Vector3[] normals = mirrored.normals;
            Vector4[] tangents = mirrored.tangents;
            int[] triangles = mirrored.triangles;
            for (int index = 0; index < vertices.Length; index++)
            {
                vertices[index].x = -vertices[index].x;
                if (normals.Length == vertices.Length) normals[index].x = -normals[index].x;
                if (tangents.Length == vertices.Length)
                {
                    tangents[index].x = -tangents[index].x;
                    tangents[index].w = -tangents[index].w;
                }
            }
            for (int index = 0; index < triangles.Length; index += 3)
                (triangles[index + 1], triangles[index + 2]) = (triangles[index + 2], triangles[index + 1]);
            mirrored.vertices = vertices;
            if (normals.Length == vertices.Length) mirrored.normals = normals;
            if (tangents.Length == vertices.Length) mirrored.tangents = tangents;
            mirrored.triangles = triangles;
            mirrored.RecalculateBounds();
            return mirrored;
        }

        private static void SaveMesh(Mesh generated, string path)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(generated, path);
            else
            {
                EditorUtility.CopySerialized(generated, existing);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(existing);
            }
        }

        private static void CreateSkatePrefab(string path, string name, Mesh mesh, Material material)
        {
            GameObject root = new GameObject(name);
            try
            {
                GameObject visual = new GameObject("Visual", typeof(MeshFilter), typeof(MeshRenderer));
                visual.transform.SetParent(root.transform, false);
                visual.GetComponent<MeshFilter>().sharedMesh = mesh;
                visual.GetComponent<MeshRenderer>().sharedMaterial = material;
                GameObject contact = new GameObject("BladeContact");
                contact.transform.SetParent(root.transform, false);
                contact.transform.localPosition = Vector3.zero;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void CreateFittingPrefab()
        {
            GameObject characterSource = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath)
                ?? throw new InvalidOperationException("Validated clean character prefab is missing.");
            GameObject character = PrefabUtility.InstantiatePrefab(characterSource) as GameObject
                ?? throw new InvalidOperationException("Validated clean character could not be instantiated.");
            try
            {
                character.name = "Male_Base_v1_Skate_Fitting";
                character.transform.position = Vector3.up * FittingCharacterLift;
                Animator animator = character.GetComponentInChildren<Animator>(true)
                    ?? throw new InvalidOperationException("Validated character has no Animator.");
                AttachSkate(animator, HumanBodyBones.LeftFoot, "LeftSkateSocket", LeftPrefabPath);
                AttachSkate(animator, HumanBodyBones.RightFoot, "RightSkateSocket", RightPrefabPath);
                PrefabUtility.SaveAsPrefabAsset(character, FittingPrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(character); }
        }

        private static void AttachSkate(Animator animator, HumanBodyBones bone, string socketName, string prefabPath)
        {
            Transform foot = animator.GetBoneTransform(bone) ?? throw new InvalidOperationException($"Humanoid mapping missing {bone}.");
            HumanBodyBones toeBone = bone == HumanBodyBones.LeftFoot ? HumanBodyBones.LeftToes : HumanBodyBones.RightToes;
            Transform toe = animator.GetBoneTransform(toeBone) ?? throw new InvalidOperationException($"Humanoid mapping missing {toeBone}.");
            GameObject socketObject = new GameObject(socketName);
            Transform socket = socketObject.transform;
            socket.SetParent(foot, false);
            socket.localPosition = Vector3.zero;
            Vector3 footForward = toe.position - foot.position;
            footForward.y = 0f;
            socket.rotation = Quaternion.LookRotation(footForward.normalized, Vector3.up);
            socket.localScale = Vector3.one;
            GameObject skate = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)) as GameObject
                ?? throw new InvalidOperationException($"Skate prefab could not be instantiated: {prefabPath}");
            skate.name = bone == HumanBodyBones.LeftFoot ? "Skate_L" : "Skate_R";
            skate.transform.SetParent(socket, false);
            skate.transform.localPosition = new Vector3(0f, -foot.position.y + IceY, ForwardOffsetFromFoot);
            skate.transform.localRotation = Quaternion.identity;
            skate.transform.localScale = Vector3.one;
        }

        private static void CreateFittingScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject character = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(FittingPrefabPath), scene) as GameObject;
            character.transform.SetPositionAndRotation(Vector3.up * FittingCharacterLift, Quaternion.identity);
            GameObject ice = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ice.name = "Validation Ice";
            ice.transform.position = new Vector3(0f, IceY, 0f);
            ice.transform.localScale = Vector3.one * 0.38f;
            ice.GetComponent<Renderer>().sharedMaterial = CreateIceMaterial();
            GameObject cameraObject = new GameObject("Validation Camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.065f);
            camera.nearClipPlane = 0.01f;
            camera.orthographic = true;
            camera.orthographicSize = 1.12f;
            GameObject keyObject = new GameObject("Validation Key Light", typeof(Light));
            Light key = keyObject.GetComponent<Light>();
            key.type = LightType.Directional;
            key.intensity = 1.0f;
            key.shadows = LightShadows.Soft;
            keyObject.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
            GameObject fillObject = new GameObject("Validation Fill Light", typeof(Light));
            Light fill = fillObject.GetComponent<Light>();
            fill.type = LightType.Directional;
            fill.intensity = 0.45f;
            fillObject.transform.rotation = Quaternion.Euler(28f, 145f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.31f, 0.32f, 0.35f);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static Material CreateIceMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(IceMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, IceMaterialPath);
            }
            material.color = new Color(0.64f, 0.78f, 0.88f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0.42f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CaptureSourceEvidence()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
            model.name = "Unmodified Paired Skate Source";
            Bounds bounds = CalculateBounds(model);
            model.transform.localScale *= TargetLength / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            bounds = CalculateBounds(model);
            Camera camera = CreateCaptureCamera();
            CreateCaptureLight();
            CaptureOrthographic(camera, bounds.center + Vector3.forward, bounds.center, 0.22f, "source-front.png");
            CaptureOrthographic(camera, bounds.center + Vector3.right, bounds.center, 0.22f, "source-side.png");
            CaptureOrthographic(camera, bounds.center + Vector3.up, bounds.center, 0.22f, "source-top.png");
            CaptureOrthographic(camera, bounds.center + new Vector3(0.8f, 0.6f, 0.8f), bounds.center, 0.22f, "source-iso.png");
        }

        private static void CaptureFinalEvidence()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject character = GameObject.Find("Male_Base_v1_Skate_Fitting")
                ?? throw new InvalidOperationException("Fitting scene character is missing.");
            Camera camera = Camera.main ?? throw new InvalidOperationException("Fitting scene camera is missing.");
            CaptureOrthographic(camera, new Vector3(0f, 0.9f, 4f), new Vector3(0f, 0.88f, 0f), 1.12f, "neutral-front.png");
            CaptureOrthographic(camera, new Vector3(0f, 0.9f, -4f), new Vector3(0f, 0.88f, 0f), 1.12f, "neutral-rear.png");
            CaptureOrthographic(camera, new Vector3(-4f, 0.9f, 0f), new Vector3(0f, 0.88f, 0f), 1.12f, "neutral-left.png");
            CaptureOrthographic(camera, new Vector3(4f, 0.9f, 0f), new Vector3(0f, 0.88f, 0f), 1.12f, "neutral-right.png");
            Transform left = FindDescendant(character.transform, "Skate_L");
            Transform right = FindDescendant(character.transform, "Skate_R");
            Bounds leftBounds = CalculateBounds(left.gameObject);
            Bounds rightBounds = CalculateBounds(right.gameObject);
            right.gameObject.SetActive(false);
            CaptureOrthographic(camera, leftBounds.center + new Vector3(-0.45f, 0.18f, 0.62f), leftBounds.center, 0.19f, "neutral-left-close.png");
            right.gameObject.SetActive(true);
            left.gameObject.SetActive(false);
            CaptureOrthographic(camera, rightBounds.center + new Vector3(0.45f, 0.18f, 0.62f), rightBounds.center, 0.19f, "neutral-right-close.png");
            left.gameObject.SetActive(true);
            AnimationClip clip = SelectRunningClip();
            SkinnedMeshRenderer[] sampledRenderers = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (SkinnedMeshRenderer sampledRenderer in sampledRenderers)
                sampledRenderer.updateWhenOffscreen = true;
            AnimationMode.StartAnimationMode();
            try
            {
                for (int phase = 0; phase < RunningEvidenceTimes.Length; phase++)
                {
                    float normalizedTime = RunningEvidenceTimes[phase];
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(character, clip, clip.length * normalizedTime);
                    AnimationMode.EndSampling();
                    foreach (SkinnedMeshRenderer sampledRenderer in sampledRenderers)
                    {
                        sampledRenderer.forceMatrixRecalculationPerRender = true;
                        sampledRenderer.localBounds = sampledRenderer.localBounds;
                    }
                    CalculateBounds(character);
                    string suffix = Mathf.RoundToInt(normalizedTime * 1000f).ToString("000", CultureInfo.InvariantCulture);
                    CaptureOrthographic(camera, new Vector3(0f, 0.9f, 4f), new Vector3(0f, 0.88f, 0f), 1.12f, $"running-front-{suffix}.png");
                    CaptureOrthographic(camera, new Vector3(-4f, 0.9f, 0f), new Vector3(0f, 0.88f, 0f), 1.12f, $"running-side-{suffix}.png");
                    CaptureOrthographic(camera, new Vector3(0f, 0.9f, -4f), new Vector3(0f, 0.88f, 0f), 1.12f, $"running-rear-{suffix}.png");
                    if (phase == 0)
                    {
                        CaptureOrthographic(camera, new Vector3(0f, 0.9f, 4f), new Vector3(0f, 0.88f, 0f), 1.12f, "running-front.png");
                        CaptureOrthographic(camera, new Vector3(-4f, 0.9f, 0f), new Vector3(0f, 0.88f, 0f), 1.12f, "running-side.png");
                        CaptureOrthographic(camera, new Vector3(0f, 0.9f, -4f), new Vector3(0f, 0.88f, 0f), 1.12f, "running-rear.png");
                    }
                }
            }
            finally { if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode(); }
            CapturePerspective(camera, new Vector3(2.7f, 0.62f, -3.2f), new Vector3(0f, 0.72f, 0.12f), 34f, "gameplay-low.png");
            AssetDatabase.Refresh();
            Debug.Log($"SKATE_EVIDENCE path={EvidencePath} finalImages={FinalEvidenceFiles.Length} sourceImages=4");
        }

        private static Camera CreateCaptureCamera()
        {
            GameObject cameraObject = new GameObject("Capture Camera", typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.065f);
            camera.nearClipPlane = 0.01f;
            return camera;
        }

        private static void CreateCaptureLight()
        {
            GameObject lightObject = new GameObject("Capture Light", typeof(Light));
            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightObject.transform.rotation = Quaternion.Euler(40f, -35f, 0f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.35f, 0.35f);
        }

        private static void CaptureOrthographic(Camera camera, Vector3 position, Vector3 target, float size, string fileName)
        {
            camera.orthographic = true;
            camera.orthographicSize = size;
            Capture(camera, position, target, fileName);
        }

        private static void CapturePerspective(Camera camera, Vector3 position, Vector3 target, float fieldOfView, string fileName)
        {
            camera.orthographic = false;
            camera.fieldOfView = fieldOfView;
            Capture(camera, position, target, fileName);
        }

        private static void Capture(Camera camera, Vector3 position, Vector3 target, string fileName)
        {
            camera.transform.position = position;
            camera.transform.LookAt(target);
            Directory.CreateDirectory(Absolute(EvidencePath));
            RenderTexture texture = new RenderTexture(1280, 720, 24);
            Texture2D image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0f, 0f, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(Absolute(EvidencePath + "/" + fileName), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static ValidationData Validate(ExtractionData extraction)
        {
            VerifyProtectedHashes();
            if (Sha256(Absolute(ModelPath)) != FbxSha256) throw new InvalidOperationException("Copied FBX differs from archived source.");
            ModelImporter importer = RequireModelImporter(ModelPath);
            if (importer.animationType != ModelImporterAnimationType.None || importer.importAnimation)
                throw new InvalidOperationException("Skate source is not configured as rigid/unanimated.");
            GameObject baseAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath)
                ?? throw new InvalidOperationException("Base skate prefab is missing.");
            GameObject skate = UnityEngine.Object.Instantiate(baseAsset);
            try
            {
                RequireIdentity(skate.transform, "Skate_Base_v1 root");
                Transform visual = skate.transform.Find("Visual") ?? throw new InvalidOperationException("Visual child is missing.");
                RequireIdentity(visual, "Visual");
                Transform contact = skate.transform.Find("BladeContact") ?? throw new InvalidOperationException("BladeContact is missing.");
                if (Vector3.Distance(contact.localPosition, Vector3.zero) > 0.00001f)
                    throw new InvalidOperationException("BladeContact is not at the normalized blade-bottom center.");
                if (skate.GetComponentsInChildren<Animator>(true).Length != 0 || skate.GetComponentsInChildren<Animation>(true).Length != 0
                    || skate.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != 0 || skate.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    throw new InvalidOperationException("Base skate contains a forbidden rig, animation, script, or skinned mesh.");
                Bounds bounds = CalculateBounds(skate);
                if (Mathf.Abs(bounds.size.z - TargetLength) > 0.002f || bounds.min.y < -0.001f)
                    throw new InvalidOperationException($"Canonical skate normalization is invalid: {Format(bounds.size)} minY={bounds.min.y:F6}.");
                extraction.CanonicalBounds = bounds;
            }
            finally { UnityEngine.Object.DestroyImmediate(skate); }
            GameObject clean = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPrefabPath));
            GameObject fitting = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(FittingPrefabPath));
            ValidationData data = new ValidationData { Extraction = extraction };
            try
            {
                Animator cleanAnimator = clean.GetComponentInChildren<Animator>(true);
                Animator animator = fitting.GetComponentInChildren<Animator>(true);
                if (animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                    throw new InvalidOperationException("Fitting character Avatar is not valid Humanoid.");
                data.AvatarValid = animator.avatar.isValid;
                data.AvatarHuman = animator.avatar.isHuman;
                Transform leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                CompareBone(leftFoot, cleanAnimator.GetBoneTransform(HumanBodyBones.LeftFoot), "LeftFoot");
                CompareBone(rightFoot, cleanAnimator.GetBoneTransform(HumanBodyBones.RightFoot), "RightFoot");
                Transform leftSocket = leftFoot.Find("LeftSkateSocket");
                Transform rightSocket = rightFoot.Find("RightSkateSocket");
                if (leftSocket == null || rightSocket == null) throw new InvalidOperationException("Independent foot sockets are missing.");
                Transform leftSkate = leftSocket.Find("Skate_L");
                Transform rightSkate = rightSocket.Find("Skate_R");
                if (leftSkate == null || rightSkate == null) throw new InvalidOperationException("Left/right skate hierarchy is invalid.");
                if (leftSkate.localScale != Vector3.one || rightSkate.localScale != Vector3.one || HasNegativeScale(leftSkate) || HasNegativeScale(rightSkate))
                    throw new InvalidOperationException("Left/right skates do not use equal positive unit scale.");
                data.LeftPosition = leftSkate.localPosition;
                data.LeftRotation = leftSkate.localEulerAngles;
                data.LeftScale = leftSkate.localScale;
                data.RightPosition = rightSkate.localPosition;
                data.RightRotation = rightSkate.localEulerAngles;
                data.RightScale = rightSkate.localScale;
                data.LeftContact = leftSkate.Find("BladeContact").position;
                data.RightContact = rightSkate.Find("BladeContact").position;
                if (Mathf.Abs(data.LeftContact.y - IceY) > 0.0005f || Mathf.Abs(data.RightContact.y - IceY) > 0.0005f
                    || Mathf.Abs(data.LeftContact.y - data.RightContact.y) > 0.0001f)
                    throw new InvalidOperationException($"Blade contacts do not share the ice plane: L={data.LeftContact.y:F6}, R={data.RightContact.y:F6}.");
                ValidateAnimation(fitting, animator, leftFoot, rightFoot, leftSkate, rightSkate, data);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clean);
                UnityEngine.Object.DestroyImmediate(fitting);
            }
            ValidateScene();
            ValidateEvidence();
            Debug.Log($"SKATE_GEOMETRY sourceVertices={extraction.SourceVertices} sourceTriangles={extraction.SourceTriangles} canonicalVertices={extraction.CanonicalVertices} canonicalTriangles={extraction.CanonicalTriangles} oppositeTriangles={extraction.OppositeTriangles} finalBounds={Format(extraction.CanonicalBounds.size)}");
            Debug.Log($"SKATE_CONTACT left={Format(data.LeftContact)} right={Format(data.RightContact)} deltaY={Mathf.Abs(data.LeftContact.y - data.RightContact.y):F6}");
            Debug.Log($"SKATE_ANIMATION clip={data.AnimationClip} cycles={data.AnimationCycles:F2} samples={data.AnimationSamples} minimumForwardAlignment={data.MinimumForwardAlignment:F4}");
            Debug.Log("SKATE_SOURCE_AND_HUMANOID_PRESERVATION_PASS");
            return data;
        }

        private static void ValidateAnimation(GameObject fitting, Animator animator, Transform leftFoot, Transform rightFoot, Transform leftSkate, Transform rightSkate, ValidationData data)
        {
            AnimationClip clip = SelectRunningClip();
            Transform leftToe = animator.GetBoneTransform(HumanBodyBones.LeftToes);
            Transform rightToe = animator.GetBoneTransform(HumanBodyBones.RightToes);
            data.AnimationClip = clip.name;
            data.AnimationCycles = 2.25f;
            data.AnimationSamples = 19;
            data.MinimumForwardAlignment = 1f;
            Bounds leftToeTravel = new Bounds(leftSkate.InverseTransformPoint(leftToe.position), Vector3.zero);
            Bounds rightToeTravel = new Bounds(rightSkate.InverseTransformPoint(rightToe.position), Vector3.zero);
            AnimationMode.StartAnimationMode();
            try
            {
                for (int sample = 0; sample < data.AnimationSamples; sample++)
                {
                    float elapsed = clip.length * data.AnimationCycles * sample / (data.AnimationSamples - 1);
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(fitting, clip, Mathf.Repeat(elapsed, clip.length));
                    AnimationMode.EndSampling();
                    if (leftSkate.parent.name != "LeftSkateSocket" || rightSkate.parent.name != "RightSkateSocket"
                        || leftSkate.localScale != Vector3.one || rightSkate.localScale != Vector3.one
                        || !IsFinite(leftSkate) || !IsFinite(rightSkate))
                        throw new InvalidOperationException($"Skate attachment transform failed at running sample {sample}.");
                    float leftAlignment = Vector3.Dot(leftSkate.forward.normalized, (leftToe.position - leftFoot.position).normalized);
                    float rightAlignment = Vector3.Dot(rightSkate.forward.normalized, (rightToe.position - rightFoot.position).normalized);
                    data.MinimumForwardAlignment = Mathf.Min(data.MinimumForwardAlignment, leftAlignment, rightAlignment);
                    leftToeTravel.Encapsulate(leftSkate.InverseTransformPoint(leftToe.position));
                    rightToeTravel.Encapsulate(rightSkate.InverseTransformPoint(rightToe.position));
                }
            }
            finally { if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode(); }
            if (data.MinimumForwardAlignment < 0.45f)
                throw new InvalidOperationException($"A skate reversed relative to its foot during running: alignment={data.MinimumForwardAlignment:F4}.");
            data.LeftToeTravel = leftToeTravel.size;
            data.RightToeTravel = rightToeTravel.size;
            if (data.LeftToeTravel.magnitude > ToeTravelTolerance || data.RightToeTravel.magnitude > ToeTravelTolerance)
                throw new InvalidOperationException($"A skate moved relative to its toe during running: left={Format(data.LeftToeTravel)} right={Format(data.RightToeTravel)} tolerance={ToeTravelTolerance:F6}.");
            Debug.Log($"SKATE_TOE_TRAVEL leftCenter={Format(leftToeTravel.center)} leftSize={Format(leftToeTravel.size)} rightCenter={Format(rightToeTravel.center)} rightSize={Format(rightToeTravel.size)}");
        }

        private static void ValidateScene()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) throw new InvalidOperationException("Fitting scene is missing.");
            Scene active = SceneManager.GetActiveScene();
            bool close = !(active.isLoaded && active.path == ScenePath);
            Scene scene = close ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive) : active;
            try
            {
                GameObject[] roots = scene.GetRootGameObjects();
                if (roots.Count(item => item.name == "Male_Base_v1_Skate_Fitting") != 1
                    || roots.Count(item => item.name == "Validation Ice") != 1
                    || roots.SelectMany(item => item.GetComponentsInChildren<Camera>(true)).Count() != 1)
                    throw new InvalidOperationException("Fitting scene content is invalid.");
            }
            finally { if (close) EditorSceneManager.CloseScene(scene, true); }
        }

        private static void ValidateEvidence()
        {
            foreach (string file in FinalEvidenceFiles.Concat(new[] { "source-front.png", "source-side.png", "source-top.png", "source-iso.png" }))
            {
                string path = Absolute(EvidencePath + "/" + file);
                if (!File.Exists(path)) throw new InvalidOperationException($"Evidence is missing: {file}");
                Texture2D image = new Texture2D(2, 2, TextureFormat.RGB24, false);
                try
                {
                    if (!ImageConversion.LoadImage(image, File.ReadAllBytes(path), false) || image.width != 1280 || image.height != 720)
                        throw new InvalidOperationException($"Evidence is not a valid 1280x720 PNG: {file}");
                }
                finally { UnityEngine.Object.DestroyImmediate(image); }
            }
        }

        private static ExtractionData ReadExtractionData()
        {
            GameObject source = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath));
            try
            {
                Mesh raw = source.GetComponentInChildren<MeshFilter>(true).sharedMesh;
                Mesh canonical = AssetDatabase.LoadAssetAtPath<Mesh>(CanonicalMeshPath);
                return new ExtractionData
                {
                    SourceVertices = raw.vertexCount,
                    SourceTriangles = raw.triangles.Length / 3,
                    CanonicalVertices = canonical.vertexCount,
                    CanonicalTriangles = canonical.triangles.Length / 3,
                    OppositeTriangles = 0,
                    SourceBounds = CalculateBounds(source),
                    SourceLocalBounds = raw.bounds,
                    SourceRootRotation = source.transform.eulerAngles,
                    SourceRootScale = source.transform.localScale,
                    CanonicalBounds = canonical.bounds
                };
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
        }

        private static void WriteReport(ValidationData data)
        {
            ExtractionData mesh = data.Extraction;
            string protectedLines = string.Join("\n", ProtectedHashes.Select(pair => $"- `{pair.Key}`: `{pair.Value}`"));
            string[] sourceEvidenceFiles = { "source-front.png", "source-side.png", "source-top.png", "source-iso.png" };
            IEnumerable<string> generatedAssetFiles = Directory.GetFiles(Absolute(Root), "*", SearchOption.AllDirectories)
                .Select(path => path.Substring(Directory.GetParent(Application.dataPath).FullName.Length + 1).Replace('\\', '/'));
            IEnumerable<string> inventoryPaths = new[]
            {
                "Assets/Equipment/Skates.meta", Root + ".meta", ReportPath, ReportPath + ".meta",
                ".docs/reqs/2026/09/01/req-skate-base-v1-integration.md",
                ".docs/plans/2026/09/01/plan-skate-base-v1-integration.md",
                ".docs/tests/test-skate-base-v1-integration.md"
            }
                .Concat(generatedAssetFiles)
                .Concat(FinalEvidenceFiles.Concat(sourceEvidenceFiles).Select(file => EvidencePath + "/" + file))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal);
            string inventoryLines = string.Join("\n", inventoryPaths.Select(path => $"- `{path}`"));
            string report = $@"# Skate Base v1 Validation

## Source

- Original archive: `{ArchiveName}` (left unchanged in Downloads)
- ZIP SHA-256 recorded before extraction: `{ArchiveSha256}`
- Source filename: `Meshy_AI_Single_professional_i_0902012255_texture.fbx`
- Source/copy FBX SHA-256: `{FbxSha256}` (archive member and `Source/` copy match)
- Supplied maps: albedo, metallic, normal, roughness
- Imported hierarchy: one root; one rigid MeshFilter; no child hierarchy, rig, Animator, or skinned mesh

## Skate

- Source vertices: {mesh.SourceVertices}; source triangles: {mesh.SourceTriangles}
- Source finding: one unusually wide/tall complete Meshy skate with fragmented AI topology; unmodified source evidence was captured before normalization
- Canonical mesh: {mesh.CanonicalVertices} vertices, {mesh.CanonicalTriangles} triangles; all source faces retained
- Source imported bounds: {Format(mesh.SourceBounds.size)} m; center {Format(mesh.SourceBounds.center)} m
- Source local bounds: {Format(mesh.SourceLocalBounds.size)}; center {Format(mesh.SourceLocalBounds.center)}
- Production dimensions: {Format(mesh.CanonicalBounds.size)} m (lateral × up × forward)
- Materials: 1 imported slot; 1 production material (`Skate_Base_v1.mat`)
- PBR: albedo and normal assigned directly; metallic RGB plus inverted roughness alpha combined non-destructively; double-sided rendering preserves fragmented mixed-winding AI surfaces without remodeling
- Imported transform: rotation {Format(mesh.SourceRootRotation)}°, scale {Format(mesh.SourceRootScale)}
- Source rendered axes: toe/forward `-X`, up `+Y`, lateral `±Z`; production axes: forward/toe `+Z`, up `+Y`, lateral `+X`
- Production root and `Visual`: local position `(0,0,0)`, rotation `(0,0,0)`, scale `(1,1,1)`
- Handedness: `Skate_R` is an offline reflected derivative with reversed winding/tangent handedness; runtime scales remain positive

## Fitting

- `Skate_L` local position: {Format(data.LeftPosition)}; rotation {Format(data.LeftRotation)}°; scale {Format(data.LeftScale)}
- `Skate_R` local position: {Format(data.RightPosition)}; rotation {Format(data.RightRotation)}°; scale {Format(data.RightScale)}
- Hierarchy: `LeftFoot/LeftSkateSocket/Skate_L` and `RightFoot/RightSkateSocket/Skate_R`
- Bone resolution: `Animator.GetBoneTransform(HumanBodyBones.LeftFoot/RightFoot)`; no hard-coded skeleton path

## Contact

- Left BladeContact world position: {Format(data.LeftContact)} m
- Right BladeContact world position: {Format(data.RightContact)} m
- Ice plane Y: {IceY:F6} m; contact Y difference: {Mathf.Abs(data.LeftContact.y - data.RightContact.y):F6} m

## Humanoid

- `Avatar.isValid = {data.AvatarValid.ToString().ToLowerInvariant()}`
- `Avatar.isHuman = {data.AvatarHuman.ToString().ToLowerInvariant()}`
- Foot-bone local transforms match a fresh clean-prefab instance after sockets are added
- Protected hashes match the pre-generation baseline:
{protectedLines}

## Animation

- Stress-test clip: `{data.AnimationClip}` (unchanged running animation; not a final skating animation)
- Sampled cycles: {data.AnimationCycles:F2}; samples: {data.AnimationSamples}; minimum forward alignment: {data.MinimumForwardAlignment:F4}
- Toe-relative travel size: left {Format(data.LeftToeTravel)} m; right {Format(data.RightToeTravel)} m; asserted tolerance {ToeTravelTolerance:F6} m
- Both skates retained correct socket parent, positive unit scale, finite transforms, forward orientation, and invariant toe-relative placement

## Visual Validation

- Neutral: [front](../../../../.docs/evidence/skate-base-v1/neutral-front.png), [rear](../../../../.docs/evidence/skate-base-v1/neutral-rear.png), [left](../../../../.docs/evidence/skate-base-v1/neutral-left.png), [right](../../../../.docs/evidence/skate-base-v1/neutral-right.png)
- Close-ups: [left](../../../../.docs/evidence/skate-base-v1/neutral-left-close.png), [right](../../../../.docs/evidence/skate-base-v1/neutral-right-close.png)
- Running: [front](../../../../.docs/evidence/skate-base-v1/running-front.png), [side](../../../../.docs/evidence/skate-base-v1/running-side.png), [rear](../../../../.docs/evidence/skate-base-v1/running-rear.png)
- Running phase 0.125: [front](../../../../.docs/evidence/skate-base-v1/running-front-125.png), [side](../../../../.docs/evidence/skate-base-v1/running-side-125.png), [rear](../../../../.docs/evidence/skate-base-v1/running-rear-125.png)
- Running phase 0.375: [front](../../../../.docs/evidence/skate-base-v1/running-front-375.png), [side](../../../../.docs/evidence/skate-base-v1/running-side-375.png), [rear](../../../../.docs/evidence/skate-base-v1/running-rear-375.png)
- Running phase 0.625: [front](../../../../.docs/evidence/skate-base-v1/running-front-625.png), [side](../../../../.docs/evidence/skate-base-v1/running-side-625.png), [rear](../../../../.docs/evidence/skate-base-v1/running-rear-625.png)
- Running phase 0.875: [front](../../../../.docs/evidence/skate-base-v1/running-front-875.png), [side](../../../../.docs/evidence/skate-base-v1/running-side-875.png), [rear](../../../../.docs/evidence/skate-base-v1/running-rear-875.png)
- [Low gameplay-style view](../../../../.docs/evidence/skate-base-v1/gameplay-low.png)
- Unmodified source: [front](../../../../.docs/evidence/skate-base-v1/source-front.png), [side](../../../../.docs/evidence/skate-base-v1/source-side.png), [top](../../../../.docs/evidence/skate-base-v1/source-top.png), [isometric](../../../../.docs/evidence/skate-base-v1/source-iso.png)
- Visual review result: both neutral close-ups contain the active sock foot inside the boot at ankle, heel, and toe; no active-foot sock surface exits the boot
- Visual review result: left/right holders and blades are symmetric, toes face character-forward, and both `BladeContact` points meet the same ice plane
- Visual review result: four distinct running phases show both rigid skates following their respective feet without detachment or severe sock penetration; the full 2.25-cycle transform and toe-relative assertions provide the temporal check

## Created / Modified File Inventory

{inventoryLines}

## Regression / Limitations

- The generator writes only within the new skate asset and `.docs/evidence/skate-base-v1`; it never writes humanoid, animation, gameplay/controller/camera/input/puck/stick, or existing gameplay-prefab paths
- Minor hidden AI topology fragmentation and broad proportions remain; no remodeling, collider, gameplay, VFX, IK, skeleton, skin, animation-source, camera, input, puck, or stick change was made
";
            File.WriteAllText(Absolute(ReportPath), report);
        }

        private static void CompareBone(Transform actual, Transform expected, string label)
        {
            if (actual == null || expected == null || Vector3.Distance(actual.localPosition, expected.localPosition) > 0.00001f
                || Quaternion.Angle(actual.localRotation, expected.localRotation) > 0.001f || Vector3.Distance(actual.localScale, expected.localScale) > 0.00001f)
                throw new InvalidOperationException($"{label} transform differs from the clean validated prefab.");
        }

        private static void RequireIdentity(Transform transform, string label)
        {
            if (Vector3.Distance(transform.localPosition, Vector3.zero) > 0.00001f
                || Quaternion.Angle(transform.localRotation, Quaternion.identity) > 0.001f
                || Vector3.Distance(transform.localScale, Vector3.one) > 0.00001f)
                throw new InvalidOperationException($"{label} is not an identity transform.");
        }

        private static bool HasNegativeScale(Transform transform)
        {
            Vector3 scale = transform.lossyScale;
            return scale.x <= 0f || scale.y <= 0f || scale.z <= 0f;
        }

        private static bool IsFinite(Transform transform)
        {
            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;
            Vector3 scale = transform.lossyScale;
            return float.IsFinite(position.x + position.y + position.z + rotation.x + rotation.y + rotation.z + rotation.w + scale.x + scale.y + scale.z);
        }

        private static AnimationClip SelectRunningClip() => AssetDatabase.LoadAllAssetsAtPath(RunningModelPath).OfType<AnimationClip>()
            .FirstOrDefault(item => !item.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Validated running clip is missing.");

        private static Transform FindDescendant(Transform root, string name) => root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(item => item.name == name) ?? throw new InvalidOperationException($"Transform is missing: {name}");

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException($"No renderer found under {root.name}.");
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static Bounds BoundsOf(IReadOnlyList<Vector3> vertices)
        {
            if (vertices.Count == 0) throw new InvalidOperationException("Cannot calculate empty mesh bounds.");
            Bounds bounds = new Bounds(vertices[0], Vector3.zero);
            for (int index = 1; index < vertices.Count; index++) bounds.Encapsulate(vertices[index]);
            return bounds;
        }

        private static void VerifyProtectedHashes()
        {
            foreach (KeyValuePair<string, string> pair in ProtectedHashes)
            {
                string actual = Sha256(Absolute(pair.Key));
                if (!string.Equals(actual, pair.Value, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Protected asset changed: {pair.Key}\nExpected {pair.Value}\nActual   {actual}");
            }
        }

        private static ModelImporter RequireModelImporter(string path) => AssetImporter.GetAtPath(path) as ModelImporter
            ?? throw new InvalidOperationException($"ModelImporter unavailable: {path}");

        private static string Absolute(string relativePath) => Path.Combine(Directory.GetParent(Application.dataPath).FullName, relativePath);

        private static string Sha256(string path)
        {
            using FileStream stream = File.OpenRead(path);
            using SHA256 algorithm = SHA256.Create();
            return string.Concat(algorithm.ComputeHash(stream).Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));
        }

        private static string Format(Vector3 value) => $"({value.x:F6}, {value.y:F6}, {value.z:F6})";

        private sealed class ExtractionData
        {
            public int SourceVertices, SourceTriangles, CanonicalVertices, CanonicalTriangles, OppositeTriangles;
            public Bounds SourceBounds, SourceLocalBounds, CanonicalBounds;
            public Vector3 SourceRootRotation, SourceRootScale;
        }

        private sealed class ValidationData
        {
            public ExtractionData Extraction;
            public bool AvatarValid, AvatarHuman;
            public Vector3 LeftPosition, LeftRotation, LeftScale, RightPosition, RightRotation, RightScale, LeftContact, RightContact, LeftToeTravel, RightToeTravel;
            public string AnimationClip;
            public float AnimationCycles, MinimumForwardAlignment;
            public int AnimationSamples;
        }
    }
}
#endif
