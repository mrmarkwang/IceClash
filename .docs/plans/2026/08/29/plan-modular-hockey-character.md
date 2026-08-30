# Modular Hockey Character Plan

## Goal

Make the existing 5v5 roster render and animate a reusable humanoid hockey player with independently replaceable equipment and two-hand stick IK while preserving every gameplay controller and puck contract.

## Current Context

- `Assets/_Project/Art/Characters/RealisticHumanMale/unity.Fbx` is the selected character, but its importer currently serializes `animationType: 2` (Generic) and has no Humanoid mapping.
- `Assets/_Project/Prefabs/Resources/Skater.prefab` is a primitive capsule loaded through `Resources.Load<GameObject>("Skater")`.
- `LocalMatchSetup` instantiates the resource prefab before adding/configuring `PlayerController`; it creates ten skaters and two controller-less goalies from the same prefab.
- `SkaterPrefabSetup.Create` can overwrite the resource prefab with a primitive and is a competing legacy generator.
- `PlayerController` must remain unmodified. The visual layer can observe its public state after explicit late binding.
- The puck contract is `StickPuckInteraction.ControlPoint`; the visual blade can follow it without replacing puck logic.
- Animation Rigging 1.4.1 is resolved for Unity 6000.5.9f1.
- The selected character includes many high-resolution textures. All textures under `RealisticHumanMale` are covered by the mobile policy.

## Decisions

- `Assets/_Project/Prefabs/HockeyPlayer.prefab` is the only authored hierarchy. `Resources/Skater.prefab` is a connected Unity prefab variant so the existing load contract stays intact without duplicate hierarchy ownership.
- `HockeyCharacterAssetSetup` configures the FBX as Humanoid and fails if its avatar is not both human and valid.
- Add presentation-only `HockeyEquipmentLoadout`, `HockeyCharacterPresentation`, and `HockeyStickRig` components. They do not own movement, shooting, possession, or puck physics.
- `HockeyCharacterPresentation.Bind(PlayerController)` is called by `LocalMatchSetup` after controller configuration. Before binding, and for `Bind(null)`, presentation remains safely idle for goalies and scene previews.
- Six stable slot anchors own replaceable Helmet, Jersey, Gloves, Pants, Skates, and Stick items. Equip/clear changes one slot atomically.
- Stable hand targets and hints live under non-replaceable `StickRigTargets`, aligned with but never parented under the equipped Stick. Clearing Stick zeroes both IK weights; equipping any replacement restores them without invalidating references.
- Placeholder Humanoid muscle clips provide idle, skating, and shooting presentation. Root motion stays disabled; runtime validation requires observable leg/torso bone motion.
- `ModularCharacterTestHarness` drives deterministic preview states and equipment operations on exactly ten prefab instances. It adds/configures a `PlayerController` on the first of those existing ten against a real scene `PuckController`, then verifies claim, carried follow, and release; it never creates an additional skater. `PrototypeArena` remains the full gameplay regression.
- The same resource prefab intentionally yields twelve humanoids in gameplay: ten bound skaters and two controller-less idle goalies. Goalie behavior is otherwise unchanged.
- Every texture under `RealisticHumanMale` is non-readable and mipmapped with explicit Android/iOS overrides at maximum size 1024 using ASTC 6x6. Character renderers share materials and disable motion vectors and reflection probes.
- `SkaterPrefabSetup.Create` delegates to the modular generator; no primitive writer remains.
- Prerequisite baseline correction: the existing rounded-corner puck-containment smoke oracle used a fixed diagonal normal for every corner sample, which falsely rejected tangent positions away from that diagonal. Use the sample's radial local-corner normal instead; this corrects the test geometry without changing puck behavior or weakening the containment threshold.
- Do not add feature flags, fallback visual prefabs, root-motion movement, inventory persistence, or duplicate puck/controller paths.

## Phased Tasks

### Phase 1 - Import and runtime contracts

- [x] Add `Assets/_Project/Tests/Editor/HockeyCharacterAssetSetup.cs` with idempotent `GenerateAll`, batch generation/validation, Humanoid import configuration, avatar validation, and mobile texture configuration.
- [x] Add `Assets/_Project/Scripts/Hockey/Character/HockeyEquipmentLoadout.cs` with the six-slot enum, serialized anchors/items, atomic equip/clear, Jersey tint, and independence validation.
- [x] Add `Assets/_Project/Scripts/Hockey/Character/HockeyCharacterPresentation.cs` with explicit late `Bind`, safe null/controller-less idle, Animator `Speed`/`Shoot` driving, and deterministic preview state.
- [x] Add `Assets/_Project/Scripts/Hockey/Character/HockeyStickRig.cs` with serialized constraints/targets/hints and equipment-aware atomic weight changes.
- [x] Update `Assets/_Project/Tests/Editor/SkaterPrefabSetup.cs` so `Create` delegates to `HockeyCharacterAssetSetup.GenerateAll`; verify no primitive capsule writer remains.

### Phase 2 - Generated assets and prefab composition

- [x] Extend `HockeyCharacterAssetSetup` to create shared materials plus `Idle.anim`, `Skate.anim`, `Shoot.anim`, and `HockeyPlayer.controller` under `Assets/_Project/Art/HockeyPrototype`.
- [x] Generate `Assets/_Project/Prefabs/HockeyPlayer.prefab` as the sole authored hierarchy with selected humanoid, Animator, CharacterController, six anchors/items, presentation components, and primitive placeholder equipment.
- [x] Configure one `RigBuilder`, one `Rig`, and independent left/right `TwoBoneIKConstraint` components using human arm bones and stable `StickRigTargets` targets/hints.
- [x] Generate `Assets/_Project/Prefabs/Resources/Skater.prefab` as a connected Unity prefab variant and validate its corresponding-source/base-prefab relationship.
- [x] Verify clearing/replacing every slot preserves the other five references; Stick clear/replacement also preserves IK references and toggles both weights.
- [x] In `HockeyCharacterAssetSetup.ValidateEditorEquipmentPersistence`, load prefab contents, change each slot through the API, save/reload a temporary prefab, verify changed-slot serialization plus other-slot identity and Stick IK references, then delete only that temporary validation prefab.
- [x] Configure shared material references, disabled motion vectors, and disabled reflection probes on the selected model and equipment renderers.

### Phase 3 - Gameplay and deterministic scene integration

- [x] Update `LocalMatchSetup` to tint `HockeyEquipmentLoadout` Jersey and call `HockeyCharacterPresentation.Bind(controller)` after each skater is configured; explicitly keep goalies in controller-less idle presentation.
- [x] Add `Assets/_Project/Scripts/Hockey/Character/ModularCharacterTestHarness.cs` to validate exactly ten characters, deterministically exercise preview animation/equipment independence, configure the first existing character with `PlayerController` plus the real puck, and verify claim/carried follow/release.
- [x] Generate `Assets/_Project/Scenes/ModularCharacterTest.unity` with exactly ten active HockeyPlayer instances, one Rigidbody/collider/`PuckController` puck, and one test harness; do not create an additional gameplay skater.
- [x] Extend `PrototypeArenaSmokeCheck` with modular structure, twelve-humanoid/two-idle-goalie, bound presentation, two-hand IK, animation, and visual-blade/control-point proximity checks without weakening existing assertions.
- [x] Add `Assets/_Project/Tests/Editor/ModularCharacterSmokeRunner.cs` with menu `Run` and batch-safe `RunBatch` entry points that require `MODULAR_CHARACTER_SMOKE_PASS players=10` and return a nonzero process code on failure.

### Phase 4 - Mobile policy and verification

- [x] Apply non-readable, mipmapped, Android/iOS 1024 ASTC 6x6 settings to every texture under `RealisticHumanMale`; record the covered count.
- [x] Run `HockeyCharacterAssetSetup.GenerateAndValidateBatch`; require exit code 0, `MODULAR_CHARACTER_ASSETS_PASS`, a valid Humanoid avatar, valid connected prefabs/IK/equipment/animation, passing Edit Mode save/reload equipment persistence, and no compile errors.
- [x] Run `ModularCharacterSmokeRunner.RunBatch`; require exit code 0 and `MODULAR_CHARACTER_SMOKE_PASS players=10`.
- [x] Run `Phase3SmokeRunner.Run`; require exit code 0 and the existing gameplay pass marker with new modular assertions for ten skaters, two goalies, and puck behavior.
- [x] Verify `git diff -- Assets/_Project/Scripts/Player/PlayerController.cs` is empty and inspect the scoped diff for duplicate gameplay components, material instances, changed 5v5 counts, legacy primitive generation, or unrelated churn.

## Validation

- Close the interactive editor before batch verification, then run `/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/markwang/mw/IceClash -executeMethod IceClash.Tests.Editor.HockeyCharacterAssetSetup.GenerateAndValidateBatch -logFile /tmp/iceclash-modular-assets.log -quit`; require exit code 0 and `MODULAR_CHARACTER_ASSETS_PASS`.
- Inspect `/tmp/iceclash-modular-assets.log` for no `error CS`, `Compilation failed`, unhandled exception, package-resolution error, or invalid-avatar message.
- Run `/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/markwang/mw/IceClash -executeMethod IceClash.Tests.Editor.ModularCharacterSmokeRunner.RunBatch -logFile /tmp/iceclash-modular-smoke.log`; require exit code 0 and `MODULAR_CHARACTER_SMOKE_PASS players=10`.
- Run `/Applications/Unity/Hub/Editor/6000.5.9f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/markwang/mw/IceClash -executeMethod IceClash.Tests.Editor.Phase3SmokeRunner.Run -logFile /tmp/iceclash-prototype-smoke.log`; require exit code 0 and the existing Phase 1 smoke pass marker plus new modular assertions.
- Execute `.docs/tests/test-modular-hockey-character.md` scenarios in the two scenes.
- Require one Animator, one CharacterController, six named slots, two constraints, one RigBuilder, no root motion, no baked gameplay controllers, and a connected resource prefab variant.
- Require shared material identity and policy compliance on all covered textures/renderers for ten test and twelve gameplay humanoids.

## Rollback / Risk

- Auto-mapping may not yield a valid Humanoid avatar. Generation stops precisely; rollback restores only importer metadata.
- Presenters initialize before runtime-added components. Only explicit post-configuration binding drives skaters; controller-less goalies remain valid and idle.
- Replaceable Stick content must never own targets/hints or replacement would break serialized constraints.
- The realistic character appears twelve times in gameplay. Mobile import settings and shared assets mitigate cost; rollback restores the previous resource prefab without gameplay changes.
- Team tinting must target Jersey only, avoiding skin and other equipment.
- The visual blade must stay near `StickPuckInteraction.ControlPoint`; both test-scene claim/carry/release and gameplay smoke validation guard against presentation/gameplay divergence.
- Roll back the complete scoped change with a Git revert/restore of `Assets/_Project/Scripts/Hockey/Character`, `Assets/_Project/Tests/Editor/HockeyCharacterAssetSetup.cs`, `Assets/_Project/Tests/Editor/ModularCharacterSmokeRunner.cs`, `Assets/_Project/Tests/Editor/SkaterPrefabSetup.cs`, `Assets/_Project/Scripts/Match/LocalMatchSetup.cs`, `Assets/_Project/Scripts/Hockey/PrototypeArenaSmokeCheck.cs`, `Assets/_Project/Prefabs/HockeyPlayer.prefab`, `Assets/_Project/Prefabs/Resources/Skater.prefab`, `Assets/_Project/Art/HockeyPrototype`, `Assets/_Project/Scenes/ModularCharacterTest.unity`, and importer metadata below `RealisticHumanMale`; do not leave generated assets or mobile overrides behind.
