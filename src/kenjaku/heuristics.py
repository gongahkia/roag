from __future__ import annotations

from collections.abc import Iterable, Sequence
from dataclasses import dataclass

from kenjaku.core import TENHOU_4P, Tile, TileType, tile_counts
from kenjaku.evaluator import evaluate_hand_value_potential, evaluate_shanten_ukeire


@dataclass(frozen=True, slots=True)
class HeuristicFactor:
    name: str
    value: float
    contribution: float


@dataclass(frozen=True, slots=True)
class HeuristicDiscardCandidate:
    tile: TileType
    score: float
    factors: tuple[HeuristicFactor, ...]


def rank_discard_heuristic(
    tiles: Iterable[Tile],
    *,
    ruleset: str = TENHOU_4P.name,
    visible_counts: Sequence[int] | None = None,
) -> tuple[HeuristicDiscardCandidate, ...]:
    hand = tuple(tiles)
    counts = tile_counts(hand)
    if len(hand) != 14:
        raise ValueError("discard ranking requires exactly fourteen tiles")
    visible = counts if visible_counts is None else tuple(visible_counts)
    candidates: list[HeuristicDiscardCandidate] = []
    for tile in sorted({item.type for item in hand}, key=lambda item: item.index):
        after_tiles = _remove_one_tile(hand, tile)
        efficiency = evaluate_shanten_ukeire(
            tile_counts(after_tiles),
            ruleset=ruleset,
            visible_counts=visible,
        )
        value = evaluate_hand_value_potential(after_tiles, ruleset=ruleset)
        factors = (
            HeuristicFactor("shanten", float(efficiency.shanten), -float(efficiency.shanten)),
            HeuristicFactor("ukeire", float(efficiency.ukeire), efficiency.ukeire / 16.0),
            HeuristicFactor(
                "bonus_han",
                float(value.visible_dora + value.red_dora),
                float(value.visible_dora + value.red_dora) / 2.0,
            ),
            HeuristicFactor(
                "structural_yaku",
                float(len(value.potential_yaku)),
                len(value.potential_yaku) / 4.0,
            ),
        )
        candidates.append(
            HeuristicDiscardCandidate(
                tile=tile,
                score=sum(factor.contribution for factor in factors),
                factors=factors,
            )
        )
    return tuple(
        sorted(
            candidates,
            key=lambda candidate: (
                _shanten_factor(candidate),
                -candidate.score,
                candidate.tile.index,
            ),
        )
    )


def _remove_one_tile(tiles: tuple[Tile, ...], tile_type: TileType) -> tuple[Tile, ...]:
    for index, tile in enumerate(tiles):
        if tile.type == tile_type and not tile.red:
            return (*tiles[:index], *tiles[index + 1 :])
    for index, tile in enumerate(tiles):
        if tile.type == tile_type:
            return (*tiles[:index], *tiles[index + 1 :])
    raise ValueError("discard tile must be in hand")


def _shanten_factor(candidate: HeuristicDiscardCandidate) -> float:
    return candidate.factors[0].value
