# IceClash

Mobile 2v2 arcade hockey prototype, beginning with a local playable Unity slice.

## Development baseline

- Unity `6000.5.9f1` (Unity 6) using the default 3D project baseline.
- Unity Input System `1.19.0` for keyboard and gamepad input.
- iOS and Android modules are installed locally; device builds are not part of Phase 1.

## Run the local 2v2 prototype

1. Open this folder in Unity Hub with Unity `6000.5.9f1`.
2. Open `Assets/_Project/Scenes/PrototypeArena.unity`.
3. Enter Play Mode.

The scene generates a vertically oriented hockey rink with rounded boards, inset goals, center/blue/goal lines, goal creases, center/zone faceoff circles and dots, one local blue skater, one allied AI, two red AI opponents, a free physics puck, and an elevated follow camera.

## Editor controls

| Action | Keyboard | Controller |
| --- | --- | --- |
| Move | WASD | Left stick |
| Sprint | Left Shift | Left-stick click |
| Shoot (Phase 2) | Space | Right trigger |
| Pass (Phase 2) | E | West button |
| Check (Phase 2) | Q | East button |

## Phase 2 status

- The local skater can claim a nearby free puck. Carrying steers the physics puck to a forward stick point without parenting it.
- Space shoots from anywhere while the skater possesses the puck; the prototype sends the shot toward the opposing net and buffers a very recent press through its short action lock. E passes to the nearest eligible teammate when one exists, and Q checks a valid nearby opposing skater. All values are Inspector-tunable on `PlayerController` and `PuckController`.
- The Phase 3 roster now supplies pass and check targets; use the Editor controls above for manual action-feel checks while AI skaters exercise the same gameplay contracts.

## Phase 3 status

- The reusable `Assets/_Project/Prefabs/Resources/Skater.prefab` supplies the common skater shell. `LocalMatchSetup` spawns the one-human/three-AI roster with stable IDs, team identity, reset positions, and live `MatchData`/`TeamData`/`PlayerData` snapshots.
- Each `AiPlayerInput` selects Defend, ChasePuck, Support, Attack, Shoot, or Recover behavior and sends movement, sprint, pass, shoot, and check commands through the same `IPlayerInput` path as keyboard/controller input.
- In Unity, choose **IceClash > Run Phase 3 Smoke Check** to verify the generated rink, local 2v2 roster, snapshot identity, shared command path, and observable AI movement with chase/defend role selection.

## Current limitations

- Scoring, menus, HUD, mobile buttons, and match flow are later phases. Phase 3 AI intentionally uses direct steering and simple distance decisions rather than pathfinding or advanced tactics.
- The rink is generated from primitives so it can be tuned/replaced without asset dependencies.
