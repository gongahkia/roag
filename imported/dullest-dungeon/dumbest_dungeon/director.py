"""Bounded qualitative director profiles selected only by visible Pressure."""

from __future__ import annotations

from dataclasses import dataclass

from .pressure import BANDS, pressure_band


@dataclass(frozen=True)
class DirectorProfile:
    band: str
    coordination: int
    patrol_aggression: int
    patrol_cadence_reduction: int
    reward_choices: int
    mutation_slots: int
    reinforcement_tickets: int
    hazard_reach: int
    enemy_health_bp: int
    enemy_damage_bp: int


PROFILES = {
    "quiet": DirectorProfile("quiet", 0, 0, 0, 0, 0, 0, 0, 10_000, 10_000),
    "watchful": DirectorProfile("watchful", 1, 1, 0, 0, 0, 0, 0, 10_000, 10_000),
    "hunted": DirectorProfile("hunted", 2, 2, 1, 1, 1, 0, 0, 10_300, 10_000),
    "lockdown": DirectorProfile("lockdown", 3, 3, 1, 1, 2, 1, 1, 10_500, 10_300),
    "overrun": DirectorProfile("overrun", 4, 4, 2, 2, 3, 2, 2, 10_800, 10_500),
}


def director_profile(pressure: int) -> DirectorProfile:
    return PROFILES[pressure_band(pressure).id]


def validate_profiles() -> None:
    if tuple(PROFILES) != tuple(band.id for band in BANDS):
        raise ValueError("director profiles must exactly cover canonical Pressure bands")
    profiles = tuple(PROFILES.values())
    integer_fields = tuple(
        field
        for field in DirectorProfile.__dataclass_fields__
        if field != "band"
    )
    for profile in profiles:
        if any(type(getattr(profile, field)) is not int for field in integer_fields):
            raise ValueError("director profile values must be integers")
        if not 10_000 <= profile.enemy_health_bp <= 11_000:
            raise ValueError("director health scaling exceeds its bounded 10% envelope")
        if not 10_000 <= profile.enemy_damage_bp <= 10_500:
            raise ValueError("director damage scaling exceeds its bounded 5% envelope")
    for before, after in zip(profiles, profiles[1:]):
        if any(getattr(after, field) < getattr(before, field) for field in integer_fields):
            raise ValueError("director profiles must be monotonic")


validate_profiles()
