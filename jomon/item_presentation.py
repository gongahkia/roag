"""Resolve base item presentation from the selected content pack."""

from __future__ import annotations

from .catalog import ItemPresentation, selected_content_pack


_KIND_PREFIXES = ("commodity:", "consumable:", "passive:", "relic:")


def item_engine_id(identity: str) -> str:
    """Normalize a physical item kind to its stable base catalog identity."""
    for prefix in _KIND_PREFIXES:
        if identity.startswith(prefix):
            return identity[len(prefix):]
    return identity


def item_presentation(engine_id: str) -> ItemPresentation:
    """Return immutable selected-pack presentation for one contracted base item."""
    return selected_content_pack().item_presentation(item_engine_id(engine_id))


def item_display_name(engine_id: str) -> str:
    return item_presentation(engine_id).display_name


def item_description(engine_id: str) -> str:
    return item_presentation(engine_id).description


def item_short_description(engine_id: str) -> str:
    presentation = item_presentation(engine_id)
    return presentation.short_description or presentation.description


def contracted_item_presentation(engine_id: str) -> ItemPresentation | None:
    """Resolve a base item when covered; leave deliberately dynamic kinds alone."""
    try:
        return item_presentation(engine_id)
    except KeyError:
        return None


def item_display_name_or_legacy(engine_id: str) -> str:
    """Render contracted base items while retaining deliberate dynamic fallbacks."""
    presentation = contracted_item_presentation(engine_id)
    if presentation:
        return presentation.display_name
    from .legendary_presentation import arc_relic_display_name
    return arc_relic_display_name(item_engine_id(engine_id)) or engine_id
