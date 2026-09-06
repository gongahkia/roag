"""Direct deterministic actions for preparation, travel, trade, and danger."""

from __future__ import annotations

from dataclasses import dataclass

from .content import COMMODITIES, JOMON_MAP, LOADOUTS, SUPPORTS
from .state import CommodityStack, GameState, Person, Position, Threat
from .world import (
    JOMON_GANGPLANK,
    REGION_GANGPLANK,
    base_tile,
    capacity,
    carried_bulk,
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


def _result(state: GameState, message: str, *, time: bool = False, changed: bool = True) -> ActionResult:
    if time:
        state.world_time += 1
        if state.location == "region":
            state.pressure_elapsed += 1
    if message:
        state.add_message(message)
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


def choose_loadout(state: GameState, loadout_id: str) -> ActionResult:
    selected = next((row for row in LOADOUTS if row[0] == loadout_id), None)
    if state.location != "jomon" or selected is None:
        return _result(state, "That loadout cannot be prepared here.", changed=False)
    state.loadout = loadout_id
    state.inventory = list(selected[2])
    return _result(state, f"Prepared {selected[1]}.")


def choose_support(state: GameState, support_id: str) -> ActionResult:
    selected = next((row for row in SUPPORTS if row[0] == support_id), None)
    if state.location != "jomon" or selected is None:
        return _result(state, "That crew support cannot be prepared here.", changed=False)
    state.support = support_id
    state.support_spent = False
    return _result(state, f"Prepared {selected[1]}.")


def depart(state: GameState) -> ActionResult:
    if state.location != "jomon" or state.position != JOMON_GANGPLANK:
        return _result(state, "Departure requires Jomon's gangplank.", changed=False)
    if state.courier is None or not state.courier.alive:
        return _result(state, "Choose an eligible courier at C first.", changed=False)
    if state.loadout is None or state.support is None:
        return _result(state, "Choose a loadout at L and support at P first.", changed=False)
    state.location = "region"
    state.position = REGION_GANGPLANK
    state.expedition_count += 1
    state.pressure_elapsed = 0
    state.noise = 0
    state.carried_goods = {}
    state.resource_taken = state.objective_status == "completed"
    state.opportunity_taken = False
    state.inventory = list(next(row[2] for row in LOADOUTS if row[0] == state.loadout))
    if state.relic_charges and "river-glass ward" not in state.inventory:
        state.inventory.append("river-glass ward")
    if state.threat.status not in {"defeated", "evaded", "negotiated"}:
        state.threat.status = "watching"
        state.threat.intent = "watches the route"
        state.threat.health = 4
    state.remember(f"Expedition {state.expedition_count}: {state.courier.name} departed for Hearthford.")
    return _result(state, "You cross Jomon's gangplank into Hearthford.", time=True)


def _distance(left: Position, right: Position) -> int:
    return max(abs(left.x - right.x), abs(left.y - right.y))


def _step_toward(state: GameState, start: Position, target: Position) -> Position:
    candidates: list[Position] = []
    if target.x != start.x:
        candidates.append(Position(start.x + (1 if target.x > start.x else -1), start.y))
    if target.y != start.y:
        candidates.append(Position(start.x, start.y + (1 if target.y > start.y else -1)))
    candidates.extend((Position(start.x, start.y + 1), Position(start.x, start.y - 1)))
    for candidate in candidates:
        if candidate != state.position and is_walkable(state, candidate, ignore_threat=True):
            return candidate
    return start


def _lose_goods(state: GameState) -> str:
    if not state.carried_goods:
        return ""
    if state.support == "harness":
        return " The cargo harness keeps the load attached."
    names = ", ".join(state.carried_goods)
    state.carried_goods.clear()
    if state.objective_status in {"accepted", "altered"}:
        state.objective_status = "failed"
    return f" The {names} is lost."


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
    loss = _lose_goods(state)
    if state.objective_status in {"accepted", "altered"}:
        state.objective_status = "failed"
        state.contact.disposition = max(-3, state.contact.disposition - 1)
        _contact_remembers(state, f"{courier.name} failed to return with the needed {state.region.objective_commodity}.")
    state.location = "jomon"
    state.position = JOMON_GANGPLANK
    state.loadout = None
    state.support = None
    if permanent:
        courier.alive = False
        courier.health = 0
        courier.injury = "dead"
        successor = _successor(state, courier)
        state.remember(f"{courier.name} died at {state.region.hazard}; the household remembers the loss.")
        if successor is None:
            state.active_courier_id = None
            state.world_ended = True
            return f"{text}{loss} No eligible household member survives; this world ends."
        state.active_courier_id = successor.id
        successor.relationships[courier.id] = min(3, successor.relationships.get(courier.id, 0) + 1)
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
    if state.support == "treatment" and not state.support_spent:
        state.support_spent = True
        return "The prepared field dressing prevents the injury."
    if amount >= courier.health and state.relic_charges > 0:
        state.relic_charges -= 1
        if "river-glass ward" in state.inventory:
            state.inventory.remove("river-glass ward")
        courier.health = 1
        courier.injury = "river-glass chill"
        state.remember(f"A finite river-glass ward broke to preserve {courier.name}'s life.")
        return "The river-glass ward breaks, leaving the courier barely alive."
    was_injured = courier.injury != "none"
    courier.health = max(0, courier.health - amount)
    if courier.health > 0:
        if courier.health <= courier.max_health // 2:
            courier.injury = "bruised ribs"
        return f"{source} deals {amount} harm."
    permanent = was_injured or pressure(state).band == "critical"
    return _return_after_defeat(state, f"{source} overwhelms {courier.name}.", permanent)


def _threat_turn(state: GameState, *, guarded: bool = False) -> str:
    threat = state.threat
    if state.location != "region" or threat.status in {"defeated", "evaded", "negotiated"}:
        return ""
    current_pressure = pressure(state)
    distance = _distance(state.position, threat.position)
    quiet_bypass = state.position.y >= 14 and (
        state.support == "charts" or "smoke pot" in state.inventory or state.courier.technique == "quiet passage"
    )
    if threat.status == "watching":
        alert = distance <= current_pressure.alert_range
        alert = alert or (state.noise + current_pressure.valuables >= 6 and distance <= current_pressure.alert_range + 3)
        if alert and not quiet_bypass:
            threat.status = "engaged"
            threat.intent = "advances with a cudgel raised"
            return f"The {threat.name} spots you and advances with a cudgel raised."
        if state.flood_control == "lowered" and state.position.x >= 33 and state.position.y >= 14:
            threat.status = "evaded"
            threat.intent = "lost behind the lowered flood barrier"
            state.remember(f"{state.courier.name} evaded the {threat.name} through the reed sluice.")
            return f"The lowered barrier cuts off the {threat.name}; the evasion is complete."
        return ""
    threat.turn += 1
    if distance <= 1:
        if guarded:
            threat.intent = "recoils from your guarded stance"
            threat.position = _step_toward(state, threat.position, Position(threat.position.x + (threat.position.x - state.position.x), threat.position.y + (threat.position.y - state.position.y)))
            return "Your guard turns the cudgel and forces space."
        if "strikes next turn" in threat.intent:
            threat.intent = "draws back after striking"
            return apply_damage(state, 4 if current_pressure.band == "critical" else 3, f"The {threat.name}'s cudgel")
        threat.intent = "plants their feet and strikes next turn"
        return f"The {threat.name} plants their feet: a strike is coming."
    for _ in range(current_pressure.pursuit_steps):
        threat.position = _step_toward(state, threat.position, state.position)
        if _distance(state.position, threat.position) <= 1:
            break
    threat.intent = "pursues quickly" if current_pressure.pursuit_steps == 2 else "closes along the path"
    return f"The {threat.name} {threat.intent}."


def _hazard_after_move(state: GameState, tile: str) -> str:
    if tile != "=" or state.flood_control != "lowered":
        return ""
    if "rope" in state.inventory:
        return "The rope keeps the lowered crossing secure underfoot."
    if state.support == "charts":
        return "The pilot's marks show the firm stones through the sluice."
    if pressure(state).band in {"strained", "critical"}:
        loss = _lose_goods(state) if pressure(state).band == "critical" else ""
        return f"The {state.region.hazard} catches your footing. {apply_damage(state, 2, 'The crossing')}{loss}"
    return f"You wade cautiously through the {state.region.hazard}."


def move(state: GameState, dx: int, dy: int) -> ActionResult:
    if state.world_ended or (dx == 0 and dy == 0):
        return _result(state, "No action is possible.", changed=False)
    target = Position(state.position.x + dx, state.position.y + dy)
    if not is_walkable(state, target):
        return _result(state, "That way is blocked.", changed=False)
    state.position = target
    tile = base_tile(state, target)
    if state.location == "region":
        if state.support != "charts" and state.courier.technique != "quiet passage" and state.pressure_elapsed % 4 == 3:
            state.noise += 1
        if tile in {"&", "="}:
            state.noise += 1
    result = _result(state, "You move.", time=True)
    if state.location == "region":
        hazard = _hazard_after_move(state, tile)
        threat = _threat_turn(state)
        message = " ".join(part for part in (hazard, threat) if part)
        if message:
            state.add_message(message)
            return ActionResult(True, True, message)
    return result


def can_alter_objective(state: GameState) -> bool:
    courier = state.courier
    return bool(
        state.loadout == "tools"
        or state.support == "charts"
        or (courier and courier.technique == "lever craft")
        or state.contact.disposition >= 2
    )


def decide_objective(state: GameState, decision: str) -> ActionResult:
    contact_position = find_tile(state.region.map_rows, "M")
    if state.location != "region" or state.position != contact_position:
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
        _contact_remembers(state, f"{courier_name} proposed repairing the flood control instead of hauling cargo.")
        state.remember(f"{courier_name} altered the request into a sluice repair.")
        message = "Altered: lower and repair the reed-sluice control, then report back."
    else:
        return _result(state, "You lack the preparation or trust needed to alter the request.", changed=False)
    result = _result(state, message, time=True)
    follow = _threat_turn(state)
    if follow:
        state.add_message(follow)
    return result


def _add_goods(state: GameState, name: str, quantity: int, condition: str) -> bool:
    extra = COMMODITIES[name]["bulk"] * quantity
    if carried_bulk(state) + extra > capacity(state):
        return False
    stack = state.carried_goods.get(name)
    if stack:
        stack.quantity += quantity
        stack.condition = condition
    else:
        state.carried_goods[name] = CommodityStack(quantity, condition)
    return True


def _complete_objective(state: GameState, unusual: bool) -> str:
    commodity = state.region.objective_commodity
    market = state.market[commodity]
    market.stock += state.objective_required if not unusual else 1
    late = state.pressure_elapsed > 55
    market.demand = max(0, market.demand - (1 if late else 2))
    state.objective_status = "completed"
    state.contact.disposition = min(3, state.contact.disposition + (2 if unusual else 1))
    method = "sluice repair" if unusual else f"delivery of {commodity}"
    memory = f"{state.courier.name} completed the request by {method}{' after the market window narrowed' if late else ''}."
    _contact_remembers(state, memory)
    state.remember(memory)
    return f"Request completed by {method}. Demand is now {market.demand}; stock is {market.stock}."


def interact(state: GameState) -> ActionResult:
    tile = base_tile(state, state.position)
    if state.location == "jomon":
        if tile == "+":
            return depart(state)
        if tile == "C":
            return ActionResult(False, False, "Choose the active courier.", "courier")
        if tile == "L":
            return ActionResult(False, False, "Choose a two-item loadout.", "loadout")
        if tile == "P":
            return ActionResult(False, False, "Choose crew support.", "support")
        if tile in {"H", "T", "b", "s"}:
            return inspect(state, "cargo" if tile == "H" else "household")
        return _result(state, "Nothing here needs handling.", changed=False)
    if tile == "+":
        return return_to_jomon(state)
    if tile == "M":
        if state.objective_status in {"unoffered", "failed"}:
            return ActionResult(False, False, f"{state.contact.name} explains the shortage.", "objective")
        commodity = state.region.objective_commodity
        if state.objective_status == "accepted" and state.carried_goods.get(commodity, CommodityStack(0, "")).quantity >= state.objective_required:
            state.carried_goods[commodity].quantity -= state.objective_required
            if state.carried_goods[commodity].quantity == 0:
                del state.carried_goods[commodity]
            message = _complete_objective(state, False)
            return _result(state, message, time=True)
        if state.objective_status == "altered" and state.flood_control == "lowered":
            message = _complete_objective(state, True)
            return _result(state, message, time=True)
        return inspect(state, "area")
    if state.position == state.region.resource_position and not state.resource_taken:
        if state.objective_status != "accepted":
            return _result(state, "The stranded cargo is accountable to Hearthford's contact.", changed=False)
        commodity = state.region.objective_commodity
        condition = COMMODITIES[commodity]["condition"]
        if not _add_goods(state, commodity, state.objective_required, condition):
            return _result(state, f"The load exceeds capacity {capacity(state)} bulk.", changed=False)
        state.resource_taken = True
        state.noise += 2
        result = _result(state, f"You secure two units of {commodity}; valuables and noise rise.", time=True)
        follow = _threat_turn(state)
        if follow:
            state.add_message(follow)
        return result
    if tile == "r" and not state.opportunity_taken:
        name = state.region.opportunity_commodity
        if not _add_goods(state, name, 1, COMMODITIES[name]["condition"]):
            return _result(state, f"No room remains within {capacity(state)} bulk.", changed=False)
        state.opportunity_taken = True
        return _result(state, f"You recover one useful unit of {name}.", time=True)
    if tile == "&":
        if state.flood_control == "lowered":
            return inspect(state, "area")
        state.flood_control = "lowered"
        state.noise += 0 if state.loadout == "tools" else 2
        method = "repair tools" if state.loadout == "tools" else "the heavy windlass"
        state.remember(f"{state.courier.name} lowered the reed-sluice barrier using {method}." )
        result = _result(state, f"You lower the flood barrier with {method}; the southern crossing opens.", time=True)
        follow = _threat_turn(state)
        if follow:
            state.add_message(follow)
        return result
    return _result(state, "Nothing here needs handling.", changed=False)


def attack(state: GameState) -> ActionResult:
    if state.location != "region" or state.threat.status != "engaged" or _distance(state.position, state.threat.position) > 1:
        return _result(state, "No hostile is within reach.", changed=False)
    courier = state.courier
    damage = 2 if state.loadout == "arms" else 1
    if courier.technique == "set stance" and state.threat.intent.endswith("next turn"):
        damage += 1
    state.threat.health = max(0, state.threat.health - damage)
    state.noise += 3
    if state.threat.health == 0:
        state.threat.status = "defeated"
        state.threat.intent = "driven from the route"
        _contact_remembers(state, f"{courier.name} injured and drove off the {state.threat.name}.")
        state.remember(f"{courier.name} defeated the {state.threat.name} in direct combat.")
        return _result(state, f"Your strike drives off the {state.threat.name}.", time=True)
    result = _result(state, f"You strike for {damage}; hostile health {state.threat.health}/4.", time=True)
    follow = _threat_turn(state)
    if follow:
        state.add_message(follow)
        return ActionResult(True, True, f"{result.message} {follow}")
    return result


def guard(state: GameState) -> ActionResult:
    if state.location != "region" or state.threat.status != "engaged":
        return _result(state, "There is no immediate attack to guard against.", changed=False)
    result = _result(state, "You set a guarded stance and give ground deliberately.", time=True)
    follow = _threat_turn(state, guarded=True)
    if follow:
        state.add_message(follow)
    return result


def use_gear(state: GameState) -> ActionResult:
    if state.location != "region" or "smoke pot" not in state.inventory:
        return _result(state, "No readied finite gear applies here.", changed=False)
    state.inventory.remove("smoke pot")
    state.noise = max(0, state.noise - 3)
    if state.threat.status == "engaged":
        state.threat.status = "watching"
        state.threat.intent = "searches through smoke"
        state.threat.position = Position(max(20, state.threat.position.x - 2), state.threat.position.y)
    return _result(state, "The finite smoke pot breaks line of sight and muffles your route.", time=True)


def negotiate(state: GameState) -> ActionResult:
    courier = state.courier
    if state.location != "region" or state.threat.status != "engaged" or _distance(state.position, state.threat.position) > 2:
        return _result(state, "No hostile is close enough to negotiate.", changed=False)
    has_terms = courier.technique == "measured terms" or "paper" in state.carried_goods or state.contact.disposition >= 2
    if not has_terms:
        return _result(state, "You lack credible terms, paper, or local trust.", changed=False)
    if "paper" in state.carried_goods:
        state.carried_goods["paper"].quantity -= 1
        if state.carried_goods["paper"].quantity == 0:
            del state.carried_goods["paper"]
    state.threat.status = "negotiated"
    state.threat.intent = "accepts witnessed terms and leaves"
    _contact_remembers(state, f"{courier.name} settled the route obstruction without bloodshed.")
    state.remember(f"{courier.name} negotiated passage from the {state.threat.name}.")
    return _result(state, "Material terms settle the obstruction; the route opens without combat.", time=True)


def retreat(state: GameState) -> ActionResult:
    if state.location != "region" or state.threat.status != "engaged":
        return _result(state, "There is no encounter to retreat from.", changed=False)
    if state.position.x > 18 and "smoke pot" not in state.inventory:
        return _result(state, "The route to Jomon is cut off; create space or use smoke.", changed=False)
    message = _return_after_defeat(state, "You abandon the route under pursuit.", permanent=False)
    return _result(state, message, time=True)


def return_to_jomon(state: GameState) -> ActionResult:
    if state.location != "region" or state.position != REGION_GANGPLANK:
        return _result(state, "Return requires the physical gangplank.", changed=False)
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
            if vessel.condition != stack.condition:
                vessel.condition = "mixed"
        else:
            state.vessel_cargo[name] = CommodityStack(stack.quantity, stack.condition)
    state.carried_goods.clear()
    state.location = "jomon"
    state.position = JOMON_GANGPLANK
    state.loadout = None
    state.support = None
    state.remember(f"{courier.name} returned physically through Jomon's gangplank.")
    return _result(state, "You cross the gangplank home. Consequences and cargo persist.", time=True)
