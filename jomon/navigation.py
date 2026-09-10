"""Deterministic, action-by-action following of already known local routes."""

from __future__ import annotations

from dataclasses import dataclass
import heapq
from itertools import count

from .inventory import load_state
from .state import GameState, Position
from .world import (
    base_tile,
    distance,
    field_of_view,
    is_walkable,
    position_key,
    vertical_destination,
)


class RouteUnavailable(ValueError):
    """Raised when a local destination has no safe remembered route."""


@dataclass(frozen=True)
class NavigationTarget:
    id: str
    label: str
    position: Position
    kind: str


@dataclass(frozen=True)
class RoutePlan:
    region_id: str
    target_id: str
    label: str
    start: Position
    destination: Position
    path: tuple[Position, ...]


@dataclass(frozen=True)
class RouteAdvance:
    next_index: int
    finished: bool
    stop_reason: str
    time_advanced: bool = False


LANDMARK_LABELS = {
    "landing": "return landing",
    "contact": "primary witness",
    "second_contact": "secondary witness",
    "settlement": "settlement work",
    "objective": "material objective",
    "cave_entrance": "below-ground entrance",
    "elevated": "elevated landmark",
    "high_view": "high viewpoint",
    "works": "regional works",
    "store": "working store",
    "ruin": "historic scar",
    "far_bank": "far-bank route",
}


def _parse_key(value: str) -> Position | None:
    try:
        x, y, z = (int(part) for part in value.split(","))
    except (TypeError, ValueError):
        return None
    return Position(x, y, z)


def remembered_positions(state: GameState) -> set[Position]:
    """Return persisted exploration without mutating it through a new FOV read."""
    known = {point for value in state.region.seen if (point := _parse_key(value))}
    known.add(state.position)
    known.add(state.region.landmarks["landing"])
    return known


def navigation_targets(state: GameState) -> tuple[NavigationTarget, ...]:
    """List named, known destinations; this is deliberately not autoexplore."""
    if state.location != "region":
        return ()
    known = remembered_positions(state)
    marks = set(state.treasure_marks.get(state.active_region_id, ()))
    targets: list[NavigationTarget] = []
    occupied: set[Position] = set()
    priorities = {
        "landing": 0, "contact": 1, "second_contact": 2,
        "objective": 3, "cave_entrance": 4, "elevated": 5,
        "high_view": 5,
    }
    for name, point in sorted(
        state.region.landmarks.items(),
        key=lambda row: (priorities.get(row[0], 8), row[0]),
    ):
        if point == state.position or point not in known or point in occupied:
            continue
        label = LANDMARK_LABELS.get(name, name.replace("_", " "))
        targets.append(NavigationTarget(f"landmark:{name}", label, point, "landmark"))
        occupied.add(point)
    for container in sorted(state.region.containers, key=lambda item: item.id):
        if (
            container.position == state.position
            or container.position in occupied
            or (container.position not in known and container.id not in marks)
        ):
            continue
        state_word = "opened" if container.opened else "marked" if container.id in marks else "seen"
        targets.append(NavigationTarget(
            f"container:{container.id}",
            f"{container.name} ({state_word})",
            container.position,
            "container",
        ))
        occupied.add(container.position)
    for link in sorted(state.region.vertical_links, key=lambda item: item.name):
        for point in (link.first, link.second):
            if point == state.position or point not in known or point in occupied:
                continue
            targets.append(NavigationTarget(
                f"link:{link.name}:{point.x},{point.y},{point.z}",
                f"{link.name} on level {point.z:+d}",
                point,
                "vertical link",
            ))
            occupied.add(point)
    return tuple(sorted(
        targets,
        key=lambda item: (distance(state.position, item.position), item.label, item.id),
    ))


def _visible_danger(state: GameState) -> set[str]:
    visible = field_of_view(state, remember=False)
    return {
        actor.id for actor in state.combatants
        if actor.status in {"watching", "engaged"}
        and actor.ecology != "prey"
        and actor.position in visible
    }


def _visible_material_danger(state: GameState) -> set[str]:
    from .materials import fields

    visible = field_of_view(state, remember=False)
    return {
        key for key, cell in fields(state).items()
        if (point := _parse_key(key)) in visible
        and (cell.fire or cell.smoke >= 2 or cell.collapse_due)
    }


def _cell_cost(state: GameState, point: Position) -> int | None:
    """Prefer firm, clear known ground and reject immediate material danger."""
    from .materials import fields, key

    if not is_walkable(state, point):
        return None
    tile = base_tile(state, point)
    if tile == "O":
        return None
    cell = fields(state).get(key(point))
    if cell and (cell.fire or cell.collapse_due):
        return None
    cost = 10
    if tile in {"m", "r", "q", "t", "w", ","}:
        cost += 5
    if position_key(point) in state.water or (cell and cell.water):
        cost += 10
    if position_key(point) in state.smoke or (cell and cell.smoke):
        cost += 8
    return cost


def _neighbours(state: GameState, point: Position, known: set[Position]) -> list[tuple[Position, int]]:
    rows: list[tuple[Position, int]] = []
    for dx, dy in (
        (-1, -1), (0, -1), (1, -1),
        (-1, 0), (1, 0),
        (-1, 1), (0, 1), (1, 1),
    ):
        candidate = Position(point.x + dx, point.y + dy, point.z)
        if candidate not in known:
            continue
        if dx and dy:
            side_a = Position(point.x + dx, point.y, point.z)
            side_b = Position(point.x, point.y + dy, point.z)
            if (
                not is_walkable(state, side_a, ignore_threat=True)
                and not is_walkable(state, side_b, ignore_threat=True)
            ):
                continue
        cost = _cell_cost(state, candidate)
        if cost is not None:
            rows.append((candidate, cost))
    vertical = vertical_destination(state, point)
    if vertical in known:
        cost = _cell_cost(state, vertical)
        if cost is not None:
            rows.append((vertical, cost + 4))
    return rows


def _path(state: GameState, destination: Position) -> tuple[Position, ...]:
    known = remembered_positions(state)
    if destination not in known:
        raise RouteUnavailable("That destination is marked, but no remembered route reaches it yet.")
    if _visible_danger(state):
        raise RouteUnavailable("Visible danger requires direct movement before route following.")
    serial = count()
    queue: list[tuple[int, int, int, Position]] = [(0, 0, next(serial), state.position)]
    best = {state.position: 0}
    previous: dict[Position, Position | None] = {state.position: None}
    found = False
    while queue and len(previous) <= 12000:
        cost, steps, _, current = heapq.heappop(queue)
        if cost != best[current]:
            continue
        if current == destination:
            found = True
            break
        for candidate, extra in _neighbours(state, current, known):
            score = cost + extra
            if score >= best.get(candidate, 10**9):
                continue
            best[candidate] = score
            previous[candidate] = current
            heapq.heappush(queue, (score, steps + 1, next(serial), candidate))
    if not found:
        raise RouteUnavailable("No safe remembered route currently reaches that destination.")
    route = [destination]
    while previous[route[-1]] is not None:
        route.append(previous[route[-1]])
    route.reverse()
    return tuple(route[1:])


def plan_route(state: GameState, target_id: str) -> RoutePlan:
    target = next((item for item in navigation_targets(state) if item.id == target_id), None)
    if target is None:
        raise RouteUnavailable("That local destination is not presently known.")
    path = _path(state, target.position)
    return RoutePlan(
        state.active_region_id,
        target.id,
        target.label,
        state.position,
        target.position,
        path,
    )


def advance_route(state: GameState, plan: RoutePlan, index: int) -> RouteAdvance:
    """Perform at most one ordinary action and report any new interruption."""
    if state.location != "region" or state.active_region_id != plan.region_id:
        return RouteAdvance(index, False, "The regional route changed.")
    expected = plan.start if index == 0 else plan.path[index - 1]
    if state.position != expected or not 0 <= index < len(plan.path):
        finished = state.position == plan.destination
        return RouteAdvance(index, finished, "Destination reached." if finished else "Your position no longer matches the planned route.")
    next_position = plan.path[index]
    if next_position not in remembered_positions(state):
        return RouteAdvance(index, False, "The next cell is not part of remembered terrain.")
    pre_visible = _visible_danger(state)
    if pre_visible:
        return RouteAdvance(index, False, "Visible danger interrupts route following.")
    if _cell_cost(state, next_position) is None:
        return RouteAdvance(index, False, "The next remembered cell has become unsafe or blocked.")
    pre_engaged = {actor.id for actor in state.combatants if actor.status == "engaged"}
    pre_material = _visible_material_danger(state)
    pre_weather = state.weather
    pre_status = set(state.terrain_statuses)
    pre_load = load_state(state)
    pre_health = state.courier.health if state.courier else 0
    pre_injury = state.courier.injury if state.courier else "none"
    pre_sounds = {(event.position, event.strength) for event in state.sound_events}
    from .actions import interact, move

    vertical = vertical_destination(state, state.position)
    if vertical == next_position:
        result = interact(state)
    else:
        result = move(
            state,
            next_position.x - state.position.x,
            next_position.y - state.position.y,
        )
    if not result.time_advanced or state.position != next_position:
        return RouteAdvance(index, False, result.message or "The planned step did not complete.")
    next_index = index + 1
    if state.position == plan.destination:
        return RouteAdvance(next_index, True, "Destination reached.", True)
    post_visible = _visible_danger(state)
    if post_visible - pre_visible:
        return RouteAdvance(next_index, False, "New visible danger interrupts route following.", True)
    post_engaged = {actor.id for actor in state.combatants if actor.status == "engaged"}
    if post_engaged - pre_engaged:
        return RouteAdvance(next_index, False, "A newly alerted actor interrupts route following.", True)
    post_material = _visible_material_danger(state)
    if post_material - pre_material:
        return RouteAdvance(next_index, False, "A new visible material hazard interrupts route following.", True)
    if state.weather != pre_weather:
        return RouteAdvance(next_index, False, f"Weather changes to {state.weather}.", True)
    added_status = set(state.terrain_statuses) - pre_status
    if added_status:
        return RouteAdvance(next_index, False, f"New condition: {sorted(added_status)[0]}.", True)
    if load_state(state) != pre_load:
        return RouteAdvance(next_index, False, "The carried load changes its movement state.", True)
    if state.courier and (state.courier.health != pre_health or state.courier.injury != pre_injury):
        return RouteAdvance(next_index, False, "Injury interrupts route following.", True)
    new_sounds = {
        (event.position, event.strength) for event in state.sound_events
    } - pre_sounds
    if any(origin not in {expected, next_position} for origin, _ in new_sounds):
        return RouteAdvance(next_index, False, "A new sound away from the route interrupts travel.", True)
    return RouteAdvance(next_index, False, "", True)

