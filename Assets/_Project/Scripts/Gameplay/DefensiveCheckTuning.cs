/*
 * IceClash persisted defensive-check tuning.
 * Provides Inspector-editable body/pull ranges, cooldown, puck pace, and bounded
 * separation values with runtime clamps that remain safe for malformed assets.
 */

using UnityEngine;

namespace IceClash.Gameplay
{
    [CreateAssetMenu(fileName = "DefensiveCheckTuning", menuName = "IceClash/Defensive Check Tuning")]
    public sealed class DefensiveCheckTuning : ScriptableObject
    {
        internal const float MaximumBodyImpulse = 6f;

        [SerializeField] private float bodyRange = 1.35f;
        [SerializeField] private float pullRange = 2.7f;
        [SerializeField] private float pullForwardDot = 0.25f;
        [SerializeField] private float cooldownSeconds = 0.65f;
        [SerializeField] private float bodyPuckSpeed = 8f;
        [SerializeField] private float pullPuckSpeed = 6f;
        [SerializeField] private float bodyImpulse = 4.5f;
        [SerializeField] private float impulseDecay = 14f;

        public Values RuntimeValues => Sanitize(bodyRange, pullRange, pullForwardDot, cooldownSeconds,
            bodyPuckSpeed, pullPuckSpeed, bodyImpulse, impulseDecay);

        private void OnValidate()
        {
            Values values = RuntimeValues;
            bodyRange = values.BodyRange;
            pullRange = values.PullRange;
            pullForwardDot = values.PullForwardDot;
            cooldownSeconds = values.CooldownSeconds;
            bodyPuckSpeed = values.BodyPuckSpeed;
            pullPuckSpeed = values.PullPuckSpeed;
            bodyImpulse = values.BodyImpulse;
            impulseDecay = values.ImpulseDecay;
        }

        internal static Values Sanitize(float bodyRangeValue, float pullRangeValue, float forwardDotValue,
            float cooldownValue, float bodyPuckSpeedValue, float pullPuckSpeedValue,
            float bodyImpulseValue, float impulseDecayValue)
        {
            bodyRangeValue = FiniteOr(bodyRangeValue, 1.35f);
            pullRangeValue = FiniteOr(pullRangeValue, 2.7f);
            forwardDotValue = FiniteOr(forwardDotValue, 0.25f);
            cooldownValue = FiniteOr(cooldownValue, 0.65f);
            bodyPuckSpeedValue = FiniteOr(bodyPuckSpeedValue, 8f);
            pullPuckSpeedValue = FiniteOr(pullPuckSpeedValue, 6f);
            bodyImpulseValue = FiniteOr(bodyImpulseValue, 4.5f);
            impulseDecayValue = FiniteOr(impulseDecayValue, 14f);
            float safeBodyRange = Mathf.Clamp(bodyRangeValue, 0.5f, 2f);
            return new Values(
                safeBodyRange,
                Mathf.Clamp(pullRangeValue, Mathf.Max(0.6f, safeBodyRange + 0.1f), 3.5f),
                Mathf.Clamp01(forwardDotValue),
                Mathf.Clamp(cooldownValue, 0.2f, 2f),
                Mathf.Clamp(bodyPuckSpeedValue, 1f, 15f),
                Mathf.Clamp(pullPuckSpeedValue, 1f, 15f),
                Mathf.Clamp(bodyImpulseValue, 0f, MaximumBodyImpulse),
                Mathf.Clamp(impulseDecayValue, 4f, 30f));
        }

        private static float FiniteOr(float value, float fallback)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
        }

        public readonly struct Values
        {
            public Values(float bodyRange, float pullRange, float pullForwardDot, float cooldownSeconds,
                float bodyPuckSpeed, float pullPuckSpeed, float bodyImpulse, float impulseDecay)
            {
                BodyRange = bodyRange;
                PullRange = pullRange;
                PullForwardDot = pullForwardDot;
                CooldownSeconds = cooldownSeconds;
                BodyPuckSpeed = bodyPuckSpeed;
                PullPuckSpeed = pullPuckSpeed;
                BodyImpulse = bodyImpulse;
                ImpulseDecay = impulseDecay;
            }

            public float BodyRange { get; }
            public float PullRange { get; }
            public float PullForwardDot { get; }
            public float CooldownSeconds { get; }
            public float BodyPuckSpeed { get; }
            public float PullPuckSpeed { get; }
            public float BodyImpulse { get; }
            public float ImpulseDecay { get; }
        }
    }
}
