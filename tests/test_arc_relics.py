from __future__ import annotations

import copy
import unittest

from jomon.actions import attack, move, use_gear
from jomon.arc_relics import ARC_RELICS, ARC_RELIC_DESCRIPTIONS, lee_sheltered, validate_arc_relics
from jomon.enemy_equipment import issue_enemy_equipment, readied_weapon
from jomon.inventory import auto_place, create_item, sync_legacy_load
from jomon.state import MaterialCell, Position, Threat, create_world, game_state_from_dict
from jomon.world import sight_radius


class AftermathArcRelicTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("aftermath arc relic behavior")

    def setUp(self):
        self.state = copy.deepcopy(self.base)
        self.state.location = "region"
        self.state.position = Position(40, 24)
        self.state.threats.clear()
        self.state.weather = "clear"
        self.state.region.materials.clear()
        self.state.water.clear()
        for y in range(18, 31):
            for x in range(32, 53):
                self.state.region.tile_changes[f"{x},{y},0"] = "."

    def carry(self, name: str):
        item = create_item(self.state, f"relic:{name}", "focused aftermath arc relic")
        self.assertTrue(auto_place(self.state, item.id, "pack", owner_id=self.state.active_courier_id))
        self.state.relics[name] = 1
        self.state.carried_relic = name
        sync_legacy_load(self.state)
        self.state.carried_relic = name
        return item

    def hostile(self, identifier="claimant", position=Position(46, 24)):
        actor = Threat(identifier, "armed aftermath claimant", "pursuer", position, 12, 12, status="engaged", morale=6)
        self.state.threats.append(actor)
        issue_enemy_equipment(self.state, actor, "hearthford")
        return actor

    def test_manifest_has_four_distinct_arc_endings_and_descriptions(self):
        validate_arc_relics()
        self.assertEqual(len(ARC_RELICS), 4)
        self.assertEqual(len(set(ARC_RELICS.values())), 4)
        self.assertEqual(set(ARC_RELICS.values()), set(ARC_RELIC_DESCRIPTIONS))

    def test_common_work_rivet_repairs_items_and_support_with_fatigue(self):
        self.carry("common-work rivet")
        self.hostile()
        worn = next(item for item in self.state.items if item.location == "readied" and item.owner_id == self.state.active_courier_id)
        worn.condition = 50
        cell = self.state.region.materials["41,24,0"] = MaterialCell(material="timber", support=1, collapse_due=10)
        result = use_gear(self.state)
        self.assertTrue(result.time_advanced)
        self.assertEqual(worn.condition, 70)
        self.assertGreaterEqual(cell.support, 2)
        self.assertEqual(cell.collapse_due, 0)
        self.assertIn("fatigued", self.state.terrain_statuses)
        self.assertNotIn("common-work rivet", self.state.relics)

    def test_lodestone_disarms_both_sides_into_one_recoverable_pile(self):
        self.carry("counterclaim lodestone")
        actor = self.hostile(position=Position(44, 24))
        enemy_weapon = readied_weapon(self.state, actor)
        own_weapon = next(item for item in self.state.items if item.location == "readied" and item.owner_id == self.state.active_courier_id)
        origin = self.state.position
        result = use_gear(self.state)
        self.assertTrue(result.time_advanced)
        self.assertIsNone(self.state.weapon)
        self.assertEqual((enemy_weapon.location, enemy_weapon.ground_position), ("ground", origin))
        self.assertEqual((own_weapon.location, own_weapon.ground_position), ("ground", origin))
        self.assertLess(actor.morale, 6)

    def test_lee_brooch_protects_sight_movement_and_prepared_bow_for_eight_actions(self):
        self.carry("lee-cloth brooch")
        target = self.hostile(position=Position(47, 24))
        self.state.weather = "hard rain"
        baseline = sight_radius(self.state)
        used = use_gear(self.state)
        self.assertTrue(used.time_advanced)
        self.assertTrue(lee_sheltered(self.state))
        self.assertGreater(sight_radius(self.state), baseline)

        self.state.region.tile_changes["41,24,0"] = "."
        before = self.state.world_time
        move(self.state, 1, 0)
        self.assertEqual(self.state.world_time, before + 1)

        self.state.weapon = "longbow"
        readied = next(item for item in self.state.items if item.location == "readied" and item.owner_id == self.state.active_courier_id)
        readied.kind = "longbow"
        ammunition = create_item(self.state, "consumable:fletched arrows", "lee bow test", quantity=2)
        self.assertTrue(auto_place(self.state, ammunition.id, "pack", owner_id=self.state.active_courier_id))
        sync_legacy_load(self.state)
        self.state.aimed_target = target.id
        before_health = target.health
        result = attack(self.state, target.id)
        self.assertNotIn("spoils", result.message)
        self.assertLess(target.health, before_health)

        loaded = game_state_from_dict(self.state.to_dict())
        self.assertEqual(loaded.to_dict(), self.state.to_dict())

    def test_channel_shuttle_crosses_real_water_and_leaves_heaviest_cargo(self):
        self.carry("channel-surety shuttle")
        self.hostile(position=Position(48, 24))
        for x in (41, 42):
            self.state.water[f"{x},24,0"] = 1
        lot = create_item(self.state, "commodity:timber", "channel surety cargo", quantity=2)
        self.assertTrue(auto_place(self.state, lot.id, "pack", owner_id=self.state.active_courier_id))
        sync_legacy_load(self.state)
        origin = self.state.position
        result = use_gear(self.state)
        self.assertTrue(result.time_advanced)
        self.assertEqual(self.state.position, Position(43, 24))
        self.assertEqual((lot.location, lot.ground_position), ("ground", origin))
        self.assertFalse(lee_sheltered(self.state))


if __name__ == "__main__":
    unittest.main()
