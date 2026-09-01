# Meshy Male Base v1 Humanoid Validation Report

## Imported sources

- Canonical model: `Assets/Characters/Male/Male_Base_v1/Meshy_AI_Navy_Training_Pose_biped_Character_output.fbx`
  - SHA-256: `5427221743566b2db9c893355373c14236853cac0b0105fd1e391ebee88acfdd`
- Running source: `Assets/Characters/Male/Male_Base_v1/Meshy_AI_Navy_Training_Pose_biped_Animation_Running_withSkin.fbx`
  - SHA-256: `17e8584b747b909b7ee0a4731c8f2024e80cdc04731c69c3455a58cbee19678b`

## Humanoid result

- Unity Avatar: `Meshy_AI_Navy_Training_Pose_biped_Character_outputAvatar`
- `Avatar.isHuman`: `true`
- `Avatar.isValid`: `true`
- Canonical importer: Humanoid / Create From This Model
- Running importer: Humanoid / Copy From Other using the canonical Avatar
- Rig Inspector evidence: `rig-importer.jpeg`

Required mapping validated through `Animator.GetBoneTransform`:

- Hips → `Hips`
- Spine → `Spine02`
- Chest → `Spine01`
- Neck → `neck`
- Head → `Head`
- Left Shoulder → `LeftShoulder`
- Right Shoulder → `RightShoulder`
- Left Upper Arm → `LeftArm`
- Right Upper Arm → `RightArm`
- Left Lower Arm → `LeftForeArm`
- Right Lower Arm → `RightForeArm`
- Left Hand → `LeftHand`
- Right Hand → `RightHand`
- Left Upper Leg → `LeftUpLeg`
- Right Upper Leg → `RightUpLeg`
- Left Lower Leg → `LeftLeg`
- Right Lower Leg → `RightLeg`
- Left Foot → `LeftFoot`
- Right Foot → `RightFoot`

## Animation result

- Base FBX clip: `Armature|Armature|clip0|baselayer`
- Running FBX clip: `Armature|Armature|running|baselayer`
- Running duration: `0.633` seconds
- Running is Humanoid motion, loops, and is the default `Running` state of `Male_Base_v1_Test.controller`.
- Live Play Mode observation ran for more than two cycles and showed continuous running motion on `Male_Base_v1_Test`.

## Deformation inspection

Twelve neutral-material captures cover front, side, and rear views at normalized times 0.125, 0.375, 0.625, and 0.875.

- Shoulders/armpits: no clear collapse or tearing was observed; the shoulder-to-torso silhouette remained connected throughout the sampled cycle.
- Elbows/wrists: visible jagged seams occur at the sleeve-to-forearm boundaries, with smaller hard seams at the wrist/hand transitions. These are most visible on the rear and side captures and may be source mesh/topology boundaries rather than Humanoid mapping errors.
- Hips/groin: clear pointed/exaggerated bulging occurs around the shorts/crotch region when a leg is driven backward, most visibly in the side captures.
- Knees: no clear knee collapse or inversion was observed in the sampled poses.
- Ankles: both ankle-to-foot transitions narrow sharply; the trailing foot shows visible twisting/pinching during toe-off.

No skin weights, skeleton topology, or animation curves were changed.

## Generated assets

- Prefab: `Assets/Characters/Male/Male_Base_v1/Male_Base_v1_Test.prefab`
- Controller: `Assets/Characters/Male/Male_Base_v1/Male_Base_v1_Test.controller`
- Scene: `Assets/Characters/Male/Male_Base_v1/Male_Base_v1_Test.unity`
- Prefab gameplay MonoBehaviours: `0`

## Verification

- Unity: `6000.5.9f1`
- Command: `/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/markwang/mw/IceClash -executeMethod IceClash.CharacterValidation.Editor.MaleBaseV1ValidationSetup.GenerateAndValidateBatch -logFile /tmp/iceclash-meshy-humanoid.log`
- Exit code: `0`
- Final marker: `MESHY_HUMANOID_VALIDATION_PASS`
- Existing gameplay prefabs changed: no
