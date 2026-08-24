/*
 * IceClash Phase 3 local 2v2 roster setup.
 * Instantiates a reusable skater prefab, assigns stable identity/team/input sources,
 * and maintains transport-agnostic player/team/match snapshots for the local simulation.
 */

using System;
using System.Collections.Generic;
using IceClash.AI;
using IceClash.Core;
using IceClash.Input;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Match
{
    public sealed class LocalMatchSetup : MonoBehaviour
    {
        [SerializeField] private MatchData matchData = new();
        private readonly List<PlayerController> players = new();
        private PuckController puck;

        public MatchData Data => matchData;
        public IReadOnlyList<PlayerController> Players => players;

        public PlayerController BuildRoster(GameObject skaterPrefab, PuckController controlledPuck, Material blueMaterial, Material redMaterial)
        {
            if (skaterPrefab == null) throw new ArgumentNullException(nameof(skaterPrefab));
            puck = controlledPuck;
            players.Clear();

            PlayerController local = Spawn(skaterPrefab, "blue-local", TeamId.Blue, new Vector3(-2.5f, 1f, -7f), blueMaterial, true);
            Spawn(skaterPrefab, "blue-ai", TeamId.Blue, new Vector3(2.5f, 1f, -5f), blueMaterial, false);
            Spawn(skaterPrefab, "red-ai-left", TeamId.Red, new Vector3(-2.5f, 1f, 5f), redMaterial, false);
            Spawn(skaterPrefab, "red-ai-right", TeamId.Red, new Vector3(2.5f, 1f, 7f), redMaterial, false);
            matchData.State = MatchStateSnapshot.Playing;
            matchData.Capture(players, puck);
            return local;
        }

        private PlayerController Spawn(GameObject prefab, string playerId, TeamId team, Vector3 position, Material material, bool isLocal)
        {
            GameObject skater = Instantiate(prefab, position, team == TeamId.Blue ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f), transform);
            skater.name = isLocal ? "Blue Skater (Local)" : team == TeamId.Blue ? "Blue Skater (AI)" : $"Red Skater (AI {players.Count})";
            Renderer renderer = skater.GetComponentInChildren<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;

            IPlayerInput inputSource = isLocal ? skater.AddComponent<LocalPlayerInput>() : skater.AddComponent<AiPlayerInput>();
            PlayerController controller = skater.AddComponent<PlayerController>();
            controller.Configure(playerId, team, inputSource);
            if (inputSource is AiPlayerInput aiInput) aiInput.Configure(controller, puck, position);
            players.Add(controller);
            return controller;
        }

        private void LateUpdate() { matchData.Capture(players, puck); }
    }
}
