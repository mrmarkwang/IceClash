# Player Attributes and Gameplay Plan

## Goal

Represent constrained player builds directly and make all nine attributes materially affect the existing local hockey systems while keeping human input authoritative and AI decision quality separate.

## Current Context

- `GameplayContracts.cs` defines the shared input contract but does not include the already-present DEKE input exposed by `PlayerInputController`.
- `PlayerController.cs` composes movement, stick, pass, reception, and shot components, exposes a constant stamina value, and passes only AI action quality into pass/shot execution.
- `PlayerMovementController.cs` owns fixed top speed, acceleration, and turn rates; `StickPuckInteraction.cs` and `PuckController.cs` own fixed claim/carry behavior.
- `ShootController.cs` uses fixed power/spread and `PassController.cs` uses random angular spread; `PassReceivingZone.cs` has fixed reception bounds.
- `DefensiveCheckController.cs` currently resolves any in-range body/pull check without a physical/attribute contest.
- `HockeyPlayerAI.cs` separately owns Easy/Normal reaction and decision quality. That separation must be preserved.
- `LocalMatchSetup.cs` runtime-builds all skaters and is the narrowest place to assign valid default role-oriented builds without changing prefabs or scenes.
- `PrototypeArenaSmokeCheck.cs` is the executable integration harness; Unity `6000.5.9f1` is installed locally and supports batch execution.

## Decisions

- Add a pure serializable `PlayerAttributeBuild` model with level `1..50`, rating bounds `40..95`, budget `(level - 1) * 8`, and progressive target-rating costs: each increment ending at ratings 41..69 costs `1`, 70..84 costs `2`, and 85..95 costs `3`. Thus 40→70 costs 31, 40→95 costs 92, and maximizing all nine costs 828. Allocation is atomic; invalid, unaffordable, or out-of-range targets leave the build unchanged. Level-25 prototype presets use the following explicit `(SPD, ACC, AGI, STA, CTR, SHT, PAS, STR, DEF)` targets: Speed `(78,75,73,58,52,45,45,45,45)` costing 175; Sniper `(60,60,72,55,74,78,50,43,43)` costing 192; Playmaker `(62,62,72,55,74,45,76,43,48)` costing 192; Power `(55,58,50,72,60,75,45,78,41)` costing 192; Two-Way `(58,58,60,68,68,49,67,55,69)` costing 192. This gives future UI/persistence code one authoritative contract without adding either now.
- Keep runtime attribute consumption on `PlayerController`, with focused effect calculations delegated to the existing movement/action/puck components. Do not introduce a global attribute singleton or dependency framework.
- Add DEKE to `IPlayerInput` and a focused `DekeController`. A valid button press while carrying starts a `0.18..0.42s` control/protection window from averaged CTR/AGI, on a `0.55s` cooldown; it never generates movement or chooses direction. Joystick direction and speed determine the actual maneuver, and tests receive explicit-time validation methods rather than sleeping on `Time.time`.
- Use rating normalization `InverseLerp(40, 95, rating)`. SPD maps top speed to `6.4..9.6`, ACC maps forward acceleration to `13.5..22.5`, and AGI maps low/high-speed turn rates to `12..20`/`6..12`. Fatigue multiplies physical/action output by `Lerp(0.68, 1, stamina/100)` and never creates input.
- Stamina stays in `0..100`. Sustained input magnitude at or above `0.8` drains `Lerp(10, 4, normalized STA)` points/second; input at or below `0.25` recovers `Lerp(9, 13, normalized STA)` points/second; intermediate input neither drains nor recovers. Reset restores 100. A delta-time validation seam exercises the same calculation as runtime.
- Scale claim/carry from normalized CTR within bounded current-tuning ranges: claim radius `1.25..1.85`, claim speed `12..17`, and carry spring/damping `0.75..1.25` of baseline. For an intended pass, store the passer's normalized PAS on the puck and compute reception quality as `0.60 * receiver CTR + 0.40 * passer PAS`; this maps reception radius `1.4..2.1` and maximum controllable entry speed `4.5..7.5`. Deke adds at most `0.15` to normalized puck-protection score during its explicit window.
- Make shots deterministic. SHT maps the existing charged power by `0.85..1.20`; maximum angular deviation maps `6..1` degrees and is multiplied by a situation challenge clamped to `0..1`: `0.25 * missing charge + 0.20 * facing-to-goal angle/90° + 0.20 * distance-to-goal/rink length + 0.15 * puck-to-control-point distance/claim radius + 0.10 * lateral speed/maximum speed + 0.10 * fatigue loss`. Its sign comes from the facing/goal cross product (stable player-id parity only breaks an exact zero), not a random draw. Pure evaluation methods accept the six explicit normalized situation inputs for smoke checks, and the runtime path derives them from the actual skater, goal, puck, and movement geometry.
- Make pass imperfection deterministic from passing distance, passer facing angle, lateral movement, fatigue, and PAS. PAS maps launch pace by `0.88..1.08`, maximum deviation by `5..0.5` degrees, and lead by `0.32..0.55s`; zero-challenge clean geometry produces zero deviation for every rating. The deviation sign comes from the facing/target cross product, not randomness. Preserve physical interception and intended-receiver-only capture, with pure evaluation methods for repeatable checks.
- Resolve checks as deterministic attacker-versus-carrier contests after the existing range/cone gates. Body attack score is `0.35 STR + 0.15 DEF + 0.20 SPD + 0.10 AGI + 0.15 approach speed + 0.05 alignment`; body protection is `0.30 CTR + 0.30 STR + 0.15 AGI + 0.10 SPD + 0.10 fatigue + 0.05 contact position`, plus the bounded deke bonus. Pull attack score is `0.40 DEF + 0.20 AGI + 0.10 STR + 0.15 approach speed + 0.15 alignment`; pull protection is `0.45 CTR + 0.25 AGI + 0.15 STR + 0.10 fatigue + 0.05 contact position`, plus the deke bonus. All weighted inputs are normalized `0..1`; success requires attack to meet or exceed protection. Runtime `approach speed = InverseLerp(0, 8, max(0, dot(checker velocity - carrier velocity, checker-to-carrier direction)))`; `alignment = InverseLerp(-1, 1, dot(checker forward, checker-to-carrier direction))` for body and `InverseLerp(pull gate dot, 1, dot(...))` for pull; `contact position = InverseLerp(-1, 1, dot(carrier forward, carrier-to-checker direction))`, making square/front-side protection stronger than being caught from behind. The user's press inside valid range/cone while approaching is the timing input—there is no automatic attempt. Expose the geometry normalization and pure contest evaluator for stageable tests.
- Remove `HockeyPlayerAI`'s difficulty-based `Movement.SetSpeedScale` and stop feeding `ActionQuality` into pass/shot physical execution. Easy/Normal continue to differ only through decision interval, target error, shot charge choice, and tactical choices; both use their skater's attributes through the same gameplay path as a human.
- Use bounded interpolation around the current prototype tuning so midrange builds retain familiar playability and legacy serialized content remains valid.
- Assign role-oriented presets in `LocalMatchSetup` only as prototype defaults. Reject feature flags, environment variables, legacy parallel tuning modes, automatic decisions, and broad scene/prefab rewrites.
- This story needs an E2E spec because it changes user-facing controls and regression-prone core gameplay.

## Phased Tasks

### Phase 1 - Attribute contract and progression foundation

- [x] Add `Assets/_Project/Scripts/Player/PlayerAttributeBuild.cs` with the nine attributes, level budget, min/max bounds, progressive costs, atomic allocation/validation, normalized access, copy support, and role-oriented valid presets.
- [x] Update `GameplayContracts.cs` so DEKE is part of the shared input contract, then update `LocalPlayerInput.cs`, `PlayerInputController.cs`, and `HockeyPlayerAI.cs`; remove AI difficulty's direct movement/action-quality overrides while preserving its reaction/decision differences.
- [x] Update `GameplayData.cs` and `PlayerController.cs` so each skater owns a validated build and snapshots capture that build plus current stamina.

### Phase 2 - Physical execution and fatigue

- [x] Update `PlayerMovementController.cs` to accept independently bounded speed, acceleration, agility, and fatigue multipliers while preserving analog/camera-relative joystick direction and external impulses.
- [x] Add the specified deterministic stamina thresholds/rates to `PlayerController.cs`, apply the `0.68..1` performance factor to physical/action execution, expose an explicit-delta validation seam, and restore stamina on actor reset.
- [x] Add `DekeController.cs`, route only explicit DEKE presses from `PlayerController.cs`, and expose a bounded active window that improves control/evasion from CTR, AGI, speed, timing, and fatigue without generating movement.

### Phase 3 - Puck, shot, pass, and defensive integration

- [x] Update `StickPuckInteraction.cs`, `PuckController.cs`, and `PassReceivingZone.cs` so CTR/PAS, current motion, fatigue, and the explicit deke window affect claim, carry stability, puck-protection, and intended reception within safe bounds.
- [x] Update `ShootController.cs` with the specified bounded power/deviation mappings, geometry-derived sign, situation challenge, and pure evaluation seam while the user still selects timing.
- [x] Update `PassController.cs` with the specified PAS pace/deviation/lead mappings, geometry-derived sign, zero clean-lane deviation, and pure evaluation seam, with no random pass-failure roll.
- [x] Update `DefensiveCheckController.cs` with the specified normalized body/pull score formulas and pure contest evaluator so attributes, approach speed/alignment, contact, fatigue, and deke protection resolve attempts without guaranteed outcomes.
- [x] Update `LocalMatchSetup.cs` to assign Center=Playmaker, Left Wing=Sniper, Right Wing=Speed, Left Defense=Power, and Right Defense=Two-Way from the pinned level-25 presets while leaving AI difficulty settings unchanged.

### Phase 4 - Integration verification and documentation

- [x] Expand `PrototypeArenaSmokeCheck.cs` with repeatable assertions for allocation limits/progressive costs and exact presets, contrasting SPD/ACC/AGI builds, stamina degradation/recovery/reset, explicit deke routing, the combined CTR/PAS reception mapping, runtime shot geometry inputs, live check geometry normalization plus contested outcomes, snapshots, and AI-difficulty separation.
- [x] Update `README.md` with the nine-attribute model, prototype builds, stamina/fatigue effects, deterministic pass rule, direct-input principle, and revised smoke-check evidence.
- [x] Run the static excluded-services boundary query and verify it remains empty.
- [x] Compile and execute the Unity `6000.5.9f1` Phase 1 smoke check in batch mode, requiring zero compiler errors and `PHASE1_PVE_SMOKE_PASS`.
- [x] Execute every automated scenario in `.docs/tests/test-player-attributes-gameplay.md` and report the observed evidence in the final verification result without materially editing the reviewed scenario specification.

## Validation

- Static boundary: `rg -n -i 'Photon|Fusion|Unity\\.Netcode|Relay|Matchmaking|Firebase|AuthenticationService|Lobby' Assets Packages/manifest.json Packages/packages-lock.json ProjectSettings`; expected output is empty.
- Unity compile and integration: `/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath <isolated-project-copy> -executeMethod IceClash.Tests.Editor.Phase3SmokeRunner.Run -logFile <log>`; expected process exit `0`, no C# compiler errors, and `PHASE1_PVE_SMOKE_PASS` with named attribute assertions.
- E2E: execute `.docs/tests/test-player-attributes-gameplay.md` against the runtime-built arena; all automated scenarios must pass. Manual feel/balance observations may remain explicitly manual and do not substitute for automated contract checks.
- Review the final diff for direct-input preservation, deterministic pass behavior, bounded modifiers, serialization safety, AI separation, and accurate documentation.

## Rollback / Risk

- Attribute multipliers can destabilize physics if unbounded. Clamp all normalized values and effective speeds/forces around the existing known-good ranges, and validate extremes in the smoke test.
- Adding an interface member requires updating every input implementation in the same change; compilation is the gate.
- Runtime-added components and serializable fields preserve scene/prefab compatibility, but default builds must validate before play. Rollback is removal of the new build/deke files plus the focused consumer changes; no data migration or external state is involved.
- Balance values are prototype defaults, not final tuning. This story validates directionality, separation, constraints, and user agency rather than competitive balance.
