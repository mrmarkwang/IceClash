# Modular Hockey Character

## Problem

IceClash still represents skaters with a primitive capsule even though a selected humanoid character asset is present. The prototype has no independently replaceable hockey equipment, no two-hand stick pose, no skating or shooting presentation, and no mobile-oriented character asset budget validation.

## Requirement

Replace the visual shell used by the existing 5v5 skater roster with a reusable humanoid `HockeyPlayer` prefab built from the selected character, modular hockey equipment, a two-hand hockey-stick IK rig, and placeholder skating and shooting animation, without changing the existing player-control or puck-gameplay contracts.

## Acceptance Criteria

- [x] The selected `RealisticHumanMale/unity.Fbx` asset is imported with a valid Unity Humanoid avatar.
- [x] A reusable `HockeyPlayer` prefab contains the selected humanoid character and remains compatible with the existing runtime-added player components.
- [x] The prefab exposes distinct Helmet, Jersey, Shoulder Pads, Gloves, Pants, Socks, Skates, and Stick equipment slots, each populated with one active item by default.
- [x] Every equipment slot can replace or clear its equipped object independently without rebuilding the player prefab or affecting the other slots.
- [x] The hockey stick is driven by a two-hand IK rig with independently inspectable left- and right-hand constraints and targets.
- [x] Placeholder idle/skating and shooting animation states visibly drive the humanoid and respond to current movement and shooting state.
- [x] The existing `PlayerController` source remains byte-for-byte unchanged.
- [x] Existing 5v5 skater spawning, team ownership, controls, AI, match flow, and puck interactions continue to work.
- [x] Character materials are shared; every texture under `RealisticHumanMale` is non-readable, mipmapped, and limited to 1024px ASTC 6x6 on Android and iOS; character renderers disable motion vectors and reflection probes.
- [x] A test scene contains exactly ten active modular skaters and can validate prefab, equipment, IK, animation, and puck integration without adding extra gameplay skaters.

## Constraints

- Preserve the current `PlayerController`, `StickPuckInteraction`, `ShootController`, `PuckController`, and 5v5 gameplay contracts.
- Preserve the existing `Resources/Skater` loading contract unless changing only the referenced visual prefab is sufficient.
- Use the installed official Unity Animation Rigging package.
- Keep equipment replaceable at runtime and in the editor through stable slot transforms.
- Prefer shared materials and low-cost primitive placeholder equipment suitable for ten simultaneous skaters.
- Treat every imported texture below `RealisticHumanMale` as covered by the mobile policy.
- Do not add a networked customization or inventory system.

## Non-Goals

- Production-quality hockey character art or licensed team uniforms.
- A full motion-captured animation set or root-motion locomotion.
- Goalie-specific equipment or goalie animation redesign.
- Refactoring existing gameplay controllers, puck physics, AI, input, or match rules.
- Adding feature flags, fallback character modes, or duplicate puck interaction implementations.
