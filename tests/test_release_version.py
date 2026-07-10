from __future__ import annotations

import subprocess
import sys
import unittest
from pathlib import Path


class ReleaseVersionTests(unittest.TestCase):
    def test_release_tag_matches_pyproject_version(self) -> None:
        version = _pyproject_version()
        result = subprocess.run(
            [sys.executable, "scripts/validate_release_version.py", f"v{version}"],
            check=False,
            capture_output=True,
            text=True,
        )

        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("release version ok", result.stdout)

    def test_release_tag_mismatch_fails(self) -> None:
        result = subprocess.run(
            [sys.executable, "scripts/validate_release_version.py", "v0.0.0"],
            check=False,
            capture_output=True,
            text=True,
        )

        self.assertEqual(result.returncode, 1)
        self.assertIn("does not match pyproject version", result.stderr)

    def test_publish_workflows_validate_release_tag(self) -> None:
        for path in (Path(".github/workflows/release.yml"), Path(".github/workflows/docker.yml")):
            with self.subTest(path=path):
                contents = path.read_text(encoding="utf-8")
                self.assertIn("scripts/validate_release_version.py", contents)

    def test_docker_workflow_can_publish_without_release_tag(self) -> None:
        contents = Path(".github/workflows/docker.yml").read_text(encoding="utf-8")

        self.assertIn("workflow_dispatch:", contents)
        self.assertIn("version:", contents)
        self.assertIn('version="${INPUT_VERSION#v}"', contents)
        self.assertIn("type=raw,value=${{ steps.version.outputs.version }}", contents)

    def test_docker_workflow_verifies_published_image(self) -> None:
        contents = Path(".github/workflows/docker.yml").read_text(encoding="utf-8")

        self.assertIn("platforms: linux/amd64,linux/arm64", contents)
        self.assertIn("scripts/validate_ghcr_image.py", contents)


def _pyproject_version() -> str:
    for line in Path("pyproject.toml").read_text(encoding="utf-8").splitlines():
        if line.startswith("version = "):
            return line.split('"', 2)[1]
    raise AssertionError("pyproject version not found")


if __name__ == "__main__":
    unittest.main()
