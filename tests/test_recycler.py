from __future__ import annotations

import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine, RuleError


class RecyclerTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.catalog = load_catalog()

    def test_stack_is_sacrificed_for_one_deterministic_future_choice(self) -> None:
        engine = GameEngine.new(self.catalog, 4201)
        engine.acquire_item("bulkhead_laminate", 3)
        facility = engine.state.facilities[0]
        engine.state.phase = "facility"
        engine.state.current_facility_id = facility.id
        before_pressure = engine.state.pressure
        message = engine.recycle_item_stack("bulkhead_laminate")
        self.assertNotIn("bulkhead_laminate", engine.state.items)
        self.assertEqual(1, engine.state.recycler_credits)
        self.assertTrue(facility.used)
        self.assertEqual("recycle", facility.outcome)
        self.assertGreater(engine.state.pressure, before_pressure)
        self.assertIn("next salvage cache", message)

        pickup = next(item for item in engine.state.pickups if item.kind == "item" and not item.resolved)
        engine.state.phase = "discovery"
        engine.state.current_pickup_id = pickup.id
        rng_before = engine.rng.getstate()
        options = engine.item_pickup_options()
        self.assertEqual(3, len(options))
        self.assertEqual(rng_before, engine.rng.getstate())
        loaded = GameEngine.from_snapshot(self.catalog, engine.snapshot())
        self.assertEqual(options, loaded.item_pickup_options())
        loaded.resolve_item_pickup(options[1])
        self.assertEqual(0, loaded.state.recycler_credits)
        self.assertIn(options[1], loaded.state.items)

    def test_recycler_is_scarce_and_never_prints_an_unoffered_item(self) -> None:
        engine = GameEngine.new(self.catalog, 4202)
        engine.acquire_item("bulkhead_laminate", 1)
        facility = engine.state.facilities[0]
        engine.state.phase = "facility"
        engine.state.current_facility_id = facility.id
        engine.state.recycler_credits = 2
        with self.assertRaisesRegex(RuleError, "buffer is already full"):
            engine.recycle_item_stack("bulkhead_laminate")
        self.assertFalse(facility.used)
        self.assertIn("bulkhead_laminate", engine.state.items)

        pickup = next(item for item in engine.state.pickups if item.kind == "item")
        engine.state.phase = "discovery"
        engine.state.current_facility_id = None
        engine.state.current_pickup_id = pickup.id
        options = engine.item_pickup_options()
        unoffered = next(item_id for item_id in self.catalog.items if item_id not in options)
        with self.assertRaisesRegex(RuleError, "not offered"):
            engine.resolve_item_pickup(unoffered)


if __name__ == "__main__":
    unittest.main()
