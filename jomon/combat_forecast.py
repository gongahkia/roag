"""Pure, bounded projections of hostile actions the courier can observe."""

from __future__ import annotations

from dataclasses import dataclass
from functools import lru_cache

from .state import GameState, Position, Threat
from .ecology_presentation import ecology_actor_counterplay, ecology_format, ecology_text
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
    intent_id: str

    @property
    def threatens_courier(self) -> bool:
        return self.target is not None and self.target in self.affected


@lru_cache(maxsize=128)
def _definition_for(archetype_id: str) -> dict | None:
    from .encounters import threat_definition

    class Identity:
        def __init__(self, identity: str) -> None:
            self.archetype_id = identity
            self.id = ""

    return threat_definition(Identity(archetype_id))


def _affected_cells(actor: Threat, point: Position) -> tuple[Position, ...]:
    data = _definition_for(actor.archetype_id or "")
    mode = data.get("mode", "") if data else ""
    offsets = (-1, 0, 1) if mode in {"surge", "firing", "shutters", "slip"} else (0,)
    return tuple(Position(point.x + dx, point.y, point.z) for dx in offsets)


def observed_forecasts(
    state: GameState, visible: set[Position] | None = None
) -> tuple[CombatForecast, ...]:
    """Describe current intent without leaking actors the courier cannot see."""
    from .world import field_of_view

    visible = visible if visible is not None else field_of_view(state, remember=False)
    forecasts: list[CombatForecast] = []
    for actor in state.combatants:
        actor_visible = actor.position in visible or (
            actor.position.z != state.position.z and courier_sees(state, actor.position)
        )
        if actor.status != "engaged" or not actor_visible:
            continue
        target = actor.marked_position or actor.aimed_at
        action = actor.intent.rstrip(".")
        if target is None and actor.intent_id in {
            "combat.intent.lowers_its_head_and_charges_next_turn",
            "intent.animal.charge_warning",
        }:
            target = state.position
        data = _definition_for(actor.archetype_id or "")
        counter = ecology_actor_counterplay(actor.archetype_id or "", str(data.get("counterplay", "")) if data else ecology_text("forecast.counter.default")) or ecology_text("forecast.counter.default")
        if target is not None:
            affected = _affected_cells(actor, target)
            path = tuple(projectile_path(actor.position, target, state))
            timing = ecology_text("forecast.timing.after_action")
        else:
            affected, path = (), ()
            timing = (
                ecology_format("forecast.timing.recovering", steps=actor.reload_turns, unit=ecology_text("forecast.unit.step" if actor.reload_turns == 1 else "forecast.unit.steps"))
                if actor.reload_turns
                else ecology_text("forecast.timing.visible")
            )
        forecasts.append(CombatForecast(
            actor.id, actor.name, actor.position, target, affected, path,
            timing, action, counter, actor.intent_id,
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
        for forecast in observed_forecasts(state, visible)
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
        if forecast.target else ecology_text("forecast.target.none")
    )
    path = " -> ".join(
        f"{point.x},{point.y}" for point in forecast.path[:6]
    ) or ecology_text("forecast.path.none")
    if len(forecast.path) > 6:
        path += ecology_text("forecast.path.more")
    return [
        ecology_format("forecast.line.danger", actor=forecast.actor_name, action=forecast.action),
        ecology_format("forecast.line.forecast", x=forecast.origin.x, y=forecast.origin.y, z=f"{forecast.origin.z:+d}", target=target, timing=forecast.timing),
        ecology_format("forecast.line.path", path=path),
        ecology_format("forecast.line.counter", counter=forecast.counter),
    ]
