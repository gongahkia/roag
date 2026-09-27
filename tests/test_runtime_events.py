from __future__ import annotations

import copy
import json
import os
import subprocess
import sys
import tempfile
import unittest
from dataclasses import FrozenInstanceError
from pathlib import Path

from jomon.actions import attack
from jomon.travel import choose_destination
from jomon.commands import (
    AttackCommand, InteractCommand, MoveCommand, RetreatCommand,
    SelectCarriedRelicCommand, EquipItemCommand, TravelCommand, UnequipItemCommand,
)
from jomon.runtime_events import (
    ActorDefeated, ActorMoved, AttackResolved, CarriedRelicSelectionChanged,
    DamageApplied, InteractionResolved, RetreatResolved, StatusChanged, ItemEquipped,
    ItemUnequipped, TravelResolved,
)
from jomon.session import GameSession
from jomon.state import Position, SoundEvent, Threat, create_world
from jomon.world import is_walkable
from test_content_packs import alternate_pack


def armed_state(seed: str, health: int = 20):
    state = create_world(seed)
    state.location, state.position, state.world_time = "region", Position(40, 25), 8
    state.weather, state.weapon = "clear", "billhook"
    for z in (-1, 0, 1):
        for y in range(20, 31):
            for x in range(30, 55):
                state.region.tile_changes[f"{x},{y},{z}"] = "."
    target = Threat("runtime-target", "runtime target", "pursuer", Position(41, 25), health, health,
                    status="engaged", morale=8)
    state.threats = [target]
    return state, target.id


class RuntimeEventTests(unittest.TestCase):
    def test_runtime_event_module_is_headless(self):
        code = "import sys; import jomon.runtime_events; assert 'curses' not in sys.modules"
        result = subprocess.run([sys.executable, "-c", code], check=False, capture_output=True, text=True)
        self.assertEqual(result.returncode, 0, result.stderr)

    def test_move_event_matches_resulting_view_and_blocked_move_is_silent(self):
        state = create_world("runtime move")
        session = GameSession(state)
        dx, dy = next(
            (dx, dy) for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
            if is_walkable(state, Position(state.position.x + dx, state.position.y + dy, state.position.z))
        )
        before = state.position
        outcome = session.submit(MoveCommand(dx, dy))
        self.assertEqual(len(outcome.events), 1)
        event = outcome.events[0]
        self.assertIsInstance(event, ActorMoved)
        self.assertEqual((event.from_position, event.to_position), (before, session.world_view().courier_position))
        self.assertEqual(event.movement_kind_id, "movement.step")
        with self.assertRaises(FrozenInstanceError):
            event.actor_id = "changed"

        rejected = session.submit(MoveCommand(0, 0))
        self.assertEqual(rejected.events, ())

    def test_interaction_event_uses_stable_choice_ids(self):
        state = create_world("runtime interaction")
        session = GameSession(state)
        choice = session.interaction_view().options[0]
        outcome = session.submit(InteractCommand(choice.target_id, choice.interaction_id))
        self.assertEqual(outcome.result_id, "interaction.resolved")
        self.assertEqual(len(outcome.events), 1)
        event = outcome.events[0]
        self.assertIsInstance(event, InteractionResolved)
        self.assertEqual((event.actor_id, event.target_id, event.interaction_id), (
            state.active_courier_id, choice.target_id, choice.interaction_id,
        ))

    def test_standard_attack_emits_causal_combat_events_and_view_agrees(self):
        state, target_id = armed_state("runtime attack")
        session = GameSession(state)
        before = session.actor_view(target_id)
        outcome = session.submit(AttackCommand(target_id))
        self.assertEqual([type(event) for event in outcome.events], [AttackResolved, DamageApplied])
        attack_event, damage_event = outcome.events
        self.assertEqual((attack_event.attacker_id, attack_event.target_id, attack_event.action_id), (
            state.active_courier_id, target_id, "attack.billhook",
        ))
        self.assertEqual(damage_event.target_actor_id, target_id)
        after = session.actor_view(target_id)
        self.assertEqual(before.health - after.health, damage_event.amount)

    def test_defeat_event_follows_attack_damage_and_status_change(self):
        state, target_id = armed_state("runtime defeat", health=1)
        outcome = GameSession(state).submit(AttackCommand(target_id))
        self.assertEqual(
            [type(event) for event in outcome.events],
            [AttackResolved, DamageApplied, StatusChanged, ActorDefeated],
        )
        self.assertEqual(outcome.events[-1].actor_id, target_id)

    def test_event_stream_is_deterministic_and_mechanically_inert(self):
        source, target_id = armed_state("runtime deterministic")
        first_state, second_state = copy.deepcopy(source), copy.deepcopy(source)
        first, second = GameSession(first_state), GameSession(second_state)
        first_outcome = first.submit(AttackCommand(target_id))
        second_outcome = second.submit(AttackCommand(target_id))
        self.assertEqual(first_outcome.events, second_outcome.events)
        self.assertEqual(first_state.to_dict(), second_state.to_dict())

        legacy_state, event_state = copy.deepcopy(source), copy.deepcopy(source)
        attack(legacy_state, target_id)
        event_outcome = GameSession(event_state).submit(AttackCommand(target_id))
        self.assertTrue(event_outcome.events)
        self.assertEqual(legacy_state.to_dict(), event_state.to_dict())

    def test_retreat_event_reports_only_committed_world_positions(self):
        state, _ = armed_state("runtime retreat")
        session = GameSession(state)
        before = state.position
        outcome = session.submit(RetreatCommand())
        self.assertEqual(len(outcome.events), 1)
        event = outcome.events[0]
        self.assertIsInstance(event, RetreatResolved)
        self.assertEqual((event.from_position, event.to_position), (before, session.world_view().courier_position))

    def test_relic_selection_event_is_committed_gameplay_not_ui_cursor_state(self):
        state = create_world("runtime relic")
        state.relics["river-glass ward"] = 1
        outcome = GameSession(state).submit(SelectCarriedRelicCommand("river-glass ward"))
        self.assertEqual(len(outcome.events), 1)
        self.assertIsInstance(outcome.events[0], CarriedRelicSelectionChanged)
        self.assertEqual(outcome.events[0].relic_id, "river-glass ward")

    def test_inventory_and_travel_events_are_typed_and_transient(self):
        session = GameSession.create("runtime inventory travel")
        equipped = session.equipment_view().slots[0]
        removed = session.submit(UnequipItemCommand(equipped.location_id))
        self.assertIsInstance(removed.events[0], ItemUnequipped)
        candidate = next(row for row in session.inventory_view().items if "equip" in row.legal_operations)
        equipped_outcome = session.submit(EquipItemCommand(candidate.id))
        self.assertIsInstance(equipped_outcome.events[0], ItemEquipped)
        destination = next(row.destination_id for row in session.travel_view().destinations if row.available)
        travelled = session.submit(TravelCommand(destination))
        self.assertIsInstance(travelled.events[0], TravelResolved)
        self.assertEqual(travelled.events[0].destination_id, destination)

    def test_multiple_voyage_interactions_use_stable_ids(self):
        state = create_world("runtime voyage choices")
        destination = next(row.destination_id for row in GameSession(state).travel_view().destinations if row.available)
        self.assertTrue(choose_destination(state, destination, forced_voyage="raiders")[0])
        session = GameSession(state)
        choices = [row for row in session.interaction_view().options if row.interaction_id.startswith("voyage.response.")]
        self.assertGreater(len(choices), 1)
        choice = next(row for row in choices if row.interaction_id == "voyage.response.y")
        outcome = session.submit(InteractCommand(choice.target_id, choice.interaction_id))
        self.assertEqual(outcome.result_id, "voyage.resolved")
        self.assertIsInstance(outcome.events[0], TravelResolved)

    def test_events_are_not_persisted_and_views_do_not_accumulate_them(self):
        state, target_id = armed_state("runtime save")
        session = GameSession(state)
        self.assertTrue(session.submit(AttackCommand(target_id)).events)
        session.world_view()
        session.actor_views()
        self.assertFalse(hasattr(session, "events"))
        with tempfile.TemporaryDirectory() as directory:
            path = session.save(Path(directory) / "save.json")
            serialized = path.read_text(encoding="utf-8")
            payload = json.loads(serialized)
            self.assertNotIn("events", payload)
            self.assertNotIn('"events"', serialized)
            restored = GameSession.load(path)
        self.assertEqual(restored.submit(MoveCommand(0, 0)).events, ())

    def test_selected_pack_presentation_does_not_change_event_stream(self):
        code = '''
import json
from dataclasses import asdict
from jomon.commands import AttackCommand
from jomon.session import GameSession
from jomon.state import Position, Threat, create_world
state=create_world("runtime-pack")
state.location,state.position,state.world_time="region",Position(40,25),8
state.weather,state.weapon="clear","billhook"
for z in (-1,0,1):
    for y in range(20,31):
        for x in range(30,55): state.region.tile_changes[f"{x},{y},{z}"]="."
target=Threat("runtime-target","runtime target","pursuer",Position(41,25),20,20,status="engaged",morale=8)
state.threats=[target]
outcome=GameSession(state).submit(AttackCommand(target.id))
print(json.dumps({"events":[(type(event).__name__,asdict(event)) for event in outcome.events], "message":state.messages[-1]}, sort_keys=True))
'''
        default_env = dict(os.environ)
        default_env.pop("JOMON_CONTENT_PACK", None)
        default = subprocess.run([sys.executable, "-c", code], capture_output=True, text=True, env=default_env, check=False)
        self.assertEqual(default.returncode, 0, default.stderr)
        with tempfile.TemporaryDirectory() as directory:
            root = alternate_pack(Path(directory) / "fixture")
            action_text_path = root / "action_text.json"
            action_text = json.loads(action_text_path.read_text(encoding="utf-8"))
            action_text["text"]["combat.attack.hit"] = "Fixture impact {weapon} {damage}{armour}{injury}; {threat} {health}/{maximum}."
            action_text["text"]["combat.attack.weapon.billhook"] = "fixture hook"
            action_text_path.write_text(json.dumps(action_text, indent=2) + "\n", encoding="utf-8")
            alternate_env = dict(default_env, JOMON_CONTENT_PACK=str(root))
            alternate = subprocess.run([sys.executable, "-c", code], capture_output=True, text=True, env=alternate_env, check=False)
        self.assertEqual(alternate.returncode, 0, alternate.stderr)
        default_result, alternate_result = json.loads(default.stdout), json.loads(alternate.stdout)
        self.assertEqual(default_result["events"], alternate_result["events"])
        self.assertNotEqual(default_result["message"], alternate_result["message"])

    def test_mechanical_sound_event_is_not_a_runtime_frontend_event(self):
        state = create_world("runtime sound boundary")
        state.sound_events.append(SoundEvent(state.position, 2))
        outcome = GameSession(state).submit(MoveCommand(0, 0))
        self.assertEqual(outcome.events, ())
        self.assertFalse(any(isinstance(event, SoundEvent) for event in outcome.events))


if __name__ == "__main__":
    unittest.main()
