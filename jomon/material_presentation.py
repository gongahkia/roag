"""Resolve selected-pack presentation for material simulation and field inspection."""
from __future__ import annotations

from .catalog import selected_content_pack


def material_text(semantic_id: str) -> str:
    return selected_content_pack().material_presentation(semantic_id).text


def material_format(semantic_id: str, **values: object) -> str:
    return material_text(semantic_id).format(**values)


def material_display_name(material_id: str) -> str:
    return material_text(f"material.name.{material_id}")


def coating_display_name(coating_id: str) -> str:
    return material_text(f"material.coating.{coating_id or 'none'}")


def fluid_display_name(fluid_id: str) -> str:
    return material_text(f"material.fluid.{fluid_id}")


def hazard_display_name(hazard_id: str) -> str:
    return material_text(f"material.hazard.{hazard_id}")


def material_verb_display_name(verb_id: str) -> str:
    return material_text(f"material.verb.{verb_id}")
