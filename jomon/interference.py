"""Eight bounded consequences that make work in one region enter another."""

from __future__ import annotations

from dataclasses import dataclass

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


INTERFERENCES = tuple(Interference(*row) for row in (
    ("measured-grain-release", "shipment", "hearthford", "marlbank", "grain", "The measured grain release", "Hearthford's settled bank work releases a witnessed seed shipment", "one mill lot leaves under a named measure", "seed demand eases and a public unloading stair appears"),
    ("wreck-iron-on-span", "institutional dispute", "greywash", "rillscar", "ironwork", "Wreck iron on the span", "Greywash salvage title follows recovered iron upriver", "the wreck account records an exported fitting", "both bridge claims contest, then mark, the fitting"),
    ("burn-refuge-migration", "ecological displacement", "greenwold", "dunmire", "charcoal", "The refuge migration", "Greenwold's worked fire margin displaces grazers toward wet islands", "the burn edge gains browsing regrowth", "fen workers open a second animal margin"),
    ("high-bell-winter-mark", "route warning", "whitecairn", "frostmere", "wool", "The bell carried into winter", "an honest high-road interval is compared with estuary soundings", "the bell account sends one named warning", "the cold route gains a forecast mark and shelter demand"),
    ("peat-bank-mill-water", "route repair", "dunmire", "hearthford", "timber", "Peat bank, mill water", "a repaired raised bank changes how the mill meadow receives floodwater", "one bank face carries less overtopping pressure", "a relief channel opens beside the public mill approach"),
    ("gorge-fitting-return", "shipment", "rillscar", "greywash", "ironwork", "The gorge fitting returned", "Rillscar sends a witnessed bridge fitting to the coast account", "the two-span ledger records an outbound repair", "the chain house receives one lower-risk iron fitting"),
    ("fired-drain-to-burn", "material spillover", "marlbank", "greenwold", "charcoal", "Fired drain at the burn edge", "Marlbank drainage tiles answer a coppice firebreak request", "the kiln court takes a witnessed fuel order", "one burn approach drains rain without feeding the fire"),
    ("winter-wool-on-ridge", "shipment", "frostmere", "whitecairn", "wool", "Winter wool on the ridge", "Frostmere shelter wool reaches an exposed bell road", "the net-house account marks a protected outgoing lot", "the ridge shelter opens and cold-route confidence rises"),
))

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
        origin.changes[f"interference-out:{row.id}"] = row.origin_change
        receiving.changes[f"interference-in:{row.id}"] = row.destination_change
        state.vessel_changes[_record_key(row)] = state.world_time
        source_market = state.regional_markets[row.origin][row.cargo]
        destination_market = state.regional_markets[row.destination][row.cargo]
        source_market.stock = max(0, source_market.stock - 1)
        destination_market.stock += 1
        destination_market.demand = max(0, destination_market.demand - 1)
        source_account = state.institutions.get(f"work:{row.origin}")
        destination_account = state.institutions.get(f"work:{row.destination}")
        evidence = f"{row.title}: {row.cause}; {row.origin_change}; {row.destination_change}."
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
                    node.seasonal_note = row.destination_change
        state.chronicle.append(evidence)
        del state.chronicle[:-24]
        state.remember(evidence)
        state.add_message(f"INTERFERENCE — {row.title}: work from {state.regions[row.origin].name} changes this arrival.", priority=3)
        applied.append(row)
    return applied


def lines_for_region(state: GameState, region_id: str) -> list[str]:
    result = []
    region = state.regions[region_id]
    for row in INTERFERENCES:
        if region.changes.get(f"interference-in:{row.id}"):
            result.append(f"FACT ARRIVAL — {row.title}: {row.destination_change}.")
        if region.changes.get(f"interference-out:{row.id}"):
            result.append(f"REMEMBERED DEPARTURE — {row.title}: {row.origin_change}.")
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


validate_interference()
