from __future__ import annotations

import argparse
import unittest

from kenjaku.commands import COMMAND_MODULES

EXPECTED_COMMANDS = (
    "status",
    "repro-report",
    "demo",
    "browser-demo",
    "serve",
    "replay-intake-review",
    "replay-share-plan",
    "replay-public-summary",
    "replay-viewer",
    "self-play-sandbox",
    "self-play-match-sandbox",
    "train-ppo-sandbox",
    "train-population-sandbox",
    "training-dashboard",
    "inspect-tenhou",
    "tenhou-to-mjai",
    "bot",
    "defense-risk-summary",
    "safety-advisor",
    "benchmark-deal-in",
    "train-placement",
    "placement-probability",
    "analyze-hand",
    "export-decision-snapshots",
    "decision-snapshot-summary",
    "interpretability-overlay",
    "review-game",
    "transformer-attention-overlay",
    "feature-importance",
    "produce-decision-predictions",
    "predict",
    "decision-snapshot-compare",
    "external-baseline-report",
    "run-external-prediction-producer",
    "train-discard-baseline",
    "train-discard-linear",
    "train-discard-mlp",
    "train-discard-transformer",
    "export-bc-examples",
    "benchmark-discard-from-examples",
    "benchmark-discard",
    "benchmark-discard-mlp",
    "benchmark-discard-transformer",
    "benchmark-report-summary",
    "benchmark-dashboard",
    "disagreement-report-summary",
    "benchmark-call",
    "benchmark-call-from-examples",
    "benchmark-riichi",
    "benchmark-kita",
    "benchmark-riichi-from-examples",
)


class CommandRegistrationTests(unittest.TestCase):
    def test_command_modules_export_register_and_handle(self) -> None:
        for module in COMMAND_MODULES:
            with self.subTest(module=module.__name__):
                self.assertTrue(callable(module.register))
                self.assertTrue(callable(module.handle))

    def test_command_modules_register_all_commands_in_order(self) -> None:
        parser = argparse.ArgumentParser(prog="kenjaku")
        subparsers = parser.add_subparsers(dest="command")
        for module in COMMAND_MODULES:
            module.register(subparsers)

        self.assertEqual(tuple(subparsers.choices), EXPECTED_COMMANDS)


if __name__ == "__main__":
    unittest.main()
