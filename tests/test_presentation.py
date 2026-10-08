from __future__ import annotations

import json
import os
import unittest
from unittest.mock import patch

from roag.presentation import (
    EffectState, PRESENTATION_FRAME_MS, WATER_GLYPHS, presentation_enabled,
)
from roag.state import Position, create_world
from roag.terminal import _draw_base, _play_loop


class PresentationStateTests(unittest.TestCase):
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
