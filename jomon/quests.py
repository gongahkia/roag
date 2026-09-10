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

ADDITIONAL_ARCS = {
    "banks": {
        "title": "Banks That Hold",
        "start": "hearthford",
        "regions": {1: "dunmire", 2: "marlbank", 3: "hearthford"},
        "requires": ("hearthford", "dunmire", "marlbank"),
        "evidence": "bound bank roll",
    },
    "soundings": {
        "title": "Soundings and Spans",
        "start": "greywash",
        "regions": {1: "frostmere", 2: "rillscar", 3: "greywash"},
        "requires": ("greywash", "frostmere", "rillscar"),
        "evidence": "sounding chain account",
    },
    "repairs": {
        "title": "Scars Kept in Use",
        "start": "greenwold",
        "regions": {1: "hearthford", 2: "rillscar", 3: "greenwold"},
        "requires": ("greenwold", "hearthford", "rillscar"),
        "requires_aftermath": True,
        "evidence": "scar repair folio",
        "public_openings": ("p",),
        "environment_choices": ("r", "s"),
    },
    "refuges": {
        "title": "Refuges at Low Water",
        "start": "greywash",
        "regions": {1: "dunmire", 2: "frostmere", 3: "greywash"},
        "requires": ("greywash", "dunmire", "frostmere"),
        "requires_aftermath": True,
        "evidence": "ebb refuge chart",
        "public_openings": ("l",),
        "environment_choices": ("i", "w"),
    },
}


def initialise_quests(state: GameState) -> None:
    from .worklines import initialise
    initialise(state)
    state.questlines = {
        region_id: state.questlines.get(region_id, QuestProgress())
        for region_id in state.regions
    }
    state.treasure_marks = {
        region_id: list(state.treasure_marks.get(region_id, []))
        for region_id in state.regions
    }
    state.cross_region_arcs = {
        arc_id: state.cross_region_arcs.get(arc_id, QuestProgress(status="locked"))
        for arc_id in ADDITIONAL_ARCS
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
    from .worklines import WORKLINES
    if state.active_region_id in WORKLINES and container_id == WORKLINES[state.active_region_id][1]:
        state.worklines[state.active_region_id].optional_done = True
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
        (container for container in containers
         if container.id != primary and not container.opened
         and container.id not in state.treasure_marks[region]),
        None,
    )
    if candidate is None:
        return False
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
    from .aftermath import record_resolution_clock

    record_resolution_clock(state, region_id)
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
    changed = False
    if completed >= 2 and state.cross_region_arc.status == "locked":
        state.cross_region_arc.status = "available"
        state.remember(
            "Two regional working settlements now trust Jomon enough to compare their route accounts."
        )
        state.add_message(
            f"Cross-region arc available: {ARC_TITLE}. Speak with a trusted primary contact.",
            priority=3,
        )
        changed = True
    for arc_id, definition in ADDITIONAL_ARCS.items():
        arc = state.cross_region_arcs[arc_id]
        source = (
            state.aftermath_quests
            if definition.get("requires_aftermath") else state.questlines
        )
        if arc.status == "locked" and all(
            source.get(region_id, QuestProgress(status="locked")).status == "completed"
            for region_id in definition["requires"]
        ):
            arc.status = "available"
            state.remember(f"{definition['title']} is available through compared material consequences.")
            state.add_message(
                f"Cross-region arc available: {definition['title']}. Begin with {state.regions[definition['start']].name}.",
                priority=3,
            )
            changed = True
    return changed


def current_arc_key(state: GameState) -> str | None:
    if state.cross_region_arc.status == "active" and ARC_REGIONS.get(state.cross_region_arc.stage) == state.active_region_id:
        return "marks"
    for arc_id, definition in ADDITIONAL_ARCS.items():
        arc = state.cross_region_arcs[arc_id]
        if arc.status == "active" and definition["regions"].get(arc.stage) == state.active_region_id:
            return arc_id
    for arc_id, definition in ADDITIONAL_ARCS.items():
        if state.cross_region_arcs[arc_id].status == "available" and definition["start"] == state.active_region_id:
            return arc_id
    if state.cross_region_arc.status == "available" and state.questlines[state.active_region_id].status == "completed":
        return "marks"
    return None


def current_arc(state: GameState) -> QuestProgress | None:
    key = current_arc_key(state)
    if key == "marks":
        return state.cross_region_arc
    return state.cross_region_arcs.get(key) if key else None


def arc_title(state: GameState) -> str:
    key = current_arc_key(state)
    return ARC_TITLE if key == "marks" else str(ADDITIONAL_ARCS[key]["title"]) if key else "Compared Accounts"


def arc_next_region(state: GameState) -> str | None:
    key = current_arc_key(state)
    arc = current_arc(state)
    if not key or not arc:
        return None
    return ARC_REGIONS.get(arc.stage) if key == "marks" else ADDITIONAL_ARCS[key]["regions"].get(arc.stage)


def arc_available_here(state: GameState) -> bool:
    return current_arc_key(state) is not None


def arc_options(state: GameState) -> tuple[tuple[str, str, str, bool, str], ...]:
    key = current_arc_key(state)
    if key and key != "marks":
        return _additional_arc_options(state, key)
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


def _additional_arc_options(state: GameState, arc_id: str) -> tuple[tuple[str, str, str, bool, str], ...]:
    arc = state.cross_region_arcs[arc_id]
    evidence = ADDITIONAL_ARCS[arc_id]["evidence"]
    has_record = any(
        item.kind == f"consumable:{evidence}"
        and item.owner_id == state.active_courier_id and item.location == "pack"
        for item in state.items
    )
    can_settle = has_record or state.trade_credit >= 2
    if arc_id == "banks":
        return {
            0: (
                ("p", "Bind the three flood accounts as a public bank record", "commitment", True, ""),
                ("f", "Carry them as a fuel-and-repair surety", "danger", True, ""),
            ),
            1: (
                ("r", "Open Dunmire's drain and record the safe water line", "commitment", True, ""),
                ("g", "Put the armed drying-bank claim on notice", "danger", True, ""),
            ),
            2: (
                ("s", "Give Marlbank's next release to seed terraces", "commitment", True, ""),
                ("k", "Hold kiln heat and accept a tighter flood margin", "danger", True, ""),
            ),
            3: (
                ("o", "Publish a shared bank-and-sluice compact", "commitment", can_settle, "recover the bound bank roll or fund a two-credit witnessed copy"),
                ("b", "Let Jomon bond repair fuel against the banks", "danger", can_settle, "recover the bound bank roll or fund a two-credit witnessed copy"),
            ),
        }.get(arc.stage, ())
    if arc_id == "soundings":
        return {
            0: (
                ("l", "Carry a sheltered-channel sounding chain", "commitment", True, ""),
                ("d", "Compare direct cuts and rapid bridge spans", "danger", True, ""),
            ),
            1: (
                ("i", "Mark Frostmere's lee route through the ice", "commitment", True, ""),
                ("n", "Challenge the armed fast-cut net claim", "danger", True, ""),
            ),
            2: (
                ("w", "Carry Rillscar's warning span and brace account", "commitment", True, ""),
                ("q", "Carry the quarry guard's faster private span", "danger", True, ""),
            ),
            3: (
                ("s", "Publish sheltered winter soundings and honest spans", "commitment", can_settle, "recover the sounding chain account or fund a two-credit witnessed copy"),
                ("r", "Open a rapid freight cut under Jomon's surety", "danger", can_settle, "recover the sounding chain account or fund a two-credit witnessed copy"),
            ),
        }.get(arc.stage, ())
    if arc_id == "repairs":
        return {
            0: (
                ("p", "Publish three aftermath scars as common repair duties", "commitment", True, ""),
                ("j", "Carry the scars under Jomon's salvage surety", "danger", True, ""),
            ),
            1: (
                ("r", "Reinforce Hearthford's public flood-mark circuit", "commitment", True, ""),
                ("a", "Put the armed wheel-timber claim on notice", "danger", True, ""),
            ),
            2: (
                ("s", "Seat Rillscar's shared switchback braces", "commitment", True, ""),
                ("q", "Carry the convoy counterweight as private leverage", "danger", True, ""),
            ),
            3: (
                ("m", "Maintain the three scars as common working approaches", "commitment", can_settle, "recover the scar repair folio or replace it after physical loss"),
                ("j", "Give Jomon first salvage against future failures", "danger", can_settle, "recover the scar repair folio or replace it after physical loss"),
            ),
        }.get(arc.stage, ())
    return {
        0: (
            ("l", "Publish low-water refuges on a common chart", "commitment", True, ""),
            ("c", "Carry refuge access as a household channel claim", "danger", True, ""),
        ),
        1: (
            ("i", "Raise Dunmire's inhabited peat walk", "commitment", True, ""),
            ("g", "Confront the armed submerged-fuel claim", "danger", True, ""),
        ),
        2: (
            ("w", "Restake Frostmere's sheltered thaw channel", "commitment", True, ""),
            ("n", "Carry the broken-ice net claim as a fast route", "danger", True, ""),
        ),
        3: (
            ("p", "Publish free storm and thaw refuges", "commitment", can_settle, "recover the ebb refuge chart or replace it after physical loss"),
            ("c", "Let Jomon collect surety on the marked refuge cuts", "danger", can_settle, "recover the ebb refuge chart or replace it after physical loss"),
        ),
    }.get(arc.stage, ())


def _give_arc_record(state: GameState, arc_id: str) -> None:
    evidence = str(ADDITIONAL_ARCS[arc_id]["evidence"])
    item = create_item(state, f"consumable:{evidence}", f"{ADDITIONAL_ARCS[arc_id]['title']} witnessed opening")
    if not auto_place(state, item.id, "pack", owner_id=state.active_courier_id):
        item.location, item.region_id, item.ground_position = "ground", state.active_region_id, state.position
    marker = f"arc:{arc_id}:{evidence}"
    if marker not in state.objective_evidence:
        state.objective_evidence.append(marker)


def _settle_arc_record(state: GameState, arc_id: str) -> bool:
    from .inventory import consume_carried

    evidence = str(ADDITIONAL_ARCS[arc_id]["evidence"])
    if consume_carried(state, f"consumable:{evidence}"):
        return True
    record = next(
        (item for item in state.items if item.kind == f"consumable:{evidence}"),
        None,
    )
    if record and record.location in {"lost", "destroyed"} and state.trade_credit >= 2:
        state.trade_credit -= 2
        state.remember(f"Two credits funded a witnessed replacement for the lost {evidence}.")
        return True
    return False


def _wake_arc_opposition(state: GameState, duty: str) -> None:
    actor = next((actor for actor in state.threats if actor.profile != "animal" and actor.status in {"watching", "dormant"}), None)
    if not actor:
        return
    actor.status = "engaged"
    actor.duty = duty
    actor.intent = f"warns before holding the route for {duty}"
    actor.objective_position = state.region.landmarks.get("control", state.position)


def _resolve_additional_arc(state: GameState, arc_id: str, choice: str) -> tuple[bool, str]:
    arc = state.cross_region_arcs[arc_id]
    option = next((row for row in _additional_arc_options(state, arc_id) if row[0] == choice), None)
    if option is None:
        return False, "That is not an available chapter decision."
    if not option[3]:
        return False, option[4]
    definition = ADDITIONAL_ARCS[arc_id]
    arc.decisions.append(f"chapter-{arc.stage}:{choice}")
    if arc.stage == 0:
        arc.status, arc.stage = "active", 1
        arc.branch = (
            "public" if choice in definition.get("public_openings", {"p", "l"})
            else "surety"
        )
        _give_arc_record(state, arc_id)
        next_region = definition["regions"][1]
        message = f"{definition['title']} begins with a physical witnessed record; {state.regions[next_region].name} holds the next account."
    elif arc.stage < 3:
        if choice in definition.get("environment_choices", {"r", "s", "i", "w"}):
            state.region.changes[f"arc:{arc_id}:environmental"] = True
            state.region.changes["environment_control_used"] = True
        else:
            state.region.changes[f"arc:{arc_id}:armed_claim"] = True
            _wake_arc_opposition(state, f"{definition['title']} armed claim")
        current = arc.stage
        arc.stage += 1
        next_region = definition["regions"][arc.stage]
        message = f"Chapter {current} is witnessed at {state.region.name}; the record now names {state.regions[next_region].name}."
    else:
        if not _settle_arc_record(state, arc_id):
            return False, "Recover the physical record or fund a two-credit witnessed copy."
        arc.stage, arc.status = 4, "completed"
        involved = set(definition["requires"])
        if arc_id == "banks" and choice == "o":
            arc.consequence = "Shared banks: flood routes and food demand ease; institutions retain local control."
            for edge in state.route_edges:
                if {edge.first, edge.second} & involved:
                    edge.weather_exposure = max(0, edge.weather_exposure - 1)
                    edge.cargo_risk = max(0, edge.cargo_risk - 1)
            for region_id in involved:
                state.regions[region_id].changes["shared_bank_compact"] = True
                state.regional_markets[region_id]["grain"].demand = max(0, state.regional_markets[region_id]["grain"].demand - 1)
        elif arc_id == "banks":
            arc.consequence = "Bonded banks: repair stocks and household credit rise, while private fuel obligations remain."
            state.trade_credit += 6
            for region_id in involved:
                state.regions[region_id].changes["bonded_bank_repairs"] = True
                state.institutions[f"work:{region_id}"].obligation = min(9, state.institutions[f"work:{region_id}"].obligation + 1)
        elif arc_id == "soundings" and choice == "s":
            arc.consequence = "Sheltered soundings: winter exposure falls and the marked lee routes remain public."
            for edge in state.route_edges:
                if {edge.first, edge.second} & involved:
                    edge.weather_exposure = max(0, edge.weather_exposure - 1)
                    edge.closed_seasons = [season for season in edge.closed_seasons if season != "winter"]
            for region_id in involved:
                state.regions[region_id].changes["sheltered_soundings"] = True
        elif arc_id == "soundings":
            arc.consequence = "Rapid soundings: freight time falls and Jomon gains credit, but exposed cargo risk rises."
            state.trade_credit += 5
            for edge in state.route_edges:
                if {edge.first, edge.second} & involved:
                    edge.travel_time = max(1, edge.travel_time - 1)
                    edge.cargo_risk = min(3, edge.cargo_risk + 1)
            for region_id in involved:
                state.regions[region_id].changes["rapid_sounding_surety"] = True
        elif arc_id == "repairs" and choice == "m":
            arc.consequence = "Common repairs: worked scars stay braced and connected cargo approaches become safer."
            for region_id in involved:
                state.regions[region_id].changes["common_aftermath_repairs"] = True
                state.institutions[f"work:{region_id}"].confidence = min(
                    3, state.institutions[f"work:{region_id}"].confidence + 1
                )
                for cell in state.regions[region_id].materials.values():
                    if cell.collapse_due or cell.support < 2:
                        cell.support = min(3, cell.support + 1)
                        cell.collapse_due = 0
            for edge in state.route_edges:
                if {edge.first, edge.second} & involved:
                    edge.cargo_risk = max(0, edge.cargo_risk - 1)
        elif arc_id == "repairs":
            arc.consequence = "Salvage surety: repair stock and household credit rise, but each work account records an obligation."
            state.trade_credit += 5
            for region_id in involved:
                state.regions[region_id].changes["jomon_salvage_surety"] = True
                account = state.institutions[f"work:{region_id}"]
                account.obligation = min(9, account.obligation + 1)
                market = state.regional_markets[region_id][account.production]
                market.stock = min(10, market.stock + 1)
        elif choice == "p":
            arc.consequence = "Public refuges: storm and thaw shelter opens without toll, easing weather exposure and local trust."
            for edge in state.route_edges:
                if {edge.first, edge.second} & involved:
                    edge.weather_exposure = max(0, edge.weather_exposure - 1)
                    edge.closed_seasons = [
                        season for season in edge.closed_seasons if season != "winter"
                    ]
            for region_id in involved:
                state.regions[region_id].changes["public_low_water_refuges"] = True
                state.contacts[region_id][0].disposition = min(
                    3, state.contacts[region_id][0].disposition + 1
                )
        else:
            arc.consequence = "Channel surety: marked refuge cuts shorten freight passage and pay Jomon, while exposed cargo risk rises."
            state.trade_credit += 5
            for edge in state.route_edges:
                if {edge.first, edge.second} & involved:
                    edge.travel_time = max(1, edge.travel_time - 1)
                    edge.cargo_risk = min(3, edge.cargo_risk + 1)
            for region_id in involved:
                state.regions[region_id].changes["jomon_refuge_surety"] = True
        state.vessel_changes[f"arc:{arc_id}:outcome"] = choice
        for region_id in involved:
            account = state.institutions[f"work:{region_id}"]
            account.witnessed_acts.append(f"{definition['title']} settled: {arc.consequence}")
            del account.witnessed_acts[:-8]
        message = arc.consequence
        if arc_id in {"repairs", "refuges"}:
            from .arc_relics import grant_arc_relic

            message += grant_arc_relic(state, arc_id, choice)
        state.remember(f"{definition['title']} ended. {arc.consequence}")
    return True, message


def resolve_arc_choice(state: GameState, choice: str) -> tuple[bool, str]:
    key = current_arc_key(state)
    if key and key != "marks":
        return _resolve_additional_arc(state, key, choice)
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


def secondary_service_options(
    state: GameState, contact_id: str | None = None
) -> tuple[tuple[str, str, str, bool, str], ...]:
    from .regional_history import (
        account_for, network_institution_for_contact, network_service_options,
    )

    if contact_id and network_institution_for_contact(state, contact_id):
        return network_service_options(state, contact_id)

    institution = account_for(state)
    injured = bool(state.courier and state.courier.injuries)
    rows = [
        ("c", "Mark a named regional cache", "ordinary", True, ""),
        ("t", "Take local practical instruction", "ordinary", True, ""),
        ("h", "Treat one persistent injury", "commitment", injured, "the courier has no persistent injury"),
    ]
    from .worklines import WORKLINES
    if state.active_region_id in WORKLINES:
        rows.append(("w", "Discuss the second local undertaking", "ordinary", True, ""))
    if institution:
        rows.append(("d", f"Deliver one {institution.dependency} to the working account", "commitment", state.market[institution.dependency].stock < 5, "stores already supplied"))
    from .aftermath import contracts_for

    contracts = contracts_for(state)
    if contracts:
        open_count = sum(
            contract.status not in {"completed", "failed"} for contract in contracts
        )
        rows.append((
            "a", f"Open aftermath contracts ({open_count} unresolved)",
            "ordinary", True, "",
        ))
    from .frontier_elites import claimant_terms
    claimant = claimant_terms(state)
    if claimant:
        rows.append(("s", f"Settle {claimant.name}'s claim: 2 credit", "commitment",
                     state.questlines[state.active_region_id].stage >= 2 and state.trade_credit >= 2,
                     "needs a witnessed material result and two credits"))
    return tuple(rows)


def use_secondary_service(
    state: GameState, choice: str, contact_id: str | None = None
) -> tuple[bool, str]:
    from .regional_history import network_institution_for_contact

    network = (
        network_institution_for_contact(state, contact_id)
        if contact_id else None
    )
    option = next(
        (row for row in secondary_service_options(state, contact_id) if row[0] == choice),
        None,
    )
    if option is None or not option[3]:
        return False, option[4] if option else "That service is unavailable."
    if network:
        from .regional_history import deliver_network_dependency, open_network_shelter
        from .practices import teach_network_practice

        if choice == "d":
            return deliver_network_dependency(state, contact_id)
        if choice == "c":
            return open_network_shelter(state, contact_id)
        return teach_network_practice(state, contact_id)
    contact = state.contacts[state.active_region_id][1]
    if choice == "s":
        from .frontier_elites import settle_claimant
        return settle_claimant(state)
    if choice == "d":
        from .regional_history import deliver_dependency

        return deliver_dependency(state)
    if choice == "c":
        changed = mark_secondary_lead(state)
        return changed, ("The local worker places a persistent named cache mark on Jomon's account." if changed else "There are no unopened, unmarked local caches left to name.")
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

    from .frontiers import FRONTIERS, ensure_frontier
    from .aftermath import AFTERMATH_LINES
    from .regions import region_reachable
    from .state import create_world
    from .worklines import WORKLINES

    failures: list[str] = []
    geography: Counter[str] = Counter()
    cache_counts: Counter[str] = Counter()
    for index in range(sample_count):
        state = create_world(f"quest-audit-{index:03d}")
        for region_id in FRONTIERS:
            ensure_frontier(state, region_id)
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
        "regions_checked": sample_count * len(QUESTS),
        "unreachable_or_invalid": failures,
        "unique_geographies": len(geography),
        "container_totals": dict(sorted(cache_counts.items())),
        "regional_questlines": len(QUESTS) + len(WORKLINES) + len(AFTERMATH_LINES),
        "regional_endings": (len(QUESTS) + len(WORKLINES) + len(AFTERMATH_LINES)) * 2,
        "cross_region_arcs": 1 + len(ADDITIONAL_ARCS),
        "cross_region_endings": 3 + len(ADDITIONAL_ARCS) * 2,
    }
