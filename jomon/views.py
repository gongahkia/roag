"""Immutable renderer-neutral projections of game state."""
from __future__ import annotations

from dataclasses import dataclass

from .catalog import selected_content_pack
from .state import Actor, CrewMember, Feature, GameState, OperationState, Position


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
    visible: bool = False


@dataclass(frozen=True)
class ContextRequirementView:
    """One known prerequisite, projected for a renderer without rule authority."""
    label: str
    met: bool
    detail: str = ""


@dataclass(frozen=True)
class ContextActionView:
    id: str
    label: str
    binding: str
    enabled: bool
    reason_id: str
    reason_text: str


@dataclass(frozen=True)
class ContextView:
    """Read-only guidance for a selected known world position.

    It contains only present terrain knowledge plus currently visible actors.
    Reducers remain the authority for every submitted action.
    """
    position: Position | None
    entity_type_id: str
    display_name: str
    description: str
    distance: int | None
    range_text: str
    relation_id: str
    health: int | None
    maximum_health: int | None
    operation_note: str
    requirements: tuple[ContextRequirementView, ...]
    actions: tuple[ContextActionView, ...]
    nearby: tuple[str, ...] = ()


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
class NeuralRecordView:
    """Read-only presentation of a persisted neural record."""
    id: str
    definition_id: str
    display_name: str
    origin_member_id: str
    origin_display_name: str
    capability_ids: tuple[str, ...] = ()
    capability_display_names: tuple[str, ...] = ()


@dataclass(frozen=True)
class NeuralIntegrationSiteView:
    """A configured integration location, projected without gameplay authority."""
    id: str
    display_name: str
    in_range: bool


@dataclass(frozen=True)
class NeuralIntegrationPreviewView:
    """Read-only evaluation of one proposed neural-record transfer."""
    revision: int
    site_id: str
    site_display_name: str | None
    sites: tuple[NeuralIntegrationSiteView, ...]
    recipient_member_id: str | None
    recipient_display_name: str | None
    destination_item_id: str | None
    source_item_id: str
    source_display_name: str | None
    reason_id: str
    confirmable: bool
    inherited_capacity: int | None
    foreign_slots_used: int
    foreign_slots_available: int
    protected_records: tuple[NeuralRecordView, ...]
    retained_records: tuple[NeuralRecordView, ...]
    source_records: tuple[NeuralRecordView, ...]
    candidate_records: tuple[NeuralRecordView, ...]
    selected_record_ids: tuple[str, ...]
    resulting_records: tuple[NeuralRecordView, ...]
    removed_destination_records: tuple[NeuralRecordView, ...]
    omitted_source_records: tuple[NeuralRecordView, ...]
    source_will_be_empty: bool


@dataclass(frozen=True)
class ItemDetailView:
    """A renderer-safe detail projection for one currently carried item."""
    id: str
    kind_id: str
    display_name: str
    description: str
    quantity: int
    equipped: bool
    custodian_member_id: str
    custodian_display_name: str
    installed_member_id: str | None
    installed_member_display_name: str | None
    neural_payload_state_id: str
    neural_records: tuple[NeuralRecordView, ...]


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
                  row.position, row.health, row.maximum_health, row.alive, row.response_policy,
                  True if row.id == state.active_member_id else _visible(state, row.position))
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


def _readable_semantic_id(identity: str) -> str:
    """Presentation-only fallback when a pack has no authored record label."""
    if identity.startswith("record."):
        identity = identity.removeprefix("record.")
    if identity.startswith("capability."):
        identity = identity.removeprefix("capability.")
    return identity.replace(".", " ").replace("-", " ").replace("_", " ").title()


def neural_record_capability_ids(definition_id: str) -> tuple[str, ...]:
    """Return declared effects for one record definition, never from display data."""
    neural = selected_content_pack().systems.get("neural")
    if not isinstance(neural, dict):
        return ()
    definition = next(
        (row for row in neural.get("record_definitions", ()) if row.get("id") == definition_id),
        None,
    )
    if not isinstance(definition, dict):
        return ()
    capability_ids = definition.get("capability_ids", ())
    if not isinstance(capability_ids, list):
        return ()
    return tuple(sorted({capability for capability in capability_ids if isinstance(capability, str)}))


def effective_neural_capabilities(
    state: GameState, member: CrewMember | None = None,
) -> tuple[str, ...]:
    """Derive live learned capabilities from one installed physical device only."""
    bearer = member if member is not None else state.courier
    if not isinstance(bearer, CrewMember) or bearer.installed_neural_item_id is None:
        return ()
    device = next((item for item in bearer.items if item.id == bearer.installed_neural_item_id), None)
    if device is None or device.neural_records is None:
        return ()
    return tuple(sorted({
        capability
        for record in device.neural_records
        for capability in neural_record_capability_ids(record.definition_id)
    }))


def neural_record_view(state: GameState, record) -> NeuralRecordView:
    """Present a record's authored metadata and declared capabilities safely."""
    member_definitions = {row["id"]: row for row in selected_content_pack().systems.get("crew", [])}
    capability_ids = neural_record_capability_ids(record.definition_id)
    return NeuralRecordView(
        record.id,
        record.definition_id,
        _readable_semantic_id(record.definition_id),
        record.origin_member_id,
        str(member_definitions.get(record.origin_member_id, {}).get("name", record.origin_member_id)),
        capability_ids,
        tuple(_readable_semantic_id(capability) for capability in capability_ids),
    )


def item_detail_view(state: GameState, item_id: str) -> ItemDetailView | None:
    """Project one active-inventory item without exposing mutable item custody."""
    if not isinstance(item_id, str):
        return None
    item = next((row for row in state.items if row.id == item_id), None)
    if item is None:
        return None
    definitions = {row["id"]: row for row in selected_content_pack().systems["items"]}
    member_definitions = {row["id"]: row for row in selected_content_pack().systems.get("crew", [])}
    custodian = state.courier
    custodian_name = str(member_definitions.get(custodian.id, {}).get("name", custodian.id))
    installed = next((member for member in state.crew if member.installed_neural_item_id == item.id), None)
    if item.neural_records is None:
        payload_state, records = "neural.none", ()
    else:
        payload_state = "neural.empty" if not item.neural_records else "neural.records"
        records = tuple(neural_record_view(state, record) for record in item.neural_records)
    return ItemDetailView(
        item.id,
        item.kind,
        str(definitions.get(item.kind, {}).get("name", item.kind)),
        str(definitions.get(item.kind, {}).get("description", "")),
        item.quantity,
        item.equipped,
        custodian.id,
        custodian_name,
        installed.id if installed is not None else None,
        str(member_definitions.get(installed.id, {}).get("name", installed.id)) if installed is not None else None,
        payload_state,
        records,
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
            capability_id = definition.get("requires_capability_id")
            has_capability = capability_id is None or capability_id in effective_neural_capabilities(state)
            available = in_range and not open_access and equipped and has_capability
            if not in_range:
                reason = "interaction.out-of-range"
            elif open_access:
                reason = "interaction.completed"
            elif not equipped:
                reason = "interaction.requires-tool"
            elif not has_capability:
                reason = "interaction.requires-capability"
            else:
                reason = "interaction.available"
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


# These are presentation labels for stable outcome IDs.  They deliberately do
# not replace the ID in CommandOutcome, which remains useful to tests and
# diagnostics.
_RESULT_TEXT = {
    "ready": "Ready.",
    "move.ok": "You move.",
    "move.rejected": "That way is blocked.",
    "move.blocked-by-actor": "A living person blocks that space.",
    "attack.resolved": "Attack resolved.",
    "attack.rejected": "Target is outside melee range or cannot be attacked.",
    "attack.select-target": "Select a visible hostile target first.",
    "interaction.select-feature": "Select a visible feature first.",
    "interaction.unknown-feature": "That feature is no longer available.",
    "interaction.out-of-range": "Move next to the feature first.",
    "interaction.already-open": "That access route is already open.",
    "interaction.access-opened": "The maintenance route opens.",
    "interaction.completed": "That access route is already open.",
    "interaction.available": "Available.",
    "interaction.requires-equipped-tool": "Equip the required tool first.",
    "interaction.requires-tool": "Equip the required tool first.",
    "interaction.requires-capability": "You lack the required learned capability.",
    "interaction.access-required": "Open a route or clear the guarded approach first.",
    "interaction.objective-unavailable": "The objective cannot be acquired right now.",
    "interaction.objective-already-held": "That objective is already in custody.",
    "interaction.objective-acquired": "You recover the assigned objective.",
    "interaction.no-delivery": "Bring the recovered objective here to deliver it.",
    "interaction.objective-missing": "The active operative is not carrying the objective.",
    "interaction.operation-delivered": "Operation delivered at the base.",
    "interaction.unsupported-feature": "That feature has no direct interaction.",
    "item.equipped": "Item equipped.",
    "item.unequipped": "Item unequipped.",
    "item.rejected": "That equipment action is unavailable.",
    "item.neural-payload-not-equippable": "Neural devices cannot be equipped as ordinary gear.",
    "inventory.empty": "Inventory is empty.",
    "inventory.detail-open": "Item details opened.",
    "inventory.detail-unavailable": "That item is no longer carried.",
    "remains.select-body": "Select a visible dead crew member first.",
    "remains.unknown": "That body is unavailable for recovery.",
    "remains.out-of-range": "Move next to the body to recover items.",
    "remains.item-unavailable": "That item is no longer on the body.",
    "remains.item-recovered": "Item recovered from the body.",
    "successor.selected": "You continue as the selected crew member.",
    "successor.current-alive": "A successor can be selected only after the active operative dies.",
    "successor.unknown-member": "That crew member is unavailable.",
    "successor.ineligible": "That crew member cannot continue the operation.",
    "courier.dead": "Choose a living successor to continue.",
    "setup.select": "Choose starting selections before creating the world.",
    "setup.completed": "Setup confirmed.",
    "setup.cancelled": "New game setup cancelled.",
    "setup.rejected": "Those setup selections are unavailable.",
    "setup.locked": "Starting selections are already locked.",
    "save.no-session": "There is no active game to save.",
    "save.failed": "The game could not be saved.",
    "pause.open": "Game paused.",
    "content.unavailable": "No playable content pack is installed.",
    "menu.unavailable": "That menu option is unavailable.",
    "command.unsupported": "That action is not supported here.",
    "neural.integration-unavailable": "Neural transfer is unavailable here.",
    "neural.integration.completed": "Selected neural records were retained.",
    "neural.integration.conflicting-record": "Those records conflict.",
    "neural.integration.duplicate-record": "A record may appear only once.",
    "neural.integration.duplicate-selection": "Choose each record only once.",
    "neural.integration.invalid-record": "That neural record is unavailable.",
    "neural.integration.invalid-selection": "That neural selection is unavailable.",
    "neural.integration.invalid-site": "That location cannot integrate records.",
    "neural.integration.invalid-source": "That item cannot be used as a neural source.",
    "neural.integration.out-of-range": "Move next to an integration site.",
    "neural.integration.over-capacity": "That selection exceeds available inherited capacity.",
    "neural.integration.recipient-unavailable": "The active operative cannot receive records.",
    "neural.integration.source-empty": "That neural source contains no records.",
    "neural.integration.source-installed": "Installed devices cannot be used as a transfer source.",
    "neural.integration.source-unavailable": "That neural source is no longer carried.",
    "neural.integration.unknown-record": "That neural record is no longer available.",
    "neural.integration.review-back": "Review closed; no transfer was made.",
    "neural.integration.review-stale": "The world changed; review the transfer again before confirming.",
}


def readable_result_text(result_id: str) -> str:
    """Map current semantic outcomes to concise player-facing feedback."""
    if result_id.startswith("save.ok:"):
        return "Game saved."
    return _RESULT_TEXT.get(result_id, "Action unavailable.")


def _distance(state: GameState, point: Position) -> int:
    return abs(point.x - state.position.x) + abs(point.y - state.position.y)


def _item_display_name(kind_id: str | None) -> str:
    if not kind_id:
        return "Required item"
    definitions = {row["id"]: row for row in selected_content_pack().systems.get("items", [])}
    return str(definitions.get(kind_id, {}).get("name", _readable_semantic_id(kind_id)))


def _feature_operation_note(feature: Feature) -> str:
    """Expose an already-mechanical operation relationship without pathfinding."""
    for operation in selected_content_pack().systems.get("operations", []):
        if feature.id == operation.get("objective_feature_id"):
            return "Contains the assigned objective."
        if feature.id == operation.get("return_feature_id"):
            return "Deliver the recovered objective here."
        for method in operation.get("methods", []):
            if feature.access_id is not None and feature.access_id == method.get("requires_access_id"):
                return "Controls a route toward the assigned objective."
    if feature.kind == "access_gate":
        for related in selected_content_pack().systems.get("world", {}).get("features", []):
            if related.get("kind") == "maintenance_latch" and related.get("access_id") == feature.access_id:
                return "This gate is controlled by a local maintenance latch."
    return ""


def _feature_context(state: GameState, feature: FeatureView) -> ContextView:
    definition = _feature_definition(feature.id)
    distance = _distance(state, feature.position)
    in_range = distance <= 1
    requirements: list[ContextRequirementView] = []
    action_label = "Interact"
    if feature.kind_id == "maintenance_latch":
        action_label = "Open route"
        state_feature = next((row for row in state.features if row.id == feature.id), None)
        item_id = state_feature.requires_equipped_item_id if state_feature is not None else None
        tool_equipped = any(item.kind == item_id and item.equipped for item in state.items)
        capability_id = definition.get("requires_capability_id")
        has_capability = capability_id is None or capability_id in effective_neural_capabilities(state)
        requirements.extend((
            ContextRequirementView("In range", in_range, "Adjacent interaction range."),
            ContextRequirementView(f"{_item_display_name(item_id)} equipped", tool_equipped),
            ContextRequirementView(
                f"{_readable_semantic_id(str(capability_id))} learned", has_capability,
            ) if capability_id is not None else ContextRequirementView("No learned capability required", True),
        ))
    elif feature.kind_id == "objective_cache":
        action_label = "Recover objective"
        operation = next((row for row in state.operations if row.id == definition.get("operation_id")), None)
        requirements.extend((
            ContextRequirementView("In range", in_range, "Adjacent interaction range."),
            ContextRequirementView(
                "A route into the enclosure is open or clear",
                bool(_satisfied_method_ids(state, operation)),
            ),
        ))
    elif feature.kind_id == "base":
        action_label = "Deliver objective"
        requirements.extend((
            ContextRequirementView("In range", in_range, "Adjacent interaction range."),
            ContextRequirementView("Active operative carries a recovered objective", feature.interaction_available or feature.availability_id == "interaction.available"),
        ))
    elif feature.kind_id == "access_gate":
        action_label = "Inspect gate"

    actions: tuple[ContextActionView, ...]
    if feature.kind_id == "access_gate":
        actions = ()
    else:
        actions = (
            ContextActionView(
                "interact", action_label, "E", feature.interaction_available,
                feature.availability_id, readable_result_text(feature.availability_id),
            ),
        )
    return ContextView(
        feature.position, feature.kind_id, feature.display_name, feature.description,
        distance, "Adjacent" if in_range else f"{distance} tiles away", "feature", None, None,
        _feature_operation_note(next(row for row in state.features if row.id == feature.id)),
        tuple(requirements), actions,
    )


def _actor_context(state: GameState, actor: ActorView) -> ContextView:
    distance = _distance(state, actor.position)
    definitions = {
        row["id"]: row
        for section in ("actors", "crew")
        for row in selected_content_pack().systems.get(section, [])
    }
    display_name = str(definitions.get(actor.id, {}).get("name", _readable_semantic_id(actor.presentation_id)))
    if not actor.alive:
        source = next((member for member in state.crew if member.id == actor.id), None)
        recoverable = bool(source and source.items and state.courier.alive and distance <= 1)
        reason = "interaction.available" if recoverable else (
            "remains.out-of-range" if source and source.items else "remains.item-unavailable"
        )
        return ContextView(
            actor.position, "remains", display_name, "A dead crew member's physical remains.",
            distance, "Adjacent" if distance <= 1 else f"{distance} tiles away", "remains",
            0, actor.maximum_health, "Recover carried items locally.",
            (
                ContextRequirementView("In range", distance <= 1, "Adjacent recovery range."),
                ContextRequirementView("Recoverable items remain", bool(source and source.items)),
            ),
            (ContextActionView("recover", "Recover remains", "R", recoverable, reason, readable_result_text(reason)),)
            if source and source.items else (),
        )

    if actor.actor_kind == "courier":
        relation, note = "self", "Active operative"
    elif actor.response_policy_id is not None:
        relation, note = "hostile", "Hostile defender"
    elif actor.actor_kind == "crew":
        relation, note = "ally", "Friendly crew member"
    else:
        relation, note = "neutral", "Neutral actor"

    actions: tuple[ContextActionView, ...] = ()
    requirements: tuple[ContextRequirementView, ...] = ()
    if relation == "hostile":
        dx = abs(actor.position.x - state.position.x)
        dy = abs(actor.position.y - state.position.y)
        cardinal = dx + dy == 1
        diagonal = dx == 1 and dy == 1 and "capability.melee-diagonal" in effective_neural_capabilities(state)
        enabled = cardinal or diagonal
        reason = "attack.resolved" if enabled else "attack.rejected"
        requirements = (ContextRequirementView("Within melee range", enabled, "Cardinal adjacent; diagonal needs a learned technique."),)
        actions = (ContextActionView("attack", "Attack", "F", enabled, reason, readable_result_text(reason)),)
    return ContextView(
        actor.position, actor.actor_kind, display_name, note, distance,
        "Adjacent" if distance <= 1 else f"{distance} tiles away", relation,
        actor.health, actor.maximum_health, "", requirements, actions,
    )


def context_view(state: GameState, selected: Position | None) -> ContextView:
    """Create known local action guidance without exposing hidden actors."""
    known = {
        cell.position for cell in world_view(state).cells if cell.visible or cell.remembered
    }
    if selected is not None and selected in known:
        feature = next((row for row in feature_views(state) if row.position == selected and (row.visible or row.remembered)), None)
        if feature is not None:
            return _feature_context(state, feature)
        actor = next((row for row in actor_views(state) if row.position == selected and row.visible), None)
        if actor is not None:
            return _actor_context(state, actor)
        tile = state.rows[selected.y][selected.x]
        return ContextView(
            selected, "terrain", "Wall" if tile == "#" else "Ground", "Known terrain.",
            _distance(state, selected), "Adjacent" if _distance(state, selected) <= 1 else f"{_distance(state, selected)} tiles away",
            "terrain", None, None, "", (), (),
        )

    nearby: list[tuple[int, str]] = []
    for feature in feature_views(state):
        if feature.visible:
            nearby.append((_distance(state, feature.position), feature.display_name))
    for actor in actor_views(state):
        if actor.visible and actor.actor_kind != "courier":
            definitions = {
                row["id"]: row
                for section in ("actors", "crew")
                for row in selected_content_pack().systems.get(section, [])
            }
            nearby.append((_distance(state, actor.position), str(definitions.get(actor.id, {}).get("name", actor.presentation_id))))
    labels = tuple(f"{name} — {distance} tile{'s' if distance != 1 else ''} away" for distance, name in sorted(nearby)[:3])
    return ContextView(
        None, "none", "Context", "Select a visible feature, person, or known cell to inspect it.",
        None, "", "", None, None, "", (), (), labels,
    )


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
