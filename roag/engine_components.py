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
    SpendCharge,
    TerrainActionFact,
    WithinRange,
    resolve_component_reactions,
)
from .state import GameState, Position


@dataclass(frozen=True)
class EngineOutcome:
    """Authoritative reaction result plus selected-pack player feedback."""

    resolution: SimulationResolution
    messages: tuple[str, ...] = ()


@dataclass(frozen=True)
class TerrainAssistance:
    """Bounded mechanical contribution to one validated terrain action."""

    power: int
    sound: int
    outcome: EngineOutcome


def registered_reaction_rules(state: GameState) -> tuple[ReactionRule, ...]:
    """Discover finite reactions from the currently fitted physical world."""
    active_space = (
        f"region:{state.active_region_id}"
        if state.location == "region"
        else "vessel"
    )
    rules: list[ReactionRule] = []
    for sensor_key, sensor in sorted(state.circuits.items()):
        if not (
            sensor.space == active_space
            and sensor.kind == "sensor"
            and sensor.enabled
            and sensor.mode in {"mass", "threat"}
        ):
            continue
        linked = nearest_connected_rack(state, sensor)
        if linked is None:
            continue
        rack_key, _ = linked
        source = ComponentRef("circuit", sensor_key)
        target = ComponentRef("circuit", rack_key)
        if sensor.mode == "threat":
            rules.append(ReactionRule(
                rule_id=f"engine.threat_sensor.kill_charge:{sensor_key}",
                source=source,
                trigger_id="actor.defeated",
                effect=GainCharge(1, target),
                condition=WithinRange(
                    sensor.space, sensor.position, sensor.threshold,
                ),
            ))
        elif sensor.mode == "mass":
            rules.append(ReactionRule(
                rule_id=f"engine.mass_sensor.terrain_power:{sensor_key}",
                source=source,
                trigger_id="terrain.action",
                effect=SpendCharge(1, target),
                condition=WithinRange(sensor.space, sensor.position, 1),
            ))
    return tuple(rules)


def resolve_engine_facts(
    state: GameState,
    facts: tuple[SimulationFact, ...],
    *,
    rules: tuple[ReactionRule, ...] | None = None,
) -> EngineOutcome:
    """Resolve current physical registrations once and expose bounded feedback."""
    resolution = resolve_component_reactions(
        state,
        facts,
        registered_reaction_rules(state) if rules is None else rules,
    )
    messages: list[str] = []
    for application in resolution.applications:
        if application.effect_id not in {"gain_charge", "spend_charge"}:
            continue
        source = state.circuits.get(application.source.instance_id)
        target = state.circuits.get(application.target.instance_id)
        if source is None or target is None:
            continue
        message_id = (
            "circuit.event.kill_charge"
            if application.effect_id == "gain_charge"
            else "circuit.event.terrain_assist"
        )
        message = circuit_format(
            message_id,
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


def resolve_terrain_assistance(
    state: GameState,
    actor_id: str,
    action_id: str,
    terrain_id: str,
    position: Position,
    maximum_power: int,
) -> TerrainAssistance:
    """Spend fitted engine charge to amplify one validated physical action."""
    if type(maximum_power) is not int or maximum_power < 0:
        raise ValueError("terrain assistance limit must be a non-negative integer")
    space = f"region:{state.active_region_id}"
    fact = TerrainActionFact(actor_id, action_id, terrain_id, space, position)
    available_rules = tuple(
        rule
        for rule in registered_reaction_rules(state)
        if (
            rule.trigger_id == fact.fact_id
            and isinstance(rule.effect, SpendCharge)
            and state.circuits.get(rule.effect.target.instance_id) is not None
            and state.circuits[rule.effect.target.instance_id].charge > 0
        )
    )
    outcome = resolve_engine_facts(
        state,
        (fact,),
        rules=available_rules[:maximum_power],
    )
    spent = sum(
        application.applied_amount
        for application in outcome.resolution.applications
        if application.effect_id == "spend_charge"
    )
    return TerrainAssistance(spent, spent, outcome)
