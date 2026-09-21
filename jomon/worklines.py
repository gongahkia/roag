"""Four second regional undertakings, authored as direct material decisions."""

from __future__ import annotations

from .catalog import CatalogError, HISTORY_SECTIONS, load_catalog
from .state import GameState, Position, QuestProgress
from .workline_presentation import (
    workline_branch_label, workline_evidence_name, workline_format,
    workline_reward_name, workline_text, workline_title,
)

_WORKLINES = load_catalog("history.json", HISTORY_SECTIONS)["undertakings"]
if (not isinstance(_WORKLINES, dict) or len(_WORKLINES) != 4
        or any(not isinstance(region, str) or not isinstance(row, list) or len(row) != 8
               or any(not isinstance(value, str) or not value for value in row)
               for region, row in _WORKLINES.items())):
    raise CatalogError("history.json has invalid undertakings")
WORKLINES = {region: tuple(row) for region, row in _WORKLINES.items()}
if len({row[2] for row in WORKLINES.values()}) != len(WORKLINES):
    raise CatalogError("history.json has repeated undertaking evidence")

EVIDENCE = {row[2]: region for region, row in WORKLINES.items()}


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
        return False, "workline.requirement.crowded"
    if region_id == "hearthford":
        return (_quantity(state, "commodity:timber") >= 1, "workline.requirement.hearthford.timber")
    if region_id == "greywash":
        return (state.lamp_oil >= (2 if branch == "l" else 1), "workline.requirement.greywash.oil")
    if region_id == "greenwold":
        if branch == "c":
            return _cutting(state), "workline.requirement.greenwold.cutting"
        from .materials import fields, key
        site = field_site(state)
        water = fields(state).get(key(site))
        return bool((water and water.water) or _quantity(state, "consumable:sealed brine pot") or state.region.changes.get("environment_control_used")), "workline.requirement.greenwold.brine"
    if branch == "b":
        return _quantity(state, "commodity:ironwork") >= 1, "workline.requirement.whitecairn.ironwork"
    return _cutting(state), "workline.requirement.whitecairn.release"


def options(state: GameState):
    if state.active_region_id not in WORKLINES:
        return ()
    quest = state.worklines[state.active_region_id]
    if quest.stage == 4:
        return ()
    if quest.stage == 0:
        row = WORKLINES[state.active_region_id]
        near = at_witness(state)
        return ((row[4], workline_branch_label(state.active_region_id, row[4]), "commitment", near, workline_text("workline.option.near_witness")),
                (row[6], workline_branch_label(state.active_region_id, row[6]), "danger", near, workline_text("workline.option.near_witness")))
    near_survey = _near(state, survey_site(state))
    # Copies are paid testimony, not regenerated unique equipment. Still-living
    # physical copies must be recovered instead of being duplicated.
    kind = "evidence:" + WORKLINES[state.active_region_id][2]
    copies = [i for i in state.items if i.kind == kind]
    recoverable = any(i.location not in {"lost", "destroyed"} for i in copies)
    can_copy = len(copies) < 4 and not recoverable and (not copies or state.trade_credit >= 1)
    rows = []
    if quest.stage in {1, 2} and not recoverable:
        rows.append(("e", workline_format("workline.option.copy.label", replacement=workline_text("workline.option.copy.replacement") if copies else ""), "ordinary", near_survey and can_copy,
                     workline_text("workline.option.copy.requirement")))
    if quest.stage == 2:
        material, reason = _work_requirement(state)
        rows.append(("f", workline_text("workline.option.field.label"), "commitment", _near(state, field_site(state)) and material and carried_evidence(state) is not None,
                     workline_format("workline.option.field.requirement", requirement=workline_text(reason))))
    if quest.stage == 3:
        rows.extend((("p", workline_text("workline.option.settle_public.label"), "commitment", at_witness(state), workline_text("workline.option.settle.requirement")),
                     ("j", workline_text("workline.option.settle_private.label"), "danger", at_witness(state), workline_text("workline.option.settle.requirement"))))
    if quest.stage in {1, 2}:
        rows.append(("a", workline_text("workline.option.abandon.label"), "refusal", at_witness(state), workline_text("workline.option.abandon.requirement")))
    return tuple(rows)


def lines(state: GameState):
    region = state.active_region_id
    if region not in WORKLINES:
        return [workline_text("workline.line.none")]
    row, quest = WORKLINES[region], state.worklines[region]
    witness = state.contacts[region][1]
    survey, site = survey_site(state), field_site(state)
    evidence = workline_evidence_name(region)
    result = [workline_title(region), workline_format("workline.line.witness", witness=witness.name, status=quest.status)]
    if quest.stage == 2:
        result += [workline_format("workline.line.field", x=site.x, y=site.y, z=site.z, requirement=workline_text(_work_requirement(state)[1])),
                   workline_format("workline.line.carry", evidence=evidence)]
    elif quest.stage == 1:
        result += [workline_format("workline.line.survey", evidence=evidence, x=survey.x, y=survey.y, z=survey.z),
                   workline_text("workline.line.survey_help")]
    elif quest.stage == 3:
        result.append(workline_text("workline.line.resolution"))
    result.append(workline_text("workline.line.general"))
    if quest.stage == 0:
        result.append(workline_text("workline.line.available"))
    if 0 < quest.stage < 3:
        result += [workline_format("workline.line.chosen", branch=workline_branch_label(region, quest.branch)),
                   workline_text("workline.line.evidence")]
    if quest.consequence:
        result.append(workline_format("workline.line.fact", consequence=quest.consequence))
    if region == "greywash" and state.region.changes.get("public_dune_light"):
        result.append(workline_format("workline.line.beacon", status="lit" if beacon_active(state) else "dark", account=state.region.changes.get("beacon_account", "")))
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
    actor.goal = workline_text("workline.guard.goal")
    actor.goal_reason = workline_text("workline.guard.reason")
    state.region.changes["undertaking_guard"] = actor.id
    if state.active_region_id == "whitecairn":
        actor.duty, actor.supplies = "cut support", 2
    state.add_message(workline_format("workline.guard.notice", actor=actor.name), priority=3)


def _survey(state):
    from .inventory import auto_place, create_item
    row = WORKLINES[state.active_region_id]
    copies = [i for i in state.items if i.kind == "evidence:" + row[2]]
    if copies:
        state.trade_credit -= 1
    item = create_item(state, "evidence:" + row[2], workline_format("workline.survey.item", title=workline_title(state.active_region_id), source=workline_text("workline.survey.replacement") if copies else workline_text("workline.survey.original")), location="ground")
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
        state.add_message(workline_text("workline.survey.whitecairn"), priority=3)
    return workline_format("workline.survey.result", evidence=workline_evidence_name(state.active_region_id), copy_state=workline_text("workline.survey.packed") if fitted else workline_text("workline.survey.ground"))


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
            text = workline_text("workline.result.hearthford.h")
        else:
            for dx in (-1, 0, 1):
                cell = ensure_cell(state, Position(site.x + dx, site.y, site.z))
                if cell:
                    cell.material, cell.support, cell.collapse_due = "timber", 3, 0
            state.market["grain"].stock = min(9, state.market["grain"].stock + 2)
            state.region.changes["loft_dry_stores"] = True
            text = workline_text("workline.result.hearthford.s")
    elif region == "greywash":
        state.lamp_oil -= 2 if branch == "l" else 1
        if branch == "l":
            from .calendar import ACTIONS_PER_DAY
            state.region.changes["public_dune_light"] = True
            state.region.changes["public_dune_light_until"] = state.world_time + ACTIONS_PER_DAY * 2
            text = workline_text("workline.result.greywash.l")
        else:
            for dx in (-2, -1, 0):
                cell = ensure_cell(state, Position(site.x + dx, site.y, site.z))
                if cell:
                    cell.material, cell.smoke, cell.fuel = "charcoal", 4, 3
            state.region.changes["screened_wreck_signal"] = True
            state.trade_credit += 2
            text = workline_text("workline.result.greywash.q")
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
            text = workline_text("workline.result.greenwold.c")
        else:
            if _quantity(state, "consumable:sealed brine pot"):
                consume_carried(state, "consumable:sealed brine pot")
            for dx in (-1, 0, 1):
                cell = ensure_cell(state, Position(site.x + dx, site.y, site.z))
                if cell:
                    cell.water, cell.fire, cell.smoke = 2, 0, 0
            state.region.changes["root_reserve_damped"] = True
            text = workline_text("workline.result.greenwold.w")
    else:
        if branch == "b":
            consume_carried(state, "commodity:ironwork")
            for dx in (-1, 0, 1):
                cell = ensure_cell(state, Position(site.x + dx, site.y, site.z))
                if cell:
                    cell.support, cell.collapse_due = 3, 0
            state.region.changes["counterweight_clamped"] = True
            text = workline_text("workline.result.whitecairn.b")
        else:
            point = Position(site.x - 1, site.y, site.z)
            cell = ensure_cell(state, point)
            if cell:
                cell.support, cell.collapse_due = 0, state.world_time + 3
            state.region.changes["counterweight_released"] = True
            state.market["lime"].stock = max(0, state.market["lime"].stock - 2)
            text = workline_text("workline.result.whitecairn.r")
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
        quest.consequence = workline_text("workline.settle.public")
        guard_id = state.region.changes.get("undertaking_guard")
        actor = next((a for a in state.combatants if a.id == guard_id and a.status in {"watching", "engaged"}), None)
        if actor:
            actor.status, actor.duty, actor.patrol = "negotiated", "", []
            actor.intent = workline_text("workline.settle.actor_intent")
            from .inventory import release_enemy_possession
            release_enemy_possession(state, actor)
    else:
        state.trade_credit += 3
        account.obligation = min(9, account.obligation + 2)
        state.market[account.production].demand = min(9, state.market[account.production].demand + 1)
        quest.consequence = workline_text("workline.settle.private")
    reward = create_item(state, "passive:" + row[3], workline_format("workline.settle.reward", title=workline_title(state.active_region_id)), location="ground")
    reward.region_id, reward.ground_position = state.active_region_id, state.position
    packed = state.auto_place_enabled and auto_place(state, reward.id, "pack", owner_id=state.active_courier_id)
    if packed:
        from .inventory import record_acquisition
        record_acquisition(state, reward)
    quest.stage, quest.status = 4, "completed"
    return workline_format(
        "workline.settle.result", consequence=quest.consequence,
        reward=workline_reward_name(state.active_region_id),
        location=workline_text("workline.settle.packed") if packed else workline_text("workline.settle.ground"),
        optional=workline_text("workline.settle.optional") if quest.optional_done else "",
    )


def resolve(state: GameState, choice: str):
    from .actions import _plain, _time_result
    row = next((row for row in options(state) if row[0] == choice), None)
    if row is None or not row[3]:
        return _plain(state, row[4] if row else workline_text("workline.resolve.unavailable"))
    quest = state.worklines[state.active_region_id]
    if quest.stage == 0:
        quest.stage, quest.status, quest.branch = 1, "active", choice
        from .quests import mark_treasure
        definition = WORKLINES[state.active_region_id]
        mark_treasure(state, state.active_region_id, definition[1], workline_format("workline.open.mark", title=workline_title(state.active_region_id), evidence=workline_evidence_name(state.active_region_id)))
        _assign_guard(state, field_site(state))
        message = workline_format("workline.open.message", title=workline_title(state.active_region_id), witness=state.contacts[state.active_region_id][1].name)
    elif choice == "e":
        message = _survey(state)
    elif choice == "f":
        message = _field_work(state)
    elif choice == "a":
        quest.stage, quest.status = 4, "refused"
        from .regional_history import account_for
        account = account_for(state)
        account.trust = max(-3, account.trust - 1)
        quest.consequence = workline_text("workline.abandon")
        message = quest.consequence
    else:
        message = _settle(state, choice)
    quest.decisions.append(f"stage-{quest.stage}:{choice}")
    _memory(state, workline_format("workline.memory", courier=state.courier.name, message=message))
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
    region.changes["beacon_account"] = workline_format(
        "workline.beacon.account", supplied=supplied,
        status=workline_text("workline.beacon.lit") if lit else workline_text("workline.beacon.dark"),
    )
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
