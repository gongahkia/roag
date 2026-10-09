"""Deterministic generated-start readiness audit for the redesigned field loop.

The audit exercises real world generation, regional entry, physical courier
loadouts, semantic terrain, and production content.  It is diagnostic only:
it does not mutate a caller-owned state or impose balance changes on normal
play.
"""

from __future__ import annotations

import argparse
import copy
import json
import statistics
from collections import Counter, deque
from dataclasses import asdict, dataclass

from .inventory import ensure_initial_field_tool, sync_legacy_load
from .production import RECIPES, SHORE_STATIONS, SOURCES, site_position
from .regions import begin_region
from .state import GameState, Position, Region, create_world
from .terrain import TerrainDefinition, terrain_at
from .terrain_actions import PHYSICAL_TERRAIN_ACTIONS, terrain_action_power


CORE_FIELD_ENGINE_RECIPES = ("circuit-rack", "circuit-sensor")


@dataclass(frozen=True)
class OpeningRoleProfile:
    role: str
    courier_id: str
    weapon: str | None
    gear: str | None
    actionable_terrain_ids: tuple[str, ...]
    nearest_actionable_steps: int | None
    obtainable_item_kinds: tuple[str, ...]
    optimistic_circuit_recipes: tuple[str, ...]
    core_engine_missing_inputs: tuple[tuple[str, tuple[str, ...]], ...]


@dataclass(frozen=True)
class OpeningProfile:
    seed: str
    region_id: str
    landing: Position
    world_time: int
    expedition_count: int
    visible_cells: int
    destructible_cells: tuple[tuple[str, int], ...]
    nearest_destructible_steps: int | None
    initial_threats: int
    initial_active_threats: int
    nearest_active_threat_steps: int | None
    production_site_steps: int | None
    production_sources: tuple[str, ...]
    shore_stations: tuple[str, ...]
    roles: tuple[OpeningRoleProfile, ...]


def _movement_distances(region: Region, start: Position) -> dict[Position, int]:
    """Return initial terrain-only movement distances using player corner rules."""
    links: dict[Position, Position] = {}
    for link in region.vertical_links:
        links[link.first] = link.second
        links[link.second] = link.first

    distances = {start: 0}
    frontier = deque((start,))
    while frontier:
        point = frontier.popleft()
        candidates: list[Position] = []
        for dy in (-1, 0, 1):
            for dx in (-1, 0, 1):
                if not (dx or dy):
                    continue
                target = Position(point.x + dx, point.y + dy, point.z)
                if not terrain_at(region, target).walkable:
                    continue
                if dx and dy:
                    side_a = Position(point.x + dx, point.y, point.z)
                    side_b = Position(point.x, point.y + dy, point.z)
                    if (
                        not terrain_at(region, side_a).walkable
                        and not terrain_at(region, side_b).walkable
                    ):
                        continue
                candidates.append(target)
        destination = links.get(point)
        if destination is not None and terrain_at(region, destination).walkable:
            candidates.append(destination)
        for target in candidates:
            if target in distances:
                continue
            distances[target] = distances[point] + 1
            frontier.append(target)
    return distances


def _terrain_cells(region: Region) -> tuple[tuple[Position, TerrainDefinition], ...]:
    return tuple(
        (Position(x, y, int(z)), definition)
        for z, rows in sorted(region.levels.items(), key=lambda pair: int(pair[0]))
        for y, row in enumerate(rows)
        for x in range(len(row))
        if (definition := terrain_at(region, Position(x, y, int(z)))).destructible
    )


def _interaction_steps(
    distances: dict[Position, int], targets: tuple[Position, ...],
) -> int | None:
    """Movement actions needed before the existing distance-one resolver can act."""
    values = tuple(
        distances[point]
        for target in targets
        for dy in (-1, 0, 1)
        for dx in (-1, 0, 1)
        if (point := Position(target.x + dx, target.y + dy, target.z)) in distances
    )
    return min(values) if values else None


def _is_interactable(distances: dict[Position, int], target: Position) -> bool:
    return any(
        Position(target.x + dx, target.y + dy, target.z) in distances
        for dy in (-1, 0, 1)
        for dx in (-1, 0, 1)
    )


def _carried_kinds(state: GameState, courier_id: str) -> set[str]:
    physical_locations = {
        "pack", "readied", "secondary", "head", "torso", "arms", "hands", "feet",
    }
    return {
        item.kind
        for item in state.items
        if item.owner_id == courier_id
        and item.location in physical_locations
    }


def _optimistic_recipe_closure(
    available: set[str], stations: set[str],
) -> tuple[set[str], tuple[str, ...]]:
    """Find recipes enabled by an ingredient vocabulary, ignoring quantities.

    The result is intentionally optimistic.  If even this closure cannot reach
    a circuit recipe, the generated start has a genuine content-path gap rather
    than merely an inconvenient quantity or travel requirement.
    """
    known = set(available)
    made: set[str] = set()
    changed = True
    while changed:
        changed = False
        for recipe_id, recipe in sorted(RECIPES.items()):
            if (
                recipe.station not in stations
                or recipe_id in made
                or any(kind not in known for kind, _quantity in recipe.inputs)
            ):
                continue
            made.add(recipe_id)
            known.add(recipe.output)
            changed = True
    circuits = tuple(sorted(
        recipe_id for recipe_id in made
        if RECIPES[recipe_id].output.startswith("circuit:")
    ))
    return known, circuits


def _role_profile(
    base_state: GameState,
    courier_id: str,
    distances: dict[Position, int],
    cells: tuple[tuple[Position, TerrainDefinition], ...],
) -> OpeningRoleProfile:
    state = copy.deepcopy(base_state)
    state.active_courier_id = courier_id
    sync_legacy_load(state)
    ensure_initial_field_tool(state)
    courier = state.courier
    if courier is None:
        raise RuntimeError(f"opening audit could not select courier {courier_id!r}")

    actionable = {
        definition.id
        for position, definition in cells
        if _is_interactable(distances, position)
        if any(
            action_id in definition.tool_actions
            and terrain_action_power(state, action_id) > 0
            for action_id in PHYSICAL_TERRAIN_ACTIONS
        )
    }
    targets = tuple(
        position for position, definition in cells if definition.id in actionable
    )
    obtainable = {
        definition.yield_item_kind
        for _position, definition in cells
        if definition.id in actionable and definition.yield_item_kind is not None
    }
    available = _carried_kinds(state, courier_id)
    available.update(obtainable)
    available.update(f"ingredient:{source}" for source in SOURCES[state.active_region_id])
    stations = {"portable", *SHORE_STATIONS[state.active_region_id]}
    known, circuit_recipes = _optimistic_recipe_closure(available, stations)
    missing = tuple(
        (
            recipe_id,
            tuple(sorted(
                kind for kind, _quantity in RECIPES[recipe_id].inputs
                if kind not in known
            )),
        )
        for recipe_id in CORE_FIELD_ENGINE_RECIPES
    )
    return OpeningRoleProfile(
        courier.role,
        courier.id,
        state.weapon,
        state.gear,
        tuple(sorted(actionable)),
        _interaction_steps(distances, targets),
        tuple(sorted(obtainable)),
        circuit_recipes,
        missing,
    )


def opening_profile(seed: str) -> OpeningProfile:
    """Inspect one actual new-run Hearthford start without changing gameplay."""
    state = create_world(seed)
    begin_region(state, state.active_region_id)
    region = state.region
    distances = _movement_distances(region, state.position)
    cells = _terrain_cells(region)
    counts = Counter(definition.id for _position, definition in cells)
    active_threats = tuple(
        distances[threat.position]
        for threat in state.threats
        if threat.status in {"watching", "engaged"} and threat.position in distances
    )
    production_position = site_position(state)
    return OpeningProfile(
        state.seed,
        state.active_region_id,
        state.position,
        state.world_time,
        state.expedition_count,
        len(state.region.seen),
        tuple(sorted(counts.items())),
        _interaction_steps(distances, tuple(position for position, _definition in cells)),
        sum(threat.status != "dead" for threat in state.threats),
        sum(threat.status in {"watching", "engaged"} for threat in state.threats),
        min(active_threats) if active_threats else None,
        distances.get(production_position) if production_position is not None else None,
        SOURCES[state.active_region_id],
        SHORE_STATIONS[state.active_region_id],
        tuple(
            _role_profile(state, courier.id, distances, cells)
            for courier in state.household
        ),
    )


def _range(values: list[int]) -> dict[str, int | float | None]:
    if not values:
        return {"min": None, "median": None, "max": None}
    return {
        "min": min(values),
        "median": statistics.median(values),
        "max": max(values),
    }


def opening_audit(samples: int = 12, *, start: int = 0) -> dict[str, object]:
    """Aggregate deterministic generated-start evidence across stable seeds."""
    if samples < 1 or start < 0:
        raise ValueError(
            "opening audit needs a positive sample count and non-negative start",
        )
    profiles = tuple(
        opening_profile(f"gameplay-opening-{index:04d}")
        for index in range(start, start + samples)
    )
    access = Counter(
        role.role
        for profile in profiles
        for role in profile.roles
        if role.nearest_actionable_steps is not None
    )
    engine = Counter(
        role.role
        for profile in profiles
        for role in profile.roles
        if all(not missing for _recipe, missing in role.core_engine_missing_inputs)
    )
    roles = tuple(role.role for role in profiles[0].roles)
    failures: list[str] = []
    if any((profile.world_time, profile.expedition_count) != (0, 1) for profile in profiles):
        failures.append("regional bootstrap changed the action clock or expedition count")
    if any(profile.nearest_destructible_steps is None for profile in profiles):
        failures.append("a generated opening has no reachable destructible terrain")
    if any(profile.production_site_steps is None for profile in profiles):
        failures.append("a generated opening cannot reach its production site")

    no_access = tuple(role for role in roles if access[role] == 0)
    no_engine = tuple(role for role in roles if engine[role] == 0)
    gaps = []
    if no_access:
        gaps.append(
            "selectable roles without generated Hearthford terrain access: "
            + ", ".join(no_access)
        )
    if no_engine:
        gaps.append(
            "selectable roles without an optimistic fresh-field rack-and-sensor path: "
            + ", ".join(no_engine)
        )
    return {
        "samples": samples,
        "seed_start": start,
        "terrain_access_samples_by_role": {
            role: access[role] for role in roles
        },
        "core_engine_ready_samples_by_role": {
            role: engine[role] for role in roles
        },
        "nearest_destructible_steps": _range([
            value for profile in profiles
            if (value := profile.nearest_destructible_steps) is not None
        ]),
        "initial_threats": _range([profile.initial_threats for profile in profiles]),
        "initial_active_threats": _range([
            profile.initial_active_threats for profile in profiles
        ]),
        "nearest_active_threat_steps": _range([
            value for profile in profiles
            if (value := profile.nearest_active_threat_steps) is not None
        ]),
        "production_site_steps": _range([
            value for profile in profiles
            if (value := profile.production_site_steps) is not None
        ]),
        "gaps": gaps,
        "failures": failures,
        "profiles": [asdict(profile) for profile in profiles],
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--samples", type=int, default=12)
    parser.add_argument("--start", type=int, default=0)
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args()
    result = opening_audit(args.samples, start=args.start)
    if args.json:
        print(json.dumps(result, indent=2, sort_keys=True))
    else:
        print(f"samples: {result['samples']}")
        print(f"nearest destructible steps: {result['nearest_destructible_steps']}")
        print(f"initial active threats: {result['initial_active_threats']}")
        print(f"nearest active threat steps: {result['nearest_active_threat_steps']}")
        print(f"production site steps: {result['production_site_steps']}")
        for gap in result["gaps"]:
            print(f"gap: {gap}")
        for failure in result["failures"]:
            print(f"failure: {failure}")
    raise SystemExit(1 if result["failures"] else 0)


if __name__ == "__main__":
    main()
