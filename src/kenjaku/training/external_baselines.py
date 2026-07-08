from __future__ import annotations

import json
from collections import Counter
from dataclasses import dataclass
from datetime import UTC, datetime
from pathlib import Path
from typing import Any

from kenjaku.core import ActionKind
from kenjaku.training.decision_snapshots import DECISION_SNAPSHOT_KIND

EXTERNAL_BASELINE_REPORT_KIND = "kenjaku-external-baseline-report-v0"
DEFAULT_MINIMUM_COMPARABLE_DECISIONS = 1000
CONFIDENCE_LEVEL = 0.95
WILSON_Z_95 = 1.959963984540054


@dataclass(frozen=True, slots=True)
class ExternalBaselineSpec:
    family: str
    name: str
    predictions_path: Path


def parse_external_baseline_spec(value: str) -> ExternalBaselineSpec:
    if "=" not in value:
        raise ValueError("baseline spec must be FAMILY:NAME=PATH or NAME=PATH")
    raw_label, raw_path = value.split("=", 1)
    label = raw_label.strip()
    path = Path(raw_path.strip())
    if not label:
        raise ValueError("baseline name must not be empty")
    if not raw_path.strip():
        raise ValueError("baseline prediction path must not be empty")

    if ":" in label:
        raw_family, raw_name = label.split(":", 1)
        family = raw_family.strip()
        name = raw_name.strip()
    else:
        family = "unspecified"
        name = label
    if not family:
        raise ValueError("baseline family must not be empty")
    if not name:
        raise ValueError("baseline name must not be empty")
    return ExternalBaselineSpec(family=family, name=name, predictions_path=path)


def build_external_baseline_report(
    snapshots_path: Path,
    baseline_specs: list[ExternalBaselineSpec],
    *,
    minimum_comparable_decisions: int = DEFAULT_MINIMUM_COMPARABLE_DECISIONS,
) -> dict[str, Any]:
    if minimum_comparable_decisions < 1:
        raise ValueError("--min-decisions must be at least 1")
    if not baseline_specs:
        raise ValueError("at least one --baseline is required")

    snapshots, snapshot_stats = _read_decision_snapshots(snapshots_path)
    baselines = [_build_baseline_section(snapshots, spec) for spec in baseline_specs]
    short_baselines = [
        f"{baseline['family']}:{baseline['name']}"
        for baseline in baselines
        if baseline["comparable_decisions"] < minimum_comparable_decisions
    ]
    if short_baselines:
        joined = ", ".join(short_baselines)
        raise ValueError(
            "baseline(s) below minimum comparable decisions "
            f"({minimum_comparable_decisions}): {joined}"
        )

    return {
        "kind": EXTERNAL_BASELINE_REPORT_KIND,
        "created_at": datetime.now(UTC).isoformat(),
        "protocol": {
            "name": "offline-shared-log-decision-snapshot",
            "comparison_unit": "decision",
            "scenario_set": str(snapshots_path),
            "automation": "offline only; no ranked automation",
            "prediction_boundary": "JSONL rows keyed by decision snapshot row_id",
            "minimum_comparable_decisions": minimum_comparable_decisions,
            "confidence_level": CONFIDENCE_LEVEL,
            "confidence_interval_method": "Wilson score interval",
        },
        "snapshots": snapshot_stats,
        "minimum_satisfied": all(
            baseline["comparable_decisions"] >= minimum_comparable_decisions
            for baseline in baselines
        ),
        "baselines": baselines,
    }


def format_external_baseline_report(report: dict[str, Any]) -> str:
    protocol = report["protocol"]
    snapshots = report["snapshots"]
    lines = [
        f"kind: {report['kind']}",
        f"protocol: {protocol['name']}",
        f"scenario_set: {protocol['scenario_set']}",
        f"snapshots: {snapshots['snapshots']}",
        f"malformed_snapshot_rows: {snapshots['malformed_rows']}",
        f"mjai_events: present={snapshots['mjai_events']['present']} "
        f"missing={snapshots['mjai_events']['missing']}",
        (f"minimum_comparable_decisions: {protocol['minimum_comparable_decisions']}"),
        f"minimum_satisfied: {_yes_no(report['minimum_satisfied'])}",
        "baselines:",
    ]
    for baseline in report["baselines"]:
        overall = baseline["overall"]
        lines.append(
            f"  {baseline['family']}:{baseline['name']}: "
            f"comparable={baseline['comparable_decisions']} "
            f"accuracy={_format_optional_rate(overall['accuracy'])} "
            f"ci95={_format_interval(overall['accuracy_ci'])} "
            f"missing={baseline['missing_predictions']} "
            f"illegal={baseline['illegal_predictions']} "
            f"malformed_predictions={baseline['malformed_prediction_rows']}"
        )
        for decision_type, bucket in baseline["by_decision_type"].items():
            lines.append(
                f"    {decision_type}: "
                f"examples={bucket['examples']} "
                f"accuracy={_format_optional_rate(bucket['accuracy'])} "
                f"ci95={_format_interval(bucket['accuracy_ci'])}"
            )
        if baseline["binary"]:
            lines.append("    binary:")
            for decision_type, bucket in baseline["binary"].items():
                lines.append(
                    f"      {decision_type}: "
                    f"accuracy={_format_optional_rate(bucket['accuracy'])} "
                    f"ci95={_format_interval(bucket['accuracy_ci'])} "
                    f"pass_recall={_format_optional_rate(bucket['pass_recall'])} "
                    f"{decision_type}_recall="
                    f"{_format_optional_rate(bucket[f'{decision_type}_recall'])}"
                )
    return "\n".join(lines)


def _read_decision_snapshots(path: Path) -> tuple[list[dict[str, Any]], dict[str, Any]]:
    snapshots: list[dict[str, Any]] = []
    rows = 0
    malformed_rows = 0
    duplicate_row_ids = 0
    seen_row_ids: set[str] = set()
    decision_types: Counter[str] = Counter()
    sources: Counter[str] = Counter()
    mjai_present = 0
    mjai_missing = 0

    for line in path.read_text(encoding="utf-8").splitlines():
        rows += 1
        try:
            payload = json.loads(line)
        except json.JSONDecodeError:
            malformed_rows += 1
            continue
        if not _valid_snapshot(payload):
            malformed_rows += 1
            continue

        row_id = payload["row_id"]
        if row_id in seen_row_ids:
            duplicate_row_ids += 1
        seen_row_ids.add(row_id)
        snapshots.append(payload)
        decision_types[payload["decision_type"]] += 1
        sources[_snapshot_source_label(payload)] += 1
        if _snapshot_has_mjai_events(payload):
            mjai_present += 1
        else:
            mjai_missing += 1

    stats = {
        "path": str(path),
        "rows": rows,
        "snapshots": len(snapshots),
        "malformed_rows": malformed_rows,
        "duplicate_row_ids": duplicate_row_ids,
        "decision_types": dict(sorted(decision_types.items())),
        "sources": dict(sorted(sources.items())),
        "mjai_events": {
            "present": mjai_present,
            "missing": mjai_missing,
        },
    }
    return snapshots, stats


def _valid_snapshot(value: Any) -> bool:
    return (
        isinstance(value, dict)
        and value.get("kind") == DECISION_SNAPSHOT_KIND
        and isinstance(value.get("row_id"), str)
        and isinstance(value.get("decision_type"), str)
        and isinstance(value.get("actual_action"), dict)
    )


def _snapshot_source_label(payload: dict[str, Any]) -> str:
    source = payload.get("source")
    if isinstance(source, dict) and isinstance(source.get("label"), str):
        return source["label"]
    return "unknown"


def _snapshot_has_mjai_events(payload: dict[str, Any]) -> bool:
    events = payload.get("mjai_events")
    return isinstance(events, list) and bool(events)


def _build_baseline_section(
    snapshots: list[dict[str, Any]],
    spec: ExternalBaselineSpec,
) -> dict[str, Any]:
    predictions, prediction_stats = _read_predictions(spec.predictions_path)
    overall = _empty_metric_bucket()
    by_decision_type: dict[str, dict[str, int]] = {}
    binary = {
        "call": _empty_binary_bucket(),
        "riichi": _empty_binary_bucket(),
    }
    missing_predictions = 0
    illegal_predictions = 0

    for snapshot in snapshots:
        row_id = snapshot["row_id"]
        predicted_action = predictions.get(row_id)
        if predicted_action is None:
            missing_predictions += 1
            continue

        actual_action = snapshot["actual_action"]
        decision_type = snapshot["decision_type"]
        if not _prediction_is_legal(snapshot, predicted_action):
            illegal_predictions += 1

        correct = _normalized_action(actual_action) == predicted_action
        _record_metric(overall, correct)
        _record_metric(
            by_decision_type.setdefault(decision_type, _empty_metric_bucket()),
            correct,
        )
        if decision_type in binary:
            _record_binary(
                binary[decision_type],
                target=decision_type,
                actual_action=actual_action,
                predicted_action=predicted_action,
            )

    comparable = overall["examples"]
    return {
        "family": spec.family,
        "name": spec.name,
        "predictions_path": str(spec.predictions_path),
        "prediction_rows": prediction_stats["valid_predictions"],
        "malformed_prediction_rows": prediction_stats["malformed_prediction_rows"],
        "duplicate_prediction_rows": prediction_stats["duplicate_prediction_rows"],
        "missing_predictions": missing_predictions,
        "illegal_predictions": illegal_predictions,
        "comparable_decisions": comparable,
        "overall": _finalize_metric_bucket(overall),
        "by_decision_type": {
            decision_type: _finalize_metric_bucket(bucket)
            for decision_type, bucket in sorted(by_decision_type.items())
        },
        "binary": {
            decision_type: _finalize_binary_bucket(bucket, target=decision_type)
            for decision_type, bucket in binary.items()
            if bucket["examples"]
        },
    }


def _read_predictions(path: Path) -> tuple[dict[str, dict[str, Any]], dict[str, int]]:
    predictions: dict[str, dict[str, Any]] = {}
    malformed_rows = 0
    duplicate_rows = 0
    for line in path.read_text(encoding="utf-8").splitlines():
        try:
            payload = json.loads(line)
        except json.JSONDecodeError:
            malformed_rows += 1
            continue
        if not isinstance(payload, dict):
            malformed_rows += 1
            continue
        row_id = payload.get("row_id")
        action = payload.get("predicted_action")
        if not isinstance(row_id, str) or not isinstance(action, dict):
            malformed_rows += 1
            continue
        if row_id in predictions:
            duplicate_rows += 1
            continue
        predictions[row_id] = _normalized_action(action)
    return predictions, {
        "valid_predictions": len(predictions),
        "malformed_prediction_rows": malformed_rows,
        "duplicate_prediction_rows": duplicate_rows,
    }


def _prediction_is_legal(
    snapshot: dict[str, Any],
    predicted_action: dict[str, Any],
) -> bool:
    legal_actions = snapshot.get("legal_actions")
    if not isinstance(legal_actions, list):
        return False
    return any(
        isinstance(action, dict) and _normalized_action(action) == predicted_action
        for action in legal_actions
    )


def _normalized_action(action: dict[str, Any]) -> dict[str, Any]:
    normalized = {"kind": action.get("kind")}
    if "tile" in action:
        normalized["tile"] = action.get("tile")
    if "tsumogiri" in action:
        normalized["tsumogiri"] = bool(action.get("tsumogiri"))
    if "consumed" in action and isinstance(action["consumed"], list):
        normalized["consumed"] = list(action["consumed"])
    return normalized


def _empty_metric_bucket() -> dict[str, int]:
    return {"examples": 0, "correct": 0}


def _record_metric(bucket: dict[str, int], correct: bool) -> None:
    bucket["examples"] += 1
    bucket["correct"] += int(correct)


def _finalize_metric_bucket(bucket: dict[str, int]) -> dict[str, Any]:
    examples = bucket["examples"]
    correct = bucket["correct"]
    return {
        "examples": examples,
        "correct": correct,
        "accuracy": _safe_ratio(correct, examples),
        "accuracy_ci": _wilson_interval(correct, examples),
    }


def _empty_binary_bucket() -> dict[str, int]:
    return {
        "examples": 0,
        "true_positive": 0,
        "false_positive": 0,
        "true_negative": 0,
        "false_negative": 0,
    }


def _record_binary(
    bucket: dict[str, int],
    *,
    target: str,
    actual_action: dict[str, Any],
    predicted_action: dict[str, Any],
) -> None:
    actual_positive = _action_is_positive(actual_action, target=target)
    predicted_positive = _action_is_positive(predicted_action, target=target)
    bucket["examples"] += 1
    if actual_positive and predicted_positive:
        bucket["true_positive"] += 1
    elif actual_positive:
        bucket["false_negative"] += 1
    elif predicted_positive:
        bucket["false_positive"] += 1
    else:
        bucket["true_negative"] += 1


def _action_is_positive(action: dict[str, Any], *, target: str) -> bool:
    kind = action.get("kind")
    if target == "call":
        return kind != ActionKind.PASS.value
    if target == "riichi":
        return kind == ActionKind.RIICHI.value
    return False


def _finalize_binary_bucket(bucket: dict[str, int], *, target: str) -> dict[str, Any]:
    true_positive = bucket["true_positive"]
    false_positive = bucket["false_positive"]
    true_negative = bucket["true_negative"]
    false_negative = bucket["false_negative"]
    target_examples = true_positive + false_negative
    pass_examples = true_negative + false_positive
    accuracy_successes = true_positive + true_negative
    return {
        **bucket,
        "accuracy": _safe_ratio(accuracy_successes, bucket["examples"]),
        "accuracy_ci": _wilson_interval(accuracy_successes, bucket["examples"]),
        f"{target}_precision": _safe_ratio(
            true_positive,
            true_positive + false_positive,
        ),
        f"{target}_precision_ci": _wilson_interval(
            true_positive,
            true_positive + false_positive,
        ),
        f"{target}_recall": _safe_ratio(true_positive, target_examples),
        f"{target}_recall_ci": _wilson_interval(true_positive, target_examples),
        "pass_recall": _safe_ratio(true_negative, pass_examples),
        "pass_recall_ci": _wilson_interval(true_negative, pass_examples),
    }


def _safe_ratio(numerator: int, denominator: int) -> float | None:
    if denominator == 0:
        return None
    return numerator / denominator


def _wilson_interval(successes: int, total: int) -> dict[str, Any] | None:
    if total == 0:
        return None
    z = WILSON_Z_95
    p_hat = successes / total
    denominator = 1 + (z * z / total)
    center = p_hat + (z * z / (2 * total))
    margin = z * ((p_hat * (1 - p_hat) + (z * z / (4 * total))) / total) ** 0.5
    low = max(0.0, (center - margin) / denominator)
    high = min(1.0, (center + margin) / denominator)
    return {
        "level": CONFIDENCE_LEVEL,
        "method": "wilson",
        "low": low,
        "high": high,
    }


def _format_optional_rate(value: float | None) -> str:
    if value is None:
        return "n/a"
    return f"{value:.4f}"


def _format_interval(interval: dict[str, Any] | None) -> str:
    if interval is None:
        return "n/a"
    return f"[{interval['low']:.4f}, {interval['high']:.4f}]"


def _yes_no(value: bool) -> str:
    return "yes" if value else "no"
