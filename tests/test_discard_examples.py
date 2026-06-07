from __future__ import annotations

import unittest
from pathlib import Path

from kenjaku.core import Tile, TileType
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

    def test_continues_after_pon_call(self) -> None:
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
              <N who="1" m="14955" />
              <E49 />
              <RYUUKYOKU />
            </mjloggm>
            """
        )

        examples = list(iter_discard_examples(game))

        self.assertEqual(len(examples), 2)
        self.assertEqual([example.event_index for example in examples], [1, 3])
        self.assertEqual(examples[1].seat, 1)
        self.assertEqual(sum(examples[1].hand_counts), 11)
        self.assertEqual(examples[1].action.tile, TileType.parse("4p"))
        self.assertEqual(examples[1].visible_counts[TileType.parse("1p").index], 3)
        self.assertEqual(examples[1].meld_counts_by_seat[1][TileType.parse("1p").index], 3)
        self.assertEqual(examples[1].meld_tiles_by_seat[1], (Tile.parse("1p"),) * 3)
        self.assertEqual(examples[1].dora_indicators, (Tile.parse("1s"),))

    def test_tracks_riichi_rivers_and_seat_turns(self) -> None:
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
              <DORA hai="72" />
              <T60 />
              <REACH who="0" step="1" />
              <D60 />
              <REACH who="0" step="2" ten="240,250,250,250" />
              <U64 />
              <E64 />
              <T61 />
              <D61 />
              <RYUUKYOKU />
            </mjloggm>
            """
        )

        examples = list(iter_discard_examples(game))

        self.assertEqual(len(examples), 3)
        self.assertEqual(examples[0].active_riichi_seats, (True, False, False, False))
        self.assertEqual(examples[1].active_riichi_seats, (True, False, False, False))
        self.assertEqual(examples[0].seat_turn_index, 0)
        self.assertEqual(examples[1].seat_turn_index, 0)
        self.assertEqual(examples[2].seat_turn_index, 1)
        self.assertEqual(examples[0].ippatsu_active_seats, (True, False, False, False))
        self.assertEqual(examples[1].ippatsu_active_seats, (True, False, False, False))
        self.assertEqual(examples[2].ippatsu_active_seats, (False, False, False, False))
        self.assertEqual(examples[0].last_discard_tsumogiri_by_seat, (None, None, None, None))
        self.assertEqual(examples[1].last_discard_tsumogiri_by_seat, (True, None, None, None))
        self.assertEqual(examples[2].last_discard_tsumogiri_by_seat, (True, True, None, None))
        self.assertEqual(examples[0].riichi_declared_turns, (0, None, None, None))
        self.assertEqual(examples[0].riichi_declared_event_indices, (1, None, None, None))
        self.assertEqual(examples[1].riichi_declared_turns, (0, None, None, None))
        self.assertEqual(examples[1].riichi_declared_event_indices, (1, None, None, None))
        seven_pin = TileType.parse("7p").index
        eight_pin = TileType.parse("8p").index
        self.assertEqual(examples[0].river_counts_by_seat[0][seven_pin], 0)
        self.assertEqual(examples[1].river_counts_by_seat[0][seven_pin], 1)
        self.assertEqual(examples[1].river_counts_by_seat[1][eight_pin], 0)
        self.assertEqual(examples[2].river_counts_by_seat[1][eight_pin], 1)
        self.assertEqual(examples[0].rivers_by_seat[0], ())
        self.assertEqual(examples[1].rivers_by_seat[0], (Tile.parse("7p"),))
        self.assertEqual(examples[2].rivers_by_seat[1], (Tile.parse("8p"),))


if __name__ == "__main__":
    unittest.main()
