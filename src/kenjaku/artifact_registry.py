"""Local-only registry for reports, checkpoints, and ONNX exports."""

from __future__ import annotations

import hashlib
import json
import os
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from pathlib import Path, PurePosixPath
from typing import Any, Literal, Self

from kenjaku.schema import CheckpointManifestV1

LOCAL_ARTIFACT_REGISTRY_V1_KIND = "kenjaku-local-artifact-registry-v1"
LOCAL_ARTIFACT_REGISTRY_V1_FIELDS = ("kind", "artifacts")
LOCAL_ARTIFACT_RECORD_V1_FIELDS = (
    "artifact_id",
    "artifact_type",
    "path",
    "sha256",
    "size_bytes",
    "checkpoint_manifest",
)
ArtifactType = Literal["checkpoint", "report", "onnx"]
ARTIFACT_TYPES: tuple[ArtifactType, ...] = ("checkpoint", "report", "onnx")
_MANIFEST_ARTIFACT_TYPES = frozenset({"checkpoint", "onnx"})


@dataclass(frozen=True, slots=True)
class LocalArtifactRecordV1:
    """Immutable local-file metadata, optionally bound to a checkpoint manifest."""

    artifact_id: str
    artifact_type: ArtifactType
    path: str
    sha256: str
    size_bytes: int
    checkpoint_manifest: CheckpointManifestV1 | None = None

    def __post_init__(self) -> None:
        _validate_non_empty_str(self.artifact_id, "artifact_id")
        if self.artifact_type not in ARTIFACT_TYPES:
            raise ValueError("unsupported artifact_type: " + self.artifact_type)
        _validate_relative_path(self.path)
        if not isinstance(self.sha256, str) or len(self.sha256) != 64:
            raise ValueError("sha256 must be a lowercase SHA-256 digest")
        if any(character not in "0123456789abcdef" for character in self.sha256):
            raise ValueError("sha256 must be a lowercase SHA-256 digest")
        if (
            isinstance(self.size_bytes, bool)
            or not isinstance(self.size_bytes, int)
            or self.size_bytes < 0
        ):
            raise ValueError("size_bytes must be a non-negative integer")
        if self.artifact_type in _MANIFEST_ARTIFACT_TYPES:
            if not isinstance(self.checkpoint_manifest, CheckpointManifestV1):
                raise ValueError(f"{self.artifact_type} artifacts require a checkpoint_manifest")
        elif self.checkpoint_manifest is not None:
            raise ValueError("report artifacts cannot include a checkpoint_manifest")
        _validate_artifact_suffix(self.artifact_type, self.path)

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> Self:
        _require_exact_fields(payload, LOCAL_ARTIFACT_RECORD_V1_FIELDS, "LocalArtifactRecordV1")
        manifest = payload["checkpoint_manifest"]
        if manifest is not None and not isinstance(manifest, Mapping):
            raise ValueError("checkpoint_manifest must be an object or null")
        artifact_type = _read_str(payload, "artifact_type")
        if artifact_type not in ARTIFACT_TYPES:
            raise ValueError("unsupported artifact_type: " + artifact_type)
        return cls(
            artifact_id=_read_str(payload, "artifact_id"),
            artifact_type=artifact_type,
            path=_read_str(payload, "path"),
            sha256=_read_str(payload, "sha256"),
            size_bytes=_read_int(payload, "size_bytes"),
            checkpoint_manifest=(
                None if manifest is None else CheckpointManifestV1.from_dict(manifest)
            ),
        )

    def to_dict(self) -> dict[str, Any]:
        return {
            "artifact_id": self.artifact_id,
            "artifact_type": self.artifact_type,
            "path": self.path,
            "sha256": self.sha256,
            "size_bytes": self.size_bytes,
            "checkpoint_manifest": (
                None if self.checkpoint_manifest is None else self.checkpoint_manifest.to_dict()
            ),
        }


@dataclass(frozen=True, slots=True)
class LocalArtifactRegistryV1:
    """Strict serializable registry whose paths are relative to its local root."""

    artifacts: tuple[LocalArtifactRecordV1, ...] = ()

    def __post_init__(self) -> None:
        if not isinstance(self.artifacts, tuple):
            raise ValueError("artifacts must be an immutable tuple")
        if any(not isinstance(artifact, LocalArtifactRecordV1) for artifact in self.artifacts):
            raise ValueError("artifacts must contain LocalArtifactRecordV1 values")
        identifiers = tuple(artifact.artifact_id for artifact in self.artifacts)
        if len(set(identifiers)) != len(identifiers):
            raise ValueError("artifact_id values must be unique")
        if tuple(sorted(identifiers)) != identifiers:
            raise ValueError("artifacts must be ordered by artifact_id")

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> Self:
        _require_exact_fields(
            payload,
            LOCAL_ARTIFACT_REGISTRY_V1_FIELDS,
            "LocalArtifactRegistryV1",
        )
        if _read_str(payload, "kind") != LOCAL_ARTIFACT_REGISTRY_V1_KIND:
            raise ValueError(
                f"LocalArtifactRegistryV1 kind must be {LOCAL_ARTIFACT_REGISTRY_V1_KIND}"
            )
        values = payload["artifacts"]
        if isinstance(values, (str, bytes)) or not isinstance(values, Sequence):
            raise ValueError("artifacts must be an array")
        artifacts: list[LocalArtifactRecordV1] = []
        for value in values:
            if not isinstance(value, Mapping):
                raise ValueError("artifacts entries must be objects")
            artifacts.append(LocalArtifactRecordV1.from_dict(value))
        return cls(artifacts=tuple(artifacts))

    def record(self, artifact_id: str) -> LocalArtifactRecordV1 | None:
        """Return an artifact by ID without reading its local file."""
        for artifact in self.artifacts:
            if artifact.artifact_id == artifact_id:
                return artifact
        return None

    def with_record(self, record: LocalArtifactRecordV1) -> Self:
        """Replace one record by ID and return a deterministically ordered registry."""
        replaced = [
            artifact for artifact in self.artifacts if artifact.artifact_id != record.artifact_id
        ]
        replaced.append(record)
        return LocalArtifactRegistryV1(
            artifacts=tuple(sorted(replaced, key=lambda item: item.artifact_id))
        )

    def to_dict(self) -> dict[str, Any]:
        return {
            "kind": LOCAL_ARTIFACT_REGISTRY_V1_KIND,
            "artifacts": [artifact.to_dict() for artifact in self.artifacts],
        }

    def to_json(self, *, indent: int | None = 2) -> str:
        return json.dumps(self.to_dict(), indent=indent, sort_keys=True) + "\n"


class LocalArtifactRegistry:
    """Read, update, and verify a registry without accepting remote artifact locations."""

    def __init__(
        self,
        root: str | Path,
        *,
        registry_name: str = "artifact-registry.json",
    ) -> None:
        self.root = Path(root).resolve()
        self.root.mkdir(parents=True, exist_ok=True)
        _validate_relative_path(registry_name)
        self.path = self.root / registry_name
        self._registry = self._load()

    @property
    def registry(self) -> LocalArtifactRegistryV1:
        """Return the current immutable registry snapshot."""
        return self._registry

    def register_file(
        self,
        *,
        artifact_id: str,
        artifact_type: ArtifactType,
        path: str | Path,
        checkpoint_manifest: CheckpointManifestV1 | None = None,
    ) -> LocalArtifactRecordV1:
        """Hash one existing local file and add or replace its immutable metadata record."""
        source = self._resolve_registered_file(path)
        relative_path = source.relative_to(self.root).as_posix()
        record = LocalArtifactRecordV1(
            artifact_id=artifact_id,
            artifact_type=artifact_type,
            path=relative_path,
            sha256=_sha256(source),
            size_bytes=source.stat().st_size,
            checkpoint_manifest=checkpoint_manifest,
        )
        self._registry = self._registry.with_record(record)
        return record

    def verify(self) -> tuple[str, ...]:
        """Return deterministic integrity errors for all registered local files."""
        errors: list[str] = []
        for record in self._registry.artifacts:
            try:
                source = self._resolve_registered_file(record.path)
            except (FileNotFoundError, ValueError) as error:
                errors.append(f"{record.artifact_id}: {error}")
                continue
            size_bytes = source.stat().st_size
            if size_bytes != record.size_bytes:
                errors.append(f"{record.artifact_id}: size_bytes differs")
                continue
            if _sha256(source) != record.sha256:
                errors.append(f"{record.artifact_id}: sha256 differs")
        return tuple(errors)

    def require_verified(self) -> None:
        """Raise when any registered local artifact is missing or has changed."""
        errors = self.verify()
        if errors:
            raise ValueError("artifact registry verification failed: " + "; ".join(errors))

    def save(self) -> Path:
        """Atomically persist the registry under its configured local root."""
        self.path.parent.mkdir(parents=True, exist_ok=True)
        temporary = self.path.with_name(self.path.name + ".tmp")
        temporary.write_text(self._registry.to_json(), encoding="utf-8")
        os.replace(temporary, self.path)
        return self.path

    def _load(self) -> LocalArtifactRegistryV1:
        if not self.path.exists():
            return LocalArtifactRegistryV1()
        if not self.path.is_file():
            raise ValueError(f"registry path is not a file: {self.path}")
        try:
            payload = json.loads(self.path.read_text(encoding="utf-8"))
        except json.JSONDecodeError as error:
            raise ValueError(f"invalid local artifact registry JSON: {self.path}") from error
        if not isinstance(payload, Mapping):
            raise ValueError("local artifact registry must be an object")
        return LocalArtifactRegistryV1.from_dict(payload)

    def _resolve_registered_file(self, path: str | Path) -> Path:
        if isinstance(path, str) and "://" in path:
            raise ValueError("path must be local and relative")
        candidate = Path(path)
        if candidate.is_absolute():
            resolved = candidate.resolve(strict=True)
        else:
            _validate_relative_path(candidate.as_posix())
            resolved = (self.root / candidate).resolve(strict=True)
        try:
            resolved.relative_to(self.root)
        except ValueError as error:
            raise ValueError("artifact path must remain under registry root") from error
        if not resolved.is_file():
            raise ValueError("artifact path must be a regular file")
        return resolved


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _validate_relative_path(value: str) -> None:
    if not isinstance(value, str) or not value:
        raise ValueError("path must be a non-empty relative path")
    if "://" in value:
        raise ValueError("path must be local and relative")
    path = PurePosixPath(value)
    if path.is_absolute() or value in {".", ".."} or ".." in path.parts:
        raise ValueError("path must be a non-traversing relative path")


def _validate_artifact_suffix(artifact_type: ArtifactType, path: str) -> None:
    suffix = PurePosixPath(path).suffix.lower()
    if artifact_type == "report" and suffix != ".json":
        raise ValueError("report artifacts must use a .json path")
    if artifact_type == "onnx" and suffix != ".onnx":
        raise ValueError("onnx artifacts must use a .onnx path")


def _require_exact_fields(payload: Mapping[str, Any], fields: Sequence[str], name: str) -> None:
    actual = set(payload)
    expected = set(fields)
    if actual == expected:
        return
    missing = sorted(expected - actual)
    unexpected = sorted(actual - expected)
    details = []
    if missing:
        details.append("missing=" + ",".join(missing))
    if unexpected:
        details.append("unexpected=" + ",".join(unexpected))
    raise ValueError(f"{name} fields must match v1 schema: {'; '.join(details)}")


def _read_str(payload: Mapping[str, Any], field: str) -> str:
    value = payload[field]
    if not isinstance(value, str):
        raise ValueError(f"{field} must be a string")
    return value


def _read_int(payload: Mapping[str, Any], field: str) -> int:
    value = payload[field]
    if isinstance(value, bool) or not isinstance(value, int):
        raise ValueError(f"{field} must be an integer")
    return value


def _validate_non_empty_str(value: Any, field: str) -> None:
    if not isinstance(value, str) or not value:
        raise ValueError(f"{field} must be a non-empty string")
