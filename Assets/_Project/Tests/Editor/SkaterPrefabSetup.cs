/*
 * IceClash Phase 3 skater prefab generator.
 * Creates the reusable primitive skater shell used by both local and AI-controlled
 * players while leaving identity and command-source wiring to LocalMatchSetup.
 */

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace IceClash.Tests.Editor
{
    [InitializeOnLoad]
    public static class SkaterPrefabSetup
    {
        static SkaterPrefabSetup()
        {
            EditorApplication.delayCall += EnsureExists;
        }

        private static void EnsureExists()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Resources/Skater.prefab") == null) Create();
        }

        public static void Create()
        {
            const string directory = "Assets/_Project/Prefabs/Resources";
            const string prefabPath = directory + "/Skater.prefab";
            if (!AssetDatabase.IsValidFolder(directory)) AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Resources");

            GameObject skater = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            skater.name = "Skater";
            Object.DestroyImmediate(skater.GetComponent<Collider>());
            CharacterController controller = skater.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.45f;
            PrefabUtility.SaveAsPrefabAsset(skater, prefabPath);
            Object.DestroyImmediate(skater);
            AssetDatabase.SaveAssets();
            Debug.Log("PHASE3_SKATER_PREFAB_CREATED path=" + prefabPath);
        }
    }
}
#endif
