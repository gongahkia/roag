"""Bounded working histories, institutional accounts, and aggregate production."""

from __future__ import annotations

from .calendar import ACTIONS_PER_DAY, calendar_at
from .state import (
    ActorSchedule, Contact, GameState, Institution, MaterialCell, Position,
    RegionalEvent, stage_rng,
)


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

INSTITUTION_SERVICES = {
    "hearthford": ("shared mill repair and measured grain exchange", "it closes its stores after unwitnessed damage to the mill race"),
    "greywash": ("witnessed salvage title and a sheltered wreck berth", "it contests cargo taken from a marked wreck without an account"),
    "greenwold": ("coppice guidance, charcoal lots and field treatment", "it bars crews who burn living medicine plots"),
    "whitecairn": ("load warnings, crossing shelter and tested ironwork", "it refuses passage to couriers who silence or evade warning bells"),
    "dunmire": ("raised shelter, peat-cut guidance and bank repair", "it opposes drainage that sends floodwater toward inhabited islands"),
    "rillscar": ("bridge access, quarry bracing and fitted ironwork", "it withholds crews when a private guard seizes either bridge"),
    "marlbank": ("seed stores, kiln water and claywork contracts", "it refuses heat or water diversions that ruin the next field yield"),
    "frostmere": ("winter soundings, net shelter and marked ice routes", "it closes fast channels after false signals or damaged net stakes"),
}

# These authored disputes make the sparse production links legible even while
# the four frontier regions are still generated lazily.
INSTITUTION_TIES = (
    ("hearthford", "dunmire", "compares flood-bank timber against the mill water account"),
    ("hearthford", "marlbank", "compares grain measures and seasonal water releases"),
    ("whitecairn", "frostmere", "shares cold-route warnings and wool shelter claims"),
)

# Four travelling material interests. Each has two embodied regional witnesses;
# they aggregate work at day boundaries and never simulate distant people.
NETWORK_ACCOUNTS = {
    "network:bank-measures": {
        "name": "Common Bank Measures", "home": "hearthford",
        "dependency": "paper", "production": "grain",
        "goal": "keep flood and field measures comparable across two banks",
        "dispute": "private marks that hide who accepted a water release",
        "presence": ("hearthford", "marlbank"),
        "service": "witnessed bank shelter and a lower-risk measured freight edge",
        "opposition": "it refuses shelter while repeated private obligations remain unpaid",
    },
    "network:wreck-and-span": {
        "name": "Wreck and Span Witnesses", "home": "greywash",
        "dependency": "timber", "production": "ironwork",
        "goal": "keep recovered fittings attached to named wreck and bridge accounts",
        "dispute": "salvage claims that cross an upriver load witness",
        "presence": ("greywash", "rillscar"),
        "service": "accounted salvage transfer and a sheltered span approach",
        "opposition": "it closes its approach after unwitnessed stripping or bridge damage",
    },
    "network:burn-shelter": {
        "name": "Burn Shelter Runners", "home": "greenwold",
        "dependency": "salt fish", "production": "charcoal",
        "goal": "carry provisions between managed burn and raised wet refuge",
        "dispute": "fuel cutting that leaves inhabited peat without a dry shelter",
        "presence": ("greenwold", "dunmire"),
        "service": "provisioned fire refuge and a marked cross-weather detour",
        "opposition": "it withholds refuge from crews that spread fire toward habitation",
    },
    "network:cold-road": {
        "name": "Cold Road Sounders", "home": "whitecairn",
        "dependency": "wool", "production": "salt fish",
        "goal": "compare high-road bells with winter channel soundings",
        "dispute": "fast private crossings that invalidate public warnings",
        "presence": ("whitecairn", "frostmere"),
        "service": "cold-route warning, dry shelter and one safer exposed edge",
        "opposition": "it withdraws warnings after false bells or damaged ice stakes",
    },
}

NETWORK_CONTACTS = (
    ("network:bank-measures", "hearthford", "network-contact-hearthford", "Adra Silt", "bank-measure runner", "paper"),
    ("network:bank-measures", "marlbank", "network-contact-marlbank", "Pelen Reed", "field-measure witness", "grain"),
    ("network:wreck-and-span", "greywash", "network-contact-greywash", "Kellan Shoal", "wreck-span registrar", "timber"),
    ("network:wreck-and-span", "rillscar", "network-contact-rillscar", "Mora Span", "bridge salvage witness", "ironwork"),
    ("network:burn-shelter", "greenwold", "network-contact-greenwold", "Tessa Ember", "burn refuge runner", "salt fish"),
    ("network:burn-shelter", "dunmire", "network-contact-dunmire", "Iren Bank", "raised-shelter keeper", "charcoal"),
    ("network:cold-road", "whitecairn", "network-contact-whitecairn", "Varo Cairn", "high-road sounder", "wool"),
    ("network:cold-road", "frostmere", "network-contact-frostmere", "Nella Sound", "winter-braid witness", "salt fish"),
)


def network_institution_for_contact(state: GameState, contact_id: str) -> Institution | None:
    institution_id = next(
        (row[0] for row in NETWORK_CONTACTS if row[2] == contact_id), None
    )
    return state.institutions.get(institution_id) if institution_id else None


def _ensure_network_contacts(state: GameState) -> None:
    from .regions import region_reachable

    for institution_id, region_id, contact_id, name, role, interest in NETWORK_CONTACTS:
        if region_id not in state.regions:
            continue
        contacts = state.contacts[region_id]
        contact = next((candidate for candidate in contacts if candidate.id == contact_id), None)
        if contact is None:
            region = state.regions[region_id]
            anchor = region.landmarks.get("second_contact", region.landmarks["contact"])
            occupied = {candidate.position for candidate in contacts if candidate.position}
            occupied |= {
                actor.position for actor in state.region_threats[region_id]
                if actor.status in {"dormant", "watching", "engaged"}
            }
            occupied |= {container.position for container in region.containers}
            occupied |= {
                point for link in region.vertical_links
                for point in (link.first, link.second)
            }
            candidates = {
                point for point in region_reachable(region)
                if point not in occupied and point not in set(region.landmarks.values())
                and all(
                    other is None or other.z != point.z
                    or max(abs(other.x - point.x), abs(other.y - point.y)) > 2
                    for other in (candidate.position for candidate in contacts)
                )
            }
            point = min(
                candidates,
                key=lambda candidate: (
                    abs(candidate.z - anchor.z) * 100
                    + abs(candidate.x - anchor.x) + abs(candidate.y - anchor.y),
                    candidate.z, candidate.y, candidate.x,
                ),
            )
            contact = Contact(
                contact_id, name, role, 0,
                [f"Represents {state.institutions[institution_id].name} in {region.name}; material transfers and shelter are witnessed."],
                interest, region_id, point,
            )
            contacts.append(contact)
        state.actor_schedules.setdefault(
            contact.id,
            ActorSchedule(
                contact.id, f"region:{region_id}", contact.position,
                "witnessing network work", state.world_time + 16,
                f"region:{region_id}", contact.position,
                disposition=contact.disposition, last_update=state.world_time,
            ),
        )


def _ensure_network_institutions(state: GameState) -> None:
    for institution_id, definition in NETWORK_ACCOUNTS.items():
        if definition["home"] not in state.regions:
            continue
        institution = state.institutions.setdefault(
            institution_id,
            Institution(
                institution_id, definition["name"], definition["home"],
                definition["dependency"], definition["production"],
                definition["goal"], definition["dispute"],
                last_day=state.world_time // ACTIONS_PER_DAY,
            ),
        )
        institution.service = definition["service"]
        institution.opposition_reason = definition["opposition"]


def reconcile_network(state: GameState) -> None:
    """Rebuild bounded, derived institutional ties without touching memories."""
    _ensure_network_institutions(state)
    _ensure_network_contacts(state)
    accounts = list(state.institutions.values())
    local_accounts = {
        account.region_id: account for account in accounts
        if account.id.startswith("work:")
    }
    for account in accounts:
        if account.id.startswith("work:"):
            account.service, account.opposition_reason = INSTITUTION_SERVICES[account.region_id]
        else:
            definition = NETWORK_ACCOUNTS[account.id]
            account.service = definition["service"]
            account.opposition_reason = definition["opposition"]
        aftermath = state.regions[account.region_id].changes.get("aftermath_configuration")
        if aftermath == "shared":
            account.service += "; aftermath crews now maintain one firm marked approach"
        elif aftermath == "claimed":
            account.service += "; claimed aftermath work is available only against a recorded obligation"
        account.relationships = {}
    for first in accounts:
        for second in accounts:
            if first.id == second.id:
                continue
            if first.production == second.dependency:
                first.relationships[second.id] = f"supplies {first.production} to {second.name}"
                second.relationships[first.id] = f"depends on {first.name} for {first.production}"
    for first_region, second_region, dispute in INSTITUTION_TIES:
        if first_region not in local_accounts or second_region not in local_accounts:
            continue
        first, second = local_accounts[first_region], local_accounts[second_region]
        first.relationships.setdefault(second.id, dispute)
        second.relationships.setdefault(first.id, dispute)
    for institution_id, definition in NETWORK_ACCOUNTS.items():
        network = state.institutions.get(institution_id)
        if network is None:
            continue
        for region_id in definition["presence"]:
            local = local_accounts.get(region_id)
            if local is None:
                continue
            network.relationships[local.id] = f"maintains an embodied witness beside {local.name}"
            local.relationships[network.id] = f"shares material accounts with {network.name}"
    from .legendary import initialise_region_legend
    for region_id in local_accounts:
        initialise_region_legend(state, region_id)


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
    institution.service, institution.opposition_reason = INSTITUTION_SERVICES[region_id]
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
    # witnesses recruit one compatible local guard detail, not opposed claims.
    preferred = next((actor for actor in guards if actor.ecology in {"warden", "worker"}), guards[0] if guards else None)
    detail = [actor for actor in guards if preferred and actor.allegiance == preferred.allegiance]
    for actor in detail[:2]:
        actor.group = institution_id
        actor.goal_reason = f"witnessed {crisis} left {name} guarding its {production} account"
        if not actor.duty:
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
        from .worklines import supply_beacon
        supply_beacon(state, region, market, elapsed)
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
    from .production import advance_craft_economy

    advance_craft_economy(state)


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
    weighed = "market weights" in state.carried_passives
    market.stock = min(10, market.stock + (3 if weighed else 2))
    market.demand = max(0, market.demand - 1)
    institution.trust = min(3, institution.trust + 1)
    institution.obligation = max(0, institution.obligation - 1)
    institution.confidence = min(3, institution.confidence + (2 if weighed else 1))
    act = f"{state.courier.name} delivered a witnessed {institution.dependency} lot on day {state.world_time // ACTIONS_PER_DAY}."
    institution.witnessed_acts.append(act)
    del institution.witnessed_acts[:-8]
    state.trade_credit += 1
    from .skill_tree import record_milestone

    record_milestone(state, f"trade:{state.active_region_id}")
    state.remember(f"{state.courier.name} delivered {institution.dependency} to {institution.name}; household trust {institution.trust}, remaining obligation {institution.obligation}.")
    return True, (
        "The physical supply settles part of the work account: one credit, household trust, and material for the next shift."
        + (" Calibrated market weights verify one additional stock and confidence." if weighed else "")
    )


def deliver_network_dependency(state: GameState, contact_id: str) -> tuple[bool, str]:
    """Move one real lot into a travelling account through its embodied witness."""
    from .inventory import consume_carried

    institution = network_institution_for_contact(state, contact_id)
    if institution is None:
        return False, "No travelling material account is represented here."
    if institution.obligation >= 3:
        return False, f"{institution.name} requires its existing obligations settled before another transfer."
    if not consume_carried(state, f"commodity:{institution.dependency}"):
        return False, f"Bring one physical {institution.dependency} lot for the compared account."
    local_market = state.market[institution.dependency]
    local_market.stock = min(10, local_market.stock + 1)
    local_market.demand = max(0, local_market.demand - 1)
    institution.trust = min(3, institution.trust + 1)
    institution.confidence = min(3, institution.confidence + 1)
    act = (
        f"{state.courier.name} transferred one {institution.dependency} lot "
        f"through {contact_id} on day {state.world_time // ACTIONS_PER_DAY}."
    )
    institution.witnessed_acts.append(act)
    del institution.witnessed_acts[:-8]
    state.trade_credit += 1
    contact = next(
        contact for contact in state.contacts[state.active_region_id]
        if contact.id == contact_id
    )
    contact.disposition = min(3, contact.disposition + 1)
    contact.memories.append(act)
    del contact.memories[:-8]
    state.remember(
        f"{institution.name} received physical {institution.dependency} at "
        f"{state.region.name}; network trust {institution.trust}."
    )
    return True, (
        f"{contact.name} witnesses the physical lot into {institution.name}: "
        "one credit, local stock and network trust remain on the account."
    )


def open_network_shelter(state: GameState, contact_id: str) -> tuple[bool, str]:
    """Spend earned trust on one persistent regional route concession."""
    institution = network_institution_for_contact(state, contact_id)
    if institution is None:
        return False, "No travelling shelter account is represented here."
    marker = f"network-shelter:{institution.id}:{state.active_region_id}"
    if state.vessel_changes.get(marker):
        return False, "This witnessed shelter and route mark is already open."
    if institution.trust < 1 or institution.obligation >= 3:
        return False, "One witnessed material transfer and fewer than three obligations are required."
    changed_edges = 0
    for edge in state.route_edges:
        if state.active_region_id in {edge.first, edge.second}:
            before = (edge.cargo_risk, edge.weather_exposure)
            edge.cargo_risk = max(0, edge.cargo_risk - 1)
            edge.weather_exposure = max(0, edge.weather_exposure - 1)
            changed_edges += int(before != (edge.cargo_risk, edge.weather_exposure))
    institution.obligation = min(9, institution.obligation + 1)
    state.vessel_changes[marker] = {
        "edges": changed_edges, "institution": institution.id,
        "region": state.active_region_id,
    }
    act = (
        f"Opened witnessed shelter at {state.region.name}; {changed_edges} "
        "connected route edges now carry less cargo or weather exposure."
    )
    institution.witnessed_acts.append(act)
    del institution.witnessed_acts[:-8]
    contact = next(
        contact for contact in state.contacts[state.active_region_id]
        if contact.id == contact_id
    )
    contact.memories.append(act)
    del contact.memories[:-8]
    state.remember(f"{institution.name}: {act} One obligation remains physical in the account.")
    return True, f"{contact.name} opens the shelter mark; {changed_edges} connected route edges become safer, and one obligation is recorded."


def network_service_options(
    state: GameState, contact_id: str
) -> tuple[tuple[str, str, str, bool, str], ...]:
    institution = network_institution_for_contact(state, contact_id)
    if institution is None:
        return ()
    marker = f"network-shelter:{institution.id}:{state.active_region_id}"
    from .practices import NETWORK_CONTACT_PRACTICE

    practice = NETWORK_CONTACT_PRACTICE[contact_id]
    learned = bool(
        state.courier and practice in state.courier.learned_techniques
    )
    return (
        (
            "d", f"Transfer one {institution.dependency} into the travelling account",
            "commitment", institution.obligation < 3,
            "three unresolved network obligations block another transfer",
        ),
        (
            "c", "Open the witnessed shelter and safer connected route",
            "commitment", institution.trust >= 1 and not state.vessel_changes.get(marker),
            "needs one network trust; each regional shelter opens once",
        ),
        (
            "t", f"Learn {practice}", "ordinary",
            institution.trust >= 1 and not learned,
            "needs one network trust; this courier may learn the practice once",
        ),
    )


def ledger_lines(state: GameState) -> list[str]:
    institution = account_for(state)
    if not institution:
        return ["No working account has been witnessed here yet."]
    region = state.region
    facts = region.generation_facts
    lines = [f"FACT — {region.name}: {facts['geology']}, {facts['climate']}.", f"FACT — work: {institution.production}; dependency: {institution.dependency}.", f"{institution.name}: {institution.goal}.", f"Service: {institution.service}.", f"Dispute: {institution.dispute}; opposition: {institution.opposition_reason}.", f"Household trust {institution.trust:+d}; obligation {institution.obligation}; confidence {institution.confidence:+d}."]
    lines.extend(f"RELATION — {text}." for text in institution.relationships.values())
    lines.extend(f"WITNESSED — {text}" for text in institution.witnessed_acts[-3:])
    present_networks = [
        state.institutions[institution_id]
        for institution_id, definition in NETWORK_ACCOUNTS.items()
        if institution_id in state.institutions
        and state.active_region_id in definition["presence"]
    ]
    for network in present_networks:
        lines += [
            f"NETWORK — {network.name}: {network.goal}.",
            f"SERVICE — {network.service}; trust {network.trust:+d}, obligation {network.obligation}.",
            f"OPPOSITION — {network.opposition_reason}.",
        ]
    legend = state.legendary_objects.get(f"legend:{state.active_region_id}")
    if legend:
        lines.append(f"RUMOR — {legend.clue}")
    lines += [str(region.changes.get("last_work_account", "No new shift has been resolved in this account.")), ""]
    for event in region.regional_history:
        lines += [f"TESTIMONY — {event.account}", f"EVIDENCE — {event.evidence}: {event.consequence}", ""]
    lines += ["FORECAST — " + forecast(state), "Information and menus cost no action. Supplies and work do."]
    from .worklines import WORKLINES, lines as work_lines
    if state.active_region_id in WORKLINES:
        lines += ["", "UNDERTAKING — optional second regional work", *work_lines(state)]
    from .aftermath import AFTERMATH_LINES, contracts_for

    contracts = contracts_for(state)
    if contracts:
        quest = state.aftermath_quests[state.active_region_id]
        lines += [
            "", "AFTERMATH — " + AFTERMATH_LINES[state.active_region_id][0],
            f"Configuration {quest.branch}; {sum(contract.status == 'completed' for contract in contracts)}/2 contracts settled.",
            *[
                f"{contract.title}: {contract.status}; {contract.cause}."
                for contract in contracts
            ],
        ]
    from .interference import lines_for_region

    lines += lines_for_region(state, state.active_region_id)
    return lines


def forecast(state: GameState) -> str:
    exposure = int(state.region.generation_facts.get("exposure", 0))
    remaining = next((t - state.pressure_elapsed for t in state.region.process_thresholds if t > state.pressure_elapsed), None)
    stage = f"{remaining} more working actions to the next {state.region.process_name} sign" if remaining is not None else f"the {state.region.process_name} has changed; inspect its material aftermath"
    return f"{calendar_at(state).season}; {'exposed' if exposure == 2 else 'partly sheltered'} water; {stage}."


def validate_accounts(state: GameState) -> None:
    if len(state.institutions) > 12:
        raise ValueError("too many working accounts")
    for institution_id, institution in state.institutions.items():
        if institution.id != institution_id or institution.region_id not in state.regions:
            raise ValueError("invalid institution identity or region")
        if institution.dependency not in state.regional_markets[institution.region_id] or institution.production not in state.regional_markets[institution.region_id]:
            raise ValueError("working account refers to an unknown material")
        if not -3 <= institution.trust <= 3 or not 0 <= institution.obligation <= 9 or not -3 <= institution.confidence <= 3:
            raise ValueError("invalid institution standing")
        if not institution.service or not institution.opposition_reason or len(institution.witnessed_acts) > 8:
            raise ValueError("incomplete or unbounded institution record")
        if any(other not in state.institutions or other == institution.id or not reason for other, reason in institution.relationships.items()):
            raise ValueError("invalid institutional relationship")
        if len(state.institutions) > 1 and not institution.relationships:
            raise ValueError("isolated institution account")
    for institution_id, definition in NETWORK_ACCOUNTS.items():
        if institution_id not in state.institutions:
            raise ValueError("travelling institution account is missing")
        contact_regions = {
            region_id for linked_id, region_id, contact_id, *_ in NETWORK_CONTACTS
            if linked_id == institution_id
            and any(
                contact.id == contact_id
                for contact in state.contacts.get(region_id, ())
            )
        }
        expected = set(definition["presence"]) & set(state.regions)
        if contact_regions != expected:
            raise ValueError("travelling institution lacks an embodied regional witness")
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
