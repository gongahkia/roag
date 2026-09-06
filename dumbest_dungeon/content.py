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
        if effect.get("target") not in {None, "self", "all_enemies", "all_allies"}:
            raise ContentError(f"{context}[{index}].target is invalid")
        if effect["op"] == "status" and not isinstance(effect.get("status"), str):
            raise ContentError(f"{context}[{index}] status effect needs a status name")


def _art_lines(value: Any, context: str, *, count: int, width: int) -> None:
    if not isinstance(value, list) or len(value) != count:
        raise ContentError(f"{context} must contain exactly {count} lines")
    for index, line in enumerate(value):
        if not isinstance(line, str) or len(line) > width:
            raise ContentError(f"{context}[{index}] must be a string no wider than {width} columns")
        if any(ord(character) < 32 or ord(character) > 126 for character in line):
            raise ContentError(f"{context}[{index}] must contain ASCII characters only")


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

    if raw.get("schema_version") != 3:
        raise ContentError("content schema_version must be 3")
    if art.get("schema_version") != 1:
        raise ContentError("ASCII art schema_version must be 1")
    heroes = _indexed(raw.get("heroes"), "heroes")
    cards = _indexed(raw.get("cards"), "cards")
    enemies = _indexed(raw.get("enemies"), "enemies")
    encounters = _indexed(raw.get("encounters"), "encounters")
    events = _indexed(raw.get("events"), "events")
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

    if len(heroes) != 15:
        raise ContentError("this release requires exactly fifteen crew archetypes")

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
    if len(cards) != 105:
        raise ContentError("this release requires exactly 105 unique cards")

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
    if len(enemies) != 35:
        raise ContentError("this release requires exactly 35 enemy types")

    for encounter in encounters.values():
        if encounter.get("kind") not in {"normal", "elite", "boss"}:
            raise ContentError(f"encounter {encounter['id']} has invalid kind")
        members = encounter.get("enemies")
        if not isinstance(members, list) or not 1 <= len(members) <= 4:
            raise ContentError(f"encounter {encounter['id']} must contain 1..4 enemies")
        for enemy_id in members:
            if enemy_id not in enemies:
                raise ContentError(f"encounter {encounter['id']} references {enemy_id}")

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

    return Catalog(raw, heroes, cards, enemies, encounters, events, afflictions, balance, art)
