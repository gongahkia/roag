"""Autonomous colonists, work orders, production, hazards, and persistence."""

from __future__ import annotations

import json
import math
import os
import random
from collections import deque
from dataclasses import asdict, dataclass, field
from pathlib import Path

from .world import Cell, MINEABLE, NAMES, SOLID, Terrain, World

TICKS_PER_CYCLE = 480
SAVE_VERSION = 2
DEFAULT_SAVE = Path.home() / ".regolith" / "colony.json"
RESCUE_COST = {"food": 2.0, "water": 2.0}
SURVIVOR_PROFILES = (
    ("Mika", "Miner", "Restless"), ("Sol", "Botanist", "Optimist"),
    ("Inez", "Medic", "Steadfast"), ("Jae", "Miner", "Night owl"),
    ("Noor", "Engineer", "Stargazer"), ("Paz", "Engineer", "Resourceful"),
    ("Ren", "Botanist", "Patient"), ("Val", "Medic", "Gentle"),
    ("Ash", "Miner", "Tenacious"), ("Uma", "Engineer", "Curious"),
)


@dataclass(frozen=True)
class Blueprint:
    name: str
    glyph: str
    cost: dict[str, float]
    work: float
    description: str
    power: float = 0.0


BLUEPRINTS = {
    "wall": Blueprint("Pressure hull", "#", {"metal": 2}, 3, "Seals atmosphere. Build a complete enclosure."),
    "floor": Blueprint("Habitat deck", ".", {"metal": 1}, 2, "Walkable deck; carries power to adjacent decks."),
    "airlock": Blueprint("Airlock", "+", {"metal": 8}, 6, "Walkable pressure seal. Can replace a hull tile."),
    "solar": Blueprint("Solar array", "S", {"metal": 14, "crystal": 2}, 10, "Up to 12 kW in sunlight. Needs a deck connection.", -12),
    "battery": Blueprint("Battery bank", "B", {"metal": 16, "crystal": 2}, 9, "Adds 240 kWh of storage to its power network."),
    "oxygen": Blueprint("Life support", "O", {"metal": 20, "crystal": 4}, 12, "Uses water and 3 kW to release oxygen and heat.", 3),
    "farm": Blueprint("Hydroponics", "H", {"metal": 24, "crystal": 4}, 14, "Turns water into food. Needs pressure and 4 kW.", 4),
    "bunk": Blueprint("Sleeping pod", "=", {"metal": 6}, 5, "A real bed restores energy and morale."),
    "medbay": Blueprint("Med station", "+", {"metal": 20, "crystal": 6}, 12, "Heals nearby resting colonists when powered.", 2),
    "beacon": Blueprint("Earth uplink", "U", {"metal": 60, "crystal": 20}, 24, "Founding charter: run an uplink after cycle 3.", 5),
}


@dataclass
class Machine:
    kind: str
    x: int
    y: int
    powered: bool = False
    progress: float = 0.0
    health: float = 100.0
    energy: float = 0.0


@dataclass
class Job:
    id: int
    kind: str
    x: int
    y: int
    blueprint: str = ""
    progress: float = 0.0
    required: float = 5.0
    assigned: int | None = None
    reserved: bool = False


@dataclass
class Colonist:
    id: int
    name: str
    role: str
    trait: str
    x: int
    y: int
    health: float = 100.0
    hunger: float = 10.0
    energy: float = 90.0
    morale: float = 80.0
    suit: float = 100.0
    job: int | None = None
    activity: str = "Arriving"
    path: list[tuple[int, int]] = field(default_factory=list)
    rest: bool = False
    alive: bool = True
    worked: float = 0.0
    expedition: int | None = None


@dataclass
class Survivor:
    id: int
    name: str
    role: str
    trait: str
    x: int
    y: int
    health: float = 62.0
    suit: float = 68.0
    status: str = "stranded"
    low_oxygen_reported: bool = False

    @property
    def time_left(self) -> int:
        """Approximate ticks before exposure is fatal without assistance."""
        return max(0, math.ceil(self.suit / 0.16 + self.health / 0.6))


@dataclass
class Expedition:
    id: int
    survivor_id: int
    crew_id: int
    phase: str = "outbound"


class Simulation:
    def __init__(self, seed: int = 42, width: int = 88, height: int = 54):
        if width < 48 or height < 32:
            raise ValueError("World must be at least 48 by 32 cells")
        self.seed = seed
        self.rng = random.Random(seed)
        self.world = World(width, height, seed)
        self.tick = 0
        self.resources = {"metal": 150.0, "water": 90.0, "food": 100.0, "crystal": 16.0}
        self.charge = 220.0
        self.capacity = 240.0
        self.generated = 0.0
        self.demand = 0.0
        self.jobs: list[Job] = []
        self.next_job = 1
        self.machines: list[Machine] = []
        self.colonists: list[Colonist] = []
        self.survivors: list[Survivor] = []
        self.expeditions: list[Expedition] = []
        self.next_signal = 180
        self.log: list[dict] = []
        self.storm_until = 0
        self.victory = False
        self.game_over = False
        self._last_alert = -100
        self._network: dict[tuple[int, int], int] = {}
        self._setup()

    @property
    def cycle(self) -> int:
        return self.tick // TICKS_PER_CYCLE + 1

    @property
    def daylight(self) -> float:
        return max(0.0, math.sin((self.tick / TICKS_PER_CYCLE + 0.18) * 2 * math.pi))

    @property
    def storm(self) -> bool:
        return self.tick < self.storm_until

    @property
    def living(self) -> list[Colonist]:
        return [c for c in self.colonists if c.alive]

    @property
    def rescued(self) -> int:
        return sum(s.status == "rescued" for s in self.survivors)

    @property
    def distress(self) -> list[Survivor]:
        return [s for s in self.survivors if s.status in ("stranded", "escorted")]

    @property
    def pressure(self) -> float:
        cx, cy = self.world.center
        cells = [self.world.at(x, y) for y in range(cy - 4, cy + 5) for x in range(cx - 8, cx + 9)]
        floors = [c for c in cells if c.terrain == Terrain.FLOOR]
        return sum(c.oxygen for c in floors) / max(1, len(floors))

    @property
    def morale(self) -> float:
        return sum(c.morale for c in self.living) / max(1, len(self.living))

    def event(self, text: str, level: str = "info"):
        self.log.append({"tick": self.tick, "text": text, "level": level})
        self.log = self.log[-160:]

    def _setup(self):
        cx, cy = self.world.center
        for kind, dx, dy in (("rtg", -7, -3), ("oxygen", -5, -3), ("farm", 3, -3),
                              ("battery", -7, 3), ("farm", 5, -3)):
            self.machines.append(Machine(kind, cx + dx, cy + dy, energy=220.0 if kind == "battery" else 0.0))
        for dx in (-4, -2, 0, 2, 4, 6):
            self.machines.append(Machine("bunk", cx + dx, cy + 3))
        self.colonists.append(Colonist(0, "Ada", "Engineer", "Meticulous", cx - 5, cy))
        self.event("TRANQUILITY OUTPOST // Ada made it. The other pods are empty.", "good")
        self.event("Grow the colony by bringing stranded survivors home. Click a red ? to send help.")
        self.event("Charter: rescue 3 people; cycle 3, 4 living crew, food, and a powered uplink.")
        self.update_power()
        self._spawn_survivor()

    def _spawn_survivor(self) -> Survivor | None:
        """Place a distress contact on reachable terrain, including beyond fog."""
        cx, cy = self.world.center
        airlock = (cx + 9, cy)
        frontier = deque([(airlock, 0)])
        seen = {airlock}
        candidates = []
        occupied = {(s.x, s.y) for s in self.distress}
        while frontier:
            pos, distance = frontier.popleft()
            if distance > 90:
                continue
            x, y = pos
            if (abs(x - cx) >= 11 or abs(y - cy) >= 7) and pos not in occupied:
                if self.world.at(x, y).terrain == Terrain.VACUUM:
                    candidates.append(pos)
            for nxt in self.world.neighbors(x, y):
                if nxt not in seen and self.world.walkable(*nxt):
                    seen.add(nxt)
                    frontier.append((nxt, distance + 1))
        if not candidates:
            self.event("A distant distress signal fades behind impassable terrain.", "warn")
            return None
        used_names = {c.name for c in self.colonists} | {s.name for s in self.survivors}
        profile = next((p for p in SURVIVOR_PROFILES if p[0] not in used_names),
                       (f"Wanderer {len(self.survivors) + 1}", "Miner", "Survivor"))
        offsets = ((14, 2), (13, -11), (15, 8), (-12, 8), (-13, -8))
        dx, dy = offsets[len(self.survivors) % len(offsets)]
        preferred = (cx + dx, cy + dy)
        candidates.sort(key=lambda p: (abs(p[0] - preferred[0]) + abs(p[1] - preferred[1]), p))
        x, y = candidates[0] if not self.survivors else self.rng.choice(candidates[:5])
        survivor = Survivor(len(self.survivors), *profile, x, y)
        self.survivors.append(survivor)
        self.world.reveal(x, y, 1)
        self.event(f"DISTRESS: {survivor.name}, {survivor.role.lower()}, stranded at {x},{y}. Click their ? to rescue.", "warn")
        return survivor

    def expedition_for(self, survivor_id: int) -> Expedition | None:
        return next((e for e in reversed(self.expeditions) if e.survivor_id == survivor_id
                     and e.phase in ("outbound", "returning")), None)

    def dispatch_expedition(self, survivor_id: int, crew_id: int | None = None) -> tuple[bool, str]:
        survivor = next((s for s in self.survivors if s.id == survivor_id), None)
        if not survivor or survivor.status != "stranded":
            return False, "That survivor is no longer awaiting rescue."
        if self.expedition_for(survivor_id):
            return False, "An expedition is already on its way."
        if self.game_over:
            return False, "There is nobody left to send."
        if not self.can_afford(RESCUE_COST):
            return False, "A rescue kit needs 2 food and 2 water."
        choices = []
        for c in self.living:
            if crew_id is not None and c.id != crew_id:
                continue
            if c.expedition is not None or c.rest or c.health < 40 or c.energy < 25 or c.suit < 65:
                continue
            route = self.path_to((c.x, c.y), {(survivor.x, survivor.y)})
            if route is not None:
                choices.append((len(route), c.id, c, route))
        if not choices:
            return False, "No ready crew with a safe route. Allow recovery, suit recharge, or clear a path."
        _, _, c, route = min(choices, key=lambda choice: choice[:2])
        for key, value in RESCUE_COST.items():
            self.resources[key] -= value
        self._release(c)
        expedition = Expedition(len(self.expeditions), survivor.id, c.id)
        self.expeditions.append(expedition)
        c.expedition, c.path, c.activity = expedition.id, route, f"Rescuing {survivor.name}"
        message = f"{c.name} dispatched to rescue {survivor.name}. Kit: 2 food, 2 water."
        self.event(message, "good")
        return True, message

    def _end_expedition(self, c: Colonist, expedition: Expedition, reason: str):
        survivor = next(s for s in self.survivors if s.id == expedition.survivor_id)
        if survivor.status == "escorted":
            survivor.status = "stranded"
            survivor.x, survivor.y = c.x, c.y
        expedition.phase = "failed"
        c.expedition, c.path = None, []
        self.event(reason, "danger")

    def _expedition_tick(self, c: Colonist):
        expedition = next((e for e in self.expeditions if e.id == c.expedition), None)
        if not expedition or expedition.phase not in ("outbound", "returning"):
            c.expedition, c.path = None, []
            return
        survivor = next(s for s in self.survivors if s.id == expedition.survivor_id)
        if survivor.status == "lost":
            self._end_expedition(c, expedition, f"{c.name}'s expedition aborted: {survivor.name}'s signal is gone.")
            return
        if expedition.phase == "outbound":
            if c.suit < 28 or c.energy < 18 or self.world.at(c.x, c.y).fire > 0.15:
                self._end_expedition(c, expedition, f"{c.name} aborted the rescue to seek safety. {survivor.name} still needs help.")
                return
            if (c.x, c.y) == (survivor.x, survivor.y):
                survivor.status, survivor.suit = "escorted", max(60.0, survivor.suit)
                survivor.health = min(100.0, survivor.health + 8)
                expedition.phase, c.path = "returning", []
                self.event(f"{c.name} reached {survivor.name}. Emergency oxygen shared; escorting home.", "good")
            else:
                if not c.path:
                    route = self.path_to((c.x, c.y), {(survivor.x, survivor.y)})
                    if route is None:
                        self._end_expedition(c, expedition, f"Route blocked: {c.name} cannot reach {survivor.name}.")
                        return
                    c.path = route
                self._move(c)
                c.activity = f"Rescuing {survivor.name}"
                return
        cell = self.world.at(c.x, c.y)
        c.activity = f"Escorting {survivor.name} home"
        if cell.terrain == Terrain.FLOOR and cell.oxygen > 0.5 and cell.fire < 0.1:
            colonist_id = max((person.id for person in self.colonists), default=-1) + 1
            self.colonists.append(Colonist(colonist_id, survivor.name, survivor.role, survivor.trait,
                                           c.x, c.y, health=survivor.health, hunger=55.0,
                                           energy=20.0, morale=55.0, suit=survivor.suit,
                                           rest=True, activity="Recovering from the wasteland"))
            survivor.x, survivor.y, survivor.status = c.x, c.y, "rescued"
            expedition.phase, c.expedition, c.path = "complete", None, []
            c.morale = min(100.0, c.morale + 12)
            self.event(f"HOME: {survivor.name} rescued and joined the colony. {len(self.living)} living crew.", "good")
            return
        if self.tick % 2 == 0:
            if not c.path:
                route = self.path_to((c.x, c.y), self._safe_tiles())
                if route is None:
                    self._end_expedition(c, expedition, f"No pressurized shelter for {c.name} and {survivor.name}. Repair the habitat.")
                    return
                c.path = route
            self._move(c)
            survivor.x, survivor.y = c.x, c.y

    def _survivor_tick(self):
        for survivor in self.distress:
            cell = self.world.at(survivor.x, survivor.y)
            if cell.terrain == Terrain.FLOOR and cell.oxygen > 0.32 and cell.fire < 0.1:
                survivor.suit = min(100.0, survivor.suit + 1.0)
            else:
                survivor.suit = max(0.0, survivor.suit - (0.24 if self.storm else 0.16))
                if survivor.suit <= 0:
                    survivor.health = max(0.0, survivor.health - 0.6)
            if cell.fire > 0.1:
                survivor.health = max(0.0, survivor.health - cell.fire)
            if survivor.suit <= 15 and not survivor.low_oxygen_reported:
                survivor.low_oxygen_reported = True
                self.event(f"{survivor.name}: reserve oxygen critical. A rescue cannot wait much longer.", "danger")
            if survivor.health <= 0:
                survivor.status = "lost"
                self.event(f"{survivor.name}'s distress signal went silent. Lost in the wasteland.", "danger")

    def machine_at(self, x: int, y: int) -> Machine | None:
        return next((m for m in self.machines if m.x == x and m.y == y), None)

    def survivor_at(self, x: int, y: int) -> Survivor | None:
        return next((s for s in self.distress if (s.x, s.y) == (x, y)), None)

    def job_at(self, x: int, y: int) -> Job | None:
        return next((j for j in self.jobs if j.x == x and j.y == y), None)

    def can_afford(self, cost: dict[str, float]) -> bool:
        return all(self.resources.get(k, 0.0) >= v for k, v in cost.items())

    def designate(self, x: int, y: int, tool: str, blueprint: str = "") -> tuple[bool, str]:
        world = self.world
        if not world.inside(x, y) or not world.at(x, y).discovered:
            return False, "Explore closer before placing an order."
        if tool == "cancel":
            job = self.job_at(x, y)
            if job:
                self.cancel_job(job)
                return True, "Order cancelled; reserved materials returned."
            return False, "No order here."
        if self.job_at(x, y):
            return False, "An order is already queued here."
        cell, machine = world.at(x, y), self.machine_at(x, y)
        if tool == "survey":
            if not world.walkable(x, y):
                return False, "Survey an open surface or deck tile."
            job = Job(self.next_job, "survey", x, y, required=1)
        elif tool == "dig":
            if cell.terrain not in MINEABLE and cell.terrain not in (Terrain.WALL, Terrain.AIRLOCK) and not machine:
                return False, "Choose basalt, a resource seam, hull, or a machine."
            job = Job(self.next_job, "salvage" if machine or cell.terrain in (Terrain.WALL, Terrain.AIRLOCK) else "dig", x, y)
            job.required = 5 if cell.terrain == Terrain.ROCK else 7
        elif tool == "build" and blueprint in BLUEPRINTS:
            bp = BLUEPRINTS[blueprint]
            if machine:
                return False, "Salvage the existing machine first."
            if blueprint == "airlock":
                allowed = (Terrain.VACUUM, Terrain.FLOOR, Terrain.WALL)
            elif blueprint in ("wall", "floor"):
                allowed = (Terrain.VACUUM, Terrain.FLOOR)
            else:
                allowed = (Terrain.FLOOR,)
            if cell.terrain not in allowed or (blueprint == "floor" and cell.terrain == Terrain.FLOOR):
                return False, "Machines need deck. Dig rock first; hull and deck can start on surface."
            if blueprint == "wall" and any(c.alive and (c.x, c.y) == (x, y) for c in self.colonists):
                return False, "Crew are standing here."
            if not self.can_afford(bp.cost):
                return False, "Insufficient material: " + ", ".join(f"{v:g} {k}" for k, v in bp.cost.items())
            for k, v in bp.cost.items():
                self.resources[k] -= v
            job = Job(self.next_job, "build", x, y, blueprint, required=bp.work, reserved=True)
        else:
            return False, "Choose a work tool first."
        self.jobs.append(job)
        self.next_job += 1
        return True, "Work order queued. Crew will find a safe route."

    def cancel_job(self, job: Job):
        if job.reserved:
            for k, v in BLUEPRINTS[job.blueprint].cost.items():
                self.resources[k] += v
        for c in self.colonists:
            if c.job == job.id:
                c.job, c.path = None, []
        if job in self.jobs:
            self.jobs.remove(job)

    def path_to(self, start: tuple[int, int], targets: set[tuple[int, int]]) -> list[tuple[int, int]] | None:
        """Breadth-first routing to the nearest reachable work/rest tile."""
        if start in targets:
            return []
        frontier = deque([start])
        parent: dict[tuple[int, int], tuple[int, int] | None] = {start: None}
        found = None
        while frontier:
            pos = frontier.popleft()
            for nxt in self.world.neighbors(*pos):
                if nxt in parent or not self.world.walkable(*nxt):
                    continue
                if self.world.at(*nxt).fire > 0.5:
                    continue
                parent[nxt] = pos
                if nxt in targets:
                    found = nxt
                    break
                frontier.append(nxt)
            if found is not None:
                break
        if found is None:
            return None
        route = []
        while found != start:
            route.append(found)
            found = parent[found]  # type: ignore[assignment]
        route.reverse()
        return route

    def job_targets(self, job: Job) -> set[tuple[int, int]]:
        return {p for p in self.world.neighbors(job.x, job.y) if self.world.walkable(*p)}

    def update_power(self, advance: bool = False, rebuild: bool = True):
        # Deck and airlock adjacency defines independent electrical islands.
        if rebuild or not self._network:
            self._rebuild_network()
        self.capacity = 240.0 * sum(m.kind == "battery" for m in self.machines)
        self.generated, self.demand = 0.0, 0.0
        groups: dict[int, list[Machine]] = {}
        for m in self.machines:
            m.powered = False
            if (m.x, m.y) in self._network:
                groups.setdefault(self._network[(m.x, m.y)], []).append(m)
        for machines in groups.values():
            supply = sum(12.0 if m.kind == "rtg" else 12.0 * self.daylight * (0.15 if self.storm else 1.0)
                         if m.kind == "solar" else 0.0 for m in machines)
            consumers = [m for m in machines if m.kind in BLUEPRINTS and BLUEPRINTS[m.kind].power > 0]
            consumers.sort(key=lambda m: {"oxygen": 0, "farm": 1, "medbay": 2, "beacon": 3}[m.kind])
            demand = sum(BLUEPRINTS[m.kind].power for m in consumers)
            banks = [m for m in machines if m.kind == "battery"]
            stored = sum(m.energy for m in banks)
            available = supply + (min(16.0, stored / 0.05) if banks else 0.0)
            used = 0.0
            for m in consumers:
                draw = BLUEPRINTS[m.kind].power
                if used + draw <= available + 1e-8:
                    m.powered = True
                    used += draw
            for m in machines:
                if m.kind in ("rtg", "solar", "battery", "bunk"):
                    m.powered = True
            self.generated += supply
            self.demand += demand
            if banks and advance:
                energy = max(0.0, min(240.0 * len(banks), stored + min(16.0, supply - used) * 0.05))
                for bank in banks:
                    bank.energy = energy / len(banks)
        self.charge = sum(m.energy for m in self.machines if m.kind == "battery")

    def _rebuild_network(self):
        self._network = {}
        group = 0
        for y in range(self.world.height):
            for x in range(self.world.width):
                if (x, y) in self._network or self.world.at(x, y).terrain not in (Terrain.FLOOR, Terrain.AIRLOCK):
                    continue
                stack = [(x, y)]
                self._network[(x, y)] = group
                while stack:
                    pos = stack.pop()
                    for nxt in self.world.neighbors(*pos):
                        if nxt not in self._network and self.world.at(*nxt).terrain in (Terrain.FLOOR, Terrain.AIRLOCK):
                            self._network[nxt] = group
                            stack.append(nxt)
                group += 1

    def _production(self):
        for m in list(self.machines):
            cell = self.world.at(m.x, m.y)
            if cell.fire > 0.2:
                m.health -= cell.fire * 0.4
            if m.health <= 0:
                self.machines.remove(m)
                self.event(f"{self.machine_name(m)} destroyed by fire.", "danger")
                continue
            if not m.powered:
                continue
            if m.kind == "rtg":
                cell.heat += 0.5
            elif m.kind == "oxygen" and self.resources["water"] >= 0.014:
                self.resources["water"] -= 0.014
                cell.oxygen = min(1.0, cell.oxygen + 0.24)
                cell.heat += max(0.0, (26.0 - cell.heat) * 0.12)
            elif m.kind == "farm":
                if cell.oxygen >= 0.38 and 0 <= cell.heat < 46 and self.resources["water"] >= 0.008:
                    tending = any(c.alive and c.role == "Botanist" and abs(c.x-m.x) + abs(c.y-m.y) <= 2 for c in self.colonists)
                    m.progress += 1.8 if tending else 1.0
                    self.resources["water"] -= 0.008
                    if m.progress >= 160:
                        m.progress -= 160
                        self.resources["food"] += 3
                        self.event("Hydroponics harvest: +3 food.", "good")

    @staticmethod
    def machine_name(m: Machine) -> str:
        return "Radioisotope generator" if m.kind == "rtg" else BLUEPRINTS[m.kind].name

    def _release(self, c: Colonist):
        if c.job is not None:
            job = next((j for j in self.jobs if j.id == c.job), None)
            if job:
                job.assigned = None
        c.job, c.path = None, []

    def _move(self, c: Colonist) -> bool:
        if not c.path:
            return False
        nx, ny = c.path[0]
        if not self.world.walkable(nx, ny) or self.world.at(nx, ny).fire > 0.5:
            c.path = []
            return False
        c.x, c.y = c.path.pop(0)
        self.world.reveal(c.x, c.y, 5)
        return True

    def _safe_tiles(self, beds: bool = False) -> set[tuple[int, int]]:
        if beds:
            return {(m.x, m.y) for m in self.machines if m.kind == "bunk"
                    and self.world.at(m.x, m.y).oxygen > 0.35 and self.world.at(m.x, m.y).fire < 0.1}
        return {(x, y) for y in range(self.world.height) for x in range(self.world.width)
                if self.world.at(x, y).terrain == Terrain.FLOOR
                and self.world.at(x, y).oxygen > 0.5 and self.world.at(x, y).fire < 0.1}

    def _complete(self, c: Colonist, job: Job):
        cell = self.world.at(job.x, job.y)
        if job.kind == "survey":
            self.world.reveal(job.x, job.y, 7)
            self.event(f"{c.name} surveyed sector {job.x},{job.y}.", "good")
        elif job.kind in ("dig", "salvage"):
            machine = self.machine_at(job.x, job.y)
            if machine:
                if machine.kind in BLUEPRINTS:
                    for k, v in BLUEPRINTS[machine.kind].cost.items():
                        self.resources[k] += v * 0.5
                self.machines.remove(machine)
                self.event(f"{c.name} salvaged {self.machine_name(machine).lower()}.")
            else:
                terrain = cell.terrain
                reward = {Terrain.ICE: ("water", 24), Terrain.ORE: ("metal", 18),
                          Terrain.CRYSTAL: ("crystal", 10), Terrain.ROCK: ("metal", 2),
                          Terrain.WALL: ("metal", 1), Terrain.AIRLOCK: ("metal", 4)}.get(terrain)
                if reward:
                    self.resources[reward[0]] += reward[1]
                    self.event(f"{c.name}: +{reward[1]} {reward[0]}.", "good")
                cell.terrain = Terrain.FLOOR
                self.world.reveal(job.x, job.y, 4)
        elif job.kind == "build":
            if job.blueprint == "wall":
                # Let a bystander clear the construction tile before sealing it.
                for other in self.living:
                    if (other.x, other.y) == (job.x, job.y):
                        exits = [p for p in self.world.neighbors(other.x, other.y) if self.world.walkable(*p)]
                        if not exits:
                            return
                        other.x, other.y = exits[0]
                        other.path = []
                cell.terrain, cell.oxygen, cell.fire = Terrain.WALL, 0.0, 0.0
            elif job.blueprint == "floor":
                cell.terrain = Terrain.FLOOR
            elif job.blueprint == "airlock":
                cell.terrain, cell.oxygen, cell.fire = Terrain.AIRLOCK, 0.0, 0.0
            else:
                self.machines.append(Machine(job.blueprint, job.x, job.y))
            self.event(f"{c.name} completed {BLUEPRINTS[job.blueprint].name.lower()}.", "good")
        self.jobs.remove(job)
        c.job, c.path = None, []
        c.worked += 1
        c.morale = min(100.0, c.morale + 1.5)
        self.update_power()

    def _crew_tick(self, c: Colonist):
        cell = self.world.at(c.x, c.y)
        breathable = cell.terrain == Terrain.FLOOR and cell.oxygen > 0.32 and cell.fire < 0.2
        c.hunger = min(100.0, c.hunger + 0.12)
        c.energy = max(0.0, c.energy - (0.10 if c.job else 0.035))
        if breathable:
            c.suit = min(100.0, c.suit + 1.6)
            cell.oxygen = max(0.0, cell.oxygen - 0.002)
        else:
            c.suit = max(0.0, c.suit - (0.28 if self.storm else 0.19))
            if c.suit <= 0:
                c.health -= 1.6
        if cell.fire > 0.15:
            c.health -= cell.fire * 1.6
        if cell.heat > 65:
            c.health -= 0.25
        if c.hunger >= 85:
            c.health -= 0.12
            c.morale = max(0.0, c.morale - 0.1)
        if c.hunger >= 35 and breathable and self.resources["food"] >= 2:
            self.resources["food"] -= 2
            c.hunger = max(0.0, c.hunger - 27)
            c.morale = min(100.0, c.morale + 1)
        if c.health <= 0:
            c.alive, c.health, c.activity = False, 0.0, "Lost"
            self._release(c)
            if c.expedition is not None:
                expedition = next(e for e in self.expeditions if e.id == c.expedition)
                self._end_expedition(c, expedition, f"{c.name}'s rescue expedition was lost.")
            self.event(f"{c.name} has died. The outpost falls silent for a moment.", "danger")
            for other in self.living:
                other.morale = max(0.0, other.morale - 18)
            return
        if c.expedition is not None:
            self._expedition_tick(c)
            return
        # Emergency retreat overrides work, including a route already in progress.
        danger = cell.fire > 0.15 or c.suit < 28 or (c.health < 30 and not breathable)
        if danger:
            self._release(c)
            path = self.path_to((c.x, c.y), self._safe_tiles())
            if path:
                c.path = path
                self._move(c)
                c.activity = "Emergency retreat"
                return
            if not breathable:
                c.activity = "Seeking safe pressure"
                return
        if c.energy < 18:
            c.rest = True
            self._release(c)
        if c.rest:
            beds = self._safe_tiles(beds=True)
            if (c.x, c.y) in beds or (not beds and breathable):
                c.activity = "Sleeping"
                c.energy = min(100.0, c.energy + 1.15)
                c.health = min(100.0, c.health + 0.04)
                c.morale = min(100.0, c.morale + 0.025)
                if c.energy >= 94:
                    c.rest = False
            else:
                if not c.path:
                    c.path = self.path_to((c.x, c.y), beds or self._safe_tiles()) or []
                self._move(c)
                c.activity = "Going to sleep"
            return
        if c.job is not None:
            job = next((j for j in self.jobs if j.id == c.job), None)
            if not job:
                self._release(c)
            elif (c.x, c.y) in self.job_targets(job):
                c.path = []
                rate = 0.56 if (job.kind == "dig" and c.role == "Miner") or (job.kind == "build" and c.role == "Engineer") else 0.34
                job.progress += rate * (0.7 + c.morale / 200)
                c.activity = ("Mining " + NAMES[self.world.at(job.x, job.y).terrain].lower()) if job.kind == "dig" else (
                    "Building " + BLUEPRINTS[job.blueprint].name.lower() if job.kind == "build" else "Surveying" if job.kind == "survey" else "Salvaging")
                if job.progress >= job.required:
                    self._complete(c, job)
                return
            else:
                if not c.path:
                    route = self.path_to((c.x, c.y), self.job_targets(job))
                    if route is None:
                        self._release(c)
                    else:
                        c.path = route
                if self._move(c):
                    c.activity = "Walking to work"
                    return
        # Firefighting is an autonomous local task, with no manual micro-management.
        nearby = [(x, y) for x, y in self.world.neighbors(c.x, c.y) if self.world.at(x, y).fire > 0]
        if nearby and breathable:
            for x, y in nearby:
                self.world.at(x, y).fire = max(0.0, self.world.at(x, y).fire - 0.22)
            c.activity = "Fighting fire"
            return
        if self.tick % 6 == c.id % 6:
            candidates = [j for j in self.jobs if j.assigned is None]
            candidates.sort(key=lambda j: abs(j.x - c.x) + abs(j.y - c.y)
                            - (10 if (j.kind == "dig" and c.role == "Miner") or (j.kind == "build" and c.role == "Engineer") else 0))
            for job in candidates:
                route = self.path_to((c.x, c.y), self.job_targets(job))
                if route is not None:
                    job.assigned, c.job, c.path = c.id, job.id, route
                    c.activity = "Taking work order"
                    return
        if c.path:
            self._move(c)
            c.activity = "Tending crops" if c.role == "Botanist" else "Patrolling habitat"
            return
        if c.health < 90 and breathable:
            medical = any(m.kind == "medbay" and m.powered and abs(m.x-c.x) + abs(m.y-c.y) <= 4 for m in self.machines)
            c.health = min(100.0, c.health + (0.25 if medical else 0.015))
        if self.tick % 32 == c.id:
            if c.role == "Botanist":
                targets = {(m.x, m.y) for m in self.machines if m.kind == "farm" and m.powered}
            elif not breathable or c.suit < 80:
                targets = self._safe_tiles()
            else:
                cx, cy = self.world.center
                targets = {(cx + self.rng.randint(-7, 7), cy + self.rng.randint(-3, 2))}
            c.path = self.path_to((c.x, c.y), targets) or []
        c.activity = "Tending crops" if c.role == "Botanist" else "On standby"

    def meteor(self, x: int | None = None, y: int | None = None):
        cx, cy = self.world.center
        if x is None or y is None:
            x, y = self.rng.choice([(cx + dx, cy - 5) for dx in range(-7, 8)]
                                    + [(cx + dx, cy + 5) for dx in range(-7, 8)])
        cell = self.world.at(x, y)
        cell.terrain, cell.oxygen = Terrain.VACUUM, 0.0
        machine = self.machine_at(x, y)
        if machine:
            self.machines.remove(machine)
        self.world.reveal(x, y, 4)
        self.event(f"Micrometeorite impact at {x},{y}. Hull breach! Queue a pressure hull repair.", "danger")
        self.update_power()

    def _events(self):
        if self.tick >= self.next_signal:
            if len(self.distress) < 3:
                self._spawn_survivor()
            self.next_signal = self.tick + 240
        # A readable first threat gives the player time to learn the base.
        if self.tick == 280:
            self.event("Long-range radar: debris cloud approaching next cycle.", "warn")
        if self.tick >= 540 and (self.tick - 540) % 640 == 0:
            self.meteor()
        if self.tick > 700 and self.tick % 710 == 0:
            self.storm_until = self.tick + 90
            self.event("Solar particle storm. Array output drops; EVA consumes extra oxygen.", "warn")
        if self.tick > 800 and self.tick % 977 == 0:
            candidates = [m for m in self.machines if m.kind in ("battery", "farm")]
            if candidates:
                m = self.rng.choice(candidates)
                self.world.at(m.x, m.y).fire = 0.5
                self.event(f"Electrical fault in {self.machine_name(m).lower()}. Fire detected.", "danger")
        if self.tick % 120 == 0:
            if self.pressure < 0.40:
                self.event("Habitat pressure critical. Seal breaches and check life support.", "danger")
            if self.resources["water"] < 15:
                self.event("Water reserve low. Designate blue ice seams for mining.", "warn")
            if self.resources["food"] < 15:
                self.event("Food reserve low. Hydroponics needs power, water, and pressure.", "warn")
        if not self.victory and self.rescued >= 3 and self.cycle >= 3 and len(self.living) >= 4 and self.resources["food"] >= 20:
            if any(m.kind == "beacon" and m.powered and self.world.at(m.x, m.y).oxygen >= 0.4 for m in self.machines):
                self.victory = True
                self.event("EARTH ANSWERS. Founding charter granted. This is home now. Keep building.", "good")
        if not self.living and not self.game_over:
            self.game_over = True
            self.event("No living crew remain. Their names will go back to Earth.", "danger")

    def step(self, count: int = 1):
        for _ in range(count):
            if self.game_over:
                break
            self.tick += 1
            self.update_power(advance=True, rebuild=False)
            self._production()
            self._survivor_tick()
            for c in list(self.colonists):
                if c.alive:
                    self._crew_tick(c)
            self.world.environment(self.rng, self.daylight, self.storm)
            self._events()

    def summary(self) -> dict:
        return {"seed": self.seed, "tick": self.tick, "cycle": self.cycle,
                "living": len(self.living), "pressure": round(self.pressure, 3),
                "resources": {k: round(v, 2) for k, v in self.resources.items()},
                "power_kw": round(self.generated, 2), "demand_kw": round(self.demand, 2),
                "battery": round(self.charge, 2), "jobs": len(self.jobs),
                "rescued": self.rescued,
                "distress": [{"name": s.name, "role": s.role, "x": s.x, "y": s.y,
                              "status": s.status, "health": round(s.health, 1),
                              "suit": round(s.suit, 1), "time_left": s.time_left} for s in self.distress],
                "expeditions": [asdict(e) for e in self.expeditions if e.phase in ("outbound", "returning")],
                "charter": self.victory, "game_over": self.game_over,
                "events": self.log[-6:]}

    def to_dict(self) -> dict:
        return {"version": SAVE_VERSION, "seed": self.seed, "tick": self.tick,
                "width": self.world.width, "height": self.world.height,
                "resources": self.resources.copy(), "charge": self.charge,
                "next_job": self.next_job, "storm_until": self.storm_until,
                "victory": self.victory, "game_over": self.game_over, "log": self.log[:],
                "rng": self.rng.getstate(),
                "cells": [[int(c.terrain), c.oxygen, c.heat, c.fire, c.discovered] for c in self.world.cells],
                "colonists": [asdict(c) for c in self.colonists],
                "machines": [asdict(m) for m in self.machines], "jobs": [asdict(j) for j in self.jobs],
                "survivors": [asdict(s) for s in self.survivors],
                "expeditions": [asdict(e) for e in self.expeditions], "next_signal": self.next_signal}

    def save(self, path: Path = DEFAULT_SAVE):
        path = Path(path).expanduser()
        path.parent.mkdir(parents=True, exist_ok=True)
        temporary = path.with_suffix(path.suffix + ".tmp")
        try:
            with temporary.open("w", encoding="utf-8") as handle:
                json.dump(self.to_dict(), handle, separators=(",", ":"), allow_nan=False)
                handle.flush()
                os.fsync(handle.fileno())
            os.replace(temporary, path)
        finally:
            temporary.unlink(missing_ok=True)

    @classmethod
    def load(cls, path: Path = DEFAULT_SAVE) -> Simulation:
        data = json.loads(Path(path).expanduser().read_text(encoding="utf-8"))
        if not isinstance(data, dict) or data.get("version") not in (1, SAVE_VERSION):
            raise ValueError("Unsupported colony save version")
        width, height = data["width"], data["height"]
        if not isinstance(width, int) or not isinstance(height, int) or not (48 <= width <= 256 and 32 <= height <= 256):
            raise ValueError("Invalid world dimensions in save")
        if len(data["cells"]) != width * height:
            raise ValueError("Incomplete world in save")
        sim = cls.__new__(cls)
        sim.seed, sim.tick = int(data["seed"]), int(data["tick"])
        sim.world = World.__new__(World)
        sim.world.width, sim.world.height, sim.world.seed = width, height, sim.seed
        sim.world.cells = [Cell(Terrain(t), float(o), float(h), float(f), bool(d)) for t, o, h, f, d in data["cells"]]
        for cell in sim.world.cells:
            if not all(math.isfinite(v) for v in (cell.oxygen, cell.heat, cell.fire)):
                raise ValueError("Non-finite environment value in save")
        sim.resources = {key: float(data["resources"][key]) for key in ("metal", "water", "food", "crystal")}
        if not all(math.isfinite(v) and v >= 0 for v in sim.resources.values()):
            raise ValueError("Invalid resources in save")
        sim.charge = float(data["charge"])
        sim.next_job = int(data["next_job"])
        sim.storm_until = int(data["storm_until"])
        sim.victory, sim.game_over = bool(data["victory"]), bool(data["game_over"])
        sim.log = data["log"][-160:]
        sim.colonists = [Colonist(**c) for c in data["colonists"]]
        sim.survivors = [Survivor(**s) for s in data.get("survivors", [])]
        sim.expeditions = [Expedition(**e) for e in data.get("expeditions", [])]
        sim.next_signal = int(data.get("next_signal", sim.tick + 180))
        for c in sim.colonists:
            c.path = [tuple(p) for p in c.path]
            if not sim.world.inside(c.x, c.y):
                raise ValueError("Crew outside world in save")
        sim.machines = [Machine(**m) for m in data["machines"]]
        sim.jobs = [Job(**j) for j in data["jobs"]]
        for item in [*sim.machines, *sim.jobs, *sim.survivors]:
            if not sim.world.inside(item.x, item.y):
                raise ValueError("Object outside world in save")
        survivor_ids = {s.id for s in sim.survivors}
        crew_ids = {c.id for c in sim.colonists}
        expedition_ids = {e.id for e in sim.expeditions}
        if any(e.survivor_id not in survivor_ids or e.crew_id not in crew_ids for e in sim.expeditions):
            raise ValueError("Invalid expedition in save")
        if any(c.expedition is not None and c.expedition not in expedition_ids for c in sim.colonists):
            raise ValueError("Invalid expedition assignment in save")
        sim.rng = random.Random()

        def tuples(value):
            return tuple(tuples(x) for x in value) if isinstance(value, list) else value

        sim.rng.setstate(tuples(data["rng"]))
        sim._last_alert, sim._network = -100, {}
        sim.capacity, sim.generated, sim.demand = 0.0, 0.0, 0.0
        # Recompute derived network state without advancing battery time.
        sim.update_power()
        return sim
