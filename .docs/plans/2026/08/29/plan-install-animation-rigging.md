# Install Animation Rigging Plan

## Goal

Make Unity's supported Animation Rigging APIs and components available to IceClash without disturbing the project's existing package graph.

## Current Context

- `ProjectSettings/ProjectVersion.txt` pins Unity 6000.5.9f1.
- `HEAD` contains the tracked Unity AI Assistant and Inference dependencies and is the clean preservation baseline. The transitional worktree manifest currently contains the unresolved, incompatible 1.4.0 entry from the failed compatibility attempt; `Packages/packages-lock.json` still matches `HEAD`.
- `Packages/packages-lock.json` is tracked and owned by Unity Package Manager.
- Unity's manual identifies `com.unity.animation.rigging` 1.4.0 as released for Unity 6000.0, while the official Unity registry now provides patch 1.4.1 for Unity 6000.0. The 1.4.1 changelog explicitly removes deprecated InstanceID API usage that causes 1.4.0 compilation failures in Unity 6000.5.9f1.
- An isolated Unity 6000.5.9f1 resolution confirmed 1.4.0 fails with CS0619 in `ConstraintsUtils.cs` and `RigUtils.cs`; successful resolution and assembly compilation of 1.4.1 in that exact editor is the compatibility gate.

## Decisions

- Add `com.unity.animation.rigging` version `1.4.1` as a direct registry dependency.
- Let the open Unity Editor resolve and update `Packages/packages-lock.json`; do not hand-edit the lock entry.
- Validate runtime/editor assembly definitions, compiled DLL presence, and error-free Editor compilation after a recorded log boundary.
- Preserve all existing dependencies exactly; do not introduce Git URLs, embedded copies, fallback IK code, or unrelated package updates.
- No E2E specification is needed because this is an internal package availability change with no user-facing flow.

## Phased Tasks

### Phase 1 - Scope and compatibility lock

- [x] Inspect `ProjectSettings/ProjectVersion.txt` and `Packages/manifest.json` to confirm the editor version and existing direct dependency state.
- [x] Verify from Unity's official manual and registry that Animation Rigging 1.4.1 targets Unity 6000.0 and removes the deprecated InstanceID API usage that fails under Unity 6000.5.9f1.
- [x] Reproduce the 1.4.0 incompatibility in an isolated Unity 6000.5.9f1 project and record the two CS0619 compiler failures that require the 1.4.1 patch.
- [x] Record that character conversion and hockey-stick constraint creation remain out of scope.
- [x] Record the pre-install byte offset of `Logs/Editor.log` before changing `Packages/manifest.json` (`8511387` bytes).

### Phase 2 - Dependency installation

- [x] Replace the incompatible `com.unity.animation.rigging` version `1.4.0` manifest entry with `1.4.1` while preserving every existing direct dependency.
- [x] Allow Unity Package Manager to resolve the package and add the corresponding `Packages/packages-lock.json` entry.
- [x] Confirm the package-file diff contains only the new direct dependency and Unity Package Manager's required lock changes, with no existing direct dependency removed or downgraded.

### Phase 3 - Verification

- [x] Confirm the resolved package contains runtime and editor `.asmdef` files and that `Library/ScriptAssemblies` contains their compiled DLLs.
- [x] Inspect only the `Logs/Editor.log` segment after the recorded pre-install byte offset for Animation Rigging package-resolution or C# compilation errors and a completed compilation/import cycle.
- [x] Record the resolved package version and stable worktree state as final evidence.

## Validation

- Verify `Packages/manifest.json` directly specifies `com.unity.animation.rigging` version `1.4.1`.
- Use `jq` on `Packages/packages-lock.json` to verify the package entry has `version == "1.4.1"`, `depth == 0`, `source == "registry"`, and `url == "https://packages.unity.com"`.
- Locate the Unity 6 hash-named `Library/PackageCache/com.unity.animation.rigging@*/package.json` and use `jq` to verify its embedded package version is exactly `1.4.1`; do not accept a stale 1.4.0 cache directory.
- Verify the exact 1.4.1 package cache contains runtime and editor `.asmdef` files.
- Run `find Library/ScriptAssemblies -maxdepth 1 -name '*Animation*Rigging*.dll'` and verify the package assemblies compiled.
- Record `wc -c < Logs/Editor.log` before editing the manifest and inspect bytes after that boundary for package-resolution/compilation completion and errors.
- Verify the manifest retains `com.unity.ai.assistant`, `com.unity.ai.inference`, `com.unity.inputsystem`, and `com.unity.ugui` unchanged.
- Verify `git diff -- Packages/manifest.json Packages/packages-lock.json` contains no unrelated dependency changes.

## Rollback / Risk

- Main risk: a package compatibility or dependency-resolution failure in Unity 6000.5. Version 1.4.0 is known incompatible and must not be used; roll back by removing only the new manifest entry and allowing Unity to regenerate the corresponding lock entry.
- The package files have a clean tracked baseline; any unrelated dependency change in the final diff is a blocker.
- The package adds assemblies and runtime components but does not affect scenes until constraints are explicitly created.
