"""Fullscreen curses presentation; gameplay remains in direct action functions."""

from __future__ import annotations

import curses
from dataclasses import dataclass
from typing import Iterable

from .actions import (
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
    retreat,
    use_gear,
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
    if aboard and glyph == "s":
        return "interactable"
    if glyph in {"M", "c", "$"}:
        return "neutral"
    if glyph in {"h", "g", "x", "b", "!"}:
        return "hostile"
    if glyph == "X":
        return "elite"
    if glyph == "~":
        return "water"
    if glyph == "#":
        return "structure"
    if glyph in {"<", ">", "^", "v", "+"}:
        return "exit"
    if glyph in {"R", "r"}:
        return "cargo"
    if glyph in {"&", "D", "O", "o", "?", "C", "L", "P", "H", "s"}:
        return "interactable"
    if glyph in {"m", "%", "=", "s"}:
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


def _draw_map(screen: curses.window, state: GameState, top: int, left: int, height: int, width: int) -> None:
    rows = map_rows(state)
    view_height, view_width = height - 2, width - 2
    map_height, map_width = len(rows), max(map(len, rows))
    origin_x, origin_y = camera_origin(
        state.position, map_width, map_height, view_width, view_height
    )
    visible = field_of_view(state, remember=False) if state.location == "region" else set()
    threats = {}
    if state.location == "region":
        threats = {
            threat.position: threat
            for threat in state.threats
            if threat.status in {"watching", "engaged"}
            and threat.position.z == state.position.z
            and threat.position in visible
        }
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
    lines = [
        "COURIER",
        identity,
        f"Health {health}; {injury}",
        f"{state.weapon or '-'} / {state.gear or '-'}",
        f"Technique: {_clip(technique, 16)}",
        "PRESSURE",
        f"Time {p.elapsed}; depth {p.depth}",
        f"Noise {p.noise}; value {p.valuables}",
        f"{p.band} {p.score}; {state.weather}",
        "HEARTHFORD",
        f"Level {state.position.z:+d}; {state.objective_status}",
        f"{state.region.objective_commodity}: {market.stock}/{market.demand}",
        f"Ammo {state.ammunition}; oil {state.lamp_oil}",
        f"Rope {state.rope_uses}; smoke {state.smoke_charges}",
        _clip(threat, 25),
    ]
    if build_combinations(state):
        lines.append(f"Combo: {_clip(build_combinations(state)[0], 20)}")
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


def _tavern_lines(state: GameState) -> list[str]:
    courier = state.courier
    identity = f"{courier.name}, {courier.role}; health {courier.health}/{courier.max_health}; {courier.injury}; {courier.technique}" if courier else "none selected"
    goods = ", ".join(f"{name} {stack.quantity}" for name, stack in state.carried_goods.items()) or "none"
    combos = ", ".join(build_combinations(state)) or "none active"
    passives = ", ".join(state.carried_passives) or "none"
    return [
        f"C Courier: {identity}", f"W Weapon: {state.weapon or 'none'}", f"G Gear/tool: {state.gear or 'none'}",
        f"S Crew support: {state.support or 'none'}", f"R Relic: {state.carried_relic or 'none'}",
        f"D Discoveries: {passives} ({passive_bulk(state)}/{passive_capacity(state)} bulk)",
        f"Build interactions: {combos}", f"Cargo: {goods}; capacity {carried_bulk(state)}/{capacity(state)}",
        "", state.region.condition, state.region.objective_text, f"Objective: {state.objective_status}",
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
    if kind == "contact":
        memories = state.contact.memories or ["No significant shared event yet."]
        return state.contact.name.upper(), [
            f"Role: {state.contact.role}; disposition: {state.contact.disposition:+d}",
            f"Material interest: {state.contact.interest}", f"Objective: {state.objective_status}",
            "Significant memories:", *[f"- {memory}" for memory in memories], "Escape closes without time.",
        ]
    if kind == "tavern":
        return "TAVERN EXPEDITION PREPARATION", _tavern_lines(state)
    if kind == "tavern:courier":
        living = [person for person in state.household if person.alive]
        return "SELECT COURIER", [f"{index + 1}. {person.name} — {person.role}; {person.health}/{person.max_health}; {person.injury}; {person.technique}" for index, person in enumerate(living)] + ["Number selects; Escape returns."]
    if kind == "tavern:weapon":
        rows = [(key, WEAPONS[key]) for key in WEAPONS if key in state.owned_weapons]
        return "SELECT WEAPON", [f"{index + 1}. {name} — {detail}" for index, (_, (name, detail)) in enumerate(rows)] + ["Number selects; Escape returns."]
    if kind == "tavern:gear":
        rows = [(key, GEAR[key]) for key in GEAR if key in state.owned_gear]
        return "SELECT SECONDARY GEAR", [f"{index + 1}. {name} — {detail}" for index, (_, (name, detail)) in enumerate(rows)] + ["Number selects; Escape returns."]
    if kind == "tavern:support":
        return "SELECT CREW SUPPORT", [f"{index + 1}. {name} — {detail}" for index, (name, detail) in enumerate(SUPPORTS.values())] + ["Number selects; Escape returns."]
    if kind == "tavern:relic":
        rows = [name for name in RELICS if state.relics.get(name, 0)]
        return "SELECT FINITE RELIC", ["0. Carry none", *[f"{index + 1}. {name} ({state.relics[name]}) — {RELICS[name]}" for index, name in enumerate(rows)], "Number selects; Escape returns."]
    if kind == "tavern:passive":
        rows = list(state.owned_passives)
        keys = "123456789abc"
        lines = [
            f"{keys[index]}. {'[x]' if name in state.carried_passives else '[ ]'} {name} "
            f"({PASSIVES[name][0]} bulk) — {PASSIVES[name][1]}"
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
    if kind == "quit":
        return "QUIT JOMON?", ["Press Y to quit. Press N or Escape to continue."]
    return "INFORMATION", [kind, "Escape closes without advancing time."]


def _handle_overlay(state: GameState, kind: str, key: int) -> tuple[str | None, bool]:
    char = chr(key).lower() if 0 <= key < 256 else ""
    if key == 27:
        return ("tavern" if kind.startswith("tavern:") else None), False
    if kind in {"help", "inventory", "equipment", "household", "hold", "contact", "info"}:
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
            "c": "tavern:courier",
            "w": "tavern:weapon",
            "g": "tavern:gear",
            "s": "tavern:support",
            "r": "tavern:relic",
            "d": "tavern:passive",
        }.get(char, kind)), False
    if kind == "tavern:courier" and char.isdigit():
        living = [person for person in state.household if person.alive]
        index = int(char) - 1
        if 0 <= index < len(living):
            choose_courier(state, living[index].id)
            return "tavern", False
    if kind == "tavern:weapon" and char.isdigit():
        rows = [key for key in WEAPONS if key in state.owned_weapons]
        index = int(char) - 1
        if 0 <= index < len(rows):
            choose_weapon(state, rows[index])
            return "tavern", False
    if kind == "tavern:gear" and char.isdigit():
        rows = [key for key in GEAR if key in state.owned_gear]
        index = int(char) - 1
        if 0 <= index < len(rows):
            choose_gear(state, rows[index])
            return "tavern", False
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
            overlay = interact(state).overlay
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
