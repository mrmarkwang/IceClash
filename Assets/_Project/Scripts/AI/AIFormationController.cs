/*
 * IceClash Phase 1 formation helper.
 * Maps the five conventional skater roles to mirrored center-faceoff/home positions,
 * then supplies role-aware live-play lanes that move with possession. The lanes
 * spread support around the play without preventing any role from crossing zones.
 */

using System;
using IceClash.Core;
using IceClash.Hockey;
using UnityEngine;

namespace IceClash.AI
{
    public static class AIFormationController
    {
        public static SkaterRole RoleForSlot(int slot)
        {
            return slot switch
            {
                0 => SkaterRole.Center,
                1 => SkaterRole.LeftWing,
                2 => SkaterRole.RightWing,
                3 => SkaterRole.LeftDefense,
                4 => SkaterRole.RightDefense,
                _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "A five-skater lineup requires slots 0 through 4.")
            };
        }

        public static Vector3 Home(TeamId team, int slot, int count)
        {
            if (count != 5) throw new ArgumentOutOfRangeException(nameof(count), count, "Role-aware formation requires exactly five skaters.");
            return Home(team, RoleForSlot(slot));
        }

        public static Vector3 Home(TeamId team, SkaterRole role)
        {
            float attack = team == TeamId.Blue ? 1f : -1f;
            float lateral = role switch
            {
                SkaterRole.LeftWing => -4.2f,
                SkaterRole.RightWing => 4.2f,
                SkaterRole.LeftDefense => -3.2f,
                SkaterRole.RightDefense => 3.2f,
                SkaterRole.Center => 0f,
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported skater role.")
            };
            float goalSideDepth = role switch
            {
                SkaterRole.Center => 0.9f,
                SkaterRole.LeftWing or SkaterRole.RightWing => 1.5f,
                SkaterRole.LeftDefense or SkaterRole.RightDefense => 5f,
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported skater role.")
            };
            return new Vector3(lateral * attack, 1f, -goalSideDepth * attack);
        }

        public static Vector3 Support(TeamId team, int slot, int count, Vector3 carrierPosition)
        {
            if (count != 5) throw new ArgumentOutOfRangeException(nameof(count), count, "Role-aware formation requires exactly five skaters.");
            return Support(team, RoleForSlot(slot), carrierPosition);
        }

        public static Vector3 Support(TeamId team, SkaterRole role, Vector3 carrierPosition)
        {
            float attack = team == TeamId.Blue ? 1f : -1f;
            float carrierProgress = carrierPosition.z * attack;
            float lateralOffset = role switch
            {
                SkaterRole.LeftWing => -4.2f,
                SkaterRole.RightWing => 4.2f,
                SkaterRole.LeftDefense => -3.2f,
                SkaterRole.RightDefense => 3.2f,
                SkaterRole.Center => 0f,
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported skater role.")
            };
            float targetProgress = role switch
            {
                SkaterRole.Center => carrierProgress - 1.5f,
                SkaterRole.LeftWing or SkaterRole.RightWing => carrierProgress + 1.5f,
                SkaterRole.LeftDefense or SkaterRole.RightDefense => carrierProgress - 6f,
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported skater role.")
            };
            return new Vector3(
                Mathf.Clamp(carrierPosition.x + lateralOffset * attack, -10.5f, 10.5f),
                1f,
                Mathf.Clamp(targetProgress, -17.5f, 17.5f) * attack);
        }

        public static Vector3 Defend(TeamId team, int slot, int count, Vector3 threatPosition)
        {
            if (count != 5) throw new ArgumentOutOfRangeException(nameof(count), count, "Role-aware formation requires exactly five skaters.");
            return Defend(team, RoleForSlot(slot), threatPosition);
        }

        public static Vector3 Defend(TeamId team, SkaterRole role, Vector3 threatPosition)
        {
            float attack = team == TeamId.Blue ? 1f : -1f;
            float threatProgress = threatPosition.z * attack;
            float lateralOffset = role switch
            {
                SkaterRole.LeftWing => -4.2f,
                SkaterRole.RightWing => 4.2f,
                SkaterRole.LeftDefense => -3.2f,
                SkaterRole.RightDefense => 3.2f,
                SkaterRole.Center => 0f,
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported skater role.")
            };
            float targetProgress = role switch
            {
                SkaterRole.LeftDefense or SkaterRole.RightDefense => threatProgress - 4f,
                SkaterRole.Center => threatProgress,
                SkaterRole.LeftWing or SkaterRole.RightWing => threatProgress + 4f,
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported skater role.")
            };
            return new Vector3(
                Mathf.Clamp(threatPosition.x + lateralOffset * attack, -10.5f, 10.5f),
                1f,
                Mathf.Clamp(targetProgress, -17.5f, 17.5f) * attack);
        }
    }
}
