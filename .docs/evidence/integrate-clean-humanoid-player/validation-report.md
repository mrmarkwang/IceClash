# Clean Humanoid Gameplay Integration Validation

Date: 2026-09-01  
Unity: 6000.5.9f1  
Result: PASS

## Scenario results

| Scenario | Result | Evidence |
|---|---:|---|
| 1. Clean production asset isolation | PASS | Immutable clean hashes match; production generation emitted `CLEAN_PLAYER_INTEGRATION_ASSETS_PASS avatarValid=true avatarHuman=true states=Idle,Running`. |
| 2. Authoritative gameplay hierarchy and collider | PASS | `HockeyPlayer.prefab` remains the root; `Resources/Skater.prefab` remains its connected variant; CharacterController serialization is unchanged; no collider or Rigidbody exists below `Visual`. |
| 3. Gameplay-driven Animator bridge | PASS | Deterministic Idle, Forward, Turn, Backward, and Braking values passed; Idle/Running transition succeeds; `applyRootMotion=false`; Animator evaluation does not move or rotate the gameplay root. |
| 4. WASD and virtual-joystick regression | PASS | Real Input System keyboard state and real UI pointer/drag joystick events independently reached the unchanged movement pipeline and emitted both named input markers. |
| 5. Camera, puck, and action preservation | PASS | Full `PHASE1_PVE_SMOKE_PASS` and modular puck/equipment/IK smoke passed; camera target remained the gameplay root. |
| 6. Ice alignment and deformation evidence | PASS | Visible-mesh contacts measured `leftY=0.1983`, `rightY=0.2017` against ice `y=0.200` (1.7 mm error each); five visual captures reviewed. |
| 7. Inspector, hierarchy, and attachment paths | PASS | Interactive Unity capture shows the expanded root/Visual hierarchy, clean Avatar, `MaleSkater` controller, and Apply Root Motion disabled; all five Humanoid bones resolve. |

## Production assets

- Gameplay prefab: `Assets/_Project/Prefabs/HockeyPlayer.prefab`
- Resource variant: `Assets/_Project/Prefabs/Resources/Skater.prefab`
- Production visual prefab: `Assets/Characters/Male/Male_Base_v1_1/Male_Base_v1_1_Clean_Visual.prefab`
- Animator controller: `Assets/Characters/Male/Male_Base_v1_1/Animation/MaleSkater.controller`
- Generated Idle clip: `Assets/Characters/Male/Male_Base_v1_1/Animation/MaleSkater_Idle.anim`
- Clean model and Avatar source: `Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean.fbx`
- Temporary Running source: `Assets/Characters/Male/Male_Base_v1/Meshy_AI_Navy_Training_Pose_biped_Animation_Running_withSkin.fbx`, clip `Armature|Armature|running|baselayer`

The controller contains only `Idle` (default) and temporary `Running`. Parameters are `Speed`, `ForwardAmount`, `TurnAmount`, `IsMoving`, `IsBackward`, `IsBraking`, `IsSprinting`, and `CrossoverDirection`. Both `IsMoving` transitions have no exit time and a 0.10 second duration.

## Transform and physics result

- Gameplay `Visual`: local position `(0, 0.292609, 0)`, local rotation `(0, 0, 0)`, local scale `(1, 1, 1)`.
- Clean visual child / production prefab: local position `(0, 0, 0)`, local rotation `(0, 0, 0)`, uniform local scale `(1.65, 1.65, 1.65)`.
- Runtime gameplay-root scale remains the existing uniform `(0.68, 0.68, 0.68)`.
- Root CharacterController before/after: enabled; center `(0,0,0)`; height `2`; radius `0.45`; slope limit `45`; step offset `0.3`; skin width `0.08`; minimum move distance `0.001`.
- Ordered root component types before/after: `Transform`, `CharacterController`, `HockeyStickRig`, `HockeyEquipmentLoadout`, `HockeyCharacterPresentation`.
- Root motion: disabled.
- Visual physics: no Rigidbody, CharacterController, MeshCollider, or Collider below `Visual`.
- Renderer result: the old `HumanoidVisual` source is absent; the clean production visual is the only humanoid body. Existing modular presentation equipment remains intentionally present.

## Runtime preservation result

- Keyboard: PASS, `LocalPlayerInput > PlayerInputController > PlayerController > PlayerMovementController`.
- Virtual joystick: PASS, `VirtualJoystick > PlayerInputController > PlayerController > PlayerMovementController`.
- Acceleration, deceleration, analog direction, and turning: PASS through the full prototype smoke.
- Camera: PASS; `HockeyCameraController.Target` remains the controlled gameplay-root transform.
- Collider: PASS; the existing root CharacterController remains the only gameplay actor collider.
- Puck/actions: PASS; claim/carry/release, Shoot, Pass, Deke, Check, and Switch regression assertions remain green.
- Animation authority: PASS; the presentation bridge writes Animator parameters only and performs no transform translation/rotation.

## Bone paths

- Head: `Visual/Male_Base_v1_1_Clean_Visual/Armature.001/Hips/Spine02/Spine01/Spine/neck/Head`
- LeftHand: `Visual/Male_Base_v1_1_Clean_Visual/Armature.001/Hips/Spine02/Spine01/Spine/LeftShoulder/LeftArm/LeftForeArm/LeftHand`
- RightHand: `Visual/Male_Base_v1_1_Clean_Visual/Armature.001/Hips/Spine02/Spine01/Spine/RightShoulder/RightArm/RightForeArm/RightHand`
- LeftFoot: `Visual/Male_Base_v1_1_Clean_Visual/Armature.001/Hips/LeftUpLeg/LeftLeg/LeftFoot`
- RightFoot: `Visual/Male_Base_v1_1_Clean_Visual/Armature.001/Hips/RightUpLeg/RightLeg/RightFoot`

## Visual review

Front, side, and moving/turning captures show one upright clean humanoid per gameplay root with the visible soles aligned to the ice in Idle and normal alternating foot lift in the temporary Running stride. No sinking, duplicate body, root-motion slide, shoulder collapse, elbow inversion, wrist blow-up, groin tear, knee inversion, or new ankle separation was observed at gameplay distance. The previously accepted rear shorts-hem flare, mild ankle faceting, and thin wrist-border line remain minor; the blocky gloves, stick, skates, and other equipment are the pre-existing modular presentation rather than new clean-mesh deformation.

## Evidence provenance

- `front-gameplay.png`, `side-gameplay.png`, and `moving-turning-gameplay.png`: rendered from `PrototypeArena` by the graphical `CaptureGameplayEvidenceBatch` Play Mode runner; marker `CLEAN_PLAYER_GAMEPLAY_CAPTURES_PASS images=3`.
- `animator-inspector.png`: interactive Unity Editor 6000.5.9f1 capture at 1376x768 with `Male_Base_v1_1_Clean_Visual` selected, showing `MaleSkater`, the clean Avatar, Apply Root Motion off, Always Animate, and the Rig Builder.
- `hierarchy.png`: distinct 700x768 hierarchy-focused crop of the interactive Unity Editor capture, with `HockeyPlayer > Visual > Male_Base_v1_1_Clean_Visual` expanded. Its SHA-256 differs from the full Inspector capture.

## Immutable asset hashes

Before and after values match:

- Clean FBX: `a5682777ea0f7b14236fb96a5c3f19826b79113997bb929772b2d92cc892a159`
- Clean FBX meta: `602cef15f63e2edfad14ee120e8c302ffa38cd729058d95216ec1d2d07cd1d3f`
- Clean test prefab: `ea32b20a09953006094f4fb7332e4e9fcf205e94530f3d18e4566c6d95fafc17`
- Clean test controller: `bb0d50d15882fc55847564eace37dca2d7e758f1be08fa817e085cfe5f5da58d`
- Clean test scene: `eb42ad7c6fa3e18f7a8c6b1416d715811c495992cf34ba6c1197fcd5433528e6`

The prohibited runtime-source diff is empty for `Scripts/Player`, `Scripts/Input`, `Scripts/Camera`, `Scripts/Puck`, `Scripts/Gameplay`, `LocalMatchSetup.cs`, and `PrototypeArenaBootstrap.cs`.

## Validation commands and markers

- Clean integration asset batch: exit 0; `CLEAN_PLAYER_INTEGRATION_ASSETS_PASS avatarValid=true avatarHuman=true states=Idle,Running`.
- Modular asset batch: exit 0; `MODULAR_CHARACTER_ASSETS_PASS`.
- Modular Play Mode smoke: exit 0; `MODULAR_CHARACTER_SMOKE_PASS players=10`.
- Full prototype Play Mode smoke (`/tmp/iceclash-prototype-smoke.log`): exit 0; `CLEAN_PLAYER_ANIMATOR_PARAMETERS_PASS cases=Idle,Forward,Turn,Backward,Braking`, `PHASE1_PVE_SMOKE_PASS`, and all requested clean-player markers.
- `git diff --check`: PASS.

Exact clean-player marker lines:

```text
CLEAN_PLAYER_INTEGRATION_ASSETS_PASS avatarValid=true avatarHuman=true states=Idle,Running
CLEAN_PLAYER_GAMEPLAY_CAPTURES_PASS images=3
CLEAN_PLAYER_INPUT_WASD_PASS source=Keyboard pipeline=LocalPlayerInput>PlayerInputController>PlayerController>PlayerMovementController
CLEAN_PLAYER_INPUT_JOYSTICK_PASS source=PointerEvent pipeline=VirtualJoystick>PlayerInputController>PlayerController>PlayerMovementController
CLEAN_PLAYER_CAMERA_COLLIDER_PASS target=GameplayRoot rootMotion=false
CLEAN_PLAYER_ANIMATOR_PARAMETERS_PASS cases=Idle,Forward,Turn,Backward,Braking
CLEAN_PLAYER_ANIMATION_PASS states=Idle,Running
CLEAN_PLAYER_FOOT_ALIGNMENT_MEASURE left=0.0017 right=0.0017 leftY=0.1983 rightY=0.2017 rootY=1.0000 visualLocalY=0.2926 iceY=0.200
CLEAN_PLAYER_FOOT_ALIGNMENT_PASS left=0.0017 right=0.0017 iceY=0.200
```

## Existing gameplay files modified

- `Assets/_Project/Prefabs/HockeyPlayer.prefab`
- `Assets/_Project/Scenes/ModularCharacterTest.unity`
- `Assets/_Project/Tests/Editor/HockeyCharacterAssetSetup.cs`
- `Assets/_Project/Scripts/Hockey/Character/HockeyCharacterPresentation.cs`
- `Assets/_Project/Scripts/Hockey/Character/ModularCharacterTestHarness.cs`
- `Assets/_Project/Scripts/Hockey/PrototypeArenaSmokeCheck.cs`

`Assets/_Project/Prefabs/Resources/Skater.prefab` was validated as the connected `HockeyPlayer.prefab` variant and did not require a serialized file change.
