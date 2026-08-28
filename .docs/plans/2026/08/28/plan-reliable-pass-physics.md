# Reliable Pass Physics Plan

## Goal

Make normal passes fast, predictable, and distance-aware while preserving physical flight, collision-based failure, and the existing possession-driven human control transfer.

## Current Context

- `PassController` selects a teammate and leads their stick, but launches every pass through `PuckController.Release` at one serialized `passSpeed`.
- `PuckController` applies the same Rigidbody damping to every free puck and only exposes ordinary `TryClaim`, which rejects puck speeds above the stick's serialized maximum claim speed.
- `StickPuckInteraction` owns each skater's configurable control point and ordinary claim radius/speed.
- `PlayerControlManager` already transfers control when `PuckController.CarrierChanged` establishes a human-team receiver as carrier.
- `PrototypeArenaSmokeCheck` already simulates stationary, moving, and obstructed passes and is the narrowest executable gameplay verification surface.

## Decisions

- Add piecewise linear short/medium/long distance-to-speed tuning to `PassController`; the values remain serialized and tunable on the actual rink scale.
- Add a focused `PassReceivingZone` component to each skater. It owns serialized reception radius and entry speed, while `PuckController` owns the active intended-pass state and invokes the zone only after the puck physically enters it.
- Use direct velocity assignment for the controlled pass launch, matching the requested launch contract and avoiding dependence on an accumulated force step. Continue ordinary Rigidbody simulation immediately afterward.
- Inside the local reception zone, redirect and reduce velocity before establishing the existing carrier relationship. This is the only pass assistance; no mid-flight homing or guaranteed catch is added.
- Clear intended-pass state on any claim, force release, save, reset, or replacement release so stale receivers cannot capture unrelated puck motion.
- Preserve `Release` for shots and generic releases. Do not add feature flags, environment variables, fallback paths, compatibility layers, or unrelated refactors.

## Phased Tasks

### Phase 1 - Lock the pass and possession boundaries

- [x] Confirm `PassController`, `PuckController`, `StickPuckInteraction`, `PlayerController`, and `PlayerControlManager` ownership boundaries and preserve generic shot/loose-puck behavior.
- [x] Confirm `PrototypeArenaSmokeCheck` can deterministically cover distance-scaled velocity, receiver capture, automatic control transfer, and interception.
- [x] Record manual-feel-only tuning as a follow-up rather than introducing fixed final pace values.

### Phase 2 - Add configurable receiver capture

- [x] Add `PassReceivingZone` under `Assets/_Project/Scripts/Puck` with serialized reception radius and reception entry speed plus a bounded intended-pass capture operation.
- [x] Update `PlayerController` composition and configuration so every skater exposes a configured receiving zone without changing ordinary stick claims.
- [x] Update `PuckController` with intended-pass launch state, controlled initial velocity, local-zone reception, and state cleanup across all existing release/reset paths.

### Phase 3 - Implement distance-scaled launch

- [x] Replace `PassController`'s fixed speed with serialized short, medium, and long distances and speeds plus a deterministic piecewise interpolation calculation.
- [x] Calculate the led target point, planar pass distance, launch direction, and distance-appropriate quality-adjusted speed before calling the pass-specific puck launch.
- [x] Preserve imperfect release spread, target recommendation feedback, normal free-flight damping, and collision response outside the reception zone.

### Phase 4 - Extend deterministic gameplay verification

- [x] Update `PrototypeArenaSmokeCheck` tuning assertions to validate configurable monotonic distance-speed behavior and reception settings.
- [x] Extend simulated outcomes to verify short, medium, and long unobstructed reception, moving-target reception, intended receiver possession/control transfer, and an obstructed interception.
- [x] Verify generic `PuckController.Release` still clears pass state and retains the shot/free-release path.

### Phase 5 - Build and evidence

- [x] Run Unity batch compilation or the repository's Phase 1 smoke runner and record its exact pass/failure output.
- [x] Run the static Phase 1 external-service boundary check and confirm no out-of-scope integration was introduced.
- [x] Update `README.md` so architecture/tuning text describes distance-scaled pass launch and local reception assistance accurately.
- [x] Record final verification evidence and mark tasks complete only after the corresponding evidence exists.

## Validation

- Compile and execute the Unity Phase 1 smoke check through Unity `6000.5.9f1`, expecting `PHASE1_PVE_SMOKE_PASS` and no C# compilation errors.
- Run `rg -n -i 'Photon|Fusion|Unity\.Netcode|Relay|Matchmaking|Firebase|AuthenticationService|Lobby' Assets Packages/manifest.json Packages/packages-lock.json ProjectSettings`; expect no matches.
- Inspect the stable diff for unchanged generic shot release semantics and intended-pass state cleanup.
- Manual device/editor feel remains recommended for final tuning because serialized pace values are intentionally not declared final.

Evidence recorded on 2026-08-28:

- Unity Editor menu `IceClash > Run Phase 1 PvE Smoke Check` logged `PHASE1_PVE_SMOKE_PASS` with `shortPassReceived=true`, `mediumPassReceived=true`, `longPassReceived=true`, `movingPassReceived=true`, `passReceptionAutoControl=true`, and `obstructedPassIntercepted=true`.
- The static Phase 1 external-service boundary command returned no matches.
- Unity's refreshed compilation produced no C# compiler errors in `Logs/Editor.log`.

## Rollback / Risk

- A reception radius that is too large can feel magnetic; keep assistance local to the receiver's stick and expose the radius for tuning.
- Intended-receiver eligibility is time-bounded and terminates once an outside-zone puck is clearly moving away, preventing late magnetic reattachment after a defeated pass.
- A reception entry speed that is too high can destabilize carry control; bound it and let the existing carry controller finish attachment.
- High pass speeds can tunnel through colliders; retain `ContinuousDynamic` collision detection and deterministic obstruction coverage.
- Rollback is localized to the new receiving component and the pass-specific launch path; generic releases remain independently callable.
