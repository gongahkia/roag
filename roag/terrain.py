"""Semantic regional terrain over format-15 glyph-backed maps.

Region rows and sparse ``tile_changes`` remain authoritative persistence. This
module gives mechanical consumers stable identities and properties without
requiring a map or save migration.
"""

from __future__ import annotations

from dataclasses import dataclass
from functools import lru_cache
from types import MappingProxyType
from typing import Mapping

from .state import Position, Region


@dataclass(frozen=True)
class TerrainDefinition:
    id: str
    glyph: str
    walkable: bool
    blocks_sight: bool
    cover: str = "open"
    tags: tuple[str, ...] = ()
    material: str | None = None
    destructible: bool = False
    hardness: int = 0
    tool_actions: tuple[str, ...] = ()
    replacement_glyph: str | None = None
    action_sound: int = 0
    yield_material: str | None = None
    yield_fuel: int = 0
    yield_item_kind: str | None = None
    yield_item_quantity: int = 0


@dataclass(frozen=True)
class TerrainCatalog:
    definitions: Mapping[str, TerrainDefinition]
    regional_ground: Mapping[str, TerrainDefinition]

    def resolve(self, glyph: str, region_id: str | None = None) -> TerrainDefinition:
        if not isinstance(glyph, str) or len(glyph) != 1:
            raise ValueError("terrain glyph must be exactly one character")
        if glyph == "." and region_id in self.regional_ground:
            return self.regional_ground[region_id]
        return self.definitions.get(glyph) or _legacy_definition(glyph)


def _definition(
    identity: str,
    glyph: str,
    *,
    walkable: bool = True,
    blocks_sight: bool = False,
    cover: str = "open",
    tags: tuple[str, ...] = (),
    material: str | None = None,
    destructible: bool = False,
    hardness: int = 0,
    tool_actions: tuple[str, ...] = (),
    replacement_glyph: str | None = None,
    action_sound: int = 0,
    yield_material: str | None = None,
    yield_fuel: int = 0,
    yield_item_kind: str | None = None,
    yield_item_quantity: int = 0,
) -> TerrainDefinition:
    if cover not in {"open", "partial", "low"}:
        raise ValueError(f"invalid terrain cover kind {cover!r}")
    if destructible != bool(hardness and tool_actions and replacement_glyph):
        raise ValueError(f"invalid destruction definition for {identity!r}")
    if replacement_glyph is not None and len(replacement_glyph) != 1:
        raise ValueError(f"invalid replacement glyph for {identity!r}")
    if hardness < 0 or action_sound < 0 or yield_fuel < 0:
        raise ValueError(f"negative terrain property for {identity!r}")
    if (
        type(yield_item_quantity) is not int
        or yield_item_quantity < 0
        or (
            yield_item_kind is not None
            and (not isinstance(yield_item_kind, str) or not yield_item_kind)
        )
        or bool(yield_item_kind) != bool(yield_item_quantity)
    ):
        raise ValueError(f"invalid physical yield for {identity!r}")
    return TerrainDefinition(
        identity,
        glyph,
        walkable,
        blocks_sight,
        cover,
        tuple(sorted(tags)),
        material,
        destructible,
        hardness,
        tuple(tool_actions),
        replacement_glyph,
        action_sound,
        yield_material,
        yield_fuel,
        yield_item_kind,
        yield_item_quantity,
    )


_DEFINITIONS = {
    " ": _definition("terrain.region.void", " ", walkable=False, tags=("void", "protected")),
    "#": _definition("terrain.region.wall", "#", walkable=False, blocks_sight=True, cover="partial", tags=("solid", "structure")),
    "~": _definition("terrain.region.deep_water", "~", walkable=False, tags=("water",)),
    "T": _definition(
        "terrain.region.standing_timber", "T", walkable=False,
        blocks_sight=True, cover="partial",
        tags=("ordinary", "solid", "timber", "vegetation"),
        material="timber", destructible=True, hardness=3,
        tool_actions=("cut",), replacement_glyph=".", action_sound=4,
        yield_material="timber", yield_fuel=4,
        yield_item_kind="commodity:timber", yield_item_quantity=1,
    ),
    "+": _definition("terrain.region.closed_door", "+", blocks_sight=True, cover="partial", tags=("door", "authored", "protected")),
    "/": _definition("terrain.region.open_door", "/", tags=("door", "authored", "protected")),
    "%": _definition("terrain.region.loose_cover", "%", cover="low", tags=("cover",)),
    ".": _definition("terrain.region.ground", ".", tags=("ground",)),
    ",": _definition("terrain.region.shallow_water", ",", tags=("water",)),
    ":": _definition("terrain.region.wet_shore", ":", tags=("shore",)),
    ";": _definition(
        "terrain.region.reeds", ";", tags=("ordinary", "vegetation"),
        material="reeds", destructible=True, hardness=1,
        tool_actions=("cut", "dig"), replacement_glyph=".", action_sound=2,
        yield_material="reeds", yield_fuel=2,
        yield_item_kind="material:reeds", yield_item_quantity=1,
    ),
    '"': _definition(
        "terrain.region.dense_reeds", '"', tags=("ordinary", "vegetation"),
        material="reeds", destructible=True, hardness=2,
        tool_actions=("cut", "dig"), replacement_glyph=".", action_sound=2,
        yield_material="reeds", yield_fuel=2,
        yield_item_kind="material:reeds", yield_item_quantity=2,
    ),
    "_": _definition("terrain.region.ice", "_", tags=("ice",)),
    "m": _definition(
        "terrain.region.mud", "m", tags=("earth", "ordinary", "soft"),
        material="soil", destructible=True, hardness=1,
        tool_actions=("dig",), replacement_glyph=".", action_sound=1,
        yield_material="soil",
        yield_item_kind="ingredient:clay", yield_item_quantity=1,
    ),
    "r": _definition("terrain.region.scree", "r", tags=("earth", "stone")),
    "q": _definition("terrain.region.sharp_limestone", "q", tags=("stone",)),
    "t": _definition("terrain.region.dense_growth", "t", tags=("vegetation",)),
    "w": _definition("terrain.region.current", "w", tags=("water",)),
    "=": _definition("terrain.region.worked_timber", "=", tags=("road", "timber")),
    "<": _definition("terrain.region.upward_connection", "<", tags=("vertical", "authored", "protected")),
    ">": _definition("terrain.region.downward_connection", ">", tags=("vertical", "authored", "protected")),
    "O": _definition("terrain.region.open_drop", "O", tags=("vertical", "authored", "protected")),
    "^": _definition("terrain.region.roof", "^", tags=("structure",)),
    "d": _definition("terrain.region.fragile_floor", "d", tags=("structure", "authored", "protected")),
    "&": _definition("terrain.region.working_control", "&", tags=("feature", "authored", "protected")),
    "R": _definition("terrain.region.cargo_marker", "R", tags=("feature", "authored", "protected")),
    "C": _definition("terrain.region.store_marker", "C", tags=("feature", "authored", "protected")),
    "o": _definition("terrain.region.open_store_marker", "o", tags=("feature", "authored", "protected")),
    "M": _definition("terrain.region.contact_marker", "M", tags=("feature", "authored", "protected")),
    "c": _definition("terrain.region.witness_marker", "c", tags=("feature", "authored", "protected")),
    "f": _definition("terrain.region.facility_marker", "f", tags=("feature", "authored", "protected")),
    "s": _definition("terrain.region.special_marker", "s", tags=("feature", "authored", "protected")),
    "*": _definition("terrain.region.shrine_marker", "*", tags=("feature", "authored", "protected")),
    "?": _definition("terrain.region.situation_marker", "?", tags=("feature", "authored", "protected")),
    "!": _definition("terrain.region.active_situation_marker", "!", tags=("feature", "authored", "protected")),
}

_REGION_IDS = (
    "hearthford", "greywash", "greenwold", "whitecairn",
    "dunmire", "rillscar", "marlbank", "frostmere",
)
_REGIONAL_GROUND = {
    region_id: _definition(
        f"terrain.region.{region_id}.ground",
        ".",
        tags=("ground", region_id),
    )
    for region_id in _REGION_IDS
}


@lru_cache(maxsize=128)
def _legacy_definition(glyph: str) -> TerrainDefinition:
    """Retain the permissive mechanics of an unknown historical glyph."""
    return _definition(
        f"terrain.region.legacy.{ord(glyph):04x}",
        glyph,
        tags=("legacy", "unknown"),
    )


REGIONAL_TERRAIN = TerrainCatalog(
    MappingProxyType(_DEFINITIONS),
    MappingProxyType(_REGIONAL_GROUND),
)


def terrain_from_glyph(glyph: str, region_id: str | None = None) -> TerrainDefinition:
    return REGIONAL_TERRAIN.resolve(glyph, region_id)


def terrain_at(region: Region, position: Position) -> TerrainDefinition:
    """Resolve sparse mutation first, then the persisted legacy row glyph."""
    rows = region.levels.get(str(position.z), [])
    if not 0 <= position.y < len(rows) or not 0 <= position.x < len(rows[position.y]):
        return terrain_from_glyph(" ", region.id)
    coordinate = f"{position.x},{position.y},{position.z}"
    glyph = region.tile_changes.get(coordinate, rows[position.y][position.x])
    return terrain_from_glyph(glyph, region.id)


def replace_terrain(
    region: Region, position: Position, glyph: str,
) -> TerrainDefinition:
    """Persist a glyph replacement and discard damage to the old terrain.

    Sparse damage belongs to the terrain identity currently occupying a cell.
    All runtime replacement paths should use this seam so damage cannot survive
    fire, collapse, authored changes, or another replacement of that identity.
    """
    replacement = terrain_from_glyph(glyph, region.id)
    coordinate = f"{position.x},{position.y},{position.z}"
    region.tile_changes[coordinate] = glyph
    region.terrain_damage.pop(coordinate, None)
    return replacement
