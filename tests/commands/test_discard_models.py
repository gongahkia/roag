from __future__ import annotations

from tests.commands.conftest import (
    Action,
    CliCommandTests,
    DiscardExample,
    Path,
    TemporaryDirectory,
    Tile,
    TileType,
    _disagreement_record,
    build_discard_mlp_benchmark_report,
    contextlib,
    importlib,
    io,
    json,
    main,
    parse_tenhou_xml_file,
    tile_counts,
)


class DiscardModelCommandTests(CliCommandTests):
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

    def test_train_discard_mlp_fixture_smoke_writes_report_artifact(self) -> None:
        if importlib.util.find_spec("torch") is None:
            self.skipTest("PyTorch is not available")
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "mlp.json"
            metrics_jsonl = Path(directory) / "mlp-metrics.jsonl"
            stderr = io.StringIO()
            with contextlib.redirect_stdout(stdout), contextlib.redirect_stderr(stderr):
                exit_code = main(
                    [
                        "train-discard-mlp",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--batch-size",
                        "2",
                        "--hidden-dim",
                        "8",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--seed",
                        "123",
                        "--device",
                        "cpu",
                        "--report",
                        str(report),
                        "--metrics-jsonl",
                        str(metrics_jsonl),
                        "--source-label",
                        "fixture-mlp",
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))
            metric_lines = [line for line in stderr.getvalue().splitlines() if line.startswith("{")]
            metric_file_lines = metrics_jsonl.read_text(encoding="utf-8").splitlines()
            metric_events = [json.loads(line) for line in metric_lines]

        self.assertEqual(exit_code, 0)
        self.assertIn("device: cpu", stdout.getvalue())
        self.assertIn("report_path:", stdout.getvalue())
        self.assertEqual(payload["kind"], "kenjaku-discard-mlp-report-v0")
        self.assertEqual(payload["source"]["label"], "fixture-mlp")
        self.assertEqual(payload["discard_examples"], 4)
        self.assertEqual(payload["split"]["train_examples"], 3)
        self.assertEqual(payload["split"]["eval_examples"], 1)
        self.assertEqual(payload["model"]["kind"], "discard-mlp-v0")
        self.assertEqual(payload["model"]["input_dim"], 68)
        self.assertEqual(payload["model"]["hidden_dim"], 8)
        self.assertEqual(payload["training"]["device"], "cpu")
        self.assertEqual(payload["training"]["seed"], 123)
        self.assertEqual(payload["training"]["best_epoch"], 1)
        self.assertEqual(payload["training"]["selection_split"], "eval")
        self.assertEqual([row["epoch"] for row in payload["training"]["history"]], [1])
        self.assertEqual(payload["training_history"]["step_unit"], "epoch")
        self.assertEqual(payload["training_history"]["records"][0]["step"], 1)
        self.assertEqual(len(payload["training_history"]["curves"]["eval_accuracy"]), 1)
        self.assertEqual(metric_file_lines, metric_lines)
        self.assertEqual(len(metric_events), len(payload["training"]["history"]))
        for event, row in zip(metric_events, payload["training"]["history"], strict=True):
            self.assertEqual(
                set(event),
                {
                    "event",
                    "model",
                    "epoch",
                    "train_loss",
                    "eval_loss",
                    "eval_accuracy",
                    "elapsed_seconds",
                },
            )
            self.assertEqual(event["event"], "train_epoch")
            self.assertEqual(event["model"], "discard-mlp-v0")
            self.assertEqual(event["epoch"], row["epoch"])
            self.assertIsInstance(event["train_loss"], float)
            self.assertIsInstance(event["eval_loss"], float)
            self.assertIsInstance(event["eval_accuracy"], float)
            self.assertIsInstance(event["elapsed_seconds"], float)
        self.assertEqual(payload["metrics"]["train"]["examples"], 3)
        self.assertEqual(payload["metrics"]["eval"]["examples"], 1)
        self.assertEqual(payload["metrics"]["best"]["eval"]["examples"], 1)
        self.assertIsNone(payload["artifacts"]["checkpoint_path"])

    def test_benchmark_report_summary_supports_standalone_mlp_report(self) -> None:
        report_payload = {
            "kind": "kenjaku-discard-mlp-report-v0",
            "source": {"label": "synthetic-mlp", "command": None, "date": None},
            "input_paths": ["synthetic"],
            "xml_file_count": 1,
            "rounds": 1,
            "draws": 0,
            "discards": 0,
            "reaches": 0,
            "calls": 0,
            "wins": 0,
            "exhaustive_draws": 0,
            "discard_examples": 4,
            "call_examples": 1,
            "split": {
                "seed": "fixed",
                "eval_fraction": 0.25,
                "train_examples": 3,
                "eval_examples": 1,
            },
            "model": {
                "kind": "discard-mlp-v0",
                "input_dim": 68,
                "hidden_dim": 8,
                "output_dim": 34,
            },
            "training": {
                "epochs": 1,
                "batch_size": 2,
                "learning_rate": 0.001,
                "device": "cpu",
                "seed": 123,
                "history": [],
                "best_epoch": 1,
                "selection_split": "eval",
            },
            "metrics": {
                "train": {"examples": 3, "loss": 1.5, "accuracy": 2 / 3},
                "eval": {"examples": 1, "loss": 2.0, "accuracy": 0.0},
                "best": {
                    "train": {"examples": 3, "loss": 1.5, "accuracy": 2 / 3},
                    "eval": {"examples": 1, "loss": 2.0, "accuracy": 0.0},
                },
            },
            "discard_shanten": {"examples": 4},
            "parse_failures": {"count": 0, "items": []},
            "artifacts": {"checkpoint_path": None},
        }

        with TemporaryDirectory() as directory:
            report = Path(directory) / "mlp.json"
            report.write_text(json.dumps(report_payload), encoding="utf-8")

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["benchmark-report-summary", str(report)])

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["benchmark-report-summary", str(report), "--json"])
            payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("model: discard-mlp-v0 hidden_dim=8", text_stdout.getvalue())
        self.assertIn("best: epoch=1 split=eval", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-discard-benchmark-summary-v0")
        self.assertEqual(payload["reports"][0]["target"], "discard_mlp")
        self.assertEqual(payload["reports"][0]["model"]["hidden_dim"], 8)

    def test_benchmark_report_summary_supports_synthetic_mlp_benchmark_report(self) -> None:
        report_payload = build_discard_mlp_benchmark_report(
            input_paths=[Path("synthetic")],
            xml_files=[Path("synthetic.xml")],
            game=parse_tenhou_xml_file(Path("data/fixtures/tenhou/minimal_4p.xml")),
            discard_examples=4,
            call_examples=1,
            split_seed="fixed",
            eval_fraction=0.25,
            train_examples=3,
            eval_examples=1,
            models={
                "frequency": {
                    "metrics": {"train_accuracy": 2 / 3, "eval_accuracy": 0.0},
                },
                "risk_context_linear": {
                    "kind": "discard-linear-risk-context-v0",
                    "feature_dim": 86,
                    "metrics": {"train_accuracy": 2 / 3, "eval_accuracy": 0.25},
                },
                "defense_context_linear": {
                    "kind": "discard-linear-defense-context-v0",
                    "feature_dim": 98,
                    "metrics": {"train_accuracy": 2 / 3, "eval_accuracy": 0.5},
                },
                "discard_mlp": {
                    "kind": "discard-mlp-v0",
                    "input_dim": 68,
                    "hidden_dim": 8,
                    "output_dim": 34,
                    "training": {
                        "epochs": 1,
                        "batch_size": 2,
                        "learning_rate": 0.001,
                        "device": "cpu",
                        "seed": 123,
                        "history": [],
                        "best_epoch": 1,
                        "selection_split": "eval",
                    },
                    "metrics": {
                        "train": {"examples": 3, "loss": 1.5, "accuracy": 2 / 3},
                        "eval": {"examples": 1, "loss": 2.0, "accuracy": 0.75},
                        "best": {
                            "train": {"examples": 3, "loss": 1.5, "accuracy": 2 / 3},
                            "eval": {"examples": 1, "loss": 2.0, "accuracy": 0.75},
                        },
                    },
                },
            },
            discard_shanten={"examples": 4},
            parse_failures=(),
            checkpoint_path=None,
            source={"label": "synthetic-mlp-benchmark", "command": None, "date": None},
        )

        with TemporaryDirectory() as directory:
            report = Path(directory) / "mlp-benchmark.json"
            report.write_text(json.dumps(report_payload), encoding="utf-8")

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["benchmark-report-summary", str(report)])

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["benchmark-report-summary", str(report), "--json"])
            payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("discard_mlp: eval=0.7500", text_stdout.getvalue())
        self.assertIn(
            "mlp_eval_accuracy_lift_over_defense_context: +0.2500",
            text_stdout.getvalue(),
        )
        self.assertEqual(
            report_payload["deltas"]["mlp_eval_accuracy_lift_over_defense_context"],
            0.25,
        )
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["reports"][0]["target"], "discard_mlp_benchmark")
        self.assertEqual(payload["reports"][0]["models"]["discard_mlp"]["hidden_dim"], 8)

    def test_train_discard_mlp_checkpoint_writes_best_state(self) -> None:
        if importlib.util.find_spec("torch") is None:
            self.skipTest("PyTorch is not available")
        torch = __import__("torch")
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            checkpoint = Path(directory) / "mlp.pt"
            report = Path(directory) / "mlp.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "train-discard-mlp",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--batch-size",
                        "2",
                        "--hidden-dim",
                        "8",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--seed",
                        "123",
                        "--device",
                        "cpu",
                        "--checkpoint",
                        str(checkpoint),
                        "--report",
                        str(report),
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))
            checkpoint_payload = torch.load(checkpoint, map_location="cpu")

        self.assertEqual(exit_code, 0)
        self.assertIn("checkpoint_path:", stdout.getvalue())
        self.assertEqual(report_payload["artifacts"]["checkpoint_path"], str(checkpoint))
        self.assertEqual(checkpoint_payload["kind"], "kenjaku-discard-mlp-checkpoint-v0")
        self.assertEqual(checkpoint_payload["model"]["kind"], "discard-mlp-v0")
        self.assertEqual(checkpoint_payload["model"]["hidden_dim"], 8)
        self.assertEqual(
            checkpoint_payload["training"]["best_epoch"],
            report_payload["training"]["best_epoch"],
        )
        self.assertEqual(
            checkpoint_payload["training"]["selection_split"],
            report_payload["training"]["selection_split"],
        )
        self.assertEqual(checkpoint_payload["metrics"]["best"], report_payload["metrics"]["best"])
        self.assertIn("net.0.weight", checkpoint_payload["model_state_dict"])

    def test_benchmark_discard_mlp_fixture_smoke_writes_report_artifact(self) -> None:
        if importlib.util.find_spec("torch") is None:
            self.skipTest("PyTorch is not available")
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "mlp-benchmark.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-discard-mlp",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--batch-size",
                        "2",
                        "--hidden-dim",
                        "8",
                        "--linear-epochs",
                        "1",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--seed",
                        "123",
                        "--device",
                        "cpu",
                        "--report",
                        str(report),
                        "--source-label",
                        "fixture-mlp-benchmark",
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))
            summary_stdout = io.StringIO()
            with contextlib.redirect_stdout(summary_stdout):
                summary_exit_code = main(["benchmark-report-summary", str(report)])
            summary_text = summary_stdout.getvalue()

        self.assertEqual(exit_code, 0)
        self.assertIn("discard_mlp_best_eval_accuracy:", stdout.getvalue())
        self.assertEqual(payload["kind"], "kenjaku-discard-mlp-benchmark-report-v0")
        self.assertEqual(payload["source"]["label"], "fixture-mlp-benchmark")
        self.assertEqual(payload["discard_examples"], 4)
        self.assertEqual(payload["split"]["train_examples"], 3)
        self.assertEqual(payload["split"]["eval_examples"], 1)
        self.assertEqual(
            set(payload["models"]),
            {"frequency", "risk_context_linear", "defense_context_linear", "discard_mlp"},
        )
        self.assertEqual(payload["models"]["discard_mlp"]["hidden_dim"], 8)
        self.assertEqual(payload["models"]["discard_mlp"]["training"]["device"], "cpu")
        self.assertIn("mlp_eval_accuracy_lift_over_defense_context", payload["deltas"])
        self.assertEqual(summary_exit_code, 0)
        self.assertIn("discard_mlp: eval=", summary_text)
        self.assertIn("deltas:", summary_text)

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
        self.assertEqual(payload["models"]["frequency"]["metrics"]["loss_kind"], "zero_one")
        self.assertAlmostEqual(payload["models"]["frequency"]["metrics"]["train_loss"], 1 / 3)
        self.assertEqual(payload["models"]["frequency"]["metrics"]["eval_loss"], 1.0)
        self.assertIn("4p", payload["models"]["frequency"]["metrics"]["train_action_recall"])
        self.assertIn("4p", payload["models"]["frequency"]["metrics"]["eval_action_recall"])
        self.assertIn("train_balanced_accuracy", payload["models"]["frequency"]["metrics"])
        self.assertIn("eval_balanced_accuracy", payload["models"]["frequency"]["metrics"])
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
        self.assertEqual(payload["models"]["raw_count_linear"]["metrics"]["loss_kind"], "zero_one")
        self.assertIn("train_action_recall", payload["models"]["raw_count_linear"]["metrics"])
        self.assertIn("eval_action_recall", payload["models"]["raw_count_linear"]["metrics"])
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

    def test_feature_importance_ranks_linear_report_features(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "benchmark.json"
            importance = Path(directory) / "importance.json"
            with contextlib.redirect_stdout(io.StringIO()):
                benchmark_exit_code = main(
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

            stdout = io.StringIO()
            with contextlib.redirect_stdout(stdout):
                importance_exit_code = main(
                    [
                        "feature-importance",
                        str(report),
                        "--profile",
                        "RISK_CONTEXT",
                        "--top-k",
                        "5",
                        "--output",
                        str(importance),
                    ]
                )
            payload = json.loads(importance.read_text(encoding="utf-8"))

        self.assertEqual(benchmark_exit_code, 0)
        self.assertEqual(importance_exit_code, 0)
        self.assertIn("model: risk_context_linear", stdout.getvalue())
        self.assertEqual(payload["kind"], "kenjaku-feature-importance-v0")
        self.assertEqual(payload["model_name"], "risk_context_linear")
        self.assertEqual(payload["top_k"], 5)
        self.assertEqual(len(payload["rankings"]), 5)
        self.assertEqual(payload["rankings"][0]["rank"], 1)
        self.assertGreaterEqual(
            payload["rankings"][0]["importance"],
            payload["rankings"][-1]["importance"],
        )
        self.assertIn("mean_feature_value", payload["rankings"][0])

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

    def test_benchmark_discard_example_limit_records_total_examples(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "benchmark-limit.json"
            with contextlib.redirect_stdout(io.StringIO()):
                exit_code = main(
                    [
                        "benchmark-discard",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--eval-fraction",
                        "0.5",
                        "--split-seed",
                        "fixed",
                        "--models",
                        "fast",
                        "--example-limit",
                        "2",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["discard_examples"], 2)
        self.assertEqual(payload["discard_examples_total"], 4)
        self.assertEqual(payload["example_limit"], 2)
        self.assertEqual(payload["split"]["train_examples"], 1)
        self.assertEqual(payload["split"]["eval_examples"], 1)

    def test_streamed_benchmarks_stop_at_example_limit_and_record_prefix(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            discard_report = root / "discard.json"
            call_report = root / "call.json"
            riichi_report = root / "riichi.json"

            with contextlib.redirect_stdout(io.StringIO()):
                discard_exit_code = main(
                    [
                        "benchmark-discard",
                        "data/fixtures/tenhou",
                        "--models",
                        "frequency",
                        "--stream-examples",
                        "--example-limit",
                        "2",
                        "--report",
                        str(discard_report),
                    ]
                )
                call_exit_code = main(
                    [
                        "benchmark-call",
                        "data/fixtures/tenhou",
                        "--models",
                        "call_frequency",
                        "--stream-examples",
                        "--example-limit",
                        "1",
                        "--report",
                        str(call_report),
                    ]
                )
                riichi_exit_code = main(
                    [
                        "benchmark-riichi",
                        "data/fixtures/tenhou",
                        "--stream-examples",
                        "--example-limit",
                        "1",
                        "--report",
                        str(riichi_report),
                    ]
                )
            discard_payload = json.loads(discard_report.read_text(encoding="utf-8"))
            call_payload = json.loads(call_report.read_text(encoding="utf-8"))
            riichi_payload = json.loads(riichi_report.read_text(encoding="utf-8"))

        self.assertEqual(discard_exit_code, 0)
        self.assertEqual(call_exit_code, 0)
        self.assertEqual(riichi_exit_code, 0)
        for payload in (discard_payload, call_payload, riichi_payload):
            self.assertTrue(payload["streaming_example_limit"])
            self.assertEqual(payload["source_xml_file_count"], 3)
            self.assertGreaterEqual(payload["parsed_xml_file_count"], 1)
            self.assertLessEqual(
                payload["parsed_xml_file_count"],
                payload["source_xml_file_count"],
            )
        self.assertEqual(discard_payload["discard_examples"], 2)
        self.assertEqual(call_payload["call_examples"], 1)
        self.assertEqual(riichi_payload["riichi_examples"], 1)

    def test_export_bc_examples_and_benchmark_from_shards(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            export_dir = root / "bc-examples"
            discard_report = root / "discard.json"
            call_report = root / "call.json"
            riichi_report = root / "riichi.json"

            export_stdout = io.StringIO()
            with contextlib.redirect_stdout(export_stdout):
                export_exit_code = main(
                    [
                        "export-bc-examples",
                        "data/fixtures/tenhou",
                        "--output-dir",
                        str(export_dir),
                        "--shard-size",
                        "2",
                        "--source-label",
                        "fixture-bc-export",
                    ]
                )
            manifest_path = export_dir / "manifest.json"
            manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
            discard_shard = next(
                shard for shard in manifest["shards"] if shard["decision_type"] == "discard"
            )
            first_discard_row = json.loads(
                (export_dir / discard_shard["path"]).read_text(encoding="utf-8").splitlines()[0],
            )

            with contextlib.redirect_stdout(io.StringIO()):
                discard_exit_code = main(
                    [
                        "benchmark-discard-from-examples",
                        str(manifest_path),
                        "--models",
                        "frequency",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--report",
                        str(discard_report),
                    ]
                )
                call_exit_code = main(
                    [
                        "benchmark-call-from-examples",
                        str(export_dir),
                        "--models",
                        "call_frequency",
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--report",
                        str(call_report),
                    ]
                )
                riichi_exit_code = main(
                    [
                        "benchmark-riichi-from-examples",
                        str(manifest_path),
                        "--eval-fraction",
                        "0.25",
                        "--split-seed",
                        "fixed",
                        "--report",
                        str(riichi_report),
                    ]
                )

            summary_stdout = io.StringIO()
            with contextlib.redirect_stdout(summary_stdout):
                summary_exit_code = main(
                    [
                        "benchmark-report-summary",
                        str(discard_report),
                        str(call_report),
                        str(riichi_report),
                    ]
                )
            discard_payload = json.loads(discard_report.read_text(encoding="utf-8"))
            call_payload = json.loads(call_report.read_text(encoding="utf-8"))
            riichi_payload = json.loads(riichi_report.read_text(encoding="utf-8"))

        self.assertEqual(export_exit_code, 0)
        self.assertIn("manifest_path:", export_stdout.getvalue())
        self.assertEqual(manifest["kind"], "kenjaku-bc-example-manifest-v0")
        self.assertEqual(manifest["source"]["label"], "fixture-bc-export")
        self.assertEqual(manifest["decision_counts"]["discard"], 4)
        self.assertEqual(manifest["decision_counts"]["call"], 1)
        self.assertEqual(manifest["decision_counts"]["riichi"], 3)
        self.assertGreaterEqual(len(manifest["shards"]), 3)
        self.assertIsInstance(first_discard_row["example"]["discard_is_tsumogiri"], bool)
        self.assertEqual(
            first_discard_row["example"]["discard_is_tsumogiri"],
            first_discard_row["example"]["action"]["tsumogiri"],
        )
        self.assertEqual(discard_exit_code, 0)
        self.assertEqual(call_exit_code, 0)
        self.assertEqual(riichi_exit_code, 0)
        self.assertEqual(discard_payload["kind"], "kenjaku-discard-benchmark-report-v0")
        self.assertEqual(discard_payload["source"]["label"], "fixture-bc-export")
        self.assertEqual(discard_payload["discard_examples"], 4)
        self.assertEqual(discard_payload["bc_example_source"]["source_xml_file_count"], 3)
        self.assertEqual(discard_payload["models"]["frequency"]["metrics"]["loss_kind"], "zero_one")
        self.assertEqual(call_payload["kind"], "kenjaku-call-benchmark-report-v0")
        self.assertEqual(call_payload["call_examples"], 1)
        self.assertEqual(call_payload["discard_examples"], 4)
        self.assertEqual(
            call_payload["models"]["call_frequency"]["metrics"]["loss_kind"],
            "zero_one",
        )
        self.assertEqual(riichi_payload["kind"], "kenjaku-riichi-benchmark-report-v0")
        self.assertEqual(riichi_payload["riichi_examples"], 3)
        self.assertEqual(riichi_payload["call_examples"], 1)
        self.assertEqual(
            riichi_payload["models"]["riichi_linear"]["metrics"]["loss_kind"],
            "zero_one",
        )
        self.assertEqual(summary_exit_code, 0)
        self.assertIn("frequency:", summary_stdout.getvalue())
        self.assertIn("call_frequency:", summary_stdout.getvalue())
        self.assertIn("riichi_linear:", summary_stdout.getvalue())

    def test_export_bc_examples_records_parse_failures_while_streaming(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            broken = root / "broken.xml"
            export_dir = root / "bc-examples"
            broken.write_text("<mjloggm><INIT /></mjloggm>", encoding="utf-8")

            with contextlib.redirect_stdout(io.StringIO()):
                exit_code = main(
                    [
                        "export-bc-examples",
                        "data/fixtures/tenhou/minimal_4p.xml",
                        str(broken),
                        "--output-dir",
                        str(export_dir),
                        "--skip-errors",
                    ]
                )
            manifest = json.loads((export_dir / "manifest.json").read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(manifest["parsed_xml_file_count"], 1)
        self.assertEqual(len(manifest["parse_failures"]), 1)
        self.assertEqual(manifest["parse_failures"][0]["error_type"], "ValueError")

    def test_benchmark_from_examples_records_limit_and_total(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            export_dir = root / "bc-examples"
            report = root / "discard-limit.json"

            with contextlib.redirect_stdout(io.StringIO()):
                export_exit_code = main(
                    [
                        "export-bc-examples",
                        "data/fixtures/tenhou",
                        "--output-dir",
                        str(export_dir),
                        "--actions",
                        "discard",
                    ]
                )
                benchmark_exit_code = main(
                    [
                        "benchmark-discard-from-examples",
                        str(export_dir / "manifest.json"),
                        "--models",
                        "frequency",
                        "--eval-fraction",
                        "0.5",
                        "--split-seed",
                        "fixed",
                        "--example-limit",
                        "2",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(export_exit_code, 0)
        self.assertEqual(benchmark_exit_code, 0)
        self.assertEqual(payload["discard_examples"], 2)
        self.assertEqual(payload["discard_examples_total"], 4)
        self.assertEqual(payload["example_limit"], 2)
        self.assertEqual(payload["split"]["train_examples"], 1)
        self.assertEqual(payload["split"]["eval_examples"], 1)

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

    def test_disagreement_record_includes_defense_risk_payload(self) -> None:
        hand = tuple(Tile.parse(tile) for tile in ["4m", "5m"])
        opponent_river = (Tile.parse("4m"),)
        rivers_by_seat = ((), opponent_river, (), ())
        example = DiscardExample(
            round_index=0,
            event_index=0,
            seat=0,
            dealer=0,
            scores=(25000, 25000, 25000, 25000),
            hand_counts=tile_counts(hand),
            visible_counts=tile_counts((*hand, *opponent_river)),
            action=Action.discard("5m"),
            active_riichi_seats=(False, True, False, False),
            river_counts_by_seat=tuple(tile_counts(river) for river in rivers_by_seat),
            rivers_by_seat=rivers_by_seat,
            riichi_declared_turns=(None, 0, None, None),
            riichi_declared_event_indices=(None, 1, None, None),
        )
        tile_4m = TileType.parse("4m")
        tile_5m = TileType.parse("5m")

        record = _disagreement_record(
            example,
            predictions={
                "risk_context_linear": tile_5m,
                "defense_context_linear": tile_4m,
            },
            correct={
                "risk_context_linear": True,
                "defense_context_linear": False,
            },
            logits_by_model={
                "risk_context_linear": {tile_4m: 0.5, tile_5m: 2.0},
                "defense_context_linear": {tile_4m: 2.0, tile_5m: 0.5},
            },
        )

        defense_risk = record["defense_risk"]
        self.assertEqual(defense_risk["actual_discard"]["tile"], "5m")
        self.assertFalse(defense_risk["actual_discard"]["calibrated_probability"])
        self.assertIn("mostly_live", defense_risk["actual_discard"]["danger_reasons"])
        self.assertEqual(defense_risk["predictions"]["defense_context_linear"]["tile"], "4m")
        self.assertIn(
            "genbutsu",
            defense_risk["predictions"]["defense_context_linear"]["safety_reasons"],
        )

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
                            "defense_risk": {
                                "actual_discard": {
                                    "tile": "5m",
                                    "risk": 0.2,
                                    "calibrated_probability": False,
                                    "active_riichi_opponents": 1,
                                    "safety_reasons": ["genbutsu"],
                                    "danger_reasons": [],
                                },
                                "predictions": {
                                    "risk_context_linear": {
                                        "tile": "5m",
                                        "risk": 0.2,
                                        "calibrated_probability": False,
                                        "active_riichi_opponents": 1,
                                        "safety_reasons": ["genbutsu"],
                                        "danger_reasons": [],
                                    },
                                    "defense_context_linear": {
                                        "tile": "8m",
                                        "risk": 0.8,
                                        "calibrated_probability": False,
                                        "active_riichi_opponents": 1,
                                        "safety_reasons": [],
                                        "danger_reasons": ["mostly_live"],
                                    },
                                },
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
        self.assertIn(
            "defense_risk: actual=5m:0.200 risk_context_linear=5m:0.200 "
            "defense_context_linear=8m:0.800",
            examples_stdout.getvalue(),
        )
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
