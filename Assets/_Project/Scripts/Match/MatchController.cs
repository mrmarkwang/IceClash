/*
 * IceClash Phase 1 local match flow.
 * Owns clock, scores, faceoff/playing/goal-pause/finished transitions, one-count
 * goals, actor resets, HUD events, and final Human/AI/Draw result text.
 */

using System.Collections.Generic;
using IceClash.AI;
using IceClash.Core;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Match
{
    public sealed class MatchController : MonoBehaviour
    {
        [SerializeField] private float matchDuration = 180f;
        [SerializeField] private float goalPauseSeconds = 1.5f;

        private readonly List<PlayerController> players = new();
        private readonly List<HockeyGoalieAI> goalies = new();
        private PuckController puck;
        private FaceoffController faceoff;
        private Vector3 puckResetPosition;
        private float stateEndsAt;

        public MatchStateSnapshot State { get; private set; } = MatchStateSnapshot.Setup;
        public int BlueScore { get; private set; }
        public int RedScore { get; private set; }
        public float RemainingSeconds { get; private set; }
        public string ResultText => BlueScore > RedScore ? "HUMAN TEAM WINS" : RedScore > BlueScore ? "AI TEAM WINS" : "DRAW";

        public void Configure(IReadOnlyList<PlayerController> skaters, IReadOnlyList<HockeyGoalieAI> goalieActors, PuckController controlledPuck, Vector3 puckReset)
        {
            players.Clear(); goalies.Clear();
            for (int i = 0; i < skaters.Count; i++) players.Add(skaters[i]);
            for (int i = 0; i < goalieActors.Count; i++) goalies.Add(goalieActors[i]);
            puck = controlledPuck;
            puckResetPosition = puckReset;
            faceoff = gameObject.GetComponent<FaceoffController>() ?? gameObject.AddComponent<FaceoffController>();
            BlueScore = RedScore = 0;
            RemainingSeconds = matchDuration;
            BeginFaceoff();
        }

        private void Update()
        {
            if (State == MatchStateSnapshot.Faceoff && faceoff.TickComplete()) SetState(MatchStateSnapshot.Playing);
            else if (State == MatchStateSnapshot.GoalPause && Time.time >= stateEndsAt) BeginFaceoff();
            else if (State == MatchStateSnapshot.Playing)
            {
                RemainingSeconds = Mathf.Max(0f, RemainingSeconds - Time.deltaTime);
                if (RemainingSeconds <= 0f) { SetPlayersEnabled(false); SetState(MatchStateSnapshot.Finished); }
                else RaiseChanged();
            }
        }

        public void RegisterGoal(TeamId scoringTeam)
        {
            if (State != MatchStateSnapshot.Playing) return;
            if (scoringTeam == TeamId.Blue) BlueScore++; else RedScore++;
            SetPlayersEnabled(false);
            State = MatchStateSnapshot.GoalPause;
            stateEndsAt = Time.time + goalPauseSeconds;
            RaiseChanged();
            ResetActors();
        }

#if UNITY_EDITOR
        public void StartPlayImmediatelyForValidation() => SetState(MatchStateSnapshot.Playing);
        public void ExpireImmediatelyForValidation()
        {
            RemainingSeconds = 0f;
            SetState(MatchStateSnapshot.Finished);
        }
#endif

        public void BeginFaceoff()
        {
            ResetActors();
            SetPlayersEnabled(false);
            State = MatchStateSnapshot.Faceoff;
            faceoff.Begin();
            RaiseChanged();
        }

        private void ResetActors()
        {
            for (int i = 0; i < players.Count; i++) players[i].ResetActor();
            for (int i = 0; i < goalies.Count; i++) goalies[i].ResetActor();
            puck.ResetPuck(puckResetPosition);
        }

        private void SetPlayersEnabled(bool value)
        {
            for (int i = 0; i < players.Count; i++) players[i].SetGameplayEnabled(value);
        }

        private void SetState(MatchStateSnapshot value)
        {
            State = value;
            SetPlayersEnabled(value == MatchStateSnapshot.Playing);
            RaiseChanged();
        }

        private void RaiseChanged() => GameplayEvents.RaiseMatchChanged(BlueScore, RedScore, RemainingSeconds, State);
    }
}
