"""Lossless extraction of heuristic factors into DecisionResultV1 rationale payloads."""

from __future__ import annotations

import math
from collections.abc import Iterable, Mapping, Sequence
from typing import Any

from kenjaku.heuristics import (
    HeuristicActionCandidate,
    HeuristicCallCandidate,
    HeuristicDiscardCandidate,
    HeuristicFactor,
)
from kenjaku.schema import (
    ActionV1,
    DecisionCounterfactualsV1,
    DecisionCounterfactualV1,
    DecisionFactorV1,
    DecisionRationaleV1,
    DecisionResultV1,
)

HeuristicRationaleCandidate = (
    HeuristicDiscardCandidate | HeuristicCallCandidate | HeuristicActionCandidate
)


def extract_heuristic_rationale(
    candidate: HeuristicRationaleCandidate,
    *,
    evidence_by_factor: Mapping[str, Sequence[str]] | None = None,
) -> DecisionRationaleV1:
    """Convert one heuristic candidate's ordered factors into a versioned rationale."""
    if not isinstance(
        candidate,
        (HeuristicDiscardCandidate, HeuristicCallCandidate, HeuristicActionCandidate),
    ):
        raise ValueError("candidate must be a heuristic candidate")
    evidence = {} if evidence_by_factor is None else evidence_by_factor
    if not isinstance(evidence, Mapping):
        raise ValueError("evidence_by_factor must be a mapping")
    if any(not isinstance(name, str) for name in evidence):
        raise ValueError("evidence_by_factor keys must be strings")
    if any(not isinstance(factor, HeuristicFactor) for factor in candidate.factors):
        raise ValueError("heuristic candidate factors must be HeuristicFactor values")
    factor_names = {factor.name for factor in candidate.factors}
    unexpected = sorted(set(evidence) - factor_names)
    if unexpected:
        raise ValueError("evidence_by_factor contains unknown factors: " + ",".join(unexpected))
    factors = tuple(
        _decision_factor(factor, evidence.get(factor.name, ())) for factor in candidate.factors
    )
    return DecisionRationaleV1(factors=factors)


def render_decision_rationale(rationale: DecisionRationaleV1) -> str:
    """Render one structured rationale as deterministic human-readable sentences."""
    if not isinstance(rationale, DecisionRationaleV1):
        raise ValueError("rationale must be a DecisionRationaleV1")
    if not rationale.factors:
        return "No structured rationale factors are available."
    return " ".join(_render_factor(factor) for factor in rationale.factors)


def build_decision_counterfactuals(
    decision: DecisionResultV1,
    *,
    selected_score: float,
    alternatives: Iterable[tuple[ActionV1, float, DecisionRationaleV1]],
    limit: int = 3,
) -> DecisionCounterfactualsV1:
    """Build a deterministically sorted top-alternative payload for one selected decision."""
    if not isinstance(decision, DecisionResultV1):
        raise ValueError("decision must be a DecisionResultV1")
    _validate_finite_number(selected_score, "selected_score")
    if not isinstance(limit, int) or isinstance(limit, bool) or limit < 0:
        raise ValueError("limit must be a non-negative integer")
    candidates: list[tuple[ActionV1, float, DecisionRationaleV1]] = []
    actions: set[ActionV1] = set()
    for candidate in alternatives:
        if isinstance(candidate, (str, bytes)) or not isinstance(candidate, Sequence):
            raise ValueError("alternatives entries must contain action, score, and rationale")
        if len(candidate) != 3:
            raise ValueError("alternatives entries must contain action, score, and rationale")
        action, score, rationale = candidate
        if not isinstance(action, ActionV1):
            raise ValueError("alternative action must be an ActionV1")
        if action.ruleset != decision.ruleset:
            raise ValueError("alternative action ruleset must match decision ruleset")
        if action == decision.selected_action:
            raise ValueError("alternatives cannot repeat the selected action")
        if action in actions:
            raise ValueError("alternative actions must be unique")
        _validate_finite_number(score, "alternative score")
        if not isinstance(rationale, DecisionRationaleV1):
            raise ValueError("alternative rationale must be a DecisionRationaleV1")
        actions.add(action)
        candidates.append((action, float(score), rationale))
    candidates.sort(key=lambda candidate: (-candidate[1], _action_sort_key(candidate[0])))
    counterfactuals = tuple(
        DecisionCounterfactualV1(
            action=action,
            score=score,
            score_delta=score - selected_score,
            rationale=rationale,
        )
        for action, score, rationale in candidates[:limit]
    )
    return DecisionCounterfactualsV1(
        decision=decision,
        selected_score=selected_score,
        top_alternatives=counterfactuals,
    )


def _decision_factor(factor: HeuristicFactor, evidence: Sequence[str]) -> DecisionFactorV1:
    if not isinstance(factor, HeuristicFactor):
        raise ValueError("heuristic candidate factors must be HeuristicFactor values")
    if isinstance(evidence, (str, bytes)) or not isinstance(evidence, Sequence):
        raise ValueError("factor evidence must be a string sequence")
    return DecisionFactorV1(
        factor=factor.name,
        value=factor.value,
        contribution=factor.contribution,
        evidence=tuple(evidence),
    )


def _render_factor(factor: DecisionFactorV1) -> str:
    name = factor.factor.replace("_", " ")
    contribution = _number_text(abs(factor.contribution))
    value = _number_text(factor.value)
    if factor.contribution > 0:
        sentence = f"{name} supported this action (+{contribution}; value {value})."
    elif factor.contribution < 0:
        sentence = f"{name} opposed this action (-{contribution}; value {value})."
    else:
        sentence = f"{name} was neutral (0; value {value})."
    if not factor.evidence:
        return sentence
    return sentence + " Evidence: " + "; ".join(factor.evidence) + "."


def _number_text(value: float) -> str:
    return format(value, ".6g")


def _action_sort_key(action: ActionV1) -> tuple[Any, ...]:
    return (
        action.action,
        "" if action.tile is None else action.tile,
        action.tsumogiri,
        action.consumed,
    )


def _validate_finite_number(value: Any, field: str) -> None:
    if isinstance(value, bool) or not isinstance(value, (int, float)):
        raise ValueError(f"{field} must be a number")
    if not math.isfinite(value):
        raise ValueError(f"{field} must be finite")
