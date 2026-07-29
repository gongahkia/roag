from __future__ import annotations

from tests.commands.conftest import (
    CliCommandTests,
    Path,
    TemporaryDirectory,
    contextlib,
    io,
    json,
    main,
)


class BenchmarkReportCommandTests(CliCommandTests):
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
            payload["reports"][0]["models"]["call_linear_v1_calibrated"]["policy_threshold_source"],
            "tenhou-100-v0-eval-sweep",
        )
        self.assertIsNotNone(
            payload["reports"][0]["models"]["call_linear_v1"]["train_best_threshold"]
        )
        self.assertEqual(
            payload["reports"][1]["models"]["riichi_linear_weighted"]["positive_class_weight"],
            2.0,
        )

    def test_benchmark_report_summary_selects_call_policy_by_balanced_accuracy(self) -> None:
        report_payload = {
            "kind": "kenjaku-call-benchmark-report-v0",
            "source": {"label": "synthetic-call", "command": None, "date": None},
            "input_paths": ["synthetic"],
            "xml_file_count": 1,
            "rounds": 1,
            "draws": 0,
            "discards": 0,
            "reaches": 0,
            "calls": 0,
            "wins": 0,
            "exhaustive_draws": 0,
            "discard_examples": 0,
            "call_examples": 100,
            "call_examples_total": 200,
            "example_limit": 100,
            "example_limit_strategy": "balanced",
            "split": {
                "seed": "fixed",
                "eval_fraction": 0.2,
                "train_examples": 80,
                "eval_examples": 20,
            },
            "models": {
                "call_linear_v1_calibrated": {
                    "kind": "call-linear-v1",
                    "feature_dim": 137,
                    "training": {"positive_class_weight": 1.0},
                    "policy": {
                        "threshold": 0.4,
                        "threshold_source": "train-best",
                    },
                    "calibration": {
                        "train": {"best": {"threshold": 0.4}},
                        "eval": {"best": {"threshold": 0.45}},
                    },
                    "metrics": {
                        "train_accuracy": 0.8,
                        "eval_accuracy": 0.78,
                        "eval_balanced_accuracy": 0.74,
                        "eval_pass_recall": 0.76,
                        "eval_call_recall": 0.72,
                    },
                },
                "call_linear_v1_weighted": {
                    "kind": "call-linear-v1",
                    "feature_dim": 137,
                    "training": {"positive_class_weight": 2.0},
                    "calibration": {
                        "train": {"best": {"threshold": 0.8}},
                        "eval": {"best": {"threshold": 0.85}},
                    },
                    "metrics": {
                        "train_accuracy": 0.7,
                        "eval_accuracy": 0.7,
                        "eval_balanced_accuracy": 0.73,
                        "eval_pass_recall": 0.60,
                        "eval_call_recall": 0.86,
                    },
                },
            },
            "timing": None,
            "feature_cache": None,
            "example_cache": None,
            "parse_failures": {"count": 0, "items": []},
        }

        with TemporaryDirectory() as directory:
            report = Path(directory) / "call.json"
            report.write_text(json.dumps(report_payload), encoding="utf-8")

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["benchmark-report-summary", str(report)])

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["benchmark-report-summary", str(report), "--json"])
            summary = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("selected_policy: model=call_linear_v1_calibrated", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(
            summary["reports"][0]["selected_policy"]["model_name"],
            "call_linear_v1_calibrated",
        )
        self.assertEqual(
            summary["reports"][0]["selected_policy"]["eval_balanced_accuracy"],
            0.74,
        )
