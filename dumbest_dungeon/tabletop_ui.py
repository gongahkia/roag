"""Curses table view embedded in Jomon's existing terminal session."""

from __future__ import annotations

import curses

from .office_content import OFFICE_ROLES, office_catalog
from .tabletop import (
    FALLBACK, HEIGHT, WIDTH, collection_for, end_turn, finish_match,
    patrons, patron_turn, play_card, start_match,
)


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
    _put(screen, 2, 2, "A painted board of desks, files and absurdly serious office workers.")
    _put(screen, 3, 2, "Jomon's people wager bragging rights over a world with no river at all.")
    _put(screen, 5, 2, f"Courier: {courier.name}  Strategy: {courier.strategy}/20 (record only)")
    _put(screen, 6, 2, f"Record: {collection['wins']}W {collection['losses']}L {collection['draws']}D   Cards: {len(collection['cards'])}/290")
    _put(screen, 8, 2, "YOUR FOUR OFFICE WORKERS", curses.A_BOLD)
    for index, role in enumerate(collection["roles"]):
        marker = ">" if slot == index else " "
        _put(screen, 9 + index, 2, f"{marker} {index + 1}. {OFFICE_ROLES[role]}"[:40])
    _put(screen, 14, 2, "PATRONS AT THE TABLE", curses.A_BOLD)
    for index, person in enumerate(people[:6]):
        _put(screen, 15 + index, 2, f"{'>' if selected == index else ' '} {person.name[:26]:26} {person.role[:22]}")
    if not people:
        _put(screen, 15, 2, "Nobody is available to play just now.")
    _put(screen, 22, 2, message or "First win against each patron per season: +1 credit and strategy.")
    _put(screen, 23, 2, "J/K patron  1-4 worker  [/] change job  D deck  Enter play  Q leave")
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
    _put(screen, 22, 2, message)
    _put(screen, 23, 2, "J/K browse  A add copy  X remove copy  Esc return")
    screen.refresh()
    return available


def _draw_match(screen: curses.window, match: dict, selected_worker: int,
                selected_card: int, cursor: tuple[int, int], message: str) -> None:
    screen.erase()
    _, cards = office_catalog()
    _put(screen, 0, 1, f"DULLEST DUNGEON  /  {match['department'].upper()}", curses.A_BOLD)
    _put(screen, 1, 1, f"Round {match['round']}/{18}  Your turn  Energy {match['energy']}/3  Approval {match['scores'][0]}:{match['scores'][1]}")
    board = [list(row) for row in match["board"]]
    for pickup in match["pickups"]:
        board[pickup["y"]][pickup["x"]] = "c" if pickup["kind"] == "coffee" else "s"
    for side, file in enumerate(match["files"]):
        if file["carrier"]:
            continue
        x, y = file["dropped"] or file["home"]
        board[y][x] = "F" if side == 0 else "f"
    for piece in match["pieces"]:
        if piece["hp"] > 0:
            board[piece["y"]][piece["x"]] = str(int(piece["id"].split(":")[1]) + 1) if piece["side"] == 0 else chr(ord("a") + int(piece["id"].split(":")[1]))
    cx, cy = cursor
    board[cy][cx] = "@" if board[cy][cx] == "." else board[cy][cx]
    for row, line in enumerate(board):
        _put(screen, row + 2, 1, "".join(line))
    _put(screen, 2, 44, "YOUR WORKERS", curses.A_BOLD)
    for index, piece in enumerate(match["pieces"][:4]):
        suffix = " FILE" if match["files"][1]["carrier"] == piece["id"] else ""
        _put(screen, 3 + index, 44, f"{'>' if selected_worker == index else ' '}{index + 1} {OFFICE_ROLES[piece['role']][:19]} {piece['hp']:2}/{piece['max_hp']}{suffix}")
    _put(screen, 8, 44, "PATRON WORKERS", curses.A_BOLD)
    for index, piece in enumerate(match["pieces"][4:]):
        suffix = " FILE" if match["files"][0]["carrier"] == piece["id"] else ""
        _put(screen, 9 + index, 44, f" {chr(ord('a') + index)} {OFFICE_ROLES[piece['role']][:19]} {piece['hp']:2}/{piece['max_hp']}{suffix}")
    _put(screen, 14, 44, "F/f files; c coffee; s staples")
    _put(screen, 15, 44, "Steal, return, hold one rival turn.")
    _put(screen, 16, 44, "First to 2; 18 rounds + overtime.")
    _put(screen, 18, 1, "HAND  (0 always Commute, 1 energy / 1 desk)", curses.A_BOLD)
    hand = match["sides"][0]["hand"]
    for index, card_id in enumerate(hand[:8]):
        card = cards[card_id]
        column = 1 if index < 4 else 41
        row = 19 + (index % 4)
        _put(screen, row, column, f"{'>' if selected_card == index else ' '}{index + 1} {card.name[:24]:24} {card.cost}E")
    selected = cards[hand[selected_card]] if 0 <= selected_card < len(hand) else None
    _put(screen, 17, 44, (selected.description if selected else "Commute one desk.")[:35])
    _put(screen, 22, 1, (message or match["log"][-1])[:76])
    _put(screen, 23, 1, "Arrows cursor  Tab worker  1-8 card/0 commute  Enter file  E end  S save  Q leave")
    screen.refresh()


def _draw_result(screen: curses.window, result: str, match: dict) -> None:
    screen.erase()
    _put(screen, 4, 6, "THE COMPANY HAS REACHED A DECISION", curses.A_BOLD)
    _put(screen, 7, 6, f"{result.upper()}  /  Approval {match['scores'][0]}:{match['scores'][1]}")
    _put(screen, 9, 6, f"Department: {match['department']}")
    _put(screen, 11, 6, "The board is folded. Someone insists this counts as work.")
    _put(screen, 23, 2, "Press any key to return to Jomon.")
    screen.refresh()


def run_tabletop(screen: curses.window, state) -> None:
    """Return to the ordinary Jomon loop without changing its terminal setup."""
    if state.courier is None:
        state.add_message("Choose a courier before opening the game box.")
        return
    selected_patron, slot, deck_index = 0, 0, 0
    worker, card_index = 0, -1
    cursor = (5, 5)
    mode = "match" if state.tabletop["active_match"] else "lobby"
    message = ""
    while True:
        height, width = screen.getmaxyx()
        if height < 24 or width < 80:
            screen.erase()
            _put(screen, 1, 1, "Dullest Dungeon needs an 80x24 terminal. Resize or press Q to leave.")
            screen.refresh()
            if screen.getch() in (ord("q"), ord("Q"), 27):
                return
            continue
        if mode == "lobby":
            people = _draw_lobby(screen, state, selected_patron, slot, message)
        elif mode == "deck":
            collection = collection_for(state, state.courier.id)
            available = _draw_editor(screen, collection, deck_index, message)
        else:
            match = state.tabletop["active_match"]
            if match["winner"] is not None:
                result = finish_match(state)
                _draw_result(screen, result, match)
                screen.getch()
                return
            _draw_match(screen, match, worker, card_index, cursor, message)
        key = screen.getch()
        normalized = ord(chr(key).lower()) if 0 <= key < 256 else key
        message = ""
        if normalized == curses.KEY_MOUSE and mode == "match":
            try:
                _, x, y, _, button = curses.getmouse()
                if button & curses.BUTTON1_CLICKED and 1 <= x <= WIDTH and 2 <= y < HEIGHT + 2:
                    cursor = x - 1, y - 2
            except curses.error:
                pass
            continue
        if mode == "lobby":
            if normalized in (ord("q"), 27):
                return
            if normalized in (ord("j"), curses.KEY_DOWN) and people:
                selected_patron = (selected_patron + 1) % len(people)
            elif normalized in (ord("k"), curses.KEY_UP) and people:
                selected_patron = (selected_patron - 1) % len(people)
            elif ord("1") <= normalized <= ord("4"):
                slot = normalized - ord("1")
            elif normalized in (ord("["), ord("]")):
                collection = collection_for(state, state.courier.id)
                roles = list(office_catalog()[0])
                current = roles.index(collection["roles"][slot])
                step = 1 if normalized == ord("]") else -1
                collection["roles"][slot] = next(roles[(current + step * offset) % len(roles)] for offset in range(1, len(roles)) if roles[(current + step * offset) % len(roles)] not in collection["roles"])
                collection["deck"] = [card for role in collection["roles"] for card in office_catalog()[0][role]["starter_deck"]]
            elif normalized == ord("d"):
                mode = "deck"
            elif normalized in (10, 13) and people:
                try:
                    match = start_match(state, people[selected_patron % len(people)].id)
                    cursor = (match["pieces"][0]["x"], match["pieces"][0]["y"])
                    mode = "match"
                except ValueError as exc:
                    message = str(exc)
        elif mode == "deck":
            if normalized in (27, ord("q")):
                mode = "lobby"
            elif normalized in (ord("j"), curses.KEY_DOWN) and available:
                deck_index = (deck_index + 1) % len(available)
            elif normalized in (ord("k"), curses.KEY_UP) and available:
                deck_index = (deck_index - 1) % len(available)
            elif normalized == ord("a") and available:
                card_id = available[deck_index % len(available)]
                if len(collection["deck"]) >= 30 or collection["deck"].count(card_id) >= collection["cards"][card_id]:
                    message = "No room or no unused copy."
                else:
                    collection["deck"].append(card_id)
            elif normalized == ord("x") and available:
                card_id = available[deck_index % len(available)]
                if len(collection["deck"]) <= 20 or card_id not in collection["deck"]:
                    message = "Keep at least twenty cards and select one in the deck."
                else:
                    collection["deck"].remove(card_id)
        else:
            match = state.tabletop["active_match"]
            if normalized in (ord("q"), 27):
                return
            if normalized == ord("s"):
                from jomon.save import SaveError, save_game

                try:
                    message = f"Saved to {save_game(state)}."
                except SaveError as exc:
                    message = str(exc)
            elif normalized in (ord("e"), ord(" ")):
                end_turn(match)
                if match["winner"] is None and match["turn"] == 1:
                    patron_turn(match)
                card_index = -1
            elif normalized in (9,):
                worker = (worker + 1) % 4
                piece = match["pieces"][worker]
                cursor = piece["x"], piece["y"]
            elif normalized in (curses.KEY_LEFT, ord("a")):
                cursor = max(1, cursor[0] - 1), cursor[1]
            elif normalized in (curses.KEY_RIGHT, ord("d")):
                cursor = min(WIDTH - 2, cursor[0] + 1), cursor[1]
            elif normalized in (curses.KEY_UP, ord("w")):
                cursor = cursor[0], max(1, cursor[1] - 1)
            elif normalized in (curses.KEY_DOWN, ord("x")):
                cursor = cursor[0], min(HEIGHT - 2, cursor[1] + 1)
            elif normalized == ord("0"):
                card_index = -1
            elif ord("1") <= normalized <= ord("8"):
                card_index = normalized - ord("1")
            elif normalized in (10, 13):
                actor = match["pieces"][worker]
                target = next((piece for piece in match["pieces"] if piece["hp"] > 0 and (piece["x"], piece["y"]) == cursor), None)
                try:
                    name = play_card(match, card_index, actor["id"], target["id"] if target else None, cursor)
                    message = f"Filed {name}."
                except (ValueError, IndexError) as exc:
                    message = str(exc)
