"""Versioned top-alternative and counterfactual decision payloads."""

from __future__ import annotations

import json
import math
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from typing import Any, Self

from kenjaku.schema.action_v1 import ActionV1
from kenjaku.schema.decision_result_v1 import DecisionRationaleV1, DecisionResultV1

DECISION_COUNTERFACTUALS_V1_KIND = "kenjaku-decision-counterfactuals-v1"
DECISION_COUNTERFACTUALS_V1_FIELDS = (
    "kind",
    "decision",
    "selected_score",
    "top_alternatives",
)
DECISION_COUNTERFACTUAL_V1_FIELDS = ("action", "score", "score_delta", "rationale")


@dataclass(frozen=True, slots=True)
class DecisionCounterfactualV1:
    """One scored alternative action with its rationale and delta from the selected action."""

    action: ActionV1
    score: float
    score_delta: float
    rationale: DecisionRationaleV1

    def __post_init__(self) -> None:
        if not isinstance(self.action, ActionV1):
            raise ValueError("action must be an ActionV1")
        _validate_finite_number(self.score, "score")
        _validate_finite_number(self.score_delta, "score_delta")
        if not isinstance(self.rationale, DecisionRationaleV1):
            raise ValueError("rationale must be a DecisionRationaleV1")

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> Self:
        _require_exact_fields(
            payload,
            DECISION_COUNTERFACTUAL_V1_FIELDS,
            "DecisionCounterfactualV1",
        )
        action = payload["action"]
        rationale = payload["rationale"]
        if not isinstance(action, Mapping):
            raise ValueError("action must be an object")
        if not isinstance(rationale, Mapping):
            raise ValueError("rationale must be an object")
        return cls(
            action=ActionV1.from_dict(action),
            score=_read_number(payload, "score"),
            score_delta=_read_number(payload, "score_delta"),
            rationale=DecisionRationaleV1.from_dict(rationale),
        )

    def to_dict(self) -> dict[str, Any]:
        return {
            "action": self.action.to_dict(),
            "score": self.score,
            "score_delta": self.score_delta,
            "rationale": self.rationale.to_dict(),
        }


@dataclass(frozen=True, slots=True)
class DecisionCounterfactualsV1:
    """One selected decision and its deterministically ordered counterfactual alternatives."""

    decision: DecisionResultV1
    selected_score: float
    top_alternatives: tuple[DecisionCounterfactualV1, ...]

    def __post_init__(self) -> None:
        if not isinstance(self.decision, DecisionResultV1):
            raise ValueError("decision must be a DecisionResultV1")
        _validate_finite_number(self.selected_score, "selected_score")
        if not isinstance(self.top_alternatives, tuple):
            raise ValueError("top_alternatives must be an immutable tuple")
        if any(
            not isinstance(alternative, DecisionCounterfactualV1)
            for alternative in self.top_alternatives
        ):
            raise ValueError("top_alternatives must contain DecisionCounterfactualV1 values")
        actions = tuple(alternative.action for alternative in self.top_alternatives)
        if self.decision.selected_action in actions:
            raise ValueError("top_alternatives cannot repeat the selected action")
        if len(set(actions)) != len(actions):
            raise ValueError("top_alternatives actions must be unique")
        for alternative in self.top_alternatives:
            if alternative.action.ruleset != self.decision.ruleset:
                raise ValueError("alternative action ruleset must match decision ruleset")
            expected_delta = alternative.score - self.selected_score
            if not math.isclose(
                alternative.score_delta,
                expected_delta,
                rel_tol=1e-12,
                abs_tol=1e-12,
            ):
                raise ValueError("score_delta must equal score minus selected_score")
        expected_order = tuple(sorted(self.top_alternatives, key=_alternative_sort_key))
        if self.top_alternatives != expected_order:
            raise ValueError("top_alternatives must be ordered by score and canonical action")

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> Self:
        _require_exact_fields(
            payload,
            DECISION_COUNTERFACTUALS_V1_FIELDS,
            "DecisionCounterfactualsV1",
        )
        if _read_str(payload, "kind") != DECISION_COUNTERFACTUALS_V1_KIND:
            raise ValueError(
                "DecisionCounterfactualsV1 kind must be " + DECISION_COUNTERFACTUALS_V1_KIND
            )
        decision = payload["decision"]
        alternatives = payload["top_alternatives"]
        if not isinstance(decision, Mapping):
            raise ValueError("decision must be an object")
        if isinstance(alternatives, (str, bytes)) or not isinstance(alternatives, Sequence):
            raise ValueError("top_alternatives must be an array")
        parsed_alternatives: list[DecisionCounterfactualV1] = []
        for alternative in alternatives:
            if not isinstance(alternative, Mapping):
                raise ValueError("top_alternatives entries must be objects")
            parsed_alternatives.append(DecisionCounterfactualV1.from_dict(alternative))
        return cls(
            decision=DecisionResultV1.from_dict(decision),
            selected_score=_read_number(payload, "selected_score"),
            top_alternatives=tuple(parsed_alternatives),
        )

    def to_dict(self) -> dict[str, Any]:
        return {
            "kind": DECISION_COUNTERFACTUALS_V1_KIND,
            "decision": self.decision.to_dict(),
            "selected_score": self.selected_score,
            "top_alternatives": [alternative.to_dict() for alternative in self.top_alternatives],
        }

    def to_json(self, *, indent: int | None = None) -> str:
        return json.dumps(self.to_dict(), indent=indent, sort_keys=True)


def _alternative_sort_key(alternative: DecisionCounterfactualV1) -> tuple[Any, ...]:
    action = alternative.action
    return (
        -alternative.score,
        action.action,
        "" if action.tile is None else action.tile,
        action.tsumogiri,
        action.consumed,
    )


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
    _validate_finite_number(value, field)
    return float(value)


def _validate_finite_number(value: Any, field: str) -> None:
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise ValueError(f"{field} must be a number")
    if not math.isfinite(value):
        raise ValueError(f"{field} must be finite")
