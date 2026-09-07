"""Three dedicated deterministic regional generators beyond Hearthford."""

from __future__ import annotations

from collections import deque
import hashlib

from .content import COMMODITIES, ENEMY_ARCHETYPES
from .encounters import production_encounter_groups, threat_from_archetype
from .state import Contact, Container, MarketEntry, Position, Region, Threat, VerticalLink, stage_rng


def _grid(width: int, height: int, fill: str = " ") -> list[list[str]]:
    return [[fill for _ in range(width)] for _ in range(height)]


def _border(grid: list[list[str]], glyph: str) -> None:
    width, height = len(grid[0]), len(grid)
    for x in range(width):
        grid[0][x] = grid[-1][x] = glyph
    for y in range(height):
        grid[y][0] = grid[y][-1] = glyph


def _rect(grid: list[list[str]], x1: int, y1: int, x2: int, y2: int, floor: str = ".") -> None:
    for y in range(y1, y2 + 1):
        for x in range(x1, x2 + 1):
            grid[y][x] = "#" if x in {x1, x2} or y in {y1, y2} else floor


def _road(grid: list[list[str]], points: list[Position], seed: str, stage: str) -> None:
    rng = stage_rng(seed, stage)
    for start, end in zip(points, points[1:]):
        x, y = start.x, start.y
        while (x, y) != (end.x, end.y):
            grid[y][x] = "="
            if x != end.x and y != end.y:
                if rng.randrange(2):
                    x += 1 if end.x > x else -1
                else:
                    y += 1 if end.y > y else -1
            elif x != end.x:
                x += 1 if end.x > x else -1
            else:
                y += 1 if end.y > y else -1
        grid[y][x] = "="


def _carve(grid: list[list[str]], points: list[Position], glyph: str = "=") -> None:
    """Carve one explicit Manhattan safety line between authored anchors."""
    for start, end in zip(points, points[1:]):
        x, y = start.x, start.y
        while x != end.x:
            grid[y][x] = glyph
            x += 1 if end.x > x else -1
        while y != end.y:
            grid[y][x] = glyph
            y += 1 if end.y > y else -1
        grid[y][x] = glyph


def _levels(ground: list[list[str]], others: dict[int, list[list[str]]]) -> dict[str, list[str]]:
    return {
        str(level): ["".join(row) for row in (ground if level == 0 else others[level])]
        for level in (-1, 0, 1, 2)
    }


def _containers(seed: str, region_id: str, specifications: list[tuple[str, str, Position, str | None]], rewards: list[str]) -> list[Container]:
    values = list(rewards)
    stage_rng(seed, f"{region_id}:treasure").shuffle(values)
    return [
        Container(f"{region_id}-{key}", name, position, values[index], requirement)
        for index, (key, name, position, requirement) in enumerate(specifications)
    ]


def _stock_containers(containers: list[Container], armour: list[str], supplies: list[str]) -> None:
    for index, container in enumerate(containers):
        container.extra_rewards = [armour[index], supplies[index]]


def region_reachable(region: Region, start: Position | None = None) -> set[Position]:
    start = start or region.landmarks["landing"]
    link_map: dict[Position, Position] = {}
    for link in region.vertical_links:
        link_map[link.first] = link.second
        link_map[link.second] = link.first

    def tile(position: Position) -> str:
        rows = region.levels.get(str(position.z), [])
        if not (0 <= position.y < len(rows) and 0 <= position.x < len(rows[position.y])):
            return " "
        return region.tile_changes.get(f"{position.x},{position.y},{position.z}", rows[position.y][position.x])

    queue, seen = deque([start]), {start}
    while queue:
        current = queue.popleft()
        candidates = [
            Position(current.x + 1, current.y, current.z), Position(current.x - 1, current.y, current.z),
            Position(current.x, current.y + 1, current.z), Position(current.x, current.y - 1, current.z),
        ]
        if current in link_map:
            candidates.append(link_map[current])
        for candidate in candidates:
            if candidate not in seen and tile(candidate) not in {" ", "#", "~", "T"}:
                seen.add(candidate)
                queue.append(candidate)
    return seen


def validate_region(region: Region) -> None:
    if set(region.levels) != {"-1", "0", "1", "2"}:
        raise RuntimeError(f"{region.name} lacks aligned levels")
    reachable = region_reachable(region)
    required_keys = ("landing", "contact", "objective", "cave_entrance")
    required = {region.landmarks[key] for key in required_keys}
    elevated = region.landmarks.get("elevated") or region.landmarks.get("high_view")
    if elevated:
        required.add(elevated)
    required.update(container.position for container in region.containers)
    if not required <= reachable:
        missing = required - reachable
        raise RuntimeError(f"{region.name} generation left required positions unreachable: {missing}")
    for link in region.vertical_links:
        if link.first not in reachable or link.second not in reachable:
            raise RuntimeError(f"{region.name} has an unreachable vertical link")


def _signature(levels: dict[str, list[str]], landmarks: dict[str, Position]) -> str:
    material = "|".join(levels["0"]) + repr(sorted((key, value.x, value.y, value.z) for key, value in landmarks.items()))
    return hashlib.sha256(material.encode()).hexdigest()[:16]


def build_greywash(seed: str) -> Region:
    """Constrained shoreline carving with three tide-parallel route bands."""
    width, height = 104, 56
    ground = _grid(width, height, ".")
    _border(ground, "#")
    rng = stage_rng(seed, "greywash:shore")
    shore = 42 + rng.randrange(-2, 3)
    for x in range(1, width - 1):
        if x % 9 == 0:
            shore = max(39, min(45, shore + rng.choice((-1, 0, 1))))
        for y in range(shore, height - 1):
            ground[y][x] = ":" if y < shore + 3 else "~"
    channel_x = 66 + rng.randrange(-3, 4)
    for y in range(34, height - 4):
        ground[y][channel_x + ((y // 4) % 2)] = "w"
    for x in range(48, 84):
        dune_y = 12 + ((x + rng.randrange(3)) % 7)
        ground[dune_y][x] = "r"
        if x % 3:
            ground[dune_y + 1][x] = "r"

    landing, settlement = Position(4, 27), Position(20, 26)
    saltworks, dunes = Position(38, 34), Position(59, 17)
    wreck, chain = Position(76, 40), Position(90, 28)
    cave = Position(83, 43)
    _road(ground, [landing, settlement, saltworks, chain], seed, "greywash:work-road")
    _road(ground, [settlement, dunes, chain], seed, "greywash:dune-road")
    _road(ground, [saltworks, wreck, chain], seed, "greywash:shore-road")
    _rect(ground, 12, 21, 29, 31)
    ground[26][12] = "+"
    ground[26][20] = "M"
    ground[29][26] = "c"
    _rect(ground, 32, 30, 44, 38)
    ground[34][32] = "+"
    ground[34][38] = "&"
    _rect(ground, 55, 12, 63, 21)
    ground[17][55] = "+"
    ground[17][59] = ">"
    _rect(ground, 85, 21, 96, 34)
    ground[28][85] = "+"
    ground[28][90] = ">"
    ground[39][76] = "O"
    ground[cave.y][cave.x] = "<"
    ground[landing.y][landing.x] = "+"
    # The cave has a guaranteed raised wreck-timber approach even at high shore seeds.
    _carve(ground, [Position(76, 39), Position(83, 39), cave])
    ground[39][76] = "O"
    ground[cave.y][cave.x] = "<"

    below, upper, roof = _grid(width, height), _grid(width, height), _grid(width, height)
    for y in range(39, 51):
        for x in range(72, 95):
            if ((x - 83) ** 2) / 140 + ((y - 44) ** 2) / 28 < 1:
                below[y][x] = ","
    below[43][83] = ">"
    below[44][91] = "&"
    _carve(below, [Position(83, 43, -1), Position(90, 44, -1)], ",")
    _rect(upper, 56, 13, 62, 20)
    upper[17][59] = "<"
    upper[14][59] = ">"
    for x in range(62, 90):
        upper[17][x] = "="
    _rect(upper, 86, 22, 94, 32)
    upper[28][90] = "<"
    upper[24][90] = "R"
    upper[23][92] = "C"
    _rect(roof, 55, 12, 63, 17, "^")
    roof[14][59] = "<"
    roof[13][62] = "C"
    _rect(roof, 86, 21, 95, 27, "^")
    roof[23][90] = "<"
    roof[22][94] = "C"

    links = [
        VerticalLink(cave, Position(cave.x, cave.y, -1), "sea-cave steps"),
        VerticalLink(Position(59, 17), Position(59, 17, 1), "signal-mast ladder"),
        VerticalLink(Position(59, 14, 1), Position(59, 14, 2), "mast roof ladder"),
        VerticalLink(Position(90, 28), Position(90, 28, 1), "chain-house stair"),
        VerticalLink(Position(90, 23, 1), Position(90, 23, 2), "chain roof hatch"),
    ]
    containers = _containers(seed, "greywash", [
        ("quay", "Quayside salt coffer", Position(27, 29), None),
        ("pan", "Salt-pan tool chest", Position(42, 36), None),
        ("wreck", "Distinctive wreck locker", Position(75, 39), "rope"),
        ("cave", "Sea-cave smuggler cache", Position(90, 44, -1), "light"),
        ("mast", "Signal-mast chest", Position(62, 13, 2), None),
        ("chain", "Chain-house strongbox", Position(92, 23, 1), "key"),
    ], ["tide ledger", "cork float", "wreck key", "brine wash", "longbow", "ebbglass spindle"])
    _stock_containers(
        containers,
        ["boiled cap", "reedscale vest", "linen sleeves", "tarred gauntlets", "leather leggings", "marsh waders"],
        ["tide pin", "brine wash", "salt-house chit", "casting net bundle", "fletched arrows", "willow dressing"],
    )
    for container in containers:
        target = ground if container.position.z == 0 else {-1: below, 1: upper, 2: roof}[container.position.z]
        target[container.position.y][container.position.x] = "C"
    levels = _levels(ground, {-1: below, 1: upper, 2: roof})
    landmarks = {
        "landing": landing, "contact": settlement, "second_contact": Position(26, 29),
        "settlement": settlement, "saltworks": saltworks, "dunes": dunes,
        "wreck": wreck, "chain_house": chain, "cave_entrance": cave,
        "objective": Position(90, 24, 1), "elevated": Position(59, 14, 2),
    }
    region = Region(
        "Wind and tide expose a wreck road only while the flats drain.",
        "Greywash salts fish, tends broad pans, and recovers accountable wreck cargo.",
        "A broken tide chain will drown the salt road before the next working ebb.",
        "Reach the upper chain house and secure two iron links or alter the sluice schedule.",
        "ironwork", "salt fish", "closing tide", width, height, levels, landmarks,
        {"Greywash village": (10, 19, 31, 33), "Salt pans": (30, 28, 47, 40), "Dune road": (47, 9, 70, 23), "Wreck flats": (63, 34, 84, 45), "Tide-chain house": (83, 19, 98, 35)},
        links, containers, {}, {}, [], _signature(levels, landmarks),
        id="greywash", name="Greywash Tidal Reach", process_name="working tide",
        process_thresholds=[28, 55, 82],
    )
    validate_region(region)
    return region


def build_greenwold(seed: str) -> Region:
    """Smoothed tree clusters leave open woodland and interconnected clearings."""
    width, height = 100, 58
    rng = stage_rng(seed, "greenwold:canopy")
    growth = [[rng.random() < 0.34 for _ in range(width)] for _ in range(height)]
    for _ in range(2):
        growth = [[
            sum(growth[ny][nx] for ny in range(max(0, y - 1), min(height, y + 2)) for nx in range(max(0, x - 1), min(width, x + 2))) >= 5
            for x in range(width)] for y in range(height)
        ]
    ground = [["T" if growth[y][x] else "." for x in range(width)] for y in range(height)]
    _border(ground, "T")
    creek_y = 31 + stage_rng(seed, "greenwold:creek").randrange(-2, 3)
    for x in range(1, width - 1):
        y = creek_y + ((x // 11) % 3) - 1
        ground[y][x] = "~"
    landing, village = Position(4, 27), Position(20, 25)
    clearing, resin, burn = Position(48, 27), Position(75, 16), Position(80, 39)
    root, watch = Position(55, 47), Position(64, 10)
    _road(ground, [landing, village, clearing, resin], seed, "greenwold:north-trail")
    _road(ground, [village, Position(34, 42), root, burn, clearing], seed, "greenwold:south-loop")
    _road(ground, [clearing, watch, resin], seed, "greenwold:high-trail")
    for cx, cy, radius in ((20, 25, 8), (48, 27, 9), (75, 16, 7), (80, 39, 8), (55, 47, 6)):
        for y in range(cy - radius, cy + radius + 1):
            for x in range(cx - radius, cx + radius + 1):
                if 0 < x < width - 1 and 0 < y < height - 1 and (x - cx) ** 2 + (y - cy) ** 2 < radius * radius:
                    if ground[y][x] == "T" and rng.random() < 0.72:
                        ground[y][x] = "."
    _rect(ground, 13, 20, 27, 30)
    ground[25][13] = "+"
    ground[25][20] = "M"
    ground[27][24] = "c"
    _rect(ground, 70, 11, 81, 21)
    ground[16][70] = "+"
    ground[16][75] = "&"
    _rect(ground, 76, 35, 87, 44)
    ground[39][76] = "+"
    ground[40][82] = "f"
    _rect(ground, 60, 6, 68, 14)
    ground[10][60] = "+"
    ground[10][64] = ">"
    # Seeded canopy can close every incidental gap around a template. Recut
    # the three authored routes after structures are stamped, then restore
    # their interaction glyphs. These are narrow forest trails, not a generic
    # connectivity pass.
    _carve(ground, [landing, village, clearing, resin])
    _carve(ground, [village, Position(34, 42), root, burn, clearing])
    _carve(ground, [clearing, Position(60, 10), watch])
    ground[25][13] = "+"
    ground[25][20] = "M"
    ground[27][24] = "c"
    ground[16][70] = "+"
    ground[16][75] = "&"
    ground[39][76] = "+"
    ground[40][82] = "f"
    ground[10][60] = "+"
    ground[10][64] = ">"
    ground[root.y][root.x] = "<"
    ground[landing.y][landing.x] = "+"

    below, upper, roof = _grid(width, height), _grid(width, height), _grid(width, height)
    for y in range(42, 53):
        for x in range(47, 66):
            if abs(x - 56) + abs(y - 47) < 13:
                below[y][x] = "." if (x + y) % 5 else ","
    below[47][55] = ">"
    below[48][63] = "R"
    _rect(upper, 61, 7, 67, 13)
    upper[10][64] = "<"
    upper[8][64] = ">"
    for x in range(64, 83):
        upper[12 + (x % 2)][x] = "="
    _rect(upper, 77, 36, 86, 43)
    upper[39][80] = "<"
    upper[38][84] = "C"
    _rect(roof, 60, 6, 68, 10, "^")
    roof[8][64] = "<"
    roof[7][67] = "C"
    _rect(roof, 77, 35, 87, 39, "^")
    roof[37][82] = "<"
    roof[36][86] = "C"
    # Keep the two canopy caches connected to their ladder mouths regardless of
    # the surrounding seeded canopy.
    _carve(upper, [Position(64, 10, 1), Position(64, 8, 1)], "^")
    upper[10][64] = "<"
    upper[8][64] = ">"
    _carve(roof, [Position(64, 8, 2), Position(67, 7, 2)], "^")
    roof[8][64] = "<"
    roof[7][67] = "C"
    links = [
        VerticalLink(root, Position(root.x, root.y, -1), "root-cellar steps"),
        VerticalLink(Position(64, 10), Position(64, 10, 1), "watch-tree ladder"),
        VerticalLink(Position(64, 8, 1), Position(64, 8, 2), "canopy ladder"),
        VerticalLink(Position(80, 39), Position(80, 39, 1), "burn-walk ladder"),
        VerticalLink(Position(82, 37, 1), Position(82, 37, 2), "smoke roof ladder"),
    ]
    containers = _containers(seed, "greenwold", [
        ("village", "Greenwold medicine chest", Position(26, 28), None),
        ("clearing", "Abandoned hunter pack", Position(51, 24), None),
        ("root", "Root-cellar locked box", Position(62, 48, -1), "key"),
        ("resin", "Resin-yard tool cabinet", Position(79, 19), None),
        ("watch", "Canopy cache", Position(67, 7, 2), "rope"),
        ("burn", "Raised burn-store coffer", Position(84, 38, 1), None),
    ], ["charcoal mask", "resin grip", "bird whistle", "pine resin dressing", "paired knives", "coalheart seed"])
    _stock_containers(
        containers,
        ["felt hood", "quilted jack", "leather vambraces", "work gloves", "wool chausses", "reed shoes"],
        ["charcoal key", "pine resin dressing", "dry smoke charge", "splint roll", "dry lamp wick", "fletched arrows"],
    )
    for container in containers:
        target = ground if container.position.z == 0 else {-1: below, 1: upper, 2: roof}[container.position.z]
        target[container.position.y][container.position.x] = "C"
    levels = _levels(ground, {-1: below, 1: upper, 2: roof})
    landmarks = {
        "landing": landing, "contact": village, "second_contact": Position(24, 27),
        "settlement": village, "clearing": clearing, "resin_yard": resin,
        "burn_walk": burn, "root_cellar": root, "cave_entrance": root,
        "watch_tree": watch, "objective": Position(63, 48, -1),
        "elevated": Position(64, 8, 2),
    }
    region = Region(
        "A shifting forest wind carries an illicit charcoal burn toward medicine coppice.",
        "Greenwold cuts managed timber, burns charcoal, gathers resin, and tends healing plants.",
        "The burn crew moved the water key into a root store while smoke thickens.",
        "Recover the water key below, rescue the wounded reeve, or redirect the burn shutters.",
        "charcoal", "timber", "spreading burn smoke", width, height, levels, landmarks,
        {"Greenwold clearing village": (10, 18, 30, 32), "Open woodland": (30, 16, 64, 43), "Resin yard": (68, 9, 84, 23), "Raised burnworks": (73, 32, 90, 46), "Root hollows": (44, 40, 67, 55)},
        links, containers, {}, {}, [], _signature(levels, landmarks),
        id="greenwold", name="Greenwold Charcoal March", process_name="shifting burn wind",
        process_thresholds=[32, 62, 92],
    )
    validate_region(region)
    return region


def build_whitecairn(seed: str) -> Region:
    """Terrace bands and seeded switchbacks emphasize exposed vertical routes."""
    width, height = 98, 60
    ground = _grid(width, height, ".")
    _border(ground, "#")
    rng = stage_rng(seed, "whitecairn:terraces")
    for band in (16, 29, 42):
        offset = rng.randrange(-2, 3)
        for x in range(12, width - 2):
            y = band + offset + ((x // 13) % 3) - 1
            ground[y][x] = "#"
            if x % 4:
                ground[y + 1][x] = "r"
            if x % 7 == 0:
                ground[y][x] = "q"
    landing, village = Position(4, 51), Position(19, 48)
    quarry, kiln = Position(57, 36), Position(73, 46)
    tower, sink = Position(83, 18), Position(55, 52)
    _road(ground, [landing, village, Position(36, 49), kiln], seed, "whitecairn:lower-road")
    _road(ground, [village, Position(31, 39), quarry, Position(70, 28), tower], seed, "whitecairn:switchback")
    _road(ground, [quarry, sink, kiln], seed, "whitecairn:quarry-loop")
    _rect(ground, 12, 44, 26, 53)
    ground[48][12] = "+"
    ground[48][19] = "M"
    ground[50][23] = "c"
    _rect(ground, 51, 31, 64, 41)
    ground[36][51] = "+"
    ground[36][57] = "&"
    _rect(ground, 68, 42, 79, 51)
    ground[46][68] = "+"
    ground[46][73] = "f"
    _rect(ground, 79, 13, 89, 23)
    ground[18][79] = "+"
    ground[18][83] = ">"
    ground[sink.y][sink.x] = "<"
    ground[landing.y][landing.x] = "+"

    below, upper, roof = _grid(width, height), _grid(width, height), _grid(width, height)
    for y in range(46, 57):
        for x in range(48, 78):
            if ((x - 61) ** 2) / 210 + ((y - 52) ** 2) / 35 < 1:
                below[y][x] = "q" if (x + y) % 6 == 0 else "."
    below[52][55] = ">"
    below[51][73] = "R"
    _rect(upper, 52, 32, 63, 40)
    upper[36][57] = "<"
    for x in range(62, 84):
        upper[34][x] = "="
    _rect(upper, 80, 14, 88, 22)
    upper[18][83] = "<"
    upper[15][83] = ">"
    upper[17][86] = "C"
    _rect(roof, 79, 12, 90, 18, "^")
    roof[15][83] = "<"
    roof[13][88] = "C"
    for x in range(54, 77):
        roof[29 + (x % 3)][x] = "^"
    roof[30][57] = "C"
    links = [
        VerticalLink(sink, Position(sink.x, sink.y, -1), "sinkhole ladder"),
        VerticalLink(Position(57, 36), Position(57, 36, 1), "quarry hoist ladder"),
        VerticalLink(Position(57, 31, 1), Position(57, 30, 2), "ridge climbing pegs"),
        VerticalLink(Position(83, 18), Position(83, 18, 1), "bell-tower stair"),
        VerticalLink(Position(83, 15, 1), Position(83, 15, 2), "bell parapet ladder"),
    ]
    # Make the authored ridge-peg endpoints explicit floor.
    upper[32][57] = "."
    upper[31][57] = ">"
    roof[30][57] = "<"
    containers = _containers(seed, "whitecairn", [
        ("village", "Carrier's limestone chest", Position(25, 51), None),
        ("quarry", "Quarry tool cabinet", Position(62, 39), None),
        ("cave", "Sink-cave buried cache", Position(69, 52, -1), "light"),
        ("kiln", "Limehouse strongbox", Position(77, 49), "key"),
        ("bridge", "Ridge bridge coffer", Position(57, 30, 2), "rope"),
        ("tower", "Bell parapet chest", Position(88, 13, 2), None),
    ], ["limestone cleat", "sling cup", "quarry brace", "quarrel case", "war hammer", "hollow-bell shard"])
    _stock_containers(
        containers,
        ["kettle helm", "riveted coat", "splinted arms", "mail mitts", "brigandine cuisses", "hobnailed boots"],
        ["limestone wedge", "sling shot pouch", "quarrel case", "splint roll", "dry lamp wick", "brine wash"],
    )
    for container in containers:
        target = ground if container.position.z == 0 else {-1: below, 1: upper, 2: roof}[container.position.z]
        target[container.position.y][container.position.x] = "C"
    levels = _levels(ground, {-1: below, 1: upper, 2: roof})
    landmarks = {
        "landing": landing, "contact": village, "second_contact": Position(23, 50),
        "settlement": village, "quarry": quarry, "lime_kiln": kiln,
        "bell_tower": tower, "sinkhole": sink, "cave_entrance": sink,
        "objective": Position(73, 51, -1), "elevated": Position(83, 15, 2),
    }
    region = Region(
        "Repeated quarry bells warn of a ridge cut that is becoming unstable.",
        "Whitecairn burns lime, cuts building stone, and moves wool over high switchbacks.",
        "A private toll crew is ringing false blasts while the real quarry face slips.",
        "Reach the lower cave braces and secure lime wedges, or expose the false bell from above.",
        "lime", "wool", "scree and rockfall", width, height, levels, landmarks,
        {"Whitecairn terrace village": (9, 42, 29, 55), "Lower switchbacks": (27, 35, 50, 53), "Quarry face": (49, 28, 66, 43), "Lime kilns": (66, 39, 81, 53), "Bell ridge": (70, 10, 92, 31)},
        links, containers, {}, {}, [], _signature(levels, landmarks),
        id="whitecairn", name="Whitecairn Limestone Rise", process_name="quarry instability",
        process_thresholds=[30, 60, 88],
    )
    validate_region(region)
    return region


def _contacts(seed: str, region: Region) -> list[Contact]:
    authored = {
        "greywash": (("Edda Marr", "salt reeve"), ("Colm Vey", "wreck registrar")),
        "greenwold": (("Nera Holt", "charcoal reeve"), ("Ivo Briar", "resin tender")),
        "whitecairn": (("Pera Chalk", "quarry factor"), ("Olin Sward", "upland carter")),
    }[region.id]
    return [
        Contact(
            f"{region.id}-contact-{index + 1}", name, role,
            stage_rng(seed, f"{region.id}:contact:{index}").choice((-1, 0, 1)), [],
            region.objective_commodity if index == 0 else region.opportunity_commodity,
            region.id, region.landmarks["contact" if index == 0 else "second_contact"],
        )
        for index, (name, role) in enumerate(authored)
    ]


def _threats(region: Region, seed: str) -> list[Threat]:
    placements = {
        "greywash": [
            Position(58, 17), Position(62, 17), Position(74, 39),
            Position(78, 39), Position(87, 28), Position(89, 30),
        ],
        "greenwold": [
            Position(45, 27), Position(51, 27), Position(73, 16),
            Position(79, 39), Position(38, 42), Position(57, 47),
        ],
        "whitecairn": [
            Position(80, 18), Position(84, 18, 1), Position(56, 36),
            Position(46, 39), Position(72, 46), Position(66, 52, -1),
        ],
    }[region.id]
    reachable = region_reachable(region)
    threats: list[Threat] = []
    groups = production_encounter_groups(seed, region.id)
    composed = [
        (group_index, archetype)
        for group_index, plan in enumerate(groups)
        for archetype in plan.archetypes
    ][:6]
    for index, ((group_index, archetype), position) in enumerate(zip(composed, placements)):
        if position not in reachable:
            position = min(
                (point for point in reachable if point.z == position.z),
                key=lambda point: (abs(point.x - position.x) + abs(point.y - position.y), point.y, point.x),
            )
        group = f"{region.id}-group-{group_index}"
        threat = threat_from_archetype(archetype, position, encounter_id=f"{region.id}-{index}", group=group)
        threats.append(threat)
    elite_key = next(
        key for key, data in ENEMY_ARCHETYPES.items()
        if data["region"] == region.id and data.get("elite")
    )
    elite_positions = {
        "greywash": Position(91, 24, 1),
        "greenwold": Position(83, 39, 1),
        "whitecairn": Position(84, 16, 1),
    }
    elite_position = elite_positions[region.id]
    if elite_position not in reachable:
        elite_position = min(
            (point for point in reachable if point.z == elite_position.z),
            key=lambda point: (
                abs(point.x - elite_position.x) + abs(point.y - elite_position.y),
                point.y,
                point.x,
            ),
        )
    elite = threat_from_archetype(
        elite_key, elite_position, encounter_id=f"{region.id}-elite",
        group=f"{region.id}-elite",
    )
    elite.status = "dormant"
    threats.append(elite)
    lookout = next((threat for threat in threats if threat.role == "lookout"), None)
    if lookout:
        route = [
            Position(lookout.position.x + dx, lookout.position.y, lookout.position.z)
            for dx in (0, 1, 2, 1)
        ]
        lookout.patrol = route
    return threats


def build_new_regions(seed: str) -> tuple[dict[str, Region], dict[str, list[Contact]], dict[str, list[Threat]], dict[str, dict[str, MarketEntry]]]:
    regions = {region.id: region for region in (build_greywash(seed), build_greenwold(seed), build_whitecairn(seed))}
    contacts = {region_id: _contacts(seed, region) for region_id, region in regions.items()}
    threats = {region_id: _threats(region, seed) for region_id, region in regions.items()}
    markets: dict[str, dict[str, MarketEntry]] = {}
    for region_id, region in regions.items():
        market = {name: MarketEntry(2, 1) for name in COMMODITIES}
        market[region.objective_commodity] = MarketEntry(0, 4)
        market[region.opportunity_commodity] = MarketEntry(4, 0)
        markets[region_id] = market
    return regions, contacts, threats, markets


def store_active_region(state) -> None:
    state.region.local_elapsed = state.pressure_elapsed
    state.region.local_objective_status = state.objective_status
    state.region.local_objective_changed = state.objective_changed
    state.regions[state.active_region_id] = state.region
    state.contacts[state.active_region_id][0] = state.contact
    state.region_threats[state.active_region_id] = state.threats
    state.regional_markets[state.active_region_id] = state.market


def reconstruct_regional_process(state) -> None:
    """Reapply persisted process geometry after travel, load, or departure."""
    if state.active_region_id == "greywash":
        if state.region.process_stage >= 2 and not state.region.changes.get("tide_held"):
            for point in (Position(76, 40), Position(77, 40), Position(78, 40)):
                state.water[f"{point.x},{point.y},{point.z}"] = 99
    elif state.active_region_id == "greenwold":
        if state.region.process_stage >= 2 and not state.region.changes.get("burn_redirected"):
            for point in (Position(79, 39), Position(80, 39), Position(80, 39, 1)):
                state.smoke[f"{point.x},{point.y},{point.z}"] = 12
    elif state.active_region_id == "whitecairn" and state.region.process_stage >= 2:
        for point in (Position(55, 36), Position(56, 36), Position(57, 36)):
            state.region.tile_changes.setdefault(f"{point.x},{point.y},{point.z}", "%")
    elif state.flood_control == "lowered":
        for point in (Position(58, 42, -1), Position(58, 42), Position(78, 22)):
            state.water[f"{point.x},{point.y},{point.z}"] = 99


def activate_region(state, region_id: str) -> None:
    if region_id not in state.regions:
        raise ValueError(f"unknown Jomon destination {region_id}")
    store_active_region(state)
    state.active_region_id = region_id
    state.region = state.regions[region_id]
    state.contact = state.contacts[region_id][0]
    state.threats = state.region_threats[region_id]
    state.market = state.regional_markets[region_id]
    state.objective_status = state.region.local_objective_status
    state.objective_changed = state.region.local_objective_changed
    state.pressure_elapsed = state.region.local_elapsed
    state.escalation_spawned = bool(state.region.changes.get("escalation_spawned"))
    state.flood_control = str(state.region.changes.get("environment_control", "raised"))
    state.smoke, state.water = {}, {}
    reconstruct_regional_process(state)
