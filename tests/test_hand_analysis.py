from __future__ import annotations

import contextlib
import io
import json
import unittest

from kenjaku.cli import main
from kenjaku.hand_analysis import build_hand_analysis, parse_hand_tiles


class HandAnalysisTests(unittest.TestCase):
    def test_parse_hand_tiles_accepts_compact_honor_notation(self) -> None:
        tiles = parse_hand_tiles("234m 567p 22s 666z")

        self.assertEqual(
            [tile.notation for tile in tiles],
            ["2m", "3m", "4m", "5p", "6p", "7p", "2s", "2s", "F", "F", "F"],
        )

    def test_build_hand_analysis_uses_bundled_checkpoint(self) -> None:
        report = build_hand_analysis(
            hand="234m 567p 22s 6z 6z 6z",
            drawn="1m",
            seat=0,
            round_wind="E",
            dora=("5p",),
        )

        self.assertEqual(report["kind"], "kenjaku-hand-analysis-v0")
        self.assertEqual(report["model_kind"], "discard-linear-raw-count-v0")
        self.assertEqual(report["recommended_discard"], "1m")
        self.assertEqual(report["candidates"][0]["tile"], "1m")
        self.assertAlmostEqual(
            sum(candidate["policy_probability"] for candidate in report["candidates"]),
            1.0,
            places=5,
        )

    def test_analyze_hand_json_command(self) -> None:
        stdout = io.StringIO()

        with contextlib.redirect_stdout(stdout):
            exit_code = main(
                [
                    "analyze-hand",
                    "--hand",
                    "234m 567p 22s 666z",
                    "--drawn",
                    "1m",
                    "--seat",
                    "0",
                    "--round",
                    "E",
                    "--dora",
                    "5p",
                    "--output",
                    "json",
                ]
            )

        self.assertEqual(exit_code, 0)
        payload = json.loads(stdout.getvalue())
        self.assertEqual(payload["recommended_discard"], "1m")
        self.assertEqual(payload["dora_indicators"], ["5p"])

    def test_analyze_hand_text_golden_output(self) -> None:
        stdout = io.StringIO()

        with contextlib.redirect_stdout(stdout):
            exit_code = main(
                [
                    "analyze-hand",
                    "--hand",
                    "234m 567p 22s 6z 6z 6z",
                    "--drawn",
                    "1m",
                    "--seat",
                    "0",
                    "--round",
                    "E",
                    "--dora",
                    "5p",
                    "--output",
                    "text",
                ]
            )

        self.assertEqual(exit_code, 0)
        self.assertEqual(
            stdout.getvalue(),
            "\n".join(
                [
                    "hand: 2m 3m 4m 5p 6p 7p 2s 2s F F F 1m",
                    "seat: 0",
                    "round: E",
                    "current_shanten: 1",
                    "model: discard-linear-raw-count-v0",
                    "recommended_discard: 1m",
                    "candidates:",
                    "- 1m: p=0.3338 shanten=1 delta=0 risk=0.000 points=0 "
                    "reasons=preserves_shanten, no_active_riichi_opponent",
                    "- 4m: p=0.3338 shanten=1 delta=0 risk=0.000 points=0 "
                    "reasons=preserves_shanten, no_active_riichi_opponent",
                    "- 2m: p=0.0475 shanten=2 delta=1 risk=0.000 points=-1200 "
                    "reasons=worsens_shanten, no_active_riichi_opponent",
                    "- 2s: p=0.0475 shanten=2 delta=1 risk=0.000 points=-1200 "
                    "reasons=worsens_shanten, no_active_riichi_opponent",
                    "- 3m: p=0.0475 shanten=2 delta=1 risk=0.000 points=-1200 "
                    "reasons=worsens_shanten, no_active_riichi_opponent",
                    "- 5p: p=0.0475 shanten=2 delta=1 risk=0.000 points=-1200 "
                    "reasons=worsens_shanten, no_active_riichi_opponent",
                    "- 6p: p=0.0475 shanten=2 delta=1 risk=0.000 points=-1200 "
                    "reasons=worsens_shanten, no_active_riichi_opponent",
                    "- 7p: p=0.0475 shanten=2 delta=1 risk=0.000 points=-1200 "
                    "reasons=worsens_shanten, no_active_riichi_opponent",
                    "- F: p=0.0475 shanten=2 delta=1 risk=0.000 points=-1200 "
                    "reasons=worsens_shanten, no_active_riichi_opponent",
                    "disclaimer: Sandbox discard estimate from a fixture-scale linear checkpoint "
                    "plus heuristic overlay; not a calibrated engine-strength policy.",
                    "",
                ]
            ),
        )


if __name__ == "__main__":
    unittest.main()
