/*
 * IceClash Phase 1 imperfect skater AI.
 * Runs the required eight-state hockey decision loop and emits independent movement,
 * tactical tap-pass, charged-shot, and active forechecking signals with Easy/Normal
 * profiles. The closest defender pressures and checks an opposing puck carrier,
 * loose-puck pursuit outranks formation recovery, and attacks align to the goal line.
 */

using IceClash.Core;
using IceClash.Gameplay;
using IceClash.Hockey;
using IceClash.Player;
using IceClash.Puck;
using System.Collections.Generic;
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
        [SerializeField] private float passProgressAdvantage = 2.5f;
        [SerializeField] private float loosePuckPredictionSeconds = 0.3f;

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
        private bool checkPressed;
        private bool forechecking;
        private PlayerController lastOpposingCarrier;
        private DefensiveCheckController defensiveChecks;
        private IReadOnlyList<PlayerController> defensiveTeam;

        public Vector2 Move => move;
        public bool PassPressed => passPressed;
        public bool ShootHeld => shootHeld;
        public bool ShootReleased => shootReleased;
        public bool SwitchPressed => false;
        public bool CheckPressed => checkPressed;
        public HockeyAIState CurrentState => stateMachine.Current;
        internal bool IsForechecking => forechecking;
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
            if (value) { move = Vector2.zero; passPressed = false; shootHeld = false; shootReleased = false; checkPressed = false; }
        }

        public void ConfigureDefense(DefensiveCheckController controller,
            IReadOnlyList<PlayerController> teammates)
        {
            defensiveChecks = controller;
            defensiveTeam = teammates;
        }

        private void Update()
        {
            passPressed = false;
            shootReleased = false;
            checkPressed = false;
            if (humanControlled || player == null || puck == null) return;

            if (shootHeld && Time.time >= releaseShotAt)
            {
                shootHeld = false;
                shootReleased = true;
            }

            TickDecision();
        }

        private void Decide()
        {
            PlayerController carrier = puck.Carrier;
            bool ownsPuck = carrier == player;
            bool teamHasPuck = carrier != null && carrier.Team == player.Team;
            Vector3 target;
            forechecking = false;
            checkPressed = false;

            if (ownsPuck)
            {
                float goalZ = OpponentGoalZ;
                Vector3 goal = new(0f, transform.position.y, goalZ);
                float goalDistance = Vector3.Distance(transform.position, goal);
                if (goalDistance <= shootDistance && !shootHeld)
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
                    passPressed = ShouldPass();
                }
            }
            else if (teamHasPuck)
            {
                stateMachine.Transition(IsLikelyPassReceiver(carrier) ? HockeyAIState.ReceivePass : HockeyAIState.Support, Time.time);
                target = AIFormationController.Support(player.Team, formationSlot, formationCount, carrier.transform.position);
            }
            else if (carrier == null && IsClosestTeammateToPuck())
            {
                stateMachine.Transition(HockeyAIState.ChasePuck, Time.time);
                target = PredictLoosePuckTarget();
            }
            else if (carrier != null && defensiveChecks != null && IsDesignatedForechecker(carrier))
            {
                stateMachine.Transition(HockeyAIState.Defend, Time.time);
                forechecking = true;
                Vector3 carrierVelocity = carrier.Movement != null ? carrier.Movement.Velocity : Vector3.zero;
                target = carrier.transform.position + carrierVelocity * 0.2f;
                checkPressed = defensiveChecks != null
                    && Vector3.Distance(transform.position, carrier.transform.position) <= defensiveChecks.MaximumRange;
            }
            else if (Vector3.Distance(transform.position, homePosition) > returnDistance)
            {
                stateMachine.Transition(HockeyAIState.ReturnToPosition, Time.time);
                target = homePosition;
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

        private float OpponentGoalZ => player.Team == TeamId.Blue
            ? PrototypeRinkGeometry.GoalLineDistance
            : -PrototypeRinkGeometry.GoalLineDistance;

        private Vector3 PredictLoosePuckTarget()
        {
            Vector3 velocity = puck.Body != null
                ? Vector3.ProjectOnPlane(puck.Body.linearVelocity, Vector3.up)
                : Vector3.zero;
            float prediction = Mathf.Clamp(loosePuckPredictionSeconds, 0f, 0.6f);
            return puck.transform.position + Vector3.ClampMagnitude(velocity * prediction, 3f);
        }

        private bool ShouldPass()
        {
            PlayerController teammate = player.Pass != null ? player.Pass.RecommendedTarget : null;
            if (teammate == null || shootHeld) return false;
            if (IsPressured()) return true;

            float attackSign = player.Team == TeamId.Blue ? 1f : -1f;
            float teammateProgress = (teammate.transform.position.z - transform.position.z) * attackSign;
            return teammateProgress >= passProgressAdvantage;
        }

        internal void DecideForValidation() => Decide();
        internal void DecideAndActForValidation()
        {
            checkPressed = false;
            Decide();
            ExecuteDefensiveCheck();
        }
        internal void TickDecisionForValidation() => TickDecision();
        internal void CompleteShotChargeForValidation()
        {
            if (!shootHeld) return;
            shootHeld = false;
            shootReleased = true;
        }

        private bool IsClosestTeammateToPuck()
        {
            float myDistance = Vector3.SqrMagnitude(transform.position - puck.transform.position);
            foreach (PlayerController candidate in FindObjectsByType<PlayerController>())
                if (candidate != player && candidate.Team == player.Team
                    && Vector3.SqrMagnitude(candidate.transform.position - puck.transform.position) < myDistance) return false;
            return true;
        }

        private bool IsDesignatedForechecker(PlayerController carrier)
        {
            if (carrier == null || defensiveTeam == null || defensiveTeam.Count == 0) return false;
            PlayerController selected = null;
            float selectedDistance = float.PositiveInfinity;
            for (int i = 0; i < defensiveTeam.Count; i++)
            {
                PlayerController candidate = defensiveTeam[i];
                if (candidate == null || candidate.Team != player.Team) continue;
                float candidateDistance = Vector3.SqrMagnitude(
                    candidate.transform.position - carrier.transform.position);
                if (selected != null && candidateDistance > selectedDistance) continue;
                if (selected != null && candidateDistance == selectedDistance
                    && string.CompareOrdinal(candidate.PlayerId, selected.PlayerId) >= 0) continue;
                selected = candidate;
                selectedDistance = candidateDistance;
            }
            return selected == player;
        }

        private void TickDecision()
        {
            RefreshForecheckAssignment();
            if (Time.time < nextDecisionTime) return;
            nextDecisionTime = Time.time + (difficulty == AIDifficulty.Easy ? easyDecisionInterval : normalDecisionInterval)
                + Random.Range(0f, difficulty == AIDifficulty.Easy ? 0.16f : 0.07f);
            Decide();
            ExecuteDefensiveCheck();
        }

        private void RefreshForecheckAssignment()
        {
            PlayerController carrier = puck.Carrier;
            PlayerController opposingCarrier = defensiveChecks != null && carrier != null
                && carrier.Team != player.Team ? carrier : null;
            bool shouldForecheck = opposingCarrier != null && IsDesignatedForechecker(opposingCarrier);
            if (opposingCarrier != lastOpposingCarrier || shouldForecheck != forechecking)
                nextDecisionTime = 0f;
            lastOpposingCarrier = opposingCarrier;
        }

        private void ExecuteDefensiveCheck()
        {
            if (checkPressed && defensiveChecks != null) defensiveChecks.TryCheck(player);
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
