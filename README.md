# IceClash — Phase 1 Local PvE Hockey

IceClash is a mobile-first Unity hockey prototype focused on simple controls and meaningful team decisions. Phase 1 is a completely local match: three human-team skaters and one goalie versus three AI-team skaters and one goalie. The user controls one human-team skater at a time; AI controls everyone else.

Multiplayer, networking, Photon Fusion, matchmaking, accounts, Firebase, backend services, monetization, and store integration are intentionally excluded until the local hockey loop is fun.

## Development baseline

- Unity `6000.5.9f1` (Unity 6), default 3D baseline.
- Unity Input System `1.19.0` for keyboard, gamepad, mouse, and touch input.
- Runtime placeholder geometry and the reusable `Assets/_Project/Prefabs/Resources/Skater.prefab`.
- Landscape mobile layout with a 60 FPS target.

## Run the prototype

1. Open this folder in Unity Hub with Unity `6000.5.9f1`.
2. Open `Assets/_Project/Scenes/PrototypeArena.unity`.
3. Enter Play Mode.

The scene builds the marked rink, six skaters, two AI goalies, physics puck, two goal triggers, faceoff/match state, hockey camera, controlled-player marker, scoreboard, timer, joystick, and three action buttons at runtime.

## Controls

| Action | Keyboard | Gamepad | Touch |
| --- | --- | --- | --- |
| Skate only | WASD | Left stick | Bottom-left joystick |
| Recommended pass | Tap E | Tap west button | Tap PASS |
| Deke input (debug only) | — | — | Tap DEKE |
| Charge / release shot | Hold/release Space | Hold/release right trigger | Hold/release SHOOT |
| Switch skater | Q | North button | — |

WASD and the floating left joystick never aim passes or shots. While the human player possesses the puck, a subtle dotted path shows the currently recommended teammate. Tap PASS to release an imperfect, interceptable physics pass along that recommendation; no drag is required and successful reception is not guaranteed. DEKE currently emits an input/debug signal only. There is no sprint, deke gameplay, poke check, stick lift, separate shot-type button, or special ability in Phase 1.

## Architecture and tuning

- `PlayerMovementController` owns acceleration, deceleration, momentum, analog speed, and speed-aware turning.
- `StickPuckInteraction` and `PuckController` keep possession force-based on an independent Rigidbody.
- `PassTargetSelector` continuously scores a recommended teammate using facing and tactical context; `PassController` owns pooled dotted-path feedback and the imperfect non-homing physics release triggered by one PASS tap.
- `ShootController` converts one held/released input into bounded charge, facing/goal-assisted direction, power, and spread without consuming movement input.
- `PlayerControlManager` automatically selects the established human-team puck carrier or a useful defender after opponent possession; it never switches from puck trajectory. `PlayerSwitchController` remains the manual SWITCH override and performs the input/AI/marker/camera transfer.
- `HockeyPlayerAI` uses the required eight-state local state machine; `AIFormationController` supplies count-independent formation slots; `HockeyGoalieAI` handles crease tracking and saves.
- `MatchController`, `FaceoffController`, and `GoalTrigger` own clock, score, resets, and results.
- `HockeyCameraController`, `VirtualJoystick`, `MobileActionButton`, `PlayerInputController`, `MobileControlsBuilder`, `SafeAreaFitter`, and `MatchHUD` provide the stable landscape presentation and shared input route.

Gameplay feel values are serialized fields on these focused components so they can be tuned in the Inspector without changing team or match architecture.

## Verification

With the Editor out of Play Mode, choose **IceClash > Run Phase 1 PvE Smoke Check**. The check enters Play Mode, builds the real arena, verifies the 3v3-plus-goalies roster and one-human invariant, checks modular gameplay/camera/HUD wiring, scores and resets a goal, expires the timer, and exits. A passing run logs:

`PHASE1_PVE_SMOKE_PASS`

Static Phase 1 boundary check:

```sh
rg -n -i 'Photon|Fusion|Unity\.Netcode|Relay|Matchmaking|Firebase|AuthenticationService|Lobby' Assets Packages/manifest.json Packages/packages-lock.json ProjectSettings
```

The expected result is no matches. Manual feel and mobile multi-touch scenarios are documented in `.docs/tests/test-mobile-hockey-mvp.md`.

## Current prototype limitations

- Placeholder primitives, no final character/stick animation, limited audio/feedback, and no production art.
- Direct steering and lightweight formation logic rather than NavMesh or a behavior tree.
- Simplified possession contests, goalie reach, faceoffs, and hockey rules; no penalties, icing, offsides, or overtime.
- Device-specific safe-area, performance, thermal, and multi-touch feel still require testing on the intended phones/emulators.
