from __future__ import annotations

import json
import subprocess
import unittest
from collections.abc import Sequence

from scripts.diagnose_actions_blocker import classify_run, diagnose_run


class ActionsBlockerDiagnosisTests(unittest.TestCase):
    def test_classifies_all_failed_empty_step_jobs_as_runner_block(self) -> None:
        payload = {
            "conclusion": "failure",
            "jobs": [
                {"name": "test", "conclusion": "failure", "steps": []},
                {"name": "lint", "conclusion": "failure", "steps": []},
            ],
        }

        result = classify_run(payload)

        self.assertEqual(result["kind"], "zero-step-runner-block")
        self.assertEqual(result["failed_zero_step_jobs"], ["test", "lint"])

    def test_classifies_step_failure_separately(self) -> None:
        payload = {
            "conclusion": "failure",
            "jobs": [{"name": "test", "conclusion": "failure", "steps": [{"name": "Run tests"}]}],
        }

        result = classify_run(payload)

        self.assertEqual(result["kind"], "step-level-or-other-failure")

    def test_diagnose_run_collects_repo_action_settings(self) -> None:
        runner = _FakeRunner()

        report = diagnose_run("123", repo="owner/repo", billing_user="owner", runner=runner)

        self.assertEqual(report["classification"]["kind"], "zero-step-runner-block")
        self.assertTrue(report["actions_permissions"]["ok"])
        self.assertTrue(report["workflow_permissions"]["ok"])
        self.assertTrue(report["selected_actions"]["ok"])
        self.assertFalse(report["billing"]["actions"]["ok"])
        self.assertEqual(
            runner.commands,
            [
                (
                    "gh",
                    "run",
                    "view",
                    "123",
                    "--repo",
                    "owner/repo",
                    "--json",
                    "jobs,conclusion,status,url",
                ),
                ("gh", "api", "/repos/owner/repo/actions/permissions"),
                ("gh", "api", "/repos/owner/repo/actions/permissions/workflow"),
                ("gh", "api", "/repos/owner/repo/actions/permissions/selected-actions"),
                ("gh", "api", "/users/owner/settings/billing/actions"),
                ("gh", "api", "/users/owner/settings/billing/packages"),
            ],
        )


class _FakeRunner:
    def __init__(self) -> None:
        self.commands: list[tuple[str, ...]] = []

    def __call__(self, command: Sequence[str]) -> subprocess.CompletedProcess[str]:
        normalized = tuple(command)
        self.commands.append(normalized)
        if normalized[:3] == ("gh", "run", "view"):
            return _json_result(
                {
                    "conclusion": "failure",
                    "status": "completed",
                    "url": "https://example.test/run",
                    "jobs": [{"name": "publish", "conclusion": "failure", "steps": []}],
                }
            )
        if normalized == ("gh", "api", "/repos/owner/repo/actions/permissions"):
            return _json_result({"enabled": True, "allowed_actions": "all"})
        if normalized == ("gh", "api", "/repos/owner/repo/actions/permissions/workflow"):
            return _json_result({"default_workflow_permissions": "read"})
        if normalized == ("gh", "api", "/repos/owner/repo/actions/permissions/selected-actions"):
            return _json_result({"message": "Conflict", "status": "409"}, returncode=1)
        if normalized[:3] == ("gh", "api", "/users/owner/settings/billing/actions"):
            return _json_result({"message": "Not Found", "status": "404"}, returncode=1)
        if normalized[:3] == ("gh", "api", "/users/owner/settings/billing/packages"):
            return _json_result({"message": "Not Found", "status": "404"}, returncode=1)
        return subprocess.CompletedProcess(normalized, 127, stdout="", stderr="unexpected command")


def _json_result(payload: object, *, returncode: int = 0) -> subprocess.CompletedProcess[str]:
    return subprocess.CompletedProcess((), returncode, stdout=json.dumps(payload), stderr="")


if __name__ == "__main__":
    unittest.main()
