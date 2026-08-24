/*
 * IceClash Phase 3 in-editor smoke runner.
 * Enters Play Mode from a menu command, verifies the live roster and role behavior,
 * then stages an opponent shot to ensure AI commands release a moving physics puck
 * before exiting Play Mode without changing the scene.
 */

#if UNITY_EDITOR
using IceClash.AI;
using IceClash.Hockey;
using IceClash.Match;
using IceClash.Player;
using IceClash.Puck;
using UnityEditor;
using UnityEngine;

namespace IceClash.Tests.Editor
{
    [InitializeOnLoad]
    public static class Phase3SmokeRunner
    {
        private const string PendingKey = "IceClash.Phase3SmokePending";
        private static AiPlayerInput[] aiInputs;
        private static Vector3[] initialAiPositions;
        private static double verifyAfter;
        private static bool observedPuckSideRole;
        private static bool observedDefenderRole;
        private static PlayerController aiShooter;
        private static PuckController puck;
        private static bool observedAiShot;
        private static bool observedAiShootState;
        private static bool observedShooterPossession;
        private static int releaseSequenceBeforeShot;

        static Phase3SmokeRunner()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("IceClash/Run Phase 3 Smoke Check")]
        public static void Run()
        {
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            aiInputs = Object.FindObjectsByType<AiPlayerInput>();
            initialAiPositions = new Vector3[aiInputs.Length];
            for (int index = 0; index < aiInputs.Length; index++) initialAiPositions[index] = aiInputs[index].transform.position;
            verifyAfter = EditorApplication.timeSinceStartup + 0.75d;
            observedPuckSideRole = false;
            observedDefenderRole = false;
            EditorApplication.update -= ObserveRoles;
            EditorApplication.update += ObserveRoles;
            EditorApplication.update -= VerifyAfterWarmup;
            EditorApplication.update += VerifyAfterWarmup;
        }

        private static void ObserveRoles()
        {
            for (int index = 0; index < aiInputs.Length; index++)
            {
                observedPuckSideRole |= aiInputs[index].BehaviorState == AiBehaviorState.ChasePuck;
                observedDefenderRole |= aiInputs[index].BehaviorState == AiBehaviorState.Defend;
            }
        }

        private static void VerifyAfterWarmup()
        {
            if (EditorApplication.timeSinceStartup < verifyAfter) return;
            EditorApplication.update -= VerifyAfterWarmup;
            try
            {
                PrototypeArenaSmokeCheck.Run();
                bool allAiMoved = aiInputs.Length == 3;
                for (int index = 0; index < aiInputs.Length; index++)
                {
                    allAiMoved &= Vector3.Distance(aiInputs[index].transform.position, initialAiPositions[index]) > 0.1f;
                }

                bool snapshotsUpdated = SnapshotsFollowPlayers();
                if (!allAiMoved || !observedPuckSideRole || !observedDefenderRole || !snapshotsUpdated)
                {
                    throw new System.InvalidOperationException($"Phase 3 AI behavior was not observable: allAiMoved={allAiMoved} chase={observedPuckSideRole} defend={observedDefenderRole} snapshotsUpdated={snapshotsUpdated}");
                }
                BeginAiShotScenario();
            }
            catch
            {
                FinishAndExit();
                throw;
            }
        }

        private static void BeginAiShotScenario()
        {
            EditorApplication.update -= ObserveRoles;
            puck = Object.FindAnyObjectByType<PuckController>();
            PlayerController[] players = Object.FindObjectsByType<PlayerController>();
            aiShooter = null;
            for (int index = 0; index < aiInputs.Length; index++)
            {
                PlayerController candidate = aiInputs[index].GetComponent<PlayerController>();
                if (candidate.Team == IceClash.Core.TeamId.Red && candidate.PlayerId != puck.CarrierPlayerId)
                {
                    aiShooter = candidate;
                    break;
                }
            }

            if (puck == null || aiShooter == null) throw new System.InvalidOperationException("Unable to stage the Phase 3 opponent shooting scenario.");
            for (int index = 0; index < players.Length; index++) puck.ForceRelease(players[index]);

            CharacterController characterController = aiShooter.GetComponent<CharacterController>();
            characterController.enabled = false;
            aiShooter.transform.SetPositionAndRotation(new Vector3(3f, 1f, -1.5f), Quaternion.Euler(0f, 180f, 0f));
            characterController.enabled = true;
            Vector3 stagedPuckPosition = new(aiShooter.ControlPoint.x, 0.55f, aiShooter.ControlPoint.z);
            puck.transform.position = stagedPuckPosition;
            puck.Body.position = stagedPuckPosition;
            puck.Body.linearVelocity = Vector3.zero;
            puck.Body.angularVelocity = Vector3.zero;
            Physics.SyncTransforms();

            observedAiShot = false;
            observedAiShootState = false;
            observedShooterPossession = false;
            releaseSequenceBeforeShot = puck.ImpulseReleaseSequence;
            verifyAfter = EditorApplication.timeSinceStartup + 0.8d;
            EditorApplication.update -= ObserveAiShot;
            EditorApplication.update += ObserveAiShot;
            EditorApplication.update -= VerifyAiShot;
            EditorApplication.update += VerifyAiShot;
        }

        private static void ObserveAiShot()
        {
            observedShooterPossession |= puck != null && aiShooter != null && puck.CarrierPlayerId == aiShooter.PlayerId;
            AiPlayerInput shooterInput = aiShooter != null ? aiShooter.GetComponent<AiPlayerInput>() : null;
            observedAiShootState |= shooterInput != null && shooterInput.BehaviorState == AiBehaviorState.Shoot;
            observedAiShot |= observedShooterPossession && puck != null && aiShooter != null
                && puck.ImpulseReleaseSequence > releaseSequenceBeforeShot
                && puck.LastImpulseReleasePlayerId == aiShooter.PlayerId
                && puck.CarrierPlayerId != aiShooter.PlayerId
                && shooterInput != null && shooterInput.BehaviorState == AiBehaviorState.Shoot
                && aiShooter.State == IceClash.Core.PlayerMovementState.Shooting
                && puck.Body.linearVelocity.z < -1f;
        }

        private static void VerifyAiShot()
        {
            if (EditorApplication.timeSinceStartup < verifyAfter) return;
            EditorApplication.update -= VerifyAiShot;
            try
            {
                if (!observedShooterPossession || !observedAiShootState || !observedAiShot)
                {
                    throw new System.InvalidOperationException($"Red AI shot verification failed: possessed={observedShooterPossession} shootState={observedAiShootState} freshReleaseTowardGoal={observedAiShot}");
                }
                Debug.Log("PHASE3_EDITOR_VERIFICATION_PASS allAiMoved=true chaseRole=true defendRole=true snapshotsUpdated=true aiOpponentShot=true");
            }
            finally
            {
                FinishAndExit();
            }
        }

        private static void FinishAndExit()
        {
            EditorApplication.update -= ObserveRoles;
            EditorApplication.update -= VerifyAfterWarmup;
            EditorApplication.update -= ObserveAiShot;
            EditorApplication.update -= VerifyAiShot;
            SessionState.EraseBool(PendingKey);
            EditorApplication.ExitPlaymode();
        }

        private static bool SnapshotsFollowPlayers()
        {
            LocalMatchSetup matchSetup = Object.FindAnyObjectByType<LocalMatchSetup>();
            if (matchSetup == null) return false;
            for (int index = 0; index < aiInputs.Length; index++)
            {
                var team = aiInputs[index].GetComponent<IceClash.Player.PlayerController>().Team == IceClash.Core.TeamId.Blue
                    ? matchSetup.Data.BlueTeam : matchSetup.Data.RedTeam;
                var snapshot = team.Players.Find(data => data.PlayerId == aiInputs[index].GetComponent<IceClash.Player.PlayerController>().PlayerId);
                if (snapshot == null || Vector3.Distance(snapshot.Position, aiInputs[index].transform.position) > 0.01f) return false;
            }
            return true;
        }
    }
}
#endif
