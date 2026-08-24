/*
 * IceClash Phase 1 one-button shooting.
 * Tracks held charge and releases forceful bounded facing/goal-assisted shots
 * with configurable inaccuracy, never reading movement input directly.
 */

using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Gameplay
{
    public sealed class ShootController : MonoBehaviour
    {
        [SerializeField] private float minimumPower = 11f;
        [SerializeField] private float maximumPower = 26f;
        [SerializeField] private float fullChargeSeconds = 0.95f;
        [SerializeField] private float cooldown = 0.4f;
        [SerializeField, Range(0f, 16f)] private float accuracyDegrees = 5f;
        [SerializeField, Range(0f, 1f)] private float goalTargetAssist = 0.72f;

        private PlayerController player;
        private PuckController puck;
        private float chargeStartedAt;
        private float nextShotTime;
        private bool charging;

        public float Charge01 => charging ? Mathf.Clamp01((Time.time - chargeStartedAt) / fullChargeSeconds) : 0f;
        public void Configure(PlayerController owner, PuckController controlledPuck) { player = owner; puck = controlledPuck; }

        public void Tick(bool held, bool released, float quality = 1f)
        {
            if (held && !charging && puck != null && puck.IsCarriedBy(player) && Time.time >= nextShotTime)
            { charging = true; chargeStartedAt = Time.time; }
            if (released && charging) ReleaseShot(quality);
            if (charging && (puck == null || !puck.IsCarriedBy(player))) charging = false;
        }

        public void ResetCharge() => charging = false;

        private void ReleaseShot(float quality)
        {
            float charge = Charge01;
            charging = false;
            if (puck == null || !puck.IsCarriedBy(player)) return;
            Vector3 direction = AssistedDirection();
            float spread = Random.Range(-accuracyDegrees, accuracyDegrees) * Mathf.Lerp(1.45f, 0.8f, Mathf.Clamp01(quality));
            direction = Quaternion.Euler(0f, spread, 0f) * direction;
            if (puck.Release(player, direction, EvaluatePower(charge))) nextShotTime = Time.time + cooldown;
        }

        internal float EvaluatePower(float normalizedCharge) => Mathf.Lerp(minimumPower, maximumPower, Mathf.Clamp01(normalizedCharge));

        private Vector3 AssistedDirection()
        {
            float goalZ = player.Team == IceClash.Core.TeamId.Blue ? 14.4f : -14.4f;
            Vector3 towardGoal = Vector3.ProjectOnPlane(new Vector3(0f, transform.position.y, goalZ) - transform.position, Vector3.up).normalized;
            Vector3 facing = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            return Vector3.Slerp(facing, towardGoal, goalTargetAssist).normalized;
        }
    }
}
