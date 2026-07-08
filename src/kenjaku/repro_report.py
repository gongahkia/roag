from __future__ import annotations

import json
import platform
import subprocess
import sys
from collections.abc import Mapping, Sequence
from datetime import UTC, datetime
from pathlib import Path
from time import perf_counter
from typing import Any

from kenjaku import __version__

REPRO_REPORT_KIND = "kenjaku-repro-report-v0"

_REPORT_STARTED_AT = datetime.now(UTC)
_REPORT_STARTED_PERF = perf_counter()
_REPORT_ARGV: tuple[str, ...] = tuple(sys.argv)


def configure_report_provenance(argv: Sequence[str]) -> None:
    global _REPORT_STARTED_AT, _REPORT_STARTED_PERF, _REPORT_ARGV
    _REPORT_STARTED_AT = datetime.now(UTC)
    _REPORT_STARTED_PERF = perf_counter()
    _REPORT_ARGV = tuple(argv)


def build_report_provenance() -> dict[str, Any]:
    state = current_git_state()
    return {
        "git_commit": state["git_commit"],
        "git_dirty": state["git_dirty"],
        "kenjaku_version": __version__,
        "python_version": platform.python_version(),
        "argv": list(_REPORT_ARGV),
        "started_at": _REPORT_STARTED_AT.isoformat().replace("+00:00", "Z"),
        "duration_seconds": round(max(0.0, perf_counter() - _REPORT_STARTED_PERF), 6),
    }


def current_git_state(cwd: Path | None = None) -> dict[str, str | bool | None]:
    commit = _git_output(["rev-parse", "HEAD"], cwd=cwd)
    status = _git_output(["status", "--porcelain"], cwd=cwd)
    return {
        "git_commit": commit,
        "git_dirty": True if commit is None or status is None else bool(status),
    }


def build_repro_report(
    path: str | Path,
    *,
    current_state: Mapping[str, object] | None = None,
    current_version: str = __version__,
) -> dict[str, Any]:
    report_path = Path(path)
    payload = _read_json_object(report_path)
    provenance = payload.get("provenance")
    state = dict(current_git_state() if current_state is None else current_state)
    warnings: list[str] = []

    if not isinstance(provenance, dict):
        warnings.append("missing provenance block")
        provenance = None
    else:
        report_commit = provenance.get("git_commit")
        current_commit = state.get("git_commit")
        if current_commit is None:
            warnings.append("current git commit unavailable")
        elif report_commit != current_commit:
            warnings.append(
                f"git commit mismatch: report={report_commit!s} current={current_commit!s}"
            )

        if provenance.get("git_dirty") is True:
            warnings.append("report was produced from a dirty worktree")
        if state.get("git_dirty") is True:
            warnings.append("current worktree is dirty")

        report_version = provenance.get("kenjaku_version")
        if report_version != current_version:
            warnings.append(
                f"kenjaku version mismatch: report={report_version!s} current={current_version}"
            )

    ok = not warnings
    return {
        "kind": REPRO_REPORT_KIND,
        "path": str(report_path),
        "report_kind": payload.get("kind"),
        "ok": ok,
        "provenance_present": provenance is not None,
        "provenance": provenance,
        "current": {
            "git_commit": state.get("git_commit"),
            "git_dirty": bool(state.get("git_dirty")),
            "kenjaku_version": current_version,
        },
        "warnings": warnings,
        "strict_exit_code": 0 if ok else 1,
    }


def format_repro_report_text(report: Mapping[str, Any]) -> str:
    lines = [
        f"path: {report.get('path')}",
        f"report_kind: {report.get('report_kind')}",
        f"provenance: {'yes' if report.get('provenance_present') else 'no'}",
        f"status: {'ok' if report.get('ok') else 'warning'}",
    ]
    provenance = report.get("provenance")
    current = report.get("current")
    if isinstance(provenance, Mapping):
        lines.extend(
            [
                f"report_git_commit: {provenance.get('git_commit')}",
                f"report_git_dirty: {provenance.get('git_dirty')}",
                f"report_kenjaku_version: {provenance.get('kenjaku_version')}",
                f"report_started_at: {provenance.get('started_at')}",
                f"report_duration_seconds: {provenance.get('duration_seconds')}",
            ]
        )
    if isinstance(current, Mapping):
        lines.extend(
            [
                f"current_git_commit: {current.get('git_commit')}",
                f"current_git_dirty: {current.get('git_dirty')}",
                f"current_kenjaku_version: {current.get('kenjaku_version')}",
            ]
        )
    for warning in report.get("warnings", []):
        lines.append(f"warning: {warning}")
    return "\n".join(lines)


def _read_json_object(path: Path) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError("report must be a JSON object")
    return payload


def _git_output(args: Sequence[str], *, cwd: Path | None) -> str | None:
    try:
        result = subprocess.run(
            ["git", *args],
            cwd=cwd,
            check=True,
            capture_output=True,
            text=True,
        )
    except (OSError, subprocess.CalledProcessError):
        return None
    return result.stdout.strip()
