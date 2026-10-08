from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from roag.frontiers import FRONTIERS, build_frontier
from roag.regions import begin_region, region_reachable
from roag.save import load_game, save_game
from roag.state import MaterialCell, Position, SAVE_FORMAT, create_world
from roag.terrain import REGIONAL_TERRAIN, terrain_at, terrain_from_glyph
from roag.views import world_view
from roag.world import blocks_sight, displayed_tile, is_walkable, position_key


BLOCKED = {" ", "#", "~", "T"}
SIGHT_BLOCKING = {"#", "T", "+"}
PARTIAL_COVER = {"#", "T", "+"}
LOW_COVER = {"%"}
MUTATION_GLYPHS = {".", "%", "=", "O", "/", ">", "<", "*", "?", "!", "C", "m", ";", "+"}


def generated_regions(seed: str):
    state = create_world(seed)
    regions = dict(state.regions)
    regions.update({region_id: build_frontier(seed, region_id) for region_id in FRONTIERS})
    return regions


class RegionalTerrainCatalogTests(unittest.TestCase):
    def test_generated_and_mutated_glyphs_have_explicit_legacy_parity(self):
        for seed in ("terrain-catalog-a", "terrain-catalog-b"):
            for region_id, region in generated_regions(seed).items():
                glyphs = {
                    glyph
                    for rows in region.levels.values()
                    for row in rows
                    for glyph in row
                } | set(region.tile_changes.values()) | MUTATION_GLYPHS
                for glyph in glyphs:
                    with self.subTest(seed=seed, region=region_id, glyph=glyph):
                        definition = terrain_from_glyph(glyph, region_id)
                        self.assertNotIn("legacy", definition.tags)
                        self.assertEqual(definition.glyph, glyph)
                        self.assertEqual(definition.walkable, glyph not in BLOCKED)
                        self.assertEqual(definition.blocks_sight, glyph in SIGHT_BLOCKING)
                        self.assertEqual(
                            definition.cover,
                            "partial" if glyph in PARTIAL_COVER else "low" if glyph in LOW_COVER else "open",
                        )

    def test_region_context_gives_ground_a_stable_semantic_identity(self):
        hearthford = terrain_from_glyph(".", "hearthford")
        greywash = terrain_from_glyph(".", "greywash")
        self.assertEqual(hearthford.glyph, greywash.glyph)
        self.assertEqual(
            (hearthford.walkable, hearthford.blocks_sight, hearthford.cover),
            (greywash.walkable, greywash.blocks_sight, greywash.cover),
        )
        self.assertEqual(hearthford.id, "terrain.region.hearthford.ground")
        self.assertEqual(greywash.id, "terrain.region.greywash.ground")
        self.assertNotEqual(hearthford.id, greywash.id)
        self.assertEqual(terrain_from_glyph(".").id, "terrain.region.ground")

    def test_unknown_historical_glyph_has_deterministic_permissive_fallback(self):
        first = terrain_from_glyph("@", "hearthford")
        second = terrain_from_glyph("@", "frostmere")
        self.assertIs(first, second)
        self.assertEqual(first.id, "terrain.region.legacy.0040")
        self.assertTrue(first.walkable)
        self.assertFalse(first.blocks_sight)
        self.assertEqual(first.cover, "open")
        self.assertEqual(first.tags, ("legacy", "unknown"))
        with self.assertRaises(ValueError):
            terrain_from_glyph("too long", "hearthford")

    def test_catalog_mappings_are_read_only(self):
        with self.assertRaises(TypeError):
            REGIONAL_TERRAIN.definitions["@"] = terrain_from_glyph("@")


class RegionalTerrainIntegrationTests(unittest.TestCase):
    def setUp(self):
        self.state = create_world("semantic regional terrain")
        begin_region(self.state, "hearthford")
        self.state.threats = []
        self.state.region_threats["hearthford"] = self.state.threats
        self.state.circuits = {}
        self.point = next(
            Position(x, y, int(z))
            for z, rows in self.state.region.levels.items()
            if int(z) == self.state.position.z
            for y, row in enumerate(rows)
            for x, glyph in enumerate(row)
            if glyph == "."
        )

    def test_world_queries_match_legacy_glyph_rules(self):
        coordinate = position_key(self.point)
        for glyph in sorted(set(REGIONAL_TERRAIN.definitions) | {"!"}):
            with self.subTest(glyph=glyph):
                self.state.region.tile_changes[coordinate] = glyph
                self.state.region.materials.pop(coordinate, None)
                self.state.smoke.pop(coordinate, None)
                self.assertEqual(
                    is_walkable(self.state, self.point, ignore_threat=True),
                    glyph not in BLOCKED,
                )
                self.assertEqual(blocks_sight(self.state, self.point), glyph in SIGHT_BLOCKING)

        self.state.region.tile_changes[coordinate] = "~"
        self.state.region.materials[coordinate] = MaterialCell(water=1, ice=True)
        self.assertTrue(is_walkable(self.state, self.point, ignore_threat=True))
        self.assertFalse(blocks_sight(self.state, self.point))

    def test_sparse_changes_and_display_overlays_do_not_replace_base_semantics(self):
        coordinate = position_key(self.point)
        self.state.region.tile_changes[coordinate] = "%"
        self.assertEqual(terrain_at(self.state.region, self.point).id, "terrain.region.loose_cover")
        self.state.region.tile_changes[coordinate] = "."
        self.state.water[coordinate] = 1
        self.assertNotEqual(displayed_tile(self.state, self.point), ".")
        self.assertEqual(terrain_at(self.state.region, self.point).glyph, ".")
        self.assertEqual(
            terrain_at(self.state.region, Position(-1, -1, 0)).id,
            "terrain.region.void",
        )

    def test_world_view_uses_semantic_terrain_id_instead_of_token_identity(self):
        view = world_view(self.state)
        cell = next(cell for cell in view.cells if cell.position == self.point)
        self.assertEqual(cell.terrain_id, terrain_at(self.state.region, self.point).id)
        self.assertFalse(cell.terrain_id.startswith("terrain.region.token."))

    def test_glyph_save_round_trip_needs_no_migration(self):
        coordinate = position_key(self.point)
        levels = self.state.region.levels.copy()
        self.state.region.tile_changes[coordinate] = "%"
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "terrain-format-15.json"
            save_game(self.state, path)
            restored = load_game(path)

        self.assertEqual(restored.save_format, SAVE_FORMAT)
        self.assertEqual(restored.region.levels, levels)
        self.assertEqual(restored.region.tile_changes[coordinate], "%")
        self.assertEqual(terrain_at(restored.region, self.point).id, "terrain.region.loose_cover")
        self.assertFalse(any(
            key in restored.to_dict()
            for key in ("terrain_catalog", "terrain_definitions", "typed_terrain")
        ))

    def test_region_reachability_remains_glyph_compatible(self):
        before = region_reachable(self.state.region)
        target = next(point for point in before if point != self.state.region.landmarks["landing"])
        coordinate = position_key(target)
        original = self.state.region.tile_changes.get(coordinate)
        self.state.region.tile_changes[coordinate] = "#"
        self.assertNotIn(target, region_reachable(self.state.region))
        if original is None:
            del self.state.region.tile_changes[coordinate]
        else:
            self.state.region.tile_changes[coordinate] = original
        self.assertEqual(region_reachable(self.state.region), before)


if __name__ == "__main__":
    unittest.main()
