from __future__ import annotations

import importlib.util
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.training.population import (
    POPULATION_SANDBOX_REPORT_KIND,
    format_population_sandbox_report,
    train_population_sandbox,
)


@unittest.skipUnless(importlib.util.find_spec("torch") is not None, "PyTorch is not available")
class PopulationSandboxTests(unittest.TestCase):
    def test_population_trainer_maintains_pool_and_reports_matchups(self) -> None:
        with TemporaryDirectory() as directory:
            report = train_population_sandbox(
                pool_size=4,
                generations=1,
                candidates_per_generation=1,
                matchups_per_candidate=1,
                total_steps=8,
                max_rounds=1,
                max_turns_per_round=8,
                evaluation_max_rounds=1,
                evaluation_max_turns_per_round=8,
                seed="population-test",
                output_dir=directory,
            )
            artifacts = sorted(Path(directory).glob("*.json"))

        text = format_population_sandbox_report(report)

        self.assertEqual(report["kind"], POPULATION_SANDBOX_REPORT_KIND)
        self.assertEqual(report["pool_size"], 4)
        self.assertEqual(len(report["pool"]), 4)
        self.assertEqual(len(report["snapshots"]), 5)
        self.assertEqual(report["matchup_counts"]["total"], 5)
        self.assertEqual(len(report["promotion_decisions"]), 1)
        self.assertEqual(report["replacement_criteria"]["metric"], "average_score")
        self.assertEqual(len(artifacts), 5)
        for snapshot in report["pool"]:
            self.assertIn("win_rate", snapshot["metrics"])
            self.assertIn("average_placement", snapshot["metrics"])
            self.assertIn("deal_in_rate", snapshot["metrics"])
            self.assertIn("average_score", snapshot["metrics"])
        self.assertTrue(report["capabilities"]["policy_snapshot_pool"])
        self.assertTrue(report["capabilities"]["minimum_four_snapshots"])
        self.assertTrue(report["capabilities"]["opponent_sampling"])
        self.assertTrue(report["capabilities"]["matchup_metrics"])
        self.assertTrue(report["capabilities"]["promotion_criteria"])
        self.assertTrue(report["capabilities"]["checkpoint_artifacts"])
        self.assertFalse(report["capabilities"]["learned_policy_environment_integration"])
        self.assertIn("pool_size: 4", text)
        self.assertIn("promotion_decisions:", text)

    def test_population_trainer_validates_pool_size(self) -> None:
        with self.assertRaisesRegex(ValueError, "pool_size must be at least 4"):
            train_population_sandbox(
                pool_size=3,
                generations=1,
                candidates_per_generation=1,
                matchups_per_candidate=1,
                total_steps=1,
                max_rounds=1,
                max_turns_per_round=1,
                seed="bad",
            )


if __name__ == "__main__":
    unittest.main()
