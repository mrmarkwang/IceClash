# Mobile 2v2 Hockey MVP — Manual Play-Mode Smoke Scenario

Run this scenario after the Unity project exists and the Phase 6 stabilization gate is reached.

## Scenario: Complete a local practice match

Given the project opens in the documented Unity version

And the documented start scene is set to the main menu

When the tester presses Play and starts a practice match

Then a marked rink, two goals, one puck, one human skater, and three AI skaters are visible

When the tester uses WASD, Shift, Space, E, and Q

Then movement/sprint, shooting, passing, and checking respond without compilation errors or stuck state

When a skater gains control of the puck, passes or shoots it, and a goal is scored

Then the puck remains physics-based, possession releases, the correct score updates once, a goal notification appears, and players/puck reset before play resumes

When the timer reaches zero

Then the result screen shows WIN or LOSS and final score, REMATCH restarts a clean match, and MAIN MENU returns to the menu

## Evidence to record

- Unity version and target platform used.
- Edit-mode and play-mode test-runner results.
- Manual pass/fail result for each Then statement.
- Device/FPS observation when a mobile test device is available.
- Known limitations that remain within the agreed MVP scope.
