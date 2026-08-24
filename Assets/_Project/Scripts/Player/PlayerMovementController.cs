/*
 * IceClash Phase 1 skating motor.
 * Provides camera-relative analog skating with acceleration, glide, braking,
 * momentum, and speed-aware turning independently of puck and AI decisions.
 */

using UnityEngine;

namespace IceClash.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovementController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float maximumSpeed = 8f;
        [SerializeField, Min(0f)] private float acceleration = 18f;
        [SerializeField, Min(0f)] private float deceleration = 10f;
        [SerializeField, Min(0f)] private float reversalBraking = 24f;
        [SerializeField, Min(0f)] private float lowSpeedTurnRate = 16f;
        [SerializeField, Min(0f)] private float highSpeedTurnRate = 8f;

        private CharacterController characterController;
        private Vector2 moveInput;
        private Vector3 planarVelocity;
        private float speedScale = 1f;
        private bool movementEnabled = true;

        public Vector3 Velocity => planarVelocity;
        public float NormalizedSpeed => maximumSpeed <= 0f ? 0f : planarVelocity.magnitude / maximumSpeed;
        public bool IsMoving => planarVelocity.sqrMagnitude > 0.04f;

        private void Awake() => characterController = GetComponent<CharacterController>();
        public void SetInput(Vector2 input) => moveInput = Vector2.ClampMagnitude(input, 1f);
        public void SetSpeedScale(float value) => speedScale = Mathf.Clamp(value, 0.4f, 1.25f);
        public void SetMovementEnabled(bool value) { movementEnabled = value; if (!value) moveInput = Vector2.zero; }

        private void Update()
        {
            Vector3 desiredDirection = CameraRelativeDirection(moveInput);
            float desiredSpeed = movementEnabled ? maximumSpeed * speedScale * Mathf.Clamp01(moveInput.magnitude) : 0f;
            Vector3 desiredVelocity = desiredDirection * desiredSpeed;
            float rate = desiredSpeed > 0.01f ? acceleration : deceleration;
            if (desiredDirection.sqrMagnitude > 0.01f && planarVelocity.sqrMagnitude > 0.1f
                && Vector3.Dot(planarVelocity.normalized, desiredDirection) < -0.15f) rate = reversalBraking;
            planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity, rate * Time.deltaTime);

            if (planarVelocity.sqrMagnitude > 0.01f)
            {
                float turnRate = Mathf.Lerp(lowSpeedTurnRate, highSpeedTurnRate, Mathf.Clamp01(NormalizedSpeed));
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(planarVelocity.normalized, Vector3.up), turnRate * Time.deltaTime);
            }

            float vertical = characterController.isGrounded ? -1f : Physics.gravity.y;
            characterController.Move((planarVelocity + Vector3.up * vertical) * Time.deltaTime);
        }

        public void ResetMotion(Vector3 position, Quaternion rotation)
        {
            bool wasEnabled = characterController.enabled;
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            characterController.enabled = wasEnabled;
            planarVelocity = Vector3.zero;
            moveInput = Vector2.zero;
        }

        private static Vector3 CameraRelativeDirection(Vector2 input)
        {
            Camera view = Camera.main;
            Vector3 forward = view != null ? Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = view != null ? Vector3.ProjectOnPlane(view.transform.right, Vector3.up).normalized : Vector3.right;
            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        }
    }
}
