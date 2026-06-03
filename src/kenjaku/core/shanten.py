from __future__ import annotations

from collections.abc import Iterable, Sequence
from functools import cache

from kenjaku.core.tiles import Tile, TileType, tile_counts

TERMINAL_HONOR_INDICES = frozenset(
    {
        0,
        8,
        9,
        17,
        18,
        26,
        *range(27, 34),
    }
)


def shanten(counts: Sequence[int]) -> int:
    """Return the best shanten number across standard, chiitoitsu, and kokushi shapes."""

    checked = _validate_counts(counts)
    return min(
        standard_shanten(checked),
        chiitoitsu_shanten(checked),
        kokushi_shanten(checked),
    )


def shanten_for_tiles(tiles: Iterable[Tile | TileType]) -> int:
    return shanten(tile_counts(tiles))


def standard_shanten(counts: Sequence[int]) -> int:
    checked = _validate_counts(counts)
    best = 8
    pair_candidates: tuple[int | None, ...] = (None, *(
        index for index, count in enumerate(checked) if count >= 2
    ))

    for pair_index in pair_candidates:
        working = list(checked)
        has_pair = pair_index is not None
        if pair_index is not None:
            working[pair_index] -= 2

        for melds, taatsu in _group_options(tuple(working)):
            usable_taatsu = min(taatsu, 4 - melds)
            best = min(best, 8 - 2 * melds - usable_taatsu - int(has_pair))

    return best


def chiitoitsu_shanten(counts: Sequence[int]) -> int:
    checked = _validate_counts(counts)
    pairs = sum(count >= 2 for count in checked)
    unique = sum(count > 0 for count in checked)
    return 6 - pairs + max(0, 7 - unique)


def kokushi_shanten(counts: Sequence[int]) -> int:
    checked = _validate_counts(counts)
    unique_terminals = sum(checked[index] > 0 for index in TERMINAL_HONOR_INDICES)
    has_terminal_pair = any(checked[index] >= 2 for index in TERMINAL_HONOR_INDICES)
    return 13 - unique_terminals - int(has_terminal_pair)


@cache
def _group_options(counts: tuple[int, ...]) -> frozenset[tuple[int, int]]:
    tile_index = _first_nonzero(counts)
    if tile_index is None:
        return frozenset({(0, 0)})

    results: set[tuple[int, int]] = set()
    _extend_options(results, counts, ((tile_index, 1),), melds=0, taatsu=0)

    if counts[tile_index] >= 3:
        _extend_options(results, counts, ((tile_index, 3),), melds=1, taatsu=0)

    if _can_sequence(counts, tile_index):
        _extend_options(
            results,
            counts,
            ((tile_index, 1), (tile_index + 1, 1), (tile_index + 2, 1)),
            melds=1,
            taatsu=0,
        )

    if counts[tile_index] >= 2:
        _extend_options(results, counts, ((tile_index, 2),), melds=0, taatsu=1)

    if _can_taatsu(counts, tile_index, offset=1):
        _extend_options(
            results,
            counts,
            ((tile_index, 1), (tile_index + 1, 1)),
            melds=0,
            taatsu=1,
        )

    if _can_taatsu(counts, tile_index, offset=2):
        _extend_options(
            results,
            counts,
            ((tile_index, 1), (tile_index + 2, 1)),
            melds=0,
            taatsu=1,
        )

    return frozenset(results)


def _extend_options(
    results: set[tuple[int, int]],
    counts: tuple[int, ...],
    removals: tuple[tuple[int, int], ...],
    *,
    melds: int,
    taatsu: int,
) -> None:
    next_counts = list(counts)
    for tile_index, amount in removals:
        next_counts[tile_index] -= amount
    for child_melds, child_taatsu in _group_options(tuple(next_counts)):
        results.add((melds + child_melds, taatsu + child_taatsu))


def _first_nonzero(counts: tuple[int, ...]) -> int | None:
    for index, count in enumerate(counts):
        if count:
            return index
    return None


def _can_sequence(counts: tuple[int, ...], tile_index: int) -> bool:
    return (
        _is_suited(tile_index)
        and tile_index % 9 <= 6
        and counts[tile_index + 1] > 0
        and counts[tile_index + 2] > 0
    )


def _can_taatsu(counts: tuple[int, ...], tile_index: int, *, offset: int) -> bool:
    return (
        _is_suited(tile_index)
        and tile_index % 9 <= 8 - offset
        and counts[tile_index + offset] > 0
    )


def _is_suited(tile_index: int) -> bool:
    return tile_index < 27


def _validate_counts(counts: Sequence[int]) -> tuple[int, ...]:
    checked = tuple(counts)
    if len(checked) != 34:
        raise ValueError("shanten counts must have length 34")
    if any(count < 0 or count > 4 for count in checked):
        raise ValueError("shanten counts must contain tile counts in the range 0..4")
    if sum(checked) > 14:
        raise ValueError("shanten counts cannot contain more than 14 tiles")
    return checked
