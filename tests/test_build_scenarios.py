import copy
import unittest

from jomon.build_scenarios import BUILD_SCENARIOS, validate_build_scenarios
from jomon.frontiers import ensure_frontier
from jomon.inventory import auto_place, create_item, load_state, sync_legacy_load
from jomon.state import Position, create_world, game_state_from_dict
from jomon.world import build_combinations


class BuildScenarioTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("twenty four build demonstrations")

    def prepare(self, scenario):
        state = copy.deepcopy(self.base)
        ensure_frontier(state, scenario.region)
        state.location = "region"
        state.active_region_id = scenario.region
        state.position = Position(40, 25, scenario.elevation)
        state.weather = scenario.weather
        state.support = scenario.support
        state.guarded_step = scenario.guarded
        state.terrain_statuses.clear()
        state.courier.technique = scenario.technique
        for item in state.items:
            if item.owner_id == state.active_courier_id:
                item.location, item.owner_id = "lost", None
        create_item(
            state, scenario.weapon, f"{scenario.id} readied form",
            location="readied", owner_id=state.active_courier_id,
        )
        create_item(
            state, scenario.gear, f"{scenario.id} secondary",
            location="secondary", owner_id=state.active_courier_id,
        )
        for passive in scenario.passives:
            item = create_item(
                state, f"passive:{passive}", f"{scenario.id} discovery",
                owner_id=state.active_courier_id,
            )
            self.assertTrue(auto_place(
                state, item.id, "pack", owner_id=state.active_courier_id
            ))
        if scenario.burdened:
            cargo = create_item(
                state, "commodity:timber", f"{scenario.id} counterweight",
                owner_id=state.active_courier_id, quantity=3,
            )
            self.assertTrue(auto_place(
                state, cargo.id, "pack", owner_id=state.active_courier_id
            ))
        sync_legacy_load(state)
        return state

    def test_manifest_has_twenty_four_distinct_valid_production_scenarios(self):
        validate_build_scenarios()
        self.assertEqual(len(BUILD_SCENARIOS), 24)
        self.assertEqual(len({entry.identity for entry in BUILD_SCENARIOS}), 24)
        self.assertEqual(len({entry.production_reducer for entry in BUILD_SCENARIOS}), 14)

    def test_each_demonstration_activates_its_documented_cross_system_result(self):
        for scenario in BUILD_SCENARIOS:
            with self.subTest(build=scenario.id):
                state = self.prepare(scenario)
                self.assertIn(scenario.expected_combo, build_combinations(state))
                self.assertTrue(scenario.decision)
                if scenario.burdened:
                    self.assertIn(load_state(state), {"laden", "encumbered"})
                loaded = game_state_from_dict(state.to_dict())
                self.assertIn(scenario.expected_combo, build_combinations(loaded))


if __name__ == "__main__":
    unittest.main()
