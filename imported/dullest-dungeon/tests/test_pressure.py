import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine
from dumbest_dungeon.pressure import ACTION_PRESSURE, BANDS, PressureSource, action_price, pressure_band, pressure_status


class PressureContractTests(unittest.TestCase):
    def test_bands_are_named_monotonic_and_forecast_the_next_threshold(self) -> None:
        self.assertEqual(sorted(band.threshold for band in BANDS), [band.threshold for band in BANDS])
        self.assertEqual(len(BANDS), len({band.id for band in BANDS}))
        for index, band in enumerate(BANDS):
            self.assertEqual(band, pressure_band(band.threshold))
            status = pressure_status(band.threshold)
            self.assertEqual(band.id, status["band"])
            self.assertEqual(0, status["progress"])
            if index + 1 < len(BANDS):
                self.assertEqual(BANDS[index + 1].threshold, status["next_threshold"])
                self.assertEqual(BANDS[index + 1].threshold - band.threshold, status["remaining"])
                self.assertEqual(BANDS[index + 1].forecast, status["forecast"])

    def test_only_registered_simulation_actions_have_integer_prices(self) -> None:
        self.assertEqual(set(PressureSource), set(ACTION_PRESSURE))
        self.assertEqual(3, action_price(PressureSource.TRAVEL, 3))
        self.assertEqual(16, action_price(PressureSource.ENEMY_ROUND, 2))
        self.assertEqual(0, action_price(PressureSource.EVENT, 99))
        for invalid in (-1, True, 1.5):
            with self.assertRaises(ValueError):
                action_price(PressureSource.TRAVEL, invalid)
        with self.assertRaises(ValueError):
            pressure_status(-1)

    def test_route_and_interface_queries_project_without_advancing_pressure(self) -> None:
        engine = GameEngine.new(load_catalog(), 4242)
        origin = (engine.state.party_x, engine.state.party_y)
        path = engine._find_path(origin, engine.room_position(1))[:4]
        before = engine.snapshot()
        intel = engine.route_intel(path)
        self.assertEqual(intel["ticks"], intel["pressure"])
        self.assertEqual(engine.state.pressure + intel["pressure"], intel["projected_pressure"])
        self.assertEqual(pressure_band(int(intel["projected_pressure"])).name,
                         intel["projected_pressure_band"])
        pressure_status(engine.state.pressure)
        self.assertEqual(before, engine.snapshot())

    def test_registered_world_actions_advance_once_with_disclosed_causes(self) -> None:
        catalog = load_catalog()

        travel = GameEngine.new(catalog, 101)
        for patrol in travel.state.patrols:
            patrol.active = False
        destination = travel._neighbors((travel.state.party_x, travel.state.party_y))[0]
        expected_ticks = travel.movement_cost(*destination)
        travel.step_exploration(*destination)
        self.assertEqual(expected_ticks, travel.state.pressure)
        self.assertEqual("travel", travel.state.pressure_recent[-1]["source"])

        facility_engine = GameEngine.new(catalog, 102)
        facility = facility_engine.state.facilities[0]
        facility_engine.state.party_x, facility_engine.state.party_y = facility.x, facility.y
        facility_engine._resolve_exploration_tile()
        definition = facility_engine.facility_definition(facility)
        option = next(option for option in definition["options"]
                      if facility_engine.facility_option_available(facility, option["id"])[0])
        before = facility_engine.state.pressure
        facility_engine.resolve_facility(option["id"])
        self.assertEqual(before + action_price(PressureSource.FACILITY), facility_engine.state.pressure)

        objective_engine = GameEngine.new(catalog, 103)
        objective = objective_engine.state.objectives[0]
        objective_engine.state.party_x, objective_engine.state.party_y = objective.x, objective.y
        objective_engine._resolve_exploration_tile()
        approach = objective_engine.mission_definition(objective.biome_id)["approaches"][0]
        objective_engine.begin_objective(approach["id"])
        objective_engine.state.party_x, objective_engine.state.party_y = objective_engine.objective_position(objective)
        objective_engine._resolve_exploration_tile()
        before = objective_engine.state.pressure
        objective_engine.advance_objective()
        self.assertEqual(before + action_price(PressureSource.OBJECTIVE_STAGE),
                         objective_engine.state.pressure)

        combat = GameEngine.new(catalog, 104)
        combat.start_combat("lost_shift")
        before = combat.state.pressure
        combat.end_turn()
        self.assertEqual(before + action_price(PressureSource.ENEMY_ROUND), combat.state.pressure)
        self.assertEqual("enemy_round", combat.state.pressure_recent[-1]["source"])

    def test_recent_causes_are_bounded_and_round_trip_at_a_band_boundary(self) -> None:
        engine = GameEngine.new(load_catalog(), 105)
        for index in range(10):
            engine._advance_pressure(PressureSource.OBJECTIVE_STAGE, 1, f"stage {index}")
        self.assertEqual(8, len(engine.state.pressure_recent))
        engine._advance_pressure(PressureSource.TRAVEL, 60, "threshold route")
        self.assertEqual("WATCHFUL", pressure_band(engine.state.pressure).name)
        restored = GameEngine.from_snapshot(engine.catalog, engine.snapshot())
        self.assertEqual(engine.snapshot(), restored.snapshot())


if __name__ == "__main__":
    unittest.main()
