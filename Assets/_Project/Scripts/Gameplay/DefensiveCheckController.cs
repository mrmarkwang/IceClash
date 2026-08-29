/*
 * IceClash team-aware contextual defensive check controller.
 * Resolves human and configured opponent-AI checks as close body or forward pull
 * challenges. STR/DEF/SPD/AGI, approach geometry, carrier puck protection,
 * stationary-carrier vulnerability, and bounded board pressure resolve contests;
 * success never grants possession.
 */

using IceClash.Core;
using IceClash.Hockey;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Gameplay
{
    public enum DefensiveCheckResult { None, BodyCheck, PullCheck }

    public sealed class DefensiveCheckController : MonoBehaviour
    {
        private const float BoardContactClearance = 0.8f;
        private const float BoardPressureClearance = 2.25f;
        private const float GuaranteedBoardTurnoverPressure = 0.7f;
        private const float StationaryCarrierSpeed = 0.2f;

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
                ContestScores contest = EvaluateLiveContest(false, checker, carrier, direction, values.PullForwardDot);
                if (!ContestSucceeds(contest, checker, carrier)) return LastResult;
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
                ContestScores contest = EvaluateLiveContest(true, checker, carrier, direction, values.PullForwardDot);
                if (!ContestSucceeds(contest, checker, carrier)) return LastResult;
                if (!puck.Dislodge(carrier, checker, -direction, values.PullPuckSpeed)) return LastResult;
                LastResult = DefensiveCheckResult.PullCheck;
            }

            SuccessfulCheckCount++;
            float cooldownUntil = Time.time + values.CooldownSeconds;
            if (checker.Team == humanTeam) nextHumanTeamCheckTime = cooldownUntil;
            else nextOpponentTeamCheckTime = cooldownUntil;
            return LastResult;
        }

        internal static float NormalizeApproachSpeed(Vector3 checkerVelocity, Vector3 carrierVelocity,
            Vector3 checkerToCarrierDirection) => Mathf.InverseLerp(0f, 8f, Mathf.Max(0f,
            Vector3.Dot(checkerVelocity - carrierVelocity, checkerToCarrierDirection.normalized)));

        internal static float NormalizeBodyAlignment(Vector3 checkerForward, Vector3 checkerToCarrierDirection) =>
            Mathf.InverseLerp(-1f, 1f, Vector3.Dot(checkerForward.normalized, checkerToCarrierDirection.normalized));

        internal static float NormalizePullAlignment(Vector3 checkerForward, Vector3 checkerToCarrierDirection,
            float gateDot) => Mathf.InverseLerp(Mathf.Clamp01(gateDot), 1f,
            Vector3.Dot(checkerForward.normalized, checkerToCarrierDirection.normalized));

        internal static float NormalizeContactPosition(Vector3 carrierForward, Vector3 carrierToCheckerDirection) =>
            Mathf.InverseLerp(-1f, 1f, Vector3.Dot(carrierForward.normalized, carrierToCheckerDirection.normalized));

        internal static float EvaluateBoardPressure(Vector3 carrierPosition)
        {
            float radius = PrototypeRinkGeometry.CornerRadius;
            Vector2 roundedBoxCore = new(
                PrototypeRinkGeometry.Width * 0.5f - radius,
                PrototypeRinkGeometry.Length * 0.5f - radius);
            Vector2 fromCore = new(
                Mathf.Abs(carrierPosition.x) - roundedBoxCore.x,
                Mathf.Abs(carrierPosition.z) - roundedBoxCore.y);
            Vector2 outsideCore = Vector2.Max(fromCore, Vector2.zero);
            float signedDistance = outsideCore.magnitude
                + Mathf.Min(Mathf.Max(fromCore.x, fromCore.y), 0f) - radius;
            float clearance = Mathf.Max(0f, -signedDistance);
            return Mathf.InverseLerp(BoardPressureClearance, BoardContactClearance, clearance);
        }

        internal static bool ContestSucceeds(ContestScores contest, float boardPressure,
            float carrierSpeed, bool carrierDeking, bool opponentChallengesHuman) => contest.Succeeds
            || (opponentChallengesHuman
                && (Mathf.Clamp01(boardPressure) >= GuaranteedBoardTurnoverPressure
                    || (!carrierDeking && Mathf.Max(0f, carrierSpeed) <= StationaryCarrierSpeed)));

        internal static ContestScores EvaluateContest(bool pull, float checkerStrength, float checkerDefense,
            float checkerSpeed, float checkerAgility, float carrierControl, float carrierStrength,
            float carrierAgility, float carrierSpeed, float approachSpeed, float alignment,
            float carrierFatigue, float contactPosition, float dekeBonus)
        {
            float attack = pull
                ? 0.4f * Clamp(checkerDefense) + 0.2f * Clamp(checkerAgility)
                    + 0.1f * Clamp(checkerStrength) + 0.15f * Clamp(approachSpeed) + 0.15f * Clamp(alignment)
                : 0.35f * Clamp(checkerStrength) + 0.15f * Clamp(checkerDefense)
                    + 0.2f * Clamp(checkerSpeed) + 0.1f * Clamp(checkerAgility)
                    + 0.15f * Clamp(approachSpeed) + 0.05f * Clamp(alignment);
            float protection = pull
                ? 0.45f * Clamp(carrierControl) + 0.25f * Clamp(carrierAgility)
                    + 0.15f * Clamp(carrierStrength) + 0.1f * Clamp(carrierFatigue)
                    + 0.05f * Clamp(contactPosition)
                : 0.3f * Clamp(carrierControl) + 0.3f * Clamp(carrierStrength)
                    + 0.15f * Clamp(carrierAgility) + 0.1f * Clamp(carrierSpeed)
                    + 0.1f * Clamp(carrierFatigue) + 0.05f * Clamp(contactPosition);
            protection += Mathf.Clamp(dekeBonus, 0f, 0.15f);
            return new ContestScores(attack, protection);
        }

        private static float Clamp(float value) => Mathf.Clamp01(value);

        private static ContestScores EvaluateLiveContest(bool pull, PlayerController checker,
            PlayerController carrier, Vector3 checkerToCarrierDirection, float pullGateDot)
        {
            Vector3 checkerVelocity = checker.Movement != null ? checker.Movement.Velocity : Vector3.zero;
            Vector3 carrierVelocity = carrier.Movement != null ? carrier.Movement.Velocity : Vector3.zero;
            Vector3 checkerForward = Vector3.ProjectOnPlane(checker.transform.forward, Vector3.up).normalized;
            Vector3 carrierForward = Vector3.ProjectOnPlane(carrier.transform.forward, Vector3.up).normalized;
            float approach = NormalizeApproachSpeed(checkerVelocity, carrierVelocity, checkerToCarrierDirection);
            float alignment = pull
                ? NormalizePullAlignment(checkerForward, checkerToCarrierDirection, pullGateDot)
                : NormalizeBodyAlignment(checkerForward, checkerToCarrierDirection);
            float contact = NormalizeContactPosition(carrierForward, -checkerToCarrierDirection);
            PlayerAttributeBuild checkerBuild = checker.Attributes;
            PlayerAttributeBuild carrierBuild = carrier.Attributes;
            return EvaluateContest(pull,
                checkerBuild.Normalized(PlayerAttribute.Strength), checkerBuild.Normalized(PlayerAttribute.Defense),
                checkerBuild.Normalized(PlayerAttribute.Speed), checkerBuild.Normalized(PlayerAttribute.Agility),
                carrierBuild.Normalized(PlayerAttribute.Control), carrierBuild.Normalized(PlayerAttribute.Strength),
                carrierBuild.Normalized(PlayerAttribute.Agility), carrierBuild.Normalized(PlayerAttribute.Speed),
                approach, alignment, carrier.Stamina / 100f, contact,
                carrier.Deke != null ? carrier.Deke.ProtectionBonus : 0f);
        }

        internal readonly struct ContestScores
        {
            public ContestScores(float attack, float protection) { Attack = attack; Protection = protection; }
            public float Attack { get; }
            public float Protection { get; }
            public bool Succeeds => Attack >= Protection;
        }

        private bool ContestSucceeds(ContestScores contest, PlayerController checker, PlayerController carrier)
        {
            bool opponentChallengesHuman = checker.Team != humanTeam && carrier.Team == humanTeam;
            float boardPressure = opponentChallengesHuman
                ? EvaluateBoardPressure(carrier.transform.position)
                : 0f;
            float carrierSpeed = carrier.Movement != null ? carrier.Movement.Velocity.magnitude : 0f;
            bool carrierDeking = carrier.Deke != null && carrier.Deke.IsActive;
            return ContestSucceeds(contest, boardPressure, carrierSpeed, carrierDeking, opponentChallengesHuman);
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
