from __future__ import annotations

from tests.commands.conftest import (
    CliCommandTests,
    Path,
    TemporaryDirectory,
    contextlib,
    importlib,
    io,
    json,
    main,
)


class TransformerCommandTests(CliCommandTests):
    def test_train_discard_transformer_fixture_smoke_writes_report_artifact(self) -> None:
        if importlib.util.find_spec("torch") is None:
            self.skipTest("PyTorch is not available")
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "transformer.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "train-discard-transformer",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--batch-size",
                        "2",
                        "--model-dim",
                        "16",
                        "--num-heads",
                        "4",
                        "--num-layers",
                        "1",
                        "--feedforward-dim",
                        "32",
                        "--dropout",
                        "0.0",
                        "--value-head",
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
                        "fixture-transformer",
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertIn("device: cpu", stdout.getvalue())
        self.assertIn("model: discard-transformer-policy-v0", stdout.getvalue())
        self.assertIn("encoder: mahjong-transformer-encoder-v0", stdout.getvalue())
        self.assertIn("value_head: yes", stdout.getvalue())
        self.assertIn("input_tokens: 152", stdout.getvalue())
        self.assertIn("report_path:", stdout.getvalue())
        self.assertEqual(payload["kind"], "kenjaku-discard-transformer-report-v0")
        self.assertEqual(payload["source"]["label"], "fixture-transformer")
        self.assertEqual(payload["discard_examples"], 4)
        self.assertEqual(payload["split"]["train_examples"], 3)
        self.assertEqual(payload["split"]["eval_examples"], 1)
        self.assertEqual(payload["model"]["kind"], "discard-transformer-policy-v0")
        self.assertEqual(payload["model"]["encoder_kind"], "mahjong-transformer-encoder-v0")
        self.assertEqual(payload["model"]["input_tokens"], 152)
        self.assertTrue(payload["model"]["value_head"])
        self.assertEqual(payload["model"]["config"]["model_dim"], 16)
        self.assertEqual(payload["model"]["config"]["num_layers"], 1)
        self.assertEqual(payload["training"]["device"], "cpu")
        self.assertEqual(payload["training"]["seed"], 123)
        self.assertEqual(payload["training"]["best_epoch"], 1)
        self.assertEqual(payload["training_history"]["step_unit"], "epoch")
        self.assertEqual(payload["training_history"]["records"][0]["step"], 1)
        self.assertEqual(len(payload["training_history"]["curves"]["eval_accuracy"]), 1)
        self.assertEqual(payload["metrics"]["train"]["examples"], 3)
        self.assertEqual(payload["metrics"]["eval"]["examples"], 1)
        self.assertIsNone(payload["artifacts"]["checkpoint_path"])

    def test_transformer_attention_overlay_writes_html(self) -> None:
        if importlib.util.find_spec("torch") is None:
            self.skipTest("PyTorch is not available")
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            root = Path(directory)
            checkpoint = root / "transformer.pt"
            snapshots = root / "snapshots.jsonl"
            output = root / "attention.html"
            with contextlib.redirect_stdout(io.StringIO()):
                train_exit_code = main(
                    [
                        "train-discard-transformer",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "0",
                        "--batch-size",
                        "2",
                        "--model-dim",
                        "16",
                        "--num-heads",
                        "4",
                        "--num-layers",
                        "1",
                        "--feedforward-dim",
                        "32",
                        "--dropout",
                        "0.0",
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
                    ]
                )
                snapshot_exit_code = main(
                    [
                        "export-decision-snapshots",
                        "data/fixtures/tenhou",
                        "--decision-types",
                        "discard",
                        "--limit",
                        "1",
                        "--output",
                        str(snapshots),
                    ]
                )
            with contextlib.redirect_stdout(stdout):
                overlay_exit_code = main(
                    [
                        "transformer-attention-overlay",
                        str(checkpoint),
                        str(snapshots),
                        "--output",
                        str(output),
                        "--limit",
                        "1",
                        "--max-heads",
                        "2",
                        "--device",
                        "cpu",
                    ]
                )
            html = output.read_text(encoding="utf-8")

        self.assertEqual(train_exit_code, 0)
        self.assertEqual(snapshot_exit_code, 0)
        self.assertEqual(overlay_exit_code, 0)
        self.assertIn("decisions: 1", stdout.getvalue())
        self.assertIn("Kenjaku Transformer Attention Overlay", html)
        self.assertIn("Head 0", html)

    def test_benchmark_report_summary_supports_synthetic_transformer_report(self) -> None:
        report_payload = {
            "kind": "kenjaku-discard-transformer-report-v0",
            "source": {"label": "synthetic-transformer", "command": None, "date": None},
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
                "kind": "discard-transformer-policy-v0",
                "encoder_kind": "mahjong-transformer-encoder-v0",
                "input_tokens": 152,
                "output_dim": 34,
                "config": {
                    "model_dim": 16,
                    "num_heads": 4,
                    "num_layers": 1,
                    "feedforward_dim": 32,
                    "dropout": 0.0,
                },
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
            report = Path(directory) / "transformer.json"
            report.write_text(json.dumps(report_payload), encoding="utf-8")

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["benchmark-report-summary", str(report)])

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["benchmark-report-summary", str(report), "--json"])
            payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn(
            "model: discard-transformer-policy-v0 encoder=mahjong-transformer-encoder-v0",
            text_stdout.getvalue(),
        )
        self.assertIn("tokens=152 dim=16 heads=4 layers=1", text_stdout.getvalue())
        self.assertIn("best: epoch=1 split=eval", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-discard-benchmark-summary-v0")
        self.assertEqual(payload["reports"][0]["target"], "discard_transformer")
        self.assertEqual(payload["reports"][0]["model"]["config"]["model_dim"], 16)

    def test_benchmark_discard_transformer_fixture_smoke_writes_report_artifact(self) -> None:
        if importlib.util.find_spec("torch") is None:
            self.skipTest("PyTorch is not available")
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            report = Path(directory) / "transformer-benchmark.json"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-discard-transformer",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "1",
                        "--batch-size",
                        "2",
                        "--model-dim",
                        "16",
                        "--num-heads",
                        "4",
                        "--num-layers",
                        "1",
                        "--feedforward-dim",
                        "32",
                        "--dropout",
                        "0.0",
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
                        "fixture-transformer-benchmark",
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertIn("device: cpu", stdout.getvalue())
        self.assertIn("discard_transformer_eval_accuracy:", stdout.getvalue())
        self.assertIn("report_path:", stdout.getvalue())
        self.assertEqual(payload["kind"], "kenjaku-discard-transformer-benchmark-report-v0")
        self.assertEqual(payload["source"]["label"], "fixture-transformer-benchmark")
        self.assertEqual(payload["discard_examples"], 4)
        self.assertEqual(
            payload["models"]["discard_transformer"]["kind"],
            "discard-transformer-policy-v0",
        )
        self.assertEqual(payload["models"]["discard_transformer"]["config"]["model_dim"], 16)
        self.assertIn("transformer_eval_accuracy_lift_over_defense_context", payload["deltas"])

    def test_benchmark_report_summary_supports_synthetic_transformer_benchmark(self) -> None:
        report_payload = {
            "kind": "kenjaku-discard-transformer-benchmark-report-v0",
            "source": {
                "label": "synthetic-transformer-benchmark",
                "command": None,
                "date": None,
            },
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
            "models": {
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
                "discard_transformer": {
                    "kind": "discard-transformer-policy-v0",
                    "encoder_kind": "mahjong-transformer-encoder-v0",
                    "input_tokens": 152,
                    "output_dim": 34,
                    "config": {
                        "model_dim": 16,
                        "num_heads": 4,
                        "num_layers": 1,
                        "feedforward_dim": 32,
                        "dropout": 0.0,
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
                        "eval": {"examples": 1, "loss": 2.0, "accuracy": 0.75},
                        "best": {
                            "train": {"examples": 3, "loss": 1.5, "accuracy": 2 / 3},
                            "eval": {"examples": 1, "loss": 2.0, "accuracy": 0.75},
                        },
                    },
                },
            },
            "deltas": {
                "transformer_eval_accuracy_lift_over_frequency": 0.75,
                "transformer_eval_accuracy_lift_over_risk_context": 0.5,
                "transformer_eval_accuracy_lift_over_defense_context": 0.25,
            },
            "discard_shanten": {"examples": 4},
            "parse_failures": {"count": 0, "items": []},
            "artifacts": {"checkpoint_path": None},
        }

        with TemporaryDirectory() as directory:
            report = Path(directory) / "transformer-benchmark.json"
            report.write_text(json.dumps(report_payload), encoding="utf-8")

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["benchmark-report-summary", str(report)])

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["benchmark-report-summary", str(report), "--json"])
            payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("discard_transformer: eval=0.7500", text_stdout.getvalue())
        self.assertIn(
            "transformer_eval_accuracy_lift_over_defense_context: +0.2500",
            text_stdout.getvalue(),
        )
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["reports"][0]["target"], "discard_transformer_benchmark")
        self.assertEqual(
            payload["reports"][0]["deltas"]["transformer_eval_accuracy_lift_over_defense_context"],
            0.25,
        )
