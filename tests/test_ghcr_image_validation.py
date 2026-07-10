from __future__ import annotations

import subprocess
import unittest
from collections.abc import Sequence

from scripts.validate_ghcr_image import validate_image


class GhcrImageValidationTests(unittest.TestCase):
    def test_accepts_multiarch_version_and_latest_images(self) -> None:
        runner = _FakeRunner()

        errors = validate_image("ghcr.io/gongahkia/kenjaku", "v0.2.0", runner=runner)

        self.assertEqual(errors, [])
        self.assertEqual(
            runner.commands,
            [
                ("docker", "buildx", "imagetools", "inspect", "ghcr.io/gongahkia/kenjaku:0.2.0"),
                ("docker", "pull", "ghcr.io/gongahkia/kenjaku:0.2.0"),
                ("docker", "run", "--rm", "ghcr.io/gongahkia/kenjaku:0.2.0"),
                ("docker", "pull", "ghcr.io/gongahkia/kenjaku:latest"),
                ("docker", "run", "--rm", "ghcr.io/gongahkia/kenjaku:latest"),
            ],
        )

    def test_rejects_missing_required_platform(self) -> None:
        runner = _FakeRunner(manifest_stdout="linux/amd64\n")

        errors = validate_image("ghcr.io/gongahkia/kenjaku", "0.2.0", runner=runner)

        self.assertIn("manifest missing linux/arm64", "\n".join(errors))
        self.assertEqual(
            runner.commands,
            [("docker", "buildx", "imagetools", "inspect", "ghcr.io/gongahkia/kenjaku:0.2.0")],
        )

    def test_rejects_wrong_runtime_version(self) -> None:
        runner = _FakeRunner(run_stdout="kenjaku 0.1.0\n")

        errors = validate_image("ghcr.io/gongahkia/kenjaku", "0.2.0", runner=runner)

        self.assertIn("expected 'kenjaku 0.2.0'", "\n".join(errors))


class _FakeRunner:
    def __init__(
        self,
        *,
        manifest_stdout: str = "linux/amd64\nlinux/arm64\n",
        run_stdout: str = "kenjaku 0.2.0\n",
    ) -> None:
        self.commands: list[tuple[str, ...]] = []
        self._manifest_stdout = manifest_stdout
        self._run_stdout = run_stdout

    def __call__(self, command: Sequence[str]) -> subprocess.CompletedProcess[str]:
        normalized = tuple(command)
        self.commands.append(normalized)
        if normalized[:4] == ("docker", "buildx", "imagetools", "inspect"):
            return subprocess.CompletedProcess(
                normalized,
                0,
                stdout=self._manifest_stdout,
                stderr="",
            )
        if normalized[:2] == ("docker", "pull"):
            return subprocess.CompletedProcess(normalized, 0, stdout="pulled\n", stderr="")
        if normalized[:3] == ("docker", "run", "--rm"):
            return subprocess.CompletedProcess(normalized, 0, stdout=self._run_stdout, stderr="")
        return subprocess.CompletedProcess(normalized, 127, stdout="", stderr="unexpected command")


if __name__ == "__main__":
    unittest.main()
