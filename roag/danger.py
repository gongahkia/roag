"""Deterministic regional pressure policy and bounded danger directives.

This module owns pressure interpretation and its bounded deterministic
consequences. It does not advance time or choose how the terminal presents
danger. Callers evaluate immutable directives, then apply them synchronously
inside the authoritative world step.
"""

from __future__ import annotations

from dataclasses import dataclass

from .action_presentation import action_format
from .content import ENEMY_ARCHETYPES
from .runtime_events import RuntimeEventCollector, ThreatSpawned
from .state import GameState, Position, stage_rng


REINFORCEMENT_INTERVAL = {"strained": 30, "critical": 15}
REINFORCEMENT_MIN_DISTANCE = 12
REINFORCEMENT_COUNT_KEY = "danger:reinforcement_count"
REINFORCEMENT_LAST_TURN_KEY = "danger:last_reinforcement_turn"
PRESSURE_THRESHOLDS = {"strained": 10, "critical": 18}


@dataclass(frozen=True)
class Pressure:
    elapsed: int
    depth: int
    noise: int
    valuables: int
    score: int
    band: str
    alert_range: int
    pursuit_steps: int


@dataclass(frozen=True)
class DangerForecast:
    """Player-legible projection of pressure without hidden actor data."""

    pressure: Pressure
    next_band: str | None
    points_to_next_band: int | None
    response_due_in: int | None


@dataclass(frozen=True)
class DangerAction:
    """One finite deterministic pressure response selected by the director."""

    action_id: str
    band: str
    target_id: str | None = None
    archetype_id: str | None = None
    position: Position | None = None
    sequence: int = 0


@dataclass(frozen=True)
class DangerOutcome:
    """Applied directives and their existing player-facing messages."""

    pressure: Pressure
    actions: tuple[DangerAction, ...] = ()
    messages: tuple[str, ...] = ()


def pressure(state: GameState) -> Pressure:
    """Return the current pressure profile without mutating simulation state."""
    if state.location != "region":
        return Pressure(0, 0, 0, 0, 0, "safe", 0, 0)
    landing = state.region.landmarks["landing"]
    distance = abs(state.position.x - landing.x) + abs(state.position.y - landing.y)
    depth = distance // 14 + abs(state.position.z) * 2
    valuables = sum(state.carried_passives.values()) + sum(
        stack.quantity for stack in state.carried_goods.values()
    )
    run_pressure = 0
    if state.run is not None and state.run.status == "active":
        from .run_items import effect_value
        from .run_progression import run_intensity

        run_pressure = (
            run_intensity(state)
            + state.run.stage_salvage // 6
            + effect_value(state, "drop_chance") // 4
            + effect_value(state, "pressure_loot") // 4
            + (state.run.challenge_tier if state.run.challenge_tier >= 4 else 0)
        )
    score = (
        state.pressure_elapsed // 18 + depth + state.noise // 2
        + valuables + run_pressure
    )
    if score >= PRESSURE_THRESHOLDS["critical"]:
        band, alert, pursuit = "critical", 12, 2
    elif score >= PRESSURE_THRESHOLDS["strained"]:
        band, alert, pursuit = "strained", 9, 1
    else:
        band, alert, pursuit = "steady", 6, 1
    return Pressure(
        state.pressure_elapsed, depth, state.noise, valuables, score,
        band, alert, pursuit,
    )


def danger_forecast(state: GameState) -> DangerForecast:
    """Project current pressure and the shared response cadence for UI use."""
    profile = pressure(state)
    if profile.band == "safe":
        return DangerForecast(profile, None, None, None)
    if profile.band == "steady":
        next_band = "strained"
    elif profile.band == "strained":
        next_band = "critical"
    else:
        next_band = None
    points = (
        PRESSURE_THRESHOLDS[next_band] - profile.score
        if next_band is not None else None
    )
    interval = REINFORCEMENT_INTERVAL.get(profile.band)
    if interval is not None and state.run is not None and state.run.challenge_tier >= 1:
        interval = max(8, interval - 6)
    due_in: int | None = None
    if interval is not None:
        last_turn = state.region.changes.get(REINFORCEMENT_LAST_TURN_KEY)
        due_in = (
            max(0, interval - (state.world_time - last_turn))
            if type(last_turn) is int else 0
        )
    return DangerForecast(profile, next_band, points, due_in)


def _reinforcement_sequence(state: GameState) -> int:
    stored = state.region.changes.get(REINFORCEMENT_COUNT_KEY, 0)
    return stored if type(stored) is int and stored >= 0 else 0


def _reinforcement_archetypes(region_id: str) -> tuple[str, ...]:
    return tuple(sorted(
        identity for identity, row in ENEMY_ARCHETYPES.items()
        if row["region"] == region_id
        and not row.get("elite")
        and not row.get("aftermath")
        and row["profile"] not in {"animal", "machinery"}
        and row.get("ecology") in {None, "", "raider"}
    ))


def _reinforcement_position(
    state: GameState,
    sequence: int,
    reserved: frozenset[Position] = frozenset(),
) -> Position | None:
    """Choose a fair, reachable arrival cell from current authoritative state."""
    from .ecology import ACTIVE_RADIUS
    from .materials import fields
    from .regions import region_reachable
    from .world import distance, field_of_view, is_walkable, line_of_sight, position_key

    visible = field_of_view(state, remember=False)
    occupied = {state.position}
    occupied.update(actor.position for actor in state.combatants)
    occupied.update(state.region.landmarks.values())
    occupied.update(box.position for box in state.region.containers)
    occupied.update(
        point for link in state.region.vertical_links for point in (link.first, link.second)
    )
    occupied.update(
        contact.position for contact in state.contacts[state.active_region_id]
        if contact.position is not None
    )
    occupied.update(
        vehicle.position for vehicle in state.vehicles.values()
        if vehicle.region_id == state.active_region_id
    )
    occupied.update(
        schedule.position for schedule in state.actor_schedules.values()
        if schedule.area == f"region:{state.active_region_id}"
    )
    materials = fields(state)
    candidates = []
    for point in region_reachable(state.region, state.position):
        separation = distance(state.position, point)
        material = materials.get(position_key(point))
        if (
            point in occupied or point in reserved
            or abs(point.z - state.position.z) > 1
            or not REINFORCEMENT_MIN_DISTANCE <= separation <= ACTIVE_RADIUS
            or point in visible
            or line_of_sight(state, state.position, point)
            or not is_walkable(state, point, ignore_threat=True)
            or (
                material is not None
                and (material.fire or material.collapse_due or material.water >= 2)
            )
        ):
            continue
        candidates.append(point)
    if not candidates:
        return None
    candidates.sort(key=lambda point: (point.z, point.y, point.x))
    return stage_rng(
        state.seed,
        f"danger:reinforcement:{state.active_region_id}:{sequence}:position",
    ).choice(candidates)


def _reinforcement_action(
    state: GameState,
    profile: Pressure,
    *,
    sequence: int | None = None,
    reserved: frozenset[Position] = frozenset(),
) -> DangerAction | None:
    if profile.band not in REINFORCEMENT_INTERVAL:
        return None
    from .ecology import ACTOR_BUDGET, REGIONAL_ACTOR_LIMIT, active_actors

    if danger_forecast(state).response_due_in != 0:
        return None
    if (
        len(state.threats) >= REGIONAL_ACTOR_LIMIT
        or len(active_actors(state)) >= ACTOR_BUDGET
    ):
        return None
    archetypes = _reinforcement_archetypes(state.active_region_id)
    if not archetypes:
        return None
    existing_ids = {actor.id for actor in state.threats}
    sequence = _reinforcement_sequence(state) if sequence is None else sequence
    while sequence < REGIONAL_ACTOR_LIMIT:
        archetype = stage_rng(
            state.seed,
            f"danger:reinforcement:{state.active_region_id}:{sequence}:archetype",
        ).choice(archetypes)
        actor_id = f"{state.active_region_id}-reinforcement-{sequence}:{archetype}"
        if actor_id not in existing_ids:
            break
        sequence += 1
    else:
        return None
    position = _reinforcement_position(state, sequence, reserved)
    if position is None:
        return None
    return DangerAction(
        "danger.spawn_reinforcement", profile.band, actor_id,
        archetype, position, sequence,
    )


def _activation_fallback_action(
    state: GameState, profile: Pressure,
) -> DangerAction | None:
    """Reuse an ordinary dormant threat when a due arrival cannot be placed."""
    if (
        profile.band not in REINFORCEMENT_INTERVAL
        or danger_forecast(state).response_due_in != 0
    ):
        return None
    from .ecology import ACTOR_BUDGET, active_actors
    from .world import distance

    if len(active_actors(state)) >= ACTOR_BUDGET:
        return None
    candidates = [
        actor for actor in state.combatants
        if actor.status == "dormant"
        and not actor.elite
        and actor.profile not in {"animal", "machinery"}
    ]
    if not candidates:
        return None
    actor = min(candidates, key=lambda candidate: (
        distance(state.position, candidate.position), candidate.id,
    ))
    return DangerAction("danger.activate_threat", profile.band, actor.id)


def evaluate_danger_step(state: GameState) -> tuple[DangerAction, ...]:
    """Select bounded per-step pressure consequences without applying them."""
    profile = pressure(state)
    if state.location != "region":
        return ()
    actions: list[DangerAction] = []
    if profile.band == "critical" and not state.escalation_spawned:
        dormant = next(
            (actor for actor in state.combatants if actor.status == "dormant"),
            None,
        )
        actions.append(DangerAction(
            "danger.critical_escalation",
            profile.band,
            dormant.id if dormant is not None else None,
        ))
    group_size = 3 if profile.band == "critical" else 2
    from .ecology import ACTOR_BUDGET, REGIONAL_ACTOR_LIMIT, active_actors

    group_size = min(
        group_size,
        max(0, ACTOR_BUDGET - len(active_actors(state))),
        max(0, REGIONAL_ACTOR_LIMIT - len(state.threats)),
    )
    sequence = _reinforcement_sequence(state)
    reserved: set[Position] = set()
    for offset in range(group_size):
        reinforcement = _reinforcement_action(
            state,
            profile,
            sequence=sequence + offset,
            reserved=frozenset(reserved),
        )
        if reinforcement is None:
            break
        actions.append(reinforcement)
        if reinforcement.position is not None:
            reserved.add(reinforcement.position)
    if not actions:
        activation = _activation_fallback_action(state, profile)
        if activation is not None:
            actions.append(activation)
    return tuple(actions)


def evaluate_band_transition(
    state: GameState, previous_band: str,
) -> tuple[DangerAction, ...]:
    """Select the legacy post-command band transition, if one occurred."""
    profile = pressure(state)
    if (
        state.location == "region"
        and profile.band != previous_band
        and profile.band in {"strained", "critical"}
    ):
        return (DangerAction("danger.band_transition", profile.band),)
    return ()


def evaluate_region_entry(state: GameState) -> tuple[DangerAction, ...]:
    """Select the authored situation established on regional entry."""
    if state.location != "region":
        return ()
    # Entry has historically established the steady situation even if carried
    # valuables make the computed pressure profile start in a higher band.
    return (DangerAction("danger.activate_situation", "steady"),)


def apply_danger_actions(
    state: GameState, actions: tuple[DangerAction, ...],
    *, collector: RuntimeEventCollector | None = None,
) -> tuple[str, ...]:
    """Apply a previously evaluated bounded directive sequence in order."""
    messages: list[str] = []
    for action in actions:
        if action.action_id == "danger.critical_escalation":
            state.escalation_spawned = True
            state.region.changes["escalation_spawned"] = True
            actor = next(
                (
                    candidate for candidate in state.combatants
                    if candidate.id == action.target_id and candidate.status == "dormant"
                ),
                None,
            )
            if actor is not None:
                actor.status = "watching"
                messages.append(
                    action_format("action.process.escalation", threat=actor.name)
                )
        elif action.action_id == "danger.band_transition":
            from .situations import activate_for_band

            activate_for_band(state, action.band)
            messages.append(action_format("action.pressure.increased", band=action.band))
        elif action.action_id == "danger.activate_situation":
            from .situations import activate_for_band

            activate_for_band(state, action.band)
        elif action.action_id == "danger.spawn_reinforcement":
            if (
                action.target_id is None
                or action.archetype_id is None
                or action.position is None
                or any(actor.id == action.target_id for actor in state.threats)
            ):
                raise ValueError("invalid reinforcement directive")
            from .encounters import threat_from_archetype
            from .enemy_equipment import issue_enemy_equipment

            actor = threat_from_archetype(
                action.archetype_id,
                action.position,
                encounter_id=f"{state.active_region_id}-reinforcement-{action.sequence}",
                group=f"{state.active_region_id}-reinforcement-{action.sequence}",
            )
            if actor.id != action.target_id:
                raise ValueError("reinforcement identity does not match directive")
            actor.status = "engaged"
            actor.last_known_position = state.position
            actor.alarmed = True
            state.threats.append(actor)
            state.region_threats[state.active_region_id] = state.threats
            issue_enemy_equipment(state, actor, state.active_region_id)
            state.region.changes[f"enemy_kit:{actor.id}"] = 1
            state.region.changes[REINFORCEMENT_COUNT_KEY] = max(
                int(state.region.changes.get(REINFORCEMENT_COUNT_KEY, 0)),
                action.sequence + 1,
            )
            state.region.changes[REINFORCEMENT_LAST_TURN_KEY] = state.world_time
            if collector is not None:
                collector.record_step_event(ThreatSpawned(
                    actor.id, action.archetype_id, action.position, action.band,
                ))
        elif action.action_id == "danger.activate_threat":
            actor = next(
                (
                    candidate for candidate in state.combatants
                    if candidate.id == action.target_id
                    and candidate.status == "dormant"
                    and not candidate.elite
                ),
                None,
            )
            if actor is None:
                raise ValueError("invalid dormant-threat activation directive")
            actor.status = "engaged"
            actor.last_known_position = state.position
            actor.alarmed = True
            state.region.changes[REINFORCEMENT_LAST_TURN_KEY] = state.world_time
            messages.append(
                action_format("action.process.escalation", threat=actor.name)
            )
        else:
            raise ValueError(f"unknown danger action {action.action_id!r}")
    return tuple(messages)


def resolve_danger_step(
    state: GameState, *, collector: RuntimeEventCollector | None = None,
) -> DangerOutcome:
    """Evaluate and apply the existing in-step pressure response."""
    actions = evaluate_danger_step(state)
    return DangerOutcome(
        pressure(state), actions,
        apply_danger_actions(state, actions, collector=collector),
    )


def resolve_band_transition(
    state: GameState, previous_band: str,
) -> DangerOutcome:
    """Evaluate and apply the existing post-command pressure transition."""
    actions = evaluate_band_transition(state, previous_band)
    return DangerOutcome(pressure(state), actions, apply_danger_actions(state, actions))


def resolve_region_entry(state: GameState) -> DangerOutcome:
    """Apply the existing initial authored pressure situation without time."""
    actions = evaluate_region_entry(state)
    return DangerOutcome(pressure(state), actions, apply_danger_actions(state, actions))
