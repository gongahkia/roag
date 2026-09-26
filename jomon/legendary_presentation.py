"""Resolve selected-pack prose for generated legends and stable arc relics."""
from __future__ import annotations
from .catalog import selected_content_pack

def legendary_text(semantic_id: str) -> str:
    return selected_content_pack().legendary_presentation(semantic_id).text

def legendary_format(semantic_id: str, /, **values: object) -> str:
    return legendary_text(semantic_id).format(**values)

def arc_relic_display_name(engine_id: str) -> str | None:
    key = f"legendary.arc.{engine_id.replace(' ', '-')}.display_name"
    try:
        return legendary_text(key)
    except KeyError:
        return None

def arc_relic_description(engine_id: str) -> str | None:
    key = f"legendary.arc.{engine_id.replace(' ', '-')}.description"
    try:
        return legendary_text(key)
    except KeyError:
        return None
