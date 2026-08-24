/*
 * IceClash floating virtual joystick.
 * Captures one EventSystem pointer inside a dedicated lower-left area, floats
 * the visible base to that pointer, and exposes dead-zone-remapped skating intent.
 */

using UnityEngine;
using UnityEngine.EventSystems;

namespace IceClash.UI
{
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private const int NoPointer = int.MinValue;

        [SerializeField, Range(0f, 0.95f)] private float deadZone = 0.12f;
        [SerializeField, Min(1f)] private float radius = 130f;
        [SerializeField] private RectTransform joystickArea;
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;

        private int activePointerId = NoPointer;
        private Vector2 origin;

        public Vector2 Direction { get; private set; }
        public int ActivePointerId => activePointerId;
        public float DeadZone => deadZone;

        public void Configure(RectTransform area, RectTransform joystickBackground, RectTransform joystickHandle,
            float joystickRadius, float configuredDeadZone)
        {
            joystickArea = area;
            background = joystickBackground;
            handle = joystickHandle;
            radius = Mathf.Max(1f, joystickRadius);
            deadZone = Mathf.Clamp(configuredDeadZone, 0f, 0.95f);
            ResetJoystick();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != NoPointer || eventData.button != PointerEventData.InputButton.Left) return;
            if (!TryGetLocalPoint(eventData, out Vector2 localPoint)) return;

            activePointerId = eventData.pointerId;
            origin = localPoint;
            background.anchoredPosition = origin;
            handle.anchoredPosition = Vector2.zero;
            Direction = Vector2.zero;
            background.gameObject.SetActive(true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId || !TryGetLocalPoint(eventData, out Vector2 localPoint)) return;
            Vector2 offset = Vector2.ClampMagnitude(localPoint - origin, radius);
            handle.anchoredPosition = offset;
            Direction = ApplyDeadZone(offset / radius, deadZone);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId) ResetJoystick();
        }

        private void OnDisable() => ResetJoystick();

        internal static Vector2 ApplyDeadZone(Vector2 input, float threshold)
        {
            float magnitude = Mathf.Clamp01(input.magnitude);
            float clampedThreshold = Mathf.Clamp(threshold, 0f, 0.95f);
            if (magnitude <= clampedThreshold) return Vector2.zero;
            float remappedMagnitude = Mathf.InverseLerp(clampedThreshold, 1f, magnitude);
            return input.normalized * remappedMagnitude;
        }

        private bool TryGetLocalPoint(PointerEventData eventData, out Vector2 localPoint)
        {
            localPoint = Vector2.zero;
            return joystickArea != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickArea, eventData.position, eventData.pressEventCamera, out localPoint);
        }

        private void ResetJoystick()
        {
            activePointerId = NoPointer;
            Direction = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            if (background != null) background.gameObject.SetActive(false);
        }
    }
}
