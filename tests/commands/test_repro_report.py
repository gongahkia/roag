from __future__ import annotations

from kenjaku.repro_report import build_repro_report
from tests.commands.conftest import (
    CliCommandTests,
    Path,
    TemporaryDirectory,
    contextlib,
    io,
    json,
    main,
)


class ReproReportCommandTests(CliCommandTests):
    def test_repro_report_accepts_matching_clean_provenance(self) -> None:
        with TemporaryDirectory() as directory:
            path = Path(directory) / "report.json"
            path.write_text(
                json.dumps(
                    {
                        "kind": "unit-report",
                        "provenance": {
                            "git_commit": "abc123",
                            "git_dirty": False,
                            "kenjaku_version": "0.test",
                            "python_version": "3.test",
                            "argv": ["kenjaku", "demo"],
                            "started_at": "2026-07-08T00:00:00Z",
                            "duration_seconds": 0.01,
                        },
                    }
                ),
                encoding="utf-8",
            )

            report = build_repro_report(
                path,
                current_state={"git_commit": "abc123", "git_dirty": False},
                current_version="0.test",
            )

        self.assertTrue(report["ok"])
        self.assertEqual(report["warnings"], [])
        self.assertEqual(report["strict_exit_code"], 0)

    def test_repro_report_flags_mismatched_commit_and_dirty_report(self) -> None:
        with TemporaryDirectory() as directory:
            path = Path(directory) / "report.json"
            path.write_text(
                json.dumps(
                    {
                        "kind": "unit-report",
                        "provenance": {
                            "git_commit": "old",
                            "git_dirty": True,
                            "kenjaku_version": "0.old",
                            "python_version": "3.test",
                            "argv": ["kenjaku", "demo"],
                            "started_at": "2026-07-08T00:00:00Z",
                            "duration_seconds": 0.01,
                        },
                    }
                ),
                encoding="utf-8",
            )

            report = build_repro_report(
                path,
                current_state={"git_commit": "new", "git_dirty": False},
                current_version="0.new",
            )

        self.assertFalse(report["ok"])
        self.assertIn("git commit mismatch: report=old current=new", report["warnings"])
        self.assertIn("report was produced from a dirty worktree", report["warnings"])
        self.assertIn("kenjaku version mismatch: report=0.old current=0.new", report["warnings"])
        self.assertEqual(report["strict_exit_code"], 1)

    def test_repro_report_command_warns_and_strict_fails_without_provenance(self) -> None:
        with TemporaryDirectory() as directory:
            path = Path(directory) / "report.json"
            path.write_text(json.dumps({"kind": "unit-report"}), encoding="utf-8")

            stdout = io.StringIO()
            with contextlib.redirect_stdout(stdout):
                exit_code = main(["repro-report", str(path)])

            strict_stdout = io.StringIO()
            with contextlib.redirect_stdout(strict_stdout):
                strict_exit_code = main(["repro-report", str(path), "--strict"])

        self.assertEqual(exit_code, 0)
        self.assertIn("provenance: no", stdout.getvalue())
        self.assertIn("warning: missing provenance block", stdout.getvalue())
        self.assertEqual(strict_exit_code, 1)
        self.assertIn("warning: missing provenance block", strict_stdout.getvalue())
