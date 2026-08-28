/*
 * IceClash Phase 1 skating motor.
 * Provides camera-relative skating plus bounded, decaying body-check separation.
 * SPD, ACC, AGI, and fatigue scale capability without ever creating direction input.
 */

using UnityEngine;

namespace IceClash.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMovementController : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float deceleration = 10f;
        [SerializeField, Min(0f)] private float reversalBraking = 24f;

        private CharacterController characterController;
        private Vector2 moveInput;
        private Vector3 planarVelocity;
        private Vector3 externalVelocity;
        private float externalVelocityDecay = 14f;
        private float performanceScale = 1f;
        private float configuredMaximumSpeed = 6.4f;
        private float configuredAcceleration = 13.5f;
        private float configuredLowSpeedTurnRate = 12f;
        private float configuredHighSpeedTurnRate = 6f;
        private bool movementEnabled = true;

        public Vector3 Velocity => planarVelocity;
        public Vector3 ExternalVelocity => externalVelocity;
        public float NormalizedSpeed => EffectiveMaximumSpeed <= 0f ? 0f : planarVelocity.magnitude / EffectiveMaximumSpeed;
        public bool IsMoving => planarVelocity.sqrMagnitude > 0.04f;
        public float EffectiveMaximumSpeed => configuredMaximumSpeed * performanceScale;
        public float EffectiveAcceleration => configuredAcceleration * performanceScale;
        public float ConfiguredLowSpeedTurnRate => configuredLowSpeedTurnRate;
        public float ConfiguredHighSpeedTurnRate => configuredHighSpeedTurnRate;

        private void Awake() => characterController = GetComponent<CharacterController>();
        public void SetInput(Vector2 input) => moveInput = Vector2.ClampMagnitude(input, 1f);
        public void SetPerformanceScale(float value) => performanceScale = Mathf.Clamp(value, 0.68f, 1f);
        public void ConfigureAttributes(PlayerAttributeBuild attributes)
        {
            float speedRating = attributes != null ? attributes.Normalized(PlayerAttribute.Speed) : 0f;
            float accelerationRating = attributes != null ? attributes.Normalized(PlayerAttribute.Acceleration) : 0f;
            float agilityRating = attributes != null ? attributes.Normalized(PlayerAttribute.Agility) : 0f;
            configuredMaximumSpeed = EvaluateMaximumSpeed(speedRating);
            configuredAcceleration = EvaluateAcceleration(accelerationRating);
            configuredLowSpeedTurnRate = EvaluateLowSpeedTurnRate(agilityRating);
            configuredHighSpeedTurnRate = EvaluateHighSpeedTurnRate(agilityRating);
        }

        internal static float EvaluateMaximumSpeed(float normalizedSpeed) => Mathf.Lerp(6.4f, 9.6f, Mathf.Clamp01(normalizedSpeed));
        internal static float EvaluateAcceleration(float normalizedAcceleration) => Mathf.Lerp(13.5f, 22.5f, Mathf.Clamp01(normalizedAcceleration));
        internal static float EvaluateLowSpeedTurnRate(float normalizedAgility) => Mathf.Lerp(12f, 20f, Mathf.Clamp01(normalizedAgility));
        internal static float EvaluateHighSpeedTurnRate(float normalizedAgility) => Mathf.Lerp(6f, 12f, Mathf.Clamp01(normalizedAgility));
        internal void SetPlanarVelocityForValidation(Vector3 velocity) => planarVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
        internal static Vector3 CameraRelativeDirectionForValidation(Vector2 input) => CameraRelativeDirection(input);
        internal void StepPlanarForValidation(Vector2 input, float deltaTime)
        {
            moveInput = Vector2.ClampMagnitude(input, 1f);
            TickPlanarVelocity(Mathf.Max(0f, deltaTime));
        }
        public void SetMovementEnabled(bool value)
        {
            movementEnabled = value;
            if (!value)
            {
                moveInput = Vector2.zero;
                externalVelocity = Vector3.zero;
            }
        }

        public void ApplyExternalImpulse(Vector3 impulse, float maximumExternalSpeed, float decay)
        {
            Vector3 planarImpulse = Vector3.ProjectOnPlane(impulse, Vector3.up);
            externalVelocity = Vector3.ClampMagnitude(externalVelocity + planarImpulse,
                Mathf.Clamp(maximumExternalSpeed, 0f, 6f));
            externalVelocityDecay = Mathf.Clamp(decay, 4f, 30f);
        }

        private void Update()
        {
            TickPlanarVelocity(Time.deltaTime);

            if (planarVelocity.sqrMagnitude > 0.01f)
            {
                float turnRate = CurrentTurnRate;
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(planarVelocity.normalized, Vector3.up), turnRate * Time.deltaTime);
            }

            float vertical = characterController.isGrounded ? -1f : Physics.gravity.y;
            characterController.Move((planarVelocity + externalVelocity + Vector3.up * vertical) * Time.deltaTime);
            externalVelocity = Vector3.MoveTowards(externalVelocity, Vector3.zero,
                externalVelocityDecay * Time.deltaTime);
        }

        private void TickPlanarVelocity(float deltaTime)
        {
            Vector3 rawDesiredDirection = CameraRelativeDirection(moveInput);
            float desiredSpeed = movementEnabled ? EffectiveMaximumSpeed * Mathf.Clamp01(moveInput.magnitude) : 0f;
            bool reversing = rawDesiredDirection.sqrMagnitude > 0.01f && planarVelocity.sqrMagnitude > 0.1f
                && Vector3.Dot(planarVelocity.normalized, rawDesiredDirection) < -0.15f;
            if (reversing)
            {
                planarVelocity = Vector3.MoveTowards(planarVelocity, Vector3.zero, reversalBraking * deltaTime);
                return;
            }
            Vector3 desiredDirection = rawDesiredDirection;
            if (desiredDirection.sqrMagnitude > 0.01f && planarVelocity.sqrMagnitude > 0.01f)
                desiredDirection = Vector3.RotateTowards(planarVelocity.normalized, desiredDirection,
                    CurrentTurnRate * deltaTime, 0f).normalized;
            Vector3 desiredVelocity = desiredDirection * desiredSpeed;
            float rate = desiredSpeed > 0.01f ? EffectiveAcceleration : deceleration;
            planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity, rate * deltaTime);
        }

        private float CurrentTurnRate => Mathf.Lerp(configuredLowSpeedTurnRate, configuredHighSpeedTurnRate,
            Mathf.Clamp01(NormalizedSpeed)) * performanceScale;

        public void ResetMotion(Vector3 position, Quaternion rotation)
        {
            bool wasEnabled = characterController.enabled;
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            characterController.enabled = wasEnabled;
            planarVelocity = Vector3.zero;
            externalVelocity = Vector3.zero;
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
