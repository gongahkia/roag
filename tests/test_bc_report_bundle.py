from __future__ import annotations

import json
import subprocess
import sys
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

SCRIPT = Path("scripts/validate_bc_report_bundle.py")


class BcReportBundleTests(unittest.TestCase):
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
        self.assertIn("bc report bundle ok:", result.stdout)

    def test_rejects_report_below_size_gate(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            bundle = _write_bundle(Path(directory), train_examples=99_999)
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(bundle)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1)
        self.assertIn("discard train_examples must be >= 100000", result.stderr)


def _runs_dir() -> Path:
    path = Path("runs/todo-102")
    path.mkdir(parents=True, exist_ok=True)
    return path


def _write_bundle(
    root: Path,
    *,
    train_examples: int = 100_000,
    eval_examples: int = 20_000,
) -> Path:
    reports = {
        "discard": root / "discard.json",
        "call": root / "call.json",
        "riichi": root / "riichi.json",
    }
    _write_report(
        reports["discard"],
        kind="kenjaku-discard-benchmark-report-v0",
        model_name="defense_context_linear",
        target="discard",
        train_examples=train_examples,
        eval_examples=eval_examples,
    )
    _write_report(
        reports["call"],
        kind="kenjaku-call-benchmark-report-v0",
        model_name="call_linear_v1_calibrated",
        target="call",
        train_examples=100_000,
        eval_examples=20_000,
    )
    _write_report(
        reports["riichi"],
        kind="kenjaku-riichi-benchmark-report-v0",
        model_name="riichi_linear_calibrated",
        target="riichi",
        train_examples=100_000,
        eval_examples=20_000,
    )
    bundle = root / "bc-report-bundle.json"
    bundle.write_text(
        json.dumps(
            {
                "kind": "kenjaku-bc-report-bundle-v0",
                "minimum_train_decisions": 100_000,
                "minimum_eval_decisions": 20_000,
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
    train_examples: int,
    eval_examples: int,
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
    if target in {"call", "riichi"}:
        metrics[f"eval_{target}_recall"] = 0.5
    path.write_text(
        json.dumps(
            {
                "kind": kind,
                "source": {"command": f"unit {target} command"},
                "split": {
                    "seed": "unit",
                    "train_examples": train_examples,
                    "eval_examples": eval_examples,
                    "eval_fraction": 0.16,
                },
                "models": {model_name: {"kind": model_name, "metrics": metrics}},
            },
            sort_keys=True,
        ),
        encoding="utf-8",
    )


if __name__ == "__main__":
    unittest.main()
