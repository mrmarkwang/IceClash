# Integrated-Skates Production Integration Plan

## Goal

Make the validated Meshy integrated-skates character the reproducible production visual for `HockeyPlayer`, with regenerated Humanoid animation, equipment, scale, blade contact, and two-hand stick IK contracts that pass the existing asset and gameplay validations.

## Current Context

- The validated model is `Assets/Characters/Male/Male_Base_v1/Models/Meshy_AI_Hockey_Player_Charact_biped_Character_output.fbx`; Unity reports a valid Humanoid Avatar and an approximately `2.126 m` raw rendered height.
- `MaleBaseV1ReplacementCompatibility.cs` redirects model/prefab constants to the replacement but points the production controller at Air Squat and throws from production generation/alignment.
- `HockeyCharacterAssetSetup.BuildPlayer` already resolves hands and feet through `Animator.GetBoneTransform`, so rebuilding the prefab can safely bind the new skeleton once legacy skate assumptions are removed.
- Runtime skaters retain a root scale of `0.68` in `LocalMatchSetup`. A target world visual height of `1.90 m` therefore requires a generated prefab-local visual height of about `2.794 m`; the exact uniform factor must be calculated from renderer bounds rather than hard-coded from the validation report.
- `Assets/_Project/Art/HockeyPrototype/Idle.anim` and `Skate.anim` are committed Humanoid muscle clips with loop settings and no skeleton-path bindings. The latter is a suitable placeholder replacement for the deleted Running FBX.
- `HockeyEquipmentLoadout.IsComplete` requires five unique anchors but does not require rendered equipment. Existing smoke tests do require an active skate item and currently assert detachable skate geometry, so the migration needs an active, non-rendering integrated-skates marker plus updated assertions.
- The planning baseline contains no modified tracked files; only this story's new RPD documents are untracked. Any unrelated changes that appear while Unity is open remain user-owned and must be preserved.

## Decisions

- Replace `MaleBaseV1ReplacementCompatibility.cs` with a correctly named `MaleBaseIntegratedSkatesGameplayIntegrationSetup` production setup and update callers. Do not retain the misleading legacy class/API, introduce a second generator, or add a fallback character path.
- Generate a production `MaleSkater.controller` under the replacement `Male_Base_v1/Animations` folder. Preserve the existing presentation parameter names and `Idle`/`Running` state names, sourcing their motions from the stable procedural Humanoid `Idle.anim` and `Skate.anim` assets.
- Instantiate the source model rather than the validation prefab so production never inherits the Air Squat controller. Assign the replacement Avatar and production controller explicitly.
- Calculate uniform visual scale from evaluated renderer bounds so the retained `0.68` actor scale produces a `1.90 m` world visual. Translate `Visual`, not the gameplay root, until the lowest rendered point of the integrated blades meets the established local gameplay ice contact plane.
- Preserve the `Skates` binding with an active empty `Integrated Skates` GameObject. Do not add a follower or standalone skate meshes, and configure unmasked and masked mesh references to the same integrated character mesh so equipment lifecycle calls cannot hide its feet/skates.
- Reuse the existing Humanoid-based hand, foot, socket, target, constraint, and stick construction. Update validations from detachable-skate triangle/BladeContact checks to integrated-mesh presence, marker identity, foot binding, duplicate-renderer absence, scale, and contact-plane checks.
- Update both deterministic modular and full gameplay smoke expectations. E2E coverage is required because prefab generation feeds resource loading, runtime composition, animation, IK, equipment, and gameplay scenes.
- Do not add flags, environment variables, alternate prefab paths, restored assets, or runtime migration code.

## Phased Tasks

### Phase 1 - Discovery and scope lock

- [x] Inspect `MaleBaseV1ReplacementCompatibility.cs`, `HockeyCharacterAssetSetup.cs`, `HockeyEquipmentLoadout.cs`, `HockeyCharacterPresentation.cs`, `LocalMatchSetup.cs`, production prefabs/controllers, and current Git status to identify retired dependencies and preserved contracts.
- [x] Confirm `Assets/_Project/Art/HockeyPrototype/Skate.anim` is a looping Humanoid muscle clip suitable for the temporary `Running` state and that Air Squat remains validation-only.
- [x] Record the retained `0.68` runtime actor scale, approximately `2.126 m` raw replacement height, five equipment slots, connected resource variant, and user-owned Unity serialization changes.

### Phase 2 - Production generation foundation

- [ ] Replace `MaleBaseV1ReplacementCompatibility.cs` with `MaleBaseIntegratedSkatesGameplayIntegrationSetup.cs`, update callers, and implement idempotent controller creation plus bounds-based scale/blade-plane alignment without legacy exceptions.
- [ ] Generate the production controller at `Assets/Characters/Male/Male_Base_v1/Animations/MaleSkater.controller` with the established parameters/transitions, procedural `Idle.anim`, procedural `Skate.anim` as `Running`, and no Air Squat or deleted FBX reference.
- [ ] Update `HockeyCharacterAssetSetup.cs` to instantiate the replacement model directly, assign the valid Avatar/controller, and retain root-motion-off renderer optimization.

### Phase 3 - Integrated equipment and IK migration

- [ ] Remove detachable-skate importer, prefab, material, masking, follower, fitting, and alignment calls from `HockeyCharacterAssetSetup.cs` while leaving unrelated equipment and stick behavior intact.
- [ ] Generate an active non-rendering `Integrated Skates` item under the stable `SkatesSlot`, bind the slot to the replacement Humanoid feet, and prevent mesh masking from changing the integrated body/skates renderer.
- [ ] Rebuild `StickSocket`, grip targets, elbow hints, two `TwoBoneIKConstraint` objects, `RigBuilder` layers, and `HockeyStickRig` references from the replacement Avatar's mapped arm and hand bones.
- [ ] Calibrate uniform character scale and `Visual` translation from renderer bounds, then align stick presentation after the final visual transform so the blade/control-point contract remains valid.
- [ ] Regenerate `HockeyPlayer.prefab`, its connected `Resources/Skater.prefab` variant, and `ModularCharacterTest.unity` without missing serialized bone or equipment references.

### Phase 4 - Validation and regression coverage

- [ ] Update `HockeyCharacterAssetSetup` validators to require the integrated marker, replacement Avatar/controller, target height/contact tolerances, no detachable skate instances, no retired GUID/path dependencies, complete equipment slots, valid IK, and stable GUIDs across regeneration.
- [ ] Update `ModularCharacterTestHarness.cs`, `PrototypeArenaSmokeCheck.cs`, and relevant editor runners so integrated skates satisfy runtime equipment checks without weakening animation, stick, roster, or puck assertions.
- [ ] Run the Unity batch asset generator/validator and require `MODULAR_CHARACTER_ASSETS_PASS` with no compiler, missing-reference, Avatar, or idempotence failure.
- [ ] Run the modular ten-player smoke scene and require `MODULAR_CHARACTER_SMOKE_PASS players=10` with visible Humanoid Running movement and no duplicate skates.
- [ ] Run the PrototypeArena smoke runner and require its existing gameplay pass marker, ten skaters, two goalies, bound presentation, correct scale, two-hand IK, and puck behavior.
- [ ] Inspect generated prefab/controller YAML and `git grep` results to confirm production assets contain no retired model/animation GUIDs, Air Squat motion, or standalone skate-prefab GUIDs.

### Phase 5 - Review and completion evidence

- [ ] Review the scoped Git diff against the requirement, preserving any unrelated changes that appear during implementation and avoiding changes to `PlayerController`, input, puck, AI, or balance code.
- [ ] Inspect the generated test scene and PrototypeArena visually for height, blade contact, duplicate skates, deformation, stick grip, and animation behavior.
- [ ] Record exact verification commands, pass markers, limitations, and final acceptance status in the RPD documents.

## Validation

- Run `/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/markwang/mw/IceClash -executeMethod IceClash.Tests.Editor.HockeyCharacterAssetSetup.GenerateAndValidateBatch -logFile /tmp/iceclash-integrated-production-assets.log -quit`; require exit code `0` and `MODULAR_CHARACTER_ASSETS_PASS`.
- Run the committed modular smoke runner against `Assets/_Project/Scenes/ModularCharacterTest.unity`; require `MODULAR_CHARACTER_SMOKE_PASS players=10` and no `MODULAR_CHARACTER_SMOKE_FAIL`.
- Run the committed PrototypeArena/Phase 3 smoke runner; require its existing overall pass marker and no modular-character, equipment, animation, IK, or gameplay regression failure.
- Inspect the generated `HockeyPlayer.prefab` and `Resources/Skater.prefab` through Unity APIs: one valid Humanoid Animator, root motion off, five anchors, active integrated marker, zero standalone skate renderers/followers, two hand constraints, valid rig targets, connected variant, approximately `1.90 m` runtime visual height, and blade contact at the configured ice plane.
- Run `git grep`/GUID checks across production assets for the retired clean-visual prefab, deleted Running FBX, standalone skate prefabs, and Air Squat controller; require no production references.
- Confirm `git diff -- Assets/_Project/Scripts/Player/PlayerController.cs` is empty and separately report preserved pre-existing changes.

## Rollback / Risk

- Bounds are evaluated from a skinned Humanoid and may change slightly by pose. Perform calibration in a deterministic bind/Idle pose and validate with a tolerance rather than exact float equality.
- Runtime root scaling affects visual height, IK, equipment, and contact coordinates. Preserve `LocalMatchSetup`'s established `0.68` value and compensate only inside the production visual hierarchy.
- The integrated skates cannot satisfy old detachable-skate mesh/marker assertions literally. Replace those assertions with equivalent integrated-mesh ownership, duplicate absence, foot binding, and ice-contact guarantees; do not remove the five-slot contract.
- The procedural Skate clip is intentionally a placeholder, not production mocap. Rollback can restore the prior controller/prefab generator from Git, but deleted source assets must not be silently recreated.
- Unity may rewrite generated YAML while the interactive editor is open. Review overlaps carefully and never discard user-owned changes wholesale.
