from __future__ import annotations

import copy
import unittest

from jomon.aftermath import (
    AFTERMATH_LINES,
    AFTERMATH_TOPOLOGIES,
    accept_contract,
    abandon_contract,
    contract_options,
    contracts_for,
    prepare_aftermath,
    settle_contract,
    supply_contract,
    validate_aftermath,
    work_contract,
)
from jomon.frontiers import FRONTIERS, ensure_frontier
from jomon.inventory import auto_place, create_item, sync_legacy_load
from jomon.materials import ensure_cell
from jomon.quests import QUESTS
from jomon.regions import activate_region, region_reachable
from jomon.state import create_world, game_state_from_dict, validate_state
from jomon.terminal import _handle_overlay, _overlay_lines, dialogue_choices


def completed_revisit(seed: str, region_id: str, ending_index: int):
    state = create_world(seed)
    for frontier in FRONTIERS:
        ensure_frontier(state, frontier)
    activate_region(state, region_id)
    state.location = "region"
    quest = state.questlines[region_id]
    ending = QUESTS[region_id]["final"][ending_index][0]
    quest.status, quest.stage = "completed", 3
    quest.decisions.append(f"ending:{ending}")
    state.region.changes["aftermath_after_return"] = 0
    state.returned_expeditions = 1
    return state


class EndingDerivedAftermathTests(unittest.TestCase):
    def prepared(self, seed: str = "playable aftermath"):
        state = completed_revisit(seed, "hearthford", 0)
        state.threats.clear()
        self.assertTrue(prepare_aftermath(state))
        contract = contracts_for(state)[0]
        witness = next(
            contact for contact in state.contacts["hearthford"]
            if contact.id == contract.participant_id
        )
        schedule = state.actor_schedules[witness.id]
        schedule.area = "region:hearthford"
        state.position = schedule.position
        return state, contract

    def pack(self, state, kind: str):
        item = create_item(state, kind, "aftermath contract test")
        self.assertTrue(auto_place(
            state, item.id, "pack", owner_id=state.active_courier_id
        ))
        sync_legacy_load(state)
        return item

    def test_all_eight_regions_build_two_branch_specific_revisits(self):
        for region_id in AFTERMATH_LINES:
            with self.subTest(region=region_id):
                shared = completed_revisit(f"shared aftermath {region_id}", region_id, 0)
                claimed = completed_revisit(f"claimed aftermath {region_id}", region_id, 1)

                self.assertTrue(prepare_aftermath(shared))
                self.assertTrue(prepare_aftermath(claimed))
                self.assertEqual(shared.region.changes["aftermath_configuration"], "shared")
                self.assertEqual(claimed.region.changes["aftermath_configuration"], "claimed")
                self.assertNotEqual(
                    shared.region.tile_changes,
                    claimed.region.tile_changes,
                )
                for state in (shared, claimed):
                    self.assertEqual(len(contracts_for(state)), 2)
                    sites = {
                        state.region.changes[f"aftermath_site:{index}"]
                        for index in range(3)
                    }
                    self.assertEqual(len(sites), 3)
                    self.assertTrue(all(
                        contract.site in region_reachable(state.region)
                        for contract in contracts_for(state)
                    ))
                    validate_aftermath(state)

    def test_aftermath_waits_for_an_actual_return_and_never_reapplies(self):
        state = completed_revisit("aftermath return gate", "hearthford", 0)
        state.returned_expeditions = 0
        self.assertFalse(prepare_aftermath(state))
        self.assertFalse(state.regional_contracts)
        state.returned_expeditions = 1
        self.assertTrue(prepare_aftermath(state))
        before = copy.deepcopy(state.to_dict())
        self.assertFalse(prepare_aftermath(state))
        self.assertEqual(state.to_dict(), before)

    def test_contracts_name_real_causes_participants_materials_and_sites(self):
        state = completed_revisit("aftermath causes", "greywash", 1)
        prepare_aftermath(state)
        witness_ids = {contact.id for contact in state.contacts["greywash"]}
        for contract in contracts_for(state):
            self.assertIn(contract.participant_id, witness_ids)
            self.assertIn(contract.commodity, state.market)
            self.assertIn(str(contract.site.x), contract.cause)
            self.assertTrue(contract.topology)

    def test_configured_aftermath_and_contracts_round_trip_exactly(self):
        state = completed_revisit("aftermath round trip", "frostmere", 0)
        prepare_aftermath(state)
        before = copy.deepcopy(state.to_dict())

        loaded = game_state_from_dict(before)

        self.assertEqual(loaded.to_dict(), before)
        validate_state(loaded)

    def test_supply_approach_consumes_real_lot_and_physical_copy(self):
        state, contract = self.prepared("aftermath supply")
        stock = state.market[contract.commodity].stock
        trust = state.institutions["work:hearthford"].trust
        self.assertTrue(accept_contract(state, contract.id)[0])
        token = next(item for item in state.items if item.id == contract.token_item_id)
        self.assertEqual(token.kind, contract.id)
        self.assertEqual(token.location, "pack")
        supplied = self.pack(state, f"commodity:{contract.commodity}")

        self.assertTrue(supply_contract(state, contract.id)[0])
        self.assertEqual(supplied.location, "destroyed")
        self.assertTrue(settle_contract(state, contract.id)[0])

        self.assertEqual(contract.status, "completed")
        self.assertEqual(token.location, "destroyed")
        self.assertEqual(state.market[contract.commodity].stock, min(10, stock + 2))
        self.assertEqual(
            state.institutions["work:hearthford"].trust,
            min(3, trust + 1),
        )

    def test_completing_both_aftermath_contracts_teaches_the_regional_practice(self):
        state, _ = self.prepared("aftermath instruction")
        for contract in contracts_for(state):
            state.position = state.actor_schedules[contract.participant_id].position
            self.assertTrue(accept_contract(state, contract.id)[0])
            self.pack(state, f"commodity:{contract.commodity}")
            self.assertTrue(supply_contract(state, contract.id)[0])
            changed, message = settle_contract(state, contract.id)
            self.assertTrue(changed)
        self.assertIn("siltgate hand", state.courier.learned_techniques)
        self.assertIn("learns siltgate hand", message)

    def test_field_approach_changes_material_and_route_then_round_trips(self):
        state, contract = self.prepared("aftermath field")
        self.assertTrue(accept_contract(state, contract.id)[0])
        state.gear = "repair tools"
        state.position = contract.site
        before_risks = {
            edge.id: edge.cargo_risk for edge in state.route_edges
            if contract.region_id in {edge.first, edge.second}
        }

        self.assertTrue(work_contract(state, contract.id)[0])
        self.assertIn(f"contract-work:{contract.id}", state.region.changes)
        loaded = game_state_from_dict(copy.deepcopy(state.to_dict()))
        loaded_contract = loaded.regional_contracts[contract.id]
        witness = loaded.actor_schedules[loaded_contract.participant_id]
        loaded.position = witness.position
        self.assertTrue(settle_contract(loaded, contract.id)[0])

        self.assertEqual(loaded_contract.approach, "field")
        self.assertTrue(all(
            edge.cargo_risk == max(0, before_risks[edge.id] - 1)
            for edge in loaded.route_edges if edge.id in before_risks
        ))
        validate_state(loaded)

    def test_only_a_lost_or_destroyed_copy_can_be_replaced(self):
        state, contract = self.prepared("aftermath lost paper")
        self.assertTrue(accept_contract(state, contract.id)[0])
        self.pack(state, f"commodity:{contract.commodity}")
        self.assertTrue(supply_contract(state, contract.id)[0])
        token = next(item for item in state.items if item.id == contract.token_item_id)
        token.location, token.owner_id = "ground", None
        credits = state.trade_credit = 2
        self.assertFalse(settle_contract(state, contract.id)[0])
        self.assertEqual(state.trade_credit, credits)
        token.location = "lost"
        self.assertTrue(settle_contract(state, contract.id)[0])
        # Replacement costs one and completed work pays one: the exchange is net zero.
        self.assertEqual(state.trade_credit, credits)

    def test_abandonment_is_persistent_finite_and_updates_line(self):
        state, contract = self.prepared("aftermath abandonment")
        other = contracts_for(state)[1]
        self.assertTrue(accept_contract(state, contract.id)[0])
        self.assertTrue(abandon_contract(state, contract.id)[0])
        self.assertEqual(contract.status, "failed")
        self.assertFalse(prepare_aftermath(state))
        self.assertEqual(len(contracts_for(state)), 2)
        self.assertEqual(state.aftermath_quests["hearthford"].stage, 1)
        self.assertEqual(other.status, "available")
        loaded = game_state_from_dict(copy.deepcopy(state.to_dict()))
        self.assertEqual(loaded.regional_contracts[contract.id].status, "failed")

    def test_contract_overlay_routes_real_actions_without_time_for_inspection(self):
        state, contract = self.prepared("aftermath terminal")
        self.assertEqual(len(dialogue_choices(state, "aftermath")), 2)
        before = state.world_time
        detail, quit_requested = _handle_overlay(state, "aftermath", ord("1"))
        self.assertEqual(detail, "aftermath-contract:" + contract.id)
        self.assertFalse(quit_requested)
        self.assertEqual(state.world_time, before)
        title, lines = _overlay_lines(state, detail)
        self.assertEqual(title, contract.title.upper())
        self.assertIn("DISCLOSED ANSWERS", " ".join(lines))
        self.assertTrue(any(option.key == "A" for option in dialogue_choices(state, detail)))
        closed, _ = _handle_overlay(state, detail, ord("a"))
        self.assertIsNone(closed)
        self.assertEqual(state.world_time, before + 1)
        self.assertEqual(contract.status, "active")

    def test_all_sixteen_contract_definitions_have_both_disclosed_approaches(self):
        contracts = []
        topologies = []
        for region_id in AFTERMATH_LINES:
            state = completed_revisit(f"all contracts {region_id}", region_id, 0)
            prepare_aftermath(state)
            for contract in contracts_for(state):
                witness = state.actor_schedules[contract.participant_id]
                state.position = witness.position
                self.assertTrue(accept_contract(state, contract.id)[0])
                keys = {row[0] for row in contract_options(state, contract.id)}
                self.assertTrue({"d", "w", "x"}.issubset(keys))
                contracts.append(contract.id)
                topologies.append(contract.topology)
        self.assertEqual(len(contracts), 16)
        self.assertEqual(len(set(contracts)), 16)
        self.assertEqual(len(set(topologies)), 16)
        self.assertEqual(
            set(topologies),
            {topology for pair in AFTERMATH_TOPOLOGIES.values() for topology in pair},
        )

    def test_four_topology_families_change_drainage_fire_support_and_recovery(self):
        hearth, _ = self.prepared("aftermath topology hearth")
        flood = next(c for c in contracts_for(hearth) if c.topology == "flood-mark circuit")
        timber = next(c for c in contracts_for(hearth) if c.topology == "wheel-timber account")
        for contract in (flood, timber):
            hearth.position = hearth.actor_schedules[contract.participant_id].position
            self.assertTrue(accept_contract(hearth, contract.id)[0])
        hearth.gear = "repair tools"
        flood_cell = ensure_cell(hearth, flood.site)
        flood_cell.water, flood_cell.ice = 3, True
        hearth.position = flood.site
        self.assertTrue(work_contract(hearth, flood.id)[0])
        self.assertEqual(flood_cell.water, 1)
        self.assertFalse(flood_cell.ice)
        timber_cell = ensure_cell(hearth, timber.site)
        timber_cell.support, timber_cell.collapse_due = 0, 9
        hearth.position = timber.site
        self.assertTrue(work_contract(hearth, timber.id)[0])
        self.assertEqual(timber_cell.support, 2)
        self.assertEqual(timber_cell.collapse_due, 0)

        coast = completed_revisit("aftermath topology coast", "greywash", 0)
        coast.threats.clear()
        prepare_aftermath(coast)
        beacon = next(c for c in contracts_for(coast) if c.topology == "storm-beacon line")
        wreck = next(c for c in contracts_for(coast) if c.topology == "shifted-wreck recovery")
        for contract in (beacon, wreck):
            coast.position = coast.actor_schedules[contract.participant_id].position
            self.assertTrue(accept_contract(coast, contract.id)[0])
        coast.gear = "repair tools"
        beacon_cell = ensure_cell(coast, beacon.site)
        beacon_cell.fire, beacon_cell.smoke, beacon_cell.fuel = 3, 4, 5
        coast.position = beacon.site
        self.assertTrue(work_contract(coast, beacon.id)[0])
        self.assertEqual((beacon_cell.fire, beacon_cell.smoke, beacon_cell.fuel), (0, 0, 3))
        wreck_cell = ensure_cell(coast, wreck.site)
        wreck_cell.coating, wreck_cell.water = "salt", 2
        coast.position = wreck.site
        self.assertTrue(work_contract(coast, wreck.id)[0])
        self.assertEqual(wreck_cell.coating, "")
        self.assertEqual(wreck_cell.water, 1)
        self.assertTrue(coast.treasure_marks["greywash"])


if __name__ == "__main__":
    unittest.main()
