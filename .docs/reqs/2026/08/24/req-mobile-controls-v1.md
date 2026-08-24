# Mobile Controls V1

## Problem

IceClash's enabled `PrototypeArena` gameplay scene currently renders fixed-position IMGUI controls. The joystick is not floating, has no configurable dead zone, and the action set does not include DEKE. The controls also lack the requested Unity UI hierarchy, reference-resolution scaling, and safe-area layout needed for reliable landscape mobile testing.

## Requirement

Provide a mobile-first landscape control overlay with a floating lower-left virtual joystick and lower-right PASS, DEKE, and larger SHOOT buttons. Keyboard/gamepad and touch input must feed the same existing skating controller through one shared player-input layer. The joystick must represent analog movement intent only, while action controls expose pointer phases and temporary debug signals without adding new puck, animation, or deke gameplay.

## Acceptance Criteria

- [x] The enabled gameplay scene creates a Unity UI `Canvas/MobileControls` hierarchy containing `JoystickArea/JoystickBackground/JoystickHandle` and `ActionButtons/PassButton`, `DekeButton`, and `ShootButton`.
- [x] The Canvas uses `Scale With Screen Size` at `1920 x 1080`, and the mobile controls remain anchored within the current device safe area in landscape layouts.
- [x] A touch or mouse press in the lower-left joystick area moves the hidden/reset joystick base to that pointer, tracks only that pointer, clamps the visible handle to its radius, outputs a maximum magnitude of one, and resets to zero on release.
- [x] The joystick applies an Inspector-configurable dead zone in the requested starting range and smoothly remaps the remaining analog magnitude.
- [x] PASS, DEKE, and SHOOT expose press state, immediate pressed-state visual feedback, and temporary `PASS`, `DEKE`, and `SHOOT` debug output; SHOOT is the largest and easiest right-thumb target.
- [x] Independent pointer ownership allows joystick movement to remain active while PASS, DEKE, or SHOOT is pressed by another finger.
- [x] WASD and gamepad movement remain available for Editor/desktop testing, and the strongest active movement source is clamped rather than summed beyond magnitude one.
- [x] Keyboard/gamepad and mobile movement pass through one shared player-input component into the existing `PlayerController` and `PlayerMovementController`; no second skating implementation or direct joystick transform manipulation is added.
- [x] Existing skating acceleration, deceleration, momentum, maximum speed, turning, and camera behavior remain owned by their current systems.
- [x] The project is restricted to landscape-left/right orientation, compiles without errors, and its automated gameplay smoke check verifies the new control structure and input invariants.

## Constraints

- Use Unity UI through Unity `6000.5.9f1`'s bundled `com.unity.ugui` package and the installed Unity Input System; add no third-party package or external-service dependency.
- Preserve the runtime composition pattern used by the empty `PrototypeArena` scene and its bootstrap.
- Preserve existing keyboard/gamepad controls and the current skating physics implementation.
- Keep joystick, action-button, input-composition, UI-building, and skating responsibilities modular.
- Do not overwrite or revert the user's existing uncommitted puck, stick, shooting, bootstrap, or smoke-check tuning.
- Device-only reachability and real multi-touch checks may remain explicitly pending when no attached phone or emulator is available.

## Non-Goals

- New puck, passing, shooting, deke, checking, goalie, AI, multiplayer, backend, account, store, progression, animation, or final-art gameplay.
- Replacing existing puck/gameplay systems already present in the repository.
- A second `Game` scene, separate mobile/desktop player controllers, feature flags, fallback control modes, or compatibility layers.
- Charge-shot gameplay changes; V1 only preserves press/hold/release-capable button phases for future use.
