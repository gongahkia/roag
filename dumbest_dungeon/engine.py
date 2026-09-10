"""Deterministic game rules with no terminal dependencies."""

from __future__ import annotations

import hashlib
import random
from collections import Counter, deque
from contextlib import contextmanager
from dataclasses import asdict, dataclass, field, replace
from fractions import Fraction
from heapq import heappop, heappush
from typing import Any, Callable

from .content import (
    CARD_STATUSES,
    DOCTRINE_TRIGGER_CONTRACTS,
    Catalog,
    load_legacy_catalog,
    load_rules,
)
from .acquisition import Lane, eligible_techniques
from .director import DirectorProfile, director_profile
from .manifest import canonical_bytes
from .migrations import MigrationError, migrate_run
from .versions import RUN_SAVE_SCHEMA
from .telemetry import RunLedger
from .resolution import Event, EventQueue, Listener, Payload
from .combat_triggers import (
    ADRENAL,
    CARD_TRIGGERS,
    CURSE_TRIGGERS,
    DEATH_SURGE,
    MERCY,
    REINFORCEMENT_CALL,
    REGISTERED,
    RIME_SHELL,
    RIPOSTE,
    SPORE_LINK,
    THIRD_BELL,
)
from .contracts import Opcode
from .passives import persistent_effect, trigger_disclosure
from .pressure import PressureSource, action_price, pressure_band, pressure_status
from .triggers import EventType, Phase


OPENING_MUTATION_EFFECTS = {
    "opening_front_block",
    "guard_rear",
    "mark_weakest",
    "dodge_rear",
    "riposte_front",
    "focus_striker",
    "shove_front_crew",
}
ACTIVE_MUTATION_EFFECTS = OPENING_MUTATION_EFFECTS | {
    "heal_weakest_round",
    "react_third_card",
    "surge_ally_death",
    "cleanse_round",
    "resist_first_stun",
    "wound_on_ally_death",
    "pull_crew_round",
    "wide_wound_round",
    "reinforce_once",
}


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
    last_action: str | None = None
    action_repeats: int = 0

    @property
    def alive(self) -> bool:
        return self.hp > 0 or (self.side == "hero" and self.deaths_door)


@dataclass
class CardInstance:
    card_id: str
    upgraded: bool = False
    bound_hero_id: str | None = None
    copy_id: int = 0
    mastery: str | None = None
    infusion_id: str | None = None


@dataclass
class Room:
    id: int
    name: str
    kind: str
    neighbors: list[int]
    resolved: bool = False
    visited: bool = False
    content_id: str | None = None
    biome_id: str = "derelict"
    enemy_ids: list[str] = field(default_factory=list)
    encounter_plan: str = "none"


@dataclass
class Patrol:
    id: str
    room_id: int
    encounter_id: str
    x: int
    y: int
    active: bool = True
    doctrine: str = "roam"
    route: list[list[int]] = field(default_factory=list)
    route_index: int = 0
    route_direction: int = 1
    alert: int = 0


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
class BiomeHazard:
    id: str
    biome_id: str
    x: int
    y: int
    triggered: bool = False
    cells: list[list[int]] = field(default_factory=list)
    triggered_cells: list[list[int]] = field(default_factory=list)
    active: bool = True
    suppressed_by: str | None = None


@dataclass
class BiomeFacility:
    id: str
    definition_id: str
    biome_id: str
    x: int
    y: int
    used: bool = False
    outcome: str | None = None


@dataclass
class AccessObjective:
    id: str
    biome_id: str
    x: int
    y: int
    completed: bool = False
    approach_sites: dict[str, list[list[int]]] = field(default_factory=dict)
    approach: str | None = None
    stage: int = 0
    outcome: str | None = None
    facts: dict[str, Any] = field(default_factory=dict)


@dataclass
class Landmark:
    id: str
    template_id: str
    biome_id: str
    x: int
    y: int
    cells: list[list[int]]
    discovered: bool = True
    state: str = "intact"


@dataclass
class GameState:
    seed: int
    phase: str
    heroes: list[Actor]
    deck: list[CardInstance]
    rooms: list[Room]
    world_tiles: list[str]
    world_id: str
    biome_ids: list[str]
    room_positions: list[list[int]]
    next_card_copy_id: int = 1
    hub_selection: list[str] = field(default_factory=list)
    hub_loadouts: dict[str, str] = field(default_factory=dict)
    doctrine_id: str | None = None
    tutorial: bool = False
    tutorial_stage: int = 0
    current_room: int = 0
    party_x: int = 5
    party_y: int = 17
    exploration_steps: int = 0
    patrols: list[Patrol] = field(default_factory=list)
    active_patrol_id: str | None = None
    pickups: list[EffectPickup] = field(default_factory=list)
    current_pickup_id: str | None = None
    hazards: list[BiomeHazard] = field(default_factory=list)
    current_hazard_id: str | None = None
    facilities: list[BiomeFacility] = field(default_factory=list)
    current_facility_id: str | None = None
    objectives: list[AccessObjective] = field(default_factory=list)
    landmarks: list[Landmark] = field(default_factory=list)
    current_objective_id: str | None = None
    required_objectives: int = 2
    known_feature_ids: list[str] = field(default_factory=list)
    travel_ticks: int = 0
    pressure: int = 0
    pressure_recent: list[dict[str, Any]] = field(default_factory=list)
    pressure_incomplete_before_tick: int | None = None
    encounter_pressure: int | None = None
    encounter_modules: list[str] = field(default_factory=list)
    reinforcement_tickets: int = 0
    reinforcement_reserve_id: str | None = None
    pending_opening_hand: int = 0
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
    ledger: RunLedger = field(default_factory=RunLedger)


def _tuples(value: Any) -> Any:
    if isinstance(value, list):
        return tuple(_tuples(item) for item in value)
    return value


WORLD_WIDTH = 117
WORLD_HEIGHT = 35
BRANCHING_POSITIONS = {
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
BRANCHING_EDGES = {
    0: [1], 1: [0, 2, 3, 4], 2: [1], 3: [1],
    4: [1, 5, 6, 7], 5: [4], 6: [4],
    7: [4, 8, 9, 10], 8: [7], 9: [7],
    10: [7, 11], 11: [10],
}
def _edge_map(*pairs: tuple[int, int]) -> dict[int, list[int]]:
    result = {room_id: [] for room_id in range(12)}
    for left, right in pairs:
        result[left].append(right)
        result[right].append(left)
    return result


WORLD_LAYOUTS: dict[str, tuple[dict[int, tuple[int, int]], dict[int, list[int]]]] = {
    "branching": (BRANCHING_POSITIONS, BRANCHING_EDGES),
    "spine": (
        {
            0: (5, 17), 1: (16, 7), 2: (24, 17), 3: (35, 27),
            4: (43, 17), 5: (54, 7), 6: (62, 17), 7: (73, 27),
            8: (81, 17), 9: (92, 7), 10: (103, 17), 11: (111, 17),
        },
        _edge_map(
            (0, 2), (2, 4), (4, 6), (6, 8), (8, 10), (10, 11),
            (0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 6),
            (6, 7), (7, 8), (8, 9), (9, 10),
        ),
    ),
    "ring": (
        {
            0: (5, 17), 1: (18, 8), 2: (38, 5), 3: (58, 7), 4: (78, 5),
            5: (98, 8), 6: (58, 17), 7: (18, 27), 8: (38, 30),
            9: (78, 30), 10: (98, 27), 11: (111, 17),
        },
        _edge_map(
            (0, 1), (1, 2), (2, 3), (3, 4), (4, 5), (5, 11),
            (0, 7), (7, 8), (8, 6), (6, 9), (9, 10), (10, 11),
            (2, 6), (4, 6),
        ),
    ),
    "clusters": (
        {
            0: (5, 17), 1: (18, 17), 2: (27, 8), 3: (29, 25), 4: (45, 17),
            5: (58, 9), 6: (61, 26), 7: (75, 17), 8: (87, 8),
            9: (90, 27), 10: (101, 17), 11: (111, 17),
        },
        _edge_map(
            (0, 1), (1, 2), (2, 3), (3, 0),
            (2, 4),
            (4, 5), (5, 7), (7, 6), (6, 4),
            (7, 8),
            (8, 10), (10, 11), (11, 9), (9, 8),
        ),
    ),
    "zigzag": (
        {
            0: (5, 17), 1: (15, 8), 2: (25, 27), 3: (35, 7), 4: (45, 28),
            5: (55, 9), 6: (65, 26), 7: (75, 7), 8: (85, 28),
            9: (95, 9), 10: (104, 25), 11: (111, 17),
        },
        _edge_map(
            *((room_id, room_id + 1) for room_id in range(11)),
            (1, 3), (3, 5), (5, 7), (7, 9), (9, 11),
            (2, 4), (4, 6), (6, 8), (8, 10),
        ),
    ),
    "fracture": (
        {
            0: (5, 17), 1: (20, 17), 2: (35, 6), 3: (35, 28), 4: (52, 17),
            5: (66, 5), 6: (66, 17), 7: (66, 29), 8: (83, 8),
            9: (83, 26), 10: (101, 17), 11: (111, 17),
        },
        _edge_map(
            (0, 1), (1, 2), (1, 3), (2, 4), (3, 4), (4, 5), (4, 6),
            (4, 7), (5, 8), (6, 8), (6, 9), (7, 9), (8, 10), (9, 10), (10, 11),
            (2, 5), (3, 7),
        ),
    ),
}
WALKABLE_TILES = frozenset(".,=~_\";:`'%-o")
VALID_WORLD_TILES = frozenset(" #O") | WALKABLE_TILES


def _build_world(
    rng: random.Random,
    positions: dict[int, tuple[int, int]],
    edges: dict[int, list[int]],
    room_biomes: dict[int, str],
    catalog: Catalog,
    layout: str,
) -> list[str]:
    floor: set[tuple[int, int]] = set()
    corridors: set[tuple[int, int]] = set()
    cluster_transfers: set[tuple[int, int]] = set()
    room_bounds: dict[int, tuple[int, int]] = {}

    def carve_segment(
        start: tuple[int, int],
        end: tuple[int, int],
        *,
        broad: bool = False,
        tracked: set[tuple[int, int]] | None = None,
    ) -> None:
        def carve(position: tuple[int, int]) -> None:
            floor.add(position)
            corridors.add(position)
            if tracked is not None:
                tracked.add(position)

        start_x, start_y = start
        end_x, end_y = end
        if start_y == end_y:
            for x in range(min(start_x, end_x), max(start_x, end_x) + 1):
                carve((x, start_y))
                if broad and start_y + 1 < WORLD_HEIGHT - 1:
                    carve((x, start_y + 1))
        elif start_x == end_x:
            for y in range(min(start_y, end_y), max(start_y, end_y) + 1):
                carve((start_x, y))
                if broad and start_x + 1 < WORLD_WIDTH - 1:
                    carve((start_x + 1, y))
        else:
            raise ValueError("world segments must be orthogonal")

    for room_id, (center_x, center_y) in positions.items():
        half_width = rng.randint(3, 5)
        half_height = rng.randint(1, 3)
        room_bounds[room_id] = (half_width, half_height)
        for y in range(center_y - half_height, center_y + half_height + 1):
            for x in range(center_x - half_width, center_x + half_width + 1):
                floor.add((x, y))

    for room_id, neighbors in edges.items():
        start_x, start_y = positions[room_id]
        for neighbor in neighbors:
            if neighbor < room_id:
                continue
            end_x, end_y = positions[neighbor]
            broad = rng.random() < 0.28
            tracked = (
                cluster_transfers
                if layout == "clusters" and {room_id, neighbor} in ({2, 4}, {7, 8})
                else None
            )
            if start_y == end_y:
                direction = 1 if end_x > start_x else -1
                first_x = start_x + direction * max(2, abs(end_x - start_x) // 3)
                second_x = end_x - direction * max(2, abs(end_x - start_x) // 3)
                detour_y = max(2, min(WORLD_HEIGHT - 3, start_y + rng.choice((-2, 2))))
                carve_segment((start_x, start_y), (first_x, start_y), broad=broad, tracked=tracked)
                carve_segment((first_x, start_y), (first_x, detour_y), broad=broad, tracked=tracked)
                carve_segment((first_x, detour_y), (second_x, detour_y), broad=broad, tracked=tracked)
                carve_segment((second_x, detour_y), (second_x, end_y), broad=broad, tracked=tracked)
                carve_segment((second_x, end_y), (end_x, end_y), broad=broad, tracked=tracked)
            else:
                middle_x = max(
                    2,
                    min(WORLD_WIDTH - 3, (start_x + end_x) // 2 + rng.randint(-3, 3)),
                )
                carve_segment((start_x, start_y), (middle_x, start_y), broad=broad, tracked=tracked)
                carve_segment((middle_x, start_y), (middle_x, end_y), broad=broad, tracked=tracked)
                carve_segment((middle_x, end_y), (end_x, end_y), broad=broad, tracked=tracked)

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
        center_x, center_y = positions[room_id]
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

    start = positions[0]
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
    cell_biomes: dict[tuple[int, int], str] = {}
    for x, y in floor:
        nearest_room = min(
            positions,
            key=lambda room_id: abs(x - positions[room_id][0]) + abs(y - positions[room_id][1]),
        )
        biome_id = room_biomes[nearest_room]
        cell_biomes[(x, y)] = biome_id
        cells[y][x] = catalog.biomes[biome_id]["glyph"]
    patterns = {
        definition["biome"]: definition
        for definition in catalog.terrain_patterns.values()
    }
    anchors = set(positions.values())
    for biome_id in sorted(set(room_biomes.values())):
        pattern = patterns[biome_id]
        eligible = sorted(
            position
            for position in floor - anchors
            if cell_biomes[position] == biome_id
        )
        if not eligible:
            continue
        if pattern["mode"] == "channels":
            patterned = [position for position in eligible if position in corridors]
        elif pattern["mode"] == "bands":
            vertical = rng.random() < 0.5
            offset = rng.randrange(7)
            patterned = [
                position
                for position in eligible
                if ((position[0] if vertical else position[1]) + offset) % 7 in {0, 1}
            ]
        else:
            patterned_set: set[tuple[int, int]] = set()
            centers = rng.sample(eligible, min(5, len(eligible)))
            for center_x, center_y in centers:
                patterned_set.update(
                    (x, y)
                    for y in range(center_y - 1, center_y + 2)
                    for x in range(center_x - 1, center_x + 2)
                    if (x, y) in floor
                    and (x, y) not in anchors
                    and cell_biomes[(x, y)] == biome_id
                    and rng.random() < 0.7
                )
            patterned = sorted(patterned_set)
        for x, y in patterned:
            cells[y][x] = pattern["glyph"]
    for x, y in cluster_transfers - anchors:
        cells[y][x] = "="
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


def _validate_world(tiles: Any, positions: dict[int, tuple[int, int]]) -> None:
    if (
        not isinstance(tiles, list)
        or len(tiles) != WORLD_HEIGHT
        or any(not isinstance(row, str) or len(row) != WORLD_WIDTH for row in tiles)
        or any(character not in VALID_WORLD_TILES for row in tiles for character in row)
    ):
        raise RuleError("save contains malformed world terrain")
    if any(tiles[y][x] not in WALKABLE_TILES for x, y in positions.values()):
        raise RuleError("save terrain blocks a compartment anchor")


class GameEngine:
    """Owns the mutable run and its seeded pseudo-random stream."""

    SAVE_VERSION = RUN_SAVE_SCHEMA
    TUTORIAL_SEED = 1
    ENCOUNTER_PLANS = {"none", "pressure", "disrupt", "screen", "sustain", "combo", "overseer"}

    def __init__(self, catalog: Catalog, state: GameState, rng: random.Random):
        self.catalog = catalog
        self.state = state
        self.rng = rng
        self._source_id: str | None = None
        self.resolution = EventQueue()

    def _new_card(
        self,
        card_id: str,
        upgraded: bool = False,
        bound_hero_id: str | None = None,
    ) -> CardInstance:
        copy_id = self.state.next_card_copy_id
        self.state.next_card_copy_id += 1
        return CardInstance(card_id, upgraded, bound_hero_id, copy_id)

    @staticmethod
    def _clone_card(card: CardInstance) -> CardInstance:
        return CardInstance(
            card.card_id,
            card.upgraded,
            card.bound_hero_id,
            card.copy_id,
            card.mastery,
            card.infusion_id,
        )

    @classmethod
    def new(cls, catalog: Catalog, seed: int, *, start_in_hub: bool = False) -> GameEngine:
        rng = random.Random(seed)
        world_id = rng.choice(list(catalog.worlds))
        world = catalog.worlds[world_id]
        positions, edges = WORLD_LAYOUTS[world["layout"]]
        remaining_biomes = list(catalog.biomes)
        biome_sequence: list[str] = []
        while len(biome_sequence) < 4:
            choice = rng.choices(
                remaining_biomes,
                weights=[3 if biome_id in world["biomes"] else 1 for biome_id in remaining_biomes],
                k=1,
            )[0]
            biome_sequence.append(choice)
            remaining_biomes.remove(choice)
        middle_biomes = [biome_sequence[index % len(biome_sequence)] for index in range(10)]
        rng.shuffle(middle_biomes)
        room_biomes = {0: biome_sequence[0], 11: biome_sequence[-1]}
        room_biomes.update({room_id: middle_biomes[room_id - 1] for room_id in range(1, 11)})
        rooms = cls._generate_rooms(catalog, rng, edges, room_biomes)
        world_tiles = _build_world(
            random.Random(seed ^ 0x4F5249534F4E),
            positions,
            edges,
            room_biomes,
            catalog,
            world["layout"],
        )
        default_party = list(catalog.heroes)[:4]
        state = GameState(
            seed=seed,
            phase="hub",
            heroes=[],
            deck=[],
            rooms=rooms,
            world_tiles=world_tiles,
            world_id=world_id,
            biome_ids=biome_sequence,
            room_positions=[list(positions[room_id]) for room_id in range(12)],
            hub_selection=default_party,
            party_x=positions[0][0],
            party_y=positions[0][1],
            light=catalog.balance.get("starting_light", 100),
            supplies=catalog.balance.get("starting_supplies", 4),
            log=[f"Crew manifest opened above {world['name']}."],
        )
        engine = cls(catalog, state, rng)
        state.pickups = engine._generate_pickups(random.Random(seed ^ 0x5049434B5550))
        state.objectives = engine._generate_objectives(random.Random(seed ^ 0x4F424A454354))
        state.landmarks = engine._generate_landmarks()
        state.hazards = engine._generate_hazards(random.Random(seed ^ 0x48415A415244))
        state.facilities = engine._generate_facilities(random.Random(seed ^ 0x464143494C495459))
        engine._update_perception()
        if not start_in_hub:
            engine.begin_expedition()
        return engine

    @classmethod
    def tutorial(cls, catalog: Catalog) -> GameEngine:
        engine = cls.new(catalog, cls.TUTORIAL_SEED, start_in_hub=True)
        engine.select_curated_squad("bulkhead_basics")
        engine.begin_expedition()
        state = engine.state
        state.tutorial = True
        state.tutorial_stage = 0
        training = next(
            (
                patrol
                for patrol in state.patrols
                if engine.room(patrol.room_id).kind == "fight"
                and engine.room(patrol.room_id).biome_id == "reactor"
            ),
            None,
        )
        if training is None:
            raise RuleError("tutorial seed has no reactor training site")
        encounter = catalog.encounters["reactor_rod_rear"]
        room = engine.room(training.room_id)
        training.encounter_id = encounter["id"]
        room.content_id = encounter["id"]
        room.enemy_ids = list(encounter["enemies"])
        room.encounter_plan = engine._formation_plan(catalog, room.enemy_ids)
        for patrol in state.patrols:
            patrol.active = patrol.id == training.id
        path = engine._find_path(
            (state.party_x, state.party_y),
            engine.room_position(training.room_id),
        )
        if len(path) < 4:
            raise RuleError("tutorial seed has no usable training route")
        training.x, training.y = path[3]
        state.log = ["Training signal acquired. Confirm the highlighted contact route."]
        return engine

    def tutorial_destination(self) -> tuple[int, int] | None:
        if not self.state.tutorial or self.state.tutorial_stage > 1:
            return None
        patrol = next((item for item in self.state.patrols if item.active), None)
        return (patrol.x, patrol.y) if patrol else None

    def advance_tutorial(self, expected: int, destination: int) -> None:
        if not self.state.tutorial or self.state.tutorial_stage != expected:
            raise RuleError("the tutorial is not at that lesson")
        self.state.tutorial_stage = destination

    def complete_tutorial(self) -> None:
        if not self.state.tutorial or self.state.tutorial_stage != 9:
            raise RuleError("the tutorial lesson is not complete")
        self.state.tutorial_stage = 10
        self.state.phase = "tutorial_complete"
        self.add_log("Training expedition complete. No run progress was retained.")

    def toggle_hub_crew(self, hero_id: str) -> None:
        if self.state.phase != "hub" or hero_id not in self.catalog.heroes:
            raise RuleError("that crew manifest entry is unavailable")
        if hero_id in self.state.hub_selection:
            self.state.hub_selection.remove(hero_id)
        elif len(self.state.hub_selection) < 4:
            self.state.hub_selection.append(hero_id)
        else:
            raise RuleError("the expedition can carry only four crew members")

    def select_curated_squad(self, squad_id: str) -> None:
        if self.state.phase != "hub" or squad_id not in self.catalog.squads:
            raise RuleError("that curated squad is unavailable")
        self.state.hub_selection = list(self.catalog.squads[squad_id]["formation"])
        self.state.hub_loadouts = {}
        self.state.doctrine_id = None

    def starter_cards_for(self, hero_id: str) -> list[str]:
        if hero_id not in self.catalog.heroes:
            raise RuleError("that crew manifest entry is unavailable")
        loadout_id = self.state.hub_loadouts.get(hero_id)
        if loadout_id is None:
            return list(self.catalog.heroes[hero_id]["starter_deck"])
        loadout = self.catalog.loadouts.get(loadout_id)
        if loadout is None or loadout["hero"] != hero_id:
            raise RuleError("the selected loadout does not belong to that crew member")
        return list(loadout["cards"])

    def select_hub_loadout(self, hero_id: str, loadout_id: str | None) -> None:
        if self.state.phase != "hub" or hero_id not in self.state.hub_selection:
            raise RuleError("select that crew member before assigning a loadout")
        if loadout_id is None:
            self.state.hub_loadouts.pop(hero_id, None)
        else:
            loadout = self.catalog.loadouts.get(loadout_id)
            if loadout is None or loadout["hero"] != hero_id:
                raise RuleError("that advanced loadout is unavailable to this crew member")
            self.state.hub_loadouts[hero_id] = loadout_id
        if self.state.doctrine_id is not None and not self.doctrine_compatible(
            self.state.doctrine_id
        ):
            self.state.doctrine_id = None

    def party_tags_and_roles(self) -> tuple[set[str], set[str]]:
        tags = {
            tag
            for hero_id in self.state.hub_selection
            for card_id in self.starter_cards_for(hero_id)
            for tag in self.catalog.cards[card_id]["tags"]
        }
        roles = {
            self.catalog.heroes[hero_id]["combat_role"]
            for hero_id in self.state.hub_selection
        }
        return tags, roles

    def doctrine_compatible(self, doctrine_id: str) -> bool:
        doctrine = self.catalog.doctrines.get(doctrine_id)
        if doctrine is None or len(self.state.hub_selection) != 4:
            return False
        tags, roles = self.party_tags_and_roles()
        return set(doctrine["requires_tags"]) <= tags and set(
            doctrine["requires_roles"]
        ) <= roles

    def select_doctrine(self, doctrine_id: str | None) -> None:
        if self.state.phase != "hub":
            raise RuleError("doctrine is fixed after departure")
        if doctrine_id is not None and not self.doctrine_compatible(doctrine_id):
            raise RuleError("the selected party does not satisfy that doctrine")
        self.state.doctrine_id = doctrine_id

    def party_warnings(self, selection: list[str] | None = None) -> list[str]:
        selection = list(self.state.hub_selection if selection is None else selection)
        warnings = []
        for rank, hero_id in enumerate(selection, 1):
            if hero_id not in self.catalog.heroes:
                continue
            hero = self.catalog.heroes[hero_id]
            if rank not in hero["preferred_ranks"]:
                preferred = ",".join(str(value) for value in hero["preferred_ranks"])
                warnings.append(f"R{rank} {hero['role']} prefers R{preferred}.")
            playable = sum(
                rank in self.catalog.cards[card_id]["from_ranks"]
                for card_id in self.starter_cards_for(hero_id)
            )
            if playable < 2:
                warnings.append(
                    f"R{rank} leaves {hero['role']} with only {playable} playable starter."
                )
        if len(selection) == 4:
            roles = {
                self.catalog.heroes[hero_id]["combat_role"]
                for hero_id in selection
                if hero_id in self.catalog.heroes
            }
            if "defender" not in roles:
                warnings.append("No defender: direct pressure will be difficult to absorb.")
            if "support" not in roles:
                warnings.append("No support: health and stress damage may persist.")
        return warnings

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

    def _patrol_route(self, patrol: Patrol) -> list[list[int]]:
        room = self.room(patrol.room_id)
        home = self.room_position(room.id)
        targets: list[tuple[int, int]] = []
        if patrol.doctrine in {"circuit", "stalk", "migrate"}:
            landmark = next(
                (item for item in self.state.landmarks if item.biome_id == room.biome_id),
                None,
            )
            if landmark:
                targets.append((landmark.x, landmark.y))
        if patrol.doctrine in {"sweep", "migrate"}:
            facility = next(
                (item for item in self.state.facilities if item.biome_id == room.biome_id),
                None,
            )
            if facility:
                if patrol.doctrine == "sweep":
                    targets.insert(0, (facility.x, facility.y))
                else:
                    targets.append((facility.x, facility.y))
        route = [home]
        current = home
        for target in targets:
            segment = self._find_path(current, target)
            route.extend(segment)
            current = target
        return [list(position) for position in route]

    def begin_expedition(self) -> None:
        if self.state.phase != "hub":
            raise RuleError("the expedition has already departed")
        if len(self.state.hub_selection) != 4 or len(set(self.state.hub_selection)) != 4:
            raise RuleError("select exactly four unique crew members")
        if any(hero_id not in self.state.hub_selection for hero_id in self.state.hub_loadouts):
            raise RuleError("a selected loadout belongs to absent crew")
        if self.state.doctrine_id is not None and not self.doctrine_compatible(self.state.doctrine_id):
            raise RuleError("the selected party does not satisfy its doctrine")
        self.state.heroes = []
        self.state.deck = []
        self.state.next_card_copy_id = 1
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
            self.state.deck.extend(self._new_card(card_id) for card_id in self.starter_cards_for(hero_id))
        self.state.patrols = []
        for room in self.state.rooms:
            if room.kind not in {"fight", "elite", "boss"}:
                continue
            doctrine = self.biome_mechanics(room.biome_id)["patrol"]["behavior"]
            patrol = Patrol(
                id=f"patrol:{room.id}",
                room_id=room.id,
                encounter_id=room.content_id or "",
                x=self.room_position(room.id)[0],
                y=self.room_position(room.id)[1],
                active=room.kind != "boss",
                doctrine=doctrine,
            )
            patrol.route = self._patrol_route(patrol)
            self.state.patrols.append(patrol)
        self.state.phase = "exploration"
        world_name = self.catalog.worlds[self.state.world_id]["name"]
        self.state.log = [f"The threshold seals. {world_name} is no longer empty."]
        self.record("departure", "expedition", seed=self.state.seed, world=self.state.world_id,
                    layout=self.catalog.worlds[self.state.world_id]["layout"], biomes=self.state.biome_ids,
                    formation=[hero.id for hero in self.living_heroes()],
                    loadouts=dict(self.state.hub_loadouts), doctrine=self.state.doctrine_id,
                    deck=[asdict(card) for card in self.state.deck], manifest=self.catalog.manifest.snapshot())

    @staticmethod
    def _effect_targets_crew(action: dict[str, Any], effect: dict[str, Any]) -> bool:
        target = effect.get("target", action["target"])
        return target not in {"self", "weakest_ally", "weakest_enemy", "all_allies"}

    @classmethod
    def _definition_roles(cls, definition: dict[str, Any]) -> set[str]:
        roles: set[str] = set()
        for action in definition["actions"]:
            for effect in action["effects"]:
                targets_crew = cls._effect_targets_crew(action, effect)
                if effect["op"] == "damage" and targets_crew:
                    roles.add("striker")
                if targets_crew and effect["op"] in {"stress", "move", "status"}:
                    roles.add("controller")
                if not targets_crew and effect["op"] in {"heal", "block", "status"}:
                    roles.add("support")
                if not targets_crew and effect["op"] == "block":
                    roles.add("defender")
        if definition["max_hp"] >= 28:
            roles.add("defender")
        return roles or {"striker"}

    @classmethod
    def _definition_setup_statuses(cls, definition: dict[str, Any]) -> set[str]:
        return {
            effect["status"]
            for action in definition["actions"]
            for effect in action["effects"]
            if effect["op"] == "status" and cls._effect_targets_crew(action, effect)
        }

    @staticmethod
    def _definition_exploit_statuses(definition: dict[str, Any]) -> set[str]:
        return {
            effect["bonus_status"]
            for action in definition["actions"]
            for effect in action["effects"]
            if effect["op"] == "damage" and effect.get("bonus_status")
        }

    def enemy_roles(self, enemy_id: str) -> set[str]:
        return self._definition_roles(self.catalog.enemies[enemy_id])

    @classmethod
    def _formation_score(cls, catalog: Catalog, enemy_ids: list[str], midpoint: int) -> float:
        definitions = [catalog.enemies[enemy_id] for enemy_id in enemy_ids]
        member_roles = [cls._definition_roles(definition) for definition in definitions]
        setups = [cls._definition_setup_statuses(definition) for definition in definitions]
        exploits = [cls._definition_exploit_statuses(definition) for definition in definitions]
        total_hp = sum(definition["max_hp"] for definition in definitions)
        combined_roles = set().union(*member_roles)
        score = len(combined_roles) * 2.0 - abs(total_hp - midpoint) / 8
        score -= (len(enemy_ids) - len(set(enemy_ids))) * 1.2
        if "striker" not in combined_roles:
            score -= 4.0
        for index, roles in enumerate(member_roles):
            for other_index, other_roles in enumerate(member_roles):
                if index == other_index:
                    continue
                if "support" in roles and ({"striker", "defender"} & other_roles):
                    score += 0.75
                if setups[index] & exploits[other_index]:
                    score += 3.0
        return score

    @classmethod
    def _formation_plan(cls, catalog: Catalog, enemy_ids: list[str]) -> str:
        definitions = [catalog.enemies[enemy_id] for enemy_id in enemy_ids]
        roles = set().union(*(cls._definition_roles(definition) for definition in definitions))
        setups = set().union(*(cls._definition_setup_statuses(definition) for definition in definitions))
        exploits = set().union(*(cls._definition_exploit_statuses(definition) for definition in definitions))
        if setups & exploits:
            return "combo"
        if {"defender", "support"} <= roles and "striker" in roles:
            return "screen"
        if any(
            effect["op"] == "heal" and not cls._effect_targets_crew(action, effect)
            for definition in definitions
            for action in definition["actions"]
            for effect in action["effects"]
        ) and "striker" in roles:
            return "sustain"
        if {"controller", "striker"} <= roles:
            return "disrupt"
        return "pressure"

    @classmethod
    def _arrange_enemy_formation(
        cls,
        catalog: Catalog,
        rng: random.Random,
        enemy_ids: list[str],
    ) -> list[str]:
        formation_setups = set().union(
            *(cls._definition_setup_statuses(catalog.enemies[enemy_id]) for enemy_id in enemy_ids)
        )
        formation_exploits = set().union(
            *(cls._definition_exploit_statuses(catalog.enemies[enemy_id]) for enemy_id in enemy_ids)
        )

        def rank_priority(enemy_id: str) -> float:
            definition = catalog.enemies[enemy_id]
            roles = cls._definition_roles(definition)
            if "defender" in roles and "support" not in roles:
                base = 0.0
            elif "striker" in roles and "controller" not in roles:
                base = 1.0
            elif "controller" in roles and "support" not in roles:
                base = 2.0
            else:
                base = 3.0
            if cls._definition_setup_statuses(definition) & formation_exploits:
                base -= 1.5
            if cls._definition_exploit_statuses(definition) & formation_setups:
                base += 1.5
            return base + rng.uniform(-0.45, 0.45)

        ordered = sorted(enemy_ids, key=rank_priority)
        formation = rng.choices(
            ("screened", "flanking", "inverted"),
            weights=(6, 3, 1),
            k=1,
        )[0]
        if formation == "flanking" and len(ordered) >= 3:
            flank = next(
                (
                    index
                    for index in range(1, len(ordered))
                    if "striker" in cls._definition_roles(catalog.enemies[ordered[index]])
                ),
                1,
            )
            ordered.append(ordered.pop(flank))
        elif formation == "inverted":
            ordered.reverse()
        return ordered

    @classmethod
    def _compose_enemy_formation(
        cls,
        catalog: Catalog,
        rng: random.Random,
        biome_id: str,
        kind: str,
        encounter_id: str,
    ) -> list[str]:
        template = list(catalog.encounters[encounter_id]["enemies"])
        if kind == "boss":
            return template
        encounter_kind = "normal" if kind == "fight" else "elite"
        allowed_kinds = {"normal", "elite"} if encounter_kind == "elite" else {"normal"}
        weighted_pool = [
            enemy_id
            for encounter in catalog.encounters.values()
            if encounter["kind"] in allowed_kinds
            and biome_id in encounter.get("biomes", ["derelict"])
            for enemy_id in encounter["enemies"]
        ]
        minimum, maximum = (40, 60) if encounter_kind == "normal" else (62, 100)
        midpoint = (minimum + maximum) // 2
        candidates: dict[tuple[str, ...], float] = {}
        template_hp = sum(catalog.enemies[enemy_id]["max_hp"] for enemy_id in template)
        if minimum <= template_hp <= maximum:
            candidates[tuple(template)] = cls._formation_score(catalog, template, midpoint)
            if 2 <= len(template) <= 4 and rng.random() < 0.5:
                return cls._arrange_enemy_formation(catalog, rng, template)
        sizes = (2, 3, 4)
        weights = (4, 5, 1) if encounter_kind == "normal" else (1, 4, 5)
        for _ in range(120):
            size = rng.choices(sizes, weights=weights, k=1)[0]
            formation = [rng.choice(template if rng.random() < 0.7 else weighted_pool)]
            formation.extend(rng.choice(weighted_pool) for _ in range(size - 1))
            total_hp = sum(catalog.enemies[enemy_id]["max_hp"] for enemy_id in formation)
            if minimum <= total_hp <= maximum:
                candidates[tuple(formation)] = cls._formation_score(catalog, formation, midpoint)
        if not candidates:
            return cls._arrange_enemy_formation(catalog, rng, template)
        candidates_by_plan: dict[str, list[tuple[tuple[str, ...], float]]] = {}
        for enemy_ids, score in candidates.items():
            plan = cls._formation_plan(catalog, list(enemy_ids))
            candidates_by_plan.setdefault(plan, []).append((enemy_ids, score))
        plans = list(candidates_by_plan)
        plan = rng.choices(
            plans,
            weights=[
                {"combo": 4, "disrupt": 3, "screen": 3, "sustain": 2, "pressure": 1}[item]
                for item in plans
            ],
            k=1,
        )[0]
        planned_candidates = dict(candidates_by_plan[plan])
        best_score = max(planned_candidates.values())
        shortlist = [
            list(enemy_ids)
            for enemy_ids, score in planned_candidates.items()
            if score >= best_score - 2.0
        ]
        return cls._arrange_enemy_formation(catalog, rng, rng.choice(shortlist))

    @classmethod
    def _generate_rooms(
        cls,
        catalog: Catalog,
        rng: random.Random,
        edges: dict[int, list[int]],
        room_biomes: dict[int, str],
    ) -> list[Room]:
        kinds = ["fight"] * 4 + ["event"] * 2 + ["camp", "upgrade", "elite", "cache"]
        rng.shuffle(kinds)
        labels = {
            "fight": "Contested Deck",
            "event": "Unstable Compartment",
            "camp": "Sealed Crew Quarters",
            "upgrade": "Machine Workshop",
            "elite": "Heavy Motion Contact",
            "cache": "Emergency Stores",
        }
        start_biome = room_biomes[0]
        rooms = [Room(0, "Arrival Threshold", "start", edges[0], True, True, biome_id=start_biome)]
        used_events: set[str] = set()
        used_encounters: set[str] = set()
        for room_id, kind in enumerate(kinds, 1):
            biome_id = room_biomes[room_id]
            content_id = None
            if kind in {"fight", "elite"}:
                encounter_kind = "normal" if kind == "fight" else "elite"
                candidates = [
                    item["id"]
                    for item in catalog.encounters.values()
                    if item["kind"] == encounter_kind
                    and biome_id in item.get("biomes", ["derelict"])
                ]
                unused = [encounter_id for encounter_id in candidates if encounter_id not in used_encounters]
                content_id = rng.choice(unused or candidates)
                used_encounters.add(content_id)
            elif kind == "event":
                candidates = [
                    event["id"]
                    for event in catalog.events.values()
                    if biome_id in event["biomes"]
                ]
                unused = [event_id for event_id in candidates if event_id not in used_events]
                content_id = rng.choice(unused or candidates)
                used_events.add(content_id)
            biome_name = catalog.biomes[biome_id]["name"]
            enemy_ids = (
                cls._compose_enemy_formation(catalog, rng, biome_id, kind, content_id)
                if kind in {"fight", "elite"} and content_id
                else []
            )
            rooms.append(
                Room(
                    room_id,
                    f"{biome_name}: {labels[kind]}",
                    kind,
                    edges[room_id],
                    content_id=content_id,
                    biome_id=biome_id,
                    enemy_ids=enemy_ids,
                    encounter_plan=(
                        cls._formation_plan(catalog, enemy_ids)
                        if enemy_ids
                        else "none"
                    ),
                )
            )
        boss_biome = room_biomes[11]
        rooms.append(
            Room(
                11,
                f"{catalog.biomes[boss_biome]['name']}: Apex Chamber",
                "boss",
                edges[11],
                content_id="the_core",
                biome_id=boss_biome,
                enemy_ids=list(catalog.encounters["the_core"]["enemies"]),
                encounter_plan="overseer",
            )
        )
        return rooms

    def _generate_pickups(self, rng: random.Random) -> list[EffectPickup]:
        categories = ["boon"] * 3 + ["item"] * 5 + ["bargain"] * 2 + ["trap"] * 2
        rng.shuffle(categories)
        positions = {room_id: self.room_position(room_id) for room_id in range(12)}
        anchors = set(positions.values())
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
                and abs(x - positions[0][0]) + abs(y - positions[0][1]) >= 6
                and abs(x - positions[11][0]) + abs(y - positions[11][1]) >= 5
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

    def _mechanic_positions(
        self,
        biome_id: str,
        excluded: set[tuple[int, int]],
        *,
        minimum_spacing: int,
    ) -> list[tuple[int, int]]:
        start = self.room_position(0)
        boss = self.room_position(11)
        return [
            (x, y)
            for y, row in enumerate(self.state.world_tiles)
            for x, character in enumerate(row)
            if character in WALKABLE_TILES
            and 0 < x < WORLD_WIDTH - 1
            and 0 < y < WORLD_HEIGHT - 1
            and self.biome_at(x, y) == biome_id
            and (x, y) not in excluded
            and abs(x - start[0]) + abs(y - start[1]) >= 5
            and abs(x - boss[0]) + abs(y - boss[1]) >= 3
            and all(abs(x - other_x) + abs(y - other_y) >= minimum_spacing for other_x, other_y in excluded)
        ]

    def _carve_landmark_site(self, x: int, y: int, glyph: str) -> None:
        for site_y in range(y - 1, y + 2):
            row = self.state.world_tiles[site_y]
            for site_x in range(x - 1, x + 2):
                row = row[:site_x] + glyph + row[site_x + 1:]
            self.state.world_tiles[site_y] = row

    def _generate_objectives(self, rng: random.Random) -> list[AccessObjective]:
        excluded = {self.room_position(room.id) for room in self.state.rooms}
        excluded |= {(pickup.x, pickup.y) for pickup in self.state.pickups}
        objectives: list[AccessObjective] = []
        for biome_id in self.state.biome_ids:
            candidates = self._mechanic_positions(
                biome_id,
                excluded,
                minimum_spacing=3,
            )
            if not candidates:
                raise RuleError(f"generated terrain has no access objective site in {biome_id}")
            x, y = rng.choice(candidates)
            self._carve_landmark_site(x, y, self.catalog.biomes[biome_id]["glyph"])
            excluded.add((x, y))
            objectives.append(AccessObjective(f"objective:{biome_id}", biome_id, x, y))
        excluded |= {
            (objective.x + offset_x, objective.y + offset_y)
            for objective in objectives
            for offset_y in (-1, 0, 1)
            for offset_x in (-1, 0, 1)
        }
        missions = {
            mission["biome"]: mission
            for mission in self.catalog.missions.values()
        }
        for objective in objectives:
            mission = missions[objective.biome_id]
            for approach in mission["approaches"]:
                previous = (objective.x, objective.y)
                sites: list[list[int]] = []
                for _ in approach["stages"]:
                    candidates = self._mechanic_positions(
                        objective.biome_id,
                        excluded,
                        minimum_spacing=2,
                    )
                    if not candidates:
                        raise RuleError(
                            f"generated terrain has no mission stage in {objective.biome_id}"
                        )
                    costs = self._travel_costs_from(previous)
                    travel = approach["telegraph"]["travel"]
                    minimum_cost, target_cost, maximum_cost = {
                        "short": (2, 6, 12),
                        "medium": (5, 10, 18),
                        "long": (8, 14, 24),
                    }[travel]
                    ranked = sorted(
                        candidates,
                        key=lambda point: (
                            abs(costs.get(point, WORLD_WIDTH * WORLD_HEIGHT * 3) - target_cost),
                            costs.get(point, WORLD_WIDTH * WORLD_HEIGHT * 3),
                            point,
                        ),
                    )
                    bounded = [
                        point
                        for point in ranked
                        if minimum_cost <= costs.get(point, -1) <= maximum_cost
                    ]
                    pool = bounded or ranked[:1]
                    x, y = rng.choice(pool)
                    sites.append([x, y])
                    excluded.add((x, y))
                    previous = (x, y)
                objective.approach_sites[approach["id"]] = sites
        return objectives

    def mission_definition(self, biome_id: str) -> dict[str, Any]:
        return next(
            mission
            for mission in self.catalog.missions.values()
            if mission["biome"] == biome_id
        )

    def objective_position(self, objective: AccessObjective) -> tuple[int, int]:
        if objective.approach is None:
            return objective.x, objective.y
        sites = objective.approach_sites[objective.approach]
        if objective.stage >= len(sites):
            return sites[-1][0], sites[-1][1]
        return sites[objective.stage][0], sites[objective.stage][1]

    def objective_route_projection(self, objective: AccessObjective) -> dict[str, int | str]:
        route = self._find_path(
            (self.state.party_x, self.state.party_y),
            self.objective_position(objective),
        )
        return self.route_intel(route)

    def _waypoint_toward(self, destination: tuple[int, int]) -> tuple[int, int]:
        route = self._find_path(
            (self.state.party_x, self.state.party_y),
            destination,
        )
        maximum = self.maximum_navigation_distance()
        spent = 0
        waypoint = (self.state.party_x, self.state.party_y)
        for tile in route:
            next_cost = self.movement_cost(*tile)
            if spent + next_cost > maximum:
                break
            spent += next_cost
            waypoint = tile
        return waypoint

    def objective_waypoint(self, objective: AccessObjective) -> tuple[int, int]:
        return self._waypoint_toward(self.objective_position(objective))

    def objective_approach_projection(
        self,
        objective: AccessObjective,
        approach_id: str,
    ) -> dict[str, int | str]:
        positions = [tuple(site) for site in objective.approach_sites[approach_id]]
        origin = (self.state.party_x, self.state.party_y)
        route: list[tuple[int, int]] = []
        for destination in positions:
            leg = self._find_path(origin, destination)
            route.extend(leg)
            origin = destination
        return self.route_intel(route)

    def _generate_landmarks(self) -> list[Landmark]:
        templates = {
            definition["biome"]: definition["id"]
            for definition in self.catalog.landmarks.values()
        }
        return [
            Landmark(
                id=f"landmark:{objective.biome_id}",
                template_id=templates[objective.biome_id],
                biome_id=objective.biome_id,
                x=objective.x,
                y=objective.y,
                cells=[
                    [objective.x + offset_x, objective.y + offset_y]
                    for offset_y in (-1, 0, 1)
                    for offset_x in (-1, 0, 1)
                ],
            )
            for objective in self.state.objectives
        ]

    def _generate_hazards(self, rng: random.Random) -> list[BiomeHazard]:
        excluded = {self.room_position(room.id) for room in self.state.rooms}
        excluded |= {(pickup.x, pickup.y) for pickup in self.state.pickups}
        excluded |= {(objective.x, objective.y) for objective in self.state.objectives}
        excluded |= {
            tuple(site)
            for objective in self.state.objectives
            for sites in objective.approach_sites.values()
            for site in sites
        }
        excluded |= {
            tuple(cell)
            for landmark in self.state.landmarks
            for cell in landmark.cells
        }
        hazards: list[BiomeHazard] = []
        for biome_id in self.state.biome_ids:
            for index in range(2):
                candidates = self._mechanic_positions(
                    biome_id,
                    excluded,
                    minimum_spacing=2,
                )
                if not candidates:
                    raise RuleError(f"generated terrain has no hazard site in {biome_id}")
                candidates = [
                    position
                    for position in candidates
                    if sum(
                        neighbor not in excluded and self.biome_at(*neighbor) == biome_id
                        for neighbor in self._neighbors(position)
                    ) >= 2
                ]
                if not candidates:
                    raise RuleError(f"generated terrain has no hazard footprint in {biome_id}")
                x, y = rng.choice(candidates)
                adjacent = [
                    position
                    for position in self._neighbors((x, y))
                    if position not in excluded and self.biome_at(*position) == biome_id
                ]
                rng.shuffle(adjacent)
                cells = [[x, y]] + [list(position) for position in adjacent[:2]]
                if len(cells) < 3:
                    raise RuleError(f"generated terrain has no hazard footprint in {biome_id}")
                excluded.update(tuple(cell) for cell in cells)
                hazards.append(
                    BiomeHazard(
                        f"hazard:{biome_id}:{index}",
                        biome_id,
                        x,
                        y,
                        cells=cells,
                    )
                )
        return hazards

    def _generate_facilities(self, rng: random.Random) -> list[BiomeFacility]:
        excluded = {self.room_position(room.id) for room in self.state.rooms}
        excluded |= {(pickup.x, pickup.y) for pickup in self.state.pickups}
        excluded |= {
            tuple(cell)
            for landmark in self.state.landmarks
            for cell in landmark.cells
        }
        excluded |= {
            tuple(site)
            for objective in self.state.objectives
            for sites in objective.approach_sites.values()
            for site in sites
        }
        excluded |= {
            tuple(cell)
            for hazard in self.state.hazards
            for cell in hazard.cells
        }
        definitions = {
            definition["biome"]: definition
            for definition in self.catalog.facilities.values()
        }
        facilities: list[BiomeFacility] = []
        for biome_id in self.state.biome_ids:
            candidates = self._mechanic_positions(
                biome_id,
                excluded,
                minimum_spacing=3,
            )
            if not candidates:
                raise RuleError(f"generated terrain has no facility site in {biome_id}")
            x, y = rng.choice(candidates)
            excluded.add((x, y))
            definition = definitions[biome_id]
            facilities.append(
                BiomeFacility(
                    id=f"facility:{biome_id}",
                    definition_id=definition["id"],
                    biome_id=biome_id,
                    x=x,
                    y=y,
                )
            )
        return facilities

    @classmethod
    def from_snapshot(cls, catalog: Catalog, snapshot: dict[str, Any]) -> GameEngine:
        try:
            snapshot = migrate_run(snapshot)
        except MigrationError as exc:
            raise RuleError(str(exc)) from exc
        try:
            saved_rules = snapshot["content_rules"]
            manifest = snapshot["content_manifest"]
            if saved_rules is None:
                catalog = load_legacy_catalog()
            else:
                if hashlib.sha256(canonical_bytes(saved_rules)).hexdigest() != manifest["fingerprint"]:
                    raise RuleError("saved rules do not match their content manifest fingerprint")
                if manifest != catalog.manifest.snapshot():
                    catalog = load_rules(saved_rules)
            if manifest != catalog.manifest.snapshot():
                raise RuleError("save content manifest does not match installed, embedded or archived rules and enabled packs")
        except (KeyError, TypeError, ValueError) as exc:
            raise RuleError(f"invalid saved content rules: {exc}") from exc
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
                world_id=raw["world_id"],
                biome_ids=raw["biome_ids"],
                room_positions=raw["room_positions"],
                next_card_copy_id=raw["next_card_copy_id"],
                hub_selection=raw["hub_selection"],
                hub_loadouts=raw["hub_loadouts"],
                doctrine_id=raw["doctrine_id"],
                tutorial=raw["tutorial"],
                tutorial_stage=raw["tutorial_stage"],
                current_room=raw["current_room"],
                party_x=raw["party_x"],
                party_y=raw["party_y"],
                exploration_steps=raw["exploration_steps"],
                patrols=[Patrol(**item) for item in raw["patrols"]],
                active_patrol_id=raw["active_patrol_id"],
                pickups=[EffectPickup(**item) for item in raw["pickups"]],
                current_pickup_id=raw["current_pickup_id"],
                hazards=[BiomeHazard(**item) for item in raw["hazards"]],
                current_hazard_id=raw["current_hazard_id"],
                facilities=[BiomeFacility(**item) for item in raw["facilities"]],
                current_facility_id=raw["current_facility_id"],
                objectives=[AccessObjective(**item) for item in raw["objectives"]],
                landmarks=[Landmark(**item) for item in raw["landmarks"]],
                current_objective_id=raw["current_objective_id"],
                required_objectives=raw["required_objectives"],
                known_feature_ids=raw["known_feature_ids"],
                travel_ticks=raw["travel_ticks"],
                pressure=raw["pressure"],
                pressure_recent=raw["pressure_recent"],
                pressure_incomplete_before_tick=raw["pressure_incomplete_before_tick"],
                encounter_pressure=raw["encounter_pressure"],
                encounter_modules=raw["encounter_modules"],
                reinforcement_tickets=raw["reinforcement_tickets"],
                reinforcement_reserve_id=raw["reinforcement_reserve_id"],
                pending_opening_hand=raw["pending_opening_hand"],
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
                ledger=RunLedger.from_snapshot(raw["ledger"]),
            )
            rng = random.Random()
            rng.setstate(_tuples(snapshot["rng_state"]))
            resolution = EventQueue.from_snapshot(snapshot["resolution_queue"])
        except (KeyError, TypeError, ValueError) as exc:
            raise RuleError(f"invalid save data: {exc}") from exc
        if state.world_id not in catalog.worlds:
            raise RuleError("save references an unknown world type")
        if (
            not isinstance(state.biome_ids, list)
            or len(state.biome_ids) != 4
            or len(set(state.biome_ids)) != 4
            or any(biome_id not in catalog.biomes for biome_id in state.biome_ids)
        ):
            raise RuleError("save contains an invalid four-biome selection")
        if (
            not isinstance(state.room_positions, list)
            or len(state.room_positions) != 12
            or any(
                not isinstance(position, list)
                or len(position) != 2
                or any(not isinstance(value, int) for value in position)
                for position in state.room_positions
            )
        ):
            raise RuleError("save contains malformed room positions")
        positions = {
            room_id: tuple(position)
            for room_id, position in enumerate(state.room_positions)
        }
        expected_positions, expected_edges = WORLD_LAYOUTS[catalog.worlds[state.world_id]["layout"]]
        if positions != expected_positions:
            raise RuleError("save room positions do not match its world type")
        _validate_world(state.world_tiles, positions)
        engine = cls(catalog, state, rng)
        engine.resolution = resolution
        if (
            not isinstance(state.tutorial, bool)
            or not isinstance(state.tutorial_stage, int)
            or state.tutorial_stage not in range(11)
            or not state.tutorial and state.tutorial_stage != 0
            or state.phase == "tutorial_complete" and not state.tutorial
        ):
            raise RuleError("save contains invalid tutorial state")
        walkable_count = sum(character in WALKABLE_TILES for row in state.world_tiles for character in row)
        if len(engine._distances_from(engine.room_position(0))) != walkable_count:
            raise RuleError("save contains disconnected world terrain")
        hero_ids = {hero.id for hero in state.heroes}
        if state.phase == "hub" and state.heroes:
            raise RuleError("hub save unexpectedly contains an active party")
        if state.phase != "hub" and (len(hero_ids) != 4 or not hero_ids <= set(catalog.heroes)):
            raise RuleError("save contains an unexpected crew roster")
        living_ranks = sorted(hero.rank for hero in state.heroes if hero.alive)
        if living_ranks != list(range(1, len(living_ranks) + 1)) or any(
            hero.rank != 0 for hero in state.heroes if not hero.alive
        ):
            raise RuleError("save contains an invalid surviving crew formation")
        if len(state.hub_selection) > 4 or any(hero_id not in catalog.heroes for hero_id in state.hub_selection):
            raise RuleError("save contains an invalid hub selection")
        if (
            not isinstance(state.hub_loadouts, dict)
            or any(
                hero_id not in state.hub_selection
                or type(loadout_id) is not str
                or loadout_id not in catalog.loadouts
                or catalog.loadouts[loadout_id]["hero"] != hero_id
                for hero_id, loadout_id in state.hub_loadouts.items()
            )
            or state.doctrine_id is not None
            and state.doctrine_id not in catalog.doctrines
        ):
            raise RuleError("save contains invalid party configuration")
        if state.doctrine_id is not None and not engine.doctrine_compatible(state.doctrine_id):
            raise RuleError("save contains an incompatible doctrine")
        if not engine.is_walkable(state.party_x, state.party_y):
            raise RuleError("save places the crew outside the ship")
        if len(state.rooms) != 12 or any(
            room.id != index
            or room.biome_id not in state.biome_ids
            or room.encounter_plan not in cls.ENCOUNTER_PLANS
            for index, room in enumerate(state.rooms)
        ):
            raise RuleError("save contains invalid biome rooms")
        if any(
            neighbor not in range(12) or room.id not in state.rooms[neighbor].neighbors
            for room in state.rooms
            for neighbor in room.neighbors
        ):
            raise RuleError("save contains invalid room connections")
        if any(sorted(room.neighbors) != sorted(expected_edges[room.id]) for room in state.rooms):
            raise RuleError("save room connections do not match its world type")
        for room in state.rooms:
            if room.kind not in {"fight", "elite", "boss"}:
                continue
            if (
                room.content_id not in catalog.encounters
                or not isinstance(room.enemy_ids, list)
                or not 1 <= len(room.enemy_ids) <= 4
                or any(enemy_id not in catalog.enemies for enemy_id in room.enemy_ids)
            ):
                raise RuleError("save contains an invalid enemy formation")
            if room.kind != "boss" and any(
                room.biome_id not in catalog.enemies[enemy_id].get("biomes", ["derelict"])
                for enemy_id in room.enemy_ids
            ):
                raise RuleError("save contains a biome-incompatible enemy formation")
            total_hp = sum(catalog.enemies[enemy_id]["max_hp"] for enemy_id in room.enemy_ids)
            minimum, maximum = (40, 60) if room.kind == "fight" else (62, 100)
            if room.kind != "boss" and not minimum <= total_hp <= maximum:
                raise RuleError("save contains an enemy formation outside its threat budget")
        actors_by_id = {actor.id: actor for actor in state.heroes + state.enemies}
        for intent in state.intents:
            enemy = actors_by_id.get(intent.get("enemy_id"))
            if enemy is None or enemy.side != "enemy" or enemy.definition_id not in catalog.enemies:
                raise RuleError("save contains an intent for an unknown actor")
            action = next(
                (
                    action
                    for action in catalog.enemies[enemy.definition_id]["actions"]
                    if action["name"] == intent.get("action")
                ),
                None,
            )
            if (
                action is None
                or intent.get("target_rule") != action["target"]
                or not isinstance(intent.get("target_ids"), list)
                or any(target_id not in actors_by_id for target_id in intent["target_ids"])
                or not isinstance(intent.get("target_labels"), list)
                or not intent["target_labels"]
                or any(not isinstance(label, str) or not label for label in intent["target_labels"])
            ):
                raise RuleError("save contains a malformed enemy intent")
        patrol_ids = {patrol.id for patrol in state.patrols}
        if len(patrol_ids) != len(state.patrols):
            raise RuleError("save contains duplicate patrols")
        for patrol in state.patrols:
            room = state.rooms[patrol.room_id] if 0 <= patrol.room_id < len(state.rooms) else None
            if (
                room is None
                or patrol.encounter_id not in catalog.encounters
                or not engine.is_walkable(patrol.x, patrol.y)
                or patrol.doctrine != engine.biome_mechanics(room.biome_id)["patrol"]["behavior"]
                or not isinstance(patrol.route, list)
                or not patrol.route
                or patrol.route[0] != list(engine.room_position(room.id))
                or any(
                    not isinstance(position, list)
                    or len(position) != 2
                    or not engine.is_walkable(*position)
                    for position in patrol.route
                )
                or any(
                    abs(left[0] - right[0]) + abs(left[1] - right[1]) != 1
                    for left, right in zip(patrol.route, patrol.route[1:])
                )
                or patrol.route_index not in range(len(patrol.route))
                or patrol.route_direction not in {-1, 1}
                or not isinstance(patrol.alert, int)
                or patrol.alert < 0
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
        pickup_kinds = [pickup.kind for pickup in state.pickups]
        if any(
            pickup_kinds.count(kind) != count
            for kind, count in {"boon": 3, "item": 5, "bargain": 2, "trap": 2}.items()
        ) or any(
            pickup.kind == "item" and pickup.payload.get("item_id") not in catalog.items
            for pickup in state.pickups
        ):
            raise RuleError("save contains an invalid discovery distribution")
        if state.current_pickup_id is not None and state.current_pickup_id not in pickup_ids:
            raise RuleError("save references an unknown map discovery")
        if (state.phase == "discovery") != (state.current_pickup_id is not None):
            raise RuleError("save contains an inconsistent active discovery")
        hazard_ids = {hazard.id for hazard in state.hazards}
        hazard_centers = {(hazard.x, hazard.y) for hazard in state.hazards}
        hazard_positions = {
            tuple(cell)
            for hazard in state.hazards
            for cell in hazard.cells
        }
        world_biomes = set(state.biome_ids)
        if (
            len(state.hazards) != len(world_biomes) * 2
            or len(hazard_ids) != len(state.hazards)
            or len(hazard_centers) != len(state.hazards)
            or len(hazard_positions) != len(state.hazards) * 3
            or any(
                hazard.biome_id not in world_biomes
                or not engine.is_walkable(hazard.x, hazard.y)
                or engine.biome_at(hazard.x, hazard.y) != hazard.biome_id
                or len(hazard.cells) != 3
                or len({tuple(cell) for cell in hazard.cells}) != 3
                or [hazard.x, hazard.y] not in hazard.cells
                or any(
                    not isinstance(cell, list)
                    or len(cell) != 2
                    or not engine.is_walkable(*cell)
                    for cell in hazard.cells
                )
                or any(cell not in hazard.cells for cell in hazard.triggered_cells)
                or len(hazard.triggered_cells) != len({tuple(cell) for cell in hazard.triggered_cells})
                or hazard.triggered != (not hazard.active)
                or (
                    not hazard.active
                    and hazard.suppressed_by is None
                    and len(hazard.triggered_cells) != len(hazard.cells)
                )
                or hazard.suppressed_by is not None and hazard.active
                for hazard in state.hazards
            )
            or any(
                sum(hazard.biome_id == biome_id for hazard in state.hazards) != 2
                for biome_id in world_biomes
            )
        ):
            raise RuleError("save contains invalid biome hazards")
        if state.current_hazard_id is not None and state.current_hazard_id not in hazard_ids:
            raise RuleError("save references an unknown biome hazard")
        if (state.phase == "hazard") != (state.current_hazard_id is not None):
            raise RuleError("save contains an inconsistent active biome hazard")

        facility_ids = {facility.id for facility in state.facilities}
        facility_positions = {(facility.x, facility.y) for facility in state.facilities}
        if (
            len(state.facilities) != len(world_biomes)
            or len(facility_ids) != len(state.facilities)
            or len(facility_positions) != len(state.facilities)
            or {facility.biome_id for facility in state.facilities} != world_biomes
            or any(
                facility.definition_id not in catalog.facilities
                or catalog.facilities[facility.definition_id]["biome"] != facility.biome_id
                or not engine.is_walkable(facility.x, facility.y)
                or engine.biome_at(facility.x, facility.y) != facility.biome_id
                or facility.used != (facility.outcome is not None)
                or facility.outcome is not None
                and facility.outcome not in {
                    option["id"]
                    for option in catalog.facilities[facility.definition_id]["options"]
                }
                for facility in state.facilities
            )
        ):
            raise RuleError("save contains invalid biome facilities")
        if state.current_facility_id is not None and state.current_facility_id not in facility_ids:
            raise RuleError("save references an unknown biome facility")
        if (state.phase == "facility") != (state.current_facility_id is not None):
            raise RuleError("save contains an inconsistent active biome facility")

        objective_ids = {objective.id for objective in state.objectives}
        objective_positions = {(objective.x, objective.y) for objective in state.objectives}
        if (
            len(state.objectives) != len(world_biomes)
            or len(objective_ids) != len(state.objectives)
            or len(objective_positions) != len(state.objectives)
            or {objective.biome_id for objective in state.objectives} != world_biomes
            or state.required_objectives not in range(1, len(state.objectives) + 1)
            or any(
                not engine.is_walkable(objective.x, objective.y)
                or engine.biome_at(objective.x, objective.y) != objective.biome_id
                for objective in state.objectives
            )
        ):
            raise RuleError("save contains invalid access objectives")
        objective_sites: set[tuple[int, int]] = set()
        for objective in state.objectives:
            mission = engine.mission_definition(objective.biome_id)
            approaches = {item["id"]: item for item in mission["approaches"]}
            if (
                set(objective.approach_sites) != set(approaches)
                or not isinstance(objective.facts, dict)
                or any(
                    len(objective.approach_sites[approach_id]) != len(approach["stages"])
                    or any(
                        not isinstance(site, list)
                        or len(site) != 2
                        or not engine.is_walkable(*site)
                        for site in objective.approach_sites[approach_id]
                    )
                    for approach_id, approach in approaches.items()
                )
            ):
                raise RuleError("save contains invalid objective stage sites")
            for sites in objective.approach_sites.values():
                for site in sites:
                    position = tuple(site)
                    if position in objective_sites:
                        raise RuleError("save contains overlapping objective stages")
                    objective_sites.add(position)
            if objective.approach is None:
                if objective.stage != 0 or objective.completed or objective.outcome is not None:
                    raise RuleError("save contains inconsistent unstarted objective state")
                continue
            approach = approaches.get(objective.approach)
            if (
                approach is None
                or objective.stage not in range(len(approach["stages"]) + 1)
                or objective.completed != (objective.stage == len(approach["stages"]))
                or objective.outcome != (approach["outcome"] if objective.completed else None)
                or objective.facts.get("approach") != objective.approach
            ):
                raise RuleError("save contains inconsistent objective progress")
        if state.current_objective_id is not None and state.current_objective_id not in objective_ids:
            raise RuleError("save references an unknown access objective")
        if (state.phase == "objective") != (state.current_objective_id is not None):
            raise RuleError("save contains an inconsistent active access objective")
        landmark_ids = {landmark.id for landmark in state.landmarks}
        if (
            len(state.landmarks) != len(world_biomes)
            or len(landmark_ids) != len(state.landmarks)
            or {landmark.biome_id for landmark in state.landmarks} != world_biomes
            or any(
                landmark.template_id not in catalog.landmarks
                or catalog.landmarks[landmark.template_id]["biome"] != landmark.biome_id
                or landmark.state not in {"intact", "changed"}
                or [landmark.x, landmark.y] not in landmark.cells
                or len(landmark.cells) != 9
                or any(
                    not engine.is_walkable(*cell)
                    for cell in landmark.cells
                )
                for landmark in state.landmarks
            )
        ):
            raise RuleError("save contains invalid biome landmarks")
        landmark_positions = {
            tuple(cell)
            for landmark in state.landmarks
            for cell in landmark.cells
        }
        if (
            pickup_positions & hazard_positions
            or pickup_positions & objective_positions
            or hazard_positions & objective_positions
            or pickup_positions & landmark_positions
            or hazard_positions & landmark_positions
            or pickup_positions & objective_sites
            or hazard_positions & objective_sites
            or facility_positions & pickup_positions
            or facility_positions & hazard_positions
            or facility_positions & objective_positions
            or facility_positions & objective_sites
            or facility_positions & landmark_positions
        ):
            raise RuleError("save contains overlapping map features")
        knowable_ids = pickup_ids | hazard_ids | facility_ids | objective_ids | landmark_ids
        if (
            not isinstance(state.known_feature_ids, list)
            or len(state.known_feature_ids) != len(set(state.known_feature_ids))
            or any(feature_id not in knowable_ids for feature_id in state.known_feature_ids)
            or any(
                pickup.hidden and pickup.id in state.known_feature_ids
                for pickup in state.pickups
            )
        ):
            raise RuleError("save contains invalid exploration knowledge")
        if (
            not isinstance(state.travel_ticks, int)
            or state.travel_ticks < state.exploration_steps
            or not isinstance(state.pending_opening_hand, int)
            or state.pending_opening_hand > 0
        ):
            raise RuleError("save contains invalid biome pressure state")
        if (
            type(state.pressure) is not int
            or state.pressure < 0
            or not isinstance(state.pressure_recent, list)
            or len(state.pressure_recent) > 8
            or any(
                not isinstance(change, dict)
                or set(change) != {"sequence", "source", "amount", "total", "detail"}
                or type(change["sequence"]) is not int
                or change["sequence"] < 1
                or not isinstance(change["source"], str)
                or change["source"] not in {source.value for source in PressureSource}
                or type(change["amount"]) is not int
                or change["amount"] < 0
                or type(change["total"]) is not int
                or change["total"] < change["amount"]
                or not isinstance(change["detail"], str)
                or not 1 <= len(change["detail"]) <= 120
                for change in state.pressure_recent
            )
            or any(after["sequence"] != before["sequence"] + 1
                   or after["total"] - before["total"] != after["amount"]
                   for before, after in zip(state.pressure_recent, state.pressure_recent[1:]))
            or state.pressure_recent and state.pressure_recent[-1]["total"] != state.pressure
            or state.pressure_incomplete_before_tick is not None
            and (type(state.pressure_incomplete_before_tick) is not int
                 or not 0 <= state.pressure_incomplete_before_tick <= state.travel_ticks)
        ):
            raise RuleError("save contains invalid expedition pressure")
        if (
            state.encounter_pressure is not None
            and (type(state.encounter_pressure) is not int
                 or not 0 <= state.encounter_pressure <= state.pressure)
            or not isinstance(state.encounter_modules, list)
            or any(not isinstance(module, str) or not module for module in state.encounter_modules)
            or len(set(state.encounter_modules)) != len(state.encounter_modules)
            or any(module not in catalog.mutations for module in state.encounter_modules)
            or type(state.reinforcement_tickets) is not int
            or state.reinforcement_tickets < 0
            or state.reinforcement_tickets > 1
            or state.reinforcement_reserve_id is not None
            and state.reinforcement_reserve_id not in catalog.enemies
            or state.phase == "combat" and state.encounter_pressure is None
            or state.phase != "combat" and state.encounter_pressure is not None
            or state.encounter_pressure is not None
            and len(state.encounter_modules) > director_profile(state.encounter_pressure).mutation_slots
            or state.phase != "combat" and (
                state.encounter_modules or state.reinforcement_tickets
                or state.reinforcement_reserve_id is not None
            )
            or ("base:reinforcement_call" in state.encounter_modules)
            != (state.reinforcement_reserve_id is not None)
            or any(
                right in catalog.mutations[left]["excludes"]
                or left in catalog.mutations[right]["excludes"]
                for index, left in enumerate(state.encounter_modules)
                for right in state.encounter_modules[index + 1:]
            )
        ):
            raise RuleError("save contains an invalid frozen encounter director")
        for hero_id, effects in state.boons.items():
            if hero_id not in hero_ids or any(
                boon_id not in catalog.boons or not isinstance(count, int) or count < 1
                for boon_id, count in effects.items()
            ):
                raise RuleError("save contains invalid boons")
        for hero_id, effects in state.curses.items():
            if hero_id not in hero_ids or any(
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
        if any(
            (card.card_id in catalog.curses)
            != (card.bound_hero_id is not None)
            or card.bound_hero_id is not None and card.bound_hero_id not in hero_ids
            for card in piles
        ):
            raise RuleError("save contains an invalid bound curse card")
        deck_copy_ids = [card.copy_id for card in state.deck]
        durable_cards = {card.copy_id: card for card in state.deck}
        combat_copy_ids = [
            card.copy_id
            for card in state.hand + state.draw_pile + state.discard_pile
        ]
        if (
            type(state.next_card_copy_id) is not int
            or state.next_card_copy_id < 1
            or any(type(copy_id) is not int or copy_id < 1 for copy_id in deck_copy_ids)
            or len(deck_copy_ids) != len(set(deck_copy_ids))
            or deck_copy_ids and state.next_card_copy_id <= max(deck_copy_ids)
            or any(
                card.infusion_id is not None and (
                    card.card_id not in catalog.cards
                    or not engine.infusion_compatible(card.card_id, card.infusion_id)
                )
                or card.mastery is not None and (
                    not card.upgraded
                    or card.card_id not in {
                        mastery["card_id"] for mastery in catalog.masteries.values()
                    }
                    or card.mastery not in {
                        branch["id"]
                        for mastery in catalog.masteries.values()
                        if mastery["card_id"] == card.card_id
                        for branch in mastery["branches"]
                    }
                )
                for card in piles
            )
            or state.phase == "combat" and sorted(combat_copy_ids) != sorted(deck_copy_ids)
            or state.phase == "combat" and any(
                asdict(card) != asdict(durable_cards.get(card.copy_id))
                for card in state.hand + state.draw_pile + state.discard_pile
            )
            or state.phase != "combat" and combat_copy_ids
        ):
            raise RuleError("save contains invalid card-copy state")
        expected_curse_cards = {
            (hero_id, curse_id): count
            for hero_id, effects in state.curses.items()
            for curse_id, count in effects.items()
            if catalog.curses[curse_id]["kind"] == "card"
        }
        actual_curse_cards = {
            (hero_id, curse_id): sum(
                card.card_id == curse_id and card.bound_hero_id == hero_id
                for card in state.deck
            )
            for hero_id, curse_id in expected_curse_cards
        }
        if expected_curse_cards != actual_curse_cards or any(
            card.card_id in catalog.curses
            and (card.bound_hero_id, card.card_id) not in expected_curse_cards
            for card in state.deck
        ):
            raise RuleError("save curse stacks do not match its bound cards")
        queued = list(resolution.state.pending)
        if resolution.state.active:
            queued.append(resolution.state.active.event)
        actor_ids = {actor.id for actor in state.heroes + state.enemies}
        if resolution.state.active:
            for listener in resolution.state.active.listeners:
                if listener.spec != REGISTERED.get(listener.spec.id) or listener.entity_id not in actor_ids:
                    raise RuleError("save queue references an unregistered listener")
        for event in queued:
            if (any(identity not in actor_ids for identity in event.target_ids)
                or event.payload.actor_id is not None and event.payload.actor_id not in actor_ids
                or len(event.target_ids) != len(set(event.target_ids))):
                raise RuleError("save queue references an unknown or repeated actor")
            continuation = event.event_type in {EventType.CARD_STEP, EventType.CARD_PLAY, EventType.CLEANUP}
            if event.event_type == EventType.DEATH:
                if event.payload.actor_id not in actor_ids or event.payload.opcode is not None:
                    raise RuleError("save queue has an invalid death notification")
            elif event.event_type in {EventType.CARD_DRAW, EventType.CARD_HELD}:
                curse = catalog.curses.get(event.payload.card_id)
                if (curse is None or curse["kind"] != "card" or event.payload.actor_id not in hero_ids
                    or event.payload.opcode is not None or event.payload.effect_index is not None or event.payload.card_upgraded):
                    raise RuleError("save queue has an invalid bound curse event")
            elif continuation:
                card = catalog.cards.get(event.payload.card_id)
                mastery = next(
                    (item for item in catalog.masteries.values()
                     if item["card_id"] == event.payload.card_id),
                    None,
                )
                owner_alive = any(
                    hero.id == event.payload.actor_id and hero.alive
                    for hero in state.heroes
                )
                if (card is None or card["hero"] != event.payload.actor_id or event.payload.opcode is not None
                    or event.payload.effect_index is None
                    or event.payload.card_mastery is not None and (
                        mastery is None
                        or event.payload.card_mastery not in {
                            branch["id"] for branch in mastery["branches"]
                        }
                    )
                    or event.payload.card_infusion is not None and (
                        event.payload.card_copy_id < 1
                        or not engine.infusion_compatible(
                            event.payload.card_id, event.payload.card_infusion
                        )
                    )
                    or event.payload.card_copy_id > 0 and owner_alive and (
                        event.payload.card_copy_id not in durable_cards
                        or durable_cards[event.payload.card_copy_id].card_id != event.payload.card_id
                        or durable_cards[event.payload.card_copy_id].mastery != event.payload.card_mastery
                        or durable_cards[event.payload.card_copy_id].infusion_id != event.payload.card_infusion
                    )):
                    raise RuleError("save queue has an invalid owned card continuation")
                effects = card["upgrade_effects"] if event.payload.card_upgraded else card["effects"]
                limit = len(effects) if event.event_type == EventType.CARD_STEP else len(CARD_TRIGGERS)
                if (event.payload.effect_index > limit
                    or event.event_type == EventType.CARD_PLAY and event.payload.effect_index == limit):
                    raise RuleError("save queue has an out-of-range card continuation")
            elif (event.payload.actor_id is None and not event.payload.raw_damage and event.payload.opcode != Opcode.HEAL
                  or event.payload.opcode is None or event.event_type.value != event.payload.opcode.value):
                raise RuleError("save queue has no matching primary actor and opcode")
            if not continuation and (
                event.payload.card_mastery is not None
                or event.payload.card_infusion is not None
                or event.payload.card_copy_id != 0
            ):
                raise RuleError("save queue attaches copy modifiers to a non-card event")
            if event.payload.card_id is not None and event.payload.card_id not in catalog.cards.keys() | catalog.curses.keys():
                raise RuleError("save queue references an unknown card")
            if any(status is not None and status not in CARD_STATUSES for status in (event.payload.status, event.payload.bonus_status)):
                raise RuleError("save queue references an unknown status")
        return engine

    def snapshot(self) -> dict[str, Any]:
        return {
            "save_version": self.SAVE_VERSION,
            "content_schema_version": self.catalog.raw["schema_version"],
            "content_manifest": self.catalog.manifest.snapshot(),
            "content_rules": self.catalog.rules,
            "state": asdict(self.state),
            "rng_state": self.rng.getstate(),
            "resolution_queue": self.resolution.snapshot(),
        }

    def add_log(self, message: str) -> None:
        self.state.log.append(message)
        del self.state.log[:-60]

    def record(self, event_kind: str, source_id: str, **data: Any) -> None:
        if self.resolution.state.active:
            event = self.resolution.state.active.event
            data = {"event_id": event.event_id, "root_action_id": event.root_action_id, "depth": event.depth, **data}
        self.state.ledger.record(event_kind, source_id, self.state.travel_ticks, self.state.round, **data)

    def _domain_rng(self, domain: str, *parts: str | int) -> random.Random:
        if not domain or any(not isinstance(part, (str, int)) or isinstance(part, bool) for part in parts):
            raise RuleError("RNG domains require a stable name and scalar identity parts")
        digest = hashlib.sha256(
            canonical_bytes(["dullest-dungeon:rng:2", self.state.seed, domain, *parts])
        ).digest()
        return random.Random(int.from_bytes(digest[:16], "big"))

    def _advance_pressure(self, source: PressureSource, units: int, detail: str) -> int:
        amount = action_price(source, units)
        if not amount:
            return 0
        previous_band = pressure_band(self.state.pressure)
        self.state.pressure += amount
        current_band = pressure_band(self.state.pressure)
        sequence = self.state.pressure_recent[-1]["sequence"] + 1 if self.state.pressure_recent else 1
        change = {
            "sequence": sequence,
            "source": source.value,
            "amount": amount,
            "total": self.state.pressure,
            "detail": detail,
        }
        self.state.pressure_recent.append(change)
        del self.state.pressure_recent[:-8]
        self.record(
            "pressure_change",
            source.value,
            amount=amount,
            total=self.state.pressure,
            band=current_band.id,
            detail=detail,
        )
        if current_band != previous_band:
            self.add_log(f"PRESSURE {current_band.name}: {current_band.forecast}")
        return amount

    @contextmanager
    def attribution(self, source_id: str):
        previous = self._source_id
        self._source_id = source_id
        try:
            yield
        finally:
            self._source_id = previous

    def room(self, room_id: int | None = None) -> Room:
        return self.state.rooms[self.state.current_room if room_id is None else room_id]

    def room_position(self, room_id: int) -> tuple[int, int]:
        try:
            position = self.state.room_positions[room_id]
            return position[0], position[1]
        except (IndexError, TypeError) as exc:
            raise RuleError("unknown ship compartment") from exc

    def current_biome(self) -> str:
        return self.biome_at(self.state.party_x, self.state.party_y)

    def biome_at(self, x: int, y: int) -> str:
        if 0 <= y < len(self.state.world_tiles) and 0 <= x < len(self.state.world_tiles[y]):
            glyph = self.state.world_tiles[y][x]
            terrain = next(
                (item for item in self.catalog.terrains.values() if item["glyph"] == glyph),
                None,
            )
            if terrain is not None and terrain.get("biome") is not None:
                return str(terrain["biome"])
        room = min(
            self.state.rooms,
            key=lambda item: abs(x - self.room_position(item.id)[0])
            + abs(y - self.room_position(item.id)[1]),
        )
        return room.biome_id

    def biome_mechanics(self, biome_id: str | None = None) -> dict[str, Any]:
        return self.catalog.biomes[biome_id or self.current_biome()]["mechanics"]

    def current_director(self) -> DirectorProfile:
        if self.catalog.raw["schema_version"] < 21:
            return director_profile(0)
        pressure = (
            self.state.encounter_pressure
            if self.state.encounter_pressure is not None
            else self.state.pressure
        )
        return director_profile(pressure)

    def world_director(self) -> DirectorProfile:
        if self.catalog.raw["schema_version"] < 21:
            return director_profile(0)
        return director_profile(self.state.pressure)

    def _select_encounter_mutations(
        self,
        encounter_id: str,
        encounter_kind: str,
        biome_id: str,
        enemy_count: int,
    ) -> list[str]:
        profile = self.current_director()
        if not profile.mutation_slots or not self.catalog.mutations or self.state.tutorial:
            return []
        band_order = {band: index for index, band in enumerate(("quiet", "watchful", "hunted", "lockdown", "overrun"))}
        candidates = sorted(
            (
                mutation
                for mutation in self.catalog.mutations.values()
                if mutation["effect"] in ACTIVE_MUTATION_EFFECTS
                and (mutation["effect"] != "guard_rear" or enemy_count > 1)
                and (mutation["effect"] not in {"surge_ally_death", "wound_on_ally_death"}
                     or enemy_count > 1)
                and band_order[mutation["min_band"]] <= band_order[profile.band]
                and encounter_kind in mutation["compatible_kinds"]
                and (not mutation["biomes"] or biome_id in mutation["biomes"])
            ),
            key=lambda mutation: (mutation["priority"], mutation["id"]),
        )
        prior = Counter(
            module
            for record in self.state.ledger.records
            if record.kind == "encounter_start"
            for module in record.data.get("modules", [])
        )
        rng = self._domain_rng(
            "encounter_mutation",
            self.resolution.state.combat_token + 1,
            encounter_id,
            biome_id,
            self.state.encounter_pressure or 0,
        )
        chosen: list[str] = []
        while candidates and len(chosen) < profile.mutation_slots:
            compatible = [
                mutation
                for mutation in candidates
                if all(
                    selected not in mutation["excludes"]
                    and mutation["id"] not in self.catalog.mutations[selected]["excludes"]
                    for selected in chosen
                )
            ]
            if not compatible:
                break
            weights = [
                max(1, 6 - prior[mutation["id"]] * 2)
                + (2 if mutation["biomes"] else 0)
                for mutation in compatible
            ]
            selected = rng.choices(compatible, weights=weights, k=1)[0]
            chosen.append(selected["id"])
            candidates.remove(selected)
        return chosen

    def movement_cost(self, x: int, y: int) -> int:
        return int(self.terrain_at(x, y)["cost"])

    def terrain_at(self, x: int, y: int) -> dict[str, Any]:
        if not self.is_walkable(x, y):
            raise RuleError("there is no traversable terrain at that position")
        glyph = self.state.world_tiles[y][x]
        terrain = next(
            (item for item in self.catalog.terrains.values() if item["glyph"] == glyph),
            None,
        )
        if terrain is None:
            raise RuleError(f"terrain glyph {glyph!r} has no movement profile")
        return terrain

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
        pending: list[tuple[int, int, int]] = [(0, start[0], start[1])]
        costs = {start: 0}
        previous: dict[tuple[int, int], tuple[int, int] | None] = {start: None}
        while pending:
            cost, current_x, current_y = heappop(pending)
            current = (current_x, current_y)
            if cost != costs[current]:
                continue
            if current == destination:
                break
            for neighbor in self._neighbors(current):
                next_cost = cost + self.movement_cost(*neighbor)
                if next_cost < costs.get(neighbor, WORLD_WIDTH * WORLD_HEIGHT * 3):
                    costs[neighbor] = next_cost
                    previous[neighbor] = current
                    heappush(pending, (next_cost, neighbor[0], neighbor[1]))
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

    def path_cost(self, path: list[tuple[int, int]]) -> int:
        return sum(self.movement_cost(x, y) for x, y in path)

    def route_intel(self, path: list[tuple[int, int]]) -> dict[str, int | str]:
        ticks = self.path_cost(path)
        pressure = action_price(PressureSource.TRAVEL, ticks)
        projected = pressure_status(self.state.pressure + pressure)
        interval = int(self.catalog.balance["exploration_steps_per_light"])
        light = (
            (self.state.travel_ticks + ticks) // interval
            - self.state.travel_ticks // interval
        )
        route_tiles = set(path)
        known_hazards = sum(
            hazard.active
            and hazard.id in self.state.known_feature_ids
            and any(
                tuple(cell) in route_tiles and cell not in hazard.triggered_cells
                for cell in hazard.cells
            )
            for hazard in self.state.hazards
        )
        perceived = [
            patrol
            for patrol in self.state.patrols
            if patrol.active and self.is_patrol_visible(patrol)
        ]
        patrol_distance = min(
            (
                abs(patrol.x - x) + abs(patrol.y - y)
                for patrol in perceived
                for x, y in route_tiles
            ),
            default=WORLD_WIDTH + WORLD_HEIGHT,
        )
        patrol_risk = "HIGH" if patrol_distance <= 1 else "WATCH" if patrol_distance <= 3 else "LOW"
        return {
            "ticks": ticks,
            "light": light,
            "pressure": pressure,
            "projected_pressure": int(projected["value"]),
            "projected_pressure_band": str(projected["name"]),
            "known_hazards": known_hazards,
            "patrol_risk": patrol_risk,
        }

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

    def _travel_costs_from(self, origin: tuple[int, int]) -> dict[tuple[int, int], int]:
        costs = {origin: 0}
        pending: list[tuple[int, int, int]] = [(0, origin[0], origin[1])]
        while pending:
            cost, x, y = heappop(pending)
            current = (x, y)
            if cost != costs[current]:
                continue
            for neighbor in self._neighbors(current):
                next_cost = cost + self.movement_cost(*neighbor)
                if next_cost < costs.get(neighbor, WORLD_WIDTH * WORLD_HEIGHT * 3):
                    costs[neighbor] = next_cost
                    heappush(pending, (next_cost, neighbor[0], neighbor[1]))
        return costs

    def path_to(self, x: int, y: int) -> list[tuple[int, int]]:
        if self.state.phase != "exploration":
            raise RuleError("the party cannot navigate right now")
        tutorial_destination = self.tutorial_destination()
        if self.state.tutorial and self.state.tutorial_stage <= 1 and (x, y) != tutorial_destination:
            raise RuleError("the tutorial route is limited to the highlighted training contact")
        if self.state.tutorial and self.state.tutorial_stage >= 9:
            raise RuleError("inspect the developed deck before leaving the tutorial")
        if not self.is_walkable(x, y):
            raise RuleError("choose a floor tile inside the ship")
        path = self._find_path((self.state.party_x, self.state.party_y), (x, y))
        if (x, y) != (self.state.party_x, self.state.party_y) and not path:
            raise RuleError("no route reaches that tile")
        maximum = self.maximum_navigation_distance()
        cost = self.path_cost(path)
        if cost > maximum:
            raise RuleError(f"destination costs {cost} travel ticks; maximum reach is {maximum}")
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
        previous_ticks = self.state.travel_ticks
        previous_light = self.state.light
        self.state.travel_ticks += self.movement_cost(x, y)
        self._advance_pressure(
            PressureSource.TRAVEL,
            self.state.travel_ticks - previous_ticks,
            f"travel through {self.biome_at(x, y)}",
        )
        interval = int(self.catalog.balance["exploration_steps_per_light"])
        light_spent = self.state.travel_ticks // interval - previous_ticks // interval
        if light_spent:
            self.state.light = max(0, self.state.light - light_spent)
            if self.state.light < self.catalog.balance["low_light_threshold"]:
                for hero in self.living_heroes():
                    self._change_stress(hero, light_spent)
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
        self.record("travel", self.biome_at(x, y), position=[x, y], ticks=self.state.travel_ticks - previous_ticks,
                    light_spent=previous_light - self.state.light, light=self.state.light, supplies=self.state.supplies)
        self._update_perception()
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
        self.start_combat(
            patrol.encounter_id,
            room.kind,
            surprised=self._surprised(),
            enemy_ids=room.enemy_ids,
        )
        if self.state.tutorial:
            self._prepare_tutorial_combat()

    def _prepare_tutorial_combat(self) -> None:
        warden = next(hero for hero in self.state.heroes if hero.id == "warden")
        engineer = next(hero for hero in self.state.heroes if hero.id == "engineer")
        warden.rank, engineer.rank = 2, 1
        warden.hp = 0
        warden.deaths_door = True
        warden.statuses["wound"] = 2

        desired_hand = ["mag_boots", "field_dressing", "scan", "arc_welder", "brace"]
        unused = list(range(len(self.state.deck)))
        hand = []
        for card_id in desired_hand:
            index = next(
                candidate
                for candidate in unused
                if self.state.deck[candidate].card_id == card_id
            )
            unused.remove(index)
            deck_card = self.state.deck[index]
            if card_id == "field_dressing":
                deck_card.upgraded = True
            hand.append(self._clone_card(deck_card))
        self.state.hand = hand
        self.state.draw_pile = [
            self._clone_card(self.state.deck[index])
            for index in unused
        ]
        self.state.discard_pile = []
        self.state.energy = self.catalog.balance["energy"]

        by_definition = {
            enemy.definition_id: enemy for enemy in self.living_enemies()
        }
        rad = by_definition["rad_acolyte"]
        control = by_definition["control_rod"]
        rad.rank, control.rank = 1, 2
        rad.max_hp = rad.hp = 16
        control.max_hp = control.hp = 20
        front = self.living_heroes()[0]

        def intent(enemy: Actor, action_name: str) -> dict[str, Any]:
            action = next(
                action
                for action in self.catalog.enemies[enemy.definition_id or enemy.id]["actions"]
                if action["name"] == action_name
            )
            return {
                "enemy_rank": enemy.rank,
                "enemy_id": enemy.id,
                "action": action_name,
                "target_rule": action["target"],
                "target_ids": [front.id],
                "target_labels": [self._intent_target_label(front)],
            }

        self.state.intents = [
            intent(rad, "Gamma Brand"),
            intent(control, "Containment Blow"),
        ]
        self.state.tutorial_stage = 2
        self.add_log("Training contact: restore the formation and read the coordinated intents.")

    def _resolve_exploration_tile(self) -> None:
        position = (self.state.party_x, self.state.party_y)
        hazard = next(
            (
                item
                for item in self.state.hazards
                if item.active
                and list(position) in item.cells
                and list(position) not in item.triggered_cells
            ),
            None,
        )
        if hazard:
            self._trigger_biome_hazard(hazard)
            return
        facility = next(
            (
                item
                for item in self.state.facilities
                if not item.used and (item.x, item.y) == position
            ),
            None,
        )
        if facility:
            self.state.phase = "facility"
            self.state.current_facility_id = facility.id
            return
        objective = next(
            (
                item
                for item in self.state.objectives
                if not item.completed and self.objective_position(item) == position
            ),
            None,
        )
        if objective:
            self.state.phase = "objective"
            self.state.current_objective_id = objective.id
            return
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
            if pickup.kind == "item":
                self.record("item_offer", pickup.id, offered=pickup.payload["item_id"],
                            copies=1 + round(self._item_effect_value("salvage_copies")))
            return
        room = next((room for room in self.state.rooms if self.room_position(room.id) == position), None)
        if room is None:
            return
        self.state.current_room = room.id
        room.visited = True
        if room.kind == "boss" and not self.boss_unlocked():
            self.add_log(
                f"Apex seal rejects the crew: {self.completed_objectives()}/"
                f"{self.state.required_objectives} access signals."
            )
            return
        if room.resolved or room.kind in {"start", "fight", "elite", "boss"}:
            return
        self.add_log(f"Entered {room.name}.")
        self._enter_room(room)

    def _trigger_biome_hazard(self, hazard: BiomeHazard) -> None:
        definition = self.biome_mechanics(hazard.biome_id)["hazard"]
        effect = definition["effect"]
        amount = int(definition["amount"])
        reach = self.world_director().hazard_reach
        if reach:
            if effect in {"damage_all", "damage_weakest", "status_all", "status_random", "wound_injured"}:
                amount += reach
            elif effect in {"light", "supplies", "opening_hand"}:
                amount -= reach
            elif effect == "stress_highest":
                amount += reach * 2
        heroes = self.living_heroes()
        if effect == "damage_all":
            for hero in list(heroes):
                self._damage(hero, amount)
        elif effect == "damage_weakest" and heroes:
            self._damage(min(heroes, key=lambda hero: hero.hp / hero.max_hp), amount)
        elif effect == "stress_highest" and heroes:
            self._change_stress(max(heroes, key=lambda hero: hero.stress), amount)
        elif effect == "light":
            self.state.light = max(0, min(100, self.state.light + amount))
        elif effect == "supplies":
            if self.state.supplies + amount >= 0:
                self.state.supplies += amount
            else:
                for hero in heroes:
                    self._change_stress(hero, 5)
        elif effect in {"status_all", "status_random"}:
            targets = heroes if effect == "status_all" else ([self.rng.choice(heroes)] if heroes else [])
            for hero in targets:
                self._add_status(hero, definition["status"], amount)
        elif effect == "wound_injured":
            for hero in heroes:
                if hero.hp < hero.max_hp or hero.deaths_door:
                    self._add_status(hero, "wound", amount)
        elif effect == "opening_hand":
            self.state.pending_opening_hand += amount
        hazard.triggered_cells.append([self.state.party_x, self.state.party_y])
        hazard.triggered = len(hazard.triggered_cells) == len(hazard.cells)
        if hazard.triggered:
            hazard.active = False
        if self.state.phase == "defeat":
            self.state.current_hazard_id = None
        else:
            self.state.current_hazard_id = hazard.id
            self.state.phase = "hazard"
        self.add_log(f"{definition['name']}: {definition['description']}")

    def suppress_hazard(self, hazard_id: str, source: str) -> None:
        hazard = next((item for item in self.state.hazards if item.id == hazard_id), None)
        if hazard is None or not hazard.active or hazard.triggered:
            raise RuleError("that hazard cannot be suppressed")
        hazard.active = False
        hazard.triggered = True
        hazard.suppressed_by = source
        self.add_log(f"{self.biome_mechanics(hazard.biome_id)['hazard']['name']} suppressed.")

    def current_hazard(self) -> BiomeHazard:
        hazard = next(
            (item for item in self.state.hazards if item.id == self.state.current_hazard_id),
            None,
        )
        if self.state.phase != "hazard" or hazard is None or not hazard.triggered_cells:
            raise RuleError("there is no biome hazard to acknowledge")
        return hazard

    def finish_hazard(self) -> None:
        self.current_hazard()
        self.state.current_hazard_id = None
        self.state.phase = "exploration"

    def facility_definition(self, facility: BiomeFacility) -> dict[str, Any]:
        return self.catalog.facilities[facility.definition_id]

    def current_facility(self) -> BiomeFacility:
        facility = next(
            (item for item in self.state.facilities if item.id == self.state.current_facility_id),
            None,
        )
        if self.state.phase != "facility" or facility is None or facility.used:
            raise RuleError("there is no biome facility to use")
        return facility

    def facility_option_available(self, facility: BiomeFacility, option_id: str) -> tuple[bool, str]:
        definition = self.facility_definition(facility)
        option = next((item for item in definition["options"] if item["id"] == option_id), None)
        if option is None:
            return False, "unknown procedure"
        cost = option["cost"]
        if cost["resource"] == "supplies" and self.state.supplies < cost["amount"]:
            return False, f"needs {cost['amount']} supply"
        if cost["resource"] == "light" and self.state.light < cost["amount"]:
            return False, f"needs {cost['amount']} light"
        if any(effect["op"] == "suppress_hazard" for effect in option["effects"]) and not any(
            hazard.active and hazard.biome_id == facility.biome_id
            for hazard in self.state.hazards
        ):
            return False, "no active hazard field"
        return True, "available"

    def _nearest_active_hazard(self, biome_id: str) -> BiomeHazard | None:
        active = [
            hazard
            for hazard in self.state.hazards
            if hazard.active and hazard.biome_id == biome_id
        ]
        if not active:
            return None
        costs = self._travel_costs_from((self.state.party_x, self.state.party_y))
        return min(
            active,
            key=lambda item: min(
                costs.get(tuple(cell), WORLD_WIDTH * WORLD_HEIGHT * 3)
                for cell in item.cells
            ),
        )

    def _reveal_biome_features(self, biome_id: str) -> None:
        known = set(self.state.known_feature_ids)
        known.update(
            hazard.id
            for hazard in self.state.hazards
            if hazard.biome_id == biome_id
        )
        known.update(
            pickup.id
            for pickup in self.state.pickups
            if not pickup.hidden and self.biome_at(pickup.x, pickup.y) == biome_id
        )
        known.update(
            item.id
            for item in self.state.facilities
            if item.biome_id == biome_id
        )
        self.state.known_feature_ids = sorted(known)

    def _stabilize_terrain(self, biome_id: str, amount: int) -> int:
        origin = (self.state.party_x, self.state.party_y)
        protected = {self.room_position(room.id) for room in self.state.rooms}
        protected |= {(pickup.x, pickup.y) for pickup in self.state.pickups}
        protected |= {(facility.x, facility.y) for facility in self.state.facilities}
        protected |= {
            tuple(cell)
            for landmark in self.state.landmarks
            for cell in landmark.cells
        }
        protected |= {
            tuple(cell)
            for hazard in self.state.hazards
            for cell in hazard.cells
            if hazard.active
        }
        candidates = sorted(
            (
                (x, y)
                for y, row in enumerate(self.state.world_tiles)
                for x, glyph in enumerate(row)
                if glyph in WALKABLE_TILES
                and (x, y) not in protected
                and self.biome_at(x, y) == biome_id
                and glyph != "="
            ),
            key=lambda position: (
                abs(position[0] - origin[0]) + abs(position[1] - origin[1]),
                position,
            ),
        )
        changed = 0
        for x, y in candidates[: max(0, amount)]:
            row = self.state.world_tiles[y]
            self.state.world_tiles[y] = row[:x] + "=" + row[x + 1:]
            changed += 1
        return changed

    def _apply_exploration_effect(
        self,
        biome_id: str,
        effect: dict[str, Any],
        source: str,
    ) -> None:
        with self.attribution(source):
            self.record("world_effect", source, biome=biome_id, effect=dict(effect))
            self._apply_exploration_effect_primary(biome_id, effect, source)

    def _apply_exploration_effect_primary(self, biome_id: str, effect: dict[str, Any], source: str) -> None:
        operation = effect["op"]
        amount = int(effect["amount"])
        if operation == "suppress_hazard":
            hazard = self._nearest_active_hazard(biome_id)
            if hazard:
                self.suppress_hazard(hazard.id, source)
        elif operation == "reveal_biome":
            self._reveal_biome_features(biome_id)
        elif operation == "stabilize_terrain":
            changed = self._stabilize_terrain(biome_id, amount)
            self.add_log(f"{changed} nearby terrain cells become stable service rail.")
        elif operation == "agitate_patrols":
            self._alert_biome_patrols(biome_id, amount)
        elif operation == "calm_patrols":
            for patrol in self.state.patrols:
                if self.room(patrol.room_id).biome_id == biome_id:
                    patrol.alert = 0
        else:
            self._apply_objective_effect(operation, amount, effect.get("status"))

    def _apply_facility_effect(self, facility: BiomeFacility, effect: dict[str, Any]) -> None:
        self._apply_exploration_effect(facility.biome_id, effect, facility.id)

    def resolve_facility(self, option_id: str) -> str:
        facility = self.current_facility()
        definition = self.facility_definition(facility)
        option = next((item for item in definition["options"] if item["id"] == option_id), None)
        if option is None:
            raise RuleError("unknown facility procedure")
        available, reason = self.facility_option_available(facility, option_id)
        if not available:
            raise RuleError(reason)
        self._apply_objective_cost(option["cost"])
        for effect in option["effects"]:
            if self.state.phase == "defeat":
                break
            self._apply_facility_effect(facility, effect)
        facility.used = True
        facility.outcome = option_id
        self.record("facility_choice", facility.definition_id, chosen=option_id,
                    offered=[option["id"] for option in definition["options"]], cost=dict(option["cost"]))
        self._advance_pressure(
            PressureSource.FACILITY,
            1,
            f"{definition['name']}: {option['label']}",
        )
        self.state.current_facility_id = None
        if self.state.phase != "defeat":
            self.state.phase = "exploration"
        message = f"{definition['name']}: {option['label'].lower()} complete."
        self.add_log(message)
        return message

    def leave_facility(self) -> None:
        facility = self.current_facility()
        self.record("facility_skipped", facility.definition_id)
        self.state.current_facility_id = None
        self.state.phase = "exploration"

    def current_objective(self) -> AccessObjective:
        objective = next(
            (item for item in self.state.objectives if item.id == self.state.current_objective_id),
            None,
        )
        if self.state.phase != "objective" or objective is None or objective.completed:
            raise RuleError("there is no access objective to resolve")
        return objective

    def completed_objectives(self) -> int:
        return sum(objective.completed for objective in self.state.objectives)

    def boss_unlocked(self) -> bool:
        return self.completed_objectives() >= self.state.required_objectives

    def core_patrol(self) -> Patrol:
        return next(
            patrol
            for patrol in self.state.patrols
            if self.room(patrol.room_id).kind == "boss"
        )

    def core_position(self) -> tuple[int, int]:
        patrol = self.core_patrol()
        if patrol.active:
            return patrol.x, patrol.y
        return self.room_position(patrol.room_id)

    def core_route_projection(self) -> dict[str, int | str]:
        route = self._find_path(
            (self.state.party_x, self.state.party_y),
            self.core_position(),
        )
        return self.route_intel(route)

    def core_waypoint(self) -> tuple[int, int]:
        return self._waypoint_toward(self.core_position())

    def _apply_objective_effect(self, effect: str, amount: int, status: str | None = None) -> None:
        heroes = self.living_heroes()
        if effect == "light":
            self.state.light = max(0, min(100, self.state.light + amount))
        elif effect == "supplies":
            self.state.supplies = max(0, self.state.supplies + amount)
        elif effect == "heal_all":
            for hero in heroes:
                self._heal(hero, amount)
        elif effect == "heal_weakest" and heroes:
            self._heal(min(heroes, key=lambda hero: hero.hp / hero.max_hp), amount)
        elif effect == "stress_all":
            for hero in heroes:
                self._change_stress(hero, amount)
        elif effect == "stress_highest" and heroes:
            self._change_stress(max(heroes, key=lambda hero: hero.stress), amount)
        elif effect == "damage_all":
            for hero in list(heroes):
                self._damage(hero, amount)
        elif effect == "damage_random" and heroes:
            self._damage(self.rng.choice(heroes), amount)
        elif effect == "status_all" and status:
            for hero in heroes:
                self._add_status(hero, status, amount)
        elif effect == "cleanse_all":
            for hero in heroes:
                for negative in ("marked", "stun", "vulnerable", "weak", "wound"):
                    hero.statuses.pop(negative, None)
        elif effect == "upgrade_random":
            candidates = [card for card in self.state.deck if not card.upgraded and card.card_id in self.catalog.cards]
            if candidates:
                card = self.rng.choice(candidates)
                card.upgraded = True
                self.record("card_upgraded", card.card_id, source="objective")
        elif effect == "remove_random":
            candidates = [card for card in self.state.deck if card.card_id in self.catalog.cards]
            if candidates:
                card = self.rng.choice(candidates)
                self.state.deck.remove(card)
                self.record("card_removed", card.card_id, card=asdict(card), source="objective")
        elif effect == "boon_random" and heroes:
            hero = self.rng.choice(heroes)
            options = self.boon_options(hero.id, count=1)
            if options:
                self.acquire_boon(hero.id, options[0])
        elif effect == "item_random":
            self.acquire_item(self.rng.choice(list(self.catalog.items)))
        elif effect == "curse_random" and heroes:
            self.acquire_curse(self.rng.choice(heroes).id, self.rng.choice(list(self.catalog.curses)))

    def _objective_approach(self, objective: AccessObjective) -> dict[str, Any]:
        if objective.approach is None:
            raise RuleError("choose an objective approach first")
        return next(
            approach
            for approach in self.mission_definition(objective.biome_id)["approaches"]
            if approach["id"] == objective.approach
        )

    def _apply_objective_cost(self, cost: dict[str, Any]) -> None:
        resource = cost["resource"]
        amount = int(cost["amount"])
        if resource == "none" or not amount:
            return
        if resource == "supplies":
            if self.state.supplies < amount:
                raise RuleError(f"this approach requires {amount} supply")
            self.state.supplies -= amount
        elif resource == "light":
            if self.state.light < amount:
                raise RuleError(f"this approach requires {amount} light")
            self.state.light -= amount
        elif resource == "stress_all":
            for hero in self.living_heroes():
                self._change_stress(hero, amount)
        elif resource == "health_all":
            for hero in list(self.living_heroes()):
                self._damage(hero, amount)

    def _alert_biome_patrols(self, biome_id: str, duration: int = 8) -> None:
        for patrol in self.state.patrols:
            if patrol.active and self.room(patrol.room_id).biome_id == biome_id:
                patrol.alert = max(patrol.alert, duration)

    def begin_objective(self, approach_id: str) -> str:
        objective = self.current_objective()
        if objective.approach is not None:
            raise RuleError("this objective approach is already committed")
        mission = self.mission_definition(objective.biome_id)
        approach = next(
            (item for item in mission["approaches"] if item["id"] == approach_id),
            None,
        )
        if approach is None:
            raise RuleError("unknown objective approach")
        self._apply_objective_cost(approach["cost"])
        objective.approach = approach_id
        self.record("objective_approach", objective.id, chosen=approach_id,
                    offered=[item["id"] for item in mission["approaches"]], cost=dict(approach["cost"]))
        objective.stage = 0
        objective.facts = {
            "approach": approach_id,
            "started_at_tick": self.state.travel_ticks,
        }
        self._alert_biome_patrols(objective.biome_id)
        self.state.current_objective_id = None
        if self.state.phase != "defeat":
            self.state.phase = "exploration"
        destination = self.objective_position(objective)
        message = (
            f"Committed to {approach['label'].lower()}. "
            f"Next: {approach['stages'][0]['label']} at {destination[0]:03},{destination[1]:02}."
        )
        self.add_log(message)
        return message

    def leave_objective(self) -> None:
        self.current_objective()
        self.state.current_objective_id = None
        self.state.phase = "exploration"

    def _start_objective_combat(self, biome_id: str, tier: int) -> None:
        encounter_kind = "elite" if tier >= 2 else "normal"
        candidates = [
            encounter
            for encounter in self.catalog.encounters.values()
            if encounter["kind"] == encounter_kind
            and biome_id in encounter.get("biomes", ["derelict"])
        ]
        if not candidates:
            raise RuleError(f"{biome_id} has no {encounter_kind} objective encounter")
        encounter = self.rng.choice(candidates)
        room_kind = "elite" if encounter_kind == "elite" else "fight"
        enemies = self._compose_enemy_formation(
            self.catalog,
            self.rng,
            biome_id,
            room_kind,
            encounter["id"],
        )
        self.start_combat(encounter["id"], "objective", enemy_ids=enemies)
        self.add_log("The objective action draws an immediate hostile response.")

    def advance_objective(self) -> str:
        objective = self.current_objective()
        approach = self._objective_approach(objective)
        if objective.stage >= len(approach["stages"]):
            raise RuleError("this objective has no remaining stage")
        stage = approach["stages"][objective.stage]
        effect = stage.get("effect")
        combat_tier = 0
        if effect and effect["op"] == "objective_combat":
            combat_tier = int(effect["amount"])
        elif effect:
            self._apply_exploration_effect(objective.biome_id, effect, objective.id)
        objective.stage += 1
        self._advance_pressure(
            PressureSource.OBJECTIVE_STAGE,
            1,
            f"{self.mission_definition(objective.biome_id)['name']}: {stage['label']}",
        )
        self.record("objective_stage", objective.id, approach=objective.approach,
                    stage=objective.stage, label=stage["label"])
        objective.facts.setdefault("stages", []).append(
            {
                "index": objective.stage,
                "label": stage["label"],
                "effect": effect["op"] if effect else "none",
                "at_tick": self.state.travel_ticks,
            }
        )
        self.state.current_objective_id = None
        if self.state.phase == "defeat":
            return f"{stage['label']} ended the expedition."
        if objective.stage < len(approach["stages"]):
            if combat_tier:
                self._start_objective_combat(objective.biome_id, combat_tier)
                message = f"{stage['label']} complete. Hostile response underway."
                self.add_log(message)
                return message
            self.state.phase = "exploration"
            destination = self.objective_position(objective)
            message = (
                f"{stage['label']} complete. Next: "
                f"{approach['stages'][objective.stage]['label']} at "
                f"{destination[0]:03},{destination[1]:02}."
            )
            self.add_log(message)
            return message
        completion = approach["completion"]
        was_unlocked = self.boss_unlocked()
        self._apply_exploration_effect(objective.biome_id, completion, objective.id)
        objective.completed = True
        objective.outcome = approach["outcome"]
        self.record("objective_completed", objective.id, approach=objective.approach, outcome=objective.outcome)
        objective.facts.update(
            {
                "outcome": approach["outcome"],
                "completed_at_tick": self.state.travel_ticks,
                "optional": was_unlocked,
            }
        )
        if self.state.phase != "defeat":
            self.state.phase = "exploration"
        progress = self.completed_objectives()
        mission = self.mission_definition(objective.biome_id)
        message = (
            f"{mission['name']} secured: {approach['outcome'].replace('_', ' ')}. "
            f"Access {progress}/{self.state.required_objectives}."
        )
        if self.boss_unlocked():
            boss_patrol = next(
                (patrol for patrol in self.state.patrols if self.room(patrol.room_id).kind == "boss"),
                None,
            )
            if boss_patrol and self.state.phase != "victory":
                boss_patrol.active = True
            if was_unlocked:
                message += " Core access was already open; this objective was optional."
            else:
                core_x, core_y = self.core_position()
                message += (
                    f" The Overseer Core seal is open at {core_x:03},{core_y:02}. "
                    "Remaining objectives are optional."
                )
        self.add_log(message)
        return message

    def resolve_objective(self, method: str) -> str:
        """Compatibility command for rule clients; objective stages still require travel."""
        objective = self.current_objective()
        if objective.approach is None:
            mission = self.mission_definition(objective.biome_id)
            if method in {"safe", "force"}:
                index = 0 if method == "safe" else 1
                return self.begin_objective(mission["approaches"][index]["id"])
            return self.begin_objective(method)
        if method not in {"advance", objective.approach, "safe", "force"}:
            raise RuleError("unknown objective procedure")
        return self.advance_objective()

    def is_hazard_visible(self, hazard: BiomeHazard) -> bool:
        radius = int(self.biome_mechanics(hazard.biome_id)["visibility"]["hazard_radius"])
        return any(
            abs(cell[0] - self.state.party_x) + abs(cell[1] - self.state.party_y) <= radius
            for cell in hazard.cells
        )

    def feature_is_known(self, feature_id: str) -> bool:
        return feature_id in self.state.known_feature_ids

    def _update_perception(self) -> None:
        known = set(self.state.known_feature_ids)
        position = (self.state.party_x, self.state.party_y)
        for objective in self.state.objectives:
            known.add(objective.id)
        for landmark in self.state.landmarks:
            if landmark.discovered:
                known.add(landmark.id)
        for hazard in self.state.hazards:
            if self.is_hazard_visible(hazard):
                known.add(hazard.id)
        for facility in self.state.facilities:
            visibility = self.biome_mechanics(facility.biome_id)["visibility"]
            radius = int(visibility.get("feature_radius", visibility["patrol_radius"]))
            if abs(facility.x - position[0]) + abs(facility.y - position[1]) <= radius:
                known.add(facility.id)
        for pickup in self.state.pickups:
            if pickup.hidden:
                continue
            biome_id = self.biome_at(pickup.x, pickup.y)
            visibility = self.biome_mechanics(biome_id)["visibility"]
            radius = int(visibility.get("feature_radius", visibility["patrol_radius"]))
            if abs(pickup.x - position[0]) + abs(pickup.y - position[1]) <= radius:
                known.add(pickup.id)
        self.state.known_feature_ids = sorted(known)

    def is_patrol_visible(self, patrol: Patrol) -> bool:
        biome_id = self.room(patrol.room_id).biome_id
        radius = int(self.biome_mechanics(biome_id)["visibility"]["patrol_radius"])
        return abs(patrol.x - self.state.party_x) + abs(patrol.y - self.state.party_y) <= radius

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

    def decline_pickup(self) -> None:
        pickup = self.current_pickup()
        if pickup.kind not in {"item", "boon"}:
            raise RuleError("this discovery cannot be skipped as a reward")
        self.record("pickup_skipped", pickup.id, kind=pickup.kind, offered=pickup.payload)
        self._finish_pickup("The crew leaves the reward behind.")

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
        self.record("boon_offer", "reward:boon", owner=hero_id, offered=options)
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
        self.record("boon_acquired", boon_id, owner=hero_id, count=owned[boon_id])
        return owned[boon_id]

    def acquire_item(self, item_id: str, copies: int = 1) -> int:
        if item_id not in self.catalog.items or copies < 1:
            raise RuleError("invalid item acquisition")
        gained = copies + round(self._item_effect_value("salvage_copies"))
        self.state.items[item_id] = self.state.items.get(item_id, 0) + gained
        self.record("item_acquired", item_id, gained=gained, count=self.state.items[item_id])
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
            card = self._new_card(curse_id, bound_hero_id=hero_id)
            self.state.deck.append(card)
            if self.state.phase == "combat":
                self.state.discard_pile.append(self._clone_card(card))
        self.record("curse_acquired", curse_id, owner=hero_id, count=owned[curse_id])
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
        self.record("boon_choice", pickup.id, owner=hero_id, chosen=boon_id,
                    offered=pickup.payload["options"])
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
            self.record("bargain_offer", pickup.id, owner=hero_id, offered=pickup.payload["options"])
        return list(pickup.payload["options"])

    def resolve_bargain(self, hero_id: str, option_index: int | None) -> str:
        pickup = self.current_pickup()
        if pickup.kind != "bargain":
            raise RuleError("this discovery offers no bargain")
        if option_index is None:
            self.record("bargain_choice", pickup.id, owner=hero_id, chosen=None,
                        offered=pickup.payload.get("options", []))
            message = "The crew leaves the anomaly unanswered."
            self._finish_pickup(message)
            return message
        options = self.bargain_options(hero_id)
        if not 0 <= option_index < len(options):
            raise RuleError("invalid bargain")
        option = options[option_index]
        self.record("bargain_choice", pickup.id, owner=hero_id, chosen=option_index, offered=options)
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

    def _route_patrol_step(
        self,
        patrol: Patrol,
        current: tuple[int, int],
        occupied: set[tuple[int, int]],
    ) -> tuple[int, int]:
        if len(patrol.route) < 2:
            return current
        route_position = tuple(patrol.route[patrol.route_index])
        if current == route_position:
            next_index = patrol.route_index + patrol.route_direction
            if next_index not in range(len(patrol.route)):
                patrol.route_direction *= -1
                next_index = patrol.route_index + patrol.route_direction
            patrol.route_index = next_index
            route_position = tuple(patrol.route[patrol.route_index])
        path = self._find_path(current, route_position)
        if not path or path[0] in occupied:
            return current
        return path[0]

    def _advance_patrols(self) -> None:
        if self.state.tutorial:
            return
        party = (self.state.party_x, self.state.party_y)
        distances = self._distances_from(party)
        occupied = {(patrol.x, patrol.y) for patrol in self.state.patrols if patrol.active}
        for patrol in (item for item in self.state.patrols if item.active):
            current = (patrol.x, patrol.y)
            occupied.discard(current)
            room = self.room(patrol.room_id)
            profile = self.biome_mechanics(room.biome_id)["patrol"]
            director = self.world_director()
            cadence = max(
                1,
                int(profile["cadence"])
                - (1 if patrol.alert else 0)
                - director.patrol_cadence_reduction,
            )
            if self.state.exploration_steps % cadence:
                occupied.add(current)
                patrol.alert = max(0, patrol.alert - 1)
                continue
            room_kind = room.kind
            aggression = (
                int(profile["aggression"])
                + (2 if room_kind in {"elite", "boss"} else 0)
                + (3 if patrol.alert else 0)
                + director.patrol_aggression
            )
            aggression = max(4, aggression - round(self._item_effect_value("patrol_aggression_reduction")))
            destination = current
            if 0 < distances.get(current, WORLD_WIDTH * WORLD_HEIGHT) <= aggression:
                choices = [tile for tile in self._neighbors(current) if tile not in occupied]
                if choices:
                    ordered = sorted(
                        choices,
                        key=lambda tile: (distances.get(tile, WORLD_WIDTH * WORLD_HEIGHT), tile),
                    )
                    if patrol.doctrine == "erratic" and len(ordered) > 1 and self.rng.random() < 0.35:
                        destination = self.rng.choice(ordered[1:])
                    else:
                        destination = ordered[0]
            elif room_kind == "boss":
                destination = current
            elif patrol.doctrine == "sentry":
                home = self.room_position(patrol.room_id)
                path = self._find_path(current, home)
                if path and path[0] not in occupied:
                    destination = path[0]
            elif room_kind != "boss" and patrol.doctrine in {"roam", "erratic"}:
                home = self.room_position(patrol.room_id)
                choices = [
                    tile
                    for tile in self._neighbors(current)
                    if tile not in occupied
                    and abs(tile[0] - home[0]) + abs(tile[1] - home[1]) <= int(profile["leash"])
                ]
                if choices:
                    destination = self.rng.choice(choices)
            elif patrol.doctrine in {"circuit", "migrate", "stalk", "sweep"}:
                destination = self._route_patrol_step(patrol, current, occupied)
            elif patrol.doctrine == "hunt":
                home = self.room_position(patrol.room_id)
                if abs(current[0] - home[0]) + abs(current[1] - home[1]) > int(profile["leash"]):
                    choices = [tile for tile in self._neighbors(current) if tile not in occupied]
                    if choices:
                        destination = min(
                            choices,
                            key=lambda tile: (abs(tile[0] - home[0]) + abs(tile[1] - home[1]), tile),
                        )
            if destination in occupied:
                destination = current
            patrol.x, patrol.y = destination
            patrol.alert = max(0, patrol.alert - 1)
            occupied.add(destination)
            if destination == party:
                self._start_patrol_combat(patrol)
                return

    def _enter_room(self, room: Room) -> None:
        if room.kind in {"fight", "elite", "boss"}:
            self.start_combat(
                room.content_id or "",
                room.kind,
                surprised=self._surprised(),
                enemy_ids=room.enemy_ids,
            )
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
        survivors = self.living_heroes()
        if not survivors:
            raise RuleError("no living crew can use a supply")
        if purpose == "heal":
            target = min(survivors, key=lambda actor: actor.hp / actor.max_hp)
            self._heal(target, 9 + round(self._item_effect_value("supply_heal_bonus")))
            message = f"A supply restores {target.name}."
        elif purpose == "calm":
            target = max(survivors, key=lambda actor: actor.stress)
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

    def _apply_opening_mutations(self) -> None:
        for mutation_id in self.state.encounter_modules:
            mutation = self.catalog.mutations[mutation_id]
            effect = mutation["effect"]
            if effect not in OPENING_MUTATION_EFFECTS:
                self.add_log(f"{mutation['marker']} — {mutation['description']}")
                continue
            amount = int(mutation["amount"])
            enemies = self.living_enemies()
            heroes = self.living_heroes()
            if not enemies or not heroes:
                return
            with self.attribution(mutation_id):
                if effect == "opening_front_block":
                    enemies[0].block += amount
                    self.record("block", mutation_id, target=enemies[0].id, amount=amount)
                elif effect == "guard_rear":
                    if len(enemies) > 1:
                        enemies[-1].guarded_by = enemies[0].id
                        enemies[-1].guard_turns = amount
                        self.record("guard", mutation_id, actor=enemies[0].id,
                                    target=enemies[-1].id, duration=amount)
                elif effect == "mark_weakest":
                    target = min(heroes, key=lambda hero: (hero.hp / hero.max_hp, hero.rank))
                    self._add_status(target, "marked", amount)
                elif effect == "dodge_rear":
                    self._add_status(enemies[-1], "dodge", amount)
                elif effect == "riposte_front":
                    self._add_status(enemies[0], "riposte", amount)
                elif effect == "focus_striker":
                    target = next(
                        (enemy for enemy in enemies if "striker" in self.enemy_roles(enemy.definition_id or enemy.id)),
                        enemies[0],
                    )
                    self._add_status(target, "focus", amount)
                elif effect == "shove_front_crew":
                    self._move(heroes[0], amount, enemies[0])
                    self.record("movement", mutation_id, target=heroes[0].id, amount=amount,
                                rank=heroes[0].rank)
                else:
                    raise RuleError(f"unregistered opening mutation effect {effect}")
            self.add_log(f"{mutation['marker']} — {mutation['description']}")

    def _apply_enemy_phase_mutations(self) -> None:
        modules = sorted(
            (self.catalog.mutations[identity] for identity in self.state.encounter_modules),
            key=lambda mutation: (mutation["priority"], mutation["id"]),
        )
        for mutation in modules:
            effect = mutation["effect"]
            if effect not in {
                "heal_weakest_round", "cleanse_round", "pull_crew_round", "wide_wound_round",
            }:
                continue
            enemies = self.living_enemies()
            heroes = self.living_heroes()
            if not enemies or not heroes or self.state.phase != "combat":
                return
            source = enemies[0]
            amount = int(mutation["amount"])
            if effect == "heal_weakest_round":
                target = min(enemies, key=lambda enemy: (enemy.hp * 10_000 // enemy.max_hp, enemy.rank, enemy.id))
                self._apply_effect(source, [target], {"op": "heal", "amount": amount}, source_id=mutation["id"])
                self.add_log(f"{mutation['marker']} — {target.name} repairs {amount} integrity.")
            elif effect == "cleanse_round":
                target = min(enemies, key=lambda enemy: (enemy.hp * 10_000 // enemy.max_hp, enemy.rank, enemy.id))
                removed = next(
                    (status for status in ("stun", "marked", "vulnerable", "weak", "wound")
                     if target.statuses.get(status)),
                    None,
                )
                if removed is None:
                    continue
                previous = target.statuses.pop(removed)
                self.record("cleanse", mutation["id"], target=target.id, status=removed,
                            previous=previous, amount=amount)
                self.add_log(f"{mutation['marker']} — {target.name} purges {removed}.")
            elif effect == "pull_crew_round":
                target = heroes[0]
                before = target.rank
                self._apply_effect(source, [target], {"op": "move", "amount": amount}, source_id=mutation["id"])
                if target.rank != before:
                    self.record("movement", mutation["id"], target=target.id,
                                amount=amount, previous=before, rank=target.rank)
                    self.add_log(f"{mutation['marker']} — {target.name} is pulled to rank {target.rank}.")
            else:
                targets = [hero for hero in heroes if hero.block == 0]
                if not targets:
                    continue
                self._apply_effect(source, targets,
                                   {"op": "status", "status": "wound", "amount": amount},
                                   source_id=mutation["id"])
                self.add_log(f"{mutation['marker']} — unblocked crew suffer wound.")

    def start_combat(
        self,
        encounter_id: str,
        kind: str | None = None,
        surprised: bool = False,
        enemy_ids: list[str] | None = None,
    ) -> None:
        encounter = self.catalog.encounters.get(encounter_id)
        if not encounter:
            raise RuleError(f"unknown encounter: {encounter_id}")
        formation = list(encounter["enemies"] if enemy_ids is None else enemy_ids)
        if not 1 <= len(formation) <= 4 or any(
            enemy_id not in self.catalog.enemies for enemy_id in formation
        ):
            raise RuleError("combat formation must contain one to four known enemies")
        self.state.encounter_pressure = self.state.pressure
        self.state.encounter_modules = self._select_encounter_mutations(
            encounter_id,
            encounter["kind"],
            self.current_biome(),
            len(formation),
        )
        self.state.reinforcement_tickets = 0
        self.state.reinforcement_reserve_id = None
        if "base:reinforcement_call" in self.state.encounter_modules:
            mutation = self.catalog.mutations["base:reinforcement_call"]
            self.state.reinforcement_tickets = int(mutation["amount"])
            self.state.reinforcement_reserve_id = formation[-1]
        self.state.phase = "combat"
        self.resolution.state.combat_token += 1
        self.state.combat_kind = kind or encounter["kind"]
        director = self.current_director()
        self.state.enemies = []
        for rank, enemy_id in enumerate(formation, 1):
            definition = self.catalog.enemies[enemy_id]
            max_hp = max(1, (int(definition["max_hp"]) * director.enemy_health_bp + 5_000) // 10_000)
            self.state.enemies.append(
                Actor(
                    f"{enemy_id}:{rank}",
                    definition["name"],
                    max_hp,
                    max_hp,
                    rank,
                    "enemy",
                    definition_id=enemy_id,
                )
            )
        self.state.draw_pile = [self._clone_card(card) for card in self.state.deck]
        self.rng.shuffle(self.state.draw_pile)
        opening = sorted(
            (
                card for card in self.state.draw_pile
                if (self.infusion_definition(card.infusion_id) or {}).get("mode") == "opening_priority"
            ),
            key=lambda card: card.copy_id,
        )
        if opening:
            opening_ids = {card.copy_id for card in opening}
            self.state.draw_pile = [
                card for card in self.state.draw_pile if card.copy_id not in opening_ids
            ] + list(reversed(opening))
        self.state.discard_pile = []
        self.state.hand = []
        self.state.round = 1
        self.state.effect_counters = {}
        self.state.intents = []
        names = " / ".join(self.catalog.enemies[enemy_id]["name"] for enemy_id in formation)
        self.record("encounter_start", encounter_id, enemies=formation, kind=self.state.combat_kind,
                    plan=self._formation_plan(self.catalog, formation), biome=self.current_biome(),
                    surprised=surprised, crew=[asdict(hero) for hero in self.living_heroes()],
                    pressure=self.state.encounter_pressure,
                    pressure_band=pressure_band(self.state.encounter_pressure).id,
                    modules=list(self.state.encounter_modules))
        self._apply_opening_mutations()
        self.add_log(f"Combat begins: {encounter['id']}. Formation: {names}.")
        self._start_player_turn()
        self.state.intents = self._choose_intents()
        if surprised:
            self.add_log("The crew is surprised in the darkness.")
            self._enemy_phase()
            if self.state.phase != "combat":
                return
            self.state.intents = self._choose_intents()

    def _start_player_turn(self) -> None:
        self.state.effect_counters["focus_cards"] = 0
        for hero in self.living_heroes():
            if hero.block:
                self.record("block_expired", "round:crew", target=hero.id, amount=hero.block)
            hero.block = 0
            self.state.effect_counters[f"round_cards:{hero.id}"] = 0
            self.state.effect_counters[f"countercurrent:{hero.id}"] = 0
            self.state.effect_counters[f"damage_cards:{hero.id}"] = 0
            self.state.effect_counters[f"block_cards:{hero.id}"] = 0
            self._tick_wound(hero)
            if not hero.alive:
                continue
            modifiers = self._affliction_modifiers(hero)
            if modifiers.get("turn_stress"):
                self._change_stress(hero, int(modifiers["turn_stress"]))
            if self.state.round == 1:
                start_block = self._hero_effect_value(hero, "boon", "start_block")
                start_block += self._item_effect_value("stacked_start_block")
                for item_id, count in self.state.items.items():
                    focus = round(self.effect_value("item", item_id, "start_focus", count))
                    if focus:
                        with self.attribution(item_id):
                            self._add_status(hero, "focus", focus)
                dodge = round(self._hero_effect_value(hero, "boon", "start_dodge"))
                if dodge:
                    with self.attribution("vigilance"):
                        self._add_status(hero, "dodge", dodge)
                hero.block += round(start_block)
                relief = round(self._hero_effect_value(hero, "boon", "start_stress_relief"))
                if relief:
                    self._change_stress(hero, -relief)
                marked = round(self._hero_effect_value(hero, "curse", "start_marked"))
                vulnerable = round(self._hero_effect_max_value(hero, "curse", "start_vulnerable"))
                if marked:
                    hero.statuses["marked"] = max(hero.statuses.get("marked", 0), marked + 1)
                if vulnerable:
                    hero.statuses["vulnerable"] = max(
                        hero.statuses.get("vulnerable", 0),
                        vulnerable + 1,
                    )
        if self.state.phase != "combat":
            return
        opening_energy = round(self._item_effect_value("first_round_energy")) if self.state.round == 1 else 0
        environment = self.biome_mechanics()["combat"]
        if self.state.round == 1:
            self._apply_biome_combat_environment(environment)
            opening_energy += sum(
                int(effect["amount"])
                for effect in environment["effects"]
                if effect["op"] == "energy"
            )
        self.state.energy = max(0, self.catalog.balance["energy"] + opening_energy)
        opening_cards = round(self._item_effect_value("opening_hand")) if self.state.round == 1 else 0
        if self.state.round == 1:
            if (self.doctrine_definition() or {}).get("mode") == "discard":
                opening_cards -= 1
            opening_cards += self.state.pending_opening_hand
            opening_cards += sum(
                int(effect["amount"])
                for effect in environment["effects"]
                if effect["op"] == "draw"
            )
            self.state.pending_opening_hand = 0
        self._draw(max(1, self.catalog.balance["hand_size"] + opening_cards) - len(self.state.hand))
        owners = {hero.id: hero for hero in self.living_heroes()}
        rank_invalid = owner_disabled = 0
        for card in self.state.hand:
            if card.card_id in self.catalog.curses:
                owner_disabled += 1
                continue
            definition = self.catalog.cards[card.card_id]
            owner = owners.get(definition["hero"])
            if owner is None or owner.statuses.get("stun"):
                owner_disabled += 1
            elif owner.rank not in definition["from_ranks"]:
                rank_invalid += 1
        self.record("turn_start", "crew", hand=[asdict(card) for card in self.state.hand],
                    rank_invalid=rank_invalid, owner_disabled=owner_disabled, energy=self.state.energy)

    def _apply_biome_combat_environment(self, environment: dict[str, Any]) -> None:
        heroes = self.living_heroes()
        enemies = self.living_enemies()
        for effect in environment["effects"]:
            if effect["op"] in {"draw", "energy"}:
                continue
            target_group = effect["target"]
            if target_group == "all":
                targets = heroes + enemies
            elif target_group == "front_crew":
                targets = heroes[:1]
            elif target_group == "back_crew":
                targets = heroes[-1:]
            elif target_group == "front_enemy":
                targets = enemies[:1]
            elif target_group == "back_enemy":
                targets = enemies[-1:]
            else:
                targets = heroes if target_group == "crew" else enemies
            if effect["op"] == "block":
                for target in targets:
                    target.block += int(effect["amount"])
            elif effect["op"] == "status":
                for target in targets:
                    self._add_status(target, effect["status"], int(effect["amount"]))
            elif effect["op"] == "stress":
                for target in targets:
                    if target.side == "hero":
                        self._change_stress(target, int(effect["amount"]))
            elif effect["op"] == "move":
                for target in targets:
                    self._move(target, int(effect["amount"]))
            elif effect["op"] == "reverse" and effect["target"] == "crew":
                count = len(heroes)
                for hero in heroes:
                    hero.rank = count + 1 - hero.rank
        self.add_log(f"Environment — {environment['name']}: {environment['description']}")

    def living_heroes(self) -> list[Actor]:
        return sorted((actor for actor in self.state.heroes if actor.alive), key=lambda actor: actor.rank)

    def living_enemies(self) -> list[Actor]:
        return sorted((actor for actor in self.state.enemies if actor.hp > 0), key=lambda actor: actor.rank)

    def card_definition(self, card: CardInstance) -> dict[str, Any]:
        if card.card_id in self.catalog.cards:
            return self.catalog.cards[card.card_id]
        return self.catalog.curses[card.card_id]

    def mastery_definition(self, card_id: str) -> dict[str, Any] | None:
        return next(
            (mastery for mastery in self.catalog.masteries.values()
             if mastery["card_id"] == card_id),
            None,
        )

    def mastery_branch(self, card: CardInstance) -> dict[str, Any] | None:
        mastery = self.mastery_definition(card.card_id)
        if mastery is None or card.mastery is None:
            return None
        return next(branch for branch in mastery["branches"] if branch["id"] == card.mastery)

    def infusion_definition(self, infusion_id: str | None) -> dict[str, Any] | None:
        return self.catalog.infusions.get(infusion_id) if infusion_id is not None else None

    def infusion_compatible(self, card_id: str, infusion_id: str) -> bool:
        if card_id not in self.catalog.cards or infusion_id not in self.catalog.infusions:
            return False
        card = self.catalog.cards[card_id]
        infusion = self.catalog.infusions[infusion_id]
        targets = set(infusion["compatible_targets"])
        tags = set(infusion["requires_any_tags"])
        return (not targets or card["target"] in targets) and (
            not tags or bool(tags & self.card_tags(card_id))
        )

    def card_origin_ranks(self, card: CardInstance) -> tuple[int, ...]:
        ranks = set(self.card_definition(card).get("from_ranks", []))
        branch = self.mastery_branch(card)
        if branch is not None and branch["mode"] == "rank_access":
            ranks |= {rank - 1 for rank in ranks if rank > 1}
            ranks |= {rank + 1 for rank in ranks if rank < 4}
        infusion = self.infusion_definition(card.infusion_id)
        if infusion is not None and infusion["mode"] == "rank_access":
            ranks = {1, 2, 3, 4}
        return tuple(sorted(ranks))

    def card_tags(self, card_id: str) -> set[str]:
        return set(self.catalog.cards[card_id]["tags"])

    @staticmethod
    def _stack_value(effect: dict[str, Any], count: int) -> int | float | Fraction:
        if "stack" in effect:
            return persistent_effect(effect).value(count)
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

    def _hero_effect_max_value(self, hero: Actor, group: str, key: str) -> float:
        owned = self.state.boons if group == "boon" else self.state.curses
        return max((self.effect_value(group, effect_id, key, count)
                    for effect_id, count in owned.get(hero.id, {}).items()), default=0)

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
            if "stack" in effect:
                contract = persistent_effect(effect)
                preview = contract.stack.preview(count)
                cap = "none" if preview["cap"] is None else contract.display(preview["cap"])
                values.append(f"{effect['key'].replace('_', ' ')} | Current: {contract.display(preview['current'])}; "
                              f"next: {contract.display(preview['next'])}. {preview['formula']}. "
                              f"{'Soft cap' if preview['soft_cap'] else 'Cap'}: {cap}. "
                              f"Trigger: {trigger_disclosure(contract.key).text()}")
                continue
            value = self._stack_value(effect, count)
            shown = f"{value * 100:.0f}%" if abs(value) < 1 and value else f"{value:g}"
            values.append(f"{effect['key'].replace('_', ' ')} | Current: {shown}")
        return f"{definition['description']} Stacks {count}. {'; '.join(values)}."

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

    def doctrine_definition(self) -> dict[str, Any] | None:
        return self.catalog.doctrines.get(self.state.doctrine_id or "")

    def doctrine_trigger_disclosure(self, doctrine_id: str) -> str:
        doctrine = self.catalog.doctrines[doctrine_id]
        contract = DOCTRINE_TRIGGER_CONTRACTS[doctrine["mode"]]
        descendants = "YES" if contract.descendants_retrigger else "NO"
        concise = {
            "once per turn": "TURN",
            "each manual card": "MANUAL CARD",
            "once per crew death": "CREW DEATH",
        }[contract.limit]
        return f"LIMIT {concise} | DESC: {descendants}"

    @staticmethod
    def _definition_has_effect(
        definition: dict[str, Any],
        effects: list[dict[str, Any]],
        opcode: str,
    ) -> bool:
        return any(effect["op"] == opcode for effect in effects)

    def _doctrine_card_committed(
        self,
        actor: Actor,
        card_id: str,
        definition: dict[str, Any],
        effects: list[dict[str, Any]],
    ) -> None:
        doctrine = self.doctrine_definition()
        if doctrine is None:
            return
        mode = doctrine["mode"]
        cost_delta = 0
        if mode == "dance" and not self._definition_has_effect(definition, effects, "move"):
            if not self.state.effect_counters.get(f"doctrine:dance:liability:{self.state.round}"):
                cost_delta = 1
            self.state.effect_counters[f"doctrine:dance:liability:{self.state.round}"] = 1
        elif mode == "guard" and self._definition_has_effect(definition, effects, "move"):
            cost_delta = 1
        elif mode in {"death_door", "casualty"}:
            if mode == "death_door" and actor.deaths_door:
                cost_delta = -1
            elif mode == "death_door" and all(
                hero.hp == hero.max_hp and not hero.deaths_door for hero in self.living_heroes()
            ) and not self.state.effect_counters.get(f"doctrine:{mode}:first:{self.state.round}"):
                cost_delta = 1
            elif (
                mode == "casualty"
                and len(self.living_heroes()) == 4
                and not self.state.effect_counters.get(f"doctrine:{mode}:first:{self.state.round}")
            ):
                cost_delta = 1
            self.state.effect_counters[f"doctrine:{mode}:first:{self.state.round}"] = 1
        elif mode == "triage" and self._definition_has_effect(definition, effects, "damage"):
            if self.state.effect_counters.get("doctrine:triage:tax"):
                cost_delta = 1
            self.state.effect_counters.pop("doctrine:triage:tax", None)
        if cost_delta:
            self.record(
                "doctrine_trigger",
                doctrine["id"],
                mode=mode,
                card=card_id,
                owner=actor.id,
                effect="cost",
                amount=cost_delta,
                limit="card_or_turn",
            )

    def _doctrine_modify_effect(
        self,
        card_id: str,
        definition: dict[str, Any],
        effect: dict[str, Any],
    ) -> dict[str, Any]:
        doctrine = self.doctrine_definition()
        if doctrine is None:
            return effect
        mode = doctrine["mode"]
        changed = dict(effect)
        key = f"doctrine:{mode}:effect:{self.state.round}"
        bonus = 0
        if (
            mode == "mark"
            and effect["op"] == "status"
            and effect.get("status") == "marked"
            and not self.state.effect_counters.get(key)
        ):
            bonus = 1
        elif (
            mode == "wound"
            and effect["op"] == "status"
            and effect.get("status") == "wound"
            and not self.state.effect_counters.get(key)
        ):
            bonus = 1
        elif (
            mode == "control"
            and effect["op"] == "status"
            and effect.get("status") in {"stun", "weak"}
            and not self.state.effect_counters.get(key)
        ):
            bonus = 1
        elif mode == "guard" and effect["op"] == "block" and any(
            hero.guarded_by for hero in self.living_heroes()
        ):
            bonus = 2
        elif mode == "mark" and effect["op"] == "damage" and "payoff:marked" not in definition["tags"]:
            bonus = -1
        elif mode == "wound" and effect["op"] == "heal":
            bonus = -1
        elif mode == "control" and effect["op"] == "damage":
            bonus = -1
        elif mode == "stress" and effect["op"] == "stress" and int(effect.get("amount", 0)) < 0:
            bonus = min(2, -int(effect["amount"]))
        elif mode == "artillery" and effect["op"] == "damage":
            target = effect.get("target", definition["target"])
            bonus = 2 if target == "all_enemies" else -1
        if not bonus:
            return effect
        amount = int(effect.get("amount", 0)) + bonus
        if effect["op"] in {"damage", "block", "heal"}:
            amount = max(0, amount)
        changed["amount"] = amount
        if mode in {"mark", "wound", "control"}:
            self.state.effect_counters[key] = 1
        self.record(
            "doctrine_trigger",
            doctrine["id"],
            mode=mode,
            card=card_id,
            effect=effect["op"],
            amount=bonus,
        )
        return changed

    def _doctrine_after_card(
        self,
        actor: Actor,
        card_id: str,
        definition: dict[str, Any],
        effects: list[dict[str, Any]],
    ) -> None:
        doctrine = self.doctrine_definition()
        if doctrine is None:
            return
        mode = doctrine["mode"]
        key = f"doctrine:{mode}:trigger:{self.state.round}"
        if self.state.effect_counters.get(key):
            return
        follow_effect: dict[str, Any] | None = None
        if mode == "dance" and self._definition_has_effect(definition, effects, "move"):
            follow_effect = {"op": "draw", "amount": 1}
        elif mode == "discard" and self._definition_has_effect(definition, effects, "discard"):
            follow_effect = {"op": "draw", "amount": 1}
        elif mode == "stress" and any(
            effect["op"] == "stress" and int(effect.get("amount", 0)) > 0
            for effect in effects
        ):
            follow_effect = {"op": "status", "status": "focus", "amount": 1}
        elif mode == "triage" and self._definition_has_effect(definition, effects, "heal"):
            follow_effect = {"op": "energy", "amount": 1}
            self.state.effect_counters["doctrine:triage:tax"] = 1
        if follow_effect is None:
            return
        self.state.effect_counters[key] = 1
        self._apply_effect(actor, [actor], follow_effect, source_id=doctrine["id"])
        self.record(
            "doctrine_trigger",
            doctrine["id"],
            mode=mode,
            card=card_id,
            effect=follow_effect["op"],
            amount=follow_effect["amount"],
            limit="once_per_turn",
        )
        self.add_log(f"DOCTRINE — {doctrine['name']} triggers.")

    def card_cost(self, card: CardInstance) -> int:
        definition = self.card_definition(card)
        if card.card_id in self.catalog.curses:
            return 99
        actor = self._actor(definition["hero"])
        delta = int(self._affliction_modifiers(actor).get("card_cost_delta", 0))
        infusion = self.infusion_definition(card.infusion_id)
        if infusion is not None:
            mode = infusion["mode"]
            discounted = (
                mode == "front_discount" and actor.rank == 1
                or mode == "rear_discount" and actor.rank == 4
                or mode == "wounded_discount" and bool(actor.statuses.get("wound"))
                or mode == "casualty_discount" and any(not hero.alive for hero in self.state.heroes)
            )
            if discounted:
                delta -= infusion["amount"]
        base = definition.get("upgrade_cost", definition["cost"]) if card.upgraded else definition["cost"]
        round_plays = self.state.effect_counters.get(f"round_cards:{actor.id}", 0)
        combat_plays = self.state.effect_counters.get(f"combat_cards:{actor.id}", 0)
        quick_plays = round(self._hero_effect_value(actor, "boon", "quick_hands"))
        if round_plays < quick_plays:
            delta -= 1
        if combat_plays == 0:
            delta += round(self._hero_effect_value(actor, "curse", "first_card_cost_increase"))
        effects = definition["upgrade_effects"] if card.upgraded else definition["effects"]
        doctrine = self.doctrine_definition()
        if doctrine is not None:
            mode = doctrine["mode"]
            has_move = self._definition_has_effect(definition, effects, "move")
            has_damage = self._definition_has_effect(definition, effects, "damage")
            if (
                mode == "dance"
                and not has_move
                and not self.state.effect_counters.get(f"doctrine:dance:liability:{self.state.round}")
            ):
                delta += 1
            elif mode == "guard" and has_move:
                delta += 1
            elif mode == "death_door":
                if actor.deaths_door:
                    delta -= 1
                elif (
                    all(hero.hp == hero.max_hp and not hero.deaths_door for hero in self.living_heroes())
                    and not self.state.effect_counters.get(f"doctrine:death_door:first:{self.state.round}")
                ):
                    delta += 1
            elif (
                mode == "casualty"
                and len(self.living_heroes()) == 4
                and not self.state.effect_counters.get(f"doctrine:casualty:first:{self.state.round}")
            ):
                delta += 1
            elif mode == "triage" and has_damage and self.state.effect_counters.get("doctrine:triage:tax"):
                delta += 1
        if any(effect["op"] == "block" for effect in effects):
            block_plays = self.state.effect_counters.get(f"block_cards:{actor.id}", 0)
            tremors = round(
                self._hero_effect_value(actor, "curse", "first_block_cost_increase")
            )
            if block_plays < tremors:
                delta += 1
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

    def play_card(self, hand_index: int, target_id: str | None = None, *, resolve: bool = True) -> None:
        if self.state.phase != "combat":
            raise RuleError("cards can only be played in combat")
        if not 0 <= hand_index < len(self.state.hand):
            raise RuleError("invalid hand position")
        card = self.state.hand[hand_index]
        definition = self.card_definition(card)
        if card.card_id in self.catalog.curses:
            raise RuleError("curse cards cannot be played")
        actor = self._actor(definition["hero"])
        if not actor.alive or actor.rank not in self.card_origin_ranks(card):
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
        if self.resolution.state.root_id is not None:
            raise RuleError("finish the pending resolution before playing another card")
        root = self.resolution.begin(card_token=card.card_id, combat_token=self.resolution.state.combat_token,
                                     turn_token=self.state.round)
        self.record("card_play", card.card_id, owner=actor.id, rank=actor.rank,
                    energy=cost, target=target_id, upgraded=card.upgraded,
                    copy_id=card.copy_id, mastery=card.mastery, infusion=card.infusion_id,
                    root_action_id=root)
        self.state.energy -= cost
        effects = definition["upgrade_effects"] if card.upgraded else definition["effects"]
        self._doctrine_card_committed(actor, card.card_id, definition, effects)
        self.state.hand.pop(hand_index)
        self.state.discard_pile.append(card)
        infusion = self.infusion_definition(card.infusion_id)
        if infusion is not None and infusion["mode"] == "exhaust":
            self.state.effect_counters[f"infusion:exhaust:{card.copy_id}"] = 1
        for key in (f"round_cards:{actor.id}", f"combat_cards:{actor.id}"):
            self.state.effect_counters[key] = self.state.effect_counters.get(key, 0) + 1
        main_targets = self._card_targets(definition["target"], target_id, actor)
        self.add_log(f"{actor.name} uses {definition['name']}.")
        self.resolution.submit(EventType.CARD_STEP, card.card_id, tuple(target.id for target in main_targets),
                               Payload(actor_id=actor.id, card_id=card.card_id,
                                       card_upgraded=card.upgraded, card_mastery=card.mastery,
                                       card_copy_id=card.copy_id, card_infusion=card.infusion_id,
                                       effect_index=0))
        if resolve:
            self.resolve_pending()

    def _continue_card(self, event: Event, queue: EventQueue) -> None:
        payload = event.payload
        actor = next(actor for actor in self.state.heroes if actor.id == payload.actor_id)
        definition = self.catalog.cards[payload.card_id]
        effects = definition["upgrade_effects"] if payload.card_upgraded else definition["effects"]
        if event.event_type == EventType.CLEANUP:
            if not self.living_enemies() and self.state.phase == "combat":
                if self.state.tutorial and self.state.tutorial_stage == 3:
                    self.state.tutorial_stage = 4
                self._combat_victory()
            elif self.state.tutorial and self.state.tutorial_stage == 3:
                self.state.tutorial_stage = 4
            return
        if not actor.alive or self.state.phase != "combat":
            queue.emit(EventType.CLEANUP, (), payload, mandatory=True, deferred=True)
            return
        index = payload.effect_index
        if event.event_type == EventType.CARD_PLAY:
            if index + 1 >= len(CARD_TRIGGERS):
                self._resolve_card_infusion(event, queue, actor, definition, effects)
                self._doctrine_after_card(actor, payload.card_id, definition, effects)
            next_type = EventType.CARD_PLAY if index + 1 < len(CARD_TRIGGERS) else EventType.CLEANUP
            queue.emit(next_type, event.target_ids, replace(payload, effect_index=index + 1), mandatory=True, deferred=True)
            return
        if index >= len(effects):
            queue.emit(EventType.CARD_PLAY, event.target_ids, replace(payload, effect_index=0), mandatory=True, deferred=True)
            return
        actors = {actor.id: actor for actor in self.state.heroes + self.state.enemies}
        main_targets = [actors[identity] for identity in event.target_ids]
        effect = effects[index]
        condition_met = self._card_effect_condition(effect, actor, main_targets)
        conditions = {key: effect[key] for key in ("condition_status", "condition_actor_state", "condition_target_state") if key in effect}
        if conditions:
            self.record("condition", payload.card_id, conditions=conditions, activated=condition_met)
        if condition_met:
            resolved_effect = dict(effect)
            mastery = self.mastery_definition(payload.card_id)
            if payload.card_mastery is not None and mastery is not None:
                branch = next(
                    branch for branch in mastery["branches"]
                    if branch["id"] == payload.card_mastery
                )
                if branch["mode"] == "effect_bonus" and branch["effect_index"] == index:
                    resolved_effect["amount"] = int(resolved_effect.get("amount", 0)) + branch["amount"]
                    self.record("mastery_effect", mastery["id"], card=payload.card_id,
                                branch=payload.card_mastery, effect_index=index,
                                amount=branch["amount"])
            infusion = self.infusion_definition(payload.card_infusion)
            infusion_effect_positions = [
                position for position, candidate in enumerate(effects)
                if candidate["op"] in {"damage", "block", "heal"}
            ]
            if (
                infusion is not None
                and infusion["mode"] == "pressure_bonus"
                and self.state.pressure >= 480
                and resolved_effect["op"] in {"damage", "block", "heal"}
                and infusion_effect_positions
                and index == infusion_effect_positions[0]
            ):
                resolved_effect["amount"] = int(resolved_effect.get("amount", 0)) + infusion["amount"]
                self.record("infusion_trigger", infusion["id"], card=payload.card_id,
                            copy_id=payload.card_copy_id, mode=infusion["mode"],
                            amount=infusion["amount"])
            if definition.get("biome") == self.current_biome():
                bonus = int(definition.get("biome_bonus", 0))
                if resolved_effect["op"] in {"damage", "block", "heal"}:
                    resolved_effect["amount"] = int(resolved_effect.get("amount", 0)) + bonus
                elif resolved_effect["op"] == "stress" and resolved_effect.get("amount", 0) < 0:
                    resolved_effect["amount"] -= bonus
            resolved_effect = self._doctrine_modify_effect(
                payload.card_id,
                definition,
                resolved_effect,
            )
            targets = self._effect_targets(effect.get("target"), main_targets, actor)
            self._apply_effect(actor, targets, resolved_effect, source_id=payload.card_id)
            if (
                index == 0
                and (self.infusion_definition(payload.card_infusion) or {}).get("mode") == "echo_first"
            ):
                self.state.effect_counters[
                    f"infusion:first:{event.root_action_id}:{payload.card_copy_id}"
                ] = 1
        queue.emit(EventType.CARD_STEP, event.target_ids, replace(payload, effect_index=index + 1), mandatory=True, deferred=True)

    def _infusion_limit_key(self, infusion: dict[str, Any], copy_id: int) -> str:
        suffix = f":{self.state.round}" if infusion["limit"] == "turn" else ""
        return (
            f"infusion:trigger:{infusion['id']}:{copy_id}:"
            f"{self.resolution.state.combat_token}{suffix}"
        )

    def _resolve_card_infusion(
        self,
        event: Event,
        queue: EventQueue,
        actor: Actor,
        definition: dict[str, Any],
        effects: list[dict[str, Any]],
    ) -> None:
        payload = event.payload
        infusion = self.infusion_definition(payload.card_infusion)
        if infusion is None or infusion["limit"] == "none":
            return
        key = self._infusion_limit_key(infusion, payload.card_copy_id)
        if self.state.effect_counters.get(key):
            return
        mode = infusion["mode"]
        main = [
            candidate for candidate in self.state.heroes + self.state.enemies
            if candidate.id in event.target_ids
        ]
        activated = False
        if mode == "echo_first" and self.state.effect_counters.get(
            f"infusion:first:{event.root_action_id}:{payload.card_copy_id}"
        ):
            effect = dict(effects[0])
            mastery = self.mastery_definition(payload.card_id)
            if payload.card_mastery is not None and mastery is not None:
                branch = next(branch for branch in mastery["branches"]
                              if branch["id"] == payload.card_mastery)
                if branch["mode"] == "effect_bonus" and branch["effect_index"] == 0:
                    effect["amount"] = int(effect.get("amount", 0)) + branch["amount"]
            if definition.get("biome") == self.current_biome():
                bonus = int(definition.get("biome_bonus", 0))
                if effect["op"] in {"damage", "block", "heal"}:
                    effect["amount"] = int(effect.get("amount", 0)) + bonus
                elif effect["op"] == "stress" and effect.get("amount", 0) < 0:
                    effect["amount"] -= bonus
            self._apply_effect(
                actor,
                self._effect_targets(effect.get("target"), main, actor),
                effect,
                source_id=infusion["id"],
            )
            activated = True
        elif mode == "follow_draw":
            self._apply_effect(actor, [actor], {"op": "draw", "amount": infusion["amount"]},
                               source_id=infusion["id"])
            activated = True
        elif mode == "movement_refund":
            self._apply_effect(actor, [actor], {"op": "energy", "amount": infusion["amount"]},
                               source_id=infusion["id"])
            activated = True
        elif mode == "self_cleanse" and any(
            status in actor.statuses for status in ("marked", "stun", "vulnerable", "weak", "wound")
        ):
            self._apply_effect(actor, [actor], {"op": "cleanse", "amount": 1},
                               source_id=infusion["id"])
            activated = True
        elif mode == "front_focus" and actor.rank == 1:
            self._apply_effect(
                actor, [actor], {"op": "status", "status": "focus", "amount": infusion["amount"]},
                source_id=infusion["id"],
            )
            activated = True
        elif mode == "mark_after_damage":
            targets = [target for target in main if target.side == "enemy" and target.alive]
            if targets:
                self._apply_effect(
                    actor, targets,
                    {"op": "status", "status": "marked", "amount": infusion["amount"]},
                    source_id=infusion["id"],
                )
                activated = True
        elif mode == "wound_transfer":
            target = next(
                (target for target in main
                 if target.side == "hero" and target.id != actor.id and target.statuses.get("wound")),
                None,
            )
            if target is not None:
                target.statuses["wound"] -= infusion["amount"]
                if target.statuses["wound"] <= 0:
                    del target.statuses["wound"]
                self._apply_effect(
                    actor, [actor],
                    {"op": "status", "status": "wound", "amount": infusion["amount"]},
                    source_id=infusion["id"],
                )
                activated = True
        if activated:
            self.state.effect_counters[key] = 1
            self.record("infusion_trigger", infusion["id"], card=payload.card_id,
                        copy_id=payload.card_copy_id, mode=mode, amount=infusion["amount"])
            self.add_log(f"{infusion['marker']} — {infusion['name']} triggers.")

    def _card_trigger(self, listener: Listener, event: Event, queue: EventQueue) -> bool:
        actor = next(actor for actor in self.state.heroes if actor.id == listener.entity_id)
        if not actor.alive or self.state.phase != "combat":
            return False
        definition = self.catalog.cards[event.payload.card_id]
        effects = definition["upgrade_effects"] if event.payload.card_upgraded else definition["effects"]
        index = event.payload.effect_index
        op, amount = None, 0
        if index == 0:
            if self.state.effect_counters[f"combat_cards:{actor.id}"] % 3 == 0:
                op, amount = "energy", round(self._hero_effect_value(actor, "boon", "resonant_energy"))
                if amount:
                    self.add_log(f"{actor.name}'s Resonant Circuit returns energy.")
        elif index == 1:
            key = f"countercurrent:{actor.id}"
            if any(effect["op"] == "move" for effect in effects) and not self.state.effect_counters.get(key):
                op, amount = "draw", round(self._hero_effect_value(actor, "boon", "countercurrent_draw"))
                if amount:
                    self.state.effect_counters[key] = 1
        elif index in (2, 3) and any(effect["op"] == "damage" for effect in effects):
            key = f"damage_cards:{actor.id}"
            plays = self.state.effect_counters.get(key, 0)
            if index == 2:
                limit = round(self._hero_effect_value(actor, "curse", "damage_discard"))
                if plays < limit and self.state.hand:
                    op, amount = "discard", 1
                    self.add_log(f"Frayed Focus discards {self.card_definition(self.state.hand[-1])['name']}.")
            else:
                limit = round(self._hero_effect_value(actor, "boon", "damage_draw"))
                if plays < limit:
                    op, amount = "draw", 1
                    self.add_log(f"{actor.name}'s Hunter's Rhythm draws a card.")
                self.state.effect_counters[key] = plays + 1
        elif index == 4 and any(effect["op"] == "block" for effect in effects):
            key = f"block_cards:{actor.id}"
            self.state.effect_counters[key] = self.state.effect_counters.get(key, 0) + 1
        elif index == 5 and any(effect["op"] == "status" and effect.get("status") == "focus" for effect in effects):
            plays = self.state.effect_counters.get("focus_cards", 0)
            if plays < round(self._item_effect_value("focus_draw")):
                op, amount = "draw", 1
                self.add_log("Focusing Lens converts focus into another draw.")
            self.state.effect_counters["focus_cards"] = plays + 1
        elif index == 6 and self.state.energy == 0 and not self.state.effect_counters.get("reserve_energy"):
            op, amount = "energy", round(self._item_effect_value("reserve_energy"))
            if amount:
                self.state.effect_counters["reserve_energy"] = 1
                self.add_log(f"Reserve Cell restores {amount} energy.")
        if op and amount:
            queue.emit(EventType(op), (actor.id,), Payload(actor_id=actor.id, opcode=Opcode(op), amount=amount),
                       source_id=listener.spec.id)
            return True
        return False

    @staticmethod
    def _actor_matches_state(actor: Actor, state: str) -> bool:
        if state == "deaths_door":
            return actor.deaths_door
        if state == "stressed":
            return actor.stress >= 50
        if state == "healthy":
            return actor.hp * 2 >= actor.max_hp
        return bool(actor.statuses.get("wound"))

    def _card_effect_condition(
        self,
        effect: dict[str, Any],
        actor: Actor,
        main_targets: list[Actor],
    ) -> bool:
        if effect.get("condition_status") and not any(
            effect["condition_status"] in target.statuses for target in main_targets
        ):
            return False
        if effect.get("condition_target_state") and not any(
            self._actor_matches_state(target, effect["condition_target_state"])
            for target in main_targets
        ):
            return False
        if effect.get("condition_actor_state") and not self._actor_matches_state(
            actor,
            effect["condition_actor_state"],
        ):
            return False
        return True

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

    def _apply_effect(self, actor: Actor, targets: list[Actor], effect: dict[str, Any], *, source_id: str | None = None) -> None:
        source = source_id or self._source_id or actor.id
        payload = Payload(actor_id=actor.id, opcode=Opcode(effect["op"]), amount=int(effect.get("amount", 0)),
                          status=effect.get("status"), bonus_status=effect.get("bonus_status"),
                          bonus=int(effect.get("bonus", 0)), card_id=source if source in self.catalog.cards else None)
        event_type = EventType(effect["op"])
        target_ids = tuple(target.id for target in targets)
        if self.resolution.state.active is not None:
            self.resolution.emit(event_type, target_ids, payload, source_id=source)
            return
        owns_root = self.resolution.state.root_id is None
        if owns_root:
            self.resolution.begin(card_token=payload.card_id, combat_token=self.resolution.state.combat_token,
                                  turn_token=self.state.round)
        self.resolution.submit(event_type, source, target_ids, payload)
        self.resolve_pending(close_root=owns_root)

    def _resolution_listeners(self, event: Event) -> tuple[Listener, ...]:
        actor_order = {
            actor.id: index
            for index, actor in enumerate(self.state.heroes + self.state.enemies, 1)
        }
        modules = set(self.state.encounter_modules)
        if event.event_type == EventType.DEATH:
            fallen = next(
                (actor for actor in self.state.enemies if actor.id == event.payload.actor_id),
                None,
            )
            if fallen is None:
                return ()
            survivors = self.living_enemies()
            specs = tuple(
                spec
                for spec in (DEATH_SURGE, SPORE_LINK)
                if spec.id in modules and survivors
            )
            listeners = tuple(
                Listener(spec, actor_order[survivors[0].id], survivors[0].id)
                for spec in specs
            )
            if (REINFORCEMENT_CALL.id in modules and self.state.reinforcement_tickets
                and self.state.reinforcement_reserve_id is not None):
                listeners += (Listener(REINFORCEMENT_CALL, actor_order[fallen.id], fallen.id),)
            return listeners
        if event.event_type == EventType.STATUS and RIME_SHELL.id in modules:
            target = next(
                (actor for actor in self.living_enemies() if actor.id in event.target_ids),
                None,
            )
            if target is not None and event.payload.status == "stun":
                return (Listener(RIME_SHELL, actor_order[target.id], target.id),)
        if event.event_type == EventType.HEAL:
            return tuple(Listener(MERCY, index, actor.id) for index, actor in enumerate(self.state.heroes, 1)
                         if actor.id == event.payload.actor_id and actor.alive
                         and self._hero_effect_value(actor, "boon", "mercy_block"))
        if event.event_type in {EventType.CARD_DRAW, EventType.CARD_HELD}:
            spec = next(spec for spec in CURSE_TRIGGERS if spec.listens == event.event_type)
            return tuple(Listener(spec, index, actor.id) for index, actor in enumerate(self.state.heroes, 1)
                         if actor.id == event.payload.actor_id and actor.alive)
        if event.event_type == EventType.CARD_PLAY:
            listeners = tuple(Listener(CARD_TRIGGERS[event.payload.effect_index], index, actor.id)
                              for index, actor in enumerate(self.state.heroes, 1)
                              if actor.id == event.payload.actor_id and actor.alive)
            if event.payload.effect_index == 0 and THIRD_BELL.id in modules and self.living_enemies():
                owner = self.living_enemies()[0]
                listeners += (Listener(THIRD_BELL, actor_order[owner.id], owner.id),)
            return listeners
        if event.event_type != EventType.DAMAGE:
            return ()
        ripostes = tuple(Listener(RIPOSTE, index, actor.id)
                     for index, actor in enumerate(self.state.heroes + self.state.enemies, 1)
                     if actor.alive and actor.statuses.get("riposte"))
        adrenal = tuple(Listener(ADRENAL, index, actor.id) for index, actor in enumerate(self.state.heroes, 1)
                        if actor.alive and self._hero_effect_value(actor, "boon", "adrenal_block")
                        and not self.state.effect_counters.get(f"adrenal:{actor.id}"))
        return ripostes + adrenal

    def _resolve_trigger(self, listener: Listener, event: Event, queue: EventQueue) -> bool:
        if listener.spec in (THIRD_BELL, DEATH_SURGE, REINFORCEMENT_CALL, RIME_SHELL, SPORE_LINK):
            return self._mutation_trigger(listener, event, queue)
        if listener.spec in (MERCY, ADRENAL):
            return self._reactive_block(listener, event, queue)
        if listener.spec in CURSE_TRIGGERS:
            return self._curse_trigger(listener, event, queue)
        if listener.spec in CARD_TRIGGERS:
            return self._card_trigger(listener, event, queue)
        if listener.spec != RIPOSTE:
            raise RuleError(f"unregistered automatic trigger {listener.spec.id}")
        actors = {actor.id: actor for actor in self.state.heroes + self.state.enemies}
        defender = actors[listener.entity_id]
        if not defender.alive or not defender.statuses.get("riposte"):
            return False
        for row in reversed(self.state.ledger.records):
            if row.kind != "damage" or row.data.get("event_id") != event.event_id:
                continue
            hit = row.data
            attacker = actors.get(hit.get("attacker"))
            if (hit["target"] == defender.id and hit["amount"] > 0 and not hit["was_deaths_door"]
                and attacker and attacker.alive and attacker.side != defender.side):
                self.add_log(f"{defender.name} answers with a riposte.")
                queue.emit(EventType.DAMAGE, (attacker.id,),
                           Payload(actor_id=defender.id, opcode=Opcode.DAMAGE, amount=4, raw_damage=True),
                           source_id=f"status:riposte:{defender.id}")
                return True
        return False

    def _mutation_trigger(self, listener: Listener, event: Event, queue: EventQueue) -> bool:
        mutation = self.catalog.mutations[listener.spec.id]
        amount = int(mutation["amount"])
        enemies = self.living_enemies()
        heroes = self.living_heroes()
        if listener.spec == REINFORCEMENT_CALL:
            reserve_id = self.state.reinforcement_reserve_id
            if not self.state.reinforcement_tickets or reserve_id is None or len(enemies) >= 4:
                return False
            definition = self.catalog.enemies[reserve_id]
            director = self.current_director()
            max_hp = max(
                1,
                (int(definition["max_hp"]) * director.enemy_health_bp + 5_000) // 10_000,
            )
            reinforcement = Actor(
                f"{reserve_id}:reserve:{self.resolution.state.combat_token}",
                definition["name"],
                max_hp,
                max_hp,
                len(enemies) + 1,
                "enemy",
                definition_id=reserve_id,
            )
            self.state.enemies.append(reinforcement)
            self.state.reinforcement_tickets -= 1
            self.record("reinforcement", listener.spec.id, event_id=event.event_id,
                        enemy=reserve_id, actor=reinforcement.id, rank=reinforcement.rank)
            self.add_log(
                f"REINFORCE:1 — reserve {definition['name']} enters rank {reinforcement.rank}."
            )
            return True
        if listener.spec == RIME_SHELL:
            if event.payload.status != "stun" or not any(
                enemy.id in event.target_ids for enemy in enemies
            ):
                return False
            queue.prevent()
            self.add_log("RESIST:STUN — Rime Shell consumes the first stun.")
            self.record("mutation_reaction", listener.spec.id, event_id=event.event_id,
                        effect="resist_first_stun", targets=list(event.target_ids))
            return True
        fallen = next(
            (enemy for enemy in self.state.enemies if enemy.id == event.payload.actor_id),
            None,
        )
        if listener.spec in (DEATH_SURGE, SPORE_LINK):
            if fallen is None or not enemies:
                return False
            if listener.spec == DEATH_SURGE:
                target = min(enemies, key=lambda enemy: (abs(enemy.rank - fallen.rank), enemy.rank, enemy.id))
                queue.emit(EventType.STATUS, (target.id,),
                           Payload(actor_id=listener.entity_id, opcode=Opcode.STATUS,
                                   amount=amount, status="focus"),
                           source_id=listener.spec.id)
                message = "SURGE:ALLY_DIES — the nearest hostile gains focus."
            else:
                if not heroes:
                    return False
                target = heroes[0]
                queue.emit(EventType.STATUS, (target.id,),
                           Payload(actor_id=listener.entity_id, opcode=Opcode.STATUS,
                                   amount=amount, status="wound"),
                           source_id=listener.spec.id)
                message = "SURGE:WOUND — spores wound the front crew member."
            self.add_log(message)
            self.record("mutation_reaction", listener.spec.id, event_id=event.event_id,
                        effect=mutation["effect"], target=target.id)
            return True
        if listener.spec == THIRD_BELL:
            cards = sum(
                self.state.effect_counters.get(f"round_cards:{hero.id}", 0)
                for hero in self.state.heroes
            )
            if cards != 3 or not enemies:
                return False
            target = enemies[0]
            queue.emit(EventType.BLOCK, (target.id,),
                       Payload(actor_id=listener.entity_id, opcode=Opcode.BLOCK, amount=amount),
                       source_id=listener.spec.id)
            self.add_log("REACT:3RD_CARD — the front hostile gains block.")
            self.record("mutation_reaction", listener.spec.id, event_id=event.event_id,
                        effect=mutation["effect"], target=target.id)
            return True
        return False

    def _reactive_block(self, listener: Listener, event: Event, queue: EventQueue) -> bool:
        actors = {actor.id: actor for actor in self.state.heroes + self.state.enemies}
        owner = actors[listener.entity_id]
        if not owner.alive:
            return False
        activated = False
        for row in self.state.ledger.records:
            if row.data.get("event_id") != event.event_id:
                continue
            target = actors.get(row.data.get("target"))
            if listener.spec == MERCY and row.kind == "healing" and target and target.alive and target.side == "hero" and target.id != owner.id:
                amount = round(self._hero_effect_value(owner, "boon", "mercy_block"))
            elif listener.spec == ADRENAL and row.kind == "damage" and target == owner and owner.hp > 0:
                attacker = actors.get(row.data.get("attacker"))
                key = f"adrenal:{owner.id}"
                if not attacker or attacker.side != "enemy" or row.data["amount"] <= 0 or self.state.effect_counters.get(key):
                    continue
                amount = round(self._hero_effect_value(owner, "boon", "adrenal_block"))
                self.state.effect_counters[key] = 1
            else:
                continue
            if amount:
                queue.emit(EventType.BLOCK, (target.id,), Payload(actor_id=owner.id, opcode=Opcode.BLOCK, amount=amount),
                           source_id=listener.spec.id)
                activated = True
        return activated

    def _resolve_event(self, event: Event, queue: EventQueue) -> None:
        payload = event.payload
        if event.event_type == EventType.DEATH:
            self.record("death_resolved", event.source_id, owner=payload.actor_id)
            return
        if event.event_type in {EventType.CARD_DRAW, EventType.CARD_HELD}:
            self.record(event.event_type.value, payload.card_id, owner=payload.actor_id)
            return
        if event.event_type in {EventType.CARD_STEP, EventType.CARD_PLAY, EventType.CLEANUP}:
            self._continue_card(event, queue)
            return
        if payload.actor_id is None and not payload.raw_damage and payload.opcode != Opcode.HEAL or payload.opcode is None:
            raise RuleError("queued primary effect is missing its actor or opcode")
        actors = {actor.id: actor for actor in self.state.heroes + self.state.enemies}
        actor = actors.get(payload.actor_id)
        if actor is not None and not actor.alive:
            self.record("owner_disabled_event", event.source_id, owner=actor.id)
            return
        targets = [actors[identity] for identity in event.target_ids if actors[identity].alive]
        if actor is None and payload.opcode == Opcode.HEAL:
            with self.attribution(event.source_id):
                for target in targets:
                    self._heal_primary(target, payload.amount)
            return
        if payload.raw_damage:
            with self.attribution(event.source_id):
                for target in targets:
                    self._damage_primary(target, payload.amount, None if "riposte" in event.proc_families else actor)
            return
        effect = {"op": payload.opcode.value, "amount": payload.amount}
        if payload.status is not None:
            effect["status"] = payload.status
        if payload.bonus_status is not None:
            effect.update(bonus_status=payload.bonus_status, bonus=payload.bonus)
        with self.attribution(event.source_id):
            self.record("effect", event.source_id, actor=actor.id, targets=list(event.target_ids), effect=effect)
            self._apply_effect_primary(actor, targets, effect)

    def resolve_pending(self, *, close_root: bool = True) -> None:
        previous_seals = len(self.resolution.state.seals)
        root = self.resolution.state.root_id
        self.resolution.drain(self._resolution_listeners, self._resolve_event, self._resolve_trigger, close_root=close_root)
        if close_root and root is not None:
            self.record("resolution_root", "resolution", root=root, trace=self.resolution.state.trace)
        for seal in self.resolution.state.seals[previous_seals:]:
            self.add_log("CHAIN SEALED: an automatic chain exceeded its event budget.")
            self.record("chain_sealed", "resolution", trace=seal)

    def _apply_effect_primary(self, actor: Actor, targets: list[Actor], effect: dict[str, Any]) -> None:
        op = effect["op"]
        amount = int(effect.get("amount", 0))
        if op == "draw":
            self._draw(amount)
        elif op == "discard":
            for _ in range(min(amount, len(self.state.hand))):
                self.state.discard_pile.append(self.state.hand.pop())
        elif op == "energy":
            previous = self.state.energy
            self.state.energy = max(0, previous + amount)
            self.record("energy", self._source_id or actor.id, requested=amount, amount=self.state.energy - previous, result=self.state.energy)
        else:
            for target in list(targets):
                if not target.alive and op != "heal":
                    continue
                if op == "damage":
                    adjusted = amount
                    if effect.get("bonus_status") in target.statuses:
                        adjusted += int(effect.get("bonus", 0))
                        self.record("payoff", self._source_id or actor.id, mechanic=effect["bonus_status"],
                                    target=target.id, bonus=int(effect.get("bonus", 0)))
                    adjusted = self._outgoing_damage(actor, adjusted, target)
                    self._damage(target, adjusted, actor)
                elif op == "block":
                    automatic = self.resolution.state.active and self.resolution.state.active.event.proc_families
                    multiplier = 1 if automatic else float(self._affliction_modifiers(target).get("block_mult", 1))
                    multiplier = max(0.5, min(2.0, multiplier))
                    gained = max(0, round(amount * multiplier))
                    target.block += gained
                    self.record("block", self._source_id or actor.id, target=target.id, amount=gained)
                elif op == "heal":
                    self._heal(target, amount, actor)
                elif op == "stress" and target.side == "hero":
                    self._change_stress(target, amount)
                elif op == "move":
                    self._move(target, amount, actor)
                elif op == "guard" and target.side == actor.side and target.id != actor.id:
                    target.guarded_by = actor.id
                    target.guard_turns = amount
                elif op == "status":
                    self._add_status(target, effect["status"], amount)
                elif op == "cleanse":
                    for status in ("marked", "stun", "vulnerable", "weak", "wound"):
                        target.statuses.pop(status, None)

    def end_turn(
        self,
        playback: Callable[[dict[str, Any]], None] | None = None,
    ) -> None:
        if self.state.phase != "combat":
            raise RuleError("there is no combat turn to end")
        if self.resolution.state.root_id is not None:
            raise RuleError("finish the pending resolution before ending the turn")
        self.record("turn_end", "crew", energy_unspent=self.state.energy,
                    hand=[asdict(card) for card in self.state.hand])
        for card in self.state.hand:
            if card.card_id == "dread_forecast":
                self._trigger_curse_card(card, "curse_held_stress")
        retained = [
            card for card in self.state.hand
            if (self.infusion_definition(card.infusion_id) or {}).get("mode") == "retain"
        ]
        self.state.discard_pile.extend(
            card for card in self.state.hand if card not in retained
        )
        self.state.hand = retained
        for hero in self.living_heroes():
            if hero.statuses.get("stun"):
                hero.statuses["stun"] -= 1
                if hero.statuses["stun"] <= 0:
                    del hero.statuses["stun"]
            self._decay_statuses(hero)
        self._enemy_phase(playback)
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
        if self.state.tutorial and self.state.tutorial_stage == 5:
            self.state.tutorial_stage = 6

    @classmethod
    def _action_setup_statuses(cls, action: dict[str, Any]) -> set[str]:
        return {
            effect["status"]
            for effect in action["effects"]
            if effect["op"] == "status" and cls._effect_targets_crew(action, effect)
        }

    @staticmethod
    def _action_exploit_statuses(action: dict[str, Any]) -> set[str]:
        return {
            effect["bonus_status"]
            for effect in action["effects"]
            if effect["op"] == "damage" and effect.get("bonus_status")
        }

    def _enemy_action_weight(
        self,
        enemy: Actor,
        action: dict[str, Any],
        planned_statuses: set[str] | None = None,
        formation_exploits: set[str] | None = None,
    ) -> float:
        planned_statuses = planned_statuses or set()
        formation_exploits = formation_exploits or set()
        weight = float(action.get("weight", 1))
        heroes = self.living_heroes()
        allies = self.living_enemies()
        crew_statuses = set().union(*(hero.statuses for hero in heroes)) if heroes else set()
        setup_statuses = self._action_setup_statuses(action)
        exploit_statuses = self._action_exploit_statuses(action)
        offensive = any(
            self._effect_targets_crew(action, effect)
            and effect["op"] in {"damage", "stress", "move", "status"}
            for effect in action["effects"]
        )

        if exploit_statuses:
            primed = exploit_statuses & (crew_statuses | planned_statuses)
            weight *= 2.5 if primed else 0.7
            if primed:
                weight *= 1 + self.current_director().coordination * 0.15
        if setup_statuses:
            missing = setup_statuses - (crew_statuses | planned_statuses)
            if setup_statuses & formation_exploits:
                weight *= 1.8 if missing else 0.65
                if missing:
                    weight *= 1 + self.current_director().coordination * 0.1
            else:
                weight *= 1.15 if missing else 0.75

        if action["target"] in {"weakest_enemy", "weakest_ally"} and allies:
            candidates = (
                [ally for ally in allies if ally.id != enemy.id]
                if action["target"] == "weakest_ally"
                else allies
            )
            support_target = min(candidates or allies, key=lambda actor: actor.hp / actor.max_hp)
        else:
            support_target = enemy
        healing = sum(
            int(effect.get("amount", 0))
            for effect in action["effects"]
            if effect["op"] == "heal" and not self._effect_targets_crew(action, effect)
        )
        blocking = sum(
            int(effect.get("amount", 0))
            for effect in action["effects"]
            if effect["op"] == "block" and not self._effect_targets_crew(action, effect)
        )
        if healing:
            health_ratio = support_target.hp / support_target.max_hp
            if health_ratio <= 0.55:
                weight *= 2.8
            elif health_ratio < 0.85:
                weight *= 1.6
            elif not offensive:
                weight *= 0.2
        if blocking:
            if support_target.block == 0:
                weight *= 1.25
            elif support_target.block >= max(blocking, support_target.max_hp // 3):
                weight *= 0.5 if not offensive else 0.8

        positive_statuses = {
            effect["status"]
            for effect in action["effects"]
            if effect["op"] == "status" and not self._effect_targets_crew(action, effect)
        }
        if positive_statuses:
            weight *= 0.65 if positive_statuses <= support_target.statuses.keys() else 1.25
        if enemy.last_action == action["name"]:
            weight *= max(0.35, 0.72 ** enemy.action_repeats)
        return max(0.05, weight)

    @staticmethod
    def _intent_target_label(actor: Actor) -> str:
        if actor.side == "hero":
            return f"R{actor.rank} {actor.hero_class[:4].upper()}"
        short_name = "".join(word[0] for word in actor.name.split()).upper()[:4]
        return f"R{actor.rank} {short_name}"

    def _choose_intents(self) -> list[dict[str, Any]]:
        intents = []
        enemies = self.living_enemies()
        formation_exploits = set().union(
            *(
                self._definition_exploit_statuses(
                    self.catalog.enemies[enemy.definition_id or enemy.id]
                )
                for enemy in enemies
            )
        ) if enemies else set()
        planned_statuses: set[str] = set()
        for enemy in enemies:
            actions = self.catalog.enemies[enemy.definition_id or enemy.id]["actions"]
            weights = [
                self._enemy_action_weight(
                    enemy,
                    action,
                    planned_statuses,
                    formation_exploits,
                )
                for action in actions
            ]
            action = self.rng.choices(actions, weights=weights, k=1)[0]
            targets = self._enemy_targets(action["target"], enemy)
            target_labels = (
                ["ALL CREW"]
                if action["target"] == "all_heroes"
                else [self._intent_target_label(target) for target in targets]
            )
            intents.append(
                {
                    "enemy_rank": enemy.rank,
                    "enemy_id": enemy.id,
                    "action": action["name"],
                    "target_rule": action["target"],
                    "target_ids": [target.id for target in targets],
                    "target_labels": target_labels,
                }
            )
            if enemy.last_action == action["name"]:
                enemy.action_repeats += 1
            else:
                enemy.last_action = action["name"]
                enemy.action_repeats = 1
            planned_statuses |= self._action_setup_statuses(action)
        return intents

    def _combat_actor_snapshot(self) -> dict[str, dict[str, Any]]:
        return {
            actor.id: {
                "name": actor.name,
                "hp": actor.hp,
                "block": actor.block,
                "stress": actor.stress,
                "rank": actor.rank,
                "alive": actor.alive,
                "statuses": dict(actor.statuses),
            }
            for actor in self.state.heroes + self.state.enemies
        }

    @staticmethod
    def _enemy_action_changes(
        before: dict[str, dict[str, Any]],
        after: dict[str, dict[str, Any]],
    ) -> list[str]:
        changes = []
        for actor_id, earlier in before.items():
            later = after[actor_id]
            name = earlier["name"]
            for field, label in (("hp", "HP"), ("block", "BLOCK"), ("stress", "STRESS")):
                delta = later[field] - earlier[field]
                if delta:
                    changes.append(f"{name} {delta:+d} {label}")
            if later["rank"] != earlier["rank"]:
                changes.append(f"{name} R{earlier['rank']}->R{later['rank']}")
            statuses = set(earlier["statuses"]) | set(later["statuses"])
            for status in sorted(statuses):
                old = earlier["statuses"].get(status, 0)
                new = later["statuses"].get(status, 0)
                if new > old:
                    changes.append(f"{name} +{status.upper()} {new}")
                elif old and not new:
                    changes.append(f"{name} -{status.upper()}")
            if earlier["alive"] and not later["alive"]:
                changes.append(f"{name} DEAD")
        return changes

    def _enemy_phase(
        self,
        playback: Callable[[dict[str, Any]], None] | None = None,
    ) -> None:
        # A legal end-turn or surprise commits this world action. Charge it once
        # here so a fatal final action and a wound-killed final enemy are both
        # represented; playback timing and inspection never enter this method.
        self._advance_pressure(
            PressureSource.ENEMY_ROUND,
            1,
            f"combat round {self.state.round}",
        )
        for hero in self.living_heroes():
            self.state.effect_counters[f"adrenal:{hero.id}"] = 0
        intents = list(self.state.intents)
        for intent in intents:
            if self.state.phase != "combat":
                return
            enemy = next((item for item in self.living_enemies() if item.id == intent["enemy_id"]), None)
            if enemy is None:
                continue
            if enemy.block:
                self.record("block_expired", "round:enemy", target=enemy.id, amount=enemy.block)
            enemy.block = 0
            self._tick_wound(enemy)
            if enemy.hp <= 0:
                self._normalize_ranks("enemy")
                if not self.living_enemies():
                    self._combat_victory()
                    return
                continue
            if enemy.statuses.get("stun", 0):
                before = self._combat_actor_snapshot()
                enemy.statuses["stun"] -= 1
                if enemy.statuses["stun"] <= 0:
                    del enemy.statuses["stun"]
                self.add_log(f"{enemy.name} is stunned.")
                self.record("control_skip", "status:stun", target=enemy.id)
                self._decay_statuses(enemy)
                if playback:
                    playback(
                        {
                            "actor_id": enemy.id,
                            "actor_name": enemy.name,
                            "actor_rank": intent["enemy_rank"],
                            "action": "STUNNED",
                            "target_labels": [self._intent_target_label(enemy)],
                            "setup": [],
                            "payoff": [],
                            "changes": self._enemy_action_changes(
                                before,
                                self._combat_actor_snapshot(),
                            ),
                        }
                    )
                continue
            actions = self.catalog.enemies[enemy.definition_id or enemy.id]["actions"]
            action = next(item for item in actions if item["name"] == intent["action"])
            if "target_ids" in intent:
                living = {actor.id: actor for actor in self.living_heroes() + self.living_enemies()}
                targets = [living[target_id] for target_id in intent["target_ids"] if target_id in living]
            else:
                targets = self._enemy_targets(action["target"], enemy)
            before = self._combat_actor_snapshot()
            self.add_log(f"{enemy.name} uses {action['name']}.")
            for effect in action["effects"]:
                self._apply_effect(enemy, self._effect_targets(effect.get("target"), targets, enemy), effect,
                                   source_id=f"{enemy.definition_id}/{action['name']}")
                if self.state.phase != "combat":
                    break
                if not self.living_enemies():
                    break
            if playback:
                playback(
                    {
                        "actor_id": enemy.id,
                        "actor_name": enemy.name,
                        "actor_rank": intent["enemy_rank"],
                        "action": action["name"],
                        "target_labels": list(intent.get("target_labels", [])),
                        "setup": sorted(self._action_setup_statuses(action)),
                        "payoff": sorted(self._action_exploit_statuses(action)),
                        "changes": self._enemy_action_changes(
                            before,
                            self._combat_actor_snapshot(),
                        ),
                    }
                )
            if self.state.phase != "combat":
                return
            if not self.living_enemies():
                self._combat_victory()
                return
            self._decay_statuses(enemy)
        self._apply_enemy_phase_mutations()
        self.record("enemy_round", "combat", result=self.state.phase)

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
        if rule == "deaths_door":
            candidates = [hero for hero in heroes if hero.deaths_door] or heroes
            return [min(candidates, key=lambda item: (item.hp / item.max_hp, -item.stress))]
        if rule == "marked":
            candidates = [hero for hero in heroes if hero.statuses.get("marked")] or heroes
            return [min(candidates, key=lambda item: item.hp / item.max_hp)]
        if rule == "wounded":
            candidates = [hero for hero in heroes if hero.statuses.get("wound")] or heroes
            return [min(candidates, key=lambda item: item.hp / item.max_hp)]
        if rule == "weakest_enemy":
            return [min(enemies, key=lambda item: item.hp / item.max_hp)]
        if rule == "weakest_ally":
            candidates = [enemy for enemy in enemies if enemy.id != actor.id]
            return [min(candidates or enemies, key=lambda item: item.hp / item.max_hp)]
        return [self.rng.choice(heroes)]

    def _draw(self, amount: int) -> None:
        if self.resolution.state.active is None or self.resolution.state.active.phase != Phase.PRIMARY:
            if self.living_heroes():
                self._apply_effect(self.living_heroes()[0], [], {"op": "draw", "amount": amount},
                                   source_id=self._source_id or "round:draw")
            return
        self._draw_primary(amount)

    def _draw_primary(self, amount: int) -> None:
        for _ in range(max(0, amount)):
            if not self.state.draw_pile:
                if not self.state.discard_pile:
                    return
                exhausted = [
                    card for card in self.state.discard_pile
                    if self.state.effect_counters.get(f"infusion:exhaust:{card.copy_id}")
                ]
                self.state.draw_pile = [
                    card for card in self.state.discard_pile if card not in exhausted
                ]
                self.state.discard_pile = exhausted
                if not self.state.draw_pile:
                    return
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
        event_type = EventType.CARD_HELD if key == "curse_held_stress" else EventType.CARD_DRAW
        payload = Payload(actor_id=hero.id, card_id=card.card_id)
        if self.resolution.state.active is not None:
            self.resolution.emit(event_type, (hero.id,), payload, source_id=card.card_id)
            return
        owns_root = self.resolution.state.root_id is None
        if owns_root:
            self.resolution.begin(combat_token=self.resolution.state.combat_token, turn_token=self.state.round)
        self.resolution.submit(event_type, card.card_id, (hero.id,), payload)
        self.resolve_pending(close_root=owns_root)

    def _curse_trigger(self, listener: Listener, event: Event, queue: EventQueue) -> bool:
        hero = next(actor for actor in self.state.heroes if actor.id == listener.entity_id)
        if not hero.alive:
            return False
        definition = self.catalog.curses[event.payload.card_id]
        effect = definition["effects"][0]
        key = effect["key"]
        if (key == "curse_held_stress") != (event.event_type == EventType.CARD_HELD):
            return False
        amount = round(self._stack_value(effect, 1))
        op, status = None, None
        if key in {"curse_draw_stress", "curse_held_stress"}:
            op = Opcode.STRESS
        elif key == "curse_draw_wound":
            op, status, amount = Opcode.STATUS, "wound", amount + 1
        elif key == "curse_draw_energy":
            op, amount = Opcode.ENERGY, -amount
        elif key == "curse_draw_move":
            op = Opcode.MOVE
        if op:
            queue.emit(EventType(op.value), (hero.id,), Payload(actor_id=hero.id, opcode=op, amount=amount, status=status),
                       source_id=event.payload.card_id)
        if key != "curse_dead_draw":
            self.add_log(f"{definition['name']} afflicts {hero.name}.")
        return op is not None

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
        result = max(0, round(amount * multiplier))
        if actor.side == "enemy":
            result = (result * self.current_director().enemy_damage_bp + 5_000) // 10_000
        return result

    def _damage(self, target: Actor, amount: int, attacker: Actor | None = None) -> None:
        frame = self.resolution.state.active
        if frame is not None and frame.phase == Phase.PRIMARY:
            self._damage_primary(target, amount, attacker)
            return
        source = self._source_id or (attacker.id if attacker else "world:unattributed")
        payload = Payload(actor_id=attacker.id if attacker else None, opcode=Opcode.DAMAGE,
                          amount=amount, raw_damage=True)
        if frame is not None:
            self.resolution.emit(EventType.DAMAGE, (target.id,), payload, source_id=source)
            return
        owns_root = self.resolution.state.root_id is None
        if owns_root:
            self.resolution.begin(combat_token=self.resolution.state.combat_token, turn_token=self.state.round)
        self.resolution.submit(EventType.DAMAGE, source, (target.id,), payload)
        self.resolve_pending(close_root=owns_root)

    def _damage_primary(self, target: Actor, amount: int, attacker: Actor | None = None) -> None:
        requested = amount
        intended_target = target.id
        source = self._source_id or (attacker.id if attacker else "world:unattributed")
        if target.guarded_by:
            allies = self.living_heroes() if target.side == "hero" else self.living_enemies()
            guard = next((item for item in allies if item.id == target.guarded_by), None)
            if guard and guard.id != target.id:
                self.add_log(f"{guard.name} intercepts the hit.")
                target = guard
        was_deaths_door = target.deaths_door
        if attacker and attacker.side != target.side and target.statuses.get("dodge"):
            target.statuses.pop("dodge", None)
            self.add_log(f"{target.name} evades the hit.")
            self.record("damage", source, target=target.id, intended_target=intended_target,
                        requested=requested, absorbed=0, amount=0, hp_loss=0, overkill=0, dodged=True,
                        attacker=attacker.id, was_deaths_door=was_deaths_door, modified=0, deflected=0)
            return
        if target.statuses.get("vulnerable"):
            amount = round(amount * 1.5)
        if target.side == "hero":
            multiplier = 1 + self._hero_effect_value(target, "curse", "incoming_damage_bonus")
            multiplier *= 1 - self._item_effect_value("incoming_damage_reduction")
            amount = max(0, round(amount * max(0.5, min(2.0, multiplier))))
        modified = amount
        deflected = 0
        absorbed = min(target.block, amount)
        target.block -= absorbed
        amount -= absorbed
        if target.side == "hero" and amount > 0:
            hit_key = f"enemy_hit:{target.id}"
            if not self.state.effect_counters.get(hit_key):
                deflected = min(amount, round(self._item_effect_value("deflection")))
                amount -= deflected
                self.state.effect_counters[hit_key] = 1
        self.record("damage", source, target=target.id, intended_target=intended_target,
                    requested=requested, absorbed=absorbed, amount=amount,
                    hp_loss=min(target.hp, amount), overkill=max(0, amount - target.hp), dodged=False,
                    attacker=attacker.id if attacker else None, was_deaths_door=was_deaths_door,
                    modified=modified, deflected=deflected)
        if amount <= 0:
            return
        if target.side == "hero" and target.deaths_door:
            death_chance = self.catalog.balance["death_chance"] - self._hero_effect_value(
                target,
                "boon",
                "death_chance_reduction",
            )
            roll = self.rng.random()
            died = roll < max(0.05, death_chance)
            self.record("deaths_door_check", source, target=target.id,
                        chance_bp=round(max(0.05, death_chance) * 10000), roll=roll.hex(), died=died)
            if died:
                target.deaths_door = False
                target.hp = 0
                self._hero_died(target)
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
                self.record("healing", "boon:second_wind", target=target.id, requested=second_wind,
                            amount=target.hp, overheal=max(0, second_wind - target.hp), replacement=True)
                self.add_log(f"Second Wind restores {target.name} for {target.hp}.")
            else:
                target.deaths_door = True
                self._change_stress(target, 12)
                self.add_log(f"{target.name} is at Death's Door.")
        elif target.side == "enemy" and target.hp == 0:
            self.add_log(f"{target.name} is destroyed.")
            if self.resolution.state.active is not None:
                self.resolution.emit(
                    EventType.DEATH,
                    (target.id,),
                    Payload(actor_id=target.id),
                    source_id=source,
                    mandatory=True,
                )
            self._normalize_ranks("enemy")
    def _hero_died(self, hero: Actor) -> None:
        death_rank = hero.rank
        hero.rank = 0
        hero.block = 0
        hero.statuses.clear()
        hero.guarded_by = None
        hero.guard_turns = 0
        for actor in self.state.heroes + self.state.enemies:
            if actor.guarded_by == hero.id:
                actor.guarded_by = None
                actor.guard_turns = 0

        def belongs_to_hero(card: CardInstance) -> bool:
            if card.card_id in self.catalog.curses:
                return card.bound_hero_id == hero.id
            return self.catalog.cards[card.card_id]["hero"] == hero.id

        removed = 0
        for zone_name in ("deck", "hand", "draw_pile", "discard_pile"):
            zone = getattr(self.state, zone_name)
            kept = [card for card in zone if not belongs_to_hero(card)]
            if zone_name == "deck":
                removed = len(zone) - len(kept)
                for card in zone:
                    if belongs_to_hero(card):
                        self.record("card_lost", card.card_id, owner=hero.id, card=asdict(card))
            setattr(self.state, zone_name, kept)

        owned_curses = self.state.curses.get(hero.id, {})
        for curse_id in list(owned_curses):
            if self.catalog.curses[curse_id]["kind"] == "card":
                del owned_curses[curse_id]
        if not owned_curses:
            self.state.curses.pop(hero.id, None)

        self._normalize_ranks("hero")
        survivors = self.living_heroes()
        self.record("crew_death", hero.id, rank=death_rank, cards_lost=removed,
                    survivors=[actor.id for actor in survivors])
        doctrine = self.doctrine_definition()
        casualty_key = f"doctrine:casualty:death:{hero.id}"
        if (
            survivors
            and doctrine is not None
            and doctrine["mode"] == "casualty"
            and not self.state.effect_counters.get(casualty_key)
        ):
            self.state.effect_counters[casualty_key] = 1
            self._apply_effect(
                survivors[0],
                survivors,
                {"op": "block", "amount": 3},
                source_id=doctrine["id"],
            )
            self._apply_effect(
                survivors[0],
                [survivors[0]],
                {"op": "draw", "amount": 1},
                source_id=doctrine["id"],
            )
            self.record(
                "doctrine_trigger",
                doctrine["id"],
                mode="casualty",
                fallen=hero.id,
                survivors=[actor.id for actor in survivors],
                block=3,
                draw=1,
                limit="once_per_death",
            )
            self.add_log(f"DOCTRINE — {doctrine['name']} holds after the casualty.")
        if self.resolution.state.active is not None:
            self.resolution.emit(EventType.DEATH, (hero.id,), Payload(actor_id=hero.id), source_id=hero.id, mandatory=True)
        if not survivors:
            if self.state.phase == "combat":
                self.record("encounter_end", "combat", result="defeat", kind=self.state.combat_kind,
                            rounds=self.state.round, crew=[asdict(actor) for actor in self.state.heroes],
                            pressure=self.state.encounter_pressure,
                            modules=list(self.state.encounter_modules))
                self._clear_encounter_director()
            self.state.phase = "defeat"
            self.add_log(f"{hero.name} dies. No crew remain.")
            return
        self.add_log(
            f"{hero.name} dies. {removed} owned cards are lost; "
            f"{len(survivors)} crew continue."
        )

    def _heal(self, target: Actor, amount: int, healer: Actor | None = None) -> None:
        if self.resolution.state.active is not None and self.resolution.state.active.phase == Phase.PRIMARY:
            self._heal_primary(target, amount, healer)
            return
        source = self._source_id or (healer.id if healer else "world:unattributed")
        payload = Payload(actor_id=healer.id if healer else None, opcode=Opcode.HEAL, amount=amount)
        if self.resolution.state.active is not None:
            self.resolution.emit(EventType.HEAL, (target.id,), payload, source_id=source)
            return
        owns_root = self.resolution.state.root_id is None
        if owns_root:
            self.resolution.begin(combat_token=self.resolution.state.combat_token, turn_token=self.state.round)
        self.resolution.submit(EventType.HEAL, source, (target.id,), payload)
        self.resolve_pending(close_root=owns_root)

    def _heal_primary(self, target: Actor, amount: int, healer: Actor | None = None) -> None:
        previous_hp = target.hp
        multiplier = float(self._affliction_modifiers(target).get("healing_mult", 1))
        if target.side == "hero":
            multiplier *= 1 - self._hero_effect_value(target, "curse", "healing_reduction")
        if healer and healer.side == "hero":
            multiplier *= 1 + self._hero_effect_value(healer, "boon", "healing_bonus")
        multiplier = max(0.5, min(2.0, multiplier))
        target.hp = min(target.max_hp, target.hp + max(0, round(amount * multiplier)))
        self.record("healing", self._source_id or (healer.id if healer else "world:unattributed"),
                    target=target.id, requested=amount, amount=target.hp - previous_hp,
                    overheal=max(0, round(amount * multiplier) - (target.hp - previous_hp)))
        if target.hp > 0:
            target.deaths_door = False

    def _change_stress(self, target: Actor, amount: int) -> None:
        previous_stress = target.stress
        if amount > 0 and target.side == "hero":
            multiplier = 1 + self._hero_effect_value(target, "curse", "stress_bonus")
            multiplier *= 1 - self._hero_effect_value(target, "boon", "stress_reduction")
            multiplier *= 1 - self._item_effect_value("stress_reduction")
            amount = max(0, round(amount * max(0.4, min(2.0, multiplier))))
        target.stress = max(0, target.stress + amount)
        self.record("stress", self._source_id or "world:unattributed", target=target.id,
                    requested=amount, amount=target.stress - previous_stress)
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
            with self.attribution(f"status:wound:{actor.id}"):
                self._damage(actor, 2)

    def _add_status(self, target: Actor, status: str, amount: int) -> None:
        if status == "wound" and target.side == "hero":
            reduction = self._hero_effect_value(target, "boon", "wound_reduction")
            reduction += self._item_effect_value("wound_reduction")
            amount = max(0, amount - round(reduction))
        if amount:
            previous = target.statuses.get(status, 0)
            target.statuses[status] = max(target.statuses.get(status, 0), amount)
            self.record("status", self._source_id or "world:unattributed", target=target.id,
                        status=status, previous=previous, amount=amount, result=target.statuses[status])

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
        occupied_ranks = sum(member.alive for member in party)
        for _ in range(abs(amount)):
            direction = 1 if amount > 0 else -1
            next_rank = actor.rank + direction
            if next_rank not in range(1, occupied_ranks + 1):
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

    def _clear_encounter_director(self) -> None:
        self.state.encounter_pressure = None
        self.state.encounter_modules = []
        self.state.reinforcement_tickets = 0
        self.state.reinforcement_reserve_id = None

    def _combat_victory(self) -> None:
        kind = self.state.combat_kind
        self.record("encounter_end", "combat", result="victory", kind=kind, rounds=self.state.round,
                    crew=[asdict(hero) for hero in self.state.heroes],
                    pressure=self.state.encounter_pressure,
                    modules=list(self.state.encounter_modules))
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
        self._clear_encounter_director()
        if kind == "boss":
            self.room().resolved = True
            self.state.phase = "victory"
            self.add_log("The Overseer falls silent. Evacuation is possible.")
            return
        if kind not in {"ambush", "objective"}:
            self.room().resolved = True
        count = 4 if self.state.light < self.catalog.balance["low_light_threshold"] else 3
        count += round(self._item_effect_value("reward_choices"))
        count += self.world_director().reward_choices
        lane = {
            "elite": Lane.ELITE,
            "objective": Lane.OBJECTIVE,
        }.get(kind, Lane.NORMAL)
        self.state.rewards = self._generate_card_rewards(count, lane)
        self.state.phase = "reward"
        if self.state.tutorial:
            self.state.tutorial_stage = 7
        self.add_log("Combat won. Choose a recovered technique.")

    def _generate_card_rewards(self, count: int, lane: Lane = Lane.NORMAL) -> list[str]:
        active_heroes = {hero.id for hero in self.living_heroes()}
        eligible = set(eligible_techniques(self.catalog, active_heroes, lane))
        # Preserve the manifest's frozen RNG-architecture enumeration after
        # applying the order-independent eligibility predicate.
        available = [card_id for card_id in self.catalog.cards if card_id in eligible]
        if not available or count <= 0:
            return []
        owned = Counter(card.card_id for card in self.state.deck if card.card_id in self.catalog.cards)
        deck_tags = Counter(
            tag
            for card in self.state.deck
            if card.card_id in self.catalog.cards
            for tag in self.card_tags(card.card_id)
        )
        chosen: list[str] = []
        chosen_shapes: set[tuple[Any, ...]] = set()
        chosen_owners: Counter[str] = Counter()

        def choose(pool: list[str], *, novelty: float = 1.0) -> None:
            candidates = [card_id for card_id in pool if card_id not in chosen]
            if not candidates or len(chosen) >= count:
                return
            structurally_distinct = [
                card_id
                for card_id in candidates
                if self._reward_shape(card_id) not in chosen_shapes
            ]
            if structurally_distinct:
                candidates = structurally_distinct
            weights = []
            for card_id in candidates:
                definition = self.catalog.cards[card_id]
                tags = self.card_tags(card_id)
                current_rank = next(
                    hero.rank for hero in self.living_heroes() if hero.id == definition["hero"]
                )
                usable_now = current_rank in definition["from_ranks"]
                duplicate_weight = 1 / (1 + owned[card_id] * novelty)
                affinity_weight = 1.2 if f"affinity:{self.current_biome()}" in tags else 1.0
                owner_weight = 1 / (1 + chosen_owners[definition["hero"]] * 0.5)
                weights.append(
                    (1.25 if usable_now else 0.8)
                    * duplicate_weight
                    * affinity_weight
                    * owner_weight
                )
            card_id = self.rng.choices(candidates, weights=weights, k=1)[0]
            chosen.append(card_id)
            chosen_shapes.add(self._reward_shape(card_id))
            chosen_owners[self.catalog.cards[card_id]["hero"]] += 1

        desired_combo_tags = {
            f"{counterpart}:{tag.split(':', 1)[1]}"
            for tag in deck_tags
            if tag.startswith(("setup:", "payoff:"))
            for counterpart in (["payoff"] if tag.startswith("setup:") else ["setup"])
        }
        bridge_pool = [
            card_id for card_id in available if self.card_tags(card_id) & desired_combo_tags
        ]
        if not bridge_pool:
            synergistic = {
                tag
                for tag, amount in deck_tags.items()
                if amount >= 2 and tag not in {"damage", "block"}
            }
            bridge_pool = [
                card_id for card_id in available if self.card_tags(card_id) & synergistic
            ]
        choose(bridge_pool or available, novelty=1.5)

        disciplines = ("damage", "block", "recovery", "control", "draw", "mobility")
        thin_disciplines = sorted(disciplines, key=lambda tag: (deck_tags[tag], tag))[:2]
        corrective_pool = [
            card_id
            for card_id in available
            if self.card_tags(card_id) & set(thin_disciplines)
            and owned[card_id] == 0
        ]
        choose(corrective_pool or [card_id for card_id in available if owned[card_id] == 0], novelty=2.0)

        generic_tags = {"damage", "block", "stress_risk"}
        available_tags = Counter(
            tag
            for card_id in available
            for tag in self.card_tags(card_id)
            if tag not in generic_tags and not tag.startswith("affinity:")
        )
        pivot_tags = {
            tag
            for tag, frequency in available_tags.items()
            if frequency >= 2 and deck_tags[tag] == 0
        }
        pivot_pool = [
            card_id
            for card_id in available
            if owned[card_id] == 0 and self.card_tags(card_id) & pivot_tags
        ]
        choose(pivot_pool, novelty=2.0)

        while len(chosen) < min(count, len(available)):
            choose(available, novelty=1.25)
        self.record("card_offer", "reward:technique", offered=chosen, eligible_count=len(available),
                    biome=self.current_biome(), lane=lane.value)
        return chosen

    def _reward_shape(self, card_id: str) -> tuple[Any, ...]:
        card = self.catalog.cards[card_id]

        def effect_shape(effect: dict[str, Any]) -> tuple[Any, ...]:
            return (
                effect["op"],
                effect.get("status"),
                effect.get("bonus_status"),
                effect.get("condition_status"),
                effect.get("condition_target_state"),
                effect.get("condition_actor_state"),
                effect.get("target"),
            )

        return card["target"], tuple(effect_shape(effect) for effect in card["effects"])

    def reward_context(self, card_id: str) -> tuple[str, str]:
        if card_id not in self.catalog.cards:
            raise RuleError("unknown reward card")
        card = self.catalog.cards[card_id]
        owner = next((hero for hero in self.living_heroes() if hero.id == card["hero"]), None)
        if owner is None:
            raise RuleError("the reward owner is no longer available")
        tags = self.card_tags(card_id)
        deck_tags = Counter(
            tag
            for card in self.state.deck
            if card.card_id in self.catalog.cards
            for tag in self.card_tags(card.card_id)
        )
        if owner.rank not in card["from_ranks"]:
            ranks = ",".join(str(rank) for rank in card["from_ranks"])
            role = self.catalog.heroes[card["hero"]]["role"]
            return "POSITION RISK", f"{role} is R{owner.rank}; this plays from R{ranks}."
        desired = {
            f"{counterpart}:{tag.split(':', 1)[1]}"
            for tag in deck_tags
            if tag.startswith(("setup:", "payoff:"))
            for counterpart in (["payoff"] if tag.startswith("setup:") else ["setup"])
        }
        bridges = sorted(tags & desired)
        if bridges:
            mechanic = bridges[0].split(":", 1)[1].replace("_", " ").upper()
            relationship = "setup" if bridges[0].startswith("payoff:") else "payoff"
            verb = "Uses" if bridges[0].startswith("payoff:") else "Supplies"
            return "SYNERGY", f"{verb} deck {mechanic} {relationship}."
        meaningful = {
            tag
            for tag in tags
            if tag not in {"damage", "block", "stress_risk"}
            and not tag.startswith("affinity:")
        }
        new_tags = sorted(tag for tag in meaningful if deck_tags[tag] == 0)
        if new_tags:
            mechanic = new_tags[0].replace(":", " ").replace("_", " ").upper()
            return "NEW LINE", f"Introduces {mechanic}; taking it increases deck breadth."
        copies = sum(card.card_id == card_id for card in self.state.deck)
        if copies:
            return "COMMIT", f"Adds copy {copies + 1}; stronger concentration, less draw variety."
        return "COVERAGE", "Adds a new card shape without committing to another copy."

    def transformation_options(self, card_index: int, count: int = 3) -> list[str]:
        if not 0 <= card_index < len(self.state.deck):
            raise RuleError("choose a card to transform")
        source = self.state.deck[card_index]
        if source.card_id in self.catalog.curses:
            raise RuleError("curse cards cannot be transformed")
        definition = self.catalog.cards[source.card_id]
        hero = next((hero for hero in self.living_heroes() if hero.id == definition["hero"]), None)
        if hero is None:
            raise RuleError("the card's owner is no longer alive")
        deck_tags = Counter(
            tag
            for index, card in enumerate(self.state.deck)
            if index != card_index and card.card_id in self.catalog.cards
            for tag in self.card_tags(card.card_id)
        )
        owned = Counter(
            card.card_id
            for index, card in enumerate(self.state.deck)
            if index != card_index and card.card_id in self.catalog.cards
        )
        desired = {
            f"payoff:{tag.split(':', 1)[1]}"
            for tag in deck_tags
            if tag.startswith("setup:")
        } | {
            f"setup:{tag.split(':', 1)[1]}"
            for tag in deck_tags
            if tag.startswith("payoff:")
        }
        candidates = [
            card_id
            for card_id, card in self.catalog.cards.items()
            if card["hero"] == hero.id and card_id != source.card_id
        ]

        def score(card_id: str) -> tuple[float, str]:
            card = self.catalog.cards[card_id]
            tags = self.card_tags(card_id)
            synergy = sum(deck_tags[tag] for tag in tags if tag not in {"damage", "block"})
            bridge = len(tags & desired) * 5
            rank_coverage = 4 if hero.rank in card["from_ranks"] else 0
            broad = len(card["from_ranks"]) + len(card.get("target_ranks", [])) * 0.25
            duplication = owned[card_id] * 4
            return bridge + synergy * 0.35 + rank_coverage + broad - duplication, card_id

        return [card_id for card_id in sorted(candidates, key=score, reverse=True)[:count]]

    def infusion_options(self, card_index: int, count: int = 3) -> list[str]:
        if not 0 <= card_index < len(self.state.deck):
            raise RuleError("choose a card to infuse")
        card = self.state.deck[card_index]
        if card.card_id in self.catalog.curses or card.infusion_id is not None:
            raise RuleError("that card cannot receive an infusion")
        eligible = sorted(
            infusion_id for infusion_id in self.catalog.infusions
            if self.infusion_compatible(card.card_id, infusion_id)
        )
        if not eligible:
            raise RuleError("that card has no compatible infusions")
        rng = self._domain_rng(
            "infusion_offer", self.state.travel_ticks, card.copy_id,
            self.state.current_room,
        )
        return rng.sample(eligible, min(count, len(eligible)))

    def transformation_comparison(
        self,
        source_id: str,
        destination_id: str,
    ) -> dict[str, Any]:
        if source_id not in self.catalog.cards or destination_id not in self.catalog.cards:
            raise RuleError("unknown transformation card")
        source = self.catalog.cards[source_id]
        destination = self.catalog.cards[destination_id]
        if source["hero"] != destination["hero"] or source_id == destination_id:
            raise RuleError("transformations require two different techniques from one owner")

        def effect_labels(card: dict[str, Any]) -> list[str]:
            labels = []
            for effect in card["effects"]:
                label = effect["op"]
                if effect.get("status"):
                    label += f":{effect['status']}"
                if effect.get("bonus_status"):
                    label += f"+{effect['bonus_status']}"
                condition = (
                    effect.get("condition_status")
                    or effect.get("condition_target_state")
                    or effect.get("condition_actor_state")
                )
                if condition:
                    label += f"?{condition}"
                labels.append(label)
            return labels

        source_tags = self.card_tags(source_id)
        destination_tags = self.card_tags(destination_id)
        return {
            "source": source["name"],
            "destination": destination["name"],
            "cost": (source["cost"], destination["cost"]),
            "ranks": (list(source["from_ranks"]), list(destination["from_ranks"])),
            "effects": (effect_labels(source), effect_labels(destination)),
            "added_tags": sorted(destination_tags - source_tags),
            "removed_tags": sorted(source_tags - destination_tags),
        }

    def choose_reward(self, index: int | None) -> None:
        if self.state.phase != "reward":
            raise RuleError("there is no reward to choose")
        if index is not None:
            if not 0 <= index < len(self.state.rewards):
                raise RuleError("invalid reward")
            self.state.deck.append(self._new_card(self.state.rewards[index]))
            self.add_log(f"Added {self.catalog.cards[self.state.rewards[index]]['name']} to the deck.")
        self.record("card_choice", "reward:technique",
                    picked=self.state.rewards[index] if index is not None else None,
                    skipped=[card for position, card in enumerate(self.state.rewards) if position != index])
        self.state.rewards = []
        self.state.phase = "exploration"
        if self.state.tutorial:
            self.state.tutorial_stage = 9
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
        biome_id = self.room().biome_id
        for effect in choice["effects"]:
            if effect["op"] == "card_reward":
                grant_reward = True
            else:
                self._apply_exploration_effect(biome_id, effect, event["id"])
            if self.state.phase == "defeat":
                break
        self.room().resolved = True
        self.state.current_event = None
        self.add_log(f"Event resolved: {choice['label']}.")
        if self.state.phase == "defeat":
            return
        if grant_reward:
            count = 3 + round(self._item_effect_value("reward_choices"))
            self.state.rewards = self._generate_card_rewards(count, Lane.NORMAL)
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
        replacement_id: str | None = None,
        mastery_branch: str | None = None,
        infusion_id: str | None = None,
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
                removed = self.state.deck.pop(card_index)
                self.record("card_removed", removed.card_id, card=asdict(removed), source="curse_treatment")
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
            self.record("card_upgraded", self.state.deck[card_index].card_id, index=card_index, source="workshop")
            self.add_log(f"Upgraded {self.catalog.cards[self.state.deck[card_index].card_id]['name']}.")
        elif action == "mastery" and self.state.service_type == "upgrade":
            if card_index is None or not 0 <= card_index < len(self.state.deck):
                raise RuleError("choose a card to master")
            card = self.state.deck[card_index]
            mastery = self.mastery_definition(card.card_id)
            if mastery is None or not card.upgraded or card.mastery is not None:
                raise RuleError("that card is not eligible for mastery")
            branches = {branch["id"]: branch for branch in mastery["branches"]}
            if mastery_branch not in branches:
                raise RuleError("choose one of the offered mastery branches")
            card.mastery = mastery_branch
            self.record("card_mastered", mastery["id"], card=card.card_id,
                        copy_id=card.copy_id, branch=mastery_branch,
                        mode=branches[mastery_branch]["mode"], source="workshop")
            self.add_log(
                f"Mastered {self.catalog.cards[card.card_id]['name']}: "
                f"{branches[mastery_branch]['name']}."
            )
        elif action == "infusion" and self.state.service_type == "upgrade":
            if card_index is None or not 0 <= card_index < len(self.state.deck):
                raise RuleError("choose a card to infuse")
            card = self.state.deck[card_index]
            offered = self.infusion_options(card_index)
            if card.infusion_id is not None or infusion_id not in offered:
                raise RuleError("choose one of the offered compatible infusions")
            card.infusion_id = infusion_id
            infusion = self.catalog.infusions[infusion_id]
            self.record("card_infused", infusion_id, card=card.card_id,
                        copy_id=card.copy_id, mode=infusion["mode"],
                        offered=offered, source="workshop")
            self.add_log(
                f"Infused {self.catalog.cards[card.card_id]['name']}: {infusion['name']}."
            )
        elif action == "remove":
            if len(self.state.deck) <= 12:
                raise RuleError("the deck cannot contain fewer than 12 cards")
            if card_index is None or not 0 <= card_index < len(self.state.deck):
                raise RuleError("choose a card to remove")
            card = self.state.deck.pop(card_index)
            self.record("card_removed", card.card_id, card=asdict(card), source="workshop")
            if card.card_id in self.catalog.curses:
                if card.bound_hero_id is None:
                    raise RuleError("curse card is missing its bound hero")
                self._decrement_curse(card.bound_hero_id, card.card_id)
            self.add_log(f"Removed {self.card_definition(card)['name']}.")
        elif action == "transform" and self.state.service_type == "upgrade":
            if card_index is None or not 0 <= card_index < len(self.state.deck):
                raise RuleError("choose a card to transform")
            options = self.transformation_options(card_index)
            if replacement_id not in options:
                raise RuleError("choose one of the offered transformations")
            source = self.state.deck[card_index]
            self.record("card_transformed", source.card_id, destination=replacement_id,
                        offered=options, index=card_index, lost_upgrade=source.upgraded,
                        lost_mastery=source.mastery, lost_infusion=source.infusion_id,
                        copy_id=source.copy_id)
            old_name = self.catalog.cards[source.card_id]["name"]
            source.card_id = replacement_id
            source.upgraded = False
            source.mastery = None
            source.infusion_id = None
            self.add_log(
                f"Transformed {old_name} into {self.catalog.cards[replacement_id]['name']}."
            )
        else:
            raise RuleError("that service is not available here")
        self.room().resolved = True
        self.state.service_type = None
        self.state.phase = "exploration"
