"""Deterministic application boundary over content-pack mechanics."""
from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

from .catalog import selected_content_pack
from .commands import (
    ActivityCommand,
    AttackCommand,
    CharacterSetupCommand,
    CraftCommand,
    EquipItemCommand,
    InteractCommand,
    MoveCommand,
    TravelCommand,
    UnequipItemCommand,
)
from .runtime_events import (
    AccessOpened,
    ActivityResolved,
    ActorMoved,
    AttackResolved,
    CraftResolved,
    DefenderResponded,
    ItemEquipped,
    ObjectiveAcquired,
    OperationDelivered,
    RuntimeEvent,
    TravelResolved,
)
from .save import load_game, save_game
from .state import Actor, Feature, GameState, Item, OperationState, Position, create_world
from .views import (
    activity_views,
    actor_views,
    character_setup_view,
    feature_views,
    inventory_view,
    operation_views,
    quest_views,
    recipe_view,
    travel_view,
    world_view,
)


@dataclass(frozen=True)
class CommandOutcome:
    accepted: bool
    changed: bool
    time_advanced: bool
    result_id: str
    revision: int
    events: tuple[RuntimeEvent, ...] = ()


class GameSession:
    def __init__(self, state: GameState):
        self._state = state
        self._revision = 0

    @classmethod
    def create(cls, seed: str) -> "GameSession":
        return cls(create_world(seed))

    @classmethod
    def load(cls, path: Path) -> "GameSession":
        return cls(load_game(path))

    @classmethod
    def pending_character_setup_view(cls, seed: str = ""):
        return character_setup_view()

    @classmethod
    def create_configured(cls, seed: str, command: CharacterSetupCommand):
        session = cls.create(seed)
        outcome = session.submit(command)
        if not outcome.accepted:
            raise ValueError("invalid initial character setup")
        return session, outcome

    def save(self, path: Path) -> Path:
        return save_game(self._state, path)

    @property
    def revision(self) -> int:
        return self._revision

    def world_view(self):
        return world_view(self._state)

    def actor_views(self):
        return actor_views(self._state)

    def actor_view(self, identity: str):
        return next((row for row in self.actor_views() if row.id == identity), None)

    def inventory_view(self):
        return inventory_view(self._state)

    def quest_views(self):
        return quest_views(self._state)

    def travel_view(self):
        return travel_view(self._state)

    def recipe_view(self):
        return recipe_view(self._state)

    def activity_views(self, category_id: str):
        return activity_views(self._state, category_id)

    def feature_views(self):
        return feature_views(self._state)

    def operation_views(self):
        return operation_views(self._state)

    def _out(
        self,
        accepted: bool,
        result_id: str,
        *,
        changed: bool = False,
        time_advanced: bool = False,
        events: tuple[RuntimeEvent, ...] = (),
    ) -> CommandOutcome:
        if changed:
            self._revision += 1
        return CommandOutcome(accepted, changed, time_advanced, result_id, self._revision, events)

    def _reject(self, result_id: str) -> CommandOutcome:
        return self._out(False, result_id)

    def _advance(self, result_id: str, events: tuple[RuntimeEvent, ...]) -> CommandOutcome:
        """Advance one player turn, then permit one eligible adjacent response."""
        self._state.turn += 1
        events = events + self._defender_response()
        return self._out(True, result_id, changed=True, time_advanced=True, events=events)

    def _operation_definition(self, operation_id: str) -> dict:
        return next(row for row in selected_content_pack().systems.get("operations", []) if row["id"] == operation_id)

    def _operation(self, operation_id: str) -> OperationState | None:
        return next((row for row in self._state.operations if row.id == operation_id), None)

    def _feature(self, feature_id: str) -> Feature | None:
        return next((row for row in self._state.features if row.id == feature_id), None)

    def _in_range(self, position: Position) -> bool:
        return abs(position.x - self._state.position.x) + abs(position.y - self._state.position.y) <= 1

    def _is_passable(self, point: Position) -> bool:
        if not (0 <= point.y < len(self._state.rows) and 0 <= point.x < len(self._state.rows[0])):
            return False
        tile = self._state.rows[point.y][point.x]
        if tile == ".":
            return True
        return any(
            feature.kind == "access_gate" and feature.position == point and feature.access_id in self._state.opened_access_ids
            for feature in self._state.features
        )

    def _occupied_by_living_actor(self, point: Position) -> bool:
        return any(actor.alive and actor.position == point for actor in self._state.actors)

    def _setup_bonus(self, command: CharacterSetupCommand) -> int:
        setup = selected_content_pack().systems["setup"]
        choices = {
            "crew": command.crew_id,
            "ancestries": command.ancestry_id,
            "origins": command.origin_id,
            "traits": command.trait_id,
        }
        return sum(
            int(next(row for row in setup[section] if row["id"] == identity).get("health_bonus", 0))
            for section, identity in choices.items()
        )

    def _record_access_evidence(self, access_id: str) -> None:
        for operation in self._state.operations:
            definition = self._operation_definition(operation.id)
            for method in definition["methods"]:
                if method["requires_access_id"] == access_id:
                    operation.evidence_method_ids.add(method["id"])
                    operation.consequence_ids.add(method["consequence_id"])

    def _record_defeat_evidence(self, actor_id: str) -> None:
        for operation in self._state.operations:
            definition = self._operation_definition(operation.id)
            for method in definition["methods"]:
                if method["requires_defeated_actor_id"] == actor_id:
                    operation.evidence_method_ids.add(method["id"])
                    operation.consequence_ids.add(method["consequence_id"])

    def _satisfied_methods(self, operation: OperationState) -> tuple[str, ...]:
        definition = self._operation_definition(operation.id)
        output: list[str] = []
        for method in definition["methods"]:
            access_ok = method["requires_access_id"] is not None and method["requires_access_id"] in self._state.opened_access_ids
            actor_id = method["requires_defeated_actor_id"]
            actor = next((row for row in self._state.actors if row.id == actor_id), None)
            actor_ok = actor_id is not None and actor is not None and not actor.alive
            if access_ok or actor_ok:
                output.append(method["id"])
        return tuple(sorted(output))

    def _defender_response(self) -> tuple[RuntimeEvent, ...]:
        """One content-configured living adjacent defender responds after a valid turn."""
        if not self._state.courier.alive:
            return ()
        eligible = sorted(
            (
                actor for actor in self._state.actors
                if actor.alive
                and actor.response_policy == "adjacent-on-valid-action"
                and abs(actor.position.x - self._state.position.x) + abs(actor.position.y - self._state.position.y) == 1
            ),
            key=lambda actor: actor.id,
        )
        if not eligible:
            return ()
        defender = eligible[0]
        damage = defender.response_power
        self._state.courier.health = max(0, self._state.courier.health - damage)
        self._state.courier.alive = self._state.courier.health > 0
        return (DefenderResponded("defender.responded", defender.id, "courier", damage),)

    def _submit_interaction(self, command: InteractCommand) -> CommandOutcome:
        feature = self._feature(command.feature_id)
        if feature is None:
            return self._reject("interaction.unknown-feature")
        if not self._in_range(feature.position):
            return self._reject("interaction.out-of-range")
        if feature.kind == "maintenance_latch":
            if feature.access_id in self._state.opened_access_ids:
                return self._reject("interaction.already-open")
            has_tool = any(
                item.kind == feature.requires_equipped_item_id and item.equipped
                for item in self._state.items
            )
            if not has_tool:
                return self._reject("interaction.requires-equipped-tool")
            assert feature.access_id is not None
            self._state.opened_access_ids.add(feature.access_id)
            self._record_access_evidence(feature.access_id)
            return self._advance("interaction.access-opened", (AccessOpened("access.opened", feature.id, feature.access_id),))
        if feature.kind == "objective_cache":
            assert feature.operation_id is not None and feature.item_id is not None
            operation = self._operation(feature.operation_id)
            if operation is None or operation.state != "assigned":
                return self._reject("interaction.objective-unavailable")
            methods = self._satisfied_methods(operation)
            if not methods:
                return self._reject("interaction.access-required")
            if any(item.kind == feature.item_id for item in self._state.items):
                return self._reject("interaction.objective-already-held")
            instance_id = f"objective.{operation.id}"
            self._state.items.append(Item(instance_id, feature.item_id))
            operation.state = "resolved"
            operation.objective_item_instance_id = instance_id
            operation.resolution_method_ids.update(methods)
            return self._advance("interaction.objective-acquired", (ObjectiveAcquired("objective.acquired", operation.id, feature.item_id, methods),))
        if feature.kind == "base":
            operation = next((row for row in self._state.operations if row.state == "resolved" and self._operation_definition(row.id)["return_feature_id"] == feature.id), None)
            if operation is None:
                return self._reject("interaction.no-delivery")
            definition = self._operation_definition(operation.id)
            objective_item = next((item for item in self._state.items if item.id == operation.objective_item_instance_id and item.kind == definition["objective_item_id"]), None)
            if objective_item is None:
                return self._reject("interaction.objective-missing")
            self._state.items.remove(objective_item)
            operation.state = "returned"
            operation.delivered_item_id = definition["objective_item_id"]
            return self._advance("interaction.operation-delivered", (OperationDelivered("operation.delivered", operation.id, feature.id),))
        return self._reject("interaction.unsupported-feature")

    def submit(self, command) -> CommandOutcome:
        state = self._state
        if isinstance(command, CharacterSetupCommand):
            if state.setup:
                return self._reject("setup.locked")
            view = character_setup_view()
            choices = (view.crew, view.ancestries, view.origins, view.traits)
            values = (command.crew_id, command.ancestry_id, command.origin_id, command.trait_id)
            if not all(any(row.id == value for row in options) for options, value in zip(choices, values)):
                return self._reject("setup.rejected")
            state.setup = dict(zip(("crew", "ancestry", "origin", "trait"), values))
            bonus = self._setup_bonus(command)
            state.courier.maximum_health += bonus
            state.courier.health += bonus
            return self._out(True, "setup.completed", changed=True)

        if not state.courier.alive:
            return self._reject("courier.dead")

        if isinstance(command, MoveCommand):
            target = Position(state.position.x + command.dx, state.position.y + command.dy)
            if not self._is_passable(target):
                return self._reject("move.rejected")
            if self._occupied_by_living_actor(target):
                return self._reject("move.blocked-by-actor")
            before = state.position
            state.position = target
            state.courier.position = target
            state.remembered.add(target)
            return self._advance("move.ok", (ActorMoved("movement.step", "courier", before, target),))

        if isinstance(command, AttackCommand):
            target = next((row for row in state.actors if row.id == command.target_actor_id and row.alive), None)
            if target is None or abs(target.position.x - state.position.x) + abs(target.position.y - state.position.y) > 1:
                return self._reject("attack.rejected")
            power = max((item.power for item in state.items if item.equipped), default=1)
            target.health = max(0, target.health - power)
            target.alive = target.health > 0
            if not target.alive:
                self._record_defeat_evidence(target.id)
            return self._advance("attack.resolved", (AttackResolved("attack.resolved", "courier", target.id, power),))

        if isinstance(command, EquipItemCommand):
            item = next((row for row in state.items if row.id == command.item_id), None)
            if item is None or item.equipped:
                return self._reject("item.rejected")
            item.equipped = True
            return self._out(True, "item.equipped", changed=True, events=(ItemEquipped("item.equipped", item.id),))

        if isinstance(command, UnequipItemCommand):
            item = next((row for row in state.items if row.id == command.item_id), None)
            if item is None or not item.equipped:
                return self._reject("item.rejected")
            item.equipped = False
            return self._out(True, "item.unequipped", changed=True)

        if isinstance(command, InteractCommand):
            return self._submit_interaction(command)

        if isinstance(command, TravelCommand):
            if command.route_id not in state.routes:
                return self._reject("travel.rejected")
            return self._advance("travel.resolved", (TravelResolved("travel.resolved", command.route_id),))

        if isinstance(command, CraftCommand):
            recipe = next((row for row in recipe_view(state) if row.id == command.recipe_id), None)
            if recipe is None or not any(row.kind == recipe.input_id for row in state.items):
                return self._reject("craft.rejected")
            state.items.append(Item(f"crafted-{state.turn}", recipe.output_id))
            return self._advance("craft.resolved", (CraftResolved("craft.resolved", recipe.id, recipe.output_id),))

        if isinstance(command, ActivityCommand):
            if not any(row.id == command.activity_id for row in self.activity_views(command.category_id)):
                return self._reject("activity.rejected")
            return self._advance("activity.resolved", (ActivityResolved("activity.resolved", command.category_id, command.activity_id),))

        return self._reject("command.unsupported")
