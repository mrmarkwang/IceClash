/*
 * Runs the scoped integrated-skates gameplay smoke check in Play Mode.
 * Verifies every spawned skater/goalie uses the Meshy combined visual, stable
 * non-rendering skate marker, valid Humanoid foot bindings, and root-motion-free
 * production animation without relying on retired detachable skate objects.
 */

#if UNITY_EDITOR
using System;
using System.Linq;
using IceClash.Hockey;
using IceClash.Hockey.Character;
using IceClash.Player;
using UnityEditor;
using UnityEngine;

namespace IceClash.Tests.Editor
{
    [InitializeOnLoad]
    public static class GameplaySkatesSmokeRunner
    {
        private const string PendingKey = "IceClash.GameplaySkatesSmokePending";
        private const string ExitPendingKey = "IceClash.GameplaySkatesSmokeExitPending";
        private const string ExitCodeKey = "IceClash.GameplaySkatesSmokeExitCode";
        private static double verifyAfter;
        private static int batchExitCode;
        private static double batchExitAfter;

        static GameplaySkatesSmokeRunner()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            if (Application.isBatchMode && SessionState.GetBool(ExitPendingKey, false))
            {
                batchExitCode = SessionState.GetInt(ExitCodeKey, 1);
                batchExitAfter = EditorApplication.timeSinceStartup + 1.5d;
                EditorApplication.update -= ExitBatchWhenReady;
                EditorApplication.update += ExitBatchWhenReady;
            }
        }

        public static void Run()
        {
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            verifyAfter = EditorApplication.timeSinceStartup + 0.8d;
            EditorApplication.update -= Verify;
            EditorApplication.update += Verify;
        }

        private static void Verify()
        {
            if (EditorApplication.timeSinceStartup < verifyAfter) return;
            EditorApplication.update -= Verify;
            try
            {
                PrototypeArenaSmokeCheck.RunIntegratedSkatesOnly();
                PlayerController player = UnityEngine.Object.FindObjectsByType<PlayerController>()
                    .OrderBy(item => item.PlayerId, StringComparer.Ordinal).First();
                HockeyCharacterPresentation presentation = player.GetComponent<HockeyCharacterPresentation>()
                    ?? throw new InvalidOperationException("Gameplay player presentation is missing.");
                HockeyEquipmentLoadout loadout = player.GetComponent<HockeyEquipmentLoadout>()
                    ?? throw new InvalidOperationException("Gameplay player equipment loadout is missing.");
                GameObject marker = loadout.GetEquipped(HockeyEquipmentSlot.Skates)
                    ?? throw new InvalidOperationException("Integrated-skates equipment marker is missing.");
                Transform visual = player.transform.Find("Visual/Male_Base_IntegratedSkates_Visual")
                    ?? throw new InvalidOperationException("Integrated-skates production visual is missing.");
                Animator animator = visual.GetComponentInChildren<Animator>(true)
                    ?? throw new InvalidOperationException("Integrated-skates Animator is missing.");

                if (marker.name != "Integrated Skates"
                    || marker.GetComponentsInChildren<Renderer>(true).Length != 0
                    || marker.GetComponent<HockeyPairedEquipmentFollower>() != null
                    || loadout.LeftFoot != animator.GetBoneTransform(HumanBodyBones.LeftFoot)
                    || loadout.RightFoot != animator.GetBoneTransform(HumanBodyBones.RightFoot)
                    || animator.applyRootMotion)
                    throw new InvalidOperationException("Integrated-skates runtime contract is invalid.");

                presentation.SetPreviewState(HockeyPresentationState.Running);
                animator.Update(0.25f);
                AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
                AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
                if (!current.IsName("Running") && !next.IsName("Running"))
                    throw new InvalidOperationException("Integrated-skates player did not enter Running.");

                Debug.Log("INTEGRATED_SKATES_GAMEPLAY_SMOKE_PASS states=Idle,Running skaters=10 goalies=2");
                Complete(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Complete(1);
            }
        }

        private static void Complete(int exitCode)
        {
            batchExitCode = exitCode;
            SessionState.EraseBool(PendingKey);
            if (Application.isBatchMode)
            {
                SessionState.SetBool(ExitPendingKey, true);
                SessionState.SetInt(ExitCodeKey, exitCode);
                EditorApplication.playModeStateChanged -= ExitBatchAfterPlayMode;
                EditorApplication.playModeStateChanged += ExitBatchAfterPlayMode;
            }
            EditorApplication.ExitPlaymode();
        }

        private static void ExitBatchAfterPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= ExitBatchAfterPlayMode;
            batchExitAfter = EditorApplication.timeSinceStartup + 1.5d;
            EditorApplication.update -= ExitBatchWhenReady;
            EditorApplication.update += ExitBatchWhenReady;
        }

        private static void ExitBatchWhenReady()
        {
            if (EditorApplication.timeSinceStartup < batchExitAfter
                || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= ExitBatchWhenReady;
            SessionState.EraseBool(ExitPendingKey);
            SessionState.EraseInt(ExitCodeKey);
            EditorApplication.Exit(batchExitCode);
        }
    }
}
#endif
