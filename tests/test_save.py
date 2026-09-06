from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine
from dumbest_dungeon.save import read_save, write_save


class SaveTests(unittest.TestCase):
    def setUp(self) -> None:
        self.catalog = load_catalog()

    def test_exploration_save_round_trip(self) -> None:
        engine = GameEngine.new(self.catalog, 101)
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
        engine.start_combat("drones")
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "run.json"
            write_save(path, engine.snapshot())
            loaded = GameEngine.from_snapshot(self.catalog, read_save(path))
        engine.end_turn()
        loaded.end_turn()
        self.assertEqual(engine.snapshot(), loaded.snapshot())


if __name__ == "__main__":
    unittest.main()
