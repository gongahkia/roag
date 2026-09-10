"""Eight optional, material vessel refits attached to physical Jomon stations."""

from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class VesselRefit:
    id: str
    name: str
    station: str
    dependency: str
    credit: int
    effect: str
    drawback: str


REFITS = {
    refit.id: refit
    for refit in (
        VesselRefit("galley-fire-cover", "Galley fire cover", "galley", "wool", 2, "Galley fire starts with less fuel; emergency work and meals take one fewer action.", "The cover occupies dry wool that cannot be traded."),
        VesselRefit("twin-bilge-strainers", "Twin bilge strainers", "bilge", "ironwork", 3, "Flood crises admit less initial water and pumping takes one action.", "Strainers do not repair an open hull seam."),
        VesselRefit("storm-backstay", "Storm backstay", "repair", "timber", 3, "Storm supports start stronger and emergency work no longer requires a readied rope.", "A failing stay still needs the courier at its physical upper-deck station."),
        VesselRefit("cargo-rail-netting", "Cargo rail netting", "storage", "wool", 3, "The first cargo loss in each voyage is caught and deck thieves start entangled.", "Cut netting makes the next loss ordinary; it does not defeat boarders."),
        VesselRefit("sounding-keel-shoes", "Sounding keel shoes", "helm", "ironwork", 4, "Careful shoal work takes one fewer action and a forced scrape costs one integrity.", "The extra iron makes severe mineral resonance more conspicuous."),
        VesselRefit("winter-hatch-felt", "Winter hatch felt", "berths", "wool", 2, "Winter route exposure falls and aboard floodwater does not chill the courier.", "Felt does not prevent wetness, load, or regional cold."),
        VesselRefit("signal-mast-shutter", "Signal mast shutter", "lookout", "timber", 3, "Named signals answer lure and inspection voyages without specialist support.", "The visible answer records Jomon on the route account."),
        VesselRefit("sickbay-sling-cot", "Sickbay sling cot", "berths", "timber", 3, "A berth treatment can spend wool and six actions to clear one persistent injury.", "Treatment consumes time and material; it does not restore lost health."),
    )
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
        return False, "already installed"
    if state.location != "jomon" or state.jomon_space != "vessel" or state.combat_active:
        return False, "Jomon must be moored and free of active danger"
    if refit_station_at(state) != refit.station:
        return False, f"work must be confirmed at Jomon's {refit.station} station"
    stack = state.vessel_cargo.get(refit.dependency)
    if not stack or stack.quantity <= 0:
        return False, f"needs one physical {refit.dependency} hold lot"
    if state.trade_credit < refit.credit:
        return False, f"needs {refit.credit} accountable credit"
    return True, "ready"


def install_refit(state, refit_id: str) -> tuple[bool, str]:
    available, reason = installation_status(state, refit_id)
    refit = REFITS[refit_id]
    if not available:
        return False, f"{refit.name} cannot be fitted: {reason}."
    stack = state.vessel_cargo[refit.dependency]
    stack.quantity -= 1
    if stack.quantity == 0:
        del state.vessel_cargo[refit.dependency]
    state.trade_credit -= refit.credit
    state.vessel_changes[f"refit:{refit_id}"] = True
    state.vessel_changes[f"refit-station:{refit_id}"] = refit.station
    from .actions import _advance_world

    _advance_world(state, steps=3)
    text = (
        f"{refit.name} is installed at Jomon's {refit.station}: {refit.effect} "
        f"Cost: one {refit.dependency}, {refit.credit} credit and three actions. {refit.drawback}"
    )
    state.chronicle.append(text)
    del state.chronicle[:-40]
    state.remember(text)
    state.add_message(text, priority=3)
    return True, text


def validate_refits() -> None:
    if len(REFITS) != 8 or len(set(REFITS)) != 8 or len({refit.name for refit in REFITS.values()}) != 8:
        raise ValueError("Jomon needs eight distinct vessel refits")
    valid_stations = {"galley", "bilge", "repair", "storage", "helm", "berths", "lookout"}
    if not set(STATION_REFITS) <= valid_stations:
        raise ValueError("refit references an unknown physical vessel station")
    if any(not refit.effect or not refit.drawback or refit.credit < 1 for refit in REFITS.values()):
        raise ValueError("each vessel refit needs effect, drawback and accountable cost")


validate_refits()
