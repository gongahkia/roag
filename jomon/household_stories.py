"""Two optional late household developments and one all-region capstone."""

from __future__ import annotations

from dataclasses import dataclass

from .catalog import AFTERMATH_SECTIONS, load_catalog
from .region_presentation import region_display_name
from .state import GameState
from .vessel_presentation import (
    household_story_display_name,
    household_story_premise,
    household_story_requirement,
    vessel_format,
    vessel_text,
)


@dataclass(frozen=True)
class HouseholdStory:
    id: str


STORIES = tuple(HouseholdStory(row["id"]) for row in load_catalog("aftermath.json", AFTERMATH_SECTIONS)["household_stories"])
BY_ID = {row.id: row for row in STORIES}


def _key(story_id: str, part: str) -> str:
    return f"household-story:{story_id}:{part}"


def _status_name(status: str) -> str:
    return vessel_text(f"vessel.story.status.{status}")


def _choice_name(story_id: str, choice: str) -> str:
    return vessel_text(f"vessel.story.choice.{story_id}.{choice}")


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
        return state.returned_expeditions >= 4, household_story_requirement(story_id)
    if story_id == "repair-share":
        damaged = state.vessel_integrity < 10 or bool(state.vessel_changes.get("hull_repairs")) or any(key.startswith("deck_scar:") for key in state.vessel_changes)
        return state.travel_count >= 4 and damaged, household_story_requirement(story_id)
    first = state.vessel_changes.get(_key("empty-watch", "status")) == "completed"
    second = state.vessel_changes.get(_key("repair-share", "status")) == "completed"
    return first and second and len(outcome_regions(state)) >= 4, household_story_requirement(story_id)


def station_choices(state: GameState) -> list[tuple[str, str, str, bool, str]]:
    result = []
    for index, story in enumerate(STORIES, 1):
        status = str(state.vessel_changes.get(_key(story.id, "status"), "unopened"))
        eligible, reason = eligibility(state, story.id)
        result.append((
            str(index),
            vessel_format("vessel.story.station.row", index=index, story=household_story_display_name(story.id), status=_status_name(status)),
            "ordinary" if status == "completed" else "commitment",
            eligible or status in {"active", "completed"},
            reason,
        ))
    return result


def story_choices(state: GameState, story_id: str) -> list[tuple[str, str, str, bool, str]]:
    status = str(state.vessel_changes.get(_key(story_id, "status"), "unopened"))
    if status == "completed":
        return []
    if status == "unopened":
        eligible, reason = eligibility(state, story_id)
        return [
            ("O", _choice_name(story_id, "o"), "commitment", eligible, reason),
            ("D", _choice_name(story_id, "d"), "ordinary", True, ""),
        ]
    if story_id == "empty-watch":
        return [
            ("W", _choice_name(story_id, "w"), "commitment", True, ""),
            ("P", _choice_name(story_id, "p"), "commitment", True, ""),
        ]
    if story_id == "repair-share":
        timber = state.vessel_cargo.get("timber")
        return [
            ("W", _choice_name(story_id, "w"), "commitment", True, ""),
            ("P", _choice_name(story_id, "p"), "commitment", bool(timber and timber.quantity), vessel_text("vessel.story.resolve.timber")),
        ]
    return [
        ("C", _choice_name(story_id, "c"), "commitment", True, ""),
        ("H", _choice_name(story_id, "h"), "commitment", True, ""),
        ("R", _choice_name(story_id, "r"), "commitment", True, ""),
    ]


def story_lines(state: GameState, story_id: str) -> list[str]:
    status = str(state.vessel_changes.get(_key(story_id, "status"), "unopened"))
    branch = state.vessel_changes.get(_key(story_id, "branch"))
    dead = [person.name for person in state.household if not person.alive]
    lines = [
        vessel_format("vessel.story.line.fact", status=_status_name(status)),
        household_story_premise(story_id),
        vessel_format("vessel.story.line.open", requirement=household_story_requirement(story_id)),
    ]
    if story_id == "empty-watch":
        lines.append(
            vessel_format("vessel.story.line.empty_watch.dead", people=", ".join(dead))
            if dead else vessel_text("vessel.story.line.empty_watch.living")
        )
    elif story_id == "repair-share":
        timber = state.vessel_cargo.get("timber")
        lines.append(vessel_format(
            "vessel.story.line.repair_share",
            integrity=state.vessel_integrity,
            voyages=state.travel_count,
            timber=timber.quantity if timber else 0,
        ))
    else:
        regions = [region_display_name(region_id) for region_id in outcome_regions(state)]
        lines.append(vessel_format(
            "vessel.story.line.eight_waters",
            regions=", ".join(regions) if regions else vessel_text("vessel.story.line.eight_waters.none"),
        ))
    if branch:
        lines += [
            vessel_format("vessel.story.line.decision", branch=branch),
            str(state.vessel_changes.get(_key(story_id, "outcome"), vessel_text("vessel.story.line.outcome.default"))),
        ]
    elif status == "active":
        lines.append(vessel_text("vessel.story.line.active"))
    else:
        lines.append(vessel_text("vessel.story.line.unopened"))
    return lines


def resolve(state: GameState, story_id: str, choice: str) -> tuple[bool, str, int]:
    status = str(state.vessel_changes.get(_key(story_id, "status"), "unopened"))
    story_name = household_story_display_name(story_id)
    if choice == "d":
        return False, vessel_format("vessel.story.resolve.defer", story=story_name), 0
    if status == "unopened":
        eligible, reason = eligibility(state, story_id)
        if choice != "o" or not eligible:
            return False, vessel_format("vessel.story.resolve.needs", story=story_name, reason=reason), 0
        state.vessel_changes[_key(story_id, "status")] = "active"
        state.remember(vessel_format("vessel.story.resolve.open.memory", story=story_name))
        return True, vessel_format("vessel.story.resolve.open.result", story=story_name), 1
    if status != "active":
        return False, vessel_format("vessel.story.resolve.completed", story=story_name), 0
    living = [person for person in state.household if person.alive]
    if not living:
        return False, vessel_text("vessel.story.resolve.no_living"), 0
    if story_id == "empty-watch" and choice in {"w", "p"}:
        branch = vessel_text(f"vessel.story.branch.{story_id}.{choice}")
        state.vessel_changes["watch_order"] = "shared" if choice == "w" else "rotating"
        if len(living) > 1:
            first, second = living[:2]
            first.relationships[second.id] = min(3, first.relationships.get(second.id, 0) + 1)
            second.relationships[first.id] = min(3, second.relationships.get(first.id, 0) + 1)
        dead = [person.name for person in state.household if not person.alive]
        outcome = (
            vessel_format("vessel.story.outcome.empty_watch.dead", people=", ".join(dead))
            if dead else vessel_text("vessel.story.outcome.empty_watch.living")
        )
    elif story_id == "repair-share" and choice in {"w", "p"}:
        if choice == "p":
            timber = state.vessel_cargo.get("timber")
            if not timber or not timber.quantity:
                return False, vessel_text("vessel.story.resolve.timber"), 0
            timber.quantity -= 1
            if not timber.quantity:
                del state.vessel_cargo["timber"]
            state.vessel_integrity = min(10, state.vessel_integrity + 2)
            branch = vessel_text(f"vessel.story.branch.{story_id}.{choice}")
            outcome = vessel_text("vessel.story.outcome.repair_share.physical")
        else:
            branch = vessel_text(f"vessel.story.branch.{story_id}.{choice}")
            outcome = vessel_text("vessel.story.outcome.repair_share.common")
            for account in state.institutions.values():
                if account.obligation:
                    account.obligation -= 1
                    break
        for person in living:
            person.memories.append(vessel_format("vessel.story.memory", story=story_name, outcome=outcome))
            del person.memories[:-8]
    elif story_id == "eight-waters" and choice in {"c", "h", "r"}:
        branch = vessel_text(f"vessel.story.branch.{story_id}.{choice}")
        regions = outcome_regions(state)
        if choice == "c":
            for account in state.institutions.values():
                account.trust = min(3, account.trust + 1)
            outcome = vessel_format("vessel.story.outcome.eight_waters.common", regions=", ".join(region_display_name(region_id) for region_id in regions))
        elif choice == "h":
            for first in living:
                for second in living:
                    if first.id != second.id:
                        first.relationships[second.id] = min(3, first.relationships.get(second.id, 0) + 1)
            state.vessel_integrity = min(10, state.vessel_integrity + 1)
            outcome = vessel_text("vessel.story.outcome.eight_waters.household")
        else:
            for account in state.institutions.values():
                account.obligation = max(0, account.obligation - 1)
            outcome = vessel_text("vessel.story.outcome.eight_waters.separate")
        state.vessel_changes["campaign:all-region-capstone"] = branch
    else:
        return False, vessel_text("vessel.story.resolve.invalid"), 0
    state.vessel_changes[_key(story_id, "status")] = "completed"
    state.vessel_changes[_key(story_id, "branch")] = branch
    state.vessel_changes[_key(story_id, "outcome")] = outcome
    record = vessel_format("vessel.story.record", story=story_name, branch=branch, outcome=outcome)
    state.chronicle.append(record)
    del state.chronicle[:-24]
    state.remember(record)
    return True, record, 1


def validate_stories() -> None:
    if len(STORIES) != 3 or len(BY_ID) != 3 or STORIES[-1].id != "eight-waters":
        raise ValueError("two developments and one all-region capstone are required")


def validate_story_state(state: GameState) -> None:
    valid_parts = {"status", "branch", "outcome"}
    for key_name, value in state.vessel_changes.items():
        if not key_name.startswith("household-story:"):
            continue
        parts = key_name.split(":")
        if len(parts) != 3 or parts[1] not in BY_ID or parts[2] not in valid_parts or not isinstance(value, str):
            raise ValueError("invalid household story record")
        if parts[2] == "status" and value not in {"active", "completed"}:
            raise ValueError("invalid household story status")


validate_stories()
