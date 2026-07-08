from __future__ import annotations

import json
from collections.abc import Sequence
from dataclasses import dataclass
from math import exp, log
from pathlib import Path
from typing import Any, cast

from kenjaku.core import TileType
from kenjaku.training.deal_in import DealInExample
from kenjaku.training.defense_features import (
    active_riichi_opponents,
    actual_discard_is_tsumogiri,
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
from kenjaku.training.defense_risk import candidate_defense_risk

DEAL_IN_LINEAR_MODEL_KIND = "deal-in-linear-v0"
DEAL_IN_LINEAR_FEATURE_NAMES = (
    "bias",
    "heuristic_defense_risk",
    "active_riichi_opponent_fraction",
    "has_active_riichi_opponent",
    "candidate_genbutsu",
    "candidate_suji",
    "candidate_kabe",
    "candidate_one_chance",
    "candidate_sotogawa",
    "candidate_seen_after_riichi",
    "candidate_seen_before_riichi",
    "candidate_visible_count",
    "candidate_unseen_count",
    "candidate_hand_count",
    "discard_is_tsumogiri",
    "candidate_terminal_or_honor",
    "candidate_honor",
    "candidate_terminal",
    "active_riichi_min_elapsed_fraction",
    "active_riichi_max_elapsed_fraction",
    "active_ippatsu_fraction",
    "seat_turn_fraction",
    "seat_is_dealer",
    "seat_score_fraction",
    "safety_reason_fraction",
    "danger_reason_fraction",
)
DEAL_IN_LINEAR_FEATURE_DIM = len(DEAL_IN_LINEAR_FEATURE_NAMES)


@dataclass(frozen=True, slots=True)
class DealInLinearModel:
    """Small dependency-free logistic model for direct ron-discard probability."""

    weights: tuple[float, ...]
    epochs: int
    learning_rate: float
    l2: float = 0.0
    positive_class_weight: float = 1.0

    def __post_init__(self) -> None:
        if len(self.weights) != DEAL_IN_LINEAR_FEATURE_DIM:
            raise ValueError(f"deal-in model rows must have {DEAL_IN_LINEAR_FEATURE_DIM} features")
        if self.epochs < 0:
            raise ValueError("epochs must be non-negative")
        if self.learning_rate <= 0:
            raise ValueError("learning_rate must be positive")
        if self.l2 < 0:
            raise ValueError("l2 must be non-negative")
        if self.positive_class_weight <= 0:
            raise ValueError("positive_class_weight must be positive")

    @classmethod
    def fit(
        cls,
        examples: Sequence[DealInExample],
        *,
        epochs: int = 50,
        learning_rate: float = 0.1,
        l2: float = 0.0,
        positive_class_weight: float = 1.0,
    ) -> DealInLinearModel:
        if not examples:
            raise ValueError("cannot train on zero deal-in examples")
        if epochs < 0:
            raise ValueError("epochs must be non-negative")
        if learning_rate <= 0:
            raise ValueError("learning_rate must be positive")
        if l2 < 0:
            raise ValueError("l2 must be non-negative")
        if positive_class_weight <= 0:
            raise ValueError("positive_class_weight must be positive")

        weights = [0.0] * DEAL_IN_LINEAR_FEATURE_DIM
        prepared = tuple(_PreparedDealInExample.from_example(example) for example in examples)
        for _ in range(epochs):
            for example in prepared:
                _apply_update(
                    weights,
                    example,
                    learning_rate=learning_rate,
                    l2=l2,
                    example_weight=positive_class_weight if example.target else 1.0,
                )

        return cls(
            weights=tuple(weights),
            epochs=epochs,
            learning_rate=learning_rate,
            l2=l2,
            positive_class_weight=positive_class_weight,
        )

    @property
    def kind(self) -> str:
        return DEAL_IN_LINEAR_MODEL_KIND

    @property
    def feature_dim(self) -> int:
        return DEAL_IN_LINEAR_FEATURE_DIM

    @property
    def feature_names(self) -> tuple[str, ...]:
        return DEAL_IN_LINEAR_FEATURE_NAMES

    def predict_probability(self, example: DealInExample) -> float:
        return _sigmoid(_dot(self.weights, _features_for_example(example)))

    def evaluate(
        self,
        examples: Sequence[DealInExample],
        *,
        threshold: float = 0.5,
    ) -> dict[str, Any]:
        probabilities = [self.predict_probability(example) for example in examples]
        return evaluate_deal_in_probabilities(examples, probabilities, threshold=threshold)

    def weight_summary(self) -> dict[str, Any]:
        return {
            "feature_count": DEAL_IN_LINEAR_FEATURE_DIM,
            "features": [
                {
                    "index": index,
                    "name": name,
                    "weight": self.weights[index],
                }
                for index, name in enumerate(DEAL_IN_LINEAR_FEATURE_NAMES)
            ],
            "overall": _numeric_summary(self.weights),
        }

    def feature_summary(self, examples: Sequence[DealInExample]) -> dict[str, Any]:
        sums = [0.0] * DEAL_IN_LINEAR_FEATURE_DIM
        sum_abs = [0.0] * DEAL_IN_LINEAR_FEATURE_DIM
        max_abs = [0.0] * DEAL_IN_LINEAR_FEATURE_DIM
        nonzero = [0] * DEAL_IN_LINEAR_FEATURE_DIM
        for example in examples:
            for index, value in enumerate(_features_for_example(example)):
                sums[index] += value
                abs_value = abs(value)
                sum_abs[index] += abs_value
                max_abs[index] = max(max_abs[index], abs_value)
                if value != 0:
                    nonzero[index] += 1

        count = len(examples)
        return {
            "examples": count,
            "feature_count": DEAL_IN_LINEAR_FEATURE_DIM,
            "features": [
                {
                    "index": index,
                    "name": name,
                    "nonzero": nonzero[index],
                    "nonzero_rate": None if count == 0 else nonzero[index] / count,
                    "mean": None if count == 0 else sums[index] / count,
                    "mean_abs": None if count == 0 else sum_abs[index] / count,
                    "max_abs": None if count == 0 else max_abs[index],
                }
                for index, name in enumerate(DEAL_IN_LINEAR_FEATURE_NAMES)
            ],
        }

    def to_dict(self) -> dict[str, Any]:
        return {
            "kind": self.kind,
            "feature_dim": DEAL_IN_LINEAR_FEATURE_DIM,
            "epochs": self.epochs,
            "learning_rate": self.learning_rate,
            "l2": self.l2,
            "positive_class_weight": self.positive_class_weight,
            "weights": list(self.weights),
        }

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> DealInLinearModel:
        if payload.get("kind") != DEAL_IN_LINEAR_MODEL_KIND:
            raise ValueError("unsupported deal-in model kind")
        if payload.get("feature_dim") != DEAL_IN_LINEAR_FEATURE_DIM:
            raise ValueError("unsupported deal-in model feature dimension")
        weights_payload = payload.get("weights")
        if not isinstance(weights_payload, list):
            raise ValueError("model payload missing weights")
        weights_payload = cast(list[Any], weights_payload)
        return cls(
            weights=tuple(float(value) for value in weights_payload),
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
    def load(cls, path: str | Path) -> DealInLinearModel:
        payload = json.loads(Path(path).read_text(encoding="utf-8"))
        if not isinstance(payload, dict):
            raise ValueError("model artifact must contain a JSON object")
        payload = cast(dict[str, Any], payload)
        return cls.from_dict(payload)


@dataclass(frozen=True, slots=True)
class _PreparedDealInExample:
    features: tuple[float, ...]
    target: bool

    @classmethod
    def from_example(cls, example: DealInExample) -> _PreparedDealInExample:
        return cls(
            features=_features_for_example(example),
            target=example.dealt_in,
        )


def evaluate_deal_in_probabilities(
    examples: Sequence[DealInExample],
    probabilities: Sequence[float],
    *,
    threshold: float = 0.5,
) -> dict[str, Any]:
    if len(examples) != len(probabilities):
        raise ValueError("examples and probabilities must have the same length")
    if not 0.0 <= threshold <= 1.0:
        raise ValueError("threshold must be between 0.0 and 1.0")

    tp = tn = fp = fn = 0
    brier_sum = 0.0
    log_loss_sum = 0.0
    for example, probability in zip(examples, probabilities, strict=True):
        p = _clamped_probability(probability)
        target = 1.0 if example.dealt_in else 0.0
        predicted = p >= threshold
        if predicted and example.dealt_in:
            tp += 1
        elif predicted:
            fp += 1
        elif example.dealt_in:
            fn += 1
        else:
            tn += 1
        brier_sum += (p - target) ** 2
        log_loss_sum += -(target * log(p) + (1.0 - target) * log(1.0 - p))

    examples_count = len(examples)
    positives = tp + fn
    negatives = tn + fp
    predicted_positives = tp + fp
    return {
        "examples": examples_count,
        "positives": positives,
        "negatives": negatives,
        "positive_rate": None if examples_count == 0 else positives / examples_count,
        "threshold": threshold,
        "true_positive": tp,
        "true_negative": tn,
        "false_positive": fp,
        "false_negative": fn,
        "predicted_positive_rate": (
            None if examples_count == 0 else predicted_positives / examples_count
        ),
        "accuracy": None if examples_count == 0 else (tp + tn) / examples_count,
        "precision": None if predicted_positives == 0 else tp / predicted_positives,
        "recall": None if positives == 0 else tp / positives,
        "specificity": None if negatives == 0 else tn / negatives,
        "balanced_accuracy": _balanced_accuracy(tp=tp, tn=tn, fp=fp, fn=fn),
        "brier_score": None if examples_count == 0 else brier_sum / examples_count,
        "log_loss": None if examples_count == 0 else log_loss_sum / examples_count,
    }


def heuristic_deal_in_probabilities(examples: Sequence[DealInExample]) -> tuple[float, ...]:
    probabilities: list[float] = []
    for example in examples:
        tile = _actual_discard_tile(example)
        probabilities.append(candidate_defense_risk(example.discard, tile).risk)
    return tuple(probabilities)


def _features_for_example(example: DealInExample) -> tuple[float, ...]:
    discard = example.discard
    tile = _actual_discard_tile(example)
    risk = candidate_defense_risk(discard, tile)
    opponents = active_riichi_opponents(discard)
    players = max(4, len(discard.active_riichi_seats), discard.seat + 1)
    visible_count = discard.visible_counts[tile.index]
    unseen_count = max(0, 4 - visible_count)
    ippatsu_count = sum(
        1
        for seat in opponents
        if seat < len(discard.ippatsu_active_seats) and discard.ippatsu_active_seats[seat]
    )
    seat_score = discard.scores[discard.seat] if discard.seat < len(discard.scores) else 25000
    features = (
        1.0,
        risk.risk,
        len(opponents) / max(1, players - 1),
        1.0 if opponents else 0.0,
        1.0 if candidate_is_genbutsu(discard, tile) else 0.0,
        1.0 if candidate_has_suji(discard, tile) else 0.0,
        1.0 if candidate_has_kabe(discard, tile) else 0.0,
        1.0 if candidate_has_one_chance(discard, tile) else 0.0,
        1.0 if candidate_has_sotogawa(discard, tile) else 0.0,
        1.0 if candidate_seen_after_riichi(discard, tile) else 0.0,
        1.0 if candidate_seen_before_riichi(discard, tile) else 0.0,
        visible_count / 4.0,
        unseen_count / 4.0,
        discard.hand_counts[tile.index] / 4.0,
        1.0 if actual_discard_is_tsumogiri(discard) else 0.0,
        1.0 if tile.is_terminal_or_honor else 0.0,
        1.0 if tile.is_honor else 0.0,
        1.0 if tile.is_terminal else 0.0,
        min_active_riichi_discards_elapsed(discard) / 18.0,
        max_active_riichi_discards_elapsed(discard) / 18.0,
        ippatsu_count / max(1, players - 1),
        discard.seat_turn_index / 18.0,
        1.0 if discard.seat == discard.dealer else 0.0,
        seat_score / 50000.0,
        len(risk.safety_reasons) / 8.0,
        len(risk.danger_reasons) / 8.0,
    )
    if len(features) != DEAL_IN_LINEAR_FEATURE_DIM:
        raise ValueError("deal-in feature construction drifted from feature names")
    return features


def _apply_update(
    weights: list[float],
    example: _PreparedDealInExample,
    *,
    learning_rate: float,
    l2: float,
    example_weight: float,
) -> None:
    probability = _sigmoid(_dot(weights, example.features))
    target = 1.0 if example.target else 0.0
    error = probability - target
    for index, value in enumerate(example.features):
        weights[index] -= learning_rate * (example_weight * error * value + l2 * weights[index])


def _actual_discard_tile(example: DealInExample) -> TileType:
    tile = example.discard.action.tile
    if tile is None:
        raise ValueError("deal-in examples must wrap tile discard examples")
    return tile


def _dot(weights: Sequence[float], features: Sequence[float]) -> float:
    return sum(weight * value for weight, value in zip(weights, features, strict=True))


def _sigmoid(value: float) -> float:
    if value >= 0:
        inverse = exp(-value)
        return 1.0 / (1.0 + inverse)
    inverse = exp(value)
    return inverse / (1.0 + inverse)


def _clamped_probability(value: float) -> float:
    return min(1.0 - 1e-15, max(1e-15, float(value)))


def _balanced_accuracy(*, tp: int, tn: int, fp: int, fn: int) -> float | None:
    positives = tp + fn
    negatives = tn + fp
    if positives == 0 or negatives == 0:
        return None
    return ((tp / positives) + (tn / negatives)) / 2.0


def _numeric_summary(values: Sequence[float]) -> dict[str, float | int | None]:
    if not values:
        return {"count": 0, "mean": None, "min": None, "max": None, "mean_abs": None}
    return {
        "count": len(values),
        "mean": sum(values) / len(values),
        "min": min(values),
        "max": max(values),
        "mean_abs": sum(abs(value) for value in values) / len(values),
    }
