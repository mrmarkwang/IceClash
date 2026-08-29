/*
 * IceClash modular skater composition and action routing.
 * Routes direct movement and action input while owning a validated player build,
 * deterministic stamina/fatigue, explicit dekes, and role-aware center/offside
 * faceoff reset state.
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
    [RequireComponent(typeof(DekeController))]
    public sealed class PlayerController : MonoBehaviour, IPlayerController, IResettableActor
    {
        [SerializeField] private string playerId = "skater";
        [SerializeField] private TeamId team = TeamId.Blue;
        [SerializeField] private SkaterRole role = SkaterRole.Center;
        [SerializeField] private PlayerAttributeBuild attributes = new();

        private IPlayerInput inputSource;
        private Vector3 resetPosition;
        private Quaternion resetRotation;
        private bool gameplayEnabled = true;
        private float actionStateUntil;
        private float stamina = 100f;

        public string PlayerId => playerId;
        public TeamId Team => team;
        public SkaterRole Role => role;
        public PlayerMovementState State { get; private set; }
        public float Stamina => stamina;
        public float PerformanceFactor => Mathf.Lerp(0.68f, 1f, stamina / 100f);
        public PlayerAttributeBuild Attributes => attributes;
        public bool GameplayEnabled => gameplayEnabled;
        public IPlayerInput InputSource => inputSource;
        public PlayerMovementController Movement { get; private set; }
        public StickPuckInteraction Stick { get; private set; }
        public PassReceivingZone PassReception { get; private set; }
        public PassController Pass { get; private set; }
        public ShootController Shoot { get; private set; }
        public DekeController Deke { get; private set; }
        public PuckController Puck { get; private set; }
        public Vector3 ControlPoint => Stick != null ? Stick.ControlPoint : transform.position + transform.forward;
        public float ControlSpeedLimit => 14f;
        public float InterceptionRadius => Stick != null ? Stick.ClaimRadius : 1.2f;
        public Vector2 MoveInput { get; private set; }

        private void Awake()
        {
            EnsureComponents();
        }

        private void EnsureComponents()
        {
            attributes ??= new PlayerAttributeBuild();
            Movement = GetComponent<PlayerMovementController>() ?? gameObject.AddComponent<PlayerMovementController>();
            Stick = GetComponent<StickPuckInteraction>() ?? gameObject.AddComponent<StickPuckInteraction>();
            PassReception = GetComponent<PassReceivingZone>() ?? gameObject.AddComponent<PassReceivingZone>();
            Pass = GetComponent<PassController>() ?? gameObject.AddComponent<PassController>();
            Shoot = GetComponent<ShootController>() ?? gameObject.AddComponent<ShootController>();
            Deke = GetComponent<DekeController>() ?? gameObject.AddComponent<DekeController>();
        }

        public void Configure(string id, TeamId playerTeam, SkaterRole playerRole, IPlayerInput source,
            PuckController controlledPuck, Vector3 spawnPosition, PlayerAttributeBuild playerAttributes = null)
        {
            EnsureComponents();
            playerId = id;
            team = playerTeam;
            role = playerRole;
            inputSource = source;
            Puck = controlledPuck;
            ApplyBuild(playerAttributes != null && playerAttributes.IsValid ? playerAttributes : new PlayerAttributeBuild());
            resetPosition = spawnPosition;
            resetRotation = playerTeam == TeamId.Blue ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            Stick.Configure(this, controlledPuck);
            PassReception.Configure(this, Stick);
            Pass.Configure(this, controlledPuck);
            Shoot.Configure(this, controlledPuck);
            Deke.Configure(this, controlledPuck);
        }

        public bool ApplyBuild(PlayerAttributeBuild build)
        {
            if (build == null || !build.IsValid) return false;
            if (attributes != null) attributes.Changed -= OnAttributesChanged;
            attributes = build.Clone();
            attributes.Changed += OnAttributesChanged;
            OnAttributesChanged();
            return true;
        }

        private void OnAttributesChanged()
        {
            if (Movement != null) Movement.ConfigureAttributes(attributes);
        }

        public void SetInputSource(IPlayerInput source) => inputSource = source;
        public void SetGameplayEnabled(bool value)
        {
            EnsureComponents();
            gameplayEnabled = value;
            Movement.SetMovementEnabled(value);
            Stick.SetInteractionEnabled(value);
            if (!value) { Pass.Cancel(); Shoot.ResetCharge(); Deke.ResetAction(); }
        }

        private void Update() => TickController(Time.deltaTime);

        internal void TickInputForValidation(IPlayerInput validationInput, float deltaTime)
        {
            IPlayerInput previous = inputSource;
            inputSource = validationInput;
            TickController(Mathf.Max(0f, deltaTime));
            inputSource = previous;
        }

        private void TickController(float deltaTime)
        {
            if (Movement == null || Stick == null || Pass == null || Shoot == null) EnsureComponents();
            if (!gameplayEnabled || inputSource == null)
            { MoveInput = Vector2.zero; Movement.SetInput(Vector2.zero); State = PlayerMovementState.Idle; return; }
            MoveInput = Vector2.ClampMagnitude(inputSource.Move, 1f);
            Movement.SetInput(MoveInput);
            TickStamina(MoveInput.magnitude, deltaTime);
            Movement.SetPerformanceScale(PerformanceFactor);
            Deke.Tick(inputSource.DekePressed);
            if (Pass.Tick(inputSource.PassPressed, inputSource is not HockeyPlayerAI))
            { State = PlayerMovementState.Passing; actionStateUntil = Time.time + 0.14f; }
            Shoot.Tick(inputSource.ShootHeld, inputSource.ShootReleased);
            if (inputSource.ShootReleased && Puck != null && !Puck.IsCarriedBy(this))
            { State = PlayerMovementState.Shooting; actionStateUntil = Time.time + 0.18f; }

            if (Time.time < actionStateUntil) return;
            State = Puck != null && Puck.IsCarriedBy(this) ? PlayerMovementState.ControllingPuck
                : Movement.IsMoving ? PlayerMovementState.Skating : PlayerMovementState.Idle;
        }

        public void ResetActor()
        {
            ResetAtPosition(resetPosition);
        }

        public void ResetAtFaceoff(Vector3 faceoffPosition)
        {
            Vector3 translatedPosition = resetPosition + new Vector3(faceoffPosition.x, 0f, faceoffPosition.z);
            ResetAtPosition(translatedPosition);
        }

        private void ResetAtPosition(Vector3 position)
        {
            Pass.Cancel();
            Shoot.ResetCharge();
            Deke.ResetAction();
            Movement.ResetMotion(position, resetRotation);
            MoveInput = Vector2.zero;
            stamina = 100f;
            Movement.SetPerformanceScale(1f);
            State = PlayerMovementState.Idle;
            actionStateUntil = 0f;
        }

        internal void TickStaminaForValidation(float inputMagnitude, float deltaTime) => TickStamina(inputMagnitude, deltaTime);
        internal void SetStaminaForValidation(float value) => stamina = Mathf.Clamp(value, 0f, 100f);

        private void TickStamina(float inputMagnitude, float deltaTime)
        {
            float normalizedStamina = attributes.Normalized(PlayerAttribute.Stamina);
            if (inputMagnitude >= 0.8f)
                stamina -= EvaluateStaminaDrainRate(normalizedStamina) * Mathf.Max(0f, deltaTime);
            else if (inputMagnitude <= 0.25f)
                stamina += EvaluateStaminaRecoveryRate(normalizedStamina) * Mathf.Max(0f, deltaTime);
            stamina = Mathf.Clamp(stamina, 0f, 100f);
        }

        internal static float EvaluateStaminaDrainRate(float normalizedStamina) => Mathf.Lerp(10f, 4f, Mathf.Clamp01(normalizedStamina));
        internal static float EvaluateStaminaRecoveryRate(float normalizedStamina) => Mathf.Lerp(9f, 13f, Mathf.Clamp01(normalizedStamina));
    }
}
