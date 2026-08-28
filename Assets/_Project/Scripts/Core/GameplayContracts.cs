/*
 * IceClash Phase 1 local gameplay contracts.
 * Defines independent movement, tap PASS, charged-shot, switch, and contextual
 * defensive-check signals shared by hardware, touch, and AI.
 */

using System;
using UnityEngine;

namespace IceClash.Core
{
    public enum TeamId { Blue, Red }
    public enum PlayerMovementState { Idle, Skating, ControllingPuck, Passing, Shooting }
    public enum HockeyAIState { Idle, Support, Attack, Defend, ChasePuck, ReceivePass, Shoot, ReturnToPosition }
    public enum AIDifficulty { Easy, Normal }
    public enum MatchStateSnapshot { Setup, Faceoff, Playing, GoalPause, Finished }

    public interface IPlayerInput
    {
        Vector2 Move { get; }
        bool PassPressed { get; }
        bool ShootHeld { get; }
        bool ShootReleased { get; }
        bool SwitchPressed { get; }
        bool CheckPressed { get; }
    }

    public interface IPlayerController
    {
        string PlayerId { get; }
        TeamId Team { get; }
        PlayerMovementState State { get; }
    }

    public interface IPuckController
    {
        TeamId? PossessionTeam { get; }
        string LastPlayerTouchId { get; }
        string CarrierPlayerId { get; }
    }

    public interface IResettableActor { void ResetActor(); }

    public static class GameplayEvents
    {
        public static event Action<string> ControlledPlayerChanged;
        public static event Action<int, int, float, MatchStateSnapshot> MatchChanged;
        public static void RaiseControlledPlayerChanged(string playerId) => ControlledPlayerChanged?.Invoke(playerId);
        public static void RaiseMatchChanged(int blue, int red, float time, MatchStateSnapshot state) => MatchChanged?.Invoke(blue, red, time, state);
    }
}
