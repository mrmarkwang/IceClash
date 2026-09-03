# Integrated-Skates Production Integration

## Problem

The validated Meshy integrated-skates male character has replaced the `Male_Base_v1` asset folder, but the production `HockeyPlayer.prefab` and its generator still depend on the retired clean visual, its deleted animation FBX, detachable skate prefabs, and serialized hand/foot bone references from the old skeleton. Production generation is intentionally blocked, so gameplay cannot safely adopt the replacement character.

## Requirement

Reconnect the production hockey-player prefab and resource variant to the validated Meshy Humanoid, rebuild stick and two-hand IK references from the replacement Avatar, treat the character's skates as integrated equipment, calibrate the visual to the established gameplay scale and ice plane, and replace retired animation references with stable Humanoid presentation clips while preserving current gameplay, input, puck, roster, and equipment-slot contracts.

## Acceptance Criteria

- [x] `Assets/_Project/Prefabs/HockeyPlayer.prefab` contains the validated Meshy integrated-skates visual and no dependency on the retired clean visual prefab or deleted character FBX.
- [x] The production Animator uses the replacement model's valid Humanoid Avatar, has root motion disabled, and uses a production controller with working `Idle` and `Running` states.
- [x] The `Running` state uses the committed Humanoid skating placeholder rather than the retired v1 animation FBX or the Air Squat validation clip.
- [x] The production visual is uniformly calibrated so a runtime skater at the retained `0.68` actor scale is approximately `1.90 m` tall and both integrated skate blades meet the gameplay ice plane.
- [x] The stick socket, left/right hand targets, arm constraints, and `HockeyStickRig` are regenerated from the replacement Avatar's mapped Humanoid bones and retain the established two-hand grip contract.
- [x] No detachable `Skate_L_v1` or `Skate_R_v1` renderers are instantiated; the stable `Skates` equipment slot remains present and active through a non-rendering integrated-skates marker.
- [x] The loadout does not mask or replace the replacement character mesh when the integrated-skates slot is active.
- [x] `Resources/Skater.prefab` remains a connected variant of `HockeyPlayer.prefab`, and existing ten-skater/two-goalie spawning and runtime presentation binding remain unchanged.
- [x] The modular-character asset validator, ten-player scene smoke test, and PrototypeArena gameplay smoke test pass without missing references, compiler errors, duplicate skates, or retired animation dependencies.
- [x] The legacy compatibility boundary is removed, the supported integrated-skates production setup regenerates assets without throwing, and regeneration is idempotent.

## Constraints

- Preserve `PlayerController`, input, puck physics, AI, match flow, roster size, and the `Resources/Skater` loading contract.
- Preserve the serialized five-slot equipment enum/anchor contract even though the skates are part of the character mesh.
- Keep Air Squat isolated to character validation; it must not appear in the production controller.
- Keep gameplay movement transform-driven with Animator root motion disabled.
- Preserve any unrelated user working-tree changes that appear during implementation; the planning baseline currently contains only this story's untracked RPD documents.
- Do not restore deleted v1 model, animation, material, or detachable-skate dependencies as fallbacks.

## Non-Goals

- Authoring a new motion-captured skating animation.
- Changing skating physics, player dimensions, game balance, controls, camera behavior, or puck interactions.
- Making the integrated skate mesh independently replaceable or removable.
- Refactoring the broader equipment or presentation architecture beyond what the integrated-skates migration requires.
