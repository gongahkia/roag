import copy
import unittest

from jomon.actions import _open_container, attack, guard, move, use_gear
from jomon.content import PASSIVES
from jomon.frontiers import FRONTIER_DISCOVERIES, FRONTIERS, build_frontier
from jomon.inventory import (
    auto_place, create_item, sync_legacy_load, terrain_status_for,
)
from jomon.materials import affect_body, ensure_cell, handle_material, key
from jomon.regional_history import account_for, deliver_dependency
from jomon.state import Position, Threat, create_world


NEW_DISCOVERIES = {
    "fen sledge", "ice awl", "fire rake tooth", "limewash seal",
    "smoke braid", "salvage tally", "counterbrace pin", "pitch cup",
    "shingle skids", "signal mirror", "market weights",
}


class ExpandedDiscoveryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("expanded discovery regression")

    def setUp(self):
        self.state = copy.deepcopy(self.base)
        self.state.location = "region"
        self.state.position = Position(40, 25)
        self.state.threats = []
        self.state.region_threats["hearthford"] = self.state.threats
        for z in (0, 1):
            for y in range(22, 29):
                for x in range(37, 50):
                    self.state.region.tile_changes[f"{x},{y},{z}"] = "."
        for item in self.state.items:
            if item.owner_id == self.state.active_courier_id and item.location in {
                "pack", "readied", "secondary",
            }:
                item.location, item.owner_id = "lost", None
        sync_legacy_load(self.state)

    def carry(self, passive):
        item = create_item(
            self.state, f"passive:{passive}", "focused discovery test",
            owner_id=self.state.active_courier_id,
        )
        self.assertTrue(auto_place(
            self.state, item.id, "pack", owner_id=self.state.active_courier_id
        ))
        sync_legacy_load(self.state)

    def hostile(self, point=Position(41, 25), *, profile="pursuer", role="interceptor"):
        threat = Threat(
            f"target-{len(self.state.threats)}", "measured target", profile,
            point, 12, 12, status="engaged", morale=8, role=role,
            home_position=point,
        )
        self.state.threats.append(threat)
        return threat

    def test_fen_sledge_and_ice_awl_prevent_their_specific_footing_states(self):
        self.carry("fen sledge")
        self.assertIsNone(terrain_status_for(self.state, "m"))
        self.state.carried_passives = {"ice awl": 1}
        self.assertIsNone(terrain_status_for(self.state, "_"))

    def test_ice_awl_and_fire_rake_use_the_shared_material_reducer(self):
        point = Position(41, 25)
        self.carry("ice awl")
        cell = ensure_cell(self.state, point)
        cell.water, cell.ice = 1, True
        changed, message = handle_material(self.state, "break", point)
        self.assertTrue(changed)
        self.assertIn("break", message)
        self.assertFalse(cell.ice)

        self.setUp()
        self.carry("fire rake tooth")
        cell = ensure_cell(self.state, point)
        cell.material, cell.fire, cell.fuel = "timber", 2, 5
        self.assertTrue(handle_material(self.state, "break", point)[0])
        self.assertEqual(cell.material, "ash")
        self.assertEqual(cell.fire, 0)
        self.assertGreater(cell.smoke, 0)

    def test_pitch_cup_measures_one_long_ignition_without_lamp_oil(self):
        self.carry("pitch cup")
        point = Position(41, 25)
        cell = ensure_cell(self.state, point)
        cell.material = "reeds"
        self.state.lamp_oil = 0
        self.assertTrue(handle_material(self.state, "ignite", point)[0])
        self.assertEqual(self.state.lamp_oil, 0)
        self.assertTrue(self.state.vessel_changes[f"pitch_cup:{self.state.expedition_count}"])
        self.assertGreaterEqual(cell.fuel, 6)
        second = ensure_cell(self.state, Position(40, 26))
        second.material = "reeds"
        self.assertFalse(handle_material(self.state, "ignite", Position(40, 26))[0])

    def test_limewash_seal_protects_body_and_carried_weapon(self):
        self.carry("limewash seal")
        weapon = create_item(
            self.state, "billhook", "focused lime test", location="readied",
            owner_id=self.state.active_courier_id,
        )
        affect_body(self.state, self.state.courier, "lime", 2, self.state.position)
        affect_body(self.state, weapon, "lime", 2, self.state.position)
        self.assertNotIn("lime-grit", self.state.terrain_statuses)
        self.assertEqual(weapon.condition, 100)

    def test_smoke_braid_adds_visible_close_harm_and_morale_pressure(self):
        self.carry("smoke braid")
        create_item(
            self.state, "cudgel", "focused smoke test", location="readied",
            owner_id=self.state.active_courier_id,
        )
        sync_legacy_load(self.state)
        target = self.hostile()
        self.state.smoke[key(self.state.position)] = 4
        result = attack(self.state, target.id)
        self.assertIn("smoke braid adds one hidden harm", result.message)
        self.assertEqual(target.health, 10)
        self.assertLessEqual(target.morale, 5)

    def test_salvage_tally_records_one_difficult_recovery_per_region(self):
        self.carry("salvage tally")
        container = next(cache for cache in self.state.region.containers if cache.requirement)
        self.state.position = container.position
        self.state.gear = "repair tools" if container.requirement == "key" else "rope"
        self.state.lamp_oil = 3
        before = self.state.trade_credit
        result = _open_container(self.state)
        self.assertIn("salvage tally", result.message)
        self.assertEqual(self.state.trade_credit, before + 1)
        self.assertTrue(self.state.vessel_changes["salvage_tally:hearthford"])

    def test_counterbrace_pin_repairs_the_guarded_cell_before_reaction(self):
        self.carry("counterbrace pin")
        self.hostile(Position(43, 25))
        cell = ensure_cell(self.state, self.state.position)
        cell.material, cell.support = "timber", 0
        cell.collapse_due = self.state.world_time + 1
        result = guard(self.state)
        self.assertIn("counterbrace pin restores one support", result.message)
        self.assertEqual(cell.support, 1)
        self.assertEqual(cell.collapse_due, 0)

    def test_shingle_skids_remove_released_water_delay(self):
        self.carry("shingle skids")
        target = Position(41, 25)
        self.state.water[key(target)] = 3
        before = self.state.world_time
        result = move(self.state, 1, 0)
        self.assertTrue(result.time_advanced)
        self.assertEqual(self.state.world_time, before + 1)

    def test_signal_mirror_interrupts_aim_but_shares_last_known_position(self):
        self.carry("signal mirror")
        self.state.position = Position(40, 25, 1)
        target = self.hostile(Position(44, 25, 1), profile="ranged", role="shooter")
        ally = self.hostile(Position(45, 25, 1), role="protector")
        target.group = ally.group = "watch-detail"
        target.aimed_at = self.state.position
        result = use_gear(self.state)
        self.assertIn("breaks one marked shot", result.message)
        self.assertIsNone(target.aimed_at)
        self.assertEqual(ally.last_known_position, self.state.position)

    def test_market_weights_change_named_account_stock_and_confidence(self):
        self.carry("market weights")
        institution = account_for(self.state)
        market = self.state.market[institution.dependency]
        market.stock = 0
        institution.confidence = 0
        lot = create_item(
            self.state, f"commodity:{institution.dependency}", "focused delivery",
            owner_id=self.state.active_courier_id,
        )
        self.assertTrue(auto_place(
            self.state, lot.id, "pack", owner_id=self.state.active_courier_id
        ))
        sync_legacy_load(self.state)
        changed, message = deliver_dependency(self.state)
        self.assertTrue(changed)
        self.assertIn("additional stock", message)
        self.assertEqual(market.stock, 3)
        self.assertEqual(institution.confidence, 2)

    def test_all_new_discoveries_have_frontier_production_and_active_text(self):
        placed = set()
        for region_id in FRONTIERS:
            region = build_frontier("expanded discovery sources", region_id)
            placed.update(
                reward for container in region.containers
                for reward in container.extra_rewards
            )
        self.assertEqual(len(PASSIVES), 48)
        self.assertEqual(set().union(*map(set, FRONTIER_DISCOVERIES.values())), NEW_DISCOVERIES)
        self.assertTrue(NEW_DISCOVERIES <= placed)
        for name in NEW_DISCOVERIES:
            self.assertTrue(PASSIVES[name][1])


if __name__ == "__main__":
    unittest.main()
