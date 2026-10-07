from __future__ import annotations

import inspect
import unittest

import jomon.actions as action_module
import jomon.inventory as inventory_module
import jomon.world as world_module
from jomon.actions import attack, merchant_stock_for, move, use_gear
from jomon.content import PASSIVES
from jomon.inventory import (
    armour_mobility,
    armour_noise,
    auto_place,
    create_item,
    protection_at,
    sync_legacy_load,
    terrain_status_for,
)
from jomon.regions import activate_region
from jomon.state import Position, Threat, create_world
from jomon.world import position_key, sight_radius


def expedition_state(seed: str = "activated build content"):
    state = create_world(seed)
    state.active_courier_id = state.household[0].id
    state.location = "region"
    state.position = Position(40, 25)
    state.region.tile_changes[position_key(state.position)] = "."
    state.threats = []
    return state


class ExistingContentActivationTests(unittest.TestCase):
    def test_every_retained_passive_is_referenced_by_production_resolution(self):
        production = "\n".join(
            inspect.getsource(module)
            for module in (action_module, inventory_module, world_module)
        )
        missing = [name for name in PASSIVES if repr(name) not in production and f'"{name}"' not in production]
        self.assertEqual(missing, [])

    def test_salt_smoke_scree_and_wind_discoveries_change_exploration(self):
        state = expedition_state()
        for item in state.items:
            if item.owner_id == state.active_courier_id and item.location == "head":
                item.location, item.owner_id = "locker", None
        ordinary_smoke = terrain_status_for(state, "s")
        self.assertEqual(ordinary_smoke[2], 4)

        state.carried_passives = {"charcoal mask": 1}
        self.assertEqual(terrain_status_for(state, "s")[2], 2)
        state.carried_passives = {"salt veil": 1}
        self.assertIsNone(terrain_status_for(state, "s"))
        self.assertIsNone(terrain_status_for(state, ":"))
        state.carried_passives = {"limestone cleat": 1}
        self.assertIsNone(terrain_status_for(state, "r"))
        self.assertIsNone(terrain_status_for(state, "q"))

        state.carried_passives = {}
        state.courier.technique = "wind listener"
        state.active_region_id = "greenwold"
        state.weather = "crosswind"
        with_wind_listener = sight_radius(state)
        state.courier.technique = "quiet passage"
        self.assertEqual(with_wind_listener, sight_radius(state) + 2)

    def test_scree_step_and_ebb_reader_change_region_traversal(self):
        state = expedition_state("regional recruit traversal")
        state.courier.technique = "scree step"
        self.assertIsNone(terrain_status_for(state, "r"))
        self.assertIsNone(terrain_status_for(state, "q"))

        activate_region(state, "greywash")
        state.location = "region"
        state.position = Position(40, 25)
        target = Position(41, 25)
        state.region.tile_changes[position_key(state.position)] = "."
        state.region.tile_changes[position_key(target)] = "."
        state.water[position_key(target)] = 5
        state.courier.technique = "ebb reader"
        before = state.world_time
        move(state, 1, 0)
        self.assertEqual(state.world_time - before, 1)

    def test_cast_bind_high_arc_and_green_poultice_are_active_techniques(self):
        state = expedition_state("active recruit combat")
        for x in range(40, 53):
            state.region.tile_changes[f"{x},25,0"] = "."
        target = Threat(
            "test-target", "test ward", "reach", Position(43, 25),
            6, 6, "engaged", "holds", morale=5,
        )
        state.threats = [target]
        readied = next(
            item for item in state.items
            if item.owner_id == state.active_courier_id and item.location == "readied"
        )
        readied.kind = "weighted net"
        state.weapon = "weighted net"
        state.courier.technique = "cast bind"
        net = create_item(
            state, "consumable:casting net bundle", "technique test",
            owner_id=state.active_courier_id,
        )
        self.assertTrue(auto_place(state, net.id, "pack", owner_id=state.active_courier_id))
        sync_legacy_load(state)
        state.weapon = "weighted net"
        before_gap = abs(target.position.x - state.position.x)
        attack(state)
        self.assertLess(abs(target.position.x - state.position.x), before_gap)
        self.assertIn("Cast Bind", state.messages[-1])

        state = expedition_state("active high arc")
        for x in range(40, 53):
            state.region.tile_changes[f"{x},25,0"] = "."
        state.courier.technique = "high arc"
        readied = next(
            item for item in state.items
            if item.owner_id == state.active_courier_id and item.location == "readied"
        )
        readied.kind = "sling"
        state.weapon = "sling"
        target = Threat(
            "far-target", "far ward", "ranged", Position(50, 25),
            4, 4, "engaged", "aims", morale=4,
        )
        state.threats = [target]
        shot = create_item(
            state, "consumable:sling shot pouch", "technique test",
            owner_id=state.active_courier_id,
        )
        self.assertTrue(auto_place(state, shot.id, "pack", owner_id=state.active_courier_id))
        sync_legacy_load(state)
        state.weapon = "sling"
        result = attack(state)
        self.assertTrue(result.time_advanced)
        self.assertLess(target.health, target.max_health)

        state = expedition_state("active green poultice")
        state.courier.technique = "green poultice"
        state.courier.health = 4
        state.courier.injuries = {"arms": "deep arm cut"}
        dressing = create_item(
            state, "consumable:pine resin dressing", "technique test",
            owner_id=state.active_courier_id,
        )
        self.assertTrue(auto_place(state, dressing.id, "pack", owner_id=state.active_courier_id))
        sync_legacy_load(state)
        result = use_gear(state)
        self.assertTrue(result.time_advanced)
        self.assertNotIn("arms", state.courier.injuries)
        self.assertEqual(state.courier.health, 8)

    def test_armour_coverage_noise_and_mobility_change_resolution(self):
        state = expedition_state("active armour fields")
        owner = state.active_courier_id
        for item in state.items:
            if item.owner_id == owner and item.location in {"torso", "arms", "legs"}:
                item.location, item.owner_id = "locker", None
        create_item(state, "linen sleeves", "coverage test", owner_id=owner, location="arms")
        state.guarded_step = False
        self.assertEqual(protection_at(state, "arms", "cut")[0], 0)
        state.guarded_step = True
        self.assertEqual(protection_at(state, "arms", "cut")[0], 1)

        create_item(state, "riveted coat", "noise test", owner_id=owner, location="torso")
        create_item(state, "brigandine cuisses", "mobility test", owner_id=owner, location="legs")
        self.assertGreaterEqual(armour_noise(state), 2)
        self.assertGreaterEqual(armour_mobility(state), 4)
        state.guarded_step = False
        state.pressure_elapsed = 0
        target = Position(41, 25)
        state.region.tile_changes[position_key(target)] = "r"
        before_time, before_noise = state.world_time, state.noise
        move(state, 1, 0)
        self.assertEqual(state.world_time - before_time, 2)
        self.assertGreater(state.noise, before_noise)

    def test_heavy_crossbow_and_pike_have_normal_merchant_sources(self):
        state = create_world("working weapon sources")
        activate_region(state, "whitecairn")
        state.objective_status = "accepted"
        self.assertIn("pike", merchant_stock_for(state))
        state.objective_status = "completed"
        self.assertIn("heavy crossbow", merchant_stock_for(state))


if __name__ == "__main__":
    unittest.main()
