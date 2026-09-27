from __future__ import annotations

import copy
import unittest
from dataclasses import FrozenInstanceError

from jomon.activities import activity_view
from jomon.commands import ActivityCommand
from jomon.production import gather, site_position
from jomon.session import GameSession
from jomon.state import create_world


def payload(state):
    result = state.to_dict()
    for key in ("messages", "history", "chronicle"):
        result.pop(key, None)
    return result


class ActivitySessionTests(unittest.TestCase):
    def test_views_are_immutable_and_observational(self):
        state = create_world("activity observation")
        before = payload(state)
        view = GameSession(state).activity_view("production")
        self.assertEqual(payload(state), before)
        self.assertTrue(view.options)
        with self.assertRaises(FrozenInstanceError):
            view.context_id = "other"
        with self.assertRaises(AttributeError):
            view.options.append(None)

    def test_production_command_matches_existing_reducer_and_emits_semantic_event(self):
        source = create_world("activity production")
        source.location = "region"
        source.position = site_position(source)
        legacy, through_session = copy.deepcopy(source), copy.deepcopy(source)
        self.assertTrue(gather(legacy, 0)[0])
        session = GameSession(through_session)
        option = next(row for row in session.activity_view("production").options
                      if row.action_id == "production.gather:0")
        self.assertTrue(option.available)
        outcome = session.submit(ActivityCommand("production", option.action_id))
        self.assertEqual(outcome.result_id, "activity.resolved")
        self.assertEqual(outcome.events[0].event_id, "gameplay.activity.resolved")
        self.assertEqual(payload(through_session), payload(legacy))

    def test_unknown_or_unavailable_activity_is_atomic(self):
        state = create_world("activity atomic")
        session = GameSession(state)
        before, revision = payload(state), session.revision
        self.assertEqual(session.submit(ActivityCommand("production", "production.make:missing")).result_id,
                         "activity.rejected")
        self.assertEqual(session.submit(ActivityCommand("unknown", "anything")).result_id,
                         "activity.rejected")
        self.assertEqual(payload(state), before)
        self.assertEqual(session.revision, revision)

    def test_activity_module_is_headless(self):
        import subprocess
        import sys

        result = subprocess.run(
            [sys.executable, "-c", "import sys; import jomon.activities; print('curses' in sys.modules or 'pygame' in sys.modules)"],
            capture_output=True, text=True, check=True,
        )
        self.assertEqual(result.stdout.strip(), "False")


if __name__ == "__main__":
    unittest.main()

class CharacterSetupSessionTests(unittest.TestCase):
    def test_character_setup_view_and_command_keep_point_buy_in_headless_boundary(self):
        from jomon.commands import CharacterSetupCommand

        state = create_world("setup session")
        session = GameSession(state)
        view = session.character_setup_view()
        self.assertTrue(view.available)
        self.assertTrue(view.crew)
        attributes = dict(view.default_attributes)
        before = payload(state)
        command = CharacterSetupCommand(
            view.crew[0].crew_id, view.crew[0].display_name,
            view.ancestry_ids[0], view.origin_ids[0], view.trait_ids[0],
            tuple(attributes.items()), view.default_competencies,
        )
        outcome = session.submit(command)
        self.assertEqual(outcome.result_id, "character.setup.completed")
        self.assertTrue(outcome.changed)
        self.assertTrue(state.courier.character_specified)
        self.assertFalse(session.character_setup_view().available)
        self.assertNotEqual(payload(state), before)
        from pathlib import Path
        from tempfile import TemporaryDirectory
        with TemporaryDirectory() as directory:
            path = Path(directory) / "character.json"
            session.save(path)
            loaded = GameSession.load(path)
        self.assertEqual(loaded.actor_view(view.crew[0].crew_id).display_name, view.crew[0].display_name)

    def test_character_setup_rejection_is_atomic(self):
        from jomon.commands import CharacterSetupCommand

        state = create_world("setup atomic")
        session = GameSession(state)
        view = session.character_setup_view()
        before, revision = payload(state), session.revision
        outcome = session.submit(CharacterSetupCommand(
            view.crew[0].crew_id, view.crew[0].display_name,
            view.ancestry_ids[0], view.origin_ids[0], view.trait_ids[0],
            tuple((*view.default_attributes, (view.attribute_ids[0], dict(view.default_attributes)[view.attribute_ids[0]] + 1))),
            view.default_competencies,
        ))
        self.assertEqual(outcome.result_id, "character.setup.rejected")
        self.assertEqual(payload(state), before)
        self.assertEqual(session.revision, revision)

    def test_vessel_activity_view_does_not_probe_mutating_rest_reducer(self):
        from jomon.vessel_refits import refit_station_at
        from jomon.world import map_rows
        from jomon.state import Position

        state = create_world("berth view")
        rows = map_rows(state)
        berth = next(
            Position(x, y, 0) for y, row in enumerate(rows) for x, token in enumerate(row)
            if token == "b"
        )
        state.position = berth
        self.assertEqual(refit_station_at(state), "berths")
        before = payload(state)
        view = GameSession(state).activity_view("vessel")
        self.assertIn("vessel.rest", {row.action_id for row in view.options})
        self.assertEqual(payload(state), before)

class ActiveGameplayCommandTests(unittest.TestCase):
    def test_loadout_and_support_reuse_existing_actions(self):
        from jomon.actions import choose_support, choose_weapon

        source = create_world("loadout equivalence")
        legacy, through_session = copy.deepcopy(source), copy.deepcopy(source)
        self.assertTrue(choose_support(legacy, legacy.support).changed)
        self.assertTrue(choose_weapon(legacy, legacy.owned_weapons[0]).changed)
        session = GameSession(through_session)
        support = next(row for row in session.activity_view("support").options
                       if row.action_id == f"support.select:{through_session.support}")
        self.assertEqual(session.submit(ActivityCommand("support", support.action_id)).result_id, "activity.resolved")
        weapon = next(row for row in session.activity_view("loadout").options
                      if row.action_id == f"loadout.weapon:{through_session.owned_weapons[0]}")
        self.assertEqual(session.submit(ActivityCommand("loadout", weapon.action_id)).result_id, "activity.resolved")
        self.assertEqual(payload(through_session), payload(legacy))

    def test_active_activity_contexts_are_observational(self):
        state = create_world("activity coverage")
        session = GameSession(state)
        before = payload(state)
        for context in (
            "production", "preparation", "progression", "magic", "materials", "vessel",
            "circuits", "vehicle", "support", "passives", "station:gathering", "workline",
            "objective", "quest:regional", "quest:arc", "merchant", "bartender", "loadout",
        ):
            session.activity_view(context)
        self.assertEqual(payload(state), before)
