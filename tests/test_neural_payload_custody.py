from __future__ import annotations

import copy
import json
import shutil
import tempfile
import unittest
from contextlib import contextmanager
from pathlib import Path

from jomon.catalog import select_content_pack, template_root
from jomon.commands import (
    CharacterSetupCommand,
    EquipItemCommand,
    MoveCommand,
    RecoverRemainsItemCommand,
    SelectSuccessorCommand,
    UnequipItemCommand,
)
from jomon.session import GameSession
from jomon.state import Item, NeuralRecord, StateError, game_state_from_dict


ROOT = Path(__file__).parent
PACK = ROOT.parents[0] / "jomon" / "content_packs" / "first-playable"
SYNTHETIC = ROOT / "fixtures" / "synthetic_content_pack"
LEGACY_SAVE = ROOT / "fixtures" / "legacy_format16_synthetic_save.json"
SETUP = CharacterSetupCommand("crew.field-member", "ancestry.baseline", "origin.maintenance", "trait.careful")


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


class NeuralPayloadCustodyTests(unittest.TestCase):
    def tearDown(self) -> None:
        select_content_pack(template_root())

    @contextmanager
    def carrier_pack(self, carrier_id: str = "item.payload-carrier"):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "payload-pack"
            shutil.copytree(PACK, root)
            manifest = json.loads((root / "manifest.json").read_text(encoding="utf-8"))
            manifest["id"] = "payload-custody-pack"
            (root / "manifest.json").write_text(json.dumps(manifest), encoding="utf-8")
            systems = json.loads((root / "systems.json").read_text(encoding="utf-8"))
            systems["items"].append({
                "id": carrier_id,
                "name": "Generic payload carrier",
                "description": "A test-only physical carrier.",
                "slot": None,
                "power": 0,
                "initial": True,
            })
            (root / "systems.json").write_text(json.dumps(systems), encoding="utf-8")
            select_content_pack(root)
            yield root

    def session(self) -> GameSession:
        session, outcome = GameSession.create_configured("payload-custody", SETUP)
        self.assertTrue(outcome.accepted)
        return session

    def carrier(self, session: GameSession, carrier_id: str) -> Item:
        return next(item for item in session._state.crew[0].items if item.id == carrier_id)

    @staticmethod
    def records(prefix: str = "record.payload") -> tuple[NeuralRecord, NeuralRecord]:
        return (
            NeuralRecord(f"{prefix}.first", "crew.initial-operative", "definition.test-first"),
            NeuralRecord(f"{prefix}.second", "crew.initial-operative", "definition.test-second"),
        )

    def kill_initial(self, session: GameSession) -> None:
        move(session, 1, 0, 7)
        while session.world_view().courier_alive:
            move(session, -1, 0)
            self.assertTrue(session.submit(MoveCommand(1, 0)).accepted)

    def kill_initial_and_reach_body(self, session: GameSession) -> None:
        self.kill_initial(session)
        self.assertTrue(session.submit(SelectSuccessorCommand("crew.survivor-one")).accepted)
        move(session, 1, 0, 8)
        move(session, 0, 1)

    def recover(self, session: GameSession, carrier_id: str):
        self.kill_initial_and_reach_body(session)
        outcome = session.submit(RecoverRemainsItemCommand("crew.initial-operative", carrier_id))
        self.assertTrue(outcome.accepted)
        return outcome

    def install_test_devices(self, session: GameSession, carrier_id: str = "item.payload-carrier"):
        donor, recipient, third = session._state.crew
        donor_device = self.carrier(session, carrier_id)
        donor_device.neural_records = self.records("record.donor")
        recipient_device = Item("device.recipient", carrier_id, neural_records=())
        third_device = Item("device.third", carrier_id, neural_records=self.records("record.carried-by-third"))
        recipient.items.append(recipient_device)
        third.items.append(third_device)
        donor.installed_neural_item_id = donor_device.id
        recipient.installed_neural_item_id = recipient_device.id
        third.installed_neural_item_id = third_device.id
        return donor, recipient, third, donor_device, recipient_device, third_device

    def kill_active_at_guard(self, session: GameSession) -> None:
        while session.world_view().courier_alive:
            move(session, -1, 0)
            self.assertTrue(session.submit(MoveCommand(1, 0)).accepted)

    def test_payload_free_legacy_and_explicit_empty_payload_round_trip(self) -> None:
        select_content_pack(SYNTHETIC)
        legacy = GameSession.load(LEGACY_SAVE)
        self.assertTrue(all(item.neural_records is None for item in legacy._state.items))
        with self.carrier_pack() as _:
            session = self.session()
            carrier = self.carrier(session, "item.payload-carrier")
            carrier.neural_records = ()
            payload = save_dict(session)
            carrier_payload = next(item for item in payload["crew"][0]["items"] if item["id"] == "item.payload-carrier")
            self.assertEqual(carrier_payload["neural_records"], [])
            self.assertNotIn("neural_records", payload["crew"][0]["items"][0])
            loaded = game_state_from_dict(payload)
            self.assertEqual(next(item for item in loaded.crew[0].items if item.id == "item.payload-carrier").neural_records, ())

    def test_recovery_transfers_two_records_once_and_survives_save_load(self) -> None:
        with self.carrier_pack("item.renamed-carrier") as _:
            session = self.session()
            records = self.records("record.renamed")
            carrier = self.carrier(session, "item.renamed-carrier")
            carrier.neural_records = records
            maximum_health = session._state.crew[1].maximum_health
            self.recover(session, "item.renamed-carrier")
            donor = session._state.crew[0]
            recipient = session._state.crew[1]
            self.assertFalse(any(item.id == "item.renamed-carrier" for item in donor.items))
            recovered = next(item for item in recipient.items if item.id == "item.renamed-carrier")
            self.assertEqual(recovered.neural_records, records)
            self.assertFalse(recovered.equipped)
            self.assertEqual(recovered.power, 0)
            self.assertEqual(recipient.maximum_health, maximum_health)
            self.assertEqual([record.origin_member_id for record in recovered.neural_records], ["crew.initial-operative"] * 2)
            before = save_dict(session)
            turn = session.world_view().turn
            repeated = session.submit(RecoverRemainsItemCommand("crew.initial-operative", "item.renamed-carrier"))
            self.assertFalse(repeated.accepted)
            self.assertFalse(repeated.time_advanced)
            self.assertEqual(session.world_view().turn, turn)
            self.assertEqual(save_dict(session), before)
            with tempfile.TemporaryDirectory() as directory:
                path = Path(directory) / "recovered.json"
                session.save(path)
                loaded = GameSession.load(path)
            loaded_recipient = next(member for member in loaded._state.crew if member.id == "crew.survivor-one")
            loaded_item = next(item for item in loaded_recipient.items if item.id == "item.renamed-carrier")
            self.assertEqual(loaded_item.neural_records, records)
            self.assertEqual(sum(item.id == "item.renamed-carrier" for member in loaded._state.crew for item in member.items), 1)

    def test_recovered_payload_continuation_matches_uninterrupted_execution(self) -> None:
        with self.carrier_pack() as _:
            uninterrupted = self.session()
            resumed = self.session()
            for session in (uninterrupted, resumed):
                donor, recipient, _, donor_device, recipient_device, _ = self.install_test_devices(session)
                self.recover(session, donor_device.id)
                self.assertIsNone(donor.installed_neural_item_id)
                self.assertEqual(recipient.installed_neural_item_id, recipient_device.id)
            with tempfile.TemporaryDirectory() as directory:
                path = Path(directory) / "recovered.json"
                resumed.save(path)
                resumed = GameSession.load(path)
                move(uninterrupted, -1, 0)
                move(resumed, -1, 0)
            self.assertEqual(save_dict(uninterrupted), save_dict(resumed))

    def test_dead_and_remote_recovery_rejections_preserve_install_references(self) -> None:
        with self.carrier_pack() as _:
            session = self.session()
            donor, recipient, _, donor_device, recipient_device, _ = self.install_test_devices(session)
            self.kill_initial(session)
            before = save_dict(session)
            outcome = session.submit(RecoverRemainsItemCommand(donor.id, donor_device.id))
            self.assertEqual(outcome.result_id, "courier.dead")
            self.assertFalse(outcome.time_advanced)
            self.assertEqual(save_dict(session), before)
            self.assertEqual(donor.installed_neural_item_id, donor_device.id)
            self.assertTrue(session.submit(SelectSuccessorCommand(recipient.id)).accepted)
            before = save_dict(session)
            outcome = session.submit(RecoverRemainsItemCommand(donor.id, donor_device.id))
            self.assertEqual(outcome.result_id, "remains.out-of-range")
            self.assertFalse(outcome.time_advanced)
            self.assertEqual(save_dict(session), before)
            self.assertEqual(donor.installed_neural_item_id, donor_device.id)
            self.assertEqual(recipient.installed_neural_item_id, recipient_device.id)

    def test_malformed_payloads_and_in_memory_serialization_reject(self) -> None:
        with self.carrier_pack() as _:
            session = self.session()
            carrier = self.carrier(session, "item.payload-carrier")
            carrier.neural_records = self.records()
            valid = save_dict(session)
            record = valid["crew"][0]["items"][-1]["neural_records"][0]
            malformed = copy.deepcopy(valid)
            malformed["crew"][0]["items"][-1]["neural_records"] = None
            with self.assertRaises(StateError):
                game_state_from_dict(malformed)
            blank_identifier = copy.deepcopy(valid)
            blank_identifier["crew"][0]["items"][-1]["neural_records"][0]["definition_id"] = ""
            with self.assertRaises(StateError):
                game_state_from_dict(blank_identifier)
            duplicate = copy.deepcopy(valid)
            duplicate["crew"][0]["items"][-1]["neural_records"] = [record, copy.deepcopy(record)]
            with self.assertRaises(StateError):
                game_state_from_dict(duplicate)
            unknown_origin = copy.deepcopy(valid)
            unknown_origin["crew"][0]["items"][-1]["neural_records"][0]["origin_member_id"] = "crew.unknown"
            with self.assertRaises(StateError):
                game_state_from_dict(unknown_origin)
            carrier.neural_records = (NeuralRecord("record.invalid", "crew.unknown", "definition.invalid"),)
            with self.assertRaises(StateError):
                session._state.to_dict()

    def test_optional_installs_accept_empty_payload_and_absent_legacy_reference(self) -> None:
        with self.carrier_pack() as _:
            session = self.session()
            payload = save_dict(session)
            for member in payload["crew"]:
                self.assertIsNone(member["installed_neural_item_id"])
                member.pop("installed_neural_item_id")
            loaded = game_state_from_dict(payload)
            self.assertTrue(all(member.installed_neural_item_id is None for member in loaded.crew))
            donor, recipient, third, donor_device, recipient_device, third_device = self.install_test_devices(session)
            loaded = game_state_from_dict(save_dict(session))
            self.assertEqual(loaded.crew[0].installed_neural_item_id, donor_device.id)
            self.assertEqual(loaded.crew[1].installed_neural_item_id, recipient_device.id)
            self.assertEqual(loaded.crew[2].installed_neural_item_id, third_device.id)
            loaded_third_device = next(item for item in loaded.crew[2].items if item.id == third_device.id)
            self.assertEqual(next(item for item in loaded.crew[1].items if item.id == recipient_device.id).neural_records, ())
            self.assertEqual(loaded_third_device.neural_records[0].origin_member_id, donor.id)
            self.assertNotEqual(loaded_third_device.neural_records[0].origin_member_id, loaded.crew[2].id)

    def test_invalid_install_references_and_in_memory_save_reject_without_overwrite(self) -> None:
        with self.carrier_pack() as _:
            session = self.session()
            donor, recipient, _, donor_device, recipient_device, _ = self.install_test_devices(session)
            valid = save_dict(session)
            for installed_id in ("missing.device", recipient_device.id, "item.maintenance-tool", 1):
                corrupt = copy.deepcopy(valid)
                corrupt["crew"][0]["installed_neural_item_id"] = installed_id
                with self.assertRaises(StateError):
                    game_state_from_dict(corrupt)
            equipped = copy.deepcopy(valid)
            device_row = next(item for item in equipped["crew"][0]["items"] if item["id"] == donor_device.id)
            device_row["equipped"] = True
            with self.assertRaises(StateError):
                game_state_from_dict(equipped)
            donor.installed_neural_item_id = "missing.device"
            with tempfile.TemporaryDirectory() as directory:
                path = Path(directory) / "existing.json"
                path.write_text("valid existing save", encoding="utf-8")
                with self.assertRaises(StateError):
                    session.save(path)
                self.assertEqual(path.read_text(encoding="utf-8"), "valid existing save")
            self.assertEqual(donor.installed_neural_item_id, "missing.device")

    def test_recovery_clears_only_matching_source_install_and_round_trips(self) -> None:
        with self.carrier_pack() as _:
            session = self.session()
            donor, recipient, _, donor_device, recipient_device, _ = self.install_test_devices(session)
            self.kill_initial_and_reach_body(session)
            self.assertEqual(donor.installed_neural_item_id, donor_device.id)
            before = save_dict(session)
            rejected = session.submit(RecoverRemainsItemCommand(donor.id, "missing.item"))
            self.assertFalse(rejected.accepted)
            self.assertEqual(save_dict(session), before)
            outcome = session.submit(RecoverRemainsItemCommand(donor.id, donor_device.id))
            self.assertTrue(outcome.accepted)
            self.assertIsNone(donor.installed_neural_item_id)
            self.assertEqual(recipient.installed_neural_item_id, recipient_device.id)
            carried = next(item for item in recipient.items if item.id == donor_device.id)
            self.assertEqual(carried.neural_records, donor_device.neural_records)
            with tempfile.TemporaryDirectory() as directory:
                path = Path(directory) / "recovered.json"
                session.save(path)
                loaded = GameSession.load(path)
            self.assertIsNone(loaded._state.crew[0].installed_neural_item_id)
            self.assertEqual(loaded._state.crew[1].installed_neural_item_id, recipient_device.id)
            self.assertEqual(next(item for item in loaded._state.crew[1].items if item.id == donor_device.id).neural_records, donor_device.neural_records)

            ordinary = self.session()
            ordinary_donor, _, _, ordinary_device, _, _ = self.install_test_devices(ordinary)
            self.kill_initial_and_reach_body(ordinary)
            self.assertTrue(ordinary.submit(RecoverRemainsItemCommand(ordinary_donor.id, "item.maintenance-tool")).accepted)
            self.assertEqual(ordinary_donor.installed_neural_item_id, ordinary_device.id)

    def test_second_body_recovery_preserves_separate_installs(self) -> None:
        with self.carrier_pack() as _:
            session = self.session()
            donor, recipient, third, donor_device, recipient_device, third_device = self.install_test_devices(session)
            self.kill_initial_and_reach_body(session)
            self.assertTrue(session.submit(RecoverRemainsItemCommand(donor.id, donor_device.id)).accepted)
            self.assertEqual(recipient.installed_neural_item_id, recipient_device.id)
            self.kill_active_at_guard(session)
            self.assertTrue(session.submit(SelectSuccessorCommand(third.id)).accepted)
            move(session, 1, 0, 8)
            move(session, 0, -1)
            self.assertTrue(session.submit(RecoverRemainsItemCommand(recipient.id, donor_device.id)).accepted)
            self.assertEqual(recipient.installed_neural_item_id, recipient_device.id)
            self.assertTrue(session.submit(RecoverRemainsItemCommand(recipient.id, recipient_device.id)).accepted)
            self.assertIsNone(recipient.installed_neural_item_id)
            self.assertEqual(third.installed_neural_item_id, third_device.id)
            recovered_donor = next(item for item in third.items if item.id == donor_device.id)
            self.assertEqual(recovered_donor.neural_records, donor_device.neural_records)
            self.assertEqual(sum(item.id == donor_device.id for member in session._state.crew for item in member.items), 1)
            with tempfile.TemporaryDirectory() as directory:
                path = Path(directory) / "third-successor.json"
                session.save(path)
                loaded = GameSession.load(path)
            self.assertIsNone(loaded._state.crew[1].installed_neural_item_id)
            self.assertEqual(loaded._state.crew[2].installed_neural_item_id, third_device.id)
            self.assertEqual(
                next(item for item in loaded._state.crew[2].items if item.id == donor_device.id).neural_records,
                donor_device.neural_records,
            )

    def test_payload_items_cannot_use_generic_equipment_and_ordinary_gear_can(self) -> None:
        with self.carrier_pack() as _:
            session = self.session()
            donor, recipient, _, donor_device, recipient_device, _ = self.install_test_devices(session)
            before = save_dict(session)
            turn = session.world_view().turn
            for command in (EquipItemCommand(donor_device.id), UnequipItemCommand(donor_device.id)):
                outcome = session.submit(command)
                self.assertEqual(outcome.result_id, "item.neural-payload-not-equippable")
                self.assertFalse(outcome.time_advanced)
                self.assertEqual(session.world_view().turn, turn)
                self.assertEqual(save_dict(session), before)
            self.assertTrue(session.submit(EquipItemCommand("item.maintenance-tool")).accepted)
            self.kill_initial_and_reach_body(session)
            self.assertTrue(session.submit(RecoverRemainsItemCommand(donor.id, donor_device.id)).accepted)
            before = save_dict(session)
            turn = session.world_view().turn
            for item_id in (recipient_device.id, donor_device.id):
                for command in (EquipItemCommand(item_id), UnequipItemCommand(item_id)):
                    outcome = session.submit(command)
                    self.assertEqual(outcome.result_id, "item.neural-payload-not-equippable")
                    self.assertFalse(outcome.time_advanced)
                    self.assertEqual(outcome.events, ())
                    self.assertEqual(session.world_view().turn, turn)
                    self.assertEqual(save_dict(session), before)
