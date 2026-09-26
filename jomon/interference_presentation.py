"""Resolve selected-pack prose for stable cross-region interference mechanics."""
from __future__ import annotations

from .catalog import selected_content_pack


def interference_text(semantic_id: str) -> str:
    return selected_content_pack().interference_presentation(semantic_id).text


def interference_format(semantic_id: str, /, **values: object) -> str:
    return interference_text(semantic_id).format(**values)


def interference_event_text(event_id: str, field: str) -> str:
    return interference_text(f"interference.{event_id}.{field}")
