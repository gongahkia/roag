from __future__ import annotations

import json
import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine, RuleError
from dumbest_dungeon.telemetry import RunLedger


class LedgerTests(unittest.TestCase):
    def test_records_are_ordered_detached_and_round_trip_without_rng_calls(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        random_state = engine.rng.getstate()
        payload = ["brace", "breach"]
        engine.record("card_offer", "reward", cards=payload)
        payload.clear()
        engine.record("card_pick", "reward", card="brace")
        self.assertEqual([1, 2], [row.sequence for row in engine.state.ledger.records])
        self.assertEqual(["brace", "breach"], engine.state.ledger.records[0].data["cards"])
        self.assertEqual(random_state, engine.rng.getstate())
        loaded = GameEngine.from_snapshot(engine.catalog, json.loads(json.dumps(engine.snapshot())))
        self.assertEqual(engine.snapshot(), loaded.snapshot())

    def test_missing_or_misordered_records_are_rejected(self) -> None:
        ledger = RunLedger()
        ledger.record("card_play", "brace", 0, 1, owner="warden")
        raw = ledger.snapshot()
        raw["records"][0]["sequence"] = 2
        with self.assertRaisesRegex(ValueError, "invalid run record"):
            RunLedger.from_snapshot(raw)
        engine = GameEngine.new(load_catalog(), 42)
        snapshot = engine.snapshot()
        del snapshot["state"]["ledger"]
        with self.assertRaises(RuleError):
            GameEngine.from_snapshot(engine.catalog, snapshot)

    def test_version_27_migration_discloses_unrecorded_history(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        raw = engine.snapshot()
        raw["save_version"] = 27
        del raw["state"]["ledger"]
        loaded = GameEngine.from_snapshot(engine.catalog, raw)
        self.assertEqual(0, loaded.state.ledger.incomplete_before_tick)
        self.assertEqual([], loaded.state.ledger.records)


if __name__ == "__main__":
    unittest.main()
