from __future__ import annotations

from collections.abc import Sequence
from dataclasses import dataclass

from kenjaku.core import TENHOU_3P, TENHOU_4P, RuleSet, TileType, shanten


@dataclass(frozen=True, slots=True)
class ShantenUkeire:
    ruleset: str
    shanten: int
    improving_tiles: tuple[TileType, ...]
    ukeire: int


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


def _ruleset(ruleset: str) -> RuleSet:
    if ruleset == TENHOU_4P.name:
        return TENHOU_4P
    if ruleset == TENHOU_3P.name:
        return TENHOU_3P
    raise ValueError("unsupported evaluator ruleset: " + ruleset)


def _validate_counts(counts: Sequence[int], *, label: str) -> tuple[int, ...]:
    if len(counts) != 34:
        raise ValueError(label + " counts must have length 34")
    if any(isinstance(count, bool) or not isinstance(count, int) for count in counts):
        raise ValueError(label + " counts must be integers")
    if any(not 0 <= count <= 4 for count in counts):
        raise ValueError(label + " counts must be in the range 0..4")
    return tuple(counts)
