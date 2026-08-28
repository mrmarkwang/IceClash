# Player Attributes and Gameplay E2E Specification

## Automated scenario: constrained builds

1. Start the real runtime-built arena through the Phase 1 smoke runner.
2. Construct a level-25 build and verify its budget is 192 points, all nine ratings start at 40, and its remaining points are 192.
3. Allocate an attribute from 40 to 70 and verify the cost is 31 points (29 increments ending at ratings 41..69 and 2 points for the increment ending at rating 70).
4. Attempt a below-minimum, above-maximum, and unaffordable allocation.
5. Verify valid allocations consume the calculated points, invalid allocations are rejected atomically, and the maximum level-50 budget of 392 is less than the 828 points required to maximize all nine attributes.
6. Verify the five pinned level-25 preset vectors and costs, then verify the roster maps Center=Playmaker, Left Wing=Sniper, Right Wing=Speed, Left Defense=Power, and Right Defense=Two-Way for both teams.

## Automated scenario: direct skating and fatigue

1. Compare otherwise-equivalent builds using rating 95 versus rating 40 for the isolated movement attribute.
2. Verify SPD maps terminal speed from 6.4 to 9.6, ACC maps acceleration from 13.5 to 22.5, and AGI maps low/high-speed turn rates from 12/6 to 20/12.
3. Apply identical sustained high-exertion input to low-STA and high-STA skaters.
4. Verify rating-40 STA drains 10 points/second, rating-95 STA drains 4 points/second, idle recovery is 9 versus 13 points/second, fatigue output never falls below 0.68, and reset restores full stamina.
5. Verify no build creates movement when movement input is zero.

## Automated scenario: explicit puck actions

1. Give the controlled skater possession and submit no action input.
2. Verify no pass, shot, or deke starts automatically.
3. Press DEKE and verify exactly one bounded deke/control window starts without changing joystick input.
4. Compare rating-40 and rating-95 CTR claim radius (1.25 versus 1.85), claim speed (12 versus 17), and carry multiplier (0.75 versus 1.25). Verify intended-pass reception quality is `0.60 CTR + 0.40 PAS`, mapping low/low to radius 1.4 and entry speed 4.5, high/high to radius 2.1 and entry speed 7.5, and mixed builds to the documented weighted midpoint.
5. Compare rating-40/rating-95 SHT power multipliers (0.85 versus 1.20) and maximum deviation (6 versus 1 degree). Stage actual charge, facing-to-goal angle, rink distance, puck-to-control-point error, lateral speed, and fatigue separately and verify each increases the runtime situation challenge by its documented weight; identical explicit inputs produce identical deviation.
6. Compare rating-40/rating-95 PAS pace multipliers (0.88 versus 1.08), maximum deviation (5 versus 0.5 degrees), and lead (0.32 versus 0.55 seconds); verify zero challenge produces zero deviation without randomness, then verify a positioned defender can physically intercept the released puck.

## Automated scenario: contested defense

1. Stage body and pull check attempts with known checker/carrier builds.
2. Verify an out-of-range or outside-cone attempt fails before scoring regardless of rating-95 attributes.
3. Stage actual checker/carrier velocities and rotations, then verify approach speed normalizes from 0 to 1 over closing speed 0..8, body alignment maps dot -1..1 to 0..1, pull alignment maps the configured cone edge..1 to 0..1, and front/behind carrier contact maps to 1/0.
4. Feed the documented body and pull formulas rating-95 attacker attributes plus normalized approach/alignment 1 against rating-40 protection plus fatigue/contact 0, and verify the attack succeeds through the live check path.
5. Feed rating-40 attacker attributes plus normalized approach/alignment 0 against rating-95 protection plus fatigue/contact 1 and an active deke bonus, and verify the live challenge is resisted.
6. Verify a successful check frees the physics puck and never grants possession directly.

## Automated scenario: persistence and AI separation

1. Capture the runtime `MatchData` after assigning distinct builds and changing stamina.
2. Verify each `PlayerData` snapshot contains the correct level, all nine ratings, and current stamina.
3. Compare Easy and Normal AI skaters with identical gameplay builds and explicit physical-action inputs.
4. Verify difficulty retains distinct decision intervals, target error, tactical choices, and charge choices while terminal movement and pass/shot physical evaluation are identical for the same build/input and neither setting mutates the build.
5. Verify exactly one human input source remains active after automatic/manual player switching.

## Manual balance scenario

1. In the real arena, play short shifts using the pinned Right Wing speed, Left Wing sniper, Center playmaker, Left Defense power, and Right Defense two-way presets.
2. Confirm each build feels distinct but still requires correct joystick direction, position, timing, and action presses.
3. Confirm fatigue is noticeable but gradual, clean passes feel reliable, interceptions remain readable, and no single attribute guarantees a shot, deke, or check outcome.
4. Record device/build observations separately; final competitive balance and production tuning are outside this story.
