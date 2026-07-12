from __future__ import annotations

import subprocess
import sys
import unittest
from pathlib import Path
from unittest.mock import patch

from scripts import run_with_memory_guard

SCRIPT = Path("scripts/run_with_memory_guard.py")


class MemoryGuardTests(unittest.TestCase):
    def test_allows_command_under_limit(self) -> None:
        result = subprocess.run(
            [
                sys.executable,
                str(SCRIPT),
                "--max-rss-mb",
                "512",
                "--poll-interval",
                "0.05",
                "--",
                sys.executable,
                "-c",
                "print('ok')",
            ],
            text=True,
            capture_output=True,
            check=False,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.strip(), "ok")
        self.assertIn("peak_memory_mb=", result.stderr)

    def test_kills_command_over_limit(self) -> None:
        result = subprocess.run(
            [
                sys.executable,
                str(SCRIPT),
                "--max-rss-mb",
                "1",
                "--poll-interval",
                "0.05",
                "--grace-seconds",
                "0.5",
                "--",
                sys.executable,
                "-c",
                "import time; _ = bytearray(8 * 1024 * 1024); time.sleep(5)",
            ],
            text=True,
            capture_output=True,
            check=False,
        )

        self.assertEqual(result.returncode, 137, result.stderr)
        self.assertIn("memory guard exceeded:", result.stderr)

    def test_macos_guard_sums_physical_footprints(self) -> None:
        with (
            patch.object(run_with_memory_guard.sys, "platform", "darwin"),
            patch.object(run_with_memory_guard, "_process_group_pids", return_value=(10, 11)),
            patch.object(
                run_with_memory_guard,
                "_macos_physical_footprint_kib",
                side_effect=(1024, 2048),
            ),
        ):
            memory_kib = run_with_memory_guard._process_group_memory_kib(10)

        self.assertEqual(memory_kib, 3072)

    def test_parses_macos_physical_footprint(self) -> None:
        self.assertEqual(
            run_with_memory_guard._parse_macos_physical_footprint_kib(
                "Physical footprint:         1.5G\n"
            ),
            1572864,
        )


if __name__ == "__main__":
    unittest.main()
