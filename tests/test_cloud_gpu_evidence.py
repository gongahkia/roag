from __future__ import annotations

import json
import subprocess
import sys
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

SCRIPT = Path("scripts/validate_cloud_gpu_evidence.py")


class CloudGpuEvidenceTests(unittest.TestCase):
    def test_accepts_cuda_transformer_evidence(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            root = Path(directory)
            evidence = _write_case(root, device="cuda:0")
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(evidence)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("cloud gpu evidence ok:", result.stdout)
        self.assertIn("device=cuda:0", result.stdout)

    def test_rejects_local_mps_transformer_evidence(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            root = Path(directory)
            evidence = _write_case(root, device="mps")
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(evidence)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1)
        self.assertIn("training device must start with cuda", result.stderr)

    def test_rejects_checked_in_fixture_input(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            root = Path(directory)
            evidence = _write_case(root, input_path="data/fixtures/tenhou")
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(evidence)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1)
        self.assertIn("input_paths must not use checked-in fixtures", result.stderr)

    def test_rejects_mismatched_report_command(self) -> None:
        with TemporaryDirectory(dir=_runs_dir()) as directory:
            root = Path(directory)
            evidence = _write_case(
                root,
                report_command="python -m kenjaku train-discard-transformer other",
            )
            result = subprocess.run(
                [sys.executable, str(SCRIPT), str(evidence)],
                text=True,
                capture_output=True,
                check=False,
            )

        self.assertEqual(result.returncode, 1)
        self.assertIn("report source.command must match evidence command", result.stderr)


def _runs_dir() -> Path:
    path = Path("runs/todo-003")
    path.mkdir(parents=True, exist_ok=True)
    return path


def _write_case(
    root: Path,
    *,
    device: str = "cuda:0",
    input_path: str | None = None,
    report_command: str | None = None,
) -> Path:
    report = root / "cloud-fixture-discard-transformer.json"
    checkpoint = root / "cloud-fixture-discard-transformer.pt"
    evidence = root / "cloud-gpu-evidence.json"
    if input_path is None:
        input_dir = root / "cloud-input-slice"
        input_dir.mkdir()
        input_path = str(input_dir)
    command = (
        f"python -m kenjaku train-discard-transformer {input_path} "
        "--device auto --checkpoint "
        f"{checkpoint} --report {report}"
    )
    checkpoint.write_text("checkpoint placeholder\n", encoding="utf-8")
    report.write_text(
        json.dumps(
            {
                "kind": "kenjaku-discard-transformer-report-v0",
                "input_paths": [input_path],
                "source": {
                    "label": "unit-cloud-gpu",
                    "date": "2026-07-09",
                    "command": report_command or command,
                },
                "training": {"device": device},
                "artifacts": {"checkpoint_path": str(checkpoint)},
            },
            sort_keys=True,
        ),
        encoding="utf-8",
    )
    evidence.write_text(
        json.dumps(
            {
                "kind": "kenjaku-cloud-gpu-dry-run-evidence-v0",
                "provider": {
                    "name": "unit-provider",
                    "region": "unit-region",
                    "gpu_type": "NVIDIA Test GPU",
                },
                "runtime": {
                    "wall_clock_seconds": 12.5,
                    "billed_seconds": 60.0,
                    "cost_usd": 0.01,
                },
                "command": command,
                "artifacts": {
                    "report_path": str(report),
                    "checkpoint_path": str(checkpoint),
                },
                "benchmark_report_summary": "device: cuda:0",
                "git_status_short": "",
            },
            sort_keys=True,
        ),
        encoding="utf-8",
    )
    return evidence


if __name__ == "__main__":
    unittest.main()
