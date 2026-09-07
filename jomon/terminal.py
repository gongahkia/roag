"""Fullscreen curses presentation; gameplay remains in direct action functions."""

from __future__ import annotations

import curses
from dataclasses import dataclass
from typing import Iterable

from .actions import (
    _advance_world,
    attack,
    choose_courier,
    choose_gear,
    choose_passive,
    choose_relic,
    choose_support,
    choose_weapon,
    decide_objective,
    guard,
    interact,
    move,
    negotiate,
    purchase_merchant_item,
    purchase_bar_drink,
    recruit_person,
    defer_recruit,
    intervene_socially,
    retreat,
    use_gear,
)
from .inventory import (
    BODY_SLOTS,
    InventoryTransaction,
    auto_pack,
    auto_place,
    can_place,
    drop_item,
    equip_item,
    equipped_item,
    grid_items,
    grid_size,
    item_spec,
    item_preview,
    load_state,
    occupied_cells,
    pack_weight,
    pin_item,
    placement_preview,
    place_item,
    record_acquisition,
    rotate_item,
    sync_legacy_load,
    transfer_to_grid,
    unequip_item,
    weight_capacity,
)
from .content import (
    COMMODITIES,
    GEAR,
    HELP_LINES,
    MERCHANT_ITEMS,
    PASSIVES,
    RELICS,
    SUPPORTS,
    WEAPONS,
)
from .save import SaveError, save_game
from .state import GameState, Position, Threat
from .travel import choose_destination, resolve_voyage, travel_animation_frames
from .route_chart import chart_move, neighbours, route_availability, route_preview
from .calendar import calendar_at
from .vessel import DRINKS, current_area
from .world import (
    area_name,
    build_combinations,
    camera_origin,
    capacity,
    carried_bulk,
    displayed_tile,
    field_of_view,
    map_rows,
    passive_bulk,
    passive_capacity,
    pressure,
    remembered,
    vertical_destination,
)

MIN_WIDTH = 80
MIN_HEIGHT = 24

SEMANTIC_ROLES = (
    "player", "ally", "neutral", "hostile", "elite", "terrain", "water",
    "structure", "exit", "cargo", "interactable", "hazard", "mystical",
)


@dataclass(frozen=True)
class ColourStyle:
    pair: int
    foreground: int | None
    bold: bool


@dataclass
class InventoryView:
    transaction: InventoryTransaction
    source: str | None = None
    pane: str = "pack"
    cursor_x: int = 0
    cursor_y: int = 0
    held_id: str | None = None
    held_rotated: bool = False
    selected_ids: set[str] | None = None
    pending_drop: bool = False
    status: str = ""
    paper_slot: int = 0
    grid_origin: tuple[int, int] = (2, 3)
    paper_screen: dict[str, tuple[int, int]] | None = None

    @classmethod
    def begin(cls, state: GameState, source: str | None = None) -> "InventoryView":
        return cls(InventoryTransaction.begin(state), source=source, selected_ids=set())


@dataclass
class RouteChartView:
    cursor: str
    overlay_mode: int = 0
    confirming: bool = False
    node_screen: dict[str, tuple[int, int]] | None = None

    @classmethod
    def begin(cls, state: GameState) -> "RouteChartView":
        return cls(state.route_current_node, node_screen={})


@dataclass(frozen=True)
class InputEvent:
    kind: str
    key: int = -1
    x: int = -1
    y: int = -1
    button: str = ""
    shift: bool = False
    double: bool = False


def normalise_input(key: int, mouse_reader=None) -> InputEvent:
    """Translate optional curses mouse bits without making mouse mandatory."""
    if key != getattr(curses, "KEY_MOUSE", -999):
        return InputEvent("key", key=key)
    reader = mouse_reader or curses.getmouse
    try:
        _, x, y, _, state = reader()
    except (curses.error, TypeError, ValueError):
        return InputEvent("unsupported-mouse", key=key)
    shift = bool(state & getattr(curses, "BUTTON_SHIFT", 0))
    mapping = (
        ("left", "BUTTON1_CLICKED", False), ("left", "BUTTON1_PRESSED", False),
        ("left", "BUTTON1_DOUBLE_CLICKED", True), ("right", "BUTTON3_CLICKED", False),
        ("wheel-up", "BUTTON4_PRESSED", False), ("wheel-down", "BUTTON5_PRESSED", False),
    )
    for button, constant, double in mapping:
        if state & getattr(curses, constant, 0):
            return InputEvent("mouse", x=x, y=y, button=button, shift=shift, double=double)
    return InputEvent("mouse", x=x, y=y, button="motion", shift=shift)


def _enable_mouse() -> bool:
    try:
        mask = curses.ALL_MOUSE_EVENTS | getattr(curses, "REPORT_MOUSE_POSITION", 0)
        available, _ = curses.mousemask(mask)
        curses.mouseinterval(180)
        return bool(available)
    except curses.error:
        return False


def semantic_colour_plan(colour_count: int, pair_count: int) -> dict[str, ColourStyle]:
    """Return a safe semantic plan without requiring curses initialization."""
    desired = {
        "player": (3, True), "ally": (6, True), "neutral": (2, False),
        "hostile": (1, True), "elite": (5, True), "terrain": (7, False),
        "water": (4, False), "structure": (7, False), "exit": (6, True),
        "cargo": (3, True), "interactable": (2, True), "hazard": (1, True),
        "mystical": (5, True),
    }
    if colour_count < 8 or pair_count <= 1:
        return {role: ColourStyle(0, None, bold) for role, (_, bold) in desired.items()}
    priority = (1, 3, 6, 2, 5, 4, 7)
    available = priority[: max(0, min(len(priority), pair_count - 1))]
    pairs = {foreground: index + 1 for index, foreground in enumerate(available)}
    return {
        role: ColourStyle(pairs.get(foreground, 0), foreground if foreground in pairs else None, bold)
        for role, (foreground, bold) in desired.items()
    }


def semantic_role(glyph: str, *, aboard: bool = False) -> str:
    if glyph == "@":
        return "player"
    if glyph == "a" or (aboard and glyph in {"T", "b"}):
        return "ally"
    if aboard and glyph in {"v", "B"}:
        return "neutral"
    if aboard and glyph in {"=", "t", "_", "F", "f"}:
        return "structure"
    if aboard and glyph in {"s", "G", "K", "N", "O", "S", "W"}:
        return "interactable"
    if glyph in {"M", "c", "$"}:
        return "neutral"
    if glyph in {"h", "g", "x", "b", "!"}:
        return "hostile"
    if glyph == "X":
        return "elite"
    if glyph in {"~", "w", ","}:
        return "water"
    if glyph == "#":
        return "structure"
    if glyph in {"<", ">", "^", "v", "+"}:
        return "exit"
    if glyph == "R":
        return "cargo"
    if glyph in {"&", "D", "O", "o", "?", "C", "L", "P", "H", "s"}:
        return "interactable"
    if glyph in {"m", "%", "r", "q", "t", ":", "s"}:
        return "hazard"
    if glyph == "*":
        return "mystical"
    return "terrain"


_COLOUR_ATTRIBUTES: dict[str, int] = {role: 0 for role in SEMANTIC_ROLES}


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
    global _COLOUR_ATTRIBUTES
    try:
        if not curses.has_colors():
            _COLOUR_ATTRIBUTES = {role: curses.A_BOLD if role in {"player", "hostile", "elite", "exit", "hazard"} else 0 for role in SEMANTIC_ROLES}
            return
        curses.start_color()
        curses.use_default_colors()
        plan = semantic_colour_plan(getattr(curses, "COLORS", 0), getattr(curses, "COLOR_PAIRS", 0))
        initialized: set[int] = set()
        for style in plan.values():
            if style.pair and style.pair not in initialized and style.foreground is not None:
                curses.init_pair(style.pair, style.foreground, -1)
                initialized.add(style.pair)
        _COLOUR_ATTRIBUTES = {
            role: (curses.color_pair(style.pair) if style.pair else 0) | (curses.A_BOLD if style.bold else 0)
            for role, style in plan.items()
        }
    except curses.error:
        _COLOUR_ATTRIBUTES = {role: 0 for role in SEMANTIC_ROLES}


def _threat_glyph(threat: Threat) -> str:
    if threat.elite:
        return "X"
    return {"pursuer": "h", "reach": "g", "ranged": "x", "animal": "b", "machinery": "!"}[threat.profile]


def visible_threats(
    state: GameState, visible: set[Position] | None = None
) -> dict[Position, Threat]:
    """Return only current, presently seen actors; memory never carries actors."""
    if state.location != "region":
        return {}
    visible = visible if visible is not None else field_of_view(state, remember=False)
    return {
        threat.position: threat
        for threat in state.threats
        if threat.status in {"watching", "engaged"}
        and threat.position.z == state.position.z
        and threat.position in visible
    }


def _draw_map(screen: curses.window, state: GameState, top: int, left: int, height: int, width: int) -> None:
    rows = map_rows(state)
    view_height, view_width = height - 2, width - 2
    map_height, map_width = len(rows), max(map(len, rows))
    origin_x, origin_y = camera_origin(
        state.position, map_width, map_height, view_width, view_height
    )
    visible = field_of_view(state, remember=False) if state.location == "region" else set()
    threats = visible_threats(state, visible)
    for sy in range(view_height):
        world_y = origin_y + sy
        if world_y >= len(rows):
            break
        for sx in range(view_width):
            world_x = origin_x + sx
            if world_x >= len(rows[world_y]):
                break
            position = Position(world_x, world_y, state.position.z)
            if (
                state.location == "region"
                and position not in visible
                and not remembered(state, position)
            ):
                _put(screen, top + 1 + sy, left + 1 + sx, " ")
                continue
            if position == state.position:
                char = "@"
            elif position in threats:
                char = _threat_glyph(threats[position])
            else:
                char = displayed_tile(state, position)
            role = semantic_role(char, aboard=state.location == "jomon")
            attr = _COLOUR_ATTRIBUTES[role]
            if state.location == "region" and position not in visible:
                attr = curses.A_DIM
            _put(screen, top + 1 + sy, left + 1 + sx, char, attr)


def _status_lines(state: GameState) -> list[str]:
    courier, p = state.courier, pressure(state)
    if courier:
        identity, health, injury = f"{courier.name}, {courier.role}", f"{courier.health}/{courier.max_health}", courier.injury
        technique = ", ".join([courier.technique, *courier.learned_techniques])
    else:
        identity, health, injury, technique = "not chosen", "-", "-", "-"
    market = state.market[state.region.objective_commodity]
    visible = field_of_view(state, remember=False) if state.location == "region" else set()
    local = [
        threat for threat in state.threats
        if threat.status in {"watching", "engaged"} and threat.position in visible
    ]
    threat = local[0].intent if local else "no visible threat"
    transition = vertical_destination(state, state.position) if state.location == "region" else None
    level_text = f"{state.position.x},{state.position.y} z{state.position.z:+d}"
    if transition:
        level_text += " " + ("v below" if transition.z < state.position.z else "^ above")
    lines = [
        "COURIER",
        identity,
        f"Health {health}; {injury}",
        f"{state.weapon or '-'} / {state.gear or '-'}",
        f"Technique: {_clip(technique, 16)}",
        f"Date {calendar_at(state).season} {calendar_at(state).day}; {calendar_at(state).time_of_day}",
        "PRESSURE",
        f"Time {p.elapsed}; depth {p.depth}",
        f"Noise {p.noise}; value {p.valuables}",
        f"{p.band} {p.score}; {state.weather}",
        state.active_region_id.upper() if state.location == "region" else _clip(state.route_nodes.get(state.route_current_node).name if state.route_nodes else state.active_region_id, 25),
        f"{level_text}; {state.objective_status}",
        f"{state.region.objective_commodity}: {market.stock}/{market.demand}",
        f"Load {pack_weight(state)}/{weight_capacity(state)} {load_state(state)}",
        f"Ammo {state.ammunition}; oil {state.lamp_oil}",
        f"Rope {state.rope_uses}; smoke {state.smoke_charges}",
        _clip(threat, 25),
    ]
    if build_combinations(state):
        lines.append(f"Combo: {_clip(build_combinations(state)[0], 20)}")
    if state.terrain_statuses:
        lines.append("Status: " + _clip(", ".join(state.terrain_statuses), 17))
    return lines


def _draw_base(screen: curses.window, state: GameState) -> None:
    screen.erase()
    height, width = screen.getmaxyx()
    if height < MIN_HEIGHT or width < MIN_WIDTH:
        _put(screen, max(0, height // 2 - 1), 1, f"Jomon needs at least {MIN_WIDTH}x{MIN_HEIGHT} terminal cells.", curses.A_BOLD)
        _put(screen, max(0, height // 2), 1, f"Current size: {width}x{height}. Resize or press Q to quit.")
        screen.refresh()
        return
    status_width, event_height, command_height = 29, 6, 2
    main_height, map_width = height - event_height - command_height, width - status_width
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
    _put(screen, height - 2, 1, "Move HJKL/YUBN/arrows  E interact  A attack  G guard/reload  X gear", curses.A_REVERSE)
    _put(screen, height - 1, 1, "V negotiate  R retreat  I inventory  S save aboard  ? help  Q quit", curses.A_REVERSE)
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


def _chart_screen_point(node_x: int, node_y: int, map_width: int, height: int) -> tuple[int, int]:
    usable_width, usable_height = max(12, map_width - 5), max(8, height - 7)
    return 2 + round(node_x / 78 * usable_width), 2 + round(node_y / 20 * usable_height)


def _chart_line(first: tuple[int, int], second: tuple[int, int]) -> list[tuple[int, int]]:
    x0, y0 = first
    x1, y1 = second
    dx, dy = abs(x1 - x0), -abs(y1 - y0)
    sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
    error, points = dx + dy, []
    while True:
        points.append((x0, y0))
        if (x0, y0) == (x1, y1):
            return points
        twice = 2 * error
        if twice >= dy:
            error += dy
            x0 += sx
        if twice <= dx:
            error += dx
            y0 += sy


def _draw_route_chart(
    screen: curses.window,
    state: GameState,
    view: RouteChartView,
    *,
    moving: tuple[int, int, str] | None = None,
) -> None:
    height, width = screen.getmaxyx()
    screen.erase()
    detail_width = max(27, min(36, width // 3))
    map_width = width - detail_width
    _frame(screen, 0, 0, height - 2, map_width, "JOMON ROUTE CHART")
    _frame(screen, 0, map_width, height - 2, detail_width, "SELECTED ROUTE")
    view.node_screen = {}
    for edge in state.route_edges:
        first = _chart_screen_point(state.route_nodes[edge.first].x, state.route_nodes[edge.first].y, map_width, height)
        second = _chart_screen_point(state.route_nodes[edge.second].x, state.route_nodes[edge.second].y, map_width, height)
        known = edge.first in state.route_known or edge.second in state.route_known
        attr = _COLOUR_ATTRIBUTES["exit"] if edge.id in state.traversed_route_edges else curses.A_DIM
        if not known:
            attr = curses.A_DIM
        for x, y in _chart_line(first, second)[1:-1]:
            glyph = "-" if first[1] == second[1] else "|" if first[0] == second[0] else "."
            _put(screen, y, x, glyph, attr)
    glyphs = {
        "region": "R", "anchorage": "A", "market": "$", "resupply": "+",
        "hazard": "!", "warning": "!", "unknown": "?",
    }
    reachable = set(neighbours(state, state.route_current_node, reachable_only=True))
    for node_id, node in state.route_nodes.items():
        x, y = _chart_screen_point(node.x, node.y, map_width, height)
        view.node_screen[node_id] = (x, y)
        known = node_id in state.route_known or node.known
        glyph = "@" if node_id == state.route_current_node else glyphs.get(node.kind, "o") if known else "?"
        role = "player" if glyph == "@" else "hazard" if glyph == "!" else "cargo" if glyph == "$" else "exit"
        attr = _COLOUR_ATTRIBUTES[role]
        if node_id in reachable:
            attr |= curses.A_BOLD
        if node_id == view.cursor:
            attr |= curses.A_REVERSE
        _put(screen, y, x, glyph, attr)
        if node.region_id and x + 2 < map_width - 1:
            _put(screen, y, x + 2, _clip(node.name, 10), curses.A_BOLD if known else curses.A_DIM)
    if moving:
        mx, my = _chart_screen_point(moving[0], moving[1], map_width, height)
        _put(screen, my, mx, "@", _COLOUR_ATTRIBUTES["player"] | curses.A_REVERSE)
        _put(screen, height - 4, 2, _clip(moving[2], map_width - 4), curses.A_BOLD)
    selected = state.route_nodes[view.cursor]
    lines = [
        selected.name.upper(), f"Type: {selected.kind}",
        *route_preview(state, view.cursor), "",
        f"Calendar: {calendar_at(state).label}",
        f"Jomon integrity: {state.vessel_integrity}/10",
        f"Chart layer {view.overlay_mode + 1}: " + ("route and risk" if view.overlay_mode == 0 else "markets and supplies" if view.overlay_mode == 1 else "season and contacts"),
    ]
    if view.confirming:
        available, reason = route_availability(state, view.cursor)
        lines += ["", f"> ENTER — {'CONFIRM LEG' if available else 'BLOCKED'}", "  ESC — cancel confirmation", reason]
    for index, line in enumerate(lines[: height - 5]):
        attr = curses.A_BOLD if index == 0 or line.startswith(">") else curses.A_DIM if "BLOCKED" in line else 0
        _put(screen, 2 + index, map_width + 2, _clip(line, detail_width - 4), attr)
    _put(screen, height - 2, 1, "Arrows/WASD/HJKL connected node  Enter preview/confirm  Tab layer  Esc close", curses.A_REVERSE)
    _put(screen, height - 1, 1, "Mouse: click selects, double-click confirms where reported; keyboard is complete", curses.A_REVERSE)
    screen.refresh()


def _route_mouse_node(view: RouteChartView, event: InputEvent) -> str | None:
    points = view.node_screen or {}
    candidates = [
        (abs(x - event.x) + abs(y - event.y), node_id)
        for node_id, (x, y) in points.items()
        if abs(x - event.x) <= 1 and abs(y - event.y) <= 1
    ]
    return min(candidates)[1] if candidates else None


def _handle_route_chart(
    state: GameState,
    view: RouteChartView,
    event: InputEvent,
) -> tuple[bool, str | None, tuple[str, str] | None]:
    if event.kind == "mouse":
        node_id = _route_mouse_node(view, event)
        if event.button == "left" and node_id:
            view.cursor = node_id
            view.confirming = event.double
            if not event.double:
                return False, None, None
        else:
            return False, None, None
        key = 10 if event.double else -1
    else:
        key = event.key
    if key == 27:
        if view.confirming:
            view.confirming = False
            return False, None, None
        return True, None, None
    if key == 9:
        view.overlay_mode = (view.overlay_mode + 1) % 3
        return False, None, None
    movement = {
        curses.KEY_LEFT: (-1, 0), curses.KEY_RIGHT: (1, 0),
        curses.KEY_UP: (0, -1), curses.KEY_DOWN: (0, 1),
        ord("a"): (-1, 0), ord("d"): (1, 0), ord("w"): (0, -1), ord("s"): (0, 1),
        ord("h"): (-1, 0), ord("l"): (1, 0), ord("k"): (0, -1), ord("j"): (0, 1),
    }
    normalized = ord(chr(key).lower()) if 0 <= key < 256 else key
    if normalized in movement:
        view.cursor = chart_move(state, view.cursor, *movement[normalized])
        view.confirming = False
        return False, None, None
    if key in {10, 13}:
        if view.cursor == state.route_current_node:
            return False, None, None
        available, reason = route_availability(state, view.cursor)
        if not available:
            state.add_message(reason, priority=2)
            view.confirming = False
            return False, None, None
        if not view.confirming:
            view.confirming = True
            return False, None, None
        origin, destination = state.route_current_node, view.cursor
        changed, message = choose_destination(state, destination)
        if not changed:
            state.add_message(message, priority=2)
            return False, None, None
        return True, "voyage" if state.voyage_status == "active" else None, (origin, destination)
    return False, None, None


def _animate_route(screen: curses.window, state: GameState, origin: str, destination: str) -> None:
    frames = travel_animation_frames(state, origin, destination, interrupted=state.voyage_status == "active")
    view = RouteChartView(destination)
    try:
        screen.timeout(120)
        for frame in frames:
            height, width = screen.getmaxyx()
            if height < MIN_HEIGHT or width < MIN_WIDTH:
                _draw_base(screen, state)
            else:
                _draw_route_chart(screen, state, view, moving=(frame.x, frame.y, frame.text))
            key = screen.getch()
            if key in {27, ord(" "), 10, 13, ord("s"), ord("S"), ord("q"), ord("Q")}:
                break
            if key == curses.KEY_RESIZE:
                continue
    finally:
        screen.timeout(-1)


def _inventory_panes(state: GameState, view: InventoryView) -> list[str]:
    if state.location == "jomon":
        return ["pack", "locker"]
    return ["pack", view.source or "ground"]


def _inventory_items(state: GameState, view: InventoryView) -> list:
    if view.pane == "pack":
        return grid_items(state, "pack", owner_id=state.active_courier_id)
    if view.pane == "locker":
        return grid_items(state, "locker")
    if view.pane.startswith("container:"):
        container_id = view.pane.split(":", 1)[1]
        return [item for item in state.items if item.location == "container" and item.container_id == container_id]
    if view.pane == "ground":
        return [
            item for item in state.items
            if item.location == "ground"
            and item.region_id == state.active_region_id
            and item.ground_position == state.position
        ]
    return []


def _inventory_item_at(state: GameState, view: InventoryView):
    items = _inventory_items(state, view)
    if view.pane in {"pack", "locker"}:
        return next((item for item in reversed(items) if (view.cursor_x, view.cursor_y) in occupied_cells(item)), None)
    return items[view.cursor_y] if 0 <= view.cursor_y < len(items) else None


def _item_colour(kind: str) -> int:
    category = item_spec(kind).category
    role = {
        "weapon": "hostile", "armour": "structure", "gear": "interactable",
        "passive": "cargo", "consumable": "neutral", "cargo": "cargo",
        "relic": "mystical",
    }.get(category, "terrain")
    return _COLOUR_ATTRIBUTES[role]


PAPER_SLOTS = ("readied", "secondary", *BODY_SLOTS)


def paper_doll_layout(state: GameState) -> tuple[str, ...]:
    courier = state.courier

    def mark(slot: str) -> str:
        item = equipped_item(state, slot)
        abbreviation = item_spec(item.kind).abbreviation if item else "--"
        injury = "!" if courier and slot in courier.injuries else " "
        return f"{abbreviation[:2]}{injury}"

    return (
        f"             +[{mark('head')}]+",
        "                |",
        f"       [{mark('arms')}]--[{mark('torso')}]--[{mark('arms')}]",
        f"       [{mark('hands')}]    |    [{mark('hands')}]",
        "                |",
        f"             +[{mark('legs')}]+",
        f"             / [{mark('feet')}] \\",
        f" W [{mark('readied')}]  T [{mark('secondary')}]  P [PACK]",
    )


def _paper_selected_item(state: GameState, view: InventoryView):
    return equipped_item(state, PAPER_SLOTS[view.paper_slot % len(PAPER_SLOTS)])


def _draw_inventory(screen: curses.window, state: GameState, view: InventoryView) -> None:
    height, width = screen.getmaxyx()
    screen.erase()
    _frame(screen, 0, 0, height - 2, width, "SPATIAL INVENTORY")
    pane_name = view.pane.split(":", 1)[0].upper()
    mode = "AUTO-PLACE ON" if state.auto_place_enabled else "AUTO-PLACE OFF"
    _put(screen, 1, 2, f"{pane_name}  Tab pane  {mode}", curses.A_BOLD)
    selected = _inventory_item_at(state, view)
    if view.held_id:
        selected = next(item for item in state.items if item.id == view.held_id)

    if view.pane in {"pack", "locker"}:
        grid_width, grid_height = grid_size(state, view.pane)
        origin_x, origin_y = 2, 3
        view.grid_origin = origin_x, origin_y
        _frame(screen, origin_y - 1, origin_x - 1, grid_height + 2, grid_width * 2 + 2, f"{grid_width}x{grid_height}")
        cell_items: dict[tuple[int, int], object] = {}
        for item in _inventory_items(state, view):
            for cell in occupied_cells(item):
                cell_items[cell] = item
        for y in range(grid_height):
            for x in range(grid_width):
                item = cell_items.get((x, y))
                text = ".."
                attr = curses.A_DIM
                if item:
                    text = item_spec(item.kind).abbreviation if (x, y) == (item.x, item.y) else "[]"
                    if item.pinned and (x, y) == (item.x, item.y):
                        text = text[:1] + "*"
                    attr = _item_colour(item.kind)
                    if view.selected_ids and item.id in view.selected_ids:
                        attr |= curses.A_BOLD | curses.A_UNDERLINE
                if view.held_id:
                    held = next(item for item in state.items if item.id == view.held_id)
                    owner = state.active_courier_id if view.pane == "pack" else None
                    preview = placement_preview(
                        state, held, view.pane, view.cursor_x, view.cursor_y,
                        rotated=view.held_rotated, owner_id=owner,
                    )
                    if (x, y) in preview.cells:
                        text = "::"
                        attr = (_COLOUR_ATTRIBUTES["exit"] if preview.valid else _COLOUR_ATTRIBUTES["hazard"]) | curses.A_REVERSE
                    if (x, y) in set().union(*(occupied_cells(other) for other in _inventory_items(state, view) if other.id in preview.blockers)):
                        attr |= curses.A_BOLD | _COLOUR_ATTRIBUTES["hazard"]
                if (x, y) == (view.cursor_x, view.cursor_y):
                    attr |= curses.A_REVERSE
                _put(screen, origin_y + y, origin_x + x * 2, text[:2].ljust(2), attr)
    else:
        _put(screen, 3, 2, "Items at this physical source:", curses.A_BOLD)
        rows = _inventory_items(state, view)
        for index, item in enumerate(rows[:10]):
            attr = _item_colour(item.kind) | (curses.A_REVERSE if index == view.cursor_y else 0)
            _put(screen, 5 + index, 3, _clip(f"{item_spec(item.kind).abbreviation} {item_spec(item.kind).name} x{item.quantity}", 34), attr)
        if not rows:
            _put(screen, 5, 3, "(empty)", curses.A_DIM)

    detail_x = min(max(27, width // 2), width - 34)
    _put(screen, 2, detail_x, "PAPER DOLL  [ / ] selects slot", curses.A_BOLD)
    view.paper_screen = {}
    for index, line in enumerate(paper_doll_layout(state)):
        _put(screen, 3 + index, detail_x, _clip(line, width - detail_x - 2))
    # Mouse hit rows are deliberately broad; exact limb art is presentation.
    for index, slot in enumerate(PAPER_SLOTS):
        view.paper_screen[slot] = (detail_x, 3 + min(index, 7))
    burden = load_state(state)
    _put(screen, 11, detail_x, f"Weight {pack_weight(state)}/{weight_capacity(state)} — {burden}", curses.A_BOLD)
    from .inventory import LOAD_EFFECTS

    for index, line in enumerate(_wrapped(LOAD_EFFECTS[burden], max(20, width - detail_x - 2))[:2]):
        _put(screen, 12 + index, detail_x, line)
    if not selected:
        selected = _paper_selected_item(state, view)
    if selected:
        spec = item_spec(selected.kind)
        held = "HELD — " if view.held_id else ""
        actual_width, actual_height = (spec.height, spec.width) if view.held_id and view.held_rotated else (spec.width, spec.height)
        _put(screen, 14, detail_x, _clip(f"{held}{spec.name} {actual_width}x{actual_height} wt {spec.weight}", width - detail_x - 2), curses.A_BOLD)
        for index, art in enumerate(item_preview(selected.kind)):
            _put(screen, 15 + index, detail_x, _clip(art, width - detail_x - 2), _item_colour(selected.kind))
        for index, line in enumerate(_wrapped(spec.description, max(20, width - detail_x - 2))[:3]):
            _put(screen, 18 + index, detail_x, line)
        _put(screen, min(height - 4, 21), detail_x, _clip(f"Condition {selected.condition}; {'PINNED; ' if selected.pinned else ''}{selected.provenance}", width - detail_x - 2))
    if view.held_id and view.pane in {"pack", "locker"}:
        held_item = next(item for item in state.items if item.id == view.held_id)
        owner = state.active_courier_id if view.pane == "pack" else None
        preview = placement_preview(state, held_item, view.pane, view.cursor_x, view.cursor_y, rotated=view.held_rotated, owner_id=owner)
        view.status = f"GHOST {preview.reason}; load {preview.resulting_weight}/{weight_capacity(state)} {preview.resulting_load}"
    count = len(view.selected_ids or ())
    if count:
        marked = [item for item in state.items if item.id in (view.selected_ids or set())]
        total = sum(item_spec(item.kind).weight * item.quantity for item in marked)
        cells = sum(len(occupied_cells(item)) for item in marked)
        view.status = f"MARKED {count}; weight {total}; cells {cells}"
    if view.pending_drop:
        view.status = "CONFIRM DROP: Y drops marked/current physical items; N/Esc cancels"
    _put(screen, height - 3, 2, _clip(view.status, width - 4), curses.A_BOLD)
    _put(screen, height - 2, 1, "Move arrows/WASD  Enter lift/place  R rotate  Space mark  * all  T transfer  E equip", curses.A_REVERSE)
    _put(screen, height - 1, 1, "O auto-pack  P pin  Z auto-place  [ ] body  D drop(confirm)  C commit  Esc cancel", curses.A_REVERSE)
    screen.refresh()


def _clamp_inventory_cursor(state: GameState, view: InventoryView) -> None:
    if view.pane in {"pack", "locker"}:
        width, height = grid_size(state, view.pane)
        view.cursor_x = max(0, min(width - 1, view.cursor_x))
        view.cursor_y = max(0, min(height - 1, view.cursor_y))
    else:
        view.cursor_x = 0
        view.cursor_y = max(0, min(max(0, len(_inventory_items(state, view)) - 1), view.cursor_y))


def _handle_inventory(state: GameState, view: InventoryView, key: int) -> tuple[bool, bool]:
    """Return (closed, committed); Escape restores the complete opening state."""
    char = chr(key).lower() if 0 <= key < 256 else ""
    movement = {
        curses.KEY_LEFT: (-1, 0), curses.KEY_RIGHT: (1, 0),
        curses.KEY_UP: (0, -1), curses.KEY_DOWN: (0, 1),
        ord("a"): (-1, 0), ord("d"): (1, 0), ord("w"): (0, -1), ord("s"): (0, 1),
    }
    normalized = ord(char) if char else key
    if key == 27:
        view.transaction.cancel(state)
        return True, False
    if key == 9:
        panes = _inventory_panes(state, view)
        view.pane = panes[(panes.index(view.pane) + 1) % len(panes)]
        view.cursor_x = view.cursor_y = 0
        return False, False
    if key == ord("D"):
        item = _inventory_item_at(state, view)
        if item and item.location == "pack" and drop_item(state, item.id):
            view.transaction.changed = True
            sync_legacy_load(state)
        return False, False
    if normalized in movement:
        dx, dy = movement[normalized]
        view.cursor_x += dx
        view.cursor_y += dy
        _clamp_inventory_cursor(state, view)
        return False, False
    if char == "c":
        if view.held_id:
            state.add_message("Place the held item before confirming the repack.")
            return False, False
        sync_legacy_load(state)
        return True, True
    if char == "r":
        if view.held_id:
            item = next(item for item in state.items if item.id == view.held_id)
            item.rotated = not item.rotated
            view.transaction.changed = True
        else:
            item = _inventory_item_at(state, view)
            if item and rotate_item(state, item.id):
                view.transaction.changed = True
        return False, False
    if key in {10, 13}:
        if view.held_id:
            if view.pane not in {"pack", "locker"}:
                return False, False
            owner = state.active_courier_id if view.pane == "pack" else None
            item = next(item for item in state.items if item.id == view.held_id)
            if place_item(state, item.id, view.pane, view.cursor_x, view.cursor_y, rotated=item.rotated, owner_id=owner):
                view.held_id = None
                view.transaction.changed = True
            return False, False
        item = _inventory_item_at(state, view)
        if item and view.pane in {"pack", "locker"}:
            item.location = "held"
            view.held_id = item.id
        return False, False
    item = _inventory_item_at(state, view)
    if char == "e" and item and item.location == "pack" and equip_item(state, item.id):
        view.transaction.changed = True
        sync_legacy_load(state)
        return False, False
    if char in "123456" and unequip_item(state, BODY_SLOTS[int(char) - 1]):
        view.transaction.changed = True
        sync_legacy_load(state)
        return False, False
    if char == "t" and item:
        moved = False
        source_container_id = item.container_id
        if view.pane == "pack" and state.location == "jomon":
            moved = transfer_to_grid(state, item.id, "locker")
        elif view.pane == "locker":
            moved = transfer_to_grid(state, item.id, "pack", owner_id=state.active_courier_id)
        elif view.pane == "pack" and state.location == "region":
            moved = drop_item(state, item.id)
        elif view.pane.startswith("container:") or view.pane == "ground":
            moved = transfer_to_grid(state, item.id, "pack", owner_id=state.active_courier_id)
            if moved:
                if source_container_id:
                    container = next((box for box in state.region.containers if box.id == source_container_id), None)
                    if container and item.id in container.item_ids:
                        container.item_ids.remove(item.id)
                record_acquisition(state, item)
        if moved:
            view.transaction.changed = True
            sync_legacy_load(state)
        _clamp_inventory_cursor(state, view)
        return False, False
    if char == "d" and item and item.location == "pack" and drop_item(state, item.id):
        view.transaction.changed = True
        sync_legacy_load(state)
    return False, False


def _tavern_lines(state: GameState) -> list[str]:
    courier = state.courier
    identity = f"{courier.name}, {courier.role}; health {courier.health}/{courier.max_health}; {courier.injury}; {courier.technique}" if courier else "none selected"
    goods = ", ".join(f"{name} {stack.quantity}" for name, stack in state.carried_goods.items()) or "none"
    combos = ", ".join(build_combinations(state)) or "none active"
    passives = ", ".join(state.carried_passives) or "none"
    return [
        f"Active courier: {identity}",
        "Speak directly to a visible adventurer to switch or recruit.",
        f"S Crew support: {state.support or 'none'}",
        f"Readied: {state.weapon or 'none'} / {state.gear or 'none'} / {state.carried_relic or 'no relic'}",
        f"Discoveries: {passives} ({passive_bulk(state)}/{passive_capacity(state)} legacy bulk)",
        f"Build interactions: {combos}", f"Cargo: {goods}; capacity {carried_bulk(state)}/{capacity(state)}",
        "", state.region.condition, state.region.objective_text, f"Objective: {state.objective_status}",
        "Use I for the pack, body slots, and Jomon locker.",
        "Enter confirms and closes. Escape cancels without advancing time.",
    ]


def _overlay_lines(state: GameState, kind: str) -> tuple[str, list[str]]:
    if kind == "help":
        return "HELP", list(HELP_LINES)
    if kind == "inventory":
        goods = [f"{name}: {stack.quantity}, {stack.condition} ({COMMODITIES[name]['bulk']} bulk each)" for name, stack in state.carried_goods.items()]
        return "INVENTORY", [
            f"Capacity: {carried_bulk(state)}/{capacity(state)} bulk",
            f"Weapon: {state.weapon or 'none'}; gear: {state.gear or 'none'}; relic: {state.carried_relic or 'none'}",
            f"Passive discoveries: {state.carried_passives or 'none'} ({passive_bulk(state)}/{passive_capacity(state)} bulk)",
            f"Finite supplies: ammunition {state.ammunition}; oil {state.lamp_oil}; rope {state.rope_uses}; smoke {state.smoke_charges}",
            "Goods:", *(goods or ["none"]), f"Consumables: {state.consumables or 'none'}",
            f"Owned weapons: {', '.join(state.owned_weapons)}", f"Owned gear: {', '.join(state.owned_gear)}",
            "Escape closes without advancing time.",
        ]
    if kind == "household":
        lines: list[str] = []
        for person in state.household:
            strongest = max(person.relationships.items(), key=lambda item: (item[1], item[0]))
            related = next(candidate.name for candidate in state.household if candidate.id == strongest[0])
            learned = ", ".join(person.learned_techniques) or "no expedition technique"
            lines.extend((
                f"{person.name} — {person.role}; {person.technique}; {person.injury}",
                f"  {learned}; closest standing: {related} ({strongest[1]:+d})",
            ))
        return "JOMON HOUSEHOLD", lines + ["Escape closes without advancing time."]
    if kind == "hold":
        cargo = [f"{name}: {stack.quantity}, {stack.condition}" for name, stack in state.vessel_cargo.items()]
        return "HOLD AND LOCAL PROBLEM", cargo + ["", state.region.condition, state.region.pressure, state.region.objective_text, f"Trade credit: {state.trade_credit}"]
    if kind == "equipment":
        return "STORES — PREPARE AT TAVERN C", [
            "Weapons: " + ", ".join(state.owned_weapons), "Secondary gear: " + ", ".join(state.owned_gear),
            "These physical stores remain aboard; all expedition selection happens at tavern C.", "Escape closes without time.",
        ]
    if kind == "contact" or kind.startswith("contact:"):
        contact = state.contact
        if kind.startswith("contact:"):
            contact_id = kind.split(":", 1)[1]
            contact = next(
                person for person in state.contacts[state.active_region_id]
                if person.id == contact_id
            )
        memories = contact.memories or ["No significant shared event yet."]
        return contact.name.upper(), [
            f"Role: {contact.role}; disposition: {contact.disposition:+d}",
            f"Material interest: {contact.interest}", f"Objective: {state.objective_status}",
            "Significant memories:", *[f"- {memory}" for memory in memories], "Escape closes without time.",
        ]
    if kind == "tavern":
        return "TAVERN EXPEDITION PREPARATION", _tavern_lines(state)
    if kind == "tavern:support":
        return "SELECT CREW SUPPORT", [f"{index + 1}. {name} — {detail}" for index, (name, detail) in enumerate(SUPPORTS.values())] + ["Number selects; Escape returns."]
    if kind == "tavern:relic":
        rows = [name for name in RELICS if state.relics.get(name, 0)]
        return "SELECT FINITE RELIC", ["0. Carry none", *[f"{index + 1}. {name} ({state.relics[name]}) — {RELICS[name]}" for index, name in enumerate(rows)], "Number selects; Escape returns."]
    if kind == "tavern:passive":
        rows = list(state.owned_passives)
        keys = "123456789abc"
        lines = [
            f"{keys[index]}. [{state.carried_passives.get(name, 0)}/{state.owned_passives[name]}] {name} "
            f"({PASSIVES[name][0]} bulk each) — {PASSIVES[name][1]}"
            for index, name in enumerate(rows[: len(keys)])
        ]
        return "PACK PASSIVE DISCOVERIES", lines + [
            f"Load: {passive_bulk(state)}/{passive_capacity(state)} bulk. Key toggles; Escape returns."
        ]
    if kind == "objective":
        alter = "available" if state.gear == "repair tools" or state.support in {"route survey", "carpenter rig"} or (state.courier and state.courier.technique == "lever craft") or state.contact.disposition >= 2 else "needs tools, support, lever craft, or trust"
        return state.contact.name.upper(), [state.region.pressure, state.region.objective_text, "A. Accept cargo recovery", "R. Refuse", f"T. Alter to mill-control repair ({alter})", "Escape cancels without time."]
    if kind == "merchant":
        lines = [f"Jomon credit: {state.trade_credit}"]
        for index, item in enumerate(state.merchant_stock):
            cost = max(1, MERCHANT_ITEMS[item][0] - (1 if state.support == "factor surety" else 0))
            lines.append(f"{index + 1}. {item} — {cost} credit")
        return "VISITING DECK MERCHANT", lines + ["Number buys; Escape closes. Stock leaves on departure."]
    if kind == "destination":
        lines = [
            f"{index + 1}. {state.regions[region_id].name}"
            + (" — current mooring" if region_id == state.active_region_id else "")
            + f"; {state.regions[region_id].process_name} stage {state.regions[region_id].process_stage}"
            for index, region_id in enumerate(DESTINATIONS)
        ]
        return "JOMON ROUTE CHART", lines + [
            "Travel costs six world measures and can produce a seeded voyage event.",
            "Number sets course; Escape keeps the current mooring without time.",
        ]
    if kind == "voyage":
        lines = [state.voyage_detail, ""]
        if state.voyage_kind == "raiders":
            lines += ["R. Repel with readied reach", "D. Distract with material preparation", "Y. Yield one cargo lot"]
        elif state.voyage_kind == "creature":
            lines += ["R. Repel with a spaced weapon", "E. Evade through pilot knowledge", "B. Bait with one salt-fish lot"]
        else:
            lines += ["A. Anchor to the real bank", "C. Counsel named crew", "N. Navigate by chart and lead line"]
        return "VOYAGE DANGER", lines + ["A response advances the action clock; Escape does not dismiss the danger."]
    if kind == "quit":
        return "QUIT JOMON?", ["Press Y to quit. Press N or Escape to continue."]
    if kind.startswith("person:"):
        from .people import person_by_id

        person_id = kind.split(":", 1)[1]
        person = person_by_id(state, person_id)
        if person is None:
            return "EMPTY SEAT", ["This person is no longer aboard."]
        standing = "active courier" if person.id == state.active_courier_id else "eligible household" if person in state.household else state.visitor_status.get(person.id, "visitor")
        lines = [
            f"{person.name} — {person.role}; {standing}",
            f"Technique: {person.technique}",
            f"Health: {person.health}/{person.max_health}; {person.injury}",
            f"Equipment affinity: {', '.join(person.equipment)}",
            f"Build tendency: {person.build_tendency}",
            f"Background: {person.background}",
            "Memories:", *[f"- {memory}" for memory in (person.memories or ["No shared expedition yet."])],
        ]
        if person in state.household and person.id != state.active_courier_id and person.alive and person.available:
            lines.append("S. Switch to this courier (zero time)")
        elif person not in state.household:
            lines.extend((f"Terms: {person.recruitment_terms}", "R. Offer a voluntary berth", "D. Defer without closing the invitation"))
        return person.name.upper(), lines + ["Escape closes without time."]
    return "INFORMATION", [kind, "Escape closes without advancing time."]


def _handle_overlay(state: GameState, kind: str, key: int) -> tuple[str | None, bool]:
    char = chr(key).lower() if 0 <= key < 256 else ""
    if key == 27:
        return ("tavern" if kind.startswith("tavern:") else None), False
    if kind in {"help", "inventory", "equipment", "household", "hold", "contact", "info"} or kind.startswith("contact:"):
        return None, False
    if kind == "quit":
        if char == "y":
            return None, True
        if char == "n":
            return None, False
        return kind, False
    if kind == "tavern":
        if key in {10, 13}:
            return None, False
        return ({
            "s": "tavern:support",
        }.get(char, kind)), False
    if kind == "tavern:support" and char.isdigit():
        rows = list(SUPPORTS)
        index = int(char) - 1
        if 0 <= index < len(rows):
            choose_support(state, rows[index])
            return "tavern", False
    if kind == "tavern:relic" and char.isdigit():
        rows = [name for name in RELICS if state.relics.get(name, 0)]
        index = int(char) - 1
        if char == "0":
            choose_relic(state, None)
            return "tavern", False
        if 0 <= index < len(rows):
            choose_relic(state, rows[index])
            return "tavern", False
    if kind == "tavern:passive" and char in "123456789abc":
        rows = list(state.owned_passives)
        index = "123456789abc".index(char)
        if index < len(rows):
            choose_passive(state, rows[index])
        return kind, False
    if kind == "objective" and char in {"a", "r", "t"}:
        result = decide_objective(state, {"a": "accept", "r": "refuse", "t": "alter"}[char])
        return (None if result.changed else kind), False
    if kind == "merchant" and char.isdigit():
        index = int(char) - 1
        if 0 <= index < len(state.merchant_stock):
            purchase_merchant_item(state, state.merchant_stock[index])
        return kind if state.merchant_stock else None, False
    if kind == "destination" and char in "1234":
        region_id = DESTINATIONS[int(char) - 1]
        changed, _ = choose_destination(state, region_id)
        if not changed:
            return kind, False
        return ("voyage" if state.voyage_status == "active" else None), False
    if kind == "voyage":
        choices = {
            "raiders": {"r": "repel", "d": "distract", "y": "yield"},
            "creature": {"r": "repel", "e": "evade", "b": "bait"},
            "lure": {"a": "anchor", "c": "counsel", "n": "navigate"},
        }[state.voyage_kind]
        if char in choices:
            resolve_voyage(state, choices[char])
            return None, False
    if kind.startswith("person:"):
        person_id = kind.split(":", 1)[1]
        from .people import person_by_id

        person = person_by_id(state, person_id)
        if char == "s" and person in state.household:
            result = choose_courier(state, person_id)
            return (None if result.changed else kind), False
        if char == "r":
            result = recruit_person(state, person_id)
            return (kind if not result.changed else None), False
        if char == "d":
            defer_recruit(state, person_id)
            return None, False
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
    inventory_view: InventoryView | None = None
    while True:
        _draw_base(screen, state)
        height, width = screen.getmaxyx()
        if inventory_view and height >= MIN_HEIGHT and width >= MIN_WIDTH:
            _draw_inventory(screen, state, inventory_view)
        elif overlay and height >= MIN_HEIGHT and width >= MIN_WIDTH:
            title, lines = _overlay_lines(state, overlay)
            _overlay(screen, title, lines)
        key = screen.getch()
        if key == curses.KEY_RESIZE:
            continue
        if height < MIN_HEIGHT or width < MIN_WIDTH:
            if key in {ord("q"), ord("Q")}:
                return state
            continue
        if inventory_view:
            closed, committed = _handle_inventory(state, inventory_view, key)
            if closed:
                if committed and inventory_view.transaction.changed:
                    if state.location == "region" and inventory_view.source is None:
                        _advance_world(state)
                        state.add_message("You complete one deliberate field repack.", priority=2)
                    else:
                        state.add_message("The physical load is arranged and accounted for.")
                inventory_view = None
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
            if result.overlay and result.overlay.startswith("inventory:container:"):
                source = result.overlay.split("inventory:", 1)[1]
                inventory_view = InventoryView.begin(state, source)
            else:
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
            inventory_view = InventoryView.begin(state)
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
