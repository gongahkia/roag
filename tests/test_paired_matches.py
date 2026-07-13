from __future__ import annotations

import unittest

from kenjaku.commands._legacy import build_parser
from kenjaku.simulation import (
    PAIRED_SEED_MATCH_3P_REPORT_KIND,
    PAIRED_SEED_MATCH_4P_REPORT_KIND,
    PairedMatchPolicy,
    run_paired_seed_matches_3p,
    run_paired_seed_matches_4p,
)


class PairedSeedMatch4pTests(unittest.TestCase):
    def test_runs_identical_profiles_on_identical_seed_pairs(self) -> None:
        report = run_paired_seed_matches_4p(
            pairs=1,
            seed="paired-fixed",
            max_rounds=12,
            max_turns_per_round=512,
            candidate=PairedMatchPolicy(),
            baseline=PairedMatchPolicy(),
        )
        row = report["rows"][0]

        self.assertEqual(report["kind"], PAIRED_SEED_MATCH_4P_REPORT_KIND)
        self.assertEqual(report["ruleset"], "tenhou-4p")
        self.assertEqual(report["players"], 4)
        self.assertEqual(row["candidate"], row["baseline"])
        self.assertEqual(report["summary"]["completed_pairs"], 1)
        self.assertEqual(len(row["placement_adjusted_score_delta_by_seat"]), 4)
        self.assertEqual(row["placement_adjusted_score_delta_by_seat"], [0.0, 0.0, 0.0, 0.0])

    def test_validates_4p_profile_and_parser(self) -> None:
        with self.assertRaisesRegex(ValueError, "4p seat"):
            run_paired_seed_matches_4p(
                pairs=1,
                seed="bad-seat",
                max_rounds=1,
                max_turns_per_round=1,
                candidate=PairedMatchPolicy(heuristic_seats=(4,)),
                baseline=PairedMatchPolicy(),
            )
        with self.assertRaisesRegex(ValueError, "pairs must be positive"):
            run_paired_seed_matches_4p(
                pairs=0,
                seed="bad",
                max_rounds=1,
                max_turns_per_round=1,
                candidate=PairedMatchPolicy(),
                baseline=PairedMatchPolicy(),
            )
        args = build_parser().parse_args(
            ["paired-match-4p", "--pairs", "2", "--candidate-heuristic-seats", "1,3"]
        )

        self.assertEqual(args.pairs, 2)
        self.assertEqual(args.candidate_heuristic_seats, "1,3")


class PairedSeedMatch3pTests(unittest.TestCase):
    def test_runs_identical_profiles_on_identical_seed_pairs(self) -> None:
        report = run_paired_seed_matches_3p(
            pairs=1,
            seed="paired-sanma-fixed",
            max_rounds=12,
            max_turns_per_round=512,
            candidate=PairedMatchPolicy(),
            baseline=PairedMatchPolicy(),
        )
        row = report["rows"][0]

        self.assertEqual(report["kind"], PAIRED_SEED_MATCH_3P_REPORT_KIND)
        self.assertEqual(report["ruleset"], "tenhou-3p")
        self.assertEqual(report["players"], 3)
        self.assertEqual(row["candidate"], row["baseline"])
        self.assertEqual(report["summary"]["completed_pairs"], 1)
        self.assertEqual(len(row["placement_adjusted_score_delta_by_seat"]), 3)
        self.assertEqual(row["placement_adjusted_score_delta_by_seat"], [0.0, 0.0, 0.0])

    def test_validates_3p_profile_and_parser(self) -> None:
        with self.assertRaisesRegex(ValueError, "3p seat"):
            run_paired_seed_matches_3p(
                pairs=1,
                seed="bad-sanma-seat",
                max_rounds=1,
                max_turns_per_round=1,
                candidate=PairedMatchPolicy(heuristic_seats=(3,)),
                baseline=PairedMatchPolicy(),
            )
        args = build_parser().parse_args(
            ["paired-match-3p", "--pairs", "2", "--candidate-heuristic-seats", "1,2"]
        )

        self.assertEqual(args.pairs, 2)
        self.assertEqual(args.candidate_heuristic_seats, "1,2")


if __name__ == "__main__":
    unittest.main()
