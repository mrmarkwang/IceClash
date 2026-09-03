/*
 * Imports and validates the isolated Meshy integrated-skates male character.
 * Generates a Humanoid test prefab, Air Squat controller, preview scene, numeric
 * report, and visual evidence without referencing gameplay code or v1 assets.
 */

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IceClash.CharacterValidation.Editor
{
    public static class MaleBaseV2IntegratedSkatesValidationSetup
    {
        private const string Root = "Assets/Characters/Male/Male_Base_v2_IntegratedSkates";
        private const string ModelPath = Root + "/Models/Meshy_AI_Hockey_Player_Charact_biped_Character_output.fbx";
        private const string AnimationPath = Root + "/Animations/Meshy_AI_Hockey_Player_Charact_biped_Animation_air_squat_withSkin.fbx";
        private const string ColorTexturePath = Root + "/Textures/Meshy_AI_Hockey_Player_Charact_biped_texture_0.png";
        private const string NormalTexturePath = Root + "/Textures/Meshy_AI_Hockey_Player_Charact_biped_texture_0_normal.png";
        private const string MetallicTexturePath = Root + "/Textures/Meshy_AI_Hockey_Player_Charact_biped_texture_0_metallic.png";
        private const string RoughnessTexturePath = Root + "/Textures/Meshy_AI_Hockey_Player_Charact_biped_texture_0_roughness.png";
        private const string CharacterMaterialPath = Root + "/Materials/Male_Base_v2_IntegratedSkates.mat";
        private const string GroundMaterialPath = Root + "/Materials/Validation_Ice.mat";
        private const string ControllerPath = Root + "/Animations/Air_Squat_Validation.controller";
        private const string PrefabPath = Root + "/Prefabs/Male_Base_v2_IntegratedSkates_Test.prefab";
        private const string ScenePath = Root + "/Male_Base_v2_IntegratedSkates_Test.unity";
        private const string EvidenceRelative = ".docs/evidence/meshy-integrated-skates-validation";

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

        public static void GenerateAndValidateBatch()
        {
            ConfigureTextureImporters();
            ConfigureModelImporter();
            Avatar avatar = LoadValidAvatar();
            AnimationClip clip = ConfigureAnimationImporter(avatar);
            AnimatorController controller = CreateController(clip);
            Material characterMaterial = CreateCharacterMaterial();
            Material groundMaterial = CreateGroundMaterial();
            CreatePrefab(avatar, controller, characterMaterial);
            CreateScene(groundMaterial);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ValidateAndWriteEvidence(avatar, clip);
            AssetDatabase.SaveAssets();
            Debug.Log("MESHY_V2_VALIDATION_PASS");
        }

        public static void CaptureEvidenceBatch()
        {
            Avatar avatar = LoadValidAvatar();
            AnimationClip clip = SelectAirSquatClip(LoadClips(AnimationPath));
            CaptureEvidence(avatar, clip);
            Debug.Log("MESHY_V2_CAPTURE_PASS");
        }

        private static void ConfigureTextureImporters()
        {
            ConfigureTexture(ColorTexturePath, TextureImporterType.Default, true);
            ConfigureTexture(MetallicTexturePath, TextureImporterType.Default, false);
            ConfigureTexture(RoughnessTexturePath, TextureImporterType.Default, false);
            ConfigureTexture(NormalTexturePath, TextureImporterType.NormalMap, false);
        }

        private static void ConfigureTexture(string path, TextureImporterType type, bool sRgb)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Texture importer missing: " + path);
            if (importer.textureType != type || importer.sRGBTexture != sRgb)
            {
                importer.textureType = type;
                importer.sRGBTexture = sRgb;
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureModelImporter()
        {
            ModelImporter importer = RequireModelImporter(ModelPath);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.optimizeGameObjects = false;
            importer.globalScale = 1f;
            importer.SaveAndReimport();
        }

        private static AnimationClip ConfigureAnimationImporter(Avatar avatar)
        {
            ModelImporter importer = RequireModelImporter(AnimationPath);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = avatar;
            importer.importAnimation = true;
            importer.optimizeGameObjects = false;
            importer.globalScale = 1f;
            importer.SaveAndReimport();

            ModelImporterClipAnimation[] settings = importer.defaultClipAnimations;
            if (settings.Length == 0)
                throw new InvalidOperationException("Air Squat FBX exposes no default animation clips.");
            foreach (ModelImporterClipAnimation setting in settings)
            {
                setting.loopTime = false;
                setting.loopPose = false;
                setting.keepOriginalOrientation = true;
                setting.keepOriginalPositionY = true;
                setting.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = settings;
            importer.SaveAndReimport();

            AnimationClip clip = SelectAirSquatClip(LoadClips(AnimationPath));
            if (!clip.humanMotion || clip.length <= 0f)
                throw new InvalidOperationException("Air Squat did not import as valid Humanoid motion.");
            return clip;
        }

        private static AnimatorController CreateController(AnimationClip clip)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState state in stateMachine.states.ToArray())
                stateMachine.RemoveState(state.state);
            AnimatorState airSquat = stateMachine.AddState("Air Squat (Validation Only)");
            airSquat.motion = clip;
            airSquat.speed = 1f;
            stateMachine.defaultState = airSquat;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static Material CreateCharacterMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No compatible lit shader is available.");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(CharacterMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Male_Base_v2_IntegratedSkates" };
                AssetDatabase.CreateAsset(material, CharacterMaterialPath);
            }
            material.shader = shader;
            SetTexture(material, new[] { "_BaseMap", "_MainTex" }, ColorTexturePath);
            SetTexture(material, new[] { "_BumpMap" }, NormalTexturePath);
            SetTexture(material, new[] { "_MetallicGlossMap", "_MetallicMap" }, MetallicTexturePath);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 1f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.35f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.35f);
            material.EnableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CreateGroundMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "Validation Ice" };
                AssetDatabase.CreateAsset(material, GroundMaterialPath);
            }
            Color color = new Color(0.48f, 0.72f, 0.85f, 1f);
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.75f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.75f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetTexture(Material material, IEnumerable<string> properties, string assetPath)
        {
            Texture texture = AssetDatabase.LoadAssetAtPath<Texture>(assetPath);
            if (texture == null) throw new InvalidOperationException("Texture asset missing: " + assetPath);
            foreach (string property in properties)
                if (material.HasProperty(property)) material.SetTexture(property, texture);
        }

        private static void CreatePrefab(Avatar avatar, RuntimeAnimatorController controller, Material material)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) throw new InvalidOperationException("Character model asset is missing.");
            GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (instance == null) throw new InvalidOperationException("Character model could not be instantiated.");
            try
            {
                instance.name = "Male_Base_v2_IntegratedSkates_Test";
                Animator animator = instance.GetComponentInChildren<Animator>(true) ?? instance.AddComponent<Animator>();
                animator.avatar = avatar;
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] materials = renderer.sharedMaterials;
                    for (int index = 0; index < materials.Length; index++) materials[index] = material;
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
            if (character == null) throw new InvalidOperationException("Test prefab could not be instantiated.");
            character.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Bounds bounds = CalculateBounds(character);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Neutral Ice Reference Plane";
            ground.transform.position = new Vector3(bounds.center.x, bounds.min.y - 0.002f, bounds.center.z);
            ground.transform.localScale = Vector3.one * Mathf.Max(0.5f, bounds.size.y * 0.4f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;
            Collider collider = ground.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);

            CreatePreviewCamera("Front Camera", character, Vector3.forward, true);
            CreatePreviewCamera("Side Camera", character, Vector3.left, false);
            CreatePreviewCamera("Rear Camera", character, Vector3.back, false);

            GameObject key = new GameObject("Key Light", typeof(Light));
            key.GetComponent<Light>().type = LightType.Directional;
            key.GetComponent<Light>().intensity = 1.2f;
            key.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            GameObject fill = new GameObject("Fill Light", typeof(Light));
            fill.GetComponent<Light>().type = LightType.Directional;
            fill.GetComponent<Light>().intensity = 0.55f;
            fill.transform.rotation = Quaternion.Euler(25f, 145f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.38f);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static Camera CreatePreviewCamera(string name, GameObject character, Vector3 direction, bool active)
        {
            GameObject cameraObject = new GameObject(name, typeof(Camera));
            Camera camera = cameraObject.GetComponent<Camera>();
            if (active) camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.065f);
            camera.nearClipPlane = 0.01f;
            camera.fieldOfView = 30f;
            FrameCamera(camera, character, direction);
            cameraObject.SetActive(active);
            return camera;
        }

        private static void ValidateAndWriteEvidence(Avatar avatar, AnimationClip clip)
        {
            ModelImporter modelImporter = RequireModelImporter(ModelPath);
            if (modelImporter.animationType != ModelImporterAnimationType.Human ||
                modelImporter.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
                throw new InvalidOperationException("Character FBX is not Humanoid/Create From This Model.");
            if (!avatar.isValid || !avatar.isHuman)
                throw new InvalidOperationException("Character Avatar is invalid or non-human.");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null) throw new InvalidOperationException("Test prefab is missing.");
            Animator prefabAnimator = prefab.GetComponentInChildren<Animator>(true);
            if (prefabAnimator == null || prefabAnimator.avatar != avatar || prefabAnimator.runtimeAnimatorController == null)
                throw new InvalidOperationException("Test prefab Animator is not wired to the v2 Avatar/controller.");
            if (prefabAnimator.applyRootMotion) throw new InvalidOperationException("Test Animator must not apply root motion.");
            if (prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                throw new InvalidOperationException("Test prefab contains MonoBehaviour scripts.");
            if (prefab.GetComponentsInChildren<Collider>(true).Length != 0 || prefab.GetComponentsInChildren<Rigidbody>(true).Length != 0)
                throw new InvalidOperationException("Test prefab contains physics components.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("Validation scene is missing.");

            StringBuilder report = new StringBuilder();
            report.AppendLine("MESHY V2 INTEGRATED SKATES UNITY VALIDATION");
            report.AppendLine("model=" + ModelPath);
            report.AppendLine("animation=" + AnimationPath);
            report.AppendLine("avatar.valid=" + avatar.isValid.ToString().ToLowerInvariant());
            report.AppendLine("avatar.human=" + avatar.isHuman.ToString().ToLowerInvariant());
            report.AppendLine("model.importScale=" + modelImporter.globalScale.ToString("F6", CultureInfo.InvariantCulture));
            report.AppendLine("model.materialSubassets=" + AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>().Count());
            report.AppendLine("model.textureSubassets=" + AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Texture>().Count());
            report.AppendLine("model.clips=" + FormatClipNames(LoadClips(ModelPath)));
            report.AppendLine("animation.clips=" + FormatClipNames(LoadClips(AnimationPath)));
            AppendClipReports(report, "model", LoadClips(ModelPath));
            AppendClipReports(report, "animation", LoadClips(AnimationPath));

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                Animator animator = instance.GetComponentInChildren<Animator>(true);
                Bounds bounds = CalculateBounds(instance);
                report.AppendLine("character.bounds=" + FormatBounds(bounds));
                report.AppendLine("character.height=" + F(bounds.size.y));
                Transform[] transforms = instance.GetComponentsInChildren<Transform>(true);
                report.AppendLine("skeleton.transformCount=" + transforms.Length);
                Transform armature = transforms.FirstOrDefault(t => t.name == "Armature");
                int boneCount = armature != null ? armature.GetComponentsInChildren<Transform>(true).Length - 1 : 0;
                report.AppendLine("skeleton.boneCount=" + boneCount);
                report.AppendLine("skeleton.hierarchy:");
                AppendHierarchy(report, instance.transform, 0);
                foreach (HumanBodyBones requiredBone in RequiredBones)
                {
                    Transform mapped = animator.GetBoneTransform(requiredBone);
                    if (mapped == null) throw new InvalidOperationException("Required Humanoid bone unmapped: " + requiredBone);
                    report.AppendLine("humanoid." + requiredBone + "=" + mapped.name);
                }
                string toeNames = string.Join(",", transforms.Where(t => t.name.IndexOf("toe", StringComparison.OrdinalIgnoreCase) >= 0).Select(t => t.name));
                report.AppendLine("skeleton.toeBones=" + (toeNames.Length == 0 ? "<none>" : toeNames));

                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                MeshFilter[] meshFilters = instance.GetComponentsInChildren<MeshFilter>(true);
                SkinnedMeshRenderer[] skinned = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                int vertices = meshFilters.Where(m => m.sharedMesh != null).Sum(m => m.sharedMesh.vertexCount) + skinned.Where(m => m.sharedMesh != null).Sum(m => m.sharedMesh.vertexCount);
                int triangles = meshFilters.Where(m => m.sharedMesh != null).Sum(m => m.sharedMesh.triangles.Length / 3) + skinned.Where(m => m.sharedMesh != null).Sum(m => m.sharedMesh.triangles.Length / 3);
                int materialCount = renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Distinct().Count();
                report.AppendLine("mesh.count=" + (meshFilters.Length + skinned.Length));
                report.AppendLine("mesh.vertices=" + vertices);
                report.AppendLine("mesh.triangles=" + triangles);
                report.AppendLine("mesh.materials=" + materialCount);
                foreach (SkinnedMeshRenderer renderer in skinned)
                {
                    report.AppendLine("skinnedMesh." + renderer.name + ".rootBone=" + (renderer.rootBone != null ? renderer.rootBone.name : "<none>"));
                    AppendSkateInfluences(report, renderer, bounds);
                }

                report.AppendLine("animation.clip=" + clip.name);
                report.AppendLine("animation.duration=" + F(clip.length));
                report.AppendLine("animation.fps=" + F(clip.frameRate));
                report.AppendLine("animation.frames=" + (Mathf.RoundToInt(clip.length * clip.frameRate) + 1));
                report.AppendLine("animation.humanMotion=" + clip.humanMotion.ToString().ToLowerInvariant());
                report.AppendLine("animation.rootMotionCurves=" + clip.hasRootCurves.ToString().ToLowerInvariant());
                report.AppendLine("animation.loop=" + clip.isLooping.ToString().ToLowerInvariant());

                float deepest = FindDeepestNormalizedTime(instance, animator, clip);
                report.AppendLine("animation.deepestNormalizedTime=" + F(deepest));
                float[] phases = { 0f, deepest * 0.5f, deepest, deepest + (1f - deepest) * 0.5f, 1f };
                string[] names = { "standing", "bend", "deepest", "recovery", "end" };
                for (int index = 0; index < phases.Length; index++)
                {
                    clip.SampleAnimation(instance, clip.length * phases[index]);
                    Physics.SyncTransforms();
                    report.AppendLine("phase." + names[index] + ".normalized=" + F(phases[index]));
                    AppendPose(report, names[index], animator);
                    AppendBladeGeometry(report, names[index], skinned, instance.transform);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }

            string evidenceDirectory = AbsoluteEvidenceDirectory();
            Directory.CreateDirectory(evidenceDirectory);
            File.WriteAllText(Path.Combine(evidenceDirectory, "unity-validation-report.txt"), report.ToString());
            Debug.Log(report.ToString());
            CaptureEvidence(avatar, clip);
        }

        private static void AppendSkateInfluences(StringBuilder report, SkinnedMeshRenderer renderer, Bounds characterBounds)
        {
            Mesh mesh = renderer.sharedMesh;
            if (mesh == null || mesh.boneWeights.Length != mesh.vertexCount) return;
            Vector3[] vertices = mesh.vertices;
            BoneWeight[] weights = mesh.boneWeights;
            float cutoff = characterBounds.min.y + characterBounds.size.y * 0.10f;
            Dictionary<string, float> left = new Dictionary<string, float>();
            Dictionary<string, float> right = new Dictionary<string, float>();
            int leftCount = 0;
            int rightCount = 0;
            for (int index = 0; index < vertices.Length; index++)
            {
                Vector3 world = renderer.transform.TransformPoint(vertices[index]);
                if (world.y > cutoff) continue;
                bool isLeft = world.x < characterBounds.center.x;
                Dictionary<string, float> target = isLeft ? left : right;
                if (isLeft) leftCount++; else rightCount++;
                AddWeight(target, renderer, weights[index].boneIndex0, weights[index].weight0);
                AddWeight(target, renderer, weights[index].boneIndex1, weights[index].weight1);
                AddWeight(target, renderer, weights[index].boneIndex2, weights[index].weight2);
                AddWeight(target, renderer, weights[index].boneIndex3, weights[index].weight3);
            }
            report.AppendLine("skates.left.bottomVertexCount=" + leftCount);
            report.AppendLine("skates.right.bottomVertexCount=" + rightCount);
            report.AppendLine("skates.left.influences=" + FormatWeights(left));
            report.AppendLine("skates.right.influences=" + FormatWeights(right));
        }

        private static void AddWeight(Dictionary<string, float> target, SkinnedMeshRenderer renderer, int boneIndex, float weight)
        {
            if (weight <= 0f || boneIndex < 0 || boneIndex >= renderer.bones.Length) return;
            string name = renderer.bones[boneIndex] != null ? renderer.bones[boneIndex].name : "<missing>";
            target[name] = target.TryGetValue(name, out float value) ? value + weight : weight;
        }

        private static string FormatWeights(Dictionary<string, float> weights)
        {
            return string.Join(",", weights.OrderByDescending(pair => pair.Value).Take(8).Select(pair => pair.Key + ":" + F(pair.Value)));
        }

        private static void AppendPose(StringBuilder report, string phase, Animator animator)
        {
            HumanBodyBones[] bones =
            {
                HumanBodyBones.Hips, HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg,
                HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg,
                HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot,
                HumanBodyBones.LeftHand, HumanBodyBones.RightHand
            };
            foreach (HumanBodyBones bone in bones)
            {
                Transform transform = animator.GetBoneTransform(bone);
                if (transform != null)
                    report.AppendLine("pose." + phase + "." + bone + "=" + FormatVector(transform.position) + "|" + FormatVector(transform.eulerAngles));
            }
        }

        private static void AppendBladeGeometry(StringBuilder report, string phase, IEnumerable<SkinnedMeshRenderer> renderers, Transform root)
        {
            List<Vector3> vertices = new List<Vector3>();
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                Mesh baked = new Mesh();
                try
                {
                    renderer.BakeMesh(baked);
                    vertices.AddRange(baked.vertices.Select(v => renderer.transform.TransformPoint(v)));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(baked);
                }
            }
            if (vertices.Count == 0) return;
            float minY = vertices.Min(v => v.y);
            float maxY = vertices.Max(v => v.y);
            float cutoff = minY + (maxY - minY) * 0.035f;
            List<Vector3> contact = vertices.Where(v => v.y <= cutoff).ToList();
            List<Vector3> left = contact.Where(v => v.x < root.position.x).ToList();
            List<Vector3> right = contact.Where(v => v.x >= root.position.x).ToList();
            report.AppendLine("blade." + phase + ".rootRelativeContactHeight=" + F(minY - root.position.y));
            report.AppendLine("blade." + phase + ".left=" + FormatPointBounds(left));
            report.AppendLine("blade." + phase + ".right=" + FormatPointBounds(right));
        }

        private static string FormatPointBounds(List<Vector3> points)
        {
            if (points.Count == 0) return "<none>";
            Bounds bounds = new Bounds(points[0], Vector3.zero);
            foreach (Vector3 point in points.Skip(1)) bounds.Encapsulate(point);
            return "count:" + points.Count + " bounds:" + FormatBounds(bounds);
        }

        private static float FindDeepestNormalizedTime(GameObject instance, Animator animator, AnimationClip clip)
        {
            Transform hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            float deepestTime = 0f;
            float lowest = float.PositiveInfinity;
            for (int index = 0; index <= 40; index++)
            {
                float normalized = index / 40f;
                clip.SampleAnimation(instance, clip.length * normalized);
                if (hips != null && hips.position.y < lowest)
                {
                    lowest = hips.position.y;
                    deepestTime = normalized;
                }
            }
            return deepestTime;
        }

        private static void CaptureEvidence(Avatar avatar, AnimationClip clip)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject character = GameObject.Find("Male_Base_v2_IntegratedSkates_Test");
            Camera camera = Camera.main;
            Animator animator = character != null ? character.GetComponentInChildren<Animator>(true) : null;
            if (character == null || camera == null || animator == null)
                throw new InvalidOperationException("Validation scene is missing character, Animator, or Main Camera.");
            animator.avatar = avatar;
            float deepest = FindDeepestNormalizedTime(character, animator, clip);
            float[] phases = { 0f, deepest * 0.5f, deepest, deepest + (1f - deepest) * 0.5f, 1f };
            string[] names = { "standing", "bend", "deepest", "recovery", "end" };
            string directory = AbsoluteEvidenceDirectory();
            Directory.CreateDirectory(directory);

            for (int index = 0; index < phases.Length; index++)
            {
                clip.SampleAnimation(character, clip.length * phases[index]);
                FrameCamera(camera, character, Vector3.forward);
                CaptureCamera(camera, Path.Combine(directory, names[index] + "-front.png"));
            }
            clip.SampleAnimation(character, 0f);
            FrameSkates(camera, character, Vector3.forward);
            CaptureCamera(camera, Path.Combine(directory, "standing-skates-front.png"));
            FrameSkates(camera, character, Vector3.left);
            CaptureCamera(camera, Path.Combine(directory, "standing-skates-side.png"));
            clip.SampleAnimation(character, clip.length * deepest);
            FrameCamera(camera, character, Vector3.left);
            CaptureCamera(camera, Path.Combine(directory, "deepest-side.png"));
            FrameCamera(camera, character, Vector3.back);
            CaptureCamera(camera, Path.Combine(directory, "deepest-rear.png"));
            FrameSkates(camera, character, Vector3.forward);
            CaptureCamera(camera, Path.Combine(directory, "deepest-skates-front.png"));
            FrameSkates(camera, character, Vector3.left);
            CaptureCamera(camera, Path.Combine(directory, "deepest-skates-side.png"));
            Debug.Log("MESHY_V2_EVIDENCE path=" + EvidenceRelative + " images=11");
        }

        private static void FrameCamera(Camera camera, GameObject character, Vector3 direction)
        {
            Bounds bounds = CalculateBounds(character);
            Vector3 target = bounds.center;
            float distance = Mathf.Max(bounds.size.y * 2.1f, 2f);
            camera.transform.position = target + direction.normalized * distance;
            camera.transform.LookAt(target);
        }

        private static void FrameSkates(Camera camera, GameObject character, Vector3 direction)
        {
            Bounds bounds = CalculateBounds(character);
            Vector3 target = new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * 0.10f, bounds.center.z);
            float distance = Mathf.Max(bounds.size.y * 0.62f, 0.65f);
            camera.transform.position = target + direction.normalized * distance;
            camera.transform.LookAt(target);
        }

        private static void CaptureCamera(Camera camera, string path)
        {
            const int width = 1280;
            const int height = 960;
            RenderTexture texture = new RenderTexture(width, height, 24);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void AppendHierarchy(StringBuilder report, Transform transform, int depth)
        {
            report.AppendLine(new string(' ', depth * 2) + transform.name);
            for (int index = 0; index < transform.childCount; index++)
                AppendHierarchy(report, transform.GetChild(index), depth + 1);
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return new Bounds(Vector3.up, new Vector3(1, 2, 1));
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        private static Avatar LoadValidAvatar()
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null) throw new InvalidOperationException("Unity created no Avatar for the character FBX.");
            if (!avatar.isValid) throw new InvalidOperationException("Unity character Avatar is invalid.");
            if (!avatar.isHuman) throw new InvalidOperationException("Unity character Avatar is not Humanoid.");
            return avatar;
        }

        private static ModelImporter RequireModelImporter(string path)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("ModelImporter missing: " + path);
            return importer;
        }

        private static IReadOnlyList<AnimationClip> LoadClips(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .OrderBy(clip => clip.name, StringComparer.Ordinal).ToArray();
        }

        private static AnimationClip SelectAirSquatClip(IReadOnlyList<AnimationClip> clips)
        {
            if (clips.Count == 0) throw new InvalidOperationException("Air Squat FBX contains no animation clips.");
            return clips.FirstOrDefault(clip => clip.name.IndexOf("squat", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? clips.OrderByDescending(clip => clip.length).First();
        }

        private static string FormatClipNames(IReadOnlyList<AnimationClip> clips)
        {
            return clips.Count == 0 ? "<none>" : string.Join("|", clips.Select(clip => clip.name));
        }

        private static void AppendClipReports(StringBuilder report, string source, IReadOnlyList<AnimationClip> clips)
        {
            for (int index = 0; index < clips.Count; index++)
            {
                AnimationClip item = clips[index];
                string prefix = source + ".clip" + index + ".";
                report.AppendLine(prefix + "name=" + item.name);
                report.AppendLine(prefix + "duration=" + F(item.length));
                report.AppendLine(prefix + "fps=" + F(item.frameRate));
                report.AppendLine(prefix + "frames=" + (Mathf.RoundToInt(item.length * item.frameRate) + 1));
                report.AppendLine(prefix + "rootMotionCurves=" + item.hasRootCurves.ToString().ToLowerInvariant());
                report.AppendLine(prefix + "loop=" + item.isLooping.ToString().ToLowerInvariant());
            }
        }

        private static string AbsoluteEvidenceDirectory()
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, EvidenceRelative);
        }

        private static string FormatBounds(Bounds bounds)
        {
            return "min:" + FormatVector(bounds.min) + " max:" + FormatVector(bounds.max) + " size:" + FormatVector(bounds.size);
        }

        private static string FormatVector(Vector3 value)
        {
            return F(value.x) + "," + F(value.y) + "," + F(value.z);
        }

        private static string F(float value)
        {
            return value.ToString("F6", CultureInfo.InvariantCulture);
        }
    }
}
#endif
