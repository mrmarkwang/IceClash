/*
 * IceClash humanoid hockey presentation adapter.
 * Binds after runtime player composition, drives placeholder Animator state from
 * existing movement/action state, and remains safe for controller-less goalies.
 */

using IceClash.Core;
using IceClash.Player;
using UnityEngine;

namespace IceClash.Hockey.Character
{
    public enum HockeyPresentationState { Idle, Skating, Shooting }

    public sealed class HockeyCharacterPresentation : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int ShootId = Animator.StringToHash("Shoot");

        [SerializeField] private Animator animator;
        [SerializeField] private HockeyEquipmentLoadout equipment;
        private PlayerController player;
        private PlayerMovementState previousPlayerState = PlayerMovementState.Idle;
        private HockeyPresentationState previewState;
        private bool previewEnabled;

        public Animator Animator => animator;
        public HockeyEquipmentLoadout Equipment => equipment;
        public PlayerController BoundPlayer => player;
        public bool IsBound => player != null;
        public HockeyPresentationState CurrentPresentationState { get; private set; }

        public void Configure(Animator characterAnimator, HockeyEquipmentLoadout loadout)
        {
            animator = characterAnimator;
            equipment = loadout;
            if (animator != null) animator.applyRootMotion = false;
        }

        public void Bind(PlayerController controller)
        {
            player = controller;
            previewEnabled = false;
            previousPlayerState = controller != null ? controller.State : PlayerMovementState.Idle;
            ApplyState(HockeyPresentationState.Idle, 0f, false);
        }

        public void SetPreviewState(HockeyPresentationState state)
        {
            previewEnabled = true;
            previewState = state;
            ApplyPreview(state == HockeyPresentationState.Shooting);
        }

        public void ClearPreview()
        {
            previewEnabled = false;
            if (player == null) ApplyState(HockeyPresentationState.Idle, 0f, false);
        }

        private void Awake()
        {
            if (animator != null) animator.applyRootMotion = false;
            ApplyState(HockeyPresentationState.Idle, 0f, false);
        }

        private void Update()
        {
            if (previewEnabled)
            {
                ApplyPreview(false);
                return;
            }

            if (player == null)
            {
                ApplyState(HockeyPresentationState.Idle, 0f, false);
                return;
            }

            float speed = player.Movement != null ? Mathf.Clamp01(player.Movement.NormalizedSpeed) : 0f;
            bool shooting = player.State == PlayerMovementState.Shooting
                && previousPlayerState != PlayerMovementState.Shooting;
            HockeyPresentationState state = player.State == PlayerMovementState.Shooting
                ? HockeyPresentationState.Shooting
                : speed > 0.05f ? HockeyPresentationState.Skating : HockeyPresentationState.Idle;
            ApplyState(state, speed, shooting);
            previousPlayerState = player.State;
        }

        private void ApplyPreview(bool triggerShoot)
        {
            float speed = previewState == HockeyPresentationState.Skating ? 1f : 0f;
            ApplyState(previewState, speed, triggerShoot);
        }

        private void ApplyState(HockeyPresentationState state, float speed, bool shoot)
        {
            CurrentPresentationState = state;
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetFloat(SpeedId, speed);
            if (shoot) animator.SetTrigger(ShootId);
        }
    }
}
