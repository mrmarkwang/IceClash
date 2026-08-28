/*
 * IceClash Phase 1 independent physics puck.
 * Owns velocity-matched possession, high-speed collision-safe physical releases,
 * save impulses, reclaim locks, carrier events, and deterministic match resets.
 * Recent change: pickup checks use the authoritative Rigidbody position so
 * interpolation lag cannot cause receivers to miss fast passes.
 */

using System;
using IceClash.Core;
using IceClash.Player;
using UnityEngine;

namespace IceClash.Puck
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class PuckController : MonoBehaviour, IPuckController
    {
        [SerializeField] private float linearDamping = 0.55f;
        [SerializeField] private float angularDamping = 1.2f;
        [SerializeField] private float controlStrength = 32f;
        [SerializeField] private float controlVelocityDamping = 10f;
        [SerializeField] private float maximumCarrySpeed = 14f;
        [SerializeField, Min(0f)] private float releasingPlayerReclaimDelay = 0.22f;

        private Rigidbody body;
        private PlayerController carrier;
        private StickPuckInteraction carrierStick;
        private string reclaimLockedPlayerId = string.Empty;
        private float reclaimLockedUntil;

        public event Action<PlayerController> CarrierChanged;
        public TeamId? PossessionTeam { get; private set; }
        public string LastPlayerTouchId { get; private set; } = string.Empty;
        public string CarrierPlayerId => carrier != null ? carrier.PlayerId : string.Empty;
        public PlayerController Carrier => carrier;
        public Rigidbody Body => body;
        public int ImpulseReleaseSequence { get; private set; }
        public string LastImpulseReleasePlayerId { get; private set; } = string.Empty;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.linearDamping = linearDamping;
            body.angularDamping = angularDamping;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public bool TryClaim(PlayerController player, StickPuckInteraction stick)
        {
            if (player == null || stick == null || carrier != null
                || (player.PlayerId == reclaimLockedPlayerId && Time.time < reclaimLockedUntil)
                || Vector3.Distance(stick.ControlPoint, body.position) > stick.ClaimRadius
                || body.linearVelocity.magnitude > stick.MaximumClaimSpeed) return false;

            carrier = player;
            carrierStick = stick;
            LastPlayerTouchId = player.PlayerId;
            PossessionTeam = player.Team;
            CarrierChanged?.Invoke(carrier);
            return true;
        }

        public bool IsCarriedBy(PlayerController player) => carrier == player;

        public bool Release(PlayerController player, Vector3 direction, float speed)
        {
            if (!IsCarriedBy(player) || direction.sqrMagnitude < 0.01f) return false;
            LastPlayerTouchId = player.PlayerId;
            LastImpulseReleasePlayerId = player.PlayerId;
            ImpulseReleaseSequence++;
            ClearCarrier(true);
            body.linearVelocity = Vector3.zero;
            body.AddForce(Vector3.ProjectOnPlane(direction, Vector3.up).normalized * speed, ForceMode.VelocityChange);
            return true;
        }

        public void ForceRelease(PlayerController player)
        {
            if (IsCarriedBy(player)) ClearCarrier(true);
        }

        public void ApplySave(Vector3 direction, float speed, TeamId goalieTeam)
        {
            if (carrier != null) ClearCarrier(false);
            PossessionTeam = goalieTeam;
            body.linearVelocity = Vector3.zero;
            body.AddForce(Vector3.ProjectOnPlane(direction, Vector3.up).normalized * speed, ForceMode.VelocityChange);
        }

        public void ResetPuck(Vector3 position)
        {
            carrier = null;
            carrierStick = null;
            PossessionTeam = null;
            LastPlayerTouchId = string.Empty;
            reclaimLockedPlayerId = string.Empty;
            body.position = position;
            body.rotation = Quaternion.identity;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            CarrierChanged?.Invoke(null);
        }

        private void ClearCarrier(bool lockReclaim)
        {
            if (lockReclaim && carrier != null)
            {
                reclaimLockedPlayerId = carrier.PlayerId;
                reclaimLockedUntil = Time.time + releasingPlayerReclaimDelay;
            }
            carrier = null;
            carrierStick = null;
            PossessionTeam = null;
            CarrierChanged?.Invoke(null);
        }

        private void FixedUpdate()
        {
            if (carrier == null || carrierStick == null) return;
            Vector3 target = carrierStick.ControlPoint;
            target.y = body.position.y;
            Vector3 carrierVelocity = carrier.Movement != null ? carrier.Movement.Velocity : Vector3.zero;
            body.AddForce(CalculateCarryAcceleration(target, carrierVelocity), ForceMode.Acceleration);
            body.linearVelocity = Vector3.ClampMagnitude(body.linearVelocity, maximumCarrySpeed);
        }

        internal Vector3 CalculateCarryAcceleration(Vector3 target, Vector3 targetVelocity)
        {
            Vector3 positionError = target - body.position;
            Vector3 velocityError = targetVelocity - body.linearVelocity;
            return positionError * controlStrength + velocityError * controlVelocityDamping;
        }
    }
}
