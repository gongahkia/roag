from __future__ import annotations

import os
import tempfile
import unittest
from pathlib import Path

os.environ.setdefault("SDL_VIDEODRIVER", "dummy")
os.environ.setdefault("SDL_AUDIODRIVER", "dummy")
import pygame

from jomon.catalog import select_content_pack, template_root


ROOT = Path(__file__).parent
PACK = ROOT.parents[0] / "jomon" / "content_packs" / "first-playable"


class FirstPlayableRendererTests(unittest.TestCase):
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

    def click_cell(self, app, x: int, y: int) -> None:
        view = app.session.world_view()
        point = next(cell.position for cell in view.cells if cell.position.x == x and cell.position.y == y)
        app.draw()
        app.handle_event(pygame.event.Event(
            pygame.MOUSEBUTTONDOWN,
            button=1,
            pos=app._rect(point, app._camera(view)).center,
        ))

    def start_from_actual_shell_input(self, renderer: str, save_path: Path):
        from jomon.pygame_frontend import create_frontend

        app = create_frontend(None, renderer=renderer, pygame=pygame, seed="renderer-path", save_path=save_path)
        self.assertEqual(app.panel, "title")
        self.key(app, pygame.K_RETURN)
        self.assertEqual(app.panel, "setup")
        self.assertIsNone(app.session)
        app.draw()
        self.key(app, pygame.K_RETURN)
        self.assertIsNone(app.panel)
        self.assertIsNotNone(app.session)
        return app

    def equip_from_actual_inventory_input(self, app, index: int) -> None:
        self.key(app, pygame.K_i)
        self.assertEqual(app.panel, "inventory")
        for _ in range(index):
            self.key(app, pygame.K_DOWN)
        self.key(app, pygame.K_e)
        self.key(app, pygame.K_i)
        self.assertIsNone(app.panel)

    def move(self, app, key: int, count: int = 1) -> None:
        for _ in range(count):
            self.key(app, key)

    def complete_tool_path(self, app) -> None:
        self.equip_from_actual_inventory_input(app, 0)
        self.move(app, pygame.K_UP, 2)
        self.move(app, pygame.K_RIGHT, 6)
        self.click_cell(app, 9, 1)
        self.key(app, pygame.K_e)
        self.assertEqual(app.last_result, "interaction.access-opened")
        self.move(app, pygame.K_RIGHT, 8)
        self.move(app, pygame.K_DOWN, 2)
        self.click_cell(app, 16, 3)
        self.key(app, pygame.K_e)
        self.assertEqual(app.last_result, "interaction.objective-acquired")
        self.move(app, pygame.K_UP, 2)
        self.move(app, pygame.K_LEFT, 14)
        self.move(app, pygame.K_DOWN, 2)
        self.click_cell(app, 2, 3)
        self.key(app, pygame.K_e)
        self.assertEqual(app.last_result, "interaction.operation-delivered")

    def complete_combat_path(self, app) -> None:
        self.equip_from_actual_inventory_input(app, 1)
        self.move(app, pygame.K_RIGHT, 7)
        self.click_cell(app, 10, 3)
        self.key(app, pygame.K_f)
        self.key(app, pygame.K_f)
        self.assertFalse(app.session.actor_view("actor.service-defender").alive)
        self.move(app, pygame.K_RIGHT, 7)
        self.click_cell(app, 16, 3)
        self.key(app, pygame.K_e)
        self.move(app, pygame.K_LEFT, 14)
        self.click_cell(app, 2, 3)
        self.key(app, pygame.K_e)
        self.assertEqual(app.last_result, "interaction.operation-delivered")

    def test_setup_cancel_creates_no_session_in_both_renderers(self) -> None:
        from jomon.pygame_frontend import create_frontend

        for renderer in ("debug", "ascii"):
            app = create_frontend(None, renderer=renderer, pygame=pygame)
            self.key(app, pygame.K_RETURN)
            self.assertEqual(app.panel, "setup")
            self.assertIsNone(app.session)
            app.draw()
            self.key(app, pygame.K_ESCAPE)
            self.assertEqual(app.panel, "title")
            self.assertIsNone(app.session)

    def test_setup_selection_and_renderer_replacement_create_no_world_until_confirm(self) -> None:
        from jomon.pygame_frontend import create_frontend

        app = create_frontend(None, renderer="debug", pygame=pygame, seed="setup-draft")
        self.key(app, pygame.K_RETURN)
        self.key(app, pygame.K_DOWN)
        self.key(app, pygame.K_RIGHT)
        self.key(app, pygame.K_DOWN)
        self.assertIsNone(app.session)
        app.requested_renderer = "ascii"
        replacement = app.replacement_renderer()
        self.assertEqual(replacement.panel, "setup")
        self.assertIsNone(replacement.session)
        self.key(replacement, pygame.K_RETURN)
        self.assertIsNotNone(replacement.session)
        self.assertGreater(replacement.session.actor_view("courier").maximum_health, 10)

    def test_debug_input_path_completes_tool_run_and_saves(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "tool.json"
            app = self.start_from_actual_shell_input("debug", path)
            self.complete_tool_path(app)
            self.key(app, pygame.K_s, pygame.KMOD_CTRL)
            self.assertTrue(path.is_file())
            operation = app.session.operation_views()[0]
            self.assertEqual(operation.state_id, "returned")
            self.assertTrue(app.session.actor_view("actor.service-defender").alive)
            app.draw()

    def test_ascii_input_path_completes_combat_run_and_draws_result(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            app = self.start_from_actual_shell_input("ascii", Path(directory) / "combat.json")
            self.complete_combat_path(app)
            operation = app.session.operation_views()[0]
            self.assertEqual(operation.state_id, "returned")
            self.assertFalse(app.session.actor_view("actor.service-defender").alive)
            app.draw()

    def test_both_renderers_drive_both_routes_through_events(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            for renderer in ("debug", "ascii"):
                tool = self.start_from_actual_shell_input(renderer, Path(directory) / f"{renderer}-tool.json")
                self.complete_tool_path(tool)
                self.assertEqual(tool.session.operation_views()[0].state_id, "returned")
                combat = self.start_from_actual_shell_input(renderer, Path(directory) / f"{renderer}-combat.json")
                self.complete_combat_path(combat)
                self.assertEqual(combat.session.operation_views()[0].state_id, "returned")

    def test_renderer_replacement_preserves_live_operation_state(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            app = self.start_from_actual_shell_input("debug", Path(directory) / "switch.json")
            self.equip_from_actual_inventory_input(app, 0)
            self.move(app, pygame.K_UP, 2)
            self.move(app, pygame.K_RIGHT, 6)
            self.click_cell(app, 9, 1)
            self.key(app, pygame.K_e)
            before = app.session.operation_views()[0]
            app.requested_renderer = "ascii"
            replacement = app.replacement_renderer()
            self.assertIs(replacement.session, app.session)
            self.assertEqual(replacement.operation_views() if hasattr(replacement, "operation_views") else replacement.session.operation_views()[0], before)
            replacement.draw()
