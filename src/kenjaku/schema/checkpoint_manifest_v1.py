"""Checkpoint manifests, schema compatibility, and Semantic Versioning validation."""

from __future__ import annotations

import json
import re
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from functools import total_ordering
from typing import Any, Self

from kenjaku.core import TENHOU_3P, TENHOU_4P
from kenjaku.schema.action_v1 import ACTION_V1_KIND
from kenjaku.schema.decision_result_v1 import DECISION_RESULT_V1_KIND
from kenjaku.schema.legal_action_mask_v1 import (
    LEGAL_ACTION_MASK_V1_DIM,
    LEGAL_ACTION_MASK_V1_KIND,
)
from kenjaku.schema.observation_v1 import OBSERVATION_V1_KIND

CHECKPOINT_MANIFEST_V1_KIND = "kenjaku-checkpoint-manifest-v1"
CHECKPOINT_MANIFEST_V1_FIELDS = (
    "kind",
    "checkpoint_id",
    "model_kind",
    "model_version",
    "rulesets",
    "compatibility",
)
CHECKPOINT_COMPATIBILITY_V1_FIELDS = (
    "observation_kind",
    "action_kind",
    "legal_action_mask_kind",
    "legal_action_mask_dim",
    "decision_result_kind",
)
_RULESETS = frozenset({TENHOU_4P.name, TENHOU_3P.name})
_SEMVER = re.compile(
    r"^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)"
    r"(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?"
    r"(?:\+([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?$"
)
_IDENTIFIER = re.compile(r"[0-9A-Za-z-]+")


@total_ordering
@dataclass(frozen=True, slots=True, eq=False)
class SemanticVersion:
    """A strict Semantic Versioning 2.0.0 value."""

    major: int
    minor: int
    patch: int
    prerelease: tuple[str, ...] = ()
    build: tuple[str, ...] = ()

    def __post_init__(self) -> None:
        for field, value in (
            ("major", self.major),
            ("minor", self.minor),
            ("patch", self.patch),
        ):
            if isinstance(value, bool) or not isinstance(value, int) or value < 0:
                raise ValueError(f"{field} must be a non-negative integer")
        if not isinstance(self.prerelease, tuple) or not isinstance(self.build, tuple):
            raise ValueError("prerelease and build must be immutable tuples")
        _validate_identifiers(self.prerelease, field="prerelease", forbid_numeric_leading_zero=True)
        _validate_identifiers(self.build, field="build", forbid_numeric_leading_zero=False)

    @classmethod
    def parse(cls, value: str) -> Self:
        """Parse one exact Semantic Versioning 2.0.0 string."""
        if not isinstance(value, str):
            raise ValueError("semantic version must be a string")
        match = _SEMVER.fullmatch(value)
        if match is None:
            raise ValueError(f"invalid semantic version: {value!r}")
        prerelease = () if match.group(4) is None else tuple(match.group(4).split("."))
        build = () if match.group(5) is None else tuple(match.group(5).split("."))
        try:
            return cls(
                major=int(match.group(1)),
                minor=int(match.group(2)),
                patch=int(match.group(3)),
                prerelease=prerelease,
                build=build,
            )
        except ValueError as error:
            raise ValueError(f"invalid semantic version: {value!r}") from error

    def __str__(self) -> str:
        value = f"{self.major}.{self.minor}.{self.patch}"
        if self.prerelease:
            value += "-" + ".".join(self.prerelease)
        if self.build:
            value += "+" + ".".join(self.build)
        return value

    def __lt__(self, other: object) -> bool:
        if not isinstance(other, SemanticVersion):
            return NotImplemented
        return self.compare_precedence(other) < 0

    def __eq__(self, other: object) -> bool:
        if not isinstance(other, SemanticVersion):
            return NotImplemented
        return self.compare_precedence(other) == 0

    def __hash__(self) -> int:
        return hash((self.major, self.minor, self.patch, self.prerelease))

    def compare_precedence(self, other: SemanticVersion) -> int:
        """Compare SemVer precedence, ignoring build metadata."""
        for left, right in zip(
            (self.major, self.minor, self.patch),
            (other.major, other.minor, other.patch),
            strict=True,
        ):
            if left != right:
                return -1 if left < right else 1
        if not self.prerelease and not other.prerelease:
            return 0
        if not self.prerelease:
            return 1
        if not other.prerelease:
            return -1
        for left, right in zip(self.prerelease, other.prerelease, strict=False):
            comparison = _compare_prerelease_identifier(left, right)
            if comparison:
                return comparison
        if len(self.prerelease) == len(other.prerelease):
            return 0
        return -1 if len(self.prerelease) < len(other.prerelease) else 1

    def has_same_precedence(self, other: SemanticVersion) -> bool:
        """Return whether this version differs only in build metadata."""
        return self.compare_precedence(other) == 0


@dataclass(frozen=True, slots=True)
class CheckpointCompatibilityV1:
    """The exact public schemas and fixed action-mask dimension a checkpoint expects."""

    observation_kind: str
    action_kind: str
    legal_action_mask_kind: str
    legal_action_mask_dim: int
    decision_result_kind: str

    def __post_init__(self) -> None:
        for field, value in (
            ("observation_kind", self.observation_kind),
            ("action_kind", self.action_kind),
            ("legal_action_mask_kind", self.legal_action_mask_kind),
            ("decision_result_kind", self.decision_result_kind),
        ):
            _validate_non_empty_str(value, field)
        if (
            isinstance(self.legal_action_mask_dim, bool)
            or not isinstance(self.legal_action_mask_dim, int)
            or self.legal_action_mask_dim <= 0
        ):
            raise ValueError("legal_action_mask_dim must be a positive integer")

    @classmethod
    def current(cls) -> Self:
        """Return compatibility requirements for the current v1 public schemas."""
        return cls(
            observation_kind=OBSERVATION_V1_KIND,
            action_kind=ACTION_V1_KIND,
            legal_action_mask_kind=LEGAL_ACTION_MASK_V1_KIND,
            legal_action_mask_dim=LEGAL_ACTION_MASK_V1_DIM,
            decision_result_kind=DECISION_RESULT_V1_KIND,
        )

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> Self:
        _require_exact_fields(
            payload,
            CHECKPOINT_COMPATIBILITY_V1_FIELDS,
            "CheckpointCompatibilityV1",
        )
        return cls(
            observation_kind=_read_str(payload, "observation_kind"),
            action_kind=_read_str(payload, "action_kind"),
            legal_action_mask_kind=_read_str(payload, "legal_action_mask_kind"),
            legal_action_mask_dim=_read_int(payload, "legal_action_mask_dim"),
            decision_result_kind=_read_str(payload, "decision_result_kind"),
        )

    def to_dict(self) -> dict[str, Any]:
        return {
            "observation_kind": self.observation_kind,
            "action_kind": self.action_kind,
            "legal_action_mask_kind": self.legal_action_mask_kind,
            "legal_action_mask_dim": self.legal_action_mask_dim,
            "decision_result_kind": self.decision_result_kind,
        }


@dataclass(frozen=True, slots=True)
class CheckpointRequirementV1:
    """A consumer's required model version, ruleset, and public-schema contract."""

    ruleset: str
    minimum_model_version: SemanticVersion
    compatibility: CheckpointCompatibilityV1
    model_kind: str | None = None

    def __post_init__(self) -> None:
        if not isinstance(self.ruleset, str) or self.ruleset not in _RULESETS:
            raise ValueError(f"unsupported checkpoint requirement ruleset: {self.ruleset!r}")
        if not isinstance(self.minimum_model_version, SemanticVersion):
            raise ValueError("minimum_model_version must be a SemanticVersion")
        if not isinstance(self.compatibility, CheckpointCompatibilityV1):
            raise ValueError("compatibility must be a CheckpointCompatibilityV1")
        if self.model_kind is not None:
            _validate_non_empty_str(self.model_kind, "model_kind")


@dataclass(frozen=True, slots=True)
class CheckpointManifestV1:
    """A versioned checkpoint identity and explicit inference compatibility declaration."""

    checkpoint_id: str
    model_kind: str
    model_version: SemanticVersion
    rulesets: tuple[str, ...]
    compatibility: CheckpointCompatibilityV1

    def __post_init__(self) -> None:
        _validate_non_empty_str(self.checkpoint_id, "checkpoint_id")
        _validate_non_empty_str(self.model_kind, "model_kind")
        if not isinstance(self.model_version, SemanticVersion):
            raise ValueError("model_version must be a SemanticVersion")
        if not isinstance(self.rulesets, tuple) or not self.rulesets:
            raise ValueError("rulesets must be a non-empty immutable tuple")
        if any(
            not isinstance(ruleset, str) or ruleset not in _RULESETS
            for ruleset in self.rulesets
        ):
            raise ValueError("rulesets must contain supported rulesets")
        if len(set(self.rulesets)) != len(self.rulesets):
            raise ValueError("rulesets must not contain duplicates")
        if not isinstance(self.compatibility, CheckpointCompatibilityV1):
            raise ValueError("compatibility must be a CheckpointCompatibilityV1")

    @classmethod
    def for_current_schemas(
        cls,
        *,
        checkpoint_id: str,
        model_kind: str,
        model_version: SemanticVersion | str,
        rulesets: Sequence[str],
    ) -> Self:
        """Create a manifest pinned to currently supported v1 public schemas."""
        parsed_version = (
            SemanticVersion.parse(model_version)
            if isinstance(model_version, str)
            else model_version
        )
        return cls(
            checkpoint_id=checkpoint_id,
            model_kind=model_kind,
            model_version=parsed_version,
            rulesets=tuple(rulesets),
            compatibility=CheckpointCompatibilityV1.current(),
        )

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> Self:
        _require_exact_fields(payload, CHECKPOINT_MANIFEST_V1_FIELDS, "CheckpointManifestV1")
        if _read_str(payload, "kind") != CHECKPOINT_MANIFEST_V1_KIND:
            raise ValueError(f"CheckpointManifestV1 kind must be {CHECKPOINT_MANIFEST_V1_KIND}")
        compatibility = payload["compatibility"]
        if not isinstance(compatibility, Mapping):
            raise ValueError("compatibility must be an object")
        return cls(
            checkpoint_id=_read_str(payload, "checkpoint_id"),
            model_kind=_read_str(payload, "model_kind"),
            model_version=SemanticVersion.parse(_read_str(payload, "model_version")),
            rulesets=_read_string_tuple(payload, "rulesets"),
            compatibility=CheckpointCompatibilityV1.from_dict(compatibility),
        )

    def compatibility_errors(self, requirement: CheckpointRequirementV1) -> tuple[str, ...]:
        """Return deterministic incompatibility reasons for one consumer requirement."""
        if not isinstance(requirement, CheckpointRequirementV1):
            raise ValueError("requirement must be a CheckpointRequirementV1")
        errors: list[str] = []
        if requirement.ruleset not in self.rulesets:
            errors.append("ruleset is unsupported")
        if requirement.model_kind is not None and self.model_kind != requirement.model_kind:
            errors.append("model_kind differs")
        if self.model_version.major != requirement.minimum_model_version.major:
            errors.append("model_version major differs")
        elif self.model_version.compare_precedence(requirement.minimum_model_version) < 0:
            errors.append("model_version is below minimum")
        for field in CHECKPOINT_COMPATIBILITY_V1_FIELDS:
            if getattr(self.compatibility, field) != getattr(requirement.compatibility, field):
                errors.append(f"compatibility.{field} differs")
        return tuple(errors)

    def is_compatible_with(self, requirement: CheckpointRequirementV1) -> bool:
        """Return whether this manifest satisfies a consumer requirement."""
        return not self.compatibility_errors(requirement)

    def require_compatible(self, requirement: CheckpointRequirementV1) -> None:
        """Raise a precise error when this manifest cannot serve a consumer requirement."""
        errors = self.compatibility_errors(requirement)
        if errors:
            raise ValueError("incompatible checkpoint manifest: " + "; ".join(errors))

    def to_dict(self) -> dict[str, Any]:
        return {
            "kind": CHECKPOINT_MANIFEST_V1_KIND,
            "checkpoint_id": self.checkpoint_id,
            "model_kind": self.model_kind,
            "model_version": str(self.model_version),
            "rulesets": list(self.rulesets),
            "compatibility": self.compatibility.to_dict(),
        }

    def to_json(self, *, indent: int | None = None) -> str:
        return json.dumps(self.to_dict(), indent=indent, sort_keys=True)


def _validate_identifiers(
    identifiers: tuple[str, ...],
    *,
    field: str,
    forbid_numeric_leading_zero: bool,
) -> None:
    for identifier in identifiers:
        if not isinstance(identifier, str) or _IDENTIFIER.fullmatch(identifier) is None:
            raise ValueError(f"{field} identifiers must be non-empty alphanumeric/hyphen strings")
        if (
            forbid_numeric_leading_zero
            and identifier.isdigit()
            and len(identifier) > 1
            and identifier.startswith("0")
        ):
            raise ValueError(f"{field} numeric identifiers cannot contain leading zeroes")


def _compare_prerelease_identifier(left: str, right: str) -> int:
    if left == right:
        return 0
    left_numeric = left.isdigit()
    right_numeric = right.isdigit()
    if left_numeric and right_numeric:
        return -1 if int(left) < int(right) else 1
    if left_numeric:
        return -1
    if right_numeric:
        return 1
    return -1 if left < right else 1


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


def _read_string_tuple(payload: Mapping[str, Any], field: str) -> tuple[str, ...]:
    values = payload[field]
    if isinstance(values, (str, bytes)) or not isinstance(values, Sequence):
        raise ValueError(f"{field} must be an array")
    if any(not isinstance(value, str) for value in values):
        raise ValueError(f"{field} entries must be strings")
    return tuple(values)


def _validate_non_empty_str(value: Any, field: str) -> None:
    if not isinstance(value, str) or not value:
        raise ValueError(f"{field} must be a non-empty string")
