"""Resolve selected-pack wording for stable vehicle mechanics."""
from __future__ import annotations
from .catalog import selected_content_pack

def vehicle_text(semantic_id: str) -> str:
    return selected_content_pack().vehicle_presentation(semantic_id).text

def vehicle_format(semantic_id: str, **values: object) -> str:
    return vehicle_text(semantic_id).format(**values)

def vehicle_display_name(vehicle_id: str) -> str:
    return vehicle_text(f"vehicle.{vehicle_id}.name")

def vehicle_resource_label(vehicle_id: str) -> str:
    return vehicle_text(f"vehicle.{vehicle_id}.resource")

def vehicle_domain_label(domain: str) -> str:
    return vehicle_text(f"vehicle.domain.{domain}")
