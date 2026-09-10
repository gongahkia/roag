"""Load and validate the JSON game catalog."""

from __future__ import annotations

import re
from dataclasses import dataclass
from functools import cached_property, lru_cache
from importlib.resources import files
from pathlib import Path
from typing import Any

from .json_data import JsonDataError, loads
from .contracts import Definition, Enemy, Opcode, Technique, freeze, runtime_definition
from .manifest import ContentManifest, canonical_bytes, content_manifest, content_rules
from .versions import CONTENT_SCHEMA
from .acquisition import Lane
from .passives import EffectKey, persistent_effect
from .ordering import ordered_definitions


class ContentError(ValueError):
    """Raised when bundled or user-supplied content is inconsistent."""


@dataclass(frozen=True)
class Catalog:
    raw: dict[str, Any]
    heroes: dict[str, dict[str, Any]]
    squads: dict[str, dict[str, Any]]
    cards: dict[str, Technique]
    enemies: dict[str, Enemy]
    encounters: dict[str, dict[str, Any]]
    events: dict[str, dict[str, Any]]
    landmarks: dict[str, dict[str, Any]]
    missions: dict[str, dict[str, Any]]
    facilities: dict[str, dict[str, Any]]
    terrains: dict[str, dict[str, Any]]
    terrain_patterns: dict[str, dict[str, Any]]
    biomes: dict[str, dict[str, Any]]
    worlds: dict[str, dict[str, Any]]
    boons: dict[str, dict[str, Any]]
    curses: dict[str, dict[str, Any]]
    items: dict[str, dict[str, Any]]
    mutations: dict[str, dict[str, Any]]
    afflictions: dict[str, dict[str, Any]]
    balance: dict[str, Any]
    art: dict[str, Any]

    def __post_init__(self) -> None:
        for name in self.__dataclass_fields__:
            value = getattr(self, name)
            if name not in {"raw", "balance", "art"}:
                value = {key: item if isinstance(item, Definition) else runtime_definition(name, item)
                         for key, item in value.items()}
                value = ordered_definitions(name, value)
            object.__setattr__(self, name, freeze(value))

    @cached_property
    def manifest(self) -> ContentManifest:
        return content_manifest(self)

    @cached_property
    def rules(self) -> dict:
        return freeze(content_rules(self))


CARD_EFFECTS = {opcode.value for opcode in Opcode}
EVENT_EFFECTS = {
    "light",
    "supplies",
    "heal_all",
    "stress_all",
    "damage_random",
    "card_reward",
}
TARGETS = {"enemy", "all_enemies", "self", "ally", "all_allies"}
ENEMY_TARGETS = {
    "all_heroes",
    "back",
    "deaths_door",
    "front",
    "marked",
    "random",
    "self",
    "stressed",
    "weakest_ally",
    "weakest_enemy",
    "wounded",
}
EFFECT_CURVES = {"linear", "diminishing", "threshold", "special"}
CARD_STATES = {"deaths_door", "stressed", "healthy", "wounded"}
CARD_STATUSES = {"dodge", "focus", "marked", "riposte", "stun", "vulnerable", "weak", "wound"}
BIOME_HAZARD_EFFECTS = {
    "damage_all",
    "damage_weakest",
    "light",
    "opening_hand",
    "status_all",
    "status_random",
    "stress_highest",
    "supplies",
    "wound_injured",
}
BIOME_PATROL_BEHAVIORS = {
    "circuit",
    "erratic",
    "hunt",
    "migrate",
    "roam",
    "sentry",
    "stalk",
    "sweep",
}
BIOME_COMBAT_TARGETS = {
    "all",
    "back_crew",
    "back_enemy",
    "crew",
    "enemies",
    "front_crew",
    "front_enemy",
}
BIOME_COMBAT_OPS = {"block", "draw", "energy", "move", "reverse", "status", "stress"}
BIOME_OBJECTIVE_EFFECTS = {
    "boon_random",
    "cleanse_all",
    "curse_random",
    "damage_all",
    "damage_random",
    "heal_all",
    "heal_weakest",
    "item_random",
    "light",
    "remove_random",
    "status_all",
    "stress_all",
    "stress_highest",
    "supplies",
    "upgrade_random",
}
FACILITY_EFFECTS = BIOME_OBJECTIVE_EFFECTS | {"reveal_biome", "suppress_hazard"}
BIOME_OBJECTIVE_EFFECTS |= {
    "agitate_patrols",
    "calm_patrols",
    "objective_combat",
    "reveal_biome",
    "stabilize_terrain",
    "suppress_hazard",
}
EVENT_EFFECTS |= {
    "agitate_patrols",
    "calm_patrols",
    "cleanse_all",
    "curse_random",
    "item_random",
    "reveal_biome",
    "stabilize_terrain",
    "status_all",
    "suppress_hazard",
}
TERRAIN_GLYPHS = frozenset(".,=~_\";:`'%-o")
CARD_TAGS = {
    "block",
    "cleanse",
    "control",
    "damage",
    "discard",
    "displacement",
    "draw",
    "energy",
    "guard",
    "mobility",
    "recovery",
    "stress_relief",
    "stress_risk",
}
EFFECT_KEYS = {key.value for key in EffectKey}


CONTENT_FIELDS = {
    "heroes": "id name role combat_role complexity preferred_ranks signature strength weakness builds summary max_hp rank starter_deck biome",
    "squads": "id name playstyle complexity formation strength weakness signature",
    "cards": "id name hero cost from_ranks target target_ranks description effects upgrade_effects tags upgrade_description biome biome_bonus lanes",
    "enemies": "id name max_hp actions biomes",
    "encounters": "id kind enemies biomes",
    "events": "id name text choices biomes",
    "landmarks": "id name biome art",
    "missions": "id name biome description approaches",
    "facilities": "id name biome description options",
    "terrains": "id name biome glyph cost description",
    "terrain_patterns": "id name biome glyph mode description",
    "biomes": "id name glyph description mechanics",
    "worlds": "id name description layout biomes",
    "boons": "id name description effects tags requires_all_tags",
    "curses": "id name description effects kind",
    "items": "id name description effects",
    "mutations": "id name marker description kind biomes effect amount compatible_kinds excludes min_band priority",
    "afflictions": "id name description modifiers",
}

MUTATION_EFFECTS = {
    "opening_front_block",
    "guard_rear",
    "mark_weakest",
    "dodge_rear",
    "riposte_front",
    "focus_striker",
    "shove_front_crew",
    "heal_weakest_round",
    "react_third_card",
    "surge_ally_death",
    "reinforce_once",
    "cleanse_round",
    "resist_first_stun",
    "wound_on_ally_death",
    "pull_crew_round",
    "wide_wound_round",
}
MUTATION_BANDS = {"hunted", "lockdown", "overrun"}


def _fields(value: Any, allowed: str, context: str) -> None:
    if not isinstance(value, dict):
        raise ContentError(f"{context} must be an object")
    unknown = value.keys() - set(allowed.split())
    if unknown:
        raise ContentError(f"{context} has unknown fields: {', '.join(sorted(unknown))}")


def _indexed(items: Any, section: str) -> dict[str, dict[str, Any]]:
    if not isinstance(items, list):
        raise ContentError(f"{section} must be a list")
    result: dict[str, dict[str, Any]] = {}
    for index, item in enumerate(items):
        if not isinstance(item, dict) or not isinstance(item.get("id"), str):
            raise ContentError(f"{section}[{index}] must have a string id")
        _fields(item, CONTENT_FIELDS[section], f"{section}[{index}]")
        if re.fullmatch(r"[a-z][a-z0-9_]*(?::[a-z][a-z0-9_]*)*", item["id"]) is None:
            raise ContentError(f"{section}[{index}] has an invalid stable id")
        if item["id"] in result:
            raise ContentError(f"duplicate {section} id: {item['id']}")
        result[item["id"]] = item
    return result


def _ranks(value: Any, context: str) -> None:
    if not isinstance(value, list) or not value:
        raise ContentError(f"{context} must be a non-empty rank list")
    if any(type(rank) is not int or rank not in range(1, 5) for rank in value):
        raise ContentError(f"{context} contains a rank outside 1..4")
    if value != sorted(set(value)):
        raise ContentError(f"{context} must contain unique ranks in ascending order")


def _effects(value: Any, allowed: set[str], context: str) -> None:
    if not isinstance(value, list) or not value:
        raise ContentError(f"{context} must be a non-empty effect list")
    for index, effect in enumerate(value):
        if not isinstance(effect, dict) or effect.get("op") not in allowed:
            raise ContentError(f"{context}[{index}] has an unknown effect operation")
        _fields(effect, "op amount target status bonus bonus_status condition_status condition_target_state condition_actor_state", f"{context}[{index}]")
        if "amount" in effect and (type(effect["amount"]) is not int or abs(effect["amount"]) > 1_000_000):
            raise ContentError(f"{context}[{index}].amount must be an integer within -1000000..1000000")
        if effect["op"] != "cleanse" and "amount" not in effect:
            raise ContentError(f"{context}[{index}] needs an amount")
        if effect["op"] in {"damage", "block", "heal", "draw", "discard", "energy", "guard", "status"} and effect["amount"] < 0:
            raise ContentError(f"{context}[{index}] requires a non-negative amount")
        if "bonus_status" in effect and (
            effect["op"] != "damage"
            or effect["bonus_status"] not in CARD_STATUSES
            or type(effect.get("bonus")) is not int
            or effect["bonus"] <= 0
        ):
            raise ContentError(f"{context}[{index}] has an invalid status damage bonus")
        if effect.get("target") not in {None, "self", "all_enemies", "all_allies"}:
            raise ContentError(f"{context}[{index}].target is invalid")
        if effect["op"] == "status" and (
            effect.get("status") not in CARD_STATUSES or effect.get("amount", 0) <= 0
        ):
            raise ContentError(f"{context}[{index}] has an invalid status effect")
        if effect["op"] == "move" and effect.get("amount") == 0:
            raise ContentError(f"{context}[{index}] has a zero-distance move")
        if "condition_status" in effect and effect["condition_status"] not in CARD_STATUSES:
            raise ContentError(f"{context}[{index}].condition_status is invalid")
        for field in ("condition_target_state", "condition_actor_state"):
            if field in effect and effect[field] not in CARD_STATES:
                raise ContentError(f"{context}[{index}].{field} is invalid")


def _derived_card_tags(card: dict[str, Any]) -> set[str]:
    tags: set[str] = set()
    for effect in card["effects"] + card["upgrade_effects"]:
        op = effect["op"]
        if op == "damage":
            tags.add("damage")
        elif op == "block":
            tags.add("block")
        elif op == "heal":
            tags.add("recovery")
        elif op == "stress":
            tags.add("stress_relief" if effect["amount"] < 0 else "stress_risk")
        elif op == "move":
            target = effect.get("target", card["target"])
            tags.add("displacement" if target in {"enemy", "all_enemies"} else "mobility")
        elif op in {"guard", "cleanse", "draw", "discard", "energy"}:
            tags.add(op)
        elif op == "status":
            status = effect["status"]
            target = effect.get("target", card["target"])
            targets_enemy = target in {"enemy", "all_enemies"}
            if status in {"marked", "vulnerable", "wound"} and targets_enemy:
                tags.add(f"setup:{status}")
            elif status in {"weak", "stun"} and targets_enemy:
                tags.add("control")
            elif status in {"dodge", "focus", "riposte"}:
                tags.add(f"status:{status}")
        bonus_status = effect.get("bonus_status") or effect.get("condition_status")
        if bonus_status:
            tags.add(f"payoff:{bonus_status}")
        if "deaths_door" in (effect.get("condition_target_state"), effect.get("condition_actor_state")):
            tags.add("payoff:deaths_door")
    if card.get("biome"):
        tags.add(f"affinity:{card['biome']}")
    return tags


def _art_lines(value: Any, context: str, *, count: int, width: int) -> None:
    if not isinstance(value, list) or len(value) != count:
        raise ContentError(f"{context} must contain exactly {count} lines")
    for index, line in enumerate(value):
        if not isinstance(line, str) or len(line) > width:
            raise ContentError(f"{context}[{index}] must be a string no wider than {width} columns")
        if any(ord(character) < 32 or ord(character) > 126 for character in line):
            raise ContentError(f"{context}[{index}] must contain ASCII characters only")


def _persistent_effects(items: dict[str, dict[str, Any]], section: str) -> None:
    names: set[str] = set()
    for definition in items.values():
        if not isinstance(definition.get("name"), str) or not isinstance(definition.get("description"), str):
            raise ContentError(f"{section[:-1]} {definition['id']} needs a name and description")
        if not definition["name"] or not definition["description"] or definition["name"] in names:
            raise ContentError(f"{section} need unique non-empty names and descriptions")
        names.add(definition["name"])
        effects = definition.get("effects")
        if not isinstance(effects, list) or not effects:
            raise ContentError(f"{section[:-1]} {definition['id']} needs effects")
        for effect in effects:
            if not isinstance(effect, dict) or effect.get("key") not in EFFECT_KEYS:
                raise ContentError(f"{section[:-1]} {definition['id']} has an unknown effect key")
            if "stack" in effect:
                try:
                    persistent_effect(effect)
                except (TypeError, ValueError) as exc:
                    raise ContentError(f"{section[:-1]} {definition['id']}: {exc}") from exc
                continue
            _fields(effect, "key curve amount cap every", f"{section[:-1]} {definition['id']} effect")
            if effect.get("curve") not in EFFECT_CURVES:
                raise ContentError(f"{section[:-1]} {definition['id']} has an invalid stack curve")
            for field in ("amount", "cap"):
                if field in effect and (type(effect[field]) not in (int, float) or effect[field] < 0):
                    raise ContentError(f"{section[:-1]} {definition['id']}.{field} must be nonnegative numeric")
            if effect["curve"] == "threshold" and (
                type(effect.get("every")) is not int or effect["every"] < 1
            ):
                raise ContentError(f"{section[:-1]} {definition['id']} needs a positive threshold")


def _biome_mechanics(biome: dict[str, Any]) -> None:
    context = f"biome {biome['id']}"
    mechanics = biome.get("mechanics")
    if not isinstance(mechanics, dict) or set(mechanics) != {
        "hazard", "traversal", "patrol", "visibility", "combat", "objective"
    }:
        raise ContentError(f"{context} needs all six mechanic sections")
    for section in mechanics.values():
        if not isinstance(section, dict) or not isinstance(section.get("description"), str):
            raise ContentError(f"{context} has an invalid mechanic description")

    hazard = mechanics["hazard"]
    if (
        not isinstance(hazard.get("name"), str)
        or hazard.get("effect") not in BIOME_HAZARD_EFFECTS
        or not isinstance(hazard.get("amount"), int)
        or (hazard["effect"].startswith("status_") and not isinstance(hazard.get("status"), str))
    ):
        raise ContentError(f"{context} has an invalid hazard")

    traversal = mechanics["traversal"]
    if traversal.get("cost") not in {1, 2, 3}:
        raise ContentError(f"{context} has an invalid traversal cost")

    patrol = mechanics["patrol"]
    if (
        patrol.get("behavior") not in BIOME_PATROL_BEHAVIORS
        or not isinstance(patrol.get("aggression"), int)
        or not 4 <= patrol["aggression"] <= 16
        or patrol.get("cadence") not in {1, 2, 3}
        or not isinstance(patrol.get("leash"), int)
        or not 4 <= patrol["leash"] <= 12
    ):
        raise ContentError(f"{context} has invalid patrol rules")

    visibility = mechanics["visibility"]
    if any(
        not isinstance(visibility.get(field), int) or visibility[field] < 1
        for field in ("patrol_radius", "hazard_radius")
    ):
        raise ContentError(f"{context} has invalid visibility rules")

    combat = mechanics["combat"]
    effects = combat.get("effects")
    if not isinstance(combat.get("name"), str) or not isinstance(effects, list) or not effects:
        raise ContentError(f"{context} has an invalid combat environment")
    for effect in effects:
        if (
            not isinstance(effect, dict)
            or effect.get("target") not in BIOME_COMBAT_TARGETS
            or effect.get("op") not in BIOME_COMBAT_OPS
            or not isinstance(effect.get("amount"), int)
            or (effect["op"] == "status" and not isinstance(effect.get("status"), str))
        ):
            raise ContentError(f"{context} has an invalid combat effect")

    objective = mechanics["objective"]
    if (
        not isinstance(objective.get("name"), str)
        or not objective["name"].strip()
        or not isinstance(objective.get("description"), str)
        or not objective["description"].strip()
    ):
        raise ContentError(f"{context} has an invalid objective identity")


def load_catalog(path: Path | None = None) -> Catalog:
    try:
        return _bundled_catalog() if path is None else _load_catalog(path)
    except ContentError:
        raise
    except (OSError, ValueError, UnicodeError) as exc:
        raise ContentError(f"cannot establish runtime content contracts: {exc}") from exc


@lru_cache(maxsize=1)
def _bundled_catalog() -> Catalog:
    return _load_catalog(None)


@lru_cache(maxsize=1)
def load_legacy_catalog() -> Catalog:
    from .migrations import LEGACY_20_FINGERPRINT

    root = Path(str(files("dumbest_dungeon.data").joinpath("legacy20")))
    catalog = _load_catalog(root / "game.json", assets=root)
    if catalog.raw["schema_version"] != 20 or catalog.manifest.fingerprint != LEGACY_20_FINGERPRINT:
        raise ContentError("historical content-20 bundle does not match its recorded fingerprint")
    return catalog


def _load_catalog(path: Path | None, *, assets: Path | None = None) -> Catalog:
    data_root = files("dumbest_dungeon.data")
    source = path or Path(str(data_root.joinpath("game.json")))
    try:
        raw = loads(source.read_text(encoding="utf-8"))
    except (OSError, JsonDataError, UnicodeError) as exc:
        raise ContentError(f"cannot load content from {source}: {exc}") from exc
    art_source = (assets / "art.json") if assets else Path(str(data_root.joinpath("art.json")))
    try:
        art = loads(art_source.read_text(encoding="utf-8"))
    except (OSError, JsonDataError, UnicodeError) as exc:
        raise ContentError(f"cannot load ASCII art from {art_source}: {exc}") from exc
    metadata_source = (assets / "card_metadata.json") if assets else Path(str(data_root.joinpath("card_metadata.json")))
    try:
        card_metadata = loads(metadata_source.read_text(encoding="utf-8"))
    except (OSError, JsonDataError, UnicodeError) as exc:
        raise ContentError(f"cannot load card metadata from {metadata_source}: {exc}") from exc
    return _catalog_from_documents(raw, art, card_metadata)


def load_rules(rules: dict) -> Catalog:
    try:
        return _compiled_rules(canonical_bytes(rules))
    except (KeyError, TypeError, ValueError) as exc:
        raise ContentError(f"invalid saved content rules: {exc}") from exc


@lru_cache(maxsize=8)
def _compiled_rules(encoded: bytes) -> Catalog:
    rules = loads(encoded.decode("ascii"))
    if not isinstance(rules, dict) or type(rules.get("content_schema")) is not int:
        raise ContentError("saved rules require a registered content schema")
    sections = set(CONTENT_FIELDS)
    if rules["content_schema"] < 22:
        sections.remove("mutations")
    if set(rules) != sections | {"balance", "art", "content_schema"}:
        raise ContentError("saved rules require exactly the registered content sections")
    raw = {"schema_version": rules["content_schema"], "balance": rules["balance"]}
    for name in sections:
        definitions = rules[name]
        if not isinstance(definitions, dict) or any(not isinstance(row, dict) or identity != row.get("id") for identity, row in definitions.items()):
            raise ContentError(f"saved {name} identities do not match their definitions")
        raw[name] = list(definitions.values())
    metadata = {"schema_version": 1, "cards": {
        identity: {"tags": card["tags"], "upgrade_description": card["upgrade_description"]}
        for identity, card in rules["cards"].items()}}
    return _catalog_from_documents(raw, rules["art"], metadata)


def _catalog_from_documents(raw: dict, art: dict, card_metadata: dict) -> Catalog:

    _fields(raw, "schema_version balance " + " ".join(CONTENT_FIELDS), "content root")
    _fields(art, "schema_version title heroes enemies card_glyphs card_marks curse_card_glyph curse_card_mark", "art root")
    _fields(card_metadata, "schema_version cards", "card metadata root")
    if type(raw.get("schema_version")) is not int or raw["schema_version"] not in {20, 21, CONTENT_SCHEMA}:
        raise ContentError(f"content schema_version must be historical 20/21 or current {CONTENT_SCHEMA}")
    if art.get("schema_version") != 1:
        raise ContentError("ASCII art schema_version must be 1")
    heroes = _indexed(raw.get("heroes"), "heroes")
    squads = _indexed(raw.get("squads"), "squads")
    cards = _indexed(raw.get("cards"), "cards")
    enemies = _indexed(raw.get("enemies"), "enemies")
    encounters = _indexed(raw.get("encounters"), "encounters")
    events = _indexed(raw.get("events"), "events")
    landmarks = _indexed(raw.get("landmarks"), "landmarks")
    missions = _indexed(raw.get("missions"), "missions")
    facilities = _indexed(raw.get("facilities"), "facilities")
    terrains = _indexed(raw.get("terrains"), "terrains")
    terrain_patterns = _indexed(raw.get("terrain_patterns"), "terrain_patterns")
    biomes = _indexed(raw.get("biomes"), "biomes")
    worlds = _indexed(raw.get("worlds"), "worlds")
    boons = _indexed(raw.get("boons"), "boons")
    curses = _indexed(raw.get("curses"), "curses")
    items = _indexed(raw.get("items"), "items")
    mutations = _indexed(raw.get("mutations", []), "mutations")
    afflictions = _indexed(raw.get("afflictions"), "afflictions")
    balance = raw.get("balance")
    if not isinstance(balance, dict):
        raise ContentError("balance must be an object")
    if card_metadata.get("schema_version") != 1 or not isinstance(card_metadata.get("cards"), dict):
        raise ContentError("card metadata schema is invalid")
    if set(card_metadata["cards"]) != set(cards):
        raise ContentError("card metadata must cover every technique exactly once")
    for card_id, metadata in card_metadata["cards"].items():
        if not isinstance(metadata, dict) or set(metadata) != {"tags", "upgrade_description"}:
            raise ContentError(f"card metadata for {card_id} is malformed")
        cards[card_id].setdefault("tags", metadata["tags"])
        cards[card_id].setdefault("upgrade_description", metadata["upgrade_description"])

    sections = {
        "heroes": heroes,
        "squads": squads,
        "cards": cards,
        "enemies": enemies,
        "encounters": encounters,
        "events": events,
        "landmarks": landmarks,
        "missions": missions,
        "facilities": facilities,
        "terrains": terrains,
        "terrain_patterns": terrain_patterns,
        "biomes": biomes,
        "worlds": worlds,
        "boons": boons,
        "curses": curses,
        "items": items,
        "afflictions": afflictions,
    }
    if any(not definitions for definitions in sections.values()):
        raise ContentError("content sections must not be empty")
    if cards.keys() & curses.keys():
        raise ContentError("technique and curse-card ids must not overlap")

    mutation_effects: set[str] = set()
    for mutation in mutations.values():
        context = f"mutation {mutation['id']}"
        for field in ("name", "marker", "description", "kind", "biomes", "effect", "amount",
                      "compatible_kinds", "excludes", "min_band", "priority"):
            if field not in mutation:
                raise ContentError(f"{context} is missing {field}")
        if (not all(isinstance(mutation[field], str) and mutation[field]
                    for field in ("name", "marker", "description", "kind", "effect", "min_band"))
            or re.fullmatch(r"[A-Z0-9:+/_-]{3,20}", mutation["marker"]) is None):
            raise ContentError(f"{context} has an invalid identity or visible marker")
        if mutation["kind"] not in {"general", "biome"}:
            raise ContentError(f"{context} has an invalid kind")
        if mutation["effect"] not in MUTATION_EFFECTS or mutation["effect"] in mutation_effects:
            raise ContentError(f"{context} needs a distinct registered tactical effect")
        mutation_effects.add(mutation["effect"])
        if type(mutation["amount"]) is not int or not 1 <= mutation["amount"] <= 99:
            raise ContentError(f"{context} has an invalid amount")
        if (not isinstance(mutation["biomes"], list)
            or any(biome not in biomes for biome in mutation["biomes"])
            or mutation["kind"] == "biome" and not mutation["biomes"]):
            raise ContentError(f"{context} has invalid biome compatibility")
        if (not isinstance(mutation["compatible_kinds"], list)
            or not mutation["compatible_kinds"]
            or any(kind not in {"normal", "elite", "boss"} for kind in mutation["compatible_kinds"])
            or len(set(mutation["compatible_kinds"])) != len(mutation["compatible_kinds"])):
            raise ContentError(f"{context} has invalid encounter compatibility")
        if (not isinstance(mutation["excludes"], list)
            or any(other == mutation["id"] or other not in mutations for other in mutation["excludes"])
            or len(set(mutation["excludes"])) != len(mutation["excludes"])):
            raise ContentError(f"{context} has invalid exclusions")
        if mutation["min_band"] not in MUTATION_BANDS:
            raise ContentError(f"{context} has an invalid minimum Pressure band")
        if type(mutation["priority"]) is not int or not 0 <= mutation["priority"] <= 999:
            raise ContentError(f"{context} has an invalid priority")

    hero_names: set[str] = set()
    hero_roles: set[str] = set()

    for hero in heroes.values():
        for field in (
            "name", "role", "combat_role", "complexity", "preferred_ranks", "signature",
            "strength", "weakness", "builds", "summary", "max_hp", "rank", "starter_deck",
        ):
            if field not in hero:
                raise ContentError(f"hero {hero['id']} is missing {field}")
        if hero["rank"] not in range(1, 5) or hero["max_hp"] <= 0:
            raise ContentError(f"hero {hero['id']} has invalid rank or max_hp")
        if not isinstance(hero["summary"], str) or not hero["summary"]:
            raise ContentError(f"hero {hero['id']} needs a summary")
        if hero["combat_role"] not in {"controller", "defender", "striker", "support"}:
            raise ContentError(f"hero {hero['id']} has an invalid combat role")
        if hero["complexity"] not in {1, 2, 3}:
            raise ContentError(f"hero {hero['id']} has an invalid complexity")
        _ranks(hero["preferred_ranks"], f"hero {hero['id']}.preferred_ranks")
        if hero["rank"] not in hero["preferred_ranks"]:
            raise ContentError(f"hero {hero['id']} default rank is not preferred")
        for field in ("signature", "strength", "weakness"):
            if not isinstance(hero[field], str) or not hero[field]:
                raise ContentError(f"hero {hero['id']} needs a {field}")
        if (
            not isinstance(hero["builds"], list)
            or len(hero["builds"]) != 2
            or len(set(hero["builds"])) != 2
            or any(not isinstance(build, str) or not build for build in hero["builds"])
        ):
            raise ContentError(f"hero {hero['id']} needs two distinct build paths")
        if hero["name"] in hero_names or hero["role"] in hero_roles:
            raise ContentError("heroes need unique names and archetype roles")
        hero_names.add(hero["name"])
        hero_roles.add(hero["role"])
        if not isinstance(hero["starter_deck"], list) or len(hero["starter_deck"]) != 5:
            raise ContentError(f"hero {hero['id']} must contribute five starter cards")
        for card_id in hero["starter_deck"]:
            if card_id not in cards:
                raise ContentError(f"hero {hero['id']} references unknown card {card_id}")
            if cards[card_id].get("hero") != hero["id"]:
                raise ContentError(f"hero {hero['id']} cannot start with another owner's card")
        owner_cards = [card for card in cards.values() if card.get("hero") == hero["id"]]
        if len(owner_cards) < 5:
            raise ContentError(f"hero {hero['id']} needs at least five techniques")
        starter_ids = set(hero["starter_deck"])
        if sum(card["id"] not in starter_ids for card in owner_cards) < 3:
            raise ContentError(f"hero {hero['id']} needs at least three non-starter techniques")
        for rank in range(1, 5):
            if not any(rank in cards[card_id].get("from_ranks", []) for card_id in hero["starter_deck"]):
                raise ContentError(f"hero {hero['id']} has no starter usable from rank {rank}")

        if hero.get("biome") is not None and hero["biome"] not in biomes:
            raise ContentError(f"hero {hero['id']} references an unknown biome")
    squad_names: set[str] = set()
    for squad in squads.values():
        for field in (
            "name", "playstyle", "complexity", "formation", "strength", "weakness", "signature",
        ):
            if field not in squad:
                raise ContentError(f"squad {squad['id']} is missing {field}")
        if squad["name"] in squad_names or not all(
            isinstance(squad[field], str) and squad[field]
            for field in ("name", "playstyle", "strength", "weakness", "signature")
        ):
            raise ContentError("squads need unique names and complete descriptions")
        squad_names.add(squad["name"])
        if squad["complexity"] not in {1, 2, 3}:
            raise ContentError(f"squad {squad['id']} has an invalid complexity")
        formation = squad["formation"]
        if (
            not isinstance(formation, list)
            or len(formation) != 4
            or len(set(formation)) != 4
            or any(hero_id not in heroes for hero_id in formation)
        ):
            raise ContentError(f"squad {squad['id']} needs four unique known crew")
        for rank, hero_id in enumerate(formation, 1):
            if rank not in heroes[hero_id]["preferred_ranks"]:
                raise ContentError(
                    f"squad {squad['id']} places {hero_id} outside a preferred rank"
                )
    card_names: set[str] = set()
    for card in cards.values():
        if "lanes" in card and (not isinstance(card["lanes"], list) or not card["lanes"]
                                or any(lane not in {item.value for item in Lane} for lane in card["lanes"])
                                or len(set(card["lanes"])) != len(card["lanes"])):
            raise ContentError(f"card {card['id']} has invalid acquisition lanes")
        if not isinstance(card.get("name"), str) or not isinstance(card.get("description"), str):
            raise ContentError(f"card {card['id']} needs a name and description")
        if card.get("hero") not in heroes:
            raise ContentError(f"card {card['id']} references an unknown hero")
        if not card["name"] or not card["description"] or card["name"] in card_names:
            raise ContentError("techniques need unique non-empty names and descriptions")
        card_names.add(card["name"])
        if card.get("target") not in TARGETS:
            raise ContentError(f"card {card['id']} has an invalid target")
        if type(card.get("cost")) is not int or not 0 <= card["cost"] <= 99:
            raise ContentError(f"card {card['id']} has an invalid cost")
        _ranks(card.get("from_ranks"), f"card {card['id']}.from_ranks")
        if card["target"] in {"enemy", "all_enemies"}:
            _ranks(card.get("target_ranks"), f"card {card['id']}.target_ranks")
        _effects(card.get("effects"), CARD_EFFECTS, f"card {card['id']}.effects")
        _effects(card.get("upgrade_effects"), CARD_EFFECTS, f"card {card['id']}.upgrade_effects")
        if card["effects"] == card["upgrade_effects"]:
            raise ContentError(f"card {card['id']} needs a meaningful upgrade")
        tags = card.get("tags", [])
        if not isinstance(tags, list) or any(
            not isinstance(tag, str)
            or (
                tag not in CARD_TAGS
                and not tag.startswith(("setup:", "payoff:", "status:", "affinity:"))
            )
            for tag in tags
        ):
            raise ContentError(f"card {card['id']} has invalid build tags")
        for tag in tags:
            if tag.startswith(("setup:", "payoff:", "status:")):
                status = tag.split(":", 1)[1]
                if status not in CARD_STATUSES | {"deaths_door"}:
                    raise ContentError(f"card {card['id']} has an unknown status build tag")
            if tag.startswith("affinity:") and tag.split(":", 1)[1] not in biomes:
                raise ContentError(f"card {card['id']} has an unknown affinity build tag")
        missing_tags = _derived_card_tags(card) - set(tags)
        if missing_tags:
            raise ContentError(
                f"card {card['id']} is missing authored build tags: {', '.join(sorted(missing_tags))}"
            )
        contradicted_tags = set(tags) - _derived_card_tags(card)
        if contradicted_tags:
            raise ContentError(f"card {card['id']} has tags unsupported by effects: {', '.join(sorted(contradicted_tags))}")
        if not isinstance(card.get("upgrade_description"), str) or not card["upgrade_description"]:
            raise ContentError(f"card {card['id']} needs an upgrade description")
        if "upgrade_cost" in card and (
            not isinstance(card["upgrade_cost"], int) or card["upgrade_cost"] < 0
        ):
            raise ContentError(f"card {card['id']} has an invalid upgrade cost")
        if card.get("biome") is not None:
            if card["biome"] not in biomes:
                raise ContentError(f"card {card['id']} references an unknown biome")
            if heroes[card["hero"]].get("biome") != card["biome"]:
                raise ContentError(f"card {card['id']} does not match its hero's biome")
            if not isinstance(card.get("biome_bonus"), int) or not 1 <= card["biome_bonus"] <= 3:
                raise ContentError(f"card {card['id']} has an invalid biome bonus")
    enemy_names: set[str] = set()
    for enemy in enemies.values():
        if not isinstance(enemy.get("name"), str) or not enemy["name"] or enemy["name"] in enemy_names:
            raise ContentError("enemies need unique non-empty names")
        enemy_names.add(enemy["name"])
        if type(enemy.get("max_hp")) is not int or not 1 <= enemy["max_hp"] <= 1_000_000:
            raise ContentError(f"enemy {enemy['id']} has invalid max_hp")
        actions = enemy.get("actions")
        if not isinstance(actions, list) or not actions:
            raise ContentError(f"enemy {enemy['id']} needs actions")
        action_names = set()
        for action in actions:
            _fields(action, "name target effects weight", f"enemy {enemy['id']} action")
            if (not isinstance(action.get("name"), str) or not action["name"]
                or action["name"] in action_names or action.get("target") not in ENEMY_TARGETS):
                raise ContentError(f"enemy {enemy['id']} has an invalid action")
            if type(action.get("weight", 1)) is not int or not 1 <= action.get("weight", 1) <= 1000:
                raise ContentError(f"enemy {enemy['id']} has an invalid action weight")
            action_names.add(action["name"])
            _effects(action.get("effects"), CARD_EFFECTS, f"enemy {enemy['id']} action")
        enemy_biomes = enemy.get("biomes", ["derelict"])
        if not isinstance(enemy_biomes, list) or not enemy_biomes or any(
            biome_id not in biomes for biome_id in enemy_biomes
        ):
            raise ContentError(f"enemy {enemy['id']} has invalid biomes")
    for encounter in encounters.values():
        if encounter.get("kind") not in {"normal", "elite", "boss"}:
            raise ContentError(f"encounter {encounter['id']} has invalid kind")
        members = encounter.get("enemies")
        if not isinstance(members, list) or not 1 <= len(members) <= 4:
            raise ContentError(f"encounter {encounter['id']} must contain 1..4 enemies")
        for enemy_id in members:
            if enemy_id not in enemies:
                raise ContentError(f"encounter {encounter['id']} references {enemy_id}")
        encounter_biomes = encounter.get("biomes", ["derelict"])
        if not isinstance(encounter_biomes, list) or not encounter_biomes or any(
            biome_id not in biomes for biome_id in encounter_biomes
        ):
            raise ContentError(f"encounter {encounter['id']} has invalid biomes")
        if encounter["kind"] != "boss" and any(
            biome_id not in enemies[enemy_id].get("biomes", ["derelict"])
            for biome_id in encounter_biomes
            for enemy_id in members
        ):
            raise ContentError(f"encounter {encounter['id']} mixes incompatible biome enemies")
    terrain_glyphs: set[str] = set()
    for terrain in terrains.values():
        if (
            not isinstance(terrain.get("name"), str)
            or not terrain["name"]
            or not isinstance(terrain.get("description"), str)
            or not terrain["description"]
            or not isinstance(terrain.get("glyph"), str)
            or len(terrain["glyph"]) != 1
            or terrain["glyph"] in terrain_glyphs
            or terrain.get("cost") not in {1, 2, 3}
            or terrain.get("biome") is not None
            and terrain["biome"] not in biomes
        ):
            raise ContentError(f"terrain {terrain['id']} is invalid")
        terrain_glyphs.add(terrain["glyph"])
    if terrain_glyphs != TERRAIN_GLYPHS:
        raise ContentError("terrain profiles must cover every traversable ASCII glyph exactly once")
    pattern_biomes: set[str] = set()
    for pattern in terrain_patterns.values():
        if (
            pattern.get("biome") not in biomes
            or pattern["biome"] in pattern_biomes
            or pattern.get("mode") not in {"bands", "channels", "pockets"}
            or pattern.get("glyph") not in {",", "="}
            or pattern["mode"] in {"bands", "channels"} and pattern["glyph"] != "="
            or pattern["mode"] == "pockets" and pattern["glyph"] != ","
            or not isinstance(pattern.get("description"), str)
            or not pattern["description"]
        ):
            raise ContentError(f"terrain pattern {pattern['id']} is invalid")
        pattern_biomes.add(pattern["biome"])
    if pattern_biomes != set(biomes):
        raise ContentError("every biome needs exactly one secondary terrain pattern")
    landmark_biomes: set[str] = set()
    for landmark in landmarks.values():
        landmark_art = landmark.get("art")
        if (
            landmark.get("biome") not in biomes
            or landmark["biome"] in landmark_biomes
            or not isinstance(landmark.get("name"), str)
            or not landmark["name"]
            or not isinstance(landmark_art, list)
            or len(landmark_art) != 3
            or any(
                not isinstance(line, str)
                or len(line) != 3
                or not line.isascii()
                or not line.isprintable()
                for line in landmark_art
            )
            or landmark_art[1][1] != "K"
        ):
            raise ContentError(f"landmark {landmark['id']} is invalid")
        landmark_biomes.add(landmark["biome"])
    if landmark_biomes != set(biomes):
        raise ContentError("every biome needs exactly one objective landmark template")
    mission_biomes: set[str] = set()
    outcome_ids: set[str] = set()
    for mission in missions.values():
        approaches = mission.get("approaches")
        if (
            mission.get("biome") not in biomes
            or mission["biome"] in mission_biomes
            or not isinstance(mission.get("name"), str)
            or not mission["name"]
            or not isinstance(mission.get("description"), str)
            or not mission["description"]
            or not isinstance(approaches, list)
            or len(approaches) != 2
        ):
            raise ContentError(f"mission {mission['id']} is invalid")
        approach_ids: set[str] = set()
        for approach in approaches:
            telegraph = approach.get("telegraph")
            cost = approach.get("cost")
            stages = approach.get("stages")
            completion = approach.get("completion")
            if (
                not isinstance(approach.get("id"), str)
                or approach["id"] in approach_ids
                or not all(
                    isinstance(approach.get(field), str) and approach[field]
                    for field in ("label", "summary", "outcome")
                )
                or approach["outcome"] in outcome_ids
                or not isinstance(telegraph, dict)
                or telegraph.get("travel") not in {"short", "medium", "long"}
                or telegraph.get("risk") not in {"low", "guarded", "severe", "unknown"}
                or telegraph.get("combat") not in {"none", "possible", "expected"}
                or not isinstance(telegraph.get("irreversible"), bool)
                or not isinstance(cost, dict)
                or cost.get("resource") not in {"none", "supplies", "light", "stress_all", "health_all"}
                or not isinstance(cost.get("amount"), int)
                or cost["amount"] < 0
                or not isinstance(stages, list)
                or not 1 <= len(stages) <= 3
                or any(
                    not isinstance(stage, dict)
                    or not isinstance(stage.get("label"), str)
                    or not stage["label"]
                    or "effect" in stage
                    and (
                        not isinstance(stage["effect"], dict)
                        or stage["effect"].get("op") not in BIOME_OBJECTIVE_EFFECTS
                        or not isinstance(stage["effect"].get("amount"), int)
                        or stage["effect"].get("op") == "status_all"
                        and stage["effect"].get("status") not in CARD_STATUSES
                    )
                    or stage.get("effect", {}).get("op") == "objective_combat"
                    and index == len(stages) - 1
                    for index, stage in enumerate(stages)
                )
                or not isinstance(completion, dict)
                or completion.get("op") not in BIOME_OBJECTIVE_EFFECTS
                or completion.get("op") == "objective_combat"
                or not isinstance(completion.get("amount"), int)
                or completion.get("op") == "status_all"
                and completion.get("status") not in CARD_STATUSES
            ):
                raise ContentError(f"mission {mission['id']} has an invalid approach")
            approach_ids.add(approach["id"])
            outcome_ids.add(approach["outcome"])
        mission_biomes.add(mission["biome"])
    if mission_biomes != set(biomes):
        raise ContentError("every biome needs exactly one expedition mission")
    facility_biomes: set[str] = set()
    facility_names: set[str] = set()
    for facility in facilities.values():
        options = facility.get("options")
        if (
            facility.get("biome") not in biomes
            or facility["biome"] in facility_biomes
            or not isinstance(facility.get("name"), str)
            or not facility["name"]
            or facility["name"] in facility_names
            or not isinstance(facility.get("description"), str)
            or not facility["description"]
            or not isinstance(options, list)
            or len(options) != 2
        ):
            raise ContentError(f"facility {facility['id']} is invalid")
        option_ids: set[str] = set()
        for option in options:
            cost = option.get("cost")
            effects = option.get("effects")
            if (
                not isinstance(option.get("id"), str)
                or option["id"] in option_ids
                or not isinstance(option.get("label"), str)
                or not option["label"]
                or not isinstance(option.get("summary"), str)
                or not option["summary"]
                or option.get("risk") not in {"low", "guarded", "severe", "unknown"}
                or not isinstance(cost, dict)
                or cost.get("resource") not in {
                    "none", "supplies", "light", "stress_all", "health_all"
                }
                or not isinstance(cost.get("amount"), int)
                or cost["amount"] < 0
                or not isinstance(effects, list)
                or not effects
                or any(
                    not isinstance(effect, dict)
                    or effect.get("op") not in FACILITY_EFFECTS
                    or not isinstance(effect.get("amount"), int)
                    or effect["op"] == "status_all"
                    and effect.get("status") not in CARD_STATUSES
                    for effect in effects
                )
            ):
                raise ContentError(f"facility {facility['id']} has an invalid option")
            option_ids.add(option["id"])
        facility_biomes.add(facility["biome"])
        facility_names.add(facility["name"])
    if facility_biomes != set(biomes):
        raise ContentError("every biome needs exactly one expedition facility")
    glyphs = set()
    for biome in biomes.values():
        if not isinstance(biome.get("name"), str) or not isinstance(biome.get("description"), str):
            raise ContentError(f"biome {biome['id']} needs a name and description")
        glyph = biome.get("glyph")
        if not isinstance(glyph, str) or len(glyph) != 1 or glyph not in "._~\";:`'%-o":
            raise ContentError(f"biome {biome['id']} has an invalid floor glyph")
        if glyph in glyphs:
            raise ContentError(f"biome {biome['id']} reuses a floor glyph")
        glyphs.add(glyph)
        terrain = next(
            (item for item in terrains.values() if item["glyph"] == glyph),
            None,
        )
        if terrain is None or terrain.get("biome") != biome["id"]:
            raise ContentError(f"biome {biome['id']} needs a matching terrain profile")
        _biome_mechanics(biome)

    for world in worlds.values():
        if not isinstance(world.get("name"), str) or not isinstance(world.get("description"), str):
            raise ContentError(f"world {world['id']} needs a name and description")
        if world.get("layout") not in {"branching", "spine", "ring", "clusters", "zigzag", "fracture"}:
            raise ContentError(f"world {world['id']} has an invalid layout")
        world_biomes = world.get("biomes")
        if not isinstance(world_biomes, list) or len(world_biomes) != 4 or any(
            biome_id not in biomes for biome_id in world_biomes
        ) or len(set(world_biomes)) != 4:
            raise ContentError(f"world {world['id']} must contain four known biomes")
    for biome_id in biomes:
        for kind in ("normal", "elite"):
            if not any(
                encounter["kind"] == kind
                and biome_id in encounter.get("biomes", ["derelict"])
                for encounter in encounters.values()
            ):
                raise ContentError(f"biome {biome_id} has no {kind} encounter pool")

    for event in events.values():
        choices = event.get("choices")
        event_biomes = event.get("biomes")
        if (
            not isinstance(event.get("name"), str)
            or not event["name"]
            or not isinstance(event.get("text"), str)
            or not event["text"]
            or not isinstance(event_biomes, list)
            or not event_biomes
            or len(set(event_biomes)) != len(event_biomes)
            or any(biome_id not in biomes for biome_id in event_biomes)
            or not isinstance(choices, list)
            or len(choices) < 2
        ):
            raise ContentError(f"event {event['id']} needs at least two choices")
        for choice in choices:
            if (
                not isinstance(choice.get("label"), str)
                or not isinstance(choice.get("summary"), str)
                or not choice["summary"]
                or choice.get("risk") not in {"low", "guarded", "severe", "unknown"}
            ):
                raise ContentError(f"event {event['id']} choice needs a label")
            if not isinstance(choice.get("cost_supplies", 0), int) or choice.get("cost_supplies", 0) < 0:
                raise ContentError(f"event {event['id']} choice has an invalid supply cost")
            _effects(choice.get("effects"), EVENT_EFFECTS, f"event {event['id']} choice")
            if any(
                effect["op"] == "status_all"
                and effect.get("status") not in CARD_STATUSES
                for effect in choice["effects"]
            ):
                raise ContentError(f"event {event['id']} choice has an invalid status")
    uncovered_event_biomes = set(biomes) - {
        biome_id
        for event in events.values()
        for biome_id in event["biomes"]
    }
    if uncovered_event_biomes:
        raise ContentError("every biome needs at least one compatible event")

    _persistent_effects(boons, "boons")
    _persistent_effects(curses, "curses")
    _persistent_effects(items, "items")
    for boon in boons.values():
        tags = boon.get("tags", [])
        requirements = boon.get("requires_all_tags", [])
        if not isinstance(tags, list) or not all(isinstance(tag, str) for tag in tags):
            raise ContentError(f"boon {boon['id']} has invalid tags")
        if not isinstance(requirements, list) or not all(isinstance(tag, str) for tag in requirements):
            raise ContentError(f"boon {boon['id']} has invalid prerequisites")
    for curse in curses.values():
        if curse.get("kind") not in {"trait", "card"}:
            raise ContentError(f"curse {curse['id']} must be a trait or card")
    required_balance = {
        "hand_size",
        "energy",
        "exploration_steps_per_light",
        "maximum_navigation_distance",
        "death_chance",
        "low_light_threshold",
    }
    missing = required_balance - balance.keys()
    if missing:
        raise ContentError(f"balance is missing: {', '.join(sorted(missing))}")
    for name in required_balance:
        if not isinstance(balance[name], (int, float)) or balance[name] < 0:
            raise ContentError(f"balance.{name} must be a non-negative number")
    if not 0 <= balance["death_chance"] <= 1:
        raise ContentError("balance.death_chance must be between 0 and 1")
    if balance["exploration_steps_per_light"] < 1:
        raise ContentError("balance.exploration_steps_per_light must be at least 1")
    if balance["maximum_navigation_distance"] < 1:
        raise ContentError("balance.maximum_navigation_distance must be at least 1")

    _art_lines(art.get("title"), "art.title", count=6, width=72)
    for hero_id in heroes:
        _art_lines(art.get("heroes", {}).get(hero_id), f"art.heroes.{hero_id}", count=5, width=7)
        _art_lines(art.get("card_glyphs", {}).get(hero_id), f"art.card_glyphs.{hero_id}", count=3, width=9)
        card_mark = art.get("card_marks", {}).get(hero_id)
        if (
            not isinstance(card_mark, str)
            or len(card_mark) != 1
            or not card_mark.isascii()
            or not card_mark.isprintable()
        ):
            raise ContentError(f"art.card_marks.{hero_id} must be one printable ASCII character")
    for enemy_id in enemies:
        _art_lines(art.get("enemies", {}).get(enemy_id), f"art.enemies.{enemy_id}", count=5, width=7)

    _art_lines(art.get("curse_card_glyph"), "art.curse_card_glyph", count=3, width=9)
    curse_mark = art.get("curse_card_mark")
    if not isinstance(curse_mark, str) or len(curse_mark) != 1 or not curse_mark.isascii():
        raise ContentError("art.curse_card_mark must be one ASCII character")

    return Catalog(
        raw,
        heroes,
        squads,
        cards,
        enemies,
        encounters,
        events,
        landmarks,
        missions,
        facilities,
        terrains,
        terrain_patterns,
        biomes,
        worlds,
        boons,
        curses,
        items,
        mutations,
        afflictions,
        balance,
        art,
    )
