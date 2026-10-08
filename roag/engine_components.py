"""Concrete registrations connecting physical components to simulation facts."""

from __future__ import annotations

from dataclasses import dataclass

from .circuit_presentation import circuit_format
from .circuits import nearest_connected_rack
from .simulation_effects import (
    ActorDefeatedFact,
    ComponentRef,
    GainCharge,
    ReactionRule,
    SimulationFact,
    SimulationResolution,
    WithinRange,
    resolve_component_reactions,
)
from .state import GameState, Position


@dataclass(frozen=True)
class EngineOutcome:
    """Authoritative reaction result plus selected-pack player feedback."""

    resolution: SimulationResolution
    messages: tuple[str, ...] = ()


def registered_reaction_rules(state: GameState) -> tuple[ReactionRule, ...]:
    """Discover finite reactions from the currently fitted physical world."""
    rules: list[ReactionRule] = []
    for sensor_key, sensor in sorted(state.circuits.items()):
        if not (
            sensor.kind == "sensor"
            and sensor.enabled
            and sensor.mode == "threat"
        ):
            continue
        linked = nearest_connected_rack(state, sensor)
        if linked is None:
            continue
        rack_key, _ = linked
        rules.append(ReactionRule(
            rule_id=f"engine.threat_sensor.kill_charge:{sensor_key}",
            source=ComponentRef("circuit", sensor_key),
            trigger_id="actor.defeated",
            effect=GainCharge(1, ComponentRef("circuit", rack_key)),
            condition=WithinRange(sensor.space, sensor.position, sensor.threshold),
        ))
    return tuple(rules)


def resolve_engine_facts(
    state: GameState, facts: tuple[SimulationFact, ...],
) -> EngineOutcome:
    """Resolve current physical registrations once and expose bounded feedback."""
    resolution = resolve_component_reactions(
        state, facts, registered_reaction_rules(state),
    )
    messages: list[str] = []
    for application in resolution.applications:
        if application.effect_id != "gain_charge":
            continue
        source = state.circuits.get(application.source.instance_id)
        target = state.circuits.get(application.target.instance_id)
        if source is None or target is None:
            continue
        message = circuit_format(
            "circuit.event.kill_charge",
            amount=application.applied_amount,
            x=source.position.x,
            y=source.position.y,
        )
        source.last_event = message
        target.last_event = message
        state.add_message(message, priority=3)
        messages.append(message)
    return EngineOutcome(resolution, tuple(messages))


def resolve_actor_defeat(
    state: GameState, actor_id: str, defeated_by_actor_id: str,
    position: Position,
) -> EngineOutcome:
    """Create the authoritative fact at the defeat producer, not from UI events."""
    space = f"region:{state.active_region_id}" if state.location == "region" else "vessel"
    fact = ActorDefeatedFact(actor_id, defeated_by_actor_id, space, position)
    return resolve_engine_facts(state, (fact,))
