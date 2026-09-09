import copy
import unittest

from jomon.actions import _return_after_defeat, _threat_action, choose_courier, negotiate
from jomon.inventory import auto_place, create_item, item_spec, sync_legacy_load
from jomon.materials import advance_materials, fields, key
from jomon.regions import activate_region, region_reachable
from jomon.state import Position, StateError, create_world, game_state_from_dict
from jomon.terminal import InputEvent, OverlayView, _handle_overlay_view, _draw_dialogue_overlay, dialogue_choices
from jomon.worklines import (
    WORKLINES, apply_local_work, at_witness, carried_evidence, field_site,
    lines, options, resolve, survey_site,
)
from jomon.world import sight_radius
from test_information_panels import PanelSink


class UndertakingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("second working accounts")

    def setUp(self):
        self.state = copy.deepcopy(self.base)
        self.state.world_time, self.state.trade_credit = 8, 12

    def witness(self):
        s = self.state
        c = s.contacts[s.active_region_id][1]
        s.location = "region"
        s.position = s.actor_schedules[c.id].position
        self.assertTrue(at_witness(s))

    def supply(self, kind):
        s = self.state
        item = create_item(s, kind, "workline exercise")
        self.assertTrue(auto_place(s, item.id, "pack", owner_id=s.active_courier_id))
        sync_legacy_load(s)
        return item

    def open(self, region, branch, *, clear_actors=True):
        activate_region(self.state, region)
        self.state.location = "region"
        if clear_actors:
            self.state.threats.clear()
        self.witness()
        self.assertTrue(resolve(self.state, branch).time_advanced)

    def survey(self):
        self.state.position = survey_site(self.state)
        result = resolve(self.state, "e")
        self.assertTrue(result.time_advanced, result.message)
        self.assertIsNotNone(carried_evidence(self.state))

    def field(self, region, branch):
        if region == "hearthford":
            self.supply("commodity:timber")
        elif region == "greenwold" and branch == "w":
            self.supply("consumable:sealed brine pot")
        elif region == "whitecairn" and branch == "b":
            self.supply("commodity:ironwork")
        self.state.position = field_site(self.state)
        result = resolve(self.state, "f")
        self.assertTrue(result.time_advanced, result.message)
        self.assertEqual(self.state.worklines[region].stage, 3)

    def test_every_approach_and_settlement_has_physical_work_and_persistent_outcome(self):
        expected = {"hearthford": ("raised_watch_approach", "loft_dry_stores"), "greywash": ("public_dune_light", "screened_wreck_signal"),
                    "greenwold": ("coppice_firebreak", "root_reserve_damped"), "whitecairn": ("counterweight_clamped", "counterweight_released")}
        for region, definition in WORKLINES.items():
            for branch_index, branch in enumerate((definition[4], definition[6])):
                for ending in ("p", "j"):
                    with self.subTest(region=region, branch=branch, ending=ending):
                        self.setUp()
                        self.open(region, branch)
                        self.survey()
                        self.state = game_state_from_dict(self.state.to_dict())
                        self.field(region, branch)
                        self.assertTrue(self.state.region.changes[expected[region][branch_index]])
                        self.witness()
                        result = resolve(self.state, ending)
                        self.assertTrue(result.time_advanced, result.message)
                        q = self.state.worklines[region]
                        self.assertEqual((q.stage, q.status), (4, "completed"))
                        self.assertIn("commons" if ending == "p" else "surety", q.consequence)
                        self.assertTrue(self.state.contacts[region][1].memories)
                        self.assertEqual(game_state_from_dict(self.state.to_dict()).to_dict(), self.state.to_dict())
                        count = len(self.state.items)
                        self.assertFalse(resolve(self.state, ending).time_advanced)
                        self.assertEqual(len(self.state.items), count)

    def test_evidence_cannot_duplicate_but_lost_testimony_has_paid_bounded_copies(self):
        self.open("hearthford", "h")
        self.survey()
        item = carried_evidence(self.state)
        self.assertEqual((item_spec(item.kind).width, item_spec(item.kind).height), (1, 2))
        before = len(self.state.items)
        self.assertFalse(resolve(self.state, "e").time_advanced)
        self.assertEqual(len(self.state.items), before)
        item.location, item.owner_id = "lost", None
        credit = self.state.trade_credit
        self.assertTrue(resolve(self.state, "e").time_advanced)
        self.assertEqual(self.state.trade_credit, credit - 1)
        self.assertIsNot(carried_evidence(self.state), item)
        for _ in range(2):
            current = carried_evidence(self.state)
            current.location, current.owner_id = "lost", None
            self.assertTrue(resolve(self.state, "e").time_advanced)
        current = carried_evidence(self.state)
        current.location, current.owner_id = "lost", None
        self.assertFalse(resolve(self.state, "e").time_advanced)
        self.witness()
        self.assertTrue(resolve(self.state, "a").time_advanced)
        self.assertEqual(self.state.worklines["hearthford"].status, "refused")

    def test_evidence_on_a_dead_courier_is_recoverable_not_reissued(self):
        self.open("hearthford", "h")
        self.survey()
        item = carried_evidence(self.state)
        old = self.state.active_courier_id
        point = self.state.position
        _return_after_defeat(self.state, "declared worksite crisis", permanent=True)
        self.assertEqual((item.location, item.ground_position), ("ground", point))
        successor = next(p for p in self.state.household if p.alive and p.id != old)
        choose_courier(self.state, successor.id)
        self.state.location, self.state.position = "region", point
        self.assertFalse(resolve(self.state, "e").time_advanced)
        self.assertTrue(auto_place(self.state, item.id, "pack", owner_id=successor.id))
        self.assertIs(carried_evidence(self.state), item)
        self.field("hearthford", "h")

    def test_survey_is_optional_to_open_but_marks_the_existing_treasure(self):
        self.open("greenwold", "c")
        self.assertIn("greenwold-root", self.state.treasure_marks["greenwold"])
        chest = next(c for c in self.state.region.containers if c.id == "greenwold-root")
        self.assertFalse(chest.opened)
        self.survey()
        self.assertFalse(self.state.worklines["greenwold"].optional_done)

    def test_physical_guard_is_redirected_not_spawned_and_accepts_specific_evidence(self):
        self.open("greywash", "l", clear_actors=False)
        state = self.state
        guard_id = state.region.changes["undertaking_guard"]
        guard = next(a for a in state.threats if a.id == guard_id)
        self.assertEqual(guard.home_position, field_site(state))
        self.assertEqual(len(guard.patrol), 2)
        # Keep the existing guard, but isolate the social check from other claims.
        state.threats[:] = [guard]
        self.survey()
        state.position = Position(40, 25)
        guard.position, guard.status, guard.patrol = Position(42, 25), "engaged", []
        state.objective_evidence, state.gear, state.support, state.carried_passives = [], None, None, {}
        state.courier.technique = "sure footing"
        state.region.tile_changes["41,25,0"] = "."
        self.assertTrue(negotiate(state).time_advanced)
        self.assertEqual(guard.status, "negotiated")

    def test_light_changes_sight_and_released_weight_warns_before_damage(self):
        self.open("greywash", "l")
        self.survey()
        self.state.position = field_site(self.state)
        before = sight_radius(self.state)
        self.field("greywash", "l")
        self.assertEqual(sight_radius(self.state), before + 2)
        self.setUp()
        self.open("whitecairn", "r")
        self.survey()
        self.field("whitecairn", "r")
        site = field_site(self.state)
        cell = fields(self.state)[key(Position(site.x - 1, site.y, site.z))]
        self.assertGreater(cell.collapse_due, self.state.world_time)
        self.state.world_time = cell.collapse_due
        advance_materials(self.state)
        self.assertEqual(cell.collapse_due, 0)
        self.assertTrue(any("collapsed" in text for text in self.state.history))

    def test_saturated_field_does_not_consume_material_or_advance_stage(self):
        from jomon.state import MaterialCell
        self.open("hearthford", "h")
        self.survey()
        timber = self.supply("commodity:timber")
        self.state.position = field_site(self.state)
        for i in range(512):
            self.state.region.materials[f"{i % 96},{i // 96},0"] = MaterialCell()
        self.assertFalse(resolve(self.state, "f").time_advanced)
        self.assertEqual(timber.location, "pack")
        self.assertEqual(self.state.worklines["hearthford"].stage, 2)

    def test_zero_time_inspection_and_physical_dialogue_entry_at_both_sizes(self):
        self.witness()
        before = self.state.to_dict()
        contact = self.state.contacts["hearthford"][1]
        view = OverlayView(f"contact-service:{contact.id}")
        closed, _ = _handle_overlay_view(self.state, view, InputEvent("key", key=ord("w")))
        self.assertFalse(closed)
        self.assertEqual(view.kind, "workline")
        for height, width in ((24, 80), (32, 100)):
            sink = PanelSink(height, width)
            _draw_dialogue_overlay(sink, self.state, view)
            self.assertEqual(len(dialogue_choices(self.state, "workline")), 2)
            self.assertEqual(len(view.option_rows), 2)
        _handle_overlay_view(self.state, view, InputEvent("key", key=27))
        self.assertEqual(self.state.to_dict(), before)

    def test_current_task_and_all_choice_keys_are_visible_at_minimum_size(self):
        for region, row in WORKLINES.items():
            for branch in (row[4], row[6]):
                self.setUp()
                self.open(region, branch)
                for stage in (1, 2, 3):
                    if stage == 2:
                        self.survey()
                    elif stage == 3:
                        self.field(region, branch)
                    view, sink = OverlayView("workline"), PanelSink()
                    _draw_dialogue_overlay(sink, self.state, view)
                    rendered = " ".join(sink.writes)
                    self.assertEqual(len(view.option_rows), len(options(self.state)))
                    for option in options(self.state):
                        self.assertIn(f"[{option[0].upper()}]", rendered)
                    self.assertIn({1: "SURVEY:", 2: "FIELD:", 3: "Return to the named witness"}[stage], rendered)

    def test_auto_place_off_leaves_evidence_and_reward_physically_at_the_site(self):
        self.open("hearthford", "h")
        self.state.auto_place_enabled = False
        self.state.position = survey_site(self.state)
        self.assertTrue(resolve(self.state, "e").time_advanced)
        item = next(i for i in self.state.items if i.kind == "evidence:flood-height lath")
        self.assertEqual((item.location, item.ground_position), ("ground", self.state.position))
        self.assertIsNone(carried_evidence(self.state))
        self.assertTrue(auto_place(self.state, item.id, "pack", owner_id=self.state.active_courier_id))
        self.field("hearthford", "h")
        self.witness()
        self.assertTrue(resolve(self.state, "p").time_advanced)
        reward = self.state.items[-1]
        self.assertEqual((reward.kind, reward.location, reward.ground_position), ("passive:roof nail", "ground", self.state.position))

    def test_public_light_consumes_stock_at_work_boundaries_and_darkens_in_shortage(self):
        from jomon.calendar import ACTIONS_PER_DAY
        from jomon.regional_history import advance_production, account_for
        from jomon.worklines import beacon_active
        self.open("greywash", "l")
        self.survey()
        self.field("greywash", "l")
        self.assertTrue(beacon_active(self.state))
        self.state.world_time = self.state.region.changes["public_dune_light_until"]
        account_for(self.state).last_day = self.state.world_time // ACTIONS_PER_DAY - 1
        self.state.market["charcoal"].stock = 1
        advance_production(self.state)
        self.assertTrue(beacon_active(self.state))
        self.assertEqual(self.state.market["charcoal"].stock, 0)
        self.state.world_time += ACTIONS_PER_DAY
        advance_production(self.state)
        self.assertFalse(beacon_active(self.state))
        self.assertIn("dark", self.state.region.changes["beacon_account"])
        self.assertEqual(game_state_from_dict(self.state.to_dict()).to_dict(), self.state.to_dict())

    def test_v6_migration_adds_empty_undertakings_and_preserves_existing_state(self):
        data = self.state.to_dict()
        data["save_format"] = 6
        data.pop("worklines")
        first, second = game_state_from_dict(data), game_state_from_dict(data)
        self.assertEqual(first.to_dict(), second.to_dict())
        self.assertEqual(first.to_dict()["items"], data["items"])
        self.assertEqual(first.to_dict()["regions"], data["regions"])
        bad = first.to_dict()
        bad["worklines"]["unknown-region"] = bad["worklines"]["hearthford"]
        with self.assertRaises(StateError):
            game_state_from_dict(bad)
        bad = first.to_dict()
        bad["worklines"]["hearthford"]["stage"] = 4
        with self.assertRaises(StateError):
            game_state_from_dict(bad)

    def test_survey_and_field_sites_are_reachable_across_seeds(self):
        for seed in ("second line wet", "second line cold", "second line wind"):
            state = create_world(seed)
            for region, row in WORKLINES.items():
                reachable = region_reachable(state.regions[region])
                self.assertIn(survey_site(state, region), reachable)
                for branch in (row[4], row[6]):
                    state.worklines[region].branch = branch
                    self.assertIn(field_site(state, region), reachable)


if __name__ == "__main__":
    unittest.main()
