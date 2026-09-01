# Two-Hand Hockey Pose Validation

## Right-Handed Grip Convention

- Right hand: authoritative top hand through `RightHand/StickSocket/Hockey_Stick_Base_v1`.
- Left hand: lower hand driven by `LeftHandIKTarget`, synchronized from the equipped `SecondaryGrip` after right-hand rig evaluation.

## Exact Transforms

- `StickSocket` local position: (0.033333, 0.000000, 0.000000)
- `StickSocket` local rotation: (284.058600, 90.229410, 216.575000) deg
- `StickSocket` local scale: (0.606061, 0.606061, 0.606061)
- `PrimaryGrip` local position: (0.133594, 0.640000, 0.000000)
- `PrimaryGrip` local rotation: (0.000000, 0.000000, 0.000000) deg
- `SecondaryGrip` local position: (0.133594, 0.340000, 0.000000)
- `SecondaryGrip` local rotation: (29.178990, 88.551080, 267.683000) deg
- `BladeContact` local position: (-0.080000, -0.790000, 0.000000)
- `BladeContact` local rotation: (0.000000, 0.000000, 0.000000) deg
- `BladeContact` world position in test pose: (0.179996, 0.411767, 1.691179)
- `LeftHandIKTarget` local position: (0.036936, 0.715461, 0.417548)
- `LeftHandIKTarget` local rotation: (84.508380, 138.866200, 221.136500) deg
- `LeftElbowHint` local position: (-0.540000, 0.820000, 0.300000)
- `LeftElbowHint` local rotation: (0.000000, 0.000000, 0.000000) deg

## Left-Hand IK Settings

- Constraint: `TwoBoneIKConstraint`
- Root / mid / tip: `LeftArm` / `LeftForeArm` / `LeftHand`
- Position weight: 1.000
- Rotation weight: 1.000
- Hint weight: 1.000
- Maintain position offset: False
- Maintain rotation offset: False
- Equipped source grip: `SecondaryGrip`

## Evidence

- [Front](front.png)
- [Side](side.png)
- [Rear](rear.png)
- [Both hands close-up](hands-close-up.png)
- [Blade close-up](blade-close-up.png)
- [Gameplay camera](gameplay-camera.png)
