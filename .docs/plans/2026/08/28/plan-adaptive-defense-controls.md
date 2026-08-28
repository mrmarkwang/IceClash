# Adaptive Defense Controls Plan

## Goal

Make opponent possession immediately playable on touch by presenting SWITCH and CHECK, routing those actions through the shared input contract, and resolving CHECK as a bounded contextual body or pull challenge while preserving existing offensive and possession behavior.

## Current Context

- `MobileControlsBuilder` creates three fixed `MobileActionButton` instances labelled PASS, DEKE, and SHOOT; `MobileActionButton` can configure its label only at construction.
- `PlayerInputController` maps those fixed buttons to offensive signals and exposes SWITCH only from `LocalPlayerInput`, so touch cannot currently switch.
- `IPlayerInput` is the shared hardware, touch, and AI action contract. `LocalPlayerInput` and `HockeyPlayerAI` are its concrete sources.
- `PlayerSwitchController` already owns useful-skater scoring, cooldown, input/AI transfer, marker movement, selection events, and camera retargeting.
- `PuckController.CarrierChanged` is the authoritative established-possession event. Loose-puck motion intentionally does not trigger automatic player selection.
- `LocalMatchSetup` composes team-level gameplay systems, while `PlayerController` composes per-skater pass and shoot actions. A defensive check must retain cooldown across controlled-skater changes, so it belongs at the human-team composition level rather than on each skater.
- `PlayerMovementController` owns planar skater velocity but has no bounded external impulse path.
- Gameplay components are added to instantiated skaters at runtime, so serialized fields on those components are not a durable prefab/scene tuning surface.
- `PrototypeArenaSmokeCheck` is the deterministic gameplay verification surface and currently contains uncommitted reliable-pass checks that must be preserved.

## Decisions

- Treat only an established non-Blue carrier as defensive mode. Human possession and no carrier use the existing offensive mode so loose-puck play does not flicker between action sets.
- Reuse the existing left and right action slots for SWITCH and CHECK and deactivate the middle DEKE slot during defensive mode. This yields exactly two defensive buttons without adding layout geometry.
- Let `PlayerInputController` own semantic mapping of the reusable touch slots and subscribe to `PuckController.CarrierChanged`; reset a button before changing its role so a held offensive action cannot become a defensive press.
- Extend `IPlayerInput` with one `CheckPressed` signal. Keep `PlayerSwitchController` as the only switch implementation and route the defensive left touch slot into its existing `SwitchPressed` input.
- Add one human-team `DefensiveCheckController` from `LocalMatchSetup`. It reads the currently controlled player from `PlayerSwitchController`, selects body check at close range, and otherwise selects pull check only inside a longer forward cone. The single controller owns a team-level cooldown that cannot be bypassed by SWITCH.
- Persist all check values in a `DefensiveCheckTuning` ScriptableObject under `Assets/_Project/Resources`. Runtime validation must enforce body range `0.5–2.0 m`, pull range `0.6–3.5 m` and at least `0.1 m` above body range, forward dot `0–1`, cooldown `0.2–2.0 s`, puck speed `1–15 m/s`, and body impulse `0–6 m/s`, even if serialized data is malformed.
- A body check dislodges the puck away from contact and applies bounded opposing impulses through a small decaying external-velocity path in `PlayerMovementController`. A pull check dislodges the puck toward the checker without directly assigning possession.
- Add a focused puck dislodge operation that validates the expected carrier, clears any intended-pass state through the existing carrier-release path, and assigns one bounded free-puck velocity. It performs no later trajectory steering.
- Do not add animation state, AI check decisions, penalties, possession guarantees, feature flags, environment variables, fallbacks, compatibility layers, or unrelated refactors.

## Phased Tasks

### Phase 1 - Lock possession and reusable-control behavior

- [x] Update `MobileActionButton` with safe runtime relabelling and input reset behavior suitable for changing an existing button's semantic role.
- [x] Update `PlayerInputController` configuration to receive `PuckController`, subscribe to established carrier changes, and expose an observable offensive/defensive control mode.
- [x] Preserve joystick geometry, action hit regions, safe-area fitting, and independent pointer ownership while toggling only action labels and the middle slot's active state.

### Phase 2 - Extend the shared action contract

- [x] Add `CheckPressed` to `IPlayerInput` and implement it in `LocalPlayerInput`, `PlayerInputController`, and `HockeyPlayerAI` without changing existing PASS, SHOOT, or SWITCH semantics.
- [x] Route the reusable left touch action to `SwitchPressed` only in defensive mode and the reusable right action to `CheckPressed` only in defensive mode.
- [x] Confirm offensive mode retains PASS, DEKE, and SHOOT routing and defensive mode cannot leak those same touch presses into offensive actions.
- [x] Add deterministic mapping-contract checks for keyboard CHECK, gamepad CHECK, and the existing hardware PASS, SHOOT, and SWITCH mappings used by `PlayerInputController`; focused-device input remains a manual scenario because batch-mode device events are focus-suppressed.

### Phase 3 - Implement contextual defensive checks

- [x] Add `DefensiveCheckTuning` under `Assets/_Project/Scripts/Gameplay` plus a persisted `Assets/_Project/Resources/DefensiveCheckTuning.asset` with Inspector-editable defaults and runtime clamps for every required numeric invariant.
- [x] Add one `DefensiveCheckController` under `Assets/_Project/Scripts/Gameplay`, compose it from `LocalMatchSetup`, load the persisted tuning asset, and retain its successful-action cooldown across controlled-skater switches.
- [x] Add a bounded decaying impulse operation to `PlayerMovementController` so body checks can separate skaters without replacing normal skating ownership; clear pending impulse in `SetMovementEnabled(false)` and `ResetMotion`.
- [x] Add a validated dislodge operation to `PuckController` that releases only the expected current carrier, clears pass state, and leaves the puck physically loose with the requested bounded planar velocity.
- [x] Tick the team-level defensive controller from `CheckPressed`, reject disabled gameplay, and reset cooldown state on match reset without allowing SWITCH to reset it during active play.

### Phase 4 - Verify adaptive controls and action outcomes

- [x] Extend `PrototypeArenaSmokeCheck` without discarding the current reliable-pass assertions to verify default offensive labels, defensive SWITCH/CHECK visibility, and restoration after opponent possession ends.
- [x] Hold a touch action through a possession-mode change, repeat Red/loose/Blue/Red transitions, and verify pointer phases reset without a synthetic SWITCH, CHECK, PASS, or SHOOT action.
- [x] Exercise the defensive touch SWITCH route and verify it uses `PlayerSwitchController` control transfer rather than a duplicate selection path.
- [x] Add deterministic body-check, pull-check, switch-during-cooldown, cooldown/out-of-range rejection, loose-puck, gameplay-disable/reset impulse cleanup, and no-direct-possession checks using the configured runtime components.
- [x] Prove body impulse never exceeds the validated `6 m/s` cap and prove a pull-check puck receives only its initial release vector across repeated puck reception ticks with no continuing steering or direct possession assignment.
- [x] Update the smoke pass/failure evidence fields so the log reports each adaptive-control and defensive-action outcome independently of unrelated legacy assertions.

### Phase 5 - Documentation and final evidence

- [x] Update `README.md` controls, gameplay explanation, architecture list, smoke-check description, and limitations for adaptive defense controls.
- [x] Compile a temporary copy with Unity `6000.5.9f1` and run the full Phase 1 smoke check, recording all adaptive-defense fields as `True` plus unrelated existing long-pass/presentation failures that prevent the aggregate pass marker.
- [x] Run the static external-service boundary command and verify no out-of-scope integration was introduced.
- [x] Mark each plan task complete only after its code, documentation, or verification evidence exists.

## Validation

- Compile and execute the Unity `6000.5.9f1` Phase 1 smoke check through `IceClash > Run Phase 1 PvE Smoke Check`; require successful compilation and explicit `True` adaptive-controls/body-check/pull-check fields from either the pass or failure report. An aggregate `PHASE1_PVE_SMOKE_PASS` is preferred but must not be claimed when named unrelated assertions fail, and every such failure must be reported separately.
- Run `rg -n -i 'Photon|Fusion|Unity\.Netcode|Relay|Matchmaking|Firebase|AuthenticationService|Lobby' Assets Packages/manifest.json Packages/packages-lock.json ProjectSettings`; expect no matches.
- Inspect the stable diff to confirm `PlayerSwitchController` remains the only player-transfer path, a check never directly assigns possession, and the in-progress reliable-pass changes remain intact.
- Corrupt or override tuning values in deterministic validation and confirm runtime values still satisfy every documented range/ordering/cooldown cap before restoring the persisted defaults.
- Manually verify final animation/feel on a device later; production feedback and tuning are explicit non-goals for this change.

Evidence recorded on 2026-08-28:

- Unity `6000.5.9f1` compiled the temporary current-source copy with exit code `0` and no C# errors.
- The Play Mode smoke report recorded `hardwareActionContract=True`, `defensiveControlMode=True`, `heldTransitionCleared=True`, `repeatedControlTransitions=True`, `tuningBounds=True`, `bodyCheck=True`, `pullCheck=True`, `sharedCooldown=True`, `rejectedCheck=True`, `impulseReset=True`, and `looseAfterCheck=True`.
- The aggregate Phase 1 smoke remained nonzero on concurrent/pre-existing assertions: `arenaPresentation=False`, `puckSizeAndPosition=False`, and the in-progress reliable-pass path's `longPassReceived=False` / `passReceptionAutoControl=False`. No adaptive-defense field failed.
- The static external-service boundary command returned no matches.

## Rollback / Risk

- Runtime relabelling can reinterpret an in-flight pointer; resetting pointer phases before role changes prevents a held PASS/SHOOT from becoming SWITCH/CHECK. Repeated transition coverage includes an in-flight pointer.
- Body impulses can destabilize the `CharacterController`; clamp the impulse and decay it independently from normal desired velocity.
- Check ranges that overlap ordinary claim range can make immediate pickup likely. This is allowed only through the existing physical claim loop, never direct possession assignment, and remains tunable.
- The smoke check has concurrent reliable-pass edits. Patch only the relevant assertions and preserve all existing pass outcome coverage.
- A team-level controller prevents CHECK → SWITCH → CHECK from bypassing cooldown; faceoff reset clears its cooldown as a new play phase.
- Rollback is localized to the new tuning asset/controller, one shared input signal, reusable action-mode mapping, bounded movement impulse, and puck dislodge method.
