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
    AttackCommand,
    CharacterSetupCommand,
    EquipItemCommand,
    IntegrateNeuralRecordsCommand,
    InteractCommand,
    MoveCommand,
    RecoverRemainsItemCommand,
    SelectSuccessorCommand,
    UnequipItemCommand,
)
from jomon.session import GameSession
from jomon.state import Item, NeuralRecord, Position, StateError, game_state_from_dict
from jomon.views import effective_neural_capabilities, neural_record_view


ROOT = Path(__file__).parent
PACK = ROOT.parents[0] / "jomon" / "content_packs" / "first-playable"
SETUP = CharacterSetupCommand("crew.field-member", "ancestry.baseline", "origin.maintenance", "trait.careful")
BASE = "feature.shared-base"
LATCH = "feature.maintenance-latch"
DONOR = "crew.initial-operative"
RECIPIENT = "crew.survivor-one"
THIRD = "crew.survivor-two"
DONOR_DEVICE = f"{DONOR}:item.neural-carrier"
RECIPIENT_DEVICE = f"{RECIPIENT}:item.neural-carrier"
THIRD_DEVICE = f"{THIRD}:item.neural-carrier"
MAINTENANCE = f"{DONOR}:record.maintenance-practice"
CLOSE_QUARTERS = f"{DONOR}:record.close-quarters-technique"
MAINTENANCE_CAPABILITY = "capability.maintenance-service"
DIAGONAL_CAPABILITY = "capability.melee-diagonal"
PRE_CAPABILITY_FINGERPRINT = "c0282d80c5085a73e6e250cc19d7798ae6266b73527218f3fd77d68ba5b1f3bf"


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


class NeuralCapabilityTests(unittest.TestCase):
    def setUp(self) -> None:
        select_content_pack(PACK)

    def tearDown(self) -> None:
        select_content_pack(template_root())

    def session(self, seed: str = "neural-capabilities") -> GameSession:
        session, outcome = GameSession.create_configured(seed, SETUP)
        self.assertTrue(outcome.accepted)
        return session

    @staticmethod
    def member(session: GameSession, member_id: str):
        return next(member for member in session._state.crew if member.id == member_id)

    @staticmethod
    def item(member, item_id: str):
        return next(item for item in member.items if item.id == item_id)

    @staticmethod
    def set_active_position(session: GameSession, x: int, y: int) -> None:
        """A labelled focused-combat fixture; it preserves the validated position mirror."""
        point = Position(x, y)
        session._state.position = point
        session._state.courier.position = point
        session._state.remembered.add(point)

    def kill_active_at_guard(self, session: GameSession) -> None:
        move(session, 1, 0, 7)
        while session.world_view().courier_alive:
            move(session, -1, 0)
            self.assertTrue(session.submit(MoveCommand(1, 0)).accepted)

    def recover_donor_at_base(self, session: GameSession, *, recover_tool: bool = False):
        self.kill_active_at_guard(session)
        self.assertTrue(session.submit(SelectSuccessorCommand(RECIPIENT)).accepted)
        move(session, 1, 0, 8)
        move(session, 0, 1)
        recovered = session.submit(RecoverRemainsItemCommand(DONOR, DONOR_DEVICE))
        self.assertTrue(recovered.accepted)
        if recover_tool:
            self.assertTrue(session.submit(RecoverRemainsItemCommand(DONOR, "item.maintenance-tool")).accepted)
        move(session, -1, 0, 7)
        self.assertEqual((session._state.position.x, session._state.position.y), (2, 3))
        return self.item(self.member(session, RECIPIENT), DONOR_DEVICE)

    def integrate(self, session: GameSession, source_id: str, *record_ids: str):
        return session.submit(IntegrateNeuralRecordsCommand(BASE, source_id, tuple(record_ids)))

    @contextmanager
    def copied_pack(self, mutate):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "capability-pack"
            shutil.copytree(PACK, root)
            manifest_path = root / "manifest.json"
            manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
            manifest["id"] = "neural-capability-test-pack"
            manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
            systems_path = root / "systems.json"
            systems = json.loads(systems_path.read_text(encoding="utf-8"))
            mutate(systems)
            systems_path.write_text(json.dumps(systems), encoding="utf-8")
            yield root

    def assert_rejected_unchanged(self, session: GameSession, command, result_id: str) -> None:
        before = save_dict(session)
        turn, revision = session._state.turn, session.revision
        outcome = session.submit(command)
        self.assertEqual(
            (outcome.accepted, outcome.changed, outcome.time_advanced, outcome.result_id, outcome.events),
            (False, False, False, result_id, ()),
        )
        self.assertEqual((session._state.turn, session.revision, save_dict(session)), (turn, revision, before))

    def test_effective_capabilities_use_only_the_installed_device_and_deduplicate(self) -> None:
        session = self.session()
        self.assertEqual(
            session.effective_neural_capabilities(),
            (MAINTENANCE_CAPABILITY, DIAGONAL_CAPABILITY),
        )
        self.assertEqual(session.effective_neural_capabilities(RECIPIENT), ())
        source = self.recover_donor_at_base(session)
        self.assertEqual(session.effective_neural_capabilities(), ())
        self.assertEqual(source.neural_records[0].origin_member_id, DONOR)
        self.assertTrue(self.integrate(session, source.id, MAINTENANCE).accepted)
        self.assertEqual(session.effective_neural_capabilities(), (MAINTENANCE_CAPABILITY,))

        def duplicate_capability(systems):
            systems["neural"]["record_definitions"][1]["capability_ids"] = [MAINTENANCE_CAPABILITY]

        with self.copied_pack(duplicate_capability) as root:
            select_content_pack(root)
            deduplicated = self.session("deduplicated")
            self.assertEqual(effective_neural_capabilities(deduplicated._state), (MAINTENANCE_CAPABILITY,))

    def test_capability_catalog_validation_and_no_capability_compatibility(self) -> None:
        for invalid in (
            ["capability.unknown"],
            [MAINTENANCE_CAPABILITY, MAINTENANCE_CAPABILITY],
            "capability.maintenance-service",
            [""],
        ):
            with self.subTest(invalid=invalid), self.copied_pack(
                lambda systems, invalid=invalid: systems["neural"]["record_definitions"][0].__setitem__("capability_ids", invalid),
            ) as root:
                with self.assertRaises(ContentError):
                    load_content_pack(root)

        def no_capabilities(systems):
            for definition in systems["neural"]["record_definitions"]:
                definition.pop("capability_ids", None)
            for feature in systems["world"]["features"]:
                feature.pop("requires_capability_id", None)

        with self.copied_pack(no_capabilities) as root:
            pack = load_content_pack(root)
            self.assertTrue(pack.playable)
            select_content_pack(root)
            self.assertEqual(self.session("no-capabilities").effective_neural_capabilities(), ())

    def test_renamed_record_definition_still_grants_its_declared_capability(self) -> None:
        def rename_maintenance(systems):
            definition = systems["neural"]["record_definitions"][0]
            definition["id"] = "record.renamed-maintenance"
            systems["neural"]["crew_initializers"][0]["record_definition_ids"][0] = definition["id"]

        with self.copied_pack(rename_maintenance) as root:
            select_content_pack(root)
            session = self.session("renamed-maintenance")
            self.assertIn(MAINTENANCE_CAPABILITY, session.effective_neural_capabilities())
            self.assertEqual(
                self.item(self.member(session, DONOR), DONOR_DEVICE).neural_records[0].definition_id,
                "record.renamed-maintenance",
            )

    def test_same_successor_maintenance_requires_tool_capability_and_range(self) -> None:
        session = self.session("maintenance")
        source = self.recover_donor_at_base(session, recover_tool=True)
        tool = self.item(self.member(session, RECIPIENT), "item.maintenance-tool")
        self.assertTrue(session.submit(EquipItemCommand(tool.id)).accepted)
        move(session, 0, -1, 2)
        move(session, 1, 0, 6)
        latch = next(row for row in session.feature_views() if row.id == LATCH)
        self.assertEqual(latch.availability_id, "interaction.requires-capability")
        self.assert_rejected_unchanged(session, InteractCommand(LATCH), "interaction.requires-capability")
        move(session, -1, 0, 6)
        move(session, 0, 1, 2)
        self.assertTrue(self.integrate(session, source.id, MAINTENANCE).accepted)
        move(session, 0, -1, 2)
        move(session, 1, 0, 6)
        self.assertEqual(session.submit(InteractCommand(LATCH)).result_id, "interaction.access-opened")

        alternate = self.session("maintenance-alternate")
        source = self.recover_donor_at_base(alternate, recover_tool=True)
        tool = self.item(self.member(alternate, RECIPIENT), "item.maintenance-tool")
        alternate.submit(EquipItemCommand(tool.id))
        self.assertTrue(self.integrate(alternate, source.id, CLOSE_QUARTERS).accepted)
        move(alternate, 0, -1, 2)
        move(alternate, 1, 0, 6)
        self.assert_rejected_unchanged(alternate, InteractCommand(LATCH), "interaction.requires-capability")

    def test_maintenance_requires_equipped_tool_and_carried_source_does_not_count(self) -> None:
        session = self.session("maintenance-tool")
        source = self.recover_donor_at_base(session, recover_tool=True)
        move(session, 0, -1, 2)
        move(session, 1, 0, 6)
        self.assert_rejected_unchanged(session, InteractCommand(LATCH), "interaction.requires-equipped-tool")
        tool = self.item(self.member(session, RECIPIENT), "item.maintenance-tool")
        session.submit(EquipItemCommand(tool.id))
        self.assert_rejected_unchanged(session, InteractCommand(LATCH), "interaction.requires-capability")
        move(session, -1, 0, 6)
        move(session, 0, 1, 2)
        self.assertTrue(self.integrate(session, source.id, MAINTENANCE).accepted)
        # The same retained capability remains insufficient until the physical
        # tool is equipped, and it does not relax the local-range rule.
        self.assertTrue(session.submit(UnequipItemCommand(tool.id)).accepted)
        self.assert_rejected_unchanged(session, InteractCommand(LATCH), "interaction.out-of-range")
        move(session, 0, -1, 2)
        move(session, 1, 0, 6)
        self.assert_rejected_unchanged(session, InteractCommand(LATCH), "interaction.requires-equipped-tool")
        self.assertTrue(session.submit(EquipItemCommand(tool.id)).accepted)
        self.assertEqual(session.submit(InteractCommand(LATCH)).result_id, "interaction.access-opened")

    def test_initial_operative_keeps_the_existing_maintenance_route(self) -> None:
        session = self.session("initial-route")
        self.assertTrue(session.submit(EquipItemCommand("item.maintenance-tool")).accepted)
        move(session, 0, -1, 2)
        move(session, 1, 0, 6)
        self.assertEqual(session.submit(InteractCommand(LATCH)).result_id, "interaction.access-opened")

    def test_same_successor_diagonal_melee_requires_close_quarters_and_preserves_damage(self) -> None:
        session = self.session("diagonal")
        source = self.recover_donor_at_base(session)
        self.assertTrue(session.submit(EquipItemCommand("crew.survivor-one:item.spare-baton")).accepted)
        self.set_active_position(session, 9, 2)
        self.assert_rejected_unchanged(session, AttackCommand("actor.service-defender"), "attack.rejected")
        self.set_active_position(session, 2, 3)
        self.assertTrue(self.integrate(session, source.id, CLOSE_QUARTERS).accepted)
        self.set_active_position(session, 9, 2)
        before_turn = session._state.turn
        diagonal = session.submit(AttackCommand("actor.service-defender"))
        self.assertEqual((diagonal.result_id, diagonal.time_advanced), ("attack.resolved", True))
        self.assertEqual(session._state.turn, before_turn + 1)
        self.assertEqual(session.actor_view("actor.service-defender").health, 1)
        self.assertFalse(any(event.event_id == "defender.responded" for event in diagonal.events))

        cardinal = self.session("cardinal")
        source = self.recover_donor_at_base(cardinal)
        cardinal.submit(EquipItemCommand("crew.survivor-one:item.spare-baton"))
        self.assertTrue(self.integrate(cardinal, source.id, CLOSE_QUARTERS).accepted)
        self.set_active_position(cardinal, 9, 3)
        outcome = cardinal.submit(AttackCommand("actor.service-defender"))
        self.assertEqual(cardinal.actor_view("actor.service-defender").health, 1)
        self.assertEqual(outcome.events[0].damage, 3)
        self.assertEqual(sum(event.event_id == "defender.responded" for event in outcome.events), 1)

    def test_maintenance_and_carried_close_quarters_do_not_enable_diagonal_or_remote_attack(self) -> None:
        for selected in (None, MAINTENANCE):
            with self.subTest(selected=selected):
                session = self.session(f"no-diagonal-{selected}")
                source = self.recover_donor_at_base(session)
                if selected is not None:
                    self.assertTrue(self.integrate(session, source.id, selected).accepted)
                self.set_active_position(session, 9, 2)
                self.assert_rejected_unchanged(session, AttackCommand("actor.service-defender"), "attack.rejected")

        enabled = self.session("diagonal-range")
        source = self.recover_donor_at_base(enabled)
        self.assertTrue(self.integrate(enabled, source.id, CLOSE_QUARTERS).accepted)
        self.set_active_position(enabled, 8, 1)
        self.assert_rejected_unchanged(enabled, AttackCommand("actor.service-defender"), "attack.rejected")

    def test_replacement_save_load_and_cross_generation_derive_without_a_cache(self) -> None:
        session = self.session("replacement")
        source = self.recover_donor_at_base(session)
        self.assertTrue(self.integrate(session, source.id, MAINTENANCE).accepted)
        self.assertEqual(session.effective_neural_capabilities(), (MAINTENANCE_CAPABILITY,))
        recipient = self.member(session, RECIPIENT)
        replacement = Item(
            "synthetic.close-source", "item.neural-carrier",
            neural_records=(NeuralRecord(CLOSE_QUARTERS, DONOR, "record.close-quarters-technique"),),
        )
        recipient.items.append(replacement)
        self.assertTrue(self.integrate(session, replacement.id, CLOSE_QUARTERS).accepted)
        self.assertEqual(session.effective_neural_capabilities(), (DIAGONAL_CAPABILITY,))
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "replacement.json"
            session.save(path)
            reloaded = GameSession.load(path)
            self.assertEqual(reloaded.effective_neural_capabilities(), (DIAGONAL_CAPABILITY,))
            self.assertEqual(
                tuple(record.id for record in self.item(self.member(reloaded, RECIPIENT), RECIPIENT_DEVICE).neural_records),
                (CLOSE_QUARTERS,),
            )
        session = reloaded
        recipient = self.member(session, RECIPIENT)

        # A focused real-custody setup: B dies with A's retained record; C recovers
        # B's installed device and retains it, preserving A as origin.
        recipient.health = 0
        recipient.alive = False
        session._state.active_member_id = THIRD
        session._state.position = self.member(session, THIRD).position
        session._state.remembered.add(session._state.position)
        third = self.member(session, THIRD)
        third.position = Position(recipient.position.x - 1, recipient.position.y)
        session._state.position = third.position
        session._state.remembered.add(third.position)
        recovered = session.submit(RecoverRemainsItemCommand(RECIPIENT, RECIPIENT_DEVICE))
        self.assertTrue(recovered.accepted)
        device = self.item(third, RECIPIENT_DEVICE)
        self.assertEqual(device.neural_records[0].origin_member_id, DONOR)
        # Move C to the configured base and use the physical recovered device.
        self.set_active_position(session, 2, 3)
        self.assertTrue(self.integrate(session, device.id, CLOSE_QUARTERS).accepted)
        self.assertEqual(session.effective_neural_capabilities(), (DIAGONAL_CAPABILITY,))
        self.assertEqual(self.item(third, THIRD_DEVICE).neural_records[0].origin_member_id, DONOR)

    def test_capability_presentation_and_old_fingerprint_rejection(self) -> None:
        session = self.session("presentation")
        device = self.item(self.member(session, DONOR), DONOR_DEVICE)
        record_view = neural_record_view(session._state, device.neural_records[0])
        self.assertEqual(record_view.capability_ids, (MAINTENANCE_CAPABILITY,))
        self.assertEqual(record_view.capability_display_names, ("Maintenance Service",))
        detail = session.item_detail_view(DONOR_DEVICE)
        self.assertEqual(detail.neural_records[1].capability_display_names, ("Melee Diagonal",))
        old = save_dict(session)
        old["fingerprint"] = PRE_CAPABILITY_FINGERPRINT
        with self.assertRaisesRegex(StateError, "different playable content pack"):
            game_state_from_dict(copy.deepcopy(old))


if __name__ == "__main__":
    unittest.main()
