from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine
from dumbest_dungeon.history import history_lines, read_history, run_report, write_run
from dumbest_dungeon.save import read_save


class HistoryTests(unittest.TestCase):
    def test_morgue_is_local_atomic_idempotent_and_detail_requires_opt_in(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        before = engine.snapshot()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = write_run(root, engine, outcome="abandoned")
            self.assertEqual("abandoned", read_save(path)["outcome"])
            self.assertFalse((root / "telemetry").exists())
            self.assertEqual(path, write_run(root, engine, outcome="abandoned"))
            self.assertEqual(1, len(list(root.glob("*.json"))))
            write_run(root, engine, outcome="abandoned", detailed=True)
            exported = next((root / "telemetry").glob("*.ndjson"))
            rows = [json.loads(line) for line in exported.read_text().splitlines()]
            self.assertEqual(engine.state.seed, rows[0]["seed"])
            self.assertEqual(len(engine.state.ledger.records), len(rows) - 1)
        self.assertEqual(before, engine.snapshot())

    def test_history_reports_invalid_files_without_discarding_valid_records(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            write_run(root, engine, outcome="abandoned")
            (root / "bad.json").write_text('{"history_schema":1,"history_schema":2}')
            records, errors = read_history(root)
            self.assertEqual(1, len(records))
            self.assertEqual(1, len(errors))
            self.assertIn("duplicate JSON key", errors[0])
            self.assertIn("Seed 42", "\n".join(history_lines(records[0])))

    def test_report_keeps_offers_plays_and_source_arithmetic_separate(self) -> None:
        engine = GameEngine.new(load_catalog(), 42)
        engine.record("card_offer", "reward:technique", offered=["brace"])
        engine.record("card_choice", "reward:technique", picked="brace", skipped=[])
        engine.record("damage", "test:hit", requested=9, absorbed=3, amount=6, hp_loss=4, overkill=2)
        report = run_report(engine)
        self.assertEqual({"offered": 1, "picked": 1}, report["cards"]["brace"])
        self.assertEqual(4, report["sources"]["test:hit"]["damage_hp_loss"])
        self.assertIsNone(report["elapsed_seconds"])
        self.assertEqual("unmeasured", report["duration_basis"])


if __name__ == "__main__":
    unittest.main()
