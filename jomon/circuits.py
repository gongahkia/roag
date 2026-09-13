"""Sparse, buildable electrical traces and finite pulse-driven work devices."""

from __future__ import annotations

from .catalog import CatalogError, load_catalog
from .state import CircuitCell, GameState, Position


PARTS = load_catalog("circuits.json", ("parts",))["parts"]
if not isinstance(PARTS, dict) or set(PARTS) != {"trace", "via", "rack", "cell", "switch", "lamp", "gate", "drain"}:
    raise CatalogError("circuits.json has invalid circuit parts")
PLACED_PARTS = set(PARTS) - {"cell"}
DEVICE_PARTS = {"rack", "switch", "lamp", "gate", "drain"}
PULSE_INTERVAL = 6
CELL_CHARGE = 24
DEVICE_HOLD = 5
MAX_CELLS = 4096


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


def active(state: GameState, cell: CircuitCell) -> bool:
    return cell.kind in {"lamp", "gate", "drain"} and 0 < cell.active_until >= state.world_time


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
    from .world import is_walkable

    reason = _target_reason(state, position, layer)
    if reason:
        return False, reason
    if kind not in PLACED_PARTS:
        return False, "Choose a buildable circuit part."
    if len(state.circuits) >= MAX_CELLS:
        return False, "The world circuit register is full."
    if layer == "buried" and kind in DEVICE_PARTS:
        return False, "Working devices need a reachable surface fitting."
    space = space_id(state)
    key = cell_key(space, position, layer)
    if key in state.circuits:
        return False, "That circuit layer is already occupied; reclaim it first."
    if layer == "surface" and not is_walkable(state, position, ignore_threat=True):
        return False, "A surface fitting needs passable ground; bury a trace beneath a wall."
    if kind == "gate" and position == state.position:
        return False, "Stand clear of the square before fitting a closed gate."
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
        return False, "Clear a pack cell before reclaiming the fitting."
    del state.circuits[key]
    sync_legacy_load(state)
    _advance_world(state)
    message = f"You reclaim the {PARTS[cell.kind]['name'].lower()} from the {layer} layer."
    state.add_message(message, priority=3)
    return True, message


def operate(state: GameState, position: Position, layer: str) -> tuple[bool, str]:
    from .actions import _advance_world

    reason = _target_reason(state, position, layer)
    if reason:
        return False, reason
    cell = cell_at(state, position, layer)
    if cell is None:
        return False, "No circuit part occupies that layer."
    if cell.kind == "switch":
        cell.enabled = not cell.enabled
        if not cell.enabled:
            cell.phase = "wire"
        message = f"You {'close' if cell.enabled else 'open'} the knife switch."
    elif cell.kind == "rack":
        if cell.charge > CELL_CHARGE:
            return False, "The rack already holds more than one cell's remaining charge."
        if _available_item(state, "cell") is None:
            return False, "A crafted galvanic cell is needed to charge the rack."
        _spend_item(state, "cell")
        cell.charge += CELL_CHARGE
        message = f"You fit a galvanic cell; the rack holds {cell.charge} pulses."
    else:
        return False, "Operate a knife switch or load a galvanic rack here."
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
            if other is not None:
                found.append(other)
    other_layer = "buried" if layer == "surface" else "surface"
    other = state.circuits.get(cell_key(space, point, other_layer))
    if other is not None and (cell.kind == "via" or other.kind == "via"):
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


def advance_circuits(state: GameState) -> None:
    """Simultaneous Wireworld phases; each rack injects one finite pulse per interval."""
    if not state.circuits:
        return
    next_phases: dict[str, str] = {}
    for key, cell in state.circuits.items():
        if cell.kind == "rack":
            next_phases[key] = "head" if state.world_time % PULSE_INTERVAL == 0 and cell.charge > 0 else (
                "tail" if cell.phase == "head" else "wire")
        elif cell.kind == "switch" and not cell.enabled:
            next_phases[key] = "wire"
        elif cell.phase == "head":
            next_phases[key] = "tail"
        elif cell.phase == "tail":
            next_phases[key] = "wire"
        else:
            heads = sum(other.phase == "head" and (other.kind != "switch" or other.enabled)
                        for other in _neighbors(state, cell))
            next_phases[key] = "head" if heads in (1, 2) else "wire"
    for key, phase in next_phases.items():
        cell = state.circuits[key]
        cell.phase = phase
        if cell.kind == "rack" and phase == "head":
            cell.charge -= 1
        if phase == "head" and cell.kind in {"lamp", "gate", "drain"}:
            cell.active_until = state.world_time + DEVICE_HOLD
            if cell.kind == "drain" and cell.space == space_id(state):
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        position = Position(cell.position.x + dx, cell.position.y + dy, cell.position.z)
                        state.water.pop(f"{position.x},{position.y},{position.z}", None)


def validate_circuits(state: GameState) -> None:
    if not isinstance(state.circuits, dict) or len(state.circuits) > MAX_CELLS:
        raise ValueError("invalid circuit register")
    for key, cell in state.circuits.items():
        if not isinstance(cell, CircuitCell) or not isinstance(cell.position, Position):
            raise ValueError("invalid circuit cell")
        if cell.space != "vessel" and not (cell.space.startswith("region:") and cell.space[7:] in state.regions):
            raise ValueError("invalid circuit space")
        if key != cell_key(cell.space, cell.position, cell.layer):
            raise ValueError("mismatched circuit key")
        if cell.layer not in {"surface", "buried"} or cell.kind not in PLACED_PARTS or cell.phase not in {"wire", "head", "tail"}:
            raise ValueError("invalid circuit part or phase")
        if cell.layer == "buried" and cell.kind in DEVICE_PARTS:
            raise ValueError("buried circuit device")
        if (type(cell.enabled) is not bool or type(cell.charge) is not int or not 0 <= cell.charge <= 2 * CELL_CHARGE
                or type(cell.active_until) is not int or cell.active_until < 0):
            raise ValueError("invalid circuit control state")
        region = state.regions.get(cell.space[7:]) if cell.space.startswith("region:") else None
        if region and (str(cell.position.z) not in region.levels or not 0 <= cell.position.x < region.width
                       or not 0 <= cell.position.y < region.height):
            raise ValueError("circuit outside regional bounds")
