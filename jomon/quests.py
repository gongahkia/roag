"""Direct authored regional questlines and one bounded cross-region account."""

from __future__ import annotations

from .inventory import auto_place, create_item, record_acquisition
from .state import GameState, QuestProgress

REGION_IDS = ("hearthford", "greywash", "greenwold", "whitecairn")

QUESTS = {
    "hearthford": {
        "title": "The Mill Race Compact",
        "cache": "reed",
        "lead": "Mara's flood marks point to the Flood-islet cache south of the road loop.",
        "final": (
            ("l", "Bind the mill labour and sluice as a public compact", "commitment", True, ""),
            ("r", "Recognise the reeve's faster private repair claim", "danger", True, ""),
        ),
    },
    "greywash": {
        "title": "The Ledger Beneath the Ebb",
        "cache": "greywash-wreck",
        "lead": "Edda names the Distinctive wreck locker on the low road before the tide covers it.",
        "final": (
            ("s", "Wait for the safer delayed salt road", "ordinary", True, ""),
            ("e", "Take the ebb salvage while the chain route is exposed", "danger", False, "open the wreck locker or dog the tide chain"),
        ),
    },
    "greenwold": {
        "title": "A Fire Kept to Its Bounds",
        "cache": "greenwold-watch",
        "lead": "Nera's bird counts mark a Canopy cache above the western clearing.",
        "final": (
            ("m", "Save the medicine coppice and narrow the burn", "commitment", True, ""),
            ("c", "Feed the charcoal contract and accept a wider managed burn", "danger", True, ""),
        ),
    },
    "whitecairn": {
        "title": "The Honest Bell",
        "cache": "whitecairn-bridge",
        "lead": "Pera's load marks identify the Ridge bridge coffer above the quarry hoist.",
        "final": (
            ("w", "Ring an honest warning and close the unstable face", "commitment", True, ""),
            ("x", "Expose the false toll with recovered quarry evidence", "danger", False, "recover the bridge coffer or brace the quarry"),
        ),
    },
    "dunmire": {
        "title": "The Ground Owed to Water", "cache": "dunmire-deep",
        "lead": "Deren's drain marks lead from the store cellar to the buried peat strongbox.",
        "final": (("b", "Breach the drying bank; save the islands, lose this fuel yield", "commitment", True, ""),
                  ("h", "Hold the drying bank and honour the winter fuel obligation", "danger", False, "brace the drying control or recover the drain record")),
    },
    "rillscar": {
        "title": "A Bridge with Two Owners", "cache": "rillscar-crown",
        "lead": "Vessa kept the bridge load account in the Weatherward roof coffer, above the cutworks.",
        "final": (("o", "Open the tailrace and retire the private bridge guard", "commitment", True, ""),
                  ("b", "Bind the two bridge claims with the recovered load account", "danger", False, "secure the upper coffer or brace the tailrace control")),
    },
    "marlbank": {
        "title": "The Kiln and the Seed Bed", "cache": "marlbank-ledger",
        "lead": "Odrin's Witnessed market coffer records which kiln water also feeds the seed terraces.",
        "final": (("f", "Give the release to the fields; cool the working kilns", "commitment", True, ""),
                  ("k", "Preserve the kiln firing and ration the seed terraces", "danger", False, "read the water account or operate the release")),
    },
    "frostmere": {
        "title": "The Last Marked Channel", "cache": "frostmere-loft",
        "lead": "Brenna's sounding board rests in the Loft survey cabinet, above the winter nets.",
        "final": (("l", "Mark the long lee channel; shelter the nets and delay trade", "commitment", True, ""),
                  ("c", "Open the direct ice cut under a witnessed pilot obligation", "danger", False, "recover the sounding board or secure the ice control")),
    },
}

QUEST_REWARDS = {
    "hearthford": "witness token",
    "greywash": "storm vane",
    "greenwold": "ember cloth",
    "whitecairn": "high tread",
    "dunmire": "cork float",
    "rillscar": "quarry brace",
    "marlbank": "ember cloth",
    "frostmere": "storm vane",
}

ARC_REGIONS = {1: "greywash", 2: "greenwold", 3: "whitecairn", 4: "hearthford"}
ARC_TITLE = "The Four Working Marks"


def initialise_quests(state: GameState) -> None:
    state.questlines = {
        region_id: state.questlines.get(region_id, QuestProgress())
        for region_id in state.regions
    }
    state.treasure_marks = {
        region_id: list(state.treasure_marks.get(region_id, []))
        for region_id in state.regions
    }


def mark_treasure(state: GameState, region_id: str, container_id: str, clue: str) -> bool:
    marks = state.treasure_marks.setdefault(region_id, [])
    if container_id in marks:
        return False
    marks.append(container_id)
    state.remember(f"Treasure lead in {state.regions[region_id].name}: {clue}")
    state.add_message(f"Treasure lead marked: {clue}", priority=3)
    return True


def _assign_regional_duty(state: GameState) -> None:
    """Tie one existing finite actor to the line's material location."""
    preferences = {
        "hearthford": ("protector", "pursuer", "reach"),
        "greywash": ("lookout", "shooter", "skirmisher"),
        "greenwold": ("suppressor", "flanker", "tracker"),
        "whitecairn": ("shooter", "protector", "lookout"),
        "dunmire": ("protector", "lookout"),
        "rillscar": ("protector", "shooter"),
        "marlbank": ("lookout", "protector"),
        "frostmere": ("shooter", "thief"),
    }[state.active_region_id]
    actors = [
        threat for threat in state.threats
        if not threat.elite and threat.profile != "animal" and threat.status in {"watching", "engaged"}
    ]
    actor = next(
        (
            threat for role in preferences for threat in actors
            if threat.role == role
        ),
        actors[0] if actors else None,
    )
    if actor is None:
        return
    target = state.region.landmarks["objective"]
    actor.home_position = target
    actor.goal = {
        "hearthford": "hold the disputed mill material",
        "greywash": "secure the tide-bound salvage",
        "greenwold": "control the burn boundary",
        "whitecairn": "guard the quarry warning",
        "dunmire": "defend the drying bank",
        "rillscar": "hold the disputed bridge account",
        "marlbank": "guard the kiln water release",
        "frostmere": "keep the winter soundings",
    }[state.active_region_id]
    actor.goal_reason = (
        f"the opening decision in {QUESTS[state.active_region_id]['title']} "
        "gave this existing patrol a material duty"
    )
    state.region.changes["quest_guard_id"] = actor.id
    state.region.changes["quest_guard_reason"] = actor.goal_reason
    state.add_message(
        f"You learn that the {actor.name} now guards the material objective; its duty can be observed or avoided.",
        priority=3,
    )


def record_objective_decision(state: GameState, decision: str) -> None:
    quest = state.questlines[state.active_region_id]
    if quest.stage > 0:
        return
    quest.branch = decision
    quest.decisions.append(f"opening:{decision}")
    if decision == "refuse":
        quest.stage, quest.status = 2, "resolution"
    else:
        quest.stage, quest.status = 1, "active"
    definition = QUESTS[state.active_region_id]
    quest.cache_marked = mark_treasure(
        state, state.active_region_id, definition["cache"], definition["lead"]
    )
    _assign_regional_duty(state)


def record_objective_completion(state: GameState, altered: bool) -> None:
    quest = state.questlines[state.active_region_id]
    quest.stage, quest.status = 2, "resolution"
    method = "material alteration" if altered else "accountable delivery"
    quest.decisions.append(f"objective:{method}")
    evidence = f"{state.active_region_id}:witnessed-{method.replace(' ', '-')}"
    if evidence not in state.objective_evidence:
        state.objective_evidence.append(evidence)


def record_environmental_control(state: GameState) -> None:
    quest = state.questlines[state.active_region_id]
    marker = "environmental-control"
    if marker not in quest.decisions:
        quest.decisions.append(marker)


def record_container_opened(state: GameState, container_id: str) -> None:
    quest = state.questlines[state.active_region_id]
    if container_id == QUESTS[state.active_region_id]["cache"]:
        quest.optional_done = True
        state.region.changes["quest_cache_opened"] = True
        if "optional-cache" not in quest.decisions:
            quest.decisions.append("optional-cache")
        state.add_message(
            f"Optional lead completed for {QUESTS[state.active_region_id]['title']}.",
            priority=3,
        )


def mark_secondary_lead(state: GameState) -> bool:
    region = state.active_region_id
    containers = state.regions[region].containers
    primary = QUESTS[region]["cache"]
    candidate = next(
        container for container in containers
        if container.id != primary and not container.opened
    )
    return mark_treasure(
        state, region, candidate.id,
        f"A named local worker marks {candidate.name} at {candidate.position.x},{candidate.position.y}, level {candidate.position.z:+d}.",
    )


def mark_elevated_lead(state: GameState) -> bool:
    if state.location != "region" or state.position.z <= 0:
        return False
    candidate = next(
        (
            container for container in reversed(state.region.containers)
            if not container.opened
            and container.id not in state.treasure_marks[state.active_region_id]
        ),
        None,
    )
    if candidate is None:
        return False
    return mark_treasure(
        state, state.active_region_id, candidate.id,
        f"From height, the form of {candidate.name} is visible near {candidate.position.x},{candidate.position.y}, level {candidate.position.z:+d}.",
    )


def regional_resolution_options(state: GameState) -> tuple[tuple[str, str, str, bool, str], ...]:
    rows = list(QUESTS[state.active_region_id]["final"])
    quest = state.questlines[state.active_region_id]
    if state.active_region_id == "greywash":
        available = quest.optional_done or state.region.changes.get("tide_held", False)
        rows[1] = (*rows[1][:3], bool(available), rows[1][4])
    elif state.active_region_id == "whitecairn":
        available = quest.optional_done or state.region.changes.get("quarry_braced", False)
        rows[1] = (*rows[1][:3], bool(available), rows[1][4])
    elif state.active_region_id not in REGION_IDS:
        available = quest.optional_done or state.region.changes.get("environment_control_used", False)
        rows[1] = (*rows[1][:3], bool(available), rows[1][4])
    return tuple(rows)


def _grant_passive_reward(state: GameState, passive: str) -> None:
    item = create_item(
        state, f"passive:{passive}",
        f"{QUESTS[state.active_region_id]['title']} consequence",
    )
    if not auto_place(
        state, item.id, "pack", owner_id=state.active_courier_id
    ):
        item.location = "ground"
        item.region_id = state.active_region_id
        item.ground_position = state.position
    record_acquisition(state, item)


def _contact_changes(state: GameState, primary: int, secondary: int) -> None:
    contacts = state.contacts[state.active_region_id]
    contacts[0].disposition = max(-3, min(3, contacts[0].disposition + primary))
    if len(contacts) > 1:
        contacts[1].disposition = max(-3, min(3, contacts[1].disposition + secondary))


def resolve_regional_quest(state: GameState, choice: str) -> tuple[bool, str]:
    region_id = state.active_region_id
    quest = state.questlines[region_id]
    option = next(
        (row for row in regional_resolution_options(state) if row[0] == choice),
        None,
    )
    if quest.stage != 2 or quest.status != "resolution":
        return False, "This regional decision is not ready."
    if option is None:
        return False, "That is not a regional resolution."
    if not option[3]:
        return False, option[4]
    region = state.region
    market = state.market[region.objective_commodity]
    if region_id == "hearthford" and choice == "l":
        region.changes["mill_public_compact"] = True
        region.tile_changes["68,24,0"] = "/"
        market.stock += 2
        market.demand = max(0, market.demand - 2)
        _contact_changes(state, 1, 2)
        consequence = "Public sluice access keeps the mill door and wetland bypass open on later visits."
    elif region_id == "hearthford":
        region.changes["mill_reeve_charter"] = True
        state.trade_credit += 2
        market.stock += 1
        market.demand = max(0, market.demand - 1)
        _contact_changes(state, 2, -1)
        consequence = "The reeve funds fast repairs, but the millwrights remember the private claim."
    elif region_id == "greywash" and choice == "s":
        region.changes["safe_salt_delay"] = True
        region.process_thresholds = [threshold + 8 for threshold in region.process_thresholds]
        market.stock += 1
        _contact_changes(state, 1, 1)
        consequence = "Later salt work waits for the safe road; profit is modest and the tide window is longer."
    elif region_id == "greywash":
        region.changes["ebb_salvage_claim"] = True
        state.trade_credit += 3
        market.stock += 2
        _contact_changes(state, 2, -1)
        consequence = "Jomon secures the exposed salvage, while the registrar records a disputed risk."
    elif region_id == "greenwold" and choice == "m":
        region.changes["medicine_coppice_saved"] = True
        state.smoke.clear()
        for threat in state.threats:
            if "smoke" in threat.name:
                threat.status = "retreated"
        market.demand = max(0, market.demand - 2)
        _contact_changes(state, 1, 2)
        consequence = "A narrow managed burn preserves medicine growth and removes smoke-tenders from later patrols."
    elif region_id == "greenwold":
        region.changes["charcoal_burn_expanded"] = True
        market.stock += 3
        state.regional_markets[region_id]["timber"].demand += 1
        _contact_changes(state, 2, -1)
        consequence = "Expanded charcoal output eases fuel demand but leaves a smokier patrol ecology."
    elif region_id == "whitecairn" and choice == "w":
        region.changes["honest_bell"] = True
        for threat in state.threats:
            if threat.role == "lookout":
                threat.status = "retreated"
        market.demand = max(0, market.demand - 2)
        _contact_changes(state, 1, 2)
        consequence = "The honest bell closes unstable work and removes the private alarm from later approaches."
    elif region_id == "whitecairn":
        region.changes["false_toll_exposed"] = True
        state.trade_credit += 3
        market.stock += 2
        _contact_changes(state, 2, -1)
        consequence = "The false toll is exposed; carriers reopen the ridge while quarry claims remain contested."
    else:
        from .frontiers import settle_frontier_claim

        consequence = settle_frontier_claim(state, choice)
    quest.stage, quest.status, quest.consequence = 3, "completed", consequence
    quest.decisions.append(f"ending:{choice}")
    from .regional_history import account_for

    account = account_for(state)
    if account:
        account.trust = min(3, account.trust + 1)
        account.confidence = min(3, account.confidence + 1)
    _grant_passive_reward(state, QUEST_REWARDS[region_id])
    memory = f"{state.courier.name} completed {QUESTS[region_id]['title']}: {consequence}"
    state.remember(memory)
    for contact in state.contacts[region_id]:
        contact.memories.append(memory)
        del contact.memories[:-8]
    maybe_unlock_arc(state)
    return True, consequence


def maybe_unlock_arc(state: GameState) -> bool:
    completed = sum(quest.status == "completed" for quest in state.questlines.values())
    if completed < 2 or state.cross_region_arc.status != "locked":
        return False
    state.cross_region_arc.status = "available"
    state.remember(
        "Two regional working settlements now trust Jomon enough to compare their route accounts."
    )
    state.add_message(
        f"Cross-region arc available: {ARC_TITLE}. Speak with a trusted primary contact.",
        priority=3,
    )
    return True


def arc_available_here(state: GameState) -> bool:
    arc = state.cross_region_arc
    if arc.status == "available":
        return state.questlines[state.active_region_id].status == "completed"
    return arc.status == "active" and ARC_REGIONS.get(arc.stage) == state.active_region_id


def arc_options(state: GameState) -> tuple[tuple[str, str, str, bool, str], ...]:
    stage = state.cross_region_arc.stage
    if stage == 0:
        return (
            ("o", "Open the compared accounts to every named settlement", "commitment", True, ""),
            ("q", "Carry the accounts quietly under Jomon's surety", "danger", True, ""),
        )
    if stage == 1:
        return (
            ("s", "Take Greywash's witnessed salt measure", "commitment", True, ""),
            ("w", "Take the disputed wreck measure as leverage", "danger", state.questlines["greywash"].optional_done, "open Greywash's named wreck locker"),
        )
    if stage == 2:
        return (
            ("m", "Bind the medicine coppice need into the account", "commitment", True, ""),
            ("f", "Bind the larger fuel contract into the account", "danger", True, ""),
        )
    if stage == 3:
        return (
            ("b", "Carry the honest bell record down the ridge", "commitment", True, ""),
            ("t", "Carry the exposed toll claim instead", "danger", True, ""),
        )
    if stage == 4:
        return (
            ("c", "Found an open carriers' compact", "commitment", True, ""),
            ("h", "Keep the marks under Jomon household surety", "ordinary", True, ""),
            ("l", "Return each mark to local control", "refusal", True, ""),
        )
    return ()


def resolve_arc_choice(state: GameState, choice: str) -> tuple[bool, str]:
    arc = state.cross_region_arc
    if not arc_available_here(state):
        return False, "The compared regional account is not available here."
    option = next((row for row in arc_options(state) if row[0] == choice), None)
    if option is None:
        return False, "That is not an available chapter decision."
    if not option[3]:
        return False, option[4]
    arc.decisions.append(f"chapter-{arc.stage}:{choice}")
    if arc.stage == 0:
        arc.status, arc.stage = "active", 1
        arc.branch = "open" if choice == "o" else "surety"
        state.objective_evidence.append("four-region:bound-working-marks")
        message = "Jomon binds the first compared accounts; Greywash holds the next salt measure."
    elif arc.stage < 4:
        current = arc.stage
        arc.stage += 1
        next_region = ARC_REGIONS[arc.stage]
        message = (
            f"Chapter {current} is witnessed in {state.region.name}; "
            f"the physical account now requires {state.regions[next_region].name}."
        )
    else:
        arc.stage, arc.status = 5, "completed"
        if choice == "c":
            arc.consequence = "Open compact: safer known routes and lower demand, but no single household controls the credit."
            for edge in state.route_edges:
                edge.cargo_risk = max(0, edge.cargo_risk - 1)
            for market in state.regional_markets.values():
                for entry in market.values():
                    entry.demand = max(0, entry.demand - 1)
            state.vessel_changes["route_reputation"] = "open compact"
        elif choice == "h":
            arc.consequence = "Household surety: Jomon gains credit and obligation while regional route risks remain."
            state.trade_credit += 6
            state.vessel_changes["route_reputation"] = "Jomon surety"
        else:
            arc.consequence = "Local marks: contacts gain authority and markets remain distinct, but chart risks do not ease."
            for contacts in state.contacts.values():
                contacts[0].disposition = min(3, contacts[0].disposition + 1)
            state.vessel_changes["route_reputation"] = "local marks"
        message = arc.consequence
        state.remember(f"{ARC_TITLE} ended. {arc.consequence}")
    return True, message


def secondary_service_options(state: GameState) -> tuple[tuple[str, str, str, bool, str], ...]:
    from .regional_history import account_for

    institution = account_for(state)
    injured = bool(state.courier and state.courier.injuries)
    rows = [
        ("c", "Mark a named regional cache", "ordinary", True, ""),
        ("t", "Take local practical instruction", "ordinary", True, ""),
        ("h", "Treat one persistent injury", "commitment", injured, "the courier has no persistent injury"),
    ]
    if institution:
        rows.append(("d", f"Deliver one {institution.dependency} to the working account", "commitment", state.market[institution.dependency].stock < 5, "stores already supplied"))
    from .frontier_elites import claimant_terms
    claimant = claimant_terms(state)
    if claimant:
        rows.append(("s", f"Settle {claimant.name}'s claim: 2 credit", "commitment",
                     state.questlines[state.active_region_id].stage >= 2 and state.trade_credit >= 2,
                     "needs a witnessed material result and two credits"))
    return tuple(rows)


def use_secondary_service(state: GameState, choice: str) -> tuple[bool, str]:
    option = next((row for row in secondary_service_options(state) if row[0] == choice), None)
    if option is None or not option[3]:
        return False, option[4] if option else "That service is unavailable."
    contact = state.contacts[state.active_region_id][1]
    if choice == "s":
        from .frontier_elites import settle_claimant
        return settle_claimant(state)
    if choice == "d":
        from .regional_history import deliver_dependency

        return deliver_dependency(state)
    if choice == "c":
        changed = mark_secondary_lead(state)
        return changed, "The local worker places a persistent named cache mark on Jomon's account."
    if choice == "t":
        technique = {
            "hearthford": "mill hearing",
            "greywash": "shoreline measure",
            "greenwold": "smoke spoor",
            "whitecairn": "bell interval",
            "dunmire": "shoreline measure",
            "rillscar": "bell interval",
            "marlbank": "mill hearing",
            "frostmere": "shoreline measure",
        }[state.active_region_id]
        if technique in state.courier.learned_techniques:
            return False, f"{state.courier.name} already knows {technique}."
        state.courier.learned_techniques.append(technique)
        contact.disposition = min(3, contact.disposition + 1)
        effects = {
            "mill hearing": "control work is quieter; weak supports can be braced without a heavy tool",
            "shoreline measure": "released water no longer adds a crossing action; coastal weather leaves a longer sightline",
            "smoke spoor": "movement through smoke is quiet; smoke leaves five paces of local visibility",
            "bell interval": "warning controls are worked quietly; brace work can be timed without a heavy tool",
        }
        return True, f"{contact.name} teaches {technique}: {effects[technique]}."
    from .regional_history import account_for

    account = account_for(state)
    entrusted_care = bool(account and account.trust >= 2 and account.obligation < 3)
    if state.trade_credit <= 0 and state.support != "field care" and not entrusted_care:
        return False, "Treatment needs one credit or the prepared healer's field care."
    if entrusted_care:
        account.obligation += 1
    elif state.support != "field care":
        state.trade_credit -= 1
    location = sorted(state.courier.injuries)[0]
    state.courier.injuries.pop(location)
    state.courier.health = min(state.courier.max_health, state.courier.health + 3)
    state.courier.injury = next(iter(state.courier.injuries.values()), "treated soreness")
    contact.memories.append(f"Treated {state.courier.name}'s {location} injury for a recorded obligation.")
    state.region.changes["care_obligation_settled"] = True
    return True, f"{contact.name} treats the {location} injury; time and a finite obligation remain consequential."


def quest_reachability_audit(sample_count: int = 25) -> dict[str, object]:
    """Inspect authored quest positions across deterministic generated worlds."""
    from collections import Counter

    from .regions import region_reachable
    from .state import create_world

    failures: list[str] = []
    geography: Counter[str] = Counter()
    cache_counts: Counter[str] = Counter()
    for index in range(sample_count):
        state = create_world(f"quest-audit-{index:03d}")
        for region_id, region in state.regions.items():
            reachable = region_reachable(region)
            required = {
                region.landmarks["landing"], region.landmarks["contact"],
                region.landmarks["second_contact"], region.landmarks["objective"],
            }
            cache = next(
                container for container in region.containers
                if container.id == QUESTS[region_id]["cache"]
            )
            required.add(cache.position)
            if not required <= reachable:
                failures.append(f"{index}:{region_id}:required-position")
            if len(QUESTS[region_id]["final"]) != 2:
                failures.append(f"{index}:{region_id}:ending-count")
            geography[f"{region_id}:{region.geography_signature}"] += 1
            cache_counts[region_id] += len(region.containers)
    return {
        "samples": sample_count,
        "regions_checked": sample_count * len(REGION_IDS),
        "unreachable_or_invalid": failures,
        "unique_geographies": len(geography),
        "container_totals": dict(sorted(cache_counts.items())),
        "regional_endings": len(REGION_IDS) * 2,
        "cross_region_endings": 3,
    }
