/*
 * IceClash clean-humanoid hockey presentation adapter.
 * Drives extensible locomotion parameters from the existing gameplay velocity
 * and input while keeping root motion and all transform authority disabled.
 * Controller-less goalies and editor previews remain safely idle. Team color
 * is applied only to captured main-character renderers, never wearable gear.
 */

using System;
using System.Collections.Generic;
using IceClash.Core;
using IceClash.Player;
using UnityEngine;

namespace IceClash.Hockey.Character
{
    public enum HockeyPresentationState { Idle, Running }

    public sealed class HockeyCharacterPresentation : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int ForwardAmountId = Animator.StringToHash("ForwardAmount");
        private static readonly int TurnAmountId = Animator.StringToHash("TurnAmount");
        private static readonly int IsMovingId = Animator.StringToHash("IsMoving");
        private static readonly int IsBackwardId = Animator.StringToHash("IsBackward");
        private static readonly int IsBrakingId = Animator.StringToHash("IsBraking");
        private static readonly int IsSprintingId = Animator.StringToHash("IsSprinting");
        private static readonly int CrossoverDirectionId = Animator.StringToHash("CrossoverDirection");
        private const float MovingThreshold = 0.05f;
        private const float InputThreshold = 0.1f;
        private const float ReversalDotThreshold = -0.15f;

        [SerializeField] private Animator animator;
        [SerializeField] private HockeyEquipmentLoadout equipment;
        [SerializeField] private Renderer[] characterRenderers = Array.Empty<Renderer>();
        private PlayerController player;
        private HockeyPresentationState previewState;
        private bool previewEnabled;

        public Animator Animator => animator;
        public HockeyEquipmentLoadout Equipment => equipment;
        public IReadOnlyList<Renderer> CharacterRenderers => characterRenderers;
        public PlayerController BoundPlayer => player;
        public bool IsBound => player != null;
        public HockeyPresentationState CurrentPresentationState { get; private set; }

        public void Configure(Animator characterAnimator, HockeyEquipmentLoadout loadout,
            Renderer[] mainCharacterRenderers)
        {
            animator = characterAnimator;
            equipment = loadout;
            characterRenderers = mainCharacterRenderers ?? Array.Empty<Renderer>();
            if (animator != null) animator.applyRootMotion = false;
        }

        public void SetTeamMaterial(Material material)
        {
            if (material == null || characterRenderers == null) return;
            for (int i = 0; i < characterRenderers.Length; i++)
            {
                Renderer renderer = characterRenderers[i];
                if (renderer == null) continue;
                Material[] materials = renderer.sharedMaterials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                    materials[materialIndex] = material;
                renderer.sharedMaterials = materials;
            }
        }

        public void Bind(PlayerController controller)
        {
            player = controller;
            previewEnabled = false;
            ApplyIdle();
        }

        public void SetPreviewState(HockeyPresentationState state)
        {
            previewEnabled = true;
            previewState = state;
            ApplyPreview();
        }

        public void ClearPreview()
        {
            previewEnabled = false;
            if (player == null) ApplyIdle();
        }

        private void Awake()
        {
            if (animator != null) animator.applyRootMotion = false;
            ApplyIdle();
        }

        private void Update()
        {
            if (previewEnabled)
            {
                ApplyPreview();
                return;
            }

            if (player == null)
            {
                ApplyIdle();
                return;
            }

            Vector3 velocity = player.Movement != null
                ? Vector3.ProjectOnPlane(player.Movement.Velocity, Vector3.up) : Vector3.zero;
            float speed = velocity.magnitude;
            bool isMoving = speed > MovingThreshold;
            Vector3 velocityDirection = isMoving ? velocity / speed : Vector3.zero;
            float forwardAmount = isMoving
                ? Mathf.Clamp(Vector3.Dot(transform.forward, velocityDirection), -1f, 1f) : 0f;
            float turnAmount = isMoving
                ? Mathf.Clamp(Vector3.SignedAngle(transform.forward, velocityDirection, Vector3.up) / 180f, -1f, 1f)
                : 0f;
            Vector2 moveInput = player.MoveInput;
            Vector3 desiredDirection = CameraRelativeDirection(moveInput);
            bool opposingInput = desiredDirection.sqrMagnitude > 0.0001f
                && Vector3.Dot(desiredDirection.normalized, velocityDirection) < ReversalDotThreshold;
            bool isBraking = isMoving && (moveInput.magnitude <= InputThreshold || opposingInput);
            ApplyState(isMoving ? HockeyPresentationState.Running : HockeyPresentationState.Idle,
                speed, forwardAmount, turnAmount, isMoving, isMoving && forwardAmount < -0.1f, isBraking);
        }

        internal void TickForValidation() => Update();

        private void ApplyPreview()
        {
            bool running = previewState == HockeyPresentationState.Running;
            ApplyState(previewState, running ? 1f : 0f, running ? 1f : 0f,
                0f, running, false, false);
        }

        private void ApplyIdle() => ApplyState(HockeyPresentationState.Idle, 0f, 0f, 0f,
            false, false, false);

        private void ApplyState(HockeyPresentationState state, float speed, float forwardAmount,
            float turnAmount, bool isMoving, bool isBackward, bool isBraking)
        {
            CurrentPresentationState = state;
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.applyRootMotion = false;
            animator.SetFloat(SpeedId, speed);
            animator.SetFloat(ForwardAmountId, forwardAmount);
            animator.SetFloat(TurnAmountId, turnAmount);
            animator.SetBool(IsMovingId, isMoving);
            animator.SetBool(IsBackwardId, isBackward);
            animator.SetBool(IsBrakingId, isBraking);
            animator.SetBool(IsSprintingId, false);
            animator.SetFloat(CrossoverDirectionId, turnAmount);
        }

        private static Vector3 CameraRelativeDirection(Vector2 input)
        {
            Camera view = Camera.main;
            Vector3 forward = view != null
                ? Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized : Vector3.forward;
            Vector3 right = view != null
                ? Vector3.ProjectOnPlane(view.transform.right, Vector3.up).normalized : Vector3.right;
            return Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);
        }
    }
}
