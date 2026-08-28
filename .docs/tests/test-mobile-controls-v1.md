# Mobile Controls V1 - E2E Scenarios

Record Unity version, Game view or device aspect ratio, and pass/fail evidence. Automated structural evidence may satisfy hierarchy and routing assertions; touch reachability and true multi-touch require device or simulator observation.

## Scenario: Preserve desktop skating

Given `PrototypeArena` is open in Unity `6000.5.9f1` and Play Mode is active

When the tester presses W, S, A, and D individually and diagonally, then releases and reverses direction

Then the controlled skater uses the existing acceleration, deceleration, momentum, speed limit, and turning behavior

And the camera remains stable

And movement input never exceeds magnitude one

## Scenario: Use the fixed lower-left joystick

Given a landscape Game view and no active joystick pointer

Then `JoystickBackground` and its centered `JoystickHandle` are already visible inside a dedicated lower-left control zone

When the tester presses and drags inside `JoystickArea`

Then `JoystickBackground` remains at its fixed origin while `JoystickHandle` follows through partial, full-cardinal, and full-diagonal positions within its radius

Then `JoystickHandle` follows within its radius and the analog direction changes smoothly up to magnitude one

And movement inside the configured dead zone remains zero while values beyond it are smoothly remapped

When the activating pointer releases

Then direction returns to zero, the handle returns to center, and the background remains visible at the same origin

When the tester presses elsewhere on the left half outside `JoystickArea`

Then the joystick does not activate or relocate

## Scenario: Keep joystick pointer ownership

Given one finger is holding and dragging the joystick

When a second finger touches elsewhere or presses an action button

Then the joystick continues tracking only its original finger

And the second finger cannot move or release the joystick

## Scenario: Use action controls independently

Given the mobile overlay is visible

When the tester presses and releases PASS, DEKE, and SHOOT with touch or Editor mouse input

Then each button immediately shows its pressed visual state

And the corresponding `PASS`, `DEKE`, or `SHOOT` debug message appears once on pointer down

And each control exposes pressed, held, and released phases for input consumers

And SHOOT is visibly larger than PASS and DEKE

## Scenario: Exercise multi-touch action combinations

Given one finger is holding the joystick away from center

When a second finger presses PASS, then DEKE, then SHOOT in separate trials

Then movement remains non-zero throughout each press

And the corresponding action signal and debug message occur without stealing joystick ownership

## Scenario: Validate landscape safe-area layouts

Given the Game view or device is set to 16:9, 19.5:9, and 20:9 landscape layouts

When the mobile overlay is displayed at each ratio and both landscape orientations

Then the joystick and all three action buttons remain inside the safe area, reachable, non-overlapping, and proportionally stable

And the Canvas scaler reports `Scale With Screen Size` with a `1920 x 1080` reference resolution

And portrait autorotation is unavailable

## Scenario: Read the controls over live play

Given the refined control overlay is displayed over active `PrototypeArena` gameplay

When the camera follows the controlled skater across bright ice, rink markings, players, and goals at 16:9, 19.5:9, and 20:9

Then the joystick base, handle, PASS, DEKE, and SHOOT visuals remain readable without opaque panels covering important play

And the visible controls use a coherent translucent circular treatment with SHOOT clearly larger than PASS and DEKE

And the transparent interaction rectangles remain inside the safe area, do not overlap each other, and are larger than or equal to their visible circular controls

## Scenario: Preserve gameplay while restyling

Given the refined controls are active and the controlled skater can move, pass, and shoot

When the tester performs partial and full joystick movement, taps PASS and DEKE, and holds then releases SHOOT

Then movement magnitude, dead-zone behavior, pass input, debug-only DEKE input, shot charging, pointer ownership, and release-to-zero match the V1 behavior

And no SWITCH, sprint, checking, special-ability, second-stick aiming, or separate shot-type control appears

## Scenario: Check two-thumb reach on a physical device

Given a target phone is running the prototype in landscape-left and landscape-right

When the tester skates continuously with the left thumb while repeatedly tapping PASS and DEKE and charging/releasing SHOOT with the right thumb

Then the joystick remains active, each action responds without stealing the joystick pointer, and the tester can reach every control without changing grip

And no control enters a notch, rounded-corner, or gesture-reserved area

And the session record includes missed-touch observations, FPS, and thermal notes

## Evidence to record

- Unity compile output with zero compiler errors.
- Updated `PHASE1_PVE_SMOKE_PASS` output proving hierarchy, binding, dead-zone, clamping, landscape, and single-input-route invariants.
- Editor observations for WASD, mouse joystick drag, action clicks, release-to-zero, visual feedback, debug logs, and camera stability.
- Simulator/device observations for real multi-touch and 16:9, 19.5:9, and 20:9 safe-area reachability; otherwise an explicit pending-device note.
- Before/after screenshots at 16:9, 19.5:9, and 20:9 showing control readability, visible circular geometry, non-overlapping hit regions, safe-area containment, and ordinary camera framing.
- Physical-device observations in both landscape orientations remain required before claiming device validation; Editor mouse testing is not equivalent evidence.

## Execution record - 2026-08-24

- Unity `6000.5.9f1` resolved the bundled uGUI `2.5.0` package and compiled `Assembly-CSharp` successfully with no compiler errors.
- `IceClash > Run Phase 1 PvE Smoke Check` passed with `unityUI=true`, `safeArea=true`, `referenceResolution=1920x1080`, `deadZone=true`, `analog=true`, `independentPointers=true`, and `movementClamped=true`.
- The live landscape Game view showed PASS, DEKE, and the larger SHOOT layout without overlap; the Unity Console showed zero warnings/errors.
- Editor pointer clicks produced the expected `PASS`, `DEKE`, and `SHOOT` debug messages.
- A real mobile device/simulator was not attached, so true device multi-touch and separate 16:9, 19.5:9, and 20:9 safe-area observations remain pending manual validation.

## Refinement execution record - 2026-08-27

- Unity `6000.5.9f1` compiled the refined runtime-generated controls with zero compiler errors.
- `IceClash > Run Phase 1 PvE Smoke Check` passed with `circularControls=true`, `separateHitVisuals=true`, `nonOverlappingActions=true`, `safeArea=true`, `independentPointers=true`, and the existing movement/pass/charged-shot invariants.
- The [16:9 Game-view capture](../evidence/mobile-controls-v1/refined-16x9.jpg) shows translucent circular PASS, DEKE, and larger SHOOT visuals over live rink play without overlap; their transparent parent hit rectangles remain larger than their decorative circles by structural assertion.
- The existing 0.12 joystick dead zone and 130-reference-pixel radius were retained because the automated analog/release assertions passed and no observed Editor regression justified changing movement feel during a presentation-only refinement.
- Static checks found one `PlayerMovementController`, no player-transform movement in UI scripts, no added package, and no SWITCH, sprint, checking, special-ability, or second-stick control.
- No archived pre-refinement screenshot was available; the 2026-08-24 execution record is the baseline description rather than invented visual evidence.
- Custom 19.5:9 and 20:9 Game-view captures and a physical-device two-thumb session were not available in this run. Safe-area reachability in those layouts, real multi-touch, missed touches, FPS, and thermals remain an explicit release risk and are not claimed as validated.

## Fixed-joystick execution record - 2026-08-27

- Unity `6000.5.9f1` compiled the fixed-zone change with zero compiler errors.
- `IceClash > Run Phase 1 PvE Smoke Check` passed with `controls=FIXED_JOYSTICK_PASS_DEKE_SHOOT`, `fixedJoystick=true`, `persistentJoystick=true`, `independentPointers=true`, `deadZone=true`, and the existing movement/pass/charged-shot invariants.
- The [fixed-joystick 16:9 capture](../evidence/mobile-controls-v1/fixed-joystick-16x9.jpg) shows the base and centered handle visible at rest in the lower-left control zone while PASS, DEKE, and SHOOT remain separate on the right.
- Visual review moved the control higher and inward after the first capture; its final `390, 430` reference-pixel center occupies the marked lower-left play area without covering the controlled skater.
- Structural checks verified a safe-area-contained 360 x 360 reference-pixel joystick hit region around the 260 x 260 visible base, a stationary base origin during simulated drag, clamped analog output, and handle/direction reset on release.
- Static searches found no `VirtualJoystick` path that assigns the background from the pointer position or hides it, no direct player-transform movement in UI code, no alternate floating mode, and no dependency change.
- Custom 19.5:9 and 20:9 Game-view captures and a physical-device two-thumb session remain unavailable; those wider-layout, reachability, FPS, thermal, and true multi-touch checks are not claimed as complete.
