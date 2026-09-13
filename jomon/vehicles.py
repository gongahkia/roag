"""Persistent small vehicles and a navigable water approach to Jomon."""

from __future__ import annotations

from functools import lru_cache

from .catalog import CatalogError, load_catalog
from .state import GameState, Position, Vehicle, stage_rng


_CONTENT = load_catalog("vehicles.json", ("harbour", "vehicles"))
HARBOUR = _CONTENT["harbour"]
SPECS = _CONTENT["vehicles"]
VEHICLE_REGIONS = {
    "tug": "harbour",
    "horse_cart": "hearthford",
    "steam_crawler": "rillscar",
    "rootwalker": "greenwold",
    "aether_glider": "whitecairn",
}
_FIELDS = {"name", "domain", "resource", "glyph", "pace", "capacity", "condition", "interior"}
if (not isinstance(HARBOUR, dict) or set(HARBOUR) != {"width", "height", "jomon_dock", "shore_dock"}
        or type(HARBOUR["width"]) is not int or type(HARBOUR["height"]) is not int
        or not 48 <= HARBOUR["width"] <= 120 or not 24 <= HARBOUR["height"] <= 60
        or not isinstance(SPECS, dict) or set(SPECS) != set(VEHICLE_REGIONS)):
    raise CatalogError("invalid vehicle harbour or roster")
for dock in ("jomon_dock", "shore_dock"):
    point = HARBOUR[dock]
    if (not isinstance(point, list) or len(point) != 2
            or any(type(value) is not int for value in point)
            or not 1 < point[0] < HARBOUR["width"] - 2
            or not 1 < point[1] < HARBOUR["height"] - 2):
        raise CatalogError(f"invalid harbour {dock}")
for vehicle_id, spec in SPECS.items():
    if (not isinstance(spec, dict) or set(spec) != _FIELDS
            or spec["domain"] not in {"water", "road", "rough", "air"}
            or any(not isinstance(spec[key], str) or not spec[key] for key in ("name", "resource"))
            or not isinstance(spec["glyph"], str) or len(spec["glyph"]) != 1 or not spec["glyph"].isascii() or not spec["glyph"].isprintable()
            or any(type(spec[key]) is not int or not 1 <= spec[key] <= 200 for key in ("pace", "capacity", "condition"))
            or spec["pace"] > 3
            or not isinstance(spec["interior"], list) or not 3 <= len(spec["interior"]) <= 12
            or any(not isinstance(row, str) or not row.isascii() or not row.isprintable() or len(row) > 48
                   for row in spec["interior"])):
        raise CatalogError(f"invalid vehicle definition: {vehicle_id}")
if SPECS["tug"]["domain"] != "water":
    raise CatalogError("the tug must navigate water")

JOMON_DOCK = Position(*HARBOUR["jomon_dock"])
SHORE_DOCK = Position(*HARBOUR["shore_dock"])


@lru_cache(maxsize=32)
def harbour_rows(seed: str) -> tuple[str, ...]:
    width, height = HARBOUR["width"], HARBOUR["height"]
    grid = [["~" for _ in range(width)] for _ in range(height)]
    for x in range(width):
        grid[0][x] = grid[-1][x] = "#"
    for y in range(height):
        grid[y][0] = grid[y][-1] = "#"
    rng = stage_rng(seed, "tug-harbour")
    for cx, cy in ((21, 6), (39, height - 6), (57, 6)):
        for y in range(cy - 3, cy + 4):
            for x in range(cx - 5, cx + 6):
                if not (1 <= x < width - 1 and 1 <= y < height - 1):
                    continue
                reach = abs(x - cx) + abs(y - cy)
                if reach < 5:
                    grid[y][x] = "#"
                elif reach < 8:
                    grid[y][x] = "w"
    for _ in range(width):
        x, y = rng.randrange(8, width - 8), rng.randrange(2, height - 2)
        if abs(y - JOMON_DOCK.y) > 2 and grid[y][x] == "~":
            grid[y][x] = "w"
    # A sounded central lane connects both moorings for every seed.
    for x in range(JOMON_DOCK.x, SHORE_DOCK.x + 1):
        grid[JOMON_DOCK.y][x] = "~"
    grid[JOMON_DOCK.y][JOMON_DOCK.x] = "J"
    grid[SHORE_DOCK.y][SHORE_DOCK.x] = "L"
    return tuple("".join(row) for row in grid)


def _regional_home(state: GameState, vehicle_id: str) -> Position:
    region = state.regions[VEHICLE_REGIONS[vehicle_id]]
    landing = region.landmarks["landing"]
    domain = SPECS[vehicle_id]["domain"]
    candidates = (
        Position(x, y)
        for y in range(max(1, landing.y - 8), min(region.height - 1, landing.y + 9))
        for x in range(max(1, landing.x - 8), min(region.width - 1, landing.x + 9))
    )
    usable = [point for point in candidates if point != landing and _terrain_ok(
        domain, region.levels["0"][point.y][point.x], regional=True,
    )]
    if not usable:
        raise CatalogError(f"no vehicle stand near {region.name}'s landing")
    return min(usable, key=lambda point: (
        abs(point.x - landing.x) + abs(point.y - landing.y),
        region.levels["0"][point.y][point.x] != "=", point.y, point.x,
    ))


def initialise_vehicles(state: GameState) -> None:
    state.vehicles = {}
    for vehicle_id, region_id in VEHICLE_REGIONS.items():
        home = JOMON_DOCK if region_id == "harbour" else _regional_home(state, vehicle_id)
        spec = SPECS[vehicle_id]
        state.vehicles[vehicle_id] = Vehicle(
            vehicle_id, region_id, home, home, spec["capacity"], spec["condition"],
        )


def active_vehicle(state: GameState) -> Vehicle | None:
    return state.vehicles.get(state.active_vehicle_id) if state.active_vehicle_id else None


def vehicle_at(state: GameState, position: Position) -> Vehicle | None:
    region_id = "harbour" if state.location == "jomon" and state.jomon_space == "harbour" else state.active_region_id if state.location == "region" else None
    return next((vehicle for vehicle in state.vehicles.values()
                 if vehicle.region_id == region_id and vehicle.position == position), None)


def _terrain_ok(domain: str, tile: str, *, regional: bool) -> bool:
    if domain == "water":
        return not regional and tile in {"~", "w", "J", "L"}
    if domain == "road":
        return tile in {"=", ".", ","}
    if domain == "rough":
        return tile in {"=", ".", ",", "m", "t", "w", "r", "q"}
    return regional and tile not in {" ", "#", "+", "O"}


def _tile(state: GameState, point: Position) -> str:
    if state.location == "jomon" and state.jomon_space == "harbour":
        rows = harbour_rows(state.seed)
        return rows[point.y][point.x] if 0 <= point.y < len(rows) and 0 <= point.x < len(rows[point.y]) else " "
    rows = state.region.levels.get(str(point.z), [])
    if not 0 <= point.y < len(rows) or not 0 <= point.x < len(rows[point.y]):
        return " "
    from .world import position_key

    return state.region.tile_changes.get(position_key(point), rows[point.y][point.x])


def board_tug(state: GameState):
    from .actions import _plain, _time_result
    from .vessel import JOMON_GANGPLANK

    tug = state.vehicles["tug"]
    if state.location != "jomon" or state.jomon_space != "vessel" or state.position != JOMON_GANGPLANK:
        return _plain(state, "Board the tug from Jomon's gangplank.")
    if state.voyage_status == "active" or state.courier is None or not state.courier.alive:
        return _plain(state, "The tug cannot cast off during a voyage crisis or without an able courier.")
    if tug.position != JOMON_DOCK:
        return _plain(state, "The tug is not at Jomon's mooring.")
    state.jomon_space, state.position, state.active_vehicle_id = "harbour", JOMON_DOCK, "tug"
    return _time_result(state, "You board the steam tug; Jomon lies at J and the regional shore at L.")


def board_region_vehicle(state: GameState):
    from .actions import _plain, _time_result

    vehicle = vehicle_at(state, state.position)
    if state.location != "region" or vehicle is None:
        return _plain(state, "No vehicle is here to board.")
    if vehicle.condition <= 0:
        return _plain(state, f"The {SPECS[vehicle.id]['name']} needs repair before moving.")
    state.active_vehicle_id = vehicle.id
    state.aimed_target = None
    return _time_result(state, f"You take the controls of the {SPECS[vehicle.id]['name']}. Tab opens its interior.")


def disembark(state: GameState):
    from .actions import _plain, _time_result
    from .world import is_walkable

    vehicle = active_vehicle(state)
    if vehicle is None or state.location != "region":
        return _plain(state, "There is no safe place to disembark.")
    if not is_walkable(state, state.position, ignore_threat=True):
        return _plain(state, "Steer onto firm, walkable ground before disembarking.")
    state.active_vehicle_id = None
    return _time_result(state, f"You step down from the {SPECS[vehicle.id]['name']}; it remains here.")


def navigate(state: GameState, dx: int, dy: int):
    from .actions import _plain, _time_result

    vehicle = active_vehicle(state)
    if vehicle is None:
        return _plain(state, "No vehicle is under your control.")
    spec = SPECS[vehicle.id]
    if vehicle.condition <= 0:
        return _plain(state, "The vehicle is disabled. Open its interior with Tab to jury-rig it.")
    water = vehicle.region_id == "harbour"
    if water != (state.location == "jomon" and state.jomon_space == "harbour"):
        return _plain(state, "The vehicle is not in this landscape.")
    pace = 1 if vehicle.fuel == 0 else spec["pace"]
    current = state.position
    travelled = 0
    strain = 0
    for _ in range(pace):
        target = Position(current.x + dx, current.y + dy, current.z)
        tile = _tile(state, target)
        if not _terrain_ok(spec["domain"], tile, regional=not water):
            break
        if dx and dy and spec["domain"] != "air":
            sides = (Position(current.x + dx, current.y, current.z), Position(current.x, current.y + dy, current.z))
            if not any(_terrain_ok(spec["domain"], _tile(state, side), regional=not water) for side in sides):
                break
        if not water and any(actor.position == target and actor.status in {"watching", "engaged"} for actor in state.combatants):
            break
        extra = 1 if tile in {"w", ",", "m"} or (spec["domain"] == "air" and state.weather == "crosswind") else 0
        cost = 1 + extra
        if vehicle.fuel < cost and vehicle.id != "tug":
            break
        if vehicle.fuel > 0 and vehicle.fuel < cost:
            break
        current = target
        travelled += 1
        if vehicle.fuel:
            vehicle.fuel -= cost
        if extra:
            strain += 1
        if water and tile in {"J", "L"}:
            break
    if not travelled:
        if vehicle.fuel == 0 and vehicle.id != "tug":
            return _plain(state, "The vehicle has no charge. Open its interior with Tab to service it.")
        return _plain(state, "That heading is blocked by terrain, a shoal, or an actor.")
    state.position = vehicle.position = current
    vehicle.travelled += travelled
    if strain and vehicle.travelled % 6 < travelled:
        vehicle.condition = max(0, vehicle.condition - 1)
    if state.location == "region":
        state.last_move_turn = state.world_time
        state.aimed_target = None
        state.noise += 1 if vehicle.id in {"steam_crawler", "rootwalker"} else 0
        # The ordinary world tick still resolves patrols and hazards at the destination.
        from .world import displayed_tile
        from .inventory import apply_terrain_status

        apply_terrain_status(state, displayed_tile(state, current))
    message = f"{spec['name']} travels {travelled} tile{'s' if travelled != 1 else ''}; {vehicle.fuel}/{spec['capacity']} {spec['resource']} remains."
    if vehicle.id == "tug" and vehicle.fuel == 0:
        message += " Sweep oars keep the tug moving, one tile per two actions."
    if vehicle.condition == 0:
        message += " The frame needs a jury-rig before it can move again."
    return _time_result(state, message, steps=2 if vehicle.id == "tug" and vehicle.fuel == 0 else 1)


def service(state: GameState, *, repair: bool = False):
    from .actions import _plain, _time_result
    from .inventory import consume_carried

    vehicle = active_vehicle(state)
    if vehicle is None:
        return _plain(state, "No vehicle is being serviced.")
    spec = SPECS[vehicle.id]
    at_stand = vehicle.position == vehicle.home or vehicle.id == "tug" and vehicle.position == SHORE_DOCK
    if repair:
        if vehicle.condition >= spec["condition"]:
            return _plain(state, "The frame is already sound.")
        if at_stand and state.trade_credit > 0:
            state.trade_credit -= 1
            vehicle.condition = min(spec["condition"], vehicle.condition + 4)
            return _time_result(state, "A counted repair at the stand restores four frame points.", steps=2)
        vehicle.condition = min(spec["condition"], vehicle.condition + 1)
        return _time_result(state, "A three-action jury-rig restores one frame point.", steps=3)
    if vehicle.fuel >= spec["capacity"]:
        return _plain(state, "The reserve is already full.")
    if vehicle.id in {"horse_cart", "rootwalker"}:
        vehicle.fuel = min(spec["capacity"], vehicle.fuel + 12)
        return _time_result(state, f"The {spec['resource']} recovers by twelve after a three-action halt.", steps=3)
    if vehicle.id == "aether_glider":
        if state.courier is None or state.courier.mana < 2:
            return _plain(state, "The glider needs two personal mana to recharge.")
        state.courier.mana -= 2
        vehicle.fuel = min(spec["capacity"], vehicle.fuel + 12)
        return _time_result(state, "Two mana enter the glider crystal; twelve aether charge returns.")
    if consume_carried(state, "commodity:charcoal"):
        vehicle.fuel = min(spec["capacity"], vehicle.fuel + 48)
        return _time_result(state, "One carried charcoal lot feeds the boiler for forty-eight charge.")
    if at_stand and state.trade_credit > 0:
        state.trade_credit -= 1
        vehicle.fuel = min(spec["capacity"], vehicle.fuel + 48)
        return _time_result(state, "One trade credit buys a counted charcoal charge at the stand.")
    return _plain(state, "Bring a charcoal lot, or refuel for one credit at a mooring or vehicle stand.")


def interior_lines(state: GameState) -> tuple[str, list[str]]:
    vehicle = active_vehicle(state)
    if vehicle is None:
        return "NO VEHICLE", ["No vehicle is under your control."]
    spec = SPECS[vehicle.id]
    lines = [
        f"{spec['name']} — {spec['domain']} navigation",
        *spec["interior"],
        f"Reserve: {vehicle.fuel}/{spec['capacity']} {spec['resource']}; frame {vehicle.condition}/{spec['condition']}.",
        f"Location: {state.active_region_id if state.location == 'region' else 'open water'} at {vehicle.position.x},{vehicle.position.y}.",
        "Arrows/HJKL steer outside. E disembarks only on safe ground or a mooring.",
        "R service reserve; F repair frame; Escape returns to the landscape.",
    ]
    if vehicle.id == "tug":
        lines.append("J marks Jomon; L marks the regional shore. No fuel: sweep oars remain usable.")
    return spec["name"].upper(), lines


def validate_vehicles(state: GameState) -> None:
    if not isinstance(state.vehicles, dict) or set(state.vehicles) != set(VEHICLE_REGIONS):
        raise ValueError("invalid vehicle roster")
    for vehicle_id, vehicle in state.vehicles.items():
        spec = SPECS[vehicle_id]
        if (not isinstance(vehicle, Vehicle) or vehicle.id != vehicle_id
                or vehicle.region_id != VEHICLE_REGIONS[vehicle_id]
                or type(vehicle.fuel) is not int or not 0 <= vehicle.fuel <= spec["capacity"]
                or type(vehicle.condition) is not int or not 0 <= vehicle.condition <= spec["condition"]
                or type(vehicle.travelled) is not int or vehicle.travelled < 0
                or vehicle.home != (JOMON_DOCK if vehicle_id == "tug" else _regional_home(state, vehicle_id))
                or not _terrain_ok(spec["domain"], _tile_for_validation(state, vehicle), regional=vehicle_id != "tug")):
            raise ValueError(f"invalid vehicle state: {vehicle_id}")
    if state.active_vehicle_id is not None:
        active = state.vehicles.get(state.active_vehicle_id)
        area = "harbour" if state.location == "jomon" and state.jomon_space == "harbour" else state.active_region_id if state.location == "region" else None
        if active is None or active.region_id != area or active.position != state.position:
            raise ValueError("active vehicle is not under the courier")
    if state.jomon_space == "harbour" and state.location == "jomon" and state.active_vehicle_id != "tug":
        raise ValueError("open water requires the tug")
    if type(state.expedition_by_tug) is not bool or type(state.returning_by_tug) is not bool:
        raise ValueError("invalid tug expedition flag")
    if state.expedition_by_tug and (state.location != "region" or state.active_vehicle_id == "tug"):
        raise ValueError("invalid shore expedition")
    if state.returning_by_tug and (state.location != "jomon" or state.jomon_space != "harbour"):
        raise ValueError("invalid tug return")


def _tile_for_validation(state: GameState, vehicle: Vehicle) -> str:
    if vehicle.id == "tug":
        rows = harbour_rows(state.seed)
    else:
        region = state.regions[vehicle.region_id]
        rows = region.levels["0"]
    point = vehicle.position
    if not 0 <= point.y < len(rows) or not 0 <= point.x < len(rows[point.y]) or point.z != 0:
        return " "
    if vehicle.id == "tug":
        return rows[point.y][point.x]
    from .world import position_key

    return state.regions[vehicle.region_id].tile_changes.get(position_key(point), rows[point.y][point.x])
