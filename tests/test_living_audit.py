from __future__ import annotations

import unittest

from jomon.living_audit import living_audit


class LivingAuditTests(unittest.TestCase):
    def test_small_audit_finds_deterministic_connected_routes_and_schedules(self):
        report = living_audit(12, 2)
        self.assertEqual(report["route_samples"], 12)
        self.assertEqual(report["disconnected_route_graphs"], 0)
        self.assertEqual(report["nondeterministic_route_graphs"], 0)
        self.assertEqual(report["schedule_validation_failures"], 0)
        self.assertEqual(report["vessel_position_overlaps"], 0)
        self.assertEqual(report["nondeterministic_schedules"], 0)


if __name__ == "__main__":
    unittest.main()
