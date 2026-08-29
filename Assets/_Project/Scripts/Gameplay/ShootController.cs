/*
 * IceClash Phase 1 one-button shooting.
 * Tracks a quick held charge and releases fast, forceful, bounded deterministic
 * shots. SHT, charge, facing, rink/puck position, lateral speed, and fatigue
 * change execution without choosing when to shoot or reading movement input.
 */

using IceClash.Hockey;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Gameplay
{
    public sealed class ShootController : MonoBehaviour
    {
        [SerializeField] private float minimumPower = 28f;
        [SerializeField] private float maximumPower = 56f;
        [SerializeField] private float fullChargeSeconds = 0.65f;
        [SerializeField] private float cooldown = 0.3f;
        [SerializeField, Range(0f, 1f)] private float goalTargetAssist = 0.9f;

        private PlayerController player;
        private PuckController puck;
        private float chargeStartedAt;
        private float nextShotTime;
        private bool charging;

        public float Charge01 => charging ? Mathf.Clamp01((Time.time - chargeStartedAt) / fullChargeSeconds) : 0f;
        internal float FullChargeSeconds => fullChargeSeconds;
        internal float CooldownSeconds => cooldown;
        public void Configure(PlayerController owner, PuckController controlledPuck) { player = owner; puck = controlledPuck; }

        public void Tick(bool held, bool released)
        {
            if (held && !charging && puck != null && puck.IsCarriedBy(player) && Time.time >= nextShotTime)
            { charging = true; chargeStartedAt = Time.time; }
            if (released && charging) ReleaseShot();
            if (charging && (puck == null || !puck.IsCarriedBy(player))) charging = false;
        }

        public void ResetCharge() => charging = false;

        private void ReleaseShot()
        {
            float charge = Charge01;
            charging = false;
            if (puck == null || !puck.IsCarriedBy(player)) return;
            Vector3 direction = AssistedDirection();
            float spread = EvaluateDeviationDegrees(player.Attributes.Normalized(PlayerAttribute.Shooting),
                RuntimeSituationChallenge(charge), DeviationSign());
            direction = Quaternion.Euler(0f, spread, 0f) * direction;
            if (puck.Release(player, direction, EvaluatePower(charge))) nextShotTime = Time.time + cooldown;
        }

        internal float EvaluatePower(float normalizedCharge) => Mathf.Lerp(minimumPower, maximumPower,
            Mathf.Clamp01(normalizedCharge)) * EvaluatePowerMultiplier(
                player != null ? player.Attributes.Normalized(PlayerAttribute.Shooting) : 0f)
            * (player != null ? player.PerformanceFactor : 1f);

        internal static float EvaluatePowerMultiplier(float normalizedShooting) => Mathf.Lerp(0.85f, 1.2f, Mathf.Clamp01(normalizedShooting));
        internal static float EvaluateMaximumDeviation(float normalizedShooting) => Mathf.Lerp(6f, 1f, Mathf.Clamp01(normalizedShooting));
        internal static float EvaluateSituationChallenge(float missingCharge, float facingAngle, float rinkDistance,
            float puckError, float lateralSpeed, float fatigueLoss) => Mathf.Clamp01(
            0.25f * Mathf.Clamp01(missingCharge) + 0.2f * Mathf.Clamp01(facingAngle)
            + 0.2f * Mathf.Clamp01(rinkDistance) + 0.15f * Mathf.Clamp01(puckError)
            + 0.1f * Mathf.Clamp01(lateralSpeed) + 0.1f * Mathf.Clamp01(fatigueLoss));
        internal static float EvaluateDeviationDegrees(float normalizedShooting, float challenge, float sign) =>
            EvaluateMaximumDeviation(normalizedShooting) * Mathf.Clamp01(challenge)
            * Mathf.Sign(sign == 0f ? 1f : sign);
        internal float EvaluateRuntimeSituationChallengeForValidation(float charge) => RuntimeSituationChallenge(charge);
        internal float EvaluateRuntimeDeviationForValidation(float charge, float sign) =>
            EvaluateDeviationDegrees(player.Attributes.Normalized(PlayerAttribute.Shooting),
                RuntimeSituationChallenge(charge), sign);

        private Vector3 AssistedDirection()
        {
            float goalZ = player.Team == IceClash.Core.TeamId.Blue
                ? PrototypeRinkGeometry.GoalLineDistance
                : -PrototypeRinkGeometry.GoalLineDistance;
            Vector3 towardGoal = Vector3.ProjectOnPlane(new Vector3(0f, transform.position.y, goalZ) - transform.position, Vector3.up).normalized;
            Vector3 facing = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            return Vector3.Slerp(facing, towardGoal, goalTargetAssist).normalized;
        }

        private float RuntimeSituationChallenge(float charge)
        {
            Vector3 goal = new(0f, transform.position.y, player.Team == IceClash.Core.TeamId.Blue
                ? PrototypeRinkGeometry.GoalLineDistance : -PrototypeRinkGeometry.GoalLineDistance);
            Vector3 towardGoal = Vector3.ProjectOnPlane(goal - transform.position, Vector3.up);
            Vector3 facing = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            float facingAngle = towardGoal.sqrMagnitude > 0.001f ? Vector3.Angle(facing, towardGoal.normalized) / 90f : 0f;
            float rinkDistance = towardGoal.magnitude / PrototypeRinkGeometry.Length;
            float puckError = puck != null && puck.Body != null
                ? Vector3.Distance(puck.Body.position, player.ControlPoint) / Mathf.Max(player.Stick.ClaimRadius, 0.01f) : 1f;
            Vector3 velocity = player.Movement != null ? player.Movement.Velocity : Vector3.zero;
            float lateralSpeed = Mathf.Abs(Vector3.Dot(velocity, transform.right))
                / Mathf.Max(player.Movement.EffectiveMaximumSpeed, 0.01f);
            return EvaluateSituationChallenge(1f - charge, facingAngle, rinkDistance, puckError,
                lateralSpeed, 1f - player.Stamina / 100f);
        }

        private float DeviationSign()
        {
            float cross = Vector3.Cross(transform.forward, AssistedDirection()).y;
            if (Mathf.Abs(cross) > 0.0001f) return cross;
            int sum = 0;
            string id = player != null ? player.PlayerId : string.Empty;
            for (int i = 0; i < id.Length; i++) sum += id[i];
            return (sum & 1) == 0 ? 1f : -1f;
        }
    }
}
