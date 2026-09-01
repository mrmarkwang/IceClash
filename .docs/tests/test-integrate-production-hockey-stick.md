# E2E Visual Spec: Integrate Production Hockey Stick

## Preconditions

- Open or batch-generate `Assets/Equipment/Sticks/Hockey_Stick_Base_v1/Hockey_Stick_Base_v1_Test.unity`.
- Use the generated `Male_Base_v1_Stick_Test` instance and its right-hand-attached stick.

## Scenario 1 - Orthographic relationship checks

1. Render the player and stick from the front, side, and rear.
2. Confirm the stick is a believable adult length relative to the approximately 1.83 m player.
3. Confirm the shaft points along the documented local up direction, the blade faces the documented direction, and it does not point backward unexpectedly.
4. Confirm the stick follows the right hand through `StickSocket` and does not visibly detach.

Expected evidence: `front.png`, `side.png`, and `rear.png` show a consistent right-handed attachment, useful scale, and blade near the ground. Any neutral-pose clipping is reported.

## Scenario 2 - Main grip check

1. Frame the right hand and upper shaft closely.
2. Confirm the upper shaft passes through the intended grip area rather than missing the hand or crossing it excessively.
3. Confirm `PrimaryGrip` aligns with the main-hand attachment and `SecondaryGrip` lies lower on the shaft.

Expected evidence: `grip-close-up.png` clearly shows the hand/shaft relationship; two-hand IK quality is explicitly out of scope.

## Scenario 3 - Blade and material check

1. Frame the blade near the ground closely.
2. Confirm the blade is near, but not buried deeply beneath, the ground plane.
3. Confirm `BladeContact` lies near the practical lower puck-contact area rather than the whole-stick center.
4. Inspect the rendered model for missing/pink material, excessive metallic/smooth appearance, seams, bright lines, or unexpected transparency.

Expected evidence: `blade-close-up.png` shows blade height and surface appearance; all visible defects are listed in the validation report.
