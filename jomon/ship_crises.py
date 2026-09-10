"""Counted voyage situations on Jomon's existing decks and material rules."""

from __future__ import annotations

from .state import GameState, Position, Threat

VOYAGES = {
    "raiders": ("Cargo-rail raiders", "Hooked skiffs close on the cargo rail; their goal is theft and escape."),
    "creature": ("Rudder grazer", "A broad-backed river grazer holds the rudder shoal. Height, bait or spaced strikes can free the vessel."),
    "lure": ("The answering hull", "Mineral resonance makes familiar voices seem to call from the wrong bank. Chart, chain and named witnesses remain reliable."),
    "boarders": ("The two-rail boarding", "A bowman holds the upper rail while a hook bearer approaches the stair. Break the lane, divide them across decks or buy passage."),
    "hold-thieves": ("The loosened hatch", "A hold thief seeks a counted shipment below while a net bearer covers the hatch. Recover the physical lot before it escapes."),
    "storm": ("A parting stay", "Wind works a cracked upper stay. The rigging at 55,10,+1 needs bracing; leaving it risks the hull and loose cargo."),
    "shoal": ("The silt shoulder", "Newly deposited silt narrows the sounding. A slower pilot passage saves the hold; forcing it risks the hull."),
    "driftwood": ("A raft without its rope", "Useful timber drifts past a broken carrier raft. A rope can secure one lot, but exposure delays the route."),
    "galley-fire": ("The spilled lamp", "Lamp oil catches beside the galley. Water and the galley fire cover can smother it; smoke can reach the hatch."),
    "split-seam": ("A working hull seam", "A lower seam admits water. The lower repair stores at 20,15,-1 can seat a brace before the hold takes more water."),
    "flooded-hold": ("The loaded bilge", "Water has followed the cargo hatch below. Work the bilge at 8,15,-1 or accept damage and abandoned cargo."),
    "inspection": ("A disputed cargo seal", "A river work patrol asks whose claim supports the cargo. Witnesses, paper or a declared payment can settle the inspection."),
}
TACTICAL = frozenset({"raiders", "creature", "boarders", "hold-thieves", "storm", "galley-fire", "split-seam", "flooded-hold"})
HAZARD_STATIONS = {
    "storm": Position(55, 10, 1), "galley-fire": Position(8, 5, 0),
    "split-seam": Position(20, 15, -1), "flooded-hold": Position(8, 15, -1),
}


def choices(state: GameState) -> list[tuple[str, str, str]]:
    kind = state.voyage_kind
    if state.vessel_changes.get("deck_crisis"):
        return [("P", "Return to the physical deck; normal actions advance danger", "danger"), ("Y", "Abandon contested cargo and withdraw with hull damage", "refusal")]
    options = {
        "raiders": [("R", "Repel with readied reach", "danger"), ("D", "Distract with material preparation", "commitment"), ("Y", "Yield one cargo lot", "refusal")],
        "creature": [("R", "Repel with a spaced weapon", "danger"), ("E", "Evade through pilot knowledge", "commitment"), ("B", "Bait with one salt-fish lot", "commitment")],
        "lure": [("A", "Anchor to the real bank", "commitment"), ("C", "Counsel named crew", "ordinary"), ("N", "Navigate by chart and lead line", "commitment")],
        "shoal": [("N", "Sound a slower channel: four extra actions", "commitment"), ("Y", "Force the shoal: two hull integrity", "danger")],
        "driftwood": [("R", "Secure timber: one rope use, three actions", "commitment"), ("Y", "Let the raft pass without a claim", "refusal")],
        "inspection": [("N", "Offer witnessed institutional trust or one paper lot", "ordinary"), ("C", "Pay two accountable credits", "commitment"), ("Y", "Refuse: lose one cargo lot to detention", "refusal")],
    }.get(kind, [("Y", "Withdraw: abandon cargo and accept two hull damage", "refusal")])
    if kind in TACTICAL:
        options = [("P", "Take the physical deck: staged threats, shared combat and tools", "danger"), *options]
    return options


def crisis_lines(state: GameState) -> list[str]:
    lines = [state.voyage_detail]
    if state.vessel_changes.get("deck_crisis"):
        kind = state.voyage_kind
        station = HAZARD_STATIONS.get(kind)
        lines += [f"Integrity {state.vessel_integrity}/10. {sum(a.status in {'watching', 'engaged'} for a in state.vessel_threats)} active deck threats."]
        lines += [f"Work position: {station.x},{station.y},z{station.z:+d}. E previews the intervention."] if station else ["Defeat, negotiate or drive off the boarders. V offers nearby terms; R accepts withdrawal losses."]
        lines += ["F inspects materials; A targets visible threats; O reads intent. Hatches take one action under danger."]
    return lines + ["Inspection and Escape cost no time. The event remains pending until resolved."]


def _spawn(state: GameState, name: str, position: Position, role: str, *, weapon="spear", duty="", profile="pursuer", health=6) -> Threat:
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
                   status="watching", role=role, ranged_kind=weapon, ammunition=4 if profile == "ranged" else 0,
                   morale=4, vision=14 if profile == "ranged" else 10, hearing=8,
                   home_position=start, region_id="jomon", group=f"boarding-{state.travel_count}", allegiance="cargo-skiffs",
                   ecology="raider" if profile != "animal" else "territorial", duty=duty,
                   capabilities=["stairs", "doors", "steal", "escape"] if profile != "animal" else ["no-climb", "defends rudder territory"],
                   intent="observes the deck before committing", goal="secure a physical cargo claim")
    state.vessel_threats.append(actor)
    from .enemy_equipment import issue_enemy_equipment

    issue_enemy_equipment(state, actor, "jomon")
    return actor


def begin_deck(state: GameState) -> tuple[bool, str]:
    from .inventory import create_item
    from .materials import ensure_cell, key
    from .state import MaterialCell

    if state.voyage_status != "active" or state.voyage_kind not in TACTICAL or state.jomon_space != "vessel":
        return False, "Only an active physical voyage can open the working decks."
    if state.vessel_changes.get("deck_crisis"):
        return True, "The deck crisis continues; no extra time passes while inspecting."
    state.vessel_threats.clear()
    state.vessel_changes.update(deck_crisis=True, deck_ticks=0, deck_work_done=False)
    state.smoke, state.water, state.sound_events, state.group_alerts = {}, {}, [], {}
    kind = state.voyage_kind
    from .vessel_refits import installed

    if kind == "raiders":
        _spawn(state, "cargo-rail hook bearer", Position(54, 10), "thief", duty="scavenge")
        _spawn(state, "skiff ward", Position(52, 12), "protector", profile="reach")
    elif kind == "boarders":
        _spawn(state, "upper-rail bow bearer", Position(55, 10, 1), "skirmisher", profile="ranged", weapon="longbow")
        _spawn(state, "stair shield bearer", Position(47, 11, 0), "protector", profile="reach")
        _spawn(state, "cross-deck hook runner", Position(35, 12, 0), "flanker")
    elif kind == "hold-thieves":
        thief = _spawn(state, "hold recoverer", Position(14, 11, -1), "thief", duty="scavenge")
        if installed(state, "cargo-rail-netting"):
            thief.conditions["net-drag"] = 3
            thief.intent = "cuts through fitted cargo-rail netting before reaching the loose shipment"
        _spawn(state, "hatch net bearer", Position(19, 10, -1), "controller", weapon="weighted net", duty="escort")
        cargo = next((name for name, stack in sorted(state.vessel_cargo.items()) if stack.quantity > 0), None)
        if cargo:
            state.vessel_cargo[cargo].quantity -= 1
            if state.vessel_cargo[cargo].quantity == 0:
                del state.vessel_cargo[cargo]
            item = create_item(state, f"commodity:{cargo}", f"unsecured Jomon shipment, voyage {state.travel_count}", location="ground")
            item.region_id, item.ground_position = "jomon", Position(17, 11, -1)
            thief.objective_position = item.ground_position
    elif kind == "creature":
        _spawn(state, "rudder shoal grazer", Position(59, 10), "territorial", profile="animal", health=9)
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
        elif kind == "storm":
            # the normal chart-to-stay walk takes 27 moves; allow work before failure.
            state.vessel_materials[key(point)] = MaterialCell(
                material="timber",
                support=2 if installed(state, "storm-backstay") else 1,
                collapse_due=state.world_time + (52 if installed(state, "storm-backstay") else 40),
            )
        else:
            state.vessel_materials[key(point)] = MaterialCell(
                material="timber",
                water=1 if installed(state, "twin-bilge-strainers") else 2,
                support=2,
            )
    message = f"Declared deck crisis: {VOYAGES[kind][0]}. Movement now bears time; no actor attacks on entry. " + (f"Work the marked station at {HAZARD_STATIONS[kind]}." if kind in HAZARD_STATIONS else "Watch the boarders' preparation or leave with R.")
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
            actor.status, actor.intent = "retreated", "left the resolved deck dispute"
    state.vessel_changes["deck_crisis"] = False
    state.vessel_changes[f"voyage_outcome:{kind}"] = message
    state.voyage_status = "resolved"
    _finish_travel(state, message)
    state.voyage_kind = None
    state.chronicle.append(f"Voyage {state.travel_count}, {kind}: {message}")
    del state.chronicle[:-40]
    return message


def abandon_deck(state: GameState) -> str:
    from .travel import _lose_vessel_cargo

    loss = _lose_vessel_cargo(state)
    state.vessel_integrity = max(1, state.vessel_integrity - 2)
    return _finish(state, f"The courier orders withdrawal; {loss}; hull loses two integrity. Existing fire and water remain for repair.")


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
            _finish(state, "The physical hazard is no longer active; material intervention or endurance clears the passage. Earlier damage remains.")
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
        state.add_message(f"{VOYAGES[kind][0]}: the work at {station.x},{station.y},z{station.z:+d} remains undone; hull {state.vessel_integrity}/10.", priority=3)
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
                state.add_message(f"{person.name} assists from the adjacent deck position against {target.name}.", priority=3)
                break
    for actor in state.vessel_threats:
        withdrawing = actor.morale <= 0 or actor.goal == "break contact"
        if actor.status != "engaged" or not withdrawing:
            continue
        if actor.home_position and distance(actor.position, actor.home_position) <= 2:
            actor.status, actor.intent = "retreated", "abandons the claim and returns to its boarding point"
            state.add_message(f"{actor.name} leaves by its boarding point; the failed claim is recorded.", priority=3)
        elif actor.stalled_turns >= 3:
            actor.status, actor.intent = "negotiated", "surrenders after failing to find a withdrawal route"
    if not station and state.vessel_threats and all(actor.status not in {"watching", "engaged"} for actor in state.vessel_threats):
        _finish(state, "The contested decks are clear. Unclaimed physical possessions remain where they fell.")


def station_action(state: GameState, tile: str):
    from .actions import ActionResult

    if state.combat_active:
        if state.voyage_kind == "creature" and tile in {"G", "H"}:
            return ActionResult(False, False, "Offer counted fish from a physical hold position.", "ship-work:bait")
        if state.position == HAZARD_STATIONS.get(state.voyage_kind):
            return ActionResult(False, False, "Preview the physical emergency work.", "ship-work:emergency")
        if tile == "+" and state.position.x == 63:
            return ActionResult(False, False, "The gangplank cannot leave an unresolved voyage.", "voyage")
    if tile == "R":
        return ActionResult(False, False, "Count repair material before working.", "ship-work:repair")
    if tile == "U":
        return ActionResult(False, False, "Inspect the bilge before pumping.", "ship-work:pump")
    if tile == "G":
        return ActionResult(False, False, "Prepare a meal from counted provisions.", "ship-work:meal")
    if tile == "b":
        return ActionResult(False, False, "Inspect injury treatment at this berth.", "ship-work:treat")
    return None


def work_lines(state: GameState, task: str) -> list[str]:
    details = {
        "repair": "Two action-clock steps and one timber lot restore three hull integrity. No timber means no work or time charged.",
        "pump": "Three action-clock steps pump sparse water from this deck, without erasing fire or changing cargo ownership.",
        "meal": "One grain or salt-fish lot and four action-clock steps restore two health and clear fatigue; persistent injury remains.",
        "emergency": "Three exposed action-clock steps secure this physical control. A rope/lever preparation helps a cracked stay; the galley cover smothers oil, the repair brace seats the seam, and the bilge pumps water.",
        "bait": "One salt-fish lot and one exposed action draw the rudder grazer clear. Fish is consumed; no animal is summoned or slain.",
        "treat": "With a fitted sickbay sling cot, one wool lot and six action-clock steps clear one persistent injury. Lost health and other injuries remain.",
    }
    cargo = "; ".join(f"{name}: {state.vessel_cargo[name].quantity if name in state.vessel_cargo else 0}" for name in ("timber", "grain", "salt fish"))
    return [f"Hull {state.vessel_integrity}/10; {state.voyage_detail if state.combat_active else 'moored work'}", details[task],
            f"Counted hold — {cargo}. Readied: {state.gear or 'none'}.",
            "Confirm F; Escape cancels. Damage already suffered and lost items are not undone."]


def work(state: GameState, task: str) -> tuple[bool, str]:
    from .actions import _advance_world
    from .world import base_tile
    from .materials import key

    if state.location != "jomon" or state.jomon_space != "vessel":
        return False, "Work requires the physical vessel station."
    tile = base_tile(state, state.position)
    from .vessel_refits import installed

    cost, message = 2, ""
    if task == "bait" and tile in {"G", "H"} and state.voyage_kind == "creature" and state.combat_active:
        fish = state.vessel_cargo.get("salt fish")
        if not fish or fish.quantity <= 0:
            return False, "No counted salt fish remains for bait."
        fish.quantity -= 1
        if not fish.quantity:
            del state.vessel_cargo["salt fish"]
        for actor in state.vessel_threats:
            if actor.profile == "animal":
                actor.status, actor.intent = "evaded", "follows the material bait out of the rudder shoal"
        cost, message = 1, "One salt-fish lot draws the territorial grazer clear of the rudder."
    elif task == "repair" and tile == "R":
        stack = state.vessel_cargo.get("timber")
        if not stack or stack.quantity <= 0 or state.vessel_integrity >= 10:
            return False, "Repair needs a timber lot and actual hull damage."
        stack.quantity -= 1
        if not stack.quantity:
            del state.vessel_cargo["timber"]
        state.vessel_integrity = min(10, state.vessel_integrity + 3)
        message = "One timber lot seats a physical hull repair: three integrity restored."
    elif task == "pump" and tile == "U":
        if not any(cell.water for cell in state.vessel_materials.values()):
            return False, "The bilge is already dry; no pumping work is needed."
        for coordinate, cell in state.vessel_materials.items():
            if coordinate.endswith(f",{state.position.z}"):
                cell.water = 0
        cost = 1 if installed(state, "twin-bilge-strainers") else 3
        message = "The bilge shift pumps this deck's counted water. Active unseated seams may admit more."
        if cost == 1:
            message += " Twin strainers make the physical stroke one action."
    elif task == "meal" and tile == "G":
        food = next((name for name in ("grain", "salt fish") if state.vessel_cargo.get(name) and state.vessel_cargo[name].quantity > 0), None)
        if food is None or state.courier is None:
            return False, "The galley needs one counted grain or salt-fish lot."
        state.vessel_cargo[food].quantity -= 1
        if not state.vessel_cargo[food].quantity:
            del state.vessel_cargo[food]
        state.courier.health = min(state.courier.max_health, state.courier.health + 2)
        state.terrain_statuses.pop("fatigue", None)
        state.terrain_statuses.pop("fatigued", None)
        restored = 3 if installed(state, "galley-fire-cover") else 2
        state.courier.health = min(state.courier.max_health, state.courier.health + max(0, restored - 2))
        cost = 3 if installed(state, "galley-fire-cover") else 4
        message = f"A shared {food} meal restores {restored} health and eases fatigue; injuries still need treatment."
    elif task == "treat" and tile == "b":
        if not installed(state, "sickbay-sling-cot"):
            return False, "This berth has no fitted sickbay sling cot."
        if state.courier is None or not state.courier.injuries:
            return False, "The courier has no persistent injury for the sling cot."
        wool = state.vessel_cargo.get("wool")
        if not wool or wool.quantity <= 0:
            return False, "Sling-cot treatment needs one clean wool hold lot."
        wool.quantity -= 1
        if wool.quantity == 0:
            del state.vessel_cargo["wool"]
        location = sorted(state.courier.injuries)[0]
        injury = state.courier.injuries.pop(location)
        state.courier.injury = next(iter(state.courier.injuries.values()), "treated soreness")
        cost, message = 6, f"The sling cot and one wool lot clear {injury} at {location}; lost health and other injuries remain."
    elif task == "emergency" and state.combat_active and state.position == HAZARD_STATIONS.get(state.voyage_kind):
        if state.voyage_kind == "storm" and state.gear != "rope" and not (state.courier and state.courier.technique == "lever craft") and not installed(state, "storm-backstay"):
            return False, "The stay needs a readied rope or lever craft; another courier can prepare it before the next voyage."
        point = Position(state.position.x + 1, state.position.y, state.position.z)
        cell = state.vessel_materials.get(key(point))
        if cell:
            cell.fire, cell.water, cell.smoke, cell.collapse_due, cell.support = 0, 0, 0, 0, 3
            cell.coating = "wet"
        state.vessel_changes["deck_work_done"] = True
        accelerated = (
            state.voyage_kind == "storm" and installed(state, "storm-backstay")
            or state.voyage_kind == "galley-fire" and installed(state, "galley-fire-cover")
            or state.voyage_kind in {"split-seam", "flooded-hold"} and installed(state, "twin-bilge-strainers")
        )
        cost = 2 if accelerated else 3
        message = "Physical emergency work secures the marked position; existing damage elsewhere remains."
        if accelerated:
            message += " The fitted station removes one exposed action."
    else:
        return False, "This is not the selected physical work position."
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
    message = f"{text} {'Death' if permanent else 'Injury'} during a declared deck crisis; the physical load remains at {site.x},{site.y},z{site.z:+d}. Regional quests were not reset."
    if state.vessel_changes.get("deck_crisis"):
        message += " " + abandon_deck(state)
    state.remember(message)
    return message


def validate_ship(state: GameState) -> None:
    from .vessel import VESSEL_LEVELS

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
