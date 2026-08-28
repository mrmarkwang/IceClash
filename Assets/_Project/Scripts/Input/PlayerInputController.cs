/*
 * IceClash shared local player input controller.
 * Selects movement input and remaps reusable touch action slots between offense
 * and SWITCH/CHECK defense when authoritative puck possession changes. DEKE is
 * routed only from an explicit hardware or offensive touch press.
 */

using IceClash.Core;
using IceClash.Player;
using IceClash.Puck;
using IceClash.UI;
using UnityEngine;

namespace IceClash.Input
{
    public enum MobileActionMode { Offense, Defense }

    public sealed class PlayerInputController : MonoBehaviour, IPlayerInput
    {
        private LocalPlayerInput hardware;
        private VirtualJoystick joystick;
        private MobileActionButton pass;
        private MobileActionButton deke;
        private MobileActionButton shoot;
        private PuckController puck;

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
        public bool PassPressed => (hardware != null && hardware.PassPressed)
            || (Mode == MobileActionMode.Offense && pass != null && pass.Pressed);
        public bool DekePressed => (hardware != null && hardware.DekePressed)
            || (Mode == MobileActionMode.Offense && deke != null && deke.Pressed);
        public bool ShootPressed => Mode == MobileActionMode.Offense && shoot != null && shoot.Pressed;
        public bool ShootHeld => (hardware != null && hardware.ShootHeld)
            || (Mode == MobileActionMode.Offense && shoot != null && shoot.Held);
        public bool ShootReleased => (hardware != null && hardware.ShootReleased)
            || (Mode == MobileActionMode.Offense && shoot != null && shoot.Released);
        public bool SwitchPressed => (hardware != null && hardware.SwitchPressed)
            || (Mode == MobileActionMode.Defense && pass != null && pass.Pressed);
        public bool CheckPressed => (hardware != null && hardware.CheckPressed)
            || (Mode == MobileActionMode.Defense && shoot != null && shoot.Pressed);
        public MobileActionMode Mode { get; private set; } = MobileActionMode.Offense;

        public VirtualJoystick Joystick => joystick;
        public MobileActionButton PassButton => pass;
        public MobileActionButton DekeButton => deke;
        public MobileActionButton ShootButton => shoot;

        public void Configure(LocalPlayerInput hardwareInput, VirtualJoystick virtualJoystick,
            MobileActionButton passButton, MobileActionButton dekeButton, MobileActionButton shootButton,
            PuckController controlledPuck)
        {
            if (puck != null) puck.CarrierChanged -= OnCarrierChanged;
            hardware = hardwareInput;
            joystick = virtualJoystick;
            pass = passButton;
            deke = dekeButton;
            shoot = shootButton;
            puck = controlledPuck;
            if (puck != null) puck.CarrierChanged += OnCarrierChanged;
            OnCarrierChanged(puck != null ? puck.Carrier : null);
        }

        private void OnDestroy()
        {
            if (puck != null) puck.CarrierChanged -= OnCarrierChanged;
        }

        private void OnCarrierChanged(PlayerController carrier)
        {
            SetMode(carrier != null && carrier.Team != TeamId.Blue
                ? MobileActionMode.Defense : MobileActionMode.Offense);
        }

        private void SetMode(MobileActionMode mode)
        {
            pass?.ResetInput();
            deke?.ResetInput();
            shoot?.ResetInput();
            Mode = mode;
            if (pass != null) pass.SetLabel(mode == MobileActionMode.Defense ? "SWITCH" : "PASS");
            if (deke != null)
            {
                deke.gameObject.SetActive(mode == MobileActionMode.Offense);
                if (mode == MobileActionMode.Offense) deke.SetLabel("DEKE");
            }
            if (shoot != null) shoot.SetLabel(mode == MobileActionMode.Defense ? "CHECK" : "SHOOT");
        }

        internal static Vector2 SelectMoveInput(Vector2 hardwareMove, Vector2 mobileMove)
        {
            Vector2 hardwareClamped = Vector2.ClampMagnitude(hardwareMove, 1f);
            Vector2 mobileClamped = Vector2.ClampMagnitude(mobileMove, 1f);
            return mobileClamped.sqrMagnitude > hardwareClamped.sqrMagnitude ? mobileClamped : hardwareClamped;
        }
    }
}
