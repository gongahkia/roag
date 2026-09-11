from pathlib import Path
import tempfile
import unittest

from jomon.frontiers import ensure_frontier
from jomon.household_stories import (
    STORIES, eligibility, outcome_regions, resolve, station_choices,
    story_choices, story_lines, validate_stories,
)
from jomon.save import load_game, save_game
from jomon.situations import BY_REGION_BAND
from jomon.state import CommodityStack, create_world


class HouseholdStoryTests(unittest.TestCase):
    def late_world(self):
        state = create_world("late-household-stories")
        for region_id in ("dunmire", "rillscar", "marlbank", "frostmere"):
            ensure_frontier(state, region_id)
        state.returned_expeditions = 4
        state.travel_count = 4
        state.vessel_integrity = 7
        state.vessel_cargo["timber"] = CommodityStack(1, "dry")
        for region_id in ("hearthford", "greywash", "greenwold", "whitecairn"):
            row = BY_REGION_BAND[region_id, "steady"]
            state.regions[region_id].changes[f"micro-site:resolved:{row.id}"] = True
        return state

    def test_catalogue_is_two_developments_and_one_capstone(self):
        validate_stories()
        self.assertEqual([row.id for row in STORIES], ["empty-watch", "repair-share", "eight-waters"])

    def test_developments_are_two_stage_time_bearing_and_defer_is_free(self):
        state = self.late_world()
        for story_id, answer in (("empty-watch", "w"), ("repair-share", "p")):
            self.assertTrue(eligibility(state, story_id)[0])
            self.assertFalse(resolve(state, story_id, "d")[0])
            opened = resolve(state, story_id, "o")
            self.assertEqual(opened[2], 1)
            completed = resolve(state, story_id, answer)
            self.assertTrue(completed[0], completed[1])
            self.assertEqual(completed[2], 1)
        self.assertEqual(state.vessel_integrity, 9)

    def test_empty_watch_preserves_death_and_supports_succession(self):
        state = self.late_world()
        dead = state.household[0]
        dead.alive = False
        state.active_courier_id = state.household[1].id
        resolve(state, "empty-watch", "o")
        resolve(state, "empty-watch", "w")
        self.assertFalse(dead.alive)
        self.assertIn(dead.name, state.vessel_changes["household-story:empty-watch:outcome"])

    def test_capstone_consumes_four_regions_and_has_three_persistent_answers(self):
        for answer in "chr":
            state = self.late_world()
            for story_id, branch in (("empty-watch", "p"), ("repair-share", "w")):
                resolve(state, story_id, "o")
                resolve(state, story_id, branch)
            self.assertGreaterEqual(len(outcome_regions(state)), 4)
            self.assertTrue(eligibility(state, "eight-waters")[0])
            resolve(state, "eight-waters", "o")
            changed, message, steps = resolve(state, "eight-waters", answer)
            self.assertTrue(changed, message)
            self.assertEqual(steps, 1)
            self.assertIn("campaign:all-region-capstone", state.vessel_changes)

    def test_story_state_round_trips_in_format_seven(self):
        state = self.late_world()
        resolve(state, "empty-watch", "o")
        resolve(state, "empty-watch", "p")
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "stories.json"
            save_game(state, path)
            loaded = load_game(path)
        self.assertEqual(loaded.vessel_changes["household-story:empty-watch:status"], "completed")
        self.assertIn("Outcome branch", " ".join(story_lines(loaded, "empty-watch")))

    def test_station_discloses_requirements_before_available(self):
        state = create_world("early-household-stories")
        rows = station_choices(state)
        self.assertEqual(len(rows), 3)
        self.assertFalse(any(row[3] for row in rows))


if __name__ == "__main__":
    unittest.main()
