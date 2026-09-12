"""Bounded structural diagnostics for generated expeditions."""

from __future__ import annotations

from collections import defaultdict
from heapq import heappop, heappush
from itertools import combinations, product
from statistics import median
from typing import Any, Iterable

from .content import Catalog
from .engine import GameEngine, WORLD_HEIGHT, WORLD_WIDTH


def _resource_delta(cost: dict[str, Any], effects: Iterable[dict[str, Any]]) -> dict[str, int]:
    result = {"light": 0, "supplies": 0, "health_all": 0, "stress_all": 0}
    resource = cost["resource"]
    if resource == "stress_all":
        result[resource] += int(cost["amount"])
    elif resource in result:
        result[resource] -= int(cost["amount"])
    for effect in effects:
        operation = effect["op"]
        if operation in result:
            result[operation] += int(effect["amount"])
    return result


def _add_resources(*values: dict[str, int]) -> dict[str, int]:
    return {
        resource: sum(value[resource] for value in values)
        for resource in ("light", "supplies", "health_all", "stress_all")
    }


def completion_corridors(
    engine: GameEngine,
    *,
    include_facilities: bool = False,
) -> list[dict[str, Any]]:
    """Enumerate static two-objective corridors without simulating combat or patrols."""
    start = (engine.state.party_x, engine.state.party_y)
    boss_room = next(room for room in engine.state.rooms if room.kind == "boss")
    core = engine.room_position(boss_room.id)
    interval = int(engine.catalog.balance["exploration_steps_per_light"])
    costs: dict[tuple[int, int], dict[tuple[int, int], int]] = {}

    def leg(origin: tuple[int, int], destination: tuple[int, int]) -> int:
        if origin not in costs:
            costs[origin] = engine._travel_costs_from(origin)
        return costs[origin][destination]

    variants: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for objective in engine.state.objectives:
        mission = engine.mission_definition(objective.biome_id)
        for approach in mission["approaches"]:
            points = [(objective.x, objective.y)] + [
                tuple(site) for site in objective.approach_sites[approach["id"]]
            ]
            resources = _resource_delta(
                approach["cost"],
                [approach["completion"]],
            )
            variants[objective.id].append(
                {
                    "objective": objective,
                    "approach": approach,
                    "points": points,
                    "internal_ticks": sum(
                        leg(origin, destination)
                        for origin, destination in zip(points, points[1:])
                    ),
                    "resources": resources,
                }
            )

    facility_visits: list[dict[str, Any] | None] = [None]
    if include_facilities:
        for facility in engine.state.facilities:
            definition = engine.facility_definition(facility)
            for option in definition["options"]:
                facility_visits.append(
                    {
                        "facility": facility,
                        "option": option,
                        "resources": _resource_delta(option["cost"], option["effects"]),
                    }
                )

    corridors: list[dict[str, Any]] = []
    for left, right in combinations(engine.state.objectives, 2):
        for first, second in ((left, right), (right, left)):
            for first_variant, second_variant in product(
                variants[first.id], variants[second.id]
            ):
                for facility_visit in facility_visits:
                    points = [start]
                    facility_id = option_id = None
                    facility_resources = {
                        "light": 0,
                        "supplies": 0,
                        "health_all": 0,
                        "stress_all": 0,
                    }
                    if facility_visit:
                        facility = facility_visit["facility"]
                        points.append((facility.x, facility.y))
                        facility_id = facility.id
                        option_id = facility_visit["option"]["id"]
                        facility_resources = facility_visit["resources"]
                    points.extend(first_variant["points"])
                    points.extend(second_variant["points"])
                    points.append(core)
                    ticks = sum(
                        leg(origin, destination)
                        for origin, destination in zip(points, points[1:])
                    )
                    travel_light = (
                        (engine.state.travel_ticks + ticks) // interval
                        - engine.state.travel_ticks // interval
                    )
                    resources = _add_resources(
                        first_variant["resources"],
                        second_variant["resources"],
                        facility_resources,
                    )
                    corridors.append(
                        {
                            "objectives": (first.biome_id, second.biome_id),
                            "approaches": (
                                first_variant["approach"]["id"],
                                second_variant["approach"]["id"],
                            ),
                            "facility_id": facility_id,
                            "facility_option": option_id,
                            "points": points,
                            "ticks": ticks,
                            "travel_light": travel_light,
                            "projected_light": engine.state.light
                            - travel_light
                            + resources["light"],
                            "projected_supplies": engine.state.supplies
                            + resources["supplies"],
                            "health_all_delta": resources["health_all"],
                            "stress_all_delta": resources["stress_all"],
                        }
                    )
    return corridors


def _path_avoiding(
    engine: GameEngine,
    start: tuple[int, int],
    destination: tuple[int, int],
    blocked: set[tuple[int, int]],
) -> list[tuple[int, int]] | None:
    if destination in blocked:
        return None
    pending: list[tuple[int, int, int]] = [(0, start[0], start[1])]
    costs = {start: 0}
    previous: dict[tuple[int, int], tuple[int, int] | None] = {start: None}
    while pending:
        cost, current_x, current_y = heappop(pending)
        current = (current_x, current_y)
        if cost != costs[current]:
            continue
        if current == destination:
            break
        for neighbor in engine._neighbors(current):
            if neighbor in blocked:
                continue
            next_cost = cost + engine.movement_cost(*neighbor)
            if next_cost < costs.get(neighbor, WORLD_WIDTH * WORLD_HEIGHT * 3):
                costs[neighbor] = next_cost
                previous[neighbor] = current
                heappush(pending, (next_cost, neighbor[0], neighbor[1]))
    if destination not in previous:
        return None
    path = []
    current = destination
    while current != start:
        path.append(current)
        parent = previous[current]
        if parent is None:
            break
        current = parent
    path.reverse()
    return path


def detail_corridor(engine: GameEngine, corridor: dict[str, Any]) -> dict[str, Any]:
    """Add spatial pressure facts to one corridor without mutating the run."""
    route: list[tuple[int, int]] = []
    for origin, destination in zip(corridor["points"], corridor["points"][1:]):
        route.extend(engine._find_path(origin, destination))
    route_tiles = set(route)
    hazard_tiles = {
        tuple(cell)
        for hazard in engine.state.hazards
        if hazard.active
        for cell in hazard.cells
        if cell not in hazard.triggered_cells
    }
    touched_hazards = sum(
        any(tuple(cell) in route_tiles for cell in hazard.cells)
        for hazard in engine.state.hazards
        if hazard.active
    )
    safe_ticks = 0
    for origin, destination in zip(corridor["points"], corridor["points"][1:]):
        safe_leg = _path_avoiding(engine, origin, destination, hazard_tiles)
        if safe_leg is None:
            safe_ticks = -1
            break
        safe_ticks += engine.path_cost(safe_leg)
    patrol_posts = sum(
        any(
            abs(engine.room_position(patrol.room_id)[0] - x)
            + abs(engine.room_position(patrol.room_id)[1] - y)
            <= 1
            for x, y in route_tiles
        )
        for patrol in engine.state.patrols
        if engine.room(patrol.room_id).kind != "boss"
    )
    return {
        **corridor,
        "hazard_fields": touched_hazards,
        "hazard_tiles": len(route_tiles & hazard_tiles),
        "hazard_avoiding_ticks": safe_ticks,
        "revisited_steps": len(route) - len(route_tiles),
        "patrol_posts": patrol_posts,
    }


def audit_expeditions(catalog: Catalog, seed_count: int) -> list[dict[str, Any]]:
    """Return one bounded structural summary per fresh seed."""
    if not 1 <= seed_count <= 500:
        raise ValueError("seed count must be between 1 and 500")
    rows = []
    for seed in range(seed_count):
        engine = GameEngine.new(catalog, seed)
        all_corridors = completion_corridors(engine, include_facilities=True)
        base = [
            corridor
            for corridor in all_corridors
            if corridor["facility_id"] is None
        ]
        assisted = [corridor for corridor in all_corridors if corridor["facility_id"] is not None]
        feasible = [
            corridor
            for corridor in base
            if corridor["projected_light"] >= 0
            and corridor["projected_supplies"] >= 0
        ]
        fastest = min(feasible or base, key=lambda item: (item["ticks"], -item["projected_light"]))
        best_light = max(feasible or base, key=lambda item: (item["projected_light"], -item["ticks"]))
        best_assisted = max(
            assisted,
            key=lambda item: (item["projected_light"], item["projected_supplies"], -item["ticks"]),
        )
        detail = detail_corridor(engine, best_light)
        rows.append(
            {
                "seed": seed,
                "world": engine.state.world_id,
                "layout": catalog.worlds[engine.state.world_id]["layout"],
                "biomes": tuple(engine.state.biome_ids),
                "fastest_ticks": fastest["ticks"],
                "best_light": best_light["projected_light"],
                "best_supplies": best_light["projected_supplies"],
                "facility_light": best_assisted["projected_light"],
                "hazard_fields": detail["hazard_fields"],
                "hazard_avoiding_ticks": detail["hazard_avoiding_ticks"],
                "revisited_steps": detail["revisited_steps"],
                "patrol_posts": detail["patrol_posts"],
            }
        )
    return rows


def format_audit(rows: list[dict[str, Any]]) -> str:
    """Format layout-level diagnostics for the command line."""
    by_layout: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for row in rows:
        by_layout[row["layout"]].append(row)
    lines = [
        "layout       seeds  fastest ticks min/median/max  best light min/median  facility light median",
    ]
    for layout in sorted(by_layout):
        values = by_layout[layout]
        ticks = [row["fastest_ticks"] for row in values]
        lights = [row["best_light"] for row in values]
        facility_lights = [row["facility_light"] for row in values]
        lines.append(
            f"{layout:<12} {len(values):>5}  "
            f"{min(ticks):>3}/{median(ticks):>6.1f}/{max(ticks):<3}"
            f"                 {min(lights):>3}/{median(lights):>6.1f}"
            f"                 {median(facility_lights):>6.1f}"
        )
    worst = min(rows, key=lambda row: row["best_light"])
    lines.append(
        f"worst best-light corridor: seed {worst['seed']} {worst['layout']} "
        f"light {worst['best_light']} supplies {worst['best_supplies']}"
    )
    lines.append("Static route diagnostic only; combat and moving patrols are not simulated.")
    return "\n".join(lines)
