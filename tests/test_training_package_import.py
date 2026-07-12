from __future__ import annotations

import os
import subprocess
import sys
import unittest


class TrainingPackageImportTests(unittest.TestCase):
    def test_base_training_import_does_not_import_torch(self) -> None:
        environment = os.environ | {"PYTHONPATH": "src"}
        result = subprocess.run(
            [
                sys.executable,
                "-c",
                "import sys; import kenjaku.training; print('torch' in sys.modules)",
            ],
            text=True,
            capture_output=True,
            check=False,
            env=environment,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.strip(), "False")

    def test_cli_import_does_not_import_torch(self) -> None:
        environment = os.environ | {"PYTHONPATH": "src"}
        result = subprocess.run(
            [
                sys.executable,
                "-c",
                "import sys; import kenjaku.cli; print('torch' in sys.modules)",
            ],
            text=True,
            capture_output=True,
            check=False,
            env=environment,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(result.stdout.strip(), "False")

    def test_ppo_exports_remain_available_lazily(self) -> None:
        from kenjaku import training

        self.assertEqual(training.PPO_ACTION_DIM, 276)


if __name__ == "__main__":
    unittest.main()
