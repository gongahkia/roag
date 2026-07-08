from __future__ import annotations

import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.io import parse_tenhou_xml_file
from kenjaku.training.decision_snapshots import (
    DECISION_SNAPSHOT_KIND,
    build_decision_snapshots,
    write_decision_snapshots_jsonl,
)

MINIMAL_FIXTURE = Path("data/fixtures/tenhou/minimal_4p.xml")
EVENTS_FIXTURE = Path("data/fixtures/tenhou/events_4p.xml")


class DecisionSnapshotTests(unittest.TestCase):
    def test_builds_discard_call_and_riichi_snapshots_in_event_order(self) -> None:
        game = parse_tenhou_xml_file(EVENTS_FIXTURE)
        snapshots = build_decision_snapshots(
            game,
            source={"label": "unit"},
            input_paths=[EVENTS_FIXTURE],
            xml_file_count=1,
        )

        self.assertEqual({snapshot["kind"] for snapshot in snapshots}, {DECISION_SNAPSHOT_KIND})
        self.assertEqual(
            {snapshot["decision_type"] for snapshot in snapshots}, {"discard", "call", "riichi"}
        )
        self.assertEqual(snapshots, sorted(snapshots, key=lambda row: row["event_index"]))
        self.assertTrue(all(snapshot["source"]["label"] == "unit" for snapshot in snapshots))

    def test_limit_and_decision_type_filter_are_applied_after_sort(self) -> None:
        game = parse_tenhou_xml_file(EVENTS_FIXTURE)
        snapshots = build_decision_snapshots(game, decision_types=("discard",), limit=1)

        self.assertEqual(len(snapshots), 1)
        self.assertEqual(snapshots[0]["decision_type"], "discard")

    def test_include_outcome_adds_terminal_payload(self) -> None:
        game = parse_tenhou_xml_file(MINIMAL_FIXTURE)
        snapshots = build_decision_snapshots(
            game,
            decision_types=("discard",),
            limit=1,
            include_outcome=True,
        )

        self.assertIn("terminal_outcome", snapshots[0])
        self.assertEqual(snapshots[0]["terminal_outcome"]["kind"], "ryuukyoku")

    def test_invalid_decision_type_and_negative_limit_fail_fast(self) -> None:
        game = parse_tenhou_xml_file(MINIMAL_FIXTURE)
        with self.assertRaisesRegex(ValueError, "unsupported decision type"):
            build_decision_snapshots(game, decision_types=("kan",))
        with self.assertRaisesRegex(ValueError, "limit"):
            build_decision_snapshots(game, limit=-1)

    def test_call_snapshot_links_called_event_from_discard_prefix(self) -> None:
        game = parse_tenhou_xml_file(EVENTS_FIXTURE)
        snapshots = build_decision_snapshots(game, decision_types=("call",))

        self.assertEqual(len(snapshots), 1)
        self.assertEqual(snapshots[0]["call_event_index"], 4)
        self.assertEqual(snapshots[0]["actual_action"]["kind"], "pon")
        self.assertEqual(snapshots[0]["mjai_events"][-1]["type"], "dahai")

    def test_write_decision_snapshots_jsonl_creates_parent_and_counts_rows(self) -> None:
        snapshots = build_decision_snapshots(
            parse_tenhou_xml_file(MINIMAL_FIXTURE),
            decision_types=("discard",),
        )
        with TemporaryDirectory() as directory:
            path = Path(directory) / "nested" / "snapshots.jsonl"
            count = write_decision_snapshots_jsonl(path, snapshots)
            rows = [json.loads(line) for line in path.read_text(encoding="utf-8").splitlines()]

        self.assertEqual(count, len(snapshots))
        self.assertEqual(rows[0]["kind"], DECISION_SNAPSHOT_KIND)


if __name__ == "__main__":
    unittest.main()
