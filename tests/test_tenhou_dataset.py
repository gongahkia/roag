from __future__ import annotations

import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.io import (
    iter_tenhou_xml_dataset,
    iter_tenhou_xml_dataset_files,
    parse_tenhou_xml_dataset,
    parse_tenhou_xml_paths,
    tenhou_xml_files,
)

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

    def test_iterates_dataset_one_file_at_a_time(self) -> None:
        games = list(iter_tenhou_xml_dataset([MINIMAL_FIXTURE, EVENTS_FIXTURE]))

        self.assertEqual([len(game.rounds) for game in games], [1, 1])

    def test_iterates_dataset_files_with_source_indexes(self) -> None:
        parsed = list(iter_tenhou_xml_dataset_files([MINIMAL_FIXTURE, EVENTS_FIXTURE]))
        expected_paths = tuple(sorted((MINIMAL_FIXTURE.resolve(), EVENTS_FIXTURE.resolve())))

        self.assertEqual([item.file_index for item in parsed], [0, 1])
        self.assertEqual([item.path for item in parsed], list(expected_paths))
        self.assertEqual([len(item.game.rounds) for item in parsed], [1, 1])

    def test_records_parse_failures_when_requested(self) -> None:
        with TemporaryDirectory() as directory:
            broken = Path(directory) / "broken.xml"
            broken.write_text("<mjloggm><INIT /></mjloggm>", encoding="utf-8")

            dataset = parse_tenhou_xml_dataset([MINIMAL_FIXTURE, broken], skip_errors=True)

        self.assertEqual(len(dataset.game.rounds), 1)
        self.assertEqual(len(dataset.files), 2)
        self.assertEqual(len(dataset.failures), 1)
        self.assertEqual(dataset.failures[0].path, broken.resolve())
        self.assertEqual(dataset.failures[0].error_type, "ValueError")

    def test_iterator_skips_parse_failures_when_requested(self) -> None:
        with TemporaryDirectory() as directory:
            broken = Path(directory) / "broken.xml"
            broken.write_text("<mjloggm><INIT /></mjloggm>", encoding="utf-8")

            games = list(iter_tenhou_xml_dataset([MINIMAL_FIXTURE, broken], skip_errors=True))

        self.assertEqual([len(game.rounds) for game in games], [1])

    def test_strict_parse_failures_still_raise(self) -> None:
        with TemporaryDirectory() as directory:
            broken = Path(directory) / "broken.xml"
            broken.write_text("<mjloggm><INIT /></mjloggm>", encoding="utf-8")

            with self.assertRaises(ValueError):
                parse_tenhou_xml_dataset([broken])

    def test_rejects_empty_directories(self) -> None:
        with TemporaryDirectory() as directory, self.assertRaises(ValueError):
            parse_tenhou_xml_paths([Path(directory)])


if __name__ == "__main__":
    unittest.main()
