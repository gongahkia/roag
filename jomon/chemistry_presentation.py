"""Resolve selected-pack chemistry presentation from stable chemistry IDs."""
from __future__ import annotations

from .catalog import selected_content_pack


def _slot(category: str, engine_id: str) -> str:
    return f"chemistry.{category}.{engine_id.replace(' ', '_').replace('-', '_')}.name"


def chemistry_text(semantic_id: str) -> str:
    return selected_content_pack().chemistry_presentation(semantic_id).text


def chemistry_format(semantic_id: str, **values: object) -> str:
    return chemistry_text(semantic_id).format(**values)


def reagent_display_name(reagent_id: str) -> str:
    if reagent_id == "legacy.unknown.reagent":
        return chemistry_text("chemistry.reagent.legacy_unknown.name")
    return chemistry_text(_slot("reagent", reagent_id))


def reaction_display_name(reaction_id: str) -> str:
    return chemistry_text(_slot("reaction", reaction_id))


def reagent_contents_display(contents: dict[str, int]) -> str:
    """Render the legacy dict-shaped flask contents without using names as keys."""
    return str({reagent_display_name(reagent): quantity for reagent, quantity in contents.items()})


def reaction_list_display(reaction_ids: list[str] | tuple[str, ...] | set[str]) -> str:
    return ", ".join(reaction_display_name(reaction) for reaction in reaction_ids)
