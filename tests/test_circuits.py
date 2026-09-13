from __future__ import annotations

import unittest

from jomon.circuits import (
    CELL_CHARGE, advance_circuits, cell_at, cell_key, glyph, item_count,
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
        state.world_time = 5
        self.tick()
        self.assertEqual(rack.charge, CELL_CHARGE - 1)
        self.assertEqual(rack.phase, "head")

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
        self.assertEqual(next_phase(state, relay)[0], "head")
        rear.phase, front.phase = "wire", "head"
        self.assertEqual(next_phase(state, relay)[0], "wire")
        relay.phase, front.phase = "head", "wire"
        self.assertEqual(next_phase(state, front)[0], "head")
        self.assertEqual(next_phase(state, rear)[0], "wire")
        self.assertIn("faces east", " ".join(diagnostic_lines(state, relay)))

    def test_counter_passes_every_chosen_input_pulse(self):
        state = self.state
        trace = self.fit("trace", 20)
        counter = self.fit("counter", 21)
        trace.phase = "head"
        self.tick()
        self.assertEqual((counter.phase, counter.count), ("wire", 1))
        trace.phase = "head"
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


if __name__ == "__main__":
    unittest.main()
