"""Renderer-facing asset resolution for selected content packs.

This module is deliberately outside mechanics: it maps stable semantic IDs to
logical asset IDs or legacy curses glyphs and never loads media bytes.
"""
from __future__ import annotations
from .catalog import AssetResource, selected_content_pack


def curses_glyph(semantic_cell_id: str, fallback: str) -> str:
    """Return a selected-pack ASCII glyph for legacy curses rendering."""
    try:
        return selected_content_pack().assets.glyph(semantic_cell_id)
    except KeyError:
        return fallback


def asset_binding(category: str, semantic_id: str) -> dict[str, str]:
    """Resolve logical presentation assets; an empty map means no binding."""
    return selected_content_pack().assets.binding(category, semantic_id)


def asset_resource(asset_id: str) -> AssetResource | None:
    """Return a pack-relative descriptor, never a loaded renderer resource."""
    return selected_content_pack().assets.resource(asset_id)


def actor_assets(archetype_id: str) -> dict[str, str]:
    return asset_binding("actors", archetype_id) or asset_binding("actors", "actor.default")


def terrain_assets(terrain_id: str) -> dict[str, str]:
    return asset_binding("terrain", terrain_id)


def action_assets(action_id: str) -> dict[str, str]:
    return asset_binding("actions", action_id) or asset_binding("actions", "attack.default")


def event_assets(event_id: str) -> dict[str, str]:
    return asset_binding("events", event_id)


def tavern_assets(semantic_id: str) -> dict[str, str]:
    """Logical tavern presentation resources, never gameplay resources."""
    return asset_binding("tavern", semantic_id)
