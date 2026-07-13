from __future__ import annotations

import argparse
import unittest
from pathlib import Path

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
    "paired-match-4p",
    "paired-match-3p",
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
    "benchmark-policy-heuristic-ablation",
    "benchmark-policy-device",
    "select-model-frozen-split",
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

    def test_bot_parser_is_owned_by_the_bot_command_module(self) -> None:
        parser = argparse.ArgumentParser(prog="kenjaku")
        subparsers = parser.add_subparsers(dest="command")
        from kenjaku.commands import bot

        bot.register(subparsers)
        args = parser.parse_args(["bot", "--player-id", "0"])

        self.assertEqual(args.policy, "frequency")
        self.assertEqual(args.policy_type, "auto")
        self.assertEqual(args.device, "cpu")
        legacy_source = Path("src/kenjaku/commands/_legacy.py").read_text(encoding="utf-8")
        self.assertNotIn('subparsers.add_parser(\n        "bot"', legacy_source)


if __name__ == "__main__":
    unittest.main()
