# Blender Deformation Cleanup

## Problem

The validated `Male_Base_v1` character is a correct Unity Humanoid, but the supplied mesh and skin weights show visible deformation defects during the Running cycle: groin/shorts bulging under hip extension, ankle/foot pinching during toe-off, jagged sleeve-to-forearm seams, and hard wrist-to-hand seams. Editing the canonical asset in place or changing its skeleton could invalidate the known-good Humanoid baseline.

## Requirement

Use the validated canonical base FBX as the Blender source for a conservative, weight-paint-first cleanup. Preserve the source asset, skeleton hierarchy, bone names, Humanoid compatibility, mesh scale, and root orientation. Export the result as a distinct `Male_Base_v1_1_Clean.fbx`, import it into Unity as Humanoid/Create From This Model, and compare its Running deformation against the unchanged v1 baseline.

## Acceptance Criteria

- [ ] The canonical source is `Assets/Characters/Male/Male_Base_v1/Meshy_AI_Navy_Training_Pose_biped_Character_output.fbx`, and its pre-cleanup SHA-256 remains unchanged after the workflow.
- [ ] No existing `Male_Base_v1` FBX, importer metadata, prefab, controller, scene, animation, evidence image, or validation utility is regenerated or modified.
- [ ] The Blender working file preserves the imported armature hierarchy, every bone name, armature/mesh object transforms, mesh scale, and root orientation.
- [ ] Cleanup uses targeted weight painting first; minor mesh cleanup is used only where evidence shows weights alone cannot remove a seam, spike, gap, or pinch.
- [ ] Groin/shorts vertices transition cleanly among `Hips`, `LeftUpLeg`, and `RightUpLeg` without the pointed bulge seen when either leg extends backward.
- [ ] Ankle/foot vertices transition cleanly between the corresponding `LeftLeg`/`LeftFoot` and `RightLeg`/`RightFoot` groups without severe narrowing or twisting during toe-off.
- [ ] Sleeve/elbow vertices transition cleanly between `LeftArm`/`LeftForeArm` and `RightArm`/`RightForeArm` without jagged separation during arm swing or elbow flexion.
- [ ] Wrist vertices transition cleanly between `LeftForeArm`/`LeftHand` and `RightForeArm`/`RightHand` without the hard visible seam during wrist motion.
- [ ] The cleaned output is saved as `Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean.fbx`; it does not overwrite or replace v1.
- [ ] Unity imports the cleaned FBX with Animation Type Humanoid and Avatar Definition Create From This Model, producing a valid human Avatar with the same required mapping as v1.
- [ ] The existing Running clip is retargeted to a separate cleaned-character test prefab/controller/scene and plays in a loop without modifying either FBX animation source.
- [ ] Before/after captures use the same Running clip, normalized sample times, camera views, material, framing, and lighting, and document whether each of the four defects improved, remained, or regressed.
- [ ] The gameplay Player prefab, `PlayerController`, camera, input system, animations, puck logic, and other gameplay systems remain unchanged.

## Constraints

- Do not rename, add, remove, reparent, reconnect, automatically orient, or apply transforms to bones.
- Do not apply automatic armature weights, remesh, decimate, subdivide, or globally smooth all vertex groups.
- Keep Unity-compatible four-bone skinning influences per vertex and normalized deform weights.
- Do not export temporary Blender pose actions or bake animation into the cleaned model FBX.
- Stop the workflow if an FBX round trip changes hierarchy, required bone names, scale, or root orientation; do not compensate by changing Unity's Humanoid mapping.
- Blender is not currently detectable on this Mac, so execution requires a compatible Blender installation or an external Blender workstation.

## Non-Goals

- Rebuilding or regenerating the Meshy model.
- Changing character proportions, clothing design, materials, UV layout, or textures for artistic polish.
- Editing the Running clip or any existing Unity animation asset.
- Replacing the gameplay Player prefab or integrating the cleaned mesh into gameplay.
- Adding equipment, controllers, camera behavior, input behavior, or puck logic.
