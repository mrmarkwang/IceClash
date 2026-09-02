# Skate Base v1 Integration Requirements

## Problem

The validated `Male_Base_v1` humanoid has a production hockey-skate asset, but gameplay skaters still wear primitive cube placeholders. The preserved Meshy skate must replace those placeholders for every red and blue gameplay skater without changing trusted humanoid, animation, movement, input, camera, puck, or stick behavior.

## Requirement

Preserve the supplied source and reusable rigid skate prefabs; attach matching left and right instances through Humanoid feet in both validation and the modular gameplay loadout; regenerate the canonical `HockeyPlayer` and `Resources/Skater` variant so every runtime red/blue skater inherits the production skates; and provide geometry, fitting, contact, animation, runtime, visual, source-integrity, and regression evidence.

## Acceptance Criteria

- [x] The downloaded ZIP's SHA-256 is recorded before extraction, all supplied FBX/PBR files are copied under `Assets/Equipment/Skates/Skate_Base_v1/Source/`, and the copied FBX is byte-identical to the archive member.
- [x] `Skate_Base_v1.prefab` is rigid static equipment with a zeroed/unit root, a `Visual` child, a correctly placed `BladeContact`, no Animator, no humanoid bones, and no skinned mesh.
- [x] The validation report records imported hierarchy, material count, vertices, triangles, bounds/dimensions, source/import transforms, scale, and the established forward/up/lateral axes, and reports geometry-integrity checks.
- [x] A dedicated fitting prefab/scene uses `Animator.GetBoneTransform` semantics for `HumanBodyBones.LeftFoot` and `HumanBodyBones.RightFoot`, creates separate `LeftSkateSocket` and `RightSkateSocket` transforms without modifying foot-bone transforms, and instantiates both skates from one canonical mesh source.
- [x] Left and right skates use equal positive scale, correct handed orientation, and contain the sock feet convincingly with heel, toes, and ankle aligned inside the boots and no obvious external penetration.
- [x] In the neutral validation pose, both `BladeContact` markers meet one ice plane without visible floating or deep penetration, and their world-Y difference is negligible and recorded.
- [x] The validated running animation is sampled through at least two complete cycles and both skates remain attached with finite, stable transforms, correct orientation, and no severe visible sock penetration.
- [x] Evidence includes neutral front, rear, both sides, both skate close-ups; running front, side, rear; and a low gameplay-camera-style view, with the requested fit/orientation/symmetry observations recorded.
- [ ] The canonical and clean humanoid Avatar remains valid and human, protected humanoid/source hashes match before and after generation, and no movement/controller/camera/input/puck/stick logic or validated animation source is modified.
- [x] The completion report lists every created or modified asset and identifies any remaining source-geometry limitations without performing optional topology changes before evidence review.
- [ ] `HockeyPlayer.prefab` equips `Skate_L_v1` and `Skate_R_v1` through the existing `Skates` loadout slot and `HockeyPairedEquipmentFollower`, with no primitive skate placeholder remaining.
- [ ] `Resources/Skater.prefab` remains a connected variant of `HockeyPlayer.prefab` and inherits both production skates, so all ten red/blue skaters spawned by `PrototypeArenaBootstrap` wear the same validated pair.
- [ ] Gameplay skates retain positive equal scale, correct handedness/forward orientation, rigid mesh/material references, foot following, and blade/ice alignment in idle and running presentation states.
- [ ] Automated gameplay validation proves all ten runtime skaters have exactly two production skate visuals and visual evidence shows the equipped skates on an actual gameplay player.

## Constraints

- Do not overwrite or edit the original ZIP, original extracted FBX bytes, canonical/clean `Male_Base_v1` assets, Avatar, skeleton hierarchy, skin weights, or validated animation sources.
- Do not edit gameplay movement, `PlayerController`, camera, input, puck, stick gameplay, or scene rules; changes to the generated `HockeyPlayer` and `Resources/Skater` prefabs are explicitly required.
- Do not rig or skin the skate, add an Animator, deform humanoid bones, or solve fitting by moving/scaling foot bones.
- Use the complete 4,136-face source mesh as the one canonical skate; avoid negative runtime scale and create the opposite handed mesh offline with corrected winding.
- Visual fit to the validated 1.83 m humanoid takes precedence over forcing a nominal measurement; the expected adult skate length is approximately 0.30-0.32 m.
- Running is attachment stress-test evidence only and must not be described as a final skating animation.

## Non-Goals

- Skate physics, colliders, ice-contact logic, VFX, edge effects, or new skating animation/IK behavior.
- Remodel, retopology, or repair of minor hidden AI topology artifacts unless attachment cannot otherwise be completed.
- Changes to the validated humanoid or any unrelated game subsystem.
