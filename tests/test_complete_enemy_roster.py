import copy
import unittest

from jomon.actions import _threat_action
from jomon.content import EXPANDED_STANDARD_ACTORS, FRONTIER_ACTORS
from jomon.encounters import compose_encounter
from jomon.encounters import roster_audit, validate_roster
from jomon.inventory import auto_place, create_item, record_acquisition
from jomon.state import Position, create_world


class CompleteEnemyRosterTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("complete enemy roster")

    def state_for(self, actor_id, point=Position(45, 25)):
        state = copy.deepcopy(self.base)
        state.location = "region"
        state.position = Position(40, 25, point.z)
        for y in range(20, 31):
            for x in range(35, 55):
                state.region.tile_changes[f"{x},{y},{point.z}"] = "."
        actor = next(threat for threat in state.threats if threat.id == actor_id)
        actor.position = point
        actor.home_position = point
        actor.status = "engaged"
        state.threats = [actor]
        return state, actor

    def test_roster_has_seventy_two_distinct_standards_and_sixteen_elites(self):
        validate_roster()
        report = roster_audit()
        self.assertEqual(report["standard_archetypes"], 72)
        self.assertEqual(report["mechanically_distinct_signatures"], 72)
        self.assertEqual(report["elite_situations"], 16)
        self.assertEqual(report["named_recurring_rivals"], 4)
        self.assertEqual(set(report["regions"].values()), {9})
        self.assertFalse(report["invalid_standard_rows"])
        self.assertFalse(report["duplicate_standard_glyphs"])

    def test_each_expanded_role_has_a_production_group_and_tested_reducer(self):
        retained_duties = {row[5] for row in FRONTIER_ACTORS}
        seen = set()
        for row in EXPANDED_STANDARD_ACTORS:
            identity, region, _, _, role, duty, ecology, *rest = row
            plan = compose_encounter(
                "expanded exact production", region, "steady", 0,
                candidates=(identity,),
            )
            self.assertEqual(plan.archetypes, (identity,))
            self.assertIn(" or ", row[-1])
            self.assertTrue(role)
            self.assertTrue(ecology)
            if duty != "feed":
                self.assertIn(duty, retained_duties)
            seen.add(region)
        self.assertEqual(len(EXPANDED_STANDARD_ACTORS), 24)
        self.assertEqual(len(seen), 8)

    def test_all_three_new_hearthford_roles_appear_in_ordinary_seeded_worlds(self):
        seen = set()
        for index in range(24):
            state = create_world(f"expanded hearthford production {index}")
            seen.update(
                actor.id.split(":", 1)[1] for actor in state.threats
                if actor.id.startswith("hearthford-expanded:")
            )
        self.assertEqual(seen, {
            "hearth-sluice-runner", "hearth-rope-cutter", "hearth-meadow-kite",
        })

    def test_hearthford_lookout_uses_group_alarm_from_production_actor(self):
        state, lookout = self.state_for("road-patrol", Position(43, 25))
        ally = next(threat for threat in self.base.threats if threat.id == "tower-bow")
        ally = copy.deepcopy(ally)
        ally.position, ally.status = Position(49, 25), "watching"
        state.threats.append(ally)
        message = _threat_action(state, lookout, False)
        self.assertIn("alarm", message)
        self.assertEqual(ally.status, "engaged")

    def test_hearthford_boar_telegraphs_charge_and_mud_is_a_counter(self):
        state, boar = self.state_for("reed-boar", Position(41, 25))
        first = _threat_action(state, boar, False)
        self.assertIn("lowers its head", first)
        state.region.tile_changes["40,25,0"] = "m"
        second = _threat_action(state, boar, False)
        self.assertIn("deep mud", second)
        self.assertEqual(boar.status, "evaded")

    def test_hearthford_roof_keeper_aims_before_heavy_shot(self):
        state, keeper = self.state_for("tower-bow", Position(48, 25, 2))
        state.position = Position(40, 25, 2)
        message = _threat_action(state, keeper, False)
        self.assertIn("aims heavy crossbow", message)
        self.assertEqual(keeper.aimed_at, state.position)

    def test_hearthford_mill_levy_interposes_for_its_slinger(self):
        state, levy = self.state_for("mill-spear", Position(46, 25))
        slinger = copy.deepcopy(next(
            threat for threat in self.base.threats if threat.id == "gantry-bow"
        ))
        slinger.position, slinger.status = Position(50, 25), "engaged"
        state.threats.append(slinger)
        message = _threat_action(state, levy, False)
        self.assertIn("ranged ally", message)
        self.assertNotEqual(levy.position, Position(46, 25))

    def test_hearthford_gantry_slinger_feeds_a_bounded_smoke_lane(self):
        state, slinger = self.state_for("gantry-bow", Position(45, 25, 1))
        state.position = Position(40, 25, 1)
        message = _threat_action(state, slinger, False)
        self.assertIn("bounded smoke lane", message)
        self.assertTrue(state.smoke)

    def test_hearthford_reaver_takes_and_can_drop_exact_physical_item(self):
        state, reaver = self.state_for("pressure-reavers", Position(41, 25))
        item = create_item(
            state, "passive:witness token", "visible accountable target",
            owner_id=state.active_courier_id,
        )
        self.assertTrue(auto_place(
            state, item.id, "pack", owner_id=state.active_courier_id
        ))
        record_acquisition(state, item)
        message = _threat_action(state, reaver, False)
        self.assertIn("takes", message)
        self.assertEqual((item.location, reaver.carrying_item_id), ("enemy", item.id))
        reaver.health = 0
        from jomon.inventory import release_enemy_possession

        self.assertIn("falls", release_enemy_possession(state, reaver))
        self.assertEqual(item.location, "ground")


if __name__ == "__main__":
    unittest.main()
