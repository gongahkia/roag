"""Frozen internal evaluation plans for both supported rulesets."""

from __future__ import annotations

import json
from hashlib import blake2b
from pathlib import Path
from typing import Any

from kenjaku.reproducibility import derive_seed
from kenjaku.simulation.paired_matches import (
    DEFAULT_PAIRED_MATCH_BOOTSTRAP_RESAMPLES,
    fixed_heuristic_baseline_manifest,
)

INTERNAL_EVALUATION_MANIFEST_KIND = "kenjaku-internal-evaluation-manifest-v0"
INTERNAL_EVALUATION_MANIFEST_RULESETS = ("tenhou-4p", "tenhou-3p")


def build_frozen_internal_evaluation_manifest(
    *,
    checkpoint_id: str,
    seed: str,
    pairs: int,
    max_rounds: int,
    max_turns_per_round: int,
    bootstrap_resamples: int = DEFAULT_PAIRED_MATCH_BOOTSTRAP_RESAMPLES,
) -> dict[str, Any]:
    """Build one fingerprinted paired-evaluation plan for 4p and Sanma."""
    if not isinstance(checkpoint_id, str) or not checkpoint_id:
        raise ValueError("checkpoint_id must be a non-empty string")
    if not isinstance(seed, str) or not seed:
        raise ValueError("seed must be a non-empty string")
    for name, value in (
        ("pairs", pairs),
        ("max_rounds", max_rounds),
        ("max_turns_per_round", max_turns_per_round),
        ("bootstrap_resamples", bootstrap_resamples),
    ):
        if type(value) is not int or value <= 0:
            raise ValueError(f"{name} must be positive")
    payload = {
        "kind": INTERNAL_EVALUATION_MANIFEST_KIND,
        "checkpoint_id": checkpoint_id,
        "seed": seed,
        "seed_provenance": {"derivation": "kenjaku-seed-v1-blake2b"},
        "rulesets": [
            _ruleset_plan(
                ruleset,
                seed=seed,
                pairs=pairs,
                max_rounds=max_rounds,
                max_turns_per_round=max_turns_per_round,
                bootstrap_resamples=bootstrap_resamples,
            )
            for ruleset in INTERNAL_EVALUATION_MANIFEST_RULESETS
        ],
    }
    return {**payload, "fingerprint": _fingerprint(payload)}


def validate_frozen_internal_evaluation_manifest(payload: dict[str, Any]) -> None:
    """Validate the exact frozen plan shape and its content fingerprint."""
    if set(payload) != {
        "kind",
        "checkpoint_id",
        "seed",
        "seed_provenance",
        "rulesets",
        "fingerprint",
    }:
        raise ValueError("internal evaluation manifest fields are invalid")
    if payload.get("kind") != INTERNAL_EVALUATION_MANIFEST_KIND:
        raise ValueError(f"manifest kind must be {INTERNAL_EVALUATION_MANIFEST_KIND}")
    if not isinstance(payload.get("checkpoint_id"), str) or not payload["checkpoint_id"]:
        raise ValueError("checkpoint_id must be a non-empty string")
    if not isinstance(payload.get("seed"), str) or not payload["seed"]:
        raise ValueError("seed must be a non-empty string")
    if payload.get("seed_provenance") != {"derivation": "kenjaku-seed-v1-blake2b"}:
        raise ValueError("seed_provenance is invalid")
    plans = payload.get("rulesets")
    if not isinstance(plans, list) or len(plans) != len(INTERNAL_EVALUATION_MANIFEST_RULESETS):
        raise ValueError("rulesets must contain 4p and 3p plans")
    if tuple(plan.get("ruleset") for plan in plans if isinstance(plan, dict)) != (
        INTERNAL_EVALUATION_MANIFEST_RULESETS
    ):
        raise ValueError("rulesets must be ordered tenhou-4p then tenhou-3p")
    for plan in plans:
        if not isinstance(plan, dict):
            raise ValueError("ruleset plan must be an object")
        _validate_ruleset_plan(plan)
    fingerprint = payload.get("fingerprint")
    frozen_payload = {key: value for key, value in payload.items() if key != "fingerprint"}
    if fingerprint != _fingerprint(frozen_payload):
        raise ValueError("internal evaluation manifest fingerprint mismatch")


def write_frozen_internal_evaluation_manifest(path: str | Path, payload: dict[str, Any]) -> None:
    """Write a verified plan once; reject replacements with different content."""
    validate_frozen_internal_evaluation_manifest(payload)
    destination = Path(path)
    encoded = json.dumps(payload, indent=2, sort_keys=True) + "\n"
    if destination.exists():
        if destination.read_text(encoding="utf-8") != encoded:
            raise ValueError("refusing to replace frozen internal evaluation manifest")
        return
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(encoded, encoding="utf-8")


def _ruleset_plan(
    ruleset: str,
    *,
    seed: str,
    pairs: int,
    max_rounds: int,
    max_turns_per_round: int,
    bootstrap_resamples: int,
) -> dict[str, Any]:
    players = 4 if ruleset == "tenhou-4p" else 3
    return {
        "ruleset": ruleset,
        "players": players,
        "paired_match": {
            "pairs": pairs,
            "seed": derive_seed(seed, "internal-final-evaluation", ruleset),
            "max_rounds": max_rounds,
            "max_turns_per_round": max_turns_per_round,
            "bootstrap_resamples": bootstrap_resamples,
            "promotion_seat": 0,
            "heuristic_baseline": fixed_heuristic_baseline_manifest(players),
        },
    }


def _validate_ruleset_plan(plan: dict[str, Any]) -> None:
    ruleset = plan.get("ruleset")
    players = 4 if ruleset == "tenhou-4p" else 3 if ruleset == "tenhou-3p" else None
    if set(plan) != {"ruleset", "players", "paired_match"} or plan.get("players") != players:
        raise ValueError("ruleset plan is invalid")
    paired_match = plan.get("paired_match")
    if not isinstance(paired_match, dict):
        raise ValueError("paired_match plan must be an object")
    required = {
        "pairs",
        "seed",
        "max_rounds",
        "max_turns_per_round",
        "bootstrap_resamples",
        "promotion_seat",
        "heuristic_baseline",
    }
    if set(paired_match) != required:
        raise ValueError("paired_match plan fields are invalid")
    for name in ("pairs", "max_rounds", "max_turns_per_round", "bootstrap_resamples"):
        if type(paired_match[name]) is not int or paired_match[name] <= 0:
            raise ValueError(f"paired_match {name} must be positive")
    if not isinstance(paired_match["seed"], str) or not paired_match["seed"]:
        raise ValueError("paired_match seed must be a non-empty string")
    if paired_match["promotion_seat"] != 0:
        raise ValueError("paired_match promotion_seat must be 0")
    if paired_match["heuristic_baseline"] != fixed_heuristic_baseline_manifest(players):
        raise ValueError("paired_match heuristic baseline is not fixed")


def _fingerprint(payload: dict[str, Any]) -> str:
    encoded = json.dumps(payload, sort_keys=True, separators=(",", ":")).encode("utf-8")
    return "blake2b-128:" + blake2b(encoded, digest_size=16).hexdigest()
