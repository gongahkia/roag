"""Sparse action-clock materials and shared physical exposure, not fluid physics."""

from __future__ import annotations

import copy
import heapq

from .state import GameState, Item, MaterialCell, Person, Position, Threat

MAX_CELLS = 512
TURN_BUDGET = 64
CHUNK_SIZE = 8
FLAMMABLE = {"reeds", "timber", "cloth", "resin", "oil", "charcoal"}
MATERIALS = FLAMMABLE | {"soil", "stone", "lime", "ash", "salt"}
COATINGS = {"", "salt", "lime", "ash", "resin", "oil", "wet"}
VERBS = ("ignite", "extinguish", "pour", "cut", "brace", "lever", "dig", "break", "push", "pull", "redirect")


def key(point: Position) -> str:
    return f"{point.x},{point.y},{point.z}"


def point_at(coordinate: str) -> Position:
    return Position(*map(int, coordinate.split(",")))


def fields(state: GameState) -> dict[str, MaterialCell]:
    return state.region.materials if state.location == "region" else state.vessel_materials


def material_at(state: GameState, point: Position) -> str:
    from .world import base_tile

    cell = fields(state).get(key(point))
    if cell:
        return cell.material
    if state.location == "region" and any(c.position == point for c in state.region.containers):
        return "timber"
    tile = base_tile(state, point)
    if tile in {"T", '"', ";"}:
        return "reeds"
    if tile in {"d", "+", "=", "&"}:
        return "timber"
    if tile in {"#", "r", "q", "%"}:
        return "stone"
    return "soil"


def ensure_cell(state: GameState, point: Position) -> MaterialCell | None:
    from .world import base_tile

    cells = fields(state)
    coordinate = key(point)
    if coordinate in cells:
        return cells[coordinate]
    if len(cells) >= MAX_CELLS or base_tile(state, point) == " ":
        return None
    cell = MaterialCell(material=material_at(state, point))
    if base_tile(state, point) in {"~", ","}:
        cell.water = 2
        cell.fluid = "salt" if state.active_region_id == "greywash" else "fresh"
    cells[coordinate] = cell
    return cell


def inspect_material(state: GameState, point: Position) -> list[str]:
    from .world import base_tile
    from .chemistry import predicted_reactions
    cell = fields(state).get(key(point), MaterialCell(material=material_at(state, point)))
    return [
        f"FACT {point.x},{point.y} z{point.z:+d}: {cell.material}; coating {cell.coating or 'none'}.",
        f"Water {cell.water}/3 {cell.fluid}; {'ice' if cell.ice else 'liquid'}; fire {cell.fire}/3; smoke {cell.smoke}/4.",
        f"Support {cell.support}/3; " + (f"COLLAPSE warned for action {cell.collapse_due}." if cell.collapse_due else "no collapse currently scheduled."),
        f"Mixture: {cell.reagents or 'none'}; next reaction: {', '.join(predicted_reactions(cell.reagents, cell)) or 'none known'}.",
        "PREDICTION Water extinguishes; smoke rises/drifts; weakened supports fall after warning.",
        *(["FACT Loose cover (%) remains passable and turns low shots; height or arcing weapons can answer it."] if base_tile(state, point) == "%" else []),
        "Handling takes one action. Inspection/cancellation takes none. Tools and finite supplies are checked before commitment.",
    ]


def material_glyph(state: GameState, point: Position) -> str | None:
    cell = fields(state).get(key(point))
    if cell is None:
        return None
    if cell.fire:
        return "f"
    if cell.reagents:
        from .chemistry import predicted_reactions

        return "!" if predicted_reactions(cell.reagents, cell) else "o"
    if cell.collapse_due:
        return "%"
    if cell.smoke >= 2:
        return "s"
    if cell.ice:
        return "_"
    if cell.water:
        return ","
    return None


def affect_body(state: GameState, body: Person | Threat | Item, reaction: str, severity: int, origin: Position) -> None:
    """One material event has body-, actor- and possession-specific consequences."""
    from .inventory import add_status, item_spec, worn_tags

    if isinstance(body, Item):
        from .workshop import attached, effective_spec

        spec = effective_spec(state, body)
        vulnerable = spec.category in {"cargo", "consumable", "passive"} or "absorbent" in spec.tags
        abrasive = reaction in {"salt", "lime"} and ("metal" in spec.tags or spec.category == "weapon")
        resisted = (
            (reaction == "salt" and "saltproof" in spec.tags)
            or (reaction == "lime" and "limeproof" in spec.tags)
            or (
                reaction == "lime"
                and "limewash seal" in state.carried_passives
                and body.owner_id == state.active_courier_id
            )
        )
        if not resisted and (reaction in {"fire", "debris"} or (reaction == "water" and vulnerable) or abrasive):
            fire_wear = 2 if "heatproof" in spec.tags else 12 if "resin-coated" in spec.tags else 8
            wear = severity * (fire_wear if reaction == "fire" else 3)
            body.condition = max(0, body.condition - wear)
            for part in attached(state, body):
                part.condition = max(0, part.condition - wear)
            if body.condition == 0 and body.location not in {"lost", "destroyed"}:
                body.location, body.owner_id = "destroyed", None
                state.remember(f"{spec.name} ({body.id}) was destroyed by {reaction} at {key(origin)}.")
                state.add_message(f"{spec.name} is destroyed by {reaction}; its loss is recorded.", priority=3)
                from .inventory import sync_legacy_load
                sync_legacy_load(state)
        return
    if isinstance(body, Threat):
        if body.status in {"defeated", "disabled", "retreated", "evaded", "negotiated"}:
            return
        if reaction in {"fire", "debris"}:
            from .enemy_equipment import harm_enemy

            harm_enemy(
                state, body, severity, f"material {reaction}",
                damage_kind="blunt" if reaction == "debris" else "cut",
            )
            body.morale -= 1
            if body.health:
                body.intent = f"caught by {reaction}; seeking clear ground"
            if reaction == "fire":
                body.conditions["burning"] = max(3, body.conditions.get("burning", 0))
            if body.health == 0:
                state.remember(f"{body.name} fell to {reaction} at {key(origin)}; possessions remain physical.")
        else:
            condition = {
                "water": "wet", "smoke": "smoking", "salt": "salt-coated",
                "lime": "lime-coated", "ash": "ash-coated",
            }.get(reaction)
            if condition:
                body.conditions[condition] = max(
                    4 if reaction != "smoke" else 3,
                    body.conditions.get(condition, 0),
                )
            if reaction == "water":
                from .calendar import calendar_at
                from .vessel_refits import installed

                if calendar_at(state).season == "winter" and not (
                    state.location == "jomon" and installed(state, "winter-hatch-felt")
                ):
                    body.conditions["chilled"] = max(
                        5, body.conditions.get("chilled", 0)
                    )
            body.aimed_at = None
            body.intent = f"{reaction} breaks its prepared lane"
        return
    if body.id == state.active_courier_id:
        from .actions import apply_damage

        if reaction in {"fire", "debris"}:
            protection = int(reaction == "fire" and "ember cloth" in state.carried_passives)
            if reaction == "fire" and "heatproof" in worn_tags(state, ("torso",)):
                from .inventory import degrade_armour
                protection = max(1, protection)
                degrade_armour(state, "torso", 6)
                state.add_message("The kiln apron takes the heat; its protective face wears. Leave the fire before it fails.", priority=3)
            if severity > protection:
                result = apply_damage(state, severity - protection, f"material {reaction} at {key(origin)}")
                state.add_message(result, priority=3)
        elif reaction == "smoke":
            if "smoke-filter" not in worn_tags(state):
                add_status(state, "smoke-inhalation", "dense material smoke", 4, "guard and ranged reach falter; reach clear air")
                state.aimed_target = None
        elif reaction == "water":
            if "weatherproof" not in worn_tags(state):
                add_status(state, "wet", "standing in flowing water", 4, "absorbent armour burdens the load; leave water to dry")
            from .calendar import calendar_at
            from .vessel_refits import installed

            if (
                calendar_at(state).season == "winter"
                and "warm" not in worn_tags(state)
                and "winter-juniper" not in state.drink_effects
                and not (state.location == "jomon" and installed(state, "winter-hatch-felt"))
            ):
                add_status(state, "chilled", "winter floodwater", 8, "aim, treatment, and recovery are slower")
        elif reaction in {"salt", "lime"}:
            protection = "saltproof" if reaction == "salt" else "limeproof"
            sealed = reaction == "lime" and "limewash seal" in state.carried_passives
            if protection not in worn_tags(state) and not sealed:
                add_status(state, f"{reaction}-grit", f"{reaction} carried in local water", 4, "guard and ranged reach weaken; leave the slurry and allow four actions to clear")
                state.aimed_target = None
    elif reaction in {"fire", "debris"}:
        # Routine named-adult simulation cannot silently kill an off-duty person.
        body.health = max(2, body.health - severity)
        body.injury = "scorched hands" if reaction == "fire" else "bruised legs"
        body.injuries["hands" if reaction == "fire" else "legs"] = body.injury
        memory = f"{reaction.title()} at {key(origin)} interrupted work; sought shelter."
        if memory not in body.memories:
            body.memories.append(memory)
            del body.memories[:-8]


def _expose(state: GameState, point: Position, reaction: str, severity: int) -> None:
    for threat in state.combatants:
        if threat.position == point:
            affect_body(state, threat, reaction, severity, point)
    area = f"region:{state.active_region_id}" if state.location == "region" else f"vessel:{point.z}"
    people = {person.id: person for person in [*state.household, *state.visitors, state.bartender, state.merchant]}
    for schedule in state.actor_schedules.values():
        if schedule.area == area and schedule.position == point and schedule.actor_id in people and schedule.actor_id != state.active_courier_id:
            affect_body(state, people[schedule.actor_id], reaction, severity, point)
    cargo_changed = False
    enemy_positions = {
        threat.id: threat.position for threat in state.combatants
        if threat.status in {"watching", "engaged", "dormant"}
    }
    for item in state.items:
        on_ground = item.location == "ground" and item.region_id == state.spatial_id and item.ground_position == point
        carried = state.position == point and item.owner_id == state.active_courier_id and item.location in {"pack", "readied", "secondary", "head", "torso", "arms", "hands", "legs", "feet"}
        enemy_carried = (
            item.owner_id in enemy_positions
            and enemy_positions[item.owner_id] == point
            and item.location in {"enemy", "readied", "secondary", "head", "torso", "arms", "hands", "legs", "feet"}
        )
        if on_ground or carried or enemy_carried:
            before = item.condition
            affect_body(state, item, reaction, severity, point)
            cargo_changed |= carried and item.kind.startswith("commodity:") and item.condition != before
    if cargo_changed:
        from .inventory import sync_legacy_load
        sync_legacy_load(state)
    # Courier defeat may change location and active person; resolve it last so
    # the successor's vessel state is not exposed to the old regional event.
    if state.position == point and state.courier:
        affect_body(state, state.courier, reaction, severity, point)


def _opening_below(state: GameState, point: Position) -> Position | None:
    from .world import vertical_open
    from .vessel import vessel_vertical_destination

    below = Position(point.x, point.y, point.z - 1)
    if state.location == "jomon":
        return below if vessel_vertical_destination(point) == below else None
    return below if str(below.z) in state.region.levels and vertical_open(state, below, point) else None


def advance_materials(state: GameState) -> int:
    """Advance at most 64 nearby sparse cells; idle/menu time never calls here."""
    from .calendar import calendar_at
    from .world import base_tile, position_key, vertical_open

    cells = fields(state)
    if not cells or (state.location == "jomon" and state.jomon_space == "tavern"):
        return 0
    chunks = {}
    for coordinate in sorted(cells):
        point = point_at(coordinate)
        chunks.setdefault((point.x // CHUNK_SIZE, point.y // CHUNK_SIZE, point.z), []).append(coordinate)
    cx, cy = state.position.x // CHUNK_SIZE, state.position.y // CHUNK_SIZE
    active = sorted(coordinate for (x, y, z), coordinates in chunks.items()
                    if abs(x - cx) <= 3 and abs(y - cy) <= 3 and abs(z - state.position.z) <= 1
                    for coordinate in coordinates)
    if not active:
        return 0
    initial_place = (state.location, state.active_region_id)
    cursor = state.region.material_cursor if state.location == "region" else state.world_time * TURN_BUDGET
    selected = (active + active)[cursor % len(active):cursor % len(active) + min(TURN_BUDGET, len(active))]
    if state.location == "region":
        state.region.material_cursor = (cursor + len(selected)) % len(active)
    rain = "rain" in state.weather or "squall" in state.weather
    winter = calendar_at(state).season == "winter"
    wind = 1 if (state.world_time // 12 + sum(map(ord, state.seed))) % 2 else -1
    collapse_queue = []
    for coordinate in selected:
        point, cell = point_at(coordinate), cells[coordinate]
        if rain and point.z >= 0 and cell.material == "soil" and state.world_time % 5 == 0:
            cell.water = min(3, cell.water + 1)
        if cell.water:
            cell.ice = winter and point.z >= 0 and cell.water == 1 and cell.fluid != "salt" and not cell.fire
            if cell.fire:
                cell.fire, cell.smoke = 0, min(4, cell.smoke + 1)
                state.add_message(f"Water quenches fire at {coordinate}; steam veils the ground.", priority=3)
            if cell.material == "soil" and state.location == "region" and base_tile(state, point) in {".", ";", ",", "m"}:
                state.region.tile_changes[coordinate] = "m"
            if cell.material == "lime":
                cell.smoke = max(2, cell.smoke)
                cell.coating = "lime"
            if not cell.ice:
                _expose(state, point, "water", cell.water)
                if initial_place != (state.location, state.active_region_id):
                    return len(selected)
                if cell.fluid == "salt" or cell.coating == "lime":
                    _expose(state, point, "lime" if cell.coating == "lime" else "salt", 1)
                destination = _opening_below(state, point)
                if destination is None and cell.water > 1:
                    neighbours = [Position(point.x + dx, point.y + dy, point.z) for dx, dy in ((wind, 0), (0, 1), (-wind, 0), (0, -1))]
                    destination = next((p for p in neighbours if base_tile(state, p) in {",", "m", ";", ".", "O"} and fields(state).get(key(p), MaterialCell()).water < cell.water - 1), None)
                if destination:
                    other = ensure_cell(state, destination)
                    if other and other.water < 3:
                        cell.water -= 1
                        other.water += 1
                        other.fluid = cell.fluid
        if cell.ice and not winter:
            cell.ice = False
            state.add_message(f"Thaw loosens the thin ice at {coordinate}.", priority=2)
        if cell.fire:
            if rain and point.z >= 0:
                cell.fire = max(0, cell.fire - 1)
                cell.coating = "wet"
            if cell.fire:
                _expose(state, point, "fire", 1)
                if initial_place != (state.location, state.active_region_id):
                    return len(selected)
                cell.fuel = max(0, cell.fuel - 1)
                cell.smoke = min(4, cell.smoke + 2)
                neighbour = Position(point.x + wind, point.y, point.z)
                if cell.fuel and material_at(state, neighbour) in FLAMMABLE:
                    other = ensure_cell(state, neighbour)
                    if other and not other.water and not other.fire and other.coating != "wet":
                        other.fire, other.fuel = 1, 3
                        state.add_message(f"Fire catches dry {other.material} downwind at {key(neighbour)}.", priority=3)
                if cell.material == "timber":
                    cell.support = max(0, cell.support - 1)
                if not cell.fuel:
                    cell.fire, cell.material, cell.coating = 0, "ash", "ash"
                    if state.location == "region" and base_tile(state, point) in {"T", '"', ";"}:
                        state.region.tile_changes[coordinate] = "."
        if cell.smoke:
            if cell.smoke >= 2:
                _expose(state, point, "smoke", 1)
                state.smoke[coordinate] = max(state.smoke.get(coordinate, 0), 2)
            above = Position(point.x, point.y, point.z + 1)
            rises = vertical_open(state, point, above)
            destination = above if rises else Position(point.x + wind, point.y, point.z)
            if cell.smoke >= 2 and base_tile(state, destination) not in {" ", "#"}:
                other = ensure_cell(state, destination)
                if other:
                    other.smoke = min(4, other.smoke + 1)
            cell.smoke = max(0, cell.smoke - 1)
        if cell.reagents:
            from .chemistry import react_cell

            reaction = react_cell(state, point, cell)
            if reaction:
                state.add_message(f"{reaction.title()} reacts at {coordinate}; shared ground and nearby bodies change.", priority=3)
                if initial_place != (state.location, state.active_region_id):
                    return len(selected)
        if cell.support == 0 and not cell.collapse_due:
            cell.collapse_due = state.world_time + 2
            state.add_message(f"Support cracks at {coordinate}; debris will fall in two actions. Brace it or leave.", priority=3)
        if cell.collapse_due:
            heapq.heappush(collapse_queue, (cell.collapse_due, coordinate))
    while collapse_queue and collapse_queue[0][0] <= state.world_time:
        _, coordinate = heapq.heappop(collapse_queue)
        cell, point = cells[coordinate], point_at(coordinate)
        below = _opening_below(state, point)
        _expose(state, below or point, "debris", 2)
        if initial_place != (state.location, state.active_region_id):
            return len(selected)
        cell.collapse_due, cell.support, cell.material = 0, 3, "stone"
        if state.location == "region":
            protected = set(state.region.landmarks.values()) | {c.position for c in state.region.containers}
            protected |= {p for link in state.region.vertical_links for p in (link.first, link.second)}
            if point not in protected:
                state.region.tile_changes[coordinate] = "O" if point.z > 0 else "%"
            if point == state.position and point.z > 0:
                from .actions import _fall
                state.add_message(_fall(state), priority=3)
        else:
            state.vessel_integrity = max(0, state.vessel_integrity - 1)
        state.remember(f"A damaged support collapsed at {coordinate}; debris changed the crossing.")
        state.add_message(f"The warned support falls at {coordinate}; debris strikes the opening.", priority=3)
    return len(selected)


def handle_material(state: GameState, verb: str, point: Position) -> tuple[bool, str]:
    cells = fields(state)
    before = copy.deepcopy(cells)
    result = _handle_material(state, verb, point)
    if not result[0]:
        cells.clear()
        cells.update(before)
    return result


def _handle_material(state: GameState, verb: str, point: Position) -> tuple[bool, str]:
    from .actions import _advance_world, emit_sound
    from .world import base_tile, distance, is_walkable, line_of_sight

    if verb not in VERBS or distance(state.position, point) > 1 or not line_of_sight(state, state.position, point):
        return False, "Choose a visible material within one pace."
    existing = fields(state).get(key(point))
    material = material_at(state, point)
    pitch_key = f"pitch_cup:{state.expedition_count}"
    measured_pitch = (
        verb == "ignite"
        and "pitch cup" in state.carried_passives
        and not state.vessel_changes.get(pitch_key)
    )
    if verb == "ignite" and (
        material not in FLAMMABLE
        or (state.lamp_oil <= 0 and not measured_pitch)
        or (existing and existing.water)
    ):
        return False, "Ignition needs dry fuel and one finite measure of lamp oil."
    learned_brace = verb == "brace" and bool({"mill hearing", "bell interval"} & set(state.courier.learned_techniques))
    from .practices import has_effect as has_practice_effect

    practice_work = (
        verb == "brace" and has_practice_effect(state, "material-brace")
        or verb == "dig" and has_practice_effect(state, "material-dig")
        or verb == "cut" and has_practice_effect(state, "material-cut")
    )
    from .workshop import active_part

    heel = active_part(state, "iron heel") if verb in {"brace", "lever", "break"} else None
    spade_work = state.weapon == "spade" and verb in {"dig", "cut"}
    special_break = verb == "break" and bool(
        existing
        and (
            (existing.ice and "ice awl" in state.carried_passives)
            or (existing.fire and "fire rake tooth" in state.carried_passives)
        )
    )
    from .legendary import permits_material
    if verb in {"brace", "lever", "break", "cut", "dig"} and not (heel or learned_brace or practice_work or spade_work or special_break or permits_material(state, verb) or state.gear == "repair tools" or state.weapon in {"hand axe", "billhook", "war hammer"} or state.courier.technique == "lever craft"):
        return False, "This work needs a cutting/levering weapon, repair tools, or Lever Craft."
    if verb in {"push", "pull"}:
        container = next((c for c in state.region.containers if c.position == point), None) if state.location == "region" else None
        if container is None or point == state.position:
            return False, "Stand beside a loose coffer to move it. Contents remain inside."
        dx, dy = point.x - state.position.x, point.y - state.position.y
        destination = state.position if verb == "pull" else Position(point.x + dx, point.y + dy, point.z)
        if not is_walkable(state, destination) or any(c.position == destination for c in state.region.containers) or destination in state.region.landmarks.values():
            return False, "That destination cannot safely hold the coffer."
    if verb in {"extinguish", "pour", "redirect"}:
        sources = [Position(state.position.x + dx, state.position.y + dy, state.position.z) for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1))]
        source = next((p for p in sources if base_tile(state, p) in {"~", ","} or fields(state).get(key(p), MaterialCell()).water), None)
        if source is None:
            return False, "Reach a water source first; no water is created from nothing."
    else:
        source = None
    cell = ensure_cell(state, point)
    if cell is None:
        return False, "This space has no accessible material or the local reaction budget is full."
    if verb == "ignite":
        if measured_pitch:
            state.vessel_changes[pitch_key] = True
        else:
            state.lamp_oil -= 1
        cell.fire, cell.fuel = 2, 7 if measured_pitch else 5
        cell.coating = "oil"
    elif verb in {"extinguish", "pour", "redirect"}:
        donor = ensure_cell(state, source)
        if donor is None or donor.water <= 0:
            return False, "The accessible measure has already been drawn."
        if source != point:
            donor.water -= 1
            cell.water = min(3, cell.water + 1)
            cell.fluid = donor.fluid
        cell.fire = 0
        cell.coating = "wet"
    elif verb == "brace":
        cell.support, cell.collapse_due = 3, 0
    elif verb == "break" and cell.ice and "ice awl" in state.carried_passives:
        cell.ice, cell.water, cell.coating = False, max(1, cell.water), "wet"
        emit_sound(state, 1, point)
    elif verb == "break" and cell.fire and "fire rake tooth" in state.carried_passives:
        cell.fire, cell.fuel, cell.material = 0, 0, "ash"
        cell.coating, cell.smoke = "ash", min(4, cell.smoke + 2)
        emit_sound(state, 2, point)
    elif verb in {"cut", "break", "lever"}:
        if cell.material not in {"timber", "stone", "reeds"}:
            return False, "There is no firm structure to work here."
        cell.support = max(0, cell.support - (2 if verb == "break" else 1))
        emit_sound(state, 4 if verb == "break" else 2, point)
    elif verb == "dig":
        cell.coating = "ash"
        cell.fire = 0
        if state.location == "region" and base_tile(state, point) in {";", "m", '"'}:
            state.region.tile_changes[key(point)] = "."
    elif verb in {"push", "pull"}:
        container.position = destination
        emit_sound(state, 2, point)
    message = f"You {verb} {cell.material} at {key(point)}; material consequences advance one action."
    if heel:
        heel.condition = max(0, heel.condition - 5)
        emit_sound(state, 2, point)
        message += " The iron heel bears the work, wears, and rings against the structure."
    _advance_world(state)
    state.add_message(message, priority=3)
    return True, message


def validate_materials(state: GameState) -> None:
    from .chemistry import validate_chemistry

    validate_chemistry(state)
    for region_id, cells in [(key, region.materials) for key, region in state.regions.items()] + [("vessel", state.vessel_materials)]:
        if len(cells) > MAX_CELLS:
            raise ValueError("sparse cell budget exceeded")
        for coordinate, cell in cells.items():
            point = point_at(coordinate)
            if region_id == "vessel":
                valid = 0 <= point.x < 64 and 0 <= point.y < 22 and point.z in {-1, 0, 1}
            else:
                region = state.regions[region_id]
                valid = 0 <= point.x < region.width and 0 <= point.y < region.height and str(point.z) in region.levels
            if not valid or cell.material not in MATERIALS or cell.coating not in COATINGS or cell.fluid not in {"fresh", "salt"}:
                raise ValueError("unknown material, coating, fluid or position")
            for name, limit in (("water", 3), ("fire", 3), ("fuel", 12), ("smoke", 4), ("support", 3)):
                value = getattr(cell, name)
                if type(value) is not int or not 0 <= value <= limit:
                    raise ValueError(f"invalid {name}")
            if (type(cell.collapse_due) is not int or cell.collapse_due < 0
                    or type(cell.reaction_due) is not int or cell.reaction_due < 0
                    or type(cell.ice) is not bool):
                raise ValueError("invalid material event")
