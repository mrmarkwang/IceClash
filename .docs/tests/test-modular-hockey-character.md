# Modular Hockey Character E2E Specification

## Scenario 1 - Ten modular characters load

1. Open `ModularCharacterTest` and enter Play Mode.
2. Observe the validation result after scene initialization.
3. Verify exactly ten active HockeyPlayer roots exist.
4. Verify every player uses the selected valid Humanoid avatar and has one active Helmet, Jersey, Shoulder Pads, Gloves, Pants, Socks, Skates, and Stick item.
5. Verify the harness configures the first existing character against the scene puck, claims it at the authoritative control point, observes carried follow, and releases it without creating an eleventh character.

Expected: validation passes with ten humanoid players, all eight modular equipment slots populated, and claim/carry/release completed through the existing puck system.

## Scenario 2 - Equipment slots replace independently

1. Select one test player.
2. Replace and clear each slot through the equipment API in turn.
3. Observe the remaining seven slots after each operation.
4. Run the Edit Mode asset validator and verify a temporary prefab save/reload preserves the selected change, all other slot references, and stable Stick IK references.

Expected: the selected slot changes or clears while every other slot keeps the same equipped object and reference.

## Scenario 3 - Two-hand stick pose and placeholder animation

1. Inspect a test player while idle, skating, and shooting presentation inputs are exercised.
2. Verify the Animator changes between idle/skating and shooting states without root motion.
3. Verify the left- and right-arm IK constraints remain valid, weighted, and target separate hand grips on the same stick.
4. Clear the Stick slot, verify both weights become zero while targets remain valid, then equip a replacement and verify both weights return.

Expected: both hands remain posed on the stick while placeholder locomotion and shooting presentation play, the character root is not moved by animation, and replacement never breaks constraint references.

## Scenario 4 - Existing 5v5 gameplay and puck contract

1. Open `PrototypeArena` and enter Play Mode.
2. Verify five Blue and five Red skaters are spawned and controllable/AI-driven as before.
3. Exercise puck pickup, carry, pass, and shot validation.
4. Verify the visual stick blade stays near the authoritative puck control point.
5. Verify match flow, player switching, AI, and team ownership assertions remain passing.
6. Verify ten skaters bind to runtime-added controllers and two goalies safely remain in controller-less idle presentation.

Expected: all prior gameplay smoke assertions pass with ten modular skaters plus two controller-less goalie visuals, and puck behavior remains governed by the existing puck system.

## Scenario 5 - Mobile asset budget settings

1. Run the editor validation for the selected character and HockeyPlayer prefab.
2. Inspect material identity across ten test players and source texture import settings.

Expected: all textures below `RealisticHumanMale` are non-readable and mipmapped with explicit Android/iOS 1024px ASTC 6x6 overrides; character renderers disable motion vectors and reflection probes; all ten test and twelve gameplay humanoids share material assets without per-player clones.
