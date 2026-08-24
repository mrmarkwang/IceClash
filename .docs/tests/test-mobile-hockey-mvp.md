# Local PvE Hockey Prototype — E2E Scenarios

Record Unity version, platform/Game view, exact log path, and pass/fail evidence for each scenario. Automated smoke evidence may satisfy structural assertions; gameplay-feel and touch assertions require observation.

## Scenario: Launch the complete local roster

Given `PrototypeArena` is opened in Unity `6000.5.9f1`

When Play Mode starts

Then the marked small rink contains three Blue skaters, one Blue goalie, three Red skaters, one Red goalie, two goals, and one independent physics puck

And exactly one Blue skater has human input while every other skater has AI input

And no networking, authentication, backend, matchmaking, or service initialization occurs

## Scenario: Skate with one-stick movement

Given active play and one controlled Blue skater

When the tester moves, releases, sharply turns, reverses, and uses partial joystick deflection

Then the skater accelerates and decelerates smoothly, retains responsive momentum, rotates toward travel, supports 360-degree movement, and changes speed with input magnitude

And there is no sprint or separate rotation control

## Scenario: Carry and contest the puck

Given a free puck within the controlled skater's forward stick-control area

When the skater approaches and carries it while changing speed and direction

Then the puck remains an independent dynamic Rigidbody, follows with bounded visible motion near the stick, and is not parented or permanently glued

When the puck is released or contested

Then reclaim locks and physical motion permit interception or possession by another eligible skater

## Scenario: Select and execute an assisted pass

Given the controlled carrier has multiple teammates at different angles with defenders between some lanes

When the tester aims toward one teammate and presses PASS

Then joystick direction strongly influences selection while distance, openness, defender separation, lane obstruction, and offensive progress also affect the target

And the puck releases physically toward a lead point with bounded error so misses and interceptions remain possible

## Scenario: Charge and aim one-button shots

Given the controlled skater has the puck

When the tester briefly presses and releases SHOOT

Then a prompt lower-power shot follows the approximate joystick/facing direction

When the tester holds SHOOT longer and releases

Then the shot is stronger but remains bounded, includes configured inaccuracy, and uses no separate shot-type button

## Scenario: Switch the controlled skater

Given at least two eligible Blue skaters and active play

When the tester presses SWITCH while defending and again while attacking

Then selection favors a useful puck challenger on defense and a useful puck/offensive-support option on attack

And local input, the YOU marker, and the camera transfer together without flicker while the former controlled skater resumes AI

## Scenario: Observe imperfect team AI

Given NORMAL difficulty and several possession changes

When Blue AI teammates and all Red skaters react

Then their state machines visibly use support, attack, defend, chase, receive, shoot, idle, and return behaviors as situations permit

And they maintain recognizable spacing, challenge one at a time, backcheck, seek lanes, pass, shoot, and occasionally make imperfect decisions

When difficulty changes to EASY

Then decisions/reactions are slower and pass/shot/movement quality is lower than NORMAL

## Scenario: Goalies save and reset

Given shots approach each goal from center and angled lanes

When a puck enters a goalie's reaction and save area

Then the goalie tracks laterally near its crease, attempts a bounded save or cover, and produces a playable rebound when not covering

When a goal reset occurs

Then both goalies return to their anchors with cleared movement/save state

## Scenario: Score and complete a match

Given an active timed match

When the puck fully enters either goal

Then the correct Human Team or AI Team score increments exactly once, play pauses with a goal message, all actors and the puck reset, and a faceoff resumes play

When the timer reaches zero

Then play stops and the HUD shows the correct Human Win, AI Win, or Draw result with the final score

## Scenario: Use the landscape mobile HUD

Given a landscape mobile Game view or device

When Play Mode starts

Then score and `MM:SS` appear at the top, one virtual joystick appears bottom-left, and only PASS, SHOOT, and SWITCH appear bottom-right

When the tester holds the joystick and simultaneously presses or holds an action

Then both inputs remain active, buttons are comfortably sized, and no sprint, deke, poke, stick-lift, separate shot-type, or special-ability control appears

## Scenario: Keep the play readable

Given active play, puck movement, and one or more player switches

When the controlled skater, puck, teammates, and goal direction spread across the nearby play area

Then the camera moves smoothly, keeps the controlled skater and puck readable, preserves a stable attacking direction, and retargets without excessive rotation

## Evidence to record

- Unity compile and smoke log containing `PHASE1_PVE_SMOKE_PASS` with zero compiler errors.
- Automated roster/component/input/goal-reset/static-prohibition assertions.
- Manual Editor observations for skating, puck feel, pass selection, shot charge, AI behavior, goalie behavior, camera, HUD, scoring, and result.
- Mobile simulator/device observations for multi-touch controls, landscape safe layout, FPS, and thermals when hardware is available; otherwise an explicit pending-device note.
- Known placeholder-animation, physics-tuning, AI-depth, and art/audio limitations that remain within Phase 1 scope.
