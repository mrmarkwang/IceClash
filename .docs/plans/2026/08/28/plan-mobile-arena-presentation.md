# Mobile Arena Presentation - Architecture and Implementation Plan

## Goal

Bring the runtime arena’s proportions and landscape composition close to the supplied mobile hockey references while preserving the current Phase 1 gameplay contracts and runtime-built scene.

## Current Context

- `PrototypeArena.unity` is intentionally empty; `PrototypeArenaBootstrap.cs` creates all rink geometry, markings, goals, materials, puck, roster, and camera at runtime.
- The current rink is `20 x 34` world units with 2.3-unit dark boards, cyan ice, a flat rear-only goal lattice, and a `58` degree follow camera offset by `(0, 16.5, -15)`, which produces a distant whole-rink view.
- `LocalMatchSetup.cs` currently applies a shared `0.8` scale to skaters and goalies. `AIFormationController.cs` and `LocalMatchSetup.cs` contain goalie/defensive anchors tied to the current rink length.
- `PrototypeArenaSmokeCheck.cs` already validates roster, player scale, equal goalie size, widened goals, controls, gameplay systems, scoring, and results. Its presentation assertions must be revised rather than discarded.
- The worktree contains active user-owned changes in bootstrap, goalie AI, shooting, match setup, mobile controls, smoke checks, RPD docs, and README. This story must patch overlapping files surgically.

## Decisions

- Keep runtime procedural geometry and refine it in place; reject a new scene, imported rink package, copied screenshot assets, or a parallel mobile arena.
- Use a `24 x 48` rink with larger rounded corners. This preserves a hockey-like 2:1 length-to-width relationship while retaining enough width for the current three-skater formations.
- Construct boundaries as a collider-backed low white board, a thin yellow kickplate, a blue top rail, and a translucent non-colliding glass layer. Preserve one physical containment boundary rather than stacking colliders.
- Keep the already-requested six-unit goal mouth and scoring width. Add rear posts, roof rails, base rails, and strand grids on back/roof/sides so the net reads correctly from the oblique camera.
- Use a closer, narrower-FOV follow camera centered on the selected player with existing puck bias and smoothing. Do not add cinematic camera states or device-specific branches.
- Scale skaters uniformly below the current `0.8`. Instantiate goalies from the same placeholder prefab for visual consistency, but use a non-uniform broader scale so their height stays close while their silhouette reads as padded.
- Align goalie and defensive anchors with the enlarged goal-line area using centralized constants in their current owning components; do not change public gameplay interfaces or persistence.
- Extend the existing smoke check with observable geometry, camera, and scale assertions. Skip a new E2E spec because the current `test-mobile-controls-v1.md` and Editor smoke flow already cover the only live scene; capture a fresh image as manual presentation evidence when the Editor permits.

## Phased Tasks

### Phase 1 - Preserve behavior and lock presentation constants

- [x] Inspect the current diffs in `PrototypeArenaBootstrap.cs`, `LocalMatchSetup.cs`, `AIFormationController.cs`, `HockeyCameraController.cs`, and `PrototypeArenaSmokeCheck.cs` and preserve unrelated tuning.
- [x] Define rink, board, goal-depth, actor-scale, goalie-anchor, and camera values that keep goal triggers and AI anchors inside the resized sheet.
- [x] Confirm no new scene, package, asset import, copied brand element, feature flag, or alternate gameplay path is introduced.

### Phase 2 - Rebuild rink boundaries and materials

- [x] Update `PrototypeArenaBootstrap.cs` to generate the `24 x 48` rounded near-white ice surface and proportionally reposition center, blue, goal, crease, faceoff, and neutral-zone markings.
- [x] Replace the tall dark boundary presentation in `PrototypeArenaBootstrap.cs` with collider-backed white boards plus non-colliding yellow kickplates, blue rails, and translucent glass panels.
- [x] Keep the puck, player, goal trigger, match, HUD, and control composition paths unchanged while aligning goal and trigger positions with the new rink length.

### Phase 3 - Build dimensional goals and actor proportions

- [x] Update `PrototypeArenaBootstrap.cs` so each goal includes front and rear frame members, roof/base depth rails, and visible back, roof, and side net strands.
- [x] Update `LocalMatchSetup.cs` so skaters use the smaller rink-relative scale and goalies use a similarly tall but broader padded silhouette from the shared placeholder prefab.
- [x] Update `AIFormationController.cs` and goalie spawn anchors in `LocalMatchSetup.cs` so defending and crease tracking remain aligned with the enlarged rink.

### Phase 4 - Tune mobile camera composition

- [x] Update `HockeyCameraController.cs` defaults and the bootstrap camera field of view so a landscape phone view normally crops the rink length while keeping rink width and nearby play readable.
- [x] Preserve selected-player smoothing, puck bias, stable rink orientation, control-transfer retargeting, and camera-relative skating.
- [x] Confirm the joystick and action controls remain inside the safe area and do not require layout changes for the new world framing.

### Phase 5 - Regression coverage and verification

- [x] Update `PrototypeArenaSmokeCheck.cs` to assert rink aspect/proportions, layered boundaries, dimensional nets, camera framing values, smaller skaters, broader goalies, aligned goal anchors, and existing widened scoring volumes.
- [x] Compile with Unity `6000.5.9f1` and run `IceClash > Run Phase 1 PvE Smoke Check`; record zero compiler errors and `PHASE1_PVE_SMOKE_PASS`.
- [x] Run `git diff --check` plus focused searches for stale equal-size/old-rink assumptions, and capture a landscape Game-view image if the available Editor session supports it.
- [x] Mark plan tasks complete only after each code change or verification result exists.

## Validation

- Run Unity `6000.5.9f1` compilation and the Editor smoke flow. Expected evidence: no `CS` compiler errors and a `PHASE1_PVE_SMOKE_PASS` log containing the new arena presentation invariants.
- Run `git diff --check`. Expected evidence: exit `0` with no whitespace errors.
- Search changed source for the legacy `20 x 34`, 2.3-unit wall, whole-rink camera, equal-scale goalie, and old goalie-anchor assumptions. Expected evidence: the legacy presentation values do not remain in active code.
- Observe the 16:9 Game view. Expected evidence: low layered boards, white elongated ice, a cropped action-oriented camera view, dimensional nets, smaller skaters, and broader goalies without control overlap.

## Rollback / Risk

- Resizing the rink without moving goal triggers and AI anchors would create invisible scoring or defensive misalignment. Update and assert those values together.
- Non-uniform goalie scale also scales the inherited character controller. Keep the footprint bounded and verify goalie tracking/scoring flow rather than adding a second goalie prefab or controller path.
- Procedural translucent materials can render differently across pipelines. Use a simple URP/Standard-compatible transparent material configuration and keep gameplay containment on opaque board colliders.
- A narrower camera may temporarily hide distant teammates. Preserve puck bias and tune only the default offset/FOV; rollback is isolated to camera defaults if play readability regresses.
- The supplied references show production art the prototype does not contain. Match composition and proportion only, and report placeholder-model limitations truthfully.
