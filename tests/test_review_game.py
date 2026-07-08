from __future__ import annotations

import contextlib
import io
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.cli import main
from kenjaku.core import TileType
from kenjaku.models import DiscardLinearModel
from kenjaku.models.linear_discard import RAW_COUNT_FEATURE_DIM, RAW_COUNT_FEATURE_PROFILE
from kenjaku.review_game import REVIEW_GAME_REPORT_KIND, build_review_game_report

FIXTURE_DIR = Path("data/fixtures/tenhou")


class ReviewGameTests(unittest.TestCase):
    def test_build_review_game_report_uses_frequency_policy(self) -> None:
        report = build_review_game_report(
            FIXTURE_DIR / "events_4p.xml",
            player=0,
            model="frequency",
        )

        self.assertEqual(report["kind"], REVIEW_GAME_REPORT_KIND)
        self.assertEqual(report["player"], 0)
        self.assertEqual(report["model"], "frequency")
        self.assertEqual(report["summary"]["rounds"], 1)
        self.assertGreaterEqual(report["summary"]["decisions"], 1)
        self.assertIn("expected_value_delta", report["summary"])
        self.assertIn("top_alternatives", report["decisions"][0])

    def test_review_game_cli_writes_html_for_all_fixtures(self) -> None:
        fixtures = sorted(FIXTURE_DIR.glob("*.xml"))
        self.assertEqual(len(fixtures), 3)

        with TemporaryDirectory() as directory:
            root = Path(directory)
            for fixture in fixtures:
                output = root / f"{fixture.stem}.html"
                stdout = io.StringIO()
                with contextlib.redirect_stdout(stdout):
                    exit_code = main(
                        [
                            "review-game",
                            str(fixture),
                            "--player",
                            "0",
                            "--model",
                            "frequency",
                            "--output",
                            str(output),
                        ]
                    )
                html = output.read_text(encoding="utf-8")
                self.assertEqual(exit_code, 0)
                self.assertIn("<!doctype html>", html)
                self.assertIn('id="review-data"', html)
                self.assertIn("Expected value delta", html)
                self.assertIn("decisions:", stdout.getvalue())

    def test_review_game_supports_linear_checkpoint(self) -> None:
        with TemporaryDirectory() as directory:
            checkpoint = Path(directory) / "discard-linear.json"
            weights = [[0.0] * RAW_COUNT_FEATURE_DIM for _ in range(34)]
            weights[TileType.parse("9m").index][0] = 3.0
            DiscardLinearModel(
                weights=tuple(tuple(row) for row in weights),
                epochs=0,
                learning_rate=0.1,
                feature_profile=RAW_COUNT_FEATURE_PROFILE,
            ).save(checkpoint)

            report = build_review_game_report(
                FIXTURE_DIR / "events_4p.xml",
                player=0,
                model=checkpoint,
            )

        self.assertTrue(str(report["model"]).startswith("linear-discard:"))
        self.assertGreaterEqual(report["summary"]["decisions"], 1)

    def test_review_game_missing_model_fails_gracefully(self) -> None:
        with self.assertRaisesRegex(SystemExit, "model checkpoint not found"):
            main(
                [
                    "review-game",
                    str(FIXTURE_DIR / "events_4p.xml"),
                    "--player",
                    "0",
                    "--model",
                    "missing-linear.json",
                    "--output",
                    "unused.html",
                ]
            )


if __name__ == "__main__":
    unittest.main()
