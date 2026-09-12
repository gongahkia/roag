from __future__ import annotations

import json
import unittest
from dataclasses import replace

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.contracts import Effect, Enemy, Opcode, Technique


class ContractTests(unittest.TestCase):
    def test_bundled_catalog_is_loaded_once_and_nested_content_is_read_only(self) -> None:
        catalog = load_catalog()
        self.assertIs(catalog, load_catalog())
        actions = (
            lambda: catalog.balance.update(death_chance=1),
            lambda: catalog.cards.pop("brace"),
            lambda: catalog.cards["brace"]["effects"].append({"op": "draw", "amount": 99}),
            lambda: catalog.cards["brace"]["effects"][0].update(amount=99),
            lambda: catalog.raw["heroes"].clear(),
            lambda: catalog.art["heroes"]["warden"].reverse(),
        )
        for action in actions:
            with self.assertRaisesRegex(TypeError, "immutable"):
                action()

    def test_registered_runtime_types_preserve_json_representation(self) -> None:
        catalog = load_catalog()
        card = catalog.cards["brace"]
        self.assertIsInstance(card, Technique)
        self.assertEqual("warden", card.owner)
        self.assertIsInstance(card.effects[0], Effect)
        self.assertEqual(Opcode.BLOCK, card.effects[0].opcode)
        self.assertEqual(dict(card), json.loads(json.dumps(card)))
        for enemy in catalog.enemies.values():
            self.assertIsInstance(enemy, Enemy)
            self.assertTrue(all(isinstance(effect, Effect) for action in enemy.actions for effect in action.effects))

    def test_explicit_fixture_override_does_not_mutate_cached_catalog(self) -> None:
        catalog = load_catalog()
        fixture = replace(catalog, balance={**catalog.balance, "death_chance": 1})
        self.assertEqual(1, fixture.balance["death_chance"])
        self.assertNotEqual(1, catalog.balance["death_chance"])
        with self.assertRaises(TypeError):
            fixture.balance["death_chance"] = 0


if __name__ == "__main__":
    unittest.main()
