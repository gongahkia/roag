import tempfile
import unittest
from pathlib import Path

from jomon.save import load_game, save_game
from jomon.state import create_world
from jomon.travel import choose_destination, resolve_voyage, voyage_for


class RegionalTravelTests(unittest.TestCase):
    def test_route_event_and_arrival_are_deterministic(self):
        first = create_world("deterministic voyage")
        second = create_world("deterministic voyage")
        self.assertEqual(voyage_for(first, "greywash"), voyage_for(second, "greywash"))
        start = first.world_time
        changed, _ = choose_destination(first, "greywash")
        self.assertTrue(changed)
        self.assertEqual(first.world_time, start + 6)
        if first.voyage_status == "active":
            response = {"raiders": "yield", "creature": "bait", "lure": "anchor"}[first.voyage_kind]
            resolve_voyage(first, response)
        self.assertEqual(first.active_region_id, "greywash")

    def test_all_three_voyage_families_have_material_responses(self):
        cases = (
            ("raiders", "repel", "pike", "route survey"),
            ("creature", "evade", "spear", "route survey"),
            ("lure", "counsel", "staff", "factor surety"),
        )
        for family, response, weapon, support in cases:
            state = create_world(f"forced {family}")
            state.active_courier_id = state.household[0].id
            state.weapon, state.gear, state.support = weapon, "rope", support
            choose_destination(state, "greywash", forced_voyage=family)
            self.assertEqual(state.voyage_kind, family)
            changed, consequence = resolve_voyage(state, response)
            with self.subTest(family=family):
                self.assertTrue(changed)
                self.assertTrue(consequence)
                self.assertEqual(state.active_region_id, "greywash")
                self.assertEqual(state.voyage_status, "resolved")

    def test_raider_failure_loses_accounted_cargo_and_injures(self):
        state = create_world("failed boarding")
        state.active_courier_id = state.household[0].id
        quantity = sum(stack.quantity for stack in state.vessel_cargo.values())
        choose_destination(state, "greenwold", forced_voyage="raiders")
        resolve_voyage(state, "repel")
        self.assertLess(sum(stack.quantity for stack in state.vessel_cargo.values()), quantity)
        self.assertIn("arms", state.courier.injuries)

    def test_active_voyage_save_round_trip(self):
        state = create_world("saved crossing")
        choose_destination(state, "whitecairn", forced_voyage="lure")
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "save.json"
            save_game(state, path)
            loaded = load_game(path)
        self.assertEqual(loaded.voyage_kind, "lure")
        self.assertEqual(loaded.voyage_status, "active")
        self.assertEqual(loaded.pending_destination, "whitecairn")


if __name__ == "__main__":
    unittest.main()
