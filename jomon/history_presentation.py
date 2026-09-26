"""Resolve validated selected-pack wording for deterministic regional history."""
from __future__ import annotations

from .catalog import selected_content_pack


def history_text(semantic_id: str) -> str:
    return selected_content_pack().history_presentation(semantic_id).text


def history_format(semantic_id: str, /, **values: object) -> str:
    return history_text(semantic_id).format(**values)
