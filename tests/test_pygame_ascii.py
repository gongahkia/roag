from __future__ import annotations

import importlib.util
import os
from pathlib import Path
import tempfile
import unittest


HAS_PYGAME = importlib.util.find_spec("pygame") is not None


@unittest.skipUnless(HAS_PYGAME, "pygame-ce is required for Pygame frontend tests")
class AsciiPygameFrontendTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        os.environ.setdefault("SDL_VIDEODRIVER", "dummy")
        os.environ.setdefault("SDL_AUDIODRIVER", "dummy")
        import pygame

        pygame.init()
        cls.pygame = pygame

    @classmethod
    def tearDownClass(cls):
        cls.pygame.quit()

    def frontend(self):
        from jomon.pygame_ascii import AsciiPygameFrontend
        from jomon.session import GameSession

        return AsciiPygameFrontend(GameSession.create("ascii-frontend"), pygame=self.pygame, size=(640, 480))

    def test_semantic_ascii_map_and_font_fallback_render_headlessly(self):
        frontend = self.frontend()
        frontend.draw()
        cell = frontend.session.world_view().cells[0]
        self.assertFalse(hasattr(cell, "glyph"))
        self.assertTrue(frontend._cell_glyph(cell))
        self.assertIn(frontend.font_stack.resolution.text_source, {"BigBlueTerm", "monospace fallback", "explicit"})
        self.assertTrue(frontend.font_stack.icon("quest"))

    def test_ascii_uses_the_shared_commands_panels_and_save_format(self):
        from jomon.commands import MoveCommand
        from jomon.session import GameSession

        frontend = self.frontend()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            outcome = frontend.submit(MoveCommand(dx, dy))
            if outcome.changed:
                break
        self.assertTrue(outcome.changed)
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_i, mod=0))
        self.assertEqual(frontend.panel, "inventory")
        frontend.draw()
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "ascii-save.json"
            frontend.session.save(path)
            loaded = GameSession.load(path)
        self.assertEqual(loaded.world_view(), frontend.session.world_view())

    def test_renderer_factory_keeps_graphical_and_ascii_over_one_session_api(self):
        from jomon.pygame_frontend import create_frontend
        from jomon.session import GameSession

        graphical = create_frontend(GameSession.create("factory"), pygame=self.pygame)
        ascii_frontend = create_frontend(GameSession.create("factory"), renderer="ascii", pygame=self.pygame)
        self.assertEqual(graphical.renderer_id, "graphical")
        self.assertEqual(ascii_frontend.renderer_id, "ascii")


if __name__ == "__main__":
    unittest.main()
