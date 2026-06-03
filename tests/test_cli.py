from __future__ import annotations

import contextlib
import io
import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.cli import main


class CliTests(unittest.TestCase):
    def test_version(self) -> None:
        stdout = io.StringIO()

        with contextlib.redirect_stdout(stdout):
            exit_code = main(["--version"])

        self.assertEqual(exit_code, 0)
        self.assertIn("kenjaku", stdout.getvalue())

    def test_inspect_tenhou_fixture(self) -> None:
        stdout = io.StringIO()

        with contextlib.redirect_stdout(stdout):
            exit_code = main(["inspect-tenhou", "data/fixtures/tenhou/minimal_4p.xml"])

        self.assertEqual(exit_code, 0)
        self.assertEqual(
            stdout.getvalue().splitlines(),
            ["rounds: 1", "discards: 2", "discard_examples: 2", "call_examples: 0"],
        )

    def test_inspect_tenhou_fixture_directory(self) -> None:
        stdout = io.StringIO()

        with contextlib.redirect_stdout(stdout):
            exit_code = main(["inspect-tenhou", "data/fixtures/tenhou"])

        self.assertEqual(exit_code, 0)
        self.assertEqual(
            stdout.getvalue().splitlines(),
            ["rounds: 3", "discards: 4", "discard_examples: 4", "call_examples: 1"],
        )

    def test_inspect_tenhou_writes_report_artifact(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "inspect.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "inspect-tenhou",
                        "data/fixtures/tenhou",
                        "--report",
                        str(report),
                        "--source-label",
                        "fixture-dir",
                        "--source-date",
                        "synthetic",
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-tenhou-inspect-report-v0")
        self.assertEqual(payload["source"]["label"], "fixture-dir")
        self.assertEqual(payload["source"]["date"], "synthetic")
        self.assertEqual(payload["xml_file_count"], 3)
        self.assertEqual(payload["rounds"], 3)
        self.assertEqual(payload["discards"], 4)
        self.assertEqual(payload["discard_examples"], 4)
        self.assertEqual(payload["call_examples"], 1)
        self.assertEqual(payload["discard_shanten"]["examples"], 4)
        self.assertIn("average_delta", payload["discard_shanten"])
        self.assertEqual(payload["parse_failures"]["count"], 0)
        self.assertIn("report_path:", stdout.getvalue())

    def test_inspect_tenhou_can_report_parse_failures(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            broken = Path(directory) / "broken.xml"
            broken.write_text("<mjloggm><INIT /></mjloggm>", encoding="utf-8")
            report = Path(directory) / "inspect.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "inspect-tenhou",
                        "data/fixtures/tenhou/minimal_4p.xml",
                        str(broken),
                        "--skip-errors",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertIn("parse_failures: 1", stdout.getvalue())
        self.assertEqual(payload["rounds"], 1)
        self.assertEqual(payload["parse_failures"]["count"], 1)
        self.assertEqual(payload["parse_failures"]["items"][0]["error_type"], "ValueError")

    def test_train_discard_baseline_fixture(self) -> None:
        stdout = io.StringIO()

        with contextlib.redirect_stdout(stdout):
            exit_code = main(["train-discard-baseline", "data/fixtures/tenhou/minimal_4p.xml"])

        self.assertEqual(exit_code, 0)
        self.assertEqual(
            stdout.getvalue().splitlines(),
            ["examples: 2", "top_discard: 4p", "training_accuracy: 0.5000"],
        )

    def test_train_discard_linear_fixture(self) -> None:
        stdout = io.StringIO()

        with contextlib.redirect_stdout(stdout):
            exit_code = main(
                [
                    "train-discard-linear",
                    "data/fixtures/tenhou/minimal_4p.xml",
                    "--epochs",
                    "5",
                    "--eval-fraction",
                    "0.5",
                ]
            )

        self.assertEqual(exit_code, 0)
        self.assertEqual(
            stdout.getvalue().splitlines(),
            [
                "examples: 2",
                "train_examples: 1",
                "eval_examples: 1",
                "train_accuracy: 1.0000",
                "eval_accuracy: 0.0000",
            ],
        )

    def test_train_discard_linear_writes_model_artifact(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            output = Path(directory) / "model.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "train-discard-linear",
                        "data/fixtures/tenhou/minimal_4p.xml",
                        "--epochs",
                        "1",
                        "--eval-fraction",
                        "0",
                        "--output",
                        str(output),
                    ]
                )
            artifact_exists = output.exists()

        self.assertEqual(exit_code, 0)
        self.assertTrue(artifact_exists)
        self.assertIn("model_path:", stdout.getvalue())

    def test_train_discard_linear_writes_report_artifact(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            output = Path(directory) / "model.json"
            report = Path(directory) / "reports" / "linear.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "train-discard-linear",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--output",
                        str(output),
                        "--report",
                        str(report),
                        "--source-label",
                        "fixture-train",
                        "--source-command",
                        "unit-test",
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-discard-linear-report-v0")
        self.assertEqual(payload["source"]["label"], "fixture-train")
        self.assertEqual(payload["source"]["command"], "unit-test")
        self.assertEqual(payload["xml_file_count"], 3)
        self.assertEqual(payload["rounds"], 3)
        self.assertEqual(payload["discard_examples"], 4)
        self.assertEqual(payload["call_examples"], 1)
        self.assertEqual(payload["split"]["seed"], "fixed")
        self.assertEqual(payload["split"]["train_examples"], 3)
        self.assertEqual(payload["split"]["eval_examples"], 1)
        self.assertEqual(payload["model"]["kind"], "discard-linear-v1")
        self.assertEqual(payload["model"]["feature_dim"], 76)
        self.assertEqual(payload["discard_shanten"]["examples"], 4)
        self.assertEqual(payload["parse_failures"]["count"], 0)
        self.assertEqual(payload["artifacts"]["model_path"], str(output))
        self.assertIn("report_path:", stdout.getvalue())

    def test_benchmark_discard_writes_report_artifact(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "benchmark.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-discard",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--report",
                        str(report),
                        "--source-label",
                        "fixture-benchmark",
                        "--source-command",
                        "unit-test",
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(
            stdout.getvalue().splitlines()[:10],
            [
                "examples: 4",
                "train_examples: 3",
                "eval_examples: 1",
                "frequency_train_accuracy: 0.6667",
                "frequency_eval_accuracy: 0.0000",
                "raw_count_linear_train_accuracy: 0.6667",
                "raw_count_linear_eval_accuracy: 0.0000",
                "linear_train_accuracy: 0.6667",
                "linear_eval_accuracy: 0.0000",
                "linear_eval_lift_over_raw_count: +0.0000",
            ],
        )
        self.assertIn("report_path:", stdout.getvalue())
        self.assertEqual(payload["kind"], "kenjaku-discard-benchmark-report-v0")
        self.assertEqual(payload["source"]["label"], "fixture-benchmark")
        self.assertEqual(payload["source"]["command"], "unit-test")
        self.assertEqual(payload["xml_file_count"], 3)
        self.assertEqual(payload["rounds"], 3)
        self.assertEqual(payload["discard_examples"], 4)
        self.assertEqual(payload["call_examples"], 1)
        self.assertEqual(payload["split"]["seed"], "fixed")
        self.assertEqual(payload["split"]["train_examples"], 3)
        self.assertEqual(payload["split"]["eval_examples"], 1)
        self.assertEqual(payload["models"]["frequency"]["metrics"]["train_accuracy"], 2 / 3)
        self.assertEqual(payload["models"]["frequency"]["metrics"]["eval_accuracy"], 0.0)
        self.assertEqual(
            payload["models"]["raw_count_linear"]["kind"],
            "discard-linear-raw-count-v0",
        )
        self.assertEqual(payload["models"]["raw_count_linear"]["feature_dim"], 69)
        self.assertEqual(
            payload["models"]["raw_count_linear"]["metrics"]["train_accuracy"],
            2 / 3,
        )
        self.assertEqual(payload["models"]["raw_count_linear"]["metrics"]["eval_accuracy"], 0.0)
        self.assertEqual(payload["models"]["linear"]["kind"], "discard-linear-v1")
        self.assertEqual(payload["models"]["linear"]["feature_dim"], 76)
        self.assertEqual(payload["models"]["linear"]["training"]["epochs"], 1)
        self.assertEqual(payload["models"]["linear"]["metrics"]["train_accuracy"], 2 / 3)
        self.assertEqual(payload["models"]["linear"]["metrics"]["eval_accuracy"], 0.0)
        self.assertEqual(payload["ablation"]["train_accuracy_lift_over_raw_count"], 0.0)
        self.assertEqual(payload["ablation"]["eval_accuracy_lift_over_raw_count"], 0.0)
        self.assertEqual(payload["discard_shanten"]["examples"], 4)
        self.assertEqual(payload["parse_failures"]["count"], 0)


if __name__ == "__main__":
    unittest.main()
