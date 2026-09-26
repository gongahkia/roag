"""Direct deterministic actions for Jomon's vessel and four regions."""

from __future__ import annotations

from dataclasses import dataclass

from .content import COMMODITIES, GEAR, MERCHANT_ITEMS, PASSIVES, RELICS, SUPPORTS, WEAPONS
from .enemy_ai import next_path_step, raise_group_alert, retreat_step, select_goal
from .expanded_weapons import ARSENAL
from .item_presentation import item_display_name, item_display_name_or_legacy
from .action_presentation import action_format
from .practices import learned_practice_ids
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
    physical_ammunition,
    prepare_kind,
    protection_at,
    item_spec,
    InventoryTransaction,
    record_acquisition,
    sync_legacy_load,
    tick_statuses,
    transfer_to_grid,
    worn_tags,
)
from .state import (
    CommodityStack, GameState, Person, Position, SoundEvent, Threat,
    legacy_combat_damage_seed, legacy_threat_intent_id, stage_rng,
)
from .tavern_games import another_game_active
from .work_weapons import WORK_WEAPONS
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
FALL_DAMAGE_SEED = "The fall"  # Legacy deterministic damage seed; never pack-authored.


from .vessel import (
    HOUSEHOLD_SEATS,
    TABLE_PATRON_SEATS,
    TABLE_PLAYER_SEAT,
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
            f"{item_display_name(name)} {stack.quantity}" for name, stack in state.vessel_cargo.items()
        )
        text = f"Jomon hold: {goods}. {state.region.pressure}"
    else:
        text = f"{state.region.condition} {state.region.objective_text}"
    return _plain(state, text, overlay=text)


def choose_courier(state: GameState, person_id: str) -> ActionResult:
    person = next((candidate for candidate in state.household if candidate.id == person_id), None)
    if state.location != "jomon" or person is None or not person.alive:
        return _plain(state, action_format("social.courier.unavailable"))
    ensure_courier_basics(state, person)
    previous_id = state.active_courier_id
    if previous_id == person.id:
        state.support = state.support or "route survey"
        sync_legacy_load(state)
        return _plain(state, action_format("social.courier.ready", courier=person.name))
    old_position = state.position
    schedule = state.actor_schedules.get(person.id)
    seat = schedule.position if schedule and schedule.area == "tavern" else state.tavern_positions.pop(person.id, state.position)
    if previous_id and previous_id != person.id:
        occupied = {
            item.position for item in state.actor_schedules.values()
            if item.area == "tavern" and item.actor_id not in {previous_id, person.id}
        }
        if state.jomon_space == "tavern" and old_position != TABLE_PLAYER_SEAT:
            previous_position = old_position
        else:
            chairs = (*TABLE_PATRON_SEATS, *HOUSEHOLD_SEATS) if state.jomon_space == "tavern" else HOUSEHOLD_SEATS
            previous_position = next(
                (point for point in chairs if point not in occupied and point != seat),
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
    from .character_presentation import role_display_name

    return _plain(state, action_format("social.courier.selected", courier=person.name, role=role_display_name(person.role)), changed=True)


def recruit_person(state: GameState, person_id: str) -> ActionResult:
    if state.location != "jomon":
        return _plain(state, action_format("social.recruit.face_to_face"))
    from .people import recruit_visitor

    changed, message = recruit_visitor(state, person_id)
    if changed:
        person = next(candidate for candidate in state.household if candidate.id == person_id)
        ensure_courier_basics(state, person)
        message += action_format("social.recruit.kit")
    return _plain(state, message, changed=changed)


def defer_recruit(state: GameState, person_id: str) -> ActionResult:
    from .people import defer_visitor

    changed, message = defer_visitor(state, person_id)
    return _plain(state, message, changed=changed)


def choose_weapon(state: GameState, weapon: str) -> ActionResult:
    if state.location != "jomon" or weapon not in WEAPONS or weapon not in state.owned_weapons:
        return _plain(state, action_format("action.setup.weapon.unavailable"))
    if state.active_courier_id is None:
        return _plain(state, action_format("action.setup.weapon.courier_required"))
    transaction = InventoryTransaction.begin(state)
    household_ids = {person.id for person in state.household}
    if not any(
        item.kind == weapon and item.location not in {"lost", "destroyed"}
        and (item.location == "locker" or item.owner_id in household_ids)
        for item in state.items
    ):
        physical = create_item(state, weapon, action_format("action.item.origin.household"))
        if not auto_place(state, physical.id, "locker"):
            transaction.cancel(state)
            return _plain(state, action_format("action.setup.weapon.locker_full"))
    if not prepare_kind(state, weapon):
        transaction.cancel(state)
        return _plain(state, action_format("action.setup.weapon.pack_full"))
    supply = {
        "crossbow": "crossbow bolts", "longbow": "fletched arrows",
        "sling": "sling shot pouch", "heavy crossbow": "quarrel case",
        "weighted net": "casting net bundle",
        "staff sling": "sling shot pouch", "hooked javelin": "throwing javelins",
        "handgonne": "handgonne charges",
    }.get(weapon)
    if weapon in ARSENAL:
        from .inventory import AMMUNITION_ITEMS, WEAPON_AMMUNITION

        supply = WEAPON_AMMUNITION.get(weapon)
        supply = supply and AMMUNITION_ITEMS[supply].split(":", 1)[1]
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
            return _plain(state, action_format("action.setup.weapon.ammunition_pack_full"))
    state.weapon, state.crossbow_loaded, state.aimed_target = weapon, True, None
    state.weapon_ready = 2 if weapon in {"heavy crossbow", "handgonne"} or weapon in ARSENAL and ARSENAL[weapon].family == "gun" else 1
    sync_legacy_load(state)
    return _plain(state, action_format("action.setup.weapon.readied", weapon=WEAPONS[weapon][0]), changed=True)


def choose_gear(state: GameState, gear: str) -> ActionResult:
    if state.location != "jomon" or gear not in GEAR or gear not in state.owned_gear:
        return _plain(state, action_format("action.setup.gear.unavailable"))
    if state.active_courier_id is None:
        return _plain(state, action_format("action.setup.gear.courier_required"))
    household_ids = {person.id for person in state.household}
    if not any(
        item.kind == gear and item.location not in {"lost", "destroyed"}
        and (item.location == "locker" or item.owner_id in household_ids)
        for item in state.items
    ):
        physical = create_item(state, gear, action_format("action.item.origin.household"))
        if not auto_place(state, physical.id, "locker"):
            state.items.remove(physical)
            return _plain(state, action_format("action.setup.gear.locker_full"))
    if not prepare_kind(state, gear):
        return _plain(state, action_format("action.setup.gear.pack_full"))
    state.gear = gear
    return _plain(state, action_format("action.setup.gear.packed", gear=GEAR[gear][0]), changed=True)


def choose_support(state: GameState, support: str) -> ActionResult:
    if state.location != "jomon" or support not in SUPPORTS:
        return _plain(state, action_format("action.setup.support.unavailable"))
    state.support, state.support_spent = support, False
    return _plain(state, action_format("action.setup.support.prepared", support=SUPPORTS[support][0]), changed=True)


def choose_relic(state: GameState, relic: str | None) -> ActionResult:
    if state.location != "jomon" or (relic is not None and state.relics.get(relic, 0) <= 0):
        return _plain(state, action_format("action.setup.relic.unavailable"))
    state.carried_relic = relic
    return _plain(state, action_format("action.setup.relic.carried", relic=item_display_name_or_legacy(relic) if relic else "none"), changed=True)


def choose_passive(state: GameState, passive: str) -> ActionResult:
    if state.location != "jomon" or passive not in state.owned_passives:
        return _plain(state, action_format("action.setup.passive.unavailable"))
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
        return _plain(state, action_format("action.setup.passive.stowed", passive=item_display_name(passive)), changed=True)
    if passive_bulk(state) + PASSIVES[passive][0] > passive_capacity(state):
        return _plain(state, action_format("action.setup.passive.capacity", capacity=passive_capacity(state)))
    state.carried_passives[passive] = carried + 1
    physical = next(
        (item for item in state.items if item.kind == f"passive:{passive}" and item.location == "locker"),
        None,
    )
    if physical is None:
        physical = create_item(state, f"passive:{passive}", action_format("action.item.origin.returned"))
    if not transfer_to_grid(state, physical.id, "pack", owner_id=state.active_courier_id):
        state.carried_passives[passive] = carried
        if carried == 0:
            del state.carried_passives[passive]
        return _plain(state, action_format("action.setup.passive.cells"))
    return _plain(
        state, action_format("action.setup.passive.packed", passive=item_display_name(passive), quantity=state.carried_passives[passive]), changed=True
    )


def _step_toward(state: GameState, threat: Threat, target: Position) -> Position:
    return next_path_step(state, threat, target)


def _step_away(state: GameState, threat: Threat) -> Position:
    return retreat_step(state, threat)


def _intent_identity(threat: Threat) -> str:
    if not threat.intent_id:
        threat.intent_id = legacy_threat_intent_id(threat.intent)
    return threat.intent_id


def _set_combat_intent(threat: Threat, semantic_id: str, **values: object) -> str:
    """Persist display text while all combat identity stays engine-owned."""
    threat.intent_id = semantic_id
    threat.intent = action_format(semantic_id, **values)
    return threat.intent


def _activate(threat: Threat) -> str:
    from .frontier_elites import definition

    threat.status = "engaged"
    key = {"pursuer": "combat.intent.activate.pursuer", "reach": "combat.intent.activate.reach", "ranged": "combat.intent.activate.ranged", "animal": "combat.intent.activate.animal", "machinery": "combat.intent.activate.machinery"}.get(threat.profile, "combat.intent.activate.default")
    _set_combat_intent(threat, key)
    special = definition(threat)
    if special:
        _set_combat_intent(threat, "combat.intent.activate.elite", mode=special["mode"], charges=threat.supplies)
    elif threat.ecology == "prey":
        _set_combat_intent(threat, "combat.intent.activate.prey")
    elif threat.ecology == "predator":
        _set_combat_intent(threat, "combat.intent.activate.predator")
    elif threat.duty:
        _set_combat_intent(threat, "combat.intent.activate.duty", duty=threat.duty)
    return action_format("combat.threat.notice", threat=threat.name, intent=threat.intent)

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
                    action_format("action.sound.echo_bead", level=f"{threat.position.z:+d}")
                )
    return messages


def _lose_goods(state: GameState) -> str:
    if not state.carried_goods:
        return ""
    if state.support == "porter watch":
        return action_format("combat.defeat.loss.porter_watch")
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
        return action_format(
            "combat.defeat.loss.protected_cargo", protection=protection, kept=kept,
        )
    lose_matching_carried(state, {f"commodity:{name}" for name in names})
    state.carried_goods.clear()
    return action_format("combat.defeat.loss.cargo", cargo=", ".join(names))


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
    loss = action_format("combat.defeat.loss.items", items=", ".join(lost_names)) if lost_names else ""
    if preserved_cargo:
        loss += action_format(
            "combat.defeat.loss.preserved_cargo",
            item=item_spec(preserved_cargo.kind).name,
        )
    if state.objective_status in {"accepted", "altered"}:
        state.objective_status = "failed"
        state.contact.disposition = max(-3, state.contact.disposition - 1)
        _remember_contact(state, action_format(
            "combat.defeat.objective_failed", courier=courier.name, region=state.region.name,
        ))
    from .regions import store_active_region

    store_active_region(state)
    state.location, state.current_room, state.position = "jomon", None, JOMON_GANGPLANK
    if permanent:
        courier.alive, courier.health, courier.injury = False, 0, "dead"
        successor = _successor(state, courier)
        state.remember(action_format(
            "combat.defeat.memory.died", courier=courier.name, hazard=state.region.hazard,
        ))
        if successor is None:
            state.active_courier_id, state.world_ended = None, True
            return action_format("combat.defeat.no_successor", text=text, loss=loss)
        state.active_courier_id = successor.id
        successor.relationships[courier.id] = min(
            3, successor.relationships.get(courier.id, 0) + 1
        )
        ensure_courier_basics(state, successor)
        state.support = "route survey"
        sync_legacy_load(state)
        return action_format(
            "combat.defeat.successor", text=text, loss=loss, successor=successor.name,
        )
    courier.health, courier.injury = max(2, courier.max_health // 3), "deep cut"
    state.remember(action_format("combat.defeat.memory.escaped", courier=courier.name))
    return action_format("combat.defeat.injured", text=text, loss=loss)


def _hit_location(state: GameState, damage_kind: str, source_seed: str) -> str:
    exposed = ["torso", "arms", "legs", "head", "hands", "feet"]
    if "fall" in source_seed.lower():
        exposed = ["legs", "feet", "arms", "head"]
    elif "bolt" in source_seed.lower() or damage_kind == "pierce":
        exposed = ["torso", "arms", "head", "legs"]
    elif state.guarded_step:
        exposed = ["arms", "hands", "legs", "feet"]
    rng = stage_rng(state.seed, f"hit:{state.world_time}:{source_seed}:{state.position}")
    return exposed[rng.randrange(len(exposed))]


def apply_damage(
    state: GameState,
    amount: int,
    source: str,
    *,
    damage_kind: str = "blunt",
    location: str | None = None,
    source_seed: str | None = None,
) -> str:
    courier = state.courier
    if courier is None:
        return action_format("action.damage.no_courier")
    if state.support == "field care" and not state.support_spent:
        state.support_spent = True
        reduction = 3 if "deep field binding" in build_combinations(state) else 2
        amount = max(0, amount - reduction)
        if amount == 0:
            return action_format("combat.damage.field_care")
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
        return action_format("combat.damage.river_glass_ward")
    source_seed = source if source_seed is None else source_seed
    location = location or _hit_location(state, damage_kind, source_seed)
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
        protection_text = action_format("combat.damage.absorbed", armour=armour_name, absorbed=absorbed) if absorbed else action_format("combat.damage.exposed", location=location)
        return action_format("combat.damage.hit", source=source, location=location, damage=amount, protection=protection_text)
    fatal = already_hurt or pressure(state).band == "critical" or "crown wheel" in source_seed
    return _return_after_defeat(state, action_format("combat.damage.defeated", source=source, courier=courier.name), fatal)


BRACE_REACTION_WEAPONS = frozenset(
    {"spear", "pike", "boar spear", "forked pike", "glaive", "quarterstaff"}
)


def brace_target_legality(state: GameState, threat: Threat) -> tuple[bool, str]:
    from .world import courier_sees

    if state.weapon not in BRACE_REACTION_WEAPONS:
        return False, action_format("combat.brace.reason.weapon")
    if threat.status != "engaged" or not courier_sees(
        state, threat.position
    ):
        return False, action_format("combat.brace.reason.not_visible")
    if threat.position.z != state.position.z:
        return False, action_format("combat.brace.reason.cross_levels")
    gap = distance(state.position, threat.position)
    minimum = 2 if state.weapon in {"pike", "boar spear", "forked pike", "glaive"} else 1
    if gap < minimum:
        return False, action_format("combat.brace.reason.minimum_range", minimum=minimum)
    if gap > effective_weapon_range(state) + 2:
        return False, action_format("combat.brace.reason.too_far")
    return True, action_format("combat.brace.reason.ready")


def _resolve_brace_reaction(state: GameState, threat: Threat) -> str | None:
    if state.aimed_target != threat.id or state.weapon not in BRACE_REACTION_WEAPONS:
        return None
    gap = distance(state.position, threat.position)
    minimum = 2 if state.weapon in {"pike", "boar spear", "forked pike", "glaive"} else 1
    if (
        threat.position.z != state.position.z
        or not minimum <= gap <= effective_weapon_range(state)
        or not line_of_sight(state, state.position, threat.position)
    ):
        return None
    state.aimed_target = None
    from .enemy_equipment import harm_enemy

    damage = {
        "spear": 2, "pike": 2, "boar spear": 2, "forked pike": 1,
        "glaive": 2, "quarterstaff": 1,
    }[state.weapon]
    harm = harm_enemy(
        state, threat, damage, f"{state.courier.name}'s prepared {item_display_name_or_legacy(state.weapon)}",
        damage_kind="cut" if state.weapon == "glaive" else "pierce",
    )
    threat.morale -= 2 if threat.profile == "animal" else 1
    if not harm.defeated and threat.morale <= 0 and threat.profile != "machinery":
        threat.status = "retreated"
        _set_combat_intent(threat, "intent.brace.retreated")
    elif not harm.defeated:
        _set_combat_intent(threat, "intent.brace.checked")
    outcome = action_format("combat.brace.outcome.defeated") if harm.defeated else action_format(
        "combat.brace.outcome.checked", damage=harm.amount,
    )
    protection = (
        action_format("combat.brace.protected", protection=harm.protection, location=harm.location)
        if harm.protection != "uncovered" else action_format("combat.brace.uncovered", location=harm.location)
    )
    return action_format("combat.brace.reaction", weapon=item_display_name_or_legacy(state.weapon), outcome=outcome, threat=threat.name, protection=protection, dropped=harm.dropped)


def _threat_action(state: GameState, threat: Threat, guarded: bool) -> str:
    gap = distance(state.position, threat.position)
    threat.turn += 1
    from .enemy_equipment import (
        attack_penalty as enemy_attack_penalty,
        readied_weapon as enemy_readied_weapon,
        recover_ground_weapon,
        wear_readied_weapon,
    )
    if threat.uses_physical_equipment and enemy_readied_weapon(state, threat) is None:
        weapons = [
            item for item in state.items
            if item.location == "ground" and item.region_id == state.spatial_id
            and item.ground_position and item.condition > 0
            and item_spec(item.kind).category == "weapon"
            and distance(threat.position, item.ground_position) <= threat.vision
            and line_of_sight(state, threat.position, item.ground_position)
        ]
        if weapons:
            replacement = min(
                weapons,
                key=lambda item: (distance(threat.position, item.ground_position), item.id),
            )
            if distance(threat.position, replacement.ground_position) <= 1:
                return recover_ground_weapon(state, threat, replacement.ground_position)
            old = threat.position
            threat.position = next_path_step(
                state, threat, replacement.ground_position, stop_distance=1,
            )
            _set_combat_intent(
                threat, "intent.weapon.recover",
                weapon=item_spec(replacement.kind).name,
            )
            return action_format("combat.threat.intent", threat=threat.name, intent=threat.intent) if threat.position != old else ""
        threat.morale = min(0, threat.morale)
        threat.goal = "break contact"
        threat.goal_reason = action_format("combat.goal.break_contact.weapon_lost")
    if ({"legs", "feet"} & set(threat.injuries)) and threat.turn % 2 == 0:
        _set_combat_intent(threat, "combat.intent.favors_an_injured_lower_limb_and_loses_ground")
        return action_format("combat.threat.intent", threat=threat.name, intent=threat.intent)
    if _intent_identity(threat).startswith(("intent.disrupted", "intent.dazed", "intent.entangled", "intent.pinned")):
        was_entangled = _intent_identity(threat).startswith("intent.entangled")
        _set_combat_intent(threat, "combat.intent.cuts_free_of_the_net_before_acting_again") if was_entangled else _set_combat_intent(threat, "combat.intent.recovers_position_before_acting_again")
        threat.reaction, threat.marked_position = "", None
        return action_format("combat.threat.loses_turn", threat=threat.name, intent=threat.intent)
    from .frontier_elites import elite_action
    special = elite_action(state, threat, guarded)
    if special is not None:
        return special
    if threat.elite and threat.archetype_id == "hearth-elite-claimant":
        if (
            state.region.changes.get("mill_public_compact")
            or state.region.changes.get("flood_control_used")
        ):
            threat.morale -= 2
            _set_combat_intent(threat, "combat.intent.cannot_claim_a_publicly_witnessed_and_dogged_sluice")
            if threat.morale <= 0:
                threat.status = "retreated"
            return action_format("combat.elite.floodgate.leverage_denied")
        if threat.aimed_at is None:
            threat.aimed_at = state.position
            _set_combat_intent(
                threat, "intent.elite.floodgate.sluice_telegraph",
                x=state.position.x, y=state.position.y,
            )
            return action_format("combat.elite.floodgate.telegraph", threat=threat.name, intent=threat.intent)
        marked, threat.aimed_at = threat.aimed_at, None
        points = (
            Position(marked.x - 1, marked.y, marked.z), marked,
            Position(marked.x + 1, marked.y, marked.z),
        )
        state.water.update({position_key(point): 7 for point in points})
        if state.position in points and not guarded:
            return apply_damage(
                state, 3, action_format("combat.elite.floodgate.sluice_source"),
                damage_kind="blunt",
                source_seed=legacy_combat_damage_seed("elite.floodgate.sluice"),
            )
        return action_format("combat.elite.floodgate.safe")
    if threat.elite and state.active_region_id == "greywash":
        if threat.archetype_id == "coast-elite-wreck":
            if (
                state.region.changes.get("tide_held")
                or state.questlines["greywash"].optional_done
            ):
                threat.morale -= 2
                _set_combat_intent(threat, "combat.intent.cannot_claim_witnessed_wreck_property")
                if threat.morale <= 0:
                    threat.status = "retreated"
                return action_format("combat.elite.reeve.leverage_denied")
            if threat.aimed_at is None:
                threat.aimed_at = state.position
                _set_combat_intent(
                    threat, "intent.elite.reeve.cover_telegraph",
                    x=state.position.x, y=state.position.y,
                )
                return action_format("combat.elite.reeve.telegraph", threat=threat.name, intent=threat.intent)
            marked, threat.aimed_at = threat.aimed_at, None
            cover = Position(marked.x - 1, marked.y, marked.z)
            if is_walkable(state, cover, ignore_threat=True):
                state.region.tile_changes[position_key(cover)] = "."
            if state.position == marked and not guarded:
                return apply_damage(
                    state, 2, action_format("combat.elite.reeve.sling_source"),
                    damage_kind="blunt",
                    source_seed=legacy_combat_damage_seed("elite.reeve.sling"),
                )
            return action_format("combat.elite.reeve.safe")
        if state.region.changes.get("tide_held"):
            threat.morale -= 2
            _set_combat_intent(threat, "combat.intent.cannot_close_the_dogged_tide_chain")
            if threat.morale <= 0:
                threat.status = "retreated"
            return action_format("combat.elite.tide_chain.leverage_denied")
        if threat.turn % 2:
            _set_combat_intent(threat, "combat.intent.hauls_the_tide_chain_the_three_marked_flats_flood_next_turn")
            return action_format("combat.threat.intent", threat=threat.name, intent=threat.intent)
        from .geography import layout_point

        points = tuple(layout_point(state.region, point) for point in (Position(88, 28), Position(89, 28), Position(90, 28)))
        state.water.update({position_key(point): 8 for point in points})
        if state.position in points and not guarded:
            return apply_damage(
                state, 3, action_format("combat.elite.tide_chain.source"),
                damage_kind="blunt",
                source_seed=legacy_combat_damage_seed("elite.tide_chain"),
            )
        return action_format("combat.elite.tide_chain.safe")
    if threat.elite and state.active_region_id == "greenwold":
        if threat.archetype_id == "forest-elite-resin":
            if state.region.changes.get("medicine_coppice_saved"):
                threat.morale -= 2
                _set_combat_intent(threat, "combat.intent.will_not_burn_the_witnessed_medicine_stand")
                if threat.morale <= 0:
                    threat.status = "retreated"
                return action_format("combat.elite.tracker.leverage_denied")
            if threat.aimed_at is None:
                threat.aimed_at = state.position
                _set_combat_intent(
                    threat, "intent.elite.tracker.resin_telegraph",
                    x=state.position.x, y=state.position.y,
                )
                return action_format("combat.threat.intent", threat=threat.name, intent=threat.intent)
            marked, threat.aimed_at = threat.aimed_at, None
            smoke_points = (marked, Position(marked.x, marked.y, min(2, marked.z + 1)))
            state.smoke.update({position_key(point): 6 for point in smoke_points})
            if state.position == marked:
                add_status(
                    state, "smoke-inhalation",
                    action_format("combat.status.resin.cause"), 5,
                    action_format("combat.status.resin.consequence"),
                )
            return action_format("combat.elite.tracker.resin_result")
        if state.region.changes.get("burn_redirected"):
            threat.morale -= 2
            _set_combat_intent(threat, "combat.intent.loses_control_of_the_crosswind_burn")
            if threat.morale <= 0:
                threat.status = "retreated"
            return action_format("combat.elite.ash_cloak.leverage_denied")
        smoke_line = [
            Position(state.position.x + offset, state.position.y, state.position.z)
            for offset in (-1, 0, 1)
        ]
        state.smoke.update({position_key(point): 5 for point in smoke_line})
        _set_combat_intent(threat, "combat.intent.drives_smoke_across_three_paces_of_your_current_route")
        return action_format("combat.elite.ash_cloak.smoke", threat=threat.name, intent=threat.intent)
    if threat.elite and state.active_region_id == "whitecairn":
        if threat.archetype_id == "upland-elite-bridge":
            if state.region.changes.get("honest_bell"):
                threat.morale -= 2
                _set_combat_intent(threat, "combat.intent.cannot_break_a_crossing_under_the_honest_warning")
                if threat.morale <= 0:
                    threat.status = "retreated"
                return action_format("combat.elite.bellward.leverage_denied")
            if threat.aimed_at is None:
                threat.aimed_at = state.position
                _set_combat_intent(
                    threat, "intent.elite.bellward.floor_telegraph",
                    x=state.position.x, y=state.position.y, z=f"{state.position.z:+d}",
                )
                return action_format("combat.elite.bellward.telegraph", threat=threat.name, intent=threat.intent)
            marked, threat.aimed_at = threat.aimed_at, None
            if base_tile(state, marked) not in {"#", " ", "~"}:
                state.region.tile_changes[position_key(marked)] = "O"
            if state.position == marked:
                fall = _fall(state)
                return action_format("combat.elite.bellward.floor_break", fall=fall)
            return action_format("combat.elite.bellward.floor_safe")
        if state.region.changes.get("quarry_braced"):
            threat.morale -= 2
            _set_combat_intent(threat, "combat.intent.cannot_release_the_braced_rock_face")
            if threat.morale <= 0:
                threat.status = "retreated"
            return action_format("combat.elite.false_bell.leverage_denied")
        if threat.aimed_at is None:
            threat.aimed_at = state.position
            _set_combat_intent(
                threat, "intent.elite.false_bell.rockfall_telegraph",
                x=state.position.x, y=state.position.y,
            )
            return action_format("combat.threat.intent", threat=threat.name, intent=threat.intent)
        marked, threat.aimed_at = threat.aimed_at, None
        state.region.tile_changes[position_key(marked)] = "%"
        if state.position == marked and not guarded:
            return apply_damage(
                state, 3, action_format("combat.elite.false_bell.rockfall_source"),
                damage_kind="blunt",
                source_seed=legacy_combat_damage_seed("elite.false_bell.rockfall"),
            )
        return action_format("combat.elite.false_bell.safe")
    if threat.profile == "machinery":
        if gap > 7:
            return ""
        swept_rows = {22, 24, 26, 28}
        lane_id = "default"
        if threat.elite:
            outer = ((threat.turn - 1) // 2) % 2 == 0
            swept_rows = {22, 28} if outer else {24, 26}
            lane_id = "outer" if outer else "inner"
        lane = action_format(f"combat.machinery.lane.{lane_id}")
        threatened = state.position.y in swept_rows
        if threat.turn % 2:
            _set_combat_intent(threat, "intent.machinery.sweep_telegraph", lane=lane)
            return action_format("combat.machinery.telegraph", threat=threat.name, lane=lane)
        if threatened and not guarded:
            source = action_format(
                "combat.machinery.crown_wheel_source"
                if threat.elite else "combat.machinery.sweep_source"
            )
            source_seed = legacy_combat_damage_seed(
                "machinery.crown_wheel" if threat.elite else "machinery.sweep",
            )
            return apply_damage(
                state, 3 if threat.elite else 2, source, source_seed=source_seed,
            )
        return action_format("combat.machinery.safe", lane=lane)
    decision = select_goal(state, threat)
    from .ecology import resolve_world_action

    world_action = resolve_world_action(state, threat, decision)
    if world_action is not None:
        return world_action
    if decision.action == "attack":
        reaction = _resolve_brace_reaction(state, threat)
        if reaction is not None:
            return reaction
    if decision.action == "alarm":
        raise_group_alert(state, threat)
        _set_combat_intent(threat, "combat.intent.signals_allies_toward_your_last_known_position")
        return action_format("combat.threat.alarm", threat=threat.name)
    if decision.action == "control":
        if threat.aimed_at is None:
            threat.aimed_at = state.position
            _set_combat_intent(
                threat, "intent.controller.net_telegraph",
                x=state.position.x, y=state.position.y,
            )
            return action_format("combat.threat.intent", threat=threat.name, intent=threat.intent)
        marked, threat.aimed_at = threat.aimed_at, None
        if state.position == marked:
            add_status(
                state, "net-drag", action_format("combat.status.net.cause"), 3,
                action_format("combat.status.net.consequence"),
            )
            _set_combat_intent(threat, "combat.intent.hauls_the_marked_net_line")
            return action_format("combat.threat.net_hit", threat=threat.name)
        _set_combat_intent(threat, "combat.intent.recovers_the_empty_net_line")
        return action_format("combat.threat.net_miss", threat=threat.name)
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
        _set_combat_intent(threat, "combat.intent.feeds_smoke_into_a_short_lane_from_its_station")
        return action_format("combat.threat.smoke", threat=threat.name)
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
            wounded.goal_reason = action_format("combat.goal.cover_retreat")
        _set_combat_intent(threat, "combat.intent.covers_a_wounded_ally_s_marked_withdrawal")
        if threat.position == previous and not wounded:
            return ""
        return action_format("combat.threat.intent", threat=threat.name, intent=threat.intent)
    if decision.action in {"retreat", "withdraw"}:
        previous = threat.position
        if state.location == "jomon" and decision.action == "retreat" and threat.home_position:
            threat.position = next_path_step(state, threat, threat.home_position, stop_distance=1)
        else:
            threat.position = retreat_step(state, threat)
        _set_combat_intent(threat, "combat.intent.withdraws_toward_cover") if decision.action == "withdraw" else _set_combat_intent(threat, "combat.intent.breaks_contact", reason=decision.reason)
        if threat.position == previous:
            threat.stalled_turns += 1
            return "" if threat.stalled_turns > 1 else action_format("combat.threat.retreat_blocked", threat=threat.name)
        threat.stalled_turns = 0
        return action_format("combat.threat.intent", threat=threat.name, intent=threat.intent)
    if decision.action == "escape":
        target = threat.home_position or threat.position
        if distance(threat.position, target) <= 1:
            threat.status = "retreated"
            _set_combat_intent(threat, "combat.intent.escaped_with_stolen_cargo")
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
                    loss = action_format("combat.threat.escape_lost", item=item_display_name_or_legacy(stolen.kind))
                    state.remember(
                        action_format("combat.threat.escape_memory", threat=threat.name.title(), region=state.spatial_id, item=item_display_name_or_legacy(stolen.kind))
                    )
                threat.carrying_item_id = None
                sync_legacy_load(state)
            return action_format("combat.threat.escape", threat=threat.name, loss=loss)
        previous = threat.position
        threat.position = next_path_step(state, threat, target, stop_distance=0)
        return "" if threat.position == previous else action_format("combat.threat.cargo", threat=threat.name)
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
            _set_combat_intent(threat, "combat.intent.escapes_with_visible_stolen_cargo")
            return action_format("combat.threat.steal", threat=threat.name, item=item_display_name_or_legacy(stolen.kind))
    if decision.action == "investigate" and decision.target:
        previous = threat.position
        threat.position = next_path_step(state, threat, decision.target, stop_distance=0)
        if threat.position == previous:
            if threat.position == decision.target:
                threat.last_known_position = None
                threat.status = "watching"
                _set_combat_intent(threat, "combat.intent.finds_no_courier")
                return action_format("combat.threat.investigate_empty", threat=threat.name)
            threat.stalled_turns += 1
            return "" if threat.stalled_turns > 1 else action_format("combat.threat.investigate_blocked", threat=threat.name)
        threat.stalled_turns = 0
        _set_combat_intent(threat, "combat.intent.investigates_a_last_known_position")
        return action_format("combat.threat.investigate", threat=threat.name)
    if decision.action == "reload":
        threat.reload_turns = max(0, threat.reload_turns - 1)
        if threat.reload_turns:
            _set_combat_intent(
                threat, "intent.ranged.reloading",
                weapon=threat.ranged_kind, remaining=threat.reload_turns,
            )
        else:
            _set_combat_intent(threat, "intent.ranged.reloaded", weapon=threat.ranged_kind)
        return action_format("combat.threat.intent", threat=threat.name, intent=threat.intent)
    if decision.action in {"intercept", "patrol", "return", "approach", "flank", "seek elevation"} and decision.target:
        stop_distance = 1 if decision.action in {"intercept", "approach"} else 0
        previous = threat.position
        steps = pressure(state).pursuit_steps if decision.action == "approach" else 1
        for _ in range(steps):
            next_position = next_path_step(
                state, threat, decision.target, stop_distance=stop_distance
            )
            if next_position == state.position:
                break
            threat.position = next_position
        _set_combat_intent(threat, f"intent.ai.{decision.action.replace(' ', '_')}")
        if threat.position == previous:
            threat.stalled_turns += 1
            return "" if threat.stalled_turns > 1 else action_format("combat.threat.route_blocked", threat=threat.name)
        threat.stalled_turns = 0
        if decision.action == "patrol" and threat.patrol:
            next_index = (threat.patrol_index + 1) % len(threat.patrol)
            if threat.position == threat.patrol[next_index]:
                threat.patrol_index = next_index
        reaction = _resolve_brace_reaction(state, threat)
        if reaction is not None:
            return reaction
        return action_format("combat.threat.intent", threat=threat.name, intent=threat.intent)
    if decision.action == "wait":
        if _intent_identity(threat) == "combat.intent.holds_without_a_perceived_courier_position":
            return ""
        _set_combat_intent(threat, "combat.intent.holds_without_a_perceived_courier_position")
        return action_format("combat.threat.wait", threat=threat.name)
    if threat.profile == "ranged":
        if threat.position.z != state.position.z and not line_of_sight(
            state, threat.position, state.position
        ):
            if _intent_identity(threat) == "intent.ranged.tracks_sound":
                return ""
            _set_combat_intent(threat, "intent.ranged.tracks_sound")
            return action_format("combat.threat.cross_level", threat=threat.name)
        if not line_of_sight(state, threat.position, state.position):
            previous = threat.position
            threat.position = _step_toward(state, threat, state.position)
            _set_combat_intent(threat, "combat.intent.moves_for_a_clear_line")
            if threat.position == previous:
                threat.stalled_turns += 1
                return "" if threat.stalled_turns > 1 else action_format("combat.threat.firing_blocked", threat=threat.name)
            threat.stalled_turns = 0
            return action_format("combat.threat.firing_line", threat=threat.name)
        effective_range = {"longbow": 12, "sling": 9, "heavy crossbow": 14, "crossbow": 8}.get(threat.ranged_kind, 8)
        if gap <= effective_range:
            if threat.aimed_at is not None:
                aimed = threat.aimed_at
                threat.aimed_at = None
                threat.ammunition = max(0, threat.ammunition - 1)
                wear_readied_weapon(state, threat)
                threat.reload_turns = {"heavy crossbow": 2, "crossbow": 1, "longbow": 1, "sling": 0}.get(threat.ranged_kind, 1)
                _set_combat_intent(threat, "intent.ranged.must_reload", weapon=threat.ranged_kind)
                if aimed != state.position:
                    if threat.role == "suppressor":
                        add_status(
                            state, "lane-denied",
                            action_format("combat.status.lane.marked.cause"), 2,
                            action_format("combat.status.lane.marked.consequence"),
                        )
                    return action_format("combat.threat.shot_empty", threat=threat.name, x=aimed.x, y=aimed.y)
                lane_cover = cover_at(state, threat.position, state.position)
                if lane_cover == "full":
                    return action_format("combat.threat.shot_cover", threat=threat.name)
                if guarded or lane_cover == "partial":
                    if threat.role == "suppressor":
                        add_status(
                            state, "lane-denied",
                            action_format("combat.status.lane.cover.cause"), 2,
                            action_format("combat.status.lane.cover.consequence"),
                        )
                    return action_format("combat.threat.shot_guard", cover=lane_cover, weapon=threat.ranged_kind)
                harm = {"sling": 1, "longbow": 2, "crossbow": 2, "heavy crossbow": 4}.get(threat.ranged_kind, 2)
                if pressure(state).band == "critical":
                    harm += 1
                if threat.role == "shooter" and load_state(state) in {"encumbered", "overloaded"}:
                    harm += 1
                harm = max(0, harm - enemy_attack_penalty(state, threat))
                movement = ""
                if threat.role == "skirmisher":
                    old = threat.position
                    threat.position = retreat_step(state, threat)
                    movement = action_format("combat.threat.skirmish") if threat.position != old else ""
                kind = "blunt" if threat.ranged_kind == "sling" else "pierce"
                source_seed = legacy_combat_damage_seed(
                    "threat.ranged", threat=threat.name, weapon=threat.ranged_kind,
                )
                return apply_damage(
                    state, harm,
                    action_format("combat.threat.ranged_source", threat=threat.name, weapon=threat.ranged_kind),
                    damage_kind=kind, source_seed=source_seed,
                ) + movement
            threat.aimed_at = state.position
            _set_combat_intent(
                threat, "intent.ranged.aim_telegraph", weapon=threat.ranged_kind,
                x=state.position.x, y=state.position.y,
            )
            return action_format("combat.threat.intent", threat=threat.name, intent=threat.intent)
    if threat.profile == "animal" and gap <= 3:
        if base_tile(state, state.position) == "m" and _intent_identity(threat) in {"combat.intent.lowers_its_head_and_charges_next_turn", "intent.animal.charge_warning"}:
            threat.status = "evaded"
            _set_combat_intent(threat, "combat.intent.bogged_in_mud")
            state.remember(action_format(
                "combat.threat.animal_mud.memory",
                courier=state.courier.name, threat=threat.name,
            ))
            return action_format("combat.threat.animal_mud", threat=threat.name)
        if gap <= 1 and _intent_identity(threat) in {"combat.intent.lowers_its_head_and_charges_next_turn", "intent.animal.charge_warning"}:
            _set_combat_intent(threat, "combat.intent.circles_before_another_charge")
            if guarded:
                return action_format("combat.threat.animal_guard", threat=threat.name)
            source_seed = legacy_combat_damage_seed("threat.animal_charge", threat=threat.name)
            return apply_damage(
                state, 3,
                action_format("combat.threat.animal_charge_source", threat=threat.name),
                source_seed=source_seed,
            )
        _set_combat_intent(threat, "combat.intent.lowers_its_head_and_charges_next_turn")
        return action_format("combat.threat.animal_warning", threat=threat.name)
    preferred = 2 if threat.profile == "reach" else 1
    if gap <= preferred:
        if guarded:
            threat.morale -= 1
            return action_format("combat.threat.guard_denied", threat=threat.name)
        if _intent_identity(threat) == ("combat.intent.attack_warning_reach" if threat.profile == "reach" else "combat.intent.attack_warning_melee"):
            _set_combat_intent(threat, "combat.intent.recovers_before_another_attack")
            wear_readied_weapon(state, threat)
            harm = max(1, 3 - enemy_attack_penalty(state, threat))
            source_seed = legacy_combat_damage_seed("threat.melee", threat=threat.name)
            return apply_damage(
                state, harm,
                action_format("combat.threat.melee_source", threat=threat.name),
                source_seed=source_seed,
            )
        _set_combat_intent(threat, "combat.intent.attack_warning_reach" if threat.profile == "reach" else "combat.intent.attack_warning_melee")
        return action_format(
            "combat.threat.attack_warning.reach"
            if threat.profile == "reach" else "combat.threat.attack_warning.melee",
            threat=threat.name,
        )
    previous = threat.position
    for _ in range(pressure(state).pursuit_steps):
        threat.position = _step_toward(state, threat, state.position)
    if threat.position == previous:
        threat.stalled_turns += 1
        _set_combat_intent(threat, "combat.intent.holds_where_the_route_is_blocked")
        return "" if threat.stalled_turns > 1 else action_format("combat.threat.no_route", threat=threat.name)
    threat.stalled_turns = 0
    _set_combat_intent(
        threat,
        "intent.advance.fast" if pressure(state).pursuit_steps == 2 else "intent.advance.normal",
    )
    reaction = _resolve_brace_reaction(state, threat)
    if reaction is not None:
        return reaction
    return action_format("combat.threat.intent", threat=threat.name, intent=threat.intent)


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
        messages.append(action_format({
            "clear": "action.weather.clear", "river fog": "action.weather.river_fog",
            "hard rain": "action.weather.hard_rain", "coast squall": "action.weather.coast_squall",
            "salt wind": "action.weather.salt_wind", "forest rain": "action.weather.forest_rain",
            "crosswind": "action.weather.crosswind", "ridge gust": "action.weather.ridge_gust",
        }[weather]))

    next_stage = sum(
        process_elapsed >= threshold for threshold in state.region.process_thresholds
    )
    if next_stage > state.region.process_stage:
        state.region.process_stage = next_stage
        if state.active_region_id == "greywash":
            if next_stage == 1:
                messages.append(action_format("action.process.greywash.1"))
            elif next_stage == 2:
                if not state.region.changes.get("tide_held"):
                    from .geography import layout_point

                    for point in (layout_point(state.region, point) for point in (Position(76, 40), Position(77, 40), Position(78, 40))):
                        state.water[position_key(point)] = 99
                    messages.append(action_format("action.process.greywash.2"))
                else:
                    messages.append(action_format("action.process.greywash.2_held"))
            else:
                messages.append(action_format("action.process.greywash.3"))
        elif state.active_region_id == "greenwold":
            if next_stage == 1:
                messages.append(action_format("action.process.greenwold.1"))
            elif next_stage == 2:
                if not (
                    state.region.changes.get("burn_redirected")
                    or state.region.changes.get("medicine_coppice_saved")
                ):
                    from .geography import layout_point

                    for point in (layout_point(state.region, point) for point in (Position(79, 39), Position(80, 39), Position(80, 39, 1))):
                        state.smoke[position_key(point)] = 12
                    messages.append(action_format("action.process.greenwold.2"))
                else:
                    messages.append(action_format("action.process.greenwold.2_safe"))
            else:
                messages.append(action_format("action.process.greenwold.3"))
        elif state.active_region_id == "whitecairn":
            if next_stage == 1:
                messages.append(action_format("action.process.whitecairn.1"))
            elif next_stage == 2:
                if not (
                    state.region.changes.get("quarry_braced")
                    or state.region.changes.get("honest_bell")
                ):
                    from .geography import layout_point

                    for point in (layout_point(state.region, point) for point in (Position(55, 36), Position(56, 36), Position(57, 36))):
                        state.region.tile_changes[position_key(point)] = "%"
                    messages.append(action_format("action.process.whitecairn.2"))
                else:
                    messages.append(action_format("action.process.whitecairn.2_safe"))
            else:
                messages.append(action_format("action.process.whitecairn.3"))
        elif state.active_region_id == "hearthford":
            messages.append(action_format("action.process.hearthford"))
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
        messages.append(action_format("action.process.objective_changed", process=state.region.process_name.title()))
    if pressure(state).band == "critical" and not state.escalation_spawned:
        state.escalation_spawned = True
        state.region.changes["escalation_spawned"] = True
        escalation = next((t for t in state.combatants if t.status == "dormant"), None)
        if escalation:
            escalation.status = "watching"
            messages.append(action_format("action.process.escalation", threat=escalation.name))
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
        from .circuits import advance_circuits

        advance_circuits(state)
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
        from .worklines import apply_local_work
        apply_local_work(state)
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
        from .enemy_equipment import tick_enemy_conditions

        tick_enemy_conditions(state)
        for message in messages:
            if message:
                state.add_message(message, priority=3)
        from .frontier_elites import record_outcomes
        record_outcomes(state)
    if state.active_vehicle_id and state.vehicles[state.active_vehicle_id].position != state.position:
        state.active_vehicle_id = None
        state.add_message(action_format("action.vehicle.separated"), priority=3)
    if state.location == "region":
        field_of_view(state)
        new_band = pressure(state).band
        if old_band != new_band and new_band in {"strained", "critical"}:
            from .situations import activate_for_band

            activate_for_band(state, new_band)
            state.add_message(
                action_format("action.pressure.increased", band=new_band),
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
    from .vehicles import SHORE_DOCK

    by_tug = state.jomon_space == "harbour" and state.position == SHORE_DOCK and state.active_vehicle_id == "tug"
    from_gangplank = state.jomon_space != "harbour" and state.position == JOMON_GANGPLANK
    if state.location != "jomon" or not (by_tug or from_gangplank):
        return _plain(state, action_format("action.depart.invalid"))
    if state.returning_by_tug:
        return _plain(state, action_format("action.depart.returning_tug"))
    if state.voyage_status == "active":
        return ActionResult(False, False, action_format("action.depart.voyage_active"), "voyage")
    if state.courier is None or not state.courier.alive:
        return _plain(state, action_format("social.depart.courier_required"))
    if state.weapon is None or state.gear is None or state.support is None:
        return _plain(
            state,
            action_format("action.depart.preparation"),
        )
    route_node = state.route_nodes.get(state.route_current_node)
    if route_node is None or route_node.region_id != state.active_region_id:
        return ActionResult(False, False, action_format("action.depart.no_landing"), "route-stop")
    state.expedition_by_tug = by_tug
    if by_tug:
        state.active_vehicle_id = None
    state.location, state.current_room = "region", state.active_region_id
    state.position = state.region.landmarks["landing"]
    state.expedition_count += 1
    state.pressure_elapsed = state.noise = 0
    state.support_spent = state.guarded_step = False
    state.crossbow_loaded, state.aimed_target = True, None
    state.weather, state.smoke, state.water = "clear", {}, {}
    from .regions import reconstruct_regional_process

    reconstruct_regional_process(state)
    from .frontier_elites import revisit_claimants
    revisit_claimants(state)
    from .aftermath import prepare_aftermath

    prepare_aftermath(state)
    state.merchant_present, state.merchant_stock = False, []
    state.merchant.available = False
    merchant_schedule = state.actor_schedules.get(state.merchant.id)
    if merchant_schedule:
        merchant_schedule.available = False
        merchant_schedule.activity = "away on a regional circuit"
    field_of_view(state)
    from .situations import activate_for_band

    activate_for_band(state, "steady")
    state.remember(
        action_format("action.depart.memory", count=state.expedition_count, courier=state.courier.name, region=state.region.name)
    )
    return _time_result(
        state,
        action_format("action.depart.started.tug" if by_tug else "action.depart.started.gangplank", region=state.region.name),
        priority=3,
    )


def _fall(state: GameState) -> str:
    if state.position.z <= -1:
        return ""
    landing = Position(state.position.x, state.position.y, state.position.z - 1)
    if not is_walkable(state, landing, ignore_threat=True):
        return action_format("action.fall.no_landing")
    state.position = landing
    if "fall sail" in state.carried_passives and state.gear == "rope":
        lateral = Position(landing.x + 1, landing.y, landing.z)
        if is_walkable(state, lateral, ignore_threat=True):
            state.position = lateral
            return action_format("action.fall.sail")
    if "gull cord" in state.carried_passives and state.carried_goods:
        return action_format("action.fall.gull_cord")
    if "cliff cord" in state.carried_passives:
        return action_format("action.fall.cliff_cord")
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
        cargo_text = action_format("action.fall.cargo", item=item_spec(dropped.kind).name)
        sync_legacy_load(state)
    return action_format("action.fall.result", damage=apply_damage(state, 2, action_format("action.fall.source"), source_seed=FALL_DAMAGE_SEED), cargo=cargo_text)


def move(state: GameState, dx: int, dy: int) -> ActionResult:
    if state.world_ended or (dx == 0 and dy == 0):
        return _plain(state, action_format("action.movement.unavailable"))
    if state.active_vehicle_id:
        from .vehicles import navigate

        return navigate(state, dx, dy)
    target = Position(
        state.position.x + dx, state.position.y + dy, state.position.z
    )
    if state.location == "jomon":
        from .people import person_at

        person = person_at(state, target)
        if person:
            return _plain(state, action_format("social.move.person_occupies", person=person.name))
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
        return _plain(state, action_format("action.movement.threat_blocks", threat=occupant.name))
    if not is_walkable(state, target):
        from .inspection import blocked_step_reason

        reason, remedy = blocked_step_reason(state, target)
        return _plain(state, action_format("action.movement.blocked", reason=reason, remedy=remedy))
    if dx and dy:
        side_a = Position(state.position.x + dx, state.position.y, state.position.z)
        side_b = Position(state.position.x, state.position.y + dy, state.position.z)
        if (
            not is_walkable(state, side_a, ignore_threat=True)
            and not is_walkable(state, side_b, ignore_threat=True)
        ):
            return _plain(state, action_format("action.movement.diagonal_blocked"))
    previous_area = area_name(state)
    from .skill_tree import weapon_family

    kept_roof_aim = bool(
        state.location == "region" and state.aimed_target
        and state.weapon != "war flail"
        and (state.position.z > 0 and "roof nail" in state.carried_passives
             or state.weapon in RANGED_WEAPONS and state.courier and "moving-volley" in state.courier.skill_nodes
             and weapon_family(state.weapon) == "bow")
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
    if state.location == "region":
        from .discoveries import reveal_nearby

        messages.extend(reveal_nearby(state))
        from .sanctums import approach as approach_sanctum

        site_event = approach_sanctum(state)
        if site_event:
            messages.append(site_event)
        from .landscape_variation import approach as approach_landform

        field_event = approach_landform(state)
        if field_event:
            messages.append(field_event)
    if kept_roof_aim:
        messages.append(action_format("action.movement.moving_volley")
                        if state.courier and "moving-volley" in state.courier.skill_nodes
                        else action_format("action.movement.roof_nail"))
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
        messages.append(action_format("action.movement.open_door"))
    quiet = state.courier and (
        state.courier.technique == "quiet passage"
        or (
            state.courier.technique == "wind listener"
            and state.active_region_id == "greenwold"
            and state.weather == "crosswind"
        )
        or "surveyed soft-step" in build_combinations(state)
        or "quiet-veil" in state.terrain_statuses
        or ("technique.smoke_spoor" in learned_practice_ids(state.courier) and position_key(target) in state.smoke)
    )
    if state.courier and state.courier.character_specified:
        from .character import effective_competency

        quiet = quiet or (effective_competency(state.courier, "fieldcraft") >= 8
                          and armour_noise(state) == 0)
    from .practices import has_effect as has_practice_effect

    practice_mud = (
        has_practice_effect(state, "mud-quiet")
        or has_practice_effect(state, "clay-step")
    )
    if tile == "m" and not (
        state.gear == "quiet shoes"
        or "reed sole wraps" in state.carried_passives
        or "fen sledge" in state.carried_passives
        or "mudproof" in worn_tags(state, ("feet",))
        or practice_mud
    ):
        messages.append(action_format("action.movement.mud"))
        messages.extend(emit_sound(state, 1, target))
    elif not quiet and state.pressure_elapsed % 8 == 7:
        if "quiet" not in worn_tags(state, ("feet",)) or tile in {"r", "q", "w"}:
            state.noise += 1
    armour_sound = armour_noise(state)
    if armour_sound and state.pressure_elapsed % max(2, 8 - armour_sound * 2) == 0:
        state.noise += 1
        messages.append(action_format("action.movement.armour_noise"))
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
        messages.append(action_format("action.movement.injury_delay"))
    water_delay = False
    if position_key(target) in state.water:
        protected = (
            state.gear == "rope"
            or (state.courier and "technique.shoreline_measure" in learned_practice_ids(state.courier))
            or has_practice_effect(state, "shallow-water-step")
            or (
                has_practice_effect(state, "wet-load-step")
                and load_state(state) in {"encumbered", "overloaded"}
            )
            or "river hooks" in state.carried_passives
            or "shingle skids" in state.carried_passives
            or (state.courier and state.courier.technique == "sure footing")
            or (
                state.active_region_id == "greywash"
                and state.courier and state.courier.technique == "ebb reader"
            )
        )
        if not protected:
            water_delay = True
            messages.append(action_format("action.movement.water_delay"))
        if "smokeleaf-infusion" in state.drink_effects:
            state.noise += 1
            messages.append(action_format("action.movement.smokeleaf_water"))
    new_area = area_name(state)
    discovered_key = f"discovered:{new_area}"
    if new_area != previous_area and not state.region.changes.get(discovered_key):
        state.region.changes[discovered_key] = True
        messages.insert(0, action_format("action.movement.area_discovered", area=new_area))
    if tile == "O":
        messages.append(_fall(state))
    burden = load_state(state)
    burden_delay = (
        burden in {"encumbered", "overloaded"}
        and state.location == "region"
        and not (
            has_practice_effect(state, "wet-load-step")
            and (tile == "m" or position_key(target) in state.water)
        )
        and not (
            "shingle skids" in state.carried_passives
            and position_key(target) in state.water
        )
    )
    if burden == "laden" and tile in {"m", "r", "t", ","}:
        state.noise += 1
    from .arc_relics import lee_sheltered

    storm_delay = water_delay or burden_delay or injury_delay or (
        state.weather == "hard rain"
        and state.position.z == 0
        and "rain cape" not in state.carried_passives
        and not lee_sheltered(state)
    )
    slowing_statuses = {
        "bogged", "current", "net-drag", "brine-chill", "coalheart-chill",
        "fatigued",
    } & set(state.terrain_statuses)
    if "fen sledge" in state.carried_passives:
        slowing_statuses.discard("bogged")
    if "shingle skids" in state.carried_passives:
        slowing_statuses.discard("current")
    if has_practice_effect(state, "clay-step"):
        slowing_statuses.discard("bogged")
    status_delay = bool(slowing_statuses)
    mapped_shortcut = (
        "coppice map" in state.carried_passives
        and state.active_region_id == "greenwold" and tile == "t"
    )
    if mapped_shortcut:
        status_delay = False
        state.noise = max(0, state.noise - 1)
        messages.append(action_format("action.movement.coppice_shortcut"))
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
        return _plain(state, action_format("social.objective.unavailable"))
    if decision == "alter" and not can_alter_objective(state):
        return _plain(state, action_format("social.objective.alter_unavailable"))
    if decision == "accept":
        state.objective_status = "accepted"
        text = action_format("social.objective.accepted", courier=state.courier.name)
    elif decision == "refuse":
        state.objective_status = "refused"
        state.contact.disposition = max(-3, state.contact.disposition - 1)
        text = action_format("social.objective.refused", courier=state.courier.name, region=state.region.name)
    elif decision == "alter":
        state.objective_status = "altered"
        text = action_format("social.objective.altered", courier=state.courier.name)
    else:
        return _plain(state, action_format("social.objective.invalid_decision"))
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
    memory = action_format("social.objective.completed_memory", courier=state.courier.name, region=state.region.name, method=method)
    _remember_contact(state, memory)
    state.remember(memory)
    from .quests import record_objective_completion

    record_objective_completion(state, altered)
    return action_format("social.objective.completed", region=state.region.name, method=method)


def _open_container(state: GameState) -> ActionResult:
    from .discoveries import discovery_name

    container = next(
        (item for item in state.region.containers if item.position == state.position),
        None,
    )
    if container is None:
        return _plain(state, action_format("action.container.none"))
    if container.opened and container.item_ids:
        return ActionResult(
            False, False, action_format("action.container.open", container=discovery_name(state.active_region_id, container)),
            f"inventory:container:{container.id}",
        )
    if container.opened:
        return _plain(state, action_format("action.container.empty"))
    requirement = container.requirement
    if requirement == "rope" and state.gear != "rope" and "river hooks" not in state.carried_passives:
        return _plain(state, action_format("action.container.requirement.rope"))
    if requirement == "light" and state.gear != "hooded lantern" and state.lamp_oil <= 0:
        return _plain(state, action_format("action.container.requirement.light"))
    if requirement == "key" and state.gear != "repair tools" and not (
        state.courier and state.courier.technique == "lever craft"
    ) and not ({"wreck key", "chalk cipher"} & set(state.carried_passives)) and not (
        {"charcoal key", "limestone wedge"} & set(state.consumables)
    ):
        return _plain(state, action_format("action.container.requirement.key"))
    if requirement == "rope" and state.gear == "rope" and "flood rig" not in build_combinations(state):
        if state.rope_uses <= 0:
            return _plain(state, action_format("action.container.rope_exhausted"))
        state.rope_uses -= 1
    if requirement == "light" and state.gear != "hooded lantern":
        state.lamp_oil -= 1
    rewards = [container.reward, *container.extra_rewards]
    legend = state.legendary_objects.get(container.legendary_id or "")
    display_rewards = [*rewards, *([legend.name] if legend else [])]
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
            action_format("action.item.origin.container", container=discovery_name(state.active_region_id, container), region=state.active_region_id),
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
    if legend:
        physical = create_item(
            state, legend.base_kind, legend.provenance, location="container"
        )
        physical.legendary_id = legend.id
        physical.container_id = container.id
        container.item_ids.append(physical.id)
        fits_pack = state.auto_place_enabled and auto_place(
            state, physical.id, "pack", owner_id=state.active_courier_id
        )
        if fits_pack:
            container.item_ids.remove(physical.id)
            record_acquisition(state, physical)
            packed.append(legend.name)
        else:
            left.append(legend.name)
    container.opened = True
    from .quests import record_container_opened

    record_container_opened(state, container.id)
    tally_text = ""
    tally_key = f"salvage_tally:{state.active_region_id}"
    if (
        requirement
        and "salvage tally" in state.carried_passives
        and not state.vessel_changes.get(tally_key)
    ):
        state.vessel_changes[tally_key] = True
        state.trade_credit += 1
        institution = state.institutions.get(f"work:{state.active_region_id}")
        if institution:
            institution.confidence = min(3, institution.confidence + 1)
        tally_text = action_format("action.container.tally")
    from .practices import has_effect as has_practice_effect

    practice_key = f"wreck_title_hold:{state.active_region_id}"
    if (
        requirement and has_practice_effect(state, "secured-salvage")
        and not state.vessel_changes.get(practice_key)
    ):
        state.vessel_changes[practice_key] = True
        institution = state.institutions.get(f"work:{state.active_region_id}")
        if institution:
            institution.confidence = min(3, institution.confidence + 1)
        tally_text += action_format("action.container.practice_tally")
    state.remember(
        action_format("action.container.memory", courier=state.courier.name, container=discovery_name(state.active_region_id, container), rewards=", ".join(display_rewards))
    )
    message = action_format("action.container.opened", container=discovery_name(state.active_region_id, container), rewards=", ".join(display_rewards), tally=tally_text)
    if packed:
        message += action_format("action.container.packed", items=", ".join(packed))
    if left:
        message += action_format("action.container.left", items=", ".join(left))
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
        or (courier and {"technique.mill_hearing", "technique.bell_interval"} & learned_practice_ids(courier))
    )
    if state.active_region_id != "hearthford":
        state.region.changes["environment_control_used"] = True
        if state.active_region_id == "greywash":
            state.water.clear()
            state.region.changes["tide_held"] = True
            text = action_format("action.environment.greywash.control")
        elif state.active_region_id == "greenwold":
            state.smoke.clear()
            state.region.changes["burn_redirected"] = True
            text = action_format("action.environment.greenwold.control")
        elif state.active_region_id == "whitecairn":
            state.region.changes["quarry_braced"] = True
            text = action_format("action.environment.whitecairn.control")
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
    from .geography import layout_point

    points = [layout_point(state.region, point) for point in [
        Position(58, 42, -1),
        Position(58, 42, 0),
        Position(78, 22, 0),
    ]]
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
        action_format("action.environment.hearthford.control", control=state.flood_control)
    )
    return _time_result(state, " ".join([text, *sounds]), priority=3)


def _furnace_interaction(state: GameState) -> ActionResult:
    machinery = next((threat for threat in state.combatants if threat.profile == "machinery"), None)
    if machinery is None:
        above = Position(state.position.x, state.position.y, min(2, state.position.z + 1))
        state.smoke.update({position_key(state.position): 6, position_key(above): 6})
        return _time_result(
            state,
            action_format("action.environment.furnace.fire"),
            priority=3,
        )
    if state.gear in {"repair tools", "rope"} or state.support == "carpenter rig":
        machinery.status, machinery.intent = "disabled", "braked at the furnace drive"
        state.region.changes["machinery_disabled"] = True
        state.smoke.clear()
        return _time_result(
            state,
            action_format("action.environment.furnace.braked"),
            priority=3,
        )
    smoke_points = [state.position, Position(state.position.x, state.position.y, 1)]
    state.smoke.update({position_key(point): 6 for point in smoke_points})
    sounds = emit_sound(state, 2)
    return _time_result(
        state,
        " ".join(
            [
                action_format("action.environment.furnace.smoke"),
                *sounds,
            ]
        ),
        priority=3,
    )


def _destroy_floor(state: GameState) -> ActionResult:
    if base_tile(state, state.position) != "d":
        return _plain(state, action_format("action.environment.floor.none"))
    can_breach = state.weapon == "hand axe" or (
        state.weapon == "cudgel" and "mill-tooth wedge" in state.carried_passives
    )
    if not can_breach:
        return _plain(
            state, action_format("action.environment.floor.requirement")
        )
    state.region.tile_changes[position_key(state.position)] = "O"
    sounds = emit_sound(state, 4)
    braced = (
        "quarry brace" in state.carried_passives
        and load_state(state) in {"laden", "encumbered"}
    )
    fall = (
        action_format("action.environment.floor.braced")
        if braced else _fall(state)
    )
    return _time_result(
        state,
        action_format("action.environment.floor.broken", fall=fall, sounds=(" " + " ".join(sounds)) if sounds else ""),
        priority=3,
    )


def interact(state: GameState) -> ActionResult:
    from .vehicles import JOMON_DOCK, SHORE_DOCK, active_vehicle, board_region_vehicle, disembark, vehicle_at

    vehicle = active_vehicle(state)
    if vehicle is not None:
        if state.location == "region":
            return disembark(state)
        if state.jomon_space == "harbour":
            if state.position == JOMON_DOCK:
                if state.returning_by_tug:
                    state.returning_by_tug = False
                    state.active_vehicle_id = None
                    return _finish_expedition_return(state)
                state.jomon_space, state.position, state.active_vehicle_id = "vessel", JOMON_GANGPLANK, None
                return _time_result(state, action_format("action.interact.tug_returned"))
            if state.position == SHORE_DOCK:
                return depart(state)
            return _plain(state, action_format("action.interact.tug_mooring"))
    if state.location == "region" and vehicle_at(state, state.position):
        return board_region_vehicle(state)
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
                return ActionResult(False, False, action_format("social.incident.response_required"), "incident")
        # A courier standing on a physical control operates it even when its
        # scheduled worker is adjacent. Conversations remain available from
        # ordinary floor cells beside that worker.
        if state.jomon_space == "tavern" and tile == "+":
            state.jomon_space = "vessel"
            state.position = Position(TAVERN_ENTRANCE.x - 1, TAVERN_ENTRANCE.y, 0)
            return _plain(state, action_format("action.interact.tavern_exit"), changed=True)
        if state.jomon_space == "tavern" and tile == "D":
            if another_game_active(state, "dullest"):
                return ActionResult(False, False, "Finish the other active tavern game before opening Dullest Dungeon.")
            return ActionResult(False, False, "The Dullest Dungeon box opens on the table.", "tabletop")
        if state.jomon_space == "tavern" and tile == "P":
            if another_game_active(state, "draw"):
                return ActionResult(False, False, action_format("action.interact.tavern_draw.busy"))
            return ActionResult(False, False, action_format("action.interact.tavern_draw.ready"), "tavern-draw")
        if state.jomon_space == "tavern" and tile == "Q":
            if another_game_active(state, "dice"):
                return ActionResult(False, False, action_format("action.interact.quay_bones.busy"))
            return ActionResult(False, False, action_format("action.interact.quay_bones.ready", bartender=state.bartender.name.split()[0]), "tavern-dice")
        if state.jomon_space == "vessel":
            destination = vessel_vertical_destination(state.position)
            if destination:
                if not is_walkable(state, destination):
                    return _plain(state, action_format("action.interact.hatch_occupied"))
                direction_id = "down" if destination.z < state.position.z else "up"
                state.position = destination
                if state.combat_active:
                    return _time_result(state, action_format(f"action.interact.vertical.combat.{direction_id}"))
                return _plain(state, action_format(f"action.interact.vertical.{direction_id}"), changed=True)
        from .ship_crises import station_action
        handled = station_action(state, tile)
        if handled is not None:
            return handled
        if state.combat_active and tile == "C":
            return _plain(state, action_format("action.interact.crisis_tavern"))
        if tile == "+":
            return depart(state)
        if tile == "C":
            state.jomon_space = "tavern"
            state.position = Position(TAVERN_EXIT.x + 1, TAVERN_EXIT.y, 0)
            return _plain(state, action_format("action.interact.tavern_enter"), changed=True)
        if tile == "P":
            return ActionResult(
                False,
                False,
                action_format("action.interact.voyage_danger") if state.voyage_status == "active" else action_format("action.interact.route_destination"),
                "voyage" if state.voyage_status == "active" else "route-chart",
            )
        if tile == "L":
            return ActionResult(False, False, action_format("action.interact.stores"), "equipment")
        if tile == "H":
            return ActionResult(False, False, action_format("action.interact.hold"), "hold")
        if tile == "s" and state.merchant_present:
            return ActionResult(False, False, action_format("social.merchant.open_stock"), "merchant")
        if tile == "K":
            return ActionResult(False, False, action_format("action.interact.chronicle"), "chronicle")
        station = {
            "G": "galley", "R": "repair", "b": "berths", "U": "bilge",
            "p": "provisions", "W": "workshop", "S": "storage",
            "N": "helm", "O": "lookout", "T": "gathering", "s": "market",
        }.get(tile)
        if station:
            return ActionResult(
                False, False, action_format(f"action.interact.station.{tile}"),
                f"station:{station}",
            )
        person = adjacent_person(state)
        if person:
            return ActionResult(False, False, action_format("social.interact.person", person=person.name), f"person:{person.id}")
        bartender_schedule = state.actor_schedules.get(state.bartender.id)
        if (
            bartender_schedule
            and bartender_schedule.area == current_area(state)
            and max(
                abs(bartender_schedule.position.x - state.position.x),
                abs(bartender_schedule.position.y - state.position.y),
            ) <= 1
        ):
            return ActionResult(False, False, action_format("social.interact.bartender", bartender=state.bartender.name), "bartender")
        return _plain(state, action_format("action.interact.nothing"))
    if state.position == state.region.landmarks["landing"]:
        return return_to_jomon(state)
    from .sanctums import undercroft as sanctum_undercroft

    if state.position == state.region.landmarks.get("sanctum_shrine"):
        return ActionResult(False, False, action_format("action.interact.sanctum_shrine"), "sanctum")
    if state.position == state.region.landmarks.get("sanctum_undercroft"):
        changed, message = sanctum_undercroft(state)
        return _time_result(state, message, priority=3) if changed else _plain(state, message)
    if state.position == state.region.landmarks.get("sanctum_secret"):
        from .sanctums import open_secret

        changed, message = open_secret(state)
        return _time_result(state, message, priority=2) if changed else _plain(state, message)
    from .situations import interaction as situation_interaction

    situation_overlay = situation_interaction(state)
    if situation_overlay:
        return ActionResult(False, False, action_format("action.interact.situation"), situation_overlay)
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
        if (state.position == state.region.landmarks.get("sanctum_entry")
                and destination.z == 1 and not state.region.changes.get("sanctum:unsealed")):
            return _plain(state, action_format("action.interact.sanctum_sealed"))
        if load_state(state) == "overloaded" and destination.z > state.position.z:
            return _plain(state, action_format("action.interact.climb_overloaded"))
        injured_climb = bool(
            state.courier and {"legs", "feet"} & set(state.courier.injuries)
            and destination.z > state.position.z
        )
        armour_climb = armour_mobility(state) >= 3 and destination.z > state.position.z
        from .practices import has_effect as has_practice_effect

        if has_practice_effect(state, "stair-economy"):
            injured_climb = armour_climb = False
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
                action_format("action.interact.vertical_blocked", threat=blocker.name),
            )
        link = next(
            item for item in state.region.vertical_links
            if state.position in {item.first, item.second}
        )
        state.position = destination
        from .sanctums import enter_tier

        sanctum_note = enter_tier(state, destination)
        from .landscape_variation import enter_structure, link_name

        structure_note = enter_structure(state)
        from .quests import mark_elevated_lead

        marked_lead = mark_elevated_lead(state)
        return _time_result(
            state,
            action_format("action.interact.vertical_used", link=link_name(state.active_region_id, link),
                          sanctum=(f" {sanctum_note}" if sanctum_note else ""),
                          structure=(f" {structure_note}" if structure_note else ""),
                          slow=action_format("action.interact.vertical.slow") if injured_climb or armour_climb else "",
                          marked=action_format("action.interact.vertical.marked") if marked_lead else ""),
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
                action_format("social.contact.regional_ready", contact=state.contact.name),
                "quest:regional",
            )
        if arc_available_here(state):
            return ActionResult(
                False, False,
                action_format("social.contact.arc_ready", contact=state.contact.name),
                "quest:arc",
            )
        if state.objective_status in {"unoffered", "failed"}:
            return ActionResult(
                False, False, action_format("social.contact.objective", contact=state.contact.name), "objective"
            )
        commodity = state.region.objective_commodity
        quantity = state.carried_goods.get(
            commodity, CommodityStack(0, "")
        ).quantity
        if state.objective_status == "accepted" and quantity >= state.objective_required:
            cargo_condition = state.carried_goods[commodity].condition
            if not consume_carried(state, f"commodity:{commodity}", state.objective_required):
                state.carried_goods[commodity].quantity -= state.objective_required
                if state.carried_goods[commodity].quantity == 0:
                    del state.carried_goods[commodity]
            result = _complete_objective(state, False)
            if cargo_condition != COMMODITIES[commodity]["condition"]:
                loss = 2 if cargo_condition == "spoiled" else 1
                state.trade_credit = max(0, state.trade_credit - loss)
                state.market[commodity].stock = max(0, state.market[commodity].stock - 1)
                state.contact.disposition = max(-3, state.contact.disposition - 1)
                account = state.institutions.get(f"work:{state.active_region_id}")
                if account:
                    account.confidence = max(-3, account.confidence - 1)
                    account.witnessed_acts.append(action_format("social.objective.reduced_account", courier=state.courier.name, condition=cargo_condition, commodity=commodity))
                    del account.witnessed_acts[:-8]
                result += action_format("social.objective.reduced_result", condition=cargo_condition, loss=loss)
            return _time_result(state, result, priority=3)
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
            False, False, action_format("social.contact.inspect", contact=state.contact.name), "contact"
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
        if second.id == f"sanctum:{state.active_region_id}:witness":
            return ActionResult(False, False, action_format("social.contact.sanctum", contact=second.name), "sanctum")
        if second.id == f"landform:{state.active_region_id}:traveller":
            return ActionResult(False, False, action_format("social.contact.traveller", contact=second.name), "field-traveller")
        quest = state.questlines[state.active_region_id]
        if quest.stage == 2 and quest.status == "resolution":
            return ActionResult(
                False, False,
                action_format("social.contact.regional_ready", contact=second.name),
                "quest:regional",
            )
        return ActionResult(
            False, False, action_format("social.contact.speak", contact=second.name),
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
                        action_format("action.objective.lost_ground", commodity=commodity, x=point.x, y=point.y, z=f"{point.z:+d}"),
                    )
                carrier = next(
                    (threat for threat in state.combatants if threat.carrying_item_id == recoverable.id),
                    None,
                )
                return _plain(
                    state,
                    action_format("action.objective.lost_enemy", carrier=carrier.name if carrier else "a withdrawing thief", commodity=commodity),
                )
            if not state.region.changes.get("objective_replacement_taken"):
                state.region.changes["objective_replacement_taken"] = True
                state.region.changes["objective_taken"] = False
                state.contact.disposition = max(-3, state.contact.disposition - 1)
                _remember_contact(
                    state,
                    action_format("social.objective.replacement_memory", courier=state.courier.name),
                )
                state.add_message(
                    action_format("social.objective.replacement_released"),
                    priority=3,
                )
            else:
                return _plain(
                    state,
                    action_format("social.objective.no_replacement"),
                )
        if state.objective_status != "accepted":
            return _plain(state, action_format("social.objective.accept_required", region=state.region.name))
        commodity = state.region.objective_commodity
        if not _add_goods(
            state,
            commodity,
            state.objective_required,
            COMMODITIES[commodity]["condition"],
        ):
            return _plain(state, action_format("action.objective.load_full"))
        state.region.changes["objective_taken"] = True
        sounds = emit_sound(state, 2)
        return _time_result(
            state,
            " ".join([action_format("action.objective.secured", commodity=commodity), *sounds]),
            priority=3,
        )
    if tile == "&":
        return _control_interaction(state)
    if tile == "f":
        return _furnace_interaction(state)
    if tile == "d":
        return _destroy_floor(state)
    return _plain(state, action_format("action.interact.nothing"))


def _attack_targets(state: GameState, attack_range: int) -> list[Threat]:
    from .world import courier_sees

    targets = (
        threat
        for threat in state.combatants
        if threat.status in {"watching", "engaged"}
        and distance(state.position, threat.position) <= attack_range
        and courier_sees(state, threat.position)
    )
    return sorted(
        targets, key=lambda threat: (distance(state.position, threat.position), threat.id)
    )


def attack_target_legality(state: GameState, target: Threat) -> tuple[bool, str]:
    """Explain targeting without mutating combat state or leaking hidden actors."""
    from .world import courier_sees

    if not state.combat_active or state.weapon is None:
        return False, "no readied combat action"
    if target.status not in {"watching", "engaged"} or not courier_sees(
        state, target.position
    ):
        return False, "actor is not presently visible"
    gap = distance(state.position, target.position)
    maximum = effective_weapon_range(state)
    minimum = 1
    if state.weapon in {"pike", "boar spear"}:
        minimum = 2
    elif state.weapon == "staff sling":
        minimum = 3
    elif state.weapon in WORK_WEAPONS:
        minimum = WORK_WEAPONS[state.weapon].minimum
    elif state.weapon in ARSENAL:
        minimum = ARSENAL[state.weapon].minimum
    if gap < minimum:
        return False, f"inside minimum range {minimum}"
    if gap > maximum:
        return False, f"beyond maximum range {maximum}"
    if state.weapon in RANGED_WEAPONS and state.weapon != "pot sling":
        if cover_at(state, state.position, target.position) == "full":
            return False, "full structure blocks the lane"
    if state.weapon == "shield and hanger":
        from .work_weapons import approach

        path, reason = approach(state, target)
        if path is None:
            return False, reason
    return True, "legal target; Enter commits one ordinary combat action"


def legal_attack_targets(state: GameState) -> list[Threat]:
    return [
        target for target in _attack_targets(state, effective_weapon_range(state))
        if attack_target_legality(state, target)[0]
    ]


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
WEAPON_RANGES.update({name: spec.reach for name, spec in WORK_WEAPONS.items()})
RANGED_WEAPONS |= {"pot sling", "throwing axe"}
WEAPON_RANGES.update({name: spec.reach for name, spec in ARSENAL.items()})
RANGED_WEAPONS |= {name for name, spec in ARSENAL.items() if spec.family in {"bow", "gun", "device"}}


def effective_weapon_range(state: GameState) -> int:
    from .workshop import active_part

    if state.weapon not in WEAPON_RANGES:
        return 0
    attack_range = WEAPON_RANGES[state.weapon]
    from .skill_tree import has_node, weapon_family

    family = weapon_family(state.weapon)
    if family in {"reach", "impact"} and has_node(state.courier, "measured-stance"):
        attack_range += 1
    if family == "bow" and has_node(state.courier, "sighted-draw"):
        attack_range += 1
    if family == "gun" and state.guarded_step and has_node(state.courier, "braced-tube"):
        attack_range += 1
    if family == "device" and has_node(state.courier, "safe-throw"):
        attack_range += 1
    from .legendary import active_legend
    legend = active_legend(state)
    if legend:
        attack_range += legend.range_bonus
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
    from .practices import has_effect as has_practice_effect

    if (
        state.weapon in RANGED_WEAPONS and state.position.z > 0
        and has_practice_effect(state, "elevated-range")
    ):
        attack_range += 1
    return attack_range


def attack(state: GameState, target_id: str | None = None, *, target_position: Position | None = None, ammunition: str | None = None) -> ActionResult:
    from .workshop import active_part, attack_effects

    if not state.combat_active or state.weapon is None:
        return _plain(state, action_format("combat.attack.unready"))
    candidates = _attack_targets(state, effective_weapon_range(state))
    if state.weapon == "pot sling":
        from .work_weapons import cast_pot
        target = next((a for a in candidates if a.id == target_id), None) if target_id else next((a for a in candidates if a.ecology != "prey"), None)
        return cast_pot(state, target_position or (target.position if target else None), ammunition)
    if state.weapon in ARSENAL:
        from .expanded_weapons import strike

        return strike(state, target_id, target_position=target_position)
    if target_id is not None:
        candidates = [target for target in candidates if target.id == target_id]
    else:
        candidates = [target for target in candidates if target.ecology != "prey"]
    if state.weapon in {"pike", "boar spear"}:
        candidates = [target for target in candidates if distance(state.position, target.position) >= 2]
    if state.weapon == "staff sling":
        candidates = [target for target in candidates if distance(state.position, target.position) >= 3]
    work_weapon = WORK_WEAPONS.get(state.weapon)
    if work_weapon:
        candidates = [target for target in candidates if distance(state.position, target.position) >= work_weapon.minimum]
    if not candidates:
        if state.weapon == "hand axe" and base_tile(state, state.position) == "d":
            return _destroy_floor(state)
        return _plain(state, action_format("combat.attack.no_target"))
    target = candidates[0]
    original_target_position = target.position
    shield_steps = []
    if state.weapon == "shield and hanger":
        from .work_weapons import approach
        shield_steps, reason = approach(state, target)
        if shield_steps is None:
            return _plain(state, reason)
    thrown_item = equipped_item(state, "readied") if state.weapon == "throwing axe" else None
    if state.weapon == "throwing axe" and (not thrown_item or thrown_item.kind != state.weapon):
        return _plain(state, action_format("combat.attack.throwing_axe"))
    target.status = "engaged"
    if state.weapon == "war flail" and state.aimed_target != target.id:
        state.aimed_target = target.id
        return _time_result(state, action_format("combat.attack.flail_prepare"), priority=3)
    if state.weapon == "war flail":
        state.aimed_target = None
    if shield_steps:
        state.position = shield_steps[-1]
    ranged = state.weapon in RANGED_WEAPONS
    prepared = state.weapon in {"crossbow", "longbow", "heavy crossbow", "handgonne"}
    from .skill_tree import has_node

    if state.weapon == "longbow" and has_node(state.courier, "quick-nock"):
        prepared = False
    ammo_key = {
        "crossbow": "bolts", "longbow": "arrows", "sling": "sling stones",
        "heavy crossbow": "heavy bolts", "javelins": "javelins", "weighted net": "nets",
        "staff sling": "sling stones", "hooked javelin": "javelins",
        "handgonne": "handgonne charges",
    }.get(state.weapon)
    if ranged:
        if state.weapon == "crossbow" and not state.crossbow_loaded:
            return _plain(state, action_format("combat.attack.crossbow_unloaded"))
        if state.weapon == "heavy crossbow" and state.weapon_ready < 2:
            return _plain(state, action_format("combat.attack.arbalest_reload", remaining=2 - state.weapon_ready))
        gun_required = 1 if has_node(state.courier, "vent-care") else 2
        if state.weapon == "handgonne" and state.weapon_ready < gun_required:
            return _plain(state, action_format("combat.attack.handgonne_load", remaining=gun_required - state.weapon_ready))
        if ammo_key and physical_ammunition(state, ammo_key) <= 0:
            return _plain(state, action_format("combat.attack.no_ammunition", ammunition=ammo_key))
        lane_cover = cover_at(state, state.position, target.position)
        if lane_cover == "full":
            return _plain(state, action_format("combat.attack.path_blocked"))
        if prepared and state.aimed_target != target.id:
            state.aimed_target = target.id
            return _time_result(
                state,
                action_format("combat.attack.prepare", weapon=item_display_name_or_legacy(state.weapon), threat=target.name, range=distance(state.position, target.position), cover=lane_cover),
                priority=3,
            )
        from .arc_relics import lee_sheltered as has_lee_shelter

        if (
            state.weapon in {"crossbow", "longbow"}
            and state.weather in {"hard rain", "coast squall", "forest rain"}
            and "weatherproof aim" not in build_combinations(state)
            and "weatherfast grip" not in build_combinations(state)
            and not active_part(state, "resin seal")
            and not has_lee_shelter(state)
            and not has_node(state.courier, "wind-hold")
        ):
            state.aimed_target = None
            return _time_result(
                state,
                action_format("combat.attack.weather_spoiled"),
                priority=3,
            )
        if ammo_key:
            if not consume_ammunition(state, ammo_key):
                return _plain(state, action_format("combat.attack.no_physical_ammunition", ammunition=ammo_key))
        if state.weapon == "crossbow":
            state.crossbow_loaded = False
        if state.weapon == "heavy crossbow":
            state.weapon_ready = 0
        if state.weapon == "handgonne":
            state.weapon_ready = 0
        state.aimed_target = None
        damage, weapon_key, sound = {
            "crossbow": (3, "crossbow", 3),
            "longbow": (3, "longbow", 2),
            "sling": (1, "sling", 2),
            "heavy crossbow": (5, "heavy_crossbow", 5),
            "javelins": (2, "javelins", 3),
            "weighted net": (0, "weighted_net", 2),
            "staff sling": (2, "staff_sling", 2),
            "hooked javelin": (2, "hooked_javelin", 3),
            "handgonne": (4, "handgonne", 6),
            "throwing axe": (3, "throwing_axe", 3),
        }[state.weapon]
        weapon_text = action_format(f"combat.attack.weapon.{weapon_key}")
        ignores_partial = state.weapon in {"heavy crossbow", "staff sling", "handgonne"} or (
            state.weapon == "sling"
            and (
                "high sling arc" in build_combinations(state)
                or (state.courier and state.courier.technique == "high arc")
            )
        )
        if lane_cover == "partial" and not ignores_partial:
            damage = max(0, damage - 1)
            weapon_text += action_format("combat.attack.effect.partial_cover")
        if state.courier and ({"head", "hands"} & set(state.courier.injuries)):
            damage = max(0, damage - 1)
            weapon_text += action_format("combat.attack.effect.injury")
    else:
        damage = work_weapon.damage if work_weapon else {
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
        weapon_text = action_format(
            f"combat.attack.weapon.{state.weapon.replace(' ', '_')}",
        )
        sound = 1 if state.weapon in {"cudgel", "staff"} else 2
        if work_weapon:
            sound = work_weapon.noise
        if "thorn-held momentum" in build_combinations(state):
            damage += 1
            target.morale -= 1
            weapon_text += action_format("combat.attack.effect.thorn")
            state.guarded_step = False
    if state.weapon == "billhook":
        target.morale -= 1
        old_position = target.position
        target.position = _step_toward(state, target, state.position)
        weapon_text += action_format("combat.attack.effect.billhook")
        if "mobile hook" in build_combinations(state):
            state.position = old_position
        _set_combat_intent(target, "intent.disrupted.billhook")
    elif state.weapon == "spear":
        target.position = _step_away(state, target)
        weapon_text += action_format("combat.attack.effect.spear")
        _set_combat_intent(target, "intent.disrupted.spear_spacing")
    elif state.weapon == "cudgel":
        target.morale -= 2
        target.position = _step_away(state, target)
        weapon_text += action_format("combat.attack.effect.cudgel")
        _set_combat_intent(target, "intent.dazed.cudgel")
    elif state.weapon == "staff":
        adjacent = [
            other for other in candidates
            if distance(state.position, other.position) <= 1
        ]
        from .enemy_equipment import harm_enemy

        for other in adjacent[1:]:
            harm_enemy(state, other, 1, "river staff sweep", damage_kind="blunt")
        state.guarded_step = True
        weapon_text += action_format("combat.attack.effect.staff")
    elif state.weapon == "hand axe":
        target.morale -= 1
        weapon_text += action_format("combat.attack.effect.axe")
    elif state.weapon == "pike":
        target.position = _step_away(state, target)
        target.position = _step_away(state, target)
        state.guarded_step = True
        weapon_text += action_format("combat.attack.effect.pike")
        _set_combat_intent(target, "intent.disrupted.pike_brace")
    elif state.weapon == "boar spear":
        _set_combat_intent(target, "intent.pinned.crossbar")
        target.morale -= 2 if target.profile == "animal" else 1
        weapon_text += action_format("combat.attack.effect.boar_spear")
    elif state.weapon == "paired knives":
        state.guarded_step = True
        target.morale -= 1
        weapon_text += action_format("combat.attack.effect.knives")
    elif state.weapon == "war hammer":
        target.morale -= 2
        target.position = _step_away(state, target)
        weapon_text += action_format("combat.attack.effect.hammer")
        emit_sound(state, 2)
    elif state.weapon == "weighted net":
        _set_combat_intent(target, "intent.entangled.net")
        target.morale -= 1
        weapon_text += action_format("combat.attack.effect.net")
        if state.courier and state.courier.technique == "cast bind":
            target.morale -= 1
            target.position = _step_toward(state, target, state.position)
            weapon_text += action_format("combat.attack.effect.net_bind")
        from .practices import has_effect as has_practice_effect

        if has_practice_effect(state, "net-recover"):
            recovered = create_item(
                state, "consumable:casting net bundle",
                "recoverable net from Thaw-net Recovery", location="ground",
            )
            recovered.region_id = state.spatial_id
            recovered.ground_position = target.position
            weapon_text += action_format("combat.attack.effect.net_recover")
    elif state.weapon == "hooked javelin":
        old_position = target.position
        target.position = _step_toward(state, target, state.position)
        _set_combat_intent(target, "intent.disrupted.hooked_shaft")
        recovered = create_item(
            state, "consumable:throwing javelins",
            "recoverable hooked shaft from a committed throw", location="ground",
        )
        recovered.region_id = state.spatial_id
        recovered.ground_position = old_position
        weapon_text += action_format("combat.attack.effect.hooked_javelin")
        if "retrieval cast" in build_combinations(state):
            if auto_place(
                state, recovered.id, "pack", owner_id=state.active_courier_id
            ):
                sync_legacy_load(state)
                weapon_text += action_format("combat.attack.effect.retrieval")
    elif state.weapon == "handgonne":
        smoke_points = (
            state.position,
            Position(state.position.x + 1, state.position.y, state.position.z),
        )
        state.smoke.update({position_key(point): 4 for point in smoke_points})
        target.morale -= 2
        weapon_text += action_format("combat.attack.effect.handgonne")
    elif state.weapon == "sling" and state.position.z > target.position.z:
        target.morale -= 1
        weapon_text += action_format("combat.attack.effect.high_arc")
        if "high sling arc" in build_combinations(state) or (
            state.courier and state.courier.technique == "high arc"
        ):
            _set_combat_intent(target, "intent.dazed.sling")
            weapon_text += action_format("combat.attack.effect.high_arc_daze")
    if (
        "high-ground drive" in build_combinations(state)
        and target.position.z < state.position.z
    ):
        target.position = _step_away(state, target)
    from .legendary import active_legend
    legend = active_legend(state)
    if legend:
        sound += 1
        weapon_text = action_format(
            "combat.attack.legendary_weapon", legend=legend.name, weapon=weapon_text,
        )
    sound, fitting_text = attack_effects(state, original_target_position, sound, ammo_key)
    working_effect = None
    if work_weapon:
        from .work_weapons import strike_effects
        working_effect = strike_effects(
            state, target, _attack_targets(state, effective_weapon_range(state))
        )
        damage += working_effect.bonus_damage
        weapon_text += "; " + working_effect.text
    if not ranged and "smoke braid" in state.carried_passives:
        from .materials import fields as material_fields, key as material_key

        cells = material_fields(state)
        smoke_cover = any(
            position_key(point) in state.smoke
            or bool(cells.get(material_key(point)) and cells[material_key(point)].smoke)
            for point in (state.position, target.position)
        )
        if smoke_cover:
            damage += 1
            target.morale -= 1
            weapon_text += action_format("combat.attack.effect.smoke_braid")
    if fitting_text:
        weapon_text += "; " + fitting_text
    from .skill_tree import apply_weapon_skills

    damage, skill_text, skill_guard = apply_weapon_skills(state, target, damage)
    if skill_text:
        weapon_text += "; " + skill_text
    sounds = emit_sound(state, sound)
    from .frontier_elites import guard_interception
    damage, protection_text = guard_interception(state, target, damage)
    if protection_text:
        weapon_text += "; " + protection_text
    from .enemy_equipment import harm_enemy

    damage_kind = (
        "pierce" if state.weapon in {"spear", "pike", "boar spear", "crossbow", "longbow", "heavy crossbow", "javelins", "hooked javelin", "handgonne"}
        else "cut" if state.weapon in {"billhook", "hand axe", "paired knives", "throwing axe", "glaive", "arming sword", "long knife"}
        else "blunt"
    )
    harm = harm_enemy(
        state, target, damage, f"{state.courier.name}'s {item_display_name_or_legacy(state.weapon)}",
        damage_kind=damage_kind,
    )
    if harm.defeated or (
        target.morale <= 0 and target.profile != "machinery"
    ):
        target.status = "defeated" if harm.defeated else "retreated"
        _set_combat_intent(target, "intent.route.removed")
        from .inventory import release_enemy_possession
        recovered = harm.dropped if harm.defeated else release_enemy_possession(state, target)
        outcome = action_format(
            "combat.attack.outcome.defeated"
            if harm.defeated else "combat.attack.outcome.drove_off",
        )
        memory = action_format("combat.attack.defeat_memory", courier=state.courier.name, outcome=outcome, threat=target.name, weapon=item_display_name_or_legacy(state.weapon))
        state.remember(memory)
        _remember_contact(state, memory)
        uncovered_verb = "were" if harm.location in {"arms", "hands", "legs", "feet"} else "was"
        armour = action_format("combat.attack.defeat_armour", protection=harm.protection, location=harm.location) if harm.protection != "uncovered" else action_format("combat.attack.defeat_uncovered", location=harm.location, verb=uncovered_verb)
        text = action_format("combat.attack.defeated", threat=target.name, weapon=weapon_text, armour=armour, recovered=recovered)
    else:
        armour = action_format("combat.attack.hit_armour", protection=harm.protection, location=harm.location) if harm.protection != "uncovered" else action_format("combat.attack.hit_uncovered", location=harm.location)
        injury = action_format("combat.attack.hit_injury", injury=harm.injury) if harm.injury else ""
        text = action_format("combat.attack.hit", weapon=weapon_text, damage=harm.amount, armour=armour, injury=injury, threat=target.name, health=target.health, maximum=target.max_health)
    used_weapon = state.weapon
    if thrown_item:
        thrown_item.location, thrown_item.owner_id = "ground", None
        thrown_item.region_id, thrown_item.ground_position = state.spatial_id, original_target_position
        state.weapon = None
    from .skill_tree import record_milestone

    family = (
        "bows" if ranged and used_weapon != "handgonne" else
        "gunworks" if used_weapon == "handgonne" else
        "blades" if used_weapon in {"hand axe", "paired knives", "throwing axe", "long knife", "arming sword", "glaive", "felling axe", "reed sickle"} else
        "reach"
    )
    record_milestone(state, f"combat:{family}")
    return _time_result(
        state,
        " ".join([text, *sounds]),
        guarded=bool(working_effect and working_effect.guarded) or skill_guard,
        priority=3,
    )


def guard(state: GameState, target_id: str | None = None) -> ActionResult:
    if not state.combat_active:
        return _plain(state, action_format("combat.guard.no_danger"))
    expanded = ARSENAL.get(state.weapon)
    if expanded and expanded.family == "gun" and state.weapon_ready < (1 if "quick" in expanded.effects or state.courier and "vent-care" in state.courier.skill_nodes else 2):
        if physical_ammunition(state, "handgonne charges") <= 0:
            return _plain(state, action_format("combat.guard.no_powder"))
        state.weapon_ready += 1
        return _time_result(state, action_format("combat.guard.gun_loading", weapon=item_display_name_or_legacy(state.weapon), current=state.weapon_ready, required=1 if "quick" in expanded.effects or state.courier and "vent-care" in state.courier.skill_nodes else 2), guarded=state.gear == "buckler", priority=3)
    if state.weapon == "crossbow" and not state.crossbow_loaded:
        if physical_ammunition(state, "bolts") <= 0:
            return _plain(state, action_format("combat.guard.no_crossbow_ammo"))
        state.crossbow_loaded = True
        return _time_result(
            state,
            action_format("combat.guard.crossbow_reload"),
            guarded=state.gear == "buckler",
            priority=3,
        )
    if state.weapon == "heavy crossbow" and state.weapon_ready < 2:
        if physical_ammunition(state, "heavy bolts") <= 0:
            return _plain(state, action_format("combat.guard.no_heavy_bolts"))
        state.weapon_ready += 1
        stage = "windlass set" if state.weapon_ready == 1 else "bolt seated and ready"
        return _time_result(
            state,
            action_format("combat.guard.arbalest_reload", current=state.weapon_ready, stage=stage),
            guarded=state.gear == "buckler",
            priority=3,
        )
    if state.weapon == "handgonne" and state.weapon_ready < (1 if state.courier and "vent-care" in state.courier.skill_nodes else 2):
        if physical_ammunition(state, "handgonne charges") <= 0:
            return _plain(state, action_format("combat.guard.no_powder"))
        state.weapon_ready += 1
        stage = "powder and wad seated" if state.weapon_ready == 1 else "ball rammed and match sheltered"
        return _time_result(
            state,
            action_format("combat.guard.handgonne_loading", current=state.weapon_ready, required=1 if state.courier and "vent-care" in state.courier.skill_nodes else 2, stage=stage),
            guarded=state.gear == "buckler",
            priority=3,
        )
    engaged = [
        threat for threat in state.combatants
        if threat.status == "engaged"
        and distance(state.position, threat.position) <= 7
    ]
    if not engaged:
        return _time_result(
            state,
            action_format("combat.guard.no_engaged"),
            priority=2,
        )
    brace_candidates = [
        threat for threat in engaged if brace_target_legality(state, threat)[0]
    ]
    brace_target = (
        next((threat for threat in brace_candidates if threat.id == target_id), None)
        if target_id is not None
        else min(
            brace_candidates,
            key=lambda threat: (distance(state.position, threat.position), threat.id),
            default=None,
        )
    )
    if target_id is not None and brace_target is None:
        chosen = next(
            (threat for threat in state.combatants if threat.id == target_id), None
        )
        reason = (
            brace_target_legality(state, chosen)[1]
            if chosen else "that actor is no longer present"
        )
        return _plain(state, action_format("combat.guard.no_brace_target", reason=reason))
    from .people import personal_practice
    from .practices import learned_practice_ids

    strong = (
        state.gear == "buckler"
        or state.weapon == "staff"
        or (state.courier and state.courier.technique == "set stance")
        or (
            state.courier
            and personal_practice(state.courier) in learned_practice_ids(state.courier)
        )
        or "hearth-ale" in state.drink_effects
        or ("brace" in worn_tags(state, ("arms",)) and "wet" not in state.terrain_statuses)
    )
    from .practices import has_effect as has_practice_effect

    if (
        state.weapon in {"spear", "pike", "boar spear", "glaive", "quarterstaff"}
        and has_practice_effect(state, "reach-retreat")
    ):
        strong = True
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
        or has_practice_effect(state, "reach-retreat")
    )
    if "shielded set stance" in build_combinations(state):
        text = action_format("combat.guard.base.shielded")
    elif strong:
        text = action_format("combat.guard.base.strong")
    else:
        text = action_format("combat.guard.base.normal")
    if "wet" in state.terrain_statuses:
        text += action_format("combat.guard.wet.strong") if {"grip", "tool-grip"} & worn_tags(state, ("hands", "arms")) else action_format("combat.guard.wet.weak")
    if "counterbrace pin" in state.carried_passives:
        from .materials import fields as material_fields, key as material_key

        support = material_fields(state).get(material_key(state.position))
        if support and (support.support < 3 or support.collapse_due):
            support.support = min(3, support.support + 1)
            support.collapse_due = 0
            text += action_format("combat.guard.counterbrace")
    if has_practice_effect(state, "support-guard"):
        from .materials import fields as material_fields, key as material_key

        support = material_fields(state).get(material_key(state.position))
        if support and (support.support < 3 or support.collapse_due):
            support.support = min(3, support.support + 1)
            support.collapse_due = 0
            text += action_format("combat.guard.support")
    if brace_target:
        state.aimed_target = brace_target.id
        text += action_format("combat.guard.brace", weapon=item_display_name_or_legacy(state.weapon), threat=brace_target.name)
    result = _time_result(state, text, guarded=True, priority=3)
    if brace_target and state.aimed_target == brace_target.id:
        state.aimed_target = None
        state.add_message(
            action_format("combat.guard.brace_expired", threat=brace_target.name),
            priority=2,
        )
    return result


def _spend_relic(state: GameState, name: str) -> None:
    """Consume one selected finite relic and its matching physical item."""
    state.relics[name] -= 1
    if not state.relics[name]:
        del state.relics[name]
    state.carried_relic = None
    consume_carried(state, f"relic:{name}")


def use_gear(state: GameState, preparation: str | None = None) -> ActionResult:
    if preparation is not None:
        from .preparations import apply_preparation

        changed, message = apply_preparation(state, preparation)
        return _time_result(state, message, priority=3) if changed else _plain(state, message)
    if not state.combat_active:
        return _plain(state, action_format("action.item.field_only"))
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
    from .arc_relics import ARC_RELIC_DESCRIPTIONS, use_arc_relic

    if state.carried_relic in ARC_RELIC_DESCRIPTIONS:
        changed, message = use_arc_relic(state, state.carried_relic)
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
                threat.status = "evaded"
                _set_combat_intent(threat, "intent.item.tide_knot")
        state.noise = max(0, state.noise - 4)
        return _time_result(
            state,
            action_format("action.item.tide_knot"),
            priority=3,
        )
    if state.carried_relic == "ebbglass spindle" and state.relics.get("ebbglass spindle", 0):
        if state.location != "region":
            return _plain(state, action_format("action.item.ebbglass.unavailable"))
        state.relics["ebbglass spindle"] -= 1
        if not state.relics["ebbglass spindle"]:
            del state.relics["ebbglass spindle"]
        state.carried_relic = None
        consume_carried(state, "relic:ebbglass spindle")
        state.region.process_thresholds = [threshold + 12 for threshold in state.region.process_thresholds]
        state.region.changes["ebbglass_spent"] = True
        return _time_result(
            state,
            action_format("action.item.ebbglass.used"),
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
        add_status(state, "coalheart-chill", action_format("action.status.coalheart.cause"), 8, action_format("action.status.coalheart.consequence"))
        return _time_result(
            state,
            action_format("action.item.coalheart.used"),
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
            action_format("action.item.hollow_bell.used") + ((" " + " ".join(sounds)) if sounds else ""),
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
            action_format("action.item.stillwater.used") + ((" " + " ".join(sounds)) if sounds else ""),
            priority=3,
        )
    if state.carried_relic == "flood-mark clasp" and state.relics.get("flood-mark clasp", 0):
        from .materials import fields as material_fields, point_at

        lowered = steadied = 0
        for coordinate, cell in material_fields(state).items():
            point = point_at(coordinate)
            if distance(state.position, point) > 5:
                continue
            if cell.water:
                cell.water -= 1
                lowered += 1
            if cell.material == "timber" and cell.coating == "wet" and cell.support < 3:
                cell.support += 1
                cell.collapse_due = 0
                steadied += 1
        for coordinate in list(state.water):
            point = point_at(coordinate)
            if distance(state.position, point) <= 5:
                del state.water[coordinate]
                lowered += 1
        if not lowered and not steadied:
            return _plain(state, action_format("action.item.flood_mark.unavailable"))
        _spend_relic(state, "flood-mark clasp")
        add_status(state, "fatigued", action_format("action.status.flood_mark.cause"), 3, action_format("action.status.flood_mark.consequence"))
        changes = state.vessel_changes if state.location == "jomon" else state.region.changes
        changes["flood_mark_clasp_spent"] = True
        return _time_result(
            state,
            action_format("action.item.flood_mark.used", lowered=lowered, steadied=steadied),
            priority=3,
        )
    if state.carried_relic == "ashglass lens" and state.relics.get("ashglass lens", 0):
        from .materials import fields as material_fields, point_at
        from .quests import mark_treasure

        cleared = 0
        for coordinate in list(state.smoke):
            if distance(state.position, point_at(coordinate)) <= 8:
                del state.smoke[coordinate]
                cleared += 1
        for coordinate, cell in material_fields(state).items():
            if cell.smoke and distance(state.position, point_at(coordinate)) <= 8:
                cell.smoke = 0
                cleared += 1
        cache = None
        if state.location == "region":
            cache = next(
                (entry for entry in state.region.containers if not entry.opened and entry.id not in state.treasure_marks[state.active_region_id]),
                None,
            )
        if not cleared and cache is None:
            return _plain(state, action_format("action.item.ashglass.unavailable"))
        _spend_relic(state, "ashglass lens")
        if cache:
            mark_treasure(
                state, state.active_region_id, cache.id,
                action_format("action.item.ashglass.mark", cache=cache.name, x=cache.position.x, y=cache.position.y, z=f"{cache.position.z:+d}"),
            )
        revealed = 0
        for threat in state.combatants:
            if distance(state.position, threat.position) <= 10 and threat.status == "watching":
                threat.status = "engaged"
                threat.last_known_position = state.position
                _set_combat_intent(threat, "intent.item.ashglass")
                revealed += 1
        return _time_result(
            state,
            action_format("action.item.ashglass.used", cleared=cleared, store="one store mark" if cache else "no new store", revealed=revealed),
            priority=3,
        )
    if state.carried_relic == "quarry echo pin" and state.relics.get("quarry echo pin", 0):
        from .materials import fields as material_fields, point_at

        supports = [
            cell for coordinate, cell in material_fields(state).items()
            if distance(state.position, point_at(coordinate)) <= 6
            and (cell.support < 3 or cell.collapse_due)
            and cell.material in {"timber", "stone"}
        ][:8]
        if not supports:
            return _plain(state, action_format("action.item.quarry_pin.unavailable"))
        for cell in supports:
            cell.support, cell.collapse_due = 3, 0
        _spend_relic(state, "quarry echo pin")
        sounds = emit_sound(state, 6, state.position)
        changes = state.vessel_changes if state.location == "jomon" else state.region.changes
        changes["quarry_echo_pin_spent"] = True
        return _time_result(
            state,
            " ".join([
                action_format("action.item.quarry_pin.used", supports=len(supports)),
                *sounds,
            ]),
            priority=3,
        )
    if state.carried_relic == "winter sounding bead" and state.relics.get("winter sounding bead", 0):
        from .calendar import calendar_at
        from .materials import ensure_cell, fields as material_fields, point_at

        if calendar_at(state).season != "winter":
            return _plain(state, action_format("action.item.winter_bead.season"))
        coordinates = set(material_fields(state)) | set(state.water)
        frozen = []
        for coordinate in sorted(coordinates):
            point = point_at(coordinate)
            if distance(state.position, point) > 6:
                continue
            cell = ensure_cell(state, point)
            if cell and cell.water and cell.fluid == "fresh" and not cell.fire:
                cell.water, cell.ice = 1, True
                state.water.pop(coordinate, None)
                frozen.append(point)
                if len(frozen) == 8:
                    break
        if not frozen:
            return _plain(state, action_format("action.item.winter_bead.unavailable"))
        _spend_relic(state, "winter sounding bead")
        add_status(state, "chilled", action_format("action.status.winter_bead.cause"), 4, action_format("action.status.winter_bead.consequence"))
        return _time_result(
            state,
            action_format("action.item.winter_bead.used", count=len(frozen)),
            priority=3,
        )
    if state.carried_relic == "red-clay seal" and state.relics.get("red-clay seal", 0):
        institution = state.institutions.get(f"work:{state.active_region_id}")
        humans = [
            threat for threat in state.combatants
            if threat.status == "engaged"
            and threat.profile in {"pursuer", "reach", "ranged"}
            and distance(state.position, threat.position) <= 5
        ]
        witnessed = bool(state.objective_evidence) or bool(institution and institution.trust >= 1)
        if not humans or not institution or not witnessed:
            return _plain(state, action_format("action.item.red_clay.unavailable"))
        group = humans[0].group
        settled = [threat for threat in humans if not group or threat.group == group][:4]
        for threat in settled:
            threat.status = "negotiated"
            _set_combat_intent(threat, "intent.item.red_clay")
        institution.obligation = min(9, institution.obligation + 2)
        institution.confidence = min(3, institution.confidence + 1)
        state.region.changes["red_clay_compact"] = institution.id
        _spend_relic(state, "red-clay seal")
        state.remember(action_format("action.item.red_clay.memory", courier=state.courier.name, institution=institution.name))
        return _time_result(
            state,
            action_format("action.item.red_clay.used", count=len(settled), institution=institution.name),
            priority=3,
        )
    if state.carried_relic == "wreck-light prism" and state.relics.get("wreck-light prism", 0):
        from .quests import mark_treasure

        cache = next(
            (
                entry for entry in reversed(state.region.containers)
                if not entry.opened and entry.id not in state.treasure_marks[state.active_region_id]
            ),
            None,
        ) if state.location == "region" else None
        if cache is None or state.lamp_oil <= 0:
            return _plain(state, action_format("action.item.wreck_light.unavailable"))
        state.lamp_oil -= 1
        _spend_relic(state, "wreck-light prism")
        mark_treasure(
            state, state.active_region_id, cache.id,
            action_format("action.item.wreck_light.mark", cache=cache.name, x=cache.position.x, y=cache.position.y, z=f"{cache.position.z:+d}"),
        )
        animals = humans = 0
        for threat in state.combatants:
            if distance(state.position, threat.position) > 9:
                continue
            if threat.profile == "animal":
                threat.status = "evaded"
                _set_combat_intent(threat, "intent.item.wreck_light")
                animals += 1
            elif threat.status == "watching":
                threat.status = "engaged"
                threat.last_known_position = state.position
                humans += 1
        return _time_result(
            state,
            action_format("action.item.wreck_light.used", cache=cache.name, animals=animals, humans=humans),
            priority=3,
        )
    if "signal mirror" in state.carried_passives and state.position.z > 0:
        target = next(
            (
                threat for threat in state.combatants
                if threat.aimed_at is not None
                and distance(state.position, threat.position) <= 12
                and line_of_sight(state, state.position, threat.position)
            ),
            None,
        )
        if target:
            target.aimed_at = None
            _set_combat_intent(target, "intent.item.signal_mirror")
            for ally in state.combatants:
                if ally.group and ally.group == target.group:
                    ally.last_known_position = state.position
                    if ally.status == "watching":
                        ally.status = "engaged"
            changes = state.vessel_changes if state.location == "jomon" else state.region.changes
            changes["signal_mirror_used"] = True
            return _time_result(
                state,
                action_format("action.item.signal_mirror.used"),
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
                threat.status = "watching"
                _set_combat_intent(threat, "intent.item.smoke_pot")
        return _time_result(
            state,
            action_format("action.item.smoke_pot.used"),
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
            action_format("action.item.bird_whistle.used") + ((" " + " ".join(sounds)) if sounds else ""),
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
                action_format("action.item.cache_bell.mark", cache=cache.name, x=cache.position.x, y=cache.position.y, z=f"{cache.position.z:+d}"),
            )
            sounds = emit_sound(state, 4)
            return _time_result(
                state,
                " ".join([
                    action_format("action.item.cache_bell.used", cache=cache.name),
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
        animal.status = "evaded"
        _set_combat_intent(animal, "intent.item.lantern")
        state.remember(
            action_format("action.item.lantern.memory", courier=state.courier.name)
        )
        return _time_result(
            state,
            action_format("action.item.lantern.used"),
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
            action_format("action.item.willow_dressing.used", amount=amount, method=action_format("action.item.willow_dressing.method") if amount == 5 else ""),
            priority=3,
        )
    if state.consumables.get("dry smoke charge", 0) and state.smoke_charges == 0:
        state.consumables["dry smoke charge"] -= 1
        if state.consumables["dry smoke charge"] == 0:
            del state.consumables["dry smoke charge"]
        consume_carried(state, "consumable:dry smoke charge")
        state.smoke_charges = 1
        return _time_result(
            state, action_format("action.item.smoke_charge.used"), priority=3
        )
    if state.consumables.get("dry lamp wick", 0):
        state.consumables["dry lamp wick"] -= 1
        if state.consumables["dry lamp wick"] == 0:
            del state.consumables["dry lamp wick"]
        consume_carried(state, "consumable:dry lamp wick")
        state.lamp_oil += 2
        return _time_result(state, action_format("action.item.lamp_wick.used"), priority=3)
    if state.consumables.get("brine wash", 0) and ({"salt-grit", "cut-feet"} & set(state.terrain_statuses)):
        state.consumables["brine wash"] -= 1
        if state.consumables["brine wash"] == 0:
            del state.consumables["brine wash"]
        consume_carried(state, "consumable:brine wash")
        state.terrain_statuses.pop("salt-grit", None)
        state.terrain_statuses.pop("cut-feet", None)
        add_status(state, "brine-chill", action_format("action.status.brine.cause"), 3, action_format("action.status.brine.consequence"))
        return _time_result(state, action_format("action.item.brine_wash.used"), priority=3)
    if state.consumables.get("splint roll", 0) and state.courier and ({"arms", "legs"} & set(state.courier.injuries)):
        location = "arms" if "arms" in state.courier.injuries else "legs"
        state.consumables["splint roll"] -= 1
        if state.consumables["splint roll"] == 0:
            del state.consumables["splint roll"]
        consume_carried(state, "consumable:splint roll")
        state.courier.injuries[location] = f"splinted {location}"
        return _time_result(state, action_format("action.item.splint.used", location=location), priority=3)
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
            action_format("action.item.green_poultice.used", location=location),
            priority=3,
        )
    return _plain(state, action_format("action.item.none"))


def negotiate(state: GameState) -> ActionResult:
    if not state.combat_active or state.courier is None:
        return _plain(state, action_format("social.negotiate.unavailable"))
    humans = sorted([
        threat for threat in state.combatants
        if threat.status == "engaged"
        and threat.profile in {"pursuer", "reach", "ranged"}
        and distance(state.position, threat.position) <= 4
    ], key=lambda threat: (distance(state.position, threat.position), threat.id))
    if not humans:
        return _plain(state, action_format("social.negotiate.no_human"))
    from .worklines import evidence_leverage
    speaker = humans[0]
    undertaking_terms = evidence_leverage(state, speaker)
    has_terms = (
        state.courier.technique == "measured terms"
        or state.gear == "trade seals"
        or state.support == "factor surety"
        or "paper" in state.carried_goods
        or "valuable leverage" in build_combinations(state)
        or "stillroom-cordial" in state.drink_effects
        or undertaking_terms
    )
    if not has_terms:
        return _plain(
            state,
            action_format("social.negotiate.no_terms"),
        )
    witnessed = undertaking_terms or bool(state.objective_evidence) or state.objective_status in {
        "altered", "completed",
    }
    if speaker.elite and not witnessed:
        return _plain(
            state,
            action_format("social.negotiate.elite_evidence"),
        )
    violence_started = any(threat.health < threat.max_health for threat in humans)
    if violence_started and speaker.morale > 2 and not witnessed:
        return _plain(
            state,
            action_format("social.negotiate.violence_evidence"),
        )
    if speaker.group:
        from .character import effective_competency

        heard = [
            threat for threat in humans
            if threat.group == speaker.group
            and distance(speaker.position, threat.position) <= 3
        ][:2 + int(effective_competency(state.courier, "speech") >= 5)
          + int("careful-terms" in state.courier.skill_nodes)]
    else:
        heard = [speaker]
    if "paper" in state.carried_goods and state.gear != "trade seals":
        if not consume_carried(state, "commodity:paper"):
            state.carried_goods["paper"].quantity -= 1
            if state.carried_goods["paper"].quantity == 0:
                del state.carried_goods["paper"]
    for threat in heard:
        threat.status = "negotiated"
        _set_combat_intent(threat, "intent.social.negotiated")
    drawback = ""
    if state.location == "region" and "stillroom-cordial" in state.drink_effects and state.contact.disposition <= 0:
        state.contact.disposition = max(-3, state.contact.disposition - 1)
        drawback = action_format("social.negotiate.drawback")
    memory = action_format("social.negotiate.memory", courier=state.courier.name, count=len(heard))
    state.remember(memory)
    _remember_contact(state, memory)
    state.courier.speech = min(20, state.courier.speech + 1)
    from .skill_tree import record_milestone

    record_milestone(state, "social:mediation")
    return _time_result(
        state,
        action_format("social.negotiate.success", count=len(heard), drawback=drawback),
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
        return _plain(state, action_format("action.retreat.none"))
    has_line = (
        bool(state.smoke)
        or state.flood_control == "lowered"
        or state.gear in {"rope", "smoke pot"}
    )
    if pressure(state).depth >= 5 and not has_line:
        return _plain(
            state,
            action_format("action.retreat.blocked"),
        )
    message = _return_after_defeat(
        state, action_format("action.retreat.used"), permanent=False
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
    implements = [key for key, definition in WORK_WEAPONS.items() if state.active_region_id in definition.regions]
    implements.extend(key for key, definition in ARSENAL.items() if state.active_region_id in definition.regions)
    if implements:
        selected_arm = stage_rng(state.seed, f"working-arms:{state.active_region_id}:{state.returned_expeditions}").choice(implements)
        stock.append(selected_arm)
        bombs = [f"{name.split()[0]} bombs" for name, definition in ARSENAL.items()
                 if definition.family == "device" and state.active_region_id in definition.regions]
        accessory = (
            "handgonne charges" if selected_arm in ARSENAL and ARSENAL[selected_arm].family == "gun" else
            stage_rng(state.seed, f"device-stock:{state.active_region_id}:{state.returned_expeditions}").choice(bombs)
            if bombs and state.returned_expeditions % 2 else
            stage_rng(state.seed, f"pot-stock:{state.active_region_id}:{state.returned_expeditions}").choice(("sealed pitch pot", "sealed lime pot", "sealed brine pot"))
        )
        stock.append(accessory)
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
        return _plain(state, action_format("social.merchant.unavailable"))
    cost, kind = MERCHANT_ITEMS[item]
    cost = max(1, cost - (1 if state.support == "factor surety" else 0)
               - int(bool(state.courier and "price-sense" in state.courier.skill_nodes)))
    account = state.institutions.get(f"work:{state.active_region_id}")
    cost = max(1, cost - (1 if account and account.trust >= 2 else 0))
    if state.trade_credit < cost:
        return _plain(
            state, action_format("social.merchant.insufficient_credit", cost=cost, credit=state.trade_credit)
        )
    physical_kind = (
        f"consumable:{item}" if kind == "consumable" else
        f"relic:{item}" if kind == "relic" else item
    )
    quantity = 3 if item == "handgonne charges" else 1
    physical = create_item(state, physical_kind, action_format("action.item.origin.merchant"), quantity=quantity)
    if not auto_place(state, physical.id, "locker"):
        state.items.remove(physical)
        return _plain(state, action_format("action.merchant.locker_full"))
    state.trade_credit -= cost
    from .skill_tree import record_milestone

    record_milestone(state, f"trade:{state.active_region_id}")
    state.merchant_stock.remove(item)
    if kind == "weapon" and item not in state.owned_weapons:
        state.owned_weapons.append(item)
    elif kind == "gear" and item not in state.owned_gear:
        state.owned_gear.append(item)
    elif kind == "consumable":
        state.consumables[item] = state.consumables.get(item, 0) + quantity
    elif kind == "relic":
        state.relics[item] = state.relics.get(item, 0) + 1
    state.remember(
        action_format("social.merchant.memory", cost=cost, item=item, merchant=state.merchant.name)
    )
    state.merchant.memories.append(
        action_format("social.merchant.sold_memory", item=item, courier=state.courier.name, region=state.active_region_id)
    )
    del state.merchant.memories[:-8]
    state.merchant.relationships[state.active_courier_id] = min(
        3, state.merchant.relationships.get(state.active_courier_id, 0) + 1
    )
    return _time_result(
        state, action_format("social.merchant.purchased", item=item_display_name_or_legacy(item)), priority=3
    )


def return_to_jomon(state: GameState) -> ActionResult:
    if (
        state.location != "region"
        or state.position != state.region.landmarks["landing"]
    ):
        return _plain(state, action_format("action.return.landing", region=state.region.name))
    if state.active_vehicle_id:
        return _plain(state, action_format("action.return.disembark"))
    if state.expedition_by_tug:
        from .vehicles import SHORE_DOCK

        state.location, state.current_room = "jomon", None
        state.jomon_space, state.position, state.active_vehicle_id = "harbour", SHORE_DOCK, "tug"
        state.vehicles["tug"].position = SHORE_DOCK
        state.expedition_by_tug, state.returning_by_tug = False, True
        return _time_result(state, action_format("action.return.tug"))
    return _finish_expedition_return(state)


def _finish_expedition_return(state: GameState) -> ActionResult:
    courier = state.courier
    if state.objective_status in {"accepted", "altered"}:
        state.objective_status = "failed"
        state.contact.disposition = max(-3, state.contact.disposition - 1)
        _remember_contact(
            state, action_format("action.return.objective_failed", courier=courier.name)
        )
    for name, stack in state.carried_goods.items():
        vessel = state.vessel_cargo.get(name)
        if vessel:
            vessel.quantity += stack.quantity
            if "spoiled" in {vessel.condition, stack.condition}:
                vessel.condition = "spoiled"
            elif vessel.condition != stack.condition:
                vessel.condition = stack.condition if stack.condition.startswith("weathered ") else vessel.condition
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
    from .people import record_personal_return, unlock_region_visitors

    completed_region = state.active_region_id
    store_active_region(state)
    state.location, state.current_room, state.position = (
        "jomon",
        None,
        JOMON_GANGPLANK,
    )
    state.jomon_space = "vessel"
    state.returned_expeditions += 1
    from .skill_tree import record_milestone

    record_milestone(state, f"return:{completed_region}")
    for formula in courier.known_formulas:
        if formula not in state.household_formulas:
            state.household_formulas.append(formula)
    development = record_personal_return(state, courier, completed_region)
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
        action_format("action.return.memory", courier=courier.name)
    )
    visitors = unlock_region_visitors(state, completed_region)
    merchant = action_format("action.return.merchant") if state.merchant_present else ""
    visitor_text = (" " + " ".join(visitors)) if visitors else ""
    development_text = (" " + development) if development else ""
    return _time_result(
        state,
        action_format("action.return.completed", merchant=merchant, visitors=visitor_text, development=development_text),
        priority=3,
    )


def purchase_bar_drink(state: GameState, drink_id: str, *, bottle: bool) -> ActionResult:
    if state.location != "jomon" or state.jomon_space != "tavern":
        return _plain(state, action_format("action.drink.tavern_required"))
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


def use_contact_service(
    state: GameState, choice: str, contact_id: str | None = None
) -> ActionResult:
    from .quests import use_secondary_service

    changed, message = use_secondary_service(state, choice, contact_id)
    return _time_result(state, message, priority=3) if changed else _plain(state, message)


def use_aftermath_contract(
    state: GameState, contract_id: str, choice: str
) -> ActionResult:
    from .aftermath import resolve_contract

    changed, message = resolve_contract(state, contract_id, choice)
    if changed:
        from .skill_tree import record_milestone

        record_milestone(state, "social:contract")
    return _time_result(state, message, priority=3) if changed else _plain(state, message)


def intervene_socially(state: GameState, response: str) -> ActionResult:
    if state.location != "jomon" or state.jomon_space != "tavern":
        return _plain(state, action_format("social.incident.unavailable"))
    changed, message = resolve_social_incident(state, response)
    if not changed:
        return _plain(state, message)
    if response == "mediate":
        from .skill_tree import record_milestone

        record_milestone(state, "social:mediation")
    return _time_result(state, message, priority=3)


def use_route_stop(state: GameState, response: str) -> ActionResult:
    from .travel_presentation import route_node_name, travel_text

    node = state.route_nodes.get(state.route_current_node)
    if state.location != "jomon" or node is None or node.region_id:
        return _plain(state, action_format("action.route.unavailable"))
    if response == "resupply":
        available_key = f"supply_available:{node.id}"
        available = int(state.vessel_changes.get(available_key, 0))
        if available <= 0:
            return _plain(state, action_format("action.route.resupply.exhausted"))
        if state.trade_credit < 1:
            return _plain(state, action_format("action.route.resupply.credit"))
        state.trade_credit -= 1
        state.vessel_changes[available_key] = available - 1
        stack = state.vessel_cargo.setdefault("grain", CommodityStack(0, "dry"))
        stack.quantity += 1
        return _time_result(state, action_format("action.route.resupply.used", node=route_node_name(node)), steps=2, priority=3)
    if response == "trade":
        commodity = node.market_interest
        stack = state.vessel_cargo.get(commodity)
        if not commodity or stack is None or stack.quantity <= 0:
            return _plain(state, action_format("action.route.trade.unavailable", node=route_node_name(node), commodity=item_display_name_or_legacy(commodity) if commodity else travel_text("travel.route.stop.no_cargo")))
        stack.quantity -= 1
        if stack.quantity == 0:
            del state.vessel_cargo[commodity]
        state.trade_credit += 2
        state.vessel_changes[f"market_served:{node.id}"] = True
        from .skill_tree import record_milestone

        record_milestone(state, "social:market")
        return _time_result(state, action_format("action.route.trade.used", node=route_node_name(node), commodity=item_display_name_or_legacy(commodity)), steps=2, priority=3)
    if response == "sound":
        from .route_chart import neighbours

        revealed = [node_id for node_id in neighbours(state, node.id) if node_id not in state.route_known]
        if not revealed:
            return _plain(state, action_format("action.route.sound.complete"))
        state.route_known.extend(revealed)
        state.route_known = sorted(set(state.route_known))
        return _time_result(state, action_format("action.route.sound.used", nodes=", ".join(route_node_name(state.route_nodes[item]) for item in revealed)), priority=3)
    return _plain(state, action_format("action.route.invalid"))
