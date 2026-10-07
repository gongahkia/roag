"""Resolve selected-pack magic wording from stable spell and intent identities."""
from __future__ import annotations

from .catalog import selected_content_pack


def magic_text(semantic_id: str) -> str:
    return selected_content_pack().magic_presentation(semantic_id).text


def magic_format(semantic_id: str, **values: object) -> str:
    return magic_text(semantic_id).format(**values)


def spell_display_name(spell_id: str) -> str:
    return magic_text(f"magic.spell.{spell_id}.name")


def magic_effect_display_name(effect_id: str) -> str:
    return magic_text(f"magic.effect.{effect_id}.name")


def spell_description(spell) -> str:
    return magic_format(
        f"magic.spell.{spell.id}.description",
        effect=magic_effect_display_name(spell.effect), power=spell.power,
        radius=spell.radius, cost=spell.cost, reach=spell.reach,
    )
