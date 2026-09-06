"""Three bounded topology builders and direct terrain dressing for Hearthford."""

from __future__ import annotations

from collections import deque

from .state import Position, Room, RoomExit, stage_rng

ROOM_WIDTH = 29
ROOM_HEIGHT = 13

EXIT_POSITION = {
    "west": Position(0, 6),
    "east": Position(ROOM_WIDTH - 1, 6),
    "north": Position(14, 0),
    "south": Position(14, ROOM_HEIGHT - 1),
}
EXIT_GLYPH = {"west": "<", "east": ">", "north": "^", "south": "v"}
INWARD_POSITION = {
    "west": Position(1, 6),
    "east": Position(ROOM_WIDTH - 2, 6),
    "north": Position(14, 1),
    "south": Position(14, ROOM_HEIGHT - 2),
}
OPPOSITE = {"west": "east", "east": "west", "north": "south", "south": "north"}


def _new_room(room_id: str, name: str, place: str, purpose: str) -> Room:
    return Room(room_id, name, place, purpose, [], {}, {}, False, None, 0)


def _link(rooms: dict[str, Room], first: str, direction: str, second: str) -> None:
    opposite = OPPOSITE[direction]
    rooms[first].exits[direction] = RoomExit(second, EXIT_POSITION[direction], INWARD_POSITION[opposite])
    rooms[second].exits[opposite] = RoomExit(first, EXIT_POSITION[opposite], INWARD_POSITION[direction])


def hub_and_branches(seed: str) -> tuple[dict[str, Room], list[tuple[str, str]]]:
    """Build Hearthford's fixed-size hub with one seed-optional cross-link."""
    rooms = {
        "hearthford_quay": _new_room("hearthford_quay", "Hearthford Quay", "settlement", "Jomon access and route hub"),
        "market_lane": _new_room("market_lane", "Market Lane", "settlement", "trade, services, and local resource"),
        "tally_house": _new_room("tally_house", "Tally House", "settlement", "contact and objective information"),
    }
    _link(rooms, "hearthford_quay", "east", "market_lane")
    _link(rooms, "hearthford_quay", "north", "tally_house")
    edges = [("hearthford_quay", "market_lane"), ("hearthford_quay", "tally_house")]
    if stage_rng(seed, "topology-hub").randrange(2):
        _link(rooms, "market_lane", "south", "tally_house")
        edges.append(("market_lane", "tally_house"))
    return rooms, edges


def branching_network(seed: str) -> tuple[dict[str, Room], list[tuple[str, str]]]:
    """Build a small branching Reedwood network with a required reconnecting loop."""
    rooms = {
        "reed_gate": _new_room("reed_gate", "Reedwood Gate", "wilderness", "route choice and controllable gate"),
        "willow_islet": _new_room("willow_islet", "Willow Islet", "wilderness", "foraging and quiet knowledge"),
        "eel_cut": _new_room("eel_cut", "Eel Cut", "wilderness", "territorial animal and shallow water"),
        "mudflats": _new_room("mudflats", "Bell-Reed Mudflats", "wilderness", "route reconnection and movable cover"),
    }
    _link(rooms, "reed_gate", "east", "willow_islet")
    _link(rooms, "reed_gate", "south", "eel_cut")
    _link(rooms, "willow_islet", "south", "mudflats")
    _link(rooms, "eel_cut", "east", "mudflats")
    edges = [
        ("reed_gate", "willow_islet"), ("reed_gate", "eel_cut"),
        ("willow_islet", "mudflats"), ("eel_cut", "mudflats"),
    ]
    if stage_rng(seed, "topology-branch").randrange(3) == 0:
        _link(rooms, "willow_islet", "east", "eel_cut")
        edges.append(("willow_islet", "eel_cut"))
    return rooms, edges


def linear_with_sides(seed: str) -> tuple[dict[str, Room], list[tuple[str, str]]]:
    """Build the critical mill route and attach one bounded seed-varying side room."""
    side_name = stage_rng(seed, "topology-side-name").choice(("Gear Shed", "Lime Store", "Rope Loft"))
    rooms = {
        "lower_towpath": _new_room("lower_towpath", "Lower Towpath", "infrastructure", "ranged obstruction and shutter"),
        "crane_walk": _new_room("crane_walk", "Crane Walk", "infrastructure", "flood control and unstable walkway"),
        "mill_yard": _new_room("mill_yard", "Mill Yard", "infrastructure", "mixed fortified encounter"),
        "wheelhouse": _new_room("wheelhouse", "Broken Wheelhouse", "infrastructure", "objective and dangerous machinery"),
        "works_side": _new_room("works_side", side_name, "infrastructure", "optional salvage and discovery"),
    }
    _link(rooms, "lower_towpath", "east", "crane_walk")
    _link(rooms, "crane_walk", "east", "mill_yard")
    _link(rooms, "mill_yard", "east", "wheelhouse")
    attach = stage_rng(seed, "topology-side-attach").choice(("lower_towpath", "crane_walk", "mill_yard"))
    _link(rooms, attach, "north", "works_side")
    edges = [
        ("lower_towpath", "crane_walk"), ("crane_walk", "mill_yard"),
        ("mill_yard", "wheelhouse"), (attach, "works_side"),
    ]
    return rooms, edges


def _blank() -> list[list[str]]:
    grid = [["." for _ in range(ROOM_WIDTH)] for _ in range(ROOM_HEIGHT)]
    for x in range(ROOM_WIDTH):
        grid[0][x] = grid[-1][x] = "#"
    for y in range(ROOM_HEIGHT):
        grid[y][0] = grid[y][-1] = "#"
    return grid


def _settlement_dressing(room: Room, seed: str) -> list[list[str]]:
    grid = _blank()
    for x in range(3, 11):
        grid[2][x] = "#"
    for x in range(18, 26):
        grid[9][x] = "#"
    if room.id == "hearthford_quay":
        grid[6][1] = "+"
        for y in (2, 3, 9, 10):
            grid[y][24] = "~"
    elif room.id == "market_lane":
        grid[4][8], grid[8][21], grid[5][18] = "r", "c", "O"
    else:
        grid[6][14], grid[4][20] = "M", "c"
    return grid


def _wilderness_dressing(room: Room, seed: str) -> list[list[str]]:
    grid = _blank()
    rng = stage_rng(seed, f"terrain:{room.id}")
    for x, y in ((3, 2), (4, 2), (24, 2), (25, 2), (3, 10), (25, 10)):
        grid[y][x] = rng.choice(("~", "T"))
    for x in range(7, 12):
        grid[3][x] = "~"
    for x in range(18, 23):
        grid[9][x] = "~"
    if room.id == "reed_gate":
        grid[6][14] = "D"
    elif room.id == "willow_islet":
        grid[5][10] = "?"
    elif room.id == "eel_cut":
        for x in range(10, 19):
            grid[6][x] = "m"
        grid[4][22] = "?"
    else:
        grid[6][13] = "O"
        grid[8][8] = "?"
    return grid


def _works_dressing(room: Room, seed: str) -> list[list[str]]:
    grid = _blank()
    for y in range(2, 11):
        if y not in (5, 6, 7):
            grid[y][5] = "#"
            grid[y][23] = "#"
    if room.id == "lower_towpath":
        grid[6][14], grid[4][18] = "D", "%"
    elif room.id == "crane_walk":
        grid[6][14], grid[8][19] = "&", "%"
    elif room.id == "mill_yard":
        grid[6][13], grid[5][19] = "O", "%"
    elif room.id == "wheelhouse":
        grid[6][21], grid[6][14] = "R", "&"
        for x in range(9, 13):
            grid[4][x] = "%"
    else:
        grid[5][12], grid[7][18] = "?", "r"
    return grid


def _dress(room: Room, seed: str) -> None:
    if room.place == "settlement":
        grid = _settlement_dressing(room, seed)
    elif room.place == "wilderness":
        grid = _wilderness_dressing(room, seed)
    else:
        grid = _works_dressing(room, seed)
    for direction, exit_ in room.exits.items():
        grid[exit_.position.y][exit_.position.x] = EXIT_GLYPH[direction]
    room.map_rows = ["".join(row) for row in grid]


def build_rooms(seed: str) -> tuple[dict[str, Room], str]:
    settlement, hub_edges = hub_and_branches(seed)
    wild, branch_edges = branching_network(seed)
    works, works_edges = linear_with_sides(seed)
    rooms = {**settlement, **wild, **works}
    _link(rooms, "hearthford_quay", "south", "reed_gate")
    _link(rooms, "mudflats", "east", "lower_towpath")
    for room in rooms.values():
        _dress(room, seed)

    discovery_rng = stage_rng(seed, "discoveries")
    first = discovery_rng.choice(("willow dressing", "reed-step notes"))
    rooms["willow_islet"].discovery = first
    rooms["eel_cut"].discovery = "sealed tally"
    rooms["works_side"].discovery = discovery_rng.choice(("pulley key", "dry smoke charge"))
    if stage_rng(seed, "tide-knot").randrange(5) == 0:
        rooms["mudflats"].discovery = "tide-knot charm"

    queue = deque(["hearthford_quay"])
    seen = {"hearthford_quay"}
    while queue:
        current = queue.popleft()
        for exit_ in rooms[current].exits.values():
            if exit_.target not in seen:
                rooms[exit_.target].depth = rooms[current].depth + 1
                seen.add(exit_.target)
                queue.append(exit_.target)
    signature_edges = hub_edges + branch_edges + works_edges + [
        ("hearthford_quay", "reed_gate"), ("mudflats", "lower_towpath")
    ]
    signature = "|".join(f"{a}>{b}" for a, b in sorted(signature_edges))
    return rooms, signature
