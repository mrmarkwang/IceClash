# Reliable Pass Physics

## Problem

Normal successful passes currently use one launch speed and depend on generic Rigidbody damping and ordinary pickup limits. This makes pass arrival vary too much by distance and can make the puck feel slow or fail to reach the intended teammate cleanly.

## Requirement

A pass must select its intended teammate, calculate a planar direction and distance, choose a configurable launch speed appropriate to that distance, and launch with controlled puck velocity. The puck must remain a normal physics object during flight so collisions and opponent interceptions can defeat the pass. When an unobstructed pass enters the intended teammate's configurable reception zone, it must transition naturally into that teammate's possession and the existing possession system must transfer human control to that receiver.

## Acceptance Criteria

- [x] Pass launch speed is calculated from pass distance using Inspector-configurable short, medium, and long distance/speed tuning rather than one fixed speed.
- [x] A launched pass receives a controlled initial velocity and then remains subject to normal Rigidbody damping and collisions.
- [x] Unobstructed short, medium, and long passes reliably enter the intended receiver's configurable reception zone without losing most of their useful travel velocity beforehand.
- [x] Entering the reception zone redirects and slows the puck into controlled possession at the intended player's stick.
- [x] Intended human-team reception triggers the existing possession event and automatically transfers human control to the receiving player.
- [x] An opponent in the passing lane can intercept or deflect the puck before intended reception.
- [x] Pass pace, distance thresholds, reception radius, and reception entry speed are configurable rather than fixed final constants.
- [x] Existing shot releases and ordinary loose-puck claims retain their prior behavior.

## Constraints

- Preserve non-homing physics flight outside the receiver's local reception zone.
- Preserve collision detection and opponent interaction throughout the pass.
- Avoid global Rigidbody damping changes as the primary means of making passes arrive.
- Keep the existing recommended-target selection, pass feedback, possession events, and control-transfer architecture.

## Non-Goals

- Adding new pass buttons, manual pass aiming, saucer passes, networking, animation, or a broader puck-physics rewrite.
- Guaranteeing reception when a collision or opponent changes the puck's path.
- Changing shot power or shot possession rules.
