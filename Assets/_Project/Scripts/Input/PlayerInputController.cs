/*
 * IceClash shared local player input controller.
 * Selects the strongest clamped hardware or virtual-joystick movement intention
 * and exposes mobile action phases without owning skating or puck behavior.
 */

using IceClash.Core;
using IceClash.UI;
using UnityEngine;

namespace IceClash.Input
{
    public sealed class PlayerInputController : MonoBehaviour, IPlayerInput
    {
        private LocalPlayerInput hardware;
        private VirtualJoystick joystick;
        private MobileActionButton pass;
        private MobileActionButton deke;
        private MobileActionButton shoot;

        public Vector2 MoveInput
        {
            get
            {
                Vector2 hardwareMove = hardware != null ? hardware.Move : Vector2.zero;
                Vector2 mobileMove = joystick != null ? joystick.Direction : Vector2.zero;
                return SelectMoveInput(hardwareMove, mobileMove);
            }
        }

        public Vector2 Move => MoveInput;
        public bool PassPressed => (hardware != null && hardware.PassPressed) || (pass != null && pass.Pressed);
        public bool DekePressed => deke != null && deke.Pressed;
        public bool ShootPressed => shoot != null && shoot.Pressed;
        public bool ShootHeld => (hardware != null && hardware.ShootHeld) || (shoot != null && shoot.Held);
        public bool ShootReleased => (hardware != null && hardware.ShootReleased) || (shoot != null && shoot.Released);
        public bool SwitchPressed => hardware != null && hardware.SwitchPressed;

        public VirtualJoystick Joystick => joystick;
        public MobileActionButton PassButton => pass;
        public MobileActionButton DekeButton => deke;
        public MobileActionButton ShootButton => shoot;

        public void Configure(LocalPlayerInput hardwareInput, VirtualJoystick virtualJoystick,
            MobileActionButton passButton, MobileActionButton dekeButton, MobileActionButton shootButton)
        {
            hardware = hardwareInput;
            joystick = virtualJoystick;
            pass = passButton;
            deke = dekeButton;
            shoot = shootButton;
        }

        internal static Vector2 SelectMoveInput(Vector2 hardwareMove, Vector2 mobileMove)
        {
            Vector2 hardwareClamped = Vector2.ClampMagnitude(hardwareMove, 1f);
            Vector2 mobileClamped = Vector2.ClampMagnitude(mobileMove, 1f);
            return mobileClamped.sqrMagnitude > hardwareClamped.sqrMagnitude ? mobileClamped : hardwareClamped;
        }
    }
}
