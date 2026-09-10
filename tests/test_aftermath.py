from __future__ import annotations

import copy
import unittest

from jomon.aftermath import (
    AFTERMATH_LINES,
    contracts_for,
    prepare_aftermath,
    validate_aftermath,
)
from jomon.frontiers import FRONTIERS, ensure_frontier
from jomon.quests import QUESTS
from jomon.regions import activate_region, region_reachable
from jomon.state import create_world, game_state_from_dict, validate_state


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


if __name__ == "__main__":
    unittest.main()
