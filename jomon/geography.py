"""Seeded, aligned regional layouts and traversable side routes."""

from __future__ import annotations

import hashlib
import heapq

from .catalog import CatalogError, load_catalog
from .state import Position, Region, stage_rng


LAYOUTS = ("original", "east", "south", "east-south")
_CATALOG = load_catalog("geography.json", ("SIDE_ROUTES", "GROUND_PATCHES", "FIELD_SECRETS"))
SIDE_ROUTES = _CATALOG["SIDE_ROUTES"]
GROUND_PATCHES = _CATALOG["GROUND_PATCHES"]
FIELD_SECRETS = _CATALOG["FIELD_SECRETS"]
if not all(isinstance(section, dict) and set(section) == set(SIDE_ROUTES) for section in (SIDE_ROUTES, GROUND_PATCHES, FIELD_SECRETS)):
    raise CatalogError("geography sections must cover the same regions")
for region_id in SIDE_ROUTES:
    routes, patches, secrets = SIDE_ROUTES[region_id], GROUND_PATCHES[region_id], FIELD_SECRETS[region_id]
    if (not isinstance(routes, list) or len(routes) != 2
            or any(not isinstance(row, list) or len(row) != 2 or any(not isinstance(name, str) for name in row) for row in routes)):
        raise CatalogError(f"{region_id} has invalid side routes")
    if not isinstance(patches, list) or len(patches) != 2 or any(not isinstance(glyph, str) or len(glyph) != 1 for glyph in patches):
        raise CatalogError(f"{region_id} has invalid terrain patches")
    if (not isinstance(secrets, list) or len(secrets) != 2
            or any(not isinstance(row, list) or len(row) != 6 or any(not isinstance(value, str) or not value for value in row)
                   or row[4] not in {"light", "key", "rope"} for row in secrets)):
        raise CatalogError(f"{region_id} has invalid field secrets")


def layout_for(seed: str, region_id: str) -> str:
    return LAYOUTS[stage_rng(seed, f"{region_id}:macro-layout").randrange(len(LAYOUTS))]


def layout_point(region: Region, point: Position) -> Position:
    layout = region.changes.get("macro_layout", "original")
    return _point(point, region.width, region.height, str(layout))


def _point(point: Position, width: int, height: int, layout: str) -> Position:
    return Position(
        width - 1 - point.x if "east" in layout else point.x,
        height - 1 - point.y if "south" in layout else point.y,
        point.z,
    )


def _path(grid: list[list[str]], start: Position, end: Position) -> list[tuple[int, int]]:
    width, height = len(grid[0]), len(grid)
    source, target = (start.x, start.y), (end.x, end.y)
    queue = [(0, source)]
    best = {source: 0}
    previous: dict[tuple[int, int], tuple[int, int]] = {}
    while queue:
        cost, current = heapq.heappop(queue)
        if cost != best[current]:
            continue
        if current == target:
            path = [current]
            while current != source:
                current = previous[current]
                path.append(current)
            return path[::-1]
        x, y = current
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if not (1 <= nx < width - 1 and 1 <= ny < height - 1):
                continue
            tile = grid[ny][nx]
            if tile in {"#", "T", " "}:
                continue
            step = 5 if tile == "=" else 3 if tile == "~" else 1
            candidate = cost + step
            if candidate < best.get((nx, ny), 10**9):
                best[(nx, ny)] = candidate
                previous[(nx, ny)] = current
                heapq.heappush(queue, (candidate, (nx, ny)))
    return []


def _shape_ground(
    levels: dict[str, list[str]], landmarks: dict[str, Position],
    seed: str, region_id: str,
) -> None:
    grid = [list(row) for row in levels["0"]]
    width, height = len(grid[0]), len(grid)
    rng = stage_rng(seed, f"{region_id}:landforms")
    protected = {(point.x, point.y) for point in landmarks.values() if point.z == 0}
    anchors = [point for point in landmarks.values() if point.z == 0]
    x1, x2 = max(3, min(p.x for p in anchors) - 5), min(width - 4, max(p.x for p in anchors) + 5)
    y1, y2 = max(3, min(p.y for p in anchors) - 5), min(height - 4, max(p.y for p in anchors) + 5)
    for patch in range(5):
        cx, cy = rng.randint(x1, x2), rng.randint(y1, y2)
        radius = rng.randint(2, 5)
        glyph = GROUND_PATCHES[region_id][patch % 2]
        for y in range(max(1, cy - radius), min(height - 1, cy + radius + 1)):
            for x in range(max(1, cx - radius), min(width - 1, cx + radius + 1)):
                if grid[y][x] == "." and (x, y) not in protected and (x - cx) ** 2 + (y - cy) ** 2 <= radius * radius and rng.randrange(5):
                    grid[y][x] = glyph

    routes = [("landing", "contact"), SIDE_ROUTES[region_id][rng.randrange(2)]]
    for start_key, end_key in routes:
        start, end = landmarks[start_key], landmarks[end_key]
        if start.z or end.z:
            continue
        mid_x = (start.x + end.x) // 2
        mid_y = (start.y + end.y) // 2 + rng.choice((-9, -7, 7, 9))
        mid = Position(max(2, min(width - 3, mid_x)), max(2, min(height - 3, mid_y)))
        if grid[mid.y][mid.x] in {"#", "T", " "}:
            candidates = ((x, y) for y in range(2, height - 2) for x in range(2, width - 2) if grid[y][x] not in {"#", "T", " "})
            x, y = min(candidates, key=lambda cell: (abs(cell[0] - mid.x) + abs(cell[1] - mid.y), cell[1], cell[0]))
            mid = Position(x, y)
        first, second = _path(grid, start, mid), _path(grid, mid, end)
        if not first or not second:
            continue
        for x, y in first + second:
            if grid[y][x] in {".", "m", "t", "r", "q", ";", ":", ",", "w", "~"} and (x, y) not in protected:
                grid[y][x] = "="
    levels["0"] = ["".join(row) for row in grid]


def orient_spatial(spatial: dict[str, object], seed: str, region_id: str, *, layout: str | None = None) -> None:
    chosen = layout or layout_for(seed, region_id)
    if chosen not in LAYOUTS:
        raise ValueError(f"unknown macro layout: {chosen}")
    levels = spatial["levels"]
    landmarks = spatial["landmarks"]
    _shape_ground(levels, landmarks, seed, region_id)
    width, height = spatial["width"], spatial["height"]
    if chosen != "original":
        spatial["levels"] = {
            z: [row[::-1] if "east" in chosen else row for row in (rows[::-1] if "south" in chosen else rows)]
            for z, rows in levels.items()
        }
        spatial["landmarks"] = {key: _point(point, width, height, chosen) for key, point in landmarks.items()}
        spatial["zones"] = {
            key: (
                width - 1 - x2 if "east" in chosen else x1,
                height - 1 - y2 if "south" in chosen else y1,
                width - 1 - x1 if "east" in chosen else x2,
                height - 1 - y1 if "south" in chosen else y2,
            ) for key, (x1, y1, x2, y2) in spatial["zones"].items()
        }
        for link in spatial["vertical_links"]:
            link.first = _point(link.first, width, height, chosen)
            link.second = _point(link.second, width, height, chosen)
        for container in spatial["containers"]:
            container.position = _point(container.position, width, height, chosen)
        if "materials" in spatial:
            spatial["materials"] = {
                _key(_point(_parse_key(key), width, height, chosen)): value
                for key, value in spatial["materials"].items()
            }
        if "history_scar" in spatial["changes"]:
            spatial["changes"]["history_scar"] = _key(_point(_parse_key(spatial["changes"]["history_scar"]), width, height, chosen))
    spatial["changes"]["macro_layout"] = chosen
    spatial["geography_signature"] = hashlib.sha256(
        ("|".join(spatial["levels"]["0"]) + repr(sorted((key, point.x, point.y, point.z) for key, point in spatial["landmarks"].items()))).encode()
    ).hexdigest()[:16]


def _parse_key(key: str) -> Position:
    return Position(*map(int, key.split(",")))


def _key(point: Position) -> str:
    return f"{point.x},{point.y},{point.z}"


def orient_region(region: Region, seed: str, *, layout: str | None = None) -> None:
    spatial = {key: getattr(region, key) for key in (
        "width", "height", "levels", "landmarks", "zones", "vertical_links",
        "containers", "changes", "materials", "geography_signature",
    )}
    orient_spatial(spatial, seed, region.id, layout=layout)
    for key in ("levels", "landmarks", "zones", "materials", "geography_signature"):
        setattr(region, key, spatial[key])
