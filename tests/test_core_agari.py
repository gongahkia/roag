from __future__ import annotations

import unittest

from kenjaku.core import (
    Tile,
    is_winning_hand,
    is_winning_hand_for_tiles,
    tile_counts,
    winning_hand_shapes,
    winning_hand_shapes_for_tiles,
)


class AgariTests(unittest.TestCase):
    def test_standard_winning_hand(self) -> None:
        counts = _counts("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m 5m")

        self.assertTrue(is_winning_hand(counts))
        self.assertEqual(winning_hand_shapes(counts), ("standard",))

    def test_chiitoitsu_winning_hand(self) -> None:
        counts = _counts("1m 1m 2m 2m 3p 3p 4p 4p 5s 5s 6s 6s E E")

        self.assertEqual(winning_hand_shapes(counts), ("chiitoitsu",))

    def test_kokushi_winning_hand(self) -> None:
        counts = _counts("1m 1m 9m 1p 9p 1s 9s E S W N P F C")

        self.assertEqual(winning_hand_shapes(counts), ("kokushi",))

    def test_non_winning_hand(self) -> None:
        counts = _counts("1m 1m 2m 2m 3p 3p 4p 4p 5s 5s 6s 6s E P")

        self.assertFalse(is_winning_hand(counts))
        self.assertEqual(winning_hand_shapes(counts), ())

    def test_tile_helpers_accept_tiles(self) -> None:
        tiles = [
            Tile.parse(token)
            for token in [
                "1m",
                "2m",
                "3m",
                "E",
                "E",
                "E",
                "1p",
                "2p",
                "3p",
                "1s",
                "2s",
                "3s",
                "5m",
                "5m",
            ]
        ]

        self.assertTrue(is_winning_hand_for_tiles(tiles))
        self.assertEqual(winning_hand_shapes_for_tiles(tiles), ("standard",))

    def test_requires_fourteen_tiles(self) -> None:
        with self.assertRaisesRegex(ValueError, "exactly 14 tiles"):
            winning_hand_shapes(_counts("1m 2m 3m"))


def _counts(text: str) -> tuple[int, ...]:
    return tile_counts(Tile.parse(token) for token in text.split())


if __name__ == "__main__":
    unittest.main()
