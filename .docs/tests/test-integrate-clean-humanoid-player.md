# Integrate Clean Humanoid Player Validation

## Preconditions

- The clean FBX and its validation asset hashes are recorded.
- The clean FBX imports as Humanoid/Create From This Model with a valid human Avatar.
- The production visual prefab and `MaleSkater.controller` have been generated.
- `HockeyPlayer.prefab` and its `Resources/Skater.prefab` variant have been regenerated from the integration-aware generator.

## Scenario 1 - Clean production asset isolation

1. Compare post-generation hashes of the clean FBX, its `.meta`, and clean test prefab/controller/scene with the baseline.
2. Inspect the production visual prefab's corresponding model source, Animator Avatar/controller/root-motion settings, local transform, and components.
3. Inspect the production controller's parameters, states, and motions, confirming Running resolves to the exact `Armature|Armature|running|baselayer` subasset from `Assets/Characters/Male/Male_Base_v1/Meshy_AI_Navy_Training_Pose_biped_Animation_Running_withSkin.fbx`, is Humanoid motion, and loops.

Expected: validation assets are byte-for-byte unchanged; the production prefab references the clean model/Avatar, has no gameplay physics/colliders, uses zero local position/rotation with calibrated uniform scale `1.65`, and keeps root motion off; the controller contains only Idle and temporary Running with all eight requested parameters. Idle is default; Idle-to-Running uses `IsMoving=true`, Running-to-Idle uses `IsMoving=false`, both disable exit time and use `0.10s` duration.

## Scenario 2 - Authoritative gameplay hierarchy and collider

1. Open `HockeyPlayer.prefab` and inspect the root CharacterController values and component set.
2. Expand `Visual/Male_Base_v1_1_Clean_Visual` and confirm the production clean model is nested there.
3. Inspect all descendants of `Visual` for Rigidbody, CharacterController, Collider, and MeshCollider components.
4. Inspect `Resources/Skater.prefab` and confirm it remains a connected variant of `HockeyPlayer.prefab`.
5. Compare the root CharacterController center, height, and radius with the recorded baseline.

Expected: the gameplay root remains authoritative and unchanged; only the clean visual appears below `Visual`; no visual physics/collider exists; the root collider stays enabled with the same settings; the resource variant relationship remains intact.

## Scenario 3 - Gameplay-driven Animator bridge

1. Instantiate a gameplay player and bind its `HockeyCharacterPresentation` to the runtime `PlayerController`.
2. Sample idle, forward velocity, turning velocity/input, low-input deceleration, and backward/opposing motion cases using the exact `0.05 m/s`, `0.1` input, and `-0.15` opposing-dot thresholds from the plan.
3. Read all Animator parameter values and observe the active state.
4. Compare the gameplay root transform before and after Animator updates with movement disabled.

Expected: Speed reflects planar velocity magnitude; zero speed clears directional/braking values; ForwardAmount uses root-forward dot velocity direction; TurnAmount/CrossoverDirection use signed root-forward-to-velocity angle divided by 180 with positive root-right turns; input is converted using the movement controller's Main Camera planar axes; IsBackward and IsBraking use the fixed thresholds; IsSprinting is false; Idle/Running changes occur; root motion stays off; Animator updates do not translate or rotate the gameplay root.

## Scenario 4 - WASD and virtual-joystick movement regression

1. Enter Play Mode in `PrototypeArena` and identify the selected gameplay-root player, camera target, collider settings, and baseline movement tuning.
2. Queue `KeyboardState(Key.W)` through the Input System, update it, and independently observe `LocalPlayerInput.Move`, `PlayerInputController.Move`, `PlayerController.MoveInput`, then acceleration/velocity in `PlayerMovementController`; release the key and observe deceleration.
3. With keyboard neutral, dispatch pointer-down and drag events through `ExecuteEvents` to the real `VirtualJoystick`, independently observe `VirtualJoystick.Direction`, `PlayerInputController.Move`, `PlayerController.MoveInput`, then acceleration/velocity in `PlayerMovementController`; dispatch pointer-up and observe deceleration.
4. Confirm the clean humanoid follows root position/rotation and animation state without a second movement delta or visible root-motion slide.

Expected: both input sources independently reach the unchanged movement pipeline and emit their exact `CLEAN_PLAYER_INPUT_*_PASS` markers; movement/turning/deceleration smoke assertions pass; the root CharacterController remains the only actor collider; the humanoid follows and animates without duplicate movement.

## Scenario 5 - Camera, puck, and action preservation

1. Confirm `HockeyCameraController.Target` is the selected player's gameplay-root transform before and after switching players.
2. Exercise existing puck claim/carry/release plus Shoot, Pass, Deke, Check, and Switch smoke paths.
3. Inspect camera composition/retarget assertions and puck/stick control-point assertions.

Expected: camera target and behavior remain root-based; every existing action and puck assertion passes; no clean-model bone or mesh transform becomes a gameplay authority.

## Scenario 6 - Ice alignment and deformation evidence

1. Bake the idle SkinnedMeshRenderer vertices, partition left/right sole contacts by foot-bone side relative to Hips, and verify each local minimum is within `0.03` of the runtime-derived contact target `(iceY 0.2 - spawnY 1.0) / rootScale 0.68 = -1.1764706`; verify each transformed contact is within `0.03` world units of prototype ice `y=0.2`, then capture the front gameplay view. Confirm the CharacterController serialization is unchanged rather than using it to compensate for the scaled visual offset.
2. Capture a side gameplay view and check floating/sinking and forward/back silhouette.
3. Capture a moving/turning view and inspect shoulders, elbows, wrists, hips/groin, knees, and ankles.
4. Check specifically for the known acceptable rear shorts-hem flare, mild ankle faceting, and thin disconnected wrist-border line.

Expected: both quantitative contact checks pass and emit `CLEAN_PLAYER_FOOT_ALIGNMENT_PASS`; the character is neither floating nor sunk, no non-uniform scale is used, no duplicate body is visible, and no new material deformation regression appears; any accepted residual remains documented.

## Scenario 7 - Inspector, hierarchy, and attachment paths

1. Capture the gameplay player's hierarchy expanded through `Visual/Male_Base_v1_1_Clean_Visual`.
2. Capture the humanoid Animator Inspector showing the clean Avatar, production controller, and Apply Root Motion disabled.
3. Resolve `HumanBodyBones.Head`, `LeftHand`, `RightHand`, `LeftFoot`, and `RightFoot` through the production Animator and record their paths relative to the gameplay root.

Expected: screenshots visibly prove the requested integration state and every required future attachment transform resolves to a stable path without adding new equipment.

## Durable result record

After all scenarios, write `.docs/evidence/integrate-clean-humanoid-player/validation-report.md` with one PASS/FAIL row per scenario; exact image provenance; immutable hashes; asset/Avatar/controller paths; Visual transform; root components and CharacterController before/after values; placeholder-renderer, collider, root-motion, input, camera, deformation, and bone-path results; exact validation markers; and the complete modified-file list.
