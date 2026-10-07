"""Resolve generated-person presentation without exposing pack text to mechanics."""
from __future__ import annotations
from .catalog import selected_content_pack

def household_first_name(slot: int) -> str:
    return selected_content_pack().people_name(f"first_{slot}")

def household_family_name(slot: int) -> str:
    return selected_content_pack().people_name(f"family_{slot}")

def contact_name(slot: int) -> str:
    return selected_content_pack().people_name(f"contact_{slot}")

def contact_role(slot: int) -> str:
    return selected_content_pack().people_name(f"role_{slot}")

def regional_context(slot: int) -> dict[str, str]:
    return selected_content_pack().people_context(f"context_{slot}")

def recruit_presentation(recruit_id: str) -> dict[str, str]:
    return selected_content_pack().people_recruit(recruit_id)
