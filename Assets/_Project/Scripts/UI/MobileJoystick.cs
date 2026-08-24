/*
 * IceClash Phase 1 landscape virtual joystick.
 * Reads a dedicated bottom-left touch or mouse pointer and exposes an analog
 * direction while allowing simultaneous action-button touches.
 */

using UnityEngine;
using UnityEngine.InputSystem;

namespace IceClash.UI
{
    public sealed class MobileJoystick : MonoBehaviour
    {
        [SerializeField] private Vector2 normalizedCenter = new(0.14f, 0.18f);
        [SerializeField] private float normalizedRadius = 0.105f;
        private int activeTouch = -1;
        public Vector2 Value { get; private set; }

        private void Update()
        {
            Vector2 center = new(Screen.width * normalizedCenter.x, Screen.height * normalizedCenter.y);
            float radius = Mathf.Min(Screen.width, Screen.height) * normalizedRadius;
            bool found = false;
            if (Touchscreen.current != null)
            {
                foreach (var touch in Touchscreen.current.touches)
                {
                    int id = touch.touchId.ReadValue();
                    if (!touch.press.isPressed) { if (id == activeTouch) activeTouch = -1; continue; }
                    Vector2 position = touch.position.ReadValue();
                    if (activeTouch == id || (activeTouch < 0 && Vector2.Distance(position, center) <= radius * 1.45f))
                    { activeTouch = id; Value = Vector2.ClampMagnitude((position - center) / radius, 1f); found = true; break; }
                }
            }
            if (!found && Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Vector2 position = Mouse.current.position.ReadValue();
                if (Vector2.Distance(position, center) <= radius * 1.45f)
                { Value = Vector2.ClampMagnitude((position - center) / radius, 1f); found = true; }
            }
            if (!found) { Value = Vector2.zero; if (activeTouch >= 0) activeTouch = -1; }
        }

        private void OnGUI()
        {
            float radius = Mathf.Min(Screen.width, Screen.height) * normalizedRadius;
            Vector2 center = new(Screen.width * normalizedCenter.x, Screen.height * (1f - normalizedCenter.y));
            Rect baseRect = new(center.x - radius, center.y - radius, radius * 2f, radius * 2f);
            Color previous = GUI.color;
            GUI.color = new Color(0.08f, 0.15f, 0.24f, 0.55f);
            GUI.Box(baseRect, string.Empty);
            GUI.color = new Color(0.8f, 0.92f, 1f, 0.75f);
            Vector2 knob = new Vector2(Value.x, -Value.y) * radius * 0.55f;
            GUI.Box(new Rect(center.x + knob.x - radius * 0.32f, center.y + knob.y - radius * 0.32f, radius * 0.64f, radius * 0.64f), string.Empty);
            GUI.color = previous;
        }
    }
}
