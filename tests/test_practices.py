from __future__ import annotations

import copy
import unittest

from jomon.actions import attack, choose_weapon, effective_weapon_range, guard, interact, move
from jomon.frontiers import FRONTIERS, ensure_frontier
from jomon.inventory import BODY_SLOTS, auto_place, create_item, load_state, terrain_status_for
from jomon.materials import handle_material
from jomon.practices import (
    AFTERMATH_REGION_PRACTICE,
    NETWORK_CONTACT_PRACTICE,
    PRACTICES,
    validate_practices,
)
from jomon.quests import secondary_service_options, use_secondary_service
from jomon.regional_history import network_institution_for_contact
from jomon.regions import activate_region
from jomon.state import MaterialCell, Position, Threat, create_world
from jomon.world import position_key, sight_radius


class LearnedPracticeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("reciprocal learned practices")
        for region_id in FRONTIERS:
            ensure_frontier(cls.base, region_id)

    def setUp(self):
        self.state = copy.deepcopy(self.base)
        activate_region(self.state, "hearthford")
        self.state.location = "region"
        self.state.position = Position(40, 24)
        self.state.threats.clear()
        self.state.weather = "clear"
        self.state.weapon, self.state.gear = "cudgel", "buckler"
        self.state.courier.technique = "quiet passage"
        for y in range(18, 31):
            for x in range(30, 51):
                self.state.region.tile_changes[f"{x},{y},0"] = "."

    def learn(self, name: str, state=None):
        (state or self.state).courier.learned_techniques.append(name)

    def test_sixteen_definitions_have_distinct_effects_and_bounded_sources(self):
        validate_practices()
        self.assertEqual(len(PRACTICES), 16)
        self.assertEqual(len({practice.effect for practice in PRACTICES.values()}), 16)
        self.assertEqual(len(NETWORK_CONTACT_PRACTICE), 8)
        self.assertEqual(len(AFTERMATH_REGION_PRACTICE), 8)

    def test_all_network_witnesses_teach_a_distinct_practice_after_trust(self):
        learned = set()
        for contact_id, practice in NETWORK_CONTACT_PRACTICE.items():
            region_id = contact_id.removeprefix("network-contact-")
            activate_region(self.state, region_id)
            institution = network_institution_for_contact(self.state, contact_id)
            institution.trust = 1
            option = next(row for row in secondary_service_options(self.state, contact_id) if row[0] == "t")
            self.assertTrue(option[3])
            changed, message = use_secondary_service(self.state, "t", contact_id)
            self.assertTrue(changed)
            self.assertIn(practice, message)
            self.assertFalse(use_secondary_service(self.state, "t", contact_id)[0])
            learned.add(practice)
        self.assertEqual(learned, set(NETWORK_CONTACT_PRACTICE.values()))

    def test_water_and_mud_practices_change_action_cost_and_noise(self):
        water = copy.deepcopy(self.state)
        water.water["41,24,0"] = 1
        before = water.world_time
        self.assertTrue(move(water, 1, 0).time_advanced)
        self.assertEqual(water.world_time, before + 2)

        cadence = copy.deepcopy(self.state)
        cadence.water["41,24,0"] = 1
        self.learn("bank-water cadence", cadence)
        before = cadence.world_time
        move(cadence, 1, 0)
        self.assertEqual(cadence.world_time, before + 1)

        mud = copy.deepcopy(self.state)
        mud.region.tile_changes["41,24,0"] = "m"
        baseline = move(mud, 1, 0)
        self.assertIn("Mud drags", baseline.message)
        measured = copy.deepcopy(self.state)
        measured.region.tile_changes["41,24,0"] = "m"
        self.learn("field-rill measure", measured)
        self.assertNotIn("Mud drags", move(measured, 1, 0).message)

    def test_porter_relay_only_removes_wet_crossing_delay_when_heavily_loaded(self):
        for state in (self.state,):
            lot = create_item(
                state, "commodity:timber", "heavy relay test", quantity=8,
            )
            self.assertTrue(auto_place(state, lot.id, "pack", owner_id=state.active_courier_id))
            self.assertIn(load_state(state), {"encumbered", "overloaded"})
            state.water["41,24,0"] = 1
        baseline = copy.deepcopy(self.state)
        before = baseline.world_time
        move(baseline, 1, 0)
        self.assertEqual(baseline.world_time, before + 2)
        self.learn("island porter relay")
        before = self.state.world_time
        move(self.state, 1, 0)
        self.assertEqual(self.state.world_time, before + 1)

    def test_smoke_storm_height_and_ice_practices_use_real_world_reducers(self):
        smoke_key = position_key(self.state.position)
        self.state.smoke[smoke_key] = 4
        self.assertEqual(sight_radius(self.state), 3)
        self.learn("ash-refuge breathing")
        self.assertEqual(sight_radius(self.state), 5)
        for item in self.state.items:
            if item.owner_id == self.state.active_courier_id and item.location in BODY_SLOTS:
                item.location, item.owner_id = "ground", None
        self.assertEqual(terrain_status_for(self.state, "s")[2], 2)

        storm = copy.deepcopy(self.state)
        storm.smoke.clear()
        storm.weather = "coast squall"
        baseline = sight_radius(storm)
        self.learn("ebb beacon watch", storm)
        self.assertEqual(sight_radius(storm), baseline + 1)

        high = copy.deepcopy(self.state)
        high.smoke.clear()
        high.position = Position(40, 24, 1)
        high.weapon = "sling"
        baseline = effective_weapon_range(high)
        self.learn("ridge-sounding line", high)
        self.assertEqual(effective_weapon_range(high), baseline + 1)

        ice = copy.deepcopy(self.state)
        self.assertEqual(terrain_status_for(ice, "_")[0], "poor-footing")
        self.learn("winter-braid reading", ice)
        self.assertIsNone(terrain_status_for(ice, "_"))

    def test_three_aftermath_work_practices_replace_tools_for_exact_verbs(self):
        cases = (
            ("siltgate hand", "dig", "soil", "m"),
            ("living firebreak", "cut", "reeds", "."),
            ("peat brace seating", "brace", "timber", "."),
        )
        for practice, verb, material, tile in cases:
            with self.subTest(practice=practice):
                state = copy.deepcopy(self.state)
                point = Position(41, 24)
                state.region.tile_changes[position_key(point)] = tile
                state.region.materials[position_key(point)] = MaterialCell(
                    material=material, support=1,
                )
                self.assertFalse(handle_material(state, verb, point)[0])
                self.learn(practice, state)
                self.assertTrue(handle_material(state, verb, point)[0])

    def test_stair_breath_removes_injury_delay_from_a_known_upward_link(self):
        link = next(link for link in self.state.region.vertical_links if link.first.z != link.second.z)
        lower = min((link.first, link.second), key=lambda point: point.z)
        self.state.position = lower
        self.state.courier.injuries["legs"] = "strained knee"
        baseline = copy.deepcopy(self.state)
        before = baseline.world_time
        self.assertTrue(interact(baseline).time_advanced)
        self.assertEqual(baseline.world_time, before + 2)
        self.learn("honest stair breath")
        before = self.state.world_time
        interact(self.state)
        self.assertEqual(self.state.world_time, before + 1)

    def test_reach_and_support_practices_change_guard_in_active_combat(self):
        target = Threat(
            "practice-target", "measured line holder", "pursuer",
            Position(46, 24), 20, 20, status="engaged", morale=9,
        )
        self.state.threats = [target]
        self.state.weapon = "spear"
        self.state.gear = "rope"
        for item in self.state.items:
            if item.owner_id == self.state.active_courier_id and item.location in BODY_SLOTS:
                item.location, item.owner_id = "ground", None
        baseline = copy.deepcopy(self.state)
        self.assertIn("yield space", guard(baseline).message)
        self.learn("two-span withdrawal")
        self.assertIn("reinforced guard", guard(self.state).message)

        support = copy.deepcopy(self.state)
        support.threats = [copy.deepcopy(target)]
        support.region.materials[position_key(support.position)] = MaterialCell(
            material="timber", support=1, collapse_due=50,
        )
        self.learn("span-watch stance", support)
        result = guard(support)
        self.assertIn("Span-watch stance", result.message)
        cell = support.region.materials[position_key(support.position)]
        self.assertGreaterEqual(cell.support, 2)
        self.assertEqual(cell.collapse_due, 0)

    def test_clay_tread_and_net_recovery_leave_inspectable_physical_results(self):
        self.assertEqual(terrain_status_for(self.state, "m")[0], "bogged")
        self.learn("seed-clay tread")
        self.assertIsNone(terrain_status_for(self.state, "m"))

        net = copy.deepcopy(self.state)
        net.owned_weapons.append("weighted net")
        net.location = "jomon"
        self.assertTrue(choose_weapon(net, "weighted net").changed)
        net.location = "region"
        net.position = Position(40, 24)
        self.learn("thaw-net recovery", net)
        bundle = create_item(net, "consumable:casting net bundle", "net test")
        self.assertTrue(auto_place(net, bundle.id, "pack", owner_id=net.active_courier_id))
        target = Threat(
            "net-target", "channel runner", "pursuer", Position(44, 24),
            12, 12, status="engaged", morale=9,
        )
        net.threats = [target]
        result = attack(net, target.id)
        self.assertIn("physical net", result.message)
        recovered = [
            item for item in net.items
            if item.location == "ground" and "Thaw-net Recovery" in item.provenance
        ]
        self.assertEqual(len(recovered), 1)
        self.assertEqual(recovered[0].kind, "consumable:casting net bundle")

    def test_wreck_title_records_only_the_first_difficult_regional_cache(self):
        container = next(item for item in self.state.region.containers if item.requirement)
        self.state.position = container.position
        self.learn("wreck-title hold")
        if container.requirement == "rope":
            self.state.gear = "rope"
        elif container.requirement == "light":
            self.state.gear = "hooded lantern"
        else:
            self.state.gear = "repair tools"
        account = self.state.institutions["work:hearthford"]
        before = account.confidence
        result = interact(self.state)
        self.assertTrue(result.time_advanced)
        self.assertIn("Wreck-title hold", result.message)
        self.assertEqual(account.confidence, min(3, before + 1))
        self.assertTrue(self.state.vessel_changes["wreck_title_hold:hearthford"])


if __name__ == "__main__":
    unittest.main()
