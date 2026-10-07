"""Resolve selected-pack wording for stable advanced-equipment mechanics."""
from __future__ import annotations
from .catalog import selected_content_pack

def equipment_text(semantic_id: str) -> str:
    return selected_content_pack().equipment_presentation(semantic_id).text

def equipment_format(semantic_id: str, **values: object) -> str:
    return equipment_text(semantic_id).format(**values)

def _key(raw_id: str) -> str:
    return raw_id.replace(" ", "_")

def work_weapon_name(raw_id: str) -> str:
    return equipment_text(f"equipment.weapon.work.{_key(raw_id)}.name")

def work_weapon_description(raw_id: str) -> str:
    return equipment_text(f"equipment.weapon.work.{_key(raw_id)}.description")

def arsenal_weapon_name(raw_id: str) -> str:
    return equipment_text(f"equipment.weapon.arsenal.{_key(raw_id)}.name")

def fitting_name(raw_id: str) -> str:
    return equipment_text(f"equipment.fitting.{_key(raw_id)}.name")

def fitting_effect(raw_id: str) -> str:
    return equipment_text(f"equipment.fitting.{_key(raw_id)}.effect")

def fitting_drawback(raw_id: str) -> str:
    return equipment_text(f"equipment.fitting.{_key(raw_id)}.drawback")


def ammunition_display_name(raw_id: str) -> str:
    return equipment_text(f"equipment.ammunition.{_key(raw_id)}.name")


def ammunition_description(raw_id: str) -> str:
    return equipment_text(f"equipment.ammunition.{_key(raw_id)}.description")


def equipment_effect_name(effect_id: str) -> str:
    return equipment_text(f"equipment.effect.{effect_id}.name")


def equipment_family_name(family_id: str) -> str:
    return equipment_text(f"equipment.family.{family_id}.name")
