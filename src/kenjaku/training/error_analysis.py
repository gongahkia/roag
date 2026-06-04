from __future__ import annotations

from collections.abc import Callable, Sequence
from typing import Any

from kenjaku.core import TileType
from kenjaku.training.discard_examples import DiscardExample
from kenjaku.training.discard_features import discard_shanten_delta

DiscardPredictor = Callable[[DiscardExample], TileType]


def summarize_discard_predictions(
    examples: Sequence[DiscardExample],
    predict: DiscardPredictor,
) -> dict[str, Any]:
    buckets = {
        "overall": _empty_bucket(),
        "by_shanten_delta": {
            "improved": _empty_bucket(),
            "preserved": _empty_bucket(),
            "worsened": _empty_bucket(),
        },
        "by_tile_family": {
            "man": _empty_bucket(),
            "pin": _empty_bucket(),
            "sou": _empty_bucket(),
            "honor": _empty_bucket(),
        },
        "by_round_event_phase": {
            "early": _empty_bucket(),
            "middle": _empty_bucket(),
            "late": _empty_bucket(),
        },
        "by_seat_turn_phase": {
            "early": _empty_bucket(),
            "middle": _empty_bucket(),
            "late": _empty_bucket(),
        },
    }

    for example in examples:
        if example.action.tile is None:
            raise ValueError("discard examples must have tile actions")
        prediction = predict(example)
        correct = prediction == example.action.tile
        shanten_bucket = _shanten_bucket(example)
        family_bucket = _tile_family(example.action.tile)
        phase_bucket = _round_event_phase(example.event_index)
        seat_turn_bucket = _seat_turn_phase(example.seat_turn_index)

        _record(buckets["overall"], correct)
        _record(buckets["by_shanten_delta"][shanten_bucket], correct)
        _record(buckets["by_tile_family"][family_bucket], correct)
        _record(buckets["by_round_event_phase"][phase_bucket], correct)
        _record(buckets["by_seat_turn_phase"][seat_turn_bucket], correct)

    return _finalize(buckets)


def _empty_bucket() -> dict[str, int | float | None]:
    return {
        "examples": 0,
        "correct": 0,
        "accuracy": None,
    }


def _record(bucket: dict[str, int | float | None], correct: bool) -> None:
    bucket["examples"] = int(bucket["examples"] or 0) + 1
    bucket["correct"] = int(bucket["correct"] or 0) + int(correct)


def _finalize(value: Any) -> Any:
    if _is_bucket(value):
        examples = int(value["examples"])
        correct = int(value["correct"])
        return {
            "examples": examples,
            "correct": correct,
            "accuracy": None if examples == 0 else correct / examples,
        }
    if isinstance(value, dict):
        return {
            key: _finalize(child)
            for key, child in value.items()
        }
    return value


def _is_bucket(value: Any) -> bool:
    return (
        isinstance(value, dict)
        and set(value) == {"examples", "correct", "accuracy"}
    )


def _shanten_bucket(example: DiscardExample) -> str:
    delta = discard_shanten_delta(example)
    if delta.delta < 0:
        return "improved"
    if delta.delta == 0:
        return "preserved"
    return "worsened"


def _tile_family(tile: TileType) -> str:
    if tile.index < 9:
        return "man"
    if tile.index < 18:
        return "pin"
    if tile.index < 27:
        return "sou"
    return "honor"


def _round_event_phase(event_index: int) -> str:
    if event_index < 40:
        return "early"
    if event_index < 100:
        return "middle"
    return "late"


def _seat_turn_phase(seat_turn_index: int) -> str:
    if seat_turn_index < 6:
        return "early"
    if seat_turn_index < 12:
        return "middle"
    return "late"
