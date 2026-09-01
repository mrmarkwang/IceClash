# Male Base v1.1 Blender Deformation Cleanup Report

## Outcome

`Male_Base_v1_1_Clean.fbx` was created as an additive cleaned candidate. The canonical v1 FBXs, metadata, prefab, controller, scene, and before-evidence report remain SHA-256 identical to the recorded baseline. No gameplay, camera, input, animation-source, or puck files changed.

Unity 6000.5.9f1 imports the cleaned model with `Animation Type: Humanoid` and `Avatar Definition: Create From This Model`. Its Avatar is valid and human. All required Humanoid mappings are identical to v1.

## Files

- Canonical source: `Assets/Characters/Male/Male_Base_v1/Meshy_AI_Navy_Training_Pose_biped_Character_output.fbx`
- Clean FBX: `Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean.fbx`
- Blender source: `ArtSource/Characters/Male/Male_Base_v1_1_Clean/Male_Base_v1_1_Clean.blend`
- Clean prefab: `Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean_Test.prefab`
- Clean controller: `Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean_Test.controller`
- Clean scene: `Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean_Test.unity`
- After captures: `.docs/evidence/meshy-humanoid-cleanup/after/`
- Canonical before captures: `.docs/evidence/meshy-humanoid-validation/`

## Blender cleanup

- Blender: 5.2.1 LTS.
- Import: automatic bone orientation off, leaf/end bones retained, apply/bake-space transform off, animation off.
- Export: `-Z Forward`, `Y Up`, scale `1.0`, apply transform off, add leaf bones off, deform-only off, animation baking off.
- Edited vertices: 1,557, all normalized and limited to at most four influences after editing.
- Groin/shorts: 545 vertices; `Hips`, `LeftUpLeg`, `RightUpLeg`; removed local lower-leg and contralateral upper-leg pull.
- Ankle/foot: 290 vertices; `LeftLeg`/`RightLeg`, `LeftFoot`/`RightFoot`; retained existing toe-base influence and spread the ankle blend across multiple loops.
- Sleeve/forearm: 283 vertices; `LeftArm`/`RightArm`, `LeftForeArm`/`RightForeArm`; replaced alternating seam weights with a continuous elbow transition.
- Wrist/hand: 439 vertices; `LeftForeArm`/`RightForeArm`, `LeftHand`/`RightHand`; applied the same continuous profile to both sides of the disconnected wrist border.
- Topology edits: 0.
- Armature/rest-pose/transform edits: 0.
- Automatic weights/remesh/subdivision/decimation: not used.

## Structural verification

- Bone count: 24.
- Bone names and parent map: exact match.
- Vertices/polygons: 7,406 / 14,568, exact match.
- Armature matrix maximum delta: `1.19e-9`.
- Mesh matrix maximum delta: `1.19e-7`.
- Unity Hips-subtree hierarchy: 23 descendants plus Hips, exact names/parents and within transform tolerances.
- Unity renderer-bounds maximum relative delta: `0.7849%` (gate: `1%`).
- Clean FBX SHA-256: `a5682777ea0f7b14236fb96a5c3f19826b79113997bb929772b2d92cc892a159`.
- Blend SHA-256: `2cce51791fab2b34acbf5da32d805c252261987a4ec5c948167091d54ac54e20`.

## Unity Humanoid mapping

The clean Avatar maps Hips=`Hips`, Spine=`Spine02`, Chest=`Spine01`, Neck=`neck`, Head=`Head`, bilateral Shoulder=`LeftShoulder`/`RightShoulder`, Upper Arm=`LeftArm`/`RightArm`, Lower Arm=`LeftForeArm`/`RightForeArm`, Hand=`LeftHand`/`RightHand`, Upper Leg=`LeftUpLeg`/`RightUpLeg`, Lower Leg=`LeftLeg`/`RightLeg`, and Foot=`LeftFoot`/`RightFoot`.

## Running validation

- Reused clip: `Armature|Armature|running|baselayer`.
- Length: 0.633 seconds.
- Looping: enabled on the unchanged Running FBX.
- Default clean-controller state: `Running`.
- Automated validation: 2.25 looped cycles sampled successfully.
- Live Unity Play Mode: observed beyond two cycles; the clean Avatar drives the model correctly.
- Matched evidence: 12 images at normalized times 0.125, 0.375, 0.625, and 0.875 from front, side, and rear.

## Visual comparison

- Groin/shorts: improved. The centerline no longer receives contralateral upper-leg or lower-leg pull. A small rear shorts-hem flare remains visible in the side 0.375 sample; it is a local silhouette/topology characteristic, so it was left unchanged rather than risking the shorts mesh.
- Ankle/foot: improved. Toe-off transitions are less pinched and the foot retains toe-base behavior. Mild faceting remains at the low-resolution ankle loops, without a new twist or collapse.
- Sleeve-to-forearm: improved. The alternating jagged deformation is reduced on both elbows. Residual surface faceting follows the source topology.
- Wrist-to-hand: improved in motion continuity. A thin hard line remains visible in some side angles because the hand/wrist is a disconnected component border; no weld was attempted because that would risk normals, UVs, and the validated skin.
- Shoulders, armpits, elbows, hips, knees, and previously acceptable regions: no new visible regressions in the matched capture set.

The remaining minor seam/hem characteristics are documented rather than hidden or repaired with risky topology changes.
