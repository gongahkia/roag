from __future__ import annotations

import contextlib
import io
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.cli import main
from kenjaku.io import parse_tenhou_xml_file, read_mjai_events, to_mjai_events, write_mjai_events

EVENTS_FIXTURE = Path("data/fixtures/tenhou/events_4p.xml")
MJAI_EVENTS_FIXTURE = Path("data/fixtures/mjai/events_4p.mjson")


class MjaiTests(unittest.TestCase):
    def test_to_mjai_events_emits_full_fixture_stream(self) -> None:
        events = to_mjai_events(parse_tenhou_xml_file(EVENTS_FIXTURE))

        self.assertEqual(
            [event["type"] for event in events],
            [
                "start_game",
                "start_kyoku",
                "reach",
                "reach_accepted",
                "tsumo",
                "dahai",
                "pon",
                "dahai",
                "hora",
                "end_kyoku",
                "end_game",
            ],
        )
        self.assertEqual(events[1]["dora_marker"], "1s")
        self.assertEqual(events[6]["target"], 0)
        self.assertEqual(events[6]["consumed"], ["1p", "1p"])
        self.assertEqual(events[8]["pai"], "4p")
        self.assertEqual(events[8]["uradora_markers"], ["1s"])

    def test_write_and_read_mjai_events_match_fixture(self) -> None:
        events = to_mjai_events(parse_tenhou_xml_file(EVENTS_FIXTURE))
        with TemporaryDirectory() as directory:
            output = Path(directory) / "events_4p.mjson"
            count = write_mjai_events(output, events)

            self.assertEqual(count, len(events))
            self.assertEqual(output.read_text(encoding="utf-8"), _fixture_text())
            self.assertEqual(read_mjai_events(output), events)

    def test_tenhou_to_mjai_cli_writes_mjson_files(self) -> None:
        stdout = io.StringIO()
        with TemporaryDirectory() as directory, contextlib.redirect_stdout(stdout):
            output_dir = Path(directory)
            exit_code = main(["tenhou-to-mjai", str(EVENTS_FIXTURE), "--output", str(output_dir)])

            output = output_dir / "events_4p.mjson"
            self.assertEqual(exit_code, 0)
            self.assertEqual(output.read_text(encoding="utf-8"), _fixture_text())
        self.assertIn("files: 1", stdout.getvalue())


def _fixture_text() -> str:
    return MJAI_EVENTS_FIXTURE.read_text(encoding="utf-8")


if __name__ == "__main__":
    unittest.main()
