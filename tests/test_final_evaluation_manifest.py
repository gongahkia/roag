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

    def test_rejects_tracked_metric_table(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            manifest = _write_manifest(Path(directory), table_path=Path("README.md"))
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(manifest)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1)
        self.assertIn("metric_tables[0].path is tracked by git: README.md", result.stderr)

    def test_rejects_untracked_evaluation_script(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            root = Path(directory)
            script = root / "generated-eval.py"
            script.write_text("print('unit')\n", encoding="utf-8")
            manifest = _write_manifest(root, evaluation_script_path=script)
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(manifest)],
                text=True,
                capture_output=True,
                check=False,
        )

        self.assertEqual(result.returncode, 1)
        self.assertIn(
            "frozen_inputs.evaluation_scripts[0].path is not tracked by git",
            result.stderr,
        )

    def test_rejects_empty_metric_table(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            root = Path(directory)
            table = root / "empty-metrics.json"
            table.write_text("", encoding="utf-8")
            manifest = _write_manifest(root, table_path=table)
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(manifest)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1)
        self.assertIn("metric_tables[0].path must not be empty", result.stderr)

    def test_rejects_dataset_directory_path(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            root = Path(directory)
            dataset_dir = root / "dataset-dir"
            dataset_dir.mkdir()
            manifest = _write_manifest(root, dataset_path=dataset_dir)
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(manifest)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1)
        self.assertIn(
            "frozen_inputs.dataset_slices[0].path must be an existing file",
            result.stderr,
        )


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
    table_path: Path | None = None,
    dataset_path: Path | None = None,
    evaluation_script_path: Path = SCRIPT,
) -> Path:
    paths = {
        "dataset": dataset_path or root / "slice-manifest.json",
        "checkpoint": root / "model.pt",
        "supervised": root / "supervised.json",
        "self_play": root / "self-play.json",
        "sanma": root / "sanma.json",
        "interpretability": root / "interpretability.html",
        "table": table_path or root / "metrics.json",
        "manifest": root / "manifest.json",
    }
    for key, path in paths.items():
        if key == "table" and table_path is not None:
            continue
        if key == "dataset" and dataset_path is not None:
            continue
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
                {"name": "validator", "path": str(evaluation_script_path)},
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
