from __future__ import annotations

import copy
import json
import shutil
import tempfile
import unittest
from contextlib import contextmanager
from pathlib import Path

from jomon.catalog import ContentError, load_content_pack, select_content_pack, template_root
from jomon.commands import CharacterSetupCommand, MoveCommand, RecoverRemainsItemCommand, SelectSuccessorCommand
from jomon.session import GameSession
from jomon.state import Item, NeuralRecord, StateError, game_state_from_dict


ROOT = Path(__file__).parent
PACK = ROOT.parents[0] / "jomon" / "content_packs" / "first-playable"
SYNTHETIC = ROOT / "fixtures" / "synthetic_content_pack"
LEGACY_SAVE = ROOT / "fixtures" / "legacy_format16_synthetic_save.json"
PRE_NEURAL_FINGERPRINT = "d6b9688a18bed3ca7dc06f11d20485a4807761324b84c686d96756d8cc3f020e"
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


class NeuralStartupTests(unittest.TestCase):
    def setUp(self) -> None:
        select_content_pack(PACK)

    def tearDown(self) -> None:
        select_content_pack(template_root())

    def session(self, seed: str = "neural-startup") -> GameSession:
        session, outcome = GameSession.create_configured(seed, SETUP)
        self.assertTrue(outcome.accepted)
        return session

    @contextmanager
    def copied_pack(self, mutate):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "alternate"
            shutil.copytree(PACK, root)
            manifest = json.loads((root / "manifest.json").read_text(encoding="utf-8"))
            manifest["id"] = "neural-startup-alternate"
            (root / "manifest.json").write_text(json.dumps(manifest), encoding="utf-8")
            systems = json.loads((root / "systems.json").read_text(encoding="utf-8"))
            mutate(systems)
            (root / "systems.json").write_text(json.dumps(systems), encoding="utf-8")
            yield root

    @staticmethod
    def device(member):
        return next(item for item in member.items if item.id == member.installed_neural_item_id)

    def recover_initial_device(self, session: GameSession) -> None:
        move(session, 1, 0, 7)
        while session.world_view().courier_alive:
            move(session, -1, 0)
            self.assertTrue(session.submit(MoveCommand(1, 0)).accepted)
        self.assertTrue(session.submit(SelectSuccessorCommand("crew.survivor-one")).accepted)
        move(session, 1, 0, 8)
        move(session, 0, 1)
        outcome = session.submit(RecoverRemainsItemCommand(
            "crew.initial-operative", "crew.initial-operative:item.neural-carrier",
        ))
        self.assertTrue(outcome.accepted)

    def test_normal_startup_seeds_three_deterministic_devices_once(self) -> None:
        first = self.session("same-startup")
        second = self.session("same-startup")
        expected = {
            "crew.initial-operative": (
                "crew.initial-operative:item.neural-carrier",
                (
                    ("crew.initial-operative:record.maintenance-practice", "crew.initial-operative", "record.maintenance-practice"),
                    ("crew.initial-operative:record.close-quarters-technique", "crew.initial-operative", "record.close-quarters-technique"),
                ),
            ),
            "crew.survivor-one": ("crew.survivor-one:item.neural-carrier", ()),
            "crew.survivor-two": ("crew.survivor-two:item.neural-carrier", ()),
        }
        for session in (first, second):
            self.assertEqual(session._state.setup, {"crew": "crew.field-member", "ancestry": "ancestry.baseline", "origin": "origin.maintenance", "trait": "trait.careful"})
            self.assertEqual([item.id for item in session._state.crew[0].items], ["item.maintenance-tool", "item.defense-baton", "crew.initial-operative:item.neural-carrier"])
            self.assertEqual(sum(item.neural_records is not None for member in session._state.crew for item in member.items), 3)
            for member in session._state.crew:
                item = self.device(member)
                device_id, records = expected[member.id]
                self.assertEqual((member.installed_neural_item_id, item.id, item.kind, item.power, item.equipped), (device_id, device_id, "item.neural-carrier", 0, False))
                self.assertEqual(tuple((record.id, record.origin_member_id, record.definition_id) for record in item.neural_records), records)
            self.assertEqual(session.submit(SETUP).result_id, "setup.locked")

    def test_renamed_and_omitted_neural_sections_are_not_special_cased(self) -> None:
        def rename(systems):
            text = json.dumps(systems)
            for old, new in (
                ("crew.initial-operative", "crew.alternate-active"),
                ("crew.survivor-one", "crew.alternate-one"),
                ("crew.survivor-two", "crew.alternate-two"),
                ("item.neural-carrier", "item.alternate-carrier"),
                ("record.maintenance-practice", "record.alternate-first"),
                ("record.close-quarters-technique", "record.alternate-second"),
            ):
                text = text.replace(old, new)
            systems.clear()
            systems.update(json.loads(text))

        with self.copied_pack(rename) as root:
            select_content_pack(root)
            session = self.session()
            active = session._state.crew[0]
            self.assertEqual(active.installed_neural_item_id, "crew.alternate-active:item.alternate-carrier")
            self.assertEqual(
                tuple(record.definition_id for record in self.device(active).neural_records),
                ("record.alternate-first", "record.alternate-second"),
            )

        with self.copied_pack(lambda systems: systems.pop("neural")) as root:
            select_content_pack(root)
            session = self.session()
            self.assertTrue(all(member.installed_neural_item_id is None for member in session._state.crew))
            self.assertFalse(any(item.neural_records is not None for member in session._state.crew for item in member.items))
            opaque = Item("opaque-carrier", "item.neural-carrier", neural_records=(
                NeuralRecord("opaque-record", "crew.initial-operative", "definition.opaque"),
            ))
            session._state.crew[0].items.append(opaque)
            self.assertEqual(next(item for item in game_state_from_dict(save_dict(session)).crew[0].items if item.id == opaque.id).neural_records, opaque.neural_records)

    def test_invalid_neural_definitions_initializers_and_runtime_definition_reject(self) -> None:
        mutations = (
            lambda systems: systems["neural"]["record_definitions"].append(copy.deepcopy(systems["neural"]["record_definitions"][0])),
            lambda systems: systems["neural"]["crew_initializers"][0].update({"record_definition_ids": ["record.missing"]}),
            lambda systems: systems["neural"]["crew_initializers"][0].update({"carrier_item_kind_id": "item.maintenance-tool"}),
            lambda systems: systems["neural"]["crew_initializers"][0].update({"record_definition_ids": ["record.maintenance-practice", "record.maintenance-practice"]}),
            lambda systems: systems["neural"]["crew_initializers"].append(copy.deepcopy(systems["neural"]["crew_initializers"][0])),
            lambda systems: systems["neural"]["crew_initializers"][0].update({"carrier_item_kind_id": "item.missing"}),
            lambda systems: systems["neural"].pop("record_definitions"),
        )
        for mutate in mutations:
            with self.copied_pack(mutate) as root:
                with self.assertRaises(ContentError):
                    load_content_pack(root)

        payload = save_dict(self.session())
        payload["crew"][0]["items"][-1]["neural_records"][0]["definition_id"] = "record.unknown"
        with self.assertRaises(StateError):
            game_state_from_dict(payload)
        session = self.session()
        device = self.device(session._state.crew[0])
        device.neural_records = (NeuralRecord("record.invalid", "crew.initial-operative", "record.unknown"),)
        with self.assertRaises(StateError):
            session._state.to_dict()

    def test_startup_device_recovery_and_resume_do_not_regenerate_devices(self) -> None:
        uninterrupted = self.session("recovery")
        resumed = self.session("recovery")
        for session in (uninterrupted, resumed):
            self.recover_initial_device(session)
            donor, recipient, _ = session._state.crew
            self.assertIsNone(donor.installed_neural_item_id)
            self.assertFalse(any(item.id == "crew.initial-operative:item.neural-carrier" for item in donor.items))
            self.assertEqual(recipient.installed_neural_item_id, "crew.survivor-one:item.neural-carrier")
            carried = next(item for item in recipient.items if item.id == "crew.initial-operative:item.neural-carrier")
            self.assertEqual(tuple(record.origin_member_id for record in carried.neural_records), ("crew.initial-operative", "crew.initial-operative"))
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "extracted.json"
            resumed.save(path)
            resumed = GameSession.load(path)
            donor, recipient, _ = resumed._state.crew
            self.assertIsNone(donor.installed_neural_item_id)
            self.assertFalse(any(item.id == "crew.initial-operative:item.neural-carrier" for item in donor.items))
            self.assertEqual(recipient.installed_neural_item_id, "crew.survivor-one:item.neural-carrier")
            move(uninterrupted, -1, 0)
            move(resumed, -1, 0)
        self.assertEqual(save_dict(uninterrupted), save_dict(resumed))

    def test_carried_payload_items_cannot_be_serialized_equipped(self) -> None:
        for member_id in ("crew.initial-operative", "crew.survivor-one"):
            session = self.session(member_id)
            member = next(row for row in session._state.crew if row.id == member_id)
            item = self.device(member)
            member.installed_neural_item_id = None
            item.equipped = True
            with self.assertRaises(StateError):
                session._state.to_dict()

    def test_pre_neural_fingerprint_rejects_and_legacy_synthetic_stays_compatible(self) -> None:
        session = self.session()
        self.assertNotEqual(session._state.fingerprint, PRE_NEURAL_FINGERPRINT)
        payload = save_dict(session)
        payload["fingerprint"] = PRE_NEURAL_FINGERPRINT
        with self.assertRaises(StateError):
            game_state_from_dict(payload)
        select_content_pack(SYNTHETIC)
        legacy = GameSession.load(LEGACY_SAVE)
        self.assertEqual(legacy.world_view().turn, 1)
        self.assertEqual(legacy._state.crew, [])
