"""A visibly rolled, four-seat Quay Bones game at the tavern table."""

from __future__ import annotations

import curses

from .character_presentation import role_display_name
from .state import GameState
from .tavern_dice import (
    MAX_ROLLS, ROUNDS, available_dice_opponents, close_match, drive_npcs,
    hold, roll, start_match,
)
from .tavern_games_ui import accent, border, dice_face, meter, put
from .ui_presentation import ui_text
from .tavern_presentation import tavern_format, tavern_text


def _draw_lobby(screen: curses.window, state: GameState, selected: list[str], cursor: int,
                message: str) -> list:
    people = available_dice_opponents(state)
    screen.erase()
    bartender_name = state.bartender.name.split()[0]
    border(screen, tavern_format("dice.ui.title.lobby", bartender=bartender_name.upper()))
    put(screen, 2, 3, tavern_text("dice.ui.rules.1"), accent("ui_accent"))
    put(screen, 3, 3, tavern_text("dice.ui.rules.2"))
    put(screen, 4, 3, tavern_text("dice.ui.rules.3"))
    put(screen, 5, 3, tavern_format("dice.ui.purse", purse=state.tavern_dice["purse"], prize=2), accent("success"))
    put(screen, 6, 3, tavern_format("dice.ui.credit", credit=state.trade_credit))
    put(screen, 7, 3, tavern_format("dice.ui.invite", selected=len(selected)), accent("ui_heading"))
    first = max(0, cursor - 10)
    for index in range(first, min(first + 11, len(people))):
        person = people[index]
        mark = "[x]" if person.id in selected else "[ ]"
        put(screen, 8 + index - first, 3,
            f"{'>' if index == cursor else ' '} {mark} {person.name[:24]:24} {role_display_name(person.role)[:22]}",
            accent("ui_accent", curses.A_REVERSE) if index == cursor else 0)
    if len(people) > 11:
        put(screen, 19, 3, f"Showing {first + 1}-{min(first + 11, len(people))} of {len(people)}.")
    put(screen, 20, 3, message[:73] if message else tavern_format("dice.ui.empty", bartender=bartender_name),
        accent("warning") if message else accent("terrain"))
    put(screen, 22, 3, tavern_text("dice.ui.controls"), accent("ui_accent", curses.A_BOLD))
    screen.refresh()
    return people


def _draw_match(screen: curses.window, state: GameState, message: str = "",
                shown_dice: tuple[int, int] | None = None) -> None:
    match = state.tavern_dice["active_match"]
    assert match is not None
    screen.erase()
    complete = match["phase"] == "complete"
    border(screen, tavern_text("dice.ui.title.complete") if complete else ui_text("ui.tavern.dice.title"),
           "success" if complete and 0 in match["winners"] else "ui_frame")
    put(screen, 2, 3,
        tavern_format("dice.ui.round", round=min(match["round"] + 1, ROUNDS), rounds=ROUNDS, purse=state.tavern_dice["purse"], credit=state.trade_credit),
        accent("ui_accent"))
    for seat in range(4):
        row = 4 + seat * 3
        active = match["turn"] == seat and not complete
        label = f"{'>' if active else ' '} {match['names'][seat][:18]:18} {match['scores'][seat]:>3}"
        put(screen, row, 3, label, accent("player" if seat == 0 else "neutral", curses.A_BOLD if active else 0))
        put(screen, row + 1, 5, meter(match["scores"][seat]) + tavern_format("dice.ui.busts", count=match["busts"][seat]),
            accent("success" if complete and seat in match["winners"] else "ui_accent" if active else "terrain"))
    put(screen, 4, 45, ui_text("ui.tavern.dice.bones"), accent("ui_heading", curses.A_BOLD))
    dice = shown_dice or tuple(match["last_dice"])
    if dice:
        for index, value in enumerate(dice):
            for offset, line in enumerate(dice_face(value)):
                put(screen, 6 + offset, 43 + index * 13, line,
                    accent("warning" if value == 1 else "success" if value == 6 else "ui_accent", curses.A_BOLD))
    else:
        put(screen, 8, 43, tavern_text("dice.ui.hidden"), accent("ui_accent"))
    put(screen, 12, 43, tavern_format("dice.ui.turn_pot", total=match["turn_total"]), accent("success", curses.A_BOLD))
    put(screen, 13, 43, tavern_format("dice.ui.rolls", count=match["roll_count"], maximum=MAX_ROLLS))
    if not complete and match["forced"]:
        put(screen, 14, 43, tavern_text("dice.ui.double"), accent("warning", curses.A_BOLD))
    if complete:
        winners = ", ".join(match["names"][seat] for seat in match["winners"])
        role = "success" if 0 in match["winners"] and match["prize"] else "warning"
        put(screen, 17, 3, tavern_format("dice.ui.outcome", outcome=tavern_text("dice.ui.win") if 0 in match["winners"] else tavern_text("dice.ui.result"), winners=winners, prize=match["prize"]),
            accent(role, curses.A_REVERSE | curses.A_BOLD))
        put(screen, 21, 3, tavern_text("dice.ui.clear"), accent("ui_accent"))
    else:
        put(screen, 17, 3, tavern_format("dice.ui.acting", player=match["names"][match["turn"]]), accent("ui_heading"))
        put(screen, 21, 3, ui_text("ui.tavern.dice.controls"), accent("ui_accent"))
    put(screen, 19, 3, (message or match["log"][-1])[:72], accent("warning") if message else accent("terrain"))
    put(screen, 22, 3, tavern_format("dice.ui.disclaimer", bartender=state.bartender.name.split()[0]))
    screen.refresh()


def _resume_other_courier(state: GameState) -> None:
    match = state.tavern_dice["active_match"]
    if not match or match["phase"] == "complete" or match["players"][0] == state.active_courier_id:
        return
    for _ in range(ROUNDS * MAX_ROLLS * 4 + 4):
        drive_npcs(state)
        if match["phase"] == "complete":
            return
        if match["roll_count"] and not match["forced"] and match["turn_total"] >= 12:
            hold(state)
        else:
            roll(state)
    raise RuntimeError("previous courier's bones contest did not finish")


def run_tavern_dice(screen: curses.window, state: GameState) -> None:
    selected: list[str] = []
    cursor = 0
    message = ""
    _resume_other_courier(state)
    while True:
        match = state.tavern_dice["active_match"]
        if match is None:
            cursor = min(cursor, max(0, len(available_dice_opponents(state)) - 1))
            people = _draw_lobby(screen, state, selected, cursor, message)
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
                    message = tavern_text("dice.ui.seats_filled")
            elif normalized in (10, 13, curses.KEY_ENTER):
                try:
                    start_match(state, selected)
                    message = ""
                except ValueError as exc:
                    message = str(exc)
            continue
        if match["phase"] != "complete" and match["turn"] != 0:
            drive_npcs(state, on_action=lambda current: (_draw_match(screen, state), curses.napms(140)))
            continue
        _draw_match(screen, state, message)
        key = screen.getch()
        normalized = ord(chr(key).lower()) if 0 <= key < 256 else key
        message = ""
        if normalized in (ord("q"), 27):
            return
        if match["phase"] == "complete":
            if normalized in (10, 13, curses.KEY_ENTER):
                close_match(state)
                selected = []
                cursor = 0
            continue
        try:
            if normalized in (ord("r"), 10, 13, curses.KEY_ENTER):
                for preview in ((2, 5), (4, 3)):
                    _draw_match(screen, state, shown_dice=preview)
                    curses.napms(65)
                roll(state)
            elif normalized == ord("h"):
                hold(state)
        except ValueError as exc:
            message = str(exc)
