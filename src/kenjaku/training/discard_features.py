from __future__ import annotations

from collections.abc import Sequence
from dataclasses import dataclass

from kenjaku.core import shanten
from kenjaku.training.discard_examples import DiscardExample


@dataclass(frozen=True, slots=True)
class DiscardShantenDelta:
    before: int
    after: int
    delta: int


def discard_shanten_delta(example: DiscardExample) -> DiscardShantenDelta:
    if example.action.tile is None:
        raise ValueError("discard examples must have tile actions")
    before = shanten(example.hand_counts)
    after_counts = list(example.hand_counts)
    tile_index = example.action.tile.index
    if after_counts[tile_index] <= 0:
        raise ValueError("discard action must be present in hand counts")
    after_counts[tile_index] -= 1
    after = shanten(tuple(after_counts))
    return DiscardShantenDelta(before=before, after=after, delta=after - before)


def summarize_discard_shanten(
    examples: Sequence[DiscardExample],
) -> dict[str, int | float | None]:
    if not examples:
        return {
            "examples": 0,
            "average_before": None,
            "average_after": None,
            "average_delta": None,
            "preserved": 0,
            "worsened": 0,
        }

    deltas = [discard_shanten_delta(example) for example in examples]
    return {
        "examples": len(deltas),
        "average_before": sum(delta.before for delta in deltas) / len(deltas),
        "average_after": sum(delta.after for delta in deltas) / len(deltas),
        "average_delta": sum(delta.delta for delta in deltas) / len(deltas),
        "preserved": sum(delta.delta == 0 for delta in deltas),
        "worsened": sum(delta.delta > 0 for delta in deltas),
    }
