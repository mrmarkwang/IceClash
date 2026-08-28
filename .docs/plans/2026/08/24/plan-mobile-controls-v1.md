# Mobile Controls V1 - Architecture and Implementation Plan

## Goal

Maintain the completed safe-area-aware Unity UI input layer while placing the virtual stick in a persistent, dedicated lower-left zone inspired by the marked reference. Preserve analog skating, pointer ownership, pass, deke-input, and charged-shot contracts.

## Current Context

- `Assets/_Project/Scenes/PrototypeArena.unity` is the only enabled build scene and is intentionally empty; `PrototypeArenaBootstrap` and `LocalMatchSetup` compose gameplay at runtime.
- `LocalPlayerInput` supplies WASD/gamepad movement, and `PlayerInputController` selects the stronger hardware or `VirtualJoystick` vector before the shared `IPlayerInput` reaches `PlayerController`.
- `PlayerController` delegates movement to `PlayerMovementController`, which clamps analog input and owns camera-relative acceleration, deceleration, reversal braking, momentum, speed, and rotation.
- `VirtualJoystick`, `MobileActionButton`, `MobileControlsBuilder`, and `SafeAreaFitter` provide the Canvas hierarchy, circular generated visuals, dead zone, independent action pointers, and safe-area layout. The joystick currently relocates its hidden base to the first pointer within a broad lower-left area, which conflicts with the newly requested fixed zone.
- `Packages/manifest.json` now declares Unity `6000.5.9f1`'s bundled `com.unity.ugui` `2.5.0`; the refinement needs no package or external-service change.
- `ProjectSettings.asset` now permits both landscape orientations and disables portrait autorotation. Active input handling remains compatible with the Input System.
- The worktree had no source changes before this AP/AR update. Any unrelated changes encountered during implementation must still be preserved, especially in the shared smoke-check path.
- The V1 hierarchy, circular control treatment, and shared input route are implemented and covered by the Phase 1 smoke check; this correction changes only joystick placement and visibility.
- The prior execution record confirms Editor pointer behavior, but true device multi-touch and representative 16:9, 19.5:9, and 20:9 thumb-reach observations remain pending.
- `MobileActionButton` already exposes press, held, and release phases. DEKE intentionally emits only an input/debug signal, while PASS and charged SHOOT already use the shared gameplay input contract.

## Decisions

- Treat the repository's enabled `PrototypeArena` as the requested gameplay scene and preserve runtime composition; do not create a redundant `Game` scene.
- Replace `MobileJoystick` with reusable `VirtualJoystick` pointer-handler UI behavior and replace `ActionButton` with `MobileActionButton`, using per-component pointer IDs so simultaneous touches remain independent.
- Replace `MobileInputSource` with `PlayerInputController` as the single human input abstraction. It will select the stronger hardware or joystick vector, clamp the result, expose PASS/DEKE/SHOOT phase properties, and keep the existing `IPlayerInput` pass/shoot/switch contract intact for existing gameplay and AI.
- Add a focused `MobileControlsBuilder` that creates only the Canvas, safe-area root, requested hierarchy, visuals, EventSystem, and bindings. It will not own movement or gameplay logic.
- Use `CanvasScaler.ScaleWithScreenSize`, a `1920 x 1080` reference, anchored safe-area layout, Unity `Button` color transitions, and a larger SHOOT rect. Placeholder generated sprites/artwork are unnecessary.
- Log action names and raise button events at pointer-down time. Preserve held/released phases so future shot charging does not require changing the mobile button contract.
- Preserve existing pass/shoot gameplay paths already in the repository but add no new puck behavior; DEKE remains input/debug-only.
- Declare the editor-bundled `com.unity.ugui` `2.5.0` package required by the requested Unity UI API; reject third-party UI packages and custom IMGUI fallback controls.
- Restrict autorotation to both landscape orientations in `ProjectSettings.asset`. Do not add feature flags, environment variables, alternate scene paths, compatibility wrappers, or packages.
- Replace the floating joystick with one always-visible base centered in a fixed lower-left hit region, while preserving the translucent circular visuals and three-action layout inspired by the reference.
- Constrain joystick pointer-down handling to its fixed hit region. Do not retain a second floating mode, configuration flag, or touches-anywhere-on-the-left compatibility path.
- Keep interaction geometry separate from presentation: retain generous transparent rectangular hit areas while circular child visuals supply the apparent control shape and pressed feedback.
- Tune serialized or centrally defined layout values only after observing the controls at 16:9, 19.5:9, and 20:9. Do not fork layouts by device model or add per-device feature flags.
- Keep PASS, DEKE, and SHOOT as the V1 action set. Actual deke behavior, additional action buttons, final production art, haptics, and accessibility settings are separate stories.

## Phased Tasks

### Phase 1 - Lock existing movement and input boundaries

- [x] Confirm `LocalPlayerInput.cs`, `PlayerController.cs`, and `PlayerMovementController.cs` keep WASD/gamepad movement and a single analog skating path with magnitude clamping.
- [x] Confirm `PrototypeArena.unity`, `PrototypeArenaBootstrap.cs`, and `LocalMatchSetup.cs` require runtime UI composition in the existing enabled gameplay scene.
- [x] Preserve the user's current uncommitted puck, stick, shot, bootstrap, and smoke-check edits while limiting this story to control/input/layout changes.

### Phase 2 - Implement reusable pointer controls

- [x] Add Unity `6000.5.9f1`'s bundled `com.unity.ugui` `2.5.0` dependency to `Packages/manifest.json` and resolve `Packages/packages-lock.json` so EventSystem and Unity UI APIs compile.
- [x] Add `VirtualJoystick.cs` as an `IPointerDownHandler`, `IDragHandler`, and `IPointerUpHandler` that captures one pointer, clamps the handle, applies configurable dead-zone remapping, and resets direction on release.
- [x] Add `MobileActionButton.cs` with captured-pointer press/hold/release phases, pointer-down action events and debug output, and Editor mouse compatibility through the EventSystem.
- [x] Remove the obsolete fixed-polling IMGUI `MobileJoystick.cs` and `ActionButton.cs` implementations and their metadata after all references migrate.

### Phase 3 - Build shared input and Unity UI composition

- [x] Add `PlayerInputController.cs` to choose and clamp the stronger hardware/joystick movement source and expose PASS, DEKE, SHOOT, and existing switch phases without duplicating skating.
- [x] Add `MobileControlsBuilder.cs` to create the `Canvas/MobileControls` safe-area hierarchy, requested joystick/action descendants, `CanvasScaler` configuration, EventSystem input module, placeholder visuals, and larger SHOOT layout.
- [x] Update `LocalMatchSetup.cs` to request the new bindings from `MobileControlsBuilder`, configure `PlayerInputController`, retain HUD creation, and leave player movement routing unchanged.
- [x] Update `PlayerSwitchController.cs` and any concrete human-input type references so control transfer continues to route the same shared player-input instance.

### Phase 4 - Landscape configuration and regression checks

- [x] Update `ProjectSettings/ProjectSettings.asset` to allow only landscape-left and landscape-right autorotation while retaining current Input System settings.
- [x] Extend `PrototypeArenaSmokeCheck.cs` to verify the Canvas hierarchy, scaler settings, safe-area component, PASS/DEKE/SHOOT sizes and bindings, shared player-input routing, clamped source selection, and dead-zone behavior without discarding existing smoke assertions.
- [x] Update existing control-related documentation only where stale names or the old PASS/SHOOT/SWITCH layout would contradict the new mobile-control surface.

### Phase 5 - Verification and status

- [x] Compile with the already-open Unity `6000.5.9f1` Editor and execute the existing Editor-driven smoke validation; record zero compiler errors and the updated smoke pass marker.
- [x] Run `git diff --check` and focused static searches confirming no direct transform movement in joystick code, no duplicate mobile/desktop skating controller, and no stale `MobileJoystick`, `ActionButton`, or `MobileInputSource` source references.
- [x] Execute the Editor-observable scenarios in `.docs/tests/test-mobile-controls-v1.md` where automation permits, and record real-device multi-touch/aspect-ratio checks as pending if no device or simulator is available.
- [x] Mark every plan task complete only after its code, documentation, or verification evidence exists.

### Phase 6 - Lock refinement baselines

- [ ] Capture `PrototypeArena` screenshots at 16:9, 19.5:9, and 20:9 landscape Game views and record joystick/button bounds, overlap, safe-area containment, ice visibility, and right-thumb reach in `.docs/tests/test-mobile-controls-v1.md`.
- [x] Inspect `MobileControlsBuilder.cs`, `VirtualJoystick.cs`, `MobileActionButton.cs`, and `SafeAreaFitter.cs` to identify which values control hit geometry, visible geometry, dead zone, opacity, and pressed feedback without changing `PlayerInputController` or skating behavior.
- [x] Record the retained action set—PASS, debug-only DEKE, and held/released SHOOT—and confirm that no SWITCH, sprint, checking, special-ability, or second aiming control is introduced.

### Phase 7 - Separate interaction and presentation

- [x] Update `MobileControlsBuilder.cs` so joystick and action hit areas remain generous and transparent while dedicated child `RectTransform`/`Image` objects render the circular rings, handle, icons or compact labels, and primary SHOOT emphasis.
- [x] Update `MobileActionButton.cs` only as needed to drive immediate normal, held, and released visual states without changing its pointer ownership, buffered input phases, label contract, or gameplay routing.
- [x] Keep the joystick base visible at its fixed lower-left origin and ensure the base, travel boundary, and centered handle remain legible before, during, and after input over light and dark rink content.

### Phase 8 - Tune landscape ergonomics

- [ ] Tune the layout constants in `MobileControlsBuilder.cs` from the captured baselines so the joystick and all action hit areas stay inside `SafeAreaFitter`, do not overlap, and can be reached without covering the controlled skater in representative 16:9, 19.5:9, and 20:9 landscape views.
- [x] Tune joystick radius and dead zone only if observed movement requires it, preserving full analog magnitude, release-to-zero, and the existing single movement route through `PlayerInputController`.
- [x] Confirm the visible SHOOT control remains the primary and largest action, PASS and DEKE remain distinguishable secondary controls, and labels/icons remain readable without importing third-party or copied reference assets.

### Phase 9 - Regression and device validation

- [x] Extend `PrototypeArenaSmokeCheck.cs` with structural assertions for separated hit/visual children, circular visual sizing, SHOOT prominence, safe-area ownership, and unchanged bindings without attempting to automate subjective thumb feel.
- [x] Run `IceClash > Run Phase 1 PvE Smoke Check` in Unity `6000.5.9f1` and record zero compiler errors plus `PHASE1_PVE_SMOKE_PASS` in `.docs/tests/test-mobile-controls-v1.md`.
- [ ] Execute the updated Game-view scenarios in `.docs/tests/test-mobile-controls-v1.md` at 16:9, 19.5:9, and 20:9, recording screenshots and observed readability, overlap, camera occlusion, and simultaneous mouse/touch input evidence.
- [x] Run one physical-device two-thumb session when a device is available; record joystick-plus-action multi-touch, safe-area reachability in both landscape orientations, missed-touch observations, FPS, and thermals, or retain an explicit pending-device release risk when hardware is unavailable.
- [x] Run `git diff --check` and focused `rg` searches confirming there is still one `PlayerMovementController`, no direct joystick-driven player transform movement, and no new package, device-specific layout fork, or out-of-scope action control.

### Phase 10 - Fix the joystick in the marked lower-left zone

- [x] Update `MobileControlsBuilder.cs` so `JoystickArea` is a bounded square hit region anchored inside the lower-left safe area, with `JoystickBackground` centered and visible at rest rather than spanning a broad floating-input region.
- [x] Update `VirtualJoystick.cs` so pointer down and drag move only `JoystickHandle`, pointer release restores centered zero input, and no input phase changes or hides `JoystickBackground` or its fixed origin.
- [x] Extend `PrototypeArenaSmokeCheck.cs` to assert the fixed joystick area's bounded square geometry, persistent visible base, stationary origin across pointer input, clamped analog output, independent pointer ownership, and release-to-center behavior.
- [x] Update `README.md` and `.docs/tests/test-mobile-controls-v1.md` so control descriptions and E2E scenarios no longer claim floating activation or a hidden base.
- [x] Compile in Unity `6000.5.9f1`, run `IceClash > Run Phase 1 PvE Smoke Check`, and record `fixedJoystick=true` plus zero compiler errors.
- [x] Capture a 16:9 Game-view screenshot showing the visible joystick in the intended lower-left zone without covering the controlled skater, and record unavailable wider-aspect/device evidence truthfully.
- [x] Run `git diff --check` and focused searches confirming `VirtualJoystick` does not reposition/hide its background, UI code does not move the player transform, and the fixed joystick introduces no alternate mode or new dependency.

## Validation

- Compile in the already-open Unity `6000.5.9f1` Editor, or use batch compilation when the project is not open elsewhere. Expected evidence: a successful Tundra build and no `CS` compiler errors.
- Run the existing `IceClash/Run Phase 1 PvE Smoke Check` flow or its batch-compatible validation entry point. Expected evidence: the updated `PHASE1_PVE_SMOKE_PASS` marker includes the mobile-control invariants.
- Run `git diff --check`. Expected evidence: exit `0` with no whitespace errors.
- Run focused `rg` searches over `Assets/_Project/Scripts` for obsolete control class names, direct joystick transform manipulation, and duplicated skating classes. Expected evidence: only the single `PlayerMovementController` remains and no obsolete source reference survives.
- In a 16:9, 19.5:9, and 20:9 landscape Game view or device, observe the persistent fixed joystick, separate action hit areas, larger SHOOT, safe-area containment, visual press feedback, simultaneous joystick/action input, clean release-to-center/zero, stable camera, and no recurring exceptions.
- For the refinement extension, capture before/after Game-view screenshots at 16:9, 19.5:9, and 20:9 and record them in `.docs/tests/test-mobile-controls-v1.md`. Expected evidence: circular visible controls remain readable against the rink, transparent hit regions do not overlap, and no control obscures the controlled skater during ordinary camera framing.
- On a physical target device when available, perform a two-thumb session in both landscape orientations. Expected evidence: continuous joystick movement survives simultaneous PASS, DEKE, and charged SHOOT interactions with no pointer stealing or recurring missed touches. Lack of hardware must be reported as a pending release risk rather than inferred from Editor mouse testing.

## Rollback / Risk

- Runtime UI event delivery depends on the editor-bundled uGUI package plus a configured EventSystem and Input System UI module. Pin the Unity-matched built-in package, build and configure the event objects explicitly when absent, then verify the resulting hierarchy and bindings in the smoke check.
- Pointer callbacks can occur in different script-update order than gameplay reads. Store press/release by frame and held state by captured pointer rather than clearing transient phases in an arbitrary `Update`.
- Safe-area coordinates vary by device and orientation. Convert `Screen.safeArea` to normalized anchors on startup and when screen dimensions or safe area change.
- Existing PASS/SHOOT gameplay already consumes the human input contract. Preserve those paths while adding DEKE only to the concrete human input layer, avoiding a broad AI/public-contract migration.
- `LocalMatchSetup`, `PrototypeArenaBootstrap`, and `PrototypeArenaSmokeCheck` overlap the user's uncommitted work. Patch only control-related regions and retain all puck-tuning changes.
- Refinement rollback is file-local: revert the new visual children, styling values, layout tuning, and matching smoke assertions while retaining the working V1 input components, `BuildInputAndHud` wiring, landscape settings, and existing tests. No data, dependency, backend, or migration cleanup is required.
- Circular art can visually imply a smaller target than the actual hit region. Keep raycast ownership on the larger parent hit area, disable raycasts on decorative children, and verify that adjacent hit rectangles do not overlap.
- A device-free Editor pass cannot establish true multi-touch reliability, hand comfort, thermal behavior, or notch reachability. The refinement can be implemented and Editor-validated without hardware, but it should not be called device-validated until the physical-device scenario is recorded.
- The reference is directional visual guidance only. Use project-owned generated UI shapes and labels; do not copy logos, screenshots, or branded artwork from the reference.
- A persistent joystick adds visual coverage even when idle. Keep its zone near the lower-left safe edge, use the existing translucent treatment, and validate that ordinary camera framing keeps the controlled skater readable.
- A bounded fixed hit region is less forgiving than the former broad floating area. Size the hit region larger than the visible ring, keep it inside the safe area, and preserve pointer capture after a valid touch begins even when the drag moves beyond the original rectangle.
