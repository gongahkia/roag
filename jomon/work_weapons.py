"""Working weapons with six explicit actions, not an item scripting language."""

from dataclasses import dataclass

from .catalog import EQUIPMENT_SECTIONS, load_catalog
from .equipment_presentation import equipment_format, work_weapon_description, work_weapon_name

@dataclass(frozen=True)
class WorkingWeapon:
    name: str
    shape: tuple[int, int]
    weight: int
    reach: int
    minimum: int
    damage: int
    noise: int
    description: str
    regions: tuple[str, ...]


@dataclass(frozen=True)
class WorkingStrike:
    """Small result shared by the authored working-weapon reducers."""

    text: str
    bonus_damage: int = 0
    guarded: bool = False


_EQUIPMENT = load_catalog("equipment.json", EQUIPMENT_SECTIONS)
WORK_WEAPONS = {
    name: WorkingWeapon(**{**row, "name": work_weapon_name(name), "description": work_weapon_description(name), "shape": tuple(row["shape"]), "regions": tuple(row["regions"])})
    for name, row in _EQUIPMENT["work_weapons"].items()
}
POT_AMMUNITION = dict(_EQUIPMENT["pot_ammunition"])


def _set_equipment_intent(threat, semantic_id: str, **values: object) -> None:
    threat.intent_id = semantic_id
    threat.intent = equipment_format(semantic_id, **values)


def available_pots(state):
    from .inventory import physical_ammunition
    return [name for name in POT_AMMUNITION if physical_ammunition(state, name)]


def cast_pot(state, point, ammunition=None):
    from .actions import _plain, _time_result, effective_weapon_range, emit_sound
    from .inventory import consume_ammunition
    from .materials import ensure_cell
    from .world import courier_sees, distance

    reach = effective_weapon_range(state)
    if point is None or not 3 <= distance(state.position, point) <= reach or not courier_sees(state, point):
        return _plain(state, equipment_format("equipment.pot.invalid_range", reach=reach))
    choices = available_pots(state)
    ammunition = ammunition or (choices[0] if choices else None)
    if ammunition not in choices:
        return _plain(state, equipment_format("equipment.pot.missing"))
    cell = ensure_cell(state, point)
    if cell is None:
        return _plain(state, equipment_format("equipment.pot.no_cell"))
    consume_ammunition(state, ammunition)
    if ammunition == "pitch pots":
        cell.material, cell.coating, cell.fuel = "oil", "oil", 5
        cell.fire = 0 if cell.water else 1
        message = equipment_format("equipment.pot.pitch.wet" if cell.water else "equipment.pot.pitch.dry")
    elif ammunition == "lime pots":
        cell.material, cell.coating, cell.smoke = "lime", "lime", 3
        message = equipment_format("equipment.pot.lime")
    else:
        cell.fluid, cell.water, cell.coating, cell.ice, cell.fire = "salt", 3, "salt", False, 0
        message = equipment_format("equipment.pot.brine")
    from .workshop import attack_effects
    sound, fitting = attack_effects(state, point, 1, None)
    emit_sound(state, sound)
    emit_sound(state, 3, point)
    if fitting:
        message += " " + equipment_format("equipment.pot.fitting", fitting=fitting)
    from .skill_tree import record_milestone

    record_milestone(state, "combat:devices")
    return _time_result(state, message, priority=3)


def approach(state, target):
    """Return a legal shielded approach, or a reason without changing state."""
    from .inventory import load_state
    from .world import base_tile, is_walkable, projectile_path

    if load_state(state) in {"encumbered", "overloaded"} or {"legs", "feet"} & set(state.courier.injuries) or {"bogged", "poor-footing", "net-drag"} & set(state.terrain_statuses):
        return None, equipment_format("equipment.approach.load")
    if state.position.z != target.position.z:
        return None, equipment_format("equipment.approach.level")
    points = projectile_path(state.position, target.position, state)[1:-1]
    if any(not is_walkable(state, p) or base_tile(state, p) in {"m", ",", "~", "d", "O"} for p in [state.position, *points]):
        return None, equipment_format("equipment.approach.blocked")
    from .materials import fields, key
    if any((cell := fields(state).get(key(p))) and (cell.water or cell.ice) for p in [state.position, *points]):
        return None, equipment_format("equipment.approach.wet")
    return points, ""


def strike_effects(state, target, candidates):
    from .enemy_equipment import harm_enemy
    from .materials import ensure_cell, fields, key, material_at
    from .world import distance, line_of_sight

    if state.weapon == "forked pike":
        _set_equipment_intent(target, "intent.pinned.fork")
        neighbours = [a for a in candidates if a.id != target.id and a.position.z == target.position.z
                      and distance(a.position, target.position) <= 1 and distance(a.position, state.position) >= 2]
        dx, dy = target.position.x - state.position.x, target.position.y - state.position.y
        across = next((a for a in neighbours if (a.position.x == target.position.x if abs(dx) >= abs(dy) else a.position.y == target.position.y)), None)
        if across:
            _set_equipment_intent(across, "intent.pinned.fork_line")
        return WorkingStrike(equipment_format("equipment.strike.forked_pike.pair", target=target.name, across=across.name) if across else equipment_format("equipment.strike.forked_pike", target=target.name))
    if state.weapon == "war flail":
        count = 0
        for actor in candidates:
            if actor.id == target.id or distance(state.position, actor.position) > 2 or not line_of_sight(state, state.position, actor.position):
                continue
            harm_enemy(state, actor, 2, "war flail sweep", damage_kind="blunt")
            count += 1
        return WorkingStrike(equipment_format("equipment.strike.war_flail", count=count))
    if state.weapon == "spade":
        cell = fields(state).get(key(state.position))
        if material_at(state, state.position) in {"soil", "ash"} and not (cell and cell.water):
            dust = ensure_cell(state, target.position)
            if dust:
                dust.coating, dust.smoke = "ash", max(2, dust.smoke)
                return WorkingStrike(equipment_format("equipment.strike.spade.dust"))
        return WorkingStrike(equipment_format("equipment.strike.spade.none"))
    if state.weapon == "throwing axe":
        if material_at(state, target.position) == "timber":
            cell = ensure_cell(state, target.position)
            if cell:
                cell.support = max(0, cell.support - 1)
                return WorkingStrike(equipment_format("equipment.strike.throwing_axe.timber"))
        return WorkingStrike(equipment_format("equipment.strike.throwing_axe.ground"))
    if state.weapon == "shield and hanger":
        state.guarded_step = True
        return WorkingStrike(equipment_format("equipment.strike.shield_hanger"), guarded=True)
    if state.weapon == "glaive":
        adjacent = next(
            (
                actor for actor in candidates
                if actor.id != target.id and actor.position.z == target.position.z
                and distance(actor.position, target.position) <= 1
            ),
            None,
        )
        if adjacent:
            harm_enemy(state, adjacent, 1, "glaive follow-through", damage_kind="cut")
        return WorkingStrike(equipment_format("equipment.strike.glaive.clip", target=adjacent.name) if adjacent else equipment_format("equipment.strike.glaive.none"))
    if state.weapon == "pollaxe":
        cell = ensure_cell(state, target.position)
        support = bool(cell and material_at(state, target.position) == "timber")
        if support:
            cell.support = max(0, cell.support - 1)
        armoured = target.elite or target.role == "protector" or target.profile == "machinery"
        return WorkingStrike(
            equipment_format("equipment.strike.pollaxe.armoured" if armoured else "equipment.strike.pollaxe.unarmoured") + (equipment_format("equipment.strike.pollaxe.support") if support else ""),
            bonus_damage=1 if armoured else 0,
        )
    if state.weapon == "arming sword":
        target.morale -= 1
        state.guarded_step = True
        return WorkingStrike(equipment_format("equipment.strike.arming_sword"), guarded=True)
    if state.weapon == "long knife":
        interrupted = target.aimed_at is not None
        target.aimed_at = None
        if interrupted:
            target.reload_turns = max(1, target.reload_turns)
            _set_equipment_intent(target, "intent.disrupted.long_knife")
        return WorkingStrike(equipment_format("equipment.strike.long_knife.interrupt" if interrupted else "equipment.strike.long_knife.normal"))
    if state.weapon == "boat hook":
        cell = fields(state).get(key(target.position))
        pulls = 2 if cell and cell.water else 1
        moved = 0
        for _ in range(pulls):
            previous = target.position
            from .enemy_ai import next_path_step
            target.position = next_path_step(state, target, state.position, stop_distance=1)
            if target.position == previous:
                break
            moved += 1
        _set_equipment_intent(target, "intent.disrupted.boat_hook")
        return WorkingStrike(equipment_format("equipment.strike.boat_hook", moved=moved, suffix="s" if moved != 1 else ""))
    if state.weapon == "flanged mace":
        target.morale -= 3
        _set_equipment_intent(target, "intent.dazed.flanged_mace")
        return WorkingStrike(equipment_format("equipment.strike.flanged_mace"))
    if state.weapon == "estoc":
        armoured = target.elite or target.role == "protector" or target.profile == "machinery"
        return WorkingStrike(
            equipment_format("equipment.strike.estoc.armoured" if armoured else "equipment.strike.estoc.unarmoured"),
            bonus_damage=2 if armoured else 0,
        )
    if state.weapon == "felling axe":
        cell = ensure_cell(state, target.position)
        cut = bool(cell and material_at(state, target.position) == "timber")
        if cut:
            cell.support = max(0, cell.support - 2)
        return WorkingStrike(equipment_format("equipment.strike.felling_axe.cut" if cut else "equipment.strike.felling_axe.none"))
    if state.weapon == "quarterstaff":
        from .enemy_ai import retreat_step
        target.position = retreat_step(state, target)
        _set_equipment_intent(target, "intent.disrupted.quarterstaff")
        state.guarded_step = True
        return WorkingStrike(equipment_format("equipment.strike.quarterstaff"), guarded=True)
    if state.weapon == "reed sickle":
        cell = ensure_cell(state, target.position)
        cut = bool(cell and material_at(state, target.position) == "reeds")
        if cut:
            cell.material, cell.fuel = "soil", max(2, cell.fuel)
        if target.profile == "animal":
            target.morale -= 1
        return WorkingStrike(equipment_format("equipment.strike.reed_sickle.cut" if cut else "equipment.strike.reed_sickle.none"))
    if state.weapon == "anchor fluke":
        from .enemy_ai import next_path_step
        target.position = next_path_step(state, target, state.position, stop_distance=1)
        courier_cell = fields(state).get(key(state.position))
        anchored = bool(courier_cell and courier_cell.water)
        if anchored:
            state.guarded_step = True
        _set_equipment_intent(target, "intent.disrupted.anchor_fluke")
        return WorkingStrike(
            equipment_format("equipment.strike.anchor_fluke.anchored" if anchored else "equipment.strike.anchor_fluke.normal"),
            guarded=anchored,
        )
    if state.weapon == "chain hook":
        _set_equipment_intent(target, "intent.entangled.chain")
        target.morale -= 1
        return WorkingStrike(equipment_format("equipment.strike.chain_hook"))
    return WorkingStrike("")
