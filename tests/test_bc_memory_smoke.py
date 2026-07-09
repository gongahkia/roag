from __future__ import annotations

import json
import subprocess
import sys
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

SCRIPT = Path("scripts/run_bc_memory_smoke.py")


class BcMemorySmokeTests(unittest.TestCase):
    def test_fixture_smoke_writes_manifest_under_guard(self) -> None:
        with TemporaryDirectory() as directory:
            output_dir = Path(directory) / "bc-smoke"
            result = subprocess.run(
                [
                    sys.executable,
                    str(SCRIPT),
                    "data/fixtures/tenhou",
                    "--output-dir",
                    str(output_dir),
                    "--limit-per-type",
                    "8",
                    "--max-rss-mb",
                    "512",
                    "--poll-interval",
                    "0.05",
                    "--overwrite",
                ],
                text=True,
                capture_output=True,
                check=False,
            )
            manifest = json.loads((output_dir / "manifest.json").read_text(encoding="utf-8"))

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("bc_memory_smoke_command=", result.stderr)
        self.assertIn("peak_rss_mb=", result.stderr)
        self.assertEqual(manifest["kind"], "kenjaku-bc-example-manifest-v0")
        self.assertGreater(sum(manifest["decision_counts"].values()), 0)


if __name__ == "__main__":
    unittest.main()
