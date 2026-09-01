# Wearable Equipment Slots

## Problem

The modular hockey-player prefab exposes shoulder pads, chest padding, jersey, pants, and socks/shin equipment as separate rendered equipment. These unsupported prototype pieces appear as detached primitive geometry and do not match the intended character customization model.

## Requirement

The player must expose only helmet, visor, gloves, and skates as independently replaceable wearables. Clothing and protective body padding must remain part of the main character visual. The existing independently equipped hockey stick and its two-hand gameplay integration must continue to work.

## Acceptance Criteria

- [x] The wearable equipment model contains independently replaceable Helmet, Visor, Gloves, and Skates slots, plus the existing gameplay Stick slot.
- [x] Shoulder pads, chest padding, jersey, pants, socks, and shin-equipment slots and placeholder renderers are absent from generated player prefabs and scenes.
- [x] Helmet, visor, gloves, and skates can each be cleared and replaced without changing another wearable or the equipped stick.
- [x] Gloves remain attached to animated hand bones and skates remain attached to animated foot bones.
- [x] The change preserves the existing Stick slot, stable IK targets, SecondaryGrip rebinding contract, and production stick/puck implementation boundary without attempting to repair or redefine pre-existing stick-pose or puck-gameplay behavior.
- [x] Blue and red team materials continue to color the main character visual without depending on a separate jersey or sock object.
- [x] Regenerating and validating modular character assets preserves the supported equipment structure and does not recreate removed body-equipment primitives.

## Constraints

- Preserve the serialized numeric values of existing retained slots so current Helmet, Gloves, Skates, and Stick bindings do not silently change meaning.
- Assign Visor the new serialized value `8`; do not reuse a removed slot value.
- Preserve the production stick prefab, grip markers, stable IK target references, and gameplay control-point implementation; stick-pose and puck-gameplay repair are outside this change.
- Do not add separate clothing, padding, sock, or shin customization.

## Non-Goals

- Replacing the remaining supported prototype wearable meshes with production art.
- Redesigning character textures or the main character mesh.
- Changing stick gameplay, player movement, puck behavior, or animation states.
