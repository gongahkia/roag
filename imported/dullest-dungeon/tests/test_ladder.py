from __future__ import annotations

import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine, RuleError
from dumbest_dungeon.ladder import RULES, modifier
from dumbest_dungeon.pressure import PressureSource


class LadderTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.catalog = load_catalog()

    def test_twenty_cumulative_ranks_each_change_the_modifier_vector(self) -> None:
        self.assertEqual(tuple(range(1, 21)), tuple(rule.rank for rule in RULES))
        self.assertTrue(all(len(rule.text) <= 90 for rule in RULES))
        keys = sorted({rule.modifier for rule in RULES})
        vectors = [tuple(modifier(rank, key) for key in keys) for rank in range(21)]
        self.assertTrue(all(before != after for before, after in zip(vectors, vectors[1:])))

    def test_base_rank_is_unchanged_and_rank_four_alters_only_disclosed_economy(self) -> None:
        base = GameEngine.new(self.catalog, 4701)
        ranked = GameEngine.new(self.catalog, 4701, ladder_rank=4)
        self.assertEqual(base.state.world_tiles, ranked.state.world_tiles)
        self.assertEqual(base.state.light - 5, ranked.state.light)
        self.assertEqual(base.state.supplies - 1, ranked.state.supplies)
        base_gain = base._advance_pressure(PressureSource.TRAVEL, 3, "test")
        ranked_gain = ranked._advance_pressure(PressureSource.TRAVEL, 3, "test")
        self.assertGreater(ranked_gain, base_gain)

    def test_anchor_ranks_change_director_dimensions_without_large_stat_inflation(self) -> None:
        base = GameEngine.new(self.catalog, 4702)
        ranked = GameEngine.new(self.catalog, 4702, ladder_rank=20)
        base.state.combat_kind = ranked.state.combat_kind = "boss"
        normal = base.current_director()
        apex = ranked.current_director()
        normal_world = base.world_director()
        apex_world = ranked.world_director()
        self.assertGreater(apex.mutation_slots, normal.mutation_slots)
        self.assertGreater(apex_world.patrol_aggression, normal_world.patrol_aggression)
        self.assertGreater(apex_world.hazard_reach, normal_world.hazard_reach)
        self.assertLessEqual(apex.enemy_health_bp - normal.enemy_health_bp, 500)
        self.assertLessEqual(apex.enemy_damage_bp - normal.enemy_damage_bp, 500)

    def test_rank_round_trips_and_invalid_ranks_fail(self) -> None:
        engine = GameEngine.new(self.catalog, 4703, ladder_rank=20)
        loaded = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        self.assertEqual(20, loaded.state.ladder_rank)
        self.assertEqual(engine.snapshot(), loaded.snapshot())
        for rank in (-1, 21, True):
            with self.assertRaises(RuleError):
                GameEngine.new(self.catalog, 4703, ladder_rank=rank)

    def test_hub_can_select_rank_but_departure_freezes_it(self) -> None:
        engine = GameEngine.new(self.catalog, 4704, start_in_hub=True)
        engine.configure_ladder_rank(4)
        self.assertEqual(4, engine.state.ladder_rank)
        self.assertEqual(95, engine.state.light)
        self.assertEqual(3, engine.state.supplies)
        engine.begin_expedition()
        with self.assertRaises(RuleError):
            engine.configure_ladder_rank(5)


if __name__ == "__main__":
    unittest.main()
