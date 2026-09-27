"""Renderer-neutral semantic commands for the Jomon application boundary."""

from __future__ import annotations

from dataclasses import dataclass

from .state import Position


@dataclass(frozen=True)
class MoveCommand:
    """Attempt one adjacent movement step expressed in world directions."""
    dx: int
    dy: int


@dataclass(frozen=True)
class InteractCommand:
    """Use a stable currently available interaction choice."""
    target_id: str | None = None
    interaction_id: str = "interact.current"


@dataclass(frozen=True)
class AttackCommand:
    """Attempt an ordinary attack against a stable threat identity."""
    target_actor_id: str | None = None


@dataclass(frozen=True)
class GuardCommand:
    """Take the ordinary guard action, optionally against a stable target."""
    target_actor_id: str | None = None


@dataclass(frozen=True)
class RetreatCommand:
    """Attempt the ordinary retreat action."""


@dataclass(frozen=True)
class UseGearCommand:
    """Use a carried preparation by stable identity."""
    preparation_id: str | None = None


@dataclass(frozen=True)
class EquipItemCommand:
    """Equip one physical carried item by its stable item ID."""
    item_id: str


@dataclass(frozen=True)
class UnequipItemCommand:
    """Return the item in one stable equipment-slot ID to the courier pack."""
    slot_id: str


@dataclass(frozen=True)
class TravelCommand:
    """Confirm one stable adjacent route destination."""
    destination_id: str


@dataclass(frozen=True)
class StartTavernGameCommand:
    """Start an active tavern game with three stable opponent IDs."""
    game_id: str
    opponent_ids: tuple[str, ...]
    wagering: bool = False


@dataclass(frozen=True)
class DrawBetCommand:
    """Make one stable Draw betting decision."""
    action_id: str


@dataclass(frozen=True)
class DrawExchangeCommand:
    """Exchange courier cards selected by stable current card IDs."""
    card_ids: tuple[str, ...]


@dataclass(frozen=True)
class DiceActionCommand:
    """Roll or hold Quay Bones using a stable action ID."""
    action_id: str


@dataclass(frozen=True)
class CloseTavernGameCommand:
    """Clear a settled active tavern game by stable game ID."""
    game_id: str


@dataclass(frozen=True)
class ActivityCommand:
    """Resolve one stable ordinary-game activity choice.

    ``context_id`` identifies the current semantic activity surface and
    ``action_id`` identifies an option published by its immutable view.  A
    cell or actor target is supplied only for actions whose existing reducer
    already requires one.
    """
    context_id: str
    action_id: str
    target_position: Position | None = None
    target_actor_id: str | None = None


@dataclass(frozen=True)
class SelectCarriedRelicCommand:
    """Select a stable relic identity for later use, or clear selection."""
    relic_id: str | None


@dataclass(frozen=True)
class SetAutoPlaceCommand:
    """Set the application-owned automatic packing preference."""
    enabled: bool


@dataclass(frozen=True)
class AdvanceWorldCommand:
    """Advance the deterministic world clock without frontend-private access."""
    steps: int = 1
    guarded: bool = False


GameCommand = (
    MoveCommand | InteractCommand | AttackCommand | GuardCommand | RetreatCommand
    | UseGearCommand | SelectCarriedRelicCommand | SetAutoPlaceCommand
    | AdvanceWorldCommand | EquipItemCommand | UnequipItemCommand | TravelCommand
    | StartTavernGameCommand | DrawBetCommand | DrawExchangeCommand
    | DiceActionCommand | CloseTavernGameCommand | ActivityCommand
)
