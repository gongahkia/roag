"""Finite personal spellcraft acting on the same sparse world as tools and fire."""

from __future__ import annotations

from dataclasses import dataclass

from .catalog import CatalogError, load_catalog
from .state import GameState, Position


@dataclass(frozen=True)
class Spell:
    id: str
    name: str
    cost: int
    reach: int
    effect: str
    power: int
    radius: int = 0
    target: str = "cell"

    @property
    def description(self) -> str:
        return f"{self.effect.replace('-', ' ')} {self.power}; radius {self.radius}; {self.cost} mana, reach {self.reach}"


_rows = load_catalog("spells.json", ("spells",))["spells"]
if not isinstance(_rows, list):
    raise CatalogError("spells.json must provide spell rows")
SPELL_ROWS = tuple(tuple(row) for row in _rows if isinstance(row, list) and len(row) == 8)
if len(SPELL_ROWS) != len(_rows):
    raise CatalogError("spells.json has invalid spell rows")
for spell_id, name, cost, reach, effect, power, radius, target in SPELL_ROWS:
    if (any(not isinstance(part, str) or not part for part in (spell_id, name, effect, target))
            or any(type(part) is not int or part < 0 for part in (cost, reach, power, radius))
            or target not in {"cell", "enemy", "self"}):
        raise CatalogError("spells.json has invalid spell fields")
if len({row[0] for row in SPELL_ROWS}) != len(SPELL_ROWS):
    raise CatalogError("spells.json repeats a spell ID")


SPELLS = {row[0]: Spell(*row) for row in SPELL_ROWS}
SPELL_TIERS = {
    node: tuple(row[0] for row in SPELL_ROWS[index * 4:(index + 1) * 4])
    for index, node in enumerate(("attunement", "elemental-shape", "ward-script", "veiling", "echo-binding", "spell-weave"))
}


def grant_tier(person, node_id: str) -> None:
    for spell_id in SPELL_TIERS.get(node_id, ()):
        if spell_id not in person.known_spells:
            person.known_spells.append(spell_id)
    if node_id == "attunement":
        person.max_mana += 2
        person.mana += 2


def spell_status(state: GameState, spell_id: str, point: Position) -> tuple[bool, str]:
    from .world import courier_sees, distance

    person = state.courier
    spell = SPELLS.get(spell_id)
    if person is None or spell is None or spell_id not in person.known_spells:
        return False, "the courier has not learned this spell"
    if state.location == "jomon" and state.jomon_space == "tavern":
        return False, "spellwork belongs outside the crowded common room"
    if person.mana < spell.cost:
        return False, f"needs {spell.cost} mana; {person.mana} remains"
    if spell.target == "self":
        return (point == state.position, "self-cast must be placed on the courier")
    if distance(state.position, point) > spell.reach or not courier_sees(state, point):
        return False, f"needs a place in sight within {spell.reach} paces"
    if spell.target == "enemy" and not any(
        actor.position == point and actor.status in {"watching", "engaged"}
        for actor in state.combatants
    ):
        return False, "needs an active opponent at the marked place"
    return True, "ready"


def cast(state: GameState, spell_id: str, point: Position) -> tuple[bool, str]:
    from .actions import _advance_world, _step_away, _step_toward, emit_sound
    from .enemy_equipment import harm_enemy
    from .inventory import add_status
    from .materials import MAX_CELLS, ensure_cell, fields, key
    from .skill_tree import record_milestone
    from .world import base_tile

    legal, reason = spell_status(state, spell_id, point)
    if not legal:
        return False, reason
    spell = SPELLS[spell_id]
    positions = [Position(point.x + dx, point.y + dy, point.z)
                 for dy in range(-spell.radius, spell.radius + 1)
                 for dx in range(-spell.radius, spell.radius + 1)
                 if max(abs(dx), abs(dy)) <= spell.radius]
    if spell.target == "cell" and any(base_tile(state, place) == " " for place in positions):
        return False, "one affected place lies beyond reach of the working ground"
    if spell.target == "cell" and len(fields(state)) + sum(key(place) not in fields(state) for place in positions) > MAX_CELLS:
        return False, "the sparse material budget is full"
    state.courier.mana -= spell.cost
    detail = []
    if spell.target == "self":
        if spell.effect == "heal":
            before = state.courier.health
            state.courier.health = min(state.courier.max_health, before + spell.power)
            detail.append(f"health {before}→{state.courier.health}")
        elif spell.effect == "ward":
            state.guarded_step = True
            detail.append("guard readied")
        elif spell.effect == "cleanse":
            removed = [name for name in ("smoke-inhalation", "salt-grit", "lime-grit", "wet") if state.terrain_statuses.pop(name, None)]
            add_status(state, "clear-breath", "a cleansing spell", spell.power + 1, "fresh smoke cannot be inhaled while the ward lasts")
            detail.append("cleared " + (", ".join(removed) or "no current exposure"))
        elif spell.effect == "quiet":
            add_status(state, "quiet-veil", "a veiling spell", spell.power + 1, "movement sheds one less sound")
            detail.append(f"quiet for {spell.power} actions")
    elif spell.target == "enemy":
        target = next(actor for actor in state.combatants if actor.position == point and actor.status in {"watching", "engaged"})
        target.status = "engaged"
        if spell.effect == "push":
            target.position = _step_away(state, target)
            target.intent = "displaced by wind magic"
        elif spell.effect == "pull":
            target.position = _step_toward(state, target, state.position)
            target.intent = "hauled by river magic"
        elif spell.effect == "bind":
            target.intent = "bound in enchanted reeds; loses a turn breaking free"
        else:
            kind = "pierce" if spell.effect == "pierce" else "blunt"
            harm = harm_enemy(state, target, spell.power, spell.name, damage_kind=kind)
            if spell.effect == "ice":
                target.conditions["chilled"] = max(3, target.conditions.get("chilled", 0))
            if spell.effect == "thunder":
                target.morale -= 1
            if harm.defeated:
                target.status = "defeated"
        detail.append(f"{target.name} at {point.x},{point.y}")
    else:
        for place in positions:
            cell = ensure_cell(state, place)
            if spell.effect == "fire":
                cell.fire, cell.fuel = min(3, max(cell.fire, spell.power)), max(cell.fuel, spell.power + 1)
            elif spell.effect == "water":
                cell.water, cell.fire, cell.fluid = min(3, cell.water + spell.power), 0, "fresh"
            elif spell.effect == "smoke":
                cell.smoke = min(4, max(cell.smoke, spell.power + 1))
            elif spell.effect == "salt":
                cell.water, cell.fluid, cell.coating = min(3, cell.water + spell.power), "salt", "salt"
            elif spell.effect == "ice":
                cell.water, cell.ice, cell.fire = max(1, cell.water), True, 0
            elif spell.effect == "support":
                cell.support, cell.collapse_due = min(3, cell.support + spell.power), 0
            elif spell.effect == "lime":
                cell.coating, cell.smoke = "lime", max(2, cell.smoke)
            elif spell.effect == "decoy":
                emit_sound(state, spell.power, place)
        detail.append(f"{len(positions)} patches altered at {point.x},{point.y}")
    if spell.effect not in {"quiet", "decoy"}:
        emit_sound(state, 1 if spell.cost < 3 else 3, point)
    record_milestone(state, "combat:spellcraft")
    message = f"{state.courier.name} casts {spell.name} ({spell.cost} mana): {'; '.join(detail)}."
    _advance_world(state)
    state.add_message(message, priority=3)
    return True, message


def rest_at_berths(state: GameState) -> tuple[bool, str]:
    from .actions import _advance_world
    from .world import base_tile

    if state.location != "jomon" or state.jomon_space != "vessel" or base_tile(state, state.position) != "b" or state.voyage_status == "active":
        return False, "rest for mana at Jomon's berth while moored"
    if state.courier.mana >= state.courier.max_mana:
        return False, "mana is already full"
    _advance_world(state, steps=6)
    state.courier.mana = state.courier.max_mana
    message = f"{state.courier.name} rests six actions at a berth; mana returns to {state.courier.mana}."
    state.add_message(message, priority=3)
    return True, message


def restore_at_shrine(state: GameState) -> tuple[bool, str]:
    from .actions import _advance_world
    from .world import distance

    if state.location != "region" or state.courier is None or not state.courier.known_spells:
        return False, "shrine restoration needs an attuned courier out of combat"
    entrance = state.region.landmarks.get("cave_entrance")
    if entrance is None or distance(state.position, entrance) > 1:
        return False, "stand beside the marked cave-mouth shrine"
    if any(threat.status in {"watching", "engaged"} and distance(state.position, threat.position) <= 5
           for threat in state.combatants):
        return False, "nearby opponents interrupt the cave-mouth vigil"
    if state.courier.mana >= state.courier.max_mana:
        return False, "mana is already full"
    key = f"shrine:{state.courier.id}"
    today = state.world_time // 36
    if state.region.changes.get(key) == today:
        return False, "this courier has already used the shrine today"
    courier = state.courier
    state.region.changes[key] = today
    before = courier.mana
    courier.mana = min(courier.max_mana, before + 3)
    restored = courier.mana
    _advance_world(state, steps=3)
    message = f"{courier.name} keeps a three-action vigil at the cave-mouth shrine; mana {before}→{restored}."
    state.add_message(message, priority=3)
    return True, message


if len(SPELLS) != 24:
    raise RuntimeError("spellcraft requires twenty-four authored spells")
