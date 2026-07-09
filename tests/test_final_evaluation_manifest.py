from __future__ import annotations

import json
import subprocess
import sys
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

SCRIPT = Path("scripts/validate_final_evaluation_manifest.py")


class FinalEvaluationManifestTests(unittest.TestCase):
    def test_accepts_complete_manifest(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            manifest = _write_manifest(Path(directory))
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(manifest)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("final evaluation manifest ok:", result.stdout)

    def test_rejects_missing_required_table_metric(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            manifest = _write_manifest(Path(directory), table_metrics=("accuracy",))
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(manifest)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1)
        self.assertIn("metric_tables missing required metrics", result.stderr)
        self.assertIn("score_delta", result.stderr)


def _runs_dir() -> Path:
    path = Path("runs/todo-601")
    path.mkdir(parents=True, exist_ok=True)
    return path


def _write_manifest(
    root: Path,
    *,
    table_metrics: tuple[str, ...] = (
        "accuracy",
        "balanced_accuracy",
        "deal_in_calibration",
        "average_placement",
        "score_delta",
        "ablations",
    ),
) -> Path:
    paths = {
        "dataset": root / "slice-manifest.json",
        "checkpoint": root / "model.pt",
        "supervised": root / "supervised.json",
        "self_play": root / "self-play.json",
        "sanma": root / "sanma.json",
        "interpretability": root / "interpretability.html",
        "table": root / "metrics.json",
        "manifest": root / "manifest.json",
    }
    for path in paths.values():
        path.write_text("{}\n", encoding="utf-8")
    payload = {
        "kind": "kenjaku-final-evaluation-manifest-v0",
        "frozen_inputs": {
            "dataset_slices": [
                {
                    "name": "unit-slice",
                    "path": str(paths["dataset"]),
                    "source_command": "unit export command",
                }
            ],
            "model_checkpoints": [{"name": "unit-model", "path": str(paths["checkpoint"])}],
            "evaluation_scripts": [
                {"name": "validator", "path": str(SCRIPT)},
            ],
        },
        "evaluations": {
            "supervised": {
                "reports": [
                    {
                        "name": "supervised",
                        "path": str(paths["supervised"]),
                        "command": "unit supervised command",
                    }
                ]
            },
            "self_play": {
                "reports": [
                    {
                        "name": "self-play",
                        "path": str(paths["self_play"]),
                        "command": "unit self-play command",
                    }
                ]
            },
            "sanma": {
                "reports": [
                    {
                        "name": "sanma",
                        "path": str(paths["sanma"]),
                        "command": "unit sanma command",
                    }
                ]
            },
            "interpretability": {
                "reports": [
                    {
                        "name": "interpretability",
                        "path": str(paths["interpretability"]),
                        "command": "unit interpretability command",
                    }
                ]
            },
        },
        "metric_tables": [
            {
                "name": "summary",
                "path": str(paths["table"]),
                "metrics": list(table_metrics),
            }
        ],
        "commands": [
            {"name": "supervised", "command": "unit supervised command"},
            {"name": "self-play", "command": "unit self-play command"},
            {"name": "sanma", "command": "unit sanma command"},
            {"name": "interpretability", "command": "unit interpretability command"},
        ],
    }
    paths["manifest"].write_text(json.dumps(payload, sort_keys=True), encoding="utf-8")
    return paths["manifest"]


if __name__ == "__main__":
    unittest.main()
