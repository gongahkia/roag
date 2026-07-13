"""Cross-ruleset legal-action oracle projected onto public v1 schemas."""

from __future__ import annotations

import json
from collections.abc import Mapping, Sequence
from dataclasses import dataclass
from typing import Any, Self

from kenjaku.core import TENHOU_3P, TENHOU_4P
from kenjaku.schema import ActionV1, LegalActionMaskV1
from kenjaku.simulation.environment import SandboxEnvironmentState, legal_sandbox_actions

LEGAL_ACTION_ORACLE_V1_KIND = "kenjaku-legal-action-oracle-v1"
LEGAL_ACTION_ORACLE_V1_FIELDS = ("kind", "ruleset", "seat", "actions", "mask")
_RULESET_PLAYERS = {TENHOU_4P.name: TENHOU_4P.players, TENHOU_3P.name: TENHOU_3P.players}


@dataclass(frozen=True, slots=True)
class LegalActionOracleV1:
    """Canonical legal actions and their fixed-width mask for one acting seat."""

    ruleset: str
    seat: int
    actions: tuple[ActionV1, ...]
    mask: LegalActionMaskV1

    def __post_init__(self) -> None:
        players = _RULESET_PLAYERS.get(self.ruleset)
        if players is None:
            raise ValueError("unsupported legal action oracle ruleset: " + self.ruleset)
        if (
            isinstance(self.seat, bool)
            or not isinstance(self.seat, int)
            or not 0 <= self.seat < players
        ):
            raise ValueError("seat must be within the ruleset player range")
        if not isinstance(self.actions, tuple) or not self.actions:
            raise ValueError("actions must be a non-empty immutable tuple")
        if any(not isinstance(action, ActionV1) for action in self.actions):
            raise ValueError("actions must contain ActionV1 values")
        if any(action.ruleset != self.ruleset for action in self.actions):
            raise ValueError("actions must match oracle ruleset")
        if not isinstance(self.mask, LegalActionMaskV1):
            raise ValueError("mask must be a LegalActionMaskV1")
        if self.mask.ruleset != self.ruleset:
            raise ValueError("mask must match oracle ruleset")
        if self.mask != LegalActionMaskV1.from_actions(self.actions, ruleset=self.ruleset):
            raise ValueError("mask must encode the oracle actions")

    @classmethod
    def from_dict(cls, payload: Mapping[str, Any]) -> Self:
        _require_exact_fields(payload, LEGAL_ACTION_ORACLE_V1_FIELDS)
        if _read_str(payload, "kind") != LEGAL_ACTION_ORACLE_V1_KIND:
            raise ValueError(f"LegalActionOracleV1 kind must be {LEGAL_ACTION_ORACLE_V1_KIND}")
        action_values = payload["actions"]
        if isinstance(action_values, (str, bytes)) or not isinstance(action_values, Sequence):
            raise ValueError("actions must be an array")
        actions: list[ActionV1] = []
        for value in action_values:
            if not isinstance(value, Mapping):
                raise ValueError("actions entries must be objects")
            actions.append(ActionV1.from_dict(value))
        mask = payload["mask"]
        if not isinstance(mask, Mapping):
            raise ValueError("mask must be an object")
        return cls(
            ruleset=_read_str(payload, "ruleset"),
            seat=_read_int(payload, "seat"),
            actions=tuple(actions),
            mask=LegalActionMaskV1.from_dict(mask),
        )

    def to_dict(self) -> dict[str, Any]:
        return {
            "kind": LEGAL_ACTION_ORACLE_V1_KIND,
            "ruleset": self.ruleset,
            "seat": self.seat,
            "actions": [action.to_dict() for action in self.actions],
            "mask": self.mask.to_dict(),
        }

    def to_json(self, *, indent: int | None = None) -> str:
        return json.dumps(self.to_dict(), indent=indent, sort_keys=True)


def legal_action_oracle_v1(
    state: SandboxEnvironmentState, *, seat: int | None = None
) -> LegalActionOracleV1:
    """Return all currently legal sandbox actions in stable public v1 form."""
    resolved_seat = state.current_seat if seat is None else seat
    actions = tuple(
        ActionV1.from_core(action, ruleset=state.ruleset)
        for action in legal_sandbox_actions(state, seat=resolved_seat)
    )
    return LegalActionOracleV1(
        ruleset=state.ruleset,
        seat=resolved_seat,
        actions=actions,
        mask=LegalActionMaskV1.from_actions(actions, ruleset=state.ruleset),
    )


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
    raise ValueError(f"LegalActionOracleV1 fields must match v1 schema: {'; '.join(details)}")


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
