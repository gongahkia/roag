from __future__ import annotations

from tests.commands.conftest import (
    CliCommandTests,
    Path,
    TemporaryDirectory,
    contextlib,
    io,
    json,
    main,
    sys,
)


class ExternalBaselineCommandTests(CliCommandTests):
    def test_external_prediction_producer_round_trips_through_subprocess_boundary(self) -> None:
        with TemporaryDirectory() as directory:
            snapshots = Path(directory) / "snapshots.jsonl"
            predictions = Path(directory) / "predictions.jsonl"
            compare_report = Path(directory) / "compare.json"
            producer = Path(directory) / "producer.py"
            producer.write_text(
                "\n".join(
                    [
                        "import json",
                        "import os",
                        "snapshots = os.environ['KENJAKU_SNAPSHOTS']",
                        "predictions = os.environ['KENJAKU_PREDICTIONS']",
                        "with open(snapshots, encoding='utf-8') as source, "
                        "open(predictions, 'w', encoding='utf-8') as target:",
                        "    for line in source:",
                        "        row = json.loads(line)",
                        "        target.write(json.dumps({"
                        "'row_id': row['row_id'], "
                        "'predicted_action': row['actual_action']"
                        "}) + '\\n')",
                    ]
                ),
                encoding="utf-8",
            )
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

            stdout = io.StringIO()
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "run-external-prediction-producer",
                        str(snapshots),
                        "--output",
                        str(predictions),
                        "--compare-report",
                        str(compare_report),
                        "--command",
                        sys.executable,
                        str(producer),
                    ]
                )
            comparison = json.loads(compare_report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertIn("command_returncode: 0", stdout.getvalue())
        self.assertIn("predictions: 5", stdout.getvalue())
        self.assertIn("compare_report_path:", stdout.getvalue())
        self.assertEqual(comparison["kind"], "kenjaku-decision-snapshot-comparison-v0")
        self.assertEqual(comparison["overall"]["accuracy"], 1.0)

    def test_external_baseline_report_compares_named_prediction_files(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            snapshots = root / "snapshots.jsonl"
            echo_predictions = root / "echo.jsonl"
            pass_predictions = root / "pass.jsonl"
            first_predictions = root / "first.jsonl"
            report_path = root / "external-baselines.json"
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
                for strategy, output in (
                    ("echo-actual", echo_predictions),
                    ("pass", pass_predictions),
                    ("first-legal", first_predictions),
                ):
                    main(
                        [
                            "produce-decision-predictions",
                            str(snapshots),
                            "--strategy",
                            strategy,
                            "--output",
                            str(output),
                        ]
                    )

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    [
                        "external-baseline-report",
                        str(snapshots),
                        "--baseline",
                        f"kenjaku:echo-actual={echo_predictions}",
                        "--baseline",
                        f"mortal-compatible:pass-smoke={pass_predictions}",
                        "--baseline",
                        f"akochan-compatible:first-legal-smoke={first_predictions}",
                        "--min-decisions",
                        "1",
                        "--json",
                    ]
                )
            payload = json.loads(json_stdout.getvalue())

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    [
                        "external-baseline-report",
                        str(snapshots),
                        "--baseline",
                        f"kenjaku:echo-actual={echo_predictions}",
                        "--baseline",
                        f"mortal-compatible:pass-smoke={pass_predictions}",
                        "--baseline",
                        f"akochan-compatible:first-legal-smoke={first_predictions}",
                        "--min-decisions",
                        "1",
                        "--report",
                        str(report_path),
                    ]
                )
            saved_report = json.loads(report_path.read_text(encoding="utf-8"))

        self.assertEqual(json_exit_code, 0)
        self.assertEqual(text_exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-external-baseline-report-v0")
        self.assertEqual(payload["protocol"]["comparison_unit"], "decision")
        self.assertTrue(payload["minimum_satisfied"])
        self.assertEqual(payload["snapshots"]["snapshots"], 5)
        self.assertEqual(len(payload["baselines"]), 3)
        self.assertEqual(payload["baselines"][0]["family"], "kenjaku")
        self.assertEqual(payload["baselines"][0]["comparable_decisions"], 5)
        self.assertEqual(payload["baselines"][0]["overall"]["accuracy"], 1.0)
        self.assertEqual(payload["baselines"][0]["overall"]["accuracy_ci"]["method"], "wilson")
        self.assertIn("mortal-compatible:pass-smoke", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertEqual(saved_report["kind"], "kenjaku-external-baseline-report-v0")

    def test_external_baseline_report_enforces_minimum_decisions(self) -> None:
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
                        "2",
                    ]
                )
                main(
                    [
                        "produce-decision-predictions",
                        str(snapshots),
                        "--strategy",
                        "echo-actual",
                        "--output",
                        str(predictions),
                    ]
                )

            with self.assertRaises(SystemExit) as raised:
                main(
                    [
                        "external-baseline-report",
                        str(snapshots),
                        "--baseline",
                        f"kenjaku:echo-actual={predictions}",
                        "--min-decisions",
                        "3",
                    ]
                )

        self.assertIn("below minimum comparable decisions", str(raised.exception))
