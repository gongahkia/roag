"""Strict, setting-neutral content-pack loading for Jomon's engine."""
from __future__ import annotations

from dataclasses import dataclass
from hashlib import sha256
from importlib.resources import files
import json
import os
from pathlib import Path
import re
from typing import Any


CONTENT_PACK_ENVIRONMENT = "JOMON_CONTENT_PACK"
_ID = re.compile(r"[a-z][a-z0-9._-]*\Z")


class ContentError(ValueError):
    pass


def _json(path: Path) -> Any:
    def no_duplicates(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
        result: dict[str, Any] = {}
        for key, value in pairs:
            if key in result:
                raise ContentError(f"{path.name}: duplicate key {key!r}")
            result[key] = value
        return result

    try:
        return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=no_duplicates)
    except (OSError, json.JSONDecodeError) as exc:
        raise ContentError(f"cannot read {path}") from exc


def _id(value: object, context: str) -> str:
    if not isinstance(value, str) or not _ID.fullmatch(value):
        raise ContentError(f"{context}: expected stable id")
    return value


def _nullable_id(value: object, context: str) -> str | None:
    return None if value is None else _id(value, context)


def _text(value: object, context: str) -> str:
    if not isinstance(value, str):
        raise ContentError(f"{context}: expected text")
    return value


def _rows(value: object, context: str) -> tuple[dict[str, Any], ...]:
    if not isinstance(value, list) or any(not isinstance(row, dict) for row in value):
        raise ContentError(f"{context}: expected rows")
    return tuple(value)


def _position(value: object, context: str) -> tuple[int, int]:
    if not isinstance(value, list) or len(value) != 2 or any(type(part) is not int for part in value):
        raise ContentError(f"{context}: expected [x, y]")
    return value[0], value[1]


@dataclass(frozen=True)
class AssetResource:
    id: str
    kind: str
    path: str | None


@dataclass(frozen=True)
class AssetManifest:
    resources: dict[str, AssetResource]
    bindings: dict[str, dict[str, dict[str, str]]]

    def resource(self, asset_id: str) -> AssetResource | None:
        return self.resources.get(asset_id)

    def binding(self, category: str, semantic_id: str) -> dict[str, str]:
        return dict(self.bindings.get(category, {}).get(semantic_id, {}))

    def glyph(self, semantic_id: str) -> str:
        value = self.binding("glyphs", semantic_id).get("glyph")
        if value is None:
            raise KeyError(semantic_id)
        return value


@dataclass(frozen=True)
class LoreEntry:
    id: str
    title: str
    summary: str
    body: str
    tags: tuple[str, ...]


@dataclass(frozen=True)
class ContentConnection:
    id: str
    source_id: str
    relation_id: str
    target_id: str
    tags: tuple[str, ...]


@dataclass(frozen=True)
class ContentPack:
    id: str
    root: Path
    format_version: int
    playable: bool
    systems: dict[str, Any]
    lore: dict[str, LoreEntry]
    connections: tuple[ContentConnection, ...]
    assets: AssetManifest


_ACTIVITY_SYSTEMS = {
    "production", "progression", "preparation", "chemistry", "magic",
    "circuits", "vehicles", "vessel", "crises", "situations", "worklines",
    "draw", "dice",
}
_REQUIRED_SYSTEMS = {
    "world", "setup", "items", "actors", "quests", "routes", "recipes", "activities",
}
_OPTIONAL_SYSTEMS = {"operations", "crew"}
_FEATURE_KINDS = {"base", "maintenance_latch", "access_gate", "objective_cache"}
_RESPONSE_POLICIES = {"adjacent-on-valid-action"}


def _presentation_free(value: Any, context: str = "") -> Any:
    """Remove authored display fields from the mechanical pack fingerprint."""
    if isinstance(value, dict):
        return {
            key: _presentation_free(item, key)
            for key, item in sorted(value.items())
            if key not in {"name", "title", "description", "summary", "body", "label", "tags"}
            and not (key == "objective" and context == "operations")
        }
    if isinstance(value, list):
        return [_presentation_free(item, context) for item in value]
    return value


def mechanical_fingerprint(pack: ContentPack) -> str:
    material = _presentation_free(pack.systems)
    return sha256(json.dumps(material, sort_keys=True, separators=(",", ":")).encode()).hexdigest()


def presentation_fingerprint(pack: ContentPack) -> str:
    material = {
        "lore": {key: entry.__dict__ for key, entry in pack.lore.items()},
        "connections": [entry.__dict__ for entry in pack.connections],
        "assets": pack.assets.bindings,
    }
    return sha256(json.dumps(material, sort_keys=True, default=list, separators=(",", ":")).encode()).hexdigest()


def _asset_manifest(value: Any) -> AssetManifest:
    if not isinstance(value, dict) or set(value) != {"format_version", "resources", "bindings"} or value["format_version"] != 1:
        raise ContentError("assets.json: invalid manifest")
    if not isinstance(value["resources"], dict) or not isinstance(value["bindings"], dict):
        raise ContentError("assets.json: invalid sections")
    resources: dict[str, AssetResource] = {}
    for identity, row in value["resources"].items():
        _id(identity, "asset id")
        if (not isinstance(row, dict) or set(row) != {"kind", "path"}
                or row["kind"] not in {"image", "audio", "font"} or not isinstance(row["path"], str)):
            raise ContentError("assets.json: invalid resource")
        if Path(row["path"]).is_absolute() or ".." in Path(row["path"]).parts:
            raise ContentError("assets.json: unsafe path")
        resources[identity] = AssetResource(identity, row["kind"], row["path"])
    bindings: dict[str, dict[str, dict[str, str]]] = {}
    for category, rows in value["bindings"].items():
        _id(category, "asset category")
        if not isinstance(rows, dict):
            raise ContentError("assets.json: invalid binding category")
        bindings[category] = {}
        for semantic, binding in rows.items():
            _id(semantic, "asset semantic id")
            if (not isinstance(binding, dict)
                    or any(not isinstance(key, str) or not isinstance(item, str) for key, item in binding.items())):
                raise ContentError("assets.json: invalid binding")
            bindings[category][semantic] = dict(binding)
    return AssetManifest(resources, bindings)


def _validate_world(world: object) -> tuple[dict[str, Any] | None, dict[str, dict[str, Any]]]:
    if world is None:
        return None, {}
    if not isinstance(world, dict) or set(world) not in ({"rows", "start"}, {"rows", "start", "features"}):
        raise ContentError("systems.json: invalid world")
    rows = world["rows"]
    if (not isinstance(rows, list) or not rows or any(not isinstance(row, str) for row in rows)
            or len({len(row) for row in rows}) != 1 or any(tile not in ".#" for row in rows for tile in row)):
        raise ContentError("systems.json: invalid world topology")
    start = _position(world["start"], "systems.world.start")
    if not (0 <= start[0] < len(rows[0]) and 0 <= start[1] < len(rows)) or rows[start[1]][start[0]] != ".":
        raise ContentError("systems.json: invalid start")
    features: dict[str, dict[str, Any]] = {}
    for row in _rows(world.get("features", []), "systems.world.features"):
        required = {
            "id", "kind", "position", "name", "description", "access_id",
            "requires_equipped_item_id", "operation_id", "item_id",
        }
        if set(row) != required:
            raise ContentError("systems.world.features: invalid feature fields")
        identity = _id(row["id"], "feature id")
        kind = row["kind"]
        if identity in features or kind not in _FEATURE_KINDS:
            raise ContentError("systems.world.features: duplicate or invalid kind")
        position = _position(row["position"], f"feature {identity} position")
        if not (0 <= position[0] < len(rows[0]) and 0 <= position[1] < len(rows)):
            raise ContentError("systems.world.features: out-of-bounds position")
        _text(row["name"], f"feature {identity} name")
        _text(row["description"], f"feature {identity} description")
        access_id = _nullable_id(row["access_id"], f"feature {identity} access")
        tool_id = _nullable_id(row["requires_equipped_item_id"], f"feature {identity} tool")
        operation_id = _nullable_id(row["operation_id"], f"feature {identity} operation")
        item_id = _nullable_id(row["item_id"], f"feature {identity} item")
        if kind == "base":
            valid = rows[position[1]][position[0]] == "." and all(value is None for value in (access_id, tool_id, operation_id, item_id))
        elif kind == "access_gate":
            valid = rows[position[1]][position[0]] == "#" and access_id is not None and all(value is None for value in (tool_id, operation_id, item_id))
        elif kind == "maintenance_latch":
            valid = rows[position[1]][position[0]] == "." and access_id is not None and tool_id is not None and operation_id is None and item_id is None
        else:
            valid = rows[position[1]][position[0]] == "." and operation_id is not None and item_id is not None and access_id is None and tool_id is None
        if not valid:
            raise ContentError("systems.world.features: invalid kind-specific fields")
        features[identity] = row
    return world, features


def _validate_setup(setup: object, entity_ids: set[str]) -> None:
    if not isinstance(setup, dict) or set(setup) != {"crew", "ancestries", "origins", "traits"}:
        raise ContentError("systems.json: invalid setup")
    for section in ("crew", "ancestries", "origins", "traits"):
        for row in _rows(setup[section], f"setup.{section}"):
            if set(row) not in ({"id", "name"}, {"id", "name", "health_bonus"}):
                raise ContentError(f"setup.{section}: invalid row")
            identity = _id(row["id"], section)
            if identity in entity_ids:
                raise ContentError(f"duplicate content id {identity}")
            _text(row["name"], section)
            if "health_bonus" in row and (type(row["health_bonus"]) is not int or not 0 <= row["health_bonus"] <= 10):
                raise ContentError(f"setup.{section}: invalid health bonus")
            entity_ids.add(identity)


def _validate_operations(
    operations: object,
    *,
    feature_rows: dict[str, dict[str, Any]],
    item_ids: set[str],
    actor_ids: set[str],
    quest_ids: set[str],
    entity_ids: set[str],
) -> None:
    for row in _rows(operations, "operations"):
        required = {"id", "name", "objective", "quest_id", "objective_feature_id", "objective_item_id", "return_feature_id", "methods"}
        if set(row) != required:
            raise ContentError("operations: invalid row")
        identity = _id(row["id"], "operation id")
        if identity in entity_ids:
            raise ContentError(f"duplicate content id {identity}")
        entity_ids.add(identity)
        _text(row["name"], f"operation {identity} name")
        _text(row["objective"], f"operation {identity} objective")
        quest_id = _id(row["quest_id"], f"operation {identity} quest")
        objective_feature_id = _id(row["objective_feature_id"], f"operation {identity} feature")
        objective_item_id = _id(row["objective_item_id"], f"operation {identity} item")
        return_feature_id = _id(row["return_feature_id"], f"operation {identity} return")
        if quest_id not in quest_ids or objective_item_id not in item_ids:
            raise ContentError("operations: unknown quest or item")
        objective_feature = feature_rows.get(objective_feature_id)
        return_feature = feature_rows.get(return_feature_id)
        if objective_feature is None or objective_feature["kind"] != "objective_cache" or objective_feature["operation_id"] != identity or objective_feature["item_id"] != objective_item_id:
            raise ContentError("operations: objective feature mismatch")
        if return_feature is None or return_feature["kind"] != "base":
            raise ContentError("operations: return feature must be a base")
        methods = _rows(row["methods"], f"operation {identity} methods")
        if not methods:
            raise ContentError("operations: missing methods")
        method_ids: set[str] = set()
        for method in methods:
            fields = {"id", "name", "requires_access_id", "requires_defeated_actor_id", "consequence_id"}
            if set(method) != fields:
                raise ContentError("operations: invalid method")
            method_id = _id(method["id"], "operation method id")
            if method_id in method_ids or method_id in entity_ids:
                raise ContentError("operations: duplicate method id")
            method_ids.add(method_id)
            entity_ids.add(method_id)
            _text(method["name"], f"operation method {method_id} name")
            access_id = _nullable_id(method["requires_access_id"], f"operation method {method_id} access")
            actor_id = _nullable_id(method["requires_defeated_actor_id"], f"operation method {method_id} actor")
            _id(method["consequence_id"], f"operation method {method_id} consequence")
            if (access_id is None) == (actor_id is None):
                raise ContentError("operations: a method needs exactly one physical prerequisite")
            if access_id is not None:
                if not any(feature["kind"] == "maintenance_latch" and feature["access_id"] == access_id for feature in feature_rows.values()):
                    raise ContentError("operations: unknown latch access")
            if actor_id is not None and actor_id not in actor_ids:
                raise ContentError("operations: unknown method actor")

def _validate_crew(value: object, item_ids: set[str], entity_ids: set[str], world: dict[str, Any] | None) -> None:
    positions: set[tuple[int, int]] = set()
    for row in _rows(value, "crew"):
        if set(row) != {"id", "kind", "position", "health", "items", "name", "description"}:
            raise ContentError("crew: invalid row")
        identity = _id(row["id"], "crew id")
        if identity in entity_ids or not isinstance(row["kind"], str):
            raise ContentError("crew: duplicate or invalid identity")
        position = _position(row["position"], "crew position")
        if (world is None or not (0 <= position[0] < len(world["rows"][0]) and 0 <= position[1] < len(world["rows"]))
                or world["rows"][position[1]][position[0]] != "." or position in positions):
            raise ContentError("crew: invalid or duplicate position")
        positions.add(position)
        if type(row["health"]) is not int or row["health"] < 1 or not isinstance(row["items"], list):
            raise ContentError("crew: invalid condition or items")
        if any(_id(item, "crew item") not in item_ids for item in row["items"]):
            raise ContentError("crew: unknown item")
        _text(row["name"], "crew name"); _text(row["description"], "crew description")
        entity_ids.add(identity)


def load_content_pack(root: Path) -> ContentPack:
    root = Path(root)
    manifest, systems, lore_value, connections_value = (
        _json(root / name) for name in ("manifest.json", "systems.json", "lore.json", "connections.json")
    )
    if not isinstance(manifest, dict) or set(manifest) != {"id", "format_version", "playable"}:
        raise ContentError("manifest: expected id, format_version, playable")
    pack_id = _id(manifest["id"], "manifest id")
    if type(manifest["format_version"]) is not int or manifest["format_version"] != 1 or type(manifest["playable"]) is not bool:
        raise ContentError("manifest: invalid values")
    if not isinstance(systems, dict) or not _REQUIRED_SYSTEMS.issubset(systems) or set(systems) - (_REQUIRED_SYSTEMS | _OPTIONAL_SYSTEMS):
        raise ContentError("systems.json: invalid system sections")
    activities = systems["activities"]
    if not isinstance(activities, dict) or set(activities) != _ACTIVITY_SYSTEMS:
        raise ContentError("systems.json: invalid activity sections")

    entity_ids: set[str] = set()
    _validate_setup(systems["setup"], entity_ids)
    item_ids: set[str] = set()
    actor_ids: set[str] = set()
    quest_ids: set[str] = set()
    for section in ("items", "actors", "quests", "routes", "recipes"):
        for row in _rows(systems[section], section):
            if "id" not in row:
                raise ContentError(f"{section}: row missing id")
            identity = _id(row["id"], section)
            if identity in entity_ids:
                raise ContentError(f"duplicate content id {identity}")
            if section == "items" and "initial" in row and type(row["initial"]) is not bool:
                raise ContentError("items: initial must be boolean")
            if section == "actors":
                if "response_policy" in row and row["response_policy"] not in _RESPONSE_POLICIES:
                    raise ContentError("actors: invalid response policy")
                if "response_power" in row and (type(row["response_power"]) is not int or row["response_power"] < 0):
                    raise ContentError("actors: invalid response power")
            entity_ids.add(identity)
            if section == "items":
                item_ids.add(identity)
            elif section == "actors":
                actor_ids.add(identity)
            elif section == "quests":
                quest_ids.add(identity)
    for category, rows in activities.items():
        for row in _rows(rows, f"activities.{category}"):
            if "id" not in row:
                raise ContentError(f"activities.{category}: row missing id")
            identity = _id(row["id"], category)
            if identity in entity_ids:
                raise ContentError(f"duplicate content id {identity}")
            entity_ids.add(identity)

    world, features = _validate_world(systems["world"])
    for identity in features:
        if identity in entity_ids:
            raise ContentError(f"duplicate content id {identity}")
        entity_ids.add(identity)
    for feature in features.values():
        if feature["requires_equipped_item_id"] is not None and feature["requires_equipped_item_id"] not in item_ids:
            raise ContentError("systems.world.features: unknown required item")
        if feature["item_id"] is not None and feature["item_id"] not in item_ids:
            raise ContentError("systems.world.features: unknown objective item")
    access_ids = {feature["access_id"] for feature in features.values() if feature["kind"] == "maintenance_latch"}
    if any(feature["kind"] == "access_gate" and feature["access_id"] not in access_ids for feature in features.values()):
        raise ContentError("systems.world.features: gate without a latch")
    _validate_operations(systems.get("operations", []), feature_rows=features, item_ids=item_ids, actor_ids=actor_ids, quest_ids=quest_ids, entity_ids=entity_ids)
    _validate_crew(systems.get("crew", []), item_ids, entity_ids, world)

    if manifest["playable"] and (world is None or not all(systems["setup"][key] for key in systems["setup"])):
        raise ContentError("playable pack needs world and setup choices")

    if not isinstance(lore_value, dict) or set(lore_value) != {"entries"} or not isinstance(lore_value["entries"], dict):
        raise ContentError("lore.json: invalid entries")
    lore: dict[str, LoreEntry] = {}
    for identity, row in lore_value["entries"].items():
        _id(identity, "lore id")
        if not isinstance(row, dict) or set(row) != {"title", "summary", "body", "tags"} or not isinstance(row["tags"], list):
            raise ContentError("lore.json: invalid entry")
        lore[identity] = LoreEntry(identity, _text(row["title"], identity), _text(row["summary"], identity), _text(row["body"], identity), tuple(_id(tag, identity) for tag in row["tags"]))

    if not isinstance(connections_value, dict) or set(connections_value) != {"connections"}:
        raise ContentError("connections.json: invalid section")
    connections: list[ContentConnection] = []
    connection_ids: set[str] = set()
    declared = entity_ids | set(lore)
    for row in _rows(connections_value["connections"], "connections"):
        if set(row) != {"id", "from", "relation", "to", "tags"} or not isinstance(row["tags"], list):
            raise ContentError("connections.json: invalid connection")
        identity, source, relation, target = (_id(row[key], f"connection.{key}") for key in ("id", "from", "relation", "to"))
        if identity in connection_ids or source not in declared or target not in declared:
            raise ContentError("connections.json: duplicate or dangling reference")
        connection_ids.add(identity)
        connections.append(ContentConnection(identity, source, relation, target, tuple(_id(tag, identity) for tag in row["tags"])))

    return ContentPack(pack_id, root, manifest["format_version"], manifest["playable"], systems, lore, tuple(sorted(connections, key=lambda row: row.id)), _asset_manifest(_json(root / "assets.json")))


def template_root() -> Path:
    return Path(files("jomon").joinpath("content_packs", "template"))


def first_playable_root() -> Path:
    return Path(files("jomon").joinpath("content_packs", "first-playable"))


_selected: ContentPack | None = None


def select_content_pack(root: Path | None = None) -> ContentPack:
    global _selected
    if root is None:
        root = Path(os.environ[CONTENT_PACK_ENVIRONMENT]) if CONTENT_PACK_ENVIRONMENT in os.environ else first_playable_root()
    _selected = load_content_pack(root)
    return _selected


def select_content_pack_from_environment() -> ContentPack:
    return select_content_pack()


def selected_content_pack() -> ContentPack:
    return _selected or select_content_pack()
