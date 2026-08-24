/*
 * IceClash recommended PASS target selection policy.
 * Continuously scores teammates by facing, openness, range, interception risk,
 * and offensive progress without consuming movement or directional gesture input.
 */

using IceClash.Core;
using IceClash.Player;
using UnityEngine;

namespace IceClash.Gameplay
{
    public readonly struct PassTargetSelection
    {
        public PassTargetSelection(PlayerController teammate, float score, Vector3 direction)
        {
            SelectedTeammate = teammate;
            TargetScore = score;
            TargetDirection = direction;
        }

        public PlayerController SelectedTeammate { get; }
        public float TargetScore { get; }
        public Vector3 TargetDirection { get; }
        public bool IsValid => SelectedTeammate != null;
    }

    public sealed class PassTargetSelector : MonoBehaviour
    {
        [SerializeField] private float maxPassDistance = 17f;
        [SerializeField] private float facingWeight = 5f;
        [SerializeField] private float opennessWeight = 1.7f;
        [SerializeField] private float distanceWeight = 0.08f;
        [SerializeField] private float interceptionRiskWeight = 2.8f;
        [SerializeField] private float offensivePositionWeight = 1.2f;

        public PassTargetSelection Select(PlayerController owner)
        {
            if (owner == null) return default;
            PlayerController[] all = FindObjectsByType<PlayerController>();
            PlayerController best = null;
            float bestScore = float.NegativeInfinity;
            Vector3 bestDirection = Vector3.zero;
            Vector3 facing = Vector3.ProjectOnPlane(owner.transform.forward, Vector3.up).normalized;

            foreach (PlayerController candidate in all)
            {
                if (candidate == owner || candidate.Team != owner.Team) continue;
                Vector3 delta = Vector3.ProjectOnPlane(candidate.Stick.ControlPoint - owner.transform.position, Vector3.up);
                float distance = delta.magnitude;
                if (distance < 1f || distance > maxPassDistance) continue;
                Vector3 direction = delta / distance;
                float facingAlignment = Vector3.Dot(facing, direction);
                float openness = Mathf.Clamp01(NearestOpponentDistance(candidate, all) / 8f);
                float interceptionRisk = CalculateInterceptionRisk(owner, candidate, all, direction, distance);
                float attackSign = owner.Team == TeamId.Blue ? 1f : -1f;
                float progress = Mathf.Clamp((candidate.transform.position.z - owner.transform.position.z) * attackSign / 10f, -1f, 1f);
                float score = facingAlignment * facingWeight
                    + openness * opennessWeight
                    - distance * distanceWeight
                    - interceptionRisk * interceptionRiskWeight
                    + progress * offensivePositionWeight;
                if (score <= bestScore) continue;
                best = candidate;
                bestScore = score;
                bestDirection = direction;
            }

            return best == null ? default : new PassTargetSelection(best, bestScore, bestDirection);
        }

        private static float NearestOpponentDistance(PlayerController candidate, PlayerController[] all)
        {
            float nearest = 8f;
            foreach (PlayerController other in all)
                if (other.Team != candidate.Team)
                    nearest = Mathf.Min(nearest, Vector3.Distance(candidate.transform.position, other.transform.position));
            return nearest;
        }

        private static float CalculateInterceptionRisk(PlayerController owner, PlayerController candidate, PlayerController[] all, Vector3 direction, float distance)
        {
            float risk = 0f;
            Vector3 start = owner.transform.position;
            foreach (PlayerController opponent in all)
            {
                if (opponent.Team == owner.Team) continue;
                Vector3 relative = Vector3.ProjectOnPlane(opponent.transform.position - start, Vector3.up);
                float along = Mathf.Clamp(Vector3.Dot(relative, direction), 0f, distance);
                float laneDistance = (relative - direction * along).magnitude;
                risk = Mathf.Max(risk, 1f - Mathf.Clamp01(laneDistance / 2.2f));
            }

            RaycastHit[] hits = Physics.SphereCastAll(start + Vector3.up * 0.4f, 0.3f, direction, distance);
            foreach (RaycastHit hit in hits)
            {
                Transform hitTransform = hit.transform;
                if (hitTransform == null || hitTransform.IsChildOf(owner.transform) || hitTransform.IsChildOf(candidate.transform)) continue;
                risk = Mathf.Max(risk, 0.85f);
            }
            return risk;
        }
    }
}
