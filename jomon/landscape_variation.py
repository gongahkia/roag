"""Seeded regional landforms and small accessible structures on both sides of ground."""

from __future__ import annotations

from collections import deque
import hashlib

from .catalog import CatalogError, load_catalog
from .state import GameState, Position, Region, VerticalLink, stage_rng


VARIANTS = load_catalog("terrain_variation.json", ("regions",))["regions"]
_TERRAIN = frozenset("mtrq_:ws")
if not isinstance(VARIANTS, dict) or len(VARIANTS) != 8:
    raise CatalogError("terrain_variation.json needs eight regional patterns")
for region_id, row in VARIANTS.items():
    if (not isinstance(region_id, str) or not isinstance(row, dict)
            or set(row) != {"pockets", "upper", "lower", "traveller"}
            or not all(isinstance(row[key], str) and row[key] for key in ("upper", "lower", "traveller"))
            or not isinstance(row["pockets"], list) or len(row["pockets"]) != 3
            or any(not isinstance(pocket, list) or len(pocket) != 2
                   or not isinstance(pocket[0], str) or not pocket[0]
                   or pocket[1] not in _TERRAIN for pocket in row["pockets"])):
        raise CatalogError(f"invalid terrain variation for {region_id}")


def _connected_ground(region: Region) -> set[tuple[int, int]]:
    landing = region.landmarks["landing"]
    queue = deque([(landing.x, landing.y)])
    seen = {(landing.x, landing.y)}
    while queue:
        x, y = queue.popleft()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if not 0 <= nx < region.width or not 0 <= ny < region.height or (nx, ny) in seen:
                continue
            tile = region.tile_changes.get(f"{nx},{ny},0", region.levels["0"][ny][nx])
            if tile in {" ", "#", "~", "T"}:
                continue
            seen.add((nx, ny))
            queue.append((nx, ny))
    return seen


def _protected(region: Region) -> set[tuple[int, int]]:
    points = {(point.x, point.y) for point in region.landmarks.values() if point.z == 0}
    points.update((box.position.x, box.position.y) for box in region.containers if box.position.z == 0)
    points.update((point.x, point.y) for link in region.vertical_links
                  for point in (link.first, link.second) if point.z == 0)
    landing = region.landmarks["landing"]
    points.update((x, y) for y in range(max(0, landing.y - 5), min(region.height, landing.y + 6))
                  for x in range(max(0, landing.x - 5), min(region.width, landing.x + 6)))
    return points


def _structure(region: Region, seed: str, level: int, ground: list[list[str]],
               reachable: set[tuple[int, int]], protected: set[tuple[int, int]]) -> None:
    key = "field_upper" if level == 1 else "field_lower"
    rows = [list(row) for row in region.levels[str(level)]]
    candidates = []
    half_width = half_height = 0
    for width, height, clearance in ((3, 2, 4), (2, 2, 3), (2, 1, 2)):
        near_protected = {(px + dx, py + dy) for px, py in protected
                          for dx in range(-clearance, clearance + 1)
                          for dy in range(-clearance, clearance + 1)
                          if abs(dx) + abs(dy) <= clearance}
        candidates = []
        for x, y in reachable:
            if (not width + 2 <= x < region.width - width - 2
                    or not height + 2 <= y < region.height - height - 2
                    or ground[y][x] not in ".,mrqt_:s" or (x, y) in near_protected):
                continue
            if any(rows[ry][rx] != " " or f"{rx},{ry},{level}" in region.tile_changes
                   for ry in range(y - height, y + height + 1)
                   for rx in range(x - width, x + width + 1)):
                continue
            candidates.append((x, y))
        if candidates:
            half_width, half_height = width, height
            break
    if not candidates:
        raise RuntimeError(f"{region.name} has no reachable {key} footprint")
    rng = stage_rng(seed, f"landform:{region.id}:{key}")
    candidates.sort()
    x, y = rng.choice(candidates)
    for ry in range(y - half_height, y + half_height + 1):
        for rx in range(x - half_width, x + half_width + 1):
            rows[ry][rx] = "#" if ry in {y - half_height, y + half_height} or rx in {x - half_width, x + half_width} else "."
    if half_width == 3:
        divider = x + (-1 if level == 1 else 1)
        for ry in range(y - half_height + 1, y + half_height):
            rows[ry][divider] = "#"
        rows[y][divider] = "+"
    lower, upper = Position(x, y, min(0, level)), Position(x, y, max(0, level))
    ground[y][x] = ">" if level == 1 else "<"
    rows[y][x] = "<" if level == 1 else ">"
    region.levels[str(level)] = ["".join(row) for row in rows]
    region.vertical_links.append(VerticalLink(lower, upper, VARIANTS[region.id]["upper" if level == 1 else "lower"]))
    region.landmarks[key] = Position(x, y, level)
    region.generation_facts[key] = f"{VARIANTS[region.id]['upper' if level == 1 else 'lower']} at {x},{y},z{level:+d}"
    protected.add((x, y))


def install(region: Region, seed: str, *, occupied: tuple[Position, ...] = ()) -> None:
    """Keep the authored roads and landmarks; add passable terrain and side routes."""
    if region.id not in VARIANTS or "field_upper" in region.landmarks:
        return
    reachable = _connected_ground(region)
    protected = _protected(region)
    protected.update((point.x + dx, point.y + dy) for point in occupied if point.z == 0
                     for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)))
    ground = [list(row) for row in region.levels["0"]]
    anchors: list[tuple[int, int]] = []
    for index, (name, glyph) in enumerate(VARIANTS[region.id]["pockets"]):
        rng = stage_rng(seed, f"landform:{region.id}:pocket:{index}")
        choices = [(x, y) for x, y in reachable
                   if 5 <= x < region.width - 5 and 5 <= y < region.height - 5
                   and ground[y][x] == "." and (x, y) not in protected
                   and all(abs(x - px) + abs(y - py) >= 9 for px, py in anchors)]
        if not choices:
            raise RuntimeError(f"{region.name} has no safe {name} pocket")
        choices.sort()
        x, y = rng.choice(choices)
        anchors.append((x, y))
        region.landmarks[f"landform_{index}"] = Position(x, y)
        for ry in range(y - 4, y + 5):
            for rx in range(x - 4, x + 5):
                if (ground[ry][rx] != "." or (rx, ry) in protected
                        or f"{rx},{ry},0" in region.tile_changes
                        or (rx - x) ** 2 + (ry - y) ** 2 > rng.randint(6, 17)):
                    continue
                ground[ry][rx] = glyph
        region.generation_facts[f"landform:{index}"] = f"{name} near {x},{y}; {glyph} footing"
    # Underground voids are scarcer in fen and cave-heavy maps; reserve one
    # before choosing the more flexible upper watch footprint.
    for level in (-1, 1):
        _structure(region, seed, level, ground, reachable, protected)
    region.levels["0"] = ["".join(row) for row in ground]
    region.geography_signature = hashlib.sha256(
        f"{region.geography_signature}:landforms:{anchors}:{region.landmarks['field_upper']}:{region.landmarks['field_lower']}".encode()
    ).hexdigest()[:16]


def _event_position(state: GameState, anchor: Position) -> Position | None:
    from .world import is_walkable

    offsets = ((2, 0), (-2, 0), (0, 2), (0, -2), (1, 1), (-1, -1), (1, 0), (-1, 0))
    occupied = {actor.position for actor in state.combatants if actor.status in {"watching", "engaged"}}
    occupied.update(schedule.position for schedule in state.actor_schedules.values()
                    if schedule.area == f"region:{state.active_region_id}")
    for dx, dy in offsets:
        point = Position(anchor.x + dx, anchor.y + dy, anchor.z)
        if point == state.position or point in occupied or not is_walkable(state, point):
            continue
        return point
    return None


def _spawn_threat(state: GameState, anchor: Position, kind: str) -> str:
    from .content import ENEMY_ARCHETYPES
    from .encounters import threat_from_archetype
    from .enemy_equipment import issue_enemy_equipment

    limit = 1 if kind == "elite" else 2
    counter = f"landform:{kind}s"
    count = int(state.region.changes.get(counter, 0))
    if count >= limit:
        return "The old tracks cross this ground, but no new creature arrives."
    place = _event_position(state, anchor)
    if place is None:
        return "Something passes beyond the visible ground, without a safe approach."
    def candidate(data: dict[str, object]) -> bool:
        if data["region"] != state.active_region_id:
            return False
        if kind == "elite":
            return bool(data.get("elite"))
        if data.get("elite"):
            return False
        if kind == "beast":
            return data["profile"] == "animal"
        return data["profile"] != "animal" and data.get("ecology") in {"raider", "warden"}

    pool = [key for key, data in ENEMY_ARCHETYPES.items() if candidate(data)]
    if not pool and kind == "raider":
        pool = [key for key, data in ENEMY_ARCHETYPES.items()
                if data["region"] == state.active_region_id
                and data["profile"] != "animal" and not data.get("elite")]
    if not pool:
        return "A disturbed track fades before any creature takes it."
    archetype = stage_rng(state.seed, f"landform:{state.active_region_id}:{kind}:{count}").choice(sorted(pool))
    actor = threat_from_archetype(
        archetype, place, encounter_id=f"landform:{state.active_region_id}:{kind}:{count}",
        group=f"landform:{state.active_region_id}:{kind}",
    )
    actor.status = "watching"
    state.threats.append(actor)
    issue_enemy_equipment(state, actor, state.active_region_id)
    state.region.changes[f"enemy_kit:{actor.id}"] = 1
    state.region.changes[counter] = count + 1
    return f"A {actor.name} appears near {place.x},{place.y}; its {actor.goal} can be observed or avoided."


def _spawn_traveller(state: GameState, anchor: Position) -> str:
    from .state import ActorSchedule, Contact

    contact_id = f"landform:{state.active_region_id}:traveller"
    existing = next((person for person in state.contacts[state.active_region_id]
                     if person.id == contact_id), None)
    if existing:
        return f"{existing.name}'s previous route marks remain in this country."
    place = _event_position(state, anchor)
    if place is None:
        return "A traveller's call carries from beyond this crowded approach."
    name = VARIANTS[state.active_region_id]["traveller"]
    person = Contact(contact_id, name, "field traveller", 0,
                     [f"Knows the {state.region.generation_facts.get('landform:0', 'regional side route')}."],
                     state.region.objective_commodity, state.active_region_id, place)
    state.contacts[state.active_region_id].append(person)
    state.actor_schedules[contact_id] = ActorSchedule(
        contact_id, f"region:{state.active_region_id}", place, "surveying varied ground",
        state.world_time + 16, f"region:{state.active_region_id}", place,
        last_update=state.world_time,
    )
    return f"{name} takes a visible stand at {place.x},{place.y}; speak beside them with E for a route clue or one physical lot."


def approach(state: GameState) -> str:
    """One seeded, bounded field encounter per discovered pocket and expedition."""
    if state.location != "region" or state.position.z != 0:
        return ""
    for index in range(3):
        anchor = state.region.landmarks.get(f"landform_{index}")
        if anchor is None or max(abs(state.position.x - anchor.x), abs(state.position.y - anchor.y)) > 2:
            continue
        marker = f"landform:{index}:visit"
        if state.region.changes.get(marker) == state.expedition_count:
            continue
        state.region.changes[marker] = state.expedition_count
        rng = stage_rng(state.seed, f"landform:{state.active_region_id}:{index}:visit:{state.expedition_count}")
        pool = ("traveller", "beast", "raider", "stonefall", "elite") if index == 2 else (
            "traveller", "beast", "raider", "stonefall")
        kind = rng.choice(pool)
        state.region.changes[f"landform:{index}:last"] = kind
        if kind == "traveller":
            return _spawn_traveller(state, anchor)
        if kind in {"beast", "raider", "elite"}:
            return _spawn_threat(state, anchor, kind)
        from .materials import ensure_cell

        place = _event_position(state, anchor)
        cell = ensure_cell(state, place) if place else None
        if cell is not None:
            cell.material, cell.support = "stone", 0
            cell.collapse_due = state.world_time + 3
            return f"The {state.region.generation_facts.get(f'landform:{index}', 'landform')} shifts at {place.x},{place.y}; stone falls in three actions."
        return "The ground sounds unstable, but no exposed cell takes a new fracture."
    return ""


def enter_structure(state: GameState) -> str:
    if state.location != "region":
        return ""
    for key in ("field_upper", "field_lower"):
        anchor = state.region.landmarks.get(key)
        if state.position != anchor:
            continue
        marker = f"landform:{key}:visit"
        if state.region.changes.get(marker) == state.expedition_count:
            return ""
        state.region.changes[marker] = state.expedition_count
        rng = stage_rng(state.seed, f"landform:{state.active_region_id}:{key}:visit:{state.expedition_count}")
        kind = rng.choice(("raider", "elite", "traveller") if key == "field_upper"
                          else ("beast", "stonefall", "traveller"))
        state.region.changes[f"landform:{key}:last"] = kind
        if kind == "traveller":
            return _spawn_traveller(state, anchor)
        if kind != "stonefall":
            return _spawn_threat(state, anchor, kind)
        from .materials import ensure_cell

        place = _event_position(state, anchor)
        cell = ensure_cell(state, place) if place else None
        if cell:
            cell.material, cell.support, cell.collapse_due = "stone", 0, state.world_time + 3
            return f"Loose stone warns of a fall inside the {VARIANTS[state.active_region_id]['lower']} at {place.x},{place.y},z-1."
        return "The lower stone grinds without opening another fracture."
    return ""


def traveller_choice(state: GameState, choice: str):
    from .actions import _plain, _time_result
    from .inventory import auto_place, create_item
    from .world import position_key

    contact_id = f"landform:{state.active_region_id}:traveller"
    contact = next((person for person in state.contacts[state.active_region_id]
                    if person.id == contact_id), None)
    if contact is None:
        return _plain(state, "No field traveller is present here.")
    if choice == "a":
        if state.region.changes.get("landform:traveller:clue"):
            return _plain(state, "The traveller has already marked this route for your courier.")
        target = state.region.landmarks["field_upper"]
        if position_key(target) not in state.region.seen:
            state.region.seen.append(position_key(target))
        state.region.changes["landform:traveller:clue"] = True
        contact.memories.append(f"Marked {target.x},{target.y},z+1 for {state.courier.name}.")
        return _time_result(state, f"{contact.name} marks the {VARIANTS[state.active_region_id]['upper']} at {target.x},{target.y},z+1 as a known destination; the intervening ground still needs exploration.")
    if choice == "b":
        if state.region.changes.get("landform:traveller:lot"):
            return _plain(state, "This traveller has no second counted lot to sell.")
        if state.trade_credit < 1:
            return _plain(state, "One trade credit is needed for the physical lot.")
        item = create_item(state, f"commodity:{contact.interest}", f"purchased from {contact.name}")
        packed = auto_place(state, item.id, "pack", owner_id=state.active_courier_id)
        if not packed:
            item.location, item.region_id, item.ground_position = "ground", state.active_region_id, state.position
        state.trade_credit -= 1
        state.region.changes["landform:traveller:lot"] = True
        return _time_result(state, f"One credit buys a physical {contact.interest} lot; "
                            + ("it is packed." if packed else "the full pack leaves it on the ground beside you."))
    return _plain(state, "Ask for a route mark or buy one counted lot.")
