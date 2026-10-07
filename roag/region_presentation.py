"""Resolve engine region identities to selected-pack presentation."""

from __future__ import annotations

from .catalog import RegionPresentation, selected_content_pack


def region_presentation(engine_id: str) -> RegionPresentation:
    """Return immutable presentation for a legacy engine region ID."""
    return selected_content_pack().region_presentation(engine_id)


def region_display_name(engine_id: str) -> str:
    return region_presentation(engine_id).display_name


def region_route_label(engine_id: str) -> str:
    return region_presentation(engine_id).route_label


def region_short_description(engine_id: str) -> str:
    return region_presentation(engine_id).short_description
