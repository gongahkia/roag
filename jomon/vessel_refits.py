"""Optional material vessel refits attached to physical Jomon stations."""

from __future__ import annotations

from dataclasses import dataclass

from .catalog import VESSEL_SECTIONS, load_catalog
from .item_presentation import item_display_name_or_legacy
from .vessel_presentation import (
    refit_display_name,
    refit_drawback,
    refit_effect,
    refit_station_display_name,
    vessel_format,
    vessel_text,
)


@dataclass(frozen=True)
class VesselRefit:
    id: str
    station: str
    dependency: str
    credit: int


REFITS = {
    row["id"]: VesselRefit(
        id=row["id"], station=row["station"], dependency=row["dependency"], credit=row["credit"]
    )
    for row in load_catalog("vessel.json", VESSEL_SECTIONS)["refits"]
}

STATION_REFITS = {
    station: tuple(refit.id for refit in REFITS.values() if refit.station == station)
    for station in {refit.station for refit in REFITS.values()}
}


def installed(state, refit_id: str) -> bool:
    return bool(state.vessel_changes.get(f"refit:{refit_id}"))


def refit_station_at(state) -> str | None:
    from .world import base_tile

    return {
        "G": "galley", "U": "bilge", "R": "repair", "S": "storage",
        "N": "helm", "b": "berths", "O": "lookout",
    }.get(base_tile(state, state.position))


def installation_status(state, refit_id: str) -> tuple[bool, str]:
    refit = REFITS[refit_id]
    if installed(state, refit_id):
        return False, vessel_text("vessel.refit.status.already")
    if state.location != "jomon" or state.jomon_space != "vessel" or state.combat_active:
        return False, vessel_text("vessel.refit.status.location")
    if refit_station_at(state) != refit.station:
        return False, vessel_format(
            "vessel.refit.status.station", station=refit_station_display_name(refit.station)
        )
    stack = state.vessel_cargo.get(refit.dependency)
    if not stack or stack.quantity <= 0:
        return False, vessel_format(
            "vessel.refit.status.cargo", cargo=item_display_name_or_legacy(refit.dependency)
        )
    if state.trade_credit < refit.credit:
        return False, vessel_format("vessel.refit.status.credit", credit=refit.credit)
    return True, vessel_text("vessel.refit.status.ready")


def install_refit(state, refit_id: str) -> tuple[bool, str]:
    available, reason = installation_status(state, refit_id)
    refit = REFITS[refit_id]
    refit_name = refit_display_name(refit_id)
    if not available:
        return False, vessel_format("vessel.refit.install.unavailable", refit=refit_name, reason=reason)
    stack = state.vessel_cargo[refit.dependency]
    stack.quantity -= 1
    if stack.quantity == 0:
        del state.vessel_cargo[refit.dependency]
    state.trade_credit -= refit.credit
    state.vessel_changes[f"refit:{refit_id}"] = True
    state.vessel_changes[f"refit-station:{refit_id}"] = refit.station
    from .actions import _advance_world

    steps = 2 if state.courier and "station-repair" in state.courier.skill_nodes else 3
    _advance_world(state, steps=steps)
    text = vessel_format(
        "vessel.refit.install.result",
        refit=refit_name,
        station=refit_station_display_name(refit.station),
        effect=refit_effect(refit_id),
        cargo=item_display_name_or_legacy(refit.dependency),
        credit=refit.credit,
        actions=steps,
        drawback=refit_drawback(refit_id),
    )
    state.chronicle.append(text)
    del state.chronicle[:-40]
    state.remember(text)
    state.add_message(text, priority=3)
    return True, text


def validate_refits() -> None:
    if len(REFITS) != 11 or len(set(REFITS)) != 11:
        raise ValueError("Jomon needs eleven distinct vessel refits")
    valid_stations = {"galley", "bilge", "repair", "storage", "helm", "berths", "lookout"}
    if not set(STATION_REFITS) <= valid_stations:
        raise ValueError("refit references an unknown physical vessel station")
    if any(refit.credit < 1 for refit in REFITS.values()):
        raise ValueError("each vessel refit needs an accountable cost")


validate_refits()
