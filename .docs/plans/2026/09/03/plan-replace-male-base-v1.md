# Replace Male Base v1 Plan

## Goal

Replace the old `Male_Base_v1` Unity asset folder with the already-validated integrated-skates set while retaining rollback data and avoiding duplicate GUIDs or out-of-scope consumer edits.

## Current Context

- Old v1 contains three FBXs, validation assets, and editor tooling with GUIDs referenced by equipment utilities and historical tests.
- Production gameplay currently uses a separate `Male_Base_v1_1_Clean_Visual` prefab, which ultimately references an FBX located in the old v1 folder.
- The integrated-skates folder is additive and internally consistent; its prefab, scene, controller, model, textures, and materials reference one another by their v2-generated GUIDs.
- A complete backup is stored under the task workspace, outside Unity's `Assets` tree.
- `ProjectSettings/ProjectSettings.asset` was already modified before this work and is unrelated.

## Decisions

- Remove the old v1 folder and its folder `.meta`, then move the entire validated v2 folder and its folder `.meta` into the `Male_Base_v1` path. Moving instead of copying avoids duplicate GUIDs.
- Update only the isolated validation utility's root constant before the move.
- Keep integrated-skates asset filenames and object names intact; the requested replacement targets the folder, not a broad rename or gameplay migration.
- Do not preserve old asset metas inside the replacement because their subasset identities do not match the new FBXs and could create misleading or broken serialization.
- Preserve the editor compilation contract with a minimal replacement-folder compatibility class exposing the referenced paths and method signatures; legacy generation and alignment must throw actionable migration errors rather than mutate gameplay.
- Do not add feature flags, fallbacks, compatibility aliases, or consumer rewrites.
- E2E coverage is unnecessary for the filesystem replacement itself; Unity batch import/validation and Git scope checks provide end-to-end evidence.

## Phased Tasks

### Phase 1 - Backup and dependency evidence

- [x] Copy the complete old v1 folder and `Male_Base_v1.meta` into the task workspace and record a checksum manifest digest.
- [x] Enumerate old v1 GUIDs and identify external path/GUID consumers that may be affected.
- [x] Confirm the target v2 folder is the validated set and record its Unity validation marker.

### Phase 2 - Replacement preparation

- [x] Update `MaleBaseV2IntegratedSkatesValidationSetup.cs` so its root resolves `Assets/Characters/Male/Male_Base_v1` after the move.
- [x] Confirm no file outside the replacement set needs editing to complete the folder operation.
- [x] Add `MaleBaseV1ReplacementCompatibility.cs` inside the replacement Editor folder after the deleted legacy generator caused external missing-type compiler errors.

### Phase 3 - Destructive folder replacement

- [x] Remove the old `Assets/Characters/Male/Male_Base_v1` folder and its folder metadata after verifying the backup exists.
- [x] Move the integrated-skates folder and its metadata to `Assets/Characters/Male/Male_Base_v1` without copying duplicate GUIDs.
- [x] Confirm the old additive v2 path is absent and the replacement files are present under v1.

### Phase 4 - Unity and scope verification

- [x] Run the replacement validation utility in the already-open Unity Editor and require `MESHY_V2_VALIDATION_PASS`; batch launch was unavailable because that Editor held the project lock.
- [x] Verify the replacement Avatar remains valid and human and the test prefab/scene remain loadable.
- [x] Require script compilation to complete without `CS0103` missing legacy-generator errors and verify the compatibility methods cannot silently regenerate gameplay.
- [x] Inspect Git status/diff to ensure no gameplay, equipment, scene, or project-setting file was changed by the replacement.
- [x] Report known broken or retired external consumers without modifying them.

### Phase 5 - Status

- [x] Record the backup path, replacement result, Unity evidence, deleted legacy asset list, and known follow-up migration needs.
- [x] Mark acceptance criteria complete only where concrete replacement and validation evidence exists.

## Validation

- Run the matching Unity editor with `-batchmode -quit -executeMethod IceClash.CharacterValidation.Editor.MaleBaseV2IntegratedSkatesValidationSetup.GenerateAndValidateBatch` and require `MESHY_V2_VALIDATION_PASS`.
- Require `Assets/Characters/Male/Male_Base_v1` to exist and `Assets/Characters/Male/Male_Base_v2_IntegratedSkates` not to exist.
- Use `git status --short` and scoped `git diff --name-only` checks to confirm no new changes outside the replacement folder and RPD documentation/evidence.

## Rollback / Risk

- Rollback data is stored outside the Unity project under the task workspace; restoring it would require deleting the replacement v1 folder and copying the backup back.
- Deleting the old GUIDs intentionally breaks consumers that directly reference the retired clean FBX, validation prefabs, animations, or materials. Migrating those consumers is a separate task because the user requested the folder replacement specifically.
- The replacement keeps v2-generated GUIDs intact, so its own prefab, scene, controller, materials, and textures remain internally connected after the directory move.
