from __future__ import annotations

import copy
import json
import shutil
import tempfile
import unittest
from pathlib import Path

from jomon.catalog import ContentError, load_content_pack, mechanical_fingerprint, select_content_pack, template_root
from jomon.commands import AttackCommand, CharacterSetupCommand, EquipItemCommand, InteractCommand, MoveCommand
from jomon.session import GameSession
from jomon.state import StateError


ROOT = Path(__file__).parent
PACK = ROOT.parents[0] / "jomon" / "content_packs" / "first-playable"
SYNTHETIC = ROOT / "fixtures" / "synthetic_content_pack"
LEGACY_SAVE = ROOT / "fixtures" / "legacy_format16_synthetic_save.json"
SETUP = CharacterSetupCommand("crew.field-member", "ancestry.baseline", "origin.maintenance", "trait.careful")


def state_dict(session: GameSession) -> dict:
    with tempfile.TemporaryDirectory() as directory:
        path = Path(directory) / "state.json"
        session.save(path)
        return json.loads(path.read_text(encoding="utf-8"))


def move(session: GameSession, dx: int, dy: int, count: int = 1) -> None:
    for _ in range(count):
        outcome = session.submit(MoveCommand(dx, dy))
        if not outcome.accepted:
            raise AssertionError(outcome.result_id)


def to_latch(session: GameSession) -> None:
    move(session, 0, -1, 2)
    move(session, 1, 0, 6)


def to_objective_by_bypass(session: GameSession) -> None:
    move(session, 1, 0, 8)
    move(session, 0, 1, 2)


def return_from_objective_by_bypass(session: GameSession) -> None:
    move(session, 0, -1, 2)
    move(session, -1, 0, 14)
    move(session, 0, 1, 2)


def to_guard(session: GameSession) -> None:
    move(session, 1, 0, 7)


def to_objective_by_main(session: GameSession) -> None:
    move(session, 1, 0, 7)


def return_from_objective_by_main(session: GameSession) -> None:
    move(session, -1, 0, 14)


class FirstPlayableTests(unittest.TestCase):
    def setUp(self) -> None:
        select_content_pack(PACK)

    def tearDown(self) -> None:
        select_content_pack(template_root())

    def session(self) -> GameSession:
        session, outcome = GameSession.create_configured("first-playable-seed", SETUP)
        self.assertTrue(outcome.accepted)
        return session

    def tool_run_to_return(self, session: GameSession) -> None:
        self.assertTrue(session.submit(EquipItemCommand("item.maintenance-tool")).accepted)
        to_latch(session)
        self.assertEqual(session.submit(InteractCommand("feature.maintenance-latch")).result_id, "interaction.access-opened")
        to_objective_by_bypass(session)
        self.assertEqual(session.submit(InteractCommand("feature.diagnostic-cache")).result_id, "interaction.objective-acquired")
        return_from_objective_by_bypass(session)
        self.assertEqual(session.submit(InteractCommand("feature.shared-base")).result_id, "interaction.operation-delivered")

    def combat_run_to_return(self, session: GameSession) -> None:
        self.assertTrue(session.submit(EquipItemCommand("item.defense-baton")).accepted)
        to_guard(session)
        self.assertEqual(session.submit(AttackCommand("actor.service-defender")).result_id, "attack.resolved")
        self.assertEqual(session.submit(AttackCommand("actor.service-defender")).result_id, "attack.resolved")
        to_objective_by_main(session)
        self.assertEqual(session.submit(InteractCommand("feature.diagnostic-cache")).result_id, "interaction.objective-acquired")
        return_from_objective_by_main(session)
        self.assertEqual(session.submit(InteractCommand("feature.shared-base")).result_id, "interaction.operation-delivered")

    def test_first_pack_is_playable_and_setup_has_mechanical_effect(self) -> None:
        self.assertTrue(load_content_pack(PACK).playable)
        sturdy = CharacterSetupCommand("crew.security-member", "ancestry.resilient", "origin.streetwise", "trait.durable")
        session, outcome = GameSession.create_configured("setup", sturdy)
        self.assertTrue(outcome.accepted)
        courier = session.actor_view("courier")
        self.assertEqual((courier.health, courier.maximum_health), (14, 14))
        self.assertEqual(session.submit(sturdy).result_id, "setup.locked")

    def test_closed_access_and_living_defender_block_real_movement(self) -> None:
        session = self.session()
        to_latch(session)
        self.assertTrue(session.submit(MoveCommand(1, 0)).accepted)
        before = state_dict(session)
        rejected = session.submit(MoveCommand(1, 0))
        self.assertEqual(rejected.result_id, "move.rejected")
        self.assertFalse(rejected.time_advanced)
        self.assertEqual(rejected.events, ())
        self.assertEqual(state_dict(session), before)
        session = self.session()
        to_guard(session)
        before = state_dict(session)
        rejected = session.submit(MoveCommand(1, 0))
        self.assertEqual(rejected.result_id, "move.blocked-by-actor")
        self.assertEqual(rejected.events, ())
        self.assertEqual(state_dict(session), before)

    def test_tool_only_run_opens_bypass_keeps_guard_alive_and_returns_once(self) -> None:
        session = self.session()
        self.tool_run_to_return(session)
        gate = next(cell for cell in session.world_view().cells if cell.position.x == 10 and cell.position.y == 1)
        operation = session.operation_views()[0]
        guard = session.actor_view("actor.service-defender")
        self.assertEqual(gate.terrain_id, "terrain.gate.open")
        self.assertTrue(guard.alive)
        self.assertEqual(operation.state_id, "returned")
        self.assertEqual(operation.evidence_method_ids, ("method.maintenance-bypass",))
        self.assertEqual(operation.resolution_method_ids, ("method.maintenance-bypass",))
        self.assertEqual(operation.consequence_ids, ("consequence.maintenance-bypass-open",))
        self.assertFalse(any(item.kind_id == "item.diagnostic-module" for item in session.inventory_view()))
        turn = session.world_view().turn
        repeated = session.submit(InteractCommand("feature.shared-base"))
        self.assertFalse(repeated.accepted)
        self.assertEqual(session.world_view().turn, turn)

    def test_combat_only_run_defeats_guard_keeps_bypass_closed_and_is_winnable(self) -> None:
        session = self.session()
        self.combat_run_to_return(session)
        gate = next(cell for cell in session.world_view().cells if cell.position.x == 10 and cell.position.y == 1)
        operation = session.operation_views()[0]
        guard = session.actor_view("actor.service-defender")
        courier = session.actor_view("courier")
        self.assertEqual(gate.terrain_id, "terrain.gate.closed")
        self.assertFalse(guard.alive)
        self.assertTrue(courier.alive)
        self.assertEqual(courier.health, 8)
        self.assertEqual(operation.state_id, "returned")
        self.assertEqual(operation.evidence_method_ids, ("method.main-clearance",))
        self.assertEqual(operation.resolution_method_ids, ("method.main-clearance",))
        self.assertEqual(operation.consequence_ids, ("consequence.service-defender-defeated",))

    def test_mixed_run_preserves_both_physical_evidence_and_one_objective(self) -> None:
        session = self.session()
        session.submit(EquipItemCommand("item.maintenance-tool"))
        to_latch(session)
        session.submit(InteractCommand("feature.maintenance-latch"))
        move(session, -1, 0, 6)
        move(session, 0, 1, 2)
        to_guard(session)
        session.submit(EquipItemCommand("item.defense-baton"))
        session.submit(AttackCommand("actor.service-defender"))
        session.submit(AttackCommand("actor.service-defender"))
        to_objective_by_main(session)
        acquired = session.submit(InteractCommand("feature.diagnostic-cache"))
        self.assertTrue(acquired.accepted)
        operation = session.operation_views()[0]
        self.assertEqual(operation.resolution_method_ids, ("method.main-clearance", "method.maintenance-bypass"))
        self.assertEqual(len([item for item in session.inventory_view() if item.kind_id == "item.diagnostic-module"]), 1)
        repeated = session.submit(InteractCommand("feature.diagnostic-cache"))
        self.assertFalse(repeated.accepted)
        self.assertEqual(repeated.events, ())

    def test_interaction_rejections_are_mutation_free(self) -> None:
        session = self.session()
        before = state_dict(session)
        for command in (InteractCommand("missing.feature"), InteractCommand("feature.maintenance-latch"), InteractCommand("feature.diagnostic-cache")):
            outcome = session.submit(command)
            self.assertFalse(outcome.accepted)
            self.assertFalse(outcome.time_advanced)
            self.assertEqual(outcome.events, ())
            self.assertEqual(state_dict(session), before)
        to_latch(session)
        before = state_dict(session)
        outcome = session.submit(InteractCommand("feature.maintenance-latch"))
        self.assertEqual(outcome.result_id, "interaction.requires-equipped-tool")
        self.assertEqual(state_dict(session), before)

    def test_defender_response_order_invalid_commands_and_death_lock(self) -> None:
        session = self.session()
        to_guard(session)
        self.assertEqual(session.actor_view("courier").health, 9)
        before_queries = state_dict(session)
        session.world_view()
        session.actor_views()
        session.feature_views()
        session.operation_views()
        self.assertEqual(state_dict(session), before_queries)
        rejected = session.submit(MoveCommand(1, 0))
        self.assertFalse(rejected.accepted)
        self.assertEqual(rejected.events, ())
        self.assertEqual(session.actor_view("courier").health, 9)
        for _ in range(9):
            self.assertTrue(session.submit(MoveCommand(-1, 0)).accepted)
            response = session.submit(MoveCommand(1, 0))
            self.assertEqual(sum(event.event_id == "defender.responded" for event in response.events), 1)
        self.assertFalse(session.actor_view("courier").alive)
        before = state_dict(session)
        after_death = session.submit(InteractCommand("feature.maintenance-latch"))
        self.assertEqual(after_death.result_id, "courier.dead")
        self.assertEqual(after_death.events, ())
        self.assertEqual(state_dict(session), before)

    def test_dead_defender_never_responds_after_its_death(self) -> None:
        session = self.session()
        session.submit(EquipItemCommand("item.defense-baton"))
        to_guard(session)
        session.submit(AttackCommand("actor.service-defender"))
        fatal = session.submit(AttackCommand("actor.service-defender"))
        self.assertFalse(session.actor_view("actor.service-defender").alive)
        self.assertFalse(any(event.event_id == "defender.responded" for event in fatal.events))

    def test_save_load_checkpoints_and_uninterrupted_continuation_match(self) -> None:
        uninterrupted = self.session()
        resumed = self.session()
        for session in (uninterrupted, resumed):
            session.submit(EquipItemCommand("item.maintenance-tool"))
            to_latch(session)
            session.submit(InteractCommand("feature.maintenance-latch"))
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "opened.json"
            resumed.save(path)
            resumed = GameSession.load(path)
            for session in (uninterrupted, resumed):
                to_objective_by_bypass(session)
                session.submit(InteractCommand("feature.diagnostic-cache"))
            acquired_path = Path(directory) / "acquired.json"
            resumed.save(acquired_path)
            resumed = GameSession.load(acquired_path)
            for session in (uninterrupted, resumed):
                return_from_objective_by_bypass(session)
                session.submit(InteractCommand("feature.shared-base"))
            self.assertEqual(state_dict(uninterrupted), state_dict(resumed))
            returned_path = Path(directory) / "returned.json"
            resumed.save(returned_path)
            loaded = GameSession.load(returned_path)
            self.assertEqual(loaded.operation_views()[0].state_id, "returned")
            self.assertEqual(loaded.actor_view("actor.service-defender").alive, True)

            combat = self.session()
            combat.submit(EquipItemCommand("item.defense-baton"))
            to_guard(combat)
            combat.submit(AttackCommand("actor.service-defender"))
            combat.submit(AttackCommand("actor.service-defender"))
            defeated_path = Path(directory) / "defeated.json"
            combat.save(defeated_path)
            defeated = GameSession.load(defeated_path)
            self.assertFalse(defeated.actor_view("actor.service-defender").alive)
            self.assertEqual(
                defeated.operation_views()[0].evidence_method_ids,
                ("method.main-clearance",),
            )

    def test_genuine_prechange_format16_synthetic_save_loads_with_defaults(self) -> None:
        select_content_pack(SYNTHETIC)
        loaded = GameSession.load(LEGACY_SAVE)
        self.assertEqual(loaded.world_view().courier_position.x, 2)
        self.assertEqual(loaded.world_view().turn, 1)
        self.assertEqual(loaded.operation_views(), ())
        select_content_pack(PACK)
        new_session = self.session()
        corrupt = state_dict(new_session)
        corrupt.pop("operations")
        with self.assertRaises(StateError):
            __import__("jomon.state", fromlist=["game_state_from_dict"]).game_state_from_dict(corrupt)
        corrupt = state_dict(new_session)
        corrupt["operations"][0].update({
            "state": "resolved",
            "objective_item_instance_id": "objective.operation.recover-diagnostic",
            "evidence_method_ids": ["method.maintenance-bypass"],
            "resolution_method_ids": ["method.maintenance-bypass"],
        })
        with self.assertRaises(StateError):
            __import__("jomon.state", fromlist=["game_state_from_dict"]).game_state_from_dict(corrupt)

    def test_operation_schema_and_presentation_are_validated_without_story_id_branches(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "alternate"
            shutil.copytree(PACK, root)
            manifest = json.loads((root / "manifest.json").read_text())
            manifest["id"] = "alternate-playable"
            (root / "manifest.json").write_text(json.dumps(manifest), encoding="utf-8")
            systems_text = (root / "systems.json").read_text(encoding="utf-8")
            for old, new in (
                ("feature.maintenance-latch", "feature.alt-latch"),
                ("access.maintenance-bypass", "access.alt-bypass"),
                ("item.maintenance-tool", "item.alt-tool"),
                ("method.maintenance-bypass", "method.alt-bypass"),
                ("consequence.maintenance-bypass-open", "consequence.alt-bypass-open"),
            ):
                systems_text = systems_text.replace(old, new)
            (root / "systems.json").write_text(systems_text, encoding="utf-8")
            select_content_pack(root)
            session, _ = GameSession.create_configured("alternate", SETUP)
            session.submit(EquipItemCommand("item.alt-tool"))
            to_latch(session)
            self.assertEqual(session.submit(InteractCommand("feature.alt-latch")).result_id, "interaction.access-opened")
            self.assertEqual(session.operation_views()[0].evidence_method_ids, ("method.alt-bypass",))
            invalid = json.loads((root / "systems.json").read_text())
            invalid["world"]["features"][2]["access_id"] = "access.no-latch"
            (root / "systems.json").write_text(json.dumps(invalid), encoding="utf-8")
            with self.assertRaises(ContentError):
                load_content_pack(root)
            invalid = json.loads((PACK / "systems.json").read_text())
            invalid["operations"][0]["methods"][1]["requires_defeated_actor_id"] = "actor.missing"
            (root / "systems.json").write_text(json.dumps(invalid), encoding="utf-8")
            with self.assertRaises(ContentError):
                load_content_pack(root)

    def test_display_only_pack_changes_do_not_change_fingerprint_or_outcome(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory) / "presentation"
            shutil.copytree(PACK, root)
            systems = json.loads((root / "systems.json").read_text())
            systems["world"]["features"][1]["description"] = "Rewritten presentation only."
            systems["operations"][0]["name"] = "Different title"
            systems["operations"][0]["objective"] = "Different wording"
            (root / "systems.json").write_text(json.dumps(systems), encoding="utf-8")
            self.assertEqual(mechanical_fingerprint(load_content_pack(PACK)), mechanical_fingerprint(load_content_pack(root)))
            select_content_pack(PACK)
            original = self.session()
            self.tool_run_to_return(original)
            original_state = state_dict(original)
            select_content_pack(root)
            rewritten, outcome = GameSession.create_configured("first-playable-seed", SETUP)
            self.assertTrue(outcome.accepted)
            self.tool_run_to_return(rewritten)
            rewritten_state = state_dict(rewritten)
            for key in ("fingerprint", "turn", "position", "crew", "active_member_id", "actors", "operations", "opened_access_ids"):
                self.assertEqual(rewritten_state[key], original_state[key])
