# Blender Deformation Cleanup Validation

## Preconditions

- The original v1 SHA-256 baseline has been recorded.
- `Male_Base_v1_1_Clean.fbx` exists as a new file and v1 remains present.
- The clean importer is Humanoid/Create From This Model and its Avatar is valid.
- The clean test controller/prefab/scene reference the existing unchanged Running clip.

## Scenario 1 - Canonical v1 preservation

1. Recompute hashes for the original v1 FBXs, their metadata, prefab, controller, scene, validation utility, evidence report, and twelve baseline images.
2. Compare every value with the pre-cleanup baseline.
3. Inspect git diff for gameplay Player, PlayerController, camera, input, animation, and puck paths.

Expected: every original v1 hash matches and no prohibited gameplay or animation path changed.

## Scenario 2 - Clean Humanoid structural parity

1. Select `Male_Base_v1_1_Clean.fbx` in Unity.
2. Confirm Humanoid/Create From This Model and a valid human Avatar.
3. Compare the complete bone name/parent map with v1.
4. Compare Hips, Spine, Chest, Neck, Head, bilateral Shoulder, UpperArm, LowerArm, Hand, UpperLeg, LowerLeg, and Foot mappings with v1.
5. Confirm root-position delta is at most `0.0001` Unity units, root-rotation delta is at most `0.1°`, per-axis scale delta is at most `0.1%`, and renderer-bounds size delta is at most `1%`.

Expected: structure and required mapping match v1, and scale/root orientation have not changed.

## Scenario 3 - Running playback

1. Open the separate cleaned-character test scene.
2. Enter Play Mode and observe more than two cycles of the existing Running clip.
3. Confirm Running remains the default looping state and the cleaned Avatar drives the mesh.

Expected: playback loops normally without modifying the animation source or using v1's prefab.

## Scenario 4 - Matched before/after deformation review

1. Capture the cleaned model at normalized times 0.125, 0.375, 0.625, and 0.875 from the same front, side, and rear views used for v1.
2. Compare each clean capture to its matching v1 image.
3. Inspect bilateral hip extension for pointed groin/shorts bulging.
4. Inspect bilateral toe-off for ankle narrowing, twisting, and foot pinching.
5. Inspect both arm swings/elbows for jagged sleeve-to-forearm separation.
6. Inspect both wrists for a hard forearm-to-hand seam.
7. Check shoulders, armpits, knees, and all previously acceptable regions for new regressions.

Expected: all four targeted defects are materially improved, no new visible deformation is introduced, and any remaining defect is reported precisely rather than hidden by camera or material changes.

## Scenario 5 - Output isolation

1. Confirm the output filename is exactly `Male_Base_v1_1_Clean.fbx`.
2. Confirm all clean prefab/controller/scene assets use distinct `Male_Base_v1_1_Clean_Test` names.
3. Confirm no clean asset replaces a v1 reference or production gameplay reference.

Expected: the cleaned candidate is fully additive and can be discarded without affecting the canonical v1 or gameplay.

## Execution results — 2026-08-31

- Scenario 1: PASS. Every hash in `.docs/evidence/meshy-humanoid-cleanup/baseline-sha256.txt` matched; prohibited gameplay-path diff was empty.
- Scenario 2: PASS. Unity reported a valid human Avatar, exact required mapping parity, exact Hips-subtree hierarchy, root transforms within tolerance, and `0.7849%` maximum bounds delta.
- Scenario 3: PASS. `Armature|Armature|running|baselayer` remains the default looping state; 2.25 cycles were sampled and more than two cycles were observed in live Play Mode.
- Scenario 4: PASS with documented minor residuals. All 12 matched captures were produced. The four target areas improved without a new regression; slight rear shorts-hem flare, ankle topology faceting, and the disconnected wrist border remain visible.
- Scenario 5: PASS. Clean FBX, controller, prefab, scene, validator, Blender source, and evidence are additive and distinctly named.
