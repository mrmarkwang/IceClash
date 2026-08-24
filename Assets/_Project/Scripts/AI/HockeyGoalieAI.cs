/*
 * IceClash Phase 1 believable goalie AI.
 * Holds a crease anchor, tracks the puck laterally, and applies bounded save
 * rebounds to incoming free pucks before returning to its reset position.
 */

using IceClash.Core;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.AI
{
    public sealed class HockeyGoalieAI : MonoBehaviour, IResettableActor
    {
        [SerializeField] private float lateralRange = 2.25f;
        [SerializeField] private float movementSpeed = 5.5f;
        [SerializeField] private float saveRadius = 1.35f;
        [SerializeField] private float reboundSpeed = 8f;
        [SerializeField] private float reactionDelay = 0.12f;

        private TeamId team;
        private PuckController puck;
        private Vector3 anchor;
        private float nextSaveTime;

        public TeamId Team => team;
        public Vector3 Anchor => anchor;

        public void Configure(TeamId goalieTeam, PuckController controlledPuck, Vector3 creaseAnchor)
        { team = goalieTeam; puck = controlledPuck; anchor = creaseAnchor; }

        private void Update()
        {
            if (puck == null) return;
            float targetX = Mathf.Clamp(puck.transform.position.x, anchor.x - lateralRange, anchor.x + lateralRange);
            Vector3 target = new(targetX, anchor.y, anchor.z);
            transform.position = Vector3.MoveTowards(transform.position, target, movementSpeed * Time.deltaTime);
            transform.rotation = team == TeamId.Blue ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);

            if (Time.time < nextSaveTime || puck.Carrier != null) return;
            Vector3 delta = puck.transform.position - transform.position;
            bool approaching = team == TeamId.Blue ? puck.Body.linearVelocity.z < -0.5f : puck.Body.linearVelocity.z > 0.5f;
            if (approaching && delta.sqrMagnitude <= saveRadius * saveRadius)
            {
                float away = team == TeamId.Blue ? 1f : -1f;
                Vector3 rebound = new Vector3(Mathf.Clamp(delta.x, -0.8f, 0.8f), 0f, away).normalized;
                puck.ApplySave(rebound, reboundSpeed, team);
                nextSaveTime = Time.time + reactionDelay + 0.28f;
            }
        }

        public void ResetActor()
        {
            transform.SetPositionAndRotation(anchor, team == TeamId.Blue ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f));
            nextSaveTime = 0f;
        }
    }
}
