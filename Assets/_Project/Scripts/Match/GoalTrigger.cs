/*
 * IceClash Phase 1 goal event source.
 * Reports a dynamic puck entering the goal volume to MatchController. Goals are
 * one-way: the puck center must be inside the bounded goal volume while travelling
 * into the net. A swept goal-line fallback catches fast shots that cross the entire
 * trigger step even when the physical net reverses their velocity on that step.
 * Match state and one-count ownership stay central.
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
        private PuckController trackedPuck;
        private TeamId scoringTeam;
        private Vector3 scoringDirection;
        private Vector3 previousPuckPosition;
        private bool hasPreviousPuckPosition;

        internal TeamId ScoringTeam => scoringTeam;
        internal Vector3 ScoringDirection => scoringDirection;

        public void Configure(MatchController controller, PuckController puck, TeamId teamThatScores,
            Vector3 travelDirectionIntoNet)
        {
            Vector3 planarDirection = Vector3.ProjectOnPlane(travelDirectionIntoNet, Vector3.up);
            if (planarDirection.sqrMagnitude < 0.01f)
                throw new System.ArgumentException("A goal requires a horizontal scoring direction.", nameof(travelDirectionIntoNet));

            match = controller;
            if (trackedPuck != null) trackedPuck.PositionReset -= HandlePuckReset;
            trackedPuck = puck;
            if (trackedPuck != null) trackedPuck.PositionReset += HandlePuckReset;
            scoringTeam = teamThatScores;
            scoringDirection = planarDirection.normalized;
            GetComponent<BoxCollider>().isTrigger = true;
            if (trackedPuck != null && trackedPuck.Body != null)
                HandlePuckReset(trackedPuck.Body.position);
        }

        private void OnDestroy()
        {
            if (trackedPuck != null) trackedPuck.PositionReset -= HandlePuckReset;
        }

        private void FixedUpdate() => TickSweptGoalLine();

        private void OnTriggerEnter(Collider other)
        {
            PuckController puck = other.GetComponent<PuckController>();
            if (puck != null) TryRegisterGoal(puck);
        }

        private void OnTriggerStay(Collider other)
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
            BoxCollider volume = GetComponent<BoxCollider>();
            Vector3 localPosition = transform.InverseTransformPoint(puckPosition) - volume.center;
            Vector3 halfExtents = volume.size * 0.5f;
            bool centerInsideVolume = Mathf.Abs(localPosition.x) <= halfExtents.x
                && Mathf.Abs(localPosition.y) <= halfExtents.y
                && Mathf.Abs(localPosition.z) <= halfExtents.z;
            float entrySide = Vector3.Dot(puckPosition - transform.position, scoringDirection);
            float inwardSpeed = Vector3.Dot(puckVelocity, scoringDirection);
            return centerInsideVolume && entrySide < -EntrySideTolerance
                && inwardSpeed > MinimumInwardSpeed;
        }

        internal void TickSweptGoalLineForValidation() => TickSweptGoalLine();
        private void HandlePuckReset(Vector3 position)
        {
            previousPuckPosition = position;
            hasPreviousPuckPosition = true;
        }

        private void TickSweptGoalLine()
        {
            if (trackedPuck == null || trackedPuck.Body == null) return;
            Vector3 currentPosition = trackedPuck.Body.position;
            if (hasPreviousPuckPosition && match != null && match.State == MatchStateSnapshot.Playing
                && CrossedGoalLine(previousPuckPosition, currentPosition))
            {
                match.RegisterGoal(scoringTeam);
                return;
            }
            previousPuckPosition = currentPosition;
            hasPreviousPuckPosition = true;
        }

        private bool CrossedGoalLine(Vector3 previousPosition, Vector3 currentPosition)
        {
            BoxCollider volume = GetComponent<BoxCollider>();
            Vector3 goalLine = transform.position - scoringDirection * (volume.size.z * 0.5f);
            float previousSide = Vector3.Dot(previousPosition - goalLine, scoringDirection);
            float currentSide = Vector3.Dot(currentPosition - goalLine, scoringDirection);
            if (previousSide >= 0f || currentSide < 0f) return false;

            float crossing = Mathf.Clamp01(-previousSide / Mathf.Max(currentSide - previousSide, 0.0001f));
            Vector3 localCrossing = transform.InverseTransformPoint(
                Vector3.Lerp(previousPosition, currentPosition, crossing)) - volume.center;
            Vector3 halfExtents = volume.size * 0.5f;
            return Mathf.Abs(localCrossing.x) <= halfExtents.x
                && Mathf.Abs(localCrossing.y) <= halfExtents.y;
        }
    }
}
