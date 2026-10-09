"""Concrete registrations connecting physical components to simulation facts."""

from __future__ import annotations

from dataclasses import dataclass

from .circuit_presentation import circuit_format
from .circuits import (
    MASS_SENSOR_ENGINE_RANGE, SENSOR_CHARGE_STEP,
    TERRAIN_ASSIST_POWER_PER_CHARGE, TERRAIN_ASSIST_SOUND_PER_CHARGE,
    nearest_connected_rack, sensor_threshold,
)
from .simulation_effects import (
    ACTOR_DEFEATED,
    RESOURCE_GAINED,
    TERRAIN_ACTION,
    ActorRef,
    ActorDefeatedFact,
    ComponentRef,
    GainCharge,
    LoadRack,
    ReactionRule,
    ResourceGainedFact,
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


def registered_reaction_rules(
    state: GameState,
    trigger_ids: frozenset[str] | None = None,
) -> tuple[ReactionRule, ...]:
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
            and sensor.mode in {"mass", "threat", "supply"}
        ):
            continue
        linked = nearest_connected_rack(state, sensor)
        if linked is None:
            continue
        rack_key, _ = linked
        source = ComponentRef("circuit", sensor_key)
        target = ComponentRef("circuit", rack_key)
        if sensor.mode == "threat":
            if trigger_ids is None or ACTOR_DEFEATED in trigger_ids:
                rules.append(ReactionRule(
                    rule_id=f"engine.threat_sensor.kill_charge:{sensor_key}",
                    source=source,
                    trigger_id=ACTOR_DEFEATED,
                    effect=GainCharge(SENSOR_CHARGE_STEP, target),
                    condition=WithinRange(
                        sensor.space, sensor.position, sensor_threshold(state, sensor),
                    ),
                ))
        elif sensor.mode == "mass":
            if trigger_ids is None or TERRAIN_ACTION in trigger_ids:
                rules.append(ReactionRule(
                    rule_id=f"engine.mass_sensor.terrain_power:{sensor_key}",
                    source=source,
                    trigger_id=TERRAIN_ACTION,
                    effect=SpendCharge(SENSOR_CHARGE_STEP, target),
                    condition=WithinRange(
                        sensor.space, sensor.position, MASS_SENSOR_ENGINE_RANGE,
                    ),
                ))
            if trigger_ids is None or RESOURCE_GAINED in trigger_ids:
                rules.append(ReactionRule(
                    rule_id=f"engine.mass_sensor.resource_charge:{sensor_key}",
                    source=source,
                    trigger_id=RESOURCE_GAINED,
                    effect=GainCharge(SENSOR_CHARGE_STEP, target),
                    condition=WithinRange(
                        sensor.space, sensor.position, MASS_SENSOR_ENGINE_RANGE,
                    ),
                ))
        elif sensor.mode == "supply":
            if trigger_ids is None or RESOURCE_GAINED in trigger_ids:
                rules.append(ReactionRule(
                    rule_id=f"engine.supply_sensor.load_rack:{sensor_key}",
                    source=source,
                    trigger_id=RESOURCE_GAINED,
                    effect=LoadRack(
                        target, ActorRef(state.active_courier_id or "courier"),
                    ),
                    priority=-10,
                    condition=WithinRange(
                        sensor.space, sensor.position, sensor_threshold(state, sensor),
                    ),
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
        registered_reaction_rules(
            state, frozenset(fact.fact_id for fact in facts),
        ) if rules is None else rules,
    )
    messages: list[str] = []
    for application in resolution.applications:
        if application.effect_id not in {
            "gain_charge", "spend_charge", "load_rack",
        }:
            continue
        source = state.circuits.get(application.source.instance_id)
        target = state.circuits.get(application.target.instance_id)
        if source is None or target is None:
            continue
        message_id = {
            ("gain_charge", ACTOR_DEFEATED): "circuit.event.kill_charge",
            ("gain_charge", RESOURCE_GAINED): "circuit.event.resource_charge",
            ("spend_charge", TERRAIN_ACTION): "circuit.event.terrain_assist",
            ("load_rack", RESOURCE_GAINED): "circuit.event.supply_load",
        }.get((application.effect_id, application.fact_id))
        if message_id is None:
            continue
        message = circuit_format(
            message_id,
            amount=application.applied_amount,
            x=source.position.x,
            y=source.position.y,
        )
        source.last_event = message
        target.last_event = message
        if state.run is not None and state.run.status == "active":
            from .run_items import effect_value

            if source.kind == "circuit" and source.instance_id in state.circuits:
                source_cell = state.circuits[source.instance_id]
                if source_cell.mode == "threat":
                    if effect_value(state, "grant_guard"):
                        state.guarded_step = True
                    state.run.damage_charge = max(
                        state.run.damage_charge,
                        effect_value(state, "damage_charge"),
                    )
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


def resolve_resource_gained(
    state: GameState,
    item_id: str,
    item_kind: str,
    quantity: int,
    position: Position,
) -> EngineOutcome:
    """React after a physical item has successfully entered the active pack."""
    space = f"region:{state.active_region_id}" if state.location == "region" else "vessel"
    fact = ResourceGainedFact(
        state.active_courier_id or "courier",
        item_id,
        item_kind,
        quantity,
        space,
        position,
    )
    outcome = resolve_engine_facts(state, (fact,))
    if state.run is not None and state.run.status == "active":
        from .circuits import gain_charge, space_id
        from .run_items import effect_value

        amount = effect_value(state, "gain_charge", family="circuit")
        racks = sorted(
            key for key, cell in state.circuits.items()
            if cell.space == space_id(state) and cell.kind == "rack"
        )
        if amount and racks:
            gain_charge(state, racks[0], amount)
    return outcome


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
        for rule in registered_reaction_rules(state, frozenset({fact.fact_id}))
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
    return TerrainAssistance(
        spent * TERRAIN_ASSIST_POWER_PER_CHARGE,
        spent * TERRAIN_ASSIST_SOUND_PER_CHARGE,
        outcome,
    )
