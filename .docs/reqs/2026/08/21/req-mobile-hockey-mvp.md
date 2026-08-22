# Mobile 2v2 Hockey MVP — Requirements

## Problem

IceClash needs a first playable mobile hockey prototype that proves the core arcade loop is fun before the team invests in online multiplayer, production art, accounts, or live-service features. The prototype must be simple to open and test in the Unity Editor while retaining clear seams for a later network-authoritative 2v2 game.

## Requirement

Create a standalone Unity project for iOS and Android containing a local 2v2 practice match: one human-controlled player, three AI players, an independently simulated puck, two goals, basic scoring, and a three-minute match flow. The player must be able to test keyboard/controller input in the Editor and have placeholder mobile controls available for later device testing.

## Acceptance Criteria

- [ ] The Unity project opens without missing-script errors and follows the agreed `_Project` folder structure for gameplay, UI, input, data, prefabs, scenes, and tests.
- [ ] Pressing Play launches a main menu; PLAY starts a practice match with one human player, one allied AI, two opposing AI players, a puck, two goals, and a basic marked rink.
- [ ] The in-game camera presents the rink in a vertical play orientation: the long skating direction and the opposing goals read from the bottom toward the top of the screen, not side-to-side.
- [ ] The human player can move with WASD, sprint with Shift, shoot with Space, pass with E, and check with Q in the Unity Editor; controller input and placeholder mobile controls are represented through the Unity Input System.
- [ ] Player movement, team identity, player ID, puck possession, stamina placeholder, and the defined movement/action states are represented as modular gameplay state rather than one monolithic component.
- [ ] The puck uses a Rigidbody and Collider, remains independently physics-simulated, has configurable friction/bounce/control behavior, and records team possession and last player touch.
- [ ] A nearby eligible player can gain, carry, pass, and shoot the puck; possession can break through a shot, pass, interception, or successful check.
- [ ] Passing targets a suitable nearby teammate and remains interceptable; shooting releases the puck and respects configurable power, accuracy, speed, and cooldown settings.
- [ ] Checking has a configurable range, force, duration, and cooldown; a valid hit can knock down an opponent and dislodge possession without realistic violence or injury systems.
- [ ] The three AI players use a simple, observable finite-state behavior to chase the puck, support the carrier, defend, attack, recover, and make basic shooting/passing decisions.
- [ ] A puck entering either goal awards the correct team, pauses play, shows a goal message, resets skaters and puck, and resumes after about two seconds.
- [ ] The HUD displays both scores, a three-minute countdown, player/team/possession context, goal notifications, and placeholder mobile action controls.
- [ ] At match end, the game displays WIN or LOSS with the final score and supports REMATCH and MAIN MENU. Overtime is excluded.
- [ ] Local input, AI commands, and future network input can drive the same player-control path through small, practical interfaces or equivalent abstractions; no online multiplayer is implemented.
- [ ] The completed prototype can be run through documented Unity steps, and its known limitations and next recommended milestone are documented.

## Constraints

- Use Unity and C#, the Unity Input System, and Unity Physics. NavMesh or a simple alternative may be used only if it supports the MVP cleanly.
- Target iOS and Android, while treating the Unity Editor keyboard/controller controls as the primary fast test path.
- Use low-poly/primitive placeholder art, simple lighting/materials, and inexpensive effects with a 60 FPS target on a typical current mobile device.
- Keep gameplay systems modular under `Assets/_Project`; separate input, player control, puck control, match state, camera, AI, UI, and data concerns.
- Expose gameplay tuning values in the Inspector or ScriptableObjects where the prototype brief identifies them as configurable.
- Verify compilation after each major subsystem once implementation starts.

## Non-Goals

- Online multiplayer, matchmaking, networking SDKs, server authority, lag compensation, or replication.
- Authentication, Firebase, account/profile/social screens, commerce, or analytics.
- Production-quality graphics, character customization, advanced animation, realistic hockey simulation, injuries, penalties, overtime, or sophisticated tactical AI.
- Flutter gameplay integration.
- Feature flags, environment-based fallbacks, compatibility layers, or premature backend/data persistence.

## Open Questions

- Which Unity LTS version and render pipeline should be the project baseline? Default recommendation: the current Unity LTS with URP only if the project is created from a mobile URP template; otherwise use the default 3D template to keep Phase 1 lean.
- Should initial device testing prioritize touch UI responsiveness or establish the editor gameplay loop first? Default plan: establish editor gameplay first, then enable the placeholder touch layout once core actions work.
