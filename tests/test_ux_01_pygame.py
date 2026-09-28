from __future__ import annotations

import os
import tempfile
import unittest
from pathlib import Path

os.environ.setdefault("SDL_VIDEODRIVER", "dummy")
os.environ.setdefault("SDL_AUDIODRIVER", "dummy")

import pygame

from jomon.catalog import select_content_pack, template_root
from jomon.pygame_frontend import create_frontend
from jomon.state import Position


PACK = Path(__file__).parents[1] / "jomon" / "content_packs" / "first-playable"


class Ux01PygameTests(unittest.TestCase):
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

    def key(self, app, key: int, mod: int = 0) -> None:
        app.handle_event(pygame.event.Event(pygame.KEYDOWN, key=key, mod=mod))

    def start(self, renderer: str, path: Path):
        app = create_frontend(None, renderer=renderer, pygame=pygame, seed=f"ux-{renderer}", save_path=path)
        self.assertEqual(app.panel, "title")
        self.key(app, pygame.K_RETURN)
        self.assertEqual(app.panel, "setup")
        self.assertIsNone(app.session)
        self.key(app, pygame.K_RETURN)
        self.assertIsNone(app.panel)
        return app

    def click_cell(self, app, x: int, y: int) -> None:
        view = app.session.world_view()
        app.draw()
        point = next(row.position for row in view.cells if (row.position.x, row.position.y) == (x, y))
        app.handle_event(pygame.event.Event(
            pygame.MOUSEBUTTONDOWN, button=1, pos=app._rect(point, app._camera(view)).center,
        ))

    def test_explicit_new_starts_setup_and_cancel_returns_to_title(self) -> None:
        for renderer in ("debug", "ascii"):
            app = create_frontend(None, renderer=renderer, pygame=pygame, shell_mode="setup")
            self.assertEqual(app.panel, "setup")
            self.assertIsNone(app.session)
            self.key(app, pygame.K_ESCAPE)
            self.assertEqual(app.panel, "title")
            self.assertIsNone(app.session)
            self.assertEqual(app.title_subtitle, "")

    def test_pause_help_layout_and_save_are_state_inert(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "pause.json"
            app = self.start("debug", path)
            before_turn = app.session.world_view().turn
            layout = app._world_layout(app.session.world_view())
            self.assertIn("diagnostic module", app.session.operation_views()[0].objective)
            self.assertGreaterEqual(layout.tile_size, 32)
            self.assertGreaterEqual(layout.objective.height, 72)
            self.assertGreater(layout.world.width, 600)
            self.assertGreater(layout.context.width, 180)
            self.assertLessEqual(layout.status.right, app.screen.get_width())
            self.assertLessEqual(layout.feedback.bottom, app.screen.get_height())
            app.screen = pygame.display.set_mode((480, 320), pygame.RESIZABLE)
            compact = app._world_layout(app.session.world_view())
            for rect in (compact.status, compact.objective, compact.world, compact.context, compact.feedback):
                self.assertGreater(rect.width, 0)
                self.assertGreater(rect.height, 0)
                self.assertLessEqual(rect.right, 480)
                self.assertLessEqual(rect.bottom, 320)
            app.screen = pygame.display.set_mode((1100, 760), pygame.RESIZABLE)
            self.key(app, pygame.K_ESCAPE)
            self.assertEqual(app.panel, "pause")
            self.assertEqual(app.session.world_view().turn, before_turn)
            self.key(app, pygame.K_h)
            self.assertEqual(app.panel, "help")
            self.key(app, pygame.K_h)
            self.assertEqual(app.panel, "pause")
            self.key(app, pygame.K_DOWN)  # Save follows Resume.
            self.key(app, pygame.K_RETURN)
            self.assertTrue(path.is_file())
            self.assertEqual(app.session.world_view().turn, before_turn)
            self.key(app, pygame.K_ESCAPE)
            self.assertIsNone(app.panel)
            session = app.session
            self.key(app, pygame.K_ESCAPE)
            self.key(app, pygame.K_DOWN)
            self.key(app, pygame.K_DOWN)
            self.key(app, pygame.K_RETURN)
            self.assertEqual(app.panel, "settings")
            self.key(app, pygame.K_ESCAPE)
            self.assertEqual(app.panel, "pause")
            self.assertIs(app.session, session)

    def test_continuation_escape_layers_pause_without_losing_the_required_choice(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            app = self.start("debug", Path(directory) / "continuation.json")
            for _ in range(7):
                self.key(app, pygame.K_RIGHT)
            while app.session.world_view().courier_alive:
                self.key(app, pygame.K_LEFT)
                self.key(app, pygame.K_RIGHT)
            self.assertEqual(app.panel, "continuation")
            paused_turn = app.session.world_view().turn
            self.key(app, pygame.K_ESCAPE)
            self.assertEqual(app.panel, "pause")
            self.key(app, pygame.K_s, pygame.KMOD_CTRL)
            self.assertTrue((Path(directory) / "continuation.json").is_file())
            self.assertEqual(app.session.world_view().turn, paused_turn)
            self.key(app, pygame.K_ESCAPE)
            self.assertEqual(app.panel, "continuation")

    def test_both_input_paths_show_context_pause_and_hide_actors_outside_visibility(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            for renderer in ("debug", "ascii"):
                app = self.start(renderer, Path(directory) / f"{renderer}.json")
                # Reach the visible, adjacent latch through real movement, then select it by mouse.
                for _ in range(2):
                    self.key(app, pygame.K_UP)
                for _ in range(6):
                    self.key(app, pygame.K_RIGHT)
                self.click_cell(app, 9, 1)
                latch = app.session.context_view(app.selected)
                self.assertEqual(latch.entity_type_id, "maintenance_latch")
                self.assertFalse(latch.actions[0].enabled)
                self.key(app, pygame.K_e)
                self.assertEqual(app.last_result, "interaction.requires-equipped-tool")
                self.assertEqual(app.session.context_view(app.selected).actions[0].reason_text, "Equip the required tool first.")
                self.key(app, pygame.K_i)
                self.key(app, pygame.K_e)
                self.key(app, pygame.K_i)
                self.assertIsNone(app.panel)
                self.assertTrue(app.session.context_view(app.selected).actions[0].enabled)

                self.click_cell(app, 10, 3)
                defender = app.session.context_view(app.selected)
                self.assertEqual(defender.relation_id, "hostile")
                self.assertFalse(defender.actions[0].enabled)
                self.key(app, pygame.K_ESCAPE)
                self.assertEqual(app.panel, "pause")
                self.key(app, pygame.K_ESCAPE)
                self.assertIsNone(app.panel)
                self.key(app, pygame.K_h)
                self.assertEqual(app.panel, "help")
                self.key(app, pygame.K_h)
                self.assertIsNone(app.panel)

                # The defender is currently visible.  Moving away hides the actor but keeps terrain memory.
                self.assertTrue(app.session.actor_view("actor.service-defender").visible)
                for _ in range(4):
                    self.key(app, pygame.K_LEFT)
                self.assertFalse(app.session.actor_view("actor.service-defender").visible)
                app.selected = Position(20, 6)  # never visible/remembered; draw clears stale selection.
                app.draw()
                self.assertIsNone(app.selected)
                app.draw()  # SDL-dummy smoke after visibility and layout updates.


if __name__ == "__main__":
    unittest.main()
