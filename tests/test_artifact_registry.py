from __future__ import annotations

import json
import unittest
from pathlib import Path
from tempfile import TemporaryDirectory

from kenjaku.artifact_registry import (
    LOCAL_ARTIFACT_REGISTRY_V1_KIND,
    LocalArtifactRegistry,
    LocalArtifactRegistryV1,
)
from kenjaku.schema import CheckpointManifestV1


class LocalArtifactRegistryTests(unittest.TestCase):
    def test_registers_saves_loads_and_verifies_local_artifacts(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            checkpoint = _write(root / "checkpoints" / "policy.pt", b"checkpoint")
            report = _write(root / "reports" / "run.json", b'{"kind":"report"}')
            onnx = _write(root / "exports" / "policy.onnx", b"onnx")
            registry = LocalArtifactRegistry(root)
            manifest = _manifest()

            registry.register_file(
                artifact_id="checkpoint-policy",
                artifact_type="checkpoint",
                path=checkpoint,
                checkpoint_manifest=manifest,
            )
            registry.register_file(
                artifact_id="report-run",
                artifact_type="report",
                path=report,
            )
            registry.register_file(
                artifact_id="onnx-policy",
                artifact_type="onnx",
                path=onnx,
                checkpoint_manifest=manifest,
            )
            path = registry.save()
            loaded = LocalArtifactRegistry(root)

            self.assertTrue(path.exists())
            self.assertEqual(registry.registry, loaded.registry)
            self.assertEqual(loaded.verify(), ())
            payload = json.loads(path.read_text(encoding="utf-8"))
            self.assertEqual(payload["kind"], LOCAL_ARTIFACT_REGISTRY_V1_KIND)
            self.assertEqual(
                [item["artifact_id"] for item in payload["artifacts"]],
                [
                    "checkpoint-policy",
                    "onnx-policy",
                    "report-run",
                ],
            )

    def test_detects_modified_files_and_rejects_paths_outside_root(self) -> None:
        with TemporaryDirectory() as directory, TemporaryDirectory() as outside_directory:
            root = Path(directory)
            report = _write(root / "reports" / "run.json", b"original")
            outside = _write(Path(outside_directory) / "outside.json", b"outside")
            registry = LocalArtifactRegistry(root)
            registry.register_file(artifact_id="report-run", artifact_type="report", path=report)
            report.write_bytes(b"altered!")

            self.assertEqual(registry.verify(), ("report-run: sha256 differs",))
            with self.assertRaisesRegex(ValueError, "verification failed"):
                registry.require_verified()
            with self.assertRaisesRegex(ValueError, "under registry root"):
                registry.register_file(
                    artifact_id="outside",
                    artifact_type="report",
                    path=outside,
                )
            with self.assertRaisesRegex(ValueError, "local and relative"):
                registry.register_file(
                    artifact_id="remote",
                    artifact_type="report",
                    path="https://example.test/report.json",
                )

    def test_enforces_manifest_binding_suffixes_and_strict_schema(self) -> None:
        with TemporaryDirectory() as directory:
            root = Path(directory)
            checkpoint = _write(root / "checkpoint.pt", b"checkpoint")
            onnx = _write(root / "policy.onnx", b"onnx")
            registry = LocalArtifactRegistry(root)

            with self.assertRaisesRegex(ValueError, "require a checkpoint_manifest"):
                registry.register_file(
                    artifact_id="checkpoint",
                    artifact_type="checkpoint",
                    path=checkpoint,
                )
            with self.assertRaisesRegex(ValueError, "report artifacts must use"):
                registry.register_file(
                    artifact_id="wrong-suffix",
                    artifact_type="report",
                    path=checkpoint,
                )
            registry.register_file(
                artifact_id="onnx",
                artifact_type="onnx",
                path=onnx,
                checkpoint_manifest=_manifest(),
            )
            payload = registry.registry.to_dict()
            payload["extra"] = True

            with self.assertRaisesRegex(ValueError, "unexpected=extra"):
                LocalArtifactRegistryV1.from_dict(payload)


def _manifest() -> CheckpointManifestV1:
    return CheckpointManifestV1.for_current_schemas(
        checkpoint_id="fixture-policy",
        model_kind="sandbox-policy",
        model_version="1.0.0",
        rulesets=("tenhou-4p",),
    )


def _write(path: Path, payload: bytes) -> Path:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(payload)
    return path


if __name__ == "__main__":
    unittest.main()
