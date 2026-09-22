"""Finite personal spellcraft acting on the same sparse world as tools and fire."""

from __future__ import annotations

from dataclasses import dataclass

from .catalog import CatalogError, load_catalog
from .magic_presentation import magic_format, magic_text, spell_display_name
from .state import GameState, Position


@dataclass(frozen=True)
class Spell:
    id: str
    cost: int
    reach: int
    effect: str
    power: int
    radius: int = 0
    target: str = "cell"


_rows = load_catalog("spells.json", ("spells",))["spells"]
if not isinstance(_rows, list):
    raise CatalogError("spells.json must provide spell rows")
SPELL_ROWS = tuple(tuple(row) for row in _rows if isinstance(row, list) and len(row) == 7)
if len(SPELL_ROWS) != len(_rows):
    raise CatalogError("spells.json has invalid spell rows")
for spell_id, cost, reach, effect, power, radius, target in SPELL_ROWS:
    if (any(not isinstance(part, str) or not part for part in (spell_id, effect, target))
            or any(type(part) is not int or part < 0 for part in (cost, reach, power, radius))
            or target not in {"cell", "enemy", "self"}):
        raise CatalogError("spells.json has invalid spell fields")
if len({row[0] for row in SPELL_ROWS}) != len(SPELL_ROWS):
    raise CatalogError("spells.json repeats a spell ID")


SPELLS = {row[0]: Spell(*row) for row in SPELL_ROWS}

# These historical display values remain engine-only inputs to the pre-existing
# deterministic enemy-hit seed. Selected-pack display names never enter combat RNG.
_LEGACY_DAMAGE_SOURCES = {
    "ash-shot": "Ash shot", "storm-chord": "Storm chord", "iron-echo": "Iron echo",
    "frostbolt": "Frostbolt", "lightning-bolt": "Lightning bolt", "magic-missile": "Magic missile",
}


def _damage_source(spell_id: str) -> str:
    return _LEGACY_DAMAGE_SOURCES[spell_id]


def _set_intent(threat, intent_id: str, **values: object) -> None:
    threat.intent_id = intent_id
    threat.intent = magic_format(intent_id, **values)


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
        return False, magic_text("magic.status.unlearned")
    if state.location == "jomon" and state.jomon_space == "tavern":
        return False, magic_text("magic.status.tavern")
    if person.mana < spell.cost:
        return False, magic_format("magic.status.mana", cost=spell.cost, mana=person.mana)
    if spell.target == "self":
        return (point == state.position, magic_text("magic.status.self"))
    if distance(state.position, point) > spell.reach or not courier_sees(state, point):
        return False, magic_format("magic.status.range", reach=spell.reach)
    if spell.target == "enemy" and not any(
        actor.position == point and actor.status in {"watching", "engaged"}
        for actor in state.combatants
    ):
        return False, magic_text("magic.status.enemy")
    return True, magic_text("magic.status.ready")


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
        return False, magic_text("magic.cast.invalid_ground")
    if spell.target == "cell" and len(fields(state)) + sum(key(place) not in fields(state) for place in positions) > MAX_CELLS:
        return False, magic_text("magic.cast.material_budget")
    state.courier.mana -= spell.cost
    detail = []
    if spell.target == "self":
        if spell.effect == "heal":
            before = state.courier.health
            state.courier.health = min(state.courier.max_health, before + spell.power)
            detail.append(magic_format("magic.cast.detail.heal", before=before, after=state.courier.health))
        elif spell.effect == "ward":
            state.guarded_step = True
            detail.append(magic_text("magic.cast.detail.ward"))
        elif spell.effect == "cleanse":
            removed = [name for name in ("smoke-inhalation", "salt-grit", "lime-grit", "wet") if state.terrain_statuses.pop(name, None)]
            add_status(state, "clear-breath", magic_text("magic.status.clear_breath.cause"), spell.power + 1, magic_text("magic.status.clear_breath.consequence"))
            detail.append(magic_format("magic.cast.detail.cleanse", statuses=", ".join(removed) or magic_text("magic.cast.detail.no_exposure")))
        elif spell.effect == "quiet":
            add_status(state, "quiet-veil", magic_text("magic.status.quiet_veil.cause"), spell.power + 1, magic_text("magic.status.quiet_veil.consequence"))
            detail.append(magic_format("magic.cast.detail.quiet", duration=spell.power))
    elif spell.target == "enemy":
        target = next(actor for actor in state.combatants if actor.position == point and actor.status in {"watching", "engaged"})
        target.status = "engaged"
        if spell.effect == "push":
            target.position = _step_away(state, target)
            _set_intent(target, "intent.magic.push")
        elif spell.effect == "pull":
            target.position = _step_toward(state, target, state.position)
            _set_intent(target, "intent.magic.pull")
        elif spell.effect == "bind":
            _set_intent(target, "intent.magic.bind")
        else:
            kind = "pierce" if spell.effect == "pierce" else "blunt"
            harm = harm_enemy(state, target, spell.power, _damage_source(spell.id), damage_kind=kind)
            if spell.effect == "ice":
                target.conditions["chilled"] = max(3, target.conditions.get("chilled", 0))
            if spell.effect == "thunder":
                target.morale -= 1
            if harm.defeated:
                target.status = "defeated"
                _set_intent(target, "intent.magic.defeated", spell=spell_display_name(spell.id), location=harm.location)
        detail.append(magic_format("magic.cast.detail.enemy", target=target.name, x=point.x, y=point.y))
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
        detail.append(magic_format("magic.cast.detail.cell", count=len(positions), x=point.x, y=point.y))
    if spell.effect not in {"quiet", "decoy"}:
        emit_sound(state, 1 if spell.cost < 3 else 3, point)
    record_milestone(state, "combat:spellcraft")
    message = magic_format("magic.cast.result", courier=state.courier.name, spell=spell_display_name(spell.id), cost=spell.cost, detail="; ".join(detail))
    _advance_world(state)
    state.add_message(message, priority=3)
    return True, message


def rest_at_berths(state: GameState) -> tuple[bool, str]:
    from .actions import _advance_world
    from .world import base_tile

    if state.location != "jomon" or state.jomon_space != "vessel" or base_tile(state, state.position) != "b" or state.voyage_status == "active":
        return False, magic_text("magic.rest.berth.invalid")
    if state.courier.mana >= state.courier.max_mana:
        return False, magic_text("magic.rest.full")
    _advance_world(state, steps=6)
    state.courier.mana = state.courier.max_mana
    message = magic_format("magic.rest.berth.result", courier=state.courier.name, mana=state.courier.mana)
    state.add_message(message, priority=3)
    return True, message


def restore_at_shrine(state: GameState) -> tuple[bool, str]:
    from .actions import _advance_world
    from .world import distance

    if state.location != "region" or state.courier is None or not state.courier.known_spells:
        return False, magic_text("magic.shrine.invalid")
    entrance = state.region.landmarks.get("cave_entrance")
    if entrance is None or distance(state.position, entrance) > 1:
        return False, magic_text("magic.shrine.entrance")
    if any(threat.status in {"watching", "engaged"} and distance(state.position, threat.position) <= 5
           for threat in state.combatants):
        return False, magic_text("magic.shrine.threat")
    if state.courier.mana >= state.courier.max_mana:
        return False, magic_text("magic.rest.full")
    key = f"shrine:{state.courier.id}"
    today = state.world_time // 36
    if state.region.changes.get(key) == today:
        return False, magic_text("magic.shrine.used")
    courier = state.courier
    state.region.changes[key] = today
    before = courier.mana
    courier.mana = min(courier.max_mana, before + 3)
    restored = courier.mana
    _advance_world(state, steps=3)
    message = magic_format("magic.shrine.result", courier=courier.name, before=before, restored=restored)
    state.add_message(message, priority=3)
    return True, message


if len(SPELLS) != 24:
    raise RuntimeError("spellcraft requires twenty-four authored spells")
