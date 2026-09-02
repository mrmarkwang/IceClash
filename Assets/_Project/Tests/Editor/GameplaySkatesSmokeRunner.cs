/*
 * Runs the scoped production-skates gameplay smoke check in Play Mode.
 * Verifies all spawned skaters/goalies and captures close idle/running evidence
 * from a real PrototypeArena player without exercising unrelated gameplay tests.
 */

#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using IceClash.Hockey;
using IceClash.Hockey.Character;
using IceClash.Player;
using IceClash.Puck;
using UnityEditor;
using UnityEngine;

namespace IceClash.Tests.Editor
{
    [InitializeOnLoad]
    public static class GameplaySkatesSmokeRunner
    {
        private const string PendingKey = "IceClash.GameplaySkatesSmokePending";
        private const string ExitPendingKey = "IceClash.GameplaySkatesSmokeExitPending";
        private const string ExitCodeKey = "IceClash.GameplaySkatesSmokeExitCode";
        private const string EvidenceDirectory = ".docs/evidence/skate-base-v1";
        private static double verifyAfter;
        private static int batchExitCode;
        private static double batchExitAfter;
        private static PlayerController evidencePlayer;
        private static HockeyCharacterPresentation evidencePresentation;
        private static GameObject evidenceSkates;
        private static HockeyPairedEquipmentFollower evidenceFollower;
        private static Camera evidenceCamera;

        static GameplaySkatesSmokeRunner()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            if (Application.isBatchMode && SessionState.GetBool(ExitPendingKey, false))
            {
                batchExitCode = SessionState.GetInt(ExitCodeKey, 1);
                batchExitAfter = EditorApplication.timeSinceStartup + 1.5d;
                EditorApplication.update -= ExitBatchWhenReady;
                EditorApplication.update += ExitBatchWhenReady;
            }
        }

        public static void Run()
        {
            SessionState.SetBool(PendingKey, true);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            verifyAfter = EditorApplication.timeSinceStartup + 0.8d;
            EditorApplication.update -= Verify;
            EditorApplication.update += Verify;
        }

        private static void Verify()
        {
            if (EditorApplication.timeSinceStartup < verifyAfter) return;
            EditorApplication.update -= Verify;
            batchExitCode = 0;
            try
            {
                PrototypeArenaSmokeCheck.RunProductionSkatesOnly();
                evidencePlayer = UnityEngine.Object.FindObjectsByType<PlayerController>()
                    .OrderBy(item => item.PlayerId, StringComparer.Ordinal).First();
                evidencePresentation = evidencePlayer.GetComponent<HockeyCharacterPresentation>()
                    ?? throw new InvalidOperationException("Gameplay player presentation is missing.");
                HockeyEquipmentLoadout loadout = evidencePlayer.GetComponent<HockeyEquipmentLoadout>()
                    ?? throw new InvalidOperationException("Gameplay player equipment loadout is missing.");
                evidenceSkates = loadout.GetEquipped(HockeyEquipmentSlot.Skates)
                    ?? throw new InvalidOperationException("Gameplay player skates are missing.");
                evidenceFollower = evidenceSkates.GetComponent<HockeyPairedEquipmentFollower>()
                    ?? throw new InvalidOperationException("Gameplay player skate follower is missing.");
                evidenceCamera = Camera.main ?? throw new InvalidOperationException("PrototypeArena camera is missing.");
                foreach (HockeyCharacterPresentation other in
                         UnityEngine.Object.FindObjectsByType<HockeyCharacterPresentation>())
                    if (other != evidencePresentation) other.gameObject.SetActive(false);
                PuckController puck = UnityEngine.Object.FindAnyObjectByType<PuckController>();
                if (puck != null) puck.gameObject.SetActive(false);
                evidencePlayer.transform.SetPositionAndRotation(new Vector3(0f, 1f, 0f), Quaternion.identity);
                evidencePresentation.SetPreviewState(HockeyPresentationState.Idle);
                verifyAfter = EditorApplication.timeSinceStartup + 0.25d;
                EditorApplication.update -= CaptureIdleWhenReady;
                EditorApplication.update += CaptureIdleWhenReady;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Complete(1);
            }
        }

        private static void CaptureIdleWhenReady()
        {
            if (EditorApplication.timeSinceStartup < verifyAfter) return;
            EditorApplication.update -= CaptureIdleWhenReady;
            try
            {
                evidencePresentation.Animator.Play("Idle", 0, 0f);
                evidencePresentation.Animator.Update(0f);
                evidenceFollower.RefreshPose();
                ForceSkinPose(evidencePlayer.gameObject);
                Transform idleLeftFoot = evidencePresentation.Animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform idleRightFoot = evidencePresentation.Animator.GetBoneTransform(HumanBodyBones.RightFoot);
                Debug.Log($"GAMEPLAY_SKATES_IDLE_BODY body={evidencePresentation.Animator.bodyPosition:F4} "
                    + $"root={evidencePresentation.Animator.rootPosition:F4} leftFoot={idleLeftFoot?.position:F4} "
                    + $"leftSkate={evidenceFollower.FirstVisual?.position:F4} rightFoot={idleRightFoot?.position:F4} "
                    + $"rightSkate={evidenceFollower.SecondVisual?.position:F4}");
                Transform leftContact = evidenceFollower.FirstVisual.Find("BladeContact");
                Transform rightContact = evidenceFollower.SecondVisual.Find("BladeContact");
                if (leftContact == null || rightContact == null
                    || Mathf.Abs(leftContact.position.y - 0.2f) > 0.02f
                    || Mathf.Abs(rightContact.position.y - 0.2f) > 0.02f)
                    throw new InvalidOperationException(
                        $"Idle gameplay skate blades are not aligned to the ice plane: "
                        + $"left={leftContact?.position.y:F4} right={rightContact?.position.y:F4}.");
                ValidateWornFit(idleLeftFoot, idleRightFoot);
                Capture(evidenceCamera, evidenceSkates, evidencePlayer.transform,
                    "gameplay-runtime-idle-skates.png");
                evidencePresentation.SetPreviewState(HockeyPresentationState.Running);
                verifyAfter = EditorApplication.timeSinceStartup + 0.55d;
                EditorApplication.update -= CaptureRunningWhenReady;
                EditorApplication.update += CaptureRunningWhenReady;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Complete(1);
            }
        }

        private static void CaptureRunningWhenReady()
        {
            if (EditorApplication.timeSinceStartup < verifyAfter) return;
            EditorApplication.update -= CaptureRunningWhenReady;
            try
            {
                evidencePresentation.Animator.Play("Running", 0, 0.625f);
                evidencePresentation.Animator.Update(0f);
                evidenceFollower.RefreshPose();
                ForceSkinPose(evidencePlayer.gameObject);
                Transform leftFoot = evidencePresentation.Animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform rightFoot = evidencePresentation.Animator.GetBoneTransform(HumanBodyBones.RightFoot);
                float leftFitDistance = Vector3.Distance(evidenceFollower.FirstVisual.position, leftFoot.position);
                float rightFitDistance = Vector3.Distance(evidenceFollower.SecondVisual.position, rightFoot.position);
                if (leftFitDistance > 0.32f || rightFitDistance > 0.32f)
                    throw new InvalidOperationException(
                        $"Running gameplay skates detached: left={leftFitDistance:F4} right={rightFitDistance:F4}.");
                Debug.Log($"GAMEPLAY_SKATES_RUNNING_POSE leftFoot={leftFoot?.position:F4} "
                    + $"leftSkate={evidenceFollower.FirstVisual?.position:F4} rightFoot={rightFoot?.position:F4} "
                    + $"rightSkate={evidenceFollower.SecondVisual?.position:F4} "
                    + $"leftFitDistance={leftFitDistance:F4} rightFitDistance={rightFitDistance:F4} "
                    + $"body={evidencePresentation.Animator.bodyPosition:F4} root={evidencePresentation.Animator.rootPosition:F4}");
                Capture(evidenceCamera, evidenceSkates, evidencePlayer.transform,
                    "gameplay-runtime-running-skates.png");
                Debug.Log("GAMEPLAY_SKATES_EVIDENCE_PASS images=2 states=Idle,Running");
                Complete(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Complete(1);
            }
        }

        private static void ValidateWornFit(Transform leftFoot, Transform rightFoot)
        {
            float leftDistance = Vector3.Distance(evidenceFollower.FirstVisual.position, leftFoot.position);
            float rightDistance = Vector3.Distance(evidenceFollower.SecondVisual.position, rightFoot.position);
            Transform actor = evidencePlayer.transform;
            bool upright = Vector3.Dot(evidenceFollower.FirstVisual.up, actor.up) > 0.99f
                && Vector3.Dot(evidenceFollower.SecondVisual.up, actor.up) > 0.99f;
            bool forward = Vector3.Dot(evidenceFollower.FirstVisual.forward, actor.forward) > 0.99f
                && Vector3.Dot(evidenceFollower.SecondVisual.forward, actor.forward) > 0.99f;
            if (leftDistance > 0.32f || rightDistance > 0.32f || !upright || !forward)
                throw new InvalidOperationException(
                    $"Gameplay skates are not worn/aligned: left={leftDistance:F4} right={rightDistance:F4} "
                    + $"upright={upright} forward={forward}.");
        }

        private static void Complete(int exitCode)
        {
            batchExitCode = exitCode;
            SessionState.EraseBool(PendingKey);
            if (Application.isBatchMode)
            {
                SessionState.SetBool(ExitPendingKey, true);
                SessionState.SetInt(ExitCodeKey, exitCode);
                EditorApplication.playModeStateChanged -= ExitBatchAfterPlayMode;
                EditorApplication.playModeStateChanged += ExitBatchAfterPlayMode;
            }
            EditorApplication.ExitPlaymode();
        }

        private static void ForceSkinPose(GameObject player)
        {
            foreach (SkinnedMeshRenderer renderer in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.updateWhenOffscreen = true;
                renderer.forceMatrixRecalculationPerRender = true;
                renderer.localBounds = renderer.localBounds;
                _ = renderer.bounds;
            }
        }

        private static void Capture(Camera camera, GameObject skates, Transform actor, string fileName)
        {
            Renderer[] renderers = skates.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 2)
                throw new InvalidOperationException("Gameplay skate evidence requires both skate visuals.");
            camera.fieldOfView = 38f;
            Bounds bounds = renderers[0].bounds;
            bounds.Encapsulate(renderers[1].bounds);
            Vector3 target = bounds.center + Vector3.up * 0.18f;
            float halfFov = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float distance = Mathf.Max(3.0f,
                bounds.extents.y / Mathf.Tan(halfFov) + 0.9f,
                bounds.extents.x / (Mathf.Tan(halfFov) * (16f / 9f)) + 0.9f);
            bool runningView = fileName.Contains("running", StringComparison.OrdinalIgnoreCase);
            Vector3 viewDirection = runningView
                ? (-actor.right + actor.forward * 0.15f + Vector3.up * 0.22f).normalized
                : (-actor.forward - actor.right * 0.60f + Vector3.up * 0.22f).normalized;
            camera.transform.position = target + viewDirection * distance;
            camera.transform.LookAt(target);
            RenderTexture texture = new(1280, 720, 24);
            Texture2D image = new(1280, 720, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                image.ReadPixels(new Rect(0f, 0f, 1280, 720), 0, 0);
                image.Apply();
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                    ?? throw new InvalidOperationException("Unity project root is unavailable.");
                string directory = Path.Combine(projectRoot, EvidenceDirectory);
                Directory.CreateDirectory(directory);
                File.WriteAllBytes(Path.Combine(directory, fileName), image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(image);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void ExitBatchAfterPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            EditorApplication.playModeStateChanged -= ExitBatchAfterPlayMode;
            batchExitAfter = EditorApplication.timeSinceStartup + 1.5d;
            EditorApplication.update -= ExitBatchWhenReady;
            EditorApplication.update += ExitBatchWhenReady;
        }

        private static void ExitBatchWhenReady()
        {
            if (EditorApplication.timeSinceStartup < batchExitAfter
                || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= ExitBatchWhenReady;
            SessionState.EraseBool(ExitPendingKey);
            SessionState.EraseInt(ExitCodeKey);
            EditorApplication.Exit(batchExitCode);
        }
    }
}
#endif
