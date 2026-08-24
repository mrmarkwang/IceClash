/*
 * IceClash Phase 1 goal event source.
 * Reports a dynamic puck entering the goal volume to MatchController. Goals are
 * one-way: the puck must enter from the rink side while travelling into the net.
 * Score ownership and one-count state validation remain centralized in the match.
 */

using IceClash.Core;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Match
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class GoalTrigger : MonoBehaviour
    {
        private const float MinimumInwardSpeed = 0.05f;
        private const float EntrySideTolerance = 0.05f;

        private MatchController match;
        private TeamId scoringTeam;
        private Vector3 scoringDirection;

        internal TeamId ScoringTeam => scoringTeam;
        internal Vector3 ScoringDirection => scoringDirection;

        public void Configure(MatchController controller, TeamId teamThatScores, Vector3 travelDirectionIntoNet)
        {
            Vector3 planarDirection = Vector3.ProjectOnPlane(travelDirectionIntoNet, Vector3.up);
            if (planarDirection.sqrMagnitude < 0.01f)
                throw new System.ArgumentException("A goal requires a horizontal scoring direction.", nameof(travelDirectionIntoNet));

            match = controller;
            scoringTeam = teamThatScores;
            scoringDirection = planarDirection.normalized;
            GetComponent<BoxCollider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            PuckController puck = other.GetComponent<PuckController>();
            if (puck != null) TryRegisterGoal(puck);
        }

        internal bool TryRegisterGoal(PuckController puck)
        {
            if (puck == null || match == null || match.State != MatchStateSnapshot.Playing
                || !IsValidEntry(puck.Body.position, puck.Body.linearVelocity)) return false;

            match.RegisterGoal(scoringTeam);
            return true;
        }

        internal bool IsValidEntry(Vector3 puckPosition, Vector3 puckVelocity)
        {
            float entrySide = Vector3.Dot(puckPosition - transform.position, scoringDirection);
            float inwardSpeed = Vector3.Dot(puckVelocity, scoringDirection);
            return entrySide < -EntrySideTolerance && inwardSpeed > MinimumInwardSpeed;
        }
    }
}
