from __future__ import annotations

import hashlib
import os
import shutil
import tempfile
import unittest
from pathlib import Path

os.environ.setdefault("SDL_VIDEODRIVER", "dummy")
os.environ.setdefault("SDL_AUDIODRIVER", "dummy")
import pygame

from jomon.catalog import load_content_pack, mechanical_fingerprint, select_content_pack, template_root
from jomon.commands import CharacterSetupCommand
from jomon.pygame_frontend import create_frontend
from jomon.session import GameSession

ROOT = Path(__file__).parent
PACK = ROOT.parents[0] / "jomon" / "content_packs" / "first-playable"
ATLAS = PACK / "assets" / "kenney-1-bit-pack" / "colored-transparent_packed.png"
LICENSE = PACK / "assets" / "kenney-1-bit-pack" / "LICENSE-Kenney-1-Bit-Pack-CC0-1.0.txt"
ATLAS_SHA256 = "801243b8b35bcfde727bd52447bcae5c2abf36b0ae2f3ac7ee54f91791575e74"
SETUP = CharacterSetupCommand("crew.field-member", "ancestry.baseline", "origin.maintenance", "trait.careful")


class KenneySpriteTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls) -> None:
        pygame.init()

    @classmethod
    def tearDownClass(cls) -> None:
        pygame.quit()

    def setUp(self) -> None:
        select_content_pack(PACK)

    def tearDown(self) -> None:
        select_content_pack(template_root())

    def test_vendored_kenney_atlas_and_cc0_license_are_pinned_and_bound(self) -> None:
        self.assertTrue(ATLAS.is_file())
        self.assertEqual(hashlib.sha256(ATLAS.read_bytes()).hexdigest(), ATLAS_SHA256)
        self.assertIn("Creative Commons Zero, CC0", LICENSE.read_text(encoding="utf-8"))
        assets = load_content_pack(PACK).assets
        resource = assets.resource("image.kenney-1bit-colored")
        self.assertEqual((resource.kind, resource.path), ("image", "assets/kenney-1-bit-pack/colored-transparent_packed.png"))
        for category, identity in (
            ("terrain", "terrain.wall"),
            ("terrain", "terrain.gate.closed"),
            ("features", "feature.base"),
            ("features", "feature.maintenance_latch"),
            ("features", "feature.objective_cache"),
            ("actors", "crew.initial-operative"),
            ("actors", "actor.service-defender"),
        ):
            binding = assets.binding(category, identity)
            self.assertEqual(binding["image"], resource.id)
            self.assertRegex(binding["rect"], r"^\d+,\d+,16,16$")

    def test_debug_uses_bound_sprites_while_ascii_remains_its_own_skin(self) -> None:
        session, outcome = GameSession.create_configured("kenney-sprite-draw", SETUP)
        self.assertTrue(outcome.accepted)
        debug = create_frontend(session, renderer="debug", pygame=pygame, size=(480, 320))
        self.assertEqual(debug._sprite("terrain", "terrain.wall", (26, 26)).get_size(), (26, 26))
        self.assertEqual(debug._sprite("actors", "crew.initial-operative", (22, 22)).get_size(), (22, 22))
        debug.draw()
        self.assertTrue(debug._sprite_cache)

        ascii_renderer = create_frontend(session, renderer="ascii", pygame=pygame, size=(480, 320))
        ascii_renderer.draw()
        self.assertEqual(ascii_renderer._sprite_cache, {})

    def test_asset_only_pack_change_is_mechanically_inert(self) -> None:
        baseline = load_content_pack(PACK)
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "kenney-presentation-copy"
            shutil.copytree(PACK, root)
            assets = root / "assets.json"
            text = assets.read_text(encoding="utf-8").replace('"rect": "48,96,16,16"', '"rect": "64,96,16,16"')
            assets.write_text(text, encoding="utf-8")
            changed = load_content_pack(root)
            self.assertEqual(mechanical_fingerprint(changed), mechanical_fingerprint(baseline))
            self.assertNotEqual(changed.assets.binding("terrain", "terrain.wall"), baseline.assets.binding("terrain", "terrain.wall"))


if __name__ == "__main__":
    unittest.main()
