"""Versioned decision results and structured rationale payloads."""

from __future__ import annotations

import json
import math
import re
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from typing import Any, Self

from kenjaku.schema.action_v1 import ActionV1

DECISION_RESULT_V1_KIND = "kenjaku-decision-result-v1"
DECISION_RESULT_V1_FIELDS = (
    "kind",
    "decision_id",
    "ruleset",
    "model_id",
    "selected_action",
    "probability",
    "rationale",
)
DECISION_RATIONALE_V1_FIELDS = ("factors",)
DECISION_FACTOR_V1_FIELDS = ("factor", "value", "contribution", "evidence")
_FACTOR_NAME = re.compile(r"[a-z][a-z0-9_]{0,63}")


@dataclass(frozen=True, slots=True)
class DecisionFactorV1:
    """One machine-readable rationale factor with signed action contribution."""

    factor: str
    value: float
    contribution: float
    evidence: tuple[str, ...] = ()

    def __post_init__(self) -> None:
        if not isinstance(self.factor, str) or _FACTOR_NAME.fullmatch(self.factor) is None:
            raise ValueError("factor must be a lowercase snake_case identifier")
        _validate_finite_number(self.value, "value")
        _validate_finite_number(self.contribution, "contribution")
        if not isinstance(self.evidence, tuple):
            raise ValueError("evidence must be an immutable tuple")
        if any(not isinstance(item, str) or not item for item in self.evidence):
            raise ValueError("evidence entries must be non-empty strings")

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> Self:
        _require_exact_fields(payload, DECISION_FACTOR_V1_FIELDS, "DecisionFactorV1")
        return cls(
            factor=_read_str(payload, "factor"),
            value=_read_number(payload, "value"),
            contribution=_read_number(payload, "contribution"),
            evidence=_read_string_tuple(payload, "evidence"),
        )

    def to_dict(self) -> dict[str, Any]:
        return {
            "factor": self.factor,
            "value": self.value,
            "contribution": self.contribution,
            "evidence": list(self.evidence),
        }


@dataclass(frozen=True, slots=True)
class DecisionRationaleV1:
    """Ordered structured evidence for one selected action."""

    factors: tuple[DecisionFactorV1, ...] = ()

    def __post_init__(self) -> None:
        if not isinstance(self.factors, tuple):
            raise ValueError("factors must be an immutable tuple")
        if any(not isinstance(factor, DecisionFactorV1) for factor in self.factors):
            raise ValueError("factors must contain DecisionFactorV1 values")
        names = tuple(factor.factor for factor in self.factors)
        if len(set(names)) != len(names):
            raise ValueError("rationale factor names must be unique")

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> Self:
        _require_exact_fields(payload, DECISION_RATIONALE_V1_FIELDS, "DecisionRationaleV1")
        values = payload["factors"]
        if isinstance(values, (str, bytes)) or not isinstance(values, Sequence):
            raise ValueError("factors must be an array")
        factors: list[DecisionFactorV1] = []
        for value in values:
            if not isinstance(value, Mapping):
                raise ValueError("factors entries must be objects")
            factors.append(DecisionFactorV1.from_dict(value))
        return cls(factors=tuple(factors))

    def to_dict(self) -> dict[str, Any]:
        return {"factors": [factor.to_dict() for factor in self.factors]}


@dataclass(frozen=True, slots=True)
class DecisionResultV1:
    """One versioned policy decision with a selected action and rationale."""

    decision_id: str
    ruleset: str
    model_id: str
    selected_action: ActionV1
    probability: float | None
    rationale: DecisionRationaleV1

    def __post_init__(self) -> None:
        _validate_non_empty_str(self.decision_id, "decision_id")
        _validate_non_empty_str(self.model_id, "model_id")
        if not isinstance(self.selected_action, ActionV1):
            raise ValueError("selected_action must be an ActionV1")
        if self.ruleset != self.selected_action.ruleset:
            raise ValueError("result ruleset must match selected_action ruleset")
        if self.probability is not None:
            _validate_finite_number(self.probability, "probability")
            if not 0.0 <= self.probability <= 1.0:
                raise ValueError("probability must be within [0, 1]")
        if not isinstance(self.rationale, DecisionRationaleV1):
            raise ValueError("rationale must be a DecisionRationaleV1")

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> Self:
        _require_exact_fields(payload, DECISION_RESULT_V1_FIELDS, "DecisionResultV1")
        if _read_str(payload, "kind") != DECISION_RESULT_V1_KIND:
            raise ValueError(f"DecisionResultV1 kind must be {DECISION_RESULT_V1_KIND}")
        selected_action = payload["selected_action"]
        rationale = payload["rationale"]
        if not isinstance(selected_action, Mapping):
            raise ValueError("selected_action must be an object")
        if not isinstance(rationale, Mapping):
            raise ValueError("rationale must be an object")
        return cls(
            decision_id=_read_str(payload, "decision_id"),
            ruleset=_read_str(payload, "ruleset"),
            model_id=_read_str(payload, "model_id"),
            selected_action=ActionV1.from_dict(selected_action),
            probability=_read_optional_number(payload, "probability"),
            rationale=DecisionRationaleV1.from_dict(rationale),
        )

    def to_dict(self) -> dict[str, Any]:
        return {
            "kind": DECISION_RESULT_V1_KIND,
            "decision_id": self.decision_id,
            "ruleset": self.ruleset,
            "model_id": self.model_id,
            "selected_action": self.selected_action.to_dict(),
            "probability": self.probability,
            "rationale": self.rationale.to_dict(),
        }

    def to_json(self, *, indent: int | None = None) -> str:
        return json.dumps(self.to_dict(), indent=indent, sort_keys=True)


def _require_exact_fields(payload: Mapping[str, Any], fields: Sequence[str], name: str) -> None:
    actual = set(payload)
    expected = set(fields)
    if actual == expected:
        return
    missing = sorted(expected - actual)
    unexpected = sorted(actual - expected)
    details = []
    if missing:
        details.append("missing=" + ",".join(missing))
    if unexpected:
        details.append("unexpected=" + ",".join(unexpected))
    raise ValueError(f"{name} fields must match v1 schema: {'; '.join(details)}")


def _read_str(payload: Mapping[str, Any], field: str) -> str:
    value = payload[field]
    if not isinstance(value, str):
        raise ValueError(f"{field} must be a string")
    return value


def _read_number(payload: Mapping[str, Any], field: str) -> float:
    value = payload[field]
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise ValueError(f"{field} must be a number")
    return float(value)


def _read_optional_number(payload: Mapping[str, Any], field: str) -> float | None:
    value = payload[field]
    if value is None:
        return None
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise ValueError(f"{field} must be a number or null")
    return float(value)


def _read_string_tuple(payload: Mapping[str, Any], field: str) -> tuple[str, ...]:
    values = payload[field]
    if isinstance(values, (str, bytes)) or not isinstance(values, Sequence):
        raise ValueError(f"{field} must be an array")
    if any(not isinstance(value, str) for value in values):
        raise ValueError(f"{field} entries must be strings")
    return tuple(values)


def _validate_non_empty_str(value: Any, field: str) -> None:
    if not isinstance(value, str) or not value:
        raise ValueError(f"{field} must be a non-empty string")


def _validate_finite_number(value: Any, field: str) -> None:
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise ValueError(f"{field} must be a number")
    if not math.isfinite(value):
        raise ValueError(f"{field} must be finite")
