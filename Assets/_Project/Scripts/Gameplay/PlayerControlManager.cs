/*
 * IceClash possession-based automatic player control policy.
 * Reacts only to established PuckController carrier changes: human possession
 * selects that carrier, opponent possession selects a useful defender, and free
 * puck movement never changes control. PlayerSwitchController remains the manual
 * override and performs the actual input/AI/marker/camera transfer.
 */

using System.Collections.Generic;
using IceClash.Core;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Gameplay
{
    public enum AutomaticControlReason { None, HumanPossession, OpponentPossession }

    public sealed class PlayerControlManager : MonoBehaviour
    {
        [Header("Defensive selection")]
        [SerializeField, Min(0f)] private float challengeDistanceWeight = 1.35f;
        [SerializeField, Min(0f)] private float goalSideWeight = 1.1f;
        [SerializeField, Min(0f)] private float approachVelocityWeight = 0.18f;
        [SerializeField, Min(0f)] private float currentPlayerStabilityBonus = 0.25f;
        [SerializeField, Min(0f)] private float challengeOffsetFromCarrier = 1.25f;

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
            // A null carrier is a pass, shot, save, or other free-puck state. Rule 5:
            // never infer the next controlled player from puck direction or proximity.
            if (carrier == null || switchController == null || humanTeam.Count == 0) return;

            if (carrier.Team == humanTeamId)
            {
                ApplyAutomaticSelection(carrier, AutomaticControlReason.HumanPossession);
                return;
            }

            PlayerController defender = FindBestDefender(carrier);
            if (defender != null) ApplyAutomaticSelection(defender, AutomaticControlReason.OpponentPossession);
        }

        private PlayerController FindBestDefender(PlayerController opponentCarrier)
        {
            Vector3 ownGoal = humanTeamId == TeamId.Blue ? new Vector3(0f, 1f, -14.25f) : new Vector3(0f, 1f, 14.25f);
            Vector3 goalSideDirection = Vector3.ProjectOnPlane(ownGoal - opponentCarrier.transform.position, Vector3.up).normalized;
            Vector3 challengePoint = opponentCarrier.transform.position + goalSideDirection * challengeOffsetFromCarrier;
            PlayerController best = null;
            float bestScore = float.NegativeInfinity;

            for (int i = 0; i < humanTeam.Count; i++)
            {
                PlayerController candidate = humanTeam[i];
                float challengeDistance = Vector3.Distance(candidate.transform.position, challengePoint);
                Vector3 carrierToCandidate = Vector3.ProjectOnPlane(candidate.transform.position - opponentCarrier.transform.position, Vector3.up);
                float goalSideAlignment = carrierToCandidate.sqrMagnitude > 0.01f
                    ? Mathf.Clamp01((Vector3.Dot(carrierToCandidate.normalized, goalSideDirection) + 1f) * 0.5f)
                    : 1f;
                Vector3 towardCarrier = Vector3.ProjectOnPlane(opponentCarrier.transform.position - candidate.transform.position, Vector3.up).normalized;
                float approachVelocity = candidate.Movement != null ? Mathf.Max(0f, Vector3.Dot(candidate.Movement.Velocity, towardCarrier)) : 0f;
                float score = -challengeDistance * challengeDistanceWeight
                    + goalSideAlignment * goalSideWeight
                    + approachVelocity * approachVelocityWeight
                    + (candidate == switchController.ControlledPlayer ? currentPlayerStabilityBonus : 0f);
                if (score > bestScore) { best = candidate; bestScore = score; }
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
