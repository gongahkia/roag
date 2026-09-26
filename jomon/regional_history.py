"""Bounded working histories, institutional accounts, and aggregate production."""

from __future__ import annotations

from .calendar import ACTIONS_PER_DAY, calendar_at
from .catalog import CatalogError, HISTORY_SECTIONS, load_catalog
from .history_presentation import history_format
from .state import (
    ActorSchedule, Contact, GameState, Institution, MaterialCell, Position,
    RegionalEvent, stage_rng,
)

_HISTORY = load_catalog("history.json", HISTORY_SECTIONS)
_working = _HISTORY["working_accounts"]
_services = _HISTORY["institution_services"]
_ties = _HISTORY["institution_ties"]
_networks = _HISTORY["network_accounts"]
_contacts = _HISTORY["network_contacts"]

if (not isinstance(_working, dict) or len(_working) != 8
        or any(not isinstance(region, str) or not isinstance(row, list) or len(row) != 7
               or any(not isinstance(value, str) or not value for value in row)
               for region, row in _working.items())):
    raise CatalogError("history.json has invalid working accounts")
WORKING_ACCOUNTS = {region: tuple(row) for region, row in _working.items()}

if (not isinstance(_services, dict) or set(_services) != set(WORKING_ACCOUNTS)
        or any(not isinstance(row, list) or len(row) != 2
               or any(not isinstance(value, str) or not value for value in row)
               for row in _services.values())):
    raise CatalogError("history.json has invalid institution services")
INSTITUTION_SERVICES = {region: tuple(row) for region, row in _services.items()}

if (not isinstance(_ties, list) or len(_ties) != 3
        or any(not isinstance(row, list) or len(row) != 3
               or any(not isinstance(value, str) or not value for value in row)
               or row[0] not in WORKING_ACCOUNTS or row[1] not in WORKING_ACCOUNTS
               for row in _ties)):
    raise CatalogError("history.json has invalid institution ties")
INSTITUTION_TIES = tuple(tuple(row) for row in _ties)

_NETWORK_FIELDS = {"name", "home", "dependency", "production", "goal", "dispute", "presence", "service", "opposition"}
if (not isinstance(_networks, dict) or len(_networks) != 4
        or any(not isinstance(identity, str) or not identity.startswith("network:")
               or not isinstance(row, dict) or set(row) != _NETWORK_FIELDS
               or any(not isinstance(row[key], str) or not row[key] for key in _NETWORK_FIELDS - {"presence"})
               or not isinstance(row["presence"], list) or len(row["presence"]) != 2
               or any(not isinstance(region, str) or region not in WORKING_ACCOUNTS
                      for region in row["presence"])
               or len(set(row["presence"])) != 2
               or row["home"] not in row["presence"]
               for identity, row in _networks.items())):
    raise CatalogError("history.json has invalid network accounts")
NETWORK_ACCOUNTS = {
    identity: {**row, "presence": tuple(row["presence"])}
    for identity, row in _networks.items()
}

if (not isinstance(_contacts, list) or len(_contacts) != 8
        or any(not isinstance(row, list) or len(row) != 6
               or any(not isinstance(value, str) or not value for value in row)
               or row[0] not in NETWORK_ACCOUNTS or row[1] not in NETWORK_ACCOUNTS[row[0]]["presence"]
               for row in _contacts)
        or len({row[2] for row in _contacts}) != len(_contacts)):
    raise CatalogError("history.json has invalid network contacts")
NETWORK_CONTACTS = tuple(tuple(row) for row in _contacts)


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
                [history_format("history.network.contact_memory", institution=state.institutions[institution_id].name, region=region.name)],
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
            account.service += history_format("history.aftermath.shared")
        elif aftermath == "claimed":
            account.service += history_format("history.aftermath.claimed")
        account.relationships = {}
    for first in accounts:
        for second in accounts:
            if first.id == second.id:
                continue
            if first.production == second.dependency:
                first.relationships[second.id] = history_format("history.relationship.supplies", production=first.production, institution=second.name)
                second.relationships[first.id] = history_format("history.relationship.depends", institution=first.name, production=first.production)
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
            network.relationships[local.id] = history_format("history.relationship.witness", institution=local.name)
            local.relationships[network.id] = history_format("history.relationship.shared", institution=network.name)
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
    landforms = {key: value for key, value in region.generation_facts.items()
                 if key.startswith("landform:") or key in {"field_upper", "field_lower"}}
    region.generation_facts = {
        "version": 1, "watershed": water, "exposure": exposure,
        "geology": geology, "climate": climate,
        "ecology": "wet refuge" if water >= 2 else "dry nesting ground",
        "work": production, "dependency": dependency,
        "crisis": crisis, "repair": recovery,
        "evidence_mode": "physical" if new_geography else "inherited testimony",
        **landforms,
    }
    contacts = state.contacts[region_id]
    witness = contacts[-1]
    cache = next(container for container in reversed(region.containers)
                 if not container.hidden and "-sanctum-" not in container.id)
    landmark = next((key for key in ("ruin", "works", "mill", "objective") if key in region.landmarks), "objective")
    point = region.landmarks[landmark]
    protected = set(region.landmarks.values()) | {c.position for c in region.containers}
    protected |= {p for link in region.vertical_links for p in (link.first, link.second)}
    candidates = [Position(point.x + dx, point.y + dy, point.z) for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1))]
    scar = next((p for p in candidates if p not in protected and 0 <= p.x < region.width and 0 <= p.y < region.height and region.levels[str(p.z)][p.y][p.x] not in {" ", "#", "~"}), point)
    coordinate = f"{scar.x},{scar.y},{scar.z}"
    accounts = [
        ("water and stone", geology, history_format("history.event.water_and_stone.account", climate=climate.title(), production=production, dependency=dependency), history_format("history.event.water_and_stone.consequence", water=water)),
        (crisis, "timber" if crisis != "flood" else "soil", history_format("history.event.crisis.account", witness=witness.name, crisis=crisis, landmark=landmark.replace("_", " ")), history_format("history.event.crisis.consequence", coordinate=coordinate, dependency=dependency)),
        (recovery, dependency, history_format("history.event.recovery.account", institution=name, recovery=recovery, crisis=crisis), history_format("history.event.recovery.private_consequence" if institution.obligation else "history.event.recovery.shared_consequence")),
        ("contested occupation", production, history_format("history.event.contested_occupation.account", dispute=dispute), history_format("history.event.contested_occupation.consequence")),
        ("unsettled account", dependency, history_format("history.event.unsettled_account.account", witness=witness.name, cache=cache.name), history_format("history.event.unsettled_account.consequence", institution=name)),
    ]
    previous = None
    for index, (kind, material, account, consequence) in enumerate(accounts):
        event_id = f"{region_id}:history:{index}"
        region.regional_history.append(RegionalEvent(event_id, previous, kind, material, landmark, witness.id, institution_id, cache.id if index == 4 else coordinate, account, consequence))
        previous = event_id
    def record_crisis_history() -> None:
        event = region.regional_history[1]
        from .state import append_narrative_record
        append_narrative_record(state,event_id="regional_history.recorded",refs={"regional_event_id":event.id,"region_id":region_id,"material_id":event.material},params={"world_time":state.world_time,"watershed":water,"exposure":exposure},rendered=event.consequence)
    # Testimony remains conflicting; neither this record nor the UI pretends to
    # have simulated the years preceding the current action clock.
    if not new_geography:
        record_crisis_history()
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
        contact.memories.append(history_format("history.contact_memory", institution=name, crisis=crisis, recovery=recovery, dispute=dispute))
        del contact.memories[:-8]
    guards = [actor for actor in state.region_threats[region_id] if not actor.elite and actor.profile != "animal"]
    # witnesses recruit one compatible local guard detail, not opposed claims.
    preferred = next((actor for actor in guards if actor.ecology in {"warden", "worker"}), guards[0] if guards else None)
    detail = [actor for actor in guards if preferred and actor.allegiance == preferred.allegiance]
    for actor in detail[:2]:
        actor.group = institution_id
        actor.goal_reason = history_format("history.guard_reason", crisis=crisis, institution=name, production=production)
        if not actor.duty:
            actor.objective_position = region.landmarks["objective"]
    for actor in state.region_threats[region_id]:
        if actor.profile == "animal" and water >= 2:
            actor.home_position = region.landmarks.get("far_bank", actor.position)
    # The history is attached to a real, finite existing cache. Its reward has
    # the normal passive reducer and bulk, not a second invisible item system.
    cache.name = history_format(f"history.cache_name.{crisis.replace(' ', '_')}", witness=witness.name.split()[0])
    reward = {"flood": "rain cape", "fire": "ember cloth", "support loss": "quarry brace"}[crisis]
    if reward not in [cache.reward, *cache.extra_rewards]:
        cache.extra_rewards.append(reward)
    for edge in state.route_edges:
        if region_id in {edge.first, edge.second}:
            edge.cargo_risk = min(3, edge.cargo_risk + (1 if institution.obligation else 0))
    node = state.route_nodes.get(region_id)
    if node:
        node.description += history_format("history.route_dependency", crisis=crisis, dependency=dependency)
        node.market_interest = dependency
    record_crisis_history()


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
        region.changes["last_work_account"] = history_format("history.production.record", day=today, output=output, production=institution.production, dependency=institution.dependency, stock=need.stock, season=season)
        if institution.region_id == state.active_region_id and state.location == "region":
            state.add_message(history_format("history.production.notice", institution=institution.name, output=output, production=institution.production, status=f"{institution.dependency} {'consumed' if output else 'short'}"), priority=2)
    from .production import advance_craft_economy

    advance_craft_economy(state)


def deliver_dependency(state: GameState) -> tuple[bool, str]:
    from .inventory import consume_carried

    institution = account_for(state)
    if institution is None:
        return False, history_format("history.local_delivery.no_account")
    market = state.market[institution.dependency]
    if market.stock >= 5:
        return False, history_format("history.local_delivery.supplied")
    kind = f"commodity:{institution.dependency}"
    if not consume_carried(state, kind):
        return False, history_format("history.local_delivery.missing", dependency=institution.dependency)
    weighed = "market weights" in state.carried_passives
    market.stock = min(10, market.stock + (3 if weighed else 2))
    market.demand = max(0, market.demand - 1)
    institution.trust = min(3, institution.trust + 1)
    institution.obligation = max(0, institution.obligation - 1)
    institution.confidence = min(3, institution.confidence + (2 if weighed else 1))
    act = history_format("history.local_delivery.act", courier=state.courier.name, dependency=institution.dependency, day=state.world_time // ACTIONS_PER_DAY)
    institution.witnessed_acts.append(act)
    del institution.witnessed_acts[:-8]
    state.trade_credit += 1 + int(bool(state.courier and "guild-broker" in state.courier.skill_nodes))
    from .skill_tree import record_milestone

    record_milestone(state, f"trade:{state.active_region_id}")
    state.remember(history_format("history.local_delivery.memory", courier=state.courier.name, dependency=institution.dependency, institution=institution.name, trust=institution.trust, obligation=institution.obligation))
    return True, (
        history_format("history.local_delivery.result", weighed=history_format("history.local_delivery.weighed") if weighed else "")
    )


def deliver_network_dependency(state: GameState, contact_id: str) -> tuple[bool, str]:
    """Move one real lot into a travelling account through its embodied witness."""
    from .inventory import consume_carried

    institution = network_institution_for_contact(state, contact_id)
    if institution is None:
        return False, history_format("history.network_delivery.no_account")
    if institution.obligation >= 3:
        return False, history_format("history.network_delivery.obligations", institution=institution.name)
    if not consume_carried(state, f"commodity:{institution.dependency}"):
        return False, history_format("history.network_delivery.missing", dependency=institution.dependency)
    local_market = state.market[institution.dependency]
    local_market.stock = min(10, local_market.stock + 1)
    local_market.demand = max(0, local_market.demand - 1)
    institution.trust = min(3, institution.trust + 1)
    institution.confidence = min(3, institution.confidence + 1)
    act = (
        history_format("history.network_delivery.act", courier=state.courier.name, dependency=institution.dependency, contact_id=contact_id, day=state.world_time // ACTIONS_PER_DAY)
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
        history_format("history.network_delivery.memory", institution=institution.name, dependency=institution.dependency, region=state.region.name, trust=institution.trust)
    )
    return True, (
        history_format("history.network_delivery.result", contact=contact.name, institution=institution.name)
    )


def open_network_shelter(state: GameState, contact_id: str) -> tuple[bool, str]:
    """Spend earned trust on one persistent regional route concession."""
    institution = network_institution_for_contact(state, contact_id)
    if institution is None:
        return False, history_format("history.network_shelter.no_account")
    marker = f"network-shelter:{institution.id}:{state.active_region_id}"
    if state.vessel_changes.get(marker):
        return False, history_format("history.network_shelter.open")
    if institution.trust < 1 or institution.obligation >= 3:
        return False, history_format("history.network_shelter.requirement")
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
        history_format("history.network_shelter.act", region=state.region.name, edges=changed_edges)
    )
    institution.witnessed_acts.append(act)
    del institution.witnessed_acts[:-8]
    contact = next(
        contact for contact in state.contacts[state.active_region_id]
        if contact.id == contact_id
    )
    contact.memories.append(act)
    del contact.memories[:-8]
    state.remember(history_format("history.network_shelter.memory", institution=institution.name, act=act))
    return True, history_format("history.network_shelter.result", contact=contact.name, edges=changed_edges)


def network_service_options(
    state: GameState, contact_id: str
) -> tuple[tuple[str, str, str, bool, str], ...]:
    institution = network_institution_for_contact(state, contact_id)
    if institution is None:
        return ()
    marker = f"network-shelter:{institution.id}:{state.active_region_id}"
    from .practices import NETWORK_CONTACT_PRACTICE, learned_practice_ids
    from .progression_presentation import practice_display_name

    practice = NETWORK_CONTACT_PRACTICE[contact_id]
    learned = bool(state.courier and practice in learned_practice_ids(state.courier))
    practice_name = practice_display_name(practice)
    return (
        (
            "d", history_format("history.network_service.delivery.label", dependency=institution.dependency),
            "commitment", institution.obligation < 3,
            history_format("history.network_service.delivery.requirement"),
        ),
        (
            "c", history_format("history.network_service.shelter.label"),
            "commitment", institution.trust >= 1 and not state.vessel_changes.get(marker),
            history_format("history.network_service.shelter.requirement"),
        ),
        (
            "t", history_format("history.network_service.practice.label", practice=practice_name), "ordinary",
            institution.trust >= 1 and not learned,
            history_format("history.network_service.practice.requirement"),
        ),
    )


def ledger_lines(state: GameState) -> list[str]:
    institution = account_for(state)
    if not institution:
        return [history_format("history.ledger.no_account")]
    region = state.region
    facts = region.generation_facts
    lines = [history_format("history.ledger.fact_region", region=region.name, geology=facts["geology"], climate=facts["climate"]), history_format("history.ledger.fact_work", production=institution.production, dependency=institution.dependency), history_format("history.ledger.institution", institution=institution.name, goal=institution.goal), history_format("history.ledger.service", service=institution.service), history_format("history.ledger.dispute", dispute=institution.dispute, opposition=institution.opposition_reason), history_format("history.ledger.standing", trust=f"{institution.trust:+d}", obligation=institution.obligation, confidence=f"{institution.confidence:+d}")]
    lines.extend(history_format("history.ledger.relation", text=text) for text in institution.relationships.values())
    lines.extend(history_format("history.ledger.witnessed", text=text) for text in institution.witnessed_acts[-3:])
    present_networks = [
        state.institutions[institution_id]
        for institution_id, definition in NETWORK_ACCOUNTS.items()
        if institution_id in state.institutions
        and state.active_region_id in definition["presence"]
    ]
    for network in present_networks:
        lines += [
            history_format("history.ledger.network", institution=network.name, goal=network.goal),
            history_format("history.ledger.network_service", service=network.service, trust=f"{network.trust:+d}", obligation=network.obligation),
            history_format("history.ledger.opposition", opposition=network.opposition_reason),
        ]
    legend = state.legendary_objects.get(f"legend:{state.active_region_id}")
    if legend:
        from .legendary_presentation import legendary_format
        lines.append(legendary_format("legendary.object.ledger", clue=legend.clue))
    lines += [str(region.changes.get("last_work_account", history_format("history.ledger.no_recent"))), ""]
    for event in region.regional_history:
        lines += [history_format("history.ledger.testimony", account=event.account), history_format("history.ledger.evidence", evidence=event.evidence, consequence=event.consequence), ""]
    landform_keys = sorted(
        (key for key in facts if key.startswith("landform:")),
        key=lambda key: int(key.partition(":")[2]),
    )
    from .landscape_variation import fact_text

    lines.extend(history_format("history.ledger.landform", text=fact_text(region.id, key, facts[key])) for key in (*landform_keys, "field_upper", "field_lower") if key in facts)
    if region.changes.get("sanctum:cleared"):
        lines.append(history_format("history.ledger.sanctum", control=region.changes.get("sanctum:control", "unsettled"), seam="open" if region.changes.get("sanctum:secret_open") else "unopened"))
    lines += [history_format("history.ledger.forecast_line", forecast=forecast(state)), history_format("history.ledger.reading")]
    from .workline_presentation import workline_text
    from .worklines import WORKLINES, lines as work_lines
    if state.active_region_id in WORKLINES:
        lines += ["", workline_text("workline.ui.ledger_heading"), *work_lines(state)]
    from .aftermath import contracts_for
    from .aftermath_presentation import aftermath_opening

    contracts = contracts_for(state)
    if contracts:
        quest = state.aftermath_quests[state.active_region_id]
        lines += [
            "", history_format("history.ledger.aftermath_heading", opening=aftermath_opening(state.active_region_id)),
            history_format("history.ledger.settlement", branch=quest.branch, completed=sum(contract.status == "completed" for contract in contracts)),
            *[
                history_format("history.ledger.contract", title=contract.title, status=contract.status, cause=contract.cause)
                for contract in contracts
            ],
        ]
    from .interference import lines_for_region

    lines += lines_for_region(state, state.active_region_id)
    return lines


def forecast(state: GameState) -> str:
    exposure = int(state.region.generation_facts.get("exposure", 0))
    remaining = next((t - state.pressure_elapsed for t in state.region.process_thresholds if t > state.pressure_elapsed), None)
    stage = history_format("history.forecast.next", remaining=remaining, process=state.region.process_name) if remaining is not None else history_format("history.forecast.changed", process=state.region.process_name)
    return history_format("history.forecast", season=calendar_at(state).season, exposure="exposed" if exposure == 2 else "partly sheltered", stage=stage)


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
        if not 3 <= len(region.regional_history) <= 7 or len(region.generation_facts) > 17:
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
