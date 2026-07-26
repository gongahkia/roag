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


class BrowserCommandTests(CliCommandTests):
    def test_browser_demo_writes_static_assets_without_serving(self) -> None:
        with TemporaryDirectory() as directory:
            output_dir = Path(directory) / "demo"
            stdout = io.StringIO()

            with contextlib.redirect_stdout(stdout):
                exit_code = main(
                    [
                        "browser-demo",
                        "--output-dir",
                        str(output_dir),
                        "--no-serve",
                    ]
                )

            index_html = (output_dir / "index.html").read_text(encoding="utf-8")
            styles_css = (output_dir / "styles.css").read_text(encoding="utf-8")
            demo_js = (output_dir / "demo.js").read_text(encoding="utf-8")
            policy_json = (output_dir / "policy.json").read_text(encoding="utf-8")

        self.assertEqual(exit_code, 0)
        self.assertIn("wrote browser demo:", stdout.getvalue())
        self.assertIn("open:", stdout.getvalue())
        self.assertIn("Kenjaku Browser Demo", index_html)
        self.assertIn("Legal Actions", index_html)
        self.assertIn("Dora", index_html)
        self.assertIn("Calls", index_html)
        self.assertIn("finishExhaustiveDraw", demo_js)
        self.assertIn("selectModelAction", demo_js)
        self.assertIn("kenjaku-browser-demo-ppo-policy-v0", policy_json)
        self.assertIn("sandbox-linear-ppo-actor-critic-v0", policy_json)
        self.assertIn("window.KenjakuDemo", demo_js)
        self.assertIn("setupAsciiField", demo_js)
        self.assertIn("setupDiscardTarget", demo_js)
        self.assertIn("playDiscardMotion", demo_js)
        self.assertIn("toggleMotion", demo_js)
        self.assertIn('id="ascii-field"', index_html)
        self.assertIn('id="discard-target"', index_html)
        self.assertIn('id="motion-button"', index_html)
        self.assertIn(".ascii-field", styles_css)
        self.assertIn(".demo-impact-heavy", styles_css)
        self.assertIn(".tile-flight", styles_css)
        self.assertIn(".tile", styles_css)
        self.assertNotIn("private", index_html + styles_css + demo_js + policy_json)
        self.assertNotIn(
            "replay",
            (index_html + styles_css + demo_js + policy_json).lower(),
        )

    def test_serve_writes_artifact_dashboard_without_serving(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory) / "runs"
            (root / "nested path").mkdir(parents=True)
            (root / "benchmark-dashboard").mkdir(parents=True)
            (root / "page.html").write_text("<h1>report</h1>", encoding="utf-8")
            (root / "report.json").write_text("{}", encoding="utf-8")
            (root / "video.mp4").write_bytes(b"mp4")
            (root / "nested path" / "data.jsonl").write_text("{}", encoding="utf-8")
            (root / "benchmark-dashboard" / "index.html").write_text(
                "<h1>bench</h1>",
                encoding="utf-8",
            )
            stdout = io.StringIO()

            with contextlib.redirect_stdout(stdout):
                exit_code = main(["serve", "--dir", str(root), "--no-serve"])

            index_html = (root / "index.html").read_text(encoding="utf-8")

        self.assertEqual(exit_code, 0)
        self.assertIn("wrote artifact dashboard:", stdout.getvalue())
        self.assertIn("open:", stdout.getvalue())
        self.assertIn("Kenjaku Artifact Dashboard", index_html)
        self.assertIn("serve-index kj-arcade-shell", index_html)
        self.assertIn("kenjaku arcade-card theme v0", index_html)
        self.assertIn("Quick Links", index_html)
        self.assertIn("Benchmark Dashboard", index_html)
        self.assertIn("Data Files", index_html)
        self.assertIn("nested path", index_html)
        self.assertIn('href="page.html"', index_html)
        self.assertIn('href="report.json"', index_html)
        self.assertIn('href="video.mp4"', index_html)
        self.assertIn('href="benchmark-dashboard/index.html"', index_html)
        self.assertIn('href="nested%20path/data.jsonl"', index_html)
        self.assertIn(">HTML<", index_html)
        self.assertIn(">JSON<", index_html)
        self.assertIn(">MP4<", index_html)
        self.assertIn(">JSONL<", index_html)
        self.assertIn("page |", index_html)
        self.assertIn("data |", index_html)
        self.assertIn("video | 3 B |", index_html)
        self.assertNotIn('href="index.html"', index_html)

    def test_demo_writes_fixture_quickstart_artifacts(self) -> None:
        with TemporaryDirectory() as directory:
            output_dir = Path(directory) / "demo"
            stdout = io.StringIO()

            with contextlib.redirect_stdout(stdout):
                exit_code = main(["demo", "--output-dir", str(output_dir)])

            index_html = (output_dir / "index.html").read_text(encoding="utf-8")
            manifest = json.loads((output_dir / "manifest.json").read_text(encoding="utf-8"))
            comparison = json.loads(
                (output_dir / "snapshot-comparison.json").read_text(encoding="utf-8")
            )
            benchmark = json.loads(
                (output_dir / "discard-benchmark.json").read_text(encoding="utf-8")
            )

        self.assertEqual(exit_code, 0)
        self.assertIn("Open", stdout.getvalue())
        self.assertIn("browser-demo/index.html", index_html)
        self.assertIn("benchmark-dashboard/index.html", index_html)
        self.assertEqual(manifest["kind"], "kenjaku-demo-manifest-v0")
        self.assertEqual(manifest["snapshot_count"], 8)
        self.assertEqual(manifest["prediction_count"], 8)
        self.assertEqual(comparison["overall"]["accuracy"], 1.0)
        self.assertEqual(benchmark["kind"], "kenjaku-discard-benchmark-report-v0")
        self.assertEqual(tuple(benchmark["models"]), ("frequency",))
