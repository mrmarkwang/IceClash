/*
 * IceClash Phase 1 imperfect skater AI.
 * Runs the required eight-state hockey decision loop and emits the same movement,
 * pass, and charged-shot input contract as a human, with Easy/Normal profiles.
 */

using IceClash.Core;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.AI
{
    [DefaultExecutionOrder(-100)]
    public sealed class HockeyPlayerAI : MonoBehaviour, IPlayerInput
    {
        [SerializeField] private AIDifficulty difficulty = AIDifficulty.Normal;
        [SerializeField] private float normalDecisionInterval = 0.16f;
        [SerializeField] private float easyDecisionInterval = 0.34f;
        [SerializeField] private float shootDistance = 10f;
        [SerializeField] private float returnDistance = 10f;

        private readonly HockeyAIStateMachine stateMachine = new();
        private PlayerController player;
        private PuckController puck;
        private int formationSlot;
        private int formationCount;
        private Vector3 homePosition;
        private float nextDecisionTime;
        private Vector2 move;
        private bool passPressed;
        private bool shootHeld;
        private bool shootReleased;
        private float releaseShotAt;
        private bool humanControlled;

        public Vector2 Move => move;
        public bool PassPressed => passPressed;
        public bool ShootHeld => shootHeld;
        public bool ShootReleased => shootReleased;
        public bool SwitchPressed => false;
        public HockeyAIState CurrentState => stateMachine.Current;
        public AIDifficulty Difficulty => difficulty;
        public float ActionQuality => difficulty == AIDifficulty.Easy ? 0.58f : 0.86f;

        public void Configure(PlayerController owner, PuckController controlledPuck, int slot, int count, AIDifficulty level)
        {
            player = owner;
            puck = controlledPuck;
            formationSlot = slot;
            formationCount = count;
            difficulty = level;
            homePosition = AIFormationController.Home(owner.Team, slot, count);
            player.Movement.SetSpeedScale(level == AIDifficulty.Easy ? 0.82f : 0.94f);
        }

        public void SetHumanControlled(bool value)
        {
            humanControlled = value;
            if (value) { move = Vector2.zero; passPressed = false; shootHeld = false; shootReleased = false; }
        }

        private void Update()
        {
            passPressed = false;
            shootReleased = false;
            if (humanControlled || player == null || puck == null) return;

            if (shootHeld && Time.time >= releaseShotAt)
            {
                shootHeld = false;
                shootReleased = true;
            }

            if (Time.time < nextDecisionTime) return;
            nextDecisionTime = Time.time + (difficulty == AIDifficulty.Easy ? easyDecisionInterval : normalDecisionInterval)
                + Random.Range(0f, difficulty == AIDifficulty.Easy ? 0.16f : 0.07f);
            Decide();
        }

        private void Decide()
        {
            PlayerController carrier = puck.Carrier;
            bool ownsPuck = carrier == player;
            bool teamHasPuck = carrier != null && carrier.Team == player.Team;
            Vector3 target;

            if (Vector3.Distance(transform.position, homePosition) > returnDistance && carrier != player)
            {
                stateMachine.Transition(HockeyAIState.ReturnToPosition, Time.time);
                target = homePosition;
            }
            else if (ownsPuck)
            {
                float goalZ = player.Team == TeamId.Blue ? 14.4f : -14.4f;
                Vector3 goal = new(0f, transform.position.y, goalZ);
                if (Vector3.Distance(transform.position, goal) <= shootDistance && !shootHeld)
                {
                    stateMachine.Transition(HockeyAIState.Shoot, Time.time);
                    target = goal;
                    shootHeld = true;
                    releaseShotAt = Time.time + Random.Range(0.2f, difficulty == AIDifficulty.Easy ? 0.55f : 0.8f);
                }
                else
                {
                    stateMachine.Transition(HockeyAIState.Attack, Time.time);
                    target = goal + Vector3.right * Random.Range(-2.5f, 2.5f);
                    if (IsPressured() && Random.value < ActionQuality * 0.7f) passPressed = true;
                }
            }
            else if (teamHasPuck)
            {
                stateMachine.Transition(IsLikelyPassReceiver(carrier) ? HockeyAIState.ReceivePass : HockeyAIState.Support, Time.time);
                target = AIFormationController.Support(player.Team, formationSlot, formationCount, carrier.transform.position);
            }
            else if (IsClosestTeammateToPuck())
            {
                stateMachine.Transition(HockeyAIState.ChasePuck, Time.time);
                target = puck.transform.position;
            }
            else if (carrier != null)
            {
                stateMachine.Transition(HockeyAIState.Defend, Time.time);
                target = AIFormationController.Defend(player.Team, formationSlot, formationCount, carrier.transform.position);
            }
            else
            {
                stateMachine.Transition(HockeyAIState.Idle, Time.time);
                target = Vector3.Lerp(homePosition, puck.transform.position, 0.2f);
            }

            Vector3 error = Random.insideUnitSphere * (difficulty == AIDifficulty.Easy ? 0.8f : 0.3f);
            error.y = 0f;
            move = ToCameraInput(target + error - transform.position);
        }

        private bool IsClosestTeammateToPuck()
        {
            float myDistance = Vector3.SqrMagnitude(transform.position - puck.transform.position);
            foreach (PlayerController candidate in FindObjectsByType<PlayerController>())
                if (candidate != player && candidate.Team == player.Team
                    && Vector3.SqrMagnitude(candidate.transform.position - puck.transform.position) < myDistance) return false;
            return true;
        }

        private bool IsPressured()
        {
            foreach (PlayerController candidate in FindObjectsByType<PlayerController>())
                if (candidate.Team != player.Team && Vector3.Distance(transform.position, candidate.transform.position) < 2.8f) return true;
            return false;
        }

        private bool IsLikelyPassReceiver(PlayerController carrier)
        {
            Vector3 lane = (transform.position - carrier.transform.position).normalized;
            return Vector3.Dot(carrier.transform.forward, lane) > 0.45f;
        }

        private static Vector2 ToCameraInput(Vector3 worldDirection)
        {
            Vector3 direction = Vector3.ProjectOnPlane(worldDirection, Vector3.up).normalized;
            Camera view = Camera.main;
            Vector3 forward = view != null ? Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = view != null ? Vector3.ProjectOnPlane(view.transform.right, Vector3.up).normalized : Vector3.right;
            return Vector2.ClampMagnitude(new Vector2(Vector3.Dot(direction, right), Vector3.Dot(direction, forward)), 1f);
        }
    }
}
