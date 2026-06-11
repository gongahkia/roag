from __future__ import annotations

from collections.abc import Iterable, Sequence

from kenjaku.core.shanten import chiitoitsu_shanten, kokushi_shanten, standard_shanten
from kenjaku.core.tiles import Tile, TileType, tile_counts

WINNING_HAND_SHAPES = ("standard", "chiitoitsu", "kokushi")


def is_winning_hand(counts: Sequence[int]) -> bool:
    return bool(winning_hand_shapes(counts))


def is_winning_hand_for_tiles(tiles: Iterable[Tile | TileType]) -> bool:
    return is_winning_hand(tile_counts(tiles))


def winning_hand_shapes(counts: Sequence[int]) -> tuple[str, ...]:
    checked = _validate_winning_counts(counts)
    shapes: list[str] = []
    if standard_shanten(checked) == -1:
        shapes.append("standard")
    if chiitoitsu_shanten(checked) == -1:
        shapes.append("chiitoitsu")
    if kokushi_shanten(checked) == -1:
        shapes.append("kokushi")
    return tuple(shapes)


def winning_hand_shapes_for_tiles(tiles: Iterable[Tile | TileType]) -> tuple[str, ...]:
    return winning_hand_shapes(tile_counts(tiles))


def _validate_winning_counts(counts: Sequence[int]) -> tuple[int, ...]:
    checked = tuple(counts)
    if len(checked) != 34:
        raise ValueError("winning hand counts must have length 34")
    if any(count < 0 or count > 4 for count in checked):
        raise ValueError("winning hand counts must contain tile counts in the range 0..4")
    if sum(checked) != 14:
        raise ValueError("winning hand counts must contain exactly 14 tiles")
    return checked
