# Mobile Controls V1 - E2E Scenarios

Record Unity version, Game view or device aspect ratio, and pass/fail evidence. Automated structural evidence may satisfy hierarchy and routing assertions; touch reachability and true multi-touch require device or simulator observation.

## Scenario: Preserve desktop skating

Given `PrototypeArena` is open in Unity `6000.5.9f1` and Play Mode is active

When the tester presses W, S, A, and D individually and diagonally, then releases and reverses direction

Then the controlled skater uses the existing acceleration, deceleration, momentum, speed limit, and turning behavior

And the camera remains stable

And movement input never exceeds magnitude one

## Scenario: Activate and release the floating joystick

Given a landscape Game view and no active joystick pointer

When the tester presses inside the lower-left `JoystickArea`

Then `JoystickBackground` appears centered at that pointer within the usable area

When the tester drags from center through partial, full-cardinal, and full-diagonal positions

Then `JoystickHandle` follows within its radius and the analog direction changes smoothly up to magnitude one

And movement inside the configured dead zone remains zero while values beyond it are smoothly remapped

When the activating pointer releases

Then direction returns to zero and the background/handle reset or hide

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

## Evidence to record

- Unity compile output with zero compiler errors.
- Updated `PHASE1_PVE_SMOKE_PASS` output proving hierarchy, binding, dead-zone, clamping, landscape, and single-input-route invariants.
- Editor observations for WASD, mouse joystick drag, action clicks, release-to-zero, visual feedback, debug logs, and camera stability.
- Simulator/device observations for real multi-touch and 16:9, 19.5:9, and 20:9 safe-area reachability; otherwise an explicit pending-device note.

## Execution record - 2026-08-24

- Unity `6000.5.9f1` resolved the bundled uGUI `2.5.0` package and compiled `Assembly-CSharp` successfully with no compiler errors.
- `IceClash > Run Phase 1 PvE Smoke Check` passed with `unityUI=true`, `safeArea=true`, `referenceResolution=1920x1080`, `deadZone=true`, `analog=true`, `independentPointers=true`, and `movementClamped=true`.
- The live landscape Game view showed PASS, DEKE, and the larger SHOOT layout without overlap; the Unity Console showed zero warnings/errors.
- Editor pointer clicks produced the expected `PASS`, `DEKE`, and `SHOOT` debug messages.
- A real mobile device/simulator was not attached, so true device multi-touch and separate 16:9, 19.5:9, and 20:9 safe-area observations remain pending manual validation.
