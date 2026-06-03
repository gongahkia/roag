from __future__ import annotations

import unittest
from pathlib import Path

from kenjaku.core import TileType
from kenjaku.io import parse_tenhou_xml, parse_tenhou_xml_file
from kenjaku.training import iter_discard_examples


FIXTURE = Path("data/fixtures/tenhou/minimal_4p.xml")


class DiscardExampleTests(unittest.TestCase):
    def test_builds_discard_examples_from_fixture(self) -> None:
        game = parse_tenhou_xml_file(FIXTURE)
        examples = list(iter_discard_examples(game))

        self.assertEqual(len(examples), 2)
        self.assertEqual(examples[0].round_index, 0)
        self.assertEqual(examples[0].event_index, 1)
        self.assertEqual(examples[0].seat, 0)
        self.assertEqual(sum(examples[0].hand_counts), 14)
        self.assertEqual(examples[0].action.tile, TileType.parse("7p"))
        self.assertTrue(examples[0].action.tsumogiri)
        self.assertEqual(examples[1].event_index, 3)

    def test_visible_counts_are_perspective_scoped(self) -> None:
        game = parse_tenhou_xml_file(FIXTURE)
        examples = list(iter_discard_examples(game))

        first, second = examples
        self.assertEqual(first.visible_counts[TileType.parse("1s").index], 1)
        self.assertEqual(first.visible_counts[TileType.parse("7p").index], 1)
        self.assertEqual(second.visible_counts[TileType.parse("7p").index], 1)
        self.assertEqual(second.visible_counts[TileType.parse("8p").index], 1)

    def test_stops_at_first_opaque_call(self) -> None:
        game = parse_tenhou_xml(
            """
            <mjloggm>
              <INIT
                seed="0,0,0,0,0,72"
                ten="250,250,250,250"
                oya="0"
                hai0="0,4,8,12,16,20,24,28,32,36,40,44,48"
                hai1="1,5,9,13,17,21,25,29,33,37,41,45,49"
                hai2="2,6,10,14,18,22,26,30,34,38,42,46,50"
                hai3="3,7,11,15,19,23,27,31,35,39,43,47,51"
              />
              <T60 />
              <D60 />
              <N who="1" m="12345" />
              <U64 />
              <E49 />
            </mjloggm>
            """
        )

        examples = list(iter_discard_examples(game))

        self.assertEqual(len(examples), 1)
        self.assertEqual(examples[0].event_index, 1)


if __name__ == "__main__":
    unittest.main()
