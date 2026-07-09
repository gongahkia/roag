#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from collections.abc import Iterable, Sequence
from pathlib import Path
from typing import Any

BUNDLE_KIND = "kenjaku-bc-report-bundle-v0"
REPORT_KINDS = {
    "discard": "kenjaku-discard-benchmark-report-v0",
    "call": "kenjaku-call-benchmark-report-v0",
    "riichi": "kenjaku-riichi-benchmark-report-v0",
}
DEFAULT_MODELS = {
    "discard": "defense_context_linear",
    "call": "call_linear_v1_calibrated",
    "riichi": "riichi_linear_calibrated",
}
REQUIRED_METRICS = (
    "train_loss",
    "eval_loss",
    "train_accuracy",
    "eval_accuracy",
    "train_balanced_accuracy",
    "eval_balanced_accuracy",
    "train_action_recall",
    "eval_action_recall",
)


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
    print(f"bc report bundle ok: {args.bundle}")
    return 0


def validate_bundle(bundle: dict[str, Any], *, bundle_path: Path) -> list[str]:
    errors: list[str] = []
    if bundle.get("kind") != BUNDLE_KIND:
        errors.append(f"bundle kind must be {BUNDLE_KIND}")
    try:
        repo_root = _repo_root(bundle_path)
    except ValueError as error:
        return [str(error)]
    minimum_train = _positive_int(bundle, "minimum_train_decisions", default=100_000)
    minimum_eval = _positive_int(bundle, "minimum_eval_decisions", default=20_000)
    if isinstance(minimum_train, str):
        errors.append(minimum_train)
        minimum_train = 100_000
    if isinstance(minimum_eval, str):
        errors.append(minimum_eval)
        minimum_eval = 20_000
    reports = bundle.get("reports")
    if not isinstance(reports, dict):
        return [*errors, "reports must be an object"]
    for target in ("discard", "call", "riichi"):
        entry = reports.get(target)
        if not isinstance(entry, dict):
            errors.append(f"reports.{target} must be an object")
            continue
        errors.extend(
            _validate_report_entry(
                target,
                entry,
                repo_root=repo_root,
                minimum_train=minimum_train,
                minimum_eval=minimum_eval,
            )
        )
    return errors


def _validate_report_entry(
    target: str,
    entry: dict[str, Any],
    *,
    repo_root: Path,
    minimum_train: int,
    minimum_eval: int,
) -> list[str]:
    errors: list[str] = []
    path_value = entry.get("path")
    if not isinstance(path_value, str) or not path_value.strip():
        return [f"reports.{target}.path must be a non-empty string"]
    path = Path(path_value)
    if not path.is_file():
        return [f"reports.{target}.path does not exist: {path}"]
    errors.extend(_validate_ignored_path(path, repo_root=repo_root, field=f"reports.{target}.path"))
    try:
        report = _json_object(path)
    except ValueError as error:
        return [*errors, str(error)]
    expected_kind = REPORT_KINDS[target]
    if report.get("kind") != expected_kind:
        errors.append(f"reports.{target}.kind must be {expected_kind}")
    model_name = entry.get("model", DEFAULT_MODELS[target])
    if not isinstance(model_name, str) or not model_name.strip():
        errors.append(f"reports.{target}.model must be a non-empty string")
        model_name = DEFAULT_MODELS[target]
    errors.extend(
        _validate_report_payload(
            target,
            report,
            model_name=model_name,
            minimum_train=minimum_train,
            minimum_eval=minimum_eval,
        )
    )
    return errors


def _validate_report_payload(
    target: str,
    report: dict[str, Any],
    *,
    model_name: str,
    minimum_train: int,
    minimum_eval: int,
) -> list[str]:
    errors: list[str] = []
    split = report.get("split")
    if not isinstance(split, dict):
        return [f"{target} report split must be an object"]
    train_examples = split.get("train_examples")
    eval_examples = split.get("eval_examples")
    if not isinstance(train_examples, int) or train_examples < minimum_train:
        errors.append(
            f"{target} train_examples must be >= {minimum_train}, got {train_examples!r}"
        )
    if not isinstance(eval_examples, int) or eval_examples < minimum_eval:
        errors.append(f"{target} eval_examples must be >= {minimum_eval}, got {eval_examples!r}")
    source = report.get("source")
    if not isinstance(source, dict) or not isinstance(source.get("command"), str):
        errors.append(f"{target} report source.command must be recorded")
    models = report.get("models")
    if not isinstance(models, dict):
        return [*errors, f"{target} report models must be an object"]
    model = models.get(model_name)
    if not isinstance(model, dict):
        return [*errors, f"{target} report missing selected model {model_name!r}"]
    metrics = model.get("metrics")
    if not isinstance(metrics, dict):
        return [*errors, f"{target} selected model metrics must be an object"]
    for metric in REQUIRED_METRICS:
        if metric not in metrics:
            errors.append(f"{target} selected model missing metric {metric}")
    recall_key = f"eval_{target}_recall"
    if target in {"call", "riichi"} and recall_key not in metrics:
        errors.append(f"{target} selected model missing metric {recall_key}")
    return errors


def _positive_int(payload: dict[str, Any], key: str, *, default: int) -> int | str:
    value = payload.get(key, default)
    if not isinstance(value, int) or isinstance(value, bool) or value <= 0:
        return f"{key} must be a positive integer"
    return value


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
    parser = argparse.ArgumentParser(description="Validate TODO-102 BC report bundle.")
    parser.add_argument("bundle", type=Path, help="BC report bundle JSON")
    return parser


if __name__ == "__main__":
    raise SystemExit(main())
