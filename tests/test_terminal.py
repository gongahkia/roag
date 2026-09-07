from __future__ import annotations

import unittest
from unittest.mock import patch

from jomon.actions import interact
from jomon.vessel import BARTENDER_POSITION
from jomon.state import Position, create_world
from jomon.terminal import (
    INVENTORY_HELP_LINES,
    InputEvent,
    OverlayView,
    _handle_overlay_view,
    SEMANTIC_ROLES,
    dialogue_choices,
    _handle_overlay,
    semantic_colour_plan,
    semantic_role,
    visible_threats,
)
from jomon.main import _centered_x, _set_cursor_visibility, landing_notice_layout
from jomon.world import field_of_view, find_tile


class InventoryLayoutTests(unittest.TestCase):
    def test_control_legend_fits_the_eighty_column_layout(self):
        self.assertEqual(len(INVENTORY_HELP_LINES), 2)
        self.assertTrue(all(len(line) <= 78 for line in INVENTORY_HELP_LINES))
        joined = " ".join(INVENTORY_HELP_LINES)
        for command in ("Enter", "R rotate", "Space", "T transfer", "E equip", "O pack", "P pin", "Z auto", "[] body", "D drop", "C confirm", "Esc cancel"):
            self.assertIn(command, joined)


class SemanticColourTests(unittest.TestCase):
    def test_full_semantic_plan_is_complete_and_distinct(self):
        plan = semantic_colour_plan(8, 16)
        self.assertEqual(set(plan), set(SEMANTIC_ROLES))
        self.assertNotEqual(plan["player"].pair, plan["hostile"].pair)
        self.assertNotEqual(plan["hostile"].pair, plan["elite"].pair)
        self.assertNotEqual(plan["water"].pair, plan["hazard"].pair)
        expected = {
            "@": "player",
            "a": "ally",
            "M": "neutral",
            "h": "hostile",
            "X": "elite",
            "~": "water",
            "#": "structure",
            ">": "exit",
            "R": "cargo",
            "C": "interactable",
            "m": "hazard",
            "*": "mystical",
        }
        for glyph, role in expected.items():
            self.assertEqual(semantic_role(glyph), role)
        self.assertEqual(semantic_role("v"), "neutral")
        self.assertEqual(semantic_role("s"), "hazard")
        self.assertEqual(semantic_role("s", aboard=True), "interactable")

    def test_limited_colour_fallback_uses_glyph_and_bold_cues(self):
        plan = semantic_colour_plan(0, 0)
        self.assertTrue(
            all(style.pair == 0 and style.foreground is None for style in plan.values())
        )
        self.assertTrue(plan["player"].bold)
        self.assertTrue(plan["hostile"].bold)

    def test_remembered_terrain_does_not_leak_moving_actor(self):
        state = create_world("actor memory")
        state.location, state.current_room = "region", "hearthford"
        state.position = Position(40, 25, 0)
        threat = state.threats[0]
        threat.position = Position(41, 25, 0)
        visible = field_of_view(state)
        self.assertIn(threat.position, visible_threats(state, visible))
        old = threat.position
        threat.position = Position(80, 40, 0)
        state.position = Position(70, 40, 0)
        current = field_of_view(state)
        self.assertNotIn(old, visible_threats(state, current))
        self.assertNotIn(threat.position, visible_threats(state, current))


class LandingLayoutTests(unittest.TestCase):
    def test_unsupported_cursor_visibility_is_a_safe_fallback(self):
        with patch("jomon.main.curses.curs_set", side_effect=__import__("curses").error):
            self.assertFalse(_set_cursor_visibility(0))

    def test_long_incompatible_save_warning_wraps_and_centres(self):
        warning = "Existing development save unavailable: incompatible format with preserved consequences that cannot be loaded safely"
        for width, height in ((80, 24), (100, 32)):
            layout = landing_notice_layout(width, height, warning, "/a/deliberately/long/save/location/jomon-save.json")
            self.assertGreaterEqual(layout.left, 1)
            self.assertLessEqual(layout.left + layout.width, width - 1)
            self.assertGreaterEqual(layout.top, 7)
            self.assertLessEqual(layout.top + layout.height, height - 1)
            self.assertTrue(all(len(line) <= layout.width - 4 for line in layout.lines))
            self.assertEqual(" ".join(layout.lines), warning)
            self.assertTrue(all(len(line) <= layout.width - 8 for line in layout.path_lines))
            self.assertEqual(_centered_x(width, "J O M O N"), (width - len("J O M O N")) // 2)

    def test_landing_layout_reflows_after_resize(self):
        warning = "Existing development save unavailable: a deliberately long deterministic migration explanation remains complete"
        small = landing_notice_layout(80, 24, warning, "/tmp/save.json")
        large = landing_notice_layout(100, 32, warning, "/tmp/save.json")
        self.assertGreaterEqual(len(small.lines), len(large.lines))
        self.assertNotEqual((small.top, small.left), (large.top, large.left))


class TavernMenuTests(unittest.TestCase):
    def test_physical_person_selection_is_zero_time(self):
        state = create_world("physical tavern")
        state.jomon_space = "tavern"
        person = state.household[1]
        seat = state.tavern_positions[person.id]
        state.position = Position(seat.x - 1, seat.y)
        started = state.world_time
        result = interact(state)
        self.assertEqual(result.overlay, f"person:{person.id}")
        overlay, _ = _handle_overlay(state, result.overlay, ord("s"))
        self.assertIsNone(overlay)
        self.assertEqual(state.courier, person)
        self.assertNotIn(person.id, state.tavern_positions)
        self.assertEqual(state.position, seat)
        self.assertEqual(state.world_time, started)


class DialogueChoiceTests(unittest.TestCase):
    def test_options_retain_keys_markers_semantics_and_unavailable_reason(self):
        state = create_world("dialogue semantics")
        state.gear = None
        state.support = None
        state.contact.disposition = 0
        options = dialogue_choices(state, "objective")
        self.assertEqual([option.key for option in options], ["A", "R", "T"])
        self.assertEqual([option.semantic for option in options], ["commitment", "refusal", "commitment"])
        self.assertFalse(options[-1].available)
        self.assertTrue(options[-1].requirement)

    def test_arrow_and_mouse_selection_do_not_advance_time(self):
        state = create_world("dialogue input")
        view = OverlayView("objective", option_rows=[10, 11, 12])
        before = state.world_time
        closed, _ = _handle_overlay_view(state, view, InputEvent("key", key=__import__("curses").KEY_DOWN))
        self.assertFalse(closed)
        self.assertEqual(view.selected, 1)
        closed, _ = _handle_overlay_view(state, view, InputEvent("mouse", x=5, y=10, button="left"))
        self.assertFalse(closed)
        self.assertEqual(view.selected, 0)
        self.assertEqual(state.world_time, before)

    def test_bar_selects_support_but_not_courier_or_equipment(self):
        state = create_world("tavern support")
        courier, weapon, gear = state.courier, state.weapon, state.gear
        state.support = None
        state.jomon_space = "tavern"
        state.position = Position(BARTENDER_POSITION.x - 1, BARTENDER_POSITION.y)
        started = state.world_time
        self.assertEqual(interact(state).overlay, "bartender")
        overlay, _ = _handle_overlay(state, "bartender", ord("s"))
        self.assertEqual(overlay, "tavern:support")
        overlay, _ = _handle_overlay(state, overlay, ord("1"))
        self.assertEqual(overlay, "bartender")
        self.assertIsNotNone(state.support)
        self.assertIs(state.courier, courier)
        self.assertEqual(state.weapon, weapon)
        self.assertEqual(state.gear, gear)
        self.assertEqual(state.world_time, started)


if __name__ == "__main__":
    unittest.main()
