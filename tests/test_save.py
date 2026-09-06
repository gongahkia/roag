from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine, RuleError
from dumbest_dungeon.save import read_save, write_save


class SaveTests(unittest.TestCase):
    def setUp(self) -> None:
        self.catalog = load_catalog()

    def test_exploration_save_round_trip(self) -> None:
        engine = GameEngine.new(self.catalog, 101)
        first_step = engine.path_to(*engine.room_position(1))[0]
        engine.step_exploration(*first_step)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "run.json"
            write_save(path, engine.snapshot())
            loaded = GameEngine.from_snapshot(self.catalog, read_save(path))
        self.assertEqual(engine.snapshot(), loaded.snapshot())

    def test_hub_selection_save_round_trip(self) -> None:
        engine = GameEngine.new(self.catalog, 100, start_in_hub=True)
        engine.toggle_hub_crew("warden")
        engine.toggle_hub_crew("breacher")
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "hub.json"
            write_save(path, engine.snapshot())
            loaded = GameEngine.from_snapshot(self.catalog, read_save(path))
        self.assertEqual("hub", loaded.state.phase)
        self.assertEqual(engine.state.hub_selection, loaded.state.hub_selection)
        self.assertEqual(engine.snapshot(), loaded.snapshot())

    def test_mid_combat_save_preserves_random_stream(self) -> None:
        engine = GameEngine.new(self.catalog, 202)
        patrol = engine.state.patrols[0]
        patrol.x, patrol.y = engine._neighbors((engine.state.party_x, engine.state.party_y))[0]
        engine.step_exploration(patrol.x, patrol.y)
        self.assertEqual(patrol.id, engine.state.active_patrol_id)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "run.json"
            write_save(path, engine.snapshot())
            loaded = GameEngine.from_snapshot(self.catalog, read_save(path))
        engine.end_turn()
        loaded.end_turn()
        self.assertEqual(engine.snapshot(), loaded.snapshot())

    def test_invalid_saved_terrain_is_rejected(self) -> None:
        engine = GameEngine.new(self.catalog, 303)
        snapshot = engine.snapshot()
        snapshot["state"]["world_tiles"][0] = "broken"
        with self.assertRaisesRegex(RuleError, "malformed world terrain"):
            GameEngine.from_snapshot(self.catalog, snapshot)

        snapshot = engine.snapshot()
        first_row = snapshot["state"]["world_tiles"][0]
        snapshot["state"]["world_tiles"][0] = "." + first_row[1:]
        with self.assertRaisesRegex(RuleError, "disconnected world terrain"):
            GameEngine.from_snapshot(self.catalog, snapshot)


if __name__ == "__main__":
    unittest.main()
