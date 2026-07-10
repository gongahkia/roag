#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from collections.abc import Iterable, Sequence
from pathlib import Path
from typing import Any

BUNDLE_KIND = "kenjaku-supervised-baseline-comparison-v0"
TARGETS = ("discard", "call", "riichi", "deal_in")
SEED_IDS = ("todo-104-s0", "todo-104-s1", "todo-104-s2")
LABELS = {"exceeded", "matched", "missed"}
ACCURACY_TOLERANCE = 0.005
LOSS_TOLERANCE = 0.001


def main(argv: Sequence[str] | None = None) -> int:
    args = _parser().parse_args(argv)
    try:
        bundle = _json_object(args.bundle)
        errors = validate_bundle(bundle, bundle_path=args.bundle)
    except ValueError as error:
        print(str(error), file=sys.stderr)
        return 1
    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1
    print(f"supervised baseline comparison ok: {args.bundle}")
    return 0


def validate_bundle(bundle: dict[str, Any], *, bundle_path: Path) -> list[str]:
    errors: list[str] = []
    if bundle.get("kind") != BUNDLE_KIND:
        errors.append(f"bundle kind must be {BUNDLE_KIND}")
    try:
        repo_root = _repo_root(bundle_path)
    except ValueError as error:
        return [str(error)]
    errors.extend(_validate_ignored_path(bundle_path, repo_root=repo_root, field="bundle"))
    errors.extend(_validate_summary_path(bundle, repo_root=repo_root))
    errors.extend(_validate_inputs(bundle, repo_root=repo_root))
    errors.extend(_validate_targets(bundle, repo_root=repo_root))
    return errors


def _validate_summary_path(bundle: dict[str, Any], *, repo_root: Path) -> list[str]:
    summary_path = bundle.get("checked_in_summary_path")
    if not isinstance(summary_path, str) or not summary_path.strip():
        return ["checked_in_summary_path must be a non-empty string"]
    path = Path(summary_path)
    if not path.is_file():
        return [f"checked_in_summary_path does not exist: {path}"]
    return _validate_tracked_path(path, repo_root=repo_root, field="checked_in_summary_path")


def _validate_inputs(bundle: dict[str, Any], *, repo_root: Path) -> list[str]:
    inputs = bundle.get("inputs")
    if not isinstance(inputs, dict):
        return ["inputs must be an object"]
    errors: list[str] = []
    for key in ("bc_manifest", "shared_snapshots", "baseline_report"):
        value = inputs.get(key)
        field = f"inputs.{key}"
        if not isinstance(value, dict):
            errors.append(f"{field} must be an object")
            continue
        errors.extend(_validate_path_object(value, repo_root=repo_root, field=field))
        if key != "baseline_report":
            source_command = value.get("source_command")
            if not isinstance(source_command, str) or not source_command.strip():
                errors.append(f"{field}.source_command must be a non-empty string")
    baseline = inputs.get("baseline_report")
    if isinstance(baseline, dict):
        family = baseline.get("family")
        row_name = baseline.get("row_name")
        for key in ("family", "row_name"):
            value = baseline.get(key)
            if not isinstance(value, str) or not value.strip():
                errors.append(f"inputs.baseline_report.{key} must be a non-empty string")
        path_value = baseline.get("path")
        if (
            isinstance(path_value, str)
            and path_value.strip()
            and Path(path_value).is_file()
            and isinstance(family, str)
            and family.strip()
            and isinstance(row_name, str)
            and row_name.strip()
        ):
            errors.extend(
                _validate_baseline_row(Path(path_value), family=family, row_name=row_name)
            )
    return errors


def _validate_baseline_row(path: Path, *, family: str, row_name: str) -> list[str]:
    try:
        report = _json_object(path)
    except ValueError as error:
        return [str(error)]
    if report.get("kind") != "kenjaku-external-baseline-report-v0":
        return ["inputs.baseline_report.kind must be kenjaku-external-baseline-report-v0"]
    baselines = report.get("baselines")
    if not isinstance(baselines, list):
        return ["inputs.baseline_report.baselines must be a list"]
    for row in baselines:
        if (
            isinstance(row, dict)
            and row.get("family") == family
            and row.get("name") == row_name
        ):
            return []
    return [f"inputs.baseline_report missing row {family}:{row_name}"]


def _validate_targets(bundle: dict[str, Any], *, repo_root: Path) -> list[str]:
    targets = bundle.get("targets")
    if not isinstance(targets, dict):
        return ["targets must be an object"]
    errors: list[str] = []
    for target in TARGETS:
        value = targets.get(target)
        if not isinstance(value, dict):
            errors.append(f"targets.{target} must be an object")
            continue
        errors.extend(_validate_target(target, value, repo_root=repo_root))
    return errors


def _validate_target(target: str, row: dict[str, Any], *, repo_root: Path) -> list[str]:
    errors: list[str] = []
    prefix = f"targets.{target}"
    primary_metric = row.get("primary_metric")
    if not isinstance(primary_metric, str) or not primary_metric.strip():
        errors.append(f"{prefix}.primary_metric must be a non-empty string")
    direction = row.get("direction")
    if direction not in {"higher", "lower"}:
        errors.append(f"{prefix}.direction must be 'higher' or 'lower'")
        direction = "higher"
    label = row.get("label")
    if label not in LABELS:
        errors.append(f"{prefix}.label must be exceeded, matched, or missed")
        label = "missed"
    baseline_value = _required_number(row.get("baseline"), "value", f"{prefix}.baseline")
    mean_value = _required_number(row.get("kenjaku"), "mean", f"{prefix}.kenjaku")
    best_value = _required_number(row.get("kenjaku"), "best", f"{prefix}.kenjaku")
    if isinstance(baseline_value, str):
        errors.append(baseline_value)
    if isinstance(mean_value, str):
        errors.append(mean_value)
    if isinstance(best_value, str):
        errors.append(best_value)
    errors.extend(_validate_seeds(target, row, direction=direction, repo_root=repo_root))
    errors.extend(_validate_guards(target, row))
    if (
        not isinstance(baseline_value, str)
        and not isinstance(mean_value, str)
        and not isinstance(best_value, str)
    ):
        expected = _expected_label(
            target,
            baseline_value,
            mean_value,
            best_value,
            direction=direction,
            ci95=_ci95(row.get("baseline")),
        )
        if label != expected:
            errors.append(f"{prefix}.label must be {expected} for recorded metrics, got {label}")
    return errors


def _validate_seeds(
    target: str,
    row: dict[str, Any],
    *,
    direction: str,
    repo_root: Path,
) -> list[str]:
    prefix = f"targets.{target}.kenjaku"
    kenjaku = row.get("kenjaku")
    if not isinstance(kenjaku, dict):
        return [f"{prefix} must be an object"]
    seeds = kenjaku.get("seeds")
    if not isinstance(seeds, list) or len(seeds) != len(SEED_IDS):
        return [f"{prefix}.seeds must contain exactly 3 seed rows"]
    errors: list[str] = []
    seen: set[str] = set()
    values: list[float] = []
    for index, seed in enumerate(seeds):
        field = f"{prefix}.seeds[{index}]"
        if not isinstance(seed, dict):
            errors.append(f"{field} must be an object")
            continue
        seed_id = seed.get("seed_id")
        if not isinstance(seed_id, str) or not seed_id.strip():
            errors.append(f"{field}.seed_id must be a non-empty string")
        else:
            seen.add(seed_id)
        errors.extend(_validate_path_object(seed, repo_root=repo_root, field=field))
        value = seed.get("value")
        number = _as_float(value)
        if number is None:
            errors.append(f"{field}.value must be a number")
        else:
            values.append(number)
    if seen != set(SEED_IDS):
        errors.append(f"{prefix}.seeds seed_id set must be {', '.join(SEED_IDS)}")
    if len(values) == len(SEED_IDS):
        mean = sum(values) / len(values)
        reported_mean = _as_float(kenjaku.get("mean"))
        if reported_mean is not None and abs(reported_mean - mean) > 1e-6:
            errors.append(f"{prefix}.mean must equal seed mean {mean:.6f}")
        best = max(values) if direction == "higher" else min(values)
        reported_best = _as_float(kenjaku.get("best"))
        if reported_best is not None and abs(reported_best - best) > 1e-6:
            errors.append(f"{prefix}.best must equal best seed {best:.6f}")
    return errors


def _validate_guards(target: str, row: dict[str, Any]) -> list[str]:
    guards = row.get("guards")
    if not isinstance(guards, dict):
        return [f"targets.{target}.guards must be an object"]
    errors: list[str] = []
    if target == "discard":
        comparable = guards.get("comparable_decisions")
        if not isinstance(comparable, int) or isinstance(comparable, bool) or comparable <= 0:
            errors.append("targets.discard.guards.comparable_decisions must be a positive integer")
        for key in ("missing_predictions", "illegal_predictions"):
            if guards.get(key) != 0:
                errors.append(f"targets.discard.guards.{key} must be 0")
    if target in {"call", "riichi"}:
        for key in ("pass_recall", f"{target}_recall"):
            value = guards.get(key)
            if not _is_number(value):
                errors.append(f"targets.{target}.guards.{key} must be a number")
    if target == "deal_in":
        for key in (
            "balanced_accuracy",
            "recall",
            "specificity",
            "heuristic_brier_delta",
            "heuristic_log_loss_delta",
        ):
            value = guards.get(key)
            if not _is_number(value):
                errors.append(f"targets.deal_in.guards.{key} must be a number")
    return errors


def _expected_label(
    target: str,
    baseline: float,
    mean: float,
    best: float,
    *,
    direction: str,
    ci95: tuple[float, float] | None,
) -> str:
    mean_delta = _delta(baseline, mean, direction=direction)
    best_delta = _delta(baseline, best, direction=direction)
    if mean_delta > 0 and best_delta > 0:
        return "exceeded"
    if ci95 is not None and ci95[0] <= mean <= ci95[1]:
        return "matched"
    tolerance = LOSS_TOLERANCE if target == "deal_in" else ACCURACY_TOLERANCE
    if mean_delta >= -tolerance:
        return "matched"
    return "missed"


def _delta(baseline: float, kenjaku: float, *, direction: str) -> float:
    if direction == "lower":
        return baseline - kenjaku
    return kenjaku - baseline


def _ci95(value: Any) -> tuple[float, float] | None:
    if not isinstance(value, dict):
        return None
    ci = value.get("ci95")
    if not isinstance(ci, list | tuple) or len(ci) != 2:
        return None
    low = _as_float(ci[0])
    high = _as_float(ci[1])
    if low is None or high is None:
        return None
    return low, high


def _required_number(value: Any, key: str, field: str) -> float | str:
    if not isinstance(value, dict):
        return f"{field} must be an object"
    current = _as_float(value.get(key))
    if current is None:
        return f"{field}.{key} must be a number"
    return current


def _validate_path_object(row: dict[str, Any], *, repo_root: Path, field: str) -> list[str]:
    path_value = row.get("path")
    if not isinstance(path_value, str) or not path_value.strip():
        return [f"{field}.path must be a non-empty string"]
    path = Path(path_value)
    if not path.is_file():
        return [f"{field}.path does not exist: {path}"]
    return _validate_ignored_path(path, repo_root=repo_root, field=f"{field}.path")


def _validate_ignored_path(path: Path, *, repo_root: Path, field: str) -> list[str]:
    try:
        relative = path.resolve().relative_to(repo_root)
    except ValueError:
        return [f"{field} must be under repo root: {path}"]
    errors: list[str] = []
    if _git(["ls-files", "--error-unmatch", "--", str(relative)], repo_root).returncode == 0:
        errors.append(f"{field} is tracked by git: {relative}")
    if _git(["check-ignore", "-q", "--", str(relative)], repo_root).returncode != 0:
        errors.append(f"{field} is not ignored by git: {relative}")
    return errors


def _validate_tracked_path(path: Path, *, repo_root: Path, field: str) -> list[str]:
    try:
        relative = path.resolve().relative_to(repo_root)
    except ValueError:
        return [f"{field} must be under repo root: {path}"]
    if _git(["ls-files", "--error-unmatch", "--", str(relative)], repo_root).returncode != 0:
        return [f"{field} is not tracked by git: {relative}"]
    return []


def _json_object(path: Path) -> dict[str, Any]:
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except FileNotFoundError as error:
        raise ValueError(f"file does not exist: {path}") from error
    except json.JSONDecodeError as error:
        raise ValueError(f"invalid JSON in {path}: {error}") from error
    if not isinstance(payload, dict):
        raise ValueError(f"JSON file must contain an object: {path}")
    return payload


def _is_number(value: Any) -> bool:
    return _as_float(value) is not None


def _as_float(value: Any) -> float | None:
    if isinstance(value, bool) or not isinstance(value, int | float):
        return None
    return float(value)


def _repo_root(path: Path) -> Path:
    result = _git(["rev-parse", "--show-toplevel"], path.parent)
    if result.returncode != 0:
        raise ValueError("git repository root not found")
    return Path(result.stdout.strip()).resolve()


def _git(args: Iterable[str], cwd: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["git", *args],
        cwd=cwd,
        text=True,
        capture_output=True,
        check=False,
    )


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Validate TODO-104 supervised baseline comparison evidence."
    )
    parser.add_argument("bundle", type=Path, help="supervised baseline comparison bundle JSON")
    return parser


if __name__ == "__main__":
    raise SystemExit(main())
