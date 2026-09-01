# Plan: Use Production Stick in Gameplay

## Scope and Decisions

Update the existing editor generator rather than hand-editing generated YAML. The generator will instantiate the production stick prefab as a nested prefab, uniformly fit its explicit `PrimaryGrip`→`BladeContact` segment from a torso-side upper grip to the unchanged gameplay blade/control point, orient its documented +Z blade-face normal toward the arena camera/up direction, and retain the existing marker objects used by IK and puck presentation. Production material references remain on the nested prefab.

The old FBX and material remain in the repository for provenance but must not be dependencies of either gameplay prefab. No runtime feature flag, fallback model, compatibility path, or mechanics change is needed.

## Phased Tasks

### Phase 1 - Baseline and contract lock

- [x] Trace `PrototypeArenaBootstrap` through `Resources/Skater.prefab`, `HockeyPlayer.prefab`, and `HockeyCharacterAssetSetup.BuildStick` to identify the rendered legacy dependency.
- [x] Record pre-change hashes for the generator, both gameplay prefabs, PrototypeArena scene, `StickPuckInteraction`, and `HockeyStickRig`.
- [x] Record the non-goals: preserve the blade/control point, marker contract, IK components, puck logic, controller, camera, input, and match systems.

### Phase 2 - Generator integration

- [x] Update `HockeyCharacterAssetSetup.cs` constants and generation flow to load the production prefab/material and keep its FBX non-readable and unanimated.
- [x] Replace low-poly vertex fitting with marker-based uniform fitting from `PrimaryGrip`/`BladeContact` to the generated reference-carry grip and unchanged `Stick Blade` control target.
- [x] Update generator validation to require the production nested prefab, PBR material, 4,347-triangle rigid mesh, correct marker reach, arena renderer policy, and absence of legacy dependencies.

### Phase 3 - Generated gameplay assets

- [x] Run `IceClash > Generate Modular Hockey Character` in Unity to regenerate `HockeyPlayer.prefab`, `Resources/Skater.prefab`, and `ModularCharacterTest.unity`.
- [x] Run `IceClash > Validate Modular Hockey Character Assets` and record `MODULAR_CHARACTER_ASSETS_VALID`.
- [x] Confirm `Resources/Skater.prefab` remains a connected variant of `HockeyPlayer.prefab`.

### Phase 4 - Gameplay visual verification

- [x] Run the PrototypeArena smoke check and enter Play Mode without changing the scene asset.
- [x] Capture a useful arena view proving active skaters render the dark production stick instead of the orange legacy visual.
- [x] Verify the production stick follows the existing two-hand rig and blade/control point remains aligned.

### Phase 5 - Preservation and reporting

- [x] Recompute protected source hashes and confirm all gameplay-system and scene sources remain unchanged.
- [x] Record prefab dependency evidence and the observed gameplay visibility/fit result.
- [x] Ensure Git changes contain only the generator, regenerated character prefabs/test scene, new evidence, and story documentation in addition to the prior production-stick assets.

### Phase 6 - Reference carry correction

- [x] Replace the nearly level grip/blade target triangle in `HockeyCharacterAssetSetup.BuildPlayer` with a higher torso-side primary grip and a lower, forward secondary grip while retaining the existing gameplay blade/control point.
- [x] Update generated shaft-reference geometry and validation so both authored grip markers contact their IK targets and the shaft has measurable lateral, vertical, and forward diagonal separation.
- [x] Regenerate `HockeyPlayer.prefab` and `ModularCharacterTest.unity`, then run modular asset validation and the full arena smoke suite.
- [x] Capture fresh `PrototypeArena` gameplay evidence showing the diagonal two-hand hockey carry and record its visibility/fit result.

## Validation

- Unity generation: `IceClash > Generate Modular Hockey Character`.
- Unity asset validation: `IceClash > Validate Modular Hockey Character Assets`; expect `MODULAR_CHARACTER_ASSETS_VALID`.
- Arena smoke check: `IceClash > Run Phase 1 PvE Smoke Check`; expect the existing pass marker.
- Prefab dependencies: production prefab/material present; legacy `hockey_stick_002.fbx` and `LowPolyHockeyStick.mat` absent.
- Visual: screenshot from `PrototypeArena` Play Mode with dark production sticks visible on active skaters.
- Hashes: protected gameplay source/scene hashes match their pre-change values.

## Rollback / Risk

Rollback is restoration of the generator and regeneration of the prior prefabs. Main risks are marker-fit roll orientation, reduced arena readability of the dark material, and accidental loss of nested-prefab or gameplay marker references. Validation checks those boundaries directly.

## Architecture Review

AR passed: no blocking architecture flaws

Evidence: this follows the existing generator-owned equipment replacement architecture; stays within character presentation; changes no public API, schema, persistence, auth, security/privacy, external dependency, infrastructure, concurrency, performance, availability, or reliability contract; retains all runtime marker and mechanics interfaces; is reversible by generator rollback; and has explicit dependency, hash, Unity validation, and visual acceptance evidence.

Pose-correction AR passed: no blocking architecture flaws. The correction remains low risk because it changes only generator-owned presentation coordinates within the existing character subsystem, preserves the blade/control point and every runtime API/reference, introduces no schema/dependency/feature flag/fallback/concurrency/performance behavior, is immediately reversible, and has unambiguous marker, smoke, and screenshot checks.
