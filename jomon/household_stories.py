"""Two optional late household developments and one all-region capstone."""

from __future__ import annotations

from dataclasses import dataclass

from .state import GameState


@dataclass(frozen=True)
class HouseholdStory:
    id: str
    name: str
    requirement: str
    premise: str


STORIES = (
    HouseholdStory("empty-watch", "The watch after an empty place", "four returned expeditions", "The household must name how a missing, injured, succeeded, or simply exhausted watch is carried by the living."),
    HouseholdStory("repair-share", "The repair share", "four voyages and a damaged or previously repaired Jomon", "The hull account has become personal: timber, injuries, credit and remembered work no longer divide neatly."),
    HouseholdStory("eight-waters", "Eight waters under one roof", "both household developments and outcomes in four regions", "Jomon's household compares four changed places, institutional accounts and its own scars before naming what kind of carrier it has become."),
)

BY_ID = {row.id: row for row in STORIES}


def _key(story_id: str, part: str) -> str:
    return f"household-story:{story_id}:{part}"


def outcome_regions(state: GameState) -> list[str]:
    changed = []
    for region_id, region in state.regions.items():
        site = any(key.startswith("micro-site:resolved:") and value for key, value in region.changes.items())
        quest = state.questlines.get(region_id)
        if site or region.changes.get("aftermath_configuration") or (quest and quest.status == "completed"):
            changed.append(region_id)
    return sorted(changed)


def eligibility(state: GameState, story_id: str) -> tuple[bool, str]:
    if story_id == "empty-watch":
        return (state.returned_expeditions >= 4, "four returned expeditions")
    if story_id == "repair-share":
        damaged = state.vessel_integrity < 10 or bool(state.vessel_changes.get("hull_repairs")) or any(key.startswith("deck_scar:") for key in state.vessel_changes)
        return (state.travel_count >= 4 and damaged, "four voyages and a damaged, repaired, or scarred Jomon")
    first = state.vessel_changes.get(_key("empty-watch", "status")) == "completed"
    second = state.vessel_changes.get(_key("repair-share", "status")) == "completed"
    return (first and second and len(outcome_regions(state)) >= 4, "both developments and at least four changed regions")


def station_choices(state: GameState) -> list[tuple[str, str, str, bool, str]]:
    result = []
    for index, story in enumerate(STORIES, 1):
        status = str(state.vessel_changes.get(_key(story.id, "status"), "unopened"))
        eligible, reason = eligibility(state, story.id)
        result.append((str(index), f"{story.name} — {status}", "ordinary" if status == "completed" else "commitment", eligible or status in {"active", "completed"}, reason))
    return result


def story_choices(state: GameState, story_id: str) -> list[tuple[str, str, str, bool, str]]:
    status = state.vessel_changes.get(_key(story_id, "status"), "unopened")
    if status == "completed":
        return []
    if status == "unopened":
        eligible, reason = eligibility(state, story_id)
        return [("O", "Open this household account (one action)", "commitment", eligible, reason), ("D", "Defer without closing it", "ordinary", True, "")]
    if story_id == "empty-watch":
        return [("W", "Share the watch by witnessed relationship", "commitment", True, ""), ("P", "Set a practical rotating watch", "commitment", True, "")]
    if story_id == "repair-share":
        timber = state.vessel_cargo.get("timber")
        return [("W", "Put the repair into common obligation", "commitment", True, ""), ("P", "Seat one physical timber repair", "commitment", bool(timber and timber.quantity), "one timber lot")]
    return [("C", "Name Jomon a common carrier", "commitment", True, ""), ("H", "Keep the household answer first", "commitment", True, ""), ("R", "Retain separate cautious accounts", "commitment", True, "")]


def story_lines(state: GameState, story_id: str) -> list[str]:
    story = BY_ID[story_id]
    status = str(state.vessel_changes.get(_key(story_id, "status"), "unopened"))
    branch = state.vessel_changes.get(_key(story_id, "branch"))
    dead = [person.name for person in state.household if not person.alive]
    lines = [f"FACT — status: {status}.", story.premise, f"Gate: {story.requirement}."]
    if story_id == "empty-watch":
        lines.append("Succession record: " + (", ".join(dead) + " are dead; no one is restored." if dead else "no household death is required; injury, fatigue and prior returns still shape the watch."))
    elif story_id == "repair-share":
        lines.append(f"Jomon integrity {state.vessel_integrity}/10; voyages {state.travel_count}; timber lots {state.vessel_cargo.get('timber').quantity if state.vessel_cargo.get('timber') else 0}.")
    else:
        names = [state.regions[rid].name for rid in outcome_regions(state)]
        lines.append("Changed regions: " + (", ".join(names) or "fewer than four recorded"))
    if branch:
        lines += [f"Outcome branch: {branch}.", str(state.vessel_changes.get(_key(story_id, "outcome"), "The household keeps the result."))]
    elif status == "active":
        lines.append("Choose the disclosed household answer; each costs one action and persists.")
    else:
        lines.append("Opening costs one action. Deferral costs none and does not close the story.")
    return lines


def resolve(state: GameState, story_id: str, choice: str) -> tuple[bool, str, int]:
    story = BY_ID[story_id]
    status = str(state.vessel_changes.get(_key(story_id, "status"), "unopened"))
    if choice == "d":
        return False, f"{story.name} remains available without advancing time.", 0
    if status == "unopened":
        eligible, reason = eligibility(state, story_id)
        if choice != "o" or not eligible:
            return False, f"{story.name} needs {reason}.", 0
        state.vessel_changes[_key(story_id, "status")] = "active"
        state.remember(f"The household opened {story.name} at Jomon's common deck.")
        return True, f"{story.name} is opened; return to the common deck to name its answer.", 1
    if status != "active":
        return False, f"{story.name} is already complete.", 0
    living = [person for person in state.household if person.alive]
    if not living:
        return False, "No living household adult can settle this account.", 0
    if story_id == "empty-watch" and choice in {"w", "p"}:
        branch = "witnessed shared watch" if choice == "w" else "practical rotating watch"
        state.vessel_changes["watch_order"] = "shared" if choice == "w" else "rotating"
        if len(living) > 1:
            first, second = living[:2]
            first.relationships[second.id] = min(3, first.relationships.get(second.id, 0) + 1)
            second.relationships[first.id] = min(3, second.relationships.get(first.id, 0) + 1)
        dead = [person.name for person in state.household if not person.alive]
        outcome = ("The living name " + ", ".join(dead) + " without restoring them; the watch passes by witnessed consent." if dead else "Fatigue and injury are named before a shared watch is set; no death is invented.")
    elif story_id == "repair-share" and choice in {"w", "p"}:
        if choice == "p":
            timber = state.vessel_cargo.get("timber")
            if not timber or not timber.quantity:
                return False, "A physical repair needs one timber lot.", 0
            timber.quantity -= 1
            if not timber.quantity:
                del state.vessel_cargo["timber"]
            state.vessel_integrity = min(10, state.vessel_integrity + 2)
            branch, outcome = "physical repair share", "One timber lot seats two integrity; the consumed lot does not return."
        else:
            branch, outcome = "common repair obligation", "The household keeps the scar and shares its next repair duty."
            for account in state.institutions.values():
                if account.obligation:
                    account.obligation -= 1
                    break
        for person in living:
            person.memories.append(f"{story.name}: {outcome}")
            del person.memories[:-8]
    elif story_id == "eight-waters" and choice in {"c", "h", "r"}:
        branch = {"c": "common carrier", "h": "household-first carrier", "r": "separate cautious accounts"}[choice]
        regions = outcome_regions(state)
        if choice == "c":
            for account in state.institutions.values():
                account.trust = min(3, account.trust + 1)
            outcome = f"Four or more changed places recognise Jomon's shared working mark: {', '.join(regions)}."
        elif choice == "h":
            for first in living:
                for second in living:
                    if first.id != second.id:
                        first.relationships[second.id] = min(3, first.relationships.get(second.id, 0) + 1)
            state.vessel_integrity = min(10, state.vessel_integrity + 1)
            outcome = "Household ties and one careful integrity point take precedence over wider claims."
        else:
            for account in state.institutions.values():
                account.obligation = max(0, account.obligation - 1)
            outcome = "Separate accounts reduce obligations but grant no universal trust."
        state.vessel_changes["campaign:all-region-capstone"] = branch
    else:
        return False, "That answer does not belong to this household account.", 0
    state.vessel_changes[_key(story_id, "status")] = "completed"
    state.vessel_changes[_key(story_id, "branch")] = branch
    state.vessel_changes[_key(story_id, "outcome")] = outcome
    record = f"{story.name}: {branch}. {outcome}"
    state.chronicle.append(record)
    del state.chronicle[:-24]
    state.remember(record)
    return True, record, 1


def validate_stories() -> None:
    if len(STORIES) != 3 or len(BY_ID) != 3 or STORIES[-1].id != "eight-waters":
        raise ValueError("two developments and one all-region capstone are required")


validate_stories()
