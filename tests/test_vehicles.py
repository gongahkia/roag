from __future__ import annotations

import unittest
import tempfile
from pathlib import Path

from jomon.actions import interact, move
from jomon.regions import activate_region
from jomon.save import load_game, save_game
from jomon.state import SAVE_FORMAT, Position, StateError, create_world, game_state_from_dict, validate_state
from jomon.terminal import InputEvent, OverlayView, _draw_map, _draw_vehicle_interior, _handle_overlay, _handle_overlay_view, _overlay_lines, dialogue_choices
from jomon.vehicles import (
    JOMON_DOCK,
    SHORE_DOCK,
    SPECS,
    board_tug,
    deck_rows,
    harbour_rows,
    interior_entry,
    interior_step,
    service,
)
from jomon.vessel import JOMON_GANGPLANK
from jomon.world import area_name, map_rows
from jomon.world import position_key


class VehicleNavigationTests(unittest.TestCase):
    class Screen:
        def __init__(self):
            self.writes = []

        def getmaxyx(self):
            return 30, 80

        def addnstr(self, row, col, value, count, attr=0):
            self.writes.append((row, col, value[:count]))

        def refresh(self):
            pass

    def test_tug_round_trip_uses_a_real_water_map_and_preserves_return(self):
        state = create_world("tug round trip")
        self.assertEqual([option.key for option in dialogue_choices(state, "gangplank")], ["1", "2"])
        self.assertEqual(_handle_overlay(state, "gangplank", ord("2")), (None, False))
        self.assertEqual((state.jomon_space, state.position, state.active_vehicle_id), ("harbour", JOMON_DOCK, "tug"))
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "on-water.json"
            save_game(state, path)
            state = load_game(path)
        self.assertEqual((state.jomon_space, state.active_vehicle_id), ("harbour", "tug"))
        self.assertIn("open-water", area_name(state))
        self.assertEqual(map_rows(state)[SHORE_DOCK.y][SHORE_DOCK.x], "L")
        self.assertEqual(harbour_rows(state.seed), harbour_rows(state.seed))
        screen = self.Screen()
        _draw_map(screen, state, 0, 0, 18, 51)
        self.assertTrue(any(glyph == "U" for _, _, glyph in screen.writes))
        screen.writes.clear()
        interior = OverlayView("vehicle-interior")
        _draw_vehicle_interior(screen, state, interior)
        self.assertTrue(any(glyph == "@" for _, _, glyph in screen.writes))
        self.assertTrue(any(glyph == "H" for _, _, glyph in screen.writes))
        self.assertEqual(interior.cursor, interior_entry("tug"))
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
        state = game_state_from_dict(state.to_dict())
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

    def test_vehicle_decks_are_walkable_and_fixtures_require_position(self):
        for vehicle_id in SPECS:
            rows = deck_rows(vehicle_id)
            self.assertEqual(len({len(row) for row in rows}), 1)
            start = interior_entry(vehicle_id)
            self.assertEqual(rows[start.y][start.x], "E")
            self.assertEqual(interior_step(vehicle_id, start, -100, 0), start)
            frontier, seen = [start], {start}
            while frontier:
                point = frontier.pop()
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    target = interior_step(vehicle_id, point, dx, dy)
                    if target not in seen:
                        seen.add(target)
                        frontier.append(target)
            self.assertTrue({rows[point.y][point.x] for point in seen} >= set("HRFCE"))

        state = create_world("cabin controls")
        board_tug(state)
        tug = state.vehicles["tug"]
        tug.fuel = 0
        state.trade_credit = 2
        view = OverlayView("vehicle-interior")
        before = state.world_time
        self.assertFalse(_handle_overlay_view(state, view, InputEvent("key", ord("l")))[0])
        self.assertEqual(state.world_time, before)
        self.assertFalse(_handle_overlay_view(state, view, InputEvent("key", ord("e")))[0])
        self.assertEqual(tug.fuel, 0)
        reserve = next(Position(x, y) for y, row in enumerate(deck_rows("tug"))
                       for x, tile in enumerate(row) if tile == "R")
        view.cursor = reserve
        self.assertFalse(_handle_overlay_view(state, view, InputEvent("key", ord("e")))[0])
        self.assertGreater(tug.fuel, 0)
        self.assertGreater(state.world_time, before)
        self.assertTrue(_handle_overlay_view(state, view, InputEvent("key", 9))[0])
        self.assertEqual((state.active_vehicle_id, state.position), ("tug", JOMON_DOCK))

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

    def test_cart_moves_on_a_real_road_and_spends_charge(self):
        state = create_world("cart road movement")
        state.location = "region"
        cart = state.vehicles["horse_cart"]
        state.position = cart.position
        self.assertTrue(interact(state).changed)
        original = state.position
        fuel = cart.fuel
        directions = ((1, 0), (-1, 0), (0, 1), (0, -1))
        heading = next((dx, dy) for dx, dy in directions
                       if state.region.levels["0"][original.y + dy][original.x + dx] in {"=", "."})
        first_step = Position(original.x + heading[0], original.y + heading[1])
        state.water[position_key(first_step)] = 3
        self.assertTrue(move(state, *heading).changed)
        self.assertEqual(state.position, first_step)
        self.assertEqual(cart.position, state.position)
        self.assertLess(cart.fuel, fuel)
        validate_state(game_state_from_dict(state.to_dict()))

    def test_rootwalker_crosses_dense_canopy_without_allowing_dismount(self):
        state = create_world("rootwalker canopy")
        activate_region(state, "greenwold")
        state.location = "region"
        rows = state.region.levels["0"]
        x, y, dx, dy = next((x, y, dx, dy) for y in range(1, len(rows) - 1)
                            for x in range(1, len(rows[y]) - 1)
                            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
                            if rows[y][x] in {".", "="} and rows[y + dy][x + dx] == "T")
        rootwalker = state.vehicles["rootwalker"]
        state.position = rootwalker.position = Position(x, y)
        state.active_vehicle_id = rootwalker.id
        self.assertTrue(move(state, dx, dy).changed)
        self.assertEqual(state.position, Position(x + dx, y + dy))
        self.assertFalse(interact(state).changed)
        validate_state(state)

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
        glider.fuel = 0
        before = state.world_time
        self.assertTrue(move(state, -dx, -dy).changed)
        self.assertEqual(state.world_time, before + 2)
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
        corrupt = migrated.to_dict()
        corrupt["vehicles"]["tug"]["fuel"] = -1
        with self.assertRaises(StateError):
            game_state_from_dict(corrupt)
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
