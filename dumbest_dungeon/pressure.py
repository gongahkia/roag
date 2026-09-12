"""Deterministic Expedition Pressure bands and disclosed action prices."""

from __future__ import annotations

from dataclasses import dataclass
from enum import StrEnum


class PressureSource(StrEnum):
    TRAVEL = "travel"
    ENEMY_ROUND = "enemy_round"
    OBJECTIVE_STAGE = "objective_stage"
    FACILITY = "facility"
    EVENT = "event"
    BARGAIN = "bargain"
    POWER_REWARD = "power_reward"
    LOOP_ENTRY = "loop_entry"


@dataclass(frozen=True)
class PressureBand:
    id: str
    name: str
    threshold: int
    forecast: str


BANDS = (
    PressureBand("quiet", "QUIET", 0, "Ordinary patrol plans and ordinary reward lanes."),
    PressureBand("watchful", "WATCHFUL", 240, "Coordinated plans and stranger rewards become available."),
    PressureBand("hunted", "HUNTED", 480, "Visible elite mutations and reinforcement plans become available."),
    PressureBand("lockdown", "LOCKDOWN", 760, "Advanced actions and stronger regional hazards become available."),
    PressureBand("overrun", "OVERRUN", 1100, "Loop-grade modules and paired power burdens become available."),
)

# Prices are simulation actions, never wall-clock or interface activity.
ACTION_PRESSURE = {
    PressureSource.TRAVEL: 1,          # per weighted travel tick
    PressureSource.ENEMY_ROUND: 8,     # per completed enemy phase
    PressureSource.OBJECTIVE_STAGE: 18,
    PressureSource.FACILITY: 24,
    PressureSource.EVENT: 0,           # an authored choice must opt in
    PressureSource.BARGAIN: 0,         # an authored bargain must opt in
    PressureSource.POWER_REWARD: 0,    # an authored reward must opt in
    PressureSource.LOOP_ENTRY: 1100,
}


def pressure_band(value: int) -> PressureBand:
    if type(value) is not int or value < 0:
        raise ValueError("pressure must be a nonnegative integer")
    return next(band for band in reversed(BANDS) if value >= band.threshold)


def pressure_status(value: int) -> dict[str, int | str | None]:
    band = pressure_band(value)
    index = BANDS.index(band)
    following = BANDS[index + 1] if index + 1 < len(BANDS) else None
    return {
        "value": value,
        "band": band.id,
        "name": band.name,
        "band_floor": band.threshold,
        "progress": value - band.threshold,
        "next_threshold": following.threshold if following else None,
        "remaining": following.threshold - value if following else None,
        "forecast": following.forecast if following else "No higher base-expedition band.",
    }


def action_price(source: PressureSource, units: int = 1) -> int:
    if not isinstance(source, PressureSource) or type(units) is not int or units < 0:
        raise ValueError("pressure actions require a registered source and nonnegative units")
    return ACTION_PRESSURE[source] * units
