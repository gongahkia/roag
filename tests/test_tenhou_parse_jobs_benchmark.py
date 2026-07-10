from __future__ import annotations

import json
import subprocess
import sys
import unittest
from pathlib import Path

SCRIPT = Path("scripts/benchmark_tenhou_parse_jobs.py")


class TenhouParseJobsBenchmarkTests(unittest.TestCase):
    def test_script_writes_benchmark_payload(self) -> None:
        result = subprocess.run(
            [
                sys.executable,
                str(SCRIPT),
                "--files",
                "2",
                "--ignored-tags-per-file",
                "1",
                "--runs",
                "1",
                "--min-speedup",
                "0",
            ],
            check=False,
            capture_output=True,
            text=True,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        payload = json.loads(result.stdout)
        self.assertEqual(payload["kind"], "kenjaku-tenhou-parse-jobs-benchmark-v0")
        self.assertEqual(payload["files"], 2)
        self.assertEqual(payload["parallel"]["jobs"], 4)


if __name__ == "__main__":
    unittest.main()
