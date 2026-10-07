"""Seeded, persistent sanctums tied to the travelling work accounts."""

from __future__ import annotations

from collections import deque
import hashlib

from .catalog import CatalogError, load_catalog
from .sanctum_presentation import sanctum_boss_name, sanctum_display_name, sanctum_format, sanctum_text, sanctum_theme
from .state import Container, GameState, MaterialCell, Position, Region, Threat, VerticalLink, stage_rng


_catalog = load_catalog("sanctums.json", ("encounters", "sanctums"))
_rows = _catalog["sanctums"]
if not isinstance(_rows, dict) or len(_rows) != 8:
    raise CatalogError("sanctums.json needs one site in each region")
SITES = _rows
ENCOUNTERS = _catalog["encounters"]
if (not isinstance(ENCOUNTERS, dict) or set(ENCOUNTERS) != {"sheltered", "contested", "network", "open", "disputed"}
        or any(not isinstance(rows, list) or not rows or any(kind not in {
            "quiet_witnesses", "stray_claimant", "unstable_stone", "dropped_parcel",
            "travelling_witness", "wandering_beast", "elite_surveyor",
        } for kind in rows) for rows in ENCOUNTERS.values())):
    raise CatalogError("sanctums.json has an invalid encounter draw")
for region_id, row in SITES.items():
    boss = row.get("boss", {}) if isinstance(row, dict) else {}
    if (not isinstance(region_id, str) or not isinstance(row, dict)
            or set(row) != {"network", "boss"}
            or not isinstance(row["network"], str) or not row["network"]
            or not isinstance(boss, dict)
            or set(boss) != {"profile", "role", "goal", "duty", "health", "glyph", "capability", "capability_id", "counterplay_id"}
            or not all(isinstance(boss[key], str) and boss[key] for key in boss if key != "health")
            or boss["duty"] not in {"drain", "rally", "kindle", "cut support", "brace", "heal"}
            or type(boss["health"]) is not int or not 8 <= boss["health"] <= 16
            or len(boss["glyph"]) != 1):
        raise CatalogError(f"invalid sanctum definition for {region_id}")
_PASSABLE = frozenset(".=r")
_BLOCKED = frozenset(" #~T")

# Bundled-default compatibility only; selected-pack wording never recovers state identity.
LEGACY_EVENT_IDS = {
    "quiet witnesses": "quiet_witnesses", "stray claimant": "stray_claimant",
    "unstable stone": "unstable_stone", "dropped parcel": "dropped_parcel",
    "travelling witness": "travelling_witness", "wandering beast": "wandering_beast",
    "elite surveyor": "elite_surveyor",
}


def _link_id(region_id: str, kind: str) -> str:
    return f"sanctum:{region_id}:link:{kind}"


def _normalize_legacy_identities(region: Region) -> None:
    event = region.changes.get("sanctum:last_event")
    if event in LEGACY_EVENT_IDS:
        region.changes["sanctum:last_event"] = LEGACY_EVENT_IDS[event]
    entry = region.landmarks.get("sanctum_entry")
    if entry is None:
        return
    expected = {
        (entry, Position(entry.x, entry.y, 1)): "entry",
        (Position(entry.x + 10, entry.y - 3, 1), Position(entry.x + 10, entry.y - 3, 2)): "reliquary",
        (Position(entry.x + 9, entry.y + 3, 1), Position(entry.x + 9, entry.y + 3, 2)): "gallery",
    }
    for link in region.vertical_links:
        kind = expected.get((link.first, link.second)) or expected.get((link.second, link.first))
        if kind and not link.id:
            link.id = _link_id(region.id, kind)


def _tile(region: Region, point: Position) -> str:
    return region.tile_changes.get(
        f"{point.x},{point.y},{point.z}", region.levels[str(point.z)][point.y][point.x]
    )


def _local_ground(region: Region, start: Position) -> set[Position]:
    queue, seen = deque([start]), {start}
    while queue:
        point = queue.popleft()
        if abs(point.x - start.x) + abs(point.y - start.y) >= 16:
            continue
        for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0)):
            other = Position(point.x + dx, point.y + dy, point.z)
            if (other in seen or not 1 <= other.x < region.width - 1
                    or not 1 <= other.y < region.height - 1
                    or _tile(region, other) in _BLOCKED):
                continue
            seen.add(other)
            queue.append(other)
    return seen


def _site_location(region: Region, seed: str, occupied: tuple[Position, ...] = ()) -> Position:
    cave = region.landmarks["cave_entrance"]
    reachable = _local_ground(region, cave)
    forbidden = set(region.landmarks.values()) | {box.position for box in region.containers}
    forbidden.update(occupied)
    forbidden.update(point for link in region.vertical_links for point in (link.first, link.second))
    candidates = []
    for point in reachable:
        x, y = point.x, point.y
        shrine = Position(x + 1, y)
        if (not 3 <= abs(x - cave.x) + abs(y - cave.y) <= 14
                or x < 4 or x + 14 >= region.width or y < 7 or y + 7 >= region.height
                or _tile(region, point) not in _PASSABLE or _tile(region, shrine) not in _PASSABLE
                or any(max(abs(other.x - x), abs(other.y - y)) <= 2 and other.z == 0 for other in forbidden)):
            continue
        if any(
            region.levels[str(z)][row][col] != " " or f"{col},{row},{z}" in region.tile_changes
            for z in (1, 2) for row in range(y - 5, y + 6) for col in range(x - 2, x + 13)
        ):
            continue
        candidates.append(point)
    if not candidates:
        raise RuntimeError(f"{region.name} has no connected sanctum footprint")
    rng = stage_rng(seed, f"sanctum:{region.id}:placement")
    candidates.sort(key=lambda point: (point.y, point.x))
    rng.shuffle(candidates)
    return min(candidates, key=lambda point: abs(point.x - cave.x) + abs(point.y - cave.y))


def _undercroft(region: Region, seed: str) -> Position:
    cave = region.landmarks["cave_entrance"]
    start = Position(cave.x, cave.y, -1)
    forbidden = {box.position for box in region.containers}
    forbidden.update(point for link in region.vertical_links for point in (link.first, link.second))
    queue, seen = deque([(start, 0)]), {start}
    candidates = []
    while queue:
        point, distance = queue.popleft()
        if 2 <= distance <= 8 and point not in forbidden and _tile(region, point) != "C":
            candidates.append((distance, point))
        if distance >= 8:
            continue
        for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0)):
            other = Position(point.x + dx, point.y + dy, -1)
            if (other in seen or not 1 <= other.x < region.width - 1
                    or not 1 <= other.y < region.height - 1
                    or _tile(region, other) in _BLOCKED):
                continue
            seen.add(other)
            queue.append((other, distance + 1))
    if not candidates:
        raise RuntimeError(f"{region.name} has no accessible undercroft seal")
    rng = stage_rng(seed, f"sanctum:{region.id}:undercroft")
    farthest = max(distance for distance, _ in candidates)
    choices = [point for distance, point in candidates if distance == farthest]
    return rng.choice(sorted(choices, key=lambda p: (p.y, p.x)))


def _tier(region: Region, entry: Position, z: int, seed: str) -> tuple[Position, Position] | None:
    x, y = entry.x, entry.y
    rows = [list(row) for row in region.levels[str(z)]]
    for row in range(y - 5, y + 6):
        for col in range(x - 2, x + 13):
            rows[row][col] = "#" if row in {y - 5, y + 5} or col in {x - 2, x + 12} else "."
    rng = stage_rng(seed, f"sanctum:{region.id}:tier:{z}")
    columns = ((x - 1, x + 2), (x + 4, x + 6), (x + 8, x + 11))
    bands = ((y - 4, y - 3), (y - 1, y + 1), (y + 3, y + 4))
    for wall_x in (x + 3, x + 7):
        for row in range(y - 4, y + 5):
            rows[row][wall_x] = "#"
    for wall_y in (y - 2, y + 2):
        for col in range(x - 1, x + 12):
            rows[wall_y][col] = "#"

    # A randomized spanning tree guarantees every chamber is reachable; one
    # extra doorway and a discoverable seam make routing a choice, not a maze trap.
    visited = {(0, 1)}
    frontier = [(0, 1)]
    opened: set[tuple[tuple[int, int], tuple[int, int]]] = set()
    while frontier:
        current = frontier[-1]
        neighbours = [(current[0] + dx, current[1] + dy)
                      for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))]
        unvisited = [node for node in neighbours if 0 <= node[0] < 3 and 0 <= node[1] < 3 and node not in visited]
        if not unvisited:
            frontier.pop()
            continue
        next_node = rng.choice(unvisited)
        opened.add(tuple(sorted((current, next_node))))
        visited.add(next_node)
        frontier.append(next_node)
    edges = [tuple(sorted(((col, row), (col + 1, row))))
             for row in range(3) for col in range(2)]
    edges += [tuple(sorted(((col, row), (col, row + 1))))
              for row in range(2) for col in range(3)]
    unopened = [edge for edge in edges if edge not in opened]
    rng.shuffle(unopened)
    opened.add(unopened.pop())

    def doorway(edge: tuple[tuple[int, int], tuple[int, int]]) -> tuple[int, int, int, int]:
        first, second = edge
        if first[1] == second[1]:
            wall_x = x + (3 if min(first[0], second[0]) == 0 else 7)
            low, high = bands[first[1]]
            door_y = rng.randint(low, high)
            return wall_x, door_y, wall_x - 1, door_y
        wall_y = y + (-2 if min(first[1], second[1]) == 0 else 2)
        low, high = columns[first[0]]
        door_x = rng.randint(low, high)
        return door_x, wall_y, door_x, wall_y - 1

    for edge in sorted(opened):
        door_x, door_y, _, _ = doorway(edge)
        rows[door_y][door_x] = "+"
    secret = None
    if z == 1 and unopened:
        door_x, door_y, marker_x, marker_y = doorway(unopened[0])
        rows[marker_y][marker_x] = "*"
        secret = Position(marker_x, marker_y, z), Position(door_x, door_y, z)
    region.levels[str(z)] = ["".join(row) for row in rows]
    return secret


def install(region: Region, seed: str, *, occupied: tuple[Position, ...] = ()) -> None:
    """Add only new geography; old region changes, containers and claims survive."""
    if region.id not in SITES:
        return
    _normalize_legacy_identities(region)
    old_door = region.landmarks.pop("sanctum_secret_door", None)
    if old_door is not None:
        region.changes.setdefault("sanctum:secret_door", f"{old_door.x},{old_door.y},{old_door.z}")
    if "sanctum_entry" in region.landmarks:
        return
    entry = _site_location(region, seed, occupied)
    lower = _undercroft(region, seed)
    x, y = entry.x, entry.y
    secret = _tier(region, entry, 1, seed)
    _tier(region, entry, 2, seed)
    upper_stair = Position(x + 10, y - 3, 1)
    side_stair = Position(x + 9, y + 3, 1)
    boss = Position(x + 1, y + 3, 2)
    hoard = Position(x + 1, y - 3, 2)
    ward = Position(x + 9, y, 1)
    region.landmarks.update({
        "sanctum_entry": entry, "sanctum_shrine": Position(x + 1, y),
        "sanctum_undercroft": lower, "sanctum_ward": ward,
        "sanctum_boss": boss, "sanctum_side_stair": side_stair,
    })
    if secret:
        marker, door = secret
        region.landmarks["sanctum_secret"] = marker
        region.changes["sanctum:secret_door"] = f"{door.x},{door.y},{door.z}"
    region.tile_changes[f"{x},{y},0"] = ">"
    region.tile_changes[f"{x + 1},{y},0"] = "*"
    region.tile_changes[f"{lower.x},{lower.y},-1"] = "*"
    region.tile_changes[f"{x},{y},1"] = "<"
    region.tile_changes[f"{upper_stair.x},{upper_stair.y},1"] = ">"
    region.tile_changes[f"{upper_stair.x},{upper_stair.y},2"] = "<"
    region.tile_changes[f"{side_stair.x},{side_stair.y},1"] = ">"
    region.tile_changes[f"{side_stair.x},{side_stair.y},2"] = "<"
    region.tile_changes[f"{hoard.x},{hoard.y},2"] = "C"
    region.vertical_links.extend((
        VerticalLink(entry, Position(x, y, 1), sanctum_text("sanctum.link.entry"), _link_id(region.id, "entry")),
        VerticalLink(upper_stair, Position(upper_stair.x, upper_stair.y, 2), sanctum_text("sanctum.link.reliquary"), _link_id(region.id, "reliquary")),
        VerticalLink(side_stair, Position(side_stair.x, side_stair.y, 2), sanctum_text("sanctum.link.gallery"), _link_id(region.id, "gallery")),
    ))
    from .expanded_weapons import ARSENAL
    from .content import PASSIVES

    arms = [name for name, arm in ARSENAL.items() if region.id in arm.regions]
    if not arms:
        arms = ["spear"]
    rewards = list(dict.fromkeys(box.reward for box in region.containers if box.reward in PASSIVES))
    if len(rewards) < 2:
        rewards = list(PASSIVES)
    rng = stage_rng(seed, f"sanctum:{region.id}:rewards")
    rng.shuffle(rewards)
    region.containers.extend((
        Container(f"{region.id}-sanctum-ward", sanctum_format("sanctum.cache.ward", sanctum=sanctum_display_name(region.id)),
                  Position(x + 1, y + 3, 1), rewards[0], "key",
                  extra_rewards=[rng.choice(arms), "willow dressing"]),
        Container(f"{region.id}-sanctum-hoard", sanctum_format("sanctum.cache.hoard", sanctum=sanctum_display_name(region.id)),
                  hoard, rewards[1], "light",
                  extra_rewards=[rng.choice(arms), "sealed tally"]),
    ))
    region.tile_changes[f"{x + 1},{y + 3},1"] = "C"
    region.geography_signature = hashlib.sha256(
        f"{region.geography_signature}:sanctum:{entry.x},{entry.y}".encode()
    ).hexdigest()[:16]


def _network(state: GameState):
    return state.institutions.get(SITES[state.active_region_id]["network"])


def _regional_guard(state: GameState, point: Position, suffix: str) -> Threat:
    from .content import ENEMY_ARCHETYPES
    from .encounters import threat_from_archetype

    pool = [key for key, data in ENEMY_ARCHETYPES.items()
            if data["region"] == state.active_region_id and not data.get("elite")
            and data["profile"] not in {"animal", "machinery"}]
    archetype = stage_rng(state.seed, f"sanctum:{state.active_region_id}:{suffix}").choice(sorted(pool))
    return threat_from_archetype(
        archetype, point, encounter_id=f"sanctum:{state.active_region_id}:{suffix}",
        group=f"sanctum:{state.active_region_id}",
    )


def _add_actor(state: GameState, actor: Threat) -> None:
    from .enemy_equipment import issue_enemy_equipment

    if any(existing.id == actor.id for existing in state.threats):
        return
    state.threats.append(actor)
    issue_enemy_equipment(state, actor, state.active_region_id)
    state.region.changes[f"enemy_kit:{actor.id}"] = 1


def _approach_place(state: GameState, shrine: Position) -> Position | None:
    from .world import is_walkable

    candidates = [Position(shrine.x + dx, shrine.y + dy, 0)
                  for dx, dy in ((3, 0), (-3, 0), (0, 3), (0, -3), (2, 2), (-2, -2), (2, -2), (-2, 2))]
    occupied = {actor.position for actor in state.combatants if actor.status in {"watching", "engaged"}}
    occupied.update(schedule.position for schedule in state.actor_schedules.values()
                    if schedule.area == f"region:{state.active_region_id}")
    return next((point for point in candidates
                 if point not in occupied and point != state.position
                 and _tile(state.region, point) in _PASSABLE and is_walkable(state, point)), None)


def _witness(state: GameState, shrine: Position) -> str:
    from .state import ActorSchedule, Contact

    contact_id = f"sanctum:{state.active_region_id}:witness"
    existing = next((contact for contact in state.contacts[state.active_region_id] if contact.id == contact_id), None)
    if existing:
        return sanctum_format("sanctum.witness.existing", witness=existing.name)
    place = _approach_place(state, shrine)
    if place is None:
        return sanctum_text("sanctum.witness.unavailable")
    region_id = state.active_region_id
    name = sanctum_text(f"sanctum.{region_id}.witness.name")
    contact = Contact(contact_id, name, sanctum_text("sanctum.witness.role"), 0,
                      [sanctum_format("sanctum.witness.memory", control=state.region.changes.get("sanctum:control", sanctum_text("sanctum.control.unsettled")), sanctum=sanctum_display_name(region_id))],
                      state.institutions[SITES[region_id]["network"]].dependency, region_id, place)
    state.contacts[region_id].append(contact)
    state.actor_schedules[contact_id] = ActorSchedule(contact_id, f"region:{region_id}", place,
        sanctum_text("sanctum.witness.schedule"), state.world_time + 16, f"region:{region_id}", place, last_update=state.world_time)
    return sanctum_format("sanctum.witness.arrival", witness=name, x=place.x, y=place.y)


def _site_encounter(state: GameState, shrine: Position, kind: str) -> str:
    from .content import ENEMY_ARCHETYPES
    kind = LEGACY_EVENT_IDS.get(kind, kind)
    from .encounters import threat_from_archetype

    limit = 1 if kind == "elite_surveyor" else 2 if kind == "wandering_beast" else 3
    counter = "elites" if kind == "elite_surveyor" else "beasts" if kind == "wandering_beast" else "claimants"
    count = int(state.region.changes.get(f"sanctum:{counter}", 0))
    if count >= limit:
        return sanctum_text("sanctum.encounter.limit")
    place = _approach_place(state, shrine)
    if place is None:
        return sanctum_text("sanctum.encounter.occupied")
    if kind == "stray_claimant":
        actor = _regional_guard(state, place, f"claimant:{count + 1}")
        actor.allegiance = f"claim:{state.active_region_id}"
    else:
        pool = [key for key, data in ENEMY_ARCHETYPES.items() if data["region"] == state.active_region_id and (bool(data.get("elite")) if kind == "elite_surveyor" else data["profile"] == "animal" and not data.get("elite"))]
        if not pool:
            return sanctum_text("sanctum.encounter.none")
        archetype = stage_rng(state.seed, f"sanctum:{state.active_region_id}:{kind}:{count + 1}").choice(sorted(pool))
        actor = threat_from_archetype(archetype, place, encounter_id=f"sanctum:{state.active_region_id}:{counter}:{count + 1}", group=f"sanctum:{state.active_region_id}:{counter}")
        if kind == "elite_surveyor":
            actor.allegiance = f"claim:{state.active_region_id}"
    actor.status = "watching"
    _add_actor(state, actor)
    state.region.changes[f"sanctum:{counter}"] = count + 1
    if state.region.changes.get("sanctum:cleared") and kind != "wandering_beast":
        state.region.changes["sanctum:control"] = "disputed"
    return sanctum_format("sanctum.encounter.arrival", actor=actor.name, x=place.x, y=place.y, goal=actor.goal)


def _active_claimants(state: GameState) -> list[Threat]:
    prefix = f"sanctum:{state.active_region_id}:"
    return [actor for actor in state.combatants if (actor.id.startswith(prefix + "claimant:") or actor.id.startswith(prefix + "elites:")) and actor.status in {"watching", "engaged"}]


def enter_tier(state: GameState, destination: Position) -> str:
    if destination.z == 1 and destination == Position(state.region.landmarks["sanctum_entry"].x, state.region.landmarks["sanctum_entry"].y, 1):
        if not state.region.changes.get("sanctum:inhabited"):
            ward = _regional_guard(state, state.region.landmarks["sanctum_ward"], "ward")
            ward.status = "watching"; _add_actor(state, ward)
            region_id = state.active_region_id; boss_row = SITES[region_id]["boss"]; point = state.region.landmarks["sanctum_boss"]
            boss = Threat(id=f"sanctum:{region_id}:boss", name=sanctum_boss_name(region_id), profile=boss_row["profile"], position=point, health=boss_row["health"], max_health=boss_row["health"], archetype_id="", morale=5, elite=True, role=boss_row["role"], goal=boss_row["goal"], goal_reason=boss_row["capability_id"], region_id=region_id, home_position=point, group=f"sanctum:{region_id}", allegiance=f"relic:{region_id}", vision=10, hearing=9, ammunition=8 if boss_row["profile"] == "ranged" else 0, glyph=boss_row["glyph"], duty=boss_row["duty"], supplies=3, capabilities=[boss_row["capability"]], objective_position=Position(point.x + 1, point.y, point.z))
            material = boss.objective_position; key = f"{material.x},{material.y},{material.z}"
            if boss.duty == "drain": state.region.materials[key] = MaterialCell(material="stone", water=3)
            elif boss.duty == "kindle": state.region.materials[key] = MaterialCell(material="timber", fuel=5)
            elif boss.duty == "cut support": state.region.materials[key] = MaterialCell(material="timber", support=2)
            elif boss.duty == "brace": state.region.materials[key] = MaterialCell(material="timber", support=1)
            _add_actor(state, boss)
            if boss.duty in {"rally", "heal", "brace"}:
                escort = _regional_guard(state, boss.objective_position, "escort"); escort.status = "watching"; _add_actor(state, escort)
            state.region.changes["sanctum:inhabited"] = True
        return sanctum_format("sanctum.enter.tier", sanctum=sanctum_display_name(state.active_region_id))
    return ""


def undercroft(state: GameState) -> tuple[bool, str]:
    if state.position != state.region.landmarks.get("sanctum_undercroft"): return False, ""
    if state.region.changes.get("sanctum:unsealed"): return False, sanctum_text("sanctum.undercroft.already")
    guard_id = f"sanctum:{state.active_region_id}:lower"; guard = next((actor for actor in state.threats if actor.id == guard_id), None)
    if guard is None:
        point = state.position
        from .world import is_walkable
        adjacent = [Position(point.x + dx, point.y + dy, -1) for dx, dy in ((0,-1),(1,0),(0,1),(-1,0))]
        place = next((other for other in adjacent if _tile(state.region, other) in _PASSABLE and is_walkable(state, other)), None)
        if place is None: return False, sanctum_text("sanctum.undercroft.no_space")
        guard = _regional_guard(state, place, "lower"); guard.id = guard_id; guard.status = "watching"; _add_actor(state, guard)
        return True, sanctum_text("sanctum.undercroft.spawned")
    if guard.status in {"watching", "engaged"}: return False, sanctum_text("sanctum.undercroft.contested")
    state.region.changes["sanctum:unsealed"] = True; state.region.changes["sanctum:opened_by"] = "undercroft"
    return True, sanctum_text("sanctum.undercroft.opened")


def open_secret(state: GameState) -> tuple[bool, str]:
    marker = state.region.landmarks.get("sanctum_secret"); door = state.region.changes.get("sanctum:secret_door")
    if state.position != marker or not isinstance(door, str): return False, sanctum_text("sanctum.secret.none")
    if state.region.changes.get("sanctum:secret_open"): return False, sanctum_text("sanctum.secret.open")
    state.region.tile_changes[door] = "+"; state.region.changes["sanctum:secret_open"] = True
    return True, sanctum_text("sanctum.secret.opened")


def inspect_lines(state: GameState) -> list[str]:
    region_id = state.active_region_id; row = SITES[region_id]; account = _network(state)
    boss = next((actor for actor in state.threats if actor.id == f"sanctum:{region_id}:boss"), None)
    standing = sanctum_format("sanctum.inspect.standing", account=account.name, trust=f"{account.trust:+d}", obligation=account.obligation) if account else sanctum_text("sanctum.inspect.no_witness")
    lines = [sanctum_format("sanctum.inspect.site", sanctum=sanctum_display_name(region_id), theme=sanctum_theme(region_id)), standing,
      sanctum_format("sanctum.inspect.seal", seal=sanctum_text("sanctum.status.seal.open") if state.region.changes.get("sanctum:unsealed") else sanctum_text("sanctum.status.seal.closed"), keeper=sanctum_text("sanctum.status.keeper.defeated") if boss and boss.status == "defeated" else sanctum_text("sanctum.status.keeper.unseen") if boss is None else sanctum_text("sanctum.status.keeper.present")),
      sanctum_format("sanctum.inspect.keeper", capability=sanctum_text(row["boss"]["capability_id"]), counterplay=sanctum_text(row["boss"]["counterplay_id"])), sanctum_text("sanctum.inspect.offer"), sanctum_text("sanctum.inspect.breach"), sanctum_text("sanctum.inspect.study")]
    marker = state.region.landmarks.get("sanctum_secret")
    if marker: lines.append(sanctum_format("sanctum.inspect.secret", x=marker.x, y=marker.y))
    control = state.region.changes.get("sanctum:control")
    if control: lines += [sanctum_format("sanctum.inspect.aftermath", control=control), sanctum_text("sanctum.inspect.shelter")]
    return lines


def shrine_choice(state: GameState, choice: str) -> tuple[bool, str, int]:
    from .inventory import consume_carried
    region_id = state.active_region_id; account = _network(state)
    if choice == "s": return False, sanctum_format("sanctum.choice.study", theme=sanctum_theme(region_id), event=state.region.changes.get("sanctum:last_event", sanctum_text("sanctum.event.none"))), 0
    if account is None: return False, sanctum_text("sanctum.choice.account_unavailable"), 0
    if choice == "h":
        if not state.region.changes.get("sanctum:cleared") or state.region.changes.get("sanctum:control") != "network": return False, sanctum_text("sanctum.choice.shelter_unavailable"), 0
        from .regional_history import NETWORK_CONTACTS, open_network_shelter
        contact_id = next((contact_id for network_id, rid, contact_id, *_ in NETWORK_CONTACTS if network_id == account.id and rid == region_id), None)
        if contact_id is None: return False, sanctum_text("sanctum.choice.shelter_witness"), 0
        changed, message = open_network_shelter(state, contact_id); return changed, message, 2 if changed else 0
    if choice == "o":
        if state.region.changes.get("sanctum:offered"): return False, sanctum_text("sanctum.choice.offered"), 0
        if not consume_carried(state, f"commodity:{account.dependency}"): return False, sanctum_format("sanctum.choice.requirement", commodity=account.dependency), 0
        state.region.changes["sanctum:offered"] = True; state.region.changes["sanctum:unsealed"] = True; state.region.changes["sanctum:opened_by"] = "offering"
        if state.region.changes.get("sanctum:cleared"): state.region.changes["sanctum:control"] = "disputed" if _active_claimants(state) else "network"
        account.trust = min(3, account.trust + 1); account.obligation = max(0, account.obligation - 1)
        account.witnessed_acts.append(sanctum_format("sanctum.choice.offering_memory", courier=state.courier.name, commodity=account.dependency, sanctum=sanctum_display_name(region_id))); del account.witnessed_acts[:-8]
        return True, sanctum_format("sanctum.choice.offering", commodity=account.dependency, account=account.name), 2
    if choice == "b":
        if state.region.changes.get("sanctum:unsealed"): return False, sanctum_text("sanctum.choice.breach_unavailable"), 0
        state.region.changes["sanctum:unsealed"] = True; state.region.changes["sanctum:opened_by"] = "breach"; account.trust = max(-3, account.trust - 1); account.obligation = min(9, account.obligation + 1)
        account.witnessed_acts.append(sanctum_format("sanctum.choice.breach_memory", courier=state.courier.name, sanctum=sanctum_display_name(region_id))); del account.witnessed_acts[:-8]
        return True, sanctum_format("sanctum.choice.breach", account=account.name), 2
    return False, sanctum_text("sanctum.choice.invalid"), 0


def approach(state: GameState) -> str:
    if state.location != "region" or "sanctum_shrine" not in state.region.landmarks: return ""
    shrine = state.region.landmarks["sanctum_shrine"]
    if state.position.z != 0 or max(abs(state.position.x-shrine.x),abs(state.position.y-shrine.y)) > 4: return ""
    if state.region.changes.get("sanctum:event_visit") == state.expedition_count: return ""
    state.region.changes["sanctum:event_visit"] = state.expedition_count; account = _network(state); trust = account.trust if account else 0; control = state.region.changes.get("sanctum:control")
    options = ENCOUNTERS[control if control in {"network","open","disputed"} else "sheltered" if trust >= 1 else "contested"]
    kind = stage_rng(state.seed, f"sanctum:{state.active_region_id}:visit:{state.expedition_count}").choice(options)
    if kind == "dropped_parcel" and int(state.region.changes.get("sanctum:parcels",0)) >= 2: kind = "quiet_witnesses"
    state.region.changes["sanctum:last_event"] = kind
    if kind == "quiet_witnesses": return sanctum_format("sanctum.event.quiet_witnesses", sanctum=sanctum_display_name(state.active_region_id))
    if kind in {"stray_claimant","wandering_beast","elite_surveyor"}: return _site_encounter(state, shrine, kind)
    if kind == "travelling_witness": return _witness(state, shrine)
    if kind == "dropped_parcel":
        from .inventory import create_item
        from .world import is_walkable
        candidates=[Position(shrine.x+dx,shrine.y+dy,0) for dx,dy in ((2,1),(-2,1),(1,-2),(-1,-2))]
        place=next((point for point in candidates if _tile(state.region,point) in _PASSABLE and is_walkable(state,point) and not any(item.location=="ground" and item.region_id==state.active_region_id and item.ground_position==point for item in state.items)),None)
        if place is None or account is None: return sanctum_text("sanctum.event.parcel_unavailable")
        item=create_item(state,f"commodity:{account.dependency}",sanctum_format("sanctum.event.parcel_provenance",sanctum=sanctum_display_name(state.active_region_id)),location="ground"); item.region_id,item.ground_position=state.active_region_id,place
        state.region.changes["sanctum:parcels"]=int(state.region.changes.get("sanctum:parcels",0))+1
        return sanctum_format("sanctum.event.parcel",commodity=account.dependency,x=place.x,y=place.y)
    from .materials import ensure_cell
    point=Position(shrine.x+2,shrine.y,0); cell=ensure_cell(state,point)
    if cell:
        cell.material,cell.support="stone",0; cell.collapse_due=state.world_time+3; return sanctum_text("sanctum.event.stone")
    return sanctum_text("sanctum.event.stone_safe")


def record_boss_defeat(state: GameState, actor: Threat) -> None:
    if actor.id != f"sanctum:{state.active_region_id}:boss" or state.region.changes.get("sanctum:cleared"): return
    state.region.changes["sanctum:cleared"] = True; opening=state.region.changes.get("sanctum:opened_by")
    state.region.changes["sanctum:control"]="disputed" if _active_claimants(state) else "network" if opening=="offering" else "disputed" if opening=="breach" else "open"; state.trade_credit += 4
    strategy_gained=False
    if state.courier: strategy_gained=state.courier.strategy<20; state.courier.strategy=min(20,state.courier.strategy+1)
    record=sanctum_format("sanctum.record.defeat",courier=state.courier.name if state.courier else sanctum_text("sanctum.record.courier.unknown"),boss=actor.name,sanctum=sanctum_display_name(state.active_region_id));state.remember(record)
    strategy_note=sanctum_text("sanctum.record.strategy_gained") if strategy_gained else sanctum_text("sanctum.record.strategy_bound") if state.courier else sanctum_text("sanctum.record.strategy_none")
    state.add_message(sanctum_format("sanctum.record.cleared",sanctum=sanctum_display_name(state.active_region_id),strategy=strategy_note),priority=3)
    from .state import append_narrative_record
    append_narrative_record(state,event_id="sanctum.resolved",refs={"region_id":state.active_region_id,"boss_id":actor.id,"control_id":str(state.region.changes["sanctum:control"])},params={"world_time":state.world_time,"credit":4},rendered=record)


def record_site_defeat(state: GameState, actor: Threat) -> None:
    record_boss_defeat(state,actor)
    if not state.region.changes.get("sanctum:cleared") or state.region.changes.get("sanctum:control") != "disputed": return
    prefix=f"sanctum:{state.active_region_id}:"
    if not (actor.id.startswith(prefix+"claimant:") or actor.id.startswith(prefix+"elites:")) or _active_claimants(state): return
    state.region.changes["sanctum:control"]="network" if state.region.changes.get("sanctum:opened_by")=="offering" else "open"
    state.add_message(sanctum_format("sanctum.record.claimants_left",sanctum=sanctum_display_name(state.active_region_id),control=state.region.changes["sanctum:control"]),priority=2)
