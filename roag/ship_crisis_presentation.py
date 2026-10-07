"""Resolve selected-pack wording for mechanically stable ship crises."""
from __future__ import annotations

from .catalog import selected_content_pack


def ship_crisis_text(semantic_id: str) -> str:
    return selected_content_pack().ship_crisis_presentation(semantic_id).text


def ship_crisis_format(semantic_id: str, **values: object) -> str:
    return ship_crisis_text(semantic_id).format(**values)


def crisis_title(kind: str) -> str:
    return ship_crisis_text(f"crisis.{kind}.title")


def crisis_description(kind: str) -> str:
    return ship_crisis_text(f"crisis.{kind}.description")


def crisis_goal_display(goal_id: str) -> str:
    """Render the small set of crisis-owned initial AI goals."""
    if goal_id == "crisis.cargo_claim":
        return ship_crisis_text("crisis.goal.cargo_claim")
    return goal_id
