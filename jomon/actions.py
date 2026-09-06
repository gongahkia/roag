"""Direct deterministic actions for preparation, rooms, trade, and danger."""

from __future__ import annotations

from dataclasses import dataclass

from .content import COMMODITIES, DISCOVERIES, GEAR, MERCHANT_ITEMS, RELICS, SUPPORTS, WEAPONS
from .state import CommodityStack, GameState, Person, Position, Threat, stage_rng
from .world import (
    JOMON_GANGPLANK,
    REGION_ARRIVAL,
    REGION_GANGPLANK,
    base_tile,
    build_combinations,
    capacity,
    carried_bulk,
    exit_at,
    find_tile,
    is_walkable,
    pressure,
)


@dataclass(frozen=True)
class ActionResult:
    changed: bool
    time_advanced: bool
    message: str
    overlay: str | None = None


def _result(
    state: GameState, message: str, *, time: bool = False, changed: bool = True, priority: int = 2
) -> ActionResult:
    old_band = pressure(state).band
    if time:
        state.world_time += 1
        if state.location == "region":
            state.pressure_elapsed += 1
    if message:
        state.add_message(message, priority=priority)
    new_pressure = pressure(state)
    if time and old_band != new_pressure.band and new_pressure.band in {"strained", "critical"}:
        effect = "hostiles notice farther away" if new_pressure.band == "strained" else "pursuit quickens and losses become harsher"
        state.add_message(f"Pressure is now {new_pressure.band}: {effect}.", priority=3)
    return ActionResult(changed, time, message)


def _contact_remembers(state: GameState, text: str) -> None:
    state.contact.memories.append(text)
    del state.contact.memories[:-8]


def inspect(state: GameState, subject: str = "area") -> ActionResult:
    if subject == "household":
        living = sum(person.alive for person in state.household)
        text = f"Household: {living}/6 living. Active courier: {state.courier.name if state.courier else 'not chosen'}."
    elif subject == "cargo":
        goods = ", ".join(f"{name} {stack.quantity} ({stack.condition})" for name, stack in state.vessel_cargo.items())
        text = f"Jomon hold: {goods}. {state.region.pressure}"
    else:
        text = f"{state.region.condition} {state.region.objective_text}"
    return ActionResult(False, False, text, overlay=text)


def choose_courier(state: GameState, person_id: str) -> ActionResult:
    if state.location != "jomon":
        return _result(state, "Courier selection is only possible aboard Jomon.", changed=False)
    person = next((candidate for candidate in state.household if candidate.id == person_id), None)
    if person is None or not person.alive:
        return _result(state, "That household member cannot serve as courier.", changed=False)
    state.active_courier_id = person.id
    return _result(state, f"{person.name}, {person.role}, will carry this expedition.")


def choose_weapon(state: GameState, weapon: str) -> ActionResult:
    if state.location != "jomon" or weapon not in WEAPONS or weapon not in state.owned_weapons:
        return _result(state, "That weapon is not available aboard Jomon.", changed=False)
    state.weapon = weapon
    state.crossbow_loaded = True
    return _result(state, f"Readied {WEAPONS[weapon][0]}.")


def choose_gear(state: GameState, gear: str) -> ActionResult:
    if state.location != "jomon" or gear not in GEAR or gear not in state.owned_gear:
        return _result(state, "That secondary item is not available aboard Jomon.", changed=False)
    state.gear = gear
    return _result(state, f"Packed {GEAR[gear][0]}.")


def choose_support(state: GameState, support: str) -> ActionResult:
    if state.location != "jomon" or support not in SUPPORTS:
        return _result(state, "That crew preparation is not available here.", changed=False)
    state.support = support
    state.support_spent = False
    return _result(state, f"Prepared {SUPPORTS[support][0]}.")


def choose_relic(state: GameState, relic: str | None) -> ActionResult:
    if state.location != "jomon" or (relic is not None and state.relics.get(relic, 0) <= 0):
        return _result(state, "That finite relic is not available.", changed=False)
    state.carried_relic = relic
    return _result(state, f"Carried relic: {relic or 'none'}.")


def depart(state: GameState) -> ActionResult:
    if state.location != "jomon" or state.position != JOMON_GANGPLANK:
        return _result(state, "Departure requires Jomon's gangplank.", changed=False)
    if state.courier is None or not state.courier.alive:
        return _result(state, "Choose an eligible courier at tavern C first.", changed=False)
    if state.weapon is None or state.gear is None or state.support is None:
        return _result(state, "Prepare weapon, secondary gear, and crew support at tavern C first.", changed=False)
    if state.weapon not in state.owned_weapons or state.gear not in state.owned_gear:
        return _result(state, "Part of that build was consumed or lost; prepare again at C.", changed=False)
    state.location = "region"
    state.current_room = "hearthford_quay"
    state.position = REGION_ARRIVAL
    state.expedition_count += 1
    state.pressure_elapsed = 0
    state.noise = 0
    state.carried_goods = {}
    state.support_spent = False
    state.crossbow_loaded = True
    state.merchant_present = False
    state.merchant_stock.clear()
    room = state.room
    first_visit = not room.discovered
    room.discovered = True
    state.remember(f"Expedition {state.expedition_count}: {state.courier.name} departed for Hearthford.")
    message = "You cross Jomon's gangplank into Hearthford Quay."
    if first_visit:
        message += " Exits lead north to the tally house, east to market, and south to Reedwood."
    return _result(state, message, time=True)


def _distance(left: Position, right: Position) -> int:
    return max(abs(left.x - right.x), abs(left.y - right.y))


def _step_toward(state: GameState, threat: Threat, target: Position) -> Position:
    start = threat.position
    candidates: list[Position] = []
    if target.x != start.x:
        candidates.append(Position(start.x + (1 if target.x > start.x else -1), start.y))
    if target.y != start.y:
        candidates.append(Position(start.x, start.y + (1 if target.y > start.y else -1)))
    candidates.extend((Position(start.x, start.y + 1), Position(start.x, start.y - 1)))
    occupied = {other.position for other in state.local_threats(active_only=True) if other.id != threat.id}
    for candidate in candidates:
        if candidate != state.position and candidate not in occupied and is_walkable(state, candidate, ignore_threat=True):
            return candidate
    return start


def _step_away(state: GameState, threat: Threat) -> Position:
    dx = 1 if threat.position.x >= state.position.x else -1
    dy = 1 if threat.position.y >= state.position.y else -1
    for candidate in (Position(threat.position.x + dx, threat.position.y), Position(threat.position.x, threat.position.y + dy)):
        if is_walkable(state, candidate, ignore_threat=True):
            return candidate
    return threat.position


def _lose_goods(state: GameState) -> str:
    if not state.carried_goods:
        return ""
    if "bonded cargo" in build_combinations(state) or state.support == "porter watch":
        return " The prepared cargo watch keeps the entire load attached."
    if state.gear == "cargo harness" or (state.room and state.room.changes.get("cover_moved")):
        name = sorted(state.carried_goods)[0]
        protected = state.carried_goods.pop(name)
        rest = ", ".join(state.carried_goods)
        state.carried_goods = {name: protected}
        return f" The harness preserves {name}; {rest or 'the remaining loose goods'} is lost."
    names = ", ".join(state.carried_goods)
    state.carried_goods.clear()
    if state.objective_status in {"accepted", "altered"}:
        state.objective_status = "failed"
    return f" The {names} is lost."


def _lose_gear(state: GameState) -> str:
    if not state.gear or state.support == "porter watch" or "bonded cargo" in build_combinations(state):
        return ""
    lost = state.gear
    if lost in state.owned_gear:
        state.owned_gear.remove(lost)
    state.gear = None
    return f" You leave {lost} behind."


def _successor(state: GameState, dead: Person) -> Person | None:
    living = [person for person in state.household if person.alive]
    if not living:
        return None
    living.sort(key=lambda person: (-person.relationships.get(dead.id, 0), person.id))
    return living[0]


def _return_after_defeat(state: GameState, text: str, permanent: bool) -> str:
    courier = state.courier
    if courier is None:
        return text
    loss = _lose_goods(state) + _lose_gear(state)
    if state.objective_status in {"accepted", "altered"}:
        state.objective_status = "failed"
        state.contact.disposition = max(-3, state.contact.disposition - 1)
        _contact_remembers(state, f"{courier.name} failed to return with the needed {state.region.objective_commodity}.")
    state.location, state.current_room, state.position = "jomon", None, JOMON_GANGPLANK
    if permanent:
        courier.alive, courier.health, courier.injury = False, 0, "dead"
        successor = _successor(state, courier)
        state.remember(f"{courier.name} died at {state.region.hazard}; the household remembers the loss.")
        if successor is None:
            state.active_courier_id = None
            state.world_ended = True
            return f"{text}{loss} No eligible household member survives; this world ends."
        state.active_courier_id = successor.id
        successor.relationships[courier.id] = min(3, successor.relationships.get(courier.id, 0) + 1)
        state.weapon = state.gear = state.support = None
        state.remember(f"{successor.name} succeeded {courier.name} as active courier.")
        return f"{text}{loss} {successor.name} takes up the household's work."
    courier.health = max(3, courier.max_health // 3)
    courier.injury = "deep cut"
    state.remember(f"{courier.name} escaped to Jomon with a lasting injury.")
    return f"{text}{loss} The courier reaches Jomon injured."


def apply_damage(state: GameState, amount: int, source: str) -> str:
    courier = state.courier
    if courier is None:
        return "No courier can be harmed."
    if state.support == "field care" and not state.support_spent:
        state.support_spent = True
        if courier.technique == "field binding":
            courier.health = max(1, courier.health - max(0, amount - 1))
            courier.injury = "bound wound"
            return "Field care and the healer's binding turn a grave wound into a bound injury."
        amount = max(0, amount - 2)
        if amount == 0:
            return "The prepared field care absorbs the injury."
    if amount >= courier.health and state.carried_relic == "river-glass ward" and state.relics.get("river-glass ward", 0):
        state.relics["river-glass ward"] -= 1
        if state.relics["river-glass ward"] == 0:
            del state.relics["river-glass ward"]
        state.carried_relic = None
        courier.health, courier.injury = 1, "river-glass chill"
        state.remember(f"A finite river-glass ward broke to preserve {courier.name}'s life.")
        return "The river-glass ward breaks, leaving the courier barely alive."
    was_injured = courier.injury != "none"
    courier.health = max(0, courier.health - amount)
    if courier.health > 0:
        if courier.health <= courier.max_health // 2:
            courier.injury = "bruised ribs"
        return f"{source} deals {amount} harm."
    permanent = was_injured or pressure(state).band == "critical" or "crown wheel" in source
    return _return_after_defeat(state, f"{source} overwhelms {courier.name}.", permanent)


def _line_blocked(state: GameState) -> bool:
    room = state.room
    return bool(room and (room.changes.get("shutter_closed") or room.changes.get("cover_moved")))


def _activate(threat: Threat) -> str:
    threat.status = "engaged"
    if threat.profile == "pursuer":
        threat.intent = "rushes directly toward you"
    elif threat.profile == "reach":
        threat.intent = "levels a spear and holds two paces"
    elif threat.profile == "ranged":
        threat.intent = "seeks a clear line for the crossbow"
    elif threat.profile == "animal":
        threat.intent = "scrapes the mud before a territorial charge"
    else:
        threat.intent = "sweep arm crosses the east aisle next turn"
    return f"The {threat.name} notices you: {threat.intent}."


def _threat_action(state: GameState, threat: Threat, guarded: bool) -> str:
    current = pressure(state)
    distance = _distance(state.position, threat.position)
    threat.turn += 1
    if threat.profile == "machinery":
        room = state.room
        room.changes["machinery_phase"] = threat.turn % 2
        if state.position.x < 12:
            threat.intent = "sweep arm works the east aisle; the west staging bay is safe"
            return "The mill sweep scours the east aisle; the west staging bay remains safe."
        if guarded or room.changes.get("cover_moved"):
            threat.intent = "sweep arm passes behind your cover"
            return "The mill sweep crashes past the braced cover."
        if "next turn" in threat.intent:
            threat.intent = "gears reverse; the flooded aisle will sweep next turn"
            if threat.elite:
                state.noise += 2
                loss = _lose_goods(state) if state.carried_goods else ""
                return f"The crown wheel floods and sweeps the aisle. {apply_damage(state, 3, 'The runaway crown wheel')}{loss}"
            return apply_damage(state, 2, "The mill sweep")
        threat.intent = "sweep arm crosses the east aisle next turn"
        return "The machinery reverses: the east aisle will be swept next turn."
    if threat.profile == "ranged":
        if _line_blocked(state):
            threat.intent = "shifts for a line around the cover"
            threat.position = _step_toward(state, threat, state.position)
            return f"The {threat.name} shifts for a line; the shutter or cargo blocks the shot."
        if distance <= 6:
            if "fires next turn" in threat.intent:
                threat.intent = "works the crossbow lever to reload"
                return apply_damage(state, 3 if current.band == "critical" else 2, f"The {threat.name}'s bolt")
            if "reload" in threat.intent:
                threat.intent = "aims and fires next turn"
                return f"The {threat.name} reloads, then takes readable aim."
            threat.intent = "aims and fires next turn"
            return f"The {threat.name} aims: a bolt will come next turn."
        threat.position = _step_toward(state, threat, state.position)
        return f"The {threat.name} moves to recover range."
    if threat.profile == "animal":
        if base_tile(state, state.position) == "m" and "charge" in threat.intent:
            threat.status, threat.intent = "evaded", "bogged in the marked mud channel"
            state.remember(f"{state.courier.name} used the mud channel to evade the reed boar.")
            return "The boar charges into deep mud; your positioned evasion leaves it bogged behind."
        if distance <= 1 and "charge" in threat.intent:
            threat.intent = "circles before another charge"
            return apply_damage(state, 3, "The reed boar's shoulder")
        if distance <= 3:
            threat.intent = "lowers its head and charges next turn"
            return "The reed boar lowers its head: move into mud or face the charge."
    if threat.profile == "reach":
        if distance <= 2:
            if guarded:
                threat.intent = "holds behind the spear haft"
                return "Your guard catches the spear and denies the formation its distance."
            if "thrusts next turn" in threat.intent:
                threat.intent = "recovers the spear point"
                return apply_damage(state, 3, f"The {threat.name}'s spear")
            threat.intent = "braces and thrusts next turn"
            return f"The {threat.name} braces at reach: a thrust comes next turn."
    elif distance <= 1:
        if guarded:
            threat.intent = "recoils from your guarded stance"
            threat.position = _step_away(state, threat)
            return f"Your guard turns the {threat.name} and forces space."
        if "strikes next turn" in threat.intent:
            threat.intent = "draws back after striking"
            return apply_damage(state, 4 if current.band == "critical" else 3, f"The {threat.name}'s blow")
        threat.intent = "plants their feet and strikes next turn"
        return f"The {threat.name} plants their feet: a strike is coming."
    for _ in range(current.pursuit_steps):
        threat.position = _step_toward(state, threat, state.position)
        if _distance(state.position, threat.position) <= (2 if threat.profile == "reach" else 1):
            break
    threat.intent = "pursues quickly" if current.pursuit_steps == 2 else "closes through the terrain"
    return f"The {threat.name} {threat.intent}."


def _advance_threats(state: GameState, *, guarded: bool = False) -> str:
    if state.location != "region":
        return ""
    messages: list[str] = []
    current = pressure(state)
    for threat in state.local_threats(active_only=True):
        distance = _distance(state.position, threat.position)
        if threat.status == "watching":
            if threat.profile == "machinery" or distance <= current.alert_range or (
                state.noise + current.valuables >= 5 and distance <= current.alert_range + 3
            ):
                if threat.profile == "animal" and "surveyed soft-step" in build_combinations(state) and state.noise == 0:
                    continue
                messages.append(_activate(threat))
            continue
        messages.append(_threat_action(state, threat, guarded))
        if state.location != "region":
            break
    message = " ".join(part for part in messages if part)
    if message:
        state.add_message(message, priority=3)
    return message


def _hazard_after_move(state: GameState, tile: str) -> str:
    if tile == "m":
        if state.gear == "quiet shoes" or "reed-step notes" in state.courier.learned_techniques:
            return "Your prepared footing crosses the mud without broadcasting the route."
        state.noise += 1
        return "Mud drags at your steps; noise rises."
    if tile == "%" and not state.room.changes.get("structure_stable"):
        if state.gear == "rope" or state.support == "carpenter rig" or state.courier.technique == "sure footing":
            return "Rigging or practiced footing steadies the unstable worksite."
        if pressure(state).band in {"strained", "critical"}:
            return apply_damage(state, 2, "The unstable walkway")
        return "The unstable boards flex underfoot."
    return ""


def _exit_summary(state: GameState) -> str:
    return ", ".join(f"{direction} to {state.region.rooms[exit_.target].name}" for direction, exit_ in state.room.exits.items())


def move(state: GameState, dx: int, dy: int) -> ActionResult:
    if state.world_ended or (dx == 0 and dy == 0):
        return _result(state, "No action is possible.", changed=False)
    target = Position(state.position.x + dx, state.position.y + dy)
    if not is_walkable(state, target):
        return _result(state, "That way is blocked.", changed=False)
    if state.location == "region":
        exit_ = exit_at(state, target)
        if exit_ is not None:
            old_room = state.room
            state.current_room = exit_.target
            state.position = exit_.arrival
            first = not state.room.discovered
            state.room.discovered = True
            message = f"You enter {state.room.name}."
            if first or state.support == "route survey":
                message += f" Exits: {_exit_summary(state)}."
            result = _result(state, message, time=True, priority=3)
            if old_room and any(t.profile == "animal" and t.status == "watching" for t in state.threats if t.room_id == old_room.id):
                for threat in state.threats:
                    if threat.room_id == old_room.id and threat.profile == "animal" and threat.status == "watching":
                        threat.status = "evaded"
            _advance_threats(state)
            return result
    state.position = target
    tile = base_tile(state, target)
    if state.location == "region":
        quiet = state.courier.technique == "quiet passage" or "surveyed soft-step" in build_combinations(state)
        if not quiet and state.pressure_elapsed % 5 == 4:
            state.noise += 1
    result = _result(state, "", time=True)
    if state.location == "region":
        hazard = _hazard_after_move(state, tile)
        if hazard:
            state.add_message(hazard, priority=2)
        _advance_threats(state)
    return result


def can_alter_objective(state: GameState) -> bool:
    courier = state.courier
    return bool(
        state.gear == "repair tools" or state.support in {"route survey", "carpenter rig"}
        or (courier and courier.technique == "lever craft") or "pulley key" in state.consumables
        or state.contact.disposition >= 2
    )


def decide_objective(state: GameState, decision: str) -> ActionResult:
    if state.location != "region" or state.current_room != state.region.contact_room or base_tile(state, state.position) != "M":
        return _result(state, "Meet the contact in person first.", changed=False)
    if state.objective_status not in {"unoffered", "failed"}:
        return _result(state, "The current request already has an answer.", changed=False)
    courier_name = state.courier.name
    if decision == "accept":
        state.objective_status = "accepted"
        _contact_remembers(state, f"{courier_name} accepted the difficult {state.region.objective_commodity} recovery.")
        state.remember(f"{courier_name} accepted {state.contact.name}'s material request.")
        message = f"Accepted: {state.region.objective_text}"
    elif decision == "refuse":
        state.objective_status = "refused"
        state.contact.disposition = max(-3, state.contact.disposition - 1)
        _contact_remembers(state, f"{courier_name} refused the urgent {state.region.objective_commodity} request.")
        state.remember(f"{courier_name} refused {state.contact.name}'s request.")
        message = "You refuse. The material shortage remains visible in Hearthford."
    elif decision == "alter" and can_alter_objective(state):
        state.objective_status = "altered"
        _contact_remembers(state, f"{courier_name} proposed stabilising the flood machinery instead of hauling cargo.")
        state.remember(f"{courier_name} altered the request into a mill-control repair.")
        message = "Altered: stabilise the crane-walk sluice and report back."
    else:
        return _result(state, "You lack practical preparation or trust needed to alter the request.", changed=False)
    result = _result(state, message, time=True)
    _advance_threats(state)
    return result


def _add_goods(state: GameState, name: str, quantity: int, condition: str) -> bool:
    if carried_bulk(state) + COMMODITIES[name]["bulk"] * quantity > capacity(state):
        return False
    stack = state.carried_goods.get(name)
    if stack:
        stack.quantity += quantity
        stack.condition = condition if stack.condition == condition else "mixed"
    else:
        state.carried_goods[name] = CommodityStack(quantity, condition)
    return True


def _complete_objective(state: GameState, unusual: bool) -> str:
    commodity = state.region.objective_commodity
    market = state.market[commodity]
    market.stock += 1 if unusual else state.objective_required
    late = pressure(state).band == "critical" or state.pressure_elapsed > 85
    market.demand = max(0, market.demand - (1 if late else 2))
    state.objective_status = "completed"
    state.contact.disposition = min(3, state.contact.disposition + (2 if unusual else 1))
    state.trade_credit += 4 if unusual and "prepared repair crew" in build_combinations(state) else 3
    method = "stabilised mill controls" if unusual else f"delivery of {commodity}"
    memory = f"{state.courier.name} completed the request by {method}{' after the market window narrowed' if late else ''}."
    _contact_remembers(state, memory)
    state.remember(memory)
    return f"Request completed by {method}. Demand is {market.demand}; stock {market.stock}; credit {state.trade_credit}."


def _discover(state: GameState) -> ActionResult:
    room, item = state.room, state.room.discovery
    if not item or room.changes.get("discovery_taken"):
        return _result(state, "Only signs of earlier salvage remain.", changed=False)
    room.changes["discovery_taken"] = True
    if item in RELICS:
        state.relics[item] = state.relics.get(item, 0) + 1
        detail = f"finite relic secured; {item} can be selected aboard Jomon next expedition"
    else:
        kind = DISCOVERIES[item][0]
        if kind == "technique":
            if item not in state.courier.learned_techniques:
                state.courier.learned_techniques.append(item)
            detail = f"{state.courier.name} learns {item}"
        elif kind == "trade":
            state.trade_credit += 1
            _add_goods(state, "paper", 1, "sealed")
            detail = "sealed tally yields one credit and accountable paper"
        else:
            state.consumables[item] = state.consumables.get(item, 0) + 1
            detail = f"{item} retained"
    state.remember(f"{state.courier.name} discovered {item} in {room.name}.")
    return _result(state, f"Discovery: {detail}.", time=True, priority=3)


def _control_interaction(state: GameState) -> ActionResult:
    room = state.room
    if room.id == "crane_walk":
        if state.flood_control == "lowered":
            return _result(state, "The sluice remains stabilised and the walkway firm.", changed=False)
        efficient = state.gear == "repair tools" or state.support == "carpenter rig" or "hooked rigging" in build_combinations(state)
        state.flood_control = "lowered"
        room.changes["structure_stable"] = True
        state.noise += 0 if efficient else 3
        method = "prepared rigging" if efficient else "the protesting hand-winch"
        state.remember(f"{state.courier.name} stabilised the crane-walk sluice with {method}.")
        result = _result(state, f"The {method} lowers the sluice; hazards settle and a retreat line opens.", time=True)
        _advance_threats(state)
        return result
    machinery = next((threat for threat in state.local_threats() if threat.profile == "machinery"), None)
    if machinery and machinery.status not in {"disabled", "defeated"}:
        has_method = state.gear == "repair tools" or state.support == "carpenter rig" or "pulley key" in state.consumables or "hooked rigging" in build_combinations(state)
        if not has_method:
            return _result(state, "The moving controls need tools, a key, or hooked rigging.", changed=False)
        machinery.status, machinery.intent = "disabled", "braked and lashed harmlessly"
        room.changes["machinery_disabled"] = True
        room.changes["structure_stable"] = True
        state.noise += 0 if state.gear == "repair tools" and state.support == "carpenter rig" else 1
        state.remember(f"{state.courier.name} disabled the {machinery.name} through material control work.")
        return _result(state, f"You brake and lash the {machinery.name}; the objective aisle is safe.", time=True)
    return _result(state, "The wheelhouse controls are already still.", changed=False)


def interact(state: GameState) -> ActionResult:
    tile = base_tile(state, state.position)
    if state.location == "jomon":
        if tile == "+":
            return depart(state)
        if tile == "C":
            return ActionResult(False, False, "Prepare the whole expedition at the tavern.", "tavern")
        if tile in {"L", "P"}:
            return ActionResult(False, False, "These stores are readouts; preparation is consolidated at tavern C.", "equipment")
        if tile == "H":
            return ActionResult(False, False, "Inspect Jomon's cargo and Hearthford's problem.", "hold")
        if tile == "s" and state.merchant_present:
            return ActionResult(False, False, "A visiting deck merchant opens three wrapped lots.", "merchant")
        if tile in {"T", "b", "s"}:
            return ActionResult(False, False, "Inspect the persistent household.", "household")
        return _result(state, "Nothing here needs handling.", changed=False)
    if tile == "+" and state.current_room == "hearthford_quay":
        return return_to_jomon(state)
    if tile == "M":
        if state.objective_status in {"unoffered", "failed"}:
            return ActionResult(False, False, f"{state.contact.name} explains the shortage.", "objective")
        commodity = state.region.objective_commodity
        quantity = state.carried_goods.get(commodity, CommodityStack(0, "")).quantity
        if state.objective_status == "accepted" and quantity >= state.objective_required:
            state.carried_goods[commodity].quantity -= state.objective_required
            if state.carried_goods[commodity].quantity == 0:
                del state.carried_goods[commodity]
            return _result(state, _complete_objective(state, False), time=True)
        if state.objective_status == "altered" and state.flood_control == "lowered":
            return _result(state, _complete_objective(state, True), time=True)
        return ActionResult(False, False, f"Inspect {state.contact.name}'s interests and memories.", "contact")
    if tile == "?":
        return _discover(state)
    if tile == "R":
        if state.room.changes.get("objective_taken"):
            return _result(state, "The accountable cart is empty.", changed=False)
        if state.objective_status != "accepted":
            return _result(state, "The stranded cargo is accountable to Hearthford's contact.", changed=False)
        if any(threat.profile == "machinery" and threat.status in {"watching", "engaged"} for threat in state.local_threats()):
            return _result(state, "The moving mill sweep denies safe access to the cart.", changed=False)
        commodity = state.region.objective_commodity
        if not _add_goods(state, commodity, state.objective_required, COMMODITIES[commodity]["condition"]):
            return _result(state, f"The load exceeds capacity {capacity(state)} bulk.", changed=False)
        state.room.changes["objective_taken"] = True
        state.noise += 2
        result = _result(state, f"You secure two units of {commodity}; valuables and noise rise.", time=True)
        _advance_threats(state)
        return result
    if tile == "r":
        if state.room.changes.get("resource_taken"):
            return _result(state, "Nothing useful remains here.", changed=False)
        name = state.region.opportunity_commodity
        if not _add_goods(state, name, 1, COMMODITIES[name]["condition"]):
            return _result(state, f"No room remains within {capacity(state)} bulk.", changed=False)
        state.room.changes["resource_taken"] = True
        return _result(state, f"You recover one useful unit of {name}.", time=True)
    if tile == "&":
        return _control_interaction(state)
    if tile == "D":
        closed = not bool(state.room.changes.get("shutter_closed"))
        state.room.changes["shutter_closed"] = closed
        state.noise += 0 if state.gear == "rope" else 1
        if closed:
            for threat in state.local_threats(active_only=True):
                if threat.profile == "pursuer":
                    threat.status, threat.intent = "evaded", "barred behind the gate"
                    state.remember(f"{state.courier.name} shut a physical gate to evade {threat.name}.")
        result = _result(state, f"You {'close' if closed else 'open'} the shutter; sight-lines and pursuit change.", time=True)
        _advance_threats(state)
        return result
    if tile == "O":
        if state.room.changes.get("cover_moved"):
            return _result(state, "The cargo stack already forms cover.", changed=False)
        state.room.changes["cover_moved"] = True
        state.noise += 2
        result = _result(state, "You heave the cargo stack into cover; bolts and sweep arms lose their line, but noise rises.", time=True)
        _advance_threats(state)
        return result
    return _result(state, "Nothing here needs handling.", changed=False)


def attack(state: GameState) -> ActionResult:
    if state.location != "region" or state.weapon is None:
        return _result(state, "No readied attack is possible.", changed=False)
    attack_range = 5 if state.weapon == "crossbow" else 2 if state.weapon == "spear" else 1
    candidates = [threat for threat in state.local_threats(active_only=True) if threat.status == "engaged" and _distance(state.position, threat.position) <= attack_range]
    if not candidates:
        return _result(state, "No hostile is within the weapon's reach.", changed=False)
    if state.weapon == "crossbow" and not state.crossbow_loaded:
        return _result(state, "The crossbow is unloaded; guard/reload with G.", changed=False)
    target = min(candidates, key=lambda threat: (_distance(state.position, threat.position), threat.id))
    damage = {"billhook": 2, "spear": 2, "cudgel": 1, "staff": 1, "hand axe": 3, "crossbow": 3}[state.weapon]
    if state.courier.technique == "set stance" and "next turn" in target.intent:
        damage += 1
    if state.weapon == "billhook" and target.profile in {"reach", "machinery"}:
        target.morale -= 1
        target.position = _step_toward(state, target, state.position)
    if state.weapon == "cudgel":
        target.morale -= 2
        state.noise += 1
    else:
        state.noise += 2 if state.weapon != "crossbow" else 4
    if state.weapon == "crossbow":
        state.crossbow_loaded = False
    target.health = max(0, target.health - damage)
    if target.health == 0 or (target.morale <= 0 and target.profile != "machinery"):
        target.status = "defeated" if target.health == 0 else "retreated"
        target.intent = "driven from the route"
        _contact_remembers(state, f"{state.courier.name} drove off the {target.name} in direct combat.")
        state.remember(f"{state.courier.name} defeated the {target.name} with {state.weapon}.")
        result = _result(state, f"Your {state.weapon} drives off the {target.name}.", time=True)
        _advance_threats(state)
        return result
    result = _result(state, f"Your {state.weapon} hits for {damage}; {target.name} has {target.health}/{target.max_health} health.", time=True)
    _advance_threats(state)
    return result


def guard(state: GameState) -> ActionResult:
    if state.location != "region" or not any(threat.status == "engaged" for threat in state.local_threats()):
        return _result(state, "There is no immediate danger to guard against.", changed=False)
    if state.weapon == "crossbow" and not state.crossbow_loaded:
        state.crossbow_loaded = True
        result = _result(state, "You work the windlass and reload behind a guarded posture.", time=True)
        _advance_threats(state, guarded=state.gear == "buckler")
        return result
    strong = state.gear == "buckler" or state.weapon == "staff" or state.courier.technique == "set stance"
    if strong:
        nearby = [threat for threat in state.local_threats(active_only=True) if _distance(state.position, threat.position) <= 2]
        for threat in nearby:
            if threat.profile != "machinery":
                threat.morale -= 1
    message = "You set a reinforced guard and pressure nearby morale." if strong else "You set a guarded stance and yield space deliberately."
    result = _result(state, message, time=True)
    _advance_threats(state, guarded=True)
    return result


def use_gear(state: GameState) -> ActionResult:
    if state.location != "region":
        return _result(state, "Expedition gear is used in the field.", changed=False)
    if state.carried_relic == "tide-knot charm" and state.relics.get("tide-knot charm", 0):
        state.relics["tide-knot charm"] -= 1
        if state.relics["tide-knot charm"] == 0:
            del state.relics["tide-knot charm"]
        state.carried_relic = None
        for threat in state.local_threats(active_only=True):
            threat.status, threat.intent = "evaded", "lost the route in unnaturally still water"
        state.noise = max(0, state.noise - 4)
        return _result(state, "The finite tide-knot unravels; water stills and pursuit loses your route.", time=True)
    if state.gear == "smoke pot" and "smoke pot" in state.owned_gear:
        state.owned_gear.remove("smoke pot")
        state.gear = None
        state.noise = max(0, state.noise - 3)
        for threat in state.local_threats(active_only=True):
            threat.status, threat.intent = "watching", "searches through smoke without a clear line"
        return _result(state, "The finite smoke pot breaks aim and pursuit; it is consumed.", time=True)
    animal = next((threat for threat in state.local_threats(active_only=True) if threat.profile == "animal"), None)
    if state.gear == "hooded lantern" and animal:
        animal.status, animal.intent = "evaded", "keeps its territory beyond the controlled light"
        state.remember(f"{state.courier.name} redirected the {animal.name} with controlled lamplight.")
        return _result(state, "You uncover the lamp away from your route; the territorial animal follows the light and yields passage.", time=True)
    if state.consumables.get("willow dressing", 0) and state.courier.injury != "none":
        state.consumables["willow dressing"] -= 1
        if state.consumables["willow dressing"] == 0:
            del state.consumables["willow dressing"]
        state.courier.health = min(state.courier.max_health, state.courier.health + 3)
        state.courier.injury = "treated soreness"
        return _result(state, "The finite willow dressing restores three health and binds the injury.", time=True)
    if state.consumables.get("dry smoke charge", 0) and "smoke pot" not in state.owned_gear:
        state.consumables["dry smoke charge"] -= 1
        if state.consumables["dry smoke charge"] == 0:
            del state.consumables["dry smoke charge"]
        state.owned_gear.append("smoke pot")
        return _result(state, "You pack the dry charge into a replacement smoke pot.", time=True)
    return _result(state, "No readied finite gear applies here.", changed=False)


def negotiate(state: GameState) -> ActionResult:
    if state.location != "region" or state.courier is None:
        return _result(state, "No negotiation is possible here.", changed=False)
    humans = [threat for threat in state.local_threats(active_only=True) if threat.status == "engaged" and threat.profile in {"pursuer", "reach", "ranged"} and _distance(state.position, threat.position) <= 4]
    if not humans:
        return _result(state, "No human obstruction is close enough to hear terms.", changed=False)
    has_terms = state.courier.technique == "measured terms" or state.gear == "trade seals" or state.support == "factor surety" or "paper" in state.carried_goods
    if not has_terms:
        return _result(state, "You lack witnessed seals, a factor's skill, surety, or accountable paper.", changed=False)
    if "paper" in state.carried_goods and state.gear != "trade seals":
        state.carried_goods["paper"].quantity -= 1
        if state.carried_goods["paper"].quantity == 0:
            del state.carried_goods["paper"]
    for threat in humans:
        threat.status, threat.intent = "negotiated", "accepts witnessed material terms and leaves"
    _contact_remembers(state, f"{state.courier.name} settled a route obstruction without bloodshed.")
    state.remember(f"{state.courier.name} negotiated passage from {len(humans)} route opponent(s).")
    return _result(state, "Witnessed material terms settle the obstruction without combat.", time=True)


def retreat(state: GameState) -> ActionResult:
    if state.location != "region" or not any(threat.status == "engaged" for threat in state.local_threats()):
        return _result(state, "There is no encounter to retreat from.", changed=False)
    room = state.room
    has_line = state.gear == "smoke pot" or state.flood_control == "lowered" or room.changes.get("shutter_closed")
    if room.depth >= 4 and not has_line:
        return _result(state, "The deep route is cut off; use terrain, gear, or create space first.", changed=False)
    message = _return_after_defeat(state, "You abandon the route under pursuit.", permanent=False)
    return _result(state, message, time=True)


def merchant_visit_due(seed: str, returned_expeditions: int) -> bool:
    if returned_expeditions <= 0:
        return False
    phase = stage_rng(seed, "merchant-cycle").randrange(1, 4)
    return (returned_expeditions - phase) % 3 == 0


def merchant_stock_for(state: GameState) -> list[str]:
    context_item = {"ironwork": "hand axe", "timber": "cargo harness", "charcoal": "hooded lantern"}[state.region.objective_commodity]
    outcome_item = "crossbow" if state.objective_status == "completed" else "smoke pot"
    finite = "tide-knot charm" if state.objective_status == "completed" and stage_rng(state.seed, f"merchant-stock:{state.returned_expeditions}").randrange(5) == 0 else "willow dressing"
    return list(dict.fromkeys((context_item, outcome_item, finite)))[:3]


def purchase_merchant_item(state: GameState, item: str) -> ActionResult:
    if state.location != "jomon" or not state.merchant_present or item not in state.merchant_stock:
        return _result(state, "That merchant lot is not available.", changed=False)
    cost, kind = MERCHANT_ITEMS[item]
    if state.support == "factor surety":
        cost = max(1, cost - 1)
    if state.trade_credit < cost:
        return _result(state, f"The lot needs {cost} credit; Jomon has {state.trade_credit}.", changed=False)
    state.trade_credit -= cost
    state.merchant_stock.remove(item)
    if kind == "weapon" and item not in state.owned_weapons:
        state.owned_weapons.append(item)
    elif kind == "gear" and item not in state.owned_gear:
        state.owned_gear.append(item)
    elif kind == "consumable":
        state.consumables[item] = state.consumables.get(item, 0) + 1
    elif kind == "relic":
        state.relics[item] = state.relics.get(item, 0) + 1
    state.remember(f"Jomon exchanged {cost} credit for {item} from the visiting merchant.")
    return _result(state, f"Purchased {item} for {cost} credit; it persists aboard Jomon.", time=True)


def return_to_jomon(state: GameState) -> ActionResult:
    if state.location != "region" or state.current_room != "hearthford_quay" or state.position != REGION_GANGPLANK:
        return _result(state, "Return requires the physical Hearthford gangplank.", changed=False)
    courier = state.courier
    if state.objective_status in {"accepted", "altered"}:
        state.objective_status = "failed"
        state.contact.disposition = max(-3, state.contact.disposition - 1)
        _contact_remembers(state, f"{courier.name} returned without completing the accepted request.")
        state.remember(f"{courier.name} returned without completing Hearthford's request.")
    for name, stack in state.carried_goods.items():
        vessel = state.vessel_cargo.get(name)
        if vessel:
            vessel.quantity += stack.quantity
            vessel.condition = vessel.condition if vessel.condition == stack.condition else "mixed"
        else:
            state.vessel_cargo[name] = CommodityStack(stack.quantity, stack.condition)
    state.carried_goods.clear()
    state.location, state.current_room, state.position = "jomon", None, JOMON_GANGPLANK
    state.returned_expeditions += 1
    state.merchant_present = merchant_visit_due(state.seed, state.returned_expeditions)
    state.merchant_stock = merchant_stock_for(state) if state.merchant_present else []
    state.remember(f"{courier.name} returned physically through Jomon's gangplank.")
    merchant = " A visiting merchant has tied alongside at the stores." if state.merchant_present else ""
    return _result(state, f"You cross the gangplank home. Cargo, equipment, and changed rooms persist.{merchant}", time=True)
