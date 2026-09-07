from __future__ import annotations

import unittest

from jomon.actions import _threat_action
from jomon.content import ENEMY_ARCHETYPES
from jomon.encounters import REGION_IDS, compose_encounter, encounter_audit
from jomon.regions import activate_region
from jomon.state import Position, create_world


class EncounterCompositionTests(unittest.TestCase):
    def test_each_new_region_has_six_standard_roles_and_one_elite(self):
        for region in REGION_IDS:
            entries = [data for data in ENEMY_ARCHETYPES.values() if data["region"] == region]
            self.assertGreaterEqual(sum(not data.get("elite") for data in entries), 5)
            self.assertEqual(sum(bool(data.get("elite")) for data in entries), 1)
            for data in entries:
                self.assertTrue(data["goal"])
                self.assertTrue(data["counterplay"])
                self.assertTrue(data["terrain"])

    def test_composition_is_deterministic_and_within_budget(self):
        for region in REGION_IDS:
            for band in ("steady", "strained", "critical"):
                first = compose_encounter("same plan", region, band, 4)
                second = compose_encounter("same plan", region, band, 4)
                self.assertEqual(first, second)
                self.assertLessEqual(first.spent, first.budget)
                self.assertEqual(len(first.archetypes), len(set(first.archetypes)))
                self.assertLessEqual(sum(ENEMY_ARCHETYPES[key]["profile"] == "ranged" for key in first.archetypes), 2)

    def test_hundred_seed_audit_has_variety_and_no_forbidden_group(self):
        report = encounter_audit(100)
        self.assertEqual(report["plans"], 900)
        self.assertEqual(report["invalid_or_forbidden"], 0)
        self.assertEqual(report["unavoidable_opening_attacks"], 0)
        self.assertEqual(report["unreachable_actors"], 0)
        self.assertGreater(report["ranged_actor_frequency"], 0)
        self.assertGreater(report["elite_frequency"], 0)
        self.assertGreaterEqual(report["unique_compositions"], 30)
        self.assertTrue(set(ENEMY_ARCHETYPES) <= set(report["archetype_frequency"]))

    def test_each_elite_changes_terrain_timing_or_visibility(self):
        for region_id in REGION_IDS:
            state = create_world(f"elite mechanics {region_id}")
            activate_region(state, region_id)
            state.active_courier_id = state.household[0].id
            state.location = "region"
            elite = next(threat for threat in state.threats if threat.elite)
            elite.status = "engaged"
            state.position = Position(elite.position.x - 1, elite.position.y, elite.position.z)
            first = _threat_action(state, elite, False)
            if region_id == "greywash":
                second = _threat_action(state, elite, False)
                self.assertIn("tide chain", first)
                self.assertTrue(state.water)
                self.assertIn("floods", second)
            elif region_id == "greenwold":
                self.assertTrue(state.smoke)
                self.assertIn("smoke line", first)
            else:
                marked = elite.aimed_at
                state.position = Position(state.position.x - 1, state.position.y, state.position.z)
                second = _threat_action(state, elite, False)
                self.assertIn("rockfall warning", first)
                self.assertIn(f"{marked.x},{marked.y},{marked.z}", state.region.tile_changes)
                self.assertIn("reposition", second)


if __name__ == "__main__":
    unittest.main()
