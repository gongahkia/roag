"""One bounded later echo for every causal voyage variant."""

from __future__ import annotations

from dataclasses import dataclass

from .catalog import VESSEL_SECTIONS, load_catalog
from .state import GameState
from .travel_presentation import echo_consequence, echo_title, travel_format, travel_text


@dataclass(frozen=True)
class VoyageEcho:
    variant_id: str
    kind: str

    @property
    def title(self) -> str:
        return echo_title(self.variant_id)

    @property
    def consequence(self) -> str:
        return echo_consequence(self.variant_id)


ECHOES = tuple(VoyageEcho(row["variant_id"], row["kind"]) for row in load_catalog("vessel.json", VESSEL_SECTIONS)["echoes"])

BY_VARIANT = {row.variant_id: row for row in ECHOES}


def _actor(state: GameState, voyage: int):
    living = [person for person in state.household if person.alive]
    return living[voyage % len(living)] if living else None


def apply_later_echoes(state: GameState) -> list[VoyageEcho]:
    """Resolve at most one prior variant on an arrival; never echo its voyage."""
    applied = []
    records = sorted(
        (int(key.rsplit(":", 1)[1]), value)
        for key, value in state.vessel_changes.items()
        if key.startswith("voyage_variant:") and key.rsplit(":", 1)[1].isdigit()
    )
    for voyage, variant_id in records:
        marker = f"voyage_echo:{variant_id}"
        if voyage >= state.travel_count or marker in state.vessel_changes or variant_id not in BY_VARIANT:
            continue
        echo = BY_VARIANT[str(variant_id)]
        state.vessel_changes[marker] = state.world_time
        region = state.regions.get(state.active_region_id)
        if region:
            region.changes[f"voyage-echo:{echo.variant_id}"] = echo_consequence(echo.variant_id)
        actor = _actor(state, voyage)
        if actor:
            memory = travel_format("travel.echo.actor_memory", title=echo_title(echo.variant_id), consequence=echo_consequence(echo.variant_id))
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
            node.seasonal_note = echo_consequence(echo.variant_id)
        if echo.kind == "rival preparation" and region:
            rival = next((a for a in state.region_threats[state.active_region_id] if a.elite and a.status in {"dormant", "watching"}), None)
            if rival:
                rival.supplies = min(8, rival.supplies + 1)
                rival.goal_reason = travel_text("travel.echo.rival_goal")
        if echo.kind == "ecological return" and region:
            region.changes["population:voyage-displacement"] = travel_text("travel.echo.population")
        if echo.kind == "merchant testimony":
            state.contact.memories.append(travel_format("travel.echo.memory", title=echo_title(echo.variant_id)))
            del state.contact.memories[:-8]
        if echo.kind == "deck scar":
            state.vessel_changes[f"deck_scar:{echo.variant_id}"] = echo_consequence(echo.variant_id)
        text = travel_format("travel.echo.activation", title=echo_title(echo.variant_id), consequence=echo_consequence(echo.variant_id))
        state.chronicle.append(text)
        del state.chronicle[:-24]
        state.remember(text)
        state.add_message(text, priority=3)
        applied.append(echo)
        # One substantial callback per arrival leaves room for the current
        # voyage consequence, regional situation, and player-authored plans.
        # Remaining eligible echoes stay in their compact saved records.
        break
    return applied


def lines(state: GameState) -> list[str]:
    return [
        travel_format("travel.echo.remembered", title=echo_title(echo.variant_id), consequence=echo_consequence(echo.variant_id))
        for echo in ECHOES
        if f"voyage_echo:{echo.variant_id}" in state.vessel_changes
    ]


def validate_echoes() -> None:
    from .voyage_variants import VARIANTS
    if len(ECHOES) != 12 or len(BY_VARIANT) != 12 or set(BY_VARIANT) != {row.id for row in VARIANTS.values()}:
        raise ValueError("every voyage variant needs one distinct later echo")
    if len({row.kind for row in ECHOES}) < 8:
        raise ValueError("voyage echoes need varied persistent consequences")


def validate_echo_state(state: GameState) -> None:
    for key_name, value in state.vessel_changes.items():
        if key_name.startswith("voyage_echo:"):
            variant_id = key_name.split(":", 1)[1]
            if variant_id not in BY_VARIANT or not isinstance(value, int) or value < 0:
                raise ValueError("invalid later voyage echo")


validate_echoes()
