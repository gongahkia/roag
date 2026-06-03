from __future__ import annotations

import json
from collections.abc import Sequence
from dataclasses import dataclass
from math import exp
from pathlib import Path
from typing import Any

from kenjaku.core import TileType, shanten
from kenjaku.training import DiscardExample

FEATURE_DIM = 76
MODEL_KIND = "discard-linear-v1"


@dataclass(frozen=True, slots=True)
class DiscardLinearModel:
    """Tiny dependency-free softmax model for discard prediction."""

    weights: tuple[tuple[float, ...], ...]
    epochs: int
    learning_rate: float

    def __post_init__(self) -> None:
        if len(self.weights) != 34:
            raise ValueError("discard model must have 34 output rows")
        if any(len(row) != FEATURE_DIM for row in self.weights):
            raise ValueError(f"discard model rows must have {FEATURE_DIM} features")

    @classmethod
    def fit(
        cls,
        examples: Sequence[DiscardExample],
        *,
        epochs: int = 25,
        learning_rate: float = 0.1,
        l2: float = 0.0,
    ) -> DiscardLinearModel:
        if not examples:
            raise ValueError("cannot train on zero examples")
        if epochs <= 0:
            raise ValueError("epochs must be positive")
        if learning_rate <= 0:
            raise ValueError("learning_rate must be positive")
        if l2 < 0:
            raise ValueError("l2 must be non-negative")

        weights = [[0.0] * FEATURE_DIM for _ in range(34)]
        for _ in range(epochs):
            for example in examples:
                _apply_update(weights, example, learning_rate=learning_rate, l2=l2)

        return cls(
            weights=tuple(tuple(row) for row in weights),
            epochs=epochs,
            learning_rate=learning_rate,
        )

    def predict(self, hand_counts: tuple[int, ...], visible_counts: tuple[int, ...]) -> TileType:
        legal_indices = _legal_indices(hand_counts)
        features_by_tile = _feature_vectors(hand_counts, visible_counts, legal_indices)
        logits = _logits(self.weights, features_by_tile)
        return TileType(max(logits, key=logits.get))

    def score(self, examples: Sequence[DiscardExample]) -> float:
        if not examples:
            raise ValueError("cannot score on zero examples")
        correct = 0
        for example in examples:
            if example.action.tile is None:
                raise ValueError("discard examples must have tile actions")
            prediction = self.predict(example.hand_counts, example.visible_counts)
            correct += prediction == example.action.tile
        return correct / len(examples)

    @property
    def kind(self) -> str:
        return MODEL_KIND

    @property
    def feature_dim(self) -> int:
        return FEATURE_DIM

    def to_dict(self) -> dict[str, Any]:
        return {
            "kind": self.kind,
            "feature_dim": self.feature_dim,
            "epochs": self.epochs,
            "learning_rate": self.learning_rate,
            "weights": [list(row) for row in self.weights],
        }

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> DiscardLinearModel:
        if payload.get("kind") != MODEL_KIND:
            raise ValueError("unsupported discard linear model kind")
        if payload.get("feature_dim") != FEATURE_DIM:
            raise ValueError("unsupported discard linear model feature dimension")

        weights_payload = payload.get("weights")
        if not isinstance(weights_payload, list):
            raise ValueError("model payload missing weights")
        weights = tuple(_parse_weight_row(row) for row in weights_payload)
        return cls(
            weights=weights,
            epochs=int(payload["epochs"]),
            learning_rate=float(payload["learning_rate"]),
        )

    def save(self, path: str | Path) -> None:
        Path(path).write_text(
            json.dumps(self.to_dict(), indent=2, sort_keys=True) + "\n",
            encoding="utf-8",
        )

    @classmethod
    def load(cls, path: str | Path) -> DiscardLinearModel:
        payload = json.loads(Path(path).read_text(encoding="utf-8"))
        if not isinstance(payload, dict):
            raise ValueError("model artifact must contain a JSON object")
        return cls.from_dict(payload)


def _apply_update(
    weights: list[list[float]],
    example: DiscardExample,
    *,
    learning_rate: float,
    l2: float,
) -> None:
    if example.action.tile is None:
        raise ValueError("discard examples must have tile actions")
    legal_indices = _legal_indices(example.hand_counts)
    target = example.action.tile.index
    if target not in legal_indices:
        raise ValueError("discard action must be legal for the example hand")

    features_by_tile = _feature_vectors(
        example.hand_counts,
        example.visible_counts,
        legal_indices,
    )
    probabilities = _softmax(_logits(weights, features_by_tile))
    for tile_index, probability in probabilities.items():
        error = probability - (1.0 if tile_index == target else 0.0)
        row = weights[tile_index]
        features = features_by_tile[tile_index]
        for feature_index, feature_value in enumerate(features):
            regularization = l2 * row[feature_index]
            row[feature_index] -= learning_rate * (error * feature_value + regularization)


def _feature_vectors(
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
    legal_indices: tuple[int, ...],
) -> dict[int, tuple[float, ...]]:
    if len(hand_counts) != 34:
        raise ValueError("hand counts must have length 34")
    if len(visible_counts) != 34:
        raise ValueError("visible counts must have length 34")
    before_shanten = shanten(hand_counts)
    return {
        tile_index: _features(
            hand_counts,
            visible_counts,
            tile_index=tile_index,
            before_shanten=before_shanten,
        )
        for tile_index in legal_indices
    }


def _features(
    hand_counts: tuple[int, ...],
    visible_counts: tuple[int, ...],
    *,
    tile_index: int,
    before_shanten: int,
) -> tuple[float, ...]:
    after_counts = list(hand_counts)
    after_counts[tile_index] -= 1
    after_shanten = shanten(tuple(after_counts))
    shanten_delta = after_shanten - before_shanten
    return (
        1.0,
        *(count / 4.0 for count in hand_counts),
        *(count / 4.0 for count in visible_counts),
        hand_counts[tile_index] / 4.0,
        visible_counts[tile_index] / 4.0,
        1.0 if _is_terminal_or_honor(tile_index) else 0.0,
        before_shanten / 8.0,
        after_shanten / 8.0,
        float(shanten_delta),
        1.0 if shanten_delta <= 0 else 0.0,
    )


def _legal_indices(hand_counts: tuple[int, ...]) -> tuple[int, ...]:
    legal_indices = tuple(index for index, count in enumerate(hand_counts) if count > 0)
    if not legal_indices:
        raise ValueError("cannot predict a discard from an empty hand")
    return legal_indices


def _logits(
    weights: Sequence[Sequence[float]],
    features_by_tile: dict[int, tuple[float, ...]],
) -> dict[int, float]:
    return {
        tile_index: sum(weight * feature for weight, feature in zip(weights[tile_index], features))
        for tile_index, features in features_by_tile.items()
    }


def _softmax(logits: dict[int, float]) -> dict[int, float]:
    max_logit = max(logits.values())
    exp_values = {
        tile_index: exp(logit - max_logit)
        for tile_index, logit in logits.items()
    }
    denominator = sum(exp_values.values())
    return {
        tile_index: value / denominator
        for tile_index, value in exp_values.items()
    }


def _parse_weight_row(row: Any) -> tuple[float, ...]:
    if not isinstance(row, list):
        raise ValueError("model weight rows must be lists")
    return tuple(float(value) for value in row)


def _is_terminal_or_honor(tile_index: int) -> bool:
    return tile_index >= 27 or tile_index % 9 in {0, 8}
