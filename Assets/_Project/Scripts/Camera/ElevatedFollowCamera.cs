/*
 * IceClash Phase 1 elevated follow camera.
 * Separates camera behavior from skater movement and frames the controlled player with the puck.
 * Its fixed negative-Z trailing offset keeps the rink's long Z axis vertical in the Game view.
 */

using UnityEngine;

namespace IceClash.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public sealed class ElevatedFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Transform puck;
        [SerializeField] private Vector3 offset = new(0f, 16f, -16f);
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private float puckBias = 0.3f;

        private Vector3 followVelocity;

        public void Configure(Transform followTarget, Transform puckTarget)
        {
            target = followTarget;
            puck = puckTarget;
            transform.position = target.position + offset;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            transform.position = Vector3.SmoothDamp(transform.position, target.position + offset, ref followVelocity, smoothTime);
            Vector3 focus = target.position;
            if (puck != null) focus = Vector3.Lerp(focus, puck.position, puckBias);
            transform.rotation = Quaternion.LookRotation((focus - transform.position).normalized, Vector3.up);
        }
    }
}
