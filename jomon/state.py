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
        return {
            "format": SAVE_FORMAT,
            "seed": self.seed,
            "pack_id": self.pack_id,
            "fingerprint": self.fingerprint,
            "rows": list(self.rows),
            "position": asdict(self.position),
            **({"crew": [asdict(member) for member in self.crew], "active_member_id": self.active_member_id}
               if self.crew else {"courier": asdict(self.courier), "items": [asdict(item) for item in self.items]}),
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

def _crew_member(row: object) -> CrewMember:
    actor = _actor(row)
    if not isinstance(row, dict) or not isinstance(row.get("items"), list):
        raise StateError("invalid crew member")
    return CrewMember(actor.id, actor.kind, actor.position, actor.health, actor.maximum_health,
                      actor.alive, actor.response_policy, actor.response_power,
                      [Item(**item) for item in row["items"]])


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
    state.remembered.add(start)
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
            items_legacy=[] if "crew" in value else [Item(**row) for row in value["items"]],
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
        if len({item.id for member in state.crew for item in member.items}) != sum(len(member.items) for member in state.crew):
            raise StateError("crew-bearing save duplicates item custody")
        for member in state.crew:
            if member.health < 0 or member.maximum_health < 1 or member.health > member.maximum_health or member.alive != (member.health > 0):
                raise StateError("crew-bearing save has invalid condition")
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
