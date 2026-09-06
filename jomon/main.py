"""Startup menu and curses application boundary."""

from __future__ import annotations

import curses
import random

from .save import SaveError, load_game, save_path, valid_save_exists
from .state import create_world
from .terminal import MIN_HEIGHT, MIN_WIDTH, _put, play

SEED_WORDS = ("reed", "hearth", "quay", "willow", "mill", "rain", "keel", "lantern")


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
    curses.curs_set(1)
    try:
        raw = screen.getstr(max(2, height // 2), max(1, width // 2 + 10), 48)
    finally:
        curses.noecho()
        curses.curs_set(0)
    seed = raw.decode("utf-8", errors="ignore").strip()
    return seed or _generated_seed()


def run(screen: curses.window) -> None:
    curses.curs_set(0)
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
        has_save = valid_save_exists()
        row = max(3, height // 2 - 1)
        if has_save:
            _put(screen, row, title_x, "C  Continue")
            row += 1
        _put(screen, row, title_x, "N  New World")
        _put(screen, row + 1, title_x, "Q  Quit")
        _put(screen, row + 3, max(1, width // 2 - 31), f"Save: {save_path()}")
        if notice:
            _put(screen, row + 5, max(1, width // 2 - 31), notice)
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
