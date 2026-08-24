/*
 * IceClash manual player-switch override and controlled-skater router.
 * Scores useful teammates only when SWITCH is tapped, then atomically transfers
 * human input, teammate AI, the marker, camera target, and selection events.
 * Possession-driven policy lives separately in PlayerControlManager.
 */

using System;
using System.Collections.Generic;
using IceClash.AI;
using IceClash.CameraSystem;
using IceClash.Core;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Gameplay
{
    public sealed class PlayerSwitchController : MonoBehaviour
    {
        [SerializeField] private float switchCooldown = 0.28f;
        private readonly List<PlayerController> team = new();
        private IPlayerInput humanInput;
        private PuckController puck;
        private Transform marker;
        private HockeyCameraController cameraController;
        private float nextSwitchTime;

        public event Action<PlayerController> ControlledPlayerChanged;
        public PlayerController ControlledPlayer { get; private set; }

        public void Configure(IReadOnlyList<PlayerController> humanTeam, IPlayerInput input, PuckController controlledPuck, Transform controlledMarker)
        {
            team.Clear();
            for (int i = 0; i < humanTeam.Count; i++) team.Add(humanTeam[i]);
            humanInput = input;
            puck = controlledPuck;
            marker = controlledMarker;
            if (team.Count > 0) SetControlled(team[0]);
        }

        public void SetCamera(HockeyCameraController value)
        {
            cameraController = value;
            if (ControlledPlayer != null) cameraController.SetTarget(ControlledPlayer.transform);
        }

        private void Update()
        {
            if (ControlledPlayer == null || humanInput == null) return;
            if (marker != null) marker.position = ControlledPlayer.transform.position + Vector3.up * 1.45f;
            if (humanInput.SwitchPressed && Time.time >= nextSwitchTime) SwitchToBest();
        }

        public void SwitchToBest()
        {
            if (team.Count < 2) return;
            PlayerController best = null;
            float bestScore = float.NegativeInfinity;
            bool defending = puck.Carrier != null && puck.Carrier.Team != TeamId.Blue;
            foreach (PlayerController candidate in team)
            {
                if (candidate == ControlledPlayer) continue;
                float puckDistance = Vector3.Distance(candidate.transform.position, puck.transform.position);
                float score = -puckDistance * (defending ? 1.3f : 0.8f);
                if (puck.Carrier == candidate) score += 20f;
                if (defending && puck.Carrier != null)
                {
                    score -= Vector3.Distance(candidate.transform.position, puck.Carrier.transform.position) * 0.8f;
                    score += Mathf.Clamp01((candidate.transform.position.z + 14f) / 28f) * 1.5f;
                }
                else score += candidate.transform.position.z * 0.12f;
                if (score > bestScore) { best = candidate; bestScore = score; }
            }
            if (best != null) { SetControlled(best); nextSwitchTime = Time.time + switchCooldown; }
        }

        public bool SetControlled(PlayerController next)
        {
            if (next == null || next == ControlledPlayer) return false;
            if (ControlledPlayer != null)
            {
                HockeyPlayerAI oldAi = ControlledPlayer.GetComponent<HockeyPlayerAI>();
                oldAi.SetHumanControlled(false);
                ControlledPlayer.SetInputSource(oldAi);
            }
            ControlledPlayer = next;
            HockeyPlayerAI ai = next.GetComponent<HockeyPlayerAI>();
            ai.SetHumanControlled(true);
            next.SetInputSource(humanInput);
            if (cameraController != null) cameraController.SetTarget(next.transform);
            GameplayEvents.RaiseControlledPlayerChanged(next.PlayerId);
            ControlledPlayerChanged?.Invoke(next);
            return true;
        }
    }
}
