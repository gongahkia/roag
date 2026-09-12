"""Jomon's three decks, dedicated tavern, schedules, drinks, and incidents."""

from __future__ import annotations

from collections import deque
from dataclasses import dataclass

from .calendar import calendar_at, initial_origin_day, seasonal_stock_modifier
from .state import ActorSchedule, GameState, Person, Position, SocialIncident, TerrainStatus, stage_rng

VESSEL_WIDTH = 64
VESSEL_HEIGHT = 22
TAVERN_WIDTH = 64
TAVERN_HEIGHT = 24
JOMON_GANGPLANK = Position(63, 10, 0)
TAVERN_ENTRANCE = Position(24, 7, 0)
TAVERN_EXIT = Position(0, 12, 0)
TABLE_PLAYER_SEAT = Position(31, 14, 0)
TABLE_PATRON_SEATS = (
    *(Position(x, 8, 0) for x in (25, 28, 31, 34, 37)),
    *(Position(x, 14, 0) for x in (25, 28, 34, 37)),
    Position(23, 10, 0), Position(23, 12, 0),
    Position(39, 10, 0), Position(39, 12, 0),
)
TABLE_SURFACE = frozenset(Position(x, y, 0) for y in range(9, 14) for x in range(24, 39))
DRAW_PLAYER_SEAT = Position(15, 17, 0)
DRAW_NPC_SEATS = (
    Position(15, 11, 0), Position(8, 14, 0), Position(22, 14, 0),
    Position(11, 11, 0), Position(19, 11, 0), Position(8, 16, 0), Position(22, 16, 0),
)
DRAW_SURFACE = frozenset(Position(x, y, 0) for y in range(12, 17) for x in range(9, 22))
DICE_PLAYER_SEAT = Position(50, 18, 0)
DICE_NPC_SEATS = (
    Position(50, 12, 0), Position(44, 15, 0), Position(56, 15, 0),
    Position(46, 12, 0), Position(54, 12, 0), Position(44, 17, 0), Position(56, 17, 0),
)
DICE_SURFACE = frozenset(Position(x, y, 0) for y in range(13, 18) for x in range(45, 56))
LOWER_HATCH = Position(14, 10, -1)
MAIN_LOWER_HATCH = Position(14, 10, 0)
MAIN_UPPER_STAIR = Position(48, 10, 0)
UPPER_STAIR = Position(48, 10, 1)


def _blank(width: int, height: int) -> list[list[str]]:
    grid = [["." for _ in range(width)] for _ in range(height)]
    for x in range(width):
        grid[0][x] = grid[-1][x] = "#"
    for y in range(height):
        grid[y][0] = grid[y][-1] = "#"
    return grid


def _walls(grid: list[list[str]], x1: int, y1: int, x2: int, y2: int) -> None:
    for x in range(x1, x2 + 1):
        grid[y1][x] = grid[y2][x] = "#"
    for y in range(y1, y2 + 1):
        grid[y][x1] = grid[y][x2] = "#"


def _vessel_levels() -> dict[int, tuple[str, ...]]:
    lower = _blank(VESSEL_WIDTH, VESSEL_HEIGHT)
    _walls(lower, 3, 3, 20, 8)
    _walls(lower, 24, 3, 43, 8)
    _walls(lower, 47, 3, 60, 8)
    _walls(lower, 3, 13, 25, 18)
    _walls(lower, 29, 13, 60, 18)
    for point, glyph in {
        (7, 5): "H", (17, 5): "L", (28, 5): "p", (39, 5): "W",
        (53, 5): "b", (57, 5): "b", (8, 15): "U", (20, 15): "R",
        (36, 15): "H", (52, 15): "S", (14, 10): ">",
    }.items():
        lower[point[1]][point[0]] = glyph
    lower[8][11] = lower[8][33] = lower[13][15] = lower[13][44] = "+"

    main = _blank(VESSEL_WIDTH, VESSEL_HEIGHT)
    _walls(main, 4, 3, 19, 8)
    _walls(main, 23, 3, 39, 8)
    _walls(main, 43, 3, 59, 8)
    _walls(main, 4, 13, 22, 18)
    _walls(main, 27, 13, 43, 18)
    _walls(main, 48, 13, 59, 18)
    for point, glyph in {
        (8, 5): "G", (16, 5): "b", (24, 7): "C", (31, 5): "K",
        (50, 5): "R", (56, 5): "T", (8, 15): "H", (18, 15): "L",
        (34, 15): "b", (39, 15): "b", (54, 15): "s", (14, 10): "<",
        (48, 10): ">", (63, 10): "+",
    }.items():
        main[point[1]][point[0]] = glyph
    main[8][12] = main[8][31] = main[8][51] = main[13][13] = main[13][35] = main[13][54] = "+"

    upper = _blank(VESSEL_WIDTH, VESSEL_HEIGHT)
    for x in range(2, VESSEL_WIDTH - 2):
        if x % 3:
            upper[4][x] = "="
            upper[17][x] = "="
    _walls(upper, 22, 7, 41, 14)
    for point, glyph in {
        (7, 10): "O", (15, 10): "S", (28, 10): "P", (35, 10): "N",
        (48, 10): "<", (55, 10): "R", (60, 10): "O",
    }.items():
        upper[point[1]][point[0]] = glyph
    upper[10][22] = upper[10][41] = "+"
    return {
        -1: tuple("".join(row) for row in lower),
        0: tuple("".join(row) for row in main),
        1: tuple("".join(row) for row in upper),
    }


def _tavern_map() -> tuple[str, ...]:
    grid = _blank(TAVERN_WIDTH, TAVERN_HEIGHT)
    grid[12][0] = "+"
    _walls(grid, 44, 2, 61, 7)
    grid[7][50] = "+"
    for x in range(45, 61):
        grid[9][x] = "="
    for point, glyph in {
        (7, 5): "F", (8, 5): "f", (12, 17): "S", (53, 5): "k",
        (47, 9): "_", (51, 9): "_", (55, 9): "_", (59, 9): "_",
    }.items():
        grid[point[1]][point[0]] = glyph
    for cx, cy in ((16, 6),):
        grid[cy][cx] = "t"
        for dx, dy in ((-2, 0), (2, 0), (0, -2), (0, 2)):
            grid[cy + dy][cx + dx] = "_"
    for point in TABLE_SURFACE:
        grid[point.y][point.x] = "=" if point.x in {24, 38} or point.y in {9, 13} else "t"
    for point in DRAW_SURFACE:
        grid[point.y][point.x] = "=" if point.x in {9, 21} or point.y in {12, 16} else "t"
    for point in DRAW_NPC_SEATS:
        grid[point.y][point.x] = "_"
    grid[DRAW_PLAYER_SEAT.y][DRAW_PLAYER_SEAT.x] = "P"
    for point in DICE_SURFACE:
        grid[point.y][point.x] = "=" if point.x in {45, 55} or point.y in {13, 17} else "t"
    for point in DICE_NPC_SEATS:
        grid[point.y][point.x] = "_"
    grid[DICE_PLAYER_SEAT.y][DICE_PLAYER_SEAT.x] = "Q"
    for x, glyph in ((27, "F"), (31, "d"), (35, "f")):
        grid[11][x] = glyph
    for point in TABLE_PATRON_SEATS:
        grid[point.y][point.x] = "_"
    grid[TABLE_PLAYER_SEAT.y][TABLE_PLAYER_SEAT.x] = "D"
    return tuple("".join(row) for row in grid)


VESSEL_LEVELS = _vessel_levels()
TAVERN_MAP = _tavern_map()


@dataclass(frozen=True)
class Drink:
    id: str
    name: str
    benefit: str
    drawback: str
    duration: int
    cost: int
    incompatible: tuple[str, ...] = ()
    rare: bool = False


DRINKS: dict[str, Drink] = {
    "hearth-ale": Drink("hearth-ale", "Hearth Ale", "guard resists fear", "steps make one more noise", 18, 1),
    "winter-juniper": Drink("winter-juniper", "Winter Juniper", "cold and chilling are resisted", "fine aim is shortened", 20, 2),
    "willow-bitter": Drink("willow-bitter", "Willow Bitter", "injury pain no longer slows tools", "fatigue follows when it clears", 14, 2),
    "miller-small-beer": Drink("miller-small-beer", "Miller's Small Beer", "guard persists through one move", "movement while guarded is slower", 16, 1),
    "stillroom-cordial": Drink("stillroom-cordial", "Stillroom Cordial", "material negotiation gains leverage", "visible intoxication harms wary contacts", 12, 2, ("hearth-ale",)),
    "smokeleaf-infusion": Drink("smokeleaf-infusion", "Smokeleaf Infusion", "smoke inhalation is resisted", "thirst makes wet crossings louder", 18, 2),
    "reed-tonic": Drink("reed-tonic", "Reed Tonic", "treatment and bog recovery are faster", "awareness is dulled", 14, 2),
    "ebbglass-measure": Drink("ebbglass-measure", "Ebbglass Measure", "one regional process warning lasts longer", "the next remembered voice may be false", 10, 4, (), True),
}


HOUSEHOLD_SEATS = (
    Position(14, 6), Position(18, 6), Position(16, 4), Position(16, 8),
    Position(11, 11), Position(19, 11), Position(8, 14), Position(22, 14),
    Position(42, 16),
)
VISITOR_SEATS = (
    Position(42, 18), Position(44, 14), Position(44, 18),
    Position(47, 9), Position(51, 9), Position(55, 9),
)
BARTENDER_POSITION = Position(53, 8)
MERCHANT_POSITION = Position(53, 15, 0)

# Scheduled work happens beside physical controls so an actor can be spoken to
# without making the chart, helm, stores, or repair point unusable.
SCHEDULE_WORK_POSITIONS: dict[tuple[str, Position], Position] = {
    ("vessel:-1", Position(20, 15, -1)): Position(19, 15, -1),
    ("vessel:0", Position(8, 15, 0)): Position(7, 15, 0),
    ("vessel:0", Position(34, 15, 0)): Position(33, 15, 0),
    ("vessel:0", Position(50, 5, 0)): Position(49, 5, 0),
    ("vessel:1", Position(28, 10, 1)): Position(27, 10, 1),
    ("vessel:1", Position(35, 10, 1)): Position(34, 10, 1),
    ("vessel:1", Position(60, 10, 1)): Position(59, 10, 1),
}


def vessel_rows(state: GameState, z: int | None = None) -> tuple[str, ...]:
    if state.jomon_space == "tavern":
        return TAVERN_MAP
    level = state.position.z if z is None else z
    original = VESSEL_LEVELS[level]
    if not state.vessel_tiles:
        return original
    rows = list(original)
    for key, glyph in state.vessel_tiles.items():
        x, y, changed_z = map(int, key.split(","))
        if changed_z == level:
            rows[y] = rows[y][:x] + glyph + rows[y][x + 1:]
    return tuple(rows)


def vessel_tile(state: GameState, position: Position) -> str:
    rows = TAVERN_MAP if state.jomon_space == "tavern" else VESSEL_LEVELS.get(position.z, ())
    if not 0 <= position.y < len(rows) or not 0 <= position.x < len(rows[position.y]):
        return " "
    return state.vessel_tiles.get(f"{position.x},{position.y},{position.z}", rows[position.y][position.x]) if state.jomon_space == "vessel" else rows[position.y][position.x]


def vessel_vertical_destination(position: Position) -> Position | None:
    if position == LOWER_HATCH:
        return MAIN_LOWER_HATCH
    if position == MAIN_LOWER_HATCH:
        return LOWER_HATCH
    if position == MAIN_UPPER_STAIR:
        return UPPER_STAIR
    if position == UPPER_STAIR:
        return MAIN_UPPER_STAIR
    return None


def refresh_bartender_stock(state: GameState) -> None:
    date = calendar_at(state)
    stock = {}
    for drink_id, drink in DRINKS.items():
        roll = stage_rng(state.seed, f"bar:{date.year}:{date.season}:{drink_id}").randrange(3)
        quantity = (0 if drink.rare and roll else 1) + seasonal_stock_modifier(state, drink_id)
        stock[drink_id] = min(3, quantity)
    state.bartender_stock = stock


def _schedule_position(state: GameState, actor_id: str, activity: str) -> tuple[str, Position]:
    index = sum(ord(char) for char in actor_id)
    if activity == "playing Dullest Dungeon":
        people = [*_all_named_people(state), state.bartender]
        seat_index = next(i for i, person in enumerate(people) if person.id == actor_id)
        return "tavern", TABLE_PATRON_SEATS[seat_index]
    if activity == "playing Tavern Draw":
        people = [*_all_named_people(state), state.bartender]
        seat_index = next(i for i, person in enumerate(people) if person.id == actor_id)
        return "tavern", DRAW_NPC_SEATS[seat_index % len(DRAW_NPC_SEATS)]
    if activity == "playing Quay Bones":
        people = [*_all_named_people(state), state.bartender]
        seat_index = next(i for i, person in enumerate(people) if person.id == actor_id)
        return "tavern", DICE_NPC_SEATS[seat_index % len(DICE_NPC_SEATS)]
    if activity in {"eating", "drinking", "socialising", "waiting"}:
        seats = HOUSEHOLD_SEATS + VISITOR_SEATS
        return "tavern", seats[index % len(seats)]
    if activity == "sleeping":
        return "vessel:-1", Position(52 + index % 7, 6, -1)
    if activity in {"standing watch", "steering", "consulting chart"}:
        return "vessel:1", {
            "standing watch": Position(59, 10, 1),
            "steering": Position(34, 10, 1),
            "consulting chart": Position(27, 10, 1),
        }[activity]
    if activity in {"repairing", "moving cargo", "treating injuries"}:
        return "vessel:0", {
            "repairing": Position(49, 5, 0),
            "moving cargo": Position(7, 15, 0),
            "treating injuries": Position(33, 15, 0),
        }[activity]
    return "tavern", HOUSEHOLD_SEATS[index % len(HOUSEHOLD_SEATS)]


def _activity_for(state: GameState, person: Person, boundary: int) -> str:
    date = calendar_at(state, boundary)
    if person.injury != "none" and date.time_of_day in {"night", "morning"}:
        return "resting"
    if date.time_of_day == "night":
        return "sleeping"
    if person in state.visitors and state.visitor_status.get(person.id) in {"visiting", "deferred"}:
        roll = sum(map(ord, person.id)) % 5
        return "playing Quay Bones" if roll == 0 else "playing Tavern Draw" if roll == 1 else "playing Dullest Dungeon"
    if date.time_of_day == "morning":
        return "eating"
    if date.time_of_day == "evening":
        return ("playing Quay Bones" if sum(map(ord, person.id)) % 5 == 0 else
                "playing Tavern Draw" if sum(map(ord, person.id)) % 3 == 0 else
                "playing Dullest Dungeon" if sum(map(ord, person.id)) % 2 else "socialising")
    by_role = {
        "pilot": "steering", "bargemaster": "consulting chart",
        "carpenter": "repairing", "guard": "standing watch",
        "healer": "treating injuries", "factor": "moving cargo",
    }
    return by_role.get(person.role, "training")


def _all_named_people(state: GameState) -> list[Person]:
    return [*state.household, *state.visitors]


def _contact_destination(state: GameState, actor_id: str, working: bool) -> tuple[str, Position] | None:
    for region_id, contacts in state.contacts.items():
        contact = next((contact for contact in contacts if contact.id == actor_id), None)
        if contact is None:
            continue
        work = contact.position or state.regions[region_id].landmarks["contact"]
        if working:
            return f"region:{region_id}", work
        rows = state.regions[region_id].levels[str(work.z)]
        for dx, dy in ((2, 0), (-2, 0), (0, 2), (0, -2), (1, 1), (-1, -1)):
            point = Position(work.x + dx, work.y + dy, work.z)
            if 0 <= point.y < len(rows) and 0 <= point.x < len(rows[point.y]) and rows[point.y][point.x] not in {" ", "#", "~", "T"}:
                return f"region:{region_id}", point
        return f"region:{region_id}", work
    return None


def initialise_living_vessel(state: GameState, *, migrated: bool = False) -> None:
    if state.calendar_origin_day == 0:
        state.calendar_origin_day = initial_origin_day(state.seed)
    if migrated and state.location == "jomon":
        state.jomon_space = "vessel"
    if state.location == "jomon" and state.position.z not in VESSEL_LEVELS:
        state.position = Position(4, 10, 0)
    refresh_bartender_stock(state)
    schedules: dict[str, ActorSchedule] = {}
    boundary = state.world_time + 6
    occupied: set[tuple[str, Position]] = set()
    tavern_positions: dict[str, Position] = {}
    for index, person in enumerate(_all_named_people(state)):
        status = state.visitor_status.get(person.id, "joined" if person in state.household else "away")
        if person.id == state.active_courier_id and state.location == "jomon":
            area = "tavern" if state.jomon_space == "tavern" else f"vessel:{state.position.z}"
            point = state.position
            activity = "ready for departure"
        elif person not in state.household and status == "away":
            area = f"region:{person.home_region}"
            region = state.regions.get(person.home_region)
            point = region.landmarks["contact"] if region else Position(1, 1)
            activity = "working at a regional site"
        else:
            activity = "playing Quay Bones" if index == 3 else "playing Tavern Draw" if index in {2, 4} else "playing Dullest Dungeon"
            area, point = _schedule_position(state, person.id, activity)
            point = _nearest_free(area, point, {used for place, used in occupied if place == area})
        occupied.add((area, point))
        schedules[person.id] = ActorSchedule(
            person.id, area, point, activity, boundary, area, point,
            available=person.available and person.alive, last_update=state.world_time,
        )
        if area == "tavern" and person.id != state.active_courier_id:
            tavern_positions[person.id] = point
    schedules[state.bartender.id] = ActorSchedule(
        state.bartender.id, "tavern", BARTENDER_POSITION, "serving",
        boundary, "tavern", BARTENDER_POSITION, last_update=state.world_time,
    )
    schedules[state.merchant.id] = ActorSchedule(
        state.merchant.id, "vessel:0", MERCHANT_POSITION,
        "trading from a counted berth" if state.merchant_present else "away on a regional circuit",
        boundary, "vessel:0", MERCHANT_POSITION,
        available=state.merchant_present, last_update=state.world_time,
    )
    for region_id, contacts in state.contacts.items():
        for contact in contacts:
            point = contact.position or state.regions[region_id].landmarks["contact"]
            schedules[contact.id] = ActorSchedule(
                contact.id, f"region:{region_id}", point, "working",
                boundary, f"region:{region_id}", point,
                disposition=contact.disposition, last_update=state.world_time,
            )
    state.actor_schedules = schedules
    state.tavern_positions = tavern_positions
    state.last_schedule_turn = state.world_time


def normalise_schedule_work_positions(state: GameState) -> None:
    """Move saved workers off controls and newly installed furniture without time."""
    occupied = {
        (schedule.area, schedule.position)
        for schedule in state.actor_schedules.values()
        if schedule.area.startswith(("vessel:", "tavern"))
    }
    for schedule in sorted(state.actor_schedules.values(), key=lambda item: item.actor_id):
        replacement = SCHEDULE_WORK_POSITIONS.get((schedule.area, schedule.position))
        if schedule.area == "tavern" and schedule.position in TABLE_SURFACE | DRAW_SURFACE | DICE_SURFACE:
            used_chairs = {point for area, point in occupied if area == "tavern" and point != schedule.position}
            chairs = (TABLE_PATRON_SEATS if schedule.position in TABLE_SURFACE else
                      DRAW_NPC_SEATS if schedule.position in DRAW_SURFACE else DICE_NPC_SEATS)
            fallback = (TABLE_PLAYER_SEAT if schedule.position in TABLE_SURFACE else
                        DRAW_PLAYER_SEAT if schedule.position in DRAW_SURFACE else DICE_PLAYER_SEAT)
            replacement = next((point for point in chairs if point not in used_chairs), fallback)
        if replacement is not None or not _walkable(schedule.area, schedule.position):
            occupied.discard((schedule.area, schedule.position))
            area_occupied = {point for area, point in occupied if area == schedule.area}
            schedule.position = _nearest_free(
                schedule.area, replacement or schedule.position, area_occupied
            )
            occupied.add((schedule.area, schedule.position))
        schedule.destination = SCHEDULE_WORK_POSITIONS.get(
            (schedule.destination_area, schedule.destination), schedule.destination
        )
        if not _walkable(schedule.destination_area, schedule.destination):
            schedule.destination = schedule.position
    if state.location == "jomon" and state.jomon_space == "tavern" and state.position in TABLE_SURFACE | DRAW_SURFACE | DICE_SURFACE:
        occupied_here = {
            schedule.position for schedule in state.actor_schedules.values()
            if schedule.area == "tavern" and schedule.actor_id != state.active_courier_id
        }
        seat = (TABLE_PLAYER_SEAT if state.position in TABLE_SURFACE else
                DRAW_PLAYER_SEAT if state.position in DRAW_SURFACE else DICE_PLAYER_SEAT)
        state.position = _nearest_free("tavern", seat, occupied_here)
    state.tavern_positions = {
        actor_id: schedule.position for actor_id, schedule in state.actor_schedules.items()
        if schedule.area == "tavern" and actor_id != state.active_courier_id
        and any(person.id == actor_id for person in _all_named_people(state))
    }


def current_area(state: GameState) -> str:
    if state.location == "region":
        return f"region:{state.active_region_id}"
    if state.jomon_space == "tavern":
        return "tavern"
    return f"vessel:{state.position.z}"


def _walkable(area: str, point: Position) -> bool:
    if area == "tavern":
        rows = TAVERN_MAP
    elif area.startswith("vessel:"):
        rows = VESSEL_LEVELS[int(area.split(":", 1)[1])]
    else:
        return True
    return (
        0 <= point.y < len(rows)
        and 0 <= point.x < len(rows[point.y])
        and rows[point.y][point.x] not in {"#", "=", "t", "F", "f"}
        and (area != "tavern" or point not in TABLE_SURFACE | DRAW_SURFACE | DICE_SURFACE)
    )


def seat_patron_at_table(state: GameState, patron_id: str) -> Position:
    """Use the accepted game's Jomon action to bring its named patron to a chair."""
    schedule = state.actor_schedules[patron_id]
    occupied = {
        item.position for item in state.actor_schedules.values()
        if item.area == "tavern" and item.actor_id != patron_id
    }
    occupied.add(state.position)
    people = [*_all_named_people(state), state.bartender]
    preferred = TABLE_PATRON_SEATS[next(i for i, person in enumerate(people) if person.id == patron_id)]
    chair = next((point for point in (preferred, *TABLE_PATRON_SEATS) if point not in occupied), None)
    if chair is None:
        raise ValueError("the gaming table has no free patron chair")
    schedule.area = schedule.destination_area = "tavern"
    schedule.position = schedule.destination = chair
    schedule.activity = "playing Dullest Dungeon"
    schedule.next_boundary = state.world_time + 6
    if any(person.id == patron_id for person in _all_named_people(state)):
        state.tavern_positions[patron_id] = chair
    return chair


def seat_draw_players(state: GameState, opponent_ids: list[str]) -> None:
    """Move the three invited adults into free chairs around the draw table."""
    occupied = {
        schedule.position for actor_id, schedule in state.actor_schedules.items()
        if schedule.area == "tavern" and actor_id not in opponent_ids
    }
    occupied.add(state.position)
    for identity in opponent_ids:
        chair = next((point for point in DRAW_NPC_SEATS if point not in occupied), None)
        if chair is None:
            raise ValueError("the draw table has no three free chairs")
        schedule = state.actor_schedules[identity]
        schedule.area = schedule.destination_area = "tavern"
        schedule.position = schedule.destination = chair
        schedule.activity = "playing Tavern Draw"
        schedule.next_boundary = state.world_time + 6
        if any(person.id == identity for person in _all_named_people(state)):
            state.tavern_positions[identity] = chair
        occupied.add(chair)


def seat_dice_players(state: GameState, opponent_ids: list[str]) -> None:
    """Bring the invited adults to the visible bones-table chairs."""
    occupied = {
        schedule.position for actor_id, schedule in state.actor_schedules.items()
        if schedule.area == "tavern" and actor_id not in opponent_ids
    }
    occupied.add(state.position)
    for identity in opponent_ids:
        chair = next((point for point in DICE_NPC_SEATS if point not in occupied), None)
        if chair is None:
            raise ValueError("the bones table has no three free chairs")
        schedule = state.actor_schedules[identity]
        schedule.area = schedule.destination_area = "tavern"
        schedule.position = schedule.destination = chair
        schedule.activity = "playing Quay Bones"
        schedule.next_boundary = state.world_time + 6
        if any(person.id == identity for person in _all_named_people(state)):
            state.tavern_positions[identity] = chair
        occupied.add(chair)


def _step_toward(area: str, start: Position, goal: Position, occupied: set[Position]) -> Position:
    if start == goal:
        return start
    queue, previous = deque([start]), {start: None}
    while queue:
        current = queue.popleft()
        if current == goal:
            break
        for dx, dy in ((0, -1), (-1, 0), (1, 0), (0, 1)):
            candidate = Position(current.x + dx, current.y + dy, current.z)
            if candidate not in previous and _walkable(area, candidate) and (candidate not in occupied or candidate == goal):
                previous[candidate] = current
                queue.append(candidate)
    if goal not in previous:
        return start
    cursor = goal
    while previous[cursor] not in {None, start}:
        cursor = previous[cursor]  # type: ignore[assignment]
    return cursor


def _nearest_free(area: str, preferred: Position, occupied: set[Position]) -> Position:
    """Resolve a coarse off-screen arrival without stacking named actors."""
    if preferred not in occupied and _walkable(area, preferred):
        return preferred
    queue, seen = deque([preferred]), {preferred}
    while queue:
        current = queue.popleft()
        for dx, dy in ((0, -1), (-1, 0), (1, 0), (0, 1)):
            candidate = Position(current.x + dx, current.y + dy, preferred.z)
            if candidate in seen:
                continue
            if _walkable(area, candidate) and candidate not in occupied:
                return candidate
            seen.add(candidate)
            if _walkable(area, candidate):
                queue.append(candidate)
    return preferred


def _advance_visible_transfer(schedule: ActorSchedule, occupied: set[Position]) -> None:
    """Move one rendered step toward a deck or tavern boundary."""
    area = schedule.area
    if area == "tavern":
        step = _step_toward(area, schedule.position, TAVERN_EXIT, occupied)
        schedule.position = step
        if step == TAVERN_EXIT:
            schedule.area = "vessel:0"
            schedule.position = Position(TAVERN_ENTRANCE.x - 1, TAVERN_ENTRANCE.y, 0)
        return
    if not area.startswith("vessel:"):
        return
    z = int(area.split(":", 1)[1])
    destination_area = schedule.destination_area
    target_z = 0 if destination_area == "tavern" else int(destination_area.split(":", 1)[1])
    if z == target_z:
        if destination_area == "tavern":
            step = _step_toward(area, schedule.position, TAVERN_ENTRANCE, occupied)
            schedule.position = step
            if step == TAVERN_ENTRANCE:
                schedule.area = "tavern"
                schedule.position = Position(TAVERN_EXIT.x + 1, TAVERN_EXIT.y, 0)
        return
    if z < target_z:
        hatch = LOWER_HATCH if z == -1 else MAIN_UPPER_STAIR
        arrival = MAIN_LOWER_HATCH if z == -1 else UPPER_STAIR
    else:
        hatch = UPPER_STAIR if z == 1 else MAIN_LOWER_HATCH
        arrival = MAIN_UPPER_STAIR if z == 1 else LOWER_HATCH
    step = _step_toward(area, schedule.position, hatch, occupied)
    schedule.position = step
    if step == hatch:
        schedule.area = f"vessel:{arrival.z}"
        schedule.position = arrival


def _social_incident(state: GameState, boundary: int) -> None:
    if state.pending_incident or len(state.household) < 2:
        return
    rng = stage_rng(state.seed, f"social:{boundary}")
    if rng.randrange(5):
        return
    people = sorted((person for person in state.household if person.alive), key=lambda person: person.id)
    first = people[rng.randrange(len(people))]
    second = people[(people.index(first) + 1 + rng.randrange(len(people) - 1)) % len(people)]
    standing = first.relationships.get(second.id, 0)
    if first.injury != "none" and second.role == "healer":
        kind, cause = "assistance", f"{second.name} noticed {first.name}'s {first.injury} after the last expedition"
    elif standing < 0:
        kind, cause = "argument", f"their standing is {standing:+d} after remembered work disagreements"
    elif first.injury != "none" or second.injury != "none":
        kind, cause = "assistance", "one returned injured and the other was available"
    else:
        kind, cause = "shared meal", "their non-hostile standing and common watch aligned"
    incident = SocialIncident(
        f"incident-{boundary}", kind, [first.id, second.id], cause,
        "pending" if current_area(state) == "tavern" and kind == "argument" else "resolved",
        boundary,
    )
    if incident.status == "pending":
        state.pending_incident = incident
        state.add_message(f"{first.name} and {second.name} argue: {cause}. You may intervene.", priority=3)
        return
    delta = 1 if kind in {"assistance", "shared meal"} else -1
    first.relationships[second.id] = max(-3, min(3, standing + delta))
    second.relationships[first.id] = max(-3, min(3, second.relationships.get(first.id, 0) + delta))
    text = f"{first.name} and {second.name}: {kind}, because {cause}."
    first.memories.append(text)
    second.memories.append(text)
    state.chronicle.append(text)
    del state.chronicle[:-30]


def resolve_social_incident(state: GameState, response: str) -> tuple[bool, str]:
    incident = state.pending_incident
    if incident is None or incident.status != "pending":
        return False, "No unresolved incident needs intervention."
    people = {person.id: person for person in state.household}
    first, second = (people[actor_id] for actor_id in incident.participants)
    if response == "mediate":
        mediator = state.courier
        delta = 2 if mediator and mediator.speech >= 10 else 1
        text = f"You name the disputed work; {first.name} and {second.name} stand down."
        if mediator:
            mediator.speech = min(20, mediator.speech + 1)
    elif response == "side-first":
        delta, text = -1, f"You support {first.name}; {second.name} leaves the table angry."
    elif response == "let-fight":
        delta, text = -1, "The argument becomes a bounded fistfight; both stop before grave harm."
        for person in (first, second):
            person.health = max(2, person.health - 1)
            person.injury = "bruised torso"
            person.injuries["torso"] = "bruised torso"
    else:
        return False, "That response does not address the incident."
    first.relationships[second.id] = max(-3, min(3, first.relationships.get(second.id, 0) + delta))
    second.relationships[first.id] = max(-3, min(3, second.relationships.get(first.id, 0) + delta))
    incident.status = "resolved"
    state.pending_incident = None
    record = f"{incident.kind.title()} resolved: {text} Cause: {incident.cause}."
    first.memories.append(record)
    second.memories.append(record)
    state.chronicle.append(record)
    del state.chronicle[:-30]
    return True, text


def advance_living_world(state: GameState) -> None:
    """Advance schedules once after a recorded action-clock tick."""
    for effect_id in list(state.drink_effects):
        effect = state.drink_effects[effect_id]
        effect.remaining -= 1
        if effect.remaining <= 0:
            del state.drink_effects[effect_id]
            state.add_message(f"{DRINKS[effect_id].name} clears: {DRINKS[effect_id].drawback} ends.", priority=2)
            if effect_id == "willow-bitter":
                state.terrain_statuses["fatigued"] = TerrainStatus("spent willow bitter", 6, "guard and climbing are slower")
    if not state.actor_schedules:
        return
    visible_area = current_area(state)
    occupied = {
        schedule.position for schedule in state.actor_schedules.values()
        if schedule.area == visible_area
    }
    all_vessel_occupied = {
        (schedule.area, schedule.position)
        for schedule in state.actor_schedules.values()
        if schedule.area.startswith(("vessel:", "tavern"))
    }
    crossed_boundary = False
    draw_hand = state.tavern_draw.get("active_hand")
    dice_match = state.tavern_dice.get("active_match")
    for schedule in sorted(state.actor_schedules.values(), key=lambda item: item.actor_id):
        all_vessel_occupied.discard((schedule.area, schedule.position))
        if (schedule.area == "tavern"
                and (draw_hand and draw_hand["phase"] != "complete" and schedule.actor_id in draw_hand["players"][1:]
                     or dice_match and dice_match["phase"] != "complete" and schedule.actor_id in dice_match["players"][1:])):
            schedule.next_boundary = state.world_time + 6
            all_vessel_occupied.add((schedule.area, schedule.position))
            continue
        if state.voyage_status == "active":
            if schedule.actor_id == state.bartender.id:
                schedule.activity = "securing the tavern"
                schedule.destination_area, schedule.destination = "tavern", BARTENDER_POSITION
            elif any(person.id == schedule.actor_id for person in state.household):
                if state.voyage_kind == "raiders":
                    schedule.activity = "defending cargo"
                    schedule.destination_area, schedule.destination = "vessel:0", Position(54, 10, 0)
                elif state.voyage_kind == "creature":
                    schedule.activity = "bracing the hull"
                    schedule.destination_area, schedule.destination = "vessel:-1", Position(19, 15, -1)
                else:
                    schedule.activity = "answering named crew"
                    schedule.destination_area, schedule.destination = "tavern", HOUSEHOLD_SEATS[0]
        if state.voyage_status != "active" and state.world_time >= schedule.next_boundary:
            crossed_boundary = True
            if schedule.actor_id == state.bartender.id:
                date = calendar_at(state)
                activity = "serving" if date.time_of_day not in {"night", "dawn"} else "sleeping"
                area, destination = ("tavern", BARTENDER_POSITION) if activity == "serving" else ("vessel:-1", Position(58, 5, -1))
            elif schedule.actor_id == state.merchant.id:
                activity = "trading from a counted berth" if state.merchant_present else "away on a regional circuit"
                area, destination = "vessel:0", MERCHANT_POSITION
            else:
                person = next((person for person in _all_named_people(state) if person.id == schedule.actor_id), None)
                if person is None:
                    working = calendar_at(state).time_of_day in {"morning", "afternoon"}
                    contact_destination = _contact_destination(state, schedule.actor_id, working)
                    if contact_destination is None:
                        schedule.next_boundary += 6
                        continue
                    activity = "working at a regional site" if working else "resting near home"
                    area, destination = contact_destination
                else:
                    activity = _activity_for(state, person, state.world_time)
                    area, destination = _schedule_position(state, person.id, activity)
            schedule.activity = activity
            schedule.destination_area = area
            schedule.destination = destination
            schedule.next_boundary = state.world_time + 6
        if schedule.area == visible_area and schedule.destination_area == visible_area:
            occupied.discard(schedule.position)
            schedule.position = _step_toward(visible_area, schedule.position, schedule.destination, occupied)
            occupied.add(schedule.position)
        elif schedule.area == visible_area:
            occupied.discard(schedule.position)
            _advance_visible_transfer(schedule, occupied)
            occupied.add(schedule.position)
        elif schedule.area != visible_area:
            # Off-screen travel is a bounded causal transition, never a tile loop.
            schedule.area = schedule.destination_area
            area_occupied = {
                point for area, point in all_vessel_occupied if area == schedule.area
            }
            schedule.position = _nearest_free(
                schedule.area, schedule.destination, area_occupied
            )
        if (
            schedule.area.startswith(("vessel:", "tavern"))
            and (schedule.area, schedule.position) in all_vessel_occupied
        ):
            area_occupied = {
                point for area, point in all_vessel_occupied if area == schedule.area
            }
            schedule.position = _nearest_free(
                schedule.area, schedule.position, area_occupied
            )
        if schedule.area.startswith(("vessel:", "tavern")):
            all_vessel_occupied.add((schedule.area, schedule.position))
        schedule.last_update = state.world_time
    if crossed_boundary:
        if calendar_at(state).day in {1, 12}:
            refresh_bartender_stock(state)
        _social_incident(state, state.world_time)
    state.tavern_positions = {
        actor_id: schedule.position
        for actor_id, schedule in state.actor_schedules.items()
        if schedule.area == "tavern" and actor_id != state.bartender.id
        and actor_id != state.active_courier_id
        and any(person.id == actor_id for person in _all_named_people(state))
    }
    state.last_schedule_turn = state.world_time


def buy_drink(state: GameState, drink_id: str, *, bottle: bool) -> tuple[bool, str]:
    drink = DRINKS.get(drink_id)
    if drink is None or state.bartender_stock.get(drink_id, 0) <= 0:
        return False, "That drink is not in the current counted stock."
    if state.bartender.relationships.get(state.active_courier_id or "", 0) <= -3:
        return False, "Sena refuses further credit after the remembered dispute."
    if state.trade_credit < drink.cost:
        return False, f"{drink.name} requires {drink.cost} credit."
    for incompatible in drink.incompatible:
        if incompatible in state.drink_effects:
            return False, f"Sena will not mix {drink.name} with {DRINKS[incompatible].name}."
    if bottle:
        from .inventory import auto_place, create_item

        item = create_item(state, f"consumable:bottle:{drink_id}", f"bought from {state.bartender.name}")
        if not auto_place(state, item.id, "pack", owner_id=state.active_courier_id):
            state.items.remove(item)
            state.next_item_id -= 1
            return False, "The bottle remains at the bar because no valid pack placement exists."
        text = f"{drink.name} is corked into the courier's pack."
    else:
        state.drink_effects[drink_id] = TerrainStatus("served at Jomon's bar", drink.duration, f"{drink.benefit}; drawback: {drink.drawback}")
        text = f"You drink {drink.name}: {drink.benefit}; drawback: {drink.drawback}."
    state.trade_credit -= drink.cost
    state.bartender_stock[drink_id] -= 1
    state.bartender.memories.append(f"Served {drink.name} to {state.courier.name if state.courier else 'Jomon'}.")
    return True, text


def drink_bottled(state: GameState, drink_id: str) -> tuple[bool, str]:
    """Consume one physical bottle without charging the bar a second time."""
    drink = DRINKS.get(drink_id)
    if drink is None:
        return False, "The bottle has no known Jomon measure."
    for incompatible in drink.incompatible:
        if incompatible in state.drink_effects:
            return False, (
                f"{drink.name} cannot be safely mixed with "
                f"{DRINKS[incompatible].name}."
            )
    from .inventory import consume_carried

    if not consume_carried(state, f"consumable:bottle:{drink_id}"):
        return False, f"No physical bottle of {drink.name} is in the pack."
    state.drink_effects[drink_id] = TerrainStatus(
        "opened expedition bottle", drink.duration,
        f"{drink.benefit}; drawback: {drink.drawback}",
    )
    return True, (
        f"You uncork {drink.name}: {drink.benefit}; "
        f"drawback: {drink.drawback}."
    )


def validate_living_vessel(state: GameState) -> None:
    if set(VESSEL_LEVELS) != {-1, 0, 1} or any(len(rows) != VESSEL_HEIGHT or any(len(row) != VESSEL_WIDTH for row in rows) for rows in VESSEL_LEVELS.values()):
        raise ValueError("Jomon deck dimensions are invalid")
    if len(TAVERN_MAP) != TAVERN_HEIGHT or any(len(row) != TAVERN_WIDTH for row in TAVERN_MAP):
        raise ValueError("Jomon tavern dimensions are invalid")
    if set(state.bartender_stock) != set(DRINKS):
        raise ValueError("bartender stock is incomplete")
    named = {
        person.id
        for person in [*state.household, *state.visitors, state.bartender, state.merchant]
    }
    contact_ids = {contact.id for contacts in state.contacts.values() for contact in contacts}
    if set(state.actor_schedules) != named | contact_ids:
        raise ValueError("named actor schedules are incomplete")
    occupied: set[tuple[str, Position]] = set()
    for schedule in state.actor_schedules.values():
        key = schedule.area, schedule.position
        if schedule.area.startswith(("vessel:", "tavern")) and key in occupied:
            raise ValueError("scheduled actors share a vessel position")
        occupied.add(key)
    if state.pending_incident and state.pending_incident.status != "pending":
        raise ValueError("only an unresolved incident may remain pending")
