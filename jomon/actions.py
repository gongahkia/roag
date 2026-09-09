"""Direct deterministic actions for Jomon's vessel and four regions."""

from __future__ import annotations

from dataclasses import dataclass

from .content import COMMODITIES, GEAR, MERCHANT_ITEMS, PASSIVES, RELICS, SUPPORTS, WEAPONS
from .enemy_ai import next_path_step, raise_group_alert, retreat_step, select_goal
from .inventory import (
    add_status,
    apply_terrain_status,
    armour_mobility,
    armour_noise,
    auto_place,
    create_item,
    consume_ammunition,
    consume_carried,
    degrade_armour,
    ensure_courier_basics,
    equipped_item,
    load_state,
    lose_matching_carried,
    pack_weight,
    physical_ammunition,
    prepare_kind,
    protection_at,
    item_spec,
    InventoryTransaction,
    record_acquisition,
    sync_legacy_load,
    tick_statuses,
    transfer_to_grid,
    weight_capacity,
    worn_tags,
)
from .state import CommodityStack, GameState, Person, Position, SoundEvent, Threat, stage_rng
from .world import (
    JOMON_GANGPLANK,
    area_name,
    base_tile,
    build_combinations,
    capacity,
    carried_bulk,
    cover_at,
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
from .calendar import record_calendar_crossings
from .vessel import (
    HOUSEHOLD_SEATS,
    TAVERN_ENTRANCE,
    TAVERN_EXIT,
    advance_living_world,
    buy_drink,
    current_area,
    resolve_social_incident,
    vessel_vertical_destination,
)


@dataclass(frozen=True)
class ActionResult:
    changed: bool
    time_advanced: bool
    message: str
    overlay: str | None = None


def _remember_contact(state: GameState, text: str) -> None:
    if state.location == "jomon":
        state.chronicle.append(text)
        del state.chronicle[:-40]
        return
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
    ensure_courier_basics(state, person)
    previous_id = state.active_courier_id
    if previous_id == person.id:
        state.support = state.support or "route survey"
        sync_legacy_load(state)
        return _plain(state, f"{person.name} remains ready for the courier watch.")
    old_position = state.position
    schedule = state.actor_schedules.get(person.id)
    seat = schedule.position if schedule and schedule.area == "tavern" else state.tavern_positions.pop(person.id, state.position)
    if previous_id and previous_id != person.id:
        occupied = set(state.tavern_positions.values())
        previous_position = old_position if state.jomon_space == "tavern" else next(
            (point for point in HOUSEHOLD_SEATS if point not in occupied),
            HOUSEHOLD_SEATS[0],
        )
        state.tavern_positions[previous_id] = previous_position
        previous = state.actor_schedules.get(previous_id)
        if previous:
            previous.area = previous.destination_area = "tavern"
            previous.position = previous.destination = previous_position
    state.active_courier_id = person.id
    state.tavern_positions.pop(person.id, None)
    state.position = seat
    state.jomon_space = "tavern"
    state.support = state.support or "route survey"
    readied = equipped_item(state, "readied", person.id)
    secondary = equipped_item(state, "secondary", person.id)
    state.weapon = readied.kind if readied else None
    state.gear = secondary.kind if secondary else None
    return _plain(state, f"{person.name}, {person.role}, will carry this expedition.", changed=True)


def recruit_person(state: GameState, person_id: str) -> ActionResult:
    if state.location != "jomon":
        return _plain(state, "Recruitment terms are settled face to face aboard Jomon.")
    from .people import recruit_visitor

    changed, message = recruit_visitor(state, person_id)
    if changed:
        person = next(candidate for candidate in state.household if candidate.id == person_id)
        ensure_courier_basics(state, person)
        message += " Jomon issues a basic role-appropriate expedition kit."
    return _plain(state, message, changed=changed)


def defer_recruit(state: GameState, person_id: str) -> ActionResult:
    from .people import defer_visitor

    changed, message = defer_visitor(state, person_id)
    return _plain(state, message, changed=changed)


def choose_weapon(state: GameState, weapon: str) -> ActionResult:
    if state.location != "jomon" or weapon not in WEAPONS or weapon not in state.owned_weapons:
        return _plain(state, "That weapon is not available aboard Jomon.")
    if state.active_courier_id is None:
        return _plain(state, "Choose the courier before fitting their weapon.")
    transaction = InventoryTransaction.begin(state)
    if not any(item.kind == weapon and item.location not in {"lost", "destroyed"} for item in state.items):
        physical = create_item(state, weapon, "Jomon household stores")
        if not auto_place(state, physical.id, "locker"):
            transaction.cancel(state)
            return _plain(state, "Jomon's locker has no room for that weapon.")
    if not prepare_kind(state, weapon):
        transaction.cancel(state)
        return _plain(state, "That weapon cannot fit the courier's pack while swapping.")
    supply = {
        "crossbow": "crossbow bolts", "longbow": "fletched arrows",
        "sling": "sling shot pouch", "heavy crossbow": "quarrel case",
        "weighted net": "casting net bundle",
        "staff sling": "sling shot pouch", "hooked javelin": "throwing javelins",
        "handgonne": "handgonne charges",
    }.get(weapon)
    if supply and not any(
        item.kind == f"consumable:{supply}" and item.owner_id == state.active_courier_id
        and item.location == "pack" for item in state.items
    ):
        ammunition_item = next(
            (item for item in state.items if item.kind == f"consumable:{supply}" and item.location == "locker"),
            None,
        )
        if ammunition_item and not transfer_to_grid(state, ammunition_item.id, "pack", owner_id=state.active_courier_id):
            transaction.cancel(state)
            return _plain(state, "The weapon fits, but its physical ammunition case does not; repack first.")
    state.weapon, state.crossbow_loaded, state.aimed_target = weapon, True, None
    state.weapon_ready = 2 if weapon in {"heavy crossbow", "handgonne"} else 1
    sync_legacy_load(state)
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
    return next_path_step(state, threat, target)


def _step_away(state: GameState, threat: Threat) -> Position:
    return retreat_step(state, threat)


def _activate(threat: Threat) -> str:
    threat.status = "engaged"
    threat.intent = {
        "pursuer": "rushes directly toward you",
        "reach": "levels a spear and holds two paces",
        "ranged": "takes aim; a bolt follows one clear turn",
        "animal": "scrapes the mud before a territorial charge",
        "machinery": "sweeps marked mill aisles on alternating turns",
    }.get(threat.profile, "turns toward the disturbance")
    if threat.ecology == "prey":
        threat.intent = "raises its head and looks for a route away from the disturbance"
    elif threat.ecology == "predator":
        threat.intent = "watches for prey and exposed movement"
    elif threat.duty:
        threat.intent = f"weighs the disturbance against its {threat.duty} duty"
    return f"The {threat.name} notices you: {threat.intent}."
def emit_sound(
    state: GameState, amount: int, origin: Position | None = None
) -> list[str]:
    """Raise noise and alert nearby actors, including actors one level away."""
    if not state.combat_active or amount <= 0:
        return []
    origin = origin or state.position
    state.noise += amount
    state.sound_events.append(SoundEvent(origin, amount))
    del state.sound_events[:-8]
    messages: list[str] = []
    for threat in state.combatants:
        horizontal = max(
            abs(threat.position.x - origin.x), abs(threat.position.y - origin.y)
        )
        if (
            threat.status == "watching"
            and abs(threat.position.z - origin.z) <= (
                2 if "echo slate" in state.carried_passives else 1
            )
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
    if (
        state.gear == "cargo harness"
        or "river hooks" in state.carried_passives
        or (
            "cork float" in state.carried_passives
            and "current" in state.terrain_statuses
        )
    ):
        kept = names[0]
        lose_matching_carried(
            state,
            {f"commodity:{name}" for name in names if name != kept},
        )
        state.carried_goods = {kept: state.carried_goods[kept]}
        protection = (
            "buoyant cork rig"
            if "cork float" in state.carried_passives
            and "current" in state.terrain_statuses
            else "harness"
        )
        return f" The {protection} keeps {kept}; other cargo is lost."
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
    if state.location == "jomon":
        from .ship_crises import deck_defeat
        return deck_defeat(state, text, permanent)
    courier = state.courier
    if courier is None:
        return text
    defeated_at = state.position
    carried_locations = {
        "pack", "readied", "secondary", "head", "torso", "arms", "hands",
        "legs", "feet",
    } if permanent else {"pack"}
    dropped = [
        item for item in state.items
        if item.owner_id == state.active_courier_id and item.location in carried_locations
    ]
    preserved_cargo = None
    if "buoyant cargo rig" in build_combinations(state):
        preserved_cargo = next(
            (item for item in dropped if item.kind.startswith("commodity:")),
            None,
        )
        if preserved_cargo:
            dropped.remove(preserved_cargo)
    lost_names: list[str] = []
    for item in dropped:
        item.location, item.owner_id = "ground", None
        item.region_id, item.ground_position, item.container_id = (
            state.active_region_id, defeated_at, None,
        )
        lost_names.append(item_spec(item.kind).name)
    sync_legacy_load(state)
    loss = (
        f" {', '.join(lost_names)} remains at the defeat site."
        if lost_names else ""
    )
    if preserved_cargo:
        loss += f" The cork-floated {item_spec(preserved_cargo.kind).name} stays on the harness."
    if state.objective_status in {"accepted", "altered"}:
        state.objective_status = "failed"
        state.contact.disposition = max(-3, state.contact.disposition - 1)
        _remember_contact(state, f"{courier.name} failed to return with {state.region.name}'s need.")
    from .regions import store_active_region

    store_active_region(state)
    state.location, state.current_room, state.position = "jomon", None, JOMON_GANGPLANK
    if permanent:
        courier.alive, courier.health, courier.injury = False, 0, "dead"
        successor = _successor(state, courier)
        state.remember(
            f"{courier.name} died in {state.region.hazard}; their physical load remains at the defeat site."
        )
        if successor is None:
            state.active_courier_id, state.world_ended = None, True
            return f"{text}{loss} No eligible adult survives; this world ends."
        state.active_courier_id = successor.id
        successor.relationships[courier.id] = min(
            3, successor.relationships.get(courier.id, 0) + 1
        )
        ensure_courier_basics(state, successor)
        state.support = "route survey"
        sync_legacy_load(state)
        return f"{text}{loss} {successor.name} succeeds the dead courier with their own working kit."
    courier.health, courier.injury = max(2, courier.max_health // 3), "deep cut"
    state.remember(
        f"{courier.name} escaped to Jomon injured; their pack remains at the defeat site."
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
        consume_carried(state, "relic:river-glass ward")
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
    if threat.intent.startswith(("disrupted", "dazed", "entangled")):
        was_entangled = threat.intent.startswith("entangled")
        threat.intent = "cuts free of the net before acting again" if was_entangled else "recovers position before acting again"
        threat.reaction, threat.marked_position = "", None
        return f"The {threat.name} loses a turn {threat.intent}."
    if threat.elite and threat.id == "floodgate-claimant":
        if (
            state.region.changes.get("mill_public_compact")
            or state.region.changes.get("flood_control_used")
        ):
            threat.morale -= 2
            threat.intent = "cannot claim a publicly witnessed and dogged sluice"
            if threat.morale <= 0:
                threat.status = "retreated"
            return "The witnessed sluice control strips the floodgate claimant's leverage."
        if threat.aimed_at is None:
            threat.aimed_at = state.position
            threat.intent = f"marks the mill crossing at {state.position.x},{state.position.y} for a sluice surge"
            return f"The {threat.name} {threat.intent}; climb, move, guard, or dog the control."
        marked, threat.aimed_at = threat.aimed_at, None
        points = (
            Position(marked.x - 1, marked.y, marked.z), marked,
            Position(marked.x + 1, marked.y, marked.z),
        )
        state.water.update({position_key(point): 7 for point in points})
        if state.position in points and not guarded:
            return apply_damage(state, 3, "The claimant's sluice surge", damage_kind="blunt")
        return "The sluice surge crosses three marked mill cells; the gantry and moved position remain safe."
    if threat.elite and state.active_region_id == "greywash":
        if threat.name == "wreck-chain reeve":
            if (
                state.region.changes.get("tide_held")
                or state.questlines["greywash"].optional_done
            ):
                threat.morale -= 2
                threat.intent = "cannot claim witnessed wreck property"
                if threat.morale <= 0:
                    threat.status = "retreated"
                return "The dogged chain or witnessed wreck account denies the reeve's salvage claim."
            if threat.aimed_at is None:
                threat.aimed_at = state.position
                threat.intent = f"hooks loose wreck cover from lane {state.position.x},{state.position.y} before a sling cast"
                return f"The {threat.name} {threat.intent}; fixed dune cover and movement remain answers."
            marked, threat.aimed_at = threat.aimed_at, None
            cover = Position(marked.x - 1, marked.y, marked.z)
            if is_walkable(state, cover, ignore_threat=True):
                state.region.tile_changes[position_key(cover)] = "."
            if state.position == marked and not guarded:
                return apply_damage(state, 2, "The wreck-chain reeve's plunging sling", damage_kind="blunt")
            return "The reeve hauls loose wreck cover from the telegraphed lane; your new position avoids the cast."
        if state.region.changes.get("tide_held"):
            threat.morale -= 2
            threat.intent = "cannot close the dogged tide chain"
            if threat.morale <= 0:
                threat.status = "retreated"
            return "The dogged windlass denies the storm-chain captain's route-changing goal."
        if threat.turn % 2:
            threat.intent = "hauls the tide chain; the three marked flats flood next turn"
            return f"The {threat.name} {threat.intent}."
        points = (Position(88, 28), Position(89, 28), Position(90, 28))
        state.water.update({position_key(point): 8 for point in points})
        if state.position in points and not guarded:
            return apply_damage(state, 3, "The hauled tide chain and current", damage_kind="blunt")
        return "The tide chain floods three marked flats; higher chain-house floor remains safe."
    if threat.elite and state.active_region_id == "greenwold":
        if threat.name == "resin-fire tracker":
            if state.region.changes.get("medicine_coppice_saved"):
                threat.morale -= 2
                threat.intent = "will not burn the witnessed medicine stand"
                if threat.morale <= 0:
                    threat.status = "retreated"
                return "The preserved medicine boundary denies the resin-fire tracker's burn."
            if threat.aimed_at is None:
                threat.aimed_at = state.position
                threat.intent = f"marks resin under {state.position.x},{state.position.y}; water or movement breaks the trap"
                return f"The {threat.name} {threat.intent}."
            marked, threat.aimed_at = threat.aimed_at, None
            smoke_points = (marked, Position(marked.x, marked.y, min(2, marked.z + 1)))
            state.smoke.update({position_key(point): 6 for point in smoke_points})
            if state.position == marked:
                add_status(state, "smoke-inhalation", "ignited resin underfoot", 5, "sight and ranged preparation worsen")
            return "Marked resin ignites and smoke rises through the aligned opening; water and crosswind ground remain clear."
        if state.region.changes.get("burn_redirected"):
            threat.morale -= 2
            threat.intent = "loses control of the crosswind burn"
            if threat.morale <= 0:
                threat.status = "retreated"
            return "Redirected burn shutters strip the ash-cloak warden of smoke control."
        smoke_line = [
            Position(state.position.x + offset, state.position.y, state.position.z)
            for offset in (-1, 0, 1)
        ]
        state.smoke.update({position_key(point): 5 for point in smoke_line})
        threat.intent = "drives a three-cell smoke line across your current route"
        return f"The {threat.name} {threat.intent}; climb or move crosswind."
    if threat.elite and state.active_region_id == "whitecairn":
        if threat.name == "bridge-breaker bellward":
            if state.region.changes.get("honest_bell"):
                threat.morale -= 2
                threat.intent = "cannot break a crossing under the honest warning"
                if threat.morale <= 0:
                    threat.status = "retreated"
                return "The honest bell exposes the bridge-breaker's private order and breaks its morale."
            if threat.aimed_at is None:
                threat.aimed_at = state.position
                threat.intent = f"marks floor brace {state.position.x},{state.position.y},{state.position.z:+d} for a heavy bolt"
                return f"The {threat.name} {threat.intent}; move levels, shelter, or brace the quarry."
            marked, threat.aimed_at = threat.aimed_at, None
            if base_tile(state, marked) not in {"#", " ", "~"}:
                state.region.tile_changes[position_key(marked)] = "O"
            if state.position == marked:
                fall = _fall(state)
                return f"The heavy bolt breaks the marked floor. {fall}"
            return "The heavy bolt opens a hole in the marked crossing; the lower switchback remains a return route."
        if state.region.changes.get("quarry_braced"):
            threat.morale -= 2
            threat.intent = "cannot release the braced rock face"
            if threat.morale <= 0:
                threat.status = "retreated"
            return "The seated quarry braces deny the false-bell master's rockfall plan."
        if threat.aimed_at is None:
            threat.aimed_at = state.position
            threat.intent = f"rings a rockfall warning over {state.position.x},{state.position.y}; leave the marked cell"
            return f"The {threat.name} {threat.intent}."
        marked, threat.aimed_at = threat.aimed_at, None
        state.region.tile_changes[position_key(marked)] = "%"
        if state.position == marked and not guarded:
            return apply_damage(state, 3, "The false bell's released rockfall", damage_kind="blunt")
        return "Rockfall strikes the marked cell and leaves unstable scree; your reposition avoids it."
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
    decision = select_goal(state, threat)
    from .ecology import resolve_world_action

    world_action = resolve_world_action(state, threat, decision)
    if world_action is not None:
        return world_action
    if decision.action == "alarm":
        raise_group_alert(state, threat)
        threat.intent = "signals allies toward your last-known position"
        return f"The {threat.name} raises an alarm; nearby allies converge on a shared position."
    if decision.action == "control":
        if threat.aimed_at is None:
            threat.aimed_at = state.position
            threat.intent = f"casts a weighted net across {state.position.x},{state.position.y}; leave the marked cell"
            return f"The {threat.name} {threat.intent}."
        marked, threat.aimed_at = threat.aimed_at, None
        if state.position == marked:
            add_status(
                state, "net-drag", "a weighted shore net", 3,
                "guarded reposition, evasion, and current crossings worsen",
            )
            threat.intent = "hauls the marked net line"
            return f"The {threat.name} hauls the net across the marked cell; movement control worsens."
        threat.intent = "recovers the empty net line"
        return f"The {threat.name}'s net closes on empty ground after your reposition."
    if decision.action == "feed smoke":
        points = (
            threat.position,
            Position(threat.position.x - 1, threat.position.y, threat.position.z),
            Position(threat.position.x + 1, threat.position.y, threat.position.z),
        )
        state.smoke.update(
            {
                position_key(point): 5 for point in points
                if is_walkable(state, point, ignore_threat=True)
            }
        )
        threat.intent = "feeds a bounded smoke lane from its material station"
        return f"The {threat.name} {threat.intent}; wind, height, or the control can answer it."
    if decision.action == "cover retreat" and decision.target:
        wounded = next(
            (
                ally for ally in state.combatants
                if ally.group == threat.group and ally.position == decision.target
                and ally.id != threat.id
            ),
            None,
        )
        previous = threat.position
        threat.position = next_path_step(
            state, threat, decision.target, stop_distance=1
        )
        if wounded:
            wounded.morale = min(3, wounded.morale + 1)
            wounded.goal = "break contact"
            wounded.goal_reason = "an ally opened a withdrawal lane"
        threat.intent = "covers a wounded ally's marked withdrawal"
        if threat.position == previous and not wounded:
            return ""
        return f"The {threat.name} {threat.intent}."
    if decision.action in {"retreat", "withdraw"}:
        previous = threat.position
        if state.location == "jomon" and decision.action == "retreat" and threat.home_position:
            threat.position = next_path_step(state, threat, threat.home_position, stop_distance=1)
        else:
            threat.position = retreat_step(state, threat)
        threat.intent = "withdraws toward cover" if decision.action == "withdraw" else f"breaks contact: {decision.reason}"
        if threat.position == previous:
            threat.stalled_turns += 1
            return "" if threat.stalled_turns > 1 else f"The {threat.name} cannot find a safe retreat."
        threat.stalled_turns = 0
        return f"The {threat.name} {threat.intent}."
    if decision.action == "escape":
        target = threat.home_position or threat.position
        if distance(threat.position, target) <= 1:
            threat.status, threat.intent = "retreated", "escaped with stolen cargo"
            loss = ""
            if threat.carrying_item_id:
                stolen = next(
                    (item for item in state.items if item.id == threat.carrying_item_id),
                    None,
                )
                if stolen:
                    stolen.location = "lost"
                    stolen.owner_id = None
                    stolen.region_id = state.spatial_id
                    stolen.ground_position = None
                    stolen.container_id = None
                    loss = f" The {item_spec(stolen.kind).name} is now recorded as lost beyond the regional route."
                    state.remember(
                        f"{threat.name.title()} escaped {state.spatial_id} with {item_spec(stolen.kind).name}; the physical item was lost."
                    )
                threat.carrying_item_id = None
                sync_legacy_load(state)
            return f"The {threat.name} escapes the encounter with stolen cargo.{loss}"
        previous = threat.position
        threat.position = next_path_step(state, threat, target, stop_distance=0)
        return "" if threat.position == previous else f"The {threat.name} carries stolen cargo toward its escape route."
    if decision.action == "steal":
        candidates = [
            item for item in state.items
            if item.owner_id == state.active_courier_id and item.location == "pack"
            and item_spec(item.kind).category in {"cargo", "passive", "relic"}
        ]
        if candidates:
            stolen = max(candidates, key=lambda item: (item_spec(item.kind).weight, item.id))
            stolen.location, stolen.owner_id = "enemy", None
            threat.carrying_item_id = stolen.id
            sync_legacy_load(state)
            threat.intent = "escapes with visible stolen cargo"
            return f"The {threat.name} takes {item_spec(stolen.kind).name} and turns for an escape route."
    if decision.action == "investigate" and decision.target:
        previous = threat.position
        threat.position = next_path_step(state, threat, decision.target, stop_distance=0)
        if threat.position == previous:
            if threat.position == decision.target:
                threat.last_known_position = None
                threat.status, threat.intent = "watching", "finds no courier at the last-known position"
                return f"The {threat.name} reaches the sound's origin and finds it empty."
            threat.stalled_turns += 1
            return "" if threat.stalled_turns > 1 else f"The {threat.name} pauses where the investigation route is blocked."
        threat.stalled_turns = 0
        threat.intent = "investigates a last-known position"
        return f"The {threat.name} investigates without knowing your current position."
    if decision.action == "reload":
        threat.reload_turns = max(0, threat.reload_turns - 1)
        if threat.reload_turns:
            threat.intent = f"reloads {threat.ranged_kind}; {threat.reload_turns} turn remains"
        else:
            threat.intent = f"finishes reloading {threat.ranged_kind}"
        return f"The {threat.name} {threat.intent}."
    if decision.action in {"intercept", "patrol", "return", "approach", "flank", "seek elevation"} and decision.target:
        stop_distance = 1 if decision.action == "intercept" else 0
        previous = threat.position
        steps = pressure(state).pursuit_steps if decision.action == "approach" else 1
        for _ in range(steps):
            threat.position = next_path_step(
                state, threat, decision.target, stop_distance=stop_distance
            )
        descriptions = {
            "intercept": "moves between you and its ranged ally",
            "patrol": "resumes its assigned patrol without knowing your position",
            "return": "returns to its guarded position",
            "approach": "pursues your last visible position",
            "flank": "moves toward a visible side approach rather than your exact position",
            "seek elevation": "takes a physical stair or climb toward a higher firing lane",
        }
        threat.intent = descriptions[decision.action]
        if threat.position == previous:
            threat.stalled_turns += 1
            return "" if threat.stalled_turns > 1 else f"The {threat.name} holds; its selected route is blocked."
        threat.stalled_turns = 0
        if decision.action == "patrol" and threat.patrol:
            next_index = (threat.patrol_index + 1) % len(threat.patrol)
            if threat.position == threat.patrol[next_index]:
                threat.patrol_index = next_index
        return f"The {threat.name} {threat.intent}."
    if decision.action == "wait":
        if threat.intent == "holds without a perceived courier position":
            return ""
        threat.intent = "holds without a perceived courier position"
        return f"The {threat.name} holds its duty; it does not know where you are."
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
            previous = threat.position
            threat.position = _step_toward(state, threat, state.position)
            threat.intent = "moves for a clear line"
            if threat.position == previous:
                threat.stalled_turns += 1
                return "" if threat.stalled_turns > 1 else f"The {threat.name} cannot find a firing line."
            threat.stalled_turns = 0
            return f"The {threat.name} shifts for a firing line."
        effective_range = {"longbow": 12, "sling": 9, "heavy crossbow": 14, "crossbow": 8}.get(threat.ranged_kind, 8)
        if gap <= effective_range:
            if threat.aimed_at is not None:
                aimed = threat.aimed_at
                threat.aimed_at = None
                threat.ammunition = max(0, threat.ammunition - 1)
                threat.reload_turns = {"heavy crossbow": 2, "crossbow": 1, "longbow": 1, "sling": 0}.get(threat.ranged_kind, 1)
                threat.intent = f"must reload {threat.ranged_kind}"
                if aimed != state.position:
                    if threat.role == "suppressor":
                        add_status(state, "lane-denied", "missiles striking the marked lane", 2, "crossing the lane adds noise")
                    return f"The {threat.name} releases along {aimed.x},{aimed.y}; your movement leaves the lane empty."
                lane_cover = cover_at(state, threat.position, state.position)
                if lane_cover == "full":
                    return f"The {threat.name}'s shot strikes full cover."
                if guarded or lane_cover == "partial":
                    if threat.role == "suppressor":
                        add_status(state, "lane-denied", "missiles striking cover", 2, "leaving cover adds noise")
                    return f"Guard and {lane_cover} cover turn the {threat.ranged_kind} shot."
                harm = {"sling": 1, "longbow": 2, "crossbow": 2, "heavy crossbow": 4}.get(threat.ranged_kind, 2)
                if pressure(state).band == "critical":
                    harm += 1
                if threat.role == "shooter" and load_state(state) in {"encumbered", "overloaded"}:
                    harm += 1
                movement = ""
                if threat.role == "skirmisher":
                    old = threat.position
                    threat.position = retreat_step(state, threat)
                    movement = " It releases while withdrawing." if threat.position != old else ""
                kind = "blunt" if threat.ranged_kind == "sling" else "pierce"
                return apply_damage(state, harm, f"The {threat.name}'s {threat.ranged_kind}", damage_kind=kind) + movement
            threat.aimed_at = state.position
            threat.intent = f"aims {threat.ranged_kind} along lane {state.position.x},{state.position.y}; move, cover, smoke, or guard"
            return f"The {threat.name} {threat.intent}."
    if threat.profile == "animal" and gap <= 3:
        if base_tile(state, state.position) == "m" and "charge" in threat.intent:
            threat.status, threat.intent = "evaded", "bogged in the mud channel"
            state.remember(f"{state.courier.name} used deep mud to evade {threat.name}.")
            return f"The {threat.name} charges into deep mud: a positional evasion."
        if gap <= 1 and "charge" in threat.intent:
            threat.intent = "circles before another charge"
            if guarded:
                return f"Your guarded footing turns the {threat.name}'s charge."
            return apply_damage(state, 3, f"The {threat.name}'s charge")
        threat.intent = "lowers its head and charges next turn"
        return f"The {threat.name} lowers its head: mud, light, or distance can redirect it."
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
    previous = threat.position
    for _ in range(pressure(state).pursuit_steps):
        threat.position = _step_toward(state, threat, state.position)
    if threat.position == previous:
        threat.stalled_turns += 1
        threat.intent = "holds where the route is blocked"
        return "" if threat.stalled_turns > 1 else f"The {threat.name} holds; no route currently reaches you."
    threat.stalled_turns = 0
    threat.intent = (
        "pursues quickly" if pressure(state).pursuit_steps == 2
        else "closes through the terrain"
    )
    return f"The {threat.name} {threat.intent}."


def _weather_and_deadline(state: GameState) -> list[str]:
    """Advance one bounded, visible regional process on the action clock."""
    elapsed = state.pressure_elapsed
    process_elapsed = max(
        0, elapsed - (6 if "ebbglass-measure" in state.drink_effects else 0)
    )
    messages: list[str] = []
    if state.active_region_id == "hearthford":
        if 45 <= elapsed % 120 < 70:
            weather = "river fog"
        elif 70 <= elapsed % 120 < 95:
            weather = "hard rain"
        else:
            weather = "clear"
    elif state.active_region_id == "greywash":
        weather = "coast squall" if 28 <= elapsed % 90 < 55 else "salt wind"
    elif state.active_region_id == "greenwold":
        weather = "forest rain" if 32 <= elapsed % 96 < 62 else "crosswind"
    elif state.active_region_id in {"dunmire", "marlbank"}:
        weather = "hard rain" if 32 <= elapsed % 96 < 62 else "river fog" if elapsed % 96 < 16 else "clear"
    elif state.active_region_id == "frostmere":
        weather = "coast squall" if 28 <= elapsed % 90 < 55 else "salt wind"
    else:
        weather = "ridge gust" if 30 <= elapsed % 90 < 60 else "clear"
    if weather != state.weather:
        state.weather = weather
        messages.append({
            "clear": "The weather opens; long sightlines return.",
            "river fog": "River fog closes floodplain sightlines.",
            "hard rain": "Hard rain slows exposed travel and feeds low water.",
            "coast squall": "A coast squall salts bowstrings and shortens the flats' sightlines.",
            "salt wind": "The salt wind clears long coastal sightlines.",
            "forest rain": "Forest rain muffles trails while making bow grips treacherous.",
            "crosswind": "A crosswind carries smoke and sound between Greenwold clearings.",
            "ridge gust": "A ridge gust exposes high shooters and makes scree footing uncertain.",
        }[weather])

    next_stage = sum(
        process_elapsed >= threshold for threshold in state.region.process_thresholds
    )
    if next_stage > state.region.process_stage:
        state.region.process_stage = next_stage
        if state.active_region_id == "greywash":
            if next_stage == 1:
                messages.append("White lines advance over the flats: the working tide has turned.")
            elif next_stage == 2:
                if not state.region.changes.get("tide_held"):
                    for point in (Position(76, 40), Position(77, 40), Position(78, 40)):
                        state.water[position_key(point)] = 99
                    messages.append("The tide covers the low wreck road; the dune road and chain walk remain.")
                else:
                    messages.append("The dogged tide chain keeps the marked low wreck road exposed.")
            else:
                messages.append("The tide chain goes taut; late recovery now requires the upper windlass.")
        elif state.active_region_id == "greenwold":
            if next_stage == 1:
                messages.append("Birds lift downwind: burn smoke has begun crossing the southern clearing.")
            elif next_stage == 2:
                if not (
                    state.region.changes.get("burn_redirected")
                    or state.region.changes.get("medicine_coppice_saved")
                ):
                    for point in (Position(79, 39), Position(80, 39), Position(80, 39, 1)):
                        state.smoke[position_key(point)] = 12
                    messages.append("The shifting wind carries smoke into the raised burnworks and level above.")
                else:
                    messages.append("The bounded burn leaves the medicine route clear of rising smoke.")
            else:
                messages.append("The medicine coppice is singed; the request changes from prevention to salvage.")
        elif state.active_region_id == "whitecairn":
            if next_stage == 1:
                messages.append("Loose limestone ticks down the switchback: the quarry face is moving.")
            elif next_stage == 2:
                if not (
                    state.region.changes.get("quarry_braced")
                    or state.region.changes.get("honest_bell")
                ):
                    for point in (Position(55, 36), Position(56, 36), Position(57, 36)):
                        state.region.tile_changes[position_key(point)] = "%"
                    messages.append("A bounded rockfall covers the direct quarry stair; the sink loop remains open.")
                else:
                    messages.append("The braced face holds; the direct quarry stair remains legible.")
            else:
                messages.append("The real quarry bell answers the false one; the lower braces begin to fail.")
        elif state.active_region_id == "hearthford":
            messages.append("The mill bell marks rising water; safe working time is visibly narrowing.")
        else:
            from .frontiers import frontier_process

            messages.extend(frontier_process(state))

    deadline = state.objective_deadline if state.active_region_id == "hearthford" else state.region.process_thresholds[-1]
    if (
        not state.objective_changed and process_elapsed >= deadline
        and state.objective_status in {"unoffered", "accepted", "altered"}
    ):
        state.objective_changed = True
        state.region.local_objective_changed = True
        state.region.changes["late_objective"] = True
        state.market[state.region.objective_commodity].demand += 1
        messages.append(f"{state.region.process_name.title()} changes the objective; local demand worsens.")
    if pressure(state).band == "critical" and not state.escalation_spawned:
        state.escalation_spawned = True
        state.region.changes["escalation_spawned"] = True
        escalation = next((t for t in state.combatants if t.status == "dormant"), None)
        if escalation:
            escalation.status = "watching"
        messages.append(
            f"High pressure wakes a stronger {state.region.name} threat; valuables and noise made it legible."
        )
    return messages


def _patrols(state: GameState) -> list[str]:
    messages: list[str] = []
    for threat in state.combatants:
        if threat.status != "watching" or not threat.patrol:
            continue
        target_index = (threat.patrol_index + 1) % len(threat.patrol)
        target = threat.patrol[target_index]
        for offset in range(1, len(threat.patrol) + 1):
            candidate_index = (threat.patrol_index + offset) % len(threat.patrol)
            candidate = threat.patrol[candidate_index]
            if candidate != threat.position and is_walkable(state, candidate, ignore_threat=True):
                target_index, target = candidate_index, candidate
                break
        moved = next_path_step(state, threat, target, stop_distance=0)
        if moved != threat.position:
            threat.position = moved
        if threat.position == target:
            threat.patrol_index = target_index
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
        previous_time = state.world_time
        state.world_time += 1
        record_calendar_crossings(state, previous_time)
        advance_living_world(state)
        from .materials import advance_materials
        from .regional_history import advance_production

        advance_production(state)
        advance_materials(state)
        if state.location == "jomon":
            from .ship_crises import advance_deck
            advance_deck(state)
        for sound in state.sound_events:
            sound.age += 1
        state.sound_events = [sound for sound in state.sound_events if sound.age <= 3]
        for ended in tick_statuses(state):
            state.add_message(ended, priority=0)
        for key in list(state.smoke):
            state.smoke[key] -= 1
            if state.smoke[key] <= 0:
                del state.smoke[key]
        if not state.combat_active:
            continue
        if state.location == "region":
            state.pressure_elapsed += 1
            state.region.local_elapsed = state.pressure_elapsed
        previously_watching = {actor.id for actor in state.combatants if actor.status == "watching"}
        messages = _weather_and_deadline(state) + _patrols(state) if state.location == "region" else []
        current = pressure(state)
        from .ecology import active_actors
        from .enemy_ai import sees_courier, heard_position

        for threat in active_actors(state):
            if not state.combat_active:
                break
            if threat.status == "watching" and not threat.patrol:
                seen = sees_courier(state, threat)
                heard = heard_position(state, threat)
                if seen or heard:
                    threat.last_known_position = state.position if seen else heard
                    messages.append(_activate(threat))
                elif threat.ecology or threat.duty:
                    result = _threat_action(state, threat, False)
                    if threat.position in field_of_view(state, remember=False):
                        messages.append(result)
            elif threat.status == "engaged":
                if threat.id in previously_watching:
                    continue
                result = _threat_action(state, threat, guarded and tick == 0)
                if threat.position in field_of_view(state, remember=False) or distance(state.position, threat.position) <= 6:
                    messages.append(result)
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
    if state.voyage_status == "active":
        return ActionResult(False, False, "Jomon is still on passage; resolve the voyage before landing.", "voyage")
    if state.courier is None or not state.courier.alive:
        return _plain(state, "Choose an eligible courier by speaking to them in the tavern.")
    if state.weapon is None or state.gear is None or state.support is None:
        return _plain(
            state,
            "Prepare weapon and gear in the inventory, and crew support at the bar.",
        )
    route_node = state.route_nodes.get(state.route_current_node)
    if route_node is None or route_node.region_id != state.active_region_id:
        return ActionResult(False, False, "This is a bounded route stop, not a regional expedition landing.", "route-stop")
    state.location, state.current_room = "region", state.active_region_id
    state.position = state.region.landmarks["landing"]
    state.expedition_count += 1
    state.pressure_elapsed = state.noise = 0
    state.support_spent = state.guarded_step = False
    state.crossbow_loaded, state.aimed_target = True, None
    state.weather, state.smoke, state.water = "clear", {}, {}
    from .regions import reconstruct_regional_process

    reconstruct_regional_process(state)
    state.merchant_present, state.merchant_stock = False, []
    state.merchant.available = False
    merchant_schedule = state.actor_schedules.get(state.merchant.id)
    if merchant_schedule:
        merchant_schedule.available = False
        merchant_schedule.activity = "away on a regional circuit"
    field_of_view(state)
    state.remember(
        f"Expedition {state.expedition_count}: {state.courier.name} crossed into {state.region.name}."
    )
    return _time_result(
        state,
        f"You cross Jomon's gangplank into {state.region.name}; the region extends beyond the viewport.",
        priority=3,
    )


def _fall(state: GameState) -> str:
    if state.position.z <= -1:
        return ""
    landing = Position(state.position.x, state.position.y, state.position.z - 1)
    if not is_walkable(state, landing, ignore_threat=True):
        return "The opening has no landing below."
    state.position = landing
    if "fall sail" in state.carried_passives and state.gear == "rope":
        lateral = Position(landing.x + 1, landing.y, landing.z)
        if is_walkable(state, lateral, ignore_threat=True):
            state.position = lateral
            return "The fall sail turns the drop into a lateral rope swing."
    if "gull cord" in state.carried_passives and state.carried_goods:
        return "The gull cord lowers courier and one secured cargo stack together."
    if "cliff cord" in state.carried_passives:
        return "The cliff cord turns the fall into a controlled descent."
    dropped = next(
        (
            item for item in state.items
            if item.owner_id == state.active_courier_id
            and item.location == "pack"
            and item.kind.startswith("commodity:")
        ),
        None,
    )
    cargo_text = ""
    if dropped:
        dropped.location, dropped.owner_id = "ground", None
        dropped.region_id, dropped.ground_position = state.active_region_id, landing
        cargo_text = f" The unsecured {item_spec(dropped.kind).name} lands on the floor below."
        sync_legacy_load(state)
    return "You fall through the opening. " + apply_damage(state, 2, "The fall") + cargo_text


def move(state: GameState, dx: int, dy: int) -> ActionResult:
    if state.world_ended or (dx == 0 and dy == 0):
        return _plain(state, "No action is possible.")
    target = Position(
        state.position.x + dx, state.position.y + dy, state.position.z
    )
    if state.location == "jomon":
        from .people import person_at

        person = person_at(state, target)
        if person:
            return _plain(state, f"{person.name} occupies that place; interact from beside them.")
    occupant = next(
        (
            threat
            for threat in state.combatants
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
    kept_roof_aim = bool(
        state.location == "region" and state.aimed_target
        and state.position.z > 0 and "roof nail" in state.carried_passives
    )
    if state.location == "region" and state.aimed_target and not kept_roof_aim:
        state.aimed_target = None
    state.position = target
    if state.location == "region":
        state.last_move_turn = state.world_time
        if "lane-denied" in state.terrain_statuses:
            state.noise += 1
        if "hearth-ale" in state.drink_effects:
            state.noise += 1
    messages: list[str] = []
    if kept_roof_aim:
        messages.append("The roof nail holds the prepared lane through one careful upper-level move.")
    tile = base_tile(state, target)
    if state.location == "jomon":
        if tile == "+" and target != JOMON_GANGPLANK:
            state.vessel_tiles[position_key(target)] = "/"
        if state.combat_active:
            state.aimed_target = None
            state.last_move_turn = state.world_time
            emit_sound(state, 1 + armour_noise(state))
            return _time_result(state, "", steps=2 if load_state(state) == "overloaded" else 1)
        return _plain(state, "", changed=True)
    if state.location == "region" and tile == "+":
        state.region.tile_changes[position_key(target)] = "/"
        messages.append("You open the door; interior sightlines change.")
    quiet = state.courier and (
        state.courier.technique == "quiet passage"
        or (
            state.courier.technique == "wind listener"
            and state.active_region_id == "greenwold"
            and state.weather == "crosswind"
        )
        or "surveyed soft-step" in build_combinations(state)
        or ("smoke spoor" in state.courier.learned_techniques and position_key(target) in state.smoke)
    )
    if tile == "m" and not (
        state.gear == "quiet shoes"
        or "reed sole wraps" in state.carried_passives
        or "mudproof" in worn_tags(state, ("feet",))
    ):
        messages.append("Mud drags at your step; sound carries.")
        messages.extend(emit_sound(state, 1, target))
    elif not quiet and state.pressure_elapsed % 8 == 7:
        if "quiet" not in worn_tags(state, ("feet",)) or tile in {"r", "q", "w"}:
            state.noise += 1
    armour_sound = armour_noise(state)
    if armour_sound and state.pressure_elapsed % max(2, 8 - armour_sound * 2) == 0:
        state.noise += 1
        messages.append("Worn armour makes this step audibly distinct.")
    status_message = apply_terrain_status(state, displayed_tile(state, target))
    if status_message:
        messages.append(status_message)
    injury_delay = bool(
        state.courier
        and {"legs", "feet"} & set(state.courier.injuries)
        and tile in {"m", "r", "q", "t", "w", ","}
        and "willow-bitter" not in state.drink_effects
    )
    if injury_delay:
        messages.append("The leg or foot injury makes this terrain cost another action.")
    water_delay = False
    if position_key(target) in state.water:
        protected = (
            state.gear == "rope"
            or (state.courier and "shoreline measure" in state.courier.learned_techniques)
            or "river hooks" in state.carried_passives
            or (state.courier and state.courier.technique == "sure footing")
            or (
                state.active_region_id == "greywash"
                and state.courier and state.courier.technique == "ebb reader"
            )
        )
        if not protected:
            water_delay = True
            messages.append("Released water makes the crossing slow and exposed.")
        if "smokeleaf-infusion" in state.drink_effects:
            state.noise += 1
            messages.append("Smokeleaf thirst makes the wet crossing audibly clumsy.")
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
    storm_delay = water_delay or burden_delay or injury_delay or (
        state.weather == "hard rain"
        and state.position.z == 0
        and "rain cape" not in state.carried_passives
    )
    status_delay = bool(
        {"bogged", "current", "net-drag", "brine-chill", "coalheart-chill", "fatigued"}
        & set(state.terrain_statuses)
    )
    mapped_shortcut = (
        "coppice map" in state.carried_passives
        and state.active_region_id == "greenwold" and tile == "t"
    )
    if mapped_shortcut:
        status_delay = False
        state.noise = max(0, state.noise - 1)
        messages.append("The coppice map identifies a firm gap through the dense growth.")
    mobility_delay = armour_mobility(state) >= 3 and tile in {"m", "r", "q", "t", "w", ","}
    guarded_step = state.guarded_step
    drink_delay = guarded_step and "miller-small-beer" in state.drink_effects
    state.guarded_step = False
    return _time_result(
        state,
        " ".join(message for message in messages if message),
        guarded=guarded_step,
        steps=2 if storm_delay or drink_delay or status_delay or mobility_delay else 1,
        priority=3 if messages else 0,
    )


def can_alter_objective(state: GameState) -> bool:
    courier = state.courier
    return bool(
        state.gear == "repair tools"
        or state.support in {"route survey", "carpenter rig"}
        or "stillroom-cordial" in state.drink_effects
        or (courier and courier.technique == "lever craft")
        or (
            state.active_region_id == "greywash"
            and courier and courier.technique == "ebb reader"
            and "tide ledger" in state.carried_passives
        )
        or state.contact.disposition >= 2
    )


def decide_objective(state: GameState, decision: str) -> ActionResult:
    contact_schedule = state.actor_schedules.get(state.contact.id)
    contact_position = contact_schedule.position if contact_schedule and contact_schedule.area == f"region:{state.active_region_id}" else state.region.landmarks["contact"]
    available = (
        state.location == "region"
        and distance(state.position, contact_position) <= 1
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
        text = f"{state.courier.name} refuses {state.region.name}'s difficult request."
    elif decision == "alter":
        state.objective_status = "altered"
        text = f"{state.courier.name} alters the request to flood-control work."
    else:
        return _plain(state, "Unknown objective decision.")
    from .quests import record_objective_decision

    record_objective_decision(state, decision)
    _remember_contact(state, text)
    state.region.local_objective_status = state.objective_status
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
    state.region.local_objective_status = "completed"
    state.contact.disposition = min(3, state.contact.disposition + (1 if late else 2))
    market = state.market[state.region.objective_commodity]
    market.stock += 1 if altered or late else 2
    market.demand = max(0, market.demand - (1 if late else 2))
    state.trade_credit += 1 if late else 2
    state.region.changes["objective_altered"] = altered
    if altered:
        method = "control work"
    elif late:
        method = "late cargo delivery"
    else:
        method = "accountable delivery"
    memory = f"{state.courier.name} completed {state.region.name}'s request by {method}."
    _remember_contact(state, memory)
    state.remember(memory)
    from .quests import record_objective_completion

    record_objective_completion(state, altered)
    return f"{state.region.name} records the {method}; stock and demand visibly change."


def _open_container(state: GameState) -> ActionResult:
    container = next(
        (item for item in state.region.containers if item.position == state.position),
        None,
    )
    if container is None:
        return _plain(state, "No material container is here.")
    if container.opened and container.item_ids:
        return ActionResult(
            False, False, f"{container.name} remains open; arrange what was left.",
            f"inventory:container:{container.id}",
        )
    if container.opened:
        return _plain(state, "The container is already empty.")
    requirement = container.requirement
    if requirement == "rope" and state.gear != "rope" and "river hooks" not in state.carried_passives:
        return _plain(state, "The cache needs a rope or river hooks.")
    if requirement == "light" and state.gear != "hooded lantern" and state.lamp_oil <= 0:
        return _plain(state, "The buried marks cannot be read without finite light.")
    if requirement == "key" and state.gear != "repair tools" and not (
        state.courier and state.courier.technique == "lever craft"
    ) and not ({"wreck key", "chalk cipher"} & set(state.carried_passives)) and not (
        {"charcoal key", "limestone wedge"} & set(state.consumables)
    ):
        return _plain(state, "The strongbox needs repair tools or lever craft.")
    if requirement == "rope" and state.gear == "rope" and "flood rig" not in build_combinations(state):
        if state.rope_uses <= 0:
            return _plain(state, "The expedition rope has no sound length remaining.")
        state.rope_uses -= 1
    if requirement == "light" and state.gear != "hooded lantern":
        state.lamp_oil -= 1
    rewards = [container.reward, *container.extra_rewards]
    packed: list[str] = []
    left: list[str] = []
    for reward in rewards:
        try:
            item_spec(reward)
            physical_kind = reward
        except KeyError:
            physical_kind = (
                f"passive:{reward}" if reward in PASSIVES else
                f"relic:{reward}" if reward in RELICS else
                f"consumable:{reward}"
            )
        physical = create_item(
            state,
            physical_kind,
            f"{container.name}, {state.active_region_id}",
            location="container",
        )
        physical.container_id = container.id
        container.item_ids.append(physical.id)
        fits_pack = state.auto_place_enabled and auto_place(
            state, physical.id, "pack", owner_id=state.active_courier_id
        )
        if fits_pack:
            container.item_ids.remove(physical.id)
            record_acquisition(state, physical)
            if reward == "sealed tally":
                state.trade_credit += 1
            packed.append(reward)
        else:
            left.append(reward)
    container.opened = True
    from .quests import record_container_opened

    record_container_opened(state, container.id)
    state.remember(
        f"{state.courier.name} opened {container.name} and found {', '.join(rewards)}."
    )
    message = f"You open {container.name}: {', '.join(rewards)}."
    if packed:
        message += f" Packed: {', '.join(packed)}."
    if left:
        message += f" Left visibly inside: {', '.join(left)}."
    _advance_world(state)
    state.add_message(message, priority=3)
    return ActionResult(True, True, message, f"inventory:container:{container.id}")


def _control_interaction(state: GameState) -> ActionResult:
    courier = state.courier
    efficient = (
        state.gear == "repair tools"
        or state.support == "carpenter rig"
        or (courier and courier.technique == "lever craft")
        or "sluice token" in state.carried_passives
        or (courier and {"mill hearing", "bell interval"} & set(courier.learned_techniques))
    )
    if state.active_region_id != "hearthford":
        state.region.changes["environment_control_used"] = True
        if state.active_region_id == "greywash":
            state.water.clear()
            state.region.changes["tide_held"] = True
            text = "You dog the tide-chain windlass; the low route remains exposed for this working tide."
        elif state.active_region_id == "greenwold":
            state.smoke.clear()
            state.region.changes["burn_redirected"] = True
            text = "You turn the burn shutters crosswind; smoke, pursuit, and the objective route change."
        elif state.active_region_id == "whitecairn":
            state.region.changes["quarry_braced"] = True
            text = "You seat the quarry braces; falling stone quiets and the lower objective remains workable."
        else:
            from .frontiers import control_frontier

            text = control_frontier(state)
        sounds = emit_sound(state, 0 if efficient else 2)
        if state.objective_status == "altered":
            state.region.changes["objective_altered"] = True
        from .quests import record_environmental_control

        record_environmental_control(state)
        return _time_result(state, " ".join([text, *sounds]), priority=3)
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
    state.region.changes["environment_control"] = state.flood_control
    sounds = emit_sound(state, 0 if efficient else 3)
    state.region.changes["flood_control_used"] = True
    if state.objective_status == "altered":
        state.region.changes["mill_stabilised"] = True
    from .quests import record_environmental_control

    record_environmental_control(state)
    text = (
        f"The sluice is {state.flood_control}; water crosses culvert and ground "
        "openings, changing route safety."
    )
    return _time_result(state, " ".join([text, *sounds]), priority=3)


def _furnace_interaction(state: GameState) -> ActionResult:
    machinery = next((threat for threat in state.combatants if threat.profile == "machinery"), None)
    if machinery is None:
        above = Position(state.position.x, state.position.y, min(2, state.position.z + 1))
        state.smoke.update({position_key(state.position): 6, position_key(above): 6})
        return _time_result(
            state,
            "The work fire throws smoke upward; visibility and sound paths change on both levels.",
            priority=3,
        )
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
    braced = (
        "quarry brace" in state.carried_passives
        and load_state(state) in {"laden", "encumbered"}
    )
    fall = (
        "The weighted quarry brace holds the courier beside the new shaft."
        if braced else _fall(state)
    )
    return _time_result(
        state,
        " ".join(["The marked floor breaks into an open vertical shaft.", fall, *sounds]),
        priority=3,
    )


def interact(state: GameState) -> ActionResult:
    tile = base_tile(state, state.position)
    if state.location == "jomon":
        from .people import adjacent_person

        if state.jomon_space == "tavern" and state.pending_incident:
            participant_positions = [
                state.actor_schedules[actor_id].position
                for actor_id in state.pending_incident.participants
                if actor_id in state.actor_schedules
                and state.actor_schedules[actor_id].area == "tavern"
            ]
            if any(max(abs(point.x - state.position.x), abs(point.y - state.position.y)) <= 2 for point in participant_positions):
                return ActionResult(False, False, "A causal tavern incident needs a response.", "incident")
        # A courier standing on a physical control operates it even when its
        # scheduled worker is adjacent. Conversations remain available from
        # ordinary floor cells beside that worker.
        if state.jomon_space == "tavern" and tile == "+":
            state.jomon_space = "vessel"
            state.position = Position(TAVERN_ENTRANCE.x - 1, TAVERN_ENTRANCE.y, 0)
            return _plain(state, "You step from the common tavern onto Jomon's working deck.", changed=True)
        if state.jomon_space == "vessel":
            destination = vessel_vertical_destination(state.position)
            if destination:
                if not is_walkable(state, destination):
                    return _plain(state, "The other end of the hatch is occupied; clear it first.")
                direction = "ladder down" if destination.z < state.position.z else "stair up"
                state.position = destination
                if state.combat_active:
                    return _time_result(state, f"You use the {direction} under voyage pressure.")
                return _plain(state, f"You use Jomon's {direction} between aligned decks.", changed=True)
        from .ship_crises import station_action
        handled = station_action(state, tile)
        if handled is not None:
            return handled
        if state.combat_active and tile == "C":
            return _plain(state, "The tavern shelters off-duty adults during this declared deck crisis.")
        if tile == "+":
            return depart(state)
        if tile == "C":
            state.jomon_space = "tavern"
            state.position = Position(TAVERN_EXIT.x + 1, TAVERN_EXIT.y, 0)
            return _plain(state, "You enter Jomon's dedicated common tavern.", changed=True)
        if tile == "P":
            return ActionResult(
                False,
                False,
                "Resolve the voyage danger." if state.voyage_status == "active" else "Set Jomon's next regional destination.",
                "voyage" if state.voyage_status == "active" else "route-chart",
            )
        if tile == "L":
            return ActionResult(False, False, "Stores are readouts.", "equipment")
        if tile == "H":
            return ActionResult(False, False, "Inspect hold and local problem.", "hold")
        if tile == "s" and state.merchant_present:
            return ActionResult(False, False, "The deck merchant opens the counted visiting stock.", "merchant")
        if tile == "K":
            return ActionResult(False, False, "Read Jomon's bounded vessel chronicle.", "chronicle")
        station = {
            "G": "galley", "R": "repair", "b": "berths", "U": "bilge",
            "p": "provisions", "W": "workshop", "S": "storage",
            "N": "helm", "O": "lookout", "T": "gathering", "s": "market",
        }.get(tile)
        if station:
            return ActionResult(
                False, False, f"Inspect Jomon's {station} position.",
                f"station:{station}",
            )
        person = adjacent_person(state)
        if person:
            return ActionResult(False, False, f"Speak with {person.name}.", f"person:{person.id}")
        bartender_schedule = state.actor_schedules.get(state.bartender.id)
        if (
            bartender_schedule
            and bartender_schedule.area == current_area(state)
            and max(
                abs(bartender_schedule.position.x - state.position.x),
                abs(bartender_schedule.position.y - state.position.y),
            ) <= 1
        ):
            return ActionResult(False, False, f"Speak with {state.bartender.name}.", "bartender")
        return _plain(state, "Nothing here needs handling.")
    if state.position == state.region.landmarks["landing"]:
        return return_to_jomon(state)
    if (
        tile == "&"
        and state.active_region_id != "hearthford"
        and not state.region.changes.get("environment_control_used")
    ):
        # Whitecairn's quarry brace shares a hoist coordinate. Material work
        # takes the first interaction; the aligned ladder remains usable after.
        return _control_interaction(state)
    destination = vertical_destination(state, state.position)
    if destination:
        if load_state(state) == "overloaded" and destination.z > state.position.z:
            return _plain(state, "The overloaded pack makes this climb unsafe; repack or leave weight.")
        injured_climb = bool(
            state.courier and {"legs", "feet"} & set(state.courier.injuries)
            and destination.z > state.position.z
        )
        armour_climb = armour_mobility(state) >= 3 and destination.z > state.position.z
        blocker = next(
            (
                threat
                for threat in state.combatants
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
        from .quests import mark_elevated_lead

        marked_lead = mark_elevated_lead(state)
        return _time_result(
            state,
            f"You use the {link.name}; nearby levels remain spatially aligned."
            + (" Lower-limb injury or heavy armour makes the climb slow." if injured_climb or armour_climb else "")
            + (" Height reveals and marks a named treasure lead." if marked_lead else ""),
            steps=2 if injured_climb or armour_climb else 1,
            priority=3,
        )
    if any(item.position == state.position for item in state.region.containers):
        return _open_container(state)
    contact_schedule = state.actor_schedules.get(state.contact.id)
    contact_position = contact_schedule.position if contact_schedule and contact_schedule.area == f"region:{state.active_region_id}" else state.region.landmarks["contact"]
    if distance(state.position, contact_position) <= 1:
        from .quests import arc_available_here

        quest = state.questlines[state.active_region_id]
        if quest.stage == 2 and quest.status == "resolution":
            return ActionResult(
                False, False,
                f"{state.contact.name} is ready to settle the regional consequence.",
                "quest:regional",
            )
        if arc_available_here(state):
            return ActionResult(
                False, False,
                f"{state.contact.name} opens the compared regional account.",
                "quest:arc",
            )
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
            and (
                state.region.changes.get("mill_stabilised")
                or state.region.changes.get("objective_altered")
            )
        ):
            return _time_result(
                state, _complete_objective(state, True), priority=3
            )
        return ActionResult(
            False, False, f"Inspect {state.contact.name}'s standing.", "contact"
        )
    second = next(
        (
            contact for contact in state.contacts.get(state.active_region_id, [])[1:]
            if distance(state.position, (
                state.actor_schedules.get(contact.id).position
                if state.actor_schedules.get(contact.id)
                and state.actor_schedules[contact.id].area == f"region:{state.active_region_id}"
                else contact.position
            )) <= 1
        ),
        None,
    )
    if second:
        quest = state.questlines[state.active_region_id]
        if quest.stage == 2 and quest.status == "resolution":
            return ActionResult(
                False, False,
                f"{second.name} is ready to settle the regional consequence.",
                "quest:regional",
            )
        return ActionResult(
            False, False, f"Speak with {second.name}.",
            f"contact-service:{second.id}",
        )
    if tile == "R":
        if state.region.changes.get("objective_taken"):
            commodity = state.region.objective_commodity
            recoverable = next(
                (
                    item for item in state.items
                    if item.kind == f"commodity:{commodity}"
                    and item.region_id == state.active_region_id
                    and item.location in {"ground", "enemy"}
                ),
                None,
            )
            if recoverable:
                if recoverable.location == "ground" and recoverable.ground_position:
                    point = recoverable.ground_position
                    return _plain(
                        state,
                        f"The load is empty, but the lost {commodity} remains at "
                        f"{point.x},{point.y}, level {point.z:+d}; recover it with I.",
                    )
                carrier = next(
                    (threat for threat in state.combatants if threat.carrying_item_id == recoverable.id),
                    None,
                )
                return _plain(
                    state,
                    f"The load is empty; {carrier.name if carrier else 'a withdrawing thief'} "
                    f"still carries the physical {commodity}.",
                )
            if not state.region.changes.get("objective_replacement_taken"):
                state.region.changes["objective_replacement_taken"] = True
                state.region.changes["objective_taken"] = False
                state.contact.disposition = max(-3, state.contact.disposition - 1)
                _remember_contact(
                    state,
                    f"{state.courier.name} needed the worksite's last replacement load.",
                )
                state.add_message(
                    "The worksite releases one inferior replacement; another loss must be resolved by control work or accepted failure.",
                    priority=3,
                )
            else:
                return _plain(
                    state,
                    "No replacement remains. Return to the contact and alter the work, or return without it and accept failure.",
                )
        if state.objective_status != "accepted":
            return _plain(state, f"Accept {state.region.name}'s request before taking the cargo.")
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
        for threat in state.combatants
        if threat.status in {"watching", "engaged"}
        and distance(state.position, threat.position) <= attack_range
        and line_of_sight(state, state.position, threat.position)
    )
    return sorted(
        targets, key=lambda threat: (distance(state.position, threat.position), threat.id)
    )


WEAPON_RANGES = {
    "billhook": 2,
    "spear": 3,
    "cudgel": 1,
    "staff": 1,
    "hand axe": 1,
    "crossbow": 7,
    "longbow": 12,
    "sling": 9,
    "heavy crossbow": 14,
    "pike": 4,
    "paired knives": 1,
    "javelins": 7,
    "war hammer": 1,
    "weighted net": 4,
    "staff sling": 10,
    "hooked javelin": 6,
    "boar spear": 4,
    "handgonne": 9,
}
RANGED_WEAPONS = frozenset(
    {
        "crossbow", "longbow", "sling", "heavy crossbow", "javelins",
        "weighted net", "staff sling", "hooked javelin", "handgonne",
    }
)


def effective_weapon_range(state: GameState) -> int:
    from .workshop import active_part

    if state.weapon not in WEAPON_RANGES:
        return 0
    attack_range = WEAPON_RANGES[state.weapon]
    if "winter-juniper" in state.drink_effects and state.weapon in RANGED_WEAPONS:
        attack_range = max(3, attack_range - 3)
    if (
        {"chilled", "salt-grit", "lime-grit", "smoke-inhalation"} & set(state.terrain_statuses)
        and state.weapon in RANGED_WEAPONS
    ):
        attack_range = max(3, attack_range - 2)
    if (
        "wind-read aim" in build_combinations(state)
        and state.weather in {"salt wind", "crosswind", "ridge gust"}
    ):
        attack_range += 2
    if state.courier and state.courier.technique == "high arc" and state.weapon == "sling":
        attack_range += 2
    if state.weapon == "staff sling" and "sighting knot" in state.carried_passives:
        attack_range += 1
    if active_part(state, "retrieval cord"):
        attack_range = max(1, attack_range - 2)
    if active_part(state, "resin seal"):
        attack_range = max(1, attack_range - 1)
    if state.weapon in RANGED_WEAPONS and active_part(state, "ash wrap") and "smoke-inhalation" in state.terrain_statuses and not {"chilled", "salt-grit", "lime-grit"} & set(state.terrain_statuses):
        attack_range += 2
    if state.weapon in RANGED_WEAPONS and "narrow-sight" in worn_tags(state, ("head",)):
        attack_range = max(1, attack_range - 2)
    return attack_range


def attack(state: GameState, target_id: str | None = None) -> ActionResult:
    from .workshop import active_part, attack_effects

    if not state.combat_active or state.weapon is None:
        return _plain(state, "No readied attack is possible.")
    candidates = _attack_targets(state, effective_weapon_range(state))
    if target_id is not None:
        candidates = [target for target in candidates if target.id == target_id]
    else:
        candidates = [target for target in candidates if target.ecology != "prey"]
    if state.weapon in {"pike", "boar spear"}:
        candidates = [target for target in candidates if distance(state.position, target.position) >= 2]
    if state.weapon == "staff sling":
        candidates = [target for target in candidates if distance(state.position, target.position) >= 3]
    if not candidates:
        if state.weapon == "hand axe" and base_tile(state, state.position) == "d":
            return _destroy_floor(state)
        return _plain(state, "No visible hostile is within this weapon's reach.")
    target = candidates[0]
    original_target_position = target.position
    target.status = "engaged"
    ranged = state.weapon in RANGED_WEAPONS
    prepared = state.weapon in {"crossbow", "longbow", "heavy crossbow", "handgonne"}
    ammo_key = {
        "crossbow": "bolts", "longbow": "arrows", "sling": "sling stones",
        "heavy crossbow": "heavy bolts", "javelins": "javelins", "weighted net": "nets",
        "staff sling": "sling stones", "hooked javelin": "javelins",
        "handgonne": "handgonne charges",
    }.get(state.weapon)
    if ranged:
        if state.weapon == "crossbow" and not state.crossbow_loaded:
            return _plain(state, "The crossbow is unloaded; reload with G.")
        if state.weapon == "heavy crossbow" and state.weapon_ready < 2:
            return _plain(state, f"The arbalest needs {2 - state.weapon_ready} more guarded reload action(s).")
        if state.weapon == "handgonne" and state.weapon_ready < 2:
            return _plain(state, f"The handgonne needs {2 - state.weapon_ready} more guarded loading action(s).")
        if ammo_key and physical_ammunition(state, ammo_key) <= 0:
            return _plain(state, f"No {ammo_key} remain in the physical load.")
        lane_cover = cover_at(state, state.position, target.position)
        if lane_cover == "full":
            return _plain(state, "Structure fully blocks that projectile path.")
        if prepared and state.aimed_target != target.id:
            state.aimed_target = target.id
            return _time_result(
                state,
                f"You prepare {state.weapon} on the {target.name}; range {distance(state.position, target.position)}, {lane_cover} cover. Firing commits the next action.",
                priority=3,
            )
        if (
            state.weapon in {"crossbow", "longbow"}
            and state.weather in {"hard rain", "coast squall", "forest rain"}
            and "weatherproof aim" not in build_combinations(state)
            and "weatherfast grip" not in build_combinations(state)
            and not active_part(state, "resin seal")
        ):
            state.aimed_target = None
            return _time_result(
                state,
                "Wet weather spoils the committed string before release.",
                priority=3,
            )
        if ammo_key:
            if not consume_ammunition(state, ammo_key):
                return _plain(state, f"No physical {ammo_key} remain.")
        if state.weapon == "crossbow":
            state.crossbow_loaded = False
        if state.weapon == "heavy crossbow":
            state.weapon_ready = 0
        if state.weapon == "handgonne":
            state.weapon_ready = 0
        state.aimed_target = None
        damage, weapon_text, sound = {
            "crossbow": (3, "crossbow bolt", 3),
            "longbow": (3, "longbow arrow", 2),
            "sling": (1, "sling stone arcs over the lane", 2),
            "heavy crossbow": (5, "arbalest bolt tears through the lane", 5),
            "javelins": (2, "thrown javelin", 3),
            "weighted net": (0, "weighted net", 2),
            "staff sling": (2, "staff-sling stone arcs over low cover", 2),
            "hooked javelin": (2, "hooked javelin", 3),
            "handgonne": (4, "handgonne ball tears through smoke and cover", 6),
        }[state.weapon]
        ignores_partial = state.weapon in {"heavy crossbow", "staff sling", "handgonne"} or (
            state.weapon == "sling"
            and (
                "high sling arc" in build_combinations(state)
                or (state.courier and state.courier.technique == "high arc")
            )
        )
        if lane_cover == "partial" and not ignores_partial:
            damage = max(0, damage - 1)
            weapon_text += " glances from partial cover"
        if state.courier and ({"head", "hands"} & set(state.courier.injuries)):
            damage = max(0, damage - 1)
            weapon_text += " wavers through injury"
    else:
        damage = {
            "billhook": 2,
            "spear": 2,
            "cudgel": 1,
            "staff": 1,
            "hand axe": 3,
            "pike": 2,
            "paired knives": 2,
            "war hammer": 3,
            "boar spear": 2,
        }[state.weapon]
        weapon_text = state.weapon
        sound = 1 if state.weapon in {"cudgel", "staff"} else 2
        if "thorn-held momentum" in build_combinations(state):
            damage += 1
            target.morale -= 1
            weapon_text += " carries guarded thorn momentum"
            state.guarded_step = False
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
                from .inventory import release_enemy_possession
                release_enemy_possession(state, other)
        state.guarded_step = True
        weapon_text += " sweeps nearby space and readies movement"
    elif state.weapon == "hand axe":
        target.morale -= 1
        weapon_text += " breaks guard"
    elif state.weapon == "pike":
        target.position = _step_away(state, target)
        target.position = _step_away(state, target)
        state.guarded_step = True
        weapon_text += " braces a four-pace lane and drives the target back"
        target.intent = "disrupted by the pike brace"
    elif state.weapon == "boar spear":
        target.intent = "pinned outside close range by the crossbar brace"
        target.morale -= 2 if target.profile == "animal" else 1
        weapon_text += " sets a crossbar brace and pins the approach"
    elif state.weapon == "paired knives":
        state.guarded_step = True
        target.morale -= 1
        weapon_text += " cut twice before a mobile guarded step"
    elif state.weapon == "war hammer":
        target.morale -= 2
        target.position = _step_away(state, target)
        weapon_text += " crushes guard and knocks back"
        emit_sound(state, 2)
    elif state.weapon == "weighted net":
        target.intent = "entangled; loses a turn cutting free"
        target.morale -= 1
        weapon_text += " entangles movement without dealing harm"
        if state.courier and state.courier.technique == "cast bind":
            target.morale -= 1
            target.position = _step_toward(state, target, state.position)
            weapon_text += "; Cast Bind hauls the restrained target one pace"
    elif state.weapon == "hooked javelin":
        old_position = target.position
        target.position = _step_toward(state, target, state.position)
        target.intent = "disrupted by the hooked shaft"
        recovered = create_item(
            state, "consumable:throwing javelins",
            "recoverable hooked shaft from a committed throw", location="ground",
        )
        recovered.region_id = state.spatial_id
        recovered.ground_position = old_position
        weapon_text += " pulls the target and leaves its shaft visibly recoverable"
        if "retrieval cast" in build_combinations(state):
            if auto_place(
                state, recovered.id, "pack", owner_id=state.active_courier_id
            ):
                sync_legacy_load(state)
                weapon_text += "; rope and gullbone reel recover it immediately"
    elif state.weapon == "handgonne":
        smoke_points = (
            state.position,
            Position(state.position.x + 1, state.position.y, state.position.z),
        )
        state.smoke.update({position_key(point): 4 for point in smoke_points})
        target.morale -= 2
        weapon_text += " fills the firing place with powder smoke"
    elif state.weapon == "sling" and state.position.z > target.position.z:
        target.morale -= 1
        weapon_text += " from a high arc"
        if "high sling arc" in build_combinations(state) or (
            state.courier and state.courier.technique == "high arc"
        ):
            target.intent = "dazed by a plunging sling cast"
            weapon_text += " that ignores low cover and dazes"
    if (
        "high-ground drive" in build_combinations(state)
        and target.position.z < state.position.z
    ):
        target.position = _step_away(state, target)
    sound, fitting_text = attack_effects(state, original_target_position, sound, ammo_key)
    if fitting_text:
        weapon_text += "; " + fitting_text
    sounds = emit_sound(state, sound)
    target.health = max(0, target.health - damage)
    if target.health == 0 or (
        target.morale <= 0 and target.profile != "machinery"
    ):
        target.status = "defeated" if target.health == 0 else "retreated"
        target.intent = "removed from the route"
        from .inventory import release_enemy_possession
        recovered = release_enemy_possession(state, target)
        memory = f"{state.courier.name} defeated {target.name} with {state.weapon}."
        state.remember(memory)
        _remember_contact(state, memory)
        text = f"The {weapon_text} removes the {target.name} from the route.{recovered}"
    else:
        text = (
            f"The {weapon_text} deals {damage}; "
            f"{target.name} has {target.health}/{target.max_health}."
        )
    return _time_result(state, " ".join([text, *sounds]), priority=3)


def guard(state: GameState) -> ActionResult:
    if not state.combat_active:
        return _plain(state, "There is no expedition danger to guard against.")
    if state.weapon == "crossbow" and not state.crossbow_loaded:
        if physical_ammunition(state, "bolts") <= 0:
            return _plain(state, "No crossbow ammunition remains.")
        state.crossbow_loaded = True
        return _time_result(
            state,
            "You reload the crossbow behind a committed guarded posture.",
            guarded=state.gear == "buckler",
            priority=3,
        )
    if state.weapon == "heavy crossbow" and state.weapon_ready < 2:
        if physical_ammunition(state, "heavy bolts") <= 0:
            return _plain(state, "No heavy bolts remain.")
        state.weapon_ready += 1
        stage = "windlass set" if state.weapon_ready == 1 else "bolt seated and ready"
        return _time_result(
            state,
            f"Arbalest reload {state.weapon_ready}/2: {stage}.",
            guarded=state.gear == "buckler",
            priority=3,
        )
    if state.weapon == "handgonne" and state.weapon_ready < 2:
        if physical_ammunition(state, "handgonne charges") <= 0:
            return _plain(state, "No wrapped powder charges remain.")
        state.weapon_ready += 1
        stage = "powder and wad seated" if state.weapon_ready == 1 else "ball rammed and match sheltered"
        return _time_result(
            state,
            f"Handgonne loading {state.weapon_ready}/2: {stage}.",
            guarded=state.gear == "buckler",
            priority=3,
        )
    engaged = [
        threat for threat in state.combatants
        if threat.status == "engaged"
        and distance(state.position, threat.position) <= 7
    ]
    if not engaged:
        return _plain(state, "There is no immediate danger to guard against.")
    strong = (
        state.gear == "buckler"
        or state.weapon == "staff"
        or (state.courier and state.courier.technique == "set stance")
        or "hearth-ale" in state.drink_effects
        or ("brace" in worn_tags(state, ("arms",)) and "wet" not in state.terrain_statuses)
    )
    if state.courier and ({"arms", "hands"} & set(state.courier.injuries)):
        strong = False
    hindering = {"poor-footing", "smoke-inhalation", "net-drag", "lime-grit"} & set(state.terrain_statuses)
    if "thorn-scratched" in state.terrain_statuses and "thorn weave" not in state.carried_passives:
        hindering.add("thorn-scratched")
    if hindering:
        strong = False
    if "wet" in state.terrain_statuses and not ({"grip", "tool-grip"} & worn_tags(state, ("hands", "arms"))):
        strong = False
    if strong:
        morale_loss = 2 if "shielded set stance" in build_combinations(state) else 1
        if "hearth-ale" in state.drink_effects:
            morale_loss += 1
        for threat in engaged:
            threat.morale -= morale_loss
    state.guarded_step = (
        state.weapon == "staff" or "reed sole wraps" in state.carried_passives
        or (
            "thorn weave" in state.carried_passives
            and "thorn-scratched" in state.terrain_statuses
        )
        or "miller-small-beer" in state.drink_effects
    )
    if "shielded set stance" in build_combinations(state):
        text = "Buckler and set stance deny the attack and press hostile morale."
    elif strong:
        text = "You set a reinforced guard; the next reposition preserves control."
    else:
        text = "You guard and yield space deliberately."
    if "wet" in state.terrain_statuses:
        text += " Working grip holds the wet guard." if {"grip", "tool-grip"} & worn_tags(state, ("hands", "arms")) else " Wet hands weaken the guard; grip coverings or dry ground restore it."
    return _time_result(state, text, guarded=True, priority=3)


def use_gear(state: GameState) -> ActionResult:
    if not state.combat_active:
        return _plain(state, "Expedition gear is used in the field.")
    bottle = next(
        (
            item for item in state.items
            if item.owner_id == state.active_courier_id and item.location == "pack"
            and item.kind.startswith("consumable:bottle:")
        ),
        None,
    )
    if bottle:
        from .vessel import drink_bottled

        changed, message = drink_bottled(state, bottle.kind.split(":", 2)[2])
        return _time_result(state, message, priority=3) if changed else _plain(state, message)
    if (
        state.carried_relic == "tide-knot charm"
        and state.relics.get("tide-knot charm", 0)
    ):
        state.relics["tide-knot charm"] -= 1
        if state.relics["tide-knot charm"] == 0:
            del state.relics["tide-knot charm"]
        state.carried_relic = None
        consume_carried(state, "relic:tide-knot charm")
        for threat in state.combatants:
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
    if state.carried_relic == "ebbglass spindle" and state.relics.get("ebbglass spindle", 0):
        if state.location != "region":
            return _plain(state, "The ebbglass holds a regional process, not a vessel crisis; it remains unspent.")
        state.relics["ebbglass spindle"] -= 1
        if not state.relics["ebbglass spindle"]:
            del state.relics["ebbglass spindle"]
        state.carried_relic = None
        consume_carried(state, "relic:ebbglass spindle")
        state.region.process_thresholds = [threshold + 12 for threshold in state.region.process_thresholds]
        state.region.changes["ebbglass_spent"] = True
        return _time_result(
            state,
            "The finite ebbglass clouds while holding the next regional change for twelve actions.",
            priority=3,
        )
    if state.carried_relic == "coalheart seed" and state.relics.get("coalheart seed", 0):
        state.relics["coalheart seed"] -= 1
        if not state.relics["coalheart seed"]:
            del state.relics["coalheart seed"]
        state.carried_relic = None
        consume_carried(state, "relic:coalheart seed")
        state.smoke.clear()
        from .materials import fields
        for cell in fields(state).values():
            cell.smoke = 0
        add_status(state, "coalheart-chill", "spent warm mineral", 8, "wetness and exposed travel become dangerous")
        return _time_result(
            state,
            "The coalheart consumes every local smoke cell, then leaves a dangerous eight-action chill.",
            priority=3,
        )
    if state.carried_relic == "hollow-bell shard" and state.relics.get("hollow-bell shard", 0):
        state.relics["hollow-bell shard"] -= 1
        if not state.relics["hollow-bell shard"]:
            del state.relics["hollow-bell shard"]
        state.carried_relic = None
        consume_carried(state, "relic:hollow-bell shard")
        top = 1 if state.location == "jomon" else 2
        shifted = Position(state.position.x, state.position.y, max(-1, min(top, state.position.z + (1 if state.position.z < top else -1))))
        sounds = emit_sound(state, 4, shifted)
        return _time_result(
            state,
            " ".join(["The hollow shard moves one loud strike to an adjacent level; unintended listeners may answer.", *sounds]),
            priority=3,
        )
    if state.carried_relic == "stillwater filament" and state.relics.get("stillwater filament", 0):
        state.relics["stillwater filament"] -= 1
        if not state.relics["stillwater filament"]:
            del state.relics["stillwater filament"]
        state.carried_relic = None
        consume_carried(state, "relic:stillwater filament")
        state.water.clear()
        from .materials import fields
        for cell in fields(state).values():
            cell.water = 0
        changes = state.vessel_changes if state.location == "jomon" else state.region.changes
        changes["stillwater_filament_spent"] = True
        sounds = emit_sound(state, 5, state.position)
        return _time_result(
            state,
            " ".join([
                "The finite filament arrests every local current, then rings its last motion to nearby listeners.",
                *sounds,
            ]),
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
            state.position.z < (1 if state.location == "jomon" else 2)
            and vertical_open(state, state.position, above)
        ):
            points.append(above)
        state.smoke.update({position_key(point): 6 for point in points})
        for threat in state.combatants:
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
    if (
        "bird whistle" in state.carried_passives
        and not (
            "cache bell" in state.carried_passives
            and state.location == "region"
            and not state.region.changes.get("cache_bell_used")
        )
    ):
        decoy = Position(state.position.x + 4, state.position.y, state.position.z)
        sounds = emit_sound(state, 3, decoy)
        return _time_result(
            state,
            " ".join(["The bird whistle places a deliberate sound four paces crosswind from your true position.", *sounds]),
            priority=3,
        )
    if (
        "cache bell" in state.carried_passives
        and state.location == "region"
        and not state.region.changes.get("cache_bell_used")
    ):
        from .quests import mark_treasure

        cache = min(
            (container for container in state.region.containers if not container.opened),
            key=lambda container: (distance(state.position, container.position), container.id),
            default=None,
        )
        if cache:
            state.region.changes["cache_bell_used"] = True
            mark_treasure(
                state, state.active_region_id, cache.id,
                f"The cache bell answers {cache.name} at {cache.position.x},{cache.position.y}, level {cache.position.z:+d}.",
            )
            sounds = emit_sound(state, 4)
            return _time_result(
                state,
                " ".join([
                    f"The finite sounding marks {cache.name}, but every listener hears it.",
                    *sounds,
                ]),
                priority=3,
            )
    animal = next(
        (
            threat
            for threat in state.combatants
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
        amount = 5 if "scar salve recipe" in state.carried_passives else 3
        state.courier.health = min(state.courier.max_health, state.courier.health + amount)
        state.courier.injury = "treated soreness"
        return _time_result(
            state,
            f"A finite willow dressing restores {amount} health"
            + (" through the scar-salve method" if amount == 5 else "")
            + "; field healing remains scarce.",
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
    if state.consumables.get("dry lamp wick", 0):
        state.consumables["dry lamp wick"] -= 1
        if state.consumables["dry lamp wick"] == 0:
            del state.consumables["dry lamp wick"]
        consume_carried(state, "consumable:dry lamp wick")
        state.lamp_oil += 2
        return _time_result(state, "A dry wick restores two finite measures of sheltered light.", priority=3)
    if state.consumables.get("brine wash", 0) and ({"salt-grit", "cut-feet"} & set(state.terrain_statuses)):
        state.consumables["brine wash"] -= 1
        if state.consumables["brine wash"] == 0:
            del state.consumables["brine wash"]
        consume_carried(state, "consumable:brine wash")
        state.terrain_statuses.pop("salt-grit", None)
        state.terrain_statuses.pop("cut-feet", None)
        add_status(state, "brine-chill", "cold brine treatment", 3, "exposed wet travel is slower")
        return _time_result(state, "Brine clears grit and sharp-ground cuts, then leaves a short chill.", priority=3)
    if state.consumables.get("splint roll", 0) and state.courier and ({"arms", "legs"} & set(state.courier.injuries)):
        location = "arms" if "arms" in state.courier.injuries else "legs"
        state.consumables["splint roll"] -= 1
        if state.consumables["splint roll"] == 0:
            del state.consumables["splint roll"]
        consume_carried(state, "consumable:splint roll")
        state.courier.injuries[location] = f"splinted {location}"
        return _time_result(state, f"A finite splint stabilises the {location}; the injury still persists.", priority=3)
    if (
        state.courier and state.courier.technique == "green poultice"
        and state.consumables.get("pine resin dressing", 0)
        and state.courier.injuries
    ):
        location = sorted(state.courier.injuries)[0]
        consume_carried(state, "consumable:pine resin dressing")
        state.courier.injuries.pop(location, None)
        state.courier.health = min(state.courier.max_health, state.courier.health + 4)
        state.courier.injury = next(iter(state.courier.injuries.values()), "treated soreness")
        return _time_result(
            state,
            f"Green Poultice spends one resin dressing, clears the {location} injury, and restores four health.",
            priority=3,
        )
    return _plain(state, "No readied finite gear applies here.")


def negotiate(state: GameState) -> ActionResult:
    if not state.combat_active or state.courier is None:
        return _plain(state, "No negotiation is possible here.")
    humans = sorted([
        threat for threat in state.combatants
        if threat.status == "engaged"
        and threat.profile in {"pursuer", "reach", "ranged"}
        and distance(state.position, threat.position) <= 4
    ], key=lambda threat: (distance(state.position, threat.position), threat.id))
    if not humans:
        return _plain(state, "No human obstruction is close enough to hear terms.")
    has_terms = (
        state.courier.technique == "measured terms"
        or state.gear == "trade seals"
        or state.support == "factor surety"
        or "paper" in state.carried_goods
        or "valuable leverage" in build_combinations(state)
        or "stillroom-cordial" in state.drink_effects
    )
    if not has_terms:
        return _plain(
            state,
            "You lack witnessed seals, material surety, paper, or valuable leverage.",
        )
    speaker = humans[0]
    witnessed = bool(state.objective_evidence) or state.objective_status in {
        "altered", "completed",
    }
    if speaker.elite and not witnessed:
        return _plain(
            state,
            "This leader will not accept broad terms without witnessed regional evidence.",
        )
    violence_started = any(threat.health < threat.max_health for threat in humans)
    if violence_started and speaker.morale > 2 and not witnessed:
        return _plain(
            state,
            "After violence begins, material terms need broken morale or witnessed evidence.",
        )
    if speaker.group:
        heard = [
            threat for threat in humans
            if threat.group == speaker.group
            and distance(speaker.position, threat.position) <= 3
        ][:2]
    else:
        heard = [speaker]
    if "paper" in state.carried_goods and state.gear != "trade seals":
        if not consume_carried(state, "commodity:paper"):
            state.carried_goods["paper"].quantity -= 1
            if state.carried_goods["paper"].quantity == 0:
                del state.carried_goods["paper"]
    for threat in heard:
        threat.status, threat.intent = "negotiated", "accepts witnessed terms"
    drawback = ""
    if state.location == "region" and "stillroom-cordial" in state.drink_effects and state.contact.disposition <= 0:
        state.contact.disposition = max(-3, state.contact.disposition - 1)
        drawback = " The wary contact remembers the visible intoxication."
    memory = (
        f"{state.courier.name} settled {len(heard)} nearby group obstruction(s) "
        "through material terms."
    )
    state.remember(memory)
    _remember_contact(state, memory)
    return _time_result(
        state,
        f"Witnessed material terms settle {len(heard)} nearby member(s) of one group; other actors keep their own goals." + drawback,
        priority=3,
    )


def retreat(state: GameState) -> ActionResult:
    if state.location == "jomon" and state.combat_active:
        from .ship_crises import abandon_deck
        return _time_result(state, abandon_deck(state), priority=3)
    nearby = [
        threat for threat in state.combatants
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
    from .inventory import REGIONAL_ARMOUR

    context = {
        "ironwork": "hand axe",
        "timber": "cargo harness",
        "charcoal": "hooded lantern",
        "lime": "war hammer",
        "grain": "willow dressing",
        "paper": "trade seals",
        "salt fish": "weighted net",
        "wool": "quiet shoes",
    }[state.region.objective_commodity]
    outcome = "crossbow" if state.objective_status == "completed" else "smoke pot"
    regional_weapon = {
        "whitecairn": "heavy crossbow" if state.objective_status == "completed" else "pike",
        "greywash": "weighted net",
        "greenwold": "longbow",
    }.get(state.active_region_id, outcome)
    rare = (
        state.objective_status == "completed"
        and stage_rng(
            state.seed, f"merchant-stock:{state.returned_expeditions}"
        ).randrange(5) == 0
    )
    finite = "tide-knot charm" if rare else "willow dressing"
    stock = list(dict.fromkeys((context, regional_weapon, finite)))[:3]
    if state.active_region_id in REGIONAL_ARMOUR:
        clothing = REGIONAL_ARMOUR[state.active_region_id]
        stock.append(stage_rng(state.seed, f"work-clothing:{state.active_region_id}:{state.returned_expeditions}").choice(clothing))
    return stock


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
        f"Jomon exchanged {cost} credit for {item} from {state.merchant.name}."
    )
    state.merchant.memories.append(
        f"Sold {item} to {state.courier.name} after {state.active_region_id}'s recorded outcome."
    )
    del state.merchant.memories[:-8]
    state.merchant.relationships[state.active_courier_id] = min(
        3, state.merchant.relationships.get(state.active_courier_id, 0) + 1
    )
    return _time_result(
        state, f"Purchased {item}; it persists aboard Jomon.", priority=3
    )


def return_to_jomon(state: GameState) -> ActionResult:
    if (
        state.location != "region"
        or state.position != state.region.landmarks["landing"]
    ):
        return _plain(state, f"Return requires {state.region.name}'s physical landing.")
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
    for item in state.items:
        if (
            item.owner_id == state.active_courier_id
            and item.location == "pack"
            and item.kind.startswith("commodity:")
        ):
            item.location, item.owner_id = "vessel_cargo", None
    sync_legacy_load(state)
    from .regions import store_active_region
    from .people import unlock_region_visitors

    completed_region = state.active_region_id
    store_active_region(state)
    state.location, state.current_room, state.position = (
        "jomon",
        None,
        JOMON_GANGPLANK,
    )
    state.jomon_space = "vessel"
    state.returned_expeditions += 1
    state.merchant_present = merchant_visit_due(
        state.seed, state.returned_expeditions
    )
    state.merchant_stock = merchant_stock_for(state) if state.merchant_present else []
    state.merchant.available = state.merchant_present
    merchant_schedule = state.actor_schedules.get(state.merchant.id)
    if merchant_schedule:
        merchant_schedule.available = state.merchant_present
        merchant_schedule.activity = (
            "trading from a counted berth"
            if state.merchant_present else "away on a regional circuit"
        )
    state.remember(
        f"{courier.name} returned physically through Jomon's gangplank; "
        "discoveries entered household stores."
    )
    visitors = unlock_region_visitors(state, completed_region)
    merchant = " A visiting merchant has tied alongside." if state.merchant_present else ""
    visitor_text = (" " + " ".join(visitors)) if visitors else ""
    return _time_result(
        state,
        "You cross the gangplank home; cargo, discoveries, terrain, and consequences "
        f"persist.{merchant}{visitor_text}",
        priority=3,
    )


def purchase_bar_drink(state: GameState, drink_id: str, *, bottle: bool) -> ActionResult:
    if state.location != "jomon" or state.jomon_space != "tavern":
        return _plain(state, "Drinks are served face to face at Jomon's bar.")
    changed, message = buy_drink(state, drink_id, bottle=bottle)
    if not changed:
        return _plain(state, message)
    return _time_result(state, message, priority=3)


def resolve_regional_quest_choice(state: GameState, choice: str) -> ActionResult:
    from .quests import resolve_regional_quest

    changed, message = resolve_regional_quest(state, choice)
    return _time_result(state, message, priority=3) if changed else _plain(state, message)


def resolve_cross_region_choice(state: GameState, choice: str) -> ActionResult:
    from .quests import resolve_arc_choice

    changed, message = resolve_arc_choice(state, choice)
    return _time_result(state, message, priority=3) if changed else _plain(state, message)


def use_contact_service(state: GameState, choice: str) -> ActionResult:
    from .quests import use_secondary_service

    changed, message = use_secondary_service(state, choice)
    return _time_result(state, message, priority=3) if changed else _plain(state, message)


def intervene_socially(state: GameState, response: str) -> ActionResult:
    if state.location != "jomon" or state.jomon_space != "tavern":
        return _plain(state, "Intervention requires the physical tavern.")
    changed, message = resolve_social_incident(state, response)
    if not changed:
        return _plain(state, message)
    return _time_result(state, message, priority=3)


def use_route_stop(state: GameState, response: str) -> ActionResult:
    node = state.route_nodes.get(state.route_current_node)
    if state.location != "jomon" or node is None or node.region_id:
        return _plain(state, "No intermediate route service is available here.")
    if response == "resupply":
        available_key = f"supply_available:{node.id}"
        available = int(state.vessel_changes.get(available_key, 0))
        if available <= 0:
            return _plain(state, "This stop's counted provisions are exhausted.")
        if state.trade_credit < 1:
            return _plain(state, "One credit is needed for witnessed provisions.")
        state.trade_credit -= 1
        state.vessel_changes[available_key] = available - 1
        stack = state.vessel_cargo.setdefault("grain", CommodityStack(0, "dry"))
        stack.quantity += 1
        return _time_result(state, f"{node.name} loads one dry grain lot for one credit.", steps=2, priority=3)
    if response == "trade":
        commodity = node.market_interest
        stack = state.vessel_cargo.get(commodity)
        if not commodity or stack is None or stack.quantity <= 0:
            return _plain(state, f"{node.name} seeks {commodity or 'no current cargo'}, which Jomon does not carry.")
        stack.quantity -= 1
        if stack.quantity == 0:
            del state.vessel_cargo[commodity]
        state.trade_credit += 2
        state.vessel_changes[f"market_served:{node.id}"] = True
        return _time_result(state, f"{node.name} takes one {commodity} lot; Jomon gains two credit.", steps=2, priority=3)
    if response == "sound":
        from .route_chart import neighbours

        revealed = [node_id for node_id in neighbours(state, node.id) if node_id not in state.route_known]
        if not revealed:
            return _plain(state, "Every connected leg is already charted.")
        state.route_known.extend(revealed)
        state.route_known = sorted(set(state.route_known))
        return _time_result(state, f"Fresh soundings reveal {', '.join(state.route_nodes[item].name for item in revealed)}.", priority=3)
    return _plain(state, "That route-stop work is not available.")
