import copy
import unittest

from jomon.actions import _advance_world
from jomon.materials import (
    MAX_CELLS, TURN_BUDGET, advance_materials, affect_body, ensure_cell,
    handle_material, inspect_material, key, validate_materials,
)
from jomon.state import MaterialCell, Position, StateError, Threat, VerticalLink, create_world, game_state_from_dict


class MaterialTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.template = create_world("material regression")

    def setUp(self):
        self.state = copy.deepcopy(self.template)
        self.state.location = "region"
        self.state.position = Position(40, 24)
        self.state.threats = []
        self.state.region_threats["hearthford"] = self.state.threats
        self.state.weather = "clear"
        for z in (-1, 0, 1):
            for y in range(20, 29):
                for x in range(35, 46):
                    self.state.region.tile_changes[f"{x},{y},{z}"] = "."

    def test_inspection_invalid_action_and_cancel_are_exactly_zero_time(self):
        state = self.state
        before = state.to_dict()
        self.assertIn("FACT", inspect_material(state, state.position)[0])
        self.assertFalse(handle_material(state, "ignite", Position(80, 40))[0])
        self.assertFalse(handle_material(state, "cut", state.position)[0])
        self.assertEqual(before, state.to_dict())

    def test_contextual_ignition_consumes_oil_and_advances_once(self):
        state = self.state
        point = Position(41, 24)
        state.region.materials[key(point)] = MaterialCell(material="reeds")
        old = state.world_time
        oil = state.lamp_oil
        self.assertTrue(handle_material(state, "ignite", point)[0])
        self.assertEqual(state.world_time, old + 1)
        self.assertEqual(state.lamp_oil, oil - 1)
        self.assertGreater(state.region.materials[key(point)].smoke, 0)

    def test_water_extinguishes_makes_mud_and_lime_reacts(self):
        state = self.state
        cell = MaterialCell(material="soil", water=1, fire=2, fuel=4)
        state.region.materials["41,24,0"] = cell
        lime = MaterialCell(material="lime", water=1)
        state.region.materials["42,24,0"] = lime
        advance_materials(state)
        self.assertEqual(cell.fire, 0)
        self.assertEqual(state.region.tile_changes["41,24,0"], "m")
        self.assertEqual(lime.coating, "lime")
        self.assertGreater(lime.smoke, 0)

    def test_structural_bracing_does_not_also_quench_fuel(self):
        state = self.state
        state.position = Position(40, 24, -1)
        point = Position(41, 24, -1)
        cell = MaterialCell(material="charcoal", support=0, fire=1, fuel=8, collapse_due=2)
        state.region.materials[key(point)] = cell
        self.assertTrue(handle_material(state, "brace", point)[0])
        self.assertEqual((cell.support, cell.collapse_due), (3, 0))
        self.assertEqual(cell.fire, 1)
        self.assertGreater(cell.smoke, 0)

    def test_rain_reduces_fire_and_wets_sparse_soil(self):
        state = self.state
        state.world_time = 5
        state.weather = "hard rain"
        fire = MaterialCell(material="cloth", fire=1, fuel=5)
        soil = MaterialCell()
        state.region.materials.update({"41,24,0": fire, "42,24,0": soil})
        advance_materials(state)
        self.assertEqual(fire.fire, 0)
        self.assertEqual(fire.coating, "wet")
        self.assertEqual(soil.water, 1)

    def test_fire_spreads_only_into_dry_fuel_and_smoke_follows_wind(self):
        state = self.state
        state.region.materials["40,24,0"] = MaterialCell(material="timber", fire=2, fuel=4)
        for x in (39, 41):
            state.region.materials[f"{x},24,0"] = MaterialCell(material="reeds")
        advance_materials(state)
        self.assertTrue(any(state.region.materials[f"{x},24,0"].fire for x in (39, 41)))
        self.assertTrue(any(state.region.materials[f"{x},24,0"].smoke for x in (39, 41)))

    def test_water_falls_and_smoke_rises_through_aligned_openings(self):
        state = self.state
        lower, middle, upper = Position(41, 24, -1), Position(41, 24), Position(41, 24, 1)
        state.region.vertical_links += [VerticalLink(lower, middle, "culvert"), VerticalLink(middle, upper, "shaft")]
        state.region.materials[key(middle)] = MaterialCell(water=2, smoke=3)
        advance_materials(state)
        self.assertGreater(state.region.materials[key(lower)].water, 0)
        self.assertGreater(state.region.materials[key(upper)].smoke, 0)

    def test_season_freezes_then_thaws_only_during_actions(self):
        state = self.state
        state.calendar_origin_day = 72
        cell = MaterialCell(water=1)
        state.region.materials["41,24,0"] = cell
        advance_materials(state)
        self.assertTrue(cell.ice)
        state.calendar_origin_day = 0
        self.assertTrue(cell.ice)
        advance_materials(state)
        self.assertFalse(cell.ice)

    def test_collapse_telegraphs_then_changes_floor_and_can_be_braced(self):
        state = self.state
        point = Position(41, 24, 1)
        cell = MaterialCell(material="timber", support=0)
        state.region.materials[key(point)] = cell
        advance_materials(state)
        self.assertEqual(cell.collapse_due, 2)
        self.assertNotEqual(state.region.tile_changes[key(point)], "O")
        state.world_time = 2
        advance_materials(state)
        self.assertEqual(state.region.tile_changes[key(point)], "O")
        state.position = Position(40, 24, 1)
        state.weapon = "billhook"
        cell.support, cell.collapse_due = 0, 5
        self.assertTrue(handle_material(state, "brace", point)[0])
        self.assertEqual(cell.collapse_due, 0)

    def test_shared_exposure_changes_actors_items_and_nonfatal_offduty_people(self):
        state = self.state
        actor = Threat("burned", "burner", "pursuer", state.position, 1, 4)
        affect_body(state, actor, "fire", 1, state.position)
        self.assertEqual(actor.status, "defeated")
        person = state.household[1]
        affect_body(state, person, "debris", 99, state.position)
        self.assertTrue(person.alive)
        self.assertEqual(person.health, 2)
        self.assertIn("legs", person.injuries)
        item = state.items[0]
        condition = item.condition
        affect_body(state, item, "fire", 1, state.position)
        self.assertLess(item.condition, condition)

    def test_dirty_cell_work_is_bounded_and_replay_deterministic(self):
        state = self.state
        for y in range(20, 28):
            for x in range(35, 45):
                state.region.materials[f"{x},{y},0"] = MaterialCell(smoke=2)
        other = copy.deepcopy(state)
        for _ in range(3):
            self.assertLessEqual(advance_materials(state), TURN_BUDGET)
            advance_materials(other)
        self.assertEqual(state.to_dict(), other.to_dict())
        self.assertLessEqual(len(state.region.materials), MAX_CELLS)
        validate_materials(state)

    def test_mid_reaction_roundtrip_and_v6_preserves_exact_old_possessions(self):
        original = self.template.to_dict()
        original["save_format"] = 6
        original.pop("vessel_materials")
        for region in original["regions"].values():
            region.pop("materials")
            region.pop("material_cursor")
        first = game_state_from_dict(original)
        self.assertEqual(first.to_dict(), game_state_from_dict(original).to_dict())
        self.assertEqual(first.to_dict()["items"], original["items"])
        for region_id in original["regions"]:
            self.assertEqual(first.to_dict()["regions"][region_id]["levels"], original["regions"][region_id]["levels"])
        first.region.materials["41,24,0"] = MaterialCell(material="timber", fire=2, fuel=3, collapse_due=4)
        self.assertEqual(game_state_from_dict(first.to_dict()).to_dict(), first.to_dict())

    def test_corrupt_material_fields_are_rejected(self):
        data = self.template.to_dict()
        data["regions"]["hearthford"]["materials"]["500,500,0"] = {"fire": 999}
        with self.assertRaises(StateError):
            game_state_from_dict(data)


if __name__ == "__main__":
    unittest.main()
