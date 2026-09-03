/*
 * IceClash integrated-skates production character setup.
 * Creates the stable two-state Humanoid controller from procedural presentation
 * clips and calibrates the Meshy visual against the retained 0.68 gameplay actor
 * scale and ice contact plane without applying root motion.
 */

#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace IceClash.CharacterValidation.Editor
{
    public static class MaleBaseIntegratedSkatesGameplayIntegrationSetup
    {
        public const string ModelPath =
            "Assets/Characters/Male/Male_Base_v1/Models/Meshy_AI_Hockey_Player_Charact_biped_Character_output.fbx";
        public const string VisualPrefabPath = ModelPath;
        public const string ControllerPath =
            "Assets/Characters/Male/Male_Base_v1/Animations/MaleSkater.controller";
        public const float RuntimeActorScale = 0.68f;
        public const float TargetRuntimeHeight = 1.90f;
        public const float GameplayIceY = 0.20f;
        // CharacterController settling places the prototype skater 0.04 m below
        // the requested y=1.00 formation position before presentation checks.
        public const float GameplaySpawnY = 0.96f;
        public const float CalibratedVisualRootLocalY = 0.160f;
        public const float IntegratedSkateContactLocalY =
            (GameplayIceY - GameplaySpawnY) / RuntimeActorScale;

        private const string IdleClipPath = "Assets/_Project/Art/HockeyPrototype/Idle.anim";
        private const string SkateClipPath = "Assets/_Project/Art/HockeyPrototype/Skate.anim";

        public static void GenerateProductionAssets()
        {
            AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath)
                ?? throw new InvalidOperationException("Production Humanoid Idle clip is missing.");
            AnimationClip skate = AssetDatabase.LoadAssetAtPath<AnimationClip>(SkateClipPath)
                ?? throw new InvalidOperationException("Production Humanoid Skate clip is missing.");
            if (!idle.humanMotion || !skate.humanMotion)
                throw new InvalidOperationException("Production presentation clips must use Humanoid muscle bindings.");

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            controller.parameters = new[]
            {
                FloatParameter("Speed"),
                FloatParameter("ForwardAmount"),
                FloatParameter("TurnAmount"),
                BoolParameter("IsMoving"),
                BoolParameter("IsBackward"),
                BoolParameter("IsBraking"),
                BoolParameter("IsSprinting"),
                FloatParameter("CrossoverDirection")
            };
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in machine.states.ToArray())
                machine.RemoveState(child.state);
            foreach (AnimatorStateTransition transition in machine.anyStateTransitions.ToArray())
                machine.RemoveAnyStateTransition(transition);

            AnimatorState idleState = machine.AddState("Idle");
            AnimatorState runningState = machine.AddState("Running");
            idleState.motion = idle;
            runningState.motion = skate;
            machine.defaultState = idleState;

            AnimatorStateTransition toRunning = idleState.AddTransition(runningState);
            toRunning.hasExitTime = false;
            toRunning.duration = 0.10f;
            toRunning.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            AnimatorStateTransition toIdle = runningState.AddTransition(idleState);
            toIdle.hasExitTime = false;
            toIdle.duration = 0.10f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
            EditorUtility.SetDirty(controller);
        }

        public static float AlignVisualToGameplayIce(GameObject gameplayRoot,
            Transform visualRoot, Animator animator)
        {
            if (gameplayRoot == null || visualRoot == null || animator == null)
                throw new ArgumentNullException("Integrated-skates alignment requires gameplay, visual, and Animator roots.");

            Renderer[] renderers = animator.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("Integrated-skates visual has no renderers to calibrate.");

            animator.transform.localScale = Vector3.one;
            animator.Rebind();
            animator.Update(0f);
            Bounds rawBounds = CalculateBounds(renderers);
            if (rawBounds.size.y <= 0.01f)
                throw new InvalidOperationException("Integrated-skates visual height is invalid.");

            float targetPrefabHeight = TargetRuntimeHeight / RuntimeActorScale;
            float uniformScale = targetPrefabHeight / rawBounds.size.y;
            animator.transform.localScale = Vector3.one * uniformScale;
            animator.Rebind();
            animator.Update(0f);

            Bounds scaledBounds = CalculateBounds(renderers);
            float targetContactWorldY = gameplayRoot.transform.TransformPoint(
                Vector3.up * IntegratedSkateContactLocalY).y;
            visualRoot.position += gameplayRoot.transform.up * (targetContactWorldY - scaledBounds.min.y);
            return uniformScale;
        }

        private static AnimatorControllerParameter FloatParameter(string name) =>
            new() { name = name, type = AnimatorControllerParameterType.Float };

        private static AnimatorControllerParameter BoolParameter(string name) =>
            new() { name = name, type = AnimatorControllerParameterType.Bool };

        private static Bounds CalculateBounds(Renderer[] renderers)
        {
            Bounds bounds = renderers[0].bounds;
            for (int index = 1; index < renderers.Length; index++)
                bounds.Encapsulate(renderers[index].bounds);
            return bounds;
        }
    }
}
#endif
