"""Tavern lobby and deck editor inside Jomon's curses session."""

from __future__ import annotations

import curses

from .content import load_catalog
from .office_art import OFFICE_SPRITES
from .office_content import DOCTRINE_NAMES, INFUSION_NAMES, OFFICE_ROLES, office_catalog
from .tabletop import collection_for, patrons


def _put(screen: curses.window, y: int, x: int, value: str, attr: int = 0) -> None:
    height, width = screen.getmaxyx()
    if 0 <= y < height and x < width:
        try:
            screen.addnstr(y, max(0, x), value[max(0, -x):], max(0, width - max(0, x) - 1), attr)
        except curses.error:
            pass


def _draw_lobby(screen: curses.window, state, selected: int, slot: int, message: str) -> list:
    screen.erase()
    people = patrons(state)
    courier = state.courier
    collection = collection_for(state, courier.id)
    _put(screen, 0, 1, "DULLEST DUNGEON  /  THE COMPANY OF NECESSARY COPIES", curses.A_BOLD)
    _put(screen, 2, 2, "A generated office dungeon, two parties, and rival confidential files.")
    _put(screen, 3, 2, "You command four specialists; a tavern patron commands the other four.")
    _put(screen, 5, 2, f"Courier: {courier.name}  Strategy: {courier.strategy}/20 (+1 supply per 5, max +2)")
    _put(screen, 6, 2, f"Record: {collection['wins']}W {collection['losses']}L {collection['draws']}D   Cards: {len(collection['cards'])}/290")
    doctrine_ids = list(load_catalog().doctrines)
    _put(screen, 7, 2, f"Company policy: {DOCTRINE_NAMES[doctrine_ids.index(collection['doctrine'])]}")
    _put(screen, 8, 2, "YOUR FOUR OFFICE WORKERS", curses.A_BOLD)
    for index, role in enumerate(collection["roles"]):
        marker = ">" if slot == index else " "
        _put(screen, 9 + index, 2, f"{marker} {index + 1}. {OFFICE_ROLES[role]}"[:40])
    selected_role = collection["roles"][slot]
    definition = load_catalog().heroes[selected_role]
    _put(screen, 8, 45, f"JOB {list(OFFICE_ROLES).index(selected_role) + 1}/25", curses.A_BOLD)
    for offset, line in enumerate(OFFICE_SPRITES[selected_role]):
        _put(screen, 9 + offset, 53, line, curses.A_BOLD)
    _put(screen, 14, 45, OFFICE_ROLES[selected_role].upper()[:32])
    _put(screen, 15, 45, f"{definition['combat_role'].upper()}  HP {definition['max_hp']}")
    _put(screen, 16, 45, "PREFERRED RANKS " + ",".join(map(str, definition["preferred_ranks"])))
    _put(screen, 18, 45, "V: browse all twenty-five jobs")
    _put(screen, 19, 45, "F: thirteen formation plans")
    _put(screen, 20, 45, "L: alternate five-card kit")
    _put(screen, 14, 2, "TAVERN PATRONS — JOIN WHEN CHOSEN", curses.A_BOLD)
    start = max(0, min(selected - 3, len(people) - 7))
    for index, person in enumerate(people[start:start + 7]):
        _put(screen, 15 + index, 2, f"{'>' if selected == start + index else ' '} {person.name[:26]:26} {person.role[:22]}")
    if not people:
        _put(screen, 15, 2, "Nobody is available to play just now.")
    _put(screen, 22, 2, message or "Each match win: +1 Strategy. First win against each patron each season: +1 credit.")
    _put(screen, 21, 45, "B: company archive")
    _put(screen, 23, 2, "J/K patron 1-4 slot [/] job V roster F squad L kit B archive P/D Enter Q")
    screen.refresh()
    return people


def _draw_editor(screen: curses.window, collection: dict, index: int, message: str) -> list[str]:
    screen.erase()
    _, cards = office_catalog()
    available = [card_id for card_id, card in cards.items() if card.role in collection["roles"] and collection["cards"].get(card_id, 0)]
    available.sort(key=lambda card_id: (cards[card_id].role, cards[card_id].name))
    _put(screen, 0, 1, "THE STATIONERY CABINET  /  DECK EDITOR", curses.A_BOLD)
    _put(screen, 2, 2, f"Deck: {len(collection['deck'])}/20-30. Owned cards only; each copy may be filed once.")
    if available:
        index %= len(available)
        start = max(0, min(index - 7, len(available) - 15))
        for row, card_id in enumerate(available[start:start + 15], 4):
            card = cards[card_id]
            owned = collection["cards"][card_id]
            used = collection["deck"].count(card_id)
            _put(screen, row, 2, f"{'>' if start + row - 4 == index else ' '} {card.name[:27]:27} {card.cost}E  {used}/{owned}  {OFFICE_ROLES[card.role][:18]}")
        selected = cards[available[index]]
        _put(screen, 20, 2, selected.description[:76])
        source = load_catalog()
        mastery = collection["masteries"].get(selected.id, "none")
        infusion = collection["infusions"].get(selected.id)
        infusion_ids = list(source.infusions)
        infusion_name = INFUSION_NAMES[infusion_ids.index(infusion)] if infusion else "none"
        _put(screen, 21, 2, f"Mastery: {mastery}  /  Card treatment: {infusion_name}"[:76])
    _put(screen, 22, 2, message)
    _put(screen, 23, 2, "J/K browse  A add  X remove  M mastery  I treatment  Esc return")
    screen.refresh()
    return available
