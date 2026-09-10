"""Sixteen local elite problems, including eight finite returning claimants."""

from __future__ import annotations

# These rows identify authored situations; their actions below remain explicit.
ELITE_ROWS = (
    ("fen-marshal", "dunmire", "Veyra Reedlock", "reach", "surge", "control the drying bank", "Three counted releases flood a marked low lane; higher ground and the spill control remain usable.", "leave the marked lane, use height, or dog the regional spill", "relic:ebbglass spindle"),
    ("fen-stack", "dunmire", "smouldering peat crown", "machinery", "smoulder", "burn through the drying rack", "Dry rack fuel feeds smoke before a warned support fails; water stops the heat chain.", "quench the rack, shelter below, or cool it through the spill", "passive:ember cloth"),
    ("gorge-cordmaster", "rillscar", "Darrin Splitspan", "ranged", "sever", "take down the private scaffold", "Marks visible floor support before cutting, then moves toward a higher escape stair.", "brace the marked support, interrupt the cut, or tension the tailrace", "relic:hollow-bell shard"),
    ("gorge-convoy", "rillscar", "counterweight convoy foreman", "reach", "convoy", "bring a guarded load through", "Nearby escort bodies take two harm from strikes on the foreman; a finite hoist signal rallies them.", "pull the escort apart, defeat its helpers, or publish the tailrace control", "passive:quarry brace"),
    ("terrace-reeve", "marlbank", "Elsa Kilnmark", "ranged", "firing", "preserve the disputed firing", "Three charcoal charges ignite a warned crosswind line; wet cells do not light.", "pour on the fuel, move crosswind, or send the release to the kilns", "relic:coalheart seed"),
    ("terrace-shutters", "marlbank", "counterweighted kiln shutters", "machinery", "shutters", "close the exposed kiln lanes", "Alternates loose cover across two marked lanes; the blades themselves telegraph a sweep.", "cross a different lane, brace the linkage, or operate the kiln release", "passive:mill-tooth wedge"),
    ("estuary-pilot", "frostmere", "Tova Frostwake", "reach", "brine", "keep the disputed sounding open", "Counted brine breaks thin ice into current; unfrozen ground instead takes salt slurry.", "leave the marked sheet, use cleats and a light load, or release the ice boom", "relic:stillwater filament"),
    ("estuary-drum", "frostmere", "loaded net-haul drum", "machinery", "haul", "drag the net load into the cut", "Marks a haul cell, then pulls an unmoved bearer toward deep water; heavy loads suffer longer restraint.", "leave the mark, guard the haul, or cut the material linkage", "passive:load ledger"),
    ("hearth-lockhand", "hearthford", "Ysolde Lockhand", "reach", "backwash", "reopen the private meadow sluice", "A counted backwash fills a three-cell lane and pushes an unguarded bearer off its marked footing.", "leave the marked lane, guard the release, or dog the public sluice", "passive:tide ledger"),
    ("coast-wreckward", "greywash", "Bran Wreckward", "pursuer", "salvage", "remove exposed goods under the old wreck claim", "Marks one physical ground item before taking it; defeat or witnessed settlement releases the same object.", "pack the marked good, block the approach, or present the witnessed wreck account", "passive:wreck key"),
    ("forest-ashstep", "greenwold", "Mara Ashstep", "ranged", "firebreak", "cut every unlicensed fire line", "A warned three-cell break extinguishes useful flame but leaves obscuring ash where dry fuel stood.", "move the fire, use a wet boundary, or preserve the medicine coppice", "passive:smoke lens"),
    ("upland-bellrope", "whitecairn", "Orren Bellrope", "reach", "counterfall", "close the exposed warning stair", "Marks one unprotected floor before dropping loose visible cover; remaining in place risks a guarded blunt fall.", "leave the mark, stand under public structure, or ring the honest bell", "passive:cliff cord"),
    ("fen-pump-train", "dunmire", "backwater pump train", "machinery", "siphon", "empty the inhabited reed cut", "Drains a warned three-cell water line into its physical pump bed, leaving wet soil as slowing mud.", "flood a second opening, brace the pump bed, or dog the regional spill", "passive:river hooks"),
    ("gorge-wedge-crane", "rillscar", "cantilever wedge crane", "machinery", "lever", "move the quarry screen across the switchback", "Shifts loose cover onto a warned cell without sealing the alternate bridge.", "take the other span, brace the linkage, or tension the public tailrace", "passive:counterweight ring"),
    ("terrace-slip-wheel", "marlbank", "clay-slip spread wheel", "machinery", "slip", "coat the disputed seed-bed crossing", "Spreads shallow clay slurry across a warned line; an unguarded bearer becomes briefly mud-burdened.", "leave the line, guard in place, or operate the kiln release", "passive:cork float"),
    ("estuary-ice-boom", "frostmere", "loaded ice-boom capstan", "machinery", "boom", "close the sheltered winter braid", "Freezes warned fresh shallows or shifts loose timber cover where no water remains.", "salt the water, cut the linkage, or release the public ice boom", "passive:ice awl"),
)

AFTERMATH_ELITES = {
    "hearth-lockhand", "coast-wreckward", "forest-ashstep",
    "upland-bellrope", "fen-pump-train", "gorge-wedge-crane",
    "terrace-slip-wheel", "estuary-ice-boom",
}
NAMED_RIVALS = {
    "fen-marshal", "gorge-cordmaster", "terrace-reeve", "estuary-pilot",
    "hearth-lockhand", "coast-wreckward", "forest-ashstep",
    "upland-bellrope",
}

ELITE_DEFINITIONS = {
    identity: {
        "region": region, "name": name, "profile": profile, "role": "elite",
        "goal": goal, "vision": 11, "hearing": 9, "range": 8,
        "capability": capability, "counterplay": counterplay, "terrain": region,
        "budget": 6, "elite": True, "morale": 5, "glyph": "X",
        "supplies": 3, "mode": mode, "reward": reward,
        "ranged_kind": "sling", "named": identity in NAMED_RIVALS,
        "aftermath": identity in AFTERMATH_ELITES,
    }
    for identity, region, name, profile, mode, goal, capability, counterplay, reward in ELITE_ROWS
}


def definition(actor):
    return ELITE_DEFINITIONS.get(actor.id.removeprefix("frontier-elite:")) if actor.id.startswith("frontier-elite:") else None


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
    state.remember(
        f"{actor.name} now contests the physical aftermath work at "
        f"{point.x},{point.y}, z{point.z:+d}; its finite mechanism and terms are inspectable."
    )
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
    return damage - intercepted, f"{helper.name} physically takes {harm.amount} harm for the convoy"


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
        message = "The counted backwash fills three cells; guard holds footing while openings accept the fresh water."
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
            message = "The marked property has moved or is already secured; the salvage claim takes nothing."
        else:
            item.location, item.owner_id, item.container_id = "enemy", actor.id, None
            item.ground_position = None
            actor.carrying_item_id = item.id
            message = "The wreckward takes the exact marked object; interception, defeat or witnessed terms can recover it."
    elif mode == "firebreak":
        broken = 0
        for target in _line(state, point):
            patch = ensure_cell(state, target)
            if patch:
                broken += int(bool(patch.fire or patch.fuel))
                patch.fire, patch.fuel = 0, 0
                patch.coating, patch.smoke = "ash", max(2, patch.smoke)
        message = f"The warned firebreak clears {broken} fuel cells and leaves a three-cell ash screen."
    elif mode == "counterfall":
        if not _protected(state, point):
            state.region.tile_changes[key(point)] = "%"
            cell.material, cell.support, cell.coating = "stone", 0, "ash"
        if state.position == point and not guarded and not _protected(state, point):
            from .actions import apply_damage

            message = apply_damage(
                state, 3, "The warned loose-stone counterfall",
                damage_kind="blunt",
            )
        else:
            message = "Loose stone drops only onto the warned unprotected floor; it becomes passable cover, not a sealed route."
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
        message = f"The pump shifts {moved} water depth into its physical bed; the drained line remains muddy."
    elif mode == "lever":
        if not _protected(state, point):
            state.region.tile_changes[key(point)] = "%"
            cell.material, cell.support = "timber", 1
        message = "The cantilever shifts loose passable cover onto the warning mark; the alternate span remains open."
    elif mode == "slip":
        for target in _line(state, point):
            patch = ensure_cell(state, target)
            if patch:
                patch.material, patch.water, patch.fluid, patch.coating = "clay", 1, "fresh", "mud"
        if state.position == point and not guarded:
            from .inventory import add_status

            add_status(
                state, "mud-burden", "the warned clay-slip line caught an unguarded step",
                2, "guard or leave the slurry to recover ordinary footing",
            )
        message = "Clay slip coats three warned cells; an unguarded bearer on the mark is briefly burdened."
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
        message = f"The ice boom freezes {frozen} fresh shallows and shifts {cover} loose, passable timber screens."
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
