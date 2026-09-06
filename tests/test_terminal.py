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
    def test_consolidated_tavern_selection_is_zero_time(self):
        state = create_world("tavern menu")
        state.position = find_tile(JOMON_MAP, "C")
        result = interact(state)
        self.assertEqual(result.overlay, "tavern")
        started = state.world_time
        for submenu in ("courier", "weapon", "gear", "support"):
            overlay, _ = _handle_overlay(state, f"tavern:{submenu}", ord("1"))
            self.assertEqual(overlay, "tavern")
        self.assertIsNotNone(state.courier)
        self.assertIsNotNone(state.weapon)
        self.assertIsNotNone(state.gear)
        self.assertIsNotNone(state.support)
        self.assertEqual(state.world_time, started)

    def test_passive_submenu_toggles_without_time(self):
        state = create_world("passive submenu")
        state.owned_passives = {"echo bead": 1}
        started = state.world_time
        overlay, _ = _handle_overlay(state, "tavern", ord("d"))
        self.assertEqual(overlay, "tavern:passive")
        overlay, _ = _handle_overlay(state, overlay, ord("1"))
        self.assertEqual(overlay, "tavern:passive")
        self.assertEqual(state.carried_passives, {"echo bead": 1})
        self.assertEqual(state.world_time, started)


if __name__ == "__main__":
    unittest.main()
