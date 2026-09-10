import unittest
from fractions import Fraction

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import CardInstance, GameEngine


class LiveStackTests(unittest.TestCase):
    def test_diminishing_item_family_uses_exact_monotonic_basis_points(self):
        expected = {
            "trauma_mesh": (200, 2000),
            "nerve_dampener": (300, 3000),
            "targeting_prism": (400, 3500),
        }
        catalog = load_catalog()
        for identity, (first, soft_cap) in expected.items():
            effect = catalog.items[identity]["effects"][0]
            contract = effect.contract
            values = [contract.value(count) for count in range(65)]
            self.assertEqual(Fraction(first, 10000), values[1])
            self.assertTrue(all(before <= after for before, after in zip(values, values[1:])))
            self.assertTrue(all(value < Fraction(soft_cap, 10000) for value in values[1:]))
            detail = GameEngine.new(catalog, 42).effect_description("item", identity, 1)
            self.assertIn("next:", detail)
            self.assertIn("Soft cap:", detail)

    def test_capacitor_changes_function_at_three_copies(self):
        for count, energy in ((1, 3), (2, 3), (3, 4), (6, 5), (100, 5)):
            engine = GameEngine.new(load_catalog(), 42)
            engine.acquire_item("capacitor_bank", count)
            engine.start_combat("lost_shift")
            self.assertEqual(energy, engine.state.energy)
            self.assertTrue(all(hero.statuses.get("focus") == 1 for hero in engine.living_heroes()))
            restored = GameEngine.from_snapshot(load_catalog(), engine.snapshot())
            self.assertEqual(engine.snapshot(), restored.snapshot())

    def test_spare_magazine_only_expands_the_opening_draw(self):
        for count, hand in ((1, 6), (2, 6), (3, 7), (99, 7)):
            engine = GameEngine.new(load_catalog(), 42)
            engine.acquire_item("spare_magazine", count)
            engine.start_combat("lost_shift")
            self.assertEqual(hand, len(engine.state.hand))
            engine.end_turn()
            self.assertEqual(5, len(engine.state.hand))

    def test_reserve_cell_releases_once_and_cannot_refill_itself(self):
        for count, expected in ((1, 1), (4, 2)):
            engine = GameEngine.new(load_catalog(), 42)
            engine.acquire_item("reserve_cell", count)
            engine.start_combat("lost_shift")
            # constructed combat, with normal card costs and shared starting energy.
            engine.state.hand = [CardInstance("brace") for _ in range(5)]
            for _ in range(3):
                engine.play_card(0)
            self.assertEqual(expected, engine.state.energy)
            engine.play_card(0)
            self.assertEqual(expected - 1, engine.state.energy)
            restored = GameEngine.from_snapshot(load_catalog(), engine.snapshot())
            self.assertEqual(engine.snapshot(), restored.snapshot())

    def test_first_copy_utility_changes_offers_wounds_and_pickups(self):
        engine = GameEngine.new(load_catalog(), 42)
        for identity in ("quiet_bearings", "reward_index", "sterile_filter", "oracle_relay"):
            engine.acquire_item(identity)
            effect = engine.catalog.items[identity]["effects"][0]
            self.assertEqual(1, effect.contract.value(1))
        hero = engine.living_heroes()[0]
        self.assertEqual(4, len(engine.boon_options(hero.id)))
        engine._add_status(hero, "wound", 3)
        self.assertEqual(2, hero.statuses["wound"])
        engine.acquire_item("salvage_magnet")
        self.assertEqual(1, engine.state.items["salvage_magnet"])
        self.assertEqual(2, engine.acquire_item("salvage_magnet"))
        self.assertEqual(3, engine.state.items["salvage_magnet"])
        self.assertEqual(2, engine.acquire_item("salvage_magnet"))
        self.assertEqual(3, engine.acquire_item("bulkhead_laminate"))
        self.assertEqual(3, engine.state.items["bulkhead_laminate"])
        self.assertEqual(engine.snapshot(), GameEngine.from_snapshot(load_catalog(), engine.snapshot()).snapshot())
