"""Causal, inspectable variants for Jomon's twelve retained voyage families."""

from __future__ import annotations

from dataclasses import dataclass

from .state import GameState


@dataclass(frozen=True)
class VoyageVariant:
    id: str
    family: str
    name: str
    cause: str
    effect: str
    counterplay: str


VARIANTS = {
    row.family: row
    for row in (
        VoyageVariant("shortage-skiffs", "raiders", "Shortage skiffs", "A recorded market shortage gives loose cargo an immediate buyer.", "A third lot-caller boards; conceding can cost a second lot.", "Repel the staged group, misdirect it, or let fitted rail netting catch the first loss."),
        VoyageVariant("displaced-pair", "creature", "Displaced breeding pair", "Altered aftermath work has shifted a territorial pair into the rudder shoal.", "Two animals hold separate deck approaches and bait requires two fish lots.", "Use spaced reach, pilot the narrow water, or pay the disclosed two-lot bait cost."),
        VoyageVariant("returning-resonance", "lure", "Returning resonance", "A prior answering or Jomon's conspicuous fitted signal gives the mineral echo a remembered pattern.", "Signal navigation takes a second action and leaves a named route account.", "Anchor materially, counsel named crew, or accept the visible signal record."),
        VoyageVariant("obligation-claim", "boarders", "Obligation claim", "An unsettled institutional obligation has been sold to a boarding company.", "A claimant joins the boarding and offers a finite account settlement.", "Take the decks, surrender cargo, or settle one real obligation before boarding."),
        VoyageVariant("marked-shortage-lot", "hold-thieves", "Marked shortage lot", "A heavily loaded hold makes one regional shortage worth marking in advance.", "The thief targets the most demanded carried commodity and arrives through net drag if fitted.", "Intercept the physical lot, exploit the fitted net, or abandon the declared claim."),
        VoyageVariant("crosswind-stay", "storm", "Crosswind stay", "Severe route exposure or present storm weather loads two sides of the upper rig.", "Emergency station work takes an extra exposed action and two supports appear.", "Brace either support directly, fit a backstay, or spend the disclosed longer station work."),
        VoyageVariant("returning-silt-tongue", "shoal", "Returning silt tongue", "A previously travelled edge now carries a newly legible deposit across its old sounding.", "Careful sounding takes two extra actions; forcing costs one extra hull integrity.", "Use keel shoes to recover one measure, accept delay, or accept the disclosed scrape."),
        VoyageVariant("fire-marked-raft", "driftwood", "Fire-marked carrier raft", "A recorded regional fire left charcoal lashed among the drifting timbers.", "Rope recovery takes one extra action but yields a wet timber lot and a charcoal lot.", "Claim both physical lots with rope or leave the dangerous salvage unclaimed."),
        VoyageVariant("grease-soaked-store", "galley-fire", "Grease-soaked galley store", "Carried wool or charcoal has trapped lamp grease and sparks beside the galley.", "A second fire starts and station work takes one extra exposed action.", "Extinguish sparse fires directly or use the fitted cover to recover one action."),
        VoyageVariant("worked-seam", "split-seam", "Worked seam reopens", "Low integrity or a recorded repair loads an older hull seam instead of clean timber.", "The seam begins wetter and weaker; station work takes an extra exposed action.", "Brace the sparse support, use strainers to recover one action, or repair after passage."),
        VoyageVariant("thaw-surge", "flooded-hold", "Thaw surge", "Spring high water or winter ice pressure drives water through more than one opening.", "Two lower-deck cells flood and emergency work takes an extra exposed action.", "Pump the whole deck, redirect water, or use strainers to recover one action."),
        VoyageVariant("counterclaim-inspection", "inspection", "Counterclaim inspection", "An outstanding institution account gives the patrol a second plausible claimant.", "Witness trust needs one additional level and a cash settlement costs three credit.", "Produce stronger trust, a physical paper lot, the signal shutter, or pay the stated account."),
    )
}

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


def validate_variants() -> None:
    from .ship_crises import VOYAGES

    if set(VARIANTS) != set(VOYAGES):
        raise ValueError("every retained voyage family needs exactly one stateful variant")
    rows = tuple(VARIANTS.values())
    if len({row.id for row in rows}) != 12 or len({row.name for row in rows}) != 12:
        raise ValueError("voyage variant identities must be distinct")
    if any(not row.cause or not row.effect or not row.counterplay for row in rows):
        raise ValueError("each voyage variant needs cause, effect and counterplay")
