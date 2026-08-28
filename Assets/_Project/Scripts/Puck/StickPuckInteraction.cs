/*
 * IceClash Phase 1 stick/puck interaction.
 * Owns a clearly forward control point and CTR-bounded proximity claims. Explicit
 * dekes add a stable input-driven lateral control offset but never generate movement.
 */

using IceClash.Player;
using UnityEngine;

namespace IceClash.Puck
{
    public sealed class StickPuckInteraction : MonoBehaviour
    {
        [SerializeField] private float forwardOffset = 1.15f;
        [SerializeField] private float lateralSway = 0.1f;
        [SerializeField] private float claimRadius = 1.55f;
        [SerializeField] private float maximumClaimSpeed = 15f;

        private PlayerController player;
        private PuckController puck;
        private bool interactionEnabled = true;

        public float ClaimRadius => player != null
            ? EvaluateClaimRadius(player.Attributes.Normalized(PlayerAttribute.Control))
            : claimRadius;
        public float MaximumClaimSpeed => player != null
            ? EvaluateMaximumClaimSpeed(player.Attributes.Normalized(PlayerAttribute.Control))
            : maximumClaimSpeed;
        public float CarryControlMultiplier => player != null
            ? EvaluateCarryControlMultiplier(player.Attributes.Normalized(PlayerAttribute.Control)) * player.PerformanceFactor
            : 1f;
        public Vector3 ControlPoint => transform.position + transform.forward * forwardOffset
            + transform.right * DekeLateralOffset + Vector3.up * 0.28f;
        public bool HasPuck => puck != null && puck.IsCarriedBy(player);

        public void Configure(PlayerController owner, PuckController controlledPuck) { player = owner; puck = controlledPuck; }
        public void SetInteractionEnabled(bool value) => interactionEnabled = value;
        internal static float EvaluateClaimRadius(float normalizedControl) => Mathf.Lerp(1.25f, 1.85f, Mathf.Clamp01(normalizedControl));
        internal static float EvaluateMaximumClaimSpeed(float normalizedControl) => Mathf.Lerp(12f, 17f, Mathf.Clamp01(normalizedControl));
        internal static float EvaluateCarryControlMultiplier(float normalizedControl) => Mathf.Lerp(0.75f, 1.25f, Mathf.Clamp01(normalizedControl));

        private float DekeLateralOffset
        {
            get
            {
                if (player == null || player.Deke == null || !player.Deke.IsActive) return 0f;
                float lateral = player.MoveInput.x;
                return Mathf.Abs(lateral) < 0.01f ? 0f : Mathf.Sign(lateral) * lateralSway * 1.8f;
            }
        }

        private void Update()
        {
            if (interactionEnabled && player != null && puck != null && !HasPuck) puck.TryClaim(player, this);
        }
    }
}
