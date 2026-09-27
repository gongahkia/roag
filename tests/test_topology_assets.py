from __future__ import annotations

from copy import deepcopy
from hashlib import sha256
import json
import os
import subprocess
import sys
from pathlib import Path
import tempfile
import unittest

from jomon.assets import action_assets, asset_resource, curses_glyph, terrain_assets
from jomon.catalog import ContentPackError, bundled_default_pack, load_content_pack
from jomon.mechanical_compatibility import main_world_mechanical_fingerprint
from jomon.semantic_topology import TAVERN_TOPOLOGY, VESSEL_TOPOLOGY, CELLS, legacy_rows, tavern_cell, vessel_cell
from jomon.state import Position, create_world
from jomon.vessel import TAVERN_MAP, VESSEL_LEVELS
from jomon.views import world_view
from jomon.world import base_tile, blocks_sight, is_walkable, semantic_cell
from tests.test_content_packs import alternate_pack


class SemanticTopologyTests(unittest.TestCase):
    def test_vessel_and_tavern_legacy_parity_is_exact(self):
        self.assertEqual({level: legacy_rows(rows) for level, rows in VESSEL_TOPOLOGY.items()}, VESSEL_LEVELS)
        self.assertEqual(legacy_rows(TAVERN_TOPOLOGY), TAVERN_MAP)
        self.assertEqual(
            {level: sha256("\n".join(rows).encode()).hexdigest() for level, rows in VESSEL_LEVELS.items()},
            {-1: "c518262a17a1efee512cce74e81aae7dcce5af860c76cec838be7260c39968df", 0: "c687efc7d3195ea0ee5f36e7158834d16c6d15189dac611bfd4b389cf1ad2d50", 1: "52a93aa4de104851b68c828c2eb20de4ebf80ab8a2a9a2370457274f04656e5f"},
        )
        self.assertEqual(sha256("\n".join(TAVERN_MAP).encode()).hexdigest(), "02fd1fd6784994556f8044de2c34ff96933ca89acecea1226d3f87145e6d2287")

    def test_semantic_cells_own_collision_and_visibility(self):
        state = create_world("semantic-topology")
        wall = next(Position(x, y, 0) for y, row in enumerate(VESSEL_LEVELS[0]) for x, token in enumerate(row) if token == "#")
        floor = next(Position(x, y, 0) for y, row in enumerate(VESSEL_LEVELS[0]) for x, token in enumerate(row) if token == ".")
        self.assertFalse(semantic_cell(state, wall).walkable)
        self.assertTrue(semantic_cell(state, wall).blocks_sight)
        self.assertFalse(is_walkable(state, wall))
        self.assertTrue(semantic_cell(state, floor).walkable)
        self.assertTrue(is_walkable(state, floor, ignore_threat=True))
        self.assertTrue(blocks_sight(state, wall))

    def test_world_view_has_semantics_without_ascii_topology(self):
        state = create_world("semantic-view")
        view = world_view(state)
        cell = next(row for row in view.cells if row.position == state.position)
        self.assertTrue(cell.terrain_id.startswith("terrain.vessel."))
        self.assertFalse(hasattr(cell, "topology_token"))


class AssetManifestTests(unittest.TestCase):
    def _pack(self):
        return bundled_default_pack()

    def test_asset_resolver_exposes_logical_resources_only(self):
        pack = self._pack()
        self.assertEqual(pack.assets.format_version, 1)
        self.assertEqual(curses_glyph("cell.vessel.floor", "."), ".")
        self.assertEqual(terrain_assets("terrain.vessel.floor")["image"], "image.terrain.floor")
        self.assertEqual(action_assets("attack.unknown")["animation"], "animation.action.attack")
        self.assertEqual(asset_resource("image.terrain.floor").path, "media/developer_tile.png")

    def test_manifest_rejects_duplicate_unknown_unsafe_and_missing_assets(self):
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "assets.json"
            original = source.read_text(encoding="utf-8")
            cases = {
                "duplicate": original.replace('"asset_manifest_format": 1', '"asset_manifest_format": 1, "asset_manifest_format": 1'),
                "format": original.replace('"asset_manifest_format": 1', '"asset_manifest_format": 2'),
                "unsafe": original.replace('"kind": "image"', '"kind": "image", "path": "../escape.png"', 1),
                "missing": original.replace('"image.terrain.floor"', '"image.not_declared"', 1),
            }
            parsed = json.loads(original)
            parsed["glyphs"].pop("cell.vessel.floor")
            cases["glyph missing"] = json.dumps(parsed)
            parsed = json.loads(original)
            parsed["glyphs"]["cell.unknown"] = "?"
            cases["glyph unknown"] = json.dumps(parsed)
            for name, document in cases.items():
                with self.subTest(name=name):
                    source.write_text(document, encoding="utf-8")
                    with self.assertRaises(ContentPackError):
                        load_content_pack(root)
                    source.write_text(original, encoding="utf-8")

    def test_asset_changes_are_presentation_only(self):
        baseline = main_world_mechanical_fingerprint()
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            source = root / "assets.json"
            data = json.loads(source.read_text(encoding="utf-8"))
            data["glyphs"]["cell.vessel.floor"] = ":"
            data["bindings"]["terrain"]["terrain.vessel.floor"]["image"] = "image.actor.default"
            source.write_text(json.dumps(data, indent=2, sort_keys=True) + "\n", encoding="utf-8")
            alternate = load_content_pack(root)
            self.assertEqual(alternate.assets.glyph("cell.vessel.floor"), ":")
            # The mechanical catalog root is identical; media never participates in its fingerprint.
            self.assertEqual(main_world_mechanical_fingerprint(), baseline)

    def test_alternate_glyphs_leave_headless_mechanics_and_events_unchanged(self):
        script = (
            "import json; from jomon.commands import MoveCommand; from jomon.session import GameSession; "
            "from jomon.world import is_walkable, curses_tile; from jomon.state import Position; "
            "from jomon.mechanical_compatibility import main_world_mechanical_fingerprint; "
            "s=GameSession.create('asset-topology-proof'); p=s._state.position; "
            "dx,dy=next((dx,dy) for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)) if is_walkable(s._state,Position(p.x+dx,p.y+dy,p.z))); "
            "o=s.submit(MoveCommand(dx,dy)); print(json.dumps({'glyph':curses_tile(s._state,s._state.position),'fp':main_world_mechanical_fingerprint(),'events':[e.event_id for e in o.events],'position':[s._state.position.x,s._state.position.y,s._state.position.z]}))"
        )
        base = subprocess.run([sys.executable, '-c', script], text=True, capture_output=True, check=True).stdout
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / 'fixture')
            # The shared fixture deliberately mutates seed words; restore mechanics for this asset-only proof.
            (root / 'data' / 'world_text.json').write_text((Path(__file__).resolve().parents[1] / 'jomon' / 'data' / 'world_text.json').read_text(encoding='utf-8'), encoding='utf-8')
            source = root / 'assets.json'; data = json.loads(source.read_text(encoding='utf-8'))
            data['glyphs']['cell.vessel.floor'] = ':'
            data['bindings']['events']['combat.damage.applied']['audio'] = 'audio.action.attack'
            source.write_text(json.dumps(data, sort_keys=True, indent=2) + '\n', encoding='utf-8')
            env = {**os.environ, 'JOMON_CONTENT_PACK': str(root)}
            changed = subprocess.run([sys.executable, '-c', script], text=True, capture_output=True, check=True, env=env).stdout
        first, second = json.loads(base), json.loads(changed)
        self.assertEqual(first['fp'], second['fp'])
        self.assertEqual(first['events'], second['events'])
        self.assertEqual(first['position'], second['position'])
        self.assertNotEqual(first['glyph'], second['glyph'])


if __name__ == "__main__":
    unittest.main()
