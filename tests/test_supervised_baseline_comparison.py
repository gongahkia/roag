from __future__ import annotations

import json
import subprocess
import sys
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

SCRIPT = Path("scripts/validate_supervised_baseline_comparison.py")
SUMMARY = Path("docs/supervised-baseline-comparison.md")


class SupervisedBaselineComparisonTests(unittest.TestCase):
    def test_accepts_complete_comparison_bundle(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            bundle = _write_bundle(Path(directory))
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(bundle)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("supervised baseline comparison ok:", result.stdout)

    def test_rejects_label_that_does_not_match_metrics(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            bundle = _write_bundle(Path(directory), discard_label="missed")
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(bundle)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1)
        self.assertIn("targets.discard.label must be exceeded", result.stderr)

    def test_rejects_missing_seed_row(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            bundle = _write_bundle(Path(directory), omit_seed=True)
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(bundle)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1)
        self.assertIn("seeds must contain exactly 3 seed rows", result.stderr)

    def test_rejects_wrong_primary_metric(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            bundle = _write_bundle(
                Path(directory),
                target_overrides={"call": {"primary_metric": "eval_accuracy"}},
            )
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(bundle)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1)
        self.assertIn(
            "targets.call.primary_metric must be one of: eval_balanced_accuracy",
            result.stderr,
        )

    def test_rejects_empty_seed_artifact(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            root = Path(directory)
            bundle = _write_bundle(root, empty_seed_artifact=True)
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(bundle)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1)
        self.assertIn("targets.discard.kenjaku.seeds[0].path must not be empty", result.stderr)


def _runs_dir() -> Path:
    path = Path("runs/todo-104")
    path.mkdir(parents=True, exist_ok=True)
    return path


def _write_bundle(
    root: Path,
    *,
    discard_label: str = "exceeded",
    omit_seed: bool = False,
    empty_seed_artifact: bool = False,
    target_overrides: dict[str, dict[str, object]] | None = None,
) -> Path:
    inputs = {
        "bc_manifest": _write_artifact(root / "bc-manifest.json"),
        "shared_snapshots": _write_artifact(root / "shared-snapshots.jsonl"),
        "baseline_report": _write_baseline_report(root / "external-baseline-report.json"),
    }
    targets = {
        "discard": _target(
            root,
            "discard",
            baseline=0.5,
            values=(0.50, 0.51, 0.52),
            direction="higher",
            label=discard_label,
            guards={
                "comparable_decisions": 1000,
                "missing_predictions": 0,
                "illegal_predictions": 0,
            },
            omit_seed=omit_seed,
            empty_seed_artifact=empty_seed_artifact,
        ),
        "call": _target(
            root,
            "call",
            baseline=0.8,
            values=(0.79, 0.80, 0.805),
            direction="higher",
            label="matched",
            ci95=(0.79, 0.81),
            guards={"pass_recall": 0.7, "call_recall": 0.8},
        ),
        "riichi": _target(
            root,
            "riichi",
            baseline=0.8,
            values=(0.70, 0.71, 0.72),
            direction="higher",
            label="missed",
            guards={"pass_recall": 0.7, "riichi_recall": 0.8},
        ),
        "deal_in": _target(
            root,
            "deal_in",
            baseline=0.1,
            values=(0.101, 0.100, 0.099),
            direction="lower",
            label="matched",
            guards={
                "balanced_accuracy": 0.6,
                "recall": 0.5,
                "specificity": 0.7,
                "heuristic_brier_delta": -0.01,
                "heuristic_log_loss_delta": -0.02,
            },
        ),
    }
    if target_overrides:
        for target, overrides in target_overrides.items():
            targets[target].update(overrides)
    bundle = root / "supervised-baseline-comparison.json"
    bundle.write_text(
        json.dumps(
            {
                "kind": "kenjaku-supervised-baseline-comparison-v0",
                "checked_in_summary_path": str(SUMMARY),
                "inputs": {
                    "bc_manifest": {
                        "path": str(inputs["bc_manifest"]),
                        "source_command": "unit export bc",
                    },
                    "shared_snapshots": {
                        "path": str(inputs["shared_snapshots"]),
                        "source_command": "unit export snapshots",
                    },
                    "baseline_report": {
                        "path": str(inputs["baseline_report"]),
                        "family": "mortal-compatible",
                        "row_name": "mortal-local-v1",
                    },
                },
                "targets": targets,
            },
            sort_keys=True,
        ),
        encoding="utf-8",
    )
    return bundle


def _target(
    root: Path,
    name: str,
    *,
    baseline: float,
    values: tuple[float, float, float],
    direction: str,
    label: str,
    guards: dict[str, float | int],
    ci95: tuple[float, float] | None = None,
    omit_seed: bool = False,
    empty_seed_artifact: bool = False,
) -> dict[str, object]:
    seeds = []
    for index, value in enumerate(values):
        path = _write_artifact(
            root / f"{name}-s{index}.json",
            empty=empty_seed_artifact and index == 0,
        )
        seeds.append(
            {
                "seed_id": f"todo-104-s{index}",
                "path": str(path),
                "value": value,
            }
        )
    if omit_seed:
        seeds.pop()
    baseline_payload: dict[str, object] = {"value": baseline}
    if ci95 is not None:
        baseline_payload["ci95"] = list(ci95)
    best = max(values) if direction == "higher" else min(values)
    return {
        "primary_metric": _primary_metric(name),
        "direction": direction,
        "baseline": baseline_payload,
        "kenjaku": {
            "mean": sum(values) / len(values),
            "best": best,
            "seeds": seeds,
        },
        "label": label,
        "guards": guards,
    }


def _primary_metric(name: str) -> str:
    if name in {"call", "riichi"}:
        return "eval_balanced_accuracy"
    if name == "deal_in":
        return "eval_brier_score"
    return "eval_accuracy"


def _write_artifact(path: Path, *, empty: bool = False) -> Path:
    path.write_text("" if empty else "{}\n", encoding="utf-8")
    return path


def _write_baseline_report(path: Path) -> Path:
    path.write_text(
        json.dumps(
            {
                "kind": "kenjaku-external-baseline-report-v0",
                "baselines": [
                    {
                        "family": "mortal-compatible",
                        "name": "mortal-local-v1",
                    }
                ],
            },
            sort_keys=True,
        ),
        encoding="utf-8",
    )
    return path


if __name__ == "__main__":
    unittest.main()
