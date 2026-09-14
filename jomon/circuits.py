"""Sparse, buildable electrical traces and finite pulse-driven work devices."""

from __future__ import annotations

from collections import deque

from .catalog import CatalogError, load_catalog
from .state import CircuitCell, GameState, Position


_CATALOG = load_catalog("circuits.json", ("parts", "fixtures"))
PARTS = _CATALOG["parts"]
FIXTURES = _CATALOG["fixtures"]
if not isinstance(PARTS, dict) or not {
    "trace", "via", "rack", "cell", "switch", "lamp", "gate", "drain",
    "sensor", "relay", "counter", "piston", "crate",
} <= set(PARTS):
    raise CatalogError("circuits.json has invalid circuit parts")
BEHAVIORS = {"conductor", "source", "fuel", "gated", "directional", "counter", "block"}
if any(not isinstance(part, dict) or set(part) != {"name", "glyph", "build_key", "behavior", "layers", "description"}
       or not isinstance(part["name"], str) or not part["name"]
       or not isinstance(part["description"], str) or not part["description"]
       or not isinstance(part["glyph"], str) or len(part["glyph"]) != 1
       or not isinstance(part["behavior"], str) or part["behavior"] not in BEHAVIORS
       or (part["build_key"] is not None and (not isinstance(part["build_key"], str) or len(part["build_key"]) != 1))
       or not isinstance(part["layers"], list)
       or any(not isinstance(layer, str) or layer not in {"surface", "buried"} for layer in part["layers"])
       for part in PARTS.values()):
    raise CatalogError("circuits.json has invalid behavior or layer metadata")
PLACED_PARTS = {kind for kind, part in PARTS.items() if part["behavior"] != "fuel"}
BUILD_KEYS = {part["build_key"]: kind for kind, part in PARTS.items() if kind in PLACED_PARTS}
if (None in BUILD_KEYS or len(BUILD_KEYS) != len(PLACED_PARTS)
        or any(part["behavior"] == "fuel" and part["build_key"] is not None for part in PARTS.values())):
    raise CatalogError("circuits.json has duplicate or missing build keys")
DIRECTIONS = {"north": (0, -1), "east": (1, 0), "south": (0, 1), "west": (-1, 0)}
FACING_GLYPHS = {"north": "^", "east": ">", "south": "v", "west": "<"}
SENSOR_MODES = ("mass", "water", "threat")
PULSE_INTERVAL = 6
CELL_CHARGE = 24
SIGNAL_SPAN = 64
DEVICE_HOLD = 7
MAX_CELLS = 4096
CRATE_LOAD_LIMIT = 12


def initialise_circuits(state: GameState) -> None:
    """Install small, authored examples only when a new world is created."""
    if state.circuits:
        return
    if not isinstance(FIXTURES, list):
        raise CatalogError("circuits.json has invalid fixtures")
    for fixture in FIXTURES:
        if (not isinstance(fixture, dict) or set(fixture) != {"id", "region", "anchor", "cells"}
                or any(not isinstance(fixture[field], str) or not fixture[field] for field in ("id", "region", "anchor"))):
            raise CatalogError("circuits.json has invalid fixture identity")
        if fixture["region"] == "vessel":
            from .ship_crises import HAZARD_STATIONS

            anchor = HAZARD_STATIONS.get(fixture["anchor"])
            space = "vessel"
        else:
            region = state.regions.get(fixture["region"])
            anchor = region.landmarks.get(fixture["anchor"]) if region else None
            space = f"region:{fixture['region']}"
        if anchor is None or not isinstance(fixture["cells"], list):
            raise CatalogError("circuits.json has missing fixture anchor")
        for row in fixture["cells"]:
            if (not isinstance(row, dict) or not {"kind", "offset", "layer"} <= set(row)
                    or set(row) - {"kind", "offset", "layer", "charge", "mode", "threshold"}
                    or not isinstance(row["kind"], str) or row["kind"] not in PLACED_PARTS
                    or not isinstance(row["layer"], str) or row["layer"] not in PARTS[row["kind"]]["layers"]
                    or not isinstance(row["offset"], list) or len(row["offset"]) != 2
                    or any(type(value) is not int for value in row["offset"])
                    or type(row.get("charge", 0)) is not int or not 0 <= row.get("charge", 0) <= 2 * CELL_CHARGE
                    or row.get("mode", "mass") not in SENSOR_MODES
                    or type(row.get("threshold", 2)) is not int or not 1 <= row.get("threshold", 2) <= 4):
                raise CatalogError("circuits.json has invalid fixture fitting")
            point = Position(anchor.x + row["offset"][0], anchor.y + row["offset"][1], anchor.z)
            if _terrain_at(state, space, point) in {" ", "#", "~", "T"}:
                raise CatalogError("circuits.json fixture crosses blocked terrain")
            key = cell_key(space, point, row["layer"])
            if key in state.circuits:
                raise CatalogError("circuits.json has overlapping fixture fittings")
            state.circuits[key] = CircuitCell(space, point, row["layer"], row["kind"],
                                              charge=row.get("charge", 0), mode=row.get("mode", "mass"),
                                              threshold=row.get("threshold", 2))
    validate_circuits(state)


def space_id(state: GameState) -> str | None:
    if state.location == "region":
        return f"region:{state.active_region_id}"
    if state.location == "jomon" and state.jomon_space == "vessel":
        return "vessel"
    return None


def cell_key(space: str, position: Position, layer: str) -> str:
    return f"{space}/{position.x},{position.y},{position.z}/{layer}"


def cell_at(state: GameState, position: Position, layer: str = "surface") -> CircuitCell | None:
    space = space_id(state)
    return state.circuits.get(cell_key(space, position, layer)) if space else None


def offset(position: Position, facing: str, steps: int = 1) -> Position:
    dx, dy = DIRECTIONS[facing]
    return Position(position.x + dx * steps, position.y + dy * steps, position.z)


def piston_head_at(state: GameState, position: Position) -> CircuitCell | None:
    space = space_id(state)
    if space is None:
        return None
    return next((cell for cell in state.circuits.values() if cell.space == space and cell.kind == "piston"
                 and active(state, cell) and offset(cell.position, cell.facing) == position), None)


def _threats_in_space(state: GameState, space: str):
    if not space.startswith("region:"):
        return state.vessel_threats
    return state.region_threats.get(space.split(":", 1)[1], [])


def sensor_active(state: GameState, cell: CircuitCell) -> bool:
    point = cell.position
    if cell.mode == "water":
        key = f"{point.x},{point.y},{point.z}"
        if cell.space == space_id(state) and state.water.get(key, 0) >= cell.threshold:
            return True
        if cell.space.startswith("region:"):
            region = state.regions.get(cell.space.split(":", 1)[1])
            material = region.materials.get(key) if region else None
            return bool(material and material.water >= cell.threshold)
        return bool(state.vessel_materials.get(key) and state.vessel_materials[key].water >= cell.threshold)
    if cell.mode == "threat":
        return any(threat.status in {"watching", "engaged"} and threat.position.z == point.z
                   and max(abs(threat.position.x - point.x), abs(threat.position.y - point.y)) <= cell.threshold
                   for threat in _threats_in_space(state, cell.space))
    if cell.space == space_id(state) and state.position == point:
        return True
    if any(threat.status in {"watching", "engaged"} and threat.position == point
           for threat in _threats_in_space(state, cell.space)):
        return True
    if any(schedule.position == point and (
           schedule.area == cell.space or cell.space == "vessel" and schedule.area.startswith("vessel:"))
           for schedule in state.actor_schedules.values()):
        return True
    if state.circuits.get(cell_key(cell.space, point, "surface"), None) and (
            state.circuits[cell_key(cell.space, point, "surface")].kind == "crate"):
        return True
    spatial = cell.space.split(":", 1)[1] if cell.space.startswith("region:") else "jomon"
    return any(item.location == "ground" and item.region_id == spatial
               and item.ground_position == point for item in state.items)


def active(state: GameState, cell: CircuitCell) -> bool:
    return cell.kind in {"lamp", "gate", "drain", "piston"} and 0 < cell.active_until >= state.world_time


def glyph(state: GameState, position: Position, layer: str = "surface") -> str | None:
    cell = cell_at(state, position, layer)
    if cell is None:
        return None
    if cell.kind == "gate":
        return "/" if active(state, cell) else "#"
    if cell.kind == "lamp":
        return "*" if active(state, cell) else "l"
    if cell.kind == "switch":
        return "S" if cell.enabled else "s"
    if cell.kind in {"piston", "relay"}:
        return FACING_GLYPHS[cell.facing]
    if cell.kind == "sensor":
        return "!" if sensor_active(state, cell) else "?"
    if cell.kind == "counter" and cell.phase == "wire":
        return str(cell.count)
    if cell.phase == "head":
        return "@"
    if cell.phase == "tail":
        return ";"
    return PARTS[cell.kind]["glyph"]


def _available_item(state: GameState, kind: str):
    return next((item for item in state.items if item.kind == f"circuit:{kind}"
                 and item.location == "pack" and item.owner_id == state.active_courier_id
                 and item.quantity > 0), None)


def item_count(state: GameState, kind: str) -> int:
    return sum(item.quantity for item in state.items if item.kind == f"circuit:{kind}"
               and item.location == "pack" and item.owner_id == state.active_courier_id)


def _spend_item(state: GameState, kind: str) -> None:
    item = _available_item(state, kind)
    if item is None:
        raise ValueError(f"no carried {kind}")
    item.quantity -= 1
    if item.quantity == 0:
        item.location, item.owner_id = "destroyed", None


def _target_reason(state: GameState, position: Position, layer: str) -> str | None:
    from .world import base_tile, distance

    if not space_id(state):
        return "Circuit work is available on Jomon's working decks or in a region."
    if state.active_vehicle_id:
        return "Disembark before laying a fixed trace."
    if state.courier is None or not state.courier.alive:
        return "An active courier must do the fitting."
    if layer not in {"surface", "buried"}:
        return "Choose the surface or buried layer."
    if position.z != state.position.z or distance(state.position, position) > 2:
        return "Work on this level within two squares of the courier."
    tile = base_tile(state, position)
    if tile in {" ", "~", "T"}:
        return "There is no sound floor or wall footing at that square."
    return None


def place(state: GameState, position: Position, layer: str, kind: str) -> tuple[bool, str]:
    from .actions import _advance_world
    from .world import base_tile, is_walkable

    reason = _target_reason(state, position, layer)
    if reason:
        return False, reason
    if kind not in PLACED_PARTS:
        return False, "Choose a buildable circuit part."
    if len(state.circuits) >= MAX_CELLS:
        return False, "The world circuit register is full."
    if layer not in PARTS[kind]["layers"]:
        return False, f"A {PARTS[kind]['name'].lower()} cannot be fitted on the {layer} layer."
    if layer == "surface" and base_tile(state, position) not in {".", ",", "_", "m", "r", "q", "%", "="}:
        return False, "Keep surface fittings on plain ground; bury a trace beneath a fixture."
    space = space_id(state)
    key = cell_key(space, position, layer)
    if key in state.circuits:
        return False, "That circuit layer is already occupied; reclaim it first."
    if layer == "surface" and not is_walkable(state, position, ignore_threat=True):
        return False, "A surface fitting needs passable ground; bury a trace beneath a wall."
    if kind in {"gate", "crate", "piston"} and position == state.position:
        return False, "Stand clear of the square before fitting a blocking device."
    if _available_item(state, kind) is None:
        return False, f"Carry a crafted {PARTS[kind]['name'].lower()} in your pack."
    _spend_item(state, kind)
    state.circuits[key] = CircuitCell(space, position, layer, kind)
    _advance_world(state)
    message = f"You fit {PARTS[kind]['name'].lower()} at {position.x},{position.y},{position.z} ({layer})."
    state.add_message(message, priority=3)
    return True, message


def reclaim(state: GameState, position: Position, layer: str) -> tuple[bool, str]:
    from .actions import _advance_world
    from .inventory import InventoryTransaction, auto_place, create_item, sync_legacy_load

    reason = _target_reason(state, position, layer)
    if reason:
        return False, reason
    space = space_id(state)
    key = cell_key(space, position, layer)
    cell = state.circuits.get(key)
    if cell is None:
        return False, "No fitted part occupies that layer."
    transaction = InventoryTransaction.begin(state)
    item = create_item(state, f"circuit:{cell.kind}", "reclaimed circuit fitting")
    if not auto_place(state, item.id, "pack", owner_id=state.active_courier_id):
        transaction.cancel(state)
        return False, "Clear space in the pack before reclaiming the fitting."
    del state.circuits[key]
    sync_legacy_load(state)
    _advance_world(state)
    message = f"You reclaim the {PARTS[cell.kind]['name'].lower()} from the {layer} layer."
    state.add_message(message, priority=3)
    return True, message


def operate(state: GameState, position: Position, layer: str, action: str = "primary") -> tuple[bool, str]:
    from .actions import _advance_world

    reason = _target_reason(state, position, layer)
    if reason:
        return False, reason
    cell = cell_at(state, position, layer)
    if cell is None:
        return False, "No circuit part occupies that layer."
    if action == "secondary" and cell.kind == "piston":
        cell.sticky = not cell.sticky
        message = f"The piston is now {'sticky' if cell.sticky else 'push-only'}."
    elif action == "secondary" and cell.kind == "counter":
        cell.count = 0
        message = "You reset the counted relay."
    elif action == "secondary" and cell.kind == "sensor":
        cell.threshold = 1 if cell.threshold == 3 else cell.threshold + 1
        message = f"The field sensor threshold is now {cell.threshold}."
    elif action != "primary":
        return False, "This fitting has no secondary setting."
    elif cell.kind == "switch":
        cell.enabled = not cell.enabled
        if not cell.enabled:
            cell.phase = "wire"
            cell.signal_steps = 0
        message = f"You {'close' if cell.enabled else 'open'} the knife switch."
    elif cell.kind == "rack":
        if cell.charge > CELL_CHARGE:
            return False, "The rack already holds more than one cell's remaining charge."
        if _available_item(state, "cell") is None:
            return False, "A crafted galvanic cell is needed to charge the rack."
        _spend_item(state, "cell")
        cell.charge += CELL_CHARGE
        message = f"You fit a galvanic cell; the rack holds {cell.charge} pulses."
    elif cell.kind in {"piston", "relay"}:
        if cell.kind == "piston" and active(state, cell):
            return False, "Wait for the piston to retract before rotating its crank."
        compass = tuple(DIRECTIONS)
        cell.facing = compass[(compass.index(cell.facing) + 1) % len(compass)]
        message = f"You turn the {PARTS[cell.kind]['name'].lower()} {cell.facing}."
    elif cell.kind == "sensor":
        cell.mode = SENSOR_MODES[(SENSOR_MODES.index(cell.mode) + 1) % len(SENSOR_MODES)]
        message = f"The field sensor now reads {cell.mode}."
    elif cell.kind == "counter":
        cell.threshold = 2 if cell.threshold == 4 else cell.threshold + 1
        cell.count = 0
        message = f"The counted relay now passes every {cell.threshold}th pulse."
    else:
        return False, "This fitting has no manual control."
    cell.last_event = message
    _advance_world(state)
    state.add_message(message, priority=3)
    return True, message


def _neighbors(state: GameState, cell: CircuitCell) -> list[CircuitCell]:
    space, point, layer = cell.space, cell.position, cell.layer
    found = []
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            if dx == dy == 0:
                continue
            other = state.circuits.get(cell_key(space, Position(point.x + dx, point.y + dy, point.z), layer))
            if other is not None and other.kind != "crate":
                found.append(other)
    other_layer = "buried" if layer == "surface" else "surface"
    other = state.circuits.get(cell_key(space, point, other_layer))
    if other is not None and other.kind != "crate" and (cell.kind == "via" or other.kind == "via"):
        found.append(other)
    if cell.kind == "via" and space.startswith("region:"):
        region = state.regions.get(space.split(":", 1)[1])
        if region:
            for link in region.vertical_links:
                endpoint = link.second if link.first == point else link.first if link.second == point else None
                if endpoint is not None:
                    other = state.circuits.get(cell_key(space, endpoint, layer))
                    if other is not None and other.kind == "via":
                        found.append(other)
    return found


def _can_transmit(state: GameState, sender: CircuitCell, receiver: CircuitCell) -> bool:
    if receiver.kind == "rack" or sender.kind == "switch" and not sender.enabled:
        return False
    if sender.kind == "sensor" and not sensor_active(state, sender):
        return False
    if receiver.kind == "switch" and not receiver.enabled:
        return False
    if receiver.kind == "sensor" and not sensor_active(state, receiver):
        return False
    if sender.kind == "relay" and receiver.position != offset(sender.position, sender.facing):
        return False
    return receiver.kind != "relay" or sender.position == offset(receiver.position, receiver.facing, -1)


def _route(state: GameState, start: CircuitCell, goal: set[str], *, open_only: bool = True) -> list[CircuitCell] | None:
    start_key = cell_key(start.space, start.position, start.layer)
    parents = {start_key: None}
    queue = deque([start_key])
    while queue:
        current_key = queue.popleft()
        current = state.circuits[current_key]
        for neighbor in _neighbors(state, current):
            key = cell_key(neighbor.space, neighbor.position, neighbor.layer)
            if key in parents or open_only and not _can_transmit(state, current, neighbor):
                continue
            parents[key] = current_key
            if key in goal:
                path = [key]
                while parents[path[-1]] is not None:
                    path.append(parents[path[-1]])
                return [state.circuits[part] for part in reversed(path)]
            queue.append(key)
    return None


def _receivers(state: GameState, space: str) -> set[str]:
    return {key for key, cell in state.circuits.items()
            if cell.space == space and cell.kind in {"lamp", "gate", "drain", "piston", "counter"}}


def _has_receiver(state: GameState, rack: CircuitCell) -> bool:
    receivers = _receivers(state, rack.space)
    route = _route(state, rack, receivers) if receivers else None
    return route is not None and len(route) - 1 <= SIGNAL_SPAN


def _blocked_link(state: GameState, path: list[CircuitCell]) -> str:
    for sender, receiver in zip(path, path[1:]):
        if _can_transmit(state, sender, receiver):
            continue
        if receiver.kind == "rack":
            part = receiver
        elif (sender.kind == "switch" and not sender.enabled
              or sender.kind == "sensor" and not sensor_active(state, sender)):
            part = sender
        elif (receiver.kind == "switch" and not receiver.enabled
              or receiver.kind == "sensor" and not sensor_active(state, receiver)):
            part = receiver
        else:
            part = sender if sender.kind == "relay" else receiver
        where = f"{part.position.x},{part.position.y},z{part.position.z:+d}"
        if part.kind == "switch":
            return f"Knife switch at {where} is open."
        if part.kind == "sensor":
            return f"{part.mode.title()} sensor at {where} is clear (needs {part.threshold})."
        if part.kind == "relay":
            return f"Relay at {where} faces {part.facing}; check input and output."
        return f"Rack at {where} cannot pass another source's pulse."
    return "No open source route."


def _head_inputs(state: GameState, cell: CircuitCell) -> list[CircuitCell]:
    return [other for other in _neighbors(state, cell)
            if other.phase == "head" and other.signal_steps > 0
            and _can_transmit(state, other, cell)]


def _heads_into(state: GameState, cell: CircuitCell) -> int:
    return len(_head_inputs(state, cell))


def _next_transition(state: GameState, cell: CircuitCell, *, at_time: int | None = None) -> tuple[str, int, int]:
    behavior = PARTS[cell.kind]["behavior"]
    if behavior == "block":
        return "wire", 0, 0
    if behavior == "source":
        tick = state.world_time if at_time is None else at_time
        phase = "head" if tick % PULSE_INTERVAL == 0 and cell.charge > 0 and _has_receiver(state, cell) else (
            "tail" if cell.phase == "head" else "wire")
        return phase, cell.count, SIGNAL_SPAN if phase == "head" else 0
    if behavior == "gated" and not (cell.enabled if cell.kind == "switch" else sensor_active(state, cell)):
        return "wire", cell.count, 0
    if cell.phase == "head":
        return "tail", cell.count, 0
    if cell.phase == "tail":
        return "wire", cell.count, 0
    inputs = _head_inputs(state, cell)
    if len(inputs) not in (1, 2):
        return "wire", cell.count, 0
    steps = max(other.signal_steps for other in inputs) - 1
    if behavior == "counter":
        count = cell.count + 1
        return ("head", 0, steps) if count >= cell.threshold else ("wire", count, 0)
    return "head", cell.count, steps


def next_phase(state: GameState, cell: CircuitCell, *, at_time: int | None = None) -> tuple[str, int]:
    phase, count, _ = _next_transition(state, cell, at_time=at_time)
    return phase, count


def diagnostic_lines(state: GameState, cell: CircuitCell) -> list[str]:
    if cell.kind == "crate":
        cargo = _crate_cargo(state, cell)
        return [f"Freight crate: {len(cargo)} ground item(s), {_crate_weight(state, cell)}/{CRATE_LOAD_LIMIT} kg.",
                "Pistons move up to three in a line; contents ride with their crate.",
                "It can cover a buried mass sensor or circuit trace."]
    seen = {cell_key(cell.space, cell.position, cell.layer)}
    queue = deque([cell])
    racks = []
    network = []
    while queue:
        current = queue.popleft()
        network.append(current)
        if current.kind == "rack":
            racks.append(current)
        for neighbor in _neighbors(state, current):
            key = cell_key(neighbor.space, neighbor.position, neighbor.layer)
            if key not in seen:
                seen.add(key)
                queue.append(neighbor)
    next_state, next_count = next_phase(state, cell, at_time=state.world_time + 1)
    inputs = _heads_into(state, cell)
    remaining = f"; pulse can travel {cell.signal_steps} more link(s)" if cell.phase == "head" else ""
    pulse_names = {"wire": "idle", "head": "arriving", "tail": "fading"}
    status = f"Pulse {pulse_names[cell.phase]} -> {pulse_names[next_state]}; {inputs} live input(s); {len(_neighbors(state, cell))} links{remaining}."
    sources = f"Network: {len(seen)} fittings; {len(racks)} rack(s), {sum(r.charge > 0 for r in racks)} charged."
    if cell.kind == "rack":
        setting = f"Cell charge {cell.charge}/{2 * CELL_CHARGE}; one pulse every {PULSE_INTERVAL} actions."
    elif cell.kind == "switch":
        setting = f"Knife switch {'CLOSED' if cell.enabled else 'OPEN'}; E changes it."
    elif cell.kind == "sensor":
        setting = f"Sensor {cell.mode} threshold {cell.threshold}: {'DETECTED' if sensor_active(state, cell) else 'clear'}; E mode, T threshold."
    elif cell.kind == "relay":
        setting = f"One-way relay faces {cell.facing}; input behind, output ahead; E rotates."
    elif cell.kind == "counter":
        setting = f"Counted relay {cell.count}/{cell.threshold}; next count {next_count}; E sets 2-4, T resets."
    elif cell.kind == "piston":
        setting = f"Piston faces {cell.facing}; {'sticky' if cell.sticky else 'push-only'}; E rotates, T changes grip."
    else:
        setting = f"{'ACTIVE' if active(state, cell) else 'idle'} until action {cell.active_until}." if cell.kind in {"lamp", "gate", "drain"} else "Passive conductor."
    charged = [rack for rack in racks if rack.charge > 0]
    if cell.kind == "rack":
        if not cell.charge:
            route = "Source empty: fit a galvanic cell with E."
        elif not _has_receiver(state, cell):
            receivers = _receivers(state, cell.space)
            open_path = _route(state, cell, receivers) if receivers else None
            physical = _route(state, cell, receivers, open_only=False) if receivers else None
            if open_path is not None:
                route = f"Source waiting: device is {len(open_path) - 1} links away; limit {SIGNAL_SPAN}."
            elif physical is None:
                route = "Source waiting: no attached device; charge is conserved."
            else:
                route = f"Source waiting: {_blocked_link(state, physical)}"
        else:
            route = f"Source ready: next pulse in {(-state.world_time) % PULSE_INTERVAL or PULSE_INTERVAL} action(s)."
    elif cell.kind == "switch" and not cell.enabled:
        route = "Path blocked here: open knife switch; E closes it."
    elif cell.kind == "sensor" and not sensor_active(state, cell):
        route = f"Path blocked here: {cell.mode} below threshold {cell.threshold}."
    elif not charged:
        in_flight = any(part.phase == "head" and part.signal_steps > 0 for part in network)
        if active(state, cell):
            route = "Device remains active briefly; no charged rack for its next pulse."
        else:
            route = "Source depleted: a final pulse is still travelling." if in_flight else "No charged rack in this network."
    else:
        target = {cell_key(cell.space, cell.position, cell.layer)}
        open_paths = [path for rack in charged if (path := _route(state, rack, target))]
        if open_paths:
            distance = min(len(path) - 1 for path in open_paths)
            if distance > SIGNAL_SPAN:
                route = f"Open route is {distance} links; one pulse reaches {SIGNAL_SPAN}. Add a rack."
            else:
                route = "Source route open; waiting for a pulse." if not active(state, cell) else "Powered device is active."
        else:
            physical = [path for rack in charged if (path := _route(state, rack, target, open_only=False))]
            route = _blocked_link(state, min(physical, key=len)) if physical else "No physical route to a charged rack."
    last = f"Last pulse: {cell.last_pulse or 'never'}. Last event: {cell.last_event or 'none'}."
    return [status, sources, setting, route, last]


def _terrain_at(state: GameState, space: str, point: Position) -> str:
    if space == "vessel":
        from .vessel import VESSEL_LEVELS

        rows = VESSEL_LEVELS.get(point.z, ())
        changed = state.vessel_tiles.get(f"{point.x},{point.y},{point.z}")
    else:
        region = state.regions.get(space.split(":", 1)[1])
        rows = region.levels.get(str(point.z), ()) if region else ()
        changed = region.tile_changes.get(f"{point.x},{point.y},{point.z}") if region else None
    if not 0 <= point.y < len(rows) or not 0 <= point.x < len(rows[point.y]):
        return " "
    return changed if changed is not None else rows[point.y][point.x]


def _actor_blocks(state: GameState, space: str, point: Position) -> bool:
    if space == space_id(state) and state.position == point:
        return True
    if any(threat.status in {"watching", "engaged"} and threat.position == point
           for threat in _threats_in_space(state, space)):
        return True
    return any(schedule.position == point and (
        schedule.area == space or space == "vessel" and schedule.area.startswith("vessel:"))
        for schedule in state.actor_schedules.values())


def _crate_at(state: GameState, space: str, point: Position) -> CircuitCell | None:
    cell = state.circuits.get(cell_key(space, point, "surface"))
    return cell if cell and cell.kind == "crate" else None


def _crate_cargo(state: GameState, crate: CircuitCell):
    spatial = crate.space.split(":", 1)[1] if crate.space.startswith("region:") else "jomon"
    return [item for item in state.items if item.location == "ground" and item.region_id == spatial
            and item.ground_position == crate.position]


def _crate_weight(state: GameState, crate: CircuitCell) -> int:
    from .inventory import item_spec

    return sum(item_spec(item.kind).weight * item.quantity for item in _crate_cargo(state, crate))


def _clear_for_piston(state: GameState, space: str, point: Position) -> tuple[bool, str]:
    tile = _terrain_at(state, space, point)
    blocked = {" ", "#", "~", "T", "+"} | ({"=", "t", "F", "f", "a", "v", "B"} if space == "vessel" else set())
    if tile in blocked:
        return False, "solid terrain"
    if state.circuits.get(cell_key(space, point, "surface")) is not None:
        return False, "another fitting"
    if _actor_blocks(state, space, point):
        return False, "a person or creature"
    if space == space_id(state) and piston_head_at(state, point):
        return False, "another extended piston"
    return True, "clear"


def _move_crate(state: GameState, crate: CircuitCell, target: Position) -> None:
    cargo = _crate_cargo(state, crate)
    del state.circuits[cell_key(crate.space, crate.position, "surface")]
    crate.position = target
    state.circuits[cell_key(crate.space, target, "surface")] = crate
    for item in cargo:
        item.ground_position = target


def _extend_piston(state: GameState, piston: CircuitCell) -> str:
    front = offset(piston.position, piston.facing)
    crates: list[CircuitCell] = []
    target = front
    while (crate := _crate_at(state, piston.space, target)) is not None:
        crates.append(crate)
        if len(crates) > 3:
            return "jammed: more than three freight crates"
        if _crate_weight(state, crate) > CRATE_LOAD_LIMIT:
            return f"jammed: crate exceeds {CRATE_LOAD_LIMIT} kg"
        target = offset(target, piston.facing)
    clear, reason = _clear_for_piston(state, piston.space, target)
    if not clear:
        return f"jammed: {reason} ahead"
    for crate in reversed(crates):
        _move_crate(state, crate, offset(crate.position, piston.facing))
    piston.active_until = state.world_time + DEVICE_HOLD
    return f"extended {piston.facing}; pushed {len(crates)} crate{'s' if len(crates) != 1 else ''}"


def _retract_piston(state: GameState, piston: CircuitCell) -> str:
    if not piston.sticky:
        return "retracted"
    front = offset(piston.position, piston.facing)
    crate = _crate_at(state, piston.space, offset(front, piston.facing))
    if crate is None:
        return "retracted; nothing to pull"
    if _crate_weight(state, crate) > CRATE_LOAD_LIMIT:
        return f"retracted; crate exceeds {CRATE_LOAD_LIMIT} kg"
    clear, reason = _clear_for_piston(state, piston.space, front)
    if not clear:
        return f"retracted; pull blocked by {reason}"
    _move_crate(state, crate, front)
    return "retracted; pulled one crate"


def _drain_water(state: GameState, cell: CircuitCell) -> int:
    removed = 0
    fields = (state.regions[cell.space.split(":", 1)[1]].materials
              if cell.space.startswith("region:") else state.vessel_materials)
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            point = Position(cell.position.x + dx, cell.position.y + dy, cell.position.z)
            key = f"{point.x},{point.y},{point.z}"
            if cell.space == space_id(state) and key in state.water:
                removed += 1
                del state.water[key]
            material = fields.get(key)
            if material and material.water:
                removed += min(2, material.water)
                material.water = max(0, material.water - 2)
    return removed


def advance_circuits(state: GameState) -> None:
    """Simultaneous finite pulses; an idle rack conserves its cell charge."""
    if not state.circuits:
        return
    transitions = {key: _next_transition(state, cell) for key, cell in state.circuits.items() if cell.kind != "crate"}
    for key, (phase, count, steps) in transitions.items():
        cell = state.circuits[key]
        old_phase = cell.phase
        retract_due = cell.kind == "piston" and cell.active_until == state.world_time - 1 and phase != "head"
        cell.phase = phase
        cell.count = count
        cell.signal_steps = steps
        if cell.kind == "rack" and phase == "head":
            cell.charge -= 1
            if cell.charge == 0:
                cell.last_event = "cell depleted; load another galvanic cell"
        if phase == "head" and old_phase != "head":
            cell.last_pulse = state.world_time
            if cell.kind == "piston":
                if active(state, cell):
                    cell.active_until = state.world_time + DEVICE_HOLD
                else:
                    cell.last_event = _extend_piston(state, cell)
            elif cell.kind == "drain":
                cell.active_until = state.world_time + DEVICE_HOLD
                cell.last_event = f"drained {_drain_water(state, cell)} water measures"
            elif cell.kind in {"lamp", "gate"}:
                cell.active_until = state.world_time + DEVICE_HOLD
                cell.last_event = "powered"
            elif cell.kind == "counter":
                cell.last_event = f"passed its {cell.threshold}th input pulse"
        elif retract_due:
            cell.last_event = _retract_piston(state, cell)


def validate_circuits(state: GameState) -> None:
    if not isinstance(state.circuits, dict) or len(state.circuits) > MAX_CELLS:
        raise ValueError("invalid circuit register")
    for key, cell in state.circuits.items():
        if not isinstance(cell, CircuitCell) or not isinstance(cell.position, Position):
            raise ValueError("invalid circuit cell")
        if (not isinstance(cell.space, str) or any(type(axis) is not int for axis in (cell.position.x, cell.position.y, cell.position.z))
                or any(not isinstance(value, str) for value in (cell.layer, cell.kind, cell.phase))):
            raise ValueError("invalid circuit coordinate")
        if cell.space != "vessel" and not (cell.space.startswith("region:") and cell.space[7:] in state.regions):
            raise ValueError("invalid circuit space")
        if key != cell_key(cell.space, cell.position, cell.layer):
            raise ValueError("mismatched circuit key")
        if cell.layer not in {"surface", "buried"} or cell.kind not in PLACED_PARTS or cell.phase not in {"wire", "head", "tail"}:
            raise ValueError("invalid circuit part or phase")
        if cell.layer not in PARTS[cell.kind]["layers"]:
            raise ValueError("circuit part on unsupported layer")
        if (type(cell.enabled) is not bool or type(cell.charge) is not int or not 0 <= cell.charge <= 2 * CELL_CHARGE
                or type(cell.active_until) is not int or cell.active_until < 0):
            raise ValueError("invalid circuit control state")
        if (not isinstance(cell.facing, str) or cell.facing not in DIRECTIONS
                or not isinstance(cell.mode, str) or cell.mode not in SENSOR_MODES
                or type(cell.threshold) is not int or not 1 <= cell.threshold <= 4
                or type(cell.count) is not int or not 0 <= cell.count < cell.threshold
                or type(cell.sticky) is not bool or type(cell.last_pulse) is not int
                or not 0 <= cell.last_pulse <= state.world_time
                or type(cell.signal_steps) is not int or not 0 <= cell.signal_steps <= SIGNAL_SPAN
                or cell.phase != "head" and cell.signal_steps != 0
                or not isinstance(cell.last_event, str) or len(cell.last_event) > 160):
            raise ValueError("invalid circuit setting or diagnostic")
        if cell.kind == "crate" and cell.phase != "wire":
            raise ValueError("movable crate cannot carry an electrical pulse")
        region = state.regions.get(cell.space[7:]) if cell.space.startswith("region:") else None
        if region and (str(cell.position.z) not in region.levels or not 0 <= cell.position.x < region.width
                       or not 0 <= cell.position.y < region.height):
            raise ValueError("circuit outside regional bounds")
        if cell.space == "vessel":
            from .vessel import VESSEL_LEVELS

            rows = VESSEL_LEVELS.get(cell.position.z)
            if rows is None or not 0 <= cell.position.y < len(rows) or not 0 <= cell.position.x < len(rows[cell.position.y]):
                raise ValueError("circuit outside vessel bounds")
