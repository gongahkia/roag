import copy
import unittest
from dataclasses import asdict

from jomon.actions import _advance_world, _threat_action, apply_damage, attack, emit_sound, interact, move
from jomon.ecology import resolve_world_action, world_options
from jomon.enemy_ai import next_path_step, sees_courier
from jomon.inventory import auto_place, create_item, drop_item, equipped_item, release_enemy_possession
from jomon.materials import _expose, advance_materials, fields
from jomon.ship_crises import HAZARD_STATIONS, TACTICAL, VOYAGES, abandon_deck, begin_deck, work
from jomon.state import CommodityStack, MaterialCell, Position, StateError, Threat, create_world, game_state_from_dict
from jomon.terminal import InputEvent, OverlayView, _draw_dialogue_overlay, _handle_overlay_view, dialogue_choices, visible_threats
from jomon.travel import choose_destination, resolve_voyage, voyage_for
from jomon.world import field_of_view, line_of_sight
from test_information_panels import PanelSink


class DeckCrisisTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("counted deck scenarios")

    def setUp(self):
        self.state = copy.deepcopy(self.base)

    def begin(self, kind):
        changed, _ = choose_destination(self.state, "reed-anchor", forced_voyage=kind)
        self.assertTrue(changed)
        self.assertTrue(begin_deck(self.state)[0])

    def test_all_twelve_have_production_choices_and_effective_resolutions(self):
        for kind in VOYAGES:
            with self.subTest(kind=kind):
                self.setUp()
                state = self.state
                self.assertTrue(choose_destination(state, "reed-anchor", forced_voyage=kind)[0])
                self.assertTrue(dialogue_choices(state, "voyage"))
                if kind in TACTICAL:
                    view = OverlayView("voyage")
                    self.assertTrue(_handle_overlay_view(state, view, InputEvent("key", ord("p")))[0])
                    self.assertTrue(state.combat_active)
                    if kind in HAZARD_STATIONS:
                        state.position = HAZARD_STATIONS[kind]
                        result = interact(state)
                        self.assertEqual(result.overlay, "ship-work:emergency")
                        before = state.world_time
                        self.assertTrue(work(state, "emergency")[0])
                        self.assertEqual(state.world_time, before + 3)
                    else:
                        for actor in state.vessel_threats:
                            actor.status = "negotiated"
                        _advance_world(state)
                else:
                    response = {"lure": "navigate", "shoal": "navigate", "driftwood": "repel", "inspection": "navigate"}[kind]
                    self.assertTrue(resolve_voyage(state, response)[0])
                self.assertEqual(state.route_current_node, "reed-anchor")
                self.assertFalse(state.combat_active)

    def test_entering_and_inspecting_never_fires_an_opening_shot(self):
        self.begin("boarders")
        state = self.state
        self.assertTrue(all(actor.aimed_at is None and actor.status == "watching" for actor in state.vessel_threats))
        before = state.to_dict()
        view = OverlayView("voyage")
        _draw_dialogue_overlay(PanelSink(), state, view)
        _handle_overlay_view(state, view, InputEvent("key", 27))
        self.assertEqual(state.to_dict(), before)
        shooter = state.vessel_threats[0]
        state.position = Position(shooter.position.x - 5, shooter.position.y, 1)
        health = state.courier.health
        _advance_world(state)
        self.assertEqual(state.courier.health, health)

    def test_unresolved_passage_cannot_be_bypassed_at_the_gangplank(self):
        from jomon.vessel import JOMON_GANGPLANK
        state = self.state
        choose_destination(state, "reed-anchor", forced_voyage="inspection")
        state.position = JOMON_GANGPLANK
        before = state.world_time
        result = interact(state)
        self.assertEqual(result.overlay, "voyage")
        self.assertEqual(state.location, "jomon")
        self.assertEqual(state.world_time, before)

    def test_ashore_populations_are_not_replaced_by_boarding_or_save(self):
        state = self.state
        before = {key: [asdict(actor) for actor in value] for key, value in state.region_threats.items()}
        self.begin("raiders")
        _advance_world(state, steps=4)
        loaded = game_state_from_dict(state.to_dict())
        self.assertEqual(before, {key: [asdict(actor) for actor in value] for key, value in loaded.region_threats.items()})
        self.assertEqual(loaded.vessel_threats, state.vessel_threats)
        abandon_deck(loaded)
        self.assertFalse(loaded.combat_active)
        self.assertTrue(all(actor.status == "retreated" for actor in loaded.vessel_threats))

    def test_movement_and_hatches_take_time_only_during_a_crisis(self):
        state = self.state
        state.position = Position(14, 10)
        before = state.world_time
        interact(state)
        self.assertEqual(state.world_time, before)
        self.begin("hold-thieves")
        state.position = Position(14, 10)
        before = state.world_time
        self.assertTrue(interact(state).time_advanced)
        self.assertEqual(state.position.z, -1)
        self.assertEqual(state.world_time, before + 1)
        self.assertTrue(move(state, -1, 0).time_advanced)

    def test_deck_doors_and_hatches_are_used_by_existing_pathfinding(self):
        self.begin("raiders")
        state = self.state
        actor = state.vessel_threats[0]
        actor.position = Position(14, 10, -1)
        self.assertEqual(next_path_step(state, actor, Position(15, 10), stop_distance=0), Position(14, 10))
        self.assertTrue(line_of_sight(state, Position(14, 10, -1), Position(14, 10)))
        self.assertFalse(line_of_sight(state, Position(20, 10, -1), Position(20, 10)))
        state.position = Position(33, 9, -1)
        move(state, 0, -1)
        self.assertEqual(state.vessel_tiles["33,8,-1"], "/")
        self.assertEqual(game_state_from_dict(state.to_dict()).vessel_tiles, state.vessel_tiles)

    def test_enemy_perception_and_rendering_do_not_see_through_a_ship_wall(self):
        self.begin("boarders")
        state = self.state
        shooter = state.vessel_threats[0]
        shooter.position, state.position = Position(42, 10, 1), Position(39, 10, 1)
        self.assertFalse(sees_courier(state, shooter))
        self.assertNotIn(shooter.position, visible_threats(state, field_of_view(state)))
        state.vessel_tiles["41,10,1"] = "/"
        self.assertTrue(sees_courier(state, shooter))
        self.assertIn(shooter.position, visible_threats(state, field_of_view(state)))

    def test_deck_sight_cache_invalidates_doors_smoke_and_location(self):
        self.begin("boarders")
        state = self.state
        state.position = Position(39, 10, 1)
        target = Position(43, 10, 1)
        before = field_of_view(state)
        self.assertNotIn(target, before)
        state.vessel_tiles["41,10,1"] = "/"
        self.assertIn(target, field_of_view(state))
        state.smoke["41,10,1"] = 3
        self.assertNotIn(target, field_of_view(state))
        state.smoke.clear()
        state.position = Position(48, 10, 1)
        self.assertIn(Position(48, 10, 0), field_of_view(state))
        self.assertNotIn("_fov_cache", state.to_dict())

    def test_deck_thief_can_take_a_visible_adjacent_couriers_physical_load(self):
        from jomon.inventory import record_acquisition
        self.begin("raiders")
        state = self.state
        item = create_item(state, "passive:witness token", "deck theft regression", location="ground")
        self.assertTrue(auto_place(state, item.id, "pack", owner_id=state.active_courier_id))
        record_acquisition(state, item)
        thief = state.vessel_threats[0]
        thief.status, thief.position = "engaged", Position(54, 11)
        state.position = Position(53, 11)
        _threat_action(state, thief, False)
        self.assertEqual(thief.carrying_item_id, item.id)
        self.assertEqual(item.location, "enemy")

    def test_hatch_sound_alerts_above_without_omniscient_last_position(self):
        self.begin("raiders")
        state = self.state
        actor = state.vessel_threats[0]
        actor.position, state.position = Position(14, 10), Position(14, 10, -1)
        origin = Position(14, 9, -1)
        emit_sound(state, 3, origin)
        from jomon.enemy_ai import heard_position
        self.assertEqual(heard_position(state, actor), origin)
        self.assertNotEqual(heard_position(state, actor), state.position)

    def test_hold_theft_moves_one_real_lot_and_defeat_releases_it(self):
        state = self.state
        cargo_before = sum(stack.quantity for stack in state.vessel_cargo.values())
        self.begin("hold-thieves")
        self.assertEqual(sum(stack.quantity for stack in state.vessel_cargo.values()), cargo_before - 1)
        item = next(item for item in state.items if item.region_id == "jomon" and item.location == "ground")
        thief = state.vessel_threats[0]
        thief.position = Position(item.ground_position.x - 1, item.ground_position.y, -1)
        state.position = Position(22, 11, -1)
        decision = max(world_options(state, thief, False), key=lambda action: action.utility)
        self.assertEqual(decision.action, "take ground item")
        resolve_world_action(state, thief, decision)
        self.assertEqual(item.location, "enemy")
        thief.status = "defeated"
        release_enemy_possession(state, thief)
        self.assertEqual(item.region_id, "jomon")
        self.assertEqual(item.ground_position, thief.position)
        self.assertEqual(sum(candidate.id == item.id for candidate in state.items), 1)

    def test_material_verbs_can_end_a_hazard_without_its_station_reducer(self):
        from jomon.materials import handle_material
        self.begin("galley-fire")
        state = self.state
        state.position = HAZARD_STATIONS["galley-fire"]
        self.assertTrue(handle_material(state, "extinguish", Position(9, 5))[0])
        self.assertEqual(state.voyage_status, "resolved")
        self.assertEqual(state.vessel_materials["9,5,0"].fire, 0)

    def test_physical_bait_and_morale_are_noncombat_deck_resolutions(self):
        self.begin("creature")
        state = self.state
        state.position = Position(8, 5)
        self.assertEqual(interact(state).overlay, "ship-work:bait")
        before = state.vessel_cargo["salt fish"].quantity
        self.assertTrue(work(state, "bait")[0])
        self.assertEqual(state.vessel_cargo["salt fish"].quantity, before - 1)
        self.assertTrue(all(actor.status == "evaded" for actor in state.vessel_threats))
        self.assertEqual(state.voyage_status, "resolved")

    def test_empty_quiver_boarder_can_leave_by_its_actual_entry(self):
        self.begin("boarders")
        state = self.state
        actor = state.vessel_threats[0]
        actor.ammunition, actor.status = 0, "engaged"
        actor.position = actor.home_position
        state.position = Position(actor.position.x - 5, actor.position.y, actor.position.z)
        _threat_action(state, actor, False)
        self.assertEqual(actor.goal, "break contact")
        self.assertIn("ammunition", actor.intent)
        _advance_world(state)
        self.assertEqual(actor.status, "retreated")

    def test_stay_warning_allows_the_normal_chart_to_control_walk(self):
        state = self.state
        state.position = Position(28, 10, 1)
        self.begin("storm")
        for _ in range(27):
            self.assertTrue(move(state, 1, 0).time_advanced)
        self.assertEqual(state.position, HAZARD_STATIONS["storm"])
        self.assertEqual(state.voyage_status, "active")
        self.assertEqual(state.vessel_integrity, 10)
        self.assertTrue(work(state, "emergency")[0])
        self.assertEqual(state.voyage_status, "resolved")
        self.assertEqual(state.vessel_integrity, 10)
        self.setUp()
        self.begin("raiders")
        state = self.state
        for actor in state.vessel_threats:
            actor.status, actor.morale = "engaged", 0
        _advance_world(state)
        self.assertEqual(state.voyage_status, "resolved")

    def test_temporary_smoke_and_statuses_expire_through_moored_work_not_idle(self):
        state = self.state
        state.smoke["14,10,0"] = 2
        before = state.to_dict()
        field_of_view(state)
        self.assertEqual(state.to_dict(), before)
        _advance_world(state, steps=2)
        self.assertNotIn("14,10,0", state.smoke)

    def test_physical_ammunition_and_attack_are_shared_on_deck(self):
        self.begin("raiders")
        state = self.state
        actor = state.vessel_threats[0]
        state.position, actor.position = Position(54, 11), Position(55, 11)
        old = actor.health
        result = attack(state, actor.id)
        self.assertTrue(result.time_advanced)
        self.assertLess(actor.health, old)
        self.assertTrue(any("Billhook" in message or "billhook" in message for message in state.messages))

    def test_player_deck_shot_consumes_physical_ammunition_and_requires_reload(self):
        from jomon.inventory import equip_item, physical_ammunition, sync_legacy_load
        self.begin("raiders")
        state = self.state
        weapon = create_item(state, "crossbow", "deck test", location="ground")
        self.assertTrue(auto_place(state, weapon.id, "pack", owner_id=state.active_courier_id))
        self.assertTrue(equip_item(state, weapon.id))
        ammo = create_item(state, "consumable:crossbow bolts", "deck test", location="ground")
        self.assertTrue(auto_place(state, ammo.id, "pack", owner_id=state.active_courier_id))
        sync_legacy_load(state)
        actor = state.vessel_threats[0]
        actor.position, state.position = Position(55, 11), Position(51, 11)
        state.aimed_target = actor.id
        before = physical_ammunition(state, "bolts")
        self.assertTrue(attack(state, actor.id).time_advanced)
        self.assertEqual(physical_ammunition(state, "bolts"), before - 1)
        self.assertFalse(state.crossbow_loaded)
        self.assertFalse(attack(state, actor.id).time_advanced)

    def test_local_relics_do_not_mutate_an_unvisited_ashore_process(self):
        from jomon.actions import use_gear
        from jomon.inventory import record_acquisition
        self.begin("flooded-hold")
        state = self.state
        spindle = create_item(state, "relic:ebbglass spindle", "deck test", location="ground")
        self.assertTrue(auto_place(state, spindle.id, "pack", owner_id=state.active_courier_id))
        record_acquisition(state, spindle)
        state.carried_relic = "ebbglass spindle"
        before = copy.deepcopy(state.region)
        time = state.world_time
        self.assertFalse(use_gear(state).time_advanced)
        self.assertEqual(state.world_time, time)
        self.assertEqual(spindle.location, "pack")
        filament = create_item(state, "relic:stillwater filament", "deck test", location="ground")
        self.assertTrue(auto_place(state, filament.id, "pack", owner_id=state.active_courier_id))
        record_acquisition(state, filament)
        state.carried_relic = "stillwater filament"
        self.assertTrue(use_gear(state).time_advanced)
        self.assertFalse(any(cell.water for cell in state.vessel_materials.values()))
        self.assertEqual(state.region, before)
        self.assertEqual(state.voyage_status, "resolved")

    def test_fire_water_and_smoke_reach_crew_actors_and_vertical_openings(self):
        self.begin("raiders")
        state = self.state
        state.position = Position(14, 10, 0)
        actor = state.vessel_threats[0]
        actor.position = Position(14, 10, -1)
        state.vessel_materials["14,10,-1"] = MaterialCell(material="timber", smoke=4, fire=1, fuel=3)
        health = actor.health
        advance_materials(state)
        self.assertLess(actor.health, health)
        self.assertGreater(state.vessel_materials["14,10,0"].smoke, 0)
        state.vessel_materials["14,10,-1"].water = 2
        advance_materials(state)
        self.assertEqual(state.vessel_materials["14,10,-1"].fire, 0)
        person = state.household[4]
        schedule = state.actor_schedules[person.id]
        schedule.area, schedule.position = "vessel:-1", Position(16, 10, -1)
        _expose(state, schedule.position, "debris", 2)
        self.assertIn("legs", person.injuries)
        self.assertTrue(person.alive)

    def test_work_is_physical_costed_and_cancellable(self):
        state = self.state
        state.vessel_integrity = 5
        state.vessel_cargo["timber"] = CommodityStack(1, "sound")
        before = state.to_dict()
        self.assertFalse(work(state, "repair")[0])
        self.assertEqual(state.to_dict(), before)
        state.position = Position(20, 15, -1)
        before = state.to_dict()
        _handle_overlay_view(state, OverlayView("ship-work:repair"), InputEvent("key", 27))
        self.assertEqual(state.to_dict(), before)
        self.assertTrue(work(state, "repair")[0])
        self.assertEqual(state.vessel_integrity, 8)
        self.assertNotIn("timber", state.vessel_cargo)
        state.position = Position(8, 5)
        state.courier.health = 5
        self.assertTrue(work(state, "meal")[0])
        self.assertEqual(state.courier.health, 7)

    def test_bilge_pumping_preserves_items_and_actual_fire(self):
        state = self.state
        state.position = Position(8, 15, -1)
        state.vessel_materials["9,15,-1"] = MaterialCell(material="timber", water=3)
        item = create_item(state, "passive:cork float", "bilge survival", location="ground")
        item.region_id, item.ground_position = "jomon", Position(9, 15, -1)
        self.assertTrue(work(state, "pump")[0])
        self.assertEqual(state.vessel_materials["9,15,-1"].water, 0)
        self.assertEqual(item.location, "ground")

    def test_death_and_succession_do_not_drop_possessions_ashore(self):
        self.begin("raiders")
        state = self.state
        parent = equipped_item(state, "readied")
        state.position = Position(35, 11)
        previous = state.active_courier_id
        quest = copy.deepcopy(state.questlines)
        state.courier.health, state.courier.injury = 1, "deep cut"
        apply_damage(state, 5, "A declared boarding strike", location="torso")
        self.assertNotEqual(state.active_courier_id, previous)
        self.assertEqual(parent.region_id, "jomon")
        self.assertEqual(parent.ground_position, Position(35, 11))
        self.assertEqual(state.questlines, quest)
        self.assertEqual(game_state_from_dict(state.to_dict()).world_ended, False)

    def test_voyage_choices_fit_both_terminal_sizes(self):
        for family in VOYAGES:
            state = copy.deepcopy(self.base)
            choose_destination(state, "reed-anchor", forced_voyage=family)
            for height, width in ((24, 80), (32, 100)):
                view = OverlayView("voyage")
                _draw_dialogue_overlay(PanelSink(height, width), state, view)
                self.assertEqual(len(view.option_rows), len(dialogue_choices(state, "voyage")))

    def test_smoke_on_deck_uses_hazard_colour_not_merchant_colour(self):
        from unittest.mock import patch
        from jomon.terminal import _COLOUR_ATTRIBUTES, _draw_map
        state = self.state
        state.position = Position(8, 6)
        state.vessel_materials["9,5,0"] = MaterialCell(smoke=3)
        sink = PanelSink(32, 100)
        attributes = {}
        def write(y, x, text, count, attr=0):
            attributes[y, x] = text[:count], attr
        sink.addnstr = write
        with patch.dict(_COLOUR_ATTRIBUTES, {"hazard": 777, "interactable": 333}):
            _draw_map(sink, state, 0, 0, 24, 66)
        self.assertEqual(attributes[6, 10], ("s", 777))

    def test_all_new_families_are_obtainable_from_recorded_route_conditions(self):
        found = set()
        state = self.state
        state.vessel_integrity = 6
        state.route_current_node = "greywash"
        for voyage in range(1, 600):
            state.travel_count = voyage
            event = voyage_for(state, "frostmere")
            if event:
                found.add(event)
        self.assertEqual(found, set(VOYAGES))

    def test_corrupt_vessel_geometry_and_actor_state_are_rejected(self):
        self.begin("boarders")
        data = self.state.to_dict()
        data["vessel_threats"][0]["position"]["x"] = 100
        with self.assertRaises(StateError):
            game_state_from_dict(data)
        data = self.state.to_dict()
        data["vessel_tiles"] = {"-1,3,0": "/"}
        with self.assertRaises(StateError):
            game_state_from_dict(data)
