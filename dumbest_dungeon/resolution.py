"""Deterministic, resumable automatic-event dispatch with finite chain isolation."""

from __future__ import annotations

import json
from copy import deepcopy
from dataclasses import asdict, dataclass, field
from typing import Callable

from .contracts import Opcode
from .manifest import canonical_bytes
from .triggers import EventType, LimitKind, Limiter, Phase, TriggerSpec


@dataclass(frozen=True)
class Payload:
    actor_id: str | None = None
    opcode: Opcode | None = None
    amount: int = 0
    status: str | None = None
    bonus_status: str | None = None
    bonus: int = 0
    card_id: str | None = None
    raw_damage: bool = False
    card_upgraded: bool = False
    effect_index: int | None = None

    def __post_init__(self) -> None:
        if type(self.amount) is not int or type(self.bonus) is not int:
            raise ValueError("event magnitudes must be integers")
        if self.opcode is not None and not isinstance(self.opcode, Opcode):
            raise ValueError("event opcode must be registered")
        if type(self.raw_damage) is not bool or self.raw_damage and self.opcode != Opcode.DAMAGE:
            raise ValueError("raw damage requires the damage opcode")
        if type(self.card_upgraded) is not bool or self.effect_index is not None and (type(self.effect_index) is not int or self.effect_index < 0):
            raise ValueError("invalid card continuation payload")
        if any(value is not None and (not isinstance(value, str) or not value)
               for value in (self.actor_id, self.status, self.bonus_status, self.card_id)):
            raise ValueError("event references must be nonempty strings")


@dataclass(frozen=True)
class Event:
    event_id: int
    root_action_id: int
    parent_event_id: int | None
    depth: int
    event_type: EventType
    source_id: str
    target_ids: tuple[str, ...]
    payload: Payload
    ancestry: tuple[int, ...] = ()
    proc_families: tuple[str, ...] = ()
    chain_id: str | None = None
    mandatory: bool = False
    deferred: bool = False


@dataclass(frozen=True)
class Listener:
    spec: TriggerSpec
    creation_id: int
    entity_id: str

    def __post_init__(self) -> None:
        if not isinstance(self.spec, TriggerSpec) or type(self.creation_id) is not int or self.creation_id < 0 or not isinstance(self.entity_id, str) or not self.entity_id:
            raise ValueError("invalid listener identity")

    @property
    def order(self) -> tuple:
        return self.spec.phase, self.spec.priority, self.creation_id, self.spec.id


@dataclass
class Dispatch:
    event: Event
    listeners: tuple[Listener, ...]
    phase: int = 0
    cursor: int = 0
    primary_done: bool = False
    prevented: bool = False


@dataclass
class QueueState:
    schema: int = 3
    next_event_id: int = 1
    next_root_id: int = 1
    root_id: int | None = None
    card_token: str | None = None
    combat_token: int = 0
    turn_token: int = 0
    pending: list[Event] = field(default_factory=list)
    active: Dispatch | None = None
    counters: dict[str, int] = field(default_factory=dict)
    chain_spent: dict[str, int] = field(default_factory=dict)
    sealed_chains: list[str] = field(default_factory=list)
    trace: list[dict] = field(default_factory=list)
    seals: list[dict] = field(default_factory=list)


class EventQueue:
    def __init__(self, state: QueueState | None = None, *, budget: int = 4096):
        if type(budget) is not int or not 1 <= budget <= 100000:
            raise ValueError("root event budget must be 1..100000")
        self.state = state or QueueState()
        self.budget = budget
        self._listener: Listener | None = None
        self._inside_callback = False

    def begin(self, *, card_token: str | None = None, combat_token: int = 0, turn_token: int = 0) -> int:
        if self.state.root_id is not None:
            raise ValueError("finish the current root action before beginning another")
        self.state.root_id = self.state.next_root_id
        self.state.next_root_id += 1
        self.state.card_token = card_token
        self.state.combat_token = combat_token
        self.state.turn_token = turn_token
        self.state.chain_spent.clear()
        self.state.sealed_chains.clear()
        self.state.trace.clear()
        keep = {}
        for key, value in self.state.counters.items():
            scope = json.loads(key)
            if scope[0] == "combat" and scope[1] == combat_token or scope[0] == "turn" and scope[1:3] == [combat_token, turn_token]:
                keep[key] = value
        self.state.counters = keep
        return self.state.root_id

    def submit(self, event_type: EventType, source_id: str, target_ids: tuple[str, ...], payload: Payload) -> Event:
        if self.state.root_id is None or self.state.active is not None:
            raise ValueError("manual events require an open root between dispatch calls")
        event = self._event(event_type, source_id, target_ids, payload)
        self.state.pending.append(event)
        return event

    def _event(self, event_type, source_id, target_ids, payload, *, parent=None, family=None, chain_id=None, mandatory=False, deferred=False) -> Event:
        if (not isinstance(event_type, EventType) or not isinstance(payload, Payload)
            or not isinstance(source_id, str) or not source_id or not isinstance(target_ids, tuple)
            or any(not isinstance(identity, str) or not identity for identity in target_ids)):
            raise ValueError("invalid typed event")
        event = Event(self.state.next_event_id, self.state.root_id, parent.event_id if parent else None,
                      parent.depth + 1 if parent else 0, event_type, source_id, target_ids, payload,
                      parent.ancestry + (parent.event_id,) if parent else (),
                      parent.proc_families + ((family,) if family else ()) if parent else (), chain_id, mandatory, deferred)
        self.state.next_event_id += 1
        return event

    def emit(self, event_type: EventType, target_ids: tuple[str, ...], payload: Payload,
             *, source_id: str | None = None, mandatory: bool = False, deferred: bool = False) -> Event | None:
        frame = self.state.active
        if frame is None or not self._inside_callback:
            raise ValueError("automatic events require an active dispatch callback")
        listener = self._listener
        if listener and (mandatory or deferred or event_type not in listener.spec.emits):
            raise ValueError("trigger emitted an undeclared event or requested mandatory dispatch")
        parent = frame.event
        source = source_id or (listener.spec.id if listener else parent.source_id)
        chain = parent.chain_id if mandatory else parent.chain_id or f"{parent.event_id}:{source}"
        if chain is not None and not mandatory:
            if chain in self.state.sealed_chains:
                return None
            if sum(self.state.chain_spent.values()) >= self.budget:
                culprit = min(self.state.chain_spent, key=lambda identity: (-self.state.chain_spent[identity], identity))
                branch = [event for event in self.state.pending if event.chain_id == culprit]
                witness = max(branch, key=lambda event: (event.depth, event.event_id)) if branch else parent
                seal = {"message": "CHAIN SEALED", "root_action_id": self.state.root_id,
                        "chain_id": culprit, "event_id": witness.event_id,
                        "trigger_event_id": parent.event_id,
                        "ancestry": list(witness.ancestry + (witness.event_id,)),
                        "trace": list(self.state.trace)}
                self.state.seals.append(seal)
                self.state.trace.append({key: value for key, value in seal.items() if key != "trace"})
                self.state.sealed_chains.append(culprit)
                self.state.pending = [event for event in self.state.pending if event.chain_id != culprit or event.mandatory]
                del self.state.chain_spent[culprit]
                if chain == culprit:
                    return None
            self.state.chain_spent[chain] = self.state.chain_spent.get(chain, 0) + 1
        event = self._event(event_type, source, target_ids, payload, parent=parent,
                            family=listener.spec.proc_family if listener else None, chain_id=chain, mandatory=mandatory, deferred=deferred)
        self.state.pending.append(event)
        return event

    def prevent(self) -> None:
        if self.state.active is None or self.state.active.phase != Phase.REPLACE:
            raise ValueError("prevention is only legal in the replace phase")
        self.state.active.prevented = True

    def _limited(self, listener: Listener, event: Event) -> bool:
        if event.chain_id in self.state.sealed_chains:
            return True
        limiter = listener.spec.limiter
        if limiter is None:
            return False
        if limiter.kind == LimitKind.FAMILY:
            return limiter.family in event.proc_families
        if limiter.kind == LimitKind.CARD and self.state.card_token is None:
            return True
        key = self._counter_key(listener)
        return self.state.counters.get(key, 0) >= limiter.amount

    def _counter_key(self, listener: Listener) -> str:
        limiter = listener.spec.limiter
        if limiter.kind in {LimitKind.COMBAT, LimitKind.CHARGES}:
            scope = ["combat", self.state.combat_token]
        elif limiter.kind == LimitKind.TURN:
            scope = ["turn", self.state.combat_token, self.state.turn_token]
        else:
            scope = ["root", self.state.root_id]
        return canonical_bytes(scope + [listener.spec.id, listener.creation_id]).decode("ascii")

    def step(self, listeners: Callable[[Event], tuple[Listener, ...]],
             primary: Callable[[Event, EventQueue], None],
             trigger: Callable[[Listener, Event, EventQueue], None]) -> bool:
        if self._inside_callback:
            raise ValueError("callbacks must enqueue descendants, not recursively dispatch")
        if self.state.active is None:
            if not self.state.pending:
                return False
            index = next((index for index, event in enumerate(self.state.pending) if not event.deferred), 0)
            event = self.state.pending.pop(index)
            snapshot = tuple(sorted((item for item in listeners(event) if item.spec.listens == event.event_type), key=lambda item: item.order))
            if len(snapshot) > 1024:
                raise ValueError("an event supports at most 1024 registered listeners")
            self.state.active = Dispatch(event, snapshot)
        frame = self.state.active
        if frame.event.chain_id in self.state.sealed_chains and not frame.event.mandatory:
            self.state.active = None
            return True
        phase = Phase(frame.phase)
        if phase == Phase.PRIMARY and not frame.primary_done:
            frame.primary_done = True
            if not frame.prevented:
                self.state.trace.append({"event_id": frame.event.event_id, "source_id": frame.event.source_id,
                                         "phase": phase.name, "depth": frame.event.depth,
                                         "parent_event_id": frame.event.parent_event_id,
                                         "root_action_id": frame.event.root_action_id, "chain_id": frame.event.chain_id,
                                         "event_type": frame.event.event_type.value,
                                         "target_ids": list(frame.event.target_ids), "payload": asdict(frame.event.payload)})
                self._inside_callback = True
                try:
                    primary(frame.event, self)
                finally:
                    self._inside_callback = False
            return True
        candidates = tuple(item for item in frame.listeners if item.spec.phase == phase)
        while frame.cursor < len(candidates):
            listener = candidates[frame.cursor]
            frame.cursor += 1
            if self._limited(listener, frame.event):
                continue
            entry = {"event_id": frame.event.event_id, "source_id": listener.spec.id,
                     "phase": phase.name, "creation_id": listener.creation_id,
                     "depth": frame.event.depth, "activated": True}
            self.state.trace.append(entry)
            self._listener, self._inside_callback = listener, True
            next_event = self.state.next_event_id
            try:
                activated = trigger(listener, frame.event, self) is not False
                entry["activated"] = activated
                if not activated and self.state.next_event_id != next_event:
                    raise ValueError("an inactive trigger cannot emit events")
                if activated and listener.spec.limiter and listener.spec.limiter.kind != LimitKind.FAMILY:
                    key = self._counter_key(listener)
                    self.state.counters[key] = self.state.counters.get(key, 0) + 1
            finally:
                self._listener, self._inside_callback = None, False
            return True
        frame.phase += 1
        frame.cursor = 0
        if frame.phase > Phase.CLEANUP:
            self.state.active = None
        return True

    def drain(self, listeners, primary, trigger, *, close_root: bool = True) -> None:
        while self.step(listeners, primary, trigger):
            pass
        if close_root:
            self.finish()

    def finish(self) -> None:
        if self.state.pending or self.state.active is not None:
            raise ValueError("cannot finish a root with unresolved events")
        self.state.root_id = None

    def snapshot(self) -> dict:
        if self._inside_callback:
            raise ValueError("checkpoint between dispatch steps, not inside a callback")
        return {"budget": self.budget, "state": asdict(self.state)}

    @classmethod
    def from_snapshot(cls, snapshot: dict) -> EventQueue:
        if not isinstance(snapshot, dict) or set(snapshot) != {"budget", "state"}:
            raise ValueError("invalid event queue snapshot")
        raw = deepcopy(snapshot["state"])
        if not isinstance(raw, dict) or set(raw) != set(QueueState.__dataclass_fields__):
            raise ValueError("invalid event queue state fields")

        def event(data):
            if not isinstance(data, dict) or set(data) != set(Event.__dataclass_fields__):
                raise ValueError("invalid queued event fields")
            data = dict(data)
            if not isinstance(data["payload"], dict) or set(data["payload"]) != set(Payload.__dataclass_fields__):
                raise ValueError("invalid event payload fields")
            operands = dict(data["payload"])
            operands["opcode"] = Opcode(operands["opcode"]) if operands["opcode"] is not None else None
            data["payload"] = Payload(**operands)
            data["event_type"] = EventType(data["event_type"])
            for key in ("target_ids", "ancestry", "proc_families"):
                if not isinstance(data[key], (tuple, list)):
                    raise ValueError("invalid event sequence")
                data[key] = tuple(data[key])
            restored = Event(**data)
            if (type(restored.event_id) is not int or not 1 <= restored.event_id < raw["next_event_id"]
                or type(restored.root_action_id) is not int or restored.root_action_id != raw["root_id"] or type(restored.depth) is not int
                or restored.depth != len(restored.ancestry)
                or type(restored.mandatory) is not bool
                or type(restored.deferred) is not bool
                or not isinstance(restored.source_id, str) or not restored.source_id
                or any(not isinstance(identity, str) or not identity for identity in restored.target_ids + restored.proc_families)
                or (restored.parent_event_id != restored.ancestry[-1] if restored.ancestry else restored.parent_event_id is not None)
                or any(type(identity) is not int or not 1 <= identity < restored.event_id for identity in restored.ancestry)):
                raise ValueError("invalid queued event ancestry")
            return restored

        def listener(data):
            data = dict(data)
            spec = dict(data["spec"])
            spec["listens"] = EventType(spec["listens"])
            spec["emits"] = tuple(EventType(value) for value in spec["emits"])
            spec["phase"] = Phase(spec["phase"])
            if spec["limiter"] is not None:
                limiter = dict(spec["limiter"])
                limiter["kind"] = LimitKind(limiter["kind"])
                spec["limiter"] = Limiter(**limiter)
            data["spec"] = TriggerSpec(**spec)
            return Listener(**data)

        try:
            if (type(raw["schema"]) is not int or raw["schema"] != 3 or any(type(raw[key]) is not int or raw[key] < 1 for key in ("next_event_id", "next_root_id"))
                or raw["root_id"] is not None and (type(raw["root_id"]) is not int or not 1 <= raw["root_id"] < raw["next_root_id"])):
                raise ValueError("invalid queue version or root identity")
            if (any(type(raw[key]) is not int or raw[key] < 0 for key in ("combat_token", "turn_token"))
                or raw["card_token"] is not None and (not isinstance(raw["card_token"], str) or not raw["card_token"])
                or any(not isinstance(raw[key], list) for key in ("pending", "trace", "seals", "sealed_chains"))
                or any(not isinstance(identity, str) or not identity for identity in raw["sealed_chains"])):
                raise ValueError("invalid queue context or trace")
            pending = [event(row) for row in raw["pending"]]
            active = raw["active"]
            if active is not None:
                if not isinstance(active, dict) or set(active) != set(Dispatch.__dataclass_fields__):
                    raise ValueError("invalid dispatch fields")
                active = Dispatch(event(active["event"]), tuple(listener(row) for row in active["listeners"]),
                                  active["phase"], active["cursor"], active["primary_done"], active["prevented"])
                if (type(active.phase) is not int or not 0 <= active.phase <= 5 or type(active.cursor) is not int
                    or not 0 <= active.cursor <= sum(item.spec.phase == active.phase for item in active.listeners)
                    or type(active.primary_done) is not bool or type(active.prevented) is not bool
                    or tuple(sorted(active.listeners, key=lambda item: item.order)) != active.listeners):
                    raise ValueError("invalid active dispatch phase or listener snapshot")
            if raw["root_id"] is None and (pending or active):
                raise ValueError("pending events have no root action")
            ids = [row.event_id for row in pending] + ([active.event.event_id] if active else [])
            if len(set(ids)) != len(ids):
                raise ValueError("duplicate queued event identity")
            for key in ("counters", "chain_spent"):
                if not isinstance(raw[key], dict) or any(type(value) is not int or value < 0 for value in raw[key].values()):
                    raise ValueError("invalid queue counters")
            for key in raw["counters"]:
                scope = json.loads(key)
                if (not isinstance(scope, list) or len(scope) not in {4, 5} or scope[0] not in {"root", "turn", "combat"}
                    or not isinstance(scope[-2], str) or type(scope[-1]) is not int
                    or any(type(value) is not int or value < 0 for value in scope[1:-2])):
                    raise ValueError("invalid queue limiter scope")
            canonical_bytes(snapshot)
            state = QueueState(**{**raw, "pending": pending, "active": active})
            return cls(state, budget=snapshot["budget"])
        except (KeyError, TypeError, ValueError) as exc:
            raise ValueError(f"invalid event queue snapshot: {exc}") from exc
