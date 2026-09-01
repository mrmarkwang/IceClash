# Two-Handed Hockey Stick Pose

## Problem

`Male_Base_v1_1_Clean` currently appears attached to the production hockey stick rather than holding it. The right palm, left hand, wrist rotations, elbow bends, shaft angle, and blade placement do not form a believable right-handed hockey stance in Play Mode.

## Requirement

Create a reusable, non-destructive right-handed two-hand grip rig and dedicated static hockey-stick idle pose. The right hand must remain authoritative for the stick through `RightHand/StickSocket/Hockey_Stick_Base_v1`; the left arm must reach the stick's `SecondaryGrip` through Animation Rigging without reparenting the left hand.

## Acceptance Criteria

- [ ] The generated player hierarchy parents the production stick through `RightHand/StickSocket` and does not constrain the right hand to a target derived from the stick.
- [ ] `PrimaryGrip` aligns with the right palm, `SecondaryGrip` aligns with the left palm, both wrists wrap naturally around the shaft, and both elbows remain bent without inversion or shoulder collapse.
- [ ] The lower hand is approximately 0.30-0.45 m below the top hand along the shaft, and a dedicated outward/downward `LeftElbowHint` stabilizes the left-arm Two Bone IK.
- [ ] The static idle stance places the hands in front of the torso, angles the shaft diagonally downward, and keeps `BladeContact` near the ice, in front of the skates, slightly right of center, outside the legs.
- [ ] A dedicated static or idle hockey-stick validation pose is used instead of the Running pose as the final hand pose.
- [ ] Scene gizmos/debug visuals identify `PrimaryGrip`, `SecondaryGrip`, `BladeContact`, the left-hand IK target, and `LeftElbowHint` without shipping visible debug geometry in gameplay.
- [ ] The existing canonical humanoid skeleton and `Male_Base_v1` source assets remain unchanged.
- [ ] `PlayerController`, joystick/WASD movement, camera, puck, shoot/pass/deke logic, gameplay colliders, and gameplay IK blending remain unchanged.
- [ ] Front, side, gameplay-camera, and two-hand close-up screenshots demonstrate believable contact, natural elbows/wrists, stable shoulders, a torso-crossing shaft, near-ice blade placement, and no major clipping.
- [ ] Exact transforms, left-hand IK settings, and the created/modified asset list are recorded in the delivered validation report.

## Constraints

- Use the existing `RightHand`, `LeftHand`, `StickSocket`, `PrimaryGrip`, `SecondaryGrip`, and `BladeContact` contracts.
- Do not reparent `LeftHand` to the stick.
- Do not move the whole player to solve grip or blade alignment.
- Do not create a feedback loop between the stick and the right-hand alignment constraint.
- Preserve gameplay movement and gameplay control-point behavior.
- Preserve all existing `Male_Base_v1` source assets.

## Non-Goals

- Shooting, passing, deke, possession, or stickhandling animation.
- Runtime gameplay IK blending.
- Skeleton, mesh, source FBX, gameplay collider, camera, or movement changes.
- Reworking the production stick geometry.
