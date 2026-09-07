from __future__ import annotations

import copy
import unittest

from jomon.actions import (
    _threat_action,
    apply_damage,
    attack,
    choose_gear,
    choose_support,
    choose_weapon,
    decide_objective,
    depart,
    interact,
    use_gear,
)
from jomon.inventory import (
    InventoryTransaction,
    auto_place,
    create_item,
    physical_ammunition,
    record_acquisition,
)
from jomon.regions import activate_region, reconstruct_regional_process
from jomon.state import Position, SAVE_FORMAT, Threat, create_world, game_state_from_dict
from jomon.world import JOMON_GANGPLANK


def expedition(seed: str = "format six integrity"):
    state = create_world(seed)
    choose_gear(state, "rope")
    choose_support(state, "route survey")
    state.position = JOMON_GANGPLANK
    depart(state)
    return state


class PhysicalStateIntegrityTests(unittest.TestCase):
    def test_inventory_cancel_restores_every_mutable_transaction_field(self):
        state = create_world("whole transaction")
        before = copy.deepcopy(state.to_dict())
        transaction = InventoryTransaction.begin(state)
        state.region.containers[0].item_ids.append("invented")
        state.items[0].location = "destroyed"
        state.ammunition_by_type["bolts"] = 999
        state.trade_credit = 17
        state.owned_weapons.append("heavy crossbow")
        state.owned_gear.clear()
        state.relics["river-glass ward"] = 9
        state.vessel_changes["transactional cost"] = True
        transaction.cancel(state)
        self.assertEqual(state.to_dict(), before)

    def test_objective_loss_is_recoverable_or_replaced_once(self):
        state = expedition("recover objective")
        state.position = state.region.landmarks["contact"]
        decide_objective(state, "accept")
        state.position = state.region.landmarks["objective"]
        self.assertTrue(interact(state).time_advanced)
        cargo = next(
            item for item in state.items
            if item.kind == f"commodity:{state.region.objective_commodity}"
            and item.owner_id == state.active_courier_id
        )
        cargo.location, cargo.owner_id = "ground", None
        cargo.region_id, cargo.ground_position = state.active_region_id, Position(30, 30)
        state.carried_goods.clear()
        result = interact(state)
        self.assertFalse(result.time_advanced)
        self.assertIn("30,30", result.message)
        cargo.location, cargo.region_id, cargo.ground_position = "lost", None, None
        replacement = interact(state)
        self.assertTrue(replacement.time_advanced)
        self.assertTrue(state.region.changes["objective_replacement_taken"])
        self.assertIn(state.region.objective_commodity, state.carried_goods)

    def test_death_leaves_exact_physical_load_at_defeat_site_for_successor(self):
        state = expedition("death reconciliation")
        state.position = Position(40, 25)
        dead_id = state.active_courier_id
        carried_ids = {
            item.id for item in state.items
            if item.owner_id == dead_id and item.location in {
                "pack", "readied", "secondary", "head", "torso", "arms",
                "hands", "legs", "feet",
            }
        }
        state.courier.health = 1
        state.courier.injury = "deep cut"
        apply_damage(state, 5, "A fatal measured blow", location="torso")
        self.assertNotEqual(state.active_courier_id, dead_id)
        for item in state.items:
            if item.id in carried_ids:
                self.assertEqual(item.location, "ground")
                self.assertEqual(item.ground_position, Position(40, 25))
                self.assertEqual(item.region_id, "hearthford")

    def test_defeated_thief_drops_the_stolen_physical_item(self):
        state = expedition("recover from thief")
        state.position = Position(40, 25)
        treasure = create_item(
            state, "passive:witness token", "marked coffer",
            owner_id=state.active_courier_id,
        )
        self.assertTrue(auto_place(state, treasure.id, "pack", owner_id=state.active_courier_id))
        record_acquisition(state, treasure)
        thief = Threat(
            "thief", "cargo runner", "pursuer", Position(41, 25), 1, 1,
            status="engaged", role="thief", morale=5,
            home_position=Position(30, 25), capabilities=["steal", "escape"],
        )
        state.threats = [thief]
        _threat_action(state, thief, False)
        self.assertEqual(treasure.location, "enemy")
        attack(state)
        self.assertEqual(treasure.location, "ground")
        self.assertEqual(treasure.ground_position, thief.position)
        self.assertIsNone(thief.carrying_item_id)

    def test_escaped_thief_records_a_physical_item_as_causally_lost(self):
        state = expedition("escaped thief reconciliation")
        treasure = create_item(
            state, "passive:witness token", "marked coffer",
            owner_id=state.active_courier_id,
        )
        self.assertTrue(auto_place(state, treasure.id, "pack", owner_id=state.active_courier_id))
        record_acquisition(state, treasure)
        thief = Threat(
            "escape-thief", "cargo runner", "pursuer", Position(41, 25), 4, 4,
            status="engaged", role="thief", morale=5,
            home_position=Position(41, 25), capabilities=["steal", "escape"],
        )
        state.position = Position(40, 25)
        state.threats = [thief]
        _threat_action(state, thief, False)
        self.assertEqual(treasure.location, "enemy")
        result = _threat_action(state, thief, False)
        self.assertIn("recorded as lost", result)
        self.assertEqual(treasure.location, "lost")
        self.assertIsNone(thief.carrying_item_id)
        self.assertIn("escaped", state.history[-1])

    def test_regional_process_geometry_reconstructs_from_persisted_stage(self):
        state = create_world("reconstruct region")
        activate_region(state, "greywash")
        state.region.process_stage = 2
        state.water.clear()
        reconstruct_regional_process(state)
        self.assertIn("76,40,0", state.water)
        activate_region(state, "greenwold")
        state.region.process_stage = 2
        state.smoke.clear()
        reconstruct_regional_process(state)
        self.assertIn("80,39,1", state.smoke)
        restored = game_state_from_dict(state.to_dict())
        self.assertIn("80,39,1", restored.smoke)

    def test_physical_ammunition_is_the_only_finite_authority(self):
        state = create_world("physical arrows")
        state.owned_weapons.append("longbow")
        self.assertTrue(choose_weapon(state, "longbow").changed)
        choose_gear(state, "rope")
        state.position = JOMON_GANGPLANK
        depart(state)
        state.position = Position(40, 25)
        for x in range(40, 51):
            state.region.tile_changes[f"{x},25,0"] = "."
        state.threats = [Threat("target", "target", "pursuer", Position(48, 25), 9, 9, status="engaged")]
        before = physical_ammunition(state, "arrows")
        attack(state)
        attack(state)
        self.assertEqual(physical_ammunition(state, "arrows"), before - 1)
        self.assertEqual(state.ammunition_by_type["arrows"], before - 1)

    def test_bottled_drink_is_consumed_in_the_field(self):
        state = expedition("bottled drink")
        bottle = create_item(
            state, "consumable:bottle:hearth-ale", "test bottle",
            owner_id=state.active_courier_id,
        )
        self.assertTrue(auto_place(state, bottle.id, "pack", owner_id=state.active_courier_id))
        result = use_gear(state)
        self.assertTrue(result.time_advanced)
        self.assertEqual(bottle.location, "destroyed")
        self.assertIn("hearth-ale", state.drink_effects)

    def test_version_five_migrates_deterministically_and_physicalises_relic(self):
        state = create_world("format five relic")
        legacy = copy.deepcopy(state.to_dict())
        legacy["save_format"] = 5
        legacy["relics"] = {"river-glass ward": 1}
        legacy["items"] = [
            item for item in legacy["items"]
            if item["kind"] != "relic:river-glass ward"
        ]
        legacy.pop("questlines")
        legacy.pop("cross_region_arc")
        legacy.pop("treasure_marks")
        legacy.pop("merchant")
        for region_id, new_id in {
            "hearthford": "compact",
            "greywash": "greywash-tide-account",
            "greenwold": "greenwold-burn-account",
            "whitecairn": "whitecairn-sink-account",
        }.items():
            legacy["regions"][region_id]["containers"] = [
                container
                for container in legacy["regions"][region_id]["containers"]
                if container["id"] != new_id
            ]
        legacy["region"] = legacy["regions"][legacy["active_region_id"]]
        first = game_state_from_dict(copy.deepcopy(legacy))
        second = game_state_from_dict(copy.deepcopy(legacy))
        self.assertEqual(first.save_format, SAVE_FORMAT)
        self.assertEqual(first.to_dict(), second.to_dict())
        wards = [
            item for item in first.items
            if item.kind == "relic:river-glass ward"
            and item.location not in {"lost", "destroyed"}
        ]
        self.assertEqual(len(wards), 1)
        self.assertEqual(first.cross_region_arc.status, "locked")
        self.assertEqual(set(first.questlines), set(first.regions))
        self.assertEqual(
            {region_id: len(region.containers) for region_id, region in first.regions.items()},
            {"hearthford": 9, "greywash": 7, "greenwold": 7, "whitecairn": 7},
        )


if __name__ == "__main__":
    unittest.main()
