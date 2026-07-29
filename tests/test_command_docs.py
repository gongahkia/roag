from __future__ import annotations

import unittest
from pathlib import Path

from kenjaku.cli import build_parser


class CommandDocsTests(unittest.TestCase):
    def test_every_cli_command_has_runbook(self) -> None:
        parser = build_parser()
        subparsers = next(action for action in parser._actions if action.dest == "command")
        commands = sorted(subparsers.choices)
        docs_dir = Path(__file__).resolve().parents[1] / "docs" / "commands"
        docs = sorted(path.stem for path in docs_dir.glob("*.md") if path.name != "README.md")

        self.assertEqual(docs, commands)

        for command in commands:
            path = docs_dir / f"{command}.md"
            text = path.read_text(encoding="utf-8")
            self.assertIn(f"# `{command}`", text)
            self.assertIn("## Purpose", text)
            self.assertIn("## Example", text)
            self.assertIn("## Gotchas", text)
            self.assertIn("## Help", text)
            self.assertIn(f"usage: kenjaku {command}", text)
