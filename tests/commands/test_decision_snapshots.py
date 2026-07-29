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

SANMA_FIXTURES_DIR = Path("data/fixtures/sanma")


class DecisionSnapshotCommandTests(CliCommandTests):
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
            rows = [json.loads(line) for line in output.read_text(encoding="utf-8").splitlines()]

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
        self.assertNotIn("terminal_outcome", rows[0])

    def test_export_decision_snapshots_can_include_outcome_labels(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            output = Path(directory) / "call-snapshots.jsonl"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "export-decision-snapshots",
                        "data/fixtures/tenhou/events_4p.xml",
                        "--decision-types",
                        "call",
                        "--include-outcome",
                        "--output",
                        str(output),
                    ]
                )
            rows = [json.loads(line) for line in output.read_text(encoding="utf-8").splitlines()]

        self.assertEqual(exit_code, 0)
        self.assertEqual(len(rows), 1)
        self.assertEqual(rows[0]["terminal_outcome"]["kind"], "agari")
        self.assertEqual(rows[0]["terminal_outcome"]["winner_seats"], [2])
        self.assertEqual(rows[0]["terminal_outcome"]["from_seats"], [1])
        self.assertIsNone(rows[0]["terminal_outcome"]["score_deltas"])
        self.assertIn("snapshots: 1", stdout.getvalue())

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
            rows = [json.loads(line) for line in output.read_text(encoding="utf-8").splitlines()]

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
            rows = [json.loads(line) for line in output.read_text(encoding="utf-8").splitlines()]

        self.assertEqual(exit_code, 0)
        self.assertGreaterEqual(len(rows), 1)
        self.assertTrue(all(row["decision_type"] == "riichi" for row in rows))
        self.assertTrue(all({"kind": "riichi"} in row["legal_actions"] for row in rows))

    def test_export_decision_snapshots_exports_sanma_discard_riichi_and_kita(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            output = Path(directory) / "sanma-snapshots.jsonl"
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "export-decision-snapshots",
                        str(SANMA_FIXTURES_DIR),
                        "--decision-types",
                        "discard,riichi,kita",
                        "--output",
                        str(output),
                    ]
                )
            rows = [json.loads(line) for line in output.read_text(encoding="utf-8").splitlines()]

        decision_types = {row["decision_type"] for row in rows}
        self.assertEqual(exit_code, 0)
        self.assertEqual(decision_types, {"discard", "riichi", "kita"})
        self.assertTrue(all(len(row["scores"]) == 3 for row in rows))
        self.assertTrue(all(len(row["active_riichi_seats"]) == 3 for row in rows))
        self.assertIn("decision_types: discard,riichi,kita", stdout.getvalue())

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
            rows = [json.loads(line) for line in snapshots.read_text(encoding="utf-8").splitlines()]
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

    def test_produce_decision_predictions_echo_actual_round_trips(self) -> None:
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

            producer_stdout = io.StringIO()
            with contextlib.redirect_stdout(producer_stdout):
                producer_exit_code = main(
                    [
                        "produce-decision-predictions",
                        str(snapshots),
                        "--strategy",
                        "echo-actual",
                        "--output",
                        str(predictions),
                    ]
                )

            compare_stdout = io.StringIO()
            with contextlib.redirect_stdout(compare_stdout):
                compare_exit_code = main(
                    ["decision-snapshot-compare", str(snapshots), str(predictions)]
                )
            prediction_rows = [
                json.loads(line) for line in predictions.read_text(encoding="utf-8").splitlines()
            ]

        self.assertEqual(producer_exit_code, 0)
        self.assertIn("strategy: echo-actual", producer_stdout.getvalue())
        self.assertIn("predictions: 5", producer_stdout.getvalue())
        self.assertEqual(compare_exit_code, 0)
        self.assertIn("overall_accuracy: 1.0000", compare_stdout.getvalue())
        self.assertTrue(all("row_id" in row for row in prediction_rows))
        self.assertTrue(all("predicted_action" in row for row in prediction_rows))
