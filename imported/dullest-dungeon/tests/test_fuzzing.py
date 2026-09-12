from __future__ import annotations

import json
import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine
from dumbest_dungeon.fuzzing import fuzz_expedition
from dumbest_dungeon.policies import Command, execute_command


class FuzzingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.catalog = load_catalog()

    def test_random_legal_commands_validate_after_every_checkpoint(self) -> None:
        for fuzz_seed in (7, 71, 701, 7001, 70001):
            report = fuzz_expedition(self.catalog, fuzz_seed, steps=90)
            self.assertGreater(len(report["commands"]), 20)
            GameEngine.from_snapshot(self.catalog, report["final"])

    def test_fuzz_transcript_replays_to_identical_canonical_snapshot(self) -> None:
        report = fuzz_expedition(self.catalog, 811, steps=70)
        engine = GameEngine.new(self.catalog, report["game_seed"])
        for row in report["commands"]:
            execute_command(engine, Command(row["method"], tuple(row["args"])))
            engine = GameEngine.from_snapshot(
                self.catalog, json.loads(json.dumps(engine.snapshot()))
            )
        self.assertEqual(report["final"], engine.snapshot())


if __name__ == "__main__":
    unittest.main()
