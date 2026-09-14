import unittest

from jomon.actions import interact
from jomon.frontiers import FRONTIERS, build_frontier
from jomon.landscape_variation import VARIANTS, _spawn_traveller, approach, enter_structure, traveller_choice
from jomon.navigation import navigation_targets
from jomon.regions import region_reachable, validate_region
from jomon.state import Position, create_world, game_state_from_dict
from jomon.world import area_name, position_key


class LandscapeVariationTests(unittest.TestCase):
    def test_all_regions_add_passable_landforms_and_reachable_upper_lower_side_rooms(self):
        for index in range(3):
            seed = f"landform geography {index}"
            state = create_world(seed)
            regions = {**state.regions, **{key: build_frontier(seed, key) for key in FRONTIERS}}
            self.assertEqual(set(regions), set(VARIANTS))
            for region in regions.values():
                with self.subTest(seed=seed, region=region.id):
                    validate_region(region)
                    reachable = region_reachable(region)
                    upper, lower = region.landmarks["field_upper"], region.landmarks["field_lower"]
                    self.assertEqual((upper.z, lower.z), (1, -1))
                    self.assertTrue({upper, lower} <= reachable)
                    self.assertTrue(any({link.first, link.second} == {upper, Position(upper.x, upper.y)}
                                        for link in region.vertical_links))
                    self.assertTrue(any({link.first, link.second} == {lower, Position(lower.x, lower.y)}
                                        for link in region.vertical_links))
                    self.assertTrue({region.landmarks[f"landform_{number}"] for number in range(3)} <= reachable)
                    self.assertTrue(all(f"landform:{number}" in region.generation_facts for number in range(3)))

    def test_field_traveller_marks_a_route_and_sells_one_physical_lot(self):
        state = create_world("field witness")
        state.location = "region"
        state.position = state.region.landmarks["landform_0"]
        self.assertIn("speak", _spawn_traveller(state, state.position))
        visitor = next(person for person in state.contacts["hearthford"] if person.id == "landform:hearthford:traveller")
        state.position = visitor.position
        self.assertEqual(interact(state).overlay, "field-traveller")
        start = state.world_time
        self.assertTrue(traveller_choice(state, "a").time_advanced)
        self.assertIn(position_key(state.region.landmarks["field_upper"]), state.region.seen)
        self.assertFalse(traveller_choice(state, "a").changed)
        state.trade_credit = 2
        self.assertTrue(traveller_choice(state, "b").time_advanced)
        self.assertEqual(state.trade_credit, 1)
        self.assertEqual(sum(item.provenance == f"purchased from {visitor.name}" for item in state.items), 1)
        self.assertFalse(traveller_choice(state, "b").changed)
        self.assertGreater(state.world_time, start)
        self.assertEqual(game_state_from_dict(state.to_dict()).to_dict(), state.to_dict())

    def test_named_landforms_and_side_rooms_appear_in_navigation_and_area(self):
        state = create_world("named side rooms")
        state.location = "region"
        state.position = state.region.landmarks["field_upper"]
        self.assertIn(VARIANTS["hearthford"]["upper"], area_name(state))
        target = state.region.landmarks["landform_1"]
        state.region.seen.append(position_key(target))
        self.assertIn(VARIANTS["hearthford"]["pockets"][1][0],
                      [item.label for item in navigation_targets(state)])

    def test_field_events_are_bounded_and_structure_visits_do_not_repeat(self):
        state = create_world("field events")
        state.location = "region"
        state.position = state.region.landmarks["landform_2"]
        state.expedition_count = 1
        self.assertTrue(approach(state))
        self.assertEqual(approach(state), "")
        state.position = state.region.landmarks["field_upper"]
        self.assertTrue(enter_structure(state))
        self.assertEqual(enter_structure(state), "")
        for visit in range(2, 35):
            state.expedition_count = visit
            state.position = state.region.landmarks["landform_2"]
            approach(state)
            state.position = state.region.landmarks["field_upper"]
            enter_structure(state)
        self.assertLessEqual(state.region.changes.get("landform:beasts", 0), 2)
        self.assertLessEqual(state.region.changes.get("landform:raiders", 0), 2)
        self.assertLessEqual(state.region.changes.get("landform:elites", 0), 1)
        self.assertEqual(game_state_from_dict(state.to_dict()).to_dict(), state.to_dict())

    def test_dense_dunmire_caves_take_a_smaller_but_connected_side_structure(self):
        region = build_frontier("systemic-audit-0043", "dunmire")
        validate_region(region)
        reachable = region_reachable(region)
        self.assertIn(region.landmarks["field_lower"], reachable)
        self.assertIn(region.landmarks["field_upper"], reachable)


if __name__ == "__main__":
    unittest.main()
