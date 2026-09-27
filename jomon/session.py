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
    SetAutoPlaceCommand, UseGearCommand, EquipItemCommand, UnequipItemCommand,
    TravelCommand,
)
from .runtime_events import (
    ActorMoved, CarriedRelicSelectionChanged, GuardResolved, InteractionResolved,
    ItemUsed, RetreatResolved, RuntimeEvent, ItemEquipped, ItemUnequipped,
    TravelResolved,
)
from .save import load_game, save_game
from .state import GameState, create_world
from .views import (
    ActorView, EquipmentView, InteractionView, InventoryView, QuestView, TravelView,
    WorldView, actor_views, equipment_view, interaction_view, inventory_view,
    quest_views, travel_view, world_view,
)


@dataclass(frozen=True)
class CommandOutcome:
    accepted: bool
    changed: bool
    time_advanced: bool
    result_id: str
    revision: int
    overlay_id: str | None = None
    target_id: str | None = None
    events: tuple[RuntimeEvent, ...] = ()


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

    def inventory_view(self) -> InventoryView:
        return inventory_view(self._state)

    def equipment_view(self) -> EquipmentView:
        return equipment_view(self._state)

    def quest_views(self) -> tuple[QuestView, ...]:
        return quest_views(self._state)

    def travel_view(self) -> TravelView:
        return travel_view(self._state)

    def _reject(self, result_id: str, target_id: str | None = None) -> CommandOutcome:
        return CommandOutcome(False, False, False, result_id, self._revision, target_id=target_id)

    def _outcome(
        self,
        result: ActionResult,
        result_id: str,
        target_id: str | None = None,
        events: tuple[RuntimeEvent, ...] = (),
    ) -> CommandOutcome:
        changed = result.changed or result.time_advanced
        if changed:
            self._revision += 1
        return CommandOutcome(
            bool(changed or result.overlay), result.changed, result.time_advanced,
            result_id, self._revision, result.overlay, target_id,
            (*result.events, *events),
        )

    def submit(self, command: GameCommand | object) -> CommandOutcome:
        """Validate a semantic command and dispatch the existing reducer once."""
        result: ActionResult
        if isinstance(command, MoveCommand):
            if (type(command.dx) is not int or type(command.dy) is not int
                    or (command.dx == 0 and command.dy == 0)
                    or max(abs(command.dx), abs(command.dy)) > 1):
                return self._reject("move.invalid")
            actor_id, before = self._state.active_courier_id or "courier", self._state.position
            result = move(self._state, command.dx, command.dy)
            events: tuple[RuntimeEvent, ...] = ()
            if result.changed and self._state.position != before:
                events = (ActorMoved(actor_id, before, self._state.position, "movement.step"),)
            return self._outcome(result, "move.ok" if result.changed else "move.rejected", events=events)
        if isinstance(command, InteractCommand):
            choices = self.interaction_view().options
            choice = next((option for option in choices if option.interaction_id == command.interaction_id), None)
            if choice is None or (command.target_id is not None and command.target_id != choice.target_id):
                return self._reject("interaction.invalid", command.target_id)
            actor_id = self._state.active_courier_id or "courier"
            if command.interaction_id.startswith("voyage.response."):
                from .travel import resolve_voyage
                origin = self._state.route_current_node
                before_time = self._state.world_time
                changed, message = resolve_voyage(self._state, command.interaction_id.rsplit(".", 1)[1])
                result = ActionResult(changed, self._state.world_time != before_time, message)
                result_id = "voyage.resolved" if changed else "voyage.rejected"
                events = (TravelResolved(origin, self._state.route_current_node, self._state.voyage_status),) if changed else ()
                return self._outcome(result, result_id, choice.target_id, events)
            result = interact(self._state)
            result_id = "interaction.opened" if result.overlay else "interaction.resolved" if result.changed else "interaction.rejected"
            events: tuple[RuntimeEvent, ...] = ()
            if result.changed and result.overlay is None:
                events = (InteractionResolved(actor_id, choice.target_id, choice.interaction_id, result_id),)
            return self._outcome(result, result_id, choice.target_id, events)
        if isinstance(command, AttackCommand):
            if command.target_actor_id is not None and not isinstance(command.target_actor_id, str):
                return self._reject("attack.invalid")
            result = attack(self._state, command.target_actor_id)
            return self._outcome(result, "attack.resolved" if result.changed else "attack.rejected", command.target_actor_id)
        if isinstance(command, GuardCommand):
            if command.target_actor_id is not None and not isinstance(command.target_actor_id, str):
                return self._reject("guard.invalid")
            actor_id = self._state.active_courier_id or "courier"
            result = guard(self._state, command.target_actor_id)
            events: tuple[RuntimeEvent, ...] = ()
            if result.changed:
                events = (GuardResolved(actor_id, command.target_actor_id),)
            return self._outcome(result, "guard.resolved" if result.changed else "guard.rejected", command.target_actor_id, events)
        if isinstance(command, RetreatCommand):
            actor_id, before = self._state.active_courier_id or "courier", self._state.position
            result = retreat(self._state)
            events: tuple[RuntimeEvent, ...] = ()
            if result.changed:
                events = (RetreatResolved(actor_id, before, self._state.position),)
            return self._outcome(result, "retreat.resolved" if result.changed else "retreat.rejected", events=events)
        if isinstance(command, UseGearCommand):
            if command.preparation_id is not None and not isinstance(command.preparation_id, str):
                return self._reject("gear.invalid")
            actor_id = self._state.active_courier_id or "courier"
            item_id = command.preparation_id or self._state.gear
            result = use_gear(self._state, command.preparation_id)
            events: tuple[RuntimeEvent, ...] = ()
            if result.changed and item_id:
                events = (ItemUsed(actor_id, item_id, "gear.use"),)
            return self._outcome(result, "gear.resolved" if result.changed else "gear.rejected", command.preparation_id, events)
        if isinstance(command, EquipItemCommand):
            if not isinstance(command.item_id, str) or not command.item_id:
                return self._reject("item.equip.invalid")
            from .inventory import equip_item, equipped_item
            item = next((row for row in self.inventory_view().items if row.id == command.item_id), None)
            if item is None or "equip" not in item.legal_operations:
                return self._reject("item.equip.rejected", command.item_id)
            changed = equip_item(self._state, command.item_id)
            current = next((row for row in self.equipment_view().slots if row.id == command.item_id), None)
            events = (ItemEquipped(self._state.active_courier_id or "courier", command.item_id, current.location_id),) if changed and current else ()
            return self._outcome(ActionResult(changed, False, ""), "item.equipped" if changed else "item.equip.rejected", command.item_id, events)
        if isinstance(command, UnequipItemCommand):
            if not isinstance(command.slot_id, str) or not command.slot_id:
                return self._reject("item.unequip.invalid")
            current = next((row for row in self.equipment_view().slots if row.location_id == command.slot_id), None)
            if current is None:
                return self._reject("item.unequip.rejected", command.slot_id)
            from .inventory import unequip_item
            changed = unequip_item(self._state, command.slot_id)
            events = (ItemUnequipped(self._state.active_courier_id or "courier", current.id, command.slot_id),) if changed else ()
            return self._outcome(ActionResult(changed, False, ""), "item.unequipped" if changed else "item.unequip.rejected", current.id, events)
        if isinstance(command, TravelCommand):
            if not isinstance(command.destination_id, str) or not command.destination_id:
                return self._reject("travel.invalid")
            destination = next((row for row in self.travel_view().destinations if row.destination_id == command.destination_id), None)
            if destination is None or not destination.available:
                return self._reject("travel.rejected", command.destination_id)
            from .travel import choose_destination
            origin = self._state.route_current_node
            before_time = self._state.world_time
            changed, message = choose_destination(self._state, command.destination_id)
            result = ActionResult(changed, self._state.world_time != before_time, message)
            events = (TravelResolved(origin, command.destination_id, self._state.voyage_status),) if changed else ()
            return self._outcome(result, "travel.resolved" if changed else "travel.rejected", command.destination_id, events)
        if isinstance(command, SelectCarriedRelicCommand):
            if command.relic_id is not None and (not isinstance(command.relic_id, str) or not command.relic_id):
                return self._reject("relic.invalid")
            actor_id = self._state.active_courier_id or "courier"
            result = choose_relic(self._state, command.relic_id)
            events: tuple[RuntimeEvent, ...] = ()
            if result.changed:
                events = (CarriedRelicSelectionChanged(actor_id, command.relic_id),)
            return self._outcome(result, "relic.selected" if result.changed else "relic.rejected", command.relic_id, events)
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
