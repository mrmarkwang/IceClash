/*
 * IceClash Phase 1 scene setup utility.
 * Uses Unity's scene serializer to create the intentionally empty PrototypeArena scene;
 * runtime bootstrap then constructs placeholder gameplay objects when the scene starts.
 */

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IceClash.Tests.Editor
{
    public static class PrototypeArenaSceneSetup
    {
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (!EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/PrototypeArena.unity"))
            {
                throw new System.InvalidOperationException("Unable to save PrototypeArena.unity.");
            }

            Debug.Log("PROTOTYPE_ARENA_SCENE_CREATED");
        }
    }
}
#endif
