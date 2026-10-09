from __future__ import annotations

import json
import os
import subprocess
import sys
import unittest
from unittest.mock import patch

from roag.commands import MoveCommand
from roag.presentation import (
    EffectState, MapEffect, PRESENTATION_FRAME_MS, WATER_GLYPHS,
    presentation_enabled,
)
from roag.runtime_events import (
    ActorDefeated, AttackResolved, AttackTelegraphed, DamageApplied,
    RuntimeEventBatch, RuntimeEventStep, TerrainChanged, TerrainDamaged,
    ThreatSpawned,
)
from roag.session import GameSession
from roag.state import Position, create_world
from roag.terminal import _consume_outcome_effects, _draw_base, _play_loop
from roag.world import is_walkable


class PresentationStateTests(unittest.TestCase):
    def test_event_batch_schedules_causal_command_then_world_step_effects(self):
        attacker = Position(10, 10, 0)
        target = Position(11, 10, 0)
        arrival = Position(15, 10, 0)
        batch = RuntimeEventBatch(
            command_events=(
                AttackResolved("courier", "target", "attack", "hit"),
                DamageApplied("courier", "target", 3, "cut", "torso"),
                ActorDefeated("target", "courier"),
            ),
            steps=(RuntimeEventStep(
                1,
                (ThreatSpawned("arrival", "raider", arrival, "strained"),),
            ),),
        )
        effects = EffectState.for_seed("presentation-event-order")

        effects.consume(batch, {"courier": attacker, "target": target})

        self.assertEqual(effects.glyph("@", attacker), ">")
        self.assertEqual(effects.glyph("e", target), "e")
        effects.advance()
        self.assertEqual(effects.glyph("e", target), "*")
        effects.advance()
        self.assertEqual(effects.glyph("e", target), "X")
        effects.advance()
        self.assertEqual(effects.glyph(".", arrival), "!")
        self.assertEqual(effects.glyph(".", arrival, visible=False), ".")
        effects.advance(3 * PRESENTATION_FRAME_MS)
        self.assertEqual(effects.effects, [])

    def test_enemy_warning_then_hit_uses_embedded_authoritative_positions(self):
        origin = Position(10, 10, 0)
        target = Position(12, 10, 0)
        batch = RuntimeEventBatch(steps=(
            RuntimeEventStep(1, (AttackTelegraphed(
                "enemy", origin, target, "intent.melee.warning",
            ),)),
            RuntimeEventStep(2, (AttackResolved(
                "enemy", "courier", "intent.melee.warning",
                "combat.enemy.hit", origin, target,
            ),)),
        ))
        effects = EffectState.for_seed("enemy warning presentation")

        effects.consume(batch)

        self.assertEqual(effects.glyph("@", target), "!")
        self.assertEqual(effects.glyph("@", target, visible=False), "@")
        self.assertEqual(effects.effect_at(target).effect_id, "combat.telegraph")
        effects.advance()
        self.assertEqual(effects.glyph("e", origin), ">")
        self.assertEqual(effects.glyph("@", target), "*")
        self.assertEqual(
            effects.effect_at(target).effect_id, "combat.enemy.impact",
        )

    def test_enemy_miss_has_a_quieter_static_and_animated_cue(self):
        origin = Position(10, 10, 0)
        marked = Position(12, 10, 0)
        event = AttackResolved(
            "enemy", "courier", "intent.ranged.aim_telegraph",
            "combat.enemy.missed", origin, marked,
        )
        effects = EffectState.for_seed("enemy miss presentation")

        effects.consume(RuntimeEventBatch(command_events=(event,)))

        self.assertEqual(effects.glyph("e", origin), ">")
        self.assertEqual(effects.glyph(".", marked), "x")
        self.assertEqual(effects.effect_at(marked).emphasis_id, "trail")

    def test_session_outcome_enters_effect_state_without_mutating_again(self):
        state = create_world("presentation-command-boundary")
        session = GameSession(state)
        before_position = state.position
        dx, dy = next(
            (dx, dy)
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
            if is_walkable(state, Position(
                before_position.x + dx,
                before_position.y + dy,
                before_position.z,
            ))
        )
        outcome = session.submit(MoveCommand(dx, dy))
        after_command = state.to_dict()
        effects = EffectState.for_seed(state.seed)

        _consume_outcome_effects(effects, state, session, outcome)

        self.assertEqual(effects.glyph(".", before_position), ".")
        self.assertEqual(effects.effect_at(before_position).effect_id, "movement.trail")
        self.assertEqual(state.to_dict(), after_command)

    def test_terrain_damage_precedes_replacement_debris(self):
        point = Position(8, 9, 0)
        batch = RuntimeEventBatch(command_events=(
            TerrainDamaged(
                "courier", point, "terrain.region.reeds", "cut", 1, 0,
            ),
            TerrainChanged(
                "courier", point, "terrain.region.reeds",
                "terrain.region.ground", "cut",
            ),
        ))
        effects = EffectState.for_seed("presentation-terrain-order")

        effects.consume(batch)

        self.assertEqual(effects.glyph(".", point), "'")
        effects.advance()
        self.assertEqual(effects.glyph(".", point), "*")
        self.assertEqual(effects.effect_at(point).effect_id, "terrain.change")

    def test_disabled_effects_ignore_batches_and_map_effects_are_immutable(self):
        effects = EffectState.for_seed("disabled-event-effects", enabled=False)
        event = ThreatSpawned(
            "arrival", "raider", Position(5, 5, 0), "strained",
        )
        effects.consume(RuntimeEventBatch(command_events=(event,)))
        self.assertEqual(effects.effects, [])
        self.assertEqual(effects.glyph(".", event.position), ".")
        with self.assertRaises(ValueError):
            MapEffect(
                "invalid", event.position, ("too wide",), "flash", 0, 1, 0,
            )

    def test_presentation_domain_remains_headless(self):
        result = subprocess.run(
            [
                sys.executable,
                "-c",
                "import sys; import roag.presentation; assert 'curses' not in sys.modules",
            ],
            check=False,
            capture_output=True,
            text=True,
        )
        self.assertEqual(result.returncode, 0, result.stderr)

    def test_ambient_glyphs_are_stable_and_non_authoritative(self):
        state = create_world("presentation-only-water")
        before = state.to_dict()
        effects = EffectState.for_seed(state.seed)
        point = Position(12, 7, 0)

        first = effects.glyph("~", point)
        self.assertEqual(first, EffectState.for_seed(state.seed).glyph("~", point))
        frames = []
        for _ in WATER_GLYPHS:
            frames.append(effects.glyph("~", point))
            effects.advance()

        self.assertGreater(len(set(frames)), 1)
        self.assertTrue(set(frames) <= set(WATER_GLYPHS))
        self.assertEqual(effects.glyph("#", point), "#")
        self.assertEqual(state.to_dict(), before)
        serialized = json.dumps(state.to_dict(), sort_keys=True)
        self.assertNotIn("EffectState", serialized)
        self.assertNotIn("elapsed_ms", serialized)
        self.assertNotIn("seed_offset", serialized)

    def test_repeated_rendering_does_not_mutate_game_state(self):
        class Screen:
            def __init__(self):
                self.writes = []

            def getmaxyx(self):
                return 24, 80

            def erase(self):
                self.writes.clear()

            def refresh(self):
                pass

            def addnstr(self, row, col, value, count, attr=0):
                self.writes.append((row, col, value[:count], attr))

        state = create_world("presentation-render-purity")
        before = state.to_dict()
        effects = EffectState.for_seed(state.seed)
        screen = Screen()

        for _ in range(4):
            _draw_base(screen, state, effects=effects)
            effects.advance()

        self.assertEqual(state.to_dict(), before)
        self.assertEqual(state.world_time, 0)

    def test_idle_timeouts_advance_only_presentation_and_modals_block(self):
        class Screen:
            def __init__(self):
                self.keys = iter((-1, -1, ord("q"), ord("y")))
                self.timeouts = []

            def getmaxyx(self):
                return 24, 80

            def timeout(self, milliseconds):
                self.timeouts.append(milliseconds)

            def getch(self):
                return next(self.keys)

        state = create_world("idle-presentation-clock")
        before = state.to_dict()
        effects = EffectState.for_seed(state.seed)
        screen = Screen()
        cached_view = object()
        with (
            patch("roag.terminal._draw_base"),
            patch("roag.terminal._draw_dialogue_overlay"),
            patch("roag.terminal.GameSession.world_view", return_value=cached_view) as world_view,
        ):
            returned = _play_loop(screen, state, effects)

        self.assertIs(returned, state)
        self.assertEqual(effects.elapsed_ms, 2 * PRESENTATION_FRAME_MS)
        self.assertEqual(screen.timeouts, [
            PRESENTATION_FRAME_MS,
            PRESENTATION_FRAME_MS,
            PRESENTATION_FRAME_MS,
            -1,
        ])
        self.assertEqual(state.to_dict(), before)
        self.assertEqual(state.world_time, 0)
        self.assertEqual(world_view.call_count, 2)

    def test_disabled_presentation_keeps_blocking_static_input(self):
        class Screen:
            def __init__(self):
                self.keys = iter((ord("q"), ord("y")))
                self.timeouts = []

            def getmaxyx(self):
                return 24, 80

            def timeout(self, milliseconds):
                self.timeouts.append(milliseconds)

            def getch(self):
                return next(self.keys)

        state = create_world("static-presentation-fallback")
        effects = EffectState.for_seed(state.seed, enabled=False)
        screen = Screen()
        with (
            patch("roag.terminal._draw_base"),
            patch("roag.terminal._draw_dialogue_overlay"),
        ):
            _play_loop(screen, state, effects)

        self.assertEqual(screen.timeouts, [-1, -1])
        self.assertEqual(effects.elapsed_ms, 0)

    def test_environment_can_disable_timed_presentation(self):
        with patch.dict(os.environ, {"ROAG_PRESENTATION": "off"}):
            self.assertFalse(presentation_enabled())
        with patch.dict(os.environ, {"ROAG_PRESENTATION": "on"}):
            self.assertTrue(presentation_enabled())
