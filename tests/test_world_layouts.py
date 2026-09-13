import tempfile
import unittest
from pathlib import Path

from jomon.actions import interact
from jomon.content import PASSIVES, RELICS
from jomon.discoveries import reveal_nearby
from jomon.frontiers import FRONTIERS, build_frontier
from jomon.geography import FIELD_SECRETS, LAYOUTS, layout_point
from jomon.inventory import item_spec
from jomon.regions import activate_region, region_reachable, validate_region
from jomon.save import load_game, save_game
from jomon.state import Position, create_world
from jomon.world import displayed_tile


class WorldLayoutTests(unittest.TestCase):
    def test_seeded_macro_layouts_move_sites_and_preserve_routes(self):
        layouts = set()
        landing_routes = set()
        for index in range(16):
            seed = f"macro world {index}"
            state = create_world(seed)
            region = state.region
            layouts.add(region.changes["macro_layout"])
            landing, contact = region.landmarks["landing"], region.landmarks["contact"]
            landing_routes.add((landing.x < contact.x, landing.y < contact.y))
            if index < 4:
                self.assertEqual(region.changes["macro_layout"], create_world(seed).region.changes["macro_layout"])
            self.assertTrue(set(region.landmarks.values()) <= region_reachable(region))
            self.assertEqual(len([box for box in region.containers if box.hidden]), 2)
        self.assertEqual(layouts, set(LAYOUTS))
        self.assertGreaterEqual(len(landing_routes), 3)

    def test_every_region_has_reachable_discoveries_and_moved_process_sites(self):
        for region_id in ("hearthford", "greywash", "greenwold", "whitecairn", *FRONTIERS):
            for index in range(4):
                seed = f"regional macro {region_id} {index}"
                state = create_world(seed)
                region = state.regions.get(region_id) or build_frontier(seed, region_id)
                with self.subTest(region=region_id, seed=index):
                    validate_region(region)
                    hidden = [box for box in region.containers if box.hidden]
                    self.assertEqual(len(hidden), 2)
                    self.assertTrue(all(box.position in region_reachable(region) for box in hidden))
                    self.assertTrue(all(not box.discovered and box.clue for box in hidden))
                    self.assertEqual(
                        layout_point(region, Position(12, 12)),
                        Position(region.width - 13 if "east" in region.changes["macro_layout"] else 12,
                                 region.height - 13 if "south" in region.changes["macro_layout"] else 12),
                    )

    def test_all_regions_have_alternate_landings_and_packable_secret_rewards(self):
        landings = {region_id: set() for region_id in FIELD_SECRETS}
        for index in range(12):
            seed = f"alternate quay {index}"
            state = create_world(seed)
            regions = {**state.regions, **{region_id: build_frontier(seed, region_id) for region_id in FRONTIERS}}
            for region_id, region in regions.items():
                landings[region_id].add(layout_point(region, region.landmarks["landing"]).y)
        self.assertTrue(all(len(rows) >= 2 for rows in landings.values()), landings)
        for rows in FIELD_SECRETS.values():
            for _, _, _, reward, _, _ in rows:
                try:
                    item_spec(reward)
                except KeyError:
                    prefix = "passive:" if reward in PASSIVES else "relic:" if reward in RELICS else "consumable:"
                    item_spec(prefix + reward)

    def test_field_trace_reveals_physical_cache_and_survives_save(self):
        state = create_world("field trace persistence")
        state.location = "region"
        cache = next(box for box in state.region.containers if box.hidden)
        self.assertNotEqual(displayed_tile(state, cache.position), "C")
        state.position = cache.position
        wayfinding = state.courier.wayfinding
        self.assertTrue(reveal_nearby(state))
        self.assertTrue(cache.discovered)
        self.assertEqual(state.courier.wayfinding, min(20, wayfinding + 1))
        self.assertIn(f"{cache.position.x},{cache.position.y},{cache.position.z}", state.region.seen)
        state.gear = "repair tools" if cache.requirement == "key" else "rope" if cache.requirement == "rope" else "hooded lantern"
        result = interact(state)
        self.assertTrue(result.time_advanced)
        self.assertTrue(cache.opened)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "world.json"
            save_game(state, path)
            loaded = load_game(path)
        restored = next(box for box in loaded.region.containers if box.id == cache.id)
        self.assertTrue(restored.discovered)
        self.assertTrue(restored.opened)
        self.assertTrue(loaded.region.changes["field_discovery_trained"])
        self.assertEqual(loaded.region.changes["macro_layout"], state.region.changes["macro_layout"])


if __name__ == "__main__":
    unittest.main()
