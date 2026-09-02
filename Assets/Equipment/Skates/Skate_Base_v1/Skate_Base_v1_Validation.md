# Skate Base v1 Validation

## Source

- Original archive: `Meshy_AI_Single_professional_i_0902012255_texture_fbx.zip` (left unchanged in Downloads)
- ZIP SHA-256 recorded before extraction: `ca529fa337583c06b37a30f07db63337d7094999e4a7cd443eec10f9733b6010`
- Source filename: `Meshy_AI_Single_professional_i_0902012255_texture.fbx`
- Source/copy FBX SHA-256: `d3b96e887ce1cc811cd1019bf5ef7e5979d5f8bf6c62551c35f7814f8e470c6b` (archive member and `Source/` copy match)
- Supplied maps: albedo, metallic, normal, roughness
- Imported hierarchy: one root; one rigid MeshFilter; no child hierarchy, rig, Animator, or skinned mesh

## Skate

- Source vertices: 4253; source triangles: 4136
- Source finding: one unusually wide/tall complete Meshy skate with fragmented AI topology; unmodified source evidence was captured before normalization
- Canonical mesh: 4253 vertices, 4136 triangles; all source faces retained
- Source imported bounds: (0.978515, 1.000000, 0.366211) m; center (-0.000976, 0.000000, 0.000489) m
- Source local bounds: (0.009785, 0.003662, 0.010000); center (-0.000010, -0.000005, 0.000000)
- Production dimensions: (0.134581, 0.316807, 0.310000) m (lateral × up × forward)
- Materials: 1 imported slot; 1 production material (`Skate_Base_v1.mat`)
- PBR: albedo and normal assigned directly; metallic RGB plus inverted roughness alpha combined non-destructively; double-sided rendering preserves fragmented mixed-winding AI surfaces without remodeling
- Imported transform: rotation (270.000000, 0.000000, 0.000000)°, scale (100.000000, 100.000000, 100.000000)
- Source rendered axes: toe/forward `-X`, up `+Y`, lateral `±Z`; production axes: forward/toe `+Z`, up `+Y`, lateral `+X`
- Production root and `Visual`: local position `(0,0,0)`, rotation `(0,0,0)`, scale `(1,1,1)`
- Handedness: `Skate_R` is an offline reflected derivative with reversed winding/tangent handedness; runtime scales remain positive

## Fitting

- `Skate_L` local position: (0.000000, -0.213328, 0.070000); rotation (0.000000, 0.000000, 0.000000)°; scale (1.000000, 1.000000, 1.000000)
- `Skate_R` local position: (0.000000, -0.217290, 0.070000); rotation (0.000000, 0.000000, 0.000000)°; scale (1.000000, 1.000000, 1.000000)
- Hierarchy: `LeftFoot/LeftSkateSocket/Skate_L` and `RightFoot/RightSkateSocket/Skate_R`
- Bone resolution: `Animator.GetBoneTransform(HumanBodyBones.LeftFoot/RightFoot)`; no hard-coded skeleton path

## Contact

- Left BladeContact world position: (-0.164210, 0.000000, 0.021998) m
- Right BladeContact world position: (0.160793, 0.000000, 0.019933) m
- Ice plane Y: 0.000000 m; contact Y difference: 0.000000 m

## Humanoid

- `Avatar.isValid = true`
- `Avatar.isHuman = true`
- Foot-bone local transforms match a fresh clean-prefab instance after sockets are added
- Protected hashes match the pre-generation baseline:
- `Assets/Characters/Male/Male_Base_v1/Meshy_AI_Navy_Training_Pose_biped_Character_output.fbx`: `5427221743566b2db9c893355373c14236853cac0b0105fd1e391ebee88acfdd`
- `Assets/Characters/Male/Male_Base_v1/Meshy_AI_Navy_Training_Pose_biped_Character_output.fbx.meta`: `ef71766943a821b14f481a010eec4040094935e0611fa4c27152a349e4713046`
- `Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean.fbx`: `a5682777ea0f7b14236fb96a5c3f19826b79113997bb929772b2d92cc892a159`
- `Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean.fbx.meta`: `602cef15f63e2edfad14ee120e8c302ffa38cd729058d95216ec1d2d07cd1d3f`
- `Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean_Test.prefab`: `ea32b20a09953006094f4fb7332e4e9fcf205e94530f3d18e4566c6d95fafc17`
- `Assets/Characters/Male/Male_Base_v1/Male_Base_v1_1_Clean_Test.unity`: `eb42ad7c6fa3e18f7a8c6b1416d715811c495992cf34ba6c1197fcd5433528e6`
- `Assets/Characters/Male/Male_Base_v1/Meshy_AI_Navy_Training_Pose_biped_Animation_Running_withSkin.fbx`: `17e8584b747b909b7ee0a4731c8f2024e80cdc04731c69c3455a58cbee19678b`
- `Assets/Characters/Male/Male_Base_v1/Meshy_AI_Navy_Training_Pose_biped_Animation_Running_withSkin.fbx.meta`: `4556ef0553e5467b1ea3c4467afba4a9add46c04a1ccc42550369894e3effebe`

## Animation

- Stress-test clip: `Armature|Armature|running|baselayer` (unchanged running animation; not a final skating animation)
- Sampled cycles: 2.25; samples: 19; minimum forward alignment: 0.7321
- Toe-relative travel size: left (0.000000, 0.000000, 0.000000) m; right (0.000000, 0.000001, 0.000000) m; asserted tolerance 0.000010 m
- Both skates retained correct socket parent, positive unit scale, finite transforms, forward orientation, and invariant toe-relative placement

## Visual Validation

- Neutral: [front](../../../../.docs/evidence/skate-base-v1/neutral-front.png), [rear](../../../../.docs/evidence/skate-base-v1/neutral-rear.png), [left](../../../../.docs/evidence/skate-base-v1/neutral-left.png), [right](../../../../.docs/evidence/skate-base-v1/neutral-right.png)
- Close-ups: [left](../../../../.docs/evidence/skate-base-v1/neutral-left-close.png), [right](../../../../.docs/evidence/skate-base-v1/neutral-right-close.png)
- Running: [front](../../../../.docs/evidence/skate-base-v1/running-front.png), [side](../../../../.docs/evidence/skate-base-v1/running-side.png), [rear](../../../../.docs/evidence/skate-base-v1/running-rear.png)
- Running phase 0.125: [front](../../../../.docs/evidence/skate-base-v1/running-front-125.png), [side](../../../../.docs/evidence/skate-base-v1/running-side-125.png), [rear](../../../../.docs/evidence/skate-base-v1/running-rear-125.png)
- Running phase 0.375: [front](../../../../.docs/evidence/skate-base-v1/running-front-375.png), [side](../../../../.docs/evidence/skate-base-v1/running-side-375.png), [rear](../../../../.docs/evidence/skate-base-v1/running-rear-375.png)
- Running phase 0.625: [front](../../../../.docs/evidence/skate-base-v1/running-front-625.png), [side](../../../../.docs/evidence/skate-base-v1/running-side-625.png), [rear](../../../../.docs/evidence/skate-base-v1/running-rear-625.png)
- Running phase 0.875: [front](../../../../.docs/evidence/skate-base-v1/running-front-875.png), [side](../../../../.docs/evidence/skate-base-v1/running-side-875.png), [rear](../../../../.docs/evidence/skate-base-v1/running-rear-875.png)
- [Low gameplay-style view](../../../../.docs/evidence/skate-base-v1/gameplay-low.png)
- Unmodified source: [front](../../../../.docs/evidence/skate-base-v1/source-front.png), [side](../../../../.docs/evidence/skate-base-v1/source-side.png), [top](../../../../.docs/evidence/skate-base-v1/source-top.png), [isometric](../../../../.docs/evidence/skate-base-v1/source-iso.png)
- Visual review result: both neutral close-ups contain the active sock foot inside the boot at ankle, heel, and toe; no active-foot sock surface exits the boot
- Visual review result: left/right holders and blades are symmetric, toes face character-forward, and both `BladeContact` points meet the same ice plane
- Visual review result: four distinct running phases show both rigid skates following their respective feet without detachment or severe sock penetration; the full 2.25-cycle transform and toe-relative assertions provide the temporal check

## Created / Modified File Inventory

- `.docs/evidence/skate-base-v1/gameplay-low.png`
- `.docs/evidence/skate-base-v1/neutral-front.png`
- `.docs/evidence/skate-base-v1/neutral-left-close.png`
- `.docs/evidence/skate-base-v1/neutral-left.png`
- `.docs/evidence/skate-base-v1/neutral-rear.png`
- `.docs/evidence/skate-base-v1/neutral-right-close.png`
- `.docs/evidence/skate-base-v1/neutral-right.png`
- `.docs/evidence/skate-base-v1/running-front-125.png`
- `.docs/evidence/skate-base-v1/running-front-375.png`
- `.docs/evidence/skate-base-v1/running-front-625.png`
- `.docs/evidence/skate-base-v1/running-front-875.png`
- `.docs/evidence/skate-base-v1/running-front.png`
- `.docs/evidence/skate-base-v1/running-rear-125.png`
- `.docs/evidence/skate-base-v1/running-rear-375.png`
- `.docs/evidence/skate-base-v1/running-rear-625.png`
- `.docs/evidence/skate-base-v1/running-rear-875.png`
- `.docs/evidence/skate-base-v1/running-rear.png`
- `.docs/evidence/skate-base-v1/running-side-125.png`
- `.docs/evidence/skate-base-v1/running-side-375.png`
- `.docs/evidence/skate-base-v1/running-side-625.png`
- `.docs/evidence/skate-base-v1/running-side-875.png`
- `.docs/evidence/skate-base-v1/running-side.png`
- `.docs/evidence/skate-base-v1/source-front.png`
- `.docs/evidence/skate-base-v1/source-iso.png`
- `.docs/evidence/skate-base-v1/source-side.png`
- `.docs/evidence/skate-base-v1/source-top.png`
- `.docs/plans/2026/09/01/plan-skate-base-v1-integration.md`
- `.docs/reqs/2026/09/01/req-skate-base-v1-integration.md`
- `.docs/tests/test-skate-base-v1-integration.md`
- `Assets/Equipment/Skates.meta`
- `Assets/Equipment/Skates/Skate_Base_v1.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Editor.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Editor/SkateBaseV1Setup.cs`
- `Assets/Equipment/Skates/Skate_Base_v1/Editor/SkateBaseV1Setup.cs.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Materials.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Materials/Skate_Base_v1.mat`
- `Assets/Equipment/Skates/Skate_Base_v1/Materials/Skate_Base_v1.mat.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Materials/Skate_Base_v1_Ice.mat`
- `Assets/Equipment/Skates/Skate_Base_v1/Materials/Skate_Base_v1_Ice.mat.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Materials/Skate_Base_v1_MetallicSmoothness.png`
- `Assets/Equipment/Skates/Skate_Base_v1/Materials/Skate_Base_v1_MetallicSmoothness.png.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Prefabs.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Prefabs/Skate_Base_v1.prefab`
- `Assets/Equipment/Skates/Skate_Base_v1/Prefabs/Skate_Base_v1.prefab.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Prefabs/Skate_Base_v1_Canonical.asset`
- `Assets/Equipment/Skates/Skate_Base_v1/Prefabs/Skate_Base_v1_Canonical.asset.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Prefabs/Skate_Base_v1_Mirrored.asset`
- `Assets/Equipment/Skates/Skate_Base_v1/Prefabs/Skate_Base_v1_Mirrored.asset.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Prefabs/Skate_L_v1.prefab`
- `Assets/Equipment/Skates/Skate_Base_v1/Prefabs/Skate_L_v1.prefab.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Prefabs/Skate_R_v1.prefab`
- `Assets/Equipment/Skates/Skate_Base_v1/Prefabs/Skate_R_v1.prefab.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Skate_Base_v1_Validation.md`
- `Assets/Equipment/Skates/Skate_Base_v1/Skate_Base_v1_Validation.md.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Source.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Source/Meshy_AI_Single_professional_i_0902012255_texture.fbx`
- `Assets/Equipment/Skates/Skate_Base_v1/Source/Meshy_AI_Single_professional_i_0902012255_texture.fbx.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Source/Meshy_AI_Single_professional_i_0902012255_texture.png`
- `Assets/Equipment/Skates/Skate_Base_v1/Source/Meshy_AI_Single_professional_i_0902012255_texture.png.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Source/Meshy_AI_Single_professional_i_0902012255_texture_metallic.png`
- `Assets/Equipment/Skates/Skate_Base_v1/Source/Meshy_AI_Single_professional_i_0902012255_texture_metallic.png.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Source/Meshy_AI_Single_professional_i_0902012255_texture_normal.png`
- `Assets/Equipment/Skates/Skate_Base_v1/Source/Meshy_AI_Single_professional_i_0902012255_texture_normal.png.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Source/Meshy_AI_Single_professional_i_0902012255_texture_roughness.png`
- `Assets/Equipment/Skates/Skate_Base_v1/Source/Meshy_AI_Single_professional_i_0902012255_texture_roughness.png.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Tests.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Tests/Male_Base_v1_Skate_Fitting.prefab`
- `Assets/Equipment/Skates/Skate_Base_v1/Tests/Male_Base_v1_Skate_Fitting.prefab.meta`
- `Assets/Equipment/Skates/Skate_Base_v1/Tests/Skate_Base_v1_Fitting.unity`
- `Assets/Equipment/Skates/Skate_Base_v1/Tests/Skate_Base_v1_Fitting.unity.meta`

## Regression / Limitations

- The generator writes only within the new skate asset and `.docs/evidence/skate-base-v1`; it never writes humanoid, animation, gameplay/controller/camera/input/puck/stick, or existing gameplay-prefab paths
- Minor hidden AI topology fragmentation and broad proportions remain; no remodeling, collider, gameplay, VFX, IK, skeleton, skin, animation-source, camera, input, puck, or stick change was made
