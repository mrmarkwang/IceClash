/*
 * IceClash shared player controller.
 * Moves every skater from an abstract IPlayerInput source and owns possession, shoot,
 * pass, check, cooldown, and knocked-down transitions. Phase 3 adds runtime identity
 * and input-source configuration so local and AI commands use the same control path.
 */

using IceClash.Core;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour, IPlayerController
    {
        [Header("Identity")]
        [SerializeField] private string playerId = "local-player";
        [SerializeField] private TeamId team = TeamId.Blue;
        [SerializeField] private float stamina = 100f;

        [Header("Movement")]
        [SerializeField] private float skatingSpeed = 7f;
        [SerializeField] private float sprintSpeed = 10f;
        [SerializeField] private float turnSpeed = 13f;

        [Header("Puck Control")]
        [SerializeField] private float stickOffset = 0.85f;
        [SerializeField] private float controlSpeedLimit = 13f;

        [Header("Shot")]
        [SerializeField] private float minimumShotPower = 9f;
        [SerializeField] private float maximumShotPower = 17f;
        [SerializeField, Range(0f, 25f)] private float shotAccuracyDegrees = 5f;
        [SerializeField] private float shotCooldown = 0.4f;
        [SerializeField] private float maximumShotSpeed = 20f;
        [SerializeField] private float shotInputBuffer = 0.15f;

        [Header("Pass")]
        [SerializeField] private float passSpeed = 12f;
        [SerializeField] private float passRange = 12f;
        [SerializeField, Range(0f, 1f)] private float passAssistStrength = 0.8f;
        [SerializeField] private float interceptionRadius = 1.2f;
        [SerializeField] private float passCooldown = 0.25f;

        [Header("Check")]
        [SerializeField] private float checkRange = 1.8f;
        [SerializeField] private float checkForce = 7f;
        [SerializeField] private float checkDuration = 0.3f;
        [SerializeField] private float checkCooldown = 0.75f;
        [SerializeField] private float knockdownDuration = 0.65f;

        private CharacterController characterController;
        private IPlayerInput playerInput;
        private Vector3 verticalVelocity;
        private PuckController puck;
        private float nextShotTime;
        private float nextPassTime;
        private float nextCheckTime;
        private float knockedDownUntil;
        private Vector3 knockdownVelocity;
        private float actionUntil;
        private PlayerMovementState activeActionState;
        private float shotRequestedUntil;

        public string PlayerId => playerId;
        public TeamId Team => team;
        public PlayerMovementState State { get; private set; } = PlayerMovementState.Idle;
        public float Stamina => stamina;
        public IPlayerInput InputSource => playerInput;
        public Vector3 ControlPoint => transform.position + transform.forward * stickOffset + Vector3.up * 0.28f;
        public float ControlSpeedLimit => controlSpeedLimit;
        public float InterceptionRadius => interceptionRadius;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerInput = FindInputSource();
            puck = FindAnyObjectByType<PuckController>();
        }

        public void Configure(string id, TeamId playerTeam, IPlayerInput inputSource)
        {
            playerId = id;
            team = playerTeam;
            playerInput = inputSource;
            if (puck == null) puck = FindAnyObjectByType<PuckController>();
        }

        private void Update()
        {
            if (playerInput == null) playerInput = FindInputSource();
            if (playerInput == null) return;

            if (playerInput.ShootPressed)
            {
                shotRequestedUntil = Time.time + shotInputBuffer;
            }

            if (Time.time < knockedDownUntil)
            {
                State = PlayerMovementState.KnockedDown;
                characterController.Move(knockdownVelocity * Time.deltaTime);
                knockdownVelocity = Vector3.MoveTowards(knockdownVelocity, Vector3.zero, checkForce * Time.deltaTime);
                return;
            }

            if (Time.time < actionUntil)
            {
                State = activeActionState;
                verticalVelocity.y = characterController.isGrounded ? -1f : verticalVelocity.y + Physics.gravity.y * Time.deltaTime;
                characterController.Move(verticalVelocity * Time.deltaTime);
                return;
            }

            TryGainPuckControl();
            if (Time.time <= shotRequestedUntil && TryShoot())
            {
                shotRequestedUntil = 0f;
                return;
            }
            if (playerInput.PassPressed && TryPass()) return;
            if (playerInput.CheckPressed && TryCheck()) return;

            Vector2 moveInput = playerInput.Move;
            Vector3 movement = GetCameraRelativeMovement(moveInput);
            bool sprinting = playerInput.SprintHeld && movement.sqrMagnitude > 0.001f;
            float speed = sprinting ? sprintSpeed : skatingSpeed;

            if (movement.sqrMagnitude > 0.001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(movement, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, turnSpeed * Time.deltaTime);
                State = puck != null && puck.IsCarriedBy(this)
                    ? PlayerMovementState.ControllingPuck
                    : sprinting ? PlayerMovementState.Sprinting : PlayerMovementState.Skating;
            }
            else
            {
                State = puck != null && puck.IsCarriedBy(this) ? PlayerMovementState.ControllingPuck : PlayerMovementState.Idle;
            }

            verticalVelocity.y = characterController.isGrounded ? -1f : verticalVelocity.y + Physics.gravity.y * Time.deltaTime;
            characterController.Move((movement * speed + verticalVelocity) * Time.deltaTime);
        }

        private void TryGainPuckControl()
        {
            if (puck == null) puck = FindAnyObjectByType<PuckController>();
            puck?.TryClaim(this);
        }

        private bool TryShoot()
        {
            if (puck == null || Time.time < nextShotTime || !puck.IsCarriedBy(this)) return false;

            float power = Mathf.Clamp(Mathf.Lerp(minimumShotPower, maximumShotPower, 0.65f), minimumShotPower, maximumShotSpeed);
            float spread = Random.Range(-shotAccuracyDegrees, shotAccuracyDegrees);
            Vector3 direction = Quaternion.Euler(0f, spread, 0f) * OpposingGoalDirection;
            if (!puck.Release(this, direction, power)) return false;

            nextShotTime = Time.time + shotCooldown;
            BeginAction(PlayerMovementState.Shooting, 0.12f);
            return true;
        }

        private bool TryPass()
        {
            if (puck == null || Time.time < nextPassTime || !puck.IsCarriedBy(this)) return false;

            PlayerController target = FindBestPassTarget();
            if (target == null) return false;

            Vector3 targetDirection = Vector3.ProjectOnPlane(target.ControlPoint - puck.transform.position, Vector3.up).normalized;
            Vector3 direction = Vector3.Slerp(transform.forward, targetDirection, passAssistStrength).normalized;
            if (!puck.Release(this, direction, passSpeed)) return false;

            nextPassTime = Time.time + passCooldown;
            BeginAction(PlayerMovementState.Passing, 0.1f);
            return true;
        }

        private PlayerController FindBestPassTarget()
        {
            PlayerController best = null;
            float closestDistance = passRange;
            foreach (PlayerController candidate in FindObjectsByType<PlayerController>())
            {
                if (candidate == this || candidate.Team != team || candidate.State == PlayerMovementState.KnockedDown) continue;
                float distance = Vector3.Distance(transform.position, candidate.transform.position);
                if (distance < closestDistance)
                {
                    best = candidate;
                    closestDistance = distance;
                }
            }
            return best;
        }

        private bool TryCheck()
        {
            if (Time.time < nextCheckTime) return false;

            Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * (checkRange * 0.5f), checkRange * 0.5f);
            foreach (Collider hit in hits)
            {
                PlayerController target = hit.GetComponent<PlayerController>();
                if (target == null || target == this || target.Team == team || Vector3.Dot(transform.forward, (target.transform.position - transform.position).normalized) < 0.2f) continue;
                target.ReceiveCheck(transform.forward * checkForce, knockdownDuration);
                nextCheckTime = Time.time + checkCooldown;
                BeginAction(PlayerMovementState.Checking, checkDuration);
                return true;
            }
            return false;
        }

        private void ReceiveCheck(Vector3 force, float duration)
        {
            knockedDownUntil = Time.time + Mathf.Max(duration, checkDuration);
            knockdownVelocity = force;
            puck?.ForceRelease(this);
        }

        private void BeginAction(PlayerMovementState actionState, float duration)
        {
            activeActionState = actionState;
            actionUntil = Time.time + duration;
            State = actionState;
        }

        private Vector3 OpposingGoalDirection => team == TeamId.Blue ? Vector3.forward : Vector3.back;

        private static Vector3 GetCameraRelativeMovement(Vector2 input)
        {
            Camera viewCamera = Camera.main;
            Vector3 forward = viewCamera != null ? Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = viewCamera != null ? Vector3.ProjectOnPlane(viewCamera.transform.right, Vector3.up).normalized : Vector3.right;
            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        }

        private IPlayerInput FindInputSource()
        {
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IPlayerInput inputSource) return inputSource;
            }
            return null;
        }
    }
}
