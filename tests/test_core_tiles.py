from __future__ import annotations

import unittest

from kenjaku.core import TENHOU_3P, Tile, TileType, all_tile_types, tile_counts


class TileTests(unittest.TestCase):
    def test_tile_type_parse_and_notation(self) -> None:
        self.assertEqual(TileType.parse("1m").index, 0)
        self.assertEqual(TileType.parse("9m").index, 8)
        self.assertEqual(TileType.parse("1p").index, 9)
        self.assertEqual(TileType.parse("1s").index, 18)
        self.assertEqual(TileType.parse("E").index, 27)
        self.assertEqual(TileType.parse("C").notation, "C")

    def test_red_five_parsing_supports_zero_and_r_suffix(self) -> None:
        self.assertEqual(Tile.parse("0m"), Tile(TileType.parse("5m"), red=True))
        self.assertEqual(Tile.parse("5mr"), Tile(TileType.parse("5m"), red=True))
        self.assertEqual(Tile.parse("0p").notation, "0p")

    def test_invalid_red_tile_rejected(self) -> None:
        with self.assertRaises(ValueError):
            Tile.parse("4mr")

    def test_all_tile_types_has_34_logical_types(self) -> None:
        self.assertEqual(len(all_tile_types()), 34)
        self.assertEqual(all_tile_types()[33], TileType.parse("C"))

    def test_tile_counts_use_logical_type_for_red_fives(self) -> None:
        counts = tile_counts([Tile.parse("5m"), Tile.parse("0m"), TileType.parse("E")])

        self.assertEqual(counts[TileType.parse("5m").index], 2)
        self.assertEqual(counts[TileType.parse("E").index], 1)

    def test_tenhou_sanma_excludes_middle_man_tiles(self) -> None:
        excluded = {tile.notation for tile in TENHOU_3P.excluded_tile_types}

        self.assertEqual(excluded, {"2m", "3m", "4m", "5m", "6m", "7m", "8m"})
        self.assertEqual(TENHOU_3P.type_counts[TileType.parse("5m").index], 0)
        self.assertEqual(TENHOU_3P.red_five_suits, frozenset({"p", "s"}))


if __name__ == "__main__":
    unittest.main()
