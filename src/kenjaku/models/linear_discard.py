from __future__ import annotations

from collections.abc import Sequence
from dataclasses import dataclass
from math import exp

from kenjaku.core import TileType
from kenjaku.training import DiscardExample

FEATURE_DIM = 69


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
        features = _features(hand_counts, visible_counts)
        legal_indices = _legal_indices(hand_counts)
        logits = _logits(self.weights, features, legal_indices)
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

    features = _features(example.hand_counts, example.visible_counts)
    probabilities = _softmax(_logits(weights, features, legal_indices))
    for tile_index, probability in probabilities.items():
        error = probability - (1.0 if tile_index == target else 0.0)
        row = weights[tile_index]
        for feature_index, feature_value in enumerate(features):
            regularization = l2 * row[feature_index]
            row[feature_index] -= learning_rate * (error * feature_value + regularization)


def _features(hand_counts: tuple[int, ...], visible_counts: tuple[int, ...]) -> tuple[float, ...]:
    if len(hand_counts) != 34:
        raise ValueError("hand counts must have length 34")
    if len(visible_counts) != 34:
        raise ValueError("visible counts must have length 34")
    return (
        1.0,
        *(count / 4.0 for count in hand_counts),
        *(count / 4.0 for count in visible_counts),
    )


def _legal_indices(hand_counts: tuple[int, ...]) -> tuple[int, ...]:
    legal_indices = tuple(index for index, count in enumerate(hand_counts) if count > 0)
    if not legal_indices:
        raise ValueError("cannot predict a discard from an empty hand")
    return legal_indices


def _logits(
    weights: Sequence[Sequence[float]],
    features: tuple[float, ...],
    legal_indices: tuple[int, ...],
) -> dict[int, float]:
    return {
        tile_index: sum(weight * feature for weight, feature in zip(weights[tile_index], features))
        for tile_index in legal_indices
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
