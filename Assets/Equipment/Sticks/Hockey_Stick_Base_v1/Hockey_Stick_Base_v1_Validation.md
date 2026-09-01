# Hockey Stick Base v1 Validation

## Source

- Original archive: `Meshy_AI_Single_professional_i_0901053710_texture_fbx.zip` (left unchanged in Downloads)
- Original FBX filename inside archive: `Meshy_AI_Single_professional_i_0901053710_texture.fbx`
- Imported filename: `Meshy_Hockey_Stick_Base_v1.fbx`
- FBX SHA-256: `250f5eacfa4094b1ff0e1aaf1f6d57ac8bf428b1728d34bb6a42ab9ef63b9620` (source and imported copy match)
- Unity import path: `Assets/Equipment/Sticks/Hockey_Stick_Base_v1/Meshy_Hockey_Stick_Base_v1.fbx`

## Geometry

- Meshes: 1 rigid MeshFilter; no SkinnedMeshRenderer
- Vertices: 4616
- Triangles/polygons after Unity import: 4347
- Imported bounds: (0.189453, 1.000000, 0.013825) m; center (0.000977, 0.000000, -0.000504) m
- Final normalized bounds: (0.303125, 1.600000, 0.022120) m
- Final overall length: 1.600 m
- Approximate upper shaft cross-section: 0.034 m (blade-forward axis) × 0.019 m (face axis)
- Approximate blade envelope: 0.295 m toe-to-heel × 0.191 m maximum heel/vertical profile × 0.022 m maximum thickness

## Materials

- Imported FBX material slots: 1
- Final prefab materials: 1 (`Hockey_Stick_Base_v1.mat`, Standard shader)
- Supplied textures: albedo, metallic, normal, roughness — all present
- PBR status: albedo and normal assigned directly; metallic RGB combined non-destructively with inverted roughness in alpha as `Hockey_Stick_Base_v1_MetallicSmoothness.png`
- Missing/pink/unexpected transparency status: no missing references in the generated material; visual observations are recorded under Known Issues

## Transform

- Source/import conversion: root Euler (270.000000, 0.000000, 0.000000)°, root scale (100.000000, 100.000000, 100.000000) from FBX axis/unit conversion
- Source rendered orientation: overall length along +Y; blade is at −Y; blade toe points −X; blade faces point ±Z
- Source pivot/origin: (0,0,0), near bounds center (0.000977, 0.000000, -0.000504) rather than at a grip or blade contact
- Normalized prefab convention: +Y shaft/up, −X blade-forward/toe, +Z outward blade-face normal
- Non-destructive scale: `1.600` on the `Model` container; source FBX geometry unchanged
- Final overall length: 1.600 m relative to the 1.83 m target player

## Grip Setup

- `PrimaryGrip` local position: (0.133594, 0.640000, 0.000000)
- `SecondaryGrip` local position: (0.133594, 0.200000, 0.000000)
- `BladeContact` local position: (-0.080000, -0.790000, 0.000000)
- `StickSocket` local position: (0.000000, 0.000000, 0.000000)
- `StickSocket` local rotation: (352.028800, 273.608400, 84.391750)°
- `StickSocket` local scale: (1.000000, 1.000000, 1.000000)
- Validation-scene `BladeContact` world position: (0.431474, 0.018420, -0.019316) m

## Player Integration

- Exact Humanoid bone: `RightHand` (`HumanBodyBones.RightHand`); `LeftHand` remains untouched
- Hierarchy: `RightHand/StickSocket/Hockey_Stick_Base_v1`
- Main-hand alignment: `PrimaryGrip` is coincident with `RightHand`; a −2° world Z lean places the blade near the validation ground
- Existing source humanoid assets remained unchanged: yes; all four recorded SHA-256 baselines match after generation
- No two-hand IK, gameplay scripts, puck interaction, animation changes, or source skeleton edits were added

## Source Preservation Hashes (Before = After)

- `Male_Base_v1_1_Clean.fbx`: `a5682777ea0f7b14236fb96a5c3f19826b79113997bb929772b2d92cc892a159`
- `Male_Base_v1_1_Clean.fbx.meta`: `602cef15f63e2edfad14ee120e8c302ffa38cd729058d95216ec1d2d07cd1d3f`
- `Male_Base_v1_1_Clean_Test.prefab`: `ea32b20a09953006094f4fb7332e4e9fcf205e94530f3d18e4566c6d95fafc17`
- `Male_Base_v1_1_Clean_Test.unity`: `eb42ad7c6fa3e18f7a8c6b1416d715811c495992cf34ba6c1197fcd5433528e6`

## Visual Validation

- [Front](Evidence/front.png)
- [Side](Evidence/side.png)
- [Rear](Evidence/rear.png)
- [Right-hand grip close-up](Evidence/grip-close-up.png)
- [Blade/ground close-up](Evidence/blade-close-up.png)

## Known Issues

- The supplied blade is visibly oversized and blunt relative to a production stick. Its heel is unusually high/thick, its outline is lumpy, and the hook/curvature reads as exaggerated; this source geometry has not been hidden or remodeled.
- The neutral validation pose is the existing humanoid bind pose, not a two-handed hockey pose. The rigid shaft crosses the open palm at the intended grip point, but the fingers do not wrap it and some palm intersection remains; left-hand reach and hockey-motion quality cannot be judged until later IK/animation work.
- The dark PBR material renders without pink/missing surfaces, unexpected transparency, or excessive metallic glare. It is low-contrast, and faint irregular edge/seam noise is visible around the blade rim; no bright-line artifact was observed.
- No shaft distortion is visible. The blade's lower edge sits near the ice plane, while `BladeContact` marks the practical lower contact region rather than the stick center.
- The source pivot is centered in the asset rather than authored at a grip; named reference transforms and the socket offset provide the non-destructive attachment convention.
