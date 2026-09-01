# Integrate Production Hockey Stick

## Context

IceClash needs its first production hockey-stick model imported from the supplied Meshy archive and validated against the already-approved `Male_Base_v1_1_Clean` humanoid without changing any existing humanoid or gameplay asset.

## Requirements

- [x] Preserve the downloaded archive and record the source FBX filename, imported filename, and SHA-256.
- [x] Import the single rigid FBX and its supplied PBR textures under `Assets/Equipment/Sticks/Hockey_Stick_Base_v1/`, with Rig/animation import disabled and no Animator, Avatar, armature, skinning, animation clips, or gameplay scripts on the stick prefab.
- [x] Record Unity-observed vertices, triangles, material count, texture references, source bounds/orientation/pivot, and any visible material defects.
- [x] Normalize the asset non-destructively to a believable adult stick length near 1.55-1.65 m relative to the 1.83 m validated player and document final dimensions, axes, and scale.
- [x] Create `Hockey_Stick_Base_v1.prefab` with a `Model` container plus empty `PrimaryGrip`, `SecondaryGrip`, and `BladeContact` reference transforms at useful positions.
- [x] Create a new right-handed `Male_Base_v1_Stick_Test.prefab` whose `StickSocket` is parented to the validated `RightHand` bone and holds the stick plausibly without editing the source humanoid.
- [x] Create an isolated `Hockey_Stick_Base_v1_Test.unity` scene with neutral lighting, ground, and camera.
- [x] Capture front, side, rear, right-hand grip, and blade/ground evidence and truthfully document defects and limitations in `Hockey_Stick_Base_v1_Validation.md`.
- [x] Verify hashes for the pre-existing humanoid production/validation assets are unchanged after generation.

## Constraints and Non-goals

- Do not modify existing Male_Base_v1 FBXs, cleaned mesh, skeleton, weights, Avatar, animations, validation prefab/scene, gameplay prefabs, controllers, camera/input, puck, or gameplay systems.
- Do not implement two-hand IK, skating-with-stick animation, stickhandling, possession, passing, shooting, checking, or gameplay integration.
- Do not destructively edit or rescale the source FBX geometry.
- Do not conceal mesh, material, fit, pivot, or scale defects.

## Acceptance

All requirement checkboxes are supported by Unity batch validation output, repository hashes, generated asset inspection, and the five requested rendered views.
