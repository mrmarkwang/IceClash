/*
 * IceClash modular skater composition and action routing.
 * Sends Move only to skating, one tap to recommended-target passing, and charged
 * SHOOT signals to assisted shooting while retaining team, conventional skater
 * role, and role-aware faceoff reset state across control changes.
 */

using IceClash.AI;
using IceClash.Core;
using IceClash.Gameplay;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Player
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerMovementController), typeof(StickPuckInteraction))]
    [RequireComponent(typeof(PassReceivingZone), typeof(PassController), typeof(ShootController))]
    public sealed class PlayerController : MonoBehaviour, IPlayerController, IResettableActor
    {
        [SerializeField] private string playerId = "skater";
        [SerializeField] private TeamId team = TeamId.Blue;
        [SerializeField] private SkaterRole role = SkaterRole.Center;

        private IPlayerInput inputSource;
        private Vector3 resetPosition;
        private Quaternion resetRotation;
        private bool gameplayEnabled = true;
        private float actionStateUntil;

        public string PlayerId => playerId;
        public TeamId Team => team;
        public SkaterRole Role => role;
        public PlayerMovementState State { get; private set; }
        public float Stamina => 100f;
        public bool GameplayEnabled => gameplayEnabled;
        public IPlayerInput InputSource => inputSource;
        public PlayerMovementController Movement { get; private set; }
        public StickPuckInteraction Stick { get; private set; }
        public PassReceivingZone PassReception { get; private set; }
        public PassController Pass { get; private set; }
        public ShootController Shoot { get; private set; }
        public PuckController Puck { get; private set; }
        public Vector3 ControlPoint => Stick != null ? Stick.ControlPoint : transform.position + transform.forward;
        public float ControlSpeedLimit => 14f;
        public float InterceptionRadius => Stick != null ? Stick.ClaimRadius : 1.2f;

        private void Awake()
        {
            EnsureComponents();
        }

        private void EnsureComponents()
        {
            Movement = GetComponent<PlayerMovementController>() ?? gameObject.AddComponent<PlayerMovementController>();
            Stick = GetComponent<StickPuckInteraction>() ?? gameObject.AddComponent<StickPuckInteraction>();
            PassReception = GetComponent<PassReceivingZone>() ?? gameObject.AddComponent<PassReceivingZone>();
            Pass = GetComponent<PassController>() ?? gameObject.AddComponent<PassController>();
            Shoot = GetComponent<ShootController>() ?? gameObject.AddComponent<ShootController>();
        }

        public void Configure(string id, TeamId playerTeam, SkaterRole playerRole, IPlayerInput source,
            PuckController controlledPuck, Vector3 spawnPosition)
        {
            EnsureComponents();
            playerId = id;
            team = playerTeam;
            role = playerRole;
            inputSource = source;
            Puck = controlledPuck;
            resetPosition = spawnPosition;
            resetRotation = playerTeam == TeamId.Blue ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            Stick.Configure(this, controlledPuck);
            PassReception.Configure(this, Stick);
            Pass.Configure(this, controlledPuck);
            Shoot.Configure(this, controlledPuck);
        }

        public void SetInputSource(IPlayerInput source) => inputSource = source;
        public void SetGameplayEnabled(bool value)
        {
            EnsureComponents();
            gameplayEnabled = value;
            Movement.SetMovementEnabled(value);
            Stick.SetInteractionEnabled(value);
            if (!value) { Pass.Cancel(); Shoot.ResetCharge(); }
        }

        private void Update()
        {
            if (Movement == null || Stick == null || Pass == null || Shoot == null) EnsureComponents();
            if (!gameplayEnabled || inputSource == null) { Movement.SetInput(Vector2.zero); State = PlayerMovementState.Idle; return; }
            Movement.SetInput(inputSource.Move);
            if (Pass.Tick(inputSource.PassPressed, inputSource is not HockeyPlayerAI, InputQuality))
            { State = PlayerMovementState.Passing; actionStateUntil = Time.time + 0.14f; }
            Shoot.Tick(inputSource.ShootHeld, inputSource.ShootReleased, InputQuality);
            if (inputSource.ShootReleased && Puck != null && !Puck.IsCarriedBy(this))
            { State = PlayerMovementState.Shooting; actionStateUntil = Time.time + 0.18f; }

            if (Time.time < actionStateUntil) return;
            State = Puck != null && Puck.IsCarriedBy(this) ? PlayerMovementState.ControllingPuck
                : Movement.IsMoving ? PlayerMovementState.Skating : PlayerMovementState.Idle;
        }

        public void ResetActor()
        {
            Pass.Cancel();
            Shoot.ResetCharge();
            Movement.ResetMotion(resetPosition, resetRotation);
            State = PlayerMovementState.Idle;
            actionStateUntil = 0f;
        }

        private float InputQuality
        {
            get
            {
                HockeyPlayerAI ai = inputSource as HockeyPlayerAI;
                return ai != null ? ai.ActionQuality : 1f;
            }
        }
    }
}
