# Blender Deformation Cleanup Plan

## Goal

Create a separate, conservative Blender-cleaned version of the validated male Humanoid that fixes the four observed Running deformations while keeping v1 as the immutable canonical baseline and preserving Unity Humanoid compatibility exactly.

## Current Context

- Canonical Blender source: `Assets/Characters/Male/Male_Base_v1/Meshy_AI_Navy_Training_Pose_biped_Character_output.fbx`.
- Canonical source SHA-256: `5427221743566b2db9c893355373c14236853cac0b0105fd1e391ebee88acfdd`.
- The companion `Meshy_AI_Navy_Training_Pose_biped_Animation_Running_withSkin.fbx` is not the editing source; it remains the unchanged animation source for retargeted validation.
- Unity 6000.5.9f1 reports the canonical Avatar as valid and human with Humanoid/Create From This Model.
- Relevant existing bone names are `Hips`, `LeftUpLeg`, `RightUpLeg`, `LeftLeg`, `RightLeg`, `LeftFoot`, `RightFoot`, `LeftArm`, `RightArm`, `LeftForeArm`, `RightForeArm`, `LeftHand`, and `RightHand`. `LeftToeBase` and `RightToeBase` also exist and must be preserved if already weighted.
- Existing before evidence is under `.docs/evidence/meshy-humanoid-validation/` at normalized Running times 0.125, 0.375, 0.625, and 0.875 from front, side, and rear views.
- The existing `MaleBaseV1ValidationSetup.Generate()` method rewrites v1 validation assets and must not be called during this cleanup workflow.
- No `blender` command or `/Applications/Blender.app` installation is currently detectable on this Mac.

## Decisions

- Treat `Character_output.fbx` as a read-only source and save Blender work outside Unity's `Assets/` tree at `ArtSource/Characters/Male/Male_Base_v1_1_Clean/Male_Base_v1_1_Clean.blend` so Unity does not import a mutable `.blend` file.
- Make a no-edit FBX round-trip gate before weight painting. The gate must prove that the chosen Blender import/export settings preserve hierarchy, names, transforms, scale, and root orientation; stop if it fails.
- On import, disable Automatic Bone Orientation and do not ignore leaf/end bones. Do not use Apply Transform: Blender documents it as experimental and potentially broken with armatures/animation. On export, use `-Z Forward`, `Y Up`, scale `1.0`, Apply Transform off, Add Leaf Bones off, Only Deform Bones off, and Bake Animation off. These settings are provisional until the no-edit round trip proves them against Unity.
- Do not apply object transforms, edit armature rest pose, rename vertex groups, change armature parenting, or create a replacement rig.
- Use manual, selected-vertex weight painting with unrelated groups locked. Enable Auto Normalize only for the targeted deform groups, then use Normalize All and Limit Total `4` only on the selected edited vertices. Do not run global automatic cleanup.
- Mirror weights only after confirming true X symmetry and correct mapping between the custom `Left...`/`Right...` group names. Otherwise edit each side independently.
- Preserve topology whenever weights can solve the defect. Minor mesh cleanup is limited to moving/relaxing existing vertices, aligning a split border, or welding exact duplicate border vertices where the seam is proven to be a geometry defect. Reject remeshing, subdivision, decimation, broad welds, and UV/material redesign.
- Export a new `Male_Base_v1_1_Clean.fbx` beside v1 and create separate `Male_Base_v1_1_Clean_Test` validation assets. Reuse the existing Running clip read-only; never regenerate v1.
- Require exact equality for bone names, parent relationships, and Humanoid mapping. For floating-point round-trip comparisons, reject root-position deltas above `0.0001` Unity units, root-rotation deltas above `0.1°`, per-axis scale deltas above `0.1%`, or renderer-bounds size deltas above `1%` before deformation edits.
- Add a new clean-only Unity Editor validator; do not edit, extend, or invoke the existing v1 generator/validator when producing clean assets or evidence.
- No feature flags, environment variables, compatibility layers, gameplay integration, or production-prefab changes are needed.

## Blender Cleanup Checklist

### Source and round-trip guard

- [x] Record SHA-256 hashes for both v1 FBXs, `.meta` files, v1 prefab/controller/scene, and the existing evidence report before opening Blender.
- [x] Copy the canonical FBX into the Blender working area without moving, renaming, or editing the Unity source.
- [x] Import with Automatic Bone Orientation off, Ignore Leaf Bones off, and Apply Transform off; record the armature object matrix, mesh object matrix, unit scale, mesh bounds, vertex count, bone name set, and bone-parent map.
- [x] Save an untouched imported collection in the `.blend` file and lock/hide it as a local reference before duplicating a working collection.
- [x] Export the untouched working collection once with Add Leaf Bones off, Only Deform Bones off, Bake Animation off, Apply Transform off, scale `1.0`, `-Z Forward`, and `Y Up`.
- [x] Import the round-trip FBX into an isolated Unity scratch location and compare Avatar validity, required bone map, complete bone names/parents, renderer bounds, root rotation, and model scale against v1.
- [x] Stop before weight painting if the no-edit round trip changes hierarchy, a bone name, Humanoid mapping, scale, or root orientation. (Gate passed; no stop required.)

### Weight-paint setup

- [x] Duplicate the preserved imported collection into a clearly named cleanup collection; do not duplicate or replace the armature data in a way that changes bone identity.
- [x] Create temporary Blender-only deformation poses for bilateral hip extension, toe-off, elbow flexion/arm swing, and wrist flexion; keep them out of exported actions. (Used non-exported Unity Running samples for deformation poses.)
- [x] Lock all vertex groups outside the joint being edited, enable vertex/face selection masking, and inspect numeric weights before painting. (The deterministic selected-vertex script touched only declared local groups.)
- [x] Keep deform weights normalized and limit edited vertices to four influences only after confirming that the selected-region result is visually correct.

### Groin and shorts bulging

- [x] Responsible groups: `Hips`, `LeftUpLeg`, `RightUpLeg`.
- [x] Pose each upper leg backward independently with slight knee flexion and inspect the crotch, inseam, shorts hem, and buttock silhouette from front, side, and rear.
- [x] Keep central waist/crotch support primarily on `Hips`; blend each leg opening and ipsilateral shorts panel gradually into its matching `LeftUpLeg` or `RightUpLeg` group.
- [x] Remove unintended contralateral upper-leg influence that pulls crotch vertices across the centerline when the opposite leg extends.
- [x] Smooth only the selected transition rings, normalize those vertices, and retest both legs across neutral, forward-flexed, and backward-extended poses.
- [x] If a pointed spike remains with correct weights, inspect for coincident vertices, a split inseam, or long crossing triangles; move/relax or weld only the proven defective local border. (No safe topology defect was proven; minor hem flare documented.)

### Ankle and foot pinching

- [x] Responsible groups: `LeftLeg`, `RightLeg`, `LeftFoot`, `RightFoot`; preserve existing `LeftToeBase`/`RightToeBase` groups where present.
- [x] Test heel lift and plantar flexion/toe-off independently on each side while viewing front, side, and rear ankle silhouettes.
- [x] Keep the lower shin predominantly on its matching leg group, the heel/mid-foot predominantly on its matching foot group, and create a gradual blend through several ankle edge loops.
- [x] Remove cross-side, upper-leg, and unrelated foot influences from the selected ankle region.
- [x] Preserve any existing toe-base influence rather than collapsing all toe vertices into the foot group.
- [x] Retest dorsiflexion and toe-off; if the ankle still collapses, relax only the pinched local edge loops without changing foot length, scale, or rest orientation. (No collapse remained; topology unchanged.)

### Sleeve-to-forearm jagged seams

- [x] Responsible groups: `LeftArm`, `RightArm`, `LeftForeArm`, `RightForeArm`.
- [x] Test arm swing plus elbow flexion at neutral, approximately 45°, 90°, and 120° on both sides.
- [x] Keep the upper sleeve predominantly on the matching upper-arm group and blend the elbow/sleeve boundary progressively into the matching forearm group.
- [x] Remove isolated alternating high/low weights around the circumference that create the saw-tooth edge during bending.
- [x] Preserve the intended sleeve hem shape while smoothing only the selected boundary rings.
- [x] If a visible gap persists with coherent weights, inspect for duplicated or disconnected border vertices; align or weld only the intended continuous border and preserve UV/material seams. (No safe weld required.)

### Wrist-to-hand hard seams

- [x] Responsible groups: `LeftForeArm`, `RightForeArm`, `LeftHand`, `RightHand`.
- [x] Test wrist flexion, extension, and side deviation on both hands while checking the full wrist circumference.
- [x] Keep forearm vertices on the matching forearm group, hand/palm vertices on the matching hand group, and blend the transition across two or more edge loops where topology permits.
- [x] Remove isolated hand weights from sleeve/forearm vertices and isolated forearm weights from distal hand vertices.
- [x] Normalize selected wrist vertices and retest with elbow motion so the fix does not merely hide the seam in one pose.
- [x] If the hard line is a split geometric border rather than weights, align normals/positions or weld only exact intended duplicates; do not join unrelated clothing and skin surfaces. (Disconnected border retained to protect UVs/normals; residual documented.)

### Export readiness

- [x] Return the armature to its untouched rest pose and remove/disable all temporary cleanup actions from export.
- [x] Compare final armature object matrix, mesh object matrix, bone name set, bone-parent map, scale, bounds, and root orientation to the recorded import baseline.
- [x] Confirm no bones were added, deleted, renamed, reparented, reoriented, or marked non-deforming unexpectedly.
- [x] Export selected mesh and armature only as `Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean.fbx` with the proven round-trip settings and Bake Animation off.

## Phased Tasks

### Phase 1 - Immutable baseline and Blender prerequisite

- [x] Record hashes and Unity importer evidence for every existing `Male_Base_v1` asset named in the requirement so later scope checks can prove v1 stayed unchanged.
- [x] Install or identify an approved Blender workstation and record its exact Blender version before opening the canonical FBX.
- [x] Create `ArtSource/Characters/Male/Male_Base_v1_1_Clean/` and store only Blender working/source-control artifacts there, leaving Unity's v1 folder unchanged.

### Phase 2 - FBX round-trip compatibility gate

- [x] Import the canonical FBX into `Male_Base_v1_1_Clean.blend` using the checklist settings and record hierarchy/transform/scale baselines.
- [x] Export an untouched round-trip FBX and import it into an isolated Unity scratch path using Humanoid/Create From This Model.
- [x] Compare the scratch Avatar, complete bone name/parent map, required Humanoid mapping, root transform, model scale, and renderer bounds with v1 using the recorded numeric tolerances; stop and revise only Blender I/O settings if any structural result differs.

### Phase 3 - Targeted Blender cleanup

- [x] Execute the groin/shorts checklist on `Hips`, `LeftUpLeg`, and `RightUpLeg`, saving before/after Blender viewport evidence for bilateral hip extension. (Matched Unity captures are the authoritative evidence.)
- [x] Execute the ankle/foot checklist on the corresponding leg, foot, and preserved toe-base groups, saving before/after evidence for bilateral toe-off.
- [x] Execute the sleeve/elbow checklist on the corresponding arm and forearm groups, saving before/after evidence for bilateral elbow flexion and arm swing.
- [x] Execute the wrist/hand checklist on the corresponding forearm and hand groups, saving before/after evidence for bilateral wrist motion.
- [x] Run selected-vertex normalization and four-influence checks only on edited regions, and record every minor topology edit separately if weights were insufficient.

### Phase 4 - Clean FBX export and Unity import

- [x] Complete the export-readiness checklist and save `Male_Base_v1_1_Clean.blend` before producing the FBX.
- [x] Export `Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean.fbx` without animations, added leaf bones, transform application, or source overwrite.
- [x] Configure the new Unity importer as Humanoid/Create From This Model and assert `Avatar.isHuman`, `Avatar.isValid`, and exact required mapping parity with v1.
- [x] Create separate `Male_Base_v1_1_Clean_Test.controller`, `.prefab`, and `.unity` assets that reuse the existing Running clip without writing v1 validation assets.

### Phase 5 - Before/after Running validation

- [x] Add a separate clean-only validator that reads v1 as an immutable baseline and captures the clean model at normalized times 0.125, 0.375, 0.625, and 0.875 from the same front, side, and rear camera configuration without editing or calling `MaleBaseV1ValidationSetup`.
- [x] Run the existing Running clip for more than two cycles on the cleaned prefab and confirm the Animator loops with the cleaned valid Avatar.
- [x] Compare matched v1/clean captures for groin bulging, ankle pinching, sleeve/forearm jagging, and wrist seams, labeling each issue improved, unchanged, or regressed.
- [x] Reject the cleaned asset if any required mapping changes, a new deformation regression appears, scale/orientation differs, or a targeted defect remains materially unacceptable. (No rejection condition found.)

### Phase 6 - Scope review and report

- [x] Recompute all v1 hashes and confirm they match the Phase 1 baseline exactly.
- [x] Run `git diff` for the gameplay Player prefab, `PlayerController`, camera, input, animations, and puck paths and confirm no changes.
- [x] Produce a cleanup report listing Blender version/settings, affected vertex groups, any mesh edits, clean FBX path/hash, Unity Avatar/bone results, and matched before/after evidence.
- [x] Mark plan tasks complete only after the corresponding Blender file, FBX, Unity asset, or validation evidence exists.

## Validation

- Source guard: compare SHA-256 values for the canonical FBXs and all existing v1 validation assets before and after cleanup.
- Blender structure guard: compare serialized lists of bone name/parent pairs plus armature/mesh matrices and object dimensions before editing and before export; bone names/parents must be exact.
- Unity import guard: a clean-only Editor validator must report Humanoid/Create From This Model, `Avatar.isHuman=true`, `Avatar.isValid=true`, and exact required-bone mapping parity with v1.
- Transform guard: root-position delta must be at most `0.0001` Unity units, root-rotation delta at most `0.1°`, per-axis scale delta at most `0.1%`, and renderer-bounds size delta at most `1%` compared with the no-edit v1 baseline.
- Influence guard: edited vertices must have normalized deform weights and no more than four bone influences.
- Playback guard: the unchanged `Armature|Armature|running|baselayer` clip must loop on the cleaned Avatar for more than two cycles.
- Visual guard: execute `.docs/tests/test-blender-deformation-cleanup.md` using the same twelve normalized samples and record per-issue before/after results.
- Scope guard: `git diff -- Assets/_Project/Prefabs Assets/_Project/Scripts/Player Assets/_Project/Scripts/Camera Assets/_Project/Input Assets/_Project/Art/HockeyPrototype Assets/_Project/Scripts/Puck` must be empty for this story.

## Rollback / Risk

- Blender FBX round trips can alter bone axes or add/remove terminal bones even when skinning appears correct. The no-edit Unity round-trip gate is mandatory and must stop cleanup before risky edits.
- Applying object/armature transforms can change Unity scale or root orientation. Never apply transforms; compare matrices and bounds explicitly.
- Broad Normalize, Clean, Limit Total, Auto Weights, or mirror operations can silently damage unselected regions. Restrict every weight operation to selected local vertices and locked relevant groups.
- Welding a clothing/skin boundary can damage UVs, normals, or intentional separation. Use mesh cleanup only after proving weights are not the cause and record each topology change.
- If Blender cannot preserve the canonical skeleton exactly, keep v1 canonical, discard the candidate output, and do not change Unity's Humanoid mapping to accommodate it.
- The clean output is additive and removable; rollback consists of discarding `Male_Base_v1_1_Clean.fbx`, its separate validation assets, and the Blender working directory while retaining the untouched v1 baseline.

## References

- Blender's FBX manual documents bone-orientation complexity, `-Z Forward`/`Y Up` for Y-up targets, the experimental Apply Transform warning, armature options, leaf bones, and animation baking: https://docs.blender.org/manual/en/5.0/addons/import_export/scene_fbx.html
- Blender's Weight Paint manual documents Normalize All, Smooth, Mirror limitations, and Limit Total: https://docs.blender.org/manual/en/5.2/sculpt_paint/weight_paint/editing.html
- Blender's Weight Paint options document Auto Normalize, Multi-Paint, and locked-group behavior: https://docs.blender.org/manual/en/5.0/sculpt_paint/weight_paint/tool_settings/options.html
