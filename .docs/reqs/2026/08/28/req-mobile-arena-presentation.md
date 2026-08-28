# Mobile Arena Presentation

## Problem

The runtime-built prototype rink currently reads as a short cyan tabletop with tall dark walls, a flat goal grid, and oversized capsule actors. In landscape mobile framing, the camera exposes nearly the whole sheet at once, so the rink, nets, skaters, and goalies do not have the proportions or close broadcast-style readability shown by the supplied hockey references.

## Requirement

Redesign the generated arena presentation for landscape mobile play so the rink has believable hockey proportions and markings, the boards and glass read as rink construction, goals have visible three-dimensional frames and netting, and the follow camera composes a closer action view. Scale skaters down relative to the rink and make goalies similarly tall but visibly broader than skaters, while preserving the existing 3v3 gameplay, controls, scoring, AI, and runtime scene composition.

## Acceptance Criteria

- [x] The generated ice surface is substantially longer than it is wide, retains rounded corners, and uses a near-white ice treatment with regulation-inspired center, blue, goal, faceoff, and crease markings.
- [x] Rink boundaries read as low light-colored boards with a contrasting lower kickplate and a translucent upper glass layer, while continuing to contain gameplay actors and the puck.
- [x] Each goal has a clearly visible red frame with front posts, crossbar, rear depth supports, and net strands covering its back, roof, and sides.
- [x] The landscape follow camera keeps the controlled action large enough to read on a phone and normally shows a cropped zone/half-rink composition instead of the entire rink.
- [x] Skaters are smaller relative to the new rink, and goalies remain similar in height while using a broader visual footprint than skaters.
- [x] Goal triggers, goalie anchors, defensive formation anchors, and rink geometry remain aligned after the proportion changes, and front-side scoring still works in both directions.
- [x] Existing 3v3-plus-goalies composition, one-human control route, mobile controls, match flow, and gameplay smoke validation remain functional.

## Constraints

- Preserve the intentionally empty `PrototypeArena` scene and its runtime bootstrap pattern.
- Preserve existing uncommitted mobile-control, shooting, goalie-difficulty, goal-width, and smoke-check work.
- Use project-owned runtime geometry and materials; do not copy branding, logos, characters, or textures from the supplied screenshots.
- Add no third-party packages, external services, alternate scenes, feature flags, or device-specific layout forks.
- Keep changes focused on presentation geometry, aligned arena anchors, camera framing, and actor scale.

## Non-Goals

- Production character models, animation, crowds, benches, arena branding, advertisements, audio, or licensed artwork.
- New hockey rules, team counts, controls, puck mechanics, scoring behavior, AI states, multiplayer, backend, or progression systems.
- Photorealistic mesh assets or a full lighting/post-processing pipeline.
