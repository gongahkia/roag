"""Canonical Dullest Dungeon mechanics identity, separate from fiction."""

from __future__ import annotations

import hashlib
import json
from dataclasses import asdict, dataclass
from typing import Any, Mapping

from .versions import ENGINE_VERSION, MANIFEST_SCHEMA, RNG_ARCHITECTURE

MECHANICAL_PROJECTION_FORMAT = 1


def canonical_bytes(value: Any) -> bytes:
    return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=True, allow_nan=False).encode("ascii")


@dataclass(frozen=True)
class ContentManifest:
    schema: int
    engine: str
    content_schema: int
    rng_architecture: int
    fingerprint: str
    enabled_packs: tuple[str, ...]

    def snapshot(self) -> dict[str, Any]:
        return json.loads(canonical_bytes(asdict(self)))


def _value(value: Any) -> Any:
    """Convert validated runtime definitions to ordinary JSON-safe values."""
    if isinstance(value, Mapping):
        return {str(key): _value(item) for key, item in value.items()}
    if isinstance(value, tuple):
        return [_value(item) for item in value]
    if isinstance(value, list):
        return [_value(item) for item in value]
    if value is None or isinstance(value, (str, int, float, bool)):
        return value
    raise ValueError(f"unsupported rules projection value: {type(value).__name__}")


def _record(record: Mapping[str, Any], *, mechanical: set[str], presentation: set[str], context: str) -> dict[str, Any]:
    unknown = set(record) - mechanical - presentation
    if unknown:
        detail = ", ".join(sorted(unknown))
        raise ValueError(f"{context} has unclassified rules fields: {detail}")
    # Catalog validation owns required versus optional fields.  Omitted optional
    # mechanical fields are represented by omission, not an invented default.
    return {field: _value(record[field]) for field in sorted(mechanical & set(record))}


def _effects(effects: list[Mapping[str, Any]], context: str) -> list[dict[str, Any]]:
    # Effect field names are all mechanical op parameters.  Content validation
    # owns the opcode-specific schema; preserving order is deliberate.
    return [_value(effect) for effect in effects]


def _biome_mechanics(value: Mapping[str, Any]) -> dict[str, Any]:
    expected = {"hazard", "traversal", "patrol", "visibility", "combat", "objective"}
    if set(value) != expected:
        raise ValueError("biome mechanics has unclassified sections")
    return {
        "hazard": _record(value["hazard"], mechanical={"effect", "amount", "status"}, presentation={"name", "description"}, context="biome hazard"),
        "traversal": _record(value["traversal"], mechanical={"cost"}, presentation={"description"}, context="biome traversal"),
        "patrol": _record(value["patrol"], mechanical={"behavior", "aggression", "cadence", "leash"}, presentation={"description"}, context="biome patrol"),
        "visibility": _record(value["visibility"], mechanical={"patrol_radius", "hazard_radius"}, presentation={"description"}, context="biome visibility"),
        "combat": _record(value["combat"], mechanical={"effects"}, presentation={"name", "description"}, context="biome combat"),
        "objective": _record(value["objective"], mechanical=set(), presentation={"name", "description"}, context="biome objective"),
    }


def _project_collection(name: str, definitions: Mapping[str, Mapping[str, Any]]) -> dict[str, Any]:
    fields: dict[str, tuple[set[str], set[str]]] = {
        "heroes": ({"id", "role", "combat_role", "complexity", "preferred_ranks", "max_hp", "rank", "starter_deck", "biome"}, {"name", "signature", "strength", "weakness", "builds", "summary"}),
        "squads": ({"id", "complexity", "formation", "doctrine"}, {"name", "playstyle", "strength", "weakness", "signature"}),
        "cards": ({"id", "hero", "cost", "from_ranks", "target", "target_ranks", "effects", "upgrade_effects", "tags", "biome", "biome_bonus", "lanes", "design_role"}, {"name", "description", "upgrade_description"}),
        "masteries": ({"id", "card_id", "branches"}, set()),
        "infusions": ({"id", "mode", "amount", "limit", "compatible_targets", "requires_any_tags"}, {"name", "marker", "description"}),
        "loadouts": ({"id", "hero", "complexity", "cards"}, {"name", "description"}),
        "doctrines": ({"id", "mode", "requires_tags", "requires_roles"}, {"name", "description", "strength", "liability"}),
        "enemies": ({"id", "max_hp", "actions", "biomes", "phases"}, {"name"}),
        "encounters": ({"id", "kind", "enemies", "biomes"}, set()),
        "events": ({"id", "biomes", "choices"}, {"name", "text"}),
        "landmarks": ({"id", "biome"}, {"name", "art"}),
        "missions": ({"id", "biome", "approaches"}, {"name", "description"}),
        "facilities": ({"id", "biome", "options"}, {"name", "description"}),
        "terrains": ({"id", "biome", "glyph", "cost"}, {"name", "description"}),
        "terrain_patterns": ({"id", "biome", "glyph", "mode"}, {"name", "description"}),
        "biomes": ({"id", "glyph", "mechanics"}, {"name", "description"}),
        "worlds": ({"id", "layout", "biomes"}, {"name", "description"}),
        "boons": ({"id", "effects", "tags", "requires_all_tags"}, {"name", "description"}),
        "curses": ({"id", "kind", "effects"}, {"name", "description"}),
        "items": ({"id", "effects"}, {"name", "description"}),
        "mutations": ({"id", "kind", "biomes", "effect", "amount", "compatible_kinds", "excludes", "min_band", "priority"}, {"name", "marker", "description"}),
        "afflictions": ({"id", "modifiers"}, {"name", "description"}),
    }
    if name not in fields:
        raise ValueError(f"unclassified rules collection: {name}")
    mechanical, presentation = fields[name]
    projected: dict[str, Any] = {}
    for identity, definition in definitions.items():
        row = _record(definition, mechanical=mechanical, presentation=presentation, context=f"{name} {identity}")
        if name == "enemies":
            row["actions"] = [
                _record(action, mechanical={"id", "target", "effects", "weight"}, presentation={"name"}, context=f"enemy {identity} action")
                for action in definition["actions"]
            ]
            row["phases"] = [
                _record(phase, mechanical={"id", "threshold_bp", "overflow", "effects"}, presentation={"message"}, context=f"enemy {identity} phase")
                for phase in definition.get("phases", [])
            ]
        elif name == "events":
            row["choices"] = [
                _record(choice, mechanical={"id", "cost_supplies", "effects"}, presentation={"label", "summary", "risk"}, context=f"event {identity} choice")
                for choice in definition["choices"]
            ]
        elif name == "facilities":
            row["options"] = [
                _record(option, mechanical={"id", "cost", "effects"}, presentation={"label", "summary", "risk"}, context=f"facility {identity} option")
                for option in definition["options"]
            ]
        elif name == "missions":
            approaches = []
            for approach in definition["approaches"]:
                approaches.append(_record(approach, mechanical={"id", "cost", "stages", "completion", "outcome"}, presentation={"label", "summary", "telegraph"}, context=f"mission {identity} approach"))
            row["approaches"] = approaches
        elif name == "masteries":
            row["branches"] = [
                _record(branch, mechanical={"id", "mode", "effect_index", "amount"}, presentation={"name", "description"}, context=f"mastery {identity} branch")
                for branch in definition["branches"]
            ]
        elif name == "biomes":
            row["mechanics"] = _biome_mechanics(definition["mechanics"])
        projected[identity] = row
    return projected


def mechanical_rules_projection(catalog) -> dict[str, Any]:
    """Explicit canonical input for the DD rules fingerprint.

    This is intentionally not a heuristic removal of names.  Every known field
    is classified, and any newly added field fails until its mechanical status
    is decided here.
    """
    collections = {
        name: _project_collection(name, {row["id"]: row for row in catalog.raw[name]})
        for name in catalog.__dataclass_fields__
        if name not in {"raw", "balance", "art"} and name in catalog.raw
    }
    return {
        "projection_format": MECHANICAL_PROJECTION_FORMAT,
        "content_schema": catalog.raw["schema_version"],
        "rng_architecture": RNG_ARCHITECTURE,
        "catalogs": collections,
        "balance": _value(catalog.balance),
    }


def content_rules(catalog) -> dict:
    """Full embedded rules remain an archival fallback for old snapshots."""
    rules = {
        name: {row["id"]: row for row in catalog.raw[name]}
        for name in catalog.__dataclass_fields__
        if name not in {"raw", "balance", "art"} and name in catalog.raw
    }
    rules.update(balance=catalog.raw["balance"], art=catalog.art, content_schema=catalog.raw["schema_version"])
    return rules


def content_manifest(catalog) -> ContentManifest:
    digest = hashlib.sha256(canonical_bytes(mechanical_rules_projection(catalog))).hexdigest()
    return ContentManifest(MANIFEST_SCHEMA, ENGINE_VERSION, catalog.raw["schema_version"], RNG_ARCHITECTURE,
                           digest, ("base:core",))
