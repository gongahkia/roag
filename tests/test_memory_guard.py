from __future__ import annotations

import subprocess
import sys
import unittest
from pathlib import Path

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
        self.assertIn("peak_rss_mb=", result.stderr)

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


if __name__ == "__main__":
    unittest.main()
