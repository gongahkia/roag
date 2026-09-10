import unittest
import random
from types import SimpleNamespace

from jomon.inventory import (
    _largest_free_area, best_fit, grid_items, grid_size, item_spec,
    occupied_cells, placement_preview,
)
from jomon.state import Item, Position, Threat, VerticalLink, create_world

from jomon.benchmark import benchmark, distribution


class MeasurementTests(unittest.TestCase):
    def test_percentiles_use_nearest_rank_and_do_not_hide_outliers(self):
        result = distribution(list(range(1, 101)))
        self.assertEqual(result["median_ms"], 50.5)
        self.assertEqual(result["p95_ms"], 95)
        self.assertEqual(result["p99_ms"], 99)
        self.assertEqual(result["worst_ms"], 100)

    def test_empty_measurements_are_not_reported_as_fast(self):
        with self.assertRaises(ValueError):
            distribution([])

    def test_benchmark_names_lazy_generation_and_revision(self):
        result = benchmark(1, "focused benchmark contract")
        self.assertTrue(result["commit"])
        self.assertEqual(result["lazy_frontier_entry"]["n"], 1)


def reference_free(width, height, occupied):
    remaining = {(x, y) for x in range(width) for y in range(height)} - occupied
    largest = 0
    while remaining:
        queue = [remaining.pop()]
        count = 0
        while queue:
            x, y = queue.pop()
            count += 1
            for point in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                if point in remaining:
                    remaining.remove(point)
                    queue.append(point)
        largest = max(largest, count)
    return largest


class PackingEquivalenceTests(unittest.TestCase):
    def test_bitset_components_equal_independent_flood_fill(self):
        rng = random.Random(8103)
        for width, height in ((1, 1), (1, 8), (8, 1), (10, 6), (18, 10)):
            for _ in range(50):
                occupied = {(x, y) for x in range(width) for y in range(height) if rng.randrange(3) == 0}
                self.assertEqual(_largest_free_area(width, height, occupied), reference_free(width, height, occupied))

    def test_scoring_retains_category_rotation_and_position_ties(self):
        rng = random.Random(908)
        for index in range(20):
            peers = [Item(f"p{n}", "passive:reed sole wraps", "locker", "fixture", x=n * 2, y=index % 3) for n in range(3)]
            item = Item("candidate", "rope", "ground", "fixture", rotated=bool(rng.randrange(2)))
            state = SimpleNamespace(items=[*peers, item], locker_width=8, locker_height=5, terrain_statuses={}, active_courier_id="none")
            width, height = grid_size(state, "locker")
            base = set().union(*(occupied_cells(peer) for peer in peers))
            spec = item_spec(item.kind)
            candidates = []
            for rotated in (False, True):
                for y in range(height):
                    for x in range(width):
                        preview = placement_preview(state, item, "locker", x, y, rotated=rotated)
                        if not preview.valid:
                            continue
                        adjacent = sum(
                            1 for peer in grid_items(state, "locker", exclude=item.id)
                            if item_spec(peer.kind).category == spec.category
                            for cx, cy in preview.cells
                            if any((cx + dx, cy + dy) in occupied_cells(peer) for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)))
                        )
                        score = (-reference_free(width, height, base | set(preview.cells)), -adjacent, 1, y, x, int(rotated))
                        candidates.append((score, (x, y, rotated)))
            self.assertEqual(best_fit(state, item, "locker"), min(candidates)[1])


class SpatialCacheTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.world = create_world("cache invalidation")

    def setUp(self):
        import copy
        self.state = copy.deepcopy(self.world)
        self.state.location = "region"
        self.state.position = Position(30, 20)
        for y in range(15, 27):
            for x in range(20, 41):
                self.state.region.tile_changes[f"{x},{y},0"] = "."

    def test_sight_cache_invalidates_on_smoke_terrain_and_remains_unsaved(self):
        from jomon.world import field_of_view
        state = self.state
        target = Position(34, 20)
        self.assertIn(target, field_of_view(state, remember=False))
        state.smoke["32,20,0"] = 4
        self.assertNotIn(target, field_of_view(state, remember=False))
        state.smoke.clear()
        self.assertIn(target, field_of_view(state, remember=False))
        state.region.tile_changes["32,20,0"] = "#"
        self.assertNotIn(target, field_of_view(state, remember=False))
        self.assertNotIn("_fov_cache", state.to_dict())

    def test_cached_path_matches_reconstruction_and_respects_changed_obstacles(self):
        from jomon.enemy_ai import next_path_step
        state = self.state
        actor = Threat("cache-patrol", "patrol", "pursuer", Position(23, 20), 4, 4)
        state.threats = [actor]
        target = Position(35, 20)
        for _ in range(6):
            cached = next_path_step(state, actor, target)
            state.__dict__.pop("_path_cache", None)
            self.assertEqual(cached, next_path_step(state, actor, target))
            actor.position = cached
        blocked = next_path_step(state, actor, target)
        state.region.tile_changes[f"{blocked.x},{blocked.y},{blocked.z}"] = "#"
        self.assertNotEqual(next_path_step(state, actor, target), blocked)
        self.assertNotIn("_path_cache", state.to_dict())

    def test_bitset_reachability_matches_independent_queue_across_levels(self):
        from collections import deque
        from jomon.regions import region_reachable
        rng = random.Random(17)
        for _ in range(30):
            width, height = 9, 7
            levels = {str(z): ["".join("#" if rng.randrange(4) == 0 else "." for x in range(width)) for y in range(height)] for z in (-1, 0, 1, 2)}
            links = [VerticalLink(Position(4, 3, z), Position(4, 3, z + 1), "test stair") for z in (-1, 0, 1)]
            changes = {f"4,3,{z}": ">" for z in (-1, 0, 1, 2)}
            region = SimpleNamespace(width=width, height=height, levels=levels, vertical_links=links, tile_changes=changes, landmarks={"landing": Position(4, 3)})
            start = region.landmarks["landing"]
            queue, expected = deque([start]), {start}
            while queue:
                p = queue.popleft()
                candidates = [Position(p.x + dx, p.y + dy, p.z) for dx, dy in ((0, 1), (0, -1), (1, 0), (-1, 0))]
                for link in links:
                    if p == link.first:
                        candidates.append(link.second)
                    if p == link.second:
                        candidates.append(link.first)
                for point in candidates:
                    if point in expected or not (0 <= point.x < width and 0 <= point.y < height):
                        continue
                    tile = changes.get(f"{point.x},{point.y},{point.z}", levels[str(point.z)][point.y][point.x])
                    if tile != "#":
                        expected.add(point)
                        queue.append(point)
            self.assertEqual(region_reachable(region), expected)

    def test_reachability_cache_is_bounded_and_invalidates_sparse_changes(self):
        from jomon.regions import (
            _REACHABLE_CACHE, _REACHABLE_CACHE_LIMIT, region_reachable,
        )
        region = self.state.region
        _REACHABLE_CACHE.clear()
        first = region_reachable(region)
        size = len(_REACHABLE_CACHE)
        self.assertEqual(region_reachable(region), first)
        self.assertEqual(len(_REACHABLE_CACHE), size)
        target = next(point for point in first if point != region.landmarks["landing"])
        region.tile_changes[f"{target.x},{target.y},{target.z}"] = "#"
        self.assertNotIn(target, region_reachable(region))
        self.assertGreater(len(_REACHABLE_CACHE), size)
        self.assertLessEqual(len(_REACHABLE_CACHE), _REACHABLE_CACHE_LIMIT)
