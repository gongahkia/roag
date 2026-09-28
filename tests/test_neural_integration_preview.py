from __future__ import annotations

import json
import os
import tempfile
import unittest
from pathlib import Path

os.environ.setdefault("SDL_VIDEODRIVER", "dummy")
os.environ.setdefault("SDL_AUDIODRIVER", "dummy")
import pygame

from jomon.catalog import select_content_pack, template_root
from jomon.commands import CharacterSetupCommand, IntegrateNeuralRecordsCommand, MoveCommand, RecoverRemainsItemCommand, SelectSuccessorCommand
from jomon.pygame_frontend import create_frontend
from jomon.session import GameSession
from jomon.state import Item, NeuralRecord

ROOT = Path(__file__).parent
PACK = ROOT.parents[0] / "jomon" / "content_packs" / "first-playable"
SETUP = CharacterSetupCommand("crew.field-member", "ancestry.baseline", "origin.maintenance", "trait.careful")
BASE = "feature.shared-base"
DONOR = "crew.initial-operative"
RECIPIENT = "crew.survivor-one"
DONOR_DEVICE = f"{DONOR}:item.neural-carrier"
RECIPIENT_DEVICE = f"{RECIPIENT}:item.neural-carrier"
MAINTENANCE = f"{DONOR}:record.maintenance-practice"
CLOSE_QUARTERS = f"{DONOR}:record.close-quarters-technique"


def move(session: GameSession, dx: int, dy: int, count: int = 1) -> None:
    for _ in range(count):
        outcome = session.submit(MoveCommand(dx, dy))
        if not outcome.accepted:
            raise AssertionError(outcome.result_id)


def save_dict(session: GameSession) -> dict:
    with tempfile.TemporaryDirectory() as directory:
        path = Path(directory) / "state.json"
        session.save(path)
        return json.loads(path.read_text(encoding="utf-8"))


class NeuralIntegrationPreviewTests(unittest.TestCase):
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

    def session(self, seed: str = "preview") -> GameSession:
        session, outcome = GameSession.create_configured(seed, SETUP)
        self.assertTrue(outcome.accepted)
        return session

    @staticmethod
    def member(session: GameSession, member_id: str):
        return next(member for member in session._state.crew if member.id == member_id)

    @staticmethod
    def item(member, item_id: str):
        return next(item for item in member.items if item.id == item_id)

    def recover_source_at_base(self, session: GameSession) -> None:
        move(session, 1, 0, 7)
        while session.world_view().courier_alive:
            move(session, -1, 0)
            self.assertTrue(session.submit(MoveCommand(1, 0)).accepted)
        self.assertTrue(session.submit(SelectSuccessorCommand(RECIPIENT)).accepted)
        move(session, 1, 0, 8)
        move(session, 0, 1)
        self.assertTrue(session.submit(RecoverRemainsItemCommand(DONOR, DONOR_DEVICE)).accepted)
        move(session, -1, 0, 7)
        self.assertEqual((session._state.position.x, session._state.position.y), (2, 3))

    def test_preview_is_state_inert_and_matches_the_shared_commit_result(self) -> None:
        session = self.session()
        self.recover_source_at_base(session)
        before, turn, revision = save_dict(session), session._state.turn, session.revision
        preview = session.neural_integration_preview(BASE, DONOR_DEVICE, (MAINTENANCE,))
        repeated = session.neural_integration_preview(BASE, DONOR_DEVICE, (MAINTENANCE,))
        self.assertEqual(preview, repeated)
        self.assertTrue(preview.confirmable)
        self.assertEqual(preview.reason_id, "neural.integration.completed")
        self.assertEqual(tuple(row.id for row in preview.source_records), (MAINTENANCE, CLOSE_QUARTERS))
        self.assertEqual(tuple(row.id for row in preview.resulting_records), (MAINTENANCE,))
        self.assertEqual(tuple(row.id for row in preview.omitted_source_records), (CLOSE_QUARTERS,))
        self.assertTrue(preview.source_will_be_empty)
        self.assertEqual((save_dict(session), session._state.turn, session.revision), (before, turn, revision))

        outcome = session.submit(IntegrateNeuralRecordsCommand(BASE, DONOR_DEVICE, (MAINTENANCE,)))
        self.assertTrue(outcome.accepted)
        recipient = self.member(session, RECIPIENT)
        self.assertEqual(tuple(row.id for row in self.item(recipient, RECIPIENT_DEVICE).neural_records),
                         tuple(row.id for row in preview.resulting_records))
        self.assertEqual(self.item(recipient, DONOR_DEVICE).neural_records, ())

    def test_preview_keeps_protected_records_and_accounts_for_replacement_losses(self) -> None:
        session = self.session("preview-protected")
        self.recover_source_at_base(session)
        recipient = self.member(session, RECIPIENT)
        destination = self.item(recipient, RECIPIENT_DEVICE)
        own = NeuralRecord("synthetic.preview-own", RECIPIENT, "record.maintenance-practice")
        retained = NeuralRecord(MAINTENANCE, DONOR, "record.maintenance-practice")
        incoming = NeuralRecord(CLOSE_QUARTERS, DONOR, "record.close-quarters-technique")
        destination.neural_records = (own, retained)
        source = Item("synthetic.preview-source", "item.neural-carrier", neural_records=(incoming,))
        recipient.items.append(source)
        before = save_dict(session)
        preview = session.neural_integration_preview(BASE, source.id, (CLOSE_QUARTERS,))
        self.assertTrue(preview.confirmable)
        self.assertEqual(tuple(record.id for record in preview.protected_records), (own.id,))
        self.assertEqual(tuple(record.id for record in preview.retained_records), (MAINTENANCE,))
        self.assertEqual(tuple(record.id for record in preview.resulting_records), (CLOSE_QUARTERS, own.id))
        self.assertEqual(tuple(record.id for record in preview.removed_destination_records), (MAINTENANCE,))
        self.assertEqual(preview.omitted_source_records, ())
        discard = session.neural_integration_preview(BASE, source.id, ())
        self.assertEqual(tuple(record.id for record in discard.resulting_records), (own.id,))
        self.assertEqual(tuple(record.id for record in discard.removed_destination_records), (MAINTENANCE,))
        self.assertEqual(tuple(record.id for record in discard.omitted_source_records), (CLOSE_QUARTERS,))
        self.assertEqual(save_dict(session), before)

    def test_over_capacity_preview_is_visible_and_never_mutates(self) -> None:
        session = self.session("over-capacity-preview")
        self.recover_source_at_base(session)
        before, turn, revision = save_dict(session), session._state.turn, session.revision
        preview = session.neural_integration_preview(BASE, DONOR_DEVICE, (MAINTENANCE, CLOSE_QUARTERS))
        self.assertFalse(preview.confirmable)
        self.assertEqual(preview.reason_id, "neural.integration.over-capacity")
        self.assertEqual(preview.foreign_slots_used, 2)
        self.assertEqual(preview.inherited_capacity, 1)
        self.assertEqual(tuple(row.id for row in preview.resulting_records), (CLOSE_QUARTERS, MAINTENANCE))
        self.assertEqual((save_dict(session), session._state.turn, session.revision), (before, turn, revision))
        unavailable = session.neural_integration_preview("feature.unknown", DONOR_DEVICE, (MAINTENANCE,))
        self.assertFalse(unavailable.confirmable)
        self.assertEqual(unavailable.reason_id, "neural.integration.invalid-site")
        self.assertEqual((save_dict(session), session._state.turn, session.revision), (before, turn, revision))

    def key(self, app, key: int, mod: int = 0) -> None:
        app.handle_event(pygame.event.Event(pygame.KEYDOWN, key=key, mod=mod))

    def click(self, app, x: int, y: int) -> None:
        view = app.session.world_view()
        app.draw()
        point = next(cell.position for cell in view.cells if (cell.position.x, cell.position.y) == (x, y))
        app.handle_event(pygame.event.Event(
            pygame.MOUSEBUTTONDOWN, button=1, pos=app._rect(point, app._camera(view)).center,
        ))

    def start(self, renderer: str, path: Path):
        app = create_frontend(None, renderer=renderer, pygame=pygame, seed=f"preview-input-{renderer}", save_path=path)
        self.key(app, pygame.K_RETURN)
        self.key(app, pygame.K_RETURN)
        self.assertIsNotNone(app.session)
        return app

    def select_inventory_item(self, app, item_id: str) -> None:
        items = app.session.inventory_view()
        index = next(index for index, item in enumerate(items) if item.id == item_id)
        steps = (index - app.inventory_cursor) % len(items)
        for _ in range(steps):
            self.key(app, pygame.K_DOWN)

    def open_detail(self, app, item_id: str) -> None:
        self.key(app, pygame.K_i)
        self.select_inventory_item(app, item_id)
        self.key(app, pygame.K_d)
        self.assertEqual((app.panel, app.detail_item_id), ("item-detail", item_id))

    def die_select_recover_and_return_via_input(self, app) -> None:
        for _ in range(7):
            self.key(app, pygame.K_RIGHT)
        while app.session.world_view().courier_alive:
            self.key(app, pygame.K_LEFT)
            self.key(app, pygame.K_RIGHT)
        self.assertEqual(app.panel, "continuation")
        successor_index = next(index for index, row in enumerate(app._rows()) if row.action == f"successor:{RECIPIENT}")
        for _ in range(successor_index):
            self.key(app, pygame.K_DOWN)
        self.key(app, pygame.K_RETURN)
        self.assertIsNone(app.panel)
        for _ in range(8):
            self.key(app, pygame.K_RIGHT)
        self.key(app, pygame.K_DOWN)
        self.click(app, 9, 3)
        self.key(app, pygame.K_r)
        source_action = f"recover:{DONOR}:{DONOR_DEVICE}"
        source_index = next(index for index, row in enumerate(app._rows()) if row.action == source_action)
        for _ in range(source_index):
            self.key(app, pygame.K_DOWN)
        self.key(app, pygame.K_RETURN)
        for _ in range(7):
            self.key(app, pygame.K_LEFT)
        self.assertEqual((app.session._state.position.x, app.session._state.position.y), (2, 3))

    def select_candidate_via_input(self, app, record_id: str) -> None:
        preview = app._neural_preview()
        index = next(index for index, record in enumerate(preview.candidate_records) if record.id == record_id)
        steps = (index - app.neural_candidate_cursor) % len(preview.candidate_records)
        for _ in range(steps):
            self.key(app, pygame.K_DOWN)
        self.key(app, pygame.K_SPACE)

    def test_both_renderer_input_paths_cancel_review_confirm_save_and_replace(self) -> None:
        for renderer in ("debug", "ascii"):
            with self.subTest(renderer=renderer), tempfile.TemporaryDirectory() as directory:
                path = Path(directory) / f"{renderer}.json"
                app = self.start(renderer, path)
                self.die_select_recover_and_return_via_input(app)
                self.open_detail(app, DONOR_DEVICE)
                before = save_dict(app.session)
                self.key(app, pygame.K_n)
                self.assertEqual(app.panel, "neural-select")
                # Saving a modal draft writes canonical state only; it does not commit.
                self.key(app, pygame.K_s, pygame.KMOD_CTRL)
                self.assertTrue(path.is_file())
                self.assertEqual((app.panel, save_dict(app.session)), ("neural-select", before))
                # A modal world key is consumed: it neither moves nor attacks.
                self.key(app, pygame.K_f)
                self.assertEqual(save_dict(app.session), before)
                self.select_candidate_via_input(app, MAINTENANCE)
                self.key(app, pygame.K_r)
                self.assertEqual(app.panel, "neural-review")
                self.key(app, pygame.K_ESCAPE)
                self.assertEqual(app.panel, "neural-select")
                self.key(app, pygame.K_ESCAPE)
                self.assertEqual(app.panel, "item-detail")
                self.assertEqual(save_dict(app.session), before)

                self.key(app, pygame.K_n)
                self.select_candidate_via_input(app, MAINTENANCE)
                self.key(app, pygame.K_r)
                preview = app._neural_preview()
                self.assertTrue(preview.confirmable)
                self.assertEqual(preview.resulting_records[0].capability_ids, ("capability.maintenance-service",))
                before_turn = app.session.world_view().turn
                self.key(app, pygame.K_c)
                self.assertEqual(app.panel, "item-detail")
                self.assertEqual(app.session.world_view().turn, before_turn + 1)
                recipient = self.member(app.session, RECIPIENT)
                self.assertEqual(tuple(record.id for record in self.item(recipient, RECIPIENT_DEVICE).neural_records), (MAINTENANCE,))
                self.assertEqual(self.item(recipient, DONOR_DEVICE).neural_records, ())
                # C has no meaning in an item detail panel, so a repeated key cannot replay the transfer.
                after = save_dict(app.session)
                self.key(app, pygame.K_c)
                self.assertEqual(save_dict(app.session), after)

                self.key(app, pygame.K_d)
                self.select_inventory_item(app, RECIPIENT_DEVICE)
                self.key(app, pygame.K_d)
                self.assertEqual((app.panel, app.detail_item_id), ("item-detail", RECIPIENT_DEVICE))
                self.assertIn("Neural device: 1 records", " ".join(text for text, _ in app._detail_lines(
                    app.session.item_detail_view(RECIPIENT_DEVICE), 500,
                )))
                self.assertIn("Grants: Maintenance Service", " ".join(text for text, _ in app._detail_lines(
                    app.session.item_detail_view(RECIPIENT_DEVICE), 500,
                )))
                self.key(app, pygame.K_d)
                self.key(app, pygame.K_i)
                self.key(app, pygame.K_s, pygame.KMOD_CTRL)
                self.assertTrue(path.is_file())
                loaded = GameSession.load(path)
                self.assertEqual(tuple(record.id for record in self.item(self.member(loaded, RECIPIENT), RECIPIENT_DEVICE).neural_records),
                                 (MAINTENANCE,))
                replacement = create_frontend(loaded, renderer="ascii" if renderer == "debug" else "debug", pygame=pygame, save_path=path)
                self.open_detail(replacement, RECIPIENT_DEVICE)
                self.assertEqual(
                    tuple(record.id for record in replacement.session.item_detail_view(RECIPIENT_DEVICE).neural_records),
                    (MAINTENANCE,),
                )
                replacement.draw()

    def test_installed_empty_and_ordinary_items_never_open_a_source_draft(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            app = self.start("debug", Path(directory) / "source-reasons.json")
            self.open_detail(app, DONOR_DEVICE)
            self.key(app, pygame.K_n)
            self.assertEqual((app.panel, app.last_result), ("item-detail", "neural.integration.source-installed"))
            self.key(app, pygame.K_d)
            self.select_inventory_item(app, "item.maintenance-tool")
            self.key(app, pygame.K_d)
            self.key(app, pygame.K_n)
            self.assertEqual((app.panel, app.last_result), ("item-detail", "neural.integration.source-unavailable"))

    def test_empty_and_overcapacity_reviews_require_no_mutating_fallback(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            app = self.start("debug", Path(directory) / "empty.json")
            self.die_select_recover_and_return_via_input(app)
            self.open_detail(app, DONOR_DEVICE)
            before = save_dict(app.session)
            self.key(app, pygame.K_n)
            self.key(app, pygame.K_r)
            self.assertEqual(app.panel, "neural-review")
            self.assertTrue(app._neural_preview().confirmable)
            self.key(app, pygame.K_c)
            recipient = self.member(app.session, RECIPIENT)
            self.assertEqual(self.item(recipient, RECIPIENT_DEVICE).neural_records, ())
            self.assertEqual(self.item(recipient, DONOR_DEVICE).neural_records, ())
            self.assertEqual(app.session.world_view().turn, before["turn"] + 1)

        with tempfile.TemporaryDirectory() as directory:
            app = self.start("debug", Path(directory) / "capacity.json")
            self.die_select_recover_and_return_via_input(app)
            self.open_detail(app, DONOR_DEVICE)
            before = save_dict(app.session)
            self.key(app, pygame.K_n)
            self.select_candidate_via_input(app, MAINTENANCE)
            self.select_candidate_via_input(app, CLOSE_QUARTERS)
            self.key(app, pygame.K_r)
            self.assertEqual((app.panel, app._neural_preview().reason_id), ("neural-review", "neural.integration.over-capacity"))
            self.key(app, pygame.K_c)
            self.assertEqual(save_dict(app.session), before)
            self.assertEqual(app.last_result, "neural.integration.over-capacity")

    def test_renderer_replacement_discards_an_uncommitted_draft(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            app = self.start("debug", Path(directory) / "replace.json")
            self.die_select_recover_and_return_via_input(app)
            self.open_detail(app, DONOR_DEVICE)
            before = save_dict(app.session)
            self.key(app, pygame.K_n)
            self.select_candidate_via_input(app, MAINTENANCE)
            replacement = app.replacement_renderer()
            self.assertEqual((replacement.panel, replacement.detail_item_id), ("item-detail", DONOR_DEVICE))
            self.assertIsNone(replacement.neural_source_item_id)
            self.assertEqual(save_dict(replacement.session), before)

    def test_review_stales_on_intervening_revision_without_submitting(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            app = self.start("debug", Path(directory) / "stale.json")
            self.die_select_recover_and_return_via_input(app)
            self.open_detail(app, DONOR_DEVICE)
            self.key(app, pygame.K_n)
            self.select_candidate_via_input(app, MAINTENANCE)
            self.key(app, pygame.K_r)
            self.assertEqual(app.panel, "neural-review")
            # This legal move keeps B adjacent to the base but changes revision.
            self.assertTrue(app.session.submit(MoveCommand(-1, 0)).accepted)
            before = save_dict(app.session)
            self.key(app, pygame.K_c)
            self.assertEqual((app.panel, app.last_result), ("neural-select", "neural.integration.review-stale"))
            self.assertEqual(save_dict(app.session), before)


if __name__ == "__main__":
    unittest.main()
