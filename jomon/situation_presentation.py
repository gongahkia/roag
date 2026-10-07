"""Resolve selected-pack presentation for regional situations."""
from __future__ import annotations
from .catalog import selected_content_pack

def situation_text(semantic_id: str) -> str:
    return selected_content_pack().situation_presentation(semantic_id).text

def situation_format(semantic_id: str, **values: object) -> str:
    return situation_text(semantic_id).format(**values)

def situation_slot(situation_id: str, field: str) -> str:
    return situation_text(f"situation.{situation_id}.{field}")
