from __future__ import annotations

import contextlib
import io
import json
import unittest

from kenjaku.cli import main
from kenjaku.safety_advisor import build_safety_advisor_report, parse_active_riichi


class SafetyAdvisorTests(unittest.TestCase):
    def test_parse_active_riichi_ignores_self(self) -> None:
        self.assertEqual(
            parse_active_riichi("0,1,3", seat=0),
            (False, True, False, True),
        )

    def test_build_report_ranks_safety_features(self) -> None:
        report = build_safety_advisor_report(
            hand="1m 7m 5p 5s E",
            river="1m 4m 4p 4p 4p 4p 4s 4s 4s",
            active_riichi="1",
        )
        by_tile = {
            candidate["tile"]: candidate
            for candidate in report["candidates"]
        }

        self.assertEqual(report["kind"], "kenjaku-safety-advisor-v0")
        self.assertEqual(report["candidates"][0]["tile"], "1m")
        self.assertTrue(by_tile["1m"]["genbutsu"])
        self.assertTrue(by_tile["7m"]["suji"])
        self.assertTrue(by_tile["5p"]["kabe"])
        self.assertTrue(by_tile["5s"]["one_chance"])
        self.assertLess(
            by_tile["1m"]["estimated_deal_in_risk"],
            by_tile["E"]["estimated_deal_in_risk"],
        )

    def test_safety_advisor_cli_json(self) -> None:
        stdout = io.StringIO()

        with contextlib.redirect_stdout(stdout):
            exit_code = main(
                [
                    "safety-advisor",
                    "--hand",
                    "1m 7m 5p 5s E",
                    "--river",
                    "1m 4m 4p 4p 4p 4p 4s 4s 4s",
                    "--active-riichi",
                    "1",
                    "--output",
                    "json",
                ]
            )
        payload = json.loads(stdout.getvalue())

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-safety-advisor-v0")
        self.assertEqual(payload["active_riichi_seats"], [1])
        self.assertEqual(payload["candidates"][0]["tile"], "1m")

    def test_safety_advisor_cli_text_table(self) -> None:
        stdout = io.StringIO()

        with contextlib.redirect_stdout(stdout):
            exit_code = main(
                [
                    "safety-advisor",
                    "--hand",
                    "1m 7m 5p 5s E",
                    "--river",
                    "1m 4m 4p 4p 4p 4p 4s 4s 4s",
                    "--active-riichi",
                    "1",
                    "--output",
                    "text",
                ]
            )
        output = stdout.getvalue()

        self.assertEqual(exit_code, 0)
        self.assertIn("candidates:", output)
        self.assertIn("- 1m: safety=", output)
        self.assertIn("reasons=genbutsu", output)


if __name__ == "__main__":
    unittest.main()
