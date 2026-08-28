# Player Attributes and Gameplay

## Problem

Every skater currently uses essentially the same fixed physical and action tuning. Players cannot create distinct builds, progression does not constrain attribute choices, and the gameplay systems do not yet express speed, acceleration, agility, stamina, puck control, shooting, passing, strength, or defense as separate capabilities.

## Requirement

Add a level-budgeted nine-attribute player model—SPD, ACC, AGI, STA, CTR, SHT, PAS, STR, and DEF—and connect it to the existing local hockey gameplay. Attributes must modify physical capability, execution quality, and forgiveness while preserving direct movement and action input. Build allocation must enforce per-attribute bounds and progressive point costs so one player cannot maximize every attribute. AI decision quality must remain controlled only by AI difficulty, independently of the player attribute model.

## Acceptance Criteria

- [x] A serializable player build contains level plus independently readable SPD, ACC, AGI, STA, CTR, SHT, PAS, STR, and DEF values, and runtime snapshots retain the active build and current stamina.
- [x] Level determines an attribute-point budget; allocation APIs enforce minimums, maximums, available points, and progressive high-rating costs, rejecting invalid or unaffordable allocations without partially mutating the build.
- [x] SPD independently controls maximum skating speed, ACC independently controls acceleration, and AGI controls turning responsiveness/radius without generating movement input.
- [x] STA drives deterministic exertion, recovery, and fatigue degradation of physical/action execution, with higher STA maintaining performance longer and reset restoring stamina.
- [x] CTR changes carry stability, claim/reception forgiveness, and user-triggered deke tolerance without automatically moving the skater or initiating a deke.
- [x] SHT changes shot power, accuracy, and forgiveness while shot timing, charge, position, facing, and puck position continue to affect the result.
- [x] PAS changes deterministic pass pace/accuracy and receiving forgiveness; a clean lane is not failed by a random roll, and interceptions remain possible through puck physics and defender positioning.
- [x] STR, DEF, SPD, AGI, relative position, speed, angle, and timing contribute to body/pull check contests and puck protection; no attribute alone guarantees a successful collision or defensive action.
- [x] Human input remains authoritative for movement, passing, shooting, deking, switching, and checking, and no IQ/automatic-decision attribute is introduced.
- [x] AI difficulty remains a separate behavior/reaction/decision setting and does not derive from or overwrite a skater's gameplay attributes.
- [x] The real arena smoke check covers budget enforcement, contrasting movement builds, stamina/fatigue, deke input, shot/pass modifiers, reception, check contests, snapshot persistence, and the continued one-human-input invariant.
- [x] Player-facing repository documentation explains the nine attributes, build trade-offs, stamina behavior, deterministic pass principle, and AI-difficulty separation.

## Constraints

- Preserve the Unity `6000.5.9f1` local-PvE architecture, current input devices, physics puck, team/role setup, and existing match flow.
- Attribute effects must be bounded and deterministic enough for repeatable smoke validation; normal puck physics and player positioning may still produce emergent outcomes.
- Do not add networking, accounts, backend persistence, store integration, or a build-allocation menu in this story.
- Existing serialized scenes, prefabs, and tuning assets must continue to load without manual migration.

## Non-Goals

- Production progression UX, account-backed saves, monetization, roster management, or online PvP synchronization.
- An IQ, awareness, auto-aim decision, auto-skate, auto-pass, auto-shoot, or automatic defensive-action attribute.
- Final animation, audio, visual effects, balance certification, or production telemetry.
- Goalies receiving the skater build system in this story.
