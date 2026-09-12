"""Private-card draw poker at the separate physical tavern table."""

from __future__ import annotations

import curses

from .state import GameState
from .tavern_draw import (
    MAX_EXPOSURE, RANK_NAMES, STARTING_NPC_CREDIT, available_opponents,
    bet_action, close_hand, draw_cards, drive_npcs, evaluate, start_hand,
)

RANKS = "23456789TJQKA"
SUITS = "SHDC"


def card_name(card: int) -> str:
    return RANKS[card % 13] + SUITS[card // 13]


def card_frame(card: int, *, selected: bool = False) -> tuple[str, ...]:
    """Adapt the framed card motif without depending on Dullest Dungeon code."""
    label = card_name(card)
    edge = "*-------*" if selected else "+-------+"
    return (edge, f"|{label:<7}|", "|       |", f"|   {label[1]}   |",
            "|       |", f"|{label:>7}|", edge)


def _put(screen: curses.window, row: int, col: int, value: str, attr: int = 0) -> None:
    height, width = screen.getmaxyx()
    if 0 <= row < height and 0 <= col < width:
        try:
            screen.addnstr(row, col, value, max(0, width - col - 1), attr)
        except curses.error:
            pass


def _draw_lobby(screen: curses.window, state: GameState, selected: list[str], cursor: int,
                wagering: bool, message: str) -> list:
    screen.erase()
    people = available_opponents(state)
    bankrolls = state.tavern_draw["bankrolls"]
    _put(screen, 0, 2, "TAVERN DRAW  /  FOUR SEATS, ONE DECK", curses.A_BOLD)
    _put(screen, 2, 2, "Five-card draw: deal, bet, exchange, bet, showdown.")
    _put(screen, 3, 2, "Free practice pays nothing. Wagered: 1-credit ante, 1-credit bet,")
    _put(screen, 4, 2, f"one raise per round; maximum loss {MAX_EXPOSURE} credit per seat.")
    _put(screen, 6, 2, f"Your credit: {state.trade_credit}   Mode: {'WAGERED' if wagering else 'FREE PRACTICE'}")
    _put(screen, 7, 2, f"Invite three adults actually in the tavern ({len(selected)}/3):")
    first = max(0, cursor - 10)
    for index in range(first, min(first + 11, len(people))):
        person = people[index]
        mark = "[x]" if person.id in selected else "[ ]"
        credit = bankrolls.get(person.id, STARTING_NPC_CREDIT)
        _put(screen, 8 + index - first, 2, f"{'>' if index == cursor else ' '} {mark} {person.name[:24]:24} {person.role[:19]:19} {credit:>3} credit",
             curses.A_REVERSE if index == cursor else 0)
    if len(people) > 11:
        _put(screen, 19, 2, f"Showing {first + 1}-{min(first + 11, len(people))} of {len(people)} tavern adults.")
    _put(screen, 20, 2, message[:75] if message else "Only named adult NPCs play. Their cards stay hidden until showdown.")
    _put(screen, 22, 2, "J/K choose  Space invite  W stakes/free  Enter deal  Q leave")
    screen.refresh()
    return people


def _draw_hand(screen: curses.window, state: GameState, marked: set[int], message: str) -> None:
    hand = state.tavern_draw["active_hand"]
    assert hand is not None
    screen.erase()
    phase = {"bet1": "FIRST BET", "draw": "EXCHANGE", "bet2": "FINAL BET", "complete": "SHOWDOWN"}[hand["phase"]]
    _put(screen, 0, 2, f"TAVERN DRAW  /  HAND {hand['number']}  /  {phase}", curses.A_BOLD)
    _put(screen, 1, 2, f"Pot {hand['pot'] if hand['phase'] != 'complete' else hand['final_pot']} credit  |  Your credit {state.trade_credit}  |  {'WAGERED' if hand['wagering'] else 'FREE'}")
    _put(screen, 2, 2, f"Dealer: {hand['names'][hand['dealer']]}   Acting: {hand['names'][hand['turn']] if hand['turn'] is not None else 'none'}")
    for seat in range(1, 4):
        column = 2 + (seat - 1) * 26
        name = hand["names"][seat][:23]
        balance = state.tavern_draw["bankrolls"][hand["players"][seat]]
        _put(screen, 4, column, f"{name} ({balance})", curses.A_BOLD)
        if not hand["active"][seat]:
            cards = "FOLDED"
        elif hand["phase"] == "complete":
            cards = " ".join(card_name(card) for card in hand["hands"][seat])
        else:
            cards = "[##] " * 5
        _put(screen, 5, column, cards[:24])
        _put(screen, 6, column, f"Paid {hand['committed'][seat]}  {'IN' if hand['active'][seat] else 'OUT'}")
    _put(screen, 8, 2, f"{hand['names'][0]}  /  {RANK_NAMES[evaluate(hand['hands'][0])[0]]}", curses.A_BOLD)
    for index, card in enumerate(hand["hands"][0]):
        column = 13 + index * 11
        for offset, line in enumerate(card_frame(card, selected=index in marked)):
            _put(screen, 10 + offset, column, line,
                 curses.A_REVERSE if index in marked else curses.A_BOLD)
        if hand["phase"] == "draw":
            _put(screen, 17, column + 3, str(index + 1))
    if hand["phase"] == "complete":
        winners = ", ".join(hand["names"][seat] for seat in hand["winners"])
        _put(screen, 18, 2, f"Winner: {winners}; paid {hand['payouts']}.")
        _put(screen, 21, 2, "Enter clear settled hand  Q return to tavern")
    elif hand["phase"] == "draw":
        _put(screen, 18, 2, f"Selected to exchange: {len(marked)} / 5")
        _put(screen, 21, 2, "1-5 mark cards  Enter exchange (or stand pat)  Q pause hand")
    else:
        due = hand["current_bet"] - hand["round_paid"][0]
        _put(screen, 18, 2, f"To call: {due} credit; raised {hand['raises']}/1 this round.")
        _put(screen, 21, 2, "C/Enter check or call  R raise  F fold  Q pause hand")
    _put(screen, 19, 2, (message or hand["log"][-1])[:75])
    _put(screen, 22, 2, f"Capped stake: {MAX_EXPOSURE} credit per seat; no automatic replay or real money.")
    screen.refresh()


def _resume_other_courier(state: GameState) -> None:
    hand = state.tavern_draw["active_hand"]
    if not hand or hand["phase"] == "complete" or hand["players"][0] == state.active_courier_id:
        return
    for _ in range(4):
        drive_npcs(state)
        if hand["phase"] == "complete":
            return
        if hand["phase"] == "draw":
            draw_cards(state, [])
        else:
            bet_action(state, "fold")
    drive_npcs(state)


def run_tavern_draw(screen: curses.window, state: GameState) -> None:
    selected: list[str] = []
    cursor = 0
    wagering = False
    marked: set[int] = set()
    message = ""
    _resume_other_courier(state)
    while True:
        hand = state.tavern_draw["active_hand"]
        if hand is None:
            cursor = min(cursor, max(0, len(available_opponents(state)) - 1))
            people = _draw_lobby(screen, state, selected, cursor, wagering, message)
            key = screen.getch()
            normalized = ord(chr(key).lower()) if 0 <= key < 256 else key
            if normalized in (ord("q"), 27):
                return
            if normalized in (ord("j"), curses.KEY_DOWN) and people:
                cursor = (cursor + 1) % len(people)
            elif normalized in (ord("k"), curses.KEY_UP) and people:
                cursor = (cursor - 1) % len(people)
            elif normalized == ord(" ") and people:
                identity = people[cursor].id
                if identity in selected:
                    selected.remove(identity)
                elif len(selected) < 3:
                    selected.append(identity)
                else:
                    message = "Three seats are filled; deselect someone before inviting another."
            elif normalized == ord("w"):
                wagering = not wagering
            elif normalized in (10, 13, curses.KEY_ENTER):
                try:
                    start_hand(state, selected, wagering=wagering)
                    marked.clear()
                    message = ""
                except ValueError as exc:
                    message = str(exc)
            continue
        if hand["phase"] != "complete" and (hand["turn"] != 0 or not hand["active"][0]):
            drive_npcs(state, on_action=lambda current: (_draw_hand(screen, state, marked, ""), curses.napms(160)))
            continue
        _draw_hand(screen, state, marked, message)
        key = screen.getch()
        normalized = ord(chr(key).lower()) if 0 <= key < 256 else key
        message = ""
        if normalized in (ord("q"), 27):
            return
        if hand["phase"] == "complete":
            if normalized in (10, 13, curses.KEY_ENTER):
                close_hand(state)
                selected = []
                marked.clear()
            continue
        if hand["phase"] == "draw":
            if ord("1") <= normalized <= ord("5"):
                index = normalized - ord("1")
                if index in marked:
                    marked.remove(index)
                else:
                    marked.add(index)
            elif normalized in (10, 13, curses.KEY_ENTER):
                try:
                    draw_cards(state, sorted(marked))
                    marked.clear()
                except ValueError as exc:
                    message = str(exc)
            continue
        action = ("fold" if normalized == ord("f") else "raise" if normalized == ord("r") else
                  "call" if normalized in (ord("c"), 10, 13, curses.KEY_ENTER) else None)
        if action:
            try:
                bet_action(state, action)
            except ValueError as exc:
                message = str(exc)
