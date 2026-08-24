# Local PvE Hockey Prototype — Architecture and Development Plan

## Goal

Turn the current local 2v2 foundation into a complete, replayable three-skaters-plus-goalie PvE hockey match with one-stick mobile movement and exactly PASS, SHOOT, and SWITCH actions. Preserve an extensible local architecture while explicitly excluding every network or service dependency.

## Current Context

- The project uses Unity `6000.5.9f1`, Input System `1.19.0`, a runtime-built `PrototypeArena`, one reusable `Skater.prefab`, and no assembly definitions or automated Unity Test Framework suite.
- `LocalMatchSetup` currently spawns two Blue and two Red skaters, with one `LocalPlayerInput` and three `AiPlayerInput` sources. There are no goalies, goals/triggers, match state owner, HUD, touch UI, switching, or reset orchestration.
- `PlayerController` currently combines immediate movement with puck claim, nearest-player passing, fixed-power shooting, and checking/sprint behavior. This conflicts with the Phase 1 controls and modularity requirements.
- `PuckController` already keeps the puck independent and force-steers it while possessed. That physics boundary will be retained while claim/release/reset hooks and stick interaction move into focused components.
- `ElevatedFollowCamera` provides a useful prototype baseline but cannot retarget through a control router or frame the puck dynamically enough for switching.
- The existing uncommitted REQ/AP/E2E edits attempted to pivot the project to online PvP. This plan supersedes that work and adds no networking packages, services, abstractions, or test harnesses.
- The generated arena and empty serialized scene are intentionally suitable for placeholder-first development. Runtime construction will continue so smoke verification can validate the whole vertical slice without hand-authored scene dependencies.

## Decisions

- Keep `PrototypeArenaBootstrap` as a composition root for placeholder geometry only. Runtime game rules live in focused components created/configured by `LocalMatchSetup`.
- Use `IPlayerInput` as the shared input seam with movement isolated from a single PASS press signal, Shoot-held/released, and Switch. Touch, keyboard/gamepad, and AI all use that contract; sprint/check inputs are removed.
- Make `PlayerController` the skater identity/composition root. `PlayerMovementController` owns locomotion; `StickPuckInteraction` owns claim/control-point behavior; `PassController` and `ShootController` own actions. No gameplay feature is hidden behind a network-ready abstraction.
- Use count-driven team definitions and spawn records in `LocalMatchSetup`, with three explicit formation slots now. Extending the arrays later supports five skaters without changing puck, match, action, camera, or UI contracts.
- Use a direct, allocation-conscious state machine in `HockeyPlayerAI`. Formation/home slots provide structure; state decisions use puck carrier, team possession, distance, threat, and pass intent. EASY/NORMAL profiles alter reaction, movement scale, pass error, and shot error.
- Use one `PlayerSwitchController` to own the controlled Blue skater, retarget one persistent local input source through a proxy, enable AI on all non-controlled Blue skaters, drive the marker, and notify the camera. A score margin prevents rapid flicker.
- Keep the puck as an independent Rigidbody. Stick control applies forces toward a slightly animated lateral control point. Passes/shots set velocity and reclaim locks; no parenting, kinematic carry mode, or teleport-follow is introduced.
- `PassTargetSelector` continuously recommends one teammate for the human puck carrier using facing plus tactical weights. `PassController` renders a dotted projected path and releases one imperfect, interceptable physics pass on a simple PASS tap. PASS drag targeting and open-space dump gestures are removed.
- Held shooting records charge time and releases between configured minimum/maximum power. Aim uses player facing and goal assistance without reading Move, with bounded random spread supplied by the acting player's difficulty/input profile.
- Implement goalies as dedicated agents, not skaters. They move laterally around crease anchors and apply bounded save deflections/covers to nearby incoming pucks.
- Implement `MatchController` as the only clock/score/state/reset owner and `GoalTrigger` as a one-shot event source. Faceoff is a timed reset phase; overtime and advanced draw mechanics are excluded.
- Build the landscape Canvas, joystick, action buttons, marker, score/timer, goal message, and result panel at runtime. `MobileJoystick` supports pointer drag; `ActionButton` tracks press/release for charged shots; UI remains minimal.
- Replace the old elevated camera with `HockeyCameraController`, retaining a fixed play-direction orientation and varying focus/distance modestly rather than orbiting.
- Validate with C# compilation, an expanded Editor-driven Play Mode smoke runner, static absence checks for forbidden networking/service references, and a manual E2E match scenario. Device execution may remain a documented manual check when no phone/emulator is attached.
- Reject NavMesh, complex behavior trees, advanced animation, separate offense/defense buttons, hard difficulty, networking packages, service setup, feature flags, compatibility gameplay paths, and broad asset-production work.

## Phased Tasks

### Phase 1 - Lock contracts and locomotion

- [x] Update `GameplayContracts.cs` so input exposes Move, Pass, Shoot-held/released, and Switch only, and add local match/player events needed for decoupled selection and HUD updates.
- [x] Add `PlayerMovementController.cs` with camera-relative analog skating, acceleration, deceleration, momentum, turn response, reset support, and Inspector tuning.
- [x] Refactor `PlayerController.cs` into identity/composition/action orchestration that delegates locomotion, stick interaction, passing, and shooting without retaining sprint/check implementation.
- [x] Update `LocalPlayerInput.cs` for keyboard/gamepad Phase 1 controls and a bindable touch-input source while preserving one input contract.

### Phase 2 - Puck possession, passing, and shooting

- [x] Extend `PuckController.cs` with safe claim/release/reset/save hooks, carrier change events, bounded carry physics, and reclaim/interception behavior while keeping the Rigidbody independent.
- [x] Add `StickPuckInteraction.cs` so possession claims and a subtly moving stick-control point are separate from movement and action logic.
- [x] Add the initial `PassController.cs` baseline with weighted target selection, defender/open-lane evaluation, lead calculation, imperfection, cooldown, and interceptable release; Phase 2A replaces its temporary movement-coupled intent.
- [x] Add the initial `ShootController.cs` baseline with hold/release charge, bounded power/spread, cooldown, and reset behavior; Phase 2A removes its temporary movement-coupled aim.

### Phase 2A - Correct independent action targeting

- [x] Simplify `GameplayContracts.cs`, `LocalPlayerInput.cs`, `MobileInputSource.cs`, and `HockeyPlayerAI.cs` back to one movement-independent PASS press signal while preserving Move exclusively for locomotion.
- [x] Remove `PassInputController.cs` and its runtime wiring so no PASS drag threshold, direction, release, or open-space dump gesture remains.
- [x] Simplify `PassTargetSelector.cs` to continuously recommend one teammate using facing, range, openness, distance, lane/interception risk, and offensive progress without directional gesture input.
- [x] Refactor `PassController.cs` to maintain the recommendation during human possession, render a subtle dotted projected path, release toward the current recommendation on tap, and clear recommendation feedback when possession ends.
- [x] Preserve bounded release error, target lead, independent puck physics, reclaim delay, and interception so a recommended pass is not guaranteed to arrive.
- [x] Keep `PlayerController.cs` and `ShootController.cs` action paths movement-independent and remove obsolete drag feedback state or APIs.

### Phase 3 - Selection and team AI

- [x] Add `PlayerSwitchController.cs` to score eligible Blue skaters for defending/attacking usefulness, transfer the shared human input, toggle teammate AI, update the controlled marker, and emit stable selection events.
- [x] Add `HockeyAIStateMachine.cs` with the eight required states and explicit transition storage.
- [x] Add `HockeyPlayerAI.cs` with count-independent formation support, offense/defense/chase/receive/shoot decisions, shared pass/shoot input pulses, and EASY/NORMAL difficulty tuning.
- [x] Add `AIFormationController.cs` to expose team-relative formation/home positions and useful offensive/defensive slots for any configured skater count.
- [x] Remove `AiPlayerInput.cs` after all AI-controlled skaters use `HockeyPlayerAI` through the shared input path.

### Phase 4 - Goalies, match flow, and roster

- [x] Add `HockeyGoalieAI.cs` with crease anchoring, lateral puck tracking, incoming-shot reaction, bounded save/rebound behavior, and reset support.
- [x] Add `GoalTrigger.cs`, `FaceoffController.cs`, and `MatchController.cs` for single-count goals, clock/state transitions, full actor/puck reset, faceoff delay, and final result.
- [x] Refactor `LocalMatchSetup.cs` to build three skaters and one goalie per team from count-driven formation records, register reset positions, and expose controlled/AI actors to match systems.
- [x] Update `GameplayData.cs` snapshots for the new match states, three-skater teams, scores, controlled-player identity, and goalie-independent roster model.

### Phase 5 - Camera, mobile UI, and arena composition

- [x] Add `HockeyCameraController.cs` with controlled-player retargeting, puck-biased framing, stable goal direction, smoothing, and reset behavior; remove `ElevatedFollowCamera.cs` after bootstrap migration.
- [x] Add `MobileJoystick.cs`, `ActionButton.cs`, `MatchHUD.cs`, and `MobileInputSource.cs` for safe, multi-touch landscape controls and score/timer/result presentation.
- [x] Update `PrototypeArenaBootstrap.cs` to compose goals with triggers, the 3v3-plus-goalies match, camera, match flow, controlled marker, and landscape HUD while retaining placeholder rink geometry.
- [x] Ensure the generated UI contains only joystick, PASS, SHOOT, and SWITCH gameplay controls and that Editor input remains active through the same routed source.
- [x] Enable `PrototypeArena.unity` in `EditorBuildSettings.asset` so mobile development builds start in the playable match.

### Phase 6 - Verification and documentation

- [x] Replace `PrototypeArenaSmokeCheck.cs` and `Phase3SmokeRunner.cs` assertions with Phase 1 PvE checks for roster counts, one-human invariant, required modular components, switching, goalie/match/HUD wiring, goal/reset behavior, and puck independence.
- [x] Run Unity compilation and live-Editor smoke verification and record the exact commands/results and `PHASE1_PVE_SMOKE_PASS` evidence.
- [x] Run static searches across `Assets`, `Packages`, and `ProjectSettings` confirming no multiplayer/network/backend/service implementation or package was added.
- [x] Execute the manual scenarios in `.docs/tests/test-mobile-hockey-mvp.md` as far as the available Editor environment permits and record any device-only checks explicitly as pending manual evidence.
- [x] Update `README.md` with Phase 1 setup, controls, architecture/tuning summary, smoke command, limitations, and the networking prohibition.
- [ ] Build and launch the development player on an attached phone or emulator, complete one touch-controlled match, and record device/runtime/layout/input evidence.

### Phase 6A - Recommended PASS regression verification

- [x] Extend `PrototypeArenaSmokeCheck.cs` with assertions that possession creates one recommendation and dotted path, movement input cannot directly alter it, tapping releases an imperfect non-homing puck, and feedback clears after release.
- [x] Update `.docs/tests/test-mobile-hockey-mvp.md` and `README.md` to document the always-visible possession recommendation, dotted path, tap-only PASS, and movement-only joystick/WASD invariant.
- [x] Run Unity compilation and the Phase 1 smoke flow, record `PHASE1_PVE_SMOKE_PASS`, and run `git diff --check` plus the static forbidden-service search.

## Validation

- Compile and run the automated Editor smoke flow with Unity `6000.5.9f1`, using the installed editor executable and a clean log path. Expected evidence: exit code `0`, no C# compiler errors, and `PHASE1_PVE_SMOKE_PASS`.
- Run `rg -n -i 'Photon|Fusion|Unity.Netcode|Relay|Matchmaking|Firebase|AuthenticationService|Lobby' Assets Packages/manifest.json ProjectSettings` and confirm no Phase 1 implementation/package references (documentation exclusions are intentional).
- In Play Mode at a landscape Game view, exercise WASD/left stick movement independently, E/controller west/touch tap PASS, Space/right trigger held SHOOT, and Q/controller north SWITCH. Confirm the dotted recommendation appears only during human possession and PASS requires no drag.
- Exercise touch controls in a mobile simulator/device when available and verify simultaneous joystick plus action input, control sizing, landscape layout, and no extra gameplay buttons.
- Execute the scenarios in `.docs/tests/test-mobile-hockey-mvp.md`; retain the Unity log and explicitly distinguish automated, Editor-observed, and device-pending evidence.

## Rollback / Risk

- The largest risk is composing many new runtime systems without serialized scene references. Mitigate through explicit `Configure` methods, null-safe startup, component requirements, and a smoke runner that builds the actual arena.
- Physics possession can oscillate during contests. Use a single carrier, release reclaim locks, relative-speed checks, and bounded claim priority; tune only after the complete loop compiles.
- AI may cluster or feel too perfect. Formation homes, one designated puck challenger, decision intervals, difficulty error, and bounded action probabilities reduce clustering and robotic execution.
- Runtime-built UI must coexist with keyboard/gamepad testing. Route touch and hardware through one composite input source and avoid EventSystem pointer state becoming the gameplay authority.
- The dotted recommendation must remain subtle and allocation-conscious while updating during possession; pool its visual dots and disable them instead of creating/destroying objects every frame.
- Existing scene/prefab/meta files and generated placeholder art remain reusable. Rollback is file-local: restore the prior committed Phase 3 sources/docs without data migration or service cleanup.
- No network/service package or external state is introduced, so rollback has no backend, account, schema, cloud-project, or compatibility concerns.
