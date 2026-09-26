"""Finite physical preparations keyed by stable aftermath mechanics."""
from __future__ import annotations

from dataclasses import dataclass
from typing import TYPE_CHECKING

from .catalog import AFTERMATH_SECTIONS, load_catalog
from .preparation_presentation import (
    preparation_condition,
    preparation_description,
    preparation_display_name,
    preparation_format,
    preparation_text,
)
if TYPE_CHECKING:
    from .state import GameState, Position, Threat


@dataclass(frozen=True)
class Preparation:
    """Mechanical preparation definition; legacy fields only map old bundled saves."""
    id: str
    name: str
    topology: str
    mode: str
    description: str
    condition: str


PREPARATIONS = {
    row["id"]: Preparation(**row)
    for row in load_catalog("aftermath.json", AFTERMATH_SECTIONS)["preparations"]
}
LEGACY_PREPARATION_IDS = {preparation.name: preparation.id for preparation in PREPARATIONS.values()}
TOPOLOGY_PREPARATION = {
    preparation.topology: preparation.id for preparation in PREPARATIONS.values()
}


def stable_preparation_id(value: str) -> str:
    """Map only shipped historical labels; unknown values remain inert."""
    return value if value in PREPARATIONS else LEGACY_PREPARATION_IDS.get(value, value)


def preparation_item_kind(preparation_id: str) -> str:
    return f"consumable:{preparation_id}"


def _set_preparation_intent(threat: Threat, semantic_id: str) -> None:
    threat.intent_id = semantic_id
    threat.intent = preparation_text(semantic_id)


def normalize_preparation_state(state: GameState) -> bool:
    """Normalize known old item and marker suffixes without consulting pack text."""
    changed = False
    for item in state.items:
        if not item.kind.startswith("consumable:"):
            continue
        legacy = item.kind.split(":", 1)[1]
        preparation_id = LEGACY_PREPARATION_IDS.get(legacy)
        if preparation_id:
            item.kind = preparation_item_kind(preparation_id)
            changed = True
    stores = [state.vessel_changes, *(region.changes for region in state.regions.values())]
    for store in stores:
        for marker, count in list(store.items()):
            if not marker.startswith("preparation-used:"):
                continue
            legacy = marker.split(":", 1)[1]
            preparation_id = LEGACY_PREPARATION_IDS.get(legacy)
            if preparation_id:
                stable_marker = f"preparation-used:{preparation_id}"
                store[stable_marker] = int(store.get(stable_marker, 0)) + int(count)
                del store[marker]
                changed = True
    return changed


def carried_preparations(state: GameState) -> list[str]:
    return list(dict.fromkeys(
        item.kind.split(":", 1)[1]
        for item in state.items
        if item.owner_id == state.active_courier_id and item.location == "pack"
        and item.kind.startswith("consumable:")
        and item.kind.split(":", 1)[1] in PREPARATIONS
    ))


def _distance(first: Position, second: Position) -> int:
    return max(abs(first.x - second.x), abs(first.y - second.y)) + abs(first.z - second.z) * 3


def _nearby_materials(state: GameState, radius: int):
    from .materials import fields, point_at

    return sorted(
        ((point_at(coordinate), cell) for coordinate, cell in fields(state).items() if _distance(state.position, point_at(coordinate)) <= radius),
        key=lambda pair: (_distance(state.position, pair[0]), pair[0].z, pair[0].y, pair[0].x),
    )


def _nearby_points(state: GameState, radius: int) -> list[Position]:
    from .state import Position

    return sorted(
        (Position(state.position.x + dx, state.position.y + dy, state.position.z) for dy in range(-radius, radius + 1) for dx in range(-radius, radius + 1) if max(abs(dx), abs(dy)) <= radius),
        key=lambda point: (_distance(state.position, point), point.y, point.x),
    )


def preparation_status(state: GameState, value: str) -> tuple[bool, str]:
    """Return a mechanical readiness reason ID, never display wording."""
    preparation_id = stable_preparation_id(value)
    if preparation_id not in PREPARATIONS or preparation_id not in carried_preparations(state):
        return False, "preparation.status.not_carried"
    preparation = PREPARATIONS[preparation_id]
    mode = preparation.mode
    from .materials import FLAMMABLE, fields, key, material_at
    from .state import Position

    nearby = _nearby_materials(state, 4)
    if mode == "waterline":
        ok = any(cell.water for _, cell in nearby) or any(_distance(state.position, Position(*map(int, coordinate.split(",")))) <= 4 for coordinate in state.water)
    elif mode == "weapon-repair":
        ok = any(item.owner_id == state.active_courier_id and item.location == "readied" and item.condition < 100 for item in state.items)
    elif mode == "storm-light":
        ok = state.lamp_oil < 8 or any(_distance(state.position, Position(*map(int, coordinate.split(",")))) <= 2 for coordinate in state.smoke) or any(cell.smoke and _distance(state.position, point) <= 2 for point, cell in nearby)
    elif mode == "item-recovery":
        ok = any(item.location == "ground" and item.region_id == state.spatial_id and item.ground_position is not None and _distance(state.position, item.ground_position) <= 4 for item in state.items)
    elif mode == "fire-blanket":
        ok = any(cell.fire for point, cell in nearby if _distance(state.position, point) <= 2) or any(actor.conditions.get("burning", 0) and _distance(state.position, actor.position) <= 2 for actor in state.combatants)
    elif mode == "firebrand":
        ok = any(material_at(state, point) in FLAMMABLE and not ((cell := fields(state).get(key(point))) and (cell.water or cell.fire)) for point in _nearby_points(state, 1))
    elif mode == "high-sounding":
        ok = state.location == "region" and state.position.z > 0 and any(not container.opened and container.id not in state.treasure_marks[state.active_region_id] for container in state.region.containers)
    elif mode == "footing":
        ok = bool({"poor-footing", "cut-feet"} & set(state.terrain_statuses))
    elif mode == "bank-plug":
        ok = any(cell.water and cell.material in {"soil", "timber"} for _, cell in _nearby_materials(state, 1))
    elif mode == "ration":
        ok = bool({"fatigued", "chilled", "coalheart-chill", "smoke-inhalation"} & set(state.terrain_statuses)) or bool(state.courier and state.courier.health < state.courier.max_health)
    elif mode == "bridge-dogs":
        ok = any(cell.material in {"timber", "stone"} and (cell.support < 3 or cell.collapse_due) for _, cell in nearby)
    elif mode == "aim-break":
        ok = state.noise > 0 or any(actor.aimed_at is not None and _distance(state.position, actor.position) <= 10 for actor in state.combatants)
    elif mode == "drain-tile":
        from .world import base_tile
        ok = any(base_tile(state, point) == "m" for point in _nearby_points(state, 1))
    elif mode == "kiln-sand":
        ok = any(cell.fire for _, cell in _nearby_materials(state, 1))
    elif mode == "ice-peg":
        from .calendar import calendar_at
        ok = calendar_at(state).season == "winter" and any(cell.water and cell.fluid == "fresh" and not cell.fire for _, cell in _nearby_materials(state, 1))
    else:
        heated = state.lamp_oil > 0 or any(cell.fire for _, cell in _nearby_materials(state, 2))
        ok = heated and any(cell.ice for _, cell in _nearby_materials(state, 2))
    return ok, "ready" if ok else f"{preparation_id}.condition"


def preparation_status_text(reason_id: str) -> str:
    return "ready" if reason_id == "ready" else preparation_text(reason_id)


def apply_preparation(state: GameState, value: str) -> tuple[bool, str]:
    from .state import Position

    preparation_id = stable_preparation_id(value)
    if preparation_id not in PREPARATIONS:
        return False, preparation_format("preparation.apply.unavailable", preparation=value, condition=preparation_text("preparation.status.not_carried"))
    available, reason_id = preparation_status(state, preparation_id)
    preparation_name = preparation_display_name(preparation_id)
    if not available:
        return False, preparation_format("preparation.apply.unavailable", preparation=preparation_name, condition=preparation_status_text(reason_id))
    from .inventory import auto_place, consume_carried, record_acquisition
    from .materials import FLAMMABLE, ensure_cell, fields, key, material_at

    mode = PREPARATIONS[preparation_id].mode
    detail = ""
    if mode == "waterline":
        changed = 0
        for point, cell in _nearby_materials(state, 4):
            if cell.water and changed < 3:
                cell.water -= 1
                changed += 1
        for coordinate in sorted(list(state.water)):
            point = Position(*map(int, coordinate.split(",")))
            if _distance(state.position, point) <= 4 and changed < 3:
                del state.water[coordinate]
                changed += 1
        detail = preparation_format("preparation.result.waterline", changed=changed)
    elif mode == "weapon-repair":
        weapon = next(item for item in state.items if item.owner_id == state.active_courier_id and item.location == "readied" and item.condition < 100)
        before = weapon.condition
        weapon.condition = min(100, weapon.condition + 30)
        detail = preparation_format("preparation.result.weapon-repair", weapon=weapon.kind, before=before, condition=weapon.condition)
    elif mode == "storm-light":
        before = state.lamp_oil
        state.lamp_oil = min(8, state.lamp_oil + 2)
        cleared = 0
        for coordinate in sorted(list(state.smoke)):
            point = Position(*map(int, coordinate.split(",")))
            if _distance(state.position, point) <= 2 and cleared < 2:
                del state.smoke[coordinate]
                cleared += 1
        for point, cell in _nearby_materials(state, 2):
            if cell.smoke and cleared < 2:
                cell.smoke = 0
                cleared += 1
        detail = preparation_format("preparation.result.storm-light", before=before, oil=state.lamp_oil, cleared=cleared)
    elif mode == "item-recovery":
        item = min((item for item in state.items if item.location == "ground" and item.region_id == state.spatial_id and item.ground_position is not None and _distance(state.position, item.ground_position) <= 4), key=lambda item: (_distance(state.position, item.ground_position), item.id))
        if not auto_place(state, item.id, "pack", owner_id=state.active_courier_id):
            return False, preparation_format("preparation.apply.unavailable", preparation=preparation_name, condition=preparation_text("preparation.item-recovery.pack_full"))
        record_acquisition(state, item)
        detail = preparation_format("preparation.result.item-recovery", item=item.kind)
    elif mode == "fire-blanket":
        changed = 0
        for point, cell in _nearby_materials(state, 2):
            if cell.fire and changed < 4:
                cell.fire = 0
                cell.coating = "wet"
                changed += 1
        for actor in state.combatants:
            if _distance(state.position, actor.position) <= 2 and actor.conditions.pop("burning", None):
                changed += 1
        detail = preparation_format("preparation.result.fire-blanket", changed=changed)
    elif mode == "firebrand":
        point = next(point for point in _nearby_points(state, 1) if material_at(state, point) in FLAMMABLE and not ((cell := fields(state).get(key(point))) and (cell.water or cell.fire)))
        cell = ensure_cell(state, point)
        cell.fire, cell.fuel, cell.coating = 2, max(6, cell.fuel), "resin"
        state.noise += 2
        for actor in state.combatants:
            if actor.status == "watching" and _distance(state.position, actor.position) <= 9:
                actor.status, actor.last_known_position = "engaged", state.position
                _set_preparation_intent(actor, "intent.preparation.firebrand")
        detail = preparation_format("preparation.result.firebrand", material=cell.material, coordinate=key(point))
    elif mode == "high-sounding":
        from .quests import mark_treasure
        cache = min((container for container in state.region.containers if not container.opened and container.id not in state.treasure_marks[state.active_region_id]), key=lambda container: (_distance(state.position, container.position), container.id))
        mark_treasure(state, state.active_region_id, cache.id, preparation_format("preparation.record.high-sounding", cache=cache.name, x=cache.position.x, y=cache.position.y, level=f"{cache.position.z:+d}"))
        from .actions import emit_sound
        emit_sound(state, 3)
        detail = preparation_format("preparation.result.high-sounding", cache=cache.name)
    elif mode == "footing":
        cleared = sorted({"poor-footing", "cut-feet"} & set(state.terrain_statuses))
        for status in cleared:
            state.terrain_statuses.pop(status, None)
        state.guarded_step = True
        detail = preparation_format("preparation.result.footing", statuses=", ".join(cleared))
    elif mode == "bank-plug":
        point, cell = next((point, cell) for point, cell in _nearby_materials(state, 1) if cell.water and cell.material in {"soil", "timber"})
        before = (cell.water, cell.support)
        cell.water -= 1
        cell.support = min(3, cell.support + 1)
        cell.collapse_due = 0
        detail = preparation_format("preparation.result.bank-plug", material=cell.material, coordinate=key(point), before=before, after=(cell.water, cell.support))
    elif mode == "ration":
        priorities = ("fatigued", "coalheart-chill", "chilled", "smoke-inhalation")
        cleared = next((status for status in priorities if status in state.terrain_statuses), "no status")
        state.terrain_statuses.pop(cleared, None)
        before = state.courier.health
        state.courier.health = min(state.courier.max_health, before + 1)
        detail = preparation_format("preparation.result.ration", status=cleared, before=before, health=state.courier.health)
    elif mode == "bridge-dogs":
        supports = [(point, cell) for point, cell in _nearby_materials(state, 4) if cell.material in {"timber", "stone"} and (cell.support < 3 or cell.collapse_due)][:2]
        for _, cell in supports:
            cell.support, cell.collapse_due = 3, 0
        from .actions import emit_sound
        emit_sound(state, 4)
        detail = preparation_format("preparation.result.bridge-dogs", supports=len(supports))
    elif mode == "aim-break":
        targets = [actor for actor in state.combatants if actor.aimed_at is not None and _distance(state.position, actor.position) <= 10][:3]
        for actor in targets:
            actor.aimed_at = None
            _set_preparation_intent(actor, "intent.preparation.aim-break")
        before = state.noise
        state.noise = max(0, state.noise - 3)
        state.sound_events = [event for event in state.sound_events if _distance(state.position, event.position) > 4]
        detail = preparation_format("preparation.result.aim-break", targets=len(targets), before=before, noise=state.noise)
    elif mode == "drain-tile":
        from .world import base_tile
        point = next(point for point in _nearby_points(state, 1) if base_tile(state, point) == "m")
        state.region.tile_changes[key(point)] = "."
        cell = ensure_cell(state, point)
        cell.water, cell.coating = 0, "ash"
        detail = preparation_format("preparation.result.drain-tile", coordinate=key(point))
    elif mode == "kiln-sand":
        point, cell = next((point, cell) for point, cell in _nearby_materials(state, 1) if cell.fire)
        before = cell.fuel
        cell.fire, cell.fuel = 0, max(0, cell.fuel - 3)
        cell.smoke, cell.coating = min(4, cell.smoke + 2), "ash"
        detail = preparation_format("preparation.result.kiln-sand", coordinate=key(point), before=before, fuel=cell.fuel)
    elif mode == "ice-peg":
        point, cell = next((point, cell) for point, cell in _nearby_materials(state, 1) if cell.water and cell.fluid == "fresh" and not cell.fire)
        cell.water, cell.ice = 1, True
        state.water.pop(key(point), None)
        from .actions import emit_sound
        emit_sound(state, 1)
        detail = preparation_format("preparation.result.ice-peg", coordinate=key(point))
    else:
        ice = [(point, cell) for point, cell in _nearby_materials(state, 2) if cell.ice][:3]
        used_lamp = not any(cell.fire for _, cell in _nearby_materials(state, 2))
        if used_lamp:
            state.lamp_oil -= 1
        for _, cell in ice:
            cell.ice = False
            cell.water = max(1, cell.water)
            cell.coating = "wet"
        state.terrain_statuses.pop("chilled", None)
        state.terrain_statuses.pop("coalheart-chill", None)
        detail = preparation_format("preparation.result.thaw", cells=len(ice), heat=preparation_text("preparation.result.thaw.lamp" if used_lamp else "preparation.result.thaw.fire"))
    if not consume_carried(state, preparation_item_kind(preparation_id)):
        raise RuntimeError("validated physical preparation disappeared before consumption")
    changes = state.region.changes if state.location == "region" else state.vessel_changes
    marker = f"preparation-used:{preparation_id}"
    changes[marker] = int(changes.get(marker, 0)) + 1
    state.remember(preparation_format("preparation.apply.memory", courier=state.courier.name, preparation=preparation_name, detail=detail))
    return True, preparation_format("preparation.apply.success", preparation=preparation_name, detail=detail)


def grant_contract_preparation(state: GameState, topology: str) -> str:
    from .inventory import auto_place, create_item, record_acquisition

    preparation_id = TOPOLOGY_PREPARATION[topology]
    item = create_item(state, preparation_item_kind(preparation_id), preparation_format("preparation.grant.provenance", topology=topology))
    if auto_place(state, item.id, "pack", owner_id=state.active_courier_id):
        record_acquisition(state, item)
        where = preparation_text("preparation.grant.packed")
    else:
        item.location, item.region_id, item.ground_position = "ground", state.spatial_id, state.position
        where = preparation_text("preparation.grant.ground")
    preparation_name = preparation_display_name(preparation_id)
    state.remember(preparation_format("preparation.grant.memory", courier=state.courier.name, preparation=preparation_name, topology=topology, where=where))
    return preparation_format("preparation.grant.result", preparation=preparation_name, where=where)


def validate_preparations() -> None:
    if len(PREPARATIONS) != 16 or len(TOPOLOGY_PREPARATION) != 16:
        raise ValueError("sixteen aftermath topologies need distinct finite preparations")
    from .aftermath import AFTERMATH_TOPOLOGIES
    topologies = {topology for pair in AFTERMATH_TOPOLOGIES.values() for topology in pair}
    if set(TOPOLOGY_PREPARATION) != topologies:
        raise ValueError("preparation production does not cover every aftermath topology")
    if len({preparation.mode for preparation in PREPARATIONS.values()}) != 16:
        raise ValueError("preparations need sixteen distinct production reducers")
    if any(preparation.id != f"preparation.{preparation.mode}" for preparation in PREPARATIONS.values()):
        raise ValueError("preparation ids must remain stable mode identities")
