/*
 * IceClash prototype contracts.
 * Defines the small, shared gameplay boundary between input sources, player control,
 * and puck ownership. Phase 2 adds carrier/action metadata without coupling a
 * local input source, AI source, or future network source to a concrete controller.
 */

using UnityEngine;

namespace IceClash.Core
{
    public enum TeamId { Blue, Red }

    public enum PlayerMovementState
    {
        Idle,
        Skating,
        Sprinting,
        ControllingPuck,
        Passing,
        Shooting,
        Checking,
        KnockedDown
    }

    public interface IPlayerInput
    {
        Vector2 Move { get; }
        bool SprintHeld { get; }
        bool ShootPressed { get; }
        bool PassPressed { get; }
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
}
