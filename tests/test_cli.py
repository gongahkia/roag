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
                        "--l2",
                        "0.001",
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
        self.assertEqual(payload["training"]["l2"], 0.001)
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
            stdout.getvalue().splitlines()[:19],
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
                "risk_context_linear_train_accuracy: 0.6667",
                "risk_context_linear_eval_accuracy: 0.0000",
                "risk_context_linear_eval_lift_over_linear: +0.0000",
                "defense_context_linear_train_accuracy: 0.6667",
                "defense_context_linear_eval_accuracy: 0.0000",
                "defense_context_linear_eval_lift_over_risk_context: +0.0000",
                "defense_context_v1_linear_train_accuracy: 0.6667",
                "defense_context_v1_linear_eval_accuracy: 0.0000",
                "defense_context_v1_linear_eval_lift_over_defense_context: +0.0000",
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
        self.assertEqual(payload["models"]["frequency"]["eval_analysis"]["overall"]["examples"], 1)
        self.assertEqual(payload["models"]["frequency"]["eval_analysis"]["overall"]["correct"], 0)
        self.assertEqual(
            payload["models"]["raw_count_linear"]["kind"],
            "discard-linear-raw-count-v0",
        )
        self.assertEqual(payload["models"]["raw_count_linear"]["feature_dim"], 69)
        self.assertEqual(payload["models"]["raw_count_linear"]["training"]["l2"], 0.0)
        self.assertEqual(
            payload["models"]["raw_count_linear"]["metrics"]["train_accuracy"],
            2 / 3,
        )
        self.assertIn("weight_summary", payload["models"]["raw_count_linear"])
        self.assertIn("feature_summary", payload["models"]["raw_count_linear"])
        self.assertEqual(
            payload["models"]["raw_count_linear"]["feature_summary"]["feature_count"],
            69,
        )
        self.assertEqual(payload["models"]["raw_count_linear"]["metrics"]["eval_accuracy"], 0.0)
        self.assertEqual(
            payload["models"]["raw_count_linear"]["eval_analysis"]["overall"]["examples"],
            1,
        )
        self.assertEqual(payload["models"]["linear"]["kind"], "discard-linear-v1")
        self.assertEqual(payload["models"]["linear"]["feature_dim"], 76)
        self.assertEqual(payload["models"]["linear"]["training"]["epochs"], 1)
        self.assertEqual(payload["models"]["linear"]["training"]["l2"], 0.0)
        self.assertEqual(payload["models"]["linear"]["metrics"]["train_accuracy"], 2 / 3)
        self.assertEqual(payload["models"]["linear"]["metrics"]["eval_accuracy"], 0.0)
        self.assertEqual(payload["models"]["linear"]["eval_analysis"]["overall"]["examples"], 1)
        self.assertIn("by_shanten_delta", payload["models"]["linear"]["eval_analysis"])
        self.assertIn("by_tile_family", payload["models"]["linear"]["eval_analysis"])
        self.assertIn("by_round_event_phase", payload["models"]["linear"]["eval_analysis"])
        self.assertIn("by_seat_turn_phase", payload["models"]["linear"]["eval_analysis"])
        self.assertEqual(
            payload["models"]["risk_context_linear"]["kind"],
            "discard-linear-risk-context-v0",
        )
        self.assertEqual(payload["models"]["risk_context_linear"]["feature_dim"], 86)
        self.assertEqual(payload["models"]["risk_context_linear"]["training"]["epochs"], 1)
        self.assertEqual(payload["models"]["risk_context_linear"]["training"]["l2"], 0.0)
        self.assertEqual(
            payload["models"]["risk_context_linear"]["metrics"]["train_accuracy"],
            2 / 3,
        )
        self.assertEqual(
            payload["models"]["risk_context_linear"]["metrics"]["eval_accuracy"],
            0.0,
        )
        self.assertEqual(
            payload["models"]["risk_context_linear"]["eval_analysis"]["overall"]["examples"],
            1,
        )
        self.assertIn(
            "by_seat_turn_phase",
            payload["models"]["risk_context_linear"]["eval_analysis"],
        )
        self.assertEqual(
            payload["models"]["defense_context_linear"]["kind"],
            "discard-linear-defense-context-v0",
        )
        self.assertEqual(payload["models"]["defense_context_linear"]["feature_dim"], 98)
        self.assertEqual(payload["models"]["defense_context_linear"]["training"]["epochs"], 1)
        self.assertEqual(payload["models"]["defense_context_linear"]["training"]["l2"], 0.0)
        self.assertEqual(
            payload["models"]["defense_context_linear"]["metrics"]["train_accuracy"],
            2 / 3,
        )
        self.assertEqual(
            payload["models"]["defense_context_linear"]["metrics"]["eval_accuracy"],
            0.0,
        )
        self.assertEqual(
            payload["models"]["defense_context_linear"]["eval_analysis"]["overall"]["examples"],
            1,
        )
        self.assertIn(
            "by_active_opponent_riichi",
            payload["models"]["defense_context_linear"]["eval_analysis"],
        )
        self.assertIn(
            "by_actual_discard_genbutsu",
            payload["models"]["defense_context_linear"]["eval_analysis"],
        )
        self.assertIn(
            "by_actual_discard_suji",
            payload["models"]["defense_context_linear"]["eval_analysis"],
        )
        self.assertEqual(
            payload["models"]["defense_context_v1_linear"]["kind"],
            "discard-linear-defense-context-v1",
        )
        self.assertEqual(payload["models"]["defense_context_v1_linear"]["feature_dim"], 112)
        self.assertEqual(payload["models"]["defense_context_v1_linear"]["training"]["epochs"], 1)
        self.assertEqual(payload["models"]["defense_context_v1_linear"]["training"]["l2"], 0.0)
        self.assertEqual(
            payload["models"]["defense_context_v1_linear"]["feature_summary"]["feature_count"],
            112,
        )
        self.assertEqual(
            payload["models"]["defense_context_v1_linear"]["metrics"]["train_accuracy"],
            2 / 3,
        )
        self.assertEqual(
            payload["models"]["defense_context_v1_linear"]["metrics"]["eval_accuracy"],
            0.0,
        )
        self.assertIn(
            "by_actual_discard_seen_after_riichi",
            payload["models"]["defense_context_v1_linear"]["eval_analysis"],
        )
        self.assertEqual(payload["ablation"]["train_accuracy_lift_over_raw_count"], 0.0)
        self.assertEqual(payload["ablation"]["eval_accuracy_lift_over_raw_count"], 0.0)
        self.assertEqual(payload["ablation"]["risk_context_train_accuracy_lift_over_linear"], 0.0)
        self.assertEqual(payload["ablation"]["risk_context_eval_accuracy_lift_over_linear"], 0.0)
        self.assertEqual(
            payload["ablation"]["defense_context_train_accuracy_lift_over_risk_context"],
            0.0,
        )
        self.assertEqual(
            payload["ablation"]["defense_context_eval_accuracy_lift_over_risk_context"],
            0.0,
        )
        self.assertEqual(
            payload["ablation"]["defense_context_v1_train_accuracy_lift_over_defense_context"],
            0.0,
        )
        self.assertEqual(
            payload["ablation"]["defense_context_v1_eval_accuracy_lift_over_defense_context"],
            0.0,
        )
        self.assertEqual(payload["discard_shanten"]["examples"], 4)
        self.assertEqual(payload["parse_failures"]["count"], 0)

    def test_benchmark_report_summary_outputs_text_and_json(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "benchmark.json"
            with contextlib.redirect_stdout(io.StringIO()):
                main(
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
                    ]
                )

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["benchmark-report-summary", str(report)])

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["benchmark-report-summary", str(report), "--json"])
            payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("selected_buckets:", text_stdout.getvalue())
        self.assertIn("risk_context_linear", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-discard-benchmark-summary-v0")
        self.assertEqual(payload["reports"][0]["models"]["linear"]["feature_dim"], 76)

    def test_benchmark_discard_models_fast_writes_sparse_report(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "benchmark-fast.json"
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
                        "--models",
                        "fast",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertNotIn("raw_count_linear_train_accuracy", stdout.getvalue())
        self.assertNotIn("defense_context_v1_linear_train_accuracy", stdout.getvalue())
        self.assertEqual(
            set(payload["models"]),
            {"frequency", "linear", "risk_context_linear", "defense_context_linear"},
        )
        self.assertIsNone(payload["ablation"]["eval_accuracy_lift_over_raw_count"])
        self.assertEqual(
            payload["ablation"]["defense_context_eval_accuracy_lift_over_risk_context"],
            0.0,
        )

    def test_benchmark_discard_explicit_models(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "benchmark-explicit.json"
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
                        "--models",
                        "risk_context_linear,defense_context_linear",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertNotIn("frequency_train_accuracy", stdout.getvalue())
        self.assertEqual(set(payload["models"]), {"risk_context_linear", "defense_context_linear"})
        self.assertIsNone(payload["ablation"]["risk_context_eval_accuracy_lift_over_linear"])
        self.assertEqual(
            payload["ablation"]["defense_context_eval_accuracy_lift_over_risk_context"],
            0.0,
        )

    def test_benchmark_discard_writes_disagreement_artifact(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            disagreements = Path(directory) / "disagreements.json"
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
                        "--disagreements",
                        str(disagreements),
                        "--max-disagreements",
                        "2",
                    ]
                )
            payload = json.loads(disagreements.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertIn("disagreements_path:", stdout.getvalue())
        self.assertEqual(payload["kind"], "kenjaku-discard-disagreements-v0")
        self.assertEqual(payload["max_per_category"], 2)
        self.assertIn("risk_correct_defense_wrong", payload["categories"])
        for category in payload["categories"].values():
            self.assertLessEqual(len(category["items"]), 2)

    def test_disagreement_report_summary_outputs_text_and_json(self) -> None:
        payload = {
            "kind": "kenjaku-discard-disagreements-v0",
            "examples": 5,
            "max_per_category": 10,
            "categories": {
                "risk_correct_defense_wrong": {
                    "count": 1,
                    "items": [
                        {
                            "actual_discard": "5m",
                            "predictions": {
                                "risk_context_linear": "5m",
                                "defense_context_linear": "8m",
                            },
                            "defense_buckets": {
                                "genbutsu": True,
                                "suji": False,
                            },
                            "candidate_logits": {
                                "risk_context_linear": [
                                    {"tile": "5m", "logit": 3.0},
                                    {"tile": "8m", "logit": 1.0},
                                ],
                                "defense_context_linear": [
                                    {"tile": "5m", "logit": 0.5},
                                    {"tile": "8m", "logit": 2.0},
                                ],
                            },
                        }
                    ],
                }
            },
        }

        with TemporaryDirectory() as directory:
            report = Path(directory) / "disagreements.json"
            report.write_text(json.dumps(payload), encoding="utf-8")

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["disagreement-report-summary", str(report)])

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["disagreement-report-summary", str(report), "--json"])
            summary = json.loads(json_stdout.getvalue())

        category = summary["reports"][0]["categories"]["risk_correct_defense_wrong"]
        self.assertEqual(text_exit_code, 0)
        self.assertIn("risk_correct_defense_wrong: count=1 stored=1", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(summary["kind"], "kenjaku-discard-disagreement-summary-v0")
        self.assertEqual(category["defense_buckets"]["genbutsu"]["true"], 1)
        self.assertEqual(category["actual_prediction_pairs"][0]["wrong_prediction"], "8m")
        self.assertEqual(
            category["logit_margins"]["correct_model_actual_margin"]["mean"],
            2.0,
        )
        self.assertEqual(
            category["logit_margins"]["wrong_model_error_margin"]["mean"],
            1.5,
        )

    def test_benchmark_call_writes_report_artifact(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "call-benchmark.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-call-benchmark-report-v0")
        self.assertEqual(payload["call_examples"], 1)
        self.assertEqual(payload["model"]["kind"], "call-frequency-v0")
        self.assertEqual(payload["model"]["counts"]["pon"], 1)
        self.assertEqual(payload["metrics"]["train_accuracy"], 1.0)
        self.assertIsNone(payload["metrics"]["eval_accuracy"])
        self.assertEqual(payload["train_analysis"]["by_call_or_pass"]["call"]["examples"], 1)
        self.assertEqual(payload["train_analysis"]["by_legal_call_kinds"]["pon"]["correct"], 1)
        self.assertIn("report_path:", stdout.getvalue())


if __name__ == "__main__":
    unittest.main()
