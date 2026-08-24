/*
 * IceClash Phase 1 goal event source.
 * Reports a dynamic puck entering the goal volume to MatchController; score
 * ownership and one-count state validation remain centralized there.
 */

using IceClash.Core;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Match
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class GoalTrigger : MonoBehaviour
    {
        private MatchController match;
        private TeamId scoringTeam;

        public void Configure(MatchController controller, TeamId teamThatScores)
        {
            match = controller;
            scoringTeam = teamThatScores;
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<PuckController>() != null) match?.RegisterGoal(scoringTeam);
        }
    }
}
