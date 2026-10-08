"""Deterministic regional pressure policy and bounded danger directives.

This module owns pressure interpretation and the existing consequences of its
bands.  It does not advance time, spawn actors, consume RNG, or choose how the
terminal presents danger.  Callers evaluate immutable directives, then apply
them synchronously inside the authoritative world step.
"""

from __future__ import annotations

from dataclasses import dataclass

from .action_presentation import action_format
from .state import GameState


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
class DangerAction:
    """One finite deterministic pressure response selected by the director."""

    action_id: str
    band: str
    target_id: str | None = None


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
    score = state.pressure_elapsed // 18 + depth + state.noise // 2 + valuables
    if score >= 18:
        band, alert, pursuit = "critical", 12, 2
    elif score >= 10:
        band, alert, pursuit = "strained", 9, 1
    else:
        band, alert, pursuit = "steady", 6, 1
    return Pressure(
        state.pressure_elapsed, depth, state.noise, valuables, score,
        band, alert, pursuit,
    )


def evaluate_danger_step(state: GameState) -> tuple[DangerAction, ...]:
    """Select existing per-step pressure consequences without applying them."""
    profile = pressure(state)
    if (
        state.location != "region"
        or profile.band != "critical"
        or state.escalation_spawned
    ):
        return ()
    dormant = next(
        (actor for actor in state.combatants if actor.status == "dormant"),
        None,
    )
    return (
        DangerAction(
            "danger.critical_escalation",
            profile.band,
            dormant.id if dormant is not None else None,
        ),
    )


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
        else:
            raise ValueError(f"unknown danger action {action.action_id!r}")
    return tuple(messages)


def resolve_danger_step(state: GameState) -> DangerOutcome:
    """Evaluate and apply the existing in-step pressure response."""
    actions = evaluate_danger_step(state)
    return DangerOutcome(pressure(state), actions, apply_danger_actions(state, actions))


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
