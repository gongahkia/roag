from __future__ import annotations

import json
import subprocess
import sys
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

SCRIPT = Path("scripts/validate_external_baseline_evidence.py")


class ExternalBaselineEvidenceTests(unittest.TestCase):
    def test_accepts_real_mortal_compatible_row(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            report = _write_report(Path(directory), name="mortal-local-v1")
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(report)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("external baseline evidence ok:", result.stdout)

    def test_rejects_smoke_only_mortal_compatible_row(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            report = _write_report(Path(directory), name="pass-smoke")
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(report)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1)
        self.assertIn("only has smoke/stub rows", result.stderr)


def _runs_dir() -> Path:
    path = Path("runs/todo-103")
    path.mkdir(parents=True, exist_ok=True)
    return path


def _write_report(root: Path, *, name: str) -> Path:
    report = root / "external-baseline-report.json"
    report.write_text(
        json.dumps(
            {
                "kind": "kenjaku-external-baseline-report-v0",
                "baselines": [
                    {
                        "family": "mortal-compatible",
                        "name": name,
                        "predictions_path": f"runs/todo-103/{name}.jsonl",
                        "comparable_decisions": 1000,
                        "missing_predictions": 0,
                        "illegal_predictions": 0,
                        "malformed_prediction_rows": 0,
                        "duplicate_prediction_rows": 0,
                        "by_decision_type": {
                            "discard": {"examples": 1000, "accuracy": 0.5}
                        },
                        "binary": {
                            "call": {"balanced_accuracy": 0.5},
                            "riichi": {"balanced_accuracy": 0.5},
                        },
                    }
                ],
            },
            sort_keys=True,
        ),
        encoding="utf-8",
    )
    return report


if __name__ == "__main__":
    unittest.main()
