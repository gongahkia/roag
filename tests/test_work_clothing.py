import copy
import unittest

from jomon.actions import apply_damage, effective_weapon_range, guard, merchant_stock_for, move, purchase_merchant_item
from jomon.content import MERCHANT_ITEMS
from jomon.frontiers import build_frontier
from jomon.inventory import (
    BODY_SLOTS, ITEM_SPECS, REGIONAL_ARMOUR, InventoryTransaction, armour_mobility,
    armour_noise, auto_place, create_item, equipped_item, load_state, pack_weight,
    protection_at, terrain_status_for, worn_tags,
)
from jomon.materials import advance_materials, affect_body
from jomon.state import MaterialCell, Position, TerrainStatus, Threat, create_world, game_state_from_dict
from jomon.terminal import OverlayView, _draw_dialogue_overlay
from jomon.world import sight_radius
from test_information_panels import PanelSink


class WorkingClothingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("physical work clothing")

    def setUp(self):
        self.state = copy.deepcopy(self.base)
        self.state.location, self.state.position = "region", Position(30, 24)
        self.state.threats.clear()
        self.state.weather = "clear"
        self.state.support = None
        for item in self.state.items:
            if item.owner_id == self.state.active_courier_id and item.location in BODY_SLOTS:
                item.location, item.owner_id = "ground", None
                item.region_id, item.ground_position = "hearthford", self.state.position
        for y in range(15, 35):
            for x in range(20, 45):
                self.state.region.tile_changes[f"{x},{y},0"] = "."

    def wear(self, kind):
        slot = ITEM_SPECS[kind].slot
        old = equipped_item(self.state, slot)
        if old:
            self.assertTrue(auto_place(self.state, old.id, "pack", owner_id=self.state.active_courier_id))
        return create_item(self.state, kind, "work-clothing scenario", location=slot, owner_id=self.state.active_courier_id)

    def test_all_eighteen_are_physical_sized_protective_and_persistent(self):
        names = set().union(*map(set, REGIONAL_ARMOUR.values()))
        self.assertEqual(len(names), 18)
        self.assertEqual(sum(spec.category == "armour" for spec in ITEM_SPECS.values()), 36)
        for kind in sorted(names):
            with self.subTest(kind=kind):
                self.setUp()
                state, spec = self.state, ITEM_SPECS[kind]
                before_weight = pack_weight(state)
                item = self.wear(kind)
                self.assertEqual(pack_weight(state) - before_weight, spec.weight)
                state.guarded_step = True
                for damage in ("cut", "pierce", "blunt"):
                    self.assertEqual(protection_at(state, spec.slot, damage)[0], getattr(spec, damage))
                damage = max(("cut", "pierce", "blunt"), key=lambda name: getattr(spec, name))
                health = state.courier.health
                apply_damage(state, 5, "declared work-clothing strike", damage_kind=damage, location=spec.slot)
                self.assertEqual(health - state.courier.health, 5 - min(4, getattr(spec, damage)))
                if getattr(spec, damage):
                    self.assertLess(item.condition, 100)
                loaded = game_state_from_dict(state.to_dict())
                self.assertEqual(next(i for i in loaded.items if i.id == item.id), item)
                self.assertTrue(auto_place(state, item.id, "pack", owner_id=state.active_courier_id))
                self.assertEqual(pack_weight(state) - before_weight, spec.weight)

    def test_fresh_frontier_containers_offer_every_work_clothing_family(self):
        found = set()
        for region_id, kinds in REGIONAL_ARMOUR.items():
            first = build_frontier("clothing sources", region_id)
            second = build_frontier("clothing sources", region_id)
            self.assertEqual(first.containers, second.containers)
            stock = {c.extra_rewards[0] for c in first.containers}
            self.assertEqual(stock, set(kinds))
            found |= stock
        self.assertEqual(found, set().union(*map(set, REGIONAL_ARMOUR.values())))

    def test_merchant_spare_is_seeded_region_specific_and_physically_purchased(self):
        state = self.state
        state.location, state.trade_credit, state.merchant_present = "jomon", 20, True
        seen = set()
        for region_id, choices in REGIONAL_ARMOUR.items():
            state.active_region_id = region_id
            for visit in range(100):
                state.returned_expeditions = visit
                stock = merchant_stock_for(state)
                self.assertEqual(stock, merchant_stock_for(state))
                self.assertIn(stock[-1], choices)
                self.assertLessEqual(len(stock), 4)
                seen.add(stock[-1])
        self.assertEqual(seen, set().union(*map(set, REGIONAL_ARMOUR.values())))
        state.active_region_id = "hearthford"
        for kind in sorted(seen):
            self.setUp()
            state = self.state
            state.location, state.trade_credit, state.merchant_present = "jomon", 20, True
            state.merchant_stock = [kind]
            self.assertTrue(purchase_merchant_item(state, kind).changed)
            bought = next(i for i in state.items if i.kind == kind)
            self.assertEqual(bought.location, "locker")
            self.assertEqual(state.trade_credit, 20 - MERCHANT_ITEMS[kind][0])
            self.assertEqual(state.merchant_stock, [])

    def test_saturated_padding_adds_load_but_waterproof_reed_does_not(self):
        state = self.state
        self.wear("frost leggings")
        self.wear("winter felt coat")
        self.wear("reed brim")
        before = pack_weight(state)
        state.terrain_statuses["wet"] = TerrainStatus("slurry", 5, "saturated padding adds weight")
        self.assertEqual(pack_weight(state) - before, 6)

    def test_buoyancy_has_a_weight_limit_and_does_not_grant_water_immunity(self):
        state = self.state
        self.wear("cork-backed coat")
        self.assertIn(load_state(state), {"light", "laden"})
        self.assertIsNone(terrain_status_for(state, "w"))
        cargo = create_item(state, "commodity:timber", "too much floating cargo", quantity=4)
        self.assertTrue(auto_place(state, cargo.id, "pack", owner_id=state.active_courier_id))
        self.assertEqual(terrain_status_for(state, "w")[0], "current")

    def test_quiet_pattens_change_actual_mud_noise_and_time(self):
        state = self.state
        state.region.tile_changes["31,24,0"] = "m"
        before = state.world_time
        ordinary = move(state, 1, 0)
        self.assertEqual(state.world_time - before, 2)
        self.assertIn("Mud drags", ordinary.message)
        self.setUp()
        state = self.state
        self.wear("peat pattens")
        state.region.tile_changes["31,24,0"] = "m"
        before = state.world_time
        protected = move(state, 1, 0)
        self.assertEqual(state.world_time - before, 1)
        self.assertNotIn("bogged", state.terrain_statuses)
        self.assertNotIn("Mud drags", protected.message)

    def test_visor_trades_point_protection_for_sight_and_ranged_range(self):
        state = self.state
        state.weapon = "longbow"
        sight, reach = sight_radius(state), effective_weapon_range(state)
        visor = self.wear("ridge visor")
        self.assertEqual(sight_radius(state), sight - 2)
        self.assertEqual(effective_weapon_range(state), reach - 2)
        self.assertEqual(protection_at(state, "head", "pierce")[0], 4)
        visor.condition = 0
        self.assertEqual(protection_at(state, "head", "pierce")[0], 0)
        self.assertEqual(effective_weapon_range(state), reach)

    def test_heatproof_cloth_wears_and_apron_only_protects_while_worn(self):
        state = self.state
        apron = self.wear("kiln apron")
        health = state.courier.health
        affect_body(state, apron, "fire", 1, state.position)
        self.assertEqual(apron.condition, 98)
        affect_body(state, state.courier, "fire", 1, state.position)
        self.assertEqual(state.courier.health, health)
        self.assertEqual(apron.condition, 92)
        self.assertTrue(auto_place(state, apron.id, "pack", owner_id=state.active_courier_id))
        affect_body(state, state.courier, "fire", 1, state.position)
        self.assertEqual(state.courier.health, health - 1)
        for kind in ("kiln face wrap", "potter mitts"):
            item = self.wear(kind)
            affect_body(state, item, "fire", 1, state.position)
            self.assertEqual(item.condition, 98)

    def test_lime_slurry_and_salt_affect_real_items_and_protected_bodies(self):
        state = self.state
        state.region.materials["30,24,0"] = MaterialCell(material="lime", water=1)
        weapon = equipped_item(state, "readied")
        advance_materials(state)
        self.assertIn("lime-grit", state.terrain_statuses)
        self.assertLess(weapon.condition, 100)
        state.terrain_statuses.clear()
        self.wear("quarry sleeves")
        affect_body(state, state.courier, "lime", 1, state.position)
        self.assertNotIn("lime-grit", state.terrain_statuses)
        mitts = self.wear("mail mitts")
        affect_body(state, mitts, "salt", 1, state.position)
        self.assertEqual(mitts.condition, 97)
        palms = self.wear("split-hide palms")
        affect_body(state, palms, "salt", 1, state.position)
        self.assertEqual(palms.condition, 100)

    def test_smoke_uses_the_status_consumed_by_range_and_guard(self):
        state = self.state
        state.weapon = "longbow"
        reach = effective_weapon_range(state)
        affect_body(state, state.courier, "smoke", 1, state.position)
        self.assertEqual(effective_weapon_range(state), reach - 2)
        state.terrain_statuses.clear()
        self.wear("kiln face wrap")
        affect_body(state, state.courier, "smoke", 1, state.position)
        self.assertNotIn("smoke-inhalation", state.terrain_statuses)

    def test_ice_cleats_change_actual_frozen_crossing_and_condition_matters(self):
        state = self.state
        self.assertEqual(terrain_status_for(state, "_")[0], "poor-footing")
        state.region.materials["31,24,0"] = MaterialCell(water=1, ice=True)
        move(state, 1, 0)
        self.assertIn("poor-footing", state.terrain_statuses)
        self.setUp()
        state = self.state
        cleats = self.wear("ice cleats")
        self.assertIsNone(terrain_status_for(state, "_"))
        self.assertIsNone(terrain_status_for(state, "r"))
        self.assertEqual(armour_noise(state), 1)
        state.region.materials["31,24,0"] = MaterialCell(water=1, ice=True)
        move(state, 1, 0)
        self.assertNotIn("poor-footing", state.terrain_statuses)
        cleats.condition = 0
        self.assertEqual(terrain_status_for(state, "_")[0], "poor-footing")

    def test_grip_and_bracing_change_guard_morale_not_just_descriptions(self):
        results = []
        for kind in (None, "archer tabs", "watch vambraces"):
            self.setUp()
            state = self.state
            if kind:
                self.wear(kind)
            state.weapon = "staff"
            state.terrain_statuses["wet"] = TerrainStatus("rain", 6, "wet grip")
            actor = Threat("guard-target", "closing ward", "pursuer", Position(34, 24), 20, 20, status="engaged", morale=10)
            state.threats.append(actor)
            guard(state)
            results.append(actor.morale)
        self.assertEqual(results, [10, 9, 9])
        self.setUp()
        self.wear("reed splints")
        actor = Threat("guard-target", "closing ward", "pursuer", Position(34, 24), 20, 20, status="engaged", morale=10)
        self.state.threats.append(actor)
        guard(self.state)
        self.assertEqual(actor.morale, 9)

    def test_slots_exposure_and_transaction_cancel_preserve_original_clothing(self):
        state = self.state
        self.wear("potter mitts")
        self.wear("quarry chaps")
        self.assertEqual(armour_mobility(state), 2)
        self.assertNotIn("limeproof", worn_tags(state, ("feet",)))
        before = state.to_dict()
        transaction = InventoryTransaction.begin(state)
        auto_place(state, equipped_item(state, "hands").id, "locker")
        transaction.cancel(state)
        self.assertEqual(state.to_dict(), before)

    def test_new_stock_choice_panel_fits_both_supported_sizes(self):
        state = self.state
        state.merchant_stock = ["winter felt coat", "ridge visor", "kiln apron", "cork-backed coat"]
        for height, width in ((24, 80), (32, 100)):
            _draw_dialogue_overlay(PanelSink(height, width), state, OverlayView("merchant"))

    def test_v6_load_does_not_replace_existing_container_contents_or_equipment(self):
        original = self.base.to_dict()
        original["save_format"] = 6
        loaded = game_state_from_dict(original)
        self.assertEqual(loaded.save_format, 7)
        self.assertEqual(loaded.items, self.base.items)
        self.assertEqual(loaded.region.containers, self.base.region.containers)


if __name__ == "__main__":
    unittest.main()
