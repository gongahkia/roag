"""Shared curses drawing primitives for the tavern's single expedition mode."""

from __future__ import annotations

import curses
import textwrap

from .content import Catalog


class TavernUIBase:
    MIN_ROWS = 24
    MIN_COLS = 80
    DAMAGE_FLASH_MS = 110
    MOVE_FRAME_MS = 55

    def __init__(self, screen: curses.window, catalog: Catalog):
        self.screen = screen
        self.catalog = catalog
        self.message = ""
        self.colour = False
        self._configure()

    def _configure(self) -> None:
        curses.curs_set(0)
        self.screen.keypad(True)
        try:
            curses.mousemask(curses.ALL_MOUSE_EVENTS)
            curses.mouseinterval(0)
        except curses.error:
            pass
        if curses.has_colors():
            curses.start_color()
            curses.use_default_colors()
            curses.init_pair(1, curses.COLOR_CYAN, -1)
            curses.init_pair(2, curses.COLOR_YELLOW, -1)
            curses.init_pair(3, curses.COLOR_RED, -1)
            curses.init_pair(4, curses.COLOR_GREEN, -1)
            curses.init_pair(5, curses.COLOR_WHITE, curses.COLOR_RED)
            curses.init_pair(6, curses.COLOR_WHITE, curses.COLOR_GREEN)
            self.colour = True

    def _route_cancel_requested(self) -> bool:
        try:
            self.screen.nodelay(True)
            key = self._key()
        finally:
            self.screen.nodelay(False)
        if key in (ord("x"), ord("X"), 27):
            return True
        if key != curses.KEY_MOUSE:
            return False
        try:
            _, _, _, _, buttons = curses.getmouse()
        except curses.error:
            return False
        right_click = getattr(curses, "BUTTON3_CLICKED", 0) | getattr(curses, "BUTTON3_PRESSED", 0)
        return bool(buttons & right_click)

    def _combat_column(self, column: int) -> int:
        return column + max(0, (self.screen.getmaxyx()[1] - self.MIN_COLS) // 2)

    @staticmethod
    def _ellipsize(value: str, width: int) -> str:
        if len(value) <= width:
            return value
        return value[:max(0, width - 3)].rstrip() + "..."

    def _draw_sprite(self, row: int, column: int, lines, attr: int = 0) -> None:
        for offset, line in enumerate(lines):
            self._put(row + offset, column, line.ljust(7), attr)

    def _target_brackets(self, row: int, column: int, attr: int) -> None:
        self._put(row, column - 1, ">", attr | curses.A_BOLD)
        self._put(row, column + 7, "<", attr | curses.A_BOLD)

    def _menu(self, title: str, choices: list[str], body: str = "", *, allow_cancel: bool = True) -> int | None:
        if not choices:
            raise ValueError("a menu needs at least one choice")
        selected = 0
        scroll = 0
        body_scroll = 0
        while True:
            self._begin(title)
            width = max(20, self.screen.getmaxyx()[1] - 6)
            body_lines = [line for paragraph in body.splitlines()
                          for line in (textwrap.wrap(paragraph, width) or [""])]
            body_height = max(1, self.screen.getmaxyx()[0] - 7 - min(4, len(choices)))
            body_scroll = max(0, min(body_scroll, max(0, len(body_lines) - body_height)))
            for offset, line in enumerate(body_lines[body_scroll:body_scroll + body_height]):
                self._put(3 + offset, 3, line)
            row = 4 + min(len(body_lines), body_height)
            available = max(1, self.screen.getmaxyx()[0] - row - 3)
            if selected < scroll:
                scroll = selected
            if selected >= scroll + available:
                scroll = selected - available + 1
            for shown, choice in enumerate(choices[scroll:scroll + available]):
                index = scroll + shown
                marker = ">" if index == selected else " "
                attr = curses.A_REVERSE if index == selected else 0
                self._put(row + shown, 3, f"{marker} {choice}"[:width], attr)
            footer = ("Up/Down choose  Home/End body  PgUp/PgDn details  Enter confirm  Esc back"
                      if len(body_lines) > body_height else "Up/Down choose  Enter confirm  Esc back")
            self._footer(footer)
            key = self._key()
            if key in (curses.KEY_UP, curses.KEY_LEFT, ord("k"), ord("h")):
                selected = (selected - 1) % len(choices)
            elif key in (curses.KEY_DOWN, curses.KEY_RIGHT, ord("j"), ord("l")):
                selected = (selected + 1) % len(choices)
            elif key == curses.KEY_PPAGE:
                body_scroll -= body_height
            elif key == curses.KEY_NPAGE:
                body_scroll += body_height
            elif key == curses.KEY_HOME:
                body_scroll = 0
            elif key == curses.KEY_END:
                body_scroll = len(body_lines) - body_height
            elif key in (10, 13, curses.KEY_ENTER):
                return selected
            elif key == 27 and allow_cancel:
                return None

    def _notice(self, title: str, body: str) -> None:
        width = max(20, self.screen.getmaxyx()[1] - 6)
        lines = [line for paragraph in body.splitlines()
                 for line in (textwrap.wrap(paragraph, width) or [""])]
        scroll = 0
        while True:
            self._begin(title)
            available = max(1, self.screen.getmaxyx()[0] - 5)
            scroll = max(0, min(scroll, max(0, len(lines) - available)))
            for offset, line in enumerate(lines[scroll:scroll + available]):
                self._put(3 + offset, 3, line)
            footer = ("Press any key" if len(lines) <= available else
                      f"Up/Down or PgUp/PgDn scroll  Home/End jump  Enter/Esc close  {scroll + 1}-{min(len(lines), scroll + available)}/{len(lines)}")
            self._footer(footer)
            key = self._key()
            if len(lines) <= available:
                return
            if key in (curses.KEY_UP, ord("k")):
                scroll -= 1
            elif key in (curses.KEY_DOWN, ord("j")):
                scroll += 1
            elif key == curses.KEY_PPAGE:
                scroll -= available
            elif key == curses.KEY_NPAGE:
                scroll += available
            elif key == curses.KEY_HOME:
                scroll = 0
            elif key == curses.KEY_END:
                scroll = len(lines) - available
            elif key in (10, 13, curses.KEY_ENTER, 27):
                return

    def _begin(self, title: str) -> None:
        self._ensure_size()
        self.screen.erase()
        width = self.screen.getmaxyx()[1]
        self._put(0, max(0, (width - len(title)) // 2), title, curses.A_BOLD | self._attr(1))
        if self.message:
            self._put(1, 2, self._ellipsize(self.message, width - 3), self._attr(2))
            self.message = ""

    def _footer(self, value: str) -> None:
        self._put(self.screen.getmaxyx()[0] - 1, 1, value, curses.A_DIM)
        self.screen.refresh()

    def _ensure_size(self) -> None:
        while True:
            rows, cols = self.screen.getmaxyx()
            if rows >= self.MIN_ROWS and cols >= self.MIN_COLS:
                return
            self.screen.erase()
            value = f"Terminal too small ({cols}x{rows}); resize to at least {self.MIN_COLS}x{self.MIN_ROWS}. Q quits."
            try:
                self.screen.addstr(0, 0, value[:max(1, cols - 1)])
            except curses.error:
                pass
            self.screen.refresh()
            if self.screen.getch() in (ord("q"), ord("Q")):
                raise KeyboardInterrupt

    def _key(self) -> int:
        try:
            return self.screen.getch()
        except curses.error:
            return -1

    def _put(self, row: int, column: int, value: str, attr: int = 0) -> None:
        rows, cols = self.screen.getmaxyx()
        if row < 0 or row >= rows or column >= cols:
            return
        try:
            self.screen.addstr(row, max(0, column), value[:max(0, cols - max(0, column) - 1)], attr)
        except curses.error:
            pass

    def _attr(self, pair: int) -> int:
        return curses.color_pair(pair) if self.colour else 0

    def _hp_attr(self, hp: int, maximum: int) -> int:
        if not self.colour:
            return 0
        ratio = hp / maximum
        return self._attr(4 if ratio > 0.5 else 2 if ratio > 0.25 else 3)
