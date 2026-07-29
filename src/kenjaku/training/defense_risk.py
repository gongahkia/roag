from __future__ import annotations

from collections import Counter
from collections.abc import Sequence
from dataclasses import dataclass
from typing import Any

from kenjaku.core import TileType
from kenjaku.training.defense_features import (
    active_riichi_opponents,
    candidate_has_kabe,
    candidate_has_one_chance,
    candidate_has_sotogawa,
    candidate_has_suji,
    candidate_is_genbutsu,
    candidate_seen_after_riichi,
    candidate_seen_before_riichi,
    max_active_riichi_discards_elapsed,
    min_active_riichi_discards_elapsed,
)
from kenjaku.training.discard_examples import DiscardExample
from kenjaku.training.outcomes import RoundOutcome


@dataclass(frozen=True, slots=True)
class DefenseRiskScore:
    """Heuristic discard danger score against active riichi opponents.

    `risk` is normalized to 0..1 for ranking and diagnostics. It is not a
    calibrated deal-in probability.
    """

    tile: TileType
    risk: float
    active_riichi_opponents: int
    calibrated_probability: bool
    safety_reasons: tuple[str, ...]
    danger_reasons: tuple[str, ...]


def candidate_defense_risk(example: DiscardExample, tile: TileType | int) -> DefenseRiskScore:
    tile_type = _tile_type(tile)
    opponents = active_riichi_opponents(example)
    if not opponents:
        return DefenseRiskScore(
            tile=tile_type,
            risk=0.0,
            active_riichi_opponents=0,
            calibrated_probability=False,
            safety_reasons=(),
            danger_reasons=("no_active_riichi_opponent",),
        )

    risk = 0.55
    safety_reasons: list[str] = []
    danger_reasons: list[str] = []

    if candidate_is_genbutsu(example, tile_type):
        risk -= 0.45
        safety_reasons.append("genbutsu")
    if candidate_seen_after_riichi(example, tile_type):
        risk -= 0.35
        safety_reasons.append("seen_after_riichi")
    if candidate_has_kabe(example, tile_type):
        risk -= 0.20
        safety_reasons.append("kabe")
    if candidate_has_suji(example, tile_type):
        risk -= 0.18
        safety_reasons.append("suji")
    if candidate_has_one_chance(example, tile_type):
        risk -= 0.10
        safety_reasons.append("one_chance")
    if candidate_has_sotogawa(example, tile_type):
        risk -= 0.08
        safety_reasons.append("sotogawa")
    if candidate_seen_before_riichi(example, tile_type):
        risk -= 0.05
        safety_reasons.append("seen_before_riichi")

    visible_count = example.visible_counts[tile_type.index]
    unseen_count = max(0, 4 - visible_count)
    if unseen_count == 4:
        risk += 0.15
        danger_reasons.append("fully_live")
    elif unseen_count == 3:
        risk += 0.08
        danger_reasons.append("mostly_live")

    if tile_type.is_terminal_or_honor and unseen_count >= 3:
        risk += 0.08
        danger_reasons.append("live_terminal_or_honor")

    if len(opponents) > 1:
        risk += 0.10 * (len(opponents) - 1)
        danger_reasons.append("multiple_active_riichi")

    if _active_ippatsu_opponents(example, opponents):
        risk += 0.10
        danger_reasons.append("ippatsu")

    min_elapsed = min_active_riichi_discards_elapsed(example)
    max_elapsed = max_active_riichi_discards_elapsed(example)
    if min_elapsed <= 1:
        risk += 0.08
        danger_reasons.append("fresh_riichi")
    elif max_elapsed >= 6:
        risk -= 0.05
        safety_reasons.append("late_riichi")

    return DefenseRiskScore(
        tile=tile_type,
        risk=_clamp(risk),
        active_riichi_opponents=len(opponents),
        calibrated_probability=False,
        safety_reasons=tuple(safety_reasons),
        danger_reasons=tuple(danger_reasons),
    )


def legal_candidate_defense_risks(example: DiscardExample) -> tuple[DefenseRiskScore, ...]:
    scores = [
        candidate_defense_risk(example, tile_index)
        for tile_index, count in enumerate(example.hand_counts)
        if count > 0
    ]
    return tuple(
        sorted(
            scores,
            key=lambda score: (-score.risk, score.tile.index),
        )
    )


def summarize_defense_risks(examples: Sequence[DiscardExample]) -> dict[str, Any]:
    actual_scores: list[DefenseRiskScore] = []
    highest_candidate_scores: list[DefenseRiskScore] = []
    active_examples = 0
    safety_reasons: Counter[str] = Counter()
    danger_reasons: Counter[str] = Counter()
    bands = {
        "low": {"examples": 0},
        "medium": {"examples": 0},
        "high": {"examples": 0},
    }

    for example in examples:
        if example.action.tile is None:
            raise ValueError("discard examples must have tile actions")
        actual = candidate_defense_risk(example, example.action.tile)
        candidates = legal_candidate_defense_risks(example)
        actual_scores.append(actual)
        if actual.active_riichi_opponents:
            active_examples += 1
        if candidates:
            highest_candidate_scores.append(candidates[0])
        safety_reasons.update(actual.safety_reasons)
        danger_reasons.update(actual.danger_reasons)
        bands[_risk_band(actual.risk)]["examples"] += 1

    return {
        "kind": "kenjaku-defense-risk-summary-v0",
        "examples": len(examples),
        "active_riichi_examples": active_examples,
        "calibrated_probability": False,
        "actual_discard_risk": _risk_distribution(actual_scores),
        "highest_candidate_risk": _risk_distribution(highest_candidate_scores),
        "actual_discard_risk_bands": bands,
        "actual_safety_reasons": _reason_counts(safety_reasons),
        "actual_danger_reasons": _reason_counts(danger_reasons),
    }


def summarize_defense_risk_outcomes(
    examples: Sequence[DiscardExample],
    outcomes: Sequence[RoundOutcome | None],
) -> dict[str, Any]:
    buckets: dict[str, list[DefenseRiskScore]] = {
        "eventual_deal_in": [],
        "no_eventual_deal_in": [],
        "eventual_win": [],
        "eventual_draw": [],
        "active_riichi_eventual_deal_in": [],
        "active_riichi_no_eventual_deal_in": [],
    }
    missing_outcomes = 0
    labeled_examples = 0

    for example in examples:
        if example.action.tile is None:
            raise ValueError("discard examples must have tile actions")
        outcome = outcomes[example.round_index] if example.round_index < len(outcomes) else None
        if outcome is None:
            missing_outcomes += 1
            continue

        labeled_examples += 1
        score = candidate_defense_risk(example, example.action.tile)
        seat = example.seat
        deal_in = _seat_flag(outcome.deal_in_flags, seat)
        win = _seat_flag(outcome.win_flags, seat)
        draw = _seat_flag(outcome.draw_flags, seat)

        buckets["eventual_deal_in" if deal_in else "no_eventual_deal_in"].append(score)
        if win:
            buckets["eventual_win"].append(score)
        if draw:
            buckets["eventual_draw"].append(score)
        if score.active_riichi_opponents:
            buckets[
                "active_riichi_eventual_deal_in" if deal_in else "active_riichi_no_eventual_deal_in"
            ].append(score)

    return {
        "kind": "kenjaku-defense-risk-outcome-analysis-v0",
        "examples": len(examples),
        "labeled_examples": labeled_examples,
        "missing_outcomes": missing_outcomes,
        "calibrated_probability": False,
        "buckets": {name: _risk_distribution(scores) for name, scores in buckets.items()},
    }


def _active_ippatsu_opponents(example: DiscardExample, opponents: tuple[int, ...]) -> bool:
    return any(
        seat < len(example.ippatsu_active_seats) and example.ippatsu_active_seats[seat]
        for seat in opponents
    )


def _clamp(value: float) -> float:
    return min(1.0, max(0.0, value))


def _tile_type(tile: TileType | int) -> TileType:
    if isinstance(tile, TileType):
        return tile
    return TileType(tile)


def _risk_distribution(scores: Sequence[DefenseRiskScore]) -> dict[str, float | int | None]:
    if not scores:
        return {
            "examples": 0,
            "mean": None,
            "min": None,
            "max": None,
        }
    risks = [score.risk for score in scores]
    return {
        "examples": len(risks),
        "mean": sum(risks) / len(risks),
        "min": min(risks),
        "max": max(risks),
    }


def _reason_counts(counter: Counter[str]) -> list[dict[str, int | str]]:
    return [
        {"reason": reason, "count": count}
        for reason, count in sorted(counter.items(), key=lambda item: (-item[1], item[0]))
    ]


def _risk_band(risk: float) -> str:
    if risk < 0.33:
        return "low"
    if risk < 0.66:
        return "medium"
    return "high"


def _seat_flag(flags: Sequence[bool], seat: int) -> bool:
    return seat < len(flags) and flags[seat]
