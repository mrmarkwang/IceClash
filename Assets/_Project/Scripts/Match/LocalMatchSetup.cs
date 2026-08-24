/*
 * IceClash Phase 1 local PvE roster and systems composition.
 * Builds count-driven three-skater teams, two goalies, one shared human/mobile
 * input with recommended-target tap PASS, per-skater AI, possession-based control,
 * manual switching, match flow, HUD, and live snapshots.
 */

using System;
using System.Collections.Generic;
using IceClash.AI;
using IceClash.Core;
using IceClash.Gameplay;
using IceClash.Input;
using IceClash.Player;
using IceClash.Puck;
using IceClash.UI;
using UnityEngine;

namespace IceClash.Match
{
    public sealed class LocalMatchSetup : MonoBehaviour
    {
        private const int SkatersPerTeam = 3;
        [SerializeField] private AIDifficulty opponentDifficulty = AIDifficulty.Normal;
        [SerializeField] private MatchData matchData = new();
        private readonly List<PlayerController> players = new();
        private readonly List<PlayerController> bluePlayers = new();
        private readonly List<HockeyGoalieAI> goalies = new();
        private PuckController puck;

        public MatchData Data => matchData;
        public IReadOnlyList<PlayerController> Players => players;
        public IReadOnlyList<HockeyGoalieAI> Goalies => goalies;
        public PlayerSwitchController SwitchController { get; private set; }
        public PlayerControlManager ControlManager { get; private set; }
        public MatchController MatchController { get; private set; }
        public MobileInputSource HumanInput { get; private set; }

        public PlayerController BuildRoster(GameObject skaterPrefab, PuckController controlledPuck, Material blueMaterial, Material redMaterial)
        {
            if (skaterPrefab == null) throw new ArgumentNullException(nameof(skaterPrefab));
            puck = controlledPuck;
            players.Clear(); bluePlayers.Clear(); goalies.Clear();
            HumanInput = BuildInputAndHud();

            for (int slot = 0; slot < SkatersPerTeam; slot++)
            {
                PlayerController blue = SpawnSkater(skaterPrefab, $"blue-{slot + 1}", TeamId.Blue, slot, blueMaterial, AIDifficulty.Normal);
                bluePlayers.Add(blue);
                SpawnSkater(skaterPrefab, $"red-{slot + 1}", TeamId.Red, slot, redMaterial, opponentDifficulty);
            }

            goalies.Add(SpawnGoalie("Blue Goalie", TeamId.Blue, new Vector3(0f, 1f, -14.25f), blueMaterial));
            goalies.Add(SpawnGoalie("Red Goalie", TeamId.Red, new Vector3(0f, 1f, 14.25f), redMaterial));

            Transform marker = BuildControlledMarker();
            SwitchController = gameObject.AddComponent<PlayerSwitchController>();
            SwitchController.Configure(bluePlayers, HumanInput, puck, marker);
            ControlManager = gameObject.AddComponent<PlayerControlManager>();
            ControlManager.Configure(bluePlayers, puck, SwitchController);
            MatchController = gameObject.AddComponent<MatchController>();
            MatchController.Configure(players, goalies, puck, new Vector3(0f, 0.55f, 0f));
            CaptureData();
            return SwitchController.ControlledPlayer;
        }

        private PlayerController SpawnSkater(GameObject prefab, string id, TeamId team, int slot, Material material, AIDifficulty difficulty)
        {
            Vector3 position = AIFormationController.Home(team, slot, SkatersPerTeam);
            Quaternion rotation = team == TeamId.Blue ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
            GameObject skater = Instantiate(prefab, position, rotation, transform);
            skater.name = $"{team} Skater {slot + 1}";
            Renderer renderer = skater.GetComponentInChildren<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            HockeyPlayerAI ai = skater.AddComponent<HockeyPlayerAI>();
            PlayerController controller = skater.AddComponent<PlayerController>();
            controller.Configure(id, team, ai, puck, position);
            ai.Configure(controller, puck, slot, SkatersPerTeam, difficulty);
            players.Add(controller);
            return controller;
        }

        private HockeyGoalieAI SpawnGoalie(string name, TeamId team, Vector3 position, Material material)
        {
            GameObject goalie = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            goalie.name = name;
            goalie.transform.SetParent(transform);
            goalie.transform.position = position;
            goalie.transform.localScale = new Vector3(1.15f, 1f, 0.7f);
            goalie.GetComponent<Renderer>().sharedMaterial = material;
            HockeyGoalieAI ai = goalie.AddComponent<HockeyGoalieAI>();
            ai.Configure(team, puck, position);
            return ai;
        }

        private MobileInputSource BuildInputAndHud()
        {
            GameObject controls = new("Mobile Controls and HUD");
            controls.transform.SetParent(transform);
            LocalPlayerInput hardware = controls.AddComponent<LocalPlayerInput>();
            MobileJoystick joystick = controls.AddComponent<MobileJoystick>();
            ActionButton pass = controls.AddComponent<ActionButton>();
            pass.Configure("PASS", new Rect(0.68f, 0.08f, 0.12f, 0.17f));
            ActionButton shoot = controls.AddComponent<ActionButton>();
            shoot.Configure("SHOOT", new Rect(0.84f, 0.12f, 0.13f, 0.2f));
            ActionButton playerSwitch = controls.AddComponent<ActionButton>();
            playerSwitch.Configure("SWITCH", new Rect(0.68f, 0.34f, 0.14f, 0.16f));
            MobileInputSource composite = controls.AddComponent<MobileInputSource>();
            composite.Configure(hardware, joystick, pass, shoot, playerSwitch);
            controls.AddComponent<MatchHUD>();
            return composite;
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
    }
}
