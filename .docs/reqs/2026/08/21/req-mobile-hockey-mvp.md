# Local PvE Hockey Prototype — Requirements

## Problem

IceClash has a small local 2v2 foundation, but it does not yet provide a complete mobile hockey match. The prototype needs to prove that one-stick movement and three actions can produce readable, replayable team hockey before any multiplayer, service, account, monetization, or production-art work begins.

## Requirement

Deliver a fully playable local PvE match in Unity for mobile landscape play. The human team and AI team each field three skaters and one AI goalie. The user controls one human-team skater at a time while AI controls every other player. A complete match must support skating, physics-based possession, assisted passing, charged shooting, useful player switching, believable imperfect team AI, goalie saves, scoring, faceoff resets, a timer, match results, a hockey camera, and uncluttered touch controls.

The prototype should optimize for simple controls and meaningful positional decisions. Placeholder geometry and visuals are expected; gameplay feel and modular tuning take priority.

## Acceptance Criteria

- [x] The Unity `6000.5.9f1` project opens and compiles without missing-script errors, with gameplay systems modular under `Assets/_Project/Scripts` and no multiplayer, networking, backend, account, Firebase, matchmaking, monetization, or store implementation.
- [x] `PrototypeArena` launches a local match containing three Blue skaters plus one Blue goalie, three Red skaters plus one Red goalie, one independent Rigidbody puck, two goals, and a clear small marked rink built from placeholder geometry.
- [x] Exactly one Blue skater is human-controlled at a time; the other Blue skaters and all Red skaters are AI-controlled, and roster construction remains count-driven so a later 5v5 expansion does not require rewriting gameplay systems.
- [x] `PlayerMovementController` provides 360-degree camera-relative skating with analog speed, smooth acceleration/deceleration, responsive momentum, speed-aware turning, and rotation toward travel direction without sprint or a separate rotation input.
- [x] Puck possession remains physics-based and modular through `PuckController` and `StickPuckInteraction`; the puck follows a moving stick-control point with visible bounded motion rather than parenting or permanent gluing, and can be contested after release.
- [x] One PASS action scores eligible teammates by joystick direction, distance, openness, defender separation, lane obstruction, and offensive progress; it releases an interceptable imperfect pass toward the best target without manual target selection.
- [x] One SHOOT action charges while held and releases on button-up; short holds release quickly at lower power, longer holds produce stronger bounded shots, joystick/facing determines approximate direction, and configured inaccuracy permits misses.
- [x] One SWITCH action selects a useful non-goalie Blue skater with stable scoring based on puck proximity, puck-carrier pressure while defending, and offensive support while attacking; input, highlight, and camera transfer together without control flicker.
- [x] Non-controlled skaters use a simple `HockeyPlayerAI` state machine containing Idle, Support, Attack, Defend, ChasePuck, ReceivePass, Shoot, and ReturnToPosition states and visibly perform formation support, passing-lane movement, puck pressure, goal-side defense, backchecking, passing, and shooting.
- [x] AI difficulty exposes EASY and NORMAL only; EASY has slower reaction/decision timing, lower movement and pass quality, and less accurate shots than NORMAL, while both levels intentionally allow mistakes.
- [x] Each `HockeyGoalieAI` stays near its crease anchor, tracks the puck laterally, attempts bounded saves or covers, releases playable rebounds, and returns to its anchor after match resets.
- [x] `HockeyCameraController` smoothly follows the controlled skater while biasing framing toward the puck, keeps play direction understandable, avoids excessive rotation, and retargets on switch/reset.
- [x] A match runs through Faceoff, Playing, GoalPause, and Finished states; a valid goal increments the correct score once, pauses play, resets every actor and the puck, resumes with a faceoff, and time expiry produces the correct Human Win, AI Win, or Draw result.
- [x] The landscape HUD shows Human Team score, AI Team score, and `MM:SS`; touch UI provides one bottom-left virtual joystick and only PASS, SHOOT, and SWITCH on the bottom right, with large multi-touch-safe controls and no sprint, deke, poke, stick-lift, shot-type, or special-ability buttons.
- [x] Keyboard/gamepad Editor controls exercise the same input contract as touch controls so movement, pass, held shoot/release, and switch can be tested without a device.
- [x] Automated smoke verification confirms the generated 3v3-plus-goalies roster, one-local-input invariant, modular movement/puck/AI/camera/match/UI wiring, score/reset behavior, and absence of networking/service packages or namespaces.
- [x] README documentation explains how to run the match, Editor and touch controls, tuning locations, verification commands, current placeholder limitations, and the explicit Phase 1 networking prohibition.
- [ ] A development build launches `PrototypeArena` on a phone or emulator and completes one touch-controlled match without a blocking runtime, layout, input, scoring, reset, goalie, AI, camera, or result defect.

## Constraints

- Use Unity/C#, the Unity Input System, Unity Physics, and placeholder primitives/assets already present in the repository.
- Target mobile landscape and 60 FPS while retaining keyboard/gamepad Editor testing.
- Keep movement, puck/stick interaction, pass, shoot, switching, AI, goalie, camera, match flow, and UI in focused components with Inspector-tunable values.
- Use three skaters and one goalie per team for Phase 1; keep roster/formation data extensible to five skaters later.
- Prefer deterministic scoring/selection rules with small bounded AI/action error over complex navigation, animation, or simulation systems.

## Non-Goals

- Multiplayer, PvP, Photon Fusion, Netcode for GameObjects, Relay, networking, matchmaking, lobbies, accounts, authentication, Firebase, backend services, cloud saves, analytics, ads, commerce, store integration, or season passes.
- Full 5v5, line changes, penalties, offsides, icing, fighting, injuries, overtime, user-controlled goalies, advanced faceoffs, or NHL-level goalie simulation.
- Sprint, deke, poke check, stick lift, separate wrist/slap-shot buttons, special abilities, or additional on-screen gameplay buttons.
- Final graphics, licensed content, production animation, cosmetics, progression, commentary, or polished audio.
- Feature flags, environment-specific gameplay forks, service fallbacks, or speculative networking abstractions.
