"""Load and validate the JSON game catalog."""

from __future__ import annotations

import json
from dataclasses import dataclass
from importlib.resources import files
from pathlib import Path
from typing import Any


class ContentError(ValueError):
    """Raised when bundled or user-supplied content is inconsistent."""


@dataclass(frozen=True)
class Catalog:
    raw: dict[str, Any]
    heroes: dict[str, dict[str, Any]]
    cards: dict[str, dict[str, Any]]
    enemies: dict[str, dict[str, Any]]
    encounters: dict[str, dict[str, Any]]
    events: dict[str, dict[str, Any]]
    biomes: dict[str, dict[str, Any]]
    worlds: dict[str, dict[str, Any]]
    boons: dict[str, dict[str, Any]]
    curses: dict[str, dict[str, Any]]
    items: dict[str, dict[str, Any]]
    afflictions: dict[str, dict[str, Any]]
    balance: dict[str, Any]
    art: dict[str, Any]


CARD_EFFECTS = {
    "damage",
    "block",
    "heal",
    "stress",
    "move",
    "guard",
    "status",
    "draw",
    "discard",
    "energy",
    "cleanse",
}
EVENT_EFFECTS = {
    "light",
    "supplies",
    "heal_all",
    "stress_all",
    "damage_random",
    "card_reward",
}
TARGETS = {"enemy", "all_enemies", "self", "ally", "all_allies"}
ENEMY_TARGETS = {"front", "back", "random", "stressed", "self", "all_heroes", "weakest_enemy"}
EFFECT_CURVES = {"linear", "diminishing", "threshold", "special"}
CARD_STATES = {"deaths_door", "stressed", "healthy", "wounded"}
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
EFFECT_KEYS = {
    "adrenal_block",
    "boon_offer_choices",
    "combat_victory_heal",
    "countercurrent_draw",
    "curse_dead_draw",
    "curse_draw_energy",
    "curse_draw_move",
    "curse_draw_stress",
    "curse_draw_wound",
    "curse_held_stress",
    "damage_bonus",
    "death_chance_reduction",
    "deflection",
    "first_card_cost_increase",
    "first_round_energy",
    "forced_move_bonus",
    "forced_move_reduction",
    "healing_bonus",
    "healing_reduction",
    "incoming_damage_bonus",
    "incoming_damage_reduction",
    "leaking_light",
    "marked_damage_bonus",
    "mercy_block",
    "night_terror_stress",
    "opening_hand",
    "outgoing_damage_reduction",
    "patrol_aggression_reduction",
    "reserve_energy",
    "resonant_energy",
    "reward_choices",
    "salvage_copies",
    "scavenger_stress",
    "stacked_start_block",
    "start_block",
    "start_dodge",
    "start_marked",
    "start_stress_relief",
    "start_vulnerable",
    "stress_bonus",
    "stress_reduction",
    "supply_heal_bonus",
    "supply_light_bonus",
    "survey_reach",
    "quick_hands",
    "second_wind",
    "stressed_damage_bonus",
    "tremor_block_reduction",
    "wound_reduction",
}


def _indexed(items: Any, section: str) -> dict[str, dict[str, Any]]:
    if not isinstance(items, list):
        raise ContentError(f"{section} must be a list")
    result: dict[str, dict[str, Any]] = {}
    for index, item in enumerate(items):
        if not isinstance(item, dict) or not isinstance(item.get("id"), str):
            raise ContentError(f"{section}[{index}] must have a string id")
        if item["id"] in result:
            raise ContentError(f"duplicate {section} id: {item['id']}")
        result[item["id"]] = item
    return result


def _ranks(value: Any, context: str) -> None:
    if not isinstance(value, list) or not value:
        raise ContentError(f"{context} must be a non-empty rank list")
    if any(not isinstance(rank, int) or rank not in range(1, 5) for rank in value):
        raise ContentError(f"{context} contains a rank outside 1..4")


def _effects(value: Any, allowed: set[str], context: str) -> None:
    if not isinstance(value, list) or not value:
        raise ContentError(f"{context} must be a non-empty effect list")
    for index, effect in enumerate(value):
        if not isinstance(effect, dict) or effect.get("op") not in allowed:
            raise ContentError(f"{context}[{index}] has an unknown effect operation")
        if "amount" in effect and not isinstance(effect["amount"], (int, float)):
            raise ContentError(f"{context}[{index}].amount must be numeric")
        if "bonus_status" in effect and (
            effect["op"] != "damage"
            or not isinstance(effect["bonus_status"], str)
            or not effect["bonus_status"]
            or not isinstance(effect.get("bonus"), (int, float))
            or effect["bonus"] <= 0
        ):
            raise ContentError(f"{context}[{index}] has an invalid status damage bonus")
        if effect.get("target") not in {None, "self", "all_enemies", "all_allies"}:
            raise ContentError(f"{context}[{index}].target is invalid")
        if effect["op"] == "status" and not isinstance(effect.get("status"), str):
            raise ContentError(f"{context}[{index}] status effect needs a status name")
        if "condition_status" in effect and not isinstance(effect["condition_status"], str):
            raise ContentError(f"{context}[{index}].condition_status must be a string")
        for field in ("condition_target_state", "condition_actor_state"):
            if field in effect and effect[field] not in CARD_STATES:
                raise ContentError(f"{context}[{index}].{field} is invalid")


def _art_lines(value: Any, context: str, *, count: int, width: int) -> None:
    if not isinstance(value, list) or len(value) != count:
        raise ContentError(f"{context} must contain exactly {count} lines")
    for index, line in enumerate(value):
        if not isinstance(line, str) or len(line) > width:
            raise ContentError(f"{context}[{index}] must be a string no wider than {width} columns")
        if any(ord(character) < 32 or ord(character) > 126 for character in line):
            raise ContentError(f"{context}[{index}] must contain ASCII characters only")


def _persistent_effects(items: dict[str, dict[str, Any]], section: str) -> None:
    if len(items) != 18:
        raise ContentError(f"this release requires exactly 18 {section}")
    for definition in items.values():
        if not isinstance(definition.get("name"), str) or not isinstance(definition.get("description"), str):
            raise ContentError(f"{section[:-1]} {definition['id']} needs a name and description")
        effects = definition.get("effects")
        if not isinstance(effects, list) or not effects:
            raise ContentError(f"{section[:-1]} {definition['id']} needs effects")
        for effect in effects:
            if not isinstance(effect, dict) or effect.get("key") not in EFFECT_KEYS:
                raise ContentError(f"{section[:-1]} {definition['id']} has an unknown effect key")
            if effect.get("curve") not in EFFECT_CURVES:
                raise ContentError(f"{section[:-1]} {definition['id']} has an invalid stack curve")
            for field in ("amount", "cap"):
                if field in effect and not isinstance(effect[field], (int, float)):
                    raise ContentError(f"{section[:-1]} {definition['id']}.{field} must be numeric")
            if effect["curve"] == "threshold" and (
                not isinstance(effect.get("every"), int) or effect["every"] < 1
            ):
                raise ContentError(f"{section[:-1]} {definition['id']} needs a positive threshold")


def load_catalog(path: Path | None = None) -> Catalog:
    data_root = files("dumbest_dungeon.data")
    source = path or Path(str(data_root.joinpath("game.json")))
    try:
        raw = json.loads(source.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ContentError(f"cannot load content from {source}: {exc}") from exc
    art_source = Path(str(data_root.joinpath("art.json")))
    try:
        art = json.loads(art_source.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ContentError(f"cannot load ASCII art from {art_source}: {exc}") from exc

    if raw.get("schema_version") != 5:
        raise ContentError("content schema_version must be 5")
    if art.get("schema_version") != 1:
        raise ContentError("ASCII art schema_version must be 1")
    heroes = _indexed(raw.get("heroes"), "heroes")
    cards = _indexed(raw.get("cards"), "cards")
    enemies = _indexed(raw.get("enemies"), "enemies")
    encounters = _indexed(raw.get("encounters"), "encounters")
    events = _indexed(raw.get("events"), "events")
    biomes = _indexed(raw.get("biomes"), "biomes")
    worlds = _indexed(raw.get("worlds"), "worlds")
    boons = _indexed(raw.get("boons"), "boons")
    curses = _indexed(raw.get("curses"), "curses")
    items = _indexed(raw.get("items"), "items")
    afflictions = _indexed(raw.get("afflictions"), "afflictions")
    balance = raw.get("balance")
    if not isinstance(balance, dict):
        raise ContentError("balance must be an object")

    for hero in heroes.values():
        for field in ("name", "role", "summary", "max_hp", "rank", "starter_deck"):
            if field not in hero:
                raise ContentError(f"hero {hero['id']} is missing {field}")
        if hero["rank"] not in range(1, 5) or hero["max_hp"] <= 0:
            raise ContentError(f"hero {hero['id']} has invalid rank or max_hp")
        if not isinstance(hero["summary"], str) or not hero["summary"]:
            raise ContentError(f"hero {hero['id']} needs a summary")
        if not isinstance(hero["starter_deck"], list) or len(hero["starter_deck"]) != 5:
            raise ContentError(f"hero {hero['id']} must contribute five starter cards")
        for card_id in hero["starter_deck"]:
            if card_id not in cards:
                raise ContentError(f"hero {hero['id']} references unknown card {card_id}")

        if hero.get("biome") is not None and hero["biome"] not in biomes:
            raise ContentError(f"hero {hero['id']} references an unknown biome")
    if len(heroes) != 25:
        raise ContentError("this release requires exactly twenty-five crew archetypes")

    for card in cards.values():
        if not isinstance(card.get("name"), str) or not isinstance(card.get("description"), str):
            raise ContentError(f"card {card['id']} needs a name and description")
        if card.get("hero") not in heroes:
            raise ContentError(f"card {card['id']} references an unknown hero")
        if card.get("target") not in TARGETS:
            raise ContentError(f"card {card['id']} has an invalid target")
        if not isinstance(card.get("cost"), int) or card["cost"] < 0:
            raise ContentError(f"card {card['id']} has an invalid cost")
        _ranks(card.get("from_ranks"), f"card {card['id']}.from_ranks")
        if card["target"] in {"enemy", "all_enemies"}:
            _ranks(card.get("target_ranks"), f"card {card['id']}.target_ranks")
        _effects(card.get("effects"), CARD_EFFECTS, f"card {card['id']}.effects")
        _effects(card.get("upgrade_effects"), CARD_EFFECTS, f"card {card['id']}.upgrade_effects")
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
        if "upgrade_description" in card and (
            not isinstance(card["upgrade_description"], str) or not card["upgrade_description"]
        ):
            raise ContentError(f"card {card['id']} has an invalid upgrade description")
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
    if len(cards) != 155:
        raise ContentError("this release requires exactly 155 unique cards")

    for enemy in enemies.values():
        if not isinstance(enemy.get("max_hp"), int) or enemy["max_hp"] <= 0:
            raise ContentError(f"enemy {enemy['id']} has invalid max_hp")
        actions = enemy.get("actions")
        if not isinstance(actions, list) or not actions:
            raise ContentError(f"enemy {enemy['id']} needs actions")
        for action in actions:
            if not isinstance(action.get("name"), str) or action.get("target") not in ENEMY_TARGETS:
                raise ContentError(f"enemy {enemy['id']} has an invalid action")
            _effects(action.get("effects"), CARD_EFFECTS, f"enemy {enemy['id']} action")
        enemy_biomes = enemy.get("biomes", ["derelict"])
        if not isinstance(enemy_biomes, list) or not enemy_biomes or any(
            biome_id not in biomes for biome_id in enemy_biomes
        ):
            raise ContentError(f"enemy {enemy['id']} has invalid biomes")
    if len(enemies) != 60:
        raise ContentError("this release requires exactly 60 enemy types")

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

    if len(biomes) != 11:
        raise ContentError("this release requires exactly eleven biomes")
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

    if len(worlds) != 6:
        raise ContentError("this release requires exactly six world types")
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
        if not isinstance(choices, list) or len(choices) < 2:
            raise ContentError(f"event {event['id']} needs at least two choices")
        for choice in choices:
            if not isinstance(choice.get("label"), str):
                raise ContentError(f"event {event['id']} choice needs a label")
            if not isinstance(choice.get("cost_supplies", 0), int) or choice.get("cost_supplies", 0) < 0:
                raise ContentError(f"event {event['id']} choice has an invalid supply cost")
            _effects(choice.get("effects"), EVENT_EFFECTS, f"event {event['id']} choice")

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
    if sum(curse["kind"] == "card" for curse in curses.values()) != 6:
        raise ContentError("this release requires exactly six curse cards")

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
        cards,
        enemies,
        encounters,
        events,
        biomes,
        worlds,
        boons,
        curses,
        items,
        afflictions,
        balance,
        art,
    )
