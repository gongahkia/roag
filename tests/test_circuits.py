from __future__ import annotations

import unittest

from jomon.circuits import (
    CELL_CHARGE, SIGNAL_SPAN, advance_circuits, cell_at, cell_key, glyph, item_count,
    diagnostic_lines, next_phase, operate, piston_head_at, place, reclaim,
    sensor_active, validate_circuits,
)
from jomon.inventory import auto_place, create_item, item_spec
from jomon.production import RECIPES, make, recipe_status
from jomon.state import CircuitCell, CommodityStack, MaterialCell, Position, StateError, VerticalLink, create_world, game_state_from_dict
from jomon.terminal import CircuitView, InputEvent, _draw_circuit, _handle_circuit
from jomon.world import displayed_tile, is_walkable, sight_radius


class CircuitTests(unittest.TestCase):
    def setUp(self):
        self.state = create_world("circuit tests")
        self.state.location = "region"
        self.state.position = Position(20, 20, 0)
        for y in range(18, 23):
            for x in range(18, 27):
                self.state.region.tile_changes[f"{x},{y},0"] = "."

    def fit(self, kind: str, x: int, *, y: int = 20, layer: str = "surface") -> CircuitCell:
        point = Position(x, y, 0)
        cell = CircuitCell("region:hearthford", point, layer, kind)
        self.state.circuits[cell_key(cell.space, point, layer)] = cell
        return cell

    def tick(self, count: int = 1) -> None:
        for _ in range(count):
            self.state.world_time += 1
            advance_circuits(self.state)

    def test_crafted_parts_place_and_reclaim_from_two_independent_layers(self):
        state = self.state
        self.assertEqual(RECIPES["circuit-trace"].quantity, 4)
        self.assertEqual(item_spec("circuit:trace").stack_limit, 8)
        item = create_item(state, "circuit:trace", "test stock", quantity=2)
        self.assertTrue(auto_place(state, item.id, "pack", owner_id=state.active_courier_id))
        point = Position(21, 20, 0)
        self.assertTrue(place(state, point, "surface", "trace")[0])
        self.assertTrue(place(state, point, "buried", "trace")[0])
        self.assertEqual(item_count(state, "trace"), 0)
        self.assertIsNotNone(cell_at(state, point, "buried"))
        self.assertEqual(displayed_tile(state, point), ":")
        self.assertTrue(reclaim(state, point, "buried")[0])
        self.assertIsNone(cell_at(state, point, "buried"))
        self.assertEqual(item_count(state, "trace"), 1)
        self.assertFalse(place(state, point, "surface", "trace")[0])

    def test_workshop_recipe_consumes_real_stock_and_yields_four_traces(self):
        state = self.state
        state.location = "jomon"
        state.jomon_space = "vessel"
        state.position = Position(39, 5, -1)
        state.vessel_cargo["ironwork"] = CommodityStack(1, "dry")
        state.vessel_cargo["wool"] = CommodityStack(1, "dry")
        self.assertTrue(recipe_status(state, "circuit-trace")[0])
        self.assertTrue(make(state, "circuit-trace")[0])
        self.assertEqual(item_count(state, "trace"), 4)
        self.assertNotIn("ironwork", state.vessel_cargo)
        self.assertNotIn("wool", state.vessel_cargo)

    def test_buried_trace_under_wall_is_hidden_and_vias_bridge_layers(self):
        state = self.state
        state.region.tile_changes["22,20,0"] = "#"
        self.fit("rack", 20).charge = 1
        self.fit("via", 21)
        self.fit("trace", 21, layer="buried")
        self.fit("trace", 22, layer="buried")
        self.fit("via", 23, layer="buried")
        lamp = self.fit("lamp", 23)
        self.assertEqual(displayed_tile(state, Position(22, 20, 0)), "#")
        state.world_time = 5
        self.tick(7)
        self.assertGreater(lamp.active_until, 0)
        self.assertEqual(glyph(state, lamp.position), "*")

    def test_switch_blocks_pulses_and_gate_affects_pathing(self):
        state = self.state
        rack = self.fit("rack", 20)
        rack.charge = 3
        switch = self.fit("switch", 21)
        switch.enabled = False
        self.fit("trace", 22)
        gate = self.fit("gate", 23)
        self.assertFalse(is_walkable(state, gate.position))
        state.world_time = 5
        self.tick(9)
        self.assertFalse(is_walkable(state, gate.position))
        state.position = Position(20, 19, 0)
        self.assertTrue(operate(state, switch.position, "surface")[0])
        self.assertTrue(switch.enabled)
        state.world_time = 17
        self.tick(4)
        self.assertTrue(is_walkable(state, gate.position))
        self.assertTrue(operate(state, switch.position, "surface")[0])
        self.tick(8)
        self.assertFalse(is_walkable(state, gate.position))

    def test_paired_vias_carry_a_pulse_through_a_real_vertical_link(self):
        state = self.state
        lower = Position(21, 20, 0)
        upper = Position(21, 20, 1)
        state.region.vertical_links.append(VerticalLink(lower, upper, "test riser"))
        self.fit("rack", 20).charge = 1
        self.fit("via", 21)
        state.circuits[cell_key("region:hearthford", upper, "surface")] = CircuitCell(
            "region:hearthford", upper, "surface", "via"
        )
        lamp_position = Position(22, 20, 1)
        lamp = CircuitCell("region:hearthford", lamp_position, "surface", "lamp")
        state.circuits[cell_key(lamp.space, lamp_position, lamp.layer)] = lamp
        state.world_time = 5
        self.tick(4)
        self.assertTrue(lamp.active_until >= state.world_time)

    def test_lamp_extends_sight_and_drain_clears_only_local_water(self):
        state = self.state
        lamp = self.fit("lamp", 21)
        drain = self.fit("drain", 23)
        base = sight_radius(state)
        lamp.active_until = state.world_time + 3
        self.assertEqual(sight_radius(state), base + 4)
        self.tick(4)
        self.assertEqual(sight_radius(state), base)
        state.water["23,20,0"] = 3
        state.water["25,20,0"] = 3
        drain.phase = "head"
        drain.active_until = 0
        self.tick()
        self.assertIn("23,20,0", state.water)
        rack = self.fit("rack", 22, y=19)
        rack.charge = 1
        state.world_time = 5
        self.tick(2)
        self.assertNotIn("23,20,0", state.water)
        self.assertIn("25,20,0", state.water)

    def test_battery_is_finite_and_reload_uses_a_carried_cell(self):
        state = self.state
        rack = self.fit("rack", 21)
        self.assertEqual(rack.charge, 0)
        cell = create_item(state, "circuit:cell", "test stock")
        self.assertTrue(auto_place(state, cell.id, "pack", owner_id=state.active_courier_id))
        self.assertTrue(operate(state, rack.position, "surface")[0])
        self.assertEqual(rack.charge, CELL_CHARGE)
        self.assertEqual(item_count(state, "cell"), 0)
        self.fit("lamp", 22)
        state.world_time = 5
        self.tick()
        self.assertEqual(rack.charge, CELL_CHARGE - 1)
        self.assertEqual(rack.phase, "head")

    def test_idle_rack_conserves_charge_and_diagnostic_names_blocking_switch(self):
        state = self.state
        rack = self.fit("rack", 20)
        rack.charge = 3
        switch = self.fit("switch", 21)
        switch.enabled = False
        lamp = self.fit("lamp", 22)
        self.tick(18)
        self.assertEqual(rack.charge, 3)
        self.assertIn("Source waiting", " ".join(diagnostic_lines(state, rack)))
        self.assertIn("Knife switch at 21,20,z+0 is open", " ".join(diagnostic_lines(state, lamp)))
        switch.enabled = True
        self.tick(8)
        self.assertEqual(rack.charge, 2)
        self.assertGreater(lamp.last_pulse, 0)

    def test_one_finite_pulse_can_work_two_devices_without_a_second_power_grid(self):
        state = self.state
        rack = self.fit("rack", 20)
        rack.charge = 1
        first = self.fit("lamp", 21)
        second = self.fit("lamp", 20, y=21)
        state.world_time = 5
        self.tick(2)
        self.assertEqual(rack.charge, 0)
        self.assertTrue(all(lamp.active_until >= state.world_time for lamp in (first, second)))

    def test_diagnostic_names_the_actual_block_after_a_closed_switch(self):
        state = self.state
        rack = self.fit("rack", 20)
        rack.charge = 2
        self.fit("switch", 21)
        sensor = self.fit("sensor", 22)
        sensor.mode = "water"
        self.fit("lamp", 23)
        self.assertIn("Water sensor at 22,20,z+0 is clear", " ".join(diagnostic_lines(state, rack)))

    def test_source_free_feedback_expires_after_finite_signal_span(self):
        state = self.state
        state.circuits = {}
        points = [(x, 0) for x in range(4)] + [(3, y) for y in range(1, 4)]
        points += [(x, 3) for x in range(2, -1, -1)] + [(0, y) for y in range(2, 0, -1)]
        traces = [self.fit("trace", x + 20, y=y + 19) for x, y in points]
        traces[0].phase, traces[0].signal_steps = "head", SIGNAL_SPAN
        traces[-1].phase = "tail"
        lamp = self.fit("lamp", 22, y=18)
        self.tick(SIGNAL_SPAN + 12)
        self.assertGreater(lamp.last_pulse, 0)
        self.assertFalse(any(cell.phase == "head" for cell in state.circuits.values()))
        self.assertEqual(sum(cell.kind == "rack" for cell in state.circuits.values()), 0)
        self.assertLess(lamp.last_pulse, state.world_time - 7)

    def test_bilge_watch_waits_dry_then_pumps_a_real_flooded_hold(self):
        from jomon.actions import _advance_world
        from jomon.ship_crises import begin_deck, crisis_lines

        state = self.state
        rack = state.circuits[cell_key("vessel", Position(5, 15, -1), "surface")]
        pump = state.circuits[cell_key("vessel", Position(10, 15, -1), "surface")]
        sensor = state.circuits[cell_key("vessel", Position(9, 15, -1), "buried")]
        self.assertEqual((sensor.kind, sensor.mode, sensor.threshold, rack.charge),
                         ("sensor", "water", 1, CELL_CHARGE))
        self.tick(30)
        self.assertEqual(rack.charge, CELL_CHARGE)
        self.assertIn("Water sensor at 9,15,z-1 is clear", " ".join(diagnostic_lines(state, rack)))
        state.location, state.jomon_space = "jomon", "vessel"
        state.position = Position(8, 14, -1)
        state.voyage_kind, state.voyage_status = "flooded-hold", "active"
        self.assertTrue(begin_deck(state)[0])
        self.assertTrue(any("bilge pump" in line for line in crisis_lines(state)))
        for _ in range(30):
            _advance_world(state)
            if state.voyage_status == "resolved":
                break
        self.assertEqual(state.voyage_status, "resolved")
        self.assertLess(rack.charge, CELL_CHARGE)
        self.assertIn("drained", pump.last_event)
        self.assertLessEqual(sum(cell.water for cell in state.vessel_materials.values()), 1)

    def test_bilge_switch_reserves_charge_and_surge_has_same_physical_counterplay(self):
        from jomon.actions import _advance_world
        from jomon.ship_crises import begin_deck

        state = self.state
        state.location, state.jomon_space = "jomon", "vessel"
        state.position = Position(7, 15, -1)
        state.voyage_kind, state.voyage_status = "flooded-hold", "active"
        state.vessel_changes["active_voyage_variant"] = "thaw-surge"
        rack = state.circuits[cell_key("vessel", Position(5, 15, -1), "surface")]
        switch = state.circuits[cell_key("vessel", Position(6, 15, -1), "surface")]
        self.assertTrue(operate(state, switch.position, "surface")[0])
        self.assertFalse(switch.enabled)
        self.assertTrue(begin_deck(state)[0])
        for _ in range(8):
            _advance_world(state)
        self.assertEqual(rack.charge, CELL_CHARGE)
        self.assertEqual(state.voyage_status, "active")
        self.assertTrue(operate(state, switch.position, "surface")[0])
        for _ in range(36):
            _advance_world(state)
            if state.voyage_status == "resolved":
                break
        self.assertEqual(state.voyage_status, "resolved")
        self.assertLess(rack.charge, CELL_CHARGE)

    def test_format_thirteen_inflight_pulse_migrates_with_one_step(self):
        state = self.state
        trace = self.fit("trace", 21)
        trace.phase, trace.signal_steps = "head", SIGNAL_SPAN
        old = state.to_dict()
        old["save_format"] = 13
        for cell in old["circuits"].values():
            cell.pop("signal_steps")
        loaded = game_state_from_dict(old)
        self.assertEqual(loaded.circuits[cell_key(trace.space, trace.position, trace.layer)].signal_steps, 1)
        old["circuits"] = {key: cell for key, cell in old["circuits"].items() if not key.startswith("vessel/")}
        self.assertFalse(any(cell.space == "vessel" for cell in game_state_from_dict(old).circuits.values()))
        old["circuits"] = []
        with self.assertRaises(StateError):
            game_state_from_dict(old)
        invalid = state.to_dict()
        invalid["circuits"][cell_key(trace.space, trace.position, trace.layer)]["signal_steps"] = SIGNAL_SPAN + 1
        with self.assertRaises(StateError):
            game_state_from_dict(invalid)

    def test_save_round_trip_and_format_eleven_migration_validate(self):
        state = self.state
        self.fit("trace", 21, layer="buried")
        loaded = game_state_from_dict(state.to_dict())
        self.assertIsNotNone(cell_at(loaded, Position(21, 20, 0), "buried"))
        old = state.to_dict()
        old["save_format"] = 11
        del old["circuits"]
        migrated = game_state_from_dict(old)
        self.assertEqual(migrated.circuits, {})
        bad = state.to_dict()
        next(iter(bad["circuits"].values()))["kind"] = "free power"
        with self.assertRaises(StateError):
            game_state_from_dict(bad)
        validate_circuits(loaded)

    def test_circuit_view_changes_layer_and_acts_on_target(self):
        state = self.state
        part = create_item(state, "circuit:trace", "test stock")
        self.assertTrue(auto_place(state, part.id, "pack", owner_id=state.active_courier_id))
        view = CircuitView.begin(state)
        self.assertFalse(_handle_circuit(state, view, InputEvent("key", key=9)))
        self.assertEqual(view.layer, "buried")
        self.assertFalse(_handle_circuit(state, view, InputEvent("key", key=ord("1"))))
        self.assertIsNotNone(cell_at(state, state.position, "buried"))
        class Screen:
            def __init__(self):
                self.writes = []

            def getmaxyx(self):
                return 30, 100

            def addnstr(self, row, col, value, count, attr=0):
                self.writes.append(value[:count])

            def refresh(self):
                pass

        screen = Screen()
        _draw_circuit(screen, state, view)
        self.assertIn(":", screen.writes)
        self.assertTrue(any("CIRCUITS BURIED" in row for row in screen.writes))
        self.assertTrue(_handle_circuit(state, view, InputEvent("key", key=27)))

    def test_authored_mill_relief_responds_to_a_courier_and_drains_material_water(self):
        state = self.state
        anchor = state.region.landmarks["mill"]
        sensor_point = Position(anchor.x - 2, anchor.y, anchor.z)
        drain_point = Position(anchor.x - 1, anchor.y, anchor.z)
        sensor = cell_at(state, sensor_point)
        self.assertEqual(sensor.kind, "sensor")
        self.assertEqual(cell_at(state, anchor).kind, "lamp")
        state.region.materials[f"{sensor_point.x},{sensor_point.y},0"] = MaterialCell(water=3)
        state.position = sensor_point
        state.world_time = 5
        self.tick(5)
        self.assertTrue(cell_at(state, anchor).active_until >= state.world_time)
        self.assertEqual(state.region.materials[f"{sensor_point.x},{sensor_point.y},0"].water, 1)
        self.assertIn("drained", cell_at(state, drain_point).last_event)

    def test_piston_pushes_three_crates_and_sticky_retraction_pulls_one(self):
        state = self.state
        piston = self.fit("piston", 20)
        piston.sticky = True
        self.fit("crate", 21)
        self.fit("crate", 22)
        self.fit("crate", 23)
        rack = self.fit("rack", 19)
        rack.charge = 1
        state.position = Position(20, 19, 0)
        state.world_time = 5
        self.tick(2)
        self.assertEqual(piston.last_event, "extended east; pushed 3 crates")
        self.assertIsNotNone(piston_head_at(state, Position(21, 20, 0)))
        self.assertFalse(is_walkable(state, Position(21, 20, 0)))
        self.assertEqual([cell_at(state, Position(x, 20, 0)).kind for x in (22, 23, 24)], ["crate"] * 3)
        loaded = game_state_from_dict(state.to_dict())
        self.assertEqual(loaded.circuits[cell_key("region:hearthford", Position(20, 20, 0), "surface")].sticky, True)
        self.assertEqual(loaded.circuits[cell_key("region:hearthford", Position(24, 20, 0), "surface")].kind, "crate")
        self.tick(8)
        self.assertIsNone(piston_head_at(state, Position(21, 20, 0)))
        self.assertEqual(cell_at(state, Position(21, 20, 0)).kind, "crate")
        self.assertIn("pulled", piston.last_event)

    def test_jammed_piston_does_not_move_crate_or_crush_courier(self):
        state = self.state
        piston = self.fit("piston", 20)
        crate = self.fit("crate", 21)
        rack = self.fit("rack", 19)
        rack.charge = 1
        state.position = Position(20, 19, 0)
        state.region.tile_changes["22,20,0"] = "#"
        state.world_time = 5
        self.tick(2)
        self.assertEqual(crate.position, Position(21, 20, 0))
        self.assertIn("jammed: solid terrain", piston.last_event)
        self.assertEqual(piston.active_until, 0)
        state.region.tile_changes["22,20,0"] = "."
        state.position = Position(22, 20, 0)
        piston.phase = "wire"
        rack.charge = 1
        state.world_time = 11
        self.tick(2)
        self.assertEqual(crate.position, Position(21, 20, 0))
        self.assertIn("person or creature", piston.last_event)

    def test_piston_moves_bounded_ground_cargo_with_a_crate(self):
        state = self.state
        piston = self.fit("piston", 20)
        crate = self.fit("crate", 21)
        self.fit("rack", 19).charge = 1
        cargo = create_item(state, "component:iron billet", "test freight", location="ground", quantity=2)
        cargo.region_id = state.active_region_id
        cargo.ground_position = crate.position
        self.assertIn("6/12 kg", diagnostic_lines(state, crate)[0])
        state.position = Position(20, 19, 0)
        state.world_time = 5
        self.tick(2)
        self.assertEqual((crate.position, cargo.ground_position), (Position(22, 20, 0),) * 2)
        self.assertIn("pushed 1 crate", piston.last_event)

    def test_overloaded_crate_jams_without_moving_contents(self):
        state = self.state
        piston = self.fit("piston", 20)
        crate = self.fit("crate", 21)
        self.fit("rack", 19).charge = 1
        for quantity in (4, 1):
            cargo = create_item(state, "component:iron billet", "test heavy freight", location="ground", quantity=quantity)
            cargo.region_id = state.active_region_id
            cargo.ground_position = crate.position
        state.position = Position(20, 19, 0)
        state.world_time = 5
        self.tick(2)
        self.assertEqual(crate.position, Position(21, 20, 0))
        self.assertIn("exceeds 12 kg", piston.last_event)

    def test_sensor_modes_and_one_way_relay_are_inspectable(self):
        state = self.state
        sensor = self.fit("sensor", 21, layer="buried")
        sensor.mode = "water"
        sensor.threshold = 2
        self.assertFalse(sensor_active(state, sensor))
        state.region.materials["21,20,0"] = MaterialCell(water=2)
        self.assertTrue(sensor_active(state, sensor))
        self.assertTrue(operate(state, sensor.position, "buried")[0])
        self.assertEqual(sensor.mode, "threat")
        self.assertTrue(operate(state, sensor.position, "buried", "secondary")[0])
        self.assertEqual(sensor.threshold, 3)
        relay = self.fit("relay", 24)
        rear = self.fit("trace", 23)
        front = self.fit("trace", 25)
        rear.phase = "head"
        rear.signal_steps = 2
        self.assertEqual(next_phase(state, relay)[0], "head")
        rear.phase, front.phase = "wire", "head"
        rear.signal_steps, front.signal_steps = 0, 2
        self.assertEqual(next_phase(state, relay)[0], "wire")
        relay.phase, front.phase = "head", "wire"
        relay.signal_steps, front.signal_steps = 2, 0
        self.assertEqual(next_phase(state, front)[0], "head")
        self.assertEqual(next_phase(state, rear)[0], "wire")
        self.assertIn("faces east", " ".join(diagnostic_lines(state, relay)))

    def test_counter_passes_every_chosen_input_pulse(self):
        state = self.state
        trace = self.fit("trace", 20)
        counter = self.fit("counter", 21)
        trace.phase = "head"
        trace.signal_steps = 2
        self.tick()
        self.assertEqual((counter.phase, counter.count), ("wire", 1))
        trace.phase = "head"
        trace.signal_steps = 2
        self.tick()
        self.assertEqual((counter.phase, counter.count), ("head", 0))
        self.assertEqual(counter.last_pulse, state.world_time)
        self.assertTrue(operate(state, counter.position, "surface")[0])
        self.assertEqual(counter.threshold, 3)

    def test_format_twelve_circuits_gain_default_settings_without_loss(self):
        state = self.state
        old = state.to_dict()
        old["save_format"] = 12
        for cell in old["circuits"].values():
            for field in ("facing", "mode", "threshold", "count", "sticky", "last_pulse", "last_event"):
                cell.pop(field)
        loaded = game_state_from_dict(old)
        self.assertEqual(loaded.save_format, state.save_format)
        self.assertEqual(len(loaded.circuits), len(state.circuits))
        self.assertTrue(all(cell.facing == "east" for cell in loaded.circuits.values()))
        invalid = state.to_dict()
        next(iter(invalid["circuits"].values()))["facing"] = "upside-down"
        with self.assertRaises(StateError):
            game_state_from_dict(invalid)

    def test_circuit_view_builds_configures_and_steps_a_piston(self):
        state = self.state
        part = create_item(state, "circuit:piston", "test stock")
        self.assertTrue(auto_place(state, part.id, "pack", owner_id=state.active_courier_id))
        view = CircuitView(Position(21, 20, 0))
        before = state.world_time
        self.assertFalse(_handle_circuit(state, view, InputEvent("key", key=ord("P"))))
        piston = cell_at(state, view.cursor)
        self.assertEqual(piston.kind, "piston")
        self.assertEqual(state.world_time, before + 1)
        self.assertFalse(_handle_circuit(state, view, InputEvent("key", key=ord("E"))))
        self.assertEqual(piston.facing, "south")
        self.assertFalse(_handle_circuit(state, view, InputEvent("key", key=ord("T"))))
        self.assertTrue(piston.sticky)
        stepped = state.world_time
        self.assertFalse(_handle_circuit(state, view, InputEvent("key", key=ord("."))))
        self.assertEqual(state.world_time, stepped + 1)


if __name__ == "__main__":
    unittest.main()
