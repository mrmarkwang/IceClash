# Meshy Integrated-Skates Character Validation

## Problem

The new Meshy male hockey-player export must be evaluated in Unity without changing the existing male character or any live gameplay behavior. The integrated skates and the supplied Air Squat motion need explicit rig, scale, deformation, and visual validation before the character can be considered for later gameplay integration.

## Requirement

Import the supplied ZIP contents as a new `Male_Base_v2_IntegratedSkates` asset set, configure a valid Unity Humanoid avatar, retain Air Squat only as an isolated validation animation, create a gameplay-free test prefab and scene, and produce evidence sufficient to judge the mesh, skeleton, scale, animation, and integrated skates.

## Acceptance Criteria

- [x] The source ZIP is left unchanged and its FBXs, textures, sizes, hashes, mesh statistics, material/texture arrangement, skeleton, and animation clips are reported.
- [x] All v2 assets live under `Assets/Characters/Male/Male_Base_v2_IntegratedSkates/`; no file is placed in or changed under `Male_Base_v1`.
- [x] The canonical v1 FBX SHA-256 is recorded before and after work and is identical.
- [x] The v2 character FBX imports as `Humanoid` with `Create From This Model`, and its Avatar is non-null, valid, and human.
- [x] The required humanoid bones, full hierarchy, total bone count, root, toe-bone presence, and lower skate-region skin influences are reported without assuming v1 bone names.
- [x] Import scale and approximate character height are reported, with no arbitrary animation-driven resize.
- [x] Both integrated skates are visually and numerically checked for attachment, orientation, blade deformation, ankle deformation, contact plane, and asymmetry.
- [x] Every imported animation clip is reported; Air Squat remains non-gameplay validation content and any hockey-forward motion remains isolated and rejected.
- [x] A gameplay-free test prefab with Animator and v2 Avatar exists at the requested path.
- [x] A separate validation scene with floor, camera views, and lighting exists at the requested path, while `Assets/Scenes/Game.unity` remains unchanged.
- [x] Neutral and representative Air Squat frames are validated, with screenshots where rendering is available.
- [x] Gameplay scripts, the existing player prefab, scenes other than the new validation scene, and gameplay systems remain unchanged.

## Constraints

- Do not overwrite, rename, delete, or modify `Male_Base_v1` or its source assets.
- Do not replace the live gameplay character or alter player, puck, input, camera, UI action, AI, or game-state logic.
- Do not alter the source skeleton or mesh to force Humanoid compatibility.
- Do not repair or adopt failed generated hockey skating animations in this task.
- Do not add old standalone skate assets.
- Stop validation if Unity cannot produce a valid Humanoid Avatar.

## Non-Goals

- Gameplay integration of v2.
- Creation or repair of skating locomotion.
- Controller, camera, input, puck, AI, score, or existing-scene changes.
- Rig or skin-weight authoring.
