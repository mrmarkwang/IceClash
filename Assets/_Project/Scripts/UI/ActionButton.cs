/*
 * IceClash Phase 1 mobile action button.
 * Tracks independent multi-touch held/pressed/released phases and draws one large
 * PASS, SHOOT, or SWITCH control without requiring a Canvas/UI package dependency.
 */

using UnityEngine;
using UnityEngine.InputSystem;

namespace IceClash.UI
{
    public sealed class ActionButton : MonoBehaviour
    {
        [SerializeField] private string label = "PASS";
        [SerializeField] private Rect normalizedRect = new(0.72f, 0.08f, 0.12f, 0.17f);
        private bool previousHeld;
        public bool Held { get; private set; }
        public bool Pressed { get; private set; }
        public bool Released { get; private set; }
        public string Label => label;

        public void Configure(string buttonLabel, Rect screenNormalizedRect) { label = buttonLabel; normalizedRect = screenNormalizedRect; }

        private void Update()
        {
            previousHeld = Held;
            Held = false;
            Rect hit = ScreenRectBottomLeft();
            if (Touchscreen.current != null)
                foreach (var touch in Touchscreen.current.touches)
                    if (touch.press.isPressed && hit.Contains(touch.position.ReadValue())) { Held = true; break; }
            if (!Held && Mouse.current != null && Mouse.current.leftButton.isPressed && hit.Contains(Mouse.current.position.ReadValue())) Held = true;
            Pressed = Held && !previousHeld;
            Released = !Held && previousHeld;
        }

        private Rect ScreenRectBottomLeft() => new(normalizedRect.x * Screen.width, normalizedRect.y * Screen.height,
            normalizedRect.width * Screen.width, normalizedRect.height * Screen.height);

        private void OnGUI()
        {
            Rect bottom = ScreenRectBottomLeft();
            Rect guiRect = new(bottom.x, Screen.height - bottom.y - bottom.height, bottom.width, bottom.height);
            Color prior = GUI.color;
            GUI.color = Held ? new Color(0.2f, 0.65f, 1f, 0.9f) : new Color(0.08f, 0.18f, 0.3f, 0.78f);
            GUI.Box(guiRect, label);
            GUI.color = prior;
        }
    }
}
