/*
 * IceClash Phase 1 minimal match HUD.
 * Displays Human/AI score, MM:SS, faceoff/goal state, and final result while the
 * dedicated joystick and three action components draw the uncluttered controls.
 */

using IceClash.Core;
using UnityEngine;

namespace IceClash.UI
{
    public sealed class MatchHUD : MonoBehaviour
    {
        private int blueScore;
        private int redScore;
        private float remaining;
        private MatchStateSnapshot state;

        private void OnEnable() => GameplayEvents.MatchChanged += OnMatchChanged;
        private void OnDisable() => GameplayEvents.MatchChanged -= OnMatchChanged;
        private void OnMatchChanged(int blue, int red, float time, MatchStateSnapshot matchState)
        { blueScore = blue; redScore = red; remaining = time; state = matchState; }

        private void OnGUI()
        {
            GUIStyle top = new(GUI.skin.box) { fontSize = Mathf.Max(18, Screen.height / 28), alignment = TextAnchor.MiddleCenter };
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);
            GUI.Box(new Rect(Screen.width * 0.25f, Screen.height * 0.025f, Screen.width * 0.5f, Screen.height * 0.1f),
                $"HUMAN TEAM  {blueScore}     {minutes:00}:{seconds:00}     {redScore}  AI TEAM", top);
            if (state == MatchStateSnapshot.Faceoff || state == MatchStateSnapshot.GoalPause || state == MatchStateSnapshot.Finished)
            {
                string message = state == MatchStateSnapshot.Faceoff ? "FACEOFF" : state == MatchStateSnapshot.GoalPause ? "GOAL!" :
                    blueScore > redScore ? "HUMAN TEAM WINS" : redScore > blueScore ? "AI TEAM WINS" : "DRAW";
                GUI.Box(new Rect(Screen.width * 0.35f, Screen.height * 0.2f, Screen.width * 0.3f, Screen.height * 0.12f), message, top);
            }
        }
    }
}
