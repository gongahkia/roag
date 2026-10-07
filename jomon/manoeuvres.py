"""Twelve deliberate applications of already learned regional practices."""

from __future__ import annotations

from dataclasses import dataclass

from .state import GameState, Position, Threat
from .progression_presentation import manoeuvre_display, practice_display_name, progression_format, progression_text


@dataclass(frozen=True)
class Manoeuvre:
    id: str
    practice: str
    mode: str
    @property
    def name(self) -> str: return manoeuvre_display(self.id, "name")
    @property
    def setup(self) -> str: return manoeuvre_display(self.id, "setup")
    @property
    def counter(self) -> str: return manoeuvre_display(self.id, "counter")
    @property
    def effect(self) -> str: return manoeuvre_display(self.id, "effect")


MANOEUVRES = tuple(Manoeuvre(*row) for row in (
    ("braced-advance", "practice.bank_water_cadence", "target"), ("quiet-crossing", "practice.field_rill_measure", "field"),
    ("hook-and-pass", "practice.wreck_title_hold", "target"), ("shield-bind", "practice.span_watch_stance", "guard"),
    ("smoke-takedown", "practice.ash_refuge_breathing", "target"), ("porter-shove", "practice.island_porter_relay", "target"),
    ("high-cast", "practice.ridge_sounding_line", "target"), ("ice-feint", "practice.winter_braid_reading", "guard"),
    ("flood-turn", "practice.siltgate_hand", "field"), ("firebreak-cut", "practice.living_firebreak", "field"),
    ("support-set", "practice.peat_brace_seating", "guard"), ("controlled-withdrawal", "practice.two_span_withdrawal", "guard"),
))

BY_ID = {row.id: row for row in MANOEUVRES}
BY_PRACTICE = {row.practice: row for row in MANOEUVRES}


def known(state: GameState, mode: str | None = None) -> list[Manoeuvre]:
    from .practices import learned_practice_ids
    learned = learned_practice_ids(state.courier) if state.courier else set()
    return [row for row in MANOEUVRES if row.practice in learned and (mode is None or row.mode == mode)]


def _target(state: GameState, target_id: str | None) -> Threat | None:
    active = [a for a in state.combatants if a.status in {"watching", "engaged"}]
    if target_id:
        return next((a for a in active if a.id == target_id), None)
    return min(active, key=lambda a: (abs(a.position.x-state.position.x)+abs(a.position.y-state.position.y), a.id), default=None)


def _cell(state: GameState, point: Position):
    from .materials import fields, key
    return fields(state).get(key(point))


def _ammo_ready(state: GameState) -> bool:
    from .actions import RANGED_WEAPONS
    from .inventory import physical_ammunition
    from .inventory import WEAPON_AMMUNITION
    if state.weapon not in RANGED_WEAPONS:
        return False
    ammunition = WEAPON_AMMUNITION.get(state.weapon or "", "")
    return state.weapon == "throwing axe" or bool(ammunition and physical_ammunition(state, ammunition))


def status(state: GameState, manoeuvre_id: str, target_id: str | None = None) -> tuple[bool, str]:
    row = BY_ID[manoeuvre_id]
    if row not in known(state):
        return False, progression_format("progression.manoeuvre.status.unlearned", practice=practice_display_name(row.practice))
    if not state.combat_active:
        return False, progression_text("progression.manoeuvre.status.danger")
    target = _target(state, target_id)
    gap = 99 if target is None else max(abs(target.position.x-state.position.x), abs(target.position.y-state.position.y))
    from .world import base_tile
    from .materials import fields, key, material_at
    adjacent = [Position(state.position.x+dx, state.position.y+dy, state.position.z) for dx, dy in ((0,-1),(1,0),(0,1),(-1,0))]
    if row.id == "braced-advance":
        okay = state.weapon in {"spear", "pike", "boar spear", "glaive", "billhook", "quarterstaff", "staff"} and target is not None and gap <= 3
    elif row.id == "quiet-crossing":
        okay = state.location == "region"
    elif row.id == "hook-and-pass":
        okay = state.weapon in {"billhook", "boat hook", "chain hook", "hooked javelin"} and target is not None and gap <= 2
    elif row.id == "shield-bind":
        okay = (state.gear == "buckler" or state.weapon == "shield and hanger") and target is not None and gap <= 1
    elif row.id == "smoke-takedown":
        smoke = any((_cell(state, p) and _cell(state, p).smoke >= 2) for p in (state.position, target.position) if target)
        okay = target is not None and gap <= 1 and smoke
    elif row.id == "porter-shove":
        from .inventory import load_state
        okay = target is not None and gap <= 1 and load_state(state) in {"laden", "encumbered", "overloaded"}
    elif row.id == "high-cast":
        okay = target is not None and target.position.z < state.position.z and _ammo_ready(state)
    elif row.id == "ice-feint":
        cell = fields(state).get(key(state.position))
        okay = target is not None and gap <= 3 and bool((cell and cell.ice) or base_tile(state, state.position) == "_")
    elif row.id == "flood-turn":
        okay = any((cell := fields(state).get(key(p))) is not None and cell.water and cell.fluid == "fresh" for p in adjacent)
    elif row.id == "firebreak-cut":
        okay = any((cell := fields(state).get(key(p))) is not None and cell.fire and material_at(state, p) in {"reeds", "timber", "resin"} for p in adjacent)
    elif row.id == "support-set":
        okay = any((cell := fields(state).get(key(p))) is not None and (cell.support < 3 or cell.collapse_due) for p in [state.position, *adjacent])
    else:
        okay = state.weapon in {"spear", "pike", "boar spear", "glaive", "billhook", "quarterstaff", "staff"} and target is not None and gap <= 4
    return (True, progression_text("progression.manoeuvre.status.ready")) if okay else (False, row.setup)


def lines(state: GameState, mode: str | None = None, target_id: str | None = None) -> list[str]:
    rows = known(state, mode)
    if not rows:
        return [progression_text("progression.manoeuvre.lines.none"), progression_text("progression.manoeuvre.lines.guidance")]
    material = [progression_text("progression.manoeuvre.lines.intro")]
    for index, row in enumerate(rows):
        ready, reason = status(state, row.id, target_id)
        status_text = progression_text("progression.target.ready") if ready else progression_format("progression.target.needs", reason=reason)
        material.extend((progression_format("progression.manoeuvre.lines.row", index=index + 1, manoeuvre=row.name, status=status_text), progression_format("progression.manoeuvre.lines.effect", effect=row.effect, counter=row.counter)))
    return material + [progression_text("progression.manoeuvre.lines.footer")]


def choices(state: GameState, mode: str | None = None, target_id: str | None = None) -> list[tuple[str, str, str, bool, str]]:
    result = []
    keys = "123456789abc"
    for key_name, row in zip(keys, known(state, mode)):
        ready, reason = status(state, row.id, target_id)
        result.append((key_name.upper(), row.name, "commitment", ready, reason))
    return result


def _safe_step(state: GameState, dx: int, dy: int) -> Position | None:
    from .world import is_walkable
    point = Position(state.position.x+dx, state.position.y+dy, state.position.z)
    return point if is_walkable(state, point) else None


def perform(state: GameState, manoeuvre_id: str, target_id: str | None = None) -> tuple[bool, str, int]:
    ready, reason = status(state, manoeuvre_id, target_id)
    if not ready:
        return False, progression_format("progression.manoeuvre.perform.failed", manoeuvre=BY_ID[manoeuvre_id].name, reason=reason), 0
    row, target = BY_ID[manoeuvre_id], _target(state, target_id)
    from .materials import ensure_cell, fields, key
    from .world import is_walkable
    from .enemy_equipment import harm_enemy

    if target:
        dx = 0 if target.position.x == state.position.x else (1 if target.position.x > state.position.x else -1)
        dy = 0 if target.position.y == state.position.y else (1 if target.position.y > state.position.y else -1)
    else:
        dx, dy = 1, 0
    if row.id == "braced-advance":
        step = _safe_step(state, dx, dy)
        if step and step != target.position:
            state.position = step
        state.guarded_step = True
        target.morale -= 1
    elif row.id == "quiet-crossing":
        candidates = [_safe_step(state, *offset) for offset in ((1,0),(0,-1),(0,1),(-1,0))]
        step = next((p for p in candidates if p), None)
        if step:
            state.position = step
        state.noise = max(0, state.noise-2)
    elif row.id == "hook-and-pass":
        old = target.position
        beyond = Position(target.position.x+dx, target.position.y+dy, target.position.z)
        # A blocked receiving lane is the manoeuvre's disclosed terrain/ally
        # counter.  Do not use the pathfinder's actor-ignoring probe here: the
        # committed reducer must never stack two physical actors in one cell.
        if is_walkable(state, beyond):
            target.position = beyond
            state.position = old
        target.morale -= 1
    elif row.id == "shield-bind":
        target.aimed_at = None
        target.reload_turns += 1
        target.morale -= 1
        state.guarded_step = True
    elif row.id == "smoke-takedown":
        from .skill_tree import manoeuvre_damage_source
        harm_enemy(state, target, 2, manoeuvre_damage_source(row.id), damage_kind="blunt")
        target.morale -= 1
        state.noise = max(0, state.noise-1)
    elif row.id == "porter-shove":
        beyond = Position(target.position.x+dx, target.position.y+dy, target.position.z)
        if is_walkable(state, beyond):
            target.position = beyond
        target.morale -= 1
    elif row.id == "high-cast":
        from .inventory import consume_ammunition
        from .inventory import WEAPON_AMMUNITION
        ammunition = WEAPON_AMMUNITION.get(state.weapon or "", "")
        if ammunition:
            consume_ammunition(state, ammunition)
        from .skill_tree import manoeuvre_damage_source
        harm_enemy(state, target, 2, manoeuvre_damage_source(row.id), damage_kind="pierce")
        target.aimed_at = None
        state.noise += 1
    elif row.id == "ice-feint":
        target.aimed_at = target.marked_position = None
        target.morale -= 2
        state.guarded_step = True
    elif row.id == "flood-turn":
        adjacent = [Position(state.position.x+ox, state.position.y+oy, state.position.z) for ox, oy in ((0,-1),(1,0),(0,1),(-1,0))]
        source = next(p for p in adjacent if (fields(state).get(key(p)) and fields(state)[key(p)].water and fields(state)[key(p)].fluid == "fresh"))
        destination = next((p for p in adjacent if p != source and is_walkable(state, p, ignore_threat=True)), state.position)
        source_cell, destination_cell = fields(state)[key(source)], ensure_cell(state, destination)
        source_cell.water -= 1
        if destination_cell:
            destination_cell.water, destination_cell.fluid = min(3, destination_cell.water+1), "fresh"
    elif row.id == "firebreak-cut":
        adjacent = [Position(state.position.x+ox, state.position.y+oy, state.position.z) for ox, oy in ((0,-1),(1,0),(0,1),(-1,0))]
        point = next(p for p in adjacent if fields(state).get(key(p)) and fields(state)[key(p)].fire)
        cell = fields(state)[key(point)]
        cell.fire, cell.fuel = 0, 0
        state.noise += 1
    elif row.id == "support-set":
        adjacent = [state.position, *(Position(state.position.x+ox, state.position.y+oy, state.position.z) for ox, oy in ((0,-1),(1,0),(0,1),(-1,0)))]
        point = next(p for p in adjacent if fields(state).get(key(p)) and (fields(state)[key(p)].support < 3 or fields(state)[key(p)].collapse_due))
        cell = fields(state)[key(point)]
        cell.support, cell.collapse_due = 3, 0
        state.guarded_step = True
    else:
        step = _safe_step(state, -dx, -dy)
        if step:
            state.position = step
        target.morale -= 1
        state.guarded_step = True
    state.vessel_changes[f"manoeuvre:last:{row.id}"] = state.world_time
    state.remember(progression_format("progression.manoeuvre.memory", courier=state.courier.name, manoeuvre=row.name, effect=row.effect))
    return True, progression_format("progression.manoeuvre.perform.result", manoeuvre=row.name, effect=row.effect), 1


def validate_manoeuvres() -> None:
    from .practices import PRACTICES
    if len(MANOEUVRES) != 12 or len(BY_ID) != 12 or len(BY_PRACTICE) != 12:
        raise ValueError("exactly twelve distinct active manoeuvres are required")
    if not set(BY_PRACTICE) <= set(PRACTICES):
        raise ValueError("every manoeuvre needs a production practice")
    if {row.mode for row in MANOEUVRES} != {"target", "guard", "field"} :
        raise ValueError("manoeuvres need all three contexts, setup, counter, and effect")


def validate_manoeuvre_state(state: GameState) -> None:
    for key_name, value in state.vessel_changes.items():
        if not key_name.startswith("manoeuvre:last:"):
            continue
        manoeuvre_id = key_name.split("manoeuvre:last:", 1)[1]
        if manoeuvre_id not in BY_ID or not isinstance(value, int) or value < 0:
            raise ValueError("invalid active-mastery record")


validate_manoeuvres()
