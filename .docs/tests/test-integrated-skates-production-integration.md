# Integrated-Skates Production Integration Test

## Scenario 1 - Idempotent production generation

1. Run the production asset generator and validator twice.
2. Confirm both runs complete without compiler, missing-reference, Avatar, or alignment errors.
3. Confirm stable asset GUIDs remain unchanged and the Console reports `MODULAR_CHARACTER_ASSETS_PASS`.

## Scenario 2 - Production prefab structure

1. Open `Assets/_Project/Prefabs/HockeyPlayer.prefab` in Prefab Mode.
2. Confirm its visual is the Meshy integrated-skates model using a valid Humanoid Avatar and production `MaleSkater.controller`, with Apply Root Motion disabled.
3. Confirm the five equipment anchors remain configured, `SkatesSlot` owns one active non-rendering `Integrated Skates` marker, and no `Skate_L_v1`, `Skate_R_v1`, or skate follower exists.
4. Confirm the replacement skeleton supplies the loadout hand/foot references, stick socket, two arm constraints, targets, hints, and rig layers without Missing references.
5. Confirm a runtime-scaled skater is approximately `1.90 m` tall and the integrated blade bottoms meet the ice reference plane.

## Scenario 3 - Animation replacement

1. Inspect the production controller.
2. Confirm `Idle` uses the procedural Humanoid Idle clip and `Running` uses the looping procedural Humanoid Skate clip.
3. Confirm existing presentation parameters and `IsMoving` transitions drive both states.
4. Confirm neither production state references the Air Squat controller/clip or retired Running FBX.

## Scenario 4 - Modular ten-player smoke

1. Open `Assets/_Project/Scenes/ModularCharacterTest.unity` and enter Play Mode through the committed smoke runner.
2. Confirm exactly ten players render the replacement character with one integrated skate pair each.
3. Confirm Idle-to-Running visibly moves Humanoid leg bones, equipment slots remain complete/independent, both hands retain the stick pose, and puck claim/follow/release succeeds.
4. Confirm the Console reports `MODULAR_CHARACTER_SMOKE_PASS players=10`.

## Scenario 5 - Full gameplay regression

1. Run the committed PrototypeArena smoke runner.
2. Confirm ten skaters and two goalies use the connected resource prefab with correct team materials and retained runtime scale.
3. Confirm moving players enter Running, stationary/goalie presentations remain Idle, stick IK stays bound, integrated blades remain near the ice, and no duplicate skates appear.
4. Confirm existing input, AI, match, scoring, roster, and puck assertions still pass.

## Execution Result — 2026-09-03

- PASS — `IceClash > Validate Modular Hockey Character Assets`: `MODULAR_CHARACTER_ASSETS_VALID`.
- PASS — `IceClash > Run Modular Character Smoke Check`: `MODULAR_CHARACTER_SMOKE_PASS players=10`.
- PASS — `IceClash > Run Integrated Skates Gameplay Smoke Check`: `INTEGRATED_SKATES_GAMEPLAY_SMOKE_PASS states=Idle,Running skaters=10 goalies=2`.
- PASS — `IceClash > Run Phase 1 PvE Smoke Check`: `PHASE1_PVE_SMOKE_PASS`, including `skaters=10`, `modularHumanoids=12`, `boundSkaters=10`, `idleGoalies=2`, and `twoHandIK=true`.
- PASS — production YAML search found no `Skate_L_v1`, `Skate_R_v1`, Air Squat, or retired `Running.anim` references in either production prefab, the modular test scene, or `MaleSkater.controller`.
- PASS — `git diff --check` reported no whitespace errors.
