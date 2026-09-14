"""Optional point-buy specification before the first courier watch."""

from __future__ import annotations

import curses

from .character import (
    ANCESTRIES, ATTRIBUTE_POINTS, COMPETENCIES, COMPETENCY_POINTS, ORIGINS, PEOPLE_EFFECTS,
    TRAITS, apply_character_spec, default_allocation,
)
from .state import ATTRIBUTES, GameState
from .tavern_games_ui import accent, meter, put

FIELDS = ("crew", "name", "ancestry", "origin", "trait", *ATTRIBUTES, *COMPETENCIES)
PANEL_WIDTH = 78
PANEL_HEIGHT = 25


def _panel_origin(screen: curses.window) -> tuple[int, int]:
    height, width = screen.getmaxyx()
    return max(0, (height - PANEL_HEIGHT) // 2), max(0, (width - PANEL_WIDTH) // 2)


def _draw(screen: curses.window, state: GameState, crew_index: int, name: str,
          ancestry: str, origin: str, trait: str, attributes: dict[str, int],
          competencies: dict[str, int], cursor: int, message: str) -> None:
    from .terminal import _frame

    screen.erase()
    top, left = _panel_origin(screen)
    height, width = screen.getmaxyx()
    _frame(screen, top, left, min(PANEL_HEIGHT, height), min(PANEL_WIDTH, width), "COURIER SPECIFICATION / FIRST WATCH")
    put(screen, top + 1, left + 3, "Choose a household adult; their role and existing bonds remain.", accent("ui_accent"))
    put(screen, top + 2, left + 3, "H/L changes the selected field. Enter names your courier. S accepts.")
    crew = state.household[crew_index]
    values = {
        "crew": f"{crew.name}  /  {crew.role}", "name": name,
        "ancestry": ancestry, "origin": origin.title(), "trait": trait,
        **{key: str(value) for key, value in attributes.items()},
        **{key: str(value) for key, value in competencies.items()},
    }
    for index, key in enumerate(FIELDS):
        row = top + 3 + index
        active = index == cursor
        label = f"{'>' if active else ' '} {key.title():12} {values[key]:31}"
        put(screen, row, left + 3, label,
            accent("ui_accent", curses.A_REVERSE | curses.A_BOLD) if active else
            accent("ui_heading") if key in {"crew", "name"} else 0)
        if key in ATTRIBUTES:
            put(screen, row, left + 51, meter(attributes[key] - 4, maximum=6, width=12), accent("success"))
        elif key in COMPETENCIES:
            put(screen, row, left + 51, meter(competencies[key], maximum=5, width=12), accent("technique"))
    attribute_spent = sum(attributes.values()) - 6 * 6
    skill_spent = sum(competencies.values())
    put(screen, top + 19, left + 3, f"Attributes {attribute_spent}/{ATTRIBUTE_POINTS}    Starting competencies {skill_spent}/{COMPETENCY_POINTS}",
        accent("success" if attribute_spent == ATTRIBUTE_POINTS and skill_spent == COMPETENCY_POINTS else "warning"))
    put(screen, top + 20, left + 3, f"{ancestry}: {PEOPLE_EFFECTS[ancestry]}.")
    put(screen, top + 21, left + 3, message[:72] if message else f"Trait: {TRAITS[trait][1]}; origin grants one practical competency.",
        accent("warning") if message else accent("terrain"))
    put(screen, top + 22, left + 3, "J/K select  H/L adjust  Enter edit name  S begin  Q back", accent("ui_accent", curses.A_BOLD))
    screen.refresh()


def _read_name(screen: curses.window) -> str:
    top, left = _panel_origin(screen)
    put(screen, top + 21, left + 3, "New name (2-32 letters): " + " " * 44, accent("ui_accent"))
    screen.refresh()
    curses.echo()
    try:
        try:
            curses.curs_set(1)
        except curses.error:
            pass
        raw = screen.getstr(top + 21, left + 29, 32)
    finally:
        curses.noecho()
        try:
            curses.curs_set(0)
        except curses.error:
            pass
    return raw.decode("utf-8", errors="ignore")


def run_character_creation(screen: curses.window, state: GameState) -> bool:
    crew_index = 0
    name = state.household[0].name
    ancestry, origin, trait = ANCESTRIES[0], ORIGINS[0], next(iter(TRAITS))
    attributes, competencies = default_allocation(state.household[0].role)
    cursor = 0
    message = ""
    while True:
        _draw(screen, state, crew_index, name, ancestry, origin, trait, attributes, competencies, cursor, message)
        key = screen.getch()
        normalized = ord(chr(key).lower()) if 0 <= key < 256 else key
        message = ""
        if normalized in (ord("q"), 27):
            return False
        if normalized in (ord("j"), curses.KEY_DOWN):
            cursor = (cursor + 1) % len(FIELDS)
            continue
        if normalized in (ord("k"), curses.KEY_UP):
            cursor = (cursor - 1) % len(FIELDS)
            continue
        field = FIELDS[cursor]
        if normalized in (ord("s"),):
            try:
                apply_character_spec(state, crew_index=crew_index, name=name, ancestry=ancestry,
                                     origin=origin, trait=trait, attributes=attributes,
                                     competencies=competencies)
                return True
            except ValueError as exc:
                message = str(exc)
            continue
        if normalized in (10, 13, curses.KEY_ENTER) and field == "name":
            from .character import clean_name

            try:
                name = clean_name(_read_name(screen))
            except ValueError as exc:
                message = str(exc)
            continue
        if normalized not in (ord("h"), ord("l"), curses.KEY_LEFT, curses.KEY_RIGHT):
            continue
        step = -1 if normalized in (ord("h"), curses.KEY_LEFT) else 1
        if field == "crew":
            crew_index = (crew_index + step) % len(state.household)
            name = state.household[crew_index].name
            attributes, competencies = default_allocation(state.household[crew_index].role)
        elif field in {"ancestry", "origin", "trait"}:
            choices = ANCESTRIES if field == "ancestry" else ORIGINS if field == "origin" else tuple(TRAITS)
            current = ancestry if field == "ancestry" else origin if field == "origin" else trait
            selected = choices[(choices.index(current) + step) % len(choices)]
            if field == "ancestry":
                ancestry = selected
            elif field == "origin":
                origin = selected
            else:
                trait = selected
        elif field in ATTRIBUTES:
            proposed = attributes[field] + step
            spent = sum(attributes.values()) - 6 * 6 + step
            if 4 <= proposed <= 10 and 0 <= spent <= ATTRIBUTE_POINTS:
                attributes[field] = proposed
            else:
                message = "This attribute is at its bound, or no points remain to allocate."
        elif field in COMPETENCIES:
            proposed = competencies[field] + step
            spent = sum(competencies.values()) + step
            if 0 <= proposed <= 5 and 0 <= spent <= COMPETENCY_POINTS:
                competencies[field] = proposed
            else:
                message = "This competency is at its bound, or no points remain to allocate."
