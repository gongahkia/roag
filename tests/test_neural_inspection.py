from __future__ import annotations

import copy
import json
import os
import shutil
import tempfile
import unittest
from contextlib import contextmanager
from dataclasses import FrozenInstanceError
from pathlib import Path

os.environ.setdefault("SDL_VIDEODRIVER", "dummy")
os.environ.setdefault("SDL_AUDIODRIVER", "dummy")
import pygame

from jomon.catalog import select_content_pack, template_root
from jomon.commands import CharacterSetupCommand, MoveCommand, RecoverRemainsItemCommand, SelectSuccessorCommand
from jomon.session import GameSession
from jomon.state import Item, NeuralRecord


ROOT = Path(__file__).parent
PACK = ROOT.parents[0] / "jomon" / "content_packs" / "first-playable"
SETUP = CharacterSetupCommand("crew.field-member", "ancestry.baseline", "origin.maintenance", "trait.careful")
B1D_FINGERPRINT = "8d0866a252e98ac452342aebcd0a1801dc18de8ec83b675d0113ed5d06683eec"


def save_dict(session: GameSession) -> dict:
    with tempfile.TemporaryDirectory() as directory:
        path = Path(directory) / "state.json"
        session.save(path)
        return json.loads(path.read_text(encoding="utf-8"))


def move(session: GameSession, dx: int, dy: int, count: int = 1) -> None:
    for _ in range(count):
        outcome = session.submit(MoveCommand(dx, dy))
        if not outcome.accepted:
            raise AssertionError(outcome.result_id)


class NeuralInspectionTests(unittest.TestCase):
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

    def session(self, seed: str = "neural-inspection") -> GameSession:
        session, outcome = GameSession.create_configured(seed, SETUP)
        self.assertTrue(outcome.accepted)
        return session

    def kill_initial_and_reach_body(self, session: GameSession) -> None:
        move(session, 1, 0, 7)
        while session.world_view().courier_alive:
            move(session, -1, 0)
            self.assertTrue(session.submit(MoveCommand(1, 0)).accepted)
        self.assertTrue(session.submit(SelectSuccessorCommand("crew.survivor-one")).accepted)
        move(session, 1, 0, 8)
        move(session, 0, 1)

    def recover_initial_device(self, session: GameSession) -> None:
        self.kill_initial_and_reach_body(session)
        outcome = session.submit(RecoverRemainsItemCommand(
            "crew.initial-operative", "crew.initial-operative:item.neural-carrier",
        ))
        self.assertTrue(outcome.accepted)

    def test_detail_projection_distinguishes_custody_installation_and_payload_state(self) -> None:
        session = self.session()
        # B2a deliberately adds mechanical integration configuration.
        self.assertNotEqual(session._state.fingerprint, B1D_FINGERPRINT)
        ordinary = session.item_detail_view("item.maintenance-tool")
        initial = session.item_detail_view("crew.initial-operative:item.neural-carrier")
        self.assertEqual(ordinary.neural_payload_state_id, "neural.none")
        self.assertEqual(ordinary.installed_member_id, None)
        self.assertEqual(initial.custodian_member_id, "crew.initial-operative")
        self.assertEqual(initial.installed_member_id, "crew.initial-operative")
        self.assertEqual(initial.neural_payload_state_id, "neural.records")
        self.assertEqual(
            tuple((record.id, record.display_name, record.origin_member_id, record.origin_display_name) for record in initial.neural_records),
            (
                ("crew.initial-operative:record.maintenance-practice", "Maintenance Practice", "crew.initial-operative", "Initial operative"),
                ("crew.initial-operative:record.close-quarters-technique", "Close Quarters Technique", "crew.initial-operative", "Initial operative"),
            ),
        )
        self.recover_initial_device(session)
        own = session.item_detail_view("crew.survivor-one:item.neural-carrier")
        recovered = session.item_detail_view("crew.initial-operative:item.neural-carrier")
        self.assertEqual((own.custodian_member_id, own.installed_member_id, own.neural_payload_state_id, own.neural_records),
                         ("crew.survivor-one", "crew.survivor-one", "neural.empty", ()))
        self.assertEqual((recovered.custodian_member_id, recovered.installed_member_id, recovered.neural_payload_state_id),
                         ("crew.survivor-one", None, "neural.records"))
        self.assertEqual(tuple(record.origin_member_id for record in recovered.neural_records),
                         ("crew.initial-operative", "crew.initial-operative"))

    @contextmanager
    def opaque_pack(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "opaque-pack"
            shutil.copytree(PACK, root)
            manifest = json.loads((root / "manifest.json").read_text(encoding="utf-8"))
            manifest["id"] = "opaque-neural-inspection-pack"
            (root / "manifest.json").write_text(json.dumps(manifest), encoding="utf-8")
            systems = json.loads((root / "systems.json").read_text(encoding="utf-8"))
            systems.pop("neural")
            (root / "systems.json").write_text(json.dumps(systems), encoding="utf-8")
            select_content_pack(root)
            yield root

    def test_opaque_mixed_origin_detail_is_immutable_and_stale_lookup_is_safe(self) -> None:
        with self.opaque_pack():
            session = self.session()
            carrier = Item("opaque-device", "item.neural-carrier", neural_records=(
                NeuralRecord("record.opaque-one", "crew.initial-operative", "definition.opaque-one"),
                NeuralRecord("record.opaque-two", "crew.survivor-one", "definition.opaque-two"),
            ))
            session._state.items.append(carrier)
            detail = session.item_detail_view(carrier.id)
            self.assertEqual(tuple(record.display_name for record in detail.neural_records), ("Definition Opaque One", "Definition Opaque Two"))
            self.assertEqual(tuple(record.origin_display_name for record in detail.neural_records), ("Initial operative", "Base crew member"))
            with self.assertRaises(FrozenInstanceError):
                detail.custodian_member_id = "crew.survivor-two"
            with self.assertRaises(FrozenInstanceError):
                detail.neural_records[0].definition_id = "definition.changed"
            self.assertIsNone(session.item_detail_view("stale.item"))

    def test_detail_reads_and_real_panel_navigation_are_state_inert_at_guard(self) -> None:
        from jomon.pygame_frontend import create_frontend

        session = self.session()
        move(session, 1, 0, 7)
        before = save_dict(session)
        turn, health, revision = session.world_view().turn, session.actor_view("courier").health, session.revision
        for _ in range(3):
            self.assertIsNotNone(session.item_detail_view("crew.initial-operative:item.neural-carrier"))
        app = create_frontend(session, renderer="debug", pygame=pygame, size=(480, 320))
        self.key(app, pygame.K_i)
        self.select_inventory_item(app, "crew.initial-operative:item.neural-carrier")
        self.key(app, pygame.K_d)
        self.assertEqual(app.panel, "item-detail")
        self.assertTrue(any("Maintenance Practice" in text for text, _ in app._detail_lines(
            session.item_detail_view(app.detail_item_id), 300,
        )))
        app.draw()
        self.key(app, pygame.K_DOWN)
        self.assertGreater(app.detail_scroll, 0)
        self.key(app, pygame.K_ESCAPE)
        self.assertEqual(app.panel, "inventory")
        self.key(app, pygame.K_i)
        self.assertIsNone(app.panel)
        app.panel = "item-detail"
        app.detail_item_id = "stale.item"
        self.key(app, pygame.K_DOWN)
        self.assertEqual((app.panel, app.last_result), ("inventory", "inventory.detail-unavailable"))
        self.assertEqual((session.world_view().turn, session.actor_view("courier").health, session.revision), (turn, health, revision))
        self.assertEqual(save_dict(session), before)

    def key(self, app, key: int, mod: int = 0) -> None:
        app.handle_event(pygame.event.Event(pygame.KEYDOWN, key=key, mod=mod))

    def click(self, app, x: int, y: int) -> None:
        view = app.session.world_view()
        point = next(cell.position for cell in view.cells if (cell.position.x, cell.position.y) == (x, y))
        app.draw()
        app.handle_event(pygame.event.Event(
            pygame.MOUSEBUTTONDOWN, button=1, pos=app._rect(point, app._camera(view)).center,
        ))

    def select_inventory_item(self, app, item_id: str) -> None:
        items = app.session.inventory_view()
        index = next(index for index, item in enumerate(items) if item.id == item_id)
        for _ in range(index):
            self.key(app, pygame.K_DOWN)

    def open_detail(self, app, item_id: str):
        self.key(app, pygame.K_i)
        self.assertEqual(app.panel, "inventory")
        self.select_inventory_item(app, item_id)
        self.key(app, pygame.K_d)
        self.assertEqual(app.panel, "item-detail")
        detail = app.session.item_detail_view(app.detail_item_id)
        self.assertEqual(detail.id, item_id)
        app.draw()
        return detail

    def close_detail(self, app) -> None:
        self.key(app, pygame.K_d)
        self.assertEqual(app.panel, "inventory")
        self.key(app, pygame.K_i)
        self.assertIsNone(app.panel)

    def start(self, renderer: str, path: Path):
        from jomon.pygame_frontend import create_frontend

        app = create_frontend(None, renderer=renderer, pygame=pygame, seed="neural-inspection-input", save_path=path)
        self.key(app, pygame.K_RETURN)
        self.key(app, pygame.K_RETURN)
        self.assertIsNotNone(app.session)
        return app

    def die_select_and_recover_through_input(self, app) -> None:
        for _ in range(7):
            self.key(app, pygame.K_RIGHT)
        while app.session.world_view().courier_alive:
            self.key(app, pygame.K_LEFT)
            self.key(app, pygame.K_RIGHT)
        self.assertEqual(app.panel, "continuation")
        successor_action = "successor:crew.survivor-one"
        for _ in range(next(index for index, row in enumerate(app._rows()) if row.action == successor_action)):
            self.key(app, pygame.K_DOWN)
        self.key(app, pygame.K_RETURN)
        self.assertIsNone(app.panel)
        for _ in range(8):
            self.key(app, pygame.K_RIGHT)
        self.key(app, pygame.K_DOWN)
        self.click(app, 9, 3)
        self.key(app, pygame.K_r)
        source_action = "recover:crew.initial-operative:crew.initial-operative:item.neural-carrier"
        for _ in range(next(index for index, row in enumerate(app._rows()) if row.action == source_action)):
            self.key(app, pygame.K_DOWN)
        self.key(app, pygame.K_RETURN)
        self.assertIsNone(app.panel)

    def test_both_renderer_input_paths_inspect_recover_reload_and_replace(self) -> None:
        from jomon.pygame_frontend import create_frontend

        for renderer in ("debug", "ascii"):
            with tempfile.TemporaryDirectory() as directory:
                path = Path(directory) / f"{renderer}.json"
                app = self.start(renderer, path)
                before = save_dict(app.session)
                original = self.open_detail(app, "crew.initial-operative:item.neural-carrier")
                self.assertEqual((original.installed_member_id, original.neural_payload_state_id, len(original.neural_records)),
                                 ("crew.initial-operative", "neural.records", 2))
                self.key(app, pygame.K_DOWN)
                self.close_detail(app)
                self.assertEqual(save_dict(app.session), before)
                self.die_select_and_recover_through_input(app)
                recovered = self.open_detail(app, "crew.initial-operative:item.neural-carrier")
                self.assertEqual((recovered.custodian_member_id, recovered.installed_member_id),
                                 ("crew.survivor-one", None))
                self.close_detail(app)
                empty = self.open_detail(app, "crew.survivor-one:item.neural-carrier")
                self.assertEqual((empty.installed_member_id, empty.neural_payload_state_id, empty.neural_records),
                                 ("crew.survivor-one", "neural.empty", ()))
                self.close_detail(app)
                self.key(app, pygame.K_s, pygame.KMOD_CTRL)
                self.assertTrue(path.is_file())
                loaded = GameSession.load(path)
                reloaded = create_frontend(loaded, renderer=renderer, pygame=pygame, save_path=path)
                recovered_after_load = self.open_detail(reloaded, "crew.initial-operative:item.neural-carrier")
                self.assertEqual(
                    tuple((record.id, record.origin_member_id, record.definition_id) for record in recovered_after_load.neural_records),
                    tuple((record.id, record.origin_member_id, record.definition_id) for record in recovered.neural_records),
                )
                reloaded.requested_renderer = "ascii" if renderer == "debug" else "debug"
                replacement = reloaded.replacement_renderer()
                self.assertIs(replacement.session, loaded)
                self.assertEqual(replacement.panel, "item-detail")
                self.assertEqual(replacement.item_detail_view(replacement.detail_item_id) if hasattr(replacement, "item_detail_view") else replacement.session.item_detail_view(replacement.detail_item_id), recovered_after_load)
                replacement.draw()
                self.close_detail(replacement)
