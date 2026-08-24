/*
 * IceClash Phase 1 local match snapshots.
 * Captures count-independent skater identity, score, clock, state, possession, and
 * current human selection without coupling data to UI or AI implementation.
 */

using System;
using System.Collections.Generic;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Core
{
    [Serializable]
    public sealed class PlayerData
    {
        public string PlayerId = string.Empty;
        public TeamId Team;
        public Vector3 Position;
        public Quaternion Rotation = Quaternion.identity;
        public PlayerMovementState State;
        public bool HasPuck;

        public void Capture(PlayerController player, PuckController puck)
        {
            PlayerId = player.PlayerId; Team = player.Team; Position = player.transform.position;
            Rotation = player.transform.rotation; State = player.State; HasPuck = puck != null && puck.IsCarriedBy(player);
        }
    }

    [Serializable]
    public sealed class TeamData
    {
        public TeamId Team;
        public int Score;
        public List<PlayerData> Players = new();
        public TeamData(TeamId team) { Team = team; }
    }

    [Serializable]
    public sealed class MatchData
    {
        public string MatchId = "local-pve";
        public MatchStateSnapshot State = MatchStateSnapshot.Setup;
        public float RemainingSeconds = 180f;
        public string ControlledPlayerId = string.Empty;
        public TeamData BlueTeam = new(TeamId.Blue);
        public TeamData RedTeam = new(TeamId.Red);

        public void Capture(IReadOnlyList<PlayerController> players, PuckController puck)
        {
            CaptureTeam(BlueTeam, players, puck);
            CaptureTeam(RedTeam, players, puck);
        }

        private static void CaptureTeam(TeamData team, IReadOnlyList<PlayerController> players, PuckController puck)
        {
            int count = 0;
            for (int i = 0; i < players.Count; i++) if (players[i].Team == team.Team) count++;
            while (team.Players.Count < count) team.Players.Add(new PlayerData());
            while (team.Players.Count > count) team.Players.RemoveAt(team.Players.Count - 1);
            int dataIndex = 0;
            for (int i = 0; i < players.Count; i++) if (players[i].Team == team.Team) team.Players[dataIndex++].Capture(players[i], puck);
        }
    }
}
