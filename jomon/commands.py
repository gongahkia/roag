"""Renderer-neutral player intent for Jomon's deterministic session."""
from __future__ import annotations

from dataclasses import dataclass


@dataclass(frozen=True)
class MoveCommand:
    dx: int
    dy: int


@dataclass(frozen=True)
class AttackCommand:
    target_actor_id: str


@dataclass(frozen=True)
class EquipItemCommand:
    item_id: str


@dataclass(frozen=True)
class UnequipItemCommand:
    item_id: str


@dataclass(frozen=True)
class TravelCommand:
    route_id: str


@dataclass(frozen=True)
class CraftCommand:
    recipe_id: str


@dataclass(frozen=True)
class CharacterSetupCommand:
    crew_id: str
    ancestry_id: str
    origin_id: str
    trait_id: str


@dataclass(frozen=True)
class InteractCommand:
    """Attempt a local interaction with a stable semantic feature ID."""
    feature_id: str

@dataclass(frozen=True)
class SelectSuccessorCommand:
    member_id: str

@dataclass(frozen=True)
class RecoverRemainsItemCommand:
    member_id: str
    item_id: str


@dataclass(frozen=True)
class ActivityCommand:
    category_id: str
    activity_id: str
