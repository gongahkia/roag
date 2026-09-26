"""Headless application façade over Jomon's existing deterministic reducers."""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

from .actions import (
    ActionResult, advance_world, attack, choose_relic, guard, interact, move,
    retreat, set_auto_place_enabled, use_gear,
)
from .commands import (
    AdvanceWorldCommand, AttackCommand, GameCommand, GuardCommand,
    InteractCommand, MoveCommand, RetreatCommand, SelectCarriedRelicCommand,
    SetAutoPlaceCommand, UseGearCommand,
)
from .save import load_game, save_game
from .state import GameState, create_world
from .views import ActorView, InteractionView, WorldView, actor_views, interaction_view, world_view


@dataclass(frozen=True)
class CommandOutcome:
    accepted: bool
    changed: bool
    time_advanced: bool
    result_id: str
    revision: int
    overlay_id: str | None = None
    target_id: str | None = None


class GameSession:
    """A non-persisted application boundary; reducers remain engine authority."""

    def __init__(self, state: GameState):
        self._state = state
        self._revision = 0
        # Format 15 retains this field while automatic-placement callers still
        # read it. Frontends now change it only through a session command.
        self._auto_place_enabled = state.auto_place_enabled

    @classmethod
    def create(cls, seed: str) -> "GameSession":
        return cls(create_world(seed))

    @classmethod
    def load(cls, path: Path | None = None) -> "GameSession":
        return cls(load_game(path))

    def save(self, path: Path | None = None) -> Path:
        return save_game(self._state, path)

    @property
    def revision(self) -> int:
        return self._revision

    @property
    def auto_place_enabled(self) -> bool:
        return self._auto_place_enabled

    def world_view(self) -> WorldView:
        return world_view(self._state)

    def actor_views(self) -> tuple[ActorView, ...]:
        return actor_views(self._state)

    def actor_view(self, actor_id: str) -> ActorView | None:
        return next((view for view in self.actor_views() if view.id == actor_id), None)

    def interaction_view(self) -> InteractionView:
        return interaction_view(self._state)

    def _reject(self, result_id: str, target_id: str | None = None) -> CommandOutcome:
        return CommandOutcome(False, False, False, result_id, self._revision, target_id=target_id)

    def _outcome(self, result: ActionResult, result_id: str, target_id: str | None = None) -> CommandOutcome:
        changed = result.changed or result.time_advanced
        if changed:
            self._revision += 1
        return CommandOutcome(
            bool(changed or result.overlay), result.changed, result.time_advanced,
            result_id, self._revision, result.overlay, target_id,
        )

    def submit(self, command: GameCommand | object) -> CommandOutcome:
        """Validate a semantic command and dispatch the existing reducer once."""
        result: ActionResult
        if isinstance(command, MoveCommand):
            if (type(command.dx) is not int or type(command.dy) is not int
                    or (command.dx == 0 and command.dy == 0)
                    or max(abs(command.dx), abs(command.dy)) > 1):
                return self._reject("move.invalid")
            result = move(self._state, command.dx, command.dy)
            return self._outcome(result, "move.ok" if result.changed else "move.rejected")
        if isinstance(command, InteractCommand):
            choices = self.interaction_view().options
            choice = next((option for option in choices if option.interaction_id == command.interaction_id), None)
            if choice is None or (command.target_id is not None and command.target_id != choice.target_id):
                return self._reject("interaction.invalid", command.target_id)
            result = interact(self._state)
            result_id = "interaction.opened" if result.overlay else "interaction.resolved" if result.changed else "interaction.rejected"
            return self._outcome(result, result_id, choice.target_id)
        if isinstance(command, AttackCommand):
            if command.target_actor_id is not None and not isinstance(command.target_actor_id, str):
                return self._reject("attack.invalid")
            result = attack(self._state, command.target_actor_id)
            return self._outcome(result, "attack.resolved" if result.changed else "attack.rejected", command.target_actor_id)
        if isinstance(command, GuardCommand):
            if command.target_actor_id is not None and not isinstance(command.target_actor_id, str):
                return self._reject("guard.invalid")
            result = guard(self._state, command.target_actor_id)
            return self._outcome(result, "guard.resolved" if result.changed else "guard.rejected", command.target_actor_id)
        if isinstance(command, RetreatCommand):
            result = retreat(self._state)
            return self._outcome(result, "retreat.resolved" if result.changed else "retreat.rejected")
        if isinstance(command, UseGearCommand):
            if command.preparation_id is not None and not isinstance(command.preparation_id, str):
                return self._reject("gear.invalid")
            result = use_gear(self._state, command.preparation_id)
            return self._outcome(result, "gear.resolved" if result.changed else "gear.rejected", command.preparation_id)
        if isinstance(command, SelectCarriedRelicCommand):
            if command.relic_id is not None and (not isinstance(command.relic_id, str) or not command.relic_id):
                return self._reject("relic.invalid")
            result = choose_relic(self._state, command.relic_id)
            return self._outcome(result, "relic.selected" if result.changed else "relic.rejected", command.relic_id)
        if isinstance(command, SetAutoPlaceCommand):
            if type(command.enabled) is not bool:
                return self._reject("auto_place.invalid")
            result = set_auto_place_enabled(self._state, command.enabled)
            if result.changed:
                self._auto_place_enabled = command.enabled
            return self._outcome(result, "auto_place.set" if result.changed else "auto_place.rejected")
        if isinstance(command, AdvanceWorldCommand):
            if type(command.steps) is not int or not 1 <= command.steps <= 100 or type(command.guarded) is not bool:
                return self._reject("world.advance.invalid")
            result = advance_world(self._state, guarded=command.guarded, steps=command.steps)
            return self._outcome(result, "world.advanced")
        return self._reject("command.invalid")
