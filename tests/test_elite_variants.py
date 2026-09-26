from __future__ import annotations

import unittest

from jomon.actions import _threat_action
from jomon.regions import activate_region, region_reachable
from jomon.state import Position, create_world
from jomon.world import position_key


class AlternativeEliteTests(unittest.TestCase):
    def test_seed_selects_one_deterministic_elite_variant_per_region(self):
        first = create_world("alt-4")
        second = create_world("alt-4")
        names = {
            region_id: next(threat.name for threat in threats if threat.elite)
            for region_id, threats in first.region_threats.items()
        }
        self.assertEqual(names, {
            "hearthford": "floodgate claimant",
            "greywash": "wreck-chain reeve",
            "greenwold": "resin-fire tracker",
            "whitecairn": "bridge-breaker bellward",
        })
        self.assertEqual(
            names,
            {
                region_id: next(threat.name for threat in threats if threat.elite)
                for region_id, threats in second.region_threats.items()
            },
        )

    def test_alternative_elites_telegraph_then_change_spatial_rules(self):
        state = create_world("alt-4")

        activate_region(state, "hearthford")
        state.location = "region"
        state.position = Position(80, 27)
        claimant = next(threat for threat in state.threats if threat.elite)
        claimant.status = "engaged"
        self.assertIn("marks", _threat_action(state, claimant, False))
        state.position = Position(80, 26)
        _threat_action(state, claimant, False)
        self.assertTrue(state.water)

        activate_region(state, "greenwold")
        state.location = "region"
        state.position = Position(81, 39, 1)
        tracker = next(threat for threat in state.threats if threat.elite)
        tracker.status = "engaged"
        self.assertIn("marks resin", _threat_action(state, tracker, False))
        state.position = Position(81, 38, 1)
        _threat_action(state, tracker, False)
        self.assertTrue(state.smoke)

        activate_region(state, "whitecairn")
        state.location = "region"
        bellward = next(threat for threat in state.threats if threat.elite)
        bellward.status = "engaged"
        state.position = min(
            (
                point for point in region_reachable(state.region)
                if point.z == bellward.position.z
                and point != bellward.position
                and max(abs(point.x - bellward.position.x), abs(point.y - bellward.position.y)) <= 3
            ),
            key=lambda point: (abs(point.x - bellward.position.x) + abs(point.y - bellward.position.y), point.y, point.x),
        )
        marked = state.position
        self.assertIn("marks floor", _threat_action(state, bellward, False))
        state.position = bellward.position
        _threat_action(state, bellward, False)
        self.assertEqual(state.region.tile_changes[position_key(marked)], "O")

    def test_each_alternative_has_a_material_noncombat_answer(self):
        state = create_world("alt-4")
        answers = {
            "hearthford": ("mill_public_compact", "floodgate claimant"),
            "greywash": ("tide_held", "wreck-chain reeve"),
            "greenwold": ("medicine_coppice_saved", "resin-fire tracker"),
            "whitecairn": ("honest_bell", "bridge-breaker bellward"),
        }
        for region_id, (change, name) in answers.items():
            with self.subTest(region=region_id):
                activate_region(state, region_id)
                elite = next(threat for threat in state.threats if threat.name == name)
                elite.status = "engaged"
                state.region.changes[change] = True
                _threat_action(state, elite, False)
                _threat_action(state, elite, False)
                self.assertEqual(elite.status, "retreated")


if __name__ == "__main__":
    unittest.main()
