"""Engine-owned semantic topology for Roag's static vessel and tavern maps.

Legacy one-character tokens are retained to keep existing reducers and persisted
state compatible. Collision and renderer-neutral cell identity originate here.
"""
from __future__ import annotations
from dataclasses import dataclass
from .catalog import CatalogError, TOPOLOGY_SECTIONS, load_catalog


@dataclass(frozen=True)
class TopologyCell:
    id: str
    terrain_id: str
    feature_id: str | None
    legacy_token: str
    walkable: bool
    blocks_sight: bool


_DATA = load_catalog("topology.json", TOPOLOGY_SECTIONS)
_raw_definitions = _DATA["definitions"]
if not isinstance(_raw_definitions, dict):
    raise CatalogError("topology definitions must be an object")
CELLS: dict[str, TopologyCell] = {}
for identity, row in _raw_definitions.items():
    if (not isinstance(identity, str) or not isinstance(row, dict)
            or set(row) != {"legacy_token", "terrain_id", "feature_id", "walkable", "blocks_sight"}
            or not isinstance(row["legacy_token"], str) or len(row["legacy_token"]) != 1
            or not isinstance(row["terrain_id"], str) or not row["terrain_id"]
            or row["feature_id"] is not None and (not isinstance(row["feature_id"], str) or not row["feature_id"])
            or type(row["walkable"]) is not bool or type(row["blocks_sight"]) is not bool):
        raise CatalogError("topology definition is invalid")
    CELLS[identity] = TopologyCell(identity, row["terrain_id"], row["feature_id"], row["legacy_token"], row["walkable"], row["blocks_sight"])


def _levels(value: object, *, expected_levels: set[str] | None, height: int) -> dict[int, tuple[tuple[str, ...], ...]]:
    if not isinstance(value, dict) or expected_levels is not None and set(value) != expected_levels:
        raise CatalogError("topology levels are invalid")
    parsed: dict[int, tuple[tuple[str, ...], ...]] = {}
    for key, rows in value.items():
        if (not isinstance(key, str) or not isinstance(rows, list) or len(rows) != height
                or any(not isinstance(row, list) or len(row) != 64 or any(cell not in CELLS for cell in row) for row in rows)):
            raise CatalogError("topology rows are invalid")
        parsed[int(key)] = tuple(tuple(row) for row in rows)
    return parsed

VESSEL_TOPOLOGY = _levels(_DATA["vessel_levels"], expected_levels={"-1", "0", "1"}, height=22)
_tavern_levels = _levels({"0": _DATA["tavern_map"]}, expected_levels={"0"}, height=24)
TAVERN_TOPOLOGY = _tavern_levels[0]


def _void(scope: str) -> TopologyCell:
    return TopologyCell(f"cell.{scope}.void", f"terrain.{scope}.void", None, " ", False, False)


def vessel_cell(level: int, x: int, y: int) -> TopologyCell:
    rows = VESSEL_TOPOLOGY.get(level, ())
    if not 0 <= y < len(rows) or not 0 <= x < len(rows[y]):
        return _void("vessel")
    return CELLS[rows[y][x]]


def tavern_cell(x: int, y: int) -> TopologyCell:
    if not 0 <= y < len(TAVERN_TOPOLOGY) or not 0 <= x < len(TAVERN_TOPOLOGY[y]):
        return _void("tavern")
    return CELLS[TAVERN_TOPOLOGY[y][x]]


def cell_from_legacy(scope: str, token: str) -> TopologyCell:
    """Decode a persisted legacy vessel tile override without consulting glyph art."""
    match = next((cell for cell in CELLS.values() if cell.id.startswith(f"cell.{scope}.") and cell.legacy_token == token), None)
    if match is None:
        # Format-15 vessel overrides historically used a few state-only tokens
        # (for example a breach).  They are engine state, never pack glyphs.
        return TopologyCell(f"cell.{scope}.dynamic.{ord(token):02x}", f"terrain.{scope}.dynamic", f"feature.{scope}.dynamic", token, token not in {" ", "#", "~", "T", "=", "t", "F", "f"}, token in {"#", "T", "+"})
    return match


def legacy_rows(rows: tuple[tuple[str, ...], ...]) -> tuple[str, ...]:
    return tuple("".join(CELLS[cell].legacy_token for cell in row) for row in rows)


VESSEL_LEVELS = {level: legacy_rows(rows) for level, rows in VESSEL_TOPOLOGY.items()}
TAVERN_MAP = legacy_rows(TAVERN_TOPOLOGY)
