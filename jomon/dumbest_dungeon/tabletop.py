"""Tavern participants and persistent card collections for Dullest Dungeon."""

from __future__ import annotations

import hashlib
from typing import Any

from .content import load_catalog
from .office_content import DEPARTMENTS, office_catalog


def shifted_doctrine(seed: str, patron_id: str) -> int:
    count = len(load_catalog().doctrines)
    return int.from_bytes(hashlib.sha256(f"{seed}:{patron_id}:doctrine".encode()).digest()[:2], "big") % count


def initial_collection() -> dict:
    roles, cards_by_id = office_catalog()
    cards = {card_id: 1 for card_id in cards_by_id}
    for role in roles.values():
        for card_id in role["starter_deck"]:
            cards[card_id] = max(cards.get(card_id, 0), role["starter_deck"].count(card_id))
    selected = list(roles)[:4]
    deck = [card for role in selected for card in roles[role]["starter_deck"]]
    return {"roles": selected, "cards": cards, "deck": deck,
            "masteries": {}, "infusions": {}, "doctrine": "base:mark_window",
            "wins": 0, "losses": 0, "draws": 0}


def collection_for(state: Any, courier_id: str) -> dict:
    collections = state.tabletop["collections"]
    if courier_id not in collections:
        collections[courier_id] = initial_collection()
    return collections[courier_id]


def patrons(state: Any) -> list[Any]:
    """Adults currently in the tavern may play; no anonymous or remote opponent."""
    people = [*state.household, *state.visitors, state.bartender]
    return [person for person in people if person.id != state.active_courier_id and person.alive and person.available
            and person.id in state.actor_schedules and state.actor_schedules[person.id].area == "tavern"]


def validate_tabletop(state: Any) -> None:
    data = state.tabletop
    if not isinstance(data, dict) or set(data) != {"collections", "records", "active_match"}:
        raise ValueError("invalid Dullest Dungeon ledger")
    if not isinstance(data["collections"], dict) or not isinstance(data["records"], list):
        raise ValueError("invalid Dullest Dungeon collection or record")
    household_ids = {person.id for person in state.household}
    roles, cards = office_catalog()
    source = load_catalog()
    for courier_id, collection in data["collections"].items():
        if courier_id not in household_ids:
            raise ValueError("collection has no household owner")
        if len(collection["roles"]) != 4 or len(set(collection["roles"])) != 4 or any(role not in roles for role in collection["roles"]):
            raise ValueError("invalid worker roster")
        if any(card not in cards or type(count) is not int or count < 0 for card, count in collection["cards"].items()):
            raise ValueError("invalid card collection")
        if not 20 <= len(collection["deck"]) <= 30 or any(collection["deck"].count(card) > collection["cards"].get(card, 0) or cards[card].role not in collection["roles"] for card in collection["deck"]):
            raise ValueError("invalid office deck")
        if collection["doctrine"] not in source.doctrines or any(card not in cards or branch not in {"engine", "coverage"} or not any(item["card_id"] == card for item in source.masteries.values()) for card, branch in collection["masteries"].items()):
            raise ValueError("invalid office mastery or doctrine")
        if any(card not in cards or infusion not in source.infusions for card, infusion in collection["infusions"].items()):
            raise ValueError("invalid office infusion")
        if any(type(collection[key]) is not int or collection[key] < 0 for key in ("wins", "losses", "draws")):
            raise ValueError("invalid office record totals")
    for record in data["records"]:
        if (not isinstance(record, dict) or set(record) != {"courier", "patron", "season", "result", "score", "department"}
                or record["courier"] not in household_ids or not isinstance(record["patron"], str)
                or not record["patron"] or not isinstance(record["season"], str)
                or record["result"] not in {"win", "loss", "draw"} or record["department"] not in DEPARTMENTS
                or not isinstance(record["score"], list) or len(record["score"]) != 2
                or any(type(score) is not int or not 0 <= score <= 2 for score in record["score"])):
            raise ValueError("invalid office match record")
