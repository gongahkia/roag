import copy
import unittest

from jomon.state import Position
from jomon.systemic_audit import expanded_world, inspect_world, systemic_audit


class SystemicAuditTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.world = expanded_world("systemic-audit-0000")

    def test_real_eight_region_placements_and_history_pass(self):
        errors, metrics = inspect_world(self.world)
        self.assertEqual(errors, [])
        self.assertEqual(len(metrics), 8)
        self.assertEqual(sum(value["containers"] for value in metrics.values()), 62)
        self.assertTrue(all(value["used_levels"] == 4 for value in metrics.values()))

    def test_audit_detects_an_unreachable_actor_and_precommitted_shot(self):
        state = copy.deepcopy(self.world)
        actor = state.region_threats["dunmire"][0]
        actor.position = Position(0, 0)
        actor.aimed_at = state.regions["dunmire"].landmarks["landing"]
        errors, _ = inspect_world(state)
        self.assertIn("dunmire: unreachable actor", errors)
        self.assertIn("dunmire: precommitted opening attack", errors)

    def test_dormant_elite_positions_are_validated_before_they_wake(self):
        state = copy.deepcopy(self.world)
        actor = next(a for a in state.region_threats["dunmire"] if a.elite)
        actor.position = Position(0, 0)
        self.assertEqual(actor.status, "dormant")
        errors, _ = inspect_world(state)
        self.assertIn("dunmire: unreachable actor", errors)

    def test_audit_detects_bad_evidence_and_seasonally_stranded_destination(self):
        state = copy.deepcopy(self.world)
        state.regions["dunmire"].regional_history[0].evidence = "outside-evidence"
        for edge in state.route_edges:
            if "frostmere" in {edge.first, edge.second}:
                edge.closed_seasons = ["winter"]
        errors, _ = inspect_world(state)
        self.assertTrue(any("invalid historical evidence" in error for error in errors))
        self.assertIn("winter: an established region lacks a seasonal return route", errors)

    def test_sample_bounds_are_explicit(self):
        for samples, start in ((0, 0), (1, -1)):
            with self.assertRaises(ValueError):
                systemic_audit(samples, start)
