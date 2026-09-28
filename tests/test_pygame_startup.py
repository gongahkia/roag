from __future__ import annotations

import os
import unittest
from pathlib import Path
from unittest.mock import patch

os.environ.setdefault("SDL_VIDEODRIVER", "dummy")
os.environ.setdefault("SDL_AUDIODRIVER", "dummy")

import pygame

from jomon.catalog import select_content_pack, template_root
from jomon.pygame_frontend import create_frontend, main


PACK = Path(__file__).parents[1] / "jomon" / "content_packs" / "first-playable"


class PygameStartupTests(unittest.TestCase):
    def tearDown(self) -> None:
        pygame.quit()

    def test_frontend_initializes_pygame_and_fonts_when_started_directly(self) -> None:
        pygame.quit()
        app = create_frontend(None, renderer="ascii", pygame=pygame)
        self.assertTrue(pygame.get_init())
        self.assertTrue(pygame.font.get_init())
        self.assertIsNotNone(app.font)

    def test_new_cli_routes_to_setup_without_creating_a_world(self) -> None:
        select_content_pack(PACK)
        captured = {}

        class Shell:
            def run(self):
                return None

        def frontend(session, **kwargs):
            captured["session"] = session
            captured.update(kwargs)
            return Shell()

        try:
            with patch("jomon.pygame_frontend.create_frontend", side_effect=frontend):
                main(["--new", "--seed", "new-routing"])
            self.assertIsNone(captured["session"])
            self.assertEqual(captured["shell_mode"], "setup")
            self.assertEqual(captured["seed"], "new-routing")
        finally:
            select_content_pack(template_root())


if __name__ == "__main__":
    unittest.main()
