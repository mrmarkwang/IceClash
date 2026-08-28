/*
 * IceClash constrained skater attribute build.
 * Owns level-derived points, nine bounded ratings, progressive costs, atomic
 * allocation, normalized lookup, copying, and validated prototype archetypes.
 */

using System;
using IceClash.Core;
using UnityEngine;

namespace IceClash.Player
{
    public enum PlayerAttribute { Speed, Acceleration, Agility, Stamina, Control, Shooting, Passing, Strength, Defense }
    public enum PlayerBuildPreset { Speed, Sniper, Playmaker, Power, TwoWay }

    [Serializable]
    public sealed class PlayerAttributeBuild
    {
        public const int MinimumLevel = 1;
        public const int MaximumLevel = 50;
        public const int MinimumRating = 40;
        public const int MaximumRating = 95;
        public const int PointsPerLevel = 8;

        [SerializeField, Range(MinimumLevel, MaximumLevel)] private int level = MinimumLevel;
        [SerializeField, Range(MinimumRating, MaximumRating)] private int speed = MinimumRating;
        [SerializeField, Range(MinimumRating, MaximumRating)] private int acceleration = MinimumRating;
        [SerializeField, Range(MinimumRating, MaximumRating)] private int agility = MinimumRating;
        [SerializeField, Range(MinimumRating, MaximumRating)] private int stamina = MinimumRating;
        [SerializeField, Range(MinimumRating, MaximumRating)] private int control = MinimumRating;
        [SerializeField, Range(MinimumRating, MaximumRating)] private int shooting = MinimumRating;
        [SerializeField, Range(MinimumRating, MaximumRating)] private int passing = MinimumRating;
        [SerializeField, Range(MinimumRating, MaximumRating)] private int strength = MinimumRating;
        [SerializeField, Range(MinimumRating, MaximumRating)] private int defense = MinimumRating;

        public event Action Changed;

        public PlayerAttributeBuild() { }
        public PlayerAttributeBuild(int playerLevel) => level = Mathf.Clamp(playerLevel, MinimumLevel, MaximumLevel);

        public int Level => level;
        public int Speed => speed;
        public int Acceleration => acceleration;
        public int Agility => agility;
        public int Stamina => stamina;
        public int Control => control;
        public int Shooting => shooting;
        public int Passing => passing;
        public int Strength => strength;
        public int Defense => defense;
        public int PointBudget => BudgetForLevel(level);
        public int SpentPoints => CostToRating(speed) + CostToRating(acceleration) + CostToRating(agility)
            + CostToRating(stamina) + CostToRating(control) + CostToRating(shooting)
            + CostToRating(passing) + CostToRating(strength) + CostToRating(defense);
        public int RemainingPoints => PointBudget - SpentPoints;
        public bool IsValid => level >= MinimumLevel && level <= MaximumLevel
            && AllRatingsInRange() && SpentPoints <= PointBudget;

        public int Get(PlayerAttribute attribute) => attribute switch
        {
            PlayerAttribute.Speed => speed,
            PlayerAttribute.Acceleration => acceleration,
            PlayerAttribute.Agility => agility,
            PlayerAttribute.Stamina => stamina,
            PlayerAttribute.Control => control,
            PlayerAttribute.Shooting => shooting,
            PlayerAttribute.Passing => passing,
            PlayerAttribute.Strength => strength,
            PlayerAttribute.Defense => defense,
            _ => throw new ArgumentOutOfRangeException(nameof(attribute), attribute, null)
        };

        public float Normalized(PlayerAttribute attribute) => NormalizeRating(Get(attribute));

        public bool TrySet(PlayerAttribute attribute, int targetRating)
        {
            if (targetRating < MinimumRating || targetRating > MaximumRating) return false;
            int current = Get(attribute);
            int projected = SpentPoints - CostToRating(current) + CostToRating(targetRating);
            if (projected > PointBudget) return false;
            SetUnchecked(attribute, targetRating);
            Changed?.Invoke();
            return true;
        }

        public void CopyFrom(PlayerAttributeBuild source)
        {
            if (source == null || !source.IsValid) return;
            level = source.level;
            speed = source.speed;
            acceleration = source.acceleration;
            agility = source.agility;
            stamina = source.stamina;
            control = source.control;
            shooting = source.shooting;
            passing = source.passing;
            strength = source.strength;
            defense = source.defense;
            Changed?.Invoke();
        }

        public PlayerAttributeBuild Clone()
        {
            PlayerAttributeBuild clone = new();
            clone.CopyFrom(this);
            return clone;
        }

        public static int BudgetForLevel(int playerLevel) => (Mathf.Clamp(playerLevel, MinimumLevel, MaximumLevel) - 1) * PointsPerLevel;

        public static int CostToRating(int rating)
        {
            int target = Mathf.Clamp(rating, MinimumRating, MaximumRating);
            int firstTier = Mathf.Max(0, Mathf.Min(target, 69) - MinimumRating);
            int secondTier = Mathf.Max(0, Mathf.Min(target, 84) - 69) * 2;
            int thirdTier = Mathf.Max(0, target - 84) * 3;
            return firstTier + secondTier + thirdTier;
        }

        public static float NormalizeRating(int rating) => Mathf.InverseLerp(MinimumRating, MaximumRating,
            Mathf.Clamp(rating, MinimumRating, MaximumRating));

        public static PlayerAttributeBuild CreatePreset(PlayerBuildPreset preset)
        {
            int[] ratings = preset switch
            {
                PlayerBuildPreset.Speed => new[] { 78, 75, 73, 58, 52, 45, 45, 45, 45 },
                PlayerBuildPreset.Sniper => new[] { 60, 60, 72, 55, 74, 78, 50, 43, 43 },
                PlayerBuildPreset.Playmaker => new[] { 62, 62, 72, 55, 74, 45, 76, 43, 48 },
                PlayerBuildPreset.Power => new[] { 55, 58, 50, 72, 60, 75, 45, 78, 41 },
                PlayerBuildPreset.TwoWay => new[] { 58, 58, 60, 68, 68, 49, 67, 55, 69 },
                _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, null)
            };
            PlayerAttributeBuild build = new(25);
            for (int i = 0; i < ratings.Length; i++)
                if (!build.TrySet((PlayerAttribute)i, ratings[i]))
                    throw new InvalidOperationException($"Invalid {preset} preset at {(PlayerAttribute)i}={ratings[i]}.");
            return build;
        }

        public static PlayerBuildPreset PresetForRole(SkaterRole role) => role switch
        {
            SkaterRole.Center => PlayerBuildPreset.Playmaker,
            SkaterRole.LeftWing => PlayerBuildPreset.Sniper,
            SkaterRole.RightWing => PlayerBuildPreset.Speed,
            SkaterRole.LeftDefense => PlayerBuildPreset.Power,
            SkaterRole.RightDefense => PlayerBuildPreset.TwoWay,
            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };

        private bool AllRatingsInRange()
        {
            foreach (PlayerAttribute attribute in Enum.GetValues(typeof(PlayerAttribute)))
                if (Get(attribute) < MinimumRating || Get(attribute) > MaximumRating) return false;
            return true;
        }

        private void SetUnchecked(PlayerAttribute attribute, int value)
        {
            switch (attribute)
            {
                case PlayerAttribute.Speed: speed = value; break;
                case PlayerAttribute.Acceleration: acceleration = value; break;
                case PlayerAttribute.Agility: agility = value; break;
                case PlayerAttribute.Stamina: stamina = value; break;
                case PlayerAttribute.Control: control = value; break;
                case PlayerAttribute.Shooting: shooting = value; break;
                case PlayerAttribute.Passing: passing = value; break;
                case PlayerAttribute.Strength: strength = value; break;
                case PlayerAttribute.Defense: defense = value; break;
                default: throw new ArgumentOutOfRangeException(nameof(attribute), attribute, null);
            }
        }
    }
}
