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


class RiichiCommandTests(CliCommandTests):
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
        self.assertEqual(model["metrics"]["loss_kind"], "zero_one")
        self.assertEqual(model["metrics"]["train_loss"], 0.0)
        self.assertIsNone(model["metrics"]["eval_loss"])
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

    def test_benchmark_riichi_example_limit_records_total_examples(self) -> None:
        with TemporaryDirectory() as directory:
            fixture_dir = Path(directory) / "fixtures"
            fixture_dir.mkdir()
            fixture_text = Path("data/fixtures/tenhou/events_4p.xml").read_text(
                encoding="utf-8",
            )
            (fixture_dir / "a.xml").write_text(fixture_text, encoding="utf-8")
            (fixture_dir / "b.xml").write_text(fixture_text, encoding="utf-8")
            report = Path(directory) / "riichi-benchmark.json"

            with contextlib.redirect_stdout(io.StringIO()):
                exit_code = main(
                    [
                        "benchmark-riichi",
                        str(fixture_dir),
                        "--eval-fraction",
                        "0.5",
                        "--split-seed",
                        "fixed",
                        "--example-limit",
                        "1",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["riichi_examples"], 1)
        self.assertEqual(payload["riichi_examples_total"], 2)
        self.assertEqual(payload["example_limit"], 1)

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
