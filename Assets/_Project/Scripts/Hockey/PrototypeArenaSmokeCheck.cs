/*
 * IceClash Phase 1 local PvE smoke assertions.
 * Verifies the generated 3v3-plus-goalies slice, one-human routing, possession-only
 * automatic control, manual switching, smooth camera retargeting, modular systems,
 * minimal mobile controls, independent puck, snapshots, and goal/reset flow.
 */

using IceClash.AI;
using IceClash.CameraSystem;
using IceClash.Core;
using IceClash.Gameplay;
using IceClash.Input;
using IceClash.Match;
using IceClash.Player;
using IceClash.Puck;
using IceClash.UI;
using UnityEngine;

namespace IceClash.Hockey
{
    public static class PrototypeArenaSmokeCheck
    {
        public static void Run()
        {
            PrototypeArenaBootstrap bootstrap = Object.FindAnyObjectByType<PrototypeArenaBootstrap>();
            if (bootstrap == null) bootstrap = new GameObject("Phase 1 PvE Smoke Arena").AddComponent<PrototypeArenaBootstrap>();
            bootstrap.BuildForValidation();

            PlayerController[] players = Object.FindObjectsByType<PlayerController>();
            HockeyPlayerAI[] skaterAi = Object.FindObjectsByType<HockeyPlayerAI>();
            HockeyGoalieAI[] goalies = Object.FindObjectsByType<HockeyGoalieAI>();
            LocalMatchSetup setup = Object.FindAnyObjectByType<LocalMatchSetup>();
            PuckController puck = Object.FindAnyObjectByType<PuckController>();
            ActionButton[] buttons = Object.FindObjectsByType<ActionButton>();
            int humanRouted = 0;
            for (int i = 0; i < players.Length; i++) if (players[i].InputSource is MobileInputSource) humanRouted++;

            bool roster = players.Length == 6 && skaterAi.Length == 6 && goalies.Length == 2
                && CountTeam(players, TeamId.Blue) == 3 && CountTeam(players, TeamId.Red) == 3 && humanRouted == 1;
            bool modular = Object.FindObjectsByType<PlayerMovementController>().Length == 6
                && Object.FindObjectsByType<StickPuckInteraction>().Length == 6
                && Object.FindObjectsByType<PassController>().Length == 6
                && Object.FindObjectsByType<ShootController>().Length == 6
                && setup != null && setup.SwitchController != null && setup.ControlManager != null && setup.MatchController != null;
            bool presentation = Object.FindAnyObjectByType<HockeyCameraController>() != null
                && Object.FindAnyObjectByType<MobileJoystick>() != null
                && Object.FindAnyObjectByType<MatchHUD>() != null
                && buttons.Length == 3 && HasButton(buttons, "PASS") && HasButton(buttons, "SHOOT") && HasButton(buttons, "SWITCH");
            bool puckIndependent = puck != null && puck.transform.parent == null && puck.Body != null;
            bool snapshots = setup != null && setup.Data.BlueTeam.Players.Count == 3 && setup.Data.RedTeam.Players.Count == 3
                && !string.IsNullOrEmpty(setup.Data.ControlledPlayerId);

            setup.MatchController.StartPlayImmediatelyForValidation();
            HockeyCameraController hockeyCamera = Object.FindAnyObjectByType<HockeyCameraController>();
            PlayerController receiver = FindPlayer(players, "blue-2");
            PlayerController expectedDefender = FindPlayer(players, "blue-3");
            PlayerController opponentCarrier = FindPlayer(players, "red-1");
            Vector3 cameraBeforeAutoSwitch = hockeyCamera.transform.position;

            StagePuckAtStick(puck, receiver);
            bool receiverClaimed = puck.TryClaim(receiver, receiver.Stick);
            bool humanPossessionAutoControl = receiverClaimed
                && setup.SwitchController.ControlledPlayer == receiver
                && setup.ControlManager.LastAutomaticReason == AutomaticControlReason.HumanPossession
                && setup.ControlManager.AutomaticSelectionCount == 1
                && hockeyCamera.Target == receiver.transform
                && hockeyCamera.transform.position == cameraBeforeAutoSwitch;

            bool releasedPass = puck.Release(receiver, receiver.transform.forward, 5f);
            bool noTrajectorySwitch = releasedPass
                && setup.SwitchController.ControlledPlayer == receiver
                && setup.ControlManager.AutomaticSelectionCount == 1;

            opponentCarrier.Movement.ResetMotion(expectedDefender.transform.position + Vector3.forward * 1.6f, Quaternion.Euler(0f, 180f, 0f));
            StagePuckAtStick(puck, opponentCarrier);
            bool opponentClaimed = puck.TryClaim(opponentCarrier, opponentCarrier.Stick);
            bool opponentPossessionAutoDefense = opponentClaimed
                && setup.SwitchController.ControlledPlayer == expectedDefender
                && setup.ControlManager.LastAutomaticReason == AutomaticControlReason.OpponentPossession
                && setup.ControlManager.AutomaticSelectionCount == 2
                && hockeyCamera.Target == expectedDefender.transform;

            setup.SwitchController.SwitchToBest();
            bool manualOverride = setup.SwitchController.ControlledPlayer != expectedDefender
                && setup.ControlManager.AutomaticSelectionCount == 2
                && hockeyCamera.Target == setup.SwitchController.ControlledPlayer.transform;

            setup.MatchController.RegisterGoal(TeamId.Blue);
            bool goalFlow = setup.MatchController.BlueScore == 1 && setup.MatchController.RedScore == 0
                && setup.MatchController.State == MatchStateSnapshot.GoalPause
                && Vector3.Distance(puck.Body.position, new Vector3(0f, 0.55f, 0f)) < 0.05f;
            setup.MatchController.ExpireImmediatelyForValidation();
            bool resultFlow = setup.MatchController.State == MatchStateSnapshot.Finished
                && setup.MatchController.RemainingSeconds == 0f && setup.MatchController.ResultText == "HUMAN TEAM WINS";

            if (!roster || !modular || !presentation || !puckIndependent || !snapshots
                || !humanPossessionAutoControl || !noTrajectorySwitch || !opponentPossessionAutoDefense
                || !manualOverride || !goalFlow || !resultFlow)
                throw new System.InvalidOperationException($"PHASE1_PVE_SMOKE_FAIL roster={roster} modular={modular} presentation={presentation} puckIndependent={puckIndependent} snapshots={snapshots} humanPossessionAutoControl={humanPossessionAutoControl} noTrajectorySwitch={noTrajectorySwitch} opponentPossessionAutoDefense={opponentPossessionAutoDefense} manualOverride={manualOverride} goalFlow={goalFlow} resultFlow={resultFlow}");

            Debug.Log("PHASE1_PVE_SMOKE_PASS skaters=6 goalies=2 humanInputs=1 aiSkaters=5 controls=PASS_SHOOT_SWITCH possessionAutoControl=true noTrajectorySwitch=true opponentAutoDefense=true manualSwitchOverride=true cameraRetargetSmooth=true puckIndependent=true goalReset=true timerResult=true");
        }

        private static int CountTeam(PlayerController[] players, TeamId team)
        {
            int count = 0;
            for (int i = 0; i < players.Length; i++) if (players[i].Team == team) count++;
            return count;
        }

        private static bool HasButton(ActionButton[] buttons, string label)
        {
            for (int i = 0; i < buttons.Length; i++) if (buttons[i].Label == label) return true;
            return false;
        }

        private static PlayerController FindPlayer(PlayerController[] players, string playerId)
        {
            for (int i = 0; i < players.Length; i++) if (players[i].PlayerId == playerId) return players[i];
            throw new System.InvalidOperationException($"Smoke check could not find player '{playerId}'.");
        }

        private static void StagePuckAtStick(PuckController puck, PlayerController player)
        {
            Vector3 position = player.Stick.ControlPoint;
            puck.ResetPuck(position);
            puck.transform.position = position;
            puck.Body.position = position;
            Physics.SyncTransforms();
        }
    }
}
