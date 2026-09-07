from __future__ import annotations

import unittest

from jomon.actions import interact
from jomon.content import JOMON_MAP
from jomon.state import Position, create_world
from jomon.terminal import (
    SEMANTIC_ROLES,
    _handle_overlay,
    semantic_colour_plan,
    semantic_role,
    visible_threats,
)
from jomon.world import field_of_view, find_tile


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


class TavernMenuTests(unittest.TestCase):
    def test_physical_person_selection_is_zero_time(self):
        state = create_world("physical tavern")
        person = state.household[0]
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

    def test_bar_selects_support_but_not_courier_or_equipment(self):
        state = create_world("tavern support")
        state.position = find_tile(JOMON_MAP, "C")
        started = state.world_time
        self.assertEqual(interact(state).overlay, "tavern")
        overlay, _ = _handle_overlay(state, "tavern", ord("s"))
        self.assertEqual(overlay, "tavern:support")
        overlay, _ = _handle_overlay(state, overlay, ord("1"))
        self.assertEqual(overlay, "tavern")
        self.assertIsNotNone(state.support)
        self.assertIsNone(state.courier)
        self.assertIsNone(state.weapon)
        self.assertEqual(state.world_time, started)


if __name__ == "__main__":
    unittest.main()
