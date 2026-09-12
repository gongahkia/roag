"""A visibly rolled, four-seat Quay Bones game at the tavern table."""

from __future__ import annotations

import curses

from .state import GameState
from .tavern_dice import (
    MAX_ROLLS, ROUNDS, available_dice_opponents, close_match, drive_npcs,
    hold, roll, start_match,
)
from .tavern_games_ui import accent, border, dice_face, meter, put


def _draw_lobby(screen: curses.window, state: GameState, selected: list[str], cursor: int,
                message: str) -> list:
    people = available_dice_opponents(state)
    screen.erase()
    border(screen, "QUAY BONES / SENA'S CHALLENGE")
    put(screen, 2, 3, "Four adults, two carved bones, three rounds.", accent("ui_accent"))
    put(screen, 3, 3, "Roll or bank. One 1 busts; doubles force another roll.")
    put(screen, 4, 3, "Six rolls maximum per turn. Best score wins; a tie pays nothing.")
    put(screen, 5, 3, f"Counted purse: {state.tavern_dice['purse']} credit. Unique winner receives up to 2.", accent("success"))
    put(screen, 6, 3, f"Your credit: {state.trade_credit}. No entry fee; the purse does not refill.")
    put(screen, 7, 3, f"Invite three adults at the table ({len(selected)}/3):", accent("ui_heading"))
    first = max(0, cursor - 10)
    for index in range(first, min(first + 11, len(people))):
        person = people[index]
        mark = "[x]" if person.id in selected else "[ ]"
        put(screen, 8 + index - first, 3,
            f"{'>' if index == cursor else ' '} {mark} {person.name[:24]:24} {person.role[:22]}",
            accent("ui_accent", curses.A_REVERSE) if index == cursor else 0)
    if len(people) > 11:
        put(screen, 19, 3, f"Showing {first + 1}-{min(first + 11, len(people))} of {len(people)}.")
    put(screen, 20, 3, message[:73] if message else "Sena keeps the prize purse on the table, not in an endless machine.",
        accent("warning") if message else accent("terrain"))
    put(screen, 22, 3, "J/K choose  Space invite  Enter begin  Q leave", accent("ui_accent", curses.A_BOLD))
    screen.refresh()
    return people


def _draw_match(screen: curses.window, state: GameState, message: str = "",
                shown_dice: tuple[int, int] | None = None) -> None:
    match = state.tavern_dice["active_match"]
    assert match is not None
    screen.erase()
    complete = match["phase"] == "complete"
    border(screen, "QUAY BONES / FINAL SCORE" if complete else "QUAY BONES / ROLL OR BANK",
           "success" if complete and 0 in match["winners"] else "ui_frame")
    put(screen, 2, 3,
        f"Round {min(match['round'] + 1, ROUNDS)}/{ROUNDS}   Prize purse {state.tavern_dice['purse']} credit   Your credit {state.trade_credit}",
        accent("ui_accent"))
    for seat in range(4):
        row = 4 + seat * 3
        active = match["turn"] == seat and not complete
        label = f"{'>' if active else ' '} {match['names'][seat][:18]:18} {match['scores'][seat]:>3}"
        put(screen, row, 3, label, accent("player" if seat == 0 else "neutral", curses.A_BOLD if active else 0))
        put(screen, row + 1, 5, meter(match["scores"][seat]) + f"  busts {match['busts'][seat]}",
            accent("success" if complete and seat in match["winners"] else "ui_accent" if active else "terrain"))
    put(screen, 4, 45, "THE BONES", accent("ui_heading", curses.A_BOLD))
    dice = shown_dice or tuple(match["last_dice"])
    if dice:
        for index, value in enumerate(dice):
            for offset, line in enumerate(dice_face(value)):
                put(screen, 6 + offset, 43 + index * 13, line,
                    accent("warning" if value == 1 else "success" if value == 6 else "ui_accent", curses.A_BOLD))
    else:
        put(screen, 8, 43, "[ hidden under cup ]", accent("ui_accent"))
    put(screen, 12, 43, f"Turn pot: {match['turn_total']:>3} points", accent("success", curses.A_BOLD))
    put(screen, 13, 43, f"Rolls: {match['roll_count']}/{MAX_ROLLS}")
    if not complete and match["forced"]:
        put(screen, 14, 43, "DOUBLE: roll again", accent("warning", curses.A_BOLD))
    if complete:
        winners = ", ".join(match["names"][seat] for seat in match["winners"])
        role = "success" if 0 in match["winners"] and match["prize"] else "warning"
        put(screen, 17, 3, f"{'WIN' if 0 in match['winners'] else 'RESULT'}: {winners}  /  {match['prize']} credit paid",
            accent(role, curses.A_REVERSE | curses.A_BOLD))
        put(screen, 21, 3, "Enter clear result  Q return to tavern", accent("ui_accent"))
    else:
        put(screen, 17, 3, f"Acting: {match['names'][match['turn']]}", accent("ui_heading"))
        put(screen, 21, 3, "R/Enter roll  H bank when allowed  Q pause contest", accent("ui_accent"))
    put(screen, 19, 3, (message or match["log"][-1])[:72], accent("warning") if message else accent("terrain"))
    put(screen, 22, 3, "No entry fee. Every paid prize leaves the finite tavern purse.")
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
                    message = "Three seats are filled. Deselect someone first."
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
