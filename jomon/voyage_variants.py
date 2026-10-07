"""Causal, inspectable variants for Jomon's twelve retained voyage families."""

from __future__ import annotations

from dataclasses import dataclass

from .catalog import VESSEL_SECTIONS, load_catalog
from .state import GameState


@dataclass(frozen=True)
class VoyageVariant:
    id: str
    family: str

    @property
    def name(self) -> str:
        from .travel_presentation import variant_display_name
        return variant_display_name(self.id)


VARIANTS = {row["family"]: VoyageVariant(row["id"], row["family"]) for row in load_catalog("vessel.json", VESSEL_SECTIONS)["variants"]}

VOYAGE_VARIANT_HISTORY_LIMIT = 12


def _market_shortage(state: GameState) -> bool:
    return any(
        entry.demand - entry.stock >= 2
        for market in state.regional_markets.values()
        for entry in market.values()
    )


def qualifies(state: GameState, family: str, destination: str) -> bool:
    """Read only current causal state; selection itself never advances the clock."""
    from .calendar import calendar_at
    from .route_chart import edge_between
    from .vessel_refits import installed

    edge = edge_between(state, state.route_current_node, destination)
    if family == "raiders":
        return bool(state.vessel_cargo) and _market_shortage(state)
    if family == "creature":
        return any(region.changes.get("aftermath_configuration") for region in state.regions.values())
    if family == "lure":
        return bool(state.vessel_changes.get("voyage_outcome:lure")) or installed(state, "signal-mast-shutter")
    if family in {"boarders", "inspection"}:
        return any(account.obligation > 0 for account in state.institutions.values())
    if family == "hold-thieves":
        return sum(stack.quantity for stack in state.vessel_cargo.values()) >= 5
    if family == "storm":
        return state.weather in {"storm", "gale"} or bool(edge and edge.weather_exposure >= 3)
    if family == "shoal":
        return bool(edge and edge.id in state.traversed_route_edges)
    if family == "driftwood":
        return any(event.kind == "fire" for region in state.regions.values() for event in region.regional_history)
    if family == "galley-fire":
        return any(state.vessel_cargo.get(name) and state.vessel_cargo[name].quantity for name in ("wool", "charcoal"))
    if family == "split-seam":
        return state.vessel_integrity <= 6 or bool(state.vessel_changes.get("hull_repairs"))
    if family == "flooded-hold":
        return calendar_at(state).season == "winter"
    return False


def select_variant(state: GameState, family: str, destination: str) -> VoyageVariant | None:
    variant = VARIANTS.get(family)
    return variant if variant and qualifies(state, family, destination) else None


def active_variant(state: GameState, family: str | None = None) -> VoyageVariant | None:
    variant_id = state.vessel_changes.get("active_voyage_variant")
    variant = next((row for row in VARIANTS.values() if row.id == variant_id), None)
    if variant is None or (family is not None and variant.family != family):
        return None
    return variant


def record_variant_outcome(state: GameState, consequence: str) -> None:
    variant = active_variant(state)
    if variant is None:
        state.vessel_changes.pop("active_voyage_variant", None)
        return
    state.vessel_changes[f"voyage_variant:{state.travel_count}"] = variant.id
    state.vessel_changes[f"voyage_variant_outcome:{state.travel_count}"] = consequence
    seen_key = f"voyage_variant_seen:{variant.id}"
    state.vessel_changes[seen_key] = int(state.vessel_changes.get(seen_key, 0)) + 1
    state.vessel_changes[f"voyage_variant_last:{variant.id}"] = consequence
    recorded = sorted(
        int(key.rsplit(":", 1)[1])
        for key in state.vessel_changes
        if key.startswith("voyage_variant:") and key.rsplit(":", 1)[1].isdigit()
    )
    for voyage in recorded[:-VOYAGE_VARIANT_HISTORY_LIMIT]:
        state.vessel_changes.pop(f"voyage_variant:{voyage}", None)
        state.vessel_changes.pop(f"voyage_variant_outcome:{voyage}", None)
    state.vessel_changes.pop("active_voyage_variant", None)
    from .state import append_narrative_record
    append_narrative_record(state,event_id="travel.variant.resolved",refs={"variant_id":variant.id,"voyage_family_id":variant.family,"region_id":state.active_region_id},params={"voyage":state.travel_count,"world_time":state.world_time},rendered=consequence)


def validate_variants() -> None:
    from .ship_crises import VOYAGES

    if set(VARIANTS) != set(VOYAGES):
        raise ValueError("every retained voyage family needs exactly one stateful variant")
    rows = tuple(VARIANTS.values())
    if len({row.id for row in rows}) != 12:
        raise ValueError("voyage variant identities must be distinct")
