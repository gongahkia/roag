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

    def test_live_linear_boon_family_preserves_each_count_and_cap(self) -> None:
        expected = {
            "iron_benediction": (3, 15), "clear_signal": (3, 15),
            "second_wind": (2, 10), "sterile_seal": (1, 3),
            "formation_anchor": (1, 2), "adrenal_coil": (2, 8),
            "field_rations": (2, 10), "mercy_circuit": (2, 8),
        }
        catalog = load_catalog()
        for identity, (amount, cap) in expected.items():
            effect = catalog.boons[identity]["effects"][0]
            for count in (0, 1, 2, 5, 10, 100):
                self.assertEqual(min(amount * count, cap), effect.contract.value(count))
        blood_price = catalog.boons["blood_price"]["effects"][0].contract
        self.assertEqual(Fraction(1, 10), blood_price.value(1))
        self.assertEqual(Fraction(1, 4), blood_price.value(100))

    def test_diminishing_boons_have_exact_first_copy_and_soft_caps(self) -> None:
        expected = {
            "gentle_hands": (800, 5000),
            "marked_quarry": (800, 5000),
            "calm_under_fire": (700, 4500),
            "last_word": (500, 2000),
        }
        catalog = load_catalog()
        for identity, (first, soft_cap) in expected.items():
            contract = catalog.boons[identity]["effects"][0].contract
            values = [contract.value(count) for count in range(65)]
            self.assertEqual(Fraction(first, 10000), values[1])
            self.assertTrue(all(before <= after for before, after in zip(values, values[1:])))
            self.assertTrue(all(value < Fraction(soft_cap, 10000) for value in values[1:]))
            detail = GameEngine.new(catalog, 42).effect_description("boon", identity, 2)
            self.assertIn("Soft cap:", detail)

    def test_trigger_boons_expose_the_values_consumed_by_their_limiters(self) -> None:
        catalog = load_catalog()
        for identity, key, cap in (
            ("hunters_rhythm", "damage_draw", 2),
            ("quick_hands", "quick_hands", 3),
            ("countercurrent", "countercurrent_draw", 2),
            ("resonant_circuit", "resonant_energy", 2),
        ):
            contract = catalog.boons[identity]["effects"][0].contract
            self.assertEqual([0, 1, min(2, cap), cap], [contract.value(n) for n in (0, 1, 2, 99)])
        vigilance = catalog.boons["vigilance"]["effects"]
        self.assertEqual([0, 2, 2, 2], [vigilance[0].contract.value(n) for n in (0, 1, 2, 99)])
        self.assertEqual([0, 0, 2, 8], [vigilance[1].contract.value(n) for n in (0, 1, 2, 99)])

    def test_linear_and_inert_curses_have_exact_authored_caps(self) -> None:
        expected = {
            "cowards_mark": (1, 4), "night_terrors": (1, 4),
            "leaking_lamp": (1, 3), "open_circuit": (1, 2),
            "scavengers_itch": (3, 9), "static_prayer": (4, 4),
            "open_wound": (1, 1), "power_leech": (1, 1),
            "gravity_knot": (1, 1), "dread_forecast": (3, 3),
        }
        catalog = load_catalog()
        for identity, (amount, cap) in expected.items():
            contract = catalog.curses[identity]["effects"][0].contract
            self.assertEqual([0, amount, min(amount * 2, cap), cap],
                             [contract.value(n) for n in (0, 1, 2, 99)])
        dead = catalog.curses["dead_channel"]["effects"][0].contract
        self.assertEqual([0, 0, 0], [dead.value(n) for n in (0, 1, 99)])

    def test_diminishing_curse_penalties_are_exact_and_bounded(self) -> None:
        expected = {
            "glass_bones": (600, 4500),
            "thin_blood": (800, 5000),
            "panic_echo": (800, 6000),
        }
        catalog = load_catalog()
        for identity, (first, soft_cap) in expected.items():
            contract = catalog.curses[identity]["effects"][0].contract
            values = [contract.value(count) for count in range(65)]
            self.assertEqual(Fraction(first, 10000), values[1])
            self.assertTrue(all(before <= after for before, after in zip(values, values[1:])))
            self.assertTrue(all(value < Fraction(soft_cap, 10000) for value in values[1:]))
            self.assertIn("Soft cap:", GameEngine.new(catalog, 42).effect_description("curse", identity, 2))

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
