"""Ending-derived revisits and sixteen finite, causal local contracts."""

from __future__ import annotations

from .state import GameState, Position, QuestProgress, RegionalContract


AFTERMATH_LINES = {
    "hearthford": ("The Silt after the Compact", ("Rehang the public flood marks", "Account for the private wheel timber"), "far_bank", "works"),
    "greywash": ("What the Ebb Returned", ("Relight the storm-scoured dune line", "Recover a shifted wreck title"), "elevated", "wreck"),
    "greenwold": ("Shoots through the Burn", ("Replant the medicine boundary", "Count charcoal taken beyond the cut"), "burn_walk", "ruin"),
    "whitecairn": ("Echo beneath the Honest Bell", ("Seat the opened warning stair", "Recover the toll brace account"), "high_view", "works"),
    "dunmire": ("Islands after the Drain", ("Raise the inhabited peat walk", "Mark fuel cut below the waterline"), "far_bank", "works"),
    "rillscar": ("The Span that Remained", ("Brace the shared switchback", "Find the convoy's dropped counterweight"), "high_view", "works"),
    "marlbank": ("Water after Firing", ("Open the seed-bed rill", "Cool the claimant's abandoned kiln"), "far_bank", "works"),
    "frostmere": ("Soundings after Thaw", ("Restake the sheltered channel", "Recover a net line from broken ice"), "far_bank", "works"),
}

AFTERMATH_TOPOLOGIES = {
    "hearthford": ("flood-mark circuit", "wheel-timber account"),
    "greywash": ("storm-beacon line", "shifted-wreck recovery"),
    "greenwold": ("medicine boundary", "charcoal cut audit"),
    "whitecairn": ("warning stair", "toll-brace recovery"),
    "dunmire": ("raised peat walk", "submerged fuel mark"),
    "rillscar": ("switchback brace", "convoy counterweight"),
    "marlbank": ("seed-bed drainage", "abandoned kiln quench"),
    "frostmere": ("sheltered channel stakes", "broken-ice net recovery"),
}

DRAINAGE_TOPOLOGIES = {
    "flood-mark circuit", "raised peat walk", "seed-bed drainage",
    "sheltered channel stakes",
}
FIRE_TOPOLOGIES = {
    "storm-beacon line", "medicine boundary", "charcoal cut audit",
    "abandoned kiln quench",
}
SUPPORT_TOPOLOGIES = {
    "wheel-timber account", "warning stair", "switchback brace",
    "convoy counterweight",
}
RECOVERY_TOPOLOGIES = {
    "shifted-wreck recovery", "toll-brace recovery", "submerged fuel mark",
    "broken-ice net recovery",
}


def initialise_aftermath(state: GameState) -> None:
    state.aftermath_quests = {
        region_id: state.aftermath_quests.get(
            region_id, QuestProgress(status="locked")
        )
        for region_id in state.regions
    }
    # A pre-feature completed line starts its revisit clock now. Nothing is
    # repainted during load, and the next actual return can expose aftermath.
    for region_id, quest in state.questlines.items():
        if quest.status == "completed":
            state.regions[region_id].changes.setdefault(
                "aftermath_after_return", state.returned_expeditions
            )


def record_resolution_clock(state: GameState, region_id: str) -> None:
    state.regions[region_id].changes.setdefault(
        "aftermath_after_return", state.returned_expeditions
    )


def _position_text(point: Position) -> str:
    return f"{point.x},{point.y},{point.z}"


def _site(state: GameState, landmark: str, used: set[Position]) -> Position:
    from .regions import region_reachable

    region = state.region
    anchor = region.landmarks.get(landmark, region.landmarks["objective"])
    reachable = region_reachable(region)
    candidates = [
        Position(anchor.x + dx, anchor.y + dy, anchor.z)
        for dx, dy in (
            (0, 0), (-1, 0), (1, 0), (0, -1), (0, 1),
            (-1, -1), (1, -1), (-1, 1), (1, 1),
        )
    ]
    return next((point for point in candidates if point in reachable and point not in used), anchor)


def _branch(state: GameState) -> str:
    from .quests import QUESTS

    ending = next(
        (
            decision.split(":", 1)[1]
            for decision in reversed(state.questlines[state.active_region_id].decisions)
            if decision.startswith("ending:")
        ),
        QUESTS[state.active_region_id]["final"][0][0],
    )
    return "shared" if ending == QUESTS[state.active_region_id]["final"][0][0] else "claimed"


def _configure_sites(state: GameState, branch: str) -> tuple[Position, Position, Position]:
    from .materials import ensure_cell, key
    from .world import base_tile

    definition = AFTERMATH_LINES[state.active_region_id]
    used: set[Position] = set()
    route = _site(state, definition[2], used)
    used.add(route)
    works = _site(state, definition[3], used)
    used.add(works)
    evidence = _site(state, "ruin", used)
    sites = (route, works, evidence)
    for index, point in enumerate(sites):
        state.region.changes[f"aftermath_site:{index}"] = _position_text(point)
    route_cell = ensure_cell(state, route)
    works_cell = ensure_cell(state, works)
    evidence_cell = ensure_cell(state, evidence)
    if branch == "shared":
        if base_tile(state, route) not in {"#", " ", "~"}:
            state.region.tile_changes[key(route)] = "="
        if route_cell:
            route_cell.material, route_cell.water, route_cell.support = "timber", 0, 3
        if works_cell:
            works_cell.support = 3
            works_cell.collapse_due = 0
        if evidence_cell:
            evidence_cell.coating = "ash" if state.region.generation_facts.get("crisis") == "fire" else "wet"
    else:
        if base_tile(state, route) not in {"#", " ", "~"}:
            state.region.tile_changes[key(route)] = "%"
        if route_cell:
            route_cell.material, route_cell.support = "timber", 1
        if works_cell:
            works_cell.coating = "oil" if state.active_region_id in {"greywash", "marlbank"} else "resin"
            works_cell.fuel = max(2, works_cell.fuel)
        if evidence_cell:
            evidence_cell.support = min(1, evidence_cell.support)
    return sites


def _change_population_and_service(state: GameState, branch: str) -> None:
    account = state.institutions[f"work:{state.active_region_id}"]
    candidates = [
        actor for actor in state.threats
        if actor.profile not in {"animal", "machinery"}
        and actor.status in {"watching", "engaged", "dormant"}
    ]
    if branch == "shared":
        account.trust = min(3, account.trust + 1)
        account.confidence = min(3, account.confidence + 1)
        account.service += "; aftermath crews now maintain one firm marked approach"
        product = state.market[account.production]
        product.stock = min(10, product.stock + 1)
        for edge in state.route_edges:
            if state.active_region_id in {edge.first, edge.second}:
                edge.cargo_risk = max(0, edge.cargo_risk - 1)
        if candidates:
            actor = candidates[0]
            actor.status, actor.goal = "negotiated", "maintain the witnessed aftermath work"
            actor.goal_reason = "the public ending replaced its former material claim"
    else:
        account.obligation = min(9, account.obligation + 1)
        account.service += "; claimed aftermath work is available only against a recorded obligation"
        state.market[account.dependency].demand = min(
            9, state.market[account.dependency].demand + 1
        )
        if candidates:
            actor = candidates[0]
            actor.status = "watching"
            actor.group = f"aftermath:{state.active_region_id}:claim"
            actor.goal = "hold the altered aftermath worksite"
            actor.goal_reason = "the private ending left a material claim unsettled"
            actor.home_position = state.region.landmarks.get("works", actor.position)


def _create_contracts(state: GameState, sites: tuple[Position, Position, Position]) -> None:
    region_id = state.active_region_id
    title, contract_titles, _, _ = AFTERMATH_LINES[region_id]
    account = state.institutions[f"work:{region_id}"]
    witness = state.contacts[region_id][1]
    crisis = str(state.region.generation_facts.get("crisis", state.region.hazard))
    branch = str(state.region.changes["aftermath_configuration"])
    topologies = AFTERMATH_TOPOLOGIES[region_id]
    definitions = (
        (
            f"contract:{region_id}:supply", contract_titles[0], topologies[0],
            sites[0], account.dependency,
            f"{account.dependency} stock is {state.market[account.dependency].stock} after {title}; the next scheduled shift consumes a real lot at {_position_text(sites[0])}",
        ),
        (
            f"contract:{region_id}:scar", contract_titles[1], topologies[1],
            sites[1], account.production,
            f"the recorded {crisis} and {branch} ending left a physical work scar at {_position_text(sites[1])}",
        ),
    )
    for contract_id, contract_title, topology, site, commodity, cause in definitions:
        state.regional_contracts.setdefault(
            contract_id,
            RegionalContract(
                contract_id, contract_title, region_id, cause, topology,
                witness.id, site, commodity,
            ),
        )


def prepare_aftermath(state: GameState) -> bool:
    """Materialise an ending only on a later physical visit."""
    if state.location != "region":
        return False
    region_id = state.active_region_id
    primary = state.questlines[region_id]
    if primary.status != "completed" or state.region.changes.get("aftermath_configuration"):
        return False
    due = int(state.region.changes.get("aftermath_after_return", state.returned_expeditions))
    if state.returned_expeditions <= due:
        return False
    branch = _branch(state)
    state.region.changes["aftermath_configuration"] = branch
    sites = _configure_sites(state, branch)
    _change_population_and_service(state, branch)
    _create_contracts(state, sites)
    quest = state.aftermath_quests[region_id]
    quest.status, quest.stage, quest.branch = "available", 0, branch
    state.remember(
        f"{state.region.name} aftermath: the {branch} ending altered three sites, one working relationship, and two finite contracts."
    )
    state.add_message(
        f"On returning, you find {AFTERMATH_LINES[region_id][0]}: three changed sites and two witnessed contracts are now physical.",
        priority=3,
    )
    return True


def contracts_for(state: GameState, region_id: str | None = None) -> tuple[RegionalContract, ...]:
    region_id = region_id or state.active_region_id
    return tuple(
        sorted(
            (
                contract for contract in state.regional_contracts.values()
                if contract.region_id == region_id
            ),
            key=lambda contract: contract.id,
        )
    )


def _participant_position(state: GameState, contract: RegionalContract) -> Position:
    contact = next(
        contact for contact in state.contacts[contract.region_id]
        if contact.id == contract.participant_id
    )
    schedule = state.actor_schedules.get(contact.id)
    if schedule and schedule.area == f"region:{contract.region_id}":
        return schedule.position
    return contact.position


def near_participant(state: GameState, contract: RegionalContract) -> bool:
    from .world import distance

    return (
        state.location == "region"
        and state.active_region_id == contract.region_id
        and distance(state.position, _participant_position(state, contract)) <= 1
    )


def near_contract_site(state: GameState, contract: RegionalContract) -> bool:
    from .world import distance

    return (
        state.location == "region"
        and state.active_region_id == contract.region_id
        and distance(state.position, contract.site) <= 1
    )


def field_work_available(state: GameState) -> bool:
    return (
        state.gear == "repair tools"
        or state.weapon in {
            "billhook", "hand axe", "war hammer", "spade", "pollaxe",
            "mattock", "mallet and wedges", "quarry pick",
        }
        or bool(state.courier and state.courier.technique == "lever craft")
    )


def _update_line_progress(state: GameState, region_id: str) -> None:
    quest = state.aftermath_quests[region_id]
    contracts = contracts_for(state, region_id)
    resolved = sum(item.status in {"completed", "failed"} for item in contracts)
    quest.stage = resolved
    if resolved < len(contracts):
        quest.status = "active"
    elif all(item.status == "completed" for item in contracts):
        quest.status = "completed"
    else:
        quest.status = "refused"
    quest.consequence = "; ".join(
        item.outcome for item in contracts if item.outcome
    )


def contract_options(
    state: GameState, contract_id: str
) -> tuple[tuple[str, str, str, bool, str], ...]:
    contract = state.regional_contracts[contract_id]
    if contract.status == "completed":
        return (("b", "Back to the finite aftermath account", "ordinary", True, ""),)
    rows = []
    if contract.stage == 0:
        rows.append((
            "a", "Accept one physical witnessed copy", "commitment",
            near_participant(state, contract), "speak beside the named witness",
        ))
    if contract.stage == 1:
        carried = any(
            item.kind == f"commodity:{contract.commodity}"
            and item.location == "pack" and item.owner_id == state.active_courier_id
            for item in state.items
        )
        rows.extend((
            (
                "d", f"Deliver one physical {contract.commodity} lot", "commitment",
                near_participant(state, contract) and carried,
                f"return beside the witness carrying one {contract.commodity} lot",
            ),
            (
                "w", "Perform field work at the recorded scar", "commitment",
                near_contract_site(state, contract) and field_work_available(state),
                f"reach {contract.site.x},{contract.site.y}, z{contract.site.z:+d} with a working or levering tool",
            ),
            (
                "x", "Abandon the finite account and record the failure", "refusal",
                near_participant(state, contract), "return to the named witness",
            ),
        ))
    if contract.stage == 2:
        token = next(
            (item for item in state.items if item.id == contract.token_item_id), None
        )
        has_copy = bool(
            token and token.location == "pack"
            and token.owner_id == state.active_courier_id
        )
        replaceable = bool(
            token and token.location in {"lost", "destroyed"}
            and state.trade_credit >= 1
        )
        rows.append((
            "s", "Settle the worked account" + (" (one-credit replacement copy)" if replaceable and not has_copy else ""),
            "commitment", near_participant(state, contract) and (has_copy or replaceable),
            "return beside the witness with the contract copy, or one credit only if that copy was destroyed or lost",
        ))
    rows.append(("b", "Back to the finite aftermath account", "ordinary", True, ""))
    return tuple(rows)


def accept_contract(state: GameState, contract_id: str) -> tuple[bool, str]:
    from .inventory import auto_place, create_item

    contract = state.regional_contracts[contract_id]
    if contract.stage != 0 or not near_participant(state, contract):
        return False, "This contract must be accepted once beside its named witness."
    item = create_item(
        state, contract.id,
        f"{contract.title}: witnessed physical copy",
        location="ground",
    )
    item.region_id, item.ground_position = state.active_region_id, state.position
    packed = state.auto_place_enabled and auto_place(
        state, item.id, "pack", owner_id=state.active_courier_id
    )
    contract.token_item_id = item.id
    contract.stage, contract.status = 1, "active"
    state.aftermath_quests[contract.region_id].status = "active"
    return True, (
        f"{contract.title} is accepted from a named witness. The physical copy is "
        + ("packed." if packed else "left at your feet; use I to pack it.")
        + f" Supply {contract.commodity} here, or work the marked scar at {contract.site.x},{contract.site.y}, z{contract.site.z:+d}."
    )


def supply_contract(state: GameState, contract_id: str) -> tuple[bool, str]:
    from .inventory import consume_carried

    contract = state.regional_contracts[contract_id]
    if contract.stage != 1 or not near_participant(state, contract):
        return False, "Supply must be handed to the named witness after acceptance."
    if not consume_carried(state, f"commodity:{contract.commodity}"):
        return False, f"Carry one physical {contract.commodity} lot."
    contract.stage, contract.status, contract.approach = 2, "worked", "supply"
    return True, (
        f"One {contract.commodity} lot enters the actual local stock. The physical contract copy must still be settled."
    )


def work_contract(state: GameState, contract_id: str) -> tuple[bool, str]:
    from .materials import ensure_cell

    contract = state.regional_contracts[contract_id]
    if contract.stage != 1 or not near_contract_site(state, contract):
        return False, "The disclosed work must be performed beside its recorded scar."
    if not field_work_available(state):
        return False, "A working or levering tool is required."
    cell = ensure_cell(state, contract.site)
    if cell is None:
        return False, "The bounded material field cannot accept more work."
    old = (cell.fire, cell.water, cell.support, cell.coating)
    effect = ""
    if contract.topology in DRAINAGE_TOPOLOGIES:
        cell.water = max(0, cell.water - 2)
        cell.support = min(3, cell.support + 1)
        cell.ice = False
        state.market[contract.commodity].demand = max(
            0, state.market[contract.commodity].demand - 1
        )
        effect = "opened drainage lowers water and local material demand"
    elif contract.topology in FIRE_TOPOLOGIES:
        cell.fire = 0
        cell.smoke = 0
        cell.fuel = max(0, cell.fuel - 2)
        cell.coating = "wet" if contract.topology == "abandoned kiln quench" else "ash"
        state.region.changes["aftermath_firebreak"] = contract.topology
        effect = "bounded fire work removes flame, smoke and loose fuel"
    elif contract.topology in SUPPORT_TOPOLOGIES:
        cell.support = min(3, cell.support + 2)
        cell.collapse_due = 0
        state.region.changes["aftermath_supported_route"] = contract.topology
        effect = "structural work restores support and cancels warned collapse"
    else:
        cell.coating = ""
        cell.water = max(0, cell.water - 1)
        cell.support = min(3, cell.support + 1)
        from .quests import mark_secondary_lead

        marked = mark_secondary_lead(state)
        effect = (
            "physical recovery clears contamination and exposes a named store clue"
            if marked else "physical recovery clears contamination at the exhausted store line"
        )
    contract.stage, contract.status, contract.approach = 2, "worked", "field"
    state.region.changes[f"contract-work:{contract.id}"] = (
        f"{contract.topology}: {effect}; fire {old[0]}→{cell.fire}; "
        f"water {old[1]}→{cell.water}; support {old[2]}→{cell.support}; "
        f"coating {old[3] or 'none'}→{cell.coating or 'none'}"
    )
    return True, (
        f"The {contract.topology} changes physically: {effect}. Fire {old[0]}→{cell.fire}, "
        f"water {old[1]}→{cell.water}, support {old[2]}→{cell.support}. Return the witnessed copy."
    )


def settle_contract(state: GameState, contract_id: str) -> tuple[bool, str]:
    from .inventory import consume_carried

    contract = state.regional_contracts[contract_id]
    if contract.stage != 2 or not near_participant(state, contract):
        return False, "The worked account must be settled beside its named witness."
    token = next(
        (item for item in state.items if item.id == contract.token_item_id), None
    )
    copied = False
    if not (
        token and token.location == "pack" and token.owner_id == state.active_courier_id
        and consume_carried(state, token.kind)
    ):
        if not token or token.location not in {"lost", "destroyed"} or state.trade_credit < 1:
            return False, "Recover the physical copy; only a destroyed or lost copy can be replaced for one credit."
        state.trade_credit -= 1
        copied = True
    account = state.institutions[f"work:{contract.region_id}"]
    if contract.approach == "supply":
        market = state.market[contract.commodity]
        market.stock = min(10, market.stock + 2)
        market.demand = max(0, market.demand - 1)
        account.trust = min(3, account.trust + 1)
        contract.outcome = "A delivered lot restores two stock and lowers one demand; the witness records household trust."
    else:
        account.confidence = min(3, account.confidence + 1)
        for edge in state.route_edges:
            if contract.region_id in {edge.first, edge.second}:
                edge.cargo_risk = max(0, edge.cargo_risk - 1)
        physical = str(
            state.regions[contract.region_id].changes.get(
                f"contract-work:{contract.id}", contract.topology
            )
        )
        contract.outcome = (
            f"{physical}. The field account raises institutional confidence and "
            "eases connected cargo risk."
        )
    contract.stage, contract.status = 3, "completed"
    _update_line_progress(state, contract.region_id)
    if state.aftermath_quests[contract.region_id].status == "completed":
        from .quests import maybe_unlock_arc

        maybe_unlock_arc(state)
    account.witnessed_acts.append(
        f"{state.courier.name} settled {contract.title} by {contract.approach}."
    )
    del account.witnessed_acts[:-8]
    state.trade_credit += 1
    state.remember(f"{contract.title}: {contract.outcome}")
    return True, contract.outcome + " One credit is paid." + (" One credit first funded the replacement copy." if copied else "")


def abandon_contract(state: GameState, contract_id: str) -> tuple[bool, str]:
    contract = state.regional_contracts[contract_id]
    if contract.stage != 1 or not near_participant(state, contract):
        return False, "Return to the witness before abandoning the accepted account."
    contract.stage, contract.status = 3, "failed"
    contract.outcome = "The witness records an unmet promise; no replacement contract is generated."
    account = state.institutions[f"work:{contract.region_id}"]
    account.trust = max(-3, account.trust - 1)
    account.witnessed_acts.append(
        f"{state.courier.name} abandoned {contract.title} after accepting it."
    )
    del account.witnessed_acts[:-8]
    _update_line_progress(state, contract.region_id)
    state.remember(f"{contract.title}: {contract.outcome}")
    return True, contract.outcome


def resolve_contract(state: GameState, contract_id: str, choice: str) -> tuple[bool, str]:
    if contract_id not in state.regional_contracts:
        return False, "That aftermath account no longer exists."
    return {
        "a": accept_contract,
        "d": supply_contract,
        "w": work_contract,
        "s": settle_contract,
        "x": abandon_contract,
    }.get(choice, lambda *_: (False, "That is not a contract action."))(state, contract_id)


def contract_lines(state: GameState, contract_id: str) -> list[str]:
    contract = state.regional_contracts[contract_id]
    participant = next(
        contact for contact in state.contacts[contract.region_id]
        if contact.id == contract.participant_id
    )
    return [
        f"FACT — cause: {contract.cause}.",
        f"Named witness: {participant.name}, {participant.role}.",
        f"Physical site: {contract.site.x},{contract.site.y}, z{contract.site.z:+d}; material: {contract.commodity}.",
        f"Objective topology: {contract.topology}.",
        f"State: stage {contract.stage}/3, {contract.status}; approach {contract.approach or 'not chosen'}.",
        "DISCLOSED ANSWERS — deliver one real lot at the witness, or use a working tool at the scar.",
        "The accepted paper copy occupies the pack, can be lost or stolen, and is consumed at settlement.",
        *( ["FACT — " + contract.outcome] if contract.outcome else [] ),
    ]


def validate_aftermath(state: GameState) -> None:
    if set(state.aftermath_quests) != set(state.regions):
        raise ValueError("aftermath quest persistence does not match the regions")
    if len(state.regional_contracts) > len(AFTERMATH_LINES) * 2:
        raise ValueError("regional contract bound exceeded")
    all_topologies = {
        topology for topologies in AFTERMATH_TOPOLOGIES.values()
        for topology in topologies
    }
    if len(all_topologies) != len(AFTERMATH_LINES) * 2:
        raise ValueError("aftermath objective topologies are not distinct")
    for region_id, quest in state.aftermath_quests.items():
        if quest.status not in {"locked", "available", "active", "resolution", "completed", "refused"}:
            raise ValueError("invalid aftermath quest status")
        if not 0 <= quest.stage <= 6:
            raise ValueError("invalid aftermath quest stage")
        if region_id not in AFTERMATH_LINES:
            raise ValueError("unknown aftermath region")
    for contract_id, contract in state.regional_contracts.items():
        if contract.id != contract_id or contract.region_id not in state.regions:
            raise ValueError("invalid regional contract identity")
        if contract.participant_id not in {contact.id for contact in state.contacts[contract.region_id]}:
            raise ValueError("regional contract has no named participant")
        if contract.commodity not in state.regional_markets[contract.region_id]:
            raise ValueError("regional contract refers to unknown commodity")
        if contract.topology not in AFTERMATH_TOPOLOGIES[contract.region_id]:
            raise ValueError("regional contract has the wrong authored topology")
        if contract.status not in {"available", "active", "worked", "completed", "failed"} or not 0 <= contract.stage <= 3:
            raise ValueError("invalid regional contract progression")
        if contract.token_item_id is not None:
            token = next(
                (item for item in state.items if item.id == contract.token_item_id),
                None,
            )
            if token is None or token.kind != contract.id:
                raise ValueError("regional contract has no matching physical copy")
        from .regions import region_reachable

        if contract.site not in region_reachable(state.regions[contract.region_id]):
            raise ValueError("regional contract site is unreachable")
