import json
import tempfile
import unittest
from fractions import Fraction
from pathlib import Path

from dumbest_dungeon.content import ContentError, load_catalog
from dumbest_dungeon.contracts import PersistentEffect
from dumbest_dungeon.passives import EffectKey, Unit, persistent_effect
from dumbest_dungeon.engine import GameEngine


class PassiveContractTests(unittest.TestCase):
    def test_live_linear_item_family_preserves_each_count_and_cap(self) -> None:
        expected = {"bulkhead_laminate": (1, 8), "med_gel_ampoule": (2, 10), "flare_phosphor": (4, 20),
                    "auto_suture": (1, 6), "survey_relay": (1, 6), "deflection_foil": (1, 6)}
        for identity, (amount, cap) in expected.items():
            effect = load_catalog().items[identity]["effects"][0]
            for count in (0, 1, 2, 5, 10, 100):
                self.assertEqual(min(amount * count, cap), effect.contract.value(count))

    def test_explicit_effect_is_typed_cached_and_has_exact_units(self) -> None:
        raw = {"key": "marked_damage_bonus", "unit": "basis_points", "stack": {"mode": "linear", "amount": 425}}
        rule = persistent_effect(raw)
        self.assertIs(rule, persistent_effect(json.loads(json.dumps(raw))))
        self.assertEqual(EffectKey.MARKED_DAMAGE_BONUS, rule.key)
        self.assertEqual(Unit.BASIS_POINTS, rule.unit)
        self.assertEqual(Fraction(17, 200), rule.value(2))
        self.assertEqual("8.50%", rule.display(rule.stack.value(2)))

    def test_multiplicative_policy_reports_factor_and_supplies_its_bonus(self) -> None:
        rule = persistent_effect({"key": "marked_damage_bonus", "unit": "basis_points",
                                  "stack": {"mode": "multiplicative", "amount": 5000, "max_effective_stacks": 100}})
        self.assertEqual(0, rule.value(0))
        self.assertEqual(Fraction(5, 4), rule.value(2))
        self.assertEqual("2.2500x", rule.display(rule.stack.value(2)))

    def test_strict_fields_units_and_unused_operands_are_rejected(self) -> None:
        valid = {"key": "start_block", "unit": "count", "stack": {"mode": "linear", "amount": 3}}
        malformed = [
            {**valid, "hidden": 1}, {**valid, "unit": "basis_points"},
            {**valid, "stack": {"mode": "linear", "amount": True}},
            {**valid, "stack": {"mode": "linear", "amount": 2, "every": 3}},
            {**valid, "stack": {"mode": "linear", "amount": 2, "converted_from": ""}},
            {**valid, "stack": {"mode": "table", "amount": 2, "table": [0, 2]}},
        ]
        for raw in malformed:
            with self.assertRaises(ValueError):
                persistent_effect(raw)

    def test_loader_exposes_immutable_typed_persistent_effects(self) -> None:
        raw = json.loads(json.dumps(load_catalog().raw))
        raw["items"][0]["effects"] = [{"key": "stacked_start_block", "unit": "count",
                                       "stack": {"mode": "linear", "amount": 1, "cap": 8}}]
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "game.json"
            path.write_text(json.dumps(raw))
            catalog = load_catalog(path)
            effect = catalog.items[raw["items"][0]["id"]]["effects"][0]
            self.assertIsInstance(effect, PersistentEffect)
            self.assertEqual(3, effect.contract.value(3))
            engine = GameEngine.new(catalog, 42)
            engine.acquire_item(raw["items"][0]["id"], 3)
            engine.start_combat("lost_shift")
            self.assertTrue(all(hero.block >= 3 for hero in engine.living_heroes()))
            description = engine.effect_description("item", raw["items"][0]["id"], 3)
            self.assertIn("Current: 3; next: 4", description)
            with self.assertRaises(TypeError):
                effect["stack"]["amount"] = 99
            raw["items"][0]["effects"][0]["stack"]["mystery"] = 1
            path.write_text(json.dumps(raw))
            with self.assertRaises(ContentError):
                load_catalog(path)
