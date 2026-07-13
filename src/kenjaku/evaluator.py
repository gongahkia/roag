from __future__ import annotations

from collections.abc import Iterable, Sequence
from dataclasses import dataclass

from kenjaku.core import TENHOU_3P, TENHOU_4P, RuleSet, Tile, TileType, shanten, tile_counts

_DRAGONS = frozenset(TileType.parse(token) for token in ("P", "F", "C"))


@dataclass(frozen=True, slots=True)
class ShantenUkeire:
    ruleset: str
    shanten: int
    improving_tiles: tuple[TileType, ...]
    ukeire: int


@dataclass(frozen=True, slots=True)
class HandValuePotential:
    ruleset: str
    visible_dora: int
    red_dora: int
    yakuhai_triplets: tuple[TileType, ...]
    flush_candidate: str | None
    potential_yaku: tuple[str, ...]


def evaluate_shanten_ukeire(
    hand_counts: Sequence[int],
    *,
    ruleset: str = TENHOU_4P.name,
    visible_counts: Sequence[int] | None = None,
) -> ShantenUkeire:
    rules = _ruleset(ruleset)
    hand = _validate_counts(hand_counts, label="hand")
    if sum(hand) > 13:
        raise ValueError("ukeire evaluation requires at most thirteen hand tiles")
    if any(hand[tile.index] for tile in rules.excluded_tile_types):
        raise ValueError("hand contains a tile unavailable in ruleset")
    visible = hand if visible_counts is None else _validate_counts(visible_counts, label="visible")
    if any(visible[index] < hand[index] for index in range(34)):
        raise ValueError("visible counts must include the hand")
    baseline_shanten = shanten(hand)
    improving_tiles: list[TileType] = []
    ukeire = 0
    for tile in rules.tile_types:
        remaining = 4 - visible[tile.index]
        if remaining <= 0:
            continue
        next_hand = list(hand)
        next_hand[tile.index] += 1
        if shanten(next_hand) < baseline_shanten:
            improving_tiles.append(tile)
            ukeire += remaining
    return ShantenUkeire(
        ruleset=rules.name,
        shanten=baseline_shanten,
        improving_tiles=tuple(improving_tiles),
        ukeire=ukeire,
    )


def evaluate_hand_value_potential(
    tiles: Iterable[Tile],
    *,
    ruleset: str = TENHOU_4P.name,
    dora_indicators: Iterable[Tile | TileType] = (),
    seat_wind: TileType | None = None,
    round_wind: TileType | None = None,
) -> HandValuePotential:
    rules = _ruleset(ruleset)
    hand = tuple(tiles)
    counts = tile_counts(hand)
    if any(count > 4 for count in counts):
        raise ValueError("hand cannot contain more than four copies of a tile")
    if any(counts[tile.index] for tile in rules.excluded_tile_types):
        raise ValueError("hand contains a tile unavailable in ruleset")
    dora_types = tuple(
        _dora_type_for_indicator(
            indicator.type if isinstance(indicator, Tile) else indicator,
            ruleset=rules.name,
        )
        for indicator in dora_indicators
    )
    visible_dora = sum(tile.type in dora_types for tile in hand)
    red_dora = sum(tile.red and tile.type.suit in rules.red_five_suits for tile in hand)
    yakuhai_triplets = tuple(
        tile
        for tile in rules.tile_types
        if counts[tile.index] >= 3 and _is_yakuhai(tile, seat_wind=seat_wind, round_wind=round_wind)
    )
    flush_candidate = _flush_candidate(hand)
    potential_yaku = ["yakuhai"] * len(yakuhai_triplets)
    if flush_candidate is not None:
        potential_yaku.append(flush_candidate)
    if hand and all(not tile.type.is_terminal_or_honor for tile in hand):
        potential_yaku.append("tanyao")
    return HandValuePotential(
        ruleset=rules.name,
        visible_dora=visible_dora,
        red_dora=red_dora,
        yakuhai_triplets=yakuhai_triplets,
        flush_candidate=flush_candidate,
        potential_yaku=tuple(potential_yaku),
    )


def _ruleset(ruleset: str) -> RuleSet:
    if ruleset == TENHOU_4P.name:
        return TENHOU_4P
    if ruleset == TENHOU_3P.name:
        return TENHOU_3P
    raise ValueError("unsupported evaluator ruleset: " + ruleset)


def _dora_type_for_indicator(indicator: TileType, *, ruleset: str) -> TileType:
    if ruleset == TENHOU_3P.name and indicator.suit == "m" and indicator.rank in {1, 9}:
        return TileType.parse("9m" if indicator.rank == 1 else "1m")
    if indicator.suit in {"m", "p", "s"}:
        rank = indicator.rank
        assert rank is not None
        return TileType.parse(f"{1 if rank == 9 else rank + 1}{indicator.suit}")
    winds = tuple(TileType.parse(token) for token in ("E", "S", "W", "N"))
    if indicator in winds:
        return winds[(winds.index(indicator) + 1) % len(winds)]
    dragons = tuple(TileType.parse(token) for token in ("P", "F", "C"))
    return dragons[(dragons.index(indicator) + 1) % len(dragons)]


def _is_yakuhai(tile: TileType, *, seat_wind: TileType | None, round_wind: TileType | None) -> bool:
    return tile in _DRAGONS or tile in (seat_wind, round_wind)


def _flush_candidate(tiles: tuple[Tile, ...]) -> str | None:
    suits = {tile.type.suit for tile in tiles if not tile.type.is_honor}
    if len(suits) != 1:
        return None
    return "honitsu" if any(tile.type.is_honor for tile in tiles) else "chinitsu"


def _validate_counts(counts: Sequence[int], *, label: str) -> tuple[int, ...]:
    if len(counts) != 34:
        raise ValueError(label + " counts must have length 34")
    if any(isinstance(count, bool) or not isinstance(count, int) for count in counts):
        raise ValueError(label + " counts must be integers")
    if any(not 0 <= count <= 4 for count in counts):
        raise ValueError(label + " counts must be in the range 0..4")
    return tuple(counts)
