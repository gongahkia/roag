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


class TrainingCommandTests(CliCommandTests):
    def test_train_ppo_sandbox_command_writes_report_checkpoint_and_json(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "ppo.json"
            checkpoint = Path(directory) / "ppo-checkpoint.json"
            metrics_jsonl = Path(directory) / "ppo-metrics.jsonl"

            text_stdout = io.StringIO()
            text_stderr = io.StringIO()
            with contextlib.redirect_stdout(text_stdout), contextlib.redirect_stderr(text_stderr):
                text_exit_code = main(
                    [
                        "train-ppo-sandbox",
                        "--total-steps",
                        "16",
                        "--rollout-games",
                        "1",
                        "--max-rounds",
                        "1",
                        "--max-turns-per-round",
                        "16",
                        "--ppo-epochs",
                        "1",
                        "--batch-size",
                        "8",
                        "--hidden-dim",
                        "16",
                        "--device",
                        "cpu",
                        "--model-seed",
                        "123",
                        "--report",
                        str(report),
                        "--checkpoint",
                        str(checkpoint),
                        "--metrics-jsonl",
                        str(metrics_jsonl),
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))
            metric_lines = [
                line for line in text_stderr.getvalue().splitlines() if line.startswith("{")
            ]
            metric_file_lines = metrics_jsonl.read_text(encoding="utf-8").splitlines()
            metric_events = [json.loads(line) for line in metric_lines]

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout), contextlib.redirect_stderr(io.StringIO()):
                json_exit_code = main(
                    [
                        "train-ppo-sandbox",
                        "--total-steps",
                        "8",
                        "--rollout-games",
                        "1",
                        "--max-rounds",
                        "1",
                        "--max-turns-per-round",
                        "8",
                        "--ppo-epochs",
                        "1",
                        "--batch-size",
                        "4",
                        "--hidden-dim",
                        "16",
                        "--device",
                        "cpu",
                        "--json",
                    ]
                )
            json_payload = json.loads(json_stdout.getvalue())
            checkpoint_exists = checkpoint.exists()

        self.assertEqual(text_exit_code, 0)
        self.assertTrue(checkpoint_exists)
        self.assertIn("environment_steps:", text_stdout.getvalue())
        self.assertIn("ppo_policy_loss: yes", text_stdout.getvalue())
        self.assertIn("checkpoint_path:", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertEqual(report_payload["kind"], "kenjaku-ppo-sandbox-report-v0")
        self.assertGreaterEqual(report_payload["training"]["environment_steps"], 16)
        self.assertTrue(report_payload["capabilities"]["ppo_policy_loss"])
        self.assertTrue(report_payload["capabilities"]["ppo_value_loss"])
        self.assertTrue(report_payload["capabilities"]["gae_advantages"])
        self.assertTrue(report_payload["capabilities"]["clipped_objective"])
        self.assertTrue(report_payload["capabilities"]["entropy_regularization"])
        self.assertTrue(report_payload["capabilities"]["checkpointing"])
        self.assertTrue(report_payload["capabilities"]["resume_support"])
        self.assertFalse(report_payload["capabilities"]["learned_policy_environment_integration"])
        self.assertEqual(metric_file_lines, metric_lines)
        self.assertEqual(len(metric_events), len(report_payload["training"]["history"]))
        for event, row in zip(metric_events, report_payload["training"]["history"], strict=True):
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
            self.assertEqual(event["model"], "sandbox-linear-ppo-actor-critic-v0")
            self.assertEqual(event["epoch"], row["update"])
            self.assertIsInstance(event["train_loss"], float)
            self.assertIsInstance(event["eval_loss"], float)
            self.assertIsInstance(event["eval_accuracy"], float)
            self.assertIsInstance(event["elapsed_seconds"], float)
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(json_payload["kind"], "kenjaku-ppo-sandbox-report-v0")
        self.assertGreaterEqual(json_payload["training"]["environment_steps"], 8)

    def test_train_population_sandbox_command_writes_report_and_artifacts(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "population.json"
            output_dir = Path(directory) / "population"

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    [
                        "train-population-sandbox",
                        "--pool-size",
                        "4",
                        "--generations",
                        "1",
                        "--candidates-per-generation",
                        "1",
                        "--matchups-per-candidate",
                        "1",
                        "--total-steps",
                        "8",
                        "--max-rounds",
                        "1",
                        "--max-turns-per-round",
                        "8",
                        "--evaluation-max-rounds",
                        "1",
                        "--evaluation-max-turns-per-round",
                        "8",
                        "--output-dir",
                        str(output_dir),
                        "--report",
                        str(report),
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))
            artifact_count = len(list(output_dir.glob("*.json")))

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    [
                        "train-population-sandbox",
                        "--pool-size",
                        "4",
                        "--generations",
                        "1",
                        "--candidates-per-generation",
                        "1",
                        "--matchups-per-candidate",
                        "1",
                        "--total-steps",
                        "4",
                        "--max-rounds",
                        "1",
                        "--max-turns-per-round",
                        "4",
                        "--evaluation-max-rounds",
                        "1",
                        "--evaluation-max-turns-per-round",
                        "4",
                        "--json",
                    ]
                )
            json_payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("pool_size: 4", text_stdout.getvalue())
        self.assertIn("matchups: 5", text_stdout.getvalue())
        self.assertIn("promotion_decisions:", text_stdout.getvalue())
        self.assertIn("output_dir:", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertEqual(artifact_count, 5)
        self.assertEqual(report_payload["kind"], "kenjaku-population-sandbox-report-v0")
        self.assertEqual(len(report_payload["pool"]), 4)
        self.assertEqual(len(report_payload["snapshots"]), 5)
        self.assertEqual(report_payload["matchup_counts"]["total"], 5)
        self.assertTrue(report_payload["capabilities"]["policy_snapshot_pool"])
        self.assertTrue(report_payload["capabilities"]["opponent_sampling"])
        self.assertTrue(report_payload["capabilities"]["promotion_criteria"])
        self.assertFalse(report_payload["capabilities"]["learned_policy_environment_integration"])
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(json_payload["kind"], "kenjaku-population-sandbox-report-v0")
        self.assertEqual(json_payload["pool_size"], 4)

    def test_training_dashboard_writes_static_html_from_metrics_jsonl(self) -> None:
        def write_jsonl(path: Path, rows: list[dict[str, object]]) -> None:
            path.write_text(
                "\n".join(json.dumps(row, sort_keys=True) for row in rows) + "\n",
                encoding="utf-8",
            )

        with TemporaryDirectory() as directory:
            root = Path(directory)
            run_a = root / "mlp-a.metrics.jsonl"
            run_b = root / "transformer-b.metrics.jsonl"
            output = root / "dashboards" / "training" / "index.html"
            write_jsonl(
                run_a,
                [
                    {
                        "run_id": "mlp-a",
                        "epoch": 1,
                        "metrics": {
                            "train_loss": 1.4,
                            "eval_loss": 1.6,
                            "eval_accuracy": 0.25,
                            "epoch_seconds": 3.5,
                        },
                        "hyperparameters": {
                            "learning_rate": 0.001,
                            "batch_size": 8,
                            "model": "discard-mlp-v0",
                        },
                    },
                    {
                        "run_id": "mlp-a",
                        "epoch": 2,
                        "metrics": {
                            "train_loss": 1.1,
                            "eval_loss": 1.3,
                            "eval_accuracy": 0.5,
                            "epoch_seconds": 3.2,
                        },
                        "hyperparameters": {
                            "learning_rate": 0.001,
                            "batch_size": 8,
                            "model": "discard-mlp-v0",
                        },
                    },
                ],
            )
            write_jsonl(
                run_b,
                [
                    {
                        "run_id": "transformer-b",
                        "epoch": 1,
                        "metrics": {
                            "train_loss": 1.5,
                            "eval_loss": 1.4,
                            "eval_accuracy": 0.75,
                            "epoch_seconds": 7.0,
                        },
                        "hyperparameters": {
                            "learning_rate": 0.0005,
                            "batch_size": 4,
                            "model": "discard-transformer-policy-v0",
                        },
                    }
                ],
            )

            stdout = io.StringIO()
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "training-dashboard",
                        str(run_a),
                        str(run_b),
                        "--output",
                        str(output),
                        "--title",
                        "Fixture Training Runs",
                    ]
                )
            html = output.read_text(encoding="utf-8")

        self.assertEqual(exit_code, 0)
        self.assertIn("wrote training dashboard:", stdout.getvalue())
        self.assertIn("Fixture Training Runs", html)
        self.assertIn("No browser network access required", html)
        self.assertIn("mlp-a", html)
        self.assertIn("transformer-b", html)
        self.assertIn("mlp-a.metrics.jsonl", html)
        self.assertIn("Train Loss", html)
        self.assertIn("Eval Loss", html)
        self.assertIn("Accuracy", html)
        self.assertIn("Epoch Time", html)
        self.assertIn("Learning Rate", html)
        self.assertIn("discard-transformer-policy-v0", html)
        self.assertIn("data-sort-column", html)
        self.assertIn('aria-label="train_loss chart"', html)
        self.assertIn('aria-label="eval_loss chart"', html)
        self.assertIn('aria-label="eval_accuracy chart"', html)
        self.assertIn('aria-label="epoch_seconds chart"', html)
        self.assertIn("<polyline", html)
