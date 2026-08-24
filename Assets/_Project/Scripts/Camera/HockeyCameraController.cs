/*
 * IceClash Phase 1 mobile hockey camera.
 * Smoothly follows the selected skater, biases framing toward the puck, preserves
 * a stable rink orientation, and blends both position and look focus across manual
 * or possession-driven control transfers without snapping.
 */

using UnityEngine;

namespace IceClash.CameraSystem
{
    [RequireComponent(typeof(Camera))]
    public sealed class HockeyCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Transform puck;
        [SerializeField] private Vector3 offset = new(0f, 16.5f, -15f);
        [SerializeField] private float smoothTime = 0.2f;
        [SerializeField] private float retargetFocusSmoothTime = 0.3f;
        [SerializeField, Range(0f, 0.6f)] private float puckBias = 0.32f;
        [SerializeField] private float maximumFocusSeparation = 8f;
        private Vector3 velocity;
        private Vector3 focusVelocity;
        private Vector3 smoothedTargetPosition;

        public Transform Target => target;
        public void Configure(Transform followTarget, Transform puckTarget) { target = followTarget; puck = puckTarget; Snap(); }
        public void SetTarget(Transform followTarget) => target = followTarget;

        private void LateUpdate()
        {
            if (target == null) return;
            smoothedTargetPosition = Vector3.SmoothDamp(smoothedTargetPosition, target.position, ref focusVelocity, retargetFocusSmoothTime);
            Vector3 focus = smoothedTargetPosition;
            if (puck != null)
            {
                Vector3 puckDelta = Vector3.ClampMagnitude(puck.position - target.position, maximumFocusSeparation);
                focus += puckDelta * puckBias;
            }
            transform.position = Vector3.SmoothDamp(transform.position, focus + offset, ref velocity, smoothTime);
            transform.rotation = Quaternion.LookRotation((focus - transform.position).normalized, Vector3.up);
        }

        private void Snap()
        {
            if (target == null) return;
            transform.position = target.position + offset;
            transform.rotation = Quaternion.LookRotation((target.position - transform.position).normalized, Vector3.up);
            velocity = Vector3.zero;
            focusVelocity = Vector3.zero;
            smoothedTargetPosition = target.position;
        }
    }
}
