"""Room-map queries, pressure, capacity, and explicit build interactions."""

from __future__ import annotations

from collections import deque
from dataclasses import dataclass

from .content import COMMODITIES, JOMON_MAP
from .state import GameState, Position, RoomExit

JOMON_GANGPLANK = Position(31, 5)
REGION_GANGPLANK = Position(1, 6)
REGION_ARRIVAL = Position(2, 6)


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
    if state.location != "region" or state.room is None:
        return Pressure(0, 0, 0, 0, 0, "safe", 0, 0)
    depth = state.room.depth
    valuables = sum(stack.quantity for stack in state.carried_goods.values())
    score = state.pressure_elapsed // 8 + depth * 2 + state.noise + valuables
    if score >= 22:
        band, alert, pursuit = "critical", 9, 2
    elif score >= 11:
        band, alert, pursuit = "strained", 7, 1
    else:
        band, alert, pursuit = "steady", 4, 1
    return Pressure(state.pressure_elapsed, depth, state.noise, valuables, score, band, alert, pursuit)


def map_rows(state: GameState) -> list[str] | tuple[str, ...]:
    if state.location == "jomon" or state.room is None:
        return JOMON_MAP
    return state.room.map_rows


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
    room = state.room
    if room is None:
        if tile == "s" and state.merchant_present:
            return "$"
        return tile
    if tile == "?" and room.changes.get("discovery_taken"):
        return "."
    if tile == "R" and room.changes.get("objective_taken"):
        return "."
    if tile == "r" and room.changes.get("resource_taken"):
        return "."
    if tile == "D" and room.changes.get("shutter_closed"):
        return "|"
    if tile == "O" and room.changes.get("cover_moved"):
        return "o"
    if tile == "%" and room.changes.get("structure_stable"):
        return "."
    return tile


def exit_at(state: GameState, position: Position) -> RoomExit | None:
    if state.room is None:
        return None
    return next((exit_ for exit_ in state.room.exits.values() if exit_.position == position), None)


def is_walkable(state: GameState, position: Position, *, ignore_threat: bool = False) -> bool:
    tile = displayed_tile(state, position)
    if tile in {"#", "~", "T"}:
        return False
    if state.location == "region" and not ignore_threat:
        if any(threat.position == position and threat.status == "engaged" for threat in state.local_threats()):
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
    return state.room.name if state.room else "Hearthford"


def carried_bulk(state: GameState) -> int:
    return sum(COMMODITIES[name]["bulk"] * stack.quantity for name, stack in state.carried_goods.items())


def capacity(state: GameState) -> int:
    courier = state.courier
    base = 10 if courier and courier.role in {"carpenter", "guard"} else 8
    if state.gear == "cargo harness":
        base += 3
    if state.support == "porter watch":
        base += 4
    return base


def build_combinations(state: GameState) -> list[str]:
    """Return the direct qualitative interactions active in the current build."""
    courier = state.courier
    technique = courier.technique if courier else ""
    combinations: list[str] = []
    if state.gear == "quiet shoes" and state.support == "route survey":
        combinations.append("surveyed soft-step")
    if state.weapon == "billhook" and state.gear == "rope":
        combinations.append("hooked rigging")
    if state.gear == "buckler" and technique == "set stance":
        combinations.append("shielded set stance")
    if state.gear == "cargo harness" and state.support == "factor surety":
        combinations.append("bonded cargo")
    if state.gear == "repair tools" and state.support == "carpenter rig":
        combinations.append("prepared repair crew")
    if state.support == "field care" and technique == "field binding":
        combinations.append("deep field binding")
    if state.gear == "cargo harness" and state.support == "porter watch":
        combinations.append("high-capacity watch")
    if state.gear == "trade seals" and technique == "measured terms":
        combinations.append("witnessed terms")
    return combinations


def connected_required_map(state: GameState) -> bool:
    rooms = state.region.rooms
    if "hearthford_quay" not in rooms:
        return False
    queue = deque(["hearthford_quay"])
    seen = {"hearthford_quay"}
    reciprocal = True
    while queue:
        room = rooms[queue.popleft()]
        for exit_ in room.exits.values():
            target = rooms.get(exit_.target)
            if target is None or not any(back.target == room.id for back in target.exits.values()):
                reciprocal = False
                continue
            if target.id not in seen:
                seen.add(target.id)
                queue.append(target.id)
    return reciprocal and state.region.objective_room in seen and len(seen) == len(rooms)


def room_route(state: GameState, start: str, target: str) -> list[str]:
    queue = deque([start])
    previous: dict[str, str | None] = {start: None}
    while queue:
        current = queue.popleft()
        if current == target:
            break
        for exit_ in state.region.rooms[current].exits.values():
            if exit_.target not in previous:
                previous[exit_.target] = current
                queue.append(exit_.target)
    if target not in previous:
        return []
    route: list[str] = []
    current: str | None = target
    while current is not None:
        route.append(current)
        current = previous[current]
    return list(reversed(route))
