import copy
import unittest

from jomon.actions import _activate, _advance_world, _threat_action, attack, depart
from jomon.encounters import frontier_population, threat_from_archetype
from jomon.frontier_elites import (
    AFTERMATH_ELITES, ELITE_DEFINITIONS, definition, guard_interception,
    install_aftermath_elite, install_elite, record_outcomes,
    revisit_claimants, settle_claimant,
)
from jomon.frontiers import FRONTIERS, build_frontier
from jomon.inventory import create_item
from jomon.materials import advance_materials
from jomon.regions import activate_region, region_reachable
from jomon.state import MaterialCell, Position, StateError, Threat, VerticalLink, create_world, game_state_from_dict
from jomon.terminal import OverlayView, _draw_dialogue_overlay, _handle_overlay, observed_life_lines, visible_danger_marks
from test_information_panels import PanelSink


class FrontierEliteTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("finite elite duties")

    def setUp(self):
        self.state = copy.deepcopy(self.base)
        self.state.location, self.state.position = "region", Position(30, 24)
        self.state.weather, self.state.support = "clear", None
        self.state.threats.clear()
        self.state.region.materials.clear()
        self.state.region.changes.clear()
        self.state.smoke.clear()
        for x in range(20, 46):
            for y in range(18, 32):
                self.state.region.tile_changes[f"{x},{y},0"] = "."

    def actor(self, identity):
        actor = threat_from_archetype(identity, Position(35, 24), encounter_id="frontier-elite", group="working claim")
        actor.status = "engaged"
        actor.morale = 10
        self.state.threats.append(actor)
        self.state.region.materials["35,24,0"] = MaterialCell(material="timber", support=2)
        return actor

    def test_every_mode_warns_before_mutating_a_target(self):
        for identity in ELITE_DEFINITIONS:
            with self.subTest(identity=identity):
                self.setUp()
                actor = self.actor(identity)
                before = copy.deepcopy(self.state.region.materials)
                health = self.state.courier.health
                message = _threat_action(self.state, actor, False)
                self.assertIn("prepares", message)
                self.assertIsNotNone(actor.marked_position)
                self.assertEqual(self.state.region.materials, before)
                self.assertEqual(self.state.courier.health, health)
                self.assertEqual(actor.supplies, 3)

    def test_first_notice_names_the_actual_mechanism_not_a_fictitious_bolt(self):
        for identity, data in ELITE_DEFINITIONS.items():
            actor = self.actor(identity)
            message = _activate(actor)
            self.assertIn(data["mode"], message)
            self.assertIn("warning", message)
            self.assertNotIn("bolt follows", message)

    def test_peaceful_resolution_releases_the_same_carried_property(self):
        actor = self.actor("fen-marshal")
        item = create_item(self.state, "passive:load ledger", "stolen claim", location="enemy")
        item.owner_id = actor.id
        actor.carrying_item_id, actor.status = item.id, "negotiated"
        record_outcomes(self.state)
        self.assertIsNone(actor.carrying_item_id)
        self.assertEqual((item.location, item.owner_id, item.ground_position), ("ground", None, actor.position))

    def test_water_release_affects_load_and_follows_material_openings(self):
        state = self.state
        actor = self.actor("fen-marshal")
        state.region.tile_changes["30,24,0"] = "O"
        state.region.tile_changes["30,24,-1"] = "."
        _threat_action(state, actor, False)
        _threat_action(state, actor, False)
        self.assertEqual([state.region.materials[f"{x},24,0"].water for x in (29, 30, 31)], [3, 3, 3])
        advance_materials(state)
        self.assertEqual(state.region.materials["30,24,-1"].water, 1)
        self.assertEqual(actor.supplies, 2)

    def test_cordmaster_recovery_uses_an_aligned_stair_before_next_cut(self):
        state = self.state
        actor = self.actor("gorge-cordmaster")
        low, high = Position(34, 24), Position(34, 24, 1)
        state.region.tile_changes["34,24,0"] = ">"
        state.region.tile_changes["34,24,1"] = "<"
        state.region.vertical_links.append(VerticalLink(low, high, "working stair"))
        _threat_action(state, actor, False)
        _threat_action(state, actor, False)
        self.assertEqual(actor.reload_turns, 2)
        _threat_action(state, actor, False)
        _threat_action(state, actor, False)
        self.assertEqual(actor.position, high)

    def test_smouldering_rack_creates_heat_smoke_and_warned_support(self):
        actor = self.actor("fen-stack")
        _threat_action(self.state, actor, False)
        _threat_action(self.state, actor, False)
        cell = self.state.region.materials["35,24,0"]
        self.assertEqual((cell.fire, cell.smoke, cell.support), (1, 4, 1))
        actor.reload_turns = 0
        _threat_action(self.state, actor, False)
        _threat_action(self.state, actor, False)
        self.assertEqual(cell.collapse_due, self.state.world_time + 2)

    def test_severing_warns_twice_and_brace_can_interrupt_the_drop(self):
        state = self.state
        actor = self.actor("gorge-cordmaster")
        for _ in range(2):
            _threat_action(state, actor, False)
        cell = state.region.materials["30,24,0"]
        self.assertEqual(cell.support, 1)
        self.assertFalse(cell.collapse_due)
        actor.reload_turns = 0
        _threat_action(state, actor, False)
        cell.support = 3
        _threat_action(state, actor, False)
        self.assertFalse(cell.collapse_due)
        actor.reload_turns = 0
        _threat_action(state, actor, False)
        _threat_action(state, actor, False)
        self.assertEqual(cell.collapse_due, state.world_time + 2)

    def test_convoy_interception_spends_a_real_escort_body_and_separation_breaks_it(self):
        state = self.state
        leader = self.actor("gorge-convoy")
        escort = Threat("escort", "rope escort", "reach", Position(36, 24), 2, 4, group=leader.group)
        state.threats.append(escort)
        damage, message = guard_interception(state, leader, 3)
        self.assertEqual((damage, escort.health, escort.status), (1, 0, "defeated"))
        self.assertIn("physically takes", message)
        self.assertEqual(guard_interception(state, leader, 3), (3, ""))
        escort.status, escort.health, escort.position = "watching", 4, Position(42, 24)
        self.assertEqual(guard_interception(state, leader, 3), (3, ""))

    def test_actual_attack_and_visible_escort_duty_use_the_convoy_body(self):
        state = self.state
        leader = self.actor("gorge-convoy")
        leader.position, leader.home_position = Position(31, 24), Position(31, 24)
        escort = Threat("escort", "rope escort", "reach", Position(32, 24), 5, 5, group=leader.group, duty="escort", status="engaged")
        state.threats.append(escort)
        state.weapon = "hand axe"
        before = leader.health
        attack(state, leader.id)
        self.assertEqual(leader.health, before - 1)
        self.assertEqual(escort.health, 3)
        self.assertEqual(escort.goal, "escort vulnerable ally")

    def test_firing_line_respects_poured_water_and_hits_shared_materials(self):
        state = self.state
        actor = self.actor("terrace-reeve")
        _threat_action(state, actor, False)
        state.region.materials["30,24,0"] = MaterialCell(water=1)
        message = _threat_action(state, actor, False)
        self.assertIn("2 dry", message)
        self.assertFalse(state.region.materials["30,24,0"].fire)
        self.assertTrue(state.region.materials["29,24,0"].fire)

    def test_shutters_change_cover_and_guard_or_reposition_avoids_the_sweep(self):
        for guarded, moved in ((True, False), (False, True), (False, False)):
            self.setUp()
            state = self.state
            actor = self.actor("terrace-shutters")
            _threat_action(state, actor, False)
            health = state.courier.health
            if moved:
                state.position = Position(30, 25)
            _threat_action(state, actor, guarded)
            self.assertEqual(state.region.tile_changes["30,24,0"], "%")
            self.assertEqual(state.courier.health == health, guarded or moved)

    def test_brine_thaws_a_real_sheet_without_conjuring_ice(self):
        actor = self.actor("estuary-pilot")
        cell = MaterialCell(water=1, ice=True)
        self.state.region.materials["30,24,0"] = cell
        _threat_action(self.state, actor, False)
        _threat_action(self.state, actor, False)
        self.assertEqual((cell.ice, cell.water, cell.fluid), (False, 3, "salt"))
        self.state.calendar_origin_day = 72
        advance_materials(self.state)
        self.assertFalse(cell.ice)
        self.assertTrue(all(not patch.ice for patch in self.state.region.materials.values() if patch.fluid == "salt"))

    def test_loaded_net_marks_then_pulls_and_guard_denies_it(self):
        for guarded in (True, False):
            self.setUp()
            state = self.state
            actor = self.actor("estuary-drum")
            _threat_action(state, actor, False)
            _threat_action(state, actor, guarded)
            self.assertEqual("net-drag" in state.terrain_statuses, not guarded)
            self.assertEqual(state.position, Position(30 if guarded else 31, 24))

    def test_backwash_and_firebreak_change_shared_material_lines(self):
        backwash = self.actor("hearth-lockhand")
        _threat_action(self.state, backwash, False)
        _threat_action(self.state, backwash, True)
        self.assertEqual(
            [self.state.region.materials[f"{x},24,0"].water for x in (29, 30, 31)],
            [2, 2, 2],
        )

        self.setUp()
        firebreak = self.actor("forest-ashstep")
        for x in (29, 30, 31):
            self.state.region.materials[f"{x},24,0"] = MaterialCell(
                material="timber", fire=1, fuel=4,
            )
        _threat_action(self.state, firebreak, False)
        _threat_action(self.state, firebreak, False)
        self.assertTrue(all(
            self.state.region.materials[f"{x},24,0"].fire == 0
            and self.state.region.materials[f"{x},24,0"].coating == "ash"
            for x in (29, 30, 31)
        ))

    def test_wreckward_takes_and_releases_the_exact_marked_object(self):
        actor = self.actor("coast-wreckward")
        item = create_item(self.state, "passive:wreck key", "exposed wreck account", location="ground")
        item.region_id, item.ground_position = self.state.spatial_id, Position(31, 24)
        _threat_action(self.state, actor, False)
        self.assertEqual(actor.marked_position, item.ground_position)
        _threat_action(self.state, actor, False)
        self.assertEqual((item.location, item.owner_id, actor.carrying_item_id), ("enemy", actor.id, item.id))
        actor.status = "negotiated"
        record_outcomes(self.state)
        self.assertEqual((item.location, item.owner_id, item.ground_position), ("ground", None, actor.position))

    def test_counterfall_is_passable_cover_and_guard_answers_impact(self):
        actor = self.actor("upland-bellrope")
        health = self.state.courier.health
        _threat_action(self.state, actor, False)
        _threat_action(self.state, actor, True)
        self.assertEqual(self.state.courier.health, health)
        self.assertEqual(self.state.region.tile_changes["30,24,0"], "%")

    def test_new_machines_have_four_distinct_bounded_material_rules(self):
        actor = self.actor("fen-pump-train")
        for x in (29, 30, 31):
            self.state.region.materials[f"{x},24,0"] = MaterialCell(water=2)
        _threat_action(self.state, actor, False)
        _threat_action(self.state, actor, False)
        self.assertTrue(all(self.state.region.materials[f"{x},24,0"].coating == "mud" for x in (29, 30, 31)))

        self.setUp()
        actor = self.actor("gorge-wedge-crane")
        _threat_action(self.state, actor, False)
        _threat_action(self.state, actor, False)
        self.assertEqual(self.state.region.tile_changes["30,24,0"], "%")

        self.setUp()
        actor = self.actor("terrace-slip-wheel")
        _threat_action(self.state, actor, False)
        _threat_action(self.state, actor, False)
        self.assertIn("mud-burden", self.state.terrain_statuses)

        self.setUp()
        actor = self.actor("estuary-ice-boom")
        for x in (29, 30, 31):
            self.state.region.materials[f"{x},24,0"] = MaterialCell(water=1, fluid="fresh")
        _threat_action(self.state, actor, False)
        _threat_action(self.state, actor, False)
        self.assertTrue(all(self.state.region.materials[f"{x},24,0"].ice for x in (29, 30, 31)))

    def test_no_current_hidden_position_is_acquired_after_losing_sight(self):
        actor = self.actor("terrace-reeve")
        self.state.region.tile_changes["33,24,0"] = "#"
        before = copy.deepcopy(self.state.region.materials)
        _threat_action(self.state, actor, False)
        self.assertIsNone(actor.marked_position)
        self.assertIsNone(actor.last_known_position)
        self.assertEqual(self.state.region.materials, before)

    def test_changed_geometry_blocks_a_prepared_release_without_spending_charge(self):
        actor = self.actor("fen-marshal")
        _threat_action(self.state, actor, False)
        self.state.region.tile_changes["33,24,0"] = "#"
        self.assertIn("blocked", _threat_action(self.state, actor, False))
        self.assertEqual(actor.supplies, 3)

    def test_disruption_cancels_prepared_material_action(self):
        actor = self.actor("terrace-reeve")
        _threat_action(self.state, actor, False)
        actor.intent = "disrupted by a hook"
        _threat_action(self.state, actor, False)
        self.assertIsNone(actor.marked_position)
        self.assertEqual(actor.supplies, 3)

    def test_all_elites_end_after_counted_charges_or_physical_control(self):
        for identity in ELITE_DEFINITIONS:
            self.setUp()
            actor = self.actor(identity)
            actor.supplies = 0
            _threat_action(self.state, actor, False)
            self.assertIn(actor.status, {"retreated", "disabled"})
            actor.status, actor.supplies = "engaged", 3
            self.state.region.changes["environment_control_used"] = True
            _threat_action(self.state, actor, False)
            self.assertIn(actor.status, {"negotiated", "disabled"})

    def test_machine_linkage_is_a_real_brace_or_cut_target(self):
        for support in (0, 3):
            self.setUp()
            actor = self.actor("estuary-drum")
            self.state.region.materials["35,24,0"].support = support
            _threat_action(self.state, actor, False)
            self.assertEqual(actor.status, "disabled")

    def test_outcome_reward_is_physical_counted_and_not_recreated_after_loss(self):
        state = self.state
        actor = self.actor("terrace-reeve")
        actor.status, actor.health = "defeated", 0
        record_outcomes(state)
        reward = next(i for i in state.items if i.kind == "relic:coalheart seed")
        self.assertEqual((reward.location, reward.ground_position), ("ground", actor.position))
        reward.location = "lost"
        record_outcomes(state)
        self.assertEqual(sum(i.kind == reward.kind for i in state.items), 1)
        self.assertEqual(reward.location, "lost")

    def test_named_return_spends_stock_occurs_once_and_does_not_revive_the_dead(self):
        state = self.state
        actor = self.actor("fen-marshal")
        actor.status, actor.health = "retreated", 2
        record_outcomes(state)
        state.market[state.region.objective_commodity].stock = 2
        revisit_claimants(state)
        self.assertEqual(actor.status, "retreated")
        state.returned_expeditions += 1
        revisit_claimants(state)
        self.assertEqual((actor.status, actor.health, actor.supplies), ("watching", 4, 2))
        self.assertEqual(state.market[state.region.objective_commodity].stock, 1)
        actor.status = "retreated"
        state.returned_expeditions += 1
        revisit_claimants(state)
        self.assertEqual(actor.status, "retreated")
        actor.status, actor.health = "defeated", 0
        revisit_claimants(state)
        self.assertEqual(actor.health, 0)

    def test_contact_settlement_checks_evidence_and_cost_and_persists_memory(self):
        state = self.state
        actor = self.actor("gorge-cordmaster")
        state.trade_credit = 2
        self.assertFalse(settle_claimant(state)[0])
        state.questlines[state.active_region_id].stage = 2
        changed, message = settle_claimant(state)
        self.assertTrue(changed)
        self.assertEqual(state.trade_credit, 0)
        self.assertEqual(actor.status, "negotiated")
        self.assertIn(actor.name, state.contacts[state.active_region_id][1].memories[-1])
        self.assertIn(actor.name, message)

    def test_returning_from_same_gangplank_triggers_one_provisioned_rival(self):
        from jomon.vessel import JOMON_GANGPLANK

        state = self.state
        actor = self.actor("fen-marshal")
        actor.status, actor.health = "retreated", 2
        record_outcomes(state)
        state.returned_expeditions += 1
        state.location, state.position = "jomon", JOMON_GANGPLANK
        state.support = "route survey"
        state.market[state.region.objective_commodity].stock = 2
        self.assertTrue(depart(state).changed)
        self.assertIn(actor.status, {"watching", "engaged"})
        self.assertTrue(state.region.changes[f"rival-return:{actor.id}"])

    def test_mid_warning_save_preserves_mark_charge_and_stolen_item(self):
        state = self.state
        actor = self.actor("fen-marshal")
        _threat_action(state, actor, False)
        item = create_item(state, "passive:witness token", "stolen working proof", location="enemy")
        item.region_id, actor.carrying_item_id = state.active_region_id, item.id
        loaded = game_state_from_dict(state.to_dict())
        restored = next(a for a in loaded.threats if a.id == actor.id)
        self.assertEqual(restored, actor)
        self.assertEqual(next(i for i in loaded.items if i.id == item.id).location, "enemy")

    def test_awareness_is_separate_from_warning_and_release(self):
        state = self.state
        actor = self.actor("terrace-reeve")
        actor.status = "watching"
        _advance_world(state)
        self.assertEqual(actor.status, "engaged")
        self.assertIsNone(actor.marked_position)
        _advance_world(state)
        self.assertIsNotNone(actor.marked_position)
        self.assertFalse(any(cell.fire for cell in state.region.materials.values()))

    def test_marks_cover_the_real_lane_and_do_not_reveal_hidden_actors(self):
        state = self.state
        actor = self.actor("fen-marshal")
        _threat_action(state, actor, False)
        expected = {Position(x, 24) for x in (29, 30, 31)}
        self.assertEqual(visible_danger_marks(state, expected | {actor.position}), expected)
        self.assertEqual(visible_danger_marks(state, expected), set())

    def test_contact_settlement_dispatch_is_reachable_from_its_actual_overlay(self):
        state = self.state
        actor = self.actor("gorge-cordmaster")
        state.trade_credit = 2
        state.questlines[state.active_region_id].stage = 2
        contact = state.contacts[state.active_region_id][1]
        schedule = state.actor_schedules[contact.id]
        state.position = Position(schedule.position.x - 1, schedule.position.y, schedule.position.z)
        kind, quit_game = _handle_overlay(state, f"contact-service:{contact.id}", ord("s"))
        self.assertFalse(quit_game)
        self.assertIsNone(kind)
        self.assertEqual(actor.status, "negotiated")

    def test_corrupt_reaction_cannot_be_assigned_to_a_different_elite(self):
        actor = self.actor("fen-marshal")
        actor.reaction = "firing"
        with self.assertRaises(StateError):
            game_state_from_dict(self.state.to_dict())


class EliteProductionTests(unittest.TestCase):
    def test_all_eight_initial_elites_are_seed_reachable_and_do_not_overlap(self):
        seen = set()
        for region_id in FRONTIERS:
            for seed in range(16):
                region = build_frontier(str(seed), region_id)
                actors = frontier_population(str(seed), region)
                actor = install_elite(str(seed), region, actors)
                self.assertEqual(actor.status, "dormant")
                self.assertEqual(len({a.position for a in actors}), len(actors))
                self.assertIn(actor.position, region_reachable(region))
                self.assertIn(f"{actor.position.x},{actor.position.y},{actor.position.z}", region.materials)
                seen.add(actor.id.split(":")[1])
        self.assertEqual(seen, set(ELITE_DEFINITIONS) - AFTERMATH_ELITES)

    def test_all_eight_aftermath_elites_have_a_persistent_production_path(self):
        seen = set()
        for region_id in sorted({data["region"] for data in ELITE_DEFINITIONS.values()}):
            state = create_world(f"aftermath elite production {region_id}")
            activate_region(state, region_id)
            state.location = "region"
            state.region.changes["aftermath_configuration"] = "shared"
            state.region.changes["aftermath_site:1"] = "50,30,0"
            actor = install_aftermath_elite(state)
            self.assertIsNotNone(actor)
            self.assertIn(actor.position, region_reachable(state.region))
            self.assertEqual(actor.status, "dormant")
            self.assertEqual(state.region.changes["aftermath_elite_installed"], actor.id.split(":", 1)[1])
            self.assertEqual(install_aftermath_elite(state), actor)
            self.assertEqual(sum(candidate.id == actor.id for candidate in state.threats), 1)
            restored = game_state_from_dict(state.to_dict())
            self.assertTrue(any(candidate == actor for candidate in restored.threats))
            seen.add(actor.id.split(":", 1)[1])
        self.assertEqual(seen, AFTERMATH_ELITES)

    def test_normal_generation_round_trip_and_contact_panel_both_sizes(self):
        state = create_world("elite smoke")
        for region_id in FRONTIERS:
            activate_region(state, region_id)
            state.location = "region"
            elites = [a for a in state.threats if definition(a)]
            self.assertEqual(len(elites), 1)
            loaded = game_state_from_dict(state.to_dict())
            self.assertEqual(loaded.threats, state.threats)
            elites[0].status = "watching"
            state.position = elites[0].position
            lines = observed_life_lines(state)
            self.assertTrue(any("Working charges 3" in line for line in lines))
            for height, width in ((24, 80), (32, 100)):
                _draw_dialogue_overlay(PanelSink(height, width), state, OverlayView(f"contact-service:{state.contacts[region_id][1].id}"))


if __name__ == "__main__":
    unittest.main()
