"""Shared fixed-width legal-action mask serialization."""

from __future__ import annotations

import json
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from typing import Any, Self

from kenjaku.core import TENHOU_3P, TENHOU_4P, ActionKind, TileType
from kenjaku.schema.action_v1 import ACTION_V1_ACTIONS_BY_RULESET, ActionV1

LEGAL_ACTION_MASK_V1_KIND = "kenjaku-legal-action-mask-v1"
LEGAL_ACTION_MASK_V1_FIELDS = ("kind", "ruleset", "mask")
LEGAL_ACTION_MASK_V1_TILE_ACTION_OFFSETS = {
    ActionKind.DISCARD.value: 0,
    ActionKind.RON.value: 34,
    ActionKind.CHI.value: 68,
    ActionKind.PON.value: 102,
    ActionKind.MINKAN.value: 136,
    ActionKind.ANKAN.value: 170,
    ActionKind.KAKAN.value: 204,
    ActionKind.KITA.value: 238,
}
LEGAL_ACTION_MASK_V1_PASS_INDEX = 272
LEGAL_ACTION_MASK_V1_TSUMO_INDEX = 273
LEGAL_ACTION_MASK_V1_RIICHI_INDEX = 274
LEGAL_ACTION_MASK_V1_KYUSHU_INDEX = 275
LEGAL_ACTION_MASK_V1_DIM = 276
_RULESETS = {TENHOU_4P.name: TENHOU_4P, TENHOU_3P.name: TENHOU_3P}
_SINGLE_ACTION_INDICES = {
    ActionKind.PASS.value: LEGAL_ACTION_MASK_V1_PASS_INDEX,
    ActionKind.TSUMO.value: LEGAL_ACTION_MASK_V1_TSUMO_INDEX,
    ActionKind.RIICHI.value: LEGAL_ACTION_MASK_V1_RIICHI_INDEX,
    ActionKind.KYUSHU.value: LEGAL_ACTION_MASK_V1_KYUSHU_INDEX,
}


@dataclass(frozen=True, slots=True)
class LegalActionMaskV1:
    """A ruleset-validated 276-slot legal-action mask."""

    ruleset: str
    mask: tuple[bool, ...]

    def __post_init__(self) -> None:
        if self.ruleset not in _RULESETS:
            raise ValueError("unsupported LegalActionMaskV1 ruleset: " + self.ruleset)
        if not isinstance(self.mask, tuple):
            raise ValueError("mask must be an immutable tuple")
        if len(self.mask) != LEGAL_ACTION_MASK_V1_DIM:
            raise ValueError(f"mask must contain {LEGAL_ACTION_MASK_V1_DIM} entries")
        if not all(isinstance(value, bool) for value in self.mask):
            raise ValueError("mask entries must be booleans")
        if not any(self.mask):
            raise ValueError("LegalActionMaskV1 cannot be empty")
        for index, enabled in enumerate(self.mask):
            if enabled and action_v1_for_mask_index(index, ruleset=self.ruleset) is None:
                raise ValueError(
                    f"mask enables an unavailable {self.ruleset} action at index {index}"
                )

    @classmethod
    def from_actions(cls, actions: Sequence[ActionV1], *, ruleset: str) -> Self:
        """Encode ActionV1 values into their deterministic shared coordinates."""
        if not actions:
            raise ValueError("LegalActionMaskV1 cannot be empty")
        mask = [False] * LEGAL_ACTION_MASK_V1_DIM
        for action in actions:
            if action.ruleset != ruleset:
                raise ValueError("all actions must use the mask ruleset")
            mask[action_v1_mask_index(action)] = True
        return cls(ruleset=ruleset, mask=tuple(mask))

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> Self:
        _require_exact_fields(payload, LEGAL_ACTION_MASK_V1_FIELDS)
        if _read_str(payload, "kind") != LEGAL_ACTION_MASK_V1_KIND:
            raise ValueError(f"LegalActionMaskV1 kind must be {LEGAL_ACTION_MASK_V1_KIND}")
        return cls(ruleset=_read_str(payload, "ruleset"), mask=_read_bool_tuple(payload, "mask"))

    def actions(self) -> tuple[ActionV1, ...]:
        """Decode every enabled coordinate to a canonical ActionV1 value."""
        actions: list[ActionV1] = []
        for index, enabled in enumerate(self.mask):
            if not enabled:
                continue
            action = action_v1_for_mask_index(index, ruleset=self.ruleset)
            if action is None:
                raise ValueError(f"mask enables unavailable action index {index}")
            actions.append(action)
        return tuple(actions)

    def to_dict(self) -> dict[str, Any]:
        return {
            "kind": LEGAL_ACTION_MASK_V1_KIND,
            "ruleset": self.ruleset,
            "mask": list(self.mask),
        }

    def to_json(self, *, indent: int | None = None) -> str:
        return json.dumps(self.to_dict(), indent=indent, sort_keys=True)


def action_v1_mask_index(action: ActionV1) -> int:
    """Return the fixed-width coordinate for one ActionV1 value."""
    return legal_action_mask_v1_index_from_parts(action.action, action.tile)


def action_v1_for_mask_index(index: int, *, ruleset: str) -> ActionV1 | None:
    """Decode one coordinate, or return None when ruleset-invalid."""
    if ruleset not in _RULESETS:
        raise ValueError("unsupported LegalActionMaskV1 ruleset: " + ruleset)
    if not 0 <= index < LEGAL_ACTION_MASK_V1_DIM:
        raise ValueError(f"mask index outside range: {index}")
    for action, offset in LEGAL_ACTION_MASK_V1_TILE_ACTION_OFFSETS.items():
        if offset <= index < offset + 34:
            tile = TileType(index - offset)
            if action not in ACTION_V1_ACTIONS_BY_RULESET[ruleset]:
                return None
            if tile not in _RULESETS[ruleset].tile_types:
                return None
            return ActionV1(ruleset=ruleset, action=action, tile=tile.notation)
    for action, action_index in _SINGLE_ACTION_INDICES.items():
        if index == action_index:
            if action not in ACTION_V1_ACTIONS_BY_RULESET[ruleset]:
                return None
            return ActionV1(ruleset=ruleset, action=action)
    raise ValueError(f"unsupported mask index: {index}")


def legal_action_mask_v1_index_from_payload(action: Mapping[str, Any]) -> int:
    """Map an existing unversioned action payload to the shared coordinate."""
    kind = action.get("kind")
    if not isinstance(kind, str):
        raise ValueError("action kind must be a string")
    tile = action.get("tile")
    if tile is not None and not isinstance(tile, str):
        raise ValueError("action tile must be a string or null")
    return legal_action_mask_v1_index_from_parts(kind, tile)


def legal_action_mask_v1_index_from_parts(kind: str, tile: str | None) -> int:
    """Map one action kind and optional tile to the shared coordinate."""
    offset = LEGAL_ACTION_MASK_V1_TILE_ACTION_OFFSETS.get(kind)
    if offset is not None:
        if tile is None:
            raise ValueError(f"{kind} action requires tile payload")
        return offset + TileType.parse(tile).index
    action_index = _SINGLE_ACTION_INDICES.get(kind)
    if action_index is not None:
        if tile is not None:
            raise ValueError(f"{kind} action cannot include tile payload")
        return action_index
    raise ValueError("unsupported legal action kind: " + kind)


def legal_action_mask_v1_from_payloads(actions: Sequence[Mapping[str, Any]]) -> list[bool]:
    """Encode existing unversioned action payloads without changing their format."""
    mask = [False] * LEGAL_ACTION_MASK_V1_DIM
    for action in actions:
        mask[legal_action_mask_v1_index_from_payload(action)] = True
    if not any(mask):
        raise ValueError("legal action mask cannot be empty")
    return mask


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
    raise ValueError(f"LegalActionMaskV1 fields must match v1 schema: {'; '.join(details)}")


def _read_str(payload: Mapping[str, Any], field: str) -> str:
    value = payload[field]
    if not isinstance(value, str):
        raise ValueError(f"{field} must be a string")
    return value


def _read_bool_tuple(payload: Mapping[str, Any], field: str) -> tuple[bool, ...]:
    values = payload[field]
    if isinstance(values, (str, bytes)) or not isinstance(values, Sequence):
        raise ValueError(f"{field} must be an array")
    if not all(isinstance(value, bool) for value in values):
        raise ValueError(f"{field} entries must be booleans")
    return tuple(values)
