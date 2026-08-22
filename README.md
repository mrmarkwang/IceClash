# IceClash

Mobile 2v2 arcade hockey prototype, beginning with a local playable Unity slice.

## Development baseline

- Unity `6000.5.9f1` (Unity 6) using the default 3D project baseline.
- Unity Input System `1.19.0` for keyboard and gamepad input.
- iOS and Android modules are installed locally; device builds are not part of Phase 1.

## Run the Phase 1 prototype

1. Open this folder in Unity Hub with Unity `6000.5.9f1`.
2. Open `Assets/_Project/Scenes/PrototypeArena.unity`.
3. Enter Play Mode.

The scene generates a vertically oriented hockey rink with rounded boards, inset goals, center/blue/goal lines, goal creases, center/zone faceoff circles and dots, a local blue skater, a free physics puck, and an elevated follow camera.

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
- This scene still contains one skater, so assisted passing and checking need the Phase 3 local 2v2 setup to be exercised manually. The action contracts are ready for those skaters.

## Current limitations

- There is one controllable skater only; AI, scoring, menus, HUD, mobile buttons, and match flow are later phases.
- The rink is generated from primitives so it can be tuned/replaced without asset dependencies.
