"""Resolve authored quest-facing wording without changing quest identities."""

from __future__ import annotations

from .catalog import QuestPresentation, QuestServicePresentation, quest_contract, selected_content_pack


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


def regional_result_text(region_id: str, choice_id: str) -> str:
    for key, text in regional_quest_presentation(region_id).results:
        if key == choice_id:
            return text
    raise KeyError(f"unknown regional quest result {region_id!r}:{choice_id!r}")


def arc_presentation(arc_id: str) -> QuestPresentation:
    return selected_content_pack().quest_presentation(f"quest.arc.{arc_id}")


def arc_title(arc_id: str) -> str:
    return arc_presentation(arc_id).title


def arc_choice_presentation(arc_id: str, stage: int, choice_id: str) -> tuple[str, str]:
    key = f"{stage}.{choice_id}"
    for entry, label, requirement in arc_presentation(arc_id).arc_choices:
        if entry == key:
            return label, requirement
    raise KeyError(f"unknown arc choice {arc_id!r}:{key}")


def arc_result_text(arc_id: str, choice_id: str) -> str:
    for key, text in arc_presentation(arc_id).results:
        if key == choice_id:
            return text
    raise KeyError(f"unknown arc result {arc_id!r}:{choice_id!r}")


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


_SERVICE_IDS = {
    "c": "quest.service.cache_mark", "t": "quest.service.practical_instruction",
    "h": "quest.service.treatment", "p": "quest.service.public_field_report",
    "r": "quest.service.private_field_report", "w": "quest.service.workline_discussion",
    "d": "quest.service.dependency_delivery", "a": "quest.service.aftermath_contracts",
    "s": "quest.service.frontier_claim",
}


def secondary_service_presentation(service_id: str) -> QuestServicePresentation:
    try:
        return selected_content_pack().quest_service_presentation(_SERVICE_IDS[service_id])
    except KeyError as exc:
        raise KeyError(f"unknown secondary service {service_id!r}") from exc


def secondary_service_text(service_id: str, field: str, /, **values: object) -> str:
    presentation = secondary_service_presentation(service_id)
    if field == "label":
        text = presentation.label
    elif field == "requirement":
        text = presentation.requirement
    else:
        for key, value in presentation.results:
            if key == field:
                text = value
                break
        else:
            raise KeyError(f"unknown secondary service text {service_id!r}:{field!r}")
    return text.format(**values)


def secondary_service_response(service_id: str, region_id: str) -> str:
    for region, values in secondary_service_presentation(service_id).responses:
        if region == region_id:
            return values[0]
    raise KeyError(f"unknown secondary service response {service_id!r}:{region_id!r}")


def secondary_service_effect(service_id: str, technique: str) -> str:
    for key, text in secondary_service_presentation(service_id).effects:
        if key == technique:
            return text
    raise KeyError(f"unknown secondary service effect {service_id!r}:{technique!r}")
