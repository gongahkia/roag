"""Renderer-neutral semantic commands for the Roag application boundary."""

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


@dataclass(frozen=True)
class TerrainActionCommand:
    """Apply a physical verb to one adjacent regional terrain cell."""
    action_id: str
    target: Position


@dataclass(frozen=True)
class AcquireGroundItemsCommand:
    """Move a stable set of items at the courier's feet into their pack."""
    item_ids: tuple[str, ...]


@dataclass(frozen=True)
class CollectRunItemCommand:
    """Collect one stable run-item drop at the courier's current cell."""
    drop_id: str


@dataclass(frozen=True)
class ChooseRunBranchCommand:
    """Cross one physically reached stage threshold."""
    region_id: str


@dataclass(frozen=True)
class AbandonRunCommand:
    """Permanently settle the current run as voluntarily abandoned."""


GameCommand = (
    MoveCommand | InteractCommand | AttackCommand | GuardCommand | RetreatCommand
    | UseGearCommand | SelectCarriedRelicCommand | SetAutoPlaceCommand
    | AdvanceWorldCommand | TerrainActionCommand | AcquireGroundItemsCommand
    | CollectRunItemCommand | ChooseRunBranchCommand | AbandonRunCommand
)
