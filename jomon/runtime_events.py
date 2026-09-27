"""Transient renderer-neutral semantic outputs from deterministic commands.

These records are command-scoped application outputs.  They are not saved,
consumed by simulation, or related to ``state.SoundEvent`` noise stimuli.
"""

from __future__ import annotations

from dataclasses import dataclass, field

from .state import Position


@dataclass(frozen=True)
class ActorMoved:
    actor_id: str
    from_position: Position
    to_position: Position
    movement_kind_id: str
    event_id: str = field(init=False, default="actor.moved")


@dataclass(frozen=True)
class InteractionResolved:
    actor_id: str
    target_id: str
    interaction_id: str
    result_id: str
    event_id: str = field(init=False, default="interaction.resolved")


@dataclass(frozen=True)
class AttackResolved:
    attacker_id: str
    target_id: str
    action_id: str
    result_id: str
    event_id: str = field(init=False, default="combat.attack.resolved")


@dataclass(frozen=True)
class DamageApplied:
    source_actor_id: str
    target_actor_id: str
    amount: int
    damage_kind_id: str
    hit_location_id: str
    event_id: str = field(init=False, default="combat.damage.applied")


@dataclass(frozen=True)
class StatusChanged:
    actor_id: str
    previous_status_id: str
    status_id: str
    event_id: str = field(init=False, default="actor.status.changed")


@dataclass(frozen=True)
class ActorDefeated:
    actor_id: str
    defeated_by_actor_id: str
    event_id: str = field(init=False, default="actor.defeated")


@dataclass(frozen=True)
class ItemUsed:
    actor_id: str
    item_id: str
    usage_id: str
    event_id: str = field(init=False, default="item.used")


@dataclass(frozen=True)
class ItemEquipped:
    actor_id: str
    item_id: str
    slot_id: str
    event_id: str = field(init=False, default="item.equipped")


@dataclass(frozen=True)
class ItemUnequipped:
    actor_id: str
    item_id: str
    slot_id: str
    event_id: str = field(init=False, default="item.unequipped")


@dataclass(frozen=True)
class TravelResolved:
    origin_id: str
    destination_id: str
    travel_status_id: str
    event_id: str = field(init=False, default="travel.resolved")


@dataclass(frozen=True)
class CarriedRelicSelectionChanged:
    actor_id: str
    relic_id: str | None
    event_id: str = field(init=False, default="relic.selection.changed")


@dataclass(frozen=True)
class GuardResolved:
    actor_id: str
    target_actor_id: str | None
    event_id: str = field(init=False, default="combat.guard.resolved")


@dataclass(frozen=True)
class RetreatResolved:
    actor_id: str
    from_position: Position
    to_position: Position
    event_id: str = field(init=False, default="combat.retreat.resolved")


RuntimeEvent = (
    ActorMoved | InteractionResolved | AttackResolved | DamageApplied
    | StatusChanged | ActorDefeated | ItemUsed | CarriedRelicSelectionChanged
    | GuardResolved | RetreatResolved | ItemEquipped | ItemUnequipped | TravelResolved
)
