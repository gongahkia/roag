from __future__ import annotations

import unittest
from pathlib import Path

from kenjaku.core import TileType
from kenjaku.io import parse_tenhou_xml_file
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


if __name__ == "__main__":
    unittest.main()
