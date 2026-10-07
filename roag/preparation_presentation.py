"""Resolve selected-pack presentation for stable preparation mechanics."""
from __future__ import annotations

from .catalog import selected_content_pack


def preparation_text(semantic_id: str) -> str:
    return selected_content_pack().preparation_presentation(semantic_id).text


def preparation_format(semantic_id: str, **values: object) -> str:
    return preparation_text(semantic_id).format(**values)


def preparation_display_name(preparation_id: str) -> str:
    return preparation_text(f"{preparation_id}.name")


def preparation_description(preparation_id: str) -> str:
    return preparation_text(f"{preparation_id}.description")


def preparation_condition(preparation_id: str) -> str:
    return preparation_text(f"{preparation_id}.condition")
