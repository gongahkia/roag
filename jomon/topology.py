"""One bounded deterministic generator for seamless Hearthford."""

from __future__ import annotations

import hashlib
from collections import deque

from .content import TREASURE_REWARDS
from .state import Container, Position, VerticalLink, stage_rng

WIDTH = 96
HEIGHT = 54
LEVELS = (-1, 0, 1, 2)


def _grid(fill: str = " ") -> list[list[str]]:
    return [[fill for _ in range(WIDTH)] for _ in range(HEIGHT)]


def _value_field(seed: str, stage: str, cell: int = 8) -> list[list[float]]:
    """Small smoothed lattice used only for Hearthford wetness and vegetation."""
    rng = stage_rng(seed, stage)
    cols, rows = WIDTH // cell + 2, HEIGHT // cell + 2
    values = [[rng.random() for _ in range(cols)] for _ in range(rows)]
    field: list[list[float]] = []
    for y in range(HEIGHT):
        gy, fy = divmod(y, cell)
        fy /= cell
        row: list[float] = []
        for x in range(WIDTH):
            gx, fx = divmod(x, cell)
            fx /= cell
            top = values[gy][gx] * (1 - fx) + values[gy][gx + 1] * fx
            bottom = values[gy + 1][gx] * (1 - fx) + values[gy + 1][gx + 1] * fx
            row.append(top * (1 - fy) + bottom * fy)
        field.append(row)
    return field


def _rect(grid: list[list[str]], x1: int, y1: int, x2: int, y2: int, floor: str = ".") -> None:
    for y in range(y1, y2 + 1):
        for x in range(x1, x2 + 1):
            grid[y][x] = "#" if x in {x1, x2} or y in {y1, y2} else floor


def _road(grid: list[list[str]], start: Position, end: Position, seed: str, stage: str) -> None:
    rng = stage_rng(seed, stage)
    x, y = start.x, start.y
    while (x, y) != (end.x, end.y):
        grid[y][x] = "="
        horizontal = x != end.x
        vertical = y != end.y
        if horizontal and vertical:
            if rng.randrange(2):
                x += 1 if end.x > x else -1
            else:
                y += 1 if end.y > y else -1
        elif horizontal:
            x += 1 if end.x > x else -1
        else:
            y += 1 if end.y > y else -1
    grid[y][x] = "="


def _carve_river(grid: list[list[str]], seed: str) -> list[int]:
    rng = stage_rng(seed, "river-curve")
    x = 7 + rng.randrange(5)
    centres: list[int] = []
    for y in range(HEIGHT):
        if y and y % 4 == 0:
            x = max(4, min(15, x + rng.choice((-1, 0, 1))))
        centres.append(x)
        for rx in range(max(1, x - 2), min(WIDTH - 1, x + 3)):
            grid[y][rx] = "~"
    return centres


def _ground(seed: str) -> tuple[list[list[str]], dict[str, Position], dict[str, tuple[int, int, int, int]]]:
    wet = _value_field(seed, "wetness")
    growth = _value_field(seed, "vegetation")
    grid = _grid(".")
    for y in range(1, HEIGHT - 1):
        for x in range(1, WIDTH - 1):
            if wet[y][x] > 0.72:
                grid[y][x] = "m"
            elif growth[y][x] > 0.68:
                grid[y][x] = "T"
    for x in range(WIDTH):
        grid[0][x] = grid[-1][x] = "T"
    for y in range(HEIGHT):
        grid[y][0] = grid[y][-1] = "T"
    river = _carve_river(grid, seed)
    landing = Position(river[10] + 3, 10)
    settlement = Position(24 + stage_rng(seed, "settlement-x").randrange(3), 12)
    mill = Position(78, 24)
    watch = Position(47, 12)
    cave = Position(58, 42)

    # Quay, settlement buildings, and doors share outdoor coordinates.
    for x in range(river[10] + 1, settlement.x + 1):
        grid[10][x] = "="
    grid[landing.y][landing.x] = "+"
    _rect(grid, settlement.x - 7, 6, settlement.x + 7, 18)
    grid[10][settlement.x - 7] = "+"
    for x in range(landing.x, settlement.x - 7):
        grid[10][x] = "="
    grid[landing.y][landing.x] = "+"
    grid[18][settlement.x] = "+"
    _rect(grid, settlement.x + 10, 8, settlement.x + 18, 15)
    grid[15][settlement.x + 14] = "+"
    grid[settlement.y][settlement.x] = "M"
    grid[settlement.y + 2][settlement.x - 3] = "c"
    grid[settlement.y - 2][settlement.x + 3] = "c"

    _road(grid, Position(settlement.x, 19), Position(42, 28), seed, "west-road")
    _road(grid, Position(42, 28), Position(68, 24), seed, "mill-road")
    _road(grid, Position(42, 28), Position(cave.x, cave.y), seed, "reed-path")
    _road(grid, Position(42, 28), Position(watch.x, watch.y + 4), seed, "watch-path")

    # Reedwood paths form a traversable loop through otherwise dense growth.
    for x in range(36, 64):
        for y in range(30, 49):
            if growth[y][x] > 0.43:
                grid[y][x] = "T"
    _road(grid, Position(42, 28), Position(38, 43), seed, "reed-west")
    _road(grid, Position(38, 43), cave, seed, "reed-south")
    _road(grid, cave, Position(61, 29), seed, "reed-east")
    _road(grid, Position(61, 29), Position(42, 28), seed, "reed-north")
    grid[38][51] = "m"
    grid[38][52] = "m"
    grid[38][53] = "m"

    _rect(grid, 44, 8, 50, 16)
    grid[16][47] = "+"
    grid[13][47] = ">"
    _rect(grid, 46, 34, 53, 40)
    grid[40][49] = "+"
    grid[37][49] = "C"
    grid[38][54] = "m"
    grid[39][54] = "m"
    grid[39][55] = "m"
    grid[cave.y][cave.x] = "<"

    # Mill ground floor and yard.
    _rect(grid, 68, 14, 90, 34)
    grid[24][68] = "+"
    grid[26][72] = ">"
    grid[27][82] = "&"
    grid[30][86] = "f"
    grid[22][78] = "O"
    grid[31][72] = "C"
    for x in range(70, 89):
        grid[35][x] = "="

    # Tempting optional sites away from the critical road.
    grid[45][38] = "C"
    grid[38][55] = "C"
    grid[25][60] = "C"

    landmarks = {
        "landing": landing,
        "contact": settlement,
        "settlement": settlement,
        "watchtower": watch,
        "mill": mill,
        "cave_entrance": cave,
        "objective": Position(84, 20, 1),
    }
    zones = {
        "Hearthford settlement": (16, 5, 43, 20),
        "Reedwood floodplain": (32, 27, 65, 51),
        "Old watch": (43, 7, 52, 18),
        "Hearthford millworks": (66, 12, 92, 36),
        "River road": (12, 19, 65, 29),
    }
    return grid, landmarks, zones


def _upper_levels() -> dict[int, list[list[str]]]:
    cellar, upper, roof = _grid(), _grid(), _grid()

    # Underground cave and culvert: irregular but constrained around a fixed route.
    for y in range(36, 49):
        for x in range(52, 73):
            if ((x - 62) ** 2) / 120 + ((y - 42) ** 2) / 34 < 1:
                cellar[y][x] = "."
    for x in range(58, 83):
        cellar[42][x] = ","
        cellar[41][x] = "."
    cellar[42][58] = ">"
    cellar[42][70] = "C"
    cellar[41][78] = "C"
    cellar[42][82] = "&"

    _rect(upper, 69, 15, 89, 33)
    upper[26][72] = "<"
    upper[22][78] = "d"
    upper[20][84] = "R"
    upper[18][86] = "C"
    upper[16][84] = ">"
    upper[30][86] = "O"
    for x in range(73, 88):
        upper[24][x] = "="

    _rect(upper, 45, 9, 49, 15)
    upper[13][47] = "<"
    upper[10][47] = ">"
    upper[11][48] = "C"

    _rect(roof, 70, 14, 89, 25, floor="^")
    roof[16][84] = "<"
    roof[19][78] = "O"
    roof[16][87] = "C"
    _rect(roof, 44, 8, 50, 13, floor="^")
    roof[10][47] = "<"
    roof[9][49] = "C"
    return {-1: cellar, 1: upper, 2: roof}


def _links() -> list[VerticalLink]:
    return [
        VerticalLink(Position(58, 42, 0), Position(58, 42, -1), "culvert steps"),
        VerticalLink(Position(72, 26, 0), Position(72, 26, 1), "mill ladder"),
        VerticalLink(Position(84, 16, 1), Position(84, 16, 2), "roof ladder"),
        VerticalLink(Position(47, 13, 0), Position(47, 13, 1), "watch ladder"),
        VerticalLink(Position(47, 10, 1), Position(47, 10, 2), "watch roof ladder"),
    ]


def _containers(seed: str) -> list[Container]:
    positions = (
        ("ruin", "Collapsed cottage coffer", Position(49, 37), None),
        ("reed", "Flood-islet cache", Position(55, 38), "rope"),
        ("road", "Abandoned carrier chest", Position(60, 25), None),
        ("cave", "Culvert mason's box", Position(70, 42, -1), "light"),
        ("cellar", "Buried mill strongbox", Position(78, 41, -1), "key"),
        ("gantry", "Gantry tool chest", Position(86, 18, 1), None),
        ("watch", "Watch-roof coffer", Position(49, 9, 2), None),
        ("roof", "Mill roof cache", Position(87, 16, 2), "rope"),
    )
    rewards = list(TREASURE_REWARDS)
    stage_rng(seed, "treasure-rewards").shuffle(rewards)
    return [Container(key, name, position, rewards[index], requirement) for index, (key, name, position, requirement) in enumerate(positions)]


def _reachable(levels: dict[str, list[str]], links: list[VerticalLink], start: Position) -> set[Position]:
    def tile(position: Position) -> str:
        return levels[str(position.z)][position.y][position.x]

    link_map: dict[Position, Position] = {}
    for link in links:
        link_map[link.first] = link.second
        link_map[link.second] = link.first
    queue, seen = deque([start]), {start}
    while queue:
        current = queue.popleft()
        neighbours = [
            Position(current.x + 1, current.y, current.z), Position(current.x - 1, current.y, current.z),
            Position(current.x, current.y + 1, current.z), Position(current.x, current.y - 1, current.z),
        ]
        if current in link_map:
            neighbours.append(link_map[current])
        for candidate in neighbours:
            if not (0 <= candidate.x < WIDTH and 0 <= candidate.y < HEIGHT) or candidate in seen:
                continue
            if tile(candidate) in {" ", "#", "~", "T"}:
                continue
            seen.add(candidate)
            queue.append(candidate)
    return seen


def build_region(seed: str) -> dict[str, object]:
    ground, landmarks, zones = _ground(seed)
    other = _upper_levels()
    levels = {str(level): ["".join(row) for row in (ground if level == 0 else other[level])] for level in LEVELS}
    links = _links()
    seen = _reachable(levels, links, landmarks["landing"])
    required = {landmarks["contact"], landmarks["objective"], landmarks["cave_entrance"], Position(84, 16, 2)}
    if not required <= seen:
        raise RuntimeError("Hearthford generation failed required reachability")
    digest = hashlib.sha256(("|".join(levels["0"]) + repr(sorted((p.x, p.y, p.z) for p in required))).encode()).hexdigest()[:16]
    return {
        "width": WIDTH,
        "height": HEIGHT,
        "levels": levels,
        "landmarks": landmarks,
        "zones": zones,
        "vertical_links": links,
        "containers": _containers(seed),
        "changes": {},
        "tile_changes": {},
        "seen": [],
        "geography_signature": digest,
    }
