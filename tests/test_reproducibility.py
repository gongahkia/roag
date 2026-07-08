from __future__ import annotations

import contextlib
import inspect
import io
import json
import random
import unittest
import warnings
from argparse import ArgumentParser, Namespace
from collections.abc import Callable
from pathlib import Path
from tempfile import TemporaryDirectory
from typing import cast
from unittest import mock

from kenjaku import cli
from kenjaku.reproducibility import pin_seeds


class ReproducibilityTests(unittest.TestCase):
    def test_pin_seeds_reseeds_python_random(self) -> None:
        with warnings.catch_warnings():
            warnings.simplefilter("ignore", RuntimeWarning)
            pin_seeds("fixed")
            first = [random.random() for _ in range(5)]
            pin_seeds("fixed")
            second = [random.random() for _ in range(5)]

        self.assertEqual(first, second)

    def test_global_seed_applies_before_subcommand(self) -> None:
        stdout = io.StringIO()
        with (
            mock.patch("kenjaku.cli.pin_seeds") as pin,
            contextlib.redirect_stdout(stdout),
        ):
            exit_code = cli.main(["--global-seed", "root-seed", "status", "--json"])

        self.assertEqual(exit_code, 0)
        pin.assert_called_once_with("root-seed")
        self.assertEqual(json.loads(stdout.getvalue())["kind"], "kenjaku-status-v0")

    def test_every_seed_command_pins_handler_entry(self) -> None:
        parser = cli.build_parser()
        subparsers = next(action for action in parser._actions if action.dest == "command")
        choices = cast(dict[str, ArgumentParser], subparsers.choices)
        seed_commands: dict[str, Callable[[Namespace], int]] = {}
        for command, subparser in choices.items():
            if any("--seed" in action.option_strings for action in subparser._actions):
                seed_commands[command] = cast(
                    Callable[[Namespace], int],
                    subparser.get_default("func"),
                )

        self.assertEqual(
            set(seed_commands),
            {
                "benchmark-discard-mlp",
                "benchmark-discard-transformer",
                "self-play-match-sandbox",
                "self-play-sandbox",
                "train-discard-mlp",
                "train-discard-transformer",
                "train-population-sandbox",
                "train-ppo-sandbox",
            },
        )
        for command, handler in seed_commands.items():
            source = inspect.getsource(handler)
            self.assertIn("pin_seeds(args.seed)", source, command)

    def test_benchmark_discard_same_seed_writes_identical_json(self) -> None:
        fixed_provenance = {
            "git_commit": "fixed",
            "git_dirty": False,
            "kenjaku_version": "0.test",
            "python_version": "3.test",
            "argv": ["kenjaku", "benchmark-discard"],
            "started_at": "2026-07-08T00:00:00Z",
            "duration_seconds": 0.0,
        }
        with TemporaryDirectory() as directory:
            report_a = Path(directory) / "a.json"
            report_b = Path(directory) / "b.json"
            base_args = [
                "--global-seed",
                "same",
                "benchmark-discard",
                "data/fixtures/tenhou",
                "--models",
                "fast",
                "--epochs",
                "1",
                "--eval-fraction",
                "0.25",
                "--split-seed",
                "same",
            ]
            with (
                warnings.catch_warnings(),
                mock.patch(
                    "kenjaku.experiments.build_report_provenance",
                    return_value=fixed_provenance,
                ),
            ):
                warnings.simplefilter("ignore", RuntimeWarning)
                with contextlib.redirect_stdout(io.StringIO()):
                    first_exit = cli.main([*base_args, "--report", str(report_a)])
                    second_exit = cli.main([*base_args, "--report", str(report_b)])

            first = report_a.read_text(encoding="utf-8")
            second = report_b.read_text(encoding="utf-8")

        self.assertEqual(first_exit, 0)
        self.assertEqual(second_exit, 0)
        self.assertEqual(first, second)
