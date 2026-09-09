import copy
import unittest

from jomon.actions import _weather_and_deadline, decide_objective, interact
from jomon.frontiers import FRONTIERS, build_frontier
from jomon.quests import QUESTS, resolve_regional_quest
from jomon.regions import activate_region, region_reachable
from jomon.route_chart import neighbours
from jomon.state import Position, create_world, game_state_from_dict


class FrontierTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("frontier integration")

    def test_new_moorings_are_charted_but_geography_is_lazy(self):
        state = copy.deepcopy(self.base)
        self.assertEqual(len(state.regions), 4)
        self.assertEqual(len(state.route_nodes), 16)
        for region_id in FRONTIERS:
            self.assertGreaterEqual(len(neighbours(state, region_id)), 2)
            activate_region(state, region_id)
            self.assertEqual(state.region.id, region_id)
        self.assertEqual(len(state.regions), 8)

    def test_scheduled_contact_can_be_addressed_from_adjacent_tile(self):
        state = copy.deepcopy(self.base)
        activate_region(state, "dunmire")
        state.location = "region"
        point = state.actor_schedules[state.contact.id].position
        state.position = Position(point.x + 1, point.y, point.z)
        self.assertEqual(interact(state).overlay, "objective")
        self.assertTrue(decide_objective(state, "alter").time_advanced)
        state.region.changes["objective_altered"] = True
        point = state.actor_schedules[state.contact.id].position
        state.position = Position(point.x + 1, point.y, point.z)
        self.assertTrue(interact(state).time_advanced)
        self.assertEqual(state.objective_status, "completed")

    def test_chart_labels_do_not_enter_details_panel_at_supported_sizes(self):
        from jomon.terminal import _chart_screen_point, chart_label_position

        for width, height in ((80, 24), (100, 32)):
            map_width = width - max(27, min(36, width // 3))
            for node in self.base.route_nodes.values():
                x, _ = _chart_screen_point(node.x, node.y, map_width, height)
                left, label = chart_label_position(x, node.name, map_width)
                self.assertGreaterEqual(left, 1)
                self.assertLess(left + len(label), map_width)

    def test_geometry_is_deterministic_varied_and_reachable(self):
        for region_id in FRONTIERS:
            signatures = set()
            for seed in ("frontier alpha", "frontier beta", "frontier gamma"):
                with self.subTest(region=region_id, seed=seed):
                    first = build_frontier(seed, region_id)
                    self.assertEqual(first, build_frontier(seed, region_id))
                    signatures.add(first.geography_signature)
                    reachable = region_reachable(first)
                    self.assertTrue(set(first.landmarks.values()) <= reachable)
                    self.assertEqual({p.z for p in reachable}, {-1, 0, 1, 2})
                    self.assertEqual(len(first.containers), 8)
            self.assertGreater(len(signatures), 1)

    def test_loading_expanded_world_never_rebuilds_old_geography(self):
        state = copy.deepcopy(self.base)
        for region in state.regions.values():
            region.containers[0].opened = True
            region.tile_changes["8,8,0"] = "/"
            region.seen.append("8,8,0")
        for region_id in FRONTIERS:
            activate_region(state, region_id)
            state.region.containers[0].opened = True
            state.region.changes["visited account"] = region_id
        original = state.to_dict()
        loaded = game_state_from_dict(original)
        self.assertEqual(loaded.to_dict(), original)
        for region_id in FRONTIERS:
            activate_region(loaded, region_id)
            self.assertTrue(loaded.region.containers[0].opened)
            self.assertEqual(loaded.region.changes["visited account"], region_id)

    def test_version_six_chart_extension_preserves_existing_legs(self):
        state = copy.deepcopy(self.base)
        state.route_edges[0].travel_time = 19
        state.route_known.append("whitecairn")
        state.traversed_route_edges.append(state.route_edges[0].id)
        data = state.to_dict()
        data["save_format"] = 6
        data["route_nodes"] = {k: v for k, v in data["route_nodes"].items() if k not in FRONTIERS}
        data["route_edges"] = [e for e in data["route_edges"] if e["first"] not in FRONTIERS and e["second"] not in FRONTIERS]
        loaded = game_state_from_dict(data)
        self.assertEqual(loaded.route_edges[0].travel_time, 19)
        self.assertIn("whitecairn", loaded.route_known)
        self.assertEqual(loaded.traversed_route_edges, state.traversed_route_edges)
        self.assertEqual(set(loaded.regions), set(state.regions))

    def test_frontier_quest_choices_have_different_persistent_consequences(self):
        for region_id in FRONTIERS:
            endings = []
            for option in QUESTS[region_id]["final"]:
                state = copy.deepcopy(self.base)
                activate_region(state, region_id)
                state.location = "region"
                state.position = state.contact.position
                state.threats.clear()
                self.assertTrue(decide_objective(state, "refuse").time_advanced)
                self.assertEqual(state.questlines[region_id].stage, 2)
                state.position = state.region.landmarks["control"]
                self.assertTrue(interact(state).time_advanced)
                self.assertTrue(resolve_regional_quest(state, option[0])[0])
                self.assertEqual(state.questlines[region_id].status, "completed")
                endings.append((state.region.changes, state.market, state.route_edges))
                loaded = game_state_from_dict(state.to_dict())
                self.assertEqual(loaded.questlines, state.questlines)
            self.assertNotEqual(*endings)

    def test_process_damage_is_persistent_and_not_refilled_on_entry(self):
        for region_id in FRONTIERS:
            state = copy.deepcopy(self.base)
            activate_region(state, region_id)
            state.location = "region"
            state.position = state.region.landmarks["landing"]
            state.pressure_elapsed = state.region.process_thresholds[1]
            _weather_and_deadline(state)
            before = copy.deepcopy(state.region.materials)
            self.assertGreaterEqual(len(before), 3)
            activate_region(state, "hearthford")
            activate_region(state, region_id)
            self.assertEqual(state.region.materials, before)
            state.position = state.region.landmarks["control"]
            state.threats.clear()
            interact(state)
            state.pressure_elapsed = state.region.process_thresholds[2]
            _weather_and_deadline(state)
            self.assertTrue(all(not cell.fire and not cell.collapse_due for cell in state.region.materials.values()))


if __name__ == "__main__":
    unittest.main()
