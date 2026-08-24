/*
 * IceClash local simulation data snapshots.
 * Represents match, team, and player identity/state without coupling the data model
 * to local input, AI decisions, scene objects, or a future network transport.
 */

using System;
using System.Collections.Generic;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Core
{
    public enum MatchStateSnapshot { Setup, Countdown, Playing, GoalPause, Finished }

    [Serializable]
    public sealed class PlayerData
    {
        public string PlayerId = string.Empty;
        public TeamId Team;
        public Vector3 Position;
        public Quaternion Rotation = Quaternion.identity;
        public PlayerMovementState State;
        public float Stamina;
        public bool HasPuck;

        public void Capture(PlayerController player, PuckController puck)
        {
            PlayerId = player.PlayerId;
            Team = player.Team;
            Position = player.transform.position;
            Rotation = player.transform.rotation;
            State = player.State;
            Stamina = player.Stamina;
            HasPuck = puck != null && puck.IsCarriedBy(player);
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
        public string MatchId = "local-practice";
        public MatchStateSnapshot State = MatchStateSnapshot.Setup;
        public float RemainingSeconds = 180f;
        public TeamData BlueTeam = new(TeamId.Blue);
        public TeamData RedTeam = new(TeamId.Red);

        public void Capture(IReadOnlyList<PlayerController> players, PuckController puck)
        {
            CaptureTeam(BlueTeam, players, puck);
            CaptureTeam(RedTeam, players, puck);
        }

        private static void CaptureTeam(TeamData team, IReadOnlyList<PlayerController> players, PuckController puck)
        {
            int playerCount = CountPlayers(team.Team, players);
            while (team.Players.Count < playerCount) team.Players.Add(new PlayerData());
            while (team.Players.Count > playerCount) team.Players.RemoveAt(team.Players.Count - 1);

            int dataIndex = 0;
            for (int index = 0; index < players.Count; index++)
            {
                if (players[index].Team != team.Team) continue;
                team.Players[dataIndex++].Capture(players[index], puck);
            }
        }

        private static int CountPlayers(TeamId team, IReadOnlyList<PlayerController> players)
        {
            int count = 0;
            for (int index = 0; index < players.Count; index++)
            {
                if (players[index].Team == team) count++;
            }
            return count;
        }
    }
}
