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


def load_catalog(path: Path | None = None) -> Catalog:
    source = path or Path(str(files("dumbest_dungeon.data").joinpath("game.json")))
    try:
        raw = json.loads(source.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ContentError(f"cannot load content from {source}: {exc}") from exc

    if raw.get("schema_version") != 1:
        raise ContentError("content schema_version must be 1")
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
        for field in ("name", "max_hp", "rank", "starter_deck"):
            if field not in hero:
                raise ContentError(f"hero {hero['id']} is missing {field}")
        if hero["rank"] not in range(1, 5) or hero["max_hp"] <= 0:
            raise ContentError(f"hero {hero['id']} has invalid rank or max_hp")
        for card_id in hero["starter_deck"]:
            if card_id not in cards:
                raise ContentError(f"hero {hero['id']} references unknown card {card_id}")

    if sorted(hero["rank"] for hero in heroes.values()) != [1, 2, 3, 4]:
        raise ContentError("the four heroes must occupy unique ranks 1..4")
    if len(heroes) != 4:
        raise ContentError("this release requires exactly four heroes")

    for card in cards.values():
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

    for enemy in enemies.values():
        if not isinstance(enemy.get("max_hp"), int) or enemy["max_hp"] <= 0:
            raise ContentError(f"enemy {enemy['id']} has invalid max_hp")
        actions = enemy.get("actions")
        if not isinstance(actions, list) or not actions:
            raise ContentError(f"enemy {enemy['id']} needs actions")
        for action in actions:
            _effects(action.get("effects"), CARD_EFFECTS, f"enemy {enemy['id']} action")

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
            _effects(choice.get("effects"), EVENT_EFFECTS, f"event {event['id']} choice")

    required_balance = {
        "hand_size",
        "energy",
        "travel_light_cost",
        "death_chance",
        "low_light_threshold",
    }
    missing = required_balance - balance.keys()
    if missing:
        raise ContentError(f"balance is missing: {', '.join(sorted(missing))}")

    return Catalog(raw, heroes, cards, enemies, encounters, events, afflictions, balance)
