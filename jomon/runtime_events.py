"""Transient, semantic, unpersisted renderer feedback."""
from __future__ import annotations

from dataclasses import dataclass

from .state import Position


@dataclass(frozen=True)
class RuntimeEvent:
    event_id: str


@dataclass(frozen=True)
class ActorMoved(RuntimeEvent):
    actor_id: str
    before: Position
    after: Position


@dataclass(frozen=True)
class AttackResolved(RuntimeEvent):
    source_id: str
    target_id: str
    damage: int


@dataclass(frozen=True)
class DefenderResponded(RuntimeEvent):
    defender_id: str
    target_id: str
    damage: int


@dataclass(frozen=True)
class ItemEquipped(RuntimeEvent):
    item_id: str


@dataclass(frozen=True)
class TravelResolved(RuntimeEvent):
    route_id: str


@dataclass(frozen=True)
class CraftResolved(RuntimeEvent):
    recipe_id: str
    item_id: str


@dataclass(frozen=True)
class ActivityResolved(RuntimeEvent):
    category_id: str
    activity_id: str


@dataclass(frozen=True)
class AccessOpened(RuntimeEvent):
    feature_id: str
    access_id: str


@dataclass(frozen=True)
class ObjectiveAcquired(RuntimeEvent):
    operation_id: str
    item_id: str
    method_ids: tuple[str, ...]


@dataclass(frozen=True)
class OperationDelivered(RuntimeEvent):
    operation_id: str
    feature_id: str

@dataclass(frozen=True)
class SuccessorSelected(RuntimeEvent):
    member_id: str

@dataclass(frozen=True)
class RemainsItemRecovered(RuntimeEvent):
    source_member_id: str
    item_id: str
