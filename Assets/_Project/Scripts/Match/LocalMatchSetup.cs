/*
 * IceClash Phase 1 local PvE roster and systems composition.
 * Builds count-driven five-skater teams with three forwards and two defensemen,
 * mirrored center-faceoff reset positions, two goalies, shared input/HUD systems,
 * per-skater role presets, AI, possession control, defensive checks, delayed
 * offside warnings/stoppages, match flow, and attribute-aware snapshots.
 */

using System;
using System.Collections.Generic;
using IceClash.AI;
using IceClash.Core;
using IceClash.Gameplay;
using IceClash.Hockey;
using IceClash.Input;
using IceClash.Player;
using IceClash.Puck;
using IceClash.UI;
using UnityEngine;

namespace IceClash.Match
{
    public sealed class LocalMatchSetup : MonoBehaviour
    {
        private const int SkatersPerTeam = 5;
        private const float SkaterScale = 0.68f;
        private static readonly Vector3 GoalieScale = new(0.9f, 0.72f, 0.78f);
        [SerializeField] private AIDifficulty opponentDifficulty = AIDifficulty.Normal;
        [SerializeField] private MatchData matchData = new();
        private readonly List<PlayerController> players = new();
        private readonly List<PlayerController> bluePlayers = new();
        private readonly List<PlayerController> redPlayers = new();
        private readonly List<HockeyGoalieAI> goalies = new();
        private PuckController puck;

        public MatchData Data => matchData;
        public IReadOnlyList<PlayerController> Players => players;
        public IReadOnlyList<HockeyGoalieAI> Goalies => goalies;
        public PlayerSwitchController SwitchController { get; private set; }
        public PlayerControlManager ControlManager { get; private set; }
        public DefensiveCheckController DefenseController { get; private set; }
        public MatchController MatchController { get; private set; }
        public OffsideController OffsideController { get; private set; }
        public PlayerInputController HumanInput { get; private set; }

        public PlayerController BuildRoster(GameObject skaterPrefab, PuckController controlledPuck, Material blueMaterial,
            Material redMaterial, GameObject blueOffensiveZoneWarning, GameObject redOffensiveZoneWarning)
        {
            if (skaterPrefab == null) throw new ArgumentNullException(nameof(skaterPrefab));
            puck = controlledPuck;
            players.Clear(); bluePlayers.Clear(); redPlayers.Clear(); goalies.Clear();
            HumanInput = BuildInputAndHud();

            for (int slot = 0; slot < SkatersPerTeam; slot++)
            {
                PlayerController blue = SpawnSkater(skaterPrefab, $"blue-{slot + 1}", TeamId.Blue, slot, blueMaterial, AIDifficulty.Normal);
                bluePlayers.Add(blue);
                PlayerController red = SpawnSkater(skaterPrefab, $"red-{slot + 1}", TeamId.Red, slot, redMaterial, opponentDifficulty);
                redPlayers.Add(red);
            }

            goalies.Add(SpawnGoalie(skaterPrefab, "Blue Goalie", TeamId.Blue, new Vector3(0f, 1f, -PrototypeRinkGeometry.GoalieAnchor), blueMaterial));
            goalies.Add(SpawnGoalie(skaterPrefab, "Red Goalie", TeamId.Red, new Vector3(0f, 1f, PrototypeRinkGeometry.GoalieAnchor), redMaterial));

            Transform marker = BuildControlledMarker();
            SwitchController = gameObject.AddComponent<PlayerSwitchController>();
            SwitchController.Configure(bluePlayers, HumanInput, puck, marker);
            ControlManager = gameObject.AddComponent<PlayerControlManager>();
            ControlManager.Configure(bluePlayers, puck, SwitchController);
            DefensiveCheckTuning defenseTuning = Resources.Load<DefensiveCheckTuning>("DefensiveCheckTuning");
            if (defenseTuning == null) throw new InvalidOperationException(
                "Missing Assets/_Project/Resources/DefensiveCheckTuning.asset.");
            DefenseController = gameObject.AddComponent<DefensiveCheckController>();
            DefenseController.Configure(HumanInput, SwitchController, puck, defenseTuning);
            for (int i = 0; i < redPlayers.Count; i++)
                redPlayers[i].GetComponent<HockeyPlayerAI>().ConfigureDefense(DefenseController, redPlayers);
            MatchController = gameObject.AddComponent<MatchController>();
            MatchController.Configure(players, goalies, puck, new Vector3(0f, PrototypeRinkGeometry.PuckY, 0f));
            OffsideController = gameObject.AddComponent<OffsideController>();
            OffsideController.Configure(players, puck, MatchController,
                blueOffensiveZoneWarning, redOffensiveZoneWarning);
            CaptureData();
            return SwitchController.ControlledPlayer;
        }

        private PlayerController SpawnSkater(GameObject prefab, string id, TeamId team, int slot, Material material, AIDifficulty difficulty)
        {
            SkaterRole role = AIFormationController.RoleForSlot(slot);
            Vector3 position = AIFormationController.Home(team, slot, SkatersPerTeam);
            Quaternion rotation = team == TeamId.Blue ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            GameObject skater = Instantiate(prefab, position, rotation, transform);
            skater.name = $"{team} {RoleDisplayName(role)}";
            skater.transform.localScale = Vector3.one * SkaterScale;
            Renderer renderer = skater.GetComponentInChildren<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            HockeyPlayerAI ai = skater.AddComponent<HockeyPlayerAI>();
            PlayerController controller = skater.AddComponent<PlayerController>();
            PlayerAttributeBuild build = PlayerAttributeBuild.CreatePreset(PlayerAttributeBuild.PresetForRole(role));
            controller.Configure(id, team, role, ai, puck, position, build);
            ai.Configure(controller, puck, slot, SkatersPerTeam, difficulty);
            players.Add(controller);
            return controller;
        }

        private HockeyGoalieAI SpawnGoalie(GameObject prefab, string name, TeamId team, Vector3 position, Material material)
        {
            Quaternion rotation = team == TeamId.Blue ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            GameObject goalie = Instantiate(prefab, position, rotation, transform);
            goalie.name = name;
            goalie.transform.localScale = GoalieScale;
            Renderer renderer = goalie.GetComponentInChildren<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            HockeyGoalieAI ai = goalie.AddComponent<HockeyGoalieAI>();
            ai.Configure(team, puck, position);
            return ai;
        }

        private PlayerInputController BuildInputAndHud()
        {
            MobileControlBindings controls = MobileControlsBuilder.Build(transform);
            LocalPlayerInput hardware = controls.CanvasRoot.AddComponent<LocalPlayerInput>();
            PlayerInputController input = controls.CanvasRoot.AddComponent<PlayerInputController>();
            input.Configure(hardware, controls.Joystick, controls.Pass, controls.Deke, controls.Shoot, puck);
            controls.CanvasRoot.AddComponent<MatchHUD>();
            return input;
        }

        private Transform BuildControlledMarker()
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "YOU Controlled Player Marker";
            marker.transform.localScale = new Vector3(0.38f, 0.035f, 0.38f);
            Renderer renderer = marker.GetComponent<Renderer>();
            renderer.material.color = new Color(1f, 0.9f, 0.1f);
            Destroy(marker.GetComponent<Collider>());
            return marker.transform;
        }

        private void LateUpdate() => CaptureData();

#if UNITY_EDITOR
        internal void CaptureDataForValidation() => CaptureData();
#endif

        private void CaptureData()
        {
            if (puck == null) return;
            matchData.Capture(players, puck);
            if (MatchController != null)
            {
                matchData.State = MatchController.State;
                matchData.RemainingSeconds = MatchController.RemainingSeconds;
                matchData.BlueTeam.Score = MatchController.BlueScore;
                matchData.RedTeam.Score = MatchController.RedScore;
            }
            matchData.ControlledPlayerId = SwitchController != null && SwitchController.ControlledPlayer != null ? SwitchController.ControlledPlayer.PlayerId : string.Empty;
        }

        private static string RoleDisplayName(SkaterRole role)
        {
            return role switch
            {
                SkaterRole.Center => "Center",
                SkaterRole.LeftWing => "Left Wing",
                SkaterRole.RightWing => "Right Wing",
                SkaterRole.LeftDefense => "Left Defense",
                SkaterRole.RightDefense => "Right Defense",
                _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unsupported skater role.")
            };
        }
    }
}
