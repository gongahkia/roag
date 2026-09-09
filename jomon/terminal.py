"""Fullscreen curses presentation; gameplay remains in direct action functions."""

from __future__ import annotations

import curses
import copy
import textwrap
from dataclasses import dataclass
from typing import Iterable

from .actions import (
    RANGED_WEAPONS,
    _advance_world,
    attack,
    can_alter_objective,
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
    effective_weapon_range,
    resolve_cross_region_choice,
    resolve_regional_quest_choice,
    intervene_socially,
    retreat,
    use_contact_service,
    use_gear,
    use_route_stop,
)
from .inventory import (
    BODY_SLOTS,
    WEAPON_AMMUNITION,
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
    physical_ammunition,
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
from .travel import DESTINATIONS, choose_destination, resolve_voyage, travel_animation_frames
from .route_chart import chart_move, neighbours, route_availability
from .calendar import calendar_at, seasonal_route_note
from .vessel import DRINKS, current_area
from .world import (
    area_name,
    build_combinations,
    camera_origin,
    capacity,
    carried_bulk,
    cover_at,
    courier_sees,
    displayed_tile,
    distance,
    field_of_view,
    line_of_sight,
    map_rows,
    passive_bulk,
    passive_capacity,
    pressure,
    projectile_path,
    remembered,
    vertical_destination,
)

INVENTORY_HELP_LINES = (
    "Arrows/WASD move  Enter lift/place  R rotate  Space mark  */K all/category",
    "T transfer E equip O pack P pin Z auto [] body D drop C confirm Esc cancel",
)
TARGET_HELP_LINE = "Arrows/WASD/HJKL  Tab target  <> level  Enter act  Mouse select  Esc cancel"
ROUTE_HELP_LINES = (
    "Arrows/WASD/HJKL connected node  Enter preview/confirm  Tab layer  Esc close",
    "Mouse click selects; double-click confirms when reported; keyboard is complete",
)

MIN_WIDTH = 80
MIN_HEIGHT = 24

SEMANTIC_ROLES = (
    "player", "ally", "neutral", "hostile", "elite", "terrain", "water",
    "structure", "exit", "cargo", "interactable", "hazard", "mystical",
    "selected_target", "target_cell",
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


@dataclass
class TargetView:
    cursor: Position
    target_ids: list[str]
    selected: int = 0

    @classmethod
    def begin(cls, state: GameState) -> "TargetView":
        attack_range = effective_weapon_range(state)
        targets = sorted(
            (
                threat for threat in state.combatants
                if threat.status in {"watching", "engaged"}
                and distance(state.position, threat.position) <= attack_range
                and courier_sees(state, threat.position)
            ),
            key=lambda threat: (distance(state.position, threat.position), threat.id),
        )
        return cls(
            targets[0].position if targets else state.position,
            [target.id for target in targets],
        )


@dataclass(frozen=True)
class InputEvent:
    kind: str
    key: int = -1
    x: int = -1
    y: int = -1
    button: str = ""
    shift: bool = False
    double: bool = False


@dataclass
class OverlayView:
    kind: str
    selected: int = 0
    option_rows: list[int | tuple[int, int]] | None = None
    scroll_offset: int = 0
    page_rows: int = 1
    line_count: int = 0


@dataclass(frozen=True)
class ChoiceOption:
    key: str
    label: str
    semantic: str = "ordinary"
    available: bool = True
    requirement: str = ""


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
        "mystical": (5, True), "selected_target": (5, True),
        "target_cell": (3, True),
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
    if glyph in {"v", "B"}:
        return "neutral"
    if aboard and glyph in {"=", "t", "_", "F", "f"}:
        return "structure"
    if aboard and glyph in {"s", "G", "K", "N", "O", "S", "U", "W"}:
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
    if glyph in {"<", ">", "^", "+"}:
        return "exit"
    if glyph == "R":
        return "cargo"
    if glyph in {"&", "D", "O", "o", "?", "C", "L", "P", "H"}:
        return "interactable"
    if glyph in {"m", "%", "r", "q", "t", ":", "s"}:
        return "hazard"
    if glyph == "f":
        return "hazard"
    if glyph == "_":
        return "water"
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
    if threat.glyph:
        return threat.glyph
    return {"pursuer": "h", "reach": "g", "ranged": "x", "animal": "b", "machinery": "!"}[threat.profile]


def observed_life_lines(state: GameState) -> list[str]:
    from .content import ENEMY_ARCHETYPES

    lines = ["FACT: only presently visible actors are listed. No inspection advances time."]
    for actor in sorted(state.combatants, key=lambda a: (distance(a.position, state.position), a.id)):
        if not courier_sees(state, actor.position) or actor.status not in {"watching", "engaged"}:
            continue
        data = next((data for data in ENEMY_ARCHETYPES.values() if data["name"] == actor.name), None)
        lines.extend((
            "", f"{_threat_glyph(actor)} {actor.name}; {actor.position.x},{actor.position.y} z{actor.position.z:+d}; {actor.health}/{actor.max_health} health.",
            f"OBSERVED INTENT: {actor.intent}.",
            f"Duty: {actor.goal}; {actor.goal_reason}.",
            f"Working charges {actor.supplies}; recovery {actor.reload_turns}; morale {actor.morale}." if actor.id.startswith("frontier-elite:") else f"Readied {actor.ranged_kind}; ammunition {actor.ammunition}, reload {actor.reload_turns}." if actor.profile == "ranged" else f"Role: {actor.role}; morale {actor.morale}; supplies {actor.supplies}.",
        ))
        if data:
            lines.extend((f"KNOWN PRACTICE: {data['capability']}.", f"COUNTERS: {data['counterplay']}."))
    if len(lines) == 1:
        lines.append("No actor is presently visible; remembered terrain does not locate them.")
    return lines


def visible_threats(
    state: GameState, visible: set[Position] | None = None
) -> dict[Position, Threat]:
    """Return only current, presently seen actors; memory never carries actors."""
    if not state.combat_active:
        return {}
    visible = visible if visible is not None else field_of_view(state, remember=False)
    return {
        threat.position: threat
        for threat in state.combatants
        if threat.status in {"watching", "engaged"}
        and threat.position.z == state.position.z
        and threat.position in visible
    }


def visible_danger_marks(state: GameState, visible: set[Position]) -> set[Position]:
    from .frontier_elites import definition

    marks = set()
    for actor in state.combatants:
        seen_above_below = actor.position.z != state.position.z and courier_sees(state, actor.position)
        if actor.status != "engaged" or not (actor.position in visible or seen_above_below):
            continue
        point = actor.marked_position or actor.aimed_at
        if point is None:
            continue
        data = definition(actor)
        offsets = (-1, 0, 1) if data and data["mode"] in {"surge", "firing", "shutters"} else (0,)
        marks.update(p for dx in offsets if (p := Position(point.x + dx, point.y, point.z)) in visible)
    return marks


def _draw_map(screen: curses.window, state: GameState, top: int, left: int, height: int, width: int) -> None:
    from .materials import fields, key

    material_cells = fields(state)
    rows = map_rows(state)
    view_height, view_width = height - 2, width - 2
    map_height, map_width = len(rows), max(map(len, rows))
    origin_x, origin_y = camera_origin(
        state.position, map_width, map_height, view_width, view_height
    )
    visible = field_of_view(state, remember=False)
    threats = visible_threats(state, visible)
    danger_marks = visible_danger_marks(state, visible)
    known = set(state.region.seen)
    marks = set(state.treasure_marks.get(state.active_region_id, []))
    marked_positions = {container.position for container in state.region.containers if container.id in marks}
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
                and f"{position.x},{position.y},{position.z}" not in known
                and position not in marked_positions
            ):
                _put(screen, top + 1 + sy, left + 1 + sx, " ")
                continue
            if position == state.position:
                char = "@"
            elif position in threats:
                char = _threat_glyph(threats[position])
            elif position in danger_marks:
                char = "!"
            else:
                char = displayed_tile(state, position)
            role = semantic_role(char, aboard=state.location == "jomon")
            if position != state.position and position not in threats:
                cell = material_cells.get(key(position))
                if cell and (cell.fire or cell.smoke >= 2 or cell.collapse_due):
                    role = "hazard"
            if position in threats and position != state.position:
                actor = threats[position]
                role = "elite" if actor.elite else "neutral" if actor.ecology == "prey" else "hostile"
            attr = _COLOUR_ATTRIBUTES[role]
            if position in danger_marks:
                attr = _COLOUR_ATTRIBUTES["hazard"] | curses.A_BOLD | curses.A_REVERSE
            if position in threats and threats[position].profile == "ranged":
                attr |= curses.A_UNDERLINE
            if state.combat_active and position not in visible:
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
    visible = field_of_view(state, remember=False)
    local = [
        threat for threat in state.combatants
        if threat.status in {"watching", "engaged"} and threat.position in visible
    ]
    threat = local[0].intent if local else "no visible threat"
    transition = vertical_destination(state, state.position) if state.location == "region" else None
    level_text = f"{state.position.x},{state.position.y} z{state.position.z:+d}"
    if transition:
        level_text += " " + ("v below" if transition.z < state.position.z else "^ above")
    ammunition, ammunition_label = _ammunition_status(state)
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
        f"{level_text}; {state.objective_status}; Q{state.questlines[state.active_region_id].stage}/3",
        f"{state.region.objective_commodity}: {market.stock}/{market.demand}",
        f"Load {pack_weight(state)}/{weight_capacity(state)} {load_state(state)}",
        f"Ammo {ammunition} {ammunition_label}; oil {state.lamp_oil}",
        f"Rope {state.rope_uses}; smoke {state.smoke_charges}",
        _clip(threat, 25),
    ]
    if state.location == "jomon" and state.combat_active:
        lines[6:10] = ["VOYAGE CRISIS", f"Hull {state.vessel_integrity}/10", f"Action {state.vessel_changes.get('deck_ticks', 0)}; noise {state.noise}", str(state.voyage_kind)]
    if build_combinations(state):
        lines.append(f"Combo: {_clip(build_combinations(state)[0], 20)}")
    if state.terrain_statuses:
        name, status = next(iter(state.terrain_statuses.items()))
        lines.append(
            _clip(
                f"{name}: {status.remaining} — {status.consequence}",
                25,
            )
        )
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
    _put(screen, height - 2, 1, "Move HJKL/arrows E interact A attack G guard X gear F material Z ledger", curses.A_REVERSE)
    _put(screen, height - 1, 1, "V negotiate  R retreat  I inventory  S save aboard  ? help  Q quit", curses.A_REVERSE)
    screen.refresh()


def _target_at_cursor(state: GameState, view: TargetView) -> Threat | None:
    return next(
        (
            threat for threat in state.combatants
            if threat.position == view.cursor
            and threat.status in {"watching", "engaged"}
            and courier_sees(state, threat.position)
        ),
        None,
    )


def _ammunition_status(state: GameState) -> tuple[int, str]:
    ammo_name = WEAPON_AMMUNITION.get(state.weapon or "", "")
    ammo_label = {"sling stones": "stones", "handgonne charges": "charges"}.get(ammo_name, ammo_name or "none")
    available = physical_ammunition(state, ammo_name) if ammo_name else 0
    return available, ammo_label


def targeting_detail(state: GameState, view: TargetView) -> str:
    available, ammo_label = _ammunition_status(state)
    return (
        f"R{distance(state.position, view.cursor)}/{effective_weapon_range(state)} "
        f"z{view.cursor.z:+d} "
        f"Cover:{cover_at(state, state.position, view.cursor)} "
        f"Ammo:{available} {ammo_label}"
    )


def targeting_lines(state: GameState, view: TargetView, width: int) -> list[str]:
    selected = _target_at_cursor(state, view)
    ready = "Enter commits one cast; Escape costs no time."
    if state.weapon == "crossbow" and not state.crossbow_loaded:
        ready = "Unloaded: Escape then G reloads; no shot is committed here."
    elif state.weapon in {"heavy crossbow", "handgonne"} and state.weapon_ready < 2:
        ready = f"Unready: {2 - state.weapon_ready} reload actions; Escape then G."
    elif state.weapon in {"crossbow", "longbow", "heavy crossbow", "handgonne"}:
        ready = "Aim set: Enter releases one shot." if selected and state.aimed_target == selected.id else "Enter prepares aim (one action); confirm again to release."
    label = f"Target: {selected.name}" if selected else "No presently visible actor at cursor."
    if view.cursor.z != state.position.z:
        label += " [ABOVE]" if view.cursor.z > state.position.z else " [BELOW]"
    return information_lines([label, targeting_detail(state, view), ready], width)


def _draw_targeting(screen: curses.window, state: GameState, view: TargetView) -> None:
    height, width = screen.getmaxyx()
    status_width, event_height, command_height = 29, 6, 2
    main_height, map_width = height - event_height - command_height, width - status_width
    rows = map_rows(state)
    view_height, view_width = main_height - 2, map_width - 2
    origin_x, origin_y = camera_origin(
        state.position, max(map(len, rows)), len(rows), view_width, view_height
    )
    screen_x = 1 + view.cursor.x - origin_x
    screen_y = 1 + view.cursor.y - origin_y
    selected = _target_at_cursor(state, view)
    for point in projectile_path(state.position, view.cursor, state)[1:-1]:
        if point.z != state.position.z or not courier_sees(state, point):
            continue
        path_x = 1 + point.x - origin_x
        path_y = 1 + point.y - origin_y
        if 1 <= path_x < map_width - 1 and 1 <= path_y < main_height - 1:
            _put(
                screen, path_y, path_x, ".",
                _COLOUR_ATTRIBUTES["target_cell"] | curses.A_BOLD,
            )
    if (
        1 <= screen_x < map_width - 1
        and 1 <= screen_y < main_height - 1
    ):
        glyph = _threat_glyph(selected) if selected else "+"
        if view.cursor.z != state.position.z:
            glyph = "^" if view.cursor.z > state.position.z else "v"
        role = "selected_target" if selected else "target_cell"
        _put(screen, screen_y, screen_x, glyph, _COLOUR_ATTRIBUTES[role] | curses.A_REVERSE)
    lines = targeting_lines(state, view, width - 2)
    for row, line in enumerate(lines, height - 1 - len(lines)):
        _put(screen, row, 1, line.ljust(width - 2), curses.A_REVERSE | curses.A_BOLD)
    _put(screen, height - 1, 1, TARGET_HELP_LINE, curses.A_REVERSE)
    screen.refresh()


def _target_cycle(state: GameState, view: TargetView) -> None:
    available = [
        threat_id for threat_id in view.target_ids
        if any(
            threat.id == threat_id and threat.status in {"watching", "engaged"}
            and courier_sees(state, threat.position)
            for threat in state.combatants
        )
    ]
    if not available:
        return
    view.selected = (view.selected + 1) % len(available)
    view.target_ids = available
    target = next(threat for threat in state.combatants if threat.id == available[view.selected])
    view.cursor = target.position


def _handle_targeting(
    state: GameState,
    view: TargetView,
    event: InputEvent | int,
    *,
    screen_size: tuple[int, int] = (24, 80),
) -> tuple[bool, bool]:
    if isinstance(event, int):
        event = InputEvent("key", key=event)
    height, width = screen_size
    key = event.key
    if event.kind == "mouse":
        status_width, event_height, command_height = 29, 6, 2
        main_height, map_width = height - event_height - command_height, width - status_width
        if event.button not in {"left", "right"} or not (
            1 <= event.x < map_width - 1 and 1 <= event.y < main_height - 1
        ):
            return False, False
        rows = map_rows(state)
        origin_x, origin_y = camera_origin(
            state.position,
            max(map(len, rows)),
            len(rows),
            map_width - 2,
            main_height - 2,
        )
        view.cursor = Position(
            origin_x + event.x - 1,
            origin_y + event.y - 1,
            state.position.z,
        )
        if event.button == "right":
            return True, False
        if not event.double:
            return False, False
        key = 10
    if key == 27:
        return True, False
    if key in {ord("<"), ord(">")}:
        levels = {-1, 0, 1} if state.location == "jomon" else {int(z) for z in state.region.levels}
        level = view.cursor.z + (1 if key == ord(">") else -1)
        if level in levels:
            view.cursor = Position(view.cursor.x, view.cursor.y, level)
        return False, False
    normalized = ord(chr(key).lower()) if 0 <= key < 256 else key
    movement = {
        curses.KEY_LEFT: (-1, 0), curses.KEY_RIGHT: (1, 0),
        curses.KEY_UP: (0, -1), curses.KEY_DOWN: (0, 1),
        ord("a"): (-1, 0), ord("d"): (1, 0),
        ord("w"): (0, -1), ord("s"): (0, 1),
        ord("h"): (-1, 0), ord("l"): (1, 0),
        ord("k"): (0, -1), ord("j"): (0, 1),
    }
    if normalized in movement:
        dx, dy = movement[normalized]
        view.cursor = Position(
            max(0, min(len(map_rows(state)[0]) - 1, view.cursor.x + dx)),
            max(0, min(len(map_rows(state)) - 1, view.cursor.y + dy)),
            view.cursor.z,
        )
        return False, False
    if key == 9:
        _target_cycle(state, view)
        return False, False
    if key in {10, 13}:
        target = _target_at_cursor(state, view)
        if target is None:
            state.add_message("No visible hostile occupies the selected cell.", priority=2)
            return False, False
        result = attack(state, target.id)
        return result.time_advanced, result.time_advanced
    return False, False


def information_lines(lines: Iterable[str], width: int) -> list[str]:
    return [part for line in lines for part in (textwrap.wrap(line, width=max(1, width)) or [""])]


def _overlay(screen: curses.window, title: str, lines: Iterable[str], view: OverlayView | None = None) -> None:
    height, width = screen.getmaxyx()
    material = list(lines)
    box_width = min(width - 4, max(44, max((len(line) for line in material), default=20) + 4))
    material = information_lines(material, box_width - 4)
    box_height = min(height - 4, len(material) + (5 if view else 4))
    page_rows = max(1, box_height - (5 if view else 3))
    offset = min(view.scroll_offset, max(0, len(material) - page_rows)) if view else 0
    if view:
        view.scroll_offset, view.page_rows, view.line_count = offset, page_rows, len(material)
    top, left = (height - box_height) // 2, (width - box_width) // 2
    for y in range(top, top + box_height):
        _put(screen, y, left, " " * box_width, curses.A_REVERSE)
    _frame(screen, top, left, box_height, box_width, title)
    for index, line in enumerate(material[offset:offset + page_rows]):
        _put(screen, top + 2 + index, left + 2, _clip(line, box_width - 4))
    if view:
        footer = f"Up/Down PgUp/PgDn Home/End; Esc close [{offset + 1}/{len(material)}]"
        _put(screen, top + box_height - 2, left + 2, _clip(footer, box_width - 4), curses.A_BOLD)
    screen.refresh()


def dialogue_choices(state: GameState, kind: str) -> list[ChoiceOption]:
    if kind == "station:workshop" or kind.startswith("workshop:"):
        from .workshop import FITTINGS, SLOTS, attached, can_fit, compatible, fit_cost

        if kind == "station:workshop":
            return [ChoiceOption(str(index + 1), f"{slot}: {item_spec(item.kind).name if (item := equipped_item(state, slot)) else 'empty'}", available=equipped_item(state, slot) is not None, requirement="equip an item through I") for index, slot in enumerate(SLOTS)] + [ChoiceOption("P", "Buy a loose fitting kit")]
        if kind == "workshop:store":
            return [ChoiceOption(chr(65 + index), f"{value.name}: {value.price} credit; stock {state.vessel_changes.get('fitting_stock:' + name, 0)}") for index, (name, value) in enumerate(FITTINGS.items())]
        if kind.startswith("workshop:slot:"):
            target = equipped_item(state, kind.split(":", 2)[2])
            if target is None:
                return []
            options = []
            for index, name in enumerate(FITTINGS):
                if compatible(target, name):
                    valid, why = can_fit(state, target, name)
                    options.append(ChoiceOption(chr(65 + index), f"Preview {name}: {fit_cost(state, name)} credit", "commitment", valid, why))
            for key, socket in zip("UVW", ("structure", "treatment", "lining")):
                if any(FITTINGS[part.kind.split(':', 1)[1]].slot == socket for part in attached(state, target)):
                    options.append(ChoiceOption(key, f"Remove {socket}: 1 credit, pack room", "commitment"))
            options.append(ChoiceOption("R", "Repair parent: 2 credit, +35 condition", "commitment", target.condition < 100 and state.trade_credit >= 2, "damaged item and two credit"))
            return options
        return [ChoiceOption("F", "Confirm the previewed work (two actions)", "commitment"), ChoiceOption("B", "Back without changes")]
    if kind == "material":
        return [ChoiceOption(str(index + 1), name) for index, name in enumerate(("Here", "North", "East", "South", "West"))]
    if kind.startswith("material:"):
        from .materials import VERBS

        return [ChoiceOption(chr(ord("a") + index), verb.title(), "danger" if verb in {"ignite", "break", "cut"} else "commitment") for index, verb in enumerate(VERBS)]
    if kind == "quit":
        return [ChoiceOption("Y", "Quit Jomon", "danger"), ChoiceOption("N", "Continue playing")]
    if kind == "tavern":
        return [ChoiceOption("S", "Choose crew support"), ChoiceOption("Enter", "Close preparation")]
    if kind == "tavern:support":
        return [ChoiceOption(str(index + 1), f"{name} — {detail}") for index, (name, detail) in enumerate(SUPPORTS.values())]
    if kind == "objective":
        return [
            ChoiceOption("A", "Accept cargo recovery", "commitment"),
            ChoiceOption("R", "Refuse the request", "refusal"),
            ChoiceOption("T", "Alter to material control work", "commitment", can_alter_objective(state), "tools, support, lever craft, or trust"),
        ]
    if kind == "quest:regional":
        from .quests import regional_resolution_options

        return [ChoiceOption(key.upper(), label, semantic, available, requirement) for key, label, semantic, available, requirement in regional_resolution_options(state)]
    if kind == "quest:arc":
        from .quests import arc_options

        return [ChoiceOption(key.upper(), label, semantic, available, requirement) for key, label, semantic, available, requirement in arc_options(state)]
    if kind.startswith("contact-service:"):
        from .quests import secondary_service_options

        return [ChoiceOption(key.upper(), label, semantic, available, requirement) for key, label, semantic, available, requirement in secondary_service_options(state)]
    if kind == "merchant":
        return [
            ChoiceOption(str(index + 1), f"Buy {item} — {max(1, MERCHANT_ITEMS[item][0] - (1 if state.support == 'factor surety' else 0))} credit", "commitment", state.trade_credit >= max(1, MERCHANT_ITEMS[item][0] - (1 if state.support == "factor surety" else 0)), "sufficient credit")
            for index, item in enumerate(state.merchant_stock)
        ]
    if kind == "voyage":
        from .ship_crises import choices
        rows = choices(state)
        return [ChoiceOption(*row) for row in rows]
    if kind.startswith("ship-work:"):
        task = kind.split(":", 1)[1]
        timber = state.vessel_cargo.get("timber")
        allowed = task != "repair" or bool(timber and timber.quantity and state.vessel_integrity < 10)
        return [ChoiceOption("F", "Confirm the counted work and time", "commitment", allowed, "needs hull damage and one timber lot" if not allowed else ""), ChoiceOption("B", "Back without work")]
    if kind == "bartender":
        return [
            ChoiceOption("D", "Browse the counted drink stock"),
            ChoiceOption("S", "Choose crew support"),
            ChoiceOption("L", "Leave the bar"),
        ]
    if kind == "bartender:drinks":
        return [
            ChoiceOption(str(index + 1), f"{drink.name} — {drink.cost} credit; stock {state.bartender_stock.get(drink_id, 0)}", "commitment", state.bartender_stock.get(drink_id, 0) > 0, "current counted stock")
            for index, (drink_id, drink) in enumerate(DRINKS.items())
        ]
    if kind.startswith("bartender:drink:"):
        drink_id = kind.split(":", 2)[2]
        drink = DRINKS[drink_id]
        available = state.bartender_stock.get(drink_id, 0) > 0 and state.trade_credit >= drink.cost
        return [
            ChoiceOption("D", "Drink one measure now", "commitment", available, f"{drink.cost} credit and stock"),
            ChoiceOption("B", "Buy one bottle into the pack", "commitment", available, f"{drink.cost} credit and pack space"),
        ]
    if kind == "incident":
        return [
            ChoiceOption("M", "Mediate by naming the disputed work", "commitment"),
            ChoiceOption("S", "Support the first speaker", "refusal"),
            ChoiceOption("F", "Let the bounded fistfight run", "danger"),
        ]
    if kind == "route-stop":
        node = state.route_nodes[state.route_current_node]
        available = int(state.vessel_changes.get(f"supply_available:{node.id}", 0))
        return [
            ChoiceOption("R", "Load one grain lot for one credit", "commitment", available > 0 and state.trade_credit >= 1, "available provision and one credit"),
            ChoiceOption("T", f"Trade one {node.market_interest or 'wanted'} lot for two credit", "commitment", bool(node.market_interest and state.vessel_cargo.get(node.market_interest)), f"one {node.market_interest or 'wanted'} cargo lot"),
            ChoiceOption("S", "Take fresh soundings of connected unknown water", "ordinary"),
        ]
    if kind.startswith("person:"):
        from .people import person_by_id

        person = person_by_id(state, kind.split(":", 1)[1])
        if person in state.household and person and person.id != state.active_courier_id and person.alive and person.available:
            return [ChoiceOption("S", "Switch to this courier", "commitment")]
        if person and person not in state.household:
            return [ChoiceOption("R", "Offer a voluntary berth", "commitment"), ChoiceOption("D", "Defer the invitation", "refusal")]
    return []


def _choice_attribute(option: ChoiceOption, selected: bool) -> int:
    role = {
        "ordinary": "exit", "commitment": "cargo", "refusal": "hazard", "danger": "elite",
    }[option.semantic]
    attr = _COLOUR_ATTRIBUTES[role] | curses.A_BOLD
    if not option.available:
        attr = curses.A_DIM
    if selected:
        attr |= curses.A_REVERSE
    return attr


def dialogue_choice_lines(option: ChoiceOption, selected: bool, width: int) -> list[str]:
    pointer = ">" if selected else " "
    availability = "" if option.available else f" [unavailable: {option.requirement}]"
    return _wrapped(f"{pointer} [{option.key}] {option.label}{availability}", width)


def _draw_dialogue_overlay(screen: curses.window, state: GameState, view: OverlayView) -> None:
    title, raw_lines = _overlay_lines(state, view.kind)
    options = dialogue_choices(state, view.kind)
    if options:
        view.selected %= len(options)
    option_prefixes = tuple(f"{option.key}." for option in options)
    narrative = [line for line in raw_lines if not line.lstrip().startswith(option_prefixes)]
    height, width = screen.getmaxyx()
    box_width = min(width - 4, max(52, min(76, max([len(line) for line in narrative] + [len(option.label) + 12 for option in options] + [30]) + 4)))
    wrapped: list[str] = []
    for line in narrative:
        wrapped.extend(_wrapped(line, box_width - 4) or [""])
    rendered_options = [
        dialogue_choice_lines(option, index == view.selected, box_width - 4)
        for index, option in enumerate(options)
    ]
    option_line_count = sum(map(len, rendered_options))
    box_height = min(height - 4, len(wrapped) + option_line_count + 5)
    top, left = (height - box_height) // 2, (width - box_width) // 2
    for y in range(top, top + box_height):
        _put(screen, y, left, " " * box_width, curses.A_REVERSE)
    _frame(screen, top, left, box_height, box_width, title)
    row = top + 2
    for line in wrapped[: max(0, box_height - option_line_count - 4)]:
        _put(screen, row, left + 2, _clip(line, box_width - 4))
        row += 1
    view.option_rows = []
    for index, (option, option_lines) in enumerate(zip(options, rendered_options)):
        if row >= top + box_height - 1:
            break
        first_row = row
        for line in option_lines:
            if row >= top + box_height - 1:
                break
            _put(screen, row, left + 2, line, _choice_attribute(option, index == view.selected))
            row += 1
        view.option_rows.append((first_row, row - 1))
    screen.refresh()


def _handle_overlay_view(state: GameState, view: OverlayView, event: InputEvent) -> tuple[bool, bool]:
    options = dialogue_choices(state, view.kind)
    if options:
        view.selected %= len(options)
    key = event.key
    if not options:
        scrolling = {
            curses.KEY_UP: -1, ord("k"): -1, ord("w"): -1,
            curses.KEY_DOWN: 1, ord("j"): 1, ord("s"): 1,
            curses.KEY_PPAGE: -view.page_rows, curses.KEY_NPAGE: view.page_rows,
            curses.KEY_HOME: -view.line_count, curses.KEY_END: view.line_count,
        }
        if event.kind == "mouse":
            key = {"wheel-up": curses.KEY_UP, "wheel-down": curses.KEY_DOWN}.get(event.button, -1)
        if key in scrolling:
            view.scroll_offset = max(0, min(max(0, view.line_count - view.page_rows), view.scroll_offset + scrolling[key]))
            return False, False
        if event.kind == "mouse":
            return False, False
    if event.kind == "mouse" and event.button == "left" and view.option_rows:
        index = next(
            (
                index for index, span in enumerate(view.option_rows)
                if (
                    span == event.y if isinstance(span, int)
                    else span[0] <= event.y <= span[1]
                )
            ),
            None,
        )
        if index is None or index >= len(options):
            return False, False
        view.selected = index
        if not event.double:
            return False, False
        key = 10
    if options:
        normalized = ord(chr(key).lower()) if 0 <= key < 256 else key
        direct_character = chr(key).lower() if 0 <= key < 256 else ""
        conflicts = any(option.key.lower() == direct_character for option in options)
        if normalized in {curses.KEY_UP, ord("w"), ord("k")} and not conflicts:
            view.selected = (view.selected - 1) % len(options)
            return False, False
        if normalized in {curses.KEY_DOWN, ord("s"), ord("j")} and not conflicts:
            view.selected = (view.selected + 1) % len(options)
            return False, False
        if key in {10, 13}:
            option = options[view.selected]
            if not option.available:
                state.add_message(f"Unavailable: {option.requirement}.", priority=2)
                return False, False
            key = 10 if option.key == "Enter" else ord(option.key.lower())
        elif 0 <= key < 256:
            direct = next((option for option in options if option.key.lower() == chr(key).lower()), None)
            if direct and not direct.available:
                state.add_message(f"Unavailable: {direct.requirement}.", priority=2)
                return False, False
    next_kind, should_quit = _handle_overlay(state, view.kind, key)
    if next_kind is None:
        return True, should_quit
    if next_kind != view.kind:
        view.kind, view.selected, view.option_rows = next_kind, 0, []
        view.scroll_offset = 0
    return False, should_quit


def _chart_screen_point(node_x: int, node_y: int, map_width: int, height: int) -> tuple[int, int]:
    usable_width, usable_height = max(12, map_width - 5), max(8, height - 7)
    return 2 + round(node_x / 78 * usable_width), 2 + round(node_y / 20 * usable_height)


def chart_label_position(x: int, label: str, map_width: int) -> tuple[int, str]:
    label = _clip(label, min(10, map_width - 4))
    left = x + 2 if x + 2 + len(label) < map_width - 1 else max(1, x - len(label) - 1)
    return left, label


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


def route_detail_lines(
    state: GameState,
    view: RouteChartView,
    max_width: int,
) -> list[str]:
    """Fit route consequences without hiding them behind ellipses."""
    node = state.route_nodes[view.cursor]
    date = calendar_at(state)
    description = f"Place: {node.description}"
    market = f"Market: {node.market_interest or 'none'}"
    contact = "Contacts: established" if node.region_id else "Contacts: no permanent stop"
    if view.cursor == state.route_current_node:
        raw = [
            node.name.upper(), f"Type: {node.kind}", description,
            "Jomon is moored here.", f"Season: {date.season}",
            market, contact, f"Calendar: {date.label}",
            f"Integrity: {state.vessel_integrity}/10",
        ]
    else:
        edge = next(
            edge for edge in state.route_edges
            if {edge.first, edge.second} == {state.route_current_node, view.cursor}
        )
        available, reason = route_availability(state, view.cursor)
        route = f"Route: {edge.hazard}; {edge.travel_time} actions; supplies {edge.supply_cost}"
        risks = f"Risks: cargo {edge.cargo_risk}/3; weather {edge.weather_exposure}/4"
        season = f"{date.season.title()}: {seasonal_route_note(state)}"
        route_layer = [description, route, risks, "REACHABLE" if available else f"BLOCKED: {reason}"]
        market_layer = [description, f"Supplies used: {edge.supply_cost}", market, contact]
        season_layer = [season, f"Calendar: {date.label}", f"Integrity: {state.vessel_integrity}/10"]
        chosen_layer = (route_layer, market_layer, season_layer)[view.overlay_mode]
        raw = [node.name.upper(), f"Type: {node.kind}", *chosen_layer]
        if view.confirming:
            raw = [
                node.name.upper(), f"Type: {node.kind}", description, route, risks,
                season, market, contact,
                "REACHABLE" if available else f"BLOCKED: {reason}", "",
                f"> ENTER — {'CONFIRM LEG' if available else 'BLOCKED'}",
                "  ESC — cancel",
            ]
    lines: list[str] = []
    for line in raw:
        lines.extend(_wrapped(line, max_width) or [""])
    return lines


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
    for y in range(3, max(3, height - 4), 4):
        for x in range(4 + (y % 3), max(4, map_width - 3), 9):
            _put(screen, y, x, "~", _COLOUR_ATTRIBUTES["water"] | curses.A_DIM)
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
        if node.region_id:
            left, label = chart_label_position(x, node.name, map_width)
            _put(screen, y, left, label, curses.A_BOLD if known else curses.A_DIM)
    if moving:
        mx, my = _chart_screen_point(moving[0], moving[1], map_width, height)
        _put(screen, my, mx, "@", _COLOUR_ATTRIBUTES["player"] | curses.A_REVERSE)
        _put(screen, height - 4, 2, _clip(moving[2], map_width - 4), curses.A_BOLD)
    selected = state.route_nodes[view.cursor]
    lines = route_detail_lines(state, view, detail_width - 4)
    for index, line in enumerate(lines[: height - 5]):
        attr = curses.A_BOLD if index == 0 or line.startswith(">") else curses.A_DIM if "BLOCKED" in line else 0
        _put(screen, 2 + index, map_width + 2, line, attr)
    for index, line in enumerate(ROUTE_HELP_LINES):
        _put(screen, height - 2 + index, 1, line, curses.A_REVERSE)
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
        return ["pack", "locker", "ground"] if any(item.location == "ground" and item.region_id == "jomon" and item.ground_position == state.position for item in state.items) else ["pack", "locker"]
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
            and item.region_id == state.spatial_id
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
        from .workshop import attached, effective_spec

        spec = effective_spec(state, selected)
        held = "HELD — " if view.held_id else ""
        actual_width, actual_height = (spec.height, spec.width) if view.held_id and view.held_rotated else (spec.width, spec.height)
        _put(screen, 14, detail_x, _clip(f"{held}{spec.name} {actual_width}x{actual_height} wt {spec.weight}", width - detail_x - 2), curses.A_BOLD)
        for index, art in enumerate(item_preview(selected.kind)):
            _put(screen, 15 + index, detail_x, _clip(art, width - detail_x - 2), _item_colour(selected.kind))
        fitted = attached(state, selected)
        if fitted:
            _put(screen, 17, detail_x, _clip("Fitted: " + ", ".join(part.kind.split(":", 1)[1] for part in fitted), width - detail_x - 2), curses.A_BOLD)
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
    for index, line in enumerate(INVENTORY_HELP_LINES):
        _put(screen, height - 2 + index, 1, line, curses.A_REVERSE)
    screen.refresh()


def _clamp_inventory_cursor(state: GameState, view: InventoryView) -> None:
    if view.pane in {"pack", "locker"}:
        width, height = grid_size(state, view.pane)
        view.cursor_x = max(0, min(width - 1, view.cursor_x))
        view.cursor_y = max(0, min(height - 1, view.cursor_y))
    else:
        view.cursor_x = 0
        view.cursor_y = max(0, min(max(0, len(_inventory_items(state, view)) - 1), view.cursor_y))


def _transfer_inventory_items(state: GameState, view: InventoryView, items: list) -> bool:
    operation = InventoryTransaction.begin(state)
    for item in items:
        source_container_id = item.container_id
        if item.location == "pack" and state.location == "jomon":
            moved = transfer_to_grid(state, item.id, "locker")
        elif item.location == "locker":
            moved = transfer_to_grid(state, item.id, "pack", owner_id=state.active_courier_id)
        elif item.location in {"container", "ground"}:
            moved = state.auto_place_enabled and auto_place(state, item.id, "pack", owner_id=state.active_courier_id)
            if moved:
                if source_container_id:
                    container = next((box for box in state.region.containers if box.id == source_container_id), None)
                    if container and item.id in container.item_ids:
                        container.item_ids.remove(item.id)
                record_acquisition(state, item)
        else:
            moved = False
        if not moved:
            operation.cancel(state)
            return False
    sync_legacy_load(state)
    return True


def _inventory_mouse_key(state: GameState, view: InventoryView, event: InputEvent) -> int | None:
    if event.kind != "mouse":
        return event.key
    if view.pane in {"pack", "locker"}:
        origin_x, origin_y = view.grid_origin
        grid_width, grid_height = grid_size(state, view.pane)
        cell_x, cell_y = (event.x - origin_x) // 2, event.y - origin_y
        if origin_x <= event.x < origin_x + grid_width * 2 and 0 <= cell_y < grid_height:
            view.cursor_x, view.cursor_y = cell_x, cell_y
            if event.shift and event.button == "left":
                item = _inventory_item_at(state, view)
                if item:
                    selected = view.selected_ids or set()
                    selected.symmetric_difference_update({item.id})
                    view.selected_ids = selected
                return None
            if event.button == "right":
                return ord("r")
            if event.button == "left":
                return ord("t") if event.double else 10
    if view.paper_screen and event.button == "left":
        candidates = [
            (abs(x - event.x) + abs(y - event.y), slot)
            for slot, (x, y) in view.paper_screen.items()
            if y == event.y and event.x >= x
        ]
        if candidates:
            slot = min(candidates)[1]
            view.paper_slot = PAPER_SLOTS.index(slot)
            if event.double and slot in BODY_SLOTS:
                return ord(str(BODY_SLOTS.index(slot) + 1))
    if event.button == "wheel-up":
        return ord("[")
    if event.button == "wheel-down":
        return ord("]")
    return None


def _handle_inventory(state: GameState, view: InventoryView, event: InputEvent | int) -> tuple[bool, bool]:
    """Return (closed, committed); Escape restores the complete opening state."""
    if isinstance(event, int):
        event = InputEvent("key", key=event)
    key = _inventory_mouse_key(state, view, event)
    if key is None:
        return False, False
    char = chr(key).lower() if 0 <= key < 256 else ""
    movement = {
        curses.KEY_LEFT: (-1, 0), curses.KEY_RIGHT: (1, 0),
        curses.KEY_UP: (0, -1), curses.KEY_DOWN: (0, 1),
        ord("a"): (-1, 0), ord("d"): (1, 0), ord("w"): (0, -1), ord("s"): (0, 1),
    }
    normalized = ord(char) if char else key
    if key == 27:
        if view.pending_drop:
            view.pending_drop = False
            view.status = "Drop cancelled; nothing moved."
            return False, False
        if view.selected_ids:
            view.selected_ids.clear()
            view.status = "Selection cleared."
            return False, False
        if view.held_id:
            view.held_id = None
            view.status = "Held preview returned to its committed source."
            return False, False
        view.transaction.cancel(state)
        return True, False
    if key == 9:
        panes = _inventory_panes(state, view)
        view.pane = panes[(panes.index(view.pane) + 1) % len(panes)]
        view.cursor_x = view.cursor_y = 0
        return False, False
    if view.pending_drop:
        if char == "y":
            targets = [item for item in state.items if item.id in (view.selected_ids or set())]
            if not targets:
                item = _inventory_item_at(state, view)
                targets = [item] if item else []
            dropped = bool(targets) and all(item.location == "pack" for item in targets)
            if dropped:
                for item in targets:
                    dropped = drop_item(state, item.id) and dropped
            if dropped:
                view.transaction.changed = True
                view.selected_ids.clear()
                sync_legacy_load(state)
                view.status = f"Dropped {len(targets)} physical item(s)."
            else:
                view.status = "Drop failed; every target remains accounted for."
            view.pending_drop = False
        elif char == "n":
            view.pending_drop = False
            view.status = "Drop cancelled."
        return False, False
    if key == ord("D"):
        if state.location == "jomon" and state.jomon_space != "vessel":
            view.status = "Use the locker or vessel deck; tavern floor storage is not available."
            return False, False
        item = _inventory_item_at(state, view)
        targets = view.selected_ids or ({item.id} if item else set())
        if targets:
            view.pending_drop = True
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
            view.held_rotated = not view.held_rotated
            view.status = "Rotation preview changed; the source remains committed."
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
            if place_item(state, item.id, view.pane, view.cursor_x, view.cursor_y, rotated=view.held_rotated, owner_id=owner):
                view.held_id = None
                view.transaction.changed = True
                view.status = "Placement committed within this repack transaction."
            else:
                view.status = "Invalid placement; the item remains at its committed source."
            return False, False
        item = _inventory_item_at(state, view)
        if item and view.pane in {"pack", "locker"}:
            view.held_id = item.id
            view.held_rotated = item.rotated
            view.status = "Placement preview lifted; source remains committed until placement."
        return False, False
    item = _inventory_item_at(state, view)
    if key == ord(" "):
        if item:
            selected = view.selected_ids or set()
            selected.symmetric_difference_update({item.id})
            view.selected_ids = selected
        return False, False
    if char == "*":
        view.selected_ids = {item.id for item in _inventory_items(state, view)}
        return False, False
    if char == "k" and item:
        category = item_spec(item.kind).category
        view.selected_ids = {other.id for other in _inventory_items(state, view) if item_spec(other.kind).category == category}
        return False, False
    if char == "p":
        targets = [other for other in state.items if other.id in (view.selected_ids or set())]
        if not targets and item:
            targets = [item]
        if targets:
            pin = not all(other.pinned for other in targets)
            for other in targets:
                pin_item(state, other.id, pin)
            view.transaction.changed = True
            view.status = f"{'Pinned' if pin else 'Unpinned'} {len(targets)} item(s)."
        return False, False
    if char == "o" and view.pane in {"pack", "locker"}:
        owner = state.active_courier_id if view.pane == "pack" else None
        selected = view.selected_ids if view.selected_ids else None
        if auto_pack(state, view.pane, owner_id=owner, selected_ids=selected):
            view.transaction.changed = True
            view.status = "Auto-pack committed; pinned items did not move."
        else:
            view.status = "Auto-pack found no complete layout; the original is unchanged."
        return False, False
    if char == "z":
        state.auto_place_enabled = not state.auto_place_enabled
        view.transaction.changed = True
        view.status = f"Auto-place new items {'enabled' if state.auto_place_enabled else 'disabled'}."
        return False, False
    if char in {"[", "]"}:
        view.paper_slot = (view.paper_slot + (-1 if char == "[" else 1)) % len(PAPER_SLOTS)
        return False, False
    if char == "e" and item and item.location == "pack" and equip_item(state, item.id):
        view.transaction.changed = True
        sync_legacy_load(state)
        return False, False
    if char in "123456" and unequip_item(state, BODY_SLOTS[int(char) - 1]):
        view.transaction.changed = True
        sync_legacy_load(state)
        return False, False
    if char == "t" and (item or view.selected_ids):
        targets = [other for other in state.items if other.id in (view.selected_ids or set())]
        if not targets and item:
            targets = [item]
        moved = _transfer_inventory_items(state, view, targets)
        if moved:
            view.transaction.changed = True
            view.selected_ids.clear()
            view.status = f"Transferred {len(targets)} item(s)."
        else:
            view.status = "Transfer could not fit every item; nothing moved."
        _clamp_inventory_cursor(state, view)
        return False, False
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
    if kind == "observed-life":
        return "VISIBLE ACTORS, DUTIES AND COUNTERS", observed_life_lines(state)
    if kind == "material":
        return "MATERIAL HANDLING", ["Choose a nearby physical target. No time passes until handling is confirmed."]
    if kind.startswith("material:"):
        from .materials import inspect_material, point_at

        return "MATERIAL — ONE ACTION PER HANDLING", inspect_material(state, point_at(kind.split(":", 1)[1]))
    if kind == "help":
        return "HELP", list(HELP_LINES)
    if kind == "inventory":
        goods = [f"{name}: {stack.quantity}, {stack.condition} ({COMMODITIES[name]['bulk']} bulk each)" for name, stack in state.carried_goods.items()]
        statuses = [
            f"{name}: {status.remaining} actions; from {status.cause}; {status.consequence}"
            for name, status in state.terrain_statuses.items()
        ]
        return "INVENTORY", [
            f"Capacity: {carried_bulk(state)}/{capacity(state)} bulk",
            f"Weapon: {state.weapon or 'none'}; gear: {state.gear or 'none'}; relic: {state.carried_relic or 'none'}",
            f"Passive discoveries: {state.carried_passives or 'none'} ({passive_bulk(state)}/{passive_capacity(state)} bulk)",
            f"Finite supplies: ammunition {state.ammunition}; oil {state.lamp_oil}; rope {state.rope_uses}; smoke {state.smoke_charges}",
            "Goods:", *(goods or ["none"]), f"Consumables: {state.consumables or 'none'}",
            "Current conditions:", *(statuses or ["none"]),
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
    if kind == "chronicle":
        return "JOMON VESSEL CHRONICLE", [*(state.chronicle[-16:] or ["No vessel incident is recorded yet."]), "Escape closes without advancing time."]
    if kind == "regional-ledger":
        from .regional_history import ledger_lines

        return "REGIONAL WORK, HISTORY AND FORECAST", ledger_lines(state)
    if kind == "station:workshop" or kind.startswith("workshop:"):
        from .workshop import FITTINGS, describe, fit_cost

        if kind == "station:workshop":
            return "LOWER WORKSHOP — OPTIONAL FITTINGS", [f"Credit {state.trade_credit}. Choose worn/readied equipment or a loose kit.", "One structural fitting plus one treatment per weapon; one lining per armour piece."]
        if kind == "workshop:store":
            return "COUNTED WORKSHOP KITS", ["Preview before buying. Kits occupy the pack until fitted.", "Stock is finite; cancelled or unfittable purchases spend nothing."]
        if kind.startswith("workshop:slot:"):
            item = equipped_item(state, kind.split(":", 2)[2])
            return "EQUIPMENT WORK", describe(state, item) if item else ["That equipment slot is empty."]
        parts = kind.split(":")
        if parts[1] == "buy":
            fitting = FITTINGS[parts[2]]
            return "BUY KIT — CONFIRM", [fitting.name, fitting.effect, fitting.drawback, f"{fitting.shape[0]}x{fitting.shape[1]} cells, weight {fitting.weight}; {fitting.price} credit; must fit in pack."]
        item = next((item for item in state.items if item.id == parts[2]), None)
        if item is None:
            return "EQUIPMENT WORK", ["The selected physical item is no longer here."]
        if parts[1] == "fit":
            fitting = FITTINGS[parts[3]]
            return "FIT EQUIPMENT — CONFIRM", [f"{fitting.name} onto {item_spec(item.kind).name}.", fitting.effect, fitting.drawback, f"Adds {fitting.weight} carried weight; {fit_cost(state, parts[3])} credit; two actions."]
        return "EQUIPMENT WORK — CONFIRM", describe(state, item) + ["Removal keeps the part if it fits. Repair leaves fitting wear unchanged.", "Removal costs one credit; repair costs two. Both take two actions."]
    if kind.startswith("station:"):
        station = kind.split(":", 1)[1]
        title, detail = {
            "galley": ("JOMON GALLEY", "Counted provisions become meals here; seasonal stores affect bar and voyage supplies."),
            "repair": ("REPAIR POSITION", f"Tools, spare timber, and rigging serve Jomon's integrity ({state.vessel_integrity}/10)."),
            "berths": ("JOMON BERTHS", "Named adults sleep or recover here according to injury and action-clock schedule."),
            "bilge": ("BALLAST AND BILGE", "Water collects on the lower deck; repair watches inspect hull access during crises."),
            "provisions": ("PROVISION STORE", "Dry grain and sealed fish are physically counted for route legs and emergencies."),
            "workshop": ("LOWER WORKSHOP", "Armour, tools, and damaged possessions are accounted for beside the cargo hold."),
            "storage": ("SERVING AND DECK STORE", "Bounded working stores support the nearby station; nothing here is unlimited."),
            "helm": ("JOMON HELM", "The scheduled pilot steers here; travel changes only at confirmed route legs."),
            "lookout": ("LOOKOUT POSITION", "This exposed upper position improves warning and becomes a defensive voyage station."),
            "gathering": ("COMMON DECK", "Crew gather, train, and dispute work here when schedules and memories align."),
            "market": ("VISITING BERTH", "Regional merchants use this bounded berth only when a recorded visit is active."),
        }.get(station, ("JOMON WORK POSITION", "A bounded vessel activity uses this physical position."))
        return title, [detail, "Inspection costs no time. Escape closes."]
    if kind == "hold":
        cargo = [f"{name}: {stack.quantity}, {stack.condition}" for name, stack in state.vessel_cargo.items()]
        return "HOLD AND LOCAL PROBLEM", cargo + ["", state.region.condition, state.region.pressure, state.region.objective_text, f"Trade credit: {state.trade_credit}"]
    if kind == "equipment":
        return "STORES — PREPARE AT TAVERN C", [
            "Weapons: " + ", ".join(state.owned_weapons), "Secondary gear: " + ", ".join(state.owned_gear),
            "These physical stores remain aboard; all expedition selection happens at tavern C.", "Escape closes without time.",
        ]
    if kind == "quest:regional":
        from .quests import QUESTS

        quest = state.questlines[state.active_region_id]
        return QUESTS[state.active_region_id]["title"].upper(), [
            f"Opening decision: {quest.branch or 'none'}.",
            f"Material work: {state.objective_status}; optional lead: {'completed' if quest.optional_done else 'open'}.",
            "This decision changes people, work, hazards, and later visits.",
            "Choose one recorded regional settlement.",
        ]
    if kind == "quest:arc":
        from .quests import ARC_REGIONS, ARC_TITLE

        arc = state.cross_region_arc
        next_region = ARC_REGIONS.get(arc.stage)
        return ARC_TITLE.upper(), [
            f"Chapter {arc.stage + 1 if arc.stage < 5 else 5}/5; account: {arc.branch or 'not yet bound'}.",
            f"Current witness: {state.region.name}.",
            f"Next required region: {state.regions[next_region].name if next_region else 'ending decision here'}.",
            "The working marks are physical evidence, not a prophecy.",
        ]
    if kind.startswith("contact-service:"):
        contact_id = kind.split(":", 1)[1]
        contact = next(
            person for person in state.contacts[state.active_region_id]
            if person.id == contact_id
        )
        return contact.name.upper(), [
            f"{contact.role}; disposition {contact.disposition:+d}; interest {contact.interest}.",
            "This local worker can mark a cache, teach practical knowledge, or treat an injury.",
            *[f"- {memory}" for memory in (contact.memories[-3:] or ["No shared service yet."])],
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
    if kind == "relic:select":
        rows = list(dict.fromkeys(
            item.kind.split(":", 1)[1] for item in state.items
            if item.owner_id == state.active_courier_id and item.location == "pack"
            and item.kind.startswith("relic:")
        ))
        return "SELECT CARRIED RELIC", [
            *[
                f"{index + 1}. {'>' if state.carried_relic == name else ' '} "
                f"{name} — {RELICS[name]}"
                for index, name in enumerate(rows)
            ],
            "Selection costs no time; X then spends the selected finite relic.",
        ]
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
    if kind == "bartender":
        schedule = state.actor_schedules.get(state.bartender.id)
        return state.bartender.name.upper(), [
            f"Bartender; {schedule.activity if schedule else 'between duties'}.",
            state.bartender.background,
            f"Opinion of courier: {state.bartender.relationships.get(state.active_courier_id or '', 0):+d}",
            "Sena's stock follows region arrivals, counted supplies, and season.",
            "D. Browse drinks", "S. Choose crew support", "L. Leave the bar",
        ]
    if kind == "bartender:drinks":
        lines = [f"Credit: {state.trade_credit}; {calendar_at(state).season} stock"]
        for index, (drink_id, drink) in enumerate(DRINKS.items()):
            lines.append(f"{index + 1}. {drink.name} — {drink.benefit}; drawback: {drink.drawback}; stock {state.bartender_stock.get(drink_id, 0)}")
        return "SENA'S COUNTED DRINKS", lines + ["Select a drink to inspect drinking or bottling."]
    if kind.startswith("bartender:drink:"):
        drink = DRINKS[kind.split(":", 2)[2]]
        return drink.name.upper(), [
            f"Benefit: {drink.benefit}.", f"Drawback: {drink.drawback}.",
            f"Duration: {drink.duration} actions; cost {drink.cost} credit.",
            f"Current stock: {state.bartender_stock.get(drink.id, 0)}.",
            "D. Drink now", "B. Buy a physical bottle",
        ]
    if kind == "incident" and state.pending_incident:
        people = {person.id: person.name for person in state.household}
        names = " and ".join(people.get(actor, actor) for actor in state.pending_incident.participants)
        return "TAVERN INCIDENT", [
            f"{names}: {state.pending_incident.kind}.",
            f"Cause: {state.pending_incident.cause}.",
            "M. Mediate", "S. Support the first speaker", "F. Let the bounded fight run",
            "No option can cause routine off-screen death.",
        ]
    if kind == "route-stop":
        node = state.route_nodes[state.route_current_node]
        return node.name.upper(), [
            node.description,
            f"Provision lots: {state.vessel_changes.get(f'supply_available:{node.id}', 0)}; market interest: {node.market_interest or 'none'}.",
            "R. Resupply", "T. Trade", "S. Take soundings",
            "Each accepted service advances only the action clock.",
        ]
    if kind == "objective":
        alter = "available" if state.gear == "repair tools" or state.support in {"route survey", "carpenter rig"} or (state.courier and state.courier.technique == "lever craft") or state.contact.disposition >= 2 else "needs tools, support, lever craft, or trust"
        return state.contact.name.upper(), [state.region.pressure, state.region.objective_text, "A. Accept cargo recovery", "R. Refuse", f"T. Alter to mill-control repair ({alter})", "Escape cancels without time."]
    if kind == "merchant":
        schedule = state.actor_schedules.get(state.merchant.id)
        lines = [
            f"{state.merchant.name}, {state.merchant.role}; "
            f"{schedule.activity if schedule else 'between recorded routes'}.",
            state.merchant.background,
            f"Opinion of courier: {state.merchant.relationships.get(state.active_courier_id or '', 0):+d}.",
            f"Jomon credit: {state.trade_credit}",
        ]
        for index, item in enumerate(state.merchant_stock):
            cost = max(1, MERCHANT_ITEMS[item][0] - (1 if state.support == "factor surety" else 0))
            lines.append(f"{index + 1}. {item} — {cost} credit")
        return state.merchant.name.upper(), lines + ["Number buys; Escape closes. Stock leaves on departure."]
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
        from .ship_crises import crisis_lines
        return "VOYAGE DANGER", crisis_lines(state)
    if kind.startswith("ship-work:"):
        from .ship_crises import work_lines
        return "COUNTED VESSEL WORK", work_lines(state, kind.split(":", 1)[1])
    if kind == "quit":
        return "QUIT JOMON?", ["Press Y to quit. Press N or Escape to continue."]
    if kind.startswith("person:"):
        from .people import person_by_id

        person_id = kind.split(":", 1)[1]
        person = person_by_id(state, person_id)
        if person is None:
            return "EMPTY SEAT", ["This person is no longer aboard."]
        standing = "active courier" if person.id == state.active_courier_id else "eligible household" if person in state.household else state.visitor_status.get(person.id, "visitor")
        physical = []
        for slot in ("readied", "secondary", "head", "torso", "arms", "hands", "legs", "feet"):
            item = equipped_item(state, slot, person.id)
            if item:
                physical.append(f"{slot}: {item_spec(item.kind).name}")
        lines = [
            f"{person.name} — {person.role}; {standing}",
            f"Technique: {person.technique}",
            f"Health: {person.health}/{person.max_health}; {person.injury}",
            f"Equipment affinity: {', '.join(person.equipment)}",
            f"Current physical kit: {', '.join(physical) if physical else 'none'}",
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
    if kind == "material" and key in map(ord, "12345"):
        dx, dy = ((0, 0), (0, -1), (1, 0), (0, 1), (-1, 0))[key - ord("1")]
        point = Position(state.position.x + dx, state.position.y + dy, state.position.z)
        return f"material:{point.x},{point.y},{point.z}", False
    if kind.startswith("material:") and ord("a") <= key <= ord("k"):
        from .materials import VERBS, handle_material, point_at

        changed, message = handle_material(state, VERBS[key - ord("a")], point_at(kind.split(":", 1)[1]))
        state.add_message(message, priority=3)
        return (None if changed else kind), False
    char = chr(key).lower() if 0 <= key < 256 else ""
    if key == 27:
        if kind.startswith("workshop:"):
            return "station:workshop", False
        if kind.startswith("bartender:"):
            return "bartender", False
        return ("bartender" if kind.startswith("tavern:") else None), False
    if kind in {"help", "inventory", "equipment", "household", "hold", "contact", "info", "chronicle", "regional-ledger", "observed-life"} or kind.startswith("contact:"):
        return None, False
    if kind == "quit":
        if char == "y":
            return None, True
        if char == "n":
            return None, False
        return kind, False
    if kind == "station:workshop":
        from .workshop import SLOTS

        if char in "12345678" and equipped_item(state, SLOTS[int(char) - 1]):
            return "workshop:slot:" + SLOTS[int(char) - 1], False
        return ("workshop:store" if char == "p" else kind), False
    if kind.startswith("ship-work:"):
        if char == "b":
            return None, False
        if char == "f":
            from .ship_crises import work
            changed, message = work(state, kind.split(":", 1)[1])
            state.add_message(message, priority=3)
            return (None if changed else kind), False
        return kind, False
    if kind.startswith("workshop:"):
        from .workshop import FITTINGS, buy_kit, install, remove, repair

        if char == "b":
            # B is a catalogue key at the store/slot, and Back only in previews.
            if not (kind == "workshop:store" or kind.startswith("workshop:slot:")):
                return "station:workshop", False
        if kind == "workshop:store" and char in "abcdefgh":
            return "workshop:buy:" + list(FITTINGS)[ord(char) - ord("a")], False
        if kind.startswith("workshop:slot:"):
            target = equipped_item(state, kind.split(":", 2)[2])
            if target is None:
                return "station:workshop", False
            if char in "abcdefgh":
                return f"workshop:fit:{target.id}:" + list(FITTINGS)[ord(char) - ord("a")], False
            if char in "uvw":
                return f"workshop:remove:{target.id}:" + {"u": "structure", "v": "treatment", "w": "lining"}[char], False
            if char == "r":
                return f"workshop:repair:{target.id}", False
        elif char == "f":
            parts = kind.split(":")
            changed, message = (buy_kit(state, parts[2]) if parts[1] == "buy" else install(state, parts[2], parts[3]) if parts[1] == "fit" else remove(state, parts[2], parts[3]) if parts[1] == "remove" else repair(state, parts[2]))
            state.add_message(message, priority=3)
            return ("station:workshop" if changed else kind), False
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
            return "bartender", False
    if kind == "tavern:relic" and char.isdigit():
        rows = [name for name in RELICS if state.relics.get(name, 0)]
        index = int(char) - 1
        if char == "0":
            choose_relic(state, None)
            return "tavern", False
        if 0 <= index < len(rows):
            choose_relic(state, rows[index])
            return "tavern", False
    if kind == "relic:select" and char.isdigit():
        rows = list(dict.fromkeys(
            item.kind.split(":", 1)[1] for item in state.items
            if item.owner_id == state.active_courier_id and item.location == "pack"
            and item.kind.startswith("relic:")
        ))
        index = int(char) - 1
        if 0 <= index < len(rows):
            state.carried_relic = rows[index]
            state.add_message(f"Selected {rows[index]} for the next finite use.")
            return None, False
    if kind == "tavern:passive" and char in "123456789abc":
        rows = list(state.owned_passives)
        index = "123456789abc".index(char)
        if index < len(rows):
            choose_passive(state, rows[index])
        return kind, False
    if kind == "objective" and char in {"a", "r", "t"}:
        result = decide_objective(state, {"a": "accept", "r": "refuse", "t": "alter"}[char])
        return (None if result.changed else kind), False
    if kind == "quest:regional" and char:
        result = resolve_regional_quest_choice(state, char)
        return (None if result.changed else kind), False
    if kind == "quest:arc" and char:
        result = resolve_cross_region_choice(state, char)
        return (None if result.changed else kind), False
    if kind.startswith("contact-service:") and char in {"c", "t", "h", "d", "s"}:
        result = use_contact_service(state, char)
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
        from .ship_crises import choices
        allowed = {key.lower() for key, _, _ in choices(state)}
        commands = {"p": "deck", "r": "repel", "d": "distract", "y": "yield", "e": "evade", "b": "bait", "a": "anchor", "c": "counsel", "n": "navigate"}
        if char in allowed:
            changed, message = resolve_voyage(state, commands[char])
            state.add_message(message, priority=3)
            return (None if changed else kind), False
    if kind == "bartender":
        if char == "d":
            return "bartender:drinks", False
        if char == "s":
            return "tavern:support", False
        if char == "l":
            return None, False
    if kind == "bartender:drinks" and char.isdigit():
        index = int(char) - 1
        rows = list(DRINKS)
        if 0 <= index < len(rows):
            return f"bartender:drink:{rows[index]}", False
    if kind.startswith("bartender:drink:") and char in {"d", "b"}:
        drink_id = kind.split(":", 2)[2]
        purchase_bar_drink(state, drink_id, bottle=char == "b")
        return "bartender:drinks", False
    if kind == "incident" and char in {"m", "s", "f"}:
        intervene_socially(state, {"m": "mediate", "s": "side-first", "f": "let-fight"}[char])
        return None, False
    if kind == "route-stop" and char in {"r", "t", "s"}:
        use_route_stop(state, {"r": "resupply", "t": "trade", "s": "sound"}[char])
        return kind, False
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
    try:
        curses.curs_set(0)
    except curses.error:
        pass
    screen.keypad(True)
    _init_colours()
    _enable_mouse()
    overlay: OverlayView | None = None
    inventory_view: InventoryView | None = None
    route_view: RouteChartView | None = None
    target_view: TargetView | None = None
    while True:
        _draw_base(screen, state)
        height, width = screen.getmaxyx()
        if inventory_view and height >= MIN_HEIGHT and width >= MIN_WIDTH:
            _draw_inventory(screen, state, inventory_view)
        elif route_view and height >= MIN_HEIGHT and width >= MIN_WIDTH:
            _draw_route_chart(screen, state, route_view)
        elif target_view and height >= MIN_HEIGHT and width >= MIN_WIDTH:
            _draw_targeting(screen, state, target_view)
        elif overlay and height >= MIN_HEIGHT and width >= MIN_WIDTH:
            if dialogue_choices(state, overlay.kind):
                _draw_dialogue_overlay(screen, state, overlay)
            else:
                title, lines = _overlay_lines(state, overlay.kind)
                _overlay(screen, title, lines, overlay)
        event = normalise_input(screen.getch())
        key = event.key
        if key == curses.KEY_RESIZE:
            continue
        if height < MIN_HEIGHT or width < MIN_WIDTH:
            if key in {ord("q"), ord("Q")}:
                return state
            continue
        if inventory_view:
            closed, committed = _handle_inventory(state, inventory_view, event)
            if closed:
                if committed and inventory_view.transaction.changed:
                    if state.combat_active and inventory_view.source is None:
                        _advance_world(state)
                        state.add_message("You complete one deliberate field repack.", priority=2)
                    else:
                        state.add_message("The physical load is arranged and accounted for.")
                inventory_view = None
            continue
        if route_view:
            closed, next_overlay, animation = _handle_route_chart(state, route_view, event)
            if animation:
                _animate_route(screen, state, *animation)
            if closed:
                route_view = None
                overlay = OverlayView(next_overlay) if next_overlay else None
            continue
        if target_view:
            closed, _ = _handle_targeting(
                state, target_view, event, screen_size=(height, width)
            )
            if closed:
                target_view = None
            continue
        if overlay:
            closed, should_quit = _handle_overlay_view(state, overlay, event)
            if should_quit:
                return state
            if closed:
                overlay = None
            continue
        normalized = ord(chr(key).lower()) if 0 <= key < 256 else key
        if normalized in MOVES:
            move(state, *MOVES[normalized])
        elif normalized in {10, 13, ord("e")}:
            result = interact(state)
            if result.overlay and result.overlay.startswith("inventory:container:"):
                source = result.overlay.split("inventory:", 1)[1]
                inventory_view = InventoryView.begin(state, source)
            elif result.overlay == "route-chart":
                route_view = RouteChartView.begin(state)
            else:
                overlay = OverlayView(result.overlay) if result.overlay else None
        elif normalized == ord("a"):
            if state.combat_active and state.weapon in RANGED_WEAPONS:
                target_view = TargetView.begin(state)
            else:
                attack(state)
        elif normalized == ord("g"):
            guard(state)
        elif normalized == ord("x"):
            carried_relics = list(dict.fromkeys(
                item.kind.split(":", 1)[1] for item in state.items
                if item.owner_id == state.active_courier_id and item.location == "pack"
                and item.kind.startswith("relic:")
            ))
            if len(carried_relics) > 1:
                overlay = OverlayView("relic:select")
            else:
                use_gear(state)
        elif normalized == ord("v"):
            negotiate(state)
        elif normalized == ord("r"):
            retreat(state)
        elif normalized == ord("i"):
            inventory_view = InventoryView.begin(state)
        elif normalized == ord("f"):
            overlay = OverlayView("material")
        elif normalized == ord("z"):
            overlay = OverlayView("regional-ledger")
        elif normalized == ord("o"):
            overlay = OverlayView("observed-life")
        elif normalized == ord("?"):
            overlay = OverlayView("help")
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
            overlay = OverlayView("quit")
