# E2E: Two-Handed Hockey Stick Pose

## Scenario 1 - Dedicated static pose

1. Generate and open the hockey-stick pose validation scene.
2. Enter Play Mode with the player idle and no movement input.
3. View the player from the front, side, and rear.
4. Verify both palms contact the shaft, wrists wrap naturally, elbows are bent without inversion, shoulders remain stable, and the shaft crosses diagonally in front of the torso.
5. Verify `BladeContact` is close to the ice, in front of the skates, slightly right of center, and does not intersect either leg.

## Scenario 2 - Hierarchy and rig contract

1. Inspect the generated player hierarchy and Animation Rigging constraints.
2. Verify the stick hierarchy is `RightHand/StickSocket/Hockey_Stick_Base_v1`.
3. Verify the right-hand target is independent of the stick and the left-hand Two Bone IK targets the equipped stick's `SecondaryGrip`.
4. Verify `LeftElbowHint` is dedicated, outward/downward, and used with a nonzero hint weight.
5. Clear and restore the stick equipment in the existing modular harness; verify IK disables safely while absent and rebinds to the restored `SecondaryGrip`.

## Scenario 3 - Gameplay preservation

1. Open `PrototypeArena` and enter Play Mode.
2. Observe the controlled skater from the gameplay camera while idle.
3. Move with the existing keyboard/joystick path, then stop.
4. Verify movement, camera follow, puck control point, shoot/pass/deke inputs, and gameplay colliders remain unchanged while the visual rig continues to present a two-handed carry.
5. Capture the gameplay-camera view and a close-up of both hands for evidence.
