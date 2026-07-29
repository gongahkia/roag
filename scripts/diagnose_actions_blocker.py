from __future__ import annotations

import argparse
import json
import subprocess
from collections.abc import Callable, Sequence
from typing import Any

Runner = Callable[[Sequence[str]], subprocess.CompletedProcess[str]]


def _run_command(command: Sequence[str]) -> subprocess.CompletedProcess[str]:
    return subprocess.run(list(command), check=False, capture_output=True, text=True)


def diagnose_run(
    run_id: str,
    *,
    repo: str = "gongahkia/kenjaku",
    billing_user: str | None = None,
    runner: Runner = _run_command,
) -> dict[str, Any]:
    run_result = runner(
        (
            "gh",
            "run",
            "view",
            run_id,
            "--repo",
            repo,
            "--json",
            "jobs,conclusion,status,url",
        )
    )
    permissions_result = runner(("gh", "api", f"/repos/{repo}/actions/permissions"))
    workflow_result = runner(("gh", "api", f"/repos/{repo}/actions/permissions/workflow"))
    selected_actions_result = runner(
        ("gh", "api", f"/repos/{repo}/actions/permissions/selected-actions")
    )

    report: dict[str, Any] = {
        "run_id": run_id,
        "repo": repo,
        "run": _json_command(run_result),
        "actions_permissions": _json_command(permissions_result),
        "workflow_permissions": _json_command(workflow_result),
        "selected_actions": _json_command(selected_actions_result, conflict_ok=True),
    }
    if report["run"]["ok"]:
        report["classification"] = classify_run(report["run"]["json"])
    else:
        report["classification"] = {
            "kind": "run-query-failed",
            "reason": report["run"]["stderr"] or report["run"]["stdout"],
        }

    if billing_user is not None:
        report["billing"] = {
            "actions": _json_command(
                runner(("gh", "api", f"/users/{billing_user}/settings/billing/actions"))
            ),
            "packages": _json_command(
                runner(("gh", "api", f"/users/{billing_user}/settings/billing/packages"))
            ),
        }
    return report


def classify_run(payload: dict[str, Any]) -> dict[str, Any]:
    jobs = payload.get("jobs")
    if not isinstance(jobs, list):
        return {"kind": "unknown", "reason": "run payload has no jobs list"}

    failed_zero_step = [
        _job_name(job)
        for job in jobs
        if isinstance(job, dict) and job.get("conclusion") == "failure" and job.get("steps") == []
    ]
    failed_jobs = [
        _job_name(job)
        for job in jobs
        if isinstance(job, dict) and job.get("conclusion") == "failure"
    ]
    if jobs and len(failed_zero_step) == len(jobs):
        return {
            "kind": "zero-step-runner-block",
            "failed_zero_step_jobs": failed_zero_step,
            "reason": "all jobs failed before any step records were created",
        }
    if failed_zero_step:
        return {
            "kind": "partial-zero-step-runner-block",
            "failed_zero_step_jobs": failed_zero_step,
            "failed_jobs": failed_jobs,
        }
    if payload.get("conclusion") == "success":
        return {"kind": "success"}
    return {"kind": "step-level-or-other-failure", "failed_jobs": failed_jobs}


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="Diagnose GitHub Actions zero-step failures.")
    parser.add_argument("run_id", help="GitHub Actions run id")
    parser.add_argument("--repo", default="gongahkia/kenjaku", help="owner/repo")
    parser.add_argument("--billing-user", help="include user billing API probes")
    args = parser.parse_args(argv)

    report = diagnose_run(args.run_id, repo=args.repo, billing_user=args.billing_user)
    print(json.dumps(report, indent=2, sort_keys=True))
    return 1 if report["classification"]["kind"].endswith("block") else 0


def _json_command(
    result: subprocess.CompletedProcess[str],
    *,
    conflict_ok: bool = False,
) -> dict[str, Any]:
    payload = _parse_json(result.stdout)
    ok = result.returncode == 0 or (
        conflict_ok and result.returncode == 1 and _status(payload) == "409"
    )
    return {
        "ok": ok,
        "returncode": result.returncode,
        "json": payload,
        "stdout": result.stdout.strip(),
        "stderr": result.stderr.strip(),
    }


def _parse_json(text: str) -> Any:
    try:
        return json.loads(text)
    except json.JSONDecodeError:
        return None


def _status(payload: Any) -> str | None:
    if isinstance(payload, dict) and payload.get("status") is not None:
        return str(payload["status"])
    return None


def _job_name(job: dict[str, Any]) -> str:
    name = job.get("name")
    return str(name) if name is not None else "<unnamed>"


if __name__ == "__main__":
    raise SystemExit(main())
