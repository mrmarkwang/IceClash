# Offside Rule E2E Scenarios

## Blue attack warning and tag-up

1. Start live play with Blue carrying the puck on or behind the north attacking blue line.
2. Move a different Blue skater beyond that line while the puck stays outside.
3. Verify the north offensive zone shows a visibly red grid with no enabled physics colliders and the south grid remains hidden.
4. Move the premature attacker back to the neutral side before the puck crosses.
5. Verify the grid disappears and play continues without changing score or entering a faceoff.

## Blue offside entry and restart

1. Recreate the Blue warning, choosing the premature attacker's/crossing puck's left or right side.
2. Move the puck from the neutral side to beyond the north attacking blue line while the warning remains active.
3. Verify exactly one offside stoppage occurs, both warning grids hide, scores remain unchanged, and player control is disabled.
4. Verify the puck is on the nearest north neutral-zone faceoff dot, skaters retain their mirrored formation around that dot, and goalies remain at their crease anchors.
5. Verify normal play resumes after the existing faceoff delay.

## Possession transition while warning

1. Arm a Blue warning, release the puck for a same-team pass, and keep the premature Blue attacker beyond the line.
2. Verify the warning remains armed while the puck is temporarily loose and an entry still produces offside.
3. Re-arm the Blue warning, then let a Red skater establish possession before the puck enters Blue's offensive zone.
4. Verify both warning grids hide immediately and subsequent Red movement into that zone does not call offside against Blue.

## Mirrored Red offside

1. Start live play with Red carrying the puck on or behind the south attacking blue line.
2. Move a different Red skater beyond that line and verify only the south offensive-zone red grid appears.
3. Move the puck beyond the south line and verify one offside stoppage with unchanged score.
4. Verify the puck and skaters restart at the nearest south neutral-zone faceoff dot and the warning hides.

## Legal entry and existing match flow

1. Put all attackers onside and cross either attacking blue line with the puck first.
2. Verify no warning or stoppage occurs.
3. Run opening faceoff, post-goal reset, goal scoring, clock expiry, and result assertions.
4. Verify center faceoffs remain centered and the score/clock/result flow remains unchanged.

## Execution Evidence

- Executed through the integrated `PrototypeArenaSmokeCheck` on 2026-08-28 using the Unity `6000.5.9f1` batch runner against an exact temporary copy of the current project sources because the main checkout was open in Unity.
- Observed process exit code: `0`.
- Observed log marker: `PHASE1_PVE_SMOKE_PASS` with `offsideWarning=true`, `offsideTagUp=true`, `offsideTurnoverClear=true`, `sweptOffsideOrdering=true`, `mirroredOffside=true`, `neutralDotRestart=true`, and `offsideFaceoffResume=true`.
- The integrated assertions cover exact-line boundaries, red/non-colliding grid presentation, tag-up clearing, temporary loose-puck retention, completed same-team reception into the zone, opponent-possession clearing, mirrored Blue/Red calls, unchanged scores, disabled faceoff gameplay, translated neutral-dot placement, timer-path resumption, legal entry, and existing center/post-goal flow.
- Swept-ordering assertions also move a tagging-up attacker and the puck across their boundaries within one sample, and move a new premature attacker plus the puck across within one sample, verifying each call from interpolated crossing-time state rather than endpoint state.
