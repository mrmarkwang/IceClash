# Offside Rule Plan

## Goal

Add deterministic two-direction offside warnings and stoppages to the local match, including a visible red zone grid and a neutral-zone faceoff restart, while preserving existing scoring and center-faceoff behavior.

## Current Context

- `Assets/_Project/Scripts/Hockey/PrototypeArenaBootstrap.cs` owns the runtime rink, blue lines, faceoff-dot markings, puck, goals, and match-system composition. The attacking blue lines are at `+/- PrototypeRinkGeometry.Length * 0.18f`.
- `Assets/_Project/Scripts/Match/LocalMatchSetup.cs` owns the ten-skater roster and composes `MatchController`; it is the natural place to compose an offside rule component with the same roster and puck.
- `Assets/_Project/Scripts/Match/MatchController.cs` owns live-play gating, scores, clock, actor resets, and the existing timed faceoff state. It currently supports only center resets.
- `Assets/_Project/Scripts/Player/PlayerController.cs` stores each skater's center-faceoff reset transform and delegates physical placement to `PlayerMovementController.ResetMotion`.
- `Assets/_Project/Scripts/Puck/PuckController.cs` exposes the established carrier, possession team, body position, and reset notification needed to distinguish live entry from teleports.
- `Assets/_Project/Scripts/Hockey/PrototypeArenaSmokeCheck.cs` is the existing integrated gameplay regression check and already validates center/post-goal faceoffs and goal flow.

## Decisions

- Add a focused `OffsideController` under the Match subsystem. It will evaluate a pure directional zone rule, track the puck's previous position for line crossing, own the pending-warning state, and request a stoppage from `MatchController` exactly once.
- A warning requires an established puck carrier outside the attacking zone plus a different attacking skater beyond the attacking blue line. Once armed, it remains associated with that attacking team during a same-team pass or temporarily loose-puck entry. It clears if all premature attackers tag up, or immediately when an opponent establishes possession, before entry.
- Treat exact equality with the blue-line coordinate as outside; crossing requires movement from at-or-before the line to beyond it in the team's attack direction.
- Build two non-colliding translucent red floor grids at arena startup and toggle only the warned attacking zone. Reuse the runtime primitive/line style rather than introducing an art dependency.
- Reuse the existing `Faceoff` match state and timer. Extend reset placement with an optional faceoff origin: goalies stay at their crease anchors, while skaters preserve their mirrored center formation translated around the selected dot.
- Add an editor-only deterministic faceoff-completion hook that expires the existing timer and executes the same production `FaceoffController.TickComplete`/match-state transition. This gives the synchronous smoke check evidence for disabled-during-delay and resumed-after-completion behavior without changing runtime timing.
- Select the existing neutral-zone dot on the offending blue-line side and choose left/right from the puck's crossing X coordinate. This keeps the restart aligned with visible rink markings.
- Do not add flags, new settings, compatibility paths, penalties, icing, referee systems, or broad AI formation changes.
- This is a user-facing, regression-prone match flow, so an E2E scenario document and integrated smoke coverage are required.

## Phased Tasks

### Phase 1 - Geometry and rule foundation

- [x] Update `PrototypeRinkGeometry` in `PrototypeArenaBootstrap.cs` with shared attacking-blue-line and neutral-faceoff-dot coordinates used by markings, rule detection, and restart selection.
- [x] Add `Assets/_Project/Scripts/Match/OffsideController.cs` with mirrored pure zone predicates, retained premature-attacker state, crossing-time interpolation, carrier-change sampling, tag-up clearing, swept puck-entry detection, reset handling, and one-shot stoppage routing.
- [x] Confirm exact-blue-line positions remain outside and that only a non-carrier teammate can arm the warning.

### Phase 2 - Warning presentation and composition foundation

- [x] Add two named non-colliding red warning grids to `PrototypeArenaBootstrap.cs` before roster construction, covering the Blue and Red offensive zones without obscuring gameplay.
- [x] Extend `LocalMatchSetup.BuildRoster` with explicit warning-grid renderer inputs and compose/configure `OffsideController` using the current roster, puck, match controller, and those renderers.
- [x] Wire warning-grid visibility to `OffsideController` so only the currently offending team's attacking zone is shown and all tag-ups, turnovers, stoppages, and resets hide both grids.

### Phase 3 - Match restart integration

- [x] Extend `PlayerController` with a reset-at-faceoff operation that preserves its configured mirrored formation offset around a supplied faceoff point.
- [x] Extend `MatchController` with an offside registration path that preserves score, enters the existing faceoff state, disables play, and resets the puck/skaters around the selected neutral-zone dot while leaving opening/post-goal center resets unchanged.
- [x] Add editor-only validation hooks to `FaceoffController`/`MatchController` that force the current timer due and execute the same production completion transition for deterministic smoke coverage.
- [x] Clear armed offside state when the opposing team establishes possession while retaining it through a same-team pass or temporarily loose puck.
- [x] Confirm no penalty, icing, referee, feature-flag, fallback, or unrelated AI behavior is introduced.

### Phase 4 - Tests and verification

- [x] Update `PrototypeArenaSmokeCheck.cs` to verify mirrored warning activation, opposite-grid exclusivity, red grid materials, absence of enabled warning-grid colliders, tag-up cancellation, opponent-possession cancellation, same-team loose/pass retention, exact-line boundary behavior, one-shot puck-entry calls, score preservation, neutral-dot restart placement, disabled gameplay during the offside faceoff, and resumed gameplay through the production completion transition.
- [x] Update the integrated smoke assertion output so offside behavior is part of the pass/fail evidence.
- [x] Run the Unity batch smoke check and record `PHASE1_PVE_SMOKE_PASS` with a zero exit code.
- [x] Run the Phase 1 static excluded-integration scan and confirm it returns no matches.

### Phase 5 - Documentation and status

- [x] Update `README.md` to describe the offside warning/stoppage/faceoff behavior and remove offsides from the prototype limitation list.
- [x] Execute the scenarios in `.docs/tests/test-offside-rule.md` against the integrated smoke evidence and record the observable outcome.
- [x] Record final evidence that every acceptance criterion in `req-offside-rule.md` is satisfied.

## Validation

- Unity compile and integrated smoke check:
  `/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/markwang/mw/IceClash -executeMethod IceClash.Tests.Editor.Phase3SmokeRunner.Run -logFile /tmp/iceclash-offside-smoke.log`
  Expected evidence: process exit code `0` and `PHASE1_PVE_SMOKE_PASS` in `/tmp/iceclash-offside-smoke.log`.
- Static scope check:
  `rg -n -i 'Photon|Fusion|Unity\.Netcode|Relay|Matchmaking|Firebase|AuthenticationService|Lobby' Assets Packages/manifest.json Packages/packages-lock.json ProjectSettings`
  Expected evidence: no matches.
- Integrated assertions must prove both attack directions, warning exclusivity, visibly red/collider-free grids, tag-up and turnover cancellation, same-team loose/pass retention, the exact-line boundary, one stoppage per entry, unchanged score, translated skater restart, disabled gameplay during the faceoff delay, resumed gameplay via the existing timer-completion transition, and opening/post-goal center-faceoff regression behavior.

## Rollback / Risk

- Swept entry detection can misread teleport resets; subscribe to puck reset notifications and reseed previous-position state whenever the puck is reset.
- Possession can become null during a pass; retain the armed attacking team only until entry, tag-up, opponent possession, a stoppage, or a match-state reset so a same-team loose puck can enter correctly without surviving a turnover.
- Translating the center formation near a neutral dot can approach rink boundaries; use existing marked dots and preserve goalie anchors to keep the prototype formation in bounds.
- Warning renderers must remain collider-free and initially disabled so they cannot alter physics or flash during arena construction.
- Rollback is confined to the new controller/grid objects and the small reset/composition hooks; no schema, saved data, or dependency migration is involved.
