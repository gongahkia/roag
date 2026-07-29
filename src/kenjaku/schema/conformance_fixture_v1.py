"""Versioned synthetic conformance fixtures for deterministic rule scenarios."""

from __future__ import annotations

import json
import math
import re
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from typing import Any, Self, cast

from kenjaku.core import TENHOU_3P, TENHOU_4P
from kenjaku.schema.action_v1 import ActionV1

CONFORMANCE_FIXTURE_V1_KIND = "kenjaku-conformance-fixture-v1"
CONFORMANCE_FIXTURE_V1_FIELDS = (
    "kind",
    "id",
    "ruleset",
    "scenario",
    "seed",
    "tags",
    "setup",
    "actions",
    "expected",
)
_RULESETS = frozenset((TENHOU_4P.name, TENHOU_3P.name))
_IDENTIFIER = re.compile(r"[a-z][a-z0-9_-]{0,127}")
_SCENARIO = re.compile(r"[a-z][a-z0-9_]{0,63}")
_TAG = re.compile(r"[a-z][a-z0-9_-]{0,63}")


@dataclass(frozen=True, slots=True)
class ConformanceFixtureV1:
    """A deterministic synthetic rule scenario and its scenario-specific assertions."""

    fixture_id: str
    ruleset: str
    scenario: str
    seed: str
    tags: tuple[str, ...]
    setup: Mapping[str, Any]
    actions: tuple[ActionV1, ...]
    expected: Mapping[str, Any]

    def __post_init__(self) -> None:
        _validate_identifier(self.fixture_id, "fixture_id", _IDENTIFIER)
        if self.ruleset not in _RULESETS:
            raise ValueError("unsupported conformance fixture ruleset: " + self.ruleset)
        _validate_identifier(self.scenario, "scenario", _SCENARIO)
        if not isinstance(self.seed, str) or not self.seed:
            raise ValueError("seed must be a non-empty string")
        if not isinstance(self.tags, tuple):
            raise ValueError("tags must be an immutable tuple")
        if any(_TAG.fullmatch(tag) is None for tag in self.tags):
            raise ValueError("tags must be lowercase identifiers")
        if len(set(self.tags)) != len(self.tags):
            raise ValueError("tags must be unique")
        if not isinstance(self.actions, tuple):
            raise ValueError("actions must be an immutable tuple")
        for action in self.actions:
            if not isinstance(action, ActionV1):
                raise ValueError("actions must contain ActionV1 values")
            if action.ruleset != self.ruleset:
                raise ValueError("fixture actions must match fixture ruleset")
        object.__setattr__(
            self, "setup", _copy_json_object(self.setup, "setup", allow_empty=False)
        )
        object.__setattr__(
            self, "expected", _copy_json_object(self.expected, "expected", allow_empty=False)
        )

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> Self:
        _require_exact_fields(payload, CONFORMANCE_FIXTURE_V1_FIELDS)
        if _read_str(payload, "kind") != CONFORMANCE_FIXTURE_V1_KIND:
            raise ValueError(
                f"ConformanceFixtureV1 kind must be {CONFORMANCE_FIXTURE_V1_KIND}"
            )
        values = payload["actions"]
        if isinstance(values, (str, bytes)) or not isinstance(values, Sequence):
            raise ValueError("actions must be an array")
        actions: list[ActionV1] = []
        for value in values:
            if not isinstance(value, Mapping):
                raise ValueError("actions entries must be objects")
            actions.append(ActionV1.from_dict(value))
        return cls(
            fixture_id=_read_str(payload, "id"),
            ruleset=_read_str(payload, "ruleset"),
            scenario=_read_str(payload, "scenario"),
            seed=_read_str(payload, "seed"),
            tags=_read_string_tuple(payload, "tags"),
            setup=_read_mapping(payload, "setup"),
            actions=tuple(actions),
            expected=_read_mapping(payload, "expected"),
        )

    def to_dict(self) -> dict[str, Any]:
        return {
            "kind": CONFORMANCE_FIXTURE_V1_KIND,
            "id": self.fixture_id,
            "ruleset": self.ruleset,
            "scenario": self.scenario,
            "seed": self.seed,
            "tags": list(self.tags),
            "setup": _copy_json_object(self.setup, "setup", allow_empty=False),
            "actions": [action.to_dict() for action in self.actions],
            "expected": _copy_json_object(self.expected, "expected", allow_empty=False),
        }

    def to_json(self, *, indent: int | None = None) -> str:
        return json.dumps(self.to_dict(), indent=indent, sort_keys=True)


def _require_exact_fields(payload: Mapping[str, Any], fields: Sequence[str]) -> None:
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
    raise ValueError(f"ConformanceFixtureV1 fields must match v1 schema: {'; '.join(details)}")


def _read_str(payload: Mapping[str, Any], field: str) -> str:
    value = payload[field]
    if not isinstance(value, str):
        raise ValueError(f"{field} must be a string")
    return value


def _read_string_tuple(payload: Mapping[str, Any], field: str) -> tuple[str, ...]:
    values = payload[field]
    if isinstance(values, (str, bytes)) or not isinstance(values, Sequence):
        raise ValueError(f"{field} must be an array")
    if not all(isinstance(value, str) for value in values):
        raise ValueError(f"{field} entries must be strings")
    return tuple(values)


def _read_mapping(payload: Mapping[str, Any], field: str) -> Mapping[str, Any]:
    value = payload[field]
    if not isinstance(value, Mapping):
        raise ValueError(f"{field} must be an object")
    return value


def _validate_identifier(value: str, field: str, pattern: re.Pattern[str]) -> None:
    if not isinstance(value, str) or pattern.fullmatch(value) is None:
        raise ValueError(f"{field} must be a lowercase identifier")


def _copy_json_object(
    value: Mapping[str, Any], field: str, *, allow_empty: bool
) -> dict[str, Any]:
    if not isinstance(value, Mapping):
        raise ValueError(f"{field} must be an object")
    if not value and not allow_empty:
        raise ValueError(f"{field} must not be empty")
    _validate_json_value(value, field)
    return cast(dict[str, Any], json.loads(json.dumps(value, allow_nan=False, sort_keys=True)))


def _validate_json_value(value: Any, field: str) -> None:
    if value is None or isinstance(value, (str, bool, int)):
        return
    if isinstance(value, float):
        if math.isfinite(value):
            return
        raise ValueError(f"{field} must contain finite JSON values")
    if isinstance(value, Mapping):
        for key, nested in value.items():
            if not isinstance(key, str):
                raise ValueError(f"{field} object keys must be strings")
            _validate_json_value(nested, field)
        return
    if isinstance(value, Sequence) and not isinstance(value, (str, bytes)):
        for nested in value:
            _validate_json_value(nested, field)
        return
    raise ValueError(f"{field} must contain JSON values")
