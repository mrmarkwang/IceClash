# Skate Base v1 Integration E2E Specification

## Scenario 1 - Neutral footwear fit and ice contact

Given the dedicated fitting scene contains the clean validated male in neutral pose with both skate sockets populated,
when the scene is viewed from front, rear, left, right, and both boot close-ups,
then each sock foot reads as contained inside its boot, heel/toes/ankle align naturally, the pair has correct opposing handed orientation and matching positive scale, blades run along the character-forward direction, holders are upright, and both `BladeContact` markers meet the same ice plane without visible float or deep penetration.

Evidence: `neutral-front.png`, `neutral-rear.png`, `neutral-left.png`, `neutral-right.png`, `neutral-left-close.png`, `neutral-right-close.png`.

## Scenario 2 - Running attachment stress test

Given the unchanged validated running clip and the foot-bone sockets,
when animation is sampled for more than two complete cycles,
then both skates follow their corresponding feet without detachment, rotation flip, scale change, reversed orientation, or severe externally visible sock penetration.

Evidence: `running-{front,side,rear}.png`, four phase sets at normalized times `0.125`, `0.375`, `0.625`, and `0.875`, plus 19 sampled transform/toe-relative assertions over 2.25 cycles in the validation log/report. Running is not represented as a final skating animation.

## Scenario 3 - Gameplay-style readability

Given the fitted character and validation ice,
when viewed from a low gameplay-camera-style angle,
then both boots and blades read as symmetric hockey footwear attached to the player rather than props sitting beneath the feet.

Evidence: `gameplay-low.png`.

## Scenario 4 - Source and regression integrity

Given pre-generation hashes for the supplied archive and protected humanoid/animation assets,
when generation, capture, and validation finish,
then source copies match the archive, protected hashes remain identical, the clean Avatar is valid and human, and no movement/controller/camera/input/puck/stick logic or validated animation source appears in the change set.

Evidence: `Skate_Base_v1_Validation.md`, Unity validation log, hash comparison, and `git diff --name-only`.

## Scenario 5 - All runtime skaters wear production skates

Given `PrototypeArenaBootstrap` spawns five red and five blue players from `Resources/Skater.prefab`,
when the arena runs through idle and moving presentation states,
then every player has one active `Skates` loadout item containing the validated `Skate_L_v1` and `Skate_R_v1` rigid meshes, neither cube placeholder remains, both skates follow the corresponding Humanoid feet with positive equal scale and correct forward orientation, and the blades remain visually aligned to the ice.

Evidence: the PrototypeArena smoke log reports `productionSkates=10/10`, generated-prefab dependency/structure assertions pass, and gameplay idle/running screenshots show the equipped production skates on a spawned player.
