"""Versioned action payloads for 4-player and Sanma policy consumers."""

from __future__ import annotations

import json
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from typing import Any, Self

from kenjaku.core import TENHOU_3P, TENHOU_4P, Action, ActionKind, Tile, TileType

ACTION_V1_KIND = "kenjaku-action-v1"
ACTION_V1_FIELDS = ("kind", "ruleset", "action", "tile", "tsumogiri", "consumed")
ACTION_V1_ACTIONS_BY_RULESET = {
    TENHOU_4P.name: (
        ActionKind.DISCARD.value,
        ActionKind.RIICHI.value,
        ActionKind.CHI.value,
        ActionKind.PON.value,
        ActionKind.MINKAN.value,
        ActionKind.ANKAN.value,
        ActionKind.KAKAN.value,
        ActionKind.PASS.value,
        ActionKind.TSUMO.value,
        ActionKind.RON.value,
        ActionKind.KYUSHU.value,
    ),
    TENHOU_3P.name: (
        ActionKind.DISCARD.value,
        ActionKind.RIICHI.value,
        ActionKind.PON.value,
        ActionKind.MINKAN.value,
        ActionKind.ANKAN.value,
        ActionKind.KAKAN.value,
        ActionKind.KITA.value,
        ActionKind.PASS.value,
        ActionKind.TSUMO.value,
        ActionKind.RON.value,
        ActionKind.KYUSHU.value,
    ),
}
_RULESETS = {TENHOU_4P.name: TENHOU_4P, TENHOU_3P.name: TENHOU_3P}
_TILE_ACTIONS = frozenset(
    {
        ActionKind.DISCARD.value,
        ActionKind.CHI.value,
        ActionKind.PON.value,
        ActionKind.MINKAN.value,
        ActionKind.ANKAN.value,
        ActionKind.KAKAN.value,
        ActionKind.KITA.value,
        ActionKind.RON.value,
    }
)
_CONSUMED_COUNTS = {
    ActionKind.CHI.value: 2,
    ActionKind.PON.value: 2,
    ActionKind.MINKAN.value: 3,
    ActionKind.ANKAN.value: 4,
    ActionKind.KAKAN.value: 1,
    ActionKind.KITA.value: 1,
}


@dataclass(frozen=True, slots=True)
class ActionV1:
    """One ruleset-validated policy action with optional physical consumed tiles."""

    ruleset: str
    action: str
    tile: str | None = None
    tsumogiri: bool = False
    consumed: tuple[str, ...] = ()

    def __post_init__(self) -> None:
        if not isinstance(self.ruleset, str):
            raise ValueError("ruleset must be a string")
        if not isinstance(self.action, str):
            raise ValueError("action must be a string")
        if not isinstance(self.consumed, tuple):
            raise ValueError("consumed must be an immutable tuple")
        rules = _RULESETS.get(self.ruleset)
        if rules is None:
            raise ValueError("unsupported ActionV1 ruleset: " + self.ruleset)
        if self.action not in ACTION_V1_ACTIONS_BY_RULESET[self.ruleset]:
            raise ValueError(f"{self.action} is unavailable in {self.ruleset}")
        if self.action in _TILE_ACTIONS:
            if self.tile is None:
                raise ValueError(f"{self.action} ActionV1 actions require a tile")
            _validate_canonical_tile_type(self.tile, self.ruleset, "tile")
        elif self.tile is not None:
            raise ValueError(f"{self.action} ActionV1 actions cannot include a tile")
        if not isinstance(self.tsumogiri, bool):
            raise ValueError("tsumogiri must be a boolean")
        if self.action != ActionKind.DISCARD.value and self.tsumogiri:
            raise ValueError("tsumogiri is only valid for discard actions")
        _validate_consumed(self.action, self.consumed, self.ruleset)

    @classmethod
    def from_core(cls, action: Action, *, ruleset: str) -> Self:
        """Encode one core Action in the ActionV1 vocabulary."""
        return cls(
            ruleset=ruleset,
            action=action.kind.value,
            tile=None if action.tile is None else action.tile.notation,
            tsumogiri=action.tsumogiri,
            consumed=tuple(tile.notation for tile in action.consumed),
        )

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> Self:
        _require_exact_fields(payload, ACTION_V1_FIELDS)
        if _read_str(payload, "kind") != ACTION_V1_KIND:
            raise ValueError(f"ActionV1 kind must be {ACTION_V1_KIND}")
        return cls(
            ruleset=_read_str(payload, "ruleset"),
            action=_read_str(payload, "action"),
            tile=_read_optional_str(payload, "tile"),
            tsumogiri=_read_bool(payload, "tsumogiri"),
            consumed=_read_string_tuple(payload, "consumed"),
        )

    def to_core(self) -> Action:
        """Decode this ActionV1 payload into the core Action representation."""
        return Action(
            kind=ActionKind(self.action),
            tile=None if self.tile is None else TileType.parse(self.tile),
            tsumogiri=self.tsumogiri,
            consumed=tuple(Tile.parse(tile) for tile in self.consumed),
        )

    def to_dict(self) -> dict[str, Any]:
        return {
            "kind": ACTION_V1_KIND,
            "ruleset": self.ruleset,
            "action": self.action,
            "tile": self.tile,
            "tsumogiri": self.tsumogiri,
            "consumed": list(self.consumed),
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
    raise ValueError(f"ActionV1 fields must match v1 schema: {'; '.join(details)}")


def _read_str(payload: Mapping[str, Any], field: str) -> str:
    value = payload[field]
    if not isinstance(value, str):
        raise ValueError(f"{field} must be a string")
    return value


def _read_optional_str(payload: Mapping[str, Any], field: str) -> str | None:
    value = payload[field]
    if value is None:
        return None
    if not isinstance(value, str):
        raise ValueError(f"{field} must be a string or null")
    return value


def _read_bool(payload: Mapping[str, Any], field: str) -> bool:
    value = payload[field]
    if not isinstance(value, bool):
        raise ValueError(f"{field} must be a boolean")
    return value


def _read_string_tuple(payload: Mapping[str, Any], field: str) -> tuple[str, ...]:
    values = payload[field]
    if isinstance(values, (str, bytes)) or not isinstance(values, Sequence):
        raise ValueError(f"{field} must be an array")
    if not all(isinstance(value, str) for value in values):
        raise ValueError(f"{field} entries must be strings")
    return tuple(values)


def _validate_canonical_tile_type(value: str, ruleset: str, field: str) -> None:
    if not isinstance(value, str):
        raise ValueError(f"{field} must be a tile string")
    tile_type = TileType.parse(value)
    if tile_type.notation != value:
        raise ValueError(f"{field} must use canonical tile-type notation")
    if tile_type not in _RULESETS[ruleset].tile_types:
        raise ValueError(f"{field} tile is unavailable in {ruleset}: {value}")


def _validate_consumed(action: str, consumed: tuple[str, ...], ruleset: str) -> None:
    expected = _CONSUMED_COUNTS.get(action)
    if expected is None:
        if consumed:
            raise ValueError(f"{action} ActionV1 actions cannot consume tiles")
        return
    if len(consumed) not in {0, expected}:
        raise ValueError(f"{action} consumed tiles must be empty or contain {expected} tiles")
    for tile in consumed:
        if not isinstance(tile, str):
            raise ValueError("consumed entries must be tile strings")
        parsed = Tile.parse(tile)
        if parsed.notation != tile:
            raise ValueError("consumed entries must use canonical tile notation")
        if parsed.type not in _RULESETS[ruleset].tile_types:
            raise ValueError(f"consumed tile is unavailable in {ruleset}: {tile}")
