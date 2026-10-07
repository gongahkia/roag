"""Resolve selected-pack wording for stable sanctum mechanics."""
from __future__ import annotations

from .catalog import selected_content_pack


def sanctum_text(semantic_id: str) -> str:
    return selected_content_pack().sanctum_presentation(semantic_id).text


def sanctum_format(semantic_id: str, **values: object) -> str:
    return sanctum_text(semantic_id).format(**values)


def sanctum_display_name(region_id: str) -> str:
    return sanctum_text(f"sanctum.{region_id}.name")


def sanctum_theme(region_id: str) -> str:
    return sanctum_text(f"sanctum.{region_id}.theme")


def sanctum_boss_name(region_id: str) -> str:
    return sanctum_text(f"sanctum.{region_id}.boss.name")


def sanctum_boss_goal(region_id: str) -> str:
    return sanctum_text(f"sanctum.{region_id}.boss.goal")
