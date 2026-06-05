from __future__ import annotations

from typing import TYPE_CHECKING

from kenjaku.core import Tile, TileType

if TYPE_CHECKING:
    from kenjaku.training.discard_examples import DiscardExample

SUJI_SAFE_RANKS = {
    1: (4,),
    2: (5,),
    3: (6,),
    4: (1, 7),
    5: (2, 8),
    6: (3, 9),
    7: (4,),
    8: (5,),
    9: (6,),
}


def active_riichi_opponents(example: DiscardExample) -> tuple[int, ...]:
    return tuple(
        seat
        for seat, active in enumerate(example.active_riichi_seats)
        if active and seat != example.seat
    )


def has_active_riichi_opponent(example: DiscardExample) -> bool:
    return bool(active_riichi_opponents(example))


def actual_discard_is_genbutsu(example: DiscardExample) -> bool:
    if example.action.tile is None:
        raise ValueError("discard examples must have tile actions")
    return candidate_is_genbutsu(example, example.action.tile)


def actual_discard_has_suji(example: DiscardExample) -> bool:
    if example.action.tile is None:
        raise ValueError("discard examples must have tile actions")
    return candidate_has_suji(example, example.action.tile)


def actual_discard_has_kabe(example: DiscardExample) -> bool:
    if example.action.tile is None:
        raise ValueError("discard examples must have tile actions")
    return candidate_has_kabe(example, example.action.tile)


def actual_discard_has_one_chance(example: DiscardExample) -> bool:
    if example.action.tile is None:
        raise ValueError("discard examples must have tile actions")
    return candidate_has_one_chance(example, example.action.tile)


def actual_discard_seen_after_riichi(example: DiscardExample) -> bool:
    if example.action.tile is None:
        raise ValueError("discard examples must have tile actions")
    return candidate_seen_after_riichi(example, example.action.tile)


def actual_discard_seen_before_riichi(example: DiscardExample) -> bool:
    if example.action.tile is None:
        raise ValueError("discard examples must have tile actions")
    return candidate_seen_before_riichi(example, example.action.tile)


def actual_discard_has_sotogawa(example: DiscardExample) -> bool:
    if example.action.tile is None:
        raise ValueError("discard examples must have tile actions")
    return candidate_has_sotogawa(example, example.action.tile)


def candidate_is_genbutsu(example: DiscardExample, tile: TileType | int) -> bool:
    tile_index = _tile_index(tile)
    return any(
        _river_count(example.river_counts_by_seat, seat, tile_index) > 0
        for seat in active_riichi_opponents(example)
    )


def candidate_has_suji(example: DiscardExample, tile: TileType | int) -> bool:
    tile_type = _tile_type(tile)
    if tile_type.is_honor:
        return False
    safe_indices = _suji_safe_indices(tile_type)
    return any(
        _river_count(example.river_counts_by_seat, seat, safe_index) > 0
        for seat in active_riichi_opponents(example)
        for safe_index in safe_indices
    )


def candidate_has_kabe(example: DiscardExample, tile: TileType | int) -> bool:
    tile_type = _tile_type(tile)
    if tile_type.is_honor:
        return False
    return any(
        example.visible_counts[adjacent_index] >= 4
        for adjacent_index in _adjacent_suited_indices(tile_type)
    )


def candidate_has_one_chance(example: DiscardExample, tile: TileType | int) -> bool:
    tile_type = _tile_type(tile)
    if tile_type.is_honor:
        return False
    return any(
        example.visible_counts[adjacent_index] == 3
        for adjacent_index in _adjacent_suited_indices(tile_type)
    )


def candidate_has_sotogawa(example: DiscardExample, tile: TileType | int) -> bool:
    tile_type = _tile_type(tile)
    if tile_type.is_honor:
        return False

    rank = tile_type.rank
    assert rank is not None
    if 4 <= rank <= 6:
        return False

    for seat in active_riichi_opponents(example):
        riichi_turn = _riichi_turn(example, seat)
        if riichi_turn is None or seat >= len(example.rivers_by_seat):
            continue
        if _river_has_sotogawa_anchor(example.rivers_by_seat[seat][:riichi_turn], tile_type):
            return True
    return False


def candidate_seen_after_riichi(example: DiscardExample, tile: TileType | int) -> bool:
    tile_index = _tile_index(tile)
    for seat in active_riichi_opponents(example):
        riichi_turn = _riichi_turn(example, seat)
        if riichi_turn is None:
            continue
        if _river_has_tile_from_turn(example.rivers_by_seat, seat, tile_index, riichi_turn):
            return True
    return False


def candidate_seen_before_riichi(example: DiscardExample, tile: TileType | int) -> bool:
    tile_index = _tile_index(tile)
    for seat in active_riichi_opponents(example):
        riichi_turn = _riichi_turn(example, seat)
        if riichi_turn is None:
            continue
        if _river_has_tile_before_turn(example.rivers_by_seat, seat, tile_index, riichi_turn):
            return True
    return False


def max_active_riichi_discards_elapsed(example: DiscardExample) -> int:
    return max(_active_riichi_discards_elapsed(example), default=0)


def min_active_riichi_discards_elapsed(example: DiscardExample) -> int:
    return min(_active_riichi_discards_elapsed(example), default=0)


def _active_riichi_discards_elapsed(example: DiscardExample) -> tuple[int, ...]:
    elapsed: list[int] = []
    for seat in active_riichi_opponents(example):
        riichi_turn = _riichi_turn(example, seat)
        if riichi_turn is None:
            continue
        river_length = (
            len(example.rivers_by_seat[seat])
            if seat < len(example.rivers_by_seat)
            else 0
        )
        elapsed.append(max(0, river_length - riichi_turn))
    return tuple(elapsed)


def _riichi_turn(example: DiscardExample, seat: int) -> int | None:
    if seat >= len(example.riichi_declared_turns):
        return None
    return example.riichi_declared_turns[seat]


def _river_count(
    river_counts_by_seat: tuple[tuple[int, ...], ...],
    seat: int,
    tile_index: int,
) -> int:
    if seat >= len(river_counts_by_seat):
        return 0
    return river_counts_by_seat[seat][tile_index]


def _river_has_tile_from_turn(
    rivers_by_seat: tuple[tuple[Tile, ...], ...],
    seat: int,
    tile_index: int,
    turn: int,
) -> bool:
    if seat >= len(rivers_by_seat):
        return False
    return any(tile.type.index == tile_index for tile in rivers_by_seat[seat][turn:])


def _river_has_tile_before_turn(
    rivers_by_seat: tuple[tuple[Tile, ...], ...],
    seat: int,
    tile_index: int,
    turn: int,
) -> bool:
    if seat >= len(rivers_by_seat):
        return False
    return any(tile.type.index == tile_index for tile in rivers_by_seat[seat][:turn])


def _river_has_sotogawa_anchor(river: tuple[Tile, ...], tile_type: TileType) -> bool:
    rank = tile_type.rank
    assert rank is not None
    for discarded in river:
        discarded_type = discarded.type
        discarded_rank = discarded_type.rank
        if discarded_type.suit != tile_type.suit or discarded_rank is None:
            continue
        if rank <= 3 and discarded_rank >= rank + 3:
            return True
        if rank >= 7 and discarded_rank <= rank - 3:
            return True
    return False


def _suji_safe_indices(tile_type: TileType) -> tuple[int, ...]:
    rank = tile_type.rank
    if rank is None:
        return ()
    suit_start = tile_type.index - rank + 1
    return tuple(suit_start + safe_rank - 1 for safe_rank in SUJI_SAFE_RANKS[rank])


def _adjacent_suited_indices(tile_type: TileType) -> tuple[int, ...]:
    rank = tile_type.rank
    if rank is None:
        return ()
    suit_start = tile_type.index - rank + 1
    adjacent_ranks = tuple(candidate for candidate in (rank - 1, rank + 1) if 1 <= candidate <= 9)
    return tuple(suit_start + adjacent_rank - 1 for adjacent_rank in adjacent_ranks)


def _tile_type(tile: TileType | int) -> TileType:
    if isinstance(tile, TileType):
        return tile
    return TileType(tile)


def _tile_index(tile: TileType | int) -> int:
    return _tile_type(tile).index
