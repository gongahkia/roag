from __future__ import annotations

from collections.abc import Sequence
from dataclasses import dataclass
from typing import Any

from kenjaku.core import TileType
from kenjaku.features.deal_in import (
    DEAL_IN_LINEAR_FEATURE_DIM,
    DEAL_IN_LINEAR_FEATURE_NAMES,
    DEAL_IN_LINEAR_MODEL_KIND,
)
from kenjaku.models._linear_base import (
    LinearModel,
    PreparedExample,
    balanced_accuracy,
    clamped_probability,
    cross_entropy,
    dot,
    feature_summary_payload,
    fit_logistic_sgd,
    model_payload,
    numeric_summary,
    parse_weight_vector,
    prepared_binary_example,
    sigmoid,
)
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


@dataclass(frozen=True, slots=True)
class DealInLinearModel(LinearModel):
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

        prepared = tuple(_prepare_example(example) for example in examples)
        weights = fit_logistic_sgd(
            prepared,
            feature_dim=DEAL_IN_LINEAR_FEATURE_DIM,
            epochs=epochs,
            learning_rate=learning_rate,
            l2=l2,
            example_weight=lambda example: positive_class_weight if example.label else 1.0,
        )

        return cls(
            weights=weights,
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
        return sigmoid(dot(self.weights, _features_for_example(example)))

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
            "overall": numeric_summary(self.weights, empty_with_none=True),
        }

    def feature_summary(self, examples: Sequence[DealInExample]) -> dict[str, Any]:
        return feature_summary_payload(
            example_count=len(examples),
            feature_names=DEAL_IN_LINEAR_FEATURE_NAMES,
            vectors=(_features_for_example(example) for example in examples),
        )

    def to_dict(self) -> dict[str, Any]:
        return model_payload(
            kind=self.kind,
            feature_dim=DEAL_IN_LINEAR_FEATURE_DIM,
            epochs=self.epochs,
            learning_rate=self.learning_rate,
            l2=self.l2,
            positive_class_weight=self.positive_class_weight,
            weights=list(self.weights),
        )

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> DealInLinearModel:
        if payload.get("kind") != DEAL_IN_LINEAR_MODEL_KIND:
            raise ValueError("unsupported deal-in model kind")
        if payload.get("feature_dim") != DEAL_IN_LINEAR_FEATURE_DIM:
            raise ValueError("unsupported deal-in model feature dimension")
        return cls(
            weights=parse_weight_vector(payload.get("weights")),
            epochs=int(payload["epochs"]),
            learning_rate=float(payload["learning_rate"]),
            l2=float(payload.get("l2", 0.0)),
            positive_class_weight=float(payload.get("positive_class_weight", 1.0)),
        )


_PreparedDealInExample = PreparedExample[bool]


def _prepare_example(example: DealInExample) -> _PreparedDealInExample:
    return prepared_binary_example(features=_features_for_example(example), label=example.dealt_in)


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
        p = clamped_probability(probability)
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
        log_loss_sum += cross_entropy(p, example.dealt_in)

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
        "balanced_accuracy": balanced_accuracy(tp=tp, tn=tn, fp=fp, fn=fn),
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


def _actual_discard_tile(example: DealInExample) -> TileType:
    tile = example.discard.action.tile
    if tile is None:
        raise ValueError("deal-in examples must wrap tile discard examples")
    return tile
