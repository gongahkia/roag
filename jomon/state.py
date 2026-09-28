"""Deterministic state for content-pack instances; no setting logic lives here."""
from __future__ import annotations

from dataclasses import asdict, dataclass, field
from typing import Any

from .catalog import mechanical_fingerprint, selected_content_pack


SAVE_FORMAT = 16


class StateError(ValueError):
    pass


class ContentUnavailable(StateError):
    pass


@dataclass(frozen=True)
class Position:
    x: int
    y: int
    z: int = 0


@dataclass
class Item:
    id: str
    kind: str
    quantity: int = 1
    equipped: bool = False
    power: int = 0
    neural_records: tuple[NeuralRecord, ...] | None = None


@dataclass(frozen=True)
class NeuralRecord:
    """A value-like, carried record; it has no effect until a later system uses it."""
    id: str
    origin_member_id: str
    definition_id: str


@dataclass
class Actor:
    id: str
    kind: str
    position: Position
    health: int
    maximum_health: int
    alive: bool = True
    response_policy: str | None = None
    response_power: int = 0

@dataclass
class CrewMember(Actor):
    """A single authoritative person record; their items are personal custody."""
    items: list[Item] = field(default_factory=list)
    installed_neural_item_id: str | None = None


@dataclass
class Quest:
    id: str
    state: str
    progress: int = 0


@dataclass(frozen=True)
class Feature:
    """Pack-defined semantic topology copied into a save-bound world instance."""
    id: str
    kind: str
    position: Position
    access_id: str | None = None
    requires_equipped_item_id: str | None = None
    operation_id: str | None = None
    item_id: str | None = None


@dataclass
class OperationState:
    """Durable operation facts, not a replayable event log."""
    id: str
    state: str = "assigned"
    objective_item_instance_id: str | None = None
    delivered_item_id: str | None = None
    evidence_method_ids: set[str] = field(default_factory=set)
    resolution_method_ids: set[str] = field(default_factory=set)
    consequence_ids: set[str] = field(default_factory=set)


@dataclass
class GameState:
    seed: str
    pack_id: str
    fingerprint: str
    rows: tuple[str, ...]
    position: Position
    courier_legacy: Actor | None
    items_legacy: list[Item]
    actors: list[Actor]
    quests: list[Quest]
    routes: tuple[str, ...]
    recipes: tuple[str, ...]
    turn: int = 0
    setup: dict[str, str] = field(default_factory=dict)
    remembered: set[Position] = field(default_factory=set)
    features: tuple[Feature, ...] = ()
    opened_access_ids: set[str] = field(default_factory=set)
    operations: list[OperationState] = field(default_factory=list)
    crew: list[CrewMember] = field(default_factory=list)
    active_member_id: str | None = None

    @property
    def courier(self) -> Actor:
        if self.crew:
            member = next((row for row in self.crew if row.id == self.active_member_id), None)
            if member is None:
                raise StateError("missing active crew member")
            return member
        if self.courier_legacy is None:
            raise StateError("missing courier")
        return self.courier_legacy

    @property
    def items(self) -> list[Item]:
        return self.courier.items if self.crew else self.items_legacy

    def to_dict(self) -> dict[str, Any]:
        _validate_active_crew_position(self)
        _validate_neural_payloads(self)
        _validate_installed_neural_items(self)
        _validate_installed_neural_capacity(self)
        return {
            "format": SAVE_FORMAT,
            "seed": self.seed,
            "pack_id": self.pack_id,
            "fingerprint": self.fingerprint,
            "rows": list(self.rows),
            "position": asdict(self.position),
            **({"crew": [_crew_to_dict(member) for member in self.crew], "active_member_id": self.active_member_id}
               if self.crew else {"courier": asdict(self.courier), "items": [_item_to_dict(item) for item in self.items]}),
            "actors": [asdict(actor) for actor in self.actors],
            "quests": [asdict(quest) for quest in self.quests],
            "routes": list(self.routes),
            "recipes": list(self.recipes),
            "turn": self.turn,
            "setup": dict(self.setup),
            "remembered": [asdict(item) for item in sorted(self.remembered, key=lambda point: (point.y, point.x, point.z))],
            "features": [asdict(feature) for feature in self.features],
            "opened_access_ids": sorted(self.opened_access_ids),
            "operations": [
                {
                    "id": operation.id,
                    "state": operation.state,
                    "objective_item_instance_id": operation.objective_item_instance_id,
                    "delivered_item_id": operation.delivered_item_id,
                    "evidence_method_ids": sorted(operation.evidence_method_ids),
                    "resolution_method_ids": sorted(operation.resolution_method_ids),
                    "consequence_ids": sorted(operation.consequence_ids),
                }
                for operation in self.operations
            ],
        }

    def remember_current_visibility(self) -> None:
        """Record terrain presently visible under the provisional radius rule.

        This is terrain knowledge only.  Actors and remains intentionally have no
        remembered representation, so a renderer cannot turn this into hidden
        actor knowledge.
        """
        for y, row in enumerate(self.rows):
            for x, _ in enumerate(row):
                point = Position(x, y)
                if abs(point.x - self.position.x) + abs(point.y - self.position.y) <= 5:
                    self.remembered.add(point)


def _validate_active_crew_position(state: GameState) -> None:
    """Keep the persisted active-position mirror explicit and unambiguous."""
    if not state.crew:
        return
    active = next((member for member in state.crew if member.id == state.active_member_id), None)
    if active is None:
        raise StateError("missing active crew member")
    if state.position != active.position:
        raise StateError("crew-bearing state has mismatched active position")


def _validate_neural_records(records: object) -> tuple[NeuralRecord, ...] | None:
    if records is None:
        return None
    if not isinstance(records, tuple) or any(not isinstance(record, NeuralRecord) for record in records):
        raise StateError("invalid neural payload")
    if any(
        not isinstance(value, str) or not value
        for record in records
        for value in (record.id, record.origin_member_id, record.definition_id)
    ):
        raise StateError("invalid neural payload")
    if len({record.id for record in records}) != len(records):
        raise StateError("duplicate neural record id")
    return records


def _validate_neural_payloads(state: GameState) -> None:
    member_ids = {member.id for member in state.crew}
    neural = selected_content_pack().systems.get("neural")
    definition_ids = None if neural is None else {row["id"] for row in neural["record_definitions"]}
    custodial_items = [item for member in state.crew for item in member.items] if state.crew else state.items_legacy
    for item in custodial_items:
        records = _validate_neural_records(item.neural_records)
        if records is not None and any(record.origin_member_id not in member_ids for record in records):
            raise StateError("neural payload has unknown origin")
        if records is not None and definition_ids is not None and any(record.definition_id not in definition_ids for record in records):
            raise StateError("neural payload has unknown definition")
        if records is not None and item.equipped:
            raise StateError("neural payload item cannot be equipped")


def _validate_installed_neural_items(state: GameState) -> None:
    for member in state.crew:
        installed_id = member.installed_neural_item_id
        if installed_id is None:
            continue
        if not isinstance(installed_id, str) or not installed_id:
            raise StateError("invalid installed neural item reference")
        matches = [item for item in member.items if item.id == installed_id]
        if len(matches) != 1 or matches[0].neural_records is None:
            raise StateError("invalid installed neural item reference")
        if matches[0].equipped:
            raise StateError("installed neural item cannot be equipped")


def _validate_installed_neural_capacity(state: GameState) -> None:
    """Enforce an opted-in pack's foreign-record limit on installed devices only."""
    neural = selected_content_pack().systems.get("neural")
    integration = neural.get("integration") if isinstance(neural, dict) else None
    if integration is None:
        return
    capacity = integration["inherited_capacity"]
    for member in state.crew:
        installed_id = member.installed_neural_item_id
        if installed_id is None:
            continue
        item = next(item for item in member.items if item.id == installed_id)
        assert item.neural_records is not None
        if sum(record.origin_member_id != member.id for record in item.neural_records) > capacity:
            raise StateError("installed neural payload exceeds inherited capacity")


def _item_to_dict(item: Item) -> dict[str, object]:
    records = _validate_neural_records(item.neural_records)
    row: dict[str, object] = {
        "id": item.id,
        "kind": item.kind,
        "quantity": item.quantity,
        "equipped": item.equipped,
        "power": item.power,
    }
    if records is not None:
        row["neural_records"] = [asdict(record) for record in records]
    return row


def _crew_to_dict(member: CrewMember) -> dict[str, object]:
    row = asdict(member)
    row["items"] = [_item_to_dict(item) for item in member.items]
    return row


def _position(value: object, context: str) -> Position:
    if not isinstance(value, dict) or set(value) != {"x", "y", "z"} or any(type(value[key]) is not int for key in value):
        raise StateError(f"invalid {context}")
    return Position(**value)


def _actor(row: object) -> Actor:
    if not isinstance(row, dict):
        raise StateError("invalid actor")
    try:
        return Actor(
            str(row["id"]), str(row["kind"]), _position(row["position"], "actor position"),
            int(row["health"]), int(row["maximum_health"]), bool(row["alive"]),
            row.get("response_policy"), int(row.get("response_power", 0)),
        )
    except (KeyError, TypeError, ValueError) as exc:
        raise StateError("invalid actor") from exc


def _neural_records(value: object) -> tuple[NeuralRecord, ...]:
    if not isinstance(value, (list, tuple)):
        raise StateError("invalid neural payload")
    records: list[NeuralRecord] = []
    for row in value:
        if not isinstance(row, dict) or set(row) != {"id", "origin_member_id", "definition_id"}:
            raise StateError("invalid neural payload")
        fields = (row["id"], row["origin_member_id"], row["definition_id"])
        if any(not isinstance(field, str) or not field for field in fields):
            raise StateError("invalid neural payload")
        records.append(NeuralRecord(*fields))
    result = tuple(records)
    _validate_neural_records(result)
    return result


def _item(row: object) -> Item:
    if not isinstance(row, dict):
        raise StateError("invalid item")
    values = dict(row)
    if "neural_records" in values:
        values["neural_records"] = _neural_records(values["neural_records"])
    try:
        return Item(**values)
    except (TypeError, ValueError) as exc:
        raise StateError("invalid item") from exc

def _crew_member(row: object) -> CrewMember:
    actor = _actor(row)
    if not isinstance(row, dict) or not isinstance(row.get("items"), list):
        raise StateError("invalid crew member")
    installed_id = row.get("installed_neural_item_id")
    if installed_id is not None and (not isinstance(installed_id, str) or not installed_id):
        raise StateError("invalid installed neural item reference")
    return CrewMember(actor.id, actor.kind, actor.position, actor.health, actor.maximum_health,
                      actor.alive, actor.response_policy, actor.response_power,
                      [_item(item) for item in row["items"]], installed_id)


def _feature(row: object) -> Feature:
    if not isinstance(row, dict) or set(row) != {"id", "kind", "position", "access_id", "requires_equipped_item_id", "operation_id", "item_id"}:
        raise StateError("invalid feature")
    return Feature(
        str(row["id"]), str(row["kind"]), _position(row["position"], "feature position"),
        row["access_id"], row["requires_equipped_item_id"], row["operation_id"], row["item_id"],
    )


def _operation(row: object) -> OperationState:
    if not isinstance(row, dict) or set(row) != {
        "id", "state", "objective_item_instance_id", "delivered_item_id",
        "evidence_method_ids", "resolution_method_ids", "consequence_ids",
    }:
        raise StateError("invalid operation state")
    if row["state"] not in {"assigned", "resolved", "returned"}:
        raise StateError("invalid operation lifecycle state")
    collections = ("evidence_method_ids", "resolution_method_ids", "consequence_ids")
    if any(not isinstance(row[key], list) or any(not isinstance(value, str) for value in row[key]) for key in collections):
        raise StateError("invalid operation evidence")
    if row["objective_item_instance_id"] is not None and not isinstance(row["objective_item_instance_id"], str):
        raise StateError("invalid operation objective item")
    if row["delivered_item_id"] is not None and not isinstance(row["delivered_item_id"], str):
        raise StateError("invalid operation delivered item")
    return OperationState(
        str(row["id"]), str(row["state"]), row["objective_item_instance_id"], row["delivered_item_id"],
        set(row["evidence_method_ids"]), set(row["resolution_method_ids"]), set(row["consequence_ids"]),
    )


def _feature_from_pack(row: dict[str, Any]) -> Feature:
    return Feature(
        row["id"], row["kind"], Position(*row["position"]), row["access_id"],
        row["requires_equipped_item_id"], row["operation_id"], row["item_id"],
    )


def create_world(seed: str) -> GameState:
    pack = selected_content_pack()
    if not pack.playable:
        raise ContentUnavailable("No playable content pack installed")
    world = pack.systems["world"]
    assert isinstance(world, dict)
    start = Position(*world["start"])
    items = [
        Item(row["id"], row["id"], 1, False, int(row.get("power", 0)))
        for row in pack.systems["items"]
        if row.get("initial", True)
    ]
    actors = [
        Actor(
            row["id"], str(row.get("kind", "neutral")), Position(*row["position"]),
            int(row.get("health", 1)), int(row.get("health", 1)), True,
            row.get("response_policy"), int(row.get("response_power", 0)),
        )
        for row in pack.systems["actors"]
    ]
    crew_definitions = pack.systems.get("crew", [])
    crew = [
        CrewMember(row["id"], str(row.get("kind", "crew")), Position(*row["position"]),
                   int(row["health"]), int(row["health"]), True, None, 0,
                   [Item(f"{row['id']}:{kind}", kind, 1, False, int(next(item.get('power', 0) for item in pack.systems['items'] if item['id'] == kind))) for kind in row["items"]])
        for row in crew_definitions
    ]
    initial = next((row for row in crew if row.id == crew_definitions[0]["id"]), None) if crew_definitions else None
    if initial is not None:
        initial.position = start
        initial.items = items
    neural = pack.systems.get("neural")
    if neural is not None:
        members = {member.id: member for member in crew}
        for initializer in neural["crew_initializers"]:
            member = members[initializer["member_id"]]
            carrier_kind = initializer["carrier_item_kind_id"]
            device_id = f"{member.id}:{carrier_kind}"
            records = tuple(
                NeuralRecord(f"{member.id}:{definition_id}", member.id, definition_id)
                for definition_id in initializer["record_definition_ids"]
            )
            member.items.append(Item(device_id, carrier_kind, neural_records=records))
            member.installed_neural_item_id = device_id
    courier = Actor("courier", "courier", start, 10, 10)
    state = GameState(
        seed=seed,
        pack_id=pack.id,
        fingerprint=mechanical_fingerprint(pack),
        rows=tuple(world["rows"]),
        position=start,
        courier_legacy=None if crew else courier,
        items_legacy=[] if crew else items,
        actors=actors,
        quests=[Quest(row["id"], str(row.get("state", "active"))) for row in pack.systems["quests"]],
        routes=tuple(row["id"] for row in pack.systems["routes"]),
        recipes=tuple(row["id"] for row in pack.systems["recipes"]),
        features=tuple(_feature_from_pack(row) for row in world.get("features", [])),
        operations=[OperationState(row["id"]) for row in pack.systems.get("operations", [])],
        crew=crew,
        active_member_id=initial.id if initial else None,
    )
    state.remember_current_visibility()
    return state


def game_state_from_dict(value: object) -> GameState:
    if not isinstance(value, dict) or value.get("format") != SAVE_FORMAT:
        raise StateError("Save belongs to pre-reset content baseline and cannot be loaded")
    pack = selected_content_pack()
    if value.get("pack_id") != pack.id or value.get("fingerprint") != mechanical_fingerprint(pack):
        raise StateError("save requires a different playable content pack")
    has_operations = bool(pack.systems.get("operations", []))
    has_crew = bool(pack.systems.get("crew", []))
    if has_operations and ("features" not in value or "opened_access_ids" not in value or "operations" not in value):
        raise StateError("operation-bearing save is missing required operation state")
    if has_crew and ("crew" not in value or "active_member_id" not in value):
        raise StateError("crew-bearing save is missing continuity state")
    try:
        state = GameState(
            seed=str(value["seed"]),
            pack_id=str(value["pack_id"]),
            fingerprint=str(value["fingerprint"]),
            rows=tuple(value["rows"]),
            position=_position(value["position"], "position"),
            courier_legacy=None if "crew" in value else _actor(value["courier"]),
            items_legacy=[] if "crew" in value else [_item(row) for row in value["items"]],
            actors=[_actor(row) for row in value["actors"]],
            quests=[Quest(**row) for row in value["quests"]],
            routes=tuple(value["routes"]),
            recipes=tuple(value["recipes"]),
            turn=int(value["turn"]),
            setup=dict(value["setup"]),
            remembered={_position(row, "remembered") for row in value["remembered"]},
            features=tuple(_feature(row) for row in value.get("features", [])),
            opened_access_ids={str(row) for row in value.get("opened_access_ids", [])},
            operations=[_operation(row) for row in value.get("operations", [])],
            crew=[_crew_member(row) for row in value.get("crew", [])],
            active_member_id=value.get("active_member_id"),
        )
    except (KeyError, TypeError, ValueError, StateError) as exc:
        raise StateError("invalid reset-baseline save") from exc
    if has_crew:
        expected = {row["id"] for row in pack.systems["crew"]}
        if {row.id for row in state.crew} != expected or state.active_member_id not in expected:
            raise StateError("crew-bearing save has invalid member identities")
        _validate_active_crew_position(state)
        if len({item.id for member in state.crew for item in member.items}) != sum(len(member.items) for member in state.crew):
            raise StateError("crew-bearing save duplicates item custody")
        for member in state.crew:
            if member.health < 0 or member.maximum_health < 1 or member.health > member.maximum_health or member.alive != (member.health > 0):
                raise StateError("crew-bearing save has invalid condition")
    _validate_neural_payloads(state)
    _validate_installed_neural_items(state)
    _validate_installed_neural_capacity(state)
    if has_operations:
        expected_features = tuple(_feature_from_pack(row) for row in pack.systems["world"].get("features", []))
        expected_feature_ids = {row.id for row in expected_features}
        expected_operation_ids = {row["id"] for row in pack.systems["operations"]}
        if (state.features != expected_features or {row.id for row in state.features} != expected_feature_ids
                or {row.id for row in state.operations} != expected_operation_ids):
            raise StateError("operation-bearing save does not match content definitions")
        access_ids = {
            feature.access_id for feature in expected_features
            if feature.kind == "maintenance_latch" and feature.access_id is not None
        }
        if not state.opened_access_ids.issubset(access_ids):
            raise StateError("operation-bearing save has unknown access state")
        definitions = {row["id"]: row for row in pack.systems["operations"]}
        for operation in state.operations:
            definition = definitions[operation.id]
            methods = {row["id"]: row for row in definition["methods"]}
            method_ids = set(methods)
            consequence_ids = {row["consequence_id"] for row in methods.values()}
            if (not operation.evidence_method_ids.issubset(method_ids)
                    or not operation.resolution_method_ids.issubset(operation.evidence_method_ids)
                    or not operation.consequence_ids.issubset(consequence_ids)):
                raise StateError("operation-bearing save has invalid operation evidence")
            objective_instance = f"objective.{operation.id}"
            custodial_items = [item for member in state.crew for item in member.items] if state.crew else state.items
            objective_items = [item for item in custodial_items if item.id == objective_instance or item.kind == definition["objective_item_id"]]
            if operation.state == "assigned" and (
                operation.objective_item_instance_id is not None
                or operation.delivered_item_id is not None
                or operation.resolution_method_ids
                or objective_items
            ):
                raise StateError("assigned operation has impossible objective state")
            if operation.state == "resolved" and (
                operation.objective_item_instance_id != objective_instance
                or operation.delivered_item_id is not None
                or not operation.resolution_method_ids
                or len(objective_items) != 1
                or objective_items[0].id != objective_instance
                or objective_items[0].kind != definition["objective_item_id"]
            ):
                raise StateError("resolved operation has impossible objective state")
            if operation.state == "returned" and (
                operation.objective_item_instance_id != objective_instance
                or operation.delivered_item_id != definition["objective_item_id"]
                or not operation.resolution_method_ids
                or objective_items
            ):
                raise StateError("returned operation has impossible delivery state")
    return state
