# E2E Visual Spec: Production Stick in Gameplay

## Preconditions

- Generate and validate the modular hockey character assets.
- Open `Assets/_Project/Scenes/PrototypeArena.unity` and enter Play Mode.

## Scenario 1 - Gameplay prefab dependency

1. Inspect `HockeyPlayer.prefab` and its `Resources/Skater.prefab` variant.
2. Confirm the equipped Stick contains a nested `Hockey_Stick_Base_v1` production prefab.
3. Confirm neither gameplay prefab depends on `hockey_stick_002.fbx` or `LowPolyHockeyStick.mat`.

Expected: the production prefab/material is the sole rendered stick visual while existing Stick marker objects remain.

## Scenario 2 - Arena rendering

1. Start PrototypeArena Play Mode.
2. Observe the controlled skater and several AI skaters from the gameplay camera.
3. Confirm sticks are dark production meshes rather than orange low-poly meshes and remain attached while skating.

Expected: production sticks are visible on active skaters with no detached or backward model.

## Scenario 3 - Gameplay contract preservation

1. Run the modular character validator and arena smoke check.
2. Move the controlled skater and observe the blade near its existing control point.
3. Confirm no errors are logged for the two-hand rig, puck interaction, player input, camera, or match setup.

Expected: visual substitution does not alter existing mechanics or control behavior.

## Scenario 4 - Reference hockey carry

1. Start PrototypeArena Play Mode and observe the controlled skater plus nearby AI skaters.
2. Confirm the upper hand sits beside the torso, the lower hand is visibly lower and farther forward, and both remain on the production shaft.
3. Confirm the shaft descends diagonally across the skater toward the puck and the blade remains on the ice at the existing control point.
4. Observe several moving skaters to rule out detached hands, inverted arms, an upright carried-prop silhouette, or a backward blade.

Expected: the pose reads like the supplied skating reference while gameplay behavior and puck-control placement remain unchanged.
