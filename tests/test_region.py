from __future__ import annotations

import unittest

from jomon.actions import (
    attack,
    choose_courier,
    choose_gear,
    choose_support,
    choose_weapon,
    emit_sound,
    interact,
    use_gear,
)
from jomon.state import Position, create_world
from jomon.world import (
    JOMON_GANGPLANK,
    camera_origin,
    connected_required_map,
    field_of_view,
    line_of_sight,
    position_key,
    reachable_positions,
    remembered,
    sight_radius,
    vertical_destination,
)


def prepared(seed: str = "seamless region", *, weapon: str = "spear", gear: str = "rope"):
    state = create_world(seed)
    choose_courier(state, state.household[0].id)
    if weapon not in state.owned_weapons:
        state.owned_weapons.append(weapon)
    if gear not in state.owned_gear:
        state.owned_gear.append(gear)
    choose_weapon(state, weapon)
    choose_gear(state, gear)
    choose_support(state, "route survey")
    state.location, state.current_room = "region", "hearthford"
    state.position = state.region.landmarks["landing"]
    state.threats = [threat for threat in state.threats if threat.id != "road-patrol"]
    return state


class RegionalGenerationTests(unittest.TestCase):
    def test_same_seed_generates_identical_region(self):
        first = create_world("broad water 81")
        second = create_world("broad water 81")
        self.assertEqual(first.to_dict(), second.to_dict())

    def test_selected_seeds_change_geography(self):
        states = [create_world(f"geography {index}") for index in range(10)]
        signatures = {state.region.geography_signature for state in states}
        landings = {state.region.landmarks["landing"] for state in states}
        self.assertGreaterEqual(len(signatures), 8)
        self.assertGreaterEqual(len(landings), 3)

    def test_landmarks_are_reachable_across_representative_seeds(self):
        for index in range(25):
            with self.subTest(seed=index):
                state = create_world(f"reachability {index}")
                self.assertTrue(connected_required_map(state))
                reachable = reachable_positions(state)
                self.assertIn(state.region.landmarks["objective"], reachable)
                self.assertIn(state.region.landmarks["landing"], reachable)
                for container in state.region.containers:
                    self.assertIn(container.position, reachable, container.name)

    def test_four_spatially_aligned_levels_and_valid_links(self):
        state = create_world("vertical links")
        state.location = "region"
        self.assertEqual(set(state.region.levels), {"-1", "0", "1", "2"})
        for link in state.region.vertical_links:
            self.assertEqual((link.first.x, link.first.y), (link.second.x, link.second.y))
            self.assertEqual(abs(link.first.z - link.second.z), 1)
            self.assertEqual(vertical_destination(state, link.first), link.second)
            self.assertEqual(vertical_destination(state, link.second), link.first)

    def test_camera_clamps_at_edges_and_centres(self):
        self.assertEqual(camera_origin(Position(0, 0), 96, 54, 49, 14), (0, 0))
        self.assertEqual(camera_origin(Position(95, 53), 96, 54, 49, 14), (47, 40))
        self.assertEqual(camera_origin(Position(48, 27), 96, 54, 49, 14), (24, 20))


class VisibilityAndVerticalTests(unittest.TestCase):
    def test_fov_wall_occludes_and_seen_terrain_is_remembered(self):
        state = prepared()
        state.position = Position(20, 22, 0)
        state.region.tile_changes[position_key(Position(21, 22, 0))] = "#"
        state.region.tile_changes[position_key(Position(22, 22, 0))] = "."
        visible = field_of_view(state)
        self.assertIn(Position(21, 22, 0), visible)
        self.assertNotIn(Position(22, 22, 0), visible)
        state.position = Position(30, 22, 0)
        field_of_view(state)
        self.assertTrue(remembered(state, Position(20, 22, 0)))

    def test_outdoor_height_weather_and_cave_change_radius(self):
        state = prepared()
        state.position = Position(42, 28, 0)
        clear = sight_radius(state)
        state.weather = "river fog"
        self.assertLess(sight_radius(state), clear)
        state.weather, state.position = "clear", Position(84, 16, 2)
        self.assertGreater(sight_radius(state), clear)
        state.position = Position(62, 42, -1)
        self.assertLess(sight_radius(state), clear)

    def test_new_region_weather_limits_player_sight(self):
        state = prepared()
        state.position = Position(40, 25)
        for y in range(10, 41):
            for x in range(25, 56):
                state.region.tile_changes[f"{x},{y},0"] = "."
        state.weather = "clear"
        clear = sight_radius(state)
        for weather in ("coast squall", "forest rain"):
            state.weather = weather
            self.assertLess(sight_radius(state), clear)

    def test_cross_level_sight_and_attack_through_ladder(self):
        state = prepared(weapon="spear")
        state.position = Position(47, 10, 1)
        target = next(threat for threat in state.threats if threat.id == "tower-bow")
        self.assertEqual(target.position, Position(47, 10, 2))
        blocked = interact(state)
        self.assertFalse(blocked.time_advanced)
        self.assertEqual(state.position, Position(47, 10, 1))
        target.status, target.health = "engaged", 5
        self.assertTrue(line_of_sight(state, state.position, target.position))
        result = attack(state)
        self.assertTrue(result.time_advanced)
        self.assertLess(target.health, 5)

    def test_destroyed_floor_causes_fall_and_persists(self):
        state = prepared(weapon="hand axe")
        state.position = Position(78, 22, 1)
        for threat in state.threats:
            threat.status = "defeated"
        health = state.courier.health
        result = interact(state)
        self.assertTrue(result.time_advanced)
        self.assertEqual(state.position, Position(78, 22, 0))
        self.assertEqual(state.region.tile_changes["78,22,1"], "O")
        self.assertLess(state.courier.health, health)

    def test_smoke_sound_and_water_cross_levels_boundedly(self):
        state = prepared(gear="smoke pot")
        state.smoke_charges = 1
        state.position = Position(78, 22, 0)
        state.region.tile_changes["78,22,1"] = "O"
        use_gear(state)
        self.assertIn("78,22,0", state.smoke)
        self.assertIn("78,22,1", state.smoke)
        watcher = next(threat for threat in state.threats if threat.profile == "ranged")
        watcher.position, watcher.status = Position(78, 22, 1), "watching"
        emit_sound(state, 2)
        self.assertEqual(watcher.status, "engaged")
        state.position = Position(82, 42, -1)
        interact(state)
        self.assertTrue({"58,42,-1", "58,42,0"} <= set(state.water))
        self.assertLessEqual(len(state.water), 3)


if __name__ == "__main__":
    unittest.main()
