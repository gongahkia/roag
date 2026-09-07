"""Direct deterministic actions for Jomon and seamless Hearthford."""

from __future__ import annotations

from dataclasses import dataclass

from .content import COMMODITIES, GEAR, MERCHANT_ITEMS, PASSIVES, RELICS, SUPPORTS, WEAPONS
from .inventory import (
    add_status,
    apply_terrain_status,
    auto_place,
    create_item,
    consume_carried,
    degrade_armour,
    equipped_item,
    load_state,
    lose_matching_carried,
    pack_weight,
    prepare_kind,
    protection_at,
    tick_statuses,
    transfer_to_grid,
    weight_capacity,
)
from .state import CommodityStack, GameState, Person, Position, Threat, stage_rng
from .world import (
    JOMON_GANGPLANK,
    area_name,
    base_tile,
    build_combinations,
    capacity,
    carried_bulk,
    distance,
    displayed_tile,
    field_of_view,
    is_walkable,
    line_of_sight,
    passive_bulk,
    passive_capacity,
    position_key,
    pressure,
    vertical_destination,
    vertical_open,
)


@dataclass(frozen=True)
class ActionResult:
    changed: bool
    time_advanced: bool
    message: str
    overlay: str | None = None


def _remember_contact(state: GameState, text: str) -> None:
    state.contact.memories.append(text)
    del state.contact.memories[:-8]


def _plain(
    state: GameState,
    message: str,
    *,
    changed: bool = False,
    overlay: str | None = None,
) -> ActionResult:
    if message and overlay is None:
        state.add_message(message)
    return ActionResult(changed, False, message, overlay)


def inspect(state: GameState, subject: str = "area") -> ActionResult:
    if subject == "household":
        living = sum(person.alive for person in state.household)
        active = state.courier.name if state.courier else "not chosen"
        text = f"Household: {living}/6 living; active courier {active}."
    elif subject == "cargo":
        goods = ", ".join(
            f"{name} {stack.quantity}" for name, stack in state.vessel_cargo.items()
        )
        text = f"Jomon hold: {goods}. {state.region.pressure}"
    else:
        text = f"{state.region.condition} {state.region.objective_text}"
    return _plain(state, text, overlay=text)


def choose_courier(state: GameState, person_id: str) -> ActionResult:
    person = next((candidate for candidate in state.household if candidate.id == person_id), None)
    if state.location != "jomon" or person is None or not person.alive:
        return _plain(state, "That household member cannot serve as courier.")
    state.active_courier_id = person.id
    readied = equipped_item(state, "readied", person.id)
    secondary = equipped_item(state, "secondary", person.id)
    state.weapon = readied.kind if readied else None
    state.gear = secondary.kind if secondary else None
    return _plain(state, f"{person.name}, {person.role}, will carry this expedition.", changed=True)


def choose_weapon(state: GameState, weapon: str) -> ActionResult:
    if state.location != "jomon" or weapon not in WEAPONS or weapon not in state.owned_weapons:
        return _plain(state, "That weapon is not available aboard Jomon.")
    if state.active_courier_id is None:
        return _plain(state, "Choose the courier before fitting their weapon.")
    if not any(item.kind == weapon and item.location not in {"lost", "destroyed"} for item in state.items):
        physical = create_item(state, weapon, "Jomon household stores")
        if not auto_place(state, physical.id, "locker"):
            state.items.remove(physical)
            return _plain(state, "Jomon's locker has no room for that weapon.")
    if not prepare_kind(state, weapon):
        return _plain(state, "That weapon cannot fit the courier's pack while swapping.")
    state.weapon, state.crossbow_loaded, state.aimed_target = weapon, True, None
    return _plain(state, f"Readied {WEAPONS[weapon][0]}.", changed=True)


def choose_gear(state: GameState, gear: str) -> ActionResult:
    if state.location != "jomon" or gear not in GEAR or gear not in state.owned_gear:
        return _plain(state, "That secondary item is not available aboard Jomon.")
    if state.active_courier_id is None:
        return _plain(state, "Choose the courier before fitting their secondary gear.")
    if not any(item.kind == gear and item.location not in {"lost", "destroyed"} for item in state.items):
        physical = create_item(state, gear, "Jomon household stores")
        if not auto_place(state, physical.id, "locker"):
            state.items.remove(physical)
            return _plain(state, "Jomon's locker has no room for that secondary item.")
    if not prepare_kind(state, gear):
        return _plain(state, "That secondary item cannot fit while swapping.")
    state.gear = gear
    return _plain(state, f"Packed {GEAR[gear][0]}.", changed=True)


def choose_support(state: GameState, support: str) -> ActionResult:
    if state.location != "jomon" or support not in SUPPORTS:
        return _plain(state, "That crew preparation is not available here.")
    state.support, state.support_spent = support, False
    return _plain(state, f"Prepared {SUPPORTS[support][0]}.", changed=True)


def choose_relic(state: GameState, relic: str | None) -> ActionResult:
    if state.location != "jomon" or (relic is not None and state.relics.get(relic, 0) <= 0):
        return _plain(state, "That finite relic is not available.")
    state.carried_relic = relic
    return _plain(state, f"Carried relic: {relic or 'none'}.", changed=True)


def choose_passive(state: GameState, passive: str) -> ActionResult:
    if state.location != "jomon" or passive not in state.owned_passives:
        return _plain(state, "That discovery is not available aboard Jomon.")
    carried = state.carried_passives.get(passive, 0)
    owned = state.owned_passives[passive]
    if carried >= owned:
        del state.carried_passives[passive]
        physical = next(
            (
                item for item in state.items
                if item.kind == f"passive:{passive}"
                and item.owner_id == state.active_courier_id
                and item.location == "pack"
            ),
            None,
        )
        if physical:
            transfer_to_grid(state, physical.id, "locker")
        return _plain(state, f"Stowed all {passive} aboard.", changed=True)
    if passive_bulk(state) + PASSIVES[passive][0] > passive_capacity(state):
        return _plain(state, f"Discovery load exceeds {passive_capacity(state)} bulk.")
    state.carried_passives[passive] = carried + 1
    physical = next(
        (item for item in state.items if item.kind == f"passive:{passive}" and item.location == "locker"),
        None,
    )
    if physical is None:
        physical = create_item(state, f"passive:{passive}", "returned expedition discovery")
    if not transfer_to_grid(state, physical.id, "pack", owner_id=state.active_courier_id):
        state.carried_passives[passive] = carried
        if carried == 0:
            del state.carried_passives[passive]
        return _plain(state, "The discovery has bulk allowance but no clear pack cells.")
    return _plain(
        state, f"Packed {passive} ({state.carried_passives[passive]}).", changed=True
    )


def _step_toward(state: GameState, threat: Threat, target: Position) -> Position:
    dx = 0 if target.x == threat.position.x else (1 if target.x > threat.position.x else -1)
    dy = 0 if target.y == threat.position.y else (1 if target.y > threat.position.y else -1)
    occupied = {
        other.position for other in state.threats
        if other.id != threat.id and other.status in {"watching", "engaged"}
    }
    candidates = (
        Position(threat.position.x + dx, threat.position.y + dy, threat.position.z),
        Position(threat.position.x + dx, threat.position.y, threat.position.z),
        Position(threat.position.x, threat.position.y + dy, threat.position.z),
    )
    for candidate in candidates:
        if candidate != state.position and candidate not in occupied and is_walkable(
            state, candidate, ignore_threat=True
        ):
            return candidate
    return threat.position


def _step_away(state: GameState, threat: Threat) -> Position:
    dx = 1 if threat.position.x >= state.position.x else -1
    dy = 1 if threat.position.y >= state.position.y else -1
    candidates = (
        Position(threat.position.x + dx, threat.position.y + dy, threat.position.z),
        Position(threat.position.x + dx, threat.position.y, threat.position.z),
        Position(threat.position.x, threat.position.y + dy, threat.position.z),
    )
    return next(
        (point for point in candidates if is_walkable(state, point, ignore_threat=True)),
        threat.position,
    )


def _activate(threat: Threat) -> str:
    threat.status = "engaged"
    threat.intent = {
        "pursuer": "rushes directly toward you",
        "reach": "levels a spear and holds two paces",
        "ranged": "takes aim; a bolt follows one clear turn",
        "animal": "scrapes the mud before a territorial charge",
        "machinery": "sweeps marked mill aisles on alternating turns",
    }[threat.profile]
    return f"The {threat.name} notices you: {threat.intent}."
def emit_sound(
    state: GameState, amount: int, origin: Position | None = None
) -> list[str]:
    """Raise noise and alert nearby actors, including actors one level away."""
    if state.location != "region" or amount <= 0:
        return []
    origin = origin or state.position
    state.noise += amount
    messages: list[str] = []
    for threat in state.threats:
        horizontal = max(
            abs(threat.position.x - origin.x), abs(threat.position.y - origin.y)
        )
        if (
            threat.status == "watching"
            and abs(threat.position.z - origin.z) <= 1
            and horizontal <= 4 + amount * 2
        ):
            messages.append(_activate(threat))
            if threat.position.z != origin.z and "echo bead" in state.carried_passives:
                messages.append(
                    f"The echo bead answers: danger stirs on level {threat.position.z:+d}."
                )
    return messages


def _lose_goods(state: GameState) -> str:
    if not state.carried_goods:
        return ""
    if state.support == "porter watch":
        return " The porter's watch preserves the accountable load."
    names = sorted(state.carried_goods)
    if state.gear == "cargo harness" or "river hooks" in state.carried_passives:
        kept = names[0]
        lose_matching_carried(
            state,
            {f"commodity:{name}" for name in names if name != kept},
        )
        state.carried_goods = {kept: state.carried_goods[kept]}
        return f" The harnessed {kept} survives; other cargo is lost."
    lose_matching_carried(state, {f"commodity:{name}" for name in names})
    state.carried_goods.clear()
    return f" The {', '.join(names)} is lost."


def _successor(state: GameState, dead: Person) -> Person | None:
    living = [person for person in state.household if person.alive]
    if not living:
        return None
    return sorted(
        living,
        key=lambda person: (-person.relationships.get(dead.id, 0), person.id),
    )[0]


def _return_after_defeat(state: GameState, text: str, permanent: bool) -> str:
    courier = state.courier
    if courier is None:
        return text
    loss = _lose_goods(state)
    lose_matching_carried(
        state,
        {
            item.kind for item in state.items
            if item.owner_id == state.active_courier_id
            and item.location == "pack"
            and item.kind.startswith(("passive:", "relic:", "consumable:"))
        },
    )
    state.carried_passives.clear()
    if state.objective_status in {"accepted", "altered"}:
        state.objective_status = "failed"
        state.contact.disposition = max(-3, state.contact.disposition - 1)
        _remember_contact(state, f"{courier.name} failed to return with Hearthford's need.")
    state.location, state.current_room, state.position = "jomon", None, JOMON_GANGPLANK
    if permanent:
        courier.alive, courier.health, courier.injury = False, 0, "dead"
        successor = _successor(state, courier)
        state.remember(
            f"{courier.name} died in {state.region.hazard}; carried discoveries were lost."
        )
        if successor is None:
            state.active_courier_id, state.world_ended = None, True
            return f"{text}{loss} No eligible adult survives; this world ends."
        state.active_courier_id = successor.id
        successor.relationships[courier.id] = min(
            3, successor.relationships.get(courier.id, 0) + 1
        )
        state.weapon = state.gear = state.support = None
        return f"{text}{loss} {successor.name} succeeds the dead courier."
    courier.health, courier.injury = max(2, courier.max_health // 3), "deep cut"
    state.remember(
        f"{courier.name} escaped to Jomon injured; carried discoveries were lost."
    )
    return f"{text}{loss} The courier reaches Jomon with a deep cut."


def _hit_location(state: GameState, damage_kind: str, source: str) -> str:
    exposed = ["torso", "arms", "legs", "head", "hands", "feet"]
    if "fall" in source.lower():
        exposed = ["legs", "feet", "arms", "head"]
    elif "bolt" in source.lower() or damage_kind == "pierce":
        exposed = ["torso", "arms", "head", "legs"]
    elif state.guarded_step:
        exposed = ["arms", "hands", "legs", "feet"]
    rng = stage_rng(state.seed, f"hit:{state.world_time}:{source}:{state.position}")
    return exposed[rng.randrange(len(exposed))]


def apply_damage(
    state: GameState,
    amount: int,
    source: str,
    *,
    damage_kind: str = "blunt",
    location: str | None = None,
) -> str:
    courier = state.courier
    if courier is None:
        return "No courier can be harmed."
    if state.support == "field care" and not state.support_spent:
        state.support_spent = True
        reduction = 3 if "deep field binding" in build_combinations(state) else 2
        amount = max(0, amount - reduction)
        if amount == 0:
            return "Prepared field care absorbs the injury."
    if (
        amount >= courier.health
        and state.carried_relic == "river-glass ward"
        and state.relics.get("river-glass ward", 0)
    ):
        state.relics["river-glass ward"] -= 1
        if state.relics["river-glass ward"] == 0:
            del state.relics["river-glass ward"]
        state.carried_relic, courier.health, courier.injury = (
            None,
            1,
            "river-glass chill",
        )
        return "The finite river-glass ward breaks instead of its bearer."
    location = location or _hit_location(state, damage_kind, source)
    protection, armour_name = protection_at(state, location, damage_kind)
    absorbed = min(max(0, amount - 1), protection)
    if absorbed:
        amount -= absorbed
        degrade_armour(state, location, 6 + absorbed * 4)
    already_hurt = courier.injury != "none" or bool(courier.injuries)
    courier.health = max(0, courier.health - amount)
    if courier.health:
        if amount:
            injury = {
                "head": "concussion",
                "torso": "bruised ribs" if damage_kind == "blunt" else "torso wound",
                "arms": "cut arm",
                "hands": "damaged hand",
                "legs": "strained leg",
                "feet": "wounded foot",
            }[location]
            if courier.health <= courier.max_health // 2 or amount >= 2:
                courier.injuries[location] = injury
                courier.injury = injury
        protection_text = f" {armour_name} absorbs {absorbed}." if absorbed else f" {location} is exposed."
        return f"{source} hits {location} for {amount} harm.{protection_text}"
    fatal = already_hurt or pressure(state).band == "critical" or "crown wheel" in source
    return _return_after_defeat(state, f"{source} overwhelms {courier.name}.", fatal)


def _threat_action(state: GameState, threat: Threat, guarded: bool) -> str:
    gap = distance(state.position, threat.position)
    threat.turn += 1
    if threat.intent.startswith(("disrupted", "dazed")):
        threat.intent = "recovers position before acting again"
        return f"The {threat.name} loses a turn recovering position."
    if threat.profile == "machinery":
        if gap > 7:
            return ""
        swept_rows = {22, 24, 26, 28}
        lane = "marked mill aisles"
        if threat.elite:
            outer = ((threat.turn - 1) // 2) % 2 == 0
            swept_rows = {22, 28} if outer else {24, 26}
            lane = "outer aisles 22/28" if outer else "inner aisles 24/26"
        threatened = state.position.y in swept_rows
        if threat.turn % 2:
            threat.intent = f"sweeps {lane} next turn"
            return f"The {threat.name} shudders: {lane} sweep next turn."
        if threatened and not guarded:
            source = "The runaway crown wheel" if threat.elite else "The mill sweep"
            return apply_damage(state, 3 if threat.elite else 2, source)
        return f"The mill sweep passes through {lane}; your position is safe."
    if threat.profile == "ranged":
        if threat.position.z != state.position.z and not line_of_sight(
            state, threat.position, state.position
        ):
            message = "tracks the sound across the levels"
            if threat.intent == message:
                return ""
            threat.intent = message
            return f"The {threat.name} hears you on another level."
        if not line_of_sight(state, threat.position, state.position):
            threat.position = _step_toward(state, threat, state.position)
            threat.intent = "moves for a clear line"
            return f"The {threat.name} shifts for a firing line."
        if gap <= 7:
            if "fires next turn" in threat.intent:
                threat.intent = "reloads before aiming again"
                if guarded:
                    return "Your guard and cover turn the bolt."
                harm = 3 if pressure(state).band == "critical" else 2
                return apply_damage(state, harm, f"The {threat.name}'s bolt")
            if "reload" in threat.intent:
                threat.intent = "aims and fires next turn"
                return f"The {threat.name} reloads and takes readable aim."
            threat.intent = "aims and fires next turn"
            return f"The {threat.name} aims: break sight or guard before the shot."
    if threat.profile == "animal" and gap <= 3:
        if base_tile(state, state.position) == "m" and "charge" in threat.intent:
            threat.status, threat.intent = "evaded", "bogged in the mud channel"
            state.remember(f"{state.courier.name} used deep mud to evade the reed boar.")
            return "The boar charges into deep mud: a positional evasion."
        if gap <= 1 and "charge" in threat.intent:
            threat.intent = "circles before another charge"
            if guarded:
                return "Your guarded footing turns the boar's charge."
            return apply_damage(state, 3, "The reed boar's shoulder")
        threat.intent = "lowers its head and charges next turn"
        return "The reed boar lowers its head: mud, light, or distance can redirect it."
    preferred = 2 if threat.profile == "reach" else 1
    if gap <= preferred:
        if guarded:
            threat.morale -= 1
            return f"Your guard denies the {threat.name}'s distance."
        marker = "thrusts next turn" if threat.profile == "reach" else "strikes next turn"
        if marker in threat.intent:
            threat.intent = "recovers before another attack"
            return apply_damage(state, 3, f"The {threat.name}'s attack")
        threat.intent = marker
        return f"The {threat.name} {marker}."
    for _ in range(pressure(state).pursuit_steps):
        threat.position = _step_toward(state, threat, state.position)
    threat.intent = (
        "pursues quickly" if pressure(state).pursuit_steps == 2
        else "closes through the terrain"
    )
    return f"The {threat.name} {threat.intent}."


def _weather_and_deadline(state: GameState) -> list[str]:
    elapsed = state.pressure_elapsed
    if 45 <= elapsed % 120 < 70:
        weather = "river fog"
    elif 70 <= elapsed % 120 < 95:
        weather = "hard rain"
    else:
        weather = "clear"
    messages: list[str] = []
    if weather != state.weather:
        state.weather = weather
        messages.append({
            "clear": "The weather opens; long sightlines return.",
            "river fog": "River fog closes floodplain sightlines.",
            "hard rain": "Hard rain slows exposed travel and feeds low water.",
        }[weather])
    if (
        not state.objective_changed
        and elapsed >= state.objective_deadline
        and state.objective_status in {"unoffered", "accepted", "altered"}
    ):
        state.objective_changed = True
        state.region.changes["late_objective"] = True
        state.market[state.region.objective_commodity].demand += 1
        messages.append(
            "The mill bell rings three times: late water worsens Hearthford's shortage."
        )
    if pressure(state).band == "critical" and not state.escalation_spawned:
        state.escalation_spawned = True
        reavers = next(t for t in state.threats if t.id == "pressure-reavers")
        reavers.status = "watching"
        messages.append(
            "High pressure draws valuable-seeking reavers onto the river road."
        )
    return messages


def _patrols(state: GameState) -> list[str]:
    messages: list[str] = []
    for threat in state.threats:
        if threat.status != "watching" or not threat.patrol:
            continue
        threat.patrol_index = (threat.patrol_index + 1) % len(threat.patrol)
        threat.position = threat.patrol[threat.patrol_index]
        if (
            distance(state.position, threat.position) <= pressure(state).alert_range
            and line_of_sight(state, threat.position, state.position)
        ):
            messages.append(_activate(threat))
    return messages


def _advance_world(
    state: GameState, *, guarded: bool = False, steps: int = 1
) -> None:
    old_band = pressure(state).band
    for tick in range(steps):
        state.world_time += 1
        if state.location != "region":
            continue
        state.pressure_elapsed += 1
        for ended in tick_statuses(state):
            state.add_message(ended, priority=0)
        for key in list(state.smoke):
            state.smoke[key] -= 1
            if state.smoke[key] <= 0:
                del state.smoke[key]
        messages = _weather_and_deadline(state) + _patrols(state)
        current = pressure(state)
        for threat in state.threats:
            if state.location != "region":
                break
            if threat.status == "watching" and not threat.patrol:
                seen = threat.position in field_of_view(state, remember=False)
                noisy = (
                    state.noise >= 3
                    and distance(state.position, threat.position) <= current.alert_range + 2
                )
                if (
                    seen and distance(state.position, threat.position) <= current.alert_range
                ) or noisy:
                    messages.append(_activate(threat))
            elif threat.status == "engaged":
                messages.append(_threat_action(state, threat, guarded and tick == 0))
        for message in messages:
            if message:
                state.add_message(message, priority=3)
    if state.location == "region":
        field_of_view(state)
        new_band = pressure(state).band
        if old_band != new_band and new_band in {"strained", "critical"}:
            state.add_message(
                f"Pressure becomes {new_band}: alert distance and pursuit increase.",
                priority=3,
            )


def _time_result(
    state: GameState,
    message: str,
    *,
    guarded: bool = False,
    steps: int = 1,
    priority: int = 2,
) -> ActionResult:
    _advance_world(state, guarded=guarded, steps=steps)
    # Keep the player's material consequence visible after same-turn intents.
    if message:
        state.add_message(message, priority=priority)
    return ActionResult(True, True, message)


def depart(state: GameState) -> ActionResult:
    if state.location != "jomon" or state.position != JOMON_GANGPLANK:
        return _plain(state, "Departure requires Jomon's gangplank.")
    if state.courier is None or not state.courier.alive:
        return _plain(state, "Choose an eligible courier at tavern C first.")
    if state.weapon is None or state.gear is None or state.support is None:
        return _plain(
            state,
            "Prepare weapon, secondary gear, and crew support at tavern C first.",
        )
    state.location, state.current_room = "region", "hearthford"
    state.position = state.region.landmarks["landing"]
    state.expedition_count += 1
    state.pressure_elapsed = state.noise = 0
    state.support_spent = state.guarded_step = False
    state.crossbow_loaded, state.aimed_target = True, None
    state.weather, state.smoke, state.water = "clear", {}, {}
    state.merchant_present, state.merchant_stock = False, []
    field_of_view(state)
    state.remember(
        f"Expedition {state.expedition_count}: {state.courier.name} crossed into seamless Hearthford."
    )
    return _time_result(
        state,
        "You cross Jomon's gangplank onto Hearthford quay; the road continues beyond the visible shore.",
        priority=3,
    )


def _fall(state: GameState) -> str:
    if state.position.z <= -1:
        return ""
    landing = Position(state.position.x, state.position.y, state.position.z - 1)
    if not is_walkable(state, landing, ignore_threat=True):
        return "The opening has no landing below."
    state.position = landing
    if "cliff cord" in state.carried_passives:
        return "The cliff cord turns the fall into a controlled descent."
    return "You fall through the opening. " + apply_damage(state, 2, "The fall")


def move(state: GameState, dx: int, dy: int) -> ActionResult:
    if state.world_ended or (dx == 0 and dy == 0):
        return _plain(state, "No action is possible.")
    target = Position(
        state.position.x + dx, state.position.y + dy, state.position.z
    )
    occupant = next(
        (
            threat
            for threat in state.threats
            if threat.position == target
            and threat.status in {"watching", "engaged"}
        ),
        None,
    )
    if occupant:
        if occupant.status == "watching":
            return _time_result(state, _activate(occupant), priority=3)
        return _plain(state, f"The {occupant.name} holds that space.")
    if not is_walkable(state, target):
        return _plain(state, "That way is blocked.")
    if dx and dy:
        side_a = Position(state.position.x + dx, state.position.y, state.position.z)
        side_b = Position(state.position.x, state.position.y + dy, state.position.z)
        if (
            not is_walkable(state, side_a, ignore_threat=True)
            and not is_walkable(state, side_b, ignore_threat=True)
        ):
            return _plain(state, "The diagonal is pinched closed.")
    previous_area = area_name(state)
    state.position = target
    messages: list[str] = []
    tile = base_tile(state, target)
    if state.location == "region" and tile == "+":
        state.region.tile_changes[position_key(target)] = "/"
        messages.append("You open the door; interior sightlines change.")
    quiet = state.courier and (
        state.courier.technique == "quiet passage"
        or "surveyed soft-step" in build_combinations(state)
    )
    if tile == "m" and not (
        state.gear == "quiet shoes"
        or "reed sole wraps" in state.carried_passives
    ):
        messages.append("Mud drags at your step; sound carries.")
        messages.extend(emit_sound(state, 1, target))
    elif not quiet and state.pressure_elapsed % 8 == 7:
        state.noise += 1
    status_message = apply_terrain_status(state, displayed_tile(state, target))
    if status_message:
        messages.append(status_message)
    water_delay = False
    if position_key(target) in state.water:
        protected = (
            state.gear == "rope"
            or "river hooks" in state.carried_passives
            or (state.courier and state.courier.technique == "sure footing")
        )
        if not protected:
            water_delay = True
            messages.append("Released water makes the crossing slow and exposed.")
    new_area = area_name(state)
    discovered_key = f"discovered:{new_area}"
    if new_area != previous_area and not state.region.changes.get(discovered_key):
        state.region.changes[discovered_key] = True
        messages.insert(0, f"You enter {new_area}; alternate routes open around the landmark.")
    if tile == "O":
        messages.append(_fall(state))
    burden = load_state(state)
    burden_delay = burden in {"encumbered", "overloaded"} and state.location == "region"
    if burden == "laden" and tile in {"m", "r", "t", ","}:
        state.noise += 1
    storm_delay = water_delay or burden_delay or (
        state.weather == "hard rain"
        and state.position.z == 0
        and "rain cape" not in state.carried_passives
    )
    guarded_step = state.guarded_step
    state.guarded_step = False
    return _time_result(
        state,
        " ".join(message for message in messages if message),
        guarded=guarded_step,
        steps=2 if storm_delay else 1,
        priority=3 if messages else 0,
    )


def can_alter_objective(state: GameState) -> bool:
    courier = state.courier
    return bool(
        state.gear == "repair tools"
        or state.support in {"route survey", "carpenter rig"}
        or (courier and courier.technique == "lever craft")
        or state.contact.disposition >= 2
    )


def decide_objective(state: GameState, decision: str) -> ActionResult:
    available = (
        state.location == "region"
        and state.position == state.region.landmarks["contact"]
        and state.objective_status in {"unoffered", "failed"}
    )
    if not available:
        return _plain(state, "No open material request can be decided here.")
    if decision == "alter" and not can_alter_objective(state):
        return _plain(state, "Alteration needs tools, route support, lever craft, or trust.")
    if decision == "accept":
        state.objective_status = "accepted"
        text = f"{state.courier.name} accepts the material recovery."
    elif decision == "refuse":
        state.objective_status = "refused"
        state.contact.disposition = max(-3, state.contact.disposition - 1)
        text = f"{state.courier.name} refuses Hearthford's difficult request."
    elif decision == "alter":
        state.objective_status = "altered"
        text = f"{state.courier.name} alters the request to flood-control work."
    else:
        return _plain(state, "Unknown objective decision.")
    _remember_contact(state, text)
    state.remember(text)
    return _time_result(state, text, priority=3)


def _add_goods(state: GameState, name: str, quantity: int, condition: str) -> bool:
    extra = COMMODITIES[name]["bulk"] * quantity
    if carried_bulk(state) + extra > capacity(state):
        return False
    physical = create_item(
        state,
        f"commodity:{name}",
        f"{state.active_region_id} material acquisition",
        owner_id=state.active_courier_id,
        quantity=quantity,
    )
    if not auto_place(state, physical.id, "pack", owner_id=state.active_courier_id):
        state.items.remove(physical)
        return False
    stack = state.carried_goods.get(name)
    if stack:
        stack.quantity += quantity
    else:
        state.carried_goods[name] = CommodityStack(quantity, condition)
    return True


def _complete_objective(state: GameState, altered: bool) -> str:
    late = state.objective_changed
    state.objective_status = "completed"
    state.contact.disposition = min(3, state.contact.disposition + (1 if late else 2))
    market = state.market[state.region.objective_commodity]
    market.stock += 1 if altered or late else 2
    market.demand = max(0, market.demand - (1 if late else 2))
    state.trade_credit += 1 if late else 2
    state.region.changes["mill_stabilised"] = altered
    if altered:
        method = "control work"
    elif late:
        method = "late cargo delivery"
    else:
        method = "accountable delivery"
    memory = f"{state.courier.name} completed Hearthford's request by {method}."
    _remember_contact(state, memory)
    state.remember(memory)
    return f"Hearthford records the {method}; stock and demand visibly change."


def _open_container(state: GameState) -> ActionResult:
    container = next(
        (item for item in state.region.containers if item.position == state.position),
        None,
    )
    if container is None or container.opened:
        return _plain(state, "The container is already empty.")
    requirement = container.requirement
    if requirement == "rope" and state.gear != "rope" and "river hooks" not in state.carried_passives:
        return _plain(state, "The cache needs a rope or river hooks.")
    if requirement == "light" and state.gear != "hooded lantern" and state.lamp_oil <= 0:
        return _plain(state, "The buried marks cannot be read without finite light.")
    if requirement == "key" and state.gear != "repair tools" and not (
        state.courier and state.courier.technique == "lever craft"
    ):
        return _plain(state, "The strongbox needs repair tools or lever craft.")
    if requirement == "rope" and state.gear == "rope" and "flood rig" not in build_combinations(state):
        if state.rope_uses <= 0:
            return _plain(state, "The expedition rope has no sound length remaining.")
        state.rope_uses -= 1
    if requirement == "light" and state.gear != "hooded lantern":
        state.lamp_oil -= 1
    reward = container.reward
    physical_kind = (
        f"passive:{reward}" if reward in PASSIVES else
        f"relic:{reward}" if reward in RELICS else
        f"consumable:{reward}"
    )
    physical = next(
        (item for item in state.items if item.id in container.item_ids),
        None,
    )
    if physical is None:
        physical = create_item(
            state,
            physical_kind,
            f"{container.name}, {state.active_region_id}",
            location="container",
        )
        physical.container_id = container.id
        container.item_ids.append(physical.id)
    fits_pack = transfer_to_grid(state, physical.id, "pack", owner_id=state.active_courier_id)
    if fits_pack:
        container.item_ids.remove(physical.id)
    if reward in PASSIVES:
        if passive_bulk(state) + PASSIVES[reward][0] > passive_capacity(state):
            return _plain(
                state,
                f"The {reward} will not fit: discovery load is full.",
            )
        state.carried_passives[reward] = state.carried_passives.get(reward, 0) + 1
    elif reward in RELICS:
        state.relics[reward] = state.relics.get(reward, 0) + 1
        state.carried_relic = reward
    elif reward == "sealed tally":
        state.trade_credit += 1
        _add_goods(state, "paper", 1, "sealed")
    else:
        state.consumables[reward] = state.consumables.get(reward, 0) + 1
    container.opened = True
    state.remember(f"{state.courier.name} opened {container.name} and found {reward}.")
    message = (
        f"You open {container.name}: {reward}. The depleted container remains visible."
        if fits_pack else
        f"You open {container.name}: {reward}. It remains inside until pack cells are cleared."
    )
    _advance_world(state)
    state.add_message(message, priority=3)
    return ActionResult(True, True, message, f"inventory:container:{container.id}")


def _control_interaction(state: GameState) -> ActionResult:
    courier = state.courier
    efficient = (
        state.gear == "repair tools"
        or state.support == "carpenter rig"
        or (courier and courier.technique == "lever craft")
    )
    state.flood_control = "lowered" if state.flood_control == "raised" else "raised"
    points = [
        Position(58, 42, -1),
        Position(58, 42, 0),
        Position(78, 22, 0),
    ]
    if state.flood_control == "lowered":
        state.water = {position_key(point): 99 for point in points}
    else:
        state.water.clear()
    sounds = emit_sound(state, 0 if efficient else 3)
    state.region.changes["flood_control_used"] = True
    if state.objective_status == "altered":
        state.region.changes["mill_stabilised"] = True
    text = (
        f"The sluice is {state.flood_control}; water crosses culvert and ground "
        "openings, changing route safety."
    )
    return _time_result(state, " ".join([text, *sounds]), priority=3)


def _furnace_interaction(state: GameState) -> ActionResult:
    machinery = next(threat for threat in state.threats if threat.profile == "machinery")
    if state.gear in {"repair tools", "rope"} or state.support == "carpenter rig":
        machinery.status, machinery.intent = "disabled", "braked at the furnace drive"
        state.region.changes["machinery_disabled"] = True
        state.smoke.clear()
        return _time_result(
            state,
            "You brake the furnace drive; machinery and rising smoke both stop.",
            priority=3,
        )
    smoke_points = [state.position, Position(state.position.x, state.position.y, 1)]
    state.smoke.update({position_key(point): 6 for point in smoke_points})
    sounds = emit_sound(state, 2)
    return _time_result(
        state,
        " ".join(
            [
                "The furnace coughs; smoke rises into the upper works and closes sightlines.",
                *sounds,
            ]
        ),
        priority=3,
    )


def _destroy_floor(state: GameState) -> ActionResult:
    if base_tile(state, state.position) != "d":
        return _plain(state, "No bounded weak floor is underfoot.")
    can_breach = state.weapon == "hand axe" or (
        state.weapon == "cudgel" and "mill-tooth wedge" in state.carried_passives
    )
    if not can_breach:
        return _plain(
            state, "The marked floor needs a hand axe or a cudgel with a mill-tooth wedge."
        )
    state.region.tile_changes[position_key(state.position)] = "O"
    sounds = emit_sound(state, 4)
    fall = _fall(state)
    return _time_result(
        state,
        " ".join(["The marked floor breaks into an open vertical shaft.", fall, *sounds]),
        priority=3,
    )


def interact(state: GameState) -> ActionResult:
    tile = base_tile(state, state.position)
    if state.location == "jomon":
        if tile == "+":
            return depart(state)
        if tile == "C":
            return ActionResult(False, False, "Prepare at the tavern.", "tavern")
        if tile in {"L", "P"}:
            return ActionResult(False, False, "Stores are readouts.", "equipment")
        if tile == "H":
            return ActionResult(False, False, "Inspect hold and local problem.", "hold")
        if tile == "s" and state.merchant_present:
            return ActionResult(False, False, "The deck merchant opens three lots.", "merchant")
        if tile in {"T", "b", "s"}:
            return ActionResult(False, False, "Inspect the household.", "household")
        return _plain(state, "Nothing here needs handling.")
    if state.position == state.region.landmarks["landing"]:
        return return_to_jomon(state)
    destination = vertical_destination(state, state.position)
    if destination:
        if load_state(state) == "overloaded" and destination.z > state.position.z:
            return _plain(state, "The overloaded pack makes this climb unsafe; repack or leave weight.")
        blocker = next(
            (
                threat
                for threat in state.threats
                if threat.position == destination
                and threat.status in {"watching", "engaged"}
            ),
            None,
        )
        if blocker:
            return _plain(
                state,
                f"The {blocker.name} holds the vertical opening; confront it across the level first.",
            )
        link = next(
            item for item in state.region.vertical_links
            if state.position in {item.first, item.second}
        )
        state.position = destination
        return _time_result(
            state,
            f"You use the {link.name}; nearby levels remain spatially aligned.",
            priority=3,
        )
    if any(item.position == state.position for item in state.region.containers):
        return _open_container(state)
    if state.position == state.region.landmarks["contact"]:
        if state.objective_status in {"unoffered", "failed"}:
            return ActionResult(
                False, False, f"{state.contact.name} explains the shortage.", "objective"
            )
        commodity = state.region.objective_commodity
        quantity = state.carried_goods.get(
            commodity, CommodityStack(0, "")
        ).quantity
        if state.objective_status == "accepted" and quantity >= state.objective_required:
            if not consume_carried(state, f"commodity:{commodity}", state.objective_required):
                state.carried_goods[commodity].quantity -= state.objective_required
                if state.carried_goods[commodity].quantity == 0:
                    del state.carried_goods[commodity]
            return _time_result(
                state, _complete_objective(state, False), priority=3
            )
        if (
            state.objective_status == "altered"
            and state.region.changes.get("mill_stabilised")
        ):
            return _time_result(
                state, _complete_objective(state, True), priority=3
            )
        return ActionResult(
            False, False, f"Inspect {state.contact.name}'s standing.", "contact"
        )
    if tile == "R":
        if state.region.changes.get("objective_taken"):
            return _plain(state, "The stranded load is empty.")
        if state.objective_status != "accepted":
            return _plain(state, "Accept Hearthford's request before taking the cargo.")
        commodity = state.region.objective_commodity
        if not _add_goods(
            state,
            commodity,
            state.objective_required,
            COMMODITIES[commodity]["condition"],
        ):
            return _plain(state, f"The load exceeds bulk or clear pack cells.")
        state.region.changes["objective_taken"] = True
        sounds = emit_sound(state, 2)
        return _time_result(
            state,
            " ".join([f"You secure two {commodity}; valuables and sound rise.", *sounds]),
            priority=3,
        )
    if tile == "&":
        return _control_interaction(state)
    if tile == "f":
        return _furnace_interaction(state)
    if tile == "d":
        return _destroy_floor(state)
    return _plain(state, "Nothing here needs handling.")


def _attack_targets(state: GameState, attack_range: int) -> list[Threat]:
    targets = (
        threat
        for threat in state.threats
        if threat.status in {"watching", "engaged"}
        and distance(state.position, threat.position) <= attack_range
        and line_of_sight(state, state.position, threat.position)
    )
    return sorted(
        targets, key=lambda threat: (distance(state.position, threat.position), threat.id)
    )


def attack(state: GameState) -> ActionResult:
    if state.location != "region" or state.weapon is None:
        return _plain(state, "No readied attack is possible.")
    ranges = {
        "billhook": 2,
        "spear": 3,
        "cudgel": 1,
        "staff": 1,
        "hand axe": 1,
        "crossbow": 7,
    }
    candidates = _attack_targets(state, ranges[state.weapon])
    if not candidates:
        if state.weapon == "hand axe" and base_tile(state, state.position) == "d":
            return _destroy_floor(state)
        return _plain(state, "No visible hostile is within this weapon's reach.")
    target = candidates[0]
    target.status = "engaged"
    if state.weapon == "crossbow":
        if not state.crossbow_loaded:
            return _plain(state, "The crossbow is unloaded; reload with G.")
        if state.aimed_target != target.id:
            state.aimed_target = target.id
            return _time_result(
                state,
                f"You aim at the {target.name}; firing commits the next action.",
                priority=3,
            )
        if (
            state.weather == "hard rain"
            and "weatherproof aim" not in build_combinations(state)
        ):
            state.aimed_target = None
            return _time_result(
                state,
                "Hard rain spoils the crossbow's committed aim before release.",
                priority=3,
            )
        if state.ammunition <= 0:
            return _plain(state, "No crossbow ammunition remains.")
        state.crossbow_loaded, state.aimed_target = False, None
        state.ammunition -= 1
        damage, weapon_text, sound = 3, "crossbow bolt", 3
    else:
        damage = {
            "billhook": 2,
            "spear": 2,
            "cudgel": 1,
            "staff": 1,
            "hand axe": 3,
        }[state.weapon]
        weapon_text = state.weapon
        sound = 1 if state.weapon in {"cudgel", "staff"} else 2
    if state.weapon == "billhook":
        target.morale -= 1
        old_position = target.position
        target.position = _step_toward(state, target, state.position)
        weapon_text += " pulls the target out of position"
        if "mobile hook" in build_combinations(state):
            state.position = old_position
        target.intent = "disrupted by the billhook"
    elif state.weapon == "spear":
        target.position = _step_away(state, target)
        weapon_text += " controls spacing"
        target.intent = "disrupted by spear spacing"
    elif state.weapon == "cudgel":
        target.morale -= 2
        target.position = _step_away(state, target)
        weapon_text += " dazes and knocks back"
        target.intent = "dazed by the cudgel"
    elif state.weapon == "staff":
        adjacent = [
            other for other in candidates
            if distance(state.position, other.position) <= 1
        ]
        for other in adjacent[1:]:
            other.health = max(0, other.health - 1)
            if other.health == 0:
                other.status = "defeated"
        state.guarded_step = True
        weapon_text += " sweeps nearby space and readies movement"
    elif state.weapon == "hand axe":
        target.morale -= 1
        weapon_text += " breaks guard"
    if (
        "high-ground drive" in build_combinations(state)
        and target.position.z < state.position.z
    ):
        target.position = _step_away(state, target)
    sounds = emit_sound(state, sound)
    target.health = max(0, target.health - damage)
    if target.health == 0 or (
        target.morale <= 0 and target.profile != "machinery"
    ):
        target.status = "defeated" if target.health == 0 else "retreated"
        target.intent = "removed from the route"
        memory = f"{state.courier.name} defeated {target.name} with {state.weapon}."
        state.remember(memory)
        _remember_contact(state, memory)
        text = f"The {weapon_text} removes the {target.name} from the route."
    else:
        text = (
            f"The {weapon_text} deals {damage}; "
            f"{target.name} has {target.health}/{target.max_health}."
        )
    return _time_result(state, " ".join([text, *sounds]), priority=3)


def guard(state: GameState) -> ActionResult:
    if state.location != "region":
        return _plain(state, "There is no expedition danger to guard against.")
    if state.weapon == "crossbow" and not state.crossbow_loaded:
        if state.ammunition <= 0:
            return _plain(state, "No crossbow ammunition remains.")
        state.crossbow_loaded = True
        return _time_result(
            state,
            "You reload the crossbow behind a committed guarded posture.",
            guarded=state.gear == "buckler",
            priority=3,
        )
    engaged = [
        threat for threat in state.threats
        if threat.status == "engaged"
        and distance(state.position, threat.position) <= 7
    ]
    if not engaged:
        return _plain(state, "There is no immediate danger to guard against.")
    strong = (
        state.gear == "buckler"
        or state.weapon == "staff"
        or (state.courier and state.courier.technique == "set stance")
    )
    if strong:
        morale_loss = 2 if "shielded set stance" in build_combinations(state) else 1
        for threat in engaged:
            threat.morale -= morale_loss
    state.guarded_step = (
        state.weapon == "staff" or "reed sole wraps" in state.carried_passives
    )
    if "shielded set stance" in build_combinations(state):
        text = "Buckler and set stance deny the attack and press hostile morale."
    elif strong:
        text = "You set a reinforced guard; the next reposition preserves control."
    else:
        text = "You guard and yield space deliberately."
    return _time_result(state, text, guarded=True, priority=3)


def use_gear(state: GameState) -> ActionResult:
    if state.location != "region":
        return _plain(state, "Expedition gear is used in the field.")
    if (
        state.carried_relic == "tide-knot charm"
        and state.relics.get("tide-knot charm", 0)
    ):
        state.relics["tide-knot charm"] -= 1
        if state.relics["tide-knot charm"] == 0:
            del state.relics["tide-knot charm"]
        state.carried_relic = None
        consume_carried(state, "relic:tide-knot charm")
        for threat in state.threats:
            if (
                threat.status == "engaged"
                and distance(state.position, threat.position) <= 10
            ):
                threat.status, threat.intent = (
                    "evaded",
                    "lost in unnaturally still water",
                )
        state.noise = max(0, state.noise - 4)
        return _time_result(
            state,
            "The finite tide-knot unravels; pursuit loses the rule of flowing water.",
            priority=3,
        )
    if state.gear == "smoke pot" and state.smoke_charges > 0:
        state.smoke_charges -= 1
        points = [
            state.position,
            Position(state.position.x + 1, state.position.y, state.position.z),
            Position(state.position.x, state.position.y + 1, state.position.z),
        ]
        above = Position(state.position.x, state.position.y, state.position.z + 1)
        if (
            state.position.z < 2
            and str(above.z) in state.region.levels
            and vertical_open(state, state.position, above)
        ):
            points.append(above)
        state.smoke.update({position_key(point): 6 for point in points})
        for threat in state.threats:
            if (
                threat.status == "engaged"
                and threat.profile != "machinery"
                and distance(state.position, threat.position) <= 7
            ):
                threat.status, threat.intent = "watching", "searches the smoke decoy"
        return _time_result(
            state,
            "Finite smoke closes adjacent sightlines and rises at an opening; ranged aim breaks.",
            priority=3,
        )
    animal = next(
        (
            threat
            for threat in state.threats
            if threat.status in {"watching", "engaged"}
            and threat.profile == "animal"
            and distance(state.position, threat.position) <= 7
        ),
        None,
    )
    if state.gear == "hooded lantern" and animal and state.lamp_oil > 0:
        state.lamp_oil -= 1
        animal.status, animal.intent = (
            "evaded",
            "follows controlled light away from the route",
        )
        state.remember(
            f"{state.courier.name} redirected the reed boar with finite lamplight."
        )
        return _time_result(
            state,
            "Controlled lamplight draws the territorial animal away: a material evasion.",
            priority=3,
        )
    if (
        state.consumables.get("willow dressing", 0)
        and state.courier
        and state.courier.injury != "none"
    ):
        state.consumables["willow dressing"] -= 1
        if state.consumables["willow dressing"] == 0:
            del state.consumables["willow dressing"]
        consume_carried(state, "consumable:willow dressing")
        state.courier.health = min(
            state.courier.max_health, state.courier.health + 3
        )
        state.courier.injury = "treated soreness"
        return _time_result(
            state,
            "A finite willow dressing restores three health; field healing remains scarce.",
            priority=3,
        )
    if state.consumables.get("dry smoke charge", 0) and state.smoke_charges == 0:
        state.consumables["dry smoke charge"] -= 1
        if state.consumables["dry smoke charge"] == 0:
            del state.consumables["dry smoke charge"]
        consume_carried(state, "consumable:dry smoke charge")
        state.smoke_charges = 1
        return _time_result(
            state, "You repack one finite smoke charge for later use.", priority=3
        )
    return _plain(state, "No readied finite gear applies here.")


def negotiate(state: GameState) -> ActionResult:
    if state.location != "region" or state.courier is None:
        return _plain(state, "No negotiation is possible here.")
    humans = [
        threat for threat in state.threats
        if threat.status == "engaged"
        and threat.profile in {"pursuer", "reach", "ranged"}
        and distance(state.position, threat.position) <= 4
    ]
    if not humans:
        return _plain(state, "No human obstruction is close enough to hear terms.")
    has_terms = (
        state.courier.technique == "measured terms"
        or state.gear == "trade seals"
        or state.support == "factor surety"
        or "paper" in state.carried_goods
        or "valuable leverage" in build_combinations(state)
    )
    if not has_terms:
        return _plain(
            state,
            "You lack witnessed seals, material surety, paper, or valuable leverage.",
        )
    if "paper" in state.carried_goods and state.gear != "trade seals":
        if not consume_carried(state, "commodity:paper"):
            state.carried_goods["paper"].quantity -= 1
            if state.carried_goods["paper"].quantity == 0:
                del state.carried_goods["paper"]
    for threat in humans:
        threat.status, threat.intent = "negotiated", "accepts witnessed terms"
    memory = (
        f"{state.courier.name} settled {len(humans)} route obstruction(s) "
        "through material terms."
    )
    state.remember(memory)
    _remember_contact(state, memory)
    return _time_result(
        state, "Witnessed material terms settle the obstruction without combat.", priority=3
    )


def retreat(state: GameState) -> ActionResult:
    nearby = [
        threat for threat in state.threats
        if threat.status == "engaged"
        and distance(state.position, threat.position) <= 9
    ]
    if state.location != "region" or not nearby:
        return _plain(state, "There is no encounter to retreat from.")
    has_line = (
        bool(state.smoke)
        or state.flood_control == "lowered"
        or state.gear in {"rope", "smoke pot"}
    )
    if pressure(state).depth >= 5 and not has_line:
        return _plain(
            state,
            "The deep route is cut off; create smoke, water, or a rigged line first.",
        )
    message = _return_after_defeat(
        state, "You abandon the route under pursuit.", permanent=False
    )
    return _time_result(state, message, priority=3)


def merchant_visit_due(seed: str, returned_expeditions: int) -> bool:
    if returned_expeditions <= 0:
        return False
    phase = stage_rng(seed, "merchant-cycle").randrange(1, 4)
    return (returned_expeditions - phase) % 3 == 0


def merchant_stock_for(state: GameState) -> list[str]:
    context = {
        "ironwork": "hand axe",
        "timber": "cargo harness",
        "charcoal": "hooded lantern",
    }[state.region.objective_commodity]
    outcome = "crossbow" if state.objective_status == "completed" else "smoke pot"
    rare = (
        state.objective_status == "completed"
        and stage_rng(
            state.seed, f"merchant-stock:{state.returned_expeditions}"
        ).randrange(5) == 0
    )
    finite = "tide-knot charm" if rare else "willow dressing"
    return list(dict.fromkeys((context, outcome, finite)))[:3]


def purchase_merchant_item(state: GameState, item: str) -> ActionResult:
    if (
        state.location != "jomon"
        or not state.merchant_present
        or item not in state.merchant_stock
    ):
        return _plain(state, "That merchant lot is not available.")
    cost, kind = MERCHANT_ITEMS[item]
    cost = max(1, cost - (1 if state.support == "factor surety" else 0))
    if state.trade_credit < cost:
        return _plain(
            state, f"The lot needs {cost} credit; Jomon has {state.trade_credit}."
        )
    physical_kind = (
        f"consumable:{item}" if kind == "consumable" else
        f"relic:{item}" if kind == "relic" else item
    )
    physical = create_item(state, physical_kind, "visiting Jomon merchant")
    if not auto_place(state, physical.id, "locker"):
        state.items.remove(physical)
        return _plain(state, "Jomon's bounded locker has no clear cells for that lot.")
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
    state.remember(
        f"Jomon exchanged {cost} credit for {item} from the visiting merchant."
    )
    return _time_result(
        state, f"Purchased {item}; it persists aboard Jomon.", priority=3
    )


def return_to_jomon(state: GameState) -> ActionResult:
    if (
        state.location != "region"
        or state.position != state.region.landmarks["landing"]
    ):
        return _plain(state, "Return requires Hearthford's physical gangplank.")
    courier = state.courier
    if state.objective_status in {"accepted", "altered"}:
        state.objective_status = "failed"
        state.contact.disposition = max(-3, state.contact.disposition - 1)
        _remember_contact(
            state, f"{courier.name} returned without completing the accepted request."
        )
    for name, stack in state.carried_goods.items():
        vessel = state.vessel_cargo.get(name)
        if vessel:
            vessel.quantity += stack.quantity
        else:
            state.vessel_cargo[name] = stack
    state.carried_goods.clear()
    for name, count in state.carried_passives.items():
        state.owned_passives[name] = state.owned_passives.get(name, 0) + count
    state.carried_passives.clear()
    state.location, state.current_room, state.position = (
        "jomon",
        None,
        JOMON_GANGPLANK,
    )
    state.returned_expeditions += 1
    state.merchant_present = merchant_visit_due(
        state.seed, state.returned_expeditions
    )
    state.merchant_stock = merchant_stock_for(state) if state.merchant_present else []
    state.remember(
        f"{courier.name} returned physically through Jomon's gangplank; "
        "discoveries entered household stores."
    )
    merchant = " A visiting merchant has tied alongside." if state.merchant_present else ""
    return _time_result(
        state,
        "You cross the gangplank home; cargo, discoveries, terrain, and consequences "
        f"persist.{merchant}",
        priority=3,
    )
