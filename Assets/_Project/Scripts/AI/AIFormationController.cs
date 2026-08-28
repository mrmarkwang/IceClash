/*
 * IceClash Phase 1 formation helper.
 * Maps the five conventional skater roles to mirrored center-faceoff/home positions,
 * then supplies the existing support and defensive targets for live play.
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
            float attack = team == TeamId.Blue ? 1f : -1f;
            float side = count <= 1 ? 0f : Mathf.Lerp(-1f, 1f, slot / (float)(count - 1));
            return carrierPosition - Vector3.forward * attack * 3.6f + Vector3.right * side * 4.2f;
        }

        public static Vector3 Defend(TeamId team, int slot, int count, Vector3 threatPosition)
        {
            float ownGoalZ = team == TeamId.Blue ? -PrototypeRinkGeometry.GoalieAnchor : PrototypeRinkGeometry.GoalieAnchor;
            Vector3 home = Home(team, slot, count);
            return Vector3.Lerp(new Vector3(home.x, 1f, ownGoalZ), threatPosition, 0.28f);
        }
    }
}
