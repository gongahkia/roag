"""Pure, bounded projections of hostile actions the courier can observe."""

from __future__ import annotations

from dataclasses import dataclass

from .state import GameState, Position, Threat
from .world import courier_sees, distance, projectile_path


@dataclass(frozen=True)
class CombatForecast:
    actor_id: str
    actor_name: str
    origin: Position
    target: Position | None
    affected: tuple[Position, ...]
    path: tuple[Position, ...]
    timing: str
    action: str
    counter: str

    @property
    def threatens_courier(self) -> bool:
        return self.target is not None and self.target in self.affected


def _definition(actor: Threat) -> dict | None:
    from .content import ENEMY_ARCHETYPES
    from .frontier_elites import definition

    return definition(actor) or next(
        (row for row in ENEMY_ARCHETYPES.values() if row["name"] == actor.name),
        None,
    )


def _affected_cells(actor: Threat, point: Position) -> tuple[Position, ...]:
    data = _definition(actor)
    mode = data.get("mode", "") if data else ""
    offsets = (-1, 0, 1) if mode in {"surge", "firing", "shutters", "slip"} else (0,)
    return tuple(Position(point.x + dx, point.y, point.z) for dx in offsets)


def observed_forecasts(state: GameState) -> tuple[CombatForecast, ...]:
    """Describe current intent without leaking actors the courier cannot see."""
    forecasts: list[CombatForecast] = []
    for actor in state.combatants:
        if actor.status != "engaged" or not courier_sees(state, actor.position):
            continue
        target = actor.marked_position or actor.aimed_at
        action = actor.intent.rstrip(".")
        if target is None and "charges next turn" in actor.intent:
            target = state.position
        data = _definition(actor)
        counter = str(data.get("counterplay", "move, use cover, guard, or interrupt")) if data else "move, use cover, guard, or interrupt"
        if target is not None:
            affected = _affected_cells(actor, target)
            path = tuple(projectile_path(actor.position, target, state))
            timing = "after your next action"
        else:
            affected, path = (), ()
            timing = (
                f"recovering for {actor.reload_turns} hostile step"
                + ("s" if actor.reload_turns != 1 else "")
                if actor.reload_turns
                else "intent visible; no committed target"
            )
        forecasts.append(CombatForecast(
            actor.id, actor.name, actor.position, target, affected, path,
            timing, action, counter,
        ))
    return tuple(sorted(
        forecasts,
        key=lambda row: (
            row.target is None,
            row.target != state.position if row.target is not None else True,
            distance(state.position, row.origin),
            row.actor_id,
        ),
    ))


def danger_cells(state: GameState, visible: set[Position]) -> set[Position]:
    """Return only forecast cells that are themselves presently observable."""
    return {
        point
        for forecast in observed_forecasts(state)
        if (
            forecast.origin in visible
            or (
                forecast.origin.z != state.position.z
                and courier_sees(state, forecast.origin)
            )
        )
        for point in forecast.affected
        if point in visible
    }


def forecast_lines(forecast: CombatForecast) -> list[str]:
    target = (
        f"{forecast.target.x},{forecast.target.y} z{forecast.target.z:+d}"
        if forecast.target else "none committed"
    )
    path = " -> ".join(
        f"{point.x},{point.y}" for point in forecast.path[:6]
    ) or "no committed path"
    if len(forecast.path) > 6:
        path += " -> ..."
    return [
        f"DANGER: {forecast.actor_name} — {forecast.action}.",
        f"FORECAST: origin {forecast.origin.x},{forecast.origin.y} z{forecast.origin.z:+d}; target {target}; {forecast.timing}.",
        f"PREDICTED PATH: {path}.",
        f"COUNTERS: {forecast.counter}.",
    ]
