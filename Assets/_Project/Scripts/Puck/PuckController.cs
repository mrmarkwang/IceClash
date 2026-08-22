/*
 * IceClash Phase 1 puck controller.
 * Keeps puck state independent from players and exposes physics tuning plus touch/possession metadata.
 * Phase 2 carries the puck by steering its Rigidbody toward a stick point; it is never parented to a skater.
 */

using IceClash.Core;
using IceClash.Player;
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
        [SerializeField] private float controlStrength = 22f;
        [SerializeField] private float controlVelocityDamping = 6f;

        private Rigidbody body;
        private PlayerController carrier;

        public TeamId? PossessionTeam { get; private set; }
        public string LastPlayerTouchId { get; private set; } = string.Empty;
        public string CarrierPlayerId => carrier != null ? carrier.PlayerId : string.Empty;
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

        public bool TryClaim(PlayerController player)
        {
            if (player == null || carrier != null || Vector3.Distance(player.ControlPoint, transform.position) > controlRadius)
            {
                return false;
            }

            carrier = player;
            RecordTouch(player.PlayerId, player.Team);
            return true;
        }

        public bool IsCarriedBy(PlayerController player) => carrier == player;

        public bool Release(PlayerController player, Vector3 direction, float speed)
        {
            if (!IsCarriedBy(player)) return false;

            carrier = null;
            LastPlayerTouchId = player.PlayerId;
            PossessionTeam = null;
            body.linearVelocity = Vector3.zero;
            body.AddForce(direction.normalized * speed, ForceMode.VelocityChange);
            return true;
        }

        public void ForceRelease(PlayerController player)
        {
            if (!IsCarriedBy(player)) return;
            carrier = null;
            ClearPossession();
        }

        private void FixedUpdate()
        {
            if (carrier == null) return;

            Vector3 target = carrier.ControlPoint;
            target.y = body.position.y;
            Vector3 error = target - body.position;
            body.AddForce(error * controlStrength - body.linearVelocity * controlVelocityDamping, ForceMode.Acceleration);
            body.linearVelocity = Vector3.ClampMagnitude(body.linearVelocity, carrier.ControlSpeedLimit);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.sqrMagnitude > 1f)
            {
                body.AddForce(-body.linearVelocity.normalized * bounciness, ForceMode.VelocityChange);
            }
        }
    }
}
