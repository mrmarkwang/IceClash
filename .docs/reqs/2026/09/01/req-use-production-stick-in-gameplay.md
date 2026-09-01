# Use Production Stick in Gameplay

## Context

`PrototypeArena` still renders the legacy orange low-poly stick because the modular character generator hard-codes `hockey_stick_002.fbx`. The validated production `Hockey_Stick_Base_v1.prefab` must become the visual stick used by the gameplay `HockeyPlayer` and `Resources/Skater` prefabs.

## Requirements

- [x] Replace the legacy low-poly rendered stick in generated gameplay prefabs with `Assets/Equipment/Sticks/Hockey_Stick_Base_v1/Hockey_Stick_Base_v1.prefab`.
- [x] Align the production prefab's `PrimaryGrip` and `BladeContact` to generated presentation targets while keeping the blade on the existing gameplay control point and preserving player-controller, camera, input, and match behavior.
- [x] Preserve the production PBR material and rigid/no-animation import configuration.
- [x] Keep the existing `Stick`, `Stick Grip`, `Stick Shaft`, and `Stick Blade` gameplay/presentation contract so `HockeyStickRig`, `HockeyEquipmentLoadout`, and puck interaction continue to work.
- [x] Regenerate and validate `HockeyPlayer.prefab`, its connected `Resources/Skater.prefab` variant, and the modular character test scene.
- [x] Verify the gameplay prefab depends on the production stick prefab and no longer depends on the legacy stick FBX or legacy unlit stick material.
- [x] Capture gameplay evidence showing the production dark stick on arena skaters and report any visibility or fit limitation.
- [x] Verify `StickPuckInteraction`, `HockeyStickRig`, `PrototypeArena.unity`, PlayerController, camera, input, and match-system source assets remain unchanged.
- [x] Present the stick in a recognizable skating carry matching the supplied reference: top hand beside the torso, second hand lower and forward, shaft diagonally crossing toward the puck, and blade resting at the existing ice-level control point.
- [x] Preserve natural two-hand contact with the production `PrimaryGrip` and `SecondaryGrip` after the pose change, without detached hands, arm inversion, or a backward blade.

## Constraints and Non-goals

- This is a visual asset substitution, not new gameplay.
- Do not change puck possession, passing, shooting, checking, controller, camera, input, match, or player movement logic.
- Do not add new IK behavior or change the blade/control-point gameplay position. Generated hand and shaft presentation targets may move only as needed to create the requested hockey carry.
- Do not modify production stick source geometry or textures.

## Acceptance

All criteria must be supported by Unity generator/validator output, prefab dependency inspection, preserved hashes, and a PrototypeArena screenshot.
