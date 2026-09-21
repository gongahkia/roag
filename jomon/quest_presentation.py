"""Resolve authored quest-facing wording without changing quest identities."""

from __future__ import annotations

from .catalog import QuestPresentation, quest_contract, selected_content_pack


def regional_quest_presentation(region_id: str) -> QuestPresentation:
    return selected_content_pack().quest_presentation(f"quest.regional.{region_id}")


def regional_quest_title(region_id: str) -> str:
    return regional_quest_presentation(region_id).title


def regional_quest_lead(region_id: str) -> str:
    lead = regional_quest_presentation(region_id).lead
    if lead is None:  # Contract validation prevents this for regional slots.
        raise KeyError(f"regional quest {region_id!r} has no authored lead")
    return lead


def regional_choice_presentation(region_id: str, choice_id: str) -> tuple[str, str]:
    """Return authored label and availability explanation for a stable branch key."""
    for key, label, requirement in regional_quest_presentation(region_id).choices:
        if key == choice_id:
            return label, requirement
    raise KeyError(f"unknown regional quest choice {region_id!r}:{choice_id!r}")


def arc_presentation(arc_id: str) -> QuestPresentation:
    return selected_content_pack().quest_presentation(f"quest.arc.{arc_id}")


def arc_title(arc_id: str) -> str:
    return arc_presentation(arc_id).title


def evidence_presentation(arc_id: str) -> QuestPresentation:
    return selected_content_pack().quest_presentation(f"quest.evidence.{arc_id}")


def evidence_display_name(arc_id: str) -> str:
    name = evidence_presentation(arc_id).evidence_name
    if name is None:
        raise KeyError(f"quest evidence {arc_id!r} has no authored display name")
    return name


def evidence_description(arc_id: str) -> str:
    description = evidence_presentation(arc_id).evidence_description
    if description is None:
        raise KeyError(f"quest evidence {arc_id!r} has no authored description")
    return description


def evidence_presentation_for_engine_id(engine_id: str) -> QuestPresentation | None:
    """Resolve a contracted evidence key while allowing unrelated consumables."""
    for slot in quest_contract():
        if slot.kind == "evidence" and slot.engine_id == engine_id:
            return selected_content_pack().quest_presentation(slot.id)
    return None
