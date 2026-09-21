"""Eight bounded consequences that make work in one region enter another."""

from __future__ import annotations

from dataclasses import dataclass

from .catalog import AFTERMATH_SECTIONS, load_catalog
from .interference_presentation import interference_event_text, interference_format
from .state import GameState


@dataclass(frozen=True)
class Interference:
    id: str
    kind: str
    origin: str
    destination: str
    cargo: str
    title: str
    cause: str
    origin_change: str
    destination_change: str


INTERFERENCES = tuple(Interference(**row) for row in load_catalog("aftermath.json", AFTERMATH_SECTIONS)["interferences"])

BY_ID = {row.id: row for row in INTERFERENCES}


def _record_key(row: Interference) -> str:
    return f"interference:{row.id}"


def _origin_ready(state: GameState, row: Interference) -> bool:
    region = state.regions.get(row.origin)
    if not region:
        return False
    situation = any(key.startswith("micro-site:resolved:") and value for key, value in region.changes.items())
    quest = state.questlines.get(row.origin)
    return situation or bool(quest and quest.status == "completed") or bool(region.changes.get("aftermath_configuration"))


def apply_arrival(state: GameState, destination: str) -> list[Interference]:
    """Apply each eligible event once, only when its receiving region is entered."""
    applied = []
    for row in INTERFERENCES:
        if row.destination != destination or _record_key(row) in state.vessel_changes or not _origin_ready(state, row):
            continue
        origin = state.regions.get(row.origin)
        receiving = state.regions.get(row.destination)
        if not origin or not receiving:
            continue
        title = interference_event_text(row.id, "title")
        cause = interference_event_text(row.id, "cause")
        origin_change = interference_event_text(row.id, "origin_change")
        destination_change = interference_event_text(row.id, "destination_change")
        origin.changes[f"interference-out:{row.id}"] = origin_change
        receiving.changes[f"interference-in:{row.id}"] = destination_change
        state.vessel_changes[_record_key(row)] = state.world_time
        source_market = state.regional_markets[row.origin][row.cargo]
        destination_market = state.regional_markets[row.destination][row.cargo]
        source_market.stock = max(0, source_market.stock - 1)
        destination_market.stock += 1
        destination_market.demand = max(0, destination_market.demand - 1)
        source_account = state.institutions.get(f"work:{row.origin}")
        destination_account = state.institutions.get(f"work:{row.destination}")
        evidence = interference_format(
            "interference.record", title=title, cause=cause,
            origin_change=origin_change, destination_change=destination_change,
        )
        for account in (source_account, destination_account):
            if account:
                account.witnessed_acts.append(evidence)
                del account.witnessed_acts[:-12]
        if destination_account:
            destination_account.confidence = min(3, destination_account.confidence + 1)
        for node in state.route_nodes.values():
            if node.region_id == row.destination:
                node.risk = max(0, node.risk - int(row.kind in {"route repair", "route warning"}))
                if row.kind == "route warning":
                    node.seasonal_note = destination_change
        state.chronicle.append(evidence)
        del state.chronicle[:-24]
        state.remember(evidence)
        state.add_message(interference_format("interference.notice", title=title, origin=state.regions[row.origin].name), priority=3)
        applied.append(row)
    return applied


def lines_for_region(state: GameState, region_id: str) -> list[str]:
    result = []
    region = state.regions[region_id]
    for row in INTERFERENCES:
        if region.changes.get(f"interference-in:{row.id}"):
            result.append(interference_format(
                "interference.ledger.arrival", title=interference_event_text(row.id, "title"),
                destination_change=interference_event_text(row.id, "destination_change"),
            ))
        if region.changes.get(f"interference-out:{row.id}"):
            result.append(interference_format(
                "interference.ledger.departure", title=interference_event_text(row.id, "title"),
                origin_change=interference_event_text(row.id, "origin_change"),
            ))
    return result


def audit_interference() -> dict[str, object]:
    failures = []
    regions = {row.origin for row in INTERFERENCES}
    if len(INTERFERENCES) != 8 or len(BY_ID) != 8:
        failures.append("exactly eight distinct events are required")
    if regions != {row.destination for row in INTERFERENCES} or len(regions) != 8:
        failures.append("every region must send and receive one event")
    if len({row.kind for row in INTERFERENCES}) < 5 or any(not all((row.cause, row.origin_change, row.destination_change)) for row in INTERFERENCES):
        failures.append("events need varied causes and two-sided consequences")
    return {"events": len(INTERFERENCES), "kinds": sorted({row.kind for row in INTERFERENCES}), "failures": failures}


def validate_interference() -> None:
    report = audit_interference()
    if report["failures"]:
        raise ValueError(report)


def validate_interference_state(state: GameState) -> None:
    for key_name, value in state.vessel_changes.items():
        if key_name.startswith("interference:"):
            event_id = key_name.split(":", 1)[1]
            if event_id not in BY_ID or not isinstance(value, int) or value < 0:
                raise ValueError("invalid cross-region interference record")


validate_interference()
