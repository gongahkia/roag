"""Deterministic model selection bound to frozen synthetic split manifests."""

from __future__ import annotations

import hashlib
import json
import math
from collections.abc import Mapping, Sequence
from typing import Any

from kenjaku.simulation.synthetic_splits import (
    SYNTHETIC_MATCH_SPLIT_MANIFEST_V1_FIELDS,
    SYNTHETIC_MATCH_SPLIT_MANIFEST_V1_KIND,
)

FROZEN_MODEL_SELECTION_KIND = "kenjaku-frozen-model-selection-v0"
_METRIC_DIRECTIONS = {"loss": "min", "accuracy": "max"}


def select_model_on_frozen_split(
    split_manifest: Mapping[str, Any],
    candidates: Sequence[Mapping[str, Any]],
    *,
    metric: str = "loss",
) -> dict[str, Any]:
    """Select one candidate from validation metrics tied to an exact frozen split."""
    if metric not in _METRIC_DIRECTIONS:
        raise ValueError("metric must be loss or accuracy")
    _validate_split_manifest(split_manifest)
    if not candidates:
        raise ValueError("at least one model candidate is required")
    rows = [_candidate_row(candidate, metric=metric) for candidate in candidates]
    if len({row["model_id"] for row in rows}) != len(rows):
        raise ValueError("candidate model_id values must be unique")
    direction = _METRIC_DIRECTIONS[metric]
    selected = sorted(
        rows,
        key=lambda row: (row["metric"], row["model_id"])
        if direction == "min"
        else (-row["metric"], row["model_id"]),
    )[0]
    return {
        "kind": FROZEN_MODEL_SELECTION_KIND,
        "split_manifest_sha256": _canonical_sha256(split_manifest),
        "ruleset": split_manifest["source"]["ruleset"],
        "selection_split": "validation",
        "metric": metric,
        "direction": direction,
        "selected": selected,
        "candidates": sorted(rows, key=lambda row: row["model_id"]),
    }


def _validate_split_manifest(manifest: Mapping[str, Any]) -> None:
    if set(manifest) != set(SYNTHETIC_MATCH_SPLIT_MANIFEST_V1_FIELDS):
        raise ValueError("split manifest fields do not match v1")
    if manifest.get("kind") != SYNTHETIC_MATCH_SPLIT_MANIFEST_V1_KIND:
        raise ValueError("unsupported split manifest kind")
    source = manifest.get("source")
    assignments = manifest.get("assignments")
    if not isinstance(source, Mapping) or not isinstance(assignments, Mapping):
        raise ValueError("split manifest source and assignments must be objects")
    ruleset = source.get("ruleset")
    match_count = source.get("match_count")
    if not isinstance(ruleset, str) or type(match_count) is not int:
        raise ValueError("split manifest source is invalid")
    expected = {"train", "validation", "test"}
    if set(assignments) != expected:
        raise ValueError("split manifest assignments must contain train, validation, and test")
    values = [assignments[name] for name in sorted(expected)]
    if any(
        not isinstance(value, list) or any(type(game) is not int for game in value)
        for value in values
    ):
        raise ValueError("split manifest assignments must be integer arrays")
    game_ids = [game for value in values for game in value]
    if len(game_ids) != match_count or len(set(game_ids)) != len(game_ids):
        raise ValueError("split manifest assignments must partition source matches")


def _candidate_row(candidate: Mapping[str, Any], *, metric: str) -> dict[str, str | float]:
    if not isinstance(candidate, Mapping):
        raise ValueError("each candidate must be an object")
    model_id = candidate.get("model_id")
    validation = candidate.get("validation")
    if not isinstance(model_id, str) or not model_id:
        raise ValueError("candidate model_id must be a non-empty string")
    if not isinstance(validation, Mapping):
        raise ValueError("candidate validation must be an object")
    value = validation.get(metric)
    if not isinstance(value, (int, float)) or isinstance(value, bool):
        raise ValueError("candidate validation metric must be a number")
    metric_value = float(value)
    if not math.isfinite(metric_value):
        raise ValueError("candidate validation metric must be finite")
    return {"model_id": model_id, "metric": metric_value}


def _canonical_sha256(value: Mapping[str, Any]) -> str:
    encoded = json.dumps(value, sort_keys=True, separators=(",", ":")).encode("utf-8")
    return hashlib.sha256(encoded).hexdigest()
