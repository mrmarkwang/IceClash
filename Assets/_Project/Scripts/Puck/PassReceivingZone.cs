/*
 * IceClash intended-pass reception zone.
 * Combines receiver CTR with passer PAS to define bounded local capture and entry
 * speed, then delegates the velocity-matched transition to PuckController.
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

        public float ReceptionQuality => EvaluateReceptionQuality(
            player != null ? player.Attributes.Normalized(PlayerAttribute.Control) : 0f,
            player != null && player.Puck != null ? player.Puck.IntendedPassQuality : 0f);
        public float Radius => player != null ? EvaluateRadius(ReceptionQuality) : receptionRadius;
        public float EntrySpeed => player != null ? EvaluateEntrySpeed(ReceptionQuality) : receptionEntrySpeed;

        public void Configure(PlayerController owner, StickPuckInteraction receivingStick)
        {
            player = owner;
            stick = receivingStick;
        }

        internal bool TryReceive(PuckController puck)
        {
            if (puck == null || player == null || stick == null || puck.Body == null) return false;

            Vector3 toReceiver = Vector3.ProjectOnPlane(player.transform.position - puck.Body.position, Vector3.up);
            float radius = Radius;
            if (toReceiver.sqrMagnitude > radius * radius) return false;

            return puck.TryCompletePassReception(player, stick, EntrySpeed);
        }

        internal static float EvaluateReceptionQuality(float receiverControl, float passerPassing) =>
            Mathf.Clamp01(receiverControl) * 0.6f + Mathf.Clamp01(passerPassing) * 0.4f;
        internal static float EvaluateRadius(float quality) => Mathf.Lerp(1.4f, 2.1f, Mathf.Clamp01(quality));
        internal static float EvaluateEntrySpeed(float quality) => Mathf.Lerp(4.5f, 7.5f, Mathf.Clamp01(quality));
    }
}
