# Wearable Equipment Slots Plan

## Goal

Make helmet, visor, gloves, and skates the only independently replaceable character wearables while preserving the separately equipped gameplay stick and folding all clothing/padding responsibility into the main character visual.

## Current Context

- `HockeyEquipmentLoadout.cs` currently serializes eight enum values and contains jersey/sock material and paired-sock behavior.
- `HockeyCharacterAssetSetup.cs` generates the canonical prefab, resource variant, modular test scene, and separate primitive objects for shoulder pads, jersey, pants, and socks.
- `LocalMatchSetup.cs` currently calls `HockeyEquipmentLoadout.SetJerseyMaterial` for team differentiation, so team coloring must move to the main character presentation before that equipment API is removed.
- `ModularCharacterTestHarness.cs` and editor persistence validation iterate every enum value and currently require all eight generated items.
- `HockeyPlayer.prefab`, `Resources/Skater.prefab`, and `ModularCharacterTest.unity` are generated outputs and must be regenerated from the updated generator.
- Retained serialized values are Helmet `0`, Gloves `2`, Skates `4`, and Stick `5`; Visor can use a new value without reusing a removed serialized value.

## Decisions

- Keep Stick as a non-wearable equipment binding because existing gameplay, IK, and puck-control code depends on it; the user's four-item rule applies to character wearables. Preserve its implementation boundary rather than expanding this story into pre-existing stick/puck repair.
- Preserve retained enum numeric values, assign Visor value `8`, and assert every retained/new numeric value in editor validation to avoid silent remapping of existing prefab data.
- Generate a distinct visor object under the head rather than merging it with the helmet, because it must be independently replaceable.
- Move team-material application to `HockeyCharacterPresentation`, targeting only the captured main-character renderers, so clothing remains part of the character visual without recoloring separately equipped wearables.
- Remove unsupported enum members, generator builders, serialized offsets, runtime binding branches, and validation assumptions instead of retaining compatibility aliases or hidden fallback slots.
- Continue using the established generator as the source of truth and regenerate checked-in Unity assets; do not hand-maintain divergent prefab YAML.
- Do not alter the production stick, character mesh, animation, movement, or puck systems.

## Phased Tasks

### Phase 1 - Lock the supported equipment contract

- [x] Update `HockeyEquipmentLoadout.cs` so the enum contains Helmet, Visor, Gloves, Skates, and Stick with retained numeric IDs unchanged.
- [x] Remove jersey/sock-specific material, offsets, capture, and paired-follow behavior from `HockeyEquipmentLoadout.cs` while preserving glove, skate, and stick behavior.
- [x] Add exact enum-value assertions to `HockeyCharacterAssetSetup.cs` for Helmet `0`, Gloves `2`, Skates `4`, Stick `5`, and Visor `8`.
- [x] Confirm repository references no longer require ShoulderPads, Jersey, Pants, or Socks enum members or `SetJerseyMaterial`.

### Phase 2 - Preserve team coloring on the main visual

- [x] Update `HockeyCharacterPresentation.cs` to serialize the main-character renderer set and expose team-material application without traversing supported equipment renderers.
- [x] Update `HockeyCharacterAssetSetup.cs` to capture and configure the main-character renderers before equipment is added.
- [x] Update `LocalMatchSetup.cs` to apply blue/red materials through `HockeyCharacterPresentation` for both skaters and goalies.
- [x] Add production-arena validation that both teams still receive distinct main-character materials after roster creation.

### Phase 3 - Update generated character assets

- [x] Update `HockeyCharacterAssetSetup.cs` to generate five bindings: Helmet, Visor, Gloves, Skates, and Stick.
- [x] Remove unsupported shoulder-pad, jersey, pants, and sock builder calls plus the dedicated shoulder-pad builder while retaining shared primitive helpers needed by supported wearables.
- [x] Add structural validation in `HockeyCharacterAssetSetup.cs` that rejects removed slot anchors/renderers, requires the distinct visor binding, and inspects the canonical prefab, resource variant, and every generated modular-scene player.
- [x] Regenerate `HockeyPlayer.prefab`, `Resources/Skater.prefab`, and `ModularCharacterTest.unity` from the updated generator.

### Phase 4 - Update behavioral validation

- [x] Update `ModularCharacterTestHarness.cs` to exercise replacement independence for the four wearables plus Stick without jersey/sock assumptions.
- [x] Update paired-equipment checks in `ModularCharacterTestHarness.cs` to cover only gloves and skates.
- [x] Update `TwoHandHockeyPoseEvidence.cs` only if its binding filter requires a concrete change after the new exact slot set is generated; otherwise record that its generic filter remains valid.

### Phase 5 - Verification

- [x] Run `HockeyCharacterAssetSetup.ValidateSupportedEquipmentContract` and require `SUPPORTED_EQUIPMENT_CONTRACT_PASS` for exact bindings, stable IDs, generated artifacts, and clear/replace persistence.
- [x] Run `ModularCharacterSmokeRunner.RunBatch`, require `SUPPORTED_WEARABLE_RUNTIME_PASS`, and record downstream pre-existing stick failures without treating them as evidence for this scoped no-regression boundary or weakening them.
- [x] Run `Phase3SmokeRunner.Run`, require `TEAM_CHARACTER_MATERIAL_PASS`, and record any unrelated full-suite failures without weakening them.
- [x] Run the scoped wearable equipment E2E inspection and verify removed object names are absent from the generated prefab and scene.

### Phase 6 - Status and evidence

- [x] Record the exact generation and smoke-test results in this plan's validation evidence.
- [x] Confirm every acceptance criterion has concrete code, generated-asset, and test evidence before completion.

## Validation

- Focused asset contract: run `IceClash > Validate Supported Equipment Contract` in the open editor (or execute `IceClash.Tests.Editor.HockeyCharacterAssetSetup.ValidateSupportedEquipmentContract` in batch); require `SUPPORTED_EQUIPMENT_CONTRACT_PASS`.
- Play Mode smoke: run `IceClash > Run Modular Character Smoke Check`; require `SUPPORTED_WEARABLE_RUNTIME_PASS`. Record, but do not conceal or weaken, the later pre-existing stick-pose failure; verify the scoped diff does not modify stick/puck implementations and the focused replacement check preserves stable targets plus SecondaryGrip rebinding.
- Full gameplay smoke: run `IceClash > Run Phase 1 PvE Smoke Check`; require `TEAM_CHARACTER_MATERIAL_PASS`. Record, but do not conceal or weaken, unrelated legacy full-suite failures.
- Structural inspection: confirm numeric enum values; confirm bindings are exactly Helmet, Visor, Gloves, Skates, and Stick across `HockeyPlayer.prefab`, `Resources/Skater.prefab`, and every generated modular-scene player; confirm removed anchors and primitive names are absent.
- E2E scenario: follow `.docs/tests/test-wearable-equipment-slots.md` after asset regeneration.

## Rollback / Risk

- Enum serialization is the primary risk. Retained numeric IDs stay unchanged and Visor receives a new ID; removed IDs are not reused.
- Generated prefabs and scenes may have broad YAML churn. Regenerate through the established idempotent tool and rely on its second-pass stable-GUID validation.
- Removing Stick would break core gameplay; it is explicitly preserved as non-wearable equipment.
- Rollback consists of reverting the loadout, generator, harness, evidence filter, and generated prefab/scene changes together.

## Validation Evidence

- Unity script compilation: Tundra build succeeded with no C# errors; only existing obsolete-API warnings were emitted.
- Focused equipment contract: `SUPPORTED_EQUIPMENT_CONTRACT_PASS slots=Helmet,Visor,Gloves,Skates,Stick persistence=true players=10`.
- Runtime wearable behavior: `SUPPORTED_WEARABLE_RUNTIME_PASS slots=Helmet,Visor,Gloves,Skates stickBindingPreserved=true` proves paired following, per-slot independence, stable IK targets, and SecondaryGrip rebinding before the pre-existing downstream failure `Both hand targets are not on the rendered stick shaft`.
- Production team coloring: `TEAM_CHARACTER_MATERIAL_PASS teams=Blue,Red wearablesExcluded=true` before unrelated existing full-suite failures (`modularCharacter=False` from the stick integration and pass-outcome regressions).
- Broader asset validation reached `SUPPORTED_EQUIPMENT_STRUCTURE_PASS slots=Helmet,Visor,Gloves,Skates,Stick players=10`, then hit the pre-existing contradictory professional-stick assertion (`PrimaryGripPose.y - BladePose.y` is `0.47` while the validator requires at least `0.8`). The failure remains enforced and is not claimed as feature evidence.
- Scoped diff inspection confirms no production stick prefab, `HockeyStickRig`, puck controller/interaction, grip-marker builder, or gameplay control-point implementation was changed by this story.
- Static E2E inspection found prefab bindings exactly `0, 8, 2, 4, 5` and no shoulder-pad, jersey, pants, or socks slot/placeholder names in the generated prefab or modular test scene.
