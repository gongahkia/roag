"""Deterministic, review-only room and corridor proposals.

These records are deliberately kept out of src/rooms.lua and live generation
until the grouped review decisions have been considered.
"""

from collections import deque


FAMILIES = (
    ("forked_gallery", "Forked Gallery", 17, 9, ("combat",),
     "Two flanking lanes and broken sight lines; an enemy can approach from either side."),
    ("twin_vault", "Twin Vault", 15, 11, ("treasure",),
     "Paired storage pockets with alternate entries around a guarded center."),
    ("watchtower", "Watchtower", 9, 13, ("combat",),
     "A narrow vertical fight with short side landings and cover."),
    ("switchback", "Switchback", 11, 15, ("combat",),
     "Offset internal shelves make a long room feel like a short maze."),
    ("broken_cloister", "Broken Cloister", 21, 15, ("landmark", "combat"),
     "A large open court surrounded by a broken walkable ring."),
    ("crossroads", "Crossroads", 13, 13, ("start",),
     "An alternate arrival room with four readable exits and quadrant cover."),
    ("long_gallery", "Long Gallery", 23, 7, ("combat",),
     "A thin ranged lane with staggered cover and escape gaps."),
    ("three_bays", "Three Bays", 23, 11, ("combat",),
     "Three connected pockets support flanking rather than one frontal fight."),
    ("staggered_archive", "Staggered Archive", 19, 13, ("treasure",),
     "Alternating shelf aisles turn a cache into a navigation choice."),
    ("courtyard", "Courtyard", 19, 17, ("landmark", "treasure"),
     "A broad central landmark with a perimeter route and several openings."),
    ("spiral", "Broken Spiral", 17, 17, ("combat",),
     "An incomplete inner ring supports pursuit and retreat around cover."),
    ("causeway", "Causeway", 21, 9, ("treasure",),
     "Two hazard banks and several crossing points guard a reward."),
    ("mosaic", "Mosaic Court", 15, 15, ("landmark", "combat"),
     "Small separated structures leave many lines of movement."),
    ("side_annex", "Side Annex", 11, 11, ("treasure", "secret"),
     "An off-axis nook makes the chest and possible hidden wall less obvious."),
    ("fan_arena", "Fan Arena", 23, 17, ("elite",),
     "A second boss arena uses diagonal cover and four spacious approaches."),
)

BIOME_DETAILS = {
    "ruins": ("b", "ward"),
    "warrens": ("g", "mushroom"),
    "canals": ("~", "pillar"),
    "courtyards": ("B", "brazier"),
    "observatory": ("i", "crystal"),
    "archive": ("m", "pillar"),
    "forge": ("c", "brazier"),
}
CORRIDOR_THEMES = {
    "ruins": "Bramble traces can punish the shortcut through a side branch.",
    "warrens": "Sticky bog patches slow the detour without sealing its exits.",
    "canals": "Shallow water banks frame the optional path and its clue.",
    "courtyards": "Breakable barricades mark a possible hidden side pocket.",
    "observatory": "Ice along the branch changes dash distance near the quest node.",
    "archive": "Mist briefly hides the clue while leaving the route walkable.",
    "forge": "Crystal patches can chain damage near a corridor encounter.",
}
WALKABLE = frozenset(".bgimc")


def reachable(grid, start, blocked=()):
    width, height = len(grid[0]), len(grid)
    blocked = set(blocked)
    if start in blocked or grid[start[1] - 1][start[0] - 1] not in WALKABLE:
        return set()
    seen = {start}
    queue = deque([start])
    while queue:
        x, y = queue.popleft()
        for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0)):
            point = (x + dx, y + dy)
            px, py = point
            if (1 <= px <= width and 1 <= py <= height and point not in seen
                    and point not in blocked and grid[py - 1][px - 1] in WALKABLE):
                seen.add(point)
                queue.append(point)
    return seen


def make_blueprint(family, width, height, variant):
    grid = [list("#" * width)] + [list("#" + "." * (width - 2) + "#")
                                        for _ in range(height - 2)] + [list("#" * width)]
    cx, cy = (width + 1) // 2, (height + 1) // 2
    sockets = [dict(x=cx, y=1, facing="N", width=1),
               dict(x=width, y=cy, facing="E", width=1),
               dict(x=cx, y=height, facing="S", width=1),
               dict(x=1, y=cy, facing="W", width=1)]
    for socket in sockets:
        grid[socket["y"] - 1][socket["x"] - 1] = "."
    protected = {(x, cy) for x in range(1, width + 1)}
    protected.update((cx, y) for y in range(1, height + 1))
    protected.add((2, 2))  # possible secret approach

    def wall(x, y):
        if 2 <= x <= width - 1 and 2 <= y <= height - 1 and (x, y) not in protected:
            grid[y - 1][x - 1] = "#"

    def horizontal(y, left, right, gaps=()):
        for x in range(left, right + 1):
            if x not in gaps:
                wall(x, y)

    def vertical(x, top, bottom, gaps=()):
        for y in range(top, bottom + 1):
            if y not in gaps:
                wall(x, y)

    if family == "forked_gallery":
        for y in (3, height - 2):
            horizontal(y, 3, width - 2, (4, cx - 2, cx + 2, width - 3))
    elif family == "twin_vault":
        vertical(5, 3, height - 2, (4, height - 3))
        vertical(width - 4, 3, height - 2, (4, height - 3))
        horizontal(3, 5, width - 4, (cx - 1, cx + 1))
        horizontal(height - 2, 5, width - 4, (cx - 1, cx + 1))
    elif family == "watchtower":
        for y in (3, 5, height - 4, height - 2):
            wall(3, y)
            wall(width - 2, y + (1 if y in (3, height - 4) else 0))
    elif family == "switchback":
        for index, y in enumerate((4, 7, 10, 13)):
            horizontal(y, 2, width - 1, (3 if index % 2 == 0 else width - 2,))
    elif family == "broken_cloister":
        horizontal(4, 5, width - 4, (cx - 3, cx + 3))
        horizontal(height - 3, 5, width - 4, (cx - 3, cx + 3))
        vertical(5, 4, height - 3, (cy - 2, cy + 2))
        vertical(width - 4, 4, height - 3, (cy - 2, cy + 2))
        for x, y in ((3, 3), (width - 2, 3), (3, height - 2), (width - 2, height - 2)):
            wall(x, y)
    elif family == "crossroads":
        for x in (3, width - 3):
            for y in (3, height - 3):
                for dx in (0, 1):
                    for dy in (0, 1):
                        wall(x + dx, y + dy)
    elif family == "long_gallery":
        for x in (4, 7, 10, 14, 17, 20):
            wall(x, 3 if x % 2 == 0 else 5)
            if x in (7, 17):
                wall(x + 1, 3 if x % 2 == 0 else 5)
    elif family == "three_bays":
        for x in (7, width - 6):
            vertical(x, 2, height - 1, (4, height - 3))
        horizontal(3, 9, width - 8, (cx,))
        horizontal(height - 2, 9, width - 8, (cx,))
    elif family == "staggered_archive":
        for index, x in enumerate((4, 7, 12, 15)):
            vertical(x, 3, height - 2, (4 + (index % 2) * 5,))
    elif family == "courtyard":
        horizontal(4, 5, width - 4, (cx - 3, cx + 3))
        horizontal(height - 3, 5, width - 4, (cx - 3, cx + 3))
        vertical(5, 4, height - 3, (cy - 3, cy + 3))
        vertical(width - 4, 4, height - 3, (cy - 3, cy + 3))
        for x in (cx - 3, cx + 3):
            for y in (cy - 3, cy + 3):
                wall(x, y)
    elif family == "spiral":
        horizontal(4, 4, width - 3, (cx - 2,))
        vertical(width - 3, 4, height - 3, (cy + 2,))
        horizontal(height - 3, 4, width - 3, (cx + 2,))
        vertical(4, 4, height - 3, (cy - 2,))
        horizontal(7, 7, width - 6, (cx - 1,))
        vertical(7, 7, height - 6, (cy + 1,))
    elif family == "causeway":
        for y in (3, height - 2):
            horizontal(y, 3, width - 2, (5, cx - 4, cx + 4, width - 4))
    elif family == "mosaic":
        for x in (3, width - 4):
            for y in (3, height - 4):
                for dx in (0, 1):
                    for dy in (0, 1):
                        wall(x + dx, y + dy)
        wall(cx - 2, cy - 2)
        wall(cx + 2, cy + 2)
    elif family == "side_annex":
        vertical(4, 3, height - 2, (4, height - 3))
        horizontal(4, 4, width - 2, (width - 3,))
        horizontal(height - 3, 4, width - 2, (width - 3,))
    elif family == "fan_arena":
        for distance in (4, 5, 6):
            for dx, dy in ((1, 1), (1, -1), (-1, 1), (-1, -1)):
                wall(cx + dx * distance, cy + dy * max(2, distance - 2))
        horizontal(4, 4, width - 3, (6, cx - 4, cx + 4, width - 5))
        horizontal(height - 3, 4, width - 3, (6, cx - 4, cx + 4, width - 5))
    else:
        raise ValueError(family)

    # Each biome gets a real geometry variation inside its family: a window
    # through existing cover and several new, separated cover pieces. Keep
    # every floor reachable and preserve the cross linking all four sockets.
    variation_order = lambda x, y: (
        (x * 37 + y * 19 + variant * (x * 11 + y * 7 + 3)
         + len(family) * 17) % 127, (x * 5 + y * 3 + variant) % 11, y, x)
    inner = [(x, y) for y in range(3, height - 1)
             for x in range(3, width - 1) if (x, y) not in protected]
    for x, y in sorted(inner, key=lambda point: variation_order(*point)):
        if grid[y - 1][x - 1] != "#" or not any(
                grid[y + dy - 1][x + dx - 1] == "."
                for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0))):
            continue
        grid[y - 1][x - 1] = "."
        if len(reachable(grid, (cx, cy))) == sum(cell == "." for row in grid for cell in row):
            break
        grid[y - 1][x - 1] = "#"
    added = []
    for x, y in sorted(inner, key=lambda point: variation_order(*point), reverse=True):
        if len(added) == 3:
            break
        if grid[y - 1][x - 1] != "." or any(
                abs(x - px) + abs(y - py) < 3 for px, py in added):
            continue
        grid[y - 1][x - 1] = "#"
        if len(reachable(grid, (cx, cy))) == sum(cell == "." for row in grid for cell in row):
            added.append((x, y))
        else:
            grid[y - 1][x - 1] = "."
    assert len(added) == 3, (family, variant)
    return grid, sockets


def nearest_floor(grid, target, forbidden):
    width, height = len(grid[0]), len(grid)
    candidates = ((abs(x - target[0]) + abs(y - target[1]), y, x)
                  for y in range(2, height) for x in range(2, width)
                  if grid[y - 1][x - 1] == "." and (x, y) not in forbidden)
    _, y, x = min(candidates)
    return dict(x=x, y=y)


def build_room(spec, biome, variant):
    family, name, width, height, roles, intent = spec
    biome_id, biome_name, strategy, room_prefix = biome
    grid, sockets = make_blueprint(family, width, height, variant)
    cx, cy = (width + 1) // 2, (height + 1) // 2
    hazard, structure_kind = BIOME_DETAILS[strategy]
    # The same family acquires biome-specific terrain without closing any
    # route or changing the protected socket approaches.
    if strategy != "ruins":
        candidates = [(x, y) for y in range(3, height - 1)
                      for x in range(3, width - 1)
                      if x != cx and y != cy and grid[y - 1][x - 1] == "."
                      and (x * 7 + y * 11 + len(family)) % 9 == 0]
        for x, y in candidates[:max(3, min(9, width // 2))]:
            old = grid[y - 1][x - 1]
            grid[y - 1][x - 1] = hazard
            if hazard not in WALKABLE and len(reachable(grid, (cx, cy))) != sum(
                    cell in WALKABLE for row in grid for cell in row):
                grid[y - 1][x - 1] = old
    else:
        for y in range(3, height - 1):
            for x in range(3, width - 1):
                if x != cx and y != cy and grid[y - 1][x - 1] == "." \
                        and (x * 7 + y * 11 + len(family)) % 13 == 0:
                    grid[y - 1][x - 1] = "b"

    forbidden = {(2, 2), (cx, cy)}
    for socket in sockets:
        x, y = socket["x"], socket["y"]
        forbidden.add((x, y))
        forbidden.add((x + (1 if x == 1 else -1 if x == width else 0),
                       y + (1 if y == 1 else -1 if y == height else 0)))
    markers = {kind: [] for kind in ("player", "enemy", "chest", "landmark",
                                     "elite", "stair", "secret_anchor", "secret_wall")}
    markers["secret_anchor"] = [dict(x=2, y=2)]
    if "start" not in roles and "elite" not in roles:
        markers["secret_wall"] = [dict(x=1, y=2)]
    if "start" in roles:
        markers["player"] = [dict(x=cx, y=cy)]
    if "elite" in roles:
        markers["elite"] = [dict(x=cx, y=cy)]
        markers["landmark"] = [dict(x=cx, y=cy)]
    if "start" not in roles and "elite" not in roles:
        markers["enemy"] = []
        for target in ((width - 3, 3), (3, height - 3)):
            point = nearest_floor(grid, target, forbidden)
            markers["enemy"].append(point)
            forbidden.add((point["x"], point["y"]))
        point = nearest_floor(grid, (width - 3, height - 3), forbidden)
        markers["chest"] = [point]
        forbidden.add((point["x"], point["y"]))
    if "landmark" in roles and "elite" not in roles:
        point = nearest_floor(grid, (cx, cy), forbidden)
        markers["landmark"] = [point]
        forbidden.add((point["x"], point["y"]))
    for points in markers.values():
        forbidden.update((p["x"], p["y"]) for p in points)

    structures = []
    # Worst-case check treats every optional structure as blocking. This keeps
    # all sockets and critical markers reachable for any later prop choice.
    goals = {(s["x"], s["y"]) for s in sockets}
    goals.update((p["x"], p["y"]) for points in markers.values()
                 for p in points if grid[p["y"] - 1][p["x"] - 1] in WALKABLE)
    candidates = sorted(
        ((abs(x - cx) + abs(y - cy), y, x) for y in range(3, height - 1)
         for x in range(3, width - 1) if grid[y - 1][x - 1] == "."
         and x != cx and y != cy and (x, y) not in forbidden),
        reverse=True)
    blocked = set()
    for _, y, x in candidates:
        if len(structures) >= 3:
            break
        if any(abs(x - p["x"]) + abs(y - p["y"]) < 4 for p in structures):
            continue
        trial = blocked | {(x, y)}
        seen = reachable(grid, (cx, cy), trial)
        if goals <= seen and len(seen) == sum(
                cell in WALKABLE for row in grid for cell in row) - len(trial):
            structures.append(dict(x=x, y=y, kind=structure_kind))
            blocked = trial
    if len(structures) < 2:
        raise ValueError(f"Too few safe structure slots: {family}/{strategy}")

    room_id = room_prefix + family
    return dict(id=room_id, name=name, biome_id=biome_id, biome_name=biome_name,
                strategy=strategy, family=family, roles=list(roles), origin="proposal",
                source="tools/room_prototypes.py", mirror=True,
                rotations=[0, 90, 180, 270], biomes=[biome_id],
                width=width, height=height, geometry_variant=variant + 1,
                rows=["".join(row) for row in grid],
                sockets=sockets, markers=markers, editable=[],
                protected_default=True, structures=structures, intent=intent)


def validate_room(room):
    grid = room["rows"]
    width, height = room["width"], room["height"]
    assert 7 <= width <= 23 and 7 <= height <= 17 and width % 2 and height % 2
    assert len(grid) == height and all(len(row) == width for row in grid)
    assert len(room["sockets"]) == 4
    goals = {(s["x"], s["y"]) for s in room["sockets"]}
    for points in room["markers"].values():
        for p in points:
            assert grid[p["y"] - 1][p["x"] - 1] == "." or (
                p in room["markers"].get("secret_wall", [])
                and grid[p["y"] - 1][p["x"] - 1] == "#")
            if grid[p["y"] - 1][p["x"] - 1] in WALKABLE:
                goals.add((p["x"], p["y"]))
    seen = reachable(grid, ((width + 1) // 2, (height + 1) // 2))
    assert goals <= seen, room["id"]
    assert len(seen) == sum(tile in WALKABLE for row in grid for tile in row), room["id"]
    blocked = {(p["x"], p["y"]) for p in room["structures"]}
    assert goals <= reachable(grid, ((width + 1) // 2, (height + 1) // 2), blocked), room["id"]


def corridor_concepts(biomes):
    """Five walkable, review-only corridor topologies with optional quest nodes."""
    descriptions = (
        ("spur", "Hidden Spur", "A short branch ends at a clue and can lead to a cache.",
         "Find a wall clue, then uncover its reward pocket.", 15),
        ("braid", "Braided Bypass", "A second path leaves and rejoins the main corridor.",
         "Take the slower branch to recover a key before its gate.", 12),
        ("switchback", "Crooked Switchback", "A bent main path gains a misleading dead end.",
         "Activate a plate in the dead end to open a side reward.", 12),
        ("hub", "Four Way Hub", "A small junction sprouts two unequal side paths.",
         "Light two braziers in either order to reveal a reward.", 10),
        ("false_end", "False End", "A dead-end branch hides a wall that opens a return loop.",
         "Follow the clue, open the hidden wall, and face an optional encounter.", 8),
    )
    concepts = []
    for slug, name, intent, quest, chance in descriptions:
        width, height, cy = 31, 13, 7
        grid = [["#"] * width for _ in range(height)]

        def carve(x, y):
            assert 1 <= x <= width and 1 <= y <= height
            grid[y - 1][x - 1] = "."

        def line(x1, y1, x2, y2):
            assert x1 == x2 or y1 == y2
            for y in range(min(y1, y2), max(y1, y2) + 1):
                for x in range(min(x1, x2), max(x1, x2) + 1):
                    carve(x, y)

        if slug == "switchback":
            for segment in ((1, 7, 8, 7), (8, 7, 8, 10), (8, 10, 18, 10),
                            (18, 10, 18, 5), (18, 5, 25, 5), (25, 5, 25, 7),
                            (25, 7, 31, 7), (13, 10, 13, 12)):
                line(*segment)
            nodes = [dict(kind="plate", x=13, y=12), dict(kind="reward", x=22, y=5)]
        else:
            line(1, cy, width, cy)
            if slug == "spur":
                line(11, 7, 11, 3)
                line(10, 3, 13, 3)
                nodes = [dict(kind="clue", x=11, y=4), dict(kind="reward", x=13, y=3)]
            elif slug == "braid":
                for segment in ((8, 7, 8, 3), (8, 3, 24, 3), (24, 3, 24, 7)):
                    line(*segment)
                nodes = [dict(kind="key", x=16, y=3), dict(kind="gate", x=25, y=7)]
            elif slug == "hub":
                for y in range(5, 10):
                    line(14, y, 18, y)
                line(16, 5, 16, 2)
                line(16, 9, 16, 12)
                nodes = [dict(kind="brazier", x=16, y=2),
                         dict(kind="brazier", x=16, y=12),
                         dict(kind="reward", x=17, y=7)]
            else:
                for segment in ((12, 7, 12, 10), (12, 10, 20, 10),
                                (20, 10, 20, 12), (22, 10, 28, 10),
                                (28, 10, 28, 7)):
                    line(*segment)
                nodes = [dict(kind="clue", x=18, y=10),
                         dict(kind="secret_wall", x=21, y=10),
                         dict(kind="enemy", x=24, y=10),
                         dict(kind="reward", x=26, y=10)]
        rows = ["".join(row) for row in grid]
        assert all(len(row) == width for row in rows)
        assert len(reachable(rows, (1, cy))) == sum(
            tile == "." for row in rows for tile in row)
        variants = []
        for biome_id, biome_name, strategy, _ in biomes:
            themed = [list(row) for row in rows]
            hazard = BIOME_DETAILS[strategy][0]
            occupied = {(node["x"], node["y"]) for node in nodes}
            added = 0
            for y in range(3, height - 1):
                for x in range(3, width - 1):
                    if added >= 5 or (x, y) in occupied or (x * 5 + y * 7) % 11:
                        continue
                    if hazard in WALKABLE and themed[y - 1][x - 1] == ".":
                        themed[y - 1][x - 1] = hazard
                        added += 1
                    elif hazard not in WALKABLE and themed[y - 1][x - 1] == "#" \
                            and any(themed[ny - 1][nx - 1] == "." for nx, ny in
                                    ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))):
                        themed[y - 1][x - 1] = hazard
                        added += 1
            variant_rows = ["".join(row) for row in themed]
            assert len(reachable(variant_rows, (1, cy))) == sum(
                tile in WALKABLE for row in variant_rows for tile in row)
            variants.append(dict(biome_id=biome_id, biome_name=biome_name,
                                 strategy=strategy, rows=variant_rows,
                                 theme_note=CORRIDOR_THEMES[strategy]))
        concepts.append(dict(id=f"proposal:corridor/{slug}", name=name,
                             family=slug, intent=intent, quest=quest,
                             suggested_frequency=f"Up to {chance}% of suitable corridor links",
                             rows=rows, width=width, height=height,
                             variants=variants,
                             sockets=[dict(x=1, y=cy, facing="W", width=1),
                                      dict(x=width, y=cy, facing="E", width=1)],
                             markers={}, quest_nodes=nodes, editable=[]))
    return concepts


def build_proposals(existing_rooms):
    biomes = []
    seen = set()
    for room in existing_rooms:
        if room["origin"] != "built-in" or room["biome_id"] in seen:
            continue
        seen.add(room["biome_id"])
        biomes.append((room["biome_id"], room["biome_name"], room["strategy"],
                       room["id"][:-len(room["family"])]))
    assert len(biomes) == len(BIOME_DETAILS) == 7
    families = []
    for spec in FAMILIES:
        variants = [build_room(spec, biome, index) for index, biome in enumerate(biomes)]
        for room in variants:
            validate_room(room)
        slug, name, width, height, roles, intent = spec
        families.append(dict(id=f"proposal:family/{slug}", name=name,
                             family=slug, width=width, height=height,
                             roles=list(roles), intent=intent, variants=variants))
    assert len(families) == 15 and sum(len(group["variants"]) for group in families) == 105
    return dict(families=families, corridors=corridor_concepts(biomes),
                status="review-only; not in live generation")
