# IceClash — Phase 1 Local PvE Hockey

IceClash is a mobile-first Unity hockey prototype focused on simple controls and meaningful team decisions. Phase 1 is a completely local match: three forwards, two defensemen, and one goalie per team. The user controls one human-team skater at a time; AI controls everyone else.

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

The scene builds the marked rink, ten skaters, two AI goalies, physics puck, two goal triggers, faceoff/match state, hockey camera, controlled-player marker, scoreboard, timer, joystick, and possession-adaptive action buttons at runtime. Each lineup has a Center, Left Wing, Right Wing, Left Defense, and Right Defense. Opening and post-goal faceoffs place centers at the center dot, wings outside the circle, defensemen goal-side, and goalies at their crease anchors.

## Controls

| Action | Keyboard | Gamepad | Touch |
| --- | --- | --- | --- |
| Skate only | WASD | Left stick | Bottom-left joystick |
| Recommended pass | Tap E | Tap west button | Tap PASS |
| Deke | Left Shift | South button | Tap DEKE |
| Charge / release shot | Hold/release Space | Hold/release right trigger | Hold/release SHOOT |
| Switch skater | Q | North button | Tap SWITCH while defending |
| Contextual defensive check | F | East button | Tap CHECK while defending |

WASD and the fixed, always-visible lower-left joystick never aim passes or shots. During human possession or loose-puck play, the touch actions are PASS, DEKE, and SHOOT. When a Red skater establishes possession, they become exactly SWITCH and CHECK; the joystick remains unchanged. SWITCH uses the same useful-defender selection as Q/gamepad switching. CHECK chooses a close body check or a longer forward pull check from the controlled Blue skater. A successful check dislodges the puck into normal free physics and never grants possession directly.

While the human player possesses the puck, a subtle dotted path shows the currently recommended teammate. Tap PASS to release a deterministic, interceptable physics pass along that recommendation; no drag is required. PAS, distance, facing, motion, and fatigue shape pace, lead, and deviation, but a clean lane never fails from a hidden random roll. An unobstructed pass is captured only after physically entering the intended teammate's CTR/PAS-weighted reception zone, while collisions and opponent claims can still defeat it. DEKE starts a short CTR/AGI-based puck-control and protection window; joystick direction, skating speed, and timing still determine the maneuver. There is no sprint, stick lift, separate shot-type button, or special ability in Phase 1.

## Player attributes and builds

Every skater has a level and nine independently allocated ratings from 40 to 95:

- **SPD** sets maximum skating speed; **ACC** separately sets how quickly that speed is reached; **AGI** changes turning response.
- **STA** slows exertion and improves recovery. Sustained hard skating drains stamina, fatigue gradually reduces physical/action output, and faceoff reset restores the skater.
- **CTR** improves claims, carry stability, deke protection, and receiving forgiveness.
- **SHT** changes charged-shot power and deterministic accuracy forgiveness while charge, facing, rink position, puck position, lateral motion, and fatigue still matter.
- **PAS** changes pass pace, deterministic deviation, lead, and its contribution to the receiver's control window. Defender positioning and puck physics still allow interceptions.
- **STR** changes body contact and puck protection; **DEF** changes body/pull defensive execution. Checks also require valid range, approach speed, alignment, contact position, timing, and a favorable contest against the carrier.

Level grants eight attribute points per level after level 1. Raising a rating through 69 costs one point per step, 70–84 costs two, and 85–95 costs three, so even a level-50 build cannot maximize all nine attributes. The prototype assigns role-oriented level-25 builds: Center/Playmaker, Left Wing/Sniper, Right Wing/Speed, Left Defense/Power, and Right Defense/Two-Way. These are runtime defaults; future allocation UI can use the same atomic budget contract.

Attributes never choose an action or direction. The human still moves, positions, passes, dekes, shoots, switches, and checks explicitly. Easy/Normal AI difficulty controls reaction intervals, target error, tactical choices, and charge choices separately; it does not overwrite physical attributes or action execution.

## Architecture and tuning

- `PlayerMovementController` owns acceleration, deceleration, momentum, analog speed, and speed-aware turning.
- `PlayerAttributeBuild` owns level budgets, progressive costs, atomic allocation, normalized ratings, and the five validated prototype builds; `PlayerController` owns current stamina and applies fatigue without synthesizing input.
- `DekeController` turns only an explicit DEKE press during possession into a bounded CTR/AGI control-protection window.
- `StickPuckInteraction` and `PuckController` keep possession force-based on an independent Rigidbody; `PassReceivingZone` provides local, intended-receiver capture into the existing stick controller.
- `PassTargetSelector` continuously scores a recommended teammate using facing and tactical context; `PassController` owns pooled dotted-path feedback, configurable distance-to-speed tuning, and the imperfect non-homing physics release triggered by one PASS tap.
- `ShootController` converts one held/released input into bounded charge, facing/goal-assisted direction, power, and spread without consuming movement input.
- `PlayerControlManager` automatically selects the established human-team puck carrier or the human-team skater closest to the puck after opponent possession; it never switches from puck trajectory. `PlayerSwitchController` remains the manual SWITCH override and performs the input/AI/marker/camera transfer.
- `PlayerInputController` reacts to established carrier changes and safely reuses the offensive action slots as SWITCH/CHECK during opponent possession. `DefensiveCheckController` owns the human-team cooldown and contextual body/pull result; `DefensiveCheckTuning` is the persisted Inspector tuning asset for ranges, cone, cooldown, puck pace, and bounded separation.
- `HockeyPlayerAI` uses the required eight-state local state machine; `AIFormationController` maps C/LW/RW/LD/RD roles to mirrored faceoff/home positions; `HockeyGoalieAI` handles crease tracking and saves.
- `MatchController`, `FaceoffController`, and `GoalTrigger` own clock, score, role-aware center-faceoff resets, and results.
- `HockeyCameraController`, `VirtualJoystick`, `MobileActionButton`, `PlayerInputController`, `MobileControlsBuilder`, `SafeAreaFitter`, and `MatchHUD` provide the stable landscape presentation and shared input route.

Gameplay feel values are serialized fields on these focused components so they can be tuned in the Inspector without changing team or match architecture.

## Verification

With the Editor out of Play Mode, choose **IceClash > Run Phase 1 PvE Smoke Check**. The check enters Play Mode, builds the real arena, verifies the 5v5-plus-goalies roster and one-human invariant, checks modular gameplay/camera/HUD wiring, validates the attribute economy/presets/mappings/fatigue/snapshots/AI separation, possession-adaptive actions, touch/hardware mappings, contested body/pull checks, deterministic shots/passes, short/medium/long and moving-target reception plus obstruction interception, scores and resets a goal, expires the timer, and exits. A passing run logs:

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
- Defensive checks use placeholder motion and puck response without final hit/stick animation, penalties, audio, or effects; attribute balance remains prototype tuning.
- Device-specific safe-area, performance, thermal, and multi-touch feel still require testing on the intended phones/emulators.
