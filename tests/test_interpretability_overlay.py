from __future__ import annotations

import contextlib
import io
import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.cli import main
from kenjaku.training.decision_snapshots import DECISION_SNAPSHOT_KIND
from kenjaku.training.interpretability_overlay import (
    INTERPRETABILITY_OVERLAY_KIND,
    build_interpretability_overlay,
    format_interpretability_overlay_html,
)


class InterpretabilityOverlayTests(unittest.TestCase):
    def test_build_overlay_renders_top_three_without_input_paths(self) -> None:
        snapshot = _discard_snapshot()
        report = build_interpretability_overlay([snapshot], min_decisions=1)
        html = format_interpretability_overlay_html(report)

        self.assertEqual(report["kind"], INTERPRETABILITY_OVERLAY_KIND)
        self.assertEqual(report["decision_count"], 1)
        self.assertEqual(len(report["decisions"][0]["top_alternatives"]), 3)
        for alternative in report["decisions"][0]["top_alternatives"]:
            self.assertIn("policy_probability", alternative)
            self.assertIn("shanten_delta", alternative)
            self.assertIn("estimated_deal_in_risk", alternative)
            self.assertIn("expected_point_impact", alternative)
        self.assertIn("Policy probability", html)
        self.assertIn("Deal-in risk", html)
        self.assertNotIn("/private/raw/replay.xml", html)

    def test_build_overlay_enforces_min_decisions(self) -> None:
        with self.assertRaisesRegex(ValueError, "expected at least 2"):
            build_interpretability_overlay([_discard_snapshot()], min_decisions=2)

    def test_cli_writes_html_overlay(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            snapshots = root / "snapshots.jsonl"
            output = root / "overlay.html"
            snapshots.write_text(
                json.dumps(_discard_snapshot(), sort_keys=True) + "\nnot-json\n",
                encoding="utf-8",
            )

            stdout = io.StringIO()
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "interpretability-overlay",
                        str(snapshots),
                        "--output",
                        str(output),
                        "--min-decisions",
                        "1",
                    ]
                )
            html = output.read_text(encoding="utf-8")

        self.assertEqual(exit_code, 0)
        self.assertIn("decisions: 1", stdout.getvalue())
        self.assertIn("malformed_snapshot_rows: 1", stdout.getvalue())
        self.assertIn("Decision 1", html)


def _discard_snapshot() -> dict[str, object]:
    hand_counts = [0] * 34
    for index in range(14):
        hand_counts[index] = 1
    visible_counts = [0] * 34
    river_counts_by_seat = [[0] * 34 for _seat in range(4)]
    river_counts_by_seat[1][0] = 1
    return {
        "kind": DECISION_SNAPSHOT_KIND,
        "row_id": "synthetic:r0:e1:discard:s0",
        "decision_type": "discard",
        "source": {"label": "synthetic-test"},
        "input_paths": ["/private/raw/replay.xml"],
        "round_index": 0,
        "event_index": 1,
        "seat": 0,
        "hand_counts": hand_counts,
        "visible_counts": visible_counts,
        "active_riichi_seats": [False, True, False, False],
        "river_counts_by_seat": river_counts_by_seat,
        "legal_actions": [
            {"kind": "discard", "tile": _tile_name(index)}
            for index, count in enumerate(hand_counts)
            if count
        ],
        "actual_action": {"kind": "discard", "tile": "1m"},
    }


def _tile_name(index: int) -> str:
    if index < 9:
        return f"{index + 1}m"
    if index < 18:
        return f"{index - 8}p"
    if index < 27:
        return f"{index - 17}s"
    return ("E", "S", "W", "N", "P", "F", "C")[index - 27]
