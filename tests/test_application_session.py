"""Headless application-boundary coverage after terminal frontend removal."""
from __future__ import annotations

from dataclasses import FrozenInstanceError
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

from jomon.commands import MoveCommand
from jomon.session import GameSession
from jomon.state import Position
from jomon.world import is_walkable


class ApplicationSessionTests(unittest.TestCase):
    def test_core_boundary_imports_without_pygame_or_curses(self):
        result = subprocess.run(
            [sys.executable, "-c", "import sys; import jomon.session,jomon.commands,jomon.views,jomon.runtime_events; print('pygame' in sys.modules, 'curses' in sys.modules)"],
            text=True, capture_output=True, check=True,
        )
        self.assertEqual(result.stdout.strip(), "False False")

    def test_session_move_view_and_format_15_save_round_trip_are_headless(self):
        session = GameSession.create("headless-session")
        before = session.world_view()
        position = before.courier_position
        command = next(
            MoveCommand(dx, dy)
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))
            if is_walkable(session._state, Position(position.x + dx, position.y + dy, position.z))
        )
        outcome = session.submit(command)
        self.assertTrue(outcome.changed)
        self.assertEqual(outcome.events[0].event_id, "actor.moved")
        with self.assertRaises(FrozenInstanceError):
            before.courier_position = position  # type: ignore[misc]
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "session.json"
            session.save(path)
            restored = GameSession.load(path)
        self.assertEqual(restored.world_view(), session.world_view())
        self.assertEqual(restored.revision, 0)


if __name__ == "__main__":
    unittest.main()
