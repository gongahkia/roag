"""Finite, physical preparations learned from the sixteen aftermath works."""

from __future__ import annotations

from dataclasses import dataclass
from typing import TYPE_CHECKING

if TYPE_CHECKING:
    from .state import GameState, Position


@dataclass(frozen=True)
class Preparation:
    name: str
    topology: str
    mode: str
    description: str
    condition: str


PREPARATIONS = {
    preparation.name: preparation
    for preparation in (
        Preparation("race-gate chalk", "flood-mark circuit", "waterline", "Lowers up to three nearby shallow-water layers and records the worked waterline.", "nearby released or material water"),
        Preparation("tallow gear wrap", "wheel-timber account", "weapon-repair", "Restores 30 condition to the physical readied weapon; the greasy wrap is spent.", "a damaged readied weapon"),
        Preparation("storm wick", "storm-beacon line", "storm-light", "Restores two lamp measures and clears two nearby smoke fields under a sheltered hood.", "spent lamp oil or nearby smoke"),
        Preparation("wreck cork sling", "shifted-wreck recovery", "item-recovery", "Floats one nearby loose physical item directly into the pack when space permits.", "a ground item within four paces and pack room"),
        Preparation("damp ember blanket", "medicine boundary", "fire-blanket", "Smothers up to four nearby fires and burning hostile conditions without creating new water.", "nearby fire or a burning hostile"),
        Preparation("resin firebrand", "charcoal cut audit", "firebrand", "Ignites one dry adjacent fuel without lamp oil; the flare makes the courier's position known.", "dry adjacent reeds, timber, cloth, resin, oil, or charcoal"),
        Preparation("stair sounding cord", "warning stair", "high-sounding", "From elevation, marks the nearest unread cache while its sounding alerts nearby listeners.", "an elevated regional position and unread cache"),
        Preparation("scree binding", "toll-brace recovery", "footing", "Clears poor footing or cut feet and preserves one guarded reposition.", "poor footing or cut-feet status"),
        Preparation("peat bank plug", "raised peat walk", "bank-plug", "Lowers nearby water and seats one support in the same worked soil or timber cell.", "nearby wet soil or timber"),
        Preparation("fen ration cake", "submerged fuel mark", "ration", "Clears one fatigue, chill, or smoke-inhalation pressure and restores one health.", "fatigue, chill, smoke inhalation, or lost health"),
        Preparation("bridge dog pair", "switchback brace", "bridge-dogs", "Seats up to two nearby damaged supports; the hammering reports to listeners.", "nearby damaged timber or stone support"),
        Preparation("echo muffler", "convoy counterweight", "aim-break", "Breaks up to three visible prepared hostile lanes and dampens accumulated noise.", "a nearby prepared hostile lane or accumulated noise"),
        Preparation("fired drainage tile", "seed-bed drainage", "drain-tile", "Turns one adjacent mud cell into a dry, ash-marked drainage footing.", "adjacent mud"),
        Preparation("kiln sand pouch", "abandoned kiln quench", "kiln-sand", "Quenches one adjacent fire, strips three fuel, and leaves concealing ash smoke.", "an adjacent material fire"),
        Preparation("marked ice peg", "sheltered channel stakes", "ice-peg", "In winter, freezes one adjacent fresh shallow into marked footing and rings once.", "winter and adjacent unfired fresh water"),
        Preparation("thaw kettle sachet", "broken-ice net recovery", "thaw", "Spends one lamp measure or nearby fire to thaw three ice cells and clear chill.", "nearby ice plus sheltered heat"),
    )
}

TOPOLOGY_PREPARATION = {
    preparation.topology: preparation.name for preparation in PREPARATIONS.values()
}


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
        (
            (point_at(coordinate), cell)
            for coordinate, cell in fields(state).items()
            if _distance(state.position, point_at(coordinate)) <= radius
        ),
        key=lambda pair: (_distance(state.position, pair[0]), pair[0].z, pair[0].y, pair[0].x),
    )


def _nearby_points(state: GameState, radius: int) -> list[Position]:
    from .state import Position

    return sorted(
        (
            Position(state.position.x + dx, state.position.y + dy, state.position.z)
            for dy in range(-radius, radius + 1)
            for dx in range(-radius, radius + 1)
            if max(abs(dx), abs(dy)) <= radius
        ),
        key=lambda point: (_distance(state.position, point), point.y, point.x),
    )


def preparation_status(state: GameState, name: str) -> tuple[bool, str]:
    from .state import Position

    if name not in carried_preparations(state):
        return False, "the physical preparation is not in this courier's pack"
    preparation = PREPARATIONS[name]
    mode = preparation.mode
    from .materials import FLAMMABLE, fields, key, material_at

    nearby = _nearby_materials(state, 4)
    if mode == "waterline":
        ok = any(cell.water for _, cell in nearby) or any(
            _distance(state.position, Position(*map(int, coordinate.split(",")))) <= 4
            for coordinate in state.water
        )
    elif mode == "weapon-repair":
        ok = any(
            item.owner_id == state.active_courier_id and item.location == "readied"
            and item.condition < 100 for item in state.items
        )
    elif mode == "storm-light":
        ok = state.lamp_oil < 8 or any(
            _distance(state.position, Position(*map(int, coordinate.split(",")))) <= 2
            for coordinate in state.smoke
        ) or any(cell.smoke and _distance(state.position, point) <= 2 for point, cell in nearby)
    elif mode == "item-recovery":
        ok = any(
            item.location == "ground" and item.region_id == state.spatial_id
            and item.ground_position is not None
            and _distance(state.position, item.ground_position) <= 4
            for item in state.items
        )
    elif mode == "fire-blanket":
        ok = any(cell.fire for _, cell in nearby if _distance(state.position, _) <= 2) or any(
            actor.conditions.get("burning", 0) and _distance(state.position, actor.position) <= 2
            for actor in state.combatants
        )
    elif mode == "firebrand":
        ok = any(
            material_at(state, point) in FLAMMABLE
            and not ((cell := fields(state).get(key(point))) and (cell.water or cell.fire))
            for point in _nearby_points(state, 1)
        )
    elif mode == "high-sounding":
        ok = state.location == "region" and state.position.z > 0 and any(
            not container.opened and container.id not in state.treasure_marks[state.active_region_id]
            for container in state.region.containers
        )
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

        ok = calendar_at(state).season == "winter" and any(
            cell.water and cell.fluid == "fresh" and not cell.fire
            for _, cell in _nearby_materials(state, 1)
        )
    else:
        heated = state.lamp_oil > 0 or any(cell.fire for _, cell in _nearby_materials(state, 2))
        ok = heated and any(cell.ice for _, cell in _nearby_materials(state, 2))
    return ok, "ready" if ok else preparation.condition


def apply_preparation(state: GameState, name: str) -> tuple[bool, str]:
    from .state import Position

    available, reason = preparation_status(state, name)
    if not available:
        return False, f"{name.title()} remains packed: needs {reason}."
    from .inventory import auto_place, consume_carried, record_acquisition
    from .materials import FLAMMABLE, ensure_cell, fields, key, material_at

    mode = PREPARATIONS[name].mode
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
        detail = f"lowers {changed} nearby water layers and records their line"
    elif mode == "weapon-repair":
        weapon = next(item for item in state.items if item.owner_id == state.active_courier_id and item.location == "readied" and item.condition < 100)
        before = weapon.condition
        weapon.condition = min(100, weapon.condition + 30)
        detail = f"restores the readied {weapon.kind} from {before} to {weapon.condition} condition"
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
        detail = f"restores lamp oil {before}→{state.lamp_oil} and clears {cleared} smoke fields"
    elif mode == "item-recovery":
        item = min(
            (
                item for item in state.items
                if item.location == "ground" and item.region_id == state.spatial_id
                and item.ground_position is not None and _distance(state.position, item.ground_position) <= 4
            ),
            key=lambda item: (_distance(state.position, item.ground_position), item.id),
        )
        if not auto_place(state, item.id, "pack", owner_id=state.active_courier_id):
            return False, "Wreck Cork Sling remains packed: the recovered item does not fit the pack."
        record_acquisition(state, item)
        detail = f"recovers physical {item.kind} into the pack"
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
        detail = f"smothers {changed} nearby fires or burning bodies"
    elif mode == "firebrand":
        point = next(
            point for point in _nearby_points(state, 1)
            if material_at(state, point) in FLAMMABLE
            and not ((cell := fields(state).get(key(point))) and (cell.water or cell.fire))
        )
        cell = ensure_cell(state, point)
        cell.fire, cell.fuel, cell.coating = 2, max(6, cell.fuel), "resin"
        state.noise += 2
        for actor in state.combatants:
            if actor.status == "watching" and _distance(state.position, actor.position) <= 9:
                actor.status, actor.last_known_position = "engaged", state.position
                actor.intent = "tracks the resin flare"
        detail = f"ignites dry {cell.material} at {key(point)} without lamp oil and exposes the courier"
    elif mode == "high-sounding":
        from .quests import mark_treasure

        cache = min(
            (container for container in state.region.containers if not container.opened and container.id not in state.treasure_marks[state.active_region_id]),
            key=lambda container: (_distance(state.position, container.position), container.id),
        )
        mark_treasure(state, state.active_region_id, cache.id, f"Stair sounding fixes {cache.name} at {cache.position.x},{cache.position.y}, level {cache.position.z:+d}.")
        from .actions import emit_sound

        emit_sound(state, 3)
        detail = f"marks {cache.name}; its report adds three noise"
    elif mode == "footing":
        cleared = sorted({"poor-footing", "cut-feet"} & set(state.terrain_statuses))
        for status in cleared:
            state.terrain_statuses.pop(status, None)
        state.guarded_step = True
        detail = f"clears {', '.join(cleared)} and preserves one guarded step"
    elif mode == "bank-plug":
        point, cell = next((point, cell) for point, cell in _nearby_materials(state, 1) if cell.water and cell.material in {"soil", "timber"})
        before = (cell.water, cell.support)
        cell.water -= 1
        cell.support = min(3, cell.support + 1)
        cell.collapse_due = 0
        detail = f"changes wet {cell.material} at {key(point)} from water/support {before} to {(cell.water, cell.support)}"
    elif mode == "ration":
        priorities = ("fatigued", "coalheart-chill", "chilled", "smoke-inhalation")
        cleared = next((status for status in priorities if status in state.terrain_statuses), "no status")
        state.terrain_statuses.pop(cleared, None)
        before = state.courier.health
        state.courier.health = min(state.courier.max_health, before + 1)
        detail = f"clears {cleared} and restores health {before}→{state.courier.health}"
    elif mode == "bridge-dogs":
        supports = [
            (point, cell) for point, cell in _nearby_materials(state, 4)
            if cell.material in {"timber", "stone"} and (cell.support < 3 or cell.collapse_due)
        ][:2]
        for _, cell in supports:
            cell.support, cell.collapse_due = 3, 0
        from .actions import emit_sound

        emit_sound(state, 4)
        detail = f"seats {len(supports)} supports and adds four hammering noise"
    elif mode == "aim-break":
        targets = [
            actor for actor in state.combatants
            if actor.aimed_at is not None and _distance(state.position, actor.position) <= 10
        ][:3]
        for actor in targets:
            actor.aimed_at = None
            actor.intent = "prepared lane muffled; reacquiring"
        before = state.noise
        state.noise = max(0, state.noise - 3)
        state.sound_events = [
            event for event in state.sound_events
            if _distance(state.position, event.position) > 4
        ]
        detail = f"breaks {len(targets)} prepared lanes and dampens noise {before}→{state.noise}"
    elif mode == "drain-tile":
        from .world import base_tile

        point = next(point for point in _nearby_points(state, 1) if base_tile(state, point) == "m")
        state.region.tile_changes[key(point)] = "."
        cell = ensure_cell(state, point)
        cell.water, cell.coating = 0, "ash"
        detail = f"turns mud at {key(point)} into a dry ash-marked drain footing"
    elif mode == "kiln-sand":
        point, cell = next((point, cell) for point, cell in _nearby_materials(state, 1) if cell.fire)
        before = cell.fuel
        cell.fire, cell.fuel = 0, max(0, cell.fuel - 3)
        cell.smoke, cell.coating = min(4, cell.smoke + 2), "ash"
        detail = f"quenches {key(point)}, strips fuel {before}→{cell.fuel}, and raises ash smoke"
    elif mode == "ice-peg":
        point, cell = next((point, cell) for point, cell in _nearby_materials(state, 1) if cell.water and cell.fluid == "fresh" and not cell.fire)
        cell.water, cell.ice = 1, True
        state.water.pop(key(point), None)
        from .actions import emit_sound

        emit_sound(state, 1)
        detail = f"freezes one fresh shallow at {key(point)} and rings once"
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
        detail = f"thaws {len(ice)} ice cells, clears chill, and {'spends one lamp measure' if used_lamp else 'uses nearby fire'}"
    if not consume_carried(state, f"consumable:{name}"):
        raise RuntimeError("validated physical preparation disappeared before consumption")
    changes = state.region.changes if state.location == "region" else state.vessel_changes
    marker = f"preparation-used:{name}"
    changes[marker] = int(changes.get(marker, 0)) + 1
    text = f"{name.title()} {detail}; the physical preparation is spent."
    state.remember(f"{state.courier.name} used {name}: {detail}.")
    return True, text


def grant_contract_preparation(state: GameState, topology: str) -> str:
    from .inventory import auto_place, create_item, record_acquisition

    name = TOPOLOGY_PREPARATION[topology]
    item = create_item(
        state, f"consumable:{name}",
        f"finite preparation taught by the completed {topology}",
    )
    if auto_place(state, item.id, "pack", owner_id=state.active_courier_id):
        record_acquisition(state, item)
        where = "packed"
    else:
        item.location, item.region_id, item.ground_position = "ground", state.spatial_id, state.position
        where = "left physically beside the witness"
    state.remember(f"{state.courier.name} received {name} from the completed {topology}; {where}.")
    return f" The witness issues one {name}, {where}."


def validate_preparations() -> None:
    if len(PREPARATIONS) != 16 or len(TOPOLOGY_PREPARATION) != 16:
        raise ValueError("sixteen aftermath topologies need distinct finite preparations")
    from .aftermath import AFTERMATH_TOPOLOGIES

    topologies = {topology for pair in AFTERMATH_TOPOLOGIES.values() for topology in pair}
    if set(TOPOLOGY_PREPARATION) != topologies:
        raise ValueError("preparation production does not cover every aftermath topology")
    if len({preparation.mode for preparation in PREPARATIONS.values()}) != 16:
        raise ValueError("preparations need sixteen distinct production reducers")
