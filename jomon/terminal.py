"""Fullscreen curses presentation; gameplay remains in the action modules."""

from __future__ import annotations

import curses
from typing import Iterable

from .actions import (
    attack,
    choose_courier,
    choose_loadout,
    choose_support,
    decide_objective,
    guard,
    interact,
    move,
    negotiate,
    retreat,
    use_gear,
)
from .content import COMMODITIES, HELP_LINES, LOADOUTS, SUPPORTS
from .save import SaveError, save_game
from .state import GameState, Position
from .world import area_name, base_tile, capacity, carried_bulk, displayed_tile, map_rows, pressure

MIN_WIDTH = 80
MIN_HEIGHT = 24


def _put(screen: curses.window, y: int, x: int, text: str, attr: int = 0) -> None:
    height, width = screen.getmaxyx()
    if 0 <= y < height and x < width - 1:
        try:
            screen.addnstr(y, max(0, x), text[max(0, -x):], max(0, width - max(0, x) - 1), attr)
        except curses.error:
            pass


def _line(screen: curses.window, y: int, x: int, width: int, title: str = "") -> None:
    label = f" {title} " if title else ""
    _put(screen, y, x, "+" + label + "-" * max(0, width - len(label) - 2) + "+")


def _frame(screen: curses.window, y: int, x: int, height: int, width: int, title: str = "") -> None:
    _line(screen, y, x, width, title)
    for row in range(y + 1, y + height - 1):
        _put(screen, row, x, "|")
        _put(screen, row, x + width - 1, "|")
    _line(screen, y + height - 1, x, width)


def _clip(text: str, width: int) -> str:
    return text if len(text) <= width else text[: max(0, width - 3)] + "..."


def _wrapped(text: str, width: int) -> list[str]:
    words = text.split()
    lines: list[str] = []
    current = ""
    for word in words:
        if current and len(current) + len(word) + 1 > width:
            lines.append(current)
            current = word
        else:
            current = f"{current} {word}".strip()
    if current:
        lines.append(current)
    return lines or [""]


def _init_colours() -> None:
    if not curses.has_colors():
        return
    curses.start_color()
    curses.use_default_colors()
    curses.init_pair(1, curses.COLOR_CYAN, -1)
    curses.init_pair(2, curses.COLOR_GREEN, -1)
    curses.init_pair(3, curses.COLOR_YELLOW, -1)
    curses.init_pair(4, curses.COLOR_RED, -1)


def _map_attr(tile: str) -> int:
    if not curses.has_colors():
        return 0
    if tile in {"~", "=", "-", "+"}:
        return curses.color_pair(1)
    if tile in {"T", "r"}:
        return curses.color_pair(2)
    if tile in {"M", "R", "&", "C", "L", "P", "H"}:
        return curses.color_pair(3)
    return 0


def _draw_map(screen: curses.window, state: GameState, top: int, left: int, height: int, width: int) -> None:
    rows = map_rows(state)
    view_height = height - 2
    view_width = width - 2
    origin_y = max(0, min(state.position.y - view_height // 2, max(0, len(rows) - view_height)))
    origin_x = max(0, min(state.position.x - view_width // 2, max(0, max(map(len, rows)) - view_width)))
    threat_visible = state.location == "region" and state.threat.status in {"watching", "engaged"}
    for sy in range(view_height):
        world_y = origin_y + sy
        if world_y >= len(rows):
            break
        for sx in range(view_width):
            world_x = origin_x + sx
            position = Position(world_x, world_y)
            if position == state.position:
                char, attr = "@", curses.A_BOLD | (curses.color_pair(3) if curses.has_colors() else 0)
            elif threat_visible and position == state.threat.position:
                char, attr = "!", curses.A_BOLD | (curses.color_pair(4) if curses.has_colors() else 0)
            else:
                char = displayed_tile(state, position)
                attr = _map_attr(char)
            _put(screen, top + 1 + sy, left + 1 + sx, char, attr)


def _status_lines(state: GameState) -> list[str]:
    courier = state.courier
    p = pressure(state)
    if courier:
        health = f"{courier.health}/{courier.max_health}"
        identity = f"{courier.name}, {courier.role}"
        injury = courier.injury
        technique = courier.technique
    else:
        identity, health, injury, technique = "not chosen", "-", "-", "-"
    commodity = state.region.objective_commodity
    market = state.market[commodity]
    objective = state.objective_status
    return [
        "COURIER", identity, f"Health: {health}", f"Injury: {injury}",
        f"Technique: {technique}", f"Loadout: {state.loadout or '-'}", f"Support: {state.support or '-'}", "",
        "PRESSURE", f"Elapsed: {p.elapsed}", f"Depth: {p.depth}", f"Noise: {p.noise}",
        f"Valuables: {p.valuables}", f"Band: {p.band} ({p.score})", "",
        "HEARTHFORD", f"Objective: {objective}", f"{commodity}: stock {market.stock}", f"Demand: {market.demand}",
        f"Threat: {state.threat.status}", _clip(state.threat.intent, 25),
    ]


def _draw_base(screen: curses.window, state: GameState) -> None:
    screen.erase()
    height, width = screen.getmaxyx()
    if height < MIN_HEIGHT or width < MIN_WIDTH:
        _put(screen, max(0, height // 2 - 1), 1, f"Jomon needs at least {MIN_WIDTH}x{MIN_HEIGHT} terminal cells.", curses.A_BOLD)
        _put(screen, max(0, height // 2), 1, f"Current size: {width}x{height}. Resize or press Q to quit.")
        screen.refresh()
        return
    status_width = 29
    event_height = 6
    command_height = 2
    main_height = height - event_height - command_height
    map_width = width - status_width
    _frame(screen, 0, 0, main_height, map_width, area_name(state).upper())
    _frame(screen, 0, map_width, main_height, status_width, "STATUS")
    _draw_map(screen, state, 0, 0, main_height, map_width)
    for index, line in enumerate(_status_lines(state)[: main_height - 2]):
        attr = curses.A_BOLD if line in {"COURIER", "PRESSURE", "HEARTHFORD"} else 0
        _put(screen, 1 + index, map_width + 2, _clip(line, status_width - 4), attr)
    _frame(screen, main_height, 0, event_height, width, "EVENTS")
    event_lines: list[str] = []
    for message in state.messages:
        event_lines.extend(_wrapped(message, width - 4))
    for index, line in enumerate(event_lines[-(event_height - 2):]):
        _put(screen, main_height + 1 + index, 2, _clip(line, width - 4))
    _put(screen, height - 2, 1, "Move HJKL/YUBN/arrows  E interact  A attack  G guard  X gear  V negotiate", curses.A_REVERSE)
    _put(screen, height - 1, 1, "I inventory  S save aboard  ? help  Q quit", curses.A_REVERSE)
    screen.refresh()


def _overlay(screen: curses.window, title: str, lines: Iterable[str]) -> None:
    height, width = screen.getmaxyx()
    material = list(lines)
    box_width = min(width - 4, max(44, max((len(line) for line in material), default=20) + 4))
    box_height = min(height - 4, len(material) + 4)
    top, left = (height - box_height) // 2, (width - box_width) // 2
    for y in range(top, top + box_height):
        _put(screen, y, left, " " * box_width, curses.A_REVERSE)
    _frame(screen, top, left, box_height, box_width, title)
    for index, line in enumerate(material[: box_height - 3]):
        _put(screen, top + 2 + index, left + 2, _clip(line, box_width - 4))
    screen.refresh()


def _overlay_lines(state: GameState, kind: str) -> tuple[str, list[str]]:
    if kind == "help":
        return "HELP", list(HELP_LINES)
    if kind == "inventory":
        goods = [f"{name}: {stack.quantity}, {stack.condition} ({COMMODITIES[name]['bulk']} bulk each)" for name, stack in state.carried_goods.items()]
        lines = [f"Capacity: {carried_bulk(state)}/{capacity(state)} bulk", "Equipment: " + (", ".join(state.inventory) or "none"), "Goods:"] + (goods or ["none"])
        return "INVENTORY", lines + ["Escape closes this inspection without advancing time."]
    if kind == "courier":
        lines = [f"{index + 1}. {p.name} — {p.role}; {p.technique}; {p.injury}" for index, p in enumerate(state.household) if p.alive]
        return "CHOOSE COURIER", lines + ["Number selects; Escape cancels. Selection takes no time."]
    if kind == "loadout":
        return "CHOOSE LOADOUT", [f"{i + 1}. {row[1]} — {row[3]}" for i, row in enumerate(LOADOUTS)] + ["Number selects; Escape cancels."]
    if kind == "support":
        return "CHOOSE SUPPORT", [f"{i + 1}. {row[1]} — {row[2]}" for i, row in enumerate(SUPPORTS)] + ["Number selects; Escape cancels."]
    if kind == "objective":
        alter = "available" if state.loadout == "tools" or state.support == "charts" or (state.courier and state.courier.technique == "lever craft") or state.contact.disposition >= 2 else "needs tools, charts, lever craft, or trust"
        return state.contact.name.upper(), [state.region.pressure, state.region.objective_text, "A. Accept the cargo recovery", "R. Refuse", f"T. Alter to flood-control repair ({alter})", "Escape cancels without time."]
    if kind == "quit":
        return "QUIT JOMON?", ["Press Y to quit. Press N or Escape to continue."]
    return "INFORMATION", [kind, "Escape closes this inspection without advancing time."]


def _handle_overlay(state: GameState, kind: str, key: int) -> tuple[str | None, bool]:
    if key == 27:
        return None, False
    char = chr(key).lower() if 0 <= key < 256 else ""
    if kind in {"help", "inventory", "info"}:
        return None, False
    if kind == "quit":
        if char == "y":
            return None, True
        if char == "n":
            return None, False
        return kind, False
    if kind == "courier" and char.isdigit():
        living = [person for person in state.household if person.alive]
        index = int(char) - 1
        if 0 <= index < len(living):
            choose_courier(state, living[index].id)
            return None, False
    if kind == "loadout" and char in "123":
        choose_loadout(state, LOADOUTS[int(char) - 1][0])
        return None, False
    if kind == "support" and char in "123":
        choose_support(state, SUPPORTS[int(char) - 1][0])
        return None, False
    if kind == "objective" and char in {"a", "r", "t"}:
        decision = {"a": "accept", "r": "refuse", "t": "alter"}[char]
        result = decide_objective(state, decision)
        return (None if result.changed else kind), False
    return kind, False


MOVES = {
    ord("h"): (-1, 0), ord("j"): (0, 1), ord("k"): (0, -1), ord("l"): (1, 0),
    ord("y"): (-1, -1), ord("u"): (1, -1), ord("b"): (-1, 1), ord("n"): (1, 1),
    curses.KEY_LEFT: (-1, 0), curses.KEY_DOWN: (0, 1), curses.KEY_UP: (0, -1), curses.KEY_RIGHT: (1, 0),
}


def play(screen: curses.window, state: GameState) -> GameState:
    curses.curs_set(0)
    screen.keypad(True)
    _init_colours()
    overlay: str | None = None
    while True:
        _draw_base(screen, state)
        height, width = screen.getmaxyx()
        if overlay and height >= MIN_HEIGHT and width >= MIN_WIDTH:
            title, lines = _overlay_lines(state, overlay)
            _overlay(screen, title, lines)
        key = screen.getch()
        if key == curses.KEY_RESIZE:
            continue
        if height < MIN_HEIGHT or width < MIN_WIDTH:
            if key in {ord("q"), ord("Q")}:
                return state
            continue
        if overlay:
            overlay, should_quit = _handle_overlay(state, overlay, key)
            if should_quit:
                return state
            continue
        normalized = ord(chr(key).lower()) if 0 <= key < 256 else key
        if normalized in MOVES:
            move(state, *MOVES[normalized])
        elif normalized in {10, 13, ord("e")}:
            result = interact(state)
            overlay = result.overlay
        elif normalized == ord("a"):
            attack(state)
        elif normalized == ord("g"):
            guard(state)
        elif normalized == ord("x"):
            use_gear(state)
        elif normalized == ord("v"):
            negotiate(state)
        elif normalized == ord("r"):
            retreat(state)
        elif normalized == ord("i"):
            overlay = "inventory"
        elif normalized == ord("?"):
            overlay = "help"
        elif normalized == ord("s"):
            if state.location != "jomon":
                state.add_message("Save is available aboard Jomon, outside immediate danger.")
            else:
                try:
                    path = save_game(state)
                    state.add_message(f"Saved to {path}.")
                except SaveError as exc:
                    state.add_message(str(exc))
        elif normalized == ord("q"):
            overlay = "quit"
