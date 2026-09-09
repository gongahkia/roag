"""Working weapons with six explicit actions, not an item scripting language."""

from dataclasses import dataclass


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


WORK_WEAPONS = {
    "pot sling": WorkingWeapon("Cooper's pot sling", (2, 3), 3, 6, 3, 0, 3, "P selects a finite pitch, lime or brine pot; Enter casts at a visible cell three to six paces away. The shared reaction can harm anyone.", ("dunmire", "marlbank")),
    "throwing axe": WorkingWeapon("Balanced throwing axe", (2, 3), 4, 5, 1, 3, 3, "Throws the actual readied axe onto the impact cell, leaving an empty weapon slot. It weakens timber support and must be physically recovered.", ("rillscar", "frostmere")),
    "forked pike": WorkingWeapon("Forked ward pike", (2, 5), 7, 3, 2, 1, 2, "Pins the target and one neighbour across its forward line, buying one turn against a pair. Adjacent foes are inside the forks.", ("marlbank", "rillscar")),
    "war flail": WorkingWeapon("Jointed threshing flail", (2, 4), 6, 2, 1, 2, 4, "One exposed wind-up precedes a sweep through nearby foes. Moving abandons the wind-up; the wide head also harms a nearby convoy escort.", ("marlbank", "dunmire")),
    "spade": WorkingWeapon("Bank cutter's spade", (2, 4), 5, 1, 1, 1, 2, "Cuts reeds and digs banks with F. A strike from dry soil or ash throws sight-obscuring dust; wet or stone footing gives no dust.", ("dunmire", "frostmere")),
    "shield and hanger": WorkingWeapon("Boarding shield and hanger", (3, 3), 8, 3, 1, 1, 4, "A clear, dry, same-level approach closes up to two paces under guard before a short cut. An encumbered load or injured leg cannot charge.", ("rillscar", "frostmere")),
}

POT_AMMUNITION = {
    "pitch pots": "consumable:sealed pitch pot",
    "lime pots": "consumable:sealed lime pot",
    "brine pots": "consumable:sealed brine pot",
}


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
        return _plain(state, f"The pot sling needs a visible landing three to {reach} paces away.")
    choices = available_pots(state)
    ammunition = ammunition or (choices[0] if choices else None)
    if ammunition not in choices:
        return _plain(state, "That physical pot is not in the pack; nothing was thrown.")
    cell = ensure_cell(state, point)
    if cell is None:
        return _plain(state, "The landing has no usable material space; the pot stays packed.")
    consume_ammunition(state, ammunition)
    if ammunition == "pitch pots":
        cell.material, cell.coating, cell.fuel = "oil", "oil", 5
        cell.fire = 0 if cell.water else 1
        message = "The pitch pot wets the landing with fuel" + ("; existing water denies ignition." if cell.water else "; fire can spread downwind into dry material.")
    elif ammunition == "lime pots":
        cell.material, cell.coating, cell.smoke = "lime", "lime", 3
        message = "The lime pot raises an abrasive cloud; water sustains its caustic slurry."
    else:
        cell.fluid, cell.water, cell.coating, cell.ice, cell.fire = "salt", 3, "salt", False, 0
        message = "The brine pot quenches the landing; salt water follows openings and burdens exposed bodies."
    from .workshop import attack_effects
    sound, fitting = attack_effects(state, point, 1, None)
    emit_sound(state, sound)
    emit_sound(state, 3, point)
    if fitting:
        message += " " + fitting + "; the pot still breaks loudly at its landing."
    return _time_result(state, message, priority=3)


def approach(state, target):
    """Return a legal shielded approach, or a reason without changing state."""
    from .inventory import load_state
    from .world import base_tile, is_walkable, projectile_path

    if load_state(state) in {"encumbered", "overloaded"} or {"legs", "feet"} & set(state.courier.injuries) or {"bogged", "poor-footing", "net-drag"} & set(state.terrain_statuses):
        return None, "A shielded approach needs a manageable load and sound legs and feet."
    if state.position.z != target.position.z:
        return None, "The shield cannot charge through a different floor."
    points = projectile_path(state.position, target.position, state)[1:-1]
    if any(not is_walkable(state, p) or base_tile(state, p) in {"m", ",", "~", "d", "O"} for p in [state.position, *points]):
        return None, "Water, broken footing or another body blocks the shielded approach."
    from .materials import fields, key
    if any((cell := fields(state).get(key(p))) and (cell.water or cell.ice) for p in [state.position, *points]):
        return None, "Wet or frozen material denies a shielded charge."
    return points, ""


def strike_effects(state, target, candidates):
    from .inventory import release_enemy_possession
    from .materials import ensure_cell, fields, key, material_at
    from .world import distance, line_of_sight

    if state.weapon == "forked pike":
        target.intent = "pinned between the fork tines"
        neighbours = [a for a in candidates if a.id != target.id and a.position.z == target.position.z
                      and distance(a.position, target.position) <= 1 and distance(a.position, state.position) >= 2]
        dx, dy = target.position.x - state.position.x, target.position.y - state.position.y
        across = next((a for a in neighbours if (a.position.x == target.position.x if abs(dx) >= abs(dy) else a.position.y == target.position.y)), None)
        if across:
            across.intent = "pinned across the forked guard line"
        return "the fork pins " + (f"both {target.name} and {across.name}" if across else target.name)
    if state.weapon == "war flail":
        count = 0
        for actor in candidates:
            if actor.id == target.id or distance(state.position, actor.position) > 2 or not line_of_sight(state, state.position, actor.position):
                continue
            actor.health = max(0, actor.health - 2)
            if actor.health == 0:
                actor.status = "defeated"
                release_enemy_possession(state, actor)
            count += 1
        return f"the exposed wind-up sweeps {count} other nearby bodies"
    if state.weapon == "spade":
        cell = fields(state).get(key(state.position))
        if material_at(state, state.position) in {"soil", "ash"} and not (cell and cell.water):
            dust = ensure_cell(state, target.position)
            if dust:
                dust.coating, dust.smoke = "ash", max(2, dust.smoke)
                return "dry bank dust obscures the struck cell and changes the next pursuit"
        return "wet or stone footing gives no dust to throw"
    if state.weapon == "throwing axe":
        if material_at(state, target.position) == "timber":
            cell = ensure_cell(state, target.position)
            if cell:
                cell.support = max(0, cell.support - 1)
                return "the lodged axe weakens timber before falling onto the impact cell"
        return "the actual axe falls onto the impact cell; the weapon slot will be empty"
    if state.weapon == "shield and hanger":
        state.guarded_step = True
        return "the shielded approach trades damage for closing the lane"
    return ""
