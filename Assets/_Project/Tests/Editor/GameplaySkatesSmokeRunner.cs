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
using UnityEditor;
using UnityEngine;

namespace IceClash.Tests.Editor
{
    [InitializeOnLoad]
    public static class GameplaySkatesSmokeRunner
    {
        private const string PendingKey = "IceClash.GameplaySkatesSmokePending";
        private const string EvidenceDirectory = ".docs/evidence/skate-base-v1";
        private static double verifyAfter;
        private static int batchExitCode;

        static GameplaySkatesSmokeRunner()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
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
                PlayerController player = UnityEngine.Object.FindObjectsByType<PlayerController>()
                    .OrderBy(item => item.PlayerId, StringComparer.Ordinal).First();
                HockeyCharacterPresentation presentation = player.GetComponent<HockeyCharacterPresentation>()
                    ?? throw new InvalidOperationException("Gameplay player presentation is missing.");
                HockeyEquipmentLoadout loadout = player.GetComponent<HockeyEquipmentLoadout>()
                    ?? throw new InvalidOperationException("Gameplay player equipment loadout is missing.");
                GameObject skates = loadout.GetEquipped(HockeyEquipmentSlot.Skates)
                    ?? throw new InvalidOperationException("Gameplay player skates are missing.");
                HockeyPairedEquipmentFollower follower = skates.GetComponent<HockeyPairedEquipmentFollower>()
                    ?? throw new InvalidOperationException("Gameplay player skate follower is missing.");
                Camera camera = Camera.main ?? throw new InvalidOperationException("PrototypeArena camera is missing.");

                presentation.SetPreviewState(HockeyPresentationState.Idle);
                presentation.Animator.Play("Idle", 0, 0f);
                presentation.Animator.Update(0f);
                follower.RefreshPose();
                Capture(camera, skates, player.transform, "gameplay-runtime-idle-skates.png");

                presentation.SetPreviewState(HockeyPresentationState.Running);
                presentation.Animator.Play("Running", 0, 0.125f);
                presentation.Animator.Update(0f);
                follower.RefreshPose();
                Capture(camera, skates, player.transform, "gameplay-runtime-running-skates.png");
                Debug.Log("GAMEPLAY_SKATES_EVIDENCE_PASS images=2 states=Idle,Running");
            }
            catch (Exception exception)
            {
                batchExitCode = 1;
                Debug.LogException(exception);
            }
            finally
            {
                SessionState.EraseBool(PendingKey);
                if (Application.isBatchMode)
                {
                    EditorApplication.playModeStateChanged -= ExitBatchAfterPlayMode;
                    EditorApplication.playModeStateChanged += ExitBatchAfterPlayMode;
                }
                EditorApplication.ExitPlaymode();
            }
        }

        private static void Capture(Camera camera, GameObject skates, Transform actor, string fileName)
        {
            Renderer[] renderers = skates.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length != 2) throw new InvalidOperationException("Gameplay skate evidence requires two renderers.");
            Bounds bounds = renderers[0].bounds;
            bounds.Encapsulate(renderers[1].bounds);
            Vector3 target = bounds.center + Vector3.up * 0.24f;
            camera.transform.position = target + actor.forward * 1.45f + actor.right * 0.48f
                + Vector3.up * 0.72f;
            camera.transform.LookAt(target);
            camera.fieldOfView = 34f;
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
            EditorApplication.Exit(batchExitCode);
        }
    }
}
#endif
