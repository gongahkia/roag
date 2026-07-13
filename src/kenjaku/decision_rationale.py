"""Lossless extraction of heuristic factors into DecisionResultV1 rationale payloads."""

from __future__ import annotations

from collections.abc import Mapping, Sequence

from kenjaku.heuristics import (
    HeuristicActionCandidate,
    HeuristicCallCandidate,
    HeuristicDiscardCandidate,
    HeuristicFactor,
)
from kenjaku.schema import DecisionFactorV1, DecisionRationaleV1

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
