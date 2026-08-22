/*
 * IceClash local input adapter.
 * Reads keyboard and gamepad values exclusively through Unity's Input System and
 * exposes commands through IPlayerInput so future AI or network sources share the controller path.
 */

using IceClash.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IceClash.Input
{
    public sealed class LocalPlayerInput : MonoBehaviour, IPlayerInput
    {
        public Vector2 Move
        {
            get
            {
                Vector2 keyboard = ReadKeyboardMove();
                Vector2 gamepad = Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
                Vector2 value = gamepad.sqrMagnitude > keyboard.sqrMagnitude ? gamepad : keyboard;
                return value.sqrMagnitude > 1f ? value.normalized : value;
            }
        }

        public bool SprintHeld => IsPressed(Key.LeftShift) || (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed);
        public bool ShootPressed => WasPressed(Key.Space) || (Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame);
        public bool PassPressed => WasPressed(Key.E) || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
        public bool CheckPressed => WasPressed(Key.Q) || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);

        private static Vector2 ReadKeyboardMove()
        {
            if (Keyboard.current == null) return Vector2.zero;

            return new Vector2(
                (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f),
                (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f));
        }

        private static bool IsPressed(Key key) => Keyboard.current != null && Keyboard.current[key].isPressed;
        private static bool WasPressed(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
    }
}
