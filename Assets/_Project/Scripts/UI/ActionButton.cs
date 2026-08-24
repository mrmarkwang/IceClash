/*
 * IceClash Phase 1 mobile action button.
 * Captures the touch or mouse pointer that begins inside a mobile action control,
 * retains stable held/pressed/released phases, and draws one large PASS, SHOOT,
 * or SWITCH control without Canvas dependencies.
 */

using UnityEngine;
using UnityEngine.InputSystem;

namespace IceClash.UI
{
    [DefaultExecutionOrder(-300)]
    public sealed class ActionButton : MonoBehaviour
    {
        [SerializeField] private string label = "PASS";
        [SerializeField] private Rect normalizedRect = new(0.72f, 0.08f, 0.12f, 0.17f);
        private bool previousHeld;
        private int activeTouchId = -1;
        private bool mouseCaptured;
        public bool Held { get; private set; }
        public bool Pressed { get; private set; }
        public bool Released { get; private set; }
        public string Label => label;

        public void Configure(string buttonLabel, Rect screenNormalizedRect) { label = buttonLabel; normalizedRect = screenNormalizedRect; }

        private void Update()
        {
            previousHeld = Held;
            Held = false;
            Pressed = false;
            Released = false;
            Rect hit = ScreenRectBottomLeft();
            if (Touchscreen.current != null)
            {
                bool foundActiveTouch = false;
                foreach (var touch in Touchscreen.current.touches)
                {
                    int id = touch.touchId.ReadValue();
                    if (activeTouchId == id)
                    {
                        foundActiveTouch = true;
                        Held = touch.press.isPressed;
                        if (!Held) activeTouchId = -1;
                        break;
                    }
                }
                if (activeTouchId >= 0 && !foundActiveTouch) activeTouchId = -1;

                if (activeTouchId < 0 && !previousHeld)
                {
                    foreach (var touch in Touchscreen.current.touches)
                    {
                        Vector2 position = touch.position.ReadValue();
                        if (!touch.press.wasPressedThisFrame || !hit.Contains(position)) continue;
                        activeTouchId = touch.touchId.ReadValue();
                        Held = true;
                        Pressed = true;
                        break;
                    }
                }
            }

            if (activeTouchId < 0 && Mouse.current != null)
            {
                if (!mouseCaptured && Mouse.current.leftButton.wasPressedThisFrame && hit.Contains(Mouse.current.position.ReadValue()))
                {
                    mouseCaptured = true;
                    Held = true;
                    Pressed = true;
                }
                else if (mouseCaptured)
                {
                    Held = Mouse.current.leftButton.isPressed;
                    if (!Held) mouseCaptured = false;
                }
            }

            if (Held && !previousHeld) Pressed = true;
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
