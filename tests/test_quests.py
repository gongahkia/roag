from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from jomon.actions import (
    _add_goods,
    decide_objective,
    interact,
    resolve_cross_region_choice,
    resolve_regional_quest_choice,
    use_contact_service,
)
from jomon.quests import QUESTS, mark_elevated_lead, record_container_opened
from jomon.regions import activate_region, region_reachable
from jomon.save import load_game, save_game
from jomon.state import Position, create_world, game_state_from_dict
from jomon.world import remembered


def at_primary(state, region_id: str) -> None:
    activate_region(state, region_id)
    state.location = "region"
    schedule = state.actor_schedules[state.contact.id]
    state.position = schedule.position


def complete_material_stage(state, region_id: str, decision: str = "accept") -> None:
    at_primary(state, region_id)
    result = decide_objective(state, decision)
    if decision == "refuse":
        return
    if decision == "alter":
        state.region.changes["objective_altered"] = True
    else:
        commodity = state.region.objective_commodity
        if not _add_goods(state, commodity, state.objective_required, "sound"):
            raise AssertionError("test objective load did not fit")
    state.position = state.actor_schedules[state.contact.id].position
    completed = interact(state)
    if not completed.time_advanced:
        raise AssertionError((region_id, result.message, completed.message))


class RegionalQuestlineTests(unittest.TestCase):
    def test_each_quest_has_opening_optional_stage_and_two_persistent_endings(self):
        choices = {
            "hearthford": ("l", "r"),
            "greywash": ("s", "e"),
            "greenwold": ("m", "c"),
            "whitecairn": ("w", "x"),
        }
        consequences: dict[str, set[str]] = {}
        for region_id, endings in choices.items():
            consequences[region_id] = set()
            for ending in endings:
                state = create_world(f"{region_id} ending {ending}")
                complete_material_stage(state, region_id)
                quest = state.questlines[region_id]
                self.assertEqual((quest.stage, quest.status), (2, "resolution"))
                self.assertTrue(quest.cache_marked)
                self.assertIn(QUESTS[region_id]["cache"], state.treasure_marks[region_id])
                if region_id in {"greywash", "whitecairn"} and ending in {"e", "x"}:
                    record_container_opened(state, QUESTS[region_id]["cache"])
                result = resolve_regional_quest_choice(state, ending)
                self.assertTrue(result.time_advanced, (region_id, ending, result.message))
                self.assertEqual((quest.stage, quest.status), (3, "completed"))
                self.assertTrue(quest.consequence)
                consequences[region_id].add(quest.consequence)
                self.assertTrue(any(item.provenance.startswith(QUESTS[region_id]["title"]) for item in state.items))
            self.assertEqual(len(consequences[region_id]), 2)

    def test_refusal_is_a_real_first_decision_not_a_dead_end(self):
        state = create_world("refusal continues")
        complete_material_stage(state, "greenwold", "refuse")
        quest = state.questlines["greenwold"]
        self.assertEqual((quest.stage, quest.status, quest.branch), (2, "resolution", "refuse"))
        result = resolve_regional_quest_choice(state, "m")
        self.assertTrue(result.time_advanced)
        self.assertEqual(quest.status, "completed")

    def test_secondary_contact_services_are_production_actions(self):
        state = create_world("secondary worker services")
        at_primary(state, "whitecairn")
        before = state.world_time
        clue = use_contact_service(state, "c")
        self.assertTrue(clue.time_advanced)
        self.assertEqual(state.world_time, before + 1)
        self.assertTrue(state.treasure_marks["whitecairn"])
        training = use_contact_service(state, "t")
        self.assertTrue(training.time_advanced)
        self.assertIn("bell interval", state.courier.learned_techniques)
        state.courier.injuries["feet"] = "cut foot"
        state.courier.injury = "cut foot"
        state.support = "field care"
        treated = use_contact_service(state, "h")
        self.assertTrue(treated.time_advanced)
        self.assertNotIn("feet", state.courier.injuries)

    def test_contact_height_and_environment_create_independent_treasure_clues(self):
        state = create_world("three clue paths")
        complete_material_stage(state, "greywash", "refuse")
        self.assertEqual(len(state.treasure_marks["greywash"]), 1)
        use_contact_service(state, "c")
        self.assertEqual(len(state.treasure_marks["greywash"]), 2)
        state.position = Position(60, 14, 2)
        self.assertTrue(mark_elevated_lead(state))
        self.assertEqual(len(state.treasure_marks["greywash"]), 3)
        marked = next(
            container for container in state.region.containers
            if container.id in state.treasure_marks["greywash"]
        )
        self.assertTrue(remembered(state, marked.position))

    def test_quest_progress_and_marks_save_mid_stage(self):
        state = create_world("mid quest persistence")
        at_primary(state, "greenwold")
        decide_objective(state, "alter")
        use_contact_service(state, "c")
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "quest.json"
            save_game(state, path)
            loaded = load_game(path)
        self.assertEqual(loaded.questlines, state.questlines)
        self.assertEqual(loaded.treasure_marks, state.treasure_marks)
        self.assertEqual(loaded.objective_status, "altered")

    def test_all_required_quest_positions_are_reachable_for_representative_seeds(self):
        for index in range(8):
            state = create_world(f"quest reachability {index}")
            for region_id, region in state.regions.items():
                reachable = region_reachable(region)
                cache = next(
                    container for container in region.containers
                    if container.id == QUESTS[region_id]["cache"]
                )
                with self.subTest(seed=index, region=region_id):
                    self.assertIn(region.landmarks["contact"], reachable)
                    self.assertIn(region.landmarks["second_contact"], reachable)
                    self.assertIn(cache.position, reachable)


class CrossRegionArcTests(unittest.TestCase):
    def _unlocked(self):
        state = create_world("cross region working marks")
        for region_id, ending in (("hearthford", "l"), ("greenwold", "m")):
            complete_material_stage(state, region_id)
            resolve_regional_quest_choice(state, ending)
        self.assertEqual(state.cross_region_arc.status, "available")
        return state

    def test_five_chapter_arc_requires_three_regions_and_changes_routes(self):
        state = self._unlocked()
        # Start with the completed Greenwold relationship, then carry the
        # physical account through Greywash, Greenwold, Whitecairn, Hearthford.
        self.assertTrue(resolve_cross_region_choice(state, "o").time_advanced)
        for region_id, choice in (
            ("greywash", "s"), ("greenwold", "m"),
            ("whitecairn", "b"), ("hearthford", "c"),
        ):
            at_primary(state, region_id)
            self.assertTrue(resolve_cross_region_choice(state, choice).time_advanced)
        self.assertEqual(state.cross_region_arc.status, "completed")
        self.assertEqual(state.vessel_changes["route_reputation"], "open compact")
        self.assertTrue(all(edge.cargo_risk >= 0 for edge in state.route_edges))
        self.assertIn("Four Working Marks ended", state.history[-1])

    def test_arc_has_distinct_household_and_local_endings(self):
        results = []
        for choice in ("h", "l"):
            state = self._unlocked()
            state.cross_region_arc.status = "active"
            state.cross_region_arc.stage = 4
            at_primary(state, "hearthford")
            before_credit = state.trade_credit
            self.assertTrue(resolve_cross_region_choice(state, choice).time_advanced)
            results.append((state.cross_region_arc.consequence, state.trade_credit - before_credit, state.vessel_changes["route_reputation"]))
        self.assertNotEqual(results[0], results[1])

    def test_unlocked_arc_remains_available_after_save(self):
        state = self._unlocked()

        loaded = game_state_from_dict(state.to_dict())

        self.assertEqual(loaded.cross_region_arc.status, "available")


if __name__ == "__main__":
    unittest.main()
