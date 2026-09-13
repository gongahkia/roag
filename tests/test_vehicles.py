from __future__ import annotations

import unittest

from jomon.actions import interact, move
from jomon.regions import activate_region
from jomon.state import SAVE_FORMAT, Position, create_world, game_state_from_dict, validate_state
from jomon.terminal import _overlay_lines
from jomon.vehicles import (
    JOMON_DOCK,
    SHORE_DOCK,
    SPECS,
    board_tug,
    harbour_rows,
    service,
)
from jomon.vessel import JOMON_GANGPLANK
from jomon.world import area_name, map_rows


class VehicleNavigationTests(unittest.TestCase):
    def test_tug_round_trip_uses_a_real_water_map_and_preserves_return(self):
        state = create_world("tug round trip")
        self.assertEqual(interact(state).overlay, "gangplank")
        self.assertTrue(board_tug(state).changed)
        self.assertEqual((state.jomon_space, state.position, state.active_vehicle_id), ("harbour", JOMON_DOCK, "tug"))
        self.assertIn("open-water", area_name(state))
        self.assertEqual(map_rows(state)[SHORE_DOCK.y][SHORE_DOCK.x], "L")
        self.assertEqual(harbour_rows(state.seed), harbour_rows(state.seed))
        self.assertEqual(move(state, 0, -1).changed, True)
        self.assertEqual(move(state, 0, 1).changed, True)
        for _ in range(25):
            if state.position == SHORE_DOCK:
                break
            self.assertTrue(move(state, 1, 0).changed)
        self.assertEqual(state.position, SHORE_DOCK)
        self.assertTrue(interact(state).changed)
        self.assertEqual(state.location, "region")
        self.assertTrue(state.expedition_by_tug)
        self.assertTrue(interact(state).changed)
        self.assertEqual((state.location, state.jomon_space, state.position), ("jomon", "harbour", SHORE_DOCK))
        self.assertTrue(state.returning_by_tug)
        for _ in range(25):
            if state.position == JOMON_DOCK:
                break
            self.assertTrue(move(state, -1, 0).changed)
        self.assertEqual(state.position, JOMON_DOCK)
        self.assertTrue(interact(state).changed)
        self.assertEqual((state.jomon_space, state.position, state.active_vehicle_id), ("vessel", JOMON_GANGPLANK, None))
        self.assertEqual(state.returned_expeditions, 1)
        validate_state(state)

    def test_water_tug_has_shoals_fuel_and_a_nonlocking_oar_fallback(self):
        state = create_world("tug oars")
        board_tug(state)
        tug = state.vehicles["tug"]
        before = state.world_time
        tug.fuel = 0
        self.assertTrue(move(state, 1, 0).changed)
        self.assertEqual(state.position, Position(JOMON_DOCK.x + 1, JOMON_DOCK.y))
        self.assertEqual(state.world_time, before + 2)
        self.assertEqual(tug.fuel, 0)
        self.assertEqual(_overlay_lines(state, "vehicle-interior")[0], SPECS["tug"]["name"].upper())

    def test_regional_vehicles_board_steer_dismount_and_persist(self):
        for region_id, vehicle_id in (("hearthford", "horse_cart"), ("rillscar", "steam_crawler"),
                                      ("greenwold", "rootwalker"), ("greywash", "aether_glider")):
            with self.subTest(vehicle=vehicle_id):
                state = create_world("vehicle " + vehicle_id)
                activate_region(state, region_id)
                state.location = "region"
                vehicle = state.vehicles[vehicle_id]
                state.position = vehicle.position
                self.assertTrue(interact(state).changed)
                self.assertEqual(state.active_vehicle_id, vehicle_id)
                self.assertGreater(vehicle.fuel, 0)
                self.assertTrue(interact(state).changed)
                self.assertIsNone(state.active_vehicle_id)
                loaded = game_state_from_dict(state.to_dict())
                self.assertEqual(loaded.vehicles[vehicle_id], vehicle)
                validate_state(loaded)

    def test_glider_crosses_deep_water_but_cannot_disembark_on_it(self):
        state = create_world("glider over estuary")
        activate_region(state, "greywash")
        state.location = "region"
        rows = state.region.levels["0"]
        x, y, dx, dy = next((x, y, dx, dy) for y in range(1, len(rows) - 1)
                            for x in range(1, len(rows[y]) - 1)
                            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                            if rows[y][x] in {".", "=", "w"} and rows[y + dy][x + dx] == "~")
        start = Position(x, y)
        glider = state.vehicles["aether_glider"]
        state.position = glider.position = start
        state.active_vehicle_id = glider.id
        self.assertTrue(move(state, dx, dy).changed)
        self.assertEqual(state.region.levels["0"][state.position.y][state.position.x], "~")
        self.assertFalse(interact(state).changed)
        self.assertEqual(state.active_vehicle_id, glider.id)
        validate_state(state)

    def test_recovery_and_old_save_migration_are_bounded(self):
        state = create_world("vehicle migration")
        old = state.to_dict()
        old["save_format"] = 10
        for key in ("vehicles", "active_vehicle_id", "expedition_by_tug", "returning_by_tug"):
            del old[key]
        migrated = game_state_from_dict(old)
        self.assertEqual(migrated.save_format, SAVE_FORMAT)
        self.assertEqual(migrated.world_time, state.world_time)
        self.assertEqual(set(migrated.vehicles), set(state.vehicles))
        board_tug(migrated)
        tug = migrated.vehicles["tug"]
        tug.condition = 0
        before = migrated.world_time
        self.assertTrue(service(migrated, repair=True).changed)
        self.assertEqual((tug.condition, migrated.world_time), (1, before + 3))
        tug.fuel = 0
        self.assertFalse(service(migrated).changed)


if __name__ == "__main__":
    unittest.main()
