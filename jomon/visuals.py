"""Packaged ASCII assets and contextual glyph mappings for Jomon."""

from __future__ import annotations

from .catalog import CatalogError, VISUAL_SECTIONS, load_catalog


VISUALS = load_catalog("visuals.json", VISUAL_SECTIONS)


def _object(value: object, context: str) -> dict:
    if not isinstance(value, dict) or any(not isinstance(key, str) for key in value):
        raise CatalogError(f"{context} must be an object with text keys")
    return value


def _glyph(value: object, context: str) -> str:
    if not isinstance(value, str) or len(value) != 1 or not value.isascii() or not value.isprintable():
        raise CatalogError(f"{context} must be one printable ASCII character")
    return value


def _glyph_map(value: object, context: str) -> dict[str, str]:
    return {key: _glyph(glyph, f"{context}.{key}") for key, glyph in _object(value, context).items()}


def _rows(value: object, count: int, width: int, context: str) -> tuple[str, ...]:
    if (not isinstance(value, list) or len(value) != count
            or any(not isinstance(row, str) or len(row) != width or not row.isascii() or not row.isprintable()
                   for row in value)):
        raise CatalogError(f"{context} must contain {count} printable ASCII rows of width {width}")
    return tuple(value)


def _role_map(value: object, context: str) -> dict[str, str]:
    result = _object(value, context)
    if any(not isinstance(role, str) or not role for role in result.values()):
        raise CatalogError(f"{context} roles must be text")
    return {_glyph(glyph, context): role for glyph, role in result.items()}


_entities = _object(VISUALS["entity_glyphs"], "entity_glyphs")
ENTITY_GLYPHS = _glyph_map(
    {key: value for key, value in _entities.items() if key != "threat_profiles"},
    "entity_glyphs",
)
THREAT_PROFILE_GLYPHS = _glyph_map(_entities.get("threat_profiles"), "threat_profiles")
_semantic = _object(VISUALS["semantic_roles"], "semantic_roles")
if set(_semantic) != {"region", "aboard"}:
    raise CatalogError("semantic_roles must define region and aboard")
SEMANTIC_GLYPH_ROLES = {scope: _role_map(value, f"semantic_roles.{scope}") for scope, value in _semantic.items()}
REGIONAL_GROUND_ROLES = _object(VISUALS["regional_ground_roles"], "regional_ground_roles")
REGIONAL_TILE_ROLES = _role_map(VISUALS["regional_tile_roles"], "regional_tile_roles")
PHYSICAL_ROLE_OVERRIDES = _role_map(VISUALS["physical_role_overrides"], "physical_role_overrides")
ROUTE_NODE_SYMBOLS = _object(VISUALS["route_node_symbols"], "route_node_symbols")
if set(ROUTE_NODE_SYMBOLS) != {"current", "unknown", "default", "kinds"}:
    raise CatalogError("route_node_symbols needs current, unknown, default, and kinds")
for key in ("current", "unknown", "default"):
    _glyph(ROUTE_NODE_SYMBOLS[key], f"route_node_symbols.{key}")
_glyph_map(ROUTE_NODE_SYMBOLS["kinds"], "route_node_symbols.kinds")
MATERIAL_OVERLAY_SYMBOLS = _glyph_map(VISUALS["material_overlay_symbols"], "material_overlay_symbols")
SITE_SYMBOLS = _glyph_map(VISUALS["site_symbols"], "site_symbols")
TAVERN_CARDS = _object(VISUALS["tavern_cards"], "tavern_cards")
if (not isinstance(TAVERN_CARDS.get("ranks"), str) or len(TAVERN_CARDS["ranks"]) != 13
        or not isinstance(TAVERN_CARDS.get("suits"), str) or len(TAVERN_CARDS["suits"]) != 4
        or any(not isinstance(TAVERN_CARDS.get(key), str) or len(TAVERN_CARDS[key]) != 9
               for key in ("edge", "selected_edge"))
        or not isinstance(TAVERN_CARDS.get("frame"), list) or len(TAVERN_CARDS["frame"]) != 7):
    raise CatalogError("tavern_cards needs ranks, suits, nine-column edges, and seven frame rows")
try:
    if any(len(row.format(edge=TAVERN_CARDS["edge"], label_left="AS", suit="S", label_right="AS")) != 9
           for row in TAVERN_CARDS["frame"]):
        raise CatalogError("tavern card frame rows must be nine columns")
except (AttributeError, KeyError, ValueError) as exc:
    raise CatalogError(f"invalid tavern card frame: {exc}") from exc
_dice = _object(VISUALS["tavern_dice"], "tavern_dice")
if set(_dice) != {str(face) for face in range(1, 7)}:
    raise CatalogError("tavern_dice needs faces one through six")
TAVERN_DICE = {int(face): _rows(rows, 5, 9, f"tavern_dice.{face}") for face, rows in _dice.items()}
_levels = _object(VISUALS["vessel_levels"], "vessel_levels")
if set(_levels) != {"-1", "0", "1"}:
    raise CatalogError("vessel_levels needs lower, main, and upper decks")
VESSEL_LEVELS = {int(level): _rows(rows, 22, 64, f"vessel_levels.{level}") for level, rows in _levels.items()}
TAVERN_MAP = _rows(VISUALS["tavern_map"], 24, 64, "tavern_map")
