/*
 * IceClash Phase 1 one-button shooting.
 * Tracks a quick held charge and releases fast, forceful, visibly airborne,
 * bounded deterministic shots. Charge changes speed only; alternating low/high
 * variants use damping-aware ballistic lift. Shooting skill improves open-lane
 * aim, power, and error while facing, rink/puck position, lateral speed, and fatigue affect execution.
 */

using System.Collections.Generic;
using IceClash.AI;
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
        [SerializeField, Min(0f)] private float minimumShotLiftSpeed = 2.2f;
        [SerializeField, Min(0f)] private float maximumShotLiftSpeed = 7.5f;
        [SerializeField, Min(0f)] private float minimumShotTargetHeight = 0.75f;
        [SerializeField, Min(0f)] private float maximumShotTargetHeight = 1.05f;
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
            ReleaseShot(charge);
        }

        private bool ReleaseShot(float charge)
        {
            if (puck == null || !puck.IsCarriedBy(player)) return false;
            float shooting = player.Attributes.Normalized(PlayerAttribute.Shooting);
            float power = EvaluatePower(charge);
            bool highShot = EvaluateHighShotVariant(player.PlayerId, puck.ImpulseReleaseSequence);
            float targetHeight = EvaluateTargetHeight(highShot, minimumShotTargetHeight, maximumShotTargetHeight);
            Vector3 assistedDirection = AssistedDirection(shooting);
            float liftSpeed = EvaluateBallisticLiftSpeed(targetHeight, power,
                DistanceToGoalAlong(assistedDirection), puck.Body.position.y,
                minimumShotTargetHeight, maximumShotTargetHeight,
                minimumShotLiftSpeed, maximumShotLiftSpeed, Mathf.Abs(Physics.gravity.y),
                puck.Body.linearDamping, Time.fixedDeltaTime);
            Vector3 direction = EvaluateShotDirection(assistedDirection, power, liftSpeed);
            float spread = EvaluateDeviationDegrees(shooting, RuntimeSituationChallenge(charge), DeviationSign());
            direction = Quaternion.Euler(0f, spread, 0f) * direction;
            bool released = puck.Release(player, direction, power);
            if (released) nextShotTime = Time.time + cooldown;
            return released;
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
        internal static bool EvaluateHighShotVariant(string playerId, int releaseSequence)
        {
            int hash = Mathf.Max(0, releaseSequence);
            string id = playerId ?? string.Empty;
            for (int i = 0; i < id.Length; i++) hash += id[i];
            return (hash & 1) != 0;
        }
        internal static float EvaluateTargetHeight(bool highShot, float minimumTargetHeight, float maximumTargetHeight) =>
            highShot ? Mathf.Max(minimumTargetHeight, maximumTargetHeight) : Mathf.Max(0f, minimumTargetHeight);
        internal static float EvaluateLaneAssist(float normalizedShooting, float maximumAssist) =>
            Mathf.Clamp01(maximumAssist) * Mathf.Lerp(0.65f, 1f, Mathf.Clamp01(normalizedShooting));
        internal static Vector3 EvaluateShotDirection(Vector3 planarDirection, float power, float liftSpeed)
        {
            Vector3 horizontal = Vector3.ProjectOnPlane(planarDirection, Vector3.up).normalized;
            if (horizontal.sqrMagnitude < 0.01f) return Vector3.zero;
            float normalizedLift = Mathf.Clamp(Mathf.Max(0f, liftSpeed) / Mathf.Max(power, 0.01f), 0f, 0.95f);
            return horizontal * Mathf.Sqrt(1f - normalizedLift * normalizedLift)
                + Vector3.up * normalizedLift;
        }
        internal static float EvaluateBallisticLiftSpeed(float targetHeight, float power,
            float horizontalDistance, float startHeight, float minimumTargetHeight, float maximumTargetHeight,
            float minimumLiftSpeed, float maximumLiftSpeed, float gravity, float linearDamping, float fixedDeltaTime)
        {
            float boundedPower = Mathf.Max(power, 0.01f);
            float minimumLift = Mathf.Max(0f, minimumLiftSpeed);
            float maximumLift = Mathf.Max(minimumLift, maximumLiftSpeed);
            float boundedTargetHeight = Mathf.Clamp(targetHeight, Mathf.Max(0f, minimumTargetHeight),
                Mathf.Max(minimumTargetHeight, maximumTargetHeight));
            float lowerLift = minimumLift;
            float upperLift = maximumLift;
            for (int iteration = 0; iteration < 16; iteration++)
            {
                float candidateLift = (lowerLift + upperLift) * 0.5f;
                float candidateHeight = EvaluateDampedBallisticHeight(startHeight, boundedPower,
                    candidateLift, horizontalDistance, gravity, linearDamping, fixedDeltaTime);
                if (candidateHeight < boundedTargetHeight) lowerLift = candidateLift;
                else upperLift = candidateLift;
            }
            return (lowerLift + upperLift) * 0.5f;
        }
        internal static float EvaluateDampedBallisticHeight(float startHeight, float power, float liftSpeed,
            float horizontalDistance, float gravity, float linearDamping, float fixedDeltaTime)
        {
            float step = Mathf.Max(0.001f, fixedDeltaTime);
            float dampingFactor = 1f / (1f + Mathf.Max(0f, linearDamping) * step);
            float horizontalSpeed = Mathf.Sqrt(Mathf.Max(power * power - liftSpeed * liftSpeed, 0.01f));
            float verticalSpeed = Mathf.Max(0f, liftSpeed);
            float distance = 0f;
            float height = startHeight;
            for (int iteration = 0; iteration < 600 && distance < horizontalDistance; iteration++)
            {
                float previousDistance = distance;
                float previousHeight = height;
                verticalSpeed = (verticalSpeed - Mathf.Max(0f, gravity) * step) * dampingFactor;
                horizontalSpeed *= dampingFactor;
                distance += horizontalSpeed * step;
                height += verticalSpeed * step;
                if (distance >= horizontalDistance)
                {
                    float progress = Mathf.InverseLerp(previousDistance, distance, horizontalDistance);
                    return Mathf.Lerp(previousHeight, height, progress);
                }
                if (horizontalSpeed <= 0.01f) break;
            }
            return height;
        }
        internal float EvaluateRuntimeSituationChallengeForValidation(float charge) => RuntimeSituationChallenge(charge);
        internal float EvaluateRuntimeDeviationForValidation(float charge, float sign) =>
            EvaluateDeviationDegrees(player.Attributes.Normalized(PlayerAttribute.Shooting),
                RuntimeSituationChallenge(charge), sign);
        internal bool ReleaseForValidation(float charge) => ReleaseShot(Mathf.Clamp01(charge));

        private Vector3 AssistedDirection(float normalizedShooting)
        {
            float goalZ = player.Team == IceClash.Core.TeamId.Blue
                ? PrototypeRinkGeometry.GoalLineDistance
                : -PrototypeRinkGeometry.GoalLineDistance;
            List<Vector3> blockers = new();
            PlayerController[] skaters = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            for (int i = 0; i < skaters.Length; i++)
                if (skaters[i] != player && skaters[i].Team != player.Team) blockers.Add(skaters[i].transform.position);
            HockeyGoalieAI[] goalies = Object.FindObjectsByType<HockeyGoalieAI>(FindObjectsSortMode.None);
            for (int i = 0; i < goalies.Length; i++)
                if (goalies[i].Team != player.Team) blockers.Add(goalies[i].transform.position);

            float laneX = EvaluateOpenLaneTargetX(transform.position, goalZ, blockers.ToArray());
            Vector3 towardGoal = Vector3.ProjectOnPlane(
                new Vector3(laneX, transform.position.y, goalZ) - transform.position, Vector3.up).normalized;
            Vector3 facing = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            return Vector3.Slerp(facing, towardGoal, EvaluateLaneAssist(normalizedShooting, goalTargetAssist)).normalized;
        }

        internal static float EvaluateOpenLaneTargetX(Vector3 shooterPosition, float goalZ, Vector3[] blockers)
        {
            float[] candidates = { -1.05f, -0.52f, 0f, 0.52f, 1.05f };
            float bestX = 0f;
            float bestClearance = float.NegativeInfinity;
            for (int candidateIndex = 0; candidateIndex < candidates.Length; candidateIndex++)
            {
                Vector2 start = new(shooterPosition.x, shooterPosition.z);
                Vector2 end = new(candidates[candidateIndex], goalZ);
                Vector2 lane = end - start;
                float minimumClearance = float.PositiveInfinity;
                bool hasRelevantBlocker = false;
                for (int blockerIndex = 0; blockerIndex < blockers.Length; blockerIndex++)
                {
                    Vector2 blocker = new(blockers[blockerIndex].x, blockers[blockerIndex].z);
                    float progress = Vector2.Dot(blocker - start, lane) / Mathf.Max(lane.sqrMagnitude, 0.001f);
                    if (progress <= 0f || progress >= 1.02f) continue;
                    hasRelevantBlocker = true;
                    float clearance = Vector2.Distance(blocker, start + lane * Mathf.Clamp01(progress));
                    minimumClearance = Mathf.Min(minimumClearance, clearance);
                }
                if (!hasRelevantBlocker) minimumClearance = 100f - Mathf.Abs(candidates[candidateIndex]) * 0.01f;
                if (minimumClearance > bestClearance)
                {
                    bestClearance = minimumClearance;
                    bestX = candidates[candidateIndex];
                }
            }
            return bestX;
        }

        private float DistanceToGoalAlong(Vector3 planarDirection)
        {
            float goalZ = player.Team == IceClash.Core.TeamId.Blue
                ? PrototypeRinkGeometry.GoalLineDistance
                : -PrototypeRinkGeometry.GoalLineDistance;
            return Mathf.Abs(goalZ - puck.Body.position.z)
                / Mathf.Max(Mathf.Abs(Vector3.ProjectOnPlane(planarDirection, Vector3.up).normalized.z), 0.01f);
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
            return EvaluateSituationChallenge(0f, facingAngle, rinkDistance, puckError,
                lateralSpeed, 1f - player.Stamina / 100f);
        }

        private float DeviationSign()
        {
            float shooting = player != null ? player.Attributes.Normalized(PlayerAttribute.Shooting) : 0f;
            float cross = Vector3.Cross(transform.forward, AssistedDirection(shooting)).y;
            if (Mathf.Abs(cross) > 0.0001f) return cross;
            int sum = 0;
            string id = player != null ? player.PlayerId : string.Empty;
            for (int i = 0; i < id.Length; i++) sum += id[i];
            return (sum & 1) == 0 ? 1f : -1f;
        }
    }
}
