"""Resolve selected-pack presentation after travel mechanics choose a result."""
from __future__ import annotations
from .catalog import selected_content_pack

def travel_text(semantic_id: str) -> str:
    return selected_content_pack().travel_presentation(semantic_id).text

def travel_format(semantic_id: str, **values: object) -> str:
    return travel_text(semantic_id).format(**values)


def variant_display_name(variant_id: str) -> str:
    return travel_text(f"travel.variant.{variant_id}.name")


def variant_cause(variant_id: str) -> str:
    return travel_text(f"travel.variant.{variant_id}.cause")


def variant_effect(variant_id: str) -> str:
    return travel_text(f"travel.variant.{variant_id}.effect")


def variant_counterplay(variant_id: str) -> str:
    return travel_text(f"travel.variant.{variant_id}.counterplay")


def echo_title(variant_id: str) -> str:
    return travel_text(f"travel.echo.{variant_id}.title")


def echo_consequence(variant_id: str) -> str:
    return travel_text(f"travel.echo.{variant_id}.consequence")


def route_node_name(node) -> str:
    if node.region_id:
        from .region_presentation import region_route_label
        return region_route_label(node.region_id)
    return travel_text(f"travel.route.node.{node.id}.name")


def route_node_description(node) -> str:
    if node.region_id:
        from .region_presentation import region_short_description
        return region_short_description(node.region_id)
    return travel_text(f"travel.route.node.{node.id}.description")


def route_edge_hazard(edge) -> str:
    return travel_text(f"travel.route.edge.{edge.id}.hazard")
