"""Spatial queries, camera, visibility, pressure, and build interactions."""

from __future__ import annotations

from collections import deque
from dataclasses import dataclass

from .content import COMMODITIES, PASSIVES
from .state import GameState, Position
from .vessel import (
    JOMON_GANGPLANK,
    TAVERN_MAP,
    VESSEL_LEVELS,
    current_area,
    vessel_rows,
    vessel_tile,
    vessel_vertical_destination,
)


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


def position_key(position: Position) -> str:
    return f"{position.x},{position.y},{position.z}"


def pressure(state: GameState) -> Pressure:
    if state.location != "region":
        return Pressure(0, 0, 0, 0, 0, "safe", 0, 0)
    landing = state.region.landmarks["landing"]
    distance = abs(state.position.x - landing.x) + abs(state.position.y - landing.y)
    depth = distance // 14 + abs(state.position.z) * 2
    valuables = sum(state.carried_passives.values()) + sum(stack.quantity for stack in state.carried_goods.values())
    score = state.pressure_elapsed // 18 + depth + state.noise // 2 + valuables
    if score >= 18:
        band, alert, pursuit = "critical", 12, 2
    elif score >= 10:
        band, alert, pursuit = "strained", 9, 1
    else:
        band, alert, pursuit = "steady", 6, 1
    return Pressure(state.pressure_elapsed, depth, state.noise, valuables, score, band, alert, pursuit)


def map_rows(state: GameState, z: int | None = None) -> list[str] | tuple[str, ...]:
    if state.location == "jomon":
        return vessel_rows(state, z)
    return state.region.levels[str(state.position.z if z is None else z)]


def base_tile(state: GameState, position: Position) -> str:
    if state.location == "jomon":
        return vessel_tile(state, position)
    else:
        rows = state.region.levels.get(str(position.z), [])
    if not 0 <= position.y < len(rows) or not 0 <= position.x < len(rows[position.y]):
        return " "
    if state.location == "region":
        changed = state.region.tile_changes.get(position_key(position))
        if changed is not None:
            return changed
    return rows[position.y][position.x]


def region_tile(state: GameState, position: Position) -> str:
    """Read regional terrain regardless of whether the courier is currently aboard."""
    rows = state.region.levels.get(str(position.z), [])
    if not 0 <= position.y < len(rows) or not 0 <= position.x < len(rows[position.y]):
        return " "
    return state.region.tile_changes.get(
        position_key(position), rows[position.y][position.x]
    )


def displayed_tile(state: GameState, position: Position) -> str:
    tile = base_tile(state, position)
    if state.location == "jomon":
        area = current_area(state)
        actor_id = next(
            (
                schedule.actor_id for schedule in state.actor_schedules.values()
                if schedule.area == area and schedule.position == position
                and schedule.actor_id != state.active_courier_id
            ),
            None,
        )
        if actor_id == state.bartender.id:
            return "B"
        if actor_id:
            return "a" if any(person.id == actor_id for person in state.household) else "v"
    if state.location == "jomon" and tile == "s" and state.merchant_present:
        return "$"
    container = next((item for item in state.region.containers if item.position == position), None) if state.location == "region" else None
    if container:
        return "o" if container.opened else "C"
    if position_key(position) in state.smoke:
        return "s"
    if position_key(position) in state.water:
        return ","
    return tile


def is_walkable(state: GameState, position: Position, *, ignore_threat: bool = False) -> bool:
    tile = displayed_tile(state, position)
    if tile in {" ", "#", "~", "T"}:
        return False
    if state.location == "jomon" and tile in {"a", "v", "B"}:
        return False
    if state.location == "region" and not ignore_threat:
        if any(threat.position == position and threat.status in {"watching", "engaged"} for threat in state.threats):
            return False
    return True


def vertical_destination(state: GameState, position: Position) -> Position | None:
    if state.location == "jomon":
        return vessel_vertical_destination(position)
    for link in state.region.vertical_links:
        if link.first == position:
            return link.second
        if link.second == position:
            return link.first
    return None


def vertical_open(state: GameState, lower: Position, upper: Position) -> bool:
    if lower.x != upper.x or lower.y != upper.y or upper.z - lower.z != 1:
        return False
    if vertical_destination(state, lower) == upper:
        return True
    lower_tile, upper_tile = base_tile(state, lower), base_tile(state, upper)
    return (
        "O" in {lower_tile, upper_tile}
        and lower_tile != "d"
        and upper_tile != "d"
    )


def distance(left: Position, right: Position) -> int:
    return max(abs(left.x - right.x), abs(left.y - right.y)) + abs(left.z - right.z) * 2


def _line(start: Position, end: Position) -> list[Position]:
    """Integer two-dimensional ray on one level."""
    x0, y0, x1, y1 = start.x, start.y, end.x, end.y
    dx, dy = abs(x1 - x0), -abs(y1 - y0)
    sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
    error, points = dx + dy, []
    while True:
        points.append(Position(x0, y0, start.z))
        if (x0, y0) == (x1, y1):
            return points
        twice = 2 * error
        if twice >= dy:
            error += dy
            x0 += sx
        if twice <= dx:
            error += dx
            y0 += sy


def blocks_sight(state: GameState, position: Position) -> bool:
    return base_tile(state, position) in {"#", "T", "+"} or position_key(position) in state.smoke


def line_of_sight(state: GameState, start: Position, end: Position) -> bool:
    if start.z == end.z:
        return all(not blocks_sight(state, point) for point in _line(start, end)[1:-1])
    if abs(start.z - end.z) != 1:
        return False
    lower, upper = (start, end) if start.z < end.z else (end, start)
    if start.x == end.x and start.y == end.y:
        return vertical_open(state, lower, upper)
    if distance(start, end) > 14 or base_tile(state, upper) == " ":
        return False
    # Elevated exterior positions project a lane across both aligned levels.
    upper_ray = _line(Position(start.x, start.y, upper.z), Position(end.x, end.y, upper.z))
    lower_ray = _line(Position(start.x, start.y, lower.z), Position(end.x, end.y, lower.z))
    return all(not blocks_sight(state, point) for point in upper_ray[1:-1]) and all(
        not blocks_sight(state, point) for point in lower_ray[1:-1]
    )


def projectile_path(start: Position, end: Position) -> list[Position]:
    """Return the inspectable horizontal projection of a shot."""
    return _line(start, Position(end.x, end.y, start.z))


def cover_at(state: GameState, shooter: Position, target: Position) -> str:
    if not line_of_sight(state, shooter, target):
        return "full"
    adjacent = (
        Position(target.x + 1, target.y, target.z), Position(target.x - 1, target.y, target.z),
        Position(target.x, target.y + 1, target.z), Position(target.x, target.y - 1, target.z),
    )
    if any(base_tile(state, point) in {"#", "T", "+"} for point in adjacent):
        return "partial"
    return "open"


def sight_radius(state: GameState) -> int:
    if state.location != "region":
        return 20
    tile = base_tile(state, state.position)
    nearby_walls = 0
    for dx, dy in ((5, 0), (-5, 0), (0, 5), (0, -5)):
        for step in range(1, 6):
            point = Position(state.position.x + dx // 5 * step, state.position.y + dy // 5 * step, state.position.z)
            if base_tile(state, point) == "#":
                nearby_walls += 1
                break
    indoor = state.position.z <= 1 and tile in {".", "=", "<", ">", "d", "O"} and nearby_walls >= 2
    radius = 7 if state.position.z < 0 or indoor else 13
    if state.position.z > 0:
        radius += 4
    from .calendar import daylight_modifier

    radius += daylight_modifier(state)
    if state.weather == "river fog":
        radius = min(radius, 7)
    elif state.weather in {"hard rain", "coast squall", "forest rain"}:
        radius = min(radius, 9)
    if position_key(state.position) in state.smoke and "smoke lens" not in state.carried_passives:
        radius = min(radius, 3)
    return radius


def field_of_view(state: GameState, *, remember: bool = True) -> set[Position]:
    if state.location != "region":
        rows = map_rows(state)
        return {Position(x, y, state.position.z) for y, row in enumerate(rows) for x in range(len(row))}
    radius = sight_radius(state)
    visible = {state.position}
    for y in range(max(0, state.position.y - radius), min(state.region.height, state.position.y + radius + 1)):
        for x in range(max(0, state.position.x - radius), min(state.region.width, state.position.x + radius + 1)):
            target = Position(x, y, state.position.z)
            if distance(state.position, target) <= radius and line_of_sight(state, state.position, target):
                visible.add(target)
    destination = vertical_destination(state, state.position)
    if destination and line_of_sight(state, state.position, destination):
        visible.add(destination)
    # Open shafts show the aligned space immediately above or below.
    for dz in (-1, 1):
        other = Position(state.position.x, state.position.y, state.position.z + dz)
        if str(other.z) in state.region.levels and line_of_sight(state, state.position, other):
            visible.add(other)
    if remember:
        known = set(state.region.seen)
        known.update(position_key(position) for position in visible)
        state.region.seen = sorted(known)
    return visible


def remembered(state: GameState, position: Position) -> bool:
    return position_key(position) in set(state.region.seen)


def camera_origin(position: Position, map_width: int, map_height: int, view_width: int, view_height: int) -> tuple[int, int]:
    x = max(0, min(position.x - view_width // 2, max(0, map_width - view_width)))
    y = max(0, min(position.y - view_height // 2, max(0, map_height - view_height)))
    return x, y


def find_tile(rows: list[str] | tuple[str, ...], tile: str, z: int = 0) -> Position:
    for y, row in enumerate(rows):
        x = row.find(tile)
        if x >= 0:
            return Position(x, y, z)
    raise ValueError(f"map has no {tile!r} tile")


def area_name(state: GameState) -> str:
    if state.location == "jomon":
        if state.jomon_space == "tavern":
            return "Jomon — common tavern"
        return {
            -1: "Jomon — hold and lower berths",
            0: "Jomon — working deck",
            1: "Jomon — helm and weather deck",
        }.get(state.position.z, "Jomon")
    if state.position.z < 0:
        return f"{state.region.name} — below"
    if state.position.z == 2:
        return f"{state.region.name} — roof and high route"
    if state.position.z == 1:
        return f"{state.region.name} — upper works"
    for name, (x1, y1, x2, y2) in state.region.zones.items():
        if x1 <= state.position.x <= x2 and y1 <= state.position.y <= y2:
            return name
    return state.region.name


def carried_bulk(state: GameState) -> int:
    return sum(COMMODITIES[name]["bulk"] * stack.quantity for name, stack in state.carried_goods.items())


def capacity(state: GameState) -> int:
    courier = state.courier
    base = 10 if courier and courier.role in {"carpenter", "guard"} else 8
    return base + (3 if state.gear == "cargo harness" else 0) + (4 if state.support == "porter watch" else 0)


def passive_bulk(state: GameState) -> int:
    return sum(PASSIVES[name][0] * count for name, count in state.carried_passives.items())


def passive_capacity(state: GameState) -> int:
    return 7 if state.gear == "cargo harness" else 5


def build_combinations(state: GameState) -> list[str]:
    courier = state.courier
    technique = courier.technique if courier else ""
    passive = state.carried_passives
    combinations: list[str] = []
    if state.gear == "quiet shoes" and state.support == "route survey":
        combinations.append("surveyed soft-step")
    if state.weapon == "billhook" and (state.gear == "rope" or "counterweight ring" in passive):
        combinations.append("mobile hook")
    if state.gear == "buckler" and technique == "set stance":
        combinations.append("shielded set stance")
    if state.weapon == "crossbow" and "waxed bowstring" in passive:
        combinations.append("weatherproof aim")
    if state.weapon in {"hand axe", "cudgel"} and "mill-tooth wedge" in passive:
        combinations.append("controlled breach")
    if state.gear == "smoke pot" and "smoke lens" in passive:
        combinations.append("smoke walker")
    if state.gear == "rope" and "river hooks" in passive:
        combinations.append("flood rig")
    if state.support == "field care" and (technique == "field binding" or "salted dressing" in passive):
        combinations.append("deep field binding")
    if "witness token" in passive and pressure(state).valuables >= 2:
        combinations.append("valuable leverage")
    if "high tread" in passive and state.position.z > 0:
        combinations.append("high-ground drive")
    if "tide ledger" in passive and technique == "ebb reader":
        combinations.append("accounted ebb")
    if "cork float" in passive and state.gear == "cargo harness":
        combinations.append("buoyant cargo rig")
    if "storm vane" in passive and state.weapon in {"longbow", "crossbow"}:
        combinations.append("wind-read aim")
    if "charcoal mask" in passive and state.gear == "smoke pot":
        combinations.append("masked smoke passage")
    if "resin grip" in passive and state.weapon in {"longbow", "billhook", "war hammer"}:
        combinations.append("weatherfast grip")
    if "bird whistle" in passive and technique == "wind listener":
        combinations.append("crosswind decoy")
    if "thorn weave" in passive and state.guarded_step:
        combinations.append("thorn-held momentum")
    if "limestone cleat" in passive and state.gear == "quiet shoes":
        combinations.append("quiet scree step")
    if "sling cup" in passive and state.weapon == "sling" and state.position.z > 0:
        combinations.append("high sling arc")
    if "quarry brace" in passive and load_state_name(state) in {"laden", "encumbered"}:
        combinations.append("weighted floor brace")
    if "fall sail" in passive and state.gear == "rope":
        combinations.append("directed fall")
    return combinations


def load_state_name(state: GameState) -> str:
    # Local import avoids making inventory depend on world pressure queries.
    from .inventory import load_state

    return load_state(state)


def reachable_positions(state: GameState, start: Position | None = None) -> set[Position]:
    start = start or state.region.landmarks["landing"]
    queue, seen = deque([start]), {start}
    while queue:
        current = queue.popleft()
        candidates = [
            Position(current.x + 1, current.y, current.z), Position(current.x - 1, current.y, current.z),
            Position(current.x, current.y + 1, current.z), Position(current.x, current.y - 1, current.z),
        ]
        destination = next(
            (
                link.second if link.first == current else link.first
                for link in state.region.vertical_links
                if current in {link.first, link.second}
            ),
            None,
        )
        if destination:
            candidates.append(destination)
        if region_tile(state, current) == "O" and current.z > -1:
            candidates.append(Position(current.x, current.y, current.z - 1))
        for candidate in candidates:
            walkable = region_tile(state, candidate) not in {" ", "#", "~", "T"}
            if candidate not in seen and walkable:
                seen.add(candidate)
                queue.append(candidate)
    return seen


def connected_required_map(state: GameState) -> bool:
    reachable = reachable_positions(state)
    required = {
        state.region.landmarks[key]
        for key in ("landing", "contact", "objective", "cave_entrance")
    }
    elevated = state.region.landmarks.get("elevated") or state.region.landmarks.get("high_view")
    if elevated:
        required.add(elevated)
    return required <= reachable
