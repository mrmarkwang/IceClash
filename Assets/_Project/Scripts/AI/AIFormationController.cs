/*
 * IceClash Phase 1 formation helper.
 * Converts count-independent slot indexes into mirrored home/support/defensive
 * positions aligned to the enlarged mobile rink and its deeper goalie anchors.
 */

using IceClash.Core;
using IceClash.Hockey;
using UnityEngine;

namespace IceClash.AI
{
    public static class AIFormationController
    {
        public static Vector3 Home(TeamId team, int slot, int count)
        {
            float sign = team == TeamId.Blue ? -1f : 1f;
            float width = count <= 1 ? 0f : Mathf.Lerp(-4.8f, 4.8f, slot / (float)(count - 1));
            float depth = slot % 2 == 0 ? 7f : 5f;
            return new Vector3(width, 1f, sign * depth);
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
