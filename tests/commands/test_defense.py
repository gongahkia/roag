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


class DefenseCommandTests(CliCommandTests):
    def test_defense_risk_summary_outputs_text_json_and_report(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "defense-risk.json"

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    [
                        "defense-risk-summary",
                        "data/fixtures/tenhou",
                        "--report",
                        str(report),
                        "--source-label",
                        "fixture-defense-risk",
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["defense-risk-summary", "data/fixtures/tenhou", "--json"])
            json_payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("examples: 4", text_stdout.getvalue())
        self.assertIn("active_riichi_examples: 1", text_stdout.getvalue())
        self.assertIn("actual_mean_risk: 0.1775", text_stdout.getvalue())
        self.assertIn("outcome_labeled_examples: 4", text_stdout.getvalue())
        self.assertIn("eventual_deal_in_examples: 1", text_stdout.getvalue())
        self.assertIn("eventual_deal_in_mean_risk: 0.7100", text_stdout.getvalue())
        self.assertIn("active_riichi_deal_in_examples: 1", text_stdout.getvalue())
        self.assertIn("actual_risk_bands: low=3 medium=0 high=1", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertEqual(report_payload["kind"], "kenjaku-defense-risk-summary-v0")
        self.assertEqual(report_payload["source"]["label"], "fixture-defense-risk")
        self.assertEqual(report_payload["xml_file_count"], 3)
        self.assertEqual(report_payload["examples"], 4)
        self.assertEqual(report_payload["active_riichi_examples"], 1)
        self.assertFalse(report_payload["calibrated_probability"])
        self.assertEqual(
            report_payload["outcome_analysis"]["kind"],
            "kenjaku-defense-risk-outcome-analysis-v0",
        )
        self.assertEqual(report_payload["outcome_analysis"]["labeled_examples"], 4)
        self.assertEqual(
            report_payload["outcome_analysis"]["buckets"]["eventual_deal_in"]["examples"],
            1,
        )
        self.assertEqual(report_payload["parse_failures"]["count"], 0)
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(json_payload["kind"], "kenjaku-defense-risk-summary-v0")
        self.assertEqual(json_payload["actual_discard_risk"]["mean"], 0.1775)
        self.assertEqual(json_payload["outcome_analysis"]["missing_outcomes"], 0)

    def test_benchmark_deal_in_outputs_text_json_and_report(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "deal-in.json"

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(
                    [
                        "benchmark-deal-in",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "2",
                        "--report",
                        str(report),
                        "--source-label",
                        "fixture-deal-in",
                    ]
                )
            report_payload = json.loads(report.read_text(encoding="utf-8"))

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(
                    [
                        "benchmark-deal-in",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "2",
                        "--json",
                    ]
                )
            json_payload = json.loads(json_stdout.getvalue())

        self.assertEqual(text_exit_code, 0)
        self.assertIn("examples: 4", text_stdout.getvalue())
        self.assertIn("direct_deal_in_examples: 1", text_stdout.getvalue())
        self.assertIn("active_riichi_examples: 1", text_stdout.getvalue())
        self.assertIn("model: deal-in-linear-v0", text_stdout.getvalue())
        self.assertIn("eval_brier_score:", text_stdout.getvalue())
        self.assertIn("heuristic_eval_brier_score:", text_stdout.getvalue())
        self.assertIn("eval_best_threshold:", text_stdout.getvalue())
        self.assertIn("report_path:", text_stdout.getvalue())
        self.assertEqual(report_payload["kind"], "kenjaku-deal-in-benchmark-report-v0")
        self.assertEqual(report_payload["source"]["label"], "fixture-deal-in")
        self.assertEqual(report_payload["deal_in_examples"], 4)
        self.assertEqual(report_payload["label_summary"]["direct_deal_in_examples"], 1)
        self.assertEqual(report_payload["model"]["kind"], "deal-in-linear-v0")
        self.assertEqual(report_payload["calibration"]["target"], "deal_in")
        self.assertEqual(len(report_payload["calibration"]["thresholds"]), 21)
        self.assertIsNotNone(report_payload["calibration"]["train"]["best"])
        self.assertIsNotNone(report_payload["calibration"]["eval"]["best"])
        self.assertFalse(report_payload["heuristic_risk_baseline"]["calibrated_probability"])
        self.assertEqual(report_payload["parse_failures"]["count"], 0)
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(json_payload["kind"], "kenjaku-deal-in-benchmark-report-v0")
        self.assertEqual(json_payload["label_summary"]["examples"], 4)
        self.assertEqual(json_payload["calibration"]["target"], "deal_in")

    def test_train_placement_and_probability_commands(self) -> None:
        with TemporaryDirectory() as directory:
            checkpoint = Path(directory) / "placement.json"

            train_stdout = io.StringIO()
            with contextlib.redirect_stdout(train_stdout):
                train_exit_code = main(
                    [
                        "train-placement",
                        "--data",
                        "data/fixtures/tenhou",
                        "--output",
                        str(checkpoint),
                        "--epochs",
                        "3",
                    ]
                )
            checkpoint_payload = json.loads(checkpoint.read_text(encoding="utf-8"))

            probability_stdout = io.StringIO()
            with contextlib.redirect_stdout(probability_stdout):
                probability_exit_code = main(
                    [
                        "placement-probability",
                        "--model",
                        str(checkpoint),
                        "--scores",
                        "25000,25000,25000,25000",
                        "--kyoku",
                        "E1",
                        "--output",
                        "json",
                    ]
                )
            probability_payload = json.loads(probability_stdout.getvalue())

        self.assertEqual(train_exit_code, 0)
        self.assertIn("examples: 12", train_stdout.getvalue())
        self.assertIn("checkpoint_path:", train_stdout.getvalue())
        self.assertIn("sandbox estimate, not published placement stats", train_stdout.getvalue())
        self.assertEqual(checkpoint_payload["kind"], "kenjaku-placement-checkpoint-v0")
        self.assertEqual(checkpoint_payload["metadata"]["examples"], 12)
        self.assertEqual(checkpoint_payload["model"]["kind"], "kenjaku-placement-linear-v0")
        self.assertEqual(probability_exit_code, 0)
        self.assertEqual(probability_payload["kind"], "kenjaku-placement-probability-v0")
        self.assertEqual(len(probability_payload["probabilities"]), 4)
        self.assertAlmostEqual(sum(probability_payload["probabilities"]), 1.0)
        self.assertEqual(
            probability_payload["disclaimer"],
            "sandbox estimate, not published placement stats",
        )

    def test_benchmark_report_summary_supports_deal_in_reports(self) -> None:
        with TemporaryDirectory() as directory:
            report = Path(directory) / "deal-in.json"
            with contextlib.redirect_stdout(io.StringIO()):
                main(
                    [
                        "benchmark-deal-in",
                        "data/fixtures/tenhou",
                        "--epochs",
                        "2",
                        "--report",
                        str(report),
                        "--source-label",
                        "fixture-deal-in",
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
        self.assertIn("model: deal-in-linear-v0 feature_dim=26", text_stdout.getvalue())
        self.assertIn("calibration: target=deal_in", text_stdout.getvalue())
        self.assertIn("heuristic_eval:", text_stdout.getvalue())
        self.assertIn("eval_brier_score_vs_heuristic:", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-benchmark-summary-v0")
        self.assertEqual(payload["reports"][0]["target"], "deal_in")
        self.assertEqual(payload["reports"][0]["label_summary"]["direct_deal_in_examples"], 1)
        self.assertEqual(payload["reports"][0]["calibration"]["target"], "deal_in")
        self.assertIsNotNone(payload["reports"][0]["calibration"]["train_best_threshold"])
        self.assertIsNotNone(payload["reports"][0]["calibration"]["eval_best_threshold"])
        self.assertIn("eval_brier_score_vs_heuristic", payload["reports"][0]["deltas"])
