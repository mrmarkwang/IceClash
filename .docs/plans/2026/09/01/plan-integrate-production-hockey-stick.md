# Plan: Integrate Production Hockey Stick

## Scope and Decisions

The source archive contains one unambiguous FBX and four textures. Extract copies into the new equipment directory, keeping texture filenames compatible with FBX references while renaming only the imported FBX canonically. Use an editor-only generator to configure the model as a rigid asset, measure it using Unity's imported mesh/render data, construct normalized prefabs and an isolated test scene, render evidence, and write a machine-derived validation report. Preserve source geometry and all existing humanoid/gameplay files.

No feature flags, fallback models, runtime scripts, gameplay integration, IK, or compatibility layers are needed. A manual visual E2E spec is required because scale, orientation, grip fit, material rendering, and blade height are observable visual contracts.

## Phased Tasks

### Phase 1 - Discovery and preservation

- [x] Inspect `/Users/markwang/Downloads/Meshy_AI_Single_professional_i_0901053710_texture_fbx.zip` and confirm there is exactly one matching FBX plus its texture payload.
- [x] Record SHA-256 baselines for the clean FBX, its importer metadata, clean validation prefab, and clean validation scene before creating stick assets.
- [x] Confirm the worktree contains unrelated in-progress humanoid/gameplay integration changes that must remain untouched.

### Phase 2 - Source import and measurement

- [x] Copy the archive's FBX as `Assets/Equipment/Sticks/Hockey_Stick_Base_v1/Meshy_Hockey_Stick_Base_v1.fbx` and copy its supplied textures without altering the download.
- [x] Add an editor-only `HockeyStickBaseV1Setup` generator/validator with file comment block to configure rigid import and report Unity mesh, material, texture, bounds, pivot, and animation/rig state.
- [x] Run Unity batch import and use the measured longest axis and bounds to determine the documented source orientation and non-destructive normalization scale.

### Phase 3 - Prefabs and scene

- [x] Generate `Hockey_Stick_Base_v1.prefab` with normalized `Model`, `PrimaryGrip`, `SecondaryGrip`, and `BladeContact` transforms and no runtime/gameplay components.
- [x] Generate `Male_Base_v1_Stick_Test.prefab` from the existing clean validation prefab, attach `StickSocket` beneath the mapped `RightHand`, and preserve the source humanoid hierarchy/assets.
- [x] Generate `Hockey_Stick_Base_v1_Test.unity` with neutral ground, lighting, and a useful validation camera.

### Phase 4 - Validation and evidence

- [x] Validate geometry counts, dimensions, material/texture assignments, rigid import state, transform conventions, grip/contact placement, hand bone/socket wiring, and absence of forbidden components.
- [x] Render front, side, rear, grip close-up, and blade close-up PNGs according to `.docs/tests/test-integrate-production-hockey-stick.md`.
- [x] Recompute protected humanoid hashes and confirm they match the Phase 1 baselines.

### Phase 5 - Reporting

- [x] Generate `Hockey_Stick_Base_v1_Validation.md` with source, geometry, materials, transform, grip, player integration, visual evidence, and known-issues sections.
- [x] Run the relevant Unity Editor validation command and record its pass marker and exact measurements.
- [x] Inspect all five captures and record visible defects without expanding into excluded gameplay or IK work.

## Validation

- Unity import/generation: run `IceClash > Equipment Validation > Generate, Validate and Capture Hockey Stick Base v1` in the already-open Unity 6000.5.9f1 Editor (batch mode was unavailable because the project was open).
- Confirm log contains `HOCKEY_STICK_VALIDATION_PASS` and no compile/import exception.
- Compare `shasum -a 256` results for the four protected humanoid assets with the captured baseline.
- Inspect five rendered PNGs for orientation, relative scale, grip clipping, blade height, and material defects.

## Rollback / Risk

All implementation assets are additive under the new equipment directory plus story documentation/evidence. Rollback is removal of those additive files. Key risks are ambiguous Meshy axes/pivot, texture workflow mismatch (roughness versus Unity smoothness), and a neutral imported pose that may not naturally resemble a two-handed hockey stance; validation must report rather than hide those limitations.

## Architecture Review

AR passed: no blocking architecture flaws

Evidence: the plan follows the existing editor-generator/isolated-validation-scene pattern in `MaleBaseV11CleanValidationSetup`; stays within the new equipment validation subsystem; changes no public API, schema, persistence, migration, auth, security/privacy, dependency contract, infrastructure, concurrency, performance, availability, or reliability behavior; is fully additive/reversible; and has explicit asset paths, pass markers, hashes, measurements, and visual acceptance evidence.
