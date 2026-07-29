from __future__ import annotations

import unittest

from kenjaku.ablation_benchmark import build_policy_heuristic_ablation_benchmark
from kenjaku.cli import build_parser
from tests.test_behavior_distillation import _manifest


class AblationBenchmarkTests(unittest.TestCase):
    def test_reports_full_and_each_evaluator_factor_ablation(self) -> None:
        report = build_policy_heuristic_ablation_benchmark(_manifest())

        self.assertEqual(report["ruleset"], "tenhou-4p")
        self.assertEqual(report["rows"]["full"]["examples"], 1)
        self.assertEqual(
            set(report["agreement_delta_from_full"]),
            {
                "without_shanten_ukeire",
                "without_hand_value",
                "without_defense_risk",
                "without_placement_endgame",
            },
        )

    def test_registers_manifest_benchmark_command(self) -> None:
        args = build_parser().parse_args(["benchmark-policy-heuristic-ablation", "input.json"])

        self.assertEqual(args.command, "benchmark-policy-heuristic-ablation")
        self.assertEqual(args.manifest.name, "input.json")


if __name__ == "__main__":
    unittest.main()
