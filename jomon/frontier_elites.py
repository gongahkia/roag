"""Eight local elite problems, including four finite returning claimants."""

from __future__ import annotations

# These rows identify authored situations; the eight actions below are explicit.
ELITE_ROWS = (
    ("fen-marshal", "dunmire", "Veyra Reedlock", "reach", "surge", "control the drying bank", "Three counted releases flood a marked low lane; higher ground and the spill control remain usable.", "leave the marked lane, use height, or dog the regional spill", "relic:ebbglass spindle"),
    ("fen-stack", "dunmire", "smouldering peat crown", "machinery", "smoulder", "burn through the drying rack", "Dry rack fuel feeds smoke before a warned support fails; water stops the heat chain.", "quench the rack, shelter below, or cool it through the spill", "passive:ember cloth"),
    ("gorge-cordmaster", "rillscar", "Darrin Splitspan", "ranged", "sever", "take down the private scaffold", "Marks visible floor support before cutting, then moves toward a higher escape stair.", "brace the marked support, interrupt the cut, or tension the tailrace", "relic:hollow-bell shard"),
    ("gorge-convoy", "rillscar", "counterweight convoy foreman", "reach", "convoy", "bring a guarded load through", "Nearby escort bodies take two harm from strikes on the foreman; a finite hoist signal rallies them.", "pull the escort apart, defeat its helpers, or publish the tailrace control", "passive:quarry brace"),
    ("terrace-reeve", "marlbank", "Elsa Kilnmark", "ranged", "firing", "preserve the disputed firing", "Three charcoal charges ignite a warned crosswind line; wet cells do not light.", "pour on the fuel, move crosswind, or send the release to the kilns", "relic:coalheart seed"),
    ("terrace-shutters", "marlbank", "counterweighted kiln shutters", "machinery", "shutters", "close the exposed kiln lanes", "Alternates loose cover across two marked lanes; the blades themselves telegraph a sweep.", "cross a different lane, brace the linkage, or operate the kiln release", "passive:mill-tooth wedge"),
    ("estuary-pilot", "frostmere", "Tova Frostwake", "reach", "brine", "keep the disputed sounding open", "Counted brine breaks thin ice into current; unfrozen ground instead takes salt slurry.", "leave the marked sheet, use cleats and a light load, or release the ice boom", "relic:stillwater filament"),
    ("estuary-drum", "frostmere", "loaded net-haul drum", "machinery", "haul", "drag the net load into the cut", "Marks a haul cell, then pulls an unmoved bearer toward deep water; heavy loads suffer longer restraint.", "leave the mark, guard the haul, or cut the material linkage", "passive:load ledger"),
)

ELITE_DEFINITIONS = {
    identity: {
        "region": region, "name": name, "profile": profile, "role": "elite",
        "goal": goal, "vision": 11, "hearing": 9, "range": 8,
        "capability": capability, "counterplay": counterplay, "terrain": region,
        "budget": 6, "elite": True, "morale": 5, "glyph": "X",
        "supplies": 3, "mode": mode, "reward": reward,
        "ranged_kind": "sling", "named": identity in {"fen-marshal", "gorge-cordmaster", "terrace-reeve", "estuary-pilot"},
    }
    for identity, region, name, profile, mode, goal, capability, counterplay, reward in ELITE_ROWS
}


def definition(actor):
    return ELITE_DEFINITIONS.get(actor.id.removeprefix("frontier-elite:")) if actor.id.startswith("frontier-elite:") else None


def install_elite(seed, region, actors):
    from .encounters import threat_from_archetype
    from .regions import region_reachable
    from .state import Position, stage_rng

    choices = sorted(key for key, data in ELITE_DEFINITIONS.items() if data["region"] == region.id)
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


def _protected(state, point):
    return point in set(state.region.landmarks.values()) or any(point in (link.first, link.second) for link in state.region.vertical_links) or any(box.position == point for box in state.region.containers)


def _line(state, point):
    from .state import Position
    from .world import base_tile
    return [p for dx in (-1, 0, 1) if base_tile(state, p := Position(point.x + dx, point.y, point.z)) not in {"#", " "}]


def guard_interception(state, target, damage):
    """The same visible escort bodies pay the cost of protecting the foreman."""
    from .world import distance, line_of_sight
    from .inventory import release_enemy_possession

    data = definition(target)
    if not data or data["mode"] != "convoy":
        return damage, ""
    helper = next((a for a in state.combatants if a.id != target.id and a.group == target.group
                   and a.status in {"watching", "engaged"} and distance(a.position, target.position) <= 2
                   and line_of_sight(state, target.position, a.position)), None)
    if helper is None or damage <= 0:
        return damage, ""
    intercepted = min(2, damage)
    helper.health = max(0, helper.health - intercepted)
    if helper.health == 0:
        helper.status = "defeated"
        release_enemy_possession(state, helper)
    return damage - intercepted, f"{helper.name} physically takes {intercepted} harm for the convoy"


def elite_action(state, actor, guarded):
    """Return None for ordinary actors; no unseen courier coordinate is targeted."""
    data = definition(actor)
    if data is None:
        return None
    from .enemy_ai import higher_access_target, next_path_step, perceive
    from .materials import ensure_cell, fields, key
    from .state import Position
    from .world import base_tile, distance, is_walkable, line_of_sight

    mode = data["mode"]
    visible, perceived, reason = perceive(state, actor)
    linkage = fields(state).get(key(actor.home_position))
    controlled = state.region.changes.get("environment_control_used")
    if controlled or (actor.profile == "machinery" and linkage and (linkage.support >= 3 or linkage.support == 0 or linkage.water)):
        actor.status, actor.intent = "disabled" if actor.profile == "machinery" else "negotiated", "the worked regional control has removed the disputed leverage"
        return f"{actor.name} stands down: the control or physical linkage has been secured."
    if actor.morale <= 0 or actor.health <= 2:
        actor.status, actor.intent = "retreated", "withdraws injured with the unresolved working claim"
        return f"{actor.name} withdraws; a surviving claimant may seek one later hearing."
    if actor.reload_turns:
        actor.reload_turns -= 1
        if mode == "sever":
            higher = higher_access_target(state, actor)
            if higher:
                step = next_path_step(state, actor, higher, stop_distance=0, limit=350)
                if step != state.position:
                    actor.position = step
        actor.intent = "resets a finite working charge; the recovery is an opening"
        return f"{actor.name} {actor.intent}."
    if actor.supplies <= 0:
        actor.status = "disabled" if actor.profile == "machinery" else "retreated"
        actor.intent = "has spent its last working charge"
        return f"{actor.name} has no charge left; the working threat ends."
    if actor.marked_position is None:
        if not visible:
            if perceived and actor.profile != "machinery":
                step = next_path_step(state, actor, perceived, stop_distance=3, limit=350)
                if step != state.position:
                    actor.position = step
            actor.intent = f"holds the material duty; {reason}"
            return ""
        if distance(actor.position, state.position) > 8:
            actor.intent = "waits for entry into its eight-pace working lane"
            return ""
        actor.marked_position = actor.home_position if mode == "smoulder" else state.position
        actor.reaction = mode
        actor.intent = f"prepares {mode} at {key(actor.marked_position)}; one action to leave, interrupt or secure the control"
        return f"{actor.name} {actor.intent}."
    point, actor.marked_position, actor.reaction = actor.marked_position, None, ""
    if not line_of_sight(state, actor.position, point):
        actor.intent = "abandons the prepared action behind blocked geometry"
        return f"{actor.name}'s marked operation is blocked by the changed sightline."
    actor.supplies -= 1
    actor.reload_turns = 2 if mode == "sever" else 1
    cell = ensure_cell(state, point)
    if cell is None:
        return f"{actor.name}'s operation finds no sound material footing."
    if mode == "surge":
        for target in _line(state, point):
            patch = ensure_cell(state, target)
            if patch:
                patch.water, patch.fluid = 3, "fresh"
        message = "The three-cell release becomes flowing water; openings carry it below and wet load now matters."
    elif mode == "smoulder":
        if cell.water:
            actor.status = "disabled"
            message = "The quenched peat crown cannot carry flame into its rack."
        else:
            cell.material, cell.fire, cell.fuel, cell.smoke = "charcoal", 1, 8, 4
            cell.support = max(0, cell.support - 1)
            if cell.support == 0:
                cell.collapse_due = state.world_time + 2
            message = "The dry peat crown smoulders; rising smoke precedes a two-action support warning."
    elif mode == "sever":
        if not _protected(state, point) and cell.support < 3:
            cell.support, cell.collapse_due = 0, state.world_time + 2
            message = "The cut support will fall in two actions; brace it or move through a different level."
        elif not _protected(state, point):
            cell.support = 1
            message = "The first cut exposes a weakened brace; the next cut can drop it unless repaired."
        else:
            message = "The marked public stair or store has independent support; the cord cut cannot drop it."
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
        message = "The finite hoist signal gathers the visible escort; separate its bodies to expose the foreman."
    elif mode == "firing":
        count = 0
        for target in _line(state, point):
            patch = ensure_cell(state, target)
            if patch and not patch.water:
                patch.material, patch.coating = "charcoal", "resin"
                patch.fire, patch.fuel = 1, 5
                count += 1
        message = f"The charcoal charge lights {count} dry marked cells; water defeats the other patches."
    elif mode == "shutters":
        for target in _line(state, point):
            if not _protected(state, target):
                coordinate = key(target)
                state.region.tile_changes[coordinate] = "." if state.region.tile_changes.get(coordinate) == "%" else "%"
        message = "Counterweights shift loose cover across the marked lane; routes stay passable."
        if state.position == point and not guarded:
            from .actions import apply_damage
            message += " " + apply_damage(state, 2, "The warned kiln shutter sweep")
    elif mode == "brine":
        frozen = cell.ice
        cell.ice, cell.water, cell.fluid, cell.coating = False, 3 if frozen else 1, "salt", "salt"
        message = "Brine breaks the marked ice into deep current." if frozen else "Brine salts the marked footing; exposed gear and aiming face abrasive slurry."
    else:
        if state.position == point and not guarded:
            from .inventory import add_status, load_state
            step = Position(point.x + (actor.position.x > point.x) - (actor.position.x < point.x), point.y, point.z)
            if step != actor.position and is_walkable(state, step):
                state.position = step
            add_status(state, "net-drag", "a loaded net drum caught the marked cell", 5 if load_state(state) in {"encumbered", "overloaded"} else 2, "guard and movement suffer; move free or wait out the haul")
            wet = ensure_cell(state, state.position)
            if wet:
                wet.water = 2
            message = "The warned haul catches the bearer; load lengthens restraint beside the wet opening."
        else:
            message = "The haul crosses its old mark; reposition or guard denies the net."
    actor.intent = f"{mode} spent; {actor.supplies} charges remain"
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
        state.remember(f"{actor.name}: {actor.status} at {actor.position}; the {state.region.name} working claim remembers {state.courier.name}.")
        if actor.status in {"defeated", "disabled", "negotiated"} and not state.region.changes.get(f"elite-reward:{actor.id}"):
            item = create_item(state, data["reward"], f"{actor.name}'s resolved working claim", location="ground")
            item.region_id, item.ground_position = state.active_region_id, actor.position
            state.region.changes[f"elite-reward:{actor.id}"] = item.id
            state.add_message(f"A physical claim reward remains at {actor.position.x},{actor.position.y}, z{actor.position.z:+d}; I opens the ground source.", priority=3)


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
        actor.intent = "returns once, provisioned by the local claim; remembers the earlier retreat"
        state.remember(f"{actor.name} used one {commodity} lot to return to the unresolved claim after retreat; old injuries and losses remain.")


def claimant_terms(state):
    return next((a for a in state.threats if definition(a) and definition(a)["named"]
                 and a.status not in {"defeated", "disabled", "negotiated"}), None)


def settle_claimant(state):
    actor = claimant_terms(state)
    if actor is None:
        return False, "No living claimant remains to settle with."
    if state.questlines[state.active_region_id].stage < 2 or state.trade_credit < 2:
        return False, "A witnessed regional material result and two credits are required."
    state.trade_credit -= 2
    actor.status, actor.intent = "negotiated", "accepts the witnessed working settlement; no further return"
    contact = state.contacts[state.active_region_id][1]
    contact.disposition = min(3, contact.disposition + 1)
    contact.memories.append(f"Carried Jomon's counted settlement to {actor.name}.")
    contact.memories[:] = contact.memories[-12:]
    record_outcomes(state)
    return True, f"{contact.name} witnesses two credits to {actor.name}; the claim ends without another fight."
