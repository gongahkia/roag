"""Resolve selected-pack wording for vessel-owned shipboard presentation."""
from __future__ import annotations

from .catalog import selected_content_pack


_SCHEDULE_IDS = {
    "ready for departure": "ready_departure",
    "working at a regional site": "regional_work",
    "resting near home": "regional_rest",
    "serving": "serving",
    "sleeping": "sleeping",
    "trading from a counted berth": "merchant_present",
    "away on a regional circuit": "merchant_away",
    "securing the tavern": "securing_tavern",
    "defending cargo": "defending_cargo",
    "bracing the hull": "bracing_hull",
    "answering named crew": "answering_crew",
    "playing Dullest Dungeon": "dullest",
    "playing Tavern Draw": "draw",
    "playing Quay Bones": "bones",
    "eating": "eating",
    "drinking": "drinking",
    "socialising": "socialising",
    "waiting": "waiting",
    "standing watch": "watch",
    "steering": "steering",
    "consulting chart": "chart",
    "repairing": "repairing",
    "moving cargo": "cargo",
    "treating injuries": "treating",
    "resting": "resting",
    "training": "training",
    "working": "working",
    "between watches": "between_watches",
}


def vessel_text(semantic_id: str) -> str:
    return selected_content_pack().vessel_presentation(semantic_id).text


def vessel_format(semantic_id: str, **values: object) -> str:
    return vessel_text(semantic_id).format(**values)


def drink_display_name(drink_id: str) -> str:
    return vessel_text(f"vessel.drink.{drink_id}.name")


def drink_benefit(drink_id: str) -> str:
    return vessel_text(f"vessel.drink.{drink_id}.benefit")


def drink_drawback(drink_id: str) -> str:
    return vessel_text(f"vessel.drink.{drink_id}.drawback")


def schedule_display_name(activity: str, *, absent: str | None = None) -> str:
    if activity in _SCHEDULE_IDS:
        return vessel_text(f"vessel.schedule.{_SCHEDULE_IDS[activity]}")
    if absent == "duties":
        return vessel_text("vessel.schedule.between_duties")
    if absent == "routes":
        return vessel_text("vessel.schedule.between_routes")
    return activity
