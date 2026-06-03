from __future__ import annotations

import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.io import parse_tenhou_xml_paths, tenhou_xml_files


FIXTURE_DIR = Path("data/fixtures/tenhou")
MINIMAL_FIXTURE = FIXTURE_DIR / "minimal_4p.xml"
EVENTS_FIXTURE = FIXTURE_DIR / "events_4p.xml"


class TenhouDatasetTests(unittest.TestCase):
    def test_collects_files_from_files_and_directories(self) -> None:
        files = tenhou_xml_files([MINIMAL_FIXTURE, FIXTURE_DIR])

        self.assertEqual(len(files), 3)
        self.assertEqual(files, tuple(sorted(files)))
        self.assertIn(MINIMAL_FIXTURE.resolve(), files)

    def test_parses_multiple_paths_into_one_game(self) -> None:
        game = parse_tenhou_xml_paths([MINIMAL_FIXTURE, EVENTS_FIXTURE])

        self.assertEqual(len(game.rounds), 2)
        self.assertEqual(sum(len(round_.discards) for round_ in game.rounds), 4)

    def test_rejects_empty_directories(self) -> None:
        with TemporaryDirectory() as directory:
            with self.assertRaises(ValueError):
                parse_tenhou_xml_paths([Path(directory)])


if __name__ == "__main__":
    unittest.main()
