"""Immutable renderer-neutral projections of game state."""
from __future__ import annotations

from dataclasses import dataclass

from .catalog import selected_content_pack
from .state import Actor, Feature, GameState, OperationState, Position


@dataclass(frozen=True)
class CellView:
    position: Position
    terrain_id: str
    visible: bool
    remembered: bool
    actor_ids: tuple[str, ...] = ()
    feature_ids: tuple[str, ...] = ()


@dataclass(frozen=True)
class ActorView:
    id: str
    presentation_id: str
    actor_kind: str
    position: Position
    health: int
    maximum_health: int
    alive: bool
    response_policy_id: str | None = None


@dataclass(frozen=True)
class ItemView:
    id: str
    kind_id: str
    display_name: str
    description: str
    quantity: int
    equipped: bool
    legal_operations: tuple[str, ...]


@dataclass(frozen=True)
class QuestView:
    id: str
    title: str
    objective: str
    state_id: str
    progress: int


@dataclass(frozen=True)
class RouteView:
    id: str
    display_name: str
    turns: int
    available: bool = True


@dataclass(frozen=True)
class RecipeView:
    id: str
    display_name: str
    input_id: str
    output_id: str
    available: bool = True


@dataclass(frozen=True)
class ActivityView:
    category_id: str
    id: str
    display_name: str
    available: bool = True


@dataclass(frozen=True)
class SetupOptionView:
    id: str
    display_name: str
    health_bonus: int = 0


@dataclass(frozen=True)
class CharacterSetupView:
    crew: tuple[SetupOptionView, ...]
    ancestries: tuple[SetupOptionView, ...]
    origins: tuple[SetupOptionView, ...]
    traits: tuple[SetupOptionView, ...]


@dataclass(frozen=True)
class FeatureView:
    id: str
    kind_id: str
    position: Position
    display_name: str
    description: str
    visible: bool
    remembered: bool
    access_open: bool
    interaction_available: bool
    availability_id: str


@dataclass(frozen=True)
class OperationView:
    id: str
    display_name: str
    objective: str
    state_id: str
    objective_item_id: str
    evidence_method_ids: tuple[str, ...]
    resolution_method_ids: tuple[str, ...]
    consequence_ids: tuple[str, ...]
    method_ids: tuple[str, ...]

@dataclass(frozen=True)
class CrewView:
    id: str
    display_name: str
    position: Position
    health: int
    maximum_health: int
    alive: bool
    active: bool
    item_ids: tuple[str, ...]


@dataclass(frozen=True)
class WorldView:
    width: int
    height: int
    cells: tuple[CellView, ...]
    courier_position: Position
    courier_alive: bool
    turn: int


def _visible(state: GameState, point: Position) -> bool:
    return abs(point.x - state.position.x) + abs(point.y - state.position.y) <= 5


def _feature_map(state: GameState) -> dict[str, Feature]:
    return {feature.id: feature for feature in state.features}


def _feature_definition(identity: str) -> dict[str, object]:
    world = selected_content_pack().systems["world"]
    assert isinstance(world, dict)
    return next(row for row in world.get("features", []) if row["id"] == identity)


def _cell_terrain_id(state: GameState, point: Position, tile: str) -> str:
    gate = next((feature for feature in state.features if feature.position == point and feature.kind == "access_gate"), None)
    if gate is not None:
        return "terrain.gate.open" if gate.access_id in state.opened_access_ids else "terrain.gate.closed"
    return "terrain.wall" if tile == "#" else "terrain.floor"


def world_view(state: GameState) -> WorldView:
    cells: list[CellView] = []
    actors: dict[Position, list[str]] = {}
    for actor in state.actors:
        if actor.alive:
            actors.setdefault(actor.position, []).append(actor.id)
    features: dict[Position, list[str]] = {}
    for feature in state.features:
        features.setdefault(feature.position, []).append(feature.id)
    for y, row in enumerate(state.rows):
        for x, tile in enumerate(row):
            point = Position(x, y)
            visible = _visible(state, point)
            remembered = point in state.remembered
            cells.append(CellView(
                point,
                _cell_terrain_id(state, point, tile),
                visible,
                remembered,
                tuple(sorted(actors.get(point, ()))) if visible else (),
                tuple(sorted(features.get(point, ()))) if (visible or remembered) else (),
            ))
    return WorldView(len(state.rows[0]), len(state.rows), tuple(cells), state.position, state.courier.alive, state.turn)


def actor_views(state: GameState) -> tuple[ActorView, ...]:
    rows = [state.courier, *state.actors, *(row for row in state.crew if row.id != state.active_member_id)]
    return tuple(
        ActorView(row.id, row.id, "courier" if row.id == state.active_member_id else row.kind,
                  row.position, row.health, row.maximum_health, row.alive, row.response_policy)
        for row in rows
    )


def inventory_view(state: GameState) -> tuple[ItemView, ...]:
    definitions = {row["id"]: row for row in selected_content_pack().systems["items"]}
    return tuple(
        ItemView(
            item.id,
            item.kind,
            str(definitions.get(item.kind, {}).get("name", item.kind)),
            str(definitions.get(item.kind, {}).get("description", "")),
            item.quantity,
            item.equipped,
            ("unequip",) if item.equipped else ("equip",),
        )
        for item in state.items
    )


def _operation_state_for_quest(state: GameState, quest_id: str) -> OperationState | None:
    pack = selected_content_pack()
    for definition in pack.systems.get("operations", []):
        if definition["quest_id"] == quest_id:
            return next((row for row in state.operations if row.id == definition["id"]), None)
    return None


def quest_views(state: GameState) -> tuple[QuestView, ...]:
    definitions = {row["id"]: row for row in selected_content_pack().systems["quests"]}
    result: list[QuestView] = []
    for quest in state.quests:
        definition = definitions[quest.id]
        operation = _operation_state_for_quest(state, quest.id)
        state_id = operation.state if operation is not None else quest.state
        progress = {"assigned": 0, "resolved": 1, "returned": 2}.get(state_id, quest.progress)
        result.append(QuestView(quest.id, str(definition.get("title", quest.id)), str(definition.get("objective", "")), state_id, progress))
    return tuple(result)

def crew_views(state: GameState) -> tuple[CrewView, ...]:
    definitions = {row["id"]: row for row in selected_content_pack().systems.get("crew", [])}
    return tuple(CrewView(row.id, str(definitions.get(row.id, {}).get("name", row.id)), row.position,
                          row.health, row.maximum_health, row.alive, row.id == state.active_member_id,
                          tuple(item.id for item in row.items)) for row in state.crew)


def travel_view(state: GameState) -> tuple[RouteView, ...]:
    return tuple(RouteView(row["id"], str(row.get("name", row["id"])), int(row.get("turns", 1))) for row in selected_content_pack().systems["routes"])


def recipe_view(state: GameState) -> tuple[RecipeView, ...]:
    return tuple(RecipeView(row["id"], str(row.get("name", row["id"])), str(row["input"]), str(row["output"])) for row in selected_content_pack().systems["recipes"])


def activity_views(state: GameState, category_id: str) -> tuple[ActivityView, ...]:
    rows = selected_content_pack().systems["activities"].get(category_id, ())
    return tuple(ActivityView(category_id, row["id"], str(row.get("name", row["id"]))) for row in rows)


def character_setup_view() -> CharacterSetupView:
    setup = selected_content_pack().systems["setup"]

    def convert(section: str) -> tuple[SetupOptionView, ...]:
        return tuple(SetupOptionView(row["id"], row["name"], int(row.get("health_bonus", 0))) for row in setup[section])

    return CharacterSetupView(convert("crew"), convert("ancestries"), convert("origins"), convert("traits"))


def feature_views(state: GameState) -> tuple[FeatureView, ...]:
    result: list[FeatureView] = []
    for feature in state.features:
        definition = _feature_definition(feature.id)
        visible = _visible(state, feature.position)
        remembered = feature.position in state.remembered
        in_range = abs(feature.position.x - state.position.x) + abs(feature.position.y - state.position.y) <= 1
        open_access = feature.access_id in state.opened_access_ids if feature.access_id else False
        if feature.kind == "maintenance_latch":
            equipped = any(item.kind == feature.requires_equipped_item_id and item.equipped for item in state.items)
            available, reason = in_range and not open_access and equipped, "interaction.available" if in_range and not open_access and equipped else "interaction.requires-tool" if in_range and not open_access else "interaction.out-of-range" if not in_range else "interaction.completed"
        elif feature.kind == "objective_cache":
            operation = next((row for row in state.operations if row.id == feature.operation_id), None)
            methods = _satisfied_method_ids(state, operation) if operation else ()
            available, reason = in_range and operation is not None and operation.state == "assigned" and bool(methods), "interaction.available" if in_range and operation is not None and operation.state == "assigned" and bool(methods) else "interaction.access-required" if in_range else "interaction.out-of-range"
        elif feature.kind == "base":
            returnable = any(row.state == "resolved" for row in state.operations)
            available, reason = in_range and returnable, "interaction.available" if in_range and returnable else "interaction.no-delivery"
        else:
            available, reason = False, "interaction.none"
        result.append(FeatureView(feature.id, feature.kind, feature.position, str(definition["name"]), str(definition["description"]), visible, remembered, open_access, available, reason))
    return tuple(result)


def _satisfied_method_ids(state: GameState, operation: OperationState | None) -> tuple[str, ...]:
    if operation is None:
        return ()
    definition = next((row for row in selected_content_pack().systems.get("operations", []) if row["id"] == operation.id), None)
    if definition is None:
        return ()
    output: list[str] = []
    for method in definition["methods"]:
        access_id = method["requires_access_id"]
        actor_id = method["requires_defeated_actor_id"]
        access_ok = access_id is not None and access_id in state.opened_access_ids
        actor = next((row for row in state.actors if row.id == actor_id), None)
        actor_ok = actor_id is not None and actor is not None and not actor.alive
        if access_ok or actor_ok:
            output.append(method["id"])
    return tuple(sorted(output))


def operation_views(state: GameState) -> tuple[OperationView, ...]:
    definitions = {row["id"]: row for row in selected_content_pack().systems.get("operations", [])}
    result: list[OperationView] = []
    for operation in state.operations:
        definition = definitions[operation.id]
        result.append(OperationView(
            operation.id,
            str(definition["name"]),
            str(definition["objective"]),
            operation.state,
            str(definition["objective_item_id"]),
            tuple(sorted(operation.evidence_method_ids)),
            tuple(sorted(operation.resolution_method_ids)),
            tuple(sorted(operation.consequence_ids)),
            tuple(method["id"] for method in definition["methods"]),
        ))
    return tuple(result)
