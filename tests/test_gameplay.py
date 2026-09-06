from __future__ import annotations

import json
import os
from pathlib import Path
import tempfile
import unittest

from jomon.actions import (
    _advance_threats,
    apply_damage,
    attack,
    choose_courier,
    choose_gear,
    choose_support,
    choose_weapon,
    decide_objective,
    depart,
    guard,
    interact,
    merchant_stock_for,
    merchant_visit_due,
    move,
    negotiate,
    purchase_merchant_item,
    return_to_jomon,
    use_gear,
)
from jomon.content import COMMODITIES, GEAR, JOMON_MAP, SUPPORTS, WEAPONS
from jomon.save import SaveError, load_game, save_game
from jomon.state import CommodityStack, Position, SAVE_FORMAT, create_world, validate_state
from jomon.terminal import SEMANTIC_ROLES, _handle_overlay, semantic_colour_plan, semantic_role
from jomon.world import (
    JOMON_GANGPLANK,
    REGION_GANGPLANK,
    build_combinations,
    capacity,
    connected_required_map,
    find_tile,
    pressure,
    room_route,
)


def prepared(
    seed: str = "rain quay 17", *, role: str = "bargemaster", weapon: str = "billhook",
    gear: str = "rope", support: str = "route survey",
):
    state = create_world(seed)
    courier = next(person for person in state.household if person.role == role)
    choose_courier(state, courier.id)
    if weapon not in state.owned_weapons:
        state.owned_weapons.append(weapon)
    if gear not in state.owned_gear:
        state.owned_gear.append(gear)
    choose_weapon(state, weapon)
    choose_gear(state, gear)
    choose_support(state, support)
    state.position = JOMON_GANGPLANK
    depart(state)
    return state


def enter_room(state, room_id: str, position: Position = Position(10, 6)):
    state.location = "region"
    state.current_room = room_id
    state.position = position
    state.region.rooms[room_id].discovered = True


def local_threat(state, profile: str):
    return next(threat for threat in state.local_threats() if threat.profile == profile)


class GenerationAndTopologyTests(unittest.TestCase):
    def test_same_seed_household_region_and_topology_are_identical(self):
        first = create_world("Hearth Rain 44")
        second = create_world("Hearth Rain 44")
        self.assertEqual(first.to_dict(), second.to_dict())

    def test_selected_seeds_vary_meaningful_topology(self):
        states = [create_world(f"topology sample {index}") for index in range(12)]
        signatures = {state.region.topology_signature for state in states}
        side_names = {state.region.rooms["works_side"].name for state in states}
        self.assertGreaterEqual(len(signatures), 3)
        self.assertGreaterEqual(len(side_names), 2)

    def test_twelve_rooms_cover_three_place_types(self):
        state = create_world("room identities")
        self.assertEqual(len(state.region.rooms), 12)
        self.assertEqual({room.place for room in state.region.rooms.values()}, {"settlement", "wilderness", "infrastructure"})
        for room in state.region.rooms.values():
            self.assertTrue(room.name)
            self.assertTrue(room.purpose)
            self.assertTrue(room.exits)

    def test_reachability_reciprocal_exits_objective_and_return(self):
        for index in range(50):
            with self.subTest(seed=index):
                state = create_world(f"route seed {index}")
                validate_state(state)
                self.assertTrue(connected_required_map(state))
                route = room_route(state, "hearthford_quay", state.region.objective_room)
                self.assertEqual(route[0], "hearthford_quay")
                self.assertEqual(route[-1], "wheelhouse")
                self.assertEqual(room_route(state, "wheelhouse", "hearthford_quay"), list(reversed(route)))

    def test_physical_exit_transition_backtracks_and_room_change_persists(self):
        state = prepared()
        enter_room(state, "reed_gate")
        state.position = find_tile(state.room.map_rows, "D")
        interact(state)
        self.assertTrue(state.room.changes["shutter_closed"])
        east = state.room.exits["east"]
        state.position = Position(east.position.x - 1, east.position.y)
        move(state, 1, 0)
        self.assertEqual(state.current_room, "willow_islet")
        west = state.room.exits["west"]
        state.position = Position(west.position.x + 1, west.position.y)
        move(state, -1, 0)
        self.assertEqual(state.current_room, "reed_gate")
        self.assertTrue(state.room.changes["shutter_closed"])

    def test_all_eight_goods_remain_physical(self):
        self.assertEqual(set(COMMODITIES), {"charcoal", "grain", "ironwork", "lime", "paper", "salt fish", "timber", "wool"})
        for definition in COMMODITIES.values():
            self.assertGreater(definition["bulk"], 0)
            self.assertTrue(definition["condition"] and definition["source"] and definition["use"])


class ColourAndPreparationTests(unittest.TestCase):
    def test_semantic_colour_roles_are_complete_and_distinct_at_eight_colours(self):
        plan = semantic_colour_plan(8, 16)
        self.assertEqual(set(plan), set(SEMANTIC_ROLES))
        self.assertNotEqual(plan["player"].pair, plan["hostile"].pair)
        self.assertNotEqual(plan["hostile"].pair, plan["elite"].pair)
        self.assertNotEqual(plan["water"].pair, plan["hazard"].pair)
        glyphs = {"@": "player", "a": "ally", "M": "neutral", "h": "hostile", "X": "elite", "~": "water", "#": "structure", ">": "exit", "R": "cargo", "&": "interactable", "%": "hazard", "*": "mystical"}
        for glyph, expected in glyphs.items():
            self.assertEqual(semantic_role(glyph), expected)

    def test_limited_colour_fallback_uses_no_pairs_and_keeps_bold_cues(self):
        plan = semantic_colour_plan(0, 0)
        self.assertTrue(all(style.pair == 0 and style.foreground is None for style in plan.values()))
        self.assertTrue(plan["player"].bold)
        self.assertTrue(plan["hostile"].bold)

    def test_tavern_opens_consolidated_menu_and_selection_is_zero_time(self):
        state = create_world("tavern preparation")
        state.position = find_tile(JOMON_MAP, "C")
        result = interact(state)
        self.assertEqual(result.overlay, "tavern")
        started = state.world_time
        overlay, _ = _handle_overlay(state, "tavern", ord("c"))
        self.assertEqual(overlay, "tavern:courier")
        overlay, _ = _handle_overlay(state, overlay, ord("1"))
        self.assertEqual(overlay, "tavern")
        _handle_overlay(state, "tavern:weapon", ord("1"))
        _handle_overlay(state, "tavern:gear", ord("1"))
        _handle_overlay(state, "tavern:support", ord("1"))
        self.assertIsNotNone(state.courier)
        self.assertIsNotNone(state.weapon)
        self.assertIsNotNone(state.gear)
        self.assertIsNotNone(state.support)
        self.assertEqual(state.world_time, started)

    def test_catalogue_is_bounded(self):
        self.assertEqual(len(WEAPONS), 6)
        self.assertEqual(len(GEAR), 8)
        self.assertEqual(len(SUPPORTS), 5)

    def test_six_qualitative_build_combinations_activate(self):
        cases = (
            ("bargemaster", "staff", "quiet shoes", "route survey", "surveyed soft-step"),
            ("bargemaster", "billhook", "rope", "route survey", "hooked rigging"),
            ("guard", "billhook", "buckler", "route survey", "shielded set stance"),
            ("factor", "cudgel", "cargo harness", "factor surety", "bonded cargo"),
            ("carpenter", "cudgel", "repair tools", "carpenter rig", "prepared repair crew"),
            ("healer", "staff", "rope", "field care", "deep field binding"),
        )
        for role, weapon, gear, support, expected in cases:
            with self.subTest(expected=expected):
                state = prepared(role=role, weapon=weapon, gear=gear, support=support)
                self.assertIn(expected, build_combinations(state))


class TimePressureAndLogTests(unittest.TestCase):
    def test_inspection_cancel_and_blocked_move_are_zero_time(self):
        state = create_world("still water")
        state.position = find_tile(JOMON_MAP, "C")
        before = state.world_time
        self.assertFalse(interact(state).time_advanced)
        _handle_overlay(state, "tavern", 27)
        blocked = move(state, -20, 0)
        self.assertFalse(blocked.changed)
        self.assertEqual(state.world_time, before)

    def test_movement_and_accepted_action_advance_exactly_once(self):
        state = prepared()
        started = state.world_time
        move(state, 1, 0)
        self.assertEqual(state.world_time, started + 1)
        enter_room(state, "tally_house", find_tile(state.region.rooms["tally_house"].map_rows, "M"))
        accepted = decide_objective(state, "accept")
        self.assertTrue(accepted.time_advanced)
        self.assertEqual(state.world_time, started + 2)

    def test_pressure_contributors_change_gameplay_effects(self):
        state = prepared()
        initial = pressure(state)
        enter_room(state, "wheelhouse")
        state.pressure_elapsed = 96
        state.noise = 5
        state.carried_goods["paper"] = CommodityStack(2, "dry")
        later = pressure(state)
        self.assertGreater(later.elapsed, initial.elapsed)
        self.assertGreater(later.depth, initial.depth)
        self.assertGreater(later.noise, initial.noise)
        self.assertGreater(later.valuables, initial.valuables)
        self.assertEqual(later.band, "critical")
        self.assertEqual(later.pursuit_steps, 2)
        self.assertGreater(later.alert_range, initial.alert_range)

    def test_routine_movement_does_not_displace_meaningful_log(self):
        state = prepared()
        state.messages = ["The crossbow watcher aims and fires next turn."]
        for _ in range(8):
            move(state, 1, 0)
            move(state, -1, 0)
        self.assertIn("The crossbow watcher aims and fires next turn.", state.messages)
        self.assertFalse(any(message == "You move." for message in state.messages))


class ThreatAndEnvironmentTests(unittest.TestCase):
    def test_pursuer_closes_and_telegraphs_close_strike(self):
        state = prepared()
        enter_room(state, "reed_gate", Position(18, 6))
        threat = local_threat(state, "pursuer")
        before = threat.position
        _advance_threats(state)
        self.assertEqual(threat.status, "engaged")
        _advance_threats(state)
        self.assertNotEqual(threat.position, before)
        self.assertTrue("closes" in threat.intent or "next turn" in threat.intent)

    def test_ranged_aim_is_readable_and_shutter_blocks_shot(self):
        state = prepared()
        enter_room(state, "lower_towpath", Position(17, 6))
        threat = local_threat(state, "ranged")
        _advance_threats(state)
        _advance_threats(state)
        self.assertIn("fires next turn", threat.intent)
        state.position = find_tile(state.room.map_rows, "D")
        interact(state)
        health = state.courier.health
        _advance_threats(state)
        self.assertEqual(state.courier.health, health)
        self.assertIn("line", threat.intent)

    def test_reach_opponent_attacks_from_two_cells(self):
        state = prepared()
        enter_room(state, "mill_yard", Position(16, 6))
        threat = local_threat(state, "reach")
        threat.status = "engaged"
        threat.position = Position(18, 6)
        _advance_threats(state)
        self.assertIn("thrusts next turn", threat.intent)

    def test_basic_guard_answers_telegraphed_attack_without_special_gear(self):
        state = prepared(weapon="billhook", gear="repair tools", support="route survey")
        enter_room(state, "mill_yard", Position(16, 6))
        threat = local_threat(state, "reach")
        threat.status = "engaged"
        threat.position = Position(18, 6)
        threat.intent = "braces and thrusts next turn"
        health = state.courier.health
        guard(state)
        self.assertEqual(state.courier.health, health)
        self.assertIn("guard catches", state.messages[-1])

    def test_animal_evasion_uses_positioned_mud(self):
        state = prepared(gear="rope")
        enter_room(state, "eel_cut")
        state.position = Position(12, 6)
        threat = local_threat(state, "animal")
        threat.position = Position(15, 6)
        threat.status = "engaged"
        threat.intent = "lowers its head and charges next turn"
        _advance_threats(state)
        self.assertEqual(threat.status, "evaded")
        self.assertTrue(any("mud channel" in event for event in state.history))

    def test_lantern_is_second_noncombat_animal_resolution(self):
        state = prepared(gear="hooded lantern")
        enter_room(state, "eel_cut", Position(18, 5))
        threat = local_threat(state, "animal")
        threat.status = "engaged"
        result = use_gear(state)
        self.assertTrue(result.time_advanced)
        self.assertEqual(threat.status, "evaded")

    def test_mixed_encounter_contains_complementary_reach_and_ranged_profiles(self):
        state = prepared()
        enter_room(state, "mill_yard", Position(17, 6))
        self.assertEqual({threat.profile for threat in state.local_threats()}, {"reach", "ranged"})
        _advance_threats(state)
        self.assertTrue(all(threat.status == "engaged" for threat in state.local_threats()))

    def test_direct_combat_victory_and_crossbow_reload(self):
        state = prepared(weapon="crossbow", gear="buckler")
        enter_room(state, "lower_towpath", Position(17, 6))
        threat = local_threat(state, "ranged")
        threat.status, threat.health, threat.position = "engaged", 3, Position(21, 6)
        attack(state)
        self.assertEqual(threat.status, "defeated")
        self.assertFalse(state.crossbow_loaded)
        enter_room(state, "mill_yard", Position(16, 6))
        local_threat(state, "reach").status = "engaged"
        guard(state)
        self.assertTrue(state.crossbow_loaded)

    def test_human_group_accepts_material_negotiation(self):
        state = prepared(role="factor", gear="trade seals", support="factor surety")
        enter_room(state, "mill_yard", Position(19, 6))
        for threat in state.local_threats():
            threat.status = "engaged"
        result = negotiate(state)
        self.assertTrue(result.time_advanced)
        self.assertTrue(all(threat.status == "negotiated" for threat in state.local_threats()))
        self.assertTrue(any("without bloodshed" in memory for memory in state.contact.memories))

    def test_three_environment_controls_cross_system_boundaries(self):
        state = prepared(weapon="billhook", gear="rope", support="carpenter rig")
        enter_room(state, "reed_gate", find_tile(state.region.rooms["reed_gate"].map_rows, "D"))
        pursuer = local_threat(state, "pursuer")
        pursuer.status = "engaged"
        interact(state)
        self.assertEqual(pursuer.status, "evaded")
        self.assertTrue(state.room.changes["shutter_closed"])

        enter_room(state, "crane_walk", find_tile(state.region.rooms["crane_walk"].map_rows, "&"))
        old_noise = state.noise
        interact(state)
        self.assertEqual(state.flood_control, "lowered")
        self.assertTrue(state.room.changes["structure_stable"])
        self.assertEqual(state.noise, old_noise)

        enter_room(state, "mill_yard", find_tile(state.region.rooms["mill_yard"].map_rows, "O"))
        interact(state)
        self.assertTrue(state.room.changes["cover_moved"])
        self.assertGreater(state.noise, old_noise)
        self.assertTrue(any("cover" in message for message in state.messages))

    def test_rare_elite_changes_terrain_timing_and_can_be_materially_disabled(self):
        state = next(
            prepared(f"elite seed {index}", gear="repair tools", support="carpenter rig")
            for index in range(100) if any(threat.elite for threat in create_world(f"elite seed {index}").threats)
        )
        enter_room(state, "wheelhouse", Position(15, 6))
        elite = local_threat(state, "machinery")
        self.assertTrue(elite.elite)
        _advance_threats(state)
        _advance_threats(state)
        self.assertIn("machinery_phase", state.room.changes)
        state.position = find_tile(state.room.map_rows, "&")
        interact(state)
        self.assertEqual(elite.status, "disabled")
        self.assertTrue(state.room.changes["structure_stable"])

    def test_machinery_has_a_readable_safe_staging_zone(self):
        state = prepared(gear="repair tools")
        enter_room(state, "wheelhouse", Position(2, 6))
        machinery = local_threat(state, "machinery")
        machinery.status = "engaged"
        health = state.courier.health
        _advance_threats(state)
        _advance_threats(state)
        self.assertEqual(state.courier.health, health)
        self.assertIn("staging bay", machinery.intent)


class PersistenceEconomyAndDefeatTests(unittest.TestCase):
    def test_returned_equipment_persists(self):
        state = prepared(weapon="spear", gear="quiet shoes", support="route survey")
        weapons, gear = list(state.owned_weapons), list(state.owned_gear)
        enter_room(state, "hearthford_quay", REGION_GANGPLANK)
        return_to_jomon(state)
        self.assertEqual(state.owned_weapons, weapons)
        self.assertEqual(state.owned_gear, gear)
        self.assertEqual(state.weapon, "spear")
        self.assertEqual(state.gear, "quiet shoes")

    def test_discovery_persists_consumes_and_loss_is_readable(self):
        state = prepared()
        enter_room(state, "willow_islet", find_tile(state.region.rooms["willow_islet"].map_rows, "?"))
        state.room.discovery = "willow dressing"
        interact(state)
        self.assertEqual(state.consumables["willow dressing"], 1)
        state.courier.injury, state.courier.health = "cut arm", 5
        use_gear(state)
        self.assertNotIn("willow dressing", state.consumables)
        self.assertGreater(state.courier.health, 5)
        state.gear = "rope"
        state.courier.health, state.courier.injury = 1, "none"
        message = apply_damage(state, 2, "The bank runner")
        self.assertNotIn("rope", state.owned_gear)
        self.assertIn("leave rope behind", message)

    def test_capacity_and_delivery_change_market(self):
        state = prepared(gear="cargo harness", support="porter watch")
        self.assertIn("high-capacity watch", build_combinations(state))
        self.assertEqual(capacity(state), 17 if state.courier.role in {"carpenter", "guard"} else 15)
        enter_room(state, "tally_house", find_tile(state.region.rooms["tally_house"].map_rows, "M"))
        decide_objective(state, "accept")
        enter_room(state, "wheelhouse", find_tile(state.region.rooms["wheelhouse"].map_rows, "R"))
        local_threat(state, "machinery").status = "disabled"
        interact(state)
        commodity = state.region.objective_commodity
        old_demand = state.market[commodity].demand
        enter_room(state, "tally_house", find_tile(state.region.rooms["tally_house"].map_rows, "M"))
        interact(state)
        self.assertEqual(state.objective_status, "completed")
        self.assertLess(state.market[commodity].demand, old_demand)
        self.assertGreater(state.trade_credit, 0)

    def test_contextual_injury_loss_and_permanent_succession(self):
        state = prepared(gear="rope", support="route survey")
        state.courier.health = 2
        state.carried_goods["paper"] = CommodityStack(1, "dry")
        message = apply_damage(state, 3, "The bolt")
        self.assertEqual(state.location, "jomon")
        self.assertEqual(state.courier.injury, "deep cut")
        self.assertFalse(state.carried_goods)
        self.assertIn("injured", message)

        state = prepared("succession", gear="rope", support="route survey")
        dead = state.courier
        dead.health, dead.injury = 1, "bruised ribs"
        message = apply_damage(state, 3, "The flood")
        self.assertFalse(dead.alive)
        self.assertNotEqual(state.active_courier_id, dead.id)
        self.assertIn("takes up", message)

    def test_merchant_timing_stock_and_exchange_are_deterministic(self):
        seed = "merchant river"
        due = [count for count in range(1, 7) if merchant_visit_due(seed, count)]
        self.assertEqual(len(due), 2)
        self.assertEqual(due[1] - due[0], 3)
        first, second = create_world(seed), create_world(seed)
        first.returned_expeditions = second.returned_expeditions = due[0]
        first.objective_status = second.objective_status = "completed"
        self.assertEqual(merchant_stock_for(first), merchant_stock_for(second))
        state = first
        state.merchant_present = True
        state.merchant_stock = merchant_stock_for(state)
        state.trade_credit = 10
        item = state.merchant_stock[0]
        before = state.world_time
        result = purchase_merchant_item(state, item)
        self.assertTrue(result.time_advanced)
        self.assertEqual(state.world_time, before + 1)
        self.assertNotIn(item, state.merchant_stock)
        self.assertTrue(item in state.owned_weapons or item in state.owned_gear or item in state.consumables or item in state.relics)

    def test_save_load_and_changed_second_expedition(self):
        state = prepared(gear="repair tools", support="carpenter rig")
        enter_room(state, "tally_house", find_tile(state.region.rooms["tally_house"].map_rows, "M"))
        decide_objective(state, "alter")
        enter_room(state, "crane_walk", find_tile(state.region.rooms["crane_walk"].map_rows, "&"))
        interact(state)
        enter_room(state, "tally_house", find_tile(state.region.rooms["tally_house"].map_rows, "M"))
        interact(state)
        changed_demand = state.market[state.region.objective_commodity].demand
        enter_room(state, "hearthford_quay", REGION_GANGPLANK)
        return_to_jomon(state)
        with tempfile.TemporaryDirectory() as directory:
            target = Path(directory) / "save.json"
            save_game(state, target)
            restored = load_game(target)
        self.assertEqual(restored, state)
        restored.position = JOMON_GANGPLANK
        depart(restored)
        self.assertEqual(restored.expedition_count, 2)
        self.assertEqual(restored.flood_control, "lowered")
        self.assertTrue(restored.region.rooms["crane_walk"].changes["structure_stable"])
        self.assertEqual(restored.market[restored.region.objective_commodity].demand, changed_demand)

    def test_atomic_save_override_and_bad_saves(self):
        with tempfile.TemporaryDirectory() as directory:
            previous = os.environ.get("JOMON_DATA_DIR")
            os.environ["JOMON_DATA_DIR"] = directory
            try:
                state = create_world("atomic room")
                target = save_game(state)
                self.assertEqual(load_game(), state)
                self.assertFalse(target.with_name(f".{target.name}.tmp").exists())
                target.write_text("{bad", encoding="utf-8")
                with self.assertRaisesRegex(SaveError, "corrupt JSON"):
                    load_game(target)
                target.write_text(json.dumps({"save_format": SAVE_FORMAT + 1}), encoding="utf-8")
                with self.assertRaisesRegex(SaveError, "incompatible save format"):
                    load_game(target)
            finally:
                if previous is None:
                    del os.environ["JOMON_DATA_DIR"]
                else:
                    os.environ["JOMON_DATA_DIR"] = previous


if __name__ == "__main__":
    unittest.main()
