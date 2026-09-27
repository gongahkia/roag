"""Transient, semantic, unpersisted renderer feedback."""
from __future__ import annotations
from dataclasses import dataclass
from .state import Position
@dataclass(frozen=True)
class RuntimeEvent: event_id:str
@dataclass(frozen=True)
class ActorMoved(RuntimeEvent): actor_id:str; before:Position; after:Position
@dataclass(frozen=True)
class AttackResolved(RuntimeEvent): source_id:str; target_id:str; damage:int
@dataclass(frozen=True)
class ItemEquipped(RuntimeEvent): item_id:str
@dataclass(frozen=True)
class TravelResolved(RuntimeEvent): route_id:str
@dataclass(frozen=True)
class CraftResolved(RuntimeEvent): recipe_id:str; item_id:str
@dataclass(frozen=True)
class ActivityResolved(RuntimeEvent): category_id:str; activity_id:str
