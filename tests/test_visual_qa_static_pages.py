from __future__ import annotations

import importlib.util
import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory
from types import ModuleType


class VisualQaStaticPagesTests(unittest.TestCase):
    def test_generate_visual_qa_pages_writes_expected_static_targets(self) -> None:
        module = _load_visual_qa_module()
        with TemporaryDirectory() as directory:
            output_dir = Path(directory) / "pages"
            pages = module.generate_visual_qa_pages(output_dir)

            slugs = {page["slug"] for page in pages}
            self.assertEqual(
                slugs,
                {
                    "browser-demo",
                    "serve-index",
                    "benchmark-dashboard",
                    "training-dashboard",
                    "replay-viewer",
                    "interpretability-overlay",
                },
            )
            for page in pages:
                html = (output_dir / page["path"]).read_text(encoding="utf-8")
                self.assertIn("<!doctype html>", html)
                self.assertIn("width=device-width, initial-scale=1", html)
            self.assertTrue((output_dir / "browser-demo" / "styles.css").is_file())
            self.assertTrue((output_dir / "browser-demo" / "demo.js").is_file())

    def test_parse_playwright_raw_json_accepts_encoded_cli_output(self) -> None:
        module = _load_visual_qa_module()
        encoded = json.dumps('{"viewport":390,"doc":390,"body":390,"text":120}')

        metrics = module._parse_playwright_raw_json(encoded)

        self.assertEqual(metrics["viewport"], 390)
        self.assertEqual(metrics["doc"], 390)

    def test_visual_metric_assertions_reject_blank_and_overflow_pages(self) -> None:
        module = _load_visual_qa_module()
        module._assert_visual_metrics(
            {"doc": 390, "body": 390, "text": 40},
            viewport_width=390,
            page="ok.html",
        )
        with self.assertRaisesRegex(RuntimeError, "appears blank"):
            module._assert_visual_metrics(
                {"doc": 390, "body": 390, "text": 0},
                viewport_width=390,
                page="blank.html",
            )
        with self.assertRaisesRegex(RuntimeError, "overflows viewport"):
            module._assert_visual_metrics(
                {"doc": 420, "body": 390, "text": 40},
                viewport_width=390,
                page="wide.html",
            )


def _load_visual_qa_module() -> ModuleType:
    path = Path(__file__).resolve().parents[1] / "scripts" / "visual_qa_static_pages.py"
    spec = importlib.util.spec_from_file_location("visual_qa_static_pages", path)
    if spec is None or spec.loader is None:
        raise AssertionError("failed to load visual QA script")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


if __name__ == "__main__":
    unittest.main()
