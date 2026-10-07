"""Sixteen local elite problems, including eight finite returning claimants."""

from __future__ import annotations

from .catalog import ACTOR_SECTIONS, CatalogError, load_catalog
from .action_presentation import action_format
from .ecology_presentation import ecology_format, ecology_text


def _set_intent(actor, intent_id: str, **values: object) -> str:
    actor.intent_id = intent_id
    actor.intent = ecology_format(intent_id, **values)
    return actor.intent
from .visuals import ENTITY_GLYPHS

_CATALOG = load_catalog("actors.json", ACTOR_SECTIONS)["FRONTIER_ELITES"]
if not isinstance(_CATALOG, dict) or set(_CATALOG) != {"rows", "aftermath", "named"}:
    raise CatalogError("FRONTIER_ELITES has invalid sections")
if (not all(isinstance(_CATALOG[key], list) for key in ("rows", "aftermath", "named"))
        or any(not isinstance(row, list) for row in _CATALOG["rows"])):
    raise CatalogError("FRONTIER_ELITES must use lists")
ELITE_ROWS = tuple(tuple(row) for row in _CATALOG["rows"])
AFTERMATH_ELITES = frozenset(_CATALOG["aftermath"])
NAMED_RIVALS = frozenset(_CATALOG["named"])
if (len(ELITE_ROWS) != 16 or any(len(row) != 9 or any(not isinstance(value, str) or not value for value in row)
                                  for row in ELITE_ROWS)
        or len({row[0] for row in ELITE_ROWS}) != len(ELITE_ROWS)
        or len(AFTERMATH_ELITES) != 8 or len(NAMED_RIVALS) != 8
        or not AFTERMATH_ELITES <= {row[0] for row in ELITE_ROWS}
        or not NAMED_RIVALS <= {row[0] for row in ELITE_ROWS}):
    raise CatalogError("FRONTIER_ELITES has invalid rows or identities")

ELITE_DEFINITIONS = {
    identity: {
        "region": region, "name": name, "profile": profile, "role": "elite",
        "goal": goal, "vision": 11, "hearing": 9, "range": 8,
        "capability": capability, "counterplay": counterplay, "terrain": region,
        "budget": 6, "elite": True, "morale": 5, "glyph": ENTITY_GLYPHS["elite"],
        "supplies": 3, "mode": mode, "reward": reward,
        "ranged_kind": "sling", "named": identity in NAMED_RIVALS,
        "aftermath": identity in AFTERMATH_ELITES,
    }
    for identity, region, name, profile, mode, goal, capability, counterplay, reward in ELITE_ROWS
}


def definition(actor):
    if not actor.id.startswith("frontier-elite:"):
        return None
    identity = actor.archetype_id or actor.id.removeprefix("frontier-elite:")
    return ELITE_DEFINITIONS.get(identity)


def install_elite(seed, region, actors):
    from .encounters import threat_from_archetype
    from .regions import region_reachable
    from .state import Position, stage_rng

    choices = sorted(
        key for key, data in ELITE_DEFINITIONS.items()
        if data["region"] == region.id and not data.get("aftermath")
    )
    identity = stage_rng(seed, f"{region.id}:working-elite").choice(choices)
    data = ELITE_DEFINITIONS[identity]
    origin = region.landmarks["works" if data["mode"] in {"convoy", "shutters", "haul"} else "far_bank"]
    if data["mode"] == "sever":
        origin = region.landmarks["works"]
    occupied = {a.position for a in actors} | set(region.landmarks.values()) | {box.position for box in region.containers}
    occupied |= {point for link in region.vertical_links for point in (link.first, link.second)}
    point = min(region_reachable(region) - occupied,
                key=lambda p: (abs(p.z - origin.z) * 100 + abs(p.x - origin.x - 3) + abs(p.y - origin.y - 2), p.z, p.y, p.x))
    actor = threat_from_archetype(identity, point, encounter_id="frontier-elite", group=f"{region.id}-working-claim")
    actor.status, actor.objective_position = "dormant", Position(point.x + 1, point.y, point.z)
    # A material linkage gives all machines a physical, inspectable disable path.
    from .state import MaterialCell
    region.materials[f"{point.x},{point.y},{point.z}"] = MaterialCell(material="timber", support=2, fuel=8 if data["mode"] == "smoulder" else 0)
    if data["mode"] == "convoy":
        helpers = sorted((a for a in actors if a.profile != "animal" and a.ecology in {"worker", "warden"}), key=lambda a: abs(a.position.x - point.x) + abs(a.position.y - point.y))[:2]
        for helper in helpers:
            helper.group = actor.group
            helper.allegiance = f"{region.id}:warden"
            helper.ecology, helper.duty = "warden", "escort"
    actors.append(actor)
    region.changes["working_elite"] = actor.id
    return actor


def install_aftermath_elite(state):
    """Materialise one finite consequence encounter after a completed ending."""
    if state.location != "region" or not state.region.changes.get("aftermath_configuration"):
        return None
    region_id = state.active_region_id
    identity = next(
        key for key, data in ELITE_DEFINITIONS.items()
        if data["region"] == region_id and data.get("aftermath")
    )
    marker = "aftermath_elite_installed"
    actor_id = f"frontier-elite:{identity}"
    if state.region.changes.get(marker):
        return next((actor for actor in state.threats if actor.id == actor_id), None)

    from .encounters import threat_from_archetype
    from .enemy_equipment import issue_enemy_equipment
    from .regions import region_reachable
    from .state import MaterialCell, Position

    raw_site = str(state.region.changes.get(
        "aftermath_site:1", ""
    )).split(",")
    anchor = (
        Position(*(int(value) for value in raw_site))
        if len(raw_site) == 3 and all(value.lstrip("-").isdigit() for value in raw_site)
        else state.region.landmarks["works"]
    )
    occupied = {
        actor.position for actor in state.threats
        if actor.status in {"dormant", "watching", "engaged"}
    } | set(state.region.landmarks.values()) | {
        container.position for container in state.region.containers
    }
    occupied |= {
        point for link in state.region.vertical_links
        for point in (link.first, link.second)
    }
    occupied.add(state.position)
    candidates = region_reachable(state.region) - occupied
    if not candidates:
        return None
    point = min(
        candidates,
        key=lambda candidate: (
            abs(candidate.z - anchor.z) * 100
            + abs(candidate.x - anchor.x)
            + abs(candidate.y - anchor.y),
            candidate.z, candidate.y, candidate.x,
        ),
    )
    actor = threat_from_archetype(
        identity, point, encounter_id="frontier-elite",
        group=f"{region_id}-aftermath-claim",
    )
    actor.status, actor.home_position = "dormant", point
    actor.objective_position = anchor
    state.threats.append(actor)
    state.region_threats[region_id] = state.threats
    linkage = state.region.materials.setdefault(
        f"{point.x},{point.y},{point.z}", MaterialCell(material="timber")
    )
    linkage.support, linkage.water = 2, 0
    state.region.changes[marker] = identity
    issue_enemy_equipment(state, actor, region_id)
    state.region.changes[f"enemy_kit:{actor.id}"] = 1
    state.remember(ecology_format("frontier.record.install", actor=actor.name, x=point.x, y=point.y, z=f"{point.z:+d}"))
    return actor


def _protected(state, point):
    return point in set(state.region.landmarks.values()) or any(point in (link.first, link.second) for link in state.region.vertical_links) or any(box.position == point for box in state.region.containers)


def _line(state, point):
    from .state import Position
    from .world import base_tile
    return [p for dx in (-1, 0, 1) if base_tile(state, p := Position(point.x + dx, point.y, point.z)) not in {"#", " "}]


def guard_interception(state, target, damage):
    """The same visible escort bodies pay the cost of protecting the foreman."""
    from .world import distance, line_of_sight
    from .enemy_equipment import harm_enemy

    data = definition(target)
    if not data or data["mode"] != "convoy":
        return damage, ""
    helper = next((a for a in state.combatants if a.id != target.id and a.group == target.group
                   and a.status in {"watching", "engaged"} and distance(a.position, target.position) <= 2
                   and line_of_sight(state, target.position, a.position)), None)
    if helper is None or damage <= 0:
        return damage, ""
    intercepted = min(2, damage)
    harm = harm_enemy(
        state, helper, intercepted, "convoy guard interception",
        damage_kind="blunt",
    )
    return damage - intercepted, ecology_format("frontier.result.guard_intercept", helper=helper.name, amount=harm.amount)


def elite_action(state, actor, guarded):
    """Return None for ordinary actors; no unseen courier coordinate is targeted."""
    data = definition(actor)
    if data is None:
        return None
    from .enemy_ai import higher_access_target, next_path_step, perceive
    from .materials import ensure_cell, fields, key
    from .state import Position
    from .world import distance, is_walkable, line_of_sight

    mode = data["mode"]
    visible, perceived, reason = perceive(state, actor)
    linkage = fields(state).get(key(actor.home_position))
    controlled = state.region.changes.get("environment_control_used")
    if controlled or (actor.profile == "machinery" and linkage and (linkage.support >= 3 or linkage.support == 0 or linkage.water)):
        actor.status = "disabled" if actor.profile == "machinery" else "negotiated"
        _set_intent(actor, "frontier.intent.control_removed")
        return ecology_format("frontier.result.stand_down", actor=actor.name)
    if actor.morale <= 0 or actor.health <= 2:
        actor.status = "retreated"
        _set_intent(actor, "frontier.intent.withdraw")
        return ecology_format("frontier.result.withdraw", actor=actor.name)
    if actor.reload_turns:
        actor.reload_turns -= 1
        if mode == "sever":
            higher = higher_access_target(state, actor)
            if higher:
                step = next_path_step(state, actor, higher, stop_distance=0, limit=350)
                if step != state.position:
                    actor.position = step
        _set_intent(actor, "frontier.intent.recover")
        return ecology_format("frontier.result.recover", actor=actor.name, intent=actor.intent)
    if actor.supplies <= 0:
        actor.status = "disabled" if actor.profile == "machinery" else "retreated"
        _set_intent(actor, "frontier.intent.depleted")
        return ecology_format("frontier.result.depleted", actor=actor.name)
    if actor.marked_position is None:
        if not visible:
            if perceived and actor.profile != "machinery":
                step = next_path_step(state, actor, perceived, stop_distance=3, limit=350)
                if step != state.position:
                    actor.position = step
            _set_intent(actor, "frontier.intent.hold", reason=reason)
            return ""
        if distance(actor.position, state.position) > 8:
            _set_intent(actor, "frontier.intent.wait")
            return ""
        if mode == "salvage":
            exposed = sorted(
                (
                    item for item in state.items
                    if item.location == "ground"
                    and item.region_id == state.spatial_id
                    and item.ground_position is not None
                    and distance(actor.position, item.ground_position) <= 8
                    and line_of_sight(state, actor.position, item.ground_position)
                ),
                key=lambda item: (
                    distance(actor.position, item.ground_position), item.id,
                ),
            )
            actor.marked_position = (
                exposed[0].ground_position if exposed else state.position
            )
        else:
            actor.marked_position = actor.home_position if mode == "smoulder" else state.position
        actor.reaction = mode
        _set_intent(actor, f"frontier.telegraph.{mode}", mode=mode, position=key(actor.marked_position))
        return ecology_format("frontier.result.prepare", actor=actor.name, intent=actor.intent)
    point, actor.marked_position, actor.reaction = actor.marked_position, None, ""
    if not line_of_sight(state, actor.position, point):
        _set_intent(actor, "frontier.intent.blocked")
        return ecology_format("frontier.result.blocked", actor=actor.name)
    actor.supplies -= 1
    actor.reload_turns = 2 if mode == "sever" else 1
    cell = ensure_cell(state, point)
    if cell is None:
        return ecology_format("frontier.result.no_footing", actor=actor.name)
    if mode == "surge":
        for target in _line(state, point):
            patch = ensure_cell(state, target)
            if patch:
                patch.water, patch.fluid = 3, "fresh"
        message = ecology_text("frontier.result.surge")
    elif mode == "smoulder":
        if cell.water:
            actor.status = "disabled"
            message = ecology_text("frontier.result.smoulder_quenched")
        else:
            cell.material, cell.fire, cell.fuel, cell.smoke = "charcoal", 1, 8, 4
            cell.support = max(0, cell.support - 1)
            if cell.support == 0:
                cell.collapse_due = state.world_time + 2
            message = ecology_text("frontier.result.smoulder")
    elif mode == "sever":
        if not _protected(state, point) and cell.support < 3:
            cell.support, cell.collapse_due = 0, state.world_time + 2
            message = ecology_text("frontier.result.sever_fall")
        elif not _protected(state, point):
            cell.support = 1
            message = ecology_text("frontier.result.sever_weaken")
        else:
            message = ecology_text("frontier.result.sever_protected")
    elif mode == "convoy":
        helpers = [a for a in state.combatants if a.id != actor.id and a.group == actor.group
                   and a.status in {"watching", "engaged"} and distance(a.position, actor.position) <= 8
                   and line_of_sight(state, actor.position, a.position)]
        for helper in helpers:
            helper.morale = min(4, helper.morale + 1)
            helper.home_position = actor.position
            helper.objective_position = actor.position
            helper.last_known_position = point
        actor.position = next_path_step(state, actor, point, stop_distance=2, limit=350)
        message = ecology_text("frontier.result.convoy")
    elif mode == "firing":
        count = 0
        for target in _line(state, point):
            patch = ensure_cell(state, target)
            if patch and not patch.water:
                patch.material, patch.coating = "charcoal", "resin"
                patch.fire, patch.fuel = 1, 5
                count += 1
        message = ecology_format("frontier.result.firing", count=count)
    elif mode == "shutters":
        for target in _line(state, point):
            if not _protected(state, target):
                coordinate = key(target)
                state.region.tile_changes[coordinate] = "." if state.region.tile_changes.get(coordinate) == "%" else "%"
        message = ecology_text("frontier.result.shutters")
        if state.position == point and not guarded:
            from .actions import apply_damage
            message += " " + apply_damage(state, 2, ecology_text("frontier.damage.shutters"))
    elif mode == "brine":
        frozen = cell.ice
        cell.ice, cell.water, cell.fluid, cell.coating = False, 3 if frozen else 1, "salt", "salt"
        message = ecology_text("frontier.result.brine_frozen" if frozen else "frontier.result.brine")
    elif mode == "backwash":
        for target in _line(state, point):
            patch = ensure_cell(state, target)
            if patch:
                patch.water, patch.fluid, patch.coating = 2, "fresh", "wet"
        if state.position == point and not guarded:
            step = Position(
                point.x + (actor.position.x < point.x) - (actor.position.x > point.x),
                point.y, point.z,
            )
            if step != actor.position and is_walkable(state, step):
                state.position = step
        message = ecology_text("frontier.result.backwash")
    elif mode == "salvage":
        item = next(
            (
                candidate for candidate in state.items
                if candidate.location == "ground"
                and candidate.region_id == state.spatial_id
                and candidate.ground_position == point
            ),
            None,
        )
        if item is None or actor.carrying_item_id:
            message = ecology_text("frontier.result.salvage_empty")
        else:
            item.location, item.owner_id, item.container_id = "enemy", actor.id, None
            item.ground_position = None
            actor.carrying_item_id = item.id
            message = ecology_text("frontier.result.salvage")
    elif mode == "firebreak":
        broken = 0
        for target in _line(state, point):
            patch = ensure_cell(state, target)
            if patch:
                broken += int(bool(patch.fire or patch.fuel))
                patch.fire, patch.fuel = 0, 0
                patch.coating, patch.smoke = "ash", max(2, patch.smoke)
        message = ecology_format("frontier.result.firebreak", broken=broken)
    elif mode == "counterfall":
        if not _protected(state, point):
            state.region.tile_changes[key(point)] = "%"
            cell.material, cell.support, cell.coating = "stone", 0, "ash"
        if state.position == point and not guarded and not _protected(state, point):
            from .actions import apply_damage

            message = apply_damage(
                state, 3, ecology_text("frontier.damage.counterfall"),
                damage_kind="blunt",
            )
        else:
            message = ecology_text("frontier.result.counterfall")
    elif mode == "siphon":
        moved = 0
        for target in _line(state, point):
            patch = ensure_cell(state, target)
            if patch:
                taken = min(2, patch.water)
                patch.water -= taken
                moved += taken
                if taken:
                    patch.coating = "mud"
        bed = ensure_cell(state, actor.home_position)
        if bed:
            bed.water, bed.fluid = min(3, bed.water + moved), "fresh"
        message = ecology_format("frontier.result.siphon", moved=moved)
    elif mode == "lever":
        if not _protected(state, point):
            state.region.tile_changes[key(point)] = "%"
            cell.material, cell.support = "timber", 1
        message = ecology_text("frontier.result.lever")
    elif mode == "slip":
        for target in _line(state, point):
            patch = ensure_cell(state, target)
            if patch:
                patch.material, patch.water, patch.fluid, patch.coating = "clay", 1, "fresh", "mud"
        if state.position == point and not guarded:
            from .inventory import add_status

            add_status(
                state, "mud-burden", ecology_text("frontier.status.slip.cause"),
                2, ecology_text("frontier.status.slip.consequence"),
            )
        message = ecology_text("frontier.result.slip")
    elif mode == "boom":
        frozen = cover = 0
        for target in _line(state, point):
            patch = ensure_cell(state, target)
            if patch and patch.water and patch.fluid != "salt":
                patch.water, patch.ice = 1, True
                frozen += 1
            elif patch and not _protected(state, target):
                state.region.tile_changes[key(target)] = "%"
                patch.material, patch.support = "timber", 1
                cover += 1
        message = ecology_format("frontier.result.boom", frozen=frozen, cover=cover)
    else:
        if state.position == point and not guarded:
            from .inventory import add_status, load_state
            step = Position(point.x + (actor.position.x > point.x) - (actor.position.x < point.x), point.y, point.z)
            if step != actor.position and is_walkable(state, step):
                state.position = step
            add_status(state, "net-drag", ecology_text("frontier.status.haul.cause"), 5 if load_state(state) in {"encumbered", "overloaded"} else 2, ecology_text("frontier.status.haul.consequence"))
            wet = ensure_cell(state, state.position)
            if wet:
                wet.water = 2
            message = ecology_text("frontier.result.haul_caught")
        else:
            message = ecology_text("frontier.result.haul_miss")
    _set_intent(actor, f"frontier.action.{mode}.spent", mode=mode, supplies=actor.supplies)
    return message


def record_outcomes(state):
    if state.location != "region":
        return
    from .inventory import create_item, release_enemy_possession
    for actor in state.threats:
        data = definition(actor)
        if not data or actor.status not in {"defeated", "retreated", "disabled", "negotiated", "evaded"}:
            continue
        marker = f"elite-outcome:{actor.id}"
        if state.region.changes.get(marker) == actor.status:
            continue
        state.region.changes[marker] = actor.status
        if actor.status in {"defeated", "disabled", "negotiated"}:
            recovered = release_enemy_possession(state, actor)
            if recovered:
                state.add_message(recovered.strip(), priority=3)
        state.region.changes[f"elite-left:{actor.id}"] = state.returned_expeditions
        state.remember(ecology_format("frontier.record.outcome", actor=actor.name, status=actor.status, position=actor.position, region=state.region.name, courier=state.courier.name))
        if actor.status in {"defeated", "disabled", "negotiated"} and not state.region.changes.get(f"elite-reward:{actor.id}"):
            item = create_item(state, data["reward"], ecology_format("frontier.provenance.reward", actor=actor.name), location="ground")
            item.region_id, item.ground_position = state.active_region_id, actor.position
            state.region.changes[f"elite-reward:{actor.id}"] = item.id
            state.add_message(ecology_format("frontier.record.reward", x=actor.position.x, y=actor.position.y, z=f"{actor.position.z:+d}"), priority=3)


def revisit_claimants(state):
    """One supplied return, never a resurrection or endless encounter refresh."""
    from .regions import region_reachable
    from .world import distance

    for actor in state.threats:
        data = definition(actor)
        if not data or not data["named"] or actor.status != "retreated" or actor.health <= 0:
            continue
        changed = state.region.changes
        marker = f"rival-return:{actor.id}"
        if changed.get(marker) or changed.get("environment_control_used"):
            continue
        if state.returned_expeditions <= changed.get(f"elite-left:{actor.id}", state.returned_expeditions):
            continue
        commodity = state.region.objective_commodity
        if state.market[commodity].stock <= 0:
            continue
        occupied = {other.position for other in state.threats if other.id != actor.id and other.status in {"watching", "engaged", "dormant"}}
        if state.location == "region":
            occupied.add(state.position)
        possible = [point for point in region_reachable(state.region) if point not in occupied and distance(point, actor.home_position) <= 3]
        if not possible:
            continue
        destination = min(possible, key=lambda point: (distance(point, actor.home_position), point.z, point.y, point.x))
        state.market[commodity].stock -= 1
        actor.status, actor.position = "watching", destination
        actor.health, actor.morale, actor.supplies = min(actor.max_health, actor.health + 2), 3, 2
        actor.reload_turns, actor.marked_position, actor.reaction = 0, None, ""
        changed[marker] = True
        _set_intent(actor, "frontier.intent.return")
        state.remember(ecology_format("frontier.record.return", actor=actor.name, commodity=commodity))


def claimant_terms(state):
    return next((a for a in state.threats if definition(a) and definition(a)["named"]
                 and a.status not in {"defeated", "disabled", "negotiated"}), None)


def settle_claimant(state):
    actor = claimant_terms(state)
    if actor is None:
        return False, action_format("social.claimant.none")
    if state.questlines[state.active_region_id].stage < 2 or state.trade_credit < 2:
        return False, action_format("social.claimant.requirements")
    state.trade_credit -= 2
    actor.status, actor.intent_id, actor.intent = "negotiated", "intent.social.claimant_settled", action_format("intent.social.claimant_settled")
    contact = state.contacts[state.active_region_id][1]
    contact.disposition = min(3, contact.disposition + 1)
    contact.memories.append(action_format("social.claimant.contact_memory", actor=actor.name))
    contact.memories[:] = contact.memories[-12:]
    record_outcomes(state)
    return True, action_format("social.claimant.settled", contact=contact.name, actor=actor.name)
