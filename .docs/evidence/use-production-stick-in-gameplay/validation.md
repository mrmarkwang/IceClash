# Production Stick Gameplay Validation

## Result

PASS. `PrototypeArena` now renders the production `Hockey_Stick_Base_v1` on all skaters. The orange low-poly visual is no longer a gameplay-prefab dependency.

## Unity evidence

- Final marker/material/prefab validator (`/tmp/iceclash-hockey-carry-postcapture-assets.log`) exited 0 and logged `MODULAR_CHARACTER_ASSETS_VALID`.
- Final dedicated two-hand animation-rig smoke (`/tmp/iceclash-hockey-carry-postcapture-modular-verified.log`) exited 0 and logged `MODULAR_CHARACTER_SMOKE_PASS players=10`; both evaluated hand bones reached their production grip targets within the 0.10 m tolerance.
- Final arena smoke process (`/tmp/iceclash-hockey-carry-postcapture-arena-verified.log`) exited 0 against the captured pose and logged `PHASE1_PVE_SMOKE_PASS` with `twoHandIK=true`.
- Supporting smoke markers passed for WASD, joystick, camera collider, Idle/Running animation, foot alignment, and ten skaters.
- Gameplay screenshot: `prototype-arena-production-stick.png` (1278 × 719).
- Post-capture freshness validation (`/tmp/iceclash-hockey-carry-final-evidence.log`) logged `PRODUCTION_STICK_GAMEPLAY_EVIDENCE_VALID`; the final capture timestamp (10:16:16) is newer than the final generated prefab and test scene (10:14:28).

## Asset and dependency evidence

- Gameplay source: `Assets/_Project/Prefabs/HockeyPlayer.prefab`.
- Runtime source: connected `Assets/_Project/Prefabs/Resources/Skater.prefab` variant.
- Production nested-prefab GUID: `327db6e4355264971aa08bd163bc5a5f`.
- The production PBR Standard material, albedo, normal, metallic/smoothness maps, rigid/no-animation importer, non-readable mesh, 4,347 triangles, marker reach, and renderer policy all pass generator validation.
- Neither gameplay prefab depends on legacy FBX GUID `75e1a863d03e745c4866ba7df65b33b5` or legacy material GUID `a1e957c02ceb9411e873b4695058968f`.
- Existing `Stick`, `Stick Shaft`, `Stick Grip`, and `Stick Blade` marker names remain in the canonical gameplay prefab.
- `BladeContact` remains exactly aligned to the fixed gameplay blade/control point. The torso-side right-hand target is exactly on `PrimaryGrip`, the lower forward left-hand target is exactly on `SecondaryGrip`, and validation checks both hand contacts, rendered bounds, and minimum lateral/vertical/forward diagonal separation.

## Visual assessment

The production stick is visibly the dark/black detailed model in the gameplay capture and follows active skaters without detachment or reversed orientation. The higher torso-side top hand and lower forward second hand create a clear across-body diagonal toward the puck, with the blade on the ice. Its physically based dark finish has lower contrast than the former orange placeholder at the distant gameplay camera, but the shaft and blade silhouettes remain readable against the ice.

## Protected hashes

Unchanged from the baseline:

- `Resources/Skater.prefab`: `d7d4d973a96d5247a2c273a5bdb80e8a0321bc7e7c0b4cb33b93483a87a7abcb`
- `PrototypeArena.unity`: `2dab5017419a2ee28505c5594da8f22a492895f3ae30dd8afc70a6a0c52ddc1a`
- `StickPuckInteraction.cs`: `65e4e543ae183ebae95a1658805591fc13e729e096a1e0b849ab58fae0c4001f`
- `HockeyStickRig.cs`: `91b44f96c59835e8cd39ace8102eb99896171a56d08ffd70981cf4eea6d4287d`
- `PlayerController.cs`: `43cf9bb31b75a803bc6cde35ae3170217554dcd3716f5de6eedb9527bcc94f4c`
- `HockeyCameraController.cs`: `9f58283f99fdad629c4ff67cd5dec8a20c82f6c6f57b407b80bd360898fd2360`
- `LocalPlayerInput.cs`: `860052b81ce61a1f0ea8cbdf769dfbae8b3eceebc79a1f80477f9f808db1174d`
- `PlayerInputController.cs`: `27f642c6335598a9ef907af60167b5610fc5f071557191a43a85899af99f0fc2`
- `LocalMatchSetup.cs`: `038d945052242fea993a2723def1cd6a9a33e3f21d92b5a71e1371611f55b528`
- `MatchController.cs`: `039acb58a3f5c8fcc173ce1bbb408cd6d1a78906e68b0e74d44f6eb096fafb9e`

Expected changed outputs:

- `HockeyCharacterAssetSetup.cs`: `d9dfb46116840acee16df36cb06c1e9aed10a546fcc2e1ca2168a9ca319d4e9e`
- `HockeyPlayer.prefab`: `b8fe8833c254ca9ae045d24d0f089cdabf151ab81142831a55d24e55b7b79ee0`
