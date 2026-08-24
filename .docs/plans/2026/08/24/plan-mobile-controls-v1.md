# Mobile Controls V1 - Architecture and Implementation Plan

## Goal

Replace the current fixed IMGUI control overlay with a safe-area-aware Unity UI control layer that supplies floating analog joystick and PASS/DEKE/SHOOT input through the existing player/skating path, while preserving desktop input and all current movement physics.

## Current Context

- `Assets/_Project/Scenes/PrototypeArena.unity` is the only enabled build scene and is intentionally empty; `PrototypeArenaBootstrap` and `LocalMatchSetup` compose gameplay at runtime.
- `LocalPlayerInput` already supplies WASD/gamepad movement, and `MobileInputSource` currently chooses the stronger of hardware and touch movement before the shared `IPlayerInput` reaches `PlayerController`.
- `PlayerController` delegates movement to `PlayerMovementController`, which already clamps analog input and owns camera-relative acceleration, deceleration, reversal braking, momentum, speed, and rotation.
- `MobileJoystick` and `ActionButton` currently poll raw Input System devices and draw IMGUI controls at normalized fixed positions. They do not create the requested Canvas hierarchy, floating origin, dead zone, DEKE input, or safe-area layout.
- The manifest includes only the low-level `com.unity.modules.ui`; the required `CanvasScaler`, EventSystem, and uGUI control assemblies come from Unity `6000.5.9f1`'s bundled `com.unity.ugui` version `2.5.0`, which is not yet declared.
- `ProjectSettings.asset` currently uses auto-rotation with portrait orientations allowed. Active input handling is already compatible with the Input System.
- Existing uncommitted edits in puck, stick, shooting, bootstrap, and smoke-check files belong to the user and must be retained while overlapping files are patched surgically.

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

## Phased Tasks

### Phase 1 - Lock existing movement and input boundaries

- [x] Confirm `LocalPlayerInput.cs`, `PlayerController.cs`, and `PlayerMovementController.cs` keep WASD/gamepad movement and a single analog skating path with magnitude clamping.
- [x] Confirm `PrototypeArena.unity`, `PrototypeArenaBootstrap.cs`, and `LocalMatchSetup.cs` require runtime UI composition in the existing enabled gameplay scene.
- [x] Preserve the user's current uncommitted puck, stick, shot, bootstrap, and smoke-check edits while limiting this story to control/input/layout changes.

### Phase 2 - Implement reusable pointer controls

- [x] Add Unity `6000.5.9f1`'s bundled `com.unity.ugui` `2.5.0` dependency to `Packages/manifest.json` and resolve `Packages/packages-lock.json` so EventSystem and Unity UI APIs compile.
- [x] Add `VirtualJoystick.cs` as an `IPointerDownHandler`, `IDragHandler`, and `IPointerUpHandler` that captures one pointer, floats its base within `JoystickArea`, clamps the handle, applies configurable dead-zone remapping, and resets/hides on release.
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

## Validation

- Compile in the already-open Unity `6000.5.9f1` Editor, or use batch compilation when the project is not open elsewhere. Expected evidence: a successful Tundra build and no `CS` compiler errors.
- Run the existing `IceClash/Run Phase 1 PvE Smoke Check` flow or its batch-compatible validation entry point. Expected evidence: the updated `PHASE1_PVE_SMOKE_PASS` marker includes the mobile-control invariants.
- Run `git diff --check`. Expected evidence: exit `0` with no whitespace errors.
- Run focused `rg` searches over `Assets/_Project/Scripts` for obsolete control class names, direct joystick transform manipulation, and duplicated skating classes. Expected evidence: only the single `PlayerMovementController` remains and no obsolete source reference survives.
- In a 16:9, 19.5:9, and 20:9 landscape Game view or device, observe floating joystick reachability, separate action hit areas, larger SHOOT, safe-area containment, visual press feedback, simultaneous joystick/action input, clean release-to-zero, stable camera, and no recurring exceptions.

## Rollback / Risk

- Runtime UI event delivery depends on the editor-bundled uGUI package plus a configured EventSystem and Input System UI module. Pin the Unity-matched built-in package, build and configure the event objects explicitly when absent, then verify the resulting hierarchy and bindings in the smoke check.
- Pointer callbacks can occur in different script-update order than gameplay reads. Store press/release by frame and held state by captured pointer rather than clearing transient phases in an arbitrary `Update`.
- Safe-area coordinates vary by device and orientation. Convert `Screen.safeArea` to normalized anchors on startup and when screen dimensions or safe area change.
- Existing PASS/SHOOT gameplay already consumes the human input contract. Preserve those paths while adding DEKE only to the concrete human input layer, avoiding a broad AI/public-contract migration.
- `LocalMatchSetup`, `PrototypeArenaBootstrap`, and `PrototypeArenaSmokeCheck` overlap the user's uncommitted work. Patch only control-related regions and retain all puck-tuning changes.
- Rollback is file-local: restore the old three input/UI scripts and the prior `BuildInputAndHud` wiring, revert the landscape-only settings, and remove new UI/input scripts and tests. No data, dependency, backend, or migration cleanup is required.
