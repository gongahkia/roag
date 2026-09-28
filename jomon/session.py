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
    IntegrateNeuralRecordsCommand,
    InteractCommand,
    MoveCommand,
    RecoverRemainsItemCommand,
    SelectSuccessorCommand,
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
    NeuralRecordsIntegrated,
    ObjectiveAcquired,
    OperationDelivered,
    RemainsItemRecovered,
    RuntimeEvent,
    TravelResolved,
    SuccessorSelected,
)
from .save import load_game, save_game
from .state import Actor, CrewMember, Feature, GameState, Item, NeuralRecord, OperationState, Position, create_world
from .views import (
    activity_views,
    actor_views,
    character_setup_view,
    effective_neural_capabilities,
    feature_views,
    item_detail_view,
    inventory_view,
    NeuralIntegrationPreviewView,
    NeuralIntegrationSiteView,
    NeuralRecordView,
    neural_record_view,
    operation_views,
    quest_views,
    recipe_view,
    travel_view,
    world_view,
)


MELEE_DIAGONAL_CAPABILITY_ID = "capability.melee-diagonal"


@dataclass(frozen=True)
class CommandOutcome:
    accepted: bool
    changed: bool
    time_advanced: bool
    result_id: str
    revision: int
    events: tuple[RuntimeEvent, ...] = ()


@dataclass(frozen=True)
class _NeuralIntegrationEvaluation:
    """The reducer's proposed transfer, kept separate from its mutation step."""
    result_id: str
    feature: Feature | None = None
    recipient: CrewMember | None = None
    destination: Item | None = None
    source: Item | None = None
    protected_records: tuple[NeuralRecord, ...] = ()
    retained_records: tuple[NeuralRecord, ...] = ()
    source_records: tuple[NeuralRecord, ...] = ()
    candidates: tuple[NeuralRecord, ...] = ()
    selected_record_ids: tuple[str, ...] = ()
    result_records: tuple[NeuralRecord, ...] = ()
    removed_destination_records: tuple[NeuralRecord, ...] = ()
    omitted_source_records: tuple[NeuralRecord, ...] = ()
    inherited_capacity: int | None = None

    @property
    def confirmable(self) -> bool:
        return self.result_id == "neural.integration.completed"


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
        if identity == "courier":
            return next((row for row in self.actor_views() if row.actor_kind == "courier"), None)
        return next((row for row in self.actor_views() if row.id == identity), None)

    def inventory_view(self):
        return inventory_view(self._state)

    def item_detail_view(self, item_id: str):
        return item_detail_view(self._state, item_id)

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

    def effective_neural_capabilities(self, member_id: str | None = None) -> tuple[str, ...]:
        """Expose the same installed-device-only capability projection to callers."""
        if member_id is None:
            return effective_neural_capabilities(self._state)
        member = next((row for row in self._state.crew if row.id == member_id), None)
        return effective_neural_capabilities(self._state, member) if member is not None else ()

    def crew_views(self):
        from .views import crew_views
        return crew_views(self._state)

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

    @staticmethod
    def _feature_required_capability(feature: Feature) -> str | None:
        world = selected_content_pack().systems.get("world", {})
        if not isinstance(world, dict):
            return None
        definition = next((row for row in world.get("features", ()) if row.get("id") == feature.id), None)
        required = definition.get("requires_capability_id") if isinstance(definition, dict) else None
        return required if isinstance(required, str) else None

    def _all_items(self):
        return tuple(item for member in self._state.crew for item in member.items) if self._state.crew else tuple(self._state.items)

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
        return any(actor.alive and actor.position == point for actor in self._state.actors) or any(
            member.alive and member.id != self._state.active_member_id and member.position == point
            for member in self._state.crew
        )

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

    def _select_successor(self, command: SelectSuccessorCommand) -> CommandOutcome:
        if self._state.courier.alive:
            return self._reject("successor.current-alive")
        member = next((row for row in self._state.crew if row.id == command.member_id), None)
        if member is None:
            return self._reject("successor.unknown-member")
        if not member.alive or member.id == self._state.active_member_id:
            return self._reject("successor.ineligible")
        self._state.active_member_id = member.id
        self._state.position = member.position
        self._state.remembered.add(member.position)
        return self._out(True, "successor.selected", changed=True, events=(SuccessorSelected("successor.selected", member.id),))

    def _recover_remains_item(self, command: RecoverRemainsItemCommand) -> CommandOutcome:
        if not self._state.courier.alive:
            return self._reject("courier.dead")
        source = next((row for row in self._state.crew if row.id == command.member_id), None)
        if source is None or source.alive:
            return self._reject("remains.unknown")
        if not self._in_range(source.position):
            return self._reject("remains.out-of-range")
        item = next((row for row in source.items if row.id == command.item_id), None)
        if item is None:
            return self._reject("remains.item-unavailable")
        source.items.remove(item)
        if source.installed_neural_item_id == item.id:
            source.installed_neural_item_id = None
        item.equipped = False
        self._state.items.append(item)
        return self._advance("remains.item-recovered", (RemainsItemRecovered("remains.item-recovered", source.id, item.id),))

    def _neural_integration_configuration(self) -> tuple[dict | None, dict | None]:
        neural = selected_content_pack().systems.get("neural")
        if not isinstance(neural, dict):
            return None, None
        integration = neural.get("integration")
        return neural, integration if isinstance(integration, dict) else None

    def _evaluate_neural_integration(
        self, command: IntegrateNeuralRecordsCommand,
    ) -> _NeuralIntegrationEvaluation:
        """Validate and calculate one transfer without mutating canonical state.

        This is deliberately the single authority for the preview and the reducer.
        """
        neural, integration = self._neural_integration_configuration()
        if neural is None or integration is None:
            return _NeuralIntegrationEvaluation("neural.integration-unavailable")
        if not isinstance(command.retained_record_ids, tuple):
            return _NeuralIntegrationEvaluation("neural.integration.invalid-selection")
        if any(not isinstance(identity, str) or not identity for identity in command.retained_record_ids):
            return _NeuralIntegrationEvaluation("neural.integration.invalid-selection")
        if len(set(command.retained_record_ids)) != len(command.retained_record_ids):
            return _NeuralIntegrationEvaluation("neural.integration.duplicate-selection")
        feature = self._feature(command.site_feature_id)
        if feature is None or command.site_feature_id not in integration["site_feature_ids"]:
            return _NeuralIntegrationEvaluation("neural.integration.invalid-site")
        if not self._in_range(feature.position):
            return _NeuralIntegrationEvaluation("neural.integration.out-of-range", feature=feature)
        recipient = self._state.courier
        if not isinstance(recipient, CrewMember) or recipient.installed_neural_item_id is None:
            return _NeuralIntegrationEvaluation("neural.integration.recipient-unavailable", feature=feature)
        destination = next((item for item in self._state.items if item.id == recipient.installed_neural_item_id), None)
        if destination is None or destination.neural_records is None or destination.equipped:
            return _NeuralIntegrationEvaluation(
                "neural.integration.recipient-unavailable", feature=feature, recipient=recipient,
            )
        if command.source_item_id == destination.id:
            return _NeuralIntegrationEvaluation(
                "neural.integration.invalid-source", feature=feature, recipient=recipient, destination=destination,
            )
        source = next((item for item in self._state.items if item.id == command.source_item_id), None)
        if source is None or source.neural_records is None or source.equipped:
            return _NeuralIntegrationEvaluation(
                "neural.integration.source-unavailable", feature=feature, recipient=recipient, destination=destination,
            )
        if not source.neural_records:
            return _NeuralIntegrationEvaluation(
                "neural.integration.source-empty", feature=feature, recipient=recipient,
                destination=destination, source=source,
            )
        if any(member.installed_neural_item_id == source.id for member in self._state.crew):
            return _NeuralIntegrationEvaluation(
                "neural.integration.source-installed", feature=feature, recipient=recipient,
                destination=destination, source=source,
            )

        definition_ids = {row["id"] for row in neural["record_definitions"]}
        member_ids = {member.id for member in self._state.crew}
        records_by_id: dict[str, NeuralRecord] = {}
        protected: dict[str, NeuralRecord] = {}
        destination_ids: set[str] = set()
        for record in destination.neural_records:
            if (not record.id or not record.origin_member_id or not record.definition_id
                    or record.origin_member_id not in member_ids or record.definition_id not in definition_ids):
                return _NeuralIntegrationEvaluation(
                    "neural.integration.invalid-record", feature, recipient, destination, source,
                )
            if record.id in destination_ids:
                return _NeuralIntegrationEvaluation(
                    "neural.integration.duplicate-record", feature, recipient, destination, source,
                )
            destination_ids.add(record.id)
            records_by_id[record.id] = record
            if record.origin_member_id == recipient.id:
                protected[record.id] = record
        source_ids: set[str] = set()
        for record in source.neural_records:
            if (not record.id or not record.origin_member_id or not record.definition_id
                    or record.origin_member_id not in member_ids or record.definition_id not in definition_ids):
                return _NeuralIntegrationEvaluation(
                    "neural.integration.invalid-record", feature, recipient, destination, source,
                )
            if record.id in source_ids:
                return _NeuralIntegrationEvaluation(
                    "neural.integration.duplicate-record", feature, recipient, destination, source,
                )
            source_ids.add(record.id)
            prior = records_by_id.get(record.id)
            if prior is not None and prior != record:
                return _NeuralIntegrationEvaluation(
                    "neural.integration.conflicting-record", feature, recipient, destination, source,
                )
            records_by_id[record.id] = record
        candidates = {identity: record for identity, record in records_by_id.items() if identity not in protected}
        protected_records = tuple(protected[identity] for identity in sorted(protected))
        retained_records = tuple(
            record for record in destination.neural_records if record.origin_member_id != recipient.id
        )
        source_records = tuple(source.neural_records)
        candidate_records = tuple(candidates[identity] for identity in sorted(candidates))
        if any(identity not in candidates for identity in command.retained_record_ids):
            return _NeuralIntegrationEvaluation(
                "neural.integration.unknown-record", feature, recipient, destination, source,
                protected_records, retained_records, source_records, candidate_records,
                command.retained_record_ids, inherited_capacity=integration["inherited_capacity"],
            )
        result_by_id = dict(protected)
        result_by_id.update({identity: candidates[identity] for identity in command.retained_record_ids})
        result = tuple(result_by_id[identity] for identity in sorted(result_by_id))
        result_ids = {record.id for record in result}
        removed_destination = tuple(
            record for record in destination.neural_records
            if record.origin_member_id != recipient.id and record.id not in result_ids
        )
        omitted_source = tuple(record for record in source.neural_records if record.id not in result_ids)
        evaluation = _NeuralIntegrationEvaluation(
            "neural.integration.completed", feature, recipient, destination, source,
            protected_records, retained_records, source_records, candidate_records,
            command.retained_record_ids, result, removed_destination, omitted_source,
            integration["inherited_capacity"],
        )
        if sum(record.origin_member_id != recipient.id for record in result) > integration["inherited_capacity"]:
            return _NeuralIntegrationEvaluation(
                "neural.integration.over-capacity", feature, recipient, destination, source,
                protected_records, retained_records, source_records, candidate_records,
                command.retained_record_ids, result, removed_destination, omitted_source,
                integration["inherited_capacity"],
            )
        return evaluation

    def neural_integration_preview(
        self, site_feature_id: str, source_item_id: str, retained_record_ids: tuple[str, ...],
    ) -> NeuralIntegrationPreviewView:
        """Expose a presentation-only transfer preview with no result/event side effect."""
        evaluation = self._evaluate_neural_integration(
            IntegrateNeuralRecordsCommand(site_feature_id, source_item_id, retained_record_ids),
        )
        neural, integration = self._neural_integration_configuration()
        feature_definitions = {
            row["id"]: row for row in selected_content_pack().systems.get("world", {}).get("features", [])
        }
        site_ids = integration.get("site_feature_ids", ()) if integration is not None else ()
        sites = tuple(
            NeuralIntegrationSiteView(
                identity,
                str(feature_definitions.get(identity, {}).get("name", identity)),
                (feature := self._feature(identity)) is not None and self._in_range(feature.position),
            )
            for identity in sorted(site_ids)
            if isinstance(identity, str)
        )
        selected_ids = tuple(identity for identity in retained_record_ids if isinstance(identity, str))
        capacity = evaluation.inherited_capacity
        foreign_used = sum(
            record.origin_member_id != evaluation.recipient.id
            for record in evaluation.result_records
        ) if evaluation.recipient is not None else 0
        item_definitions = {row["id"]: row for row in selected_content_pack().systems.get("items", [])}
        crew_definitions = {row["id"]: row for row in selected_content_pack().systems.get("crew", [])}
        feature = evaluation.feature
        return NeuralIntegrationPreviewView(
            self._revision,
            site_feature_id,
            str(feature_definitions.get(site_feature_id, {}).get("name", site_feature_id)) if feature else None,
            sites,
            evaluation.recipient.id if evaluation.recipient is not None else None,
            str(crew_definitions.get(evaluation.recipient.id, {}).get("name", evaluation.recipient.id)) if evaluation.recipient is not None else None,
            evaluation.destination.id if evaluation.destination is not None else None,
            source_item_id,
            str(item_definitions.get(evaluation.source.kind, {}).get("name", evaluation.source.kind)) if evaluation.source is not None else None,
            evaluation.result_id,
            evaluation.confirmable,
            capacity,
            foreign_used,
            max(0, capacity - foreign_used) if capacity is not None else 0,
            tuple(neural_record_view(self._state, record) for record in evaluation.protected_records),
            tuple(neural_record_view(self._state, record) for record in evaluation.retained_records),
            tuple(neural_record_view(self._state, record) for record in evaluation.source_records),
            tuple(neural_record_view(self._state, record) for record in evaluation.candidates),
            selected_ids,
            tuple(neural_record_view(self._state, record) for record in evaluation.result_records),
            tuple(neural_record_view(self._state, record) for record in evaluation.removed_destination_records),
            tuple(neural_record_view(self._state, record) for record in evaluation.omitted_source_records),
            evaluation.source is not None and evaluation.result_id in {
                "neural.integration.completed", "neural.integration.over-capacity",
            },
        )

    def _integrate_neural_records(self, command: IntegrateNeuralRecordsCommand) -> CommandOutcome:
        """Commit the evaluator's already-described payload change, then take one turn."""
        evaluation = self._evaluate_neural_integration(command)
        if not evaluation.confirmable:
            return self._reject(evaluation.result_id)
        assert evaluation.feature is not None
        assert evaluation.recipient is not None
        assert evaluation.destination is not None
        assert evaluation.source is not None
        evaluation.destination.neural_records = evaluation.result_records
        evaluation.source.neural_records = ()
        retained = tuple(
            record.id for record in evaluation.result_records
            if record.origin_member_id != evaluation.recipient.id
        )
        return self._advance(
            "neural.integration.completed",
            (NeuralRecordsIntegrated(
                "neural.records-integrated", evaluation.feature.id, evaluation.source.id,
                evaluation.destination.id, retained,
            ),),
        )

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
            required_capability = self._feature_required_capability(feature)
            if required_capability is not None and required_capability not in effective_neural_capabilities(self._state):
                return self._reject("interaction.requires-capability")
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
            if any(item.kind == feature.item_id for item in self._all_items()):
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

        if isinstance(command, SelectSuccessorCommand):
            return self._select_successor(command)

        if isinstance(command, RecoverRemainsItemCommand):
            return self._recover_remains_item(command)

        if not state.courier.alive:
            return self._reject("courier.dead")

        if isinstance(command, IntegrateNeuralRecordsCommand):
            return self._integrate_neural_records(command)

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
            if target is None:
                return self._reject("attack.rejected")
            dx = abs(target.position.x - state.position.x)
            dy = abs(target.position.y - state.position.y)
            cardinal = dx + dy == 1
            diagonal = (
                dx == 1 and dy == 1
                and MELEE_DIAGONAL_CAPABILITY_ID in effective_neural_capabilities(state)
            )
            if not cardinal and not diagonal:
                return self._reject("attack.rejected")
            power = max((item.power for item in state.items if item.equipped), default=1)
            target.health = max(0, target.health - power)
            target.alive = target.health > 0
            if not target.alive:
                self._record_defeat_evidence(target.id)
            return self._advance("attack.resolved", (AttackResolved("attack.resolved", "courier", target.id, power),))

        if isinstance(command, EquipItemCommand):
            item = next((row for row in state.items if row.id == command.item_id), None)
            if item is None:
                return self._reject("item.rejected")
            if item.neural_records is not None:
                return self._reject("item.neural-payload-not-equippable")
            if item.equipped:
                return self._reject("item.rejected")
            item.equipped = True
            return self._out(True, "item.equipped", changed=True, events=(ItemEquipped("item.equipped", item.id),))

        if isinstance(command, UnequipItemCommand):
            item = next((row for row in state.items if row.id == command.item_id), None)
            if item is None:
                return self._reject("item.rejected")
            if item.neural_records is not None:
                return self._reject("item.neural-payload-not-equippable")
            if not item.equipped:
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
