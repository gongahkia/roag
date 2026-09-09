import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import CardInstance, GameEngine


class LiveStackTests(unittest.TestCase):
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
