"""One bounded later echo for every causal voyage variant."""

from __future__ import annotations

from dataclasses import dataclass

from .state import GameState


@dataclass(frozen=True)
class VoyageEcho:
    variant_id: str
    kind: str
    title: str
    consequence: str


ECHOES = tuple(VoyageEcho(*row) for row in (
    ("shortage-skiffs", "rival preparation", "Hooks remembered at the next mooring", "a local cargo thief arrives with one extra finite supply, but names the marked shortage lot"),
    ("displaced-pair", "ecological return", "Tracks beside Jomon's wake", "the receiving region records where the displaced pair settled, changing its population margin"),
    ("returning-resonance", "crew memory", "The answer repeated correctly", "two household adults compare the false voice with Jomon's named signal and trust the shared watch"),
    ("obligation-claim", "institutional claim", "The boarding paper comes ashore", "the receiving work account records the settled or refused obligation instead of forgetting it"),
    ("marked-shortage-lot", "cargo claim", "A cut mark on the surviving lot", "the most demanded local cargo gains a witnessed claim and one point of market confidence"),
    ("crosswind-stay", "deck scar", "The stay's pale working scar", "the upper rig keeps a visible repair mark and the next storm station estimate is clearer"),
    ("returning-silt-tongue", "route mark", "A second sounding on the old line", "the destination chart gains the new silt note and one point less hidden shoal risk"),
    ("fire-marked-raft", "merchant testimony", "Charcoal knots at the landing", "the local contact recognises the fire-marked salvage and records its origin"),
    ("grease-soaked-store", "deck scar", "The scrubbed galley board", "the galley retains an inspectable smoke scar and a counted fire-cover drill"),
    ("worked-seam", "repair memory", "The seam entered in the household share", "the carpenter-minded adult records the repair and improves one strained relationship"),
    ("thaw-surge", "route forecast", "Thaw water in the next account", "the destination receives a visible high-water forecast rather than an untelegraphed penalty"),
    ("counterclaim-inspection", "institutional claim", "The counterclaim finds a witness", "the destination account records who accepted the inspection and adjusts one obligation"),
))

BY_VARIANT = {row.variant_id: row for row in ECHOES}


def _actor(state: GameState, voyage: int):
    living = [person for person in state.household if person.alive]
    return living[voyage % len(living)] if living else None


def apply_later_echoes(state: GameState) -> list[VoyageEcho]:
    """Resolve prior variants on a later arrival; never echo the same voyage."""
    applied = []
    records = sorted(
        (int(key.rsplit(":", 1)[1]), value)
        for key, value in state.vessel_changes.items()
        if key.startswith("voyage_variant:") and key.rsplit(":", 1)[1].isdigit()
    )
    for voyage, variant_id in records:
        marker = f"voyage_echo:{voyage}:{variant_id}"
        if voyage >= state.travel_count or marker in state.vessel_changes or variant_id not in BY_VARIANT:
            continue
        echo = BY_VARIANT[str(variant_id)]
        state.vessel_changes[marker] = state.world_time
        region = state.regions.get(state.active_region_id)
        if region:
            region.changes[f"voyage-echo:{echo.variant_id}"] = echo.consequence
        actor = _actor(state, voyage)
        if actor:
            memory = f"{echo.title}: {echo.consequence}."
            actor.memories.append(memory)
            del actor.memories[:-8]
            if state.active_courier_id and actor.id != state.active_courier_id:
                actor.relationships[state.active_courier_id] = min(3, actor.relationships.get(state.active_courier_id, 0) + 1)
        account = state.institutions.get(f"work:{state.active_region_id}")
        node = state.route_nodes.get(state.route_current_node)
        if echo.kind in {"institutional claim", "cargo claim"} and account:
            account.confidence = min(3, account.confidence + 1)
            if "counterclaim" in echo.variant_id:
                account.obligation = max(0, account.obligation - 1)
        if echo.kind in {"route mark", "route forecast"} and node:
            node.risk = max(0, node.risk - 1)
            node.seasonal_note = echo.consequence
        if echo.kind == "rival preparation" and region:
            rival = next((a for a in state.region_threats[state.active_region_id] if a.elite and a.status in {"dormant", "watching"}), None)
            if rival:
                rival.supplies = min(8, rival.supplies + 1)
                rival.goal_reason = "followed a remembered shortage mark from Jomon's voyage"
        if echo.kind == "ecological return" and region:
            region.changes["population:voyage-displacement"] = "a displaced pair now uses a marked outer margin"
        if echo.kind == "merchant testimony":
            state.contact.memories.append(f"{echo.title}: recognised the physical voyage salvage.")
            del state.contact.memories[:-8]
        if echo.kind == "deck scar":
            state.vessel_changes[f"deck_scar:{echo.variant_id}"] = echo.consequence
        text = f"LATER ECHO — {echo.title}: {echo.consequence}."
        state.chronicle.append(text)
        del state.chronicle[:-24]
        state.remember(text)
        state.add_message(text, priority=3)
        applied.append(echo)
    return applied


def lines(state: GameState) -> list[str]:
    return [
        f"REMEMBERED VOYAGE — {echo.title}: {echo.consequence}."
        for echo in ECHOES
        if any(key.startswith("voyage_echo:") and key.endswith(":" + echo.variant_id) for key in state.vessel_changes)
    ]


def validate_echoes() -> None:
    from .voyage_variants import VARIANTS
    if len(ECHOES) != 12 or len(BY_VARIANT) != 12 or set(BY_VARIANT) != {row.id for row in VARIANTS.values()}:
        raise ValueError("every voyage variant needs one distinct later echo")
    if len({row.kind for row in ECHOES}) < 8 or any(not row.consequence for row in ECHOES):
        raise ValueError("voyage echoes need varied persistent consequences")


validate_echoes()
