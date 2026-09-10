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
    definitions = (
        (
            f"contract:{region_id}:supply", contract_titles[0], "delivery-or-route-work",
            sites[0], account.dependency,
            f"{account.dependency} stock is {state.market[account.dependency].stock} after {title}; the next scheduled shift consumes a real lot at {_position_text(sites[0])}",
        ),
        (
            f"contract:{region_id}:scar", contract_titles[1], "recovery-or-material-work",
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


def validate_aftermath(state: GameState) -> None:
    if set(state.aftermath_quests) != set(state.regions):
        raise ValueError("aftermath quest persistence does not match the regions")
    if len(state.regional_contracts) > len(AFTERMATH_LINES) * 2:
        raise ValueError("regional contract bound exceeded")
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
        if contract.status not in {"available", "active", "worked", "completed", "failed"} or not 0 <= contract.stage <= 3:
            raise ValueError("invalid regional contract progression")
        from .regions import region_reachable

        if contract.site not in region_reachable(state.regions[contract.region_id]):
            raise ValueError("regional contract site is unreachable")
