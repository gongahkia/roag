"""Zero-time contextual inspection and concise production action hints."""

from __future__ import annotations

from dataclasses import dataclass

from .catalog import CatalogError, WORLD_TEXT_SECTIONS, load_catalog
from .material_presentation import fluid_display_name, material_format, material_text
from .state import GameState, Position
from .world import (
    base_tile,
    courier_sees,
    displayed_tile,
    distance,
    is_walkable,
    position_key,
    vertical_destination,
)


_TERRAIN_NAMES = load_catalog("world_text.json", WORLD_TEXT_SECTIONS)["terrain_names"]
if (not isinstance(_TERRAIN_NAMES, dict)
        or any(not isinstance(glyph, str) or len(glyph) != 1
               or not isinstance(name, str) or not name
               for glyph, name in _TERRAIN_NAMES.items())):
    raise CatalogError("world_text.json has invalid terrain names")
TERRAIN_NAMES = _TERRAIN_NAMES


@dataclass(frozen=True)
class MovementPreview:
    destination: Position
    legal: bool
    time_cost: str
    consequence: str
    remedy: str = ""


def blocked_step_reason(state: GameState, destination: Position) -> tuple[str, str]:
    """Name the physical obstruction and a useful next verb."""
    tile = base_tile(state, destination)
    if tile == "#":
        return material_text("material.inspection.blocked.stone"), material_text("material.inspection.remedy.stone")
    if tile == "T":
        return material_text("material.inspection.blocked.timber"), material_text("material.inspection.remedy.timber")
    if tile == "~":
        return material_text("material.inspection.blocked.water"), material_text("material.inspection.remedy.water")
    if tile == " ":
        return material_text("material.inspection.blocked.empty"), material_text("material.inspection.remedy.empty")
    if state.location == "jomon" and tile in {"=", "t", "F", "f"}:
        return material_text("material.inspection.blocked.vessel"), material_text("material.inspection.remedy.vessel")
    return material_format("material.inspection.blocked.generic", terrain=TERRAIN_NAMES.get(tile, "A physical obstruction")), material_text("material.inspection.remedy.generic")


def movement_preview(state: GameState, destination: Position) -> MovementPreview:
    """Explain an adjacent step without mutating or pretending exact path cost."""
    dx, dy = destination.x - state.position.x, destination.y - state.position.y
    if destination.z != state.position.z or max(abs(dx), abs(dy)) != 1:
        return MovementPreview(destination, False, material_text("material.inspection.time.none"), material_text("material.inspection.preview.nonadjacent"))
    occupant = next((
        actor for actor in state.combatants
        if actor.position == destination and actor.status in {"watching", "engaged"}
    ), None)
    if occupant:
        if occupant.status == "watching":
            return MovementPreview(destination, False, material_text("material.inspection.time.one"), material_format("material.inspection.preview.watching", actor=occupant.name), material_text("material.inspection.preview.watching_remedy"))
        return MovementPreview(destination, False, material_text("material.inspection.time.none"), material_format("material.inspection.preview.engaged", actor=occupant.name), material_text("material.inspection.preview.engaged_remedy"))
    if not is_walkable(state, destination):
        reason, remedy = blocked_step_reason(state, destination)
        return MovementPreview(destination, False, material_text("material.inspection.time.none"), reason.rstrip("."), remedy.rstrip("."))
    if dx and dy:
        side_a = Position(state.position.x + dx, state.position.y, state.position.z)
        side_b = Position(state.position.x, state.position.y + dy, state.position.z)
        if not is_walkable(state, side_a, ignore_threat=True) and not is_walkable(state, side_b, ignore_threat=True):
            return MovementPreview(destination, False, material_text("material.inspection.time.none"), material_text("material.inspection.preview.diagonal"), material_text("material.inspection.preview.diagonal_remedy"))

    from .inventory import load_state, terrain_status_for
    from .materials import fields, key

    tile = displayed_tile(state, destination)
    cell = fields(state).get(key(destination))
    consequences: list[str] = []
    slow = load_state(state) in {"encumbered", "overloaded"}
    status = terrain_status_for(state, tile)
    if status:
        name, cause, _, effect = status
        consequences.append(material_format("material.inspection.preview.status", status=name.replace("-", " "), cause=cause, effect=effect))
        slow = slow or name in {"bogged", "current"}
    if cell:
        if cell.fire:
            consequences.append(material_format("material.inspection.preview.fire", fire=cell.fire))
        if cell.smoke >= 2:
            consequences.append(material_format("material.inspection.preview.smoke", smoke=cell.smoke))
        if cell.collapse_due:
            consequences.append(material_format("material.inspection.preview.collapse", beats=max(0, cell.collapse_due - state.world_time)))
        if cell.water and not status:
            consequences.append(material_format("material.inspection.preview.water", fluid=fluid_display_name(cell.fluid), water=cell.water))
    if base_tile(state, destination) == "+":
        consequences.append(material_text("material.inspection.preview.door"))
    if base_tile(state, destination) == "O":
        consequences.append(material_text("material.inspection.preview.fall"))
    if not consequences:
        consequences.append(material_format("material.inspection.preview.enter", terrain=TERRAIN_NAMES.get(tile, "passable ground")))
    return MovementPreview(
        destination, True, material_text("material.inspection.time.possibly_two" if slow else "material.inspection.time.one"),
        "; ".join(consequences),
        material_text("material.inspection.preview.avoid") if slow or status else "",
    )


def _knowledge(state: GameState, point: Position) -> str:
    if courier_sees(state, point):
        return "visible"
    if state.location == "region" and position_key(point) in state.region.seen:
        return "remembered"
    return "unknown"


def inspect_lines(state: GameState, point: Position) -> list[str]:
    knowledge = _knowledge(state, point)
    if knowledge == "unknown":
        return [
            material_format("material.inspection.unknown", x=point.x, y=point.y, z=f"{point.z:+d}"),
            material_text("material.inspection.unknown.detail"),
            material_text("material.inspection.no_time"),
        ]
    tile = displayed_tile(state, point) if knowledge == "visible" else base_tile(state, point)
    prefix = material_text("material.inspection.prefix.visible" if knowledge == "visible" else "material.inspection.prefix.remembered")
    lines = [
        material_format("material.inspection.line", prefix=prefix, x=point.x, y=point.y, z=f"{point.z:+d}", terrain=TERRAIN_NAMES.get(tile, "worked terrain"), tile=tile)
    ]
    if knowledge == "remembered":
        lines.extend((
            material_text("material.inspection.remembered.detail"),
            material_text("material.inspection.remembered.return"),
            material_text("material.inspection.no_time"),
        ))
        return lines

    actor = next((
        row for row in state.combatants
        if row.position == point and row.status in {"watching", "engaged"}
    ), None)
    if actor:
        from .combat_forecast import forecast_lines, observed_forecasts
        lines.append(material_format("material.inspection.actor", actor=actor.name, health=actor.health, maximum=actor.max_health, morale=actor.morale, intent=actor.intent))
        forecast = next((row for row in observed_forecasts(state) if row.actor_id == actor.id), None)
        if forecast:
            lines.extend(forecast_lines(forecast))
    if state.location == "region":
        container = next((row for row in state.region.containers if row.position == point), None)
        if container and container.hidden and not container.discovered:
            if max(abs(point.x - state.position.x), abs(point.y - state.position.y)) <= 3:
                lines.append(material_format("material.inspection.trace", clue=container.clue))
        elif container:
            lines.append(material_format("material.inspection.container", container=container.name, state=material_text("material.inspection.container.opened" if container.opened else "material.inspection.container.closed")))
    ground = [
        item for item in state.items
        if item.location == "ground" and item.ground_position == point
        and item.region_id == state.spatial_id
    ]
    if ground:
        from .inventory import item_spec
        lines.append(material_format("material.inspection.ground", items=", ".join(f"{item_spec(item.kind).name} {item.condition}%" for item in ground)))
    from .circuits import PARTS, active as circuit_active, cell_at

    circuit = cell_at(state, point)
    if circuit:
        detail = material_format("material.inspection.circuit.rack", charge=circuit.charge) if circuit.kind == "rack" else material_text("material.inspection.circuit.active" if circuit_active(state, circuit) else "material.inspection.circuit.inactive")
        lines.append(material_format("material.inspection.circuit", fitting=PARTS[circuit.kind]["name"], phase=circuit.phase, detail=detail))
    from .materials import fields, inspect_material, key
    if key(point) in fields(state) or tile in {",", "~", "_", "m", "r", "q", "t", "s", "%", "f"}:
        lines.extend(inspect_material(state, point))
    transition = vertical_destination(state, point)
    if transition:
        direction = material_text("material.inspection.direction.descend" if transition.z < point.z else "material.inspection.direction.climb")
        lines.append(material_format("material.inspection.transition", direction=direction, z=f"{transition.z:+d}"))
    if point.z == state.position.z and distance(state.position, point) == 1:
        preview = movement_preview(state, point)
        lines.append(material_format("material.inspection.prediction", legal=material_text("material.inspection.legal" if preview.legal else "material.inspection.blocked"), time=preview.time_cost, consequence=preview.consequence))
        if preview.remedy:
            lines.append(material_format("material.inspection.counter", remedy=preview.remedy))
    lines.append(material_text("material.inspection.footer"))
    return lines


def contextual_hints(
    state: GameState, limit: int = 2, *, forecasts=None
) -> tuple[str, ...]:
    """Return a stable, short list of presently useful verbs."""
    hints: list[str] = []
    from .combat_forecast import observed_forecasts

    forecasts = observed_forecasts(state) if forecasts is None else forecasts
    danger = next((row for row in forecasts if state.position in row.affected), None)
    if danger:
        hints.append(material_format("material.inspection.hint.danger", actor=danger.actor_name))
    if state.combat_active:
        from .manoeuvres import known, status
        for row in known(state):
            ready, _ = status(state, row.id)
            if ready:
                hints.append(material_format("material.inspection.hint.manoeuvre", manoeuvre=row.name, effect=row.effect))
                break
    if vertical_destination(state, state.position):
        transition = vertical_destination(state, state.position)
        verb = material_text("material.inspection.direction.descend" if transition.z < state.position.z else "material.inspection.direction.climb")
        hints.append(material_format("material.inspection.hint.transition", direction=verb, z=f"{transition.z:+d}"))
    if state.location == "region" and not state.combat_active:
        # Full target enumeration parses every remembered cell and belongs in
        # the T overlay, not the per-frame status path.
        if len(state.region.seen) > 1:
            hints.append(material_text("material.inspection.hint.map"))
    if not hints:
        hints.append(material_text("material.inspection.hint.default"))
    return tuple(hints[:limit])


def contextual_advice(state: GameState, speaker: str) -> str:
    """Let an embodied adult teach one presently relevant, transferable rule."""
    from .combat_forecast import observed_forecasts

    forecast = next((row for row in observed_forecasts(state) if state.position in row.affected), None)
    if forecast:
        return material_format("material.inspection.advice.forecast", speaker=speaker, actor=forecast.actor_name, counter=forecast.counter)
    if state.terrain_statuses:
        name, status = next(iter(state.terrain_statuses.items()))
        return material_format("material.inspection.advice.status", speaker=speaker, status=name.replace("-", " "), cause=status.cause, consequence=status.consequence)
    from .materials import fields
    nearby = [
        cell for coordinate, cell in fields(state).items()
        if (point := _point(coordinate)) is not None
        and point.z == state.position.z and distance(point, state.position) <= 2
    ]
    if any(cell.fire for cell in nearby):
        return material_format("material.inspection.advice.fire", speaker=speaker)
    if any(cell.smoke >= 2 for cell in nearby):
        return material_format("material.inspection.advice.smoke", speaker=speaker)
    if any(cell.collapse_due for cell in nearby):
        return material_format("material.inspection.advice.collapse", speaker=speaker)
    from .calendar import calendar_at
    season = calendar_at(state).season
    if state.weather == "hard rain":
        return material_format("material.inspection.advice.rain", speaker=speaker)
    if "wind" in state.weather or "gust" in state.weather:
        return material_format("material.inspection.advice.wind", speaker=speaker)
    if season == "winter":
        return material_format("material.inspection.advice.winter", speaker=speaker)
    from .world import build_combinations
    combo = next(iter(build_combinations(state)), None)
    if combo:
        return material_format("material.inspection.advice.combo", speaker=speaker, combo=combo)
    return material_format("material.inspection.advice.default", speaker=speaker)


def _point(coordinate: str) -> Position | None:
    try:
        return Position(*(int(value) for value in coordinate.split(",")))
    except (TypeError, ValueError):
        return None
