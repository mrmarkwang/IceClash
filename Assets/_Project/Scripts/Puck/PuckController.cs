/*
 * IceClash Phase 1 independent physics puck.
 * Owns velocity-matched possession, high-speed collision-safe physical releases,
 * intended-pass reception, save impulses, reclaim locks, carrier events, and
 * deterministic resets and validated defensive dislodges. Rounded-rink containment
 * keeps loose and carried pucks clear of board colliders, including when a carrier's
 * stick control point reaches beyond the ice. Reset notifications let swept goal-line
 * checks discard teleport history. CTR scales carry control; intended passes retain
 * PAS quality for deterministic CTR/PAS reception.
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
        private PlayerController intendedPassReceiver;
        private PassReceivingZone intendedReceptionZone;
        private float intendedPassExpiresAt;
        private float intendedPassQuality;
        private string reclaimLockedPlayerId = string.Empty;
        private float reclaimLockedUntil;
        private float rinkWidth;
        private float rinkLength;
        private float rinkCornerRadius;
        private float rinkClearance;
        private bool hasRinkBoundary;

        public event Action<PlayerController> CarrierChanged;
        internal event Action<Vector3> PositionReset;
        public TeamId? PossessionTeam { get; private set; }
        public string LastPlayerTouchId { get; private set; } = string.Empty;
        public string CarrierPlayerId => carrier != null ? carrier.PlayerId : string.Empty;
        public PlayerController Carrier => carrier;
        public Rigidbody Body => body;
        public int ImpulseReleaseSequence { get; private set; }
        public string LastImpulseReleasePlayerId { get; private set; } = string.Empty;
        internal PlayerController IntendedPassReceiver => intendedPassReceiver;
        internal float IntendedPassQuality => intendedPassQuality;
        internal float ConfiguredRinkClearance => rinkClearance;

        internal void ConfigureRinkBoundary(float width, float length, float cornerRadius, float clearance)
        {
            rinkWidth = Mathf.Max(0f, width);
            rinkLength = Mathf.Max(0f, length);
            rinkCornerRadius = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(rinkWidth, rinkLength) * 0.5f);
            rinkClearance = Mathf.Clamp(clearance, 0f, rinkCornerRadius);
            hasRinkBoundary = rinkWidth > 0f && rinkLength > 0f;
        }

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
                || (intendedPassReceiver != null && player.Team == intendedPassReceiver.Team)
                || (player.PlayerId == reclaimLockedPlayerId && Time.time < reclaimLockedUntil)
                || Vector3.Distance(stick.ControlPoint, body.position) > stick.ClaimRadius
                || body.linearVelocity.magnitude > stick.MaximumClaimSpeed) return false;

            EstablishCarrier(player, stick);
            return true;
        }

        public bool IsCarriedBy(PlayerController player) => carrier == player;

        public bool Release(PlayerController player, Vector3 direction, float speed)
        {
            if (!IsCarriedBy(player) || direction.sqrMagnitude < 0.01f) return false;
            PrepareRelease(player);
            ClearIntendedPass();
            body.linearVelocity = Vector3.ProjectOnPlane(direction, Vector3.up).normalized * speed;
            return true;
        }

        internal bool ReleasePass(PlayerController player, PlayerController receiver, Vector3 direction, float speed,
            float receptionEligibilitySeconds, float passerPassingQuality)
        {
            if (!IsCarriedBy(player) || receiver == null || receiver.PassReception == null
                || direction.sqrMagnitude < 0.01f || speed <= 0f) return false;

            PrepareRelease(player);
            intendedPassReceiver = receiver;
            intendedReceptionZone = receiver.PassReception;
            intendedPassExpiresAt = Time.time + Mathf.Max(0.1f, receptionEligibilitySeconds);
            intendedPassQuality = Mathf.Clamp01(passerPassingQuality);
            body.linearVelocity = Vector3.ProjectOnPlane(direction, Vector3.up).normalized * speed;
            return true;
        }

        public void ForceRelease(PlayerController player)
        {
            if (!IsCarriedBy(player)) return;
            ClearIntendedPass();
            ClearCarrier(true);
        }

        public bool Dislodge(PlayerController expectedCarrier, PlayerController checker,
            Vector3 direction, float speed)
        {
            if (expectedCarrier == null || checker == null || carrier != expectedCarrier
                || checker.Team == expectedCarrier.Team || direction.sqrMagnitude < 0.01f) return false;

            float boundedSpeed = Mathf.Clamp(speed, 1f, 15f);
            ClearIntendedPass();
            ClearCarrier(true);
            LastPlayerTouchId = checker.PlayerId;
            LastImpulseReleasePlayerId = checker.PlayerId;
            ImpulseReleaseSequence++;
            body.linearVelocity = Vector3.ProjectOnPlane(direction, Vector3.up).normalized * boundedSpeed;
            return true;
        }

        public void ApplySave(Vector3 direction, float speed, TeamId goalieTeam)
        {
            if (carrier != null) ClearCarrier(false);
            ClearIntendedPass();
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
            ClearIntendedPass();
            body.position = position;
            body.rotation = Quaternion.identity;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            PositionReset?.Invoke(position);
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

        private void PrepareRelease(PlayerController player)
        {
            LastPlayerTouchId = player.PlayerId;
            LastImpulseReleasePlayerId = player.PlayerId;
            ImpulseReleaseSequence++;
            ClearCarrier(true);
        }

        private void EstablishCarrier(PlayerController player, StickPuckInteraction stick)
        {
            ClearIntendedPass();
            carrier = player;
            carrierStick = stick;
            LastPlayerTouchId = player.PlayerId;
            PossessionTeam = player.Team;
            CarrierChanged?.Invoke(carrier);
        }

        private void ClearIntendedPass()
        {
            intendedPassReceiver = null;
            intendedReceptionZone = null;
            intendedPassExpiresAt = 0f;
            intendedPassQuality = 0f;
        }

        internal bool TryCompletePassReception(PlayerController player, StickPuckInteraction stick, float entrySpeed)
        {
            if (carrier != null || player == null || stick == null || player != intendedPassReceiver) return false;

            Vector3 receiverVelocity = player.Movement != null ? player.Movement.Velocity : Vector3.zero;
            Vector3 toControlPoint = Vector3.ProjectOnPlane(stick.ControlPoint - body.position, Vector3.up);
            float controlledEntrySpeed = Mathf.Min(
                Vector3.ProjectOnPlane(body.linearVelocity - receiverVelocity, Vector3.up).magnitude,
                Mathf.Max(0f, entrySpeed));
            Vector3 entryDirection = toControlPoint.sqrMagnitude > 0.0001f
                ? toControlPoint.normalized
                : Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
            body.linearVelocity = receiverVelocity + entryDirection * controlledEntrySpeed;
            EstablishCarrier(player, stick);
            return true;
        }

        private void FixedUpdate()
        {
            ContainInRink();
            if (carrier == null)
            {
                TickPassReception();
                return;
            }
            if (carrierStick == null) return;
            Vector3 target = carrierStick.ControlPoint;
            target.y = body.position.y;
            Vector3 carrierVelocity = carrier.Movement != null ? carrier.Movement.Velocity : Vector3.zero;
            target = ConstrainCarryMotion(target, ref carrierVelocity);
            body.AddForce(CalculateCarryAcceleration(target, carrierVelocity), ForceMode.Acceleration);
            body.linearVelocity = Vector3.ClampMagnitude(body.linearVelocity, maximumCarrySpeed);
        }

        private Vector3 ConstrainCarryMotion(Vector3 target, ref Vector3 targetVelocity)
        {
            if (!hasRinkBoundary) return target;

            Vector3 containedTarget = ClampToRoundedRink(
                target, rinkWidth, rinkLength, rinkCornerRadius, rinkClearance);
            targetVelocity = RemoveOutwardVelocity(targetVelocity, containedTarget - target);
            return containedTarget;
        }

        private void ContainInRink()
        {
            if (!hasRinkBoundary || body == null) return;

            Vector3 contained = ClampToRoundedRink(
                body.position, rinkWidth, rinkLength, rinkCornerRadius, rinkClearance);
            Vector3 correction = contained - body.position;
            if (correction.sqrMagnitude <= 0.000001f) return;

            body.position = contained;
            body.linearVelocity = RemoveOutwardVelocity(body.linearVelocity, correction);
        }

        internal static Vector3 RemoveOutwardVelocity(Vector3 velocity, Vector3 inwardCorrection)
        {
            if (inwardCorrection.sqrMagnitude <= 0.000001f) return velocity;
            Vector3 inward = inwardCorrection.normalized;
            float inwardSpeed = Vector3.Dot(velocity, inward);
            return inwardSpeed < 0f ? velocity - inward * inwardSpeed : velocity;
        }

        internal static Vector3 ClampToRoundedRink(Vector3 position, float width, float length,
            float cornerRadius, float clearance)
        {
            float halfWidth = Mathf.Max(0f, width * 0.5f);
            float halfLength = Mathf.Max(0f, length * 0.5f);
            float boundedRadius = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(halfWidth, halfLength));
            float innerRadius = Mathf.Max(0f, boundedRadius - Mathf.Max(0f, clearance));
            float coreHalfWidth = halfWidth - boundedRadius;
            float coreHalfLength = halfLength - boundedRadius;
            Vector2 point = new(position.x, position.z);
            Vector2 core = new(
                Mathf.Clamp(point.x, -coreHalfWidth, coreHalfWidth),
                Mathf.Clamp(point.y, -coreHalfLength, coreHalfLength));
            Vector2 offset = point - core;

            if (offset.sqrMagnitude > innerRadius * innerRadius)
                point = innerRadius > 0f ? core + offset.normalized * innerRadius : core;

            return new Vector3(point.x, position.y, point.y);
        }

        internal void TickRinkContainmentForValidation() => ContainInRink();

        internal Vector3 ConstrainCarryMotionForValidation(Vector3 target, ref Vector3 targetVelocity) =>
            ConstrainCarryMotion(target, ref targetVelocity);

        internal void TickPassReception()
        {
            if (intendedReceptionZone == null || intendedPassReceiver == null) return;
            if (Time.time >= intendedPassExpiresAt)
            {
                ClearIntendedPass();
                return;
            }
            if (intendedReceptionZone.TryReceive(this)) return;

            Vector3 receiverVelocity = intendedPassReceiver.Movement != null
                ? intendedPassReceiver.Movement.Velocity : Vector3.zero;
            Vector3 toReceiver = Vector3.ProjectOnPlane(intendedPassReceiver.transform.position - body.position, Vector3.up);
            Vector3 relativeVelocity = Vector3.ProjectOnPlane(body.linearVelocity - receiverVelocity, Vector3.up);
            if (toReceiver.sqrMagnitude > intendedReceptionZone.Radius * intendedReceptionZone.Radius
                && relativeVelocity.sqrMagnitude > 0.01f && Vector3.Dot(relativeVelocity, toReceiver) <= 0f)
                ClearIntendedPass();
        }

        internal Vector3 CalculateCarryAcceleration(Vector3 target, Vector3 targetVelocity)
        {
            Vector3 positionError = target - body.position;
            Vector3 velocityError = targetVelocity - body.linearVelocity;
            float controlMultiplier = carrierStick != null ? carrierStick.CarryControlMultiplier : 1f;
            return positionError * controlStrength * controlMultiplier
                + velocityError * controlVelocityDamping * controlMultiplier;
        }

    }
}
