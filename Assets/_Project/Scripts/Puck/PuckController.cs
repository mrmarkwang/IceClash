/*
 * IceClash Phase 1 puck controller.
 * Keeps puck state independent from players and exposes physics tuning plus touch/possession metadata.
 * Possession influence is intentionally deferred to Phase 2; this puck is never parented to a skater.
 */

using IceClash.Core;
using UnityEngine;

namespace IceClash.Puck
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class PuckController : MonoBehaviour, IPuckController
    {
        [Header("Physics")]
        [SerializeField] private float linearDamping = 0.65f;
        [SerializeField] private float angularDamping = 1.2f;
        [SerializeField] private float bounciness = 0.35f;
        [SerializeField] private float controlRadius = 1.4f;

        private Rigidbody body;

        public TeamId? PossessionTeam { get; private set; }
        public string LastPlayerTouchId { get; private set; } = string.Empty;
        public Rigidbody Body => body;
        public float ControlRadius => controlRadius;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.linearDamping = linearDamping;
            body.angularDamping = angularDamping;
        }

        public void RecordTouch(string playerId, TeamId team)
        {
            LastPlayerTouchId = playerId;
            PossessionTeam = team;
        }

        public void ClearPossession() => PossessionTeam = null;

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.sqrMagnitude > 1f)
            {
                body.AddForce(-body.linearVelocity.normalized * bounciness, ForceMode.VelocityChange);
            }
        }
    }
}
