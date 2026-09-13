"""Packaged ASCII assets and contextual glyph mappings for Jomon."""

from __future__ import annotations

from .catalog import CatalogError, VISUAL_SECTIONS, load_catalog


VISUALS = load_catalog("visuals.json", VISUAL_SECTIONS)


def _glyph(value: object, context: str) -> str:
    if not isinstance(value, str) or len(value) != 1 or not value.isascii() or not value.isprintable():
        raise CatalogError(f"{context} must be one printable ASCII character")
    return value


def _glyph_map(value: object, context: str) -> dict[str, str]:
    if not isinstance(value, dict) or any(not isinstance(key, str) for key in value):
        raise CatalogError(f"{context} must be a glyph map")
    return {key: _glyph(glyph, f"{context}.{key}") for key, glyph in value.items()}


ENTITY_GLYPHS = _glyph_map(
    {key: value for key, value in VISUALS["entity_glyphs"].items() if key != "threat_profiles"},
    "entity_glyphs",
)
THREAT_PROFILE_GLYPHS = _glyph_map(VISUALS["entity_glyphs"]["threat_profiles"], "threat_profiles")
SEMANTIC_GLYPH_ROLES = VISUALS["semantic_roles"]
REGIONAL_GROUND_ROLES = VISUALS["regional_ground_roles"]
REGIONAL_TILE_ROLES = VISUALS["regional_tile_roles"]
PHYSICAL_ROLE_OVERRIDES = VISUALS["physical_role_overrides"]
ROUTE_NODE_SYMBOLS = VISUALS["route_node_symbols"]
MATERIAL_OVERLAY_SYMBOLS = _glyph_map(VISUALS["material_overlay_symbols"], "material_overlay_symbols")
SITE_SYMBOLS = _glyph_map(VISUALS["site_symbols"], "site_symbols")
TAVERN_CARDS = VISUALS["tavern_cards"]
TAVERN_DICE = {int(face): tuple(rows) for face, rows in VISUALS["tavern_dice"].items()}
VESSEL_LEVELS = {int(level): tuple(rows) for level, rows in VISUALS["vessel_levels"].items()}
TAVERN_MAP = tuple(VISUALS["tavern_map"])

if set(VESSEL_LEVELS) != {-1, 0, 1} or any(len(rows) != 22 or any(len(row) != 64 for row in rows)
                                         for rows in VESSEL_LEVELS.values()):
    raise CatalogError("visuals.json vessel levels must be three 64x22 maps")
if len(TAVERN_MAP) != 24 or any(len(row) != 64 for row in TAVERN_MAP):
    raise CatalogError("visuals.json tavern map must be 64x24")
if set(TAVERN_DICE) != set(range(1, 7)) or any(len(rows) != 5 or any(len(row) != 9 for row in rows)
                                             for rows in TAVERN_DICE.values()):
    raise CatalogError("visuals.json must contain six five-line dice faces")
