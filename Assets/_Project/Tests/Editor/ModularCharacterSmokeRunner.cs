/*
 * IceClash ten-character scene smoke runner.
 * Loads the generated modular scene, waits for its deterministic harness, and
 * returns a truthful batch process code with a named pass marker.
 */

#if UNITY_EDITOR
using IceClash.Hockey.Character;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IceClash.Tests.Editor
{
    [InitializeOnLoad]
    public static class ModularCharacterSmokeRunner
    {
        private const string PendingKey = "IceClash.ModularCharacterSmokePending";
        private const string ScenePath = "Assets/_Project/Scenes/ModularCharacterTest.unity";
        private static double timeoutAt;

        static ModularCharacterSmokeRunner()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("IceClash/Run Modular Character Smoke Check")]
        public static void Run()
        {
            EditorSceneManager.OpenScene(ScenePath);
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
        }

        public static void RunBatch() => Run();

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            timeoutAt = EditorApplication.timeSinceStartup + 20d;
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            ModularCharacterTestHarness harness = Object.FindAnyObjectByType<ModularCharacterTestHarness>();
            if (harness != null && harness.Passed)
            {
                Complete(0);
                return;
            }
            if (harness != null && !string.IsNullOrEmpty(harness.Failure))
            {
                Debug.LogError("MODULAR_CHARACTER_SMOKE_FAIL " + harness.Failure);
                Complete(1);
                return;
            }
            if (EditorApplication.timeSinceStartup >= timeoutAt)
            {
                Debug.LogError("MODULAR_CHARACTER_SMOKE_FAIL timeout");
                Complete(1);
            }
        }

        private static void Complete(int exitCode)
        {
            EditorApplication.update -= Poll;
            SessionState.EraseBool(PendingKey);
            if (Application.isBatchMode)
            {
                EditorApplication.playModeStateChanged -= state => ExitAfterPlay(state, exitCode);
                EditorApplication.playModeStateChanged += state => ExitAfterPlay(state, exitCode);
            }
            EditorApplication.ExitPlaymode();
        }

        private static void ExitAfterPlay(PlayModeStateChange state, int exitCode)
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.Exit(exitCode);
        }
    }
}
#endif
