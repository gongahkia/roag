"""Rescues must survive the outward trip, escort, countdown, and save/load."""

import json
import tempfile
import unittest
from pathlib import Path

from regolith.simulation import Colonist, Simulation
from regolith.world import Terrain


class ExpeditionTests(unittest.TestCase):
    def setUp(self):
        self.sim = Simulation(42, 48, 32)
        self.ada = self.sim.colonists[0]
        self.survivor = self.sim.survivors[0]

    def test_dispatch_spends_one_kit_and_preempts_work(self):
        cx, cy = self.sim.world.center
        self.sim.designate(cx, cy + 1, "build", "solar")
        self.sim.step(7)
        job = self.sim.jobs[0]
        self.assertEqual(job.assigned, self.ada.id)
        before = self.sim.resources.copy()
        self.assertTrue(self.sim.dispatch_expedition(self.survivor.id)[0])
        self.assertIsNone(job.assigned)
        self.assertIsNone(self.ada.job)
        self.assertIsNotNone(self.ada.expedition)
        self.assertEqual(self.sim.resources["food"], before["food"] - 2)
        self.assertEqual(self.sim.resources["water"], before["water"] - 2)
        self.assertFalse(self.sim.dispatch_expedition(self.survivor.id)[0])
        self.assertEqual(self.sim.resources["food"], before["food"] - 2)

    def test_survivor_joins_only_after_escort_reaches_pressure(self):
        self.assertTrue(self.sim.dispatch_expedition(self.survivor.id)[0])
        self.assertEqual(len(self.sim.living), 1)
        for _ in range(100):
            self.sim.step()
            if self.survivor.status == "escorted":
                break
        self.assertEqual(self.survivor.status, "escorted")
        self.assertEqual(len(self.sim.living), 1)
        self.assertGreaterEqual(self.survivor.suit, 59)
        self.assertEqual(self.sim.expeditions[0].phase, "returning")
        self.sim.step(80)
        self.assertEqual(self.sim.rescued, 1)
        self.assertEqual(len(self.sim.living), 2)
        mika = self.sim.colonists[1]
        self.assertEqual((mika.name, mika.role), ("Mika", "Miner"))
        self.assertGreater(self.sim.world.at(mika.x, mika.y).oxygen, 0.5)
        self.assertEqual(self.sim.expeditions[0].phase, "complete")
        self.assertIsNone(self.ada.expedition)

    def test_unready_crew_or_missing_kit_does_not_spend(self):
        before = self.sim.resources.copy()
        self.ada.suit = 10
        self.assertFalse(self.sim.dispatch_expedition(0)[0])
        self.assertEqual(self.sim.resources, before)
        self.ada.suit = 100
        self.sim.resources["food"] = 1
        self.assertFalse(self.sim.dispatch_expedition(0)[0])
        self.assertFalse(self.sim.expeditions)

    def test_blocked_route_does_not_spend_kit(self):
        cx, cy = self.sim.world.center
        self.sim.world.at(cx + 9, cy).terrain = Terrain.WALL
        before = self.sim.resources.copy()
        self.assertFalse(self.sim.dispatch_expedition(0)[0])
        self.assertEqual(self.sim.resources, before)

    def test_ignored_survivor_loses_oxygen_then_health_and_dies(self):
        health = self.survivor.health
        self.sim.step(20)
        self.assertLess(self.survivor.suit, 68)
        self.assertEqual(self.survivor.health, health)
        self.survivor.suit, self.survivor.health = 0.01, 0.5
        self.sim.step()
        self.assertEqual(self.survivor.status, "lost")
        self.assertEqual(self.sim.rescued, 0)
        self.assertEqual(len(self.sim.living), 1)
        self.assertFalse(self.sim.dispatch_expedition(0)[0])

    def test_survivor_death_during_expedition_releases_rescuer(self):
        self.sim.dispatch_expedition(0)
        self.survivor.suit, self.survivor.health = 0.01, 0.5
        self.sim.step()
        self.assertEqual(self.sim.expeditions[0].phase, "failed")
        self.assertIsNone(self.ada.expedition)
        self.assertTrue(self.ada.alive)

    def test_rescuer_death_leaves_live_survivor_available_for_another_attempt(self):
        self.sim.dispatch_expedition(0)
        self.sim.step(2)
        self.ada.health = 0
        cx, cy = self.sim.world.center
        self.sim.colonists.append(Colonist(1, "Ren", "Medic", "Patient", cx, cy))
        self.sim.step()
        self.assertEqual(self.sim.expeditions[0].phase, "failed")
        self.assertEqual(self.survivor.status, "stranded")
        self.assertTrue(self.sim.dispatch_expedition(0)[0])
        self.assertEqual(self.sim.expeditions[-1].crew_id, 1)

    def test_new_distress_signals_appear_after_the_first_rescue(self):
        self.sim.dispatch_expedition(0)
        self.sim.step(180)
        self.assertEqual(self.sim.rescued, 1)
        self.assertEqual([s.name for s in self.sim.distress], ["Sol"])
        self.assertNotEqual(self.sim.survivors[0].id, self.sim.survivors[1].id)

    def test_spawned_contacts_are_reachable_for_multiple_seeds(self):
        for seed in (7, 81, 8675309):
            sim = Simulation(seed, 48, 32)
            for _ in range(5):
                survivor = sim.survivors[-1]
                self.assertIsNotNone(sim.path_to(sim.world.center, {(survivor.x, survivor.y)}))
                sim._spawn_survivor()

    def test_outbound_and_returning_expeditions_resume_identically(self):
        self.sim.dispatch_expedition(0)
        for phase in ("outbound", "returning"):
            with self.subTest(phase=phase), tempfile.TemporaryDirectory() as directory:
                for _ in range(100):
                    if self.sim.expeditions[0].phase == phase:
                        break
                    self.sim.step()
                self.assertEqual(self.sim.expeditions[0].phase, phase)
                path = Path(directory) / "colony.json"
                self.sim.save(path)
                restored = Simulation.load(path)
                self.assertEqual(restored.to_dict(), self.sim.to_dict())
                # Compare just a few ticks, leaving time to save the escort too.
                self.sim.step(2)
                restored.step(2)
                self.assertEqual(restored.to_dict(), self.sim.to_dict())
        self.sim.step(100)
        self.assertEqual(self.sim.rescued, 1)

    def test_version_one_save_preserves_existing_crew_and_adds_future_signals(self):
        cx, cy = self.sim.world.center
        self.sim.colonists.append(Colonist(1, "Mika", "Miner", "Restless", cx, cy))
        payload = self.sim.to_dict()
        payload["version"] = 1
        for key in ("survivors", "expeditions", "next_signal"):
            payload.pop(key)
        for person in payload["colonists"]:
            person.pop("expedition")
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "old.json"
            path.write_text(json.dumps(payload))
            restored = Simulation.load(path)
        self.assertEqual(len(restored.living), 2)
        self.assertFalse(restored.survivors)
        restored.step(180)
        self.assertEqual(restored.distress[0].name, "Sol")


if __name__ == "__main__":
    unittest.main()
