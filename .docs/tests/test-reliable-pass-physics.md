# Reliable Pass Physics E2E Scenarios

## Scenario 1 - Distance-scaled launch pace

1. Build the prototype arena and establish possession with a human-team passer.
2. Place the intended teammate at representative short, medium, and long distances.
3. Initiate a deterministic pass at each distance.
4. Verify each launch uses a controlled initial velocity calculated from the configured distance bands.
5. Verify launch speed increases monotonically across the representative distances and stays within the configured endpoint speeds.

## Scenario 2 - Reliable unobstructed reception

1. Clear opponents from the passing lane.
2. Send stationary-target passes at representative short, medium, and long distances.
3. Simulate normal Rigidbody physics without moving the puck directly during flight.
4. Verify each puck reaches the intended reception zone and becomes controlled by the intended receiver.

## Scenario 3 - Moving receiver and automatic control

1. Move the intended receiver laterally while leading the pass target with the existing lead calculation.
2. Send the pass and simulate receiver movement plus normal puck physics.
3. Verify the local reception zone redirects/slows the puck into the receiver's stick control.
4. Verify possession changes to that receiver and the existing control manager selects the receiving human-team player.

## Scenario 4 - Interceptable physical flight

1. Place an opponent directly in the passing lane before the intended reception zone.
2. Send the pass and simulate normal physics.
3. Attempt ordinary opponent possession as the puck reaches the opponent's stick.
4. Verify the opponent can become carrier, or the collision changes the trajectory so the intended receiver does not receive the original clean pass.
5. Send a separate pass beyond the receiver so it is moving away outside the reception zone.
6. Verify intended-receiver eligibility terminates and later repositioning/re-entry does not magnetically restore that defeated pass.

## Scenario 5 - Generic release isolation

1. Establish possession and perform a generic puck release used by shots/free releases.
2. Verify the generic release applies its requested velocity and has no active intended receiver capture for that puck motion.
3. Place a loose puck inside ordinary stick range above the stick's maximum claim speed and verify the claim is rejected.
4. Place a slow loose puck outside ordinary stick range and verify the claim is rejected.
