# Skater Roles and Center-Ice Faceoff — Architecture and Implementation Plan

## Goal

Represent the five conventional skater roles explicitly and use them to place both teams into a mirrored, recognizable center-ice faceoff formation at every match reset.

## Current Context

- `GameplayContracts.cs` defines shared team/player contracts but has no skater role type.
- `PlayerController.cs` stores player identity, team, and one immutable reset transform configured when the roster is built.
- `LocalMatchSetup.cs` creates five skaters per team from integer slots and configures both the player reset transform and `HockeyPlayerAI` formation slot.
- `AIFormationController.cs` maps generic slot/count pairs to home, support, and defensive positions; its current `Home` positions are not a center-ice faceoff formation.
- `PrototypeArenaBootstrap.cs` owns the center-circle drawing radius as a private `2.4f` literal even though `PrototypeRinkGeometry` already centralizes rink dimensions and goalie/goal anchors.
- `MatchController.BeginFaceoff()` already resets every skater, goalie, and the puck before the faceoff countdown, so role-aware reset transforms can use the existing flow.
- `GameplayData.cs` captures skater identity/team/state but currently cannot expose role identity in snapshots.
- `PrototypeArenaSmokeCheck.cs` verifies the 5v5 roster and reset flow but not role distribution or faceoff geometry.

## Decisions

- Add `SkaterRole` with the five concrete positions: Center, LeftWing, RightWing, LeftDefense, and RightDefense. Reject a forward/defense-only boolean because it cannot define deterministic wing/defense sides or verify one actor per position.
- Map roster slot order deterministically to those five roles and retain count-driven construction. Reject Inspector arrays and duplicated Blue/Red spawn tables because the lineup is fixed and mirrored.
- Promote the center-faceoff-circle radius into `PrototypeRinkGeometry` and use it for both arena markings and formation validation so wing placement has one geometry source of truth.
- Make `AIFormationController` the single owner of role mapping and role-aware center-faceoff/home positions. Blue attacks positive Z; Red mirrors across center ice and faces negative Z. Left/right mirror relative to each team's attacking direction.
- Use the role-aware home position as the player's reset position and AI home target. Preserve the existing `MatchController.BeginFaceoff()` and `PlayerController.ResetActor()` lifecycle rather than introducing a second faceoff reset path.
- Expose role on `IPlayerController`, `PlayerController`, and `PlayerData` so the gameplay contract and snapshots can prove role identity persists. This is an additive local contract change with no persistence migration.
- Keep support and active-play defensive algorithms structurally unchanged except that their home reference comes from the role-aware formation. Do not add position-specific tactics, line changes, alternate faceoff dots, flags, fallbacks, or compatibility paths.
- Extend the existing smoke runner and local PvE E2E document because faceoff placement is user-visible and regression-prone; do not create a parallel runner.

## Phased Tasks

### Phase 1 - Define role and formation contracts

- [x] Add `SkaterRole` and the `IPlayerController.Role` contract in `GameplayContracts.cs` so every skater exposes one conventional position.
- [x] Add role capture to `PlayerData` in `GameplayData.cs` so live team snapshots retain the configured position identity.
- [x] Add `PrototypeRinkGeometry.CenterFaceoffCircleRadius` in `PrototypeArenaBootstrap.cs` and replace the private center-circle drawing literal so gameplay formation and rink presentation share one value.
- [x] Add deterministic slot-to-role mapping and mirrored role-aware `Home` coordinates in `AIFormationController.cs`, including clear failure behavior for unsupported slots.

### Phase 2 - Wire roster construction and reset behavior

- [x] Update `PlayerController.Configure` and its role property in `PlayerController.cs` so role and reset transform are configured together and persist through normal resets.
- [x] Update `LocalMatchSetup.SpawnSkater` in `LocalMatchSetup.cs` to derive each slot's role, use the role-aware home position, assign readable role-based object names, and pass role to the player.
- [x] Add an Editor-only `LocalMatchSetup.CaptureDataForValidation()` seam in `LocalMatchSetup.cs` so the synchronous smoke runner can refresh snapshots after staged control, possession, and reset transitions instead of reading the initial snapshot repeatedly.
- [x] Update `HockeyPlayerAI.Configure` in `HockeyPlayerAI.cs` to derive its home target from the same role-aware formation contract while preserving existing decision states.
- [x] Confirm `MatchController.BeginFaceoff()` continues to reset all skaters, goalies, and the center puck through the existing actor reset path without a second reset system.
- [x] Add an Editor-only `MatchController.CompleteGoalPauseForValidation()` transition helper that follows the production GoalPause-to-`BeginFaceoff()` path without waiting on wall-clock time, matching the class's existing validation helpers.

### Phase 3 - Add role and faceoff regression coverage

- [x] Extend `PrototypeArenaSmokeCheck.cs` to assert one of each skater role per team, role values in snapshots, the full same-role Blue/Red center-ice mirror, role-correct left/right lateral sides relative to each team's attack orientation, wings outside `PrototypeRinkGeometry.CenterFaceoffCircleRadius`, defensemen goal-side of forwards, attacking rotations, goalie anchors, and center puck reset.
- [x] Add `PrototypeArenaSmokeCheck.cs` geometry assertions that every faceoff skater is within rink bounds, every skater pair has safe horizontal clearance, every skater clears the center puck, and every skater clears both goalie crease anchors.
- [x] Capture a baseline player-ID-to-role mapping in `PrototypeArenaSmokeCheck.cs`, run the existing control switch and Blue/Red possession transitions, call `LocalMatchSetup.CaptureDataForValidation()` at each checkpoint, and recheck actor and freshly captured snapshot roles before and after a faceoff reset.
- [x] Exercise the real post-goal sequence in `PrototypeArenaSmokeCheck.cs`: move actors, register/stage a valid goal from active play, verify the immediate GoalPause reset, call `CompleteGoalPauseForValidation()` to execute the production faceoff entry, and verify all ten skater positions/rotations, both goalie anchors, and the center puck before play resumes.
- [x] Update smoke diagnostics and pass output so role/faceoff failures are distinguishable from unrelated gameplay checks.

### Phase 4 - Update user-facing documentation

- [x] Update `README.md` to describe three forwards, two defensemen, and the center-ice faceoff reset.
- [x] Update `.docs/tests/test-mobile-hockey-mvp.md` with observable role distribution and faceoff placement steps for match start and post-goal resets.
- [x] Search tracked source and current requirements/tests for stale generic five-slot or non-standard faceoff statements that contradict the new behavior.

### Phase 5 - Validate the integrated change

- [x] Run `git diff --check` and record a clean result.
- [x] Compile and run `IceClash.Tests.Editor.Phase3SmokeRunner.Run` with Unity `6000.5.9f1` against a temporary project copy when the main project is open; record role, faceoff, roster, goalie, puck-reset, and one-human evidence separately from unrelated smoke failures.
- [x] Run the human-readable scenarios in `.docs/tests/test-skater-roles-faceoff.md` using automated smoke evidence for structural assertions and report any visual/manual observation that remains pending.
- [x] Mark every plan task complete only after its corresponding code, documentation, or verification evidence exists.

## Validation

- `git diff --check` must exit `0` without whitespace errors.
- Unity command: `Unity -batchmode -projectPath <temporary-copy> -executeMethod IceClash.Tests.Editor.Phase3SmokeRunner.Run -logFile <log>`.
- Expected role evidence: each team reports exactly one Center, Left Wing, Right Wing, Left Defense, and Right Defense; snapshot roles match actors.
- Expected faceoff evidence: every same-role Blue/Red pair is mirrored across center ice, Left/Right roles occupy the correct lateral side relative to each team's attacking direction, both centers are closest to the center dot on opposite sides, wings are outside the shared center-circle radius, defensemen are farther toward their own goal than all forwards, all skaters face their attacking direction, every skater is inside the rink and clear of other skaters/the puck/both crease anchors, goalies remain at crease anchors, and the puck is centered.
- Expected persistence evidence: one baseline player-ID-to-role mapping matches actors and freshly refreshed snapshots after human-control switching, Blue and Red possession changes, the immediate post-goal reset, and the subsequent faceoff reset.
- Expected post-goal evidence: a goal registered from Playing enters GoalPause with actors and puck reset, then the GoalPause-to-Faceoff path preserves the same role formation before gameplay is re-enabled.
- Existing full-smoke failures must be attributed from raw diagnostics and must not be reported as passes unless they actually pass.

## Rollback / Risk

- A mismatched role mapping between player resets and AI home targets would make actors drift away from faceoff positions. Both must use one `AIFormationController` mapping and smoke assertions must compare them.
- Centers must not overlap each other or the puck. Use small mirrored Z offsets around the center dot while keeping them closer than wings and defensemen.
- Faceoff coordinates can drift out of agreement with rink artwork or collide as tuning changes. Use shared rink/center-circle geometry and assert rink bounds plus pairwise, puck, and crease-anchor clearances.
- Left/right semantics must mirror relative to attacking direction, not merely reuse world X labels for both teams.
- Existing uncommitted 5v5 work shares `LocalMatchSetup.cs`, `PrototypeArenaSmokeCheck.cs`, README, and requirements/tests. Patch those files surgically and preserve the validated roster changes.
- Rollback is limited to the additive role contract, role-aware formation mapping, smoke assertions, and documentation; no saved data or external system is affected.
