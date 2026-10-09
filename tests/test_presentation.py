from __future__ import annotations

import json
import os
import subprocess
import sys
import unittest
from unittest.mock import patch

from roag.commands import MoveCommand
from roag.presentation import (
    CameraEffect, EffectState, FIRE_GLYPHS, MapEffect, PRESENTATION_FRAME_MS,
    SHALLOW_WATER_GLYPHS, SMOKE_GLYPHS, WATER_GLYPHS, presentation_enabled,
)
from roag.runtime_events import (
    ActorDefeated, AreaResolved, AreaTelegraphed, AttackResolved,
    AttackTelegraphed, CollapseResolved, DamageApplied, ProjectileResolved,
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

    def test_projectile_uses_embedded_path_and_delays_impact(self):
        origin = Position(10, 10, 0)
        first = Position(11, 10, 0)
        second = Position(12, 10, 0)
        target = Position(13, 10, 0)
        events = (
            AttackResolved(
                "enemy", "courier", "intent.ranged.aim_telegraph",
                "combat.enemy.hit", origin, target,
            ),
            ProjectileResolved(
                "enemy", origin, target, (origin, first, second, target),
                "crossbow", "combat.enemy.hit",
            ),
        )
        effects = EffectState.for_seed("projectile presentation")

        effects.consume(RuntimeEventBatch(steps=(RuntimeEventStep(1, events),)))

        self.assertEqual(effects.glyph("e", origin), ">")
        self.assertEqual(effects.glyph(".", first), "-")
        self.assertEqual(effects.glyph("@", target), "@")
        effects.advance()
        self.assertEqual(effects.glyph(".", second), "-")
        self.assertEqual(effects.glyph("@", target), "@")
        effects.advance()
        self.assertEqual(effects.glyph("@", target), "*")
        self.assertEqual(
            effects.effect_at(target).effect_id, "combat.projectile.impact",
        )
        self.assertEqual(effects.glyph("@", target, visible=False), "@")

    def test_area_warning_is_immediate_and_resolution_expands_from_origin(self):
        center = Position(20, 10, 0)
        left = Position(19, 10, 0)
        right = Position(21, 10, 0)
        cells = (center, left, right)
        warning = EffectState.for_seed("area telegraph presentation")
        warning.consume(RuntimeEventBatch(command_events=(AreaTelegraphed(
            "enemy", center, cells,
            "intent.elite.floodgate.sluice_telegraph",
        ),)))

        self.assertEqual(
            [warning.glyph(".", point) for point in cells], ["!", "!", "!"],
        )
        self.assertEqual(warning.glyph(".", left, visible=False), ".")

        resolution = EffectState.for_seed("area resolution presentation")
        resolution.consume(RuntimeEventBatch(command_events=(AreaResolved(
            "enemy", center, cells,
            "intent.elite.floodgate.sluice_telegraph", "combat.enemy.hit",
        ),)))

        self.assertEqual(resolution.glyph(".", center), "*")
        self.assertEqual(resolution.glyph(".", left), ".")
        self.assertEqual(resolution.glyph(".", right), ".")
        resolution.advance()
        self.assertEqual(resolution.glyph(".", left), "*")
        self.assertEqual(resolution.glyph(".", right), ".")
        resolution.advance()
        self.assertEqual(resolution.glyph(".", right), "*")

    def test_collapse_stages_debris_shockwave_and_visibility_safe_camera_motion(self):
        origin = Position(20, 10, 0)
        impact = Position(20, 10, -1)
        collapse = CollapseResolved(
            origin, (origin, impact), 2, "collapse.open_drop",
        )
        terrain = TerrainChanged(
            "environment", origin, "terrain.region.ground",
            "terrain.region.open_drop", "collapse",
        )
        effects = EffectState.for_seed("collapse presentation")

        effects.consume(RuntimeEventBatch(steps=(
            RuntimeEventStep(1, (collapse, terrain)),
        )))

        self.assertEqual(effects.glyph("O", origin), "#")
        self.assertEqual(effects.glyph(".", Position(21, 10, 0)), ".")
        self.assertEqual(effects.camera_offset({origin}), (1, 0))
        self.assertEqual(effects.camera_offset(set()), (0, 0))
        effects.advance()
        self.assertEqual(effects.glyph("O", origin), "*")
        self.assertEqual(effects.glyph(".", Position(21, 10, 0)), "-")
        self.assertEqual(effects.glyph(".", impact), "*")
        self.assertEqual(effects.camera_offset({origin}), (-1, 0))
        effects.advance()
        self.assertEqual(effects.glyph(".", Position(21, 10, 0)), ".")
        self.assertEqual(effects.camera_offset({origin}), (0, 1))
        effects.advance(2 * PRESENTATION_FRAME_MS)
        self.assertEqual(effects.camera_offset({origin}), (0, 0))
        self.assertEqual(effects.glyph("O", origin), "O")
        self.assertEqual(effects.camera_effects, [])

    def test_collapse_presentation_is_deterministic_bounded_and_disableable(self):
        origin = Position(6, 7, 0)
        event = CollapseResolved(origin, (origin,), 3, "collapse.rubble")
        batch = RuntimeEventBatch(command_events=(event,))
        first = EffectState.for_seed("deterministic collapse")
        second = EffectState.for_seed("deterministic collapse")

        first.consume(batch)
        second.consume(batch)

        self.assertEqual(first.effects, second.effects)
        self.assertEqual(first.camera_effects, second.camera_effects)
        self.assertTrue(all(
            abs(value) <= 1
            for effect in first.camera_effects
            for offset in effect.offsets
            for value in offset
        ))
        disabled = EffectState.for_seed("disabled collapse", enabled=False)
        disabled.consume(batch)
        self.assertEqual(disabled.effects, [])
        self.assertEqual(disabled.camera_effects, [])
        self.assertEqual(disabled.camera_offset({origin}), (0, 0))
        with self.assertRaises(ValueError):
            CameraEffect("invalid", origin, ((3, 0),), 0, 1, 0)

    def test_terminal_applies_ui_owned_camera_offset_without_state_mutation(self):
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

        state = create_world("presentation camera composition")
        before = state.to_dict()
        effects = EffectState.for_seed(state.seed)
        screen = Screen()
        with (
            patch("roag.terminal.camera_origin", return_value=(5, 5)),
            patch(
                "roag.terminal.curses_tile",
                side_effect=lambda _state, point: str(point.x % 10),
            ),
            patch.object(effects, "camera_offset", return_value=(1, 0)) as offset,
        ):
            _draw_base(screen, state, effects=effects)

        offset.assert_called_once()
        self.assertEqual(
            next(value for row, column, value, _ in screen.writes
                 if (row, column) == (1, 1)),
            "6",
        )
        self.assertEqual(state.to_dict(), before)

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

    def test_material_fields_animate_only_while_presently_visible(self):
        effects = EffectState.for_seed("presentation material fields")
        fire = Position(12, 7, 0)
        smoke = Position(13, 7, 0)
        shallow = Position(14, 7, 0)
        frames = {"fire": [], "smoke": [], "water": []}

        for _ in range(4):
            frames["fire"].append(effects.ambient_cell(
                "f", fire, environment_id="fire",
            ).glyph)
            frames["smoke"].append(effects.ambient_cell(
                "s", smoke, environment_id="smoke",
            ).glyph)
            frames["water"].append(effects.ambient_cell(
                ",", shallow, environment_id="water",
            ).glyph)
            effects.advance()

        self.assertGreater(len(set(frames["fire"])), 1)
        self.assertGreater(len(set(frames["smoke"])), 1)
        self.assertGreater(len(set(frames["water"])), 1)
        self.assertTrue(set(frames["fire"]) <= set(FIRE_GLYPHS))
        self.assertTrue(set(frames["smoke"]) <= set(SMOKE_GLYPHS))
        self.assertTrue(set(frames["water"]) <= set(SHALLOW_WATER_GLYPHS))
        self.assertEqual(
            effects.ambient_cell(
                "f", fire, environment_id="fire", visible=False,
            ).glyph,
            "f",
        )

    def test_precipitation_is_sparse_stable_and_never_hides_map_symbols(self):
        first = EffectState.for_seed("presentation regional weather")
        second = EffectState.for_seed("presentation regional weather")
        points = tuple(Position(x, y, 0) for y in range(8) for x in range(16))

        def rainy_cells(effects):
            return {
                point: effects.ambient_cell(
                    ".", point, weather_id="hard rain", exposed=True,
                )
                for point in points
            }

        initial = rainy_cells(first)
        self.assertEqual(initial, rainy_cells(second))
        wet = {point for point, frame in initial.items() if frame.glyph != "."}
        self.assertTrue(wet)
        self.assertLess(len(wet), len(points))
        self.assertTrue(all(initial[point].role_id == "shallow_water" for point in wet))
        first.advance()
        self.assertNotEqual(wet, {
            point for point, frame in rainy_cells(first).items()
            if frame.glyph != "."
        })

        for glyph in ("@", "e", "!", "#", "+", "f", "s"):
            self.assertEqual(
                first.ambient_cell(
                    glyph, Position(3, 3), weather_id="coast squall",
                    exposed=True,
                ).glyph,
                glyph,
            )
        self.assertEqual(first.ambient_cell(
            ".", Position(3, 3), weather_id="hard rain", exposed=False,
        ).glyph, ".")
        self.assertEqual(first.ambient_cell(
            ".", Position(3, 3), weather_id="clear", exposed=True,
        ).glyph, ".")

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
        state.weather = "hard rain"
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
