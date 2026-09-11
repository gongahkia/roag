from __future__ import annotations

import copy
import curses
import unittest

from jomon.combat_forecast import danger_cells, forecast_lines, observed_forecasts
from jomon.inspection import contextual_hints, inspect_lines, movement_preview
from jomon.actions import move
from jomon.state import MaterialCell, Position, TerrainStatus, Threat, create_world
from jomon.terminal import InputEvent, LookView, _draw_base, _handle_look, _status_lines
from jomon.world import field_of_view


class InspectionAndForecastTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("inspection-and-forecast")

    def setUp(self):
        self.state = copy.deepcopy(self.base)
        state = self.state
        state.location, state.position = "region", Position(40, 25, 0)
        state.threats = []
        for y in range(22, 29):
            for x in range(36, 49):
                state.region.tile_changes[f"{x},{y},0"] = "."
        field_of_view(state)

    def test_movement_preview_is_zero_time_and_explains_material_pressure(self):
        state = self.state
        target = Position(41, 25, 0)
        state.region.materials["41,25,0"] = MaterialCell(material="timber", fire=2, smoke=3)
        before = state.to_dict()
        preview = movement_preview(state, target)
        self.assertTrue(preview.legal)
        self.assertIn("fire 2/3", preview.consequence)
        self.assertIn("smoke 3/4", preview.consequence)
        self.assertEqual(state.to_dict(), before)

    def test_blocked_movement_names_wall_water_and_recovery_without_time(self):
        state = self.state
        before = state.world_time
        state.region.tile_changes["41,25,0"] = "#"
        wall = move(state, 1, 0)
        self.assertIn("stone wall", wall.message)
        self.assertIn("another lane", wall.message)
        state.region.tile_changes["41,25,0"] = "~"
        water = move(state, 1, 0)
        self.assertIn("Deep water", water.message)
        self.assertIn("shallows", water.message)
        self.assertEqual(state.world_time, before)

    def test_inspection_distinguishes_visible_remembered_and_unknown(self):
        state = self.state
        visible = Position(41, 25, 0)
        remembered = Position(25, 25, 0)
        unknown = Position(100, 40, 0)
        if "25,25,0" not in state.region.seen:
            state.region.seen.append("25,25,0")
        self.assertTrue(inspect_lines(state, visible)[0].startswith("FACT:"))
        remembered_text = " ".join(inspect_lines(state, remembered))
        self.assertIn("REMEMBERED:", remembered_text)
        self.assertIn("terrain only", remembered_text)
        self.assertTrue(inspect_lines(state, unknown)[0].startswith("UNKNOWN:"))

    def test_observed_forecast_has_origin_target_timing_path_and_counter(self):
        state = self.state
        actor = Threat("forecaster", "visible bow carrier", "ranged", Position(46, 25), 8, 8, status="engaged", aimed_at=state.position)
        state.threats.append(actor)
        rows = observed_forecasts(state)
        self.assertEqual(len(rows), 1)
        forecast = rows[0]
        text = " ".join(forecast_lines(forecast))
        for word in ("origin", "target", "after your next action", "PATH", "COUNTERS"):
            self.assertIn(word, text)
        self.assertIn(state.position, danger_cells(state, field_of_view(state, remember=False)))
        self.assertIn("move/cover/G", " ".join(contextual_hints(state)))

    def test_forecast_never_leaks_hidden_actor(self):
        state = self.state
        actor = Threat("hidden", "hidden claimant", "ranged", Position(46, 25), 8, 8, status="engaged", aimed_at=state.position)
        state.threats.append(actor)
        state.region.tile_changes["43,25,0"] = "#"
        self.assertEqual(observed_forecasts(state), ())
        self.assertNotIn(actor.name, " ".join(inspect_lines(state, state.position)))

    def test_look_keyboard_and_mouse_are_zero_time_and_right_click_closes(self):
        state = self.state
        view = LookView.begin(state)
        before = state.to_dict()
        self.assertFalse(_handle_look(state, view, curses.KEY_RIGHT))
        self.assertEqual(view.cursor, Position(41, 25, 0))
        self.assertFalse(_handle_look(state, view, InputEvent("mouse", button="left", x=27, y=8)))
        self.assertTrue(_handle_look(state, view, InputEvent("mouse", button="right", x=27, y=8)))
        self.assertEqual(state.to_dict(), before)

    def test_minimum_status_keeps_resources_threat_condition_and_action(self):
        state = self.state
        actor = Threat("urgent", "urgent bow carrier", "ranged", Position(46, 25), 8, 8, status="engaged", aimed_at=state.position)
        state.threats.append(actor)
        state.terrain_statuses["bogged"] = TerrainStatus("deep mud", 3, "movement is slower")
        lines = _status_lines(state, 14)
        self.assertEqual(len(lines), 14)
        text = " ".join(lines)
        for phrase in ("Ammo", "DANGER urgent bow carrier", "STATUS bogged", "ACTION"):
            self.assertIn(phrase, text)

        sink = _PanelSink(24, 80)
        _draw_base(sink, state)
        rendered = " ".join(sink.writes)
        for phrase in ("Ammo", "DANGER urgent bow", "STATUS bogged", "ACTION"):
            self.assertIn(phrase, rendered)


class _PanelSink:
    def __init__(self, height: int, width: int):
        self.height, self.width, self.writes = height, width, []

    def getmaxyx(self):
        return self.height, self.width

    def erase(self):
        self.writes.clear()

    def refresh(self):
        pass

    def addnstr(self, y, x, text, n, attr=0):
        del y, x, attr
        self.writes.append(text[:n])


if __name__ == "__main__":
    unittest.main()
