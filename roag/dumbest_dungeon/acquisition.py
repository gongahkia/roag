"""Scoped acquisition and content-pack contracts, shared by rewards and audits."""

from __future__ import annotations

from dataclasses import dataclass
from enum import StrEnum


class Lane(StrEnum):
    NORMAL = "normal"
    ELITE = "elite"
    OBJECTIVE = "objective"
    FACILITY = "facility"
    BARGAIN = "bargain"
    GUARDIAN = "guardian"
    FINALE = "finale"
    LOOP = "loop"


@dataclass(frozen=True, order=True)
class ContentRef:
    section: str
    id: str

    def __post_init__(self) -> None:
        if any(not isinstance(value, str) or not value for value in (self.section, self.id)):
            raise ValueError("content references require a section and stable ID")


@dataclass(frozen=True)
class ContentPack:
    id: str
    members: tuple[ContentRef, ...]
    requires: tuple[str, ...] = ()
    excludes: tuple[str, ...] = ()

    def __post_init__(self) -> None:
        if not isinstance(self.id, str) or ":" not in self.id:
            raise ValueError("content packs need stable namespaced IDs")
        if (not isinstance(self.members, tuple) or not all(isinstance(member, ContentRef) for member in self.members)
            or len(set(self.members)) != len(self.members)):
            raise ValueError("pack members must be unique typed content references")
        for values in (self.requires, self.excludes):
            if not isinstance(values, tuple) or not all(isinstance(value, str) for value in values) or len(set(values)) != len(values):
                raise ValueError("pack relations must contain unique IDs")
        if self.id in self.requires or self.id in self.excludes:
            raise ValueError("pack cannot require or exclude itself")


def validate_packs(catalog, packs: tuple[ContentPack, ...], enabled: tuple[str, ...]) -> tuple[ContentRef, ...]:
    registry = {pack.id: pack for pack in packs}
    if len(registry) != len(packs) or len(set(enabled)) != len(enabled):
        raise ValueError("duplicate pack ID")
    if any(identity not in registry for identity in enabled):
        raise ValueError("unknown enabled content pack")
    for pack in packs:
        if any(identity not in registry for identity in pack.requires + pack.excludes):
            raise ValueError("pack references an unknown dependency or exclusion")
        for member in pack.members:
            sections = set(catalog.__dataclass_fields__) - {"raw", "balance", "art"}
            if member.section not in sections or member.id not in getattr(catalog, member.section):
                raise ValueError(f"pack {pack.id} references unknown content {member.section}/{member.id}")
        if pack.id in enabled:
            if not set(pack.requires) <= set(enabled):
                raise ValueError(f"pack {pack.id} has disabled requirements")
            if set(pack.excludes) & set(enabled):
                raise ValueError(f"pack {pack.id} conflicts with enabled content")
    return tuple(sorted({member for identity in enabled for member in registry[identity].members}))


def eligible_techniques(catalog, owners: set[str], lane: Lane, *, members: tuple[ContentRef, ...] | None = None) -> tuple[str, ...]:
    if not isinstance(lane, Lane):
        raise ValueError("unregistered acquisition lane")
    active = {member.id for member in members if member.section == "cards"} if members is not None else set(catalog.cards)
    return tuple(sorted(identity for identity, card in catalog.cards.items()
                        if identity in active and card["hero"] in owners
                        and lane.value in card.get("lanes", tuple(Lane))))
