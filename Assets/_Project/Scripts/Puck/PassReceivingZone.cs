/*
 * IceClash intended-pass reception zone.
 * Defines configurable local capture around the target player and delegates the
 * velocity-matched stick transition to PuckController once the intended pass
 * physically enters that receiver's local zone.
 */

using IceClash.Player;
using UnityEngine;

namespace IceClash.Puck
{
    public sealed class PassReceivingZone : MonoBehaviour
    {
        [SerializeField, Min(0.25f)] private float receptionRadius = 1.75f;
        [SerializeField, Min(0f)] private float receptionEntrySpeed = 6f;

        private PlayerController player;
        private StickPuckInteraction stick;

        public float Radius => receptionRadius;
        public float EntrySpeed => receptionEntrySpeed;

        public void Configure(PlayerController owner, StickPuckInteraction receivingStick)
        {
            player = owner;
            stick = receivingStick;
        }

        internal bool TryReceive(PuckController puck)
        {
            if (puck == null || player == null || stick == null || puck.Body == null) return false;

            Vector3 toReceiver = Vector3.ProjectOnPlane(player.transform.position - puck.Body.position, Vector3.up);
            if (toReceiver.sqrMagnitude > receptionRadius * receptionRadius) return false;

            return puck.TryCompletePassReception(player, stick, receptionEntrySpeed);
        }
    }
}
