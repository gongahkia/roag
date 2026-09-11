import copy
from pathlib import Path
import tempfile
import unittest

from jomon.actions import depart
from jomon.frontiers import ensure_frontier
from jomon.regions import activate_region
from jomon.save import load_game, save_game
from jomon.situations import (
    BY_REGION_BAND, SITUATIONS, activate_for_band, audit_situations,
    choices, inspect_lines, resolve, site_glyph, site_point,
    validate_situations,
)
from jomon.state import create_world
from jomon.world import is_walkable


class MixedSituationTests(unittest.TestCase):
    def ready(self):
        state = create_world("mixed-situation-tests")
        state.weapon, state.gear = "billhook", "repair tools"
        depart(state)
        return state

    def test_catalogue_has_three_distinct_situations_per_region(self):
        validate_situations()
        self.assertEqual(len(SITUATIONS), 24)
        self.assertEqual(audit_situations(200)["failures"], [])
        self.assertLess(audit_situations(20)["max_share"], 0.1)

    def test_sites_are_safe_reachable_and_do_not_replace_required_points(self):
        state = self.ready()
        for region_id in sorted({row.region_id for row in SITUATIONS}):
            if region_id not in state.regions:
                ensure_frontier(state, region_id)
            activate_region(state, region_id)
            region = state.region
            points = [site_point(state, row) for row in SITUATIONS if row.region_id == region_id]
            self.assertEqual(len(set(points)), 3)
            self.assertFalse(set(points) & set(region.landmarks.values()))
            self.assertFalse(set(points) & {c.position for c in region.containers})
            self.assertTrue(all(
                landmark.z != point.z
                or max(abs(landmark.x-point.x), abs(landmark.y-point.y)) > 2
                for point in points for landmark in region.landmarks.values()
            ))
            self.assertTrue(all(is_walkable(state, point, ignore_threat=True) for point in points))

    def test_activation_wakes_two_groups_and_exposes_three_answers(self):
        state = self.ready()
        row = BY_REGION_BAND["hearthford", "steady"]
        self.assertEqual(state.region.changes["situation:active"], row.id)
        self.assertEqual(site_glyph(state, site_point(state, row)), "!")
        self.assertEqual(len(choices(state, row.id)), 3)
        attached = [a for a in state.combatants if a.objective_position == site_point(state, row)]
        self.assertEqual(len(attached), 2)
        self.assertEqual(len({a.group for a in attached}), 2)

    def test_each_solution_changes_a_different_system_and_cost(self):
        for method, expected_steps in (("t", 2), ("m", 1), ("a", 1)):
            state = self.ready()
            row = BY_REGION_BAND["hearthford", "steady"]
            before_rope = state.rope_uses
            before_demand = state.market[state.region.objective_commodity].demand
            changed, message, steps = resolve(state, row.id, method)
            self.assertTrue(changed, message)
            self.assertEqual(steps, expected_steps)
            self.assertEqual(site_glyph(state, site_point(state, row)), "*")
            self.assertIn("Persistent consequence", " ".join(inspect_lines(state, row.id)))
            if method == "m":
                self.assertEqual(state.rope_uses, before_rope - 1)
            if method == "a":
                self.assertEqual(state.market[state.region.objective_commodity].demand, before_demand - 1)

    def test_unavailable_answer_costs_nothing(self):
        state = self.ready()
        row = BY_REGION_BAND["hearthford", "steady"]
        state.weapon = state.gear = None
        state.courier.technique = "quiet passage"
        before = copy.deepcopy(state.to_dict())
        changed, _, steps = resolve(state, row.id, "t")
        self.assertFalse(changed)
        self.assertEqual(steps, 0)
        self.assertEqual(state.to_dict(), before)

    def test_pressure_band_replaces_active_situation_and_round_trips(self):
        state = self.ready()
        row = activate_for_band(state, "strained")
        self.assertEqual(state.region.changes["situation:active"], row.id)
        resolve(state, row.id, "t")
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "situation.json"
            save_game(state, path)
            loaded = load_game(path)
        self.assertEqual(loaded.region.changes[f"micro-site:outcome:{row.id}"], row.answers[0])
        self.assertEqual(site_point(loaded, row), site_point(state, row))


if __name__ == "__main__":
    unittest.main()
