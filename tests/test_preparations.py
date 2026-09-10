from __future__ import annotations

import copy
import unittest

from jomon.actions import use_gear
from jomon.aftermath import AFTERMATH_TOPOLOGIES
from jomon.inventory import auto_place, create_item, item_spec, sync_legacy_load
from jomon.preparations import (
    PREPARATIONS,
    TOPOLOGY_PREPARATION,
    apply_preparation,
    carried_preparations,
    preparation_status,
    validate_preparations,
)
from jomon.state import MaterialCell, Position, TerrainStatus, Threat, create_world, game_state_from_dict
from jomon.terminal import _handle_overlay, _overlay_lines, dialogue_choices


class FinitePreparationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("aftermath field preparations")

    def state_for(self, name: str):
        state = copy.deepcopy(self.base)
        state.location = "region"
        state.position = Position(40, 24)
        state.threats.clear()
        state.weather = "clear"
        state.region.materials.clear()
        state.water.clear()
        state.smoke.clear()
        for y in range(19, 30):
            for x in range(34, 48):
                state.region.tile_changes[f"{x},{y},0"] = "."
        item = create_item(state, f"consumable:{name}", "focused preparation test")
        self.assertTrue(auto_place(state, item.id, "pack", owner_id=state.active_courier_id))
        sync_legacy_load(state)
        self.assertIn(name, carried_preparations(state))
        return state

    def use(self, state, name: str):
        changed, message = apply_preparation(state, name)
        self.assertTrue(changed, message)
        self.assertIn("physical preparation is spent", message)
        self.assertNotIn(name, state.consumables)
        return message

    def test_sixteen_topologies_have_distinct_inspectable_physical_results(self):
        validate_preparations()
        expected = {topology for pair in AFTERMATH_TOPOLOGIES.values() for topology in pair}
        self.assertEqual(len(PREPARATIONS), 16)
        self.assertEqual(set(TOPOLOGY_PREPARATION), expected)
        self.assertEqual(len({entry.mode for entry in PREPARATIONS.values()}), 16)
        for name, entry in PREPARATIONS.items():
            self.assertEqual(item_spec(f"consumable:{name}").description, entry.description)

    def test_water_weapon_light_and_ground_recovery_are_physical(self):
        water = self.state_for("race-gate chalk")
        for x in range(40, 44):
            water.region.materials[f"{x},24,0"] = MaterialCell(water=2)
        before = sum(cell.water for cell in water.region.materials.values())
        self.use(water, "race-gate chalk")
        self.assertEqual(sum(cell.water for cell in water.region.materials.values()), before - 3)

        wrap = self.state_for("tallow gear wrap")
        weapon = next(item for item in wrap.items if item.location == "readied" and item.owner_id == wrap.active_courier_id)
        weapon.condition = 55
        self.use(wrap, "tallow gear wrap")
        self.assertEqual(weapon.condition, 85)

        wick = self.state_for("storm wick")
        wick.lamp_oil = 4
        wick.smoke["40,24,0"] = wick.smoke["41,24,0"] = 4
        self.use(wick, "storm wick")
        self.assertEqual(wick.lamp_oil, 6)
        self.assertFalse(wick.smoke)

        sling = self.state_for("wreck cork sling")
        loose = create_item(sling, "consumable:brine wash", "loose wreck lot", location="ground")
        loose.region_id, loose.ground_position = sling.spatial_id, Position(43, 24)
        self.use(sling, "wreck cork sling")
        self.assertEqual((loose.location, loose.owner_id), ("pack", sling.active_courier_id))

    def test_fire_footing_and_sounding_preparations_change_world_and_ai(self):
        blanket = self.state_for("damp ember blanket")
        blanket.region.materials["41,24,0"] = MaterialCell(material="timber", fire=2, fuel=6)
        burning = Threat("burning", "burning obstruction", "pursuer", Position(41, 25), 9, 9, status="engaged", conditions={"burning": 3})
        blanket.threats = [burning]
        self.use(blanket, "damp ember blanket")
        self.assertEqual(blanket.region.materials["41,24,0"].fire, 0)
        self.assertNotIn("burning", burning.conditions)

        brand = self.state_for("resin firebrand")
        brand.region.materials["41,24,0"] = MaterialCell(material="reeds")
        watcher = Threat("watcher", "watching line", "ranged", Position(47, 24), 9, 9, status="watching")
        brand.threats = [watcher]
        oil = brand.lamp_oil
        self.use(brand, "resin firebrand")
        self.assertEqual(brand.region.materials["41,24,0"].fire, 2)
        self.assertEqual(brand.lamp_oil, oil)
        self.assertEqual(watcher.status, "engaged")

        binding = self.state_for("scree binding")
        binding.terrain_statuses["poor-footing"] = TerrainStatus("scree", 3, "weak guard")
        self.use(binding, "scree binding")
        self.assertNotIn("poor-footing", binding.terrain_statuses)
        self.assertTrue(binding.guarded_step)

        cord = self.state_for("stair sounding cord")
        cord.position = Position(40, 24, 1)
        self.use(cord, "stair sounding cord")
        self.assertTrue(cord.treasure_marks["hearthford"])
        self.assertEqual(cord.noise, 3)

    def test_banks_rations_bridges_and_mufflers_change_shared_state(self):
        plug = self.state_for("peat bank plug")
        plug.region.materials["41,24,0"] = MaterialCell(material="soil", water=2, support=1, collapse_due=9)
        self.use(plug, "peat bank plug")
        cell = plug.region.materials["41,24,0"]
        self.assertEqual((cell.water, cell.support, cell.collapse_due), (1, 2, 0))

        ration = self.state_for("fen ration cake")
        ration.courier.health -= 2
        ration.terrain_statuses["fatigued"] = TerrainStatus("load", 3, "slow")
        before = ration.courier.health
        self.use(ration, "fen ration cake")
        self.assertEqual(ration.courier.health, before + 1)
        self.assertNotIn("fatigued", ration.terrain_statuses)

        dogs = self.state_for("bridge dog pair")
        dogs.region.materials["41,24,0"] = MaterialCell(material="timber", support=1, collapse_due=8)
        dogs.region.materials["42,24,0"] = MaterialCell(material="stone", support=0, collapse_due=9)
        self.use(dogs, "bridge dog pair")
        self.assertEqual([dogs.region.materials[f"{x},24,0"].support for x in (41, 42)], [3, 3])
        self.assertEqual(dogs.noise, 4)

        muffler = self.state_for("echo muffler")
        target = Threat("aim", "prepared shooter", "ranged", Position(45, 24), 9, 9, status="engaged", aimed_at=muffler.position)
        muffler.threats, muffler.noise = [target], 5
        self.use(muffler, "echo muffler")
        self.assertIsNone(target.aimed_at)
        self.assertEqual(muffler.noise, 2)

    def test_drain_sand_ice_and_thaw_are_bounded_material_tools(self):
        drain = self.state_for("fired drainage tile")
        drain.region.tile_changes["41,24,0"] = "m"
        drain.region.materials["41,24,0"] = MaterialCell(material="soil", water=2)
        self.use(drain, "fired drainage tile")
        self.assertEqual(drain.region.tile_changes["41,24,0"], ".")
        self.assertEqual(drain.region.materials["41,24,0"].water, 0)

        sand = self.state_for("kiln sand pouch")
        sand.region.materials["41,24,0"] = MaterialCell(material="timber", fire=2, fuel=7)
        self.use(sand, "kiln sand pouch")
        self.assertEqual((sand.region.materials["41,24,0"].fire, sand.region.materials["41,24,0"].fuel), (0, 4))
        self.assertEqual(sand.region.materials["41,24,0"].smoke, 2)

        peg = self.state_for("marked ice peg")
        peg.calendar_origin_day = 72
        peg.region.materials["41,24,0"] = MaterialCell(water=2, fluid="fresh")
        self.use(peg, "marked ice peg")
        self.assertTrue(peg.region.materials["41,24,0"].ice)

        thaw = self.state_for("thaw kettle sachet")
        thaw.lamp_oil = 2
        coordinates = ("41,24,0", "42,24,0", "40,25,0")
        for coordinate in coordinates:
            thaw.region.materials[coordinate] = MaterialCell(water=1, ice=True)
        thaw.terrain_statuses["chilled"] = TerrainStatus("ice", 3, "short aim")
        self.use(thaw, "thaw kettle sachet")
        self.assertTrue(all(not thaw.region.materials[coordinate].ice for coordinate in coordinates))
        self.assertEqual(thaw.lamp_oil, 1)
        self.assertNotIn("chilled", thaw.terrain_statuses)

    def test_contextual_overlay_previews_and_commits_exact_selected_supply(self):
        state = self.state_for("storm wick")
        second = create_item(state, "consumable:echo muffler", "second option")
        self.assertTrue(auto_place(state, second.id, "pack", owner_id=state.active_courier_id))
        sync_legacy_load(state)
        state.lamp_oil = 5
        title, lines = _overlay_lines(state, "field-use")
        self.assertEqual(title, "SELECT CONTEXTUAL FIELD USE")
        self.assertIn("READY", " ".join(lines))
        self.assertEqual(len(dialogue_choices(state, "field-use")), 3)
        before = state.world_time
        closed, _ = _handle_overlay(state, "field-use", ord("1"))
        self.assertIsNone(closed)
        self.assertEqual(state.world_time, before + 1)
        self.assertNotIn("storm wick", state.consumables)

    def test_successful_use_round_trips_result_and_invalid_use_is_zero_time(self):
        state = self.state_for("tallow gear wrap")
        before = state.to_dict()
        result = use_gear(state, "tallow gear wrap")
        self.assertFalse(result.time_advanced)
        self.assertEqual(state.world_time, before["world_time"])
        self.assertIn("tallow gear wrap", state.consumables)
        weapon = next(item for item in state.items if item.location == "readied" and item.owner_id == state.active_courier_id)
        weapon.condition = 40
        self.assertTrue(use_gear(state, "tallow gear wrap").time_advanced)
        loaded = game_state_from_dict(state.to_dict())
        self.assertEqual(loaded.to_dict(), state.to_dict())


if __name__ == "__main__":
    unittest.main()
