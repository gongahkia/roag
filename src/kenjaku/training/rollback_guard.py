"""Fail-closed rollback decisions for conservative RL fine-tuning."""

from __future__ import annotations

from collections.abc import Mapping
from typing import Any

RL_ROLLBACK_GUARD_KIND = "kenjaku-rl-rollback-guard-v0"


def assess_rl_rollback_guard(
    *,
    baseline_checkpoint: str,
    candidate_checkpoint: str,
    baseline_conformance: Mapping[str, Any],
    candidate_conformance: Mapping[str, Any],
    promotion_gate: Mapping[str, Any],
) -> dict[str, Any]:
    """Require rollback if matched conformance or promotion evidence regresses."""
    if not isinstance(baseline_checkpoint, str) or not baseline_checkpoint:
        raise ValueError("baseline_checkpoint must be a non-empty string")
    if not isinstance(candidate_checkpoint, str) or not candidate_checkpoint:
        raise ValueError("candidate_checkpoint must be a non-empty string")
    baseline = _conformance_summary(baseline_conformance, name="baseline_conformance")
    candidate = _conformance_summary(candidate_conformance, name="candidate_conformance")
    if baseline["ruleset"] != candidate["ruleset"]:
        raise ValueError("conformance rulesets must match")
    if baseline["cases"] != candidate["cases"]:
        raise ValueError("conformance case counts must match")
    promotion_allowed = promotion_gate.get("allowed")
    if type(promotion_allowed) is not bool:
        raise ValueError("promotion_gate allowed must be a boolean")
    reasons: list[str] = []
    if candidate["passed"] < baseline["passed"]:
        reasons.append("rule_conformance_regressed")
    if not promotion_allowed:
        reasons.append("promotion_gate_rejected")
    rollback_required = bool(reasons)
    return {
        "kind": RL_ROLLBACK_GUARD_KIND,
        "ruleset": baseline["ruleset"],
        "baseline_checkpoint": baseline_checkpoint,
        "candidate_checkpoint": candidate_checkpoint,
        "baseline_conformance": baseline,
        "candidate_conformance": candidate,
        "promotion_gate": {
            "allowed": promotion_allowed,
            "reason": promotion_gate.get("reason"),
        },
        "rollback_required": rollback_required,
        "action": "rollback_to_baseline" if rollback_required else "keep_candidate",
        "reasons": reasons,
    }


def _conformance_summary(payload: Mapping[str, Any], *, name: str) -> dict[str, Any]:
    ruleset = payload.get("ruleset")
    cases = payload.get("cases")
    passed = payload.get("passed")
    if ruleset not in ("tenhou-3p", "tenhou-4p"):
        raise ValueError(f"{name} ruleset must be tenhou-3p or tenhou-4p")
    if (
        type(cases) is not int
        or type(passed) is not int
        or cases <= 0
        or passed < 0
        or passed > cases
    ):
        raise ValueError(f"{name} requires valid cases and passed counts")
    return {"ruleset": ruleset, "cases": cases, "passed": passed, "failed": cases - passed}
