/*
 * IceClash Phase 1 hardware input adapter.
 * Maps keyboard and gamepad to movement-independent tap PASS, charged SHOOT,
 * and SWITCH signals through the same compact contract used by touch controls.
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
                return Vector2.ClampMagnitude(value, 1f);
            }
        }

        public bool PassPressed => WasPressed(Key.E) || (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
        public bool ShootHeld => IsPressed(Key.Space) || (Gamepad.current != null && Gamepad.current.rightTrigger.isPressed);
        public bool ShootReleased => WasReleased(Key.Space) || (Gamepad.current != null && Gamepad.current.rightTrigger.wasReleasedThisFrame);
        public bool SwitchPressed => WasPressed(Key.Q) || (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);

        private static Vector2 ReadKeyboardMove()
        {
            if (Keyboard.current == null) return Vector2.zero;
            return new Vector2(
                (Keyboard.current.dKey.isPressed ? 1f : 0f) - (Keyboard.current.aKey.isPressed ? 1f : 0f),
                (Keyboard.current.wKey.isPressed ? 1f : 0f) - (Keyboard.current.sKey.isPressed ? 1f : 0f));
        }

        private static bool IsPressed(Key key) => Keyboard.current != null && Keyboard.current[key].isPressed;
        private static bool WasPressed(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        private static bool WasReleased(Key key) => Keyboard.current != null && Keyboard.current[key].wasReleasedThisFrame;
    }
}
