"""Additional physical arms sharing Jomon's targeting, inventory and action clock."""

from __future__ import annotations

from dataclasses import dataclass

from .catalog import EQUIPMENT_SECTIONS, load_catalog

@dataclass(frozen=True)
class ArsenalWeapon:
    name: str
    family: str
    shape: tuple[int, int]
    weight: int
    reach: int
    minimum: int
    damage: int
    noise: int
    effects: tuple[str, ...]
    regions: tuple[str, ...]
    ammunition: str | None = None

    @property
    def description(self) -> str:
        ammunition = ammunition_for(self.name)
        cost = f" Consumes one physical {ammunition}." if ammunition else ""
        return f"{self.family.title()} arm; {self.minimum}-{self.reach} pace reach, {self.damage} base harm. {', '.join(self.effects)}.{cost}"


_EQUIPMENT = load_catalog("equipment.json", EQUIPMENT_SECTIONS)
ARSENAL: dict[str, ArsenalWeapon] = {
    row["name"]: ArsenalWeapon(**{
        **row, "shape": tuple(row["shape"]),
        "effects": tuple(row["effects"]), "regions": tuple(row["regions"]),
    })
    for row in _EQUIPMENT["arsenal"]
}
BOMB_AMMUNITION = dict(_EQUIPMENT["bomb_ammunition"])


def ammunition_for(name: str) -> str | None:
    weapon = ARSENAL.get(name)
    if weapon is None:
        return None
    return f"{name.split()[0]} bombs" if weapon.family == "device" else weapon.ammunition


def strike(state, target_id=None, *, target_position=None):
    """Resolve one new arm through the same sight, cover, damage and world clock."""
    from .actions import (
        _attack_targets, _plain, _step_away, _step_toward, _time_result,
        attack_target_legality, effective_weapon_range, emit_sound,
    )
    from .enemy_equipment import harm_enemy
    from .inventory import consume_ammunition, physical_ammunition, release_enemy_possession
    from .materials import ensure_cell, material_at
    from .skill_tree import apply_weapon_skills, has_node, record_milestone
    from .world import cover_at, courier_sees, distance

    weapon = ARSENAL[state.weapon]
    targets = [actor for actor in _attack_targets(state, effective_weapon_range(state))
               if (target_id is None or actor.id == target_id)
               and (target_id is not None or actor.ecology != "prey")
               and attack_target_legality(state, actor)[0]]
    target = targets[0] if targets else None
    if weapon.family == "device":
        point = target_position or (target.position if target else None)
        if point is None or not weapon.minimum <= distance(state.position, point) <= effective_weapon_range(state) or not courier_sees(state, point):
            return _plain(state, "Choose a visible landing within this device's throwing reach.")
        cell = ensure_cell(state, point)
        if cell is None:
            return _plain(state, "That landing cannot hold a material reaction.")
        delayed = weapon.effects[0] == "thunder" and has_node(state.courier, "delayed-fuse")
        if delayed and (len(set(cell.reagents) | {"brine", "spark salt"}) > 4
                        or sum(cell.reagents.values()) > 6
                        or cell.reagents.get("brine", 0) >= 4 or cell.reagents.get("spark salt", 0) >= 4):
            return _plain(state, "The warned mineral flash needs two free sparse-cell measures.")
        ammunition = ammunition_for(state.weapon)
        if physical_ammunition(state, ammunition) <= 0 or not consume_ammunition(state, ammunition):
            return _plain(state, f"No physical {ammunition} remain in the pack.")
        effect = weapon.effects[0]
        if effect == "smoke":
            cell.smoke = max(cell.smoke, 4)
        elif effect == "pitch":
            cell.coating, cell.fuel, cell.fire = "oil", max(5 if has_node(state.courier, "adhesive-coat") else 3, cell.fuel), 0 if cell.water else 1
        elif effect == "lime":
            cell.coating, cell.smoke = "lime", max(2, cell.smoke)
        elif effect == "brine":
            cell.water, cell.fluid, cell.fire, cell.ice = 3, "salt", 0, False
        elif effect == "resin":
            cell.coating, cell.fuel = "resin", max(4 if has_node(state.courier, "adhesive-coat") else 2, cell.fuel)
        elif effect == "thunder":
            if delayed:
                cell.reagents["brine"] = cell.reagents.get("brine", 0) + 1
                cell.reagents["spark salt"] = cell.reagents.get("spark salt", 0) + 1
                cell.reaction_due = state.world_time + 2
            else:
                cell.smoke = max(2, cell.smoke)
        affected = [actor for actor in state.combatants
                    if actor.status in {"watching", "engaged"} and distance(actor.position, point) <= (1 if effect == "thunder" or effect == "resin" and has_node(state.courier, "line-trap") else 0)]
        for actor in affected:
            actor.status = "engaged"
            if effect == "thunder" and not delayed:
                harm_enemy(state, actor, 1, "warned thunder bomb", damage_kind="blunt")
                actor.morale -= 1 + int(has_node(state.courier, "controlled-chain"))
            elif effect == "thunder" and has_node(state.courier, "controlled-chain"):
                actor.morale -= 1
            elif effect == "resin":
                actor.intent = "bound by spilled resin; loses a turn pulling free"
            elif effect == "lime":
                actor.intent = "eyes stung by the lime cloud"
        if has_node(state.courier, "scatter-bank") and effect == "smoke":
            from .state import Position

            second = ensure_cell(state, Position(point.x + 1, point.y, point.z))
            if second:
                second.smoke = max(second.smoke, 2)
        sound = emit_sound(state, weapon.noise, point)
        record_milestone(state, "combat:devices")
        warning = " A mineral flash is warned for the next action." if delayed else ""
        return _time_result(state, f"{state.courier.name} throws one {ammunition} onto {point.x},{point.y}; {effect} changes the shared ground.{warning} " + " ".join(sound), priority=3)

    if target is None:
        return _plain(state, "No legal visible hostile is within this weapon's reach.")
    ammunition = ammunition_for(state.weapon)
    if ammunition and physical_ammunition(state, ammunition) <= 0:
        return _plain(state, f"No physical {ammunition} remain in the pack.")
    if weapon.family == "gun":
        required = 1 if "quick" in weapon.effects or has_node(state.courier, "vent-care") else 2
        if state.weapon_ready < required:
            return _plain(state, f"{state.weapon} needs {required - state.weapon_ready} guarded loading action(s); press G.")
    quick_bow = weapon.family == "bow" and has_node(state.courier, "quick-nock")
    if weapon.family in {"gun", "bow"} and "quick" not in weapon.effects and not quick_bow and state.aimed_target != target.id:
        state.aimed_target = target.id
        return _time_result(state, f"{state.courier.name} prepares {state.weapon} on {target.name}; firing commits the next action.", priority=3)
    if weapon.family == "gun" and state.weather in {"hard rain", "coast squall", "forest rain"} and not has_node(state.courier, "dry-load"):
        state.aimed_target = None
        return _time_result(state, "Wet weather spoils the exposed gun aim before the charge is released.", priority=3)
    if weapon.family == "bow" and state.weather in {"hard rain", "coast squall", "forest rain"} and not has_node(state.courier, "wind-hold"):
        state.aimed_target = None
        return _time_result(state, "Wet weather spoils the drawn bow string before the arrow is released.", priority=3)
    if ammunition and not consume_ammunition(state, ammunition):
        return _plain(state, f"No physical {ammunition} remain.")
    if weapon.family == "gun":
        state.weapon_ready = 0
    state.aimed_target = None
    target.status = "engaged"
    damage = weapon.damage
    cover = cover_at(state, state.position, target.position) if ammunition else "none"
    if cover == "partial" and "armour" not in weapon.effects:
        damage = max(0, damage - 1)
    if "armour" in weapon.effects and (target.elite or target.role == "protector" or target.profile == "machinery"):
        damage += 2
    if "morale" in weapon.effects:
        target.morale -= 2
    if "interrupt" in weapon.effects:
        target.aimed_at = None
        target.intent = "prepared action interrupted by a close strike"
    if "bind" in weapon.effects:
        target.intent = "bound by a committed strike; loses a turn breaking free"
    if "push" in weapon.effects:
        target.position = _step_away(state, target)
    if "pull" in weapon.effects:
        target.position = _step_toward(state, target, state.position)
    if "guard" in weapon.effects:
        state.guarded_step = True
    if "charge" in weapon.effects and state.last_move_turn == state.world_time - 1:
        damage += 1
    if "sweep" in weapon.effects:
        for other in state.combatants:
            if other.id != target.id and other.status in {"watching", "engaged"} and distance(other.position, target.position) <= 1:
                harm_enemy(state, other, 1, f"{state.weapon} sweep", damage_kind="cut")
    if "timber" in weapon.effects and material_at(state, target.position) == "timber":
        cell = ensure_cell(state, target.position)
        if cell:
            cell.support = max(0, cell.support - 1)
    if "reeds" in weapon.effects and material_at(state, target.position) == "reeds":
        cell = ensure_cell(state, target.position)
        if cell:
            cell.material, cell.fuel = "soil", max(2, cell.fuel)
    if "smoke" in weapon.effects:
        cell = ensure_cell(state, state.position)
        if cell:
            cell.smoke = max(2, cell.smoke)
        target.morale -= 1
    kind = "pierce" if "pierce" in weapon.effects else "cut" if "cut" in weapon.effects else "blunt"
    sound = emit_sound(state, weapon.noise)
    damage, skill_text, skill_guard = apply_weapon_skills(state, target, damage)
    harm = harm_enemy(state, target, damage, f"{state.courier.name}'s {state.weapon}", damage_kind=kind)
    if harm.defeated or (target.morale <= 0 and target.profile != "machinery"):
        target.status = "defeated" if harm.defeated else "retreated"
        target.intent = "removed from the route"
        recovered = harm.dropped if harm.defeated else release_enemy_possession(state, target)
        state.remember(f"{state.courier.name} {target.status} {target.name} with {state.weapon}.")
        message = f"{state.weapon.title()} removes {target.name} from the route.{recovered}"
    else:
        message = f"{state.weapon.title()} deals {harm.amount} {kind} harm to {target.name}; {target.health}/{target.max_health} remains."
    branch = {"blade": "blades", "reach": "reach", "impact": "reach", "bow": "bows", "gun": "gunworks"}[weapon.family]
    record_milestone(state, f"combat:{branch}")
    if skill_text:
        message += " " + skill_text + "."
    return _time_result(state, " ".join([message, *sound]), guarded="guard" in weapon.effects or skill_guard, priority=3)


if len(ARSENAL) != 36:
    raise RuntimeError("expanded arsenal requires thirty-six physical arms")
