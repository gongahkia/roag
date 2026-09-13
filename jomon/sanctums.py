"""Seeded, persistent sanctums tied to the travelling work accounts."""

from __future__ import annotations

from collections import deque
import hashlib

from .catalog import CatalogError, load_catalog
from .state import Container, GameState, MaterialCell, Position, Region, Threat, VerticalLink, stage_rng


_catalog = load_catalog("sanctums.json", ("encounters", "sanctums"))
_rows = _catalog["sanctums"]
if not isinstance(_rows, dict) or len(_rows) != 8:
    raise CatalogError("sanctums.json needs one site in each region")
SITES = _rows
ENCOUNTERS = _catalog["encounters"]
if (not isinstance(ENCOUNTERS, dict) or set(ENCOUNTERS) != {"sheltered", "contested", "network", "open", "disputed"}
        or any(not isinstance(rows, list) or not rows or any(kind not in {
            "quiet witnesses", "stray claimant", "unstable stone", "dropped parcel",
            "travelling witness", "wandering beast", "elite surveyor",
        } for kind in rows) for rows in ENCOUNTERS.values())):
    raise CatalogError("sanctums.json has an invalid encounter draw")
for region_id, row in SITES.items():
    boss = row.get("boss", {}) if isinstance(row, dict) else {}
    if (not isinstance(region_id, str) or not isinstance(row, dict)
            or set(row) != {"name", "theme", "network", "witness", "boss"}
            or not all(isinstance(row[key], str) and row[key] for key in ("name", "theme", "network", "witness"))
            or not isinstance(boss, dict)
            or set(boss) != {"name", "profile", "role", "goal", "duty", "health", "glyph", "capability", "counterplay"}
            or not all(isinstance(boss[key], str) and boss[key] for key in boss if key != "health")
            or boss["duty"] not in {"drain", "rally", "kindle", "cut support", "brace", "heal"}
            or type(boss["health"]) is not int or not 8 <= boss["health"] <= 16
            or len(boss["glyph"]) != 1):
        raise CatalogError(f"invalid sanctum definition for {region_id}")
_PASSABLE = frozenset(".=r")
_BLOCKED = frozenset(" #~T")


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


def _site_location(region: Region, seed: str) -> Position:
    cave = region.landmarks["cave_entrance"]
    reachable = _local_ground(region, cave)
    forbidden = set(region.landmarks.values()) | {box.position for box in region.containers}
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


def install(region: Region, seed: str) -> None:
    """Add only new geography; old region changes, containers and claims survive."""
    if region.id not in SITES or "sanctum_entry" in region.landmarks:
        return
    entry = _site_location(region, seed)
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
        region.landmarks["sanctum_secret"], region.landmarks["sanctum_secret_door"] = secret
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
        VerticalLink(entry, Position(x, y, 1), "sanctum stair"),
        VerticalLink(upper_stair, Position(upper_stair.x, upper_stair.y, 2), "reliquary stair"),
        VerticalLink(side_stair, Position(side_stair.x, side_stair.y, 2), "side gallery stair"),
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
        Container(f"{region.id}-sanctum-ward", f"{SITES[region.id]['name']} ward chest",
                  Position(x + 1, y + 3, 1), rewards[0], "key",
                  extra_rewards=[rng.choice(arms), "willow dressing"]),
        Container(f"{region.id}-sanctum-hoard", f"{SITES[region.id]['name']} reliquary",
                  hoard, rewards[1], "light",
                  extra_rewards=[rng.choice(arms), "sealed tally"]),
    ))
    region.tile_changes[f"{x + 1},{y + 3},1"] = "C"
    region.geography_signature = hashlib.sha256(
        f"{region.geography_signature}:sanctum:{entry.x},{entry.y}".encode()
    ).hexdigest()[:16]
    from .landscape_variation import install as install_landforms

    install_landforms(region, seed)


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
    existing = next((contact for contact in state.contacts[state.active_region_id]
                     if contact.id == contact_id), None)
    if existing:
        return f"{existing.name} still keeps a physical witness position by the shrine; speak from beside them with E."
    place = _approach_place(state, shrine)
    if place is None:
        return "A travelling witness reached the site, but the shrine's working ground is occupied."
    name = SITES[state.active_region_id]["witness"]
    contact = Contact(contact_id, name, "sanctum witness", 0,
                      [f"Saw the {state.region.changes.get('sanctum:control', 'unsettled')} holding at {SITES[state.active_region_id]['name']}."],
                      state.institutions[SITES[state.active_region_id]["network"]].dependency,
                      state.active_region_id, place)
    state.contacts[state.active_region_id].append(contact)
    state.actor_schedules[contact_id] = ActorSchedule(
        contact_id, f"region:{state.active_region_id}", place, "keeping the sanctum account",
        state.world_time + 16, f"region:{state.active_region_id}", place,
        last_update=state.world_time,
    )
    return f"{name} arrives at {place.x},{place.y} to record the site. Speak beside them with E."


def _site_encounter(state: GameState, shrine: Position, kind: str) -> str:
    from .content import ENEMY_ARCHETYPES
    from .encounters import threat_from_archetype

    limit = 1 if kind == "elite surveyor" else 2 if kind == "wandering beast" else 3
    counter = "elites" if kind == "elite surveyor" else "beasts" if kind == "wandering beast" else "claimants"
    count = int(state.region.changes.get(f"sanctum:{counter}", 0))
    if count >= limit:
        return "Old tracks cross the gallery, but no new party arrives."
    place = _approach_place(state, shrine)
    if place is None:
        return "A fresh trail reaches the shrine, but no creature can take the occupied approach."
    if kind == "stray claimant":
        actor = _regional_guard(state, place, f"claimant:{count + 1}")
        actor.allegiance = f"claim:{state.active_region_id}"
    else:
        pool = [key for key, data in ENEMY_ARCHETYPES.items()
                if data["region"] == state.active_region_id
                and (bool(data.get("elite")) if kind == "elite surveyor"
                     else data["profile"] == "animal" and not data.get("elite"))]
        if not pool:
            return "The tracks leave the site without a creature to follow them."
        archetype = stage_rng(state.seed, f"sanctum:{state.active_region_id}:{kind}:{count + 1}").choice(sorted(pool))
        actor = threat_from_archetype(
            archetype, place, encounter_id=f"sanctum:{state.active_region_id}:{counter}:{count + 1}",
            group=f"sanctum:{state.active_region_id}:{counter}",
        )
        if kind == "elite surveyor":
            actor.allegiance = f"claim:{state.active_region_id}"
    actor.status = "watching"
    _add_actor(state, actor)
    state.region.changes[f"sanctum:{counter}"] = count + 1
    if state.region.changes.get("sanctum:cleared") and kind != "wandering beast":
        state.region.changes["sanctum:control"] = "disputed"
    return f"A {actor.name} takes the approach at {place.x},{place.y}; its {actor.goal} is visible before contact."


def _active_claimants(state: GameState) -> list[Threat]:
    prefix = f"sanctum:{state.active_region_id}:"
    return [actor for actor in state.combatants
            if (actor.id.startswith(prefix + "claimant:") or actor.id.startswith(prefix + "elites:"))
            and actor.status in {"watching", "engaged"}]


def enter_tier(state: GameState, destination: Position) -> str:
    if destination.z == 1 and destination == Position(
        state.region.landmarks["sanctum_entry"].x,
        state.region.landmarks["sanctum_entry"].y, 1,
    ):
        if not state.region.changes.get("sanctum:inhabited"):
            ward = _regional_guard(state, state.region.landmarks["sanctum_ward"], "ward")
            ward.status = "watching"
            _add_actor(state, ward)
            boss_row = SITES[state.active_region_id]["boss"]
            point = state.region.landmarks["sanctum_boss"]
            boss = Threat(
                id=f"sanctum:{state.active_region_id}:boss", name=boss_row["name"],
                profile=boss_row["profile"], position=point,
                health=boss_row["health"], max_health=boss_row["health"],
                morale=5, elite=True, role=boss_row["role"], goal=boss_row["goal"],
                goal_reason=boss_row["capability"], region_id=state.active_region_id,
                home_position=point, group=f"sanctum:{state.active_region_id}",
                allegiance=f"relic:{state.active_region_id}", vision=10, hearing=9,
                ammunition=8 if boss_row["profile"] == "ranged" else 0,
                glyph=boss_row["glyph"], duty=boss_row["duty"], supplies=3,
                capabilities=[boss_row["capability"]],
                objective_position=Position(point.x + 1, point.y, point.z),
            )
            material = boss.objective_position
            key = f"{material.x},{material.y},{material.z}"
            if boss.duty == "drain":
                state.region.materials[key] = MaterialCell(material="stone", water=3)
            elif boss.duty == "kindle":
                state.region.materials[key] = MaterialCell(material="timber", fuel=5)
            elif boss.duty == "cut support":
                state.region.materials[key] = MaterialCell(material="timber", support=2)
            elif boss.duty == "brace":
                state.region.materials[key] = MaterialCell(material="timber", support=1)
            _add_actor(state, boss)
            if boss.duty in {"rally", "heal", "brace"}:
                escort = _regional_guard(
                    state, boss.objective_position, "escort"
                )
                escort.status = "watching"
                _add_actor(state, escort)
            state.region.changes["sanctum:inhabited"] = True
        return f"{SITES[state.active_region_id]['name']} opens in divided tiers. A warder and named keeper hold the upper route."
    return ""


def undercroft(state: GameState) -> tuple[bool, str]:
    if state.position != state.region.landmarks.get("sanctum_undercroft"):
        return False, ""
    if state.region.changes.get("sanctum:unsealed"):
        return False, "The undercroft seal has already been carried upstairs."
    guard_id = f"sanctum:{state.active_region_id}:lower"
    guard = next((actor for actor in state.threats if actor.id == guard_id), None)
    if guard is None:
        point = state.position
        from .world import is_walkable

        adjacent = [Position(point.x + dx, point.y + dy, -1)
                    for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0))]
        place = next((other for other in adjacent if _tile(state.region, other) in _PASSABLE and is_walkable(state, other)), None)
        if place is None:
            return False, "The old seal is watched, but the cave gives no space for the warder to emerge."
        guard = _regional_guard(state, place, "lower")
        guard.id = guard_id
        guard.status = "watching"
        _add_actor(state, guard)
        return True, "A buried warder answers the inscription. Defeat or evade it, then read the seal again."
    if guard.status in {"watching", "engaged"}:
        return False, "The buried warder still contests the inscription."
    state.region.changes["sanctum:unsealed"] = True
    state.region.changes["sanctum:opened_by"] = "undercroft"
    return True, "You copy the undercroft measure; the sanctum stair's seal releases."


def open_secret(state: GameState) -> tuple[bool, str]:
    marker = state.region.landmarks.get("sanctum_secret")
    door = state.region.landmarks.get("sanctum_secret_door")
    if state.position != marker or door is None:
        return False, "No scored gallery seam is here."
    if state.region.changes.get("sanctum:secret_open"):
        return False, "The gallery shortcut is already open."
    state.region.tile_changes[f"{door.x},{door.y},{door.z}"] = "+"
    state.region.changes["sanctum:secret_open"] = True
    return True, "The scored stone turns aside. A second route through the ward gallery opens."


def inspect_lines(state: GameState) -> list[str]:
    row = SITES[state.active_region_id]
    account = _network(state)
    boss = next((actor for actor in state.threats if actor.id == f"sanctum:{state.active_region_id}:boss"), None)
    standing = f"{account.name}: trust {account.trust:+d}; obligation {account.obligation}" if account else "No travelling witness has arrived."
    lines = [
        f"{row['name']} — {row['theme']}.",
        standing,
        f"Seal: {'open' if state.region.changes.get('sanctum:unsealed') else 'closed'}; keeper: {'defeated' if boss and boss.status == 'defeated' else 'unseen' if boss is None else 'present'}.",
        f"Keeper: {row['boss']['capability']}; answer: {row['boss']['counterplay']}.",
        "O. Offer one physical faction dependency lot; improve the shared account and open the stair (once).",
        "B. Break the seal without a lot; open the stair, but incur a witnessed obligation (once).",
        "S. Study the inscription and the current encounter. Escape leaves the shrine.",
    ]
    marker = state.region.landmarks.get("sanctum_secret")
    if marker:
        lines.append(f"A scored seam lies in the ward gallery near {marker.x},{marker.y},z+1; stand on its mark and press E.")
    control = state.region.changes.get("sanctum:control")
    if control:
        lines.append(f"Aftermath: {control}; the next visit can change who holds the gallery.")
        lines.append("H. At a network-held, cleared site, ask its witness to open the existing one-time route shelter; this records one obligation.")
    return lines


def shrine_choice(state: GameState, choice: str) -> tuple[bool, str, int]:
    from .inventory import consume_carried

    account = _network(state)
    if choice == "s":
        event = state.region.changes.get("sanctum:last_event", "no fresh sign")
        return False, f"{SITES[state.active_region_id]['theme']}. This visit: {event}.", 0
    if account is None:
        return False, "The travelling institution account is not ready here.", 0
    if choice == "h":
        if not state.region.changes.get("sanctum:cleared") or state.region.changes.get("sanctum:control") != "network":
            return False, "Only a cleared site held by its travelling network can open a route shelter.", 0
        from .regional_history import NETWORK_CONTACTS, open_network_shelter

        contact_id = next((contact_id for network_id, region_id, contact_id, *_ in NETWORK_CONTACTS
                           if network_id == account.id and region_id == state.active_region_id), None)
        if contact_id is None:
            return False, "No regional witness represents this route account.", 0
        changed, message = open_network_shelter(state, contact_id)
        return changed, message, 2 if changed else 0
    if choice == "o":
        if state.region.changes.get("sanctum:offered"):
            return False, "This shrine has already accepted one witnessed offering.", 0
        if not consume_carried(state, f"commodity:{account.dependency}"):
            return False, f"Bring one physical {account.dependency} lot in the courier's pack.", 0
        state.region.changes["sanctum:offered"] = True
        state.region.changes["sanctum:unsealed"] = True
        state.region.changes["sanctum:opened_by"] = "offering"
        if state.region.changes.get("sanctum:cleared"):
            state.region.changes["sanctum:control"] = "disputed" if _active_claimants(state) else "network"
        account.trust = min(3, account.trust + 1)
        account.obligation = max(0, account.obligation - 1)
        account.witnessed_acts.append(f"{state.courier.name} offered {account.dependency} at {SITES[state.active_region_id]['name']}.")
        del account.witnessed_acts[:-8]
        return True, f"The physical {account.dependency} is witnessed. {account.name} trust rises and the stair opens across its paired regions.", 2
    if choice == "b":
        if state.region.changes.get("sanctum:unsealed"):
            return False, "The seal is already open; there is nothing left to break.", 0
        state.region.changes["sanctum:unsealed"] = True
        state.region.changes["sanctum:opened_by"] = "breach"
        account.trust = max(-3, account.trust - 1)
        account.obligation = min(9, account.obligation + 1)
        account.witnessed_acts.append(f"{state.courier.name} broke the {SITES[state.active_region_id]['name']} seal without an account.")
        del account.witnessed_acts[:-8]
        return True, f"The seal breaks. {account.name} records the breach; future site encounters grow less sheltered.", 2
    return False, "Choose Offer, Break, Study, or a held-site shelter.", 0


def approach(state: GameState) -> str:
    """Draw at most one physical site event per expedition and region."""
    if state.location != "region" or "sanctum_shrine" not in state.region.landmarks:
        return ""
    shrine = state.region.landmarks["sanctum_shrine"]
    if state.position.z != 0 or max(abs(state.position.x - shrine.x), abs(state.position.y - shrine.y)) > 4:
        return ""
    if state.region.changes.get("sanctum:event_visit") == state.expedition_count:
        return ""
    state.region.changes["sanctum:event_visit"] = state.expedition_count
    account = _network(state)
    trust = account.trust if account else 0
    control = state.region.changes.get("sanctum:control")
    options = ENCOUNTERS[control if control in {"network", "open", "disputed"}
                         else "sheltered" if trust >= 1 else "contested"]
    kind = stage_rng(state.seed, f"sanctum:{state.active_region_id}:visit:{state.expedition_count}").choice(options)
    if kind == "dropped parcel" and int(state.region.changes.get("sanctum:parcels", 0)) >= 2:
        kind = "quiet witnesses"
    state.region.changes["sanctum:last_event"] = kind
    if kind == "quiet witnesses":
        return f"Quiet witnesses mark the way to {SITES[state.active_region_id]['name']}; the shrine records their account."
    if kind in {"stray claimant", "wandering beast", "elite surveyor"}:
        return _site_encounter(state, shrine, kind)
    if kind == "travelling witness":
        return _witness(state, shrine)
    if kind == "dropped parcel":
        from .inventory import create_item
        from .world import is_walkable

        candidates = [Position(shrine.x + dx, shrine.y + dy, 0)
                      for dx, dy in ((2, 1), (-2, 1), (1, -2), (-1, -2))]
        place = next((point for point in candidates
                      if _tile(state.region, point) in _PASSABLE and is_walkable(state, point)
                      and not any(item.location == "ground" and item.region_id == state.active_region_id
                                  and item.ground_position == point for item in state.items)), None)
        if place is None or account is None:
            return "A parcel's trail reaches the shrine, but the working ground holds no recoverable lot."
        item = create_item(state, f"commodity:{account.dependency}",
                           f"lost travelling account near {SITES[state.active_region_id]['name']}", location="ground")
        item.region_id, item.ground_position = state.active_region_id, place
        state.region.changes["sanctum:parcels"] = int(state.region.changes.get("sanctum:parcels", 0)) + 1
        return f"A physical {account.dependency} parcel lies at {place.x},{place.y}; recover it with I, offer it, trade it, or leave it."
    from .materials import ensure_cell

    point = Position(shrine.x + 2, shrine.y, 0)
    cell = ensure_cell(state, point)
    if cell:
        cell.material, cell.support = "stone", 0
        cell.collapse_due = state.world_time + 3
        return "The shrine's outer stone cracks. Debris will fall in three actions; brace it or move clear."
    return "The shrine's outer stone sounds hollow, but the ground does not take a new fracture."


def record_boss_defeat(state: GameState, actor: Threat) -> None:
    if actor.id != f"sanctum:{state.active_region_id}:boss" or state.region.changes.get("sanctum:cleared"):
        return
    state.region.changes["sanctum:cleared"] = True
    opening = state.region.changes.get("sanctum:opened_by")
    state.region.changes["sanctum:control"] = (
        "disputed" if _active_claimants(state) else
        "network" if opening == "offering" else "disputed" if opening == "breach" else "open"
    )
    state.trade_credit += 4
    strategy_gained = False
    if state.courier:
        strategy_gained = state.courier.strategy < 20
        state.courier.strategy = min(20, state.courier.strategy + 1)
    state.remember(f"{state.courier.name if state.courier else 'A courier'} defeated {actor.name} at {SITES[state.active_region_id]['name']}.")
    strategy_note = ("one Strategy" if strategy_gained else
                     "Strategy is already at its bound" if state.courier else
                     "no active courier receives Strategy")
    state.add_message(f"{SITES[state.active_region_id]['name']} is cleared: four credits; {strategy_note}. Its physical reliquary remains to be opened.", priority=3)


def record_site_defeat(state: GameState, actor: Threat) -> None:
    record_boss_defeat(state, actor)
    if not state.region.changes.get("sanctum:cleared") or state.region.changes.get("sanctum:control") != "disputed":
        return
    prefix = f"sanctum:{state.active_region_id}:"
    if not (actor.id.startswith(prefix + "claimant:") or actor.id.startswith(prefix + "elites:")):
        return
    if _active_claimants(state):
        return
    state.region.changes["sanctum:control"] = "network" if state.region.changes.get("sanctum:opened_by") == "offering" else "open"
    state.add_message(f"The last armed claimant leaves {SITES[state.active_region_id]['name']}; its holding is {state.region.changes['sanctum:control']} again.", priority=2)
