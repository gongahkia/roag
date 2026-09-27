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
    InteractCommand, MoveCommand, RetreatCommand, NegotiateCommand, SelectCarriedRelicCommand,
    SetAutoPlaceCommand, UseGearCommand, MoveItemCommand, DropItemCommand, EquipItemCommand, UnequipItemCommand,
    TravelCommand, StartTavernGameCommand, DrawBetCommand, DrawExchangeCommand,
    DiceActionCommand, CloseTavernGameCommand,
    ActivityCommand, CharacterSetupCommand,
)
from .runtime_events import (
    ActorMoved, CarriedRelicSelectionChanged, GuardResolved, InteractionResolved,
    ItemUsed, RetreatResolved, RuntimeEvent, ItemMoved, ItemDropped, ItemEquipped, ItemUnequipped,
    TravelResolved, TavernCardsExchanged, TavernDiceRolled, TavernGameSettled,
    TavernGameStarted,
    GameplayActivityResolved,
)
from .save import load_game, save_game
from .state import GameState, create_world
from .views import (
    ActorView, EquipmentView, InteractionView, InventoryView, QuestView, TravelView,
    TavernDiceView, TavernDrawView,
    WorldView, actor_views, equipment_view, interaction_view, inventory_view,
    quest_views, travel_view, world_view,
    tavern_dice_view, tavern_draw_view,
    ActivityView, CharacterSetupView, character_setup_view, pending_character_setup_view,
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
    def pending_character_setup_view(cls, seed: str, crew_id: str | None = None) -> CharacterSetupView:
        """Read the seed-derived setup choices before a world is created."""
        return pending_character_setup_view(seed, crew_id)

    @classmethod
    def create_configured(cls, seed: str, command: CharacterSetupCommand) -> tuple["GameSession", CommandOutcome]:
        """Create one world and immediately commit its initial courier setup.

        The temporary unconfigured state is never exposed to a frontend: the
        caller receives a session only after the authoritative setup reducer
        accepts the stable choices.
        """
        session = cls.create(seed)
        outcome = session.submit(command)
        if not outcome.accepted:
            raise ValueError("invalid initial character setup")
        return session, outcome

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

    def inventory_view(self, source_id: str | None = None) -> InventoryView:
        return inventory_view(self._state, source_id)

    def equipment_view(self) -> EquipmentView:
        return equipment_view(self._state)

    def quest_views(self) -> tuple[QuestView, ...]:
        return quest_views(self._state)

    def travel_view(self) -> TravelView:
        return travel_view(self._state)

    def tavern_draw_view(self) -> TavernDrawView:
        return tavern_draw_view(self._state)

    def tavern_dice_view(self) -> TavernDiceView:
        return tavern_dice_view(self._state)

    def activity_view(self, context_id: str) -> ActivityView:
        """Expose one current ordinary-game semantic activity surface."""
        from .activities import activity_view
        return activity_view(self._state, context_id)

    def character_setup_view(self, crew_id: str | None = None) -> CharacterSetupView:
        """Expose the initial courier choices before ordinary play starts."""
        return character_setup_view(self._state, crew_id)

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
            if (command.target_actor_id is not None and not isinstance(command.target_actor_id, str)
                    or command.target_position is not None and not hasattr(command.target_position, "x")
                    or command.ammunition_id is not None and not isinstance(command.ammunition_id, str)):
                return self._reject("attack.invalid")
            result = attack(self._state, command.target_actor_id,
                            target_position=command.target_position, ammunition=command.ammunition_id)
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
        if isinstance(command, NegotiateCommand):
            from .actions import negotiate
            result = negotiate(self._state)
            return self._outcome(result, "negotiate.resolved" if result.changed else "negotiate.rejected")
        if isinstance(command, UseGearCommand):
            if command.preparation_id is not None and not isinstance(command.preparation_id, str):
                return self._reject("gear.invalid")
            actor_id = self._state.active_courier_id or "courier"
            bottle = next((item for item in self._state.items if item.owner_id == self._state.active_courier_id
                           and item.location == "pack" and item.kind.startswith("consumable:bottle:")), None)
            item_id = command.preparation_id or (bottle.id if bottle else self._state.gear)
            result = use_gear(self._state, command.preparation_id)
            events: tuple[RuntimeEvent, ...] = ()
            if result.changed and item_id:
                events = (ItemUsed(actor_id, item_id, "gear.use"),)
            return self._outcome(result, "gear.resolved" if result.changed else "gear.rejected", command.preparation_id, events)
        if isinstance(command, MoveItemCommand):
            if command.destination_id not in {"pack", "locker"} or not isinstance(command.item_id, str):
                return self._reject("item.move.invalid", command.item_id)
            physical = next((row for row in self._state.items if row.id == command.item_id), None)
            source_id = (
                f"container:{physical.container_id}" if physical and physical.location == "container"
                else "ground" if physical and physical.location == "ground"
                else None
            )
            item = next((row for row in self.inventory_view(source_id).items if row.id == command.item_id), None)
            if item is None or "move" not in item.legal_operations or item.location_id == command.destination_id:
                return self._reject("item.move.rejected", command.item_id)
            from .inventory import auto_place, record_acquisition, sync_legacy_load, transfer_to_grid
            if item.location_id in {"container", "ground"}:
                # This is the established transfer behaviour: a
                # source item is taken only when automatic placement can fit
                # it, then its stable container membership is retired.
                moved = self._state.auto_place_enabled and auto_place(
                    self._state, command.item_id, "pack", owner_id=self._state.active_courier_id,
                )
                if moved and physical is not None:
                    container = (
                        next((row for row in self._state.region.containers
                              if row.id == source_id.split(":", 1)[1]), None)
                        if source_id and source_id.startswith("container:") else None
                    )
                    if container and command.item_id in container.item_ids:
                        container.item_ids.remove(command.item_id)
                    record_acquisition(self._state, physical)
            else:
                moved = transfer_to_grid(self._state, command.item_id, command.destination_id,
                                         owner_id=self._state.active_courier_id if command.destination_id == "pack" else None)
            if not moved:
                return self._reject("item.move.rejected", command.item_id)
            sync_legacy_load(self._state)
            return self._outcome(ActionResult(True, False, ""), "item.moved", command.item_id,
                                 (ItemMoved(self._state.active_courier_id or "courier", command.item_id,
                                            source_id or item.location_id, command.destination_id),))
        if isinstance(command, DropItemCommand):
            if not isinstance(command.item_id, str):
                return self._reject("item.drop.invalid", command.item_id)
            item = next((row for row in self.inventory_view().items if row.id == command.item_id), None)
            if (item is None or "drop" not in item.legal_operations
                    or self._state.location == "jomon" and self._state.jomon_space != "vessel"):
                return self._reject("item.drop.rejected", command.item_id)
            from .inventory import drop_item, sync_legacy_load
            if not drop_item(self._state, command.item_id):
                return self._reject("item.drop.rejected", command.item_id)
            sync_legacy_load(self._state)
            return self._outcome(ActionResult(True, False, ""), "item.dropped", command.item_id,
                                 (ItemDropped(self._state.active_courier_id or "courier", command.item_id,
                                              self._state.position),))
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
        if isinstance(command, StartTavernGameCommand):
            from .tavern_games import tavern_game_available

            if command.game_id == "dullest":
                return self._reject("tavern.game.retired", command.game_id)
            if (not tavern_game_available(command.game_id)
                    or not isinstance(command.opponent_ids, tuple)
                    or len(command.opponent_ids) != 3
                    or len(set(command.opponent_ids)) != 3
                    or any(not isinstance(identity, str) or not identity for identity in command.opponent_ids)
                    or type(command.wagering) is not bool):
                return self._reject("tavern.game.invalid", command.game_id)
            before_time = self._state.world_time
            try:
                if command.game_id == "draw":
                    from .tavern_draw import drive_npcs, start_hand
                    match = start_hand(self._state, list(command.opponent_ids), wagering=command.wagering)
                    drive_npcs(self._state)
                    match_id, players = f"draw.hand.{match['number']}", tuple(match["players"])
                else:
                    from .tavern_dice import drive_npcs, start_match
                    match = start_match(self._state, list(command.opponent_ids))
                    drive_npcs(self._state)
                    match_id, players = f"dice.match.{match['number']}", tuple(match["players"])
            except ValueError:
                return self._reject("tavern.game.rejected", command.game_id)
            result = ActionResult(True, self._state.world_time != before_time, "")
            return self._outcome(result, "tavern.game.started", command.game_id,
                                 (TavernGameStarted(command.game_id, match_id, players),))
        if isinstance(command, DrawBetCommand):
            view = self.tavern_draw_view()
            if command.action_id not in view.legal_actions:
                return self._reject("tavern.draw.rejected", command.action_id)
            if not command.action_id.startswith("draw.bet."):
                return self._reject("tavern.draw.invalid", command.action_id)
            from .tavern_draw import bet_action, drive_npcs
            action = command.action_id.rsplit(".", 1)[1]
            try:
                bet_action(self._state, action)
                drive_npcs(self._state)
            except ValueError:
                return self._reject("tavern.draw.rejected", command.action_id)
            updated = self.tavern_draw_view()
            events: tuple[RuntimeEvent, ...] = ()
            if updated.phase_id == "draw.phase.complete":
                events = (TavernGameSettled("draw", updated.winner_ids),)
            return self._outcome(ActionResult(True, False, ""), "tavern.draw.resolved", command.action_id, events)
        if isinstance(command, DrawExchangeCommand):
            view = self.tavern_draw_view()
            if ("draw.exchange" not in view.legal_actions or not isinstance(command.card_ids, tuple)
                    or len(command.card_ids) != len(set(command.card_ids))
                    or any(not isinstance(card_id, str) for card_id in command.card_ids)):
                return self._reject("tavern.draw.rejected")
            card_index = {card.card_id: index for index, card in enumerate(view.hand)}
            if any(card_id not in card_index for card_id in command.card_ids):
                return self._reject("tavern.draw.invalid")
            from .tavern_draw import draw_cards, drive_npcs
            try:
                draw_cards(self._state, [card_index[card_id] for card_id in command.card_ids])
                drive_npcs(self._state)
            except ValueError:
                return self._reject("tavern.draw.rejected")
            updated = self.tavern_draw_view()
            events: tuple[RuntimeEvent, ...] = (TavernCardsExchanged(
                self._state.active_courier_id or "courier", command.card_ids),)
            if updated.phase_id == "draw.phase.complete":
                events += (TavernGameSettled("draw", updated.winner_ids),)
            return self._outcome(ActionResult(True, False, ""), "tavern.draw.exchanged", events=events)
        if isinstance(command, DiceActionCommand):
            view = self.tavern_dice_view()
            if command.action_id not in view.legal_actions:
                return self._reject("tavern.dice.rejected", command.action_id)
            from .tavern_dice import drive_npcs, hold, roll
            events: tuple[RuntimeEvent, ...] = ()
            try:
                if command.action_id == "dice.roll":
                    dice = roll(self._state)
                    total = self._state.tavern_dice["active_match"]["turn_total"]
                    events = (TavernDiceRolled(self._state.active_courier_id or "courier", dice, total),)
                elif command.action_id == "dice.hold":
                    hold(self._state)
                else:
                    return self._reject("tavern.dice.invalid", command.action_id)
                drive_npcs(self._state)
            except ValueError:
                return self._reject("tavern.dice.rejected", command.action_id)
            updated = self.tavern_dice_view()
            if updated.phase_id == "dice.phase.complete":
                events += (TavernGameSettled("dice", updated.winner_ids),)
            return self._outcome(ActionResult(True, False, ""), "tavern.dice.resolved", command.action_id, events)
        if isinstance(command, CloseTavernGameCommand):
            if command.game_id == "draw" and "draw.close" in self.tavern_draw_view().legal_actions:
                from .tavern_draw import close_hand
                close_hand(self._state)
            elif command.game_id == "dice" and "dice.close" in self.tavern_dice_view().legal_actions:
                from .tavern_dice import close_match
                close_match(self._state)
            else:
                return self._reject("tavern.close.rejected", command.game_id)
            return self._outcome(ActionResult(True, False, ""), "tavern.closed", command.game_id)
        if isinstance(command, CharacterSetupCommand):
            setup = self.character_setup_view()
            if not setup.available or not isinstance(command.crew_id, str) or not isinstance(command.name, str):
                return self._reject("character.setup.rejected", command.crew_id)
            crew_index = next((index for index, row in enumerate(self._state.household)
                               if row.id == command.crew_id), None)
            if crew_index is None:
                return self._reject("character.setup.rejected", command.crew_id)
            try:
                attributes = dict(command.attributes)
                competencies = dict(command.competencies)
                if (len(attributes) != len(command.attributes)
                        or len(competencies) != len(command.competencies)):
                    raise ValueError("duplicate character allocation identity")
                from .character import apply_character_spec
                apply_character_spec(
                    self._state, crew_index=crew_index, name=command.name,
                    ancestry=command.ancestry_id, origin=command.origin_id, trait=command.trait_id,
                    attributes=attributes, competencies=competencies,
                )
            except (TypeError, ValueError):
                return self._reject("character.setup.rejected", command.crew_id)
            return self._outcome(ActionResult(True, False, ""), "character.setup.completed", command.crew_id)
        if isinstance(command, ActivityCommand):
            if (not isinstance(command.context_id, str) or not command.context_id
                    or not isinstance(command.action_id, str) or not command.action_id):
                return self._reject("activity.invalid")
            if command.target_position is not None and not hasattr(command.target_position, "x"):
                return self._reject("activity.target.invalid", command.action_id)
            from .activities import resolve_activity
            resolution = resolve_activity(
                self._state, command.context_id, command.action_id,
                command.target_position, command.target_actor_id,
            )
            if not resolution.changed:
                return self._reject("activity.rejected", command.action_id)
            target_id = command.target_actor_id
            event = GameplayActivityResolved(command.context_id, command.action_id, target_id)
            return self._outcome(
                ActionResult(True, resolution.time_advanced, resolution.message),
                "activity.resolved", command.action_id, (event,),
            )
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
