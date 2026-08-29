/*
 * IceClash possession-based automatic player control policy.
 * Reacts to PuckController carrier changes: human possession selects that carrier,
 * opponent possession selects the closest defender, and a newly loose puck selects
 * the closest human-team skater once. PlayerSwitchController remains the persistent
 * manual override and performs the input/AI/marker/camera transfer.
 */

using System.Collections.Generic;
using IceClash.Core;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Gameplay
{
    public enum AutomaticControlReason { None, HumanPossession, LoosePuck, OpponentPossession }

    public sealed class PlayerControlManager : MonoBehaviour
    {
        private readonly List<PlayerController> humanTeam = new();
        private PuckController puck;
        private PlayerSwitchController switchController;
        private TeamId humanTeamId;

        public AutomaticControlReason LastAutomaticReason { get; private set; }
        public int AutomaticSelectionCount { get; private set; }

        public void Configure(IReadOnlyList<PlayerController> players, PuckController controlledPuck, PlayerSwitchController manualSwitchController)
        {
            if (puck != null) puck.CarrierChanged -= OnCarrierChanged;
            humanTeam.Clear();
            for (int i = 0; i < players.Count; i++) humanTeam.Add(players[i]);
            puck = controlledPuck;
            switchController = manualSwitchController;
            if (humanTeam.Count > 0) humanTeamId = humanTeam[0].Team;
            if (puck != null) puck.CarrierChanged += OnCarrierChanged;
        }

        private void OnDestroy()
        {
            if (puck != null) puck.CarrierChanged -= OnCarrierChanged;
        }

        private void OnCarrierChanged(PlayerController carrier)
        {
            if (switchController == null || humanTeam.Count == 0) return;

            if (carrier == null)
            {
                PlayerController closest = FindClosestHumanToPuck();
                if (closest != null) ApplyAutomaticSelection(closest, AutomaticControlReason.LoosePuck);
                return;
            }

            if (carrier.Team == humanTeamId)
            {
                ApplyAutomaticSelection(carrier, AutomaticControlReason.HumanPossession);
                return;
            }

            PlayerController defender = FindClosestHumanToPuck();
            if (defender != null) ApplyAutomaticSelection(defender, AutomaticControlReason.OpponentPossession);
        }

        private PlayerController FindClosestHumanToPuck()
        {
            Vector3 puckPosition = puck.Body != null ? puck.Body.position : puck.transform.position;
            PlayerController best = null;
            float bestDistanceSquared = float.PositiveInfinity;

            for (int i = 0; i < humanTeam.Count; i++)
            {
                PlayerController candidate = humanTeam[i];
                float distanceSquared = (candidate.transform.position - puckPosition).sqrMagnitude;
                if (distanceSquared < bestDistanceSquared)
                {
                    best = candidate;
                    bestDistanceSquared = distanceSquared;
                }
            }

            return best;
        }

        private void ApplyAutomaticSelection(PlayerController player, AutomaticControlReason reason)
        {
            LastAutomaticReason = reason;
            if (switchController.SetControlled(player)) AutomaticSelectionCount++;
        }
    }
}
