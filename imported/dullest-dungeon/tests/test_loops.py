from __future__ import annotations

import unittest
from dataclasses import asdict

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine, RuleError


class LoopTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.catalog = load_catalog()

    def won_engine(self, seed: int = 4901) -> GameEngine:
        engine = GameEngine.new(self.catalog, seed)
        encounter_id = engine._freeze_finale(engine.state.objectives[1])
        room = next(room for room in engine.state.rooms if room.kind == "boss")
        engine.state.current_room = room.id
        engine.start_combat(encounter_id, "boss")
        for enemy in engine.state.enemies:
            enemy.hp = 0
        engine._combat_victory()
        return engine

    def test_finale_records_base_clear_before_player_choice(self) -> None:
        engine = self.won_engine()
        self.assertEqual("post_victory", engine.state.phase)
        self.assertTrue(engine.state.base_victory)
        self.assertEqual(0, engine.state.loop_depth)
        self.assertGreater(engine.state.score, 0)
        self.assertEqual(1, len(engine.state.boss_sequence))
        self.assertEqual("base_victory", engine.state.ledger.records[-1].kind)

    def test_extract_ends_and_invalid_loop_commands_fail(self) -> None:
        engine = self.won_engine()
        engine.extract()
        self.assertEqual("victory", engine.state.phase)
        with self.assertRaises(RuleError):
            engine.descend_again()

    def test_descent_preserves_build_and_remixes_short_saveable_loop(self) -> None:
        engine = self.won_engine()
        crew = [(hero.id, hero.hp, hero.alive) for hero in engine.state.heroes]
        deck = [asdict(card) for card in engine.state.deck]
        old_world = engine.state.world_tiles
        item_id = next(iter(self.catalog.items))
        engine.state.items[item_id] = 3
        engine.descend_again()
        self.assertEqual("exploration", engine.state.phase)
        self.assertEqual(1, engine.state.loop_depth)
        self.assertEqual(1, engine.state.required_objectives)
        self.assertGreaterEqual(engine.state.pressure, 1100)
        self.assertNotEqual(old_world, engine.state.world_tiles)
        self.assertEqual(crew, [(hero.id, hero.hp, hero.alive) for hero in engine.state.heroes])
        self.assertEqual(deck, [asdict(card) for card in engine.state.deck])
        self.assertEqual(3, engine.state.items[item_id])
        loaded = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        self.assertEqual(engine.snapshot(), loaded.snapshot())

    def test_loop_seed_and_world_are_deterministic(self) -> None:
        first = self.won_engine(4902)
        second = self.won_engine(4902)
        first.descend_again()
        second.descend_again()
        self.assertEqual(first.snapshot(), second.snapshot())


if __name__ == "__main__":
    unittest.main()
