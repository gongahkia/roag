"""Small shared ASCII presentation pieces for Jomon's tavern games."""

from __future__ import annotations

import curses


def put(screen: curses.window, row: int, col: int, value: str, attr: int = 0) -> None:
    height, width = screen.getmaxyx()
    if 0 <= row < height and 0 <= col < width:
        try:
            screen.addnstr(row, col, value, max(0, width - col - 1), attr)
        except curses.error:
            pass


def accent(role: str, extra: int = 0) -> int:
    from .terminal import colour_attribute

    return colour_attribute(role) | extra


def border(screen: curses.window, title: str, role: str = "ui_frame") -> None:
    height, width = screen.getmaxyx()
    if height < 3 or width < 4:
        return
    right = width - 2
    edge = "+" + "-" * (right - 1) + "+"
    put(screen, 0, 0, edge, accent(role))
    put(screen, height - 1, 0, edge, accent(role))
    for row in range(1, height - 1):
        put(screen, row, 0, "|", accent(role))
        put(screen, row, right, "|", accent(role))
    put(screen, 0, 2, f"[ {title} ]", accent("ui_heading", curses.A_BOLD))


def meter(value: int, *, maximum: int = 60, width: int = 14) -> str:
    filled = min(width, max(0, value) * width // maximum)
    return "[" + "#" * filled + "." * (width - filled) + "]"


def dice_face(value: int) -> tuple[str, ...]:
    if value not in range(1, 7):
        raise ValueError("a six-sided die needs a face from one to six")
    marks = {
        1: ((1, 1),),
        2: ((0, 0), (2, 2)),
        3: ((0, 0), (1, 1), (2, 2)),
        4: ((0, 0), (2, 0), (0, 2), (2, 2)),
        5: ((0, 0), (2, 0), (1, 1), (0, 2), (2, 2)),
        6: ((0, 0), (1, 0), (2, 0), (0, 2), (1, 2), (2, 2)),
    }[value]
    rows = [[" " for _ in range(3)] for _ in range(3)]
    for x, y in marks:
        rows[y][x] = "o"
    return ("+-------+", *("| " + " ".join(row) + " |" for row in rows), "+-------+")
