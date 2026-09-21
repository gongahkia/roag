"""Resolve selected-pack action narration after combat mechanics choose an outcome."""
from __future__ import annotations
from .catalog import selected_content_pack

def action_text(semantic_id: str) -> str:
    return selected_content_pack().action_presentation(semantic_id).text

def action_format(semantic_id: str, **values: object) -> str:
    return action_text(semantic_id).format(**values)
