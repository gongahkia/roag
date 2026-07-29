#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from collections.abc import Iterable, Sequence
from pathlib import Path
from typing import Any

MANIFEST_KIND = "kenjaku-final-evaluation-manifest-v0"
REQUIRED_EVALUATIONS = ("supervised", "self_play", "sanma", "interpretability")
REQUIRED_TABLE_METRICS = (
    "accuracy",
    "balanced_accuracy",
    "deal_in_calibration",
    "average_placement",
    "score_delta",
    "ablations",
)


def main(argv: Sequence[str] | None = None) -> int:
    args = _parser().parse_args(argv)
    try:
        manifest = _json_object(args.manifest)
        errors = validate_manifest(manifest, manifest_path=args.manifest)
    except ValueError as error:
        print(str(error), file=sys.stderr)
        return 1
    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1
    print(f"final evaluation manifest ok: {args.manifest}")
    return 0


def validate_manifest(manifest: dict[str, Any], *, manifest_path: Path) -> list[str]:
    errors: list[str] = []
    if manifest.get("kind") != MANIFEST_KIND:
        errors.append(f"manifest kind must be {MANIFEST_KIND}")
    try:
        repo_root = _repo_root(manifest_path)
    except ValueError as error:
        return [str(error)]
    errors.extend(_validate_frozen_inputs(manifest, repo_root=repo_root))
    errors.extend(_validate_evaluations(manifest, repo_root=repo_root))
    errors.extend(_validate_tables(manifest, repo_root=repo_root))
    errors.extend(_validate_commands(manifest))
    return errors


def _validate_frozen_inputs(manifest: dict[str, Any], *, repo_root: Path) -> list[str]:
    errors: list[str] = []
    frozen = manifest.get("frozen_inputs")
    if not isinstance(frozen, dict):
        return ["frozen_inputs must be an object"]
    errors.extend(
        _validate_named_paths(
            frozen.get("dataset_slices"),
            field="frozen_inputs.dataset_slices",
            repo_root=repo_root,
            require_ignored=True,
            require_tracked=False,
            required_text_fields=("source_command",),
        )
    )
    errors.extend(
        _validate_named_paths(
            frozen.get("model_checkpoints"),
            field="frozen_inputs.model_checkpoints",
            repo_root=repo_root,
            require_ignored=True,
            require_tracked=False,
            required_text_fields=(),
        )
    )
    errors.extend(
        _validate_named_paths(
            frozen.get("evaluation_scripts"),
            field="frozen_inputs.evaluation_scripts",
            repo_root=repo_root,
            require_ignored=False,
            require_tracked=True,
            required_text_fields=(),
        )
    )
    return errors


def _validate_evaluations(manifest: dict[str, Any], *, repo_root: Path) -> list[str]:
    errors: list[str] = []
    evaluations = manifest.get("evaluations")
    if not isinstance(evaluations, dict):
        return ["evaluations must be an object"]
    for section in REQUIRED_EVALUATIONS:
        value = evaluations.get(section)
        if not isinstance(value, dict):
            errors.append(f"evaluations.{section} must be an object")
            continue
        reports = value.get("reports")
        errors.extend(
            _validate_named_paths(
                reports,
                field=f"evaluations.{section}.reports",
                repo_root=repo_root,
                require_ignored=True,
                require_tracked=False,
                required_text_fields=("command",),
            )
        )
    return errors


def _validate_tables(manifest: dict[str, Any], *, repo_root: Path) -> list[str]:
    errors: list[str] = []
    tables = manifest.get("metric_tables")
    if not isinstance(tables, list) or not tables:
        return ["metric_tables must be a non-empty list"]
    seen_metrics: set[str] = set()
    for index, row in enumerate(tables):
        prefix = f"metric_tables[{index}]"
        if not isinstance(row, dict):
            errors.append(f"{prefix} must be an object")
            continue
        errors.extend(
            _validate_name_path(
                row,
                field=prefix,
                repo_root=repo_root,
                require_ignored=True,
                require_tracked=False,
            )
        )
        metrics = row.get("metrics")
        if not isinstance(metrics, list) or not metrics:
            errors.append(f"{prefix}.metrics must be a non-empty list")
            continue
        for metric in metrics:
            if not isinstance(metric, str) or not metric.strip():
                errors.append(f"{prefix}.metrics values must be non-empty strings")
            else:
                seen_metrics.add(metric)
    missing = sorted(set(REQUIRED_TABLE_METRICS) - seen_metrics)
    if missing:
        errors.append("metric_tables missing required metrics: " + ", ".join(missing))
    return errors


def _validate_commands(manifest: dict[str, Any]) -> list[str]:
    commands = manifest.get("commands")
    if not isinstance(commands, list) or not commands:
        return ["commands must be a non-empty list"]
    errors: list[str] = []
    for index, row in enumerate(commands):
        prefix = f"commands[{index}]"
        if not isinstance(row, dict):
            errors.append(f"{prefix} must be an object")
            continue
        for key in ("name", "command"):
            value = row.get(key)
            if not isinstance(value, str) or not value.strip():
                errors.append(f"{prefix}.{key} must be a non-empty string")
    return errors


def _validate_named_paths(
    rows: Any,
    *,
    field: str,
    repo_root: Path,
    require_ignored: bool,
    require_tracked: bool,
    required_text_fields: Sequence[str],
) -> list[str]:
    if not isinstance(rows, list) or not rows:
        return [f"{field} must be a non-empty list"]
    errors: list[str] = []
    for index, row in enumerate(rows):
        prefix = f"{field}[{index}]"
        if not isinstance(row, dict):
            errors.append(f"{prefix} must be an object")
            continue
        errors.extend(
            _validate_name_path(
                row,
                field=prefix,
                repo_root=repo_root,
                require_ignored=require_ignored,
                require_tracked=require_tracked,
            )
        )
        for key in required_text_fields:
            value = row.get(key)
            if not isinstance(value, str) or not value.strip():
                errors.append(f"{prefix}.{key} must be a non-empty string")
    return errors


def _validate_name_path(
    row: dict[str, Any],
    *,
    field: str,
    repo_root: Path | None,
    require_ignored: bool,
    require_tracked: bool,
) -> list[str]:
    errors: list[str] = []
    name = row.get("name")
    path_value = row.get("path")
    if not isinstance(name, str) or not name.strip():
        errors.append(f"{field}.name must be a non-empty string")
    if not isinstance(path_value, str) or not path_value.strip():
        errors.append(f"{field}.path must be a non-empty string")
        return errors
    path = Path(path_value)
    if not path.is_file():
        errors.append(f"{field}.path must be an existing file: {path}")
        return errors
    if path.stat().st_size <= 0:
        errors.append(f"{field}.path must not be empty: {path}")
    if require_ignored:
        if repo_root is None:
            errors.append(f"{field}.path cannot check git ignore without repo root")
        else:
            errors.extend(_validate_ignored_path(path, repo_root=repo_root, field=f"{field}.path"))
    if require_tracked:
        if repo_root is None:
            errors.append(f"{field}.path cannot check git tracking without repo root")
        else:
            errors.extend(_validate_tracked_path(path, repo_root=repo_root, field=f"{field}.path"))
    return errors


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
    parser = argparse.ArgumentParser(description="Validate TODO-601 final evaluation manifest.")
    parser.add_argument("manifest", type=Path, help="final evaluation manifest JSON")
    return parser


if __name__ == "__main__":
    raise SystemExit(main())
