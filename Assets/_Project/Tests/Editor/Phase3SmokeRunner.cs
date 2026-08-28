/*
 * IceClash Phase 1 in-editor PvE smoke runner.
 * Enters Play Mode from a menu command, warms the runtime-built arena, runs the
 * complete structural/goal-flow assertions, logs one pass marker, and exits.
 * Batch execution returns a truthful process code after Play Mode finishes.
 */

#if UNITY_EDITOR
using IceClash.Hockey;
using UnityEditor;
using UnityEngine;

namespace IceClash.Tests.Editor
{
    [InitializeOnLoad]
    public static class Phase3SmokeRunner
    {
        private const string PendingKey = "IceClash.Phase1PveSmokePending";
        private static double verifyAfter;
        private static int batchExitCode;

        static Phase3SmokeRunner()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("IceClash/Run Phase 1 PvE Smoke Check")]
        public static void Run()
        {
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            verifyAfter = EditorApplication.timeSinceStartup + 0.7d;
            EditorApplication.update -= Verify;
            EditorApplication.update += Verify;
        }

        private static void Verify()
        {
            if (EditorApplication.timeSinceStartup < verifyAfter) return;
            EditorApplication.update -= Verify;
            try
            {
                PrototypeArenaSmokeCheck.Run();
                batchExitCode = 0;
            }
            catch (System.Exception exception)
            {
                batchExitCode = 1;
                Debug.LogException(exception);
            }
            finally
            {
                SessionState.EraseBool(PendingKey);
                if (Application.isBatchMode)
                {
                    EditorApplication.playModeStateChanged -= ExitBatchAfterPlayMode;
                    EditorApplication.playModeStateChanged += ExitBatchAfterPlayMode;
                }
                EditorApplication.ExitPlaymode();
            }
        }

        private static void ExitBatchAfterPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= ExitBatchAfterPlayMode;
            EditorApplication.Exit(batchExitCode);
        }
    }
}
#endif
