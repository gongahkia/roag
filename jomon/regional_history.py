"""Bounded working histories, institutional accounts, and aggregate production."""

from __future__ import annotations

from .calendar import ACTIONS_PER_DAY, calendar_at
from .state import GameState, Institution, MaterialCell, Position, RegionalEvent, stage_rng


# Geology and water name real work, not a universal faction or history engine.
WORKING_ACCOUNTS = {
    "hearthford": ("alluvium", "river meadow", "Mill Race Fellowship", "ironwork", "grain", "keep shared water working", "private repair claims"),
    "greywash": ("salt gravel", "tidal coast", "Greywash Salvage Table", "timber", "salt fish", "recover witnessed wreck cargo", "unwitnessed salvage"),
    "greenwold": ("woodland loam", "rain woodland", "Coppice Work Circle", "salt fish", "charcoal", "keep burn and medicine boundaries", "fuel against living coppice"),
    "whitecairn": ("limestone", "exposed ridge", "Whitecairn Load Witnesses", "wool", "ironwork", "keep warning bells honest", "tolls on safe crossings"),
    "dunmire": ("peat", "fen islands", "Raised Bank Company", "timber", "charcoal", "keep inhabited islands above water", "fuel banks displacing water"),
    "rillscar": ("ironstone", "sheltered gorge", "Two Bridge Account", "charcoal", "ironwork", "maintain both bridge claims", "fuel debt and private guarding"),
    "marlbank": ("clay", "flood terraces", "Marlbank Seed Court", "timber", "grain", "divide kiln water and field water", "firing heat against seed growth"),
    "frostmere": ("glacial gravel", "cold estuary", "Marked Channel Pilots", "wool", "salt fish", "maintain witnessed winter soundings", "fast cuts against sheltered nets"),
}


def initialise_account(state: GameState, region_id: str, *, new_geography: bool) -> None:
    """Five linked events; old saves get testimony, not invented terrain edits."""
    region = state.regions[region_id]
    if region.regional_history:
        return
    geology, climate, name, dependency, production, goal, dispute = WORKING_ACCOUNTS[region_id]
    rng = stage_rng(state.seed, f"working-history-v1:{region_id}")
    water, exposure = rng.randrange(1, 4), rng.randrange(3)
    crisis = rng.choice(("flood", "fire", "support loss"))
    recovery = rng.choice(("shared repair", "private advance"))
    institution_id = f"work:{region_id}"
    institution = state.institutions.setdefault(institution_id, Institution(
        institution_id, name, region_id, dependency, production, goal, dispute,
        obligation=1 if recovery == "private advance" else 0,
        last_day=state.world_time // ACTIONS_PER_DAY,
    ))
    region.generation_facts = {
        "version": 1, "watershed": water, "exposure": exposure,
        "geology": geology, "climate": climate,
        "ecology": "wet refuge" if water >= 2 else "dry nesting ground",
        "work": production, "dependency": dependency,
        "crisis": crisis, "repair": recovery,
        "evidence_mode": "physical" if new_geography else "inherited testimony",
    }
    contacts = state.contacts[region_id]
    witness = contacts[-1]
    cache = region.containers[-1]
    landmark = next((key for key in ("ruin", "works", "mill", "objective") if key in region.landmarks), "objective")
    point = region.landmarks[landmark]
    protected = set(region.landmarks.values()) | {c.position for c in region.containers}
    protected |= {p for link in region.vertical_links for p in (link.first, link.second)}
    candidates = [Position(point.x + dx, point.y + dy, point.z) for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1))]
    scar = next((p for p in candidates if p not in protected and 0 <= p.x < region.width and 0 <= p.y < region.height and region.levels[str(p.z)][p.y][p.x] not in {" ", "#", "~"}), point)
    coordinate = f"{scar.x},{scar.y},{scar.z}"
    accounts = [
        ("water and stone", geology, f"{climate.title()} supported {production} work; {dependency} had to arrive by vessel.", f"Water exposure {water} determined the working bank and its supply dependence."),
        (crisis, "timber" if crisis != "flood" else "soil", f"{witness.name} records a {crisis} at the {landmark.replace('_', ' ')}.", f"A scar at {coordinate} and a shortage of {dependency} remain evidence of the same loss."),
        (recovery, dependency, f"{name} secured a {recovery} after the {crisis}.", "The repair left a private obligation." if institution.obligation else "Shared repair raised market confidence, but output remains bounded by supplies."),
        ("contested occupation", production, f"Work guards arrived to settle {dispute}; local testimony disagrees about their mandate.", "One finite guard group protects the recorded work; it is not an endlessly renewed population."),
        ("unsettled account", dependency, f"{witness.name} placed the repair account with {cache.name}.", f"The named cache and the {name} supply account can still be resolved independently."),
    ]
    previous = None
    for index, (kind, material, account, consequence) in enumerate(accounts):
        event_id = f"{region_id}:history:{index}"
        region.regional_history.append(RegionalEvent(event_id, previous, kind, material, landmark, witness.id, institution_id, cache.id if index == 4 else coordinate, account, consequence))
        previous = event_id
    # Testimony remains conflicting; neither this record nor the UI pretends to
    # have simulated the years preceding the current action clock.
    if not new_geography:
        return
    if scar not in protected:
        if crisis == "flood":
            region.materials[coordinate] = MaterialCell(material="soil", water=water, coating="wet")
            region.tile_changes[coordinate] = "m"
        elif crisis == "fire":
            region.materials[coordinate] = MaterialCell(material="timber", coating="ash", support=2)
            region.tile_changes[coordinate] = ";"
        else:
            region.materials[coordinate] = MaterialCell(material="timber", support=1)
    region.process_thresholds = [threshold + water * 3 - exposure * 2 for threshold in region.process_thresholds]
    market = state.regional_markets[region_id]
    market[dependency].demand = min(8, market[dependency].demand + water)
    market[production].stock = min(8, market[production].stock + (1 if recovery == "shared repair" else 0))
    institution.confidence = 1 if recovery == "shared repair" else -1
    for contact in contacts:
        contact.memories.append(f"{name}: {crisis}, then {recovery}; {dispute} remains unsettled.")
        del contact.memories[:-8]
    guards = [actor for actor in state.region_threats[region_id] if not actor.elite and actor.profile != "animal"]
    for actor in guards[:2]:
        actor.group = institution_id
        actor.goal_reason = f"witnessed {crisis} left {name} guarding its {production} account"
        actor.objective_position = region.landmarks["objective"]
    for actor in state.region_threats[region_id]:
        if actor.profile == "animal" and water >= 2:
            actor.home_position = region.landmarks.get("far_bank", actor.position)
    # The history is attached to a real, finite existing cache. Its reward has
    # the normal passive reducer and bulk, not a second invisible item system.
    cache.name = f"{witness.name.split()[0]}'s {('Flood Binding' if crisis == 'flood' else 'Ash Account' if crisis == 'fire' else 'Load Witness')} coffer"
    reward = {"flood": "rain cape", "fire": "ember cloth", "support loss": "quarry brace"}[crisis]
    if reward not in [cache.reward, *cache.extra_rewards]:
        cache.extra_rewards.append(reward)
    for edge in state.route_edges:
        if region_id in {edge.first, edge.second}:
            edge.cargo_risk = min(3, edge.cargo_risk + (1 if institution.obligation else 0))
    node = state.route_nodes.get(region_id)
    if node:
        node.description += f"; {crisis} repairs depend on {dependency}"
        node.market_interest = dependency


def account_for(state: GameState, region_id: str | None = None) -> Institution | None:
    return state.institutions.get(f"work:{region_id or state.active_region_id}")


def advance_production(state: GameState) -> None:
    """Coarse named working accounts at day boundaries, no distant tile work."""
    today = state.world_time // ACTIONS_PER_DAY
    for institution in state.institutions.values():
        if today <= institution.last_day:
            continue
        elapsed = min(3, today - institution.last_day)
        institution.last_day = today
        market = state.regional_markets[institution.region_id]
        need, product = market[institution.dependency], market[institution.production]
        region = state.regions[institution.region_id]
        season = calendar_at(state).season
        output = elapsed if need.stock else 0
        if season == "winter" and institution.production == "grain":
            output = 0
        if region.process_stage >= 2 and not region.changes.get("environment_control_used"):
            output = max(0, output - 1)
        if output:
            need.stock = max(0, need.stock - 1)
            product.stock = min(10, product.stock + output)
            product.demand = max(0, product.demand - 1)
        else:
            need.demand = min(8, need.demand + 1)
            product.stock = max(0, product.stock - 1)
        region.changes["last_work_account"] = f"Day {today}: {output} {institution.production} output; {institution.dependency} stock {need.stock}; {season}."
        if institution.region_id == state.active_region_id and state.location == "region":
            state.add_message(f"{institution.name}: {output} {institution.production} output; {institution.dependency} {'consumed' if output else 'short'}.", priority=2)


def deliver_dependency(state: GameState) -> tuple[bool, str]:
    from .inventory import consume_carried

    institution = account_for(state)
    if institution is None:
        return False, "This place has no witnessed working account."
    market = state.market[institution.dependency]
    if market.stock >= 5:
        return False, "The association has enough supplies for now; it offers no empty contract."
    kind = f"commodity:{institution.dependency}"
    if not consume_carried(state, kind):
        return False, f"Bring one physical {institution.dependency} lot; the store is short."
    market.stock = min(10, market.stock + 2)
    market.demand = max(0, market.demand - 1)
    institution.trust = min(3, institution.trust + 1)
    institution.obligation = max(0, institution.obligation - 1)
    institution.confidence = min(3, institution.confidence + 1)
    state.trade_credit += 1
    state.remember(f"{state.courier.name} delivered {institution.dependency} to {institution.name}; household trust {institution.trust}, remaining obligation {institution.obligation}.")
    return True, "The physical supply settles part of the work account: one credit, household trust, and material for the next shift."


def ledger_lines(state: GameState) -> list[str]:
    institution = account_for(state)
    if not institution:
        return ["No working account has been witnessed here yet."]
    region = state.region
    facts = region.generation_facts
    lines = [f"FACT — {region.name}: {facts['geology']}, {facts['climate']}.", f"FACT — work: {institution.production}; dependency: {institution.dependency}.", f"{institution.name}: {institution.goal}.", f"Dispute: {institution.dispute}.", f"Household trust {institution.trust:+d}; obligation {institution.obligation}; confidence {institution.confidence:+d}.", str(region.changes.get("last_work_account", "No new shift has been resolved in this account.")), ""]
    for event in region.regional_history:
        lines += [f"TESTIMONY — {event.account}", f"EVIDENCE — {event.evidence}: {event.consequence}", ""]
    lines += ["FORECAST — " + forecast(state), "Information and menus cost no action. Supplies and work do."]
    return lines


def forecast(state: GameState) -> str:
    exposure = int(state.region.generation_facts.get("exposure", 0))
    remaining = next((t - state.pressure_elapsed for t in state.region.process_thresholds if t > state.pressure_elapsed), None)
    stage = f"{remaining} more working actions to the next {state.region.process_name} sign" if remaining is not None else f"the {state.region.process_name} has changed; inspect its material aftermath"
    return f"{calendar_at(state).season}; {'exposed' if exposure == 2 else 'partly sheltered'} water; {stage}."


def validate_accounts(state: GameState) -> None:
    if len(state.institutions) > 8:
        raise ValueError("too many working accounts")
    for institution_id, institution in state.institutions.items():
        if institution.id != institution_id or institution.region_id not in state.regions:
            raise ValueError("invalid institution identity or region")
        if institution.dependency not in state.regional_markets[institution.region_id] or institution.production not in state.regional_markets[institution.region_id]:
            raise ValueError("working account refers to an unknown material")
        if not -3 <= institution.trust <= 3 or not 0 <= institution.obligation <= 9 or not -3 <= institution.confidence <= 3:
            raise ValueError("invalid institution standing")
    for region in state.regions.values():
        if not region.regional_history:
            continue
        if not 3 <= len(region.regional_history) <= 7 or len(region.generation_facts) > 16:
            raise ValueError("regional causal record exceeds its bounds")
        seen = set()
        for event in region.regional_history:
            if event.id in seen or event.previous is not None and event.previous not in seen:
                raise ValueError("broken regional event chain")
            if event.institution not in state.institutions or event.witness not in {contact.id for contact in state.contacts[region.id]}:
                raise ValueError("regional event has an invalid named participant")
            if event.place not in region.landmarks:
                raise ValueError("regional event has an unknown landmark")
            seen.add(event.id)
