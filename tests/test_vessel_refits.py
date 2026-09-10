from __future__ import annotations

import copy
import unittest

from jomon.actions import interact
from jomon.materials import affect_body, key
from jomon.ship_crises import HAZARD_STATIONS, begin_deck, work
from jomon.state import CommodityStack, MaterialCell, Position, create_world, game_state_from_dict
from jomon.terminal import InputEvent, OverlayView, _handle_overlay_view, dialogue_choices
from jomon.travel import _lose_vessel_cargo, choose_destination, resolve_voyage
from jomon.vessel_refits import REFITS, install_refit, installed


STATIONS = {
    "galley": Position(8, 5, 0),
    "bilge": Position(8, 15, -1),
    "repair": Position(50, 5, 0),
    "storage": Position(15, 10, 1),
    "helm": Position(35, 10, 1),
    "berths": Position(16, 5, 0),
    "lookout": Position(7, 10, 1),
}


class VesselRefitTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("physical vessel refits")

    def state_for(self, refit_id: str):
        state = copy.deepcopy(self.base)
        refit = REFITS[refit_id]
        state.position = STATIONS[refit.station]
        state.vessel_cargo[refit.dependency] = CommodityStack(1, "sound and dry")
        state.trade_credit = 20
        return state

    def test_all_eight_refits_install_only_at_their_physical_station_and_round_trip(self):
        self.assertEqual(len(REFITS), 8)
        for refit_id, refit in REFITS.items():
            with self.subTest(refit=refit_id):
                state = self.state_for(refit_id)
                state.position = Position(63, 10, 0)
                before = state.to_dict()
                self.assertFalse(install_refit(state, refit_id)[0])
                self.assertEqual(state.to_dict(), before)
                state.position = STATIONS[refit.station]
                time = state.world_time
                credit = state.trade_credit
                changed, message = install_refit(state, refit_id)
                self.assertTrue(changed, message)
                self.assertTrue(installed(state, refit_id))
                self.assertNotIn(refit.dependency, state.vessel_cargo)
                self.assertEqual(state.trade_credit, credit - refit.credit)
                self.assertEqual(state.world_time, time + 3)
                loaded = game_state_from_dict(state.to_dict())
                self.assertTrue(installed(loaded, refit_id))
                self.assertEqual(loaded.vessel_changes, state.vessel_changes)

    def test_station_overlay_previews_then_installs_without_hidden_time(self):
        state = self.state_for("cargo-rail-netting")
        before = state.world_time
        self.assertEqual(interact(state).overlay, "station:storage")
        view = OverlayView("station:storage")
        self.assertTrue(any(option.key == "V" for option in dialogue_choices(state, view.kind)))
        closed, _ = _handle_overlay_view(state, view, InputEvent("key", ord("v")))
        self.assertFalse(closed)
        self.assertEqual(view.kind, "vessel-refits:storage")
        self.assertEqual(state.world_time, before)
        _handle_overlay_view(state, view, InputEvent("key", ord("1")))
        self.assertTrue(installed(state, "cargo-rail-netting"))
        self.assertEqual(state.world_time, before + 3)
        self.assertEqual(view.kind, "station:storage")

    def test_galley_cover_changes_fire_emergency_and_meal_work(self):
        state = copy.deepcopy(self.base)
        state.vessel_changes["refit:galley-fire-cover"] = True
        choose_destination(state, "reed-anchor", forced_voyage="galley-fire")
        self.assertTrue(begin_deck(state)[0])
        self.assertEqual(state.vessel_materials["9,5,0"].fuel, 5)
        state.position = HAZARD_STATIONS["galley-fire"]
        before = state.world_time
        self.assertTrue(work(state, "emergency")[0])
        self.assertEqual(state.world_time, before + 2)

        meal = copy.deepcopy(self.base)
        meal.vessel_changes["refit:galley-fire-cover"] = True
        meal.position = STATIONS["galley"]
        meal.courier.health = 5
        meal.vessel_cargo["grain"] = CommodityStack(1, "dry")
        before = meal.world_time
        self.assertTrue(work(meal, "meal")[0])
        self.assertEqual(meal.courier.health, 8)
        self.assertEqual(meal.world_time, before + 3)

    def test_strainers_bound_flooding_and_pumping_work(self):
        state = copy.deepcopy(self.base)
        state.vessel_changes["refit:twin-bilge-strainers"] = True
        choose_destination(state, "reed-anchor", forced_voyage="flooded-hold")
        self.assertTrue(begin_deck(state)[0])
        self.assertEqual(state.vessel_materials["9,15,-1"].water, 1)
        state.position = STATIONS["bilge"]
        before = state.world_time
        self.assertTrue(work(state, "pump")[0])
        self.assertEqual(state.world_time, before + 1)
        self.assertFalse(any(cell.water for cell in state.vessel_materials.values()))

    def test_backstay_supplies_physical_storm_counter_without_rope(self):
        state = copy.deepcopy(self.base)
        state.vessel_changes["refit:storm-backstay"] = True
        state.gear = "trade seals"
        state.courier.technique = "quiet passage"
        choose_destination(state, "reed-anchor", forced_voyage="storm")
        self.assertTrue(begin_deck(state)[0])
        cell = state.vessel_materials[key(Position(56, 10, 1))]
        self.assertEqual(cell.support, 2)
        state.position = HAZARD_STATIONS["storm"]
        before = state.world_time
        self.assertTrue(work(state, "emergency")[0])
        self.assertEqual(state.world_time, before + 2)

    def test_net_keel_and_signal_each_change_a_route_consequence(self):
        net = copy.deepcopy(self.base)
        net.vessel_changes["refit:cargo-rail-netting"] = True
        net.travel_count = 3
        before = sum(stack.quantity for stack in net.vessel_cargo.values())
        self.assertIn("catches", _lose_vessel_cargo(net))
        self.assertEqual(sum(stack.quantity for stack in net.vessel_cargo.values()), before)
        self.assertIn("lost", _lose_vessel_cargo(net))
        self.assertEqual(sum(stack.quantity for stack in net.vessel_cargo.values()), before - 1)

        keel = copy.deepcopy(self.base)
        keel.vessel_changes["refit:sounding-keel-shoes"] = True
        choose_destination(keel, "reed-anchor", forced_voyage="shoal")
        before = keel.world_time
        self.assertTrue(resolve_voyage(keel, "navigate")[0])
        self.assertEqual(keel.world_time, before + 3)

        signal = copy.deepcopy(self.base)
        signal.vessel_changes["refit:signal-mast-shutter"] = True
        signal.support = None
        signal.courier.role = "bargemaster"
        choose_destination(signal, "reed-anchor", forced_voyage="lure")
        changed, message = resolve_voyage(signal, "navigate")
        self.assertTrue(changed)
        self.assertIn("shutter", message)

    def test_hatch_felt_prevents_only_aboard_winter_chill(self):
        exposed = copy.deepcopy(self.base)
        exposed.calendar_origin_day = 72
        for item in exposed.items:
            if item.owner_id == exposed.active_courier_id and item.location not in {"pack", "readied", "secondary"}:
                item.condition = 0
        affect_body(exposed, exposed.courier, "water", 1, exposed.position)
        self.assertIn("wet", exposed.terrain_statuses)
        self.assertIn("chilled", exposed.terrain_statuses)

        sheltered = copy.deepcopy(self.base)
        sheltered.calendar_origin_day = 72
        sheltered.vessel_changes["refit:winter-hatch-felt"] = True
        for item in sheltered.items:
            if item.owner_id == sheltered.active_courier_id and item.location not in {"pack", "readied", "secondary"}:
                item.condition = 0
        affect_body(sheltered, sheltered.courier, "water", 1, sheltered.position)
        self.assertIn("wet", sheltered.terrain_statuses)
        self.assertNotIn("chilled", sheltered.terrain_statuses)

    def test_sling_cot_clears_one_injury_for_material_and_time(self):
        state = copy.deepcopy(self.base)
        state.vessel_changes["refit:sickbay-sling-cot"] = True
        state.position = STATIONS["berths"]
        state.vessel_cargo["wool"] = CommodityStack(1, "clean")
        state.courier.injuries = {"arms": "cut arm", "legs": "bruised knee"}
        state.courier.injury = "cut arm"
        before = state.world_time
        changed, message = work(state, "treat")
        self.assertTrue(changed, message)
        self.assertEqual(state.courier.injuries, {"legs": "bruised knee"})
        self.assertEqual(state.world_time, before + 6)
        self.assertNotIn("wool", state.vessel_cargo)


if __name__ == "__main__":
    unittest.main()
