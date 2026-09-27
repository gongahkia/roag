"""Renderer-neutral player intent for the content-engine baseline."""
from __future__ import annotations
from dataclasses import dataclass
@dataclass(frozen=True)
class MoveCommand: dx:int; dy:int
@dataclass(frozen=True)
class AttackCommand: target_actor_id:str
@dataclass(frozen=True)
class EquipItemCommand: item_id:str
@dataclass(frozen=True)
class UnequipItemCommand: item_id:str
@dataclass(frozen=True)
class TravelCommand: route_id:str
@dataclass(frozen=True)
class CraftCommand: recipe_id:str
@dataclass(frozen=True)
class CharacterSetupCommand:
    crew_id:str; ancestry_id:str; origin_id:str; trait_id:str
@dataclass(frozen=True)
class InteractCommand: interaction_id:str
