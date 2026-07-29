from __future__ import annotations

import unittest

from kenjaku.commands._legacy import build_parser
from kenjaku.simulation import (
    FIXED_HEURISTIC_BASELINE_KIND,
    FIXED_HEURISTIC_BASELINE_VERSION,
    PAIRED_MATCH_CHECKPOINT_PROMOTION_GATE_KIND,
    PAIRED_SEED_MATCH_3P_REPORT_KIND,
    PAIRED_SEED_MATCH_4P_REPORT_KIND,
    PairedMatchPolicy,
    checkpoint_promotion_gate,
    fixed_heuristic_baseline_manifest,
    fixed_heuristic_baseline_policy,
    paired_matches,
    run_paired_seed_matches_3p,
    run_paired_seed_matches_4p,
)


class PairedSeedMatch4pTests(unittest.TestCase):
    def test_runs_identical_profiles_on_identical_seed_pairs(self) -> None:
        report = run_paired_seed_matches_4p(
            pairs=1,
            seed="paired-fixed",
            max_rounds=1,
            max_turns_per_round=1,
            candidate=fixed_heuristic_baseline_policy(4),
            baseline=fixed_heuristic_baseline_policy(4),
        )
        row = report["rows"][0]

        self.assertEqual(report["kind"], PAIRED_SEED_MATCH_4P_REPORT_KIND)
        self.assertEqual(report["ruleset"], "tenhou-4p")
        self.assertEqual(report["players"], 4)
        self.assertEqual(
            report["heuristic_baseline"],
            fixed_heuristic_baseline_manifest(4),
        )
        self.assertEqual(report["heuristic_baseline"]["kind"], FIXED_HEURISTIC_BASELINE_KIND)
        self.assertEqual(report["heuristic_baseline"]["version"], FIXED_HEURISTIC_BASELINE_VERSION)
        self.assertEqual(row["candidate"], row["baseline"])
        self.assertEqual(report["summary"]["completed_pairs"], 0)
        self.assertIsNone(row["placement_adjusted_score_delta_by_seat"])
        self.assertIsNone(report["summary"]["placement_adjusted_score_delta_ci_by_seat"])
        self.assertEqual(
            report["checkpoint_promotion"],
            {
                "kind": PAIRED_MATCH_CHECKPOINT_PROMOTION_GATE_KIND,
                "paired_report_kind": PAIRED_SEED_MATCH_4P_REPORT_KIND,
                "ruleset": "tenhou-4p",
                "metric": "placement_adjusted_score_delta",
                "evaluation_seat": 0,
                "confidence_interval": None,
                "allowed": False,
                "reason": "no_completed_paired_matches",
            },
        )

    def test_validates_4p_profile_and_parser(self) -> None:
        with self.assertRaisesRegex(ValueError, "4p seat"):
            run_paired_seed_matches_4p(
                pairs=1,
                seed="bad-seat",
                max_rounds=1,
                max_turns_per_round=1,
                candidate=PairedMatchPolicy(heuristic_seats=(4,)),
                baseline=fixed_heuristic_baseline_policy(4),
            )
        with self.assertRaisesRegex(ValueError, "pairs must be positive"):
            run_paired_seed_matches_4p(
                pairs=0,
                seed="bad",
                max_rounds=1,
                max_turns_per_round=1,
                candidate=PairedMatchPolicy(),
                baseline=fixed_heuristic_baseline_policy(4),
            )
        with self.assertRaisesRegex(ValueError, "bootstrap_resamples must be positive"):
            run_paired_seed_matches_4p(
                pairs=1,
                seed="bad-bootstrap",
                max_rounds=1,
                max_turns_per_round=1,
                candidate=PairedMatchPolicy(),
                baseline=fixed_heuristic_baseline_policy(4),
                bootstrap_resamples=0,
            )
        with self.assertRaisesRegex(ValueError, "promotion_seat must be a 4p seat index"):
            run_paired_seed_matches_4p(
                pairs=1,
                seed="bad-promotion-seat",
                max_rounds=1,
                max_turns_per_round=1,
                candidate=PairedMatchPolicy(),
                baseline=fixed_heuristic_baseline_policy(4),
                promotion_seat=4,
            )
        args = build_parser().parse_args(
            [
                "paired-match-4p",
                "--pairs",
                "2",
                "--candidate-heuristic-seats",
                "1,3",
                "--bootstrap-resamples",
                "17",
                "--promotion-seat",
                "1",
            ]
        )

        self.assertEqual(args.pairs, 2)
        self.assertEqual(args.candidate_heuristic_seats, "1,3")
        self.assertEqual(args.bootstrap_resamples, 17)
        self.assertEqual(args.promotion_seat, 1)
        self.assertEqual(args.baseline_heuristic_seats, "0,1,2,3")


class PairedSeedMatch3pTests(unittest.TestCase):
    def test_runs_identical_profiles_on_identical_seed_pairs(self) -> None:
        report = run_paired_seed_matches_3p(
            pairs=1,
            seed="paired-sanma-fixed",
            max_rounds=1,
            max_turns_per_round=1,
            candidate=fixed_heuristic_baseline_policy(3),
            baseline=fixed_heuristic_baseline_policy(3),
            bootstrap_resamples=17,
        )
        row = report["rows"][0]

        self.assertEqual(report["kind"], PAIRED_SEED_MATCH_3P_REPORT_KIND)
        self.assertEqual(report["ruleset"], "tenhou-3p")
        self.assertEqual(report["players"], 3)
        self.assertEqual(report["heuristic_baseline"], fixed_heuristic_baseline_manifest(3))
        self.assertEqual(row["candidate"], row["baseline"])
        self.assertEqual(report["summary"]["completed_pairs"], 0)
        self.assertIsNone(row["placement_adjusted_score_delta_by_seat"])
        self.assertIsNone(report["summary"]["placement_adjusted_score_delta_ci_by_seat"])

    def test_validates_3p_profile_and_parser(self) -> None:
        with self.assertRaisesRegex(ValueError, "3p seat"):
            run_paired_seed_matches_3p(
                pairs=1,
                seed="bad-sanma-seat",
                max_rounds=1,
                max_turns_per_round=1,
                candidate=PairedMatchPolicy(heuristic_seats=(3,)),
                baseline=fixed_heuristic_baseline_policy(3),
            )
        args = build_parser().parse_args(
            [
                "paired-match-3p",
                "--pairs",
                "2",
                "--candidate-heuristic-seats",
                "1,2",
                "--bootstrap-resamples",
                "17",
                "--promotion-seat",
                "1",
            ]
        )

        self.assertEqual(args.pairs, 2)
        self.assertEqual(args.candidate_heuristic_seats, "1,2")
        self.assertEqual(args.bootstrap_resamples, 17)
        self.assertEqual(args.promotion_seat, 1)
        self.assertEqual(args.baseline_heuristic_seats, "0,1,2")


class PairedBootstrapTests(unittest.TestCase):
    def test_resampling_is_seeded_and_preserves_per_seat_intervals(self) -> None:
        rows = [
            {"placement_adjusted_score_delta_by_seat": [10.0, -10.0, 0.0]},
            {"placement_adjusted_score_delta_by_seat": [0.0, 0.0, 0.0]},
            {"placement_adjusted_score_delta_by_seat": [-10.0, 10.0, 0.0]},
        ]
        first = paired_matches._bootstrap_score_delta_confidence_intervals(
            rows,
            players=3,
            seed="bootstrap-fixed",
            resamples=101,
        )
        second = paired_matches._bootstrap_score_delta_confidence_intervals(
            rows,
            players=3,
            seed="bootstrap-fixed",
            resamples=101,
        )

        self.assertEqual(first, second)
        self.assertEqual(first[0]["level"], 0.95)
        self.assertEqual(first[0]["method"], "paired-bootstrap-percentile")
        self.assertEqual(first[0]["resamples"], 101)
        self.assertLess(first[0]["low"], 0.0)
        self.assertGreater(first[0]["high"], 0.0)
        self.assertEqual(first[2]["low"], 0.0)
        self.assertEqual(first[2]["high"], 0.0)


class CheckpointPromotionGateTests(unittest.TestCase):
    def test_allows_only_strictly_positive_95_percent_ci(self) -> None:
        report = {
            "kind": PAIRED_SEED_MATCH_3P_REPORT_KIND,
            "ruleset": "tenhou-3p",
            "players": 3,
            "heuristic_baseline": fixed_heuristic_baseline_manifest(3),
            "summary": {
                "placement_adjusted_score_delta_ci_by_seat": [
                    {
                        "level": 0.95,
                        "method": "paired-bootstrap-percentile",
                        "resamples": 17,
                        "low": 0.1,
                        "high": 0.5,
                    },
                    {
                        "level": 0.95,
                        "method": "paired-bootstrap-percentile",
                        "resamples": 17,
                        "low": 0.0,
                        "high": 0.2,
                    },
                    {
                        "level": 0.95,
                        "method": "paired-bootstrap-percentile",
                        "resamples": 17,
                        "low": -0.1,
                        "high": 0.3,
                    },
                ]
            },
        }

        allowed = checkpoint_promotion_gate(report, evaluation_seat=0)
        zero_bound = checkpoint_promotion_gate(report, evaluation_seat=1)

        self.assertTrue(allowed["allowed"])
        self.assertEqual(allowed["reason"], "paired_95_percent_ci_strictly_positive")
        self.assertFalse(zero_bound["allowed"])
        self.assertEqual(zero_bound["reason"], "paired_95_percent_ci_not_strictly_positive")

    def test_rejects_incomplete_or_invalid_reports(self) -> None:
        incomplete = {
            "kind": PAIRED_SEED_MATCH_3P_REPORT_KIND,
            "ruleset": "tenhou-3p",
            "players": 3,
            "heuristic_baseline": fixed_heuristic_baseline_manifest(3),
            "summary": {"placement_adjusted_score_delta_ci_by_seat": None},
        }

        gate = checkpoint_promotion_gate(incomplete)

        self.assertFalse(gate["allowed"])
        self.assertEqual(gate["reason"], "no_completed_paired_matches")
        with self.assertRaisesRegex(ValueError, "95% confidence interval"):
            checkpoint_promotion_gate(
                {
                    **incomplete,
                    "summary": {
                        "placement_adjusted_score_delta_ci_by_seat": [
                            {
                                "level": 0.9,
                                "method": "paired-bootstrap-percentile",
                                "resamples": 17,
                                "low": 0.1,
                                "high": 0.2,
                            }
                        ]
                        * 3
                    },
                }
            )

    def test_rejects_unpinned_heuristic_baseline(self) -> None:
        report = {
            "kind": PAIRED_SEED_MATCH_3P_REPORT_KIND,
            "ruleset": "tenhou-3p",
            "players": 3,
            "heuristic_baseline": {"version": "untracked"},
            "summary": {"placement_adjusted_score_delta_ci_by_seat": None},
        }

        gate = checkpoint_promotion_gate(report)

        self.assertFalse(gate["allowed"])
        self.assertEqual(gate["reason"], "fixed_heuristic_baseline_mismatch")


class FixedHeuristicBaselineTests(unittest.TestCase):
    def test_requires_versioned_all_seat_baseline(self) -> None:
        self.assertEqual(
            fixed_heuristic_baseline_policy(3).heuristic_seats,
            (0, 1, 2),
        )
        self.assertEqual(fixed_heuristic_baseline_manifest(4), fixed_heuristic_baseline_manifest(4))
        with self.assertRaisesRegex(ValueError, "fixed heuristic baseline"):
            run_paired_seed_matches_3p(
                pairs=1,
                seed="custom-baseline",
                max_rounds=1,
                max_turns_per_round=1,
                candidate=PairedMatchPolicy(),
                baseline=PairedMatchPolicy(),
            )


if __name__ == "__main__":
    unittest.main()
