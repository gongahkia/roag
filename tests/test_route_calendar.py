from __future__ import annotations

import copy
import unittest

from jomon.actions import _advance_world
from jomon.calendar import (
    ACTIONS_PER_DAY,
    DAYS_PER_SEASON,
    calendar_at,
    seasonal_stock_modifier,
)
from jomon.route_chart import (
    build_route_graph,
    chart_move,
    connected_nodes,
    neighbours,
    route_availability,
    route_preview,
)
from jomon.state import SAVE_FORMAT, create_world, game_state_from_dict
from jomon.travel import choose_destination, travel_animation_frames

class RouteGraphTests(unittest.TestCase):
    def test_seeded_graph_is_deterministic_connected_and_varied(self):
        first_nodes, first_edges = build_route_graph("route-one")
        again_nodes, again_edges = build_route_graph("route-one")
        _, other_edges = build_route_graph("route-two")
        self.assertEqual(first_nodes, again_nodes)
        self.assertEqual(first_edges, again_edges)
        self.assertNotEqual([edge.id for edge in first_edges], [edge.id for edge in other_edges])
        state = create_world("route-one")
        self.assertEqual(connected_nodes(state), set(first_nodes))
        self.assertEqual(set(neighbours(state, "hearthford")), {"reed-anchor", "willow-ferry", "dunmire", "marlbank"})




    def test_travel_frames_are_presentation_only_and_skip_parity_is_exact(self):
        animated = create_world("skip parity")
        skipped = create_world("skip parity")
        before = copy.deepcopy(animated.to_dict())
        frames = travel_animation_frames(animated, "hearthford", "reed-anchor")
        self.assertGreaterEqual(len(frames), 4)
        self.assertEqual(animated.to_dict(), before)
        choose_destination(animated, "reed-anchor")
        choose_destination(skipped, "reed-anchor")
        self.assertEqual(animated.to_dict(), skipped.to_dict())

    def test_season_can_close_a_known_edge(self):
        state = create_world("winter route")
        state.route_current_node = "greywash"
        target_day = (3 * DAYS_PER_SEASON - state.calendar_origin_day) % (4 * DAYS_PER_SEASON)
        state.world_time = target_day * ACTIONS_PER_DAY
        self.assertEqual(calendar_at(state).season, "winter")
        available, reason = route_availability(state, "ebb-crossing")
        self.assertFalse(available)
        self.assertIn("closed", reason)



class CalendarAndMigrationTests(unittest.TestCase):
    def test_calendar_advances_only_with_actions_and_records_observances(self):
        state = create_world("calendar")
        before = calendar_at(state)
        self.assertEqual(calendar_at(state), before)
        actions_to_next_season = ((DAYS_PER_SEASON - state.calendar_origin_day) * ACTIONS_PER_DAY)
        _advance_world(state, steps=actions_to_next_season)
        self.assertEqual(calendar_at(state).season, "summer")
        self.assertTrue(any("Summer begins" in event for event in state.calendar_events))

    def test_solstice_equinox_and_seasonal_stock_are_explicit(self):
        state = create_world("observances")
        state.calendar_origin_day = 0
        spring = calendar_at(state, 0)
        summer_solstice = calendar_at(
            state, (DAYS_PER_SEASON + DAYS_PER_SEASON // 2 - 1) * ACTIONS_PER_DAY
        )
        self.assertEqual(spring.observance, "spring equinox")
        self.assertEqual(summer_solstice.observance, "summer solstice")
        state.world_time = 0
        self.assertEqual(seasonal_stock_modifier(state, "reed-tonic"), 1)
        self.assertEqual(seasonal_stock_modifier(state, "winter-juniper"), 0)

    def test_save_round_trip_preserves_route_schedule_and_calendar_exactly(self):
        import tempfile
        from pathlib import Path
        from jomon.save import load_game, save_game

        state = create_world("living persistence")
        _advance_world(state, steps=19)
        choose_destination(state, neighbours(state, state.route_current_node)[0])
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "living.json"
            save_game(state, path)
            loaded = load_game(path)
        self.assertEqual(loaded.to_dict(), state.to_dict())

    def test_format_four_migrates_without_item_or_consequence_loss(self):
        state = create_world("format-four")
        state.history.append("preserved consequence")
        raw = state.to_dict()
        raw["save_format"] = 4
        for key in (
            "jomon_space", "vessel_integrity", "vessel_changes", "route_nodes",
            "route_edges", "route_current_node", "route_known",
            "traversed_route_edges", "auto_place_enabled", "actor_schedules",
            "bartender", "bartender_stock", "drink_effects", "calendar_origin_day",
            "calendar_events", "chronicle", "pending_incident", "last_schedule_turn",
        ):
            raw.pop(key, None)
        for item in raw["items"]:
            item.pop("pinned", None)
            item.pop("merged_into", None)
        item_ids = {item["id"] for item in raw["items"]}
        loaded = game_state_from_dict(raw)
        self.assertEqual(loaded.save_format, SAVE_FORMAT)
        self.assertEqual({item.id for item in loaded.items}, item_ids)
        self.assertIn("preserved consequence", loaded.history)
        self.assertEqual(set(loaded.regions), {"hearthford", "greywash", "greenwold", "whitecairn"})


if __name__ == "__main__":
    unittest.main()
