/*
 * IceClash modular skater compatibility entry point.
 * Delegates legacy setup calls to the single humanoid HockeyPlayer generator so
 * no editor path can overwrite the resource variant with a primitive capsule.
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
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Resources/Skater.prefab");
            if (prefab == null || prefab.GetComponent<IceClash.Hockey.Character.HockeyEquipmentLoadout>() == null) Create();
        }

        public static void Create() => HockeyCharacterAssetSetup.GenerateAll();
    }
}
#endif
