import unittest

from jomon.frontiers import ensure_frontier
from jomon.interference import INTERFERENCES, apply_arrival, audit_interference
from jomon.regions import activate_region
from jomon.situations import BY_REGION_BAND
from jomon.state import create_world


class CrossRegionInterferenceTests(unittest.TestCase):
    def world(self):
        state = create_world("cross-region-interference")
        for region_id in ("dunmire", "rillscar", "marlbank", "frostmere"):
            ensure_frontier(state, region_id)
        for row in INTERFERENCES:
            situation = BY_REGION_BAND[row.origin, "steady"]
            state.regions[row.origin].changes[f"micro-site:resolved:{situation.id}"] = True
        return state

    def test_catalogue_covers_every_region_both_directions(self):
        self.assertEqual(audit_interference()["failures"], [])
        self.assertEqual(len(INTERFERENCES), 8)

    def test_arrivals_apply_all_events_once_and_change_both_regions(self):
        state = self.world()
        for destination in sorted({row.destination for row in INTERFERENCES}):
            activate_region(state, destination)
        for row in INTERFERENCES:
            self.assertIn(f"interference-out:{row.id}", state.regions[row.origin].changes)
            self.assertIn(f"interference-in:{row.id}", state.regions[row.destination].changes)
            self.assertIn(f"interference:{row.id}", state.vessel_changes)
        before = dict(state.vessel_changes)
        for destination in sorted({row.destination for row in INTERFERENCES}):
            self.assertEqual(apply_arrival(state, destination), [])
        self.assertEqual(state.vessel_changes, before)

    def test_shipment_changes_real_stock_and_institutional_evidence(self):
        state = self.world()
        row = next(row for row in INTERFERENCES if row.kind == "shipment")
        source = state.regional_markets[row.origin][row.cargo].stock
        destination = state.regional_markets[row.destination][row.cargo].stock
        activate_region(state, row.destination)
        self.assertEqual(state.regional_markets[row.origin][row.cargo].stock, max(0, source - 1))
        self.assertEqual(state.regional_markets[row.destination][row.cargo].stock, destination + 1)
        self.assertTrue(any(row.title in act for act in state.institutions[f"work:{row.destination}"].witnessed_acts))

    def test_unsettled_origin_cannot_emit_event(self):
        state = create_world("unsettled-interference")
        row = INTERFERENCES[0]
        self.assertEqual(apply_arrival(state, row.destination), [])
        self.assertNotIn(f"interference:{row.id}", state.vessel_changes)


if __name__ == "__main__":
    unittest.main()
