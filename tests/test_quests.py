from __future__ import annotations

import copy
import tempfile
import unittest
from pathlib import Path

from jomon.actions import (
    _add_goods,
    apply_damage,
    decide_objective,
    interact,
    resolve_cross_region_choice,
    resolve_regional_quest_choice,
    use_contact_service,
)
from jomon.quests import (
    ADDITIONAL_ARCS,
    QUESTS,
    mark_elevated_lead,
    maybe_unlock_arc,
    quest_reachability_audit,
    record_container_opened,
)
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

    def test_opening_decision_assigns_a_finite_actor_to_a_material_duty(self):
        for region_id in QUESTS:
            state = create_world(f"{region_id} authored duty")
            at_primary(state, region_id)
            before_ids = {threat.id for threat in state.threats}

            decide_objective(state, "accept")

            guard_id = state.region.changes["quest_guard_id"]
            guard = next(threat for threat in state.threats if threat.id == guard_id)
            self.assertIn(guard.id, before_ids)
            self.assertFalse(guard.elite)
            self.assertEqual(guard.home_position, state.region.landmarks["objective"])
            self.assertIn(QUESTS[region_id]["title"], guard.goal_reason)

    def test_permanent_defeat_during_a_quest_preserves_progress_and_succession(self):
        state = create_world("quest succession")
        at_primary(state, "greywash")
        decide_objective(state, "accept")
        quest_before = copy.deepcopy(state.questlines["greywash"])
        dead = state.courier
        dead.health = 1
        dead.injury = "bruised ribs"

        result = apply_damage(state, 3, "The tide chain")

        self.assertFalse(dead.alive)
        self.assertNotEqual(state.active_courier_id, dead.id)
        self.assertEqual(state.questlines["greywash"], quest_before)
        self.assertEqual(state.objective_status, "failed")
        self.assertIn("succeeds", result)

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

    def test_quest_audit_reports_no_invalid_path(self):
        report = quest_reachability_audit(4)
        self.assertEqual(report["regions_checked"], 32)
        self.assertEqual(report["unreachable_or_invalid"], [])
        self.assertEqual(report["regional_questlines"], 20)
        self.assertEqual(report["regional_endings"], 40)
        self.assertEqual(report["cross_region_arcs"], 5)
        self.assertEqual(report["cross_region_endings"], 11)


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

    def _additional_unlocked(self, arc_id: str):
        state = create_world(f"additional arc {arc_id}")
        for region_id in ADDITIONAL_ARCS[arc_id]["requires"]:
            at_primary(state, region_id)
            state.questlines[region_id].status = "completed"
            if ADDITIONAL_ARCS[arc_id].get("requires_aftermath"):
                state.aftermath_quests[region_id].status = "completed"
        maybe_unlock_arc(state)
        self.assertEqual(state.cross_region_arcs[arc_id].status, "available")
        at_primary(state, ADDITIONAL_ARCS[arc_id]["start"])
        return state

    def test_banks_arc_has_environmental_and_armed_paths_with_distinct_endings(self):
        outcomes = []
        for opening, dun, marl, ending in (("p", "r", "s", "o"), ("f", "g", "k", "b")):
            state = self._additional_unlocked("banks")
            self.assertTrue(resolve_cross_region_choice(state, opening).time_advanced)
            record = next(item for item in state.items if item.kind == "consumable:bound bank roll")
            self.assertEqual((record.owner_id, record.location), (state.active_courier_id, "pack"))
            at_primary(state, "dunmire")
            self.assertTrue(resolve_cross_region_choice(state, dun).time_advanced)
            if dun == "g":
                self.assertTrue(any("Banks That Hold" in actor.duty for actor in state.threats))
            else:
                self.assertTrue(state.region.changes["arc:banks:environmental"])
            at_primary(state, "marlbank")
            self.assertTrue(resolve_cross_region_choice(state, marl).time_advanced)
            at_primary(state, "hearthford")
            self.assertTrue(resolve_cross_region_choice(state, ending).time_advanced)
            self.assertEqual(state.cross_region_arcs["banks"].status, "completed")
            self.assertEqual(record.location, "destroyed")
            outcomes.append(state.cross_region_arcs["banks"].consequence)
        self.assertNotEqual(*outcomes)

    def test_soundings_arc_changes_real_routes_and_survives_mid_arc_save(self):
        state = self._additional_unlocked("soundings")
        self.assertTrue(resolve_cross_region_choice(state, "l").time_advanced)
        at_primary(state, "frostmere")
        self.assertTrue(resolve_cross_region_choice(state, "i").time_advanced)
        state = game_state_from_dict(state.to_dict())
        self.assertEqual(state.cross_region_arcs["soundings"].stage, 2)
        at_primary(state, "rillscar")
        self.assertTrue(resolve_cross_region_choice(state, "w").time_advanced)
        before = [(edge.id, edge.weather_exposure, tuple(edge.closed_seasons)) for edge in state.route_edges]
        at_primary(state, "greywash")
        self.assertTrue(resolve_cross_region_choice(state, "s").time_advanced)
        after = [(edge.id, edge.weather_exposure, tuple(edge.closed_seasons)) for edge in state.route_edges]
        self.assertNotEqual(before, after)
        self.assertEqual(state.cross_region_arcs["soundings"].status, "completed")
        self.assertEqual(state.vessel_changes["arc:soundings:outcome"], "s")

    def test_lost_arc_record_can_be_replaced_without_resetting_progress(self):
        state = self._additional_unlocked("soundings")
        resolve_cross_region_choice(state, "d")
        record = next(item for item in state.items if item.kind == "consumable:sounding chain account")
        record.location, record.owner_id = "lost", None
        state.cross_region_arcs["soundings"].stage = 3
        state.cross_region_arcs["soundings"].status = "active"
        state.trade_credit = 2
        at_primary(state, "greywash")
        result = resolve_cross_region_choice(state, "r")
        self.assertTrue(result.time_advanced)
        self.assertEqual(state.trade_credit, 5)
        self.assertEqual(state.cross_region_arcs["soundings"].status, "completed")

    def test_new_arcs_unlock_only_from_three_completed_aftermaths(self):
        state = create_world("aftermath arc gates")
        for arc_id in ("repairs", "refuges"):
            for region_id in ADDITIONAL_ARCS[arc_id]["requires"]:
                at_primary(state, region_id)
                state.questlines[region_id].status = "completed"
            maybe_unlock_arc(state)
            self.assertEqual(state.cross_region_arcs[arc_id].status, "locked")
            for region_id in ADDITIONAL_ARCS[arc_id]["requires"]:
                state.aftermath_quests[region_id].status = "completed"
            maybe_unlock_arc(state)
            self.assertEqual(state.cross_region_arcs[arc_id].status, "available")

    def test_common_repairs_arc_consumes_three_aftermaths_and_changes_material_routes(self):
        state = self._additional_unlocked("repairs")
        self.assertTrue(resolve_cross_region_choice(state, "p").time_advanced)
        record = next(
            item for item in state.items
            if item.kind == "consumable:scar repair folio"
        )
        at_primary(state, "hearthford")
        self.assertTrue(resolve_cross_region_choice(state, "r").time_advanced)
        at_primary(state, "rillscar")
        self.assertTrue(resolve_cross_region_choice(state, "s").time_advanced)
        before = [edge.cargo_risk for edge in state.route_edges]
        at_primary(state, "greenwold")
        self.assertTrue(resolve_cross_region_choice(state, "m").time_advanced)

        self.assertEqual(state.cross_region_arcs["repairs"].status, "completed")
        self.assertEqual(record.location, "destroyed")
        self.assertNotEqual(before, [edge.cargo_risk for edge in state.route_edges])
        self.assertTrue(all(
            state.regions[region].changes["common_aftermath_repairs"]
            for region in ADDITIONAL_ARCS["repairs"]["requires"]
        ))

    def test_low_water_refuges_arc_has_armed_surety_and_public_outcomes(self):
        outcomes = []
        for opening, dunmire, frostmere, ending in (
            ("l", "i", "w", "p"), ("c", "g", "n", "c")
        ):
            state = self._additional_unlocked("refuges")
            self.assertTrue(resolve_cross_region_choice(state, opening).time_advanced)
            at_primary(state, "dunmire")
            self.assertTrue(resolve_cross_region_choice(state, dunmire).time_advanced)
            at_primary(state, "frostmere")
            self.assertTrue(resolve_cross_region_choice(state, frostmere).time_advanced)
            at_primary(state, "greywash")
            self.assertTrue(resolve_cross_region_choice(state, ending).time_advanced)
            self.assertEqual(state.cross_region_arcs["refuges"].status, "completed")
            outcomes.append(state.cross_region_arcs["refuges"].consequence)
        self.assertNotEqual(*outcomes)

    def test_arc_copy_on_recoverable_ground_cannot_be_bought_away(self):
        state = self._additional_unlocked("repairs")
        resolve_cross_region_choice(state, "p")
        record = next(
            item for item in state.items
            if item.kind == "consumable:scar repair folio"
        )
        record.location, record.owner_id = "ground", None
        record.region_id, record.ground_position = "greenwold", state.position
        state.cross_region_arcs["repairs"].stage = 3
        state.trade_credit = 9
        at_primary(state, "greenwold")
        result = resolve_cross_region_choice(state, "m")
        self.assertFalse(result.time_advanced)
        self.assertEqual(state.trade_credit, 9)


if __name__ == "__main__":
    unittest.main()
