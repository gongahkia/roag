"""Resolve validated selected-pack wording for deterministic regional history."""
from __future__ import annotations

from .catalog import selected_content_pack


def history_text(semantic_id: str) -> str:
    return selected_content_pack().history_presentation(semantic_id).text


def history_format(semantic_id: str, /, **values: object) -> str:
    return history_text(semantic_id).format(**values)


def history_region_text(region_id: str, field: str) -> str:
    return history_text(f"history.region.{region_id}.{field}")


def history_network_text(network_id: str, field: str) -> str:
    return history_text(f"history.network.{network_id.removeprefix('network:')}.{field}")


def history_contact_text(contact_id: str, field: str) -> str:
    return history_text(f"history.contact.{contact_id}.{field}")


def history_value(category: str, identity: str) -> str:
    return history_text(f"history.value.{category}.{identity}")
