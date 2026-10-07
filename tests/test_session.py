from __future__ import annotations

import copy
import json
import subprocess
import sys
import tempfile
import unittest
from dataclasses import FrozenInstanceError
from pathlib import Path

from jomon.actions import advance_world, attack, choose_relic, interact, move
from jomon.commands import (
    AdvanceWorldCommand, AttackCommand, InteractCommand, MoveCommand,
    SelectCarriedRelicCommand, SetAutoPlaceCommand,
)
from jomon.session import GameSession
from jomon.state import Position, Threat, create_world
from jomon.inventory import create_item
from jomon.terminal import InputEvent, InventoryView, OverlayView, _handle_inventory, _handle_overlay_view
from jomon.world import is_walkable


def mechanical_payload(state):
    """Compare simulation state without presentation history/cache output."""
    payload = state.to_dict()
    payload.pop("messages", None)
    payload.pop("history", None)
    payload.pop("chronicle", None)
    return payload


def armed_state(seed: str):
    state = create_world(seed)
    state.location, state.position, state.world_time = "region", Position(40, 25), 8
    state.weather, state.weapon = "clear", "billhook"
    for z in (-1, 0, 1):
        for y in range(20, 31):
            for x in range(30, 55):
                state.region.tile_changes[f"{x},{y},{z}"] = "."
    target = Threat("session-target", "session target", "pursuer", Position(41, 25), 20, 20,
                    status="engaged", morale=8)
    state.threats = [target]
    return state, target.id


class GameSessionTests(unittest.TestCase):
    def test_modules_are_headless(self):
        code = (
            "import sys; import jomon.commands, jomon.session, jomon.views; "
            "assert 'curses' not in sys.modules"
        )
        result = subprocess.run([sys.executable, "-c", code], check=False, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)

    def test_move_interaction_attack_and_advance_match_existing_reducers(self):
        source = create_world("session move")
        dx, dy = next(
            (dx, dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
            if is_walkable(source, Position(source.position.x + dx, source.position.y + dy, source.position.z))
        )
        legacy, through_session = copy.deepcopy(source), copy.deepcopy(source)
        move(legacy, dx, dy)
        outcome = GameSession(through_session).submit(MoveCommand(dx, dy))
        self.assertEqual(outcome.result_id, "move.ok")
        self.assertEqual(mechanical_payload(through_session), mechanical_payload(legacy))

        legacy, through_session = copy.deepcopy(source), copy.deepcopy(source)
        interact(legacy)
        session = GameSession(through_session)
        choice = session.interaction_view().options[0]
        outcome = session.submit(InteractCommand(choice.target_id, choice.interaction_id))
        self.assertEqual(outcome.result_id, "interaction.resolved")
        self.assertEqual(mechanical_payload(through_session), mechanical_payload(legacy))

        legacy, through_session = copy.deepcopy(source), copy.deepcopy(source)
        advance_world(legacy, steps=2)
        outcome = GameSession(through_session).submit(AdvanceWorldCommand(steps=2))
        self.assertEqual(outcome.result_id, "world.advanced")
        self.assertEqual(mechanical_payload(through_session), mechanical_payload(legacy))

        legacy, target_id = armed_state("session attack")
        through_session = copy.deepcopy(legacy)
        attack(legacy, target_id)
        outcome = GameSession(through_session).submit(AttackCommand(target_id))
        self.assertEqual(outcome.result_id, "attack.resolved")
        self.assertEqual(mechanical_payload(through_session), mechanical_payload(legacy))

    def test_relic_selection_and_terminal_adoption_use_public_operation(self):
        legacy = create_world("session relic")
        legacy.relics["river-glass ward"] = 1
        through_session = copy.deepcopy(legacy)
        choose_relic(legacy, "river-glass ward")
        session = GameSession(through_session)
        outcome = session.submit(SelectCarriedRelicCommand("river-glass ward"))
        self.assertTrue(outcome.accepted)
        self.assertEqual(outcome.result_id, "relic.selected")
        self.assertEqual(mechanical_payload(through_session), mechanical_payload(legacy))

        terminal_state = create_world("terminal relic command")
        terminal_state.relics["river-glass ward"] = 1
        create_item(
            terminal_state, "relic:river-glass ward", "session fixture",
            location="pack", owner_id=terminal_state.active_courier_id,
        )
        terminal_session = GameSession(terminal_state)
        closed, quit_requested = _handle_overlay_view(
            terminal_state, OverlayView("relic:select"), InputEvent("key", key=ord("1")), terminal_session,
        )
        self.assertTrue(closed)
        self.assertFalse(quit_requested)
        self.assertEqual(terminal_state.carried_relic, "river-glass ward")
        self.assertEqual(terminal_session.revision, 1)

    def test_auto_place_changes_only_through_session_command(self):
        state = create_world("session auto place")
        session = GameSession(state)
        view = InventoryView.begin(state)
        _handle_inventory(state, view, InputEvent("key", key=ord("z")), session)
        self.assertFalse(session.auto_place_enabled)
        self.assertFalse(state.auto_place_enabled)
        self.assertEqual(session.revision, 1)
        self.assertEqual(session.submit(SetAutoPlaceCommand(False)).result_id, "auto_place.rejected")

    def test_invalid_commands_are_atomic_and_do_not_consume_rng(self):
        state, _ = armed_state("session invalid")
        session = GameSession(state)
        before = mechanical_payload(state)
        revision = session.revision
        self.assertEqual(session.submit(MoveCommand(0, 0)).result_id, "move.invalid")
        self.assertEqual(session.submit(InteractCommand("missing", "interact.current")).result_id, "interaction.invalid")
        self.assertEqual(session.submit(AttackCommand("missing")).result_id, "attack.rejected")
        self.assertEqual(session.submit(SelectCarriedRelicCommand("missing")).result_id, "relic.rejected")
        self.assertEqual(session.revision, revision)
        self.assertEqual(mechanical_payload(state), before)

    def test_views_are_immutable_and_observational(self):
        state = create_world("session views")
        session = GameSession(state)
        before = mechanical_payload(state)
        world = session.world_view()
        actors = session.actor_views()
        interactions = session.interaction_view()
        self.assertEqual(mechanical_payload(state), before)
        self.assertEqual(world.courier_position, state.position)
        self.assertEqual(interactions.options[0].interaction_id, "interact.current")
        self.assertTrue(any(actor.id == state.active_courier_id for actor in actors))
        with self.assertRaises(FrozenInstanceError):
            world.width = 1
        with self.assertRaises(AttributeError):
            world.cells.append(None)

    def test_session_save_load_keeps_format_fifteen_without_session_state(self):
        session = GameSession.create("session save")
        session.submit(AdvanceWorldCommand())
        with tempfile.TemporaryDirectory() as directory:
            path = session.save(Path(directory) / "session.json")
            payload = json.loads(path.read_text(encoding="utf-8"))
            self.assertEqual(payload["save_format"], 15)
            self.assertNotIn("revision", payload)
            self.assertNotIn("session", payload)
            restored = GameSession.load(path)
        self.assertEqual(restored.revision, 0)
        self.assertEqual(restored.world_view().courier_position, session.world_view().courier_position)
        self.assertEqual(restored.submit(AdvanceWorldCommand()).result_id, "world.advanced")
        self.assertEqual(restored.revision, 1)

    def test_terminal_no_longer_reaches_known_private_or_raw_mutation_paths(self):
        source = Path("jomon/terminal.py").read_text(encoding="utf-8")
        self.assertNotIn("_advance_world", source)
        self.assertNotRegex(source, r"state\.carried_relic\s*=(?!=)")
        self.assertNotRegex(source, r"state\.auto_place_enabled\s*=(?!=)")


if __name__ == "__main__":
    unittest.main()
