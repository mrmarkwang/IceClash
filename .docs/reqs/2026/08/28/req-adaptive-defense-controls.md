# Adaptive Defense Controls — Requirements

## Problem

The mobile action controls always show offensive actions, even after an opponent establishes possession. Touch players cannot manually switch defenders or attempt a defensive puck challenge, so opponent possession is readable but not meaningfully playable.

## Requirement

When a Red opponent possesses the puck, replace the Blue user's offensive touch actions with two defensive actions: SWITCH and CHECK. SWITCH must transfer control through the existing useful-defender selection path. CHECK must perform a contextual defensive challenge from the controlled Blue skater: a close-range body check or a longer stick-range pull check. Human possession restores the normal offensive controls. A newly loose puck automatically selects the closest Blue skater once and shows only SWITCH so manual override remains available.

## Acceptance Criteria

- [x] Confirmed opponent possession changes the visible touch actions from PASS, DEKE, and SHOOT to exactly SWITCH and CHECK without moving or disabling the joystick.
- [x] Human possession shows PASS, DEKE, and SHOOT; loose-puck play selects the closest Blue skater once and shows only SWITCH; repeated changes leave no stale labels or held input.
- [x] Tapping the defensive SWITCH touch action invokes the existing manual switch scoring, cooldown, input/AI transfer, marker transfer, and camera retarget path.
- [x] Tapping CHECK while a controlled Blue skater is in close body-contact range of the Red puck carrier performs a body check that dislodges the puck and applies bounded physical separation.
- [x] Tapping CHECK with the Red carrier in the controlled skater's forward stick range performs a pull check that dislodges and draws the puck toward the checker without homing or granting automatic possession.
- [x] CHECK does nothing when there is no opponent carrier, the carrier is outside the configured range or forward cone, the action is cooling down, or gameplay is disabled.
- [ ] Keyboard/gamepad controls can exercise CHECK through the same shared action contract used by touch, while existing PASS, SHOOT, and SWITCH mappings continue to work.
- [x] Defensive action ranges, forward cone, cooldown, puck release speeds, and body separation strength are persisted in an Inspector-editable tuning asset whose runtime validation enforces a positive cooldown, body range below pull range, a valid forward cone, and capped release/impulse values.
- [x] A successful CHECK starts one human-team cooldown that remains active after SWITCH, and match disable/reset clears pending body impulse without granting an extra check or carrying motion into the next faceoff.
- [x] Automated gameplay verification covers offense/loose/defense transitions, loose-puck closest-player selection, persistent touch SWITCH override, successful checks, rejected checks, and restoration of offensive controls.
- [x] README control and architecture documentation describes the possession-adaptive actions and contextual CHECK behavior accurately.

## Constraints

- Preserve the existing possession-driven automatic player selection and `PlayerSwitchController` as the sole manual control-transfer implementation.
- Keep the puck independent and physics-driven after a successful check; a check may dislodge it but may not directly assign possession to the checker.
- Preserve the in-progress distance-scaled passing and intended-receiver work in the current worktree.
- Reuse the existing mobile action-button layout and safe-area/multi-touch behavior; do not add a fourth action button.
- Runtime clamps must keep body range within `0.5–2.0 m`, pull range within `0.6–3.5 m` and above body range, forward dot within `0–1`, cooldown within `0.2–2.0 s`, puck release speed within `1–15 m/s`, and body impulse within `0–6 m/s`.
- Keep the feature local-only with no networking, service, package, or persistence changes.

## Non-Goals

- Penalties, checking fouls, injuries, stamina costs, fighting, checking attributes, or difficulty balancing.
- Final body/stick animations, hit reactions, audio, particles, controller rumble, or production visual feedback.
- AI-triggered checks, goalie checks, automatic checking, or continuous proximity-driven switching while the puck remains loose.
- A player-selected body-versus-pull action, additional defensive buttons, feature flags, compatibility layers, or environment-specific control modes.
