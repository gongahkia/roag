from __future__ import annotations

import hashlib
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

SCRIPT = Path(__file__).parents[1] / "scripts" / "verify_browser_model_assets.py"


class VerifyBrowserModelAssetsTests(unittest.TestCase):
    def test_accepts_empty_and_hashed_asset_manifests(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            site_dir = Path(directory)
            self._write_manifest(site_dir, [])
            self.assertEqual(self._verify(site_dir).returncode, 0)

            asset = site_dir / "models" / "policy.onnx"
            asset.write_bytes(b"browser model fixture")
            self._write_manifest(
                site_dir,
                [
                    {
                        "path": "models/policy.onnx",
                        "sha256": hashlib.sha256(asset.read_bytes()).hexdigest(),
                        "bytes": asset.stat().st_size,
                    }
                ],
            )

            result = self._verify(site_dir)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("verified 1 browser model assets", result.stdout)

    def test_rejects_tampering_and_unsafe_paths(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            site_dir = Path(directory)
            asset = site_dir / "models" / "policy.onnx"
            asset.parent.mkdir(parents=True)
            asset.write_bytes(b"original")
            self._write_manifest(
                site_dir,
                [
                    {
                        "path": "models/policy.onnx",
                        "sha256": hashlib.sha256(b"original").hexdigest(),
                        "bytes": len(b"original"),
                    }
                ],
            )
            asset.write_bytes(b"tampered")

            result = self._verify(site_dir)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("sha256 differs", result.stderr)

            self._write_manifest(
                site_dir,
                [
                    {
                        "path": "../policy.onnx",
                        "sha256": hashlib.sha256(b"original").hexdigest(),
                        "bytes": len(b"original"),
                    }
                ],
            )
            result = self._verify(site_dir)
            self.assertNotEqual(result.returncode, 0)
            self.assertIn("path is unsafe", result.stderr)

    def _verify(self, site_dir: Path) -> subprocess.CompletedProcess[str]:
        return subprocess.run(
            [sys.executable, str(SCRIPT), str(site_dir)],
            check=False,
            text=True,
            capture_output=True,
        )

    def _write_manifest(self, site_dir: Path, assets: list[dict[str, object]]) -> None:
        manifest = site_dir / "models" / "manifest.json"
        manifest.parent.mkdir(parents=True, exist_ok=True)
        manifest.write_text(
            json.dumps({"kind": "kenjaku-browser-model-assets-v1", "assets": assets}),
            encoding="utf-8",
        )


if __name__ == "__main__":
    unittest.main()
