from __future__ import annotations

import json
import os
from pathlib import Path
import tempfile
import unittest

from jomon.actions import (
    apply_damage,
    attack,
    choose_courier,
    choose_loadout,
    choose_support,
    decide_objective,
    depart,
    inspect,
    interact,
    move,
)
from jomon.content import COMMODITIES
from jomon.save import SaveError, load_game, save_game, save_path
from jomon.state import CommodityStack, Position, create_world
from jomon.world import JOMON_GANGPLANK, capacity, connected_required_map, find_tile, pressure


def prepared(seed: str = "rain quay 17", *, loadout: str = "arms", support: str = "charts"):
    state = create_world(seed)
    choose_courier(state, state.household[0].id)
    choose_loadout(state, loadout)
    choose_support(state, support)
    state.position = JOMON_GANGPLANK
    depart(state)
    return state


class GenerationTests(unittest.TestCase):
    def test_same_seed_household_and_region_are_identical(self):
        first = create_world("Hearth Rain 44")
        second = create_world("Hearth Rain 44")
        self.assertEqual(first.to_dict(), second.to_dict())

    def test_different_seed_changes_meaningful_world_facts(self):
        first = create_world("Hearth Rain 44")
        second = create_world("Willow Tide 91")
        facts = lambda state: (
            [person.name for person in state.household],
            state.region.condition,
            state.region.objective_commodity,
            state.contact.name,
            state.threat.name,
            state.region.resource_position,
        )
        self.assertNotEqual(facts(first), facts(second))

    def test_all_sampled_maps_connect_required_objectives(self):
        for index in range(50):
            with self.subTest(seed=index):
                self.assertTrue(connected_required_map(create_world(f"route seed {index}")))


class TimeAndPressureTests(unittest.TestCase):
    def test_inspection_and_cancelled_or_blocked_actions_are_zero_time(self):
        state = create_world("still water")
        before = state.to_dict()
        result = inspect(state, "cargo")
        self.assertFalse(result.time_advanced)
        self.assertEqual(state.world_time, 0)
        self.assertEqual(before, state.to_dict())
        blocked = move(state, -20, 0)
        self.assertFalse(blocked.changed)
        self.assertEqual(state.world_time, 0)

    def test_movement_and_accepted_actions_advance_once(self):
        state = prepared()
        started = state.world_time
        result = move(state, 1, 0)
        self.assertTrue(result.time_advanced)
        self.assertEqual(state.world_time, started + 1)
        state.position = find_tile(state.region.map_rows, "M")
        accepted = decide_objective(state, "accept")
        self.assertTrue(accepted.time_advanced)
        self.assertEqual(state.world_time, started + 2)

    def test_pressure_contributors_change_alert_and_pursuit(self):
        state = prepared()
        initial = pressure(state)
        state.pressure_elapsed = 80
        state.noise = 5
        state.carried_goods["paper"] = CommodityStack(2, "dry")
        state.position = Position(40, 9)
        later = pressure(state)
        self.assertGreater(later.elapsed, initial.elapsed)
        self.assertGreater(later.depth, initial.depth)
        self.assertGreater(later.noise, initial.noise)
        self.assertGreater(later.valuables, initial.valuables)
        self.assertEqual(later.band, "critical")
        self.assertGreater(later.alert_range, initial.alert_range)
        self.assertEqual(later.pursuit_steps, 2)


class TacticalTests(unittest.TestCase):
    def test_direct_combat_victory(self):
        state = prepared(loadout="arms", support="treatment")
        state.position = Position(20, 9)
        state.threat.position = Position(21, 9)
        state.threat.status = "engaged"
        state.threat.health = 4
        attack(state)
        result = attack(state)
        self.assertTrue(result.changed)
        self.assertEqual(state.threat.status, "defeated")
        self.assertTrue(any("direct combat" in event for event in state.history))

    def test_genuine_evasion_uses_quiet_route_and_environment(self):
        state = prepared(loadout="smoke", support="charts")
        state.position = find_tile(state.region.map_rows, "M")
        decide_objective(state, "accept")
        state.position = find_tile(state.region.map_rows, "&")
        lowered = interact(state)
        self.assertTrue(lowered.time_advanced)
        self.assertEqual(state.flood_control, "lowered")
        for x in range(state.position.x + 1, 34):
            result = move(state, 1, 0)
            self.assertTrue(result.changed, x)
        self.assertEqual(state.threat.status, "evaded")
        self.assertTrue(any("evaded" in event for event in state.history))

    def test_environmental_interaction_alters_objective(self):
        state = prepared(loadout="tools", support="harness")
        state.position = find_tile(state.region.map_rows, "M")
        result = decide_objective(state, "alter")
        self.assertTrue(result.changed)
        state.position = find_tile(state.region.map_rows, "&")
        interact(state)
        state.position = find_tile(state.region.map_rows, "M")
        completed = interact(state)
        self.assertTrue(completed.time_advanced)
        self.assertEqual(state.objective_status, "completed")
        self.assertTrue(any("sluice repair" in memory for memory in state.contact.memories))

    def test_nonfatal_defeat_injures_and_loses_material(self):
        state = prepared(support="charts")
        state.courier.health = 2
        state.carried_goods[state.region.objective_commodity] = CommodityStack(1, "sound")
        state.objective_status = "accepted"
        message = apply_damage(state, 3, "The cudgel")
        self.assertEqual(state.location, "jomon")
        self.assertEqual(state.courier.injury, "deep cut")
        self.assertFalse(state.carried_goods)
        self.assertEqual(state.objective_status, "failed")
        self.assertIn("injured", message)

    def test_permanent_death_selects_household_successor(self):
        state = prepared(support="charts")
        dead = state.courier
        dead.health = 1
        dead.injury = "bruised ribs"
        message = apply_damage(state, 3, "The flood")
        self.assertFalse(dead.alive)
        self.assertNotEqual(state.active_courier_id, dead.id)
        self.assertTrue(state.courier.alive)
        self.assertIn("takes up", message)
        self.assertTrue(any("succeeded" in event for event in state.history))


class EconomyAndPersistenceTests(unittest.TestCase):
    def test_capacity_and_delivery_change_market(self):
        state = prepared(loadout="tools", support="harness")
        commodity = state.region.objective_commodity
        self.assertIn(commodity, COMMODITIES)
        self.assertEqual(capacity(state), 14 if state.courier.role in {"carpenter", "guard"} else 12)
        state.position = find_tile(state.region.map_rows, "M")
        decide_objective(state, "accept")
        state.position = state.region.resource_position
        acquired = interact(state)
        self.assertTrue(acquired.changed)
        self.assertEqual(state.carried_goods[commodity].quantity, 2)
        old_demand = state.market[commodity].demand
        state.position = find_tile(state.region.map_rows, "M")
        delivered = interact(state)
        self.assertTrue(delivered.changed)
        self.assertEqual(state.objective_status, "completed")
        self.assertLess(state.market[commodity].demand, old_demand)
        self.assertGreater(state.market[commodity].stock, 0)

    def test_relationship_and_significant_memory(self):
        state = prepared()
        state.position = find_tile(state.region.map_rows, "M")
        before = state.contact.disposition
        decide_objective(state, "refuse")
        self.assertLess(state.contact.disposition, before)
        self.assertTrue(any("refused" in memory for memory in state.contact.memories))
        self.assertTrue(any("refused" in event for event in state.history))

    def test_json_save_load_equivalence_and_override(self):
        with tempfile.TemporaryDirectory() as directory:
            previous = os.environ.get("JOMON_DATA_DIR")
            os.environ["JOMON_DATA_DIR"] = directory
            try:
                state = prepared()
                target = save_game(state)
                self.assertEqual(target, Path(directory) / "jomon-save.json")
                self.assertEqual(load_game(), state)
                self.assertFalse(target.with_name(f".{target.name}.tmp").exists())
            finally:
                if previous is None:
                    del os.environ["JOMON_DATA_DIR"]
                else:
                    os.environ["JOMON_DATA_DIR"] = previous

    def test_corrupt_and_incompatible_saves_are_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            corrupt = Path(directory) / "bad.json"
            corrupt.write_text("{not-json", encoding="utf-8")
            with self.assertRaisesRegex(SaveError, "corrupt JSON"):
                load_game(corrupt)
            corrupt.write_text(json.dumps({"save_format": 999}), encoding="utf-8")
            with self.assertRaisesRegex(SaveError, "incompatible save format"):
                load_game(corrupt)

    def test_complete_reload_and_begin_changed_later_expedition(self):
        state = prepared(loadout="tools", support="charts")
        state.position = find_tile(state.region.map_rows, "M")
        decide_objective(state, "alter")
        state.position = find_tile(state.region.map_rows, "&")
        interact(state)
        state.position = find_tile(state.region.map_rows, "M")
        interact(state)
        changed_demand = state.market[state.region.objective_commodity].demand
        state.position = Position(0, 9)
        interact(state)
        self.assertEqual(state.location, "jomon")
        with tempfile.TemporaryDirectory() as directory:
            target = Path(directory) / "save.json"
            save_game(state, target)
            restored = load_game(target)
        choose_loadout(restored, "arms")
        choose_support(restored, "harness")
        restored.position = JOMON_GANGPLANK
        depart(restored)
        self.assertEqual(restored.expedition_count, 2)
        self.assertEqual(restored.flood_control, "lowered")
        self.assertEqual(restored.objective_status, "completed")
        self.assertEqual(restored.market[restored.region.objective_commodity].demand, changed_demand)


if __name__ == "__main__":
    unittest.main()
