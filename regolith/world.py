"""Seeded geology and synchronous, local-rule environmental automata."""

from __future__ import annotations

import random
from dataclasses import dataclass
from enum import IntEnum


class Terrain(IntEnum):
    VACUUM = 0
    ROCK = 1
    ICE = 2
    ORE = 3
    CRYSTAL = 4
    FLOOR = 5
    WALL = 6
    AIRLOCK = 7


SOLID = {Terrain.ROCK, Terrain.ICE, Terrain.ORE, Terrain.CRYSTAL, Terrain.WALL}
MINEABLE = {Terrain.ROCK, Terrain.ICE, Terrain.ORE, Terrain.CRYSTAL}
SEALED = {Terrain.FLOOR}
NAMES = {
    Terrain.VACUUM: "Lunar surface",
    Terrain.ROCK: "Basalt",
    Terrain.ICE: "Water ice",
    Terrain.ORE: "Iron deposit",
    Terrain.CRYSTAL: "Silicate crystal",
    Terrain.FLOOR: "Habitat deck",
    Terrain.WALL: "Pressure hull",
    Terrain.AIRLOCK: "Cycling airlock",
}


@dataclass
class Cell:
    terrain: Terrain
    oxygen: float = 0.0
    heat: float = -35.0
    fire: float = 0.0
    discovered: bool = False


class World:
    def __init__(self, width: int, height: int, seed: int):
        self.width, self.height, self.seed = width, height, seed
        self.cells: list[Cell] = []
        self._generate(random.Random(seed))

    @property
    def center(self) -> tuple[int, int]:
        return self.width // 2, self.height // 2

    def index(self, x: int, y: int) -> int:
        return y * self.width + x

    def inside(self, x: int, y: int) -> bool:
        return 0 <= x < self.width and 0 <= y < self.height

    def at(self, x: int, y: int) -> Cell:
        return self.cells[self.index(x, y)]

    def neighbors(self, x: int, y: int):
        for nx, ny in ((x, y - 1), (x - 1, y), (x + 1, y), (x, y + 1)):
            if self.inside(nx, ny):
                yield nx, ny

    def walkable(self, x: int, y: int) -> bool:
        return self.inside(x, y) and self.at(x, y).terrain not in SOLID

    def reveal(self, x: int, y: int, radius: int = 5):
        for yy in range(max(0, y - radius), min(self.height, y + radius + 1)):
            for xx in range(max(0, x - radius), min(self.width, x + radius + 1)):
                if (xx - x) ** 2 + (yy - y) ** 2 <= radius ** 2:
                    self.at(xx, yy).discovered = True

    def _generate(self, rng: random.Random):
        # Moore-neighborhood majority rule creates connected basalt massifs.
        mask = [rng.random() < 0.44 for _ in range(self.width * self.height)]
        for _ in range(5):
            next_mask = mask[:]
            for y in range(self.height):
                for x in range(self.width):
                    count = sum(
                        1
                        for dy in (-1, 0, 1)
                        for dx in (-1, 0, 1)
                        if (dx or dy)
                        and (not self.inside(x + dx, y + dy)
                             or mask[self.index(x + dx, y + dy)])
                    )
                    next_mask[self.index(x, y)] = count >= (4 if mask[self.index(x, y)] else 5)
            mask = next_mask
        self.cells = [Cell(Terrain.ROCK if solid else Terrain.VACUUM) for solid in mask]
        for _ in range(65):
            x, y = rng.randrange(2, self.width - 2), rng.randrange(2, self.height - 2)
            resource = rng.choices([Terrain.ICE, Terrain.ORE, Terrain.CRYSTAL], [4, 5, 2])[0]
            for _ in range(rng.randint(4, 13)):
                if self.inside(x, y):
                    self.at(x, y).terrain = resource
                dx, dy = rng.choice(((1, 0), (-1, 0), (0, 1), (0, -1)))
                x, y = min(self.width - 2, max(1, x + dx)), min(self.height - 2, max(1, y + dy))
        cx, cy = self.center
        # Safe landing clearing, with finite starter seams just beyond the hull.
        for y in range(cy - 9, cy + 10):
            for x in range(cx - 13, cx + 14):
                self.at(x, y).terrain = Terrain.VACUUM
        for y in range(cy - 5, cy + 6):
            for x in range(cx - 9, cx + 10):
                cell = self.at(x, y)
                edge = abs(x - cx) == 9 or abs(y - cy) == 5
                cell.terrain = Terrain.WALL if edge else Terrain.FLOOR
                cell.oxygen = 0.0 if edge else 0.92
                cell.heat = 19.0 if not edge else -10.0
        self.at(cx + 9, cy).terrain = Terrain.AIRLOCK
        self.at(cx + 9, cy).heat = 5.0
        for ox, oy, resource in ((-7, -8, Terrain.ICE), (5, 8, Terrain.ORE), (12, -5, Terrain.CRYSTAL)):
            for dy in range(3):
                for dx in range(3):
                    self.at(cx + ox + dx, cy + oy + dy).terrain = resource
        self.reveal(cx, cy, 19)

    def environment(self, rng: random.Random, daylight: float, storm: bool = False):
        """One double-buffered von Neumann update; no traversal-order bias.

        Each open edge exchanges 12% of its oxygen difference and 5% of its
        temperature difference. Surface cells vent; airlocks are gas barriers.
        Fire consumes oxygen, heats neighbors, and propagates locally.
        """
        oxygen = [c.oxygen for c in self.cells]
        heat = [c.heat for c in self.cells]
        fire = [c.fire for c in self.cells]
        ambient = -55.0 + 85.0 * daylight
        for y in range(self.height):
            for x in range(self.width):
                i = self.index(x, y)
                cell = self.cells[i]
                if cell.terrain in SOLID:
                    oxygen[i] = 0.0
                    fire[i] = 0.0
                    continue
                if cell.terrain == Terrain.AIRLOCK:
                    oxygen[i] = 0.0
                    heat[i] += (ambient - cell.heat) * 0.025
                    fire[i] = 0.0
                    continue
                # Process each edge once; apply equal and opposite transfers.
                for nx, ny in ((x + 1, y), (x, y + 1)):
                    if not self.inside(nx, ny):
                        continue
                    j = self.index(nx, ny)
                    other = self.cells[j]
                    if other.terrain in SOLID or other.terrain == Terrain.AIRLOCK:
                        continue
                    transfer = (cell.oxygen - other.oxygen) * 0.12
                    oxygen[i] -= transfer
                    oxygen[j] += transfer
                    warmth = (cell.heat - other.heat) * 0.05
                    heat[i] -= warmth
                    heat[j] += warmth
                    if cell.fire > 0.25 and other.terrain == Terrain.FLOOR and other.oxygen > 0.18:
                        if rng.random() < cell.fire * 0.018:
                            fire[j] = max(fire[j], 0.35)
                    if other.fire > 0.25 and cell.terrain == Terrain.FLOOR and cell.oxygen > 0.18:
                        if rng.random() < other.fire * 0.018:
                            fire[i] = max(fire[i], 0.35)
                if cell.terrain == Terrain.VACUUM:
                    oxygen[i] *= 0.35
                    heat[i] += (ambient - cell.heat) * 0.12
                else:
                    heat[i] += (17.0 - cell.heat) * 0.002
                if cell.fire:
                    oxygen[i] -= cell.fire * 0.06
                    heat[i] += cell.fire * 5.0
                    fire[i] = min(1.0, cell.fire + 0.035) if cell.oxygen > 0.14 else max(0.0, cell.fire - 0.16)
        for i, cell in enumerate(self.cells):
            cell.oxygen = max(0.0, min(1.0, oxygen[i]))
            cell.heat = max(-100.0, min(180.0, heat[i]))
            cell.fire = max(0.0, min(1.0, fire[i]))
