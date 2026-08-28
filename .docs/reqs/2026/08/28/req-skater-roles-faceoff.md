# Skater Roles and Center-Ice Faceoff — Requirements

## Problem

The five skaters on each IceClash team are currently anonymous formation slots. Their reset locations spread them across their own half but do not communicate a conventional hockey lineup or place both teams into a recognizable center-ice faceoff formation.

## Requirement

Each team must field three forwards—Center, Left Wing, and Right Wing—and two defensemen—Left Defense and Right Defense—plus its existing goalie. At match start and after every goal, the skaters must reset into a mirrored center-ice faceoff formation: centers opposite each other at the center dot, wings outside the center circle, defensemen behind the forwards toward their own goal, and goalies at their crease anchors. All skaters must face their attacking direction.

## Acceptance Criteria

- [x] Each team has exactly one Center, one Left Wing, one Right Wing, one Left Defense, and one Right Defense, for three forwards and two defensemen.
- [x] Skater role is represented explicitly in the gameplay model and remains attached to the same player across control switches, possession changes, snapshots, and resets.
- [x] Blue and Red center-ice faceoff positions are mirrored across center ice, with centers nearest the puck without overlapping it, wings outside the center circle, and defensemen goal-side of all three forwards.
- [x] Match-start and post-goal faceoffs reset every skater to the role-appropriate position and attacking-direction rotation before play resumes.
- [x] Goalies continue to reset at their existing crease anchors, and the puck continues to reset on the center dot.
- [x] Automated smoke verification checks the complete role distribution and center-faceoff formation for both teams while preserving the five-skaters-plus-goalie roster and one-human-control invariant.
- [x] README and the local PvE E2E scenario describe the role distribution and center-ice faceoff layout.

## Constraints

- Preserve the current five-skater roster, goalie behavior, one-human-control route, count-driven team construction, match states, score flow, and runtime-built arena.
- Keep all faceoff positions inside the existing rink and clear of the center puck, opposing skaters, and goalie crease anchors.
- Use the existing player reset and match faceoff paths rather than adding an alternate reset system.
- Add no feature flag, environment setting, fallback formation, external dependency, or persistence migration.

## Non-Goals

- Faceoff stick battles, timing input, referee or puck-drop animation, tie-up outcomes, or faceoff ratings.
- Selecting among neutral-zone or offensive/defensive-zone faceoff dots.
- Line changes, substitutions, multiple forward lines, defensive pair rotation, special teams, penalties, or user-controlled goalies.
- Rewriting the broader AI state machine or adding position-specific tactics beyond role-aware home/faceoff alignment.
