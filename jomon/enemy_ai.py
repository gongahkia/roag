"""Small, readable enemy goal selection and obstacle-aware movement."""

from __future__ import annotations

from collections import deque
from dataclasses import dataclass

from .state import GameState, GroupAlert, Position, Threat
from .world import base_tile, distance, is_walkable, line_of_sight, pressure, vertical_destination


@dataclass(frozen=True)
class EnemyDecision:
    goal: str
    action: str
    reason: str
    target: Position | None = None
    utility: int = 0


def effective_vision(state: GameState, threat: Threat) -> int:
    penalty = 3 if state.weather in {"river fog", "hard rain", "coast squall", "forest rain"} else 0
    if threat.position.z > state.position.z:
        penalty = max(0, penalty - 2)
    return max(3, threat.vision - penalty)


def sees_courier(state: GameState, threat: Threat) -> bool:
    return (
        state.combat_active
        and distance(threat.position, state.position) <= effective_vision(state, threat)
        and line_of_sight(state, threat.position, state.position)
    )


def heard_position(state: GameState, threat: Threat) -> Position | None:
    audible = [
        event for event in state.sound_events
        if event.age <= 3
        and abs(event.position.z - threat.position.z) <= 1
        and distance(event.position, threat.position) <= threat.hearing + event.strength
    ]
    return max(audible, key=lambda event: (event.strength - event.age, -event.age), default=None).position if audible else None


def perceive(state: GameState, threat: Threat) -> tuple[bool, Position | None, str]:
    if sees_courier(state, threat):
        threat.last_known_position = state.position
        return True, state.position, "the courier is in sight"
    heard = heard_position(state, threat)
    if heard:
        threat.last_known_position = heard
        return False, heard, "a recent sound has a known origin"
    alert = state.group_alerts.get(threat.group) if threat.group else None
    source = next((actor for actor in state.combatants if alert and actor.id == alert.source_id), None)
    if alert and source and distance(source.position, threat.position) <= threat.hearing + 4 and state.world_time - alert.raised_turn <= 12:
        threat.last_known_position = alert.position
        return False, alert.position, "an ally shared a last-known position"
    return False, threat.last_known_position, "the courier is out of contact"


def higher_access_target(state: GameState, threat: Threat) -> Position | None:
    if state.location == "jomon":
        from .vessel import UPPER_STAIR, MAIN_UPPER_STAIR
        return UPPER_STAIR if threat.position.z == MAIN_UPPER_STAIR.z else None
    candidates: list[Position] = []
    for link in state.region.vertical_links:
        lower, upper = (
            (link.first, link.second)
            if link.first.z < link.second.z else (link.second, link.first)
        )
        if lower.z == threat.position.z and upper.z > threat.position.z:
            candidates.append(upper)
    return min(
        candidates,
        key=lambda point: (distance(threat.position, point), point.y, point.x),
        default=None,
    )


def select_goal(state: GameState, threat: Threat) -> EnemyDecision:
    visible, perceived, perception_reason = perceive(state, threat)
    gap = distance(threat.position, state.position) if visible else 99
    scores: list[EnemyDecision] = []
    from .ecology import world_options

    scores.extend(world_options(state, threat, visible))

    def option(goal: str, action: str, utility: int, reason: str, target: Position | None = None) -> None:
        scores.append(EnemyDecision(goal, action, reason, target, utility))

    if threat.carrying_item_id:
        option("escape with cargo", "escape", 120, "it has obtained the cargo it came for", threat.home_position)
    if threat.morale <= 0 or (threat.health <= max(1, threat.max_health // 3) and threat.morale < 3):
        option("break contact", "retreat", 110, "injury and morale make survival more valuable")
    local_key = f"{threat.position.x},{threat.position.y},{threat.position.z}"
    if local_key in state.smoke:
        option("escape smoke", "withdraw", 101, "smoke has removed a reliable firing or guard position")
    if local_key in state.water and "crosses draining mud quickly" not in threat.capabilities:
        option("leave rising water", "withdraw", 99, "the regional process made this position unsafe")
    territorial = threat.role == "territorial" or any(
        "territor" in capability or "wallow" in capability
        for capability in threat.capabilities
    )
    if (
        territorial and threat.home_position
        and (distance(threat.position, threat.home_position) > 7
             or visible and distance(state.position, threat.home_position) > 7)
    ):
        option(
            "defend territory", "return", 108,
            "the courier has left the boundary it defends", threat.home_position,
        )
    if visible:
        if threat.role == "thief" and gap <= 1 and (state.carried_goods or state.carried_passives or state.carried_relic):
            option("steal cargo", "steal", 105, "an exposed valuable load is within reach", state.position)
        if threat.role == "lookout" and not threat.alarmed:
            option("raise alarm", "alarm", 98, "its guarded route has been breached", threat.home_position)
        if threat.role == "controller" and gap <= 4:
            option("deny route", "control", 95, "the courier is inside its material control range", state.position)
        if threat.role == "flanker" and 2 <= gap <= 7:
            horizontal = state.position.x - threat.position.x
            vertical = state.position.y - threat.position.y
            side = -1 if (threat.turn + sum(ord(char) for char in threat.id)) % 2 else 1
            offset_x = side if abs(horizontal) >= abs(vertical) else 0
            offset_y = side if abs(horizontal) < abs(vertical) else 0
            flank = Position(state.position.x + offset_x * 2, state.position.y + offset_y * 2, state.position.z)
            option("flank last sight", "flank", 94, "a side approach avoids the courier's facing", flank)
        if threat.profile == "ranged":
            wants_height = threat.goal == "seek elevation" or any(
                "height" in capability or "elevation" in capability
                for capability in threat.capabilities
            )
            higher = higher_access_target(state, threat) if wants_height else None
            if higher and gap >= 5:
                option(
                    "seek elevation", "seek elevation", 97,
                    "an upper firing position improves its visible lane", higher,
                )
            feeds_smoke = any(
                "feeds smoke" in capability or "vents caustic smoke" in capability
                for capability in threat.capabilities
            )
            if feeds_smoke and local_key not in state.smoke and gap <= 9:
                option(
                    "deny area", "feed smoke", 96,
                    "its assigned fuel or vent can close the courier's lane", state.position,
                )
            if threat.reload_turns > 0:
                option("prepare shot", "reload", 100, "the ranged weapon is not ready")
            elif threat.ammunition <= 0:
                option("break contact", "retreat", 96, "no ammunition remains")
            elif gap < 4 and threat.role in {"shooter", "skirmisher", "suppressor"}:
                option("hold distance", "withdraw", 94, "the courier is inside its useful range")
            elif line_of_sight(state, threat.position, state.position):
                option("obtain line of fire", "aim", 92, "it has a clear telegraphed lane", state.position)
            else:
                option("obtain line of fire", "seek line", 86, "terrain blocks the projectile lane", state.position)
        if threat.role == "protector":
            ranged_ally = next(
                (
                    ally for ally in state.combatants
                    if ally.id != threat.id and ally.group == threat.group
                    and ally.profile == "ranged" and ally.status in {"watching", "engaged"}
                ),
                None,
            )
            if ranged_ally:
                option("protect ally", "intercept", 90, "a ranged ally needs space", ranged_ally.position)
            wounded_ally = next(
                (
                    ally for ally in state.combatants
                    if ally.id != threat.id and ally.group == threat.group
                    and ally.status == "engaged"
                    and (ally.health <= ally.max_health // 2 or ally.morale <= 1)
                ),
                None,
            )
            if wounded_ally:
                option(
                    "cover retreat", "cover retreat", 99,
                    "a wounded ally needs a physical withdrawal lane",
                    wounded_ally.position,
                )
        preferred = 2 if threat.profile == "reach" else 1
        if gap <= preferred:
            option("press attack", "attack", 88, "the courier is inside its preferred distance", state.position)
        else:
            option("pursue last sight", "approach", 76, perception_reason, state.position)
    elif perceived:
        option("investigate", "investigate", 70, perception_reason, perceived)
    if threat.patrol:
        target = threat.patrol[(threat.patrol_index + 1) % len(threat.patrol)]
        option("patrol", "patrol", 40, "the assigned route remains quiet", target)
    if threat.home_position:
        option("guard position", "return", 30, "no stronger perceived fact displaces its duty", threat.home_position)
    option("hold", "wait", 10, "no legal higher-value action is apparent", threat.position)

    decision = max(scores, key=lambda item: (item.utility, item.goal, item.action))
    threat.goal, threat.goal_reason = decision.goal, decision.reason
    if state.location == "region" and state.region.changes.get("quest_guard_id") == threat.id:
        duty = state.region.changes.get("quest_guard_reason")
        if duty:
            threat.goal_reason += f"; {duty}"
    return decision


def _neighbours(state: GameState, position: Position, threat: Threat, context=None) -> list[Position]:
    candidates = [
        Position(position.x + 1, position.y, position.z),
        Position(position.x - 1, position.y, position.z),
        Position(position.x, position.y + 1, position.z),
        Position(position.x, position.y - 1, position.z),
    ]
    vertical = context[0].get(position) if context else vertical_destination(state, position)
    if vertical and "no-climb" not in threat.capabilities:
        candidates.append(vertical)
    occupied = context[1] if context else {
        other.position for other in state.combatants
        if other.id != threat.id and other.status in {"watching", "engaged"}
    }
    def walkable(candidate):
        if context is None:
            return is_walkable(state, candidate, ignore_threat=True)
        if candidate not in context[2]:
            context[2][candidate] = is_walkable(state, candidate, ignore_threat=True)
        return context[2][candidate]

    legal = [
        candidate for candidate in candidates
        if candidate not in occupied and walkable(candidate)
    ]
    safe_scree = any("scree" in capability for capability in threat.capabilities)
    return sorted(
        legal,
        key=lambda candidate: (
            0 if safe_scree or base_tile(state, candidate) not in {"r", "q", "%"} else 1,
            candidate.z,
            candidate.y,
            candidate.x,
        ),
    )


def next_path_step(
    state: GameState,
    threat: Threat,
    target: Position,
    *,
    stop_distance: int = 1,
    limit: int = 700,
) -> Position:
    if distance(threat.position, target) <= stop_distance:
        return threat.position
    occupied = frozenset(
        other.position for other in state.combatants
        if other.id != threat.id and other.status in {"watching", "engaged"}
    )
    signature = (
        state.active_region_id, state.location, target, stop_distance, limit,
        tuple(threat.capabilities), occupied,
        tuple((z, tuple(rows)) for z, rows in state.region.levels.items()),
        tuple(sorted(state.region.tile_changes.items())),
        tuple(sorted(state.vessel_tiles.items())),
        tuple((key, cell.ice, cell.water) for key, cell in sorted(state.region.materials.items())),
        tuple(sorted(state.smoke)), tuple(sorted(state.water)),
        tuple((link.first, link.second) for link in state.region.vertical_links),
        tuple((c.position, c.opened) for c in state.region.containers),
        tuple((actor.actor_id, actor.area, actor.position) for actor in state.actor_schedules.values()),
    )
    cache = getattr(state, "_path_cache", {})
    saved = cache.get(threat.id)
    if saved and saved[0] == signature and threat.position in saved[1]:
        index = saved[1].index(threat.position)
        return saved[1][min(index + 1, len(saved[1]) - 1)]
    links = {}
    if state.location == "jomon":
        from .vessel import LOWER_HATCH, MAIN_LOWER_HATCH, MAIN_UPPER_STAIR, UPPER_STAIR
        links = {LOWER_HATCH: MAIN_LOWER_HATCH, MAIN_LOWER_HATCH: LOWER_HATCH, MAIN_UPPER_STAIR: UPPER_STAIR, UPPER_STAIR: MAIN_UPPER_STAIR}
    else:
        for link in state.region.vertical_links:
            links[link.first], links[link.second] = link.second, link.first
    context = (links, occupied, {})
    queue = deque([threat.position])
    previous: dict[Position, Position | None] = {threat.position: None}
    found: Position | None = None
    while queue and len(previous) <= limit:
        current = queue.popleft()
        if distance(current, target) <= stop_distance:
            found = current
            break
        for candidate in _neighbours(state, current, threat, context):
            if candidate not in previous:
                previous[candidate] = current
                queue.append(candidate)
    if found is None:
        return threat.position
    path = [found]
    while previous[path[-1]] is not None:
        path.append(previous[path[-1]])
    path.reverse()
    if len(cache) >= 32 and threat.id not in cache:
        del cache[next(iter(cache))]
    cache[threat.id] = (signature, tuple(path))
    state._path_cache = cache
    return path[1] if len(path) > 1 else threat.position


def retreat_step(state: GameState, threat: Threat) -> Position:
    from .ecology import safe_step

    danger = state.position if sees_courier(state, threat) else threat.last_known_position or threat.position
    return safe_step(state, threat, danger)


def raise_group_alert(state: GameState, threat: Threat) -> None:
    if not threat.group:
        return
    state.group_alerts[threat.group] = GroupAlert(state.position, state.world_time, threat.id)
    threat.alarmed = True
    for ally in state.combatants:
        if ally.group == threat.group and ally.status == "watching" and distance(ally.position, threat.position) <= ally.hearing + 4:
            ally.last_known_position = state.position
            ally.status = "engaged"
