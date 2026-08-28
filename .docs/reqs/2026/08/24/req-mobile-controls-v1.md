# Mobile Controls V1

## Problem

IceClash's mobile overlay now provides the shared input route, safe-area layout, and PASS/DEKE/SHOOT controls, but the joystick base is hidden until a touch lands anywhere in a broad floating-input region. The requested reference instead keeps the virtual stick visible and predictable inside a dedicated lower-left control zone, so players can find it before touching the screen.

## Requirement

Provide a mobile-first landscape control overlay with a fixed, always-visible lower-left virtual joystick and lower-right PASS, DEKE, and larger SHOOT buttons. Keyboard/gamepad and touch input must feed the same existing skating controller through one shared player-input layer. The joystick must represent analog movement intent only, while action controls expose pointer phases and temporary debug signals without adding new puck, animation, or deke gameplay.

Refine the completed overlay before later production work so its visible controls use a coherent, reference-inspired translucent circular treatment, its generous interaction regions remain independent from decorative geometry, and its readability and two-thumb ergonomics can be evaluated across representative landscape layouts without changing the established input or gameplay contracts.

## Acceptance Criteria

- [x] The enabled gameplay scene creates a Unity UI `Canvas/MobileControls` hierarchy containing `JoystickArea/JoystickBackground/JoystickHandle` and `ActionButtons/PassButton`, `DekeButton`, and `ShootButton`.
- [x] The Canvas uses `Scale With Screen Size` at `1920 x 1080`, and the mobile controls remain anchored within the current device safe area in landscape layouts.
- [x] The joystick base remains visible and centered in a dedicated lower-left safe-area-contained control zone before, during, and after input; pressing or dragging inside that zone moves only the handle, never relocates the base.
- [x] The fixed joystick tracks only the pointer that began inside its hit region, clamps the handle to its radius, outputs a maximum magnitude of one, and returns the handle and direction to center/zero on release while leaving the base visible.
- [x] The joystick applies an Inspector-configurable dead zone in the requested starting range and smoothly remaps the remaining analog magnitude.
- [x] PASS, DEKE, and SHOOT expose press state, immediate pressed-state visual feedback, and temporary `PASS`, `DEKE`, and `SHOOT` debug output; SHOOT is the largest and easiest right-thumb target.
- [x] Independent pointer ownership allows joystick movement to remain active while PASS, DEKE, or SHOOT is pressed by another finger.
- [x] WASD and gamepad movement remain available for Editor/desktop testing, and the strongest active movement source is clamped rather than summed beyond magnitude one.
- [x] Keyboard/gamepad and mobile movement pass through one shared player-input component into the existing `PlayerController` and `PlayerMovementController`; no second skating implementation or direct joystick transform manipulation is added.
- [x] Existing skating acceleration, deceleration, momentum, maximum speed, turning, and camera behavior remain owned by their current systems.
- [x] The project is restricted to landscape-left/right orientation, compiles without errors, and its automated gameplay smoke check verifies the new control structure and input invariants.
- [x] The joystick base/handle and PASS, DEKE, and SHOOT visuals use a coherent translucent circular treatment that remains readable over bright ice and rink markings, with SHOOT visibly larger and more prominent than PASS and DEKE.
- [x] Each action's raycast target is a generous safe-area-contained interaction region that is at least as large as its visible control, decorative children do not intercept input, and adjacent action hit regions do not overlap.
- [x] The refined overlay preserves fixed-zone analog activation, dead-zone behavior, release-to-zero, independent pointer ownership, PASS, debug-only DEKE, charged SHOOT, desktop input, and the single shared skating route.
- [ ] At 16:9, 19.5:9, and 20:9 landscape Game views, recorded screenshots and observations show readable, non-overlapping controls that remain inside the safe area and do not obscure the controlled skater during ordinary camera framing.
- [x] The smoke check verifies the refined visual/hit hierarchy and unchanged bindings; a physical-device two-thumb session records both landscape orientations, multi-touch behavior, reachability, missed touches, FPS, and thermals when hardware is available, or the lack of device evidence remains explicitly reported as a release risk.

## Constraints

- Use Unity UI through Unity `6000.5.9f1`'s bundled `com.unity.ugui` package and the installed Unity Input System; add no third-party package or external-service dependency.
- Preserve the runtime composition pattern used by the empty `PrototypeArena` scene and its bootstrap.
- Preserve existing keyboard/gamepad controls and the current skating physics implementation.
- Keep joystick, action-button, input-composition, UI-building, and skating responsibilities modular.
- Do not overwrite or revert the user's existing uncommitted puck, stick, shooting, bootstrap, or smoke-check tuning.
- Device-only reachability and real multi-touch checks may remain explicitly pending when no attached phone or emulator is available.
- Use project-owned Unity UI shapes, generated visuals, and labels; the supplied image is visual direction and must not be copied as branded artwork.
- Keep visible circular geometry separate from larger transparent raycast targets so visual styling does not reduce touch accessibility.
- Keep the fixed joystick zone large enough for comfortable thumb travel but bounded so touches elsewhere on the left half of the rink do not unexpectedly steer the player.

## Non-Goals

- New puck, passing, shooting, deke, checking, goalie, AI, multiplayer, backend, account, store, progression, animation, or final-art gameplay.
- Replacing existing puck/gameplay systems already present in the repository.
- A second `Game` scene, separate mobile/desktop player controllers, feature flags, fallback control modes, or compatibility layers.
- Changes to the existing charged-shot mechanics; the refinement only preserves and presents the current press/hold/release input behavior.
- Actual deke behavior, new action types, a second aiming stick, haptics, accessibility settings, final production art, animation, and multiplayer work.
- Per-device layout forks, new packages, copied reference assets, and broad input or skating refactors.
- A user-positionable, floating, or dynamically relocating joystick mode.
