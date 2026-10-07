"""Compatibility views over selected-pack Dullest Dungeon presentation.

Stable IDs and rules remain in :mod:`content`; this module deliberately owns no
fiction.  Older callers can use its maps while presentation remains live for
the selected Jomon pack.
"""
from __future__ import annotations

from collections.abc import Sequence
from dataclasses import dataclass

from .content import load_catalog
from .presentation import DdTextMap, card_description, card_name, dd_text

OFFICE_ROLES = DdTextMap("heroes", "name")
OFFICE_BIOMES = DdTextMap("biomes", "name")
OFFICE_WORLDS = DdTextMap("worlds", "name")
OFFICE_SQUADS = DdTextMap("squads")
OFFICE_TARGETS = DdTextMap("targets")
OFFICE_STATUSES = DdTextMap("statuses")
OFFICE_STATES = DdTextMap("states")


class _DdNameSequence(Sequence[str]):
    """Stable catalog ordering with live selected-pack labels."""
    def __init__(self, collection: str, prefix: str):
        self.collection, self.prefix = collection, prefix
    def _ids(self) -> tuple[str, ...]:
        return tuple(getattr(load_catalog(), self.collection))
    def __len__(self) -> int: return len(self._ids())
    def __getitem__(self, index):
        ids = self._ids()
        if isinstance(index, slice):
            return [dd_text(f"{self.prefix}.{identity}") for identity in ids[index]]
        return dd_text(f"{self.prefix}.{ids[index]}")


DOCTRINE_NAMES = _DdNameSequence("doctrines", "doctrines")
INFUSION_NAMES = _DdNameSequence("infusions", "infusions")


@dataclass(frozen=True)
class OfficeCard:
    id: str
    name: str
    role: str
    cost: int
    target: str
    effects: tuple[dict, ...]
    description: str


def office_facility_option(effect_ops: set[str], cost: dict) -> str:
    """Render a generic selected-pack facility choice after rules select it."""
    operation = next(iter(sorted(effect_ops)), "service")
    return dd_text("narration.facility", operation=operation.replace("_", " "),
                   amount=cost.get("amount", 0), resource=cost.get("resource", "none"))


def office_card_description(card_id: str, *, upgraded: bool = False) -> str:
    del upgraded  # The selected pack owns the visible card wording.
    return card_description(card_id)


def office_catalog() -> tuple[dict[str, dict], dict[str, OfficeCard]]:
    """Compatibility materialization using stable catalog IDs and pack text."""
    source = load_catalog()
    roles = {
        hero_id: {
            "name": OFFICE_ROLES[hero_id],
            "max_hp": hero["max_hp"],
            "starter_deck": list(hero["starter_deck"]),
            "signature": dd_text(f"heroes.{hero_id}.signature"),
        }
        for hero_id, hero in source.heroes.items()
    }
    cards = {
        card_id: OfficeCard(
            card_id, card_name(card_id), definition["hero"], definition["cost"],
            definition["target"], tuple(dict(effect) for effect in definition["effects"]),
            office_card_description(card_id),
        )
        for card_id, definition in source.cards.items()
    }
    return roles, cards
