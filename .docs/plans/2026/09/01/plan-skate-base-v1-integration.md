# Skate Base v1 Integration Plan

## Goal

Create and visually validate a preserved, reusable rigid hockey-skate asset fitted as removable left/right footwear on the trusted `Male_Base_v1` humanoid, then replace the gameplay cube placeholders so every red/blue runtime skater inherits the production pair without changing gameplay behavior.

## Current Context

- The project uses Unity `6000.5.9f1`; the executable is available under `/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity`.
- The source archive is `/Users/markwang/Downloads/Meshy_AI_Single_professional_i_0902012255_texture_fbx.zip`; it contains one FBX plus albedo, metallic, normal, and roughness textures.
- The pre-extraction ZIP SHA-256 is `ca529fa337583c06b37a30f07db63337d7094999e4a7cd443eec10f9733b6010`.
- The clean character prefab is `Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean_Test.prefab`; the unchanged running clip comes from `Assets/Characters/Male/Male_Base_v1/Meshy_AI_Navy_Training_Pose_biped_Animation_Running_withSkin.fbx`.
- `Assets/Equipment/Sticks/Hockey_Stick_Base_v1/Editor/HockeyStickBaseV1Setup.cs` establishes the repository pattern for additive importer configuration, generated PBR material, prefab/scene creation, animation sampling, evidence capture, protected hashes, and a generated Markdown report.
- Baseline protected hashes will include the clean/canonical FBXs and metas, clean prefab/scene, and running FBX/meta. Git is clean before work.
- Unity source inspection reports 4,253 vertices, 4,136 triangles, one material, and one rigid complete skate mesh, matching the face count expected by the brief. Unmodified renders are in `.docs/evidence/skate-base-v1/source-*.png`.
- `HockeyCharacterAssetSetup.BuildPlayer` already owns the modular `Skates` slot and paired-foot follower, but currently creates two cube primitives. `PrototypeArenaBootstrap` loads `Resources/Skater.prefab`, a connected variant of `HockeyPlayer.prefab`, for all ten red/blue skaters.

## Decisions

- Preserve all archive members unchanged under `Source/`; configure Unity import settings on `.meta` files only.
- Use an additive editor generator/validator under the skate asset's `Editor/` folder. It will create material assets, a canonical static prefab, a character fitting prefab, a dedicated scene, screenshots, and a deterministic report.
- Normalize the canonical prefab as `Skate_Base_v1/Visual` plus sibling `BladeContact`; keep all source-axis conversion and calibrated scale/offset below `Visual` so the equipment root stays identity.
- Derive the canonical `Mesh` asset from all 4,136 source faces after axis/scale normalization. Half-mesh experiments are explicitly rejected because each produces torn shells and floating components; the isometric near/far surfaces belong to one unusually wide Meshy boot. Create the opposite-hand `Mesh` asset offline by reflecting canonical vertices and reversing triangle winding/tangent handedness.
- Attach skate instances through independent foot-bone socket children found from `Animator.GetBoneTransform`, preserving foot-bone transforms exactly. Use the canonical and derived mirrored meshes with positive runtime scale; use no runtime negative scale.
- Use editor animation sampling over more than two running cycles for deterministic attachment validation and capture representative running views. This is an equipment stress test, not animation production.
- Capture evidence before considering mesh modification. No feature flags, environment variables, fallback attachment paths, compatibility layers, colliders, or topology edits are warranted.
- Replace only the existing primitive `Skates` item builder with prefab-backed `Skate_L_v1`/`Skate_R_v1` visuals. Preserve the current `HockeyEquipmentLoadout` and `HockeyPairedEquipmentFollower` contract rather than adding a second runtime attachment system.
- Calibrate gameplay placement from Humanoid foot positions and each prefab's `BladeContact`; preserve positive equal scale and character-forward orientation. Regenerate the canonical player, resource variant, and generated modular test scene through the existing deterministic generator.
- This asset-integration story warrants visual/E2E evidence because fit and animation behavior cannot be proven by compilation alone; the E2E spec is `.docs/tests/test-skate-base-v1-integration.md`.

## Phased Tasks

### Phase 1 - Preserve and inspect source

- [x] Record the ZIP hash and protected-asset hash baseline, extract to a temporary directory, and copy the unchanged FBX/PBR members into `Assets/Equipment/Skates/Skate_Base_v1/Source/`.
- [x] Verify the copied FBX hash matches the extracted archive member and inventory the supplied maps without modifying source bytes.
- [x] Inspect the imported FBX hierarchy, meshes, topology statistics, bounds, transforms, materials, axes, and major geometry integrity so normalization is based on evidence; unmodified renders show one rigid complete skate mesh, 4,253 vertices, 4,136 triangles, and no second complete skate or duplicated blade.

### Phase 2 - Build canonical skate assets

- [x] Complete `Assets/Equipment/Skates/Skate_Base_v1/Editor/SkateBaseV1Setup.cs` with deterministic importer, spatial boot extraction, offline positive-scale mirror generation, validation entry points, protected-hash checks, and no writes outside the skate asset/report outputs.
- [x] Generate `Skate_Base_v1_Canonical.asset` from all 4,136 source faces and `Skate_Base_v1_Mirrored.asset` by reflected vertices with corrected winding/tangent handedness; validate the canonical output retains the full source vertex/triangle counts and complete visible shell.
- [x] Generate the combined metallic/smoothness texture and `Materials/Skate_Base_v1.mat` from the preserved PBR maps without changing the originals.
- [x] Generate `Prefabs/Skate_Base_v1.prefab` from the canonical single-boot mesh with identity root, `Visual`, positive calibrated scale/orientation, `BladeContact`, static mesh renderers only, and adult-humanoid dimensions.

### Phase 3 - Fit left and right footwear

- [x] Generate a dedicated character fitting prefab that resolves left/right feet through `Animator.GetBoneTransform`, creates zero-position/unit-scale `LeftSkateSocket`/`RightSkateSocket` children with calibrated local yaw, and records unchanged foot-bone transforms.
- [x] Fit `Skate_L` from the canonical prefab and `Skate_R` from its offline mirrored derivative using equal positive scales and calibrated socket-local position/rotation so heel, toes, ankle, and blade orientation are visually correct.
- [x] Generate `Tests/Skate_Base_v1_Fitting.unity` with the clean validated humanoid, a single calibrated ice plane, validation lighting/camera, and no runtime gameplay scripts.

### Phase 4 - Validate and capture evidence

- [x] Validate prefab hierarchy, rigid/static renderers, identity root, equal positive scale, Humanoid bone lookup, Avatar validity, unchanged foot bones, blade-contact height agreement, material references, and source/protected hashes.
- [x] Sample the unchanged running clip over at least two complete cycles and assert both skate hierarchies remain attached with finite stable scale and no transform flips.
- [x] Capture all neutral, close-up, running, and low-gameplay views named in `.docs/tests/test-skate-base-v1-integration.md` before any optional topology change.
- [x] Review the rendered evidence for sock containment, heel/toe/ankle fit, blade and holder orientation, symmetry, contact, and running attachment; adjust only skate socket/prefab transforms and regenerate when evidence fails.

### Phase 5 - Report and regression verification

- [x] Generate `Skate_Base_v1_Validation.md` with source hashes, geometry/material data, axes, fitting transforms, contact values, Avatar state, animation result, evidence links, integrity observations, and known source limitations.
- [x] Compare protected hashes and `git diff --name-only` against the baseline to prove canonical humanoid, validated animation, gameplay/controller/camera/input/puck/stick files and existing gameplay prefabs are unchanged.
- [x] Run Unity batch generation/validation and the project's relevant EditMode tests or compilation check, record exact results, and mark tasks complete only when evidence exists.

### Phase 6 - Equip all gameplay skaters

- [ ] Update `HockeyCharacterAssetSetup` to load the validated left/right skate prefabs, replace the primitive `Skates` pair, place both `BladeContact` roots on the gameplay ice plane, and preserve the existing paired-equipment follower/loadout contract.
- [ ] Extend generated-asset validation to require both production skate prefab dependencies, exact rigid mesh/material structure, positive equal scale, correct left/right orientation, absence of cube placeholders, and connected `Resources/Skater` inheritance.
- [ ] Extend `PrototypeArenaSmokeCheck` to assert each of the ten runtime players has one active `Skates` slot containing exactly the validated left/right production visuals and an aligned follower.
- [ ] Regenerate `HockeyPlayer.prefab`, `Resources/Skater.prefab`, and `ModularCharacterTest.unity`, then run the existing modular-character batch validation and PrototypeArena smoke check.
- [ ] Capture and inspect gameplay evidence showing production skates on an actual spawned player in idle and running presentation states; record the new files and results in the validation report.
- [ ] Verify the clean/source hashes remain unchanged and the final change set contains no movement/controller/camera/input/puck/stick logic modifications.

## Validation

- Generate/import/capture: `/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/markwang/mw/IceClash -executeMethod IceClash.EquipmentValidation.Editor.SkateBaseV1Setup.GenerateValidateAndCaptureBatch -logFile /tmp/iceclash-skate-generation.log`; expect exit 0 and `SKATE_BASE_V1_VALIDATION_PASS`.
- Revalidate stable assets: same Unity invocation with `-executeMethod IceClash.EquipmentValidation.Editor.SkateBaseV1Setup.ValidateBatch`; expect exit 0 and the same pass marker.
- Compile/tests: detect available Unity test assemblies after import; run the narrowest EditMode suite when present, otherwise use the successful batch editor load/execute as the compilation check and state that no matching automated test assembly exists.
- Visual evidence: inspect every PNG in `.docs/evidence/skate-base-v1/` at full resolution against `.docs/tests/test-skate-base-v1-integration.md`.
- Integrity: `shasum -a 256` on every protected path before and after; exact equality is required. `git diff --name-only` must contain no prohibited path.
- Gameplay generation/validation: run `IceClash.Tests.Editor.HockeyCharacterAssetSetup.GenerateAndValidateBatch`; expect `MODULAR_CHARACTER_ASSETS_PASS` and production-skate validation for the canonical and resource prefabs.
- Runtime E2E: run the existing PrototypeArena play-mode smoke entry point and require all ten red/blue skaters to pass the production-skate assertion; capture idle/running gameplay evidence under `.docs/evidence/skate-base-v1/`.

## Rollback / Risk

- The primary risk is incorrect Meshy scale/axes or a visually plausible neutral fit that fails in animation. All adjustment is isolated to the generated `Visual` and socket transforms and is reversible by regenerating.
- The source is unusually wide/tall and has fragmented AI topology, making its near/far surfaces resemble a pair in one isometric angle. Evidence and the brief's exact face expectation establish that all faces belong to the canonical skate; any half extraction is forbidden because it tears the shell. The offline mirrored derivative must reverse triangle winding/tangent handedness to avoid inverted lighting.
- Headless rendering can differ from the interactive editor. Deterministic camera/light settings and direct inspection of captured PNGs mitigate this; visual acceptance is not inferred from batch success.
- Gameplay integration risk is scale-space mismatch between the 1.65× clean visual and 0.68× runtime player root. Validate `BladeContact`, foot containment, and animation at runtime rather than copying the fitting-scene offsets blindly.
- Rollback restores the generated player/resource/test-scene artifacts and the skate-builder/validator changes; preserved source and standalone skate assets remain valid.
