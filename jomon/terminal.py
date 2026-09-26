"""Fullscreen curses presentation; gameplay remains in direct action functions."""

from __future__ import annotations
from .ecology_presentation import ecology_actor_capability, ecology_actor_counterplay

import curses
import os
import signal
import textwrap
from dataclasses import dataclass
from typing import Iterable

from .actions import (
    BRACE_REACTION_WEAPONS,
    RANGED_WEAPONS,
    _advance_world,
    attack,
    attack_target_legality,
    brace_target_legality,
    can_alter_objective,
    choose_courier,
    choose_passive,
    choose_relic,
    choose_support,
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
    use_aftermath_contract,
    use_gear,
    use_route_stop,
)
from .character_presentation import role_display_name
from .item_presentation import item_display_name, item_display_name_or_legacy
from .ui_presentation import notice_kind, notice_label, render_notice, ui_format
from .inventory import (
    BODY_SLOTS,
    WEAPON_AMMUNITION,
    InventoryTransaction,
    auto_pack,
    auto_place,
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
    HELP_LINES,
    INTERFACE_LEDGERS,
    INTERFACE_LABELS,
    MERCHANT_ITEMS,
    PASSIVES,
    RELICS,
    SUPPORTS,
    WEAPONS,
)
from .save import SaveError, save_game
from .navigation import (
    RoutePlan,
    RouteUnavailable,
    advance_route,
    navigation_targets,
    plan_route,
)
from .state import GameState, MaterialCell, Position, Threat
from .travel import DESTINATIONS, choose_destination, resolve_voyage, travel_animation_frames
from .route_chart import chart_move, edge_between, neighbours, route_availability
from .calendar import calendar_at, seasonal_route_note
from .vessel import DRINKS
from .vessel_presentation import drink_benefit, drink_display_name, drink_drawback, schedule_display_name, vessel_format
from .visuals import (
    ENTITY_GLYPHS, PHYSICAL_ROLE_OVERRIDES, REGIONAL_GROUND_ROLES,
    REGIONAL_TILE_ROLES, ROUTE_NODE_SYMBOLS, SEMANTIC_GLYPH_ROLES,
    THREAT_PROFILE_GLYPHS,
)
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
    map_rows,
    passive_bulk,
    passive_capacity,
    projectile_path,
)

INVENTORY_HELP_LINES = INTERFACE_LEDGERS["inventory"]
TARGET_HELP_LINE = INTERFACE_LEDGERS["target"]
ROUTE_HELP_LINES = INTERFACE_LEDGERS["route"]
BASE_HELP_LINES = INTERFACE_LEDGERS["base"]
LOOK_HELP_LINE = INTERFACE_LEDGERS["look"]

MIN_WIDTH = 80
MIN_HEIGHT = 24

SEMANTIC_ROLES = (
    # Actors and tactical selection.
    "player", "ally", "neutral", "hostile", "elite", "selected_target",
    "target_cell",
    # Terrain and construction. Regional ground roles make the eight major
    # landscapes visually distinct without changing their authoritative glyphs.
    "terrain", "hearthford_ground", "greywash_ground", "greenwold_ground",
    "whitecairn_ground", "dunmire_ground", "rillscar_ground",
    "marlbank_ground", "frostmere_ground", "vegetation", "road", "earth",
    "stone", "timber", "structure", "exit",
    # Sparse physical states. `water` and `hazard` remain compatibility roles
    # for older callers while production rendering uses the specific variants.
    "water", "shallow_water", "deep_water", "ice", "mud", "fire", "smoke",
    "collapse", "salt", "lime", "ash", "resin", "oil", "hazard",
    # Physical objects and services.
    "cargo", "interactable", "weapon", "armour", "tool", "technique",
    "consumable", "commodity", "relic", "mystical",
    # Presentation and provenance. Words and glyphs remain authoritative.
    "ui_frame", "ui_heading", "ui_accent", "fact", "rumour", "forecast",
    "warning", "success", "unavailable", "remembered",
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
    ammunition: str | None = None
    mastery_id: str | None = None
    spell_id: str | None = None

    @classmethod
    def begin(cls, state: GameState) -> "TargetView":
        targets = sorted(
            (
                threat for threat in state.combatants
                if attack_target_legality(state, threat)[0]
                or brace_target_legality(state, threat)[0]
            ),
            key=lambda threat: (distance(state.position, threat.position), threat.id),
        )
        from .work_weapons import available_pots
        pots = available_pots(state) if state.weapon == "pot sling" else []
        return cls(
            targets[0].position if targets else state.position,
            [target.id for target in targets],
            ammunition=pots[0] if pots else None,
        )

    @classmethod
    def begin_spell(cls, state: GameState, spell_id: str) -> "TargetView":
        from .magic import SPELLS, spell_status

        spell = SPELLS[spell_id]
        targets = [actor for actor in state.combatants if spell_status(state, spell_id, actor.position)[0]] if spell.target == "enemy" else []
        targets.sort(key=lambda actor: (distance(state.position, actor.position), actor.id))
        return cls(targets[0].position if targets else state.position,
                   [actor.id for actor in targets], spell_id=spell_id)


@dataclass
class LookView:
    cursor: Position

    @classmethod
    def begin(cls, state: GameState) -> "LookView":
        return cls(state.position)


@dataclass
class CircuitView:
    cursor: Position
    layer: str = "surface"

    @classmethod
    def begin(cls, state: GameState) -> "CircuitView":
        return cls(state.position)


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
    result: str | None = None
    cursor: Position | None = None


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


def _disable_mouse() -> None:
    try:
        curses.mousemask(0)
    except curses.error:
        pass


def suspend_terminal(screen: curses.window, *, stop=None) -> bool:
    """Suspend on POSIX and restore curses state after SIGCONT."""
    if not hasattr(signal, "SIGTSTP"):
        return False
    stop = stop or (lambda: os.kill(os.getpid(), signal.SIGTSTP))
    try:
        curses.def_prog_mode()
        curses.endwin()
        stop()
    except (curses.error, OSError):
        return False
    finally:
        try:
            curses.reset_prog_mode()
            screen.keypad(True)
            screen.timeout(-1)
            screen.refresh()
        except curses.error:
            pass
    return True


def semantic_colour_plan(colour_count: int, pair_count: int) -> dict[str, ColourStyle]:
    """Return a bounded 256-, 16-, 8-colour, or monochrome semantic plan."""
    bold_roles = {
        "player", "ally", "hostile", "elite", "selected_target", "target_cell",
        "exit", "fire", "collapse", "hazard", "cargo", "interactable", "relic",
        "mystical", "ui_heading", "ui_accent", "warning", "success",
    }
    # The seven family colours are deliberately first in allocation order. If
    # a terminal reports few pairs, later shades fall back to an allocated
    # family instead of silently losing all colour.
    families = {
        "red": 1, "yellow": 3, "cyan": 6, "green": 2,
        "magenta": 5, "blue": 4, "white": 7,
    }
    family_for = {
        "player": "yellow", "ally": "cyan", "neutral": "green",
        "hostile": "red", "elite": "magenta", "selected_target": "magenta",
        "target_cell": "yellow", "terrain": "white",
        "hearthford_ground": "green", "greywash_ground": "white",
        "greenwold_ground": "green", "whitecairn_ground": "white",
        "dunmire_ground": "yellow", "rillscar_ground": "red",
        "marlbank_ground": "yellow", "frostmere_ground": "cyan",
        "vegetation": "green", "road": "yellow", "earth": "yellow",
        "stone": "white", "timber": "yellow", "structure": "white",
        "exit": "cyan", "water": "blue", "shallow_water": "cyan",
        "deep_water": "blue", "ice": "cyan", "mud": "yellow",
        "fire": "red", "smoke": "white", "collapse": "red", "salt": "cyan",
        "lime": "yellow", "ash": "white", "resin": "yellow", "oil": "red",
        "hazard": "red", "cargo": "yellow", "interactable": "green",
        "weapon": "red", "armour": "cyan", "tool": "green",
        "technique": "magenta", "consumable": "green", "commodity": "yellow",
        "relic": "magenta", "mystical": "magenta", "ui_frame": "blue",
        "ui_heading": "cyan", "ui_accent": "yellow", "fact": "cyan",
        "rumour": "magenta", "forecast": "cyan", "warning": "red",
        "success": "green", "unavailable": "white", "remembered": "white",
    }
    desired = {role: families[family_for[role]] for role in SEMANTIC_ROLES}
    if colour_count >= 256:
        desired.update({
            "player": 220, "ally": 45, "neutral": 114, "hostile": 196,
            "elite": 201, "selected_target": 207, "target_cell": 226,
            "terrain": 250, "hearthford_ground": 149, "greywash_ground": 252,
            "greenwold_ground": 71, "whitecairn_ground": 255,
            "dunmire_ground": 100, "rillscar_ground": 166,
            "marlbank_ground": 173, "frostmere_ground": 153,
            "vegetation": 70, "road": 180, "earth": 137, "stone": 245,
            "timber": 130, "structure": 250, "exit": 51, "water": 33,
            "shallow_water": 45, "deep_water": 27, "ice": 117, "mud": 94,
            "fire": 208, "smoke": 247, "collapse": 202, "salt": 159,
            "lime": 229, "ash": 244, "resin": 172, "oil": 160,
            "hazard": 196, "cargo": 221, "interactable": 48,
            "weapon": 203, "armour": 110, "tool": 84, "technique": 141,
            "consumable": 213, "commodity": 221, "relic": 207,
            "mystical": 135, "ui_frame": 60, "ui_heading": 81,
            "ui_accent": 220, "fact": 117, "rumour": 183, "forecast": 81,
            "warning": 203, "success": 84, "unavailable": 244,
            "remembered": 242,
        })
    elif colour_count >= 16:
        # Bright ANSI indices are standardized by the terminal's reported
        # palette; no colour redefinition is attempted.
        bright = {
            "red": 9, "yellow": 11, "cyan": 14, "green": 10,
            "magenta": 13, "blue": 12, "white": 15,
        }
        desired = {role: bright[family_for[role]] for role in SEMANTIC_ROLES}
        desired.update({
            "hearthford_ground": 10, "greywash_ground": 7,
            "greenwold_ground": 2, "whitecairn_ground": 15,
            "dunmire_ground": 3, "rillscar_ground": 1,
            "marlbank_ground": 11, "frostmere_ground": 14,
            "vegetation": 2, "road": 11, "earth": 3, "stone": 7,
            "timber": 3, "structure": 15, "water": 12,
            "shallow_water": 6, "deep_water": 12, "ice": 14, "mud": 3,
            "fire": 9, "smoke": 8, "collapse": 1, "salt": 6,
            "lime": 11, "ash": 8, "resin": 3, "oil": 1,
            "weapon": 9, "armour": 14, "tool": 10, "technique": 13,
            "consumable": 2, "commodity": 11, "relic": 5,
            "ui_frame": 12, "remembered": 8,
        })
    if colour_count < 8 or pair_count <= 1:
        return {role: ColourStyle(0, None, role in bold_roles) for role in SEMANTIC_ROLES}
    core = [desired[next(role for role in SEMANTIC_ROLES if family_for[role] == family)] for family in ("red", "yellow", "cyan", "green", "magenta", "blue", "white")]
    priority = tuple(dict.fromkeys([*core, *(desired[role] for role in SEMANTIC_ROLES)]))
    available = priority[:max(0, min(len(priority), pair_count - 1))]
    pairs = {foreground: index + 1 for index, foreground in enumerate(available)}
    plan = {}
    for role in SEMANTIC_ROLES:
        foreground = desired[role]
        if foreground not in pairs:
            fallback = desired[next(candidate for candidate in SEMANTIC_ROLES if family_for[candidate] == family_for[role])]
            foreground = fallback if fallback in pairs else foreground
        plan[role] = ColourStyle(
            pairs.get(foreground, 0), foreground if foreground in pairs else None,
            role in bold_roles,
        )
    return plan


def terrain_colour_role(
    glyph: str,
    region_id: str = "",
    *,
    aboard: bool = False,
    material: MaterialCell | None = None,
) -> str:
    """Classify a visible physical cell; glyphs remain the primary cue."""
    if material:
        if material.fire:
            return "fire"
        if material.collapse_due:
            return "collapse"
        if material.smoke >= 2:
            return "smoke"
        if material.ice:
            return "ice"
        if material.water:
            return "deep_water" if material.water >= 2 else "shallow_water"
        if material.coating in {"salt", "lime", "ash", "resin", "oil"}:
            return material.coating
    # These overwhelmingly common regional cells avoid a second general glyph
    # classification and keep the colour pass effectively constant-time.
    if not aboard:
        if glyph == ".":
            return REGIONAL_GROUND_ROLES.get(region_id, "terrain")
        tile_role = REGIONAL_TILE_ROLES.get(glyph)
        if tile_role:
            return tile_role
        override = PHYSICAL_ROLE_OVERRIDES.get(glyph)
        if override:
            return override
    role = semantic_role(glyph, aboard=aboard)
    override = PHYSICAL_ROLE_OVERRIDES.get(glyph)
    if override and role in {"water", "hazard"}:
        return override
    if role != "terrain" or aboard:
        return role
    return REGIONAL_GROUND_ROLES.get(region_id, "terrain")


def semantic_role(glyph: str, *, aboard: bool = False) -> str:
    return SEMANTIC_GLYPH_ROLES["aboard" if aboard else "region"].get(glyph, "terrain")


_COLOUR_ATTRIBUTES: dict[str, int] = {role: 0 for role in SEMANTIC_ROLES}


def colour_attribute(role: str) -> int:
    """Read the active palette without exposing its replaceable dictionary."""
    return _COLOUR_ATTRIBUTES.get(role, 0)


def _put(screen: curses.window, y: int, x: int, text: str, attr: int = 0) -> None:
    height, width = screen.getmaxyx()
    if 0 <= y < height and x < width - 1:
        try:
            screen.addnstr(y, max(0, x), text[max(0, -x):], max(0, width - max(0, x) - 1), attr)
        except curses.error:
            pass


def _line(screen: curses.window, y: int, x: int, width: int, title: str = "", attr: int | None = None) -> None:
    label = f" {title} " if title else ""
    if attr is None:
        attr = _COLOUR_ATTRIBUTES["ui_frame"]
    _put(screen, y, x, "+" + label + "-" * max(0, width - len(label) - 2) + "+", attr)


def _frame(screen: curses.window, y: int, x: int, height: int, width: int, title: str = "", attr: int | None = None) -> None:
    if attr is None:
        attr = _COLOUR_ATTRIBUTES["ui_frame"]
    _line(screen, y, x, width, title, attr)
    for row in range(y + 1, y + height - 1):
        _put(screen, row, x, "|", attr)
        _put(screen, row, x + width - 1, "|", attr)
    _line(screen, y + height - 1, x, width, attr=attr)


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


def information_colour_role(text: str) -> str:
    """Classify provenance-bearing prose; its prefix remains the primary cue."""
    upper = text.strip().upper()
    typed_notice = notice_kind(text)
    if typed_notice:
        return {"rumour": "rumour", "fact": "fact", "forecast": "forecast", "warning": "warning"}[typed_notice]
    if upper.startswith(("FACT", "OBSERVED", "CURRENTLY VISIBLE", "PHYSICAL KIT", "BODY:")):
        return "fact"
    if upper.startswith(("RUMOUR", "RUMOR", "CLAIM", "TESTIMONY", "INFERRED")):
        return "rumour"
    if upper.startswith(("FORECAST", "PREDICTION", "WEATHER", "SEASON:")):
        return "forecast"
    if upper.startswith(("WARNING", "DANGER", "BLOCKED", "G BLOCKED", "COLLAPSE", "UNLOADED", "UNREADY")):
        return "warning"
    if upper.startswith(("SUCCESS", "COMPLETE", "COMPLETED", "REACHABLE", "SAVED", "LEGAL", "G REACTION")):
        return "success"
    if upper.startswith(("EFFECT", "COUNTERS", "AIM SET")):
        return "technique"
    if upper.startswith(("REMEMBERED", "MEMORY")):
        return "remembered"
    if upper and upper == text.strip() and any(character.isalpha() for character in upper):
        return "ui_heading"
    return "terrain"


def event_colour_role(text: str) -> str:
    """Colour recent consequences while their full causal text stays visible."""
    explicit = information_colour_role(text)
    if explicit != "terrain":
        return explicit
    lower = text.lower()
    if any(word in lower for word in (
        "complete", "secured", "repaired", "recovers", "holds", "opened",
        "quenches", "saved", "negotiated", "surrenders", "succeeds",
    )):
        return "success"
    if any(word in lower for word in (
        "cannot", "blocked", "fails", "failed", "injur", "damage", "burn",
        "collapse", "destroyed", "drown", "strikes", "attacks", "danger",
    )):
        return "warning"
    return "terrain"


def _causal_event(text: str) -> bool:
    lower = text.lower()
    return any(word in lower for word in (
        "dies", "death", "killed", "fell to", "falls to", "injur", "wound",
        "destroyed", "lost", "collapse", "drown", "defeat", "surrender",
        "complete", "secured", "stolen", "retreat", "saved", "later echo",
    ))


def _routine_event_kind(text: str) -> str:
    lower = text.lower()
    if any(word in lower for word in ("moves toward", "investigates", "circles", "steps toward", "seeks cover")):
        return "movement"
    if any(word in lower for word in ("reload", "recovers", "holds without", "watches", "waits for")):
        return "readiness"
    if any(word in lower for word in ("smoke drifts", "water flows", "rain wets", "fire consumes")):
        return "material"
    return ""


def _event_feed_entries(messages: Iterable[str], width: int, max_rows: int) -> list[tuple[str, str]]:
    """Render persisted messages while retaining their engine-recognized source."""
    groups: list[tuple[str, int, str]] = []
    for message in messages:
        kind = "" if _causal_event(message) else _routine_event_kind(message)
        if kind and groups and groups[-1][0] == kind:
            _, count, _ = groups[-1]
            groups[-1] = (kind, count + 1, message)
        else:
            groups.append((kind, 1, message))
    rendered: list[tuple[str, str]] = []
    for kind, count, message in groups:
        text = f"ROUTINE {kind.upper()} x{count} — {message}" if kind and count > 1 else message
        rendered.extend((part, text) for part in _wrapped(render_notice(text), width))
    return rendered[-max_rows:]


def event_feed_lines(messages: Iterable[str], width: int, max_rows: int) -> list[str]:
    """Compact adjacent routine updates but retain exact causal consequences."""
    return [line for line, _ in _event_feed_entries(messages, width, max_rows)]


def status_colour_role(text: str) -> str:
    """Give the compact status rail visual grouping without hiding its labels."""
    stripped, lower = text.strip(), text.lower()
    if stripped and stripped == stripped.upper() and any(character.isalpha() for character in stripped):
        return "ui_heading"
    if lower.startswith("health "):
        try:
            current, maximum = map(int, lower.rsplit(" ", 1)[-1].split("/"))
            return "success" if current * 2 >= maximum else "warning"
        except ValueError:
            return "warning"
    if lower.startswith("role:"):
        return "technique"
    if lower.startswith("load "):
        try:
            current, maximum = map(int, lower.split()[1].split("/"))
            return "warning" if current > maximum else "cargo"
        except (IndexError, ValueError):
            return "cargo"
    if lower.startswith("location:"):
        return "ui_heading"
    return "terrain"


def _init_colours() -> None:
    global _COLOUR_ATTRIBUTES
    try:
        if not curses.has_colors():
            plan = semantic_colour_plan(0, 0)
            _COLOUR_ATTRIBUTES = {
                role: curses.A_BOLD if style.bold else 0
                for role, style in plan.items()
            }
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
        return ENTITY_GLYPHS["elite"]
    if threat.glyph:
        return threat.glyph
    return THREAT_PROFILE_GLYPHS[threat.profile]


def observed_life_lines(state: GameState) -> list[str]:
    from .encounters import threat_definition
    from .enemy_equipment import actor_items, readied_weapon
    from .inventory import item_spec
    from .ship_crisis_presentation import crisis_goal_display

    from .combat_forecast import forecast_lines, observed_forecasts

    visible = field_of_view(state, remember=False)
    forecasts = {row.actor_id: row for row in observed_forecasts(state, visible)}
    lines = [f"{notice_label('fact')}: only presently visible actors are listed. No inspection advances time."]
    for actor in sorted(state.combatants, key=lambda a: (distance(a.position, state.position), a.id)):
        if not courier_sees(state, actor.position) or actor.status not in {"watching", "engaged"}:
            continue
        data = threat_definition(actor)
        if actor.id == f"sanctum:{state.active_region_id}:boss":
            from .sanctum_presentation import sanctum_boss_goal, sanctum_text
            goal = sanctum_boss_goal(state.active_region_id)
            reason = sanctum_text(f"sanctum.{state.active_region_id}.boss.capability")
        else:
            goal = crisis_goal_display(actor.goal)
            reason = actor.goal_reason
        lines.extend((
            "", f"{_threat_glyph(actor)} {actor.name}; {actor.position.x},{actor.position.y} z{actor.position.z:+d}; {actor.health}/{actor.max_health} health.",
            f"OBSERVED INTENT: {actor.intent}.",
            f"Duty: {goal}; {reason}.",
            f"Working charges {actor.supplies}; recovery {actor.reload_turns}; morale {actor.morale}." if actor.id.startswith("frontier-elite:") else f"Readied {actor.ranged_kind}; ammunition {actor.ammunition}, reload {actor.reload_turns}." if actor.profile == "ranged" else f"Role: {actor.role}; morale {actor.morale}; supplies {actor.supplies}.",
        ))
        if actor.uses_physical_equipment:
            weapon = readied_weapon(state, actor)
            armour = [
                f"{item_spec(item.kind).name} {item.condition}%"
                for item in actor_items(state, actor)
                if item.location in {"head", "torso", "arms", "hands", "legs", "feet"}
            ]
            lines.append(
                "PHYSICAL KIT: "
                + (f"{item_spec(weapon.kind).name} {weapon.condition}%" if weapon else "unarmed")
                + (f"; protection {', '.join(armour)}" if armour else "; no worn protection")
                + "."
            )
            lines.append(
                "BODY: "
                + (", ".join(f"{slot} {injury}" for slot, injury in sorted(actor.injuries.items())) or "no observed injury")
                + "; conditions "
                + (", ".join(f"{name} {turns}" for name, turns in sorted(actor.conditions.items())) or "none")
                + "."
            )
        if data:
            lines.extend((f"KNOWN PRACTICE: {ecology_actor_capability(actor.archetype_id or '', str(data['capability']))}.", f"COUNTERS: {ecology_actor_counterplay(actor.archetype_id or '', str(data['counterplay']))}."))
        forecast = forecasts.get(actor.id)
        if forecast:
            lines.extend(forecast_lines(forecast)[1:])
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
    from .combat_forecast import danger_cells

    return danger_cells(state, visible)


def _draw_map(screen: curses.window, state: GameState, top: int, left: int, height: int, width: int) -> None:
    from .materials import fields, key
    from .circuits import active as circuit_active, space_id

    material_cells = fields(state)
    rows = map_rows(state)
    view_height, view_width = height - 2, width - 2
    map_height, map_width = len(rows), max(map(len, rows))
    origin_x, origin_y = camera_origin(
        state.position, map_width, map_height, view_width, view_height
    )
    visible = field_of_view(state, remember=False)
    threats = visible_threats(state, visible)
    from .vehicles import SPECS

    vehicle_region = "harbour" if state.location == "jomon" and state.jomon_space == "harbour" else state.active_region_id if state.location == "region" else None
    vehicle_marks = {vehicle.position: vehicle for vehicle in state.vehicles.values() if vehicle.region_id == vehicle_region}
    circuit_marks = {cell.position: cell for cell in state.circuits.values()
                     if cell.space == space_id(state) and cell.layer == "surface" and cell.position.z == state.position.z}
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
            vehicle = vehicle_marks.get(position)
            if position == state.position:
                char = SPECS[vehicle.id]["glyph"] if vehicle and state.active_vehicle_id == vehicle.id else ENTITY_GLYPHS["courier"]
            elif position in threats:
                char = _threat_glyph(threats[position])
            elif vehicle:
                char = SPECS[vehicle.id]["glyph"]
            elif position in danger_marks:
                char = ENTITY_GLYPHS["danger"]
            else:
                char = displayed_tile(state, position)
            cell = material_cells.get(key(position)) if position != state.position and position not in threats else None
            role = terrain_colour_role(
                char, state.active_region_id,
                aboard=state.location == "jomon", material=cell,
            )
            if position in threats and position != state.position:
                actor = threats[position]
                role = "elite" if actor.elite else "neutral" if actor.ecology == "prey" else "hostile"
            elif vehicle:
                role = "player" if state.active_vehicle_id == vehicle.id and position == state.position else "interactable"
            elif position in circuit_marks and position != state.position and position not in danger_marks:
                cell = circuit_marks[position]
                role = "ui_accent" if cell.phase == "head" else "warning" if cell.phase == "tail" else "success" if circuit_active(state, cell) else "interactable"
            attr = _COLOUR_ATTRIBUTES[role]
            if position in danger_marks:
                attr = _COLOUR_ATTRIBUTES["hazard"] | curses.A_BOLD | curses.A_REVERSE
            if position in threats and threats[position].profile == "ranged":
                attr |= curses.A_UNDERLINE
            if state.combat_active and position not in visible:
                attr = curses.A_DIM
            _put(screen, top + 1 + sy, left + 1 + sx, char, attr)


def _status_lines(state: GameState, capacity: int | None = None) -> list[str]:
    courier = state.courier
    if courier:
        maximum = max(1, courier.max_health)
        filled = min(8, max(0, (courier.health * 8 + maximum - 1) // maximum))
        health = f"Health [{'#' * filled}{'.' * (8 - filled)}] {courier.health}/{courier.max_health}"
        name = textwrap.wrap(courier.name, width=25, break_long_words=True) or ["not chosen"]
        from .character_presentation import role_display_name

        role = role_display_name(courier.role)
    else:
        health, name, role = "Health [........] -", ["not chosen"], "-"
    location = state.region.name if state.location == "region" and state.position.z == 0 else area_name(state)
    lines = [
        "COURIER", *name, f"Role: {role}", health,
        f"Load {pack_weight(state)}/{weight_capacity(state)} kg",
        *textwrap.wrap(f"Location: {location}", width=25, break_long_words=True),
    ]
    return lines if capacity is None else lines[:capacity]


def _draw_minimum_size_notice(screen: curses.window) -> None:
    height, width = screen.getmaxyx()
    messages = (
        ui_format("ui.minimum_size.need", width=MIN_WIDTH, height=MIN_HEIGHT),
        ui_format("ui.minimum_size.present", width=width, height=height),
    )
    lines = [
        (part, index == 0)
        for index, message in enumerate(messages)
        for part in textwrap.wrap(message, width=max(1, width - 2))
    ][:height]
    top = max(0, (height - len(lines)) // 2)
    for offset, (line, heading) in enumerate(lines):
        _put(screen, top + offset, max(0, (width - len(line)) // 2), line, curses.A_BOLD if heading else 0)


def _draw_base(screen: curses.window, state: GameState) -> None:
    screen.erase()
    height, width = screen.getmaxyx()
    if height < MIN_HEIGHT or width < MIN_WIDTH:
        _draw_minimum_size_notice(screen)
        screen.refresh()
        return
    status_width, event_height, command_height = 29, 6, 0
    main_height, map_width = height - event_height - command_height, width - status_width
    _frame(screen, 0, 0, main_height, map_width, area_name(state).upper())
    _frame(screen, 0, map_width, main_height, status_width, INTERFACE_LABELS["watch_log"])
    _draw_map(screen, state, 0, 0, main_height, map_width)
    for index, line in enumerate(_status_lines(state, main_height - 2)):
        role = status_colour_role(line)
        if line.startswith("COURIER") or index == 1:
            role = "player"
        elif line.startswith("Location:"):
            role = REGIONAL_GROUND_ROLES.get(state.active_region_id, "ui_heading") if state.location == "region" else "ui_heading"
        attr = _COLOUR_ATTRIBUTES[role]
        if role == "ui_heading":
            attr |= curses.A_BOLD
        _put(screen, 1 + index, map_width + 2, _clip(line, status_width - 4), attr)
    _frame(screen, main_height, 0, event_height, width, INTERFACE_LABELS["chronicle"])
    event_lines = _event_feed_entries(state.messages, width - 4, event_height - 2)
    for index, (line, source) in enumerate(event_lines):
        _put(screen, main_height + 1 + index, 2, _clip(line, width - 4), _COLOUR_ATTRIBUTES[event_colour_role(source)])
    screen.refresh()


def _cursor_screen_position(
    state: GameState, point: Position, height: int, width: int
) -> tuple[int, int, int, int]:
    status_width, event_height, command_height = 29, 6, 0
    main_height, map_width = height - event_height - command_height, width - status_width
    rows = map_rows(state)
    origin_x, origin_y = camera_origin(
        state.position, max(map(len, rows)), len(rows), map_width - 2, main_height - 2
    )
    return 1 + point.x - origin_x, 1 + point.y - origin_y, main_height, map_width


def _draw_look(screen: curses.window, state: GameState, view: LookView) -> None:
    from .inspection import inspect_lines

    height, width = screen.getmaxyx()
    screen_x, screen_y, main_height, map_width = _cursor_screen_position(
        state, view.cursor, height, width
    )
    if 1 <= screen_x < map_width - 1 and 1 <= screen_y < main_height - 1:
        glyph = displayed_tile(state, view.cursor)
        if view.cursor.z != state.position.z:
            glyph = "^" if view.cursor.z > state.position.z else "v"
        _put(screen, screen_y, screen_x, glyph, _COLOUR_ATTRIBUTES["target_cell"] | curses.A_REVERSE | curses.A_BOLD)
    lines = information_lines(inspect_lines(state, view.cursor), width - 2)
    shown = lines[-min(len(lines), 8):]
    for row, line in enumerate(shown, height - len(shown)):
        _put(screen, row, 1, line.ljust(width - 2), _COLOUR_ATTRIBUTES[information_colour_role(line)] | curses.A_REVERSE)
    _put(screen, height - 1, 1, LOOK_HELP_LINE, _COLOUR_ATTRIBUTES["ui_accent"] | curses.A_REVERSE | curses.A_BOLD)
    screen.refresh()


def _handle_look(
    state: GameState,
    view: LookView,
    event: InputEvent | int,
    *,
    screen_size: tuple[int, int] = (24, 80),
) -> bool:
    if isinstance(event, int):
        event = InputEvent("key", key=event)
    height, width = screen_size
    key = event.key
    if event.kind == "mouse":
        if event.button == "right":
            return True
        screen_x, screen_y, main_height, map_width = _cursor_screen_position(
            state, view.cursor, height, width
        )
        del screen_x, screen_y
        if event.button != "left" or not (1 <= event.x < map_width - 1 and 1 <= event.y < main_height - 1):
            return False
        rows = map_rows(state)
        origin_x, origin_y = camera_origin(
            state.position, max(map(len, rows)), len(rows), map_width - 2, main_height - 2
        )
        view.cursor = Position(origin_x + event.x - 1, origin_y + event.y - 1, state.position.z)
        return False
    if key in {27, ord(";")}:
        return True
    if key in {ord("<"), ord(">")}:
        levels = (
            {-1, 0, 1}
            if state.location == "jomon"
            else {int(z) for z in state.region.levels}
        )
        level = view.cursor.z + (1 if key == ord(">") else -1)
        if level in levels:
            view.cursor = Position(view.cursor.x, view.cursor.y, level)
        return False
    normalized = ord(chr(key).lower()) if 0 <= key < 256 else key
    movement = {
        curses.KEY_LEFT: (-1, 0), curses.KEY_RIGHT: (1, 0),
        curses.KEY_UP: (0, -1), curses.KEY_DOWN: (0, 1),
        ord("a"): (-1, 0), ord("d"): (1, 0), ord("w"): (0, -1), ord("s"): (0, 1),
        ord("h"): (-1, 0), ord("l"): (1, 0), ord("k"): (0, -1), ord("j"): (0, 1),
    }
    if normalized in movement:
        dx, dy = movement[normalized]
        rows = map_rows(state, view.cursor.z)
        view.cursor = Position(
            max(0, min(max(map(len, rows)) - 1, view.cursor.x + dx)),
            max(0, min(len(rows) - 1, view.cursor.y + dy)),
            view.cursor.z,
        )
    return False


def _draw_circuit(screen: curses.window, state: GameState, view: CircuitView) -> None:
    from .circuits import BUILD_KEYS, active, cell_at, diagnostic_lines, glyph, item_count, space_id
    from .circuit_presentation import circuit_format, circuit_layer_name, circuit_part_name, circuit_text

    height, width = screen.getmaxyx()
    visible = field_of_view(state, remember=False)
    for cell in state.circuits.values():
        if cell.space != space_id(state) or cell.layer != view.layer or cell.position.z != state.position.z:
            continue
        if state.location == "region" and cell.position not in visible:
            continue
        x, y, main_height, map_width = _cursor_screen_position(state, cell.position, height, width)
        if 1 <= x < map_width - 1 and 1 <= y < main_height - 1:
            role = "ui_accent" if cell.phase == "head" else "warning" if cell.phase == "tail" else "success" if active(state, cell) else "interactable"
            _put(screen, y, x, glyph(state, cell.position, view.layer) or "?", _COLOUR_ATTRIBUTES[role] | curses.A_BOLD)
    x, y, main_height, map_width = _cursor_screen_position(state, view.cursor, height, width)
    if 1 <= x < map_width - 1 and 1 <= y < main_height - 1:
        mark = glyph(state, view.cursor, view.layer) or displayed_tile(state, view.cursor)
        _put(screen, y, x, mark, _COLOUR_ATTRIBUTES["target_cell"] | curses.A_REVERSE | curses.A_BOLD)
    cell = cell_at(state, view.cursor, view.layer)
    description = circuit_part_name(cell.kind) if cell else circuit_text("circuit.terminal.empty")
    build_options = list(BUILD_KEYS.items())
    build_rows = [" ".join(circuit_format("circuit.terminal.build_option", key=key.upper(), part=circuit_part_name(kind), count=item_count(state, kind)) for key, kind in build_options[index:index + 7])
                  for index in range(0, len(build_options), 7)]
    lines = (
        circuit_format("circuit.terminal.heading", layer=circuit_layer_name(view.layer).upper(), x=view.cursor.x, y=view.cursor.y, z=view.cursor.z, description=description),
        *(diagnostic_lines(state, cell) if cell else [circuit_text("circuit.terminal.inspect_empty")]),
        *build_rows,
        circuit_format("circuit.terminal.cells", count=item_count(state, "cell")),
        circuit_text("circuit.terminal.controls"),
        (state.messages[-1] if state.messages else circuit_text("circuit.terminal.fallback")),
    )
    for index, line in enumerate(lines, height - len(lines)):
        _put(screen, index, 0, line[:width].ljust(width), _COLOUR_ATTRIBUTES["ui_accent"] | curses.A_REVERSE)
    screen.refresh()


def _handle_circuit(
    state: GameState, view: CircuitView, event: InputEvent,
    *, screen_size: tuple[int, int] = (24, 80),
) -> bool:
    from .circuits import BUILD_KEYS, operate, place, reclaim

    key = event.key
    if key in {27, ord("\\")}:
        return True
    if key == 9:
        view.layer = "buried" if view.layer == "surface" else "surface"
        return False
    normalized = ord(chr(key).lower()) if 0 <= key < 256 else key
    build_kind = BUILD_KEYS.get(chr(normalized)) if 0 <= normalized < 256 else None
    if build_kind:
        changed, message = place(state, view.cursor, view.layer, build_kind)
        if not changed:
            state.add_message(message, priority=2)
        return False
    if normalized in {ord("e"), 10, 13}:
        changed, message = operate(state, view.cursor, view.layer)
        if not changed:
            state.add_message(message, priority=2)
        return False
    if normalized == ord("t"):
        changed, message = operate(state, view.cursor, view.layer, "secondary")
        if not changed:
            state.add_message(message, priority=2)
        return False
    if key == ord("."):
        from .actions import _advance_world

        _advance_world(state)
        from .circuit_presentation import circuit_text
        state.add_message(circuit_text("circuit.terminal.step"))
        return False
    if normalized == ord("r"):
        changed, message = reclaim(state, view.cursor, view.layer)
        if not changed:
            state.add_message(message, priority=2)
        return False
    look = LookView(view.cursor)
    _handle_look(state, look, event, screen_size=screen_size)
    view.cursor = look.cursor
    return False


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


def _ammunition_status(state: GameState, ammunition: str | None = None) -> tuple[int, str]:
    if state.weapon == "throwing axe":
        readied = equipped_item(state, "readied")
        return int(bool(readied and readied.kind == "throwing axe")), "readied axe"
    if state.weapon == "pot sling":
        from .work_weapons import available_pots
        choices = available_pots(state)
        ammunition = ammunition or (choices[0] if choices else "pots")
        return physical_ammunition(state, ammunition), ammunition
    ammo_name = WEAPON_AMMUNITION.get(state.weapon or "", "")
    ammo_label = {"sling stones": "stones", "handgonne charges": "charges"}.get(ammo_name, ammo_name or "none")
    available = physical_ammunition(state, ammo_name) if ammo_name else 0
    return available, ammo_label


def targeting_detail(state: GameState, view: TargetView) -> str:
    if view.spell_id:
        from .magic import SPELLS
        from .magic_presentation import magic_format

        spell = SPELLS[view.spell_id]
        return magic_format("magic.target.detail", distance=distance(state.position, view.cursor), reach=spell.reach,
                            z=f"{view.cursor.z:+d}", mana=state.courier.mana, max_mana=state.courier.max_mana, cost=spell.cost)
    available, ammo_label = _ammunition_status(state, view.ammunition)
    detail = (
        f"R{distance(state.position, view.cursor)}/{effective_weapon_range(state)} "
        f"z{view.cursor.z:+d} "
        f"Cover:{cover_at(state, state.position, view.cursor)}"
    )
    if state.weapon in RANGED_WEAPONS:
        detail += f" Ammo:{available} {ammo_label}"
    return detail


def targeting_lines(state: GameState, view: TargetView, width: int) -> list[str]:
    from .encounters import threat_definition
    from .enemy_equipment import actor_items
    from .work_weapons import WORK_WEAPONS

    selected = _target_at_cursor(state, view)
    if view.spell_id:
        from .magic import SPELLS, spell_status
        from .magic_presentation import magic_format, magic_text, spell_description, spell_display_name

        spell = SPELLS[view.spell_id]
        legal, reason = spell_status(state, view.spell_id, view.cursor)
        return information_lines([
            magic_format("magic.target.header", spell=spell_display_name(spell.id).upper(), description=spell_description(spell)),
            targeting_detail(state, view),
            magic_format("magic.target.legality", status=magic_text("magic.target.ready" if legal else "magic.target.blocked"), reason=reason),
            magic_format("magic.target.observed", target=selected.name, intent=selected.intent) if selected else magic_text("magic.target.empty"),
        ], width)
    ready = "Enter commits one cast; Escape costs no time."
    if state.weapon == "pot sling":
        from .equipment_presentation import equipment_text
        ready = equipment_text("equipment.target.pot")
    from .expanded_weapons import ARSENAL

    if state.weapon in ARSENAL:
        expanded = ARSENAL[state.weapon]
        if expanded.family == "device":
            from .equipment_presentation import equipment_format
            ready = equipment_format("equipment.target.device", bomb=state.weapon.split()[0])
        elif expanded.family == "gun" and state.weapon_ready < (1 if "quick" in expanded.effects or state.courier and "vent-care" in state.courier.skill_nodes else 2):
            from .equipment_presentation import equipment_text
            ready = equipment_text("equipment.target.gun_loading")
        elif expanded.family in {"bow", "gun"} and "quick" not in expanded.effects and not (expanded.family == "bow" and state.courier and "quick-nock" in state.courier.skill_nodes):
            from .equipment_presentation import equipment_text
            ready = equipment_text("equipment.target.aim.ready" if selected and state.aimed_target == selected.id else "equipment.target.aim.prepare")
    if state.weapon == "crossbow" and not state.crossbow_loaded:
        ready = "Unloaded: Escape then G reloads; no shot is committed here."
    elif state.weapon in {"heavy crossbow", "handgonne"} and state.weapon_ready < 2:
        ready = f"Unready: {2 - state.weapon_ready} reload actions; Escape then G."
    elif state.weapon in {"crossbow", "longbow", "heavy crossbow", "handgonne"}:
        ready = "Aim set: Enter releases one shot." if selected and state.aimed_target == selected.id else "Enter prepares aim (one action); confirm again to release."
    if view.mastery_id:
        from .manoeuvres import BY_ID, status

        manoeuvre = BY_ID[view.mastery_id]
        legal, reason = status(state, view.mastery_id, selected.id if selected else None)
        from .progression_presentation import progression_format, progression_text
        ready = progression_format("progression.target.mastery", manoeuvre=manoeuvre.name, effect=manoeuvre.effect, status=progression_text("progression.target.ready") if legal else progression_format("progression.target.needs", reason=reason))
    label = f"Target: {selected.name}" if selected else "No presently visible actor at cursor."
    if view.cursor.z != state.position.z:
        label += " [ABOVE]" if view.cursor.z > state.position.z else " [BELOW]"
    lines = [label, targeting_detail(state, view)]
    if selected:
        legal, reason = attack_target_legality(state, selected)
        lines.append(("LEGAL — " if legal else "BLOCKED — ") + reason + ".")
        weapon = WEAPONS.get(state.weapon or "")
        if state.weapon in WORK_WEAPONS:
            weapon_text = WORK_WEAPONS[state.weapon].description
        elif state.weapon in ARSENAL:
            weapon_text = ARSENAL[state.weapon].description
        else:
            weapon_text = weapon[1] if weapon else "No readied effect."
        lines.append("EFFECT — " + weapon_text)
        armour = [
            f"{item_spec(item.kind).name} {item.condition}%"
            for item in actor_items(state, selected)
            if item.location in BODY_SLOTS
        ]
        lines.append(
            f"OBSERVED — intent {selected.intent}; morale {selected.morale}; "
            + ("protection " + ", ".join(armour) if armour else "no worn protection")
            + "."
        )
        data = threat_definition(selected)
        if data:
            lines.append(f"COUNTERS — {ecology_actor_counterplay(selected.archetype_id or '', str(data['counterplay']))}.")
        from .combat_forecast import forecast_lines, observed_forecasts
        forecast = next((row for row in observed_forecasts(state) if row.actor_id == selected.id), None)
        if forecast:
            lines.extend(forecast_lines(forecast)[1:3])
        brace_ok, brace_reason = brace_target_legality(state, selected)
        if state.weapon in BRACE_REACTION_WEAPONS:
            lines.append(
                ("G REACTION — " if brace_ok else "G BLOCKED — ") + brace_reason + "."
            )
    lines.append(ready)
    return information_lines(lines, width)


def _draw_targeting(screen: curses.window, state: GameState, view: TargetView) -> None:
    height, width = screen.getmaxyx()
    status_width, event_height, command_height = 29, 6, 0
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
        role = information_colour_role(line)
        _put(screen, row, 1, line.ljust(width - 2), _COLOUR_ATTRIBUTES[role] | curses.A_REVERSE | curses.A_BOLD)
    _put(screen, height - 1, 1, TARGET_HELP_LINE, _COLOUR_ATTRIBUTES["ui_accent"] | curses.A_REVERSE)
    screen.refresh()


def _target_cycle(state: GameState, view: TargetView) -> None:
    if view.spell_id:
        from .magic import spell_status

        available = [actor for actor in state.combatants if actor.id in view.target_ids and spell_status(state, view.spell_id, actor.position)[0]]
        if available:
            view.selected = (view.selected + 1) % len(available)
            view.cursor = available[view.selected].position
        return
    available = [
        threat_id for threat_id in view.target_ids
        if any(
            threat.id == threat_id and threat.status in {"watching", "engaged"}
            and courier_sees(state, threat.position)
            and (
                attack_target_legality(state, threat)[0]
                or brace_target_legality(state, threat)[0]
            )
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
        status_width, event_height, command_height = 29, 6, 0
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
    if normalized == ord("p") and state.weapon == "pot sling":
        from .work_weapons import available_pots
        choices = available_pots(state)
        if choices:
            view.ammunition = choices[(choices.index(view.ammunition) + 1) % len(choices)] if view.ammunition in choices else choices[0]
        return False, False
    if normalized == ord("m"):
        from .manoeuvres import known

        rows = known(state, "target")
        ids = [None, *(row.id for row in rows)]
        current = ids.index(view.mastery_id) if view.mastery_id in ids else 0
        view.mastery_id = ids[(current + 1) % len(ids)]
        return False, False
    if normalized == ord("g"):
        target = _target_at_cursor(state, view)
        if target is None:
            state.add_message("No visible engaged actor occupies the selected lane.")
            return False, False
        result = guard(state, target.id)
        return result.time_advanced, result.time_advanced
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
        if view.spell_id:
            from .magic import cast

            changed, message = cast(state, view.spell_id, view.cursor)
            if not changed:
                state.add_message(message, priority=2)
            return changed, changed
        if view.mastery_id:
            from .actions import _advance_world
            from .manoeuvres import perform

            target = _target_at_cursor(state, view)
            changed, message, steps = perform(
                state, view.mastery_id, target.id if target else None
            )
            if changed:
                _advance_world(state, steps=steps)
            state.add_message(message, priority=3)
            return changed, changed
        if state.weapon == "pot sling":
            result = attack(state, target_position=view.cursor, ammunition=view.ammunition)
            return result.time_advanced, result.time_advanced
        from .expanded_weapons import ARSENAL

        if state.weapon in ARSENAL and ARSENAL[state.weapon].family == "device":
            result = attack(state, target_position=view.cursor)
            return result.time_advanced, result.time_advanced
        target = _target_at_cursor(state, view)
        if target is None:
            state.add_message("No visible hostile stands at the marked place.", priority=2)
            return False, False
        result = attack(state, target.id)
        return result.time_advanced, result.time_advanced
    return False, False


def information_lines(lines: Iterable[str], width: int) -> list[str]:
    return [part for line in lines for part in (textwrap.wrap(line, width=max(1, width)) or [""])]


def _overlay(screen: curses.window, title: str, lines: Iterable[str], view: OverlayView | None = None) -> None:
    height, width = screen.getmaxyx()
    source_material = list(lines)
    material = [(render_notice(line), line) for line in source_material]
    box_width = min(width - 4, max(44, max((len(line) for line, _ in material), default=20) + 4))
    material = [(part, source) for line, source in material for part in information_lines([line], box_width - 4)]
    box_height = min(height - 4, len(material) + (5 if view else 4))
    page_rows = max(1, box_height - (5 if view else 3))
    offset = min(view.scroll_offset, max(0, len(material) - page_rows)) if view else 0
    if view:
        view.scroll_offset, view.page_rows, view.line_count = offset, page_rows, len(material)
    top, left = (height - box_height) // 2, (width - box_width) // 2
    for y in range(top, top + box_height):
        _put(screen, y, left, " " * box_width, curses.A_REVERSE)
    _frame(screen, top, left, box_height, box_width, title)
    for index, (line, source) in enumerate(material[offset:offset + page_rows]):
        _put(
            screen, top + 2 + index, left + 2, _clip(line, box_width - 4),
            _COLOUR_ATTRIBUTES[information_colour_role(source)],
        )
    if view:
        footer = f"Up/Down PgUp/PgDn Home/End; Esc close [{offset + 1}/{len(material)}]"
        _put(screen, top + box_height - 2, left + 2, _clip(footer, box_width - 4), _COLOUR_ATTRIBUTES["ui_accent"] | curses.A_BOLD)
    screen.refresh()


def _draw_vehicle_interior(screen: curses.window, state: GameState, view: OverlayView) -> None:
    from .vehicles import SPECS, active_vehicle, deck_rows, interior_entry
    from .vehicle_presentation import vehicle_display_name, vehicle_format, vehicle_resource_label, vehicle_text

    vehicle = active_vehicle(state)
    if vehicle is None:
        return
    rows = deck_rows(vehicle.id)
    if view.cursor is None:
        view.cursor = interior_entry(vehicle.id)
    spec = SPECS[vehicle.id]
    title = vehicle_display_name(vehicle.id).upper()
    height, width = screen.getmaxyx()
    box_width, box_height = width - 4, height - 4
    top, left = (height - box_height) // 2, (width - box_width) // 2
    map_top = top + max(2, (box_height - len(rows)) // 2 - 2)
    map_left = left + (box_width - len(rows[0])) // 2
    for y in range(top, top + box_height):
        _put(screen, y, left, " " * box_width, curses.A_REVERSE)
    _frame(screen, top, left, box_height, box_width, title)
    for y, row in enumerate(rows):
        for x, tile in enumerate(row):
            shown = "@" if (x, y) == (view.cursor.x, view.cursor.y) else tile
            role = ("player" if shown == "@" else "structure" if tile in "#+"
                    else "exit" if tile == "E" else "cargo" if tile == "C"
                    else "interactable" if tile in "HRF" else "terrain")
            _put(screen, map_top + y, map_left + x, shown, _COLOUR_ATTRIBUTES[role])
    status = vehicle_format("vehicle.interior.draw.status", fuel=vehicle.fuel, capacity=spec["capacity"], resource=vehicle_resource_label(vehicle.id), condition=vehicle.condition, maximum=spec["condition"])
    location = (vehicle_format("vehicle.interior.draw.location.region", region=state.active_region_id, x=vehicle.position.x, y=vehicle.position.y) if state.location == "region" else vehicle_format("vehicle.interior.draw.location.water", x=vehicle.position.x, y=vehicle.position.y))
    _put(screen, map_top + len(rows) + 1, left + 2, _clip(status, box_width - 4), _COLOUR_ATTRIBUTES["fact"])
    _put(screen, map_top + len(rows) + 2, left + 2, _clip(location, box_width - 4), _COLOUR_ATTRIBUTES["fact"])
    _put(screen, top + box_height - 2, left + 2,
         _clip(vehicle_text("vehicle.interior.draw.controls"), box_width - 4),
         _COLOUR_ATTRIBUTES["ui_accent"] | curses.A_BOLD)
    screen.refresh()


def dialogue_choices(state: GameState, kind: str) -> list[ChoiceOption]:
    if kind == "gangplank":
        from .vehicle_presentation import vehicle_text
        node = state.route_nodes.get(state.route_current_node)
        can_land = bool(node and node.region_id == state.active_region_id and state.voyage_status != "active")
        return [
            ChoiceOption("1", vehicle_text("vehicle.gangplank.choice.ashore"), "ordinary", can_land, vehicle_text("vehicle.gangplank.requirement")),
            ChoiceOption("2", vehicle_text("vehicle.gangplank.choice.tug"), "ordinary", can_land, vehicle_text("vehicle.gangplank.requirement")),
        ]
    if kind == "station:gathering":
        from .household_stories import station_choices
        from .skill_tree import journals_at_hand

        from .progression_presentation import progression_text
        return [ChoiceOption(*row) for row in station_choices(state)] + [
            ChoiceOption("J", progression_text("progression.choice.journal.write.label"), "commitment", bool(state.courier and state.courier.skill_nodes), progression_text("progression.choice.journal.write.requirement")),
            ChoiceOption("K", progression_text("progression.choice.journal.study.label"), "commitment", bool(journals_at_hand(state)), progression_text("progression.choice.journal.study.requirement")),
        ]
    if kind.startswith("household-story:"):
        from .household_stories import story_choices

        return [ChoiceOption(*row) for row in story_choices(state, kind.split(":", 1)[1])]
    if kind == "mastery":
        from .manoeuvres import choices

        return [ChoiceOption(*row) for row in choices(state)]
    if kind.startswith("situation:"):
        from .situations import choices

        return [ChoiceOption(*row) for row in choices(state, kind.split(":", 1)[1])]
    if kind == "navigation":
        keys = "123456789abcdefghijklmnopqrstuvwxyz"
        return [
            ChoiceOption(
                keys[index],
                f"{target.label} — {distance(state.position, target.position)} paces; level {target.position.z:+d}",
            )
            for index, target in enumerate(navigation_targets(state)[:len(keys)])
        ]
    if kind == "field-use":
        from .preparations import carried_preparations, preparation_status, preparation_status_text
        from .preparation_presentation import preparation_display_name, preparation_format

        keys = "123456789abcdefg"
        rows = []
        for key, preparation_id in zip(keys, carried_preparations(state)):
            available, reason = preparation_status(state, preparation_id)
            rows.append(ChoiceOption(key.upper(), preparation_format("preparation.overlay.choice", preparation=preparation_display_name(preparation_id)), "commitment", available, preparation_status_text(reason)))
        from .manoeuvres import known

        if known(state):
            from .progression_presentation import progression_text
            rows.append(ChoiceOption("M", progression_text("progression.choice.manoeuvre.label"), "commitment", bool(state.combat_active), progression_text("progression.choice.manoeuvre.requirement")))
        rows.append(ChoiceOption("X", "Use the carried bottle, selected relic, or readied gear", "commitment", state.combat_active, "requires active danger"))
        return rows
    if kind == "field-traveller":
        from .topology_presentation import topology_text

        return [
            ChoiceOption("A", topology_text("topology.landform.overlay.clue.label"), "ordinary",
                         not state.region.changes.get("landform:traveller:clue"), topology_text("topology.landform.overlay.clue.requirement")),
            ChoiceOption("B", topology_text("topology.landform.overlay.lot.label").format(commodity=state.region.objective_commodity), "commitment",
                         state.trade_credit >= 1 and not state.region.changes.get("landform:traveller:lot"),
                         topology_text("topology.landform.overlay.lot.requirement")),
        ]
    if kind == "aftermath":
        from .aftermath import contracts_for

        return [
            ChoiceOption(
                str(index + 1),
                f"{contract.title} — {contract.status}",
                "ordinary" if contract.status in {"available", "completed"} else "commitment",
            )
            for index, contract in enumerate(contracts_for(state))
        ]
    if kind.startswith("aftermath-contract:"):
        from .aftermath import contract_options

        contract_id = kind.split("aftermath-contract:", 1)[1]
        return [
            ChoiceOption(key.upper(), label, semantic, available, requirement)
            for key, label, semantic, available, requirement
            in contract_options(state, contract_id)
        ]
    if kind == "station:workshop" or kind.startswith("workshop:"):
        from .workshop import FITTINGS, SLOTS, attached, can_fit, compatible, fit_cost

        if kind == "station:workshop":
            from .equipment_presentation import equipment_format
            return [ChoiceOption(str(index + 1), equipment_format("equipment.overlay.slot", slot=slot, item=item_spec(item.kind).name if (item := equipped_item(state, slot)) else equipment_format("equipment.overlay.slot.empty")), available=equipped_item(state, slot) is not None, requirement=equipment_format("equipment.overlay.slot.requirement")) for index, slot in enumerate(SLOTS)] + [ChoiceOption("P", equipment_format("equipment.overlay.buy.choice"))]
        if kind == "workshop:store":
            from .equipment_presentation import equipment_format
            return [ChoiceOption(chr(65 + index), equipment_format("equipment.overlay.store.row", fitting=value.name, price=value.price, stock=state.vessel_changes.get('fitting_stock:' + name, 0))) for index, (name, value) in enumerate(FITTINGS.items())]
        if kind.startswith("workshop:slot:"):
            target = equipped_item(state, kind.split(":", 2)[2])
            if target is None:
                return []
            options = []
            for index, name in enumerate(FITTINGS):
                if compatible(target, name):
                    valid, why = can_fit(state, target, name)
                    from .equipment_presentation import equipment_format
                    options.append(ChoiceOption(chr(65 + index), equipment_format("equipment.overlay.preview", fitting=FITTINGS[name].name, cost=fit_cost(state, name)), "commitment", valid, why))
            for key, socket in zip("UVW", ("structure", "treatment", "lining")):
                if any(FITTINGS[part.kind.split(':', 1)[1]].slot == socket for part in attached(state, target)):
                    from .equipment_presentation import equipment_format
                    options.append(ChoiceOption(key, equipment_format("equipment.overlay.remove.choice", slot=socket), "commitment"))
            from .equipment_presentation import equipment_text
            options.append(ChoiceOption("R", equipment_text("equipment.overlay.repair.choice"), "commitment", target.condition < 100 and state.trade_credit >= 2, equipment_text("equipment.overlay.repair.requirement")))
            return options
        from .equipment_presentation import equipment_text
        return [ChoiceOption("F", equipment_text("equipment.overlay.confirm.choice"), "commitment"), ChoiceOption("B", equipment_text("equipment.overlay.back.choice"))]
    if kind.startswith("vessel-refits:"):
        from .vessel_refits import REFITS, STATION_REFITS, installation_status

        station = kind.split(":", 1)[1]
        rows = []
        for index, refit_id in enumerate(STATION_REFITS.get(station, ())):
            refit = REFITS[refit_id]
            available, reason = installation_status(state, refit_id)
            rows.append(ChoiceOption(str(index + 1), f"Fit {refit.name}", "commitment", available, reason))
        return rows + [ChoiceOption("B", "Back without changes")]
    if kind.startswith("station:"):
        from .vessel_refits import STATION_REFITS

        station = kind.split(":", 1)[1]
        if station in STATION_REFITS:
            return [ChoiceOption("V", "Inspect optional vessel refits")]
    if kind == "material":
        from .worklines import WORKLINES
        rows = [ChoiceOption(str(index + 1), name) for index, name in enumerate(("Here", "North", "East", "South", "West"))]
        if state.location == "region" and state.active_region_id in WORKLINES:
            from .workline_presentation import workline_text
            rows.append(ChoiceOption("W", workline_text("workline.ui.material_entry")))
        from .aftermath import contracts_for, near_contract_site

        if any(
            contract.stage == 1 and near_contract_site(state, contract)
            for contract in contracts_for(state)
        ):
            rows.append(ChoiceOption("A", "Accepted aftermath work at this scar"))
        from .manoeuvres import known

        if known(state):
            from .progression_presentation import progression_text
            rows.append(ChoiceOption("M", progression_text("progression.choice.manoeuvre.label")))
        return rows
    if kind == "workline":
        from .worklines import options
        return [ChoiceOption(key.upper(), label, semantic, available, requirement) for key, label, semantic, available, requirement in options(state)]
    if kind.startswith("material:"):
        from .materials import VERBS
        from .material_presentation import material_verb_display_name
        from .chemistry import carried_flasks

        return [ChoiceOption(chr(ord("a") + index), material_verb_display_name(verb), "danger" if verb in {"ignite", "break", "cut"} else "commitment") for index, verb in enumerate(VERBS)] + [
            ChoiceOption("L", "Pour a carried flask here", "danger", bool(carried_flasks(state)), "a filled carried field flask"),
            ChoiceOption("M", "Drink a carried flask", "commitment", bool(carried_flasks(state)), "a filled carried field flask"),
        ]
    if kind == "quit":
        return [ChoiceOption("Y", "Sign the leave book", "danger"), ChoiceOption("N", "Return to the vessel")]
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

        contact_id = kind.split(":", 1)[1]
        return [ChoiceOption(key.upper(), label, semantic, available, requirement) for key, label, semantic, available, requirement in secondary_service_options(state, contact_id)]
    if kind == "merchant":
        return [
            ChoiceOption(str(index + 1), f"Buy {item_display_name_or_legacy(item)} — {max(1, MERCHANT_ITEMS[item][0] - (1 if state.support == 'factor surety' else 0))} credit", "commitment", state.trade_credit >= max(1, MERCHANT_ITEMS[item][0] - (1 if state.support == "factor surety" else 0)), "sufficient credit")
            for index, item in enumerate(state.merchant_stock)
        ]
    if kind == "voyage":
        from .ship_crises import choices
        rows = choices(state)
        return [ChoiceOption(*row) for row in rows]
    if kind.startswith("ship-work:"):
        from .vessel_refits import STATION_REFITS, refit_station_at

        task = kind.split(":", 1)[1]
        timber = state.vessel_cargo.get("timber")
        allowed = task != "repair" or bool(timber and timber.quantity and state.vessel_integrity < 10)
        rows = [ChoiceOption("F", "Confirm the counted work and time", "commitment", allowed, "needs hull damage and one timber lot" if not allowed else "")]
        if refit_station_at(state) in STATION_REFITS:
            rows.append(ChoiceOption("V", "Inspect optional vessel refits"))
        return rows + [ChoiceOption("B", "Back without work")]
    if kind == "bartender":
        return [
            ChoiceOption("D", vessel_format("vessel.bar.choice.browse")),
            ChoiceOption("S", vessel_format("vessel.bar.choice.support")),
            ChoiceOption("L", vessel_format("vessel.bar.choice.leave")),
        ]
    if kind == "bartender:drinks":
        return [
            ChoiceOption(str(index + 1), vessel_format("vessel.bar.stock.choice", drink=drink_display_name(drink_id), cost=drink.cost, stock=state.bartender_stock.get(drink_id, 0)), "commitment", state.bartender_stock.get(drink_id, 0) > 0, vessel_format("vessel.bar.stock.requirement"))
            for index, (drink_id, drink) in enumerate(DRINKS.items())
        ]
    if kind.startswith("bartender:drink:"):
        drink_id = kind.split(":", 2)[2]
        drink = DRINKS[drink_id]
        available = state.bartender_stock.get(drink_id, 0) > 0 and state.trade_credit >= drink.cost
        return [
            ChoiceOption("D", vessel_format("vessel.bar.choice.drink"), "commitment", available, vessel_format("vessel.bar.drink.requirement", cost=drink.cost)),
            ChoiceOption("B", vessel_format("vessel.bar.choice.bottle"), "commitment", available, vessel_format("vessel.bar.bottle.requirement", cost=drink.cost)),
        ]
    if kind == "incident":
        return [
            ChoiceOption("M", "Mediate by naming the disputed work", "commitment"),
            ChoiceOption("S", "Support the first speaker", "refusal"),
            ChoiceOption("F", "Let the fistfight run its course", "danger"),
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
        options = [ChoiceOption("C", "Open full character sheet")]
        if person and person.id != state.active_courier_id and state.courier and "teaching" in state.courier.skill_nodes:
            from .progression_presentation import progression_text
            options.append(ChoiceOption("T", progression_text("progression.choice.teach.label"), "commitment"))
        if person in state.household and person and person.id != state.active_courier_id and person.alive and person.available:
            return options + [ChoiceOption("S", "Switch to this courier", "commitment")]
        if person and person not in state.household:
            return options + [ChoiceOption("R", "Offer a voluntary berth", "commitment"), ChoiceOption("D", "Defer the invitation", "refusal")]
        return options
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
        _put(screen, row, left + 2, _clip(line, box_width - 4), _COLOUR_ATTRIBUTES[information_colour_role(line)])
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
    if view.kind == "vehicle-interior":
        from .vehicles import active_vehicle, interior_entry, interior_fixture, interior_step, service

        vehicle = active_vehicle(state)
        if vehicle is None or event.key in {9, 27}:
            return True, False
        if view.cursor is None:
            view.cursor = interior_entry(vehicle.id)
        normalized = ord(chr(event.key).lower()) if 0 <= event.key < 256 else event.key
        if normalized in MOVES:
            dx, dy = MOVES[normalized]
            view.cursor = interior_step(vehicle.id, view.cursor, dx, dy)
        elif normalized in {10, 13, ord("e")}:
            fixture = interior_fixture(vehicle.id, view.cursor)
            if fixture in {"E", "H"}:
                return True, False
            if fixture in {"R", "F"}:
                service(state, repair=fixture == "F")
            elif fixture == "C":
                from .vehicle_presentation import vehicle_text
                state.add_message(vehicle_text("vehicle.interior.cargo"))
            else:
                from .vehicle_presentation import vehicle_text
                state.add_message(vehicle_text("vehicle.interior.fixture"))
        return False, False
    options = dialogue_choices(state, view.kind)
    if options:
        view.selected %= len(options)
    key = event.key
    if view.kind.startswith("spell-tier:") and ord("1") <= key <= ord("4"):
        from .magic import SPELL_TIERS

        spell_id = SPELL_TIERS[view.kind.split(":", 1)[1]][key - ord("1")]
        if spell_id in state.courier.known_spells:
            view.result = "spell:" + spell_id
            return True, False
        from .magic_presentation import magic_text
        state.add_message(magic_text("magic.selection.unlearned"), priority=2)
        return False, False
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
        if view.kind == "navigation":
            direct = next(
                (option for option in options if option.key.lower() == chr(key).lower()),
                None,
            ) if 0 <= key < 256 else None
            if direct:
                index = options.index(direct)
                view.result = navigation_targets(state)[index].id
                return True, False
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
    contact = "Contacts: established" if node.region_id else "Contacts: no settled witness"
    if view.cursor == state.route_current_node:
        raw = [
            node.name.upper(), f"Type: {node.kind}", description,
            "Jomon is moored here.", f"Season: {date.season}",
            market, contact, f"Calendar: {date.label}",
            f"Integrity: {state.vessel_integrity}/10",
        ]
    else:
        edge = edge_between(state, state.route_current_node, view.cursor)
        if edge is None:
            raw = [
                node.name.upper(), f"Type: {node.kind}", description,
                f"No direct charted leg from {state.route_nodes[state.route_current_node].name}.",
                f"Season: {date.season}", market, contact,
                "Follow a connected line from Jomon's mooring to set sail.",
            ]
        else:
            available, reason = route_availability(state, view.cursor)
            from .route_chart import leg_travel_time

            route = f"Route: {edge.hazard}; {leg_travel_time(state, edge)} actions; supplies {edge.supply_cost}"
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
    _frame(screen, 0, map_width, height - 2, detail_width, INTERFACE_LABELS["charted_passage"])
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
    glyphs = ROUTE_NODE_SYMBOLS["kinds"]
    reachable = set(neighbours(state, state.route_current_node, reachable_only=True))
    for node_id, node in state.route_nodes.items():
        x, y = _chart_screen_point(node.x, node.y, map_width, height)
        view.node_screen[node_id] = (x, y)
        known = node_id in state.route_known or node.known
        glyph = (ROUTE_NODE_SYMBOLS["current"] if node_id == state.route_current_node
                 else glyphs.get(node.kind, ROUTE_NODE_SYMBOLS["default"]) if known else ROUTE_NODE_SYMBOLS["unknown"])
        role = "player" if glyph == "@" else "hazard" if glyph == "!" else "cargo" if glyph == "$" else "exit"
        attr = _COLOUR_ATTRIBUTES[role]
        if node_id in reachable:
            attr |= curses.A_BOLD
        if node_id == view.cursor:
            attr |= curses.A_REVERSE
        _put(screen, y, x, glyph, attr)
        if node.region_id:
            left, label = chart_label_position(x, node.name, map_width)
            label_role = REGIONAL_GROUND_ROLES.get(node.region_id, "ui_heading")
            _put(screen, y, left, label, _COLOUR_ATTRIBUTES[label_role] | (curses.A_BOLD if known else curses.A_DIM))
    if moving:
        mx, my = _chart_screen_point(moving[0], moving[1], map_width, height)
        _put(screen, my, mx, ENTITY_GLYPHS["courier"], _COLOUR_ATTRIBUTES["player"] | curses.A_REVERSE)
        _put(screen, height - 4, 2, _clip(moving[2], map_width - 4), curses.A_BOLD)
    lines = route_detail_lines(state, view, detail_width - 4)
    for index, line in enumerate(lines[: height - 5]):
        role = information_colour_role(line)
        if line.startswith("Market:"):
            role = "commodity"
        elif line.startswith(("Route:", "Risks:")):
            role = "warning"
        attr = _COLOUR_ATTRIBUTES[role]
        if index == 0 or line.startswith(">"):
            attr |= curses.A_BOLD
        if "BLOCKED" in line:
            attr |= curses.A_DIM
        _put(screen, 2 + index, map_width + 2, line, attr)
    for index, line in enumerate(ROUTE_HELP_LINES):
        _put(screen, height - 2 + index, 1, line, _COLOUR_ATTRIBUTES["ui_accent"] | curses.A_REVERSE)
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
        "weapon": "weapon", "armour": "armour", "gear": "tool",
        "passive": "technique", "consumable": "consumable",
        "cargo": "commodity", "relic": "relic",
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
    _frame(screen, 0, 0, height - 2, width, INTERFACE_LABELS["pack_and_stores"])
    pane_name = view.pane.split(":", 1)[0].upper()
    mode = INTERFACE_LABELS["ordered_stowage"] if state.auto_place_enabled else INTERFACE_LABELS["free_stowage"]
    _put(screen, 1, 2, f"{pane_name}  Tab pane  {mode}", _COLOUR_ATTRIBUTES["ui_heading"] | curses.A_BOLD)
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
        _put(screen, 3, 2, "Items at this physical source:", _COLOUR_ATTRIBUTES["ui_heading"] | curses.A_BOLD)
        rows = _inventory_items(state, view)
        for index, item in enumerate(rows[:10]):
            attr = _item_colour(item.kind) | (curses.A_REVERSE if index == view.cursor_y else 0)
            _put(screen, 5 + index, 3, _clip(f"{item_spec(item.kind).abbreviation} {item_spec(item.kind).name} x{item.quantity}", 34), attr)
        if not rows:
            _put(screen, 5, 3, "(empty)", curses.A_DIM)

    detail_x = min(max(27, width // 2), width - 34)
    _put(screen, 2, detail_x, INTERFACE_LABELS["carried_kit"], _COLOUR_ATTRIBUTES["ui_heading"] | curses.A_BOLD)
    view.paper_screen = {}
    for index, line in enumerate(paper_doll_layout(state)):
        _put(screen, 3 + index, detail_x, _clip(line, width - detail_x - 2))
    # Mouse hit rows are deliberately broad; exact limb art is presentation.
    for index, slot in enumerate(PAPER_SLOTS):
        view.paper_screen[slot] = (detail_x, 3 + min(index, 7))
    burden = load_state(state)
    burden_role = "warning" if burden in {"encumbered", "overloaded"} else "cargo"
    _put(screen, 11, detail_x, f"Weight {pack_weight(state)}/{weight_capacity(state)} — {burden}", _COLOUR_ATTRIBUTES[burden_role] | curses.A_BOLD)
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
        _put(screen, 14, detail_x, _clip(f"{held}{spec.name} {actual_width}x{actual_height} wt {spec.weight}", width - detail_x - 2), _item_colour(selected.kind) | curses.A_BOLD)
        for index, art in enumerate(item_preview(selected.kind)):
            _put(screen, 15 + index, detail_x, _clip(art, width - detail_x - 2), _item_colour(selected.kind))
        fitted = attached(state, selected)
        if fitted:
            _put(screen, 17, detail_x, _clip("Fitted: " + ", ".join(part.kind.split(":", 1)[1] for part in fitted), width - detail_x - 2), _COLOUR_ATTRIBUTES["technique"] | curses.A_BOLD)
        for index, line in enumerate(_wrapped(spec.description, max(20, width - detail_x - 2))[:3]):
            _put(screen, 18 + index, detail_x, line)
        _put(screen, min(height - 4, 21), detail_x, _clip(f"Condition {selected.condition}; {'PINNED; ' if selected.pinned else ''}{selected.provenance}", width - detail_x - 2))
    if view.held_id and view.pane in {"pack", "locker"}:
        held_item = next(item for item in state.items if item.id == view.held_id)
        owner = state.active_courier_id if view.pane == "pack" else None
        preview = placement_preview(state, held_item, view.pane, view.cursor_x, view.cursor_y, rotated=view.held_rotated, owner_id=owner)
        view.status = ui_format("ui.inventory.preview_load", label=INTERFACE_LABELS["placement_mark"], used=preview.reason, weight=preview.resulting_weight, total=weight_capacity(state), load=preview.resulting_load)
    count = len(view.selected_ids or ())
    if count:
        marked = [item for item in state.items if item.id in (view.selected_ids or set())]
        total = sum(item_spec(item.kind).weight * item.quantity for item in marked)
        cells = sum(len(occupied_cells(item)) for item in marked)
        view.status = f"{INTERFACE_LABELS['pack_tally']}: {count}; weight {total}; spaces {cells}"
    if view.pending_drop:
        view.status = f"{INTERFACE_LABELS['drop_tally']}: Y sets the marked goods down; N/Esc keeps them."
    status_role = "warning" if any(word in view.status.lower() for word in ("invalid", "failed", "confirm drop")) else "success" if view.status else "terrain"
    _put(screen, height - 3, 2, _clip(view.status, width - 4), _COLOUR_ATTRIBUTES[status_role] | curses.A_BOLD)
    for index, line in enumerate(INVENTORY_HELP_LINES):
        _put(screen, height - 2 + index, 1, line, _COLOUR_ATTRIBUTES["ui_accent"] | curses.A_REVERSE)
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
    from .character_presentation import role_display_name

    courier = state.courier
    identity = f"{courier.name}, {role_display_name(courier.role)}; health {courier.health}/{courier.max_health}; {courier.injury}; {courier.technique}" if courier else "none selected"
    goods = ", ".join(f"{item_display_name(name)} {stack.quantity}" for name, stack in state.carried_goods.items()) or "none"
    combos = ", ".join(build_combinations(state)) or "none active"
    passives = ", ".join(state.carried_passives) or "none"
    return [
        f"Courier on watch: {identity}",
        "Speak directly to a visible adventurer to switch or recruit.",
        f"S Household counsel: {item_display_name_or_legacy(state.support) if state.support else 'none'}",
        f"Readied: {item_display_name_or_legacy(state.weapon) if state.weapon else 'none'} / {item_display_name_or_legacy(state.gear) if state.gear else 'none'} / {item_display_name(state.carried_relic) if state.carried_relic else 'no relic'}",
        f"Discoveries: {', '.join(item_display_name(name) for name in state.carried_passives) or 'none'} ({passive_bulk(state)}/{passive_capacity(state)} carried bulk)",
        f"Working strengths: {combos}", f"Cargo: {goods}; capacity {carried_bulk(state)}/{capacity(state)}",
        "", state.region.condition, state.region.objective_text, f"Local work: {state.objective_status}",
        "I opens the pack, carried kit, and Jomon locker.",
        "Enter binds the departure terms and folds this slate. Escape leaves no mark.",
    ]


def _overlay_lines(state: GameState, kind: str) -> tuple[str, list[str]]:
    if kind == "gangplank":
        from .vehicle_presentation import vehicle_format, vehicle_text
        return vehicle_text("vehicle.gangplank.title"), [
            vehicle_format("vehicle.gangplank.line.ashore", region=state.region.name),
            vehicle_text("vehicle.gangplank.line.tug"),
            vehicle_text("vehicle.gangplank.line.crossing"),
        ]
    if kind == "vehicle-interior":
        from .vehicles import interior_lines

        return interior_lines(state)
    if kind.startswith("journal-write:"):
        from .skill_tree import NODES
        from .progression_presentation import progression_format, progression_text

        page = int(kind.split(":", 1)[1])
        nodes = [node for node in state.courier.skill_nodes if node not in state.courier.journal_nodes]
        pages = max(1, (len(nodes) + 7) // 8)
        return progression_text("progression.journal.write.title"), [
            progression_format("progression.journal.write.summary", courier=state.courier.name, written=len(state.courier.journal_nodes)),
            *[progression_format("progression.journal.write.row", index=index + 1, node=NODES[node].name, description=NODES[node].effect)
              for index, node in enumerate(nodes[page * 8:page * 8 + 8])],
            progression_format("progression.journal.write.page", page=page + 1, pages=pages),
        ]
    if kind.startswith("journal-study:"):
        from .skill_tree import NODES, journals_at_hand
        from .progression_presentation import progression_format, progression_text

        page = int(kind.split(":", 1)[1])
        journals = journals_at_hand(state)
        pages = max(1, (len(journals) + 7) // 8)
        return progression_text("progression.journal.study.title"), [
            progression_format("progression.journal.study.summary", courier=state.courier.name, inherited=state.courier.taught_nodes),
            *[progression_format("progression.journal.study.row", index=index + 1, journal=item.id, node=NODES[item.lesson_node].name, provenance=item.provenance)
              for index, item in enumerate(journals[page * 8:page * 8 + 8])],
            progression_format("progression.journal.study.page", page=page + 1, pages=pages),
        ]
    if kind.startswith("teach-person:"):
        from .people import person_by_id
        from .skill_tree import NODES
        from .progression_presentation import progression_format, progression_text

        _, recipient_id, page_text = kind.split(":")
        person = person_by_id(state, recipient_id)
        page = int(page_text)
        nodes = [node for node in state.courier.skill_nodes if node not in person.skill_nodes]
        pages = max(1, (len(nodes) + 7) // 8)
        return progression_format("progression.teach.title", person=person.name.upper()), [
            progression_format("progression.teach.summary", person=person.name, inherited=person.taught_nodes, teacher=state.courier.name),
            *[progression_format("progression.teach.row", index=index + 1, node=NODES[node].name, parents=", ".join(NODES[parent].name for parent in NODES[node].parents) or progression_text("progression.teach.none"))
              for index, node in enumerate(nodes[page * 8:page * 8 + 8])],
            progression_format("progression.teach.page", page=page + 1, pages=pages),
        ]
    if kind.startswith("craft-catalog:"):
        from .chemistry_presentation import reagent_display_name
        from .production import RECIPES, SOURCES, at_shore_site, input_count, recipe_status, stations_here
        from .production_presentation import (
            production_format, production_recipe_name, production_station_name, production_text,
        )

        page = int(kind.split(":", 1)[1])
        stations = stations_here(state)
        ids = [key for key, recipe in RECIPES.items() if recipe.station in stations]
        page = min(page, max(0, (len(ids) - 1) // 8))
        pages = max(1, (len(ids) + 7) // 8)
        rows = [production_format("production.overlay.catalog.stations",
                                  stations=", ".join(production_station_name(station) for station in sorted(stations)),
                                  page=page + 1, pages=pages)]
        for index, recipe_id in enumerate(ids[page * 8:page * 8 + 8]):
            recipe = RECIPES[recipe_id]
            legal, reason = recipe_status(state, recipe_id)
            requirements = ", ".join(
                production_format("production.overlay.catalog.requirement_item", quantity=quantity,
                                  item=item_display_name_or_legacy(item_kind),
                                  held=input_count(state, item_kind))
                for item_kind, quantity in recipe.inputs
            )
            rows.append(production_format("production.overlay.catalog.recipe", index=index + 1,
                                          recipe=production_recipe_name(recipe.id, recipe.output),
                                          quantity=recipe.quantity,
                                          output=item_display_name_or_legacy(recipe.output),
                                          status=reason if not legal else production_text("production.status.ready").upper()))
            rows.append(production_format("production.overlay.catalog.requirements", requirements=requirements))
        rows.append(production_text("production.overlay.catalog.guidance"))
        if at_shore_site(state):
            first, second = SOURCES[state.active_region_id]
            rows.append(production_format("production.overlay.catalog.gather", first=reagent_display_name(first),
                                          second=reagent_display_name(second),
                                          stock=state.production["sites"][state.active_region_id]["stock"]))
        return production_text("production.overlay.catalog.title"), rows
    if kind == "craft-mix":
        from .chemistry import carried_flasks
        from .chemistry_presentation import chemistry_format, chemistry_text, reagent_contents_display, reaction_list_display

        return chemistry_text("chemistry.overlay.flasks.title"), [
            *[chemistry_format("chemistry.overlay.flasks.row", index=index + 1, flask=flask.id,
                               contents=reagent_contents_display(flask.contents) if flask.contents else chemistry_text("chemistry.overlay.empty"),
                               measures=sum(flask.contents.values()))
              for index, flask in enumerate(carried_flasks(state)[:9])],
            chemistry_format("chemistry.overlay.flasks.journal",
                             practiced=reaction_list_display(state.courier.known_formulas) or chemistry_text("chemistry.overlay.none"),
                             household=reaction_list_display(state.household_formulas) or chemistry_text("chemistry.overlay.none")),
            chemistry_text("chemistry.overlay.flasks.guidance"),
            chemistry_text("chemistry.overlay.flasks.return"),
        ]
    if kind.startswith("craft-distil:"):
        from .chemistry import carried_flasks
        from .chemistry_presentation import chemistry_format, chemistry_text, reagent_contents_display, reagent_display_name

        flask_id = kind.split(":", 1)[1]
        flask = next((item for item in carried_flasks(state) if item.id == flask_id), None)
        return chemistry_text("chemistry.overlay.distill.title"), [
            chemistry_format("chemistry.overlay.distill.status", flask=flask_id,
                             contents=reagent_contents_display(flask.contents) if flask else chemistry_text("chemistry.overlay.no_longer_carried")),
            *[chemistry_format("chemistry.overlay.distill.option", index=index + 1, reagent=reagent_display_name(reagent))
              for index, reagent in enumerate(flask.contents if flask else ())],
            chemistry_text("chemistry.overlay.distill.return"),
        ]
    if kind.startswith("craft-fill:"):
        from .chemistry import carried_ingredients
        from .chemistry_presentation import chemistry_format, chemistry_text, reagent_display_name

        parts = kind.split(":")
        flask_id, page = parts[1], int(parts[2])
        ingredients = carried_ingredients(state)
        return chemistry_text("chemistry.overlay.fill.title"), [
            *[chemistry_format("chemistry.overlay.fill.option", index=index + 1, item=item.id,
                               reagent=reagent_display_name(item.kind.split(':', 1)[1]), quantity=item.quantity)
              for index, item in enumerate(ingredients[page * 8:page * 8 + 8])],
            chemistry_format("chemistry.overlay.fill.pages", page=page + 1, pages=max(1, (len(ingredients) + 7) // 8))
        ]
    if kind == "spellbook":
        from .magic import SPELL_TIERS
        from .magic_presentation import magic_format, magic_text
        from .skill_tree import NODES

        return magic_text("magic.spellbook.title"), [
            magic_format("magic.spellbook.summary", courier=state.courier.name, mana=state.courier.mana, max_mana=state.courier.max_mana),
            *[magic_format("magic.spellbook.tier", index=index + 1, discipline=NODES[node].name,
                           learned=sum(spell in state.courier.known_spells for spell in spells))
              for index, (node, spells) in enumerate(SPELL_TIERS.items())],
            magic_text("magic.spellbook.guidance"),
        ]
    if kind.startswith("spell-tier:"):
        from .magic import SPELLS, SPELL_TIERS
        from .magic_presentation import magic_format, magic_text, spell_description, spell_display_name
        from .skill_tree import NODES

        node = kind.split(":", 1)[1]
        return NODES[node].name.upper(), [
            *[magic_format("magic.spellbook.row", index=index + 1, spell=spell_display_name(spell_id),
                           status=magic_text("magic.spellbook.status.ready" if spell_id in state.courier.known_spells else "magic.spellbook.status.unlearned"),
                           description=spell_description(SPELLS[spell_id]))
              for index, spell_id in enumerate(SPELL_TIERS[node])],
            magic_text("magic.spellbook.tier.guidance"),
        ]
    if kind == "skill-tree":
        from .skill_tree import BRANCHES, NODES
        from .progression_presentation import branch_display_name, progression_format, progression_text

        person = state.courier
        return progression_text("progression.overlay.skill.title"), [
            progression_format("progression.overlay.skill.summary", courier=person.name, points=person.skill_points, learned=len(person.skill_nodes)),
            *[progression_format("progression.overlay.skill.branch", index="123456789a"[index], branch=title, learned=sum(node.branch == branch and node.id in person.skill_nodes for node in NODES.values()))
              for index, (branch, _) in enumerate(BRANCHES.items())
              for title in (branch_display_name(branch),)],
            progression_text("progression.overlay.skill.guidance"),
        ]
    if kind.startswith("skill-branch:"):
        from .skill_tree import BRANCHES, NODES
        from .progression_presentation import branch_display_name, progression_format, progression_text

        branch = kind.split(":", 1)[1]
        title = branch_display_name(branch)
        person = state.courier
        rows = [progression_format("progression.overlay.branch.summary", courier=person.name, points=person.skill_points)]
        for index, node in enumerate(node for node in NODES.values() if node.branch == branch):
            unmet = [NODES[parent].name for parent in node.parents if parent not in person.skill_nodes]
            status = (progression_text("progression.overlay.status.learned") if node.id in person.skill_nodes else progression_format("progression.overlay.status.needs", parents=", ".join(unmet)) if unmet else progression_text("progression.overlay.status.ready") if person.skill_points else progression_text("progression.overlay.status.point"))
            rows.append(progression_format("progression.overlay.branch.row", index=index + 1, node=node.name, status=status, description=node.effect))
        return title.upper(), rows
    if kind.startswith("household-story:"):
        from .household_stories import BY_ID, story_lines

        story_id = kind.split(":", 1)[1]
        return BY_ID[story_id].name.upper(), story_lines(state, story_id)
    if kind == "mastery":
        from .manoeuvres import lines

        from .progression_presentation import progression_text
        return progression_text("progression.overlay.mastery.title"), lines(state)
    if kind.startswith("situation:"):
        from .situations import BY_ID, inspect_lines

        situation_id = kind.split(":", 1)[1]
        return BY_ID[situation_id].name.upper(), inspect_lines(state, situation_id)
    if kind == "sanctum":
        from .sanctums import inspect_lines
        from .sanctum_presentation import sanctum_display_name

        return sanctum_display_name(state.active_region_id).upper(), inspect_lines(state)
    if kind == "field-traveller":
        from .landscape_variation import structure_name, traveller_name
        from .topology_presentation import topology_text

        return topology_text("topology.landform.overlay.title").format(
            traveller=traveller_name(state.active_region_id),
        ).upper(), [
            topology_text("topology.landform.overlay.summary").format(
                structure=structure_name(state.active_region_id, "field_upper"),
                commodity=state.region.objective_commodity,
            ),
            topology_text("topology.landform.overlay.guidance"),
        ]
    if kind == "navigation":
        return "FOLLOW A KNOWN LOCAL ROUTE", [
            "Choose a seen landmark, vertical link, or marked store.",
            "You follow remembered ground one pace at a time. Unseen ground needs your own eyes.",
            "Any key stops the walk. New danger, sound, weather, injury, load change, or a material hazard also stops it.",
            "Selection and cancellation cost no time.",
        ]
    if kind == "aftermath":
        from .aftermath import AFTERMATH_LINES, contracts_for

        quest = state.aftermath_quests[state.active_region_id]
        return AFTERMATH_LINES[state.active_region_id][0].upper(), [
            f"Regional settlement: {quest.branch or 'undecided'}; later work: {quest.status}.",
            "The witnesses have named work arising from that settlement.",
            *[
                f"{contract.title}: {contract.cause}."
                for contract in contracts_for(state)
            ],
        ]
    if kind.startswith("aftermath-contract:"):
        from .aftermath import contract_lines

        contract_id = kind.split("aftermath-contract:", 1)[1]
        contract = state.regional_contracts[contract_id]
        return contract.title.upper(), contract_lines(state, contract_id)
    if kind == "observed-life":
        return INTERFACE_LABELS["watch_records"], observed_life_lines(state)
    if kind == "material":
        from .material_presentation import material_text
        return INTERFACE_LABELS["field_materials"], [material_text("material.overlay.choice.guidance")]
    if kind.startswith("material:"):
        from .materials import inspect_material, point_at

        from .material_presentation import material_text
        return material_text("material.overlay.title"), inspect_material(state, point_at(kind.split(":", 1)[1]))
    if kind.startswith("flask-pour:") or kind.startswith("flask-drink:"):
        from .chemistry import carried_flasks, predicted_reactions
        from .chemistry_presentation import chemistry_format, chemistry_text, reagent_contents_display, reaction_list_display
        from .materials import fields, key, point_at

        pouring = kind.startswith("flask-pour:")
        point = point_at(kind.split(":", 1)[1])
        cell = fields(state).get(key(point))
        rows = [chemistry_format("chemistry.overlay.target", x=point.x, y=point.y, z=f"{point.z:+d}")]
        for index, flask in enumerate(carried_flasks(state)[:9]):
            merged = flask.contents.copy()
            if pouring and cell:
                for reagent, quantity in cell.reagents.items():
                    merged[reagent] = merged.get(reagent, 0) + quantity
            rows.append(chemistry_format("chemistry.overlay.flask_prediction", index=index + 1, flask=flask.id,
                                         contents=reagent_contents_display(flask.contents) if flask.contents else chemistry_text("chemistry.overlay.empty"),
                                         reactions=reaction_list_display(predicted_reactions(merged, cell if pouring else None)) or chemistry_text("chemistry.overlay.none")))
        return (chemistry_text("chemistry.overlay.pour.title") if pouring else chemistry_text("chemistry.overlay.drink.title")), rows
    if kind == "help":
        return INTERFACE_LABELS["clerk_slate"], [*BASE_HELP_LINES, "", *HELP_LINES]
    if kind == "inventory":
        goods = [f"{item_display_name(name)}: {stack.quantity}, {stack.condition} ({COMMODITIES[name]['bulk']} bulk each)" for name, stack in state.carried_goods.items()]
        statuses = [
            f"{name}: {status.remaining} actions; from {status.cause}; {status.consequence}"
            for name, status in state.terrain_statuses.items()
        ]
        return INTERFACE_LABELS["stores_ledger"], [
            f"Capacity: {carried_bulk(state)}/{capacity(state)} bulk",
            f"Weapon: {item_display_name_or_legacy(state.weapon) if state.weapon else 'none'}; "
            f"gear: {item_display_name_or_legacy(state.gear) if state.gear else 'none'}; "
            f"relic: {item_display_name_or_legacy(state.carried_relic) if state.carried_relic else 'none'}",
            f"Passive discoveries: {', '.join(item_display_name(name) for name in state.carried_passives) or 'none'} "
            f"({passive_bulk(state)}/{passive_capacity(state)} bulk)",
            f"Supplies on hand: ammunition {state.ammunition}; oil {state.lamp_oil}; rope {state.rope_uses}; smoke {state.smoke_charges}",
            "Goods:", *(goods or ["none"]),
            f"Consumables: {', '.join(item_display_name(name) for name in state.consumables) or 'none'}",
            "Current conditions:", *(statuses or ["none"]),
            f"Owned weapons: {', '.join(item_display_name(name) for name in state.owned_weapons)}", f"Owned gear: {', '.join(item_display_name(name) for name in state.owned_gear)}",
            "Escape closes without advancing time.",
        ]
    if kind == "household":
        from .people import personal_practice
        from .practices import learned_practice_ids

        lines: list[str] = []
        for person in state.household:
            strongest = max(person.relationships.items(), key=lambda item: (item[1], item[0]))
            related = next(candidate.name for candidate in state.household if candidate.id == strongest[0])
            from .progression_presentation import technique_display_name
            from .progression_presentation import progression_text
            learned = ", ".join(technique_display_name(value) for value in person.learned_techniques) or progression_text("progression.household.none")
            lines.extend((
                f"{person.name} — {person.ancestry} {role_display_name(person.role)}; {person.technique}; {person.injury}",
                f"  {learned}; closest standing: {related} ({strongest[1]:+d})",
            ))
            if personal_practice(person) in learned_practice_ids(person):
                from .progression_presentation import progression_text
                lines.append("  " + progression_text("progression.household.personal_effect"))
        return "JOMON HOUSEHOLD", lines + ["Escape closes without advancing time."]
    if kind == "chronicle":
        from .echoes import lines as echo_lines

        return "JOMON VESSEL CHRONICLE", [*(state.chronicle[-16:] or ["No vessel incident is recorded yet."]), *echo_lines(state)[-6:], "Escape closes without advancing time."]
    if kind == "regional-ledger":
        from .regional_history import ledger_lines

        return "REGIONAL WORK, HISTORY AND FORECAST", ledger_lines(state)
    if kind == "station:workshop" or kind.startswith("workshop:"):
        from .workshop import FITTINGS, describe, fit_cost

        if kind == "station:workshop":
            from .equipment_presentation import equipment_format, equipment_text
            return equipment_text("equipment.overlay.station.title"), [equipment_format("equipment.overlay.station.summary", credit=state.trade_credit), equipment_text("equipment.overlay.station.guidance")]
        if kind == "workshop:store":
            from .equipment_presentation import equipment_text
            return equipment_text("equipment.overlay.store.title"), [equipment_text("equipment.overlay.store.summary"), equipment_text("equipment.overlay.store.stock")]
        if kind.startswith("workshop:slot:"):
            item = equipped_item(state, kind.split(":", 2)[2])
            from .equipment_presentation import equipment_text
            return equipment_text("equipment.overlay.work.title"), describe(state, item) if item else [equipment_text("equipment.overlay.empty")]
        parts = kind.split(":")
        if parts[1] == "buy":
            fitting = FITTINGS[parts[2]]
            from .equipment_presentation import equipment_format, equipment_text
            return equipment_text("equipment.overlay.buy.title"), [fitting.name, fitting.effect, fitting.drawback, equipment_format("equipment.overlay.buy.detail", fitting=fitting.name, effect=fitting.effect, drawback=fitting.drawback, width=fitting.shape[0], height=fitting.shape[1], weight=fitting.weight, price=fitting.price).split("\n")[-1]]
        item = next((item for item in state.items if item.id == parts[2]), None)
        if item is None:
            from .equipment_presentation import equipment_text
            return equipment_text("equipment.overlay.work.title"), [equipment_text("equipment.overlay.item.missing")]
        if parts[1] == "fit":
            fitting = FITTINGS[parts[3]]
            from .equipment_presentation import equipment_format, equipment_text
            return equipment_text("equipment.overlay.fit.title"), equipment_format("equipment.overlay.fit.detail", fitting=fitting.name, item=item_spec(item.kind).name, effect=fitting.effect, drawback=fitting.drawback, weight=fitting.weight, cost=fit_cost(state, parts[3])).split("\n")
        from .equipment_presentation import equipment_text
        return equipment_text("equipment.overlay.confirm.title"), describe(state, item) + [equipment_text("equipment.overlay.confirm.guidance"), equipment_text("equipment.overlay.confirm.costs")]
    if kind.startswith("vessel-refits:"):
        from .vessel_refits import REFITS, STATION_REFITS, installed

        station = kind.split(":", 1)[1]
        lines = [
            "Optional work: preview and cancellation cost no time; fitting costs three actions.",
            "Each refit consumes one physical hold lot plus accountable credit and remains with Jomon.",
        ]
        for index, refit_id in enumerate(STATION_REFITS.get(station, ())):
            refit = REFITS[refit_id]
            status = "INSTALLED" if installed(state, refit_id) else f"one {refit.dependency}; {refit.credit} credit"
            lines.extend((
                f"{index + 1}. {refit.name} — {status}",
                f"   Effect: {refit.effect}",
                f"   Trade-off: {refit.drawback}",
            ))
        return f"{station.upper()} REFITS", lines
    if kind.startswith("station:"):
        from .vessel_refits import REFITS, STATION_REFITS, installed

        station = kind.split(":", 1)[1]
        title, detail = {
            "galley": ("JOMON GALLEY", "Counted provisions become meals here; seasonal stores affect bar and voyage supplies."),
            "repair": ("REPAIR POSITION", f"Tools, spare timber, and rigging serve Jomon's integrity ({state.vessel_integrity}/10)."),
            "berths": ("JOMON BERTHS", "Household adults sleep or recover here as their watches and injuries allow."),
            "bilge": ("BALLAST AND BILGE", "Water collects on the lower deck; repair watches inspect hull access during crises."),
            "provisions": ("PROVISION STORE", "Dry grain and sealed fish are physically counted for route legs and emergencies."),
            "workshop": ("LOWER WORKSHOP", "Armour, tools, and damaged possessions are accounted for beside the cargo hold."),
            "storage": ("SERVING AND DECK STORE", "The counted stores support work at the nearby station."),
            "helm": ("JOMON HELM", "The scheduled pilot steers here; travel changes only at confirmed route legs."),
            "lookout": ("LOOKOUT POSITION", "This exposed upper position improves warning and becomes a defensive voyage station."),
            "gathering": ("COMMON DECK", "Crew gather, train, and dispute work here when schedules and memories align."),
            "market": ("VISITING BERTH", "A regional merchant trades here during a recorded visit."),
        }.get(station, ("JOMON WORK POSITION", "Household work is done at this station."))
        fitted = [REFITS[refit_id].name for refit_id in STATION_REFITS.get(station, ()) if installed(state, refit_id)]
        lines = [detail]
        if station == "berths":
            lines.append(f"R. Rest six actions to restore personal mana ({state.courier.mana}/{state.courier.max_mana}); moored and out of danger only.")
        if station == "gathering":
            from .household_stories import station_choices

            lines.extend(f"{key}. {label} — {'AVAILABLE' if available else 'NEEDS ' + requirement}" for key, label, _, available, requirement in station_choices(state))
            from .progression_presentation import progression_text
            lines.append(progression_text("progression.station.gathering.journal_guidance"))
        if station in STATION_REFITS:
            lines.append("V. Inspect optional physical refits." + (f" Installed: {', '.join(fitted)}." if fitted else " None installed here."))
        return title, lines + ["Inspection costs no time. Escape closes."]
    if kind == "hold":
        cargo = [f"{item_display_name(name)}: {stack.quantity}, {stack.condition}" for name, stack in state.vessel_cargo.items()]
        return "HOLD AND LOCAL PROBLEM", cargo + ["", state.region.condition, state.region.pressure, state.region.objective_text, f"Trade credit: {state.trade_credit}"]
    if kind == "equipment":
        return "STORES — PREPARE AT TAVERN C", [
            "Weapons: " + ", ".join(item_display_name(name) for name in state.owned_weapons), "Secondary gear: " + ", ".join(item_display_name(name) for name in state.owned_gear),
            "These physical stores remain aboard; all expedition selection happens at tavern C.", "Escape closes without time.",
        ]
    if kind == "quest:regional":
        from .quest_presentation import regional_quest_title

        quest = state.questlines[state.active_region_id]
        return regional_quest_title(state.active_region_id).upper(), [
            f"Opening decision: {quest.branch or 'none'}.",
            f"Material work: {state.objective_status}; optional lead: {'completed' if quest.optional_done else 'open'}.",
            "This decision changes people, work, hazards, and later visits.",
            "Choose one recorded regional settlement.",
        ]
    if kind == "workline":
        from .worklines import lines
        text = lines(state)
        return text[0].upper(), text[1:]
    if kind == "quest:arc":
        from .quests import arc_next_region, arc_title, current_arc

        arc = current_arc(state)
        next_region = arc_next_region(state)
        final_stage = 5 if arc is state.cross_region_arc else 4
        return arc_title(state).upper(), [
            f"Witnessed account {arc.stage + 1 if arc and arc.stage < final_stage else final_stage}/{final_stage}; terms: {arc.branch if arc and arc.branch else 'not yet bound'}.",
            f"Current witness: {state.region.name}.",
            f"Next required region: {state.regions[next_region].name if next_region else 'ending decision here'}.",
            "The compared record is physical evidence, not a prophecy.",
        ]
    if kind.startswith("contact-service:"):
        contact_id = kind.split(":", 1)[1]
        contact = next(
            person for person in state.contacts[state.active_region_id]
            if person.id == contact_id
        )
        from .regional_history import network_institution_for_contact
        from .inspection import contextual_advice

        network = network_institution_for_contact(state, contact_id)
        if network:
            return contact.name.upper(), [
                f"{contact.role}; disposition {contact.disposition:+d}; interest {contact.interest}.",
                f"Represents {network.name}: {network.goal}.",
                f"Dependency {network.dependency}; production {network.production}.",
                f"Service: {network.service}.",
                f"Opposition: {network.opposition_reason}.",
                f"Network trust {network.trust:+d}; obligation {network.obligation}; confidence {network.confidence:+d}.",
                contextual_advice(state, contact.name),
                *[f"- {memory}" for memory in contact.memories[-3:]],
            ]
        return contact.name.upper(), [
            f"{contact.role}; disposition {contact.disposition:+d}; interest {contact.interest}.",
            "This local worker can mark a cache, teach practical knowledge, or treat an injury.",
            contextual_advice(state, contact.name),
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
        from .inspection import contextual_advice
        return contact.name.upper(), [
            f"Role: {contact.role}; disposition: {contact.disposition:+d}",
            f"Material interest: {contact.interest}", f"Local work: {state.objective_status}",
            contextual_advice(state, contact.name),
            "Significant memories:", *[f"- {memory}" for memory in memories], "Escape closes without time.",
        ]
    if kind == "tavern":
        return "TAVERN DEPARTURE TERMS", _tavern_lines(state)
    if kind == "tavern:support":
        return INTERFACE_LABELS["household_counsel"], [f"{index + 1}. {name} — {detail}" for index, (name, detail) in enumerate(SUPPORTS.values())] + ["Name a household aid; Escape folds the slate."]
    if kind == "tavern:relic":
        rows = [name for name in RELICS if state.relics.get(name, 0)]
        return INTERFACE_LABELS["relic_case"], ["0. Carry none", *[f"{index + 1}. {item_display_name_or_legacy(name)} ({state.relics[name]}) — {RELICS[name]}" for index, name in enumerate(rows)], "Name a relic to carry; Escape folds the case."]
    if kind == "relic:select":
        rows = list(dict.fromkeys(
            item.kind.split(":", 1)[1] for item in state.items
            if item.owner_id == state.active_courier_id and item.location == "pack"
            and item.kind.startswith("relic:")
        ))
        return INTERFACE_LABELS["carried_relic"], [
            *[
                f"{index + 1}. {'>' if state.carried_relic == name else ' '} "
                f"{item_display_name_or_legacy(name)} — {RELICS[name]}"
                for index, name in enumerate(rows)
            ],
            "Naming a carried relic spends no watch; X puts the named relic to use.",
        ]
    if kind == "field-use":
        from .preparations import carried_preparations, preparation_status, preparation_status_text
        from .preparation_presentation import preparation_description, preparation_display_name, preparation_format, preparation_text

        keys = "123456789abcdefg"
        lines = []
        for key, preparation_id in zip(keys, carried_preparations(state)):
            available, reason = preparation_status(state, preparation_id)
            status = preparation_text("preparation.overlay.ready") if available else preparation_format("preparation.overlay.needs", condition=preparation_status_text(reason))
            lines.append(preparation_format("preparation.overlay.row", key=key, preparation=preparation_display_name(preparation_id), description=preparation_description(preparation_id), status=status))
        lines.append("X. Use the carried bottle, selected relic, or ordinary readied gear.")
        from .manoeuvres import known

        if known(state):
            from .progression_presentation import progression_text
            lines.append(progression_text("progression.choice.manoeuvre.guidance"))
        lines.append(preparation_text("preparation.overlay.guidance"))
        return INTERFACE_LABELS["field_kit"], lines
    if kind == "tavern:passive":
        rows = list(state.owned_passives)
        keys = "123456789abc"
        lines = [
            f"{keys[index]}. [{state.carried_passives.get(name, 0)}/{state.owned_passives[name]}] {item_display_name(name)} "
            f"({PASSIVES[name][0]} bulk each) — {PASSIVES[name][1]}"
            for index, name in enumerate(rows[: len(keys)])
        ]
        return INTERFACE_LABELS["packed_findings"], lines + [
            f"Load: {passive_bulk(state)}/{passive_capacity(state)} bulk. The named mark changes the carry; Escape folds the ledger."
        ]
    if kind == "bartender":
        schedule = state.actor_schedules.get(state.bartender.id)
        from .inspection import contextual_advice
        return state.bartender.name.upper(), [
            vessel_format("vessel.bar.profile", role=role_display_name(state.bartender.role).title(), schedule=schedule_display_name(schedule.activity, absent="duties") if schedule else schedule_display_name("", absent="duties")),
            state.bartender.background,
            vessel_format("vessel.bar.opinion", opinion=f"{state.bartender.relationships.get(state.active_courier_id or '', 0):+d}"),
            vessel_format("vessel.bar.stock_note", bartender=state.bartender.name.split()[0]),
            contextual_advice(state, state.bartender.name),
            vessel_format("vessel.bar.menu"), vessel_format("vessel.bar.support"), vessel_format("vessel.bar.leave"),
        ]
    if kind == "bartender:drinks":
        lines = [vessel_format("vessel.bar.credit", credit=state.trade_credit, season=calendar_at(state).season)]
        for index, (drink_id, drink) in enumerate(DRINKS.items()):
            lines.append(vessel_format("vessel.bar.stock.line", index=index + 1, drink=drink_display_name(drink_id), benefit=drink_benefit(drink_id), drawback=drink_drawback(drink_id), stock=state.bartender_stock.get(drink_id, 0)))
        return vessel_format("vessel.bar.stock.title", bartender=state.bartender.name.split()[0].upper()), lines + [vessel_format("vessel.bar.stock.prompt")]
    if kind.startswith("bartender:drink:"):
        drink_id = kind.split(":", 2)[2]
        drink = DRINKS[drink_id]
        return drink_display_name(drink_id).upper(), [
            vessel_format("vessel.bar.detail.benefit", benefit=drink_benefit(drink_id)), vessel_format("vessel.bar.detail.drawback", drawback=drink_drawback(drink_id)),
            vessel_format("vessel.bar.detail.duration", duration=drink.duration, cost=drink.cost),
            vessel_format("vessel.bar.detail.stock", stock=state.bartender_stock.get(drink.id, 0)),
            vessel_format("vessel.bar.detail.drink"), vessel_format("vessel.bar.detail.bottle"),
        ]
    if kind == "incident" and state.pending_incident:
        people = {person.id: person.name for person in state.household}
        names = " and ".join(people.get(actor, actor) for actor in state.pending_incident.participants)
        return "TAVERN INCIDENT", [
            f"{names}: {state.pending_incident.kind}.",
            f"Cause: {state.pending_incident.cause}.",
            "M. Mediate", "S. Support the first speaker", "F. Let the fight run its course",
            "Only a witnessed, present struggle can bring mortal harm.",
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
            f"{state.merchant.name}, {role_display_name(state.merchant.role)}; "
            f"{schedule_display_name(schedule.activity, absent='routes') if schedule else schedule_display_name('', absent='routes')}.",
            state.merchant.background,
            f"Opinion of courier: {state.merchant.relationships.get(state.active_courier_id or '', 0):+d}.",
            f"Jomon credit: {state.trade_credit}",
        ]
        for index, item in enumerate(state.merchant_stock):
            cost = max(1, MERCHANT_ITEMS[item][0] - (1 if state.support == "factor surety" else 0))
            lines.append(f"{index + 1}. {item_display_name_or_legacy(item)} — {cost} credit")
        return state.merchant.name.upper(), lines + ["Number buys; Escape closes. Stock leaves on departure."]
    if kind == "destination":
        lines = [
            f"{index + 1}. {state.regions[region_id].name}"
            + (" — current mooring" if region_id == state.active_region_id else "")
            + f"; {state.regions[region_id].process_name}, measure {state.regions[region_id].process_stage}"
            for index, region_id in enumerate(DESTINATIONS)
        ]
        return "JOMON ROUTE CHART", lines + [
            "Travel takes the charted time; weather, travellers, or cargo claims may interrupt the passage.",
            "Number sets course; Escape keeps the current mooring without time.",
        ]
    if kind == "voyage":
        from .ship_crises import crisis_lines
        return "VOYAGE DANGER", crisis_lines(state)
    if kind.startswith("ship-work:"):
        from .ship_crises import work_lines
        from .vessel_refits import STATION_REFITS, refit_station_at

        lines = work_lines(state, kind.split(":", 1)[1])
        if refit_station_at(state) in STATION_REFITS:
            lines.append("V. Inspect optional refits at this physical station; preview costs no time.")
        return "COUNTED VESSEL WORK", lines
    if kind == "quit":
        return INTERFACE_LABELS["leave_book"], ["Y signs departure. N or Escape returns to Jomon."]
    if kind.startswith("character-sheet:"):
        from .character import character_sheet
        from .people import person_by_id

        person = person_by_id(state, kind.split(":", 1)[1])
        return (f"{person.name.upper()} / {INTERFACE_LABELS['courier_record']}", character_sheet(person)) if person else (INTERFACE_LABELS["vacant_berth"], ["This person is no longer aboard."])
    if kind.startswith("person:"):
        from .people import person_by_id, personal_practice
        from .practices import learned_practice_ids
        from .progression_presentation import progression_format, progression_text, technique_display_name

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
            f"{person.name} — {person.ancestry} {role_display_name(person.role)}; {standing}",
            f"Technique: {person.technique}",
            progression_format("progression.person.learned", practices=", ".join(technique_display_name(value) for value in person.learned_techniques) or progression_text("progression.person.none")),
            f"Health: {person.health}/{person.max_health}; {person.injury}",
            f"Competencies /20: Strategy {person.strategy}; Speech {person.speech}; Wayfinding {person.wayfinding}",
            f"Fieldcraft {person.fieldcraft}; Craft {person.craft}",
            f"Equipment affinity: {', '.join(person.equipment)}",
            f"Current physical kit: {', '.join(physical) if physical else 'none'}",
            f"Build tendency: {person.build_tendency}",
            f"Background: {person.background}",
            "Memories:", *[f"- {memory}" for memory in (person.memories or ["No shared expedition yet."])],
        ]
        from .practices import PRACTICES

        practice_details = [
            progression_format("progression.person.practice_effect", practice=technique_display_name(name), description=PRACTICES[name].description)
            for name in learned_practice_ids(person) if name in PRACTICES
        ]
        lines[3:3] = practice_details
        if personal_practice(person) in learned_practice_ids(person):
            lines.insert(3, progression_text("progression.person.personal_effect"))
        if person in state.household and person.id != state.active_courier_id and person.alive and person.available:
            lines.append("S. Switch to this courier (zero time)")
        elif person not in state.household:
            lines.extend((f"Terms: {person.recruitment_terms}", "R. Offer a voluntary berth", "D. Defer without closing the invitation"))
        if person.id != state.active_courier_id and state.courier and "teaching" in state.courier.skill_nodes:
            lines.append(progression_text("progression.person.teach"))
        return person.name.upper(), lines + ["Escape closes without time."]
    return INTERFACE_LABELS["recorded_notice"], [kind, "Escape closes without advancing time."]


def _handle_overlay(state: GameState, kind: str, key: int) -> tuple[str | None, bool]:
    char = chr(key).lower() if 0 <= key < 256 else ""
    if kind == "field-traveller" and char in {"a", "b"}:
        from .landscape_variation import traveller_choice

        traveller_choice(state, char)
        return kind, False
    if kind == "gangplank" and char in {"1", "2"}:
        from .actions import depart
        from .vehicles import board_tug

        result = depart(state) if char == "1" else board_tug(state)
        return (None if result.changed else kind), False
    if kind == "station:gathering" and char in {"j", "k"}:
        return ("journal-write:0" if char == "j" else "journal-study:0"), False
    if kind.startswith(("journal-write:", "journal-study:")):
        from .skill_tree import journals_at_hand, study_journal, write_journal

        writing = kind.startswith("journal-write:")
        page = int(kind.split(":", 1)[1])
        rows = ([node for node in state.courier.skill_nodes if node not in state.courier.journal_nodes]
                if writing else journals_at_hand(state))
        if key == 27 or char == "b":
            return "station:gathering", False
        if char == "n":
            return f"{'journal-write' if writing else 'journal-study'}:{min(max(0, (len(rows) - 1) // 8), page + 1)}", False
        if char == "p":
            return f"{'journal-write' if writing else 'journal-study'}:{max(0, page - 1)}", False
        if char in "12345678":
            index = page * 8 + int(char) - 1
            if index < len(rows):
                changed, message = (write_journal(state, rows[index]) if writing else study_journal(state, rows[index].id))
                if not changed:
                    state.add_message(message, priority=2)
                return ("station:gathering" if changed else kind), False
        return kind, False
    if kind.startswith("teach-person:"):
        from .people import person_by_id
        from .skill_tree import teach_node

        _, recipient_id, page_text = kind.split(":")
        page = int(page_text)
        recipient = person_by_id(state, recipient_id)
        rows = [node for node in state.courier.skill_nodes if node not in recipient.skill_nodes]
        if key == 27 or char == "b":
            return f"person:{recipient_id}", False
        if char == "n":
            return f"teach-person:{recipient_id}:{min(max(0, (len(rows) - 1) // 8), page + 1)}", False
        if char == "p":
            return f"teach-person:{recipient_id}:{max(0, page - 1)}", False
        if char in "12345678":
            index = page * 8 + int(char) - 1
            if index < len(rows):
                changed, message = teach_node(state, recipient_id, rows[index])
                if not changed:
                    state.add_message(message, priority=2)
                return (f"person:{recipient_id}" if changed else kind), False
        return kind, False
    if kind.startswith("craft-catalog:"):
        from .production import RECIPES, at_shore_site, delegate, gather, make, stations_here

        page = int(kind.split(":", 1)[1])
        ids = [key for key, recipe in RECIPES.items() if recipe.station in stations_here(state)]
        max_page = max(0, (len(ids) - 1) // 8)
        if char == "n":
            return f"craft-catalog:{min(max_page, page + 1)}", False
        if char == "p":
            return f"craft-catalog:{max(0, page - 1)}", False
        if char == "m":
            return "craft-mix", False
        if char in "90" and at_shore_site(state):
            changed, message = gather(state, 0 if char == "9" else 1)
            if not changed:
                state.add_message(message, priority=2)
            return kind, False
        if char in "12345678abcdefgh":
            index = "12345678abcdefgh".index(char) % 8 + page * 8
            if index < len(ids):
                changed, message = (delegate(state, ids[index]) if char in "abcdefgh" else make(state, ids[index]))
                if not changed:
                    state.add_message(message, priority=2)
                return kind, False
    if kind == "craft-mix":
        if char == "b":
            return "craft-catalog:0", False
        if char in "123456789":
            from .chemistry import carried_flasks

            flasks = carried_flasks(state)
            index = int(char) - 1
            if index < len(flasks):
                return f"craft-fill:{flasks[index].id}:0", False
    if kind.startswith("craft-fill:"):
        from .chemistry import carried_ingredients, fill_flask

        parts = kind.split(":")
        flask_id, page = parts[1], int(parts[2])
        ingredients = carried_ingredients(state)
        if char == "b":
            return "craft-mix", False
        if char == "d":
            return f"craft-distil:{flask_id}", False
        if char == "n":
            return f"craft-fill:{flask_id}:{min(max(0, (len(ingredients) - 1) // 8), page + 1)}", False
        if char == "p":
            return f"craft-fill:{flask_id}:{max(0, page - 1)}", False
        if char in "12345678":
            index = page * 8 + int(char) - 1
            if index < len(ingredients):
                changed, message = fill_flask(state, flask_id, ingredients[index].id)
                if not changed:
                    state.add_message(message, priority=2)
                return ("craft-mix" if changed else kind), False
    if kind == "spellbook" and char in "123456":
        from .magic import SPELL_TIERS

        return f"spell-tier:{list(SPELL_TIERS)[int(char) - 1]}", False
    if kind == "spellbook" and char == "r":
        from .magic import restore_at_shrine

        changed, message = restore_at_shrine(state)
        if not changed:
            state.add_message(message, priority=2)
        return kind, False
    if kind.startswith("spell-tier:") and char == "b":
        return "spellbook", False
    if kind == "skill-tree" and char in "123456789a":
        from .skill_tree import BRANCHES

        return f"skill-branch:{list(BRANCHES)['123456789a'.index(char)]}", False
    if kind.startswith("skill-branch:"):
        if char == "b":
            return "skill-tree", False
        if char in "123456":
            from .skill_tree import NODES, buy_node

            branch = kind.split(":", 1)[1]
            node = [node for node in NODES.values() if node.branch == branch][int(char) - 1]
            _, message = buy_node(state, node.id)
            state.add_message(message, priority=3)
            return kind, False
    if kind == "station:gathering" and char in "123":
        from .household_stories import STORIES, eligibility

        story = STORIES[int(char)-1]
        status = state.vessel_changes.get(f"household-story:{story.id}:status", "unopened")
        eligible, _ = eligibility(state, story.id)
        return (f"household-story:{story.id}" if eligible or status in {"active", "completed"} else kind), False
    if kind.startswith("household-story:") and char in {"o", "d", "w", "p", "c", "h", "r"}:
        from .actions import _advance_world
        from .household_stories import resolve

        changed, message, steps = resolve(state, kind.split(":", 1)[1], char)
        if changed:
            _advance_world(state, steps=steps)
        state.add_message(message, priority=3)
        return (None if changed else kind), False
    if kind == "mastery" and char in "123456789abc":
        from .actions import _advance_world
        from .manoeuvres import known, perform

        rows = known(state)
        index = "123456789abc".index(char)
        if 0 <= index < len(rows):
            changed, message, steps = perform(state, rows[index].id)
            if changed:
                _advance_world(state, steps=steps)
            state.add_message(message, priority=3)
            return (None if changed else kind), False
        return kind, False
    if kind.startswith("craft-distil:"):
        from .chemistry import carried_flasks, distill_flask

        flask_id = kind.split(":", 1)[1]
        flask = next((item for item in carried_flasks(state) if item.id == flask_id), None)
        if char == "b" or key == 27:
            return f"craft-fill:{flask_id}:0", False
        if char in "1234" and flask and int(char) <= len(flask.contents):
            reagent = list(flask.contents)[int(char) - 1]
            changed, message = distill_flask(state, flask_id, reagent)
            if not changed:
                state.add_message(message, priority=2)
            return ("craft-mix" if changed else kind), False
        return kind, False
    if kind.startswith("situation:") and char in {"t", "m", "a"}:
        from .actions import _advance_world
        from .situations import resolve

        changed, message, steps = resolve(state, kind.split(":", 1)[1], char)
        if changed:
            _advance_world(state, steps=steps)
        state.add_message(message, priority=3)
        return (None if changed else kind), False
    if kind == "sanctum" and char in {"o", "b", "s", "h"}:
        from .actions import _advance_world
        from .sanctums import shrine_choice

        changed, message, steps = shrine_choice(state, char)
        if changed:
            _advance_world(state, steps=steps)
        state.add_message(message, priority=3)
        return (None if changed else kind), False
    if kind == "aftermath" and char in "12":
        from .aftermath import contracts_for

        contracts = contracts_for(state)
        index = int(char) - 1
        return (
            "aftermath-contract:" + contracts[index].id
            if index < len(contracts) else kind
        ), False
    if kind.startswith("aftermath-contract:"):
        if char == "b" or key == 27:
            return "aftermath", False
        if char in {"a", "d", "w", "s", "x"}:
            contract_id = kind.split("aftermath-contract:", 1)[1]
            result = use_aftermath_contract(state, contract_id, char)
            return (None if result.changed else kind), False
        return kind, False
    if kind == "material" and key in map(ord, "12345"):
        dx, dy = ((0, 0), (0, -1), (1, 0), (0, 1), (-1, 0))[key - ord("1")]
        point = Position(state.position.x + dx, state.position.y + dy, state.position.z)
        return f"material:{point.x},{point.y},{point.z}", False
    if kind.startswith("material:") and ord("a") <= key <= ord("k"):
        from .materials import VERBS, handle_material, point_at

        changed, message = handle_material(state, VERBS[key - ord("a")], point_at(kind.split(":", 1)[1]))
        state.add_message(message, priority=3)
        return (None if changed else kind), False
    if kind.startswith("material:") and char in {"l", "m"}:
        return ("flask-pour:" if char == "l" else "flask-drink:") + kind.split(":", 1)[1], False
    if kind.startswith(("flask-pour:", "flask-drink:")):
        if char == "b":
            return "material:" + kind.split(":", 1)[1], False
        if char in "123456789":
            from .chemistry import carried_flasks, drink_flask, pour_flask
            from .materials import point_at

            flasks = carried_flasks(state)
            index = int(char) - 1
            if index < len(flasks):
                changed, message = (pour_flask(state, flasks[index].id, point_at(kind.split(":", 1)[1]))
                                    if kind.startswith("flask-pour:") else drink_flask(state, flasks[index].id))
                if not changed:
                    state.add_message(message, priority=2)
                return (None if changed else kind), False
        return kind, False
    if kind == "material" and char == "a":
        from .aftermath import contracts_for, near_contract_site

        contract = next(
            (
                contract for contract in contracts_for(state)
                if contract.stage == 1 and near_contract_site(state, contract)
            ),
            None,
        )
        return (
            "aftermath-contract:" + contract.id if contract else kind
        ), False
    if char == "m" and kind in {"material", "field-use"}:
        return "mastery", False
    if char == "w" and (kind == "material" or kind.startswith("contact-service:")):
        from .worklines import WORKLINES
        if state.location == "region" and state.active_region_id in WORKLINES:
            return "workline", False
    if kind.startswith("contact-service:") and char == "a":
        from .aftermath import contracts_for

        return ("aftermath" if contracts_for(state) else kind), False
    if kind == "workline" and char and key != 27:
        from .worklines import resolve
        result = resolve(state, char)
        return (None if result.changed else kind), False
    if key == 27:
        if kind.startswith("spell-tier:"):
            return "spellbook", False
        if kind.startswith("workshop:"):
            return "station:workshop", False
        if kind.startswith("vessel-refits:"):
            return "station:" + kind.split(":", 1)[1], False
        if kind.startswith("bartender:"):
            return "bartender", False
        return ("bartender" if kind.startswith("tavern:") else None), False
    if kind in {"help", "inventory", "equipment", "household", "hold", "contact", "info", "chronicle", "regional-ledger", "observed-life", "navigation"} or kind.startswith("contact:"):
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
    if kind == "station:berths" and char == "r":
        from .magic import rest_at_berths

        changed, message = rest_at_berths(state)
        if not changed:
            state.add_message(message, priority=2)
        return kind, False
    if kind.startswith("ship-work:"):
        if char == "b":
            return None, False
        if char == "v":
            from .vessel_refits import STATION_REFITS, refit_station_at

            station = refit_station_at(state)
            if station in STATION_REFITS:
                return "vessel-refits:" + station, False
        if char == "f":
            from .ship_crises import work
            changed, message = work(state, kind.split(":", 1)[1])
            state.add_message(message, priority=3)
            return (None if changed else kind), False
        return kind, False
    if kind.startswith("vessel-refits:"):
        station = kind.split(":", 1)[1]
        if char == "b":
            return "station:" + station, False
        if char.isdigit():
            from .vessel_refits import STATION_REFITS, install_refit

            rows = STATION_REFITS.get(station, ())
            index = int(char) - 1
            if 0 <= index < len(rows):
                changed, _ = install_refit(state, rows[index])
                return ("station:" + station if changed else kind), False
        return kind, False
    if kind.startswith("station:") and char == "v":
        from .vessel_refits import STATION_REFITS

        station = kind.split(":", 1)[1]
        if station in STATION_REFITS:
            return "vessel-refits:" + station, False
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
            state.add_message(f"{rows[index]} is ready for its next use.")
            return None, False
    if kind == "field-use":
        from .preparations import carried_preparations

        keys = "123456789abcdefg"
        if char in keys:
            rows = carried_preparations(state)
            index = keys.index(char)
            if index < len(rows):
                result = use_gear(state, rows[index])
                return (None if result.time_advanced else kind), False
        if char == "x":
            result = use_gear(state)
            return (None if result.time_advanced else kind), False
        if char == "m":
            return "mastery", False
        return kind, False
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
        result = use_contact_service(state, char, kind.split(":", 1)[1])
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
        if char == "c":
            return f"character-sheet:{person_id}", False
        if char == "t" and state.courier and "teaching" in state.courier.skill_nodes:
            return f"teach-person:{person_id}:0", False
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
    try:
        curses.set_escdelay(25)
    except (curses.error, AttributeError):
        pass
    screen.keypad(True)
    _init_colours()
    _enable_mouse()
    try:
        return _play_loop(screen, state)
    finally:
        _disable_mouse()
        try:
            screen.timeout(-1)
        except curses.error:
            pass


def _play_loop(screen: curses.window, state: GameState) -> GameState:
    from .vessel import JOMON_GANGPLANK

    overlay: OverlayView | None = None
    inventory_view: InventoryView | None = None
    route_view: RouteChartView | None = None
    target_view: TargetView | None = None
    look_view: LookView | None = None
    circuit_view: CircuitView | None = None
    local_route: RoutePlan | None = None
    local_route_index = 0
    while True:
        _draw_base(screen, state)
        height, width = screen.getmaxyx()
        if inventory_view and height >= MIN_HEIGHT and width >= MIN_WIDTH:
            _draw_inventory(screen, state, inventory_view)
        elif route_view and height >= MIN_HEIGHT and width >= MIN_WIDTH:
            _draw_route_chart(screen, state, route_view)
        elif target_view and height >= MIN_HEIGHT and width >= MIN_WIDTH:
            _draw_targeting(screen, state, target_view)
        elif look_view and height >= MIN_HEIGHT and width >= MIN_WIDTH:
            _draw_look(screen, state, look_view)
        elif circuit_view and height >= MIN_HEIGHT and width >= MIN_WIDTH:
            _draw_circuit(screen, state, circuit_view)
        elif overlay and height >= MIN_HEIGHT and width >= MIN_WIDTH:
            if overlay.kind == "vehicle-interior":
                _draw_vehicle_interior(screen, state, overlay)
            elif dialogue_choices(state, overlay.kind):
                _draw_dialogue_overlay(screen, state, overlay)
            else:
                title, lines = _overlay_lines(state, overlay.kind)
                _overlay(screen, title, lines, overlay)
        if (
            local_route
            and not any((inventory_view, route_view, target_view, look_view, circuit_view, overlay))
            and height >= MIN_HEIGHT and width >= MIN_WIDTH
        ):
            try:
                screen.timeout(45)
                interrupted = screen.getch()
            except curses.error:
                interrupted = -1
            finally:
                screen.timeout(-1)
            if interrupted != -1:
                state.add_message("You stop following the known route; no extra action is spent.")
                local_route = None
                continue
            progress = advance_route(state, local_route, local_route_index)
            local_route_index = progress.next_index
            if progress.stop_reason or progress.finished:
                state.add_message(progress.stop_reason or "Destination reached.", priority=2)
                local_route = None
            continue
        event = normalise_input(screen.getch())
        key = event.key
        if key == curses.KEY_RESIZE:
            continue
        if key == 26:  # Ctrl-Z: action clock remains stopped while suspended.
            if suspend_terminal(screen):
                state.add_message("Jomon resumes exactly where the action clock stopped.")
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
        if look_view:
            if _handle_look(state, look_view, event, screen_size=(height, width)):
                look_view = None
            continue
        if circuit_view:
            if _handle_circuit(state, circuit_view, event, screen_size=(height, width)):
                circuit_view = None
            continue
        if overlay:
            closed, should_quit = _handle_overlay_view(state, overlay, event)
            if should_quit:
                return state
            if closed:
                selected_route = overlay.result
                overlay = None
                if selected_route and selected_route.startswith("spell:"):
                    target_view = TargetView.begin_spell(state, selected_route.split(":", 1)[1])
                    continue
                if selected_route:
                    try:
                        local_route = plan_route(state, selected_route)
                    except RouteUnavailable as exc:
                        state.add_message(str(exc), priority=2)
                    else:
                        local_route_index = 0
                        state.add_message(
                            f"Following remembered ground toward {local_route.label}; any key stops."
                        )
            continue
        normalized = ord(chr(key).lower()) if 0 <= key < 256 else key
        if event.kind == "mouse" and event.button == "left":
            _, _, main_height, map_width = _cursor_screen_position(
                state, state.position, height, width
            )
            if 1 <= event.x < map_width - 1 and 1 <= event.y < main_height - 1:
                rows = map_rows(state)
                origin_x, origin_y = camera_origin(
                    state.position, max(map(len, rows)), len(rows), map_width - 2, main_height - 2
                )
                look_view = LookView(Position(
                    origin_x + event.x - 1,
                    origin_y + event.y - 1,
                    state.position.z,
                ))
        elif normalized in MOVES:
            move(state, *MOVES[normalized])
        elif normalized == ord(";"):
            look_view = LookView.begin(state)
        elif key == ord("\\"):
            from .circuits import space_id

            if space_id(state):
                circuit_view = CircuitView.begin(state)
            else:
                from .circuit_presentation import circuit_text
                state.add_message(circuit_text("circuit.target.no_space"))
        elif normalized in {10, 13, ord("e")}:
            result = interact(state)
            if result.overlay and result.overlay.startswith("inventory:container:"):
                source = result.overlay.split("inventory:", 1)[1]
                inventory_view = InventoryView.begin(state, source)
            elif result.overlay == "route-chart":
                route_view = RouteChartView.begin(state)
            elif result.overlay == "tabletop":
                from .dumbest_dungeon.expedition_ui import run_expedition

                run_expedition(screen, state)
            elif result.overlay == "tavern-draw":
                from .tavern_draw_ui import run_tavern_draw

                run_tavern_draw(screen, state)
            elif result.overlay == "tavern-dice":
                from .tavern_dice_ui import run_tavern_dice

                run_tavern_dice(screen, state)
            else:
                overlay = OverlayView(result.overlay) if result.overlay else None
        elif normalized == ord("a"):
            if state.combat_active and state.weapon:
                target_view = TargetView.begin(state)
            else:
                attack(state)
        elif normalized == ord("g"):
            guard(state)
        elif normalized == ord("t"):
            if state.active_vehicle_id:
                state.add_message("Disembark before following a walking route; use the helm to steer.")
            elif state.location != "region":
                state.add_message("Known-route following is available during regional expeditions.")
            elif not navigation_targets(state):
                state.add_message("No other seen landmark or marked store is known yet.")
            else:
                overlay = OverlayView("navigation")
        elif normalized == ord("x"):
            from .preparations import carried_preparations

            preparations = carried_preparations(state)
            carried_relics = list(dict.fromkeys(
                item.kind.split(":", 1)[1] for item in state.items
                if item.owner_id == state.active_courier_id and item.location == "pack"
                and item.kind.startswith("relic:")
            ))
            if preparations:
                overlay = OverlayView("field-use")
            elif len(carried_relics) > 1:
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
        elif normalized == ord("m"):
            if state.combat_active:
                overlay = OverlayView("mastery")
            else:
                from .progression_presentation import progression_text
                state.add_message(progression_text("progression.manoeuvre.unavailable"))
        elif normalized == ord("z"):
            overlay = OverlayView("regional-ledger")
        elif normalized == ord("c") and state.courier:
            overlay = OverlayView(f"character-sheet:{state.courier.id}")
        elif normalized == ord("p") and state.courier:
            overlay = OverlayView("skill-tree")
        elif normalized == ord("d") and state.courier:
            overlay = OverlayView("spellbook")
        elif normalized == ord("w") and state.courier:
            from .production import stations_here

            if stations_here(state):
                overlay = OverlayView("craft-catalog:0")
            else:
                state.add_message("Crafting needs a physical work area outside the tavern.", priority=2)
        elif normalized == ord("o"):
            overlay = OverlayView("observed-life")
        elif key == 9:
            if state.active_vehicle_id:
                overlay = OverlayView("vehicle-interior")
            elif state.location == "jomon" and state.jomon_space == "vessel" and state.position == JOMON_GANGPLANK:
                overlay = OverlayView("gangplank")
            else:
                state.add_message("Tab opens the tug choice at the gangplank or a boarded vehicle's interior.")
        elif normalized == ord("?"):
            overlay = OverlayView("help")
        elif normalized == ord("s"):
            if state.location != "jomon":
                state.add_message("The chronicle can be sealed aboard Jomon, beyond immediate danger.")
            else:
                try:
                    save_game(state)
                    state.add_message("Jomon's chronicle is sealed.")
                except SaveError as exc:
                    state.add_message(str(exc))
        elif normalized == ord("q"):
            overlay = OverlayView("quit")
