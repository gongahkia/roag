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

SANMA_KITA_FIXTURE = Path("data/fixtures/sanma/sanma_kita_3p.xml")


class KitaCommandTests(CliCommandTests):
    def test_benchmark_kita_accepts_tenhou_sanma_empty_fourth_seat(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            root = Path(directory)
            input_path = root / "tenhou-sanma.xml"
            input_path.write_text(
                SANMA_KITA_FIXTURE.read_text(encoding="utf-8").replace(
                    'hai2="2,6,10,14,18,22,26,30,34,38,42,46,122"',
                    'hai2="2,6,10,14,18,22,26,30,34,38,42,46,122" hai3=""',
                ),
                encoding="utf-8",
            )
            report = root / "kita-benchmark.json"

            with contextlib.redirect_stdout(stdout):
                exit_code = main(["benchmark-kita", str(input_path), "--report", str(report)])
            payload = json.loads(report.read_text(encoding="utf-8"))

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["kita_examples"], 2)
        self.assertIn("kita_examples: 2", stdout.getvalue())

    def test_benchmark_kita_writes_report_and_summary(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            root = Path(directory)
            report = root / "kita-benchmark.json"

            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-kita",
                        str(SANMA_KITA_FIXTURE),
                        "--eval-fraction",
                        "0.5",
                        "--split-seed",
                        "fixed",
                        "--report",
                        str(report),
                    ]
                )
            payload = json.loads(report.read_text(encoding="utf-8"))

            text_stdout = io.StringIO()
            with contextlib.redirect_stdout(text_stdout):
                text_exit_code = main(["benchmark-report-summary", str(report)])

            json_stdout = io.StringIO()
            with contextlib.redirect_stdout(json_stdout):
                json_exit_code = main(["benchmark-report-summary", str(report), "--json"])
            summary = json.loads(json_stdout.getvalue())

        self.assertEqual(exit_code, 0)
        self.assertEqual(payload["kind"], "kenjaku-kita-benchmark-report-v0")
        self.assertEqual(payload["kita_examples"], 2)
        self.assertEqual(payload["kita_examples_total"], 2)
        self.assertEqual(payload["split"]["train_examples"], 1)
        self.assertEqual(payload["split"]["eval_examples"], 1)
        self.assertEqual(set(payload["models"]), {"kita_frequency"})
        model = payload["models"]["kita_frequency"]
        self.assertEqual(model["kind"], "kita-frequency-v0")
        self.assertEqual(set(model["counts"]), {"pass", "kita"})
        self.assertEqual(sum(model["counts"].values()), 1)
        self.assertEqual(model["metrics"]["loss_kind"], "zero_one")
        self.assertIn("kita_examples: 2", stdout.getvalue())
        self.assertIn("kita_frequency_eval_kita_recall:", stdout.getvalue())
        self.assertEqual(text_exit_code, 0)
        self.assertIn("kita_frequency", text_stdout.getvalue())
        self.assertIn("kita_recall=", text_stdout.getvalue())
        self.assertEqual(json_exit_code, 0)
        self.assertEqual(summary["kind"], "kenjaku-benchmark-summary-v0")
        self.assertEqual(summary["reports"][0]["target"], "kita")
        self.assertIn("eval_kita_recall", summary["reports"][0]["models"]["kita_frequency"])
