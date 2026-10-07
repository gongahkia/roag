"""Startup menu and curses application boundary."""

from __future__ import annotations

import curses
import random
import textwrap
from dataclasses import dataclass

from .catalog import CatalogError, WORLD_TEXT_SECTIONS, load_catalog
from .character_ui import run_character_creation
from .save import SaveError, load_game, save_path
from .state import create_world
from .terminal import MIN_HEIGHT, MIN_WIDTH, _draw_minimum_size_notice, _init_colours, _put, colour_attribute, play
from .ui_presentation import ui_format, ui_text

_SEED_WORDS = load_catalog("world_text.json", WORLD_TEXT_SECTIONS)["seed_words"]
if (not isinstance(_SEED_WORDS, list) or len(_SEED_WORDS) < 2
        or any(not isinstance(word, str) or not word for word in _SEED_WORDS)):
    raise CatalogError("world_text.json has invalid seed words")
SEED_WORDS = tuple(_SEED_WORDS)


@dataclass(frozen=True)
class LandingNoticeLayout:
    top: int
    left: int
    width: int
    height: int
    lines: tuple[str, ...]
    path_lines: tuple[str, ...]


def _set_cursor_visibility(visibility: int) -> bool:
    """Best-effort cursor control for terminals that do not expose it."""
    try:
        curses.curs_set(visibility)
        return True
    except curses.error:
        return False


def _centered_x(width: int, text: str) -> int:
    return max(1, (width - len(text)) // 2)


def landing_notice_layout(
    width: int,
    height: int,
    notice: str,
    path: str = "",
) -> LandingNoticeLayout:
    """Lay out one centred save warning card without competing menu content."""
    panel_width = max(20, min(68, width - 8))
    text_width = max(10, panel_width - 8)
    lines = tuple(textwrap.wrap(notice, width=text_width, break_long_words=False, break_on_hyphens=False)) or ("",)
    path_lines = tuple(textwrap.wrap(
        f"Save file: {path}", width=text_width,
        break_long_words=True, break_on_hyphens=False,
    )) if path else ()
    panel_height = len(lines) + len(path_lines) + 8
    block_height = panel_height + 6
    block_top = max(1, (height - block_height) // 2)
    return LandingNoticeLayout(
        block_top + 6,
        max(1, (width - panel_width) // 2),
        panel_width,
        panel_height,
        lines,
        path_lines,
    )


def _draw_notice_landing(
    screen: curses.window,
    width: int,
    height: int,
    notice: str,
) -> None:
    from .terminal import _frame

    layout = landing_notice_layout(width, height, notice, str(save_path()))
    title = ui_text("ui.title.game")
    subtitle = ui_text("ui.title.subtitle")
    _put(screen, layout.top - 6, _centered_x(width, title), title, colour_attribute("ui_heading") | curses.A_BOLD)
    _put(screen, layout.top - 4, _centered_x(width, subtitle), subtitle, colour_attribute("ui_accent"))
    _frame(screen, layout.top, layout.left, layout.height, layout.width, ui_text("ui.start.save_mismatch"))
    inner_left = layout.left + 4
    for index, line in enumerate(layout.lines):
        _put(screen, layout.top + 2 + index, _centered_x(width, line), line, colour_attribute("warning") | curses.A_BOLD)
    path_row = layout.top + 3 + len(layout.lines)
    for index, line in enumerate(layout.path_lines):
        _put(screen, path_row + index, _centered_x(width, line), line, curses.A_DIM)
    option_row = path_row + len(layout.path_lines) + 2
    _put(screen, option_row, inner_left, "> ", colour_attribute("ui_accent") | curses.A_BOLD)
    _put(screen, option_row, inner_left + 2, "N", colour_attribute("success") | curses.A_BOLD | curses.A_REVERSE)
    _put(screen, option_row, inner_left + 4, ui_text("ui.start.new_world"), colour_attribute("success") | curses.A_BOLD)
    _put(screen, option_row + 1, inner_left, "  ")
    _put(screen, option_row + 1, inner_left + 2, "Q", colour_attribute("warning") | curses.A_BOLD | curses.A_REVERSE)
    _put(screen, option_row + 1, inner_left + 4, ui_text("ui.start.leave_unopened"))


def _generated_seed() -> str:
    rng = random.SystemRandom()
    return f"{rng.choice(SEED_WORDS)}-{rng.choice(SEED_WORDS)}-{rng.randrange(1000, 10000)}"


def _read_seed(screen: curses.window) -> str:
    height, width = screen.getmaxyx()
    screen.erase()
    _put(screen, max(1, height // 2 - 2), max(1, width // 2 - 28), ui_text("ui.start.begin"), colour_attribute("ui_heading") | curses.A_BOLD)
    _put(screen, max(2, height // 2), max(1, width // 2 - 28), ui_text("ui.start.seed_prompt"), colour_attribute("ui_accent"))
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
    _init_colours()
    notice = ""
    while True:
        screen.erase()
        height, width = screen.getmaxyx()
        if height < MIN_HEIGHT or width < MIN_WIDTH:
            _draw_minimum_size_notice(screen)
            screen.refresh()
            key = screen.getch()
            if key in {ord("q"), ord("Q")}:
                return
            continue
        try:
            load_game()
            has_save = True
        except SaveError as exc:
            has_save = False
            if save_path().exists() and not notice:
                notice = f"This chronicle cannot be opened: {exc}. The file has not been changed."
        if notice:
            _draw_notice_landing(screen, width, height, notice)
            screen.refresh()
            key = screen.getch()
            char = chr(key).lower() if 0 <= key < 256 else ""
            if char == "q":
                return
            if char == "n":
                state = create_world(_read_seed(screen))
                if run_character_creation(screen, state):
                    play(screen, state)
                    return
            continue
        title = ui_text("ui.title.game")
        subtitle = ui_text("ui.title.subtitle")
        _put(screen, max(1, height // 2 - 6), _centered_x(width, title), title, colour_attribute("ui_heading") | curses.A_BOLD)
        _put(screen, max(2, height // 2 - 4), _centered_x(width, subtitle), subtitle, colour_attribute("ui_accent"))
        title_x = max(1, width // 2 - 8)
        row = max(3, height // 2 - 1)
        if has_save:
            _put(screen, row, title_x, ui_text("ui.start.continue"), colour_attribute("success"))
            row += 1
        _put(screen, row, title_x, ui_text("ui.start.new_chronicle"), colour_attribute("ui_accent"))
        _put(screen, row + 1, title_x, ui_text("ui.start.close"), colour_attribute("warning"))
        path_text = ui_format("ui.start.save_path", path=save_path())
        _put(screen, row + 3, max(1, (width - min(len(path_text), width - 4)) // 2), path_text)
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
            if run_character_creation(screen, state):
                play(screen, state)
                return
