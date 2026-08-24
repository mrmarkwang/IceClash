/*
 * IceClash Phase 1 composite local input.
 * Merges Editor hardware with landscape touch controls while keeping movement
 * isolated from the simple recommended-target PASS tap on the right side.
 */

using IceClash.Core;
using IceClash.UI;
using UnityEngine;

namespace IceClash.Input
{
    public sealed class MobileInputSource : MonoBehaviour, IPlayerInput
    {
        private LocalPlayerInput hardware;
        private MobileJoystick joystick;
        private ActionButton pass;
        private ActionButton shoot;
        private ActionButton playerSwitch;

        public Vector2 Move
        {
            get
            {
                Vector2 touch = joystick != null ? joystick.Value : Vector2.zero;
                Vector2 local = hardware != null ? hardware.Move : Vector2.zero;
                return touch.sqrMagnitude > local.sqrMagnitude ? touch : local;
            }
        }
        public bool PassPressed => (hardware != null && hardware.PassPressed) || (pass != null && pass.Pressed);
        public bool ShootHeld => (hardware != null && hardware.ShootHeld) || (shoot != null && shoot.Held);
        public bool ShootReleased => (hardware != null && hardware.ShootReleased) || (shoot != null && shoot.Released);
        public bool SwitchPressed => (hardware != null && hardware.SwitchPressed) || (playerSwitch != null && playerSwitch.Pressed);

        public void Configure(LocalPlayerInput hardwareInput, MobileJoystick stick, ActionButton passButton, ActionButton shootButton, ActionButton switchButton)
        { hardware = hardwareInput; joystick = stick; pass = passButton; shoot = shootButton; playerSwitch = switchButton; }
    }
}
