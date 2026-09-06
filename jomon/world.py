"""Map queries and deterministic pressure calculations."""

from __future__ import annotations

from collections import deque
from dataclasses import dataclass

from .content import COMMODITIES, JOMON_MAP
from .state import GameState, Position

JOMON_GANGPLANK = Position(31, 5)
REGION_GANGPLANK = Position(0, 9)


@dataclass(frozen=True)
class Pressure:
    elapsed: int
    depth: int
    noise: int
    valuables: int
    score: int
    band: str
    alert_range: int
    pursuit_steps: int


def pressure(state: GameState) -> Pressure:
    if state.location != "region":
        return Pressure(0, 0, 0, 0, 0, "safe", 0, 0)
    depth = max(0, state.position.x - 1) // 7
    valuables = sum(stack.quantity for stack in state.carried_goods.values())
    score = state.pressure_elapsed // 4 + depth + state.noise + valuables
    if score >= 24:
        band, alert, pursuit = "critical", 8, 2
    elif score >= 12:
        band, alert, pursuit = "strained", 6, 1
    else:
        band, alert, pursuit = "steady", 4, 1
    return Pressure(state.pressure_elapsed, depth, state.noise, valuables, score, band, alert, pursuit)


def map_rows(state: GameState) -> list[str] | tuple[str, ...]:
    return JOMON_MAP if state.location == "jomon" else state.region.map_rows


def base_tile(state: GameState, position: Position) -> str:
    rows = map_rows(state)
    if position.y < 0 or position.y >= len(rows):
        return "#"
    row = rows[position.y]
    if position.x < 0 or position.x >= len(row):
        return "#"
    return row[position.x]


def displayed_tile(state: GameState, position: Position) -> str:
    tile = base_tile(state, position)
    if state.location != "region":
        return tile
    if tile == "R" and position != state.region.resource_position:
        return "."
    if tile == "R" and state.resource_taken:
        return "."
    if tile == "r" and state.opportunity_taken:
        return "."
    if tile == "=" and state.flood_control == "lowered":
        return "-"
    return tile


def is_walkable(state: GameState, position: Position, *, ignore_threat: bool = False) -> bool:
    tile = base_tile(state, position)
    if tile in {"#", "~", "T"}:
        return False
    if tile == "=" and state.flood_control != "lowered":
        return False
    if (
        state.location == "region"
        and not ignore_threat
        and state.threat.status == "engaged"
        and position == state.threat.position
    ):
        return False
    return True


def find_tile(rows: list[str] | tuple[str, ...], tile: str) -> Position:
    for y, row in enumerate(rows):
        x = row.find(tile)
        if x >= 0:
            return Position(x, y)
    raise ValueError(f"map has no {tile!r} tile")


def area_name(state: GameState) -> str:
    if state.location == "jomon":
        return "Jomon — working deck"
    x, y = state.position.x, state.position.y
    if x <= 15:
        return "Hearthford"
    if y <= 7:
        return "Low Wood"
    if y >= 12 and x < 36:
        return "Reed Sluice"
    if x >= 36:
        return "Mill Reach"
    return "Flooded Towpath"


def carried_bulk(state: GameState) -> int:
    return sum(COMMODITIES[name]["bulk"] * stack.quantity for name, stack in state.carried_goods.items())


def capacity(state: GameState) -> int:
    courier = state.courier
    base = 10 if courier and courier.role in {"carpenter", "guard"} else 8
    return base + (4 if state.support == "harness" else 0)


def connected_required_map(state: GameState) -> bool:
    """Return whether gangplank, contact, control, and resource share a route.

    The flood crossing is treated as open because its control is reachable from
    the west. This checks generation topology, not current traversal state.
    """
    if state.location != "region":
        original = state.location
        state.location = "region"
    else:
        original = None
    try:
        targets = {
            find_tile(state.region.map_rows, "M"),
            find_tile(state.region.map_rows, "&"),
            state.region.resource_position,
        }
        queue = deque([REGION_GANGPLANK])
        seen = {REGION_GANGPLANK}
        while queue:
            current = queue.popleft()
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                candidate = Position(current.x + dx, current.y + dy)
                tile = base_tile(state, candidate)
                if candidate not in seen and tile not in {"#", "~", "T"}:
                    seen.add(candidate)
                    queue.append(candidate)
        return targets <= seen
    finally:
        if original is not None:
            state.location = original
