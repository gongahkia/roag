#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import sys
from collections.abc import Sequence
from pathlib import Path
from typing import Any

EVIDENCE_KIND = "kenjaku-cloud-gpu-dry-run-evidence-v0"
TRANSFORMER_REPORT_KIND = "kenjaku-discard-transformer-report-v0"
TRANSFORMER_BENCHMARK_REPORT_KIND = "kenjaku-discard-transformer-benchmark-report-v0"


def main(argv: Sequence[str] | None = None) -> int:
    args = _parser().parse_args(argv)
    try:
        evidence = _json_object(args.evidence)
        report = _json_object(Path(_required_path(evidence, ("artifacts", "report_path"))))
        errors = validate_evidence(evidence, report, evidence_path=args.evidence)
    except ValueError as error:
        print(str(error), file=sys.stderr)
        return 1
    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1
    provider = _required_str(evidence, ("provider", "name"))
    gpu_type = _required_str(evidence, ("provider", "gpu_type"))
    device = _report_device(report)
    report_path = _required_str(evidence, ("artifacts", "report_path"))
    print(
        "cloud gpu evidence ok: "
        f"provider={provider} gpu={gpu_type} device={device} report={report_path}"
    )
    return 0


def validate_evidence(
    evidence: dict[str, Any],
    report: dict[str, Any],
    *,
    evidence_path: Path,
) -> list[str]:
    errors: list[str] = []
    try:
        repo_root = _repo_root(evidence_path)
    except ValueError as error:
        return [str(error)]
    errors.extend(_validate_ignored_path(evidence_path, repo_root=repo_root, field="evidence path"))
    if evidence.get("kind") != EVIDENCE_KIND:
        errors.append(f"evidence kind must be {EVIDENCE_KIND}")
    for path in (
        ("provider", "name"),
        ("provider", "region"),
        ("provider", "gpu_type"),
        ("command",),
        ("artifacts", "report_path"),
        ("artifacts", "checkpoint_path"),
        ("benchmark_report_summary",),
    ):
        try:
            _required_str(evidence, path)
        except ValueError as error:
            errors.append(str(error))
    if not isinstance(evidence.get("git_status_short"), str):
        errors.append("git_status_short must be a string")
    for path in (
        ("runtime", "wall_clock_seconds"),
        ("runtime", "billed_seconds"),
        ("runtime", "cost_usd"),
    ):
        try:
            _required_nonnegative_number(evidence, path, positive=path[-1] != "cost_usd")
        except ValueError as error:
            errors.append(str(error))
    if "train-discard-transformer" not in str(evidence.get("command", "")):
        errors.append("command must include train-discard-transformer")
    report_path = Path(str(_nested(evidence, ("artifacts", "report_path"))))
    checkpoint_path = Path(str(_nested(evidence, ("artifacts", "checkpoint_path"))))
    errors.extend(
        _validate_report(
            report,
            evidence=evidence,
            evidence_path=evidence_path,
            report_path=report_path,
            checkpoint_path=checkpoint_path,
        )
    )
    errors.extend(
        _validate_ignored_artifacts((report_path, checkpoint_path), evidence_path=evidence_path)
    )
    status = str(evidence.get("git_status_short", ""))
    for path in (str(report_path), str(checkpoint_path)):
        if path and path in status:
            errors.append(f"git_status_short must not include generated artifact {path}")
    return errors


def _validate_report(
    report: dict[str, Any],
    *,
    evidence: dict[str, Any],
    evidence_path: Path,
    report_path: Path,
    checkpoint_path: Path,
) -> list[str]:
    errors: list[str] = []
    try:
        device = _report_device(report)
    except ValueError as error:
        errors.append(str(error))
    else:
        if not device.startswith("cuda"):
            errors.append(f"training device must start with cuda, got {device!r}")
    if not report_path.is_file():
        errors.append(f"report_path does not exist: {report_path}")
    if not checkpoint_path.is_file():
        errors.append(f"checkpoint_path does not exist: {checkpoint_path}")
    report_checkpoint = _nested(report, ("artifacts", "checkpoint_path"))
    if str(report_checkpoint) != str(checkpoint_path):
        errors.append(
            "report artifacts.checkpoint_path must match evidence checkpoint_path "
            f"({report_checkpoint!r} != {str(checkpoint_path)!r})"
        )
    source_command = _nested(report, ("source", "command"))
    if not isinstance(source_command, str) or not source_command.strip():
        errors.append("report source.command must be recorded")
    elif source_command != evidence.get("command"):
        errors.append("report source.command must match evidence command")
    errors.extend(_validate_ignored_input_paths(report, evidence_path=evidence_path))
    return errors


def _validate_ignored_input_paths(report: dict[str, Any], *, evidence_path: Path) -> list[str]:
    input_paths = report.get("input_paths")
    if not isinstance(input_paths, list) or not input_paths:
        return ["report input_paths must be a non-empty list"]
    try:
        repo_root = _repo_root(evidence_path)
    except ValueError as error:
        return [str(error)]
    errors: list[str] = []
    for value in input_paths:
        if not isinstance(value, str) or not value.strip():
            errors.append("report input_paths entries must be non-empty strings")
            continue
        path = Path(value)
        text = str(path)
        if "data/fixtures" in text or "fixtures/tenhou" in text:
            errors.append(f"report input_paths must not use checked-in fixtures: {text}")
            continue
        if not path.exists():
            errors.append(f"report input path does not exist: {path}")
        errors.extend(_validate_ignored_path(path, repo_root=repo_root, field="report input path"))
    return errors


def _validate_ignored_artifacts(paths: Sequence[Path], *, evidence_path: Path) -> list[str]:
    errors: list[str] = []
    try:
        repo_root = _repo_root(evidence_path)
    except ValueError as error:
        return [str(error)]
    for path in paths:
        errors.extend(_validate_ignored_path(path, repo_root=repo_root, field="artifact path"))
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


def _report_device(report: dict[str, Any]) -> str:
    kind = report.get("kind")
    if kind == TRANSFORMER_REPORT_KIND:
        return _required_str(report, ("training", "device"))
    if kind == TRANSFORMER_BENCHMARK_REPORT_KIND:
        return _required_str(report, ("models", "discard_transformer", "training", "device"))
    raise ValueError(
        "report kind must be "
        f"{TRANSFORMER_REPORT_KIND} or {TRANSFORMER_BENCHMARK_REPORT_KIND}"
    )


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


def _required_path(payload: dict[str, Any], path: Sequence[str]) -> str:
    value = _required_str(payload, path)
    if not Path(value).is_file():
        raise ValueError(f"{'.'.join(path)} does not exist: {value}")
    return value


def _required_str(payload: dict[str, Any], path: Sequence[str]) -> str:
    value = _nested(payload, path)
    if not isinstance(value, str) or not value.strip():
        raise ValueError(f"{'.'.join(path)} must be a non-empty string")
    return value


def _required_nonnegative_number(
    payload: dict[str, Any],
    path: Sequence[str],
    *,
    positive: bool,
) -> float:
    value = _nested(payload, path)
    if not isinstance(value, int | float) or isinstance(value, bool):
        raise ValueError(f"{'.'.join(path)} must be a number")
    if positive and value <= 0:
        raise ValueError(f"{'.'.join(path)} must be positive")
    if not positive and value < 0:
        raise ValueError(f"{'.'.join(path)} must be non-negative")
    return float(value)


def _nested(payload: dict[str, Any], path: Sequence[str]) -> Any:
    current: Any = payload
    for key in path:
        if not isinstance(current, dict) or key not in current:
            return None
        current = current[key]
    return current


def _repo_root(path: Path) -> Path:
    result = _git(["rev-parse", "--show-toplevel"], path.parent)
    if result.returncode != 0:
        raise ValueError("git repository root not found")
    return Path(result.stdout.strip()).resolve()


def _git(args: Sequence[str], cwd: Path) -> subprocess.CompletedProcess[str]:
    return subprocess.run(
        ["git", *args],
        cwd=cwd,
        text=True,
        capture_output=True,
        check=False,
    )


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Validate TODO-003 cloud GPU dry-run evidence."
    )
    parser.add_argument("evidence", type=Path, help="cloud GPU evidence JSON")
    return parser


if __name__ == "__main__":
    raise SystemExit(main())
