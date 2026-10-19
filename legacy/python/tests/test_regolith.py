"""Behavioral checks for the automata, colony economy, and saved simulations."""

import json
import math
import random
import tempfile
import unittest
from pathlib import Path

from regolith.__main__ import demo_orders
from regolith.simulation import Machine, Simulation
from regolith.world import Cell, Terrain, World


def chamber(size=7):
    world = World.__new__(World)
    world.width = world.height = size
    world.seed = 0
    world.cells = [Cell(Terrain.WALL, heat=20) for _ in range(size * size)]
    for y in range(1, size - 1):
        for x in range(1, size - 1):
            world.at(x, y).terrain = Terrain.FLOOR
    return world


class AutomataTests(unittest.TestCase):
    def test_seeded_geology_is_repeatable(self):
        first, second = World(48, 32, 81), World(48, 32, 81)
        self.assertEqual(first.cells, second.cells)
        self.assertNotEqual(first.cells, World(48, 32, 82).cells)

    def test_diffusion_is_local_symmetric_and_conserves_oxygen(self):
        world = chamber()
        world.at(3, 3).oxygen = 1
        world.environment(random.Random(0), 0.5)
        self.assertAlmostEqual(sum(c.oxygen for c in world.cells), 1)
        self.assertAlmostEqual(world.at(3, 3).oxygen, 0.52)
        for pos in world.neighbors(3, 3):
            self.assertAlmostEqual(world.at(*pos).oxygen, 0.12)
        self.assertEqual(world.at(1, 3).oxygen, 0)

    def test_diffusion_remains_bounded(self):
        world = chamber()
        rng = random.Random(0)
        for cell in world.cells:
            if cell.terrain == Terrain.FLOOR:
                cell.oxygen = rng.random()
        initial = sum(c.oxygen for c in world.cells)
        for _ in range(80):
            world.environment(rng, 0.4)
        self.assertAlmostEqual(sum(c.oxygen for c in world.cells), initial)
        self.assertTrue(all(0 <= c.oxygen <= 1 for c in world.cells))

    def test_hull_breach_vents_and_repair_stops_loss(self):
        world = chamber()
        for cell in world.cells:
            cell.oxygen = 0.9 if cell.terrain == Terrain.FLOOR else 0
        initial = sum(c.oxygen for c in world.cells)
        world.at(3, 0).terrain = Terrain.VACUUM
        for _ in range(40):
            world.environment(random.Random(0), 0.4)
        after_breach = sum(c.oxygen for c in world.cells)
        self.assertLess(after_breach, initial - 1)
        world.at(3, 0).terrain = Terrain.WALL
        world.at(3, 0).oxygen = 0
        after_repair = sum(c.oxygen for c in world.cells)
        for _ in range(40):
            world.environment(random.Random(0), 0.4)
        self.assertAlmostEqual(sum(c.oxygen for c in world.cells), after_repair)

    def test_airlock_blocks_gas_but_allows_walking(self):
        world = chamber()
        for y in range(1, 6):
            world.at(3, y).terrain = Terrain.WALL
        world.at(3, 3).terrain = Terrain.AIRLOCK
        world.at(2, 3).oxygen = 1
        for _ in range(20):
            world.environment(random.Random(0), 0.4)
        self.assertTrue(world.walkable(3, 3))
        self.assertEqual(world.at(4, 3).oxygen, 0)

    def test_heat_spreads_and_fire_consumes_air(self):
        world = chamber()
        world.at(3, 3).heat = 80
        world.at(3, 3).oxygen = 0.8
        world.at(3, 3).fire = 0.6
        world.environment(random.Random(4), 0.5)
        self.assertGreater(world.at(2, 3).heat, 20)
        self.assertLess(sum(c.oxygen for c in world.cells), 0.8)

    def test_fire_dies_in_vacuum(self):
        world = chamber()
        world.at(3, 3).fire = 0.7
        for _ in range(8):
            world.environment(random.Random(4), 0.5)
        self.assertEqual(world.at(3, 3).fire, 0)


class ColonyTests(unittest.TestCase):
    def setUp(self):
        self.sim = Simulation(42, 48, 32)
        self.cx, self.cy = self.sim.world.center

    def test_initial_base_has_power_pressure_and_one_colonist(self):
        self.assertEqual(len(self.sim.living), 1)
        self.assertEqual(self.sim.living[0].name, "Ada")
        self.assertEqual(len(self.sim.distress), 1)
        self.assertGreater(self.sim.pressure, 0.9)
        self.assertEqual(self.sim.generated, 12)
        self.assertEqual(self.sim.demand, 11)
        self.assertTrue(all(m.powered for m in self.sim.machines))

    def test_building_reserves_and_cancellation_refunds_exactly(self):
        before = self.sim.resources.copy()
        x, y = self.cx + 1, self.cy + 1
        ok, _ = self.sim.designate(x, y, "build", "solar")
        self.assertTrue(ok)
        self.assertEqual(self.sim.resources["metal"], before["metal"] - 14)
        self.assertFalse(self.sim.designate(x, y, "build", "solar")[0])
        self.assertTrue(self.sim.designate(x, y, "cancel")[0])
        self.assertEqual(self.sim.resources, before)
        self.assertFalse(self.sim.jobs)

    def test_unaffordable_and_invalid_orders_do_not_spend_resources(self):
        self.sim.resources["metal"] = 0
        before = self.sim.resources.copy()
        self.assertFalse(self.sim.designate(self.cx, self.cy, "build", "beacon")[0])
        self.assertFalse(self.sim.designate(-1, -1, "dig")[0])
        self.assertEqual(self.sim.resources, before)

    def test_machines_need_deck(self):
        self.assertFalse(self.sim.designate(self.cx + 10, self.cy, "build", "farm")[0])
        self.assertTrue(self.sim.designate(self.cx + 10, self.cy, "build", "floor")[0])

    def test_crew_mine_and_deliver_resources(self):
        x, y = self.cx + 12, self.cy - 5
        initial = self.sim.resources["crystal"]
        self.assertTrue(self.sim.designate(x, y, "dig")[0])
        for _ in range(120):
            self.sim.step()
            if not self.sim.jobs:
                break
        self.assertFalse(self.sim.jobs)
        self.assertEqual(self.sim.resources["crystal"], initial + 10)
        self.assertEqual(self.sim.world.at(x, y).terrain, Terrain.FLOOR)
        self.assertTrue(any(c.worked for c in self.sim.colonists))

    def test_crew_build_a_working_solar_array(self):
        x, y = self.cx, self.cy + 1
        self.assertTrue(self.sim.designate(x, y, "build", "solar")[0])
        self.sim.step(70)
        machine = self.sim.machine_at(x, y)
        self.assertIsNotNone(machine)
        self.assertEqual(machine.kind, "solar")
        self.assertTrue(machine.powered)
        self.assertGreater(self.sim.generated, 12)

    def test_pathfinding_passes_airlock_and_avoids_hull(self):
        target = (self.cx + 11, self.cy)
        path = self.sim.path_to((self.cx, self.cy), {target})
        self.assertIsNotNone(path)
        self.assertIn((self.cx + 9, self.cy), path)
        self.assertTrue(all(self.sim.world.walkable(*p) for p in path))
        self.assertIsNone(self.sim.path_to((self.cx, self.cy), {(self.cx - 9, self.cy)}))

    def test_isolated_machine_cannot_steal_base_power(self):
        x, y = self.cx + 12, self.cy + 7
        self.sim.world.at(x, y).terrain = Terrain.FLOOR
        isolated = Machine("oxygen", x, y)
        self.sim.machines.append(isolated)
        self.sim.update_power()
        self.assertFalse(isolated.powered)
        self.assertTrue(next(m for m in self.sim.machines if m.kind == "oxygen" and m is not isolated).powered)

    def test_battery_storage_stays_with_its_electrical_island(self):
        x, y = self.cx + 12, self.cy + 7
        for dx in (0, 1, 2):
            self.sim.world.at(x + dx, y).terrain = Terrain.FLOOR
        battery = Machine("battery", x, y, energy=10)
        oxygen = Machine("oxygen", x + 1, y)
        self.sim.machines += [battery, oxygen]
        base_bank = next(m for m in self.sim.machines if m.kind == "battery" and m is not battery)
        self.sim.update_power(advance=True)
        self.assertTrue(oxygen.powered)
        self.assertLess(battery.energy, 10)
        self.assertGreater(base_bank.energy, 220)
        battery.energy = 0
        self.sim.update_power()
        self.assertFalse(oxygen.powered)

    def test_power_prioritizes_life_support_when_storage_empty(self):
        for m in self.sim.machines:
            if m.kind == "battery":
                m.energy = 0
        self.sim.machines.append(Machine("beacon", self.cx, self.cy + 1))
        self.sim.update_power()
        self.assertTrue(next(m for m in self.sim.machines if m.kind == "oxygen").powered)
        self.assertFalse(next(m for m in self.sim.machines if m.kind == "beacon").powered)

    def test_power_inspection_does_not_advance_battery(self):
        initial = self.sim.charge
        for _ in range(10):
            self.sim.update_power()
        self.assertEqual(self.sim.charge, initial)

    def test_survey_reveals_new_terrain(self):
        x, y = self.cx + 15, self.cy
        self.sim.world.at(x, y).terrain = Terrain.VACUUM
        before = sum(c.discovered for c in self.sim.world.cells)
        self.assertTrue(self.sim.designate(x, y, "survey")[0])
        self.sim.step(100)
        self.assertFalse(self.sim.jobs)
        self.assertGreater(sum(c.discovered for c in self.sim.world.cells), before)

    def test_founding_charter_is_achievable(self):
        for _ in range(600):
            for survivor in self.sim.distress:
                if self.sim.expedition_for(survivor.id) is None:
                    self.sim.dispatch_expedition(survivor.id)
            self.sim.step()
        self.assertGreaterEqual(self.sim.rescued, 3)
        self.sim.tick = 960
        self.sim.machines.append(Machine("beacon", self.cx, self.cy + 1))
        self.sim.update_power()
        self.sim.step()
        self.assertTrue(self.sim.victory)
        self.assertFalse(self.sim.game_over)

    def test_no_crew_is_game_over(self):
        for c in self.sim.colonists:
            c.health = 0
        self.sim.step()
        self.assertTrue(self.sim.game_over)
        self.assertFalse(self.sim.jobs)
        tick = self.sim.tick
        self.sim.step(5)
        self.assertEqual(self.sim.tick, tick)

    def test_save_roundtrip_and_random_continuation_are_exact(self):
        self.sim.designate(self.cx + 12, self.cy - 5, "dig")
        self.sim.step(12)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "colony.json"
            self.sim.save(path)
            restored = Simulation.load(path)
            self.assertEqual(restored.to_dict(), self.sim.to_dict())
            # Cross the scripted impact to exercise saved random state as well.
            self.sim.tick = restored.tick = 530
            self.sim.step(30)
            restored.step(30)
            self.assertEqual(restored.to_dict(), self.sim.to_dict())
            self.assertFalse(path.with_suffix(".json.tmp").exists())

    def test_invalid_save_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "colony.json"
            path.write_text('{"version": 99}')
            with self.assertRaises(ValueError):
                Simulation.load(path)
            payload = self.sim.to_dict()
            payload["cells"] = []
            path.write_text(json.dumps(payload))
            with self.assertRaises(ValueError):
                Simulation.load(path)

    def test_environment_stays_finite_during_busy_colony(self):
        demo_orders(self.sim)
        self.sim.step(180)
        self.assertEqual(len(self.sim.living), 2)
        self.assertTrue(all(math.isfinite(c.oxygen) and math.isfinite(c.heat) and 0 <= c.oxygen <= 1
                            for c in self.sim.world.cells))
        self.assertTrue(all(v >= 0 for v in self.sim.resources.values()))


if __name__ == "__main__":
    unittest.main()
