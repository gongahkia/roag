from __future__ import annotations

import unittest
from pathlib import Path

from kenjaku.core import ActionKind, TileType
from kenjaku.io import parse_tenhou_xml, parse_tenhou_xml_file
from kenjaku.training.call_examples import iter_call_examples

EVENTS_FIXTURE = Path("data/fixtures/tenhou/events_4p.xml")


class CallExampleTests(unittest.TestCase):
    def test_builds_actual_pon_example_from_fixture(self) -> None:
        game = parse_tenhou_xml_file(EVENTS_FIXTURE)
        examples = list(iter_call_examples(game))

        self.assertEqual(len(examples), 1)
        example = examples[0]
        self.assertEqual(example.event_index, 3)
        self.assertEqual(example.call_event_index, 4)
        self.assertEqual(example.seat, 1)
        self.assertEqual(example.from_seat, 0)
        self.assertEqual(example.discarded_tile.type, TileType.parse("1p"))
        self.assertEqual(example.legal_call_kinds, (ActionKind.PON,))
        self.assertEqual(example.action.kind, ActionKind.PON)
        self.assertEqual(example.action.tile, TileType.parse("1p"))
        self.assertEqual(len(example.action.consumed), 2)
        self.assertEqual(example.visible_counts[TileType.parse("1p").index], 3)

    def test_builds_pass_example_for_declined_pon(self) -> None:
        game = parse_tenhou_xml(
            """
            <mjloggm>
              <INIT
                seed="0,0,0,0,0,72"
                ten="250,250,250,250"
                oya="0"
                hai0="0,4,8,12,16,20,24,28,32,40,44,48,52"
                hai1="1,5,9,13,17,21,25,29,33,36,37,41,49"
                hai2="2,6,10,14,18,22,26,30,34,42,46,50,54"
                hai3="3,7,11,15,19,23,27,31,35,39,43,47,51"
              />
              <DORA hai="72" />
              <T38 />
              <D38 />
              <U64 />
              <E49 />
            </mjloggm>
            """
        )

        examples = list(iter_call_examples(game))

        self.assertEqual(len(examples), 1)
        self.assertEqual(examples[0].seat, 1)
        self.assertEqual(examples[0].call_event_index, None)
        self.assertEqual(examples[0].legal_call_kinds, (ActionKind.PON,))
        self.assertEqual(examples[0].action.kind, ActionKind.PASS)

    def test_builds_chi_pass_example_only_for_next_seat(self) -> None:
        game = parse_tenhou_xml(
            """
            <mjloggm>
              <INIT
                seed="0,0,0,0,0,72"
                ten="250,250,250,250"
                oya="0"
                hai0="0,4,8,12,16,20,24,28,32,40,44,48,52"
                hai1="1,5,9,13,17,21,25,29,33,40,44,57,61"
                hai2="2,6,10,14,18,22,26,30,34,42,46,50,54"
                hai3="3,7,11,15,19,23,27,31,35,39,43,47,51"
              />
              <T49 />
              <D49 />
              <U64 />
            </mjloggm>
            """
        )

        examples = list(iter_call_examples(game))

        self.assertEqual(len(examples), 1)
        self.assertEqual(examples[0].seat, 1)
        self.assertEqual(examples[0].legal_call_kinds, (ActionKind.CHI,))
        self.assertEqual(examples[0].action.kind, ActionKind.PASS)

    def test_sanma_does_not_build_chi_window(self) -> None:
        game = parse_tenhou_xml(
            """
            <mjloggm>
              <INIT
                seed="0,0,0,0,0,72"
                ten="350,350,350"
                oya="0"
                hai0="0,4,8,12,16,20,24,28,32,36,40,56,60"
                hai1="1,5,9,13,17,21,25,29,33,44,48,57,61"
                hai2="2,6,10,14,18,22,26,30,34,38,42,46,62"
              />
              <T52 />
              <D52 />
              <U64 />
            </mjloggm>
            """
        )

        examples = list(iter_call_examples(game))

        self.assertEqual(examples, [])


if __name__ == "__main__":
    unittest.main()
