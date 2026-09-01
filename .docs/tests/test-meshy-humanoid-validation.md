# Meshy Humanoid Validation Test

## Preconditions

- Open the project with Unity 6000.5.9f1.
- Complete the scoped importer/setup utility successfully.
- Confirm the Console has no model-import errors for either Meshy FBX.

## Scenario 1 - Humanoid Avatar and required mapping

1. Select the canonical base FBX in `Assets/Characters/Male/Male_Base_v1/`.
2. Confirm the Rig tab shows Humanoid and Create From This Model.
3. Open Configure Avatar.
4. Confirm Unity reports a valid Humanoid and that Hips, Spine, Chest, Neck, Head, both Shoulders, both Upper Arms, both Lower Arms, both Hands, both Upper Legs, both Lower Legs, and both Feet are mapped.

Expected: the Avatar is valid, every required mapping is present, and Unity shows no blocking configuration error.

## Scenario 2 - Isolated running playback

1. Open `Male_Base_v1_Test.unity`.
2. Select the test character and confirm its Animator references the generated base Avatar and `Male_Base_v1_Test.controller`.
3. Enter Play Mode and observe at least two complete running cycles.
4. Confirm the Running state is active, time advances, and the animation loops without the character leaving the inspection area.

Expected: Running plays continuously on the canonical model with no gameplay scripts or controller behavior.

## Scenario 3 - Deformation inspection

1. Observe a full running cycle from the front.
2. Observe a full running cycle from the side.
3. Observe a full running cycle from the rear or rear three-quarter view.
4. Inspect shoulders, armpits, elbows, wrists, hips, groin, knees, and ankles at their most flexed points.
5. Record each visible collapse, stretch, twist, clipping region, or discontinuity by its exact joint/area.

Expected: either no visible deformation issue is found, or every issue is reported precisely without automatic weight or mesh changes.

## Scenario 4 - Scope preservation

1. Inspect the test prefab components and verify no IceClash gameplay controllers are attached.
2. Check git status/diff for `Assets/_Project/Prefabs/HockeyPlayer.prefab` and `Assets/_Project/Prefabs/Resources/Skater.prefab`.

Expected: the validation prefab is presentation-only and existing gameplay prefabs are unchanged.
