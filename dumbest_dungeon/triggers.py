"""Typed automatic-trigger contracts and a finite-cycle proof for the queue."""

from __future__ import annotations

from dataclasses import dataclass
from enum import IntEnum, StrEnum


class Phase(IntEnum):
    REPLACE = 0
    BEFORE = 1
    PRIMARY = 2
    AFTER = 3
    DEATH = 4
    CLEANUP = 5


class EventType(StrEnum):
    CARD_PLAY = "card_play"
    DAMAGE = "damage"
    BLOCK = "block"
    HEAL = "heal"
    STRESS = "stress"
    MOVE = "move"
    STATUS = "status"
    DRAW = "draw"
    DISCARD = "discard"
    ENERGY = "energy"
    GUARD = "guard"
    CLEANSE = "cleanse"
    TURN_START = "turn_start"
    TURN_END = "turn_end"
    DEATH = "death"
    CLEANUP = "cleanup"


class LimitKind(StrEnum):
    ROOT = "once_per_root"
    CARD = "once_per_card"
    TURN = "once_per_turn"
    COMBAT = "once_per_combat"
    CHARGES = "finite_charges"
    RETRIGGERS = "max_retriggers"
    FAMILY = "exclude_proc_family"


@dataclass(frozen=True)
class Limiter:
    kind: LimitKind
    amount: int = 1
    family: str | None = None

    def __post_init__(self) -> None:
        if not isinstance(self.kind, LimitKind) or type(self.amount) is not int or not 1 <= self.amount <= 10000:
            raise ValueError("trigger limiter requires a registered kind and 1..10000 finite capacity")
        if self.kind in {LimitKind.ROOT, LimitKind.CARD, LimitKind.TURN, LimitKind.COMBAT} and self.amount != 1:
            raise ValueError("once limiters have exactly one use per scope")
        if (self.kind == LimitKind.FAMILY) != (isinstance(self.family, str) and bool(self.family)):
            raise ValueError("proc-family limiter requires exactly one excluded family")


@dataclass(frozen=True)
class TriggerSpec:
    id: str
    listens: EventType
    emits: tuple[EventType, ...]
    phase: Phase = Phase.AFTER
    priority: int = 0
    limiter: Limiter | None = None
    proc_family: str = "automatic"

    def __post_init__(self) -> None:
        if not isinstance(self.id, str) or not self.id or not isinstance(self.proc_family, str) or not self.proc_family:
            raise ValueError("trigger identity and proc family must be nonempty")
        if (not isinstance(self.listens, EventType) or not isinstance(self.phase, Phase)
            or not isinstance(self.emits, tuple) or any(not isinstance(event, EventType) for event in self.emits)
            or type(self.priority) is not int or abs(self.priority) > 10000):
            raise ValueError("invalid trigger event, phase or priority")
        if self.limiter is not None and not isinstance(self.limiter, Limiter):
            raise ValueError("invalid trigger limiter")
        if self.limiter and self.limiter.kind == LimitKind.FAMILY and self.limiter.family != self.proc_family:
            raise ValueError("a cycle-limiting exclusion must exclude the trigger's own proc family")


def strongly_connected(graph: dict[str, tuple[str, ...]]) -> tuple[tuple[str, ...], ...]:
    visited, order = set(), []
    for start in sorted(graph):
        if start in visited:
            continue
        stack = [(start, False)]
        while stack:
            node, finished = stack.pop()
            if finished:
                order.append(node)
            elif node not in visited:
                visited.add(node)
                stack.append((node, True))
                stack.extend((neighbor, False) for neighbor in reversed(graph[node]) if neighbor not in visited)
    reverse = {node: [] for node in graph}
    for node, neighbors in graph.items():
        for neighbor in neighbors:
            reverse[neighbor].append(node)
    visited, components = set(), []
    for start in reversed(order):
        if start in visited:
            continue
        component, pending = [], [start]
        visited.add(start)
        while pending:
            node = pending.pop()
            component.append(node)
            for neighbor in sorted(reverse[node], reverse=True):
                if neighbor not in visited:
                    visited.add(neighbor)
                    pending.append(neighbor)
        components.append(tuple(sorted(component)))
    return tuple(sorted(components))


def validate_trigger_graph(specs: tuple[TriggerSpec, ...]) -> dict[str, tuple[str, ...]]:
    identities = {spec.id for spec in specs}
    if len(identities) != len(specs):
        raise ValueError("duplicate automatic trigger ID")
    graph = {source.id: tuple(sorted(target.id for target in specs if target.listens in source.emits))
             for source in sorted(specs, key=lambda spec: spec.id)}
    unlimited = {spec.id for spec in specs if spec.limiter is None}
    residual = {node: tuple(neighbor for neighbor in graph[node] if neighbor in unlimited) for node in sorted(unlimited)}
    for component in strongly_connected(residual):
        if len(component) > 1 or component[0] in residual[component[0]]:
            raise ValueError("unbounded automatic trigger cycle: " + " -> ".join(component))
    return graph
