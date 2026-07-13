from __future__ import annotations

import unittest

from kenjaku.training.rollback_guard import (
    RL_ROLLBACK_GUARD_KIND,
    assess_rl_rollback_guard,
)


class RlRollbackGuardTests(unittest.TestCase):
    def test_keeps_candidate_only_with_equal_conformance_and_promotion(self) -> None:
        report = assess_rl_rollback_guard(
            baseline_checkpoint="distilled-v1",
            candidate_checkpoint="rl-v1",
            baseline_conformance={"ruleset": "tenhou-3p", "cases": 20, "passed": 20},
            candidate_conformance={"ruleset": "tenhou-3p", "cases": 20, "passed": 20},
            promotion_gate={"allowed": True, "reason": "paired_95_percent_ci_strictly_positive"},
        )

        self.assertEqual(report["kind"], RL_ROLLBACK_GUARD_KIND)
        self.assertFalse(report["rollback_required"])
        self.assertEqual(report["action"], "keep_candidate")
        self.assertEqual(report["reasons"], [])

    def test_rolls_back_on_conformance_or_promotion_regression(self) -> None:
        report = assess_rl_rollback_guard(
            baseline_checkpoint="distilled-v1",
            candidate_checkpoint="rl-v1",
            baseline_conformance={"ruleset": "tenhou-4p", "cases": 20, "passed": 20},
            candidate_conformance={"ruleset": "tenhou-4p", "cases": 20, "passed": 19},
            promotion_gate={
                "allowed": False,
                "reason": "paired_95_percent_ci_not_strictly_positive",
            },
        )

        self.assertTrue(report["rollback_required"])
        self.assertEqual(report["action"], "rollback_to_baseline")
        self.assertEqual(
            report["reasons"],
            ["rule_conformance_regressed", "promotion_gate_rejected"],
        )

    def test_rejects_unmatched_or_incomplete_evidence(self) -> None:
        with self.assertRaisesRegex(ValueError, "case counts"):
            assess_rl_rollback_guard(
                baseline_checkpoint="distilled-v1",
                candidate_checkpoint="rl-v1",
                baseline_conformance={"ruleset": "tenhou-3p", "cases": 20, "passed": 20},
                candidate_conformance={"ruleset": "tenhou-3p", "cases": 19, "passed": 19},
                promotion_gate={"allowed": True},
            )
        with self.assertRaisesRegex(ValueError, "allowed"):
            assess_rl_rollback_guard(
                baseline_checkpoint="distilled-v1",
                candidate_checkpoint="rl-v1",
                baseline_conformance={"ruleset": "tenhou-3p", "cases": 20, "passed": 20},
                candidate_conformance={"ruleset": "tenhou-3p", "cases": 20, "passed": 20},
                promotion_gate={},
            )


if __name__ == "__main__":
    unittest.main()
