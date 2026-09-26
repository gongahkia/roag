"""Private-card draw poker at the separate physical tavern table."""

from __future__ import annotations

import curses

from .character_presentation import role_display_name
from .state import GameState
from .tavern_draw import (
    MAX_EXPOSURE, available_opponents,
    bet_action, close_hand, draw_cards, drive_npcs, evaluate, start_hand,
)
from .tavern_games import npc_credit
from .tavern_games_ui import accent, border, put as _put
from .ui_presentation import ui_text
from .tavern_presentation import draw_phase_name, draw_rank_name, tavern_cards, tavern_format, tavern_text


def card_name(card: int) -> str:
    cards = tavern_cards()
    return cards["ranks"][card % 13] + cards["suits"][card // 13]


def card_frame(card: int, *, selected: bool = False) -> tuple[str, ...]:
    """Adapt the framed card motif without depending on Dullest Dungeon code."""
    label = card_name(card)
    cards = tavern_cards()
    edge = cards["selected_edge"] if selected else cards["edge"]
    return tuple(row.format(edge=edge, label_left=label, suit=label[1], label_right=label)
                 for row in cards["frame"])


def _draw_lobby(screen: curses.window, state: GameState, selected: list[str], cursor: int,
                wagering: bool, message: str) -> list:
    screen.erase()
    border(screen, ui_text("ui.tavern.draw.title"))
    people = available_opponents(state)
    _put(screen, 1, 2, ui_text("ui.tavern.draw.rules"), accent("ui_heading", curses.A_BOLD))
    _put(screen, 2, 2, tavern_text("draw.ui.rules"))
    _put(screen, 3, 2, tavern_format("draw.ui.practice", maximum=MAX_EXPOSURE))
    _put(screen, 6, 2, tavern_format("draw.ui.credit", credit=state.trade_credit, stakes=tavern_text("draw.ui.wagered") if wagering else tavern_text("draw.ui.free_practice")),
         accent("warning" if wagering else "success", curses.A_BOLD))
    _put(screen, 7, 2, tavern_format("draw.ui.invite", selected=len(selected)))
    first = max(0, cursor - 10)
    for index in range(first, min(first + 11, len(people))):
        person = people[index]
        mark = "[x]" if person.id in selected else "[ ]"
        credit = npc_credit(state, person.id)
        _put(screen, 8 + index - first, 2, f"{'>' if index == cursor else ' '} {mark} {person.name[:24]:24} {role_display_name(person.role)[:19]:19} {credit:>3} credit",
             accent("ui_accent", curses.A_REVERSE) if index == cursor else 0)
    if len(people) > 11:
        _put(screen, 19, 2, f"Showing {first + 1}-{min(first + 11, len(people))} of {len(people)} tavern adults.")
    _put(screen, 20, 2, message[:75] if message else tavern_text("draw.ui.empty"),
         accent("warning") if message else 0)
    _put(screen, 22, 2, ui_text("ui.tavern.draw.controls"), accent("ui_accent", curses.A_BOLD))
    screen.refresh()
    return people


def _draw_hand(screen: curses.window, state: GameState, marked: set[int], message: str,
               reveal_count: int = 3) -> None:
    hand = state.tavern_draw["active_hand"]
    assert hand is not None
    screen.erase()
    border(screen, tavern_text("draw.ui.title.complete") if hand["phase"] == "complete" else tavern_text("draw.ui.title.active"),
           "success" if hand["phase"] == "complete" and 0 in hand["winners"] else "ui_frame")
    phase = draw_phase_name(hand["phase"])
    _put(screen, 1, 2, tavern_format("draw.ui.hand", number=hand["number"], phase=phase), accent("ui_heading", curses.A_BOLD))
    _put(screen, 2, 2, tavern_format("draw.ui.pot", pot=hand["pot"] if hand["phase"] != "complete" else hand["final_pot"], credit=state.trade_credit, stakes=tavern_text("draw.ui.wagered") if hand["wagering"] else tavern_text("draw.ui.free_practice")),
         accent("warning" if hand["wagering"] else "ui_accent"))
    _put(screen, 3, 2, tavern_format("draw.ui.dealer", dealer=hand["names"][hand["dealer"]], acting=hand["names"][hand["turn"]] if hand["turn"] is not None else tavern_text("draw.ui.none")))
    for seat in range(1, 4):
        column = 2 + (seat - 1) * 26
        name = hand["names"][seat][:23]
        balance = npc_credit(state, hand["players"][seat])
        _put(screen, 4, column, f"{name} ({balance})", accent("neutral", curses.A_BOLD))
        if not hand["active"][seat]:
            cards = tavern_text("draw.ui.folded")
        elif hand["phase"] == "complete" and seat <= reveal_count:
            cards = " ".join(card_name(card) for card in hand["hands"][seat])
        else:
            cards = "[##] " * 5
        _put(screen, 5, column, cards[:24])
        _put(screen, 6, column, tavern_format("draw.ui.paid", paid=hand["committed"][seat], status=tavern_text("draw.ui.in") if hand["active"][seat] else tavern_text("draw.ui.out")))
    _put(screen, 8, 2, f"{hand['names'][0]}  /  {draw_rank_name(evaluate(hand['hands'][0])[0])}", accent("player", curses.A_BOLD))
    for index, card in enumerate(hand["hands"][0]):
        column = 13 + index * 11
        for offset, line in enumerate(card_frame(card, selected=index in marked)):
            suit_role = "warning" if card // 13 in {1, 2} else "ui_accent"
            _put(screen, 10 + offset, column, line,
                 accent(suit_role, curses.A_REVERSE | curses.A_BOLD if index in marked else curses.A_BOLD))
        if hand["phase"] == "draw":
            _put(screen, 17, column + 3, str(index + 1))
    if hand["phase"] == "complete":
        winners = ", ".join(hand["names"][seat] for seat in hand["winners"])
        net = hand["payouts"][0] - hand["committed"][0]
        outcome = tavern_text("draw.ui.win") if 0 in hand["winners"] else tavern_text("draw.ui.out")
        _put(screen, 18, 2, tavern_format("draw.ui.outcome", outcome=outcome, winners=winners[:38], net=f"{net:+d}"),
             accent("success" if 0 in hand["winners"] else "warning", curses.A_REVERSE | curses.A_BOLD))
        _put(screen, 21, 2, tavern_text("draw.ui.clear"), accent("ui_accent"))
    elif hand["phase"] == "draw":
        _put(screen, 18, 2, tavern_format("draw.ui.selected", count=len(marked)))
        _put(screen, 21, 2, tavern_text("draw.ui.draw_controls"), accent("ui_accent"))
    else:
        due = hand["current_bet"] - hand["round_paid"][0]
        _put(screen, 18, 2, tavern_format("draw.ui.call", due=due, raises=hand["raises"]))
        _put(screen, 21, 2, tavern_text("draw.ui.bet_controls"), accent("ui_accent"))
    _put(screen, 19, 2, (message or hand["log"][-1])[:75], accent("warning") if message else 0)
    _put(screen, 22, 2, tavern_format("draw.ui.disclaimer", maximum=MAX_EXPOSURE), accent("terrain"))
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
    announced: int | None = None
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
                    message = tavern_text("draw.ui.seats_filled")
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
            drive_npcs(state, on_action=lambda current: (_draw_hand(screen, state, marked, "", 0), curses.napms(160)))
            continue
        if hand["phase"] == "complete" and announced != hand["number"]:
            for revealed in (1, 2, 3):
                _draw_hand(screen, state, marked, "", revealed)
                curses.napms(150)
            announced = hand["number"]
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
