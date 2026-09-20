"""Resolve fixed character and mechanical-role presentation from the selected pack."""

from __future__ import annotations

from .catalog import CharacterPresentation, RolePresentation, selected_content_pack


def character_presentation(semantic_id: str) -> CharacterPresentation:
    """Return immutable presentation for one engine-owned static character slot."""
    return selected_content_pack().character_presentation(semantic_id)


def role_presentation(engine_id: str) -> RolePresentation | None:
    """Return presentation for a known mechanical role, if this slice owns it."""
    try:
        return selected_content_pack().role_presentation(engine_id)
    except KeyError:
        return None


def role_display_name(engine_id: str) -> str:
    presentation = role_presentation(engine_id)
    return presentation.display_label if presentation else engine_id


def household_background(ancestry: str, origin: str, role_id: str) -> str:
    return selected_content_pack().household_background_template.format(
        ancestry=ancestry, origin=origin, role_label=role_display_name(role_id),
    )


def character_short_name(semantic_id: str) -> str:
    return character_presentation(semantic_id).display_name.split()[0]
