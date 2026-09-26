"""Counted voyage situations on Jomon's existing decks and material rules."""

from __future__ import annotations

from .catalog import VESSEL_SECTIONS, load_catalog
from .item_presentation import item_display_name_or_legacy
from .ship_crisis_presentation import crisis_description, crisis_title, ship_crisis_format, ship_crisis_text
from .travel_presentation import variant_cause, variant_counterplay, variant_effect
from .state import GameState, Position, Threat

_VESSEL = load_catalog("vessel.json", VESSEL_SECTIONS)
VOYAGES = {kind: tuple(row) for kind, row in _VESSEL["voyages"].items()}
TACTICAL = frozenset({"raiders", "creature", "boarders", "hold-thieves", "storm", "galley-fire", "split-seam", "flooded-hold"})
HAZARD_STATIONS = {kind: Position(**row) for kind, row in _VESSEL["hazard_stations"].items()}


def choices(state: GameState) -> list[tuple[str, str, str]]:
    """Return engine response keys with selected-pack labels."""
    kind = state.voyage_kind
    from .voyage_variants import active_variant

    variant = active_variant(state, kind)
    if state.vessel_changes.get("deck_crisis"):
        return [("P", ship_crisis_text("crisis.choice.deck.return"), "danger"), ("Y", ship_crisis_text("crisis.choice.deck.withdraw"), "refusal")]
    options = {
        "raiders": [("R", "crisis.choice.raiders.repel", "danger"), ("D", "crisis.choice.raiders.distract", "commitment"), ("Y", "crisis.choice.raiders.yield", "refusal")],
        "creature": [("R", "crisis.choice.creature.repel", "danger"), ("E", "crisis.choice.creature.evade", "commitment"), ("B", "crisis.choice.creature.bait.pair" if variant else "crisis.choice.creature.bait.single", "commitment")],
        "lure": [("A", "crisis.choice.lure.anchor", "commitment"), ("C", "crisis.choice.lure.counsel", "ordinary"), ("N", "crisis.choice.lure.navigate", "commitment")],
        "shoal": [("N", "crisis.choice.shoal.navigate.variant" if variant else "crisis.choice.shoal.navigate", "commitment"), ("Y", "crisis.choice.shoal.yield.variant" if variant else "crisis.choice.shoal.yield", "danger")],
        "driftwood": [("R", "crisis.choice.driftwood.repel.variant" if variant else "crisis.choice.driftwood.repel", "commitment"), ("Y", "crisis.choice.driftwood.yield", "refusal")],
        "inspection": [("N", "crisis.choice.inspection.navigate", "ordinary"), ("C", "crisis.choice.inspection.counsel.variant" if variant else "crisis.choice.inspection.counsel", "commitment"), ("Y", "crisis.choice.inspection.yield", "refusal")],
    }.get(kind, [("Y", "crisis.choice.default.withdraw", "refusal")])
    if kind == "boarders" and variant:
        options = [("C", "crisis.choice.boarders.counsel", "commitment"), *options]
    if kind in TACTICAL:
        options = [("P", "crisis.choice.tactical.deck", "danger"), *options]
    return [(key, ship_crisis_text(text_id), semantic) for key, text_id, semantic in options]


def crisis_lines(state: GameState) -> list[str]:
    lines = [state.voyage_detail]
    from .voyage_variants import active_variant

    variant = active_variant(state, state.voyage_kind)
    if variant:
        lines += [
            ship_crisis_format("crisis.line.variant.cause", cause=variant_cause(variant.id)),
            ship_crisis_format("crisis.line.variant.effect", effect=variant_effect(variant.id)),
            ship_crisis_format("crisis.line.variant.counterplay", counterplay=variant_counterplay(variant.id)),
        ]
    if state.vessel_changes.get("deck_crisis"):
        kind = state.voyage_kind
        station = HAZARD_STATIONS.get(kind)
        lines += [ship_crisis_format("crisis.line.integrity", integrity=state.vessel_integrity, threats=sum(a.status in {"watching", "engaged"} for a in state.vessel_threats))]
        lines += [ship_crisis_format("crisis.line.work_position", x=station.x, y=station.y, z=f"{station.z:+d}")] if station else [ship_crisis_text("crisis.line.boarders")]
        if kind == "flooded-hold":
            from .circuits import cell_key
            pump = state.circuits.get(cell_key("vessel", Position(station.x + 2, station.y, station.z), "surface"))
            if pump and pump.kind == "drain":
                lines += [ship_crisis_text("crisis.line.pump")]
        lines += [ship_crisis_text("crisis.line.help")]
    return lines + [ship_crisis_text("crisis.line.pending")]


def _set_crisis_intent(actor: Threat, intent_id: str) -> None:
    actor.intent_id = intent_id
    actor.intent = ship_crisis_text(intent_id)


def _spawn(state: GameState, actor_slot: str, position: Position, role: str, *, weapon="spear", duty="", profile="pursuer", health=6) -> Threat:
    name = ship_crisis_text(f"crisis.actor.{actor_slot}")
    from .world import distance, is_walkable

    occupied = {a.position for a in state.vessel_threats}
    points = [Position(x, y, position.z) for y in range(max(1, position.y - 5), min(21, position.y + 6)) for x in range(max(1, position.x - 5), min(63, position.x + 6))]
    valid = [p for p in points if p not in occupied and distance(p, state.position) >= 7 and is_walkable(state, p)]
    if not valid:
        valid = [Position(x, y, position.z) for y in range(1, 21) for x in range(1, 63)
                 if Position(x, y, position.z) not in occupied and distance(Position(x, y, position.z), state.position) >= 7 and is_walkable(state, Position(x, y, position.z))]
    if not valid:
        raise ValueError("no fair physical boarding position")
    start = min(valid, key=lambda p: (distance(p, position), p.y, p.x))
    actor = Threat(f"voyage-{state.travel_count}-{len(state.vessel_threats)}", name, profile, start, health, health,
                   archetype_id="",
                   status="watching", role=role, ranged_kind=weapon, ammunition=4 if profile == "ranged" else 0,
                   morale=4, vision=14 if profile == "ranged" else 10, hearing=8,
                   home_position=start, region_id="jomon", group=f"boarding-{state.travel_count}", allegiance="cargo-skiffs",
                   ecology="raider" if profile != "animal" else "territorial", duty=duty,
                   capabilities=["stairs", "doors", "steal", "escape"] if profile != "animal" else ["no-climb", "defends rudder territory"],
                   goal="crisis.cargo_claim")
    _set_crisis_intent(actor, "intent.crisis.observe")
    state.vessel_threats.append(actor)
    from .enemy_equipment import issue_enemy_equipment

    issue_enemy_equipment(state, actor, "jomon")
    return actor


def begin_deck(state: GameState) -> tuple[bool, str]:
    from .inventory import create_item
    from .materials import key
    from .state import MaterialCell

    if state.voyage_status != "active" or state.voyage_kind not in TACTICAL or state.jomon_space != "vessel":
        return False, ship_crisis_text("crisis.begin.invalid")
    if state.vessel_changes.get("deck_crisis"):
        return True, ship_crisis_text("crisis.begin.continues")
    state.vessel_threats.clear()
    state.vessel_changes.update(deck_crisis=True, deck_ticks=0, deck_work_done=False)
    state.smoke, state.water, state.sound_events, state.group_alerts = {}, {}, [], {}
    kind = state.voyage_kind
    from .vessel_refits import installed
    from .voyage_variants import active_variant

    variant = active_variant(state, kind)

    if kind == "raiders":
        _spawn(state, "raider_hook", Position(54, 10), "thief", duty="scavenge")
        _spawn(state, "raider_ward", Position(52, 12), "protector", profile="reach")
        if variant:
            _spawn(state, "raider_caller", Position(50, 8), "controller", profile="ranged", weapon="sling", duty="escort")
    elif kind == "boarders":
        _spawn(state, "boarder_bow", Position(55, 10, 1), "skirmisher", profile="ranged", weapon="longbow")
        _spawn(state, "boarder_shield", Position(47, 11, 0), "protector", profile="reach")
        _spawn(state, "boarder_hook", Position(35, 12, 0), "flanker")
        if variant:
            _spawn(state, "boarder_claimant", Position(31, 12, 0), "protector", profile="reach", duty="escort")
    elif kind == "hold-thieves":
        thief = _spawn(state, "hold_recoverer", Position(14, 11, -1), "thief", duty="scavenge")
        if installed(state, "cargo-rail-netting"):
            thief.conditions["net-drag"] = 3
            _set_crisis_intent(thief, "intent.crisis.netting")
        _spawn(state, "hold_net", Position(19, 10, -1), "controller", weapon="weighted net", duty="escort")
        available = [name for name, stack in state.vessel_cargo.items() if stack.quantity > 0]
        if variant:
            def shortage(name: str) -> int:
                return max((market[name].demand - market[name].stock for market in state.regional_markets.values() if name in market), default=0)
            cargo = max(available, key=lambda name: (shortage(name), name)) if available else None
        else:
            cargo = min(available) if available else None
        if cargo:
            state.vessel_cargo[cargo].quantity -= 1
            if state.vessel_cargo[cargo].quantity == 0:
                del state.vessel_cargo[cargo]
            item = create_item(state, f"commodity:{cargo}", ship_crisis_format("crisis.item.unsecured_shipment", voyage=state.travel_count), location="ground")
            item.region_id, item.ground_position = "jomon", Position(17, 11, -1)
            thief.objective_position = item.ground_position
    elif kind == "creature":
        _spawn(state, "rudder_grazer", Position(59, 10), "territorial", profile="animal", health=9)
        if variant:
            _spawn(state, "rudder_mate", Position(54, 15), "territorial", profile="animal", health=7)
    else:
        station = HAZARD_STATIONS[kind]
        point = Position(station.x + 1, station.y, station.z)
        if kind == "galley-fire":
            state.vessel_materials[key(point)] = MaterialCell(
                material="oil", fire=1,
                fuel=5 if installed(state, "galley-fire-cover") else 8,
                smoke=1,
            )
            state.vessel_materials[key(Position(station.x, station.y + 1, station.z))] = MaterialCell(material="stone", water=2)
            for x in (10, 11, 12):
                state.vessel_materials.setdefault(key(Position(x, station.y, station.z)), MaterialCell(material="cloth"))
            if variant:
                state.vessel_materials[key(Position(station.x + 2, station.y, station.z))] = MaterialCell(material="cloth", fire=1, fuel=5, smoke=2, coating="oil")
        elif kind == "storm":
            # the normal chart-to-stay walk takes 27 moves; allow work before failure.
            state.vessel_materials[key(point)] = MaterialCell(
                material="timber",
                support=2 if installed(state, "storm-backstay") else 1,
                collapse_due=state.world_time + (52 if installed(state, "storm-backstay") else 40),
            )
            if variant:
                state.vessel_materials[key(Position(station.x - 1, station.y, station.z))] = MaterialCell(material="timber", support=1, collapse_due=state.world_time + 32)
        else:
            state.vessel_materials[key(point)] = MaterialCell(
                material="timber",
                water=(2 if installed(state, "twin-bilge-strainers") else 3) if variant else (1 if installed(state, "twin-bilge-strainers") else 2),
                support=1 if variant and kind == "split-seam" else 2,
            )
            if variant:
                state.vessel_materials[key(Position(station.x + 2, station.y, station.z))] = MaterialCell(material="timber", water=2, support=1 if kind == "split-seam" else 2)
    message = (ship_crisis_format("crisis.begin.alarm.station", title=crisis_title(kind), station=HAZARD_STATIONS[kind]) if kind in HAZARD_STATIONS else ship_crisis_format("crisis.begin.alarm.boarders", title=crisis_title(kind)))
    state.remember(message)
    state.add_message(message, priority=3)
    return True, message


def _finish(state: GameState, message: str) -> str:
    from .travel import _finish_travel
    from .inventory import release_enemy_possession

    kind = state.voyage_kind
    for actor in state.vessel_threats:
        release_enemy_possession(state, actor)
        if actor.status in {"watching", "engaged"}:
            actor.status = "retreated"
            _set_crisis_intent(actor, "intent.crisis.retreated")
    state.vessel_changes["deck_crisis"] = False
    state.vessel_changes[f"voyage_outcome:{kind}"] = message
    state.voyage_status = "resolved"
    _finish_travel(state, message)
    state.voyage_kind = None
    state.chronicle.append(ship_crisis_format("crisis.finish.chronicle", voyage=state.travel_count, kind=kind, message=message))
    del state.chronicle[:-40]
    return message


def abandon_deck(state: GameState) -> str:
    from .travel import _lose_vessel_cargo

    loss = _lose_vessel_cargo(state)
    state.vessel_integrity = max(1, state.vessel_integrity - 2)
    return _finish(state, ship_crisis_format("crisis.finish.abandon", loss=loss))


def advance_deck(state: GameState) -> None:
    if not state.combat_active or state.location != "jomon":
        return
    from .materials import ensure_cell, key, affect_body
    from .world import distance, line_of_sight

    ticks = int(state.vessel_changes.get("deck_ticks", 0)) + 1
    state.vessel_changes["deck_ticks"] = min(1000, ticks)
    kind = state.voyage_kind
    station = HAZARD_STATIONS.get(kind)
    if station and not state.vessel_changes.get("deck_work_done"):
        cell = state.vessel_materials.get(key(Position(station.x + 1, station.y, station.z)))
        settled = (kind == "galley-fire" and not any(c.fire for c in state.vessel_materials.values())
                   or kind == "storm" and cell and cell.support >= 3 and not cell.collapse_due
                   or kind == "split-seam" and cell and cell.support >= 3 and cell.water <= 1
                   or kind == "flooded-hold" and sum(c.water for c in state.vessel_materials.values()) <= 1)
        if settled:
            _finish(state, ship_crisis_text("crisis.finish.hazard_settled"))
            return
    from .vessel_refits import installed

    flood_interval = 6 if installed(state, "twin-bilge-strainers") else 4
    if station and not state.vessel_changes.get("deck_work_done") and ticks <= 24 and ticks % flood_interval == 0:
        point = Position(station.x + 1, station.y, station.z)
        cell = ensure_cell(state, point)
        if cell and kind in {"split-seam", "flooded-hold"}:
            cell.water = min(3, cell.water + 1)
        if kind in {"split-seam", "flooded-hold"} and ticks >= 12 and ticks % 8 == 0:
            state.vessel_integrity = max(1, state.vessel_integrity - 1)
        state.add_message(ship_crisis_format("crisis.advance.work_undone", title=crisis_title(kind), x=station.x, y=station.y, z=f"{station.z:+d}", integrity=state.vessel_integrity), priority=3)
    # nearby adults help physically; no remote attack or silent named death.
    if ticks % 3 == 0:
        for person in state.household:
            schedule = state.actor_schedules.get(person.id)
            if person.id == state.active_courier_id or not person.alive or person.injuries or schedule is None or not schedule.area.startswith("vessel:"):
                continue
            targets = [actor for actor in state.vessel_threats if actor.status == "engaged" and distance(schedule.position, actor.position) <= 1 and line_of_sight(state, schedule.position, actor.position)]
            if targets and person.role in {"guard", "carpenter", "bargemaster"}:
                target = min(targets, key=lambda actor: actor.id)
                affect_body(state, target, "debris", 1, target.position)
                state.add_message(ship_crisis_format("crisis.advance.assist", person=person.name, target=target.name), priority=3)
                break
    for actor in state.vessel_threats:
        withdrawing = actor.morale <= 0 or actor.goal == "break contact"
        if actor.status != "engaged" or not withdrawing:
            continue
        if actor.home_position and distance(actor.position, actor.home_position) <= 2:
            actor.status = "retreated"
            _set_crisis_intent(actor, "intent.crisis.retreated")
            state.add_message(ship_crisis_format("crisis.advance.leave", threat=actor.name), priority=3)
        elif actor.stalled_turns >= 3:
            actor.status = "negotiated"
            _set_crisis_intent(actor, "intent.crisis.surrendered")
    if not station and state.vessel_threats and all(actor.status not in {"watching", "engaged"} for actor in state.vessel_threats):
        _finish(state, ship_crisis_text("crisis.finish.decks_clear"))


def station_action(state: GameState, tile: str):
    from .actions import ActionResult

    if state.combat_active:
        if state.voyage_kind == "creature" and tile in {"G", "H"}:
            return ActionResult(False, False, ship_crisis_text("crisis.station.bait"), "ship-work:bait")
        if state.position == HAZARD_STATIONS.get(state.voyage_kind):
            return ActionResult(False, False, ship_crisis_text("crisis.station.emergency"), "ship-work:emergency")
        if tile == "+" and state.position.x == 63:
            return ActionResult(False, False, ship_crisis_text("crisis.station.gangplank"), "voyage")
    if tile == "R":
        return ActionResult(False, False, ship_crisis_text("crisis.station.repair"), "ship-work:repair")
    if tile == "U":
        return ActionResult(False, False, ship_crisis_text("crisis.station.pump"), "ship-work:pump")
    if tile == "G":
        return ActionResult(False, False, ship_crisis_text("crisis.station.meal"), "ship-work:meal")
    if tile == "b":
        return ActionResult(False, False, ship_crisis_text("crisis.station.treat"), "ship-work:treat")
    return None


def work_lines(state: GameState, task: str) -> list[str]:
    cargo = "; ".join(ship_crisis_format("crisis.work.line.cargo", item=item_display_name_or_legacy(name), quantity=state.vessel_cargo[name].quantity if name in state.vessel_cargo else 0) for name in ("timber", "grain", "salt fish"))
    detail = state.voyage_detail if state.combat_active else ship_crisis_text("crisis.work.moored")
    lines = [
        ship_crisis_format("crisis.work.line.header", integrity=state.vessel_integrity, detail=detail),
        ship_crisis_text(f"crisis.work.detail.{task}"),
        ship_crisis_format("crisis.work.line.hold", cargo=cargo, ready=item_display_name_or_legacy(state.gear) if state.gear else ship_crisis_text("crisis.work.no_readied")),
    ]
    from .voyage_variants import active_variant
    variant = active_variant(state, state.voyage_kind)
    if variant and task in {"emergency", "bait"}:
        lines.append(ship_crisis_format("crisis.work.line.variant", effect=variant_effect(variant.id), counterplay=variant_counterplay(variant.id)))
    return lines + [ship_crisis_text("crisis.work.line.confirm")]


def work(state: GameState, task: str) -> tuple[bool, str]:
    from .actions import _advance_world
    from .world import base_tile
    from .materials import key

    if state.location != "jomon" or state.jomon_space != "vessel":
        return False, ship_crisis_text("crisis.work.invalid_station")
    tile = base_tile(state, state.position)
    from .vessel_refits import installed
    from .voyage_variants import active_variant

    variant = active_variant(state, state.voyage_kind)

    cost, message = 2, ""
    if task == "bait" and tile in {"G", "H"} and state.voyage_kind == "creature" and state.combat_active:
        fish = state.vessel_cargo.get("salt fish")
        required = 2 if variant else 1
        if not fish or fish.quantity < required:
            return False, ship_crisis_format("crisis.work.bait_requirement", required=required, suffix="s" if required > 1 else "")
        fish.quantity -= required
        if not fish.quantity:
            del state.vessel_cargo["salt fish"]
        for actor in state.vessel_threats:
            if actor.profile == "animal":
                actor.status = "evaded"
                _set_crisis_intent(actor, "intent.crisis.baited")
        cost, message = 1, ship_crisis_format("crisis.work.bait", required=required, suffix="s" if required > 1 else "", animal="pair" if variant else "grazer")
    elif task == "repair" and tile == "R":
        stack = state.vessel_cargo.get("timber")
        if not stack or stack.quantity <= 0 or state.vessel_integrity >= 10:
            return False, ship_crisis_text("crisis.work.repair_requirement")
        stack.quantity -= 1
        if not stack.quantity:
            del state.vessel_cargo["timber"]
        state.vessel_integrity = min(10, state.vessel_integrity + 3)
        state.vessel_changes["hull_repairs"] = min(1000, int(state.vessel_changes.get("hull_repairs", 0)) + 1)
        message = ship_crisis_text("crisis.work.repair")
    elif task == "pump" and tile == "U":
        if not any(cell.water for cell in state.vessel_materials.values()):
            return False, ship_crisis_text("crisis.work.pump_requirement")
        for coordinate, cell in state.vessel_materials.items():
            if coordinate.endswith(f",{state.position.z}"):
                cell.water = 0
        cost = 1 if installed(state, "twin-bilge-strainers") else 3
        message = ship_crisis_text("crisis.work.pump")
        if cost == 1:
            message += " " + ship_crisis_text("crisis.work.pump.strainers")
    elif task == "meal" and tile == "G":
        food = next((name for name in ("grain", "salt fish") if state.vessel_cargo.get(name) and state.vessel_cargo[name].quantity > 0), None)
        if food is None or state.courier is None:
            return False, ship_crisis_text("crisis.work.meal_requirement")
        state.vessel_cargo[food].quantity -= 1
        if not state.vessel_cargo[food].quantity:
            del state.vessel_cargo[food]
        state.courier.health = min(state.courier.max_health, state.courier.health + 2)
        state.terrain_statuses.pop("fatigue", None)
        state.terrain_statuses.pop("fatigued", None)
        restored = 3 if installed(state, "galley-fire-cover") else 2
        state.courier.health = min(state.courier.max_health, state.courier.health + max(0, restored - 2))
        cost = 3 if installed(state, "galley-fire-cover") else 4
        message = ship_crisis_format("crisis.work.meal", food=food, restored=restored)
    elif task == "treat" and tile == "b":
        if not installed(state, "sickbay-sling-cot"):
            return False, ship_crisis_text("crisis.work.treat.cot")
        if state.courier is None or not state.courier.injuries:
            return False, ship_crisis_text("crisis.work.treat.injury")
        wool = state.vessel_cargo.get("wool")
        if not wool or wool.quantity <= 0:
            return False, ship_crisis_text("crisis.work.treat.wool")
        wool.quantity -= 1
        if wool.quantity == 0:
            del state.vessel_cargo["wool"]
        location = sorted(state.courier.injuries)[0]
        injury = state.courier.injuries.pop(location)
        state.courier.injury = next(iter(state.courier.injuries.values()), "treated soreness")
        cost, message = 6, ship_crisis_format("crisis.work.treat", injury=injury, location=location)
    elif task == "emergency" and state.combat_active and state.position == HAZARD_STATIONS.get(state.voyage_kind):
        if state.voyage_kind == "storm" and state.gear != "rope" and not (state.courier and state.courier.technique == "lever craft") and not installed(state, "storm-backstay"):
            return False, ship_crisis_text("crisis.work.emergency.requirement")
        point = Position(state.position.x + 1, state.position.y, state.position.z)
        cell = state.vessel_materials.get(key(point))
        pressure = False
        auxiliary = Position(
            state.position.x - 1 if state.voyage_kind == "storm" else state.position.x + 2,
            state.position.y,
            state.position.z,
        )
        auxiliary_cell = state.vessel_materials.get(key(auxiliary)) if variant else None
        if auxiliary_cell:
            pressure = (
                bool(auxiliary_cell.fire) if state.voyage_kind == "galley-fire"
                else auxiliary_cell.support < 2 if state.voyage_kind in {"storm", "split-seam"}
                else auxiliary_cell.water > 1
            )
        if cell:
            cell.fire, cell.water, cell.smoke, cell.collapse_due, cell.support = 0, 0, 0, 0, 3
            cell.coating = "wet"
        if auxiliary_cell:
            auxiliary_cell.fire, auxiliary_cell.water, auxiliary_cell.smoke = 0, 0, 0
            auxiliary_cell.collapse_due, auxiliary_cell.support = 0, 3
            auxiliary_cell.coating = "wet"
        state.vessel_changes["deck_work_done"] = True
        accelerated = (
            state.voyage_kind == "storm" and installed(state, "storm-backstay")
            or state.voyage_kind == "galley-fire" and installed(state, "galley-fire-cover")
            or state.voyage_kind in {"split-seam", "flooded-hold"} and installed(state, "twin-bilge-strainers")
        )
        cost = 3 + int(pressure) - int(accelerated)
        message = ship_crisis_text("crisis.work.emergency")
        if pressure:
            message += " " + ship_crisis_text("crisis.work.emergency.pressure")
        if accelerated:
            message += " " + ship_crisis_text("crisis.work.emergency.accelerated")
    else:
        return False, ship_crisis_text("crisis.work.none")
    _advance_world(state, steps=cost)
    if task == "emergency" and state.voyage_status == "active":
        _finish(state, message)
    state.remember(message)
    state.add_message(message, priority=3)
    return True, message


def deck_defeat(state: GameState, text: str, permanent: bool) -> str:
    from .actions import _successor
    from .inventory import sync_legacy_load, ensure_courier_basics
    from .vessel import JOMON_GANGPLANK

    courier = state.courier
    if courier is None:
        return text
    site = state.position
    for item in state.items:
        if item.owner_id == courier.id and (permanent or item.location == "pack"):
            item.location, item.owner_id, item.region_id, item.ground_position = "ground", None, "jomon", site
            item.container_id = None
    if permanent:
        courier.alive, courier.health, courier.injury = False, 0, "dead"
        successor = _successor(state, courier)
        state.active_courier_id = successor.id if successor else None
        state.world_ended = successor is None
        if successor:
            ensure_courier_basics(state, successor)
    else:
        courier.health = max(2, courier.health)
    state.position = JOMON_GANGPLANK
    sync_legacy_load(state)
    message = ship_crisis_format("crisis.defeat.message", text=text, outcome="Death" if permanent else "Injury", x=site.x, y=site.y, z=f"{site.z:+d}")
    if state.vessel_changes.get("deck_crisis"):
        message += " " + abandon_deck(state)
    state.remember(message)
    return message


def validate_ship(state: GameState) -> None:
    from .vessel import VESSEL_LEVELS
    from .voyage_variants import VARIANTS

    if len(state.vessel_threats) > 8 or len({actor.id for actor in state.vessel_threats}) != len(state.vessel_threats):
        raise ValueError("invalid bounded vessel actor identities")
    for actor in state.vessel_threats:
        p = actor.position
        if p.z not in VESSEL_LEVELS or not (0 <= p.x < 64 and 0 <= p.y < 22) or not 0 <= actor.health <= actor.max_health or actor.ammunition < 0:
            raise ValueError("invalid vessel actor state")
    for coordinate, glyph in state.vessel_tiles.items():
        x, y, z = map(int, coordinate.split(","))
        if z not in VESSEL_LEVELS or not (0 <= x < 64 and 0 <= y < 22) or glyph not in {"/", ".", "O", "%"}:
            raise ValueError("invalid vessel geometry mutation")
    if state.vessel_changes.get("deck_crisis") and (state.location != "jomon" or state.voyage_status != "active" or state.voyage_kind not in TACTICAL):
        raise ValueError("orphaned physical voyage crisis")
    marker = state.vessel_changes.get("active_voyage_variant")
    known = {variant.id: variant.family for variant in VARIANTS.values()}
    if marker and (marker not in known or state.voyage_status != "active" or known[marker] != state.voyage_kind):
        raise ValueError("orphaned or mismatched voyage variant")
