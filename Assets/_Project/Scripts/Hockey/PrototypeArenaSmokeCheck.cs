/*
 * IceClash headless arena smoke check through Phase 3.
 * Verifies the generated rink, independent puck, camera, reusable local 2v2 roster,
 * identity snapshots, and shared local/AI player-control contract wiring.
 */

using IceClash.CameraSystem;
using IceClash.AI;
using IceClash.Core;
using IceClash.Input;
using IceClash.Match;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Hockey
{
    public static class PrototypeArenaSmokeCheck
    {
        public static void Run()
        {
            PrototypeArenaBootstrap bootstrap = Object.FindAnyObjectByType<PrototypeArenaBootstrap>();
            if (bootstrap == null) bootstrap = new GameObject("Phase 3 Smoke Arena").AddComponent<PrototypeArenaBootstrap>();
            bootstrap.BuildForValidation();

            PlayerController[] players = Object.FindObjectsByType<PlayerController>();
            AiPlayerInput[] aiInputs = Object.FindObjectsByType<AiPlayerInput>();
            LocalPlayerInput[] localInputs = Object.FindObjectsByType<LocalPlayerInput>();
            LocalMatchSetup matchSetup = Object.FindAnyObjectByType<LocalMatchSetup>();
            bool hasLocal2v2 = players.Length == 4 && aiInputs.Length == 3 && localInputs.Length == 1;
            bool hasTeamIdentity = CountTeam(players, TeamId.Blue) == 2 && CountTeam(players, TeamId.Red) == 2 && HasUniquePlayerIds(players);
            PuckController puck = Object.FindAnyObjectByType<PuckController>();
            bool hasMatchSnapshots = SnapshotsMatchRuntime(matchSetup, players, puck);
            bool usesSharedControlPath = UsesExpectedInputSources(players);
            bool hasCamera = Object.FindAnyObjectByType<ElevatedFollowCamera>() != null;
            bool puckIsIndependent = puck != null && puck.transform.parent == null && puck.GetComponent<Rigidbody>() != null;
            GameObject ice = GameObject.Find("Ice");
            GameObject blueGoal = GameObject.Find("Blue Goal Post A");
            MeshFilter iceMesh = ice != null ? ice.GetComponent<MeshFilter>() : null;
            MeshCollider iceCollider = ice != null ? ice.GetComponent<MeshCollider>() : null;
            bool rinkIsVertical = iceMesh != null && iceCollider != null && iceMesh.sharedMesh.bounds.size.z > iceMesh.sharedMesh.bounds.size.x
                && iceMesh.sharedMesh.bounds.size.y >= 0.4f
                && blueGoal != null && Mathf.Abs(blueGoal.transform.position.z) > Mathf.Abs(blueGoal.transform.position.x);
            bool hockeyRinkShape = GameObject.Find("Rounded Board 00") != null
                && GameObject.Find("Blue Goal Line") != null
                && GameObject.Find("Red Goal Crease") != null
                && GameObject.Find("Center Faceoff Circle") != null
                && GameObject.Find("Faceoff Circle North East Dot") != null
                && GameObject.Find("Blue Goal Net Vertical 0") != null;

            if (!hasLocal2v2 || !hasTeamIdentity || !hasMatchSnapshots || !usesSharedControlPath || !hasCamera || !puckIsIndependent || !rinkIsVertical || !hockeyRinkShape)
            {
                Debug.LogError($"PHASE3_SMOKE_FAIL local2v2={hasLocal2v2} teamIdentity={hasTeamIdentity} snapshots={hasMatchSnapshots} sharedControl={usesSharedControlPath} camera={hasCamera} puckIndependent={puckIsIndependent} rinkVertical={rinkIsVertical} hockeyRinkShape={hockeyRinkShape}");
                throw new System.InvalidOperationException("Phase 3 arena bootstrap did not create its required local 2v2 slice.");
            }

            Debug.Log("PHASE3_SMOKE_PASS local2v2=true teamIdentity=true snapshots=true sharedControl=true aiCount=3 camera=true puckIndependent=true rinkVertical=true hockeyRinkShape=true");
        }

        private static int CountTeam(PlayerController[] players, TeamId team)
        {
            int count = 0;
            for (int index = 0; index < players.Length; index++)
            {
                if (players[index].Team == team) count++;
            }
            return count;
        }

        private static bool HasUniquePlayerIds(PlayerController[] players)
        {
            for (int left = 0; left < players.Length; left++)
            {
                if (string.IsNullOrEmpty(players[left].PlayerId)) return false;
                for (int right = left + 1; right < players.Length; right++)
                {
                    if (players[left].PlayerId == players[right].PlayerId) return false;
                }
            }
            return true;
        }

        private static bool UsesExpectedInputSources(PlayerController[] players)
        {
            for (int index = 0; index < players.Length; index++)
            {
                bool isLocal = players[index].PlayerId == "blue-local";
                if (isLocal && players[index].InputSource is not LocalPlayerInput) return false;
                if (!isLocal && players[index].InputSource is not AiPlayerInput) return false;
            }
            return true;
        }

        private static bool SnapshotsMatchRuntime(LocalMatchSetup matchSetup, PlayerController[] players, PuckController puck)
        {
            if (matchSetup == null || matchSetup.Data.State != MatchStateSnapshot.Playing
                || matchSetup.Data.BlueTeam.Players.Count != 2 || matchSetup.Data.RedTeam.Players.Count != 2) return false;

            for (int index = 0; index < players.Length; index++)
            {
                TeamData team = players[index].Team == TeamId.Blue ? matchSetup.Data.BlueTeam : matchSetup.Data.RedTeam;
                PlayerData snapshot = team.Players.Find(data => data.PlayerId == players[index].PlayerId);
                if (snapshot == null || snapshot.Team != players[index].Team || snapshot.State != players[index].State
                    || Mathf.Abs(snapshot.Stamina - players[index].Stamina) > 0.001f
                    || Vector3.Distance(snapshot.Position, players[index].transform.position) > 0.01f
                    || snapshot.HasPuck != (puck != null && puck.IsCarriedBy(players[index]))) return false;
            }
            return true;
        }
    }
}
