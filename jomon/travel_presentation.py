"""Resolve selected-pack presentation after travel mechanics choose a result."""
from __future__ import annotations
from .catalog import selected_content_pack

def travel_text(semantic_id: str) -> str:
    return selected_content_pack().travel_presentation(semantic_id).text

def travel_format(semantic_id: str, **values: object) -> str:
    return travel_text(semantic_id).format(**values)
