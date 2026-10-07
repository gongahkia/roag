"""Resolve selected-pack production wording from stable production identities."""
from __future__ import annotations

from .catalog import selected_content_pack


def production_text(semantic_id: str) -> str:
    """Return immutable selected-pack text for one engine-owned production slot."""
    return selected_content_pack().production_presentation(semantic_id).text


def production_format(semantic_id: str, **values: object) -> str:
    """Format validated selected-pack production text with engine-supplied values."""
    return production_text(semantic_id).format(**values)


def production_station_name(station_id: str) -> str:
    return production_text(f"production.station.{station_id}.name")


def production_recipe_name(recipe_id: str, output_kind: str | None = None) -> str:
    """Resolve a recipe label without making that label part of recipe identity."""
    if recipe_id.startswith("make:"):
        if output_kind is None:
            raise ValueError("fabrication recipe requires an output kind")
        from .item_presentation import item_display_name_or_legacy

        return production_format("production.recipe.fabricate", item=item_display_name_or_legacy(output_kind))
    return production_text(f"production.recipe.{recipe_id}.name")
