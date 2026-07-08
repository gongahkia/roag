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


class BenchmarkDashboardCommandTests(CliCommandTests):
    def test_benchmark_dashboard_writes_public_static_html(self) -> None:
        def report_payload(
            label: str,
            *,
            eval_accuracy: float,
            balanced_accuracy: float,
        ) -> dict[str, object]:
            return {
                "kind": "kenjaku-call-benchmark-report-v0",
                "source": {
                    "label": label,
                    "command": "kenjaku benchmark-call local/private/raw.xml",
                    "date": "2026-06-12",
                },
                "xml_file_count": 1,
                "rounds": 2,
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
                            "eval_accuracy": eval_accuracy,
                            "eval_balanced_accuracy": balanced_accuracy,
                            "eval_pass_recall": 0.76,
                            "eval_call_recall": 0.72,
                        },
                    },
                },
            }

        with TemporaryDirectory() as directory:
            reports = [
                Path(directory) / "call-a.json",
                Path(directory) / "call-b.json",
                Path(directory) / "call-c.json",
            ]
            output = Path(directory) / "public" / "index.html"
            payloads = [
                report_payload(
                    "fixture-call-a",
                    eval_accuracy=0.78,
                    balanced_accuracy=0.74,
                ),
                report_payload(
                    "fixture-call-b",
                    eval_accuracy=0.82,
                    balanced_accuracy=0.80,
                ),
                report_payload(
                    "fixture-call-c",
                    eval_accuracy=0.74,
                    balanced_accuracy=0.70,
                ),
            ]
            for report, payload in zip(reports, payloads, strict=True):
                report.write_text(json.dumps(payload), encoding="utf-8")

            stdout = io.StringIO()
            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "benchmark-dashboard",
                        *(str(report) for report in reports),
                        "--output",
                        str(output),
                        "--title",
                        "Fixture Public Benchmarks",
                    ]
                )
            html = output.read_text(encoding="utf-8")

        self.assertEqual(exit_code, 0)
        self.assertIn("wrote public benchmark dashboard:", stdout.getvalue())
        self.assertIn("Fixture Public Benchmarks", html)
        self.assertIn("Report Comparison", html)
        self.assertIn("fixture-call-a", html)
        self.assertIn("fixture-call-b", html)
        self.assertIn("fixture-call-c", html)
        self.assertIn("call_linear_v1_calibrated", html)
        self.assertIn("0.820", html)
        self.assertIn("balanced_accuracy", html)
        self.assertIn("data-sort-column", html)
        self.assertIn("diff-best", html)
        self.assertIn("diff-down", html)
        self.assertIn('class="sparkline"', html)
        self.assertIn("call-a.json", html)
        self.assertIn("call-b.json", html)
        self.assertIn("call-c.json", html)
        self.assertIn("Report JSON", html)
        self.assertIn("Live rank tracking: not included", html)
        self.assertIn("requires explicit platform permission", html)
        self.assertNotIn("local/private/raw.xml", html)
        self.assertNotIn("current live rank", html.lower())
