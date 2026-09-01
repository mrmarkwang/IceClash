# Wearable Equipment Slots E2E

## Scenario: Generated player exposes only supported wearables

1. Regenerate the modular hockey character assets.
2. Open the generated `HockeyPlayer` prefab.
3. Inspect the equipment bindings and visible equipment hierarchy.
4. Verify the independent wearable bindings are Helmet, Visor, Gloves, and Skates.
5. Verify the separately equipped production Stick remains present.
6. Repeat the structural check for the `Resources/Skater` variant and every generated player in `ModularCharacterTest`.
7. Verify no ShoulderPads, Jersey, Pants, Socks, chest-padding, or shin-equipment slot or placeholder renderer is present in any checked artifact.

## Scenario: Supported equipment remains independent during animation

1. Run the modular character Play Mode smoke check.
2. Clear and replace each supported binding through the loadout validation path.
3. Verify changing one binding does not change any other binding.
4. Verify gloves track both animated hands and skates track both animated feet.
5. Verify clearing and re-equipping Stick disables and restores two-hand IK without replacing the stable targets.
6. Verify the puck can still be claimed, carried, and released by the generated player.

## Scenario: Team coloring uses the main character visual

1. Run the full prototype-arena smoke check so the production roster is built.
2. Inspect at least one blue skater, one red skater, and both goalies.
3. Verify each main-character renderer uses its configured team material.
4. Verify supported helmet, visor, glove, and skate renderers are not implicitly recolored by the main-character material operation.
5. Verify the blue and red character visuals remain visibly distinguishable without separate jersey or sock equipment.
