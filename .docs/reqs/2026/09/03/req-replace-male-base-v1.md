# Replace Male Base v1

## Problem

The validated integrated-skates character currently exists as an additive v2 asset set, while the user has explicitly directed that the existing `Male_Base_v1` asset folder be overwritten rather than preserved.

## Requirement

Retire the existing `Assets/Characters/Male/Male_Base_v1` folder and replace it with the validated integrated-skates asset set, preserving the replacement set's internal Unity GUID relationships and keeping a recoverable backup outside the Unity project.

## Acceptance Criteria

- [x] A complete backup of the pre-replacement `Male_Base_v1` folder and folder metadata exists outside the Unity project.
- [x] `Assets/Characters/Male/Male_Base_v1` contains the validated integrated-skates model, textures, material, Air Squat controller, prefab, scene, and validation utility.
- [x] The former additive `Assets/Characters/Male/Male_Base_v2_IntegratedSkates` path no longer exists, avoiding duplicate Unity GUIDs.
- [x] The replacement validation utility resolves the new `Male_Base_v1` root and Unity again reports a valid human Avatar and explicit validation pass.
- [x] The Unity editor project compiles after replacement; external editor consumers receive an explicit retired-generator error rather than missing-type compiler failures or silent gameplay regeneration.
- [x] No gameplay, equipment, scene, project-setting, or script file outside the replaced folder is modified by this operation.
- [x] Known external references to retired old-v1 asset GUIDs are reported rather than silently rewritten.

## Constraints

- This operation is explicitly destructive and supersedes the earlier instruction to preserve `Male_Base_v1`.
- Do not alter live gameplay or equipment consumers as part of the folder replacement.
- A compile-only compatibility boundary may be added inside the replacement folder when required by existing editor consumers, but it must not regenerate gameplay.
- Do not retain two copies of the same replacement `.meta` GUIDs under both v1 and v2 paths.
- Preserve the pre-existing unrelated `ProjectSettings/ProjectSettings.asset` modification.

## Non-Goals

- Migrating gameplay, stick, skate, animation, or test consumers to the new character.
- Repairing references to deleted legacy v1 assets.
- Changing the integrated-skates source model, skeleton, skin weights, or animation.
