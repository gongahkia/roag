from __future__ import annotations

import json
from collections.abc import Sequence
from dataclasses import dataclass
from functools import cache
from math import exp
from pathlib import Path
from typing import Any

from kenjaku.core import ActionKind, all_tile_types, shanten
from kenjaku.models.riichi_frequency import RIICHI_DECISION_KINDS
from kenjaku.training import RiichiExample

RIICHI_LINEAR_MODEL_KIND = "riichi-linear-v0"

_ALL_TILE_TYPES = all_tile_types()
_TILE_FEATURE_NAMES = tuple(tile_type.notation for tile_type in _ALL_TILE_TYPES)
RIICHI_LINEAR_FEATURE_NAMES = (
    "bias",
    *(f"candidate_{kind.value}" for kind in RIICHI_DECISION_KINDS),
    *(f"hand_count_{name}" for name in _TILE_FEATURE_NAMES),
    *(f"visible_count_{name}" for name in _TILE_FEATURE_NAMES),
    "seat_score_fraction",
    "seat_has_riichi_stick_points",
    "score_delta_to_top",
    "score_rank_fraction",
    "seat_is_dealer",
    "seat_turn_fraction",
    "own_riichi_active",
    "active_riichi_opponent_fraction",
    "has_active_riichi_opponent",
    "self_river_count_fraction",
    "opponent_river_count_fraction",
    "all_river_count_fraction",
    "closed_shanten_proxy",
    "closed_tenpai_proxy",
    "terminal_honor_count",
    "pair_count",
    "triplet_count",
    "unique_tile_count",
)
RIICHI_LINEAR_FEATURE_DIM = len(RIICHI_LINEAR_FEATURE_NAMES)


@dataclass(frozen=True, slots=True)
class _PreparedRiichiExample:
    target: ActionKind
    features_by_kind: dict[ActionKind, tuple[float, ...]]


@dataclass(frozen=True, slots=True)
class RiichiLinearModel:
    """Tiny dependency-free masked softmax model for riichi/pass decisions."""

    weights: tuple[tuple[float, ...], ...]
    epochs: int
    learning_rate: float
    l2: float = 0.0
    positive_class_weight: float = 1.0

    def __post_init__(self) -> None:
        if len(self.weights) != len(RIICHI_DECISION_KINDS):
            raise ValueError("riichi linear model has unsupported output rows")
        if any(len(row) != RIICHI_LINEAR_FEATURE_DIM for row in self.weights):
            raise ValueError(
                f"riichi linear model rows must have {RIICHI_LINEAR_FEATURE_DIM} features"
            )
        if self.epochs <= 0:
            raise ValueError("epochs must be positive")
        if self.learning_rate <= 0:
            raise ValueError("learning_rate must be positive")
        if self.l2 < 0:
            raise ValueError("l2 must be non-negative")
        if self.positive_class_weight <= 0:
            raise ValueError("positive_class_weight must be positive")

    @classmethod
    def fit(
        cls,
        examples: list[RiichiExample],
        *,
        epochs: int = 25,
        learning_rate: float = 0.1,
        l2: float = 0.0,
        positive_class_weight: float = 1.0,
    ) -> RiichiLinearModel:
        if not examples:
            raise ValueError("cannot train on zero examples")
        if epochs <= 0:
            raise ValueError("epochs must be positive")
        if learning_rate <= 0:
            raise ValueError("learning_rate must be positive")
        if l2 < 0:
            raise ValueError("l2 must be non-negative")
        if positive_class_weight <= 0:
            raise ValueError("positive_class_weight must be positive")

        weights = [[0.0] * RIICHI_LINEAR_FEATURE_DIM for _ in RIICHI_DECISION_KINDS]
        prepared_examples = tuple(_prepare_example(example) for example in examples)
        for _ in range(epochs):
            for example in prepared_examples:
                _apply_update(
                    weights,
                    example,
                    learning_rate=learning_rate,
                    l2=l2,
                    example_weight=(
                        positive_class_weight
                        if example.target == ActionKind.RIICHI
                        else 1.0
                    ),
                )

        return cls(
            weights=tuple(tuple(row) for row in weights),
            epochs=epochs,
            learning_rate=learning_rate,
            l2=l2,
            positive_class_weight=positive_class_weight,
        )

    @property
    def kind(self) -> str:
        return RIICHI_LINEAR_MODEL_KIND

    @property
    def feature_dim(self) -> int:
        return RIICHI_LINEAR_FEATURE_DIM

    @property
    def feature_names(self) -> tuple[str, ...]:
        return RIICHI_LINEAR_FEATURE_NAMES

    def to_dict(self) -> dict[str, Any]:
        return {
            "kind": self.kind,
            "feature_dim": self.feature_dim,
            "epochs": self.epochs,
            "learning_rate": self.learning_rate,
            "l2": self.l2,
            "positive_class_weight": self.positive_class_weight,
            "weights": [list(row) for row in self.weights],
        }

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> RiichiLinearModel:
        if payload.get("kind") != RIICHI_LINEAR_MODEL_KIND:
            raise ValueError("unsupported riichi linear model kind")
        if payload.get("feature_dim") != RIICHI_LINEAR_FEATURE_DIM:
            raise ValueError("unsupported riichi linear model feature dimension")
        weights_payload = payload.get("weights")
        if not isinstance(weights_payload, list):
            raise ValueError("model payload missing weights")
        return cls(
            weights=tuple(_parse_weight_row(row) for row in weights_payload),
            epochs=int(payload["epochs"]),
            learning_rate=float(payload["learning_rate"]),
            l2=float(payload.get("l2", 0.0)),
            positive_class_weight=float(payload.get("positive_class_weight", 1.0)),
        )

    def save(self, path: str | Path) -> None:
        Path(path).write_text(
            json.dumps(self.to_dict(), indent=2, sort_keys=True) + "\n",
            encoding="utf-8",
        )

    @classmethod
    def load(cls, path: str | Path) -> RiichiLinearModel:
        payload = json.loads(Path(path).read_text(encoding="utf-8"))
        if not isinstance(payload, dict):
            raise ValueError("model artifact must contain a JSON object")
        return cls.from_dict(payload)

    def predict(self, example: RiichiExample) -> ActionKind:
        logits = self.logits_for_example(example)
        return max(logits, key=lambda kind: (logits[kind], -_kind_index(kind)))

    def logits_for_example(self, example: RiichiExample) -> dict[ActionKind, float]:
        prepared = _prepare_example(example)
        return self.logits_for_prepared(prepared)

    def probabilities_for_example(self, example: RiichiExample) -> dict[ActionKind, float]:
        return _softmax(self.logits_for_example(example))

    def prepare_examples(
        self,
        examples: Sequence[RiichiExample],
    ) -> tuple[_PreparedRiichiExample, ...]:
        return tuple(_prepare_example(example) for example in examples)

    def predict_prepared(self, prepared: _PreparedRiichiExample) -> ActionKind:
        logits = self.logits_for_prepared(prepared)
        return max(logits, key=lambda kind: (logits[kind], -_kind_index(kind)))

    def logits_for_prepared(
        self,
        prepared: _PreparedRiichiExample,
    ) -> dict[ActionKind, float]:
        return {
            kind: _dot(self.weights[_kind_index(kind)], features)
            for kind, features in prepared.features_by_kind.items()
        }

    def probabilities_for_prepared(
        self,
        prepared: _PreparedRiichiExample,
    ) -> dict[ActionKind, float]:
        return _softmax(self.logits_for_prepared(prepared))

    def score(self, examples: list[RiichiExample]) -> float:
        if not examples:
            raise ValueError("cannot score on zero examples")
        prepared_examples = self.prepare_examples(examples)
        correct = sum(
            self.predict_prepared(prepared) == example.action.kind
            for example, prepared in zip(examples, prepared_examples, strict=True)
        )
        return correct / len(examples)


def _prepare_example(example: RiichiExample) -> _PreparedRiichiExample:
    if example.action.kind not in RIICHI_DECISION_KINDS:
        raise ValueError(f"unsupported riichi action kind: {example.action.kind.value}")
    return _PreparedRiichiExample(
        target=example.action.kind,
        features_by_kind={
            kind: _features_for_candidate(example, kind)
            for kind in RIICHI_DECISION_KINDS
        },
    )


def _features_for_candidate(example: RiichiExample, kind: ActionKind) -> tuple[float, ...]:
    players = max(4, len(example.scores), example.seat + 1, len(example.active_riichi_seats))
    score = _seat_score(example)
    top_score = max(example.scores) if example.scores else score
    score_rank = _score_rank(example.scores, example.seat)
    active_opponents = sum(
        1
        for seat in range(players)
        if seat != example.seat and _active_riichi_at(example.active_riichi_seats, seat)
    )
    self_river_count = _river_count(example, example.seat)
    opponent_river_count = sum(
        _river_count(example, seat)
        for seat in range(players)
        if seat != example.seat
    )
    hand_total = max(1, sum(example.hand_counts))
    hand_shanten = _safe_shanten(example.hand_counts)
    terminal_honor_count = sum(
        count
        for index, count in enumerate(example.hand_counts)
        if _ALL_TILE_TYPES[index].is_terminal_or_honor
    )
    pair_count = sum(count >= 2 for count in example.hand_counts)
    triplet_count = sum(count >= 3 for count in example.hand_counts)
    unique_count = sum(count > 0 for count in example.hand_counts)

    features: list[float] = [1.0]
    features.extend(1.0 if kind == candidate else 0.0 for candidate in RIICHI_DECISION_KINDS)
    features.extend(count / 4.0 for count in example.hand_counts)
    features.extend(count / 4.0 for count in example.visible_counts)
    features.extend(
        (
            score / 25000.0,
            1.0 if score >= 1000 else 0.0,
            (score - top_score) / 25000.0,
            score_rank / max(1, players - 1),
            1.0 if example.seat == example.dealer else 0.0,
            example.seat_turn_index / 18.0,
            1.0 if _active_riichi_at(example.active_riichi_seats, example.seat) else 0.0,
            active_opponents / max(1, players - 1),
            1.0 if active_opponents else 0.0,
            self_river_count / 18.0,
            opponent_river_count / max(1, 18 * (players - 1)),
            (self_river_count + opponent_river_count) / max(1, 18 * players),
            hand_shanten / 8.0,
            1.0 if hand_shanten == 0 else 0.0,
            terminal_honor_count / hand_total,
            pair_count / 7.0,
            triplet_count / 4.0,
            unique_count / hand_total,
        )
    )

    if len(features) != RIICHI_LINEAR_FEATURE_DIM:
        raise ValueError("riichi linear feature construction drifted from feature names")
    return tuple(features)


def _seat_score(example: RiichiExample) -> int:
    if example.seat < len(example.scores):
        return example.scores[example.seat]
    return 0


def _score_rank(scores: tuple[int, ...], seat: int) -> float:
    if not scores or seat >= len(scores):
        return 0.0
    seat_score = scores[seat]
    return float(sum(score > seat_score for score in scores))


def _active_riichi_at(active_riichi_seats: tuple[bool, ...], seat: int) -> bool:
    return seat < len(active_riichi_seats) and active_riichi_seats[seat]


def _river_count(example: RiichiExample, seat: int) -> int:
    if seat >= len(example.river_counts_by_seat):
        return 0
    return sum(example.river_counts_by_seat[seat])


@cache
def _safe_shanten(counts: tuple[int, ...]) -> int:
    try:
        return shanten(counts)
    except ValueError:
        return 8


def _apply_update(
    weights: list[list[float]],
    example: _PreparedRiichiExample,
    *,
    learning_rate: float,
    l2: float,
    example_weight: float,
) -> None:
    logits = {
        kind: _dot(weights[_kind_index(kind)], features)
        for kind, features in example.features_by_kind.items()
    }
    probabilities = _softmax(logits)
    for kind, features in example.features_by_kind.items():
        row = weights[_kind_index(kind)]
        target = 1.0 if kind == example.target else 0.0
        error = probabilities[kind] - target
        scaled_error = learning_rate * example_weight * error
        if l2 == 0:
            for index, value in enumerate(features):
                if value:
                    row[index] -= scaled_error * value
        else:
            for index, value in enumerate(features):
                row[index] -= learning_rate * (example_weight * error * value + l2 * row[index])


def _softmax(logits: dict[ActionKind, float]) -> dict[ActionKind, float]:
    max_logit = max(logits.values())
    exp_values = {
        kind: exp(logit - max_logit)
        for kind, logit in logits.items()
    }
    total = sum(exp_values.values())
    return {
        kind: value / total
        for kind, value in exp_values.items()
    }


def _dot(weights: tuple[float, ...] | list[float], features: tuple[float, ...]) -> float:
    total = 0.0
    for weight, feature in zip(weights, features, strict=True):
        if feature:
            total += weight * feature
    return total


def _kind_index(kind: ActionKind) -> int:
    if kind not in RIICHI_DECISION_KINDS:
        raise ValueError(f"unsupported riichi decision kind: {kind.value}")
    return RIICHI_DECISION_KINDS.index(kind)


def _parse_weight_row(row: Any) -> tuple[float, ...]:
    if not isinstance(row, list):
        raise ValueError("model weight rows must be lists")
    return tuple(float(value) for value in row)
