"""Transient renderer-neutral semantic outputs from deterministic commands.

These records are command-scoped application outputs.  They are not saved,
consumed by simulation, or related to ``state.SoundEvent`` noise stimuli.
"""

from __future__ import annotations

from dataclasses import dataclass, field

from .state import Position


@dataclass(frozen=True)
class ActorMoved:
    actor_id: str
    from_position: Position
    to_position: Position
    movement_kind_id: str
    event_id: str = field(init=False, default="actor.moved")


@dataclass(frozen=True)
class InteractionResolved:
    actor_id: str
    target_id: str
    interaction_id: str
    result_id: str
    event_id: str = field(init=False, default="interaction.resolved")


@dataclass(frozen=True)
class AttackResolved:
    attacker_id: str
    target_id: str
    action_id: str
    result_id: str
    event_id: str = field(init=False, default="combat.attack.resolved")


@dataclass(frozen=True)
class DamageApplied:
    source_actor_id: str
    target_actor_id: str
    amount: int
    damage_kind_id: str
    hit_location_id: str
    event_id: str = field(init=False, default="combat.damage.applied")


@dataclass(frozen=True)
class StatusChanged:
    actor_id: str
    previous_status_id: str
    status_id: str
    event_id: str = field(init=False, default="actor.status.changed")


@dataclass(frozen=True)
class ActorDefeated:
    actor_id: str
    defeated_by_actor_id: str
    event_id: str = field(init=False, default="actor.defeated")


@dataclass(frozen=True)
class ItemUsed:
    actor_id: str
    item_id: str
    usage_id: str
    event_id: str = field(init=False, default="item.used")


@dataclass(frozen=True)
class CarriedRelicSelectionChanged:
    actor_id: str
    relic_id: str | None
    event_id: str = field(init=False, default="relic.selection.changed")


@dataclass(frozen=True)
class GuardResolved:
    actor_id: str
    target_actor_id: str | None
    event_id: str = field(init=False, default="combat.guard.resolved")


@dataclass(frozen=True)
class RetreatResolved:
    actor_id: str
    from_position: Position
    to_position: Position
    event_id: str = field(init=False, default="combat.retreat.resolved")


@dataclass(frozen=True)
class TerrainDamaged:
    actor_id: str
    position: Position
    terrain_id: str
    action_id: str
    amount: int
    remaining: int
    event_id: str = field(init=False, default="terrain.damaged")


@dataclass(frozen=True)
class TerrainChanged:
    actor_id: str
    position: Position
    previous_terrain_id: str
    terrain_id: str
    action_id: str
    event_id: str = field(init=False, default="terrain.changed")


@dataclass(frozen=True)
class ThreatSpawned:
    actor_id: str
    archetype_id: str
    position: Position
    pressure_band: str
    event_id: str = field(init=False, default="threat.spawned")


RuntimeEvent = (
    ActorMoved | InteractionResolved | AttackResolved | DamageApplied
    | StatusChanged | ActorDefeated | ItemUsed | CarriedRelicSelectionChanged
    | GuardResolved | RetreatResolved | TerrainDamaged | TerrainChanged
    | ThreatSpawned
)


@dataclass(frozen=True)
class RuntimeEventStep:
    """The renderer-neutral events emitted during one world-clock step."""

    step_index: int
    events: tuple[RuntimeEvent, ...] = ()


@dataclass(frozen=True)
class RuntimeEventBatch:
    """Transient semantic output for one submitted command.

    ``command_events`` preserve command-resolution order. ``steps`` preserve
    authoritative clock order, including clock steps that emitted no runtime
    events. Nothing in this record is simulation input or persisted state.
    """

    command_events: tuple[RuntimeEvent, ...] = ()
    steps: tuple[RuntimeEventStep, ...] = ()

    @property
    def events(self) -> tuple[RuntimeEvent, ...]:
        """Compatibility order: command events followed by world-step events."""
        return self.command_events + tuple(
            event for step in self.steps for event in step.events
        )


@dataclass
class RuntimeEventCollector:
    """Mutable command-scoped builder for one :class:`RuntimeEventBatch`.

    Runtime-event producers retain their existing ownership. Callers record
    each event exactly once here, then use the frozen batch for both new and
    compatibility consumers. This is deliberately not a global event bus.
    """

    _command_events: list[RuntimeEvent] = field(default_factory=list)
    _steps: list[RuntimeEventStep] = field(default_factory=list)
    _current_step_events: list[RuntimeEvent] | None = None
    _frozen: bool = False

    @property
    def step_count(self) -> int:
        return len(self._steps)

    def record_command_event(self, event: RuntimeEvent) -> None:
        self._require_open()
        self._command_events.append(event)

    def record_command_events(self, events: tuple[RuntimeEvent, ...]) -> None:
        self._require_open()
        self._command_events.extend(events)

    def begin_step(self) -> None:
        self._require_open()
        if self._current_step_events is not None:
            raise RuntimeError("runtime event step is already open")
        self._current_step_events = []

    def record_step_event(self, event: RuntimeEvent) -> None:
        self._require_open()
        if self._current_step_events is None:
            raise RuntimeError("runtime event step is not open")
        self._current_step_events.append(event)

    def end_step(self) -> None:
        self._require_open()
        if self._current_step_events is None:
            raise RuntimeError("runtime event step is not open")
        self._steps.append(
            RuntimeEventStep(len(self._steps) + 1, tuple(self._current_step_events))
        )
        self._current_step_events = None

    def record_empty_steps(self, count: int) -> None:
        """Record already-resolved legacy clock steps that emitted no events."""
        if count < 0:
            raise ValueError("runtime event step count cannot be negative")
        for _ in range(count):
            self.begin_step()
            self.end_step()

    def freeze(self) -> RuntimeEventBatch:
        self._require_open()
        if self._current_step_events is not None:
            raise RuntimeError("cannot freeze a runtime event batch with an open step")
        self._frozen = True
        return RuntimeEventBatch(tuple(self._command_events), tuple(self._steps))

    def _require_open(self) -> None:
        if self._frozen:
            raise RuntimeError("runtime event collector is frozen")
