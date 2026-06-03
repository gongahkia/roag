from __future__ import annotations

import unittest
from pathlib import Path

from kenjaku.core import ActionKind, Tile
from kenjaku.io import (
    TenhouAgari,
    TenhouCall,
    TenhouDiscard,
    TenhouDraw,
    TenhouReach,
    TenhouRyuukyoku,
    parse_tenhou_xml_file,
    tenhou_tile,
)


FIXTURE = Path("data/fixtures/tenhou/minimal_4p.xml")
EVENTS_FIXTURE = Path("data/fixtures/tenhou/events_4p.xml")
RYUUKYOKU_FIXTURE = Path("data/fixtures/tenhou/ryuukyoku_4p.xml")


class TenhouXmlTests(unittest.TestCase):
    def test_tenhou_tile_maps_physical_ids_to_logical_tiles(self) -> None:
        self.assertEqual(tenhou_tile(0), Tile.parse("1m"))
        self.assertEqual(tenhou_tile(16), Tile.parse("0m"))
        self.assertEqual(tenhou_tile(52), Tile.parse("0p"))
        self.assertEqual(tenhou_tile(88), Tile.parse("0s"))

    def test_parse_minimal_fixture(self) -> None:
        game = parse_tenhou_xml_file(FIXTURE)

        self.assertEqual(len(game.rounds), 1)
        round_ = game.rounds[0]
        self.assertEqual(round_.dealer, 0)
        self.assertEqual(round_.scores, (25000, 25000, 25000, 25000))
        self.assertEqual(len(round_.starting_hands), 4)
        self.assertEqual(round_.starting_hands[0][4], Tile.parse("0m"))
        self.assertEqual(round_.dora_indicators, (Tile.parse("1s"),))

    def test_parse_discards_and_tsumogiri(self) -> None:
        game = parse_tenhou_xml_file(FIXTURE)
        discards = game.rounds[0].discards

        self.assertEqual(len(discards), 2)
        self.assertEqual(discards[0].seat, 0)
        self.assertEqual(discards[0].tile, Tile.parse("7p"))
        self.assertTrue(discards[0].tsumogiri)
        self.assertEqual(discards[0].action.kind, ActionKind.DISCARD)

        self.assertEqual(discards[1].seat, 1)
        self.assertEqual(discards[1].tile, Tile.parse("4p"))
        self.assertFalse(discards[1].tsumogiri)

    def test_parse_draws_and_ordered_events(self) -> None:
        game = parse_tenhou_xml_file(FIXTURE)
        round_ = game.rounds[0]

        self.assertEqual([draw.tile for draw in round_.draws], [Tile.parse("7p"), Tile.parse("8p")])
        self.assertIsInstance(round_.events[0], TenhouDraw)
        self.assertIsInstance(round_.events[1], TenhouDiscard)
        self.assertIsInstance(round_.events[2], TenhouDraw)
        self.assertIsInstance(round_.events[3], TenhouDiscard)
        self.assertEqual([event.seat for event in round_.events[:4]], [0, 0, 1, 1])
        self.assertIsInstance(round_.events[4], TenhouRyuukyoku)

    def test_parse_reach_call_and_agari_events(self) -> None:
        game = parse_tenhou_xml_file(EVENTS_FIXTURE)
        round_ = game.rounds[0]

        self.assertEqual(len(round_.reaches), 2)
        self.assertEqual(round_.reaches[0], TenhouReach(seat=0, step=1, event_index=0))
        self.assertEqual(round_.reaches[1].scores, (24000, 25000, 25000, 25000))
        self.assertEqual(len(round_.calls), 1)
        self.assertEqual(round_.calls[0].seat, 1)
        self.assertEqual(round_.calls[0].meld_code, 14955)
        self.assertEqual(round_.calls[0].event_index, 4)
        self.assertEqual(round_.calls[0].meld.kind, ActionKind.PON)
        self.assertEqual(round_.calls[0].meld.tile_ids, (36, 37, 38))
        self.assertEqual(len(round_.agari), 1)

        agari = round_.agari[0]
        self.assertIsInstance(round_.events[6], TenhouAgari)
        self.assertEqual(agari.winner, 2)
        self.assertEqual(agari.from_seat, 1)
        self.assertEqual(agari.machi, Tile.parse("4p"))
        self.assertEqual(agari.points, (30, 1000))
        self.assertEqual(agari.yaku, (1, 1))
        self.assertEqual(agari.dora_indicators, (Tile.parse("1s"),))
        self.assertEqual(agari.ura_dora_indicators, (Tile.parse("1s"),))

    def test_parse_ryuukyoku_event(self) -> None:
        game = parse_tenhou_xml_file(RYUUKYOKU_FIXTURE)
        round_ = game.rounds[0]

        self.assertEqual(
            round_.ryuukyoku,
            TenhouRyuukyoku(
                event_index=0,
                reason="yao9",
                scores=(25000, 25000, 25000, 25000),
            ),
        )
        self.assertIsInstance(round_.events[0], TenhouRyuukyoku)


if __name__ == "__main__":
    unittest.main()
