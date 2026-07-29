from __future__ import annotations

import json
from abc import ABC, abstractmethod
from collections.abc import Callable, Hashable, Iterable, Mapping, Sequence
from dataclasses import dataclass
from math import exp, log
from pathlib import Path
from typing import Any, Generic, Self, TypeVar, cast

K = TypeVar("K", bound=Hashable)


@dataclass(frozen=True, slots=True)
class PreparedExample(Generic[K]):
    features: tuple[float, ...]
    label: K
    weight: float = 1.0
    mask: tuple[bool, ...] | None = None
    features_by_label: dict[K, tuple[float, ...]] | None = None

    @property
    def target(self) -> K:
        return self.label


class LinearModel(ABC):
    @classmethod
    def fit(cls, *args: Any, **kwargs: Any) -> Self:
        raise NotImplementedError

    def predict(self, *args: Any, **kwargs: Any) -> Any:
        raise NotImplementedError

    @abstractmethod
    def to_dict(self) -> dict[str, Any]:
        raise NotImplementedError

    @classmethod
    @abstractmethod
    def from_dict(cls, payload: dict[str, Any]) -> Self:
        raise NotImplementedError

    def save(self, path: str | Path) -> None:
        Path(path).write_text(model_json(self.to_dict()), encoding="utf-8")

    @classmethod
    def load(cls, path: str | Path) -> Self:
        return cls.from_dict(read_model_payload(path))


def prepared_softmax_example(
    *,
    label: K,
    features_by_label: Mapping[K, tuple[float, ...]],
    weight: float = 1.0,
    mask: tuple[bool, ...] | None = None,
) -> PreparedExample[K]:
    return PreparedExample(
        features=(),
        label=label,
        weight=weight,
        mask=mask,
        features_by_label=dict(features_by_label),
    )


def prepared_binary_example(
    *,
    label: bool,
    features: tuple[float, ...],
    weight: float = 1.0,
) -> PreparedExample[bool]:
    return PreparedExample(features=features, label=label, weight=weight)


def require_features_by_label(example: PreparedExample[K]) -> dict[K, tuple[float, ...]]:
    if example.features_by_label is None:
        raise ValueError("prepared example missing candidate features")
    return example.features_by_label


def model_payload(
    *,
    kind: str,
    feature_dim: int,
    epochs: int,
    learning_rate: float,
    l2: float,
    weights: object,
    feature_profile: str | None = None,
    positive_class_weight: float | None = None,
) -> dict[str, Any]:
    payload: dict[str, Any] = {"kind": kind}
    if feature_profile is not None:
        payload["feature_profile"] = feature_profile
    payload["feature_dim"] = feature_dim
    payload["epochs"] = epochs
    payload["learning_rate"] = learning_rate
    payload["l2"] = l2
    if positive_class_weight is not None:
        payload["positive_class_weight"] = positive_class_weight
    payload["weights"] = weights
    return payload


def model_json(payload: dict[str, Any]) -> str:
    return json.dumps(payload, indent=2, sort_keys=True) + "\n"


def read_model_payload(path: str | Path) -> dict[str, Any]:
    payload = json.loads(Path(path).read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError("model artifact must contain a JSON object")
    return cast(dict[str, Any], payload)


def parse_weight_row(row: Any) -> tuple[float, ...]:
    if not isinstance(row, list):
        raise ValueError("model weight rows must be lists")
    row = cast(list[Any], row)
    return tuple(float(value) for value in row)


def parse_weight_matrix(payload: object) -> tuple[tuple[float, ...], ...]:
    if not isinstance(payload, list):
        raise ValueError("model payload missing weights")
    payload = cast(list[Any], payload)
    return tuple(parse_weight_row(row) for row in payload)


def parse_weight_vector(payload: object) -> tuple[float, ...]:
    if not isinstance(payload, list):
        raise ValueError("model payload missing weights")
    payload = cast(list[Any], payload)
    return tuple(float(value) for value in payload)


def weight_matrix_payload(weights: Sequence[Sequence[float]]) -> list[list[float]]:
    return [list(row) for row in weights]


def fit_softmax_sgd(
    prepared_examples: Sequence[PreparedExample[K]],
    *,
    output_count: int,
    feature_dim: int,
    epochs: int,
    learning_rate: float,
    l2: float,
    index_of: Callable[[K], int],
    example_weight: Callable[[PreparedExample[K]], float] | None = None,
) -> tuple[tuple[float, ...], ...]:
    weights = [[0.0] * feature_dim for _ in range(output_count)]
    for _ in range(epochs):
        for example in prepared_examples:
            apply_softmax_sgd_update(
                weights,
                example,
                learning_rate=learning_rate,
                l2=l2,
                index_of=index_of,
                example_weight=1.0 if example_weight is None else example_weight(example),
            )
    return tuple(tuple(row) for row in weights)


def fit_logistic_sgd(
    prepared_examples: Sequence[PreparedExample[bool]],
    *,
    feature_dim: int,
    epochs: int,
    learning_rate: float,
    l2: float,
    example_weight: Callable[[PreparedExample[bool]], float] | None = None,
) -> tuple[float, ...]:
    weights = [0.0] * feature_dim
    for _ in range(epochs):
        for example in prepared_examples:
            apply_sgd_update(
                weights,
                example,
                learning_rate=learning_rate,
                l2=l2,
                example_weight=1.0 if example_weight is None else example_weight(example),
            )
    return tuple(weights)


def apply_sgd_update(
    theta: list[float],
    example: PreparedExample[bool],
    *,
    learning_rate: float,
    l2: float,
    example_weight: float = 1.0,
) -> None:
    probability = sigmoid(dot(theta, example.features))
    target = 1.0 if example.label else 0.0
    error = probability - target
    scaled_weight = example.weight * example_weight
    for index, value in enumerate(example.features):
        theta[index] -= learning_rate * (scaled_weight * error * value + l2 * theta[index])


def apply_softmax_sgd_update(
    weights: list[list[float]],
    example: PreparedExample[K],
    *,
    learning_rate: float,
    l2: float,
    index_of: Callable[[K], int],
    example_weight: float = 1.0,
) -> None:
    features_by_label = require_features_by_label(example)
    probabilities = softmax(logits_for_candidates(weights, features_by_label, index_of=index_of))
    scaled_weight = example.weight * example_weight
    for label, features in features_by_label.items():
        row = weights[index_of(label)]
        target = 1.0 if label == example.label else 0.0
        error = probabilities[label] - target
        scaled_error = learning_rate * scaled_weight * error
        if l2 == 0:
            for index, value in enumerate(features):
                if value:
                    row[index] -= scaled_error * value
        else:
            for index, value in enumerate(features):
                row[index] -= learning_rate * (scaled_weight * error * value + l2 * row[index])


def logits_for_candidates(
    weights: Sequence[Sequence[float]],
    features_by_label: Mapping[K, tuple[float, ...]],
    *,
    index_of: Callable[[K], int],
) -> dict[K, float]:
    return {
        label: dot(weights[index_of(label)], features)
        for label, features in features_by_label.items()
    }


def softmax(logits: Mapping[K, float]) -> dict[K, float]:
    max_logit = max(logits.values())
    exp_values = {label: exp(logit - max_logit) for label, logit in logits.items()}
    total = sum(exp_values.values())
    return {label: value / total for label, value in exp_values.items()}


def sigmoid(value: float) -> float:
    if value >= 0:
        inverse = exp(-value)
        return 1.0 / (1.0 + inverse)
    inverse = exp(value)
    return inverse / (1.0 + inverse)


def cross_entropy(probability: float, target: bool) -> float:
    p = clamped_probability(probability)
    y = 1.0 if target else 0.0
    return -(y * log(p) + (1.0 - y) * log(1.0 - p))


def dot(weights: Sequence[float], features: Sequence[float]) -> float:
    total = 0.0
    for weight, feature in zip(weights, features, strict=True):
        if feature:
            total += weight * feature
    return total


def clamped_probability(value: float) -> float:
    return min(1.0 - 1e-15, max(1e-15, float(value)))


def balanced_accuracy(*, tp: int, tn: int, fp: int, fn: int) -> float | None:
    positives = tp + fn
    negatives = tn + fp
    if positives == 0 or negatives == 0:
        return None
    return ((tp / positives) + (tn / negatives)) / 2.0


def numeric_summary(
    values: Iterable[float],
    *,
    empty_with_none: bool = False,
    include_max_abs: bool = False,
) -> dict[str, float | int | None]:
    value_list = list(values)
    if not value_list:
        if empty_with_none:
            payload: dict[str, float | int | None] = {
                "count": 0,
                "mean": None,
                "min": None,
                "max": None,
                "mean_abs": None,
            }
        else:
            payload = {
                "count": 0,
                "min": 0.0,
                "max": 0.0,
                "mean": 0.0,
                "mean_abs": 0.0,
            }
        if include_max_abs:
            payload["max_abs"] = None if empty_with_none else 0.0
        return payload

    payload = {
        "count": len(value_list),
        "min": min(value_list),
        "max": max(value_list),
        "mean": sum(value_list) / len(value_list),
        "mean_abs": sum(abs(value) for value in value_list) / len(value_list),
    }
    if include_max_abs:
        payload["max_abs"] = max(abs(value) for value in value_list)
    return payload


@dataclass(frozen=True, slots=True)
class FeatureColumnStats:
    sums: tuple[float, ...]
    sum_abs: tuple[float, ...]
    max_abs: tuple[float, ...]
    nonzero: tuple[int, ...]
    vector_count: int


def collect_feature_column_stats(
    vectors: Iterable[Sequence[float]],
    *,
    feature_count: int,
) -> FeatureColumnStats:
    sums = [0.0] * feature_count
    sum_abs = [0.0] * feature_count
    max_abs = [0.0] * feature_count
    nonzero = [0] * feature_count
    vector_count = 0
    for features in vectors:
        vector_count += 1
        for index, value in enumerate(features):
            sums[index] += value
            abs_value = abs(value)
            sum_abs[index] += abs_value
            max_abs[index] = max(max_abs[index], abs_value)
            if value != 0:
                nonzero[index] += 1
    return FeatureColumnStats(
        sums=tuple(sums),
        sum_abs=tuple(sum_abs),
        max_abs=tuple(max_abs),
        nonzero=tuple(nonzero),
        vector_count=vector_count,
    )


def feature_summary_payload(
    *,
    example_count: int,
    feature_names: Sequence[str],
    vectors: Iterable[Sequence[float]],
    candidate_vectors_key: str | None = None,
    include_overall: bool = False,
) -> dict[str, Any]:
    feature_count = len(feature_names)
    stats = collect_feature_column_stats(vectors, feature_count=feature_count)
    payload: dict[str, Any] = {"examples": example_count}
    if candidate_vectors_key is not None:
        payload[candidate_vectors_key] = stats.vector_count
    payload["feature_count"] = feature_count
    payload["features"] = [
        {
            "index": index,
            "name": name,
            "nonzero": stats.nonzero[index],
            "nonzero_rate": (
                None if stats.vector_count == 0 else stats.nonzero[index] / stats.vector_count
            ),
            "mean": None if stats.vector_count == 0 else stats.sums[index] / stats.vector_count,
            "mean_abs": (
                None if stats.vector_count == 0 else stats.sum_abs[index] / stats.vector_count
            ),
            "max_abs": None if stats.vector_count == 0 else stats.max_abs[index],
        }
        for index, name in enumerate(feature_names)
    ]
    if include_overall:
        payload["overall"] = feature_overall_summary(stats, feature_count=feature_count)
    return payload


def feature_overall_summary(
    stats: FeatureColumnStats,
    *,
    feature_count: int,
) -> dict[str, float | int | None]:
    value_count = stats.vector_count * feature_count
    if value_count == 0:
        return {
            "count": 0,
            "nonzero": 0,
            "nonzero_rate": None,
            "mean": None,
            "mean_abs": None,
            "max_abs": None,
        }
    return {
        "count": value_count,
        "nonzero": sum(stats.nonzero),
        "nonzero_rate": sum(stats.nonzero) / value_count,
        "mean": sum(stats.sums) / value_count,
        "mean_abs": sum(stats.sum_abs) / value_count,
        "max_abs": max(stats.max_abs, default=0.0),
    }
