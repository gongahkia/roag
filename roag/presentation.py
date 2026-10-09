"""Transient presentation state that never participates in simulation."""

from __future__ import annotations

from dataclasses import dataclass, field
import hashlib
import os
from typing import Mapping

from .runtime_events import (
    ActorDefeated, ActorMoved, AreaResolved, AreaTelegraphed, AttackResolved,
    AttackTelegraphed,
    CarriedRelicSelectionChanged, DamageApplied, GuardResolved, ItemUsed,
    ProjectileResolved, RetreatResolved, RuntimeEvent, RuntimeEventBatch, StatusChanged,
    TerrainChanged, TerrainDamaged, ThreatSpawned,
)
from .state import Position


PRESENTATION_FRAME_MS = 100
WATER_GLYPHS = ("~", "≈", "≋", "≈")
MAX_TRANSIENT_EFFECTS = 256
_DISABLED_VALUES = frozenset({"0", "false", "no", "off"})


def presentation_enabled() -> bool:
    """Return whether the terminal should use timed presentation frames."""
    return os.environ.get("ROAG_PRESENTATION", "on").strip().lower() not in _DISABLED_VALUES


@dataclass(frozen=True)
class MapEffect:
    """One renderer-neutral transient glyph treatment at a world position."""

    effect_id: str
    position: Position
    glyphs: tuple[str, ...]
    emphasis_id: str
    started_ms: int
    priority: int
    sequence: int
    visible_only: bool = True

    def __post_init__(self) -> None:
        if not self.glyphs or any(len(glyph) != 1 for glyph in self.glyphs):
            raise ValueError("presentation effect glyphs must be single characters")
        if self.started_ms < 0 or self.priority < 0 or self.sequence < 0:
            raise ValueError("presentation effect timing and ordering must be non-negative")

    @property
    def ended_ms(self) -> int:
        return self.started_ms + len(self.glyphs) * PRESENTATION_FRAME_MS

    def glyph_at(self, elapsed_ms: int) -> str | None:
        if not self.started_ms <= elapsed_ms < self.ended_ms:
            return None
        frame = (elapsed_ms - self.started_ms) // PRESENTATION_FRAME_MS
        return self.glyphs[frame]


@dataclass
class EffectState:
    """UI-owned clock and transient effect state for one play session.

    It deliberately contains no ``GameState`` reference and is never saved.
    Later presentation effects can grow here without becoming simulation input.
    """

    enabled: bool = True
    elapsed_ms: int = 0
    seed_offset: int = 0
    effects: list[MapEffect] = field(default_factory=list)
    _next_sequence: int = 0

    @classmethod
    def for_seed(cls, seed: str, *, enabled: bool = True) -> "EffectState":
        digest = hashlib.blake2b(seed.encode("utf-8"), digest_size=2).digest()
        return cls(enabled=enabled, seed_offset=int.from_bytes(digest, "big"))

    @property
    def frame_index(self) -> int:
        return self.elapsed_ms // PRESENTATION_FRAME_MS

    def advance(self, milliseconds: int = PRESENTATION_FRAME_MS) -> None:
        if milliseconds < 0:
            raise ValueError("presentation time cannot move backwards")
        if self.enabled:
            self.elapsed_ms += milliseconds
            self.effects[:] = [
                effect for effect in self.effects
                if effect.ended_ms > self.elapsed_ms
            ]

    def consume(
        self,
        batch: RuntimeEventBatch,
        actor_positions: Mapping[str, Position] | None = None,
    ) -> None:
        """Schedule a command batch without feeding presentation into gameplay."""
        if not self.enabled:
            return
        actor_positions = actor_positions or {}
        projectile_attacks = _projectile_attacks(batch.command_events)
        for index, event in enumerate(batch.command_events):
            self._schedule_event(
                event,
                actor_positions,
                self.elapsed_ms + index * PRESENTATION_FRAME_MS,
                suppress_ranged_impact=(
                    isinstance(event, AttackResolved)
                    and (event.attacker_id, event.target_position) in projectile_attacks
                ),
            )
        step_offset = len(batch.command_events)
        for step in batch.steps:
            projectile_attacks = _projectile_attacks(step.events)
            started_ms = self.elapsed_ms + (
                step_offset + step.step_index - 1
            ) * PRESENTATION_FRAME_MS
            for event in step.events:
                self._schedule_event(
                    event,
                    actor_positions,
                    started_ms,
                    suppress_ranged_impact=(
                        isinstance(event, AttackResolved)
                        and (event.attacker_id, event.target_position) in projectile_attacks
                    ),
                )
        if len(self.effects) > MAX_TRANSIENT_EFFECTS:
            self.effects[:] = self.effects[-MAX_TRANSIENT_EFFECTS:]

    def _schedule_event(
        self,
        event: RuntimeEvent,
        actor_positions: Mapping[str, Position],
        started_ms: int,
        *,
        suppress_ranged_impact: bool = False,
    ) -> None:
        if isinstance(event, ProjectileResolved):
            self._schedule_projectile(event, started_ms)
            return
        if isinstance(event, AreaTelegraphed):
            for position in event.cells:
                self._append_effect(
                    "world.area.telegraph", position, ("!", "·", "!"),
                    "warning", started_ms, 5,
                )
            return
        if isinstance(event, AreaResolved):
            ordered_cells = sorted(
                event.cells,
                key=lambda position: (
                    abs(position.x - event.origin.x)
                    + abs(position.y - event.origin.y)
                    + abs(position.z - event.origin.z),
                    position.z,
                    position.y,
                    position.x,
                ),
            )
            for index, position in enumerate(ordered_cells):
                self._append_effect(
                    "world.area.resolved", position, ("*", ":", "."),
                    "debris", started_ms + index * PRESENTATION_FRAME_MS, 6,
                )
            return
        for specification in _effect_specifications(
            event, actor_positions, suppress_ranged_impact=suppress_ranged_impact,
        ):
            effect_id, position, glyphs, emphasis_id, priority = specification
            self._append_effect(
                effect_id, position, glyphs, emphasis_id, started_ms, priority,
            )

    def _schedule_projectile(
        self,
        event: ProjectileResolved,
        started_ms: int,
    ) -> None:
        previous = event.origin
        interior = event.path[1:-1]
        for index, position in enumerate(interior):
            self._append_effect(
                f"combat.projectile.{event.projectile_id}",
                position,
                (_projectile_glyph(previous, position), "."),
                "trail",
                started_ms + index * PRESENTATION_FRAME_MS,
                4,
            )
            previous = position
        if event.result_id == "combat.enemy.hit":
            glyphs, emphasis_id, priority = ("*", "+", "*"), "flash", 6
        elif event.result_id in {"combat.enemy.missed", "combat.enemy.blocked"}:
            glyphs, emphasis_id, priority = ("x", "."), "trail", 3
        else:
            glyphs, emphasis_id, priority = ("*", "."), "motion", 4
        self._append_effect(
            "combat.projectile.impact",
            event.target_position,
            glyphs,
            emphasis_id,
            started_ms + len(interior) * PRESENTATION_FRAME_MS,
            priority,
        )

    def _append_effect(
        self,
        effect_id: str,
        position: Position,
        glyphs: tuple[str, ...],
        emphasis_id: str,
        started_ms: int,
        priority: int,
    ) -> None:
        self.effects.append(MapEffect(
            effect_id,
            position,
            glyphs,
            emphasis_id,
            started_ms,
            priority,
            self._next_sequence,
        ))
        self._next_sequence += 1

    def effect_at(self, position: Position, *, visible: bool = True) -> MapEffect | None:
        if not self.enabled:
            return None
        candidates = [
            effect for effect in self.effects
            if effect.position == position
            and effect.glyph_at(self.elapsed_ms) is not None
            and (visible or not effect.visible_only)
        ]
        return max(
            candidates,
            key=lambda effect: (effect.priority, effect.sequence),
            default=None,
        )

    def glyph(
        self,
        authoritative_glyph: str,
        position: Position,
        *,
        visible: bool = True,
    ) -> str:
        """Compose ambient and transient glyphs over authoritative state."""
        if not self.enabled:
            return authoritative_glyph
        effect = self.effect_at(position, visible=visible)
        return (
            effect.glyph_at(self.elapsed_ms)
            if effect is not None
            else self.ambient_glyph(authoritative_glyph, position)
        )

    def ambient_glyph(self, authoritative_glyph: str, position: Position) -> str:
        """Compose deterministic idle terrain without scanning active effects."""
        if not self.enabled or authoritative_glyph != "~":
            return authoritative_glyph
        offset = self.seed_offset + position.x * 3 + position.y * 5 + position.z * 7
        return WATER_GLYPHS[(self.frame_index + offset) % len(WATER_GLYPHS)]


def _effect_specifications(
    event: RuntimeEvent,
    actor_positions: Mapping[str, Position],
    *,
    suppress_ranged_impact: bool = False,
) -> tuple[tuple[str, Position, tuple[str, ...], str, int], ...]:
    """Choose presentation treatment from semantic facts, never simulation text."""
    if isinstance(event, AttackTelegraphed):
        return ((
            "combat.telegraph",
            event.target_position,
            ("!", "·", "!"),
            "warning",
            5,
        ),)
    if isinstance(event, ActorMoved):
        return (("movement.trail", event.from_position, (".", "."), "trail", 1),)
    if isinstance(event, RetreatResolved):
        return (("movement.retreat", event.from_position, (">", "."), "motion", 2),)
    if isinstance(event, TerrainDamaged):
        return (("terrain.damage", event.position, ("'", ":"), "debris", 3),)
    if isinstance(event, TerrainChanged):
        return (("terrain.change", event.position, ("*", ":", "."), "debris", 5),)
    if isinstance(event, ThreatSpawned):
        return (("threat.arrival", event.position, ("!", "?", "!"), "warning", 5),)

    actor_id: str | None = None
    if isinstance(event, AttackResolved):
        actor_id = event.attacker_id
        style = ("combat.attack", (">", "/"), "motion", 3)
    elif isinstance(event, DamageApplied):
        actor_id = event.target_actor_id
        style = ("combat.damage", ("*", "+", "*"), "flash", 6)
    elif isinstance(event, StatusChanged):
        actor_id = event.actor_id
        style = ("actor.status", ("!", "!"), "warning", 4)
    elif isinstance(event, ActorDefeated):
        actor_id = event.actor_id
        style = ("actor.defeated", ("X", "%", "."), "flash", 7)
    elif isinstance(event, GuardResolved):
        actor_id = event.actor_id
        style = ("combat.guard", ("[", "]"), "guard", 3)
    elif isinstance(event, ItemUsed):
        actor_id = event.actor_id
        style = ("item.used", ("!", "+"), "flash", 3)
    elif isinstance(event, CarriedRelicSelectionChanged):
        actor_id = event.actor_id
        style = ("relic.selected", ("*", "+"), "flash", 2)
    else:
        return ()
    position = (
        event.origin
        if isinstance(event, AttackResolved) and event.origin is not None
        else actor_positions.get(actor_id)
    )
    if position is None:
        return ()
    effect_id, glyphs, emphasis_id, priority = style
    specifications = [(effect_id, position, glyphs, emphasis_id, priority)]
    if (
        isinstance(event, AttackResolved)
        and event.target_position is not None
        and not suppress_ranged_impact
    ):
        if event.result_id == "combat.enemy.hit":
            impact = ("*", "+", "*")
            impact_emphasis, impact_priority = "flash", 6
        elif event.result_id in {"combat.enemy.missed", "combat.enemy.blocked"}:
            impact = ("x", ".")
            impact_emphasis, impact_priority = "trail", 3
        else:
            impact = ("*", ".")
            impact_emphasis, impact_priority = "motion", 4
        specifications.append((
            "combat.enemy.impact",
            event.target_position,
            impact,
            impact_emphasis,
            impact_priority,
        ))
    return tuple(specifications)


def _projectile_glyph(previous: Position, position: Position) -> str:
    """Choose a one-cell ASCII direction cue from embedded path geometry."""
    dx = position.x - previous.x
    dy = position.y - previous.y
    if position.z != previous.z:
        return "|"
    if dx and dy:
        return "\\" if (dx > 0) == (dy > 0) else "/"
    if dx:
        return "-"
    return "|"


def _projectile_attacks(
    events: tuple[RuntimeEvent, ...],
) -> set[tuple[str, Position]]:
    """Match a resolved attack only to a projectile in its causal event group."""
    return {
        (event.attacker_id, event.target_position)
        for event in events
        if isinstance(event, ProjectileResolved)
    }
