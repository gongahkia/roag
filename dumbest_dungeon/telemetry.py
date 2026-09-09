"""Deterministic local run ledger. Recording never reads time or random state."""

from __future__ import annotations

from copy import deepcopy
from collections import Counter, defaultdict
from dataclasses import asdict, dataclass, field
from typing import Any

from .manifest import canonical_bytes
from .versions import TELEMETRY_SCHEMA


@dataclass(frozen=True)
class RunRecord:
    sequence: int
    kind: str
    source_id: str
    tick: int
    round: int
    data: dict[str, Any]


@dataclass
class RunLedger:
    schema: int = TELEMETRY_SCHEMA
    incomplete_before_tick: int | None = None
    records: list[RunRecord] = field(default_factory=list)

    def record(self, event_kind: str, source_id: str, tick: int, round: int, **data: Any) -> None:
        canonical_bytes(data)
        self.records.append(RunRecord(len(self.records) + 1, event_kind, source_id, tick, round, deepcopy(data)))

    @classmethod
    def from_snapshot(cls, raw: dict[str, Any]) -> RunLedger:
        if not isinstance(raw, dict) or set(raw) != {"schema", "incomplete_before_tick", "records"}:
            raise ValueError("invalid run ledger fields")
        if type(raw["schema"]) is not int or raw["schema"] != TELEMETRY_SCHEMA:
            raise ValueError("unsupported telemetry schema")
        boundary = raw["incomplete_before_tick"]
        if boundary is not None and (type(boundary) is not int or boundary < 0):
            raise ValueError("invalid historical telemetry boundary")
        if not isinstance(raw["records"], list):
            raise ValueError("invalid run records")
        records = []
        for index, row in enumerate(raw["records"], 1):
            if not isinstance(row, dict) or set(row) != set(RunRecord.__dataclass_fields__):
                raise ValueError("invalid run record fields")
            if (type(row["sequence"]) is not int or row["sequence"] != index
                or any(type(row[key]) is not int or row[key] < 0 for key in ("tick", "round"))
                or any(not isinstance(row[key], str) or not row[key] for key in ("kind", "source_id"))
                or not isinstance(row["data"], dict)):
                raise ValueError("invalid run record")
            canonical_bytes(row["data"])
            records.append(RunRecord(**deepcopy(row)))
        return cls(TELEMETRY_SCHEMA, boundary, records)

    def snapshot(self) -> dict[str, Any]:
        return asdict(self)


def decision_counts(ledger: RunLedger) -> dict[str, dict[str, int]]:
    cards = defaultdict(Counter)
    for record in ledger.records:
        if record.kind == "card_offer":
            for identity in record.data["offered"]:
                cards[identity]["offered"] += 1
        elif record.kind == "card_choice":
            if record.data["picked"] is not None:
                cards[record.data["picked"]]["picked"] += 1
            for identity in record.data["skipped"]:
                cards[identity]["skipped"] += 1
        elif record.kind in {"card_play", "card_upgraded", "card_transformed", "card_removed", "card_lost"}:
            cards[record.source_id][record.kind.removeprefix("card_")] += 1
    return {identity: dict(sorted(counts.items())) for identity, counts in sorted(cards.items())}
