/*
 * IceClash Phase 1 hockey AI state machine.
 * Stores the eight required readable behavior states and transition timing while
 * leaving decisions and movement commands to HockeyPlayerAI.
 */

using IceClash.Core;

namespace IceClash.AI
{
    public sealed class HockeyAIStateMachine
    {
        public HockeyAIState Current { get; private set; } = HockeyAIState.Idle;
        public float EnteredAt { get; private set; }

        public bool Transition(HockeyAIState next, float now)
        {
            if (Current == next) return false;
            Current = next;
            EnteredAt = now;
            return true;
        }
    }
}
