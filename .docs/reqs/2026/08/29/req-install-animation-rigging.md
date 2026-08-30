# Install Animation Rigging

## Problem

IceClash does not currently reference Unity's Animation Rigging package, so the planned two-hand hockey-stick constraints cannot use its supported rig graph and constraint components.

## Requirement

Install the official Animation Rigging package version compatible with the project's Unity 6000.5 editor while preserving all existing package dependencies and project behavior.

## Acceptance Criteria

- [x] The project manifest directly references the released Unity 6000-compatible Animation Rigging package.
- [x] Unity resolves the package into the package lock without removing or downgrading existing direct dependencies.
- [x] Unity imports the Animation Rigging runtime and editor assemblies without package-resolution or compilation errors.

## Constraints

- Preserve the existing Unity AI, Input System, and UI dependency changes already present in the worktree.
- Use the official Unity registry package rather than a Git URL or embedded package copy.
- Do not create hockey-stick constraints or modify scenes as part of package installation.

## Non-Goals

- Converting the character FBX to Humanoid.
- Building the two-hand hockey-stick IK rig.
- Adding compatibility wrappers, feature flags, or fallback IK implementations.
