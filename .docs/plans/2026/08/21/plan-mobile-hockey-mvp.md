# Mobile 2v2 Hockey MVP — Development Plan

## Goal

Deliver a small, playable local 2v2 arcade hockey prototype in Unity: one human player, three AI skaters, physics puck play, goals, a three-minute match, and a restartable result flow. The project must establish uncomplicated seams that let future local, AI, and network sources drive the same gameplay control layer.

## Current Context

- The workspace is empty as of 2026-08-21: there is no Unity project, source code, scene, test harness, or Git repository to preserve.
- The supplied MVP brief is the sole product specification. Its required platform is Unity/C# for iOS and Android; Flutter is explicitly out of scope for gameplay.
- Editor keyboard controls are the first validation surface. Touch controls are a functional placeholder rather than a visual-design milestone.
- The primary risk is building independently functioning systems that do not form a playable loop. Each phase therefore ends at an observable gameplay boundary.

## Decisions

- Create a standalone Unity project in `Assets/_Project` with scene, prefab, script, ScriptableObject, and test folders matching the supplied structure.
- Use a single authoritative local simulation in this milestone. `PlayerController` consumes abstracted commands; `LocalPlayerInput` and AI command generation feed the same controller. Future network input is an extension point only.
- Keep the puck as a Rigidbody-driven object. Possession influences it toward a configurable stick/control point through physics instead of parenting it to the skater.
- Use explicit small state types/interfaces (`IPlayerInput`, `IPlayerController`, `IPuckController`, `IMatchState`, player action state) rather than a generalized game framework.
- Use a simple finite-state AI and direct steering before considering NavMesh. The rink is flat, bounded, and low-obstacle; NavMesh would add setup with little prototype benefit.
- Use generated primitives and Unity UI placeholders. No external art, backend, or new service dependency is required.
- Exclude feature flags, environment variables, backward-compatibility layers, networking packages, and speculative persistence from this MVP.
- E2E coverage is needed as a human-readable Unity play-mode smoke scenario because this is a user-facing real-time game loop; executable automated UI E2E is deferred until the Unity project and test runner exist.

## Phased Tasks

### Phase 1 - Project foundation and first playable slice (start today)

- [x] Create the Unity project using the selected LTS/version and record it in `README.md` and `ProjectSettings/ProjectVersion.txt`.
- [x] Create `Assets/_Project/{Art,Audio,Materials,Prefabs,Scenes,Scripts,ScriptableObjects,Tests}` plus the required script domains: `Core`, `Player`, `Puck`, `Hockey`, `AI`, `Camera`, `UI`, `Input`, and `Match`.
- [x] Configure the Unity Input System and create an input action asset supporting Editor WASD, Shift, Space, E, Q, and a controller map; ensure actions are readable through `IPlayerInput` rather than directly from movement code.
- [x] Build a `PrototypeArena` scene containing an ice plane, boards, center line, goal areas, two placeholder goals, lighting, and a fixed game-start location.
- [x] Replace the square-corner arena geometry in `PrototypeArenaBootstrap` with a vertical hockey-rink outline: straight side/end boards connected by rounded-corner board segments, inset goals, red center/goal lines, blue lines, and visible crease markings.
- [x] Add reference-style faceoff markings to `PrototypeArenaBootstrap`: blue center circle/dot, four red zone circles with center dots, and neutral-zone faceoff dots; keep their layout symmetric about the vertical rink axis.
- [x] Replace block-segment faceoff/crease circles in `PrototypeArenaBootstrap` with smooth low-poly line or mesh curves, and add lightweight visible net geometry behind each inset goal frame without changing puck-board collision behavior.
- [x] Implement the initial `PlayerController`, player data/team identity, movement state, sprint placeholder, and `LocalPlayerInput` so one visible skater moves and turns correctly in the arena.
- [x] Implement an independent puck prefab with Rigidbody, Collider, tunable drag/friction/bounce values, and `IPuckController` ownership metadata; verify it collides with boards without being parented to the player.
- [x] Add a separate elevated follow camera that tracks the controlled player while keeping the puck and immediate play area in view.
- [x] Update `PrototypeArenaBootstrap` and `ElevatedFollowCamera` so the rink's long axis and goals are presented vertically in the Game view; keep camera-relative movement aligned with that orientation.
- [x] Run the Unity Editor compile check and the headless `IceClash.Hockey.PrototypeArenaSmokeCheck.Run` verification; confirm it creates the player, elevated camera, and unparented Rigidbody puck.
- [ ] Manually open `PrototypeArena`, press Play, confirm the vertical rounded hockey-rink shape, then move/sprint the human skater with WASD/Shift and observe the independently colliding puck in the Unity Editor.

### Phase 2 - Core puck interactions and player actions

- [x] Add a configurable control-radius and stick/control-point influence so eligible players can gain and carry an independently simulated puck.
- [x] Implement player action transitions for idle, skating, sprinting, controlling puck, passing, shooting, checking, and knocked down, keeping action cooldowns owned by gameplay components.
- [x] Implement shoot from the configured Input System action so puck possession—not distance to the opposing goal—is the only range gate; buffer a recent input through a short action/cooldown boundary, target the opposing net, use configurable per-player minimum/maximum power, accuracy, cooldown, and maximum speed, and release possession before applying puck impulse.
- [x] Implement assisted teammate selection and passing with configurable pass speed, range, assist strength, and interception radius.
- [x] Implement arcade checking with configurable range, force, active duration, and cooldown; apply knockdown/dispossession only to valid opposing targets.
- [ ] Add focused edit/play-mode tests for possession release, shot cooldown, pass targeting, and check eligibility once the Unity test assemblies exist.
- [ ] Manually verify all Editor action controls against a free puck and record observed limitations in `README.md` (requires the Phase 3 teammate/opponent spawns for pass/check coverage).

### Phase 3 - Local 2v2 setup and basic AI

- [ ] Add `MatchData`, `TeamData`, and `PlayerData` with IDs, team association, transform/state snapshots, puck status, and score/match state needed by the local simulation.
- [ ] Create a reusable skater prefab and spawn/configure one human player, one allied AI, and two opponent AI players at clear reset positions.
- [ ] Implement AI command generation through the same player-control path as local input, without letting AI call player movement internals directly.
- [ ] Implement a simple finite-state AI behavior with Defend, ChasePuck, Support, Attack, Shoot, and Recover behavior using direct steering and configurable decision distances.
- [ ] Add basic team role selection so the puck-side AI chases/attacks, a teammate supports, and defenders bias toward their own goal.
- [ ] Verify that all four players move, AI can pursue/support/defend, and AI actions use the existing puck/action contracts rather than a special shortcut.

### Phase 4 - Goals, match flow, and results

- [ ] Build goal trigger volumes that determine scoring team from the scoring puck direction/goal ownership and prevent duplicate scoring during a reset.
- [ ] Implement `MatchManager` and `IMatchState` for start countdown, active play, goal pause, reset, end-of-match, pause/resume, time, and score.
- [ ] Reset players, AI roles, and puck after a goal; show a goal state for approximately two seconds before play resumes.
- [ ] Implement the three-minute countdown and terminal match decision; determine WIN/LOSS from the final score and keep overtime out of scope.
- [ ] Create main-menu, match, and result navigation: PLAY starts practice match; REMATCH resets it; MAIN MENU returns safely.
- [ ] Add play-mode tests for goal scoring once per entry, reset state, timer end, and result determination.
- [ ] Manually play a shortened debug-duration match and then a standard-duration match to verify scoring, reset, end result, rematch, and menu return.

### Phase 5 - HUD, mobile placeholders, and tuning

- [ ] Build the HUD showing Team A score, Team B score, countdown, active player name/team, possession indicator, and goal notification.
- [ ] Add placeholder left joystick and right-side PASS/SHOOT/CHECK/SPRINT buttons that feed the same Input System actions as Editor controls.
- [ ] Expose all specified shot, pass, check, puck control, camera, AI, and match tuning values through Inspector fields or ScriptableObjects with sensible prototype defaults.
- [ ] Add a pause-safe UI state so input and match simulation do not continue underneath goal/result screens.
- [ ] Test UI at representative phone aspect ratios in the Unity Game view and correct overlap, unreachable controls, or unreadable score/timer text.
- [ ] Perform a basic profiling pass in the Editor and on one iOS or Android test device when available; record the scene, device, FPS, and obvious hotspots.

### Phase 6 - Stabilization, playtest, and handoff

- [ ] Run Unity compilation and the complete edit/play-mode test suite; fix project-owned failures before handoff.
- [ ] Execute `.docs/tests/test-mobile-hockey-mvp.md` as a full manual play-mode scenario and record each pass/fail result.
- [ ] Playtest the full three-minute loop for responsiveness, puck recovery, AI behavior, goal resets, and rematch stability; make only targeted balance/bug fixes within MVP scope.
- [ ] Update `README.md` with Unity version, setup, opening scene, how to run, Editor controls, mobile placeholder controls, and known limitations.
- [ ] Document files created/modified, verification evidence, excluded multiplayer work, and the next milestone recommendation in `.docs/done/2026/08/21/mobile-hockey-mvp.md` after implementation is complete.

## Validation

- Phase 1 gate: Unity reports no compile errors; `PrototypeArena` plays with player movement/sprint, elevated camera, and a free physics puck.
- Phase 2 gate: manual Editor checks confirm possession, pass, shot, check, cooldowns, interception/dispossession paths, and no puck parenting.
- Phase 3 gate: one human and three AI skaters complete visible role behavior through shared controller contracts.
- Phase 4 gate: goal scoring, pause/reset, score, timer, result, rematch, and main-menu return work in a full match flow.
- Phase 5 gate: HUD and touch placeholders are usable in representative portrait/landscape layouts selected for the game; profiling evidence is recorded.
- Phase 6 commands: run the Unity Test Runner for all edit-mode and play-mode tests, then enter Play Mode from the documented start scene and execute the manual E2E scenario.
- Expected final evidence: the 17 supplied milestone deliverables are checked off in the manual scenario, with compilation/test outcomes and any device-performance limitation written down honestly.

## Rollback / Risk

- Use incremental, independently playable commits once Git is initialized; each phase should leave the project compiling and launchable.
- Physics feel is the highest gameplay risk. Keep force, drag, possession strength, control radius, and action cooldowns centralized and tunable, not hard-coded across scripts.
- AI can become unstable near boards/goals. Start with direct steering and bounded targets; only introduce NavMesh if profiling and behavior evidence show it is necessary.
- The future multiplayer seam is a design constraint, not a networking deliverable. Avoid serializing/synchronizing objects or adding a networking package during this work.
- If a dependency or Unity package blocks compilation, revert only the affected phase-level change and preserve earlier working scenes/prefabs; do not remove broad project files or reset the workspace.
