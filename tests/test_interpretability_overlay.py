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
        self.assertEqual(report["page_size"], 100)
        self.assertEqual(len(report["decisions"][0]["top_alternatives"]), 3)
        self.assertIn("hand_pattern", report["decisions"][0])
        self.assertEqual(report["decisions"][0]["dora_indicators"], ["5m"])
        self.assertIn(
            report["decisions"][0]["shanten_delta_bin"],
            {"improves", "same", "worsens", "unknown"},
        )
        for alternative in report["decisions"][0]["top_alternatives"]:
            self.assertIn("policy_probability", alternative)
            self.assertIn("shanten_delta", alternative)
            self.assertIn("estimated_deal_in_risk", alternative)
            self.assertIn("expected_point_impact", alternative)
        self.assertIn("Policy probability", html)
        self.assertIn("Deal-in risk", html)
        self.assertIn('id="search"', html)
        self.assertIn('id="round-filter"', html)
        self.assertIn('id="seat-filter"', html)
        self.assertIn('id="tile-filter"', html)
        self.assertIn('id="shanten-filter"', html)
        self.assertIn('"hand_pattern"', html)
        self.assertIn('"dora_indicators":["5m"]', html)
        self.assertNotIn("/private/raw/replay.xml", html)

    def test_large_overlay_keeps_static_dom_paginated(self) -> None:
        report = {
            "title": "Large Overlay",
            "policy_kind": "heuristic-discard-overlay-v0",
            "top_alternatives_per_decision": 3,
            "page_size": 100,
            "decision_count": 10_000,
            "source_labels": {"synthetic": 10_000},
            "disclaimer": "synthetic",
            "decisions": [_minimal_decision(index) for index in range(10_000)],
        }

        html = format_interpretability_overlay_html(report)

        self.assertIn('"page_size":100', html)
        self.assertIn("matching decisions", html)
        self.assertEqual(html.count('<section class="decision">'), 1)

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
        self.assertIn('id="decision-list"', html)
        self.assertIn('"decisions"', html)


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
        "dora_indicators": ["5m"],
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


def _minimal_decision(index: int) -> dict[str, object]:
    return {
        "index": index,
        "row_id": f"synthetic:{index}",
        "round_index": index // 250,
        "event_index": index,
        "seat": index % 4,
        "actual_discard": _tile_name(index % 34),
        "current_shanten": index % 4,
        "actual_shanten_delta": (index % 3) - 1,
        "shanten_delta_bin": ("improves", "same", "worsens")[index % 3],
        "hand_pattern": "1m 2m 3m",
        "hand_tiles": ["1m", "2m", "3m"],
        "dora_indicators": [_tile_name((index + 4) % 34)],
        "top_alternatives": [],
    }
