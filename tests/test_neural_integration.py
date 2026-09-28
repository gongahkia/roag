from __future__ import annotations

import copy
import json
import shutil
import tempfile
import unittest
from contextlib import contextmanager
from pathlib import Path

from jomon.catalog import ContentError, load_content_pack, select_content_pack, template_root
from jomon.commands import (
    CharacterSetupCommand,
    IntegrateNeuralRecordsCommand,
    MoveCommand,
    RecoverRemainsItemCommand,
    SelectSuccessorCommand,
)
from jomon.session import GameSession
from jomon.state import Item, NeuralRecord, StateError, game_state_from_dict


ROOT = Path(__file__).parent
PACK = ROOT.parents[0] / "jomon" / "content_packs" / "first-playable"
SYNTHETIC = ROOT / "fixtures" / "synthetic_content_pack"
LEGACY_SAVE = ROOT / "fixtures" / "legacy_format16_synthetic_save.json"
B1D_FINGERPRINT = "8d0866a252e98ac452342aebcd0a1801dc18de8ec83b675d0113ed5d06683eec"
SETUP = CharacterSetupCommand("crew.field-member", "ancestry.baseline", "origin.maintenance", "trait.careful")
BASE = "feature.shared-base"
DONOR = "crew.initial-operative"
RECIPIENT = "crew.survivor-one"
THIRD = "crew.survivor-two"
DONOR_DEVICE = f"{DONOR}:item.neural-carrier"
RECIPIENT_DEVICE = f"{RECIPIENT}:item.neural-carrier"
THIRD_DEVICE = f"{THIRD}:item.neural-carrier"
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


class NeuralIntegrationTests(unittest.TestCase):
    def setUp(self) -> None:
        select_content_pack(PACK)

    def tearDown(self) -> None:
        select_content_pack(template_root())

    def session(self, seed: str = "neural-integration") -> GameSession:
        session, outcome = GameSession.create_configured(seed, SETUP)
        self.assertTrue(outcome.accepted)
        return session

    @contextmanager
    def copied_pack(self, mutate):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "integration-pack"
            shutil.copytree(PACK, root)
            manifest_path = root / "manifest.json"
            manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
            manifest["id"] = "neural-integration-test-pack"
            manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
            systems_path = root / "systems.json"
            systems = json.loads(systems_path.read_text(encoding="utf-8"))
            mutate(systems)
            systems_path.write_text(json.dumps(systems), encoding="utf-8")
            yield root

    @staticmethod
    def member(session: GameSession, member_id: str):
        return next(member for member in session._state.crew if member.id == member_id)

    @staticmethod
    def item(member, item_id: str) -> Item:
        return next(item for item in member.items if item.id == item_id)

    def kill_active_at_guard(self, session: GameSession) -> None:
        if (session._state.position.x, session._state.position.y) != (9, 3):
            move(session, 1, 0, 7)
        while session.world_view().courier_alive:
            move(session, -1, 0)
            self.assertTrue(session.submit(MoveCommand(1, 0)).accepted)

    def recover_initial_device_at_base(self, session: GameSession) -> Item:
        self.kill_active_at_guard(session)
        self.assertTrue(session.submit(SelectSuccessorCommand(RECIPIENT)).accepted)
        move(session, 1, 0, 8)
        move(session, 0, 1)
        recovered = session.submit(RecoverRemainsItemCommand(DONOR, DONOR_DEVICE))
        self.assertTrue(recovered.accepted)
        move(session, -1, 0, 7)
        self.assertEqual((session._state.position.x, session._state.position.y), (2, 3))
        return self.item(self.member(session, RECIPIENT), DONOR_DEVICE)

    def integrate(self, session: GameSession, source_id: str, *record_ids: str):
        return session.submit(IntegrateNeuralRecordsCommand(BASE, source_id, tuple(record_ids)))

    def assert_rejected_unchanged(self, session: GameSession, command, result_id: str) -> None:
        before, turn, revision = save_dict(session), session._state.turn, session.revision
        outcome = session.submit(command)
        self.assertEqual((outcome.accepted, outcome.changed, outcome.time_advanced, outcome.result_id, outcome.events),
                         (False, False, False, result_id, ()))
        self.assertEqual((session._state.turn, session.revision, save_dict(session)), (turn, revision, before))

    def test_real_recovery_retains_one_selected_record_without_immediate_stat_change(self) -> None:
        for selected, other in ((MAINTENANCE, CLOSE_QUARTERS), (CLOSE_QUARTERS, MAINTENANCE)):
            session = self.session(selected)
            source = self.recover_initial_device_at_base(session)
            recipient = self.member(session, RECIPIENT)
            destination = self.item(recipient, RECIPIENT_DEVICE)
            before_turn = session._state.turn
            before_health = (recipient.health, recipient.maximum_health)
            before_equipment = tuple((item.id, item.equipped, item.power) for item in recipient.items)
            before_operations = tuple(session.operation_views())

            outcome = self.integrate(session, source.id, selected)

            self.assertTrue(outcome.accepted)
            self.assertEqual((outcome.result_id, outcome.time_advanced), ("neural.integration.completed", True))
            self.assertEqual(session._state.turn, before_turn + 1)
            self.assertEqual(tuple((record.id, record.origin_member_id, record.definition_id) for record in destination.neural_records),
                             ((selected, DONOR, "record.maintenance-practice" if selected == MAINTENANCE else "record.close-quarters-technique"),))
            self.assertEqual(source.neural_records, ())
            self.assertNotIn(other, tuple(record.id for record in destination.neural_records))
            self.assertEqual((recipient.health, recipient.maximum_health), before_health)
            self.assertEqual(tuple((item.id, item.equipped, item.power) for item in recipient.items), before_equipment)
            self.assertEqual(tuple(session.operation_views()), before_operations)
            self.assertEqual(outcome.events[0].retained_record_ids, (selected,))

    def test_rejections_capacity_and_save_validation_are_atomic(self) -> None:
        session = self.session()
        source = self.recover_initial_device_at_base(session)
        move(session, 1, 0, 7)
        self.assert_rejected_unchanged(
            session, IntegrateNeuralRecordsCommand(BASE, source.id, (MAINTENANCE,)),
            "neural.integration.out-of-range",
        )
        move(session, -1, 0, 7)
        self.assert_rejected_unchanged(
            session, IntegrateNeuralRecordsCommand(BASE, source.id, (MAINTENANCE, CLOSE_QUARTERS)),
            "neural.integration.over-capacity",
        )
        self.assert_rejected_unchanged(
            session, IntegrateNeuralRecordsCommand(BASE, source.id, (MAINTENANCE, MAINTENANCE)),
            "neural.integration.duplicate-selection",
        )
        self.assert_rejected_unchanged(
            session, IntegrateNeuralRecordsCommand(BASE, source.id, ("record.unknown",)),
            "neural.integration.unknown-record",
        )
        self.assert_rejected_unchanged(
            session, IntegrateNeuralRecordsCommand("feature.maintenance-latch", source.id, (MAINTENANCE,)),
            "neural.integration.invalid-site",
        )
        self.assert_rejected_unchanged(
            session, IntegrateNeuralRecordsCommand(BASE, "missing.source", (MAINTENANCE,)),
            "neural.integration.source-unavailable",
        )
        recipient = self.member(session, RECIPIENT)
        destination = self.item(recipient, RECIPIENT_DEVICE)
        recipient.installed_neural_item_id = None
        self.assert_rejected_unchanged(
            session, IntegrateNeuralRecordsCommand(BASE, source.id, (MAINTENANCE,)),
            "neural.integration.recipient-unavailable",
        )
        recipient.installed_neural_item_id = destination.id
        source.equipped = True
        before_turn, before_revision, before_destination = session._state.turn, session.revision, destination.neural_records
        outcome = self.integrate(session, source.id, MAINTENANCE)
        self.assertEqual((outcome.accepted, outcome.result_id, outcome.events),
                         (False, "neural.integration.source-unavailable", ()))
        self.assertEqual((session._state.turn, session.revision, destination.neural_records),
                         (before_turn, before_revision, before_destination))
        source.equipped = False
        donor = self.member(session, DONOR)
        donor.installed_neural_item_id = source.id
        outcome = self.integrate(session, source.id, MAINTENANCE)
        self.assertEqual((outcome.accepted, outcome.result_id, outcome.events),
                         (False, "neural.integration.source-installed", ()))
        self.assertEqual((session._state.turn, session.revision, destination.neural_records),
                         (before_turn, before_revision, before_destination))
        donor.installed_neural_item_id = None
        source.neural_records = (source.neural_records[0], source.neural_records[0])
        outcome = self.integrate(session, source.id, MAINTENANCE)
        self.assertEqual((outcome.accepted, outcome.result_id, outcome.events),
                         (False, "neural.integration.duplicate-record", ()))
        self.assertEqual((session._state.turn, session.revision, destination.neural_records),
                         (before_turn, before_revision, before_destination))
        source.neural_records = (NeuralRecord(MAINTENANCE, DONOR, "record.maintenance-practice"),
                                 NeuralRecord(CLOSE_QUARTERS, DONOR, "record.close-quarters-technique"))

        self.assertTrue(self.integrate(session, source.id, MAINTENANCE).accepted)
        self.assert_rejected_unchanged(
            session, IntegrateNeuralRecordsCommand(BASE, source.id, (MAINTENANCE,)),
            "neural.integration.source-empty",
        )
        destination.neural_records = (
            NeuralRecord(MAINTENANCE, DONOR, "record.maintenance-practice"),
            NeuralRecord(CLOSE_QUARTERS, DONOR, "record.close-quarters-technique"),
        )
        with self.assertRaisesRegex(StateError, "exceeds inherited capacity"):
            session._state.to_dict()
        third = self.member(session, THIRD)
        third_device = self.item(third, THIRD_DEVICE)
        third.health = 0
        third.alive = False
        third_device.neural_records = destination.neural_records
        with self.assertRaisesRegex(StateError, "exceeds inherited capacity"):
            session._state.to_dict()
        third.health = third.maximum_health
        third.alive = True
        third_device.neural_records = ()
        destination.neural_records = (NeuralRecord(MAINTENANCE, DONOR, "record.maintenance-practice"),)
        over_capacity_save = save_dict(session)
        saved_recipient = next(member for member in over_capacity_save["crew"] if member["id"] == RECIPIENT)
        saved_destination = next(item for item in saved_recipient["items"] if item["id"] == RECIPIENT_DEVICE)
        saved_destination["neural_records"].append({
            "id": CLOSE_QUARTERS,
            "origin_member_id": DONOR,
            "definition_id": "record.close-quarters-technique",
        })
        saved_recipient["health"] = 0
        saved_recipient["alive"] = False
        with self.assertRaises(StateError):
            game_state_from_dict(over_capacity_save)
        recipient.health = 0
        recipient.alive = False
        before_turn, before_revision, before_source = session._state.turn, session.revision, source.neural_records
        outcome = self.integrate(session, source.id, MAINTENANCE)
        self.assertEqual((outcome.accepted, outcome.result_id, outcome.events), (False, "courier.dead", ()))
        self.assertEqual((session._state.turn, session.revision, source.neural_records),
                         (before_turn, before_revision, before_source))

    def test_complete_selection_preserves_own_records_deduplicates_and_discards(self) -> None:
        def add_recipient_native_record(systems):
            systems["neural"]["crew_initializers"][1]["record_definition_ids"] = ["record.maintenance-practice"]

        with self.copied_pack(add_recipient_native_record) as root:
            select_content_pack(root)
            session = self.session("own-records")
            source = self.recover_initial_device_at_base(session)
            recipient = self.member(session, RECIPIENT)
            destination = self.item(recipient, RECIPIENT_DEVICE)
            native = f"{RECIPIENT}:record.maintenance-practice"
            self.assertEqual(tuple(record.id for record in destination.neural_records), (native,))

            self.assertTrue(self.integrate(session, source.id, CLOSE_QUARTERS).accepted)
            self.assertEqual(tuple(record.id for record in destination.neural_records), (CLOSE_QUARTERS, native))

            duplicate = Item("source.identical", "item.neural-carrier", neural_records=(
                NeuralRecord(CLOSE_QUARTERS, DONOR, "record.close-quarters-technique"),
            ))
            recipient.items.append(duplicate)
            self.assertTrue(self.integrate(session, duplicate.id, CLOSE_QUARTERS).accepted)
            self.assertEqual(tuple(record.id for record in destination.neural_records), (CLOSE_QUARTERS, native))
            self.assertEqual(duplicate.neural_records, ())

            conflict = Item("source.conflict", "item.neural-carrier", neural_records=(
                NeuralRecord(CLOSE_QUARTERS, DONOR, "record.maintenance-practice"),
            ))
            recipient.items.append(conflict)
            self.assert_rejected_unchanged(
                session, IntegrateNeuralRecordsCommand(BASE, conflict.id, (CLOSE_QUARTERS,)),
                "neural.integration.conflicting-record",
            )

            replacement = Item("source.replacement", "item.neural-carrier", neural_records=(
                NeuralRecord("record.replacement", DONOR, "record.maintenance-practice"),
            ))
            recipient.items.append(replacement)
            self.assertTrue(self.integrate(session, replacement.id, replacement.neural_records[0].id).accepted)
            self.assertEqual(tuple(record.id for record in destination.neural_records), (native, "record.replacement"))
            self.assertEqual(replacement.neural_records, ())

            discard = Item("source.discard", "item.neural-carrier", neural_records=(
                NeuralRecord("record.discard", DONOR, "record.close-quarters-technique"),
            ))
            recipient.items.append(discard)
            self.assertTrue(self.integrate(session, discard.id).accepted)
            self.assertEqual(tuple(record.id for record in destination.neural_records), (native,))
            self.assertEqual(discard.neural_records, ())
            payload = save_dict(session)
            loaded = game_state_from_dict(payload)
            loaded_recipient = next(member for member in loaded.crew if member.id == RECIPIENT)
            loaded_destination = self.item(loaded_recipient, RECIPIENT_DEVICE)
            self.assertEqual(tuple(record.id for record in loaded_destination.neural_records), (native,))
            self.assertEqual(self.item(loaded_recipient, source.id).neural_records, ())
            self.assertEqual(self.item(loaded_recipient, replacement.id).neural_records, ())
            self.assertEqual(self.item(loaded_recipient, discard.id).neural_records, ())
            # A rejected conflict is an independent physical source, so it is not erased.
            self.assertEqual(self.item(loaded_recipient, conflict.id).neural_records, conflict.neural_records)

    def test_multilife_transfer_response_and_save_load_preserve_provenance(self) -> None:
        uninterrupted = self.session("multilife")
        resumed = self.session("multilife")
        for session in (uninterrupted, resumed):
            source = self.recover_initial_device_at_base(session)
            self.assertTrue(self.integrate(session, source.id, MAINTENANCE).accepted)
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "retained.json"
            resumed.save(path)
            resumed = GameSession.load(path)
            for session in (uninterrupted, resumed):
                self.kill_active_at_guard(session)
                self.assertTrue(session.submit(SelectSuccessorCommand(THIRD)).accepted)
                move(session, 1, 0, 8)
                move(session, 0, -1)
                self.assertTrue(session.submit(RecoverRemainsItemCommand(RECIPIENT, RECIPIENT_DEVICE)).accepted)
                self.assertTrue(session.submit(RecoverRemainsItemCommand(RECIPIENT, DONOR_DEVICE)).accepted)
                move(session, -1, 0, 7)
                carried = self.item(self.member(session, THIRD), RECIPIENT_DEVICE)
                self.assertEqual(tuple(record.id for record in carried.neural_records), (MAINTENANCE,))
                self.assertEqual(carried.neural_records[0].origin_member_id, DONOR)
                self.assertEqual(self.item(self.member(session, THIRD), DONOR_DEVICE).neural_records, ())
                self.assertTrue(self.integrate(session, carried.id, MAINTENANCE).accepted)
                third_device = self.item(self.member(session, THIRD), THIRD_DEVICE)
                self.assertEqual(tuple((record.id, record.origin_member_id) for record in third_device.neural_records), ((MAINTENANCE, DONOR),))
                self.assertEqual(carried.neural_records, ())
            self.assertEqual(save_dict(uninterrupted), save_dict(resumed))

        lethal = self.session("lethal-response")
        source = self.recover_initial_device_at_base(lethal)
        recipient = self.member(lethal, RECIPIENT)
        recipient.health = 1
        lethal._state.actors[0].position = type(lethal._state.position)(3, 3)
        outcome = self.integrate(lethal, source.id, MAINTENANCE)
        self.assertTrue(outcome.accepted)
        self.assertFalse(recipient.alive)
        self.assertEqual(sum(event.event_id == "defender.responded" for event in outcome.events), 1)
        self.assertEqual(self.item(recipient, RECIPIENT_DEVICE).neural_records[0].id, MAINTENANCE)
        self.assertEqual(source.neural_records, ())

    def test_integration_catalog_fingerprint_and_no_integration_legacy_policy(self) -> None:
        bad_mutations = (
            lambda systems: systems["neural"]["integration"].update({"site_feature_ids": ["feature.unknown"]}),
            lambda systems: systems["neural"]["integration"].update({"site_feature_ids": []}),
            lambda systems: systems["neural"]["integration"].update({"site_feature_ids": [BASE, BASE]}),
            lambda systems: systems["neural"]["integration"].update({"inherited_capacity": True}),
            lambda systems: systems["neural"]["integration"].update({"inherited_capacity": -1}),
            lambda systems: systems["neural"]["integration"].update({"extra": "invalid"}),
        )
        for mutate in bad_mutations:
            with self.copied_pack(mutate) as root:
                with self.assertRaises(ContentError):
                    load_content_pack(root)
        with self.copied_pack(lambda systems: systems["neural"].pop("integration")) as root:
            pack = load_content_pack(root)
            self.assertNotIn("integration", pack.systems["neural"])
            select_content_pack(root)
            session = self.session("no-integration")
            before = save_dict(session)
            self.assertEqual(session.submit(IntegrateNeuralRecordsCommand(BASE, DONOR_DEVICE, ())).result_id,
                             "neural.integration-unavailable")
            self.assertEqual(save_dict(session), before)

        select_content_pack(PACK)
        session = self.session("fingerprint")
        self.assertNotEqual(session._state.fingerprint, B1D_FINGERPRINT)
        incompatible = save_dict(session)
        incompatible["fingerprint"] = B1D_FINGERPRINT
        with self.assertRaisesRegex(StateError, "different playable content pack"):
            game_state_from_dict(incompatible)
        select_content_pack(SYNTHETIC)
        legacy = GameSession.load(LEGACY_SAVE)
        before = legacy._state.to_dict()
        outcome = legacy.submit(IntegrateNeuralRecordsCommand("feature.none", "source.none", ()))
        self.assertEqual((outcome.accepted, outcome.result_id, outcome.events),
                         (False, "neural.integration-unavailable", ()))
        self.assertEqual(legacy._state.to_dict(), before)

    def test_renamed_site_member_and_record_ids_drive_the_same_transfer(self) -> None:
        alternate = {
            BASE: "feature.alternate-base",
            DONOR: "crew.alternate-donor",
            RECIPIENT: "crew.alternate-recipient",
            THIRD: "crew.alternate-third",
            "record.maintenance-practice": "record.alternate-maintenance",
            "record.close-quarters-technique": "record.alternate-close-quarters",
        }

        def rename(systems):
            text = json.dumps(systems)
            for old, new in alternate.items():
                text = text.replace(old, new)
            systems.clear()
            systems.update(json.loads(text))

        with self.copied_pack(rename) as root:
            # Connections are presentation-only but still validate their declared endpoints.
            (root / "connections.json").write_text('{"connections": []}', encoding="utf-8")
            select_content_pack(root)
            session = self.session("renamed")
            donor = alternate[DONOR]
            recipient = alternate[RECIPIENT]
            base = alternate[BASE]
            source_id = f"{donor}:item.neural-carrier"
            destination_id = f"{recipient}:item.neural-carrier"
            retained = f"{donor}:{alternate['record.maintenance-practice']}"
            self.kill_active_at_guard(session)
            self.assertTrue(session.submit(SelectSuccessorCommand(recipient)).accepted)
            move(session, 1, 0, 8)
            move(session, 0, 1)
            self.assertTrue(session.submit(RecoverRemainsItemCommand(donor, source_id)).accepted)
            move(session, -1, 0, 7)
            result = session.submit(IntegrateNeuralRecordsCommand(base, source_id, (retained,)))
            self.assertTrue(result.accepted)
            installed = self.item(self.member(session, recipient), destination_id)
            self.assertEqual(tuple((record.id, record.origin_member_id) for record in installed.neural_records),
                             ((retained, donor),))


if __name__ == "__main__":
    unittest.main()
