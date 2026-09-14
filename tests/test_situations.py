import copy
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

from jomon.actions import depart
from jomon.frontiers import ensure_frontier
from jomon.regions import activate_region
from jomon.quests import FIELD_REPORT_RESPONSES, secondary_service_options, use_secondary_service
from jomon.save import load_game, save_game
from jomon.situations import (
    BY_REGION_BAND, SITUATIONS, activate_for_band, audit_situations,
    choices, inspect_lines, resolve, site_glyph, site_point,
    validate_situations,
)
from jomon.state import StateError, create_world, game_state_from_dict, validate_state
from jomon.terminal import _overlay_lines, dialogue_choices
from jomon.world import is_walkable
from jomon.materials import fields, key


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
        self.assertIn(key(site_point(state, row)), fields(state))
        self.assertIn(f"micro-site:material:{row.id}", state.region.changes)
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
            self.assertIn("Continuing effect", " ".join(inspect_lines(state, row.id)))
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

    def test_changed_site_offers_one_maintained_or_physical_sample_follow_up(self):
        row = BY_REGION_BAND["hearthford", "steady"]
        state = self.ready()
        self.assertTrue(resolve(state, row.id, "t")[0])
        self.assertEqual({key for key, *_ in choices(state, row.id)}, {"T", "M"})
        self.assertEqual({option.key for option in dialogue_choices(state, f"situation:{row.id}")}, {"T", "M"})
        self.assertIn("MAINTAIN", " ".join(_overlay_lines(state, f"situation:{row.id}")[1]))
        point = site_point(state, row)
        cell = fields(state)[key(point)]
        cell.support, cell.fire, cell.smoke = 1, 1, 2
        account = state.institutions["work:hearthford"]
        confidence = account.confidence
        changed, message, steps = resolve(state, row.id, "t")
        self.assertTrue(changed, message)
        self.assertEqual(steps, 2)
        self.assertEqual((cell.support, cell.fire, cell.smoke), (3, 0, 0))
        self.assertEqual(account.confidence, min(3, confidence + 1))
        self.assertEqual(state.region.changes[f"micro-site:afterwork:{row.id}"], "maintenance")
        self.assertEqual(choices(state, row.id), [])
        self.assertFalse(resolve(state, row.id, "m")[0])

        sampled = self.ready()
        self.assertTrue(resolve(sampled, row.id, "t")[0])
        before = copy.deepcopy(sampled.to_dict())
        with patch("jomon.inventory.auto_place", return_value=False):
            changed, _, steps = resolve(sampled, row.id, "m")
        self.assertFalse(changed)
        self.assertEqual(steps, 0)
        self.assertEqual(sampled.to_dict(), before)
        changed, message, steps = resolve(sampled, row.id, "m")
        self.assertTrue(changed, message)
        self.assertEqual(steps, 1)
        self.assertTrue(any(item.kind == "ingredient:spring water" and item.location == "pack"
                            for item in sampled.items))
        self.assertEqual(sampled.region.changes[f"micro-site:afterwork:{row.id}"], "sample")
        validate_state(sampled)

    def test_local_worker_reports_changed_site_publicly_or_privately_once(self):
        self.assertEqual(set(FIELD_REPORT_RESPONSES), {row.region_id for row in SITUATIONS})
        row = BY_REGION_BAND["hearthford", "steady"]
        for choice, marker in (("p", "public"), ("r", "private")):
            with self.subTest(choice=choice):
                state = self.ready()
                self.assertFalse(next(option for option in secondary_service_options(state) if option[0] == choice)[3])
                self.assertFalse(use_secondary_service(state, choice)[0])
                self.assertTrue(resolve(state, row.id, "t")[0])
                self.assertTrue(next(option for option in secondary_service_options(state) if option[0] == choice)[3])
                contact_id = state.contacts["hearthford"][1].id
                self.assertTrue(any(option.key == choice.upper() and option.available
                                    for option in dialogue_choices(state, f"contact-service:{contact_id}")))
                account = state.institutions["work:hearthford"]
                contact = state.contacts["hearthford"][1]
                before = (account.trust, account.confidence, account.obligation, contact.disposition, state.trade_credit)
                changed, message = use_secondary_service(state, choice)
                self.assertTrue(changed, message)
                self.assertEqual(state.region.changes[f"micro-site:report:{row.id}"], marker)
                if choice == "p":
                    self.assertEqual((account.trust, account.confidence),
                                     (min(3, before[0] + 1), min(3, before[1] + 1)))
                    self.assertEqual(state.trade_credit, before[4])
                else:
                    self.assertEqual(account.obligation, before[2] + 1)
                    self.assertEqual(contact.disposition, max(-3, before[3] - 1))
                    self.assertEqual(state.trade_credit, before[4] + 2)
                self.assertFalse(use_secondary_service(state, choice)[0])
                loaded = game_state_from_dict(state.to_dict())
                self.assertEqual(loaded.region.changes[f"micro-site:report:{row.id}"], marker)
                validate_state(loaded)

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

    def test_corrupt_site_and_mastery_references_are_rejected(self):
        state = self.ready()
        broken = state.to_dict()
        broken["regions"]["hearthford"]["changes"]["situation:active"] = "missing-situation"
        with self.assertRaises(StateError):
            game_state_from_dict(broken)
        broken = state.to_dict()
        broken["vessel_changes"]["manoeuvre:last:missing"] = 2
        with self.assertRaises(StateError):
            game_state_from_dict(broken)
        broken = state.to_dict()
        row = BY_REGION_BAND["hearthford", "steady"]
        broken["regions"]["hearthford"]["changes"][f"micro-site:report:{row.id}"] = "public"
        with self.assertRaises(StateError):
            game_state_from_dict(broken)


if __name__ == "__main__":
    unittest.main()
