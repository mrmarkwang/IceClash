# Adaptive Defense Controls E2E Scenarios

## Scenario 1 - Possession changes the visible actions

1. Start the arena with no established puck carrier.
2. Verify the joystick and PASS, DEKE, and SHOOT actions are visible.
3. Establish possession with a Red skater.
4. Verify the joystick remains unchanged and exactly SWITCH and CHECK are visible in the action area.
5. Hold one visible action pointer while changing Red possession to loose-puck play, then release the old pointer.
6. Verify PASS, DEKE, and SHOOT return and the old pointer creates no synthetic offensive or defensive action.
7. Repeat Red → loose → Blue → Red possession and verify every state has the expected labels, visibility, and cleared press/hold/release phases.

## Scenario 2 - Touch SWITCH uses the existing control transfer

1. Establish Red possession and record the controlled Blue defender.
2. Tap the visible SWITCH action.
3. Verify `PlayerSwitchController` selects another useful Blue skater subject to its existing cooldown.
4. Verify input ownership, AI ownership, controlled marker, camera target, and selection event all follow the newly controlled skater.

## Scenario 3 - Close body check

1. Place the controlled Blue skater in configured body-contact range of the Red puck carrier.
2. Tap CHECK.
3. Verify the action resolves as a body check, the expected Red carrier releases the puck, and bounded separation is applied.
4. Verify the applied external velocity does not exceed the validated `6 m/s` cap.
5. Verify the puck remains independent and loose; the Blue checker is not assigned possession by the check operation.

## Scenario 4 - Forward pull check

1. Place the Red puck carrier outside body-contact range but inside the controlled Blue skater's configured forward pull range and cone.
2. Tap CHECK.
3. Verify the action resolves as a pull check and dislodges the puck toward the checker.
4. Simulate multiple normal physics ticks and verify no check code applies continuing steering or homing after the initial release velocity.
5. Verify the puck remains independent and any later Blue possession occurs only through the existing physical claim path.

## Scenario 5 - Rejected and bounded checks

1. Attempt CHECK with no carrier, a Blue carrier, a Red carrier outside pull range, and a Red carrier behind the checker outside the forward cone.
2. Verify each attempt leaves possession and player motion unchanged.
3. Perform one successful check and immediately attempt another before cooldown expires.
4. Verify the second attempt is rejected and no duplicate dislodge or impulse occurs.
5. SWITCH to a different Blue skater during that cooldown and verify CHECK remains rejected until the same human-team cooldown expires.
6. Disable gameplay during a body impulse and verify CHECK has no effect and pending external motion is cleared.
7. Reset actors for a faceoff and verify no external motion carries into the reset positions and the new play phase begins with reset check cooldown.

## Scenario 6 - Persisted tuning clamps

1. Load the Inspector-editable `DefensiveCheckTuning` resource used by the runtime controller.
2. Exercise validation with inverted ranges, invalid cone values, zero/negative cooldown, and excessive puck speed/body impulse.
3. Verify runtime values satisfy body range `0.5–2.0 m`, pull range `0.6–3.5 m` and above body range, forward dot `0–1`, cooldown `0.2–2.0 s`, puck speed `1–15 m/s`, and body impulse `0–6 m/s`.

## Scenario 7 - Hardware action contract

1. Verify the exact Input System mapping declarations used by `LocalPlayerInput`: keyboard `F` and gamepad east for CHECK.
2. Verify the same mapping contract retains keyboard/gamepad PASS, held/released SHOOT, and SWITCH declarations.
3. In a focused Editor or device session, press keyboard `F` and gamepad east while defensive mode is active and verify `PlayerInputController.CheckPressed` observes each input.
4. Exercise the retained PASS, held/released SHOOT, and SWITCH mappings and verify their existing shared-contract signals remain unchanged.
