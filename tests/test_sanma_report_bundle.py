from __future__ import annotations

import json
import subprocess
import sys
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

SCRIPT = Path("scripts/validate_sanma_report_bundle.py")


class SanmaReportBundleTests(unittest.TestCase):
    def test_accepts_complete_report_bundle(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            bundle = _write_bundle(Path(directory))
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(bundle)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("sanma report bundle ok:", result.stdout)

    def test_rejects_synthetic_fixture_source(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            bundle = _write_bundle(Path(directory), source_label="synthetic-fixture")
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(bundle)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1)
        self.assertIn("source.label must not be synthetic or fixture", result.stderr)

    def test_rejects_missing_input_path(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            root = Path(directory)
            bundle = _write_bundle(root, input_paths=(str(root / "missing-sanma-slice"),))
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(bundle)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1)
        self.assertIn("report input path does not exist", result.stderr)

    def test_rejects_nonnumeric_metric(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            bundle = _write_bundle(Path(directory), metric_overrides={"eval_accuracy": "0.6"})
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(bundle)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1)
        self.assertIn("selected model metric eval_accuracy must be a number", result.stderr)

    def test_rejects_non_ignored_bundle_path(self) -> None:
        with TemporaryDirectory(dir=Path(".")) as directory:
            bundle = _write_bundle(Path(directory))
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(bundle)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1)
        self.assertIn("bundle path is not ignored by git", result.stderr)


def _runs_dir() -> Path:
    path = Path("runs/todo-403")
    path.mkdir(parents=True, exist_ok=True)
    return path


def _write_bundle(
    root: Path,
    *,
    source_label: str = "real-sanma-local",
    input_paths: tuple[str, ...] | None = None,
    metric_overrides: dict[str, object] | None = None,
) -> Path:
    if input_paths is None:
        input_slice = root / "input-slice"
        input_slice.mkdir()
        input_paths = (str(input_slice),)
    reports = {
        "discard": root / "discard.json",
        "call": root / "call.json",
        "riichi": root / "riichi.json",
        "kita": root / "kita.json",
    }
    model_names = {
        "discard": "defense_context_linear",
        "call": "call_linear_v1_calibrated",
        "riichi": "riichi_linear_calibrated",
        "kita": "kita_frequency",
    }
    kinds = {
        "discard": "kenjaku-discard-benchmark-report-v0",
        "call": "kenjaku-call-benchmark-report-v0",
        "riichi": "kenjaku-riichi-benchmark-report-v0",
        "kita": "kenjaku-kita-benchmark-report-v0",
    }
    for target, path in reports.items():
        _write_report(
            path,
            kind=kinds[target],
            model_name=model_names[target],
            target=target,
            source_label=source_label,
            input_paths=input_paths,
            metric_overrides=metric_overrides,
        )
    bundle = root / "sanma-report-bundle.json"
    bundle.write_text(
        json.dumps(
            {
                "kind": "kenjaku-sanma-report-bundle-v0",
                "minimum_xml_files": 1000,
                "minimum_eval_decisions": 1,
                "reports": {
                    target: {"path": str(path)}
                    for target, path in reports.items()
                },
            },
            sort_keys=True,
        ),
        encoding="utf-8",
    )
    return bundle


def _write_report(
    path: Path,
    *,
    kind: str,
    model_name: str,
    target: str,
    source_label: str,
    input_paths: tuple[str, ...],
    metric_overrides: dict[str, object] | None,
) -> None:
    metrics = {
        "train_loss": 0.2,
        "eval_loss": 0.3,
        "train_accuracy": 0.7,
        "eval_accuracy": 0.6,
        "train_balanced_accuracy": 0.7,
        "eval_balanced_accuracy": 0.6,
        "train_action_recall": {},
        "eval_action_recall": {},
    }
    if metric_overrides:
        metrics.update(metric_overrides)
    if target in {"call", "riichi", "kita"}:
        metrics[f"eval_{target}_recall"] = 0.5
    path.write_text(
        json.dumps(
            {
                "kind": kind,
                "source": {
                    "label": source_label,
                    "command": f"unit {target} command",
                },
                "input_paths": list(input_paths),
                "xml_file_count": 1000,
                "split": {
                    "seed": "unit",
                    "train_examples": 10,
                    "eval_examples": 1,
                    "eval_fraction": 0.1,
                },
                "models": {model_name: {"kind": model_name, "metrics": metrics}},
            },
            sort_keys=True,
        ),
        encoding="utf-8",
    )


if __name__ == "__main__":
    unittest.main()
