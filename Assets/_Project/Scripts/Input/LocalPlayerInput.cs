/*
 * IceClash Phase 1 hardware input adapter.
 * Maps keyboard and gamepad to movement-independent PASS, charged SHOOT,
 * SWITCH, and contextual CHECK signals shared with touch controls.
 */

using IceClash.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace IceClash.Input
{
    public sealed class LocalPlayerInput : MonoBehaviour, IPlayerInput
    {
        internal const Key PassKeyboardKey = Key.E;
        internal const Key ShootKeyboardKey = Key.Space;
        internal const Key SwitchKeyboardKey = Key.Q;
        internal const Key CheckKeyboardKey = Key.F;
        internal const GamepadButton PassGamepadButton = GamepadButton.West;
        internal const GamepadButton SwitchGamepadButton = GamepadButton.North;
        internal const GamepadButton CheckGamepadButton = GamepadButton.East;

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

        public bool PassPressed => WasPressed(PassKeyboardKey) || WasPressed(PassGamepadButton);
        public bool ShootHeld => IsPressed(ShootKeyboardKey) || (Gamepad.current != null && Gamepad.current.rightTrigger.isPressed);
        public bool ShootReleased => WasReleased(ShootKeyboardKey) || (Gamepad.current != null && Gamepad.current.rightTrigger.wasReleasedThisFrame);
        public bool SwitchPressed => WasPressed(SwitchKeyboardKey) || WasPressed(SwitchGamepadButton);
        public bool CheckPressed => WasPressed(CheckKeyboardKey) || WasPressed(CheckGamepadButton);

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
        private static bool WasPressed(GamepadButton button) => Gamepad.current != null
            && Gamepad.current[button].wasPressedThisFrame;
    }
}
