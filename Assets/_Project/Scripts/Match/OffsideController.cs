/*
 * IceClash local delayed-offside rule.
 * Detects mirrored premature zone entry, retains the actual early attackers,
 * interpolates their positions at swept puck crossings, clears on tag-up or
 * opponent possession, and routes stoppages to the nearest neutral-zone dot.
 */

using System;
using System.Collections.Generic;
using IceClash.Core;
using IceClash.Hockey;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Match
{
    public sealed class OffsideController : MonoBehaviour
    {
        private readonly List<PlayerController> players = new();
        private readonly Dictionary<PlayerController, Vector3> previousPlayerPositions = new();
        private readonly HashSet<PlayerController> prematureAttackers = new();
        private PuckController puck;
        private MatchController match;
        private GameObject blueOffensiveZoneWarning;
        private GameObject redOffensiveZoneWarning;
        private TeamId? warnedTeam;
        private TeamId? looseAttackTeam;
        private PlayerController sampledCarrier;
        private Vector3 previousPuckPosition;
        private bool hasPreviousPuckPosition;
#if UNITY_EDITOR
        private bool ruleEnabledForValidation = true;
#endif

        internal TeamId? WarnedTeam => warnedTeam;
        internal bool BlueWarningVisible => blueOffensiveZoneWarning != null && blueOffensiveZoneWarning.activeSelf;
        internal bool RedWarningVisible => redOffensiveZoneWarning != null && redOffensiveZoneWarning.activeSelf;
        internal GameObject BlueWarningObject => blueOffensiveZoneWarning;
        internal GameObject RedWarningObject => redOffensiveZoneWarning;

        public void Configure(IReadOnlyList<PlayerController> skaters, PuckController controlledPuck,
            MatchController matchController, GameObject blueZoneWarning, GameObject redZoneWarning)
        {
            if (skaters == null) throw new ArgumentNullException(nameof(skaters));
            if (controlledPuck == null) throw new ArgumentNullException(nameof(controlledPuck));
            if (matchController == null) throw new ArgumentNullException(nameof(matchController));

            players.Clear();
            for (int i = 0; i < skaters.Count; i++) players.Add(skaters[i]);
            if (puck != null) puck.PositionReset -= HandlePuckReset;
            if (puck != null) puck.CarrierChanged -= HandleCarrierChanged;
            puck = controlledPuck;
            puck.PositionReset += HandlePuckReset;
            puck.CarrierChanged += HandleCarrierChanged;
            match = matchController;
            blueOffensiveZoneWarning = blueZoneWarning;
            redOffensiveZoneWarning = redZoneWarning;
            HandlePuckReset(puck.Body.position);
        }

        private void OnDestroy()
        {
            if (puck != null)
            {
                puck.PositionReset -= HandlePuckReset;
                puck.CarrierChanged -= HandleCarrierChanged;
            }
        }

        private void FixedUpdate()
        {
            if (puck != null && puck.Body != null) TickRule(puck.Body.position);
        }

        private void HandlePuckReset(Vector3 position)
        {
            previousPuckPosition = position;
            hasPreviousPuckPosition = true;
            looseAttackTeam = null;
            sampledCarrier = null;
            ClearWarning();
            CapturePlayerPositions();
        }

        private void HandleCarrierChanged(PlayerController newCarrier)
        {
            if (puck == null || puck.Body == null) return;
            TickRule(puck.Body.position);
        }

        private void TickRule(Vector3 currentPuckPosition)
        {
#if UNITY_EDITOR
            if (!ruleEnabledForValidation)
            {
                ClearWarning();
                CaptureSample(currentPuckPosition);
                return;
            }
#endif
            if (puck == null || match == null || match.State != MatchStateSnapshot.Playing)
            {
                ClearWarning();
                CaptureSample(currentPuckPosition);
                return;
            }

            if (warnedTeam.HasValue)
            {
                TeamId attackingTeam = warnedTeam.Value;
                bool crossed = hasPreviousPuckPosition
                    && CrossedAttackingBlueLine(attackingTeam, previousPuckPosition, currentPuckPosition);
                if (crossed && HasPrematureAttackerAtCrossing(attackingTeam, currentPuckPosition))
                {
                    RegisterOffside(attackingTeam, currentPuckPosition);
                    return;
                }
                if (puck.PossessionTeam.HasValue && puck.PossessionTeam.Value != attackingTeam)
                {
                    ClearWarning();
                }
                else
                {
                    UpdatePrematureAttackers(attackingTeam, ContinuousCarrier(attackingTeam));
                    if (prematureAttackers.Count == 0) ClearWarning();
                }
            }

            if (!warnedTeam.HasValue)
            {
                TeamId? entryTeam = looseAttackTeam ?? (puck.Carrier != null ? puck.Carrier.Team : null);
                if (entryTeam.HasValue && hasPreviousPuckPosition
                    && CrossedAttackingBlueLine(entryTeam.Value, previousPuckPosition, currentPuckPosition)
                    && HasAnyEarlyAttackerAtCrossing(entryTeam.Value, currentPuckPosition,
                        ContinuousCarrier(entryTeam.Value)))
                {
                    RegisterOffside(entryTeam.Value, currentPuckPosition);
                    return;
                }

                if (puck.Carrier != null && !IsInsideOffensiveZone(puck.Carrier.Team, currentPuckPosition))
                {
                    UpdatePrematureAttackers(puck.Carrier.Team, puck.Carrier);
                    if (prematureAttackers.Count > 0) ShowWarning(puck.Carrier.Team);
                }
            }

            CaptureSample(currentPuckPosition);
        }

        private void UpdatePrematureAttackers(TeamId team, PlayerController excludedContinuousCarrier)
        {
            for (int i = 0; i < players.Count; i++)
            {
                PlayerController player = players[i];
                if (player == null || player.Team != team) continue;
                bool wasPremature = prematureAttackers.Contains(player);
                bool inside = IsInsideOffensiveZone(team, player.transform.position);
                if (inside && (player != excludedContinuousCarrier || wasPremature)) prematureAttackers.Add(player);
                else if (!inside) prematureAttackers.Remove(player);
            }
        }

        private void ShowWarning(TeamId team)
        {
            warnedTeam = team;
            SetWarningVisible(blueOffensiveZoneWarning, team == TeamId.Blue);
            SetWarningVisible(redOffensiveZoneWarning, team == TeamId.Red);
        }

        private void ClearWarning()
        {
            warnedTeam = null;
            prematureAttackers.Clear();
            SetWarningVisible(blueOffensiveZoneWarning, false);
            SetWarningVisible(redOffensiveZoneWarning, false);
        }

        private void RegisterOffside(TeamId attackingTeam, Vector3 crossingPosition)
        {
            Vector3 faceoffPoint = NearestOffsideFaceoffPoint(attackingTeam, crossingPosition.x);
            ClearWarning();
            looseAttackTeam = null;
            match.RegisterOffside(faceoffPoint);
        }

        private PlayerController ContinuousCarrier(TeamId team)
        {
            return sampledCarrier != null && sampledCarrier == puck.Carrier && sampledCarrier.Team == team
                ? sampledCarrier : null;
        }

        private bool HasPrematureAttackerAtCrossing(TeamId team, Vector3 currentPuckPosition)
        {
            float fraction = CrossingFraction(team, previousPuckPosition, currentPuckPosition);
            PlayerController continuousCarrier = ContinuousCarrier(team);
            for (int i = 0; i < players.Count; i++)
            {
                PlayerController player = players[i];
                if (player == null || player.Team != team) continue;
                bool eligible = prematureAttackers.Contains(player) || player != continuousCarrier;
                if (eligible && IsInsideOffensiveZone(team, InterpolatedPlayerPosition(player, fraction))) return true;
            }
            return false;
        }

        private bool HasAnyEarlyAttackerAtCrossing(TeamId team, Vector3 currentPuckPosition,
            PlayerController excludedContinuousCarrier)
        {
            float fraction = CrossingFraction(team, previousPuckPosition, currentPuckPosition);
            for (int i = 0; i < players.Count; i++)
            {
                PlayerController player = players[i];
                if (player != null && player.Team == team && player != excludedContinuousCarrier
                    && IsInsideOffensiveZone(team, InterpolatedPlayerPosition(player, fraction))) return true;
            }
            return false;
        }

        private Vector3 InterpolatedPlayerPosition(PlayerController player, float fraction)
        {
            Vector3 previous = previousPlayerPositions.TryGetValue(player, out Vector3 sampled)
                ? sampled : player.transform.position;
            return Vector3.Lerp(previous, player.transform.position, fraction);
        }

        private static float CrossingFraction(TeamId team, Vector3 previousPosition, Vector3 currentPosition)
        {
            float attack = team == TeamId.Blue ? 1f : -1f;
            float previousProgress = previousPosition.z * attack;
            float currentProgress = currentPosition.z * attack;
            return Mathf.Clamp01((PrototypeRinkGeometry.AttackingBlueLineDistance - previousProgress)
                / Mathf.Max(currentProgress - previousProgress, 0.0001f));
        }

        private void CaptureSample(Vector3 puckPosition)
        {
            previousPuckPosition = puckPosition;
            hasPreviousPuckPosition = true;
            sampledCarrier = puck.Carrier;
            if (sampledCarrier != null) looseAttackTeam = sampledCarrier.Team;
            CapturePlayerPositions();
        }

        private void CapturePlayerPositions()
        {
            for (int i = 0; i < players.Count; i++)
            {
                PlayerController player = players[i];
                if (player != null) previousPlayerPositions[player] = player.transform.position;
            }
        }

        private static void SetWarningVisible(GameObject warning, bool visible)
        {
            if (warning != null && warning.activeSelf != visible) warning.SetActive(visible);
        }

        internal static bool IsInsideOffensiveZone(TeamId team, Vector3 position)
        {
            float attack = team == TeamId.Blue ? 1f : -1f;
            return position.z * attack > PrototypeRinkGeometry.AttackingBlueLineDistance;
        }

        internal static bool CrossedAttackingBlueLine(TeamId team, Vector3 previousPosition, Vector3 currentPosition)
        {
            float attack = team == TeamId.Blue ? 1f : -1f;
            return previousPosition.z * attack <= PrototypeRinkGeometry.AttackingBlueLineDistance
                && currentPosition.z * attack > PrototypeRinkGeometry.AttackingBlueLineDistance;
        }

        internal static Vector3 NearestOffsideFaceoffPoint(TeamId attackingTeam, float crossingX)
        {
            float attack = attackingTeam == TeamId.Blue ? 1f : -1f;
            float x = crossingX < 0f ? -PrototypeRinkGeometry.NeutralFaceoffDotX : PrototypeRinkGeometry.NeutralFaceoffDotX;
            return new Vector3(x, PrototypeRinkGeometry.PuckY, PrototypeRinkGeometry.NeutralFaceoffDotZ * attack);
        }

#if UNITY_EDITOR
        internal void SetRuleEnabledForValidation(bool enabled)
        {
            ruleEnabledForValidation = enabled;
            ClearWarning();
            if (puck != null && puck.Body != null) CaptureSample(puck.Body.position);
        }

        internal void TickForValidation(Vector3 previousPosition)
        {
            previousPuckPosition = previousPosition;
            hasPreviousPuckPosition = true;
            TickRule(puck.Body.position);
        }
#endif
    }
}
