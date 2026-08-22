/*
 * IceClash Phase 1 headless smoke check.
 * Provides a repeatable Unity batch-mode verification that the runtime bootstrap can create
 * a vertical rink, player, independent physics puck, and elevated follow camera without scene-specific wiring.
 */

using IceClash.CameraSystem;
using IceClash.Player;
using IceClash.Puck;
using UnityEngine;

namespace IceClash.Hockey
{
    public static class PrototypeArenaSmokeCheck
    {
        public static void Run()
        {
            PrototypeArenaBootstrap bootstrap = new GameObject("Phase 1 Smoke Arena").AddComponent<PrototypeArenaBootstrap>();
            bootstrap.BuildForValidation();

            bool hasPlayer = Object.FindAnyObjectByType<PlayerController>() != null;
            PuckController puck = Object.FindAnyObjectByType<PuckController>();
            bool hasCamera = Object.FindAnyObjectByType<ElevatedFollowCamera>() != null;
            bool puckIsIndependent = puck != null && puck.transform.parent == null && puck.GetComponent<Rigidbody>() != null;
            GameObject ice = GameObject.Find("Ice");
            GameObject blueGoal = GameObject.Find("Blue Goal Post A");
            bool rinkIsVertical = ice != null && ice.transform.localScale.z > ice.transform.localScale.x
                && blueGoal != null && Mathf.Abs(blueGoal.transform.position.z) > Mathf.Abs(blueGoal.transform.position.x);

            if (!hasPlayer || !hasCamera || !puckIsIndependent || !rinkIsVertical)
            {
                Debug.LogError($"PHASE1_SMOKE_FAIL player={hasPlayer} camera={hasCamera} puckIndependent={puckIsIndependent} rinkVertical={rinkIsVertical}");
                throw new System.InvalidOperationException("Phase 1 arena bootstrap did not create its required playable slice.");
            }

            Debug.Log("PHASE1_SMOKE_PASS player=true camera=true puckIndependent=true rinkVertical=true");
        }
    }
}
