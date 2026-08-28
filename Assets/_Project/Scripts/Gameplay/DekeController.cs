/*
 * IceClash explicit deke action window.
 * Converts only a DEKE signal while carrying into bounded skill, speed, timing,
 * and fatigue-based protection; it never supplies movement or decisions.
 */

using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Gameplay
{
    public sealed class DekeController : MonoBehaviour
    {
        private const float CooldownSeconds = 0.55f;
        private PlayerController player;
        private PuckController puck;
        private float activeUntil;
        private float nextDekeTime;

        public bool IsActive => Time.time < activeUntil;
        public float ProtectionBonus => EvaluateProtectionBonus(Time.time,
            player != null && player.Movement != null ? player.Movement.NormalizedSpeed : 0f,
            player != null ? player.PerformanceFactor : 0f);
        public int StartedCount { get; private set; }

        public void Configure(PlayerController owner, PuckController controlledPuck)
        {
            player = owner;
            puck = controlledPuck;
        }

        public bool Tick(bool pressed)
        {
            if (!pressed) return false;
            return TryBegin(Time.time);
        }

        public void ResetAction()
        {
            activeUntil = 0f;
            nextDekeTime = 0f;
        }

        internal float EvaluateWindowSeconds() => EvaluateWindowSeconds(
            player != null ? player.Attributes.Normalized(PlayerAttribute.Control) : 0f,
            player != null ? player.Attributes.Normalized(PlayerAttribute.Agility) : 0f);

        internal static float EvaluateWindowSeconds(float normalizedControl, float normalizedAgility) =>
            Mathf.Lerp(0.18f, 0.42f, Mathf.Clamp01((normalizedControl + normalizedAgility) * 0.5f));

        internal bool TryBeginForValidation(float now) => TryBegin(now);
        internal bool IsActiveAtForValidation(float now) => now < activeUntil;
        internal float EvaluateProtectionBonusForValidation(float now, float normalizedSpeed, float performance) =>
            EvaluateProtectionBonus(now, normalizedSpeed, performance);

        private bool TryBegin(float now)
        {
            if (player == null || puck == null || !puck.IsCarriedBy(player) || now < nextDekeTime) return false;
            activeUntil = now + EvaluateWindowSeconds();
            nextDekeTime = now + CooldownSeconds;
            StartedCount++;
            return true;
        }

        private float EvaluateProtectionBonus(float now, float normalizedSpeed, float performance)
        {
            if (player == null || now >= activeUntil) return 0f;
            float duration = EvaluateWindowSeconds();
            float timing = Mathf.Clamp01((activeUntil - now) / Mathf.Max(duration, 0.01f));
            float skill = (player.Attributes.Normalized(PlayerAttribute.Control)
                + player.Attributes.Normalized(PlayerAttribute.Agility)) * 0.5f;
            float speed = Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(normalizedSpeed));
            return 0.15f * skill * speed * timing * Mathf.Clamp(performance, 0.68f, 1f);
        }
    }
}
