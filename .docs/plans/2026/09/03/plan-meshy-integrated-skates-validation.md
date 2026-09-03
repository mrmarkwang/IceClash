# Meshy Integrated-Skates Character Validation Plan

## Goal

Create and validate an isolated Unity Humanoid asset set for the Meshy integrated-skates male player, with reproducible evidence and no changes to v1 or live gameplay.

## Current Context

- Unity project: `/Users/markwang/mw/IceClash`, Unity `6000.5.9f1`.
- Source ZIP: `/Users/markwang/Downloads/Meshy_AI_Hockey_Player_Charact_biped.zip` (20,577,299 bytes), containing two FBXs and four external PNG textures.
- Canonical protected asset: `Assets/Characters/Male/Male_Base_v1/Meshy_AI_Navy_Training_Pose_biped_Character_output.fbx` with pre-change SHA-256 `5427221743566b2db9c893355373c14236853cac0b0105fd1e391ebee88acfdd`.
- The repository has a pre-existing modification to `ProjectSettings/ProjectSettings.asset`; it is outside this task and must be preserved.
- An installed matching Unity Editor is available. Existing v1 editor automation demonstrates the local import, prefab, scene, and evidence pattern, but v1 itself must not be touched.

## Decisions

- Keep source model, animation, textures, generated materials, controller, prefab, scene, and editor validation tooling in named v2 subfolders under the requested root.
- Configure the character FBX with its own Humanoid Avatar. Import the animation FBX as Humanoid motion copied onto that Avatar, non-looping, with root motion disabled on the test Animator.
- Create a material that explicitly references the external textures instead of extracting or rewriting FBX source data.
- Use Air Squat only in an isolated controller and scene. Do not introduce feature flags, environment variables, compatibility layers, gameplay hooks, standalone skates, or animation repairs.
- Generate numeric evidence from Unity import/runtime APIs and visual evidence by sampling representative frames from front, side, rear, and skate-close views.
- E2E coverage is unnecessary because this is isolated asset import/validation with no user-facing gameplay flow or public contract; batch Unity generation plus scene/image inspection is the end-to-end evidence.

## Phased Tasks

### Phase 1 - Discovery and scope lock

- [x] Record Git status and hashes of every existing v1 FBX, the live scene set, gameplay scripts, and the existing player prefab before changes; note that the requested `Assets/Scenes/Game.unity` path does not exist in this checkout.
- [x] Inspect the ZIP inventory, file sizes, hashes, FBX/material/texture arrangement, and confirm no hockey-forward clip is present.
- [x] Confirm the new target directory is absent or contains no unrelated assets before import.
- [x] Record the task non-goals so no v1, gameplay, rig-authoring, or skating-animation repair work is introduced.

### Phase 2 - Isolated asset import and Humanoid setup

- [x] Create the v2 `Models`, `Animations`, `Textures`, `Materials`, `Prefabs`, and `Editor` folders and copy only the relevant ZIP contents into them.
- [x] Add v2-specific Unity Editor automation that configures the character importer as Humanoid/Create From This Model and stops on an invalid or non-human Avatar.
- [x] Configure the Air Squat FBX as non-looping Humanoid validation motion using the v2 Avatar, without modifying its source skeleton or mesh.
- [x] Create external Unity materials and retain the supplied color, normal, metallic, and roughness textures; wire the directly supported color, normal, and metallic inputs while preserving roughness as an external source map.

### Phase 3 - Test assets and evidence

- [x] Create `Prefabs/Male_Base_v2_IntegratedSkates_Test.prefab` with the imported character and Animator only, using the v2 Avatar and no gameplay scripts or physics.
- [x] Create `Male_Base_v2_IntegratedSkates_Test.unity` with the prefab, neutral reference floor, front/side/rear cameras, and simple lighting.
- [x] Generate an isolated Air Squat controller with root motion disabled and Air Squat as the default validation state.
- [x] Generate Unity evidence covering model/mesh statistics, hierarchy and humanoid mapping, scale, clip metadata, lower skate-region bone influences, per-phase blade/contact bounds, and asset wiring.
- [x] Capture neutral, representative squat phases, deepest-squat front/side/rear, and skate-close screenshots when Unity rendering is available.

### Phase 4 - Verification

- [x] Run the Unity batch generator/validator and require the explicit validation-pass marker with no compiler/import exceptions.
- [x] Inspect rendered evidence for pelvis, knees, groin/shorts, ankles, skate attachment/orientation, blades, shoulders, elbows, wrists, tearing, and severe skin-weight deformation.
- [x] Verify the prefab has no gameplay MonoBehaviours or physics and the scene is separate from the live scene set; note that `Assets/Scenes/Game.unity` is absent.
- [x] Recompute v1, gameplay script, and existing player prefab hashes and compare them with the pre-change values; verify no tracked scene is changed.
- [x] Review the final Git diff/status and confirm every task-created path is scoped to v2 or validation documentation/evidence.

### Phase 5 - Status

- [x] Record exact commands and observable validation evidence in the final report.
- [x] Mark acceptance criteria complete only where Unity and visual evidence support them.

## Validation

- Run Unity `-batchmode -quit -projectPath /Users/markwang/mw/IceClash -executeMethod IceClash.CharacterValidation.Editor.MaleBaseV2IntegratedSkatesValidationSetup.GenerateAndValidateBatch -logFile <log>` and require exit code 0 plus `MESHY_V2_VALIDATION_PASS`.
- Run a separate capture method if image generation is not reliable during import, then inspect PNGs directly.
- Compare SHA-256 manifests captured before and after for protected files; require byte-for-byte equality.
- Inspect `git status --short` and `git diff --name-only` to ensure no live gameplay path was touched.

## Rollback / Risk

- All intended Unity assets are isolated under a new v2 root; rollback consists of deleting that new root and its generated `.meta` files. No rollback action will be taken without explicit user direction.
- Humanoid import may fail because of the Meshy skeleton; in that case, stop after reporting Unity's exact import/avatar failure rather than editing the skeleton.
- Headless graphics may prevent screenshot capture. Numeric Unity validation remains available, but absence of inspectable images blocks a confident deformation pass and must be reported.
- Preserve the pre-existing `ProjectSettings/ProjectSettings.asset` modification and do not attribute it to this task.
