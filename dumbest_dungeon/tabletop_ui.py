"""Curses table view embedded in Jomon's existing terminal session."""

from __future__ import annotations

import curses
import textwrap

from .content import load_catalog
from .office_content import DOCTRINE_NAMES, INFUSION_NAMES, OFFICE_ROLES, office_catalog
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
    doctrine_ids = list(load_catalog().doctrines)
    _put(screen, 7, 2, f"Company policy: {DOCTRINE_NAMES[doctrine_ids.index(collection['doctrine'])]}")
    _put(screen, 8, 2, "YOUR FOUR OFFICE WORKERS", curses.A_BOLD)
    for index, role in enumerate(collection["roles"]):
        marker = ">" if slot == index else " "
        _put(screen, 9 + index, 2, f"{marker} {index + 1}. {OFFICE_ROLES[role]}"[:40])
    _put(screen, 14, 2, "PATRONS AT THE TABLE", curses.A_BOLD)
    start = max(0, min(selected - 3, len(people) - 7))
    for index, person in enumerate(people[start:start + 7]):
        _put(screen, 15 + index, 2, f"{'>' if selected == start + index else ' '} {person.name[:26]:26} {person.role[:22]}")
    if not people:
        _put(screen, 15, 2, "Nobody is available to play just now.")
    _put(screen, 22, 2, message or "First win against each patron per season: +1 credit and strategy.")
    _put(screen, 23, 2, "J/K patron  1-4 worker  [/] job  P policy  D deck  Enter play  Q leave")
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


def _draw_match(screen: curses.window, match: dict, selected_worker: int,
                selected_card: int, cursor: tuple[int, int], message: str) -> None:
    screen.erase()
    _, cards = office_catalog()
    _put(screen, 0, 1, f"DULLEST DUNGEON  /  {match['department'].upper()}", curses.A_BOLD)
    round_label = f"OT {match['round'] - 18}/4" if match["round"] > 18 else f"Round {match['round']}/18"
    _put(screen, 1, 1, f"{round_label}  vs {match.get('patron_name', 'the patron')[:18]}  Energy {match['energy']}/3  Approval {match['scores'][0]}:{match['scores'][1]}")
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
    _put(screen, cy + 2, cx + 1, board[cy][cx], curses.A_REVERSE | curses.A_BOLD)
    _put(screen, 2, 44, "YOUR WORKERS", curses.A_BOLD)
    for index, piece in enumerate(match["pieces"][:4]):
        suffix = " FILE" if match["files"][1]["carrier"] == piece["id"] else ""
        _put(screen, 3 + index, 44, f"{'>' if selected_worker == index else ' '}{index + 1} {OFFICE_ROLES[piece['role']][:19]} {piece['hp']:2}/{piece['max_hp']}{suffix}")
    _put(screen, 8, 44, "PATRON WORKERS", curses.A_BOLD)
    for index, piece in enumerate(match["pieces"][4:]):
        suffix = " FILE" if match["files"][0]["carrier"] == piece["id"] else ""
        _put(screen, 9 + index, 44, f" {chr(ord('a') + index)} {OFFICE_ROLES[piece['role']][:19]} {piece['hp']:2}/{piece['max_hp']}{suffix}")
    status_line = ", ".join(f"{key}:{value}" for key, value in match["pieces"][selected_worker]["statuses"].items())
    _put(screen, 13, 44, f"Status: {status_line or 'clear'}"[:35])
    _put(screen, 14, 44, "F/f files; c coffee; s staples")
    _put(screen, 15, 44, "Steal, return, hold one rival turn.")
    _put(screen, 16, 44, "First to 2; 18 rounds + overtime.")
    _put(screen, 18, 1, "HAND  (0 always Commute, 1 energy / 1 desk)", curses.A_BOLD)
    hand = match["sides"][0]["hand"]
    for index, card_id in enumerate(hand[:8]):
        card = cards[card_id]
        column = 1 if index < 4 else 41
        row = 19 + (index % 4)
        _put(screen, row, column, f"{'>' if selected_card == index else ' '}{index + 1} {OFFICE_ROLES[card.role][:3]} {card.name[:20]:20} {card.cost}E")
    selected = cards[hand[selected_card]] if 0 <= selected_card < len(hand) else None
    _put(screen, 17, 44, (selected.description if selected else "Commute one desk.")[:35])
    _put(screen, 17, 1, (message or match["log"][-1])[:40])
    _put(screen, 23, 1, "Arrows cursor  Tab worker  1-8/0 card  Enter play  E end  ? rules  S save  Q exit")
    screen.refresh()


def _draw_result(screen: curses.window, result: str, match: dict) -> None:
    screen.erase()
    _put(screen, 4, 6, "THE COMPANY HAS REACHED A DECISION", curses.A_BOLD)
    _put(screen, 7, 6, f"{result.upper()}  /  Approval {match['scores'][0]}:{match['scores'][1]}")
    _put(screen, 9, 6, f"Department: {match['department']}")
    _put(screen, 11, 6, "The board is folded. Someone insists this counts as work.")
    _put(screen, 23, 2, "Press any key to return to Jomon.")
    screen.refresh()


def _draw_inspect(screen: curses.window, match: dict, card_index: int) -> None:
    screen.erase()
    _put(screen, 0, 1, "DULLEST DUNGEON  /  CARD AND RULES", curses.A_BOLD)
    hand = match["sides"][0]["hand"]
    if 0 <= card_index < len(hand):
        card = office_catalog()[1][hand[card_index]]
        _put(screen, 2, 2, f"{card.name}  /  {OFFICE_ROLES[card.role]}  /  {card.cost} energy", curses.A_BOLD)
        description = card.description
        mastery = match["sides"][0]["masteries"].get(card.id, "none")
        infusion = match["sides"][0]["infusions"].get(card.id)
        infusion_ids = list(load_catalog().infusions)
        treatment = INFUSION_NAMES[infusion_ids.index(infusion)] if infusion else "none"
        _put(screen, 6, 2, f"Mastery: {mastery}; card treatment: {treatment}")
    else:
        _put(screen, 2, 2, "Commute  /  any worker  /  1 energy", curses.A_BOLD)
        description = "Move the chosen worker exactly one clear desk toward the cursor. This fallback card is always available and is never drawn or discarded."
    for index, line in enumerate(textwrap.wrap(description, 72)):
        _put(screen, 4 + index, 2, line)
    _put(screen, 9, 2, "The card's job title must match the chosen worker.")
    _put(screen, 10, 2, "Move effects follow a clear route toward the cursor, up to their printed range.")
    _put(screen, 11, 2, "Other targets use the worker under the cursor, or the nearest legal rival.")
    _put(screen, 13, 2, "F/f are the two files. Steal the rival file and bring it near your own tray.")
    _put(screen, 14, 2, "Your file must be home. Hold through the patron's next turn to score.")
    _put(screen, 15, 2, "A knocked-out worker drops the file and returns after two turns.")
    _put(screen, 16, 2, "c coffee adds one energy; s staples adds cover and a card draw.")
    _put(screen, 18, 2, "First to two approvals wins; otherwise compare scores after eighteen rounds.")
    _put(screen, 19, 2, "A tie receives four overtime rounds and may end in a draw.")
    _put(screen, 23, 2, "Press any key to return to the board.")
    screen.refresh()


def run_tabletop(screen: curses.window, state) -> None:
    """Return to the ordinary Jomon loop without changing its terminal setup."""
    if state.courier is None or not state.courier.alive:
        state.add_message("Choose a living courier before opening the game box.")
        return
    active = state.tabletop["active_match"]
    if active and active["courier_id"] != state.courier.id:
        owner = next((person for person in state.household if person.id == active["courier_id"]), None)
        if owner and not owner.alive:
            active["winner"] = 1
            finish_match(state)
            state.add_message(f"{owner.name}'s unfinished table game is archived as a loss.")
        else:
            state.add_message(f"{owner.name if owner else 'Another courier'} must finish the open table game.")
            return
    selected_patron, slot, deck_index = 0, 0, 0
    worker, card_index = 0, -1
    cursor = (5, 5)
    mode = "match" if state.tabletop["active_match"] else "lobby"
    message = ""
    inspecting = False
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
            if inspecting:
                _draw_inspect(screen, match, card_index)
            else:
                _draw_match(screen, match, worker, card_index, cursor, message)
        key = screen.getch()
        normalized = ord(chr(key).lower()) if 0 <= key < 256 else key
        if inspecting:
            inspecting = False
            continue
        message = ""
        if normalized == curses.KEY_MOUSE:
            try:
                _, x, y, _, button = curses.getmouse()
                double_mask = getattr(curses, "BUTTON1_DOUBLE_CLICKED", 0)
                left = bool(button & (getattr(curses, "BUTTON1_CLICKED", 0) | getattr(curses, "BUTTON1_PRESSED", 0) | double_mask))
                double = bool(button & double_mask)
                if not left:
                    continue
                if mode == "lobby" and 15 <= y <= 21:
                    start = max(0, min(selected_patron - 3, len(people) - 7))
                    if start + y - 15 < len(people):
                        selected_patron = start + y - 15
                        normalized = 13 if double else -1
                elif mode == "lobby" and 9 <= y <= 12:
                    slot = y - 9
                    normalized = -1
                elif mode == "deck" and 4 <= y <= 18:
                    start = max(0, min(deck_index - 7, len(available) - 15))
                    if start + y - 4 < len(available):
                        deck_index = start + y - 4
                        normalized = ord("a") if double else -1
                elif mode == "match" and 1 <= x <= WIDTH and 2 <= y < HEIGHT + 2:
                    cursor = x - 1, y - 2
                    normalized = 13 if double else -1
                elif mode == "match" and 44 <= x < 79 and 3 <= y <= 6:
                    worker = y - 3
                    piece = match["pieces"][worker]
                    cursor = piece["x"], piece["y"]
                    normalized = -1
                elif mode == "match" and 19 <= y <= 22:
                    candidate = y - 19 + (4 if x >= 41 else 0)
                    if candidate < len(match["sides"][0]["hand"]):
                        card_index = candidate
                    normalized = -1
                else:
                    normalized = -1
            except curses.error:
                normalized = -1
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
            elif normalized == ord("p"):
                collection = collection_for(state, state.courier.id)
                doctrines = list(load_catalog().doctrines)
                collection["doctrine"] = doctrines[(doctrines.index(collection["doctrine"]) + 1) % len(doctrines)]
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
            elif normalized == ord("m") and available:
                card_id = available[deck_index % len(available)]
                if not any(item["card_id"] == card_id for item in load_catalog().masteries.values()):
                    message = "This card has no mastery branches."
                else:
                    current = collection["masteries"].get(card_id)
                    if current == "coverage":
                        collection["masteries"].pop(card_id)
                    else:
                        collection["masteries"][card_id] = "engine" if current is None else "coverage"
            elif normalized == ord("i") and available:
                card_id = available[deck_index % len(available)]
                infusions = list(load_catalog().infusions)
                current = collection["infusions"].get(card_id)
                if current == infusions[-1]:
                    collection["infusions"].pop(card_id)
                else:
                    collection["infusions"][card_id] = infusions[0] if current is None else infusions[infusions.index(current) + 1]
        else:
            match = state.tabletop["active_match"]
            if normalized in (ord("q"), 27):
                return
            if normalized == ord("?"):
                inspecting = True
                continue
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
