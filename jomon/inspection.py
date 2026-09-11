"""Zero-time contextual inspection and concise production action hints."""

from __future__ import annotations

from dataclasses import dataclass

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


TERRAIN_NAMES = {
    " ": "unmapped space", ".": "firm ground", ",": "shallow water",
    "~": "deep water", "_": "ice", "m": "mud", "r": "scree",
    "q": "sharp limestone", "t": "dense growth", "T": "standing timber",
    "#": "stone wall", "+": "closed door", "/": "open doorway",
    "=": "worked timber", "%": "loose cover", "O": "open drop",
    "<": "upward connection", ">": "downward connection", "w": "current",
    "&": "working control", "R": "cargo", "C": "closed store",
    "o": "opened store", "M": "regional contact", "c": "local witness",
}


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
        return "A stone wall blocks that step.", "Use another lane; marked weak structure can be inspected with F."
    if tile == "T":
        return "Standing timber blocks that step.", "Follow a gap or inspect whether cutting is legal with F."
    if tile == "~":
        return "Deep water blocks ordinary footing.", "Find shallows, a crossing, or a disclosed water-control action."
    if tile == " ":
        return "There is no traversable floor there.", "Use a mapped entrance or aligned vertical connection."
    if state.location == "jomon" and tile in {"=", "t", "F", "f"}:
        return "Fixed vessel structure blocks that step.", "Use the doorway or interact with a nearby working station."
    return f"{TERRAIN_NAMES.get(tile, 'A physical obstruction')} blocks that step.", "Inspect with ; or take another lane."


def movement_preview(state: GameState, destination: Position) -> MovementPreview:
    """Explain an adjacent step without mutating or pretending exact path cost."""
    dx, dy = destination.x - state.position.x, destination.y - state.position.y
    if destination.z != state.position.z or max(abs(dx), abs(dy)) != 1:
        return MovementPreview(destination, False, "no time", "not an adjacent step")
    occupant = next((
        actor for actor in state.combatants
        if actor.position == destination and actor.status in {"watching", "engaged"}
    ), None)
    if occupant:
        if occupant.status == "watching":
            return MovementPreview(destination, False, "one action", f"wakes {occupant.name}; it occupies the cell", "attack, negotiate, or take another lane")
        return MovementPreview(destination, False, "no time", f"{occupant.name} holds the cell", "attack, control, or take another lane")
    if not is_walkable(state, destination):
        reason, remedy = blocked_step_reason(state, destination)
        return MovementPreview(destination, False, "no time", reason.rstrip("."), remedy.rstrip("."))
    if dx and dy:
        side_a = Position(state.position.x + dx, state.position.y, state.position.z)
        side_b = Position(state.position.x, state.position.y + dy, state.position.z)
        if not is_walkable(state, side_a, ignore_threat=True) and not is_walkable(state, side_b, ignore_threat=True):
            return MovementPreview(destination, False, "no time", "the diagonal is pinched closed", "take an orthogonal step")

    from .inventory import load_state, terrain_status_for
    from .materials import fields, key

    tile = displayed_tile(state, destination)
    cell = fields(state).get(key(destination))
    consequences: list[str] = []
    slow = load_state(state) in {"encumbered", "overloaded"}
    status = terrain_status_for(state, tile)
    if status:
        name, cause, _, effect = status
        consequences.append(f"{name.replace('-', ' ')} from {cause}: {effect}")
        slow = slow or name in {"bogged", "current"}
    if cell:
        if cell.fire:
            consequences.append(f"fire {cell.fire}/3 burns bodies and possessions")
        if cell.smoke >= 2:
            consequences.append(f"smoke {cell.smoke}/4 obscures sight and strains breath")
        if cell.collapse_due:
            consequences.append(f"support is warned to collapse at action {cell.collapse_due}")
        if cell.water and not status:
            consequences.append(f"{cell.fluid} water depth {cell.water}/3 wets load")
    if base_tile(state, destination) == "+":
        consequences.append("the door opens and changes sightlines")
    if base_tile(state, destination) == "O":
        consequences.append("the step becomes a fall to the aligned level below")
    if not consequences:
        consequences.append(f"enter {TERRAIN_NAMES.get(tile, 'passable ground')}")
    return MovementPreview(
        destination, True, "possibly two actions" if slow else "one action",
        "; ".join(consequences),
        "use equipment or another lane to avoid the disclosed pressure" if slow or status else "",
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
            f"UNKNOWN: {point.x},{point.y} z{point.z:+d} has not been observed.",
            "No actor, material reaction, or current hazard is inferred there.",
            "Inspection costs no time.",
        ]
    tile = displayed_tile(state, point) if knowledge == "visible" else base_tile(state, point)
    prefix = "FACT" if knowledge == "visible" else "REMEMBERED"
    lines = [
        f"{prefix}: {point.x},{point.y} z{point.z:+d}; {TERRAIN_NAMES.get(tile, 'worked terrain')} ({tile})."
    ]
    if knowledge == "remembered":
        lines.extend((
            "MEMORY: terrain only; actors and active material states are not carried by memory.",
            "Return to sight before relying on fire, smoke, water, cargo, or structural state.",
            "Inspection costs no time.",
        ))
        return lines

    actor = next((
        row for row in state.combatants
        if row.position == point and row.status in {"watching", "engaged"}
    ), None)
    if actor:
        from .combat_forecast import forecast_lines, observed_forecasts
        lines.append(f"OBSERVED: {actor.name}; {actor.health}/{actor.max_health} health; morale {actor.morale}; intent {actor.intent}.")
        forecast = next((row for row in observed_forecasts(state) if row.actor_id == actor.id), None)
        if forecast:
            lines.extend(forecast_lines(forecast))
    if state.location == "region":
        container = next((row for row in state.region.containers if row.position == point), None)
        if container:
            lines.append(f"FACT: {container.name}; {'opened' if container.opened else 'closed'} physical store. E from beside it inspects or transfers contents.")
    ground = [
        item for item in state.items
        if item.location == "ground" and item.ground_position == point
        and item.region_id == state.spatial_id
    ]
    if ground:
        from .inventory import item_spec
        lines.append("PHYSICAL OBJECTS: " + ", ".join(f"{item_spec(item.kind).name} {item.condition}%" for item in ground) + ".")
    from .materials import fields, inspect_material, key
    if key(point) in fields(state) or tile in {",", "~", "_", "m", "r", "q", "t", "s", "%", "f"}:
        lines.extend(inspect_material(state, point))
    transition = vertical_destination(state, point)
    if transition:
        direction = "descend" if transition.z < point.z else "climb"
        lines.append(f"ACTION: stand here and press E to {direction} to z{transition.z:+d}; one action.")
    if point.z == state.position.z and distance(state.position, point) == 1:
        preview = movement_preview(state, point)
        lines.append(
            f"PREDICTION: moving here is {'LEGAL' if preview.legal else 'BLOCKED'}; {preview.time_cost}; {preview.consequence}."
        )
        if preview.remedy:
            lines.append(f"COUNTER: {preview.remedy}.")
    lines.append("Inspection costs no time; committing movement, E, F, A, G or M follows its displayed cost.")
    return lines


def contextual_hints(state: GameState, limit: int = 2) -> tuple[str, ...]:
    """Return a stable, short list of presently useful verbs."""
    hints: list[str] = []
    from .combat_forecast import observed_forecasts

    danger = next((row for row in observed_forecasts(state) if state.position in row.affected), None)
    if danger:
        hints.append(f"[!] {danger.actor_name}: move/cover/G before next hostile step")
    if state.combat_active:
        from .manoeuvres import known, status
        for row in known(state):
            ready, _ = status(state, row.id)
            if ready:
                hints.append(f"[M] {row.name}: {row.effect}; one action")
                break
    if vertical_destination(state, state.position):
        transition = vertical_destination(state, state.position)
        verb = "descend" if transition.z < state.position.z else "climb"
        hints.append(f"[E] {verb} to z{transition.z:+d}; one action")
    if state.location == "region" and not state.combat_active:
        from .navigation import navigation_targets
        if navigation_targets(state):
            hints.append("[T] follow remembered ground; any key interrupts")
    if not hints:
        hints.append("[;] inspect a cell; no time")
    return tuple(hints[:limit])


def contextual_advice(state: GameState, speaker: str) -> str:
    """Let an embodied adult teach one presently relevant, transferable rule."""
    from .combat_forecast import observed_forecasts

    forecast = next((row for row in observed_forecasts(state) if state.position in row.affected), None)
    if forecast:
        return f"ADVICE — {speaker}: {forecast.actor_name}'s mark resolves after your next action; {forecast.counter}."
    if state.terrain_statuses:
        name, status = next(iter(state.terrain_statuses.items()))
        return f"ADVICE — {speaker}: {name.replace('-', ' ')} came from {status.cause}; {status.consequence}."
    from .materials import fields
    nearby = [
        cell for coordinate, cell in fields(state).items()
        if (point := _point(coordinate)) is not None
        and point.z == state.position.z and distance(point, state.position) <= 2
    ]
    if any(cell.fire for cell in nearby):
        return f"ADVICE — {speaker}: water removes fire immediately; cutting dry fuel first can stop the next spread."
    if any(cell.smoke >= 2 for cell in nearby):
        return f"ADVICE — {speaker}: dense smoke breaks prepared lanes and sight; wind and clear ground determine its useful edge."
    if any(cell.collapse_due for cell in nearby):
        return f"ADVICE — {speaker}: a warned support falls only after its shown action; leave, brace, or interrupt before then."
    from .calendar import calendar_at
    season = calendar_at(state).season
    if state.weather == "hard rain":
        return f"ADVICE — {speaker}: rain checks open flame but slows exposed travel unless clothing, route, or shelter answers it."
    if "wind" in state.weather or "gust" in state.weather:
        return f"ADVICE — {speaker}: the named wind shifts smoke and firing value consistently; read it before committing a lane."
    if season == "winter":
        return f"ADVICE — {speaker}: fresh shallows can freeze; salt water thaws them, while cleats turn the firm sheet into a route."
    from .world import build_combinations
    combo = next(iter(build_combinations(state)), None)
    if combo:
        return f"ADVICE — {speaker}: your {combo} is active now; its equipment and footing are already satisfying the combination."
    return f"ADVICE — {speaker}: inspect with ; before committing; remembered ground never reveals a present actor or reaction."


def _point(coordinate: str) -> Position | None:
    try:
        return Position(*(int(value) for value in coordinate.split(",")))
    except (TypeError, ValueError):
        return None
