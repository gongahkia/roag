"""Immutable renderer-neutral projections of current Jomon state."""

from __future__ import annotations

from dataclasses import dataclass

from .state import GameState, Position
from .world import base_tile, field_of_view, position_key


@dataclass(frozen=True)
class CellView:
    """One observed map cell.

    ``topology_token`` is an engine topology token, not a curses glyph contract.
    It remains only until the dedicated ASCII/topology split.
    """
    position: Position
    topology_token: str
    visible: bool
    remembered: bool
    feature_ids: tuple[str, ...]
    actor_ids: tuple[str, ...]


@dataclass(frozen=True)
class WorldView:
    width: int
    height: int
    level: int
    cells: tuple[CellView, ...]
    courier_position: Position


@dataclass(frozen=True)
class ActorView:
    id: str
    actor_kind: str
    presentation_id: str
    position: Position | None
    active: bool
    alive: bool
    health: int
    max_health: int
    status_id: str | None
    intent_id: str | None
    goal_id: str | None
    role_id: str | None


@dataclass(frozen=True)
class InteractionOptionView:
    interaction_id: str
    target_id: str
    available: bool
    reason_id: str | None = None


@dataclass(frozen=True)
class InteractionView:
    position: Position
    options: tuple[InteractionOptionView, ...]


def _area_actor_positions(state: GameState) -> dict[Position, list[str]]:
    actors: dict[Position, list[str]] = {}
    area = f"region:{state.active_region_id}" if state.location == "region" else state.jomon_space
    for schedule in state.actor_schedules.values():
        if schedule.area == area and schedule.position is not None:
            actors.setdefault(schedule.position, []).append(schedule.actor_id)
    for threat in state.combatants:
        if threat.status in {"dormant", "watching", "engaged"}:
            actors.setdefault(threat.position, []).append(threat.id)
    return actors


def world_view(state: GameState) -> WorldView:
    """Return an observational map projection without revealing or changing it."""
    visible = field_of_view(state, remember=False)
    remembered = set(state.region.seen) if state.location == "region" else set()
    actors = _area_actor_positions(state)
    if state.location == "region":
        rows = state.region.levels[str(state.position.z)]
    else:
        from .world import map_rows
        rows = map_rows(state)
    cells: list[CellView] = []
    for y, row in enumerate(rows):
        for x in range(len(row)):
            point = Position(x, y, state.position.z)
            features: list[str] = []
            if position_key(point) in state.smoke:
                features.append("field.smoke")
            if position_key(point) in state.water:
                features.append("field.water")
            cells.append(CellView(
                point, base_tile(state, point), point in visible,
                position_key(point) in remembered, tuple(features),
                tuple(sorted(actors.get(point, ()))),
            ))
    return WorldView(len(rows[0]) if rows else 0, len(rows), state.position.z, tuple(cells), state.position)


def actor_views(state: GameState) -> tuple[ActorView, ...]:
    """Return actor projections keyed only by stable engine identities."""
    views: list[ActorView] = []
    people = [*state.household, *state.visitors, state.bartender, state.merchant]
    for contacts in state.contacts.values():
        people.extend(contacts)
    for person in people:
        schedule = state.actor_schedules.get(person.id)
        views.append(ActorView(
            person.id, "person", getattr(person, "people_presentation_id", None) or person.id,
            schedule.position if schedule else getattr(person, "position", None),
            bool(getattr(person, "available", True)), bool(getattr(person, "alive", True)),
            int(getattr(person, "health", 0)), int(getattr(person, "max_health", 0)),
            None, None, None, person.role,
        ))
    for threat in state.combatants:
        views.append(ActorView(
            threat.id, "threat", threat.archetype_id or threat.id, threat.position,
            threat.status in {"watching", "engaged"}, threat.status != "defeated",
            threat.health, threat.max_health, threat.status, threat.intent_id or None,
            threat.goal_id or None, threat.role or None,
        ))
    return tuple(sorted(views, key=lambda view: view.id))


def interaction_view(state: GameState) -> InteractionView:
    """Expose the ordinary current-position interaction without menu metadata."""
    available = not state.world_ended
    return InteractionView(
        state.position,
        (InteractionOptionView(
            "interact.current", position_key(state.position), available,
            None if available else "interaction.world_ended",
        ),),
    )
