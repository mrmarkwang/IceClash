/*
 * IceClash Phase 1 player controller.
 * Moves a local skater using abstract IPlayerInput commands while exposing team,
 * identity, stamina placeholder, and movement state for later AI/network integration.
 */

using IceClash.Core;
using IceClash.Input;
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

        private CharacterController characterController;
        private IPlayerInput playerInput;
        private Vector3 verticalVelocity;

        public string PlayerId => playerId;
        public TeamId Team => team;
        public PlayerMovementState State { get; private set; } = PlayerMovementState.Idle;
        public float Stamina => stamina;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerInput = GetComponent<LocalPlayerInput>();
        }

        private void Update()
        {
            if (playerInput == null) return;

            Vector2 moveInput = playerInput.Move;
            Vector3 movement = GetCameraRelativeMovement(moveInput);
            bool sprinting = playerInput.SprintHeld && movement.sqrMagnitude > 0.001f;
            float speed = sprinting ? sprintSpeed : skatingSpeed;

            if (movement.sqrMagnitude > 0.001f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(movement, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, turnSpeed * Time.deltaTime);
                State = sprinting ? PlayerMovementState.Sprinting : PlayerMovementState.Skating;
            }
            else
            {
                State = PlayerMovementState.Idle;
            }

            verticalVelocity.y = characterController.isGrounded ? -1f : verticalVelocity.y + Physics.gravity.y * Time.deltaTime;
            characterController.Move((movement * speed + verticalVelocity) * Time.deltaTime);
        }

        private static Vector3 GetCameraRelativeMovement(Vector2 input)
        {
            Camera viewCamera = Camera.main;
            Vector3 forward = viewCamera != null ? Vector3.ProjectOnPlane(viewCamera.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = viewCamera != null ? Vector3.ProjectOnPlane(viewCamera.transform.right, Vector3.up).normalized : Vector3.right;
            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        }
    }
}
