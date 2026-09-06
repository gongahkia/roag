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
class GameState:
    seed: int
    phase: str
    heroes: list[Actor]
    deck: list[CardInstance]
    rooms: list[Room]
    hub_selection: list[str] = field(default_factory=list)
    current_room: int = 0
    party_x: int = 5
    party_y: int = 17
    exploration_steps: int = 0
    patrols: list[Patrol] = field(default_factory=list)
    active_patrol_id: str | None = None
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


def _build_world() -> tuple[str, ...]:
    floor: set[tuple[int, int]] = set()
    for center_x, center_y in ROOM_POSITIONS.values():
        for y in range(center_y - 2, center_y + 3):
            for x in range(center_x - 4, center_x + 5):
                floor.add((x, y))

    for room_id, neighbors in ROOM_EDGES.items():
        start_x, start_y = ROOM_POSITIONS[room_id]
        for neighbor in neighbors:
            if neighbor < room_id:
                continue
            end_x, end_y = ROOM_POSITIONS[neighbor]
            middle_x = (start_x + end_x) // 2
            for x in range(min(start_x, middle_x), max(start_x, middle_x) + 1):
                floor.add((x, start_y))
            for y in range(min(start_y, end_y), max(start_y, end_y) + 1):
                floor.add((middle_x, y))
            for x in range(min(middle_x, end_x), max(middle_x, end_x) + 1):
                floor.add((x, end_y))

    cells = [[" " for _ in range(WORLD_WIDTH)] for _ in range(WORLD_HEIGHT)]
    for x, y in floor:
        cells[y][x] = "."
    for x, y in floor:
        for adjacent_y in range(y - 1, y + 2):
            for adjacent_x in range(x - 1, x + 2):
                if (
                    0 <= adjacent_x < WORLD_WIDTH
                    and 0 <= adjacent_y < WORLD_HEIGHT
                    and cells[adjacent_y][adjacent_x] == " "
                ):
                    cells[adjacent_y][adjacent_x] = "#"
    return tuple("".join(row) for row in cells)


WORLD_TILES = _build_world()


class GameEngine:
    """Owns the mutable run and its seeded pseudo-random stream."""

    SAVE_VERSION = 3

    def __init__(self, catalog: Catalog, state: GameState, rng: random.Random):
        self.catalog = catalog
        self.state = state
        self.rng = rng

    @classmethod
    def new(cls, catalog: Catalog, seed: int, *, start_in_hub: bool = False) -> GameEngine:
        rng = random.Random(seed)
        rooms = cls._generate_rooms(catalog, rng)
        default_party = list(catalog.heroes)[:4]
        state = GameState(
            seed=seed,
            phase="hub",
            heroes=[],
            deck=[],
            rooms=rooms,
            hub_selection=default_party,
            party_x=ROOM_POSITIONS[0][0],
            party_y=ROOM_POSITIONS[0][1],
            light=catalog.balance.get("starting_light", 100),
            supplies=catalog.balance.get("starting_supplies", 4),
            log=["Crew manifest opened in the Orison airlock."],
        )
        engine = cls(catalog, state, rng)
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
                hub_selection=raw["hub_selection"],
                current_room=raw["current_room"],
                party_x=raw["party_x"],
                party_y=raw["party_y"],
                exploration_steps=raw["exploration_steps"],
                patrols=[Patrol(**item) for item in raw["patrols"]],
                active_patrol_id=raw["active_patrol_id"],
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
        hero_ids = {hero.id for hero in state.heroes}
        if state.phase == "hub" and state.heroes:
            raise RuleError("hub save unexpectedly contains an active party")
        if state.phase != "hub" and (len(hero_ids) != 4 or not hero_ids <= set(catalog.heroes)):
            raise RuleError("save contains an unexpected crew roster")
        if len(state.hub_selection) > 4 or any(hero_id not in catalog.heroes for hero_id in state.hub_selection):
            raise RuleError("save contains an invalid hub selection")
        if not cls.is_walkable(state.party_x, state.party_y):
            raise RuleError("save places the crew outside the ship")
        patrol_ids = {patrol.id for patrol in state.patrols}
        if len(patrol_ids) != len(state.patrols):
            raise RuleError("save contains duplicate patrols")
        for patrol in state.patrols:
            room = state.rooms[patrol.room_id] if 0 <= patrol.room_id < len(state.rooms) else None
            if (
                room is None
                or patrol.encounter_id not in catalog.encounters
                or not cls.is_walkable(patrol.x, patrol.y)
            ):
                raise RuleError("save contains an invalid patrol")
        if state.active_patrol_id is not None and state.active_patrol_id not in patrol_ids:
            raise RuleError("save references an unknown active patrol")
        piles = state.deck + state.hand + state.draw_pile + state.discard_pile
        if any(card.card_id not in catalog.cards for card in piles):
            raise RuleError("save references an unknown card")
        return cls(catalog, state, rng)

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

    @staticmethod
    def world_tiles() -> tuple[str, ...]:
        return WORLD_TILES

    @staticmethod
    def is_walkable(x: int, y: int) -> bool:
        return 0 <= y < WORLD_HEIGHT and 0 <= x < WORLD_WIDTH and WORLD_TILES[y][x] == "."

    @staticmethod
    def _neighbors(position: tuple[int, int]) -> list[tuple[int, int]]:
        x, y = position
        return [
            candidate
            for candidate in ((x, y - 1), (x - 1, y), (x + 1, y), (x, y + 1))
            if GameEngine.is_walkable(*candidate)
        ]

    @classmethod
    def _find_path(
        cls,
        start: tuple[int, int],
        destination: tuple[int, int],
    ) -> list[tuple[int, int]]:
        if not cls.is_walkable(*destination):
            return []
        pending = deque([start])
        previous: dict[tuple[int, int], tuple[int, int] | None] = {start: None}
        while pending:
            current = pending.popleft()
            if current == destination:
                break
            for neighbor in cls._neighbors(current):
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

    @classmethod
    def _distances_from(cls, origin: tuple[int, int]) -> dict[tuple[int, int], int]:
        distances = {origin: 0}
        pending = deque([origin])
        while pending:
            current = pending.popleft()
            for neighbor in cls._neighbors(current):
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
        maximum = int(self.catalog.balance["maximum_navigation_distance"])
        if len(path) > maximum:
            raise RuleError(f"destination is beyond the maximum reach of {maximum} tiles")
        return path

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
        room = next((room for room in self.state.rooms if self.room_position(room.id) == position), None)
        if room is None:
            return
        self.state.current_room = room.id
        room.visited = True
        if room.resolved or room.kind in {"start", "fight", "elite", "boss"}:
            return
        self.add_log(f"Entered {room.name}.")
        self._enter_room(room)

    def _advance_patrols(self) -> None:
        party = (self.state.party_x, self.state.party_y)
        distances = self._distances_from(party)
        occupied = {(patrol.x, patrol.y) for patrol in self.state.patrols if patrol.active}
        for patrol in (item for item in self.state.patrols if item.active):
            current = (patrol.x, patrol.y)
            occupied.discard(current)
            room_kind = self.room(patrol.room_id).kind
            aggression = 12 if room_kind == "elite" else 8 if room_kind == "boss" else 10
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
            self._heal(target, 9)
            message = f"A supply restores {target.name}."
        elif purpose == "calm":
            target = max(self.state.heroes, key=lambda actor: actor.stress)
            self._change_stress(target, -14)
            message = f"A supply steadies {target.name}."
        elif purpose == "light":
            self.state.light = min(100, self.state.light + 25)
            message = "A flare restores 25 light."
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
        self.state.draw_pile = [CardInstance(card.card_id, card.upgraded) for card in self.state.deck]
        self.rng.shuffle(self.state.draw_pile)
        self.state.discard_pile = []
        self.state.hand = []
        self.state.round = 1
        self.state.intents = self._choose_intents()
        self.add_log(f"Combat begins: {encounter['id']}.")
        if surprised:
            self.add_log("The crew is surprised in the darkness.")
            self._enemy_phase()
            if self.state.phase != "combat":
                return
            self.state.intents = self._choose_intents()
        self._start_player_turn()

    def _start_player_turn(self) -> None:
        for hero in self.living_heroes():
            hero.block = 0
            self._tick_wound(hero)
            modifiers = self._affliction_modifiers(hero)
            if modifiers.get("turn_stress"):
                self._change_stress(hero, int(modifiers["turn_stress"]))
        if self.state.phase != "combat":
            return
        self.state.energy = self.catalog.balance["energy"]
        self._draw(self.catalog.balance["hand_size"] - len(self.state.hand))

    def living_heroes(self) -> list[Actor]:
        return sorted((actor for actor in self.state.heroes if actor.alive), key=lambda actor: actor.rank)

    def living_enemies(self) -> list[Actor]:
        return sorted((actor for actor in self.state.enemies if actor.hp > 0), key=lambda actor: actor.rank)

    def card_definition(self, card: CardInstance) -> dict[str, Any]:
        return self.catalog.cards[card.card_id]

    def card_cost(self, card: CardInstance) -> int:
        definition = self.card_definition(card)
        actor = self._actor(definition["hero"])
        delta = int(self._affliction_modifiers(actor).get("card_cost_delta", 0))
        base = definition.get("upgrade_cost", definition["cost"]) if card.upgraded else definition["cost"]
        return max(0, base + delta)

    def valid_targets(self, hand_index: int) -> list[str]:
        if not 0 <= hand_index < len(self.state.hand):
            return []
        definition = self.card_definition(self.state.hand[hand_index])
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
                    adjusted = self._outgoing_damage(actor, adjusted)
                    self._damage(target, adjusted, actor)
                elif op == "block":
                    multiplier = float(self._affliction_modifiers(target).get("block_mult", 1))
                    target.block += max(0, round(amount * multiplier))
                elif op == "heal":
                    self._heal(target, amount)
                elif op == "stress" and target.side == "hero":
                    self._change_stress(target, amount)
                elif op == "move":
                    self._move(target, amount)
                elif op == "guard" and target.side == "hero" and target.id != actor.id:
                    target.guarded_by = actor.id
                    target.guard_turns = amount
                elif op == "status":
                    target.statuses[effect["status"]] = max(target.statuses.get(effect["status"], 0), amount)
                elif op == "cleanse":
                    for status in ("marked", "stun", "vulnerable", "weak", "wound"):
                        target.statuses.pop(status, None)

    def end_turn(self) -> None:
        if self.state.phase != "combat":
            raise RuleError("there is no combat turn to end")
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
            self.state.hand.append(self.state.draw_pile.pop())

    def _actor(self, actor_id: str) -> Actor:
        matches = [item for item in self.state.heroes + self.state.enemies if item.id == actor_id and item.alive]
        if not matches:
            raise RuleError(f"unknown or inactive actor: {actor_id}")
        return matches[0]

    def _outgoing_damage(self, actor: Actor, amount: int) -> int:
        multiplier = float(self._affliction_modifiers(actor).get("damage_mult", 1))
        if actor.statuses.get("weak"):
            multiplier *= 0.75
        if actor.statuses.get("focus"):
            multiplier *= 1.25
        return max(0, round(amount * multiplier))

    def _damage(self, target: Actor, amount: int, attacker: Actor | None = None) -> None:
        if target.side == "hero" and target.guarded_by:
            guard = next((item for item in self.living_heroes() if item.id == target.guarded_by), None)
            if guard and guard.id != target.id:
                self.add_log(f"{guard.name} intercepts the hit.")
                target = guard
        if target.statuses.get("dodge"):
            target.statuses.pop("dodge", None)
            self.add_log(f"{target.name} evades the hit.")
            return
        if target.statuses.get("vulnerable"):
            amount = round(amount * 1.5)
        absorbed = min(target.block, amount)
        target.block -= absorbed
        amount -= absorbed
        if amount <= 0:
            return
        if target.side == "hero" and target.deaths_door:
            if self.rng.random() < self.catalog.balance["death_chance"]:
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
            target.deaths_door = True
            self._change_stress(target, 12)
            self.add_log(f"{target.name} is at Death's Door.")
        elif target.side == "enemy" and target.hp == 0:
            self.add_log(f"{target.name} is destroyed.")
            self._normalize_ranks("enemy")
        if attacker and attacker.alive and target.statuses.get("riposte") and attacker.side != target.side:
            self.add_log(f"{target.name} answers with a riposte.")
            self._damage(attacker, 4)

    def _heal(self, target: Actor, amount: int) -> None:
        multiplier = float(self._affliction_modifiers(target).get("healing_mult", 1))
        target.hp = min(target.max_hp, target.hp + max(0, round(amount * multiplier)))
        if target.hp > 0:
            target.deaths_door = False

    def _change_stress(self, target: Actor, amount: int) -> None:
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

    def _decay_statuses(self, actor: Actor) -> None:
        for status in list(actor.statuses):
            if status == "stun":
                continue
            actor.statuses[status] -= 1
            if actor.statuses[status] <= 0:
                del actor.statuses[status]

    def _move(self, actor: Actor, amount: int) -> None:
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
            self.state.rewards = self.rng.sample(pool, k=min(3, len(pool)))
            self.state.phase = "reward"
        else:
            self.state.phase = "exploration"

    def service(self, action: str, card_index: int | None = None) -> None:
        if self.state.phase != "service":
            raise RuleError("no facility is available")
        if action == "recover" and self.state.service_type == "camp":
            for hero in self.living_heroes():
                self._heal(hero, 7)
                self._change_stress(hero, -10)
            self.add_log("The crew rests behind a welded door.")
        elif action == "upgrade":
            if card_index is None or not 0 <= card_index < len(self.state.deck):
                raise RuleError("choose a card to upgrade")
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
            self.add_log(f"Removed {self.catalog.cards[card.card_id]['name']}.")
        else:
            raise RuleError("that service is not available here")
        self.room().resolved = True
        self.state.service_type = None
        self.state.phase = "exploration"
