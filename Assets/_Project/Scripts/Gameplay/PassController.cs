/*
 * IceClash Phase 1 assisted passing.
 * Scores teammates using aim, distance, openness, lane safety, and offensive
 * progress, then releases a lead pass with bounded imperfection.
 */

using IceClash.Core;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Gameplay
{
    public sealed class PassController : MonoBehaviour
    {
        [SerializeField] private float maximumRange = 17f;
        [SerializeField] private float passSpeed = 12.5f;
        [SerializeField] private float cooldown = 0.28f;
        [SerializeField] private float leadSeconds = 0.22f;
        [SerializeField, Range(0f, 12f)] private float errorDegrees = 3.5f;

        private PlayerController player;
        private PuckController puck;
        private float nextPassTime;

        public void Configure(PlayerController owner, PuckController controlledPuck) { player = owner; puck = controlledPuck; }

        public bool TryPass(Vector2 aimInput, float quality = 1f)
        {
            if (player == null || puck == null || Time.time < nextPassTime || !puck.IsCarriedBy(player)) return false;
            PlayerController target = FindBestTarget(aimInput);
            if (target == null) return false;
            Vector3 lead = target.Movement != null ? target.Movement.Velocity * leadSeconds : Vector3.zero;
            Vector3 direction = Vector3.ProjectOnPlane(target.Stick.ControlPoint + lead - puck.transform.position, Vector3.up).normalized;
            float spread = Random.Range(-errorDegrees, errorDegrees) * Mathf.Lerp(1.5f, 0.75f, Mathf.Clamp01(quality));
            direction = Quaternion.Euler(0f, spread, 0f) * direction;
            if (!puck.Release(player, direction, passSpeed * Mathf.Lerp(0.88f, 1f, quality))) return false;
            nextPassTime = Time.time + cooldown;
            return true;
        }

        private PlayerController FindBestTarget(Vector2 aimInput)
        {
            Vector3 aim = AimWorld(aimInput);
            PlayerController best = null;
            float bestScore = float.NegativeInfinity;
            PlayerController[] all = FindObjectsByType<PlayerController>();
            foreach (PlayerController candidate in all)
            {
                if (candidate == player || candidate.Team != player.Team) continue;
                Vector3 delta = Vector3.ProjectOnPlane(candidate.transform.position - transform.position, Vector3.up);
                float distance = delta.magnitude;
                if (distance > maximumRange || distance < 1f) continue;
                float alignment = Vector3.Dot(aim, delta.normalized);
                float openness = NearestOpponentDistance(candidate, all) / 8f;
                float attackSign = player.Team == TeamId.Blue ? 1f : -1f;
                float progress = Mathf.Clamp((candidate.transform.position.z - transform.position.z) * attackSign / 10f, -1f, 1f);
                bool blocked = Physics.SphereCast(transform.position + Vector3.up * 0.4f, 0.3f, delta.normalized, out RaycastHit hit, distance)
                    && hit.transform != candidate.transform && hit.transform != transform;
                float score = alignment * 5f - distance * 0.08f + openness * 1.7f + progress * 1.2f - (blocked ? 2.5f : 0f);
                if (score > bestScore) { best = candidate; bestScore = score; }
            }
            return best;
        }

        private float NearestOpponentDistance(PlayerController candidate, PlayerController[] all)
        {
            float nearest = 8f;
            foreach (PlayerController other in all)
                if (other.Team != candidate.Team) nearest = Mathf.Min(nearest, Vector3.Distance(candidate.transform.position, other.transform.position));
            return nearest;
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
