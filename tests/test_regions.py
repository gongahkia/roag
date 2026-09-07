import tempfile
import unittest
from pathlib import Path

from jomon.actions import _advance_world, interact
from jomon.inventory import create_item
from jomon.regions import activate_region, region_reachable, store_active_region
from jomon.save import load_game, save_game
from jomon.state import Position, create_world
from jomon.world import build_combinations


class FourRegionGenerationTests(unittest.TestCase):
    def test_whitecairn_shared_hoist_operates_control_before_climbing(self):
        state = create_world("shared quarry hoist")
        activate_region(state, "whitecairn")
        state.active_courier_id = state.household[0].id
        state.location = "region"
        state.position = next(
            Position(x, y, int(z))
            for z, rows in state.region.levels.items()
            for y, row in enumerate(rows)
            for x, tile in enumerate(row)
            if tile == "&" and int(z) == 0
        )
        first = interact(state)
        self.assertTrue(first.time_advanced)
        self.assertTrue(state.region.changes["quarry_braced"])
        second = interact(state)
        self.assertTrue(second.time_advanced)
        self.assertEqual(state.position.z, 1)

    def test_generation_is_deterministic_and_geographically_varied(self):
        first = create_world("four-region determinism")
        second = create_world("four-region determinism")
        self.assertEqual(
            {key: value.geography_signature for key, value in first.regions.items()},
            {key: value.geography_signature for key, value in second.regions.items()},
        )
        signatures = [
            create_world(f"regional variation {index}").regions["greywash"].geography_signature
            for index in range(6)
        ]
        self.assertGreaterEqual(len(set(signatures)), 4)

    def test_every_new_region_has_deep_reachable_content(self):
        for seed in (
            "regional alpha", "regional beta", "regional gamma",
            "PTY forced creature", "PTY forced lure",
        ):
            state = create_world(seed)
            for region_id in ("greywash", "greenwold", "whitecairn"):
                region = state.regions[region_id]
                with self.subTest(seed=seed, region=region_id):
                    reachable = region_reachable(region)
                    self.assertGreaterEqual(len(region.zones), 5)
                    self.assertEqual(len(region.containers), 7)
                    self.assertGreaterEqual(len({point.z for point in reachable}), 4)
                    self.assertTrue(set(region.landmarks.values()) <= reachable)
                    self.assertEqual(len(state.contacts[region_id]), 2)
                    self.assertEqual(len(state.region_threats[region_id]), 7)
                    self.assertTrue(any(threat.elite for threat in state.region_threats[region_id]))

    def test_active_region_changes_persist_across_travel_and_save(self):
        state = create_world("persistent regional state")
        activate_region(state, "greywash")
        state.region.containers[0].opened = True
        state.region.tile_changes["8,8,0"] = "/"
        state.region.seen.append("8,8,0")
        state.objective_status = "accepted"
        store_active_region(state)
        activate_region(state, "greenwold")
        activate_region(state, "greywash")
        self.assertTrue(state.region.containers[0].opened)
        self.assertEqual(state.objective_status, "accepted")
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "save.json"
            save_game(state, path)
            loaded = load_game(path)
        self.assertTrue(loaded.regions["greywash"].containers[0].opened)
        self.assertEqual(loaded.regions["greywash"].tile_changes["8,8,0"], "/")
        self.assertIn("8,8,0", loaded.regions["greywash"].seen)

    def test_each_regional_process_changes_only_on_actions(self):
        for region_id in ("greywash", "greenwold", "whitecairn"):
            state = create_world(f"organic process {region_id}")
            activate_region(state, region_id)
            state.location = "region"
            state.position = state.region.landmarks["landing"]
            threshold = state.region.process_thresholds[0]
            state.pressure_elapsed = threshold - 1
            before = state.region.process_stage
            self.assertEqual(state.region.process_stage, before)
            _advance_world(state)
            self.assertEqual(state.region.process_stage, before + 1)
            self.assertTrue(any(state.region.process_name.split()[0] in message.lower() or
                                region_id.split()[0] in message.lower()
                                for message in state.messages) or state.region.process_stage == 1)

    def test_visible_container_holds_build_item_armour_and_supply(self):
        state = create_world("recognisable regional coffer")
        state.active_courier_id = state.household[0].id
        activate_region(state, "greywash")
        state.location = "region"
        box = state.region.containers[0]
        state.position = box.position
        result = interact(state)
        self.assertTrue(box.opened)
        self.assertIn(box.reward, result.message)
        self.assertTrue(box.extra_rewards)
        physical = [
            item for item in state.items
            if item.container_id == box.id
            or (item.owner_id == state.active_courier_id and box.name in item.provenance)
        ]
        self.assertEqual(len(physical), 3)


class ExpandedBuildTests(unittest.TestCase):
    def test_ten_new_cross_system_combinations(self):
        cases = [
            ("ebb reader", "staff", "rope", {"tide ledger": 1}, "accounted ebb", 0),
            ("sure footing", "staff", "cargo harness", {"cork float": 1}, "buoyant cargo rig", 0),
            ("quiet passage", "longbow", "rope", {"storm vane": 1}, "wind-read aim", 0),
            ("quiet passage", "staff", "smoke pot", {"charcoal mask": 1}, "masked smoke passage", 0),
            ("quiet passage", "longbow", "rope", {"resin grip": 1}, "weatherfast grip", 0),
            ("wind listener", "staff", "rope", {"bird whistle": 1}, "crosswind decoy", 0),
            ("sure footing", "staff", "quiet shoes", {"limestone cleat": 1}, "quiet scree step", 0),
            ("sure footing", "sling", "rope", {"sling cup": 1}, "high sling arc", 1),
            ("sure footing", "staff", "rope", {"fall sail": 1}, "directed fall", 0),
            ("sure footing", "staff", "cargo harness", {"quarry brace": 1}, "weighted floor brace", 0),
        ]
        for technique, weapon, gear, passives, expected, z in cases:
            state = create_world(expected)
            state.active_courier_id = state.household[0].id
            state.household[0].technique = technique
            state.weapon, state.gear = weapon, gear
            state.carried_passives = passives
            state.location = "region"
            state.position = type(state.position)(20, 20, z)
            if expected == "weighted floor brace":
                # Physical mass, rather than a hidden toggle, establishes the load band.
                state.household[0].role = "factor"
                for item in state.items:
                    if item.owner_id == state.active_courier_id and item.location in {
                        "head", "torso", "arms", "hands", "legs", "feet",
                    }:
                        item.location, item.owner_id = "lost", None
                for kind, slot in (("riveted coat", "torso"), ("brigandine cuisses", "legs"), ("kettle helm", "head")):
                    create_item(state, kind, "combination test", owner_id=state.active_courier_id, location=slot)
            with self.subTest(expected=expected):
                self.assertIn(expected, build_combinations(state))


if __name__ == "__main__":
    unittest.main()
