import json
import tempfile
import unittest
from fractions import Fraction
from pathlib import Path

from dumbest_dungeon.content import ContentError, load_catalog
from dumbest_dungeon.contracts import PersistentEffect
from dumbest_dungeon.passives import EffectKey, TRIGGER_DISCLOSURES, Unit, persistent_effect
from dumbest_dungeon.engine import GameEngine


class PassiveContractTests(unittest.TestCase):
    def test_every_effect_has_an_explicit_trigger_and_descendant_disclosure(self) -> None:
        self.assertEqual(set(EffectKey), set(TRIGGER_DISCLOSURES))
        catalog = load_catalog()
        engine = GameEngine.new(catalog, 42)
        for group in ("item", "boon", "curse"):
            definitions = getattr(catalog, group + "s")
            for identity in definitions:
                detail = engine.effect_description(group, identity, 1)
                self.assertIn("Trigger:", detail)
                self.assertIn("descendants", detail)

    def test_live_linear_item_family_preserves_each_count_and_cap(self) -> None:
        expected = {"bulkhead_laminate": (1, 8), "med_gel_ampoule": (2, 10), "flare_phosphor": (4, 20),
                    "auto_suture": (1, 6), "survey_relay": (1, 6), "deflection_foil": (1, 6)}
        for identity, (amount, cap) in expected.items():
            effect = load_catalog().items[identity]["effects"][0]
            for count in (0, 1, 2, 5, 10, 100):
                self.assertEqual(min(amount * count, cap), effect.contract.value(count))

    def test_route_recovery_and_payoff_item_bridges_have_exact_thresholds(self) -> None:
        catalog = load_catalog()
        expected = {
            "base:pathfinder_spool": ([1, 0], [4, 1]),
            "base:hush_index": ([1, 0], [2, 1]),
            "base:trauma_satchel": ([2, 0], [8, 1]),
            "base:flare_capacitor": ([3, 0], [12, 1]),
            "base:scar_laminate": ([1, 0], [2, 2]),
            "base:quarry_battery": ([Fraction(3, 100), 0], [Fraction(923, 10000), 1]),
        }
        for identity, (first, fourth) in expected.items():
            effects = catalog.items[identity]["effects"]
            self.assertEqual(first, [effect.contract.value(1) for effect in effects])
            self.assertEqual(fourth, [effect.contract.value(4) for effect in effects])
            detail = GameEngine.new(catalog, 42).effect_description("item", identity, 3)
            self.assertIn("Current:", detail)
            self.assertIn("next:", detail)

    def test_focus_reserve_and_choice_item_bridges_change_jobs_at_thresholds(self) -> None:
        catalog = load_catalog()
        expected = {
            "base:focus_manifold": ([1, 0], [1, 2]),
            "base:reserve_laminate": ([1, 0], [2, 2]),
            "base:recovery_index": ([1, 0], [4, 0]),
            "base:stealth_spool": ([1, 1], [2, 3]),
            "base:oracle_magnet": ([1, 0], [2, 0]),
            "base:trauma_prism": ([Fraction(3, 200), 0], [Fraction(480, 10000), 1]),
        }
        for identity, (first, fourth) in expected.items():
            effects = catalog.items[identity]["effects"]
            self.assertEqual(first, [effect.contract.value(1) for effect in effects])
            self.assertEqual(fourth, [effect.contract.value(4) for effect in effects])
            self.assertTrue(all(
                effect.contract.value(3) <= effect.contract.value(4)
                for effect in effects
            ))
        self.assertEqual(1, catalog.items["base:recovery_index"]["effects"][1].contract.value(5))
        self.assertEqual(1, catalog.items["base:oracle_magnet"]["effects"][1].contract.value(5))

    def test_late_stack_item_pivots_and_multiplier_are_bounded(self) -> None:
        catalog = load_catalog()
        prism = catalog.items["base:execution_prism"]["effects"][0].contract
        self.assertEqual(Fraction(2, 25), prism.value(1))
        self.assertEqual(prism.value(8), prism.value(80))
        self.assertGreater(prism.value(8), Fraction(4, 5))
        nerve = catalog.items["base:nerve_manifold"]["effects"]
        self.assertEqual((0, 1), (nerve[1].contract.value(3), nerve[1].contract.value(4)))
        magnet = catalog.items["base:field_magnet"]["effects"]
        self.assertEqual((1, 2, 5), (
            magnet[0].contract.value(1), magnet[0].contract.value(5), magnet[1].contract.value(5)
        ))
        reserve = catalog.items["base:reserve_optics"]["effects"]
        self.assertEqual((0, 1, 0, 1), tuple(
            effect.contract.value(count)
            for effect, count in ((reserve[0], 2), (reserve[0], 3), (reserve[1], 4), (reserve[1], 5))
        ))
        silent = catalog.items["base:silent_suture"]["effects"]
        self.assertEqual((1, 2, 1, 2), tuple(
            effect.contract.value(count)
            for effect, count in ((silent[0], 1), (silent[0], 4), (silent[1], 2), (silent[1], 4))
        ))
        broad = catalog.items["base:broad_capacitor"]["effects"]
        self.assertEqual((0, 1, 2, 1), tuple(
            effect.contract.value(count)
            for effect, count in ((broad[0], 1), (broad[0], 2), (broad[0], 4), (broad[1], 5))
        ))

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

    def test_expansion_boons_are_unique_rules_with_distinct_bridges(self) -> None:
        catalog = load_catalog()
        identities = [identity for identity in catalog.boons if identity.startswith("base:")]
        self.assertEqual(12, len(identities))
        shapes = []
        for identity in identities:
            boon = catalog.boons[identity]
            self.assertEqual(2, len(boon["effects"]))
            self.assertTrue(all(
                effect.contract.stack.mode.value in {"unique", "duration_refresh"}
                for effect in boon["effects"]
            ))
            self.assertEqual(
                [effect.contract.value(1) for effect in boon["effects"]],
                [effect.contract.value(9) for effect in boon["effects"]],
            )
            shapes.append(tuple(
                (effect["key"], effect.contract.stack.mode.value, effect.contract.stack.amount)
                for effect in boon["effects"]
            ))
        self.assertEqual(len(shapes), len(set(shapes)))

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

    def test_sequencing_and_threshold_curses_publish_their_real_rules(self) -> None:
        catalog = load_catalog()
        for identity in ("frayed_focus", "tremors"):
            contract = catalog.curses[identity]["effects"][0].contract
            self.assertEqual([0, 1, 2, 3], [contract.value(n) for n in (0, 1, 2, 99)])
        lead = catalog.curses["lead_feet"]["effects"]
        self.assertEqual([0, 1, 1], [lead[0].contract.value(n) for n in (0, 1, 99)])
        self.assertEqual([0, 0, 0, 1], [lead[1].contract.value(n) for n in (0, 1, 2, 3)])
        brittle = catalog.curses["brittle_guard"]["effects"][0].contract
        self.assertEqual([0, 1, 1, 2], [brittle.value(n) for n in (0, 1, 2, 3)])

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
