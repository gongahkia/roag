from __future__ import annotations

import unittest

from kenjaku.core import (
    Tile,
    chiitoitsu_shanten,
    kokushi_shanten,
    shanten,
    shanten_for_tiles,
    standard_shanten,
    tile_counts,
)


class ShantenTests(unittest.TestCase):
    def test_standard_complete_hand_is_minus_one(self) -> None:
        counts = _counts("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m 5m")

        self.assertEqual(standard_shanten(counts), -1)
        self.assertEqual(shanten(counts), -1)

    def test_standard_tenpai_is_zero(self) -> None:
        counts = _counts("1m 2m 3m 1p 2p 3p 1s 2s 3s E E E 5m")

        self.assertEqual(standard_shanten(counts), 0)
        self.assertEqual(shanten(counts), 0)

    def test_standard_one_shanten_shape(self) -> None:
        counts = _counts("1m 2m 3m 1p 2p 3p 1s 2s 3s E 4m 5m 9s")

        self.assertEqual(standard_shanten(counts), 1)

    def test_chiitoitsu_complete_hand_is_minus_one(self) -> None:
        counts = _counts("1m 1m 2m 2m 3p 3p 4p 4p 5s 5s 6s 6s E E")

        self.assertEqual(chiitoitsu_shanten(counts), -1)
        self.assertEqual(shanten(counts), -1)

    def test_kokushi_shapes(self) -> None:
        tenpai = _counts("1m 9m 1p 9p 1s 9s E S W N P F C")
        complete = _counts("1m 1m 9m 1p 9p 1s 9s E S W N P F C")

        self.assertEqual(kokushi_shanten(tenpai), 0)
        self.assertEqual(kokushi_shanten(complete), -1)
        self.assertEqual(shanten(complete), -1)

    def test_shanten_for_tiles_accepts_tiles(self) -> None:
        tiles = [
            Tile.parse(token)
            for token in [
                "1m",
                "2m",
                "3m",
                "1p",
                "2p",
                "3p",
                "E",
                "E",
                "E",
                "4s",
                "5s",
                "6s",
                "7m",
            ]
        ]

        self.assertEqual(shanten_for_tiles(tiles), 0)

    def test_rejects_invalid_counts(self) -> None:
        with self.assertRaises(ValueError):
            shanten(tuple([0] * 33))
        with self.assertRaises(ValueError):
            shanten(tuple([5, *([0] * 33)]))
        with self.assertRaises(ValueError):
            shanten(_counts("1m 1m 1m 1m 2m 2m 2m 2m 3m 3m 3m 3m 4m 4m 4p"))


def _counts(text: str) -> tuple[int, ...]:
    return tile_counts(Tile.parse(token) for token in text.split())


if __name__ == "__main__":
    unittest.main()
