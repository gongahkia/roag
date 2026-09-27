"""Immutable renderer-neutral projections of current Jomon state."""

from __future__ import annotations

from dataclasses import dataclass

from .state import GameState, Position
from .world import base_tile, field_of_view, position_key, semantic_cell


@dataclass(frozen=True)
class CellView:
    """One observed map cell.

    ``terrain_id`` and ``feature_ids`` are renderer-neutral semantic identities.
    Legacy curses glyphs are deliberately absent from this projection.
    """
    position: Position
    terrain_id: str
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
    display_name: str
    role_label: str | None


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
            semantic = semantic_cell(state, point)
            terrain_id = semantic.terrain_id if semantic is not None else f"terrain.region.token.{ord(base_tile(state, point)):02x}"
            features: list[str] = ([semantic.feature_id] if semantic is not None and semantic.feature_id else [])
            if position_key(point) in state.smoke:
                features.append("field.smoke")
            if position_key(point) in state.water:
                features.append("field.water")
            cells.append(CellView(
                point, terrain_id, point in visible,
                position_key(point) in remembered, tuple(features),
                tuple(sorted(actors.get(point, ()))),
            ))
    return WorldView(len(rows[0]) if rows else 0, len(rows), state.position.z, tuple(cells), state.position)


def actor_views(state: GameState) -> tuple[ActorView, ...]:
    """Return actor projections keyed only by stable engine identities."""
    from .character_presentation import character_presentation, role_display_name
    from .ecology_presentation import ecology_actor_name, ecology_text
    from .people_presentation import contact_name, household_family_name, household_first_name, recruit_presentation

    def person_presentation(person: object) -> tuple[str, str | None]:
        """Resolve current names from stable presentation slots, never catalogs."""
        person_id = str(getattr(person, "id"))
        role_id = str(getattr(person, "role", ""))
        try:
            name = character_presentation(person_id).display_name
        except KeyError:
            given = getattr(person, "given_name_slot", None)
            family = getattr(person, "family_name_slot", None)
            presentation_id = getattr(person, "people_presentation_id", None)
            name_slot = getattr(person, "name_slot", None)
            try:
                if given is not None and family is not None:
                    name = f"{household_first_name(int(given.removeprefix('first_')))} {household_family_name(int(family.removeprefix('family_')))}"
                elif presentation_id:
                    name = recruit_presentation(presentation_id)["name"]
                elif name_slot:
                    name = contact_name(int(name_slot.removeprefix("contact_")))
                else:
                    name = person_id
            except (KeyError, ValueError):
                # Old snapshots without stable presentation slots retain only a
                # stable identity here; their frozen historical text is not a
                # new current-presentation authority.
                name = person_id
        return name, role_display_name(role_id) if role_id else None

    def threat_presentation(archetype_id: str, threat_id: str) -> str:
        if threat_id in {"wheel-train", "floodgate-claimant"}:
            return ecology_text(f"ecology.special.{threat_id}.name")
        return ecology_actor_name(archetype_id, archetype_id or threat_id)

    views: list[ActorView] = []
    people = [*state.household, *state.visitors, state.bartender, state.merchant]
    for contacts in state.contacts.values():
        people.extend(contacts)
    for person in people:
        schedule = state.actor_schedules.get(person.id)
        name, role_label = person_presentation(person)
        views.append(ActorView(
            person.id, "person", getattr(person, "people_presentation_id", None) or person.id,
            schedule.position if schedule else getattr(person, "position", None),
            bool(getattr(person, "available", True)), bool(getattr(person, "alive", True)),
            int(getattr(person, "health", 0)), int(getattr(person, "max_health", 0)),
            None, None, None, person.role, name, role_label,
        ))
    for threat in state.combatants:
        presentation_id = threat.archetype_id or threat.id
        views.append(ActorView(
            threat.id, "threat", presentation_id, threat.position,
            threat.status in {"watching", "engaged"}, threat.status != "defeated",
            threat.health, threat.max_health, threat.status, threat.intent_id or None,
            threat.goal_id or None, threat.role or None,
            threat_presentation(presentation_id, threat.id), None,
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
