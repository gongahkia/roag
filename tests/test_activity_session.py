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
