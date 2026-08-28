# Skater Roles and Center-Ice Faceoff — E2E Scenarios

## Scenario: Launch a conventional five-skater lineup

Given `PrototypeArena` is opened in Unity `6000.5.9f1`

When Play Mode starts and the opening faceoff is shown

Then each team contains one Center, one Left Wing, one Right Wing, one Left Defense, and one Right Defense plus one goalie

And exactly one Blue skater has human input while every other skater has AI input

## Scenario: Use center-ice faceoff positions

Given the opening faceoff or a faceoff after a goal

When all actors reset before play resumes

Then the Blue and Red centers face each other nearest the center puck without overlapping it

And both wings stand outside the center circle on their role-appropriate sides

And both defensemen stand behind all three forwards toward their own goal

And every skater faces the attacking direction while both goalies remain at their crease anchors

## Scenario: Preserve roles through match play

Given active play after the opening faceoff

When control switches, possession changes, and a goal triggers another faceoff

Then every skater retains the same role identity in the live match snapshot

And every skater returns to the faceoff position assigned to that role before play resumes

## Evidence to record

- Unity smoke diagnostics for per-team role counts, role snapshot identity, mirrored faceoff geometry, attacking rotations, center puck reset, goalie anchors, five-skater roster, and one-human routing.
- A landscape Game-view observation of the opening or post-goal center-ice faceoff if an interactive Editor view is available.
- Any unrelated smoke failure must be recorded separately and must not be treated as a failure of role/faceoff behavior without direct evidence.
