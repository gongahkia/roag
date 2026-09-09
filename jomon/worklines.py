"""Four second regional undertakings, authored as direct material decisions."""

from __future__ import annotations

from .state import GameState, Position, QuestProgress


WORKLINES = {
    "hearthford": ("The Houses Above the Race", "watch", "flood-height lath", "roof nail",
                   "h", "Shore the flooded watch-house approach", "s", "Raise dry stores in the mill loft"),
    "greywash": ("The Light without a Toll", "greywash-cave", "salt lens pattern", "sighting knot",
                 "l", "Light a public warning above the dunes", "q", "Keep a screened signal for accountable salvage"),
    "greenwold": ("Root and Ash", "greenwold-root", "coppice root tally", "ember cloth",
                  "c", "Cut a narrow coppice firebreak", "w", "Dampen the root reserve without cutting trees"),
    "whitecairn": ("The Load Below the Bell", "whitecairn-bridge", "counterweight rubbing", "quarry brace",
                   "b", "Brace the lower counterweight and keep the haul", "r", "Release the burden and accept a fallen face"),
}

EVIDENCE = {row[2]: (row[0], region) for region, row in WORKLINES.items()}


def initialise(state: GameState) -> None:
    for region in WORKLINES:
        state.worklines.setdefault(region, QuestProgress())


def survey_site(state: GameState, region_id: str | None = None) -> Position:
    region_id = region_id or state.active_region_id
    return next(c.position for c in state.regions[region_id].containers if c.id == WORKLINES[region_id][1])


def field_site(state: GameState, region_id: str | None = None) -> Position:
    region_id = region_id or state.active_region_id
    region = state.regions[region_id]
    branch = state.worklines[region_id].branch
    if region_id == "hearthford":
        return region.landmarks["watchtower" if branch == "h" else "objective"]
    if region_id == "greywash":
        return region.landmarks["elevated" if branch == "l" else "wreck"]
    if region_id == "greenwold":
        return region.landmarks["burn_walk"] if branch == "c" else region.landmarks["objective"]
    return region.landmarks["objective"]


def _near(state, point):
    from .world import distance, courier_sees
    return state.location == "region" and state.position.z == point.z and distance(state.position, point) <= 1 and courier_sees(state, point)


def at_witness(state):
    contact = state.contacts[state.active_region_id][1]
    schedule = state.actor_schedules.get(contact.id)
    point = schedule.position if schedule and schedule.area == f"region:{state.active_region_id}" else contact.position
    return _near(state, point)


def carried_evidence(state):
    if state.active_region_id not in WORKLINES:
        return None
    kind = "evidence:" + WORKLINES[state.active_region_id][2]
    return next((i for i in state.items if i.kind == kind and i.location == "pack" and i.owner_id == state.active_courier_id), None)


def evidence_leverage(state, actor):
    return bool(carried_evidence(state) and actor.id == state.region.changes.get("undertaking_guard"))


def _quantity(state, kind):
    return sum(i.quantity for i in state.items if i.kind == kind and i.location == "pack" and i.owner_id == state.active_courier_id)


def _cutting(state):
    return state.weapon in {"billhook", "hand axe", "spade", "war hammer"} or state.gear == "repair tools" or bool(state.courier and state.courier.technique == "lever craft")


def _work_requirement(state):
    region_id, branch = state.active_region_id, state.worklines[state.active_region_id].branch
    from .materials import MAX_CELLS, fields, key
    site = field_site(state)
    possible = {key(Position(site.x + dx, site.y + dy, site.z)) for dx in range(-6, 7) for dy in (0, 1)}
    if len(fields(state)) + len(possible - fields(state).keys()) > MAX_CELLS:
        return False, "the bounded material field is saturated; existing changes must be resolved before this work"
    if region_id == "hearthford":
        return (_quantity(state, "commodity:timber") >= 1, "one physical timber lot for the framing")
    if region_id == "greywash":
        return (state.lamp_oil >= (2 if branch == "l" else 1), "two lamp-oil measures for a public light; one for a screened signal")
    if region_id == "greenwold":
        if branch == "c":
            return _cutting(state), "a cutting weapon, repair tools or Lever Craft"
        from .materials import fields, key
        site = field_site(state)
        water = fields(state).get(key(site))
        return bool((water and water.water) or _quantity(state, "consumable:sealed brine pot") or state.region.changes.get("environment_control_used")), "bring a brine pot, expose actual water here, or operate the burn water control"
    if branch == "b":
        return _quantity(state, "commodity:ironwork") >= 1, "one physical ironwork lot for the lower clamp"
    return _cutting(state), "a cutting or levering implement; the warned neighbouring support will fall"


def options(state: GameState):
    if state.active_region_id not in WORKLINES:
        return ()
    quest = state.worklines[state.active_region_id]
    if quest.stage == 4:
        return ()
    if quest.stage == 0:
        row = WORKLINES[state.active_region_id]
        near = at_witness(state)
        return ((row[4], row[5], "commitment", near, "speak beside the local secondary contact"),
                (row[6], row[7], "danger", near, "speak beside the local secondary contact"))
    near_survey = _near(state, survey_site(state))
    # Copies are paid testimony, not regenerated unique equipment. Still-living
    # physical copies must be recovered instead of being duplicated.
    kind = "evidence:" + WORKLINES[state.active_region_id][2]
    copies = [i for i in state.items if i.kind == kind]
    recoverable = any(i.location not in {"lost", "destroyed"} for i in copies)
    can_copy = len(copies) < 4 and not recoverable and (not copies or state.trade_credit >= 1)
    rows = []
    if quest.stage in {1, 2} and not recoverable:
        rows.append(("e", "Copy the surviving site marks" + (" — 1 credit for replacement" if copies else ""), "ordinary", near_survey and can_copy,
                     "survey the coffer; replacement after loss costs one credit (at most three copies)"))
    if quest.stage == 2:
        material, requirement = _work_requirement(state)
        rows.append(("f", "Perform the disclosed field work", "commitment", _near(state, field_site(state)) and material and carried_evidence(state) is not None,
                     f"carry the site evidence to the field position; {requirement}"))
    if quest.stage == 3:
        rows.extend((("p", "Give the working result to the local commons", "commitment", at_witness(state), "return to the named secondary witness"),
                     ("j", "Hold the result under Jomon's paid obligation", "danger", at_witness(state), "return to the named secondary witness")))
    if quest.stage in {1, 2}:
        rows.append(("a", "Abandon this undertaking and record its failure", "refusal", at_witness(state), "return to the witness to accept the consequence"))
    return tuple(rows)


def lines(state: GameState):
    region = state.active_region_id
    if region not in WORKLINES:
        return ["No second undertaking is offered here."]
    row, quest = WORKLINES[region], state.worklines[region]
    witness = state.contacts[region][1]
    survey, site = survey_site(state), field_site(state)
    result = [row[0], f"Witness {witness.name}. Stage {quest.stage}/4; {quest.status}."]
    if quest.stage == 2:
        result += [f"FIELD: {site.x},{site.y} z{site.z:+d}; " + _work_requirement(state)[1] + ".",
                   f"Carry {row[2]}; an existing copy must be recovered, not duplicated."]
    elif quest.stage == 1:
        result += [f"SURVEY: {row[2]}, {survey.x},{survey.y} z{survey.z:+d}.",
                   "F then W copies the outside marks. Opening the coffer too is optional: one evidence credit at settlement."]
    elif quest.stage == 3:
        result.append("Physical work is recorded. Return to the named witness to decide its ownership.")
    result.append("F then W: local work; Z: full ledger. Inspection is free; confirmed work advances time.")
    if quest.stage == 0:
        result.append("Both approaches remain optional. Escape declines for now without changing the account.")
    if 0 < quest.stage < 3:
        result += [f"Chosen approach: {row[5] if quest.branch == row[4] else row[7]}.",
                   "Evidence is a physical 1x2 pack item: it may be dropped, stolen or left on a defeated courier."]
    if quest.consequence:
        result.append("FACT: " + quest.consequence)
    if region == "greywash" and state.region.changes.get("public_dune_light"):
        result.append("FACT: Dune light " + ("lit" if beacon_active(state) else "dark") + "; local charcoal maintains it after two initial days. " + state.region.changes.get("beacon_account", ""))
    return result


def _memory(state, text):
    contact = state.contacts[state.active_region_id][1]
    contact.memories.append(text)
    del contact.memories[:-8]
    state.remember(text)


def _assign_guard(state, site):
    actors = [a for a in state.combatants if not a.elite and a.profile not in {"animal", "machinery"} and a.status in {"watching", "engaged"}
              and a.id != state.region.changes.get("quest_guard_id")]
    if not actors:
        return
    from .world import distance
    actor = min(actors, key=lambda a: (distance(a.position, site), a.id))
    actor.home_position, actor.objective_position = site, site
    actor.patrol, actor.patrol_index = [actor.position, site], 0
    actor.goal = "hold the disputed worksite"
    actor.goal_reason = "a witnessed undertaking would change this worker's material claim"
    state.region.changes["undertaking_guard"] = actor.id
    if state.active_region_id == "whitecairn":
        actor.duty, actor.supplies = "cut support", 2
    state.add_message(f"{actor.name} is assigned to the worksite, not spawned there. Watch its approach, evade it or show the survey with V.", priority=3)


def _survey(state):
    from .inventory import auto_place, create_item
    row = WORKLINES[state.active_region_id]
    copies = [i for i in state.items if i.kind == "evidence:" + row[2]]
    if copies:
        state.trade_credit -= 1
    item = create_item(state, "evidence:" + row[2], f"{row[0]}: {'paid replacement copied from' if copies else 'copied from'} surviving site marks", location="ground")
    item.region_id, item.ground_position = state.active_region_id, survey_site(state)
    fitted = state.auto_place_enabled and auto_place(state, item.id, "pack", owner_id=state.active_courier_id)
    quest = state.worklines[state.active_region_id]
    quest.optional_done = next(c.opened for c in state.region.containers if c.id == row[1])
    quest.stage, quest.status = max(2, quest.stage), "active" if quest.stage < 3 else "resolution"
    if state.active_region_id == "whitecairn" and not copies:
        from .materials import ensure_cell
        site = field_site(state)
        for dx in (-1, 0, 1):
            cell = ensure_cell(state, Position(site.x + dx, site.y, site.z))
            if cell:
                cell.support = min(1, cell.support)
        state.add_message("The rubbing identifies cracked lower supports: F inspection shows their actual weakened condition below the bell.", priority=3)
    return f"The {row[2]} is {'packed' if fitted else 'left physically at the marked survey coffer; use I to pack it'}. Its testimony supports this undertaking, not every hostile claim."


def _field_work(state):
    from .inventory import consume_carried, sync_legacy_load
    from .materials import ensure_cell, key
    from .world import base_tile
    region, branch = state.active_region_id, state.worklines[state.active_region_id].branch
    site = field_site(state)
    if region == "hearthford":
        consume_carried(state, "commodity:timber")
        if branch == "h":
            # Repair the existing approach, not a new sealed room.
            for dx, dy in ((-2, 0), (-1, 0), (0, 1), (1, 1), (2, 1)):
                point = Position(site.x + dx, site.y + dy, site.z)
                if base_tile(state, point) in {"m", ",", ";", ".", "=", "%"}:
                    state.region.tile_changes[key(point)] = "="
                    cell = ensure_cell(state, point)
                    if cell:
                        cell.material, cell.water, cell.support, cell.fuel = "timber", 0, 3, 3
            state.region.changes["raised_watch_approach"] = True
            text = "Timber raises the watch-house approach; flooded ground becomes a firm shared route."
        else:
            for dx in (-1, 0, 1):
                cell = ensure_cell(state, Position(site.x + dx, site.y, site.z))
                if cell:
                    cell.material, cell.support, cell.collapse_due = "timber", 3, 0
            state.market["grain"].stock = min(9, state.market["grain"].stock + 2)
            state.region.changes["loft_dry_stores"] = True
            text = "The loft receives new supports and two counted grain lots; storage survives where the lower houses do not improve."
    elif region == "greywash":
        state.lamp_oil -= 2 if branch == "l" else 1
        if branch == "l":
            from .calendar import ACTIONS_PER_DAY
            state.region.changes["public_dune_light"] = True
            state.region.changes["public_dune_light_until"] = state.world_time + ACTIONS_PER_DAY * 2
            text = "The dune light gives two more sight paces within twenty paces of its mast for two days. After that, the local workers consume one charcoal lot per day to keep it lit; shortages darken it."
        else:
            for dx in (-2, -1, 0):
                cell = ensure_cell(state, Position(site.x + dx, site.y, site.z))
                if cell:
                    cell.material, cell.smoke, cell.fuel = "charcoal", 4, 3
            state.region.changes["screened_wreck_signal"] = True
            state.trade_credit += 2
            text = "A screened smoke signal obscures the wreck approach and pays two salvage credits; the broad public sightline is not improved."
    elif region == "greenwold":
        if branch == "c":
            for dx in range(-6, 7):
                point = Position(site.x + dx, site.y, 0)
                if base_tile(state, point) in {"T", "t", ";", ".", "="}:
                    state.region.tile_changes[key(point)] = "."
                    cell = ensure_cell(state, point)
                    if cell:
                        cell.material, cell.fuel, cell.fire = "soil", 0, 0
            state.region.changes["coppice_firebreak"] = True
            text = "The narrow cut opens a ground-level firebreak and longer sightline; concealment and standing timber are sacrificed."
        else:
            if _quantity(state, "consumable:sealed brine pot"):
                consume_carried(state, "consumable:sealed brine pot")
            for dx in (-1, 0, 1):
                cell = ensure_cell(state, Position(site.x + dx, site.y, site.z))
                if cell:
                    cell.water, cell.fire, cell.smoke = 2, 0, 0
            state.region.changes["root_reserve_damped"] = True
            text = "Water is held in the root reserve; local smoke clears while woodland concealment stays intact."
    else:
        if branch == "b":
            consume_carried(state, "commodity:ironwork")
            for dx in (-1, 0, 1):
                cell = ensure_cell(state, Position(site.x + dx, site.y, site.z))
                if cell:
                    cell.support, cell.collapse_due = 3, 0
            state.region.changes["counterweight_clamped"] = True
            text = "A physical iron clamp steadies the lower support and keeps the haul's material claim alive."
        else:
            point = Position(site.x - 1, site.y, site.z)
            cell = ensure_cell(state, point)
            if cell:
                cell.support, cell.collapse_due = 0, state.world_time + 3
            state.region.changes["counterweight_released"] = True
            state.market["lime"].stock = max(0, state.market["lime"].stock - 2)
            text = "The counterweight is released. Its neighbouring support will fall in three actions; leave the marked cell. Two lime lots are forfeited."
    sync_legacy_load(state)
    state.worklines[region].stage, state.worklines[region].status = 3, "resolution"
    return text


def _settle(state, choice):
    from .inventory import auto_place, create_item
    from .regional_history import account_for
    quest, row = state.worklines[state.active_region_id], WORKLINES[state.active_region_id]
    public = choice == "p"
    account = account_for(state)
    if quest.optional_done:
        state.trade_credit += 1
    if public:
        account.trust = min(3, account.trust + 1)
        state.contacts[state.active_region_id][1].disposition = min(3, state.contacts[state.active_region_id][1].disposition + 1)
        for edge in state.route_edges:
            if state.active_region_id in {edge.first, edge.second}:
                edge.cargo_risk = max(0, edge.cargo_risk - 1)
        quest.consequence = "Local commons: the work remains in place, local trust rises and connected cargo routes ease; Jomon takes no fee."
        guard_id = state.region.changes.get("undertaking_guard")
        actor = next((a for a in state.combatants if a.id == guard_id and a.status in {"watching", "engaged"}), None)
        if actor:
            actor.status, actor.duty, actor.patrol = "negotiated", "", []
            actor.intent = "accepts the named witness's public working settlement"
            from .inventory import release_enemy_possession
            release_enemy_possession(state, actor)
    else:
        state.trade_credit += 3
        account.obligation = min(9, account.obligation + 2)
        state.market[account.production].demand = min(9, state.market[account.production].demand + 1)
        quest.consequence = "Household surety: three credits now, two recorded obligations and higher local demand; the physical work remains, but route risk does not ease."
    reward = create_item(state, "passive:" + row[3], f"{row[0]}: one witnessed settlement reward", location="ground")
    reward.region_id, reward.ground_position = state.active_region_id, state.position
    packed = state.auto_place_enabled and auto_place(state, reward.id, "pack", owner_id=state.active_courier_id)
    if packed:
        from .inventory import record_acquisition
        record_acquisition(state, reward)
    quest.stage, quest.status = 4, "completed"
    return quest.consequence + f" {row[3]} is {'packed' if packed else 'left visibly at the witness'} once." + (" Opening the optional coffer earned one evidence credit." if quest.optional_done else "")


def resolve(state: GameState, choice: str):
    from .actions import _plain, _time_result
    row = next((row for row in options(state) if row[0] == choice), None)
    if row is None or not row[3]:
        return _plain(state, row[4] if row else "That undertaking action is not available.")
    quest = state.worklines[state.active_region_id]
    if quest.stage == 0:
        quest.stage, quest.status, quest.branch = 1, "active", choice
        from .quests import mark_treasure
        definition = WORKLINES[state.active_region_id]
        mark_treasure(state, state.active_region_id, definition[1], f"{definition[0]}: the {definition[2]} can be copied beside this coffer.")
        _assign_guard(state, field_site(state))
        message = f"{definition[0]} opens under {state.contacts[state.active_region_id][1].name}'s witness. Survey the marked coffer; F then W exposes the work and costs."
    elif choice == "e":
        message = _survey(state)
    elif choice == "f":
        message = _field_work(state)
    elif choice == "a":
        quest.stage, quest.status = 4, "refused"
        from .regional_history import account_for
        account = account_for(state)
        account.trust = max(-3, account.trust - 1)
        quest.consequence = "The undertaking is abandoned; the witness records a lost working promise, not a silently unwinnable expedition."
        message = quest.consequence
    else:
        message = _settle(state, choice)
    quest.decisions.append(f"stage-{quest.stage}:{choice}")
    _memory(state, f"{state.courier.name}: {message}")
    return _time_result(state, message, priority=3)


def validate(state):
    if set(state.worklines) != set(WORKLINES):
        raise ValueError("missing or unknown regional undertaking")
    for region, quest in state.worklines.items():
        row = WORKLINES[region]
        if type(quest.stage) is not int or not 0 <= quest.stage <= 4 or quest.status not in {"available", "active", "resolution", "completed", "refused"}:
            raise ValueError("invalid undertaking stage or status")
        if quest.branch not in {"", row[4], row[6]} or len(quest.decisions) > 24:
            raise ValueError("invalid undertaking decisions")
        statuses = {0: {"available"}, 1: {"active"}, 2: {"active"}, 3: {"resolution"}, 4: {"completed", "refused"}}
        if quest.status not in statuses[quest.stage] or (quest.stage == 0) != (quest.branch == ""):
            raise ValueError("inconsistent undertaking stage")


def beacon_active(state):
    region = state.regions.get("greywash")
    return bool(region and region.changes.get("public_dune_light") and state.world_time < region.changes.get("public_dune_light_until", 0))


def supply_beacon(state, region, market, elapsed):
    from .calendar import ACTIONS_PER_DAY
    if region.id != "greywash" or not region.changes.get("public_dune_light"):
        return
    if state.world_time < region.changes.get("public_dune_light_until", 0):
        return
    stock = market["charcoal"]
    supplied = min(stock.stock, elapsed)
    stock.stock -= supplied
    # Catch-up accounts consume recorded stock, without replaying distant tiles.
    lit = supplied == elapsed
    region.changes["public_dune_light_until"] = state.world_time + ACTIONS_PER_DAY if lit else state.world_time
    region.changes["beacon_account"] = f"Dune light: {supplied} charcoal lots consumed; {'lit until the next work day' if lit else 'dark until charcoal reaches the local market'}."
    if state.active_region_id == "greywash" and state.location == "region":
        state.add_message(region.changes["beacon_account"], priority=2)


def apply_local_work(state):
    """Existing continuing regional processes respect completed material work."""
    if state.location != "region":
        return
    from .materials import fields, key
    region = state.active_region_id
    if region == "greenwold" and state.region.changes.get("root_reserve_damped"):
        site = state.region.landmarks["objective"]
        for dx in (-1, 0, 1):
            coordinate = key(Position(site.x + dx, site.y, site.z))
            state.smoke.pop(coordinate, None)
            cell = fields(state).get(coordinate)
            if cell and cell.water:
                cell.smoke, cell.fire = 0, 0
