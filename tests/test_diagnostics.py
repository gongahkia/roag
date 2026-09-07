from __future__ import annotations

import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.diagnostics import (
    audit_expeditions,
    completion_corridors,
    detail_corridor,
    format_audit,
)
from dumbest_dungeon.engine import GameEngine


class ExpeditionDiagnosticTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.catalog = load_catalog()

    def test_two_objective_corridors_cover_orders_approaches_and_facilities(self) -> None:
        engine = GameEngine.new(self.catalog, 42)
        base = completion_corridors(engine)
        assisted = completion_corridors(engine, include_facilities=True)
        self.assertEqual(48, len(base))
        self.assertEqual(432, len(assisted))
        self.assertTrue(all(corridor["facility_id"] is None for corridor in base))
        self.assertEqual(
            {facility.id for facility in engine.state.facilities},
            {
                corridor["facility_id"]
                for corridor in assisted
                if corridor["facility_id"] is not None
            },
        )
        self.assertTrue(all(len(corridor["objectives"]) == 2 for corridor in assisted))

    def test_corridor_detail_reports_spatial_pressure_without_mutation(self) -> None:
        engine = GameEngine.new(self.catalog, 42)
        before = engine.snapshot()
        corridor = min(completion_corridors(engine), key=lambda item: item["ticks"])
        detailed = detail_corridor(engine, corridor)
        self.assertEqual(corridor["ticks"], detailed["ticks"])
        self.assertGreaterEqual(detailed["hazard_fields"], 0)
        self.assertGreaterEqual(detailed["revisited_steps"], 0)
        self.assertGreaterEqual(detailed["patrol_posts"], 0)
        self.assertEqual(before, engine.snapshot())

    def test_bounded_audit_is_deterministic_and_reports_every_layout(self) -> None:
        first = audit_expeditions(self.catalog, 24)
        second = audit_expeditions(self.catalog, 24)
        self.assertEqual(first, second)
        self.assertEqual(
            {world["layout"] for world in self.catalog.worlds.values()},
            {row["layout"] for row in first},
        )
        self.assertTrue(all(row["best_light"] >= 0 for row in first))
        self.assertTrue(all(row["best_supplies"] >= 0 for row in first))
        rendered = format_audit(first)
        self.assertIn("Static route diagnostic only", rendered)
        self.assertIn("clusters", rendered)

    def test_audit_rejects_unbounded_seed_counts(self) -> None:
        with self.assertRaisesRegex(ValueError, "between 1 and 500"):
            audit_expeditions(self.catalog, 0)
        with self.assertRaisesRegex(ValueError, "between 1 and 500"):
            audit_expeditions(self.catalog, 501)


if __name__ == "__main__":
    unittest.main()
