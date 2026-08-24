/*
 * IceClash mobile action button input.
 * Captures one EventSystem pointer, buffers frame-stable press/hold/release phases
 * across update order, raises events, and emits temporary action debug messages.
 */

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IceClash.UI
{
    public sealed class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private const int NoPointer = int.MinValue;

        [SerializeField] private string label = "PASS";
        [SerializeField] private Text labelText;

        private int activePointerId = NoPointer;
        private int pressedFrame = -1;
        private int releasedFrame = -1;
        private int pressSequence;
        private int releaseSequence;
        private int deliveredPressSequence;
        private int deliveredReleaseSequence;
        private int pressDeliveryFrame = -1;
        private int releaseDeliveryFrame = -1;

        public event Action PressedEvent;
        public event Action ReleasedEvent;

        public string Label => label;
        public bool Pressed => ReadBufferedPhase(pressSequence, ref deliveredPressSequence, pressedFrame, ref pressDeliveryFrame);
        public bool Held => activePointerId != NoPointer;
        public bool Released => ReadBufferedPhase(releaseSequence, ref deliveredReleaseSequence, releasedFrame, ref releaseDeliveryFrame);
        public int ActivePointerId => activePointerId;

        public void Configure(string buttonLabel, Text text)
        {
            label = buttonLabel;
            labelText = text;
            if (labelText != null) labelText.text = label;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != NoPointer || eventData.button != PointerEventData.InputButton.Left) return;
            activePointerId = eventData.pointerId;
            pressedFrame = Time.frameCount;
            pressSequence++;
            Debug.Log(label);
            PressedEvent?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId) return;
            activePointerId = NoPointer;
            releasedFrame = Time.frameCount;
            releaseSequence++;
            ReleasedEvent?.Invoke();
        }

        private static bool ReadBufferedPhase(int sequence, ref int deliveredSequence, int eventFrame, ref int deliveryFrame)
        {
            if (deliveryFrame == Time.frameCount && deliveredSequence == sequence) return true;
            if (sequence == deliveredSequence) return false;
            if (eventFrame < Time.frameCount - 1)
            {
                deliveredSequence = sequence;
                return false;
            }

            deliveredSequence = sequence;
            deliveryFrame = Time.frameCount;
            return true;
        }

        private void OnDisable()
        {
            activePointerId = NoPointer;
            pressedFrame = -1;
            releasedFrame = -1;
            deliveredPressSequence = pressSequence;
            deliveredReleaseSequence = releaseSequence;
            pressDeliveryFrame = -1;
            releaseDeliveryFrame = -1;
        }
    }
}
