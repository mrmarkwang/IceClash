/*
 * IceClash Phase 3 AI command adapter.
 * Chooses a small observable hockey behavior state and exposes movement/actions only
 * through IPlayerInput, so AI never bypasses PlayerController gameplay rules. AI input
 * executes before PlayerController so action pulses are consumed deterministically;
 * carriers shoot from attacking range or after a bounded carry.
 */

using IceClash.Core;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.AI
{
    public enum AiBehaviorState { Defend, ChasePuck, Support, Attack, Shoot, Recover }

    [DefaultExecutionOrder(-100)]
    public sealed class AiPlayerInput : MonoBehaviour, IPlayerInput
    {
        [Header("Decision distances")]
        [SerializeField, Min(0f)] private float shootDistance = 14f;
        [SerializeField, Min(0f)] private float maximumCarryBeforeShot = 2.25f;
        [SerializeField] private float supportDistance = 4.5f;
        [SerializeField] private float checkDistance = 1.7f;
        [SerializeField] private float recoverOutsideRadius = 17f;
        [SerializeField] private float decisionInterval = 0.12f;

        private PlayerController controlledPlayer;
        private PuckController puck;
        private Vector2 move;
        private bool shootPressed;
        private bool passPressed;
        private bool checkPressed;
        private float nextDecisionTime;
        private Vector3 homePosition;
        private bool ownedPuckLastFrame;
        private float possessionStartedAt;

        public Vector2 Move => move;
        public bool SprintHeld => BehaviorState == AiBehaviorState.ChasePuck || BehaviorState == AiBehaviorState.Attack;
        public bool ShootPressed => shootPressed;
        public bool PassPressed => passPressed;
        public bool CheckPressed => checkPressed;
        public AiBehaviorState BehaviorState { get; private set; } = AiBehaviorState.Defend;
        public string TeamRole { get; private set; } = "Defender";

        public void Configure(PlayerController player, PuckController controlledPuck, Vector3 resetPosition)
        {
            controlledPlayer = player;
            puck = controlledPuck;
            homePosition = resetPosition;
        }

        private void Update()
        {
            shootPressed = false;
            passPressed = false;
            checkPressed = false;
            if (controlledPlayer == null || puck == null) return;
            TrackPossession();
            if (Time.time < nextDecisionTime) return;
            nextDecisionTime = Time.time + decisionInterval;
            Decide();
        }

        private void TrackPossession()
        {
            bool ownsPuck = puck.CarrierPlayerId == controlledPlayer.PlayerId;
            if (ownsPuck && !ownedPuckLastFrame) possessionStartedAt = Time.time;
            if (!ownsPuck) possessionStartedAt = 0f;
            ownedPuckLastFrame = ownsPuck;
        }

        private void Decide()
        {
            PlayerController[] players = FindObjectsByType<PlayerController>();
            PlayerController carrier = FindCarrier(players);
            bool ownsPuck = carrier == controlledPlayer;
            bool teamHasPuck = carrier != null && carrier.Team == controlledPlayer.Team;
            bool isPuckSidePlayer = IsClosestTeammateToPuck(players);
            Vector3 target;

            if (controlledPlayer.State == PlayerMovementState.KnockedDown || Mathf.Abs(transform.position.x) > recoverOutsideRadius || Mathf.Abs(transform.position.z) > recoverOutsideRadius)
            {
                BehaviorState = AiBehaviorState.Recover;
                TeamRole = "Recovering";
                target = homePosition;
            }
            else if (ownsPuck)
            {
                float goalDistance = Vector3.Distance(transform.position, OpposingGoalPosition);
                bool carriedTooLong = Time.time - possessionStartedAt >= maximumCarryBeforeShot;
                if (goalDistance <= shootDistance || carriedTooLong)
                {
                    BehaviorState = AiBehaviorState.Shoot;
                    TeamRole = "Attacker";
                    target = OpposingGoalPosition;
                    shootPressed = true;
                }
                else
                {
                    BehaviorState = AiBehaviorState.Attack;
                    TeamRole = "Attacker";
                    target = OpposingGoalPosition;
                    passPressed = ShouldPass(players);
                }
            }
            else if (!teamHasPuck && isPuckSidePlayer)
            {
                BehaviorState = AiBehaviorState.ChasePuck;
                TeamRole = "PuckSide";
                target = puck.transform.position;
                checkPressed = carrier != null && carrier.Team != controlledPlayer.Team
                    && Vector3.Distance(transform.position, carrier.transform.position) <= checkDistance;
            }
            else if (teamHasPuck)
            {
                BehaviorState = AiBehaviorState.Support;
                TeamRole = "Support";
                target = SupportPosition(carrier);
            }
            else
            {
                BehaviorState = AiBehaviorState.Defend;
                TeamRole = "Defender";
                target = DefendingPosition(carrier);
            }

            move = ToCameraRelativeInput(target - transform.position);
        }

        private PlayerController FindCarrier(PlayerController[] players)
        {
            if (string.IsNullOrEmpty(puck.CarrierPlayerId)) return null;
            for (int index = 0; index < players.Length; index++)
            {
                if (players[index].PlayerId == puck.CarrierPlayerId) return players[index];
            }
            return null;
        }

        private bool IsClosestTeammateToPuck(PlayerController[] players)
        {
            PlayerController closest = null;
            float closestDistance = float.MaxValue;
            for (int index = 0; index < players.Length; index++)
            {
                PlayerController candidate = players[index];
                if (candidate.Team != controlledPlayer.Team || candidate.State == PlayerMovementState.KnockedDown) continue;
                float distance = Vector3.SqrMagnitude(candidate.transform.position - puck.transform.position);
                if (distance < closestDistance)
                {
                    closest = candidate;
                    closestDistance = distance;
                }
            }
            return closest == controlledPlayer;
        }

        private bool ShouldPass(PlayerController[] players)
        {
            for (int index = 0; index < players.Length; index++)
            {
                PlayerController opponent = players[index];
                if (opponent.Team != controlledPlayer.Team && Vector3.Distance(transform.position, opponent.transform.position) < supportDistance) return true;
            }
            return false;
        }

        private Vector3 SupportPosition(PlayerController carrier)
        {
            Vector3 attackDirection = controlledPlayer.Team == TeamId.Blue ? Vector3.forward : Vector3.back;
            float side = transform.position.x >= 0f ? 1f : -1f;
            return carrier.transform.position - attackDirection * supportDistance + Vector3.right * side * 3f;
        }

        private Vector3 DefendingPosition(PlayerController carrier)
        {
            Vector3 ownGoal = controlledPlayer.Team == TeamId.Blue ? new Vector3(0f, 0f, -14f) : new Vector3(0f, 0f, 14f);
            return carrier == null ? Vector3.Lerp(homePosition, ownGoal, 0.45f) : Vector3.Lerp(ownGoal, carrier.transform.position, 0.3f);
        }

        private Vector3 OpposingGoalPosition => controlledPlayer.Team == TeamId.Blue ? new Vector3(0f, 0f, 14.9f) : new Vector3(0f, 0f, -14.9f);

        private static Vector2 ToCameraRelativeInput(Vector3 worldDirection)
        {
            Vector3 direction = Vector3.ProjectOnPlane(worldDirection, Vector3.up).normalized;
            Camera viewCamera = Camera.main;
            Vector3 forward = viewCamera != null ? Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = viewCamera != null ? Vector3.ProjectOnPlane(viewCamera.transform.right, Vector3.up).normalized : Vector3.right;
            return Vector2.ClampMagnitude(new Vector2(Vector3.Dot(direction, right), Vector3.Dot(direction, forward)), 1f);
        }
    }
}
