from __future__ import annotations

import contextlib
import io
import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.cli import _limit_call_examples, main
from kenjaku.core import Action, ActionKind, Tile
from kenjaku.training import CallExample


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

    def test_export_decision_snapshots_writes_jsonl(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            output = Path(directory) / "snapshots.jsonl"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "export-decision-snapshots",
                        "data/fixtures/tenhou",
                        "--output",
                        str(output),
                        "--limit",
                        "5",
                        "--source-label",
                        "fixture-snapshots",
                    ]
                )
            rows = [
                json.loads(line)
                for line in output.read_text(encoding="utf-8").splitlines()
            ]

        self.assertEqual(exit_code, 0)
        self.assertEqual(len(rows), 5)
        self.assertIn("snapshots: 5", stdout.getvalue())
        self.assertEqual(rows[0]["kind"], "kenjaku-decision-snapshot-v0")
        self.assertEqual(rows[0]["source"]["label"], "fixture-snapshots")
        self.assertEqual(rows[0]["xml_file_count"], 3)
        self.assertEqual(rows[0]["mjai_events"][0]["type"], "start_kyoku")
        self.assertIn(rows[0]["decision_type"], {"discard", "call", "riichi"})
        self.assertIn("actual_action", rows[0])
        self.assertIn("legal_actions", rows[0])

    def test_export_decision_snapshots_filters_types(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            output = Path(directory) / "call-snapshots.jsonl"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "export-decision-snapshots",
                        "data/fixtures/tenhou",
                        "--decision-types",
                        "call",
                        "--output",
                        str(output),
                    ]
                )
            rows = [
                json.loads(line)
                for line in output.read_text(encoding="utf-8").splitlines()
            ]

        self.assertEqual(exit_code, 0)
        self.assertEqual(len(rows), 1)
        self.assertEqual(rows[0]["decision_type"], "call")
        self.assertEqual(rows[0]["actual_action"]["kind"], "pon")
        self.assertEqual(rows[0]["discarded_tile"], "1p")
        self.assertIn({"kind": "pass"}, rows[0]["legal_actions"])
        self.assertIn("decision_types: call", stdout.getvalue())

    def test_export_decision_snapshots_exports_riichi_type(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            output = Path(directory) / "riichi-snapshots.jsonl"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "export-decision-snapshots",
                        "data/fixtures/tenhou/events_4p.xml",
                        "--decision-types",
                        "riichi",
                        "--output",
                        str(output),
                    ]
                )
            rows = [
                json.loads(line)
                for line in output.read_text(encoding="utf-8").splitlines()
            ]

        self.assertEqual(exit_code, 0)
        self.assertGreaterEqual(len(rows), 1)
        self.assertTrue(all(row["decision_type"] == "riichi" for row in rows))
        self.assertTrue(
            all(
                {"kind": "riichi"} in row["legal_actions"]
                for row in rows
            )
        )

    def test_decision_snapshot_summary_reports_counts_and_malformed_rows(self) -> None:
        with TemporaryDirectory() as directory:
            output = Path(directory) / "snapshots.jsonl"
            with contextlib.redirect_stdout(io.StringIO()):
                main(
                    [
                        "export-decision-snapshots",
                        "data/fixtures/tenhou",
                        "--output",
                        str(output),
                        "--limit",
                        "5",
                        "--source-label",
                        "fixture-snapshots",
                    ]
                )
            with output.open("a", encoding="utf-8") as handle:
                handle.write("not-json\n")

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["decision-snapshot-summary", str(output)])

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["decision-snapshot-summary", str(output), "--json"])
            payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("snapshots: 5", text_stdout.getvalue())
        self.assertIn("malformed_rows: 1", text_stdout.getvalue())
        self.assertIn("fixture-snapshots", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-decision-snapshot-summary-v0")
        self.assertEqual(payload["snapshots"], 5)
        self.assertEqual(payload["malformed_rows"], 1)
        self.assertEqual(payload["sources"]["fixture-snapshots"], 5)
        self.assertGreaterEqual(payload["mjai_events"]["present"], 1)

    def test_decision_snapshots_include_row_id_and_compare_predictions(self) -> None:
        with TemporaryDirectory() as directory:
            snapshots = Path(directory) / "snapshots.jsonl"
            predictions = Path(directory) / "predictions.jsonl"
            with contextlib.redirect_stdout(io.StringIO()):
                main(
                    [
                        "export-decision-snapshots",
                        "data/fixtures/tenhou",
                        "--output",
                        str(snapshots),
                        "--limit",
                        "5",
                    ]
                )
            rows = [
                json.loads(line)
                for line in snapshots.read_text(encoding="utf-8").splitlines()
            ]
            prediction_rows = [
                {
                    "row_id": row["row_id"],
                    "predicted_action": row["actual_action"],
                }
                for row in rows[:-1]
            ]
            predictions.write_text(
                "\n".join(json.dumps(row) for row in prediction_rows) + "\nnot-json\n",
                encoding="utf-8",
            )

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    ["decision-snapshot-compare", str(snapshots), str(predictions)]
                )

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    [
                        "decision-snapshot-compare",
                        str(snapshots),
                        str(predictions),
                        "--json",
                    ]
                )
            payload = json.loads(json_stdout.getvalue())

        self.assertTrue(all(isinstance(row["row_id"], str) for row in rows))
        self.assertEqual(text_exit_code, 0)
        self.assertIn("missing_predictions: 1", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-decision-snapshot-comparison-v0")
        self.assertEqual(payload["snapshots"], 5)
        self.assertEqual(payload["predictions"], 4)
        self.assertEqual(payload["missing_predictions"], 1)
        self.assertEqual(payload["malformed_prediction_rows"], 1)
        self.assertEqual(payload["overall"]["correct"], 4)

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

    def test_benchmark_report_summary_supports_call_and_riichi_reports(self) -> None:
        with TemporaryDirectory() as directory:
            call_report = Path(directory) / "call.json"
            riichi_report = Path(directory) / "riichi.json"
            with contextlib.redirect_stdout(io.StringIO()):
                main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--include-weighted",
                        "--report",
                        str(call_report),
                    ]
                )
                main(
                    [
                        "benchmark-riichi",
                        "data/fixtures/tenhou/events_4p.xml",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--include-weighted",
                        "--report",
                        str(riichi_report),
                    ]
                )

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    ["benchmark-report-summary", str(call_report), str(riichi_report)]
                )

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    [
                        "benchmark-report-summary",
                        str(call_report),
                        str(riichi_report),
                        "--json",
                    ]
                )
            payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("call_linear_v1_calibrated", text_stdout.getvalue())
        self.assertIn("threshold=0.40", text_stdout.getvalue())
        self.assertIn("source=tenhou-100-v0-eval-sweep", text_stdout.getvalue())
        self.assertIn("train_best=", text_stdout.getvalue())
        self.assertIn("riichi_linear_calibrated", text_stdout.getvalue())
        self.assertIn("threshold=0.95", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-benchmark-summary-v0")
        self.assertEqual(payload["reports"][0]["target"], "call")
        self.assertEqual(payload["reports"][1]["target"], "riichi")
        self.assertEqual(
            payload["reports"][0]["models"]["call_linear_v1_calibrated"]["policy_threshold"],
            0.4,
        )
        self.assertEqual(
            payload["reports"][0]["models"]["call_linear_v1_calibrated"][
                "policy_threshold_source"
            ],
            "tenhou-100-v0-eval-sweep",
        )
        self.assertIsNotNone(
            payload["reports"][0]["models"]["call_linear_v1"]["train_best_threshold"]
        )
        self.assertEqual(
            payload["reports"][1]["models"]["riichi_linear_weighted"]["positive_class_weight"],
            2.0,
        )

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
                                "active_riichi_opponent": True,
                                "genbutsu": True,
                                "suji": False,
                            },
                            "shanten_delta": {
                                "before": 2,
                                "after": 2,
                                "delta": 0,
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

            examples_stdout = io.StringIO()
            with contextlib.redirect_stdout(examples_stdout):
                examples_exit_code = main(
                    ["disagreement-report-summary", str(report), "--examples", "1"]
                )

            tags_stdout = io.StringIO()
            with contextlib.redirect_stdout(tags_stdout):
                tags_exit_code = main(
                    ["disagreement-report-summary", str(report), "--examples", "1", "--tags"]
                )

            filtered_stdout = io.StringIO()
            with contextlib.redirect_stdout(filtered_stdout):
                filtered_exit_code = main(
                    [
                        "disagreement-report-summary",
                        str(report),
                        "--examples",
                        "1",
                        "--tag",
                        "defense_signal",
                    ]
                )

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    ["disagreement-report-summary", str(report), "--json", "--tags"]
                )
            summary = json.loads(json_stdout.getvalue())

        category = summary["reports"][0]["categories"]["risk_correct_defense_wrong"]
        tags = summary["tags"]["reports"][0]["categories"]["risk_correct_defense_wrong"]
        self.assertEqual(text_exit_code, 0)
        self.assertIn("risk_correct_defense_wrong: count=1 stored=1", text_stdout.getvalue())
        self.assertEqual(examples_exit_code, 0)
        self.assertIn("examples:", examples_stdout.getvalue())
        self.assertIn("actual=5m", examples_stdout.getvalue())
        self.assertIn("logits:", examples_stdout.getvalue())
        self.assertEqual(tags_exit_code, 0)
        self.assertIn("tags:", tags_stdout.getvalue())
        self.assertIn("defense_signal=1", tags_stdout.getvalue())
        self.assertIn(
            "tags: defense_signal, efficiency_like, active_riichi, safe_tile_candidate",
            tags_stdout.getvalue(),
        )
        self.assertEqual(filtered_exit_code, 0)
        self.assertIn("actual=5m", filtered_stdout.getvalue())
        self.assertIn("tags: defense_signal", filtered_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(summary["kind"], "kenjaku-discard-disagreement-summary-v0")
        self.assertEqual(category["defense_buckets"]["genbutsu"]["true"], 1)
        self.assertEqual(tags["stored"], 1)
        self.assertEqual(tags["defense_signal"], 1)
        self.assertEqual(tags["efficiency_like"], 1)
        self.assertEqual(tags["close_logit"], 0)
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
        self.assertEqual(
            set(payload["models"]),
            {
                "call_frequency",
                "call_legal_frequency",
                "call_linear",
                "call_linear_v1",
                "call_linear_v1_calibrated",
            },
        )
        frequency = payload["models"]["call_frequency"]
        legal_frequency = payload["models"]["call_legal_frequency"]
        call_linear = payload["models"]["call_linear"]
        call_linear_v1 = payload["models"]["call_linear_v1"]
        call_linear_v1_calibrated = payload["models"]["call_linear_v1_calibrated"]
        self.assertEqual(frequency["kind"], "call-frequency-v0")
        self.assertEqual(frequency["counts"]["pon"], 1)
        self.assertEqual(frequency["metrics"]["train_accuracy"], 1.0)
        self.assertEqual(frequency["metrics"]["train_balanced_accuracy"], 1.0)
        self.assertEqual(frequency["metrics"]["train_call_recall"], 1.0)
        self.assertIsNone(frequency["metrics"]["train_pass_recall"])
        self.assertIsNone(frequency["metrics"]["eval_accuracy"])
        self.assertIsNone(frequency["metrics"]["eval_balanced_accuracy"])
        self.assertEqual(frequency["train_analysis"]["by_call_or_pass"]["call"]["examples"], 1)
        self.assertEqual(frequency["train_analysis"]["by_legal_call_kinds"]["pon"]["correct"], 1)
        self.assertEqual(legal_frequency["kind"], "call-legal-frequency-v0")
        self.assertEqual(legal_frequency["counts"]["pon"], 1)
        self.assertEqual(legal_frequency["metrics"]["train_action_recall"]["pon"], 1.0)
        self.assertEqual(call_linear["kind"], "call-linear-v0")
        self.assertEqual(call_linear["feature_dim"], 120)
        self.assertEqual(call_linear["feature_profile"], "v0")
        self.assertEqual(call_linear["training"]["epochs"], 25)
        self.assertEqual(call_linear["training"]["positive_class_weight"], 1.0)
        self.assertEqual(call_linear["metrics"]["train_action_recall"]["pon"], 1.0)
        self.assertEqual(call_linear["calibration"]["target"], "call")
        self.assertEqual(len(call_linear["calibration"]["thresholds"]), 21)
        self.assertIsNotNone(call_linear["calibration"]["train"]["best"])
        self.assertIsNone(call_linear["calibration"]["eval"]["best"])
        self.assertEqual(call_linear_v1["kind"], "call-linear-v1")
        self.assertGreater(call_linear_v1["feature_dim"], call_linear["feature_dim"])
        self.assertEqual(call_linear_v1["feature_profile"], "v1")
        self.assertEqual(call_linear_v1["training"]["epochs"], 25)
        self.assertEqual(call_linear_v1["training"]["positive_class_weight"], 1.0)
        self.assertEqual(call_linear_v1["calibration"]["target"], "call")
        self.assertEqual(
            call_linear_v1["calibration"]["train"]["best"]["call_recall"],
            1.0,
        )
        self.assertEqual(call_linear_v1_calibrated["kind"], "call-linear-v1")
        self.assertEqual(call_linear_v1_calibrated["feature_profile"], "v1")
        self.assertEqual(
            call_linear_v1_calibrated["policy"],
            {
                "kind": "threshold-calibrated-v0",
                "target": "call",
                "base_model": "call_linear_v1",
                "threshold": 0.4,
                "threshold_source": "tenhou-100-v0-eval-sweep",
            },
        )
        self.assertEqual(call_linear_v1_calibrated["training"]["positive_class_weight"], 1.0)
        self.assertIn("call_frequency_eval_balanced_accuracy:", stdout.getvalue())
        self.assertIn("call_legal_frequency_eval_call_recall:", stdout.getvalue())
        self.assertIn("call_linear_eval_call_recall:", stdout.getvalue())
        self.assertIn("call_linear_eval_best_threshold:", stdout.getvalue())
        self.assertIn("call_linear_v1_eval_call_recall:", stdout.getvalue())
        self.assertIn("call_linear_v1_eval_best_threshold:", stdout.getvalue())
        self.assertIn("call_linear_v1_calibrated_policy_threshold: 0.40", stdout.getvalue())
        self.assertIn("report_path:", stdout.getvalue())

    def test_benchmark_call_models_fast_writes_sparse_report(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "call-benchmark-fast.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-limit",
                        "1",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(
            set(payload["models"]),
            {
                "call_frequency",
                "call_legal_frequency",
                "call_linear_v1",
                "call_linear_v1_calibrated",
            },
        )
        self.assertNotIn("call_linear", payload["models"])
        self.assertEqual(payload["example_limit"], 1)
        self.assertEqual(payload["call_examples_total"], 1)
        self.assertIn("call_linear_v1_eval_best_threshold:", stdout.getvalue())

    def test_benchmark_call_supports_zero_epochs(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "call-benchmark-zero-epochs.json"
            with contextlib.redirect_stdout(io.StringIO()):
                exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--epochs",
                        "0",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["models"]["call_linear_v1"]["training"]["epochs"], 0)
        self.assertEqual(
            payload["models"]["call_linear_v1_calibrated"]["training"]["epochs"],
            0,
        )

    def test_benchmark_call_profiles_and_reuses_feature_cache(self) -> None:
        with TemporaryDirectory() as directory:
            cache = Path(directory) / "feature-cache.json"
            first_report = Path(directory) / "call-first.json"
            second_report = Path(directory) / "call-second.json"

            first_stdout = io.StringIO()
            with contextlib.redirect_stdout(first_stdout):
                first_exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-limit",
                        "1",
                        "--profile-stages",
                        "--feature-cache",
                        str(cache),
                        "--report",
                        str(first_report),
                    ]
                )
            first_payload = json.loads(first_report.read_text(encoding="utf-8"))

            second_stdout = io.StringIO()
            with contextlib.redirect_stdout(second_stdout):
                second_exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-limit",
                        "1",
                        "--profile-stages",
                        "--feature-cache",
                        str(cache),
                        "--report",
                        str(second_report),
                    ]
                )
            second_payload = json.loads(second_report.read_text(encoding="utf-8"))
            cache_payload = json.loads(cache.read_text(encoding="utf-8"))
            cache_exists = cache.exists()

        self.assertEqual(first_exit_code, 0)
        self.assertTrue(cache_exists)
        self.assertIn("stage_parse_seconds:", first_stdout.getvalue())
        self.assertIn("feature_cache_misses: v1", first_stdout.getvalue())
        self.assertEqual(first_payload["feature_cache"]["misses"], ["v1"])
        self.assertEqual(first_payload["feature_cache"]["writes"], ["v1"])
        self.assertIn("parse", first_payload["timing"])
        self.assertEqual(second_exit_code, 0)
        self.assertIn("feature_cache_hits: v1", second_stdout.getvalue())
        self.assertEqual(second_payload["feature_cache"]["hits"], ["v1"])
        self.assertEqual(second_payload["feature_cache"]["misses"], [])
        self.assertIn("example_signature", cache_payload["cache_key"])
        self.assertIn("train_signature", cache_payload["cache_key"])
        self.assertIn("eval_signature", cache_payload["cache_key"])

    def test_balanced_call_example_limit_keeps_non_pass_examples_first(self) -> None:
        tile = Tile.parse("1p")

        def example(index: int, action: Action) -> CallExample:
            return CallExample(
                round_index=0,
                event_index=index,
                call_event_index=index if action.kind != ActionKind.PASS else None,
                seat=1,
                from_seat=0,
                dealer=0,
                scores=(25000, 25000, 25000, 25000),
                discarded_tile=tile,
                legal_call_kinds=(ActionKind.PON,),
                hand_counts=(0,) * 34,
                visible_counts=(0,) * 34,
                action=action,
            )

        examples = [
            example(0, Action.pass_()),
            example(1, Action(ActionKind.PON, tile.type)),
            example(2, Action.pass_()),
            example(3, Action(ActionKind.PON, tile.type)),
            example(4, Action(ActionKind.PON, tile.type)),
            example(5, Action.pass_()),
            example(6, Action(ActionKind.PON, tile.type)),
            example(7, Action.pass_()),
        ]

        selected = _limit_call_examples(examples, 4, strategy="balanced")

        self.assertEqual(
            [example.action.kind for example in selected],
            [ActionKind.PON, ActionKind.PON, ActionKind.PASS, ActionKind.PASS],
        )

    def test_benchmark_call_models_rejects_unknown_name(self) -> None:
        with self.assertRaises(SystemExit) as context:
            main(
                [
                    "benchmark-call",
                    "data/fixtures/tenhou",
                    "--models",
                    "call_linear_v9",
                ]
            )

        self.assertIn("unsupported call benchmark model", str(context.exception))

    def test_benchmark_call_can_include_weighted_variant(self) -> None:
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
                        "--include-weighted",
                        "--call-positive-weight",
                        "3.0",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertIn("call_linear_v1_weighted", payload["models"])
        weighted = payload["models"]["call_linear_v1_weighted"]
        self.assertEqual(weighted["kind"], "call-linear-v1")
        self.assertEqual(weighted["feature_profile"], "v1")
        self.assertEqual(weighted["training"]["positive_class_weight"], 3.0)
        self.assertNotIn("policy", weighted)
        self.assertIn("call_linear_v1_weighted_eval_call_recall:", stdout.getvalue())

    def test_benchmark_call_can_use_train_best_threshold_source(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "call-benchmark.json"
            with contextlib.redirect_stdout(io.StringIO()):
                exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--call-threshold-source",
                        "train-best",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        base = payload["models"]["call_linear_v1"]
        calibrated = payload["models"]["call_linear_v1_calibrated"]
        self.assertEqual(exit_code, 0)
        self.assertEqual(calibrated["policy"]["threshold_source"], "train-best")
        self.assertEqual(
            calibrated["policy"]["threshold"],
            base["calibration"]["train"]["best"]["threshold"],
        )

    def test_benchmark_riichi_writes_report_artifact(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "riichi-benchmark.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-riichi",
                        "data/fixtures/tenhou/events_4p.xml",
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
        self.assertEqual(payload["kind"], "kenjaku-riichi-benchmark-report-v0")
        self.assertEqual(payload["riichi_examples"], 1)
        self.assertEqual(
            set(payload["models"]),
            {"riichi_frequency", "riichi_linear", "riichi_linear_calibrated"},
        )
        model = payload["models"]["riichi_frequency"]
        linear = payload["models"]["riichi_linear"]
        calibrated = payload["models"]["riichi_linear_calibrated"]
        self.assertEqual(model["kind"], "riichi-frequency-v0")
        self.assertEqual(model["counts"]["riichi"], 1)
        self.assertEqual(model["metrics"]["train_riichi_recall"], 1.0)
        self.assertEqual(linear["kind"], "riichi-linear-v0")
        self.assertGreater(linear["feature_dim"], 0)
        self.assertEqual(linear["training"]["epochs"], 25)
        self.assertEqual(linear["training"]["positive_class_weight"], 1.0)
        self.assertEqual(linear["metrics"]["train_riichi_recall"], 1.0)
        self.assertEqual(linear["calibration"]["target"], "riichi")
        self.assertEqual(len(linear["calibration"]["thresholds"]), 21)
        self.assertIsNotNone(linear["calibration"]["train"]["best"])
        self.assertIsNone(linear["calibration"]["eval"]["best"])
        self.assertEqual(linear["calibration"]["train"]["best"]["riichi_recall"], 1.0)
        self.assertEqual(calibrated["kind"], "riichi-linear-v0")
        self.assertEqual(
            calibrated["policy"],
            {
                "kind": "threshold-calibrated-v0",
                "target": "riichi",
                "base_model": "riichi_linear",
                "threshold": 0.95,
                "threshold_source": "tenhou-100-v0-eval-sweep",
            },
        )
        self.assertEqual(calibrated["training"]["positive_class_weight"], 1.0)
        self.assertIn("riichi_frequency_eval_balanced_accuracy:", stdout.getvalue())
        self.assertIn("riichi_linear_eval_balanced_accuracy:", stdout.getvalue())
        self.assertIn("riichi_linear_eval_best_threshold:", stdout.getvalue())
        self.assertIn("riichi_linear_calibrated_policy_threshold: 0.95", stdout.getvalue())
        self.assertIn("report_path:", stdout.getvalue())

    def test_benchmark_riichi_can_use_train_best_threshold_source(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "riichi-benchmark.json"
            with contextlib.redirect_stdout(io.StringIO()):
                exit_code = main(
                    [
                        "benchmark-riichi",
                        "data/fixtures/tenhou/events_4p.xml",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--riichi-threshold-source",
                        "train-best",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        base = payload["models"]["riichi_linear"]
        calibrated = payload["models"]["riichi_linear_calibrated"]
        self.assertEqual(exit_code, 0)
        self.assertEqual(calibrated["policy"]["threshold_source"], "train-best")
        self.assertEqual(
            calibrated["policy"]["threshold"],
            base["calibration"]["train"]["best"]["threshold"],
        )

    def test_benchmark_riichi_can_include_weighted_variant(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "riichi-benchmark.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-riichi",
                        "data/fixtures/tenhou/events_4p.xml",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--include-weighted",
                        "--riichi-positive-weight",
                        "3.0",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertIn("riichi_linear_weighted", payload["models"])
        weighted = payload["models"]["riichi_linear_weighted"]
        self.assertEqual(weighted["kind"], "riichi-linear-v0")
        self.assertEqual(weighted["training"]["positive_class_weight"], 3.0)
        self.assertNotIn("policy", weighted)
        self.assertIn("riichi_linear_weighted_eval_riichi_recall:", stdout.getvalue())


if __name__ == "__main__":
    unittest.main()
