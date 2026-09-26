"""Small action-clock calendar and bounded seasonal consequences."""

from __future__ import annotations

from dataclasses import dataclass

from .state import GameState, stage_rng

ACTIONS_PER_DAY = 36
DAYS_PER_SEASON = 24
SEASONS = ("spring", "summer", "autumn", "winter")


@dataclass(frozen=True)
class CalendarDate:
    year: int
    season: str
    day: int
    time_of_day: str
    observance: str | None

    @property
    def label(self) -> str:
        text = f"Year {self.year}, {self.season} day {self.day}, {self.time_of_day}"
        return f"{text} — {self.observance}" if self.observance else text


def initial_origin_day(seed: str) -> int:
    """Start early in spring while retaining deterministic world variation."""
    return stage_rng(seed, "calendar-origin").randrange(0, 6)


def calendar_at(state: GameState, world_time: int | None = None) -> CalendarDate:
    moment = state.world_time if world_time is None else world_time
    absolute_day = state.calendar_origin_day + moment // ACTIONS_PER_DAY
    year = absolute_day // (DAYS_PER_SEASON * len(SEASONS)) + 1
    within_year = absolute_day % (DAYS_PER_SEASON * len(SEASONS))
    season_index, day_index = divmod(within_year, DAYS_PER_SEASON)
    action = moment % ACTIONS_PER_DAY
    if action < 5:
        time_of_day = "dawn"
    elif action < 14:
        time_of_day = "morning"
    elif action < 24:
        time_of_day = "afternoon"
    elif action < 30:
        time_of_day = "evening"
    else:
        time_of_day = "night"
    season, day = SEASONS[season_index], day_index + 1
    observance = None
    if day == 1 and season in {"spring", "autumn"}:
        observance = f"{season} equinox"
    elif day == DAYS_PER_SEASON // 2 and season in {"summer", "winter"}:
        observance = f"{season} solstice"
    return CalendarDate(year, season, day, time_of_day, observance)


def record_calendar_crossings(state: GameState, previous_time: int) -> list[str]:
    """Record crossed day boundaries; menu and wall-clock time never call this."""
    if state.world_time <= previous_time:
        return []
    events: list[str] = []
    first_day = previous_time // ACTIONS_PER_DAY
    last_day = state.world_time // ACTIONS_PER_DAY
    for day_boundary in range(first_day + 1, last_day + 1):
        date = calendar_at(state, day_boundary * ACTIONS_PER_DAY)
        if date.day == 1:
            events.append(f"{date.season.title()} begins in year {date.year}.")
        if date.observance:
            events.append(f"Jomon records the {date.observance}.")
    for event in events:
        if not state.calendar_events or state.calendar_events[-1] != event:
            state.calendar_events.append(event)
            del state.calendar_events[:-16]
            state.chronicle.append(event)
            del state.chronicle[:-30]
            state.add_message(event, priority=3)
    return events


def daylight_modifier(state: GameState) -> int:
    date = calendar_at(state)
    if date.time_of_day == "night":
        return -5 if date.season == "winter" else -4
    if date.time_of_day in {"dawn", "evening"}:
        return -2
    if date.season == "summer":
        return 1
    return 0


def seasonal_route_note(state: GameState) -> str:
    season = calendar_at(state).season
    return {
        "spring": "high rivers and soft banks",
        "summer": "long light and exposed shoals",
        "autumn": "crosswinds and crowded markets",
        "winter": "short light, ice, and cold rigging",
    }[season]


def seasonal_stock_modifier(state: GameState, drink_id: str) -> int:
    season = calendar_at(state).season
    favored = {
        "spring": {"reed-tonic", "smokeleaf-infusion"},
        "summer": {"hearth-ale", "stillroom-cordial"},
        "autumn": {"willow-bitter", "miller-small-beer"},
        "winter": {"winter-juniper", "ebbglass-measure"},
    }[season]
    return 1 if drink_id in favored else 0
