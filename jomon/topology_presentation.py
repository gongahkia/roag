"""Resolve selected-pack presentation for Hearthford's fixed topology slots."""
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
    try:
        return topology_text(f"topology.hearthford.landmark.{landmark_id}")
    except KeyError:
        return None
