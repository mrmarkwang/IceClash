# Meshy Humanoid Validation Plan

## Goal

Produce an isolated, reproducible Unity validation setup proving whether the downloaded Meshy male character is a valid Humanoid and whether its supplied running motion plays without unacceptable deformation, while leaving all gameplay prefabs and systems untouched.

## Current Context

- The project uses Unity `6000.5.9f1` and already contains editor-side asset-generation patterns under `Assets/_Project/Tests/Editor/`.
- The requested destination `Assets/Characters/Male/Male_Base_v1/` does not currently exist.
- Downloads contains two binary FBX 7.4 files:
  - `/Users/markwang/Downloads/Meshy_AI_Navy_Training_Pose_biped/Meshy_AI_Navy_Training_Pose_biped_Character_output.fbx`
  - `/Users/markwang/Downloads/Meshy_AI_Navy_Training_Pose_biped/Meshy_AI_Navy_Training_Pose_biped_Animation_Running_withSkin.fbx`
- `Character_output.fbx` will be the canonical model/Avatar source. `Animation_Running_withSkin.fbx` will be imported alongside it as the supplied running source rather than conflating the animated export with the base prefab model.
- Existing production character assets and prefabs live under `Assets/_Project/`; they are outside this validation change.
- The working tree was clean during planning.

## Decisions

- Import both discovered Meshy FBXs into the requested folder because the download separates the base model from the explicitly named running export.
- Configure the base FBX as `Humanoid` with `Create From This Model`. Configure the running FBX as Humanoid and, when Unity permits, copy the Avatar from the base model so playback validates retargeting against the canonical character.
- Use a small, scoped Unity Editor validation utility to configure importers, create the controller/prefab/scene, and emit deterministic bone/clip evidence. This avoids relying only on undocumented Inspector state and makes the result repeatable.
- Validate required mappings through `Animator.GetBoneTransform(HumanBodyBones)` on an instantiated model using the generated Avatar; also open the importer Avatar configuration in Unity for visual confirmation.
- Build the test prefab from the base model with exactly one active Animator using the generated Avatar and temporary controller. No IceClash gameplay MonoBehaviours will be added.
- Create an isolated scene inside `Assets/Characters/Male/Male_Base_v1/` with the test prefab plus only minimal preview support.
- Perform deformation inspection visually in Play Mode from front, side, and rear/three-quarter views while Running loops. Automated mesh-weight changes are explicitly rejected.
- No feature flags, environment variables, compatibility layers, production prefab edits, or equipment/gameplay integrations are needed.

## Phased Tasks

### Phase 1 - Scope lock and source import

- [x] Recheck `git status --short` and record hashes/timestamps for both downloaded FBXs so the exact sources are identifiable.
- [x] Create `Assets/Characters/Male/Male_Base_v1/` and copy `Meshy_AI_Navy_Training_Pose_biped_Character_output.fbx` plus `Meshy_AI_Navy_Training_Pose_biped_Animation_Running_withSkin.fbx` into it without touching `Assets/_Project/Prefabs/HockeyPlayer.prefab`.
- [x] Allow Unity 6000.5.9f1 to import the copied files and record any model-import errors from the Console or batch log.

### Phase 2 - Humanoid importer and Avatar validation

- [x] Add a scoped Editor utility under `Assets/Characters/Male/Male_Base_v1/Editor/` with a top file comment block, constants for the two FBXs, validation prefab, controller, and scene, and clear failures for missing assets or invalid Avatar state.
- [x] Configure the base `ModelImporter` with `animationType = Human` and `avatarSetup = CreateFromThisModel`, save/reimport, and assert that its Avatar is non-null, `isHuman`, and `isValid`.
- [x] Configure the running FBX for Humanoid animation, using the base Avatar as the source when supported, then enumerate every imported `AnimationClip` sub-asset and record its exact name.
- [x] Instantiate the base model and assert non-null mappings for Hips, Spine, Chest, Neck, Head, bilateral Shoulder, UpperArm, LowerArm, Hand, UpperLeg, LowerLeg, and Foot transforms.
- [x] Open the base FBX Rig inspector in Unity, visually confirm Humanoid/Create From This Model with no importer errors, and confirm every required mapping through the scoped validator.

### Phase 3 - Temporary playback assets

- [x] Identify the supplied running clip by imported clip name/content, set its `ModelImporterClipAnimation.loopTime` value to true, and reimport without modifying the source mesh or weights.
- [x] Create `Male_Base_v1_Test.controller` with the running motion as its layer's default state and no gameplay parameters or transitions.
- [x] Create `Male_Base_v1_Test.prefab` from the canonical base model, ensure its Animator references the generated base Avatar and temporary controller, apply a neutral validation-only material for joint visibility, disable root motion for stationary inspection, and assert that no IceClash gameplay controller MonoBehaviours are present.
- [x] Create `Male_Base_v1_Test.unity` containing the prefab and only a close-framed camera, low-key light, and neutral ground needed for deformation inspection.
- [x] Save/refresh the generated assets and verify the production `HockeyPlayer.prefab` content hash or git status remains unchanged.

### Phase 4 - Automated and visual verification

- [x] Run Unity in batch mode against the scoped Editor utility and capture explicit pass/fail markers for Avatar validity, every required bone, discovered clip names, loop configuration, prefab Animator wiring, gameplay-script absence, and scene existence.
- [x] Open `Male_Base_v1_Test.unity`, enter Play Mode, and confirm the running state's normalized time advances and loops on `Male_Base_v1_Test`.
- [x] Inspect shoulders, armpits, elbows, wrists, hips, groin, knees, and ankles from front, side, and rear/three-quarter views during the full running cycle and record each visible defect by exact area, or explicitly record that none were observed.
- [x] Capture front, side, and rear images at multiple points in the running cycle as manual evidence without changing skin weights.

### Phase 5 - Review and completion evidence

- [x] Review `git diff` against the requirement, confirming only validation assets, their `.meta` files, the scoped Editor utility, and RPD documentation changed.
- [x] Record exact Unity batch command, exit code, pass markers, imported clip names, Avatar/bone results, playback outcome, deformation findings, prefab path, and scene path in the completion report.
- [x] Update this plan's checkboxes only after each corresponding asset, validation result, or visual observation exists.

## Validation

- Batch import/setup command: `\"/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity\" -batchmode -quit -projectPath /Users/markwang/mw/IceClash -executeMethod <scoped-validator-method> -logFile /tmp/iceclash-meshy-humanoid.log`.
- Expected batch evidence: exit code `0`; valid/human Avatar marker; one positive marker for every required bone; exact imported clip list; running loop/controller marker; prefab/scene marker; gameplay-script-absence marker.
- Manual test: follow `.docs/tests/test-meshy-humanoid-validation.md` in the Unity Editor and retain a screenshot plus written joint-area observations.
- Scope check: `git status --short` and `git diff -- Assets/_Project/Prefabs/HockeyPlayer.prefab Assets/_Project/Prefabs/Resources/Skater.prefab` must show no changes to gameplay prefabs.

## Rollback / Risk

- Binary FBX import may yield Unity-specific bone-name auto-mapping failures. Report the exact unmapped bone rather than silently overriding mappings or weights.
- The running export may contain its own skinned model and Avatar. Keeping it as a separate animation source avoids accidentally using it as the canonical prefab mesh.
- Meshy materials or embedded textures may render unexpectedly; material polish is out of scope unless visibility prevents deformation inspection.
- The first live preview was overexposed and too distant for trustworthy joint inspection. Use neutral validation-only materials, lower-key lighting, and close camera framing in the isolated prefab/scene; do not alter the imported mesh, skeleton, skin weights, or production materials.
- Root motion can move the character out of the inspection area; disabling Animator root motion on the test prefab keeps the preview stationary without modifying animation curves.
- Rollback is limited to removing the new `Assets/Characters/Male/Male_Base_v1/` assets and their metadata; existing production prefabs and systems remain untouched.
