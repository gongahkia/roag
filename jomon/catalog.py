"""Strict, setting-neutral content-pack loading for the engine baseline."""
from __future__ import annotations
from dataclasses import dataclass
from hashlib import sha256
from importlib.resources import files
import json, os, re
from pathlib import Path
from typing import Any

CONTENT_PACK_ENVIRONMENT = "JOMON_CONTENT_PACK"
_ID = re.compile(r"[a-z][a-z0-9._-]*\Z")

class ContentError(ValueError): pass

def _json(path: Path) -> Any:
    def no_duplicates(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
        out: dict[str, Any] = {}
        for key, value in pairs:
            if key in out: raise ContentError(f"{path.name}: duplicate key {key!r}")
            out[key] = value
        return out
    try: return json.loads(path.read_text(encoding="utf-8"), object_pairs_hook=no_duplicates)
    except (OSError, json.JSONDecodeError) as exc: raise ContentError(f"cannot read {path}") from exc

def _id(value: object, context: str) -> str:
    if not isinstance(value, str) or not _ID.fullmatch(value): raise ContentError(f"{context}: expected stable id")
    return value

def _text(value: object, context: str) -> str:
    if not isinstance(value, str): raise ContentError(f"{context}: expected text")
    return value

def _rows(value: object, context: str) -> tuple[dict[str, Any], ...]:
    if not isinstance(value, list) or any(not isinstance(row, dict) for row in value): raise ContentError(f"{context}: expected rows")
    return tuple(value)

@dataclass(frozen=True)
class AssetResource:
    id: str
    kind: str
    path: str | None

@dataclass(frozen=True)
class AssetManifest:
    resources: dict[str, AssetResource]
    bindings: dict[str, dict[str, dict[str, str]]]
    def resource(self, asset_id: str) -> AssetResource | None: return self.resources.get(asset_id)
    def binding(self, category: str, semantic_id: str) -> dict[str, str]: return dict(self.bindings.get(category, {}).get(semantic_id, {}))
    def glyph(self, semantic_id: str) -> str:
        value = self.binding("glyphs", semantic_id).get("glyph")
        if value is None: raise KeyError(semantic_id)
        return value

@dataclass(frozen=True)
class LoreEntry:
    id: str; title: str; summary: str; body: str; tags: tuple[str, ...]
@dataclass(frozen=True)
class ContentConnection:
    id: str; source_id: str; relation_id: str; target_id: str; tags: tuple[str, ...]
@dataclass(frozen=True)
class ContentPack:
    id: str; root: Path; format_version: int; playable: bool; systems: dict[str, Any]
    lore: dict[str, LoreEntry]; connections: tuple[ContentConnection, ...]; assets: AssetManifest

_ACTIVITY_SYSTEMS = {"production", "progression", "preparation", "chemistry", "magic", "circuits", "vehicles", "vessel", "crises", "situations", "worklines", "draw", "dice"}
_REQUIRED_SYSTEMS = {"world", "setup", "items", "actors", "quests", "routes", "recipes", "activities"}

def _presentation_free(value: Any) -> Any:
    """Remove authored display fields from the mechanical pack fingerprint."""
    if isinstance(value, dict):
        return {key: _presentation_free(item) for key, item in sorted(value.items())
                if key not in {"name", "title", "description", "summary", "body", "label", "tags"}}
    if isinstance(value, list): return [_presentation_free(item) for item in value]
    return value

def mechanical_fingerprint(pack: ContentPack) -> str:
    return sha256(json.dumps(_presentation_free(pack.systems), sort_keys=True, separators=(",", ":")).encode()).hexdigest()

def presentation_fingerprint(pack: ContentPack) -> str:
    material = {"lore": {key: entry.__dict__ for key, entry in pack.lore.items()},
                "connections": [entry.__dict__ for entry in pack.connections], "assets": pack.assets.bindings}
    return sha256(json.dumps(material, sort_keys=True, default=list, separators=(",", ":")).encode()).hexdigest()

def _asset_manifest(value: Any) -> AssetManifest:
    if not isinstance(value, dict) or set(value) != {"format_version", "resources", "bindings"} or value["format_version"] != 1:
        raise ContentError("assets.json: invalid manifest")
    resources: dict[str, AssetResource] = {}
    if not isinstance(value["resources"], dict) or not isinstance(value["bindings"], dict): raise ContentError("assets.json: invalid sections")
    for identity, row in value["resources"].items():
        _id(identity, "asset id")
        if not isinstance(row, dict) or set(row) != {"kind", "path"} or row["kind"] not in {"image", "audio", "font"} or not isinstance(row["path"], str):
            raise ContentError("assets.json: invalid resource")
        if Path(row["path"]).is_absolute() or ".." in Path(row["path"]).parts: raise ContentError("assets.json: unsafe path")
        resources[identity] = AssetResource(identity, row["kind"], row["path"])
    bindings: dict[str, dict[str, dict[str, str]]] = {}
    for category, rows in value["bindings"].items():
        _id(category, "asset category")
        if not isinstance(rows, dict): raise ContentError("assets.json: invalid binding category")
        bindings[category] = {}
        for semantic, binding in rows.items():
            _id(semantic, "asset semantic id")
            if not isinstance(binding, dict) or any(not isinstance(k, str) or not isinstance(v, str) for k,v in binding.items()): raise ContentError("assets.json: invalid binding")
            bindings[category][semantic] = dict(binding)
    return AssetManifest(resources, bindings)

def load_content_pack(root: Path) -> ContentPack:
    root = Path(root)
    manifest, systems, lore_value, conn_value = (_json(root / name) for name in ("manifest.json", "systems.json", "lore.json", "connections.json"))
    if not isinstance(manifest, dict) or set(manifest) != {"id", "format_version", "playable"}: raise ContentError("manifest: expected id, format_version, playable")
    pack_id = _id(manifest["id"], "manifest id")
    if type(manifest["format_version"]) is not int or manifest["format_version"] != 1 or type(manifest["playable"]) is not bool: raise ContentError("manifest: invalid values")
    if not isinstance(systems, dict) or set(systems) != _REQUIRED_SYSTEMS: raise ContentError("systems.json: invalid system sections")
    activities = systems["activities"]
    if not isinstance(activities, dict) or set(activities) != _ACTIVITY_SYSTEMS:
        raise ContentError("systems.json: invalid activity sections")
    setup = systems["setup"]
    if not isinstance(setup, dict) or set(setup) != {"crew", "ancestries", "origins", "traits"}: raise ContentError("systems.json: invalid setup")
    entity_ids: set[str] = set()
    for section in ("crew", "ancestries", "origins", "traits"):
        for row in _rows(setup[section], f"setup.{section}"):
            if set(row) != {"id", "name"}: raise ContentError(f"setup.{section}: invalid row")
            entity_ids.add(_id(row["id"], section)); _text(row["name"], section)
    for section in ("items", "actors", "quests", "routes", "recipes"):
        for row in _rows(systems[section], section):
            if "id" not in row: raise ContentError(f"{section}: row missing id")
            identity = _id(row["id"], section)
            if identity in entity_ids: raise ContentError(f"duplicate content id {identity}")
            entity_ids.add(identity)
    for category, rows in activities.items():
        for row in _rows(rows, f"activities.{category}"):
            if "id" not in row: raise ContentError(f"activities.{category}: row missing id")
            identity = _id(row["id"], category)
            if identity in entity_ids: raise ContentError(f"duplicate content id {identity}")
            entity_ids.add(identity)
    if systems["world"] is not None:
        world = systems["world"]
        if not isinstance(world, dict) or set(world) != {"rows", "start"} or not isinstance(world["rows"], list) or not world["rows"] or any(not isinstance(row,str) for row in world["rows"]): raise ContentError("systems.json: invalid world")
        if len({len(row) for row in world["rows"]}) != 1 or any(char not in ".#" for row in world["rows"] for char in row): raise ContentError("systems.json: invalid world topology")
        if not isinstance(world["start"], list) or len(world["start"]) != 2 or any(type(part) is not int for part in world["start"]): raise ContentError("systems.json: invalid start")
    if manifest["playable"] and (systems["world"] is None or not all(setup[key] for key in setup)): raise ContentError("playable pack needs world and setup choices")
    if not isinstance(lore_value, dict) or set(lore_value) != {"entries"} or not isinstance(lore_value["entries"], dict): raise ContentError("lore.json: invalid entries")
    lore: dict[str,LoreEntry] = {}
    for identity,row in lore_value["entries"].items():
        _id(identity, "lore id")
        if not isinstance(row, dict) or set(row) != {"title","summary","body","tags"} or not isinstance(row["tags"], list): raise ContentError("lore.json: invalid entry")
        lore[identity] = LoreEntry(identity, _text(row["title"], identity), _text(row["summary"], identity), _text(row["body"], identity), tuple(_id(tag, identity) for tag in row["tags"]))
    if not isinstance(conn_value, dict) or set(conn_value) != {"connections"}: raise ContentError("connections.json: invalid section")
    connections: list[ContentConnection] = []
    connection_ids: set[str] = set(); declared = entity_ids | set(lore)
    for row in _rows(conn_value["connections"], "connections"):
        if set(row) != {"id","from","relation","to","tags"} or not isinstance(row["tags"], list): raise ContentError("connections.json: invalid connection")
        identity, source, relation, target = (_id(row[key], f"connection.{key}") for key in ("id","from","relation","to"))
        if identity in connection_ids or source not in declared or target not in declared: raise ContentError("connections.json: duplicate or dangling reference")
        connection_ids.add(identity); connections.append(ContentConnection(identity,source,relation,target,tuple(_id(tag, identity) for tag in row["tags"])))
    return ContentPack(pack_id, root, manifest["format_version"], manifest["playable"], systems, lore, tuple(sorted(connections,key=lambda row:row.id)), _asset_manifest(_json(root / "assets.json")))

def template_root() -> Path: return Path(files("jomon").joinpath("content_packs", "template"))
_selected: ContentPack | None = None
def select_content_pack(root: Path | None = None) -> ContentPack:
    global _selected
    _selected = load_content_pack(root or Path(os.environ.get(CONTENT_PACK_ENVIRONMENT, template_root())))
    return _selected
def select_content_pack_from_environment() -> ContentPack: return select_content_pack()
def selected_content_pack() -> ContentPack: return _selected or select_content_pack()
