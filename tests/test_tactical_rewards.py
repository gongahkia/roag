from __future__ import annotations

import unittest

from jomon.actions import _control_interaction, attack, guard, move, use_gear
from jomon.content import PASSIVES, RELICS, WEAPONS
from jomon.inventory import (
    AMMUNITION_ITEMS,
    auto_place,
    create_item,
    physical_ammunition,
    sync_legacy_load,
    weight_capacity,
)
from jomon.state import Position, Threat, create_world
from jomon.world import build_combinations, capacity, position_key


def armed_state(weapon: str, ammunition: str | None = None):
    state = create_world(f"weapon production {weapon}")
    state.location = "region"
    state.position = Position(40, 25)
    for x in range(38, 53):
        state.region.tile_changes[f"{x},25,0"] = "."
    for item in state.items:
        if item.owner_id == state.active_courier_id and item.location in {"readied", "pack"}:
            item.location, item.owner_id = "lost", None
    create_item(
        state, weapon, "focused weapon test", location="readied",
        owner_id=state.active_courier_id,
    )
    if ammunition:
        item = create_item(
            state, AMMUNITION_ITEMS[ammunition], "focused ammunition test",
            owner_id=state.active_courier_id, quantity=4,
        )
        if not auto_place(state, item.id, "pack", owner_id=state.active_courier_id):
            raise AssertionError("test ammunition did not fit")
    sync_legacy_load(state)
    state.threats = [Threat(
        "target", "measured target", "pursuer", Position(46, 25),
        12, 12, status="watching", morale=8, home_position=Position(46, 25),
    )]
    return state


class TacticalRewardTests(unittest.TestCase):
    def test_four_new_weapons_change_actions_and_physical_resources(self):
        sling = armed_state("staff sling", "sling stones")
        before = physical_ammunition(sling, "sling stones")
        cast = attack(sling, "target")
        self.assertIn("arcs over low cover", cast.message)
        self.assertEqual(physical_ammunition(sling, "sling stones"), before - 1)

        hooked = armed_state("hooked javelin", "javelins")
        hooked.gear = "rope"
        reel = create_item(
            hooked, "passive:gullbone reel", "focused passive test",
            owner_id=hooked.active_courier_id,
        )
        self.assertTrue(auto_place(hooked, reel.id, "pack", owner_id=hooked.active_courier_id))
        sync_legacy_load(hooked)
        hooked.gear = "rope"
        before = physical_ammunition(hooked, "javelins")
        thrown = attack(hooked, "target")
        self.assertIn("recover it immediately", thrown.message)
        self.assertEqual(hooked.threats[0].position, Position(45, 25))
        self.assertEqual(physical_ammunition(hooked, "javelins"), before)

        spear = armed_state("boar spear")
        spear.threats[0].position = Position(43, 25)
        spear.threats[0].profile = "animal"
        braced = attack(spear, "target")
        self.assertIn("pins the approach", braced.message)

        gonne = armed_state("handgonne", "handgonne charges")
        before = physical_ammunition(gonne, "handgonne charges")
        aimed = attack(gonne, "target")
        self.assertIn("prepare handgonne", aimed.message)
        fired = attack(gonne, "target")
        self.assertIn("powder smoke", fired.message)
        self.assertTrue(gonne.smoke)
        self.assertEqual(physical_ammunition(gonne, "handgonne charges"), before - 1)
        self.assertIn("loading 1/2", guard(gonne).message.lower())

    def test_new_passives_and_relic_have_observable_production_hooks(self):
        state = armed_state("staff sling", "sling stones")
        state.carried_passives = {"sighting knot": 1}
        self.assertIn("surveyed sling lane", build_combinations(state))

        base_bulk, base_weight = capacity(state), weight_capacity(state)
        state.carried_passives["load ledger"] = 1
        self.assertEqual(capacity(state), base_bulk + 2)
        self.assertEqual(weight_capacity(state), base_weight + 4)

        state.carried_passives = {"roof nail": 1}
        state.position = Position(40, 25, 1)
        state.region.tile_changes[position_key(state.position)] = "."
        state.region.tile_changes[position_key(Position(41, 25, 1))] = "."
        state.aimed_target = "target"
        moved = move(state, 1, 0)
        self.assertEqual(state.aimed_target, "target")
        self.assertIn("roof nail", moved.message)

        state.carried_passives = {"cache bell": 1}
        state.position = state.region.landmarks["landing"]
        sounded = use_gear(state)
        self.assertTrue(sounded.time_advanced)
        self.assertTrue(state.treasure_marks["hearthford"])
        self.assertGreaterEqual(state.noise, 1)

        state.carried_passives = {"sluice token": 1}
        state.position = state.region.landmarks["objective"]
        state.noise = 0
        controlled = _control_interaction(state)
        self.assertTrue(controlled.time_advanced)
        self.assertEqual(state.noise, 0)

        relic = create_item(
            state, "relic:stillwater filament", "test relic",
            owner_id=state.active_courier_id,
        )
        self.assertTrue(auto_place(state, relic.id, "pack", owner_id=state.active_courier_id))
        state.relics["stillwater filament"] = 1
        state.carried_relic = "stillwater filament"
        state.water[position_key(state.position)] = 4
        used = use_gear(state)
        self.assertIn("arrests every local current", used.message)
        self.assertFalse(state.water)
        self.assertNotIn("stillwater filament", state.relics)

    def test_new_content_is_bounded_and_every_reward_is_defined(self):
        self.assertEqual(len(WEAPONS), 24)
        self.assertEqual(len(PASSIVES), 37)
        self.assertEqual(len(RELICS), 6)
        state = create_world("new quest caches")
        self.assertEqual(
            {region_id: len(region.containers) for region_id, region in state.regions.items()},
            {"hearthford": 9, "greywash": 7, "greenwold": 7, "whitecairn": 7},
        )
        placed = {
            reward
            for region in state.regions.values()
            for container in region.containers
            for reward in (container.reward, *container.extra_rewards)
        }
        self.assertTrue({
            "boar spear", "staff sling", "hooked javelin", "handgonne",
            "stillwater filament", "scar salve recipe", "gullbone reel",
        } <= placed)


if __name__ == "__main__":
    unittest.main()
