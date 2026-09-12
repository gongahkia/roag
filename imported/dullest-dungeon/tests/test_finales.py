from __future__ import annotations

import unittest

from dumbest_dungeon.content import load_catalog
from dumbest_dungeon.engine import GameEngine


class FinaleTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        cls.catalog = load_catalog()

    def test_catalog_has_four_distinct_finale_identities(self) -> None:
        finale_ids = {
            "the_core",
            "base:finale_signal_tyrant",
            "base:finale_mercy_engine",
            "base:finale_breach_oracle",
        }
        self.assertTrue(finale_ids <= set(self.catalog.encounters))
        identities = {
            tuple(self.catalog.encounters[encounter_id]["enemies"])
            for encounter_id in finale_ids
        }
        self.assertEqual(4, len(identities))
        for identity in identities:
            for enemy_id in identity:
                self.assertGreaterEqual(len(self.catalog.art["enemies"][enemy_id]), 5)

    def test_finale_freeze_is_seeded_durable_and_updates_patrol(self) -> None:
        first = GameEngine.new(self.catalog, 4401)
        second = GameEngine.new(self.catalog, 4401)
        for engine in (first, second):
            objective = engine.state.objectives[1]
            encounter_id = engine._freeze_finale(objective)
            room = next(room for room in engine.state.rooms if room.kind == "boss")
            self.assertEqual(encounter_id, room.content_id)
            self.assertEqual(encounter_id, engine.core_patrol().encounter_id)
            self.assertEqual(
                self.catalog.encounters[encounter_id]["enemies"],
                room.enemy_ids,
            )
        self.assertEqual(first.finale_encounter_id(), second.finale_encounter_id())
        loaded = GameEngine.from_snapshot(self.catalog, first.snapshot())
        self.assertEqual(first.snapshot(), loaded.snapshot())

    def test_refreezing_never_rerolls_after_more_pressure(self) -> None:
        engine = GameEngine.new(self.catalog, 4402)
        objective = engine.state.objectives[1]
        frozen = engine._freeze_finale(objective)
        engine.state.pressure += 9999
        self.assertEqual(frozen, engine._freeze_finale(engine.state.objectives[2]))

    def test_intel_progresses_from_unknown_to_profile_to_exact(self) -> None:
        engine = GameEngine.new(self.catalog, 4403)
        self.assertEqual("unknown", engine.finale_intel()["level"])
        engine.state.objectives[0].completed = True
        self.assertEqual("profile", engine.finale_intel()["level"])
        encounter_id = engine._freeze_finale(engine.state.objectives[1])
        intel = engine.finale_intel()
        self.assertEqual("exact", intel["level"])
        self.assertEqual(encounter_id, intel["encounter_id"])
        self.assertIn(intel["name"], intel["summary"])

    def test_seed_cohort_reaches_every_finale(self) -> None:
        seen = set()
        for seed in range(80):
            engine = GameEngine.new(self.catalog, seed)
            seen.add(engine._freeze_finale(engine.state.objectives[1]))
        self.assertEqual(
            {
                "the_core",
                "base:finale_signal_tyrant",
                "base:finale_mercy_engine",
                "base:finale_breach_oracle",
            },
            seen,
        )


if __name__ == "__main__":
    unittest.main()
