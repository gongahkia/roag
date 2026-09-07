"""Startup menu and curses application boundary."""

from __future__ import annotations

import curses
import random
import textwrap
from dataclasses import dataclass

from .save import SaveError, load_game, save_path
from .state import create_world
from .terminal import MIN_HEIGHT, MIN_WIDTH, _put, play

SEED_WORDS = ("reed", "hearth", "quay", "willow", "mill", "rain", "keel", "lantern")


@dataclass(frozen=True)
class LandingNoticeLayout:
    top: int
    left: int
    width: int
    lines: tuple[str, ...]


def _set_cursor_visibility(visibility: int) -> bool:
    """Best-effort cursor control for terminals that do not expose it."""
    try:
        curses.curs_set(visibility)
        return True
    except curses.error:
        return False


def landing_notice_layout(width: int, height: int, notice: str) -> LandingNoticeLayout:
    """Wrap a complete startup warning inside a centered bounded panel."""
    panel_width = max(20, min(68, width - 8))
    text_width = max(10, panel_width - 4)
    lines = tuple(textwrap.wrap(notice, width=text_width, break_long_words=False, break_on_hyphens=False)) or ("",)
    panel_height = len(lines) + 4
    return LandingNoticeLayout(
        min(max(1, height - panel_height - 1), max(1, height // 2 + 3)),
        max(1, (width - panel_width) // 2),
        panel_width,
        lines,
    )


def _generated_seed() -> str:
    rng = random.SystemRandom()
    return f"{rng.choice(SEED_WORDS)}-{rng.choice(SEED_WORDS)}-{rng.randrange(1000, 10000)}"


def _read_seed(screen: curses.window) -> str:
    height, width = screen.getmaxyx()
    screen.erase()
    _put(screen, max(1, height // 2 - 2), max(1, width // 2 - 28), "NEW JOMON WORLD", curses.A_BOLD)
    _put(screen, max(2, height // 2), max(1, width // 2 - 28), "Readable seed (blank generates one): ")
    screen.refresh()
    curses.echo()
    _set_cursor_visibility(1)
    try:
        raw = screen.getstr(max(2, height // 2), max(1, width // 2 + 10), 48)
    finally:
        curses.noecho()
        _set_cursor_visibility(0)
    seed = raw.decode("utf-8", errors="ignore").strip()
    return seed or _generated_seed()


def run(screen: curses.window) -> None:
    _set_cursor_visibility(0)
    screen.keypad(True)
    notice = ""
    while True:
        screen.erase()
        height, width = screen.getmaxyx()
        if height < MIN_HEIGHT or width < MIN_WIDTH:
            _put(screen, max(0, height // 2 - 1), 1, f"Jomon needs at least {MIN_WIDTH}x{MIN_HEIGHT} terminal cells.", curses.A_BOLD)
            _put(screen, max(0, height // 2), 1, f"Current size: {width}x{height}. Resize or press Q to quit.")
            screen.refresh()
            key = screen.getch()
            if key in {ord("q"), ord("Q")}:
                return
            continue
        title_x = max(1, width // 2 - 18)
        _put(screen, max(1, height // 2 - 6), title_x, "J O M O N", curses.A_BOLD)
        _put(screen, max(2, height // 2 - 4), max(1, width // 2 - 31), "A vessel-household terminal roguelike")
        try:
            load_game()
            has_save = True
        except SaveError as exc:
            has_save = False
            if save_path().exists() and not notice:
                notice = f"Existing development save unavailable: {exc}"
        row = max(3, height // 2 - 1)
        if has_save:
            _put(screen, row, title_x, "C  Continue")
            row += 1
        _put(screen, row, title_x, "N  New World")
        _put(screen, row + 1, title_x, "Q  Quit")
        path_text = f"Save: {save_path()}"
        _put(screen, row + 3, max(1, (width - min(len(path_text), width - 4)) // 2), path_text)
        if notice:
            layout = landing_notice_layout(width, height, notice)
            from .terminal import _frame

            _frame(screen, layout.top, layout.left, len(layout.lines) + 4, layout.width, "DEVELOPMENT SAVE")
            for index, line in enumerate(layout.lines):
                _put(screen, layout.top + 2 + index, layout.left + max(2, (layout.width - len(line)) // 2), line)
            _put(
                screen, layout.top + len(layout.lines) + 2,
                layout.left + max(2, (layout.width - 34) // 2),
                "[N] Create new save   [Q] Return", curses.A_BOLD | curses.A_REVERSE,
            )
        screen.refresh()
        key = screen.getch()
        char = chr(key).lower() if 0 <= key < 256 else ""
        if char == "q":
            return
        if char == "c" and has_save:
            try:
                play(screen, load_game())
                return
            except SaveError as exc:
                notice = str(exc)
        if char == "n":
            state = create_world(_read_seed(screen))
            play(screen, state)
            return
