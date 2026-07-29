from __future__ import annotations

import contextlib
import io
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.cli import main
from kenjaku.io import (
    parse_tenhou_xml,
    parse_tenhou_xml_file,
    read_mjai_events,
    to_mjai_events,
    write_mjai_events,
)

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

    def test_tenhou_to_mjai_compat_mode_matches_upstream_shape(self) -> None:
        game = parse_tenhou_xml(
            """
            <mjloggm>
              <UN n0="%E3%81%82" n1="b" n2="c" n3="d" />
              <INIT
                seed="0,0,0,0,0,72"
                ten="250,250,250,250"
                oya="0"
                hai0="48,0,72,36,16,4,8,12,20,24,28,32,40"
                hai1="1,5,9,13,17,21,25,29,33,37,41,45,49"
                hai2="2,6,10,14,18,22,26,30,34,38,42,46,50"
                hai3="3,7,11,15,19,23,27,31,35,39,43,47,51"
              />
              <REACH who="0" step="1" />
              <REACH who="0" step="2" ten="240,250,250,250" />
              <AGARI
                who="0"
                fromWho="1"
                machi="60"
                ten="30,1000,0"
                sc="250,10,250,-10,250,0,250,0"
                yaku="1,1"
                doraHai="72"
                uraDoraHai="73"
              />
            </mjloggm>
            """
        )

        events = to_mjai_events(game, compat="tenhou-to-mjai")

        self.assertEqual(events[0]["names"], ["あ", "b", "c", "d"])
        self.assertEqual(
            list(events[1]),
            [
                "type",
                "bakaze",
                "dora_marker",
                "kyoku",
                "honba",
                "kyotaku",
                "oya",
                "scores",
                "tehais",
            ],
        )
        self.assertEqual(
            events[1]["tehais"][0],
            [
                "1m",
                "2m",
                "3m",
                "4m",
                "5mr",
                "6m",
                "7m",
                "8m",
                "9m",
                "1p",
                "2p",
                "4p",
                "1s",
            ],
        )
        self.assertEqual(events[3], {"type": "reach_accepted", "actor": 0})
        self.assertEqual(
            events[4],
            {
                "type": "hora",
                "actor": 0,
                "target": 1,
                "deltas": [1000, -1000, 0, 0],
                "ura_markers": ["1s"],
            },
        )

    def test_tenhou_to_mjai_cli_accepts_compat_mode(self) -> None:
        stdout = io.StringIO()
        with TemporaryDirectory() as directory, contextlib.redirect_stdout(stdout):
            output_dir = Path(directory)
            exit_code = main(
                [
                    "tenhou-to-mjai",
                    str(EVENTS_FIXTURE),
                    "--output",
                    str(output_dir),
                    "--compat",
                    "tenhou-to-mjai",
                ]
            )

            output = output_dir / "events_4p.mjson"
            events = read_mjai_events(output)
            self.assertEqual(exit_code, 0)
            self.assertEqual(events[8]["ura_markers"], ["1s"])
            self.assertNotIn("uradora_markers", events[8])
        self.assertIn("files: 1", stdout.getvalue())


def _fixture_text() -> str:
    return MJAI_EVENTS_FIXTURE.read_text(encoding="utf-8")


if __name__ == "__main__":
    unittest.main()
