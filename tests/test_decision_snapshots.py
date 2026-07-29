from __future__ import annotations

import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.io import parse_tenhou_xml, parse_tenhou_xml_file, read_mjai_events
from kenjaku.training.decision_snapshots import (
    DECISION_SNAPSHOT_KIND,
    build_decision_snapshots,
    write_decision_snapshots_jsonl,
    write_mjai_decision_snapshots_jsonl,
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

    def test_builds_sanma_kita_snapshots(self) -> None:
        game = parse_tenhou_xml(_sanma_kita_xml())
        snapshots = build_decision_snapshots(game, decision_types=("kita",))

        self.assertEqual([snapshot["decision_type"] for snapshot in snapshots], ["kita", "kita"])
        self.assertEqual(
            [snapshot["actual_action"]["kind"] for snapshot in snapshots],
            ["kita", "pass"],
        )
        self.assertEqual(snapshots[0]["kita_event_index"], 1)
        self.assertEqual(snapshots[0]["draw_event_index"], 0)
        self.assertEqual(snapshots[0]["actual_action"]["tile"], "N")
        self.assertEqual(snapshots[0]["actual_action"]["consumed"], ["N"])
        self.assertEqual(
            [action["kind"] for action in snapshots[1]["legal_actions"]],
            ["pass", "kita"],
        )
        self.assertIsNone(snapshots[1]["kita_event_index"])

    def test_mjai_export_accepts_kita_snapshots(self) -> None:
        snapshots = build_decision_snapshots(
            parse_tenhou_xml(_sanma_kita_xml()),
            decision_types=("kita",),
        )
        with TemporaryDirectory() as directory:
            path = Path(directory) / "kita.mjson"
            count = write_mjai_decision_snapshots_jsonl(path, snapshots)
            events = read_mjai_events(path)

        self.assertEqual(count, 2)
        self.assertEqual(events[0]["kenjaku_meta"]["decision_type"], "kita")

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

    def test_write_mjai_decision_snapshots_parse_as_mjai_events(self) -> None:
        snapshots = build_decision_snapshots(
            parse_tenhou_xml_file(EVENTS_FIXTURE),
            decision_types=("discard", "call", "riichi"),
            limit=3,
        )
        with TemporaryDirectory() as directory:
            path = Path(directory) / "snapshots.mjson"
            count = write_mjai_decision_snapshots_jsonl(path, snapshots)
            events = read_mjai_events(path)

        self.assertEqual(count, len(snapshots))
        self.assertEqual(len(events), len(snapshots))
        self.assertTrue(all(event["type"] == "request_action" for event in events))
        self.assertEqual(events[0]["kenjaku_meta"]["row_id"], snapshots[0]["row_id"])
        self.assertIn("observed_action", events[0]["kenjaku_meta"])


def _sanma_kita_xml() -> str:
    return """
    <mjloggm>
      <UN n0="east" n1="south" n2="west" />
      <INIT
        seed="0,0,0,0,0,72"
        ten="350,350,350"
        oya="0"
        hai0="0,4,8,12,16,20,24,28,32,36,40,44,120"
        hai1="1,5,9,13,17,21,25,29,33,37,41,45,121"
        hai2="2,6,10,14,18,22,26,30,34,38,42,46,122"
      />
      <T123 />
      <N who="0" m="30752" />
      <T124 />
      <D124 />
      <RYUUKYOKU ten="350,350,350" />
    </mjloggm>
    """


if __name__ == "__main__":
    unittest.main()
