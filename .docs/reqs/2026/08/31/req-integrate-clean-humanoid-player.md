# Integrate Clean Humanoid Player

## Problem

IceClash gameplay currently uses a generated humanoid presentation based on `RealisticHumanMale/unity.Fbx`, while the separately validated and deformation-cleaned `Male_Base_v1_1_Clean.fbx` is not used by the gameplay player. The cleaned character must become the visible skater without allowing animation or mesh hierarchy changes to alter the authoritative controller, input, camera, collider, physics, puck, or action behavior.

## Requirement

Integrate `Male_Base_v1_1_Clean` as a production visual child of the existing gameplay player root. Give it a dedicated, extensible Humanoid locomotion Animator whose parameters are driven from existing gameplay movement while root motion remains disabled. Preserve the existing runtime player composition and all gameplay contracts, validate the result in the prototype arena with keyboard and virtual-joystick input paths, capture the requested visual/Inspector/hierarchy evidence, and report the production asset paths, transform alignment, Avatar and bone paths, validation results, and complete modified gameplay-file list.

## Acceptance Criteria

- [x] `Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean.fbx` remains unchanged and is imported as Humanoid/Create From This Model with an Avatar for which `isValid` and `isHuman` are true.
- [x] A distinct production visual prefab exists at `Assets/Characters/Male/Male_Base_v1_1/Male_Base_v1_1_Clean_Visual.prefab` and references the cleaned model without replacing or modifying the cleaned validation prefab, controller, or scene.
- [x] `Assets/_Project/Prefabs/HockeyPlayer.prefab` remains the authoritative gameplay root, retains its original `CharacterController` settings and gameplay-component composition, and owns a `Visual` child containing the clean production visual; `Assets/_Project/Prefabs/Resources/Skater.prefab` remains its connected resource variant.
- [x] The visual uses uniform scale, inherits the gameplay-root rotation, aligns both feet to the ice by changing only the `Visual` child's local Y when adjustment is required, and introduces no Rigidbody, CharacterController, gameplay collider, or MeshCollider below `Visual`.
- [x] Any previous placeholder/character renderer is absent or disabled while the gameplay `CharacterController` remains enabled and unchanged; there is no duplicate visible player body.
- [x] `Assets/Characters/Male/Male_Base_v1_1/Animation/MaleSkater.controller` provides only Idle and temporary Running locomotion states for this milestone and declares `Speed`, `ForwardAmount`, `TurnAmount`, `IsMoving`, `IsBackward`, `IsBraking`, `IsSprinting`, and `CrossoverDirection` parameters for future skating extension.
- [x] The gameplay presentation bridge derives locomotion parameters from existing planar gameplay velocity/input, does not translate or independently rotate the player, and keeps `Animator.applyRootMotion` false.
- [x] Existing movement/controller, joystick/WASD input, camera, puck, Shoot, Pass, Deke, Check, Switch, Rigidbody/CharacterController, and gameplay-collider source behavior is unchanged.
- [x] Play Mode validation injects Input System keyboard state through `LocalPlayerInput` and UI pointer/drag events through `VirtualJoystick`, proves each real source independently reaches `PlayerInputController`, `PlayerController`, and the unchanged `PlayerMovementController`, and confirms acceleration/deceleration and turning checks still pass, the camera target remains the gameplay root, animation plays without duplicate/root-motion movement, and the gameplay collider remains correct.
- [x] Visual review checks front, side, moving/turning, shoulders, elbows, wrists, hips/groin, knees, and ankles; only the previously accepted rear shorts-hem flare, mild ankle faceting, and thin disconnected wrist-border line may remain and all observed artifacts are reported.
- [x] Validation evidence includes front gameplay, side gameplay, moving/turning, Animator Inspector, and gameplay-root-plus-Visual hierarchy screenshots.
- [x] Head, LeftHand, RightHand, LeftFoot, and RightFoot transform paths are resolved from the production Animator and reported for future equipment attachment, without adding equipment in this milestone.
- [x] The final report includes every requested path, Avatar, local transform, renderer/collider/root-motion/camera/input result, deformation observation, bone path, and every existing gameplay file or prefab modified.

## Constraints

- Do not rewrite or replace `PlayerMovementController`, `PlayerController`, input routing, camera behavior, puck behavior, action logic, gameplay physics, or gameplay colliders.
- Do not modify any canonical or validated asset under `Assets/Characters/Male/Male_Base_v1` except importer metadata only if required to maintain the already validated Humanoid/Create From This Model configuration; prefer no change when it is already correct.
- Do not use root motion, mesh position, a second controller, a second rotation system, mesh colliders, non-uniform character scale, or Rigidbodies on bones.
- Running is a temporary playback clip, not a final skating animation.
- Do not add Shoot, Pass, Deke, Check, Crossover, Brake, Sprint, or equipment animation states in this milestone.
- Preserve the existing camera target transform and puck/stick gameplay attachment contracts.

## Non-Goals

- Producing final skating, crossover, braking, sprinting, shooting, passing, deking, or checking animation.
- Additional mesh, topology, material, skin-weight, or deformation cleanup.
- Adding helmets, gloves, skates, sticks, or other new equipment to the cleaned visual.
- Retuning movement, acceleration, turning, camera composition, colliders, rink physics, AI, match rules, or mobile controls.
- Adding feature flags, alternate-character fallback modes, compatibility layers, or parallel movement implementations.
