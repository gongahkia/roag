"""Deterministic game rules with no terminal dependencies."""

from __future__ import annotations

import random
from collections import deque
from dataclasses import asdict, dataclass, field
from typing import Any

from .content import Catalog


class RuleError(ValueError):
    """Raised when a command is not legal in the current state."""


@dataclass
class Actor:
    id: str
    name: str
    max_hp: int
    hp: int
    rank: int
    side: str
    hero_class: str | None = None
    definition_id: str | None = None
    stress: int = 0
    block: int = 0
    deaths_door: bool = False
    affliction: str | None = None
    statuses: dict[str, int] = field(default_factory=dict)
    guarded_by: str | None = None
    guard_turns: int = 0

    @property
    def alive(self) -> bool:
        return self.hp > 0 or (self.side == "hero" and self.deaths_door)


@dataclass
class CardInstance:
    card_id: str
    upgraded: bool = False
    bound_hero_id: str | None = None


@dataclass
class Room:
    id: int
    name: str
    kind: str
    neighbors: list[int]
    resolved: bool = False
    visited: bool = False
    content_id: str | None = None


@dataclass
class Patrol:
    id: str
    room_id: int
    encounter_id: str
    x: int
    y: int
    active: bool = True


@dataclass
class EffectPickup:
    id: str
    kind: str
    x: int
    y: int
    hidden: bool = False
    resolved: bool = False
    payload: dict[str, Any] = field(default_factory=dict)


@dataclass
class GameState:
    seed: int
    phase: str
    heroes: list[Actor]
    deck: list[CardInstance]
    rooms: list[Room]
    world_tiles: list[str]
    hub_selection: list[str] = field(default_factory=list)
    current_room: int = 0
    party_x: int = 5
    party_y: int = 17
    exploration_steps: int = 0
    patrols: list[Patrol] = field(default_factory=list)
    active_patrol_id: str | None = None
    pickups: list[EffectPickup] = field(default_factory=list)
    current_pickup_id: str | None = None
    boons: dict[str, dict[str, int]] = field(default_factory=dict)
    curses: dict[str, dict[str, int]] = field(default_factory=dict)
    items: dict[str, int] = field(default_factory=dict)
    effect_counters: dict[str, int] = field(default_factory=dict)
    light: int = 100
    supplies: int = 4
    round: int = 0
    energy: int = 0
    enemies: list[Actor] = field(default_factory=list)
    draw_pile: list[CardInstance] = field(default_factory=list)
    discard_pile: list[CardInstance] = field(default_factory=list)
    hand: list[CardInstance] = field(default_factory=list)
    intents: list[dict[str, Any]] = field(default_factory=list)
    rewards: list[str] = field(default_factory=list)
    current_event: str | None = None
    service_type: str | None = None
    combat_kind: str | None = None
    log: list[str] = field(default_factory=list)


def _tuples(value: Any) -> Any:
    if isinstance(value, list):
        return tuple(_tuples(item) for item in value)
    return value


WORLD_WIDTH = 117
WORLD_HEIGHT = 35
ROOM_POSITIONS = {
    0: (5, 17),
    1: (18, 17),
    2: (30, 7),
    3: (30, 27),
    4: (43, 17),
    5: (55, 7),
    6: (55, 27),
    7: (68, 17),
    8: (80, 7),
    9: (80, 27),
    10: (93, 17),
    11: (111, 17),
}
ROOM_EDGES = {
    0: [1], 1: [0, 2, 3], 2: [1, 4], 3: [1, 4],
    4: [2, 3, 5, 6], 5: [4, 7], 6: [4, 7],
    7: [5, 6, 8, 9], 8: [7, 10], 9: [7, 10],
    10: [8, 9, 11], 11: [10],
}
WALKABLE_TILES = frozenset(".,=~")
VALID_WORLD_TILES = frozenset(" #O") | WALKABLE_TILES


def _build_world(rng: random.Random) -> list[str]:
    floor: set[tuple[int, int]] = set()
    corridors: set[tuple[int, int]] = set()
    room_bounds: dict[int, tuple[int, int]] = {}

    def carve_segment(
        start: tuple[int, int],
        end: tuple[int, int],
        *,
        broad: bool = False,
    ) -> None:
        start_x, start_y = start
        end_x, end_y = end
        if start_y == end_y:
            for x in range(min(start_x, end_x), max(start_x, end_x) + 1):
                floor.add((x, start_y))
                corridors.add((x, start_y))
                if broad and start_y + 1 < WORLD_HEIGHT - 1:
                    floor.add((x, start_y + 1))
                    corridors.add((x, start_y + 1))
        elif start_x == end_x:
            for y in range(min(start_y, end_y), max(start_y, end_y) + 1):
                floor.add((start_x, y))
                corridors.add((start_x, y))
                if broad and start_x + 1 < WORLD_WIDTH - 1:
                    floor.add((start_x + 1, y))
                    corridors.add((start_x + 1, y))
        else:
            raise ValueError("world segments must be orthogonal")

    for room_id, (center_x, center_y) in ROOM_POSITIONS.items():
        half_width = rng.randint(3, 5)
        half_height = rng.randint(1, 3)
        room_bounds[room_id] = (half_width, half_height)
        for y in range(center_y - half_height, center_y + half_height + 1):
            for x in range(center_x - half_width, center_x + half_width + 1):
                floor.add((x, y))

    for room_id, neighbors in ROOM_EDGES.items():
        start_x, start_y = ROOM_POSITIONS[room_id]
        for neighbor in neighbors:
            if neighbor < room_id:
                continue
            end_x, end_y = ROOM_POSITIONS[neighbor]
            broad = rng.random() < 0.28
            if start_y == end_y:
                direction = 1 if end_x > start_x else -1
                first_x = start_x + direction * max(2, abs(end_x - start_x) // 3)
                second_x = end_x - direction * max(2, abs(end_x - start_x) // 3)
                detour_y = max(2, min(WORLD_HEIGHT - 3, start_y + rng.choice((-2, 2))))
                carve_segment((start_x, start_y), (first_x, start_y), broad=broad)
                carve_segment((first_x, start_y), (first_x, detour_y), broad=broad)
                carve_segment((first_x, detour_y), (second_x, detour_y), broad=broad)
                carve_segment((second_x, detour_y), (second_x, end_y), broad=broad)
                carve_segment((second_x, end_y), (end_x, end_y), broad=broad)
            else:
                middle_x = max(
                    2,
                    min(WORLD_WIDTH - 3, (start_x + end_x) // 2 + rng.randint(-3, 3)),
                )
                carve_segment((start_x, start_y), (middle_x, start_y), broad=broad)
                carve_segment((middle_x, start_y), (middle_x, end_y), broad=broad)
                carve_segment((middle_x, end_y), (end_x, end_y), broad=broad)

    for _ in range(10):
        start_x, start_y = rng.choice(sorted(floor))
        step_x, step_y = rng.choice(((1, 0), (-1, 0), (0, 1), (0, -1)))
        length = rng.randint(2, 5)
        end_x = (
            max(2, min(WORLD_WIDTH - 3, start_x + step_x * length)) if step_x else start_x
        )
        end_y = (
            max(2, min(WORLD_HEIGHT - 3, start_y + step_y * length)) if step_y else start_y
        )
        carve_segment((start_x, start_y), (end_x, end_y))
        for y in range(end_y - 1, end_y + 2):
            for x in range(end_x - 1, end_x + 2):
                if 0 <= x < WORLD_WIDTH and 0 <= y < WORLD_HEIGHT:
                    floor.add((x, y))

    pillars: set[tuple[int, int]] = set()
    for room_id, (half_width, half_height) in room_bounds.items():
        center_x, center_y = ROOM_POSITIONS[room_id]
        candidates = [
            (x, y)
            for y in range(center_y - half_height, center_y + half_height + 1)
            for x in range(center_x - half_width, center_x + half_width + 1)
            if (x, y) != (center_x, center_y) and (x, y) not in corridors
        ]
        rng.shuffle(candidates)
        for position in candidates[: rng.randint(0, 2)]:
            floor.discard(position)
            pillars.add(position)

    start = ROOM_POSITIONS[0]
    reachable = {start}
    pending = deque([start])
    while pending:
        x, y = pending.popleft()
        for position in ((x, y - 1), (x - 1, y), (x + 1, y), (x, y + 1)):
            if position in floor and position not in reachable:
                reachable.add(position)
                pending.append(position)
    floor &= reachable

    cells = [[" " for _ in range(WORLD_WIDTH)] for _ in range(WORLD_HEIGHT)]
    for x, y in floor:
        cells[y][x] = "."
    decoration_candidates = sorted(floor - set(ROOM_POSITIONS.values()))
    for symbol in (",", "=", "~"):
        for _ in range(5):
            center_x, center_y = rng.choice(decoration_candidates)
            for y in range(center_y - 1, center_y + 2):
                for x in range(center_x - 1, center_x + 2):
                    if (x, y) in floor and rng.random() < 0.65:
                        cells[y][x] = symbol
    for x, y in pillars:
        cells[y][x] = "O"
    for x, y in floor:
        for adjacent_y in range(y - 1, y + 2):
            for adjacent_x in range(x - 1, x + 2):
                if (
                    0 <= adjacent_x < WORLD_WIDTH
                    and 0 <= adjacent_y < WORLD_HEIGHT
                    and cells[adjacent_y][adjacent_x] == " "
                ):
                    cells[adjacent_y][adjacent_x] = "#"
    return ["".join(row) for row in cells]


def _validate_world(tiles: Any) -> None:
    if (
        not isinstance(tiles, list)
        or len(tiles) != WORLD_HEIGHT
        or any(not isinstance(row, str) or len(row) != WORLD_WIDTH for row in tiles)
        or any(character not in VALID_WORLD_TILES for row in tiles for character in row)
    ):
        raise RuleError("save contains malformed world terrain")
    if any(tiles[y][x] not in WALKABLE_TILES for x, y in ROOM_POSITIONS.values()):
        raise RuleError("save terrain blocks a compartment anchor")


class GameEngine:
    """Owns the mutable run and its seeded pseudo-random stream."""

    SAVE_VERSION = 5

    def __init__(self, catalog: Catalog, state: GameState, rng: random.Random):
        self.catalog = catalog
        self.state = state
        self.rng = rng

    @classmethod
    def new(cls, catalog: Catalog, seed: int, *, start_in_hub: bool = False) -> GameEngine:
        rng = random.Random(seed)
        rooms = cls._generate_rooms(catalog, rng)
        world_tiles = _build_world(random.Random(seed ^ 0x4F5249534F4E))
        default_party = list(catalog.heroes)[:4]
        state = GameState(
            seed=seed,
            phase="hub",
            heroes=[],
            deck=[],
            rooms=rooms,
            world_tiles=world_tiles,
            hub_selection=default_party,
            party_x=ROOM_POSITIONS[0][0],
            party_y=ROOM_POSITIONS[0][1],
            light=catalog.balance.get("starting_light", 100),
            supplies=catalog.balance.get("starting_supplies", 4),
            log=["Crew manifest opened in the Orison airlock."],
        )
        engine = cls(catalog, state, rng)
        state.pickups = engine._generate_pickups(random.Random(seed ^ 0x5049434B5550))
        if not start_in_hub:
            engine.begin_expedition()
        return engine

    def toggle_hub_crew(self, hero_id: str) -> None:
        if self.state.phase != "hub" or hero_id not in self.catalog.heroes:
            raise RuleError("that crew manifest entry is unavailable")
        if hero_id in self.state.hub_selection:
            self.state.hub_selection.remove(hero_id)
        elif len(self.state.hub_selection) < 4:
            self.state.hub_selection.append(hero_id)
        else:
            raise RuleError("the expedition can carry only four crew members")

    def reorder_hub_crew(self, hero_id: str, direction: int) -> None:
        if self.state.phase != "hub" or hero_id not in self.state.hub_selection:
            raise RuleError("select that crew member before assigning a rank")
        index = self.state.hub_selection.index(hero_id)
        destination = max(0, min(len(self.state.hub_selection) - 1, index + direction))
        if destination != index:
            self.state.hub_selection[index], self.state.hub_selection[destination] = (
                self.state.hub_selection[destination],
                self.state.hub_selection[index],
            )

    def begin_expedition(self) -> None:
        if self.state.phase != "hub":
            raise RuleError("the expedition has already departed")
        if len(self.state.hub_selection) != 4 or len(set(self.state.hub_selection)) != 4:
            raise RuleError("select exactly four unique crew members")
        self.state.heroes = []
        self.state.deck = []
        for rank, hero_id in enumerate(self.state.hub_selection, 1):
            hero = self.catalog.heroes[hero_id]
            self.state.heroes.append(
                Actor(
                    id=hero_id,
                    name=hero["name"],
                    hero_class=hero["role"],
                    max_hp=hero["max_hp"],
                    hp=hero["max_hp"],
                    rank=rank,
                    side="hero",
                )
            )
            self.state.deck.extend(CardInstance(card_id) for card_id in hero["starter_deck"])
        self.state.patrols = [
            Patrol(
                id=f"patrol:{room.id}",
                room_id=room.id,
                encounter_id=room.content_id or "",
                x=ROOM_POSITIONS[room.id][0],
                y=ROOM_POSITIONS[room.id][1],
            )
            for room in self.state.rooms
            if room.kind in {"fight", "elite", "boss"}
        ]
        self.state.phase = "exploration"
        self.state.log = ["The airlock seals. The Orison is no longer empty."]

    @staticmethod
    def _generate_rooms(catalog: Catalog, rng: random.Random) -> list[Room]:
        kinds = ["fight"] * 4 + ["event"] * 2 + ["camp", "upgrade", "elite", "cache"]
        rng.shuffle(kinds)
        normal = [item["id"] for item in catalog.encounters.values() if item["kind"] == "normal"]
        elite = [item["id"] for item in catalog.encounters.values() if item["kind"] == "elite"]
        events = list(catalog.events)
        rng.shuffle(events)
        labels = {
            "fight": "Contested Deck",
            "event": "Unstable Compartment",
            "camp": "Sealed Crew Quarters",
            "upgrade": "Machine Workshop",
            "elite": "Heavy Motion Contact",
            "cache": "Emergency Stores",
        }
        rooms = [Room(0, "Docking Airlock", "start", ROOM_EDGES[0], True, True)]
        event_index = 0
        for room_id, kind in enumerate(kinds, 1):
            content_id = None
            if kind == "fight":
                content_id = rng.choice(normal)
            elif kind == "elite":
                content_id = rng.choice(elite)
            elif kind == "event":
                content_id = events[event_index]
                event_index += 1
            rooms.append(Room(room_id, labels[kind], kind, ROOM_EDGES[room_id], content_id=content_id))
        rooms.append(Room(11, "Overseer Chamber", "boss", ROOM_EDGES[11], content_id="the_core"))
        return rooms

    def _generate_pickups(self, rng: random.Random) -> list[EffectPickup]:
        categories = ["boon"] * 3 + ["item"] * 5 + ["bargain"] * 2 + ["trap"] * 2
        rng.shuffle(categories)
        anchors = set(ROOM_POSITIONS.values())
        selected: list[tuple[int, int]] = []
        pickups: list[EffectPickup] = []
        bands = ((0, 38), (39, 77), (78, WORLD_WIDTH - 1))
        for index, kind in enumerate(categories):
            left, right = bands[index // 4]
            candidates = [
                (x, y)
                for y, row in enumerate(self.state.world_tiles)
                for x, character in enumerate(row)
                if left <= x <= right
                and character in WALKABLE_TILES
                and (x, y) not in anchors
                and abs(x - ROOM_POSITIONS[0][0]) + abs(y - ROOM_POSITIONS[0][1]) >= 6
                and abs(x - ROOM_POSITIONS[11][0]) + abs(y - ROOM_POSITIONS[11][1]) >= 5
                and all(abs(x - ax) + abs(y - ay) >= 3 for ax, ay in anchors)
                and all(abs(x - px) + abs(y - py) >= 5 for px, py in selected)
            ]
            if kind == "trap":
                hazardous = [
                    position
                    for position in candidates
                    if self.state.world_tiles[position[1]][position[0]] in ",~"
                ]
                candidates = hazardous or candidates
            if not candidates:
                raise RuleError("generated terrain has no valid discovery positions")
            x, y = rng.choice(candidates)
            selected.append((x, y))
            pickups.append(
                EffectPickup(
                    id=f"discovery:{index}",
                    kind=kind,
                    x=x,
                    y=y,
                    hidden=kind == "trap",
                )
            )

        item_pool = rng.sample(list(self.catalog.items), 5)
        item_ids = [item_pool[0], item_pool[0], item_pool[1], item_pool[2], item_pool[3]]
        rng.shuffle(item_ids)
        for pickup, item_id in zip(
            (item for item in pickups if item.kind == "item"),
            item_ids,
            strict=True,
        ):
            pickup.payload["item_id"] = item_id
        return pickups

    @classmethod
    def from_snapshot(cls, catalog: Catalog, snapshot: dict[str, Any]) -> GameEngine:
        if snapshot.get("save_version") != cls.SAVE_VERSION:
            raise RuleError("unsupported save version")
        if snapshot.get("content_schema_version") != catalog.raw["schema_version"]:
            raise RuleError("save was created for a different content schema")
        raw = snapshot.get("state")
        if not isinstance(raw, dict):
            raise RuleError("save has no game state")
        try:
            state = GameState(
                seed=raw["seed"],
                phase=raw["phase"],
                heroes=[Actor(**item) for item in raw["heroes"]],
                deck=[CardInstance(**item) for item in raw["deck"]],
                rooms=[Room(**item) for item in raw["rooms"]],
                world_tiles=raw["world_tiles"],
                hub_selection=raw["hub_selection"],
                current_room=raw["current_room"],
                party_x=raw["party_x"],
                party_y=raw["party_y"],
                exploration_steps=raw["exploration_steps"],
                patrols=[Patrol(**item) for item in raw["patrols"]],
                active_patrol_id=raw["active_patrol_id"],
                pickups=[EffectPickup(**item) for item in raw["pickups"]],
                current_pickup_id=raw["current_pickup_id"],
                boons=raw["boons"],
                curses=raw["curses"],
                items=raw["items"],
                effect_counters=raw["effect_counters"],
                light=raw["light"],
                supplies=raw["supplies"],
                round=raw["round"],
                energy=raw["energy"],
                enemies=[Actor(**item) for item in raw["enemies"]],
                draw_pile=[CardInstance(**item) for item in raw["draw_pile"]],
                discard_pile=[CardInstance(**item) for item in raw["discard_pile"]],
                hand=[CardInstance(**item) for item in raw["hand"]],
                intents=raw["intents"],
                rewards=raw["rewards"],
                current_event=raw["current_event"],
                service_type=raw["service_type"],
                combat_kind=raw["combat_kind"],
                log=raw["log"],
            )
            rng = random.Random()
            rng.setstate(_tuples(snapshot["rng_state"]))
        except (KeyError, TypeError, ValueError) as exc:
            raise RuleError(f"invalid save data: {exc}") from exc
        _validate_world(state.world_tiles)
        engine = cls(catalog, state, rng)
        walkable_count = sum(character in WALKABLE_TILES for row in state.world_tiles for character in row)
        if len(engine._distances_from(ROOM_POSITIONS[0])) != walkable_count:
            raise RuleError("save contains disconnected world terrain")
        hero_ids = {hero.id for hero in state.heroes}
        if state.phase == "hub" and state.heroes:
            raise RuleError("hub save unexpectedly contains an active party")
        if state.phase != "hub" and (len(hero_ids) != 4 or not hero_ids <= set(catalog.heroes)):
            raise RuleError("save contains an unexpected crew roster")
        if len(state.hub_selection) > 4 or any(hero_id not in catalog.heroes for hero_id in state.hub_selection):
            raise RuleError("save contains an invalid hub selection")
        if not engine.is_walkable(state.party_x, state.party_y):
            raise RuleError("save places the crew outside the ship")
        patrol_ids = {patrol.id for patrol in state.patrols}
        if len(patrol_ids) != len(state.patrols):
            raise RuleError("save contains duplicate patrols")
        for patrol in state.patrols:
            room = state.rooms[patrol.room_id] if 0 <= patrol.room_id < len(state.rooms) else None
            if (
                room is None
                or patrol.encounter_id not in catalog.encounters
                or not engine.is_walkable(patrol.x, patrol.y)
            ):
                raise RuleError("save contains an invalid patrol")
        if state.active_patrol_id is not None and state.active_patrol_id not in patrol_ids:
            raise RuleError("save references an unknown active patrol")
        pickup_ids = {pickup.id for pickup in state.pickups}
        pickup_positions = {(pickup.x, pickup.y) for pickup in state.pickups}
        if (
            len(state.pickups) != 12
            or len(pickup_ids) != 12
            or len(pickup_positions) != 12
            or any(pickup.kind not in {"boon", "item", "bargain", "trap"} for pickup in state.pickups)
            or any(not engine.is_walkable(pickup.x, pickup.y) for pickup in state.pickups)
        ):
            raise RuleError("save contains invalid map discoveries")
        if state.current_pickup_id is not None and state.current_pickup_id not in pickup_ids:
            raise RuleError("save references an unknown map discovery")
        for hero_id, effects in state.boons.items():
            if hero_id not in catalog.heroes or any(
                boon_id not in catalog.boons or not isinstance(count, int) or count < 1
                for boon_id, count in effects.items()
            ):
                raise RuleError("save contains invalid boons")
        for hero_id, effects in state.curses.items():
            if hero_id not in catalog.heroes or any(
                curse_id not in catalog.curses or not isinstance(count, int) or count < 1
                for curse_id, count in effects.items()
            ):
                raise RuleError("save contains invalid curses")
        if any(
            item_id not in catalog.items or not isinstance(count, int) or count < 1
            for item_id, count in state.items.items()
        ):
            raise RuleError("save contains invalid items")
        piles = state.deck + state.hand + state.draw_pile + state.discard_pile
        if any(
            card.card_id not in catalog.cards
            and not (card.card_id in catalog.curses and catalog.curses[card.card_id]["kind"] == "card")
            for card in piles
        ):
            raise RuleError("save references an unknown card")
        return engine

    def snapshot(self) -> dict[str, Any]:
        return {
            "save_version": self.SAVE_VERSION,
            "content_schema_version": self.catalog.raw["schema_version"],
            "state": asdict(self.state),
            "rng_state": self.rng.getstate(),
        }

    def add_log(self, message: str) -> None:
        self.state.log.append(message)
        del self.state.log[:-60]

    def room(self, room_id: int | None = None) -> Room:
        return self.state.rooms[self.state.current_room if room_id is None else room_id]

    @staticmethod
    def room_position(room_id: int) -> tuple[int, int]:
        try:
            return ROOM_POSITIONS[room_id]
        except KeyError as exc:
            raise RuleError("unknown ship compartment") from exc

    def world_tiles(self) -> list[str]:
        return self.state.world_tiles

    def is_walkable(self, x: int, y: int) -> bool:
        return (
            0 <= y < WORLD_HEIGHT
            and 0 <= x < WORLD_WIDTH
            and self.state.world_tiles[y][x] in WALKABLE_TILES
        )

    def _neighbors(self, position: tuple[int, int]) -> list[tuple[int, int]]:
        x, y = position
        return [
            candidate
            for candidate in ((x, y - 1), (x - 1, y), (x + 1, y), (x, y + 1))
            if self.is_walkable(*candidate)
        ]

    def _find_path(
        self,
        start: tuple[int, int],
        destination: tuple[int, int],
    ) -> list[tuple[int, int]]:
        if not self.is_walkable(*destination):
            return []
        pending = deque([start])
        previous: dict[tuple[int, int], tuple[int, int] | None] = {start: None}
        while pending:
            current = pending.popleft()
            if current == destination:
                break
            for neighbor in self._neighbors(current):
                if neighbor not in previous:
                    previous[neighbor] = current
                    pending.append(neighbor)
        if destination not in previous:
            return []
        path = []
        current = destination
        while current != start:
            path.append(current)
            parent = previous[current]
            if parent is None:
                break
            current = parent
        path.reverse()
        return path

    def _distances_from(self, origin: tuple[int, int]) -> dict[tuple[int, int], int]:
        distances = {origin: 0}
        pending = deque([origin])
        while pending:
            current = pending.popleft()
            for neighbor in self._neighbors(current):
                if neighbor not in distances:
                    distances[neighbor] = distances[current] + 1
                    pending.append(neighbor)
        return distances

    def path_to(self, x: int, y: int) -> list[tuple[int, int]]:
        if self.state.phase != "exploration":
            raise RuleError("the party cannot navigate right now")
        if not self.is_walkable(x, y):
            raise RuleError("choose a floor tile inside the ship")
        path = self._find_path((self.state.party_x, self.state.party_y), (x, y))
        if (x, y) != (self.state.party_x, self.state.party_y) and not path:
            raise RuleError("no route reaches that tile")
        maximum = self.maximum_navigation_distance()
        if len(path) > maximum:
            raise RuleError(f"destination is beyond the maximum reach of {maximum} tiles")
        return path

    def maximum_navigation_distance(self) -> int:
        return int(self.catalog.balance["maximum_navigation_distance"]) + round(
            self._item_effect_value("survey_reach")
        )

    def move_to(self, room_id: int) -> None:
        destination = self.room_position(room_id)
        path = self._find_path((self.state.party_x, self.state.party_y), destination)
        for x, y in path:
            self.step_exploration(x, y)
            if self.state.phase != "exploration":
                return

    def step_exploration(self, x: int, y: int) -> None:
        if self.state.phase != "exploration":
            raise RuleError("the party cannot move right now")
        if (x, y) not in self._neighbors((self.state.party_x, self.state.party_y)):
            raise RuleError("the party can move only one floor tile at a time")
        self.state.party_x = x
        self.state.party_y = y
        self.state.exploration_steps += 1
        interval = int(self.catalog.balance["exploration_steps_per_light"])
        if self.state.exploration_steps % interval == 0:
            self.state.light = max(0, self.state.light - 1)
            if self.state.light < self.catalog.balance["low_light_threshold"]:
                for hero in self.living_heroes():
                    self._change_stress(hero, 1)
        if self.state.exploration_steps % 8 == 0:
            for hero in self.living_heroes():
                amount = round(self._hero_effect_value(hero, "curse", "night_terror_stress"))
                if amount:
                    self._change_stress(hero, amount)
        if self.state.exploration_steps % 10 == 0:
            leak = sum(
                round(self._hero_effect_value(hero, "curse", "leaking_light"))
                for hero in self.living_heroes()
            )
            if leak:
                self.state.light = max(0, self.state.light - leak)
                self.add_log(f"Leaking Lamp drains {leak} light.")
        patrol = self._patrol_at(x, y)
        if patrol:
            self._start_patrol_combat(patrol)
            return
        self._resolve_exploration_tile()
        if self.state.phase == "exploration":
            self._advance_patrols()

    def _patrol_at(self, x: int, y: int) -> Patrol | None:
        return next((patrol for patrol in self.state.patrols if patrol.active and (patrol.x, patrol.y) == (x, y)), None)

    def _start_patrol_combat(self, patrol: Patrol) -> None:
        room = self.room(patrol.room_id)
        self.state.current_room = room.id
        room.visited = True
        self.state.active_patrol_id = patrol.id
        self.add_log(f"{room.name}: hostile contact at close range.")
        self.start_combat(patrol.encounter_id, room.kind, surprised=self._surprised())

    def _resolve_exploration_tile(self) -> None:
        position = (self.state.party_x, self.state.party_y)
        pickup = next(
            (
                item
                for item in self.state.pickups
                if not item.resolved and (item.x, item.y) == position
            ),
            None,
        )
        if pickup:
            self.state.phase = "discovery"
            self.state.current_pickup_id = pickup.id
            return
        room = next((room for room in self.state.rooms if self.room_position(room.id) == position), None)
        if room is None:
            return
        self.state.current_room = room.id
        room.visited = True
        if room.resolved or room.kind in {"start", "fight", "elite", "boss"}:
            return
        self.add_log(f"Entered {room.name}.")
        self._enter_room(room)

    def current_pickup(self) -> EffectPickup:
        pickup = next(
            (item for item in self.state.pickups if item.id == self.state.current_pickup_id),
            None,
        )
        if self.state.phase != "discovery" or pickup is None or pickup.resolved:
            raise RuleError("there is no discovery to resolve")
        return pickup

    def _finish_pickup(self, message: str) -> None:
        pickup = self.current_pickup()
        pickup.resolved = True
        self.state.current_pickup_id = None
        self.state.phase = "exploration"
        self.add_log(message)
        self._resolve_exploration_tile()

    def _boon_is_eligible(self, hero_id: str, boon_id: str) -> bool:
        tags = {
            tag
            for owned_id in self.state.boons.get(hero_id, {})
            for tag in self.catalog.boons[owned_id].get("tags", [])
        }
        requirements = set(self.catalog.boons[boon_id].get("requires_all_tags", []))
        return requirements <= tags

    def boon_options(self, hero_id: str, *, count: int | None = None) -> list[str]:
        if hero_id not in {hero.id for hero in self.living_heroes()}:
            raise RuleError("that hero cannot receive a boon")
        eligible = [
            boon_id for boon_id in self.catalog.boons if self._boon_is_eligible(hero_id, boon_id)
        ]
        count = count or 3 + round(self._item_effect_value("boon_offer_choices"))
        options = []
        while eligible and len(options) < count:
            weights = [3 if boon_id not in self.state.boons.get(hero_id, {}) else 1 for boon_id in eligible]
            choice = self.rng.choices(eligible, weights=weights, k=1)[0]
            options.append(choice)
            eligible.remove(choice)
        return options

    def acquire_boon(self, hero_id: str, boon_id: str) -> int:
        if (
            hero_id not in {hero.id for hero in self.living_heroes()}
            or boon_id not in self.catalog.boons
            or not self._boon_is_eligible(hero_id, boon_id)
        ):
            raise RuleError("invalid boon recipient or definition")
        owned = self.state.boons.setdefault(hero_id, {})
        owned[boon_id] = owned.get(boon_id, 0) + 1
        return owned[boon_id]

    def acquire_item(self, item_id: str, copies: int = 1) -> int:
        if item_id not in self.catalog.items or copies < 1:
            raise RuleError("invalid item acquisition")
        gained = copies + round(self._item_effect_value("salvage_copies"))
        self.state.items[item_id] = self.state.items.get(item_id, 0) + gained
        for hero in self.living_heroes():
            stress = round(self._hero_effect_value(hero, "curse", "scavenger_stress"))
            if stress:
                self._change_stress(hero, stress)
        return gained

    def acquire_curse(self, hero_id: str, curse_id: str) -> int:
        if hero_id not in {hero.id for hero in self.living_heroes()} or curse_id not in self.catalog.curses:
            raise RuleError("invalid curse victim or definition")
        owned = self.state.curses.setdefault(hero_id, {})
        owned[curse_id] = owned.get(curse_id, 0) + 1
        if self.catalog.curses[curse_id]["kind"] == "card":
            self.state.deck.append(CardInstance(curse_id, bound_hero_id=hero_id))
        return owned[curse_id]

    def boon_pickup_options(self, hero_id: str) -> list[str]:
        pickup = self.current_pickup()
        if pickup.kind != "boon":
            raise RuleError("this discovery is not a boon beacon")
        if pickup.payload.get("hero_id") != hero_id:
            pickup.payload = {
                "hero_id": hero_id,
                "options": self.boon_options(hero_id),
            }
        return list(pickup.payload["options"])

    def resolve_boon_pickup(self, hero_id: str, boon_id: str) -> str:
        pickup = self.current_pickup()
        if pickup.kind != "boon":
            raise RuleError("this discovery is not a boon beacon")
        if boon_id not in self.boon_pickup_options(hero_id):
            raise RuleError("that boon was not offered")
        count = self.acquire_boon(hero_id, boon_id)
        hero = self._actor(hero_id)
        name = self.catalog.boons[boon_id]["name"]
        message = f"{hero.name} receives {name} x{count}."
        self._finish_pickup(message)
        return message

    def resolve_item_pickup(self) -> str:
        pickup = self.current_pickup()
        if pickup.kind != "item":
            raise RuleError("this discovery is not salvage")
        item_id = str(pickup.payload["item_id"])
        gained = self.acquire_item(item_id)
        name = self.catalog.items[item_id]["name"]
        message = f"Recovered {name} x{gained}. Total {self.state.items[item_id]}."
        self._finish_pickup(message)
        return message

    def bargain_options(self, hero_id: str) -> list[dict[str, str | int]]:
        pickup = self.current_pickup()
        if pickup.kind != "bargain":
            raise RuleError("this discovery offers no bargain")
        if pickup.payload.get("hero_id") != hero_id:
            trait_ids = [
                curse_id for curse_id, curse in self.catalog.curses.items() if curse["kind"] == "trait"
            ]
            card_ids = [
                curse_id for curse_id, curse in self.catalog.curses.items() if curse["kind"] == "card"
            ]
            item_pool = [
                item.payload["item_id"] for item in self.state.pickups if item.kind == "item"
            ]
            pickup.payload = {
                "hero_id": hero_id,
                "options": [
                    {
                        "reward_kind": "boon",
                        "reward_id": self.rng.choice(self.boon_options(hero_id)),
                        "curse_id": self.rng.choice(trait_ids),
                        "copies": 1,
                    },
                    {
                        "reward_kind": "item",
                        "reward_id": self.rng.choice(item_pool),
                        "curse_id": self.rng.choice(card_ids),
                        "copies": 2,
                    },
                ],
            }
        return list(pickup.payload["options"])

    def resolve_bargain(self, hero_id: str, option_index: int | None) -> str:
        pickup = self.current_pickup()
        if pickup.kind != "bargain":
            raise RuleError("this discovery offers no bargain")
        if option_index is None:
            message = "The crew leaves the anomaly unanswered."
            self._finish_pickup(message)
            return message
        options = self.bargain_options(hero_id)
        if not 0 <= option_index < len(options):
            raise RuleError("invalid bargain")
        option = options[option_index]
        curse_id = str(option["curse_id"])
        reward_id = str(option["reward_id"])
        self.acquire_curse(hero_id, curse_id)
        if option["reward_kind"] == "boon":
            self.acquire_boon(hero_id, reward_id)
            reward_name = self.catalog.boons[reward_id]["name"]
        else:
            gained = self.acquire_item(reward_id, int(option["copies"]))
            reward_name = f"{self.catalog.items[reward_id]['name']} x{gained}"
        curse_name = self.catalog.curses[curse_id]["name"]
        message = f"Accepted {reward_name}; {curse_name} takes hold."
        self._finish_pickup(message)
        return message

    def resolve_hidden_trap(self) -> str:
        pickup = self.current_pickup()
        if pickup.kind != "trap":
            raise RuleError("this discovery is not a hidden trap")
        hero = self.rng.choice(self.living_heroes())
        curse_id = self.rng.choice(list(self.catalog.curses))
        count = self.acquire_curse(hero.id, curse_id)
        name = self.catalog.curses[curse_id]["name"]
        message = f"Hidden anomaly: {hero.name} gains {name} x{count}."
        self._finish_pickup(message)
        return message

    def _advance_patrols(self) -> None:
        party = (self.state.party_x, self.state.party_y)
        distances = self._distances_from(party)
        occupied = {(patrol.x, patrol.y) for patrol in self.state.patrols if patrol.active}
        for patrol in (item for item in self.state.patrols if item.active):
            current = (patrol.x, patrol.y)
            occupied.discard(current)
            room_kind = self.room(patrol.room_id).kind
            aggression = 12 if room_kind == "elite" else 8 if room_kind == "boss" else 10
            aggression = max(4, aggression - round(self._item_effect_value("patrol_aggression_reduction")))
            destination = current
            if 0 < distances.get(current, WORLD_WIDTH * WORLD_HEIGHT) <= aggression:
                choices = [tile for tile in self._neighbors(current) if tile not in occupied]
                if choices:
                    destination = min(choices, key=lambda tile: (distances.get(tile, WORLD_WIDTH * WORLD_HEIGHT), tile))
            elif room_kind != "boss" and self.state.exploration_steps % 2 == 0:
                home = self.room_position(patrol.room_id)
                choices = [
                    tile
                    for tile in self._neighbors(current)
                    if tile not in occupied and abs(tile[0] - home[0]) + abs(tile[1] - home[1]) <= 7
                ]
                if choices:
                    destination = self.rng.choice(choices)
            if destination in occupied:
                destination = current
            patrol.x, patrol.y = destination
            occupied.add(destination)
            if destination == party:
                self._start_patrol_combat(patrol)
                return

    def _enter_room(self, room: Room) -> None:
        if room.kind in {"fight", "elite", "boss"}:
            self.start_combat(room.content_id or "", room.kind, surprised=self._surprised())
        elif room.kind == "event":
            self.state.phase = "event"
            self.state.current_event = room.content_id
        elif room.kind in {"camp", "upgrade"}:
            self.state.phase = "service"
            self.state.service_type = room.kind
        elif room.kind == "cache":
            self.state.supplies += 2
            self.state.light = min(100, self.state.light + 20)
            room.resolved = True
            self.add_log("Emergency stores yield 2 supplies and 20 light.")

    def _surprised(self) -> bool:
        return (
            self.state.light < self.catalog.balance["low_light_threshold"]
            and self.rng.random() < self.catalog.balance.get("low_light_ambush_chance", 0.35)
        )

    def use_supply(self, purpose: str) -> None:
        if self.state.phase != "exploration" or self.state.supplies < 1:
            raise RuleError("no supply can be used now")
        if purpose == "heal":
            target = min(self.state.heroes, key=lambda actor: actor.hp / actor.max_hp)
            self._heal(target, 9 + round(self._item_effect_value("supply_heal_bonus")))
            message = f"A supply restores {target.name}."
        elif purpose == "calm":
            target = max(self.state.heroes, key=lambda actor: actor.stress)
            self._change_stress(target, -14)
            message = f"A supply steadies {target.name}."
        elif purpose == "light":
            amount = 25 + round(self._item_effect_value("supply_light_bonus"))
            self.state.light = min(100, self.state.light + amount)
            message = f"A flare restores {amount} light."
        else:
            raise RuleError("unknown supply use")
        self.state.supplies -= 1
        self.add_log(message)

    def start_combat(self, encounter_id: str, kind: str | None = None, surprised: bool = False) -> None:
        encounter = self.catalog.encounters.get(encounter_id)
        if not encounter:
            raise RuleError(f"unknown encounter: {encounter_id}")
        self.state.phase = "combat"
        self.state.combat_kind = kind or encounter["kind"]
        self.state.enemies = []
        for rank, enemy_id in enumerate(encounter["enemies"], 1):
            definition = self.catalog.enemies[enemy_id]
            self.state.enemies.append(
                Actor(
                    f"{enemy_id}:{rank}",
                    definition["name"],
                    definition["max_hp"],
                    definition["max_hp"],
                    rank,
                    "enemy",
                    definition_id=enemy_id,
                )
            )
        self.state.draw_pile = [
            CardInstance(card.card_id, card.upgraded, card.bound_hero_id) for card in self.state.deck
        ]
        self.rng.shuffle(self.state.draw_pile)
        self.state.discard_pile = []
        self.state.hand = []
        self.state.round = 1
        self.state.effect_counters = {}
        self.state.intents = self._choose_intents()
        self.add_log(f"Combat begins: {encounter['id']}.")
        self._start_player_turn()
        if surprised:
            self.add_log("The crew is surprised in the darkness.")
            self._enemy_phase()
            if self.state.phase != "combat":
                return
            self.state.intents = self._choose_intents()

    def _start_player_turn(self) -> None:
        for hero in self.living_heroes():
            hero.block = 0
            self.state.effect_counters[f"round_cards:{hero.id}"] = 0
            self.state.effect_counters[f"countercurrent:{hero.id}"] = 0
            self._tick_wound(hero)
            modifiers = self._affliction_modifiers(hero)
            if modifiers.get("turn_stress"):
                self._change_stress(hero, int(modifiers["turn_stress"]))
            if self.state.round == 1:
                start_block = self._hero_effect_value(hero, "boon", "start_block")
                start_block += self._item_effect_value("stacked_start_block")
                vigilance = self.state.boons.get(hero.id, {}).get("vigilance", 0)
                if vigilance:
                    hero.statuses["dodge"] = max(hero.statuses.get("dodge", 0), 2)
                    start_block += min(8, max(0, vigilance - 1) * 2)
                hero.block += round(start_block)
                relief = round(self._hero_effect_value(hero, "boon", "start_stress_relief"))
                if relief:
                    self._change_stress(hero, -relief)
                marked = round(self._hero_effect_value(hero, "curse", "start_marked"))
                vulnerable = 0
                brittle = self.state.curses.get(hero.id, {}).get("brittle_guard", 0)
                if brittle:
                    vulnerable = 2 if brittle >= 3 else 1
                if self.state.curses.get(hero.id, {}).get("lead_feet", 0) >= 3:
                    vulnerable = max(vulnerable, 1)
                if marked:
                    hero.statuses["marked"] = max(hero.statuses.get("marked", 0), marked)
                if vulnerable:
                    hero.statuses["vulnerable"] = max(hero.statuses.get("vulnerable", 0), vulnerable)
        if self.state.phase != "combat":
            return
        opening_energy = round(self._item_effect_value("first_round_energy")) if self.state.round == 1 else 0
        self.state.energy = self.catalog.balance["energy"] + opening_energy
        opening_cards = round(self._item_effect_value("opening_hand")) if self.state.round == 1 else 0
        self._draw(self.catalog.balance["hand_size"] + opening_cards - len(self.state.hand))

    def living_heroes(self) -> list[Actor]:
        return sorted((actor for actor in self.state.heroes if actor.alive), key=lambda actor: actor.rank)

    def living_enemies(self) -> list[Actor]:
        return sorted((actor for actor in self.state.enemies if actor.hp > 0), key=lambda actor: actor.rank)

    def card_definition(self, card: CardInstance) -> dict[str, Any]:
        if card.card_id in self.catalog.cards:
            return self.catalog.cards[card.card_id]
        return self.catalog.curses[card.card_id]

    @staticmethod
    def _stack_value(effect: dict[str, Any], count: int) -> float:
        amount = float(effect.get("amount", 0))
        cap = float(effect.get("cap", amount * count))
        curve = effect["curve"]
        if curve == "linear":
            return min(cap, amount * count)
        if curve == "diminishing":
            if cap <= 0:
                return 0
            return cap * (1 - (1 - amount / cap) ** count)
        if curve == "threshold":
            return min(cap, (count // int(effect["every"])) * amount)
        return min(cap, amount * count)

    def effect_value(self, group: str, effect_id: str, key: str, count: int) -> float:
        catalog = {
            "boon": self.catalog.boons,
            "curse": self.catalog.curses,
            "item": self.catalog.items,
        }[group]
        return sum(
            self._stack_value(effect, count)
            for effect in catalog[effect_id]["effects"]
            if effect["key"] == key
        )

    def _hero_effect_value(self, hero: Actor, group: str, key: str) -> float:
        owned = self.state.boons if group == "boon" else self.state.curses
        return sum(
            self.effect_value(group, effect_id, key, count)
            for effect_id, count in owned.get(hero.id, {}).items()
        )

    def _item_effect_value(self, key: str) -> float:
        return sum(
            self.effect_value("item", item_id, key, count)
            for item_id, count in self.state.items.items()
        )

    def effect_counts(self) -> tuple[int, int, int]:
        return (
            sum(sum(values.values()) for values in self.state.boons.values()),
            sum(sum(values.values()) for values in self.state.curses.values()),
            sum(self.state.items.values()),
        )

    def effect_description(self, group: str, effect_id: str, count: int) -> str:
        catalog = {
            "boon": self.catalog.boons,
            "curse": self.catalog.curses,
            "item": self.catalog.items,
        }[group]
        definition = catalog[effect_id]
        values = []
        for effect in definition["effects"]:
            value = self._stack_value(effect, count)
            shown = f"{value * 100:.0f}%" if abs(value) < 1 and value else f"{value:g}"
            values.append(f"{effect['key'].replace('_', ' ')} {shown}")
        return f"{definition['description']} Current: {', '.join(values)}."

    def compact_effect_summary(self, limit: int = 2) -> str:
        effects = []
        for hero in self.living_heroes():
            for effect_id, count in self.state.boons.get(hero.id, {}).items():
                effects.append(f"{self.catalog.boons[effect_id]['name']} x{count}")
            for effect_id, count in self.state.curses.get(hero.id, {}).items():
                effects.append(f"{self.catalog.curses[effect_id]['name']} x{count}")
        cargo = [f"{self.catalog.items[item_id]['name']} x{count}" for item_id, count in self.state.items.items()]

        def abbreviated(entries: list[str]) -> str:
            visible = entries[:limit]
            if len(entries) > limit:
                visible.append(f"+{len(entries) - limit} more")
            return ", ".join(visible) or "none"

        return f"FX {abbreviated(effects)} | CARGO {abbreviated(cargo)}"

    def card_cost(self, card: CardInstance) -> int:
        definition = self.card_definition(card)
        if card.card_id in self.catalog.curses:
            return 99
        actor = self._actor(definition["hero"])
        delta = int(self._affliction_modifiers(actor).get("card_cost_delta", 0))
        base = definition.get("upgrade_cost", definition["cost"]) if card.upgraded else definition["cost"]
        round_plays = self.state.effect_counters.get(f"round_cards:{actor.id}", 0)
        combat_plays = self.state.effect_counters.get(f"combat_cards:{actor.id}", 0)
        quick_stacks = self.state.boons.get(actor.id, {}).get("quick_hands", 0)
        if round_plays < min(quick_stacks, 3):
            delta -= 1
        if combat_plays == 0:
            delta += round(self._hero_effect_value(actor, "curse", "first_card_cost_increase"))
        return max(0, base + delta)

    def valid_targets(self, hand_index: int) -> list[str]:
        if not 0 <= hand_index < len(self.state.hand):
            return []
        definition = self.card_definition(self.state.hand[hand_index])
        if self.state.hand[hand_index].card_id in self.catalog.curses:
            return []
        target = definition["target"]
        if target == "self":
            return [definition["hero"]]
        if target in {"all_enemies", "all_allies"}:
            return [target]
        if target == "enemy":
            ranks = definition["target_ranks"]
            return [actor.id for actor in self.living_enemies() if actor.rank in ranks]
        if target == "ally":
            return [actor.id for actor in self.living_heroes()]
        return []

    def play_card(self, hand_index: int, target_id: str | None = None) -> None:
        if self.state.phase != "combat":
            raise RuleError("cards can only be played in combat")
        if not 0 <= hand_index < len(self.state.hand):
            raise RuleError("invalid hand position")
        card = self.state.hand[hand_index]
        definition = self.card_definition(card)
        if card.card_id in self.catalog.curses:
            raise RuleError("curse cards cannot be played")
        actor = self._actor(definition["hero"])
        if not actor.alive or actor.rank not in definition["from_ranks"]:
            raise RuleError("the acting hero is not in a valid rank")
        if actor.statuses.get("stun"):
            raise RuleError("the acting hero is stunned this turn")
        cost = self.card_cost(card)
        if cost > self.state.energy:
            raise RuleError("not enough energy")
        valid = self.valid_targets(hand_index)
        target_id = target_id or (valid[0] if len(valid) == 1 else None)
        if target_id not in valid:
            raise RuleError("choose a valid target")
        self.state.energy -= cost
        self.state.hand.pop(hand_index)
        self.state.discard_pile.append(card)
        round_key = f"round_cards:{actor.id}"
        combat_key = f"combat_cards:{actor.id}"
        self.state.effect_counters[round_key] = self.state.effect_counters.get(round_key, 0) + 1
        self.state.effect_counters[combat_key] = self.state.effect_counters.get(combat_key, 0) + 1
        effects = definition["upgrade_effects"] if card.upgraded else definition["effects"]
        main_targets = self._card_targets(definition["target"], target_id, actor)
        self.add_log(f"{actor.name} uses {definition['name']}.")
        for effect in effects:
            if self.state.phase != "combat":
                break
            if effect.get("condition_status") and not any(
                effect["condition_status"] in target.statuses for target in main_targets
            ):
                continue
            targets = self._effect_targets(effect.get("target"), main_targets, actor)
            self._apply_effect(actor, targets, effect)
        resonant_stacks = self.state.boons.get(actor.id, {}).get("resonant_circuit", 0)
        if self.state.effect_counters[combat_key] % 3 == 0 and resonant_stacks:
            self.state.energy += 2 if resonant_stacks >= 4 else 1
            self.add_log(f"{actor.name}'s Resonant Circuit returns energy.")
        moved = any(effect["op"] == "move" for effect in effects)
        counter_key = f"countercurrent:{actor.id}"
        if moved and not self.state.effect_counters.get(counter_key):
            counter_stacks = self.state.boons.get(actor.id, {}).get("countercurrent", 0)
            draws = 2 if counter_stacks >= 3 else int(bool(counter_stacks))
            if draws:
                self._draw(draws)
                self.state.effect_counters[counter_key] = 1
        reserve = round(self._item_effect_value("reserve_energy"))
        if self.state.energy == 0 and reserve and not self.state.effect_counters.get("reserve_energy"):
            self.state.energy += reserve
            self.state.effect_counters["reserve_energy"] = 1
            self.add_log(f"Reserve Cell restores {reserve} energy.")
        if not self.living_enemies() and self.state.phase == "combat":
            self._combat_victory()

    def _card_targets(self, target_type: str, target_id: str | None, actor: Actor) -> list[Actor]:
        if target_type == "self":
            return [actor]
        if target_type == "all_enemies":
            return self.living_enemies()
        if target_type == "all_allies":
            return self.living_heroes()
        return [self._actor(target_id or "")]

    def _effect_targets(self, override: str | None, main: list[Actor], actor: Actor) -> list[Actor]:
        if not override:
            return main
        if override == "self":
            return [actor]
        if override == "all_enemies":
            return self.living_enemies() if actor.side == "hero" else self.living_heroes()
        if override == "all_allies":
            return self.living_heroes() if actor.side == "hero" else self.living_enemies()
        return main

    def _apply_effect(self, actor: Actor, targets: list[Actor], effect: dict[str, Any]) -> None:
        op = effect["op"]
        amount = int(effect.get("amount", 0))
        if op == "draw":
            self._draw(amount)
        elif op == "discard":
            for _ in range(min(amount, len(self.state.hand))):
                self.state.discard_pile.append(self.state.hand.pop())
        elif op == "energy":
            self.state.energy += amount
        else:
            for target in list(targets):
                if not target.alive and op != "heal":
                    continue
                if op == "damage":
                    adjusted = amount
                    if effect.get("bonus_status") in target.statuses:
                        adjusted += int(effect.get("bonus", 0))
                    adjusted = self._outgoing_damage(actor, adjusted, target)
                    self._damage(target, adjusted, actor)
                elif op == "block":
                    multiplier = float(self._affliction_modifiers(target).get("block_mult", 1))
                    if target.side == "hero":
                        reduction = self._hero_effect_value(
                            target,
                            "curse",
                            "tremor_block_reduction",
                        )
                        multiplier *= 1 - reduction
                    multiplier = max(0.5, min(2.0, multiplier))
                    target.block += max(0, round(amount * multiplier))
                elif op == "heal":
                    self._heal(target, amount, actor)
                elif op == "stress" and target.side == "hero":
                    self._change_stress(target, amount)
                elif op == "move":
                    self._move(target, amount, actor)
                elif op == "guard" and target.side == "hero" and target.id != actor.id:
                    target.guarded_by = actor.id
                    target.guard_turns = amount
                elif op == "status":
                    self._add_status(target, effect["status"], amount)
                elif op == "cleanse":
                    for status in ("marked", "stun", "vulnerable", "weak", "wound"):
                        target.statuses.pop(status, None)

    def end_turn(self) -> None:
        if self.state.phase != "combat":
            raise RuleError("there is no combat turn to end")
        for card in self.state.hand:
            if card.card_id == "dread_forecast":
                self._trigger_curse_card(card, "curse_held_stress")
        self.state.discard_pile.extend(self.state.hand)
        self.state.hand = []
        for hero in self.living_heroes():
            if hero.statuses.get("stun"):
                hero.statuses["stun"] -= 1
                if hero.statuses["stun"] <= 0:
                    del hero.statuses["stun"]
            self._decay_statuses(hero)
        self._enemy_phase()
        if self.state.phase != "combat":
            return
        for hero in self.living_heroes():
            if hero.guard_turns:
                hero.guard_turns -= 1
                if hero.guard_turns <= 0:
                    hero.guarded_by = None
        self.state.round += 1
        self.state.intents = self._choose_intents()
        self._start_player_turn()

    def _choose_intents(self) -> list[dict[str, Any]]:
        intents = []
        for enemy in self.living_enemies():
            actions = self.catalog.enemies[enemy.definition_id or enemy.id]["actions"]
            action = self.rng.choices(actions, weights=[item.get("weight", 1) for item in actions], k=1)[0]
            intents.append({"enemy_rank": enemy.rank, "enemy_id": enemy.id, "action": action["name"]})
        return intents

    def _enemy_phase(self) -> None:
        for hero in self.living_heroes():
            self.state.effect_counters[f"enemy_hit:{hero.id}"] = 0
            self.state.effect_counters[f"adrenal:{hero.id}"] = 0
        intents = list(self.state.intents)
        for intent in intents:
            if self.state.phase != "combat":
                return
            enemy = next((item for item in self.living_enemies() if item.id == intent["enemy_id"]), None)
            if enemy is None:
                continue
            enemy.block = 0
            self._tick_wound(enemy)
            if enemy.hp <= 0:
                self._normalize_ranks("enemy")
                if not self.living_enemies():
                    self._combat_victory()
                    return
                continue
            if enemy.statuses.get("stun", 0):
                enemy.statuses["stun"] -= 1
                if enemy.statuses["stun"] <= 0:
                    del enemy.statuses["stun"]
                self.add_log(f"{enemy.name} is stunned.")
                self._decay_statuses(enemy)
                continue
            actions = self.catalog.enemies[enemy.definition_id or enemy.id]["actions"]
            action = next(item for item in actions if item["name"] == intent["action"])
            targets = self._enemy_targets(action["target"], enemy)
            self.add_log(f"{enemy.name} uses {action['name']}.")
            for effect in action["effects"]:
                self._apply_effect(enemy, self._effect_targets(effect.get("target"), targets, enemy), effect)
                if self.state.phase != "combat":
                    return
                if not self.living_enemies():
                    self._combat_victory()
                    return
            self._decay_statuses(enemy)

    def _enemy_targets(self, rule: str, actor: Actor) -> list[Actor]:
        heroes = self.living_heroes()
        enemies = self.living_enemies()
        if rule == "self":
            return [actor]
        if rule == "all_heroes":
            return heroes
        if rule == "front":
            return [heroes[0]]
        if rule == "back":
            return [heroes[-1]]
        if rule == "stressed":
            return [max(heroes, key=lambda item: item.stress)]
        if rule == "weakest_enemy":
            return [min(enemies, key=lambda item: item.hp / item.max_hp)]
        return [self.rng.choice(heroes)]

    def _draw(self, amount: int) -> None:
        for _ in range(max(0, amount)):
            if not self.state.draw_pile:
                if not self.state.discard_pile:
                    return
                self.state.draw_pile = self.state.discard_pile
                self.state.discard_pile = []
                self.rng.shuffle(self.state.draw_pile)
            card = self.state.draw_pile.pop()
            self.state.hand.append(card)
            if card.card_id in self.catalog.curses:
                key = self.catalog.curses[card.card_id]["effects"][0]["key"]
                if key != "curse_held_stress":
                    self._trigger_curse_card(card, key)

    def _trigger_curse_card(self, card: CardInstance, key: str) -> None:
        if not card.bound_hero_id:
            return
        hero = next((item for item in self.living_heroes() if item.id == card.bound_hero_id), None)
        if hero is None:
            return
        definition = self.catalog.curses[card.card_id]
        effect = next((item for item in definition["effects"] if item["key"] == key), None)
        if effect is None:
            return
        amount = round(self._stack_value(effect, 1))
        if key in {"curse_draw_stress", "curse_held_stress"}:
            self._change_stress(hero, amount)
        elif key == "curse_draw_wound":
            self._add_status(hero, "wound", amount)
        elif key == "curse_draw_energy":
            self.state.energy = max(0, self.state.energy - amount)
        elif key == "curse_draw_move":
            self._move(hero, amount)
        if key != "curse_dead_draw":
            self.add_log(f"{definition['name']} afflicts {hero.name}.")

    def _actor(self, actor_id: str) -> Actor:
        matches = [item for item in self.state.heroes + self.state.enemies if item.id == actor_id and item.alive]
        if not matches:
            raise RuleError(f"unknown or inactive actor: {actor_id}")
        return matches[0]

    def _outgoing_damage(self, actor: Actor, amount: int, target: Actor | None = None) -> int:
        multiplier = float(self._affliction_modifiers(actor).get("damage_mult", 1))
        if actor.statuses.get("weak"):
            multiplier *= 0.75
        if actor.statuses.get("focus"):
            multiplier *= 1.25
        if actor.side == "hero":
            multiplier *= 1 + self._hero_effect_value(actor, "boon", "damage_bonus")
            multiplier *= 1 + self._item_effect_value("damage_bonus")
            multiplier *= 1 - self._hero_effect_value(
                actor,
                "curse",
                "outgoing_damage_reduction",
            )
            if actor.stress >= 50:
                multiplier *= 1 + self._hero_effect_value(
                    actor,
                    "boon",
                    "stressed_damage_bonus",
                )
            if target and target.statuses.get("marked"):
                multiplier *= 1 + self._hero_effect_value(
                    actor,
                    "boon",
                    "marked_damage_bonus",
                )
                multiplier *= 1 + self._item_effect_value("marked_damage_bonus")
        multiplier = max(0.5, min(2.0, multiplier))
        return max(0, round(amount * multiplier))

    def _damage(self, target: Actor, amount: int, attacker: Actor | None = None) -> None:
        if target.side == "hero" and target.guarded_by:
            guard = next((item for item in self.living_heroes() if item.id == target.guarded_by), None)
            if guard and guard.id != target.id:
                self.add_log(f"{guard.name} intercepts the hit.")
                target = guard
        if attacker and attacker.side != target.side and target.statuses.get("dodge"):
            target.statuses.pop("dodge", None)
            self.add_log(f"{target.name} evades the hit.")
            return
        if target.statuses.get("vulnerable"):
            amount = round(amount * 1.5)
        if target.side == "hero":
            multiplier = 1 + self._hero_effect_value(target, "curse", "incoming_damage_bonus")
            multiplier *= 1 - self._item_effect_value("incoming_damage_reduction")
            amount = max(0, round(amount * max(0.5, min(2.0, multiplier))))
        absorbed = min(target.block, amount)
        target.block -= absorbed
        amount -= absorbed
        if target.side == "hero" and amount > 0:
            hit_key = f"enemy_hit:{target.id}"
            if not self.state.effect_counters.get(hit_key):
                amount = max(0, amount - round(self._item_effect_value("deflection")))
                self.state.effect_counters[hit_key] = 1
        if amount <= 0:
            return
        if target.side == "hero" and target.deaths_door:
            death_chance = self.catalog.balance["death_chance"] - self._hero_effect_value(
                target,
                "boon",
                "death_chance_reduction",
            )
            if self.rng.random() < max(0.05, death_chance):
                target.deaths_door = False
                target.hp = 0
                self.state.phase = "defeat"
                self.add_log(f"{target.name} dies. The expedition is lost.")
            else:
                self._change_stress(target, 10)
                self.add_log(f"{target.name} survives Death's Door.")
            return
        target.hp = max(0, target.hp - amount)
        if target.side == "hero" and target.hp == 0:
            second_wind_key = f"second_wind:{target.id}"
            second_wind = round(self._hero_effect_value(target, "boon", "second_wind"))
            if second_wind and not self.state.effect_counters.get(second_wind_key):
                target.hp = min(target.max_hp, second_wind)
                self.state.effect_counters[second_wind_key] = 1
                self.add_log(f"Second Wind restores {target.name} for {target.hp}.")
            else:
                target.deaths_door = True
                self._change_stress(target, 12)
                self.add_log(f"{target.name} is at Death's Door.")
        elif target.side == "enemy" and target.hp == 0:
            self.add_log(f"{target.name} is destroyed.")
            self._normalize_ranks("enemy")
        if target.side == "hero" and target.hp > 0 and attacker and attacker.side == "enemy":
            adrenal_key = f"adrenal:{target.id}"
            adrenal = round(self._hero_effect_value(target, "boon", "adrenal_block"))
            if adrenal and not self.state.effect_counters.get(adrenal_key):
                target.block += adrenal
                self.state.effect_counters[adrenal_key] = 1
        if (
            attacker
            and attacker.alive
            and target.alive
            and target.statuses.get("riposte")
            and attacker.side != target.side
        ):
            self.add_log(f"{target.name} answers with a riposte.")
            self._damage(attacker, 4)

    def _heal(self, target: Actor, amount: int, healer: Actor | None = None) -> None:
        multiplier = float(self._affliction_modifiers(target).get("healing_mult", 1))
        if target.side == "hero":
            multiplier *= 1 - self._hero_effect_value(target, "curse", "healing_reduction")
        if healer and healer.side == "hero":
            multiplier *= 1 + self._hero_effect_value(healer, "boon", "healing_bonus")
        multiplier = max(0.5, min(2.0, multiplier))
        target.hp = min(target.max_hp, target.hp + max(0, round(amount * multiplier)))
        if target.hp > 0:
            target.deaths_door = False
        if healer and healer.side == "hero" and target.side == "hero" and healer.id != target.id:
            mercy = round(self._hero_effect_value(healer, "boon", "mercy_block"))
            if mercy:
                target.block += mercy

    def _change_stress(self, target: Actor, amount: int) -> None:
        if amount > 0 and target.side == "hero":
            multiplier = 1 + self._hero_effect_value(target, "curse", "stress_bonus")
            multiplier *= 1 - self._hero_effect_value(target, "boon", "stress_reduction")
            multiplier *= 1 - self._item_effect_value("stress_reduction")
            amount = max(0, round(amount * max(0.4, min(2.0, multiplier))))
        target.stress = max(0, target.stress + amount)
        limit = self.catalog.balance.get("stress_limit", 100)
        if target.stress < limit:
            return
        if target.affliction:
            target.stress = 50
            target.hp = 0
            target.deaths_door = True
            self.add_log(f"{target.name} collapses at Death's Door.")
        else:
            target.stress = 50
            target.affliction = self.rng.choice(list(self.catalog.afflictions))
            name = self.catalog.afflictions[target.affliction]["name"]
            self.add_log(f"{target.name} becomes {name}.")

    def _affliction_modifiers(self, actor: Actor) -> dict[str, Any]:
        if not actor.affliction:
            return {}
        return self.catalog.afflictions[actor.affliction].get("modifiers", {})

    def _tick_wound(self, actor: Actor) -> None:
        if actor.statuses.get("wound"):
            self._damage(actor, 2)

    def _add_status(self, target: Actor, status: str, amount: int) -> None:
        if status == "wound" and target.side == "hero":
            reduction = self._hero_effect_value(target, "boon", "wound_reduction")
            reduction += self._item_effect_value("wound_reduction")
            amount = max(0, amount - round(reduction))
        if amount:
            target.statuses[status] = max(target.statuses.get(status, 0), amount)

    def _decay_statuses(self, actor: Actor) -> None:
        for status in list(actor.statuses):
            if status == "stun":
                continue
            actor.statuses[status] -= 1
            if actor.statuses[status] <= 0:
                del actor.statuses[status]

    def _move(self, actor: Actor, amount: int, source: Actor | None = None) -> None:
        if source and source.side != actor.side and actor.side == "hero":
            direction = 1 if amount > 0 else -1
            distance = abs(amount)
            distance += round(self._hero_effect_value(actor, "curse", "forced_move_bonus"))
            distance -= round(self._hero_effect_value(actor, "boon", "forced_move_reduction"))
            amount = direction * max(0, distance)
        party = self.state.heroes if actor.side == "hero" else self.state.enemies
        for _ in range(abs(amount)):
            direction = 1 if amount > 0 else -1
            next_rank = actor.rank + direction
            if next_rank not in range(1, 5):
                break
            occupant = next((item for item in party if item.alive and item.rank == next_rank), None)
            old_rank = actor.rank
            actor.rank = next_rank
            if occupant:
                occupant.rank = old_rank

    def _normalize_ranks(self, side: str) -> None:
        party = self.state.heroes if side == "hero" else self.state.enemies
        for rank, actor in enumerate(sorted((item for item in party if item.alive), key=lambda item: item.rank), 1):
            actor.rank = rank

    def _combat_victory(self) -> None:
        kind = self.state.combat_kind
        for hero in self.living_heroes():
            healing = round(self._hero_effect_value(hero, "boon", "combat_victory_heal"))
            if healing:
                self._heal(hero, healing)
        auto_suture = round(self._item_effect_value("combat_victory_heal"))
        if auto_suture and self.living_heroes():
            target = min(self.living_heroes(), key=lambda actor: actor.hp / actor.max_hp)
            self._heal(target, auto_suture)
        active_patrol = next(
            (patrol for patrol in self.state.patrols if patrol.id == self.state.active_patrol_id),
            None,
        )
        if active_patrol:
            active_patrol.active = False
            self.state.current_room = active_patrol.room_id
            self.room().resolved = True
        self.state.active_patrol_id = None
        self.state.hand = []
        self.state.draw_pile = []
        self.state.discard_pile = []
        self.state.intents = []
        if kind == "boss":
            self.room().resolved = True
            self.state.phase = "victory"
            self.add_log("The Overseer falls silent. Evacuation is possible.")
            return
        if kind != "ambush":
            self.room().resolved = True
        count = 4 if self.state.light < self.catalog.balance["low_light_threshold"] else 3
        count += round(self._item_effect_value("reward_choices"))
        active_heroes = {hero.id for hero in self.state.heroes}
        pool = [card_id for card_id, card in self.catalog.cards.items() if card["hero"] in active_heroes]
        self.state.rewards = self.rng.sample(pool, k=min(count, len(pool)))
        self.state.phase = "reward"
        self.add_log("Combat won. Choose a recovered technique.")

    def choose_reward(self, index: int | None) -> None:
        if self.state.phase != "reward":
            raise RuleError("there is no reward to choose")
        if index is not None:
            if not 0 <= index < len(self.state.rewards):
                raise RuleError("invalid reward")
            self.state.deck.append(CardInstance(self.state.rewards[index]))
            self.add_log(f"Added {self.catalog.cards[self.state.rewards[index]]['name']} to the deck.")
        self.state.rewards = []
        self.state.phase = "exploration"
        self._resolve_exploration_tile()

    def choose_event(self, index: int) -> None:
        if self.state.phase != "event" or not self.state.current_event:
            raise RuleError("there is no event choice to make")
        event = self.catalog.events[self.state.current_event]
        if not 0 <= index < len(event["choices"]):
            raise RuleError("invalid event choice")
        choice = event["choices"][index]
        cost = choice.get("cost_supplies", 0)
        if cost > self.state.supplies:
            raise RuleError("not enough supplies")
        self.state.supplies -= cost
        grant_reward = False
        for effect in choice["effects"]:
            op, amount = effect["op"], int(effect.get("amount", 0))
            if op == "light":
                self.state.light = min(100, max(0, self.state.light + amount))
            elif op == "supplies":
                self.state.supplies = max(0, self.state.supplies + amount)
            elif op == "heal_all":
                for hero in self.living_heroes():
                    self._heal(hero, amount)
            elif op == "stress_all":
                for hero in self.living_heroes():
                    self._change_stress(hero, amount)
            elif op == "damage_random":
                self._damage(self.rng.choice(self.living_heroes()), amount)
            elif op == "card_reward":
                grant_reward = True
        self.room().resolved = True
        self.state.current_event = None
        self.add_log(f"Event resolved: {choice['label']}.")
        if self.state.phase == "defeat":
            return
        if grant_reward:
            active_heroes = {hero.id for hero in self.state.heroes}
            pool = [card_id for card_id, card in self.catalog.cards.items() if card["hero"] in active_heroes]
            count = 3 + round(self._item_effect_value("reward_choices"))
            self.state.rewards = self.rng.sample(pool, k=min(count, len(pool)))
            self.state.phase = "reward"
        else:
            self.state.phase = "exploration"

    def _decrement_curse(self, hero_id: str, curse_id: str) -> None:
        owned = self.state.curses.get(hero_id, {})
        if owned.get(curse_id, 0) <= 0:
            raise RuleError("that curse is not present")
        owned[curse_id] -= 1
        if owned[curse_id] == 0:
            del owned[curse_id]
        if not owned:
            self.state.curses.pop(hero_id, None)

    def service(
        self,
        action: str,
        card_index: int | None = None,
        *,
        hero_id: str | None = None,
        curse_id: str | None = None,
    ) -> None:
        if self.state.phase != "service":
            raise RuleError("no facility is available")
        if action == "recover" and self.state.service_type == "camp":
            for hero in self.living_heroes():
                self._heal(hero, 7)
                self._change_stress(hero, -10)
            self.add_log("The crew rests behind a welded door.")
        elif action == "treat" and self.state.service_type == "camp":
            if self.state.supplies < 2:
                raise RuleError("curse treatment requires 2 supplies")
            if hero_id is None or curse_id is None:
                raise RuleError("choose a curse to treat")
            if curse_id not in self.catalog.curses:
                raise RuleError("choose a known curse to treat")
            card_index = None
            if self.catalog.curses[curse_id]["kind"] == "card":
                card_index = next(
                    (
                        index
                        for index, card in enumerate(self.state.deck)
                        if card.card_id == curse_id and card.bound_hero_id == hero_id
                    ),
                    None,
                )
                if card_index is None:
                    raise RuleError("the bound curse card is missing from the deck")
            self._decrement_curse(hero_id, curse_id)
            if card_index is not None:
                self.state.deck.pop(card_index)
            self.state.supplies -= 2
            hero = self._actor(hero_id)
            self.add_log(f"Treated {self.catalog.curses[curse_id]['name']} on {hero.name}.")
        elif action == "upgrade":
            if card_index is None or not 0 <= card_index < len(self.state.deck):
                raise RuleError("choose a card to upgrade")
            if self.state.deck[card_index].card_id in self.catalog.curses:
                raise RuleError("curse cards cannot be upgraded")
            if self.state.deck[card_index].upgraded:
                raise RuleError("that card is already upgraded")
            self.state.deck[card_index].upgraded = True
            self.add_log(f"Upgraded {self.catalog.cards[self.state.deck[card_index].card_id]['name']}.")
        elif action == "remove":
            if len(self.state.deck) <= 12:
                raise RuleError("the deck cannot contain fewer than 12 cards")
            if card_index is None or not 0 <= card_index < len(self.state.deck):
                raise RuleError("choose a card to remove")
            card = self.state.deck.pop(card_index)
            if card.card_id in self.catalog.curses:
                if card.bound_hero_id is None:
                    raise RuleError("curse card is missing its bound hero")
                self._decrement_curse(card.bound_hero_id, card.card_id)
            self.add_log(f"Removed {self.card_definition(card)['name']}.")
        else:
            raise RuleError("that service is not available here")
        self.room().resolved = True
        self.state.service_type = None
        self.state.phase = "exploration"
