/*
 * IceClash Phase 1 one-button shooting.
 * Tracks held charge and releases bounded aim-based shots with configurable
 * strength and inaccuracy, leaving shot-type choice implicit.
 */

using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Gameplay
{
    public sealed class ShootController : MonoBehaviour
    {
        [SerializeField] private float minimumPower = 9f;
        [SerializeField] private float maximumPower = 19f;
        [SerializeField] private float fullChargeSeconds = 1.15f;
        [SerializeField] private float cooldown = 0.4f;
        [SerializeField, Range(0f, 16f)] private float accuracyDegrees = 5f;

        private PlayerController player;
        private PuckController puck;
        private float chargeStartedAt;
        private float nextShotTime;
        private bool charging;

        public float Charge01 => charging ? Mathf.Clamp01((Time.time - chargeStartedAt) / fullChargeSeconds) : 0f;
        public void Configure(PlayerController owner, PuckController controlledPuck) { player = owner; puck = controlledPuck; }

        public void Tick(bool held, bool released, Vector2 aimInput, float quality = 1f)
        {
            if (held && !charging && puck != null && puck.IsCarriedBy(player) && Time.time >= nextShotTime)
            { charging = true; chargeStartedAt = Time.time; }
            if (released && charging) ReleaseShot(aimInput, quality);
            if (charging && (puck == null || !puck.IsCarriedBy(player))) charging = false;
        }

        public void ResetCharge() => charging = false;

        private void ReleaseShot(Vector2 aimInput, float quality)
        {
            float charge = Charge01;
            charging = false;
            if (puck == null || !puck.IsCarriedBy(player)) return;
            Vector3 direction = AimWorld(aimInput);
            float spread = Random.Range(-accuracyDegrees, accuracyDegrees) * Mathf.Lerp(1.45f, 0.8f, Mathf.Clamp01(quality));
            direction = Quaternion.Euler(0f, spread, 0f) * direction;
            if (puck.Release(player, direction, Mathf.Lerp(minimumPower, maximumPower, charge))) nextShotTime = Time.time + cooldown;
        }

        private Vector3 AimWorld(Vector2 input)
        {
            if (input.sqrMagnitude < 0.04f) return transform.forward;
            Camera view = Camera.main;
            Vector3 forward = view != null ? Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = view != null ? Vector3.ProjectOnPlane(view.transform.right, Vector3.up).normalized : Vector3.right;
            return (forward * input.y + right * input.x).normalized;
        }
    }
}
