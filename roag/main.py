"""Startup menu and curses application boundary."""

from __future__ import annotations

import curses
import random
import textwrap
from dataclasses import dataclass

from .catalog import CatalogError, WORLD_TEXT_SECTIONS, load_catalog
from .save import SaveError, load_game, save_path
from .state import GameState, create_world
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


def _start_new_world(screen: curses.window, state: GameState) -> bool:
    """Select a compact class kit and enter a fresh run without point-buy."""
    from .inventory import ensure_initial_field_tool

    from .profile import load_profile, persist_run_profile, save_profile
    from .run_progression import start_run

    profile = load_profile()
    class_id = _read_run_class(screen, profile.last_run_class)
    while True:
        profile.last_run_class = class_id
        try:
            save_profile(profile)
        except Exception:
            # Selection still belongs to this run if a local preferences write
            # is unavailable; settled-run persistence retains its reporting.
            pass
        ensure_initial_field_tool(state)
        start_run(state, profile=profile, class_id=class_id)
        play(screen, state)
        if state.run is None or state.run.status == "active":
            return True
        try:
            persist_run_profile(state)
            profile = load_profile()
        except Exception:
            # A completed run is still playable/retryable if its optional
            # profile write cannot be completed.
            pass
        next_action = _read_run_completion(screen, state, class_id)
        if next_action == "title":
            return False
        if next_action == "class":
            class_id = _read_run_class(screen, profile.last_run_class)
        else:
            class_id = profile.last_run_class
        # Retrying must not inherit terrain, health, issued ammunition, drops,
        # class cooldowns, or build state from the settled world.
        state = create_world(_generated_seed())


def _read_run_completion(
    screen: curses.window, state: GameState, class_id: str,
) -> str:
    """Offer an immediate clean retry after a victory or defeat."""
    run = state.run
    status = run.status.upper() if run is not None else "RUN ENDED"
    detail = run.failure_reason if run and run.failure_reason else "The final claimant fell."
    while True:
        screen.erase()
        height, width = screen.getmaxyx()
        _put(
            screen, max(1, height // 2 - 4), _centered_x(width, status), status,
            colour_attribute("ui_heading") | curses.A_BOLD,
        )
        for index, line in enumerate(textwrap.wrap(detail, width=max(24, width - 12))[:2]):
            _put(screen, max(2, height // 2 - 2 + index), 4, line)
        if run is not None:
            summary = f"Stage {run.stage_index}/5; level {run.level}; bosses defeated {len(run.boss_kills)}."
            _put(screen, min(height - 5, height // 2 + 1), 4, summary[:max(1, width - 8)])
        _put(screen, min(height - 3, height // 2 + 3), 4,
             f"R: fresh retry as {class_id.title()}    C: choose class    Q: title")
        screen.refresh()
        key = screen.getch()
        if key in {ord("r"), ord("R"), 10, 13}:
            return "retry"
        if key in {ord("c"), ord("C")}:
            return "class"
        if key in {ord("q"), ord("Q"), 27}:
            return "title"


def _read_run_class(screen: curses.window, last_class_id: str = "breaker") -> str:
    """Choose one complete fixed kit before a disposable run begins."""
    from .run_classes import RUN_CLASSES

    classes = tuple(RUN_CLASSES.values())
    selected = next(
        (index for index, definition in enumerate(classes) if definition.id == last_class_id),
        0,
    )
    while True:
        screen.erase()
        height, width = screen.getmaxyx()
        title = "CHOOSE RUN CLASS"
        _put(screen, max(1, height // 2 - 7), _centered_x(width, title), title,
             colour_attribute("ui_heading") | curses.A_BOLD)
        for index, definition in enumerate(classes):
            marker = ">" if index == selected else " "
            line = f"{marker} {definition.name}: {definition.description}"
            ability_line = (
                f"  [A] {definition.basic_description}  "
                f"[B] {definition.movement_name}  [X] {definition.signature_name}"
            )
            row = max(2, height // 2 - 5 + index * 2)
            _put(
                screen, row, 2,
                line[:max(1, width - 4)],
                colour_attribute("ui_accent") if index == selected else 0,
            )
            _put(screen, row + 1, 4, ability_line[:max(1, width - 6)],
                 colour_attribute("success") if index == selected else 0)
        _put(screen, min(height - 2, height // 2 + 4), 2,
             "Up/Down to choose; Enter to begin. Class kits are run-local.")
        screen.refresh()
        key = screen.getch()
        if key in {curses.KEY_UP, ord("k")}:
            selected = (selected - 1) % len(classes)
        elif key in {curses.KEY_DOWN, ord("j")}:
            selected = (selected + 1) % len(classes)
        elif key in {10, 13}:
            return classes[selected].id


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
                if _start_new_world(screen, state):
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
            if _start_new_world(screen, state):
                return
