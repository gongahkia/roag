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


    def test_ascii_shared_controller_reaches_activity_travel_draw_and_dice(self):
        """The terminal-styled renderer consumes the same panels and commands."""
        from jomon.production import site_position
        from jomon.vessel import DICE_PLAYER_SEAT, DRAW_PLAYER_SEAT

        frontend = self.frontend()
        state = frontend.session._state  # Test fixture setup; frontend code has no such access.
        state.location, state.position = "region", site_position(state)
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_c, mod=0))
        self.assertEqual(frontend.panel, "activity")
        frontend.panel_cursor = next(index for index, row in enumerate(frontend._panel_rows())
                                     if row.action_id == "production.gather:0")
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_RETURN, mod=0))
        self.assertEqual(frontend.last_result, "activity.resolved")
        frontend.draw()

        state.location, frontend.panel = "jomon", None
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_t, mod=0))
        frontend.panel_cursor = next(index for index, row in enumerate(frontend._panel_rows()) if row.available)
        frontend.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_RETURN, mod=0))
        self.assertEqual(frontend.last_result, "travel.resolved")

        draw = self.frontend()
        draw.session._state.jomon_space, draw.session._state.position = "tavern", DRAW_PLAYER_SEAT
        draw.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_e, mod=0))
        self.assertEqual(draw.panel, "tavern-draw")
        draw.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_RETURN, mod=0))
        self.assertEqual(draw.last_result, "tavern.game.started")
        draw.draw()

        dice = self.frontend()
        dice.session._state.jomon_space, dice.session._state.position = "tavern", DICE_PLAYER_SEAT
        dice.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_e, mod=0))
        self.assertEqual(dice.panel, "tavern-dice")
        dice.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_RETURN, mod=0))
        dice.handle_event(self.pygame.event.Event(self.pygame.KEYDOWN, key=self.pygame.K_r, mod=0))
        self.assertEqual(dice.last_result, "tavern.dice.resolved")
        dice.draw()


    def test_cross_renderer_format_15_save_round_trip(self):
        from jomon.commands import MoveCommand
        from jomon.pygame_frontend import create_frontend
        from jomon.session import GameSession

        graphical = create_frontend(GameSession.create("cross-renderer"), pygame=self.pygame)
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if graphical.submit(MoveCommand(dx, dy)).changed:
                break
        with tempfile.TemporaryDirectory() as directory:
            first = Path(directory) / "graphical.json"
            second = Path(directory) / "ascii.json"
            graphical.session.save(first)
            ascii_frontend = create_frontend(GameSession.load(first), renderer="ascii", pygame=self.pygame)
            ascii_frontend.draw()
            ascii_frontend.session.save(second)
            restored = GameSession.load(second)
        self.assertEqual(restored.world_view(), graphical.session.world_view())
        self.assertEqual(restored.revision, 0)


if __name__ == "__main__":
    unittest.main()
