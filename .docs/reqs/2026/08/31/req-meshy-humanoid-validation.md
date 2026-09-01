# Meshy Humanoid Validation

## Problem

The downloaded Meshy male hockey base character has not yet been imported or validated in IceClash. Before it can replace the current capsule-based player presentation, the project needs evidence that Unity recognizes the model as a valid Humanoid, maps the required body bones, and can play the supplied running motion without unacceptable skin deformation.

## Requirement

Import the Meshy male base character and its available running animation into a dedicated `Assets/Characters/Male/Male_Base_v1/` area. Configure the base model to create a Humanoid Avatar, build a gameplay-free test prefab and isolated preview scene, and validate bone mapping, animation playback, and visible deformation. Preserve the existing gameplay Player prefab unchanged.

## Acceptance Criteria

- [ ] `Assets/Characters/Male/Male_Base_v1/` exists and contains the downloaded base-character FBX.
- [ ] The base-character importer uses Humanoid animation with `Create From This Model`, and Unity reports the generated Avatar as valid and human.
- [ ] Hips, Spine, Chest, Neck, Head, both Shoulders, both Upper Arms, both Lower Arms, both Hands, both Upper Legs, both Lower Legs, and both Feet are mapped to non-null transforms in the generated HumanDescription.
- [ ] All animation clips discovered in the imported Meshy FBXs are recorded by name.
- [ ] If the supplied running animation is importable, its clip loops, is the default state of a temporary Animator Controller, and plays on `Male_Base_v1_Test`.
- [ ] A `Male_Base_v1_Test` prefab contains the rigged model, an Animator using the generated Humanoid Avatar, and no gameplay controller scripts.
- [ ] An isolated test scene previews only the validation character and any minimal camera/light/ground needed to inspect it.
- [ ] Shoulders, armpits, elbows, wrists, hips, groin, knees, and ankles are visually inspected during running playback, with each visible issue reported by exact joint/area.
- [ ] Mesh weights are not automatically changed; any deformation repair is deferred unless separately authorized after a clear issue is identified.
- [ ] The existing gameplay Player prefab and gameplay logic remain unchanged.
- [ ] Final evidence reports the FBX path, Avatar validity, required bone mapping, imported clip names, running result, deformation findings, prefab path, and scene path.

## Constraints

- Use Unity 6000.5.9f1, matching `ProjectSettings/ProjectVersion.txt`.
- Do not add skates, gloves, helmet, stick, gameplay movement, puck behavior, shooting, passing, or deking.
- Do not attach gameplay controller scripts to the validation prefab.
- Do not modify `Assets/_Project/Prefabs/HockeyPlayer.prefab` or its gameplay-facing variants.
- Keep temporary animation and preview assets inside the dedicated `Male_Base_v1` validation area.

## Non-Goals

- Replacing the current player capsule or production player presentation.
- Retargeting production hockey animations.
- Editing skin weights, skeleton topology, mesh geometry, clothing, or materials for production quality.
- Integrating hockey equipment or gameplay behavior.
