from __future__ import annotations

import copy
import unittest

from jomon.ship_crises import HAZARD_STATIONS, begin_deck, choices, crisis_lines, work
from jomon.state import CommodityStack, create_world, game_state_from_dict
from jomon.travel import choose_destination, resolve_voyage
from jomon.voyage_variants import VARIANTS, active_variant, validate_variants


class StatefulVoyageVariantTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.base = create_world("causal voyage variants")

    def variant_state(self, family: str):
        state = copy.deepcopy(self.base)
        if family == "creature":
            state.regions["hearthford"].changes["aftermath_configuration"] = "shared"
        elif family == "lure":
            state.vessel_changes["refit:signal-mast-shutter"] = True
        elif family == "storm":
            state.weather = "storm"
        elif family == "shoal":
            state.traversed_route_edges.append("h-r")
        elif family == "galley-fire":
            state.vessel_cargo["wool"] = CommodityStack(1, "dry")
        elif family == "split-seam":
            state.vessel_integrity = 6
        elif family == "flooded-hold":
            state.calendar_origin_day = 72
        changed, message = choose_destination(state, "reed-anchor", forced_voyage=family)
        self.assertTrue(changed, message)
        self.assertEqual(active_variant(state, family), VARIANTS[family])
        return state

    def test_exactly_one_causal_inspectable_variant_exists_for_every_family(self):
        validate_variants()
        self.assertEqual(len(VARIANTS), 12)
        for family, expected in VARIANTS.items():
            with self.subTest(family=family):
                state = self.variant_state(family)
                self.assertIn(expected.name, state.voyage_detail)
                lines = crisis_lines(state)
                self.assertTrue(any(line.startswith("CAUSE ") for line in lines))
                self.assertTrue(any(line.startswith("CHANGED RULE ") for line in lines))
                self.assertTrue(any(line.startswith("COUNTERS ") for line in lines))
                loaded = game_state_from_dict(state.to_dict())
                self.assertEqual(active_variant(loaded, family), expected)

    def test_ordinary_causal_state_does_not_force_unqualified_variants(self):
        cases = ("creature", "lure", "storm", "shoal", "galley-fire", "split-seam", "flooded-hold")
        for family in cases:
            with self.subTest(family=family):
                state = copy.deepcopy(self.base)
                for account in state.institutions.values():
                    account.obligation = 0
                for market in state.regional_markets.values():
                    for entry in market.values():
                        entry.stock = entry.demand
                state.vessel_cargo = {"grain": CommodityStack(1, "dry")}
                state.weather = "clear"
                state.calendar_origin_day = 24
                choose_destination(state, "reed-anchor", forced_voyage=family)
                self.assertIsNone(active_variant(state))

    def test_raider_creature_and_obligation_variants_change_disclosed_costs(self):
        raiders = self.variant_state("raiders")
        before = sum(stack.quantity for stack in raiders.vessel_cargo.values())
        self.assertTrue(resolve_voyage(raiders, "yield")[0])
        self.assertEqual(sum(stack.quantity for stack in raiders.vessel_cargo.values()), before - 2)
        self.assertEqual(raiders.vessel_changes["voyage_variant:1"], "shortage-skiffs")

        creature = self.variant_state("creature")
        before = creature.vessel_cargo["salt fish"].quantity
        self.assertIn("two salt-fish", next(label for key, label, _ in choices(creature) if key == "B"))
        self.assertTrue(resolve_voyage(creature, "bait")[0])
        self.assertEqual(creature.vessel_cargo.get("salt fish"), None)
        self.assertEqual(before, 2)

        boarders = self.variant_state("boarders")
        obligations = sum(account.obligation for account in boarders.institutions.values())
        cargo = copy.deepcopy(boarders.vessel_cargo)
        self.assertTrue(any(key == "C" for key, _, _ in choices(boarders)))
        self.assertTrue(resolve_voyage(boarders, "counsel")[0])
        self.assertEqual(sum(account.obligation for account in boarders.institutions.values()), obligations - 1)
        self.assertEqual(boarders.vessel_cargo, cargo)

    def test_lure_shoal_driftwood_and_inspection_variants_change_route_resolution(self):
        lure = self.variant_state("lure")
        before = lure.world_time
        self.assertTrue(resolve_voyage(lure, "navigate")[0])
        self.assertEqual(lure.world_time, before + 2)
        self.assertEqual(lure.vessel_changes["signal_account_voyage"], 1)

        shoal = self.variant_state("shoal")
        before = shoal.world_time
        self.assertTrue(resolve_voyage(shoal, "navigate")[0])
        self.assertEqual(shoal.world_time, before + 6)

        drift = self.variant_state("driftwood")
        before = drift.world_time
        charcoal = drift.vessel_cargo.get("charcoal")
        previous = charcoal.quantity if charcoal else 0
        self.assertTrue(resolve_voyage(drift, "repel")[0])
        self.assertEqual(drift.world_time, before + 4)
        self.assertEqual(drift.vessel_cargo["charcoal"].quantity, previous + 1)

        inspection = self.variant_state("inspection")
        inspection.trade_credit = 4
        before_credit = inspection.trade_credit
        self.assertIn("three accountable credits", next(label for key, label, _ in choices(inspection) if key == "C"))
        self.assertTrue(resolve_voyage(inspection, "counsel")[0])
        self.assertEqual(inspection.trade_credit, before_credit - 3)

    def test_tactical_variants_change_real_actors_cargo_and_material_fields(self):
        raiders = self.variant_state("raiders")
        self.assertTrue(begin_deck(raiders)[0])
        self.assertEqual(len(raiders.vessel_threats), 3)

        creature = self.variant_state("creature")
        self.assertTrue(begin_deck(creature)[0])
        self.assertEqual(sum(actor.profile == "animal" for actor in creature.vessel_threats), 2)

        boarders = self.variant_state("boarders")
        self.assertTrue(begin_deck(boarders)[0])
        self.assertEqual(len(boarders.vessel_threats), 4)

        thieves = self.variant_state("hold-thieves")
        fish_before = thieves.vessel_cargo["salt fish"].quantity
        self.assertTrue(begin_deck(thieves)[0])
        shipment = next(item for item in thieves.items if item.provenance.startswith("unsecured Jomon shipment"))
        self.assertEqual(shipment.kind, "commodity:salt fish")
        self.assertEqual(thieves.vessel_cargo["salt fish"].quantity, fish_before - 1)

    def test_four_hazard_variants_add_a_second_sparse_front_and_one_work_action(self):
        cases = {
            "storm": "54,10,1",
            "galley-fire": "10,5,0",
            "split-seam": "22,15,-1",
            "flooded-hold": "10,15,-1",
        }
        for family, auxiliary in cases.items():
            with self.subTest(family=family):
                state = self.variant_state(family)
                state.gear = "rope"
                self.assertTrue(begin_deck(state)[0])
                self.assertIn(auxiliary, state.vessel_materials)
                cell = state.vessel_materials[auxiliary]
                self.assertTrue(cell.fire or cell.water or cell.support < 2)
                state.position = HAZARD_STATIONS[family]
                before = state.world_time
                self.assertTrue(work(state, "emergency")[0])
                self.assertEqual(state.world_time, before + 4)
                self.assertEqual(state.voyage_status, "resolved")

    def test_corrupt_or_mismatched_active_variant_is_rejected(self):
        state = self.variant_state("storm")
        broken = state.to_dict()
        broken["vessel_changes"]["active_voyage_variant"] = "not-a-variant"
        with self.assertRaisesRegex(ValueError, "voyage variant"):
            game_state_from_dict(broken)
        broken = state.to_dict()
        broken["voyage_kind"] = "raiders"
        with self.assertRaisesRegex(ValueError, "voyage variant"):
            game_state_from_dict(broken)


if __name__ == "__main__":
    unittest.main()
