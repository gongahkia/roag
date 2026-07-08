from __future__ import annotations

import json
import re
import shutil
import subprocess
import unittest
from html.parser import HTMLParser
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.browser_demo import (
    BROWSER_DEMO_FILES,
    BROWSER_DEMO_KIND,
    FIXTURE_WALL_SEED,
    fixture_wall,
    write_browser_demo,
)


class _AssetParser(HTMLParser):
    def __init__(self) -> None:
        super().__init__()
        self.ids: set[str] = set()
        self.classes: set[str] = set()
        self.links: list[str] = []
        self.scripts: list[str] = []

    def handle_starttag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        values = dict(attrs)
        if "id" in values and values["id"] is not None:
            self.ids.add(values["id"])
        if "class" in values and values["class"] is not None:
            self.classes.update(values["class"].split())
        rel = values.get("rel") or ""
        if tag == "link" and "stylesheet" in rel.split() and values.get("href") is not None:
            self.links.append(str(values["href"]))
        if tag == "script" and values.get("src") is not None:
            self.scripts.append(str(values["src"]))


class BrowserDemoTests(unittest.TestCase):
    def test_write_browser_demo_writes_relative_static_assets(self) -> None:
        with TemporaryDirectory() as directory:
            output_dir = Path(directory)
            result = write_browser_demo(output_dir)
            html = (output_dir / "index.html").read_text(encoding="utf-8")
            css = (output_dir / "styles.css").read_text(encoding="utf-8")
            js = (output_dir / "demo.js").read_text(encoding="utf-8")

        self.assertEqual(result["kind"], BROWSER_DEMO_KIND)
        self.assertEqual([Path(path).name for path in result["files"]], list(BROWSER_DEMO_FILES))
        self.assertTrue(html.startswith("<!doctype html>"))
        self.assertIn("kenjaku-static-frontend-helpers-v0", html)
        self.assertIn('href="styles.css"', html)
        self.assertIn('src="demo.js"', html)
        self.assertIn("kj-table-surface", html)
        self.assertIn("action-rail", html)
        self.assertIn("kj-motion-lift", html)
        self.assertIn("kj-motion-confirm-flash", css)
        self.assertIn("window.KenjakuMotion", js)
        self.assertIn("countUp", js)
        for contents in (html, css, js):
            self.assertNotIn(directory, contents)
            self.assertNotIn("file://", contents)
            self.assertNotIn("http://", contents)
            self.assertNotIn("https://", contents)

    def test_html_assets_and_css_selectors_resolve(self) -> None:
        with TemporaryDirectory() as directory:
            output_dir = Path(directory)
            write_browser_demo(output_dir)
            html = (output_dir / "index.html").read_text(encoding="utf-8")
            css = (output_dir / "styles.css").read_text(encoding="utf-8")
            js = (output_dir / "demo.js").read_text(encoding="utf-8")

        parser = _AssetParser()
        parser.feed(html)
        self.assertEqual(parser.links, ["styles.css"])
        self.assertEqual(parser.scripts, ["demo.js"])
        selector_names = _css_selector_names(css)
        source_names = set(re.findall(r"[A-Za-z][A-Za-z0-9_-]+", html + js))
        optional_shared = {"is-disabled", "is-error"}
        missing = sorted(
            name
            for name in selector_names
            if name not in source_names
            and not name.startswith("kj-")
            and name not in optional_shared
        )
        self.assertEqual(missing, [])

    def test_arcade_table_dom_hooks_exist(self) -> None:
        with TemporaryDirectory() as directory:
            output_dir = Path(directory)
            write_browser_demo(output_dir)
            html = (output_dir / "index.html").read_text(encoding="utf-8")

        parser = _AssetParser()
        parser.feed(html)
        self.assertGreaterEqual(
            parser.ids,
            {
                "wall-meter",
                "call-zone-summary",
                "live-delta",
                "player-hand",
                "legal-actions",
                "scoreboard",
                "discard-grid",
                "call-grid",
                "event-log",
            },
        )
        self.assertGreaterEqual(
            parser.classes,
            {
                "table-hud",
                "table-core",
                "table-center",
                "player-console",
                "action-rail",
                "score-chip",
                "kj-motion-lift",
                "kj-motion-press",
            },
        )

    def test_demo_javascript_parses(self) -> None:
        node = shutil.which("node")
        if node is None:
            self.skipTest("node is not available")
        with TemporaryDirectory() as directory:
            output_dir = Path(directory)
            write_browser_demo(output_dir)
            script = output_dir / "demo.js"
            result = subprocess.run(
                [node, "--check", str(script)],
                check=False,
                capture_output=True,
                text=True,
            )

        self.assertEqual(result.returncode, 0, result.stderr)

    def test_fixture_wall_is_seed_deterministic(self) -> None:
        first = fixture_wall(FIXTURE_WALL_SEED)
        second = fixture_wall(FIXTURE_WALL_SEED)
        alternate = fixture_wall(f"{FIXTURE_WALL_SEED}:alternate")

        self.assertEqual(first, second)
        self.assertNotEqual(first, alternate)
        self.assertEqual(len(first), 16)
        self.assertEqual(sorted(first), sorted(alternate))

    def test_demo_javascript_embeds_default_fixture_wall(self) -> None:
        with TemporaryDirectory() as directory:
            output_dir = Path(directory)
            write_browser_demo(output_dir)
            js = (output_dir / "demo.js").read_text(encoding="utf-8")

        match = re.search(r"const FIXTURE_WALL = (\[.*?\]);", js)
        self.assertIsNotNone(match)
        assert match is not None
        self.assertEqual(tuple(json.loads(match.group(1))), fixture_wall())


def _css_selector_names(css: str) -> set[str]:
    names: set[str] = set()
    for match in re.finditer(r"([^{}]+)\{", css):
        selector = match.group(1)
        if selector.strip().startswith("@"):
            continue
        names.update(
            token[1:] for token in re.findall(r"(?<![\w-])([.#][A-Za-z_][\w-]*)", selector)
        )
    return names
