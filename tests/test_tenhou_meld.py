from __future__ import annotations

import unittest

from kenjaku.core import ActionKind, Tile
from kenjaku.io import decode_tenhou_meld


class TenhouMeldTests(unittest.TestCase):
    def test_decodes_chi(self) -> None:
        meld = decode_tenhou_meld(_encode_chi(sequence_start=0, called=1, copies=(0, 1, 2)))

        self.assertEqual(meld.kind, ActionKind.CHI)
        self.assertEqual(meld.from_offset, 3)
        self.assertEqual(meld.tile_ids, (0, 5, 10))
        self.assertEqual(meld.tiles, (Tile.parse("1m"), Tile.parse("2m"), Tile.parse("3m")))
        self.assertEqual(meld.called_tile_id, 5)
        self.assertEqual(meld.called_tile, Tile.parse("2m"))

    def test_decodes_pon(self) -> None:
        meld = decode_tenhou_meld(_encode_pon(base=9, called=2, unused=3))

        self.assertEqual(meld.kind, ActionKind.PON)
        self.assertEqual(meld.from_offset, 2)
        self.assertEqual(meld.tile_ids, (36, 37, 38))
        self.assertEqual(meld.tiles, (Tile.parse("1p"), Tile.parse("1p"), Tile.parse("1p")))
        self.assertEqual(meld.called_tile_id, 38)
        self.assertIsNone(meld.added_tile)

    def test_decodes_kakan(self) -> None:
        meld = decode_tenhou_meld(_encode_kakan(base=4, called=1, unused=0))

        self.assertEqual(meld.kind, ActionKind.KAKAN)
        self.assertEqual(meld.tile_ids, (16, 17, 18, 19))
        self.assertEqual(meld.tiles[0], Tile.parse("0m"))
        self.assertEqual(meld.called_tile_id, 18)
        self.assertEqual(meld.added_tile_id, 16)
        self.assertEqual(meld.added_tile, Tile.parse("0m"))

    def test_decodes_kan_variants(self) -> None:
        ankan = decode_tenhou_meld(_encode_kan(base=31, called=0, from_offset=0))
        minkan = decode_tenhou_meld(_encode_kan(base=27, called=2, from_offset=1))

        self.assertEqual(ankan.kind, ActionKind.ANKAN)
        self.assertEqual(ankan.tile_ids, (124, 125, 126, 127))
        self.assertIsNone(ankan.called_tile_id)

        self.assertEqual(minkan.kind, ActionKind.MINKAN)
        self.assertEqual(minkan.tile_ids, (108, 109, 110, 111))
        self.assertEqual(minkan.called_tile_id, 110)
        self.assertEqual(minkan.called_tile, Tile.parse("E"))

    def test_rejects_out_of_range_codes(self) -> None:
        with self.assertRaises(ValueError):
            decode_tenhou_meld(-1)
        with self.assertRaises(ValueError):
            decode_tenhou_meld(1 << 16)


def _encode_chi(
    *,
    sequence_start: int,
    called: int,
    copies: tuple[int, int, int],
    from_offset: int = 3,
) -> int:
    encoded_base = (sequence_start // 9) * 7 + sequence_start % 9
    return (
        ((encoded_base * 3 + called) << 10)
        | (copies[0] << 3)
        | (copies[1] << 5)
        | (copies[2] << 7)
        | 0x4
        | from_offset
    )


def _encode_pon(*, base: int, called: int, unused: int, from_offset: int = 2) -> int:
    return ((base * 3 + called) << 9) | (unused << 5) | 0x8 | from_offset


def _encode_kakan(*, base: int, called: int, unused: int, from_offset: int = 2) -> int:
    return ((base * 3 + called) << 9) | (unused << 5) | 0x10 | from_offset


def _encode_kan(*, base: int, called: int, from_offset: int) -> int:
    return ((base * 4 + called) << 8) | from_offset


if __name__ == "__main__":
    unittest.main()
