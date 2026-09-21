"""Resolve selected-pack presentation for fixed regional topology slots."""
from __future__ import annotations

from .catalog import selected_content_pack


def topology_text(semantic_id: str) -> str:
    return selected_content_pack().topology_presentation(semantic_id).text


def hearthford_zone_name(zone_id: str) -> str:
    return topology_text(f"topology.hearthford.zone.{zone_id}")


def hearthford_container_name(container_id: str) -> str:
    return topology_text(f"topology.hearthford.container.{container_id}")


def hearthford_link_name(link_id: str) -> str:
    return topology_text(f"topology.hearthford.link.{link_id}")


def hearthford_landmark_label(landmark_id: str) -> str | None:
    return regional_landmark_label("hearthford", landmark_id)


def regional_generator_text(region_id: str, field: str) -> str:
    return topology_text(f"topology.{region_id}.{field}")


def regional_zone_name(region_id: str, zone_id: str) -> str:
    return topology_text(f"topology.{region_id}.zone.{zone_id}")


def regional_container_name(region_id: str, container_id: str) -> str:
    return topology_text(f"topology.{region_id}.container.{container_id.replace('-', '_')}")


def regional_link_name(region_id: str, link_id: str) -> str:
    return topology_text(f"topology.{region_id}.link.{link_id}")


def regional_contact_name(region_id: str, contact_index: int) -> str:
    return topology_text(f"topology.{region_id}.contact.{contact_index}.name")


def regional_contact_role(region_id: str, contact_index: int) -> str:
    return topology_text(f"topology.{region_id}.contact.{contact_index}.role")


def regional_contact_memory(region_id: str, contact_index: int, **values: str) -> str:
    return topology_text(f"topology.{region_id}.contact.{contact_index}.memory").format(**values)


def regional_generator_format(region_id: str, field: str, **values: str) -> str:
    return regional_generator_text(region_id, field).format(**values)


def regional_landmark_label(region_id: str, landmark_id: str) -> str | None:
    try:
        return topology_text(f"topology.{region_id}.landmark.{landmark_id}")
    except KeyError:
        return None
