# Offside Rule

## Problem

The local hockey match currently allows attacking skaters to enter the offensive zone before the puck without warning or a stoppage, so a core hockey positioning rule is missing.

## Requirement

During live play, detect when a puck-carrying team has a same-team skater inside its offensive zone while the puck remains outside that zone. Show a red grid over that offensive zone while this delayed-offside condition exists. If the puck subsequently enters the zone before all attacking skaters tag up, stop play for offside and restart with a faceoff at the nearest neutral-zone offside dot. Apply the same behavior in both attacking directions.

## Acceptance Criteria

- [x] A puck-carrying team is warned when at least one other same-team skater is beyond its attacking blue line while the puck remains outside the offensive zone.
- [x] The warned offensive zone displays a clearly red, non-colliding grid and the opposite zone does not display the warning.
- [x] The warning clears without a stoppage when every premature attacker returns onside before the puck crosses the blue line.
- [x] The warning clears without a stoppage when the defending team establishes possession before zone entry, while a same-team pass or temporarily loose puck can retain the pending warning.
- [x] The puck crossing the attacking blue line while the warning is active produces exactly one offside stoppage, does not change either score, and prevents live player control during the faceoff delay.
- [x] An offside restart places the puck and skaters around the nearest neutral-zone faceoff dot on the applicable rink side, then resumes normal play after the existing faceoff delay.
- [x] Blue and Red attacks use mirrored zone-entry, warning, and restart behavior.
- [x] Existing opening/post-goal center faceoffs, goals, match clock, and match results remain unchanged.

## Constraints

- Keep the rule local and deterministic; do not add networking, penalties, icing, or referee systems.
- Use the runtime-built prototype arena and its existing match/faceoff flow.
- The warning visual must not add physics collisions or obstruct puck/skater movement.
- Treat a player or puck exactly on the blue line as outside the offensive zone until it has crossed beyond the line.

## Non-Goals

- Intentional-offside faceoff placement rules, delayed-whistle touch-up variants, or possession behavior beyond the specified same-team pass, temporary loose-puck, opponent-turnover, and tag-up rules.
- Penalties, icing, overtime, video review, referee animation, audio, or final production effects.
- A feature flag, environment variable, fallback rule mode, or compatibility layer for the offside system.
