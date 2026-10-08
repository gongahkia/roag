"""Bounded deterministic reactions to transient authoritative facts.

Simulation facts are mechanically meaningful during command resolution, unlike
the renderer-facing records in :mod:`roag.runtime_events`.  This module does
not discover component content or retain facts: callers explicitly provide an
ordered fact batch and the registered component rules that may react to it.
"""

from __future__ import annotations

from dataclasses import dataclass, field

from .circuits import (
    CELL_CHARGE, gain_charge, load_rack_from_pack,
    space_id as active_space_id, spend_charge,
)
from .state import GameState, Position


ACTOR_DEFEATED = "actor.defeated"
THREAT_DETECTED = "threat.detected"
TERRAIN_ACTION = "terrain.action"
RESOURCE_GAINED = "resource.gained"
TRIGGER_IDS = frozenset({
    ACTOR_DEFEATED, THREAT_DETECTED, TERRAIN_ACTION, RESOURCE_GAINED,
})
SOURCE_KINDS = frozenset({"circuit"})
MAX_FACTS = 128
MAX_RULES = 128
MAX_MATCH_CHECKS = 4096


@dataclass(frozen=True)
class ActorDefeatedFact:
    """An actor ceased to be active through another actor's action."""

    actor_id: str
    defeated_by_actor_id: str
    space_id: str
    position: Position
    fact_id: str = field(init=False, default=ACTOR_DEFEATED)

    def __post_init__(self) -> None:
        if (not self.actor_id or not self.defeated_by_actor_id or not self.space_id
                or not isinstance(self.position, Position)):
            raise ValueError("invalid actor-defeated fact")


@dataclass(frozen=True)
class ThreatDetectedFact:
    """A detector observed an active threat in its simulation space."""

    actor_id: str
    detector_id: str
    space_id: str
    position: Position
    fact_id: str = field(init=False, default=THREAT_DETECTED)

    def __post_init__(self) -> None:
        if (not self.actor_id or not self.detector_id or not self.space_id
                or not isinstance(self.position, Position)):
            raise ValueError("invalid threat-detected fact")


@dataclass(frozen=True)
class TerrainActionFact:
    """A courier began one validated physical action against regional terrain."""

    actor_id: str
    action_id: str
    terrain_id: str
    space_id: str
    position: Position
    fact_id: str = field(init=False, default=TERRAIN_ACTION)

    def __post_init__(self) -> None:
        if (
            not self.actor_id
            or self.action_id not in {"break", "cut", "dig"}
            or not self.terrain_id
            or not self.space_id
            or not isinstance(self.position, Position)
        ):
            raise ValueError("invalid terrain-action fact")


@dataclass(frozen=True)
class ResourceGainedFact:
    """A physical item entered an actor's available inventory."""

    actor_id: str
    item_id: str
    item_kind: str
    quantity: int
    space_id: str
    position: Position
    fact_id: str = field(init=False, default=RESOURCE_GAINED)

    def __post_init__(self) -> None:
        if (
            not self.actor_id
            or not self.item_id
            or not self.item_kind
            or type(self.quantity) is not int
            or self.quantity <= 0
            or not self.space_id
            or not isinstance(self.position, Position)
        ):
            raise ValueError("invalid resource-gained fact")


SimulationFact = (
    ActorDefeatedFact | ThreatDetectedFact | TerrainActionFact
    | ResourceGainedFact
)


@dataclass(frozen=True)
class GainCharge:
    """Request bounded charge for the physical component owning a rule."""

    amount: int
    target: ComponentRef | None = None
    effect_id: str = field(init=False, default="gain_charge")

    def __post_init__(self) -> None:
        if (type(self.amount) is not int or self.amount <= 0
                or self.target is not None and not isinstance(self.target, ComponentRef)):
            raise ValueError("charge effect amount must be a positive integer")


@dataclass(frozen=True)
class SpendCharge:
    """Request bounded charge consumption from a physical component."""

    amount: int
    target: ComponentRef
    effect_id: str = field(init=False, default="spend_charge")

    def __post_init__(self) -> None:
        if (
            type(self.amount) is not int
            or self.amount <= 0
            or not isinstance(self.target, ComponentRef)
        ):
            raise ValueError("charge effect amount must be a positive integer")


@dataclass(frozen=True)
class ActorRef:
    """Reference an authoritative actor without making it a component source."""

    actor_id: str

    def __post_init__(self) -> None:
        if not isinstance(self.actor_id, str) or not self.actor_id:
            raise ValueError("invalid actor reference")


@dataclass(frozen=True)
class ConsumeResource:
    """Request all-or-nothing consumption from one actor's physical pack."""

    item_kind: str
    amount: int
    target: ActorRef
    effect_id: str = field(init=False, default="consume_resource")

    def __post_init__(self) -> None:
        if (
            not isinstance(self.item_kind, str)
            or not self.item_kind
            or type(self.amount) is not int
            or self.amount <= 0
            or not isinstance(self.target, ActorRef)
        ):
            raise ValueError("resource effect requires valid identity and quantity")


@dataclass(frozen=True)
class ComponentRef:
    """Reference a concrete component without conflating its representation."""

    kind: str
    instance_id: str

    def __post_init__(self) -> None:
        if self.kind not in SOURCE_KINDS or not isinstance(self.instance_id, str) or not self.instance_id:
            raise ValueError("invalid component reference")


@dataclass(frozen=True)
class LoadRack:
    """Atomically turn one physical galvanic cell into its full rack yield."""

    target: ComponentRef
    actor: ActorRef
    amount: int = field(init=False, default=CELL_CHARGE)
    resource_kind: str = field(init=False, default="circuit:cell")
    resource_amount: int = field(init=False, default=1)
    effect_id: str = field(init=False, default="load_rack")

    def __post_init__(self) -> None:
        if not isinstance(self.target, ComponentRef) or not isinstance(self.actor, ActorRef):
            raise ValueError("rack load requires circuit and actor targets")


SimulationEffect = GainCharge | SpendCharge | ConsumeResource | LoadRack


SimulationTarget = ComponentRef | ActorRef


@dataclass(frozen=True)
class WithinRange:
    """Match facts on one level within a component's Chebyshev range."""

    space_id: str
    origin: Position
    maximum: int

    def __post_init__(self) -> None:
        if (not self.space_id or not isinstance(self.origin, Position)
                or type(self.maximum) is not int or not 1 <= self.maximum <= 12):
            raise ValueError("invalid simulation fact range condition")


SimulationCondition = WithinRange


@dataclass(frozen=True)
class ReactionRule:
    """One registered component reaction using the finite trigger/effect set."""

    rule_id: str
    source: ComponentRef
    trigger_id: str
    effect: SimulationEffect
    priority: int = 0
    condition: SimulationCondition | None = None

    def __post_init__(self) -> None:
        if (not isinstance(self.rule_id, str) or not self.rule_id
                or self.trigger_id not in TRIGGER_IDS
                or not isinstance(self.effect, (
                    GainCharge, SpendCharge, ConsumeResource, LoadRack,
                ))
                or self.condition is not None and not isinstance(self.condition, WithinRange)):
            raise ValueError("invalid component reaction rule")
        if type(self.priority) is not int:
            raise ValueError("reaction priority must be an integer")


@dataclass(frozen=True)
class EffectApplication:
    """Immutable audit result for one state mutation accepted by a component."""

    fact_index: int
    fact_id: str
    rule_id: str
    source: ComponentRef
    target: SimulationTarget
    effect_id: str
    requested_amount: int
    applied_amount: int
    resource_kind: str | None = None
    resource_spent: int = 0


@dataclass(frozen=True)
class SimulationResolution:
    """Transient result of resolving a finite ordered fact/rule set."""

    facts: tuple[SimulationFact, ...]
    applications: tuple[EffectApplication, ...]


@dataclass
class SimulationFactCollector:
    """Command-scoped ordered fact builder; never persistent or global."""

    _facts: list[SimulationFact] = field(default_factory=list)
    _frozen: bool = False

    def emit(self, fact: SimulationFact) -> None:
        if self._frozen:
            raise RuntimeError("simulation fact collector is frozen")
        if not isinstance(fact, (
            ActorDefeatedFact, ThreatDetectedFact, TerrainActionFact,
            ResourceGainedFact,
        )):
            raise TypeError("invalid simulation fact")
        if len(self._facts) >= MAX_FACTS:
            raise RuntimeError("simulation fact limit exceeded")
        self._facts.append(fact)

    def freeze(self) -> tuple[SimulationFact, ...]:
        if self._frozen:
            raise RuntimeError("simulation fact collector is frozen")
        self._frozen = True
        return tuple(self._facts)


def resolve_component_reactions(
    state: GameState,
    facts: tuple[SimulationFact, ...],
    rules: tuple[ReactionRule, ...],
) -> SimulationResolution:
    """Apply matching rules once in deterministic order.

    Effects neither emit more facts nor recursively invoke the resolver.  The
    explicit limits keep a content mistake from turning one command into an
    unbounded rules pass. Rules are ordered independently of registration
    order, while facts retain their authoritative production order.
    """
    if len(facts) > MAX_FACTS or len(rules) > MAX_RULES:
        raise ValueError("simulation reaction input exceeds bounded limits")
    if len(facts) * len(rules) > MAX_MATCH_CHECKS:
        raise ValueError("simulation reaction match budget exceeded")
    if any(not isinstance(
        fact, (
            ActorDefeatedFact, ThreatDetectedFact, TerrainActionFact,
            ResourceGainedFact,
        ),
    ) for fact in facts):
        raise TypeError("invalid simulation fact")
    if any(not isinstance(rule, ReactionRule) for rule in rules):
        raise TypeError("invalid simulation reaction rule")
    ordered_rules = tuple(sorted(rules, key=lambda rule: (rule.priority, rule.rule_id)))
    if len({rule.rule_id for rule in ordered_rules}) != len(ordered_rules):
        raise ValueError("simulation reaction rule IDs must be unique")

    applications: list[EffectApplication] = []
    for fact_index, fact in enumerate(facts):
        for rule in ordered_rules:
            if not _matches(fact, rule):
                continue
            target, applied = _apply_effect(state, fact, rule)
            if applied:
                resource_kind = None
                resource_spent = 0
                if isinstance(rule.effect, ConsumeResource):
                    resource_kind = rule.effect.item_kind
                    resource_spent = applied
                elif isinstance(rule.effect, LoadRack):
                    resource_kind = rule.effect.resource_kind
                    resource_spent = rule.effect.resource_amount
                applications.append(EffectApplication(
                    fact_index=fact_index,
                    fact_id=fact.fact_id,
                    rule_id=rule.rule_id,
                    source=rule.source,
                    target=target,
                    effect_id=rule.effect.effect_id,
                    requested_amount=rule.effect.amount,
                    applied_amount=applied,
                    resource_kind=resource_kind,
                    resource_spent=resource_spent,
                ))
    return SimulationResolution(facts, tuple(applications))


def _matches(fact: SimulationFact, rule: ReactionRule) -> bool:
    if rule.trigger_id != fact.fact_id:
        return False
    if isinstance(rule.condition, WithinRange):
        return (
            fact.space_id == rule.condition.space_id
            and fact.position.z == rule.condition.origin.z
            and max(
                abs(fact.position.x - rule.condition.origin.x),
                abs(fact.position.y - rule.condition.origin.y),
            ) <= rule.condition.maximum
        )
    return True


def _apply_effect(
    state: GameState, fact: SimulationFact, rule: ReactionRule,
) -> tuple[SimulationTarget, int]:
    """Delegate a finite effect to the domain that owns its state mutation."""
    if isinstance(rule.effect, (GainCharge, SpendCharge)) and rule.source.kind == "circuit":
        source = state.circuits.get(rule.source.instance_id)
        target = (
            rule.effect.target
            if isinstance(rule.effect, SpendCharge)
            else rule.effect.target or rule.source
        )
        target_cell = state.circuits.get(target.instance_id)
        if (source is None or source.space != fact.space_id
                or target.kind != "circuit" or target_cell is None
                or target_cell.space != fact.space_id):
            return target, 0
        if isinstance(rule.effect, GainCharge):
            return target, gain_charge(state, target.instance_id, rule.effect.amount)
        return target, spend_charge(state, target.instance_id, rule.effect.amount)
    if isinstance(rule.effect, ConsumeResource) and rule.source.kind == "circuit":
        source = state.circuits.get(rule.source.instance_id)
        target = rule.effect.target
        if (
            source is None
            or source.space != fact.space_id
            or fact.space_id != active_space_id(state)
            or target.actor_id != state.active_courier_id
        ):
            return target, 0
        from .inventory import consume_pack_items

        return target, consume_pack_items(
            state, target.actor_id, rule.effect.item_kind, rule.effect.amount,
        )
    if isinstance(rule.effect, LoadRack) and rule.source.kind == "circuit":
        source = state.circuits.get(rule.source.instance_id)
        target = rule.effect.target
        target_cell = state.circuits.get(target.instance_id)
        if (
            not isinstance(fact, ResourceGainedFact)
            or fact.item_kind != rule.effect.resource_kind
            or fact.actor_id != rule.effect.actor.actor_id
            or source is None
            or source.space != fact.space_id
            or fact.space_id != active_space_id(state)
            or rule.effect.actor.actor_id != state.active_courier_id
            or target.kind != "circuit"
            or target_cell is None
            or target_cell.space != fact.space_id
        ):
            return target, 0
        return target, load_rack_from_pack(
            state, target.instance_id, rule.effect.actor.actor_id,
        )
    return rule.source, 0
