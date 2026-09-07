"""Small, readable enemy goal selection and obstacle-aware movement."""

from __future__ import annotations

from collections import deque
from dataclasses import dataclass

from .state import GameState, GroupAlert, Position, Threat
from .world import distance, is_walkable, line_of_sight, pressure, vertical_destination


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
        state.location == "region"
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
    if alert and state.world_time - alert.raised_turn <= 12:
        threat.last_known_position = alert.position
        return False, alert.position, "an ally shared a last-known position"
    return False, threat.last_known_position, "the courier is out of contact"


def select_goal(state: GameState, threat: Threat) -> EnemyDecision:
    visible, perceived, perception_reason = perceive(state, threat)
    gap = distance(threat.position, state.position) if visible else 99
    scores: list[EnemyDecision] = []

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
    if visible:
        if threat.role == "thief" and gap <= 1 and pressure(state).valuables:
            option("steal cargo", "steal", 105, "an exposed valuable load is within reach", state.position)
        if threat.role == "lookout" and not threat.alarmed:
            option("raise alarm", "alarm", 98, "its guarded route has been breached", threat.home_position)
        if threat.profile == "ranged":
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
                    ally for ally in state.threats
                    if ally.id != threat.id and ally.group == threat.group
                    and ally.profile == "ranged" and ally.status in {"watching", "engaged"}
                ),
                None,
            )
            if ranged_ally:
                option("protect ally", "intercept", 90, "a ranged ally needs space", ranged_ally.position)
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
    return decision


def _neighbours(state: GameState, position: Position, threat: Threat) -> list[Position]:
    candidates = [
        Position(position.x + 1, position.y, position.z),
        Position(position.x - 1, position.y, position.z),
        Position(position.x, position.y + 1, position.z),
        Position(position.x, position.y - 1, position.z),
    ]
    vertical = vertical_destination(state, position)
    if vertical and "no-climb" not in threat.capabilities:
        candidates.append(vertical)
    occupied = {
        other.position for other in state.threats
        if other.id != threat.id and other.status in {"watching", "engaged"}
    }
    return [
        candidate for candidate in candidates
        if candidate not in occupied and is_walkable(state, candidate, ignore_threat=True)
    ]


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
    queue = deque([threat.position])
    previous: dict[Position, Position | None] = {threat.position: None}
    found: Position | None = None
    while queue and len(previous) <= limit:
        current = queue.popleft()
        if distance(current, target) <= stop_distance:
            found = current
            break
        for candidate in _neighbours(state, current, threat):
            if candidate not in previous:
                previous[candidate] = current
                queue.append(candidate)
    if found is None:
        return threat.position
    while previous[found] not in {None, threat.position}:
        found = previous[found]  # type: ignore[index]
    return found


def retreat_step(state: GameState, threat: Threat) -> Position:
    candidates = _neighbours(state, threat.position, threat)
    if not candidates:
        return threat.position
    return max(
        candidates,
        key=lambda point: (
            distance(point, state.position),
            -distance(point, threat.home_position) if threat.home_position else 0,
            -point.y,
            -point.x,
        ),
    )


def raise_group_alert(state: GameState, threat: Threat) -> None:
    if not threat.group:
        return
    state.group_alerts[threat.group] = GroupAlert(state.position, state.world_time, threat.id)
    threat.alarmed = True
    for ally in state.threats:
        if ally.group == threat.group and ally.status == "watching":
            ally.last_known_position = state.position
            ally.status = "engaged"
