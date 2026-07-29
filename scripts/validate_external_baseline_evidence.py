#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from collections.abc import Iterable, Sequence
from pathlib import Path
from typing import Any

REPORT_KIND = "kenjaku-external-baseline-report-v0"
DEFAULT_FAMILY = "mortal-compatible"
SMOKE_MARKERS = ("smoke", "stub", "pass", "first-legal", "echo", "protocol")


def main(argv: Sequence[str] | None = None) -> int:
    args = _parser().parse_args(argv)
    try:
        report = _json_object(args.report)
        errors = validate_report(
            report,
            report_path=args.report,
            family=args.family,
            minimum_decisions=args.min_decisions,
        )
    except ValueError as error:
        print(str(error), file=sys.stderr)
        return 1
    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1
    print(f"external baseline evidence ok: family={args.family} report={args.report}")
    return 0


def validate_report(
    report: dict[str, Any],
    *,
    report_path: Path,
    family: str,
    minimum_decisions: int,
) -> list[str]:
    errors: list[str] = []
    if report.get("kind") != REPORT_KIND:
        errors.append(f"report kind must be {REPORT_KIND}")
    if minimum_decisions < 1:
        errors.append("--min-decisions must be positive")
    try:
        repo_root = _repo_root(report_path)
    except ValueError as error:
        return [str(error)]
    errors.extend(_validate_ignored_path(report_path, repo_root=repo_root, field="report"))
    errors.extend(_validate_scenario_set(report, repo_root=repo_root))
    baselines = report.get("baselines")
    if not isinstance(baselines, list):
        return [*errors, "baselines must be a list"]
    candidates = [row for row in baselines if isinstance(row, dict) and row.get("family") == family]
    if not candidates:
        return [*errors, f"missing baseline family {family!r}"]
    real_candidates = [row for row in candidates if not _looks_like_smoke(row)]
    if not real_candidates:
        return [*errors, f"baseline family {family!r} only has smoke/stub rows"]
    for row in real_candidates:
        errors.extend(
            _validate_baseline_row(
                row,
                family=family,
                minimum_decisions=minimum_decisions,
                repo_root=repo_root,
            )
        )
    return errors


def _validate_baseline_row(
    row: dict[str, Any],
    *,
    family: str,
    minimum_decisions: int,
    repo_root: Path,
) -> list[str]:
    errors: list[str] = []
    label = f"{family}:{row.get('name', '<unnamed>')}"
    predictions_path = row.get("predictions_path")
    if not isinstance(predictions_path, str) or not predictions_path.strip():
        errors.append(f"{label} predictions_path must be a non-empty string")
    else:
        errors.extend(
            _validate_ignored_path(
                Path(predictions_path),
                repo_root=repo_root,
                field=f"{label} predictions_path",
            )
        )
        if not Path(predictions_path).is_file():
            errors.append(f"{label} predictions_path does not exist: {predictions_path}")
        elif Path(predictions_path).stat().st_size <= 0:
            errors.append(f"{label} predictions_path must not be empty: {predictions_path}")
    comparable = row.get("comparable_decisions")
    if not isinstance(comparable, int) or comparable < minimum_decisions:
        errors.append(
            f"{label} comparable_decisions must be >= {minimum_decisions}, got {comparable!r}"
        )
    for field in (
        "missing_predictions",
        "illegal_predictions",
        "malformed_prediction_rows",
        "duplicate_prediction_rows",
    ):
        value = row.get(field)
        if value != 0:
            errors.append(f"{label} {field} must be 0, got {value!r}")
    by_decision_type = row.get("by_decision_type")
    if not isinstance(by_decision_type, dict) or "discard" not in by_decision_type:
        errors.append(f"{label} must include discard decision metrics")
    binary = row.get("binary")
    if isinstance(binary, dict):
        for target in ("call", "riichi"):
            bucket = binary.get(target)
            if isinstance(bucket, dict) and bucket.get("balanced_accuracy") is None:
                errors.append(f"{label} {target} balanced_accuracy must not be null")
    return errors


def _validate_scenario_set(report: dict[str, Any], *, repo_root: Path) -> list[str]:
    protocol = report.get("protocol")
    if not isinstance(protocol, dict):
        return ["protocol must be an object"]
    scenario_set = protocol.get("scenario_set")
    if not isinstance(scenario_set, str) or not scenario_set.strip():
        return ["protocol.scenario_set must be a non-empty string"]
    path = Path(scenario_set)
    errors: list[str] = []
    text = str(path)
    if "data/fixtures" in text or "fixtures/tenhou" in text:
        errors.append("protocol.scenario_set must not use checked-in fixtures")
    if not path.is_file():
        errors.append(f"protocol.scenario_set does not exist: {path}")
    errors.extend(_validate_ignored_path(path, repo_root=repo_root, field="protocol.scenario_set"))
    return errors


def _looks_like_smoke(row: dict[str, Any]) -> bool:
    name = str(row.get("name", "")).lower()
    path = str(row.get("predictions_path", "")).lower()
    text = f"{name} {path}"
    return any(marker in text for marker in SMOKE_MARKERS)


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
    parser = argparse.ArgumentParser(description="Validate TODO-103 external baseline evidence.")
    parser.add_argument("report", type=Path, help="external-baseline-report JSON")
    parser.add_argument("--family", default=DEFAULT_FAMILY)
    parser.add_argument("--min-decisions", type=int, default=1000)
    return parser


if __name__ == "__main__":
    raise SystemExit(main())
