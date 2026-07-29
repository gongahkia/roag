"""Deterministic multiclass calibration by decision family."""

from __future__ import annotations

from collections import defaultdict
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from math import isfinite, log

CALIBRATION_DECISION_FAMILIES = ("discard", "call_pass", "special_action")
CALIBRATION_TEMPERATURES = tuple(round(0.25 + step * 0.05, 2) for step in range(76))


@dataclass(frozen=True, slots=True)
class CalibrationExample:
    """One probability distribution and its observed class index."""

    family: str
    probabilities: tuple[float, ...]
    target: int

    def __post_init__(self) -> None:
        if self.family not in CALIBRATION_DECISION_FAMILIES:
            raise ValueError("unsupported calibration family: " + self.family)
        if len(self.probabilities) < 2:
            raise ValueError("calibration probabilities need at least two classes")
        if not all(
            isinstance(value, float) and isfinite(value) and value >= 0
            for value in self.probabilities
        ):
            raise ValueError("calibration probabilities must be finite non-negative floats")
        if abs(sum(self.probabilities) - 1.0) > 1e-9:
            raise ValueError("calibration probabilities must sum to one")
        if not isinstance(self.target, int) or isinstance(self.target, bool):
            raise ValueError("calibration target must be an integer")
        if not 0 <= self.target < len(self.probabilities):
            raise ValueError("calibration target outside probability range")


def fit_decision_family_temperatures(
    examples: Sequence[CalibrationExample],
    *,
    temperatures: Sequence[float] = CALIBRATION_TEMPERATURES,
) -> dict[str, float]:
    """Select minimum-NLL temperature independently for each observed family."""
    candidates = _temperatures(temperatures)
    grouped = _grouped(examples)
    if not grouped:
        raise ValueError("cannot fit calibration with zero examples")
    fitted: dict[str, float] = {}
    for family, rows in grouped.items():
        fitted[family] = min(
            candidates,
            key=lambda temperature: (_nll(rows, temperature), temperature),
        )
    return fitted


def evaluate_decision_family_calibration(
    examples: Sequence[CalibrationExample],
    *,
    temperatures: Mapping[str, float] | None = None,
    bins: int = 10,
) -> dict[str, dict[str, int | float]]:
    """Return per-family NLL, Brier, accuracy, and top-label ECE."""
    if bins <= 0:
        raise ValueError("bins must be positive")
    resolved = _resolved_temperatures(temperatures)
    reports: dict[str, dict[str, int | float]] = {}
    for family, rows in _grouped(examples).items():
        temperature = resolved.get(family, 1.0)
        probabilities = [
            apply_temperature(row.probabilities, temperature=temperature) for row in rows
        ]
        nll = sum(
            -log(max(values[row.target], 1e-15))
            for row, values in zip(rows, probabilities, strict=True)
        ) / len(rows)
        brier = sum(
            sum(
                (value - (1.0 if index == row.target else 0.0)) ** 2
                for index, value in enumerate(values)
            )
            for row, values in zip(rows, probabilities, strict=True)
        ) / len(rows)
        accuracy = sum(
            max(range(len(values)), key=values.__getitem__) == row.target
            for row, values in zip(rows, probabilities, strict=True)
        ) / len(rows)
        reports[family] = {
            "examples": len(rows),
            "temperature": temperature,
            "nll": nll,
            "brier_score": brier,
            "accuracy": accuracy,
            "ece": _ece(rows, probabilities, bins=bins),
        }
    return reports


def apply_temperature(probabilities: Sequence[float], *, temperature: float) -> tuple[float, ...]:
    """Temperature-scale one normalized probability distribution."""
    if (
        not isinstance(temperature, (int, float))
        or isinstance(temperature, bool)
        or temperature <= 0
    ):
        raise ValueError("temperature must be positive")
    values = tuple(float(value) for value in probabilities)
    if not values or any(not isfinite(value) or value < 0 for value in values):
        raise ValueError("probabilities must be finite non-negative values")
    total = sum(values)
    if abs(total - 1.0) > 1e-9:
        raise ValueError("probabilities must sum to one")
    powers = tuple(max(value, 1e-15) ** (1.0 / float(temperature)) for value in values)
    normalizer = sum(powers)
    return tuple(value / normalizer for value in powers)


def _temperatures(values: Sequence[float]) -> tuple[float, ...]:
    if not values:
        raise ValueError("temperatures cannot be empty")
    resolved = tuple(float(value) for value in values)
    if any(not isfinite(value) or value <= 0 for value in resolved):
        raise ValueError("temperatures must be positive finite numbers")
    return tuple(sorted(set(resolved)))


def _grouped(examples: Sequence[CalibrationExample]) -> dict[str, list[CalibrationExample]]:
    grouped: dict[str, list[CalibrationExample]] = defaultdict(list)
    for example in examples:
        if not isinstance(example, CalibrationExample):
            raise ValueError("calibration examples must be CalibrationExample values")
        grouped[example.family].append(example)
    return dict(grouped)


def _resolved_temperatures(values: Mapping[str, float] | None) -> dict[str, float]:
    if values is None:
        return {}
    resolved: dict[str, float] = {}
    for family, temperature in values.items():
        if family not in CALIBRATION_DECISION_FAMILIES:
            raise ValueError("unsupported calibration family: " + family)
        resolved[family] = _temperatures((temperature,))[0]
    return resolved


def _nll(rows: Sequence[CalibrationExample], temperature: float) -> float:
    return sum(
        -log(max(apply_temperature(row.probabilities, temperature=temperature)[row.target], 1e-15))
        for row in rows
    ) / len(rows)


def _ece(
    rows: Sequence[CalibrationExample],
    probabilities: Sequence[tuple[float, ...]],
    *,
    bins: int,
) -> float:
    bucket_count = [0] * bins
    confidence_sum = [0.0] * bins
    correct_sum = [0.0] * bins
    for row, values in zip(rows, probabilities, strict=True):
        predicted = max(range(len(values)), key=values.__getitem__)
        confidence = values[predicted]
        bucket = min(int(confidence * bins), bins - 1)
        bucket_count[bucket] += 1
        confidence_sum[bucket] += confidence
        correct_sum[bucket] += float(predicted == row.target)
    return sum(
        count / len(rows) * abs(confidence_sum[index] / count - correct_sum[index] / count)
        for index, count in enumerate(bucket_count)
        if count
    )
