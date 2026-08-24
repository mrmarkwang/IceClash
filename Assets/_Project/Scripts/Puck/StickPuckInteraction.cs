/*
 * IceClash Phase 1 stick/puck interaction.
 * Owns the forward, subtly moving control point and proximity claim attempts so
 * puck feel remains independent from locomotion and action selection.
 */

using IceClash.Player;
using UnityEngine;

namespace IceClash.Puck
{
    public sealed class StickPuckInteraction : MonoBehaviour
    {
        [SerializeField] private float forwardOffset = 0.92f;
        [SerializeField] private float lateralSway = 0.13f;
        [SerializeField] private float swayFrequency = 5f;
        [SerializeField] private float claimRadius = 1.25f;
        [SerializeField] private float maximumClaimSpeed = 15f;

        private PlayerController player;
        private PuckController puck;
        private bool interactionEnabled = true;

        public float ClaimRadius => claimRadius;
        public float MaximumClaimSpeed => maximumClaimSpeed;
        public Vector3 ControlPoint => transform.position + transform.forward * forwardOffset
            + transform.right * (Mathf.Sin(Time.time * swayFrequency + GetHashCode() * 0.01f) * lateralSway) + Vector3.up * 0.28f;
        public bool HasPuck => puck != null && puck.IsCarriedBy(player);

        public void Configure(PlayerController owner, PuckController controlledPuck) { player = owner; puck = controlledPuck; }
        public void SetInteractionEnabled(bool value) => interactionEnabled = value;

        private void Update()
        {
            if (interactionEnabled && player != null && puck != null && !HasPuck) puck.TryClaim(player, this);
        }
    }
}
