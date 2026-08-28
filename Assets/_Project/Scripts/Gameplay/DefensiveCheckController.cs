/*
 * IceClash team-aware contextual defensive check controller.
 * Resolves human and configured opponent-AI checks as close body or forward pull
 * challenges. Each team has its own shared cooldown, and successful checks dislodge
 * only the expected opposing carrier without assigning possession.
 */

using IceClash.Core;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Gameplay
{
    public enum DefensiveCheckResult { None, BodyCheck, PullCheck }

    public sealed class DefensiveCheckController : MonoBehaviour
    {
        private IPlayerInput input;
        private PlayerSwitchController switchController;
        private PuckController puck;
        private DefensiveCheckTuning tuning;
        private TeamId humanTeam = TeamId.Blue;
        private bool gameplayEnabled;
        private float nextHumanTeamCheckTime;
        private float nextOpponentTeamCheckTime;
        private MatchStateSnapshot lastMatchState = MatchStateSnapshot.Setup;

        public DefensiveCheckResult LastResult { get; private set; }
        public int SuccessfulCheckCount { get; private set; }
        public float NextCheckTime => nextHumanTeamCheckTime;
        public DefensiveCheckTuning Tuning => tuning;
        internal float MaximumRange => tuning != null ? tuning.RuntimeValues.PullRange : 0f;

        public void Configure(IPlayerInput playerInput, PlayerSwitchController manualSwitchController,
            PuckController controlledPuck, DefensiveCheckTuning checkTuning)
        {
            input = playerInput;
            switchController = manualSwitchController;
            puck = controlledPuck;
            tuning = checkTuning;
            if (switchController != null && switchController.ControlledPlayer != null)
                humanTeam = switchController.ControlledPlayer.Team;
            GameplayEvents.MatchChanged -= OnMatchChanged;
            GameplayEvents.MatchChanged += OnMatchChanged;
        }

        private void OnDestroy() => GameplayEvents.MatchChanged -= OnMatchChanged;

        private void Update()
        {
            if (gameplayEnabled && input != null && input.CheckPressed) TryCheck();
        }

        public DefensiveCheckResult TryCheck()
        {
            PlayerController checker = switchController != null ? switchController.ControlledPlayer : null;
            return TryCheck(checker);
        }

        internal DefensiveCheckResult TryCheck(PlayerController checker)
        {
            LastResult = DefensiveCheckResult.None;
            if (!gameplayEnabled || checker == null || puck == null || tuning == null) return LastResult;

            float nextTeamCheckTime = checker.Team == humanTeam
                ? nextHumanTeamCheckTime
                : nextOpponentTeamCheckTime;
            if (Time.time < nextTeamCheckTime) return LastResult;

            PlayerController carrier = puck.Carrier;
            if (carrier == null || carrier.Team == checker.Team
                || !checker.GameplayEnabled || checker.Movement == null || carrier.Movement == null) return LastResult;

            DefensiveCheckTuning.Values values = tuning.RuntimeValues;
            Vector3 checkerToCarrier = Vector3.ProjectOnPlane(carrier.transform.position - checker.transform.position, Vector3.up);
            float distance = checkerToCarrier.magnitude;
            Vector3 direction = distance > 0.001f ? checkerToCarrier / distance : checker.transform.forward;

            if (distance <= values.BodyRange)
            {
                if (!puck.Dislodge(carrier, checker, direction, values.BodyPuckSpeed)) return LastResult;
                carrier.Movement.ApplyExternalImpulse(direction * values.BodyImpulse,
                    DefensiveCheckTuning.MaximumBodyImpulse, values.ImpulseDecay);
                checker.Movement.ApplyExternalImpulse(-direction * values.BodyImpulse * 0.35f,
                    DefensiveCheckTuning.MaximumBodyImpulse, values.ImpulseDecay);
                LastResult = DefensiveCheckResult.BodyCheck;
            }
            else
            {
                Vector3 checkerForward = Vector3.ProjectOnPlane(checker.transform.forward, Vector3.up).normalized;
                if (distance > values.PullRange || Vector3.Dot(checkerForward, direction) < values.PullForwardDot) return LastResult;
                if (!puck.Dislodge(carrier, checker, -direction, values.PullPuckSpeed)) return LastResult;
                LastResult = DefensiveCheckResult.PullCheck;
            }

            SuccessfulCheckCount++;
            float cooldownUntil = Time.time + values.CooldownSeconds;
            if (checker.Team == humanTeam) nextHumanTeamCheckTime = cooldownUntil;
            else nextOpponentTeamCheckTime = cooldownUntil;
            return LastResult;
        }

        internal void SetGameplayEnabledForValidation(bool value)
        {
            gameplayEnabled = value;
            if (!value) LastResult = DefensiveCheckResult.None;
        }

        internal void ResetCooldownForValidation()
        {
            nextHumanTeamCheckTime = 0f;
            nextOpponentTeamCheckTime = 0f;
        }

        private void OnMatchChanged(int blue, int red, float remaining, MatchStateSnapshot state)
        {
            gameplayEnabled = state == MatchStateSnapshot.Playing;
            if (state == MatchStateSnapshot.Faceoff && lastMatchState != MatchStateSnapshot.Faceoff)
            {
                nextHumanTeamCheckTime = 0f;
                nextOpponentTeamCheckTime = 0f;
                LastResult = DefensiveCheckResult.None;
            }
            lastMatchState = state;
        }
    }
}
