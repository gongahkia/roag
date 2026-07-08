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


class KitaCommandTests(CliCommandTests):
    def test_benchmark_kita_writes_report_and_summary(self) -> None:
        stdout = io.StringIO()

        with TemporaryDirectory() as directory:
            root = Path(directory)
            xml = root / "sanma-kita.xml"
            report = root / "kita-benchmark.json"
            xml.write_text(_sanma_kita_xml(), encoding="utf-8")

            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-kita",
                        str(xml),
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


def _sanma_kita_xml() -> str:
    return """
    <mjloggm>
      <UN n0="east" n1="south" n2="west" />
      <INIT
        seed="0,0,0,0,0,72"
        ten="350,350,350"
        oya="0"
        hai0="0,4,8,12,16,20,24,28,32,36,40,44,120"
        hai1="1,5,9,13,17,21,25,29,33,37,41,45,121"
        hai2="2,6,10,14,18,22,26,30,34,38,42,46,122"
      />
      <T123 />
      <N who="0" m="30752" />
      <T124 />
      <D124 />
      <RYUUKYOKU ten="350,350,350" />
    </mjloggm>
    """
